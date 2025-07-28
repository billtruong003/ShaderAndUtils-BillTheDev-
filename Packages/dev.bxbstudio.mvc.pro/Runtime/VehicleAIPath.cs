#region Namespaces

using System;
using System.Linq;
using System.Collections;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using Utilities;
using MVC.Core;
using MVC.Base;
using MVC.Utilities;
using MVC.Utilities.Internal;
using UnityEngine.SceneManagement;

#endregion

namespace MVC.AI
{
	[AddComponentMenu("Multiversal Vehicle Controller/AI/AI Path")]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(-80)]
	public class VehicleAIPath : VehicleZonesContainer
	{
		#region Enumerators

		public enum CurveType { Bezier, Hermite }

		#endregion

		#region Modules

		#region Jobs

#if !MVC_COMMUNITY
		[BurstCompile]
		private struct CurvesDistanceTablesJob : IJobParallelFor
		{
			#region Variables

			[ReadOnly]
			public NativeArray<BezierCurve> bezierCurves;
			[ReadOnly]
			public int lengthSamples;
			public NativeArray<float> curvesLength;
			public NativeArray<float> totalLength;

			#endregion

			#region Methods

			public void Execute(int index)
			{
				float length = bezierCurves[index].Length(lengthSamples);

				curvesLength[index] = length;
				totalLength[0] += length;
			}

			#endregion
		}
		[BurstCompile]
		private struct GeneratePointsJob : IJob
		{
			#region Variables

			[ReadOnly]
			public NativeArray<BezierCurve> bezierCurves;
			[ReadOnly]
			public int lengthSamples;
			[ReadOnly]
			public float spacing;
			[ReadOnly]
			public float totalLength;
			[ReadOnly]
			public bool loopedPath;
			public NativeList<SpacedPathPoint> spacedPoints;

			#endregion

			#region Methods

			public void Execute()
			{
				int curvesCount = bezierCurves.Length;
				float distance = default;

				for (int i = 0; i < curvesCount; i++)
				{
					var curve = bezierCurves[i];
					NativeList<SpacedCurvePoint> spacedCurvePoints = new(Allocator.Temp);
					var distanceTable = curve.DistanceTable(lengthSamples, Allocator.Temp);

					curve.GetSpacedPoints(spacing, distanceTable, spacedCurvePoints, ref distance);

					foreach (var point in spacedCurvePoints)
						spacedPoints.Add(new(point, i));

					spacedCurvePoints.Dispose();
					distanceTable.Dispose();
				}

				if (!loopedPath && curvesCount > 0)
					spacedPoints.Add(new(new(bezierCurves[^1].GetPoint(1f), 1f), curvesCount - 1));
			}

			#endregion
		}
		[BurstCompile]
		private struct AlignPointsToGroundJob : IJobParallelFor
		{
			#region Variables

			[ReadOnly]
			public NativeArray<RaycastHit> raycastResults;
			[ReadOnly]
			public NativeArray<int> raycastResultLayers;
			[ReadOnly]
			public LayerMask groundLayerMask;
			public NativeArray<SpacedPathPoint> spacedPoints;

			#endregion

			#region Methods

			public void Execute(int index)
			{
				var raycastHitLayer = raycastResultLayers[index];

				if (raycastHitLayer < 0 || !Utility.MaskHasLayer(groundLayerMask, raycastHitLayer))
					return;

				var raycastHit = raycastResults[index];
				var spacedPathPoint = spacedPoints[index];
				var curvePoint = spacedPathPoint.spacedCurvePoint.curvePoint;

				curvePoint.up = raycastHit.normal;
				curvePoint.right = math.cross(curvePoint.forward, curvePoint.up);
				curvePoint.rotation = quaternion.LookRotationSafe(curvePoint.forward, curvePoint.up);
				curvePoint.position = raycastHit.point;
				spacedPathPoint.spacedCurvePoint.curvePoint = curvePoint;
				spacedPoints[index] = spacedPathPoint;
			}

			#endregion
		}
#endif

		#endregion

		#region Components

		public struct SpacedPathPoint
		{
			#region Variables

			public SpacedCurvePoint spacedCurvePoint;
			public int curveIndex;

			#endregion

			#region Methods

			public readonly override bool Equals(object obj)
			{
				return obj is SpacedPathPoint point &&
					   spacedCurvePoint == point.spacedCurvePoint &&
					   curveIndex == point.curveIndex;
			}
			public readonly override int GetHashCode()
			{
				return HashCode.Combine(spacedCurvePoint, curveIndex);
			}

			#endregion

			#region Operators

			public static bool operator ==(SpacedPathPoint a, SpacedPathPoint b) => a.Equals(b);
			public static bool operator !=(SpacedPathPoint a, SpacedPathPoint b) => !(a == b);

			#endregion

			#region Constructors

			public SpacedPathPoint(SpacedCurvePoint spacedPoint, int curveIndex)
			{
				this.spacedCurvePoint = spacedPoint;
				this.curveIndex = curveIndex;
			}

			#endregion
		}

#if !MVC_COMMUNITY
		private struct VisualizerMeshAnchor
		{
			public Vector4 tangent;
			public Vector3 position;
			public Vector3 normal;
			public Vector3 left;
			public Vector3 right;
			public float opacity;
			public float heat;
		}
#endif

		#endregion

		#endregion

		#region Constants

		public const string VisualizerName = "path_visualizer";
		public const float DefaultVehicleFrictionForVelocityEvaluation = 1.25f;

		#endregion

		#region Variables

		#region Properties

		public GameObject Visualizer =>
#if MVC_COMMUNITY
			null;
#else
			visualizer;
#endif
		public bool IsShowingVisualizer =>
#if MVC_COMMUNITY
			false;
#else
			isShowingVisualizer;
#endif
		public float TotalLength =>
#if MVC_COMMUNITY
			default;
#else
			totalLength;
#endif
		public bool IsValid => bezierCurves != null && hermiteCurves != null && bezierCurves.Length == hermiteCurves.Length && bezierCurves.Length > 0;

#if !MVC_COMMUNITY
		private GameObject visualizer;
		private MeshFilter visualizerFilter;
		private MeshRenderer visualizerRenderer;
		private VisualizerMeshAnchor[] visualizerAnchors;
		private Camera[] followerCameras;
		private SpacedPathPoint[] spacedPoints;
		private int generatedPathLengthSamples;
		private float spacedPointsSpacing;
		private Coroutine showHideCoroutine;
		private bool isShowingVisualizer;
		private bool showHideInProgress;
		private float totalLength;
#endif

		#endregion

		#region Fields

		public CurveType curveType;
		public bool UseCardinal
		{
			get
			{
				return useCardinal;
			}
			set
			{
#if !MVC_COMMUNITY
				if (!(useCardinal = value))
					return;

				RefreshCardinal();
#endif
			}
		}
		public float CardinalScale
		{
			get
			{
				return cardinalScale;
			}
			set
			{
#if !MVC_COMMUNITY
				cardinalScale = value;

				if (useCardinal)
					RefreshCardinal();
#endif
			}
		}
		public BezierCurve[] BezierCurves
		{
			get
			{
#if !MVC_COMMUNITY
				if (bezier != null && CurvesCount < 1)
					TryLoadLegacyBezierPath();

#endif
				return bezierCurves;
			}
			set
			{
#if !MVC_COMMUNITY
				if (value == null)
					return;

				bezierCurves = value;

				if (bezierCurves.Length > 0)
					SyncBezierHermiteCurves(CurveType.Bezier);
				else
					hermiteCurves = new HermiteCurve[] { };
#endif
			}
		}
		public HermiteCurve[] HermiteCurves
		{
			get
			{
#if !MVC_COMMUNITY
				if (bezier != null && CurvesCount < 1)
					TryLoadLegacyBezierPath();

#endif
				return hermiteCurves;
			}
			set
			{
#if !MVC_COMMUNITY
				if (value == null)
					return;

				hermiteCurves = value;

				if (hermiteCurves.Length > 0)
					SyncBezierHermiteCurves(CurveType.Hermite);
				else
					bezierCurves = new BezierCurve[] { };
#endif
			}
		}
		public SpacedPathPoint? FirstPoint
		{
			get
			{
				if (CurvesCount > 0)
					return new(new(bezierCurves[0].GetPoint(0f), 0f), 0);

				return firstPointHasValue ? firstPoint : null;
			}
			set
			{
				if (CurvesCount > 0)
				{
					if (value.HasValue)
						bezierCurves[0].P0 = value.Value.spacedCurvePoint.curvePoint.position;

					return;
				}

				firstPointHasValue = value.HasValue;
				firstPoint = value.GetValueOrDefault();
			}
		}
		public float startOffset;
		public float DrawWidth
		{
			get
			{
				return drawWidth;
			}
			set
			{
				drawWidth = math.max(value, .1f);
			}
		}
		public int StartPointIndex { get; private set; }
		public int CurvesCount
		{
			get
			{
				return bezierCurves != null ? bezierCurves.Length : default;
			}
		}
		public int SpacedPointsCount =>
#if !MVC_COMMUNITY
			spacedPoints != null ? spacedPoints.Length :
#endif
			default;
		public bool LoopedPath
		{
			get
			{
				return loopedPath;
			}
			set
			{
#if !MVC_COMMUNITY
				if (loopedPath == value)
					return;
				else if (CurvesCount < 1)
					return;
				else if (loopedPath)
				{
					loopedPath = false;

					RemoveLastCurve();

					return;
				}

				var firstCurve = hermiteCurves[0];

				loopedPath = AddNextCurve(firstCurve.P0);
				loopedPath &= GetPreviousCurveIndex(0, out int lastCurveIndex);

				if (!loopedPath)
					return;

				var lastCurve = hermiteCurves[lastCurveIndex];

				loopedPath &= GetPreviousCurveIndex(lastCurveIndex, out int previousLastCurveIndex);

				if (!loopedPath)
					return;

				var previousLastCurve = hermiteCurves[previousLastCurveIndex];

				lastCurve.V1 = firstCurve.V0;
				lastCurve.V0 = previousLastCurve.V1;
				lastCurve.EndWidth = firstCurve.StartWidth;
				hermiteCurves[lastCurveIndex] = lastCurve;

				SyncBezierHermiteCurves(CurveType.Hermite);

				if (useCardinal)
					RefreshCardinal();
#endif
			}
		}
		public LayerMask groundLayerMask = -1;
		[Min(math.EPSILON)]
		public float groundDetectionRayHeight = .5f;
		[Range(0f, 2f)]
		public float visualizerSpeedMultiplier = 1f;
		[Min(0f)]
		public float visualizerShowHidePointDuration = .002f;
		public bool showVisualizerAtStart = true;
		public bool snapZones = true;

#if MVC_COMMUNITY
#pragma warning disable CS0169
#pragma warning disable CS0649
#endif
		[SerializeField]
		private Bezier.Path bezier;
		[SerializeField]
		private bool useCardinal;
		[SerializeField]
		private float cardinalScale = .5f;
		[SerializeField]
		private bool loopedPath;
		[SerializeField]
		private BezierCurve[] bezierCurves;
		[SerializeField]
		private HermiteCurve[] hermiteCurves;
		[SerializeField]
		private float[] curvesLength;
		[SerializeField]
		private float drawWidth = 5f;
		[SerializeField]
		private SpacedPathPoint firstPoint;
		[SerializeField]
		private bool firstPointHasValue;
#if MVC_COMMUNITY
#pragma warning restore CS0169
#pragma warning restore CS0649
#endif

		#endregion

		#endregion

		#region Methods

		#region Awake

		public void Restart()
		{
#if !MVC_COMMUNITY
			Awaken = false;

			Initialize();

			if (Settings.Problems.DisableToolkitBehaviourOnProblems(ToolkitSettings.SettingsEditorFoldout.AI, this) || Settings.Problems.DisableToolkitBehaviourOnProblems(ToolkitSettings.SettingsEditorFoldout.VFX, this) || CurvesCount < 1)
				return;

			followerCameras = Follower.GetComponentsInChildren<Camera>(true);

			GenerateSpacedPoints();
			AlignSpacedPointsToGround();
			CreatePathVisualizer();
			DrawPathVisualizer();

			if (loopedPath)
			{
				int spacedPointsCount = spacedPoints.Length;

				StartPointIndex = (int)math.round(Utility.Lerp(0, spacedPointsCount, startOffset));

				if (StartPointIndex == spacedPointsCount)
					StartPointIndex = 0;
			}
			else
				StartPointIndex = default;

			Awaken = true;
#endif
		}

#if !MVC_COMMUNITY
		private void Awake()
		{
			if (Awaken)
				return;

			Restart();
		}
		private void Initialize()
		{
			StartPointIndex = default;
			spacedPoints = null;

			if (visualizerFilter)
				Utility.Destroy(true, visualizerFilter.gameObject);

			if (visualizerRenderer)
				Utility.Destroy(true, visualizerRenderer.gameObject);

			visualizerFilter = null;
			visualizerRenderer = null;
			visualizerAnchors = null;
			followerCameras = null;
		}
#endif

		#endregion

		#region Utilities

		#region Static Methods

		private static int LoopIndex(int index, int pointsCount)
		{
			if (pointsCount < 1)
				return 0;

			while (index < 0)
				index += pointsCount;

			while (index >= pointsCount)
				index -= pointsCount;

			return index;
		}

		#endregion

		#region Virtual Methods

		public override VehicleAIZone AddAIZone(Vector3 position, VehicleAIZone.AIZoneType type)
		{
			var zone = base.AddAIZone(position, type);
#if !MVC_COMMUNITY
			int closestPathPointIndex = ClosestCurveIndex(position);

			if (closestPathPointIndex > -1)
			{
				var closestPathPoint = spacedPoints[closestPathPointIndex];

				switch (type)
				{
					case VehicleAIZone.AIZoneType.Brake:
						zone.BrakeSpeedTarget = closestPathPoint.spacedCurvePoint.curvePoint.TargetVelocity(DefaultVehicleFrictionForVelocityEvaluation, Physics.gravity.magnitude);

						break;
				}

				if (snapZones)
					zone.transform.SetPositionAndRotation(closestPathPoint.spacedCurvePoint.curvePoint.position, closestPathPoint.spacedCurvePoint.curvePoint.rotation);
			}
#endif

			return zone;
		}
		public override VehicleAudioZone AddAudioZone(Vector3 position, VehicleAudioZone.AudioZoneType type)
		{
			throw new NotImplementedException();
		}
		public override VehicleDamageZone AddDamageZone(Vector3 position, VehicleDamageZone.DamageZoneType type)
		{
			throw new NotImplementedException();
		}
		public override VehicleWeatherZone AddWeatherZone(Vector3 position, VehicleWeatherZone.WeatherZoneType type)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region Global Methods

		public void NewPath()
		{
#if !MVC_COMMUNITY
			float3 position = transform.position;

			bezierCurves = new BezierCurve[] { new(float3.zero, new(1f, 0f, 0f), new(0f, 0f, 1f), new(1f, 0f, 1f), drawWidth) };
			hermiteCurves = bezierCurves.Select(bezier => bezier.ToHermite()).ToArray();
			isShowingVisualizer = false;
			firstPointHasValue = true;
			firstPoint = default;
			useCardinal = false;

			if (visualizer)
				DestroyImmediate(visualizer);

			GenerateSpacedPoints();
#endif
		}
		public void ResetPath()
		{
#if !MVC_COMMUNITY
			bezierCurves = new BezierCurve[] { };
			hermiteCurves = new HermiteCurve[] { };
			curvesLength = new float[] { };
			spacedPoints = new SpacedPathPoint[] { };
			showVisualizerAtStart = true;
			isShowingVisualizer = false;
			firstPointHasValue = false;
			firstPoint = default;
			drawWidth = 5f;

			if (visualizer)
				DestroyImmediate(visualizer);
#endif
		}
		public bool AddNextCurve(float3 position)
		{
#if MVC_COMMUNITY
			return false;
#else
			BezierCurve bezierCurve;
			bool orgLoopedPath = loopedPath;

			if (orgLoopedPath)
				LoopedPath = false;

			if (CurvesCount < 1)
			{
				if (!firstPointHasValue)
				{
					firstPointHasValue = true;
					firstPoint = new(new(new()
					{
						position = position,
						rotation = Quaternion.identity,
						forward = transform.forward,
						right = transform.right,
						up = transform.up,
						radius = math.INFINITY
					}, 0f), 0);

					return false;
				}

				float3 firstPointPosition = firstPoint.spacedCurvePoint.curvePoint.position;
				float3 averagePoint = Utility.Average(firstPointPosition, position);

				bezierCurve = new(firstPointPosition, Utility.Average(firstPointPosition, averagePoint), Utility.Average(averagePoint, position), position, drawWidth);
			}
			else
			{
				var lastCurve = bezierCurves[^1];
				float3 p0 = lastCurve.P3;
				float3 p1 = p0 * 2f - lastCurve.P2;

				bezierCurve = new(p0, p1, Utility.Average(position, p1), position, lastCurve.EndWidth, drawWidth);
			}

			bezierCurves ??= new BezierCurve[] { };
			hermiteCurves ??= new HermiteCurve[] { };

			Array.Resize(ref bezierCurves, bezierCurves.Length + 1);
			Array.Resize(ref hermiteCurves, hermiteCurves.Length + 1);

			bezierCurves[^1] = bezierCurve;
			hermiteCurves[^1] = bezierCurve.ToHermite();
			LoopedPath = orgLoopedPath;

			if (useCardinal)
				RefreshCardinal();

			return true;
#endif
		}
		public bool AddNextCurve(float3 position, out BezierCurve bezierCurve, out HermiteCurve hermiteCurve)
		{
			bezierCurve = default;
			hermiteCurve = default;

#if MVC_COMMUNITY
			return false;
#else
			if (!AddNextCurve(position))
				return false;

			bezierCurve = bezierCurves[^1];
			hermiteCurve = hermiteCurves[^1];

			return true;
#endif
		}
		public bool SplitCurve(int curveIndex, float3 position)
		{
#if MVC_COMMUNITY
			return false;
#else
			int curvesCount = CurvesCount;

			if (curveIndex < 0 || curveIndex >= curvesCount)
				throw GetArgumentOutOfRangeException(nameof(curveIndex), curveIndex, curvesCount);

			float t = Utility.Clamp01(InverseCurvePosition(curveIndex, position));

			if (Mathf.Approximately(t, 0f) || Mathf.Approximately(t, 1f))
				return false;

			var curve = bezierCurves[curveIndex];
			// Get original curve points
			float3 p0 = curve.P0;
			float3 p1 = curve.P1;
			float3 p2 = curve.P2;
			float3 p3 = curve.P3;
			// Compute intermediate points
			float3 q0 = (1f - t) * p0 + t * p1;
			float3 q1 = (1f - t) * p1 + t * p2;
			float3 q2 = (1f - t) * p2 + t * p3;
			// Compute next level points
			float3 r0 = (1f - t) * q0 + t * q1;
			float3 r1 = (1f - t) * q1 + t * q2;
			// Compute split point
			float3 s0 = (1f - t) * r0 + t * r1;
			// Create curves
			float startWidth = curve.StartWidth;
			float endWidth = curve.EndWidth;
			float splitWidth = Utility.LerpUnclamped(startWidth, endWidth, t);
			BezierCurve newPreviousCurve = new(p0, q0, r0, s0, startWidth, splitWidth);
			BezierCurve newNextCurve = new(s0, r1, q2, p3, splitWidth, endWidth);
			// Insert curves
			int insertIndex = curveIndex + 1;

			Array.Resize(ref bezierCurves, curvesCount + 1);
			Array.Resize(ref hermiteCurves, curvesCount + 1);
			Array.Copy(bezierCurves, insertIndex, bezierCurves, insertIndex + 1, curvesCount - insertIndex);
			Array.Copy(hermiteCurves, insertIndex, hermiteCurves, insertIndex + 1, curvesCount - insertIndex);

			bezierCurves[curveIndex] = newPreviousCurve;
			bezierCurves[curveIndex + 1] = newNextCurve;
			hermiteCurves[curveIndex] = newPreviousCurve.ToHermite();
			hermiteCurves[curveIndex + 1] = newNextCurve.ToHermite();

			if (useCardinal)
				RefreshCardinal();

			return true;
#endif
		}
		public void MergeCurves(int curve1Index, int curve2Index)
		{
#if !MVC_COMMUNITY
			if (curve1Index == curve2Index)
				throw new ArgumentException($"`{nameof(curve1Index)}` and `{nameof(curve2Index)}` must not match");

			int curvesCount = CurvesCount;

			if (curve1Index < 0 || curve1Index >= curvesCount)
				throw GetArgumentOutOfRangeException(nameof(curve1Index), curve1Index, curvesCount);
			else if (curve2Index < 0 || curve2Index >= curvesCount)
				throw GetArgumentOutOfRangeException(nameof(curve2Index), curve2Index, curvesCount);

			var curve1 = bezierCurves[curve1Index];
			var curve2 = bezierCurves[curve2Index];

			if (!curve2.P0.Equals(curve1.P3))
				throw new ArgumentException("Curves must be neighbors: P3 of Curve 1 is not equal to P0 of Curve 2");

			BezierCurve curve = new(curve1.P0, curve1.P1, curve2.P2, curve2.P3, curve1.StartWidth, curve2.EndWidth);

			bezierCurves[curve1Index] = curve;
			hermiteCurves[curve1Index] = curve.ToHermite();

			if (curve2Index + 1 < curvesCount)
			{
				Array.Copy(bezierCurves, curve2Index + 1, bezierCurves, curve2Index, curvesCount - curve2Index - 1);
				Array.Copy(hermiteCurves, curve2Index + 1, hermiteCurves, curve2Index, curvesCount - curve2Index - 1);
			}

			Array.Resize(ref bezierCurves, curvesCount - 1);
			Array.Resize(ref hermiteCurves, curvesCount - 1);

			if (useCardinal)
				RefreshCardinal();
#endif
		}
		public bool RemoveFirstCurve()
		{
#if MVC_COMMUNITY
			return false;
#else
			return RemoveCurve(true);
#endif
		}
		public bool RemoveLastCurve()
		{
#if MVC_COMMUNITY
			return false;
#else
			return RemoveCurve(false);
#endif
		}
		public bool GetNextCurveIndex(int curveIndex, out int nextCurveIndex)
		{
			nextCurveIndex = -1;

#if MVC_COMMUNITY
			return false;
#else
			int curvesCount = CurvesCount;

			if (curvesCount < 2)
				return false;
			else if (!loopedPath && curveIndex + 1 >= curvesCount)
				return false;

			nextCurveIndex = LoopIndex(curveIndex + 1, curvesCount);

			return true;
#endif
		}
		public bool GetNextCurveIndex(int curveIndex, out int nextCurveIndex, out BezierCurve nextBezierCurve)
		{
			nextBezierCurve = default;
#if MVC_COMMUNITY
			nextCurveIndex = -1;

			return false;
#else

			if (!GetNextCurveIndex(curveIndex, out nextCurveIndex))
				return false;

			nextBezierCurve = bezierCurves[nextCurveIndex];

			return true;
#endif
		}
		public bool GetNextCurveIndex(int curveIndex, out int nextCurveIndex, out HermiteCurve nextHermiteCurve)
		{
			nextHermiteCurve = default;
#if MVC_COMMUNITY
			nextCurveIndex = -1;

			return false;
#else

			if (!GetNextCurveIndex(curveIndex, out nextCurveIndex))
				return false;

			nextHermiteCurve = hermiteCurves[nextCurveIndex];

			return true;
#endif
		}
		public bool GetPreviousCurveIndex(int curveIndex, out int previousCurveIndex)
		{
			previousCurveIndex = -1;

#if MVC_COMMUNITY
			return false;
#else
			int curvesCount = CurvesCount;

			if (curvesCount < 2)
				return false;
			else if (!loopedPath && curveIndex < 1)
				return false;

			previousCurveIndex = LoopIndex(curveIndex - 1, curvesCount);

			return true;
#endif
		}
		public bool GetPreviousCurveIndex(int curveIndex, out int previousCurveIndex, out BezierCurve nextBezierCurve)
		{
			nextBezierCurve = default;
#if MVC_COMMUNITY
			previousCurveIndex = -1;

			return false;
#else

			if (!GetPreviousCurveIndex(curveIndex, out previousCurveIndex))
				return false;

			nextBezierCurve = bezierCurves[previousCurveIndex];

			return true;
#endif
		}
		public bool GetPreviousCurveIndex(int curveIndex, out int previousCurveIndex, out HermiteCurve nextHermiteCurve)
		{
			nextHermiteCurve = default;
#if MVC_COMMUNITY
			previousCurveIndex = -1;

			return false;
#else

			if (!GetPreviousCurveIndex(curveIndex, out previousCurveIndex))
				return false;

			nextHermiteCurve = hermiteCurves[previousCurveIndex];

			return true;
#endif
		}
		public bool GetPreviousSpacedPointIndex(int pointIndex, out int previousPointIndex)
		{
			previousPointIndex = -1;

#if MVC_COMMUNITY
			return false;
#else
			int pointsCount = SpacedPointsCount;

			if (pointsCount < 2)
				return false;
			else if (!loopedPath && pointIndex < 1)
				return false;

			previousPointIndex = LoopIndex(pointIndex - 1, pointsCount);

			return true;
#endif
		}
		public bool GetPreviousSpacedPointIndex(int pointIndex, out int previousPointIndex, out SpacedPathPoint previousPoint)
		{
			previousPoint = default;
#if MVC_COMMUNITY
			previousPointIndex = -1;

			return false;
#else

			if (!GetPreviousSpacedPointIndex(pointIndex, out previousPointIndex))
				return false;

			previousPoint = spacedPoints[previousPointIndex];

			return true;
#endif
		}
		public bool GetNextSpacedPointIndex(int pointIndex, out int nextPointIndex)
		{
			nextPointIndex = -1;

#if MVC_COMMUNITY
			return false;
#else
			int pointsCount = SpacedPointsCount;

			if (pointsCount < 2)
				return false;
			else if (!loopedPath && pointIndex < 1)
				return false;

			nextPointIndex = LoopIndex(pointIndex + 1, pointsCount);

			return true;
#endif
		}
		public bool GetNextSpacedPointIndex(int pointIndex, out int nextPointIndex, out SpacedPathPoint nextPoint)
		{
			nextPoint = default;
#if MVC_COMMUNITY
			nextPointIndex = -1;

			return false;
#else

			if (!GetNextSpacedPointIndex(pointIndex, out nextPointIndex))
				return false;

			nextPoint = spacedPoints[nextPointIndex];

			return true;
#endif
		}
		public int ClosestSpacedPointIndex(float3 position)
		{
#if MVC_COMMUNITY
			return -1;
#else
			if (SpacedPointsCount < 1)
				return -1;

			return ClosestSpacedPointIndex(position, 0, spacedPoints.Length);
#endif
		}
		public int ClosestSpacedPointIndex(float3 position, int startIndex, int count)
		{
#if MVC_COMMUNITY
			return -1;
#else
			if (count < 1 || startIndex < 0 || startIndex >= count || SpacedPointsCount < 1)
				return -1;

			int closestSpacedPointIndex = -1;
			float distance = default;

			for (int i = startIndex; i < count; i++)
			{
				var point = spacedPoints[i];
				float newDistance = Utility.DistanceSqr(position, point.spacedCurvePoint.curvePoint.position);

				if (closestSpacedPointIndex < 0 || newDistance < distance)
				{
					distance = newDistance;
					closestSpacedPointIndex = i;
				}
			}

			return closestSpacedPointIndex;
#endif
		}
		public int ClosestSpacedPointIndex(float3 position, float distanceRange)
		{
#if MVC_COMMUNITY
			return -1;
#else
			int closedSpacedPointIndex = ClosestSpacedPointIndex(position);

			if (closedSpacedPointIndex < 0)
				return -1;

			var spacedPoint = spacedPoints[closedSpacedPointIndex];

			if (Utility.DistanceSqr(position, spacedPoint.spacedCurvePoint.curvePoint.position) > distanceRange * distanceRange)
				return -1;

			return closedSpacedPointIndex;
#endif
		}
		public int ClosestCurveIndex(float3 position)
		{
#if MVC_COMMUNITY
			return -1;
#else
			int closedSpacedPointIndex = ClosestSpacedPointIndex(position);

			if (closedSpacedPointIndex < 0)
				return -1;

			return spacedPoints[closedSpacedPointIndex].curveIndex;
#endif
		}
		public int ClosestCurveIndex(float3 position, float distanceRange)
		{
#if MVC_COMMUNITY
			return -1;
#else
			int closedSpacedPointIndex = ClosestSpacedPointIndex(position, distanceRange);

			if (closedSpacedPointIndex < 0)
				return -1;

			return spacedPoints[closedSpacedPointIndex].curveIndex;
#endif
		}
		public int ClosestCurveIndexFast(float3 position)
		{
#if MVC_COMMUNITY
			return -1;
#else
			int curvesCount = CurvesCount;
			int closestCurveIndex = -1;
			float p0Distance = default;
			float p3Distance = default;

			for (int i = 0; i < curvesCount; i++)
			{
				var curve = bezierCurves[i];
				float3 p0 = curve.P0;
				float3 p3 = curve.P3;
				float newP0Distance = Utility.DistanceSqr(position, p0);
				float newP3Distance = Utility.DistanceSqr(position, p3);

				if (closestCurveIndex < 0 || newP0Distance < p0Distance || newP3Distance < p3Distance)
				{
					p0Distance = newP0Distance;
					p3Distance = newP3Distance;
					closestCurveIndex = i;
				}
			}

			return closestCurveIndex;
#endif
		}
		public float InverseCurvePosition(int curveIndex, float3 position)
		{
#if MVC_COMMUNITY
			return default;
#else
			int curvesCount = CurvesCount;

			if (curveIndex < 0 || curveIndex >= curvesCount)
				throw GetArgumentOutOfRangeException(nameof(curveIndex), curveIndex, curvesCount);

			// Get curve spaced points
			NativeList<SpacedCurvePoint> spacedPoints = new(Allocator.Temp);

			GetCurveSpacedPoints(curveIndex, spacedPoints, true);

			// Find the closest spaced point
			int spacedPointsCount = spacedPoints.Length;
			int closestSpacedPointIndex = -1;
			float distance = 0f;

			for (int i = 0; i < spacedPointsCount; i++)
			{
				float newDistance = Utility.DistanceSqr(spacedPoints[i].curvePoint.position, position);

				if (closestSpacedPointIndex < 0 || newDistance < distance)
				{
					closestSpacedPointIndex = i;
					distance = newDistance;
				}
			}

			// Find the second closest point (must be a neighbour point)
			int secondClosestSpacedPointIndex;
			int previousSpacedPointIndex = closestSpacedPointIndex - 1;
			int nextSpacedPointIndex = closestSpacedPointIndex + 1;

			if (closestSpacedPointIndex < 1)
				secondClosestSpacedPointIndex = nextSpacedPointIndex;
			else if (closestSpacedPointIndex + 1 >= spacedPointsCount)
				secondClosestSpacedPointIndex = closestSpacedPointIndex;
			else
			{
				var previousSpacedPoint = spacedPoints[previousSpacedPointIndex];
				var nextSpacedPoint = spacedPoints[nextSpacedPointIndex];
				float previousPointDistance = Utility.DistanceSqr(position, previousSpacedPoint.curvePoint.position);
				float nextPointDistance = Utility.DistanceSqr(position, nextSpacedPoint.curvePoint.position);

				if (previousPointDistance < nextPointDistance)
					secondClosestSpacedPointIndex = previousSpacedPointIndex;
				else
					secondClosestSpacedPointIndex = nextSpacedPointIndex;
			}

			// Sort the most two closest points
			int firstSpacedPointIndex, secondSpacedPointIndex;

			if (closestSpacedPointIndex > secondClosestSpacedPointIndex)
			{
				firstSpacedPointIndex = secondClosestSpacedPointIndex;
				secondSpacedPointIndex = closestSpacedPointIndex;
			}
			else
			{
				firstSpacedPointIndex = closestSpacedPointIndex;
				secondSpacedPointIndex = secondClosestSpacedPointIndex;
			}

			// Find the position's T value relative to curve
			var firstSpacedPoint = spacedPoints[firstSpacedPointIndex];
			var secondsSpacedPoint = spacedPoints[secondSpacedPointIndex];
			float spacedPointsT = Utility.InverseLerpUnclamped(firstSpacedPoint.curvePoint.position, secondsSpacedPoint.curvePoint.position, position);
			float t = math.lerp(firstSpacedPoint.t, secondsSpacedPoint.t, spacedPointsT);

			spacedPoints.Dispose();

			return t;
#endif
		}
		public void SetCurvesWidth(float width)
		{
			int curvesCount = CurvesCount;

			if (curvesCount < 1)
				return;

			width = math.max(width, .1f);

			for (int i = 0; i < curvesCount; i++)
			{
				var curve = bezierCurves[i];

				curve.StartWidth = curve.EndWidth = width;
				bezierCurves[i] = curve;
			}

			SyncBezierHermiteCurves(CurveType.Bezier);
		}
		public void SyncBezierHermiteCurves()
		{
			SyncBezierHermiteCurves(curveType);
		}
		public void SyncBezierHermiteCurves(CurveType sourceCurveType)
		{
#if !MVC_COMMUNITY
			switch (sourceCurveType)
			{
				case CurveType.Hermite:
					if (hermiteCurves == null || hermiteCurves.Length < 1)
						goto log_invalid_warning;

					bezierCurves = hermiteCurves.Select(hermite => hermite.ToBezier()).ToArray();

					break;

				default:
					if (bezierCurves == null || bezierCurves.Length < 1)
						goto log_invalid_warning;

					hermiteCurves = bezierCurves.Select(bezier => bezier.ToHermite()).ToArray();

					break;
			}

			return;

		log_invalid_warning:
			ToolkitDebug.Warning("Cannot sync an invalid path with empty curve arrays", gameObject);
#endif
		}
		public void OffsetCurves(float3 offset)
		{
#if !MVC_COMMUNITY
			switch (curveType)
			{
				case CurveType.Hermite:
					if (hermiteCurves == null || hermiteCurves.Length < 1)
						return;

					for (int i = 0; i < hermiteCurves.Length; i++)
						hermiteCurves[i].Offset(offset);

					break;

				default:
					if (bezierCurves == null || bezierCurves.Length < 1)
						return;

					for (int i = 0; i < bezierCurves.Length; i++)
						bezierCurves[i].Offset(offset);

					break;
			}

			SyncBezierHermiteCurves();
#endif
		}
		public SpacedPathPoint GetSpacedPoint(int index)
		{
#if MVC_COMMUNITY
			return default;
#else
			return spacedPoints[LoopIndex(index, spacedPoints.Length)];
#endif
		}
		public float EvaluateSpacedPointSteerAngle(int index, float wheelBase)
		{
#if MVC_COMMUNITY
			return default;
#else
			var spacedPoint = GetSpacedPoint(index);

			return math.atan(spacedPoint.spacedCurvePoint.curvePoint.curvature * wheelBase);
#endif
		}
		public bool TryGetSpacedPoint(int index, out SpacedPathPoint spacedPoint)
		{
			spacedPoint = default;

#if MVC_COMMUNITY
			return false;
#else
			if (spacedPoints == null || index < 0 || index >= spacedPoints.Length)
				return false;

			spacedPoint = spacedPoints[index];

			return true;
#endif
		}
		public SpacedCurvePoint[] GetCurveSpacedPoints(int curveIndex, bool addStartEndPoints)
		{
#if MVC_COMMUNITY
			return null;
#else
			NativeList<SpacedCurvePoint> tempList = new(Allocator.Temp);
			
			GetCurveSpacedPoints(curveIndex, tempList, addStartEndPoints);

			var spacedPoints = tempList.ToArray();

			tempList.Dispose();

			return spacedPoints;
#endif
		}
		public void GetCurveSpacedPoints(int curveIndex, NativeList<SpacedCurvePoint> list, bool addStartEndPoints)
		{
#if !MVC_COMMUNITY
			int curvesCount = bezierCurves.Length;

			if (curveIndex < 0 || curveIndex >= curvesCount)
				throw GetArgumentOutOfRangeException(nameof(curveIndex), curveIndex, curvesCount);

			var curve = bezierCurves[curveIndex];

			if (addStartEndPoints)
				list.Add(new(curve.GetPoint(0f), 0f));

			for (int i = 0; i < spacedPoints.Length; i++)
			{
				if (spacedPoints[i].curveIndex > curveIndex)
					break;
				else if (spacedPoints[i].curveIndex != curveIndex)
					continue;

				list.Add(spacedPoints[i].spacedCurvePoint);
			}

			if (addStartEndPoints || loopedPath && curveIndex + 1 == curvesCount)
				list.Add(new(curve.GetPoint(1f), 1f));
#endif
		}
		public float GetCurveLength(int index)
		{
#if MVC_COMMUNITY
			return default;
#else
			if (curvesLength == null || curvesLength.Length != CurvesCount)
				CalculateCurvesLength();

			return curvesLength[index];
#endif
		}
		public void GenerateSpacedPoints()
		{
#if !MVC_COMMUNITY
			CalculateCurvesLength(out NativeArray<BezierCurve> bezierCurves, out NativeArray<float> curvesLength);

			if (CurvesCount < 1)
			{
				this.spacedPoints = new SpacedPathPoint[] { };

				bezierCurves.Dispose();
				curvesLength.Dispose();

				return;
			}

			NativeList<SpacedPathPoint> spacedPoints = new(Allocator.TempJob);
			var settings = Settings;

			new GeneratePointsJob
			{
				bezierCurves = bezierCurves,
				lengthSamples = generatedPathLengthSamples = settings.AIPathLengthSamples,
				spacing = spacedPointsSpacing = settings.AIPathSpacedPointsSpacing,
				spacedPoints = spacedPoints,
				totalLength = totalLength,
				loopedPath = loopedPath,
			}.
			Schedule().Complete();

			this.spacedPoints = spacedPoints.AsArray().ToArray();

			spacedPoints.Dispose();
			bezierCurves.Dispose();
			curvesLength.Dispose();
#endif
		}
		public void AlignSpacedPointsToGround()
		{
#if !MVC_COMMUNITY
			int spacedPointsCount = this.spacedPoints.Length;
			NativeArray<RaycastCommand> raycastCommands = new(spacedPointsCount, Allocator.TempJob);
			NativeArray<RaycastHit> raycastResults = new(spacedPointsCount, Allocator.TempJob);

#if UNITY_2022_2_OR_NEWER
			QueryParameters queryParameters = QueryParameters.Default;
			
			queryParameters.layerMask = groundLayerMask;
			queryParameters.hitTriggers = QueryTriggerInteraction.Ignore;
#else
			bool orgQueriesHitTriggers = Physics.queriesHitTriggers;

			Physics.queriesHitTriggers = false;
#endif

			for (int i = 0; i < spacedPointsCount; i++)
			{
				var spacedPoint = this.spacedPoints[i];
				
				raycastCommands[i] = new(spacedPoint.spacedCurvePoint.curvePoint.position + groundDetectionRayHeight * Utility.Float3Up, -Vector3.up,
#if UNITY_2022_2_OR_NEWER
					queryParameters, float.MaxValue
#else
					layerMask: groundLayerMask
#endif
				);
			}

			RaycastCommand.ScheduleBatch(raycastCommands, raycastResults, spacedPointsCount).Complete();
			raycastCommands.Dispose();

			NativeArray<int> raycastResultLayers = new(raycastResults.Length, Allocator.TempJob);
			NativeArray<SpacedPathPoint> spacedPoints = new(this.spacedPoints, Allocator.TempJob);

			for (int i = 0; i < raycastResultLayers.Length; i++)
			{
				var resultCollider = raycastResults[i].collider;

				raycastResultLayers[i] = !resultCollider ? -1 : resultCollider.gameObject.layer;
			}

			new AlignPointsToGroundJob()
			{
				spacedPoints = spacedPoints,
				raycastResults = raycastResults,
				raycastResultLayers = raycastResultLayers,
				groundLayerMask = groundLayerMask,
			}
			.Schedule(spacedPointsCount, spacedPointsCount).Complete();

			this.spacedPoints = spacedPoints.ToArray();
#if !UNITY_2022_2_OR_NEWER
			Physics.queriesHitTriggers = orgQueriesHitTriggers;
#endif

			raycastResultLayers.Dispose();
			raycastResults.Dispose();
			spacedPoints.Dispose();
#endif
		}
		public void CreatePathVisualizer()
		{
#if !MVC_COMMUNITY
			if (!Settings.useAIPathVisualizer)
				return;

			var visualizerTransform = transform.Find(VisualizerName);

			if (visualizerTransform)
				visualizer = visualizerTransform.gameObject;

			if (visualizer)
				DestroyImmediate(visualizer);

			visualizer = new(VisualizerName)
			{
				layer = gameObject.layer
			};
			visualizerFilter = visualizer.AddComponent<MeshFilter>();
			visualizerRenderer = visualizer.AddComponent<MeshRenderer>();
			visualizerRenderer.sharedMaterial = Settings.pathVisualizerMaterial;
			visualizerRenderer.shadowCastingMode = ShadowCastingMode.Off;

			visualizer.transform.SetParent(transform, true);

			if (Settings.useHideFlags)
				visualizer.hideFlags = HideFlags.HideInHierarchy;

			if (CurvesCount < 1)
				throw new InvalidOperationException("Could not create visualizer. Path need to have at least one curve");
			else if (SpacedPointsCount < 1)
				throw new InvalidOperationException("Could not create visualizer. Spaced points need to be generated first");

			int pointsLength = spacedPoints.Length;
			float groundOffset = Settings.pathVisualizerGroundOffset;
			float width = Settings.pathVisualizerWidth;

			visualizerAnchors = new VisualizerMeshAnchor[pointsLength];
			isShowingVisualizer = showVisualizerAtStart;

			for (int i = 0; i < pointsLength; i++)
			{
				SpacedPathPoint point = GetSpacedPoint(i);
				float3 up = point.spacedCurvePoint.curvePoint.up;
				float3 position = point.spacedCurvePoint.curvePoint.position + groundOffset * up;
				float3 right = width * .5f * point.spacedCurvePoint.curvePoint.right;
				float3 tangentVector = position - right;
				float4 tangent = new(tangentVector.x, tangentVector.y, tangentVector.z, 1f);

				visualizerAnchors[i] = new()
				{
					tangent = tangent,
					position = position,
					normal = up,
					left = position - right,
					right = position + right,
					heat = default,
					opacity = Utility.BoolToNumber(showVisualizerAtStart)
				};
			}
#endif
		}
		public void DrawPathVisualizer()
		{
#if !MVC_COMMUNITY
			if (visualizerAnchors == null || visualizerAnchors.Length < 1 || !visualizer)
				throw new InvalidOperationException("Cannot draw path visualizer. Create path visualizer first");

			int anchorsLength = visualizerAnchors.Length + Utility.BoolToNumber(loopedPath);
			var heatColorGradient = Settings.pathVisualizerHeatColorGradient;
			Vector3[] vertices = new Vector3[anchorsLength * 4];
			Vector4[] tangents = new Vector4[vertices.Length];
			Vector3[] normals = new Vector3[vertices.Length];
			Vector2[] uvs = new Vector2[vertices.Length];
			Color[] colors = new Color[vertices.Length];
			int[] triangles = new int[anchorsLength * 6];
			float passedDistance = 0f;
			Mesh visualizerMesh = new()
			{
				name = $"{name}_visualizer_mesh"
			};

			for (int i = 0; i < visualizerAnchors.Length - 1; i++)
			{
				var anchor = visualizerAnchors[i + 1];
				var lastAnchor = visualizerAnchors[i];
				float anchorLength = Utility.Distance(lastAnchor.position, anchor.position);
				Color anchorColor = heatColorGradient.Evaluate(anchor.heat);

				anchorColor.a *= anchor.opacity;

				triangles[i * 6 + 0] = i * 4 + 0;
				triangles[i * 6 + 1] = i * 4 + 2;
				triangles[i * 6 + 2] = i * 4 + 1;
				triangles[i * 6 + 3] = i * 4 + 2;
				triangles[i * 6 + 4] = i * 4 + 3;
				triangles[i * 6 + 5] = i * 4 + 1;
				vertices[i * 4 + 0] = lastAnchor.left;
				vertices[i * 4 + 1] = lastAnchor.right;
				vertices[i * 4 + 2] = anchor.left;
				vertices[i * 4 + 3] = anchor.right;
				tangents[i * 4 + 0] = lastAnchor.tangent;
				tangents[i * 4 + 1] = lastAnchor.tangent;
				tangents[i * 4 + 2] = anchor.tangent;
				tangents[i * 4 + 3] = anchor.tangent;
				normals[i * 4 + 0] = lastAnchor.normal;
				normals[i * 4 + 1] = lastAnchor.normal;
				normals[i * 4 + 2] = anchor.normal;
				normals[i * 4 + 3] = anchor.normal;
				colors[i * 4 + 0] = anchorColor;
				colors[i * 4 + 1] = anchorColor;
				colors[i * 4 + 2] = anchorColor;
				colors[i * 4 + 3] = anchorColor;
				uvs[i * 4 + 0] = new(0f, passedDistance);
				uvs[i * 4 + 1] = new(1f, passedDistance);
				uvs[i * 4 + 2] = new(0f, passedDistance + anchorLength);
				uvs[i * 4 + 3] = new(1f, passedDistance + anchorLength);

				passedDistance += anchorLength;
			}

			visualizerMesh.vertices = vertices;
			visualizerMesh.normals = normals;
			visualizerMesh.tangents = tangents;
			visualizerMesh.triangles = triangles;
			visualizerMesh.colors = colors;
			visualizerMesh.uv = uvs;

			if (Application.isPlaying)
				visualizerFilter.mesh = visualizerMesh;
			else
				visualizerFilter.sharedMesh = visualizerMesh;
#endif
		}
		public void RefreshPathVisualizer()
		{
#if !MVC_COMMUNITY
			Vehicle playerVehicle;

			if (visualizerAnchors == null || visualizerAnchors.Length < 1 || !(playerVehicle = Manager.PlayerVehicle))
				return;

			float indexRange = Settings.AIPathSpacedPointsPerMeter * 50f;
			int closestPointIndex = ClosestSpacedPointIndex(playerVehicle.transform.position);
			int startRange = showHideInProgress ? 0 : (int)math.floor(closestPointIndex - indexRange * .5f);
			int endRange = showHideInProgress ? visualizerAnchors.Length - 1 : (int)math.ceil(closestPointIndex + indexRange * 1.5f);

			if (!loopedPath)
			{
				if (startRange < 0)
					startRange = 0;

				if (endRange >= visualizerAnchors.Length)
					endRange = visualizerAnchors.Length - 1;
			}

			var playerPathFollower = playerVehicle.GetComponent<VehicleAIPathFollower>();
			var vehicleStats = playerVehicle.Stats;
			float vehicleFriction = vehicleStats.averageWheelsForwardFrictionStiffness;
			float vehicleVelocity = vehicleStats.currentSpeed / 3.6f;
			Color32[] colors = visualizerFilter.mesh.colors32;
			int anchorPointsCount = visualizerAnchors.Length;
			float gravity = Physics.gravity.magnitude;

			for (int i = startRange; i <= endRange; i++)
			{
				int index = LoopIndex(i, anchorPointsCount);
				var anchor = visualizerAnchors[index];

				if (!showHideInProgress)
				{
					bool hidden = true;

					foreach (var camera in followerCameras)
						if (camera.PointInFrontOfViewport(anchor.position))
						{
							hidden = false;

							break;
						}

					if (hidden)
						continue;
				}

				float pointTargetVelocity = spacedPoints[index].spacedCurvePoint.curvePoint.TargetVelocity(vehicleFriction, gravity) * visualizerSpeedMultiplier;
				float heat = Utility.InverseLerp(pointTargetVelocity, pointTargetVelocity + 15f, vehicleVelocity);

				Color32 color = Settings.pathVisualizerHeatColorGradient.Evaluate(heat);

				color.a = (byte)math.round(color.a * anchor.opacity);

				if (GetPreviousSpacedPointIndex(index, out int previousIndex))
				{
					Color32 previousColorA = colors[LoopIndex(previousIndex * 4 + 2, anchorPointsCount * 4)];
					Color32 previousColorB = colors[LoopIndex(previousIndex * 4 + 3, anchorPointsCount * 4)];

					colors[index * 4 + 0] = previousColorA;
					colors[index * 4 + 1] = previousColorB;
				}
				else
				{
					colors[index * 4 + 0] = color;
					colors[index * 4 + 1] = color;
				}

				colors[index * 4 + 2] = color;
				colors[index * 4 + 3] = color;
				anchor.heat = heat;
			}

			visualizerFilter.mesh.colors32 = colors;
#endif
		}
		public void ShowPathVisualizer()
		{
#if !MVC_COMMUNITY
			ShowHidePathVisualizer(true);
#endif
		}
		public void HidePathVisualizer()
		{
#if !MVC_COMMUNITY
			ShowHidePathVisualizer(false);
#endif
		}

#if !MVC_COMMUNITY
		private void ShowHidePathVisualizer(bool show)
		{
			if (showHideCoroutine != null)
				StopCoroutine(showHideCoroutine);

			showHideCoroutine = StartCoroutine(ShowHidePathVisualizerCoroutine(show, visualizerShowHidePointDuration));
			isShowingVisualizer = show;
		}
		private IEnumerator ShowHidePathVisualizerCoroutine(bool show, float duration)
		{
			showHideInProgress = true;

			bool immediate = duration < 0f || Mathf.Approximately(duration, 0f);
			float targetOpacity = Utility.BoolToNumber(show);
			float timeStep = immediate ? default : Time.deltaTime / duration;
			Vehicle playerVehicle = Manager.PlayerVehicle;
			int startIndex = playerVehicle ? ClosestSpacedPointIndex(playerVehicle.transform.position) : default;
			int visualizerAnchorsCount = visualizerAnchors.Length;

			for (int i = startIndex; i < visualizerAnchorsCount + startIndex; i++)
			{
				int index = LoopIndex(i, visualizerAnchorsCount);

				while (visualizerAnchors[index].opacity != targetOpacity)
				{
					visualizerAnchors[index].opacity = immediate ? targetOpacity : Mathf.MoveTowards(visualizerAnchors[index].opacity, targetOpacity, timeStep);

					if (!immediate)
						yield return null;
				}
			}

			yield return null;

			showHideInProgress = false;
		}
		private ArgumentOutOfRangeException GetArgumentOutOfRangeException(string name, object value, int count)
		{
			return new(name, value, $"`{name}` our of range{(count > 0 ? $" [0, {count - 1}]" : "")}");
		}
		private void RefreshCardinal()
		{
			hermiteCurves = hermiteCurves.ToCardinal(loopedPath, cardinalScale);

			SyncBezierHermiteCurves(CurveType.Hermite);
		}
		private void TryLoadLegacyBezierPath()
		{
			if (!bezier)
				return;

			BezierCurves = bezier.ToBezierCurves(6f);
			loopedPath = bezier.LoopedPath;
			bezier = null;
#if UNITY_EDITOR

			UnityEditor.EditorUtility.SetDirty(this);
#endif
		}
		private void CalculateCurvesLength(out NativeArray<BezierCurve> bezierCurves, out NativeArray<float> curvesLength)
		{
			int curvesCount = CurvesCount;

			bezierCurves = new(curvesCount > 0 ? this.bezierCurves : new BezierCurve[] { }, Allocator.TempJob);
			curvesLength = new(bezierCurves.Length, Allocator.TempJob);

			if (curvesCount < 1)
			{
				this.curvesLength = new float[] { };
				this.totalLength = default;

				return;
			}
			
			NativeArray<float> totalLength = new(1, Allocator.TempJob);

			new CurvesDistanceTablesJob
			{
				bezierCurves = bezierCurves,
				lengthSamples = Settings.AIPathLengthSamples,
				curvesLength = curvesLength,
				totalLength = totalLength,
			}.
			Schedule(curvesCount, curvesCount).Complete();

			this.curvesLength = curvesLength.ToArray();
			this.totalLength = totalLength[0];

			totalLength.Dispose();
		}
		private void CalculateCurvesLength()
		{
			CalculateCurvesLength(out NativeArray<BezierCurve> bezierCurves, out NativeArray<float> curvesLength);

			bezierCurves.Dispose();
			curvesLength.Dispose();
		}
		private bool RemoveCurve(bool firstCurve)
		{
			if (loopedPath)
			{
				ToolkitDebug.Error(new InvalidOperationException($"Curves cannot be removed in a looped path. Use `{nameof(MergeCurves)}` instead."));

				return false;
			}

			int curvesCount = CurvesCount;

			if (curvesCount < 1)
				return false;
			else if (curvesCount < 2)
			{
				ResetPath();

				return true;
			}

			if (firstCurve)
			{
				Array.Copy(bezierCurves, 1, bezierCurves, 0, curvesCount - 1);
				Array.Copy(hermiteCurves, 1, hermiteCurves, 0, curvesCount - 1);
			}

			Array.Resize(ref bezierCurves, curvesCount - 1);
			Array.Resize(ref hermiteCurves, curvesCount - 1);

			if (useCardinal)
				RefreshCardinal();

			return true;
		}
#endif

		#endregion

		#endregion

		#region Late Update

#if !MVC_COMMUNITY
		private void LateUpdate()
		{
			if (!Awaken || !Manager.PlayerVehicle || spacedPoints.Length < 1)
				return;

			RefreshPathVisualizer();
		}
#endif

		#endregion

		#region Gizmos & Reset

		public void Reset()
		{
			ResetPath();
		}
		public void OnDrawGizmosSelected()
		{
#if UNITY_EDITOR && !MVC_COMMUNITY
			var settings = Settings;

			if (SpacedPointsCount < 1 || spacedPointsSpacing != settings.AIPathSpacedPointsSpacing || generatedPathLengthSamples != settings.AIPathLengthSamples)
			{
				GenerateSpacedPoints();
				AlignSpacedPointsToGround();

				if (isShowingVisualizer)
				{
					CreatePathVisualizer();
					DrawPathVisualizer();
				}
			}

			int generatedPointsCount = spacedPoints.Length;

			if (generatedPointsCount < 2)
				return;

			Color pointColor = settings.AIPathBezierColor;
			Color meshColor = pointColor;
			Color lineColor = pointColor;

			meshColor *= .25f;
			lineColor *= .5f;

			Vector3[] vertices = new Vector3[(generatedPointsCount - Utility.BoolToNumber(!loopedPath)) * 4];
			int[] triangles = new int [(generatedPointsCount - Utility.BoolToNumber(!loopedPath)) * 6];
			Vector3[] normals = new Vector3[vertices.Length];

			for (int i = 0; i < generatedPointsCount; i++)
			{
				var point = spacedPoints[i];
				Camera gizmosCamera = UnityEditor.SceneView.lastActiveSceneView.camera;

				if (!gizmosCamera)
					gizmosCamera = FindAnyObjectByType<Camera>();

				if (!gizmosCamera || gizmosCamera.PointInFrontOfViewport(point.spacedCurvePoint.curvePoint.position))
				{
					Gizmos.color = pointColor;

					Gizmos.DrawSphere(point.spacedCurvePoint.curvePoint.position, settings.gizmosSize * .2f);
				}

				if (!loopedPath && i + 1 >= generatedPointsCount)
					break;

				var nextPoint = GetSpacedPoint(i + 1);
				var curve = bezierCurves[point.curveIndex];
				var nextCurve = bezierCurves[nextPoint.curveIndex];
				float extent = Utility.LerpUnclamped(curve.StartWidth, curve.EndWidth, point.spacedCurvePoint.t) * .5f;
				float nextExtent = Utility.LerpUnclamped(nextCurve.StartWidth, nextCurve.EndWidth, nextPoint.spacedCurvePoint.t) * .5f;
				float3 normal = point.spacedCurvePoint.curvePoint.up;
				float3 nextNormal = point.spacedCurvePoint.curvePoint.up;
				float3 point1 = point.spacedCurvePoint.curvePoint.position - extent * point.spacedCurvePoint.curvePoint.right + .05f * normal;
				float3 point2 = nextPoint.spacedCurvePoint.curvePoint.position - nextExtent * nextPoint.spacedCurvePoint.curvePoint.right + .05f * nextNormal;
				float3 point3 = nextPoint.spacedCurvePoint.curvePoint.position + nextExtent * nextPoint.spacedCurvePoint.curvePoint.right + .05f * nextNormal;
				float3 point4 = point.spacedCurvePoint.curvePoint.position + extent * point.spacedCurvePoint.curvePoint.right + .05f * normal;

				if (gizmosCamera && !gizmosCamera.AnyPointInFrontOfViewport(point1, point2, point3, point4))
					continue;

				Gizmos.color = lineColor;

				Gizmos.DrawLine(point1, point2);
				Gizmos.DrawLine(point4, point3);

				vertices[i * 4]		= point1;
				vertices[i * 4 + 1] = point2;
				vertices[i * 4 + 2] = point3;
				vertices[i * 4 + 3] = point4;

				triangles[i * 6]	 = i * 4 + 2;
				triangles[i * 6 + 1] = i * 4 + 1;
				triangles[i * 6 + 2] = i * 4;
				triangles[i * 6 + 3] = i * 4 + 3;
				triangles[i * 6 + 4] = i * 4 + 2;
				triangles[i * 6 + 5] = i * 4;

				normals[i * 4]		= normal;
				normals[i * 4 + 1]	= nextNormal;
				normals[i * 4 + 2]	= nextNormal;
				normals[i * 4 + 3]	= normal;
			}

			Gizmos.color = meshColor;

			Mesh mesh = new()
			{
				vertices = vertices,
				triangles = triangles,
				normals = normals
			};

			mesh.RecalculateTangents();
			mesh.RecalculateBounds();

			Gizmos.DrawMesh(mesh);
#endif
		}

		#endregion

		#endregion
	}
}

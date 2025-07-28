#region Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using Utilities;
using MVC.Core;
using MVC.Base;

using Object = UnityEngine.Object;
using System.Text.RegularExpressions;
using System.Drawing;
using UnityEngine.UIElements;



#endregion

namespace MVC.Utilities
{
	#region Extensions

	public static class VehicleAIExtensions
	{
		public static BezierCurve[] ToBezierCurves(this Bezier.Path path, float width)
		{
			int segmentsCount = path.SegmentsCount;
			BezierCurve[] bezierCurves = new BezierCurve[segmentsCount];

			for (int i = 0; i < segmentsCount; i++)
			{
				Vector3[] segmentPoints = path.GetSegmentPoints(i);

				bezierCurves[i] = new(segmentPoints[0], segmentPoints[1], segmentPoints[2], segmentPoints[3], width);
			}

			return bezierCurves;
		}
		public static bool Equals(this NavMeshBuildSettings settings, NavMeshBuildSettings other, bool useExtension)
		{
			return useExtension ? settings.agentRadius == other.agentRadius && settings.agentSlope == other.agentSlope &&
				settings.agentClimb == other.agentClimb && settings.agentHeight == other.agentHeight : settings.Equals(other);
		}
		public static bool IsEmpty(this NavMeshBuildSettings settings)
		{
			return settings.Equals(default, true);
		}
		public static BezierCurve ToBezier(this HermiteCurve hermite)
		{
			float3 p0 = hermite.P0;
			float3 p1 = p0 + hermite.V0 / 3f;
			float3 p3 = hermite.P1;
			float3 p2 = p3 - hermite.V1 / 3f;

			return new(p0, p1, p2, p3, hermite.StartWidth, hermite.EndWidth);
		}
		public static HermiteCurve ToHermite(this BezierCurve bezier)
		{
			float3 p0 = bezier.P0;
			float3 v0 = 3f * (bezier.P1 - p0);
			float3 p1 = bezier.P3;
			float3 v1 = 3f * (p1 - bezier.P2);

			return new(p0, v0, p1, v1, bezier.StartWidth, bezier.EndWidth);
		}
		public static void ToLinear(this HermiteCurve hermite, out LinearLine a, out LinearLine b, out LinearLine c)
		{
			hermite.ToBezier().ToLinear(out a, out b, out c);
		}
		public static void ToLinear(this BezierCurve bezier, out LinearLine a, out LinearLine b, out LinearLine c)
		{
			float3 p0 = bezier.P0, p1 = bezier.P1, p2 = bezier.P2, p3 = bezier.P3;
			float startWidth = bezier.StartWidth;
			float endWidth = bezier.EndWidth;
			float aEndWidth = Utility.LerpUnclamped(startWidth, endWidth, 1f / 3f);
			float bEndWidth = Utility.LerpUnclamped(startWidth, endWidth, 2f / 3f);

			a = new(p0, p1, startWidth, aEndWidth);
			b = new(p1, p2, aEndWidth, bEndWidth);
			c = new(p2, p3, bEndWidth, endWidth);
		}
		public static float[] DistanceTable<T>(this T curve, int samples) where T : ICurve
		{
			var distanceTable = DistanceTable(curve, samples, Allocator.Temp);
			var result = distanceTable.ToArray();

			distanceTable.Dispose();

			return result;
		}
		public static NativeArray<float> DistanceTable<T>(this T curve, int samples, Allocator allocator) where T : ICurve
		{
			NativeArray<float> distanceTable = new(samples, allocator);
			float distance = default;

			for (int i = 0; i < samples; i++)
			{
				distance += math.distance(curve.Evaluate((float)i / samples), curve.Evaluate((i + 1f) / samples));
				distanceTable[i] = distance;
			}

			return distanceTable;
		}
		public static void GetSpacedPoints<T>(this T curve, float approximateSpace, NativeArray<float> distanceTable, NativeList<SpacedCurvePoint> spacedPointsList, ref float distance) where T : ICurve
		{
			float curveLength = distanceTable[^1];

			while (distance < curveLength)
			{
				float t = ICurve.InverseDistance(distance, distanceTable);

				spacedPointsList.Add(new() { curvePoint = curve.GetPoint(t), t = t });

				distance += approximateSpace;
			}

			distance -= curveLength;
		}
		public static void GetSpacedPoints<T>(this T curve, float approximateSpace, float[] distanceTable, ref List<SpacedCurvePoint> spacedPointsList, ref float distance) where T : ICurve
		{
			var tempSpacedPointsList = spacedPointsList.ToNativeList(Allocator.Temp);
			NativeArray<float> tempDistanceTable = new(distanceTable, Allocator.Temp);

			GetSpacedPoints(curve, approximateSpace, tempDistanceTable, tempSpacedPointsList, ref distance);

			tempSpacedPointsList.Dispose();
			tempDistanceTable.Dispose();
		}
		public static void GetSpacedPoints<T>(this T curve, float approximateSpace, NativeArray<float> distanceTable, NativeList<SpacedCurvePoint> spacedPointsList, bool addLastPoint) where T : ICurve
		{
			float distance = 0;

			GetSpacedPoints(curve, approximateSpace, distanceTable, spacedPointsList, ref distance);

			if (addLastPoint)
				spacedPointsList.Add(new() { curvePoint = curve.GetPoint(1f), t = 1f });
		}
		public static void GetSpacedPoints<T>(this T curve, float approximateSpace, float[] distanceTable, ref List<SpacedCurvePoint> spacedPointsList, bool addLastPoint) where T : ICurve
		{
			float distance = 0;

			GetSpacedPoints(curve, approximateSpace, distanceTable, ref spacedPointsList, ref distance);

			if (addLastPoint)
				spacedPointsList.Add(new() { curvePoint = curve.GetPoint(1f), t = 1f });
		}
		public static float Length<T>(this T curve, int samples) where T : ICurve
		{
			var distanceTable = curve.DistanceTable(samples, Allocator.Temp);
			float length = distanceTable[^1];

			distanceTable.Dispose();

			return length;
		}
		public static float3 Tangent<T>(this T curve, float t) where T : ICurve
		{
			return math.normalize(curve.Velocity(t));
		}
		public static float3 Normal<T>(this T curve, float t, float3 up) where T : ICurve
		{
			return math.cross(math.normalize(curve.Velocity(t)), up);
		}
		public static float Kappa<T>(this T curve, float t) where T : ICurve
		{
			float3 velocity = curve.Velocity(t);
			float speed = math.length(velocity);

			if (Mathf.Approximately(speed, 0f))
				return math.INFINITY;

			float3 acceleration = curve.Acceleration(t);
			float speed3 = speed * speed * speed;

			return Determinant(velocity, acceleration) / speed3;
		}
		public static float Radius<T>(this T curve, float t) where T : ICurve
		{
			float kappa = curve.Kappa(t);

			if (Mathf.Approximately(kappa, 0f))
				return math.INFINITY;

			return 1f / kappa;
		}

		public static NativeArray<HermiteCurve> ToCardinal(this NativeArray<LinearLine> lines, Allocator allocator, bool loopCurves, float scale = .5f, float3 startCardinalPoint = default, float3 endCardinalPoint = default)
		{
			if (startCardinalPoint.Equals(default))
				startCardinalPoint = loopCurves ? lines[^1].A : lines[0].BMirror;

			if (endCardinalPoint.Equals(default))
				endCardinalPoint = loopCurves ? lines[0].B : lines[^1].AMirror;

			NativeArray<HermiteCurve> cardinal = new(lines.Length, allocator);

			Cardinal(cardinal, scale, startCardinalPoint, endCardinalPoint);

			return cardinal;
		}
		public static void ToCardinal(this LinearLine[] lines, out HermiteCurve[] cardinal, bool loopCurves, float scale, float3 startCardinalPoint = default, float3 endCardinalPoint = default)
		{
			NativeArray<LinearLine> linesTemp = new(lines, Allocator.Temp);
			NativeArray<HermiteCurve> cardinalTemp = linesTemp.ToCardinal(Allocator.Temp, loopCurves, scale, startCardinalPoint, endCardinalPoint);

			cardinal = cardinalTemp.ToArray();

			cardinalTemp.Dispose();
			linesTemp.Dispose();
		}
		public static void ToCardinal(this NativeArray<HermiteCurve> cardinal, bool loopCurves, float scale = .5f, float3 startCardinalPoint = default, float3 endCardinalPoint = default)
		{
			if (startCardinalPoint.Equals(default))
				startCardinalPoint = loopCurves ? cardinal[^1].P0 : cardinal[0].P1Mirror;

			if (endCardinalPoint.Equals(default))
				endCardinalPoint = loopCurves ? cardinal[0].P1 : cardinal[^1].P0Mirror;

			Cardinal(cardinal, scale, startCardinalPoint, endCardinalPoint);
		}
		public static HermiteCurve[] ToCardinal(this HermiteCurve[] cardinal, bool loopCurves, float scale = .5f, float3 startCardinalPoint = default, float3 endCardinalPoint = default)
		{
			NativeArray<HermiteCurve> cardinalTemp = new(cardinal, Allocator.Temp);

			ToCardinal(cardinalTemp, loopCurves, scale, startCardinalPoint, endCardinalPoint);

			cardinal = cardinalTemp.ToArray();

			cardinalTemp.Dispose();

			return cardinal;
		}

		private static void Cardinal(NativeArray<HermiteCurve> cardinal, float scale, float3 startCardinalPoint, float3 endCardinalPoint)
		{
			scale = Utility.Clamp01(scale);

			for (int i = 0; i < cardinal.Length; i++)
			{
				var curve = cardinal[i];
				float3 v0Start = i > 0 ? cardinal[i - 1].P0 : startCardinalPoint;
				float3 v0End = curve.P1;
				float3 v1Start = curve.P0;
				float3 v1End = i + 1 < cardinal.Length ? cardinal[i + 1].P1 : endCardinalPoint;

				cardinal[i] = new(curve.P0, (v0End - v0Start) * scale, curve.P1, (v1End - v1Start) * scale, curve.StartWidth, curve.EndWidth);
			}
		}
		private static float Determinant(float3 a, float3 b)
		{
			return (a.y * b.z - a.z * b.y) + (a.z * b.x - a.x * b.z) + (a.x * b.y - a.y * b.x);
		}
	}
	public static class VehicleAudio
	{
		#region Modules

		[Serializable]
		public struct EngineMixersGroup
		{
			public AudioMixerGroup engine;
			public AudioMixerGroup exhaust;
		}
		[Serializable]
		public struct MixersGroup
		{
			public AudioMixerGroup chargersEffects;
			public AudioMixerGroup exhaustEffects;
			public AudioMixerGroup transmission;
			[FormerlySerializedAs("brakesEffects")]
			public AudioMixerGroup brakeEffects;
			public AudioMixerGroup supercharger;
			public AudioMixerGroup turbocharger;
			public AudioMixerGroup wheelEffects;
			public AudioMixerGroup NOS;
			public AudioMixerGroup environmentalEffects;
		}

		#endregion

		#region Methods

		public static VehicleAudioSource[] NewAudioSources(AudioClip[] clips, ToolkitBehaviour parent, string name, float minDistance, float maxDistance, float volume, bool loop, bool playOnAwake, bool disableOnAwake, float playDelay = default, AudioMixerGroup mixerGroup = null)
		{
			return NewAudioSources(clips, parent, Vector3.zero, name, minDistance, maxDistance, volume, loop, playOnAwake, disableOnAwake, playDelay, mixerGroup);
		}
		public static VehicleAudioSource[] NewAudioSources(AudioClip[] clips, ToolkitBehaviour parent, Vector3 localPosition, string name, float minDistance, float maxDistance, float volume, bool loop, bool playOnAwake, bool disableOnAwake, float playDelay = default, AudioMixerGroup mixerGroup = null)
		{
			if (clips == null || !ToolkitBehaviour.Settings)
				return null;

			VehicleAudioSource[] sources = new VehicleAudioSource[clips.Length];

			for (int i = 0; i < sources.Length; i++)
			{
				if (!clips[i])
					continue;

				sources[i] = NewAudioSource(parent, localPosition, $"{name}{ToolkitBehaviour.Settings.engineSFXNameSplitter}{i}", minDistance, maxDistance, volume, clips[i], loop, playOnAwake, disableOnAwake, playDelay, mixerGroup);
			}

			return sources;
		}
		public static VehicleAudioSource NewAudioSource(ToolkitBehaviour parent, string name, float minDistance, float maxDistance, float volume, AudioClip clip, bool loop, bool playOnAwake, bool disableOnAwake, float playDelay = default, AudioMixerGroup mixerGroup = null)
		{
			return NewAudioSource(parent, Vector3.zero, name, minDistance, maxDistance, volume, clip, loop, playOnAwake, disableOnAwake, playDelay, mixerGroup);
		}
		public static VehicleAudioSource NewAudioSource(ToolkitBehaviour parent, Vector3 localPosition, string name, float minDistance, float maxDistance, float volume, AudioClip clip, bool loop, bool playOnAwake, bool disableOnAwake, float playDelay = default, AudioMixerGroup mixerGroup = null)
		{
			if (playOnAwake && !clip)
				return null;

			if (parent is Vehicle)
				parent = parent.GetComponentInChildren<VehicleChassis>();

			if (!parent)
				return null;

			VehicleAudioSource vehicleSource = new GameObject(name).AddComponent<VehicleAudioSource>();

			if (!vehicleSource.GetComponent<AudioSource>())
				vehicleSource.source = vehicleSource.gameObject.AddComponent<AudioSource>();

			vehicleSource.source.minDistance = minDistance;
			vehicleSource.source.maxDistance = maxDistance;
			vehicleSource.source.volume = volume;
			vehicleSource.source.clip = clip;
			vehicleSource.source.loop = loop;
			vehicleSource.source.outputAudioMixerGroup = mixerGroup;
			vehicleSource.source.spatialBlend = minDistance == 0f && maxDistance == 0f ? 0f : 1f;
			vehicleSource.source.playOnAwake = playOnAwake;
			vehicleSource.source.enabled = !disableOnAwake;

			Transform newParent = parent.transform.Find("SoundEffects");

			if (!newParent)
			{
				newParent = new GameObject("SoundEffects").transform;

				newParent.SetParent(parent.transform, false);
			}

			if (ToolkitBehaviour.Settings.useHideFlags)
				newParent.hideFlags = HideFlags.HideInHierarchy;

			vehicleSource.transform.SetParent(newParent);
			vehicleSource.transform.SetLocalPositionAndRotation(localPosition, Quaternion.identity);

			if (clip)
				clip.LoadAudioData();

			if (playOnAwake)
			{
				if (playDelay > 0f)
					vehicleSource.source.PlayDelayed(playDelay);
				else
					vehicleSource.source.Play();
			}

			return vehicleSource;
		}
		public static IEnumerator DestroyAudioSource(AudioSource source)
		{
			yield return new WaitWhile(() => source && source.isPlaying);

			if (source)
				Utility.Destroy(false, source.gameObject);
		}

		#endregion
	}
	public static class VehicleVisuals
	{
		#region Modules

		public struct ContactPointAccess
		{
			#region Variables

			public float3 point;
			public float3 normal;

			#endregion

			#region Constructors

			public ContactPointAccess(ContactPoint contact)
			{
				point = contact.point;
				normal = contact.normal;
			}
			public ContactPointAccess(ContactPointAccess contact)
			{
				point = contact.point;
				normal = contact.normal;
			}

			#endregion

			#region Operators

			public static implicit operator ContactPointAccess(ContactPoint contact) => new(contact);

			#endregion
		}

		#endregion

		#region Methods

		public static ParticleSystem NewParticleSystem(ToolkitBehaviour parent, ParticleSystem particle, string name, Vector3 position, Quaternion rotation, bool loop, bool playOnAwake, bool overrideChildren)
		{
			if (ToolkitBehaviour.HasInternalErrors || !ToolkitBehaviour.Settings.useParticleSystems)
				return null;

			if (parent is Vehicle)
			{
				parent = parent.GetComponentInChildren<VehicleChassis>();

				if (!parent)
					return null;
			}

			Transform newParent = parent.transform.Find("VisualEffects");

			if (!newParent)
			{
				newParent = new GameObject("VisualEffects").transform;

				newParent.SetParent(parent.transform, false);
			}

			if (ToolkitBehaviour.Settings.useHideFlags)
				newParent.hideFlags = HideFlags.HideInHierarchy;

			return NewParticleSystem(newParent, particle, name, position, rotation, loop, playOnAwake, overrideChildren);
		}
		public static ParticleSystem NewParticleSystem(Transform parent, ParticleSystem particle, string name, Vector3 position, Quaternion rotation, bool loop, bool playOnAwake, bool overrideChildren)
		{
			if (ToolkitBehaviour.HasInternalErrors || !ToolkitBehaviour.Settings.useParticleSystems)
				return null;

			if (parent)
				particle = Object.Instantiate(particle.gameObject, parent).GetComponent<ParticleSystem>();
			else
				particle = Object.Instantiate(particle.gameObject).GetComponent<ParticleSystem>();

			particle.name = name;

			particle.transform.SetPositionAndRotation(position, rotation);

			if (overrideChildren)
			{
				ParticleSystem[] subParticles = particle.GetComponentsInChildren<ParticleSystem>();

				for (int i = 0; i < subParticles.Length; i++)
				{
					ParticleSystem.MainModule main = subParticles[i].main;

					main.playOnAwake = playOnAwake;
					main.loop = loop;
				}
			}
			else
			{
				ParticleSystem.MainModule main = particle.main;

				main.playOnAwake = playOnAwake;
				main.loop = loop;
			}

			if (!playOnAwake && particle.isPlaying)
				particle.Stop(overrideChildren, ParticleSystemStopBehavior.StopEmittingAndClear);

			if (playOnAwake)
				particle.Play(overrideChildren);

			return particle;
		}
		public static TrailRenderer NewTrailRenderer(ToolkitBehaviour parent, TrailRenderer trail, string name, Vector3 position, Quaternion rotation)
		{
			if (ToolkitBehaviour.HasInternalErrors || !ToolkitBehaviour.Settings.useWheelMarks)
				return null;

			if (parent is Vehicle)
				parent = parent.GetComponentInChildren<VehicleChassis>();

			if (!parent)
				return null;

			Transform newParent = parent.transform.Find("VisualEffects");

			if (!newParent)
			{
				newParent = new GameObject("VisualEffects").transform;

				newParent.SetParent(parent.transform, false);
			}

			return NewTrailRenderer(newParent, trail, name, position, rotation);
		}
		public static TrailRenderer NewTrailRenderer(Transform parent, TrailRenderer trail, string name, Vector3 position, Quaternion rotation)
		{
			if (ToolkitBehaviour.HasInternalErrors || !ToolkitBehaviour.Settings.useWheelMarks)
				return null;

			if (parent)
				trail = Object.Instantiate(trail.gameObject, parent).GetComponent<TrailRenderer>();
			else
				trail = Object.Instantiate(trail.gameObject).GetComponent<TrailRenderer>();

			trail.name = name;

			trail.transform.SetPositionAndRotation(position, rotation);

			return trail;
		}
		public static ParticleSystemForceField NewParticleSystemForceField(ToolkitBehaviour parent, ParticleSystemForceField forceField, string name, Vector3 position, Quaternion rotation)
		{
			if (ToolkitBehaviour.HasInternalErrors || !ToolkitBehaviour.Settings.useParticleSystems)
				return null;

			if (parent is Vehicle)
				parent = parent.GetComponentInChildren<VehicleChassis>();

			if (!parent)
				return null;

			Transform newParent = parent.transform.Find("VisualEffects");

			if (!newParent)
			{
				newParent = new GameObject("VisualEffects").transform;

				newParent.SetParent(parent.transform, false);
			}

			return NewParticleSystemForceField(newParent, forceField, name, position, rotation);
		}
		public static ParticleSystemForceField NewParticleSystemForceField(Transform parent, ParticleSystemForceField forceField, string name, Vector3 position, Quaternion rotation)
		{
			if (ToolkitBehaviour.HasInternalErrors || !ToolkitBehaviour.Settings.useParticleSystems)
				return null;

			if (parent)
				forceField = Object.Instantiate(forceField.gameObject, parent).GetComponent<ParticleSystemForceField>();
			else
				forceField = Object.Instantiate(forceField.gameObject).GetComponent<ParticleSystemForceField>();

			forceField.name = name;

			forceField.transform.SetPositionAndRotation(position, rotation);

			return forceField;
		}
		public static IEnumerator DestroyParticleSystem(ParticleSystem particle)
		{
			if (particle && particle.particleCount < 1)
				yield return new WaitUntil(() => particle && particle.particleCount > 0);

			yield return DestroyParticleSystem(particle, particle.main.duration);
		}
		public static IEnumerator DestroyParticleSystem(ParticleSystem particle, float afterTime)
		{
			if (particle && particle.isPlaying)
			{
				particle.Stop(true);
				particle.Stop();
			}

			yield return new WaitForSeconds(afterTime);
			yield return new WaitWhile(() => particle && !particle.isPlaying && particle.particleCount > 0);

			if (particle)
				Utility.Destroy(false, particle.gameObject);
		}
		public static IEnumerator DestroyParticleSystemForceField(ParticleSystemForceField forceField, WaitWhile waitWhile)
		{
			yield return waitWhile;

			if (forceField)
				Utility.Destroy(false, forceField.gameObject);
		}

		#endregion
	}
	public static class VehicleUtilityExtensions
	{
		public static string SpacePascalCase(this string str)
		{
			return Regex.Replace(str, "[a-z][A-Z]", match => $"{match.Value[0]} {match.Value[1]}");
		}
		public static bool PointInViewport(this Camera camera, Vector3 point)
		{
			Vector3 viewportMono = camera.WorldToViewportPoint(point, Camera.MonoOrStereoscopicEye.Mono);
			Vector3 viewportLeft = camera.WorldToViewportPoint(point, Camera.MonoOrStereoscopicEye.Mono);
			Vector3 viewportRight = camera.WorldToViewportPoint(point, Camera.MonoOrStereoscopicEye.Mono);
			bool inCameraFrustum = viewportMono.x >= 0 && viewportMono.x <= 1 && viewportMono.y >= 0 && viewportMono.y <= 1 ||
				viewportLeft.x >= 0 && viewportLeft.x <= 1 && viewportLeft.y >= 0 && viewportLeft.y <= 1 ||
				viewportRight.x >= 0 && viewportRight.x <= 1 && viewportRight.y >= 0 && viewportRight.y <= 1;
			bool inFrontOfCamera = viewportMono.z > 0 || viewportLeft.z > 0 || viewportRight.z > 0;

			return inCameraFrustum && inFrontOfCamera;
		}
		public static bool PointInFrontOfViewport(this Camera camera, Vector3 point)
		{
			Vector3 viewport = camera.WorldToViewportPoint(point);
			bool inFrontOfCamera = viewport.z > 0;

			return inFrontOfCamera;
		}
		public static bool PointBehindViewport(this Camera camera, Vector3 point)
		{
			return !camera.PointInFrontOfViewport(point);
		}
		public static bool AllPointsInViewport(this Camera camera, params Vector3[] points)
		{
			if (points.Length < 1)
				return false;

			bool result = true;

			foreach (var point in points)
				result &= camera.PointInViewport(point);

			return result;
		}
		public static bool AnyPointInViewport(this Camera camera, params Vector3[] points)
		{
			if (points.Length < 1)
				return false;

			foreach (var point in points)
				if (camera.PointInViewport(point))
					return true;

			return false;
		}
		public static bool AllPointsInFrontOfViewport(this Camera camera, params Vector3[] points)
		{
			if (points.Length < 1)
				return false;

			foreach (var point in points)
				if (camera.PointInFrontOfViewport(point))
					return true;

			return false;
		}
		public static bool AnyPointInFrontOfViewport(this Camera camera, params Vector3[] points)
		{
			if (points.Length < 1)
				return false;

			bool result = true;

			foreach (var point in points)
				result &= camera.PointInFrontOfViewport(point);

			return result;
		}
		public static bool AllPointsBehindViewport(this Camera camera, params Vector3[] points)
		{
			if (points.Length < 1)
				return false;

			foreach (var point in points)
				if (camera.PointBehindViewport(point))
					return true;

			return false;
		}
		public static bool AnyPointBehindViewport(this Camera camera, params Vector3[] points)
		{
			if (points.Length < 1)
				return false;

			bool result = true;

			foreach (var point in points)
				result &= camera.PointBehindViewport(point);

			return result;
		}
		public static float ApproximateEvaluation(this WheelFrictionCurve frictionCurve, float slip)
		{
			return ApproximateEvaluation(frictionCurve.extremumSlip, frictionCurve.extremumValue, frictionCurve.asymptoteSlip, frictionCurve.asymptoteValue, frictionCurve.stiffness, slip);
		}
		public static float ApproximateEvaluation(this VehicleTireCompound.WheelColliderFrictionCurve frictionCurve, float slip)
		{
			return ApproximateEvaluation(frictionCurve.extremumSlip, frictionCurve.extremumValue, frictionCurve.asymptoteSlip, frictionCurve.asymptoteValue, frictionCurve.stiffness, slip);
		}
		public static Utility.Interval ToInterval(this Utility.SimpleInterval simpleInterval, bool overrideBorders, bool clampToZero)
		{
			return new(simpleInterval.a, simpleInterval.b, overrideBorders, clampToZero);
		}
		public static Utility.SimpleInterval ToSimpleInterval(this Utility.Interval interval)
		{
			return new(interval);
		}
		
		private static float ApproximateEvaluation(float extremumSlip, float extremumValue, float asymptoteSlip, float asymptoteValue, float stiffness, float slip)
		{
			float slipSign = math.sign(slip);

			slip = math.abs(slip);

			if (asymptoteSlip < extremumSlip)
				(asymptoteSlip, extremumSlip) = (extremumSlip, asymptoteSlip);

			float value;

			if (slip <= extremumSlip)
				value = extremumValue * slip / extremumSlip;
			else if (slip <= asymptoteSlip)
				value = Utility.LerpUnclamped(extremumValue, asymptoteValue, Utility.InverseLerpUnclamped(extremumSlip, asymptoteSlip, slip));
			else
				value = asymptoteValue;

			return slipSign * value * stiffness;
		}
	}

	#endregion

	#region Interfaces

	public interface ISpline
	{
		#region Constants

		public const int Count = 2;

		#endregion

		#region Properties

		public float StartWidth { get; set; }
		public float EndWidth { get; set; }

		#endregion

		#region Indexers

		public float3 this[int index] { get; set; }

		#endregion

		#region Utilities

		public static float InverseDistance(float distance, float length)
		{
			return distance / length;
		}

		public float3 Evaluate(float t);
		public float3 Velocity(float t);
		public float3 Acceleration(float t);
		public float3 Jolt();
		public Bounds Bounds();
		public CurvePoint GetPoint(float t);
		public float Width(float t);
		public void Offset(float3 offset);

		#endregion
	}
	public interface ICurve : ISpline
	{
		#region Constants

		public new const int Count = 4;

		#endregion

		#region Indexers

		public new float3 this[int index] { get; set; }

		#endregion

		#region Utilities

		public static float InverseDistance(float distance, float[] distanceTable)
		{
			NativeArray<float> tempDistanceTable = new(distanceTable, Allocator.Temp);
			float t = InverseDistance(distance, tempDistanceTable);

			tempDistanceTable.Dispose();

			return t;
		}
		public static float InverseDistance(float distance, NativeArray<float> distanceTable)
		{
			float length = distanceTable[^1];
			int samples = distanceTable.Length;

			if (distance > 0f && distance < length)
				for (int i = 0; i < samples - 1; i++)
				{
					float currentDistance = distanceTable[i];
					float nextDistance = distanceTable[i + 1];

					if (distance >= currentDistance && distance <= nextDistance)
					{
						float currentT = i / (samples - 1f);
						float nextT = (i + 1f) / (samples - 1f);

						return math.lerp(currentT, nextT, math.unlerp(currentDistance, nextDistance, distance));
					}
				}

			return InverseDistance(distance, length);
		}

		#endregion
	}

	#endregion

	#region Modules

	[Serializable]
	public struct BezierCurve : ICurve
	{
		#region Variables

		public float3 P0
		{
			readonly get
			{
				return p0;
			}
			set
			{
				p1 += value - p0;
				p0 = value;

				RefreshCoefficients();
			}
		}
		public float3 P1
		{
			readonly get
			{
				return p1;
			}
			set
			{
				p1 = value;

				RefreshCoefficients();
			}
		}
		public float3 P3
		{
			readonly get
			{
				return p3;
			}
			set
			{
				p2 += value - p3;
				p3 = value;

				RefreshCoefficients();
			}
		}
		public float3 P2
		{
			readonly get
			{
				return p2;
			}
			set
			{
				p2 = value;

				RefreshCoefficients();
			}
		}
		public float StartWidth
		{
			readonly get
			{
				return startWidth;
			}
			set
			{
				startWidth = math.max(value, .1f);
			}
		}
		public float EndWidth
		{
			readonly get
			{
				return endWidth;
			}
			set
			{
				endWidth = math.max(value, .1f);
			}
		}

		[SerializeField]
		private float3 p0, p1, p2, p3;
		[SerializeField]
		private float3 bt0, bt1, bt2, bt3;
		[SerializeField]
		private float3 vt0, vt1, vt2;
		[SerializeField]
		private float3 at0, at1;
		[SerializeField]
		private float3 jt0;
		[SerializeField]
		private float startWidth;
		[SerializeField]
		private float endWidth;

		#endregion

		#region Indexers

		public float3 this[int index]
		{
			readonly get
			{
				return index switch
				{
					0 => p0,
					1 => p1,
					2 => p2,
					3 => p3,
					_ => throw new IndexOutOfRangeException("index is out of range [0, 3]"),
				};
			}
			set
			{
				switch (index)
				{
					case 0:
						p0 = value;

						break;

					case 1:
						p1 = value;

						break;

					case 2:
						p2 = value;

						break;

					case 3:
						p3 = value;

						break;

					default:
						throw new IndexOutOfRangeException("index is out of range [0, 3]");
				};
			}
		}

		#endregion

		#region Utilities

		public readonly float3 Evaluate(float t)
		{
			float t3 = t * t * t;
			float t2 = t * t;

			return bt0 + t * bt1 + t2 * bt2 + t3 * bt3;
		}
		public readonly float3 Velocity(float t)
		{
			float t2 = t * t;

			return	vt0 + t * vt1 + t2 * vt2;
		}
		public readonly float3 Acceleration(float t)
		{
			return  at0 + t * at1;
		}
		public readonly float3 Jolt()
		{
			return jt0;
		}
		public readonly Bounds Bounds()
		{
			Vector3 a = -3f * p0 + 9f * p1 - 9f * p2 + 3f * p3;
			Vector3 b = 6f * p0 - 12f * p1 + 6f * p2;
			Vector3 c = -3f * p0 + 3f * p1;

			Bounds bounds = new(a, Vector3.zero);

			bounds.Encapsulate(b);
			bounds.Encapsulate(c);

			return bounds;
		}
		public readonly CurvePoint GetPoint(float t)
		{
			return new(this, t);
		}
		public readonly float Width(float t)
		{
			return Utility.LerpUnclamped(startWidth, endWidth, t);
		}
		public void Offset(float3 offset)
		{
			p0 += offset;
			p1 += offset;
			p2 += offset;
			p3 += offset;
		}

		#endregion

		#region Methods

		// Calculate polynomial coefficients
		private void RefreshCoefficients()
		{
			// Bezier evaluation coefficients
			bt0 = p0;
			bt1 = -3f * p0 + 3f * p1;
			bt2 = 3f * p0 - 6f * p1 + 3f * p2;
			bt3 = -p0 + 3f * p1 - 3f * p2 + p3;

			// Velocity coefficients (B')
			vt0 = bt1; // or -3f * p0 + 3f * p1
			vt1 = 6f * p0 - 12f * p1 + 6f * p2;
			vt2 = -3f * p0 + 9f * p1 - 9f * p2 + 3f * p3;

			// Acceleration coefficients (B'')
			at0 = vt1; // or 6f * p0 - 12f * p1 + 6f * p2
			at1 = -6f * p0 + 18f * p1 - 18f * p2 + 6f * p3;

			// Jolt/Jerk coefficient
			jt0 = at1; // or -6f * p0 + 18f * p1 + -18f * p2 + 6f * p3
		}

		#endregion

		#region Constructors

		public BezierCurve(float3 p0, float3 p1, float3 p2, float3 p3) : this()
		{
			this.p0 = p0;
			this.p1 = p1;
			this.p2 = p2;
			this.p3 = p3;
			startWidth = 1f;
			endWidth = 1f;

			RefreshCoefficients();
		}
		public BezierCurve(float3 p0, float3 p1, float3 p2, float3 p3, float width) : this(p0, p1, p2, p3, width, width) { }
		public BezierCurve(float3 p0, float3 p1, float3 p2, float3 p3, float startWidth, float endWidth) : this(p0, p1, p2, p3)
		{
			this.startWidth = startWidth;
			this.endWidth = endWidth;
		}

		#endregion
	}
	[Serializable]
	public struct HermiteCurve : ICurve
	{
		#region Variables

		public float3 P0
		{
			readonly get
			{
				return p0;
			}
			set
			{
				p0 = value;

				RefreshCoefficients();
			}
		}
		public readonly float3 P0Mirror
		{
			get
			{
				return 2f * p1 - p0;
			}
		}
		public float3 V0
		{
			readonly get
			{
				return v0;
			}
			set
			{
				v0 = value;

				RefreshCoefficients();
			}
		}
		public float3 P1
		{
			readonly get
			{
				return p1;
			}
			set
			{
				p1 = value;

				RefreshCoefficients();
			}
		}
		public readonly float3 P1Mirror
		{
			get
			{
				return 2f * p0 - p1;
			}
		}
		public float3 V1
		{
			readonly get
			{
				return v1;
			}
			set
			{
				v1 = value;

				RefreshCoefficients();
			}
		}
		public float StartWidth
		{
			readonly get
			{
				return startWidth;
			}
			set
			{
				startWidth = math.max(value, .1f);
			}
		}
		public float EndWidth
		{
			readonly get
			{
				return endWidth;
			}
			set
			{
				endWidth = math.max(value, .1f);
			}
		}

		[SerializeField]
		private float3 p0, v0, p1, v1;
		[SerializeField]
		private float3 ht0, ht1, ht2, ht3;
		[SerializeField]
		private float3 vt0, vt1, vt2;
		[SerializeField]
		private float3 at0, at1;
		[SerializeField]
		private float3 jt0;
		[SerializeField]
		private float startWidth;
		[SerializeField]
		private float endWidth;

		#endregion

		#region Indexers

		public float3 this[int index]
		{
			readonly get
			{
				return index switch
				{
					0 => p0,
					1 => v0,
					2 => p1,
					3 => v1,
					_ => throw new IndexOutOfRangeException("index is out of range [0, 3]"),
				};
			}
			set
			{
				switch (index)
				{
					case 0:
						p0 = value;

						break;

					case 1:
						v0 = value;

						break;

					case 2:
						p1 = value;

						break;

					case 3:
						v1 = value;

						break;

					default:
						throw new IndexOutOfRangeException("index is out of range [0, 3]");
				};
			}
		}

		#endregion

		#region Utilities

		public readonly float3 Evaluate(float t)
		{
			float t3 = t * t * t;
			float t2 = t * t;

			return ht0 + t * ht1 + t2 * ht2 + t3 * ht3;
		}
		public readonly float3 Velocity(float t)
		{
			float t2 = t * t;

			return vt0 + t * vt1 + t2 * vt2;
		}
		public readonly float3 Acceleration(float t)
		{
			return at0 + t * at1;
		}
		public readonly float3 Jolt()
		{
			return jt0;
		}
		public readonly Bounds Bounds()
		{
			float3 q0 = p0 + v0;
			float3 q1 = p1 - v1;
			float3 a = -3f * p0 + q0 - 3f * q1 + 3f * p1;
			float3 b = 6f * p0 - 4f * q0 + 2f * q1;
			float3 c = -3f * p0 + q0;
			Bounds bounds = new(a, Vector3.zero);

			bounds.Encapsulate(b);
			bounds.Encapsulate(c);

			return bounds;
		}
		public readonly CurvePoint GetPoint(float t)
		{
			return new(this, t);
		}
		public readonly float Width(float t)
		{
			return Utility.LerpUnclamped(startWidth, endWidth, t);
		}
		public void Offset(float3 offset)
		{
			p0 += offset;
			p1 += offset;
		}

		#endregion

		#region Methods

		// Calculate polynomial coefficients
		private void RefreshCoefficients()
		{
			// Hermite evaluation coefficients
			ht0 = p0;
			ht1 = v0;
			ht2 = -3f * p0 - 2f * v0 + 3f * p1 - v1;
			ht3 = 2f * p0 + v0 - 2f * p1 + v1;

			// Velocity coefficients
			vt0 = ht1; // or v0
			vt1 = -6f * p0 - 4f * v0 + 6f * p1 - 2f * v1;
			vt2 = 6f * p0 + 3f * v0 - 6f * p1 + 3f * v1;

			// Acceleration coefficients
			at0 = vt1; // or -6f * p0 - 4f * v0 + 6f * p1 - 2f * v1
			at1 = 12f * p0 + 6f * v0 - 12f * p1 + 6f * v1;

			// Jolt/Jerk coefficient
			jt0 = at1; // or 12f * p0 + 6f * v0 - 12f * p1 + 6f * v1
		}

		#endregion

		#region Constructors

		public HermiteCurve(float3 p0, float3 v0, float3 p1, float3 v1) : this()
		{
			this.p0 = p0;
			this.v0 = v0;
			this.p1 = p1;
			this.v1 = v1;
			startWidth = 1f;
			endWidth = 1f;

			RefreshCoefficients();
		}
		public HermiteCurve(float3 p0, float3 v0, float3 p1, float3 v1, float width) : this(p0, v0, p1, v1, width, width) { }
		public HermiteCurve(float3 p0, float3 v0, float3 p1, float3 v1, float startWidth, float endWidth) : this(p0, v0, p1, v1)
		{
			this.startWidth = startWidth;
			this.endWidth = endWidth;
		}

		#endregion
	}
	[Serializable]
	public struct LinearLine : ISpline
	{
		#region Variables

		public float3 A
		{
			readonly get
			{
				return a;
			}
			set
			{
				a = value;

				RefreshCoefficients();
			}
		}
		public readonly float3 AMirror
		{
			get
			{
				return 2f * b - a;
			}
		}
		public float3 B
		{
			readonly get
			{
				return b;
			}
			set
			{
				b = value;

				RefreshCoefficients();
			}
		}
		public readonly float3 BMirror
		{
			get
			{
				return 2f * a - b;
			}
		}
		public float StartWidth
		{
			readonly get
			{
				return startWidth;
			}
			set
			{
				startWidth = math.max(value, .1f);
			}
		}
		public float EndWidth
		{
			readonly get
			{
				return endWidth;
			}
			set
			{
				endWidth = math.max(value, .1f);
			}
		}
		public readonly float Length
		{
			get
			{
				return length;
			}
		}

		[SerializeField]
		private float3 a, b, tangent;
		[SerializeField]
		private float startWidth;
		[SerializeField]
		private float endWidth;
		[SerializeField]
		private float length;

		#endregion

		#region Indexers

		public float3 this[int index]
		{
			readonly get
			{
				return index switch
				{
					0 => a,
					1 => b,
					_ => throw new IndexOutOfRangeException("index is out of range [0, 1]"),
				};
			}
			set
			{
				switch (index)
				{
					case 0:
						a = value;

						break;

					case 1:
						b = value;

						break;

					default:
						throw new IndexOutOfRangeException("index is out of range [0, 1]");
				};
			}
		}

		#endregion

		#region Utilities

		public static BezierCurve ToBezier(LinearLine a, LinearLine b, LinearLine c)
		{
			float3 aA = a.A, aB = a.B, bA = b.A, bB = b.B, cA = c.A, cB = c.B;

			if (!aB.Equals(bA))
				throw new ArgumentException("Linear curves `a` and `b` must be connected", "b");
			else if (!bB.Equals(cA))
				throw new ArgumentException("Linear curves `b` and `c` must be connected", "c");

			return new(aA, aB, cA, cB, a.StartWidth, c.EndWidth);
		}

		public readonly float3 Evaluate(float t)
		{
			return (1f - t) * a + t * b;
		}
		public readonly float3 Velocity(float t)
		{
			return float3.zero;
		}
		public readonly float3 Acceleration(float t)
		{
			return float3.zero;
		}
		public readonly float3 Tangent()
		{
			return tangent;
		}
		public readonly float3 Normal(float3 up)
		{
			return math.cross(tangent, up);
		}
		public readonly float3 Jolt()
		{
			return float3.zero;
		}
		public readonly Bounds Bounds()
		{
			Bounds bounds = new(a, Vector3.zero);

			bounds.Encapsulate(b);

			return bounds;
		}
		public readonly float Kappa()
		{
			return default;
		}
		public readonly float Radius()
		{
			return math.INFINITY;
		}
		public readonly CurvePoint GetPoint(float t)
		{
			return new(this, t);
		}
		public readonly float Width(float t)
		{
			return Utility.LerpUnclamped(startWidth, endWidth, t);
		}
		public void Offset(float3 offset)
		{
			a += offset;
			b += offset;
		}

		#endregion

		#region Methods

		private void RefreshCoefficients()
		{
			tangent = Utility.Direction(a, b);
			length = Utility.Distance(a, b);
		}

		#endregion

		#region Constructors

		public LinearLine(float3 a, float3 b) : this()
		{
			this.a = a;
			this.b = b;

			RefreshCoefficients();
		}
		public LinearLine(float3 a, float3 b, float width) : this(a, b, width, width) { }
		public LinearLine(float3 a, float3 b, float startWidth, float endWidth) : this(a, b)
		{
			this.startWidth = startWidth;
			this.endWidth = endWidth;
		}

		#endregion
	}
	public struct CurvePoint
	{
		#region Variables

		public readonly float3 LeftExtent => position - .5f * width * right;
		public readonly float3 RightExtent => position + .5f * width * right;

		public quaternion rotation;
		public float3 position;
		public float3 forward;
		public float3 right;
		public float3 up;
		public float width;
		public float radius;
		public float curvature;

		#endregion

		#region Utilities

		public static CurvePoint Lerp(CurvePoint a, CurvePoint b, float t)
		{
			return LerpUnclamped(a, b, Utility.Clamp01(t));
		}
		public static CurvePoint LerpUnclamped(CurvePoint a, CurvePoint b, float t)
		{
			return new()
			{
				position = math.lerp(a.position, b.position, t),
				forward = math.lerp(a.forward, b.forward, t),
				right = math.lerp(a.right, b.right, t),
				up = math.lerp(a.up, b.up, t),
				radius = math.lerp(a.radius, b.radius, t),
				curvature = math.lerp(a.curvature, b.curvature, t),
			};
		}

		public readonly float InversePoint(float3 point)
		{
			return Utility.InverseLerp(LeftExtent, RightExtent, point);
		}
		public readonly float3 ClosestPoint(float3 point, float widthMultiplier)
		{
			return math.lerp(math.lerp(position, LeftExtent, widthMultiplier), math.lerp(position, RightExtent, widthMultiplier), InversePoint(point));
		}
		public readonly float TargetVelocity(float friction, float gravity)
		{
			return math.sqrt(friction * gravity / math.abs(curvature));
		}

		#endregion

		#region Methods

		public readonly override bool Equals(object obj)
		{
			return obj is CurvePoint point &&
				   rotation.Equals(point.rotation) &&
				   position.Equals(point.position) &&
				   right.Equals(point.right) &&
				   up.Equals(point.up) &&
				   width == point.width &&
				   radius == point.radius &&
				   curvature == point.curvature;
		}
		public readonly override int GetHashCode()
		{
			return HashCode.Combine(rotation, position, forward, right, up, width, radius, curvature);
		}

		#endregion

		#region Operators

		public static bool operator ==(CurvePoint a, CurvePoint b) => a.Equals(b);
		public static bool operator !=(CurvePoint a, CurvePoint b) => !(a == b);

		#endregion

		#region Constructors

		public CurvePoint(LinearLine line, float t)
		{
			up = Utility.Float3Up;
			position = line.Evaluate(t);
			forward = line.Tangent();
			right = line.Normal(up);
			radius = line.Radius();
			curvature = line.Kappa();
			width = line.Width(t);
			rotation = quaternion.LookRotationSafe(forward, up);
		}
		public CurvePoint(BezierCurve curve, float t)
		{
			up = Utility.Float3Up;
			position = curve.Evaluate(t);
			forward = curve.Tangent(t);
			right = curve.Normal(t, up);
			radius = curve.Radius(t);
			curvature = curve.Kappa(t);
			width = curve.Width(t);
			rotation = quaternion.LookRotationSafe(forward, up);
		}
		public CurvePoint(HermiteCurve curve, float t)
		{
			up = Utility.Float3Up;
			position = curve.Evaluate(t);
			forward = curve.Tangent(t);
			right = curve.Normal(t, up);
			radius = curve.Radius(t);
			curvature = curve.Kappa(t);
			width = curve.Width(t);
			rotation = quaternion.LookRotationSafe(forward, up);
		}

		#endregion
	}
	public struct SpacedCurvePoint
	{
		#region Variables

		public CurvePoint curvePoint;
		public float t;

		#endregion

		#region Methods

		public readonly override bool Equals(object obj)
		{
			return obj is SpacedCurvePoint point &&
				   curvePoint == point.curvePoint &&
				   t == point.t;
		}
		public readonly override int GetHashCode()
		{
			return HashCode.Combine(curvePoint, t);
		}

		#endregion

		#region Operators

		public static bool operator ==(SpacedCurvePoint a, SpacedCurvePoint b) => a.Equals(b);
		public static bool operator !=(SpacedCurvePoint a, SpacedCurvePoint b) => !(a == b);

		#endregion

		#region Constructors

		public SpacedCurvePoint(CurvePoint curvePoint, float time)
		{
			this.curvePoint = curvePoint;
			t = time;
		}

		#endregion
	}

	#endregion
}

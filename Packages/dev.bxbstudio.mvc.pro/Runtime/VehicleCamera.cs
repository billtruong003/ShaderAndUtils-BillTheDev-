#region Namespaces

using System;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;
using Utilities.Inputs;

#endregion

namespace MVC.Core
{
	[Serializable]
	public class VehicleCamera : ToolkitComponent
	{
		#region Enumerators

		public enum CameraType { Follower, Pivot }
		public enum PivotPositionType { Outside, Inside }
		[Flags]
		public enum FollowerFeature { None, VerticalOrbit, HorizontalOrbit, SpeedPulse = 4, GearPulse = 8, SidewaysSlipPulse = 16, SteeringTilt = 32, NOSFieldOfView = 64, SpeedShaking = 128, OffRoadShaking = 256, NOSShaking = 512, SidewaysSlipShaking = 1024, ForwardSlipShaking = 2048, BrakeSlipShaking = 4096, Showcase = 8192, StuntsShowcase = 16384, ObstaclesDetection = 32768 }
		[Flags]
		public enum PivotFeature { None, VerticalOrbit, HorizontalOrbit, SpeedFieldOfView = 4, SteeringSidePulse = 8, SteeringTilt = 16, NOSFieldOfView = 32, SpeedShaking = 64, OffRoadShaking = 128, NOSShaking = 256, ObstaclesDetection = 512 }
		public enum ShakingType { Normal, Intense }
		[Flags]
		public enum ImpactAxis { None, VerticalY, HorizontalX }

		#endregion

		#region Variables

		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				if (value.IsNullOrEmpty() || value.IsNullOrWhiteSpace())
					value = "New Camera";

				string prefix = default;

				if (Settings && Settings.Cameras != null && Settings.Cameras.Length > 0)
					for (int i = 0; Array.Find(Settings.Cameras, camera => camera != this && camera.Name.ToUpper() == $"{value.ToUpper()}{prefix}"); i++)
						prefix = i > 0 ? $" ({i})" : "";

				name = $"{value}{prefix}";
			}
		}
		public CameraType Type
		{
			get
			{
				return type;
			}
			set
			{
				type = value;
			}
		}
		public PivotPositionType PivotPosition
		{
			get
			{
				if (Type != CameraType.Pivot)
					return PivotPositionType.Outside;

				return pivotPosition;
			}
			set
			{
				if (Type != CameraType.Pivot)
					return;

				pivotPosition = value;
			}
		}
		[Obsolete("Use `followerFeatures` or `pivotFeatures` instead.", true)]
		public int featuresMask;
		[FormerlySerializedAs("featuresMask")]
		public FollowerFeature followerFeatures;
		[FormerlySerializedAs("featuresMask")]
		public PivotFeature pivotFeatures;
		public int3 PivotPoint
		{
			get
			{
				if (Type != CameraType.Follower)
					return int3.zero;

				return pivotPoint;
			}
			set
			{
				if (Type != CameraType.Follower)
					return;

				pivotPoint.x = math.clamp(value.x, -1, 1);
				pivotPoint.y = math.clamp(value.y, -1, 1);
				pivotPoint.z = math.clamp(value.z, -1, 1);
			}
		}
		public ShakingType impactShaking;
		[Obsolete("Use `ImpactAxes` instead.")]
		public ImpactAxis ImpactAxesMask
		{
			get
			{
				return ImpactAxes;
			}
			set
			{
				ImpactAxes = value;
			}
		}
		public ImpactAxis ImpactAxes
		{
			get
			{
				if (Type == CameraType.Pivot)
					return ImpactAxis.None;

				return impactAxes;
			}
			set
			{
				if (Type == CameraType.Pivot)
					return;

				impactAxes = value;
			}
		}
		public Utility.Interval2 orbitInterval = new(-math.INFINITY, math.INFINITY, -90f, 90f);
		public bool invertXOrbit;
		public bool invertYOrbit = true;
		public bool orbitUsingMouseButton = true;
		public MouseButton orbitMouseButton = MouseButton.Right;
		public bool skipDistantOrbit;
		public float OrbitSkipAngle
		{
			get
			{
				return orbitSkipAngle;
			}
			set
			{
				orbitSkipAngle = math.clamp(value, 1f, 180f);
			}
		}
		public float OrbitTimeout
		{
			get
			{
				return orbitTimeout;
			}
			set
			{
				orbitTimeout = math.max(value, 0f);
			}
		}
		public Utility.Interval FieldOfViewInterval
		{
			get
			{
				return fieldOfViewInterval;
			}
			set
			{
				value.Min = math.max(value.Min, 1f);
				value.Max = math.max(value.Max, value.Min);
				value.OverrideBorders = false;
				value.ClampToZero = true;
				fieldOfViewInterval = value;
			}
		}
		public Utility.Interval ShakeSpeedInterval
		{
			get
			{
				return shakeSpeedInterval;
			}
			set
			{
				value.Min = math.max(value.Min, 1f);
				value.Max = math.max(value.Max, value.Min);
				value.OverrideBorders = false;
				value.ClampToZero = true;
				shakeSpeedInterval = value;
			}
		}
		public float TiltAngle
		{
			get
			{
				return tiltAngle;
			}
			set
			{
				tiltAngle = math.clamp(value, 1f, 360f);
			}
		}
		public bool invertTiltAngle;

		[SerializeField]
		private CameraType type;
		[SerializeField]
		private PivotPositionType pivotPosition;
		[SerializeField]
		private string name;
		[SerializeField, FormerlySerializedAs("impactAxesMask")]
		private ImpactAxis impactAxes = (ImpactAxis)(-1);
		[SerializeField]
		private int3 pivotPoint;
		[SerializeField]
		private float orbitSkipAngle = 150f;
		[SerializeField]
		private float orbitTimeout = 10f;
		[SerializeField]
		private Utility.Interval fieldOfViewInterval = new(50f, 60f, true, true);
		[SerializeField]
		private Utility.Interval shakeSpeedInterval = new(150f, 300f, false, true);
		[SerializeField]
		private float tiltAngle = 10f;

		#endregion

		#region Utilities

		public bool HasFollowerFeature(FollowerFeature feature)
		{
			if (Type != CameraType.Follower)
				return false;

			return (followerFeatures & feature) == feature;
		}
		public bool HasPivotFeature(PivotFeature feature)
		{
			if (Type != CameraType.Pivot)
				return false;

			return (pivotFeatures & feature) == feature;
		}
		public bool HasImpactAxis(ImpactAxis axis)
		{
			return (impactAxes & axis) == axis;
		}

		#endregion

		#region Constructors

		public VehicleCamera(string name)
		{
			Name = name;
		}
		public VehicleCamera(VehicleCamera camera)
		{
			Name = camera.Name;
			Type = camera.Type;
			followerFeatures = camera.followerFeatures;
			pivotFeatures = camera.pivotFeatures;
			impactShaking = camera.impactShaking;
			ImpactAxes = camera.ImpactAxes;
			orbitInterval = new(camera.orbitInterval);
			skipDistantOrbit = camera.skipDistantOrbit;
			OrbitSkipAngle = camera.OrbitSkipAngle;
			OrbitTimeout = camera.OrbitTimeout;
			fieldOfViewInterval = new(camera.fieldOfViewInterval);
			shakeSpeedInterval = new(camera.shakeSpeedInterval);
			TiltAngle = camera.TiltAngle;
		}

		#endregion
	}

	internal struct VehicleCameraAccess
	{
		#region Variables

		public VehicleCamera.CameraType type;
		public VehicleCamera.PivotPositionType pivotPosition;
		public VehicleCamera.FollowerFeature followerFeatures;
		public VehicleCamera.PivotFeature pivotFeatures;
		public VehicleCamera.ShakingType impactShaking;
		public VehicleCamera.ImpactAxis impactAxes;
		public int3 pivotPoint;
		public Utility.Interval2 orbitInterval;
		[MarshalAs(UnmanagedType.U1)]
		public bool skipDistantOrbit;
		[MarshalAs(UnmanagedType.U1)]
		public bool invertXOrbit;
		[MarshalAs(UnmanagedType.U1)]
		public bool invertYOrbit;
		[MarshalAs(UnmanagedType.U1)]
		public bool orbitUsingMouseButton;
		public float orbitSkipAngle;
		public float orbitTimeout;
		public MouseButton orbitMouseButton;
		public Utility.Interval fieldOfViewInterval;
		public Utility.Interval shakeSpeedInterval;
		public float tiltAngle;
		[MarshalAs(UnmanagedType.U1)]
		public bool invertTiltAngle;
		[MarshalAs(UnmanagedType.U1)]
		public bool isPivotCamera;

		#endregion

		#region Utilities

		public readonly bool HasFollowerFeature(VehicleCamera.FollowerFeature feature)
		{
			return !isPivotCamera && (followerFeatures & feature) == feature;
		}
		public readonly bool HasPivotFeature(VehicleCamera.PivotFeature feature)
		{
			return isPivotCamera && (pivotFeatures & feature) == feature;
		}
		public readonly bool HasImpactAxis(VehicleCamera.ImpactAxis axis)
		{
			return (impactAxes & axis) == axis;
		}

		#endregion

		#region Constructors

		public VehicleCameraAccess(VehicleCamera camera) : this()
		{
			if (!camera)
				return;

			type = camera.Type;
			pivotPosition = camera.PivotPosition;
			followerFeatures = camera.followerFeatures;
			pivotFeatures = camera.pivotFeatures;
			impactShaking = camera.impactShaking;
			impactAxes = camera.ImpactAxes;
			pivotPoint = camera.PivotPoint;
			orbitInterval = camera.orbitInterval;
			skipDistantOrbit = camera.skipDistantOrbit;
			invertXOrbit = camera.invertXOrbit;
			invertYOrbit = camera.invertYOrbit;
			orbitUsingMouseButton = camera.orbitUsingMouseButton;
			orbitSkipAngle = camera.OrbitSkipAngle;
			orbitTimeout = camera.OrbitTimeout;
			orbitMouseButton = camera.orbitMouseButton;
			fieldOfViewInterval = camera.FieldOfViewInterval;
			shakeSpeedInterval = camera.ShakeSpeedInterval;
			tiltAngle = camera.TiltAngle;
			invertTiltAngle = camera.invertTiltAngle;
			isPivotCamera = type == VehicleCamera.CameraType.Pivot;
		}

		#endregion

		#region Operators

		public static implicit operator VehicleCameraAccess(VehicleCamera camera) => new(camera);

		#endregion
	}
}

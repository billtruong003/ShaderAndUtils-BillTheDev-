#region Namespaces

using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;
using MVC.Base;
using MVC.Utilities.Internal;

#endregion

namespace MVC.Core
{
	#region Enumerators

	[Flags]
	public enum VehicleChassisWingBehaviour { None, Speed, Brake, Deceleration = 4, Steer = 8 }
	public enum VehicleEngineChassisTorque { Off, Auto, AlwaysOn }

	#endregion

	#region Modules

	[AddComponentMenu("")]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(-70)]
	public class VehicleChassis : ToolkitBehaviour
	{
		#region Modules

		[Serializable]
		public class WingModule
		{
			#region Enumerators

			public enum Interpolation { Linear, Logarithmic }

			#endregion

			#region Modules

			[Serializable]
			public class BehaviourModule
			{
				#region Variables

				public Interpolation interpolation;
				public float interpolationTime = 1f;
				public float interpolationTimeAlt = 1f;
				[FormerlySerializedAs("activePoint")]
				public float activationThreshold;
				public float downforce = 100f;
				public float downforceAlt = 100f;
				public float drag = .01f;
				public float dragAlt = .01f;
				public Vector3 localPosition;
				public Vector3 localRotation;
				public Vector3 localPositionAlt;
				public Vector3 localRotationAlt;
				public Utility.Interval speedRange = new(100f, Mathf.Infinity);

				[SerializeField]
				internal VehicleChassis chassis;
				[SerializeField]
				internal int wingIndex;

				#endregion

				#region Methods

				public float InterpolationTime(float speed)
				{
#if MVC_COMMUNITY
					return default;
#else
					if (speedRange.Max != Mathf.Infinity && speed > speedRange.Max && chassis.wings[wingIndex].steer != this)
						return interpolationTimeAlt;

					return interpolationTime;
#endif
				}
				public float Downforce(float speed)
				{
#if MVC_COMMUNITY
					return default;
#else
					if (speedRange.Max != Mathf.Infinity && speed > speedRange.Max)
						return downforceAlt;

					return downforce;
#endif
				}
				public float Drag(float speed)
				{
#if MVC_COMMUNITY
					return default;
#else
					if (speedRange.Max != Mathf.Infinity && speed > speedRange.Max)
						return dragAlt;

					return drag;
#endif
				}
				public Vector3 LocalPosition(float speed)
				{
#if MVC_COMMUNITY
					return default;
#else
					if (chassis.wings[wingIndex].steer == this)
						return Vector3.Lerp(localPositionAlt, localPosition, (Mathf.InverseLerp(activationThreshold, 1f, math.abs(chassis.VehicleInstance.Inputs.SteeringWheel)) * Mathf.Sign(chassis.VehicleInstance.Inputs.SteeringWheel) + 1f) * .5f);

					if (speedRange.Max != Mathf.Infinity && speed > speedRange.Max)
						return localPositionAlt;

					return localPosition;
#endif
				}
				public Vector3 LocalRotation(float speed)
				{
#if MVC_COMMUNITY
					return default;
#else
					if (this == chassis.wings[wingIndex].steer)
						return Vector3.Lerp(localRotationAlt, localRotation, (Mathf.InverseLerp(activationThreshold, 1f, Mathf.Abs(chassis.VehicleInstance.Stats.steerAngle)) * Mathf.Sign(chassis.VehicleInstance.Stats.steerAngle) + 1f) * .5f);

					if (speedRange.Max != Mathf.Infinity && speed > speedRange.Max)
						return localRotationAlt;

					return localRotation;
#endif
				}

				#endregion

				#region Constructors

				public BehaviourModule(VehicleChassis chassis, int wingIndex)
				{
					this.chassis = chassis;
					this.wingIndex = wingIndex;
				}

				#endregion

				#region Operators

				public static implicit operator bool(BehaviourModule behaviour) => behaviour != null;

				#endregion
			}

			#endregion

			#region Variables

			#region Editor Variables

			[NonSerialized]
			public bool editorFoldout;

			#endregion

			#region Global Variables

			public Transform transform;
			public VehicleChassisWingBehaviour behaviour;
			public bool IsActive => IsSpeedActive || IsBrakeActive || IsSteerActive || IsDecelActive;
			public bool IsFixedWing => behaviour == VehicleChassisWingBehaviour.None;
			public float fixedDownforce = 100f;
			public float fixedDrag = 100f;
			public BehaviourModule speed;
			public bool IsSpeedWing => behaviour.HasFlag(VehicleChassisWingBehaviour.Speed);
			public bool IsSpeedActive => IsSpeedWing && !IsSteerActive && !IsBrakeActive && !IsSteerActive && chassis.VehicleInstance.Stats.currentSpeed >= speed.speedRange.Min;
			public BehaviourModule brake;
			public bool IsBrakeWing => behaviour.HasFlag(VehicleChassisWingBehaviour.Brake);
			public bool IsBrakeActive => IsBrakeWing && Utility.Round(chassis.VehicleInstance.Inputs.Brake, 2) > brake.activationThreshold && chassis.VehicleInstance.Stats.currentSpeed >= brake.speedRange.Min;
			public BehaviourModule steer;
			public bool IsSteerWing => behaviour.HasFlag(VehicleChassisWingBehaviour.Steer);
			public bool IsSteerActive => IsSteerWing && !IsBrakeActive && Mathf.Abs(Utility.Round(chassis.VehicleInstance.Stats.steerAngle, 2)) > steer.activationThreshold && chassis.VehicleInstance.Stats.currentSpeed >= steer.speedRange.Min;
			public BehaviourModule decel;
			public bool IsDecelWing => behaviour.HasFlag(VehicleChassisWingBehaviour.Deceleration);
			public bool IsDecelActive => IsDecelWing && !IsBrakeActive && !IsSteerActive && !IsSpeedActive && chassis.VehicleInstance.Inputs.RawFuel < .05f && chassis.VehicleInstance.Stats.currentSpeed >= decel.speedRange.Min;
			public float CurrentDownforce
			{
				get
				{
#if MVC_COMMUNITY
					return default;
#else
					if (!IsValid)
						return 0f;

					if (IsFixedWing)
						return fixedDownforce;
					else
					{
						BehaviourModule behaviour = GetCurrentBehaviour();
						float downforce = behaviour.Downforce(chassis.VehicleInstance.Stats.currentSpeed);

						if (downforce == 0f)
							return 0f;

						float downForceFactor = Utility.InverseLerp(StartPosition, behaviour.LocalPosition(chassis.VehicleInstance.Stats.currentSpeed), transform.localPosition);

						return downforce * downForceFactor;
					}
#endif
				}
			}
			public float CurrentDrag
			{
				get
				{
#if MVC_COMMUNITY
					return default;
#else
					if (!IsValid)
						return 0f;

					if (IsFixedWing)
						return fixedDrag;
					else
					{
						BehaviourModule behaviour = GetCurrentBehaviour();
						float drag = behaviour.Drag(chassis.VehicleInstance.Stats.currentSpeed);

						if (drag == 0f)
							return 0f;

						float dragFactor = Utility.InverseLerp(StartPosition, behaviour.LocalPosition(chassis.VehicleInstance.Stats.currentSpeed), transform.localPosition);

						return drag * dragFactor;
					}
#endif
				}
			}
			public bool IsValid => transform && parent && transform.parent == parent && chassis && transform != chassis.transform && transform.IsChildOf(chassis.transform);

#if !MVC_COMMUNITY
			internal Vector3 StartPosition
			{
				get
				{
					if (!IsValid)
						return Vector3.zero;

					return startPosition;
				}
				set
				{
					startPosition = value;
				}
			}
			internal Vector3 StartRotation
			{
				get
				{
					if (!IsValid)
						return Vector3.zero;

					return startRotation;
				}
				set
				{
					startRotation = value;
				}
			}
#endif

			[SerializeField]
			private VehicleChassis chassis;
			[SerializeField]
			private Transform parent;
#if !MVC_COMMUNITY
			private BehaviourModule lastBehaviour;
			private Vector3 startPosition;
			private Vector3 startRotation;
#endif

			#endregion

			#endregion

			#region Methods

			public BehaviourModule GetDefaultBehaviour()
			{
#if MVC_COMMUNITY
				return null;
#else
				int newWingIndex = Array.IndexOf(chassis.wings, this);

				if (speed.wingIndex < 0)
					speed.wingIndex = newWingIndex;

				if (brake.wingIndex < 0)
					brake.wingIndex = newWingIndex;

				if (steer.wingIndex < 0)
					steer.wingIndex = newWingIndex;

				if (decel.wingIndex < 0)
					decel.wingIndex = newWingIndex;

				if (IsBrakeWing)
					return brake;
				else if (IsSteerWing)
					return steer;
				else if(IsSpeedWing)
					return speed;
				else if (IsDecelWing)
					return decel;
				else
					return null;
#endif
			}
			public BehaviourModule GetCurrentBehaviour()
			{
#if MVC_COMMUNITY
				return null;
#else
				int newWingIndex = Array.IndexOf(chassis.wings, this);

				if (speed.wingIndex < 0)
					speed.wingIndex = newWingIndex;

				if (brake.wingIndex < 0)
					brake.wingIndex = newWingIndex;

				if (steer.wingIndex < 0)
					steer.wingIndex = newWingIndex;

				if (decel.wingIndex < 0)
					decel.wingIndex = newWingIndex;

				if (IsFixedWing || !lastBehaviour)
					lastBehaviour = GetDefaultBehaviour();
				else if (IsBrakeActive)
					lastBehaviour = brake;
				else if (IsSteerActive)
					lastBehaviour = steer;
				else if (IsSpeedActive)
					lastBehaviour = speed;
				else if (IsDecelActive)
					lastBehaviour = decel;

				if (!lastBehaviour.chassis)
					lastBehaviour.chassis = chassis;

				return lastBehaviour;
#endif
			}
			public void UpdateParent(VehicleChassis chassis)
			{
				if (!transform)
					return;

				this.chassis = chassis;

				Transform newParent = transform.parent;

				if (!newParent || parent == newParent)
					return;

				UpdateBehaviourParent(brake, newParent);
				UpdateBehaviourParent(steer, newParent);
				UpdateBehaviourParent(speed, newParent);
				UpdateBehaviourParent(decel, newParent);

				parent = newParent;
			}

			private void UpdateBehaviourParent(BehaviourModule behaviour, Transform newParent)
			{
				behaviour.chassis = chassis;

				if (behaviour.wingIndex < 0)
					behaviour.wingIndex = Array.IndexOf(chassis.wings, this);

				if (behaviour.localPosition != Vector3.zero)
					behaviour.localPosition = newParent.InverseTransformPoint(parent.TransformPoint(behaviour.localPosition));

				if (behaviour.localRotation != Vector3.zero)
					behaviour.localRotation = newParent.InverseTransformVector(parent.TransformVector(behaviour.localRotation));
			}

			#endregion

			#region Constructors

			public WingModule(VehicleChassis chassis, int wingIndex)
			{
				this.chassis = chassis;
				brake = new(chassis, wingIndex);
				steer = new(chassis, wingIndex);
				speed = new(chassis, wingIndex);
				decel = new(chassis, wingIndex);
			}

			#endregion

			#region Operators

			public static implicit operator bool(WingModule wing) => wing != null;

			#endregion
		}

		#endregion

		#region Variables

		public Vehicle VehicleInstance
		{
			get
			{
				if (!vehicleInstance)
					vehicleInstance = GetComponentInParent<Vehicle>();

				return vehicleInstance;
			}
		}
		public Vector3 EngineCenterPosition
		{
			get
			{
				if (trailerInstance)
					return default;

				if (engineCenterPosition == default)
					RecalculateEnginePosition();

				return engineCenterPosition;
			}
		}
		public VehicleEngine.EnginePosition EnginePosition
		{
			get
			{
				if (trailerInstance)
					return VehicleEngine.EnginePosition.Front;

				return enginePosition;
			}
			set
			{
				if (trailerInstance || enginePosition == value)
					return;

				enginePosition = value;

				if (value == VehicleEngine.EnginePosition.MidRear)
					EnginePositionOffset = .5f;
				else
					EnginePositionOffset = 0f;
			}
		}
		public float EnginePositionOffset
		{
			get
			{
				if (trailerInstance)
					return 0f;

				return enginePositionOffset;
			}
			set
			{
				if (trailerInstance)
					return;

				enginePositionOffset = value;

				RecalculateEnginePosition();
			}
		}
		public VehicleEngineChassisTorque engineChassisTorque = VehicleEngineChassisTorque.Auto;
		public float EngineChassisTorqueMultiplier
		{
			get
			{
				return engineChassisTorqueMultiplier;
			}
			set
			{
				engineChassisTorqueMultiplier = math.max(value, 0f);
			}
		}
		public WingModule[] wings = new WingModule[] { };
		//public VehicleChassisPart[] parts = new VehicleChassisPart[] { };
		public Collider[] ignoredColliders;
		public Transform ExhaustModel
		{
			get
			{
				if (trailerInstance)
					return null;

				return exhaustModel;
			}
			set
			{
				if (trailerInstance || value && !value.IsChildOf(transform))
					return;

				exhaustModel = value;
			}
		}
		public Utility.Precision exhaustShakingPrecision;
		public Vector3 ExhaustShakeIntensity
		{
			get
			{
				if (trailerInstance)
					return default;

				return exhaustShakeIntensity;
			}
			set
			{
				if (trailerInstance)
					return;

				exhaustShakeIntensity = exhaustShakingPrecision == Utility.Precision.Simple ? Vector3.one * math.max(value.x, 0f) : math.max(value, 0f);
			}
		}
		[Obsolete("Use `FollowerPivots` instead.")]
		public VehicleFollowerPivot[] CameraPivots
		{
			get
			{
				return FollowerPivots;
			}
		}
		public VehicleFollowerPivot[] FollowerPivots
		{
			get
			{
				if (trailerInstance)
					return null;

				if (followerPivots == null)
					RefreshFollowerPivots();

				return followerPivots;
			}
		}
		public float CurrentWingsDownforce { get; private set; }
		public float CurrentWingsDrag { get; private set; }

		private Vehicle vehicleInstance;
		private VehicleTrailer trailerInstance;
		private Vector3 engineCenterPosition;
#if !MVC_COMMUNITY
		private Vector3[] wingsCenter;
#endif
		[SerializeField]
		private VehicleEngine.EnginePosition enginePosition;
		[SerializeField]
		private Transform exhaustModel;
		[SerializeField]
		private Vector3 exhaustShakeIntensity = Vector3.one;
		private Vector3 orgExhaustModelPosition;
		[SerializeField, FormerlySerializedAs("cameraPivots")]
		private VehicleFollowerPivot[] followerPivots;
		[SerializeField]
		private float engineChassisTorqueMultiplier = 1f;
		[SerializeField]
		private float enginePositionOffset;

		#endregion

		#region Methods

		#region Awake

		public void Restart()
		{
			Awaken = false;

			Initialize();

			if (HasInternalErrors || !IsSetupDone || !Settings || !VehicleInstance)
				return;

			trailerInstance = VehicleInstance as VehicleTrailer;

#if !MVC_COMMUNITY
			if (!trailerInstance)
			{
				wingsCenter = new Vector3[wings.Length];

				for (int i = 0; i < wings.Length; i++)
				{
					wings[i].UpdateParent(this);

					if (!wings[i].IsValid)
						continue;

					wings[i].StartPosition = wings[i].transform.localPosition;
					wings[i].StartRotation = wings[i].transform.localEulerAngles;
					wingsCenter[i] = transform.InverseTransformPoint(Utility.GetObjectBounds(wings[i].transform.gameObject).center);
				}
			}

			IgnoreColliders(true);
#endif

			if (!trailerInstance && ExhaustModel)
				orgExhaustModelPosition = ExhaustModel.localPosition;

			Awaken = true;
		}

		private void Initialize()
		{
#if !MVC_COMMUNITY
			wingsCenter = null;
#endif

			if (!trailerInstance)
			{
#if !MVC_COMMUNITY
				for (int i = 0; i < wings.Length; i++)
				{
					wings[i].UpdateParent(this);

					if (!wings[i].IsValid)
						continue;

					if (wings[i].StartPosition != Vector3.zero)
						wings[i].transform.localPosition = wings[i].StartPosition;

					wings[i].StartPosition = Vector3.zero;

					if (wings[i].StartRotation != Vector3.zero)
						wings[i].transform.localEulerAngles = wings[i].StartRotation;

					wings[i].StartRotation = Vector3.zero;
				}
#endif

				if (ExhaustModel && orgExhaustModelPosition != Vector3.zero)
					ExhaustModel.transform.localPosition = orgExhaustModelPosition;

				orgExhaustModelPosition = default;
			}

			trailerInstance = null;
		}

		#endregion

		#region Utilities

		public void IgnoreColliders(bool ignore)
		{
#if !MVC_COMMUNITY
			if (ignoredColliders == null || ignoredColliders.Length < 1)
				return;

			for (int i = 0; i < ignoredColliders.Length; i++)
				IgnoreCollider(ignoredColliders[i], ignore);
#endif
		}
		public void IgnoreCollider(Collider collider, bool ignore)
		{
#if !MVC_COMMUNITY
			if (!collider)
				return;
			else if (!collider.transform.IsChildOf(transform))
			{
				ToolkitDebug.Error("The requested collider to ignore is not a part of the Chassis colliders.");

				return;
			}
			else if (!Manager)
				return;

			var groundMappers = Manager.GroundMappers;

			if (groundMappers != null)
				for (int i = 0; i < groundMappers.Length; i++)
					Physics.IgnoreCollision(collider, groundMappers[i].ColliderInstance, ignore);
#endif
		}
		public void RefreshFollowerPivots()
		{
			if (trailerInstance)
				return;

			followerPivots = GetComponentsInChildren<VehicleFollowerPivot>(true);

			if (FollowerPivots.Length < 1 || !VehicleInstance || vehicleInstance.IsPrefab)
				return;

			Transform parent = transform.Find("CameraPivots");

			if (parent)
				parent.name = "FollowerPivots";

			parent = transform.Find("FollowerPivots");

			if (!parent)
			{
				parent = new GameObject("FollowerPivots").transform;

				parent.SetParent(transform, false);
			}

			if (Settings.useHideFlags)
				parent.gameObject.hideFlags = HideFlags.HideInHierarchy;

			for (int i = 0; i < FollowerPivots.Length; i++)
				FollowerPivots[i].transform.SetParent(parent, true);
		}

		private void RecalculateEnginePosition()
		{
			if (trailerInstance || !VehicleInstance)
				return;

			Vector3 chassisCenter = VehicleInstance.ChassisBounds.center;

			engineCenterPosition = chassisCenter + (EnginePosition == VehicleEngine.EnginePosition.Front ? Vector3.forward * VehicleInstance.ChassisBounds.extents.z : -Vector3.forward * VehicleInstance.ChassisBounds.extents.z);
			engineCenterPosition = new(0f, Utility.Average(engineCenterPosition.y, chassisCenter.y), Mathf.Lerp(engineCenterPosition.z, chassisCenter.z, enginePositionOffset));
		}

		#endregion

		#region Update

		private void Update()
		{
			if (!Awaken || trailerInstance)
				return;

#if !MVC_COMMUNITY
			Wings();
#endif
			Exhausts();
		}

#if !MVC_COMMUNITY
		private void Wings()
		{
			if (!Settings.useChassisWings)
				return;

			for (int i = 0; i < wings.Length; i++)
			{
				if (!wings[i].IsValid || wings[i].IsFixedWing)
					continue;

				WingModule.BehaviourModule behaviour = wings[i].GetCurrentBehaviour();
				Vector3 newLocalPosition = behaviour.LocalPosition(VehicleInstance.Stats.currentSpeed);
				Vector3 targetPosition = wings[i].IsActive ? newLocalPosition : wings[i].StartPosition;
				Vector3 newLocalRotation = behaviour.LocalRotation(VehicleInstance.Stats.currentSpeed);
				Vector3 targetRotation = wings[i].IsActive ? newLocalRotation : wings[i].StartRotation;
				float interpolationTime = behaviour.InterpolationTime(VehicleInstance.Stats.currentSpeed);
				bool logarithmicInterpolation = behaviour.interpolation == WingModule.Interpolation.Logarithmic;

				if (logarithmicInterpolation)
					wings[i].transform.SetLocalPositionAndRotation(Vector3.Lerp(wings[i].transform.localPosition, targetPosition, Time.deltaTime * interpolationTime), Quaternion.Slerp(wings[i].transform.localRotation, Quaternion.Euler(targetRotation), Time.deltaTime * interpolationTime));
				else if (interpolationTime > 0f)
				{
					float distanceFromTarget = Utility.Distance(wings[i].transform.localPosition, targetPosition);
					float angleFromTarget = Quaternion.Angle(wings[i].transform.localRotation, Quaternion.Euler(targetPosition));
					float inertia = Time.deltaTime / interpolationTime;

					wings[i].transform.SetLocalPositionAndRotation(Vector3.MoveTowards(wings[i].transform.localPosition, targetPosition, distanceFromTarget * inertia), Quaternion.RotateTowards(wings[i].transform.localRotation, Quaternion.Euler(targetRotation), angleFromTarget * inertia));
				}
				else
					wings[i].transform.SetLocalPositionAndRotation(targetPosition, Quaternion.Euler(targetRotation));
			}
		}
#endif
		private void Exhausts()
		{
			if (VehicleInstance.IsElectric || !ExhaustModel)
				return;

			ExhaustModel.localPosition = orgExhaustModelPosition + ((VehicleInstance.Stats.isEngineStarting ? 1f : VehicleInstance.Stats.rawEnginePower / VehicleInstance.Engine.Power) * .001f * 1.618f * Utility.Multiply(UnityEngine.Random.insideUnitSphere, ExhaustShakeIntensity));
		}

		#endregion

		#region Fixed Update

#if !MVC_COMMUNITY
		private void FixedUpdate()
		{
			if (!Awaken)
				return;

			if (!trailerInstance)
				WingForces();
		}
		
		private void WingForces()
		{
			if (!Settings.useChassisWings || !Settings.useDownforce && !Settings.useDrag)
				return;

			CurrentWingsDownforce = 0f;
			CurrentWingsDrag = 0f;

			for (int i = 0; i < wings.Length; i++)
			{
				if (!wings[i].IsValid)
					continue;

				float currentDrag = wings[i].CurrentDrag;

				if (wings[i].CurrentDownforce == 0f && currentDrag == 0f)
					continue;

				if (Settings.useDownforce && (Settings.useDownforceWhenNotGrounded || VehicleInstance.Stats.isGrounded))
					VehicleInstance.Rigidbody.AddForceAtPosition((Settings.useDownforceWhenNotGrounded ? 1f : VehicleInstance.Stats.groundedWheelsCount / VehicleInstance.Wheels.Length) * CurrentWingsDownforce * -VehicleInstance.transform.up, transform.TransformPoint(wingsCenter[i]));
				
				if (Settings.useDrag)
					VehicleInstance.Rigidbody.
#if UNITY_6000_0_OR_NEWER
					linearDamping
#else
					drag
#endif
						+= currentDrag;
			}
		}
#endif

		#endregion

		#region Late Update

		private void LateUpdate()
		{
			if (!Awaken)
				return;

			Lights();
		}
		private void Lights()
		{
			if (HasInternalErrors || !Settings.useLights)
				return;

			for (int i = 0; i < VehicleInstance.Lights.Length; i++)
				VehicleInstance.Lights[i].Update();
		}

		#endregion

		#region Gizmos

		private void OnDrawGizmosSelected()
		{
			if (!trailerInstance && VehicleInstance && VehicleInstance.IsTrailer)
				trailerInstance = VehicleInstance as VehicleTrailer;

			if (HasInternalErrors || !IsSetupDone || !Settings || trailerInstance)
				return;

			Vector3 enginePosition = transform.TransformPoint(EngineCenterPosition);
			Color orgColor = Gizmos.color;

			Gizmos.color = Settings.engineGizmoColor;

			Gizmos.DrawSphere(enginePosition, Settings.gizmosSize / 16f);

			Gizmos.color = orgColor;
		}

		#endregion

		#endregion
	}

	#endregion
}

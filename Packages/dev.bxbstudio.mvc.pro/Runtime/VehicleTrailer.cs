#region Namespaces

using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
using MVC.Core;

#endregion

namespace MVC.Base
{
	[AddComponentMenu("")]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(-20)]
	public class VehicleTrailer : Vehicle
	{
		#region Modules

		[Serializable]
		public class JointModule
		{
			#region Variables

			public Vehicle ConnectedVehicle
			{
				get
				{
					if (!connectedVehicle && ConnectedLink)
						connectedVehicle = ConnectedLink.GetComponent<Vehicle>();
					else if (!ConnectedLink)
						connectedVehicle = null;

					return connectedVehicle;
				}
			}
			public VehicleTrailerLink ConnectedLink { get; set; }
			public Vector3 Position
			{
				get
				{
					return position;
				}
				set
				{
					position = new(0f, value.y, math.max(value.z, 0f));
				}
			}
			public Vector3 RotationAxis
			{
				get
				{
					if (jointRotationAxis == default)
						jointRotationAxis = Vector3.up;

					return jointRotationAxis;
				}
				set
				{
					jointRotationAxis = value == default ? Vector3.up : value.normalized;
				}
			}
			public Utility.Interval angularMotionXLimit = new(-30f, 30f);
			public float BreakForce
			{
				get
				{
					return breakForce;
				}
				set
				{
					breakForce = math.max(value, 0f);
				}
			}
			public float BreakTorque
			{
				get
				{
					return breakTorque;
				}
				set
				{
					breakTorque = math.max(value, 0f);
				}
			}

			private Vehicle connectedVehicle;
			[SerializeField]
			private Vector3 position;
			[SerializeField]
			private Vector3 jointRotationAxis = Vector3.up;
			[SerializeField]
			private float breakForce = Mathf.Infinity;
			[SerializeField]
			private float breakTorque = Mathf.Infinity;

			#endregion

			#region Operators

			public static implicit operator bool(JointModule module) => module != null;

			#endregion
		}
		[Serializable]
		public class ComponentsModule
		{
			#region Variables

			public bool IsValid
			{
				get
				{
					return vehicle;
				}
			}

			[SerializeField]
			private Vehicle vehicle;

			#endregion

			#region Constructors

			public ComponentsModule(Vehicle vehicle)
			{
				this.vehicle = vehicle;
			}

			#endregion

			#region Operators

			public static implicit operator bool(ComponentsModule module) => module != null;

			#endregion
		}
		[Serializable]
		public new class BehaviourModule
		{
			#region Variables

			public float CurbWeight
			{
				get
				{
					return curbWeight;
				}
				set
				{
					curbWeight = math.max(value, 10f);
				}
			}

			[SerializeField]
			private float curbWeight = 1500f;

			#endregion

			#region Operators

			public static implicit operator bool(BehaviourModule module) => module != null;

			#endregion
		}
		[Serializable]
		public class FollowerModifierModule
		{
			#region Variables

			public float Distance
			{
				get
				{
					return distance;
				}
				set
				{
					distance = Mathf.Clamp(value, 0f, 3f);
				}
			}
			public float Height
			{
				get
				{
					return height;
				}
				set
				{
					height = Mathf.Clamp(value, 0f, 2f);
				}
			}

			[SerializeField]
			private float distance = 1f;
			[SerializeField]
			private float height = 1f;

			#endregion

			#region Operators

			public static implicit operator bool(FollowerModifierModule module) => module != null;

			#endregion
		}

		internal struct FollowerModifierAccess
		{
			#region Variables

			public float distance;
			public float height;

			#endregion

			#region Constructors

			public FollowerModifierAccess(FollowerModifierModule module) : this()
			{
				if (!module)
					return;

				distance = module.Distance;
				height = module.Height;
			}

			#endregion

			#region Operators

			public static implicit operator FollowerModifierAccess(FollowerModifierModule module) => new(module);

			#endregion
		}

		#endregion

		#region Variables

		#region Components

		public Transform Stands
		{
			get
			{
				if (!Chassis)
					return null;
				else if (!Application.isPlaying && stands)
				{
					standsIdleLocalPosition = stands.localPosition;
					standsIdleLocalRotation = stands.localRotation;
				}

				return stands;
			}
			set
			{
				if (!Chassis || value && !value.transform.IsChildOf(Chassis.transform) || stands == value)
					return;

				if (value)
				{
					standsIdleLocalPosition = value.localPosition;
					standsIdleLocalRotation = value.localRotation;
				}

				stands = value;
			}
		}
		public Quaternion StandsIdleLocalRotation
		{
			get
			{
				if (!Stands)
					return default;

				return standsIdleLocalRotation;
			}
			set
			{
				if (!Stands)
					return;

				standsIdleLocalRotation = value;
				stands.localRotation = value;
			}
		}
		public Quaternion StandsLiftedLocalRotation
		{
			get
			{
				if (!Stands)
					return default;
				else if (standsLiftedLocalRotation == default && standsLiftedLocalRotation != Stands.localRotation)
					return standsIdleLocalRotation;

				return standsLiftedLocalRotation;
			}
			set
			{
				if (!Stands)
					return;

				standsLiftedLocalRotation = value;
			}
		}
		public Vector3 StandsIdleLocalPosition
		{
			get
			{
				if (!Stands)
					return default;

				return standsIdleLocalPosition;
			}
			set
			{
				if (!Stands)
					return;

				standsIdleLocalPosition = value;
				stands.localPosition = value;
			}
		}
		public Vector3 StandsLiftedLocalPosition
		{
			get
			{
				if (!Stands)
					return default;
				else if (standsLiftedLocalPosition == default && standsLiftedLocalPosition != Stands.localPosition)
					return standsIdleLocalPosition;

				return standsLiftedLocalPosition;
			}
			set
			{
				if (!Stands)
					return;

				standsLiftedLocalPosition = value;
			}
		}
		public float StandsLiftTime
		{
			get
			{
				if (!Stands)
					return default;

				return standsLiftTime;
			}
			set
			{
				if (!Stands)
					return;

				standsLiftTime = value;
			}
		}
		public bool LiftStands { get; set; }

		[SerializeField]
		private Transform stands;
		[SerializeField]
		private Quaternion standsIdleLocalRotation;
		[SerializeField]
		private Quaternion standsLiftedLocalRotation;
		[SerializeField]
		private Vector3 standsIdleLocalPosition;
		[SerializeField]
		private Vector3 standsLiftedLocalPosition;
		[SerializeField]
		private float standsLiftTime = 2f;

		#endregion

		#region Modules

		public JointModule Joint
		{
			get
			{
				if (!joint)
					joint = new();

				return joint;
			}
		}
		public new BehaviourModule Behaviour
		{
			get
			{
				if (!trailerBehaviour)
					trailerBehaviour = new();

				return trailerBehaviour;
			}
		}
		public BrakeModule Brakes
		{
			get
			{
				if (!useBrakes)
					return null;

				if (!trailerBrakes)
					trailerBrakes = new();

				return trailerBrakes;
			}
		}
		public FollowerModifierModule FollowerModifier
		{
			get
			{
				if (!followerModifier)
					followerModifier = new();

				return followerModifier;
			}
		}
		public bool useBrakes;

		[SerializeField]
		private JointModule joint;
		[SerializeField]
		private BehaviourModule trailerBehaviour;
		[SerializeField]
		private BrakeModule trailerBrakes;
		[SerializeField]
		private FollowerModifierModule followerModifier;

		#endregion

		#region Temp

		private VehicleTrailerLink detectedLink;
		private ConfigurableJoint linkJoint;
		private Collider[] linkHits;
		private Vector3 targetConnectedAnchor;
		private float standsLiftPositionFactor;
		private float standsLiftRotationFactor;
		private VehicleTrailerLink linkJustBroke;

		#endregion

		#endregion

		#region Methods

		#region Virtual Methods

		internal override void OnDrawGizmosSelected()
		{
			if (!IsSetupDone || !Settings)
				return;

			base.OnDrawGizmosSelected();

			if (!Chassis)
				return;

			Color orgGizmosColor = Gizmos.color;

			Gizmos.color = Settings.jointsGizmoColor;

			Vector3 position = Chassis.transform.TransformPoint(Joint.Position);
			Vector3 direction = Chassis.transform.TransformDirection(Joint.RotationAxis);

			Gizmos.DrawSphere(position, Settings.gizmosSize / 16f);
			Utility.DrawArrowForGizmos(position, direction * Settings.gizmosSize / 2f, Settings.gizmosSize / 8f);

			Gizmos.color = orgGizmosColor;
		}

		#endregion

		#region Global Methods

		#region Awake

		public new void Restart()
		{
			Initialize();

			linkHits = new Collider[20];

			Linking();

			if (Joint.ConnectedVehicle && Stands)
				Stands.SetLocalPositionAndRotation(StandsLiftedLocalPosition, StandsLiftedLocalRotation);

			standsLiftPositionFactor = !Stands || StandsIdleLocalPosition != StandsLiftedLocalPosition ? Utility.BoolToNumber(Joint.ConnectedVehicle) : 1f;
			standsLiftRotationFactor = !Stands || StandsIdleLocalRotation != StandsLiftedLocalRotation ? Utility.BoolToNumber(Joint.ConnectedVehicle) : 1f;
			LiftStands = Stands && standsLiftPositionFactor >= 1f && standsLiftRotationFactor >= 1f;
		}

		private void Initialize()
		{
			LiftStands = default;
			linkJoint = null;
			linkHits = null;
			linkJustBroke = default;

			if (Stands)
				Stands.SetLocalPositionAndRotation(StandsIdleLocalPosition, StandsIdleLocalRotation);

			standsLiftPositionFactor = default;
			standsLiftRotationFactor = default;
		}

		#endregion

		#region Utilities

		public void UnlinkTrailer()
		{
			StartCoroutine(StartUnlinkingTrailer());
		}

		private IEnumerator StartUnlinkingTrailer()
		{
			LiftStands = false;

			yield return new WaitWhile(() => Stands && (StandsIdleLocalPosition != StandsLiftedLocalPosition && standsLiftPositionFactor > 0f) || (StandsIdleLocalRotation != StandsLiftedLocalRotation && standsLiftRotationFactor > 0f));

			RemoveLinkJoint();
		}
		private ConfigurableJoint AddOrGetLinkJoint(VehicleTrailerLink link)
		{
			if (!link || !link.VehicleInstance)
				return null;

			ConfigurableJoint joint = GetComponent<ConfigurableJoint>();

			if (!joint)
				joint = gameObject.AddComponent<ConfigurableJoint>();

			joint.anchor = transform.InverseTransformPoint(Chassis.transform.TransformPoint(Joint.Position));
			joint.connectedBody = link.VehicleInstance.Rigidbody;
			joint.autoConfigureConnectedAnchor = true;
			joint.xMotion = ConfigurableJointMotion.Locked;
			joint.yMotion = ConfigurableJointMotion.Locked;
			joint.zMotion = ConfigurableJointMotion.Locked;
			joint.angularXMotion = ConfigurableJointMotion.Limited;
			joint.angularYMotion = ConfigurableJointMotion.Free;
			joint.angularZMotion = ConfigurableJointMotion.Locked;
			joint.lowAngularXLimit = new() { limit = Joint.angularMotionXLimit.Min };
			joint.highAngularXLimit = new() { limit = Joint.angularMotionXLimit.Max };
			joint.enableCollision = true;
			joint.breakForce = Joint.BreakForce;
			joint.breakTorque = Joint.BreakTorque;

			if (Settings.useHideFlags)
				joint.hideFlags = HideFlags.HideInInspector;

			return joint;
		}
		private void RemoveLinkJoint()
		{
			ConfigurableJoint joint = GetComponent<ConfigurableJoint>();

			if (!joint)
				return;

			Utility.Destroy(true, joint);
		}

		#endregion

		#region Update

		private void Update()
		{
			if (!Awaken)
				return;

			StandsLift();
		}
		private void StandsLift()
		{
			if (!Stands)
				return;

			if (StandsIdleLocalPosition != StandsLiftedLocalPosition)
			{
				if (standsLiftPositionFactor != (LiftStands ? 1f : 0f))
				{
					standsLiftPositionFactor += (LiftStands ? 1f : -1f) * Time.deltaTime / StandsLiftTime;
					standsLiftPositionFactor = Mathf.Clamp01(standsLiftPositionFactor);
				}

				Stands.localPosition = Vector3.Lerp(StandsIdleLocalPosition, StandsLiftedLocalPosition, standsLiftPositionFactor);
			}

			if (StandsIdleLocalRotation != StandsLiftedLocalRotation)
			{
				if (standsLiftRotationFactor != (LiftStands ? 1f : 0f))
				{
					standsLiftRotationFactor += (LiftStands ? 1f : -1f) * Time.deltaTime / StandsLiftTime;
					standsLiftRotationFactor = Mathf.Clamp01(standsLiftRotationFactor);
				}

				Stands.localRotation = Quaternion.Lerp(StandsIdleLocalRotation, StandsLiftedLocalRotation, standsLiftRotationFactor);
			}
		}

		#endregion

		#region Fixed Update

		private void FixedUpdate()
		{
			if (!Awaken)
				return;

			Linking();
			Torque();
		}
		private void Linking()
		{
			if (Joint.ConnectedVehicle)
			{
				if (!linkJoint)
				{
					linkJustBroke = Joint.ConnectedLink;

					if (Joint.ConnectedLink)
						Joint.ConnectedLink.ConnectedTrailer = null;

					Joint.ConnectedLink = null;
				}
				else
				{
					linkJustBroke = null;

					if (Joint.ConnectedVehicle == Manager.PlayerVehicle && Joint.ConnectedVehicle.Inputs.TrailerLinkSwitchWasPressed)
						UnlinkTrailer();
					else
					{
						targetConnectedAnchor = linkJoint.connectedBody.transform.InverseTransformPoint(Joint.ConnectedLink.VehicleInstance.Chassis.transform.TransformPoint(Joint.ConnectedLink.LinkPoint));

						if (linkJoint.connectedAnchor != targetConnectedAnchor)
						{
							linkJoint.breakForce = Mathf.Infinity;
							linkJoint.breakTorque = Mathf.Infinity;

							if (linkJoint.autoConfigureConnectedAnchor)
								linkJoint.autoConfigureConnectedAnchor = false;

							linkJoint.connectedAnchor = Utility.Round(Vector3.Lerp(linkJoint.connectedAnchor, targetConnectedAnchor, Time.fixedDeltaTime * 2f), 4);
						}
						else if (!linkJoint.autoConfigureConnectedAnchor)
						{
							linkJoint.autoConfigureConnectedAnchor = true;
							linkJoint.breakForce = Joint.BreakForce;
							linkJoint.breakTorque = Joint.BreakTorque;
						}
					}
				}
			}
			else
			{
				if (linkJoint)
				{
					RemoveLinkJoint();

					linkJustBroke = Joint.ConnectedLink;
				}

			detect_links:
				Vector3 detectionPosition = Chassis.transform.TransformPoint(Joint.Position);
				int hitsCount = Physics.OverlapSphereNonAlloc(detectionPosition, .125f, linkHits, LayerMask.GetMask(LayerMask.LayerToName(Settings.vehiclesLayer)));
				bool addedLinkDetectors = false;

				if (!linkJustBroke)
				{
					for (int i = 0; i < hitsCount; i++)
					{
						detectedLink = linkHits[i].GetComponentInParent<VehicleTrailerLink>();

						if (detectedLink && !detectedLink.GetTrailerDetector())
							addedLinkDetectors = addedLinkDetectors || detectedLink.AddTrailerDetector();

						if (!linkHits[i].isTrigger)
						{
							detectedLink = null;

							continue;
						}

						if (detectedLink && !detectedLink.ConnectedTrailer && Utility.Distance(detectionPosition, detectedLink.transform.TransformPoint(detectedLink.LinkPoint)) < detectedLink.LinkRadius + .125f)
							break;
						else
							detectedLink = null;
					}

					if (addedLinkDetectors)
						goto detect_links;
					else if (detectedLink && detectedLink != TrailerLink && (!Settings.linkTrailerManually || detectedLink.VehicleInstance.IsAI || detectedLink.VehicleInstance.IsTrailer || detectedLink.VehicleInstance.Inputs.TrailerLinkSwitchWasPressed))
					{
						detectedLink.ConnectedTrailer = this;
						Joint.ConnectedLink = detectedLink;
						linkJoint = AddOrGetLinkJoint(detectedLink);
						LiftStands = Stands;
						detectedLink = null;

						StabilityModule orgStability = Stability;

						SetStability(new(Joint.ConnectedVehicle.Stability));

						if (orgStability)
						{
							Stability.useAntiSwayBars = orgStability.useAntiSwayBars;
							Stability.AntiSwayRear = orgStability.AntiSwayRear;
							Stability.HandlingRate = orgStability.HandlingRate;
							Stability.WeightDistribution = orgStability.WeightDistribution;
							Stability.WeightHeight = orgStability.WeightHeight;
							Stability.useDownforce = orgStability.useDownforce;
							Stability.RearDownforce = orgStability.RearDownforce;
							Stability.Drag = orgStability.Drag;
							Stability.AngularDrag = orgStability.AngularDrag;
							/*Stability.DragCoefficient = orgStability.DragCoefficient;
							Stability.FrontalArea = orgStability.FrontalArea;
							Stability.FrontLateralDragCoefficient = orgStability.FrontLateralDragCoefficient;
							Stability.RearLateralDragCoefficient = orgStability.RearLateralDragCoefficient;
							Stability.VerticalDragCoefficient = orgStability.VerticalDragCoefficient;*/
							Stability.DragScale = orgStability.DragScale;
						}
					}
				}
				else
				{
					Vector3 linkPoint = linkJustBroke.VehicleInstance.Chassis.transform.TransformPoint(linkJustBroke.LinkPoint);

					if (Utility.Distance(detectionPosition, linkPoint) * .5f - .25f > linkJustBroke.LinkRadius)
					{
						linkJustBroke = null;
						linkHits = new Collider[20];
					}
				}
			}
		}
		private void Torque()
		{
			if (!ToolkitSettings.UsingWheelColliderPhysics)
				return;

			for (int i = 0; i < Wheels.Length; i++)
				if (Wheels[i].Instance)
					Wheels[i].Instance.motorTorque = Joint.ConnectedVehicle ? Mathf.Clamp01(Joint.ConnectedVehicle.Inputs.Fuel - Joint.ConnectedVehicle.Inputs.Brake) * Joint.ConnectedVehicle.Inputs.Direction : 0f;
		}

		#endregion

		#endregion

		#endregion
	}
}

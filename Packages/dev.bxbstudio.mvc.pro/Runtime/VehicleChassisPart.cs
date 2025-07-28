/*#region Namespaces

using System;
using System.Linq;
using UnityEngine;
using Utilities;
using MVC.Core;

#endregion

namespace MVC.Base
{
	[AddComponentMenu("Multiversal Vehicle Controller/Misc/Chassis Part", 20)]
	[DisallowMultipleComponent]
	public class VehicleChassisPart : ToolkitBehaviour
	{
		#region Enumerators

		public enum PartType { None = -1, Bumper, Grill, Fender, Hood, Roof, Door, DoorMirror, SideSkirt, Trunk, Wing, Diffuser, Glass, HeadLight, TailLight }

		#endregion

		#region Modules

		[Serializable]
		public class JointsGroup
		{
			#region Variables

			public FixedJoint staticJoint;
			public HingeJoint dynamicJoint;
			public Rigidbody StaticRigid
			{
				get
				{

					if (!staticRigid)
					{
						if (!staticJoint)
							return null;

						staticRigid = staticJoint.GetComponent<Rigidbody>();
					}

					return staticRigid;
				}
			}
			public Rigidbody DynamicRigid
			{
				get
				{
					if (!dynamicJoint)
						return null;

					if (!dynamicRigid)
						dynamicRigid = dynamicJoint.GetComponent<Rigidbody>();

					return dynamicRigid;
				}
			}

			private Rigidbody staticRigid;
			private Rigidbody dynamicRigid;

			#endregion

			#region Constructors

			public JointsGroup()
			{
				staticJoint = null;
				dynamicJoint = null;
			}
			public JointsGroup(JointsGroup group)
			{
				staticJoint = group.staticJoint;
				dynamicJoint = group.dynamicJoint;
			}

			#endregion
		}

		#endregion

		#region Variables

		public VehicleChassis ChassisInstance
		{
			get
			{
				if (!chassisInstance)
					chassisInstance = GetComponentInParent<VehicleChassis>();

				return chassisInstance;
			}
		}
		public PartType type = PartType.None;
		public Vector3[] pivots = new Vector3[1];
		public float mass = 20f;
		public bool isDamageable = true;
		public JointsGroup Joints => Settings.chassisPartJoints.GetJoints(type);

		[SerializeField]
		private VehicleChassis chassisInstance;
		private JointsGroup jointsGroup;
		private Collider[] Colliders => GetComponentsInChildren<Collider>();
		private Quaternion orgRotation;

		#endregion

		#region Methods

		#region Virtual Methods

		public void Restart()
		{
			Awaken = false;

			Initialize();

			if (!ToolkitSettings.IsPlusVersion || ChassisInstance.parts.ToList().IndexOf(this) < 0)
				return;

			IgnoreCollisions(true);

			jointsGroup = new(Joints);

			if (jointsGroup.staticJoint)
			{
				jointsGroup.staticJoint = Instantiate(jointsGroup.staticJoint.gameObject, transform.position, ChassisInstance.transform.rotation, transform.parent).GetComponent<FixedJoint>();
				jointsGroup.staticJoint.connectedBody = ChassisInstance.VehicleInstance.Rigidbody;
				jointsGroup.staticJoint.enablePreprocessing = false;
				jointsGroup.staticJoint.enableCollision = false;
				jointsGroup.StaticRigid.mass = mass;
			}

			if (jointsGroup.dynamicJoint)
			{
				jointsGroup.dynamicJoint = Instantiate(jointsGroup.dynamicJoint.gameObject, transform.position, ChassisInstance.transform.rotation, transform.parent).GetComponent<HingeJoint>();
				jointsGroup.dynamicJoint.connectedBody = ChassisInstance.VehicleInstance.Rigidbody;
				jointsGroup.dynamicJoint.autoConfigureConnectedAnchor = false;
				jointsGroup.dynamicJoint.connectedAnchor = ChassisInstance.VehicleInstance.transform.InverseTransformPoint(transform.position);
				jointsGroup.dynamicJoint.anchor = pivots[0];
				jointsGroup.dynamicJoint.enablePreprocessing = false;
				jointsGroup.dynamicJoint.enableCollision = false;
				jointsGroup.DynamicRigid.mass = mass;

				if (!jointsGroup.staticJoint)
					transform.SetParent(jointsGroup.dynamicJoint.transform, true);
				else
					transform.SetParent(jointsGroup.staticJoint.transform, true);
			}
			else if (jointsGroup.staticJoint)
				transform.SetParent(jointsGroup.staticJoint.transform, true);

			orgRotation = transform.localRotation;
			Awaken = true;
		}

		private void Initialize()
		{
			jointsGroup = new();
			orgRotation = Quaternion.identity;
		}

		internal void Update()
		{
			if (!ToolkitSettings.IsPlusVersion || !Settings.useDamage)
				return;

			if (!jointsGroup.staticJoint && jointsGroup.StaticRigid && jointsGroup.dynamicJoint)
			{
				transform.SetParent(jointsGroup.dynamicJoint.transform, true);
				transform.localPosition = Vector3.zero;
				transform.localRotation = orgRotation;

				Utility.Destroy(jointsGroup.StaticRigid.gameObject);
			}
		}

		#endregion

		#region Global Methods

		private void IgnoreCollisions(bool ignore)
		{
			for (int i = 0; i < Colliders.Length; i++)
				for (int j = 0; j < ChassisInstance.parts.Length; j++)
				{
					if (!ChassisInstance.parts[j])
						continue;

					for (int k = 0; k < ChassisInstance.parts[j].Colliders.Length; k++)
						Physics.IgnoreCollision(Colliders[i], ChassisInstance.parts[j].Colliders[k], ignore);
				}
		}

		#endregion

		#endregion
	}
}*/

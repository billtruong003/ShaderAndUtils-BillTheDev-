/*#region Namespaces

using System;
using UnityEngine;
using Utilities;
using MVC.Core;
using System.Collections.Generic;

#endregion

namespace MVC.Base
{
	[AddComponentMenu("Multiversal Vehicle Controller/Visuals/Suspension Joint", 70)]
	[DisallowMultipleComponent]
	public class VehicleSuspensionJoint : ToolkitBehaviour
	{
		#region Modules

		[Serializable]
		public struct JointPivot
		{
			#region Enumerators

			public enum PivotType { Fixed, Transform }

			#endregion

			#region Variables

			public PivotType type;
			public Transform transform;
			public Vector3 position;
			public bool IsValid
			{
				get
				{
					return joint && joint.VehicleInstance;
				}
			}

			[SerializeField]
			private VehicleSuspensionJoint joint;

			#endregion

			#region Methods

			#region Virtual Methods

			public override bool Equals(object obj)
			{
				return obj is JointPivot pivot &&
					   type == pivot.type &&
					   EqualityComparer<Transform>.Default.Equals(transform, pivot.transform) &&
					   position.Equals(pivot.position);
			}
			public override int GetHashCode()
			{
				int hashCode = -862732038;

				hashCode = hashCode * -1521134295 + type.GetHashCode();
				hashCode = hashCode * -1521134295 + EqualityComparer<Transform>.Default.GetHashCode(transform);
				hashCode = hashCode * -1521134295 + position.GetHashCode();

				return hashCode;
			}

			#endregion

			#region Global Methods

			public Vector3 GetPoint()
			{
				if (!IsValid)
					return Vector3.zero;

				return joint.VehicleInstance.transform.TransformPoint(position);
			}
			public Vector3 GetPoint(Vector3 offset)
			{
				if (!IsValid)
					return Vector3.zero;

				return joint.VehicleInstance.transform.TransformPoint(position + offset);
			}

			#endregion

			#endregion

			#region Constructors

			public JointPivot(VehicleSuspensionJoint joint)
			{
				type = PivotType.Fixed;
				transform = null;
				position = Vector3.zero;
				this.joint = joint;
			}

			#endregion

			#region Operators

			public static bool operator ==(JointPivot pivot1, JointPivot pivot2)
			{
				return pivot1.Equals(pivot2);
			}
			public static bool operator !=(JointPivot pivot1, JointPivot pivot2)
			{
				return !(pivot1 == pivot2);
			}

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
		public JointPivot OriginPivot
		{
			get
			{
				if (VehicleInstance && !origin.IsValid)
				{
					Bounds bounds = Utility.GetObjectBounds(gameObject, true);

					origin = new(this)
					{
						position = bounds.center - Mathf.Sign(VehicleInstance.transform.InverseTransformPoint(bounds.center).x) * bounds.extents.x * VehicleInstance.transform.right
					};
				}

				return origin;
			}
			set
			{
				if (!VehicleInstance || value.type == JointPivot.PivotType.Transform && value.transform && !value.transform.IsChildOf(VehicleInstance.transform))
					return;

				origin = value;
			}
		}
		public JointPivot TargetPivot
		{
			get
			{
				if (VehicleInstance && !target.IsValid)
				{
					Bounds bounds = Utility.GetObjectBounds(gameObject, true);

					target = new(this)
					{
						position = bounds.center + Mathf.Sign(VehicleInstance.transform.InverseTransformPoint(bounds.center).x) * bounds.extents.x * VehicleInstance.transform.right
					};
				}

				return target;
			}
			set
			{
				if (!VehicleInstance || value.type == JointPivot.PivotType.Transform && value.transform && !value.transform.IsChildOf(VehicleInstance.transform))
					return;

				target = value;
			}
		}
		public Vector3 rotationAxis = Vector3.forward;

		private Vehicle vehicleInstance;
		[SerializeField]
		private JointPivot origin;
		[SerializeField]
		private JointPivot target;
		private Vector3 orgOriginTransform;
		private Vector3 orgTargetTransform;
		private Vector3 currentOriginTransform;
		private Vector3 currentTargetTransform;
		private Vector3 orgPosition;
		private Quaternion orgRotation;

		#endregion

		#region Methods

		#region Awake

		public void Restart()
		{
			Awaken = false;

			Initialize();

			if (!VehicleInstance || OriginPivot.type == JointPivot.PivotType.Transform && !OriginPivot.transform || TargetPivot.type == JointPivot.PivotType.Transform && !TargetPivot.transform)
				return;

			if (OriginPivot.transform && !OriginPivot.transform.IsChildOf(VehicleInstance.transform) || TargetPivot.transform && !TargetPivot.transform.IsChildOf(VehicleInstance.transform))
				return;

			rotationAxis.Normalize();
			orgOriginTransform = VehicleInstance.transform.InverseTransformPoint(OriginPivot.transform ? OriginPivot.transform.position : Vector3.zero);
			orgTargetTransform = VehicleInstance.transform.InverseTransformPoint(TargetPivot.transform ? TargetPivot.transform.position : Vector3.zero);
			orgPosition = transform.localPosition;
			orgRotation = transform.localRotation;
			Awaken = true;
		}

		private void Initialize()
		{
			vehicleInstance = null;
			orgOriginTransform = default;
			orgTargetTransform = default;
			currentOriginTransform = default;
			currentTargetTransform = default;
			orgPosition = default;
			orgRotation = default;
		}

		#endregion

		#region Late Update

		private void LateUpdate()
		{
			if (!Awaken)
				return;

			currentOriginTransform = VehicleInstance.transform.InverseTransformPoint(OriginPivot.transform ? OriginPivot.transform.position : Vector3.zero) - orgOriginTransform;
			currentTargetTransform = VehicleInstance.transform.InverseTransformPoint(TargetPivot.transform ? TargetPivot.transform.position : Vector3.zero) - orgTargetTransform;
			transform.localPosition = orgPosition;
			transform.localRotation = orgRotation;

			transform.RotateAround(OriginPivot.GetPoint(currentOriginTransform), rotationAxis, Utility.Multiply(rotationAxis, Quaternion.LookRotation(Utility.Direction(OriginPivot.GetPoint(currentOriginTransform), TargetPivot.GetPoint(currentTargetTransform)), VehicleInstance.transform.up).eulerAngles).magnitude);
		}

		#endregion

		#region Gizmos

		private void OnDrawGizmosSelected()
		{
			if (UnityEditor.Selection.activeGameObject != gameObject)
				return;

			Vector3 gizmosDirection = VehicleInstance.transform.rotation * rotationAxis;

			Gizmos.color = Settings.jointsGizmoColor;

			Gizmos.DrawSphere(OriginPivot.GetPoint(), Settings.gizmosSize * .03125f);
			Gizmos.DrawSphere(TargetPivot.GetPoint(), Settings.gizmosSize * .03125f);
			Gizmos.DrawLine(OriginPivot.GetPoint(), TargetPivot.GetPoint());
			Gizmos.DrawLine(OriginPivot.GetPoint() - gizmosDirection * .125f, OriginPivot.GetPoint() + gizmosDirection * .125f);
			Gizmos.DrawLine(TargetPivot.GetPoint() - gizmosDirection * .125f, TargetPivot.GetPoint() + gizmosDirection * .125f);
		}

		#endregion

		#endregion
	}
}
*/
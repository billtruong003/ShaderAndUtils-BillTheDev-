#region Namespaces

using Unity.Mathematics;
using UnityEngine;
using Utilities;
using MVC.Base;

#endregion

namespace MVC.AI
{
	[AddComponentMenu("")]
	public class VehicleAIZone : VehicleZone
	{
		#region Enumerators

		public enum AIZoneType { Brake, Handbrake, NOS }

		#endregion

		#region Variables

		public VehicleAIPath PathInstance
		{
			get
			{
				if (ContainerInstance && ContainerInstance is VehicleAIPath path)
					return path;

				return null;
			}
		}
		public AIZoneType zoneType;
		public float BrakingIntensity
		{
			get
			{
				return brakingIntensity;
			}
			set
			{
				brakingIntensity = Utility.Clamp01(value);
			}
		}
		public float BrakeSpeedTarget
		{
			get
			{
				return brakeSpeedTarget;
			}
			set
			{
				brakeSpeedTarget = math.max(value, 1f);
			}
		}
		public bool snapBrakeSpeedTarget = true;
		public float HandbrakeSlipTarget
		{
			get
			{
				return handbrakeSlipTarget;
			}
			set
			{
				handbrakeSlipTarget = math.clamp(value, 0f, 1.5f);
			}
		}
		public float MinimumNOSTarget
		{
			get
			{
				return minimumNOSTarget;
			}
			set
			{
				minimumNOSTarget = Utility.Clamp01(value);
			}
		}

		[SerializeField]
		private float brakeSpeedTarget = 50f;
		[SerializeField]
		private float brakingIntensity = 1f;
		[SerializeField]
		private float handbrakeSlipTarget = .75f;
		[SerializeField]
		private float minimumNOSTarget = .5f;

		#endregion

		#region Methods

		public override void OnDrawGizmosSelected()
		{
			Gizmos.color = zoneType == AIZoneType.Brake ? Settings.AIBrakeZoneGizmoColor : zoneType == AIZoneType.Handbrake ? Settings.AIHandbrakeZoneGizmoColor : zoneType == AIZoneType.NOS ? Settings.AINOSZoneGizmoColor : Color.white;

			base.OnDrawGizmosSelected();

			Gizmos.color = Color.white;
		}

		#endregion
	}
}

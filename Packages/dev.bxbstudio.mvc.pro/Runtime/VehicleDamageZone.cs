#region Namespaces

using UnityEngine;

#endregion

namespace MVC.Base
{
	[AddComponentMenu("")]
	public class VehicleDamageZone : VehicleZone
	{
		#region Enumerators

		public enum DamageZoneType { RepairVehicle, Wheel }

		#endregion

		#region Variables

		public DamageZoneType zoneType;

		#endregion

		#region Methods

		public override void OnDrawGizmosSelected()
		{
			Gizmos.color = zoneType == DamageZoneType.Wheel ? Settings.wheelsDamageZoneGizmoColor : Settings.damageRepairZoneGizmoColor;

			base.OnDrawGizmosSelected();

			Gizmos.color = Color.white;
		}

		#endregion
	}
}

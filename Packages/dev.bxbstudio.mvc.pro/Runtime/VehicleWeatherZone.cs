#region Namespaces

using UnityEngine;

#endregion

namespace MVC.Base
{
	[AddComponentMenu("")]
	public class VehicleWeatherZone : VehicleZone
	{
		#region Enumerators

		public enum WeatherZoneType { Darkness, Fog }

		#endregion

		#region Variables

		public WeatherZoneType zoneType;

		#endregion

		#region Methods

		public override void OnDrawGizmosSelected()
		{
			Gizmos.color = zoneType == WeatherZoneType.Fog ? Settings.fogWeatherZoneGizmoColor : Settings.darknessWeatherZoneGizmoColor;

			base.OnDrawGizmosSelected();

			Gizmos.color = Color.white;
		}

		#endregion
	}
}

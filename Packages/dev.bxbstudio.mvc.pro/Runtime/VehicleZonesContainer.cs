#region Namespaces

using UnityEngine;
using MVC.AI;

#endregion

namespace MVC.Base
{
	[AddComponentMenu("Multiversal Vehicle Controller/Misc/Zones Container", 45)]
	[DisallowMultipleComponent]
	public class VehicleZonesContainer : ToolkitBehaviour
	{
		#region Variables

		public int ZonesCount
		{
			get
			{
				int newChildCount = transform.childCount;

				if (childCount != newChildCount)
				{
					zonesCount = GetComponentsInChildren<VehicleZone>(true).Length;
					childCount = newChildCount;
				}

				return zonesCount;
			}
		}

		[SerializeField]
		private int zonesCount;
		private int childCount;

		#endregion

		#region Methods

		public virtual VehicleAIZone AddAIZone(Vector3 position, VehicleAIZone.AIZoneType type)
		{
			VehicleAIZone newZone = AddZone<VehicleAIZone>(position);

			newZone.zoneType = type;

			return newZone;
		}
		public virtual VehicleAudioZone AddAudioZone(Vector3 position, VehicleAudioZone.AudioZoneType type)
		{
			VehicleAudioZone newZone = AddZone<VehicleAudioZone>(position);

			newZone.zoneType = type;

			return newZone;
		}
		public virtual VehicleDamageZone AddDamageZone(Vector3 position, VehicleDamageZone.DamageZoneType type)
		{
			VehicleDamageZone newZone = AddZone<VehicleDamageZone>(position);

			newZone.zoneType = type;

			return newZone;
		}
		public virtual VehicleWeatherZone AddWeatherZone(Vector3 position, VehicleWeatherZone.WeatherZoneType type)
		{
			VehicleWeatherZone newZone = AddZone<VehicleWeatherZone>(position);

			newZone.zoneType = type;

			return newZone;
		}

		private T AddZone<T>(Vector3 position) where T : VehicleZone
		{
			GameObject newZoneObject = new($"{ZonesCount:000}_{typeof(T).Name.Replace("Vehicle", "")}");

			newZoneObject.transform.SetParent(transform);

			newZoneObject.transform.position = position;
			newZoneObject.transform.localRotation = Quaternion.identity;
			newZoneObject.transform.localScale = 10f * Vector3.one;

			return newZoneObject.AddComponent<T>();
		}

		#endregion
	}
}

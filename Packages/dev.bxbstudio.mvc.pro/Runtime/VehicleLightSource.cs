#region Namespaces

using System;
using UnityEngine;
using MVC.Core;
using Utilities;

#endregion

namespace MVC.Base
{
	[AddComponentMenu("")]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(10)]
	public class VehicleLightSource : ToolkitBehaviour
	{
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
		public VehicleChassis ChassisInstance
		{
			get
			{
				if (!chassisInstance)
					chassisInstance = GetComponentInParent<VehicleChassis>();

				return chassisInstance;
			}
		}
		public VehicleLight Light
		{
			get
			{
				if (!light)
					light = Array.Find(VehicleInstance.Lights, light => light.GetLightSource() == this);

				return light;
			}
		}
		public Light Instance
		{
			get
			{
				if (!instance)
					instance = GetComponent<Light>();

				return instance;
			}
		}
		private bool IsOn
		{
			get
			{
				return isOn;
			}
		}

		[SerializeField]
		private Vehicle vehicleInstance;
		[SerializeField]
		private VehicleChassis chassisInstance;
		[SerializeField]
		private Light instance;
#if UNITY_EDITOR
		private new VehicleLight light;
#else
		private VehicleLight light;
#endif
		private float intensity;
		private bool hasHighBeamHeadlights;
		private bool isOn;

		#endregion

		#region Methods

		#region Awake

		public void Restart()
		{
			Awaken = false;

			Initialize();

			if (HasInternalErrors || !VehicleInstance || !ChassisInstance || !Light || !Instance)
				return;

			hasHighBeamHeadlights = Array.Find(VehicleInstance.Lights, l => l.IsHighBeamHeadlight);
			intensity = Instance.intensity;
			Awaken = true;
		}

		private void Initialize()
		{
			if (Instance && intensity != 0f)
				Instance.intensity = intensity;

			intensity = default;
			hasHighBeamHeadlights = default;
		}

		#endregion

		#region Update

		internal void OnUpdate()
		{
			if (!Awaken)
				return;

			State();
			Source();
		}
		private void State()
		{
			isOn = Light.IsOn && (!Light.IsLowBeamHeadlight || !hasHighBeamHeadlights || !VehicleInstance.Stats.isHighBeamHeadlightsOn);
		}
		private void Source()
		{
			switch (Light.technologyType)
			{
				case VehicleLight.Technology.Lamp:
					Instance.intensity = Mathf.Lerp(Instance.intensity, IsOn ? intensity : 0f, Time.deltaTime * Settings.lampLightsIntensityDamping);

					break;

				case VehicleLight.Technology.LED:
					Instance.intensity = IsOn ? intensity : 0f;

					break;
			}

			if (IsOn && !VehicleInstance.IsElectric && VehicleInstance.Stats.isEngineStarting && !VehicleInstance.Stats.isEngineRunning)
				Instance.intensity *= UnityEngine.Random.Range(0f, 1f);

			Instance.enabled = Instance.intensity > 0f;
		}

		#endregion

		#region Gizmos

		private void OnDrawGizmosSelected()
		{
			if (HasInternalErrors || !VehicleInstance || !ChassisInstance)
				return;

			Color orgColor = Gizmos.color;

			for (int i = 0; i < VehicleInstance.Lights.Length; i++)
			{
				if (VehicleInstance.Lights[i].GetLightSource() == this)
					continue;

				if (VehicleInstance.Lights[i].IsHeadlight)
					Gizmos.color = Settings.headlightGizmoColor;
#if !MVC_COMMUNITY
				else if (VehicleInstance.Lights[i].IsInteriorLight)
					Gizmos.color = Settings.interiorLightGizmoColor;
#endif
				else if (VehicleInstance.Lights[i].IsSideSignalLight)
					Gizmos.color = Settings.sideSignalLightGizmoColor;
				else if (VehicleInstance.Lights[i].IsRearLight)
					Gizmos.color = Settings.rearLightGizmoColor;

				Vector3 position = VehicleInstance.Lights[i].GetSourcePosition();

				if (position != Vector3.zero)
					Gizmos.DrawSphere(position, Settings.gizmosSize / 32f);
			}

			Gizmos.color = orgColor;
		}

		#endregion

		#endregion
	}
}

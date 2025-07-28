#region Namespaces

using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
using MVC.Core;
using MVC.Utilities.Internal;

using Object = UnityEngine.Object;

#endregion

namespace MVC.Base
{
	[Serializable]
	public class VehicleLight : ToolkitComponent
	{
		#region Enumerators

		public enum Type
		{
			Headlight,
#if !MVC_COMMUNITY
			InteriorLight,
#endif
			SideSignalLight = 2,
			RearLight
		}
		public enum Side { Left, Right }
		public enum Position { Static, Dynamic }
		public enum Interpolation { Linear, Logarithmic }
		public enum Technology { Lamp, LED }
		public enum EmissionType { None, Color, SwitchGameObjects }
		public enum HeadlightBehaviour { DaytimeRunning, Fog, HighBeam, Indicator, LicensePlate, LowBeam }
		public enum RearLightBehaviour { Brake, DaytimeRunning, Fog, Indicator, LicensePlate, Reverse, Tail }

		#endregion

		#region Variables

		public Vehicle VehicleInstance
		{
			get
			{
				if (!vehicleInstance)
				{
					if (renderer)
						vehicleInstance = renderer.GetComponentInParent<Vehicle>();
					else if (emissiveRenderer)
						vehicleInstance = emissiveRenderer.GetComponentInParent<Vehicle>();
					else if (source)
						vehicleInstance = source.GetComponentInParent<Vehicle>();
				}

				return vehicleInstance;
			}
		}
		public Type type;
		public Side side;
		public Position positionType;
		public Transform DynamicTransform
		{
			get
			{
				return dynamicTransform;
			}
			set
			{
				if (Application.isPlaying || value && !value.IsChildOf(VehicleInstance.Chassis.transform))
					return;

				dynamicTransform = value;
				dynamicTransformParent = value ? value.parent : null;
			}
		}
		public Interpolation positionInterpolationType;
		public Vector3 DynamicLocalPosition
		{
			get
			{
				if (positionType != Position.Dynamic || !DynamicTransform)
					return Vector3.zero;

				return dynamicLocalPosition;
			}
			set
			{
				if (Application.isPlaying || positionType != Position.Dynamic || !DynamicTransform)
					return;

				dynamicLocalPosition = value;
			}
		}
		public Quaternion DynamicLocalRotation
		{
			get
			{
				if (positionType != Position.Dynamic || !DynamicTransform)
					return Quaternion.identity;

				return dynamicLocalRotation;
			}
			set
			{
				if (Application.isPlaying || positionType != Position.Dynamic || !DynamicTransform)
					return;

				dynamicLocalRotation = value;
			}
		}
		public float DynamicInterpolationTime
		{
			get
			{
				return dynamicInterpolationTime;
			}
			set
			{
				dynamicInterpolationTime = math.max(value, .001f);
			}
		}
		public Technology technologyType;
		public EmissionType emission;
		public int behaviour;
		public MeshRenderer renderer;
		public MeshRenderer emissiveRenderer;
		public int materialIndex;
		public Color emissionColor;
		public bool IsHeadlight => type == Type.Headlight;
		public bool IsDaytimeRunningHeadlight => IsHeadlight && behaviour == (int)HeadlightBehaviour.DaytimeRunning;
		public bool IsFogHeadlight => IsHeadlight && behaviour == (int)HeadlightBehaviour.Fog;
		public bool IsHighBeamHeadlight => IsHeadlight && behaviour == (int)HeadlightBehaviour.HighBeam;
		public bool IsIndicatorHeadlight => IsHeadlight && behaviour == (int)HeadlightBehaviour.Indicator;
		public bool IsIndicatorLeftHeadlight => IsIndicatorHeadlight && side == Side.Left;
		public bool IsIndicatorRightHeadlight => IsIndicatorHeadlight && side == Side.Right;
		public bool IsLicensePlateHeadlight => IsHeadlight && behaviour == (int)HeadlightBehaviour.LicensePlate;
		public bool IsLowBeamHeadlight => IsHeadlight && behaviour == (int)HeadlightBehaviour.LowBeam;
#if !MVC_COMMUNITY
		public bool IsInteriorLight => type == Type.InteriorLight;
#endif
		public bool IsSideSignalLight => type == Type.SideSignalLight;
		public bool IsSideSignalLeftLight => IsSideSignalLight && side == Side.Left;
		public bool IsSideSignalRightLight => IsSideSignalLight && side == Side.Right;
		public bool IsRearLight => type == Type.RearLight;
		public bool IsBrakeRearLight => IsRearLight && behaviour == (int)RearLightBehaviour.Brake;
		public bool IsDaytimeRunningRearLight => IsRearLight && behaviour == (int)RearLightBehaviour.DaytimeRunning;
		public bool IsFogRearLight => IsRearLight && behaviour == (int)RearLightBehaviour.Fog;
		public bool IsIndicatorRearLight => IsRearLight && behaviour == (int)RearLightBehaviour.Indicator;
		public bool IsIndicatorLeftRearLight => IsIndicatorRearLight && side == Side.Left;
		public bool IsIndicatorRightRearLight => IsIndicatorRearLight && side == Side.Right;
		public bool IsLicensePlateRearLight => IsRearLight && behaviour == (int)RearLightBehaviour.LicensePlate;
		public bool IsReverseRearLight => IsRearLight && behaviour == (int)RearLightBehaviour.Reverse;
		public bool IsTailRearLight => IsRearLight && behaviour == (int)RearLightBehaviour.Tail;
		public bool IsOn
		{
			get
			{
				Update();

				return isOn;
			}
			private set
			{
				isOn = value;
			}
		}

		internal bool Awaken { get; private set; }

		[SerializeField]
		private Vehicle vehicleInstance;
		private VehicleTrailer trailerInstance;
		[SerializeField]
		private VehicleLightSource source;
		[SerializeField]
		private Transform dynamicTransform;
		[SerializeField]
		private Vector3 dynamicLocalPosition;
		[SerializeField]
		private Quaternion dynamicLocalRotation;
		[SerializeField]
		private float dynamicInterpolationTime = 1f;
		[SerializeField]
		private Transform dynamicTransformParent;
		[NonSerialized]
		private List<VehicleLight> lightsWithCommonDynamicTransform;
		[NonSerialized]
		private List<VehicleLight> lightsWithCommonRenderer;
		[NonSerialized]
		private List<VehicleLight> lightsInferiorWithCommonRenderer;
		[NonSerialized]
		private List<VehicleLight> lightsWithCommonEmissiveRenderer;
		private Vector3 orgLocalPosition;
		private Quaternion orgLocalRotation;
		private string materialEmissionColorPropertyName;
		private float dynamicMovementTimer;
		private float dynamicActiveFactor;
		private float currentActiveFactor;
		private float maxDynamicActiveFactor;
		private bool isOn;
		private int indexOfLight;
		private int lastUpdateFrame;

		#endregion

		#region Methods

		#region Awake

		public void Restart()
		{
			Awaken = false;

			Initialize();

			if (HasInternalErrors || !VehicleInstance)
				return;

			if (VehicleInstance.IsTrailer)
				trailerInstance = VehicleInstance as VehicleTrailer;

			if (DynamicTransform)
			{
				orgLocalPosition = DynamicTransform.localPosition;
				orgLocalRotation = DynamicTransform.localRotation;

				if (DynamicLocalPosition == Vector3.zero)
					DynamicLocalPosition = orgLocalPosition;

				if (DynamicLocalRotation == Quaternion.identity)
					DynamicLocalRotation = orgLocalRotation;
			}

			indexOfLight = Array.IndexOf(VehicleInstance.Lights, this);
			lightsWithCommonDynamicTransform = new();

			if (DynamicTransform && indexOfLight > 0)
				for (int i = 0; i < VehicleInstance.Lights.Length; i++)
					if (i != indexOfLight && DynamicTransform == VehicleInstance.Lights[i].DynamicTransform)
						lightsWithCommonDynamicTransform.Add(VehicleInstance.Lights[i]);

			lightsWithCommonRenderer = new();
			lightsInferiorWithCommonRenderer = new();
			lightsWithCommonEmissiveRenderer = new();

			if (renderer)
				renderer.gameObject.SetActive(true);

			if (indexOfLight > 0)
				for (int i = 0; i < indexOfLight; i++)
				{
					if (renderer && renderer == VehicleInstance.Lights[i].renderer && (emission == EmissionType.SwitchGameObjects || emission == EmissionType.Color && materialIndex == VehicleInstance.Lights[i].materialIndex))
						lightsWithCommonRenderer.Add(VehicleInstance.Lights[i]);

					if (emissiveRenderer && emissiveRenderer == VehicleInstance.Lights[i].emissiveRenderer && emission == EmissionType.SwitchGameObjects)
						lightsWithCommonEmissiveRenderer.Add(VehicleInstance.Lights[i]);
				}

			if (renderer && indexOfLight + 1 < VehicleInstance.Lights.Length)
				for (int i = indexOfLight + 1; i < VehicleInstance.Lights.Length; i++)
					if (renderer == VehicleInstance.Lights[i].renderer && emission == EmissionType.Color && materialIndex == VehicleInstance.Lights[i].materialIndex)
						lightsInferiorWithCommonRenderer.Add(VehicleInstance.Lights[i]);

			if (emission == EmissionType.SwitchGameObjects)
			{
				if (renderer)
					renderer.enabled = true;

				if (emissiveRenderer)
				{
					emissiveRenderer.gameObject.SetActive(true);

					emissiveRenderer.enabled = false;
				}
				else
					ToolkitDebug.Warning($"A {GetTypeName()} doesn't have an emissive renderer as the `Emission` value is set to \"Switch Game Objects\".");
			}
			else if (renderer)
			{
				materialIndex = Mathf.Clamp(materialIndex, 0, renderer.sharedMaterials.Length - 1);

				if (emissionColor.maxColorComponent <= .5f)
					ToolkitDebug.Log($"The emission color of a \"{GetTypeName()}\" is not powerful enough to illuminate, emission might not take place as it should.");
			}

			if (source)
				source.Restart();

			Awaken = true;
		}

		private void Initialize()
		{
			trailerInstance = null;
			lightsWithCommonRenderer = null;
			lightsInferiorWithCommonRenderer = null;
			lightsWithCommonEmissiveRenderer = null;
			materialEmissionColorPropertyName = default;
			IsOn = default;
			indexOfLight = -1;

			if (DynamicTransform)
			{
				if (orgLocalPosition != Vector3.zero)
					DynamicTransform.localPosition = orgLocalPosition;

				orgLocalPosition = Vector3.zero;

				if (orgLocalRotation != Quaternion.identity)
					DynamicTransform.localRotation = orgLocalRotation;

				orgLocalRotation = Quaternion.identity;
			}

			if (source && source.enabled)
				source.enabled = default;
		}

		#endregion

		#region Utilities

		public string GetTypeName()
		{
			string name = "";

			switch (type)
			{
				case Type.Headlight:
					name = "Headlight";

					break;

#if !MVC_COMMUNITY
				case Type.InteriorLight:
					name = "Interior Light";

					break;

#endif
				case Type.SideSignalLight:
					name = "Side Signal Light";

					break;

				case Type.RearLight:
					name = "Rear Light";

					break;
			}

			return name;
		}
		public string GetName()
		{
			string name = GetTypeName();

			switch (type)
			{
				case Type.Headlight:
					if (IsDaytimeRunningHeadlight)
						name += " Daytime Running";
					else if (IsFogHeadlight)
						name += " Fog";
					else if (IsHighBeamHeadlight)
						name += " High Beam";
					else if (IsIndicatorHeadlight)
					{
						name += " Indicator";

						if (IsIndicatorLeftHeadlight)
							name += " Left";
						else if (IsIndicatorRightHeadlight)
							name += " Right";
					}
					else if (IsLicensePlateHeadlight)
						name += " License Plate";
					else if (IsLowBeamHeadlight)
						name += " Low Beam";

					break;

				case Type.SideSignalLight:
					if (IsSideSignalLeftLight)
						name += " Left";
					else if (IsSideSignalRightLight)
						name += " Right";

					break;

				case Type.RearLight:
					if (IsBrakeRearLight)
						name += " Brake";
					else if (IsDaytimeRunningRearLight)
						name += " Daytime Running";
					else if (IsFogRearLight)
						name += " Fog";
					else if (IsIndicatorRearLight)
					{
						name += " Indicator";

						if (IsIndicatorLeftRearLight)
							name += " Left";
						else if (IsIndicatorRightRearLight)
							name += " Right";
					}
					else if (IsLicensePlateRearLight)
						name += " License Plate";
					else if (IsReverseRearLight)
						name += " Reverse";
					else if (IsTailRearLight)
						name += " Tail";

					break;
			}

			return name;
		}
		public string GetLightSourceName()
		{
			string name = GetName();

			while (name.IndexOf(' ') > -1)
				name = name.Replace(' ', '_');

			return name;
		}
		public VehicleLightSource AddLightSource()
		{
			if (!VehicleInstance || !VehicleInstance.Chassis)
				return null;

			Transform parent = VehicleInstance.Chassis.transform.Find("LightSources");

			if (!parent)
			{
				parent = new GameObject("LightSources").transform;

				parent.SetParent(VehicleInstance.Chassis.transform, false);
			}

			if (Settings.useHideFlags)
				parent.hideFlags = HideFlags.HideInHierarchy;

			source = new GameObject(GetLightSourceName()).AddComponent<VehicleLightSource>();

			source.gameObject.AddComponent<Light>();
			source.transform.SetParent(parent, false);

			Vector3 position = new(0f, .5f, 2f);
			Vector3 rotation = Vector3.zero;

			if (IsRearLight)
			{
				position.z *= -1f;
				rotation.y = 180f;

				if (IsIndicatorRearLight)
				{
					position.x = 1f;

					if (IsIndicatorLeftRearLight)
						position.x *= -1f;
				}

				if (IsLicensePlateRearLight)
				{
					position.y *= .5f;
					rotation.x = 90f;
				}
			}
			else if (IsSideSignalLight)
			{
				position.x = 1f;
				position.z = 0f;
				rotation.y = 90f;

				if (IsSideSignalLeftLight)
				{
					position.x *= -1f;
					rotation.y *= -1f;
				}
			}
#if !MVC_COMMUNITY
			else if (IsInteriorLight)
				position.z = 0f;
#endif
			else if (IsHeadlight)
			{
				if (IsIndicatorHeadlight)
				{
					position.x = 1f;

					if (IsIndicatorLeftHeadlight)
						position.x *= -1f;
				}

				if (IsLicensePlateHeadlight)
				{
					position.y *= .5f;
					rotation.x = 90f;
				}
			}

			source.transform.localPosition = position;
			source.transform.localEulerAngles = rotation;
			source.Instance.type = LightType.Spot;

			VehicleInstance.RefreshLayersAndTags();

			return source;
		}
		public VehicleLightSource GetLightSource()
		{
			return source;
		}
		public void RemoveLightSource()
		{
			if (!source)
				return;

			if (Application.isPlaying)
				Object.Destroy(source.gameObject);
			else
				Object.DestroyImmediate(source.gameObject);
		}
		public Vector3 GetSourcePosition()
		{
			if (GetLightSource())
				return GetLightSource().transform.position;

			return default;
		}
		public string GetMaterialEmissionColorPropertyName()
		{
			if (!renderer || materialIndex < 0 || materialIndex >= renderer.sharedMaterials.Length || !renderer.sharedMaterials[materialIndex] || !renderer.sharedMaterials[materialIndex].shader)
				return string.Empty;

			if (materialEmissionColorPropertyName.IsNullOrEmpty())
			{
				if (renderer.sharedMaterials[materialIndex].shader.name.StartsWith("HDRP/Lit"))
					materialEmissionColorPropertyName = "_EmissiveColor";
				else if (renderer.sharedMaterials[materialIndex].shader.name.StartsWith("Universal Render Pipeline/Lit"))
					materialEmissionColorPropertyName = "_EmissionColor";
				else if (renderer.sharedMaterials[materialIndex].shader.name.StartsWith("Standard"))
					materialEmissionColorPropertyName = "_EmissionColor";
				else if (renderer.sharedMaterials[materialIndex].shader.name.StartsWith("Particles/Standard"))
					materialEmissionColorPropertyName = "_Color";
				else if (!HasInternalErrors)
					materialEmissionColorPropertyName = Settings.customLightShaderEmissionColorProperty;
			}

			return materialEmissionColorPropertyName;
		}
		public void RefreshMaterialEmissionColorPropertyName()
		{
			materialEmissionColorPropertyName = string.Empty;
		}
		public void UpdateParent()
		{
			if (!DynamicTransform)
				return;

			Transform newParent = DynamicTransform.parent;

			if (!newParent || dynamicTransformParent == newParent)
				return;

			if (dynamicTransformParent)
			{
				if (DynamicLocalPosition != Vector3.zero)
					DynamicLocalPosition = newParent.InverseTransformPoint(dynamicTransformParent.TransformPoint(DynamicLocalPosition));

				if (DynamicLocalRotation != Quaternion.identity)
					DynamicLocalRotation = Quaternion.Euler(newParent.InverseTransformVector(dynamicTransformParent.TransformVector(DynamicLocalRotation.eulerAngles)));
			}

			dynamicTransformParent = newParent;
		}

		#endregion

		#region Update

		internal void Update()
		{
			if (!Awaken || lastUpdateFrame == Time.frameCount)
				return;

			lastUpdateFrame = Time.frameCount;

			State();
			PositionAndRotation();
			Emission();
			Source();
		}

		private void State()
		{
			IsOn = false;

			bool isEngineRunning = VehicleInstance.Stats.isEngineRunning || VehicleInstance.Stats.isEngineStarting || VehicleInstance.Stats.isEngineStall;
			bool isLightsOn = ToolkitBehaviour.Manager.nightMode && (!trailerInstance || trailerInstance.Joint.ConnectedVehicle) || VehicleInstance.Stats.isLightsOn || VehicleInstance.Stats.inDarknessWeatherZone;

			switch (type)
			{
				case Type.Headlight:
					if (IsIndicatorLeftHeadlight && VehicleInstance.Stats.isSignalLeftLightsOn)
						IsOn = true;
					else if (IsIndicatorRightHeadlight && VehicleInstance.Stats.isSignalRightLightsOn)
						IsOn = true;
					else if (IsFogHeadlight && (ToolkitBehaviour.Manager.fogTime || VehicleInstance.Stats.inFogWeatherZone) && (isEngineRunning || isLightsOn))
						IsOn = true;
					else if (IsHighBeamHeadlight && VehicleInstance.Stats.isHighBeamHeadlightsOn && (isEngineRunning || isLightsOn))
						IsOn = true;
					else if (IsLicensePlateHeadlight && isLightsOn)
						IsOn = true;
					else if (IsDaytimeRunningHeadlight && (isEngineRunning || isLightsOn || Manager.fogTime || VehicleInstance.Stats.inFogWeatherZone))
						IsOn = true;
					else if (IsLowBeamHeadlight && isLightsOn)
						IsOn = true;

					break;

#if !MVC_COMMUNITY
				case Type.InteriorLight:
					if (VehicleInstance.Stats.isInteriorLightsOn)
						IsOn = true;

					break;
#endif

				case Type.SideSignalLight:
					if (IsSideSignalLeftLight && VehicleInstance.Stats.isSignalLeftLightsOn)
						IsOn = true;
					else if (IsSideSignalRightLight && VehicleInstance.Stats.isSignalRightLightsOn)
						IsOn = true;

					break;

				case Type.RearLight:
					if (IsReverseRearLight && VehicleInstance.Stats.isReversing && (isEngineRunning || isLightsOn))
						IsOn = true;
					else if (IsIndicatorLeftRearLight && VehicleInstance.Stats.isSignalLeftLightsOn)
						IsOn = true;
					else if (IsIndicatorRightRearLight && VehicleInstance.Stats.isSignalRightLightsOn)
						IsOn = true;
					else if (IsBrakeRearLight && VehicleInstance.Inputs.Brake > Utility.BoolToNumber(VehicleInstance.IsAI) * .25f && (isEngineRunning || isLightsOn))
						IsOn = true;
					else if (IsFogRearLight && (ToolkitBehaviour.Manager.fogTime || VehicleInstance.Stats.inFogWeatherZone) && (isEngineRunning || isLightsOn))
						IsOn = true;
					else if (IsLicensePlateRearLight && isLightsOn)
						IsOn = true;
					else if (IsDaytimeRunningRearLight && (isEngineRunning || isLightsOn))
						IsOn = true;
					else if (IsTailRearLight && (isLightsOn || VehicleInstance.Stats.inFogWeatherZone))
						IsOn = true;

					break;
			}
		}
		private void PositionAndRotation()
		{
			if (!DynamicTransform || positionType == Position.Static)
				return;

			switch (positionType)
			{
				case Position.Dynamic:
					switch (positionInterpolationType)
					{
						case Interpolation.Logarithmic:
							dynamicMovementTimer = Mathf.Lerp(dynamicMovementTimer, Utility.BoolToNumber(IsOn), Time.deltaTime * DynamicInterpolationTime);

							break;

						default:
							if (IsOn && dynamicMovementTimer < DynamicInterpolationTime)
								dynamicMovementTimer += Time.deltaTime;
							else if (!IsOn && dynamicMovementTimer > 0f)
								dynamicMovementTimer -= Time.deltaTime;

							if (dynamicMovementTimer < 0f || dynamicMovementTimer > DynamicInterpolationTime)
								dynamicMovementTimer = Mathf.Clamp(dynamicMovementTimer, 0f, DynamicInterpolationTime);

							break;
					}

					break;
			}

			dynamicActiveFactor = dynamicMovementTimer / DynamicInterpolationTime;
			maxDynamicActiveFactor = lightsWithCommonDynamicTransform.Count > 0f ? Mathf.Max(dynamicActiveFactor, lightsWithCommonDynamicTransform.Max(light => light.dynamicActiveFactor)) : dynamicActiveFactor;

			if (dynamicActiveFactor >= maxDynamicActiveFactor && currentActiveFactor != dynamicActiveFactor)
				DynamicTransform.SetLocalPositionAndRotation(Vector3.Lerp(orgLocalPosition, DynamicLocalPosition, dynamicActiveFactor), Quaternion.Lerp(orgLocalRotation, DynamicLocalRotation, dynamicActiveFactor));

			currentActiveFactor = dynamicActiveFactor;
			IsOn = IsOn && maxDynamicActiveFactor >= .95f;
		}
		private void Emission()
		{
			if (!renderer && emission == EmissionType.Color || !emissiveRenderer && emission == EmissionType.SwitchGameObjects)
				return;

			if (emission == EmissionType.SwitchGameObjects)
			{
				if (!emissiveRenderer)
					return;

				if (renderer)
					renderer.enabled = !IsOn && !lightsWithCommonRenderer.Find(light => light.IsOn);

				emissiveRenderer.enabled = IsOn || lightsWithCommonEmissiveRenderer.Find(light => light.IsOn);
			}
			else if (emission == EmissionType.Color)
			{
				if (lightsWithCommonRenderer.Find(light => light.IsOn) || lightsInferiorWithCommonRenderer.Find(light => light.IsOn) && !IsOn)
					return;

				Color targetEmission = emissionColor * Utility.BoolToNumber(IsOn);
				Color currentEmission = renderer.materials[materialIndex].GetColor(GetMaterialEmissionColorPropertyName());

				if (!VehicleInstance.IsElectric && VehicleInstance.Stats.isEngineStarting && !VehicleInstance.Stats.isEngineRunning)
					targetEmission *= UnityEngine.Random.Range(0f, 1f);

				switch (technologyType)
				{
					case Technology.Lamp:
						currentEmission = Color.Lerp(currentEmission, targetEmission, Time.deltaTime * Settings.lampLightsIntensityDamping);

						break;

					case Technology.LED:
						currentEmission = targetEmission;

						break;
				}

				renderer.materials[materialIndex].SetColor(GetMaterialEmissionColorPropertyName(), currentEmission);
			}
		}
		private void Source()
		{
			if (!source)
				return;

			source.OnUpdate();
		}

		#endregion

		#endregion

		#region Constructors

		public VehicleLight(Vehicle vehicle)
		{
			vehicleInstance = vehicle;
		}
		public VehicleLight(VehicleLight instance)
		{
			vehicleInstance = instance.vehicleInstance;
			technologyType = instance.technologyType;
			type = instance.type;
			side = instance.side;
			emission = instance.emission;
			behaviour = instance.behaviour;
			renderer = instance.renderer;
			materialIndex = instance.materialIndex;
			emissionColor = instance.emissionColor;

			if (instance.source)
			{
				source = Object.Instantiate(instance.source, instance.source.transform.position, instance.source.transform.rotation, instance.source.transform.parent);
				source.name = GetLightSourceName();
			}
		}

		#endregion

		#region Operators

		public static implicit operator bool(VehicleLight light) => light != null;

		#endregion
	}
}

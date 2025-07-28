#region Namespaces

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Utilities;
using Utilities.Inputs;
using MVC.AI;
using MVC.Core;
using MVC.Base;
#if !MVC_COMMUNITY
using MVC.Internal;
#endif
using MVC.Utilities;
using MVC.Utilities.Internal;

using Input = Utilities.Inputs.Input;

#endregion

namespace MVC
{
	[Serializable]
	[CreateAssetMenu(fileName ="New MVC Settings Asset", menuName = "MVC/Settings Asset", order = 0)]
	public class ToolkitSettings : ToolkitScriptableObject
	{
		#region Modules & Enumerators

		#region Enumerators

		#region Editor Enumerators

		public enum SettingsEditorFoldout
		{
			None,
			General,
#if !MVC_COMMUNITY
			AI,
#endif
			Behaviour = 3,
			Damage,
			Cameras,
			PlayerInputs,
			EnginesChargers,
			TireCompounds,
			Grounds,
			SFX,
			VFX,
			Editor,
			License
		}
		public enum SettingsEditorSFXFoldout { None, Impacts, Gears, Exhaust, /*Exterior, */NOS, /*Horns, */Transmission }
		public enum EditorSpringTargetMeasurement { Percentage, Length }

		#endregion

		#region Global Enumerators

		public enum PhysicsType { WheelCollider }
		public enum TransmissionType { Manual, Automatic }
		public enum EngineStartMode { Always, AnyKeyPress, EngineStartKey }
		public enum WheelRendererRefreshType { Approximate, Accurate }
		public enum InputSystem { InputsManager, UnityLegacyInputManager }
		public enum GamepadRumbleType { Off, FollowCamera, Independent }
		[Flags]
		public enum GamepadRumbleMask { Nothing = 0, Speed = 1, OffRoad = 2, NOS = 4, SidewaysSlip = 8, ForwardSlip = 16, BrakeSlip = 32 }
		public enum AIChasingDetectionMode { Collision, Distance, Visibility }
		public enum SteeringInterpolation { Instant, Linear, Exponential }
		public enum VFXOverrideMode { ParticleSystem, Material }
		public enum CounterSteerType { PhysicsAndVisuals, VisualsOnly }

		#endregion

		#endregion

		#region Modules

		[Serializable]
		public class ImpactClips
		{
			#region Variables

			public LevelledClipsGroup cars = new();
			public LevelledClipsGroup walls = new();
			public AudioClip[] glassCrack = new AudioClip[] { };
			public AudioClip[] glassShatter = new AudioClip[] { };
			public AudioClip[] metalFences = new AudioClip[] { };
			public AudioClip[] roadCones = new AudioClip[] { };
			public AudioClip[] roadMisc = new AudioClip[] { };
			public AudioClip[] roadSigns = new AudioClip[] { };
			public float ClipsVolume
			{
				get
				{
					return clipsVolume;
				}
				set
				{
					clipsVolume = Mathf.Clamp01(value);
				}
			}

			[SerializeField]
			private float clipsVolume = 1f;

			#endregion

			#region Operators

			public static implicit operator bool(ImpactClips clips) => clips != null;

			#endregion
		}
		[Serializable]
		public class GearShiftingClips
		{
			#region Variables

			public AudioClip[] up = new AudioClip[] { };
			public AudioClip[] down = new AudioClip[] { };
			public float Volume
			{
				get
				{
					return volume;
				}
				set
				{
					volume = Mathf.Clamp01(value);
				}
			}

			[SerializeField]
			private float volume = 1f;

			#endregion

			#region Operators

			public static implicit operator bool(GearShiftingClips clips) => clips != null;

			#endregion
		}
		[Serializable]
		public class ExhaustBlowoutClips
		{
			#region Variables

			public ExhaustsBlowoutClipsGroup americanMuscle = new();
			public ExhaustsBlowoutClipsGroup eliteCars = new();
			public ExhaustsBlowoutClipsGroup f1Cars = new();
			public ExhaustsBlowoutClipsGroup roadCars = new();
			public ExhaustsBlowoutClipsGroup superCars = new();
			public ExhaustsBlowoutClipsGroup roadHeavyTrucks = new();
			public ExhaustsBlowoutClipsGroup sportHeavyTrucks = new();

			#endregion

			#region Methods

			public static ExhaustsBlowoutClipsGroup GetGroup(Vehicle vehicle)
			{
				if (!vehicle.Behaviour)
					return null;

				switch (vehicle.Behaviour.vehicleType)
				{
					case Vehicle.VehicleType.Car:
						switch (vehicle.Behaviour.carClass)
						{
							case Vehicle.CarClass.AmericanMuscle:
								return instance.exhaustClips.americanMuscle;

							case Vehicle.CarClass.Elite:
								return instance.exhaustClips.eliteCars;

							case Vehicle.CarClass.F1:
								return instance.exhaustClips.f1Cars;

							case Vehicle.CarClass.Road:
								return instance.exhaustClips.roadCars;

							case Vehicle.CarClass.Super:
								return instance.exhaustClips.superCars;
						}

						break;

					case Vehicle.VehicleType.HeavyTruck:
						switch (vehicle.Behaviour.heavyTruckClass)
						{
							case Vehicle.HeavyTruckClass.Road:
								return instance.exhaustClips.roadHeavyTrucks;

							case Vehicle.HeavyTruckClass.Sport:
								return instance.exhaustClips.sportHeavyTrucks;
						}

						break;
				}

				return null;
			}

			#endregion
		}
		[Serializable]
		public class ExhaustsBlowoutClipsGroup
		{
			#region Variables

			public AudioClip[] gurgle = new AudioClip[] { };
			public AudioClip[] pop = new AudioClip[] { };
			public float Volume
			{
				get
				{
					return volume;
				}
				set
				{
					volume = Mathf.Clamp01(value);
				}
			}

			[SerializeField]
			private float volume = 1f;

			#endregion

			#region Operators

			public static implicit operator bool(ExhaustsBlowoutClipsGroup group) => group != null;

			#endregion
		}
		/*[Serializable]
		public class ExteriorPartsClips
		{
			public ExteriorPartsClipsGroup doors = new();
			public ExteriorPartsClipsGroup hood = new();
			public ExteriorPartsClipsGroup trunk = new();
		}
		[Serializable]
		public class ExteriorPartsClipsGroup
		{
			public AudioClip[] open = new AudioClip[] { };
			public AudioClip[] close = new AudioClip[] { };
			public float Volume
			{
				get
				{
					return volume;
				}
				set
				{
					volume = Mathf.Clamp01(value);
				}
			}

			[SerializeField]
			private float volume = 1f;
		}*/
		[Serializable]
		public class LevelledClipsGroup
		{
			#region Variables

			public AudioClip[] low = new AudioClip[] { };
			public AudioClip[] medium = new AudioClip[] { };
			public AudioClip[] high = new AudioClip[] { };
			public float Volume
			{
				get
				{
					return volume;
				}
				set
				{
					volume = Mathf.Clamp01(value);
				}
			}

			[SerializeField]
			private float volume = 1f;

			#endregion

			#region Operators

			public static implicit operator bool(LevelledClipsGroup group) => group != null;

			#endregion
		}
		[Serializable]
		public class NOSClipsGroup
		{
			#region Variables

			public AudioClip[] starting = new AudioClip[] { };
			public AudioClip[] active = new AudioClip[] { };
			public float Volume
			{
				get
				{
					return volume;
				}
				set
				{
					volume = Mathf.Clamp01(value);
				}
			}

			[SerializeField]
			private float volume = 1f;

			#endregion

			#region Operators

			public static implicit operator bool(NOSClipsGroup group) => group != null;

			#endregion
		}
		[Serializable]
		public class TransmissionWhineClipsGroup
		{
			#region Variables

			public string name;
			public TransmissionWhineClip[] clips = new TransmissionWhineClip[] { };
			public AudioMixerGroup mixer;
			public float DecelerationLowPassFrequency
			{
				get
				{
					return decelerationLowPassFrequency;
				}
				set
				{
					decelerationLowPassFrequency = Mathf.Clamp(value, 10f, 22000f);
				}
			}
			public float ClutchVolume
			{
				get
				{
					return clutchVolume;
				}
				set
				{
					clutchVolume = Mathf.Clamp01(value);
				}
			}
			public float Volume
			{
				get
				{
					return volume;
				}
				set
				{
					volume = Mathf.Clamp01(value);
				}
			}

			[SerializeField]
			private float decelerationLowPassFrequency = 22000f;
			[SerializeField]
			private float clutchVolume = .5f;
			[SerializeField]
			private float volume = 1f;

			#endregion

			#region Constructors

			public TransmissionWhineClipsGroup() { }
			public TransmissionWhineClipsGroup(string name)
			{
				this.name = name;
			}
			public TransmissionWhineClipsGroup(string name, TransmissionWhineClip[] clips, AudioMixerGroup mixer = null, float lowPassFreq = 22000f)
			{
				this.name = name;
				this.clips = clips;
				this.mixer = mixer;
				DecelerationLowPassFrequency = lowPassFreq;
			}
			public TransmissionWhineClipsGroup(TransmissionWhineClipsGroup group)
			{
				name = group.name;
				clips = group.clips;
				mixer = group.mixer;
				decelerationLowPassFrequency = group.DecelerationLowPassFrequency;
				clutchVolume = group.ClutchVolume;
				volume = group.Volume;
			}

			#endregion
		}
		[Serializable]
		public class TransmissionWhineClip
		{
			public AudioClip clip;
			public float Speed
			{
				get
				{
					return speed;
				}
				set
				{
					speed = math.max(value, 1f);
				}
			}

			[SerializeField]
			private float speed;
		}
		/*[Serializable]
		public class ChassisPartsJointsGroup
		{
			#region Variables

			public VehicleChassisPart.JointsGroup bumper = new();
			public VehicleChassisPart.JointsGroup fender = new();
			public VehicleChassisPart.JointsGroup hood = new();
			public VehicleChassisPart.JointsGroup door = new();
			public VehicleChassisPart.JointsGroup doorMirror = new();
			public VehicleChassisPart.JointsGroup sideSkirt = new();
			public VehicleChassisPart.JointsGroup exhaust = new();
			public VehicleChassisPart.JointsGroup trunk = new();
			public VehicleChassisPart.JointsGroup wing = new();

			#endregion

			#region Methods

			public VehicleChassisPart.JointsGroup GetJoints(VehicleChassisPart.PartType partType)
			{
				switch (partType)
				{
					case VehicleChassisPart.PartType.Bumper:
						return bumper;

					case VehicleChassisPart.PartType.Door:
						return door;

					case VehicleChassisPart.PartType.DoorMirror:
						return doorMirror;

					case VehicleChassisPart.PartType.Fender:
						return fender;

					case VehicleChassisPart.PartType.Hood:
						return hood;

					case VehicleChassisPart.PartType.SideSkirt:
						return sideSkirt;

					case VehicleChassisPart.PartType.Wing:
						return wing;

					case VehicleChassisPart.PartType.Trunk:
						return trunk;

					default:
						return new();
				}
			}

			#endregion
		}*/
		public readonly struct ProblemsSheet
		{
			#region Variables

			public bool HasGeneralProblems
			{
				get
				{
					if (!IsSheetValid)
						return false;

					if (!InputsManager.DataAssetExists)
					{
						ToolkitDebug.Error("The Inputs Manager data asset has been removed or doesn't exist.");

						return true;
					}

					Input startEngineInput;

					try
					{
						startEngineInput = InputsManager.GetInput(settings.engineStartSwitchInput);
					}
					catch (Exception e)
					{
						ToolkitDebug.Error(e);

						startEngineInput = null;
					}

					if (!startEngineInput)
						return true;

					Key startEngineKey = default;

					if (startEngineInput.Main.Positive != default)
						startEngineKey = startEngineInput.Main.Positive;
					else if (startEngineInput.Main.Negative != default)
						startEngineKey = startEngineInput.Main.Negative;
					else if (startEngineInput.Alt.Positive != default)
						startEngineKey = startEngineInput.Alt.Negative;
					else if (startEngineInput.Alt.Negative != default)
						startEngineKey = startEngineInput.Alt.Negative;

					GamepadBinding startEngineButton = default;

					if (startEngineKey == default)
					{
						if (startEngineInput.Main.GamepadPositive != default)
							startEngineButton = startEngineInput.Main.GamepadPositive;
						else if (startEngineInput.Main.GamepadNegative != default)
							startEngineButton = startEngineInput.Main.GamepadNegative;
						else if (startEngineInput.Alt.GamepadPositive != default)
							startEngineButton = startEngineInput.Alt.GamepadNegative;
						else if (startEngineInput.Alt.GamepadNegative != default)
							startEngineButton = startEngineInput.Alt.GamepadNegative;
					}

					if (startEngineKey == default && startEngineButton == default)
						return true;

					if (settings.vehiclesLayer == default || settings.vehicleWheelsLayer == default || settings.vehiclesLayer == settings.vehicleWheelsLayer)
						return true;

					return false;
				}
			}
			public bool HasAIProblems
			{
				get
				{
					return false;
				}
			}
			public bool HasBehaviourProblems
			{
				get
				{
					return false;
				}
			}
			public bool HasDamageProblems
			{
				get
				{
					return false;
				}
			}
			public bool HasCamerasProblems
			{
				get
				{
					return false;
				}
			}
			public bool HasPlayerInputsProblems
			{
				get
				{
					List<string> inputs = new()
					{
						Settings.steerInput,
						Settings.fuelInput,
						Settings.brakeInput,
						Settings.clutchInput,
						Settings.handbrakeInput,
						Settings.gearShiftUpButtonInput,
						Settings.gearShiftDownButtonInput,
						Settings.engineStartSwitchInput,
						Settings.NOSButtonInput,
						Settings.launchControlSwitchInput,
						Settings.resetButtonInput,
						Settings.changeCameraButtonInput,
						Settings.forwardCameraViewInput,
						Settings.sidewaysCameraViewInput,
						Settings.lightSwitchInput,
						Settings.highBeamLightSwitchInput,
						Settings.interiorLightSwitchInput,
						Settings.sideSignalLeftLightSwitchInput,
						Settings.sideSignalRightLightSwitchInput,
						Settings.hazardLightsSwitchInput,
						Settings.trailerLinkSwitchInput
					};

					for (int i = 0; i < inputs.Count; i++)
						for (int j = 0; j < inputs.Count; j++)
							if (i != j && inputs[i] == inputs[j])
								return true;

					return false;
				}
			}
			public bool HasEnginesProblems
			{
				get
				{
					return false;
				}
			}
			public bool HasChargersProblems
			{
				get
				{
					return false;
				}
			}
			public bool HasGroundsProblems
			{
				get
				{
					return false;
				}
			}
			public bool HasSFXProblems
			{
				get
				{
					return false;
				}
			}
			public bool HasVFXProblems
			{
				get
				{
					if (!IsSheetValid)
						return false;

					if (settings.useParticleSystems)
						if (settings.exhaustFlame && (settings.NOSFlameOverrideMode == VFXOverrideMode.Material && !settings.NOSFlameMaterial || settings.NOSFlameOverrideMode != VFXOverrideMode.Material && !settings.NOSFlame))
							return true;

					if (settings.useAIPathVisualizer)
						if (!settings.pathVisualizerMaterial)
							return true;

					return false;
				}
			}
			public bool HasProblems
			{
				get
				{
					return HasGeneralProblems || HasAIProblems || HasBehaviourProblems || HasDamageProblems || HasCamerasProblems || HasPlayerInputsProblems || HasEnginesProblems || HasChargersProblems || HasGroundsProblems || HasSFXProblems || HasVFXProblems;
				}
			}
			public bool IsSheetValid
			{
				get
				{
					return settings;
				}
			}

			private readonly ToolkitSettings settings;

			#endregion

			#region Methods

			#region Virtual Methods

			public override readonly bool Equals(object obj)
			{
				return obj is ProblemsSheet sheet &&
					   EqualityComparer<ToolkitSettings>.Default.Equals(settings, sheet.settings);
			}
			public override readonly int GetHashCode()
			{
				return -779396024 + EqualityComparer<ToolkitSettings>.Default.GetHashCode(settings);
			}

			#endregion

			#region Utilities

			public bool DisableToolkitBehaviourOnProblems(SettingsEditorFoldout foldout, ToolkitBehaviour behaviour)
			{
				if (SettingsHaveProblems(foldout))
				{
					ToolkitBehaviour[] behaviours = behaviour.GetComponentsInChildren<ToolkitBehaviour>();

					for (int i = 0; i < behaviours.Length; i++)
						behaviours[i].enabled = false;

					ToolkitDebug.Error($"The {behaviour.name}'s MVC behaviour has been disabled! We have had some errors that need to be fixed in order for the MVC behaviours to work properly. Please check the Settings Panel's {foldout} foldout for more information.");
				}

				return false;
			}
			public bool SettingsHaveProblems(SettingsEditorFoldout settings)
			{
				return settings switch
				{
					SettingsEditorFoldout.General => HasGeneralProblems,
#if !MVC_COMMUNITY
					SettingsEditorFoldout.AI => HasAIProblems,
#endif
					SettingsEditorFoldout.Behaviour => HasBehaviourProblems,
					SettingsEditorFoldout.Damage => HasDamageProblems,
					SettingsEditorFoldout.Cameras => HasCamerasProblems,
					SettingsEditorFoldout.PlayerInputs => HasPlayerInputsProblems,
					SettingsEditorFoldout.EnginesChargers => HasEnginesProblems || HasChargersProblems,
					SettingsEditorFoldout.Grounds => HasGroundsProblems,
					SettingsEditorFoldout.SFX => HasSFXProblems,
					SettingsEditorFoldout.VFX => HasVFXProblems,
					_ => false,
				};
			}

			#endregion

			#endregion

			#region Constructors

			public ProblemsSheet(ToolkitSettings settings)
			{
				this.settings = settings;
			}

			#endregion

			#region Operators

			public static bool operator ==(ProblemsSheet sheet1, ProblemsSheet sheet2)
			{
				return sheet1.Equals(sheet2);
			}
			public static bool operator !=(ProblemsSheet sheet1, ProblemsSheet sheet2)
			{
				return !(sheet1 == sheet2);
			}

			#endregion
		}

		[Serializable]
		private struct MaterialProperty
		{
			#region Enumerators

			public enum PropertyType { Color, Vector, Float, Range, Texture }

			#endregion

			#region Variables

			public string name;
			public PropertyType type;

			#endregion

			#region Methods

			public override readonly bool Equals(object obj)
			{
				return obj is MaterialProperty property &&
					   name == property.name &&
					   type == property.type;
			}
			public override readonly int GetHashCode()
			{
				return HashCode.Combine(name, type);
			}

			#endregion

			#region Constructors

			public MaterialProperty(MaterialProperty property)
			{
				name = property.name;
				type = property.type;
			}
			public MaterialProperty(string name, PropertyType type)
			{
				this.name = name;
				this.type = type;
			}

			#endregion

			#region Operators

			public static bool operator ==(MaterialProperty propertyA, MaterialProperty propertyB)
			{
				return propertyA.Equals(propertyB);
			}
			public static bool operator !=(MaterialProperty propertyA, MaterialProperty propertyB)
			{
				return !(propertyA == propertyB);
			}

			#endregion
		}

		#endregion

		#endregion

		#region Variables

		#region Editor Variables

		public SettingsEditorFoldout settingsFoldout;
		public SettingsEditorSFXFoldout soundEffectsFoldout;
		public bool autoCheckForUpdates = true;
		public Utility.UnitType editorValuesUnit = Utility.UnitType.Metric;
		public Utility.UnitType editorTorqueUnit = Utility.UnitType.Metric;
		public Utility.UnitType editorPowerUnit = Utility.UnitType.Metric;
		public EditorSpringTargetMeasurement springTargetMeasurement = EditorSpringTargetMeasurement.Length;
		public float exhaustSimulationFPS = 60f;
		public bool previewSuspensionAdjustmentsAtEditMode = true;
		public bool useHideFlags = true;
		public float gizmosSize = 1f;
		public Color COMGizmoColor = Color.white;
		public Color engineGizmoColor = Utility.Color.purple;
		public Color exhaustGizmoColor = Utility.Color.orange;
		public Color wheelMarkGizmoColor = Color.magenta;
		public Color headlightGizmoColor = Color.white;
		public Color sideSignalLightGizmoColor = Color.yellow;
		public Color interiorLightGizmoColor = Color.gray;
		public Color rearLightGizmoColor = Color.red;
		public Color jointsGizmoColor = Color.green;
		public Color driverIKGizmoColor = Color.blue;
		public Color audioReverbZoneGizmoColor = new(1f, .5f, 0f, .5f);
		public Color darknessWeatherZoneGizmoColor = new(.5f, 0f, 1f, .5f);
		public Color fogWeatherZoneGizmoColor = new(.5f, .5f, .5f, .5f);
		public Color wheelsDamageZoneGizmoColor = new(1f, 0f, 1f, .5f);
		public Color damageRepairZoneGizmoColor = new(0f, 1f, .5f, .5f);
		public Color AIPathBezierColor = Color.cyan;
		public Color AIPathBezierSelectedColor = Color.yellow;
		public Color AIPathAnchorGizmoColor = Color.blue;
		public Color AIPathBezierStartAnchorColor = Color.green;
		public Color AIPathControlsGizmoColor = Color.gray;
		public Color AIBrakeZoneGizmoColor = new(1f, 0f, 0f, .5f);
		public Color AIHandbrakeZoneGizmoColor = new(0f, 1f, 0f, .5f);
		public Color AINOSZoneGizmoColor = new(0f, .5f, 1f, .5f);
		public Color AIObstaclesSensorGizmoColor = Color.green;
		public Color AIObstaclesSensorActiveGizmoColor = Color.red;

		#endregion

		#region Static Variables

		public static readonly string AssetPath = Path.Combine("Settings", "MVCSettings_Data");
		public static bool UsingStandardRenderPipeline => instance && instance.RenderPipeline == Utility.RenderPipeline.Standard;
		public static bool UsingURP => instance && instance.RenderPipeline == Utility.RenderPipeline.URP;
		public static bool UsingHDRP => instance && instance.RenderPipeline == Utility.RenderPipeline.HDRP;
		public static bool UsingCustomRenderPipeline => instance && instance.RenderPipeline == Utility.RenderPipeline.Custom;
		public static bool UsingWheelColliderPhysics =>
#if MVC_COMMUNITY
			true
#else
			instance && (instance.physics == PhysicsType.WheelCollider)
#endif
			;

		internal new static bool HasInternalErrors
		{
			get
			{
				if (!instance)
					LoadData();

				return !instance || instance.hasInternalErrors;
			}
		}
		internal static ToolkitSettings Instance
		{
			get
			{
				return instance;
			}
		}

		private static readonly string AssetOldPath = Path.Combine("ScriptableObjects", "MVCSettings_Data");
		private static ToolkitSettings instance;

		#endregion

		#region Global Variables

		public ProblemsSheet Problems
		{
			get
			{
				if (!problems.IsSheetValid)
					problems = new(this);

				return problems;
			}
		}
		public bool AutomaticTransmission => transmissionType == TransmissionType.Automatic;
		public bool ManualTransmission => !AutomaticTransmission;
		public Utility.RenderPipeline RenderPipeline
		{
			get
			{
				if (renderPipeline == Utility.RenderPipeline.Unknown)
					renderPipeline = Utility.GetCurrentRenderPipeline();

				return renderPipeline;
			}
		}
		public PhysicsType Physics
		{
			get
			{
				return physics;
			}
			set
			{
#if !MVC_COMMUNITY
				if (value != PhysicsType.WheelCollider)
					return;

				physics = value;
#endif
			}
		}
		public Utility.IntervalInt physicsSubSteps = new(15, 50, false, true);
		public Vehicle.SteeringModule.SteerMethod steerMethod = Vehicle.SteeringModule.SteerMethod.Ackermann;
		public TransmissionType transmissionType = TransmissionType.Automatic;
		public WheelRendererRefreshType wheelRendererRefreshType;
		public Utility.UnitType valuesUnit;
		public Utility.UnitType powerUnit;
		public Utility.UnitType torqueUnit;
		[Obsolete("Use `engineStartMode` instead.", true)]
		public bool startEnginesAtAwake = true;
		public EngineStartMode engineStartMode = EngineStartMode.AnyKeyPress;
		public bool autoEngineShutDown = true;
		public float engineShutDownTimeout = 60f;
		public bool useEngineStalling;
		public bool useGearLimiterOnReverseAndFirstGear;
		public bool burnoutsForAutomaticTransmissions = true;
		//public bool useAutomaticClutch = true;
		public bool useFuelSystem = true;
		public bool useInterior = true;
		public bool useReset = true;
		public float resetPreviewTime = 5f;
		public float resetTimeout = 3f;
		public int vehiclesLayer;
		public int vehicleWheelsLayer;
		public string gameControllerTag = "GameController";
		public string playerVehicleTag = "Player";

		public VehicleAILayer[] AILayers
		{
			get
			{
				if (aiLayers == null || aiLayers.Length < 1)
					RefreshInternalErrors();

				return aiLayers;
			}
			set
			{
				if (value == null || value.Length < 1)
					return;

				aiLayers = value;
			}
		}
		public bool useDynamicAIPathPointDistance = true;
		public Utility.Interval AIPathPointDistanceInterval
		{
			get
			{
				return aiPathPointDistanceInterval;
			}
			set
			{
				value.Min = math.max(value.Min, .1f);
				value.OverrideBorders = false;
				value.ClampToZero = true;
				aiPathPointDistanceInterval = value;
			}
		}
		public float AIPathPointDistance
		{
			get
			{
				return aiPathPointDistance;
			}
			set
			{
				aiPathPointDistance = math.max(value, .1f);
			}
		}
		public float AIPathSpacedPointsPerMeter
		{
			get
			{
				return 1f / aiPathSpacedPointsSpacing;
			}
			set
			{
				aiPathSpacedPointsSpacing = 1f / math.clamp(value, .1f, 20f);
			}
		}
		public float AIPathSpacedPointsSpacing
		{
			get
			{
				return aiPathSpacedPointsSpacing;
			}
			set
			{
				aiPathSpacedPointsSpacing = math.max(value, .1f);
			}
		}
		public int AIPathLengthSamples
		{
			get
			{
				return aiPathLengthSamples;
			}
			set
			{
				aiPathLengthSamples = math.max(value, 1);
			}
		}
		public float AIReverseTimeout
		{
			get
			{
				return aiReverseTimeout;
			}
			set
			{
				aiReverseTimeout = math.max(value, 0f);
			}
		}
		public float AIReverseTimeSpan
		{
			get
			{
				return aiReverseTimeSpan;
			}
			set
			{
				aiReverseTimeSpan = math.max(value, 0f);
			}
		}

		public bool useDownforce = true;
		public bool useDownforceWhenNotGrounded;
		public bool useAirAntiRoll = true;
		public float airAntiRollIntensity = 1f;
		public bool useDrag = true;
		public bool useChassisWings = true;
		public bool useAntiGroundColliders = true;
		public bool useEngineMass = true;
		public bool useWheelsMass = true;
		public bool UseAdditionalMass => useWheelsMass || useEngineMass;
		public SteeringInterpolation steeringInterpolation = SteeringInterpolation.Exponential;
		public float steerIntensity = 1f;
		public float steerReleaseIntensity = 5f;
		public float maximumSteerAngle = 45f;
		public float minimumSteerAngle = 5f;
		[FormerlySerializedAs("useSuspentionAdjustments")]
		public bool useSuspensionAdjustments = true;
		public float maximumCamberAngle = 10f;
		public float maximumCasterAngle = 10f;
		public float maximumToeAngle = 10f;
		public float maximumSideOffset = .25f;
		public bool sideOffsetAffectHandling = true;
		public bool useNOS = true;
		public bool useNOSReload = true;
		public bool useNOSDelay = true;
		public float NOSDelay = .75f;
		public bool useProgressiveNOS = true;
		public bool useOneShotNOS;
		public bool useLaunchControl = true;
		public bool useCounterSteer = true;
		public CounterSteerType counterSteerType;
		public int counterSteerPriority = -1;
		public float counterSteerIntensity = 1f;
		public float counterSteerDamping = 25f;
		public bool useDynamicSteering = true;
		public bool useDamage = true;
		public float damageLowVelocity = 5f;
		public float damageMediumVelocity = 15f;
		public float damageHighVelocity = 25f;
		//public float damageVertexRandomization = 1f;
		//public float damageRadius = .5f;
		//public float minimumVertDistanceForDamagedMesh = .002f;
		//public float maximumDamage = .5f;
		//public float damageIntensity = 1f;
		public bool useWheelHealth = true;
		public float tireDamageIntensity = 1f;
		public float tireExplosionForce = 1000f;
		public Color rimEdgeEmissionColor = Color.black;
		public string customRimShaderEmissionColorProperty;
		//public ChassisPartsJointsGroup chassisPartJoints;

		public VehicleCamera[] Cameras
		{
			get
			{
				if (cameras == null || cameras.Length < 1)
					RefreshInternalErrors();

				return cameras;
			}
		}

		public InputSystem inputSystem;
		public bool useMobileInputs = true;
		public GamepadRumbleType gamepadRumbleType = GamepadRumbleType.Independent;
		public GamepadRumbleMask gamepadRumbleMask = (GamepadRumbleMask)(-1);
		public Utility.Interval GamepadRumbleSpeedInterval
		{
			get
			{
				return gamepadRumbleSpeedInterval;
			}
			set
			{
				value.Min = math.max(value.Min, 0f);
				value.Max = math.max(value.Max, value.Min);
				value.OverrideBorders = false;
				value.ClampToZero = true;
				gamepadRumbleSpeedInterval = value;
			}
		}
		[FormerlySerializedAs("useGamepadLeveledRumble")]
		public bool useGamepadLevelledRumble = true;
		[FormerlySerializedAs("separateGamepadRumbleSides")]
		public bool useSeparateGamepadRumbleSides = true;
		public string steerInput = "Steering Wheel";
		public string fuelInput = "Fuel Pedal";
		public string brakeInput = "Brake Pedal";
		public string clutchInput = "Clutch Pedal";
		public string handbrakeInput = "Handbrake";
		public string engineStartSwitchInput = "Start Engine";
		public string gearShiftUpButtonInput = "Gear Shift Up";
		public string gearShiftDownButtonInput = "Gear Shift Down";
		public string NOSButtonInput = "NOS Boost";
		public string launchControlSwitchInput = "Switch Launch Control";
		//public string hornButtonInput = "Horn";
		public string resetButtonInput = "Reset Vehicle";
		public string changeCameraButtonInput = "Change Camera";
		public string forwardCameraViewInput = "Camera Forward";
		public string sidewaysCameraViewInput = "Camera Sideways";
		public string lightSwitchInput = "Switch Light";
		public string highBeamLightSwitchInput = "Switch High Beam Light";
		public string interiorLightSwitchInput = "Switch Interior Light";
		public string sideSignalLeftLightSwitchInput = "Switch Left Side Signal Light";
		public string sideSignalRightLightSwitchInput = "Switch Right Side Signal Light";
		public string hazardLightsSwitchInput = "Switch Hazard Lights";
		public string trailerLinkSwitchInput = "Switch Trailer Link";
		public bool switchHighBeamOnDoublePress = true;
		public bool linkTrailerManually = false;

		[SerializeField]
		private Utility.Interval gamepadRumbleSpeedInterval = new(150f, 300f, false, true);

		public VehicleEngine[] Engines
		{
			get
			{
				if (engines == null || engines.Length < 1)
					RefreshInternalErrors();

				return engines;
			}
		}
		public VehicleCharger[] Chargers
		{
			get
			{
				return chargers;
			}
		}
		public VehicleTireCompound[] TireCompounds
		{
			get
			{
				if (tireCompounds == null || tireCompounds.Length < 1)
					RefreshInternalErrors();

				return tireCompounds;
			}
		}

		public VehicleGroundMapper.GroundModule[] Grounds
		{
			get
			{
				if (grounds == null || grounds.Length < 1)
					RefreshInternalErrors();

				return grounds;
			}
		}

		public VehicleAudio.MixersGroup audioMixers = new();
		public string audioFolderPath = "Audio";
		public string EngineSFXFolderPath
		{
			get
			{
				return Path.Combine(audioFolderPath, engineSFXFolderPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			}
		}
		public string engineSFXFolderPath = $"SFX{Path.DirectorySeparatorChar}Engines";
		public char engineSFXNameSplitter = '_';
		public int maxEngineSFXClips = 10;
		public float SFXVolume = 1f;
		public ImpactClips impactClips = new();
		public GearShiftingClips gearShiftingClips = new();
		public ExhaustBlowoutClips exhaustClips = new();
		//public ExteriorPartsClips exteriorClips = new();
		public NOSClipsGroup NOSClips = new();
		public AudioClip[] hornClips = new AudioClip[] { };
		public TransmissionWhineClipsGroup[] transmissionWhineGroups = new TransmissionWhineClipsGroup[] { };
		public AudioClip reversingClip;
		public AudioClip windClip;
		public AudioClip brakeClip;
		public AudioClip indicatorClip;
		public AudioClip tireExplosionClip;

		public bool useParticleSystems = true;
		public ParticleSystem exhaustSmoke;
		public ParticleSystem exhaustFlame;
		public VFXOverrideMode NOSFlameOverrideMode = VFXOverrideMode.ParticleSystem;
		public ParticleSystem NOSFlame;
		public Material NOSFlameMaterial;
		public ParticleSystem damageSparks;
		public ParticleSystem collisionSparks;
		public ParticleSystem tireExplosion;
		public ParticleSystemForceField tireExplosionForceField;
		public bool useWheelMarks = true;
		public float wheelMarkResolution = 5f;
		public float wheelMarkGroundOffset = .02f;
		public float wheelMarkHideDistance = 50f;
		public bool useAIPathVisualizer = true;
		public Material pathVisualizerMaterial;
		public float pathVisualizerGroundOffset = .1f;
		public float pathVisualizerWidth = .25f;
		public Gradient pathVisualizerHeatColorGradient = new()
		{
			colorKeys = new GradientColorKey[]
			{
				new(new(0f, .5f, 1f), 0f),
				new(Color.yellow, .5f),
				new(Color.red, 1f)
			},
			alphaKeys = new GradientAlphaKey[]
			{
				new(1f, 0f),
				new(1f, 1f)
			}
		};
		public bool useLights = true;
		[FormerlySerializedAs("useIndicatorLightsAtEngineStal")]
		public bool useIndicatorLightsAtEngineStall = true;
		public bool useIndicatorLightsInFog = true;
		public Light exhaustFlameLight;
		public string customLightShaderEmissionColorProperty;
		public float signalLightsPeriod = 1f;
		public float lampLightsIntensityDamping = 10f;
		public bool useWheelSpinEffect = true;
		public int WheelSpinRPMThreshold
		{
			get
			{
				return wheelSpinRPMThreshold;
			}
			set
			{
				wheelSpinRPMThreshold = math.max(value, 1);
			}
		}
		public int WheelHideRPMThreshold
		{
			get
			{
				return wheelHideRPMThreshold;
			}
			set
			{
				wheelHideRPMThreshold = math.clamp(value, 1, wheelSpinRPMThreshold);
			}
		}
		public int WheelSpinSamplesCount
		{
			get
			{
				return wheelSpinSamplesCount;
			}
			set
			{
				wheelSpinSamplesCount = Mathf.Clamp(value, 1, 64);
			}
		}
		public float WheelSpinAlphaOffset
		{
			get
			{
				return wheelSpinAlphaOffset;
			}
			set
			{
				wheelSpinAlphaOffset = Mathf.Clamp(value, -1f, 1f);
			}
		}
		public float WheelSpinCullingDistance
		{
			get
			{
				return wheelSpinCullingDistance;
			}
			set
			{
				wheelSpinCullingDistance = math.max(value, 0f);
			}
		}
#pragma warning disable CS0169 // Remove unused private members
#pragma warning disable CS0414 // Remove unused private members
#pragma warning disable IDE0052 // Remove unused private members
#pragma warning disable IDE0044 // Remove unused private members
		[SerializeField, FormerlySerializedAs("wheelSpinMeshTransparentMaterial")]
		private Material wheelSpinTransparentMaterial;
		[SerializeField]
		private string wheelSpinMeshMaterialAlbedoProperty = "_BaseColorMap";
		[SerializeField]
		private string wheelSpinMeshMaterialNormalMapProperty = "_NormalMap";
		[SerializeField]
		private string wheelSpinMeshMaterialMaskMapProperty = "_MaskMap";
		[SerializeField]
		private string wheelSpinMeshMaterialColorProperty = "_BaseColor";
		[SerializeField]
		private MaterialProperty[] wheelSpinMeshMaterialOtherProperties = new MaterialProperty[]
		{
			new("_Metallic", MaterialProperty.PropertyType.Range),
			new("_Smoothness", MaterialProperty.PropertyType.Range),
			new("_NormalScale", MaterialProperty.PropertyType.Range),
			new("_SmoothnessRemapMin", MaterialProperty.PropertyType.Float),
			new("_SmoothnessRemapMax", MaterialProperty.PropertyType.Float),
			new("_AORemapMin", MaterialProperty.PropertyType.Float),
			new("_AORemapMax", MaterialProperty.PropertyType.Float),
			new("_EmissiveColorMap", MaterialProperty.PropertyType.Texture),
			new("_EmissiveColor", MaterialProperty.PropertyType.Color)
		};
		[SerializeField]
		private string[] wheelSpinMeshMaterialKeywords = new string[]
		{
			"_NORMALMAP",
			"_MASKMAP",
			"_EMISSIVE_COLOR_MAP"
		};
#pragma warning restore IDE0044 // Remove unused private members
#pragma warning restore IDE0052 // Remove unused private members
#pragma warning restore CS0414 // Remove unused private members
#pragma warning restore CS0169 // Remove unused private members
		public bool useBrakesHeat = true;
		public string brakeMaterialEmissionColorProperty = "_EmissiveColor";
		public Color brakeHeatEmissionColor = new Color(.75f, .125f, 0f) * 10f;
		public float BrakeHeatingSpeed
		{
			get
			{
				return brakeHeatingSpeed;
			}
			set
			{
				brakeHeatingSpeed = Mathf.Clamp(value, .1f, 10f);
			}
		}
		public float BrakeCoolingSpeed
		{
			get
			{
				return brakeCoolingSpeed;
			}
			set
			{
				brakeCoolingSpeed = Mathf.Clamp(value, .1f, 10f);
			}
		}
		public bool clampBrakeHeat = true;
		public bool brakeHeatAffectPerformance;

		private ProblemsSheet problems;
		[SerializeField]
		private bool hasInternalErrors;
		[SerializeField]
		private PhysicsType physics = PhysicsType.WheelCollider;
		[SerializeField]
		private Utility.Interval aiPathPointDistanceInterval = new(5f, 20f, false, true);
		[SerializeField]
		private VehicleAILayer[] aiLayers;
		[SerializeField, FormerlySerializedAs("aiPlayerChasability")]
		private List<int> aiChasePlayerLayers;
		[SerializeField, FormerlySerializedAs("aiLayersChasability")]
		private List<Vector2Int> aiChaseLayerPairs;
		[SerializeField, FormerlySerializedAs("AIPathPointDistance")]
		private float aiPathPointDistance = 20f;
		[SerializeField, FormerlySerializedAs("aiPathGeneratedPointsSpacing")]
		private float aiPathSpacedPointsSpacing = 2f;
		[SerializeField]
		private int aiPathLengthSamples = 128;
		[SerializeField]
		private float aiReverseTimeout = 1.5f;
		[SerializeField]
		private float aiReverseTimeSpan = 1.5f;
		[SerializeField]
		private VehicleCamera[] cameras = new VehicleCamera[] { };
		[SerializeField, FormerlySerializedAs("enginePresets")]
		private VehicleEngine[] engines = new VehicleEngine[] { };
		[SerializeField, FormerlySerializedAs("chargerPresets")]
		private VehicleCharger[] chargers = new VehicleCharger[] { };
		[SerializeField]
		private VehicleTireCompound[] tireCompounds = new VehicleTireCompound[] { };
		[SerializeField]
		private VehicleGroundMapper.GroundModule[] grounds = new VehicleGroundMapper.GroundModule[] { };
		[SerializeField]
		private int wheelSpinRPMThreshold = 1000;
		[SerializeField]
		private int wheelHideRPMThreshold = 300;
		[SerializeField]
		private int wheelSpinSamplesCount = 32;
		[SerializeField]
		private float wheelSpinAlphaOffset = .5f;
		[SerializeField]
		private float wheelSpinCullingDistance = 20f;
		[SerializeField]
		private float brakeHeatingSpeed = 1f;
		[SerializeField]
		private float brakeCoolingSpeed = 1f;
		private Utility.RenderPipeline renderPipeline = Utility.RenderPipeline.Unknown;

		#endregion

		#endregion

		#region Methods

		#region Static Methods

		public static ToolkitSettings LoadData(bool forceReload = false)
		{
			if (forceReload || !instance)
			{
				InputsManager.ForceDataChange();
				InputsManager.LoadData();

				instance = LoadData(AssetPath);

				if (!instance)
				{
					instance = LoadData(AssetOldPath);

					if (instance && Application.isEditor)
					{
						string assetOldPath = $"Assets/Resources/{AssetOldPath}.asset";
						string assetPath = $"Assets/BxB Studio/MVC/Resources/{AssetPath}.asset";
						string assetDirectoryPath = Path.GetDirectoryName(assetPath);

						if (!Directory.Exists(assetDirectoryPath))
							Directory.CreateDirectory(assetDirectoryPath);

						if (File.Exists(assetOldPath))
						{
							File.Move(assetOldPath, assetPath);
							File.Move($"{assetOldPath}.meta", $"{assetPath}.meta");
						}
					}
				}
			}

			return instance;
		}
		public static ToolkitSettings LoadData(string path)
		{
			try
			{
				return Resources.Load<ToolkitSettings>(path);
			}
			catch (UnityException e)
			{
				Debug.Log(e.Message);

				return null;
			}
		}
		public static void RefreshInternalErrors()
		{
			LoadData(true);

			if (!instance)
				return;

			instance.hasInternalErrors = instance.aiLayers == null || instance.aiLayers.Length < 1 || instance.cameras == null || instance.cameras.Length < 1 || instance.engines == null || instance.engines.Length < 1 || instance.tireCompounds == null || instance.tireCompounds.Length < 1 || instance.grounds == null || instance.grounds.Length < 1;
		}

		#endregion

		#region Global Methods

		#region Utilities

		public string[] GetAILayersNames(bool useNumbers)
		{
			if (AILayers == null || AILayers.Length < 1)
				return new string[] { };

			return aiLayers.Select((layer, index) => $"{(useNumbers ? $"{index}. " : "")}{layer.Name}").ToArray();
		}
		public void AddAILayer(VehicleAILayer layer)
		{
			if (AILayers == null || AILayers.Length < 1)
			{
				ToolkitDebug.Error("Couldn't add the requested camera, the controller is currently facing some internal problems that need to be fixed!");

				return;
			}

			int index = aiLayers.Length;

			Array.Resize(ref aiLayers, index + 1);

			aiLayers[index] = layer;

			aiChasePlayerLayers.Add(index);

			for (int i = 0; i < aiLayers.Length - 1; i++)
			{
				aiChaseLayerPairs.Add(new(i, index));
				aiChaseLayerPairs.Add(new(index, i));
			}

			aiChasePlayerLayers = aiChasePlayerLayers.Distinct().ToList();
			aiChaseLayerPairs = aiChaseLayerPairs.Distinct().ToList();
		}
		public void MoveAILayer(int index, int newIndex)
		{
			if (AILayers == null || AILayers.Length < 1)
			{
				ToolkitDebug.Error("Couldn't add the requested camera, the controller is currently facing some internal problems that need to be fixed!");

				return;
			}

			if (index < 0 || index >= aiLayers.Length || newIndex < 0 || newIndex >= aiLayers.Length)
			{
				ToolkitDebug.Error("Couldn't move the requested camera to its new position, seems like one of the indexes is out of range!");

				return;
			}

			List<VehicleAILayer> layersList = aiLayers.ToList();
			VehicleAILayer layer = layersList[index];

			layersList.RemoveAt(index);
			layersList.Insert(newIndex, layer);

			aiChaseLayerPairs = aiChaseLayerPairs.Distinct().ToList();

			for (int i = 0; i < aiChaseLayerPairs.Count; i++)
			{
				Vector2Int pair = aiChaseLayerPairs[i];

				if (pair.x == index)
					pair.x = newIndex;
				else if (pair.x == newIndex)
					pair.x = index;
				
				if (pair.y == index)
					pair.y = newIndex;
				else if (pair.y == newIndex)
					pair.y = index;

				aiChaseLayerPairs[i] = pair;
			}

			for (int i = 0; i < aiChasePlayerLayers.Count; i++)
			{
				if (aiChasePlayerLayers[i] == index)
					aiChasePlayerLayers[i] = newIndex;
				else if (aiChasePlayerLayers[i] == newIndex)
					aiChasePlayerLayers[i] = index;
			}

			aiLayers = layersList.ToArray();
		}
		public void RemoveAILayer(int index)
		{
			if (AILayers == null || AILayers.Length < 1)
			{
				ToolkitDebug.Error("Couldn't remove the requested AI layer, the controller is currently facing some internal problems that need to be fixed!");

				return;
			}

			if (index < 0 || index >= aiLayers.Length)
			{
				ToolkitDebug.Error("Couldn't remove the requested AI layer, it seems like the index is out of range!");

				return;
			}

			if (aiLayers.Length < 2)
			{
				ToolkitDebug.Warning("Cannot remove more AI layers from the list, that will cause internal problems to the controller!");

				return;
			}

			List<VehicleAILayer> layersList = aiLayers.ToList();

			aiChaseLayerPairs = aiChaseLayerPairs.Distinct().ToList();

			layersList.RemoveAt(index);
			aiChaseLayerPairs.RemoveAll(pair => pair.x == index || pair.y == index);
			aiChasePlayerLayers.Remove(index);

			aiLayers = layersList.ToArray();
		}
		public bool CanAIChasePlayer(string layer)
		{
			int layerIndex = VehicleAILayer.GetLayerIndexFromName(layer);

			if (layerIndex < 0)
				return false;

			return CanAILayerChasePlayer(layerIndex);
		}
		public bool CanAILayerChasePlayer(int layer)
		{
			if (aiChasePlayerLayers == null || aiChasePlayerLayers.Count < 1)
				return false;

			if (layer < 0 || layer >= Settings.AILayers.Length)
				throw new IndexOutOfRangeException("Could not get AI layer chasing value as index `layer` is out of range.");

			return aiChasePlayerLayers.Contains(layer);
		}
		public bool CanAILayersChase(string layer1, string layer2)
		{
			int layerIndex1 = VehicleAILayer.GetLayerIndexFromName(layer1);
			int layerIndex2 = VehicleAILayer.GetLayerIndexFromName(layer2);

			if (layerIndex1 < 0 || layerIndex2 < 0)
				return false;

			return CanAILayersChase(layerIndex1, layerIndex2);
		}
		public bool CanAILayersChase(int layer1, int layer2)
		{
			if (aiChaseLayerPairs == null || aiChaseLayerPairs.Count < 1)
				return false;

			if (layer1 < 0 || layer1 >= Settings.AILayers.Length)
				throw new IndexOutOfRangeException("Could not get AI layer chase value as index `layer1` is out of range.");
			else if (layer2 < 0 || layer2 >= Settings.AILayers.Length)
				throw new IndexOutOfRangeException("Could not get AI layer chase value as index `layer2` is out of range.");

			if (layer1 == layer2)
				return false;

			return aiChaseLayerPairs.Contains(new(layer1, layer2));
		}
		public void SetAILayerChasePlayer(string layer, bool state)
		{
			if (Application.isPlaying)
				return;

			int layerIndex = VehicleAILayer.GetLayerIndexFromName(layer);

			if (layerIndex < 0)
				return;

			SetAILayerChasePlayer(layerIndex, state);
		}
		public void SetAILayerChasePlayer(int layer, bool state)
		{
			if (Application.isPlaying)
				return;

			aiChasePlayerLayers ??= new();

			if (layer < 0 || layer >= Settings.AILayers.Length)
				throw new IndexOutOfRangeException("Could not set AI layer chase value as index `layer` is out of range.");

			switch (state)
			{
				case true:
					if (CanAILayerChasePlayer(layer))
						return;

					aiChasePlayerLayers.Add(layer);

					return;

				default:
					if (!CanAILayerChasePlayer(layer))
						return;

					aiChasePlayerLayers.Remove(layer);

					return;
			}
		}
		public void SetAILayerPairChase(string layer1, string layer2, bool state)
		{
			if (Application.isPlaying)
				return;

			int layerIndex1 = VehicleAILayer.GetLayerIndexFromName(layer1);
			int layerIndex2 = VehicleAILayer.GetLayerIndexFromName(layer2);

			if (layerIndex1 < 0 || layerIndex2 < 0)
				return;

			SetAILayerPairChase(layerIndex1, layerIndex2, state);
		}
		public void SetAILayerPairChase(int layer1, int layer2, bool state)
		{
			if (Application.isPlaying)
				return;

			aiChaseLayerPairs ??= new();

			if (layer1 < 0 || layer1 >= Settings.AILayers.Length)
				throw new IndexOutOfRangeException("Could not set AI layer chase value as index `layer1` is out of range.");
			else if (layer2 < 0 || layer2 >= Settings.AILayers.Length)
				throw new IndexOutOfRangeException("Could not set AI layer chase value as index `layer2` is out of range.");

			if (layer1 == layer2)
				return;

			Vector2Int layerChasePair = new(layer1, layer2);

			switch (state)
			{
				case true:
					if (CanAILayersChase(layer1, layer2))
						return;

					aiChaseLayerPairs.Add(layerChasePair);

					aiChaseLayerPairs = aiChaseLayerPairs.Distinct().ToList();

					return;

				default:
					if (!CanAILayersChase(layer1, layer2))
						return;

					aiChaseLayerPairs.Remove(layerChasePair);

					aiChaseLayerPairs = aiChaseLayerPairs.Distinct().ToList();

					return;
			}
		}
		public void ResetAILayers()
		{
			if (Application.isPlaying)
				return;

			aiLayers = new VehicleAILayer[]
			{
				new("Temp")
			};
			aiLayers = new VehicleAILayer[]
			{
				new("Default")
			};
			aiChaseLayerPairs = new();
			aiChasePlayerLayers = new() { 0 };
		}
		public string[] GetCamerasNames(bool useNumbers)
		{
			if (Cameras == null || Cameras.Length < 1)
				return new string[] { };

			return Cameras.Select((preset, index) => $"{(useNumbers ? $"{index}. " : "")}{preset.Name}").ToArray();
		}
		public void AddCamera(VehicleCamera camera)
		{
			if (Cameras == null || Cameras.Length < 1)
			{
				ToolkitDebug.Error("Couldn't add the requested camera, the controller is currently facing some internal problems that need to be fixed!");

				return;
			}

			Array.Resize(ref cameras, cameras.Length + 1);

			cameras[^1] = camera;
		}
		public void MoveCamera(int index, int newIndex)
		{
			if (Cameras == null || Cameras.Length < 1)
			{
				ToolkitDebug.Error("Couldn't add the requested camera, the controller is currently facing some internal problems that need to be fixed!");

				return;
			}

			if (index < 0 || index >= cameras.Length || newIndex < 0 || newIndex >= cameras.Length)
			{
				ToolkitDebug.Error("Couldn't move the requested camera to its new position, seems like one of the indexes is out of range!");

				return;
			}

			List<VehicleCamera> camerasList = cameras.ToList();
			VehicleCamera camera = camerasList[index];

			camerasList.RemoveAt(index);
			camerasList.Insert(newIndex, camera);

			cameras = camerasList.ToArray();
		}
		public void RemoveCamera(int index)
		{
			if (Cameras == null || Cameras.Length < 1)
			{
				ToolkitDebug.Error("Couldn't remove the requested camera, the controller is currently facing some internal problems that need to be fixed!");

				return;
			}

			if (index < 0 || index >= cameras.Length)
			{
				ToolkitDebug.Error("Couldn't remove the requested camera, it seems like the index is out of range!");

				return;
			}

			if (cameras.Length < 2)
			{
				ToolkitDebug.Warning("Cannot remove more cameras from the list, that will cause internal problems to the controller!");

				return;
			}

			List<VehicleCamera> camerasList = Cameras.ToList();

			camerasList.RemoveAt(index);

			cameras = camerasList.ToArray();
		}
		public void ResetCameras()
		{
			cameras = new VehicleCamera[]
			{
				new("Temp")
			};
			cameras = new VehicleCamera[]
			{
				new("Default")
			};
		}
		public string[] GetEnginesNames(bool useNumbers)
		{
			if (Engines == null || Engines.Length < 1)
				return new string[] { };

			return engines.Select((preset, index) => $"{(useNumbers ? $"{index}. " : "")}{preset.Name}").ToArray();
		}
		public void AddEngine(VehicleEngine engine)
		{
			if (Engines == null || Engines.Length < 1)
			{
				ToolkitDebug.Error("Couldn't add the requested engine, the controller is currently facing some internal problems that need to be fixed!");

				return;
			}

			if (!engine.IsValid)
			{
				ToolkitDebug.Error("Couldn't add the requested engine because it's invalid!");

				return;
			}

			Array.Resize(ref engines, engines.Length + 1);

			engines[^1] = engine;
		}
		public void MoveEngine(int index, int newIndex)
		{
			if (index < 0 || index >= Engines.Length || newIndex < 0 || newIndex >= Engines.Length)
			{
				ToolkitDebug.Error("Couldn't move the requested engine to its new position, seems like one of the indexes is out of range!");

				return;
			}

			List<VehicleEngine> enginesList = Engines.ToList();
			VehicleEngine engine = enginesList[index];

			enginesList.RemoveAt(index);
			enginesList.Insert(newIndex, engine);

			engines = enginesList.ToArray();

			if (Chargers != null && Chargers.Length > 0)
				for (int i = 0; i < chargers.Length; i++)
				{
					int engineIndex1 = chargers[i].GetCompatibleEngineIndex(index);
					int engineIndex2 = chargers[i].GetCompatibleEngineIndex(newIndex);

					if (engineIndex1 > -1)
						chargers[i].SetCompatibleEngine(engineIndex1, newIndex);

					if (engineIndex2 > -1)
						chargers[i].SetCompatibleEngine(engineIndex2, index);
				}
		}
		public void RemoveEngine(int index)
		{
			if (Engines == null || Engines.Length < 1)
			{
				ToolkitDebug.Error("Couldn't remove the requested engine, the controller is currently facing some internal problems that need to be fixed!");

				return;
			}

			if (index < 0 || index >= this.engines.Length)
			{
				ToolkitDebug.Error("Couldn't remove the requested engine, it seems like the index is out of range!");

				return;
			}

			if (this.engines.Length < 2)
			{
				ToolkitDebug.Warning("Cannot remove more engines from the list, that will cause internal problems to the controller!");

				return;
			}

			List<VehicleEngine> engines = this.engines.ToList();

			engines.RemoveAt(index);

			this.engines = engines.ToArray();

			if (chargers != null && chargers.Length > 0)
				for (int i = 0; i < chargers.Length; i++)
				{
					int engineIndex = chargers[i].GetCompatibleEngineIndex(index);

					if (engineIndex < 0)
						continue;

					chargers[i].RemoveCompatibleEngineAtIndex(engineIndex);
				}
		}
		public void ResetEngines()
		{
			engines = new VehicleEngine[]
			{
				new("Temp")
			};
			engines = new VehicleEngine[]
			{
				new("Default Engine"),
				new("Electric Engine")
				{
					MinimumRPM = 0f,
					OverRevRPM = 9500f,
					MaximumRPM = 10000f,
					Power = 500f,
					Torque = 500f,
					Type = VehicleEngine.EngineType.Electric
				}
			};

			if (chargers != null)
				for (int i = 0; i < chargers.Length; i++)
					chargers[i].ResetCompatibleEngines();
		}
		public string[] GetChargersNames(bool useNumbers)
		{
			if (Chargers == null || Chargers.Length < 1)
				return new string[] { };

			return chargers.Select((preset, index) => $"{(useNumbers ? $"{index}. " : "")}{preset.Name}").ToArray();
		}
		public void AddCharger(VehicleCharger charger)
		{
			if (Chargers == null)
				chargers = new VehicleCharger[] { };

			if (!charger.IsValid || !charger.IsCompatible || charger.HasIssues)
			{
				ToolkitDebug.Error("Couldn't add the requested charger, due to its invalidity or it may has issues that need to be fixed before proceeding!");

				return;
			}

			Array.Resize(ref chargers, chargers.Length + 1);

			chargers[^1] = charger;
		}
		public void MoveCharger(int index, int newIndex)
		{
			if (index < 0 || index >= Chargers.Length || newIndex < 0 || newIndex >= Chargers.Length)
			{
				ToolkitDebug.Error("Couldn't move the requested charger to its new position, seems like one of the indexes is out of range!");

				return;
			}

			List<VehicleCharger> chargersList = Chargers.ToList();
			VehicleCharger charger = chargersList[index];

			chargersList.RemoveAt(index);
			chargersList.Insert(newIndex, charger);

			chargers = chargersList.ToArray();
		}
		public void RemoveCharger(int index)
		{
			if (Chargers == null)
				this.chargers = new VehicleCharger[] { };

			if (this.chargers.Length < 1)
			{
				ToolkitDebug.Warning("Couldn't remove the requested charger, the list is already empty!");

				return;
			}

			if (index < 0 || index >= this.chargers.Length)
			{
				ToolkitDebug.Error("Couldn't remove the requested charger, it seems like the index is out of range!");

				return;
			}

			List<VehicleCharger> chargers = this.chargers.ToList();

			chargers.RemoveAt(index);

			this.chargers = chargers.ToArray();
		}
		public void ResetChargers()
		{
			chargers = new VehicleCharger[]
			{
				new("Temp")
			};
			chargers = new VehicleCharger[]
			{
				new("Default Turbocharger")
				{
					Type = VehicleCharger.ChargerType.Turbocharger
				},
				new("Default Supercharger")
				{
					Type = VehicleCharger.ChargerType.Supercharger
				}
			};
		}
		public string[] GetTireCompoundsNames(bool useNumbers)
		{
			if (TireCompounds == null || tireCompounds.Length < 1)
				return new string[] { };

			return tireCompounds.Select((preset, index) => $"{(useNumbers ? $"{index}. " : "")}{preset.Name}").ToArray();
		}
		public void AddTireCompound(VehicleTireCompound tireCompound)
		{
			if (TireCompounds == null || tireCompounds.Length < 1)
			{
				ToolkitDebug.Error("Couldn't add the requested tire compound, the controller is currently facing some internal problems that need to be fixed!");

				return;
			}

			if (!tireCompound.IsValid)
			{
				ToolkitDebug.Error("Couldn't add the requested tire compound because it's invalid!");

				return;
			}

			Array.Resize(ref tireCompounds, tireCompounds.Length + 1);

			tireCompounds[^1] = tireCompound;
		}
		public void MoveTireCompound(int index, int newIndex)
		{
			if (index < 0 || index >= TireCompounds.Length || newIndex < 0 || newIndex >= tireCompounds.Length)
			{
				ToolkitDebug.Error("Couldn't move the requested tire compound to its new position, seems like one of the indexes is out of range!");

				return;
			}

			List<VehicleTireCompound> tireCompoundList = tireCompounds.ToList();
			VehicleTireCompound tireCompound = tireCompoundList[index];

			tireCompoundList.RemoveAt(index);
			tireCompoundList.Insert(newIndex, tireCompound);

			tireCompounds = tireCompoundList.ToArray();
		}
		public void RemoveTireCompound(int index)
		{
			if (TireCompounds == null || this.tireCompounds.Length < 1)
			{
				ToolkitDebug.Error("Couldn't remove the requested tire compound, the controller is currently facing some internal problems that need to be fixed!");

				return;
			}

			if (index < 0 || index >= this.engines.Length)
			{
				ToolkitDebug.Error("Couldn't remove the requested tire compound, it seems like the index is out of range!");

				return;
			}

			if (this.engines.Length < 2)
			{
				ToolkitDebug.Warning("Cannot remove more tire compounds from the list, that will cause internal problems to the controller!");

				return;
			}

			List<VehicleTireCompound> tireCompounds = this.tireCompounds.ToList();

			tireCompounds.RemoveAt(index);

			this.tireCompounds = tireCompounds.ToArray();
		}
		public void ResetTireCompounds()
		{
			tireCompounds = new VehicleTireCompound[]
			{
				new("Temp")
			};
			tireCompounds = new VehicleTireCompound[]
			{
				new("Default")
			};
		}
		public string[] GetGroundsNames(bool useNumbers)
		{
			if (Grounds == null || Grounds.Length < 1)
				return new string[] { };

			return Grounds.Select((preset, index) => $"{(useNumbers ? $"{index}. " : "")}{preset.Name}").ToArray();
		}
		public void AddGround(VehicleGroundMapper.GroundModule ground)
		{
			if (grounds == null || grounds.Length < 1)
			{
				RefreshInternalErrors();
				ToolkitDebug.Error("Couldn't add the requested grounds, the controller is currently facing some internal problems that need to be fixed!");

				return;
			}

			Array.Resize(ref grounds, grounds.Length + 1);

			grounds[^1] = ground;
		}
		public void MoveGround(int index, int newIndex)
		{
			if (index < 0 || index >= Grounds.Length || newIndex < 0 || newIndex >= Grounds.Length)
			{
				ToolkitDebug.Error("Couldn't move the requested ground to its new position, seems like one of the indexes is out of range!");

				return;
			}

			List<VehicleGroundMapper.GroundModule> groundsList = Grounds.ToList();
			VehicleGroundMapper.GroundModule ground = groundsList[index];

			groundsList.RemoveAt(index);
			groundsList.Insert(newIndex, ground);

			grounds = groundsList.ToArray();
		}
		public void RemoveGround(int index)
		{
			if (grounds == null || grounds.Length < 1)
			{
				RefreshInternalErrors();
				ToolkitDebug.Error("Couldn't remove the requested grounds, the controller is currently facing some internal problems that need to be fixed!");

				return;
			}

			if (index < 0 || index >= grounds.Length)
			{
				ToolkitDebug.Error("Couldn't remove the requested ground, it seems like the index is out of range!");

				return;
			}

			if (grounds.Length < 2)
			{
				ToolkitDebug.Warning("Cannot remove more grounds from the list, that will cause internal problems to the controller!");

				return;
			}

			List<VehicleGroundMapper.GroundModule> groundsList = grounds.ToList();

			groundsList.RemoveAt(index);

			grounds = groundsList.ToArray();
		}
		public void ResetGrounds()
		{
			grounds = new VehicleGroundMapper.GroundModule[]
			{
				new("Default")
			};
		}

		#endregion

		#region Enable, Disable & Destroy
		
		private void OnEnable()
		{
			instance = LoadData();
		}
		private void OnDisable()
		{
			if (instance == this)
				instance = null;
		}
		private void OnDestroy()
		{
			OnDisable();
		}

		#endregion

		#endregion

		#endregion

		#region Operators

		public static implicit operator bool(ToolkitSettings settings) => settings != null;

		#endregion
	}
}

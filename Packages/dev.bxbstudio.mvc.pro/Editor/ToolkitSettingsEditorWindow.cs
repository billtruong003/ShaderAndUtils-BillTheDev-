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
using UnityEditor;
using UnityEditorInternal;
using Utilities;
using Utilities.Editor;
using Utilities.Inputs;
using Utilities.Inputs.Editor;
using MVC.AI.Editor;
using MVC.Core;
using MVC.Base;
using MVC.Base.Editor;
using MVC.Internal;
using MVC.Internal.Editor;
using MVC.Utilities;
using MVC.Utilities.Internal;
using MVC.Utilities.Editor;

using Object = UnityEngine.Object;
using Input = Utilities.Inputs.Input;

#endregion

namespace MVC.Editor
{
	public class ToolkitSettingsEditorWindow : ToolkitEditorWindow
	{
		#region Modules

		[Serializable]
		private struct CameraPresets
		{
			public CameraPreset[] cameras;
		}
		[Serializable]
		private struct CameraPreset
		{
			#region Variables

			public string name;
			public VehicleCamera.CameraType type;
			public VehicleCamera.PivotPositionType pivotPosition;
			public VehicleCamera.FollowerFeature followerFeatures;
			public VehicleCamera.PivotFeature pivotFeatures;
			public int3 pivotPoint;
			public VehicleCamera.ImpactAxis impactAxes;
			public Utility.Interval2 orbitInterval;
			public bool invertXOrbit;
			public bool invertYOrbit;
			public bool orbitUsingMouseButton;
			public MouseButton orbitMouseButton;
			public bool skipDistantOrbit;
			public float orbitSkipAngle;
			public float orbitTimeout;
			public Utility.Interval fieldOfViewInterval;
			public Utility.Interval shakeSpeedInterval;
			public float tiltAngle;
			public bool invertTiltAngle;

			#endregion

			#region Methods

			public readonly void SetOnSettings(int index)
			{
				SetToCamera(ref Settings.Cameras[index]);
			}

			private readonly void SetToCamera(ref VehicleCamera camera)
			{
				camera.Type = type;
				camera.PivotPosition = pivotPosition;
				camera.followerFeatures = followerFeatures;
				camera.pivotFeatures = pivotFeatures;
				camera.PivotPoint = pivotPoint;
				camera.ImpactAxes = impactAxes;
				camera.orbitInterval = orbitInterval;
				camera.invertXOrbit = invertXOrbit;
				camera.invertYOrbit = invertYOrbit;
				camera.orbitUsingMouseButton = orbitUsingMouseButton;
				camera.orbitMouseButton = orbitMouseButton;
				camera.skipDistantOrbit = skipDistantOrbit;
				camera.OrbitSkipAngle = orbitSkipAngle;
				camera.OrbitTimeout = orbitTimeout;
				camera.FieldOfViewInterval = fieldOfViewInterval;
				camera.ShakeSpeedInterval = shakeSpeedInterval;
				camera.TiltAngle = tiltAngle;
				camera.invertTiltAngle = invertTiltAngle;
			}

			#endregion

			#region Operators

			public static implicit operator CameraPreset(VehicleCamera camera)
			{
				return new CameraPreset
				{
					name = camera.Name,
					type = camera.Type,
					pivotPosition = camera.PivotPosition,
					followerFeatures = camera.followerFeatures,
					pivotFeatures = camera.pivotFeatures,
					pivotPoint = camera.PivotPoint,
					impactAxes = camera.ImpactAxes,
					orbitInterval = camera.orbitInterval,
					invertXOrbit = camera.invertXOrbit,
					invertYOrbit = camera.invertYOrbit,
					orbitUsingMouseButton = camera.orbitUsingMouseButton,
					orbitMouseButton = camera.orbitMouseButton,
					skipDistantOrbit = camera.skipDistantOrbit,
					orbitSkipAngle = camera.OrbitSkipAngle,
					orbitTimeout = camera.OrbitTimeout,
					fieldOfViewInterval = camera.FieldOfViewInterval,
					shakeSpeedInterval = camera.ShakeSpeedInterval,
					tiltAngle = camera.TiltAngle,
					invertTiltAngle = camera.invertTiltAngle
				};
			}
			public static implicit operator VehicleCamera(CameraPreset preset)
			{
				VehicleCamera camera = new(preset.name);

				preset.SetToCamera(ref camera);

				return camera;
			}

			#endregion
		}
		[Serializable]
		private struct EngineChargerPresets
		{
			public EnginePreset[] engines;
			public ChargerPreset[] chargers;
		}
		[Serializable]
		private struct EnginePreset
		{
			#region Modules

			[Serializable]
			public struct AudioModule
			{
				#region Variables

				public string folderName;
				public VehicleEngine.AudioModule.OutputType outputs;
				public VehicleEngine.AudioModule.CelerationType celerationType;
				public AssetIdentifier startingClip;
				public AssetIdentifier engineMixer;
				public AssetIdentifier exhaustMixer;
				public float lowRPMVolume;
				public float maxRPMDistortion;
				public float overRPMDistortion;
				public float decelLowPassFrequency;
				public bool useAccelLowPass;
				public float accelLowPassFrequency;
				public float accelLowPassRPMEnd;
				public bool useLowPassDamping;
				public float lowPassDamping;

				#endregion

				#region Operators

				public static implicit operator VehicleEngine.AudioModule(AudioModule module)
				{
					return new VehicleEngine.AudioModule
					{
						folderName = module.folderName,
						outputs = module.outputs,
						celerationType = module.celerationType,
						startingClip = module.startingClip.GetAsset<AudioClip>(),
						mixerGroups = new VehicleAudio.EngineMixersGroup
						{
							engine = module.engineMixer.GetAsset<AudioMixerGroup>(),
							exhaust = module.exhaustMixer.GetAsset<AudioMixerGroup>()
						},
						lowRPMVolume = module.lowRPMVolume,
						maxRPMDistortion = module.maxRPMDistortion,
						overRPMDistortion = module.overRPMDistortion,
						decelLowPassFrequency = module.decelLowPassFrequency,
						useAccelLowPass = module.useAccelLowPass,
						accelLowPassFrequency = module.accelLowPassFrequency,
						accelLowPassRPMEnd = module.accelLowPassRPMEnd,
						useLowPassDamping = module.useLowPassDamping,
						lowPassDamping = module.lowPassDamping
					};
				}
				public static implicit operator AudioModule(VehicleEngine.AudioModule module)
				{
					return new AudioModule
					{
						folderName = module.folderName,
						outputs = module.outputs,
						celerationType = module.celerationType,
						startingClip = module.startingClip,
						engineMixer = module.mixerGroups.engine,
						exhaustMixer = module.mixerGroups.exhaust,
						lowRPMVolume = module.lowRPMVolume,
						maxRPMDistortion = module.maxRPMDistortion,
						overRPMDistortion = module.overRPMDistortion,
						decelLowPassFrequency = module.decelLowPassFrequency,
						useAccelLowPass = module.useAccelLowPass,
						accelLowPassFrequency = module.accelLowPassFrequency,
						accelLowPassRPMEnd = module.accelLowPassRPMEnd,
						useLowPassDamping = module.useLowPassDamping,
						lowPassDamping = module.lowPassDamping
					};
				}

				#endregion
			}

			#endregion

			#region Variables

			public string name;
			public VehicleEngine.EngineType type;
			public VehicleEngine.EngineFuelType fuelType;
			public int cylinderCount;
			public float mass;
			public float minimumRPM;
			public float redlineRPM;
			public float overRevRPM;
			public float maximumRPM;
			public float power;
			public float peakPowerRPM;
			public float torque;
			public float peakTorqueRPM;
			public AudioModule audio;

			#endregion

			#region Methods

			public readonly void SetOnSettings(int index)
			{
				SetToEngine(ref Settings.Engines[index]);
			}

			private readonly void SetToEngine(ref VehicleEngine engine)
			{
				engine.Type = type;
				engine.FuelType = fuelType;
				engine.CylinderCount = cylinderCount;
				engine.Mass = mass;
				engine.MinimumRPM = minimumRPM;
				engine.RedlineRPM = redlineRPM;
				engine.OverRevRPM = overRevRPM;
				engine.MaximumRPM = maximumRPM;
				engine.Power = power;
				engine.PeakPowerRPM = peakPowerRPM;
				engine.Torque = torque;
				engine.PeakTorqueRPM = peakTorqueRPM;
				engine.Audio = audio;
			}

			#endregion

			#region Operators

			public static implicit operator EnginePreset(VehicleEngine engine)
			{
				return new EnginePreset
				{
					name = engine.Name,
					type = engine.Type,
					fuelType = engine.FuelType,
					cylinderCount = engine.CylinderCount,
					mass = engine.Mass,
					minimumRPM = engine.MinimumRPM,
					redlineRPM = engine.RedlineRPM,
					overRevRPM = engine.OverRevRPM,
					maximumRPM = engine.MaximumRPM,
					power = engine.Power,
					peakPowerRPM = engine.PeakPowerRPM,
					torque = engine.Torque,
					peakTorqueRPM = engine.PeakTorqueRPM,
					audio = engine.Audio
				};
			}
			public static implicit operator VehicleEngine(EnginePreset preset)
			{
				VehicleEngine engine = new(preset.name);

				preset.SetToEngine(ref engine);

				return engine;
			}

			#endregion
		}
		[Serializable]
		private struct ChargerPreset
		{
			#region Modules

			[Serializable]
			public struct AudioModule
			{
				#region Variables

				public AssetIdentifier idleClip;
				public AssetIdentifier activeClip;
				public AssetIdentifier fuelBlowoutClip;
				public AssetIdentifier gearBlowoutClip;
				public AssetIdentifier revBlowoutClip;

				#endregion

				#region Operators

				public static implicit operator VehicleCharger.AudioModule(AudioModule module)
				{
					return new VehicleCharger.AudioModule
					{
						idleClip = module.idleClip.GetAsset<AudioClip>(),
						activeClip = module.activeClip.GetAsset<AudioClip>(),
						fuelBlowoutClip = module.fuelBlowoutClip.GetAsset<AudioClip>(),
						gearBlowoutClip = module.gearBlowoutClip.GetAsset<AudioClip>(),
						revBlowoutClip = module.revBlowoutClip.GetAsset<AudioClip>()
					};
				}
				public static implicit operator AudioModule(VehicleCharger.AudioModule module)
				{
					return new AudioModule
					{
						idleClip = module.idleClip,
						activeClip = module.activeClip,
						fuelBlowoutClip = module.fuelBlowoutClip,
						gearBlowoutClip = module.gearBlowoutClip,
						revBlowoutClip = module.revBlowoutClip
					};
				}

				#endregion
			}

			#endregion

			#region Variables

			public string name;
			public bool isStock;
			public float massDifference;
			public float weightDistributionDifference;
			public VehicleCharger.ChargerType type;
			public VehicleCharger.TurbochargerCount turboCount;
			public VehicleCharger.Supercharger superchargerType;
			public float minimumBoost;
			public float maximumBoost;
			public float inertiaRPM;
			public float chargerSize;
			public AudioModule audio;
			public int[] compatibleEngineIndexes;

			#endregion

			#region Methods

			public readonly void SetOnSettings(int index)
			{
				SetToCharger(ref Settings.Chargers[index]);
			}

			private readonly void SetToCharger(ref VehicleCharger charger)
			{
				charger.IsStock = isStock;
				charger.MassDifference = massDifference;
				charger.WeightDistributionDifference = weightDistributionDifference;
				charger.Type = type;
				charger.TurboCount = turboCount;
				charger.SuperchargerType = superchargerType;
				charger.MinimumBoost = minimumBoost;
				charger.MaximumBoost = maximumBoost;
				charger.InertiaRPM = inertiaRPM;
				charger.ChargerSize = chargerSize;
				charger.Audio = audio;

				charger.ResetCompatibleEngines();

				foreach (int engineIndex in compatibleEngineIndexes)
					charger.AddCompatibleEngine(engineIndex);
			}

			#endregion

			#region Operators

			public static implicit operator ChargerPreset(VehicleCharger charger)
			{
				return new ChargerPreset
				{
					name = charger.Name,
					isStock = charger.IsStock,
					massDifference = charger.MassDifference,
					weightDistributionDifference = charger.WeightDistributionDifference,
					type = charger.Type,
					turboCount = charger.TurboCount,
					superchargerType = charger.SuperchargerType,
					minimumBoost = charger.MinimumBoost,
					maximumBoost = charger.MaximumBoost,
					inertiaRPM = charger.InertiaRPM,
					chargerSize = charger.ChargerSize,
					audio = charger.Audio,
					compatibleEngineIndexes = charger.CompatibleEngineIndexes
				};
			}
			public static implicit operator VehicleCharger(ChargerPreset preset)
			{
				VehicleCharger charger = new(preset.name);

				preset.SetToCharger(ref charger);

				return charger;
			}

			#endregion
		}
		[Serializable]
		private struct TireCompoundPreset
		{
			#region Variables

			public string name;
			public VehicleTireCompound.WheelColliderFrictionCurve wheelColliderAccelerationFriction;
			public VehicleTireCompound.WheelColliderFrictionCurve wheelColliderBrakeFriction;
			public VehicleTireCompound.WheelColliderFrictionCurve wheelColliderSidewaysFriction;
			public VehicleTireCompound.WidthFrictionModifier[] widthFrictionModifiers;
			public float wetFrictionMultiplier;
			public float wetSlipMultiplier;

			#endregion

			#region Methods

			public readonly void SetOnSettings(int index)
			{
				SetToTireCompound(ref Settings.TireCompounds[index]);
			}

			private readonly void SetToTireCompound(ref VehicleTireCompound tireCompound)
			{
				tireCompound.wheelColliderAccelerationFriction = wheelColliderAccelerationFriction;
				tireCompound.wheelColliderBrakeFriction = wheelColliderBrakeFriction;
				tireCompound.wheelColliderSidewaysFriction = wheelColliderSidewaysFriction;
				tireCompound.widthFrictionModifiers = widthFrictionModifiers;
				tireCompound.wetFrictionMultiplier = wetFrictionMultiplier;
				tireCompound.wetSlipMultiplier = wetSlipMultiplier;
			}

			#endregion

			#region Operators

			public static implicit operator TireCompoundPreset(VehicleTireCompound tireCompound)
			{
				return new TireCompoundPreset
				{
					name = tireCompound.Name,
					wheelColliderAccelerationFriction = tireCompound.wheelColliderAccelerationFriction,
					wheelColliderBrakeFriction = tireCompound.wheelColliderBrakeFriction,
					wheelColliderSidewaysFriction = tireCompound.wheelColliderSidewaysFriction,
					widthFrictionModifiers = tireCompound.widthFrictionModifiers,
					wetFrictionMultiplier = tireCompound.wetFrictionMultiplier,
					wetSlipMultiplier = tireCompound.wetSlipMultiplier
				};
			}
			public static implicit operator VehicleTireCompound(TireCompoundPreset preset)
			{
				VehicleTireCompound tireCompound = new(preset.name);

				preset.SetToTireCompound(ref tireCompound);

				return tireCompound;
			}

			#endregion
		}
		[Serializable]
		private struct TireCompoundPresets
		{
			public TireCompoundPreset[] tireCompounds;
		}
		[Serializable]
		private struct GroundPresets
		{
			public GroundPreset[] grounds;
		}
		[Serializable]
		private struct GroundPreset
		{
			#region Variables

			public string name;
			public float frictionStiffness;
			public float damagedWheelStiffness;
			public float wheelDampingRate;
			public float wheelBurnoutDampingRate;
			public bool isOffRoad;
			public AssetIdentifier particleEffect;
			public AssetIdentifier flatWheelParticleEffect;
			public AssetIdentifier wheelMarkMaterial;
			public float markMaterialTiling;
			public AssetIdentifier flatWheelMarkMaterial;
			public float flatWheelMarkMaterialTiling;
			public bool useSpeedEmission;
			public AssetIdentifier forwardSkid;
			public AssetIdentifier brakingSkid;
			public AssetIdentifier sidewaysSkid;
			public AssetIdentifier rollingSound;
			public AssetIdentifier flatSkidSound;
			public AssetIdentifier flatRollingSound;
			public float volume;

			#endregion

			#region Methods

			public readonly void SetOnSettings(int index)
			{
				SetToGround(ref Settings.Grounds[index]);
			}

			private readonly void SetToGround(ref VehicleGroundMapper.GroundModule ground)
			{
				ground.FrictionStiffness = frictionStiffness;
				ground.DamagedWheelStiffness = damagedWheelStiffness;
				ground.WheelDampingRate = wheelDampingRate;
				ground.WheelBurnoutDampingRate = wheelBurnoutDampingRate;
				ground.isOffRoad = isOffRoad;
				ground.particleEffect = particleEffect.GetAsset<ParticleSystem>();
				ground.flatWheelParticleEffect = flatWheelParticleEffect.GetAsset<ParticleSystem>();
				ground.markMaterial = wheelMarkMaterial.GetAsset<Material>();
				ground.MarkMaterialTiling = markMaterialTiling;
				ground.flatWheelMarkMaterial = flatWheelMarkMaterial.GetAsset<Material>();
				ground.FlatWheelMarkMaterialTiling = flatWheelMarkMaterialTiling;
				ground.useSpeedEmission = useSpeedEmission;
				ground.forwardSkidClip = forwardSkid.GetAsset<AudioClip>();
				ground.brakeSkidClip = brakingSkid.GetAsset<AudioClip>();
				ground.sidewaysSkidClip = sidewaysSkid.GetAsset<AudioClip>();
				ground.rollClip = rollingSound.GetAsset<AudioClip>();
				ground.flatSkidClip = flatSkidSound.GetAsset<AudioClip>();
				ground.flatRollClip = flatRollingSound.GetAsset<AudioClip>();
				ground.Volume = volume;
			}

			#endregion

			#region Operators

			public static implicit operator GroundPreset(VehicleGroundMapper.GroundModule module)
			{
				return new GroundPreset
				{
					name = module.Name,
					frictionStiffness = module.FrictionStiffness,
					damagedWheelStiffness = module.DamagedWheelStiffness,
					wheelDampingRate = module.WheelDampingRate,
					wheelBurnoutDampingRate = module.WheelBurnoutDampingRate,
					isOffRoad = module.isOffRoad,
					particleEffect = module.particleEffect,
					flatWheelParticleEffect = module.flatWheelParticleEffect,
					wheelMarkMaterial = module.flatWheelMarkMaterial,
					markMaterialTiling = module.MarkMaterialTiling,
					flatWheelMarkMaterial = module.flatWheelMarkMaterial,
					flatWheelMarkMaterialTiling = module.FlatWheelMarkMaterialTiling,
					useSpeedEmission = module.useSpeedEmission,
					forwardSkid = module.forwardSkidClip,
					brakingSkid = module.brakeSkidClip,
					sidewaysSkid = module.sidewaysSkidClip,
					rollingSound = module.rollClip,
					flatSkidSound = module.flatSkidClip,
					flatRollingSound = module.flatRollClip,
					volume = module.Volume
				};
			}
			public static implicit operator VehicleGroundMapper.GroundModule(GroundPreset preset)
			{
				VehicleGroundMapper.GroundModule ground = new(preset.name);

				preset.SetToGround(ref ground);

				return ground;
			}

			#endregion
		}
		[Serializable]
		private struct AssetIdentifier
		{
			#region Variables

			public string path;
			public string guid;

			#endregion

			#region Methods

			public readonly T GetAsset<T>() where T : Object
			{
				if (guid.IsNullOrEmpty() && path.IsNullOrEmpty())
					return null;

				T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));

				if (asset)
					return asset;

				asset = AssetDatabase.LoadAssetAtPath<T>(path);

				if (asset)
					return asset;

				string[] paths = AssetDatabase.FindAssets($"t:{nameof(T)}");
				string name = Path.GetFileNameWithoutExtension(path).ToLower();

				foreach (string path in paths)
					if (Path.GetFileNameWithoutExtension(path).ToLower() == name)
					{
						asset = AssetDatabase.LoadAssetAtPath<T>(path);

						break;
					}

				return asset;
			}
			public readonly bool TryGetAsset<T>(out T asset) where T : Object
			{
				asset = GetAsset<T>();

				return asset;
			}

			#endregion

			#region Operators

			public static implicit operator AssetIdentifier(Object @object)
			{
				if (!@object)
					return default;

				string path = AssetDatabase.GetAssetPath(@object);

				return new AssetIdentifier
				{
					path = path,
					guid = AssetDatabase.AssetPathToGUID(path)
				};
			}

			#endregion
		}

		#endregion

		#region Variables

		#region Static Variables

		internal static ToolkitSettingsEditorWindow instance;

		private static ReorderableList reorderableList;
		private static GUIStyle scrollViewStyle;

		#endregion

		#region Global Variables

		private CameraPreset[] importExportCamerasList = null;
		private EnginePreset[] importExportEnginesList = null;
		private ChargerPreset[] importExportChargersList = null;
		private TireCompoundPreset[] importExportTireCompoundsList = null;
		private GroundPreset[] importExportGroundsList = null;
		private bool[] importExportCamerasStatesList = null;
		private bool[] importExportEnginesStatesList = null;
		private bool[] importExportChargersStatesList = null;
		private bool[] importExportTireCompoundsStatesList = null;
		private bool[] importExportGroundsStatesList = null;
		private SerializedObject serializedObject;
		private Vector2 scrollView;
		//private ToolkitSettings.MaterialProperty newAddWheelSpinMeshMaterialOtherProperty;
		//private string newAddWheelSpinMeshMaterialKeyword;
		private string cameraName;
		private string engineName;
		private string chargerName;
#if !MVC_COMMUNITY
		private string tireCompoundName;
		private string groundName;
#endif
		private string errorSmiley;
		public string importPresetsJson;
		public bool exportPresets;
		private int currentCamera = -1;
		private int currentEngine = -1;
		private int currentCharger = -1;
#if !MVC_COMMUNITY
		private int currentTireCompound = -1;
		private int currentGround = -1;
#endif

		#endregion

		#endregion

		#region Methods

		#region Static Methods

		[MenuItem("Tools/Multiversal Vehicle Controller/Help/About", false, 10)]
		private static void AboutPopup()
		{
			switch (EditorUtility.DisplayDialogComplex("Multiversal Vehicle Controller: Info", $"Multiversal Vehicle Controller™ (MVC)\r\n" +
				$"License: {ToolkitInfo.License}{(ToolkitInfo.License < ToolkitInfo.LicenseType.Pro ? " (Personal Use Only)" : "")}\r\n" +
				$"Version: {ToolkitInfo.Version}\r\n" +
				"All Copyrights Reserved to BxB Studio © 2025", "Read more...", "Close", "Check for updates"))
			{
				case 0:
					About();

					break;

				case 2:
					ToolkitUpdate.CheckForUpdates(true, false);

					break;
			}
		}

		#endregion

		#region Global Methods

		#region Utilities

		private void EnableFoldout(ToolkitSettings.SettingsEditorFoldout foldout, bool recordUndo)
		{
			if (recordUndo)
				Undo.RegisterCompleteObjectUndo(Settings, "Change Tab");

			if (Settings.settingsFoldout == foldout)
				Settings.settingsFoldout = default;
			else
				Settings.settingsFoldout = foldout;

			importPresetsJson = default;
			exportPresets = default;
			currentCamera = -1;
			currentEngine = -1;
			currentCharger = -1;
#if !MVC_COMMUNITY
			currentGround = -1;
#endif

			if (recordUndo)
				EditorUtility.SetDirty(Settings);
		}
		private void EnableSFXFoldout(ToolkitSettings.SettingsEditorSFXFoldout sfxFoldout, bool recordUndo)
		{
			if (recordUndo)
				Undo.RegisterCompleteObjectUndo(Settings, "Change Tab");

			Settings.soundEffectsFoldout = sfxFoldout;

			if (recordUndo)
				EditorUtility.SetDirty(Settings);
		}
		private bool ImportPresets<T>(out T presets)
		{
			ExitImportExportPresets();

			presets = default;

			string importPath = EditorUtility.OpenFilePanel("Importing Presets...", string.Empty, "json");

			if (importPath.IsNullOrWhiteSpace())
				return false;

			if (!File.Exists(importPath))
			{
				ToolkitDebug.Error("The selected file has been moved or removed.");

				return false;
			}

			string json = File.ReadAllText(importPath, System.Text.Encoding.UTF8);

			if (json.IsNullOrWhiteSpace())
			{
				ToolkitDebug.Error("The imported file is invalid!");

				return false;
			}

			importPresetsJson = json;
			presets = JsonUtility.FromJson<T>(json);

			return true;
		}
		private void ImportCameraPresets()
		{
			if (!ImportPresets(out CameraPresets presets))
				return;

			if (presets.cameras == null || presets.cameras.Length < 1)
			{
				ToolkitDebug.Error("The imported file is invalid!");

				importPresetsJson = default;

				return;
			}

			importExportCamerasList = presets.cameras;
			importExportCamerasStatesList = Enumerable.Repeat(true, presets.cameras.Length).ToArray();
		}
		private void ImportEngineChargerPresets()
		{
			if (!ImportPresets(out EngineChargerPresets presets))
				return;

			if (presets.engines == null && presets.chargers == null || presets.engines == null && presets.chargers.Length < 1 || presets.chargers == null && presets.engines.Length < 1 || presets.engines.Length < 1 && presets.chargers.Length < 1)
			{
				ToolkitDebug.Error("The imported file is invalid!");

				importPresetsJson = default;

				return;
			}

			importExportEnginesList = presets.engines;
			importExportEnginesStatesList = Enumerable.Repeat(true, presets.engines.Length).ToArray();
			importExportChargersList = presets.chargers;
			importExportChargersStatesList = Enumerable.Repeat(true, presets.chargers.Length).ToArray();
		}
		private void ImportTireCompoundPresets()
		{
			if (!ImportPresets(out TireCompoundPresets presets))
				return;

			if (presets.tireCompounds == null || presets.tireCompounds.Length < 1)
			{
				ToolkitDebug.Error("The imported file is invalid!");

				importPresetsJson = default;

				return;
			}

			importExportTireCompoundsList = presets.tireCompounds;
			importExportTireCompoundsStatesList = Enumerable.Repeat(true, presets.tireCompounds.Length).ToArray();
		}
		private void ImportGroundPresets()
		{
			if (!ImportPresets(out GroundPresets presets))
				return;

			if (presets.grounds == null || presets.grounds.Length < 1)
			{
				ToolkitDebug.Error("The imported file is invalid!");

				importPresetsJson = default;

				return;
			}

			importExportGroundsList = presets.grounds;
			importExportGroundsStatesList = Enumerable.Repeat(true, presets.grounds.Length).ToArray();
		}
		private void FinishImportPresets()
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Import Presets");

			bool overrideAll = false;
			bool overrideThis;
			int index;

			if (importExportCamerasList != null)
				for (int i = 0; i < importExportCamerasList.Length; i++)
				{
					if (!importExportCamerasStatesList[i])
						continue;

					CameraPreset cameraPreset = importExportCamerasList[i];

					index = Array.FindIndex(Settings.Cameras, c => c.Name == cameraPreset.name);
					overrideThis = overrideAll || index < 0;

					if (!overrideThis)
					{
						int overrideChoice = EditorUtility.DisplayDialogComplex("Importing Presets...", $"The camera \"{cameraPreset.name}\" does already exist! Select an action to continue", "Override", "Ignore", "Override All");

						overrideAll = overrideChoice == 2;
						overrideThis = overrideAll || overrideChoice == 0;
					}

					if (index > -1)
						cameraPreset.SetOnSettings(index);
					else
						Settings.AddCamera(cameraPreset);
				}
			else if (importExportEnginesList != null && importExportChargersList != null)
			{
				for (int i = 0; i < importExportEnginesList.Length; i++)
				{
					if (!importExportEnginesStatesList[i])
						continue;

					EnginePreset enginePreset = importExportEnginesList[i];

					index = Array.FindIndex(Settings.Engines, e => e.Name == enginePreset.name);
					overrideThis = overrideAll || index < 0;

					if (!overrideThis)
					{
						int overrideChoice = EditorUtility.DisplayDialogComplex("Importing Presets...", $"The engine \"{enginePreset.name}\" does already exist! Select an action to continue", "Override", "Ignore", "Override All");

						overrideAll = overrideChoice == 2;
						overrideThis = overrideAll || overrideChoice == 0;
					}

					if (index > -1)
						enginePreset.SetOnSettings(index);
					else
						Settings.AddEngine(enginePreset);
				}

				for (int i = 0; i < importExportChargersList.Length; i++)
				{
					if (!importExportChargersStatesList[i])
						continue;

					ChargerPreset chargerPreset = importExportChargersList[i];

					index = Array.FindIndex(Settings.Chargers, c => c.Name == chargerPreset.name);
					overrideThis = overrideAll || index < 0;

					if (!overrideThis)
					{
						int overrideChoice = EditorUtility.DisplayDialogComplex("Importing Presets...", $"The charger \"{chargerPreset.name}\" does already exist! Select an action to continue", "Override", "Ignore", "Override All");

						overrideAll = overrideChoice == 2;
						overrideThis = overrideAll || overrideChoice == 0;
					}

					if (index > -1)
						chargerPreset.SetOnSettings(index);
					else
						Settings.AddCharger(chargerPreset);
				}
			}
			else if (importExportTireCompoundsList != null)
				for (int i = 0; i < importExportTireCompoundsList.Length; i++)
				{
					if (!importExportTireCompoundsStatesList[i])
						continue;

					TireCompoundPreset tireCompoundPreset = importExportTireCompoundsList[i];

					index = Array.FindIndex(Settings.TireCompounds, g => g.Name == tireCompoundPreset.name);
					overrideThis = overrideAll || index < 0;

					if (!overrideThis)
					{
						int overrideChoice = EditorUtility.DisplayDialogComplex("Importing Presets...", $"The tire compound \"{tireCompoundPreset.name}\" does already exist! Select an action to continue", "Override", "Ignore", "Override All");

						overrideAll = overrideChoice == 2;
						overrideThis = overrideAll || overrideChoice == 0;
					}

					if (index > -1)
						tireCompoundPreset.SetOnSettings(index);
					else
						Settings.AddTireCompound(tireCompoundPreset);
				}
			else if (importExportGroundsList != null)
				for (int i = 0; i < importExportGroundsList.Length; i++)
				{
					if (!importExportGroundsStatesList[i])
						continue;

					GroundPreset groundPreset = importExportGroundsList[i];

					index = Array.FindIndex(Settings.Grounds, g => g.Name == groundPreset.name);
					overrideThis = overrideAll || index < 0;

					if (!overrideThis)
					{
						int overrideChoice = EditorUtility.DisplayDialogComplex("Importing Presets...", $"The ground \"{groundPreset.name}\" does already exist! Select an action to continue", "Override", "Ignore", "Override All");

						overrideAll = overrideChoice == 2;
						overrideThis = overrideAll || overrideChoice == 0;
					}

					if (index > -1)
						groundPreset.SetOnSettings(index);
					else
						Settings.AddGround(groundPreset);
				}

			EditorUtility.SetDirty(Settings);
			ExitImportExportPresets();
		}
		private void ExportPresets()
		{
			exportPresets = true;

			switch (Settings.settingsFoldout)
			{
				case ToolkitSettings.SettingsEditorFoldout.Cameras:
					importExportCamerasList = Settings.Cameras.Select(camera => (CameraPreset)camera).ToArray();
					importExportCamerasStatesList = Enumerable.Repeat(true, importExportCamerasList.Length).ToArray();

					break;

				case ToolkitSettings.SettingsEditorFoldout.EnginesChargers:
					importExportEnginesList = Settings.Engines.Select(engine => (EnginePreset)engine).ToArray();
					importExportChargersList = Settings.Chargers.Select(charger => (ChargerPreset)charger).ToArray();
					importExportEnginesStatesList = Enumerable.Repeat(true, importExportEnginesList.Length).ToArray();
					importExportChargersStatesList = Enumerable.Repeat(true, importExportChargersList.Length).ToArray();

					break;

				case ToolkitSettings.SettingsEditorFoldout.TireCompounds:
					importExportTireCompoundsList = Settings.TireCompounds.Select(tireCompound => (TireCompoundPreset)tireCompound).ToArray();
					importExportTireCompoundsStatesList = Enumerable.Repeat(true, importExportTireCompoundsList.Length).ToArray();

					break;

				case ToolkitSettings.SettingsEditorFoldout.Grounds:
					importExportGroundsList = Settings.Grounds.Select(ground => (GroundPreset)ground).ToArray();
					importExportGroundsStatesList = Enumerable.Repeat(true, importExportGroundsList.Length).ToArray();

					break;
			}
		}
		private void FinishExportPresets<T>() where T : struct
		{
			EditorUtility.DisplayProgressBar("Exporting Presets...", "Generating Json...", 0f);

			string json = Settings.settingsFoldout switch
			{
				ToolkitSettings.SettingsEditorFoldout.Cameras => JsonUtility.ToJson(new CameraPresets
				{
					cameras = importExportCamerasList.Where((_, index) => importExportCamerasStatesList[index]).ToArray()
				}, true),
				ToolkitSettings.SettingsEditorFoldout.EnginesChargers => JsonUtility.ToJson(new EngineChargerPresets
				{
					engines = importExportEnginesList.Where((_, index) => importExportEnginesStatesList[index]).ToArray(),
					chargers = importExportChargersList.Where((_, index) => importExportChargersStatesList[index]).ToArray()
				}, true),
				ToolkitSettings.SettingsEditorFoldout.TireCompounds => JsonUtility.ToJson(new TireCompoundPresets
				{
					tireCompounds = importExportTireCompoundsList.Where((_, index) => importExportTireCompoundsStatesList[index]).ToArray(),
				}, true),
				ToolkitSettings.SettingsEditorFoldout.Grounds => JsonUtility.ToJson(new GroundPresets
				{
					grounds = importExportGroundsList.Where((_, index) => importExportGroundsStatesList[index]).Select(ground => (GroundPreset)ground).ToArray()
				}, true),
				_ => default
			};

			EditorUtility.ClearProgressBar();

			if (json.IsNullOrWhiteSpace())
			{
				ToolkitDebug.Error("We've had some errors while generating a json script for the selected items.");

				return;
			}

			string exportPath = EditorUtility.SaveFilePanel("Exporting Preset...", string.Empty, $"{typeof(T).Name}_{DateTime.Now:yyyy-MM-dd}.json", "json");

			if (exportPath.IsNullOrWhiteSpace())
				return;

			EditorUtility.DisplayProgressBar("Exporting Preset...", "Saving file...", 1f);

		retry:
			StreamWriter stream = null;

			try
			{
				string renamePath = default;

				for (int i = 1; renamePath.IsNullOrEmpty() || File.Exists(renamePath); i++)
					renamePath = Path.Combine(Path.GetDirectoryName(exportPath), $"{Path.GetFileNameWithoutExtension(exportPath)}_{i}{Path.GetExtension(exportPath)}");

				bool replaceFile = File.Exists(exportPath);

				if (replaceFile)
					File.Move(exportPath, renamePath);

				stream = File.CreateText(exportPath);

				for (int i = 0; i < json.Length; i++)
					stream.Write(json[i]);

				if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The preset json file has been saved successfully!", "Close"))
					return;

				if (replaceFile)
					File.Delete(renamePath);
			}
			catch (Exception e)
			{
				if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "We've had some errors while saving your file...", "Retry", "Cancel"))
					goto retry;

				throw e;
			}
			finally
			{
				stream?.Close();
				EditorUtility.ClearProgressBar();
			}

			ExitImportExportPresets();
		}
		private void ExitImportExportPresets()
		{
			importPresetsJson = default;
			exportPresets = default;
			importExportCamerasList = null;
			importExportCamerasStatesList = null;
			importExportEnginesList = null;
			importExportEnginesStatesList = null;
			importExportChargersList = null;
			importExportChargersStatesList = null;
			importExportTireCompoundsList = null;
			importExportTireCompoundsStatesList = null;
			importExportGroundsList = null;
			importExportGroundsStatesList = null;
		}
		private void AddCamera()
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Add New Camera");
			Settings.AddCamera(new("New Camera"));
			EditorUtility.SetDirty(Settings);
		}
		private void EditCamera(int index)
		{
			currentCamera = index;
			cameraName = Settings.Cameras[index].Name;
		}
		private void RemoveCamera(int index)
		{
			if (Settings.Cameras.Length < 2)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You are unable to remove the last remaining camera because the MVC physics system depends on it to work properly!", "Okay");

				return;
			}

			Undo.RegisterCompleteObjectUndo(Settings, "Remove Camera");
			Settings.RemoveCamera(index);
			EditorUtility.SetDirty(Settings);
		}
		private void DuplicateCamera(int index)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Duplicate Camera");
			Settings.AddCamera(new(Settings.Cameras[index]));
			EditorUtility.SetDirty(Settings);
		}
		private void MoveCamera(int index, int newIndex)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Sort Cameras");
			Settings.MoveCamera(index, newIndex);
			EditorUtility.SetDirty(Settings);
		}
		private void SaveCamera()
		{
			if (currentCamera < -1)
				currentCamera = -1;

			if (currentCamera < 0 || currentCamera >= Settings.Cameras.Length)
				return;

			if (cameraName.IsNullOrEmpty() || cameraName.IsNullOrWhiteSpace())
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "A valid camera name is required!", "Okay");
			else if (Settings.Cameras.Where(camera => camera.Name == cameraName && camera != Settings.Cameras[currentCamera]).Count() > 0)
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "The camera name does exist already!", "Okay");
			else
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Camera");

				Settings.Cameras[currentCamera].Name = cameraName;
				cameraName = string.Empty;
				currentCamera = -1;

				EditorUtility.SetDirty(Settings);
			}
		}
		private void SortCameras()
		{
			currentCamera = -2;
		}
		private void AddEngine()
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Add New Engine");
			Settings.AddEngine(new("New Engine"));
			EditorUtility.SetDirty(Settings);
		}
		private void EditEngine(int index)
		{
			currentEngine = index;
			engineName = Settings.Engines[index].Name;
		}
		private void DuplicateEngine(int index)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Duplicate Engine");
			Settings.AddEngine(new(Settings.Engines[index]));
			EditorUtility.SetDirty(Settings);
		}
		private void MoveEngine(int index, int newIndex)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Sort Engines");
			Settings.MoveEngine(index, newIndex);
			EditorUtility.SetDirty(Settings);
		}
		private void RemoveEngine(int index)
		{
			if (Settings.Engines.Length < 2)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You are unable to remove the last remaining engine because the MVC systems depends on it to work properly!", "Okay");

				return;
			}

			Undo.RegisterCompleteObjectUndo(Settings, "Remove Engine");
			Settings.RemoveEngine(index);
			EditorUtility.SetDirty(Settings);
		}
		private void SaveEngine()
		{
			if (currentEngine < -1)
				currentEngine = -1;

			if (currentEngine < 0 || currentEngine >= Settings.Engines.Length)
				return;

			if (engineName.IsNullOrEmpty() || engineName.IsNullOrWhiteSpace())
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "A valid engine name is required!", "Okay");
			else if (Settings.Engines.Where(engine => engine.Name == engineName && engine != Settings.Engines[currentEngine]).Count() > 0)
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "The engine name does exist already!", "Okay");
			else
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Engine");

				Settings.Engines[currentEngine].Name = engineName;
				engineName = string.Empty;
				currentEngine = -1;

				EditorUtility.SetDirty(Settings);
			}
		}
		private void AddCharger()
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Add New Charger");
			Settings.AddCharger(new("New Charger"));
			EditorUtility.SetDirty(Settings);
		}
		private void AddChargerEngine(VehicleCharger charger, int engineIndex)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Add Engine");
			charger.AddCompatibleEngine(engineIndex);
			EditorUtility.SetDirty(Settings);
		}
		private void EditCharger(int index)
		{
			currentCharger = index;
			chargerName = Settings.Chargers[index].Name;
		}
		private void DuplicateCharger(int index)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Duplicate Charger");
			Settings.AddCharger(new(Settings.Chargers[index]));
			EditorUtility.SetDirty(Settings);
		}
		private void MoveCharger(int index, int newIndex)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Sort Chargers");
			Settings.MoveCharger(index, newIndex);
			EditorUtility.SetDirty(Settings);
		}
		private void RemoveCharger(int index)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Remove Charger");
			Settings.RemoveCharger(index);
			EditorUtility.SetDirty(Settings);
		}
		private void RemoveChargerEngine(VehicleCharger charger, int index)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Remove Engine");
			charger.RemoveCompatibleEngineAtIndex(index);
			EditorUtility.SetDirty(Settings);
		}
		private void SaveCharger()
		{
			if (currentCharger < -1)
				currentCharger = -1;

			if (currentCharger < 0 || currentCharger >= Settings.Chargers.Length)
				return;

			if (chargerName.IsNullOrEmpty() || chargerName.IsNullOrWhiteSpace())
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "A valid engine name is required!", "Okay");
			else if (Settings.Chargers.Where(charger => charger.Name == chargerName && charger != Settings.Chargers[currentCharger]).Count() > 0)
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "The engine name does exist already!", "Okay");
			else
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Charger");

				Settings.Chargers[currentCharger].Name = chargerName;
				chargerName = default;
				currentCharger = -1;

				EditorUtility.SetDirty(Settings);
			}
		}
		private void SortEnginesAndChargers()
		{
			currentEngine = -2;
			currentCharger = -2;
		}
#if !MVC_COMMUNITY
		private void AddTireCompound()
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Add Tire Compound");
			Settings.AddTireCompound(new("New Tire Compound"));
			EditorUtility.SetDirty(Settings);
		}
		private void EditTireCompound(int index)
		{
			currentTireCompound = index;
			tireCompoundName = Settings.TireCompounds[index].Name;
		}
		private void RemoveTireCompound(int index)
		{
			if (Settings.TireCompounds.Length < 2)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You are unable to remove the last remaining tire compound because the MVC physics system depends on it to work properly!", "Okay");

				return;
			}

			Undo.RegisterCompleteObjectUndo(Settings, "Remove Tire Compound");
			Settings.RemoveTireCompound(index);
			EditorUtility.SetDirty(Settings);
		}
		private void DuplicateTireCompound(int index)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Duplicate Tire Compound");
			Settings.AddTireCompound(new(Settings.TireCompounds[index]));
			EditorUtility.SetDirty(Settings);
		}
		private void MoveTireCompound(int index, int newIndex)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Sort Tire Compounds");
			Settings.MoveTireCompound(index, newIndex);
			EditorUtility.SetDirty(Settings);
		}
		private void SaveTireCompound()
		{
			if (currentTireCompound < -1)
				currentTireCompound = -1;

			reorderableList = null;

			if (currentTireCompound < 0 || currentTireCompound >= Settings.TireCompounds.Length)
				return;

			if (tireCompoundName.IsNullOrEmpty() || tireCompoundName.IsNullOrWhiteSpace())
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "A valid tire compound name is required!", "Okay");
			else if (Settings.TireCompounds.Where(tireCompound => tireCompound.Name == tireCompoundName && tireCompound != Settings.TireCompounds[currentTireCompound]).Count() > 0)
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "The tire compound name does exist already!", "Okay");
			else
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Tire Compound");

				Settings.TireCompounds[currentTireCompound].Name = tireCompoundName;
				tireCompoundName = string.Empty;
				currentTireCompound = -1;

				EditorUtility.SetDirty(Settings);
			}
		}
		private void SortTireCompounds()
		{
			currentTireCompound = -2;
		}
		private void AddGround()
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Add Ground");
			Settings.AddGround(new("New Ground"));
			EditorUtility.SetDirty(Settings);
		}
		private void EditGround(int index)
		{
			currentGround = index;
			groundName = Settings.Grounds[index].Name;
		}
		private void RemoveGround(int index)
		{
			if (Settings.Grounds.Length < 2)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You are unable to remove the last remaining ground because the MVC physics system depends on it to work properly!", "Okay");

				return;
			}

			Undo.RegisterCompleteObjectUndo(Settings, "Remove Ground");
			Settings.RemoveGround(index);
			EditorUtility.SetDirty(Settings);
		}
		private void DuplicateGround(int index)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Duplicate Ground");
			Settings.AddGround(new(Settings.Grounds[index]));
			EditorUtility.SetDirty(Settings);
		}
		private void MoveGround(int index, int newIndex)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Sort Grounds");
			Settings.MoveGround(index, newIndex);
			EditorUtility.SetDirty(Settings);
		}
		private void SaveGround()
		{
			if (currentGround < -1)
				currentGround = -1;

			if (currentGround < 0 || currentGround >= Settings.Grounds.Length)
				return;

			if (groundName.IsNullOrEmpty() || groundName.IsNullOrWhiteSpace())
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "A valid ground name is required!", "Okay");
			else if (Settings.Grounds.Where(ground => ground.Name == groundName && ground != Settings.Grounds[currentGround]).Count() > 0)
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "The ground name does exist already!", "Okay");
			else
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Ground");

				Settings.Grounds[currentGround].Name = groundName;
				groundName = string.Empty;
				currentGround = -1;

				EditorUtility.SetDirty(Settings);
			}
		}
		private void SortGrounds()
		{
			currentGround = -2;
		}
#endif
		private void RandomizeErrorSmiley()
		{
			string[] smileys = new string[]
			{
				"¯\\_(ʘ_ʘ)_/¯",
				"(・_・)",
				"(˘_˘٥)",
				"(✖╭╮✖)",
				"へ（>_<へ)",
				"¯\\_(Ω_Ω)_/¯",
				"(´･_･`)",
				"(-_-)ゞ",
				"(・_・)ゞ",
				"「(°ヘ°)",
				"ヽ(ಠ_ಠ)ノ",
				"┌(ಠ_ಠ)ノ",
				"¯\\_(´・_・`)_/¯"
			};
			string newSmiley = smileys[UnityEngine.Random.Range(0, smileys.Length)];

			while (errorSmiley == newSmiley)
				newSmiley = smileys[UnityEngine.Random.Range(0, smileys.Length)];

			errorSmiley = newSmiley;
		}
		private void TryInputsManagerQuickFix()
		{
			InputsManagerEditor.OpenWindow();

			if (InputsManager.DataAssetExists)
				InputsManagerEditor.CloseWindow();
		}

		#endregion

		#region Editor

		private void OnGUI()
		{
			if (EditorApplication.isCompiling)
			{
				EditorGUI.LabelField(new(position.width * .5f - 256f, position.height * .5f - EditorGUIUtility.singleLineHeight * 1.25f, 512f, EditorGUIUtility.singleLineHeight * 2.5f), "Please wait...\r\nIt seems like something is compiling...", new GUIStyle(EditorStyles.boldLabel) { fontSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight), alignment = TextAnchor.MiddleCenter });
				Repaint();

				return;
			}
			else if (!ToolkitInfo.IsSetupDone)
			{
				if (errorSmiley.IsNullOrEmpty())
					RandomizeErrorSmiley();

				EditorGUI.LabelField(new(position.width * .5f - 400f, position.height * .5f - 320f, 800f, 512f), errorSmiley, new GUIStyle(EditorStyles.boldLabel) { fontSize = 72, alignment = TextAnchor.MiddleCenter });
				EditorGUI.LabelField(new(position.width * .5f - 256f, position.height * .5f + 48f, 512f, EditorGUIUtility.singleLineHeight * 2.5f), "In order to use the Multiversal Vehicle Controller,\r\nyou have to finish the setup process first!", new GUIStyle(EditorStyles.boldLabel) { fontSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight), alignment = TextAnchor.MiddleCenter });
				GUILayout.FlexibleSpace();
				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Let me see!", new GUIStyle(GUI.skin.button) { fixedWidth = 256f, fixedHeight = EditorGUIUtility.singleLineHeight * 2f }))
				{
					ToolkitSetupWizardWindow.OpenWindow();
					Close();
				}

				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(position.height * .5f - 144f);

				return;
			}
			else if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
				ToolkitSettingsEditor.SaveSettings();

			ToolbarEditor();

			if (HasInternalErrors)
			{
				EditorGUI.DrawPreviewTexture(new(position.width * .5f - 64f, position.height * .5f - 96f, 128f, 128f), EditorUtilities.Icons.Error, new Material(Shader.Find("Unlit/Transparent")));
				EditorGUI.LabelField(new(position.width * .5f - 256f, position.height * .5f + 48f, 512f, EditorGUIUtility.singleLineHeight * 2.5f), "It seems like the Multiversal Vehicle Controller\r\nhas some internal issues!", new GUIStyle(EditorStyles.boldLabel) { fontSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight), alignment = TextAnchor.MiddleCenter });
				GUILayout.FlexibleSpace();
				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Try some quick fixes!", new GUIStyle(GUI.skin.button) { fixedWidth = 256f, fixedHeight = EditorGUIUtility.singleLineHeight * 2f }))
					ToolkitBehaviourEditor.FixInternalProblems(Selection.activeObject);

				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(position.height * .5f - 144f);

				return;
			}
			else if (Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.License)
			{
				LicenseEditor();

				return;
			}

			EditorGUILayout.BeginHorizontal(GUILayout.Width(position.width), GUILayout.Height(position.height));

			bool showNavEditor = Settings.settingsFoldout != ToolkitSettings.SettingsEditorFoldout.TireCompounds && Settings.settingsFoldout != ToolkitSettings.SettingsEditorFoldout.SFX ||
#if !MVC_COMMUNITY
				Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.TireCompounds && currentTireCompound < 0 ||
#endif
				Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.SFX && (Settings.soundEffectsFoldout == ToolkitSettings.SettingsEditorSFXFoldout.None || Settings.soundEffectsFoldout == ToolkitSettings.SettingsEditorSFXFoldout.Exhaust || /*Settings.soundEffectsFoldout == ToolkitSettings.SettingsEditorSFXFoldout.Exterior || */Settings.soundEffectsFoldout == ToolkitSettings.SettingsEditorSFXFoldout.Gears || /*Settings.soundEffectsFoldout == ToolkitSettings.SFXFoldout.Horns || */Settings.soundEffectsFoldout == ToolkitSettings.SettingsEditorSFXFoldout.Transmission || Settings.soundEffectsFoldout == ToolkitSettings.SettingsEditorSFXFoldout.NOS);

			if (showNavEditor)
				NavEditor();

			ContainerEditor(showNavEditor);
			EditorGUILayout.EndHorizontal();
		}
		private void LicenseEditor()
		{
			string license = ToolkitInfo.License.ToString();

			if (license.Length > 1)
			{
				string capital = license.Remove(1).ToUpper();
				string word = license.Remove(0, 1).ToLower();

				license = capital + word;
			}
			else
				license = ToolkitInfo.License.ToString().ToUpper();

			EditorGUI.DrawPreviewTexture(new(position.width * .5f - 64f, position.height * .5f - 96f, 128f, 128f), EditorUtilities.Icons.CheckCircle, new Material(Shader.Find("Unlit/Transparent")));
			EditorGUI.LabelField(new(position.width * .5f - 256f, position.height * .5f + 48f, 512f, EditorGUIUtility.singleLineHeight * 2.5f), $"You are currently using the {license} version\r\nof the Multiversal Vehicle Controller.", new GUIStyle(EditorStyles.boldLabel)
			{
				fontSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight),
				alignment = TextAnchor.MiddleCenter
			});
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
#if MVC_COMMUNITY

			if (GUILayout.Button("Upgrade to Pro...", new GUIStyle(GUI.skin.button) { fixedWidth = 256f, fixedHeight = EditorGUIUtility.singleLineHeight * 2f }))
				ToolkitBehaviourEditor.BuyLicense();
#endif

			if (GUILayout.Button("Go to Main Panel", new GUIStyle(GUI.skin.button) { fixedWidth = 256f, fixedHeight = EditorGUIUtility.singleLineHeight * 2f }))
			{
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.None, false);

				Settings.soundEffectsFoldout = ToolkitSettings.SettingsEditorSFXFoldout.None;
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(position.height * .5f - 144f);
		}
		private void ToolbarEditor()
		{
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Width(position.width));

			if (Settings)
			{
				EditorGUI.BeginDisabledGroup(!EditorUtility.IsDirty(Settings));

				if (GUILayout.Button("Save", EditorStyles.toolbarButton))
					ToolkitSettingsEditor.SaveSettings();

				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
				EditorGUI.BeginDisabledGroup(HasInternalErrors);

				if (GUILayout.Button("Reset", EditorStyles.toolbarButton))
					ToolkitSettingsEditor.ResetSettings();

				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(HasInternalErrors && !ToolkitBehaviourEditor.BackupAsset);

				if (ToolkitBehaviourEditor.BackupAsset)
				{
					if (GUILayout.Button("Restore Backup", EditorStyles.toolbarButton))
						ToolkitSettingsEditor.RestoreSettings();
				}
				else if (GUILayout.Button("Create Backup", EditorStyles.toolbarButton))
					ToolkitSettingsEditor.BackupSettings();

				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
			}

			if (GUILayout.Button("Open Documentation", EditorStyles.toolbarButton))
				VisitDocumentation();

			if (GUILayout.Button("Tutorials", EditorStyles.toolbarButton))
				OpenTutorialsPlaylist();

			if (GUILayout.Button("Help", EditorStyles.toolbarButton))
				switch (EditorUtility.DisplayDialogComplex("Multiversal Vehicle Controller", "Do you want some help? ", "Contact us", "Close", "Report Error"))
				{
					case 0:
						ContactUs();

						break;

					case 2:
						ReportError();

						break;
				}

			if (GUILayout.Button("About", EditorStyles.toolbarButton))
				AboutPopup();

			GUILayout.FlexibleSpace();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (IsSetupDone && Settings)
			{
				if (ToolkitPrefs.HasFlag(ToolkitConstants.HasUpdate))
					if (GUILayout.Button("Update Available", EditorStyles.toolbarButton))
						ToolkitUpdate.CheckForUpdates(true, true);

				if (Settings.settingsFoldout != ToolkitSettings.SettingsEditorFoldout.License)
					if (GUILayout.Button(
#if !MVC_COMMUNITY
						ToolkitInfo.License > ToolkitInfo.LicenseType.Community ? "Check License" :
#endif
						"Upgrade to Pro!", EditorStyles.toolbarButton))
						EnableFoldout(ToolkitSettings.SettingsEditorFoldout.License, false);
			}

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (EditorApplication.isPlaying)
			{
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Width(position.width));
				EditorGUILayout.LabelField("You're currently in play mode. Some settings might be disabled and can't be changed.", EditorStyles.miniLabel, GUILayout.Width(position.width - 200f));
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Exit Play Mode", EditorStyles.toolbarButton))
					EditorApplication.isPlaying = false;

				EditorGUILayout.EndHorizontal();
			}
		}
		private void NavEditor()
		{
			GUIStyle navStyle = new(EditorStyles.toolbar)
			{
				fixedWidth = 256f,
				stretchHeight = true,
				fixedHeight = position.height - EditorStyles.toolbarTextField.fixedHeight
			};
			GUIStyle navButtonStyle = new(GUI.skin.button)
			{
				fixedHeight = 25f
			};
			bool settingsGeneralFoldout = Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.General;
#if !MVC_COMMUNITY
			bool settingsAIFoldout = Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.AI;
#endif
			bool settingsBehaviourFoldout = Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.Behaviour;
			bool settingsDamageFoldout = Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.Damage;
			bool settingsCamerasFoldout = Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.Cameras;
			bool settingsInputsFoldout = Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.PlayerInputs;
			bool settingsEnginesChargersFoldout = Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.EnginesChargers;
			bool settingsTireCompoundsFoldout = Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.TireCompounds;
			bool settingsGroundsFoldout = Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.Grounds;
			bool settingsSFXFoldout = Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.SFX;
			bool settingsVFXFoldout = Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.VFX;
			bool settingsEditorFoldout = Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.Editor;

			EditorGUILayout.BeginVertical(navStyle);
			EditorGUILayout.Space();

			bool newSettingsGeneralFoldout = GUILayout.Toggle(settingsGeneralFoldout, "General", navButtonStyle);

			if (newSettingsGeneralFoldout != settingsGeneralFoldout)
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.General, true);
#if !MVC_COMMUNITY

			bool newSettingsAIFoldout = GUILayout.Toggle(settingsAIFoldout, "Artificial Intelligence (AI)", navButtonStyle);

			if (newSettingsAIFoldout != settingsAIFoldout)
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.AI, true);
#endif

			bool newSettingsBehaviourFoldout = GUILayout.Toggle(settingsBehaviourFoldout, "Behaviour & Physics", navButtonStyle);

			if (newSettingsBehaviourFoldout != settingsBehaviourFoldout)
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.Behaviour, true);

			bool newSettingsDamageFoldout = GUILayout.Toggle(settingsDamageFoldout, "Damage", navButtonStyle);

			if (newSettingsDamageFoldout != settingsDamageFoldout)
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.Damage, true);

			bool newSettingsCamerasFoldout = GUILayout.Toggle(settingsCamerasFoldout, "Cameras", navButtonStyle);

			if (newSettingsCamerasFoldout != settingsCamerasFoldout)
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.Cameras, true);

			bool newSettingsInputsFoldout = GUILayout.Toggle(settingsInputsFoldout, "Player Inputs", navButtonStyle);

			if (newSettingsInputsFoldout != settingsInputsFoldout)
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.PlayerInputs, true);

			bool newSettingsEnginesChargersFoldout = GUILayout.Toggle(settingsEnginesChargersFoldout, "Engines & Chargers", navButtonStyle);

			if (newSettingsEnginesChargersFoldout != settingsEnginesChargersFoldout)
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.EnginesChargers, true);

			bool newSettingsTireCompoundsFoldout = GUILayout.Toggle(settingsTireCompoundsFoldout, "Tire Compounds", navButtonStyle);

			if (newSettingsTireCompoundsFoldout != settingsTireCompoundsFoldout)
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.TireCompounds, true);

			bool newSettingsGroundsFoldout = GUILayout.Toggle(settingsGroundsFoldout, "Ground & Surfaces", navButtonStyle);

			if (newSettingsGroundsFoldout != settingsGroundsFoldout)
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.Grounds, true);

			bool newSettingsSFXFoldout = GUILayout.Toggle(settingsSFXFoldout, "Sound Effects (SFX)", navButtonStyle);

			if (newSettingsSFXFoldout != settingsSFXFoldout)
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.SFX, true);

			bool newSettingsVFXFoldout = GUILayout.Toggle(settingsVFXFoldout, "Visual Effects (VFX)", navButtonStyle);

			if (newSettingsVFXFoldout != settingsVFXFoldout)
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.VFX, true);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Other Settings", EditorStyles.miniBoldLabel);

			bool newSettingsEditorFoldout = GUILayout.Toggle(settingsEditorFoldout, "Editor", navButtonStyle);

			if (newSettingsEditorFoldout != settingsEditorFoldout)
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.Editor, true);

			EditorGUILayout.EndVertical();
		}
		private void ContainerEditor(bool showNavEditor)
		{
			GUIStyle containerStyle = new()
			{
				fixedWidth = position.width,
				fixedHeight = position.height - EditorGUIUtility.singleLineHeight * (EditorApplication.isPlaying ? 2f : 1f),
				padding = new(10, 0, 7, 7)
			};

			if (showNavEditor)
				containerStyle.fixedWidth -= 256f;

			scrollViewStyle = new(GUI.skin.scrollView)
			{
				padding = new(0, 20, 0, 0)
			};

			EditorGUILayout.BeginVertical(containerStyle);

			if (Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.EnginesChargers && currentEngine < 0 && currentCharger < 0)
				EditorGUILayout.BeginVertical(GUILayout.Width(position.width - 270f));
			else
				EditorGUILayout.BeginVertical();

			float orgLabelWidth = EditorGUIUtility.labelWidth;

			if (Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.EnginesChargers && currentEngine < 0 && currentCharger < 0 || Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.SFX && Settings.soundEffectsFoldout != ToolkitSettings.SettingsEditorSFXFoldout.None)
				EditorGUIUtility.labelWidth = 100f;
			else
				EditorGUIUtility.labelWidth = Mathf.Clamp((position.width - 276f) * .5f, 100f, 300f);

			switch (Settings.settingsFoldout)
			{
				case ToolkitSettings.SettingsEditorFoldout.General:
					GeneralSettingsEditor();

					break;
#if !MVC_COMMUNITY

				case ToolkitSettings.SettingsEditorFoldout.AI:
					AISettingsEditor();

					break;
#endif

				case ToolkitSettings.SettingsEditorFoldout.Behaviour:
					BehaviourSettingsEditor();

					break;

				case ToolkitSettings.SettingsEditorFoldout.Damage:
					DamageSettingsEditor();

					break;

				case ToolkitSettings.SettingsEditorFoldout.Cameras:
					CameraSettingsEditor();

					break;

				case ToolkitSettings.SettingsEditorFoldout.PlayerInputs:
					PlayerInputsSettingsEditor();

					break;

				case ToolkitSettings.SettingsEditorFoldout.EnginesChargers:
					EnginesChargersSettingsEditor();

					break;

				case ToolkitSettings.SettingsEditorFoldout.TireCompounds:
					TireCompoundsSettingsEditor();

					break;

				case ToolkitSettings.SettingsEditorFoldout.Grounds:
					GroundsSettingsEditor();

					break;

				case ToolkitSettings.SettingsEditorFoldout.SFX:
					SFXSettingsEditor();

					break;

				case ToolkitSettings.SettingsEditorFoldout.VFX:
					VFXSettingsEditor();

					break;

				case ToolkitSettings.SettingsEditorFoldout.Editor:
					EditorSettingsEditor();

					break;

				default:
					EditorGUI.DrawPreviewTexture(new(256f + (position.width - 256f) * .5f - 48f, position.height * .5f - 112f, 96f, 96f), EditorUtilities.Icons.Settings, new Material(Shader.Find("Unlit/Transparent")));
					EditorGUI.LabelField(new(256f + (position.width - 300f) * .5f - 128f, position.height * .5f, 300f, EditorGUIUtility.singleLineHeight * 2f), "MVC Settings Panel", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight * 1.5f) });
					EditorGUI.LabelField(new(256f + (position.width - 256f) * .5f - 128f, position.height * .5f + EditorGUIUtility.singleLineHeight * 2f, 256f, EditorGUIUtility.singleLineHeight), "Select a Tab from the left menu to get started!", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight * .65f) });

					break;
			}

			EditorGUIUtility.labelWidth = orgLabelWidth;

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndVertical();
		}
		private void GeneralSettingsEditor()
		{
			GUIStyle largeLabel = new(EditorStyles.boldLabel)
			{
				fontSize = 17,
				fixedHeight = 20f
			};

			EditorGUILayout.LabelField("General", largeLabel);
			EditorGUILayout.Space();

			scrollView = EditorGUILayout.BeginScrollView(scrollView, scrollViewStyle);

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			ToolkitSettings.PhysicsType newPhysicsType = (ToolkitSettings.PhysicsType)EditorGUILayout.EnumPopup(new GUIContent("Physics System", "The physics system used to control all the vehicles"), Settings.Physics);

			if (Settings.Physics != newPhysicsType)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Physics");

				Settings.Physics = newPhysicsType;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.indentLevel++;

			EditorGUI.EndDisabledGroup();

			Utility.IntervalInt newPhysicsSubSteps = ToolkitEditorUtility.IntervalSlider(new GUIContent("Sub Steps", $"The {Settings.Physics} Sub Step count interval, higher values improve stability and handling rates"), null, new GUIContent("Min", "The minimum Sub Step count"), new GUIContent("Max", "The maximum Sub Step count"), Settings.physicsSubSteps, 1, 100, Settings, "Change Sub Steps");

			if (Settings.physicsSubSteps != newPhysicsSubSteps)
				Settings.physicsSubSteps = newPhysicsSubSteps;

			EditorGUI.indentLevel--;

			Vehicle.SteeringModule.SteerMethod newSteerMethod = (Vehicle.SteeringModule.SteerMethod)EditorGUILayout.Popup(new GUIContent("Steering Method", "The steer method use by the vehicles. This basically helps the stability depending on the used method"), (int)Settings.steerMethod, new string[] { "Simple", "Dynamic", "Ackermann" });

			if (Settings.steerMethod != newSteerMethod)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Steer Method");

				Settings.steerMethod = newSteerMethod;

				EditorUtility.SetDirty(Settings);
			}

			ToolkitSettings.TransmissionType newTransmissionType = (ToolkitSettings.TransmissionType)EditorGUILayout.Popup(new GUIContent("Transmission", "The type of gearbox transmission used to shift gears"), (int)Settings.transmissionType, new string[] { "Manual", "Automatic" });

			if (Settings.transmissionType != newTransmissionType)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Transmission");

				Settings.transmissionType = newTransmissionType;

				EditorUtility.SetDirty(Settings);
			}

			ToolkitSettings.WheelRendererRefreshType newWheelRendererRefreshType = (ToolkitSettings.WheelRendererRefreshType)EditorGUILayout.EnumPopup(new GUIContent("Wheel Renderer Refresh", "The type of wheel renderers refresh at Runtime; This refers to the wheel rotation calculations"), Settings.wheelRendererRefreshType);

			if (Settings.wheelRendererRefreshType != newWheelRendererRefreshType)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Type");

				Settings.wheelRendererRefreshType = newWheelRendererRefreshType;

				EditorUtility.SetDirty(Settings);
			}

			Utility.UnitType newValuesUnit = (Utility.UnitType)EditorGUILayout.EnumPopup(new GUIContent("Runtime Units", "The runtime (Play mode) values units type"), Settings.valuesUnit);

			if (Settings.valuesUnit != newValuesUnit)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Units Type");

				Settings.valuesUnit = newValuesUnit;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.indentLevel++;

			Utility.UnitType newPowerUnit = (Utility.UnitType)EditorGUILayout.Popup(new GUIContent("Power Unit", "The runtime (Play mode) power values units"), (int)Settings.powerUnit, new string[] { $"{Utility.FullUnit(Utility.Units.Power, Utility.UnitType.Metric)} ({Utility.Unit(Utility.Units.Power, Utility.UnitType.Metric)})", $"{Utility.FullUnit(Utility.Units.Power, Utility.UnitType.Imperial)} ({Utility.Unit(Utility.Units.Power, Utility.UnitType.Imperial)})" });

			if (Settings.powerUnit != newPowerUnit)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Power Unit");

				Settings.powerUnit = newPowerUnit;

				EditorUtility.SetDirty(Settings);
			}

			Utility.UnitType newTorqueUnit = (Utility.UnitType)EditorGUILayout.Popup(new GUIContent("Torque Unit", "The runtime (Play mode) torque values units"), (int)Settings.torqueUnit, new string[] { $"{Utility.FullUnit(Utility.Units.Torque, Utility.UnitType.Metric)} ({Utility.Unit(Utility.Units.Torque, Utility.UnitType.Metric)})", $"{Utility.FullUnit(Utility.Units.Torque, Utility.UnitType.Imperial)} ({Utility.Unit(Utility.Units.Torque, Utility.UnitType.Imperial)})" });

			if (Settings.torqueUnit != newTorqueUnit)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Torque Unit");

				Settings.torqueUnit = newTorqueUnit;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.indentLevel--;

			ToolkitSettings.EngineStartMode newEngineStartMode = ToolkitEditorUtility.EnumField(new GUIContent("Engine Start Mode", "Determines the behaviour of the engine start"), Settings.engineStartMode, Settings, "Switch Start Mode");

			if (Settings.engineStartMode != newEngineStartMode)
				Settings.engineStartMode = newEngineStartMode;

			EditorGUI.BeginDisabledGroup(Settings.engineStartMode == ToolkitSettings.EngineStartMode.Always);

			EditorGUI.indentLevel++;

			bool newAutoEngineShutDown = ToolkitEditorUtility.ToggleButtons(new GUIContent("Auto Engine Shutdown", "Should the player's vehicle engine be shutdown after a specified timeout in case it's stationary"), null, "On", "Off", Settings.autoEngineShutDown, Settings, "Switch Auto Shutdown");

			if (Settings.autoEngineShutDown != newAutoEngineShutDown)
				Settings.autoEngineShutDown = newAutoEngineShutDown;

			if (Settings.engineStartMode != ToolkitSettings.EngineStartMode.Always)
			{
				EditorGUI.BeginDisabledGroup(!Settings.autoEngineShutDown);

				EditorGUI.indentLevel++;

				float newEngineShutDownTimeout = ToolkitEditorUtility.NumberField(new GUIContent("Timeout", "How much time does it take for the engine to be shutdown automatically"), Settings.engineShutDownTimeout, Utility.Units.Time, 2, Settings, "Change Timeout");

				if (Settings.engineShutDownTimeout != newEngineShutDownTimeout)
					Settings.engineShutDownTimeout = newEngineShutDownTimeout;

				EditorGUI.indentLevel--;

				EditorGUI.EndDisabledGroup();
			}

			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();

			if (Settings.engineStartMode == ToolkitSettings.EngineStartMode.EngineStartKey && Settings.inputSystem == ToolkitSettings.InputSystem.InputsManager && !InputsManagerEditor.EditingInput)
			{
				bool inputsManagerDataAssetExists = InputsManager.DataAssetExists;
				Input startEngineInput;

				if (inputsManagerDataAssetExists)
				{
					try
					{
						startEngineInput = InputsManager.GetInput(Settings.engineStartSwitchInput);
					}
					catch (Exception e)
					{
						ToolkitDebug.Error(e);

						startEngineInput = null;
					}
				}
				else
					startEngineInput = null;

				GamepadBinding startEngineButton = default;
				Key startEngineKey = default;

				if (startEngineInput)
				{
					if (startEngineInput.Main.Positive != default)
						startEngineKey = startEngineInput.Main.Positive;
					else if (startEngineInput.Main.Negative != default)
						startEngineKey = startEngineInput.Main.Negative;
					else if (startEngineInput.Alt.Positive != default)
						startEngineKey = startEngineInput.Alt.Negative;
					else if (startEngineInput.Alt.Negative != default)
						startEngineKey = startEngineInput.Alt.Negative;

					if (startEngineInput.Main.GamepadPositive != default)
						startEngineButton = startEngineInput.Main.GamepadPositive;
					else if (startEngineInput.Main.GamepadNegative != default)
						startEngineButton = startEngineInput.Main.GamepadNegative;
					else if (startEngineInput.Alt.GamepadPositive != default)
						startEngineButton = startEngineInput.Alt.GamepadNegative;
					else if (startEngineInput.Alt.GamepadNegative != default)
						startEngineButton = startEngineInput.Alt.GamepadNegative;
				}

				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUIUtility.labelWidth + 5f);
				EditorGUILayout.BeginVertical();

				if (!inputsManagerDataAssetExists)
					EditorGUILayout.HelpBox("It seems that the Inputs Manager data asset has been removed or doesn't exist anymore!", MessageType.Error);
				else if (!startEngineInput)
					EditorGUILayout.HelpBox("It seems that the input responsible for switching the engine on and off is missing!", MessageType.Error);
				else if (startEngineKey != default || startEngineButton != default)
					EditorGUILayout.HelpBox($"Use {(startEngineKey != default ? $"`{startEngineKey}` on your Keyboard" : "")}{(startEngineButton != default ? $"{(startEngineKey != default ? " or " : "")}`{startEngineButton}` on your Gamepad" : "")} to start & shutdown your engine.", MessageType.Info);
				else
					EditorGUILayout.HelpBox("You won't be able to start or shutdown a vehicle's engine as long as the used Input doesn't have any valid bindings.", MessageType.Warning);

				EditorGUILayout.BeginHorizontal();

				if (inputsManagerDataAssetExists)
				{
					if (GUILayout.Button("Change Input", EditorStyles.miniButtonLeft))
						EnableFoldout(ToolkitSettings.SettingsEditorFoldout.PlayerInputs, true);

					EditorGUI.BeginDisabledGroup(!startEngineInput);

					if (GUILayout.Button("Edit Input", EditorStyles.miniButtonRight))
						InputsManagerEditor.OpenWindow(startEngineInput);

					EditorGUI.EndDisabledGroup();
				}
				else if (GUILayout.Button("Try a quick fix", EditorStyles.miniButton))
					TryInputsManagerQuickFix();

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();
			}

#if !MVC_COMMUNITY
			bool newUseFuelSystem = ToolkitEditorUtility.ToggleButtons(new GUIContent("Fuel/Battery System", "Enabling this, will make vehicles limited to its fuel/battery capacity in order to run"), null, "On", "Off", Settings.useFuelSystem, Settings, "Switch Fuel System");

			if (Settings.useFuelSystem != newUseFuelSystem)
				Settings.useFuelSystem = newUseFuelSystem;
#endif

			bool newUseEngineStalling = ToolkitEditorUtility.ToggleButtons(new GUIContent("Engine Stall", "Should vehicles engine stall at very low RPMs?"), null, "On", "Off", Settings.useEngineStalling, Settings, "Switch Engine Stall");

			if (Settings.useEngineStalling != newUseEngineStalling)
				Settings.useEngineStalling = newUseEngineStalling;

			bool newUseGearLimiterOnReverseAndFirstGear = ToolkitEditorUtility.ToggleButtons(new GUIContent("Gear Limiter (Reverse/First Gear)", "Disabling this will allow the engine wheels RPM to go below the engine's minimum RPM & stop the vehicle from moving"), null, "On", "Off", Settings.useGearLimiterOnReverseAndFirstGear, Settings, "Switch Gear Limiter");

			if (Settings.useGearLimiterOnReverseAndFirstGear != newUseGearLimiterOnReverseAndFirstGear)
				Settings.useGearLimiterOnReverseAndFirstGear = newUseGearLimiterOnReverseAndFirstGear;

			bool newBurnoutsForAutomaticTransmissions = ToolkitEditorUtility.ToggleButtons(new GUIContent("Burnouts For Auto Transmissions", "Should vehicles with automatic transmissions be allowed to perform burnouts"), null, "On", "Off", Settings.burnoutsForAutomaticTransmissions, Settings, "Switch Burnouts");

			if (Settings.burnoutsForAutomaticTransmissions != newBurnoutsForAutomaticTransmissions)
				Settings.burnoutsForAutomaticTransmissions = newBurnoutsForAutomaticTransmissions;

			/*bool newUseAutomaticClutch = VehicleEditorUtility.ToggleButtons(new GUIContent("Automatic Clutch", "Should the MVC manage the Clutch pedal while you drive?"), null, "On", "Off", Settings.useAutomaticClutch, Settings, "Switch Auto Clutch");
			
			if (Settings.useAutomaticClutch != newUseAutomaticClutch)
				Settings.useAutomaticClutch = newUseAutomaticClutch;*/

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

#if !MVC_COMMUNITY
			bool newUseInterior = ToolkitEditorUtility.ToggleButtons(new GUIContent($"Vehicles Interior", "Use Interior Components?"), null, "On", "Off", Settings.useInterior, Settings, "Switch Interior");

			if (Settings.useInterior != newUseInterior)
				Settings.useInterior = newUseInterior;
#endif

			bool newUseReset = ToolkitEditorUtility.ToggleButtons(new GUIContent("Reset Vehicles", "Should vehicles reset when requested at runtime? When a vehicle is flipped for example"), null, "On", "Off", Settings.useReset, Settings, "Switch Reset");

			if (Settings.useReset != newUseReset)
				Settings.useReset = newUseReset;

			EditorGUI.BeginDisabledGroup(!Settings.useReset);

			EditorGUI.indentLevel++;

			float newResetTimeout = ToolkitEditorUtility.NumberField(new GUIContent("Timeout", "How much time does it take for the controller to reset the vehicle in case of crash or flipping"), Settings.resetTimeout, Utility.Units.Time, 2, Settings, "Change Timeout");

			if (Settings.resetTimeout != newResetTimeout)
				Settings.resetTimeout = newResetTimeout;

			float newResetPreviewTime = ToolkitEditorUtility.NumberField(new GUIContent("Cooldown Time", "The time it takes the vehicle to finish the reset visually"), Settings.resetPreviewTime, Utility.Units.Time, 2, Settings, "Change Time");

			if (Settings.resetPreviewTime != newResetPreviewTime)
				Settings.resetPreviewTime = newResetPreviewTime;

			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Tags & Layers", EditorStyles.boldLabel);

			int newVehiclesLayer = EditorGUILayout.LayerField(new GUIContent("Vehicles Layer", "The MVC vehicles layer; The same layer as in AI settings"), Settings.vehiclesLayer);

			if (Settings.vehiclesLayer != newVehiclesLayer)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Layer");

				Settings.vehiclesLayer = newVehiclesLayer;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.indentLevel++;

			int newVehicleWheelsLayer = EditorGUILayout.LayerField(new GUIContent("Wheels Layer", "The MVC vehicle wheels layer"), Settings.vehicleWheelsLayer);

			if (Settings.vehicleWheelsLayer != newVehicleWheelsLayer)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Layer");

				Settings.vehicleWheelsLayer = newVehicleWheelsLayer;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.indentLevel--;

			if (Settings.vehiclesLayer == default || Settings.vehicleWheelsLayer == default)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUIUtility.labelWidth + 5f);
				EditorGUILayout.HelpBox("Please assign both the `Vehicles Layer` & `Wheels Layer` for the toolkit to differentiate between vehicle components.", MessageType.Warning);
				EditorGUILayout.EndHorizontal();
			}
			else if (Settings.vehiclesLayer == Settings.vehicleWheelsLayer)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUIUtility.labelWidth + 5f);
				EditorGUILayout.HelpBox("Setting the `Vehicles Layer` the same as the `Wheels Layer` might confuse the toolkit systems at Runtime.", MessageType.Warning);
				EditorGUILayout.EndHorizontal();
			}

			string newGameControllerTag = EditorGUILayout.TagField(new GUIContent("Game Controller Tag", "The game controller game object tag"), Settings.gameControllerTag);

			if (Settings.gameControllerTag != newGameControllerTag)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Tag");

				Settings.gameControllerTag = newGameControllerTag;

				EditorUtility.SetDirty(Settings);
			}

			string newPlayerVehicleTag = EditorGUILayout.TagField(new GUIContent("Player Tag", "The player vehicle tag; Note: This tag will not be used to find the main player vehicle"), Settings.playerVehicleTag);

			if (Settings.playerVehicleTag != newPlayerVehicleTag)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Tag");

				Settings.playerVehicleTag = newPlayerVehicleTag;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}
#if !MVC_COMMUNITY
		private void AISettingsEditor()
		{
			GUIStyle largeLabel = new(EditorStyles.boldLabel)
			{
				fontSize = 17,
				fixedHeight = 20f
			};

			EditorGUILayout.LabelField("Artificial Intelligence (AI)", largeLabel);
			EditorGUILayout.Space();

			scrollView = EditorGUILayout.BeginScrollView(scrollView, scrollViewStyle);

			/*EditorGUILayout.BeginHorizontal();
			GUILayout.Space(EditorGUIUtility.labelWidth + 5f);

			if (GUILayout.Button("Open Layers Editor"))
			{
				VehicleAILayersEditorWindow.OpenWindow(true);
				Close();
			}

			EditorGUILayout.EndHorizontal();*/
			EditorGUILayout.LabelField("Path", EditorStyles.boldLabel);

			GUIContent AIPathPointDistanceLabel = new("Distance", "The required distance between an AI vehicle and a path point to move to the next one");
			bool newUseDynamicAIPathPointDistance = ToolkitEditorUtility.ToggleButtons(new GUIContent("Vehicle Path Distance", "The type of AI path point to vehicle distance."), null, new GUIContent("Dynamic", "Dynamic sensors can change length depending on vehicle speed"), new GUIContent("Fixed", "Fixed sensors do not change length"), Settings.useDynamicAIPathPointDistance, Settings, "Change Distance");

			if (Settings.useDynamicAIPathPointDistance != newUseDynamicAIPathPointDistance)
				Settings.useDynamicAIPathPointDistance = newUseDynamicAIPathPointDistance;

			EditorGUI.indentLevel++;

			if (Settings.useDynamicAIPathPointDistance)
			{
				Utility.Interval newAIPathPointDistanceInterval = ToolkitEditorUtility.IntervalField(AIPathPointDistanceLabel, null, new GUIContent("Min", $"The minimum distance at 0 {Utility.FullUnit(Utility.Units.Speed, Settings.editorValuesUnit)} ({Utility.Unit(Utility.Units.Speed, Settings.editorValuesUnit)})"), new GUIContent("Max", $"The maximum distance at {math.round(Utility.UnitMultiplier(Utility.Units.Speed, Settings.editorValuesUnit) * 100f)} {Utility.FullUnit(Utility.Units.Speed, Settings.editorValuesUnit)} ({Utility.Unit(Utility.Units.Speed, Settings.editorValuesUnit)})"), Utility.Units.Distance, 3, Settings.AIPathPointDistanceInterval, Settings, "Change Distance", false);

				if (Settings.AIPathPointDistanceInterval != newAIPathPointDistanceInterval)
					Settings.AIPathPointDistanceInterval = newAIPathPointDistanceInterval;
			}
			else
			{
				float newAIPathPointDistance = ToolkitEditorUtility.NumberField(AIPathPointDistanceLabel, Settings.AIPathPointDistance, Utility.Units.Distance, 3, Settings, "Change Distance");

				if (Settings.AIPathPointDistance != newAIPathPointDistance)
					Settings.AIPathPointDistance = newAIPathPointDistance;
			}

			EditorGUI.indentLevel--;

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			string distanceUnit = Utility.Unit(Utility.Units.Distance, Settings.editorValuesUnit);
			string distanceFullUnit = Utility.FullUnit(Utility.Units.Distance, Settings.editorValuesUnit);
			float distanceUnitMultiplier = Utility.UnitMultiplier(Utility.Units.Distance, Settings.editorValuesUnit);
			float newAIPathPointsCountPerMeter = ToolkitEditorUtility.NumberField(new GUIContent("Path Spaced Points Count", "Number of spaced points in an AI Path per meter"), Settings.AIPathSpacedPointsPerMeter / distanceUnitMultiplier, $"point/{distanceUnit}", $"Point per {distanceFullUnit}", false, Settings, "Change Count") * distanceUnitMultiplier;

			if (Settings.AIPathSpacedPointsPerMeter != newAIPathPointsCountPerMeter)
				Settings.AIPathSpacedPointsPerMeter = newAIPathPointsCountPerMeter;

			EditorGUILayout.HelpBox($"Estimated Points Spacing: {Utility.NumberToValueWithUnit(Settings.AIPathSpacedPointsSpacing, Utility.Units.Distance, Settings.editorValuesUnit, 3):0.000}", MessageType.None);

			if (newAIPathPointsCountPerMeter > 5f)
				EditorGUILayout.HelpBox("Increasing the points count to higher values might lead to performance issues without any noticeable accuracy.", MessageType.Warning);

			int newAIPathLengthSamples = ToolkitEditorUtility.NumberField(new GUIContent("Path Length Samples", "Number of divisions each bezier curve will be split into to approximate their respective length. Higher samples count lead to higher accuracy"), Settings.AIPathLengthSamples, Settings, "Change Samples");

			if (Settings.AIPathLengthSamples != newAIPathLengthSamples)
				Settings.AIPathLengthSamples = newAIPathLengthSamples;

			if (newAIPathLengthSamples > 256)
				EditorGUILayout.HelpBox("Increasing the length samples count to higher values might lead to performance issues without any noticeable accuracy.", MessageType.Warning);

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Behaviour", EditorStyles.boldLabel);

			float newAIReverseTimeout = ToolkitEditorUtility.NumberField(new GUIContent("Reverse Timeout", "The time it takes an AI vehicle to make a decision to reverse when it's stuck"), Settings.AIReverseTimeout, Utility.Units.Time, 3, Settings, "Change Timeout");

			if (Settings.AIReverseTimeout != newAIReverseTimeout)
				Settings.AIReverseTimeout = newAIReverseTimeout;

			float newAIReverseTimeSpan = ToolkitEditorUtility.NumberField(new GUIContent("Reverse Time Span", "How long does it take for an AI vehicle to pull out of reverse"), Settings.AIReverseTimeSpan, Utility.Units.Time, 3, Settings, "Change Time Span");

			if (Settings.AIReverseTimeSpan != newAIReverseTimeSpan)
				Settings.AIReverseTimeSpan = newAIReverseTimeSpan;

			EditorGUILayout.Space();
			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}
#endif
		private void BehaviourSettingsEditor()
		{
			GUIStyle largeLabel = new(EditorStyles.boldLabel)
			{
				fontSize = 17,
				fixedHeight = 20f
			};

			EditorGUILayout.LabelField("Behaviour & Physics", largeLabel);
			EditorGUILayout.Space();

			scrollView = EditorGUILayout.BeginScrollView(scrollView, scrollViewStyle);

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

#if !MVC_COMMUNITY
			bool newUseChassisWings = ToolkitEditorUtility.ToggleButtons(new GUIContent("Aerodynamic Wings", "Should the chassis wings be available for use?"), null, "On", "Off", Settings.useChassisWings, Settings, "Switch Wings");

			if (Settings.useChassisWings != newUseChassisWings)
				Settings.useChassisWings = newUseChassisWings;
#endif

			EditorGUI.EndDisabledGroup();

			bool newUseDownforce = ToolkitEditorUtility.ToggleButtons(new GUIContent("Downforce", "Should vehicles use downforce for more stability?"), null, "On", "Off", Settings.useDownforce, Settings, "Switch Downforce");

			if (Settings.useDownforce != newUseDownforce)
				Settings.useDownforce = newUseDownforce;

			EditorGUI.BeginDisabledGroup(!Settings.useDownforce);

			EditorGUI.indentLevel++;

			bool newUseAirAntiRoll = ToolkitEditorUtility.ToggleButtons(new GUIContent("Air Anti-Roll", "Should vehicles use anti-roll while flying in the air, so it may not land flipped?"), null, "On", "Off", Settings.useAirAntiRoll, Settings, "Switch Anti-Roll");

			if (Settings.useAirAntiRoll != newUseAirAntiRoll)
				Settings.useAirAntiRoll = newUseAirAntiRoll;

			EditorGUI.BeginDisabledGroup(!Settings.useAirAntiRoll);

			if (Settings.useDownforce)
			{
				EditorGUI.indentLevel++;

				float newAirAntiRollIntensity = ToolkitEditorUtility.Slider(new GUIContent("Intensity", "The vehicle's air anti-roll intensity"), Settings.airAntiRollIntensity, 0f, 10f, Settings, "Change Intensity");

				if (Settings.airAntiRollIntensity != newAirAntiRollIntensity)
					Settings.airAntiRollIntensity = newAirAntiRollIntensity;

				EditorGUI.indentLevel--;
			}

			EditorGUI.EndDisabledGroup();

			bool newUseDownforceWhenNotGrounded = ToolkitEditorUtility.ToggleButtons(new GUIContent("Non-Ground Downforce", "Should vehicles use downforce when its wheels are not on the ground?"), null, "On", "Off", Settings.useDownforceWhenNotGrounded, Settings, "Switch Downforce");

			if (Settings.useDownforceWhenNotGrounded != newUseDownforceWhenNotGrounded)
				Settings.useDownforceWhenNotGrounded = newUseDownforceWhenNotGrounded;

			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();

			bool newUseDrag = ToolkitEditorUtility.ToggleButtons(new GUIContent("Drag", "Should vehicles use drag (ie. air resistance force)?"), null, "On", "Off", Settings.useDrag, Settings, "Switch Drag");

			if (Settings.useDrag != newUseDrag)
				Settings.useDrag = newUseDrag;

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			bool newUseEngineMass = ToolkitEditorUtility.ToggleButtons(new GUIContent("Engine Mass", "Should the total wheels mass affect the vehicle curb weight?"), null, "On", "Off", Settings.useEngineMass, Settings, "Switch Engine Mass");

			if (Settings.useEngineMass != newUseEngineMass)
				Settings.useEngineMass = newUseEngineMass;

			bool newUseWheelsMass = ToolkitEditorUtility.ToggleButtons(new GUIContent("Wheels Mass", "Should the total wheels mass affect the vehicle curb weight?"), null, "On", "Off", Settings.useWheelsMass, Settings, "Switch Wheels Mass");

			if (Settings.useWheelsMass != newUseWheelsMass)
				Settings.useWheelsMass = newUseWheelsMass;

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			ToolkitSettings.SteeringInterpolation newSteeringInterpolation = (ToolkitSettings.SteeringInterpolation)EditorGUILayout.EnumPopup(new GUIContent("Steering Interpolation", "The type of interpolation steering values use to change over time"), Settings.steeringInterpolation);

			if (Settings.steeringInterpolation != newSteeringInterpolation)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Interpolation");

				Settings.steeringInterpolation = newSteeringInterpolation;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.BeginDisabledGroup(Settings.steeringInterpolation == ToolkitSettings.SteeringInterpolation.Instant);

			EditorGUI.indentLevel++;

			float newSteeringIntensity = ToolkitEditorUtility.Slider(new GUIContent("Steer Intensity", "The vehicle steering speed, higher values can result in a very responsive steering behaviour"), Settings.steerIntensity, 1f, 20f, Settings, "Change Intensity");

			if (Settings.steerIntensity != newSteeringIntensity)
				Settings.steerIntensity = newSteeringIntensity;

			float newReleaseSteeringIntensity = ToolkitEditorUtility.Slider(new GUIContent("Release Intensity", "The vehicle steering speed when the steering wheel input has been released"), Settings.steerReleaseIntensity, 1f, 20f, Settings, "Change Intensity");

			if (Settings.steerReleaseIntensity != newReleaseSteeringIntensity)
				Settings.steerReleaseIntensity = newReleaseSteeringIntensity;

			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			float newMaximumSteerAngle = ToolkitEditorUtility.Slider(new GUIContent("Maximum Steer Angle", "The maximum steer angle for all the vehicle. This controls the maximum steer angle in the vehicle's inspector"), Settings.maximumSteerAngle, Settings.minimumSteerAngle, 90f, "°", "Degrees", Settings, "Change Angle");

			if (Settings.maximumSteerAngle != newMaximumSteerAngle)
				Settings.maximumSteerAngle = newMaximumSteerAngle;

			float newMinimumSteerAngle = ToolkitEditorUtility.Slider(new GUIContent("Minimum Steer Angle", "The minimum steer angle for all the vehicle. This controls the minimum steer angle in the vehicle's inspector and in runtime at higher speed"), Settings.minimumSteerAngle, 1f, Settings.maximumSteerAngle, "°", "Degrees", Settings, "Change Angle");

			if (Settings.minimumSteerAngle != newMinimumSteerAngle)
				Settings.minimumSteerAngle = newMinimumSteerAngle;

#if !MVC_COMMUNITY
			bool newUseSuspensionAdjustments = ToolkitEditorUtility.ToggleButtons(new GUIContent("Suspension Adjustments", "Should the MVC controller consider using the wheels camber angle or not?"), null, "On", "Off", Settings.useSuspensionAdjustments, Settings, "Switch Adjustments");

			if (Settings.useSuspensionAdjustments != newUseSuspensionAdjustments)
				Settings.useSuspensionAdjustments = newUseSuspensionAdjustments;
#endif

			EditorGUI.BeginDisabledGroup(!Settings.useSuspensionAdjustments);

			EditorGUI.indentLevel++;

			bool hasSuspensionAdjustmentIssues = Settings.useSuspensionAdjustments && Settings.maximumCamberAngle == 0f && Settings.maximumCasterAngle == 0f && Settings.maximumToeAngle == 0f && Settings.maximumSideOffset == 0f;
			float newMaximumCamberAngle = ToolkitEditorUtility.Slider(new GUIContent("Maximum Camber", "The wheels maximum camber angle"), Settings.maximumCamberAngle, 0f, 45f, "°", "Degrees", Settings, "Change Camber");

			if (Settings.maximumCamberAngle != newMaximumCamberAngle)
				Settings.maximumCamberAngle = newMaximumCamberAngle;

			if (!hasSuspensionAdjustmentIssues && Settings.maximumCamberAngle == 0f)
				EditorGUILayout.HelpBox("The camber angle is going to be disabled", MessageType.None);

			float newMaximumCasterAngle = ToolkitEditorUtility.Slider(new GUIContent("Maximum Caster", "The wheels maximum caster angle"), Settings.maximumCasterAngle, 0f, 45f, "°", "Degrees", Settings, "Change Caster");

			if (Settings.maximumCasterAngle != newMaximumCasterAngle)
				Settings.maximumCasterAngle = newMaximumCasterAngle;

			if (!hasSuspensionAdjustmentIssues && Settings.maximumCasterAngle == 0f)
				EditorGUILayout.HelpBox("The caster angle is going to be disabled", MessageType.None);

			float newMaximumToeAngle = ToolkitEditorUtility.Slider(new GUIContent("Maximum Toe", "The wheels maximum toe angle"), Settings.maximumToeAngle, 0f, 45f, "°", "Degrees", Settings, "Change Toe");

			if (Settings.maximumToeAngle != newMaximumToeAngle)
				Settings.maximumToeAngle = newMaximumToeAngle;

			if (!hasSuspensionAdjustmentIssues && Settings.maximumToeAngle == 0f)
				EditorGUILayout.HelpBox("The toe angle is going to be disabled", MessageType.None);

			float newMaximumSideOffset = ToolkitEditorUtility.Slider(new GUIContent("Maximum Side Offset", "The wheels maximum side offset"), Settings.maximumSideOffset, 0f, 1f, Utility.Units.SizeAccurate, Settings, "Change Offset");

			if (Settings.maximumSideOffset != newMaximumSideOffset)
				Settings.maximumSideOffset = newMaximumSideOffset;

			if (!hasSuspensionAdjustmentIssues && Settings.maximumSideOffset == 0f)
				EditorGUILayout.HelpBox("The side offset is going to be disabled", MessageType.None);

			EditorGUI.BeginDisabledGroup(hasSuspensionAdjustmentIssues || Settings.maximumSideOffset == 0f);

			EditorGUI.indentLevel++;

			bool newSideOffsetAffectHandling = ToolkitEditorUtility.ToggleButtons(new GUIContent($"Affect Handling", "Should the wheel side offset values affect vehicles handling?"), null, "Yes", "No", Settings.sideOffsetAffectHandling, Settings, "Switch Handling");

			if (Settings.sideOffsetAffectHandling != newSideOffsetAffectHandling)
				Settings.sideOffsetAffectHandling = newSideOffsetAffectHandling;

			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();

			if (hasSuspensionAdjustmentIssues)
				EditorGUILayout.HelpBox("The maximum stance values are null (equal to 0), therefore it's unnecessary to turn it on.", MessageType.Info);

			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();

			bool newUseLaunchControl = ToolkitEditorUtility.ToggleButtons(new GUIContent("Launch Control", "Could vehicles be able to use launch control?"), null, "On", "Off", Settings.useLaunchControl, Settings, "Switch Launch Control");

			if (Settings.useLaunchControl != newUseLaunchControl)
				Settings.useLaunchControl = newUseLaunchControl;

			bool newUseCounterSteer = ToolkitEditorUtility.ToggleButtons(new GUIContent("Counter Steer", "Should vehicles counter steer when sliding sideways to regain traction?"), null, "On", "Off", Settings.useCounterSteer, Settings, "Switch Counter Steer");

			if (Settings.useCounterSteer != newUseCounterSteer)
				Settings.useCounterSteer = newUseCounterSteer;

			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup(!Settings.useCounterSteer);


			EditorGUI.indentLevel++;
			ToolkitSettings.CounterSteerType newCounterSteerType = (ToolkitSettings.CounterSteerType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The type of Counter Steering, Physics And Visuals Or Visuals Only"), Settings.counterSteerType);
			if (Settings.counterSteerType != newCounterSteerType)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Type");

				Settings.counterSteerType = newCounterSteerType;

				EditorUtility.SetDirty(Settings);
			}

			int newCounterSteerPriority = EditorGUILayout.Popup(new GUIContent("Priority", "The counter steer priority"), Settings.counterSteerPriority + 1, new string[] { "Steering Wheel", "Average", "Counter Steer" }) - 1;

			if (Settings.counterSteerPriority != newCounterSteerPriority)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Priority");

				Settings.counterSteerPriority = newCounterSteerPriority;

				EditorUtility.SetDirty(Settings);
			}

			float newCounterSteerIntensity = ToolkitEditorUtility.Slider(new GUIContent("Intensity", "The counter steer intensity or multiplier"), Settings.counterSteerIntensity, 0f, 1f, Settings, "Change Intensity");

			if (Settings.counterSteerIntensity != newCounterSteerIntensity)
				Settings.counterSteerIntensity = newCounterSteerIntensity;

			if (Settings.useCounterSteer && newCounterSteerIntensity == 0f)
				EditorGUILayout.HelpBox("The counter steer intensity is null (equal to 0), therefore it's unnecessary to turn on its toggle.", MessageType.Info);

			float newCounterSteerDamping = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Damping", "The counter steer smoothness damping"), Settings.counterSteerDamping, false, Settings, "Change Damping"), 1f);

			if (Settings.counterSteerDamping != newCounterSteerDamping)
				Settings.counterSteerDamping = newCounterSteerDamping;

			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			bool newUseNOS = ToolkitEditorUtility.ToggleButtons(new GUIContent("NOS", "Could vehicles be equipped with NOS (Nitrous)?"), null, "On", "Off", Settings.useNOS, Settings, "Switch NOS");

			if (Settings.useNOS != newUseNOS)
				Settings.useNOS = newUseNOS;

			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup(!Settings.useNOS);

			EditorGUI.indentLevel++;

			bool newUseNOSReload = ToolkitEditorUtility.ToggleButtons(new GUIContent("NOS Reloading", "Should NOS bottles reload them selves?"), null, "On", "Off", Settings.useNOSReload, Settings, "Switch NOS Reload");

			if (Settings.useNOSReload != newUseNOSReload)
				Settings.useNOSReload = newUseNOSReload;

			bool newUseNOSDelay = ToolkitEditorUtility.ToggleButtons(new GUIContent("Delayed NOS", "Should NOS emission be delayed?"), null, "On", "Off", Settings.useNOSDelay, Settings, "Switch NOS Delay");

			if (Settings.useNOSDelay != newUseNOSDelay)
				Settings.useNOSDelay = newUseNOSDelay;

			if (Settings.useNOS)
			{
				EditorGUI.BeginDisabledGroup(!Settings.useNOSDelay);

				EditorGUI.indentLevel++;

				float newNOSDelay = ToolkitEditorUtility.NumberField(new GUIContent("Delay", "The NOS emission delay"), Settings.NOSDelay * 1000f, Utility.Units.TimeAccurate, true, Settings, "Change Delay") * .001f;

				if (Settings.NOSDelay != newNOSDelay)
					Settings.NOSDelay = newNOSDelay;

				if (Settings.useNOSDelay && newNOSDelay == 0f)
					EditorGUILayout.HelpBox("The NOS delay is null (equal to 0), therefore it's unnecessary to turn it on.", MessageType.Info);

				EditorGUI.indentLevel--;

				EditorGUI.EndDisabledGroup();
			}

			bool newUseProgressiveNOS = ToolkitEditorUtility.ToggleButtons(new GUIContent("Progressive NOS", "Progressive NOS give vehicles the ability to boost its engines on how much NOS bottles are filled, the lower quantity of NOS, the lower boost is given"), null, "On", "Off", Settings.useProgressiveNOS, Settings, "Switch Prog. NOS");

			if (Settings.useProgressiveNOS != newUseProgressiveNOS)
				Settings.useProgressiveNOS = newUseProgressiveNOS;

			bool newUseOneShotNOS = ToolkitEditorUtility.ToggleButtons(new GUIContent("One Shot NOS", "One shot NOS give vehicles the ability to boost the engine continuously once the NOS input is pressed until NOS bottles are fully empty"), null, "On", "Off", Settings.useOneShotNOS, Settings, "Switch One-Shot NOS");

			if (Settings.useOneShotNOS != newUseOneShotNOS)
				Settings.useOneShotNOS = newUseOneShotNOS;

			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}
		private void DamageSettingsEditor()
		{
			GUIStyle largeLabel = new(EditorStyles.boldLabel)
			{
				fontSize = 17,
				fixedHeight = 20f
			};
			GUIStyle leftLargeButton = new(EditorStyles.miniButtonLeft)
			{
				fixedWidth = 40f,
				fixedHeight = 25f
			};
			GUIStyle rightLargeButton = new(EditorStyles.miniButtonRight)
			{
				fixedWidth = 40f,
				fixedHeight = 25f
			};

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUILayout.BeginHorizontal();

			bool newUseDamage = ToolkitEditorUtility.ToggleButtons("Damage", largeLabel, "On", "Off", Settings.useDamage, Settings, "Switch Damage", true, leftLargeButton, rightLargeButton);

			if (Settings.useDamage != newUseDamage)
				Settings.useDamage = newUseDamage;

			GUILayout.Space(20f);
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			scrollView = EditorGUILayout.BeginScrollView(scrollView, scrollViewStyle);

			EditorGUI.BeginDisabledGroup(!Settings.useDamage);
			EditorGUILayout.PrefixLabel(new GUIContent("Velocity Levels", "This helps the damage system to manage between low to high impacts"));

			EditorGUI.indentLevel++;

			float newDamageLowVelocity = ToolkitEditorUtility.NumberField("Low", Settings.damageLowVelocity, Utility.Units.Velocity, Settings.damageLowVelocity >= 10 ? (uint)1 : 2, Settings, "Change Velocity");

			if (Settings.damageLowVelocity != newDamageLowVelocity)
				Settings.damageLowVelocity = newDamageLowVelocity;

			float newDamageMediumVelocity = ToolkitEditorUtility.NumberField("Medium", Settings.damageMediumVelocity, Utility.Units.Velocity, Settings.damageMediumVelocity >= 10 ? (uint)1 : 2, Settings, "Change Velocity");

			if (Settings.damageMediumVelocity != newDamageMediumVelocity)
				Settings.damageMediumVelocity = newDamageMediumVelocity;

			float newDamageHighVelocity = ToolkitEditorUtility.NumberField("High", Settings.damageHighVelocity, Utility.Units.Velocity, Settings.damageHighVelocity >= 10 ? (uint)1 : 2, Settings, "Change Velocity");

			if (Settings.damageHighVelocity != newDamageHighVelocity)
				Settings.damageHighVelocity = newDamageHighVelocity;

			EditorGUI.indentLevel--;

			/*EditorGUILayout.Space();
			
			float newDamageRadius = VehicleEditorUtility.NumberField(new GUIContent("Radius", "The maximum damage radius around the main contact point"), Settings.damageRadius * 1000f, Utility.Units.DistanceAccurate, 3, Settings, "Change Radius", true, VehicleEditorUtility.CenterAlignedTextField) * .001f;

			if (Settings.damageRadius != newDamageRadius)
				Settings.damageRadius = newDamageRadius;

			float newDamageVertexRandomization = math.max(VehicleEditorUtility.NumberField(new GUIContent("Vertex Randomization", "The damage vertices randomization intensity inside the contact point radius"), Settings.damageVertexRandomization, VehicleEditorUtility.CenterAlignedTextField), 0f);

			if (Settings.damageVertexRandomization != newDamageVertexRandomization)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Randomization");

				Settings.damageVertexRandomization = newDamageVertexRandomization;

				EditorUtility.SetDirty(Settings);
			}

			float newMinimumVertDistanceForDamagedMesh =  math.max(VehicleEditorUtility.NumberField(new GUIContent("Minimum Repair Distance", "The minimum vertex distance to be detected as repaired or damaged"), Settings.minimumVertDistanceForDamagedMesh * 1000f, Utility.Units.SizeAccurate, 3, Settings, "Change Distance", true, VehicleEditorUtility.CenterAlignedTextField) * .001f, 1e-05f);

			if (Settings.minimumVertDistanceForDamagedMesh != newMinimumVertDistanceForDamagedMesh)
				Settings.minimumVertDistanceForDamagedMesh = newMinimumVertDistanceForDamagedMesh;

			float newMaximumDamage = VehicleEditorUtility.NumberField(new GUIContent("Maximum Depth", "The maximum damage depth into the vehicle's chassis"), Settings.maximumDamage * 1000f, Utility.Units.DistanceAccurate, 3, Settings, "Change Depth", true, VehicleEditorUtility.CenterAlignedTextField) * .001f;

			if (Settings.maximumDamage != newMaximumDamage)
				Settings.maximumDamage = newMaximumDamage;

			float newDamageIntensity = VehicleEditorUtility.Slider(new GUIContent("Intensity", "The damage velocity multiplier"), Settings.damageIntensity, 0f, 5f, Settings, "Change Intensity");

			if (Settings.damageIntensity != newDamageIntensity)
				Settings.damageIntensity = newDamageIntensity;*/

			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			bool newUseWheelHealth = ToolkitEditorUtility.ToggleButtons(new GUIContent("Wheel Health", "Should wheel tires use the health system to enable damaging?"), null, "On", "Off", Settings.useWheelHealth, Settings, "Switch Wheel Health");

			if (Settings.useWheelHealth != newUseWheelHealth)
				Settings.useWheelHealth = newUseWheelHealth;

			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup(!Settings.useWheelHealth);

			EditorGUI.indentLevel++;

			float newTireDamageIntensity = ToolkitEditorUtility.Slider(new GUIContent("Damage Intensity", "The wheel tire damage intensity. Increasing this may lead for the tire to damage much more faster than usual, depending on its thickness"), Settings.tireDamageIntensity, 0f, 10f, Settings, "Change Intensity");

			if (Settings.tireDamageIntensity != newTireDamageIntensity)
				Settings.tireDamageIntensity = newTireDamageIntensity;

			float newTireExplosionForce = ToolkitEditorUtility.NumberField(new GUIContent("Explosion Force", "The wheel tire explosion force"), Settings.tireExplosionForce, Utility.Units.Force, 1, Settings, "Change Force");

			if (Settings.tireExplosionForce != newTireExplosionForce)
				Settings.tireExplosionForce = newTireExplosionForce;

			Color newRimEdgeEmissionColor = EditorGUILayout.ColorField(new GUIContent("Rim Edge Emission Color", "The rims edges emission color used when the rims are heated up due to continuous friction with the ground"), Settings.rimEdgeEmissionColor, false, false, true);

			if (Settings.rimEdgeEmissionColor != newRimEdgeEmissionColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				newRimEdgeEmissionColor.a = 1f;
				Settings.rimEdgeEmissionColor = newRimEdgeEmissionColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			string newCustomRimShaderEmissionColorProperty = EditorGUILayout.TextField(new GUIContent("Custom Emission Property", "The custom shader emission color property identifier (if using one)"), Settings.customRimShaderEmissionColorProperty);

			if (Settings.customRimShaderEmissionColorProperty != newCustomRimShaderEmissionColorProperty)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Property");

				Settings.customRimShaderEmissionColorProperty = newCustomRimShaderEmissionColorProperty;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.EndDisabledGroup();

			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();
			/*EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUILayout.BeginHorizontal();

			float orgLabelWidth = EditorGUIUtility.labelWidth;
			bool jointChanged = false;

			EditorGUIUtility.labelWidth = 1f;

			EditorGUILayout.LabelField("Joints", EditorStyles.boldLabel);
			EditorGUILayout.LabelField(new GUIContent("Static", "This joint is for a fully repaired part"), EditorStyles.miniBoldLabel);
			EditorGUILayout.LabelField(new GUIContent("Damaged", "This joint is for a damaged part"), EditorStyles.miniBoldLabel);
			EditorGUILayout.EndHorizontal();

			VehicleChassisPart.JointsGroup newBumperJoints = Settings.chassisPartJoints.bumper;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent("Bumper", "The vehicle (front/rear) bumpers joints"));

			FixedJoint newBumperStaticJoint = EditorGUILayout.ObjectField(newBumperJoints.staticJoint, typeof(FixedJoint), false) as FixedJoint;

			if (newBumperJoints.staticJoint != newBumperStaticJoint)
			{
				newBumperJoints.staticJoint = newBumperStaticJoint;
				jointChanged = true;
			}

			HingeJoint newBumperDynamicJoint = EditorGUILayout.ObjectField(newBumperJoints.dynamicJoint, typeof(HingeJoint), false) as HingeJoint;

			if (newBumperJoints.dynamicJoint != newBumperDynamicJoint)
			{
				newBumperJoints.dynamicJoint = newBumperDynamicJoint;
				jointChanged = true;
			}

			if (jointChanged)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Joint");

				Settings.chassisPartJoints.bumper = newBumperJoints;
				jointChanged = false;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();

			VehicleChassisPart.JointsGroup newFenderJoints = Settings.chassisPartJoints.fender;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent("Fender", "The vehicle (front/rear) (left/right) fenders joints"));

			FixedJoint newFenderStaticJoint = EditorGUILayout.ObjectField(newFenderJoints.staticJoint, typeof(FixedJoint), false) as FixedJoint;

			if (newFenderJoints.staticJoint != newFenderStaticJoint)
			{
				newFenderJoints.staticJoint = newFenderStaticJoint;
				jointChanged = true;
			}

			HingeJoint newFenderDynamicJoint = EditorGUILayout.ObjectField(newFenderJoints.dynamicJoint, typeof(HingeJoint), false) as HingeJoint;

			if (newFenderJoints.dynamicJoint != newFenderDynamicJoint)
			{
				newFenderJoints.dynamicJoint = newFenderDynamicJoint;
				jointChanged = true;
			}

			if (jointChanged)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Joint");

				Settings.chassisPartJoints.fender = newFenderJoints;
				jointChanged = false;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();

			VehicleChassisPart.JointsGroup newHoodJoints = Settings.chassisPartJoints.hood;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent("Hood", "The vehicle hood(bonnet) joints"));

			FixedJoint newHoodStaticJoint = EditorGUILayout.ObjectField(newHoodJoints.staticJoint, typeof(FixedJoint), false) as FixedJoint;

			if (newHoodJoints.staticJoint != newHoodStaticJoint)
			{
				newHoodJoints.staticJoint = newHoodStaticJoint;
				jointChanged = true;
			}

			HingeJoint newHoodDynamicJoint = EditorGUILayout.ObjectField(newHoodJoints.dynamicJoint, typeof(HingeJoint), false) as HingeJoint;

			if (newHoodJoints.dynamicJoint != newHoodDynamicJoint)
			{
				newHoodJoints.dynamicJoint = newHoodDynamicJoint;
				jointChanged = true;
			}

			if (jointChanged)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Joint");

				Settings.chassisPartJoints.hood = newHoodJoints;
				jointChanged = false;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();

			VehicleChassisPart.JointsGroup newDoorJoints = Settings.chassisPartJoints.door;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent("Door", "The vehicle doors joints"));

			FixedJoint newDoorStaticJoint = EditorGUILayout.ObjectField(newDoorJoints.staticJoint, typeof(FixedJoint), false) as FixedJoint;

			if (newDoorJoints.staticJoint != newDoorStaticJoint)
			{
				newDoorJoints.staticJoint = newDoorStaticJoint;
				jointChanged = true;
			}

			HingeJoint newDoorDynamicJoint = EditorGUILayout.ObjectField(newDoorJoints.dynamicJoint, typeof(HingeJoint), false) as HingeJoint;

			if (newDoorJoints.dynamicJoint != newDoorDynamicJoint)
			{
				newDoorJoints.dynamicJoint = newDoorDynamicJoint;
				jointChanged = true;
			}

			if (jointChanged)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Joint");

				Settings.chassisPartJoints.door = newDoorJoints;
				jointChanged = false;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();

			VehicleChassisPart.JointsGroup newDoorMirrorJoints = Settings.chassisPartJoints.doorMirror;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent("Door Mirror", "The vehicle door mirrors joints"));

			FixedJoint newDoorMirrorStaticJoint = EditorGUILayout.ObjectField(newDoorMirrorJoints.staticJoint, typeof(FixedJoint), false) as FixedJoint;

			if (newDoorMirrorJoints.staticJoint != newDoorMirrorStaticJoint)
			{
				newDoorMirrorJoints.staticJoint = newDoorMirrorStaticJoint;
				jointChanged = true;
			}

			HingeJoint newDoorMirrorDynamicJoint = EditorGUILayout.ObjectField(newDoorMirrorJoints.dynamicJoint, typeof(HingeJoint), false) as HingeJoint;

			if (newDoorMirrorJoints.dynamicJoint != newDoorMirrorDynamicJoint)
			{
				newDoorMirrorJoints.dynamicJoint = newDoorMirrorDynamicJoint;
				jointChanged = true;
			}

			if (jointChanged)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Joint");

				Settings.chassisPartJoints.doorMirror = newDoorMirrorJoints;
				jointChanged = false;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();

			VehicleChassisPart.JointsGroup newSideSkirtJoints = Settings.chassisPartJoints.sideSkirt;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent("Side Skirt", "The vehicle side-skirts joints"));

			FixedJoint newSideSkirtStaticJoint = EditorGUILayout.ObjectField(newSideSkirtJoints.staticJoint, typeof(FixedJoint), false) as FixedJoint;

			if (newSideSkirtJoints.staticJoint != newSideSkirtStaticJoint)
			{
				newSideSkirtJoints.staticJoint = newSideSkirtStaticJoint;
				jointChanged = true;
			}

			HingeJoint newSideSkirtDynamicJoint = EditorGUILayout.ObjectField(newSideSkirtJoints.dynamicJoint, typeof(HingeJoint), false) as HingeJoint;

			if (newSideSkirtJoints.dynamicJoint != newSideSkirtDynamicJoint)
			{
				newSideSkirtJoints.dynamicJoint = newSideSkirtDynamicJoint;
				jointChanged = true;
			}

			if (jointChanged)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Joint");

				Settings.chassisPartJoints.sideSkirt = newSideSkirtJoints;
				jointChanged = false;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();

			VehicleChassisPart.JointsGroup newExhaustJoints = Settings.chassisPartJoints.exhaust;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent("Exhaust", "The vehicle exhaust joints"));

			FixedJoint newExhaustStaticJoint = EditorGUILayout.ObjectField(newExhaustJoints.staticJoint, typeof(FixedJoint), false) as FixedJoint;

			if (newExhaustJoints.staticJoint != newExhaustStaticJoint)
			{
				newExhaustJoints.staticJoint = newExhaustStaticJoint;
				jointChanged = true;
			}

			HingeJoint newExhaustDynamicJoint = EditorGUILayout.ObjectField(newExhaustJoints.dynamicJoint, typeof(HingeJoint), false) as HingeJoint;

			if (newExhaustJoints.dynamicJoint != newExhaustDynamicJoint)
			{
				newExhaustJoints.dynamicJoint = newExhaustDynamicJoint;
				jointChanged = true;
			}

			if (jointChanged)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Joint");

				Settings.chassisPartJoints.exhaust = newExhaustJoints;
				jointChanged = false;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();

			VehicleChassisPart.JointsGroup newTrunkJoints = Settings.chassisPartJoints.trunk;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent("Trunk", "The vehicle trunk(boot) joints"));

			FixedJoint newTrunkStaticJoint = EditorGUILayout.ObjectField(newTrunkJoints.staticJoint, typeof(FixedJoint), false) as FixedJoint;

			if (newTrunkJoints.staticJoint != newTrunkStaticJoint)
			{
				newTrunkJoints.staticJoint = newTrunkStaticJoint;
				jointChanged = true;
			}

			HingeJoint newTrunkDynamicJoint = EditorGUILayout.ObjectField(newTrunkJoints.dynamicJoint, typeof(HingeJoint), false) as HingeJoint;

			if (newTrunkJoints.dynamicJoint != newTrunkDynamicJoint)
			{
				newTrunkJoints.dynamicJoint = newTrunkDynamicJoint;
				jointChanged = true;
			}

			if (jointChanged)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Joint");

				Settings.chassisPartJoints.trunk = newTrunkJoints;
				jointChanged = false;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();

			VehicleChassisPart.JointsGroup newWingJoints = Settings.chassisPartJoints.wing;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent("Wing", "The vehicle wing joints"));

			FixedJoint newWingStaticJoint = EditorGUILayout.ObjectField(newWingJoints.staticJoint, typeof(FixedJoint), false) as FixedJoint;

			if (newWingJoints.staticJoint != newWingStaticJoint)
			{
				newWingJoints.staticJoint = newWingStaticJoint;
				jointChanged = true;
			}

			HingeJoint newWingDynamicJoint = EditorGUILayout.ObjectField(newWingJoints.dynamicJoint, typeof(HingeJoint), false) as HingeJoint;

			if (newWingJoints.dynamicJoint != newWingDynamicJoint)
			{
				newWingJoints.dynamicJoint = newWingDynamicJoint;
				jointChanged = true;
			}

			if (jointChanged)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Joint");

				Settings.chassisPartJoints.wing = newWingJoints;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();

			EditorGUIUtility.labelWidth = orgLabelWidth;

			EditorGUI.EndDisabledGroup();

			if (!EditorApplication.isPlaying)
				EditorGUILayout.HelpBox("Please make sure all the following joints (Bumper, Fender, Door, Door Mirror, Side Skirt and Wing) are made as a reference to the front left side of each part, so the joint can be optimized and ready to be auto customized by our damage system.", MessageType.None);*/

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EditorGUILayout.HelpBox("The damage system is still in its Beta phase, we'll add more features to it in the near future, so keep your MVC copy updated by downloading the latest versions.", MessageType.Info);
			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}
		private void CameraSettingsEditor()
		{
			int firstFollowerIndex = Array.FindIndex(Settings.Cameras, camera => camera.Type == VehicleCamera.CameraType.Follower);
			GUIStyle largeLabel = new(EditorStyles.boldLabel)
			{
				fontSize = 17,
				fixedHeight = 20f
			};
			GUIContent label = new("Cameras");

			EditorGUILayout.BeginHorizontal();

			if (exportPresets || !importPresetsJson.IsNullOrEmpty())
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					ExitImportExportPresets();

				label.text = $"{(exportPresets ? "Exporting" : "Importing")} Cameras";

				GUILayout.Space(5f);
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Select All", EditorStyles.miniButtonLeft, GUILayout.Width(85f)))
					importExportCamerasStatesList = Enumerable.Repeat(true, importExportCamerasList.Length).ToArray();

				if (GUILayout.Button("Deselect All", EditorStyles.miniButtonRight, GUILayout.Width(85f)))
					importExportCamerasStatesList = Enumerable.Repeat(false, importExportCamerasList.Length).ToArray();

				EditorGUI.BeginDisabledGroup(importExportCamerasStatesList == null || importExportCamerasStatesList.Where(state => state).Count() < 1);

				if (GUILayout.Button(exportPresets ? EditorUtilities.Icons.Save : EditorUtilities.Icons.Check, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					if (exportPresets)
						FinishExportPresets<CameraPresets>();
					else
						FinishImportPresets();
				}

				EditorGUI.EndDisabledGroup();
			}
			else if (currentCamera < -1)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					SaveCamera();

				label.text = "Sorting Cameras";

				GUILayout.Space(5f);
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
			}
			else if (currentCamera < 0)
			{
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
				GUILayout.FlexibleSpace();
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				if (GUILayout.Button("Import", EditorStyles.miniButtonLeft, GUILayout.Width(75f)))
					ImportCameraPresets();

				if (GUILayout.Button("Export", EditorStyles.miniButtonRight, GUILayout.Width(75f)))
					ExportPresets();

				EditorGUI.BeginDisabledGroup(Settings.Cameras.Length < 2);

				if (GUILayout.Button(EditorUtilities.Icons.Sort, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					SortCameras();

				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					AddCamera();

				EditorGUI.BeginDisabledGroup(Settings.Cameras.Length < 2);

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You are trying to remove all the available camera presets. Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Cameras");
						Settings.ResetCameras();
						EditorUtility.SetDirty(Settings);
					}

				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				currentCamera = math.clamp(currentCamera, 0, Settings.Cameras.Length - 1);

				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					SaveCamera();

				label.text = $"{Settings.Cameras[math.clamp(currentCamera, 0, Settings.Cameras.Length - 1)].Name} Configurations";

				GUILayout.Space(5f);
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
				GUILayout.FlexibleSpace();
			}

			GUILayout.Space(20f);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			scrollView = EditorGUILayout.BeginScrollView(scrollView, scrollViewStyle);

			if (currentCamera < 0)
				for (int i = 0; i < (exportPresets || !importPresetsJson.IsNullOrEmpty() ? importExportCamerasList.Length : Settings.Cameras.Length); i++)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);

					if (exportPresets || !importPresetsJson.IsNullOrEmpty())
						importExportCamerasStatesList[i] = EditorGUILayout.ToggleLeft(importExportCamerasList[i].name, importExportCamerasStatesList[i], EditorStyles.miniBoldLabel);
					else
					{
						if (currentCamera < -1)
						{
							EditorGUI.BeginDisabledGroup(i == 0);

							if (GUILayout.Button(EditorUtilities.Icons.CaretUp, ToolkitEditorUtility.UnstretchableMiniButtonLeft))
								MoveCamera(i, i - 1);

							EditorGUI.EndDisabledGroup();
							EditorGUI.BeginDisabledGroup(i >= Settings.Cameras.Length - 1);

							if (GUILayout.Button(EditorUtilities.Icons.CaretDown, ToolkitEditorUtility.UnstretchableMiniButtonRight))
								MoveCamera(i, i + 1);

							EditorGUI.EndDisabledGroup();
							GUILayout.Space(5f);
						}

						EditorGUILayout.LabelField(Settings.Cameras[i].Name, EditorStyles.miniBoldLabel);

						if (currentCamera > -2)
						{
							if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
								EditCamera(i);

							EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

							if (GUILayout.Button(EditorUtilities.Icons.Clone, ToolkitEditorUtility.UnstretchableMiniButtonWide))
								DuplicateCamera(i);

							if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
								RemoveCamera(i);

							EditorGUI.EndDisabledGroup();
						}
					}

					EditorGUILayout.EndHorizontal();
				}
			else
			{
				VehicleCamera camera = Settings.Cameras[currentCamera];

				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				cameraName = EditorGUILayout.TextField(new GUIContent("Name", "The camera name"), cameraName);

				EditorGUILayout.Space();

				VehicleCamera.CameraType newType = (VehicleCamera.CameraType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The camera type"), camera.Type);

				if (camera.Type != newType)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Type");

					camera.Type = newType;

					EditorUtility.SetDirty(Settings);
				}

				if (camera.Type == VehicleCamera.CameraType.Pivot)
				{
					EditorGUI.indentLevel++;

					VehicleCamera.PivotPositionType newPivotPosition = (VehicleCamera.PivotPositionType)EditorGUILayout.EnumPopup(new GUIContent("Pivot Position", "The follower pivot's desired position within the vehicle"), camera.PivotPosition);

					if (camera.PivotPosition != newPivotPosition)
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Position");

						camera.PivotPosition = newPivotPosition;

						EditorUtility.SetDirty(Settings);
					}

					EditorGUI.indentLevel--;
				}

				EditorGUI.EndDisabledGroup();

				switch (camera.Type)
				{
					case VehicleCamera.CameraType.Pivot:
						VehicleCamera.PivotFeature newPivotFeatures = (VehicleCamera.PivotFeature)EditorGUILayout.EnumFlagsField(new GUIContent("Features", "The pivot features mask"), camera.pivotFeatures);

						if (camera.pivotFeatures != newPivotFeatures)
						{
							Undo.RegisterCompleteObjectUndo(Settings, "Change Features");

							camera.pivotFeatures = newPivotFeatures;

							EditorUtility.SetDirty(Settings);
						}

						break;

					default:
						VehicleCamera.FollowerFeature newFollowerFeatures = (VehicleCamera.FollowerFeature)EditorGUILayout.EnumFlagsField(new GUIContent("Features", "The follower features mask"), camera.followerFeatures);

						if (camera.followerFeatures != newFollowerFeatures)
						{
							Undo.RegisterCompleteObjectUndo(Settings, "Change Features");

							camera.followerFeatures = newFollowerFeatures;

							EditorUtility.SetDirty(Settings);
						}

						break;
				}

				EditorGUILayout.Space();

				if (camera.Type == VehicleCamera.CameraType.Follower)
				{
					EditorGUILayout.LabelField(new GUIContent("Pivot", "The point that the camera should rotate around and look at, ie. focus point"));

					EditorGUI.indentLevel++;

					int3 newPivotPoint = camera.PivotPoint;

					newPivotPoint.x = EditorGUILayout.Popup("X", camera.PivotPoint.x + 1, new string[] { "Left", "Center", "Right" }) - 1;
					newPivotPoint.y = EditorGUILayout.Popup("Y", camera.PivotPoint.y + 1, new string[] { "Top", "Middle", "Bottom" }) - 1;
					newPivotPoint.z = EditorGUILayout.Popup("Z", camera.PivotPoint.z + 1, new string[] { "Front", "Middle", "Rear" }) - 1;

					if (!camera.PivotPoint.Equals(newPivotPoint))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Pivot");

						camera.PivotPoint = newPivotPoint;

						EditorUtility.SetDirty(Settings);
					}

					EditorGUI.indentLevel--;

					EditorGUILayout.Space();

					VehicleCamera.ImpactAxis newImpactAxes = (VehicleCamera.ImpactAxis)EditorGUILayout.EnumFlagsField(new GUIContent("Impact Shaking", "The impact axes the impact shaking is going to be simulated on"), camera.ImpactAxes);

					if (camera.ImpactAxes != newImpactAxes)
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Axes");

						camera.ImpactAxes = newImpactAxes;

						EditorUtility.SetDirty(Settings);
					}

					EditorGUI.BeginDisabledGroup(camera.ImpactAxes == VehicleCamera.ImpactAxis.None);

					EditorGUI.indentLevel++;

					VehicleCamera.ShakingType newImpactShaking = (VehicleCamera.ShakingType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The shaking method the camera is going to use in case the followed vehicle is being hit or having a heavy impact"), camera.impactShaking);

					if (camera.impactShaking != newImpactShaking)
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Type");

						camera.impactShaking = newImpactShaking;

						EditorUtility.SetDirty(Settings);
					}

					EditorGUI.indentLevel--;

					EditorGUI.EndDisabledGroup();
				}

				bool canVerticalOrbit = camera.HasFollowerFeature(VehicleCamera.FollowerFeature.VerticalOrbit) || camera.HasPivotFeature(VehicleCamera.PivotFeature.VerticalOrbit);
				bool canHorizontalOrbit = camera.HasFollowerFeature(VehicleCamera.FollowerFeature.HorizontalOrbit) || camera.HasPivotFeature(VehicleCamera.PivotFeature.HorizontalOrbit);

				if (canVerticalOrbit || canHorizontalOrbit)
				{
					EditorGUILayout.Space();
					EditorGUILayout.LabelField(new GUIContent("Mouse Orbit", "The mouse orbit intervals around the followed vehicle. Measured in Degrees (°)"));

					if (canHorizontalOrbit)
					{
						EditorGUI.indentLevel++;

						Utility.Interval newOrbitIntervalX = ToolkitEditorUtility.IntervalField(new GUIContent(canVerticalOrbit && canHorizontalOrbit ? "Horizontal (X)" : "Interval", "The sideways orbit interval"), null, "Min", "Max", "°", "Degrees", 1, camera.orbitInterval.x, Settings, "Change Angle", false);

						if (camera.orbitInterval.x != newOrbitIntervalX)
							camera.orbitInterval.x = newOrbitIntervalX;

						EditorGUI.indentLevel++;

						bool newInvertXOrbit = ToolkitEditorUtility.ToggleButtons(new GUIContent("Invert", $"Should the camera{(canHorizontalOrbit && canVerticalOrbit ? " X" : "")} orbit be inverted?"), null, "Yes", "No", camera.invertXOrbit, Settings, "Invert Orbit");

						if (camera.invertXOrbit != newInvertXOrbit)
							camera.invertXOrbit = newInvertXOrbit;

						EditorGUI.indentLevel--;
						EditorGUI.indentLevel--;
					}

					if (canVerticalOrbit)
					{
						EditorGUI.indentLevel++;

						Utility.Interval newOrbitIntervalY = ToolkitEditorUtility.IntervalField(new GUIContent(canVerticalOrbit && canHorizontalOrbit ? "Vertical (Y)" : "Interval", "The perpendicular orbit interval"), null, "Min", "Max", "°", "Degrees", 1, camera.orbitInterval.y, Settings, "Change Angle", false);

						if (camera.orbitInterval.y != newOrbitIntervalY)
							camera.orbitInterval.y = newOrbitIntervalY;

						EditorGUI.indentLevel++;

						bool newInvertYOrbit = ToolkitEditorUtility.ToggleButtons(new GUIContent("Invert", $"Should the camera{(canHorizontalOrbit && canVerticalOrbit ? " Y" : "")} orbit be inverted?"), null, "Yes", "No", camera.invertYOrbit, Settings, "Invert Orbit");

						if (camera.invertYOrbit != newInvertYOrbit)
							camera.invertYOrbit = newInvertYOrbit;

						EditorGUI.indentLevel--;
						EditorGUI.indentLevel--;
					}

					EditorGUI.indentLevel++;

					bool newOrbitUsingMouseButton = ToolkitEditorUtility.ToggleButtons(new GUIContent("Mouse Button", "Force the camera to orbit only when pressing a specified mouse button"), null, "On", "Off", camera.orbitUsingMouseButton, Settings, "Switch Button");

					if (camera.orbitUsingMouseButton != newOrbitUsingMouseButton)
						camera.orbitUsingMouseButton = newOrbitUsingMouseButton;

					if (camera.orbitUsingMouseButton)
					{
						EditorGUI.indentLevel++;

						MouseButton newOrbitMouseButton = (MouseButton)EditorGUILayout.EnumPopup(new GUIContent("Button", "The mouse button used to orbit the camera"), camera.orbitMouseButton);

						if (camera.orbitMouseButton != newOrbitMouseButton)
						{
							Undo.RegisterCompleteObjectUndo(Settings, "Change Button");

							camera.orbitMouseButton = newOrbitMouseButton;

							EditorUtility.SetDirty(Settings);
						}

						EditorGUI.indentLevel--;
					}

					bool newSkipDistantOrbit = ToolkitEditorUtility.ToggleButtons(new GUIContent("Skip Distant Angles", "Should the camera skip distant orbit angles?"), null, "Yes", "No", camera.skipDistantOrbit, Settings, "Switch Skip Angles");

					if (camera.skipDistantOrbit != newSkipDistantOrbit)
						camera.skipDistantOrbit = newSkipDistantOrbit;

					EditorGUI.BeginDisabledGroup(!camera.skipDistantOrbit);

					EditorGUI.indentLevel++;

					float newOrbitSkipAngle = ToolkitEditorUtility.Slider(new GUIContent("Angle", "The angle which the camera should skip orbit smoothing at"), camera.OrbitSkipAngle, 1f, 180f, "°", "Degrees", Settings, "Change Angle");

					if (camera.OrbitSkipAngle != newOrbitSkipAngle)
						camera.OrbitSkipAngle = newOrbitSkipAngle;

					EditorGUI.indentLevel--;

					EditorGUI.EndDisabledGroup();

					float newOrbitTimeout = ToolkitEditorUtility.NumberField(new GUIContent("Timeout", "The camera orbit timeout"), camera.OrbitTimeout, Utility.Units.Time, (uint)(camera.OrbitTimeout < 10f ? 2 : 1), Settings, "Change Timeout");

					if (camera.OrbitTimeout != newOrbitTimeout)
						camera.OrbitTimeout = newOrbitTimeout;

					EditorGUI.indentLevel--;
				}

				EditorGUILayout.Space();

				if (camera.HasFollowerFeature(VehicleCamera.FollowerFeature.NOSFieldOfView) || camera.HasPivotFeature(VehicleCamera.PivotFeature.SpeedFieldOfView) || camera.HasPivotFeature(VehicleCamera.PivotFeature.NOSFieldOfView))
				{
					Utility.Interval newFieldOfViewInterval = ToolkitEditorUtility.IntervalField(new GUIContent("Field Of View", "The camera field of view interval from Idle to High velocities"), null, "Idle", "High", "°", "Degrees", 1, camera.FieldOfViewInterval, Settings, "Change Angle", false);

					if (camera.FieldOfViewInterval != newFieldOfViewInterval)
						camera.FieldOfViewInterval = newFieldOfViewInterval;
				}
				else
				{
					float newFieldOfView = ToolkitEditorUtility.NumberField(new GUIContent("Field Of View", "The camera default Field Of View"), camera.FieldOfViewInterval.Min, "°", "Degrees", 1, Settings, "Change Field Of View");

					if (camera.FieldOfViewInterval.Min != newFieldOfView)
					{
						Utility.Interval newFieldOfViewInterval = camera.FieldOfViewInterval;

						newFieldOfViewInterval.Max = newFieldOfViewInterval.Min = newFieldOfView;
						camera.FieldOfViewInterval = newFieldOfViewInterval;
					}
				}

				if (camera.HasFollowerFeature(VehicleCamera.FollowerFeature.SpeedShaking) || camera.HasPivotFeature(VehicleCamera.PivotFeature.SpeedShaking))
				{
					EditorGUILayout.Space();

					Utility.Interval newShakeSpeedInterval = ToolkitEditorUtility.IntervalField(new GUIContent("Shaking Speed", "The camera shaking speed interval"), null, "Min", "Max", Utility.Units.Speed, 2, camera.ShakeSpeedInterval, Settings, "Change Speed", false);

					if (camera.ShakeSpeedInterval != newShakeSpeedInterval)
						camera.ShakeSpeedInterval = newShakeSpeedInterval;
				}

				if (camera.HasFollowerFeature(VehicleCamera.FollowerFeature.SteeringTilt) || camera.HasPivotFeature(VehicleCamera.PivotFeature.SteeringTilt))
				{
					EditorGUILayout.Space();

					float newTiltAngle = ToolkitEditorUtility.NumberField(new GUIContent("Tilt Angle", "The maximum camera tilt angle when the followed vehicle is fully steering"), camera.TiltAngle, "°", "Degrees", 1, Settings, "Change Angle");

					if (camera.TiltAngle != newTiltAngle)
						camera.TiltAngle = newTiltAngle;

					EditorGUI.indentLevel++;

					bool newInvertTiltAngle = ToolkitEditorUtility.ToggleButtons(new GUIContent("Invert", $"Should camera tilt angle be inverted?"), null, "Yes", "No", camera.invertTiltAngle, Settings, "Switch Invert");

					if (camera.invertTiltAngle != newInvertTiltAngle)
						camera.invertTiltAngle = newInvertTiltAngle;

					EditorGUI.indentLevel--;
				}

				EditorGUILayout.Space();
			}

			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}
		private void PlayerInputsSettingsEditor()
		{
			GUIStyle largeLabel = new(EditorStyles.boldLabel)
			{
				fontSize = 17,
				fixedHeight = 20f
			};

			EditorGUILayout.LabelField("Player Inputs", largeLabel);
			EditorGUILayout.Space();

			scrollView = EditorGUILayout.BeginScrollView(scrollView, scrollViewStyle);

			EditorGUILayout.BeginHorizontal();

			ToolkitSettings.InputSystem newInputSystem = (ToolkitSettings.InputSystem)EditorGUILayout.EnumPopup(new GUIContent("Input System", "The input system the MVC is going to use to control vehicles"), Settings.inputSystem);

			if (Settings.inputSystem != newInputSystem)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Input System");

				Settings.inputSystem = newInputSystem;

				EditorUtility.SetDirty(Settings);
			}

			if (Settings.inputSystem == ToolkitSettings.InputSystem.InputsManager)
				if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					InputsManagerEditor.OpenWindow();

			EditorGUILayout.EndHorizontal();

			if (Settings.inputSystem == ToolkitSettings.InputSystem.UnityLegacyInputManager)
				EditorGUILayout.HelpBox("Unity's Legacy Input Manager doesn't give you the ability to choose which gamepad/joystick the Player uses to control his vehicle. To use the mentioned feature, please consider using another input system.", MessageType.Info);
			else
			{
				EditorGUILayout.Space();

				ToolkitSettings.GamepadRumbleType newGamepadRumbleType = (ToolkitSettings.GamepadRumbleType)EditorGUILayout.EnumPopup(new GUIContent("Gamepad Rumble", "Gamepad Rumble has the following options:\r\nOff: Disables gamepad rumble.\r\nFollow Camera: Using this mode, the gamepad rumble will follow the camera's shaking behaviour.\r\nIndependent: This mode allows to customize the gamepad rumble behaviour independently from the selected camera."), Settings.gamepadRumbleType);

				if (Settings.gamepadRumbleType != newGamepadRumbleType)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Type");

					Settings.gamepadRumbleType = newGamepadRumbleType;

					EditorUtility.SetDirty(Settings);
				}

				EditorGUI.indentLevel++;

				EditorGUI.BeginDisabledGroup(Settings.gamepadRumbleType == ToolkitSettings.GamepadRumbleType.Off);

				ToolkitSettings.GamepadRumbleMask newGamepadRumbleMask = (ToolkitSettings.GamepadRumbleMask)EditorGUILayout.EnumFlagsField(new GUIContent("Behaviours", "Using this mask, you are able to select multiple rumble behaviours as explained below:\r\nSpeed: Rumble at high speeds.\r\nOff Road: Rumble when car is running on top of an Off-Road ground surface.\r\nNOS: Rumble when NOS is in action.\r\nSideways Slip: Rumble when drifting sideways.\r\nForward Slip: Rumble when slipping while accelerating.\r\nBrake Slip: Rumble when slipping while braking."), Settings.gamepadRumbleMask);

				if (Settings.gamepadRumbleMask != newGamepadRumbleMask)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Mask");

					Settings.gamepadRumbleMask = newGamepadRumbleMask;

					EditorUtility.SetDirty(Settings);
				}

				if (Settings.gamepadRumbleMask.HasFlag(ToolkitSettings.GamepadRumbleMask.Speed))
				{
					EditorGUI.indentLevel++;

					Utility.Interval newGamepadRumbleSpeedInterval = ToolkitEditorUtility.IntervalField(new GUIContent("Speed Interval", "The camera shaking speed interval"), null, "Min", "Max", Utility.Units.Speed, 2, Settings.GamepadRumbleSpeedInterval, Settings, "Change Speed", false);

					if (Settings.GamepadRumbleSpeedInterval != newGamepadRumbleSpeedInterval)
						Settings.GamepadRumbleSpeedInterval = newGamepadRumbleSpeedInterval;

					EditorGUI.indentLevel--;
				}

				bool newUseGamepadLevelledRumble = ToolkitEditorUtility.ToggleButtons(new GUIContent("Levelled Rumble", "This features allows for the gamepad rumble to vary from low to high"), null, "On", "Off", Settings.useGamepadLevelledRumble, Settings, "Switch Levelled Rumble");

				if (Settings.useGamepadLevelledRumble != newUseGamepadLevelledRumble)
					Settings.useGamepadLevelledRumble = newUseGamepadLevelledRumble;

				bool newSeparateGamepadRumbleSides = ToolkitEditorUtility.ToggleButtons(new GUIContent("Merge Rumble Sides", "If merged, the rumble on both sides of the gamepad will be the same"), null, "On", "Off", Settings.useSeparateGamepadRumbleSides, Settings, "Switch Separate Rumble");

				if (Settings.useSeparateGamepadRumbleSides == newSeparateGamepadRumbleSides)
					Settings.useSeparateGamepadRumbleSides = !newSeparateGamepadRumbleSides;

				EditorGUI.EndDisabledGroup();

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();

			bool newUseMobileInputs = ToolkitEditorUtility.ToggleButtons(new GUIContent("Mobile Inputs", "Enabling this feature will allow you to control the player's vehicle using UI & mobile sensors"), null, "On", "Off", Settings.useMobileInputs, Settings, "Switch Mobile Inputs");

			if (Settings.useMobileInputs != newUseMobileInputs)
				Settings.useMobileInputs = newUseMobileInputs;

			EditorGUILayout.Space();

			if (Settings.inputSystem == ToolkitSettings.InputSystem.InputsManager && (!InputsManager.DataAssetExists || !InputsManager.DataLoaded))
			{
				EditorGUILayout.HelpBox("It seems that the Inputs Manager data asset has been removed or doesn't exist anymore!", MessageType.Error);

				if (GUILayout.Button("Try a quick fix", EditorStyles.miniButton))
					TryInputsManagerQuickFix();
			}
			else
			{
				if (Settings.Problems.HasPlayerInputsProblems)
				{
					EditorGUILayout.HelpBox("Some inputs have duplicate names. Make sure every single input has his own unique name.", MessageType.Warning);
					EditorGUILayout.Space();
				}

				string[] inputsList = InputsManager.DataLoaded ? InputsManager.GetInputsNames() : new string[] { };

				InputEditor(ref Settings.steerInput, new GUIContent("Steering Wheel", "Steering wheel input"), inputsList);
				InputEditor(ref Settings.fuelInput, new GUIContent("Fuel Pedal", "Fuel pedal input"), inputsList);
				InputEditor(ref Settings.brakeInput, new GUIContent("Brake Pedal", "Brake pedal input"), inputsList);
				InputEditor(ref Settings.clutchInput, new GUIContent("Clutch Pedal", "Clutch pedal input"), inputsList);
				InputEditor(ref Settings.handbrakeInput, new GUIContent("Handbrake", "Handbrake input"), inputsList);
				InputEditor(ref Settings.gearShiftUpButtonInput, new GUIContent("Gear Shift Up", "Gear shift up input"), inputsList);
				InputEditor(ref Settings.gearShiftDownButtonInput, new GUIContent("Gear Shift Down", "Gear shift down input"), inputsList);
				InputEditor(ref Settings.engineStartSwitchInput, new GUIContent("Turn On/Off Engine", "Engine switch input"), inputsList);

				if (Settings.engineStartMode == ToolkitSettings.EngineStartMode.Always)
				{
					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(EditorGUIUtility.labelWidth + 5f);
					EditorGUILayout.BeginVertical();
					EditorGUILayout.HelpBox("Although you can change the `Turn On/Off Engine` input, it's useless as long as `Engine Start Mode` is set to `Always` in the `General` settings tab.", MessageType.Info);
					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
				}

				InputEditor(ref Settings.NOSButtonInput, new GUIContent("NOS", "NOS boost input"), inputsList);
				InputEditor(ref Settings.launchControlSwitchInput, new GUIContent("Turn On/Off Launch Control", "Vehicle's launch control switch input"), inputsList);
				//InputEditor(ref Settings.hornButtonInput, new GUIContent("Horn", "Horn input"), inputsList);
				InputEditor(ref Settings.resetButtonInput, new GUIContent("Reset", "Vehicle reset input"), inputsList);
				InputEditor(ref Settings.changeCameraButtonInput, new GUIContent("Change Camera", "Camera change mode input"), inputsList);
				InputEditor(ref Settings.forwardCameraViewInput, new GUIContent("Camera Forward View", "Camera view input on the Z axis"), inputsList);
				InputEditor(ref Settings.sidewaysCameraViewInput, new GUIContent("Camera Sideways View", "Camera view input on the X axis"), inputsList);
				InputEditor(ref Settings.lightSwitchInput, new GUIContent("Turn On/Off Light", "Light switch input"), inputsList);
				InputEditor(ref Settings.highBeamLightSwitchInput, new GUIContent("Turn On/Off HighBeam", "High-beam light switch input"), inputsList);

				if (Settings.inputSystem == ToolkitSettings.InputSystem.InputsManager)
				{
					EditorGUI.indentLevel++;

					bool newSwitchHighBeamOnDoublePress = ToolkitEditorUtility.ToggleButtons(new GUIContent("Key Press", "If Double is selected, the player has to double press the high beam light switch button to turn them on, holding the button turns on the lights as well but they turn off as the player releases the button"), null, "Double", "Single", Settings.switchHighBeamOnDoublePress, Settings, "Change Press Type");

					if (Settings.switchHighBeamOnDoublePress != newSwitchHighBeamOnDoublePress)
						Settings.switchHighBeamOnDoublePress = newSwitchHighBeamOnDoublePress;

					EditorGUI.indentLevel--;
				}

#if !MVC_COMMUNITY
				InputEditor(ref Settings.interiorLightSwitchInput, new GUIContent("Turn On/Off Interior Light", "Interior light switch button input"), inputsList);
#endif
				InputEditor(ref Settings.sideSignalLeftLightSwitchInput, new GUIContent("Turn On/Off Left Signal Light", "Left side signal (indicator) lights switch button input"), inputsList);
				InputEditor(ref Settings.sideSignalRightLightSwitchInput, new GUIContent("Turn On/Off Right Signal Light", "Right side signal (indicator) lights switch button input"), inputsList);
				InputEditor(ref Settings.hazardLightsSwitchInput, new GUIContent("Turn On/Off Hazard Lights", "Left & right side signal (indicator) lights switch button input"), inputsList);
				InputEditor(ref Settings.trailerLinkSwitchInput, new GUIContent("Link/Un-link Trailer", "Links/un-links the closest trailer to the player's vehicle"), inputsList);

				EditorGUI.indentLevel++;

				bool newLinkTrailerManually = !ToolkitEditorUtility.ToggleButtons(new GUIContent("Trailer Linking", "How should the trailer linking be?"), null, "Auto", "Manual", !Settings.linkTrailerManually, Settings, "Change Linking");

				if (Settings.linkTrailerManually != newLinkTrailerManually)
					Settings.linkTrailerManually = newLinkTrailerManually;

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}
		private void InputEditor(ref string input, GUIContent content, string[] inputsList)
		{
			bool useInputsManager = Settings.inputSystem == ToolkitSettings.InputSystem.InputsManager;
			string newInput;

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (useInputsManager)
			{
				EditorGUILayout.BeginHorizontal();

				int newIndexIndex = EditorGUILayout.Popup(content, Array.IndexOf(inputsList, input), inputsList);

				newInput = newIndexIndex > -1 && newIndexIndex < inputsList.Length ? inputsList[newIndexIndex] : input;

				if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					InputsManagerEditor.OpenWindow(InputsManager.GetInput(newInput));

				EditorGUILayout.EndHorizontal();
			}
			else
				newInput = EditorGUILayout.TextField(content, input);

			if (input != newInput)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Input");

				input = newInput;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.EndDisabledGroup();
		}
		private void EnginesChargersSettingsEditor()
		{
			GUIStyle largeLabel = new(EditorStyles.boldLabel)
			{
				fontSize = 17,
				fixedHeight = 20f
			};
			GUIContent label = new("Engines & Chargers");

			EditorGUILayout.BeginHorizontal();

			if (exportPresets || !importPresetsJson.IsNullOrEmpty())
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					ExitImportExportPresets();

				label.text = $"{(exportPresets ? "Exporting" : "Importing")} Engines & Chargers";

				GUILayout.Space(5f);
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Select All", EditorStyles.miniButtonLeft, GUILayout.Width(85f)))
				{
					int choice = EditorUtility.DisplayDialogComplex("Multiversal Vehicle Controller: Info", "Which Items you want to select?", "Engines", "Chargers", "Both");

					if (choice == 2 || choice == 0)
						importExportEnginesStatesList = Enumerable.Repeat(true, importExportEnginesList.Length).ToArray();

					if (choice == 2 || choice == 1)
						importExportChargersStatesList = Enumerable.Repeat(true, importExportChargersList.Length).ToArray();
				}

				if (GUILayout.Button("Deselect All", EditorStyles.miniButtonRight, GUILayout.Width(85f)))
				{
					int choice = EditorUtility.DisplayDialogComplex("Multiversal Vehicle Controller: Info", "Which items you want to deselect?", "Engines", "Chargers", "Both");

					if (choice == 2 || choice == 0)
						importExportEnginesStatesList = Enumerable.Repeat(false, importExportEnginesList.Length).ToArray();

					if (choice == 2 || choice == 1)
						importExportChargersStatesList = Enumerable.Repeat(false, importExportChargersList.Length).ToArray();
				}

				EditorGUI.BeginDisabledGroup(importExportEnginesStatesList == null || importExportChargersStatesList == null || importExportEnginesStatesList.Where(state => state).Count() < 1 && importExportChargersStatesList.Where(state => state).Count() < 1);

				if (GUILayout.Button(exportPresets ? EditorUtilities.Icons.Save : EditorUtilities.Icons.Check, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					if (exportPresets)
						FinishExportPresets<EngineChargerPresets>();
					else
						FinishImportPresets();
				}

				EditorGUI.EndDisabledGroup();
			}
			else if (currentEngine < -1 && currentCharger < -1)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					SaveEngine();
					SaveCharger();
				}

				label.text = "Sorting Engines & Chargers";

				GUILayout.Space(5f);
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
			}
			else if (currentEngine < 0 && currentCharger < 0)
			{
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
				GUILayout.FlexibleSpace();
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				if (GUILayout.Button("Import", EditorStyles.miniButtonLeft, GUILayout.Width(75f)))
					ImportEngineChargerPresets();

				if (GUILayout.Button("Export", EditorStyles.miniButtonRight, GUILayout.Width(75f)))
					ExportPresets();

				EditorGUI.BeginDisabledGroup(Settings.Engines.Length < 2 && Settings.Chargers.Length < 2);

				if (GUILayout.Button(EditorUtilities.Icons.Sort, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					SortEnginesAndChargers();

				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
			}
			else if (currentEngine > -1)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					SaveEngine();

				label.text = $"{engineName} Configurations";

				GUILayout.Space(5f);
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
				GUILayout.FlexibleSpace();

				if (GUILayout.Button(EditorGUIUtility.IconContent("Settings"), ToolkitEditorUtility.UnstretchableMiniButtonWide))
					VehicleEngineEditor.OpenEngineWindow(currentEngine);
			}
			else if (currentCharger > -1)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					SaveCharger();

				label.text = $"{chargerName} Configurations";

				GUILayout.Space(5f);
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
				GUILayout.FlexibleSpace();
			}

			GUILayout.Space(20f);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			scrollView = EditorGUILayout.BeginScrollView(scrollView, scrollViewStyle);

			if (currentEngine < 0 && currentCharger < 0)
			{
				EditorGUIUtility.labelWidth -= 10f;

				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(currentEngine < -1 ? "Engines" : $"Engines ({(!importPresetsJson.IsNullOrEmpty() ? importExportChargersList.Length : Settings.Engines.Length)})", EditorStyles.boldLabel);

				if (currentEngine > -2 && !exportPresets && importPresetsJson.IsNullOrEmpty())
				{
					EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

					if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						AddEngine();

					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You are trying to remove all the available engine presets. Are you sure?", "Yes", "No"))
						{
							Undo.RegisterCompleteObjectUndo(Settings, "Remove Engines");
							Settings.ResetEngines();
							EditorUtility.SetDirty(Settings);
						}

					EditorGUI.EndDisabledGroup();
				}

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();

				if (Settings.Engines != null && Settings.Engines.Length > 0)
					for (int i = 0; i < (exportPresets || !importPresetsJson.IsNullOrEmpty() ? importExportEnginesList.Length : Settings.Engines.Length); i++)
					{
						EditorGUILayout.BeginHorizontal(GUI.skin.box);

						if (exportPresets || !importPresetsJson.IsNullOrEmpty())
							importExportEnginesStatesList[i] = EditorGUILayout.ToggleLeft(importExportEnginesList[i].name, importExportEnginesStatesList[i], EditorStyles.miniBoldLabel);
						else
						{
							if (currentEngine < -1)
							{
								EditorGUI.BeginDisabledGroup(i == 0);

								if (GUILayout.Button(EditorUtilities.Icons.CaretUp, ToolkitEditorUtility.UnstretchableMiniButtonLeft))
									MoveEngine(i, i - 1);

								EditorGUI.EndDisabledGroup();
								EditorGUI.BeginDisabledGroup(i >= Settings.Engines.Length - 1);

								if (GUILayout.Button(EditorUtilities.Icons.CaretDown, ToolkitEditorUtility.UnstretchableMiniButtonRight))
									MoveEngine(i, i + 1);

								EditorGUI.EndDisabledGroup();
								GUILayout.Space(5f);
							}

							EditorGUILayout.LabelField(Settings.Engines[i].Name, EditorStyles.miniBoldLabel);

							if (currentEngine > -2)
							{
								if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
									EditEngine(i);

								EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

								if (GUILayout.Button(EditorUtilities.Icons.Clone, ToolkitEditorUtility.UnstretchableMiniButtonWide))
									DuplicateEngine(i);

								if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
									RemoveEngine(i);

								EditorGUI.EndDisabledGroup();
							}
						}

						EditorGUILayout.EndHorizontal();
					}

				EditorGUILayout.EndVertical();
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(currentCharger < -1 ? "Chargers" : $"Chargers ({(!importPresetsJson.IsNullOrEmpty() ? importExportChargersList.Length : Settings.Chargers.Length)})", EditorStyles.boldLabel);

				if (currentCharger > -2 && !exportPresets && importPresetsJson.IsNullOrEmpty())
				{
					EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

					if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						AddCharger();

					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You are trying to remove all the available charger presets. Are you sure?", "Yes", "No"))
						{
							Undo.RegisterCompleteObjectUndo(Settings, "Remove Chargers");
							Settings.ResetChargers();
							EditorUtility.SetDirty(Settings);
						}

					EditorGUI.EndDisabledGroup();
				}

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();

				if (Settings.Chargers != null && Settings.Chargers.Length > 0)
					for (int i = 0; i < (exportPresets || !importPresetsJson.IsNullOrEmpty() ? importExportChargersList.Length : Settings.Chargers.Length); i++)
					{
						EditorGUILayout.BeginHorizontal(GUI.skin.box);

						if (exportPresets || !importPresetsJson.IsNullOrEmpty())
							importExportChargersStatesList[i] = EditorGUILayout.ToggleLeft(importExportChargersList[i].name, importExportChargersStatesList[i], EditorStyles.miniBoldLabel);
						else
						{
							if (currentEngine < -1)
							{
								EditorGUI.BeginDisabledGroup(i == 0);

								if (GUILayout.Button(EditorUtilities.Icons.CaretUp, ToolkitEditorUtility.UnstretchableMiniButtonLeft))
									MoveCharger(i, i - 1);

								EditorGUI.EndDisabledGroup();
								EditorGUI.BeginDisabledGroup(i >= Settings.Chargers.Length - 1);

								if (GUILayout.Button(EditorUtilities.Icons.CaretDown, ToolkitEditorUtility.UnstretchableMiniButtonRight))
									MoveCharger(i, i + 1);

								EditorGUI.EndDisabledGroup();
								GUILayout.Space(5f);
							}

							EditorGUILayout.LabelField(Settings.Chargers[i].Name, EditorStyles.miniBoldLabel);

							if (currentCharger > -2 && !exportPresets && importPresetsJson.IsNullOrEmpty())
							{
								if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
									EditCharger(i);

								EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

								if (GUILayout.Button(EditorUtilities.Icons.Clone, ToolkitEditorUtility.UnstretchableMiniButtonWide))
									DuplicateCharger(i);

								if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
									RemoveCharger(i);

								EditorGUI.EndDisabledGroup();
							}
						}

						EditorGUILayout.EndHorizontal();
					}
				else if (currentCharger > -2 && !exportPresets && importPresetsJson.IsNullOrEmpty())
					EditorGUILayout.HelpBox("Click on the `+` button to add a new charger to this list.", MessageType.Info);
				else
					EditorGUILayout.HelpBox($"To add a new charger you have to exit the {(exportPresets ? "exporting" : !importPresetsJson.IsNullOrEmpty() ? "importing" : "sorting")} mode.", MessageType.Info);

				EditorGUILayout.EndVertical();
				EditorGUILayout.EndHorizontal();

				EditorGUIUtility.labelWidth += 10f;
			}
			else if (currentEngine > -1)
			{
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying || HasOpenInstances<VehicleEngineEditor>());

				engineName = EditorGUILayout.TextField(new GUIContent("Name", "The engine name"), engineName);

				EditorGUI.EndDisabledGroup();
				EditorGUILayout.Space();
				VehicleEngineEditor.EngineEditor(ref Settings.Engines[currentEngine], engineName, this);
			}
			else if (currentCharger > -1)
			{
				VehicleCharger charger = Settings.Chargers[currentCharger];
				bool chargerChanged = false;

				chargerName = EditorGUILayout.TextField(new GUIContent("Name", "The charger name"), chargerName);

				EditorGUILayout.Space();

				bool newIsStock = ToolkitEditorUtility.ToggleButtons(new GUIContent("Is Stock?", "If yes, the charger boost will not be considered and it's just going to adjust the power/torque curves for a more realistic simulation behaviour"), null, "Yes", "No", charger.IsStock, Settings, "Switch Stock");

				if (charger.IsStock != newIsStock)
				{
					charger.IsStock = newIsStock;
					chargerChanged = true;
				}

				if (!charger.IsStock)
				{
					EditorGUI.indentLevel++;

					float newMassDifference = ToolkitEditorUtility.NumberField(new GUIContent("Mass Difference", "The charger mass difference on the engine"), charger.MassDifference, Utility.Units.Weight, 6, Settings, "Change Difference");

					if (charger.MassDifference != newMassDifference)
					{
						charger.MassDifference = newMassDifference;
						chargerChanged = true;
					}

					float newWeightDistributionDifference = ToolkitEditorUtility.Slider(new GUIContent("Weight Distribution Diff.", "The charger adjustment (difference) on the vehicle weight distribution"), charger.WeightDistributionDifference, -1f, 1f, Settings, "Change Difference");

					if (charger.WeightDistributionDifference != newWeightDistributionDifference)
					{
						charger.WeightDistributionDifference = newWeightDistributionDifference;
						chargerChanged = true;
					}

					EditorGUI.indentLevel--;

					EditorGUILayout.Space();
				}

				VehicleCharger.ChargerType newType = (VehicleCharger.ChargerType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The charger type, either a turbocharger or a supercharger"), charger.Type);

				if (charger.Type != newType)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Charger Type");

					charger.Type = newType;
					chargerChanged = true;
				}

				switch (charger.Type)
				{
					case VehicleCharger.ChargerType.Turbocharger:
						VehicleCharger.TurbochargerCount newTurboCount = (VehicleCharger.TurbochargerCount)EditorGUILayout.EnumPopup(new GUIContent("Turbos Count", "The number of turbochargers this kit has"), charger.TurboCount);

						if (charger.TurboCount != newTurboCount)
						{
							Undo.RegisterCompleteObjectUndo(Settings, "Change Turbo Count");

							charger.TurboCount = newTurboCount;
							chargerChanged = true;
						}

						float newTurboInertiaRPM = ToolkitEditorUtility.NumberField(new GUIContent("Inertia RPM", "The RPM at which the turbochargers starts boosting"), charger.InertiaRPM, "rpm", "Revolutions per Minute", true, Settings, "Change RPM");

						if (charger.InertiaRPM != newTurboInertiaRPM)
						{
							charger.InertiaRPM = newTurboInertiaRPM;
							chargerChanged = true;
						}

						float newTurboSize = ToolkitEditorUtility.Slider(new GUIContent("Turbo Size", "The size of a single turbocharger"), charger.ChargerSize, 0f, 5f, Settings, "Change Size");

						if (charger.ChargerSize != newTurboSize)
						{
							charger.ChargerSize = newTurboSize;
							chargerChanged = true;
						}

						break;

					case VehicleCharger.ChargerType.Supercharger:
						VehicleCharger.Supercharger newSuperchargerType = (VehicleCharger.Supercharger)EditorGUILayout.EnumPopup(new GUIContent("Supercharger Type", "The type of the supercharger"), charger.SuperchargerType);

						if (charger.SuperchargerType != newSuperchargerType)
						{
							Undo.RegisterCompleteObjectUndo(Settings, "Change Supercharger Type");

							charger.SuperchargerType = newSuperchargerType;
							chargerChanged = true;
						}

						if (charger.SuperchargerType == VehicleCharger.Supercharger.Centrifugal)
						{
							float newInertiaRPM = ToolkitEditorUtility.NumberField(new GUIContent("Inertia RPM", "The RPM at which the supercharger starts boosting"), charger.InertiaRPM, "rpm", "Revolutions per Minute", true, Settings, "Change RPM");

							if (charger.InertiaRPM != newInertiaRPM)
							{
								charger.InertiaRPM = newInertiaRPM;
								chargerChanged = true;
							}

							float newSuperchargerSize = ToolkitEditorUtility.Slider(new GUIContent("Size", "The size of the supercharger"), charger.ChargerSize, 0f, 5f, Settings, "Change Size");

							if (charger.ChargerSize != newSuperchargerSize)
							{
								charger.ChargerSize = newSuperchargerSize;
								chargerChanged = true;
							}
						}

						break;
				}

				if (charger.Type == VehicleCharger.ChargerType.Turbocharger || charger.Type == VehicleCharger.ChargerType.Supercharger && charger.SuperchargerType == VehicleCharger.Supercharger.Centrifugal)
				{
					float newMinimumBoost = ToolkitEditorUtility.Slider(new GUIContent("Minimum Boost", "The charger boost multiplier when the engine is in idle mode"), charger.MinimumBoost, 0f, 2f, Settings, "Change Boost");

					if (charger.MinimumBoost != newMinimumBoost)
					{
						charger.MinimumBoost = newMinimumBoost;
						chargerChanged = true;
					}
				}

				float newMaximumBoost = ToolkitEditorUtility.Slider(new GUIContent("Maximum Boost", "The charger boost multiplier at its full potential"), charger.MaximumBoost, 1f, 3f, Settings, "Change Boost");

				if (charger.MaximumBoost != newMaximumBoost)
				{
					charger.MaximumBoost = newMaximumBoost;
					chargerChanged = true;
				}

				EditorGUILayout.Space();

				VehicleCharger.AudioModule chargerAudio = charger.Audio;

				EditorGUILayout.LabelField("Sound Effects", EditorStyles.boldLabel);

				AudioClip newIdleClip = EditorGUILayout.ObjectField(new GUIContent("Idle Clip", "The charger idle sound clip"), chargerAudio.idleClip, typeof(AudioClip), false) as AudioClip;

				if (chargerAudio.idleClip != newIdleClip)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Audio Clip");

					chargerAudio.idleClip = newIdleClip;
					chargerChanged = true;
				}

				AudioClip newActiveClip = EditorGUILayout.ObjectField(new GUIContent("Active Clip", "The charger boosting sound clip"), chargerAudio.activeClip, typeof(AudioClip), false) as AudioClip;

				if (chargerAudio.activeClip != newActiveClip)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Audio Clip");

					chargerAudio.activeClip = newActiveClip;
					chargerChanged = true;
				}

				AudioClip newFuelBlowoutClip = EditorGUILayout.ObjectField(new GUIContent("Fuel Blowout Clip", "The charger releasing fuel pedal blowout sound clip"), chargerAudio.fuelBlowoutClip, typeof(AudioClip), false) as AudioClip;

				if (chargerAudio.fuelBlowoutClip != newFuelBlowoutClip)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Audio Clip");

					chargerAudio.fuelBlowoutClip = newFuelBlowoutClip;
					chargerChanged = true;
				}

				AudioClip newGearBlowoutClip = EditorGUILayout.ObjectField(new GUIContent("Gear Blowout Clip", "The charger changing gear blowout sound clip"), chargerAudio.gearBlowoutClip, typeof(AudioClip), false) as AudioClip;

				if (chargerAudio.gearBlowoutClip != newGearBlowoutClip)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Audio Clip");

					chargerAudio.gearBlowoutClip = newGearBlowoutClip;
					chargerChanged = true;
				}

				AudioClip newRevBlowoutClip = EditorGUILayout.ObjectField(new GUIContent("Rev Blowout Clip", "The charger Over-Rev blowout sound clip"), chargerAudio.revBlowoutClip, typeof(AudioClip), false) as AudioClip;

				if (chargerAudio.revBlowoutClip != newRevBlowoutClip)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Audio Clip");

					chargerAudio.revBlowoutClip = newRevBlowoutClip;
					chargerChanged = true;
				}

				EditorGUILayout.Space();
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField($"Compatible Engines ({charger.CompatibleEngineIndexes.Length})", EditorStyles.boldLabel);
				EditorGUILayout.Space();

				for (int i = 0; i < charger.CompatibleEngineIndexes.Length; i++)
				{
					if (charger.CompatibleEngineIndexes[i] < 0 || charger.CompatibleEngineIndexes[i] >= Settings.Engines.Length)
					{
						RemoveChargerEngine(charger, i);

						chargerChanged = true;

						continue;
					}

					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField($"{charger.CompatibleEngines[i].Name}", EditorStyles.miniBoldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						RemoveChargerEngine(charger, i);

						chargerChanged = true;
					}

					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();

				List<string> enginesPopupList = new()
				{
					"None"
				};

				enginesPopupList.AddRange(Settings.GetEnginesNames(false));

				int newEngine = EditorGUILayout.Popup(0, enginesPopupList.ToArray()) - 1;

				if (newEngine > -1)
				{
					if (charger.HasCompatibleEngine(newEngine))
						EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The selected engine does already exist in the compatibility list, there's no need to add it twice.", "Okay");
					else if (Settings.Engines[newEngine].IsElectric)
						EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You can't add an electric engine, as they cannot use Chargers!", "Okay");
					else
					{
						AddChargerEngine(charger, newEngine);

						chargerChanged = true;
					}
				}

				EditorGUI.BeginDisabledGroup(true);
				GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();

				if (!charger.IsValid)
				{
					if (charger.Type == VehicleCharger.ChargerType.Turbocharger || charger.Type == VehicleCharger.ChargerType.Supercharger && charger.SuperchargerType == VehicleCharger.Supercharger.Centrifugal)
					{
						if (charger.InertiaRPM < charger.MinimumEnginesRPM)
							EditorGUILayout.HelpBox($"The `Inertia RPM` cannot go below {charger.MinimumEnginesRPM} rpm as some engines have a `Minimum RPM` of that value.", MessageType.Error);
						else if (charger.InertiaRPM >= charger.RedlineEnginesRPM)
							EditorGUILayout.HelpBox($"The `Inertia RPM` cannot exceed {charger.RedlineEnginesRPM} rpm as some engines have a `Redline RPM` of that value.", MessageType.Error);
						else if (charger.ChargerSize <= 0f)
							EditorGUILayout.HelpBox($"The `Charger Size` cannot be set to null (equal to 0).", MessageType.Error);
					}
				}
				else if (!charger.IsCompatible)
				{
					if (charger.CompatibleEngineIndexes.Length < 1 && charger.IsStock)
						EditorGUILayout.HelpBox($"The {charger.Type} is set as \"Stock\" but doesn't have any compatible engines! Please consider changing the current `Is Stock?` state to `No` or add a compatible engine to the list above.", MessageType.Warning);
					else if (charger.CompatibleEngineIndexes.Length > 0 && Array.Exists(charger.CompatibleEngines, engine => engine.IsElectric))
						EditorGUILayout.HelpBox($"The `Compatible Engines` list cannot contain any electric engines.", MessageType.Warning);
				}
				else if (charger.HasIssues)
				{
					if (charger.Type == VehicleCharger.ChargerType.Turbocharger || charger.Type == VehicleCharger.ChargerType.Supercharger && charger.SuperchargerType == VehicleCharger.Supercharger.Centrifugal)
					{
						float recommendedInertiaRPM = Utility.Average(charger.MinimumEnginesRPM, charger.OverRevEnginesRPM);

						if (charger.CompatibleEngineIndexes.Length > 0 && charger.InertiaRPM > recommendedInertiaRPM)
							EditorGUILayout.HelpBox($"The `Inertia RPM` is kind of high, consider adding a value lower or equal to {recommendedInertiaRPM} rpm.", MessageType.Info);
					}
				}

				if (chargerChanged)
				{
					charger.Audio = chargerAudio;

					Repaint();
					EditorUtility.SetDirty(Settings);
				}
			}

			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}
		private void TireCompoundsSettingsEditor()
		{
			GUIStyle largeLabel = new(EditorStyles.boldLabel)
			{
				fontSize = 17,
				fixedHeight = 20f
			};
			GUIContent label = new("Tire Compounds");

			EditorGUILayout.BeginHorizontal();

#if MVC_COMMUNITY
			int currentTireCompound = 0;
#else
			if (exportPresets || !importPresetsJson.IsNullOrEmpty())
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					ExitImportExportPresets();

				label.text = $"{(exportPresets ? "Exporting" : "Importing")} Tire Compounds";

				GUILayout.Space(5f);
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Select All", EditorStyles.miniButtonLeft, GUILayout.Width(85f)))
					importExportTireCompoundsStatesList = Enumerable.Repeat(true, importExportTireCompoundsList.Length).ToArray();

				if (GUILayout.Button("Deselect All", EditorStyles.miniButtonRight, GUILayout.Width(85f)))
					importExportTireCompoundsStatesList = Enumerable.Repeat(false, importExportTireCompoundsList.Length).ToArray();

				EditorGUI.BeginDisabledGroup(importExportTireCompoundsStatesList == null || importExportTireCompoundsStatesList.Where(state => state).Count() < 1);

				if (GUILayout.Button(exportPresets ? EditorUtilities.Icons.Save : EditorUtilities.Icons.Check, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					if (exportPresets)
						FinishExportPresets<TireCompoundPresets>();
					else
						FinishImportPresets();
				}

				EditorGUI.EndDisabledGroup();
			}
			else if (currentTireCompound < -1)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					SaveTireCompound();

				label.text = "Sorting Tire Compounds";

				GUILayout.Space(5f);
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
			}
			else if (currentTireCompound < 0)
			{
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
				GUILayout.FlexibleSpace();
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				if (GUILayout.Button("Import", EditorStyles.miniButtonLeft, GUILayout.Width(75f)))
					ImportTireCompoundPresets();

				if (GUILayout.Button("Export", EditorStyles.miniButtonRight, GUILayout.Width(75f)))
					ExportPresets();

				EditorGUI.BeginDisabledGroup(Settings.TireCompounds.Length < 2);

				if (GUILayout.Button(EditorUtilities.Icons.Sort, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					SortTireCompounds();

				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					AddTireCompound();

				EditorGUI.BeginDisabledGroup(Settings.TireCompounds.Length < 2);

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You are trying to remove all the available tire compound presets. Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Tire Compounds");
						Settings.ResetTireCompounds();
						EditorUtility.SetDirty(Settings);
					}

				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
			}
			else
			{
#endif
			currentTireCompound = math.clamp(currentTireCompound, 0, Settings.TireCompounds.Length - 1);

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
#if MVC_COMMUNITY
				EnableFoldout(ToolkitSettings.SettingsEditorFoldout.None, true);
#else
					SaveTireCompound();
#endif

			GUILayout.Space(5f);

			label.text = $"{Settings.TireCompounds[math.clamp(currentTireCompound, 0, Settings.TireCompounds.Length - 1)].Name} Configurations";

			EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
			GUILayout.FlexibleSpace();
#if !MVC_COMMUNITY
			}
#endif

			GUILayout.Space(20f);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			scrollView = EditorGUILayout.BeginScrollView(scrollView, scrollViewStyle);

#if !MVC_COMMUNITY
			if (currentTireCompound < 0)
				for (int i = 0; i < (exportPresets || !importPresetsJson.IsNullOrEmpty() ? importExportTireCompoundsList.Length : Settings.TireCompounds.Length); i++)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);

					if (exportPresets || !importPresetsJson.IsNullOrEmpty())
						importExportTireCompoundsStatesList[i] = EditorGUILayout.ToggleLeft(importExportTireCompoundsList[i].name, importExportTireCompoundsStatesList[i], EditorStyles.miniBoldLabel);
					else
					{
						if (currentTireCompound < -1)
						{
							EditorGUI.BeginDisabledGroup(i == 0);

							if (GUILayout.Button(EditorUtilities.Icons.CaretUp, ToolkitEditorUtility.UnstretchableMiniButtonLeft))
								MoveTireCompound(i, i - 1);

							EditorGUI.EndDisabledGroup();
							EditorGUI.BeginDisabledGroup(i >= Settings.TireCompounds.Length - 1);

							if (GUILayout.Button(EditorUtilities.Icons.CaretDown, ToolkitEditorUtility.UnstretchableMiniButtonRight))
								MoveTireCompound(i, i + 1);

							EditorGUI.EndDisabledGroup();
							GUILayout.Space(5f);
						}

						EditorGUILayout.LabelField(Settings.TireCompounds[i].Name, EditorStyles.miniBoldLabel);

						if (currentTireCompound > -2)
						{
							if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
								EditTireCompound(i);

							EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

							if (GUILayout.Button(EditorUtilities.Icons.Clone, ToolkitEditorUtility.UnstretchableMiniButtonWide))
								DuplicateTireCompound(i);

							if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
								RemoveTireCompound(i);

							EditorGUI.EndDisabledGroup();
						}
					}

					EditorGUILayout.EndHorizontal();
				}
			else
			{
#endif
			VehicleTireCompound tireCompound = Settings.TireCompounds[currentTireCompound];

#if !MVC_COMMUNITY
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				tireCompoundName = EditorGUILayout.TextField(new GUIContent("Name", "The tire compound name"), tireCompoundName);

				EditorGUI.EndDisabledGroup();
#endif
			VehicleTireCompound.FrictionComplexity newFrictionComplexity = ToolkitEditorUtility.EnumField(new GUIContent("Complexity", "Tire friction curve complexity"), tireCompound.frictionComplexity, Settings, "Change Complexity");

			if (tireCompound.frictionComplexity != newFrictionComplexity)
				tireCompound.frictionComplexity = newFrictionComplexity;

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();

			static void FrictionEditor(string name, Rect position, bool complex, ref VehicleTireCompound.WheelColliderFrictionCurve frictionCurve)
			{
				float orgLabelWidth = EditorGUIUtility.labelWidth;

				EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth * (complex ? 1f / 3f : .5f) - (complex ? 71.5f : 76f);

				EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.labelWidth));
				EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
				EditorGUILayout.Space();

				EditorGUIUtility.labelWidth = math.min(EditorGUIUtility.labelWidth, 150f);

				EditorGUI.indentLevel++;

				float newExtremumSlip = ToolkitEditorUtility.NumberField("Extremum Slip", frictionCurve.extremumSlip, false, Settings, "Change Slip");

				if (frictionCurve.extremumSlip != newExtremumSlip)
					frictionCurve.extremumSlip = newExtremumSlip;

				float newExtremumValue = ToolkitEditorUtility.NumberField("Extremum Value", frictionCurve.extremumValue, false, Settings, "Change Extremum");

				if (frictionCurve.extremumValue != newExtremumValue)
					frictionCurve.extremumValue = newExtremumValue;

				EditorGUILayout.Space();

				float newAsymptoteSlip = ToolkitEditorUtility.NumberField("Asymptote Slip", frictionCurve.asymptoteSlip, false, Settings, "Change Slip");

				if (frictionCurve.asymptoteSlip != newAsymptoteSlip)
					frictionCurve.asymptoteSlip = newAsymptoteSlip;

				float newAsymptoteValue = ToolkitEditorUtility.NumberField("Asymptote Value", frictionCurve.asymptoteValue, false, Settings, "Change Asymptote");

				if (frictionCurve.asymptoteValue != newAsymptoteValue)
					frictionCurve.asymptoteValue = newAsymptoteValue;

				EditorGUILayout.Space();

				VehicleTireCompound.FrictionComplexity newStiffnessComplexity = ToolkitEditorUtility.EnumField(new GUIContent("Stiffness", "Friction curve stiffness complexity"), frictionCurve.stiffnessComplexity, Settings, "Change Complexity");

				if (frictionCurve.stiffnessComplexity != newStiffnessComplexity)
					frictionCurve.stiffnessComplexity = newStiffnessComplexity;

				switch (frictionCurve.stiffnessComplexity)
				{
					case VehicleTireCompound.FrictionComplexity.Complex:
						EditorGUIUtility.labelWidth = orgLabelWidth;

						Utility.SimpleInterval newStiffnessInterval = ToolkitEditorUtility.IntervalField(new GUIContent("Interval", "Friction curve peak multiplier interval"), null, "A", "B", "", "", false, frictionCurve.stiffnessInterval.ToInterval(true, true), Settings, "Change Stiffness").ToSimpleInterval();

						if (frictionCurve.stiffnessInterval != newStiffnessInterval)
							frictionCurve.stiffnessInterval = newStiffnessInterval;

						Utility.SimpleInterval newStiffnessSpeedInterval = ToolkitEditorUtility.IntervalField(new GUIContent("Speed", "Friction curve peak speed thresholds interval"), null, "Min", "Max", Utility.Units.Speed, false, frictionCurve.stiffnessSpeedInterval.ToInterval(false, true), Settings, "Change Stiffness").ToSimpleInterval();

						if (frictionCurve.stiffnessSpeedInterval != newStiffnessSpeedInterval)
							frictionCurve.stiffnessSpeedInterval = newStiffnessSpeedInterval;

						break;

					default:
						float newStiffness = ToolkitEditorUtility.NumberField(new GUIContent("Value", "Friction curve peak multiplier"), frictionCurve.stiffness, false, Settings, "Change Stiffness");

						if (frictionCurve.stiffness != newStiffness)
							frictionCurve.stiffness = newStiffness;

						EditorGUIUtility.labelWidth = orgLabelWidth;

						break;
				}

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
				EditorGUILayout.EndVertical();
			}

			bool complex = tireCompound.frictionComplexity == VehicleTireCompound.FrictionComplexity.Complex;

			FrictionEditor($"{(complex ? "Acceleration" : "Forward")} Friction", position, complex, ref tireCompound.wheelColliderAccelerationFriction);

			EditorGUI.indentLevel++;

			if (complex)
				FrictionEditor("Brake Friction", position, complex, ref tireCompound.wheelColliderBrakeFriction);

			FrictionEditor("Sideways Friction", position, complex, ref tireCompound.wheelColliderSidewaysFriction);

			EditorGUI.indentLevel--;

			EditorGUILayout.EndHorizontal();

			var sortedWidthFrictionModifiers = tireCompound.widthFrictionModifiers.ToArray();

			Array.Sort(sortedWidthFrictionModifiers.Select(modifier => modifier.width).ToArray(), sortedWidthFrictionModifiers);
			serializedObject.Update();

			SerializedProperty tireCompoundProperty = serializedObject.FindProperty("tireCompounds").GetArrayElementAtIndex(currentTireCompound);
			SerializedProperty widthFrictionModifiersProperty = tireCompoundProperty.FindPropertyRelative(nameof(tireCompound.widthFrictionModifiers));

			reorderableList ??= new(serializedObject, widthFrictionModifiersProperty, true, false, true, true);
			reorderableList.displayRemove = tireCompound.widthFrictionModifiers.Length > 2;

			ToolkitEditorUtility.ReorderableList("Width Friction Modifiers", widthFrictionModifiersProperty, ref reorderableList);
			serializedObject.ApplyModifiedProperties();

			if (tireCompound.widthFrictionModifiers.Length < 2)
				EditorGUILayout.HelpBox("There must be at least two Width Friction Modifiers, otherwise the physics system won't be able to calculate the friction multiplier interpolation.", MessageType.Error);
			else
			{
				bool isSortValid = true;

				for (int i = 0; i < tireCompound.widthFrictionModifiers.Length && isSortValid; i++)
					isSortValid &= tireCompound.widthFrictionModifiers[i].width == sortedWidthFrictionModifiers[i].width;

				if (!isSortValid)
					EditorGUILayout.HelpBox("The Width Friction Modifiers array needs to be sorted by width values!", MessageType.Warning);
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Wetness Multipliers", EditorStyles.boldLabel);

			float newWetFrictionMultiplier = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Wet Friction Multiplier", "Friction multiplier used when the tire is in contact with a wet surface."), tireCompound.wetFrictionMultiplier, false, Settings, "Change Multiplier"), 0f);

			if (tireCompound.wetFrictionMultiplier != newWetFrictionMultiplier)
				tireCompound.wetFrictionMultiplier = newWetFrictionMultiplier;

			float newWetSlipMultiplier = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Wet Slip Multiplier", "Slip multiplier used when the tire is in contact with a wet surface."), tireCompound.wetSlipMultiplier, false, Settings, "Change Multiplier"), 0f);

			if (tireCompound.wetSlipMultiplier != newWetSlipMultiplier)
				tireCompound.wetSlipMultiplier = newWetSlipMultiplier;

			if (ToolkitSettings.UsingWheelColliderPhysics)
				EditorGUILayout.HelpBox("All slip multipliers are ignored when the physics system is set to use Unity's standard Wheel Colliders.", MessageType.Info);

			EditorGUILayout.Space();
#if !MVC_COMMUNITY
			}
#endif

			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}
		private void GroundsSettingsEditor()
		{
			GUIStyle largeLabel = new(EditorStyles.boldLabel)
			{
				fontSize = 17,
				fixedHeight = 20f
			};
			GUIContent label = new("Grounds & Surfaces");

			EditorGUILayout.BeginHorizontal();

#if MVC_COMMUNITY
			int currentGround = 0;
#else
			if (exportPresets || !importPresetsJson.IsNullOrEmpty())
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					ExitImportExportPresets();

				label.text = $"{(exportPresets ? "Exporting" : "Importing")} Grounds & Surfaces";

				GUILayout.Space(5f);
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Select All", EditorStyles.miniButtonLeft, GUILayout.Width(85f)))
					importExportGroundsStatesList = Enumerable.Repeat(true, importExportGroundsList.Length).ToArray();

				if (GUILayout.Button("Deselect All", EditorStyles.miniButtonRight, GUILayout.Width(85f)))
					importExportGroundsStatesList = Enumerable.Repeat(false, importExportGroundsList.Length).ToArray();

				EditorGUI.BeginDisabledGroup(importExportGroundsStatesList == null || importExportGroundsStatesList.Where(state => state).Count() < 1);

				if (GUILayout.Button(exportPresets ? EditorUtilities.Icons.Save : EditorUtilities.Icons.Check, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					if (exportPresets)
						FinishExportPresets<GroundPresets>();
					else
						FinishImportPresets();
				}

				EditorGUI.EndDisabledGroup();
			}
			else if (currentGround < -1)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					SaveGround();

				label.text = "Sorting Grounds";

				GUILayout.Space(5f);
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
			}
			else if (currentGround < 0)
			{
				EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
				GUILayout.FlexibleSpace();
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				if (GUILayout.Button("Import", EditorStyles.miniButtonLeft, GUILayout.Width(75f)))
					ImportGroundPresets();

				if (GUILayout.Button("Export", EditorStyles.miniButtonRight, GUILayout.Width(75f)))
					ExportPresets();

				EditorGUI.BeginDisabledGroup(Settings.Grounds.Length < 2);

				if (GUILayout.Button(EditorUtilities.Icons.Sort, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					SortGrounds();

				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					AddGround();

				EditorGUI.BeginDisabledGroup(Settings.Grounds.Length < 2);

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You are trying to remove all the available ground presets. Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Grounds");
						Settings.ResetGrounds();
						EditorUtility.SetDirty(Settings);
					}

				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				currentGround = math.clamp(currentGround, 0, Settings.Grounds.Length - 1);

				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					SaveGround();

				GUILayout.Space(5f);
#endif

			label.text = $"{Settings.Grounds[math.clamp(currentGround, 0, Settings.Grounds.Length - 1)].Name} Configurations";

			EditorGUILayout.LabelField(label, largeLabel, GUILayout.Width(largeLabel.CalcSize(label).x));
			GUILayout.FlexibleSpace();
#if !MVC_COMMUNITY
			}
#endif

			GUILayout.Space(20f);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			scrollView = EditorGUILayout.BeginScrollView(scrollView, scrollViewStyle);

#if !MVC_COMMUNITY
			if (currentGround < 0)
				for (int i = 0; i < (exportPresets || !importPresetsJson.IsNullOrEmpty() ? importExportGroundsList.Length : Settings.Grounds.Length); i++)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);

					if (exportPresets || !importPresetsJson.IsNullOrEmpty())
						importExportGroundsStatesList[i] = EditorGUILayout.ToggleLeft(importExportGroundsList[i].name, importExportGroundsStatesList[i], EditorStyles.miniBoldLabel);
					else
					{
						if (currentGround < -1)
						{
							EditorGUI.BeginDisabledGroup(i == 0);

							if (GUILayout.Button(EditorUtilities.Icons.CaretUp, ToolkitEditorUtility.UnstretchableMiniButtonLeft))
								MoveGround(i, i - 1);

							EditorGUI.EndDisabledGroup();
							EditorGUI.BeginDisabledGroup(i >= Settings.Grounds.Length - 1);

							if (GUILayout.Button(EditorUtilities.Icons.CaretDown, ToolkitEditorUtility.UnstretchableMiniButtonRight))
								MoveGround(i, i + 1);

							EditorGUI.EndDisabledGroup();
							GUILayout.Space(5f);
						}

						EditorGUILayout.LabelField(Settings.Grounds[i].Name, EditorStyles.miniBoldLabel);

						if (currentGround > -2)
						{
							if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
								EditGround(i);

							EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

							if (GUILayout.Button(EditorUtilities.Icons.Clone, ToolkitEditorUtility.UnstretchableMiniButtonWide))
								DuplicateGround(i);

							if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
								RemoveGround(i);

							EditorGUI.EndDisabledGroup();
						}
					}

					EditorGUILayout.EndHorizontal();
				}
			else
			{
#endif
			VehicleGroundMapper.GroundModule ground = Settings.Grounds[currentGround];
			bool groundChanged = false;

#if !MVC_COMMUNITY
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
				
				groundName = EditorGUILayout.TextField(new GUIContent("Name", "The ground surface name"), groundName);

				EditorGUI.EndDisabledGroup();
#endif

			float newDefaultStiffness = ToolkitEditorUtility.NumberField(new GUIContent("Default Stiffness", "The ground default friction stiffness"), ground.FrictionStiffness, false, Settings, "Change Stiffness");

			if (ground.FrictionStiffness != newDefaultStiffness)
			{
				ground.FrictionStiffness = newDefaultStiffness;
				groundChanged = true;
			}

			if (Settings.useWheelHealth)
			{
				EditorGUILayout.BeginHorizontal();

				float newDamageStiffness = ToolkitEditorUtility.NumberField(new GUIContent("Damage Stiffness", "The ground friction stiffness used while the wheel tire is totally damaged"), ground.DamagedWheelStiffness, false, Settings, "Change Stiffness");

				if (ground.DamagedWheelStiffness != newDamageStiffness)
				{
					ground.DamagedWheelStiffness = newDamageStiffness;
					groundChanged = true;
				}

				EditorGUILayout.EndHorizontal();
			}

			float newWheelDampingRate = ToolkitEditorUtility.Slider(new GUIContent("Wheel Damping", "The lower this value is higher the more the wheel loses traction"), ground.WheelDampingRate, 0f, 1f, Settings, "Change Damping");

			if (ground.WheelDampingRate != newWheelDampingRate)
			{
				ground.WheelDampingRate = newWheelDampingRate;
				groundChanged = true;
			}

			float newWheelBurnoutDampingRate = ToolkitEditorUtility.Slider(new GUIContent("Wheel Burnout Damping", "Wheel Damping Rate at full burnout intensity"), ground.WheelBurnoutDampingRate, 0f, 1f, Settings, "Change Damping");

			if (ground.WheelBurnoutDampingRate != newWheelBurnoutDampingRate)
			{
				ground.WheelBurnoutDampingRate = newWheelBurnoutDampingRate;
				groundChanged = true;
			}

			bool newIsOffRoad = ToolkitEditorUtility.ToggleButtons(new GUIContent("Off-Road", "Should type of surfaces be considered as an Off-Road ground?"), null, "Yes", "No", ground.isOffRoad, Settings, "Switch Off-Road");

			if (ground.isOffRoad != newIsOffRoad)
			{
				ground.isOffRoad = newIsOffRoad;
				groundChanged = true;
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Visual Effects", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (Settings.useParticleSystems)
			{
				ParticleSystem newParticleEffect = EditorGUILayout.ObjectField(new GUIContent("Particle Effect", "The ground particle system effect used while sliding or at higher speeds"), ground.particleEffect, typeof(ParticleSystem), false) as ParticleSystem;

				if (ground.particleEffect != newParticleEffect)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Particle Effect");

					ground.particleEffect = newParticleEffect;
					groundChanged = true;
				}

				if (Settings.useDamage)
				{
					EditorGUILayout.BeginHorizontal();

					ParticleSystem newFlatParticleEffect = EditorGUILayout.ObjectField(new GUIContent("Flat Wheel Particle Effect", "The ground particle system effect used for wheels with totally damaged tires, while sliding or at higher speeds"), ground.flatWheelParticleEffect, typeof(ParticleSystem), false) as ParticleSystem;

					if (ground.flatWheelParticleEffect != newFlatParticleEffect)
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Particle Effect");

						ground.flatWheelParticleEffect = newFlatParticleEffect;
						groundChanged = true;
					}

					EditorGUILayout.EndHorizontal();
				}
			}

			if (Settings.useWheelMarks)
			{
				Material newMarkMaterial = EditorGUILayout.ObjectField(new GUIContent("Wheel Mark Material", "The ground skid mark trail material"), ground.markMaterial, typeof(Material), false) as Material;

				if (ground.markMaterial != newMarkMaterial)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Mark Material");

					ground.markMaterial = newMarkMaterial;
					groundChanged = true;
				}

				if (ground.markMaterial)
				{
					EditorGUI.indentLevel++;

					float newMarkMaterialTiling = ToolkitEditorUtility.NumberField(new GUIContent("Tiling", "The material UV tiling multiplier"), ground.MarkMaterialTiling, false, Settings, "Change Tiling");

					if (ground.MarkMaterialTiling != newMarkMaterialTiling)
					{
						ground.MarkMaterialTiling = newMarkMaterialTiling;
						groundChanged = true;
					}

					EditorGUI.indentLevel--;
				}

				if (Settings.useDamage)
				{
					EditorGUILayout.BeginHorizontal();

					Material newFlatMarkMaterial = EditorGUILayout.ObjectField(new GUIContent("Flat Wheel Mark Material", "The flat wheel (without tires) ground mark trail material"), ground.flatWheelMarkMaterial, typeof(Material), false) as Material;

					if (ground.flatWheelMarkMaterial != newFlatMarkMaterial)
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Mark Material");

						ground.flatWheelMarkMaterial = newFlatMarkMaterial;
						groundChanged = true;
					}

					EditorGUILayout.EndHorizontal();

					if (ground.flatWheelMarkMaterial)
					{
						EditorGUI.indentLevel++;

						float newFlatMarkMaterialTiling = ToolkitEditorUtility.NumberField(new GUIContent("Tiling", "The material UV tiling multiplier"), ground.FlatWheelMarkMaterialTiling, false, Settings, "Change Tiling");

						if (ground.FlatWheelMarkMaterialTiling != newFlatMarkMaterialTiling)
						{
							ground.FlatWheelMarkMaterialTiling = newFlatMarkMaterialTiling;
							groundChanged = true;
						}

						EditorGUI.indentLevel--;
					}
				}
			}

			if (Settings.useParticleSystems || Settings.useWheelMarks)
			{
				bool newUseSpeedEmission = ToolkitEditorUtility.ToggleButtons(new GUIContent("Speed Emissive", "Would wheel lower speed rotations be enough to emit both of particle and mark effects?"), null, "Yes", "No", ground.useSpeedEmission, Settings, "Switch Speed Emission");

				if (ground.useSpeedEmission != newUseSpeedEmission)
				{
					ground.useSpeedEmission = newUseSpeedEmission;
					groundChanged = true;
				}
			}
			else
				EditorGUILayout.HelpBox("The Visual Effects settings will appear here once you enable the Particle Systems or the Wheel Marks toggles from the VFX Panel.", MessageType.Info);

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Sound Effects", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			AudioClip newForwardSkidSound = EditorGUILayout.ObjectField(new GUIContent("Forward Skid", "The ground skid sound on the Z axis of the wheel"), ground.forwardSkidClip, typeof(AudioClip), false) as AudioClip;

			if (ground.forwardSkidClip != newForwardSkidSound)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Clip");

				ground.forwardSkidClip = newForwardSkidSound;
				groundChanged = true;
			}

			AudioClip newBrakeSkidSound = EditorGUILayout.ObjectField(new GUIContent("Braking Skid", "The ground skid sound on the Z axis of the wheel while braking"), ground.brakeSkidClip, typeof(AudioClip), false) as AudioClip;

			if (ground.brakeSkidClip != newBrakeSkidSound)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Clip");

				ground.brakeSkidClip = newBrakeSkidSound;
				groundChanged = true;
			}

			AudioClip newLowPitchSkidSound = EditorGUILayout.ObjectField(new GUIContent("Sideways Skid", "The ground skid sound on the X axis of the wheel"), ground.sidewaysSkidClip, typeof(AudioClip), false) as AudioClip;

			if (ground.sidewaysSkidClip != newLowPitchSkidSound)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Clip");

				ground.sidewaysSkidClip = newLowPitchSkidSound;
				groundChanged = true;
			}

			AudioClip newRollSound = EditorGUILayout.ObjectField(new GUIContent("Rolling Sound", "The wheel rolling sound"), ground.rollClip, typeof(AudioClip), false) as AudioClip;

			if (ground.rollClip != newRollSound)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Clip");

				ground.rollClip = newRollSound;
				groundChanged = true;
			}

			if (Settings.useDamage)
			{
				EditorGUILayout.BeginHorizontal();

				AudioClip newFlatSkidClip = EditorGUILayout.ObjectField(new GUIContent("Flat Skid Sound", "The ground skid sound of the vehicle's wheel without tires"), ground.flatSkidClip, typeof(AudioClip), false) as AudioClip;

				if (ground.flatSkidClip != newFlatSkidClip)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Clip");

					ground.flatSkidClip = newFlatSkidClip;
					groundChanged = true;
				}

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();

				AudioClip newFlatRollClip = EditorGUILayout.ObjectField(new GUIContent("Flat Rolling Sound", "The wheel rolling sound without tires"), ground.flatRollClip, typeof(AudioClip), false) as AudioClip;

				if (ground.flatRollClip != newFlatRollClip)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Clip");

					ground.flatRollClip = newFlatRollClip;
					groundChanged = true;
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUI.EndDisabledGroup();

			float newVolume = ToolkitEditorUtility.Slider("Volume", ground.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (ground.Volume != newVolume)
			{
				ground.Volume = newVolume;
				groundChanged = true;
			}

			EditorGUILayout.Space();

			if (!EditorApplication.isPlaying)
				EditorGUILayout.HelpBox("Features with empty fields are going to be ignored at runtime since they are not required.", MessageType.Info);

			if (groundChanged)
			{
				Settings.Grounds[currentGround] = ground;

				EditorUtility.SetDirty(Settings);
			}
#if !MVC_COMMUNITY
			}
#endif

			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}
		private void SFXSettingsEditor()
		{
			GUIStyle largeLabel = new(EditorStyles.boldLabel)
			{
				fontSize = 17,
				fixedHeight = 20f
			};

			EditorGUILayout.BeginHorizontal();

			if (Settings.soundEffectsFoldout != ToolkitSettings.SettingsEditorSFXFoldout.None)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					Settings.soundEffectsFoldout = ToolkitSettings.SettingsEditorSFXFoldout.None;

				GUILayout.Space(5f);
			}

			switch (Settings.soundEffectsFoldout)
			{
				case ToolkitSettings.SettingsEditorSFXFoldout.Impacts:
					EditorGUILayout.LabelField("Impact Sound Effects", largeLabel);

					break;

				case ToolkitSettings.SettingsEditorSFXFoldout.Gears:
					EditorGUILayout.LabelField("Gear Sound Effects", largeLabel);

					float newGearShiftingVolume = ToolkitEditorUtility.Slider("Volume", Settings.gearShiftingClips.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

					if (Settings.gearShiftingClips.Volume != newGearShiftingVolume)
						Settings.gearShiftingClips.Volume = newGearShiftingVolume;

					break;

				case ToolkitSettings.SettingsEditorSFXFoldout.Exhaust:
					EditorGUILayout.LabelField("Exhaust Sound Effects", largeLabel);

					break;

				/*case ToolkitSettings.SettingsEditorSFXFoldout.Exterior:
					EditorGUILayout.LabelField("Exterior Sound Effects", largeLabel);
					GUILayout.FlexibleSpace();

#if !MVC_COMMUNITY
					if (!ToolkitInfo.IsProLicense)
#endif
						GUILayoutProIcon();

					break;*/

				case ToolkitSettings.SettingsEditorSFXFoldout.NOS:
					EditorGUILayout.LabelField("NOS Sound Effects", largeLabel);

					float newNOSVolume = ToolkitEditorUtility.Slider("Volume", Settings.NOSClips.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

					if (Settings.NOSClips.Volume != newNOSVolume)
						Settings.NOSClips.Volume = newNOSVolume;

					break;

				/*case ToolkitSettings.SFXFoldout.Horns:
					EditorGUILayout.LabelField("Horn Sound Effects", largeLabel);

					break;*/

				case ToolkitSettings.SettingsEditorSFXFoldout.Transmission:
					EditorGUILayout.LabelField("Transmission Sound Effects", largeLabel);

					break;

				default:
					EditorGUILayout.LabelField("Sound Effects", largeLabel);

					break;
			}

			GUILayout.Space(20f);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			scrollView = EditorGUILayout.BeginScrollView(scrollView, scrollViewStyle);

			switch (Settings.soundEffectsFoldout)
			{
				case ToolkitSettings.SettingsEditorSFXFoldout.Impacts:
					SFXImpactClipsEditor();

					break;

				case ToolkitSettings.SettingsEditorSFXFoldout.Gears:
					SFXGearShiftingClipsEditor();

					break;

				case ToolkitSettings.SettingsEditorSFXFoldout.Exhaust:
					SFXExhaustClipsEditor();

					break;

				/*case ToolkitSettings.SettingsEditorSFXFoldout.Exterior:
					SFXExteriorClipsEditor();

					break;*/

				case ToolkitSettings.SettingsEditorSFXFoldout.NOS:
					SFXNOSClipsEditor();

					break;

				/*case ToolkitSettings.SFXFoldout.Horns:
					SFXHornClipsEditor();

					break;*/

				case ToolkitSettings.SettingsEditorSFXFoldout.Transmission:
					SFXTransmissionClipsEditor();

					break;

				default:
					SFXDefaultEditor();

					break;
			}

			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}
		private void SFXDefaultEditor()
		{
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.TextField(new GUIContent("Audio Folder Path", "The audio folder path in your project's Resources folder"), Path.Combine("Resources", Settings.audioFolderPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

			if (GUILayout.Button(new GUIContent("...", "Browse..."), GUILayout.Width(25f)))
			{
				string suggestedAudioPath = $"{Path.Combine("BxB Studio", "MVC", "Resources").Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}";
				string newAudioFolderPath = EditorUtility.OpenFolderPanel("Locate the Audio folder...", Path.Combine(Application.dataPath, suggestedAudioPath), "Audio").Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

				if (!newAudioFolderPath.IsNullOrEmpty())
				{
					string resourcesFolderPart = $"{Path.DirectorySeparatorChar}Resources{Path.DirectorySeparatorChar}";
					int lastResourcesFolderIndex = newAudioFolderPath.LastIndexOf(resourcesFolderPart);

					if (lastResourcesFolderIndex < 0)
						EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", $"The audio folder has to be somewhere inside the Project's Resources folder.\r\n\r\nExample: \"{Path.Combine(Path.GetFileName(Path.GetDirectoryName(Application.dataPath)), "Assets", suggestedAudioPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)}\"", "Got it!");
					else
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Path");

						Settings.audioFolderPath = newAudioFolderPath.Remove(0, lastResourcesFolderIndex + resourcesFolderPart.Length);

						EditorUtility.SetDirty(Settings);
					}
				}
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.TextField(new GUIContent("Engine SFX Path", "The engine sound effects folder path in your project's audio folder"), Settings.engineSFXFolderPath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

			if (GUILayout.Button(new GUIContent("...", "Browse..."), GUILayout.Width(25f)))
			{
				string suggestedAudioPath = $"{Path.Combine("BxB Studio", "MVC", "Resources", Settings.audioFolderPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)}{Path.DirectorySeparatorChar}";
				string newEngineSFXFolderPath = EditorUtility.OpenFolderPanel("Locate the Engine SFX folder...", Path.Combine(Application.dataPath, suggestedAudioPath) + Path.DirectorySeparatorChar, "Engines").Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

				if (!newEngineSFXFolderPath.IsNullOrEmpty())
				{
					string audioFolderPart = $"{Path.DirectorySeparatorChar}Resources{Path.DirectorySeparatorChar}{Settings.audioFolderPath}{Path.DirectorySeparatorChar}";
					int audioFolderIndex = newEngineSFXFolderPath.IndexOf(audioFolderPart);

					if (audioFolderIndex < 0)
						EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", $"The engines SFX folder has to be somewhere inside the audio folder: \"{Path.Combine(Path.GetFileName(Path.GetDirectoryName(Application.dataPath)), "Assets", suggestedAudioPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)}\"", "Got it!");
					else
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Path");

						Settings.engineSFXFolderPath = newEngineSFXFolderPath.Remove(0, audioFolderIndex + audioFolderPart.Length);

						EditorUtility.SetDirty(Settings);
					}
				}
			}

			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();

			char newEngineNameSplitter = EditorGUILayout.TextField(new GUIContent("Engine Name Splitter", "The character the audio system is going to use to split the audio clip names in pieces and get the RPM level of each file"), Settings.engineSFXNameSplitter.ToString()).FirstOrDefault();

			if (Settings.engineSFXNameSplitter != newEngineNameSplitter)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Splitter");

				Settings.engineSFXNameSplitter = newEngineNameSplitter;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			int newMaxEngineSFXClips = ToolkitEditorUtility.Slider(new GUIContent("Maximum Engine Clips", "The maximum engine clips allocation number per vehicle, decreasing this may optimize performance by may lower the audio quality as well"), Settings.maxEngineSFXClips, 1, 20, Settings, "Change Count");

			if (Settings.maxEngineSFXClips != newMaxEngineSFXClips)
				Settings.maxEngineSFXClips = newMaxEngineSFXClips;

			EditorGUI.EndDisabledGroup();

			float newSFXVolume = ToolkitEditorUtility.Slider(new GUIContent("Global Volume", "The overall SFX volume for every Audio Source controlled by the MVC Systems"), Settings.SFXVolume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.SFXVolume != newSFXVolume)
				Settings.SFXVolume = newSFXVolume;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Audio Clips", EditorStyles.boldLabel);

			EditorGUI.indentLevel++;

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			AudioClip newReversingClip = EditorGUILayout.ObjectField(new GUIContent("Reversing", "The reversing vehicle's gearbox sound"), Settings.reversingClip, typeof(AudioClip), false) as AudioClip;

			if (Settings.reversingClip != newReversingClip)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Clip");

				Settings.reversingClip = newReversingClip;

				EditorUtility.SetDirty(Settings);
			}

			AudioClip newWindClip = EditorGUILayout.ObjectField(new GUIContent("Wind", "The vehicle's aerodynamic wind effect at higher speeds"), Settings.windClip, typeof(AudioClip), false) as AudioClip;

			if (Settings.windClip != newWindClip)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Clip");

				Settings.windClip = newWindClip;

				EditorUtility.SetDirty(Settings);
			}

			AudioClip newBrakeClip = EditorGUILayout.ObjectField(new GUIContent("Brakes", "The vehicle brakes sound while pressing the brake pedal"), Settings.brakeClip, typeof(AudioClip), false) as AudioClip;

			if (Settings.brakeClip != newBrakeClip)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Clip");

				Settings.brakeClip = newBrakeClip;

				EditorUtility.SetDirty(Settings);
			}

			if (Settings.useLights)
			{
				EditorGUILayout.BeginHorizontal();

				AudioClip newIndicatorClip = EditorGUILayout.ObjectField(new GUIContent("Indicator Lights", "The vehicle indicator (side signal) lights sound while switching"), Settings.indicatorClip, typeof(AudioClip), false) as AudioClip;

				if (Settings.indicatorClip != newIndicatorClip)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Clip");

					Settings.indicatorClip = newIndicatorClip;

					EditorUtility.SetDirty(Settings);
				}

				EditorGUILayout.EndHorizontal();
			}

			if (Settings.useDamage && Settings.useWheelHealth)
			{
				EditorGUILayout.BeginHorizontal();

				AudioClip newTireExplosionClip = EditorGUILayout.ObjectField(new GUIContent("Tire Explosion", "The vehicle wheel tire explosion sound"), Settings.tireExplosionClip, typeof(AudioClip), false) as AudioClip;

				if (Settings.tireExplosionClip != newTireExplosionClip)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Clip");

					Settings.tireExplosionClip = newTireExplosionClip;

					EditorUtility.SetDirty(Settings);
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Impact Clips", EditorStyles.miniBoldLabel);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				EnableSFXFoldout(ToolkitSettings.SettingsEditorSFXFoldout.Impacts, true);

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Gear Shifting Clips", EditorStyles.miniBoldLabel);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				EnableSFXFoldout(ToolkitSettings.SettingsEditorSFXFoldout.Gears, true);

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Exhaust Clips", EditorStyles.miniBoldLabel);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				EnableSFXFoldout(ToolkitSettings.SettingsEditorSFXFoldout.Exhaust, true);

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
			/*EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Exterior Clips", EditorStyles.miniBoldLabel);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

#if !MVC_COMMUNITY
			if (!ToolkitInfo.IsProLicense)
#endif
				GUILayoutProIcon();

			if (GUILayout.Button(EditorUtilities.Icons.Pencil, VehicleEditorUtility.UnstretchableMiniButtonWide))
				EnableSFXFoldout(ToolkitSettings.SettingsEditorSFXFoldout.Exterior, true);

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();*/
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("NOS Clips", EditorStyles.miniBoldLabel);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				EnableSFXFoldout(ToolkitSettings.SettingsEditorSFXFoldout.NOS, true);

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
			/*EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Horn Clips", EditorStyles.miniBoldLabel);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (!ToolkitSettings.IsPlusVersion)
				ToolkitBehaviourEditor.EditorGUILayoutPlusIcon();

			if (GUILayout.Button(EditorUtilities.Icons.Pencil, VehicleEditorUtility.UnstretchableMiniButtonWide))
				EnableSFXFoldout(ToolkitSettings.SFXFoldout.Horns, true);
			
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();*/
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Transmission Clips", EditorStyles.miniBoldLabel);

			if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				EnableSFXFoldout(ToolkitSettings.SettingsEditorSFXFoldout.Transmission, true);

			EditorGUILayout.EndHorizontal();

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Audio Mixers", EditorStyles.boldLabel);

			EditorGUI.indentLevel++;

			AudioMixerGroup newExhaustEffectsMixer = EditorGUILayout.ObjectField(new GUIContent("Exhaust Effects", "The engine exhaust effects audio mixer group"), Settings.audioMixers.exhaustEffects, typeof(AudioMixerGroup), false) as AudioMixerGroup;

			if (Settings.audioMixers.exhaustEffects != newExhaustEffectsMixer)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Mixer");

				Settings.audioMixers.exhaustEffects = newExhaustEffectsMixer;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			AudioMixerGroup newChargersEffectsMixer = EditorGUILayout.ObjectField(new GUIContent("Chargers Effects", "The chargers effects audio mixer group"), Settings.audioMixers.chargersEffects, typeof(AudioMixerGroup), false) as AudioMixerGroup;

			if (Settings.audioMixers.chargersEffects != newChargersEffectsMixer)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Mixer");

				Settings.audioMixers.chargersEffects = newChargersEffectsMixer;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.EndDisabledGroup();

			AudioMixerGroup newTurbochargerMixer = EditorGUILayout.ObjectField(new GUIContent("Turbocharger", "The turbocharger audio mixer group"), Settings.audioMixers.turbocharger, typeof(AudioMixerGroup), false) as AudioMixerGroup;

			if (Settings.audioMixers.turbocharger != newTurbochargerMixer)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Mixer");

				Settings.audioMixers.turbocharger = newTurbochargerMixer;

				EditorUtility.SetDirty(Settings);
			}

			AudioMixerGroup newSuperchargerMixer = EditorGUILayout.ObjectField(new GUIContent("Supercharger", "The supercharger audio mixer group"), Settings.audioMixers.supercharger, typeof(AudioMixerGroup), false) as AudioMixerGroup;

			if (Settings.audioMixers.supercharger != newSuperchargerMixer)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Mixer");

				Settings.audioMixers.supercharger = newSuperchargerMixer;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			AudioMixerGroup newTransmissionMixer = EditorGUILayout.ObjectField(new GUIContent("Transmission", "The transmission audio mixer group"), Settings.audioMixers.transmission, typeof(AudioMixerGroup), false) as AudioMixerGroup;

			if (Settings.audioMixers.transmission != newTransmissionMixer)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Mixer");

				Settings.audioMixers.transmission = newTransmissionMixer;

				EditorUtility.SetDirty(Settings);
			}
			EditorGUI.EndDisabledGroup();


			AudioMixerGroup newBrakesMixer = EditorGUILayout.ObjectField(new GUIContent("Brakes Effects", "The brakes audio mixer group"), Settings.audioMixers.brakeEffects, typeof(AudioMixerGroup), false) as AudioMixerGroup;

			if (Settings.audioMixers.brakeEffects != newBrakesMixer)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Mixer");

				Settings.audioMixers.brakeEffects = newBrakesMixer;

				EditorUtility.SetDirty(Settings);
			}

			AudioMixerGroup newNOSMixer = EditorGUILayout.ObjectField(new GUIContent("NOS", "The NOS audio mixer group"), Settings.audioMixers.NOS, typeof(AudioMixerGroup), false) as AudioMixerGroup;

			if (Settings.audioMixers.NOS != newNOSMixer)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Mixer");

				Settings.audioMixers.NOS = newNOSMixer;

				EditorUtility.SetDirty(Settings);
			}

			AudioMixerGroup newWheelEffectsMixer = EditorGUILayout.ObjectField(new GUIContent("Wheel Effects", "The wheel effects audio mixer group"), Settings.audioMixers.wheelEffects, typeof(AudioMixerGroup), false) as AudioMixerGroup;

			if (Settings.audioMixers.wheelEffects != newWheelEffectsMixer)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Mixer");

				Settings.audioMixers.wheelEffects = newWheelEffectsMixer;

				EditorUtility.SetDirty(Settings);
			}

			AudioMixerGroup newEnvironmentalEffectsMixer = EditorGUILayout.ObjectField(new GUIContent("Enviro. Effects", "The environmental effects audio mixer group"), Settings.audioMixers.environmentalEffects, typeof(AudioMixerGroup), false) as AudioMixerGroup;

			if (Settings.audioMixers.environmentalEffects != newEnvironmentalEffectsMixer)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Mixer");

				Settings.audioMixers.environmentalEffects = newEnvironmentalEffectsMixer;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
		}
		private void SFXImpactClipsEditor()
		{
			List<AudioClip> clipsList;
			AudioClip newClip;
			bool clipsListChanged = false;

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Cars", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			float newCarsImpactClipsVolume = ToolkitEditorUtility.Slider("Volume", Settings.impactClips.cars.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.impactClips.cars.Volume != newCarsImpactClipsVolume)
				Settings.impactClips.cars.Volume = newCarsImpactClipsVolume;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.impactClips.cars.low.ToList();

			EditorGUILayout.LabelField($"Low ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.impactClips.cars.low = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.impactClips.cars.medium.ToList();

			EditorGUILayout.LabelField($"Medium ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.impactClips.cars.medium = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.impactClips.cars.high.ToList();

			EditorGUILayout.LabelField($"High ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.impactClips.cars.high = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Walls", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			float newWallsImpactClipsVolume = ToolkitEditorUtility.Slider("Volume", Settings.impactClips.walls.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.impactClips.walls.Volume != newWallsImpactClipsVolume)
				Settings.impactClips.walls.Volume = newWallsImpactClipsVolume;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.impactClips.walls.low.ToList();

			EditorGUILayout.LabelField($"Low ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.impactClips.walls.low = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.impactClips.walls.medium.ToList();

			EditorGUILayout.LabelField($"Medium ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.impactClips.walls.medium = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.impactClips.walls.high.ToList();

			EditorGUILayout.LabelField($"High ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.impactClips.walls.high = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Others", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			float newImpactClipsVolume = ToolkitEditorUtility.Slider("Volume", Settings.impactClips.ClipsVolume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.impactClips.ClipsVolume != newImpactClipsVolume)
				Settings.impactClips.ClipsVolume = newImpactClipsVolume;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.impactClips.glassCrack.ToList();

			EditorGUILayout.LabelField($"Glass Crack ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.impactClips.glassCrack = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.impactClips.glassShatter.ToList();

			EditorGUILayout.LabelField($"Glass Shatter ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.impactClips.glassShatter = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.impactClips.metalFences.ToList();

			EditorGUILayout.LabelField($"Metal Fences ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.impactClips.metalFences = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.impactClips.roadCones.ToList();

			EditorGUILayout.LabelField($"Road Cones ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.impactClips.roadCones = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.impactClips.roadSigns.ToList();

			EditorGUILayout.LabelField($"Road Signs ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.impactClips.roadSigns = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.impactClips.roadMisc.ToList();

			EditorGUILayout.LabelField($"Road Misc ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.impactClips.roadMisc = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
		}
		private void SFXGearShiftingClipsEditor()
		{
			List<AudioClip> clipsList;
			AudioClip newClip;
			bool clipsListChanged = false;

			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.gearShiftingClips.up.ToList();

			EditorGUILayout.LabelField($"Shifting Up ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.gearShiftingClips.up = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.gearShiftingClips.down.ToList();

			EditorGUILayout.LabelField($"Shifting Down ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.gearShiftingClips.down = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}
		private void SFXExhaustClipsEditor()
		{
			List<AudioClip> clipsList;
			AudioClip newClip;
			bool clipsListChanged = false;

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("American Muscle Cars", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			float newAmericanMuscleExhaustClipsVolume = ToolkitEditorUtility.Slider("Volume", Settings.exhaustClips.americanMuscle.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.exhaustClips.americanMuscle.Volume != newAmericanMuscleExhaustClipsVolume)
				Settings.exhaustClips.americanMuscle.Volume = newAmericanMuscleExhaustClipsVolume;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.americanMuscle.pop.ToList();

			EditorGUILayout.LabelField($"Pops ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.americanMuscle.pop = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.americanMuscle.gurgle.ToList();

			EditorGUILayout.LabelField($"Gurgles ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.americanMuscle.gurgle = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Elite Cars", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			float newEliteExhaustClipsVolume = ToolkitEditorUtility.Slider("Volume", Settings.exhaustClips.eliteCars.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.exhaustClips.eliteCars.Volume != newEliteExhaustClipsVolume)
				Settings.exhaustClips.eliteCars.Volume = newEliteExhaustClipsVolume;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.eliteCars.pop.ToList();

			EditorGUILayout.LabelField($"Pops ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.eliteCars.pop = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.eliteCars.gurgle.ToList();

			EditorGUILayout.LabelField($"Gurgles ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.eliteCars.gurgle = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("F1 Cars", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			float newF1ExhaustClipsVolume = ToolkitEditorUtility.Slider("Volume", Settings.exhaustClips.f1Cars.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.exhaustClips.f1Cars.Volume != newF1ExhaustClipsVolume)
				Settings.exhaustClips.f1Cars.Volume = newF1ExhaustClipsVolume;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.f1Cars.pop.ToList();

			EditorGUILayout.LabelField($"Pops ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.f1Cars.pop = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.f1Cars.gurgle.ToList();

			EditorGUILayout.LabelField($"Gurgles ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.f1Cars.gurgle = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Road Cars", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			float newRoadExhaustClipsVolume = ToolkitEditorUtility.Slider("Volume", Settings.exhaustClips.roadCars.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.exhaustClips.roadCars.Volume != newRoadExhaustClipsVolume)
				Settings.exhaustClips.roadCars.Volume = newRoadExhaustClipsVolume;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.roadCars.pop.ToList();

			EditorGUILayout.LabelField($"Pops ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.roadCars.pop = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.roadCars.gurgle.ToList();

			EditorGUILayout.LabelField($"Gurgles ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.roadCars.gurgle = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Super Cars", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			float newSuperExhaustClipsVolume = ToolkitEditorUtility.Slider("Volume", Settings.exhaustClips.superCars.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.exhaustClips.superCars.Volume != newSuperExhaustClipsVolume)
				Settings.exhaustClips.superCars.Volume = newSuperExhaustClipsVolume;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.superCars.pop.ToList();

			EditorGUILayout.LabelField($"Pops ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.superCars.pop = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.superCars.gurgle.ToList();

			EditorGUILayout.LabelField($"Gurgles ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.superCars.gurgle = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Road Heavy Trucks", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			float newRoadHeavyExhaustClipsVolume = ToolkitEditorUtility.Slider("Volume", Settings.exhaustClips.roadHeavyTrucks.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.exhaustClips.roadHeavyTrucks.Volume != newRoadHeavyExhaustClipsVolume)
				Settings.exhaustClips.roadHeavyTrucks.Volume = newRoadHeavyExhaustClipsVolume;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.roadHeavyTrucks.pop.ToList();

			EditorGUILayout.LabelField($"Pops ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.roadHeavyTrucks.pop = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.roadHeavyTrucks.gurgle.ToList();

			EditorGUILayout.LabelField($"Gurgles ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.roadHeavyTrucks.gurgle = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Sport Heavy Trucks", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			float newSportHeavyExhaustClipsVolume = ToolkitEditorUtility.Slider("Volume", Settings.exhaustClips.sportHeavyTrucks.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.exhaustClips.sportHeavyTrucks.Volume != newSportHeavyExhaustClipsVolume)
				Settings.exhaustClips.sportHeavyTrucks.Volume = newSportHeavyExhaustClipsVolume;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.sportHeavyTrucks.pop.ToList();

			EditorGUILayout.LabelField($"Pops ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.sportHeavyTrucks.pop = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exhaustClips.sportHeavyTrucks.gurgle.ToList();

			EditorGUILayout.LabelField($"Gurgles ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.exhaustClips.sportHeavyTrucks.gurgle = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}
		/*private void SFXExteriorClipsEditor()
		{
			List<AudioClip> clipsList;
			AudioClip newClip;
			bool clipsListChanged = false;

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Doors", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			float newDoorsClipsVolume = VehicleEditorUtility.Slider("Volume", Settings.exteriorClips.doors.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.exteriorClips.doors.Volume != newDoorsClipsVolume)
				Settings.exteriorClips.doors.Volume = newDoorsClipsVolume;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exteriorClips.doors.open.ToList();

			EditorGUILayout.LabelField($"Opening ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, VehicleEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, VehicleEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
		
			if (clipsListChanged)
			{
				Settings.exteriorClips.doors.open = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exteriorClips.doors.close.ToList();

			EditorGUILayout.LabelField($"Closing ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, VehicleEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, VehicleEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
		
			if (clipsListChanged)
			{
				Settings.exteriorClips.doors.close = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Hood (Bonnet)", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			float newHoodClipsVolume = VehicleEditorUtility.Slider("Volume", Settings.exteriorClips.hood.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.exteriorClips.hood.Volume != newHoodClipsVolume)
				Settings.exteriorClips.hood.Volume = newHoodClipsVolume;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exteriorClips.hood.open.ToList();

			EditorGUILayout.LabelField($"Opening ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, VehicleEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, VehicleEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
		
			if (clipsListChanged)
			{
				Settings.exteriorClips.hood.open = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exteriorClips.hood.close.ToList();

			EditorGUILayout.LabelField($"Closing ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, VehicleEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, VehicleEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
		
			if (clipsListChanged)
			{
				Settings.exteriorClips.hood.close = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Trunk (Boot)", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			float newTrunkClipsVolume = VehicleEditorUtility.Slider("Volume", Settings.exteriorClips.trunk.Volume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (Settings.exteriorClips.trunk.Volume != newTrunkClipsVolume)
				Settings.exteriorClips.trunk.Volume = newTrunkClipsVolume;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exteriorClips.trunk.open.ToList();

			EditorGUILayout.LabelField($"Opening ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, VehicleEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, VehicleEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
		
			if (clipsListChanged)
			{
				Settings.exteriorClips.trunk.open = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.exteriorClips.trunk.close.ToList();

			EditorGUILayout.LabelField($"Closing ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, VehicleEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, VehicleEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
		
			if (clipsListChanged)
			{
				Settings.exteriorClips.trunk.close = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}*/
		private void SFXNOSClipsEditor()
		{
			List<AudioClip> clipsList;
			AudioClip newClip;
			bool clipsListChanged = false;

			EditorGUI.BeginDisabledGroup(!Settings.useNOSDelay);
			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.NOSClips.starting.ToList();

			EditorGUILayout.LabelField($"Starting ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.NOSClips.starting = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();

			if (!Settings.useNOSDelay)
				EditorGUILayout.HelpBox("The feature above has been disabled because the `Use NOS Delay` toggle has been turned off.", MessageType.Info);

			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.NOSClips.active.ToList();

			EditorGUILayout.LabelField($"Boosting (Active) ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.NOSClips.active = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}
		/*private void SFXHornClipsEditor()
		{
			List<AudioClip> clipsList;
			AudioClip newClip;
			bool clipsListChanged = false;

			EditorGUILayout.BeginVertical(GUI.skin.box);

			clipsList = Settings.hornClips.ToList();

			EditorGUILayout.LabelField($"Horns ({clipsList.Count})", EditorStyles.miniBoldLabel);

			for (int i = 0; i < clipsList.Count; i++)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(clipsList[i], typeof(AudioClip), false);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, VehicleEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						clipsList.RemoveAt(i);

						clipsListChanged = true;
					}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.BeginHorizontal();

			newClip = EditorGUILayout.ObjectField(null, typeof(AudioClip), false) as AudioClip;

			if (newClip)
			{
				if (clipsList.IndexOf(newClip) > -1)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The added asset seem to already exist in this list, there's no need to add it multiple times.", "Sure!");
				else
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					clipsList.Add(newClip);

					clipsListChanged = true;
				}
			}

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(EditorUtilities.Icons.Add, VehicleEditorUtility.UnstretchableMiniButtonWide);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (clipsListChanged)
			{
				Settings.hornClips = clipsList.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}*/
		private void SFXTransmissionClipsEditor()
		{
			List<ToolkitSettings.TransmissionWhineClipsGroup> groups = Settings.transmissionWhineGroups.ToList();
			bool groupsListChanged = false;
			bool clipsListChanged = false;
			int removeGroup = -1;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField($"Transmissions ({groups.Count})", EditorStyles.miniBoldLabel);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Add New Group");
				groups.Add(new("New Transmission"));
				EditorUtility.SetDirty(Settings);

				groupsListChanged = true;

			}

			if (groups.Count > 1)
				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Are you sure you want to remove all the transmission groups?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Groups");
						groups.Clear();

						groupsListChanged = true;
					}

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			for (int i = 0; i < groups.Count; i++)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginHorizontal();

				float orgLabelWidth = EditorGUIUtility.labelWidth;

				EditorGUIUtility.labelWidth = 150f;

				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				List<ToolkitSettings.TransmissionWhineClip> newClipsList = groups[i].clips.ToList();
				string newName = EditorGUILayout.TextField("Name", groups[i].name);

				if (groups[i].name != newName)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Name");

					groups[i].name = newName;
					groupsListChanged = true;
				}

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					removeGroup = i;

				EditorGUI.EndDisabledGroup();
				EditorGUILayout.EndHorizontal();
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
				EditorGUILayout.BeginHorizontal();

				AudioMixerGroup newMixer = EditorGUILayout.ObjectField("Audio Mixer", groups[i].mixer, typeof(AudioMixerGroup), false) as AudioMixerGroup;

				if (groups[i].mixer != newMixer)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Mixer");

					groups[i].mixer = newMixer;
					groupsListChanged = true;
				}

				EditorGUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();

				float newDecelerationLowPassFrequency = ToolkitEditorUtility.Slider(new GUIContent("Decel. Low Pass Freq.", "The low pass frequency of the audio clips while deceleration"), groups[i].DecelerationLowPassFrequency, 10f, 22000f, Utility.Units.Frequency, Settings, "Change Frequency");

				if (groups[i].DecelerationLowPassFrequency != newDecelerationLowPassFrequency)
				{
					groups[i].DecelerationLowPassFrequency = newDecelerationLowPassFrequency;
					groupsListChanged = true;
				}

				float newClutchVolume = ToolkitEditorUtility.Slider(new GUIContent("Clutch Volume", "The volume of the audio clips when the vehicle Clutch is engaged"), groups[i].ClutchVolume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

				if (groups[i].ClutchVolume != newClutchVolume)
				{
					groups[i].ClutchVolume = newClutchVolume;
					groupsListChanged = true;
				}

				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Clips", EditorStyles.miniBoldLabel);

				if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Add Clip");
					newClipsList.Add(new()
					{
						Speed = newClipsList.Count > 0 ? newClipsList[^1].Speed + 50f : 50f
					});

					clipsListChanged = true;
				}

				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel++;

				for (int j = 0; j < newClipsList.Count; j++)
				{
					EditorGUILayout.BeginHorizontal();

					float newSpeed = ToolkitEditorUtility.NumberField($"Clip {j + 1}", newClipsList[j].Speed, Utility.Units.Speed, 1, Settings, "Change Speed");

					if (newClipsList[j].Speed != newSpeed)
					{
						newClipsList[j].Speed = newSpeed;
						clipsListChanged = true;
					}

					EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

					EditorGUI.indentLevel--;

					AudioClip newClip = EditorGUILayout.ObjectField(newClipsList[j].clip, typeof(AudioClip), false) as AudioClip;

					if (newClipsList[j].clip != newClip)
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Clip");

						newClipsList[j].clip = newClip;
						clipsListChanged = true;
					}

					EditorGUI.indentLevel++;

					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Clip");
						newClipsList.RemoveAt(j);

						clipsListChanged = true;
					}

					EditorGUI.EndDisabledGroup();
					EditorGUILayout.EndHorizontal();
				}

				EditorGUI.indentLevel--;

				if (!EditorApplication.isPlaying && newClipsList.Count < 1)
					EditorGUILayout.HelpBox("To add a new Group Clip, you have to click on the `+` button!", MessageType.Info);
				else
					EditorGUILayout.Space();

				if (i < groups.Count && clipsListChanged)
					groups[i].clips = newClipsList.ToArray();

				EditorGUIUtility.labelWidth = orgLabelWidth;

				EditorGUILayout.EndVertical();
			}

			if (removeGroup != -1)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Remove Group");
				groups.RemoveAt(removeGroup);

				groupsListChanged = true;
			}

			if (groupsListChanged || clipsListChanged)
			{
				Settings.transmissionWhineGroups = groups.ToArray();

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.Space();

			if (!EditorApplication.isPlaying && groups.Count < 1)
				EditorGUILayout.HelpBox("To add a new Transmission Group, you have to click on the `+` button!", MessageType.Info);

			EditorGUILayout.Space();
		}
		private void VFXSettingsEditor()
		{
			GUIStyle largeLabel = new(EditorStyles.boldLabel)
			{
				fontSize = 17,
				fixedHeight = 20f
			};

			EditorGUILayout.LabelField("Visual Effects", largeLabel);
			EditorGUILayout.Space();

			scrollView = EditorGUILayout.BeginScrollView(scrollView, scrollViewStyle);

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			bool newUseParticleSystems = ToolkitEditorUtility.ToggleButtons("Particle Systems", EditorStyles.boldLabel, "On", "Off", Settings.useParticleSystems, Settings, "Switch Particle Systems");

			if (Settings.useParticleSystems != newUseParticleSystems)
				Settings.useParticleSystems = newUseParticleSystems;

			EditorGUI.EndDisabledGroup();

			if (Settings.useParticleSystems)
			{
				EditorGUI.indentLevel++;

				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
				EditorGUILayout.BeginHorizontal();

				ParticleSystem newExhaustSmoke = EditorGUILayout.ObjectField(new GUIContent("Exhaust Smoke", "The main exhaust smoke particle effect system for diesel vehicles"), Settings.exhaustSmoke, typeof(ParticleSystem), false) as ParticleSystem;

				if (Settings.exhaustSmoke != newExhaustSmoke)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Smoke");

					Settings.exhaustSmoke = newExhaustSmoke;

					EditorUtility.SetDirty(Settings);
				}

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();

				ParticleSystem newExhaustFlame = EditorGUILayout.ObjectField(new GUIContent("Exhaust Flame", "The main exhaust flame particle effect system"), Settings.exhaustFlame, typeof(ParticleSystem), false) as ParticleSystem;

				if (Settings.exhaustFlame != newExhaustFlame)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Flame");

					Settings.exhaustFlame = newExhaustFlame;

					EditorUtility.SetDirty(Settings);
				}

				EditorGUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(!Settings.exhaustFlame);
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				EditorGUI.indentLevel++;

				ToolkitSettings.VFXOverrideMode newNOSFlameOverrideMode = (ToolkitSettings.VFXOverrideMode)EditorGUILayout.EnumPopup(new GUIContent("NOS Flame Type", "The used method to override the NOS flame, either by using a particle system, or using the Exhaust Flame effect and a simple Material"), Settings.NOSFlameOverrideMode);

				if (Settings.NOSFlameOverrideMode != newNOSFlameOverrideMode)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Mode");

					Settings.NOSFlameOverrideMode = newNOSFlameOverrideMode;

					EditorUtility.SetDirty(Settings);
				}

				if (Settings.NOSFlameOverrideMode == ToolkitSettings.VFXOverrideMode.Material)
				{
					Material newNOSFlameMaterial = EditorGUILayout.ObjectField(new GUIContent("Material", "The exhaust's NOS flame particle effect renderer material"), Settings.NOSFlameMaterial, typeof(Material), false) as Material;

					if (Settings.NOSFlameMaterial != newNOSFlameMaterial)
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Material");

						Settings.NOSFlameMaterial = newNOSFlameMaterial;

						EditorUtility.SetDirty(Settings);
					}

					if (Settings.exhaustFlame && !Settings.NOSFlameMaterial)
						EditorGUILayout.HelpBox("The NOS Flame Material instance is missing, this means it cannot be added in runtime unless you assign it. Even though, we'll try our best to find another alternative!", MessageType.Warning);
				}
				else
				{
					ParticleSystem newNOSFlame = EditorGUILayout.ObjectField(new GUIContent("Particle System", "The exhaust's NOS flame particle effect system"), Settings.NOSFlame, typeof(ParticleSystem), false) as ParticleSystem;

					if (Settings.NOSFlame != newNOSFlame)
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Flame");

						Settings.NOSFlame = newNOSFlame;

						EditorUtility.SetDirty(Settings);
					}

					if (Settings.exhaustFlame && !Settings.NOSFlame)
						EditorGUILayout.HelpBox("The NOS Flame Particle System instance is missing, this means it cannot be instantiated in runtime unless you assign it. Even though, we'll try our best to find another alternative!", MessageType.Warning);
				}

				EditorGUI.indentLevel--;

				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();

				if (!EditorApplication.isPlaying && (!Settings.exhaustSmoke || !Settings.exhaustFlame))
					EditorGUILayout.HelpBox("It seems like one of the exhaust particle systems is missing, some features may not work properly!", MessageType.Info);

				EditorGUILayout.Space();
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
				EditorGUI.BeginDisabledGroup(!Settings.useDamage);
				EditorGUILayout.BeginHorizontal();

				ParticleSystem newDamageSparks = EditorGUILayout.ObjectField(new GUIContent("Damage Sparks", "The collision damage sparks"), Settings.damageSparks, typeof(ParticleSystem), false) as ParticleSystem;

				if (Settings.damageSparks != newDamageSparks)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Sparks");

					Settings.damageSparks = newDamageSparks;

					EditorUtility.SetDirty(Settings);
				}

				EditorGUILayout.EndHorizontal();
				EditorGUI.BeginDisabledGroup(!Settings.useWheelHealth);
				EditorGUILayout.BeginHorizontal();

				ParticleSystem newTireExplosion = EditorGUILayout.ObjectField(new GUIContent("Tire Explosion", "The wheel tire explosion effect"), Settings.tireExplosion, typeof(ParticleSystem), false) as ParticleSystem;

				if (Settings.tireExplosion != newTireExplosion)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Effect");

					Settings.tireExplosion = newTireExplosion;

					EditorUtility.SetDirty(Settings);
				}

				EditorGUILayout.EndHorizontal();
				EditorGUI.BeginDisabledGroup(!Settings.tireExplosion);

				EditorGUI.indentLevel++;

				ParticleSystemForceField newTireExplosionForce = EditorGUILayout.ObjectField(new GUIContent("Force Field", "The wheel tire explosion force field"), Settings.tireExplosionForceField, typeof(ParticleSystemForceField), false) as ParticleSystemForceField;

				if (Settings.tireExplosionForceField != newTireExplosionForce)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Force");

					Settings.tireExplosionForceField = newTireExplosionForce;

					EditorUtility.SetDirty(Settings);
				}

				EditorGUI.indentLevel--;

				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();

				ParticleSystem newCollisionSparks = EditorGUILayout.ObjectField(new GUIContent("Collision Sparks", "The collision sparks"), Settings.collisionSparks, typeof(ParticleSystem), false) as ParticleSystem;

				if (Settings.collisionSparks != newCollisionSparks)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Sparks");

					Settings.collisionSparks = newCollisionSparks;

					EditorUtility.SetDirty(Settings);
				}

				if (!EditorApplication.isPlaying && (!Settings.damageSparks || !Settings.tireExplosion || !Settings.collisionSparks))
					EditorGUILayout.HelpBox("It seems like some particle systems are missing! No effect will be used for those that are missing", MessageType.Info);

				EditorGUI.EndDisabledGroup();

				EditorGUILayout.Space();

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
#if !MVC_COMMUNITY
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUILayout.BeginHorizontal();

			bool newUseAIPathVisualizer = ToolkitEditorUtility.ToggleButtons("AI Path Visualizer", EditorStyles.boldLabel, "On", "Off", Settings.useAIPathVisualizer, Settings, "Switch Visualizer");

			if (Settings.useAIPathVisualizer != newUseAIPathVisualizer)
				Settings.useAIPathVisualizer = newUseAIPathVisualizer;

			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();

			if (Settings.useAIPathVisualizer)
			{
				EditorGUI.indentLevel++;

				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				Material newPathVisualizerMaterial = EditorGUILayout.ObjectField(new GUIContent("Renderer Material", "The AI path visualizer mesh renderer material"), Settings.pathVisualizerMaterial, typeof(Material), false) as Material;

				if (Settings.pathVisualizerMaterial != newPathVisualizerMaterial)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Material");

					Settings.pathVisualizerMaterial = newPathVisualizerMaterial;

					EditorUtility.SetDirty(Settings);
				}

				if (!Settings.pathVisualizerMaterial)
					EditorGUILayout.HelpBox("Please consider adding an AI path visualizer material, or else all AI components will be disabled. Otherwise, you can disable this feature from the toggle above to prevent the toolkit from crashing.", MessageType.Warning);

				EditorGUI.BeginDisabledGroup(!Settings.pathVisualizerMaterial);

				float newPathVisualizerGroundOffset = ToolkitEditorUtility.Slider(new GUIContent("Ground Offset", "The AI path visualizer mesh offset starting from the path points, considering their normals"), Settings.pathVisualizerGroundOffset, 0f, 3f, Utility.Units.Distance, Settings, "Change Offset");

				if (Settings.pathVisualizerGroundOffset != newPathVisualizerGroundOffset)
					Settings.pathVisualizerGroundOffset = newPathVisualizerGroundOffset;

				float newPathVisualizerWidth = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Width", "The path visualizer mark width"), Settings.pathVisualizerWidth * 1000f, Utility.Units.SizeAccurate, true, Settings, "Change Width") * .001f, .001f);

				if (Settings.pathVisualizerWidth != newPathVisualizerWidth)
					Settings.pathVisualizerWidth = newPathVisualizerWidth;

				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(!Settings.pathVisualizerMaterial);

				Gradient newPathVisualizerColorGradient = EditorGUILayout.GradientField(new GUIContent("Heat Gradient", "This gradient indicates the player's vehicle speed compared to the path current maximum speed"), Settings.pathVisualizerHeatColorGradient);

				if (Settings.pathVisualizerHeatColorGradient != newPathVisualizerColorGradient)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Gradient");

					Settings.pathVisualizerHeatColorGradient = newPathVisualizerColorGradient;

					EditorUtility.SetDirty(Settings);
				}

				EditorGUI.EndDisabledGroup();
				EditorGUILayout.Space();

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
#endif
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			bool newUseWheelMarks = ToolkitEditorUtility.ToggleButtons("Wheel Marks", EditorStyles.boldLabel, "On", "Off", Settings.useWheelMarks, Settings, "Switch Wheel Marks");

			if (Settings.useWheelMarks != newUseWheelMarks)
				Settings.useWheelMarks = newUseWheelMarks;

			EditorGUI.EndDisabledGroup();

			if (Settings.useWheelMarks)
			{
				EditorGUI.indentLevel++;

				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				float newWheelMarkResolution = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Resolution", "The wheel skid mark mesh resolution"), Settings.wheelMarkResolution), Mathf.Epsilon);

				if (Settings.wheelMarkResolution != newWheelMarkResolution)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Resolution");

					Settings.wheelMarkResolution = newWheelMarkResolution;

					EditorUtility.SetDirty(Settings);
				}

				float newWheelMarkGroundOffset = ToolkitEditorUtility.Slider(new GUIContent("Ground Offset", "The wheel skid mark mesh offset starting from the wheel hit point, considering it's normal"), Settings.wheelMarkGroundOffset, 0f, .1f, Settings, "Change Offset");

				if (Settings.wheelMarkGroundOffset != newWheelMarkGroundOffset)
					Settings.wheelMarkGroundOffset = newWheelMarkGroundOffset;

				EditorGUI.EndDisabledGroup();

				float newWheelMarkHideDistance = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Occlusion Distance", "The wheel skid mark mesh hiding distance, even when it's visible to one of the available cameras in the scene or within a game view"), Settings.wheelMarkHideDistance, Utility.Units.Distance, 1, Settings, "Change Distance"), Settings.wheelMarkHideDistance >= 10f ? 1f : 2f);

				if (Settings.wheelMarkHideDistance != newWheelMarkHideDistance)
					Settings.wheelMarkHideDistance = newWheelMarkHideDistance;

				EditorGUILayout.Space();

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			bool newUseLights = ToolkitEditorUtility.ToggleButtons("Lights", EditorStyles.boldLabel, "On", "Off", Settings.useLights, Settings, "Switch Lights");

			if (Settings.useLights != newUseLights)
				Settings.useLights = newUseLights;

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (Settings.useLights)
			{
				EditorGUI.indentLevel++;

				bool newUseIndicatorLightsAtEngineStall = ToolkitEditorUtility.ToggleButtons(new GUIContent("Indicators At Engine Stall", "Should vehicles side signal lights be turned on when its engine stalls?"), null, "On", "Off", Settings.useIndicatorLightsAtEngineStall, Settings, "Switch Indicators");

				if (Settings.useIndicatorLightsAtEngineStall != newUseIndicatorLightsAtEngineStall)
					Settings.useIndicatorLightsInFog = newUseIndicatorLightsAtEngineStall;

				bool newUseIndicatorLightsInFog = ToolkitEditorUtility.ToggleButtons(new GUIContent("Indicators At Fog Time", "Should vehicles side signal lights be turned on when its inside of a fog zone or the fog flag is enabled?"), null, "On", "Off", Settings.useIndicatorLightsInFog, Settings, "Switch Indicators");

				if (Settings.useIndicatorLightsInFog != newUseIndicatorLightsInFog)
					Settings.useIndicatorLightsInFog = newUseIndicatorLightsInFog;

				EditorGUI.BeginDisabledGroup(!Settings.useParticleSystems || EditorApplication.isPlaying);

				Light newExhaustFlameLight = EditorGUILayout.ObjectField(new GUIContent("Exhaust Flame Light", "The exhaust flame light prefab"), Settings.exhaustFlameLight, typeof(Light), false) as Light;

				if (Settings.exhaustFlameLight != newExhaustFlameLight)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Prefab");

					Settings.exhaustFlameLight = newExhaustFlameLight;

					EditorUtility.SetDirty(Settings);
				}

				EditorGUI.EndDisabledGroup();

				if (!EditorApplication.isPlaying && !Settings.useParticleSystems)
					EditorGUILayout.HelpBox("The exhaust flame light effect has been disabled because the particle system effects have been disabled, it makes no sense having a light without the flame effect actually going on.", MessageType.Info);

				float newSignalLightsPeriod = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Side Signals Period", "The vehicle's side signal lights period to complete an On and Off cycle"), Settings.signalLightsPeriod * 1000f, Utility.Units.TimeAccurate, true, Settings, "Change Period") * .001f, .01f);

				if (Settings.signalLightsPeriod != newSignalLightsPeriod)
					Settings.signalLightsPeriod = newSignalLightsPeriod;

				float newLampLightsIntensityDamping = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Lamp Lights Damping", "As you may know, lamp lights (bulbs) are not instant like LED, therefore they need a small time to reach their full potential overtime, the damping value determines how much faster the intensity goes from 0 to 1 or backward"), Settings.lampLightsIntensityDamping, false, Settings, "Change Damping"), 1f);

				if (Settings.lampLightsIntensityDamping != newLampLightsIntensityDamping)
					Settings.lampLightsIntensityDamping = newLampLightsIntensityDamping;

				EditorGUILayout.Space();
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				string newCustomLightShaderEmissionColorProperty = EditorGUILayout.TextField(new GUIContent("Custom Emission Property", "The custom shader emission color property identifier (if using one)"), Settings.customLightShaderEmissionColorProperty);

				if (Settings.customLightShaderEmissionColorProperty != newCustomLightShaderEmissionColorProperty)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Property");

					Settings.customLightShaderEmissionColorProperty = newCustomLightShaderEmissionColorProperty;

					EditorUtility.SetDirty(Settings);
				}

				EditorGUI.EndDisabledGroup();
				EditorGUILayout.Space();

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();
#if !MVC_COMMUNITY
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUILayout.BeginHorizontal();

			bool newUseWheelSpinEffect = ToolkitEditorUtility.ToggleButtons("Wheel Spin Effect", EditorStyles.boldLabel, "On", "Off", Settings.useWheelSpinEffect, Settings, "Switch Spin Effect");

			if (Settings.useWheelSpinEffect != newUseWheelSpinEffect)
				Settings.useWheelSpinEffect = newUseWheelSpinEffect;

			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();

			if (Settings.useWheelSpinEffect)
			{
				EditorGUI.indentLevel++;

				EditorGUILayout.BeginHorizontal();

				int newWheelSpinRPMThreshold = ToolkitEditorUtility.NumberField(new GUIContent("RPM Threshold", "The RPM value a wheel should exceed for the blur effect to fully take effect"), Settings.WheelSpinRPMThreshold, "rpm", "Revolutions per Minute", Settings, "Change Threshold");

				if (Settings.WheelSpinRPMThreshold != newWheelSpinRPMThreshold)
					Settings.WheelSpinRPMThreshold = newWheelSpinRPMThreshold;

				EditorGUI.indentLevel--;

				string approximateRPMToSpeed = $"Ø {Mathf.Round(30f * Utility.UnitMultiplier(Utility.Units.Size, Settings.editorValuesUnit))} {Utility.Unit(Utility.Units.Size, Settings.editorValuesUnit)} ≈ {Utility.Round(Utility.RPMToSpeed(Settings.WheelSpinRPMThreshold, .3f) * Utility.UnitMultiplier(Utility.Units.Speed, Settings.editorValuesUnit), 1):0.0} {Utility.Unit(Utility.Units.Speed, Settings.editorValuesUnit)}";

				EditorGUILayout.LabelField(approximateRPMToSpeed, EditorStyles.miniLabel, GUILayout.Width(EditorStyles.miniLabel.CalcSize(new GUIContent(approximateRPMToSpeed)).x));

				EditorGUI.indentLevel++;

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();

				int newWheelHideRPMThreshold = ToolkitEditorUtility.NumberField(new GUIContent("Hide RPM Threshold", "The RPM value at which the original wheel mesh is hidden"), Settings.WheelHideRPMThreshold, "rpm", "Revolutions per Minute", Settings, "Change Threshold");

				if (Settings.WheelHideRPMThreshold != newWheelHideRPMThreshold)
					Settings.WheelHideRPMThreshold = newWheelHideRPMThreshold;

				EditorGUI.indentLevel--;

				string approximateHideRPMToSpeed = $"Ø {Mathf.Round(30f * Utility.UnitMultiplier(Utility.Units.Size, Settings.editorValuesUnit))} {Utility.Unit(Utility.Units.Size, Settings.editorValuesUnit)} ≈ {Utility.Round(Utility.RPMToSpeed(Settings.WheelHideRPMThreshold, .3f) * Utility.UnitMultiplier(Utility.Units.Speed, Settings.editorValuesUnit), 1):0.0} {Utility.Unit(Utility.Units.Speed, Settings.editorValuesUnit)}";

				EditorGUILayout.LabelField(approximateHideRPMToSpeed, EditorStyles.miniLabel, GUILayout.Width(EditorStyles.miniLabel.CalcSize(new GUIContent(approximateHideRPMToSpeed)).x));

				EditorGUI.indentLevel++;

				EditorGUILayout.EndHorizontal();

				int newWheelSpinSamples = ToolkitEditorUtility.Slider(new GUIContent("Samples Count", "The number iterations used to blur a wheel mesh"), Settings.WheelSpinSamplesCount, 1, 64, Settings, "Change Samples");

				if (Settings.WheelSpinSamplesCount != newWheelSpinSamples)
					Settings.WheelSpinSamplesCount = newWheelSpinSamples;

				float newWheelSpinAlphaOffset = ToolkitEditorUtility.Slider(new GUIContent("Alpha Offset", "The blur transparency alpha offset"), Settings.WheelSpinAlphaOffset, -1f, 1f, Settings, "Change Offset");

				if (Settings.WheelSpinAlphaOffset != newWheelSpinAlphaOffset)
					Settings.WheelSpinAlphaOffset = newWheelSpinAlphaOffset;

				float newWheelSpinCullingDistance = ToolkitEditorUtility.NumberField(new GUIContent("Culling Distance", "The required distance between any camera and the wheel spin mesh to be culled"), Settings.WheelSpinCullingDistance, Utility.Units.Distance, false, Settings, "Change Distance");

				if (Settings.WheelSpinCullingDistance != newWheelSpinCullingDistance)
					Settings.WheelSpinCullingDistance = newWheelSpinCullingDistance;

				if (Mathf.Approximately(Settings.WheelSpinCullingDistance, 0f))
					EditorGUILayout.HelpBox("Setting the `Culling Distance` to zero (0) will render the spin meshes invisible at all times. It's advised to increase the `Culling Distance` value or disable the `Wheel Spin Effect` instead.", MessageType.Info);

				/*Color orgGUIBackgroundColor = GUI.backgroundColor;
				bool newAddWheelSpinMeshMaterialOtherPropertyExists;
				bool newAddWheelSpinMeshMaterialOtherPropertyTypeIsInvalid;

				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				Material newWheelSpinMeshTransparentMaterial = EditorGUILayout.ObjectField(new GUIContent("Spin Material (Transparent)", "The base material that the wheel spin behaviour will use to blur its mesh"), Settings.wheelSpinTransparentMaterial, typeof(Material), false) as Material;

				if (Settings.wheelSpinTransparentMaterial != newWheelSpinMeshTransparentMaterial)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Material");

					Settings.wheelSpinTransparentMaterial = newWheelSpinMeshTransparentMaterial;

					EditorUtility.SetDirty(Settings);
				}

				EditorGUI.EndDisabledGroup();

				if (Settings.wheelSpinTransparentMaterial)
				{
					ToolkitSettings.MaterialProperty property = new(Settings.wheelSpinMeshMaterialColorProperty, ToolkitSettings.MaterialProperty.PropertyType.Color);
					MaterialProperty materialProperty = MaterialEditor.GetMaterialProperty(new Material[] { Settings.wheelSpinTransparentMaterial }, Settings.wheelSpinMeshMaterialColorProperty);
					bool propertyIsInvalid = !Settings.wheelSpinMeshMaterialColorProperty.IsNullOrEmpty() && !Settings.wheelSpinMeshMaterialColorProperty.IsNullOrWhiteSpace() && !Settings.wheelSpinTransparentMaterial.HasProperty(Settings.wheelSpinMeshMaterialColorProperty);

					EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

					newAddWheelSpinMeshMaterialOtherPropertyExists = property == newAddWheelSpinMeshMaterialOtherProperty;
					GUI.backgroundColor = propertyIsInvalid ? Color.red : materialProperty.type != MaterialProperty.PropType.Color ? Color.yellow : property == newAddWheelSpinMeshMaterialOtherProperty ? Color.cyan : orgGUIBackgroundColor;

					string newWheelSpinMeshMaterialColorProperty = EditorGUILayout.TextField(new GUIContent("Color Property", "The color property index of the selected material"), Settings.wheelSpinMeshMaterialColorProperty);

					if (Settings.wheelSpinMeshMaterialColorProperty != newWheelSpinMeshMaterialColorProperty)
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Property");

						Settings.wheelSpinMeshMaterialColorProperty = newWheelSpinMeshMaterialColorProperty;

						EditorUtility.SetDirty(Settings);
					}

					property = new(Settings.wheelSpinMeshMaterialAlbedoProperty, ToolkitSettings.MaterialProperty.PropertyType.Texture);
					materialProperty = MaterialEditor.GetMaterialProperty(new Material[] { Settings.wheelSpinTransparentMaterial }, Settings.wheelSpinMeshMaterialAlbedoProperty);
					propertyIsInvalid = !Settings.wheelSpinMeshMaterialAlbedoProperty.IsNullOrEmpty() && !Settings.wheelSpinMeshMaterialAlbedoProperty.IsNullOrWhiteSpace() && !Settings.wheelSpinTransparentMaterial.HasProperty(Settings.wheelSpinMeshMaterialAlbedoProperty);
					newAddWheelSpinMeshMaterialOtherPropertyExists = newAddWheelSpinMeshMaterialOtherPropertyExists || property == newAddWheelSpinMeshMaterialOtherProperty;
					GUI.backgroundColor = propertyIsInvalid ? Color.red : materialProperty.type != MaterialProperty.PropType.Texture ? Color.yellow : property == newAddWheelSpinMeshMaterialOtherProperty ? Color.cyan : orgGUIBackgroundColor;

					string newWheelSpinMeshMaterialAlbedoProperty = EditorGUILayout.TextField(new GUIContent("Albedo Property", "The albedo texture property index of the selected material"), Settings.wheelSpinMeshMaterialAlbedoProperty);

					if (Settings.wheelSpinMeshMaterialAlbedoProperty != newWheelSpinMeshMaterialAlbedoProperty)
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Property");

						Settings.wheelSpinMeshMaterialAlbedoProperty = newWheelSpinMeshMaterialAlbedoProperty;

						EditorUtility.SetDirty(Settings);
					}

					property = new(Settings.wheelSpinMeshMaterialNormalMapProperty, ToolkitSettings.MaterialProperty.PropertyType.Texture);
					materialProperty = MaterialEditor.GetMaterialProperty(new Material[] { Settings.wheelSpinTransparentMaterial }, Settings.wheelSpinMeshMaterialNormalMapProperty);
					propertyIsInvalid = !Settings.wheelSpinMeshMaterialNormalMapProperty.IsNullOrEmpty() && !Settings.wheelSpinMeshMaterialNormalMapProperty.IsNullOrWhiteSpace() && !Settings.wheelSpinTransparentMaterial.HasProperty(Settings.wheelSpinMeshMaterialNormalMapProperty);
					newAddWheelSpinMeshMaterialOtherPropertyExists = newAddWheelSpinMeshMaterialOtherPropertyExists || property == newAddWheelSpinMeshMaterialOtherProperty;
					GUI.backgroundColor = propertyIsInvalid ? Color.red : materialProperty.type != MaterialProperty.PropType.Texture ? Color.yellow : property == newAddWheelSpinMeshMaterialOtherProperty ? Color.cyan : orgGUIBackgroundColor;

					string newWheelSpinMeshMaterialNormalProperty = EditorGUILayout.TextField(new GUIContent("Normal Map Property", "The normal map property index of the selected material"), Settings.wheelSpinMeshMaterialNormalMapProperty);

					if (Settings.wheelSpinMeshMaterialNormalMapProperty != newWheelSpinMeshMaterialNormalProperty)
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Property");

						Settings.wheelSpinMeshMaterialNormalMapProperty = newWheelSpinMeshMaterialNormalProperty;

						EditorUtility.SetDirty(Settings);
					}

					property = new(Settings.wheelSpinMeshMaterialMaskMapProperty, ToolkitSettings.MaterialProperty.PropertyType.Texture);
					materialProperty = MaterialEditor.GetMaterialProperty(new Material[] { Settings.wheelSpinTransparentMaterial }, Settings.wheelSpinMeshMaterialMaskMapProperty);
					propertyIsInvalid = !Settings.wheelSpinMeshMaterialMaskMapProperty.IsNullOrEmpty() && !Settings.wheelSpinMeshMaterialMaskMapProperty.IsNullOrWhiteSpace() && !Settings.wheelSpinTransparentMaterial.HasProperty(Settings.wheelSpinMeshMaterialMaskMapProperty);
					newAddWheelSpinMeshMaterialOtherPropertyExists = newAddWheelSpinMeshMaterialOtherPropertyExists || property == newAddWheelSpinMeshMaterialOtherProperty;
					GUI.backgroundColor = propertyIsInvalid ? Color.red : materialProperty.type != MaterialProperty.PropType.Texture ? Color.yellow : property == newAddWheelSpinMeshMaterialOtherProperty ? Color.cyan : orgGUIBackgroundColor;

					string newWheelSpinMeshMaterialMaskMapProperty = EditorGUILayout.TextField(new GUIContent("Mask Map Property", "The mask map property index of the selected material"), Settings.wheelSpinMeshMaterialMaskMapProperty);

					if (Settings.wheelSpinMeshMaterialMaskMapProperty != newWheelSpinMeshMaterialMaskMapProperty)
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Property");

						Settings.wheelSpinMeshMaterialMaskMapProperty = newWheelSpinMeshMaterialMaskMapProperty;

						EditorUtility.SetDirty(Settings);
					}

					EditorGUI.EndDisabledGroup();

					GUI.backgroundColor = orgGUIBackgroundColor;

					EditorGUILayout.LabelField(new GUIContent("Other Properties", "The list of suggested properties for the spin system to copy to the spin material from the existing original mesh material"));

					List<ToolkitSettings.MaterialProperty> newWheelSpinMeshMaterialOtherProperties = Settings.wheelSpinMeshMaterialOtherProperties.ToList();

					EditorGUI.indentLevel++;

					for (int i = 0; i < newWheelSpinMeshMaterialOtherProperties.Count; i++)
					{
						property = newWheelSpinMeshMaterialOtherProperties[i];
						materialProperty = MaterialEditor.GetMaterialProperty(new Material[] { Settings.wheelSpinTransparentMaterial }, property.name);
						newAddWheelSpinMeshMaterialOtherPropertyExists = newAddWheelSpinMeshMaterialOtherPropertyExists || newAddWheelSpinMeshMaterialOtherProperty == property;

						EditorGUILayout.BeginHorizontal();
						EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
						EditorGUI.BeginDisabledGroup(true);

						GUI.backgroundColor = (int)materialProperty.type != (int)property.type ? Color.yellow : newAddWheelSpinMeshMaterialOtherProperty == property ? Color.cyan : orgGUIBackgroundColor;

						EditorGUILayout.EnumPopup(property.type);

						GUI.backgroundColor = orgGUIBackgroundColor;

						EditorGUI.EndDisabledGroup();

						EditorGUI.indentLevel -= 2;

						GUI.backgroundColor = !Settings.wheelSpinTransparentMaterial.HasProperty(property.name) ? Color.red : newAddWheelSpinMeshMaterialOtherProperty == property ? Color.cyan : orgGUIBackgroundColor;

						string newPropertyName = EditorGUILayout.TextField(property.name);

						GUI.backgroundColor = orgGUIBackgroundColor;

						EditorGUI.indentLevel += 2;

						if (property.name != newPropertyName)
							if (Settings.wheelSpinTransparentMaterial.HasProperty(newPropertyName))
							{
								Undo.RegisterCompleteObjectUndo(Settings, "Change Property");

								property.name = newPropertyName;
								newWheelSpinMeshMaterialOtherProperties[i] = property;
								Settings.wheelSpinMeshMaterialOtherProperties = newWheelSpinMeshMaterialOtherProperties.ToArray();

								EditorUtility.SetDirty(Settings);
							}

						if (GUILayout.Button(EditorUtilities.Icons.Trash, VehicleEditorUtility.UnstretchableMiniButtonWide))
						{
							Undo.RegisterCompleteObjectUndo(Settings, "Remove Property");
							newWheelSpinMeshMaterialOtherProperties.RemoveAt(i);

							Settings.wheelSpinMeshMaterialOtherProperties = newWheelSpinMeshMaterialOtherProperties.ToArray();

							EditorUtility.SetDirty(Settings);
						}

						EditorGUI.EndDisabledGroup();
						EditorGUILayout.EndHorizontal();
					}

					EditorGUILayout.BeginHorizontal();
					EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

					materialProperty = MaterialEditor.GetMaterialProperty(new Material[] { Settings.wheelSpinTransparentMaterial }, newAddWheelSpinMeshMaterialOtherProperty.name);
					newAddWheelSpinMeshMaterialOtherPropertyTypeIsInvalid = !newAddWheelSpinMeshMaterialOtherProperty.name.IsNullOrEmpty() && !newAddWheelSpinMeshMaterialOtherProperty.name.IsNullOrWhiteSpace() && Settings.wheelSpinTransparentMaterial.HasProperty(newAddWheelSpinMeshMaterialOtherProperty.name) && (int)materialProperty.type != (int)newAddWheelSpinMeshMaterialOtherProperty.type;
					GUI.backgroundColor = newAddWheelSpinMeshMaterialOtherPropertyExists ? Color.cyan : newAddWheelSpinMeshMaterialOtherPropertyTypeIsInvalid ? Color.yellow : orgGUIBackgroundColor;
					newAddWheelSpinMeshMaterialOtherProperty.type = (ToolkitSettings.MaterialProperty.PropertyType)EditorGUILayout.EnumPopup(newAddWheelSpinMeshMaterialOtherProperty.type);

					EditorGUI.indentLevel -= 2;

					GUI.backgroundColor = newAddWheelSpinMeshMaterialOtherPropertyExists ? Color.cyan : orgGUIBackgroundColor;
					newAddWheelSpinMeshMaterialOtherProperty.name = EditorGUILayout.TextField(newAddWheelSpinMeshMaterialOtherProperty.name);
					GUI.backgroundColor = orgGUIBackgroundColor;

					EditorGUI.indentLevel += 2;

					EditorGUI.BeginDisabledGroup(true);
					GUILayout.Button(EditorUtilities.Icons.Add, VehicleEditorUtility.UnstretchableMiniButtonWide);
					EditorGUI.EndDisabledGroup();
					EditorGUI.EndDisabledGroup();
					EditorGUILayout.EndHorizontal();

					if (!newAddWheelSpinMeshMaterialOtherProperty.name.IsNullOrEmpty() && !newAddWheelSpinMeshMaterialOtherProperty.name.IsNullOrWhiteSpace())
						if (Settings.wheelSpinTransparentMaterial.HasProperty(newAddWheelSpinMeshMaterialOtherProperty.name))
							if (newAddWheelSpinMeshMaterialOtherProperty.name != Settings.wheelSpinMeshMaterialColorProperty &&
								newAddWheelSpinMeshMaterialOtherProperty.name != Settings.wheelSpinMeshMaterialAlbedoProperty &&
								newAddWheelSpinMeshMaterialOtherProperty.name != Settings.wheelSpinMeshMaterialNormalMapProperty &&
								newAddWheelSpinMeshMaterialOtherProperty.name != Settings.wheelSpinMeshMaterialMaskMapProperty &&
								!newWheelSpinMeshMaterialOtherProperties.Exists(prop => prop.name == newAddWheelSpinMeshMaterialOtherProperty.name))
							{
								materialProperty = MaterialEditor.GetMaterialProperty(new Material[] { Settings.wheelSpinTransparentMaterial }, newAddWheelSpinMeshMaterialOtherProperty.name);

								if ((int)materialProperty.type == (int)newAddWheelSpinMeshMaterialOtherProperty.type)
								{
									Undo.RegisterCompleteObjectUndo(Settings, "Add Property");
									newWheelSpinMeshMaterialOtherProperties.Add(newAddWheelSpinMeshMaterialOtherProperty);
									Settings.wheelSpinMeshMaterialOtherProperties = newWheelSpinMeshMaterialOtherProperties.ToArray();
									newAddWheelSpinMeshMaterialOtherProperty = new();
				
									EditorUtility.SetDirty(Settings);
									Repaint();
								}
							}

					EditorGUI.indentLevel--;

					newWheelSpinMeshMaterialOtherProperties.Clear();
					EditorGUILayout.LabelField(new GUIContent("Keywords", "The list of suggested keywords for the spin system to copy to the spin material from the existing original mesh material in case of existence"));
					EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

					EditorGUI.indentLevel++;

					List<string> newWheelSpinMeshMaterialKeywords = Settings.wheelSpinMeshMaterialKeywords.ToList();

					for (int i = 0; i < newWheelSpinMeshMaterialKeywords.Count; i++)
					{
						EditorGUILayout.BeginHorizontal();

						string newKeyword = EditorGUILayout.TextField($"Keyword {i}", newWheelSpinMeshMaterialKeywords[i]);

						if (newWheelSpinMeshMaterialKeywords[i] != newKeyword)
						{
							Undo.RegisterCompleteObjectUndo(Settings, "Change Keyword");

							newWheelSpinMeshMaterialKeywords[i] = newKeyword;
							Settings.wheelSpinMeshMaterialKeywords = newWheelSpinMeshMaterialKeywords.ToArray();

							EditorUtility.SetDirty(Settings);
						}

						if (GUILayout.Button(EditorUtilities.Icons.Trash, VehicleEditorUtility.UnstretchableMiniButtonWide) || newKeyword.IsNullOrEmpty() || newKeyword.IsNullOrWhiteSpace())
						{
							if (!newKeyword.IsNullOrEmpty() && !newKeyword.IsNullOrWhiteSpace())
								Undo.RegisterCompleteObjectUndo(Settings, "Remove Keyword");

							newWheelSpinMeshMaterialKeywords.RemoveAt(i);

							Settings.wheelSpinMeshMaterialKeywords = newWheelSpinMeshMaterialKeywords.ToArray();

							EditorUtility.SetDirty(Settings);
						}

						EditorGUILayout.EndHorizontal();
					}

					EditorGUILayout.BeginHorizontal();

					newAddWheelSpinMeshMaterialKeyword = EditorGUILayout.TextField($"New Keyword", newAddWheelSpinMeshMaterialKeyword);

					EditorGUI.BeginDisabledGroup(newAddWheelSpinMeshMaterialKeyword.IsNullOrEmpty() || newAddWheelSpinMeshMaterialKeyword.IsNullOrWhiteSpace());

					if (GUILayout.Button(EditorUtilities.Icons.Add, VehicleEditorUtility.UnstretchableMiniButtonWide))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Add Keyword");

						newWheelSpinMeshMaterialKeywords.Add(newAddWheelSpinMeshMaterialKeyword);

						newAddWheelSpinMeshMaterialKeyword = string.Empty;
						Settings.wheelSpinMeshMaterialKeywords = newWheelSpinMeshMaterialKeywords.ToArray();

						EditorUtility.SetDirty(Settings);
					}

					EditorGUI.EndDisabledGroup();
					EditorGUI.EndDisabledGroup();
					EditorGUILayout.EndHorizontal();
					newWheelSpinMeshMaterialKeywords.Clear();

					EditorGUI.indentLevel--;

					property = default;
					materialProperty = null;
				}*/

				EditorGUILayout.Space();
				EditorGUILayout.Space();

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
#endif
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUILayout.BeginHorizontal();

			bool newUseBrakesHeat = ToolkitEditorUtility.ToggleButtons("Brakes Heat", EditorStyles.boldLabel, "On", "Off", Settings.useBrakesHeat, Settings, "Switch Brakes Heat");

			if (Settings.useBrakesHeat != newUseBrakesHeat)
				Settings.useBrakesHeat = newUseBrakesHeat;

			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();

			if (Settings.useBrakesHeat)
			{
				EditorGUI.indentLevel++;

				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				string newBrakeMaterialEmissionColorProperty = EditorGUILayout.TextField(new GUIContent("Emission Color Property", "The emission color property of the used material on brake meshes"), Settings.brakeMaterialEmissionColorProperty);

				if (Settings.brakeMaterialEmissionColorProperty != newBrakeMaterialEmissionColorProperty)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Property");

					Settings.brakeMaterialEmissionColorProperty = newBrakeMaterialEmissionColorProperty;

					EditorUtility.SetDirty(Settings);
				}

				EditorGUI.EndDisabledGroup();

				Color newBrakeHeatEmissionColor = EditorGUILayout.ColorField(new GUIContent("Heat Emission Color", "The brake mesh material heat emission color"), Settings.brakeHeatEmissionColor, false, false, true);

				if (Settings.brakeHeatEmissionColor != newBrakeHeatEmissionColor)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

					Settings.brakeHeatEmissionColor = newBrakeHeatEmissionColor;

					EditorUtility.SetDirty(Settings);
				}

				float newBrakeHeatingSpeed = ToolkitEditorUtility.Slider(new GUIContent("Heating Speed", "The brake heat increasing speed multiplier"), Settings.BrakeHeatingSpeed, .1f, 10f, Settings, "Change Speed");

				if (Settings.BrakeHeatingSpeed != newBrakeHeatingSpeed)
					Settings.BrakeHeatingSpeed = newBrakeHeatingSpeed;

				float newBrakeCoolingSpeed = ToolkitEditorUtility.Slider(new GUIContent("Cooling Speed", "The brake heat decreasing speed multiplier"), Settings.BrakeCoolingSpeed, .1f, 10f, Settings, "Change Speed");

				if (Settings.BrakeCoolingSpeed != newBrakeCoolingSpeed)
					Settings.BrakeCoolingSpeed = newBrakeCoolingSpeed;

				bool newClampBrakeHeat = ToolkitEditorUtility.ToggleButtons(new GUIContent("Clamp Brake Heat", "Should the brakes heat be limited between certain levels?"), null, "Yes", "No", Settings.clampBrakeHeat, Settings, "Switch Heat Clamp");

				if (Settings.clampBrakeHeat != newClampBrakeHeat)
					Settings.clampBrakeHeat = newClampBrakeHeat;

				bool newBrakeHeatAffectPerformance = ToolkitEditorUtility.ToggleButtons(new GUIContent("Affect Performance", "Should wheel brakes heat affect brake torque?"), null, "Yes", "No", Settings.brakeHeatAffectPerformance, Settings, "Switch Affect Perf.");

				if (Settings.brakeHeatAffectPerformance != newBrakeHeatAffectPerformance)
					Settings.brakeHeatAffectPerformance = newBrakeHeatAffectPerformance;

				EditorGUILayout.Space();

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}
		private void EditorSettingsEditor()
		{
			GUIStyle largeLabel = new(EditorStyles.boldLabel)
			{
				fontSize = 17,
				fixedHeight = 20f
			};

			EditorGUILayout.LabelField("Editor", largeLabel);
			EditorGUILayout.Space();

			scrollView = EditorGUILayout.BeginScrollView(scrollView, scrollViewStyle);

			bool newAutoCheckForUpdates = ToolkitEditorUtility.ToggleButtons(new GUIContent("Auto Update Check", "Should the toolkit check for updates automatically?"), null, "On", "Off", Settings.autoCheckForUpdates, Settings, "Switch Update");

			if (Settings.autoCheckForUpdates != newAutoCheckForUpdates)
				Settings.autoCheckForUpdates = newAutoCheckForUpdates;

			if (!Settings.autoCheckForUpdates)
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUIUtility.labelWidth + 5f);

				if (GUILayout.Button("Check For Updates"))
					ToolkitUpdate.CheckForUpdates(true, false);

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();

			Utility.UnitType newEditorUnitType = (Utility.UnitType)EditorGUILayout.EnumPopup(new GUIContent("Values Unit", "The type of unit measurements within the Unity Editor"), Settings.editorValuesUnit);

			if (Settings.editorValuesUnit != newEditorUnitType)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Values Unit");

				Settings.editorValuesUnit = newEditorUnitType;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.indentLevel++;

			Utility.UnitType newEditorPowerUnit = (Utility.UnitType)EditorGUILayout.Popup(new GUIContent("Power Unit", "The type of unit measurements within the Unity Editor for power values"), (int)Settings.editorPowerUnit, new string[] { $"{Utility.FullUnit(Utility.Units.Power, Utility.UnitType.Metric)} ({Utility.Unit(Utility.Units.Power, Utility.UnitType.Metric)})", $"{Utility.FullUnit(Utility.Units.Power, Utility.UnitType.Imperial)} ({Utility.Unit(Utility.Units.Power, Utility.UnitType.Imperial)})" });

			if (Settings.editorPowerUnit != newEditorPowerUnit)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Power Unit");

				Settings.editorPowerUnit = newEditorPowerUnit;

				EditorUtility.SetDirty(Settings);
			}

			Utility.UnitType newEditorTorqueUnit = (Utility.UnitType)EditorGUILayout.Popup(new GUIContent("Torque Unit", "The type of unit measurements within the Unity Editor for torque values"), (int)Settings.editorPowerUnit, new string[] { $"{Utility.FullUnit(Utility.Units.Torque, Utility.UnitType.Metric)} ({Utility.Unit(Utility.Units.Torque, Utility.UnitType.Metric)})", $"{Utility.FullUnit(Utility.Units.Torque, Utility.UnitType.Imperial)} ({Utility.Unit(Utility.Units.Torque, Utility.UnitType.Imperial)})" });

			if (Settings.editorTorqueUnit != newEditorTorqueUnit)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Torque Unit");

				Settings.editorTorqueUnit = newEditorTorqueUnit;

				EditorUtility.SetDirty(Settings);
			}

			ToolkitSettings.EditorSpringTargetMeasurement newSpringTargetMeasurement = (ToolkitSettings.EditorSpringTargetMeasurement)EditorGUILayout.EnumPopup(new GUIContent("Spring Target Measurement", "The way the suspension spring target should appear in the vehicle editor"), Settings.springTargetMeasurement);

			if (Settings.springTargetMeasurement != newSpringTargetMeasurement)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Measurement");

				Settings.springTargetMeasurement = newSpringTargetMeasurement;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUI.indentLevel--;

			float newExhaustSimulationFPS = ToolkitEditorUtility.NumberField(new GUIContent("Exhaust Simulation FPS", "The minimum number of Frames per Second while simulating the exhaust flame in edit mode"), Settings.exhaustSimulationFPS, "FPS", "Frames per Second", true, Settings, "Change FPS");

			if (Settings.exhaustSimulationFPS != newExhaustSimulationFPS)
				Settings.exhaustSimulationFPS = newExhaustSimulationFPS;

			bool newPreviewSuspensionAdjustmentsAtEditMode = ToolkitEditorUtility.ToggleButtons(new GUIContent("Preview Suspension Adjustments", "Should suspension adjustments be shown at Edit mode"), null, "Yes", "No", Settings.previewSuspensionAdjustmentsAtEditMode, Settings, "Switch Preview");

			if (Settings.previewSuspensionAdjustmentsAtEditMode != newPreviewSuspensionAdjustmentsAtEditMode)
				Settings.previewSuspensionAdjustmentsAtEditMode = newPreviewSuspensionAdjustmentsAtEditMode;

			bool newUseHideFlags = ToolkitEditorUtility.ToggleButtons(new GUIContent("Hide Flags", "Hide unnecessary GameObjects and Components from the Hierarchy and Inspector windows"), null, "On", "Off", Settings.useHideFlags, Settings, "Switch Hide");

			if (Settings.useHideFlags != newUseHideFlags)
				Settings.useHideFlags = newUseHideFlags;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Gizmos", EditorStyles.boldLabel);

			float newGizmosSize = ToolkitEditorUtility.Slider(new GUIContent("Gizmo Size", "The Scene View gizmos size rate"), Settings.gizmosSize, 0f, 3f, Settings, "Change Gizmo Size");

			if (Settings.gizmosSize != newGizmosSize)
				Settings.gizmosSize = newGizmosSize;

			Color newCOMGizmoColor = EditorGUILayout.ColorField(new GUIContent("COM", "Gizmo color for the vehicle's Center Of Mass"), Settings.COMGizmoColor);

			if (Settings.COMGizmoColor != newCOMGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.COMGizmoColor = newCOMGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			Color newEngineGizmoColor = EditorGUILayout.ColorField(new GUIContent("Engine", "Gizmo color for the vehicle's engine position"), Settings.engineGizmoColor);

			if (Settings.engineGizmoColor != newEngineGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.engineGizmoColor = newEngineGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			Color newExhaustGizmoColor = EditorGUILayout.ColorField(new GUIContent("Exhausts", "Gizmo color for the vehicle's exhausts position"), Settings.exhaustGizmoColor);

			if (Settings.exhaustGizmoColor != newExhaustGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.exhaustGizmoColor = newExhaustGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			Color newWheelMarkGizmoColor = EditorGUILayout.ColorField(new GUIContent("Wheel Marks", "Gizmo color for the vehicle wheel mark"), Settings.wheelMarkGizmoColor);

			if (Settings.wheelMarkGizmoColor != newWheelMarkGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.wheelMarkGizmoColor = newWheelMarkGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.BeginHorizontal();

			Color newHeadlightGizmoColor = EditorGUILayout.ColorField(new GUIContent("Headlights", "Gizmo color for the vehicle's headlights"), Settings.headlightGizmoColor);

			if (Settings.headlightGizmoColor != newHeadlightGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.headlightGizmoColor = newHeadlightGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newSideSignalLightGizmoColor = EditorGUILayout.ColorField(new GUIContent("Side Signal Lights", "Gizmo color for the vehicle's side signal lights"), Settings.sideSignalLightGizmoColor);

			if (Settings.sideSignalLightGizmoColor != newSideSignalLightGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.sideSignalLightGizmoColor = newSideSignalLightGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
#if !MVC_COMMUNITY
			EditorGUILayout.BeginHorizontal();

			Color newInteriorLightGizmoColor = EditorGUILayout.ColorField(new GUIContent("Interior Lights", "Gizmo color for the vehicle's interior lights"), Settings.interiorLightGizmoColor);

			if (Settings.interiorLightGizmoColor != newInteriorLightGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.interiorLightGizmoColor = newInteriorLightGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
#endif
			EditorGUILayout.BeginHorizontal();

			Color newRearLightGizmoColor = EditorGUILayout.ColorField(new GUIContent("Rear Lights", "Gizmo color for the vehicle's rear lights"), Settings.rearLightGizmoColor);

			if (Settings.rearLightGizmoColor != newRearLightGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.rearLightGizmoColor = newRearLightGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			/*EditorGUILayout.BeginHorizontal();

			Color newJointsGizmoColor = EditorGUILayout.ColorField(new GUIContent("Joints", "Gizmo color for the vehicle's joints"), Settings.jointsGizmoColor);

			if (Settings.jointsGizmoColor != newJointsGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.jointsGizmoColor = newJointsGizmoColor;

				EditorUtility.SetDirty(Settings);
			}
			
			EditorGUILayout.EndHorizontal();*/
			EditorGUILayout.BeginHorizontal();

			Color newDriverIKGizmoColor = EditorGUILayout.ColorField(new GUIContent("IK Pivot", "Gizmo color for the driver's IK pivots"), Settings.driverIKGizmoColor);

			if (Settings.driverIKGizmoColor != newDriverIKGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.driverIKGizmoColor = newDriverIKGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newAudioReverbZoneGizmoColor = EditorGUILayout.ColorField(new GUIContent("Audio Reverb Zone", "Gizmo color for audio reverb zones"), Settings.audioReverbZoneGizmoColor);

			if (Settings.audioReverbZoneGizmoColor != newAudioReverbZoneGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.audioReverbZoneGizmoColor = newAudioReverbZoneGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newDarknessWeatherZoneGizmoColor = EditorGUILayout.ColorField(new GUIContent("Darkness Weather Zone", "Gizmo color for the vehicle weather zones in case of darkness"), Settings.darknessWeatherZoneGizmoColor);

			if (Settings.darknessWeatherZoneGizmoColor != newDarknessWeatherZoneGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.darknessWeatherZoneGizmoColor = newDarknessWeatherZoneGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newFogWeatherZoneGizmoColor = EditorGUILayout.ColorField(new GUIContent("Fog Weather Zone", "Gizmo color for the vehicle fog weather zones"), Settings.fogWeatherZoneGizmoColor);

			if (Settings.fogWeatherZoneGizmoColor != newFogWeatherZoneGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.fogWeatherZoneGizmoColor = newFogWeatherZoneGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newWheelsDamageZoneGizmoColor = EditorGUILayout.ColorField(new GUIContent("Wheels Damage Zone", "Gizmo color for the wheels damage zones"), Settings.wheelsDamageZoneGizmoColor);

			if (Settings.wheelsDamageZoneGizmoColor != newWheelsDamageZoneGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.wheelsDamageZoneGizmoColor = newWheelsDamageZoneGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newDamageRepairZoneGizmoColor = EditorGUILayout.ColorField(new GUIContent("Damage Repair Zone", "Gizmo color for the wheels repair zones"), Settings.damageRepairZoneGizmoColor);

			if (Settings.damageRepairZoneGizmoColor != newDamageRepairZoneGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.damageRepairZoneGizmoColor = newDamageRepairZoneGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
#if !MVC_COMMUNITY
			EditorGUILayout.BeginHorizontal();

			Color newAIPathBezierColor = EditorGUILayout.ColorField(new GUIContent("AI Path Bezier", "Gizmo color for the AI Bezier path"), Settings.AIPathBezierColor);

			if (Settings.AIPathBezierColor != newAIPathBezierColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.AIPathBezierColor = newAIPathBezierColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newAIPathBezierSelectedColor = EditorGUILayout.ColorField(new GUIContent("AI Path Bezier Selected", "Gizmo color for the AI Bezier path when selected"), Settings.AIPathBezierSelectedColor);

			if (Settings.AIPathBezierSelectedColor != newAIPathBezierSelectedColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.AIPathBezierSelectedColor = newAIPathBezierSelectedColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newAIPathAnchorGizmoColor = EditorGUILayout.ColorField(new GUIContent("AI Path Anchor", "Gizmo color for the AI Bezier path anchor points"), Settings.AIPathAnchorGizmoColor);

			if (Settings.AIPathAnchorGizmoColor != newAIPathAnchorGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.AIPathAnchorGizmoColor = newAIPathAnchorGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newAIPathStartAnchorGizmoColor = EditorGUILayout.ColorField(new GUIContent("AI Path Start Anchor", "Gizmo color for the AI Bezier anchor start point"), Settings.AIPathBezierStartAnchorColor);

			if (Settings.AIPathBezierStartAnchorColor != newAIPathStartAnchorGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.AIPathBezierStartAnchorColor = newAIPathStartAnchorGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newAIPathControlsGizmoColor = EditorGUILayout.ColorField(new GUIContent("AI Path Controls", "Gizmo color for the AI Bezier path controls"), Settings.AIPathControlsGizmoColor);

			if (Settings.AIPathControlsGizmoColor != newAIPathControlsGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.AIPathControlsGizmoColor = newAIPathControlsGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newAIBrakeZoneGizmoColor = EditorGUILayout.ColorField(new GUIContent("AI Brake Zone", "Gizmo color for the AI brake zone"), Settings.AIBrakeZoneGizmoColor);

			if (Settings.AIBrakeZoneGizmoColor != newAIBrakeZoneGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.AIBrakeZoneGizmoColor = newAIBrakeZoneGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newAIHandbrakeZoneGizmoColor = EditorGUILayout.ColorField(new GUIContent("AI Handbrake Zone", "Gizmo color for the AI handbrake zone"), Settings.AIHandbrakeZoneGizmoColor);

			if (Settings.AIHandbrakeZoneGizmoColor != newAIHandbrakeZoneGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.AIHandbrakeZoneGizmoColor = newAIHandbrakeZoneGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newAINOSZoneGizmoColor = EditorGUILayout.ColorField(new GUIContent("AI NOS Zone", "Gizmo color for the AI NOS zone"), Settings.AINOSZoneGizmoColor);

			if (Settings.AINOSZoneGizmoColor != newAINOSZoneGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.AINOSZoneGizmoColor = newAINOSZoneGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newAIObstaclesSensorGizmoColor = EditorGUILayout.ColorField(new GUIContent("AI Obstacles Sensor", "Gizmo color for AI obstacle sensors"), Settings.AIObstaclesSensorGizmoColor);

			if (Settings.AIObstaclesSensorGizmoColor != newAIObstaclesSensorGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.AIObstaclesSensorGizmoColor = newAIObstaclesSensorGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			Color newAIObstaclesSensorActiveGizmoColor = EditorGUILayout.ColorField(new GUIContent("AI Obstacles Active Sensor", "Gizmo color for AI obstacle active sensors"), Settings.AIObstaclesSensorActiveGizmoColor);

			if (Settings.AIObstaclesSensorActiveGizmoColor != newAIObstaclesSensorActiveGizmoColor)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Color");

				Settings.AIObstaclesSensorActiveGizmoColor = newAIObstaclesSensorActiveGizmoColor;

				EditorUtility.SetDirty(Settings);
			}

			EditorGUILayout.EndHorizontal();
#endif
			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
		}

		#endregion

		#region Enable & Destroy

		private void OnEnable()
		{
			ToolkitSettings.LoadData(true);

			if (Settings)
				serializedObject = new(Settings);

			Undo.undoRedoPerformed += Repaint;
		}
		private void OnDestroy()
		{
			if (!cameraName.IsNullOrEmpty())
				SaveCamera();

			if (!engineName.IsNullOrEmpty())
				SaveEngine();

			if (!chargerName.IsNullOrEmpty())
				SaveCharger();
#if !MVC_COMMUNITY

			if (!tireCompoundName.IsNullOrEmpty())
				SaveTireCompound();

			if (!groundName.IsNullOrEmpty())
				SaveGround();
#endif

			if (Settings && Settings.settingsFoldout == ToolkitSettings.SettingsEditorFoldout.License)
				Settings.settingsFoldout = ToolkitSettings.SettingsEditorFoldout.None;

			ToolkitSettingsEditor.SaveSettings();

			Undo.undoRedoPerformed -= Repaint;
		}

		#endregion

		#endregion

		#endregion
	}
}

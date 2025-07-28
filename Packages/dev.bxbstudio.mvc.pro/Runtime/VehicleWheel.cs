#region Namespaces

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;
using MVC.Base;
using MVC.VFX;
using MVC.Utilities;

#endregion

namespace MVC.Core
{
	[AddComponentMenu(""), DisallowMultipleComponent]
	[DefaultExecutionOrder(-40)]
	public class VehicleWheel : ToolkitBehaviour
	{
		#region Enumerators

		public enum EditorFoldout { None, Components, Dimensions }
		public enum DriveTrain { Front = 1, Rear = -1 }
		public enum Side { Left = -1, Middle, Right }

		#endregion

		#region Modules

		[Serializable]
		public class WheelModule
		{
			#region Variables

			#region Editor Variables

			[NonSerialized]
			public EditorFoldout editorFoldout;

			#endregion

			#region Global Variables

			public VehicleWheel Instance;
			public Transform Model
			{
				get
				{
					return model;
				}
				set
				{
					model = value;

					if (value)
					{
						side = value.transform.localPosition.x > 0f ? Side.Right : value.transform.localPosition.x < 0f ? Side.Left : Side.Middle;
						driveTrain = value.transform.localPosition.z > 0f ? DriveTrain.Front : DriveTrain.Rear;
					}

					if (Instance)
					{
						Instance.radius = 0f;
						Instance.diameter = 0;
						Instance.width = 0f;
						Instance.radius = 0f;
					}

					bounds = default;
					rimBounds = default;
					tireBounds = default;
				}
			}
			public Transform Rim
			{
				get
				{
					return rim;
				}
				set
				{
					if (!Model || value && !value.IsChildOf(Model))
						return;

					rim = value;

					if (Instance)
						Instance.diameter = 0;

					rimBounds = default;
					bounds = default;
				}
			}
			public MeshRenderer RimEdgeRenderer
			{
				get
				{
					return rimEdgeRenderer;
				}
				set
				{
					if (!Model || value && !value.transform.IsChildOf(Rim))
						return;

					rimEdgeRenderer = value;
					rimEdgeMaterialIndex = 0;
				}
			}
			public bool HideRimEdgePerDefault
			{
				get
				{
					if (!RimEdgeRenderer)
						return false;

					return hideRimEdgePerDefault;
				}
				set
				{
					if (!RimEdgeRenderer)
						return;

					hideRimEdgePerDefault = value;
					RimEdgeRenderer.enabled = !value;
				}
			}
			public int RimEdgeMaterialIndex
			{
				get
				{
					if (!RimEdgeRenderer)
						return default;

					return rimEdgeMaterialIndex;
				}
				set
				{
					if (!RimEdgeRenderer || RimEdgeRenderer.sharedMaterials.Length < 2)
					{
						rimEdgeMaterialIndex = default;

						return;
					}

					rimEdgeMaterialIndex = math.clamp(value, 0, RimEdgeRenderer.sharedMaterials.Length - 1);
				}
			}
			public Transform Tire
			{
				get
				{
					return tire;
				}
				set
				{
					if (!Model || value && !value.IsChildOf(Model))
						return;

					tire = value;

					if (Instance)
					{
						Instance.width = 0f;
						Instance.radius = 0f;
					}

					tireBounds = default;
					bounds = default;
				}
			}
			public MeshRenderer BrakeDiscRenderer
			{
				get
				{
					return brakeDiscRenderer;
				}
				set
				{
					if (!Model || value && !value.transform.IsChildOf(Model))
						return;

					brakeDiscRenderer = value;
				}
			}
			public int BrakeDiscMaterialIndex
			{
				get
				{
					if (!BrakeDiscRenderer)
						return default;

					return brakeDiscMaterialIndex;
				}
				set
				{
					if (!BrakeDiscRenderer || BrakeDiscRenderer.sharedMaterials.Length < 2)
					{
						brakeDiscMaterialIndex = default;

						return;
					}

					brakeDiscMaterialIndex = math.clamp(value, 0, BrakeDiscRenderer.sharedMaterials.Length - 1);
				}
			}
			public Transform BrakeCalliper
			{
				get
				{
					return brakeCalliper;
				}
				set
				{
					if (value && vehicle && !value.IsChildOf(vehicle.transform))
						return;

					brakeCalliper = value;
				}
			}
			public Bounds Bounds
			{
				get
				{
					if (!Model)
						return default;

					if (bounds == default)
						bounds = Utility.GetObjectBounds(Model.gameObject);

					return bounds;
				}
			}
			public Bounds RimBounds
			{
				get
				{
					if (!Model)
						return default;

					if (rimBounds == default)
						rimBounds = Rim ? Utility.GetObjectBounds(Rim.gameObject) : Bounds;

					return rimBounds;
				}
			}
			public Bounds TireBounds
			{
				get
				{
					if (!Model)
						return default;

					if (tireBounds == default)
						tireBounds = Tire ? Utility.GetObjectBounds(Tire.gameObject) : Bounds;

					return tireBounds;
				}
			}
			public DriveTrain DriveTrain
			{
				get
				{
					if (IsTrailerWheel)
						driveTrain = DriveTrain.Rear;

					return driveTrain;
				}
				set
				{
					if (IsTrailerWheel)
						return;

					driveTrain = value;
				}
			}
			[FormerlySerializedAs("position")]
			public Side side;
			public string WheelName => Model ? Model.name : "New Wheel";
			public bool IsFrontWheel => driveTrain == DriveTrain.Front;
			public bool IsTrailerWheel => IsValid && vehicle.IsTrailer;
			public bool IsLeftWheel => side == Side.Left;
			public bool IsRightWheel => side == Side.Right;
			public bool IsSteerWheel
			{
				get
				{
					if (!IsValid || IsTrailerWheel)
						return false;

					return isSteerWheel;
				}
				set
				{
					if (!IsValid || IsTrailerWheel)
						return;

					isSteerWheel = value;
				}
			}
			public bool IsMotorWheel
			{
				get
				{
					if (!IsValid || IsTrailerWheel)
						return false;

					return isMotorWheel;
				}
				set
				{
					if (!IsValid || IsTrailerWheel)
						return;

					isMotorWheel = value;
				}
			}
			public bool IsValid
			{
				get
				{
					if (!vehicle && Instance)
						vehicle = Instance.VehicleInstance;

					return vehicle;
				}
			}

			[SerializeField]
			private Vehicle vehicle;
			[SerializeField]
			private Transform model;
			[SerializeField]
			private Transform rim;
			[SerializeField]
			private MeshRenderer rimEdgeRenderer;
			[SerializeField]
			private Transform tire;
			[SerializeField, FormerlySerializedAs("brakeCaliper")]
			private Transform brakeCalliper;
			[SerializeField]
			private Bounds bounds;
			[SerializeField]
			private Bounds rimBounds;
			[SerializeField]
			private Bounds tireBounds;
			[SerializeField]
			private DriveTrain driveTrain;
			[SerializeField]
			private MeshRenderer brakeDiscRenderer;
			[SerializeField]
			private bool isSteerWheel;
			[SerializeField]
			private bool isMotorWheel;
			[SerializeField, FormerlySerializedAs("hideRimEdgeAtIdle")]
			private bool hideRimEdgePerDefault;
			[SerializeField]
			private int rimEdgeMaterialIndex;
			[SerializeField]
			private int brakeDiscMaterialIndex;

			#endregion

			#endregion

			#region Constructors

			public WheelModule(Vehicle vehicle)
			{
				this.vehicle = vehicle;
			}

			#endregion

			#region Operators

			public static implicit operator bool(WheelModule wheel) => wheel != null;

			#endregion
		}
		[Serializable]
		public class SuspensionModule
		{
			#region Variables

			public float Length
			{
				get
				{
					return length;
				}
				set
				{
					length = math.max(value, .001f);
					LengthStance = lengthStance;
				}
			}
			public float LengthStance
			{
				get
				{
					return lengthStance;
				}
				set
				{
					lengthStance = math.clamp(value, -Utility.Clamp01(1f - (.05f / length)), 1f);

					if (!Application.isPlaying && Settings.previewSuspensionAdjustmentsAtEditMode)
						vehicle.RefreshWheelsRenderers();
				}
			}
			public float Stiffness
			{
				get
				{
					return stiffness;
				}
				set
				{
					stiffness = math.max(value, 1f);
				}
			}
			public float Damper
			{
				get
				{
					return damper;
				}
				set
				{
					damper = math.max(value, 1f);
				}
			}
			public float Target
			{
				get
				{
					return target;
				}
				set
				{
					target = Utility.Clamp01(value);
				}
			}
			public float Camber
			{
				get
				{
					return camber;
				}
				set
				{
					camber = math.clamp(value, -Settings.maximumCamberAngle, Settings.maximumCamberAngle);

					if (!Application.isPlaying && Settings.previewSuspensionAdjustmentsAtEditMode)
						vehicle.RefreshWheelsRenderers();
				}
			}
			public float Caster
			{
				get
				{
					return caster;
				}
				set
				{
					caster = math.clamp(value, -Settings.maximumCasterAngle, Settings.maximumCasterAngle);

					if (!Application.isPlaying && Settings.previewSuspensionAdjustmentsAtEditMode)
						vehicle.RefreshWheelsRenderers();
				}
			}
			public float Toe
			{
				get
				{
					return toe;
				}
				set
				{
					toe = math.clamp(value, -Settings.maximumToeAngle, Settings.maximumToeAngle);

					if (!Application.isPlaying && Settings.previewSuspensionAdjustmentsAtEditMode)
						vehicle.RefreshWheelsRenderers();
				}
			}
			public float SideOffset
			{
				get
				{
					return sideOffset;
				}
				set
				{
					sideOffset = math.clamp(value, -Settings.maximumSideOffset, Settings.maximumSideOffset);

					if (!Application.isPlaying && Settings.previewSuspensionAdjustmentsAtEditMode)
						vehicle.RefreshWheelsRenderers();
				}
			}
			public bool IsValid
			{
				get
				{
					return vehicle;
				}
			}

			[SerializeField]
			private float length = .2f;
			[SerializeField]
			private float lengthStance;
			[SerializeField]
			private float stiffness = 30000f;
			[SerializeField]
			private float damper = 4500f;
			[SerializeField]
			private float target = .5f;
			[SerializeField]
			private float camber;
			[SerializeField]
			private float caster;
			[SerializeField]
			private float toe;
			[SerializeField]
			private float sideOffset;
			[SerializeField]
			private Vehicle vehicle;

			#endregion

			#region Constructors

			public SuspensionModule(Vehicle vehicle)
			{
				this.vehicle = vehicle;
			}

			internal SuspensionModule(Vehicle vehicle, SuspensionModule module) : this(vehicle)
			{
				if (module.length > 0)
					length = module.length;

				lengthStance = module.lengthStance;

				if (module.stiffness > 0)
					stiffness = module.stiffness;

				if (module.damper > 0)
					damper = module.damper;

				if (module.target > 0)
					target = module.target;

				if (module.camber != default)
					camber = module.camber;

				if (module.caster != default)
					caster = module.caster;

				if (module.toe != default)
					toe = module.toe;

				if (module.sideOffset != default)
					sideOffset = module.sideOffset;
			}

			#endregion

			#region Operators

			public static implicit operator bool(SuspensionModule suspension) => suspension != null;

			#endregion
		}
		[Serializable]
		[Obsolete("Use `VehicleTireCompound.WheelColliderFrictionCurve` instead.", true)]
		public struct FrictionModule
		{
			public float extremumSlip;
			public float extremumValue;
			public float asymptoteSlip;
			public float asymptoteValue;
			public float stiffness;
		}

		internal readonly struct WheelAccess
		{
			#region Variables

			public readonly bool IsFrontWheel => driveTrain == DriveTrain.Front;
			public readonly bool IsLeftWheel => position == Side.Left;
			public readonly bool IsRightWheel => position == Side.Right;
			public readonly Side position;
			public readonly DriveTrain driveTrain;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool isSteerWheel;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool isMotorWheel;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool hideRimEdgePerDefault;
			public readonly int rimEdgeMaterialIndex;
			public readonly int brakeDiscMaterialIndex;
			public readonly float radius;
			public readonly float rimRadius;
			public readonly float width;
			public readonly int diameter;
			public readonly int aspect;
			public readonly float height;
			public readonly VehicleTireCompound.WheelColliderFrictionCurve wheelColliderAccelerationFriction;
			public readonly VehicleTireCompound.WheelColliderFrictionCurve wheelColliderBrakeFriction;
			public readonly VehicleTireCompound.WheelColliderFrictionCurve wheelColliderSidewaysFriction;

			#endregion

			#region Constructors

			public WheelAccess(WheelModule module)
			{
				position = module.side;
				driveTrain = module.DriveTrain;
				isSteerWheel = module.IsSteerWheel;
				isMotorWheel = module.IsMotorWheel;
				hideRimEdgePerDefault = module.HideRimEdgePerDefault;
				rimEdgeMaterialIndex = module.RimEdgeMaterialIndex;
				brakeDiscMaterialIndex = module.BrakeDiscMaterialIndex;
				radius = module.Instance.Radius;
				rimRadius = module.Instance.RimRadius;
				width = module.Instance.Width;
				diameter = module.Instance.diameter;
				aspect = module.Instance.aspect;
				height = module.Instance.height;

				var tireCompound = module.Instance.TireCompound;

				wheelColliderAccelerationFriction = tireCompound.wheelColliderAccelerationFriction;
				wheelColliderBrakeFriction = tireCompound.wheelColliderBrakeFriction;
				wheelColliderSidewaysFriction = tireCompound.wheelColliderSidewaysFriction;
			}

			#endregion

			#region Operators

			public static implicit operator WheelAccess(WheelModule module) => new(module);

			#endregion
		}
		internal class SFXSheet
		{
			#region Variables

			public readonly AudioClip[] forwardSkidClips;
			public readonly AudioClip[] brakeSkidClips;
			public readonly AudioClip[] sidewaysSkidClips;
			public readonly AudioClip[] rollClips;
			public readonly AudioClip[] flatSkidClips;
			public readonly AudioClip[] flatRollClips;
			public readonly AudioClip brakingClip;
			public readonly AudioClip tireExplosionClip;

			private readonly VehicleWheel wheel;
			private readonly VehicleAudioSource[] forwardSkidSources;
			private readonly VehicleAudioSource[] brakeSkidSources;
			private readonly VehicleAudioSource[] sidewaysSkidSources;
			private readonly VehicleAudioSource[] rollSources;
			private readonly VehicleAudioSource[] flatSkidSources;
			private readonly VehicleAudioSource[] flatRollSources;
			private readonly VehicleAudioSource brakeSource;
			private VehicleAudioSource forwardSkidSource;
			private VehicleAudioSource brakeSkidSource;
			private VehicleAudioSource sidewaysSkidSource;
			private VehicleAudioSource rollSource;
			private VehicleAudioSource flatSkidSource;
			private VehicleAudioSource flatRollSource;
			private readonly VehicleAudioSource tireExplosionSource;
			private float forwardSlip;
			private float brakeSlip;
			private float sidewaysSlip;
			private int currentGround;

			#endregion

			#region Methods

			#region Utilities

			public void ExplodeTire()
			{
				if (!Settings.useDamage || !Settings.useWheelHealth)
					return;

				tireExplosionSource.source.volume = Settings.SFXVolume;

				tireExplosionSource.PlayOnceAndDisable();
			}

			#endregion

			#region Update

			public void Update()
			{
				if (HasInternalErrors || !wheel || !wheel.VehicleInstance)
					return;

				if (wheel.CurrentGroundIndex != currentGround)
				{
					if (currentGround > -1)
					{
						if (forwardSkidSources[currentGround] && forwardSkidSources[currentGround].source.isPlaying)
							forwardSkidSources[currentGround].PauseAndDisable();

						if (brakeSkidSources[currentGround] && brakeSkidSources[currentGround].source.isPlaying)
							brakeSkidSources[currentGround].PauseAndDisable();

						if (sidewaysSkidSources[currentGround] && sidewaysSkidSources[currentGround].source.isPlaying)
							sidewaysSkidSources[currentGround].PauseAndDisable();

						if (rollSources[currentGround] && rollSources[currentGround].source.isPlaying)
							rollSources[currentGround].PauseAndDisable();

						if (flatSkidSources[currentGround] && flatSkidSources[currentGround].source.isPlaying)
							flatSkidSources[currentGround].PauseAndDisable();

						if (flatSkidSources[currentGround] && flatSkidSources[currentGround].source.isPlaying)
							flatSkidSources[currentGround].PauseAndDisable();
					}

					currentGround = wheel.CurrentGroundIndex;

					if (currentGround > -1)
					{
						forwardSkidSource = forwardSkidSources[currentGround];
						brakeSkidSource = brakeSkidSources[currentGround];
						sidewaysSkidSource = sidewaysSkidSources[currentGround];
						rollSource = rollSources[currentGround];
						flatSkidSource = flatSkidSources[currentGround];
						flatRollSource = flatRollSources[currentGround];
					}
					else
					{
						forwardSkidSource = null;
						brakeSkidSource = null;
						sidewaysSkidSource = null;
						rollSource = null;
						flatSkidSource = null;
						flatRollSource = null;
					}
				}

				if (brakeSource)
				{
					brakeSource.source.volume = Utility.Clamp01(wheel.VehicleInstance.Inputs.Brake + wheel.VehicleInstance.Inputs.Handbrake) * Utility.Clamp01(math.abs(wheel.RPM) * .025f) * wheel.BrakeTorqueLoss * Settings.SFXVolume;

					if (Mathf.Approximately(brakeSource.source.volume, 0f))
					{
						if (brakeSource.source.isPlaying)
							brakeSource.PauseAndDisable();
					}
					else if (!brakeSource.source.isPlaying)
					{
						if (brakeSource.source.time > 0f)
							brakeSource.UnPauseAndEnable();
						else
							brakeSource.PlayAndEnable();
					}
				}

				if (currentGround < 0)
					return;

				if (wheel.TireHealth > 0f)
				{
					if (forwardSkidSource && !forwardSkidSource.source.isPlaying)
					{
						if (forwardSkidSource.source.time > 0f)
							forwardSkidSource.UnPauseAndEnable();
						else
							forwardSkidSource.PlayAndEnable();
					}

					if (brakeSkidSource && !brakeSkidSource.source.isPlaying)
					{
						if (brakeSkidSource.source.time > 0f)
							brakeSkidSource.UnPauseAndEnable();
						else
							brakeSkidSource.PlayAndEnable();
					}

					if (sidewaysSkidSource && !sidewaysSkidSource.source.isPlaying)
					{
						if (sidewaysSkidSource.source.time > 0f)
							sidewaysSkidSource.UnPauseAndEnable();
						else
							sidewaysSkidSource.PlayAndEnable();
					}

					if (rollSource && !rollSource.source.isPlaying)
					{
						if (rollSource.source.time > 0f)
							rollSource.UnPauseAndEnable();
						else
							rollSource.PlayAndEnable();
					}

					if (flatSkidSource && flatSkidSource.source.isPlaying)
						sidewaysSkidSource.PauseAndDisable();

					if (flatRollSource && flatRollSource.source.isPlaying)
						flatRollSource.PauseAndDisable();
				}
				else
				{
					if (forwardSkidSource && forwardSkidSource.source.isPlaying)
						forwardSkidSource.PauseAndDisable();

					if (brakeSkidSource && brakeSkidSource.source.isPlaying)
						brakeSkidSource.PauseAndDisable();

					if (sidewaysSkidSource && sidewaysSkidSource.source.isPlaying)
						sidewaysSkidSource.PauseAndDisable();

					if (rollSource && rollSource.source.isPlaying)
						rollSource.PauseAndDisable();

					if (flatSkidSource && !flatSkidSource.source.isPlaying)
					{
						if (flatSkidSource.source.time > 0f)
							flatSkidSource.UnPauseAndEnable();
						else
							flatSkidSource.PlayAndEnable();
					}

					if (flatRollSource && !flatRollSource.source.isPlaying)
					{
						if (flatRollSource.source.time > 0f)
							flatRollSource.UnPauseAndEnable();
						else
							flatRollSource.PlayAndEnable();
					}
				}

				float speedSign = Mathf.Sign(wheel.RPM);

				forwardSlip = Utility.InverseLerp(.05f, 1f, Utility.Clamp01(speedSign * wheel.HitInfo.forwardSlip));
				brakeSlip = Utility.InverseLerp(.05f, 1f, Utility.Clamp01(speedSign * -wheel.HitInfo.forwardSlip));
				sidewaysSlip = math.abs(wheel.HitInfo.sidewaysSlip);

				if (forwardSkidSource)
				{
					forwardSkidSource.source.volume = forwardSlip * Settings.SFXVolume * Settings.Grounds[currentGround].Volume;
					forwardSkidSource.source.pitch = forwardSlip;
				}

				if (brakeSkidSource)
				{
					brakeSkidSource.source.volume = brakeSlip * Settings.SFXVolume * Settings.Grounds[currentGround].Volume;
					brakeSkidSource.source.pitch = brakeSlip;
				}

				if (sidewaysSkidSource)
				{
					sidewaysSkidSource.source.volume = sidewaysSlip * Settings.SFXVolume * Settings.Grounds[currentGround].Volume;
					sidewaysSkidSource.source.pitch = Utility.Clamp01(sidewaysSlip / wheel.sidewaysExtremumSlip);
				}

				if (rollSource)
				{
					rollSource.source.volume = wheel.IsGrounded ? Utility.Lerp(Utility.Clamp01(math.abs(wheel.Speed) * .01f), 0f, wheel.totalSlipFactor) * Settings.SFXVolume * Settings.Grounds[currentGround].Volume : 0f;
					rollSource.source.pitch = 1f + forwardSlip;
				}

				if (flatSkidSource)
				{
					flatSkidSource.source.volume = Utility.Clamp01(wheel.totalSlipFactor) * Settings.SFXVolume * Settings.Grounds[currentGround].Volume;
					flatSkidSource.source.pitch = Utility.Clamp01(wheel.totalSlip);
				}

				if (flatRollSource)
				{
					flatRollSource.source.volume = wheel.IsGrounded ? Utility.Lerp(Utility.Clamp01(math.abs(wheel.Speed) * .02f), 0f, wheel.totalSlipFactor) * Settings.SFXVolume * Settings.Grounds[currentGround].Volume : 0f;
					flatRollSource.source.pitch = Utility.Clamp01(math.abs(wheel.Speed) * .1f);
				}
			}

			#endregion

			#endregion

			#region Constructors

			public SFXSheet(VehicleWheel wheel)
			{
				if (HasInternalErrors)
					return;

				this.wheel = wheel;

				if (!wheel || !wheel.VehicleInstance)
					return;

				currentGround = -1;
				brakingClip = Settings.brakeClip;
				tireExplosionClip = Settings.tireExplosionClip;
				brakeSource = VehicleAudio.NewAudioSource(wheel, "BrakeSFX", 4f, 10f, 0f, brakingClip, true, false, true, 0f, Settings.audioMixers.brakeEffects);
				tireExplosionSource = VehicleAudio.NewAudioSource(wheel, "TireExplosionSFX", 5f, 50f, Settings.SFXVolume, tireExplosionClip, false, false, true, 0f, Settings.audioMixers.wheelEffects);

				if (Settings.Grounds.Length < 1)
					return;

				string[] groundNames = Settings.GetGroundsNames(false);
				int groundsCount = Settings.Grounds.Length;

				forwardSkidClips = new AudioClip[groundsCount];
				brakeSkidClips = new AudioClip[groundsCount];
				sidewaysSkidClips = new AudioClip[groundsCount];
				rollClips = new AudioClip[groundsCount];
				flatSkidClips = new AudioClip[groundsCount];
				flatRollClips = new AudioClip[groundsCount];
				forwardSkidSources = new VehicleAudioSource[groundsCount];
				brakeSkidSources = new VehicleAudioSource[groundsCount];
				sidewaysSkidSources = new VehicleAudioSource[groundsCount];
				rollSources = new VehicleAudioSource[groundsCount];
				flatSkidSources = new VehicleAudioSource[groundsCount];
				flatRollSources = new VehicleAudioSource[groundsCount];

				for (int i = 0; i < groundsCount; i++)
				{
					forwardSkidClips[i] = Settings.Grounds[i].forwardSkidClip;
					brakeSkidClips[i] = Settings.Grounds[i].brakeSkidClip;
					sidewaysSkidClips[i] = Settings.Grounds[i].sidewaysSkidClip;
					rollClips[i] = Settings.Grounds[i].rollClip;
					flatSkidClips[i] = Settings.Grounds[i].flatSkidClip;
					flatRollClips[i] = Settings.Grounds[i].flatRollClip;

					if (forwardSkidClips[i])
						forwardSkidSources[i] = VehicleAudio.NewAudioSource(
							 wheel, $"{groundNames[i]}ForwardSkidSFX", 5f, 35f, 0f, forwardSkidClips[i],
							 true, false, true, 0f, Settings.audioMixers.wheelEffects
						);

					if (brakeSkidClips[i])
						brakeSkidSources[i] = VehicleAudio.NewAudioSource(
							 wheel, $"{groundNames[i]}BrakingSkidSFX", 5f, 35f, 0f, brakeSkidClips[i],
							 true, false, true, 0f, Settings.audioMixers.wheelEffects
						);

					if (sidewaysSkidClips[i])
						sidewaysSkidSources[i] = VehicleAudio.NewAudioSource(
							wheel, $"{groundNames[i]}SidewaysSkidSFX", 5f, 35f, 0f, sidewaysSkidClips[i],
							true, false, true, 0f, Settings.audioMixers.wheelEffects
						);

					if (rollClips[i])
						rollSources[i] = VehicleAudio.NewAudioSource(
							wheel, $"{groundNames[i]}RollSFX", 5f, 35f, 0f, rollClips[i],
							true, false, true, 0f, Settings.audioMixers.wheelEffects
						);

					if (flatSkidClips[i])
						flatSkidSources[i] = VehicleAudio.NewAudioSource(
							wheel, $"{groundNames[i]}FlatSkidSFX", 5f, 35f, 0f, flatSkidClips[i],
							true, false, true, 0f, Settings.audioMixers.wheelEffects
						);

					if (flatRollClips[i])
						flatRollSources[i] = VehicleAudio.NewAudioSource(
							wheel, $"{groundNames[i]}FlatRollSFX", 5f, 35f, 0f, flatRollClips[i],
							true, false, true, 0f, Settings.audioMixers.wheelEffects
						);
				}

				wheel.SFXParent = wheel.transform.Find("SoundEffects");
			}

			#endregion
		}
		internal class VFXSheet
		{
			#region Variables

			public readonly ParticleSystem[] particleSystems;
			public readonly ParticleSystem[] damageParticleSystems;
			public VehicleWheelMark[] groundMarks;

			private readonly VehicleWheel wheel;
			private readonly float[] orgEmissionsTimeRates;
			private readonly float[] orgEmissionsDistanceRates;
			private readonly float[] orgDamageEmissionsTimeRates;
			private readonly float[] orgDamageEmissionsDistanceRates;
			private VehicleGroundMapper.GroundModule ground;
			private ParticleSystem particleSystem;
			private ParticleSystem tireExplosion;
			private ParticleSystemForceField tireExplosionForce;
			private ParticleSystem damageParticleSystemLeft;
			private ParticleSystem damageParticleSystemRight;
			private ParticleSystem.EmissionModule emission;
			private ParticleSystem.VelocityOverLifetimeModule velocity;
			private float emissionMultiplier;
			private float lastTireHealth;
			private bool damageStateChanged;
			private bool groundChanged;
			private int currentGround;
			private int currentGroundMark;

			#endregion

			#region Methods

			#region Utilities

			public void ExplodeTire()
			{
				if (!Settings.useParticleSystems || !Settings.useDamage || !Settings.useWheelHealth || !Settings.tireExplosion || !wheel || !wheel.Module.Model)
					return;

				tireExplosion = VehicleVisuals.NewParticleSystem(wheel, Settings.tireExplosion, "TireExplosionEffect", wheel.Module.Model.position, wheel.Module.Model.rotation, false, true, true);

				wheel.StartCoroutine(VehicleVisuals.DestroyParticleSystem(tireExplosion));

				if (Settings.tireExplosionForceField)
				{
					tireExplosionForce = VehicleVisuals.NewParticleSystemForceField(wheel, Settings.tireExplosionForceField, "ExplosionForce", wheel.Module.Model.position, wheel.Module.Model.rotation);

					wheel.StartCoroutine(VehicleVisuals.DestroyParticleSystemForceField(tireExplosionForce, new WaitWhile(() => tireExplosion)));
				}
			}

			#endregion

			#region Update

			public void Update()
			{
				if (HasInternalErrors || !wheel || !wheel.vehicleInstance)
					return;

				groundChanged = wheel.CurrentGroundIndex != currentGround;

				if (groundChanged)
				{
					if (currentGround > -1)
					{
						if (particleSystem && particleSystem.isPlaying)
							particleSystem.Stop(true);

						if (damageParticleSystemLeft && damageParticleSystemLeft.isPlaying)
							damageParticleSystemLeft.Stop(true);

						if (damageParticleSystemRight && damageParticleSystemRight.isPlaying)
							damageParticleSystemRight.Stop(true);
					}

					currentGround = wheel.CurrentGroundIndex;
					currentGroundMark = -1;

					if (currentGround > -1)
					{
						if (Settings.useParticleSystems)
						{
							particleSystem = particleSystems[currentGround];

							if (damageParticleSystems != null)
							{
								damageParticleSystemLeft = damageParticleSystems[currentGround * 2];
								damageParticleSystemRight = damageParticleSystems[currentGround * 2 + 1];
							}
						}

						ground = Settings.Grounds[currentGround];
					}
					else
					{
						particleSystem = null;
						damageParticleSystemLeft = null;
						damageParticleSystemRight = null;
						ground = default;
					}
				}

				damageStateChanged = wheel.TireHealth <= 0f && lastTireHealth > 0f || lastTireHealth <= 0f && wheel.TireHealth > 0f;
				lastTireHealth = wheel.TireHealth;

				if (currentGround > -1 && Settings.useParticleSystems)
				{
					if (particleSystem)
					{
						emissionMultiplier = Utility.Clamp01(ground.useSpeedEmission ? wheel.totalSlip + (math.abs(wheel.Speed) / 80f) : wheel.totalSlip * wheel.TireTemperature);
						emission = particleSystem.emission;
						emission.rateOverTimeMultiplier = orgEmissionsTimeRates[currentGround] * emissionMultiplier;
						emission.rateOverDistanceMultiplier = orgEmissionsDistanceRates[currentGround] * emissionMultiplier;

						if (!ground.useSpeedEmission && (wheel.TireHealth <= 0f || wheel.TireTemperature < wheel.averageExtremumSlip || wheel.totalSlip < wheel.averageExtremumSlip))
							particleSystem.Stop(true);
						else if (!particleSystem.isPlaying)
							particleSystem.Play();
					}

					if (damageParticleSystems != null && damageParticleSystemLeft && damageParticleSystemRight)
					{
						emissionMultiplier = wheel.totalSlip;
						emission = damageParticleSystemLeft.emission;
						emission.enabled = true;
						emission.rateOverTimeMultiplier = orgEmissionsTimeRates[currentGround] * emissionMultiplier;
						emission.rateOverDistanceMultiplier = orgEmissionsDistanceRates[currentGround] * emissionMultiplier;
						emission = damageParticleSystemRight.emission;
						emission.enabled = true;
						emission.rateOverTimeMultiplier = orgEmissionsTimeRates[currentGround] * emissionMultiplier;
						emission.rateOverDistanceMultiplier = orgEmissionsDistanceRates[currentGround] * emissionMultiplier;
						velocity = damageParticleSystemLeft.velocityOverLifetime;
						velocity.enabled = true;
						velocity.space = ParticleSystemSimulationSpace.Local;
						velocity.orbitalOffsetXMultiplier = 0f;
						velocity.orbitalOffsetYMultiplier = 0f;
						velocity.orbitalOffsetZMultiplier = 0f;
						velocity.orbitalXMultiplier = 0f;
						velocity.orbitalYMultiplier = 0f;
						velocity.orbitalZMultiplier = 0f;
						velocity.radialMultiplier = 0f;
						velocity.xMultiplier = -1f * wheel.LocalVelocity.x;
						velocity.yMultiplier = -1f * wheel.LocalVelocity.y;
						velocity.zMultiplier = -1f * wheel.LocalVelocity.z;
						velocity.speedModifier = 1f;
						velocity = damageParticleSystemRight.velocityOverLifetime;
						velocity.enabled = true;
						velocity.space = ParticleSystemSimulationSpace.Local;
						velocity.orbitalOffsetXMultiplier = 0f;
						velocity.orbitalOffsetYMultiplier = 0f;
						velocity.orbitalOffsetZMultiplier = 0f;
						velocity.orbitalXMultiplier = 0f;
						velocity.orbitalYMultiplier = 0f;
						velocity.orbitalZMultiplier = 0f;
						velocity.xMultiplier = 0f;
						velocity.yMultiplier = 0f;
						velocity.zMultiplier = -1f * Mathf.Sign(wheel.RPM);
						velocity.speedModifier = 1f;

						if (wheel.TireHealth > 0f || emissionMultiplier <= 0f)
							damageParticleSystemLeft.Stop(true);
						else if (!damageParticleSystemLeft.isPlaying)
							damageParticleSystemLeft.Play();

						if (wheel.TireHealth > 0f || emissionMultiplier <= 0f)
							damageParticleSystemRight.Stop(true);
						else if (!damageParticleSystemRight.isPlaying)
							damageParticleSystemRight.Play();

						damageParticleSystemLeft.transform.SetPositionAndRotation(wheel.position, wheel.transform.rotation * Quaternion.Euler(0f, wheel.CurrentSteerAngle - wheel.ToeAngle, 0f));
						damageParticleSystemRight.transform.SetPositionAndRotation(wheel.position, wheel.transform.rotation * Quaternion.Euler(0f, wheel.CurrentSteerAngle - wheel.ToeAngle, 0f));

#if !MVC_COMMUNITY
						if (ToolkitSettings.UsingWheelColliderPhysics && Settings.useSuspensionAdjustments)
						{
							damageParticleSystemLeft.transform.RotateAround(wheel.transform.position + .5f * wheel.Radius * wheel.transform.up, wheel.VehicleInstance.transform.forward, wheel.CamberAngle);
							damageParticleSystemRight.transform.RotateAround(wheel.transform.position + .5f * wheel.Radius * wheel.transform.up, wheel.VehicleInstance.transform.forward, wheel.CamberAngle);
						}
#endif

						damageParticleSystemLeft.transform.position += (.02f - wheel.RimRadius) * damageParticleSystemLeft.transform.up - .5f * wheel.Width * damageParticleSystemLeft.transform.right;
						damageParticleSystemRight.transform.position += (.02f - wheel.RimRadius) * damageParticleSystemRight.transform.up + .5f * wheel.Width * damageParticleSystemRight.transform.right;
					}
				}

				if (Settings.useWheelMarks)
				{
					float absForwardSlip = math.abs(wheel.LateHitInfo.forwardSlip);
					float absSidewaysSlip = math.abs(wheel.LateHitInfo.sidewaysSlip);
					bool groundUseSpeedEmission = currentGround > -1 && Settings.Grounds[currentGround].useSpeedEmission;

					if ((currentGround > -1 && wheel.LateHitInfo.point != default && (absForwardSlip >= wheel.forwardExtremumSlip || absSidewaysSlip >= wheel.sidewaysExtremumSlip) || groundUseSpeedEmission) && !damageStateChanged)
					{
						if (currentGroundMark < 0)
							AddGroundMark();
						else
						{
							float forwardSlipFactor = Utility.InverseLerpUnclamped(wheel.forwardExtremumSlip, 1f, absForwardSlip);
							float sidewaysSlipFactor = Utility.InverseLerpUnclamped(wheel.sidewaysExtremumSlip, 1f, absSidewaysSlip);

							if (!UpdateGroundMark(groundUseSpeedEmission ? 1f : math.max(forwardSlipFactor, sidewaysSlipFactor)))
								EndGroundMark();
						}
					}
					else
						EndGroundMark();
				}
			}

			private void AddGroundMark()
			{
				currentGroundMark = groundMarks.Length;

				if (currentGroundMark < 1 || currentGroundMark > 0 && groundMarks[currentGroundMark - 1])
					Array.Resize(ref groundMarks, currentGroundMark + 1);

				if (currentGroundMark < 0 || currentGroundMark >= groundMarks.Length)
					return;

				if (groundMarks[currentGroundMark])
					groundMarks[currentGroundMark].UpdateMark(wheel, 0f);
				else
					groundMarks[currentGroundMark] = VehicleWheelMark.CreateNew(wheel, 0f);
			}
			private bool UpdateGroundMark(float opacity)
			{
				if (currentGroundMark < 0 || currentGroundMark >= groundMarks.Length || !groundMarks[currentGroundMark])
					return false;

				return groundMarks[currentGroundMark].UpdateMark(wheel, opacity);
			}
			private void EndGroundMark()
			{
				if (currentGroundMark < 0 || currentGroundMark >= groundMarks.Length)
					return;

				if (groundMarks[currentGroundMark])
				{
					groundMarks[currentGroundMark].UpdateMark(wheel, 0f);

					if (groundMarks[currentGroundMark].AnchorsCount < 3)
						groundMarks[currentGroundMark].renderer.enabled = false;
				}

				currentGroundMark = -1;
			}

			#endregion

			#endregion

			#region Constructors

			public VFXSheet(VehicleWheel wheel)
			{
				if (HasInternalErrors)
					return;

				this.wheel = wheel;

				if (!wheel || !wheel.VehicleInstance)
					return;

				currentGround = -1;
				currentGroundMark = -1;

				int groundsCount = Settings.Grounds.Length;

				if (groundsCount < 1)
					return;

				string[] groundNames = Settings.GetGroundsNames(false);

				lastTireHealth = wheel.TireHealth;
				groundMarks = new VehicleWheelMark[] { };

				if (Settings.useParticleSystems)
				{
					particleSystems = new ParticleSystem[groundsCount];
					damageParticleSystems = Settings.useDamage && Settings.useWheelHealth ? new ParticleSystem[groundsCount * 2] : null;
					orgEmissionsTimeRates = new float[groundsCount];
					orgEmissionsDistanceRates = new float[groundsCount];
					orgDamageEmissionsTimeRates = Settings.useDamage && Settings.useWheelHealth ? new float[groundsCount * 2] : null;
					orgDamageEmissionsDistanceRates = Settings.useDamage && Settings.useWheelHealth ? new float[groundsCount * 2] : null;

					for (int i = 0; i < groundsCount; i++)
					{
						if (Settings.Grounds[i].particleEffect)
						{
							particleSystems[i] = VehicleVisuals.NewParticleSystem(wheel, Settings.Grounds[i].particleEffect, $"{groundNames[i]}ParticleEffect", wheel.Module.Model.position, wheel.Module.Model.rotation, true, false, true);
							orgEmissionsTimeRates[i] = particleSystems[i].emission.rateOverTimeMultiplier;
							orgEmissionsDistanceRates[i] = particleSystems[i].emission.rateOverDistanceMultiplier;
						}

						if (damageParticleSystems != null && Settings.Grounds[i].flatWheelParticleEffect)
						{
							damageParticleSystems[i * 2] = VehicleVisuals.NewParticleSystem(wheel, Settings.Grounds[i].flatWheelParticleEffect, $"{groundNames[i]}DamageParticleEffect_1", wheel.Module.Model.position, wheel.Module.Model.rotation, true, false, true);
							damageParticleSystems[i * 2 + 1] = VehicleVisuals.NewParticleSystem(wheel, Settings.Grounds[i].flatWheelParticleEffect, $"{groundNames[i]}DamageParticleEffect_2", wheel.Module.Model.position, wheel.Module.Model.rotation, true, false, true);
							orgDamageEmissionsTimeRates[i * 2] = damageParticleSystems[i * 2].emission.rateOverTimeMultiplier;
							orgDamageEmissionsTimeRates[i * 2 + 1] = damageParticleSystems[i * 2 + 1].emission.rateOverTimeMultiplier;
							orgDamageEmissionsDistanceRates[i * 2] = damageParticleSystems[i * 2].emission.rateOverDistanceMultiplier;
							orgDamageEmissionsDistanceRates[i * 2 + 1] = damageParticleSystems[i * 2 + 1].emission.rateOverDistanceMultiplier;
						}
					}

					wheel.VFXParent = wheel.transform.Find("VisualEffects");
				}
			}

			#endregion
		}
		internal readonly struct WheelStatsAccess
		{
			#region Variables

			public readonly float3 position;
			public readonly quaternion rotation;
			public readonly float3 scale;
			public readonly float3 forwardDir;
			public readonly float3 sidewaysDir;
			public readonly float3 hitPoint;
			public readonly float3 localVelocity;
			public readonly float currentRadius;
			public readonly float rpm;
			public readonly float speed;
			public readonly float camberAngle;
			public readonly float casterAngle;
			public readonly float toeAngle;
			public readonly float motorTorque;
			public readonly float brakeTorque;
			public readonly float tireHealth;
			public readonly float tireTemperature;
			public readonly float rimTemperature;
			public readonly float brakeTemperature;
			public readonly float brakeTorqueLoss;
			public readonly float forwardFrictionStiffness;
			public readonly float sidewaysFrictionStiffness;
			public readonly float forwardSlip;
			public readonly float sidewaysSlip;
			public readonly int currentGroundIndex;
			public readonly int clampedCurrentGroundIndex;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool isGrounded;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool isOffRoading;
			public readonly VehicleTireCompound.WheelColliderFrictionCurve currentWheelColliderForwardFriction;
			public readonly VehicleTireCompound.WheelColliderFrictionCurve currentWheelColliderSidewaysFriction;

			#endregion

			#region Constructors

			public WheelStatsAccess(WheelModule module)
			{
				position = module.Instance.transform.position;
				rotation = module.Instance.transform.rotation;
				scale = module.Instance.transform.lossyScale;
				forwardDir = module.Instance.HitInfo.forwardDir;
				sidewaysDir = module.Instance.HitInfo.sidewaysDir;
				hitPoint = module.Instance.HitInfo.point;
				localVelocity = module.Instance.LocalVelocity;
				currentRadius = module.Instance.CurrentRadius;
				rpm = module.Instance.RPM;
				speed = module.Instance.Speed;
				camberAngle = module.Instance.CamberAngle;
				casterAngle = module.Instance.CasterAngle;
				toeAngle = module.Instance.ToeAngle;
				motorTorque = module.Instance.motorTorque;
				brakeTorque = module.Instance.brakeTorque;
				tireHealth = module.Instance.TireHealth;
				tireTemperature = module.Instance.TireTemperature;
				rimTemperature = module.Instance.RimTemperature;
				brakeTemperature = module.Instance.BrakeTemperature;
				brakeTorqueLoss = module.Instance.BrakeTorqueLoss;
				forwardFrictionStiffness = module.Instance.CurrentWheelColliderForwardFrictionStiffness;
				sidewaysFrictionStiffness = module.Instance.CurrentWheelColliderSidewaysFrictionStiffness;
				forwardSlip = module.Instance.HitInfo.forwardSlip;
				sidewaysSlip = module.Instance.HitInfo.sidewaysSlip;
				currentGroundIndex = module.Instance.CurrentGroundIndex;
				clampedCurrentGroundIndex = module.Instance.ClampedCurrentGroundIndex;
				isGrounded = module.Instance.IsGrounded;
				isOffRoading = module.Instance.IsOffRoading;
				currentWheelColliderForwardFriction = module.Instance.CurrentWheelColliderForwardFriction;
				currentWheelColliderSidewaysFriction = module.Instance.CurrentWheelColliderSidewaysFriction;
			}

			#endregion

			#region Operators

			public static implicit operator WheelStatsAccess(WheelModule module) => new(module);

			#endregion
		}

		#endregion

		#region Constants

		private const float MinDriftForwardFrictionMultiplier = .75f;
		private const float MaxDriftForwardFrictionMultiplier = 1.25f;
		private const float MinDriftSidewaysFrictionMultiplier = .45f;
		private const float MaxDriftSidewaysFrictionMultiplier = 1f;

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
		public VehicleTrailer TrailerInstance
		{
			get
			{
				if (!trailerInstance)
					trailerInstance = GetComponentInParent<VehicleTrailer>();

				return trailerInstance;
			}
		}
		public WheelModule Module
		{
			get
			{
				if (!VehicleInstance)
					return null;

				return VehicleInstance.Wheels[ModuleIndex];
			}
			set
			{
				if (!VehicleInstance || VehicleInstance.Wheels == null || VehicleInstance.Wheels.Length < 1 || ModuleIndex < 0)
					return;

				VehicleInstance.Wheels[ModuleIndex] = value;
			}
		}
		public int ModuleIndex
		{
			get
			{
				if (!VehicleInstance)
					return -1;

				if (moduleIndex < 0 || moduleIndex >= VehicleInstance.Wheels.Length)
					moduleIndex = VehicleInstance.Wheels != null && VehicleInstance.Wheels.Length > 0 ? Array.IndexOf(VehicleInstance.Wheels, Array.Find(VehicleInstance.Wheels, wheel => wheel.Instance == this)) : -1;

				return moduleIndex;
			}
		}
		public SuspensionModule Suspension
		{
			get
			{
				if (!VehicleInstance || !Module)
					return null;

				return suspension = Module.IsFrontWheel && !TrailerInstance ? VehicleInstance.FrontSuspension : VehicleInstance.RearSuspension;
			}
		}
		public float mass = 40f;
		public float tireThickness = 3f;
		public VehicleTireCompound TireCompound
		{
			get
			{
				var tireCompounds = Settings.TireCompounds;

				if (HasInternalErrors || tireCompoundIndex > tireCompounds.Length)
					tireCompoundIndex = -1;

				if (TireCompoundIndex < 0)
					tireCompound = null;
				else if (!tireCompound || tireCompound != tireCompounds[tireCompoundIndex])
					tireCompound = tireCompounds[tireCompoundIndex];

				return tireCompound;
			}
		}
		public int TireCompoundIndex
		{
			get
			{
				var tireCompounds = Settings.TireCompounds;

				if (tireCompoundIndex > -1 && tireCompoundIndex < tireCompounds.Length && tireCompoundName.IsNullOrEmpty())
					tireCompoundName = tireCompounds[tireCompoundIndex].Name;
				else if (!tireCompoundName.IsNullOrEmpty() && (!tireCompound || tireCompound && tireCompound.Name != tireCompoundName))
				{
					VehicleTireCompound tireCompound = tireCompounds.FirstOrDefault(compound => compound.Name == tireCompoundName);

					if (tireCompound)
					{
						tireCompoundIndex = Array.IndexOf(tireCompounds, tireCompound);
						this.tireCompound = tireCompound;
					}
					else
					{
						tireCompoundIndex = -1;
						this.tireCompound = null;
						tireCompoundName = string.Empty;
					}
				}

				return tireCompoundIndex;
			}
			set
			{
				if (HasInternalErrors || tireCompoundIndex == value || value < 0)
					return;

				var tireCompounds = Settings.TireCompounds;

				if (tireCompoundIndex >= tireCompounds.Length)
					return;

				tireCompound = tireCompounds[value];
				tireCompoundName = tireCompound?.Name;
				tireCompoundIndex = value;
			}
		}
		public VehicleTireCompound.WheelColliderFrictionCurve CurrentWheelColliderForwardFriction;
		public VehicleTireCompound.WheelColliderFrictionCurve CurrentWheelColliderSidewaysFriction;
		[Obsolete("Use `TireCompound.wheelColliderAccelerationFriction` or `TireCompound.wheelColliderBrakeFriction` instead.")]
		public WheelFrictionCurve forwardFrictionCurve = new()
		{
			extremumSlip = .2f,
			extremumValue = 1f,
			asymptoteSlip = .8f,
			asymptoteValue = .75f,
			stiffness = 1.5f
		};
		[Obsolete("Use `TireCompound.wheelColliderSidewaysFriction` instead.")]
		public WheelFrictionCurve sidewaysFrictionCurve = new()
		{
			extremumSlip = .25f,
			extremumValue = 1f,
			asymptoteSlip = .5f,
			asymptoteValue = .75f,
			stiffness = 1.5f
		};
		public float motorTorque;
		public float brakeTorque;
		public float steerAngle;
		public float CurrentSteerAngle;
		public float CurrentFakeSteerAngle;
		public VehicleGroundMapper GroundMapper;
		public Collider GroundCollider;
		public WheelHit HitInfo;
		public float3 LocalVelocity;
		public float3 RelativeVelocity;
		public float3 LocalAngularVelocity;
		public WheelHit LateHitInfo;
		public float3 LateLocalVelocity;
		public float3 LateRelativeVelocity;
		public float3 LateLocalAngularVelocity;
		public float CurrentRadius;
		public float Radius
		{
			get
			{
				if (!Module)
					return default;

				if (radius <= 0f)
					radius = Module.Bounds.extents.y;

				return radius;
			}
		}
		public float RimRadius
		{
			get
			{
				if (!Module)
					return default;

				if (diameter <= 0f)
					diameter = (int)math.round(Module.RimBounds.size.y * 100f * Utility.UnitMultiplier(Utility.Units.Size, Utility.UnitType.Imperial));

				return diameter * .005f / Utility.UnitMultiplier(Utility.Units.Size, Utility.UnitType.Imperial);
			}
		}
		public float Width
		{
			get
			{
				if (!Module)
					return default;

				if (width <= 0f)
					RefreshWidth();

				return width;
			}
			set
			{
				if (!Module)
					return;

				width = math.max(value, .001f);
				height = Aspect * Width * .01f;
			}
		}
		public int Diameter
		{
			get
			{
				if (!Module)
					return 0;

				if (diameter <= 0)
					diameter = (int)math.round(Module.RimBounds.size.y * 100f * Utility.UnitMultiplier(Utility.Units.Size, Utility.UnitType.Imperial));

				return diameter;
			}
			set
			{
				diameter = math.max(value, 1);
			}
		}
		public int Aspect
		{
			get
			{
				if (!Module)
					return 0;

				if (aspect <= 0)
					aspect = (int)math.round(100 * Height / Width);

				return aspect;
			}
			set
			{
				if (!Module)
					return;

				aspect = math.max(value, 0);
				height = Aspect * Width * .01f;
			}
		}
		public float Height
		{
			get
			{
				if (height <= 0f)
					height = Radius - RimRadius;

				return height;
			}
			set
			{
				height = math.max(value, 0f);
				aspect = (int)math.round(Height * 100 / Width);
			}
		}
		public float Inertia
		{
			get
			{
				return .5f * mass * math.lengthsq(CurrentRadius);
			}
		}
		public float RPM;
		public float AngularVelocity;
		public float Speed;
		public float CamberAngle;
		public float CasterAngle;
		public float ToeAngle;
		public float TireHealth
		{
			get
			{
				return tireHealth;
			}
			private set
			{
				tireHealth = value;
				CurrentRadius = TireHealth > 0f ? Radius - Utility.Lerp(tireThickness, 0f, TireHealth) * .01f : RimRadius;

				if (Module.Tire)
					Module.Tire.gameObject.SetActive(!Settings.useDamage || !Settings.useWheelHealth || TireHealth > 0f);
			}
		}
		public float TireTemperature;
		public float RimTemperature;
		public float CurrentWheelColliderForwardFrictionStiffness
		{
			get
			{
				if (wheelCollider)
					return wheelCollider.forwardFriction.stiffness;

				return default;
			}
		}
		public float CurrentWheelColliderSidewaysFrictionStiffness
		{
			get
			{
				if (wheelCollider)
					return wheelCollider.sidewaysFriction.stiffness;

				return default;
			}
		}
		[Obsolete("Use `CurrentWheelColliderForwardFrictionStiffness` instead.")]
		public float ForwardFrictionStiffness => CurrentWheelColliderForwardFrictionStiffness;
		[Obsolete("Use `CurrentWheelColliderSidewaysFrictionStiffness` instead.")]
		public float SidewaysFrictionStiffness => CurrentWheelColliderSidewaysFrictionStiffness;
		public bool IsGrounded;
		public bool IsOffRoading;
		public int CurrentGroundIndex;
		public int CurrentGroundMapIndex;
		public int ClampedCurrentGroundIndex;
		public int ClampedCurrentGroundMapIndex;
		public float BrakeTemperature;
		public float BrakeTorqueLoss;

		internal VehicleTireCompound tireCompound;
		internal WheelCollider wheelCollider;
		internal SFXSheet sfx;
		internal VFXSheet vfx;

		private Vehicle vehicleInstance;
		private VehicleTrailer trailerInstance;
		private int moduleIndex = -1;
		private Transform VFXParent;
		private Transform SFXParent;
		private Vehicle.BrakeModule brakeModule;
		private VehicleWheelSpin BrakeDiscSpinModule
		{
			get
			{
				if (!Module)
					return null;

				return brakeDiscSpinModule;
			}
		}
		private VehicleWheelSpin brakeDiscSpinModule;
		private Material rimEdgeMaterial;
		private Material brakeDiscMaterial;
		private SuspensionModule suspension;
		private VehicleWheelSpin.SpinMaterialModule brakeDiscSpinMaterialModule;
		private List<VehicleDamageZone> damageZones;
		private Collider[] explosionHits;
		private WheelFrictionCurve tempTargetWheelFrictionCurve;
		private WheelFrictionCurve tempDriftWheelFrictionCurve;
		private quaternion rotation;
		private float3 position;
		private float3 newRimScale;
		private float3 newTireScale;
		private float3 newBrakeDiscScale;
		private float sqrRelativeVelocity;
#if !MVC_COMMUNITY
		private float[] groundsStrengths;
#endif
		private float wheelRotationFromRPM;
		private string rimMaterialEmissionColorPropertyName;
		[SerializeField]
		private float radius;
		[SerializeField]
		private float width;
		[SerializeField]
		private int aspect;
		[SerializeField]
		private float height;
		[SerializeField]
		private int diameter;
		[SerializeField]
		private int tireCompoundIndex;
		[SerializeField]
		private string tireCompoundName;
		private float tireHealth;
		private float slipInertia;
		private float dampingRate;
		private float totalSlip;
		private float totalSlipFactor;
		private float averageExtremumSlip;
		private float forwardExtremumSlip;
		private float sidewaysExtremumSlip;
		private float forwardSmoothSlip;
		private float sidewaysSmoothSlip;
		private float forwardFrictionStiffnessMultiplier = 1f;
		private float sidewaysFrictionStiffnessMultiplier = 1f;
		private float damageFactor;
		private float newTireHealth;
		private float slipFactor;
		private float deltaTime;
		private float brakeTemperature;
		private float brakeIntensity;
		private float donutIntensity;
		private float donutDirection;
#if !MVC_COMMUNITY
		private float sideOffset;
#endif
		private bool hasResetWheel;
		private bool updated;
		private int wheelPosition;

		#endregion

		#region Methods

		#region Utilities

		public void ResetWheel()
		{
			if (HasInternalErrors)
				return;

			hasResetWheel = true;
		}
		public void MultiplyForwardFrictionStiffness(float stiffness)
		{
			forwardFrictionStiffnessMultiplier *= math.abs(stiffness);
		}
		public void MultiplySidewaysFrictionStiffness(float stiffness)
		{
			sidewaysFrictionStiffnessMultiplier *= math.abs(stiffness);
		}
		public void DamageWheel()
		{
			TireHealth = 0f;
		}
		public void RepairWheel()
		{
			TireHealth = 1f;
		}
		public void RefreshTireThickness()
		{
			if (!Module || !Module.Model || !Module.Rim)
				return;

			tireThickness = (Radius - RimRadius) * 100f / 3f;
		}
		public void RefreshWidth()
		{
			if (!Module || !Module.Model)
				return;

			width = Module.TireBounds.size.x;
		}
		public string GetRimMaterialEmissionColorPropertyName()
		{
			if (!Module || !Module.RimEdgeRenderer || !Module.RimEdgeRenderer.sharedMaterials[Module.RimEdgeMaterialIndex] || !Module.RimEdgeRenderer.sharedMaterials[Module.RimEdgeMaterialIndex].shader)
				return string.Empty;

			if (rimMaterialEmissionColorPropertyName.IsNullOrEmpty())
			{
				if (Module.RimEdgeRenderer.sharedMaterials[Module.RimEdgeMaterialIndex].shader.name.StartsWith("HDRP/Lit"))
					rimMaterialEmissionColorPropertyName = "_EmissiveColor";
				else if (Module.RimEdgeRenderer.sharedMaterials[Module.RimEdgeMaterialIndex].shader.name.StartsWith("Universal Render Pipeline/"))
					rimMaterialEmissionColorPropertyName = "_EmissionColor";
				else if (Module.RimEdgeRenderer.sharedMaterials[Module.RimEdgeMaterialIndex].shader.name.StartsWith("Standard") || Module.RimEdgeRenderer.sharedMaterials[Module.RimEdgeMaterialIndex].shader.name.Contains("LightweightPipeline/Particles/Standard"))
					rimMaterialEmissionColorPropertyName = "_EmissionColor";
				else if (Module.RimEdgeRenderer.sharedMaterials[Module.RimEdgeMaterialIndex].shader.name.StartsWith("Particles/Standard") || Module.RimEdgeRenderer.sharedMaterials[Module.RimEdgeMaterialIndex].shader.name.Contains("LightweightPipeline/Particles/Standard"))
					rimMaterialEmissionColorPropertyName = "_Color";
				else if (!HasInternalErrors)
					rimMaterialEmissionColorPropertyName = Settings.customRimShaderEmissionColorProperty;
			}

			return rimMaterialEmissionColorPropertyName;
		}
		public void RefreshRimMaterialEmissionColorPropertyName()
		{
			rimMaterialEmissionColorPropertyName = string.Empty;
		}
		public void RefreshRenderers()
		{
			if (Application.isPlaying)
				return;

			Angles();
			Renderers();
		}

		internal void AddDamageZone(VehicleDamageZone zone)
		{
			if (damageZones.IndexOf(zone) < 0)
				damageZones.Add(zone);
		}
		internal void RemoveDamageZone(VehicleDamageZone zone)
		{
			damageZones.Remove(zone);
		}

		private WheelHit GetWheelHit()
		{
			WheelHit hit = default;

			switch (Settings.Physics)
			{
				default:
					if (wheelCollider)
						IsGrounded = wheelCollider.GetGroundHit(out hit);

					break;
			}

			return hit;
		}

		#endregion

		#region Awake

		public void Restart()
		{
			Awaken = false;

			Initialize();

			if (HasInternalErrors || !IsSetupDone)
				return;

			if (!VehicleInstance || !Module)
			{
				if (!VehicleInstance)
					throw new NullReferenceException($"No Vehicle Component found in parent! Vehicle Wheel instance \"{name}\" cannot be executed.");
				else
					throw new NullReferenceException($"No wheel module could be found in the Vehicle wheels list! Vehicle Wheel instance \"{name}\" cannot be executed.");
			}

			TireHealth = 1f;
			wheelCollider = GetComponentInChildren<WheelCollider>(true);

			if (!wheelCollider)
			{
				GameObject colliderGameObject = new("WheelCollider");

				colliderGameObject.transform.SetParent(transform, false);

				wheelCollider = colliderGameObject.AddComponent<WheelCollider>();

				if (Settings.useHideFlags)
					colliderGameObject.hideFlags = HideFlags.HideInHierarchy;
			}

			wheelCollider.radius = Radius;
			wheelCollider.forceAppPointDistance = 0f;
			suspension = Suspension;
			suspension.Camber = suspension.Camber;
			suspension.Caster = suspension.Caster;
			suspension.Toe = suspension.Toe;

			if (Module.IsTrailerWheel)
				brakeModule = TrailerInstance.Brakes;
			else
				brakeModule = Module.IsFrontWheel ? VehicleInstance.Behaviour.FrontBrakes : VehicleInstance.Behaviour.RearBrakes;

			Calculations();

			if (Module.Rim && Module.Tire)
			{
				newTireScale.x *= Width / Module.TireBounds.size.x;
				newRimScale.x *= newTireScale.x;
				Module.Tire.localScale = newTireScale;
				Module.Rim.localScale = newRimScale;
			}

			if (Module.BrakeDiscRenderer)
			{
				Bounds brakeDiscBounds = Utility.GetObjectBounds(Module.BrakeDiscRenderer.gameObject);

				newBrakeDiscScale.y = brakeModule.Diameter / brakeDiscBounds.size.y;
				newBrakeDiscScale.z = brakeModule.Diameter / brakeDiscBounds.size.z;
				Module.BrakeDiscRenderer.transform.localScale = newBrakeDiscScale;

				if (Module.BrakeCalliper)
					Module.BrakeCalliper.localScale = newBrakeDiscScale;
			}

			sfx = new(this);
			vfx = new(this);
			wheelPosition = (int)Module.side;

			brakeDiscSpinModule = Module.BrakeDiscRenderer ? Module.BrakeDiscRenderer.GetComponent<VehicleWheelSpin>() : null;
			brakeDiscMaterial = Module.BrakeDiscRenderer && Module.BrakeDiscRenderer.materials.Length > 0 ? Module.BrakeDiscRenderer.materials[Module.BrakeDiscMaterialIndex] : null;
#if !MVC_COMMUNITY
			groundsStrengths = new float[Settings.Grounds.Length];
#endif
			rimEdgeMaterial = Module.RimEdgeRenderer ? Module.RimEdgeRenderer.materials[Module.RimEdgeMaterialIndex] : null;

			if (Module.RimEdgeRenderer)
				Module.RimEdgeRenderer.sharedMaterials[Module.RimEdgeMaterialIndex] = new(rimEdgeMaterial);

			if (BrakeDiscSpinModule)
				brakeDiscSpinMaterialModule = brakeDiscSpinModule.GetSpinMaterial(Module.BrakeDiscMaterialIndex);

			damageZones = new();
			Awaken = true;
		}

		private void Initialize()
		{
			sfx = default;
			vehicleInstance = null;
			brakeDiscSpinModule = null;
			brakeDiscSpinMaterialModule = null;
			brakeDiscMaterial = null;
			suspension = null;
			damageZones = null;
			explosionHits = new Collider[32];
			HitInfo = default;
			TireHealth = default;
			CurrentRadius = default;
			RPM = default;
			AngularVelocity = default;
			Speed = default;
			CamberAngle = default;
			CasterAngle = default;
			ToeAngle = default;
			TireTemperature = default;
			RimTemperature = default;
			IsGrounded = default;
			IsOffRoading = default;
			CurrentGroundIndex = -1;
			ClampedCurrentGroundIndex = 0;
			LocalVelocity = default;
			LocalAngularVelocity = default;
			newRimScale = Vector3.one;
			newTireScale = Vector3.one;
			newBrakeDiscScale = Vector3.one;
#if !MVC_COMMUNITY
			groundsStrengths = null;
#endif
			slipInertia = default;
			dampingRate = default;
			forwardSmoothSlip = default;
			sidewaysSmoothSlip = default;
			forwardFrictionStiffnessMultiplier = 1f;
			sidewaysFrictionStiffnessMultiplier = 1f;
			damageFactor = default;
			newTireHealth = default;
			slipFactor = default;
			deltaTime = default;
			BrakeTemperature = default;
			brakeTemperature = default;
			hasResetWheel = default;
			updated = default;
			wheelPosition = default;

			if (VFXParent)
				Utility.Destroy(true, VFXParent.gameObject);

			if (SFXParent)
				Utility.Destroy(true, SFXParent.gameObject);

			if (wheelCollider)
				Utility.Destroy(true, wheelCollider.gameObject);

			wheelCollider = null;
			sfx = null;
			vfx = null;

			if (Module.Rim)
				Module.Rim.localScale = Vector3.one;

			if (Module.Tire)
				Module.Tire.localScale = Vector3.one;

			if (Module.BrakeDiscRenderer)
				Module.BrakeDiscRenderer.transform.localScale = Vector3.one;

			if (rimEdgeMaterial && Module.RimEdgeRenderer)
				Module.RimEdgeRenderer.sharedMaterials[Module.RimEdgeMaterialIndex] = rimEdgeMaterial;

			rimEdgeMaterial = null;

			RefreshRimMaterialEmissionColorPropertyName();
		}

		#endregion

		#region Fixed Update

		private void FixedUpdate()
		{
			if (!Awaken)
				return;

			Calculations();
			Torque();
			Angles();

			if (!Module.IsTrailerWheel)
				Donuts();
		}
		private void Calculations()
		{
			mass = math.max(mass, 1f);
			deltaTime = Utility.DeltaTime;

			if (Awaken)
			{
				CurrentSteerAngle = TrailerInstance ? 0f : steerAngle;
				CurrentFakeSteerAngle = TrailerInstance ? 0f : steerAngle;

				if (!TrailerInstance && Module.IsFrontWheel && Settings.counterSteerType == ToolkitSettings.CounterSteerType.VisualsOnly)
					vehicleInstance.ApplyCounterSteerHelper(ref CurrentFakeSteerAngle);
			}

			if (!wheelCollider)
				return;

			HitInfo = GetWheelHit();
			RPM = wheelCollider.rpm;
			AngularVelocity = RPM * 2f * math.PI / 60f;
			Speed = Utility.RPMToSpeed(RPM, CurrentRadius);

			RefreshGround(wheelCollider.transform);

			LocalVelocity = transform.InverseTransformVector(VehicleInstance.Rigidbody.GetPointVelocity(IsGrounded ? HitInfo.point : transform.position));
			RelativeVelocity = transform.InverseTransformVector(VehicleInstance.Rigidbody.
#if UNITY_6000_0_OR_NEWER
					linearVelocity
#else
					velocity
#endif
				);
			LocalAngularVelocity = transform.InverseTransformDirection(VehicleInstance.Rigidbody.angularVelocity);
			slipInertia = Utility.Round(RPM, 1) != 0f ? 2.5f * deltaTime : 1f;
			forwardSmoothSlip = Utility.Lerp(forwardSmoothSlip, HitInfo.forwardSlip, slipInertia);
			sidewaysSmoothSlip = Utility.Lerp(sidewaysSmoothSlip, HitInfo.sidewaysSlip, slipInertia);
			sqrRelativeVelocity = Utility.Lerp(sqrRelativeVelocity, RelativeVelocity.x * RelativeVelocity.x * .02f, Time.fixedDeltaTime * 5f);

			if (HitInfo.forwardSlip > 0)
				sqrRelativeVelocity += math.abs(HitInfo.forwardSlip) * .5f;

			if (Awaken)
			{
				float handlingRate = VehicleInstance.Stability.HandlingRate;
				float groundWetness = GroundMapper ? GroundMapper.Map[ClampedCurrentGroundMapIndex].Wetness : 0f;
				bool isFrontWheel = Module.IsFrontWheel;

				// Forward Drift
				CurrentWheelColliderForwardFriction = tireCompound.frictionComplexity == VehicleTireCompound.FrictionComplexity.Simple ? tireCompound.wheelColliderAccelerationFriction : Mathf.Sign(HitInfo.forwardSlip) * Mathf.Sign(Mathf.Approximately(RPM, 0f) ? LocalVelocity.z : RPM) >= 0f ? tireCompound.wheelColliderAccelerationFriction : tireCompound.wheelColliderBrakeFriction;

				if (isFrontWheel)
				{
					tempDriftWheelFrictionCurve.extremumValue = math.clamp(CurrentWheelColliderForwardFriction.extremumValue - sqrRelativeVelocity * 2f, MinDriftForwardFrictionMultiplier / 2f, MaxDriftForwardFrictionMultiplier);
					tempDriftWheelFrictionCurve.asymptoteValue = math.clamp(CurrentWheelColliderForwardFriction.asymptoteValue - sqrRelativeVelocity * 2f, MinDriftForwardFrictionMultiplier / 2f, MaxDriftForwardFrictionMultiplier);
				}
				else
				{
					tempDriftWheelFrictionCurve.extremumValue = math.clamp(CurrentWheelColliderForwardFriction.extremumValue - sqrRelativeVelocity, MinDriftForwardFrictionMultiplier, MaxDriftForwardFrictionMultiplier);
					tempDriftWheelFrictionCurve.asymptoteValue = math.clamp(CurrentWheelColliderForwardFriction.asymptoteValue + sqrRelativeVelocity, MinDriftForwardFrictionMultiplier, MaxDriftForwardFrictionMultiplier);
				}

				tempTargetWheelFrictionCurve.extremumSlip = forwardExtremumSlip = CurrentWheelColliderForwardFriction.extremumSlip;
				tempTargetWheelFrictionCurve.asymptoteSlip = CurrentWheelColliderForwardFriction.asymptoteSlip;
				tempTargetWheelFrictionCurve.extremumValue = Utility.Lerp(tempDriftWheelFrictionCurve.extremumValue, CurrentWheelColliderForwardFriction.extremumValue, handlingRate);
				tempTargetWheelFrictionCurve.asymptoteValue = Utility.Lerp(tempDriftWheelFrictionCurve.asymptoteValue, CurrentWheelColliderForwardFriction.asymptoteValue, handlingRate);
				tempTargetWheelFrictionCurve.stiffness = CurrentWheelColliderForwardFriction.GetStiffness(Speed) * forwardFrictionStiffnessMultiplier;
				tempTargetWheelFrictionCurve.stiffness = tireCompound.GetStiffness(tempTargetWheelFrictionCurve.stiffness, width * 1000f, groundWetness);
				wheelCollider.forwardFriction = tempTargetWheelFrictionCurve;

				// Sideways Drift
				CurrentWheelColliderSidewaysFriction = TireCompound.wheelColliderSidewaysFriction;

				if (isFrontWheel)
				{
					tempDriftWheelFrictionCurve.extremumValue = math.clamp(CurrentWheelColliderSidewaysFriction.extremumValue - sqrRelativeVelocity, MinDriftSidewaysFrictionMultiplier, MaxDriftSidewaysFrictionMultiplier);
					tempDriftWheelFrictionCurve.asymptoteValue = math.clamp(CurrentWheelColliderSidewaysFriction.asymptoteValue - sqrRelativeVelocity, MinDriftSidewaysFrictionMultiplier, MaxDriftSidewaysFrictionMultiplier);
				}
				else
				{
					tempDriftWheelFrictionCurve.extremumValue = math.clamp(CurrentWheelColliderSidewaysFriction.extremumValue - sqrRelativeVelocity, MinDriftSidewaysFrictionMultiplier, MaxDriftSidewaysFrictionMultiplier);
					tempDriftWheelFrictionCurve.asymptoteValue = math.clamp(CurrentWheelColliderSidewaysFriction.asymptoteValue - sqrRelativeVelocity, MinDriftSidewaysFrictionMultiplier, MaxDriftSidewaysFrictionMultiplier);
				}

				tempTargetWheelFrictionCurve.extremumSlip = sidewaysExtremumSlip = CurrentWheelColliderSidewaysFriction.extremumSlip;
				tempTargetWheelFrictionCurve.asymptoteSlip = CurrentWheelColliderSidewaysFriction.asymptoteSlip;
				tempTargetWheelFrictionCurve.extremumValue = Utility.Lerp(tempDriftWheelFrictionCurve.extremumValue, CurrentWheelColliderSidewaysFriction.extremumValue, handlingRate);
				tempTargetWheelFrictionCurve.asymptoteValue = Utility.Lerp(tempDriftWheelFrictionCurve.asymptoteValue, CurrentWheelColliderSidewaysFriction.asymptoteValue, handlingRate);
				tempTargetWheelFrictionCurve.stiffness = CurrentWheelColliderSidewaysFriction.GetStiffness(Speed) * sidewaysFrictionStiffnessMultiplier;
				tempTargetWheelFrictionCurve.stiffness = tireCompound.GetStiffness(tempTargetWheelFrictionCurve.stiffness, width * 1000f, groundWetness);
				wheelCollider.sidewaysFriction = tempTargetWheelFrictionCurve;

				averageExtremumSlip = Utility.Average(forwardExtremumSlip, sidewaysExtremumSlip);
				sidewaysFrictionStiffnessMultiplier = 1f;
				forwardFrictionStiffnessMultiplier = 1f;
			}

			wheelCollider.radius = CurrentRadius;
			updated = wheelCollider.mass != mass || suspension.Length
#if !MVC_COMMUNITY
						* (1f + suspension.LengthStance)
#endif
						!= wheelCollider.suspensionDistance ||
						wheelCollider.suspensionSpring.spring != suspension.Stiffness ||
						wheelCollider.suspensionSpring.damper != suspension.Damper ||
						wheelCollider.suspensionSpring.targetPosition != suspension.Target
#if !MVC_COMMUNITY
						|| Settings.useSuspensionAdjustments && Settings.sideOffsetAffectHandling && sideOffset != suspension.SideOffset
#endif
						;

			if (updated)
			{
#if !MVC_COMMUNITY
				sideOffset = Settings.useSuspensionAdjustments && Settings.sideOffsetAffectHandling ? suspension.SideOffset : 0f;
#endif
				wheelCollider.transform.localPosition = -Vector3.up * Radius + transform.InverseTransformDirection(VehicleInstance.transform.up) * ((1f - suspension.Target) * suspension.Length + Radius);
#if !MVC_COMMUNITY
				wheelCollider.transform.position += Quaternion.AngleAxis(ToeAngle * -wheelPosition, VehicleInstance.transform.up) * VehicleInstance.transform.right * sideOffset * wheelPosition;
#endif
				wheelCollider.mass = mass;
				wheelCollider.suspensionDistance = suspension.Length
#if !MVC_COMMUNITY
					* (1f + suspension.LengthStance)
#endif
					;
				wheelCollider.suspensionSpring = new()
				{
					spring = suspension.Stiffness,
					damper = suspension.Damper,
					targetPosition = suspension.Target
				};
			}

			if (Awaken)
			{
				Handling(ClampedCurrentGroundIndex);

				for (int i = 0; i < damageZones.Count; i++)
					DamageTrigger(damageZones[i]);
			}
		}
		private void RefreshGround(Transform colliderTransform)
		{
			if (!IsGrounded)
			{
				GroundCollider = null;
				GroundMapper = null;
				CurrentGroundIndex = -1;
				CurrentGroundMapIndex = -1;
				ClampedCurrentGroundIndex = 0;
				ClampedCurrentGroundMapIndex = 0;
				IsOffRoading = false;

				return;
			}

			GroundMapper = HitInfo.collider.GetComponent<VehicleGroundMapper>();

			if (!GroundMapper)
			{
				GroundMapper = HitInfo.collider.gameObject.AddComponent<VehicleGroundMapper>();

				Manager.RefreshGroundMappers();

				for (int i = 0; i < VehicleInstance.Chassis.ignoredColliders.Length; i++)
					Physics.IgnoreCollision(VehicleInstance.Chassis.ignoredColliders[i], GroundMapper.ColliderInstance, true);
			}

			if (GroundCollider == HitInfo.collider && (!GroundMapper || GroundMapper.Type != VehicleGroundMapper.GroundType.Terrain))
			{
				ClampedCurrentGroundMapIndex = 0;
				CurrentGroundMapIndex = GroundMapper ? 0 : -1;

				return;
			}

			GroundCollider = HitInfo.collider;

			if (!GroundMapper || GroundMapper.Type == VehicleGroundMapper.GroundType.Invalid)
			{
				CurrentGroundIndex = -1;
				ClampedCurrentGroundIndex = 0;

#if !MVC_COMMUNITY
				for (int i = 0; i < groundsStrengths.Length; i++)
					groundsStrengths[i] = 0f;
#endif
			}
#if !MVC_COMMUNITY
			else if (GroundMapper.Type == VehicleGroundMapper.GroundType.Terrain)
			{
				float[] layers = VehicleGroundMapper.GetTerrainSplatMapMix(GroundMapper.TerrainInstance, colliderTransform.position);
				float layerStrength = 0f;

				if (layers != null)
				{
					var mapArray = GroundMapper.Map;

					for (int i = 0; i < mapArray.Length; ++i)
					{
						var map = mapArray[i];
						int newGround = map.index;

						if (layers[i] > layerStrength)
						{
							CurrentGroundIndex = newGround;
							ClampedCurrentGroundIndex = newGround;
							groundsStrengths[map.index] = layerStrength;
							layerStrength = layers[i];
							CurrentGroundMapIndex = ClampedCurrentGroundMapIndex = i;
						}
					}
				}
			}
#endif
			else
			{
				CurrentGroundMapIndex = ClampedCurrentGroundMapIndex = 0;
#if MVC_COMMUNITY
				CurrentGroundIndex = 0;
				ClampedCurrentGroundIndex = 0;
#else

				var groundMap = GroundMapper.Map[0];

				CurrentGroundIndex = groundMap.index;
				ClampedCurrentGroundIndex = groundMap.index;

				for (int i = 0; i < groundsStrengths.Length; i++)
					groundsStrengths[i] = Utility.BoolToNumber(i == groundMap.index);
#endif
			}

			IsOffRoading = Settings.Grounds[ClampedCurrentGroundIndex].isOffRoad;
		}
		private void Handling(int groundIndex)
		{
			var ground = Settings.Grounds[groundIndex];

			totalSlip = math.max(math.abs(HitInfo.forwardSlip), math.abs(HitInfo.sidewaysSlip));
			totalSlipFactor = math.max(Utility.InverseLerpUnclamped(averageExtremumSlip, 1f, totalSlip), 0f);
			dampingRate = Utility.Lerp(ground.WheelDampingRate, ground.WheelBurnoutDampingRate, VehicleInstance.burnoutIntensity);

			if (Module.IsMotorWheel)
				wheelCollider.wheelDampingRate = dampingRate;

			damageFactor = Utility.InverseLerp(RimRadius, Radius, CurrentRadius);

			if (Settings.useWheelHealth)
				MultiplyForwardFrictionStiffness(Utility.Lerp(ground.DamagedWheelStiffness, 1f, damageFactor));

			MultiplyForwardFrictionStiffness(ground.FrictionStiffness);
			MultiplySidewaysFrictionStiffness(ground.FrictionStiffness);

			if (Settings.useWheelHealth)
			{
				newTireHealth = TireHealth;

				if (newTireHealth > 0f)
				{
					slipFactor = Utility.Clamp01(math.abs(HitInfo.forwardSlip) + math.abs(HitInfo.sidewaysSlip) * .25f);
					newTireHealth -= slipFactor * TireTemperature * math.max(ground.FrictionStiffness, 0f) * Time.fixedDeltaTime * .01f * Settings.tireDamageIntensity;
				}

				if (TireHealth > 0f && newTireHealth <= 0f)
				{
					vfx.ExplodeTire();
					sfx.ExplodeTire();

					if (VehicleInstance.Rigidbody)
					{
						Vector3 explosionPosition = HitInfo.point + CurrentRadius * transform.up;
						int explosionHitsCount = Physics.OverlapSphereNonAlloc(explosionPosition, Radius * 2f, explosionHits, Utility.ExclusiveMask(Settings.vehiclesLayer, Settings.vehicleWheelsLayer), QueryTriggerInteraction.Ignore);

						if (explosionHitsCount > 0)
						{
							float forceMultiplier = (Radius - tireThickness * .01f) * Width * 16f; // = (Radius - tireThickness * .01f) * Width / .25f / .25f;
							Vector3 explosionForceDirection = Vector3.zero;
							float explosionForceAverage = 0f;
							int actualExplosionHitsCount = 0;

							for (int i = 0; i < explosionHitsCount; i++)
							{
								if (!explosionHits[i])
									continue;

								Rigidbody hitRigidbody = explosionHits[i].attachedRigidbody;
								Vector3 hitPoint = explosionHits[i].ClosestPoint(explosionPosition);

								if (Mathf.Approximately(hitPoint.x, explosionPosition.x) && Mathf.Approximately(hitPoint.y, explosionPosition.y) && Mathf.Approximately(hitPoint.z, explosionPosition.z))
									hitPoint = explosionHits[i].ClosestPointOnBounds(explosionPosition);

								Vector3 normal = Utility.Direction(hitPoint, explosionPosition);
								float distance = Utility.Distance(explosionPosition, hitPoint);
								float forceFactor = 1f - Utility.InverseLerp(Radius, Radius * 2f, distance);
								float forceStep = hitRigidbody ? hitRigidbody.mass / Settings.tireExplosionForce : 1f;

								explosionForceAverage += Settings.tireExplosionForce * forceStep * forceFactor * forceMultiplier;
								explosionForceDirection += normal;

								if (hitRigidbody)
									hitRigidbody.AddForceAtPosition(Settings.tireExplosionForce * forceFactor * -normal, explosionPosition, ForceMode.Impulse);

								actualExplosionHitsCount++;
							}

							explosionForceAverage /= actualExplosionHitsCount;

							Vector3 explosionForce = explosionForceAverage * math.normalizesafe(explosionForceDirection, transform.up);

							VehicleInstance.Rigidbody.AddForceAtPosition(explosionForce, explosionPosition, ForceMode.Impulse);
						}
					}

					TireHealth = 0f;
				}
				else
					TireHealth = newTireHealth;
			}
			else if (TireHealth < 1f)
				TireHealth = 1f;

			float groundWetness = GroundMapper ? GroundMapper.Map[ClampedCurrentGroundMapIndex].Wetness : 0f;

			TireTemperature = TireHealth > 0f ? math.max(Mathf.MoveTowards(TireTemperature, totalSlipFactor * (1f - groundWetness), ground.FrictionStiffness * .1f * Utility.Lerp(.05f, 1f, totalSlipFactor * (1f - groundWetness)) * Time.fixedDeltaTime), 0f) : 0f;
		}
		private void Donuts()
		{
			if (!ToolkitSettings.UsingWheelColliderPhysics || VehicleInstance.Steertrain == Vehicle.Train.AWD && !VehicleInstance.Steering.invertRearSteer || !Module.IsMotorWheel || VehicleInstance.Behaviour.vehicleType == Vehicle.VehicleType.HeavyTruck)
				return;

			float newDonutIntensity = 1f - Utility.Clamp01(VehicleInstance.Inputs.Direction * VehicleInstance.Stats.currentSpeed / 60f);

			newDonutIntensity *= Utility.Clamp01(Speed) * math.abs(VehicleInstance.Stats.steerAngle / VehicleInstance.Steering.MaximumSteerAngle) * (VehicleInstance.Stats.wheelPower / math.max(VehicleInstance.Engine.Power, VehicleInstance.Stats.rawEnginePower));
			newDonutIntensity *= Utility.Clamp01(-Mathf.Sign(VehicleInstance.Stats.localAngularVelocity.y) * VehicleInstance.Stats.averageMotorWheelsSmoothSidewaysSlip);

			float newDonutDirection = Utility.Clamp01(Mathf.Sign(VehicleInstance.Stats.localAngularVelocity.y) * Mathf.Sign(VehicleInstance.Stats.steerAngle));

			donutDirection = Utility.Lerp(donutDirection, newDonutDirection, Time.fixedDeltaTime);
			newDonutIntensity *= donutDirection * VehicleInstance.Stability.HandlingRate;
			newDonutIntensity *= VehicleInstance.Stats.engineRPM / VehicleInstance.Behaviour.PeakTorqueRPM;
			newDonutIntensity /= math.max(math.abs(VehicleInstance.Stats.localAngularVelocity.y), .1f);
			donutIntensity = Utility.Lerp(donutIntensity, newDonutIntensity, math.pow(1.618f, donutIntensity > newDonutIntensity ? 2.617924f : 1f) * Time.fixedDeltaTime);

			MultiplySidewaysFrictionStiffness(Utility.Clamp01(1f - donutIntensity));
		}
		private void Angles()
		{
			if (!suspension)
				return;

			transform.localEulerAngles = Vector3.zero;

#if !MVC_COMMUNITY
			if (Settings.useSuspensionAdjustments)
			{
				CasterAngle = suspension.Caster * Mathf.Sign(transform.localPosition.z);
				ToeAngle = wheelPosition * suspension.Toe;
				CamberAngle = -suspension.Camber;
				CamberAngle -= TrailerInstance ? default : CasterAngle * wheelPosition * (CurrentSteerAngle + ToeAngle) / VehicleInstance.Steering.MaximumSteerAngle;
				CamberAngle = math.clamp(CamberAngle, -Settings.maximumCamberAngle, Settings.maximumCamberAngle) * wheelPosition;
			}
			else if (!Application.isPlaying)
			{
#endif
			CasterAngle = default;
			ToeAngle = default;
			CamberAngle = default;
#if !MVC_COMMUNITY
			}
#endif

			if (Application.isPlaying)
			{
				wheelCollider.steerAngle = CurrentSteerAngle
#if !MVC_COMMUNITY
						- ToeAngle
#endif
						;
			}
		}
		private void Torque()
		{
			brakeTorque = math.clamp(brakeTorque, 0f, brakeModule ? brakeModule.BrakeTorque : math.INFINITY);

			if (Settings.useBrakesHeat && Settings.brakeHeatAffectPerformance)
				brakeTorque *= 1f - BrakeTorqueLoss;

			if (Module.IsMotorWheel && !hasResetWheel)
				GearLimiter();

			if (hasResetWheel)
			{
				motorTorque = 0f;
				brakeTorque = brakeModule ? brakeModule.BrakeTorque : 0f;
			}

			wheelCollider.motorTorque = motorTorque;
			wheelCollider.brakeTorque = brakeTorque;

			if (hasResetWheel && VehicleInstance.Inputs.Fuel > 0f)
				hasResetWheel = false;
		}
		private void GearLimiter()
		{
			float speed = math.abs(VehicleInstance.Stats.averageMotorWheelsSpeed);
			float targetGearTorque = VehicleInstance.Stats.rawEngineTorque * VehicleInstance.Transmission.FinalGearRatio;

			if (Module.IsFrontWheel)
				targetGearTorque *= VehicleInstance.Transmission.FrontDifferential.GearRatio;
			else
				targetGearTorque *= VehicleInstance.Transmission.RearDifferential.GearRatio;

			VehicleInstance.Stats.SetEngineBrakeTorque(VehicleInstance, 0f);

			if (!VehicleInstance.IsElectric && VehicleInstance.Stats.isEngineStall || Mathf.Sign(RPM) != Mathf.Sign(VehicleInstance.Inputs.Direction) && Utility.Round(Speed, 1) != 0f && Utility.Round(VehicleInstance.Stats.currentSpeed, 1) != 0f)
			{
				motorTorque = 0f;
				VehicleInstance.Stats.SetEngineBrakeTorque(VehicleInstance, VehicleInstance.Behaviour.Torque * VehicleInstance.Transmission.FinalGearRatio * VehicleInstance.Stats.currentGearRatio * (1f - VehicleInstance.Inputs.Clutch) * (VehicleInstance.Stats.isEngineStall ? 1f - Utility.Clamp01(VehicleInstance.Stats.rawEnginePower / VehicleInstance.Behaviour.Power) : 1f));
			}
			else if (!VehicleInstance.IsElectric)
			{
				if (VehicleInstance.Stats.isOverRev)
				{
					float targetBrake = VehicleInstance.Stats.currentGearSpeedTarget > 0f ? speed / VehicleInstance.Stats.currentGearSpeedTarget : speed;

					targetBrake = math.max(targetBrake - 1f, 0f);
					targetBrake *= 1f - VehicleInstance.Inputs.Clutch;
					VehicleInstance.Stats.SetEngineBrakeTorque(VehicleInstance, targetGearTorque * VehicleInstance.Stats.currentGearRatio * targetBrake);
				}
				else if (speed <= VehicleInstance.Stats.currentGearMinSpeedTarget && (VehicleInstance.Stats.isEngineRunning || VehicleInstance.Stats.isEngineStarting) && (Settings.useGearLimiterOnReverseAndFirstGear || VehicleInstance.Stats.currentGear > 0))
				{
					float targetTorque = 1f - Utility.Clamp01(VehicleInstance.Stats.currentGearMinSpeedTarget > 0f ? speed / VehicleInstance.Stats.currentGearMinSpeedTarget : 1f);

					targetTorque *= 1f - Utility.Clamp01(VehicleInstance.Inputs.Clutch * Utility.Clamp01(1f - math.abs(VehicleInstance.Inputs.Direction) + VehicleInstance.Inputs.ClutchPedal) + Utility.BoolToNumber(VehicleInstance.Stats.currentGear == 0) * VehicleInstance.Inputs.Brake);
					motorTorque += targetGearTorque * VehicleInstance.Inputs.Direction * targetTorque * math.max(VehicleInstance.Stats.currentGearRatio, 1f) / Utility.Clamp01(VehicleInstance.Stats.currentGearRatio);
				}
				else
				{
					float targetBrake = Utility.Clamp01((VehicleInstance.Stats.rawEnginePower / VehicleInstance.Engine.Power) - VehicleInstance.Inputs.RawFuel - VehicleInstance.Inputs.Clutch);

					VehicleInstance.Stats.SetEngineBrakeTorque(VehicleInstance, math.max(VehicleInstance.Engine.Torque * VehicleInstance.Transmission.FinalGearRatio - targetGearTorque, 0f) * VehicleInstance.Stats.currentGearRatio * targetBrake);
				}
			}

			brakeTorque += VehicleInstance.Stats.engineBrakeTorque;
		}
		private void DamageTrigger(VehicleDamageZone zone)
		{
			if (!zone || !zone.collider.bounds.Contains(HitInfo.point))
				return;

			switch (zone.zoneType)
			{
				case VehicleDamageZone.DamageZoneType.RepairVehicle:
					RepairWheel();

					break;

				case VehicleDamageZone.DamageZoneType.Wheel:
					if (!IsGrounded)
						break;

					DamageWheel();

					break;
			}
		}

		#endregion

		#region LateUpdate

		private void LateUpdate()
		{
			if (!Awaken)
				return;

			LateCalculations();
			Renderers();
			sfx.Update();
			vfx.Update();
		}
		private void LateCalculations()
		{
			LateHitInfo = GetWheelHit();
			LateLocalVelocity = transform.InverseTransformVector(VehicleInstance.Rigidbody.
#if UNITY_6000_0_OR_NEWER
					linearVelocity
#else
					velocity
#endif
					);
			LateRelativeVelocity = transform.InverseTransformVector(VehicleInstance.Rigidbody.
#if UNITY_6000_0_OR_NEWER
					linearVelocity
#else
					velocity
#endif
					);
			LateLocalAngularVelocity = transform.InverseTransformVector(VehicleInstance.Rigidbody.angularVelocity);
		}
		private void Renderers()
		{
			if (!Module)
				return;

			if (Application.isPlaying)
			{
				wheelCollider.GetWorldPose(out Vector3 newPosition, out _);

				position = newPosition;
				wheelRotationFromRPM += Settings.wheelRendererRefreshType switch
				{
					ToolkitSettings.WheelRendererRefreshType.Approximate => wheelCollider.rpm * 6f * Time.deltaTime,
					_ => wheelCollider.rpm / 60f * 360f * Time.deltaTime
				};
			}
			else
			{
				position = transform.position;
				wheelRotationFromRPM = default;
				CurrentSteerAngle = default;
				CurrentFakeSteerAngle = default;
			}

			rotation = math.mul(transform.rotation, quaternion.Euler(math.radians(wheelRotationFromRPM), math.radians((Settings.counterSteerType == ToolkitSettings.CounterSteerType.VisualsOnly ? CurrentFakeSteerAngle : CurrentSteerAngle)
#if !MVC_COMMUNITY
				- ToeAngle
#endif
				), 0f));

			Module.Model.SetPositionAndRotation(position, transform.rotation);

#if !MVC_COMMUNITY
			if (Settings.useSuspensionAdjustments)
				if (ToolkitSettings.UsingWheelColliderPhysics)
				{
					if (Application.isPlaying)
						Module.Model.RotateAround(wheelCollider.transform.position, Module.Model.forward, CamberAngle);
					else
						Module.Model.RotateAround(position, VehicleInstance.transform.forward, CamberAngle);

					if (!Settings.sideOffsetAffectHandling)
						Module.Model.position += Quaternion.AngleAxis((Settings.counterSteerType == ToolkitSettings.CounterSteerType.VisualsOnly ? CurrentFakeSteerAngle : CurrentSteerAngle) - ToeAngle, VehicleInstance.transform.up) * VehicleInstance.transform.right * wheelPosition * Suspension.SideOffset;
				}
#endif

			Module.Model.transform.rotation = rotation;

			if (Module.BrakeCalliper)
			{
				Module.BrakeCalliper.SetPositionAndRotation(position, transform.rotation * Quaternion.Euler(0f, (Settings.counterSteerType == ToolkitSettings.CounterSteerType.VisualsOnly ? CurrentFakeSteerAngle : CurrentSteerAngle)
#if !MVC_COMMUNITY
					- ToeAngle
#endif
					, 0f));

#if !MVC_COMMUNITY
				if (Settings.useSuspensionAdjustments)
					if (ToolkitSettings.UsingWheelColliderPhysics)
					{
						if (Application.isPlaying)
							Module.Model.RotateAround(wheelCollider.transform.position, VehicleInstance.transform.forward, CamberAngle);
						else
							Module.BrakeCalliper.RotateAround((Vector3)position + .5f * Radius * transform.up, VehicleInstance.transform.forward, CamberAngle);

						if (!Settings.sideOffsetAffectHandling)
							Module.BrakeCalliper.position += Quaternion.AngleAxis((Settings.counterSteerType == ToolkitSettings.CounterSteerType.VisualsOnly ? CurrentFakeSteerAngle : CurrentSteerAngle) - ToeAngle, VehicleInstance.transform.up) * VehicleInstance.transform.right * wheelPosition * suspension.SideOffset;
					}
#endif
			}

			if (Application.isPlaying)
			{
				if (Settings.useBrakesHeat)
				{
					brakeIntensity = Utility.Clamp01((brakeTorque - VehicleInstance.Stats.engineBrakeTorque) * Utility.Clamp01(math.abs(RPM * .025f)) / brakeModule.BrakeTorque);
					brakeTemperature += Utility.LerpUnclamped(-1f, 1f, brakeIntensity * (1f + math.abs(RPM / 100f))) * Time.deltaTime * .05f * Utility.Lerp(Settings.BrakeCoolingSpeed, Settings.BrakeHeatingSpeed, brakeIntensity);
					brakeTemperature = Settings.clampBrakeHeat ? math.clamp(brakeTemperature, 0f, 1f + brakeModule.BrakeHeatThreshold) : math.max(brakeTemperature, 0f);

					if (BrakeTemperature != brakeTemperature)
					{
						BrakeTemperature = brakeTemperature;
						BrakeTorqueLoss = Utility.InverseLerp(brakeModule.BrakeHeatThreshold, 1f + brakeModule.BrakeHeatThreshold, BrakeTemperature);

						if (BrakeTemperature >= brakeModule.BrakeHeatThreshold && Module.BrakeDiscRenderer && brakeDiscMaterial)
						{
							brakeDiscMaterial.SetColor(Settings.brakeMaterialEmissionColorProperty, Settings.brakeHeatEmissionColor * BrakeTorqueLoss);

							if (brakeDiscSpinMaterialModule)
								brakeDiscSpinMaterialModule.RuntimeSpinMaterial.SetColor(Settings.brakeMaterialEmissionColorProperty, brakeDiscMaterial.GetColor(Settings.brakeMaterialEmissionColorProperty));
						}
					}
				}

				if (Settings.useDamage && Settings.useWheelHealth && Module.RimEdgeRenderer)
				{
					RimTemperature = TireHealth > 0f ? default : Utility.Lerp(RimTemperature, math.abs(HitInfo.forwardSlip) * Utility.Clamp01(math.abs(RPM)), Time.deltaTime * 2.5f);

					Module.RimEdgeRenderer.sharedMaterials[Module.RimEdgeMaterialIndex].SetColor(GetRimMaterialEmissionColorPropertyName(), Settings.rimEdgeEmissionColor * RimTemperature);
					Module.RimEdgeRenderer.gameObject.SetActive(!Module.HideRimEdgePerDefault || RimTemperature > 0);
				}
			}
		}

		#endregion

		#region Destroy & Gizmos

		private void OnDestroy()
		{
			Utility.Destroy(false, gameObject);
		}
		private void OnDrawGizmosSelected()
		{
			if (!IsSetupDone || HasInternalErrors || !VehicleInstance || !Module || !Suspension || VehicleInstance.Exhausts != null && Array.Find(VehicleInstance.Exhausts, exhaust => exhaust.editorFoldout))
				return;

			bool awaken = Awaken;

			Awaken = false;

			Calculations();

			Awaken = awaken;
			wheelPosition = (int)Module.side;

			float3 position = Application.isPlaying ? (float3)Module.Model.position : (float3)transform.position + wheelPosition * suspension.SideOffset * math.mul(quaternion.AxisAngle(VehicleInstance.transform.up.y, math.radians(suspension.Toe * -wheelPosition)), VehicleInstance.transform.right);
			float3 euler = new()
			{
				x = ToolkitSettings.UsingWheelColliderPhysics ? 0f : -suspension.Caster * Mathf.Sign(transform.localPosition.z),
				y = CurrentSteerAngle - suspension.Toe * wheelPosition,
				z = -suspension.Camber - (TrailerInstance ? 0f : suspension.Caster * Mathf.Sign(transform.localPosition.z) * wheelPosition * (CurrentSteerAngle + wheelPosition * suspension.Toe) / VehicleInstance.Steering.MaximumSteerAngle)
			};

			euler.z = math.clamp(euler.z, -Settings.maximumCamberAngle, Settings.maximumCamberAngle) * wheelPosition;
			euler = math.radians(euler);

			quaternion rotation = Application.isPlaying ? (quaternion)Module.Model.rotation : math.mul(VehicleInstance.transform.rotation, quaternion.Euler(euler));
			float3 point0 = Radius * new float3(0f, math.sin(0f), math.cos(0f));
			float3 point1;
			float3 point2 = Radius * new float3(0f, math.sin(0f), math.cos(0f));
			float3 point3;
			float3 point4 = RimRadius * new float3(0f, math.sin(0f), math.cos(0f));
			float3 point5;
			float3 point6 = (Radius - tireThickness * .01f) * new float3(0f, math.sin(0f), math.cos(0f));
			float3 point7;
			float3 right = Vector3.right;
			float3 up = Vector3.up;

			point0 = position + math.mul(rotation, point0 + .5f * wheelPosition * Width * right);
			point2 = position + math.mul(rotation, point2 - .5f * wheelPosition * Width * right);
			point4 = position + math.mul(rotation, point4 + .5f * wheelPosition * Width * right);
			point6 = position + math.mul(rotation, point6 + .5f * wheelPosition * Width * right);

			for (int i = 1; i <= (int)math.round(122.9660359019731f * Radius); i++)
			{
				Gizmos.color = !Application.isPlaying || IsGrounded ? Color.green : Color.white;
				point1 = Radius * new float3(0, math.sin(i / math.round(122.9660359019731f * Radius) * math.PI * 2f), math.cos(i / math.round(122.9660359019731f * Radius) * math.PI * 2f));
				point1 = position + math.mul(rotation, (point1 + .5f * wheelPosition * Width * right));
				point3 = Radius * new float3(0, math.sin(i / math.round(122.9660359019731f * Radius) * math.PI * 2f), math.cos(i / math.round(122.9660359019731f * Radius) * math.PI * 2f));
				point3 = position + math.mul(rotation, (point3 - .5f * wheelPosition * Width * right));

				Gizmos.DrawLine(point0, point1);
				Gizmos.DrawLine(point2, point3);
				Gizmos.DrawLine(point0, point2);

				point0 = point1;
				point2 = point3;
				Gizmos.color = Color.red;
				point5 = RimRadius * new float3(0, math.sin(i / math.round(122.9660359019731f * Radius) * math.PI * 2f), math.cos(i / math.round(122.9660359019731f * Radius) * math.PI * 2f));
				point5 = position + math.mul(rotation, (point5 + .5f * wheelPosition * Width * right));

				Gizmos.DrawLine(point4, point5);

				point4 = point5;

				int j = (int)math.round((122.9660359019731f * (Radius - tireThickness * .01f)) * i / (122.9660359019731f * Radius));

				Gizmos.color = Utility.Color.orange;
				point7 = (Radius - tireThickness * .01f) * new float3(0, math.sin(j / math.round(122.9660359019731f * (Radius - tireThickness * .01f)) * math.PI * 2f), math.cos(j / math.round(122.9660359019731f * (Radius - tireThickness * .01f)) * math.PI * 2f));
				point7 = position + math.mul(rotation, point7 + .5f * wheelPosition * Width * right);

				Gizmos.DrawLine(point6, point7);

				point6 = point7;
			}

			Gizmos.color = Utility.Color.orange;
			euler.x = math.radians(suspension.Caster) * -Mathf.Sign(transform.localPosition.z);
			rotation = math.mul(VehicleInstance.transform.rotation, quaternion.Euler(euler));

			float length = suspension.Length * (1f + suspension.LengthStance);
			float spring = length * (1f - suspension.Target);

			Gizmos.DrawLine(position + math.mul(rotation, up * (-spring + Radius)), position + math.mul(rotation, up) * (length - spring + Radius));

			Gizmos.color = Color.white;
		}

		#endregion

		#endregion
	}
}

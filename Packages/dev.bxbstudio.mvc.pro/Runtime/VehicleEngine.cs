#region Namespaces

using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Utilities;
using MVC.Core;
using MVC.Utilities;
using System.Runtime.InteropServices;

#endregion

namespace MVC.Base
{
	[Serializable]
	public class VehicleEngine : ToolkitComponent
	{
		#region Enumerators

		public enum EngineFuelType { Diesel, Gas }
		public enum EnginePosition { Front, MidRear, Rear }
		public enum EngineType { Inline, V, Boxer, W, Rotary, Electric }

		#endregion

		#region Modules

		public readonly struct CurveSheet
		{
			#region Variables

			private readonly Vehicle.BehaviourModule behaviour;

			#endregion

			#region Methods

			#region Static Methods

			public static float MaxValueInCurve(AnimationCurve curve)
			{
				float maxValue = default;

				for (int i = 0; i < curve.length; i++)
					if (maxValue < curve.keys[i].value)
						maxValue = curve.keys[i].value;

				return maxValue;
			}

			#endregion

			#region Global Methods

			public AnimationCurve GetTorqueCurve(bool powerCurveDependent)
			{
				AnimationCurve curve = new()
				{
					preWrapMode = WrapMode.Clamp,
					postWrapMode = WrapMode.Clamp
				};

				if (!behaviour.Engine || !behaviour.Engine.IsValid)
					return curve;

				if (powerCurveDependent)
				{
					float multiplier = default;
					int maxKeyIndex = default;
					int keyIndex = default;
					float rpmStep = 500f;

					// Evaluate Power Curve and find maximum rpm
					for (float rpm = rpmStep; rpm < behaviour.Engine.MaximumRPM; rpm += rpmStep, keyIndex++)
					{
						float newMultiplier = behaviour.PowerCurve.Evaluate(rpm) * 5252f / math.max(rpm, 1f);

						if (newMultiplier >= multiplier)
						{
							multiplier = newMultiplier;
							maxKeyIndex = keyIndex;
						}
					}

					// Generate keys
					Keyframe[] keys = new Keyframe[keyIndex + 1];

					multiplier = behaviour.Torque / multiplier;
					keyIndex = default;

					for (float rpm = rpmStep; rpm < behaviour.Engine.MaximumRPM; rpm += rpmStep, keyIndex++)
						keys[keyIndex] = new(rpm, multiplier * behaviour.PowerCurve.Evaluate(rpm) * 5252f / math.max(rpm, 1f));

					keys[keyIndex] = new(behaviour.Engine.MaximumRPM, multiplier * behaviour.PowerCurve.Evaluate(behaviour.Engine.MaximumRPM) * 5252f / behaviour.Engine.MaximumRPM);

					// Offset keys RPM/time
					float curvePeakTorqueRPM = keys[maxKeyIndex].time;
					bool skipOffsetting = keys[maxKeyIndex].time == behaviour.PeakTorqueRPM;

					for (int i = 0; i < keys.Length; i++)
					{
						if (skipOffsetting)
							continue;

						if (i < maxKeyIndex)
							keys[i].time = Utility.Lerp(0f, behaviour.PeakTorqueRPM, Utility.InverseLerp(0f, curvePeakTorqueRPM, keys[i].time));
						else
							keys[i].time = Utility.Lerp(behaviour.PeakTorqueRPM, behaviour.Engine.MaximumRPM, Utility.InverseLerp(curvePeakTorqueRPM, behaviour.Engine.MaximumRPM, keys[i].time));
					}

					// Draw curve
					curve.AddKey(0f, behaviour.Engine.IsElectric ? behaviour.Torque : 0f);

					for (int i = 0; i < keys.Length; i++)
					{
						keys[i].inTangent = i > 0 ? (keys[i].value > keys[i - 1].value ? -1f : 1f) * EvaluateTangent(keys[i], keys[i - 1]) : 0f;
						keys[i].outTangent = i < keys.Length - 1 ? (keys[i].value > keys[i + 1].value ? -1f : 1f) * EvaluateTangent(keys[i], keys[i + 1]) : 0f;

						curve.AddKey(keys[i]);
					}

					return curve;
				}
				else if (behaviour.Engine.IsElectric)
				{
					curve.AddKey(new(0f, behaviour.Torque, 0f, 0f));
					curve.AddKey(new(behaviour.PeakTorqueRPM, behaviour.Torque, 0f, 0f));
					curve.AddKey(new(behaviour.Engine.MaximumRPM, 0f, 0f, 0f));
					curve.MoveKey(1, new(curve[1].time, curve[1].value, 0f, -EvaluateTangent(curve[1], curve[2])));

					return curve;
				}

				curve.AddKey(0f, 0f);
				curve.AddKey(behaviour.Engine.minimumRPM, behaviour.Torque * .5f);
				curve.AddKey(behaviour.PeakTorqueRPM, behaviour.Torque);
				curve.AddKey(
					behaviour.Engine.OverRevRPM,
					curve.Evaluate(math.clamp(behaviour.Engine.MinimumRPM * 1.5f, behaviour.Engine.MinimumRPM, behaviour.Engine.OverRevRPM))
				);
				curve.AddKey(behaviour.Engine.MaximumRPM, 0f);

				return curve;
			}
			public AnimationCurve GetPowerCurve()
			{
				AnimationCurve curve = new()
				{
					preWrapMode = WrapMode.Clamp,
					postWrapMode = WrapMode.Clamp
				};

				if (!behaviour.Engine || !behaviour.Engine.IsValid)
					return curve;

				if (behaviour.Engine.IsElectric)
				{
					curve.AddKey(0f, 0f);
					curve.AddKey(behaviour.PeakPowerRPM, behaviour.Power);
					curve.AddKey(behaviour.Engine.MaximumRPM, behaviour.Power);
					curve.MoveKey(1, new(curve[1].time, curve[1].value, EvaluateTangent(curve[1], curve[0]), 0f));

					return curve;
				}

				curve.AddKey(0f, 0f);
				curve.AddKey(behaviour.PeakPowerRPM, behaviour.Power);
				curve.AddKey(behaviour.Engine.MaximumRPM, 0f);

				return curve;
			}

			private float EvaluateTangent(Keyframe origin, Keyframe destination)
			{
				Vector3 newOrigin = new(0f, origin.value, origin.time);
				Vector3 newDestination = new(0f, destination.value, destination.time);
				float lookAngle = Quaternion.LookRotation(Utility.Direction(newOrigin, newDestination)).eulerAngles.x;

				return math.tan(math.radians(lookAngle));
			}

			#endregion

			#endregion

			#region Constructors

			public CurveSheet(Vehicle vehicle)
			{
				behaviour = vehicle.Behaviour;
			}

			#endregion
		}
		[Serializable]
		public class AudioModule
		{
			#region Modules & Enumerators

			public enum OutputType { EngineAndExhaust, Engine, Exhaust }
			public enum CelerationType { Merged, Separated }

			internal struct CelerationClip
			{
				#region Modules

				public struct SortComparer : IComparer<CelerationClip>
				{
					readonly int IComparer<CelerationClip>.Compare(CelerationClip x, CelerationClip y) => x.rpm - y.rpm >= 0f ? 1 : -1;
				}

				#endregion

				#region Variables

				public AudioClip clip;
				public float rpm;

				#endregion
			}
			internal struct CelerationSource
			{
				public VehicleAudioSource instance;
				public AudioDistortionFilter distortionFilter;
				public AudioLowPassFilter lowPassFilter;
				public float rpm;
			}
			internal struct CelerationSourceAccess
			{
				public float cutOffFrequency;
				public float distortionLevel;
				public float volume;
				public float pitch;
				public float rpm;
				[MarshalAs(UnmanagedType.U1)]
				public bool enabled;
			}

			#endregion

			#region Variables

			public OutputType outputs;
			public CelerationType celerationType;
			public AudioClip startingClip;
			public string folderName;
			[Range(0f, 1f)]
			public float lowRPMVolume = .5f;
			public float maxRPMDistortion;
			public float overRPMDistortion;
			[Range(10f, 22000f)]
			public float decelLowPassFrequency;
			public bool useAccelLowPass;
			[Range(10f, 22000f)]
			public float accelLowPassFrequency = 5000f;
			[Range(0f, 1f)]
			public float accelLowPassRPMEnd = .5f;
			public bool useLowPassDamping;
			public float lowPassDamping;
			public VehicleAudio.EngineMixersGroup mixerGroups;

			#endregion

			#region Constructors

			public AudioModule()
			{
				maxRPMDistortion = .67f;
				overRPMDistortion = .33f;
				decelLowPassFrequency = 5000f;
				accelLowPassFrequency = 22000f;
				accelLowPassRPMEnd = 1f;
				lowPassDamping = 5f;
			}
			public AudioModule(AudioModule module)
			{
				outputs = module.outputs;
				celerationType = module.celerationType;
				maxRPMDistortion = module.maxRPMDistortion;
				overRPMDistortion = module.overRPMDistortion;
				decelLowPassFrequency = module.decelLowPassFrequency;
				useAccelLowPass = module.useAccelLowPass;
				accelLowPassFrequency = module.accelLowPassFrequency;
				accelLowPassRPMEnd = module.accelLowPassRPMEnd;
				useLowPassDamping = module.useLowPassDamping;
				lowPassDamping = module.lowPassDamping;
				mixerGroups = module.mixerGroups;
				startingClip = module.startingClip;
				folderName = module.folderName;
			}

			#endregion

			#region Operators

			public static implicit operator bool(AudioModule module) => module != null;

			#endregion
		}

		internal readonly struct AudioAccess
		{
			#region Variables

			public readonly AudioModule.OutputType outputs;
			public readonly AudioModule.CelerationType celerationType;
			public readonly float lowRPMVolume;
			public readonly float maxRPMDistortion;
			public readonly float overRPMDistortion;
			public readonly float decelLowPassFrequency;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useAccelLowPass;
			public readonly float accelLowPassFrequency;
			public readonly float accelLowPassRPMEnd;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useLowPassDamping;
			public readonly float lowPassDamping;

			#endregion

			#region Constructors

			public AudioAccess(AudioModule module) : this()
			{
				if (!module)
					return;

				outputs = module.outputs;
				celerationType = module.celerationType;
				lowRPMVolume = module.lowRPMVolume;
				maxRPMDistortion = module.maxRPMDistortion;
				overRPMDistortion = module.overRPMDistortion;
				decelLowPassFrequency = module.decelLowPassFrequency;
				useAccelLowPass = module.useAccelLowPass;
				accelLowPassFrequency = module.accelLowPassFrequency;
				accelLowPassRPMEnd = module.accelLowPassRPMEnd;
				useLowPassDamping = module.useLowPassDamping;
				lowPassDamping = module.lowPassDamping;
			}

			#endregion

			#region Operators

			public static implicit operator AudioAccess(AudioModule module) => new(module);

			#endregion
		}

		#endregion

		#region Variables

		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				if (value.IsNullOrEmpty() || value.IsNullOrWhiteSpace())
					value = "New Engine";

				string prefix = default;

				if (Settings && Settings.Engines != null && Settings.Engines.Length > 0)
					for (int i = 0; Array.Find(Settings.Engines, engine => engine != this && engine.Name.ToUpper() == $"{value.ToUpper()}{prefix}"); i++)
						prefix = i > 0 ? $" ({i})" : "";

				name = $"{value}{prefix}";
			}
		}
		public EngineType Type
		{
			get
			{
				return type;
			}
			set
			{
				type = value;
				isElectric = type == EngineType.Electric;

				if (type == EngineType.Rotary)
					fuelType = EngineFuelType.Gas;

				if (IsElectric)
					MinimumRPM = 0f;
				else if (MinimumRPM < 1f)
					MinimumRPM = 1000f;

				RefreshValidity();
			}
		}
		public EngineFuelType FuelType
		{
			get
			{
				return fuelType;
			}
			set
			{
				fuelType = type == EngineType.Rotary ? EngineFuelType.Gas : value;

				RefreshValidity();
			}
		}
		public int CylinderCount
		{
			get
			{
				return cylinderCount;
			}
			set
			{
				cylinderCount = math.clamp(value, 1, 32);
			}
		}
		public float Mass
		{
			get
			{
				return mass;
			}
			set
			{
				mass = math.max(value, 10f);

				RefreshValidity();
			}
		}
		public float MinimumRPM
		{
			get
			{
				return minimumRPM;
			}
			set
			{
				minimumRPM = math.clamp(value, IsElectric ? 0f : 1f, redlineRPM);

				RefreshValidity();
			}
		}
		public float RedlineRPM
		{
			get
			{
				return redlineRPM;
			}
			set
			{
				redlineRPM = math.clamp(value, minimumRPM, overRevRPM);

				RefreshValidity();
			}
		}
		public float OverRevRPM
		{
			get
			{
				return overRevRPM;
			}
			set
			{
				overRevRPM = math.clamp(value, redlineRPM, maximumRPM);

				RefreshValidity();
			}
		}
		public float MaximumRPM
		{
			get
			{
				return maximumRPM;
			}
			set
			{
				maximumRPM = math.max(value, overRevRPM);

				RefreshValidity();
			}
		}
		public float Power
		{
			get
			{
				return power;
			}
			set
			{
				power = math.max(value, 0f);

				RefreshValidity();
			}
		}
		public float PeakPowerRPM
		{
			get
			{
				if (peakPowerRPM == 0f)
					peakPowerRPM = MaxPowerAtRPM(Power, Torque);

				return peakPowerRPM;
			}
			set
			{
				peakPowerRPM = math.clamp(value, MinimumRPM, OverRevRPM);
			}
		}
		public float Torque
		{
			get
			{
				return torque;
			}
			set
			{
				torque = math.max(value, 0f);

				RefreshValidity();
			}
		}
		public float PeakTorqueRPM
		{
			get
			{
				if (peakTorqueRPM == 0f)
					peakTorqueRPM = MaxPowerAtRPM(Power, Torque);

				return peakTorqueRPM;
			}
			set
			{
				peakTorqueRPM = math.clamp(value, MinimumRPM, OverRevRPM);
			}
		}
		public bool IsElectric
		{
			get
			{
				return isElectric;
			}
		}
		public bool IsValid
		{
			get
			{
				if (!isValid)
					RefreshValidity();

				return isValid;
			}
		}
		public AudioModule Audio
		{
			get
			{
				return audio;
			}
			set
			{
				audio = value;
			}
		}

		[SerializeField]
		private string name;
		[SerializeField]
		private EngineType type = EngineType.Inline;
		[SerializeField]
		private EngineFuelType fuelType = EngineFuelType.Gas;
		[SerializeField]
		private float mass = 200f;
		[SerializeField]
		private float minimumRPM = 1000f;
		[SerializeField]
		private float redlineRPM = 5500f;
		[SerializeField]
		private float overRevRPM = 6000f;
		[SerializeField]
		private float maximumRPM = 7000f;
		[SerializeField]
		private float power = 300f;
		[SerializeField]
		private float peakPowerRPM = 5000f;
		[SerializeField]
		private float torque = 400f;
		[SerializeField]
		private float peakTorqueRPM = 3500f;
		[SerializeField]
		private int cylinderCount = 6;
		[SerializeField]
		private bool isElectric;
		[SerializeField]
		private bool isValid = true;
		[SerializeField]
		private AudioModule audio;

		#endregion

		#region Methods

		#region Static Methods

		public static float MaxPowerAtRPM(float power, float torque)
		{
			return torque * Utility.UnitMultiplier(Utility.Units.Torque, Utility.UnitType.Imperial) * 5252f / power;
		}
		public static float MaxTorqueAtRPM(float power, float torque)
		{
			return power * 5252f / torque / Utility.UnitMultiplier(Utility.Units.Power, Utility.UnitType.Imperial);
		}
		public static float PowerToTorque(float power, float rpm)
		{
			return power * Utility.UnitMultiplier(Utility.Units.Power, Utility.UnitType.Imperial) * 5252f / rpm;
		}
		public static float TorqueToPower(float torque, float rpm)
		{
			return torque * Utility.UnitMultiplier(Utility.Units.Torque, Utility.UnitType.Imperial) * rpm / 5252f;
		}

		#endregion

		#region Global Methods

		public float Inertia(Vehicle.BehaviourModule behaviour)
		{
			float rate = InertiaRate(behaviour);

			if (Type != EngineType.Electric)
				switch (FuelType)
				{
					case EngineFuelType.Diesel:
						rate = Utility.Lerp(1f, 3.75f, InertiaRate(behaviour));

						break;

					case EngineFuelType.Gas:
						rate = Utility.Lerp(1.3333f, 3.25f, InertiaRate(behaviour));

						break;
				}

			return rate;
		}
		public float InertiaRate(Vehicle.BehaviourModule behaviour)
		{
			if (Type == EngineType.Electric)
				return 1f;

			float inertia = Type switch
			{
				EngineType.Boxer => .9f,
				EngineType.Inline => .8333f,
				EngineType.V => 1f,
				EngineType.W => .875f,
				_ => 1f
			};

			return Utility.Lerp(.5f, 1f, Utility.Average(inertia, Utility.InverseLerpUnclamped(200f, 500f, math.max(behaviour.Torque, 200f))));
		}

		private void RefreshValidity()
		{
			isValid = (MinimumRPM > 0f || IsElectric && MinimumRPM == 0f) && RedlineRPM > MinimumRPM && OverRevRPM > RedlineRPM && MaximumRPM > OverRevRPM && (type != EngineType.Rotary || FuelType == EngineFuelType.Gas);
		}

		#endregion

		#endregion

		#region Constructors

		public VehicleEngine(string name)
		{
			Name = name;
			audio = new();
		}
		public VehicleEngine(VehicleEngine engine)
		{
			Name = engine.Name;
			type = engine.Type;
			fuelType = engine.FuelType;
			mass = engine.Mass;
			minimumRPM = engine.MinimumRPM;
			redlineRPM = engine.RedlineRPM;
			overRevRPM = engine.OverRevRPM;
			maximumRPM = engine.MaximumRPM;
			power = engine.Power;
			peakPowerRPM = engine.PeakPowerRPM;
			torque = engine.Torque;
			peakTorqueRPM = engine.PeakTorqueRPM;
			cylinderCount = engine.CylinderCount;
			isElectric = engine.IsElectric;
			isValid = engine.IsValid;
			audio = new(engine.Audio);
		}

		#endregion

		#region Operators

		public static implicit operator bool(VehicleEngine engine) => (engine as object) != null;

		#endregion
	}

	internal readonly struct VehicleEngineAccess
	{
		#region Variables

		public readonly VehicleEngine.EngineType type;
		public readonly VehicleEngine.EngineFuelType fuelType;
		public readonly float mass;
		public readonly float minimumRPM;
		public readonly float redlineRPM;
		public readonly float overRevRPM;
		public readonly float maximumRPM;
		public readonly float power;
		public readonly float peakPowerRPM;
		public readonly float torque;
		public readonly float peakTorqueRPM;
		public readonly int cylinderCount;
		[MarshalAs(UnmanagedType.U1)]
		public readonly bool isElectric;
		public readonly VehicleEngine.AudioAccess audio;

		#endregion

		#region Constructors

		public VehicleEngineAccess(VehicleEngine engine) : this()
		{
			if (!engine)
				return;

			type = engine.Type;
			fuelType = engine.FuelType;
			mass = engine.Mass;
			minimumRPM = engine.MinimumRPM;
			redlineRPM = engine.RedlineRPM;
			overRevRPM = engine.OverRevRPM;
			maximumRPM = engine.MaximumRPM;
			power = engine.Power;
			peakPowerRPM = engine.PeakPowerRPM;
			torque = engine.Torque;
			peakTorqueRPM = engine.Torque;
			peakTorqueRPM = engine.PeakTorqueRPM;
			cylinderCount = engine.CylinderCount;
			isElectric = engine.IsElectric;
			audio = engine.Audio;
		}

		#endregion

		#region Operators

		public static implicit operator VehicleEngineAccess(VehicleEngine engine) => new(engine);

		#endregion
	}
}

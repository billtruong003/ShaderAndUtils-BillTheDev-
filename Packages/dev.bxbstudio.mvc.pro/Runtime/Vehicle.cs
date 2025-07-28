#region Namespaces

using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Serialization;
using Utilities;
using Utilities.Inputs;
using MVC.Base;
using MVC.AI;
using MVC.IK;
using MVC.VFX;
using MVC.UI;
using MVC.Utilities;
using MVC.Utilities.Internal;

using Input = UnityEngine.Input;
using Object = UnityEngine.Object;

#endregion

namespace MVC.Core
{
	[AddComponentMenu("")]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(-50)]
	public class Vehicle : ToolkitBehaviour
	{
		#region Enumerators

		public enum VehicleType { Car, HeavyTruck }
		public enum CarClass { AmericanMuscle, Elite, F1, Road, Super, Electric }
		public enum HeavyTruckClass { Road, Sport, Electric }
		public enum Train { None, FWD, RWD, AWD }
		public enum Aspiration { Natural, Turbocharger, Supercharger, Mix }

		#endregion

		#region Modules

		[Serializable]
		public class BehaviourModule
		{
			#region Variables

			public VehicleType vehicleType;
			public CarClass carClass = CarClass.Road;
			public HeavyTruckClass heavyTruckClass;
			public float CurbWeight
			{
				get
				{
					return curbWeight + turbochargerWeight + superchargerWeight;
				}
				set
				{
					float newCurbWeight = value;
					var chargers = Settings.Chargers;

					if (isTurbocharged && !HasInternalErrors)
					{
						var turbocharger = chargers[TurbochargerIndex];

						if (!turbocharger.IsStock)
							newCurbWeight -= turbochargerWeight = turbocharger.MassDifference;
						else
							turbochargerWeight = default;
					}

					if (isSupercharged && !HasInternalErrors)
					{
						var supercharger = chargers[SuperchargerIndex];

						if (!supercharger.IsStock)
							newCurbWeight -= superchargerWeight = supercharger.MassDifference;
						else
							superchargerWeight = default;
					}

					curbWeight = math.max(newCurbWeight, 10f);
				}
			}
			public int EngineIndex
			{
				get
				{
					var engines = Settings.Engines;
					int enginesCount = engines.Length;

					if (engineIndex > -1 && engineIndex < enginesCount && engineName.IsNullOrEmpty())
						engineName = engines[engineIndex].Name;
					else if (!engineName.IsNullOrEmpty() && (!engine || engine && engine.Name != engineName))
					{
						VehicleEngine engine = engines.FirstOrDefault(eng => eng.Name == engineName);

						if (engine)
						{
							engineIndex = Array.IndexOf(engines, engine);
							this.engine = engine;
						}
					}

					return engineIndex;
				}
				set
				{
					if (!vehicle || HasInternalErrors)
						return;
					else if (engineIndex == value || value < -1)
						return;

					var engines = Settings.Engines;
					int enginesCount = engines.Length;

					if (value >= enginesCount || !engines[value].IsValid)
						return;

					engineIndex = value;

					if (engineIndex > -1)
					{
						engine = engines[engineIndex];
						engineName = engine?.Name;

						RegenerateCurves();
					}
					else
					{
						engine = null;
						engineName = string.Empty;
						powerCurve = new();
						torqueCurve = new();
					}
				}
			}
			public Aspiration Aspiration
			{
				get
				{
					return aspiration;
				}
				set
				{
					aspiration = value;
					isTurbocharged = aspiration == Aspiration.Turbocharger || aspiration == Aspiration.Mix;
					isSupercharged = aspiration == Aspiration.Supercharger || aspiration == Aspiration.Mix;
					turbochargerIndex = -1;
					superchargerIndex = -1;
				}
			}
			public int TurbochargerIndex
			{
				get
				{
					if (!isTurbocharged)
						return -1;

					var chargers = Settings.Chargers;
					int chargersCount = chargers.Length;
					bool emptyName = turbochargerName.IsNullOrEmpty();

					if (emptyName && (turbochargerIndex < 0 || turbochargerIndex >= chargersCount))
					{
						if (HasInternalErrors)
							return -1;

						turbochargerIndex = Array.FindIndex(chargers, charger => charger.Type == VehicleCharger.ChargerType.Supercharger);

						if (turbochargerIndex < 0)
							turbochargerIndex = default;

						if (turbochargerIndex < chargersCount)
						{
							var turbocharger = chargers[turbochargerIndex];

							turbochargerName = turbocharger.Name;
							this.turbocharger = turbocharger;
						}
					}
					else if (emptyName && turbochargerIndex > -1 && turbochargerIndex < chargersCount)
					{
						if (HasInternalErrors)
							return -1;

						turbochargerName = chargers[turbochargerIndex].Name;
					}
					else if (!emptyName && (!turbocharger || turbocharger.Name != turbochargerName))
					{
						if (HasInternalErrors)
							return -1;

						turbochargerIndex = Array.FindIndex(chargers, charger => charger.Name == turbochargerName);

						if (turbochargerIndex > -1)
							turbocharger = chargers[turbochargerIndex];
					}

					return turbochargerIndex;
				}
				set
				{
					if (HasInternalErrors)
						return;

					var chargers = Settings.Chargers;
					int chargersCount = chargers.Length;

					if (value < 0 || value >= chargersCount)
						throw new IndexOutOfRangeException();
					else if (chargers[value].Type != VehicleCharger.ChargerType.Turbocharger)
						throw new InvalidCastException("Cannot assign a non-turbocharger index");

					turbochargerIndex = value;
					turbocharger = chargers[value];

					if (turbocharger)
					{
						turbochargerName = turbocharger.Name;
						turbochargerWeight = !turbocharger.IsStock ? turbocharger.MassDifference : 0;
					}
				}
			}
			public int SuperchargerIndex
			{
				get
				{
					if (!isSupercharged)
						return -1;

					var chargers = Settings.Chargers;
					int chargersCount = chargers.Length;
					bool emptyName = superchargerName.IsNullOrEmpty();

					if (emptyName && (superchargerIndex < 0 || superchargerIndex >= chargersCount))
					{
						if (HasInternalErrors)
							return -1;

						superchargerIndex = Array.FindIndex(chargers, charger => charger.Type == VehicleCharger.ChargerType.Supercharger);

						if (superchargerIndex < 0)
							superchargerIndex = default;

						if (superchargerIndex < chargersCount)
						{
							var supercharger = chargers[superchargerIndex];

							superchargerName = supercharger.Name;
							this.supercharger = supercharger;
						}
					}
					else if (emptyName && superchargerIndex > -1 && superchargerIndex < chargersCount)
					{
						if (HasInternalErrors)
							return -1;

						superchargerName = chargers[superchargerIndex].Name;
					}
					else if (!emptyName && (!supercharger || supercharger.Name != superchargerName))
					{
						if (HasInternalErrors)
							return -1;

						superchargerIndex = Array.FindIndex(chargers, charger => charger.Name == superchargerName);

						if (superchargerIndex > -1)
							supercharger = chargers[superchargerIndex];
					}

					return superchargerIndex;
				}
				set
				{
					if (HasInternalErrors)
						return;

					var chargers = Settings.Chargers;
					int chargersCount = chargers.Length;

					if (value < 0 || value >= chargersCount)
						throw new IndexOutOfRangeException();
					else if (chargers[value].Type != VehicleCharger.ChargerType.Supercharger)
						throw new InvalidCastException("Cannot assign a non-supercharger index");

					superchargerIndex = value;
					supercharger = chargers[value];

					if (supercharger)
					{
						superchargerName = supercharger.Name;
						superchargerWeight = !supercharger.IsStock ? supercharger.MassDifference : 0;
					}
				}
			}
			public bool IsTurbocharged
			{
				get
				{
					return isTurbocharged;
				}
			}
			public bool IsSupercharged
			{
				get
				{
					return isSupercharged;
				}
			}
			public float Power
			{
				get
				{
					if (!Engine)
						return default;

					return math.max(engine.Power + powerOffset, 0f);
				}
			}
			public float PowerOffset
			{
				get
				{
					return powerOffset;
				}
				set
				{
					powerOffset = value;

					if (AutoCurves)
						RegeneratePowerCurve();
				}
			}
			public float PeakPowerRPM
			{
				get
				{
					return !Engine || OverridePeakPowerRPM ? peakPowerRPM : Engine.PeakPowerRPM;
				}
				set
				{
					peakPowerRPM = math.clamp(value, Engine.MinimumRPM, Engine.RedlineRPM);

					if (AutoCurves)
						RegeneratePowerCurve();
				}
			}
			public bool OverridePeakPowerRPM
			{
				get
				{
					return overridePeakPowerRPM;
				}
				set
				{
					overridePeakPowerRPM = value;

					if (AutoCurves)
						RegeneratePowerCurve();
				}
			}
			public float Torque
			{
				get
				{
					if (!Engine)
						return default;

					return math.max(engine.Torque + torqueOffset, 0f);
				}
			}
			public float TorqueOffset
			{
				get
				{
					return torqueOffset;
				}
				set
				{
					torqueOffset = value;

					if (AutoCurves)
						RegenerateTorqueCurve(LinkPowerTorqueCurves);
				}
			}
			public float PeakTorqueRPM
			{
				get
				{
					return !Engine || OverridePeakTorqueRPM ? peakTorqueRPM : Engine.PeakTorqueRPM;
				}
				set
				{
					if (!Engine)
						return;

					peakTorqueRPM = math.clamp(value, Engine.MinimumRPM, Engine.RedlineRPM);

					if (AutoCurves)
						RegenerateTorqueCurve(LinkPowerTorqueCurves);
					else
#if UNITY_EDITOR
						if (Application.isPlaying)
#endif
						vehicle.RefreshGearShiftRPMFactor();
				}
			}
			public bool OverridePeakTorqueRPM
			{
				get
				{
					return overridePeakTorqueRPM;
				}
				set
				{
					overridePeakTorqueRPM = value;

					if (AutoCurves)
						RegenerateTorqueCurve(LinkPowerTorqueCurves);
					else
#if UNITY_EDITOR
						if (Application.isPlaying)
#endif
						vehicle.RefreshGearShiftRPMFactor();
				}
			}
			public float TorqueOutputScale
			{
				get
				{
					return torqueOutputScale;
				}
				set
				{
					torqueOutputScale = math.max(value, 0f);
				}
			}
			[Obsolete("Use `TorqueOutputScale` instead.", true)]
			public float powerTorqueOutputScale;
			[Obsolete("Use `Transmission.CenterDifferential.BiasAB` instead.", true)]
			public float frontRearOutputFactor = .5f;
			public AnimationCurve PowerCurve
			{
				get
				{
					if (powerCurve == null || powerCurve.length < 2)
						RegeneratePowerCurve();
					else if (!Application.isPlaying && autoCurves)
					{
						float maxPowerValue = VehicleEngine.CurveSheet.MaxValueInCurve(powerCurve);

						if (!Mathf.Approximately(maxPowerValue, Power))
							RegeneratePowerCurve();
					}

					return powerCurve;
				}
				set
				{
					if (autoCurves)
						return;

					powerCurve = value;
				}
			}
			public AnimationCurve TorqueCurve
			{
				get
				{
					if (torqueCurve == null || torqueCurve.length < 2)
						RegenerateTorqueCurve(LinkPowerTorqueCurves);
					else if (!Application.isPlaying && AutoCurves)
					{
						float maxTorqueValue = VehicleEngine.CurveSheet.MaxValueInCurve(torqueCurve);

						if (!Mathf.Approximately(maxTorqueValue, Torque))
							RegenerateTorqueCurve(LinkPowerTorqueCurves);
					}

					return torqueCurve;
				}
				set
				{
					if (AutoCurves)
						return;

					torqueCurve = value;
				}
			}
			public bool AutoCurves
			{
				get
				{
					return autoCurves;
				}
				set
				{
					autoCurves = value;

					if (autoCurves)
						RegenerateCurves();
				}
			}
			public bool LinkPowerTorqueCurves
			{
				get
				{
					return linkPowerTorqueCurves;
				}
				set
				{
					linkPowerTorqueCurves = value;

					if (AutoCurves)
						RegenerateCurves();
				}
			}
			public float TopSpeed
			{
				get
				{
					return topSpeed;
				}
				set
				{
					if (!vehicle.Transmission.AutoGearRatios)
						return;

					topSpeed = math.max(value, 1f);

					vehicle.transmission.RefreshGears();
				}
			}
			public float FuelCapacity
			{
				get
				{
					return fuelCapacity;
				}
				set
				{
					fuelCapacity = math.max(value, 0f);
				}
			}
			public Utility.Precision fuelConsumptionPrecision = Utility.Precision.Advanced;
			public float FuelConsumptionCity
			{
				get
				{
					if (fuelConsumptionPrecision == Utility.Precision.Simple)
						return FuelConsumptionCombined;

					return fuelConsumptionCity;
				}
				set
				{
					if (fuelConsumptionPrecision == Utility.Precision.Simple)
						return;

					fuelConsumptionCity = math.max(value, 0f);
				}
			}
			public float FuelConsumptionHighway
			{
				get
				{
					if (fuelConsumptionPrecision == Utility.Precision.Simple)
						return FuelConsumptionCombined;

					return fuelConsumptionHighway;
				}
				set
				{
					if (fuelConsumptionPrecision == Utility.Precision.Simple)
						return;

					fuelConsumptionHighway = math.max(value, 0f);
				}
			}
			public float FuelConsumptionCombined
			{
				get
				{
					if (fuelConsumptionPrecision != Utility.Precision.Simple)
						return Utility.Average(FuelConsumptionCity, FuelConsumptionHighway);

					return fuelConsumptionCombined;
				}
				set
				{
					if (fuelConsumptionPrecision != Utility.Precision.Simple)
						return;

					fuelConsumptionCombined = math.max(value, 0f);
				}
			}
			public bool UseRegenerativeBrakes
			{
				get
				{
					return vehicle.IsElectric && useRegenerativeBrakes;
				}
				set
				{
					if (!vehicle.IsElectric)
						return;

					useRegenerativeBrakes = value;
				}
			}
			public bool useRevLimiter = true;
			public bool useExhaustEffects = true;
			public float ExhaustFlameEmissionProbability
			{
				get
				{
					if (!useExhaustEffects)
						return default;

					return exhaustFlameEmissionProbability;
				}
				set
				{
					if (!useExhaustEffects)
						return;

					exhaustFlameEmissionProbability = Utility.Clamp01(value);
				}
			}
			public bool useNOS;
			public int NOSBottlesCount
			{
				get
				{
					return m_NOSBottlesCount;
				}
				set
				{
					m_NOSBottlesCount = math.clamp(value, 1, 10);
				}
			}
			public float NOSCapacity
			{
				get
				{
					return m_NOSCapacity;
				}
				set
				{
					m_NOSCapacity = math.clamp(value, 0f, math.round(20f / 2.20462262185f));
				}
			}
			public float NOSBoost
			{
				get
				{
					return m_NOSBoost;
				}
				set
				{
					m_NOSBoost = Utility.Clamp01(value);
				}
			}
			public float NOSConsumption
			{
				get
				{
					return m_NOSConsumption;
				}
				set
				{
					m_NOSConsumption = Utility.Clamp01(value);
				}
			}
			public float NOSRegenerateTime
			{
				get
				{
					return m_NOSRegenerateTime;
				}
				set
				{
					m_NOSRegenerateTime = math.clamp(value, 0f, 30f);
				}
			}
			public BrakeModule FrontBrakes
			{
				get
				{
					if (!frontBrakes)
						frontBrakes = new();

					return frontBrakes;
				}
			}
			public BrakeModule RearBrakes
			{
				get
				{
					if (!rearBrakes)
						rearBrakes = new();

					return rearBrakes;
				}
			}

			internal VehicleEngine Engine
			{
				get
				{
					var engines = Settings.Engines;
					int enginesCount = engines != null ? engines.Length : 0;

					if (HasInternalErrors || EngineIndex < -1 || engineIndex > enginesCount)
						engineIndex = -1;

					if (EngineIndex < 0)
						engine = null;
					else if (engines != null)
					{
						VehicleEngine newEngine = engines[engineIndex];

						if (!engine || engine != newEngine)
							engine = newEngine;
					}

					return engine;
				}
			}
			internal VehicleCharger Turbocharger
			{
				get
				{
					int newTurbochargerIndex = TurbochargerIndex;
					var chargers = Settings.Chargers;
					int chargersCount = chargers != null ? chargers.Length : 0;

					if (HasInternalErrors || newTurbochargerIndex < -1 || newTurbochargerIndex >= chargersCount)
						turbochargerIndex = newTurbochargerIndex = -1;

					if (newTurbochargerIndex < 0)
						turbocharger = null;
					else if (chargers != null && (!turbocharger || turbocharger != chargers[newTurbochargerIndex]))
					{
						turbocharger = chargers[newTurbochargerIndex];
						turbochargerName = turbocharger?.Name;
					}

					return turbocharger;
				}
			}
			internal VehicleCharger Supercharger
			{
				get
				{
					int newSuperchargerIndex = SuperchargerIndex;
					var chargers = Settings.Chargers;
					int chargersCount = chargers != null ? chargers.Length : 0;

					if (HasInternalErrors || newSuperchargerIndex < -1 || newSuperchargerIndex >= chargersCount)
						superchargerIndex = newSuperchargerIndex = -1;

					if (newSuperchargerIndex < 0)
						supercharger = null;
					else if (chargers != null && (!supercharger || supercharger != chargers[newSuperchargerIndex]))
					{
						supercharger = chargers[newSuperchargerIndex];
						superchargerName = supercharger?.Name;
					}

					return supercharger;
				}
			}
			internal VehicleEngine engine;
			internal VehicleCharger turbocharger;
			internal VehicleCharger supercharger;
			internal bool IsValid
			{
				get
				{
					return isValid;
				}
			}

#pragma warning disable IDE0044 // Add readonly modifier
			[SerializeField]
			private bool isValid;
			[SerializeField]
			private Vehicle vehicle;
#pragma warning restore IDE0044 // Add readonly modifier
			[SerializeField]
			private float curbWeight = 1500f;
			[SerializeField]
			private float turbochargerWeight;
			[SerializeField]
			private float superchargerWeight;
			[SerializeField]
			private string engineName;
			[SerializeField]
			private int engineIndex;
			[SerializeField]
			private Aspiration aspiration = Aspiration.Natural;
			[SerializeField]
			private bool isTurbocharged;
			[SerializeField]
			private bool isSupercharged;
			[SerializeField]
			private string turbochargerName;
			[SerializeField]
			private int turbochargerIndex;
			[SerializeField]
			private string superchargerName;
			[SerializeField]
			private int superchargerIndex;
			[SerializeField]
			private float powerOffset;
			[SerializeField]
			private float peakPowerRPM = 5000f;
			[SerializeField]
			private bool overridePeakPowerRPM;
			[SerializeField]
			private float torqueOffset;
			[SerializeField]
			private float peakTorqueRPM = 3500f;
			[SerializeField]
			private bool overridePeakTorqueRPM;
			[SerializeField, FormerlySerializedAs("powerTorqueOutputScale")]
			private float torqueOutputScale = 1f;
			[SerializeField]
			private AnimationCurve powerCurve;
			[SerializeField]
			private AnimationCurve torqueCurve;
			[SerializeField]
			private bool autoCurves = true;
			[SerializeField]
			private bool linkPowerTorqueCurves = true;
			[SerializeField]
			private float topSpeed = 270f;
			[SerializeField]
			private float fuelCapacity = 50f;
			[SerializeField]
			private float fuelConsumptionCity = 20f;
			[SerializeField]
			private float fuelConsumptionHighway = 10f;
			[SerializeField]
			private float fuelConsumptionCombined = 15f;
			[SerializeField]
			private bool useRegenerativeBrakes = true;
			[SerializeField]
			private float exhaustFlameEmissionProbability = .5f;
			[SerializeField]
			private int m_NOSBottlesCount = 4;
			[SerializeField]
			private float m_NOSCapacity = 1f;
			[SerializeField]
			private float m_NOSBoost = .5f;
			[SerializeField]
			private float m_NOSConsumption = .5f;
			[SerializeField]
			private float m_NOSRegenerateTime = 3f;
			[SerializeField]
			private BrakeModule frontBrakes;
			[SerializeField]
			private BrakeModule rearBrakes;

			#endregion

			#region Methods

			public void RegenerateCurves()
			{
				VehicleEngine.CurveSheet sheet = new(vehicle);

				powerCurve = sheet.GetPowerCurve();
				torqueCurve = sheet.GetTorqueCurve(linkPowerTorqueCurves);

#if UNITY_EDITOR
				if (Application.isPlaying)
#endif
					vehicle.RefreshGearShiftRPMFactor();
			}
			public void RegeneratePowerCurve()
			{
				VehicleEngine.CurveSheet sheet = new(vehicle);

				powerCurve = sheet.GetPowerCurve();
			}
			public void RegenerateTorqueCurve(bool powerCurveDependent)
			{
				VehicleEngine.CurveSheet sheet = new(vehicle);

				torqueCurve = sheet.GetTorqueCurve(powerCurveDependent);
			}

			#endregion

			#region Constructors

			public BehaviourModule(Vehicle vehicle)
			{
				isValid = vehicle;
				this.vehicle = vehicle;
			}

			#endregion

			#region Operators

			public static implicit operator bool(BehaviourModule module) => module != null;

			#endregion
		}
		[Serializable]
		public class BrakeModule
		{
			#region Enumerators

			public enum BrakeType { Steel, DrilledSteel, SlottedSteel, CarbonCeramic, Drum }

			#endregion

			#region Variables

			public BrakeType Type
			{
				get
				{
					return type;
				}
				set
				{
					density = value switch
					{
						BrakeType.DrilledSteel => 4500f,
						BrakeType.SlottedSteel => 3500f,
						BrakeType.CarbonCeramic => 1900f,
						BrakeType.Drum => 7250f,
						_ => 5000f,
					};
					type = value;
					brakeHeatThreshold = Utility.Clamp01(1f - (Density / 7250f));

					RecalculateBrakeTorque();
				}
			}
			public float Density
			{
				get
				{
					return density;
				}
			}
			public float Diameter
			{
				get
				{
					return diameter;
				}
				set
				{
					diameter = math.max(value, .01f);

					RecalculateBrakeTorque();
				}
			}
			public int Pistons
			{
				get
				{
					return pistons;
				}
				set
				{
					pistons = math.max(value, 1);

					RecalculateBrakeTorque();
				}
			}
			public int Pads
			{
				get
				{
					return pads;
				}
				set
				{
					pads = math.max(value, 1);

					RecalculateBrakeTorque();
				}
			}
			public float Pressure
			{
				get
				{
					return pressure;
				}
				set
				{
					pressure = math.max(value, 1f);

					RecalculateBrakeTorque();
				}
			}
			public float Friction
			{
				get
				{
					return friction;
				}
				set
				{
					friction = math.max(value, .01f);

					RecalculateBrakeTorque();
				}
			}
			public float BrakeTorque
			{
				get
				{
					if (brakeTorque < 10f)
						RecalculateBrakeTorque();

					return brakeTorque;
				}
				set
				{
					brakeTorque = math.max(value, 10f);
				}
			}
			public float BrakeHeatThreshold
			{
				get
				{
					if (brakeHeatThreshold == 0f)
						brakeHeatThreshold = 1f - Density * .0001f;

					return brakeHeatThreshold;
				}
			}

			[SerializeField]
			private BrakeType type;
			[SerializeField]
			private float density = 5000f;
			[SerializeField]
			private float diameter = .2f;
			[SerializeField]
			private int pistons = 2;
			[SerializeField]
			private int pads = 2;
			[SerializeField]
			private float pressure = 70f;
			[SerializeField]
			private float friction = .9f;
			[SerializeField]
			private float brakeTorque;
			[SerializeField]
			private float brakeHeatThreshold;

			#endregion

			#region Methods

			private void RecalculateBrakeTorque()
			{
				if (type == BrakeType.Drum)
				{
					float actuatorRadius = diameter / 3f;
					float pinRadius = diameter * 2.5f / 6f;
					float pinAngle = 120f * Mathf.Deg2Rad;
					float force = actuatorRadius + pinRadius * math.cos(pinAngle);
					float shoeBeginAngle = 5f * Mathf.Deg2Rad;
					float shoeSpanAngle = 120f * Mathf.Deg2Rad;

					BrakeTorque = force * friction * pressure * 2f * math.pow(diameter * .5f, 2f) * (math.cos(shoeBeginAngle) - math.cos(shoeSpanAngle)) * 10000f / math.sin(shoeSpanAngle);
				}
				else
					BrakeTorque = friction * math.PI * Pads * Diameter * Pressure * Pistons * 10f;
			}

			#endregion

			#region Operators

			public static implicit operator bool(BrakeModule module) => module != null;

			#endregion
		}
		[Serializable]
		public class TransmissionModule
		{
			#region Enumerators

			public enum GearboxType { Manual, Automatic }

			#endregion

			#region Variables

			public GearboxType Gearbox
			{
				get
				{
					return gearboxType;
				}
				set
				{
					gearboxType = value;
				}
			}
			public bool UseDoubleGearbox
			{
				get
				{
					if (vehicle && vehicle.Drivetrain != Train.AWD || gearboxType == GearboxType.Manual)
						return false;

					return useDoubleGearbox;
				}
				set
				{
					if (!vehicle || vehicle.Drivetrain != Train.AWD || gearboxType == GearboxType.Manual)
						return;

					useDoubleGearbox = value;
				}
			}
			public int ClipsGroupIndex
			{
				get
				{
					if (HasInternalErrors)
						return -1;

					var transmissionWhineGroups = Settings.transmissionWhineGroups;
					int transmissionWhineGroupsCount = transmissionWhineGroups.Length;

					if (this.transmissionWhineGroupsCount != transmissionWhineGroupsCount)
					{
						clipsGroupIndex = math.clamp(clipsGroupIndex, -1, transmissionWhineGroupsCount - 1);
						this.transmissionWhineGroupsCount = transmissionWhineGroupsCount;
					}

					return clipsGroupIndex;
				}
				set
				{
					if (HasInternalErrors)
						return;

					clipsGroupIndex = math.clamp(value, -1, Settings.transmissionWhineGroups.Length - 1);
				}
			}
			public int PlayClipsStartingFromGear
			{
				get
				{
					return playClipsGroupStartingFromGear;
				}
				set
				{
					playClipsGroupStartingFromGear = math.clamp(value, 1, gearsCount);
				}
			}
			public float MaximumTorque
			{
				get
				{
					return maximumTorque;
				}
				set
				{
					if (maximumTorque == value)
						return;

					maximumTorque = math.max(value, 0f);

					RefreshEfficiency();
				}
			}
			public float Efficiency
			{
				get
				{
					var behaviour = vehicle.Behaviour;
					var engine = vehicle.Engine;

					if (engine && behaviour && (engineTorque != engine.Torque || powerTorqueOutputScale != behaviour.TorqueOutputScale))
						RefreshEfficiency();

					return efficiency;
				}
			}
			public float ShiftDelay
			{
				get
				{
					return shiftDelay;
				}
				set
				{
					shiftDelay = math.clamp(value, .001f, 1f);
				}
			}
			public float ClutchInDelay
			{
				get
				{
					return clutchInDelay;
				}
				set
				{
					clutchInDelay = math.clamp(value, .001f, 1f);
				}
			}
			public float ClutchOutDelay
			{
				get
				{
					return clutchOutDelay;
				}
				set
				{
					clutchOutDelay = math.clamp(value, .001f, 1f);
				}
			}
			public int GearsCount
			{
				get
				{
					return gearsCount;
				}
				set
				{
					gearsCount = math.max(value, 1);

					RefreshGears();
				}
			}
			public bool AutoGearRatios
			{
				get
				{
					if (UseDoubleGearbox)
						return false;

					return autoGearRatios;
				}
				set
				{
					if (UseDoubleGearbox)
						return;

					autoGearRatios = value;

					RefreshGears();
				}
			}
			public float FinalGearRatio
			{
				get
				{
					return finalGearRatio;
				}
				set
				{
					finalGearRatio = math.max(value, .1f);

					RefreshGears();
				}
			}
			public float GearShiftTorque
			{
				get
				{
					return vehicle ? gearShiftTorqueMultiplier * vehicle.Behaviour.Torque : gearShiftTorqueMultiplier;
				}
			}
			public float GearShiftTorqueMultiplier
			{
				get
				{
					return gearShiftTorqueMultiplier;
				}
				set
				{
					gearShiftTorqueMultiplier = math.clamp(value, 0f, 1f);

#if UNITY_EDITOR
					if (Application.isPlaying)
#endif
						vehicle.RefreshGearShiftRPMFactor();
				}
			}
			public VehicleDifferential FrontDifferential
			{
				get
				{
					if (vehicle.Drivetrain != Train.FWD && vehicle.drivetrain != Train.AWD)
						return null;

					if (!frontDifferential)
						frontDifferential = new();

					return frontDifferential;
				}
			}
			public VehicleDifferential CenterDifferential
			{
				get
				{
					if (vehicle.Drivetrain != Train.AWD)
						return null;

					if (!centerDifferential)
						centerDifferential = new();

					return centerDifferential;
				}
			}
			public VehicleDifferential RearDifferential
			{
				get
				{
					if (vehicle.Drivetrain != Train.RWD && vehicle.drivetrain != Train.AWD)
						return null;

					if (!rearDifferential)
						rearDifferential = new();

					return rearDifferential;
				}
			}

			internal bool IsValid
			{
				get
				{
					return isValid;
				}
			}

#pragma warning disable IDE0044 // Add readonly modifier
			[SerializeField]
			private bool isValid;
			[SerializeField]
			private Vehicle vehicle;
#pragma warning restore IDE0044 // Add readonly modifier
			[SerializeField]
			private GearboxType gearboxType = GearboxType.Manual;
			[SerializeField]
			private bool useDoubleGearbox;
			[SerializeField]
			private int clipsGroupIndex = -1;
			[SerializeField]
			private int playClipsGroupStartingFromGear = 1;
			[SerializeField]
			private float maximumTorque = 1000f;
			[SerializeField]
			private float efficiency;
			[SerializeField]
			private float clutchInDelay = .05f;
			[SerializeField]
			private float clutchOutDelay = .05f;
			[SerializeField]
			private float shiftDelay = .15f;
			[SerializeField]
			private int gearsCount = 5;
			[SerializeField]
			private bool autoGearRatios = true;
			[SerializeField]
			private float reverseSpeedTarget = 50f;
			[SerializeField]
			private float reverseMinSpeedTarget = 5f;
			[SerializeField]
			private float reverseGearRatio = 2f;
			[SerializeField]
			private float reverseGearRatio2 = 2f;
			[SerializeField]
			private float[] speedTargets;
			[SerializeField]
			private float[] minSpeedTargets;
			[SerializeField]
			private float[] gearRatios;
			[SerializeField]
			private float[] gearRatios2;
			[SerializeField]
			private float[] gearShiftUpOverrideSpeeds;
			[SerializeField]
			private bool[] overrideGearShiftUpSpeeds;
			[SerializeField]
			private float finalGearRatio = 4f;
			[SerializeField]
			private float gearShiftTorqueMultiplier = .5f;
			[SerializeField]
			private VehicleDifferential frontDifferential;
			[SerializeField]
			private VehicleDifferential centerDifferential;
			[SerializeField]
			private VehicleDifferential rearDifferential;
			private float engineTorque;
			private float powerTorqueOutputScale;
			private int transmissionWhineGroupsCount;

			#endregion

			#region Methods

			#region Static Methods

			public static float GetRatioFromSpeed(float speed, float wheelRadius, float rpm)
			{
				return Utility.RPMToSpeed(rpm, wheelRadius) / speed;
			}
			public static float GetSpeedFromRatio(float ratio, float wheelRadius, float rpm)
			{
				return Utility.RPMToSpeed(rpm, wheelRadius) / ratio;
			}

			#endregion

			#region Global Methods

			public float GetSpeedTarget(int direction, int gearIndex)
			{
				if (gearIndex < 0 || gearIndex >= gearsCount)
				{
					ToolkitDebug.Error("We couldn't get the requested vehicle speed target, it seems like the gear index is out of range!", vehicle);

					return default;
				}

				if (speedTargets == null || speedTargets.Length != gearsCount)
					RefreshGears();

				if (direction < 0)
					return reverseSpeedTarget;

				return speedTargets[gearIndex];
			}
			public float GetMinSpeedTarget(int direction, int gearIndex)
			{
				if (gearIndex < 0 || gearIndex >= gearsCount)
				{
					ToolkitDebug.Error($"We couldn't get the requested vehicle speed target, it seems like the gear index is out of range!", vehicle);

					return default;
				}

				if (minSpeedTargets == null || minSpeedTargets.Length != gearsCount)
					RefreshGears();

				if (direction < 0)
					return reverseMinSpeedTarget;

				return minSpeedTargets[gearIndex];
			}
			public float GetGearRatio(int direction, int gearIndex, bool applyFinalRatio = false, bool secondGearbox = false)
			{
				if (gearIndex < 0 || gearIndex >= gearsCount)
				{
					ToolkitDebug.Error($"We couldn't get the requested vehicle gear ratio, it seems like the gear index is out of range!", vehicle);

					return default;
				}

				bool useDoubleGearbox = UseDoubleGearbox;

				secondGearbox &= useDoubleGearbox;

				if (gearRatios == null || gearRatios.Length != gearsCount || useDoubleGearbox && (gearRatios2 == null || gearRatios2.Length != gearsCount))
					RefreshGears();

				float ratio = direction >= 0 ? (secondGearbox ? gearRatios2[gearIndex] : gearRatios[gearIndex]) : secondGearbox ? reverseGearRatio2 : reverseGearRatio;

				if (applyFinalRatio)
					ratio *= finalGearRatio;

				return ratio;
			}
			public void SetSpeedTarget(int direction, int gearIndex, float speed)
			{
				if (!AutoGearRatios)
					return;

				if (gearIndex < 0 || gearIndex >= gearsCount)
				{
					ToolkitDebug.Error($"We couldn't set the requested vehicle speed target, it seems like the gear index is out of range!", vehicle);

					return;
				}

				if (direction >= 0)
				{
					float minimumSpeedTarget = gearIndex == 0 ? 1f : speedTargets[gearIndex - 1];
					float maximumSpeedTarget = gearIndex < gearsCount - 1 ? speedTargets[gearIndex + 1] : vehicle.Behaviour.TopSpeed;

					speedTargets[gearIndex] = math.clamp(speed, minimumSpeedTarget, maximumSpeedTarget);
				}
				else
					reverseSpeedTarget = math.clamp(speed, 1f, vehicle.TopSpeed);

				RefreshGears();
			}
			public void SetGearRatio(int direction, int gearIndex, float ratio, bool secondGearbox = false)
			{
				if (AutoGearRatios)
					return;

				if (gearIndex < 0 || gearIndex >= gearsCount)
				{
					ToolkitDebug.Error($"We couldn't set the requested vehicle gear ratio, it seems like the gear index is out of range!", vehicle);

					return;
				}

				secondGearbox &= UseDoubleGearbox;

				if (direction >= 0)
				{
					float minimumGearRatio = gearIndex < gearsCount - 1 ? (secondGearbox ? gearRatios2[gearIndex + 1] : gearRatios[gearIndex + 1]) : .1f;
					float maximumGearRatio = gearIndex == 0 ? 20f : secondGearbox ? gearRatios2[gearIndex - 1] : gearRatios[gearIndex - 1];

					ratio = math.clamp(ratio, minimumGearRatio, maximumGearRatio);

					if (secondGearbox)
						gearRatios2[gearIndex] = ratio;
					else
						gearRatios[gearIndex] = ratio;
				}
				else
				{
					ratio = math.clamp(ratio, secondGearbox ? gearRatios2[gearsCount - 1] : gearRatios[gearsCount - 1], 20f);

					if (secondGearbox)
						reverseGearRatio2 = ratio;
					else
						reverseGearRatio = ratio;
				}

				RefreshGears();
			}
			public void RefreshGears()
			{
				if (!vehicle || vehicle.Awaken)
					return;

				bool overrideSpeedTargets = false;

				if (speedTargets == null || speedTargets.Length != gearsCount)
				{
					speedTargets = new float[gearsCount];
					overrideSpeedTargets = true;
				}

				bool overrideGearRatios = false;

				if (gearRatios == null || gearRatios.Length != gearsCount || overrideSpeedTargets)
				{
					gearRatios = new float[gearsCount];
					overrideGearRatios = true;
				}

				bool useDoubleGearbox = UseDoubleGearbox;

				if (useDoubleGearbox && (gearRatios2 == null || gearRatios2.Length != gearsCount || overrideSpeedTargets))
				{
					gearRatios2 = new float[gearsCount];
					overrideGearRatios = true;
				}

				float frontRadius = 0f;
				float rearRadius = 0f;
				float frontMotorWheelsCoefficient = Utility.Clamp01(!useDoubleGearbox && vehicle.drivetrain == Train.AWD ? CenterDifferential.BiasAB : 1f);
				float rearMotorWheelsCoefficient = Utility.Clamp01(!useDoubleGearbox && vehicle.drivetrain == Train.AWD ? 1f - CenterDifferential.BiasAB : 1f);

				foreach (var motorWheel in vehicle.MotorWheels)
				{
					if (!motorWheel.Instance)
						continue;

					if (motorWheel.IsFrontWheel)
						frontRadius += motorWheel.Instance.Radius;
					else
						rearRadius += motorWheel.Instance.Radius;
				}

				if (vehicle.frontMotorWheelsCount > 0)
					frontRadius *= frontMotorWheelsCoefficient / vehicle.frontMotorWheelsCount;

				if (vehicle.rearMotorWheelsCount > 0)
					rearRadius *= rearMotorWheelsCoefficient / vehicle.rearMotorWheelsCount;

				minSpeedTargets = new float[gearsCount];

				float radius = frontRadius + rearRadius;

				if (useDoubleGearbox)
					radius *= .5f;

				bool autoGearRatios = AutoGearRatios;

				for (int i = 0; i < gearsCount; i++)
				{
					if (autoGearRatios || overrideGearRatios)
					{
						if (i >= gearsCount - 1 || overrideSpeedTargets)
							speedTargets[i] = Utility.Round(vehicle.Behaviour.TopSpeed * (i + 1) / gearsCount, 1);

						gearRatios[i] = Utility.Round(GetRatioFromSpeed(speedTargets[i] * finalGearRatio, useDoubleGearbox ? frontRadius * frontMotorWheelsCoefficient : radius, vehicle.Engine.RedlineRPM), 3);

						if (useDoubleGearbox)
							gearRatios2[i] = Utility.Round(GetRatioFromSpeed(speedTargets[i] * finalGearRatio, useDoubleGearbox ? rearRadius * rearMotorWheelsCoefficient : radius, vehicle.Engine.RedlineRPM), 3);
					}
					else
						speedTargets[i] = Utility.Round(GetSpeedFromRatio(useDoubleGearbox ? math.max(gearRatios[i], gearRatios2[i]) : gearRatios[i], radius, vehicle.Engine.RedlineRPM), 1) / finalGearRatio;

					minSpeedTargets[i] = vehicle.IsElectric && i == 0 ? default : Utility.Round(GetSpeedFromRatio(useDoubleGearbox ? math.min(gearRatios[i], gearRatios2[i]) : gearRatios[i], radius, vehicle.Engine.MinimumRPM), 1) / finalGearRatio;

					if (!GetGearShiftOverrideSpeed(i, out _))
						SetGearShiftOverrideSpeed(i, false, speedTargets[i]);
				}

				if (overrideGearRatios)
				{
					reverseGearRatio = gearRatios.Average() * 2f;

					if (useDoubleGearbox)
						reverseGearRatio2 = gearRatios2.Average() * 2f;
				}
				else if (autoGearRatios)
				{
					reverseGearRatio = Utility.Round(GetRatioFromSpeed(reverseSpeedTarget * finalGearRatio, useDoubleGearbox ? frontRadius * frontMotorWheelsCoefficient : radius, vehicle.Engine.RedlineRPM), 3);

					if (useDoubleGearbox)
						reverseGearRatio2 = Utility.Round(GetRatioFromSpeed(reverseSpeedTarget * finalGearRatio, useDoubleGearbox ? rearRadius * rearMotorWheelsCoefficient : radius, vehicle.Engine.RedlineRPM), 3);
				}

				if (!autoGearRatios || overrideGearRatios)
					reverseSpeedTarget = math.clamp(Utility.Round(GetSpeedFromRatio(useDoubleGearbox ? math.max(reverseGearRatio, reverseGearRatio2) : reverseGearRatio, radius, vehicle.Engine.RedlineRPM), 1) / finalGearRatio, 1f, vehicle.TopSpeed);

				reverseMinSpeedTarget = vehicle.IsElectric ? default : Utility.Round(GetSpeedFromRatio(useDoubleGearbox ? math.min(reverseGearRatio, reverseGearRatio2) : reverseGearRatio, radius, vehicle.Engine.MinimumRPM), 1) / finalGearRatio;

				vehicle.topSpeed = autoGearRatios ? vehicle.Behaviour.TopSpeed : speedTargets[gearsCount - 1];
			}
			public void ResetGears()
			{
				if (!vehicle)
					return;

				reverseGearRatio = default;
				reverseSpeedTarget = default;
				gearRatios = null;
				gearRatios2 = null;
				speedTargets = null;
				autoGearRatios = true;

				RefreshGears();
			}
			public void SetGearShiftOverrideSpeed(int gearIndex, bool state)
			{
				if (overrideGearShiftUpSpeeds == null)
					overrideGearShiftUpSpeeds = new bool[gearsCount];
				else if (overrideGearShiftUpSpeeds.Length != gearsCount)
					Array.Resize(ref overrideGearShiftUpSpeeds, gearsCount);

				if (gearIndex < 0 || gearIndex >= gearsCount)
				{
					ToolkitDebug.Error($"We couldn't set the requested vehicle gear shift speed, it seems like the gear index is out of range!", vehicle);

					return;
				}

				overrideGearShiftUpSpeeds[gearIndex] = state;
				vehicle.topSpeed = default;
			}
			public void SetGearShiftOverrideSpeed(int gearIndex, bool state, float speed)
			{
				if (speedTargets == null || speedTargets.Length != gearsCount)
					RefreshGears();

				if (gearShiftUpOverrideSpeeds == null)
					gearShiftUpOverrideSpeeds = speedTargets.Clone() as float[];
				else if (gearShiftUpOverrideSpeeds.Length != gearsCount)
					Array.Resize(ref gearShiftUpOverrideSpeeds, gearsCount);

				if (gearIndex < 0 || gearIndex >= gearsCount)
				{
					ToolkitDebug.Error($"We couldn't set the requested vehicle gear shift speed, it seems like the gear index is out of range!", vehicle);

					return;
				}

				gearShiftUpOverrideSpeeds[gearIndex] = math.clamp(speed, gearIndex > 0 ? speedTargets[gearIndex - 1] : minSpeedTargets[gearIndex], gearIndex + 1 < gearsCount ? speedTargets[gearIndex] : math.INFINITY);
				overrideGearShiftUpSpeeds[gearIndex] = state;
				vehicle.topSpeed = default;
			}
			public bool GetGearShiftOverrideSpeed(int gearIndex, out float speed)
			{
				speed = default;

				if (!Application.isPlaying || !vehicle.Awaken)
				{
					if (speedTargets == null || speedTargets.Length != gearsCount)
						RefreshGears();

					if (overrideGearShiftUpSpeeds == null)
						overrideGearShiftUpSpeeds = new bool[gearsCount];
					else if (overrideGearShiftUpSpeeds.Length != gearsCount)
						Array.Resize(ref overrideGearShiftUpSpeeds, gearsCount);

					if (gearShiftUpOverrideSpeeds == null)
						gearShiftUpOverrideSpeeds = speedTargets.Clone() as float[];
					else if (gearShiftUpOverrideSpeeds.Length != gearsCount)
						Array.Resize(ref gearShiftUpOverrideSpeeds, gearsCount);
				}

				if (gearIndex < 0 || gearIndex >= gearsCount)
				{
					ToolkitDebug.Error($"We couldn't get the requested vehicle gear shift speed, it seems like the gear index is out of range!", vehicle);

					return false;
				}

				if (gearShiftUpOverrideSpeeds[gearIndex] == default)
					gearShiftUpOverrideSpeeds[gearIndex] = speedTargets[gearIndex];

				speed = gearShiftUpOverrideSpeeds[gearIndex];

				return overrideGearShiftUpSpeeds[gearIndex];
			}

			private void RefreshEfficiency()
			{
				engineTorque = vehicle.Engine.Torque;
				powerTorqueOutputScale = vehicle.Behaviour.TorqueOutputScale;
				efficiency = Mathf.Approximately(maximumTorque, 0f) ? default : Utility.Clamp01(1f - Utility.Average(Utility.InverseLerp(maximumTorque * .5f, maximumTorque, engineTorque * powerTorqueOutputScale), Utility.InverseLerp(maximumTorque, maximumTorque * 1.5f, engineTorque * powerTorqueOutputScale)));
			}

			#endregion

			#endregion

			#region Constructors

			public TransmissionModule(Vehicle vehicle)
			{
				isValid = vehicle;
				this.vehicle = vehicle;
			}

			#endregion

			#region Operators

			public static implicit operator bool(TransmissionModule module) => module != null;

			#endregion
		}
		[Serializable]
		public class SteeringModule
		{
			#region Enumerators

			public enum SteerMethod { Simple, Responsive, Ackermann };

			#endregion

			#region Variables

			public SteerMethod Method
			{
				get
				{
					return HasInternalErrors || overrideMethod ? method : Settings.steerMethod;
				}
				set
				{
					if (!overrideMethod)
						return;

					method = value;
				}
			}
			public bool overrideMethod;
			public float MaximumSteerAngle
			{
				get
				{
					return maximumSteerAngle;
				}
				set
				{
					if (HasInternalErrors)
						return;

					maximumSteerAngle = math.clamp(value, Method != SteerMethod.Simple ? MinimumSteerAngle : Settings.minimumSteerAngle, Settings.maximumSteerAngle);
				}
			}
			public float MinimumSteerAngle
			{
				get
				{
					return minimumSteerAngle;
				}
				set
				{
					if (HasInternalErrors)
						return;

					minimumSteerAngle = math.clamp(value, Settings.minimumSteerAngle, MaximumSteerAngle);
				}
			}
			public float LowSteerAngleSpeed
			{
				get
				{
					return lowSteerAngleSpeed;
				}
				set
				{
					lowSteerAngleSpeed = math.max(value, 1f);
				}
			}
			public bool clampSteerAngle = true;
			public bool invertRearSteer = true;
			public bool UseDynamicSteering
			{
				get
				{
					return !HasInternalErrors && Settings.useDynamicSteering && useDynamicSteering;
				}
				set
				{
					useDynamicSteering = value;
				}
			}
			public float DynamicSteeringIntensity
			{
				get
				{
					return dynamicSteeringIntensity;
				}
				set
				{
					dynamicSteeringIntensity = Utility.Clamp01(value);
				}
			}

			internal bool IsValid
			{
				get
				{
					return vehicle;
				}
			}

#pragma warning disable IDE0044 // Add readonly modifier
			[SerializeField]
			private Vehicle vehicle;
#pragma warning restore IDE0044 // Add readonly modifier
			[SerializeField]
			private SteerMethod method = SteerMethod.Ackermann;
			[SerializeField]
			private float maximumSteerAngle = 35f;
			[SerializeField]
			private float minimumSteerAngle = 10f;
			[SerializeField]
			private float lowSteerAngleSpeed = 240f;
			[SerializeField]
			private bool useDynamicSteering;
			[SerializeField]
			private float dynamicSteeringIntensity = .1f;

			#endregion

			#region Constructors

			public SteeringModule(Vehicle vehicle)
			{
				this.vehicle = vehicle;
			}

			#endregion

			#region Operators

			public static implicit operator bool(SteeringModule module) => module != null;

			#endregion
		}
		[Serializable]
		public class StabilityModule
		{
			#region Variables

			public bool useAntiSwayBars = true;
			public float AntiSwayFront
			{
				get
				{
					if (!useAntiSwayBars)
						return default;

					return antiSwayFront;
				}
				set
				{
					if (!useAntiSwayBars)
						return;

					antiSwayFront = math.max(value, 0f);
				}
			}
			public float AntiSwayRear
			{
				get
				{
					if (!useAntiSwayBars)
						return default;

					return antiSwayRear;
				}
				set
				{
					if (!useAntiSwayBars)
						return;

					antiSwayRear = math.max(value, 0f);
				}
			}
			public bool useABS = true;
			public float ABSThreshold
			{
				get
				{
					if (!useABS)
						return 1f;

					return m_ABSThreshold;
				}
				set
				{
					if (!useABS)
						return;

					m_ABSThreshold = Utility.Clamp01(value);
				}
			}
			public bool useHandbrakeABS = true;
			public bool useESP = true;
			public float ESPStrength
			{
				get
				{
					if (!useESP)
						return default;

					return m_ESPStrength;
				}
				set
				{
					if (!useESP)
						return;

					m_ESPStrength = Utility.Clamp01(value);
				}
			}
			public float ESPSpeedThreshold
			{
				get
				{
					if (!useESP)
						return Mathf.Infinity;

					return ESPAllowDonuts ? math.max(m_ESPSpeedThreshold, 60f) : m_ESPSpeedThreshold;
				}
				set
				{
					if (!useESP)
						return;

					m_ESPSpeedThreshold = math.max(value, ESPAllowDonuts ? 60f : 1f);
				}
			}
			public bool ESPAllowDonuts = true;
			public bool useTCS = true;
			public float TCSThreshold
			{
				get
				{
					if (!useTCS)
						return 1f;

					return m_TCSThreshold;
				}
				set
				{
					if (!useTCS)
						return;

					m_TCSThreshold = Utility.Clamp01(value);
				}
			}
			public bool TCSAllowBurnouts = true;
			[FormerlySerializedAs("useSteeringHelper")]
			public bool useArcadeSteerHelpers = true;
			[Obsolete("Use `useArcadeSteerHelpers` instead.", true)]
			public bool useSteeringHelper = true;
			public float ArcadeLinearSteerHelperIntensity
			{
				get
				{
					return arcadeLinearSteerHelperIntensity;
				}
				set
				{
					arcadeLinearSteerHelperIntensity = math.clamp(value, 0f, 2f);
				}
			}
			[Obsolete("Use `ArcadeLinearSteerHelperIntensity` instead.")]
			public float SteerHelperLinearVelocityStrength
			{
				get
				{
					return ArcadeLinearSteerHelperIntensity;
				}
				set
				{
					ArcadeLinearSteerHelperIntensity = value;
				}
			}
			public float ArcadeAngularSteerHelperIntensity
			{
				get
				{
					return arcadeAngularSteerHelperIntensity;
				}
				set
				{
					arcadeAngularSteerHelperIntensity = math.clamp(value, 0f, 2f);
				}
			}
			[Obsolete("Use `ArcadeAngularSteerHelperIntensity` instead.")]
			public float SteerHelperAngularVelocityStrength
			{
				get
				{
					return ArcadeAngularSteerHelperIntensity;
				}
				set
				{
					ArcadeAngularSteerHelperIntensity = value;
				}
			}
			public bool UseCounterSteer
			{
				get
				{
					return !HasInternalErrors && Settings.useCounterSteer && useCounterSteer;
				}
				set
				{
					useCounterSteer = value;
				}
			}
			public bool UseLaunchControl
			{
				get
				{
					return !HasInternalErrors && Settings.useLaunchControl && useTCS;
				}
			}
			public float HandlingRate
			{
				get
				{
					return handlingRate;
				}
				set
				{
					handlingRate = Utility.Clamp01(value);
				}
			}
			/*public float OffRoadRate
			{
				get
				{
					return offRoadRate;
				}
				set
				{
					offRoadRate = Utility.Clamp01(value);
				}
			}*/
			public float WeightDistribution
			{
				get
				{
					return weightDistribution;
				}
				set
				{
					weightDistribution = Utility.Clamp01(value);
				}
			}
			public float WeightHeight
			{
				get
				{
					return weightHeight;
				}
				set
				{
					weightHeight = Utility.Clamp01(value);
				}
			}
			public bool useDownforce = true;
			public float FrontDownforce
			{
				get
				{
					if (!useDownforce)
						return default;

					return frontDownforce;
				}
				set
				{
					if (!useDownforce)
						return;

					frontDownforce = value;
				}
			}
			public float RearDownforce
			{
				get
				{
					if (!useDownforce)
						return default;

					return rearDownforce;
				}
				set
				{
					if (!useDownforce)
						return;

					rearDownforce = value;
				}
			}
			public float Drag
			{
				get
				{
					return drag;
				}
				set
				{
					drag = math.max(value, 0f);
				}
			}
			public float AngularDrag
			{
				get
				{
					return angularDrag;
				}
				set
				{
					angularDrag = math.max(value, 0f);
				}
			}
			/*public float DragCoefficient
			{
				get
				{
					return dragCoefficient;
				}
				set
				{
					dragCoefficient = Utility.Clamp01(value);
				}
			}
			public float VerticalDragCoefficient
			{
				get
				{
					return verticalDragCoefficient;
				}
				set
				{
					verticalDragCoefficient = Utility.Clamp01(value);
				}
			}
			public float FrontLateralDragCoefficient
			{
				get
				{
					return frontLateralDragCoefficient;
				}
				set
				{
					frontLateralDragCoefficient = Utility.Clamp01(value);
				}
			}
			public float RearLateralDragCoefficient
			{
				get
				{
					return rearLateralDragCoefficient;
				}
				set
				{
					rearLateralDragCoefficient = Utility.Clamp01(value);
				}
			}
			public float FrontalArea
			{
				get
				{
					return frontalArea;
				}
				set
				{
					FrontalArea = math.max(value);
				}
			}*/
			public float DragScale
			{
				get
				{
					return dragScale;
				}
				set
				{
					dragScale = math.clamp(value, 0f, 2f);
				}
			}

			internal bool IsValid
			{
				get
				{
					return vehicle;
				}
			}

			[SerializeField]
			private float antiSwayFront = 10000f;
			[SerializeField]
			private float antiSwayRear = 10000f;
			[SerializeField]
			private float m_ABSThreshold = .1f;
			[SerializeField]
			private float m_ESPStrength = 1f;
			[SerializeField]
			private float m_ESPSpeedThreshold = 10f;
			[SerializeField]
			private float m_TCSThreshold = .25f;
			[SerializeField, FormerlySerializedAs("steeringHelperLinearStrength")]
			private float arcadeLinearSteerHelperIntensity = 1f;
			[SerializeField, FormerlySerializedAs("steeringHelperAngularStrength")]
			private float arcadeAngularSteerHelperIntensity = 0f;
			[SerializeField]
			private bool useCounterSteer = true;
			[SerializeField]
			private float handlingRate = 1f;
			[SerializeField]
			private float weightDistribution = .5f;
			[SerializeField]
			private float weightHeight = -1f;
			[SerializeField]
			private float frontDownforce = 50f;
			[SerializeField]
			private float rearDownforce = 50f;
			[SerializeField]
			private float drag = .05f;
			[SerializeField]
			private float angularDrag = .25f;
			[SerializeField, FormerlySerializedAs("dragCoeficient")]
			private float dragCoefficient = .3f;
			[SerializeField, FormerlySerializedAs("verticalDragCoeficient")]
			private float verticalDragCoefficient = .5f;
			[SerializeField, FormerlySerializedAs("frontLateralDragCoeficient")]
			private float frontLateralDragCoefficient = .25f;
			[SerializeField, FormerlySerializedAs("rearLateralDragCoeficient")]
			private float rearLateralDragCoefficient = .25f;
#pragma warning disable IDE0044 // Add readonly modifier
			[SerializeField]
			private float frontalArea = 1f;
#pragma warning restore IDE0044 // Add readonly modifier
			[SerializeField]
			private float dragScale = 1f;
#pragma warning disable IDE0044 // Add readonly modifier
			[SerializeField]
			private Vehicle vehicle;
#pragma warning restore IDE0044 // Add readonly modifier

			#endregion

			#region Methods

			private static void TryRefreshWeightHeight(StabilityModule stability)
			{
				if (!stability.vehicle || stability.weightHeight >= 0f)
					return;

				stability.vehicle.RefreshWeightHeight(stability);
			}

			#endregion

			#region Constructors

			public StabilityModule(Vehicle vehicle)
			{
				this.vehicle = vehicle;

				TryRefreshWeightHeight(this);
			}
			public StabilityModule(StabilityModule module)
			{
				useAntiSwayBars = module.useAntiSwayBars;
				antiSwayFront = module.antiSwayFront;
				antiSwayRear = module.antiSwayRear;
				useABS = module.useABS;
				m_ABSThreshold = module.m_ABSThreshold;
				useESP = module.useESP;
				m_ESPStrength = module.m_ESPStrength;
				useTCS = module.useTCS;
				m_TCSThreshold = module.m_TCSThreshold;
				arcadeLinearSteerHelperIntensity = module.arcadeLinearSteerHelperIntensity;
				arcadeAngularSteerHelperIntensity = module.arcadeAngularSteerHelperIntensity;
				useCounterSteer = module.useCounterSteer;
				handlingRate = module.handlingRate;
				//offRoadRate = module.offRoadRate;
				weightDistribution = module.weightDistribution;
				weightHeight = module.weightHeight;
				useDownforce = module.useDownforce;
				frontDownforce = module.frontDownforce;
				rearDownforce = module.rearDownforce;
				drag = module.drag;
				angularDrag = module.angularDrag;
				dragCoefficient = module.dragCoefficient;
				verticalDragCoefficient = module.verticalDragCoefficient;
				frontLateralDragCoefficient = module.frontLateralDragCoefficient;
				rearLateralDragCoefficient = module.rearLateralDragCoefficient;
				frontalArea = module.frontalArea;
				dragScale = module.dragScale;
				vehicle = module.vehicle;

				TryRefreshWeightHeight(this);
				TryRefreshWeightHeight(module);
			}

			internal StabilityModule(Vehicle vehicle, StabilityModule module) : this(module)
			{
				this.vehicle = vehicle;

				TryRefreshWeightHeight(this);
				TryRefreshWeightHeight(module);
			}

			private StabilityModule() { }

			#endregion

			#region Operators

			public static implicit operator bool(StabilityModule stability) => stability != null;

			#endregion
		}
		/*[Serializable]
		public class DamageModule
		{
			#region Variables

			public MeshFilter[] MeshFilters
			{
				get
				{
					if (!vehicleInstance.Chassis)
						return null;

					if (parts == null || parts.Length < 1 || meshFilters == null || meshFilters.Length < 1)
					{
						parts = vehicleInstance.Chassis.parts;

						List<MeshFilter> meshFiltersList = new();

						for (int i = 0; i < parts.Length; i++)
						{
							if (!parts[i].isDamageable)
								continue;

							meshFiltersList.AddRange(parts[i].GetComponentsInChildren<MeshFilter>());
						}

						meshFilters = meshFiltersList.ToArray();
					}

					return meshFilters;
				}
			}
			public bool repaired = true;
			public bool overrideVertexRandomization = false;
			public float vertexRandomization = 1f;
			public bool overrideRadius = false;
			public float radius = .5f;

			internal bool IsValid => vehicleInstance;

			[SerializeField]
			private Vehicle vehicleInstance;
			private VehicleChassisPart[] parts;
			private MeshFilter[] meshFilters;
			private Vector3[][] orgMeshesVertices;

			#endregion

			#region Methods

			public void LoadData()
			{
				orgMeshesVertices = new Vector3[MeshFilters.Length][];

				for (int i = 0; i < MeshFilters.Length; i++)
					orgMeshesVertices[i] = MeshFilters[i].mesh.vertices;
			}
			public Vector3[] GetOriginalMeshVertices(int index)
			{
				if (orgMeshesVertices == null || orgMeshesVertices.Length < 1)
					LoadData();

				return orgMeshesVertices[index];
			}

			#endregion

			#region Constructors

			public DamageModule(Vehicle vehicle)
			{
				vehicleInstance = vehicle;
			}

			#endregion

			#region Operators

			public static implicit operator bool(DamageModule damage) => damage != null;

			#endregion
		}*/
		[Serializable]
		public class InteriorModule
		{
			#region Modules

			[Serializable]
			public class ComponentModule
			{
				#region Enumerators

				#endregion

				#region Variables

				public Transform transform;
				public Utility.Axis3 rotationAxis = Utility.Axis3.Z;
				public float StartAngle
				{
					get
					{
						return startAngle;
					}
					set
					{
						startAngle = math.clamp(value, -360f, 360f);
					}
				}
				public float TargetAngle
				{
					get
					{
						return targetAngle;
					}
					set
					{
						targetAngle = math.clamp(value, -360f, 360f);
					}
				}
				public bool overrideTarget;
				public float Target
				{
					get
					{
						return target;
					}
					set
					{
						target = math.max(value, 1f);
					}
				}

#if !MVC_COMMUNITY
				private Vector3 orgEulerAngles;
#endif
				[SerializeField]
				private float startAngle;
				[SerializeField]
				private float targetAngle = 180f;
				[SerializeField]
				private float target = 1f;
#if !MVC_COMMUNITY
				private bool awaken;
#endif

				#endregion

				#region Methods

				#region Awake

				public void Restart()
				{
#if !MVC_COMMUNITY
					awaken = false;

					if (!transform)
						return;

					Initialize();

					orgEulerAngles = transform.localEulerAngles;
					awaken = true;
#endif
				}

#if !MVC_COMMUNITY
				private void Initialize()
				{
					if (orgEulerAngles != default)
						transform.localEulerAngles = orgEulerAngles;

					orgEulerAngles = default;
				}
#endif

				#endregion

				#region Update

				public void SetRotation(float angle)
				{
#if !MVC_COMMUNITY
					if (!awaken)
						return;

					Vector3 eulerAngles = orgEulerAngles;

					switch (rotationAxis)
					{
						case Utility.Axis3.X:
							eulerAngles.x += angle;

							break;

						case Utility.Axis3.Y:
							eulerAngles.y += angle;

							break;

						case Utility.Axis3.Z:
							eulerAngles.z += angle;

							break;
					}

					transform.localEulerAngles = eulerAngles;
#endif
				}

				#endregion

				#endregion

				#region Operators

				public static implicit operator bool(ComponentModule module) => module != null;

				#endregion
			}
			[Serializable]
			public class LightModule
			{
				#region Variables

				public MeshRenderer renderer;
				public int MaterialIndex
				{
					get
					{
						if (!renderer || materialIndex >= renderer.sharedMaterials.Length)
							return -1;

						return materialIndex;
					}
					set
					{
						if (!renderer)
							return;

						value = math.clamp(value, 0, renderer.sharedMaterials.Length - 1);

						if (!renderer.sharedMaterials[value])
							return;

						materialIndex = value;
					}
				}
				public Color emissionColor;

				private string materialEmissionColorPropertyName;
				private bool awaken;
				private bool state;
				[SerializeField]
				private int materialIndex;

				#endregion

				#region Methods

				#region Utilities

				public string GetMaterialEmissionColorPropertyName()
				{
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
					materialEmissionColorPropertyName = default;
				}

				#endregion

				#region Awake

				public void Restart()
				{
					awaken = false;

					if (!renderer || MaterialIndex < 0)
						return;

					RefreshMaterialEmissionColorPropertyName();

					awaken = true;
				}

				#endregion

				#region Update

				public void Illuminate(bool state)
				{
					if (!awaken || this.state == state)
						return;

					renderer.materials[MaterialIndex].SetColor(GetMaterialEmissionColorPropertyName(), state ? emissionColor : Color.black);

					this.state = state;
				}

				#endregion

				#endregion

				#region Operators

				public static implicit operator bool(LightModule module) => module != null;

				#endregion
			}

			#endregion

			#region Variables

			public ComponentModule SteeringWheel
			{
				get
				{
					if (!steeringWheel)
						steeringWheel = new()
						{
							Target = vehicle.Steering.MaximumSteerAngle
						};

					return steeringWheel;
				}
			}
			public ComponentModule RPMNeedle
			{
				get
				{
					if (!rpmNeedle)
						rpmNeedle = new()
						{
							Target = vehicle.Engine.MaximumRPM
						};

					return rpmNeedle;
				}
			}
			public ComponentModule SpeedNeedle
			{
				get
				{
					if (!speedNeedle)
						speedNeedle = new()
						{
							Target = vehicle.TopSpeed
						};

					return speedNeedle;
				}
			}
			public ComponentModule FuelNeedle
			{
				get
				{
					if (!fuelNeedle)
						fuelNeedle = new()
						{
							Target = vehicle.Behaviour.FuelCapacity
						};

					return fuelNeedle;
				}
			}
			public LightModule IndicatorLeft
			{
				get
				{
					if (!indicatorLeft)
						indicatorLeft = new();

					return indicatorLeft;
				}
			}
			public LightModule IndicatorRight
			{
				get
				{
					if (!indicatorRight)
						indicatorRight = new();

					return indicatorRight;
				}
			}
			public LightModule Handbrake
			{
				get
				{
					if (!handbrake)
						handbrake = new();

					return handbrake;
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
			private ComponentModule steeringWheel;
			[SerializeField]
			private ComponentModule rpmNeedle;
			[SerializeField]
			private ComponentModule speedNeedle;
			[SerializeField]
			private ComponentModule fuelNeedle;
			[SerializeField]
			private LightModule indicatorLeft;
			[SerializeField]
			private LightModule indicatorRight;
			[SerializeField]
			private LightModule handbrake;
#pragma warning disable IDE0044 // Add readonly modifier
			[SerializeField]
			private Vehicle vehicle;
#pragma warning restore IDE0044 // Add readonly modifier

			#endregion

			#region Methods

			public void Restart()
			{
#if !MVC_COMMUNITY
				if (!Settings.useInterior)
					return;

				SteeringWheel.Restart();
				RPMNeedle.Restart();
				SpeedNeedle.Restart();

				if (Settings.useFuelSystem)
					FuelNeedle.Restart();

				IndicatorLeft.Restart();
				IndicatorRight.Restart();
				Handbrake.Restart();
#endif
			}
			public void Update()
			{
#if !MVC_COMMUNITY
				if (!Settings.useInterior)
					return;

				if (!IsValid)
					return;

				steeringWheel.SetRotation(Utility.LerpUnclamped(steeringWheel.StartAngle, steeringWheel.TargetAngle, vehicle.stats.rawSteerAngle / (steeringWheel.overrideTarget ? steeringWheel.Target : vehicle.steering.MaximumSteerAngle)));
				rpmNeedle.SetRotation(Utility.LerpUnclamped(rpmNeedle.StartAngle, rpmNeedle.TargetAngle, vehicle.stats.engineRPM / (rpmNeedle.overrideTarget ? rpmNeedle.Target : vehicle.engine.MaximumRPM)));
				speedNeedle.SetRotation(Utility.LerpUnclamped(speedNeedle.StartAngle, speedNeedle.TargetAngle, math.abs(vehicle.stats.averageMotorWheelsSpeed) / (speedNeedle.overrideTarget ? speedNeedle.Target : vehicle.TopSpeed)));

				if (Settings.useFuelSystem)
					fuelNeedle.SetRotation(Utility.LerpUnclamped(fuelNeedle.StartAngle, fuelNeedle.TargetAngle, math.abs(vehicle.stats.fuelTank) / (fuelNeedle.overrideTarget ? fuelNeedle.Target : vehicle.behaviour.FuelCapacity)));

				indicatorLeft.Illuminate(vehicle.stats.isSignalLeftLightsOn);
				indicatorRight.Illuminate(vehicle.stats.isSignalRightLightsOn);
				handbrake.Illuminate(vehicle.inputs.Handbrake >= .05f);
#endif
			}

			#endregion

			#region Constructors

			public InteriorModule(Vehicle vehicle)
			{
				this.vehicle = vehicle;
			}

			#endregion

			#region Operators

			public static implicit operator bool(InteriorModule module) => module != null;

			#endregion
		}
		[Serializable]
		public class AudioMixersModule
		{
			#region Variables

			public AudioMixerGroup Engine
			{
				get
				{
					if (engine || !vehicle)
						return engine;

					return vehicle.Engine?.Audio.mixerGroups.engine;
				}
			}
			public AudioMixerGroup Exhaust
			{
				get
				{
					if (exhaust || !vehicle)
						return exhaust;

					return vehicle.Engine?.Audio.mixerGroups.exhaust;
				}
			}
			public AudioMixerGroup ExhaustEffects
			{
				get
				{
					if (exhaustEffects || HasInternalErrors)
						return exhaustEffects;

					return Settings.audioMixers.exhaustEffects;
				}
			}
			public AudioMixerGroup Transmission
			{
				get
				{
					if (transmission || HasInternalErrors)
						return transmission;

					var settings = Settings;
					int clipsGroupIndex = vehicle.Transmission.ClipsGroupIndex;

					if (clipsGroupIndex > -1)
					{
						var mixer = settings.transmissionWhineGroups[clipsGroupIndex].mixer;

						if (mixer)
							return mixer;
					}

					return settings.audioMixers.transmission;
				}
			}

			public AudioMixerGroup BrakeEffects
			{
				get
				{
					if (brakeEffects || HasInternalErrors)
						return brakeEffects;

					return Settings.audioMixers.brakeEffects;
				}
			}
			public AudioMixerGroup ChargersEffects
			{
				get
				{
					if (chargersEffects || HasInternalErrors)
						return chargersEffects;

					return Settings.audioMixers.chargersEffects;
				}
			}
			public AudioMixerGroup Turbocharger
			{
				get
				{
					if (turbocharger || HasInternalErrors)
						return turbocharger;

					return Settings.audioMixers.turbocharger;
				}
			}
			public AudioMixerGroup Supercharger
			{
				get
				{
					if (supercharger || HasInternalErrors)
						return supercharger;

					return Settings.audioMixers.supercharger;
				}
			}
			public AudioMixerGroup engine;
			public AudioMixerGroup exhaust;
			public AudioMixerGroup exhaustEffects;
			public AudioMixerGroup transmission;
			public AudioMixerGroup brakeEffects;
			public AudioMixerGroup chargersEffects;
			public AudioMixerGroup turbocharger;
			public AudioMixerGroup supercharger;
			public float InteriorLowPassFreq
			{
				get
				{
					return interiorLowPassFreq;
				}
				set
				{
					interiorLowPassFreq = math.clamp(value, 10f, 22000f);
				}
			}
			public float EngineVolume
			{
				get
				{
					return engineVolume;
				}
				set
				{
					engineVolume = Utility.Clamp01(value);
				}
			}
			public float ExhaustVolume
			{
				get
				{
					return exhaustVolume;
				}
				set
				{
					exhaustVolume = Utility.Clamp01(value);
				}
			}
			public float ExhaustEffectsVolume
			{
				get
				{
					return exhaustEffectsVolume;
				}
				set
				{
					exhaustEffectsVolume = Utility.Clamp01(value);
				}
			}
			public float TransmissionVolume
			{
				get
				{
					return transmissionVolume;
				}
				set
				{
					transmissionVolume = Utility.Clamp01(value);
				}
			}
			public float BrakeEffectsVolume
			{
				get
				{
					return brakeEffectsVolume;
				}
				set
				{
					brakeEffectsVolume = Utility.Clamp01(value);
				}
			}
			public float ChargersEffectsVolume
			{
				get
				{
					return chargersEffectsVolume;
				}
				set
				{
					chargersEffectsVolume = Utility.Clamp01(value);
				}
			}
			public float TurbochargerVolume
			{
				get
				{
					return turbochargerVolume;
				}
				set
				{
					turbochargerVolume = Utility.Clamp01(value);
				}
			}
			public float SuperchargerVolume
			{
				get
				{
					return superchargerVolume;
				}
				set
				{
					superchargerVolume = Utility.Clamp01(value);
				}
			}
			public bool IsValid
			{
				get
				{
					return vehicle;
				}
			}

#pragma warning disable IDE0044 // Add readonly modifier
			[SerializeField]
			private Vehicle vehicle;
#pragma warning restore IDE0044 // Add readonly modifier
			[SerializeField]
			private float interiorLowPassFreq = 5000f;
			[SerializeField]
			private float engineVolume = 1f;
			[SerializeField]
			private float exhaustVolume = 1f;
			[SerializeField]
			private float exhaustEffectsVolume = 1f;
			[SerializeField]
			private float transmissionVolume = 1f;
			[SerializeField]
			private float brakeEffectsVolume = 1f;
			[SerializeField]
			private float chargersEffectsVolume = 1f;
			[SerializeField]
			private float turbochargerVolume = 1f;
			[SerializeField]
			private float superchargerVolume = 1f;

			#endregion

			#region Constructors

			public AudioMixersModule(Vehicle vehicle)
			{
				this.vehicle = vehicle;
			}

			#endregion

			#region Operators

			public static implicit operator bool(AudioMixersModule module) => module != null;

			#endregion
		}
		[Serializable]
		public class DriverIKModule
		{
			#region Variables

			public VehicleDriver Driver
			{
				get
				{
					if (driver && !driver.transform.IsChildOf(vehicle.transform))
						driver = null;

					return driver;
				}
				set
				{
					if (value)
					{
						Transform driverContainer = vehicle.transform.Find("Driver");

						if (!driverContainer || driverContainer == value.transform)
						{
							driverContainer = new GameObject("Driver").transform;
							driverContainer.parent = vehicle.transform;

							driverContainer.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
						}

						value.transform.parent = driverContainer;

						value.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
					}
					else if (driver)
						driver.transform.parent = null;

					driver = value;
				}
			}
			public VehicleFollowerPivot FPSFollowerPivot
			{
				get
				{
					return fpsFollowerPivot;
				}
				set
				{
					if (!vehicle || !vehicle.Chassis || value && !value.transform.IsChildOf(vehicle.Chassis.transform))
						return;

					fpsFollowerPivot = value;
				}
			}
			public string steeringWheelAngleParameter = "Angle";
			[Obsolete("Use `leftHandSteeringWheelPivot` instead.")]
			public VehicleIKPivot LeftHandSteeringWheelPivot
			{
				get => leftHandSteeringWheelPivot;
				set => leftHandSteeringWheelPivot = value;
			}
			[Obsolete("Use `rightHandSteeringWheelPivot` instead.")]
			public VehicleIKPivot RightHandSteeringWheelPivot
			{
				get => rightHandSteeringWheelPivot;
				set => rightHandSteeringWheelPivot = value;
			}
			public VehicleIKPivot leftHandSteeringWheelPivot;
			public VehicleIKPivot rightHandSteeringWheelPivot;
			public bool HasAllSteeringWheelPivots
			{
				get
				{
					return leftHandSteeringWheelPivot && rightHandSteeringWheelPivot;
				}
			}
			[Obsolete("Use `leftFootPivot` instead.")]
			public VehicleIKPivot LeftFootPivot
			{
				get => leftFootPivot;
				set => leftFootPivot = value;
			}
			[Obsolete("Use `rightFootPivot` instead.")]
			public VehicleIKPivot RightFootPivot
			{
				get => rightFootPivot;
				set => rightFootPivot = value;
			}
			public VehicleIKPivot leftFootPivot;
			public VehicleIKPivot rightFootPivot;
			public bool HasAllFeetPivots
			{
				get
				{
					return leftFootPivot && rightFootPivot;
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
			private VehicleDriver driver;
			[SerializeField, FormerlySerializedAs("fpsCameraPivot")]
			private VehicleFollowerPivot fpsFollowerPivot;
#pragma warning disable IDE0044 // Add readonly modifier
			[SerializeField]
			private Vehicle vehicle;
#pragma warning restore IDE0044 // Add readonly modifier

			#endregion

			#region Constructors

			public DriverIKModule(Vehicle vehicle)
			{
				this.vehicle = vehicle;
			}

			#endregion

			#region Operators

			public static implicit operator bool(DriverIKModule module) => module != null;

			#endregion
		}
		public readonly struct ProblemsAccess
		{
			#region Variables

			public int MissingWheelTransformsCount => !HasInternalErrors ? vehicle.Wheels.Where(wheel => !wheel.Model).Count() : 0;
			public int MissingWheelRimsCount => !HasInternalErrors ? vehicle.Wheels.Where(wheel => !wheel.Rim).Count() : 0;
			public int MissingWheelRimEdgesCount => !HasInternalErrors && Settings.useDamage && Settings.useWheelHealth ? vehicle.Wheels.Where(wheel => wheel.Rim && !wheel.RimEdgeRenderer).Count() : 0;
			public int MissingWheelTiresCount => !HasInternalErrors ? vehicle.Wheels.Where(wheel => !wheel.Tire).Count() : 0;
			public int MissingWheelBrakeDiscsCount => !HasInternalErrors && (!vehicle.trailerInstance || vehicle.trailerInstance.useBrakes) ? vehicle.Wheels.Where(wheel => !wheel.BrakeDiscRenderer).Count() : 0;
			public int MissingWheelBrakeCallipersCount => !HasInternalErrors && (!vehicle.trailerInstance || vehicle.trailerInstance.useBrakes) ? vehicle.Wheels.Where(wheel => !wheel.BrakeCalliper).Count() : 0;
			public bool IsMissingChassis => !HasInternalErrors && !vehicle.Chassis;
			public int MissingWheelBehavioursCount => !HasInternalErrors ? vehicle.Wheels.Where(wheel => !wheel.Instance).Count() : 0;
			public bool HasDisabledWheels => !HasInternalErrors && Array.FindIndex(vehicle.Wheels, wheel => wheel.Instance && (!wheel.Instance.enabled || !wheel.Instance.gameObject.activeSelf)) > -1;
			public bool HasTrailerJointIssues => !HasInternalErrors && vehicle.trailerInstance && vehicle.trailerInstance.Joint.Position == default;
			public bool HasComponentsWarnings => !HasInternalErrors && (vehicle.Wheels.Length < 1 || !vehicle.trailerInstance && !vehicle.hasFrontWheels || !vehicle.hasRearWheels || HasDisabledWheels || Utility.Round(vehicle.transform.localScale, 1) != Vector3.one);
			public bool HasComponentsErrors => !HasInternalErrors && (MissingWheelTransformsCount > 0 || (MissingWheelBehavioursCount - MissingWheelTransformsCount) > 0 || IsMissingChassis);
			public bool HasComponentsIssues => !HasInternalErrors && (vehicle.Chassis && !vehicle.Chassis.GetComponentInChildren<Collider>() || MissingWheelRimsCount > 0 || MissingWheelRimEdgesCount > 0 || MissingWheelTiresCount > 0 || MissingWheelBrakeCallipersCount > 0 || MissingWheelBrakeDiscsCount > 0 || !vehicle.isAllBalanced || !vehicle.trailerInstance && vehicle.Steertrain == Train.None || !vehicle.trailerInstance && vehicle.Drivetrain == Train.None || !Application.isPlaying && Array.Find(vehicle.Wheels, wheel => wheel.Rim && Utility.Round(wheel.Rim.localScale, 1) != Vector3.one || wheel.Tire && Utility.Round(wheel.Tire.localScale, 1) != Vector3.one || wheel.BrakeDiscRenderer && Utility.Round(wheel.BrakeDiscRenderer.transform.localScale, 1) != Vector3.one || wheel.BrakeCalliper && Utility.Round(wheel.BrakeCalliper.localScale, 1) != Vector3.one) || !vehicle.trailerInstance && !vehicle.IsElectric && !vehicle.Chassis.ExhaustModel && vehicle.Exhausts.Length > 0);
			public bool HasBehaviourWarnings => !HasInternalErrors && !vehicle.trailerInstance && (vehicle.Engine && vehicle.Engine.IsElectric != vehicle.IsElectric || !vehicle.IsElectric && (vehicle.Behaviour.IsTurbocharged && (!vehicle.Turbocharger || vehicle.Turbocharger && !vehicle.Turbocharger.IsValid) || vehicle.behaviour.IsSupercharged && (!vehicle.Supercharger || vehicle.Supercharger && !vehicle.Supercharger.IsValid)));
			public bool HasBehaviourErrors => !HasInternalErrors && !vehicle.trailerInstance && (!vehicle.Engine || vehicle.Engine.MinimumRPM > vehicle.Behaviour.PeakTorqueRPM || vehicle.behaviour.PeakTorqueRPM > vehicle.Engine.RedlineRPM || vehicle.Engine.MinimumRPM > vehicle.behaviour.PeakPowerRPM || vehicle.behaviour.PeakPowerRPM > vehicle.Engine.RedlineRPM || !Mathf.Approximately(vehicle.behaviour.Power, vehicle.behaviour.PowerCurve.Evaluate(vehicle.behaviour.PeakPowerRPM)) || !Mathf.Approximately(vehicle.behaviour.Torque, vehicle.behaviour.TorqueCurve.Evaluate(vehicle.behaviour.PeakTorqueRPM)) || vehicle.behaviour.IsTurbocharged && vehicle.Turbocharger && (vehicle.behaviour.TurbochargerIndex >= Settings.Chargers.Length || vehicle.Turbocharger.Type != VehicleCharger.ChargerType.Turbocharger || !vehicle.Turbocharger.IsCompatible || vehicle.Turbocharger.CompatibleEngineIndexes.Length != 0 && !vehicle.Turbocharger.HasCompatibleEngine(vehicle.behaviour.EngineIndex)) || vehicle.behaviour.IsSupercharged && vehicle.Supercharger && (vehicle.behaviour.SuperchargerIndex >= Settings.Chargers.Length || vehicle.Supercharger.Type != VehicleCharger.ChargerType.Supercharger || !vehicle.Supercharger.IsCompatible || vehicle.Supercharger.CompatibleEngineIndexes.Length != 0 && !vehicle.Supercharger.HasCompatibleEngine(vehicle.behaviour.EngineIndex)) && !vehicle.IsElectric);
			public bool HasBehaviourIssues => !HasInternalErrors && !vehicle.trailerInstance && (vehicle.Behaviour.Torque * 10f / vehicle.behaviour.CurbWeight < 1f / 3f || vehicle.behaviour.Power / vehicle.behaviour.Torque > 3f || vehicle.behaviour.Torque / vehicle.behaviour.Power > 3f || (vehicle.behaviour.IsTurbocharged && vehicle.Turbocharger.HasIssues || vehicle.behaviour.IsSupercharged && vehicle.Supercharger.HasIssues) && !vehicle.IsElectric || Settings.useFuelSystem && (vehicle.behaviour.FuelCapacity == 0f || vehicle.behaviour.FuelConsumptionCity == 0f && vehicle.behaviour.FuelConsumptionCombined == 0f && vehicle.behaviour.FuelConsumptionHighway == 0f));
			public bool HasTransmissionIssues => !HasInternalErrors && !vehicle.trailerInstance && (vehicle.Transmission.Efficiency < .5f || vehicle.Drivetrain != Train.None && (
				vehicle.Drivetrain != Train.RWD && ((vehicle.frontDifferential = vehicle.transmission.FrontDifferential).GearRatio <= 0f || vehicle.frontDifferential.Type == VehicleDifferentialType.Locked && Mathf.Approximately(vehicle.frontDifferential.Stiffness, 0f) || vehicle.frontDifferential.Type != VehicleDifferentialType.Locked && !Mathf.Approximately(vehicle.frontDifferential.BiasAB, .5f) || vehicle.frontDifferential.Type == VehicleDifferentialType.LimitedSlip && (Mathf.Approximately(vehicle.frontDifferential.SlipTorque, 0f) || Mathf.Approximately(vehicle.frontDifferential.PowerRamp, 0f) && Mathf.Approximately(vehicle.frontDifferential.CoastRamp, 0f))) ||
				vehicle.Drivetrain == Train.AWD && ((vehicle.centerDifferential = vehicle.transmission.CenterDifferential).GearRatio <= 0f || vehicle.centerDifferential.Type == VehicleDifferentialType.Locked && Mathf.Approximately(vehicle.centerDifferential.Stiffness, 0f) || vehicle.centerDifferential.Type == VehicleDifferentialType.LimitedSlip && (Mathf.Approximately(vehicle.centerDifferential.SlipTorque, 0f) || Mathf.Approximately(vehicle.centerDifferential.PowerRamp, 0f) && Mathf.Approximately(vehicle.centerDifferential.CoastRamp, 0f))) ||
				vehicle.Drivetrain != Train.FWD && ((vehicle.rearDifferential = vehicle.transmission.RearDifferential).GearRatio <= 0f || vehicle.rearDifferential.Type == VehicleDifferentialType.Locked && Mathf.Approximately(vehicle.rearDifferential.Stiffness, 0f) || vehicle.rearDifferential.Type != VehicleDifferentialType.Locked && !Mathf.Approximately(vehicle.rearDifferential.BiasAB, .5f) || vehicle.rearDifferential.Type == VehicleDifferentialType.LimitedSlip && (Mathf.Approximately(vehicle.rearDifferential.SlipTorque, 0f) || Mathf.Approximately(vehicle.rearDifferential.PowerRamp, 0f) && Mathf.Approximately(vehicle.rearDifferential.CoastRamp, 0f)))
				));
			public bool HasBrakingIssues => !HasInternalErrors && vehicle.gameObject.activeInHierarchy && (!vehicle.trailerInstance || vehicle.trailerInstance.useBrakes) && !Array.Find(vehicle.Wheels, wheel => !wheel.Instance || !wheel.Instance.gameObject.activeInHierarchy || wheel.BrakeDiscRenderer && !wheel.BrakeDiscRenderer.gameObject.activeInHierarchy) && ((!vehicle.trailerInstance && vehicle.frontWheels.Length > 0 && vehicle.Behaviour.FrontBrakes.Diameter * 100f * Utility.UnitMultiplier(Utility.Units.Size, Utility.UnitType.Imperial) >= vehicle.frontWheels.Where(wheel => wheel.Instance).Min(wheel => wheel.Instance.Diameter)) || (vehicle.trailerInstance && vehicle.wheels.Length > 0 || vehicle.rearWheels.Length > 0) && (vehicle.trailerInstance ? vehicle.trailerInstance.Brakes : vehicle.behaviour.RearBrakes).Diameter * 100f * Utility.UnitMultiplier(Utility.Units.Size, Utility.UnitType.Imperial) >= (vehicle.trailerInstance ? vehicle.wheels : vehicle.rearWheels).Where(wheel => wheel.Instance).Min(wheel => wheel.Instance.Diameter));
			public bool HasSteeringErrors => !HasInternalErrors && !vehicle.trailerInstance && (vehicle.Steering.MaximumSteerAngle < vehicle.steering.MinimumSteerAngle);
			public bool HasSteeringIssues => !HasInternalErrors && !vehicle.trailerInstance && (vehicle.Steering.Method != SteeringModule.SteerMethod.Simple && (vehicle.steering.LowSteerAngleSpeed > vehicle.TopSpeed || vehicle.steering.MinimumSteerAngle <= 0f) || vehicle.steering.MaximumSteerAngle <= 0f || vehicle.steering.UseDynamicSteering && vehicle.steering.DynamicSteeringIntensity <= 0f);
			public bool HasSuspensionsIssues => !HasInternalErrors && vehicle.Stability.useAntiSwayBars && (!vehicle.trailerInstance && vehicle.FrontLeftWheels.Length > 0 && vehicle.frontRightWheels.Length > 0 && vehicle.FrontSuspension.Length * (1f + vehicle.frontSuspension.LengthStance) * 33333f > vehicle.stability.AntiSwayFront || vehicle.rearLeftWheels.Length > 0 && vehicle.rearRightWheels.Length > 0 && vehicle.RearSuspension.Length * (1f + vehicle.rearSuspension.LengthStance) * 33333f > vehicle.stability.AntiSwayRear);
			public bool HasTiresWarnings => !HasInternalErrors && vehicle.wheels.Any(wheel => wheel.Instance && (!wheel.Instance.TireCompound || !wheel.Instance.TireCompound.IsValid));
			public bool HasStabilityIssues => !HasInternalErrors && (vehicle.rigidbody.interpolation == RigidbodyInterpolation.None || vehicle.rigidbody.maxAngularVelocity <= 0f || !vehicle.trailerInstance && vehicle.Stability.useAntiSwayBars && (vehicle.FrontLeftWheels.Length > 0 && vehicle.frontRightWheels.Length > 0 && vehicle.stability.AntiSwayFront <= 0f || vehicle.rearLeftWheels.Length > 0 && vehicle.rearRightWheels.Length > 0 && vehicle.stability.AntiSwayRear <= 0f) || vehicle.stability.useESP && vehicle.stability.ESPStrength <= 0f || !vehicle.trailerInstance && vehicle.stability.useArcadeSteerHelpers && vehicle.stability.ArcadeLinearSteerHelperIntensity <= 0f && vehicle.stability.ArcadeAngularSteerHelperIntensity <= 0f || vehicle.stability.useDownforce && vehicle.stability.FrontDownforce == 0f && vehicle.stability.RearDownforce == 0f);
			public bool HasAIErrors => !HasInternalErrors && vehicle.IsAI && (vehicle.activeAIBehaviourIndex >= vehicle.AIBehaviours.Length || vehicle.AIBehaviours.Any(behaviour => behaviour && behaviour.Errors != null && behaviour.Errors.Length > 0));
			public bool HasAIWarnings => !HasInternalErrors && vehicle.IsAI && vehicle.AIBehaviours.Any(behaviour => behaviour && behaviour.Warnings != null && behaviour.Warnings.Length > 0);
			public bool HasAIIssues => !HasInternalErrors && vehicle.IsAI && vehicle.AIBehaviours.Any(behaviour => behaviour && behaviour.Issues != null && behaviour.Issues.Length > 0);
			public bool HasInteriorIssues => !HasInternalErrors && Settings.useInterior && !vehicle.trailerInstance && vehicle.Chassis && (vehicle.Interior.SteeringWheel.transform && !vehicle.interior.SteeringWheel.transform.IsChildOf(vehicle.Chassis.transform) || vehicle.interior.RPMNeedle.transform && !vehicle.interior.RPMNeedle.transform.IsChildOf(vehicle.Chassis.transform) || vehicle.interior.SpeedNeedle.transform && !vehicle.interior.SpeedNeedle.transform.IsChildOf(vehicle.Chassis.transform));
			public bool HasTrailerLinkIssues => !HasInternalErrors && vehicle.Chassis && vehicle.TrailerLink && (vehicle.TrailerLink.LinkRadius > vehicle.ChassisBounds.extents.x);
			public bool HasIssues => HasBehaviourIssues || HasBrakingIssues || HasComponentsIssues || vehicle.trailerInstance && HasInteriorIssues || HasStabilityIssues || HasSteeringIssues || HasSuspensionsIssues || HasTrailerJointIssues || HasTrailerLinkIssues || HasTransmissionIssues || HasAIIssues;
			public bool IsSheetValid => vehicle;

			private readonly Vehicle vehicle;

			#endregion

			#region Methods

			#region Virtual Methods

			public override bool Equals(object obj)
			{
				return obj is ProblemsAccess sheet &&
					   EqualityComparer<Vehicle>.Default.Equals(vehicle, sheet.vehicle);
			}
			public override int GetHashCode()
			{
				return HashCode.Combine(vehicle);
			}

			#endregion

			#region Utilities

			public bool DisableVehicleOnInternalErrors()
			{
				if (!IsSheetValid || !HasInternalErrors)
					return false;

				DisableAllBehaviours();
				ToolkitDebug.Error($"The {vehicle.name}'s MVC behaviour has been disabled! We have had some internal errors that need to be fixed in order for the MVC behaviours to work properly. You can check the Vehicle inspector editor log for more information.");

				return true;
			}
			public bool DisableVehicleOnWarnings()
			{
				if (!IsSheetValid || !HasComponentsWarnings && !HasBehaviourWarnings && !HasTiresWarnings && !HasAIWarnings && !HasDisabledWheels)
					return false;

				DisableAllBehaviours();
				ToolkitDebug.Warning($"The {vehicle.name}'s Vehicle component has been disabled! It may have some errors that need to be fixed. You can check the Vehicle inspector editor log for more information.");

				return true;
			}
			public bool DisableVehicleOnErrors()
			{
				if (!IsSheetValid || !HasComponentsErrors && !HasBehaviourErrors && !HasSteeringErrors && !HasAIErrors && !HasDisabledWheels)
					return false;

				DisableAllBehaviours();
				ToolkitDebug.Error($"The {vehicle.name}'s Vehicle component has been disabled! It may have some problems that need to be fixed. You can check the Vehicle inspector editor log for more information.");

				return true;
			}

			private void DisableAllBehaviours()
			{
				ToolkitBehaviour[] behaviours = vehicle.GetComponentsInChildren<ToolkitBehaviour>();

				for (int i = 0; i < behaviours.Length; i++)
					behaviours[i].enabled = false;
			}

			#endregion

			#endregion

			#region Constructors

			public ProblemsAccess(Vehicle vehicle)
			{
				this.vehicle = vehicle;

				if (vehicle && !vehicle.rigidbody)
					vehicle.GetOrCreateRigidbody();

				if (vehicle && vehicle is VehicleTrailer trailer)
					vehicle.trailerInstance = trailer;
			}

			#endregion

			#region Operators

			public static bool operator ==(ProblemsAccess sheetA, ProblemsAccess sheetB)
			{
				return sheetA.Equals(sheetB);
			}
			public static bool operator !=(ProblemsAccess sheetA, ProblemsAccess sheetB)
			{
				return !(sheetA == sheetB);
			}

			#endregion
		}
		public struct InputsAccess
		{
			#region Jobs

#if !MVC_COMMUNITY
			[BurstCompile]
#endif
			private struct UpdateJob : IJob
			{
				#region Variables

				public NativeArray<InputsAccess> inputs;
				public TransmissionAccess transmission;
				public StatsAccess stats;
				public float deltaTime;
				public bool isEngineRunning;
				public bool isNOSActive;
				public bool manualTransmission;
				public bool isChangingGear;
				public bool isReversing;
				public bool isNeutral;
				public int currentGear;

				private bool automaticGearbox;

				#endregion

				#region Methods

				public void Execute()
				{
					InputsAccess data = inputs[0];

					if (data.overrideInputs)
					{
						data.SteeringWheel = data.steeringWheel;
						data.FuelPedal = data.overrideFuelPedal;
						data.BrakePedal = data.overrideBrakePedal;
						data.Handbrake = data.overrideHandbrake;
						data.ClutchPedal = data.overrideClutchPedal;
						data.EngineStartSwitchWasPressed = data.overrideEngineStartSwitchWasPressed;
						data.GearShiftUpWasPressed = data.overrideGearShiftUpWasPressed;
						data.GearShiftDownWasPressed = data.overrideGearShiftDownWasPressed;
						data.NOS = data.overrideNOSButton;
						data.LaunchControlSwitchWasPressed = data.overrideLaunchControlSwitchWasPressed;
						data.ResetButtonWasPressed = data.overrideResetButtonWasPressed;
						data.Direction = data.overrideDirection;
						data.AnyInputWasPressed = data.FuelPedalWasPressed || !manualTransmission && data.BrakePedalWasPressed || data.GearShiftUpWasPressed || data.GearShiftDownWasPressed || data.EngineStartSwitchWasPressed || data.NOS || data.LaunchControlSwitchWasPressed;
					}

					if (!isEngineRunning)
						data.RawFuel = 0f;
					else if (isNOSActive)
						data.RawFuel = 1f;
					else if (manualTransmission)
						data.RawFuel = Utility.Clamp01(data.FuelPedal);
					else
						data.RawFuel = isReversing ? Utility.Clamp01(data.BrakePedal) : Utility.Clamp01(data.FuelPedal);

					data.Fuel = stats.isRevLimiting ? 0f : data.RawFuel;

					if (manualTransmission)
						data.Brake = Utility.Clamp01(data.BrakePedal);
					else
						data.Brake = isReversing ? Utility.Clamp01(data.FuelPedal) : Utility.Clamp01(data.BrakePedal);

					data.wheelsSpeed = stats.averageMotorWheelsSpeedAbs;
					data.minimumSpeed = transmission.minSpeedTargets[currentGear];
					automaticGearbox = transmission.gearbox == TransmissionModule.GearboxType.Automatic;

					if (isNeutral)
						data.Clutch = Mathf.MoveTowards(data.Clutch, 1f, deltaTime / transmission.clutchOutDelay);
					else if (data.wheelsSpeed < data.minimumSpeed * transmission.efficiency && automaticGearbox)
						data.Clutch = Utility.Lerp(data.Clutch, Mathf.Sign(stats.averageMotorWheelsRPM) == Mathf.Sign(data.Direction) ? Utility.Lerp(1f - Utility.Clamp01(data.wheelsSpeed / data.minimumSpeed * transmission.efficiency), 0f, data.wheelsSpeed / data.minimumSpeed * transmission.efficiency) : 0f, deltaTime * 50f);
					else// if (useAutomaticClutch)
						data.Clutch = Mathf.MoveTowards(data.Clutch, Utility.BoolToNumber(isChangingGear), deltaTime / (isChangingGear ? transmission.clutchOutDelay : transmission.clutchInDelay));

					if (automaticGearbox && isNeutral)
						data.ClutchPedalOrHandbrake = Utility.Clamp01(data.ClutchPedal + data.Handbrake + data.FuelPedal * data.BrakePedal);
					else
						data.ClutchPedalOrHandbrake = Utility.Clamp01(data.ClutchPedal + data.Handbrake);

					data.Clutch = Utility.Max(data.Clutch, data.ClutchPedalOrHandbrake, Utility.Clamp01(1f - transmission.efficiency));

					inputs[0] = data;
				}

				#endregion
			}
#if !MVC_COMMUNITY
			[BurstCompile]
#endif
			private struct RumbleJob : IJob
			{
				#region Variables

				public VehicleGroundMapper.GroundAccess ground;
				public NativeArray<InputsAccess> inputs;
				public VehicleCameraAccess camera;
				public BehaviourAccess behaviour;
				public StatsAccess stats;
				public float idleMaximumSpeed;
				public float NOSBoost;
				public ToolkitSettings.GamepadRumbleType gamepadRumbleType;
				public int gamepadRumbleMask;
				public Utility.Interval gamepadRumbleSpeedInterval;
				public bool useGamepadLevelledRumble;
				public bool useSeparateGamepadRumbleSides;
				public bool isNOSActive;
				public int wheelsCount;
				public int leftWheelsCount;
				public int centerWheelsCount;
				public int rightWheelsCount;
				public int motorLeftWheelsCount;
				public int motorCenterWheelsCount;
				public int motorRightWheelsCount;

				#endregion

				#region Methods

				public void Execute()
				{
					InputsAccess data = inputs[0];

					bool useOffroadRumble = gamepadRumbleType switch
					{
						ToolkitSettings.GamepadRumbleType.Independent => (gamepadRumbleMask & (int)ToolkitSettings.GamepadRumbleMask.OffRoad) != 0,
						ToolkitSettings.GamepadRumbleType.FollowCamera => camera.HasFollowerFeature(VehicleCamera.FollowerFeature.OffRoadShaking) || camera.HasPivotFeature(VehicleCamera.PivotFeature.OffRoadShaking),
						_ => false
					};
					bool useNOSRumble = gamepadRumbleType switch
					{
						ToolkitSettings.GamepadRumbleType.Independent => (gamepadRumbleMask & (int)ToolkitSettings.GamepadRumbleMask.NOS) != 0,
						ToolkitSettings.GamepadRumbleType.FollowCamera => camera.HasFollowerFeature(VehicleCamera.FollowerFeature.NOSShaking) || camera.HasPivotFeature(VehicleCamera.PivotFeature.NOSShaking),
						_ => false
					};
					bool useSidewaysSlipRumble = gamepadRumbleType switch
					{
						ToolkitSettings.GamepadRumbleType.Independent => (gamepadRumbleMask & (int)ToolkitSettings.GamepadRumbleMask.SidewaysSlip) != 0,
						ToolkitSettings.GamepadRumbleType.FollowCamera => camera.HasFollowerFeature(VehicleCamera.FollowerFeature.SidewaysSlipShaking),
						_ => false
					};
					bool useForwardSlipRumble = gamepadRumbleType switch
					{
						ToolkitSettings.GamepadRumbleType.Independent => (gamepadRumbleMask & (int)ToolkitSettings.GamepadRumbleMask.ForwardSlip) != 0,
						ToolkitSettings.GamepadRumbleType.FollowCamera => camera.HasFollowerFeature(VehicleCamera.FollowerFeature.ForwardSlipShaking),
						_ => false
					};
					bool useBrakeSlipRumble = gamepadRumbleType switch
					{
						ToolkitSettings.GamepadRumbleType.Independent => (gamepadRumbleMask & (int)ToolkitSettings.GamepadRumbleMask.BrakeSlip) != 0,
						ToolkitSettings.GamepadRumbleType.FollowCamera => camera.HasFollowerFeature(VehicleCamera.FollowerFeature.BrakeSlipShaking),
						_ => false
					};
					bool useSpeedRumble = gamepadRumbleType switch
					{
						ToolkitSettings.GamepadRumbleType.Independent => (gamepadRumbleMask & (int)ToolkitSettings.GamepadRumbleMask.Speed) != 0,
						ToolkitSettings.GamepadRumbleType.FollowCamera => camera.HasFollowerFeature(VehicleCamera.FollowerFeature.SpeedShaking) || camera.HasPivotFeature(VehicleCamera.PivotFeature.SpeedShaking),
						_ => false
					};
					Utility.Interval speedRumbleInterval = gamepadRumbleType switch
					{
						ToolkitSettings.GamepadRumbleType.Independent => gamepadRumbleSpeedInterval,
						ToolkitSettings.GamepadRumbleType.FollowCamera => camera.shakeSpeedInterval,
						_ => default
					};
					bool hasSideWheels = leftWheelsCount > 0 || rightWheelsCount > 0;
					bool hasCenterWheels = centerWheelsCount > 0;
					float leftWheelsSpeedSign = math.sign(stats.averageFrontLeftWheelsSpeed + stats.averageRearLeftWheelsSpeed);
					float centerWheelsSpeedSign = math.sign(stats.averageFrontCenterWheelsSpeed + stats.averageRearCenterWheelsSpeed);
					float rightWheelsSpeedSign = math.sign(stats.averageFrontRightWheelsSpeed + stats.averageRearRightWheelsSpeed);
					float leftMotorWheelsSpeedSign = math.sign(stats.averageLeftMotorWheelsSpeed);
					float centerMotorWheelsSpeedSign = math.sign(stats.averageCenterMotorWheelsSpeed);
					float rightMotorWheelsSpeedSign = math.sign(stats.averageRightMotorWheelsSpeed);

					useSeparateGamepadRumbleSides &= data.usingGamepadInput;

					if (useSeparateGamepadRumbleSides)
					{
						if (useGamepadLevelledRumble)
						{
							if (hasSideWheels)
							{
								if (useOffroadRumble)
								{
									data.offRoadLeftSideRumble = (stats.offRoadingRearLeftWheelsCount + stats.offRoadingFrontLeftWheelsCount) / leftWheelsCount;
									data.offRoadRightSideRumble = (stats.offRoadingRearRightWheelsCount + stats.offRoadingFrontRightWheelsCount) / rightWheelsCount;
								}

								if (useForwardSlipRumble)
								{
									data.forwardSlipLeftSideRumble = Utility.InverseLerp(stats.averageMotorLeftWheelsForwardExtremumSlip, 1f, stats.averageMotorLeftWheelsForwardSlip * leftMotorWheelsSpeedSign);
									data.forwardSlipRightSideRumble = Utility.InverseLerp(stats.averageMotorRightWheelsForwardExtremumSlip, 1f, stats.averageMotorRightWheelsForwardSlip * rightMotorWheelsSpeedSign);
								}

								if (useBrakeSlipRumble)
								{
									data.brakeSlipLeftSideRumble = Utility.InverseLerp(stats.averageLeftWheelsForwardExtremumSlip, 1f, -stats.averageLeftWheelsForwardSlip * leftWheelsSpeedSign);
									data.brakeSlipRightSideRumble = Utility.InverseLerp(stats.averageRightWheelsForwardExtremumSlip, 1f, -stats.averageRightWheelsForwardSlip * rightWheelsSpeedSign);
								}
							}

							if (hasCenterWheels)
							{
								if (useOffroadRumble)
								{
									data.offRoadLeftSideRumble += (stats.offRoadingFrontCenterWheelsCount + stats.offRoadingRearCenterWheelsCount) / centerWheelsCount;
									data.offRoadRightSideRumble += (stats.offRoadingFrontCenterWheelsCount + stats.offRoadingRearCenterWheelsCount) / centerWheelsCount;

									if (hasSideWheels)
									{
										data.offRoadLeftSideRumble *= .5f;
										data.offRoadRightSideRumble *= .5f;
									}
								}

								if (useForwardSlipRumble)
								{
									data.forwardSlipLeftSideRumble += Utility.InverseLerp(stats.averageMotorCenterWheelsForwardExtremumSlip, 1f, stats.averageMotorCenterWheelsForwardSlip * centerMotorWheelsSpeedSign);
									data.forwardSlipRightSideRumble += Utility.InverseLerp(stats.averageMotorCenterWheelsForwardExtremumSlip, 1f, stats.averageMotorCenterWheelsForwardSlip * centerMotorWheelsSpeedSign);

									if (hasSideWheels)
									{
										data.forwardSlipLeftSideRumble *= .5f;
										data.forwardSlipRightSideRumble *= .5f;
									}
								}

								if (useBrakeSlipRumble)
								{
									data.brakeSlipLeftSideRumble += Utility.InverseLerp(stats.averageCenterWheelsForwardExtremumSlip, 1f, -stats.averageCenterWheelsForwardSlip * centerWheelsSpeedSign);
									data.brakeSlipRightSideRumble += Utility.InverseLerp(stats.averageCenterWheelsForwardExtremumSlip, 1f, -stats.averageCenterWheelsForwardSlip * centerWheelsSpeedSign);

									if (hasSideWheels)
									{
										data.brakeSlipLeftSideRumble *= .5f;
										data.brakeSlipRightSideRumble *= .5f;
									}
								}
							}
						}
						else
						{
							if (useOffroadRumble)
							{
								data.offRoadLeftSideRumble = Utility.BoolToNumber(stats.offRoadingFrontLeftWheelsCount > 0 || stats.offRoadingRearLeftWheelsCount > 0 || stats.offRoadingFrontCenterWheelsCount > 0 || stats.offRoadingRearCenterWheelsCount > 0);
								data.offRoadRightSideRumble = Utility.BoolToNumber(stats.offRoadingFrontRightWheelsCount > 0 || stats.offRoadingRearRightWheelsCount > 0 || stats.offRoadingFrontCenterWheelsCount > 0 || stats.offRoadingRearCenterWheelsCount > 0);
							}

							if (useForwardSlipRumble)
							{
								data.forwardSlipLeftSideRumble = Utility.BoolToNumber(stats.averageMotorLeftWheelsForwardSlip * leftMotorWheelsSpeedSign >= stats.averageMotorLeftWheelsForwardExtremumSlip || stats.averageMotorCenterWheelsForwardSlip * centerMotorWheelsSpeedSign >= stats.averageMotorCenterWheelsForwardExtremumSlip);
								data.forwardSlipRightSideRumble = Utility.BoolToNumber(stats.averageMotorRightWheelsForwardSlip * rightMotorWheelsSpeedSign >= stats.averageMotorRightWheelsForwardExtremumSlip || stats.averageMotorCenterWheelsForwardSlip * centerMotorWheelsSpeedSign >= stats.averageMotorCenterWheelsForwardExtremumSlip);
							}

							if (useBrakeSlipRumble)
							{
								data.brakeSlipLeftSideRumble = Utility.BoolToNumber(-stats.averageLeftWheelsForwardSlip * leftWheelsSpeedSign >= stats.averageLeftWheelsForwardExtremumSlip || -stats.averageCenterWheelsForwardSlip * centerWheelsSpeedSign >= stats.averageCenterWheelsForwardExtremumSlip);
								data.brakeSlipRightSideRumble = Utility.BoolToNumber(-stats.averageRightWheelsForwardSlip * rightWheelsSpeedSign >= stats.averageRightWheelsForwardExtremumSlip || -stats.averageCenterWheelsForwardSlip * centerWheelsSpeedSign >= stats.averageCenterWheelsForwardExtremumSlip);
							}
						}
					}
					else
					{
						if (useGamepadLevelledRumble)
						{
							if (useOffroadRumble)
								data.offRoadRightSideRumble = data.offRoadLeftSideRumble = (stats.offRoadingRearWheelsCount + stats.offRoadingFrontWheelsCount) / wheelsCount;

							if (useForwardSlipRumble)
							{
								data.forwardSlipLeftSideRumble = Utility.Average(Utility.InverseLerp(stats.averageMotorLeftWheelsForwardExtremumSlip, 1f, stats.averageMotorLeftWheelsForwardSlip * leftMotorWheelsSpeedSign), Utility.InverseLerp(stats.averageMotorRightWheelsForwardExtremumSlip, 1f, stats.averageMotorRightWheelsForwardSlip * rightMotorWheelsSpeedSign));

								if (hasCenterWheels)
								{
									data.forwardSlipLeftSideRumble += Utility.InverseLerp(stats.averageMotorCenterWheelsForwardExtremumSlip, 1f, stats.averageMotorCenterWheelsForwardSlip * centerMotorWheelsSpeedSign);

									if (hasSideWheels)
										data.forwardSlipLeftSideRumble *= .5f;
								}

								data.forwardSlipRightSideRumble = data.forwardSlipLeftSideRumble;
							}

							if (useBrakeSlipRumble)
							{
								data.brakeSlipLeftSideRumble = Utility.Average(Utility.InverseLerp(stats.averageLeftWheelsForwardExtremumSlip, 1f, -stats.averageLeftWheelsForwardSlip * leftWheelsSpeedSign), Utility.InverseLerp(stats.averageRightWheelsForwardExtremumSlip, 1f, -stats.averageMotorRightWheelsForwardSlip * rightWheelsSpeedSign));

								if (hasCenterWheels)
								{
									data.brakeSlipLeftSideRumble += Utility.InverseLerp(stats.averageCenterWheelsForwardExtremumSlip, 1f, -stats.averageCenterWheelsForwardSlip * centerWheelsSpeedSign);

									if (hasSideWheels)
										data.brakeSlipLeftSideRumble *= .5f;
								}

								data.brakeSlipRightSideRumble = data.brakeSlipLeftSideRumble;
							}
						}
						else
						{
							if (useOffroadRumble)
								data.offRoadRightSideRumble = data.offRoadLeftSideRumble = Utility.BoolToNumber(stats.isOffRoading);

							if (useForwardSlipRumble)
								data.forwardSlipRightSideRumble = data.forwardSlipLeftSideRumble = Utility.BoolToNumber(stats.averageMotorLeftWheelsForwardSlip * leftMotorWheelsSpeedSign >= stats.averageMotorLeftWheelsForwardExtremumSlip || stats.averageMotorCenterWheelsForwardSlip * centerMotorWheelsSpeedSign >= stats.averageMotorCenterWheelsForwardExtremumSlip || stats.averageMotorRightWheelsForwardSlip * rightMotorWheelsSpeedSign >= stats.averageMotorRightWheelsForwardExtremumSlip);

							if (useBrakeSlipRumble)
								data.brakeSlipRightSideRumble = data.brakeSlipLeftSideRumble = Utility.BoolToNumber(-stats.averageLeftWheelsForwardSlip * leftWheelsSpeedSign >= stats.averageLeftWheelsForwardExtremumSlip || -stats.averageCenterWheelsForwardSlip * centerWheelsSpeedSign >= stats.averageCenterWheelsForwardExtremumSlip || -stats.averageMotorRightWheelsForwardSlip * rightWheelsSpeedSign >= stats.averageRightWheelsForwardExtremumSlip);
						}
					}

					if (useSpeedRumble && useGamepadLevelledRumble)
						data.speedRightSideRumble = data.speedLeftSideRumble = Utility.InverseLerp(speedRumbleInterval.Min, speedRumbleInterval.Max, stats.currentSpeed);
					else if (useSpeedRumble)
						data.speedRightSideRumble = data.speedLeftSideRumble = Utility.BoolToNumber(stats.currentSpeed >= speedRumbleInterval.Min);
					else
						data.speedRightSideRumble = data.speedLeftSideRumble = default;

					if (useOffroadRumble)
					{
						data.offRoadRumbleMultiplier = useGamepadLevelledRumble || stats.currentSpeed >= idleMaximumSpeed ? Utility.Clamp01(stats.currentSpeed / idleMaximumSpeed) : default;
						data.offRoadLeftSideRumble *= data.offRoadRumbleMultiplier;
						data.offRoadRightSideRumble *= data.offRoadRumbleMultiplier;
					}
					else
						data.offRoadLeftSideRumble = data.offRoadRightSideRumble = data.offRoadRumbleMultiplier = default;

					if (useNOSRumble)
						data.nosRumble = useGamepadLevelledRumble ? NOSBoost / behaviour.NOSBoost : Utility.BoolToNumber(isNOSActive);
					else
						data.nosRumble = default;

					if (useSidewaysSlipRumble)
					{
						data.sidewaysSlipSideRumble = Utility.Average(math.abs(stats.averageFrontWheelsSidewaysSlip), math.abs(stats.averageRearWheelsSidewaysSlip));

						float averageSidewaysExtremumSlip = Utility.Average(math.abs(stats.averageFrontWheelsSidewaysExtremumSlip), math.abs(stats.averageRearWheelsSidewaysExtremumSlip));

						if (!useGamepadLevelledRumble)
							data.sidewaysSlipSideRumble = Utility.BoolToNumber(data.sidewaysSlipSideRumble >= averageSidewaysExtremumSlip);
					}
					else
						data.sidewaysSlipSideRumble = default;

					if (!useForwardSlipRumble)
						data.forwardSlipLeftSideRumble = data.forwardSlipRightSideRumble = default;

					if (!useBrakeSlipRumble)
						data.brakeSlipLeftSideRumble = data.brakeSlipRightSideRumble = default;

					data.gamepadLeftSideRumble = Utility.Max(data.offRoadLeftSideRumble, data.speedLeftSideRumble, data.nosRumble, data.sidewaysSlipSideRumble, data.forwardSlipLeftSideRumble, data.brakeSlipLeftSideRumble);

					if (useSeparateGamepadRumbleSides)
					{
						data.gamepadRightSideRumble = Utility.Max(data.offRoadRightSideRumble, data.speedRightSideRumble, data.nosRumble, data.sidewaysSlipSideRumble, data.forwardSlipRightSideRumble, data.brakeSlipRightSideRumble);
						data.mobileRumble = default;
					}
					else
					{
						data.mobileRumble = data.gamepadRightSideRumble = data.gamepadLeftSideRumble;

						if (data.usingMobileInput)
							data.gamepadRightSideRumble = data.gamepadLeftSideRumble = 0f;
					}

					inputs[0] = data;
				}

				#endregion
			}

			#endregion

			#region Variables

			public float SteeringWheel;
			public float FuelPedal;
			public float BrakePedal;
			public float ClutchPedal;
			public float Handbrake;
			public bool NOS;
			public bool FuelPedalWasPressed { get; internal set; }
			public bool FuelPedalWasReleased { get; internal set; }
			public bool BrakePedalWasPressed { get; internal set; }
			public bool ClutchPedalWasPressed { get; internal set; }
			public bool HandbrakeWasPressed { get; internal set; }
			public float ClutchPedalOrHandbrake { get; internal set; }
			public bool EngineStartSwitchWasPressed { get; internal set; }
			public bool GearShiftUpWasPressed { get; internal set; }
			public bool GearShiftDownWasPressed { get; internal set; }
			[Obsolete("Use `NOS` instead.", true)]
			public bool NOSButton { get; internal set; }
			public bool LaunchControlSwitchWasPressed { get; internal set; }
			public bool ResetButtonWasPressed { get; internal set; }
			public bool LightSwitchWasPressed { get; internal set; }
			public bool HighBeamLightButton { get; internal set; }
			public bool HighBeamLightSwitchWasPressed { get; internal set; }
			public bool HighBeamLightSwitchWasDoublePressed { get; internal set; }
			public bool InteriorLightSwitchWasPressed { get; internal set; }
			public bool SideSignalLeftLightSwitchWasPressed { get; internal set; }
			public bool SideSignalRightLightSwitchWasPressed { get; internal set; }
			public bool HazardLightsSwitchWasPressed { get; internal set; }
			public bool TrailerLinkSwitchWasPressed { get; internal set; }
			public float RawFuel { get; internal set; }
			public float Fuel { get; internal set; }
			public float Brake { get; internal set; }
			public float Clutch { get; internal set; }
			public bool AnyInputWasPressed { get; internal set; }
			public int Direction { get; internal set; }

			private float wheelsSpeed;
			private float minimumSpeed;
			private float speedLeftSideRumble;
			private float speedRightSideRumble;
			private float offRoadLeftSideRumble;
			private float offRoadRightSideRumble;
			private float offRoadRumbleMultiplier;
			private float nosRumble;
			private float sidewaysSlipSideRumble;
			private float forwardSlipLeftSideRumble;
			private float forwardSlipRightSideRumble;
			private float brakeSlipLeftSideRumble;
			private float brakeSlipRightSideRumble;
			private float gamepadLeftSideRumble;
			private float gamepadRightSideRumble;
			private float mobileRumble;
			private float m_gamepadLeftSideRumble;
			private float m_gamepadRightSideRumble;
			private float m_mobileRumble;
			private float steeringWheel;
			private float overrideFuelPedal;
			private float overrideBrakePedal;
			private float overrideClutchPedal;
			private float overrideHandbrake;
			[MarshalAs(UnmanagedType.U1)]
			private bool overrideInputs;
			[MarshalAs(UnmanagedType.U1)]
			private bool overrideEngineStartSwitchWasPressed;
			[MarshalAs(UnmanagedType.U1)]
			private bool overrideGearShiftUpWasPressed;
			[MarshalAs(UnmanagedType.U1)]
			private bool overrideGearShiftDownWasPressed;
			[MarshalAs(UnmanagedType.U1)]
			private bool overrideNOSButton;
			[MarshalAs(UnmanagedType.U1)]
			private bool overrideLaunchControlSwitchWasPressed;
			[MarshalAs(UnmanagedType.U1)]
			private bool overrideResetButtonWasPressed;
			[MarshalAs(UnmanagedType.U1)]
			private bool usingGamepadInput;
			[MarshalAs(UnmanagedType.U1)]
			private bool usingMobileInput;
			private int overrideDirection;

			#endregion

			#region Methods

			#region Virtual Methods

			public override readonly string ToString()
			{
				return $"Steering Wheel: {SteeringWheel:0.00} | Fuel: {Fuel:0.00} | Brake: {Brake:0.00} | Clutch: {Clutch:0.00} | Handbrake: {Handbrake:0.00}";
			}

			#endregion

			#region Static Methods

			public static void SetSteeringWheel(Vehicle vehicle, float value)
			{
				vehicle.inputs.steeringWheel = value;
			}
			public static void SetFuelPedal(Vehicle vehicle, float value)
			{
				vehicle.inputs.overrideFuelPedal = value;
				vehicle.inputs.FuelPedalWasReleased = Utility.IsUpFromLastState(value, vehicle.inputs.overrideFuelPedal);
				vehicle.inputs.FuelPedalWasPressed = Utility.IsDownFromLastState(value, vehicle.inputs.overrideFuelPedal);
			}
			public static void SetBrakePedal(Vehicle vehicle, float value)
			{
				vehicle.inputs.overrideBrakePedal = value;
				vehicle.inputs.BrakePedalWasPressed = Utility.IsDownFromLastState(value, vehicle.inputs.overrideFuelPedal);
			}
			public static void SetClutchPedal(Vehicle vehicle, float value)
			{
				vehicle.inputs.overrideClutchPedal = value;
				vehicle.inputs.ClutchPedalWasPressed = Utility.IsDownFromLastState(value, vehicle.inputs.overrideHandbrake);
			}
			public static void SetHandbrake(Vehicle vehicle, float value)
			{
				vehicle.inputs.overrideHandbrake = value;
				vehicle.inputs.HandbrakeWasPressed = Utility.IsDownFromLastState(value, vehicle.inputs.overrideHandbrake);
			}
			public static void SetStartOrEngineButtonSwitch(Vehicle vehicle, bool downState)
			{
				vehicle.inputs.overrideEngineStartSwitchWasPressed = downState;
			}
			public static void SetGearShiftUpSwitch(Vehicle vehicle, bool downState)
			{
				vehicle.inputs.overrideGearShiftUpWasPressed = downState;
			}
			public static void SetGearShiftDownSwitch(Vehicle vehicle, bool downState)
			{
				vehicle.inputs.overrideGearShiftDownWasPressed = downState;
			}
			public static void SetNOSButton(Vehicle vehicle, bool pressState)
			{
				vehicle.inputs.overrideNOSButton = pressState;
			}
			public static void SetLaunchControlSwitch(Vehicle vehicle, bool downState)
			{
				vehicle.inputs.overrideLaunchControlSwitchWasPressed = downState;
			}
			public static void SetResetButtonSwitch(Vehicle vehicle, bool downState)
			{
				vehicle.inputs.overrideResetButtonWasPressed = downState;
			}
			public static void SetDirection(Vehicle vehicle, int direction)
			{
				vehicle.inputs.overrideDirection = direction;
			}
			public static void SetOverrideInputs(Vehicle vehicle, bool state)
			{
				vehicle.inputs.overrideInputs = state;
			}
			public static bool GetOverrideInputs(Vehicle vehicle)
			{
				return vehicle.inputs.overrideInputs;
			}

			#endregion

			#region Global Methods

			internal void Update(Vehicle vehicle)
			{
				if ((!vehicle.isAI || vehicle.activeAIBehaviourIndex < 0) && vehicle && Manager.PlayerVehicle == vehicle && !overrideInputs)
				{
					switch (Settings.inputSystem)
					{
						case ToolkitSettings.InputSystem.InputsManager:
							UpdateInputsManager(vehicle);

							break;

						default:
							UpdateUnityLegacyInputSystem(vehicle);

							break;
					}

					UpdateMobileInputs(vehicle);
				}

				AnyInputWasPressed = FuelPedalWasPressed || Settings.AutomaticTransmission && BrakePedalWasPressed || GearShiftUpWasPressed || GearShiftDownWasPressed || EngineStartSwitchWasPressed || NOS || LaunchControlSwitchWasPressed;

				if (!vehicle)
					return;

				NativeArray<InputsAccess> inputs = new(1, Allocator.TempJob);

				inputs[0] = this;

				UpdateJob updateJob = new()
				{
					inputs = inputs,
					transmission = vehicle.transmissionAccess,
					stats = vehicle.Stats,
					deltaTime = Utility.DeltaTime,
					isEngineRunning = vehicle.stats.isEngineRunning,
					isNOSActive = vehicle.stats.isNOSActive,
					manualTransmission = vehicle.isAIActive || Settings.ManualTransmission,
					isChangingGear = vehicle.stats.isChangingGear,
					isReversing = vehicle.stats.isReversing,
					isNeutral = vehicle.stats.isNeutral,
					currentGear = vehicle.stats.currentGear
				};

				updateJob.Schedule().Complete();

				this = inputs[0];

				if ((Settings.gamepadRumbleType != ToolkitSettings.GamepadRumbleType.Off && Settings.gamepadRumbleType != ToolkitSettings.GamepadRumbleType.Independent || Settings.gamepadRumbleType == ToolkitSettings.GamepadRumbleType.Independent && Settings.gamepadRumbleMask != ToolkitSettings.GamepadRumbleMask.Nothing) && Follower && Follower.Cameras != null && (usingGamepadInput || usingMobileInput))
				{
					RumbleJob rumbleJob = new()
					{
						ground = vehicle.grounds[vehicle.stats.clampedGroundIndex],
						camera = Follower.Cameras[Follower.CurrentCameraIndex],
						inputs = inputs,
						behaviour = vehicle.behaviourAccess,
						stats = vehicle.stats,
						idleMaximumSpeed = Follower.IdleMaximumSpeed,
						NOSBoost = vehicle.stats.NOSBoost,
						gamepadRumbleType = Settings.gamepadRumbleType,
						gamepadRumbleMask = (int)Settings.gamepadRumbleMask,
						gamepadRumbleSpeedInterval = Settings.GamepadRumbleSpeedInterval,
						useGamepadLevelledRumble = Settings.useGamepadLevelledRumble,
						useSeparateGamepadRumbleSides = Settings.useSeparateGamepadRumbleSides,
						isNOSActive = vehicle.stats.isNOSActive,
						wheelsCount = vehicle.wheels.Length,
						leftWheelsCount = vehicle.leftWheels.Length,
						centerWheelsCount = vehicle.centerWheels.Length,
						rightWheelsCount = vehicle.rightWheels.Length,
						motorLeftWheelsCount = vehicle.motorLeftWheels.Length,
						motorCenterWheelsCount = vehicle.motorCenterWheels.Length,
						motorRightWheelsCount = vehicle.motorRightWheels.Length,
					};

					rumbleJob.Schedule().Complete();
				}
				else if (gamepadLeftSideRumble != 0f || gamepadRightSideRumble != 0f || mobileRumble != 0f)
				{
					gamepadLeftSideRumble = 0f;
					gamepadRightSideRumble = 0f;
					mobileRumble = 0f;
				}

				if (!Mathf.Approximately(gamepadLeftSideRumble, m_gamepadLeftSideRumble) || Mathf.Approximately(gamepadRightSideRumble, m_gamepadRightSideRumble))
				{
					InputsManager.GamepadVibration(gamepadLeftSideRumble, gamepadRightSideRumble, Manager.PlayerGamepadIndex);

					m_gamepadLeftSideRumble = gamepadLeftSideRumble;
					m_gamepadRightSideRumble = gamepadRightSideRumble;
				}

				if (!Mathf.Approximately(mobileRumble, m_mobileRumble))
				{
					// Apply mobile vibration intensity

					m_mobileRumble = mobileRumble;
				}

				vehicle.inputs = inputs[0];

				inputs.Dispose();
			}
			internal void ResetInputs(Vehicle vehicle)
			{
				SteeringWheel = default;
				FuelPedal = default;
				FuelPedalWasPressed = default;
				FuelPedalWasReleased = default;
				RawFuel = default;
				Fuel = default;
				BrakePedal = default;
				BrakePedalWasPressed = default;
				Brake = default;
				Handbrake = default;
				HandbrakeWasPressed = default;
				ClutchPedal = default;
				ClutchPedalWasPressed = default;
				Clutch = 1f;
				ClutchPedalOrHandbrake = default;
				vehicle.inputs = this;
			}

			private void UpdateInputsManager(Vehicle vehicle)
			{
				SteeringWheel = InputsManager.InputValue(Settings.steerInput, Manager.PlayerGamepadIndex);
				FuelPedal = InputsManager.InputValue(Settings.fuelInput, Manager.PlayerGamepadIndex);
				FuelPedalWasPressed = InputsManager.InputDown(Settings.fuelInput, Manager.PlayerGamepadIndex);
				FuelPedalWasReleased = InputsManager.InputUp(Settings.fuelInput, Manager.PlayerGamepadIndex);
				BrakePedal = InputsManager.InputValue(Settings.brakeInput, Manager.PlayerGamepadIndex);
				BrakePedalWasPressed = InputsManager.InputDown(Settings.brakeInput, Manager.PlayerGamepadIndex);
				ClutchPedal = InputsManager.InputValue(Settings.clutchInput, Manager.PlayerGamepadIndex);
				ClutchPedalWasPressed = InputsManager.InputDown(Settings.clutchInput, Manager.PlayerGamepadIndex);
				Handbrake = InputsManager.InputValue(Settings.handbrakeInput, Manager.PlayerGamepadIndex);
				HandbrakeWasPressed = InputsManager.InputDown(Settings.handbrakeInput, Manager.PlayerGamepadIndex);
				EngineStartSwitchWasPressed = (Settings.engineStartMode != ToolkitSettings.EngineStartMode.Always || vehicle.stats.isEngineStall) && InputsManager.InputDown(Settings.engineStartSwitchInput, Manager.PlayerGamepadIndex);
				GearShiftUpWasPressed = InputsManager.InputUp(Settings.gearShiftUpButtonInput, Manager.PlayerGamepadIndex);
				GearShiftDownWasPressed = InputsManager.InputUp(Settings.gearShiftDownButtonInput, Manager.PlayerGamepadIndex);
				NOS = InputsManager.InputPress(Settings.NOSButtonInput, Manager.PlayerGamepadIndex);
				LaunchControlSwitchWasPressed = InputsManager.InputDown(Settings.launchControlSwitchInput, Manager.PlayerGamepadIndex);
				ResetButtonWasPressed = InputsManager.InputDown(Settings.resetButtonInput, Manager.PlayerGamepadIndex);
				LightSwitchWasPressed = InputsManager.InputDown(Settings.lightSwitchInput, Manager.PlayerGamepadIndex);
				HighBeamLightButton = InputsManager.InputPress(Settings.highBeamLightSwitchInput, Manager.PlayerGamepadIndex);
				HighBeamLightSwitchWasPressed = InputsManager.InputDown(Settings.highBeamLightSwitchInput, Manager.PlayerGamepadIndex);
				HighBeamLightSwitchWasDoublePressed = InputsManager.InputDoublePress(Settings.highBeamLightSwitchInput, Manager.PlayerGamepadIndex);
				InteriorLightSwitchWasPressed = InputsManager.InputDown(Settings.interiorLightSwitchInput, Manager.PlayerGamepadIndex);
				SideSignalLeftLightSwitchWasPressed = InputsManager.InputDown(Settings.sideSignalLeftLightSwitchInput, Manager.PlayerGamepadIndex);
				SideSignalRightLightSwitchWasPressed = InputsManager.InputDown(Settings.sideSignalRightLightSwitchInput, Manager.PlayerGamepadIndex);
				HazardLightsSwitchWasPressed = InputsManager.InputDown(Settings.hazardLightsSwitchInput, Manager.PlayerGamepadIndex);
				TrailerLinkSwitchWasPressed = InputsManager.InputDown(Settings.trailerLinkSwitchInput, Manager.PlayerGamepadIndex);
				usingGamepadInput = InputsManager.LastDefaultInputSource == InputSource.Gamepad;

				if (usingGamepadInput)
					usingMobileInput = false;
			}
			private void UpdateMobileInputs(Vehicle vehicle)
			{
				if (InputsManager.AnyInputPress(true) || !Settings.useMobileInputs || !UIController || UIController.MobilePresets.Length < 1)
					return;

				VehicleUIController.MobileInputPreset mobilePreset = UIController.MobilePresets[UIController.ActiveMobilePreset];

				if (!mobilePreset.AnyInputInUse)
					return;

				SteeringWheel = mobilePreset.SteeringWheelRight.Value - mobilePreset.SteeringWheelLeft.Value;
				FuelPedalWasReleased = mobilePreset.FuelPedal.Source ? mobilePreset.FuelPedal.Source.WasReleased : Utility.IsUpFromLastState(math.round(mobilePreset.FuelPedal.Value), math.round(FuelPedal));
				FuelPedalWasPressed = mobilePreset.FuelPedal.Source ? mobilePreset.FuelPedal.Source.WasPressed : Utility.IsDownFromLastState(math.round(mobilePreset.FuelPedal.Value), math.round(FuelPedal));
				FuelPedal = mobilePreset.FuelPedal.Value;
				BrakePedalWasPressed = mobilePreset.BrakePedal.Source ? mobilePreset.BrakePedal.Source.WasPressed : Utility.IsDownFromLastState(math.round(mobilePreset.BrakePedal.Value), math.round(BrakePedal));
				BrakePedal = mobilePreset.BrakePedal.Value;
				ClutchPedalWasPressed = mobilePreset.ClutchPedal.Source ? mobilePreset.ClutchPedal.Source.WasPressed : Utility.IsDownFromLastState(math.round(mobilePreset.ClutchPedal.Value), math.round(ClutchPedal));
				ClutchPedal = mobilePreset.ClutchPedal.Value;
				HandbrakeWasPressed = mobilePreset.Handbrake.Source ? mobilePreset.Handbrake.Source.WasPressed : Utility.IsDownFromLastState(math.round(mobilePreset.Handbrake.Value), math.round(Handbrake));
				Handbrake = mobilePreset.Handbrake.Value;
				EngineStartSwitchWasPressed = (Settings.engineStartMode != ToolkitSettings.EngineStartMode.Always || vehicle.stats.isEngineStall) && mobilePreset.EngineStartSwitch.IsValid && mobilePreset.EngineStartSwitch.Source.WasPressed;
				GearShiftUpWasPressed = mobilePreset.GearShiftUp.IsValid && mobilePreset.GearShiftUp.Source.WasPressed;
				GearShiftDownWasPressed = mobilePreset.GearShiftDown.IsValid && mobilePreset.GearShiftDown.Source.WasPressed;
				NOS = mobilePreset.NOS.IsValid && mobilePreset.NOS.Source.IsPressed;
				LaunchControlSwitchWasPressed = mobilePreset.LaunchControlSwitch.IsValid && mobilePreset.LaunchControlSwitch.Source.WasPressed;
				ResetButtonWasPressed = mobilePreset.Reset.IsValid && mobilePreset.Reset.Source.WasPressed;
				LightSwitchWasPressed = mobilePreset.LightSwitch.IsValid && mobilePreset.LightSwitch.Source.WasPressed;
				HighBeamLightButton = mobilePreset.HighBeamLightSwitch.IsValid && mobilePreset.HighBeamLightSwitch.Source.IsPressed;
				HighBeamLightSwitchWasPressed = mobilePreset.HighBeamLightSwitch.IsValid && mobilePreset.HighBeamLightSwitch.Source.WasPressed;
				HighBeamLightSwitchWasDoublePressed = mobilePreset.HighBeamLightSwitch.IsValid && mobilePreset.HighBeamLightSwitch.Source.WasPressed;
				InteriorLightSwitchWasPressed = mobilePreset.InteriorLightSwitch.IsValid && mobilePreset.InteriorLightSwitch.Source.WasPressed;
				SideSignalLeftLightSwitchWasPressed = mobilePreset.LeftSideSignalSwitch.IsValid && mobilePreset.LeftSideSignalSwitch.Source.WasPressed;
				SideSignalRightLightSwitchWasPressed = mobilePreset.RightSideSignalSwitch.IsValid && mobilePreset.RightSideSignalSwitch.Source.WasPressed;
				HazardLightsSwitchWasPressed = mobilePreset.HazardLightsSwitch.IsValid && mobilePreset.HazardLightsSwitch.Source.WasPressed;
				TrailerLinkSwitchWasPressed = mobilePreset.TrailerLinkSwitch.IsValid && mobilePreset.TrailerLinkSwitch.Source.WasPressed;
				usingGamepadInput = false;
				usingMobileInput = true;
			}
			private void UpdateUnityLegacyInputSystem(Vehicle vehicle)
			{
				SteeringWheel = Input.GetAxis(Settings.steerInput);
				FuelPedal = Input.GetAxis(Settings.fuelInput);
				FuelPedalWasPressed = Input.GetButtonDown(Settings.fuelInput);
				FuelPedalWasReleased = Input.GetButtonUp(Settings.fuelInput);
				BrakePedal = Input.GetAxis(Settings.brakeInput);
				BrakePedalWasPressed = Input.GetButtonDown(Settings.brakeInput);
				ClutchPedal = Input.GetAxis(Settings.clutchInput);
				ClutchPedalWasPressed = Input.GetButtonDown(Settings.clutchInput);
				Handbrake = Input.GetAxis(Settings.handbrakeInput);
				HandbrakeWasPressed = Input.GetButtonDown(Settings.handbrakeInput);
				EngineStartSwitchWasPressed = (Settings.engineStartMode != ToolkitSettings.EngineStartMode.Always || vehicle.stats.isEngineStall) && Input.GetButtonDown(Settings.engineStartSwitchInput);
				GearShiftUpWasPressed = Input.GetButtonUp(Settings.gearShiftUpButtonInput);
				GearShiftDownWasPressed = Input.GetButtonUp(Settings.gearShiftDownButtonInput);
				NOS = Input.GetButton(Settings.NOSButtonInput);
				LaunchControlSwitchWasPressed = Input.GetButtonDown(Settings.launchControlSwitchInput);
				ResetButtonWasPressed = Input.GetButtonDown(Settings.resetButtonInput);
				LightSwitchWasPressed = Input.GetButtonDown(Settings.lightSwitchInput);
				HighBeamLightButton = Input.GetButton(Settings.highBeamLightSwitchInput);
				HighBeamLightSwitchWasPressed = Input.GetButtonDown(Settings.highBeamLightSwitchInput);
				InteriorLightSwitchWasPressed = Input.GetButtonDown(Settings.interiorLightSwitchInput);
				SideSignalLeftLightSwitchWasPressed = Input.GetButtonDown(Settings.sideSignalLeftLightSwitchInput);
				SideSignalRightLightSwitchWasPressed = Input.GetButtonDown(Settings.sideSignalRightLightSwitchInput);
				HazardLightsSwitchWasPressed = Input.GetButtonDown(Settings.hazardLightsSwitchInput);
				TrailerLinkSwitchWasPressed = Input.GetButtonDown(Settings.trailerLinkSwitchInput);
				usingGamepadInput = false;

				if (usingGamepadInput)
					usingMobileInput = false;
			}

			#endregion

			#endregion
		}
#pragma warning disable IDE0064 // Make readonly fields writable
		public struct StatsAccess
		{
			#region Jobs

#if !MVC_COMMUNITY
			[BurstCompile]
#endif
			private struct FixedUpdateJob : IJob
			{
				#region Variables

				public NativeArray<VehicleGroundMapper.GroundAccess> grounds;
				public NativeArray<VehicleWheel.WheelAccess> wheelsAccess;
				public NativeArray<VehicleWheel.WheelStatsAccess> wheelStats;
#if !MVC_COMMUNITY
				public NativeArray<int> groundsAppearance;
#endif
				public NativeArray<StatsAccess> stats;
				public VehicleEngineAccess engineAccess;
				public BehaviourAccess behaviourAccess;
				public TransmissionAccess transmissionAccess;
				public InputsAccess inputs;
				public float deltaTime;
				public float gravity;
				public uint randomSeed;

				#endregion

				#region Methods

				public void Execute()
				{
					StatsAccess data = stats[0];

					data.localVelocity = !data.velocity.Equals(float3.zero) ? math.mul(math.inverse(data.rotation), data.velocity) : float3.zero;
					data.localAngularVelocity = !data.velocity.Equals(float3.zero) ? math.mul(math.inverse(data.rotation), data.angularVelocity) : float3.zero;
					data.currentSpeed = math.length(data.velocity) * 3.6f;
					data.averageFrontLeftWheelsHitPoint = float3.zero;
					data.averageFrontCenterWheelsHitPoint = float3.zero;
					data.averageFrontRightWheelsHitPoint = float3.zero;
					data.averageRearLeftWheelsHitPoint = float3.zero;
					data.averageRearCenterWheelsHitPoint = float3.zero;
					data.averageRearRightWheelsHitPoint = float3.zero;
					data.averageSteerWheelsForwardDirection = float3.zero;
					data.groundedWheelsCount = 0;
					data.groundedFrontWheelsCount = 0;
					data.groundedFrontLeftWheelsCount = 0;
					data.groundedFrontCenterWheelsCount = 0;
					data.groundedFrontRightWheelsCount = 0;
					data.groundedRearWheelsCount = 0;
					data.groundedRearLeftWheelsCount = 0;
					data.groundedRearCenterWheelsCount = 0;
					data.groundedRearRightWheelsCount = 0;
					data.offRoadingWheelsCount = 0;
					data.offRoadingMotorWheelsCount = 0;
					data.offRoadingFrontWheelsCount = 0;
					data.offRoadingFrontLeftWheelsCount = 0;
					data.offRoadingFrontCenterWheelsCount = 0;
					data.offRoadingFrontRightWheelsCount = 0;
					data.offRoadingRearWheelsCount = 0;
					data.offRoadingRearLeftWheelsCount = 0;
					data.offRoadingRearCenterWheelsCount = 0;
					data.offRoadingRearRightWheelsCount = 0;
					data.averageFrontLeftWheelsRadius = 0f;
					data.averageFrontCenterWheelsRadius = 0f;
					data.averageFrontRightWheelsRadius = 0f;
					data.averageRearLeftWheelsRadius = 0f;
					data.averageRearCenterWheelsRadius = 0f;
					data.averageRearRightWheelsRadius = 0f;
					data.averageMotorWheelsRadius = 0f;
					data.averageWheelsRadius = 0f;
					data.averageWheelsBrakeTorque = 0f;
					data.averageFrontWheelsForwardFrictionStiffness = 0f;
					data.averageRearWheelsForwardFrictionStiffness = 0f;
					data.averageFrontWheelsSidewaysFrictionStiffness = 0f;
					data.averageRearWheelsSidewaysFrictionStiffness = 0f;
					data.averageWheelsForwardFriction = 0f;
					data.averageFrontWheelsForwardExtremumSlip = 0f;
					data.averageFrontWheelsForwardSlip = 0f;
					data.averageFrontWheelsSidewaysExtremumSlip = 0f;
					data.averageFrontWheelsSidewaysSlip = 0f;
					data.averageRearWheelsForwardExtremumSlip = 0f;
					data.averageRearWheelsForwardSlip = 0f;
					data.averageRearWheelsSidewaysExtremumSlip = 0f;
					data.averageRearWheelsSidewaysSlip = 0f;
					data.averageMotorLeftWheelsForwardExtremumSlip = 0f;
					data.averageMotorLeftWheelsForwardSlip = 0f;
					data.averageMotorCenterWheelsForwardExtremumSlip = 0f;
					data.averageMotorCenterWheelsForwardSlip = 0f;
					data.averageMotorRightWheelsForwardExtremumSlip = 0f;
					data.averageMotorRightWheelsForwardSlip = 0f;
					data.averageMotorWheelsSidewaysSlip = 0f;
					data.averageNonSteerWheelsSidewaysSlip = 0f;
					data.averageLeftWheelsForwardExtremumSlip = 0f;
					data.averageLeftWheelsForwardSlip = 0f;
					data.averageLeftWheelsSidewaysExtremumSlip = 0f;
					data.averageLeftWheelsSidewaysSlip = 0f;
					data.averageCenterWheelsForwardExtremumSlip = 0f;
					data.averageCenterWheelsForwardSlip = 0f;
					data.averageCenterWheelsSidewaysExtremumSlip = 0f;
					data.averageCenterWheelsSidewaysSlip = 0f;
					data.averageRightWheelsForwardExtremumSlip = 0f;
					data.averageRightWheelsForwardSlip = 0f;
					data.averageRightWheelsSidewaysExtremumSlip = 0f;
					data.averageRightWheelsSidewaysSlip = 0f;
					data.averageLeftMotorWheelsSpeed = 0f;
					data.averageCenterMotorWheelsSpeed = 0f;
					data.averageRightMotorWheelsSpeed = 0f;
					data.averageFrontLeftWheelsSpeed = 0f;
					data.averageFrontCenterWheelsSpeed = 0f;
					data.averageFrontRightWheelsSpeed = 0f;
					data.averageRearLeftWheelsSpeed = 0f;
					data.averageRearCenterWheelsSpeed = 0f;
					data.averageRearRightWheelsSpeed = 0f;
					data.averageNonMotorWheelsSpeed = 0f;
					data.averageMotorWheelsSpeed = 0f;
					data.averageMotorWheelsRPM = 0f;
					data.clampedGroundIndex = 0;
					data.averageMotorWheelsFrictionStiffnessSqr = 0f;
					data.groundedSteerWheelsCount = 0;
					data.groundedMotorWheelsCount = 0;
					data.groundedNonSteerWheelsCount = 0;
					data.groundIndex = -1;
					data.wheelTorque = 0f;
					data.isGrounded = false;
					data.isFullyGrounded = true;
					data.isOffRoading = false;
					data.isFullyOffRoading = true;

					int wheelsCount = wheelsAccess.Length;

					for (int i = 0; i < wheelsCount; i++)
					{
						var wheelAccess = wheelsAccess[i];
						var wheelStat = wheelStats[i];

						if (wheelAccess.IsFrontWheel)
						{
							data.averageFrontWheelsForwardFrictionStiffness += wheelStat.forwardFrictionStiffness;
							data.averageFrontWheelsSidewaysFrictionStiffness += wheelStat.sidewaysFrictionStiffness;
							data.averageFrontWheelsForwardExtremumSlip += wheelStat.currentWheelColliderForwardFriction.extremumSlip;
							data.averageFrontWheelsForwardSlip += wheelStat.forwardSlip;
							data.averageFrontWheelsSidewaysExtremumSlip += wheelAccess.wheelColliderSidewaysFriction.extremumSlip;
							data.averageFrontWheelsSidewaysSlip += wheelStat.sidewaysSlip;

							if (wheelAccess.IsLeftWheel)
							{
								data.averageFrontLeftWheelsRadius += wheelStat.currentRadius;
								data.averageFrontLeftWheelsSpeed += wheelStat.speed;

								if (wheelStat.isGrounded)
									data.groundedFrontLeftWheelsCount++;

								if (wheelStat.isOffRoading)
									data.offRoadingFrontLeftWheelsCount++;
							}
							else if (wheelAccess.IsRightWheel)
							{
								data.averageFrontRightWheelsRadius += wheelStat.currentRadius;
								data.averageFrontRightWheelsSpeed = wheelStat.speed;

								if (wheelStat.isGrounded)
									data.groundedFrontRightWheelsCount++;

								if (wheelStat.isOffRoading)
									data.offRoadingFrontRightWheelsCount++;
							}
							else
							{
								data.averageFrontCenterWheelsRadius += wheelStat.currentRadius;
								data.averageFrontCenterWheelsSpeed += wheelStat.speed;

								if (wheelStat.isGrounded)
									data.groundedFrontCenterWheelsCount++;

								if (wheelStat.isOffRoading)
									data.offRoadingFrontCenterWheelsCount++;
							}

							if (wheelStat.isGrounded)
								data.groundedFrontWheelsCount++;

							if (wheelStat.isOffRoading)
								data.offRoadingFrontWheelsCount++;
						}
						else
						{
							data.averageRearWheelsForwardFrictionStiffness += wheelStat.forwardFrictionStiffness;
							data.averageRearWheelsSidewaysFrictionStiffness += wheelStat.sidewaysFrictionStiffness;
							data.averageRearWheelsForwardExtremumSlip += wheelStat.currentWheelColliderForwardFriction.extremumSlip;
							data.averageRearWheelsForwardSlip += wheelStat.forwardSlip;
							data.averageRearWheelsSidewaysExtremumSlip += wheelAccess.wheelColliderSidewaysFriction.extremumSlip;
							data.averageRearWheelsSidewaysSlip += wheelStat.sidewaysSlip;

							if (wheelAccess.IsLeftWheel)
							{
								data.averageRearLeftWheelsRadius += wheelStat.currentRadius;
								data.averageRearLeftWheelsSpeed += wheelStat.speed;

								if (wheelStat.isGrounded)
									data.groundedRearLeftWheelsCount++;

								if (wheelStat.isOffRoading)
									data.offRoadingRearLeftWheelsCount++;
							}
							else if (wheelAccess.IsRightWheel)
							{
								data.averageRearRightWheelsRadius += wheelStat.currentRadius;
								data.averageRearRightWheelsSpeed += wheelStat.speed;

								if (wheelStat.isGrounded)
									data.groundedRearRightWheelsCount++;

								if (wheelStat.isOffRoading)
									data.offRoadingRearRightWheelsCount++;
							}
							else
							{
								data.averageRearCenterWheelsRadius += wheelStat.currentRadius;
								data.averageRearCenterWheelsSpeed += wheelStat.speed;

								if (wheelStat.isGrounded)
									data.groundedRearCenterWheelsCount++;

								if (wheelStat.isOffRoading)
									data.offRoadingRearCenterWheelsCount++;
							}

							if (wheelStat.isGrounded)
								data.groundedRearWheelsCount++;

							if (wheelStat.isOffRoading)
								data.offRoadingRearWheelsCount++;
						}

						if (wheelAccess.IsLeftWheel)
						{
							data.averageLeftWheelsForwardExtremumSlip += wheelStat.currentWheelColliderForwardFriction.extremumSlip;
							data.averageLeftWheelsForwardSlip += wheelStat.forwardSlip;
							data.averageLeftWheelsSidewaysExtremumSlip += wheelAccess.wheelColliderSidewaysFriction.extremumSlip;
							data.averageLeftWheelsSidewaysSlip += wheelStat.sidewaysSlip;
						}
						else if (wheelAccess.IsRightWheel)
						{
							data.averageRightWheelsForwardExtremumSlip += wheelStat.currentWheelColliderForwardFriction.extremumSlip;
							data.averageRightWheelsForwardSlip += wheelStat.forwardSlip;
							data.averageRightWheelsSidewaysExtremumSlip += wheelAccess.wheelColliderSidewaysFriction.extremumSlip;
							data.averageRightWheelsSidewaysSlip += wheelStat.sidewaysSlip;
						}
						else
						{
							data.averageCenterWheelsForwardExtremumSlip += wheelStat.currentWheelColliderForwardFriction.extremumSlip;
							data.averageCenterWheelsForwardSlip += wheelStat.forwardSlip;
							data.averageCenterWheelsSidewaysExtremumSlip += wheelAccess.wheelColliderSidewaysFriction.extremumSlip;
							data.averageCenterWheelsSidewaysSlip += wheelStat.sidewaysSlip;
						}

						if (wheelAccess.isMotorWheel)
						{
							if (wheelAccess.IsLeftWheel)
							{
								data.averageMotorLeftWheelsForwardExtremumSlip += wheelStat.currentWheelColliderForwardFriction.extremumSlip;
								data.averageMotorLeftWheelsForwardSlip += wheelStat.forwardSlip;
								data.averageLeftMotorWheelsSpeed += wheelStat.speed;
							}
							else if (wheelAccess.IsRightWheel)
							{
								data.averageMotorRightWheelsForwardExtremumSlip += wheelStat.currentWheelColliderForwardFriction.extremumSlip;
								data.averageMotorRightWheelsForwardSlip += wheelStat.forwardSlip;
								data.averageRightMotorWheelsSpeed += wheelStat.speed;
							}
							else
							{
								data.averageMotorCenterWheelsForwardExtremumSlip += wheelStat.currentWheelColliderForwardFriction.extremumSlip;
								data.averageMotorCenterWheelsForwardSlip += wheelStat.forwardSlip;
								data.averageCenterMotorWheelsSpeed += wheelStat.speed;
							}

							if (wheelStat.isGrounded)
								data.groundedMotorWheelsCount++;

							if (wheelStat.isOffRoading)
								data.offRoadingMotorWheelsCount++;

							data.averageMotorWheelsSidewaysSlip += wheelStat.sidewaysSlip;
							data.averageMotorWheelsSpeed += wheelStat.speed;
							data.averageMotorWheelsRPM += wheelStat.rpm;
							data.averageMotorWheelsRadius += wheelStat.currentRadius;
							data.averageMotorWheelsFrictionStiffnessSqr += wheelStat.forwardFrictionStiffness * wheelStat.sidewaysFrictionStiffness;
							data.wheelTorque += wheelStat.motorTorque;
						}
						else
							data.averageNonMotorWheelsSpeed += wheelStat.speed;

						if (wheelAccess.isSteerWheel)
						{
							data.averageSteerWheelsForwardDirection += wheelStat.forwardDir;
							data.averageSteerWheelsSidewaysSlip += wheelStat.sidewaysSlip;

							if (wheelStat.isGrounded)
								data.groundedSteerWheelsCount++;
						}
						else
						{
							data.averageNonSteerWheelsSidewaysSlip += wheelStat.sidewaysSlip;

							if (wheelStat.isGrounded)
								data.groundedNonSteerWheelsCount++;
						}

						data.isGrounded |= wheelStat.isGrounded;
						data.isFullyGrounded &= wheelStat.isGrounded;
						data.isOffRoading |= wheelStat.isOffRoading;
						data.isFullyOffRoading &= wheelStat.isOffRoading;

						if (wheelStat.isGrounded)
						{
#if !MVC_COMMUNITY
							groundsAppearance[wheelStat.clampedCurrentGroundIndex]++;
#endif
							data.groundedWheelsCount++;
						}

						if (wheelStat.isOffRoading)
							data.offRoadingWheelsCount++;

						if (wheelAccess.IsFrontWheel)
						{
							if (wheelAccess.IsLeftWheel)
								data.averageFrontLeftWheelsHitPoint += Utility.PointWorldToLocal(wheelStat.hitPoint, wheelStat.position, wheelStat.rotation, wheelStat.scale);
							else if (wheelAccess.IsRightWheel)
								data.averageFrontRightWheelsHitPoint += Utility.PointWorldToLocal(wheelStat.hitPoint, wheelStat.position, wheelStat.rotation, wheelStat.scale);
							else
								data.averageFrontCenterWheelsHitPoint += Utility.PointWorldToLocal(wheelStat.hitPoint, wheelStat.position, wheelStat.rotation, wheelStat.scale);
						}
						else
						{
							if (wheelAccess.IsLeftWheel)
								data.averageRearLeftWheelsHitPoint += Utility.PointWorldToLocal(wheelStat.hitPoint, wheelStat.position, wheelStat.rotation, wheelStat.scale);
							else if (wheelAccess.IsRightWheel)
								data.averageRearRightWheelsHitPoint += Utility.PointWorldToLocal(wheelStat.hitPoint, wheelStat.position, wheelStat.rotation, wheelStat.scale);
							else
								data.averageRearCenterWheelsHitPoint += Utility.PointWorldToLocal(wheelStat.hitPoint, wheelStat.position, wheelStat.rotation, wheelStat.scale);
						}

						data.averageWheelsForwardFriction += wheelStat.currentWheelColliderForwardFriction.ApproximateEvaluation(math.abs(wheelStat.forwardSlip));
						data.averageWheelsRadius += wheelAccess.radius;
						data.averageWheelsBrakeTorque += wheelStat.brakeTorque;
					}

					if (!data.isFullyGrounded && data.oldIsFullyGrounded)
						data.lastAirTime = 0f;

					data.oldIsFullyGrounded = data.isFullyGrounded;

					if (data.isGrounded)
#if MVC_COMMUNITY
						data.groundIndex = 0;
#else
						for (int i = 0; i < groundsAppearance.Length; i++)
						{
							if (data.groundIndex < 0 || groundsAppearance[i] > groundsAppearance[data.groundIndex])
								data.groundIndex = i;
						}
#endif
					else
					{
						if (data.localVelocity.z * 3.6f > 10f)
							data.lastAirTime += deltaTime;

						data.groundIndex = -1;
					}

					data.clampedGroundIndex = math.max(data.groundIndex, 0);

					if (data.frontWheelsCount > 1)
					{
						data.averageFrontWheelsForwardFrictionStiffness /= data.frontWheelsCount;
						data.averageFrontWheelsSidewaysFrictionStiffness /= data.frontWheelsCount;
						data.averageFrontWheelsForwardExtremumSlip /= data.frontWheelsCount;
						data.averageFrontWheelsForwardSlip /= data.frontWheelsCount;
						data.averageFrontWheelsSidewaysExtremumSlip /= data.frontWheelsCount;
						data.averageFrontWheelsSidewaysSlip /= data.frontWheelsCount;

						if (data.frontLeftWheelsCount > 1)
						{
							data.averageFrontLeftWheelsRadius /= data.frontLeftWheelsCount;
							data.averageFrontLeftWheelsHitPoint = Utility.Divide(data.averageFrontLeftWheelsHitPoint, data.frontLeftWheelsCount);
							data.averageFrontLeftWheelsSpeed /= data.frontLeftWheelsCount;
						}

						if (data.frontCenterWheelsCount > 1)
						{
							data.averageFrontCenterWheelsRadius /= data.frontCenterWheelsCount;
							data.averageFrontCenterWheelsHitPoint = Utility.Divide(data.averageFrontCenterWheelsHitPoint, data.frontCenterWheelsCount);
							data.averageFrontCenterWheelsSpeed /= data.frontCenterWheelsCount;
						}

						if (data.frontRightWheelsCount > 1)
						{
							data.averageFrontRightWheelsRadius /= data.frontRightWheelsCount;
							data.averageFrontRightWheelsHitPoint = Utility.Divide(data.averageFrontRightWheelsHitPoint, data.frontRightWheelsCount);
							data.averageFrontRightWheelsSpeed /= data.frontRightWheelsCount;
						}
					}

					if (data.rearWheelsCount > 1)
					{
						data.averageRearWheelsForwardFrictionStiffness /= data.rearWheelsCount;
						data.averageRearWheelsSidewaysFrictionStiffness /= data.rearWheelsCount;
						data.averageRearWheelsForwardExtremumSlip /= data.rearWheelsCount;
						data.averageRearWheelsForwardSlip /= data.rearWheelsCount;
						data.averageRearWheelsSidewaysExtremumSlip /= data.rearWheelsCount;
						data.averageRearWheelsSidewaysSlip /= data.rearWheelsCount;

						if (data.rearLeftWheelsCount > 1)
						{
							data.averageRearLeftWheelsRadius /= data.rearLeftWheelsCount;
							data.averageRearLeftWheelsHitPoint = Utility.Divide(data.averageRearLeftWheelsHitPoint, data.rearLeftWheelsCount);
							data.averageRearLeftWheelsSpeed /= data.rearLeftWheelsCount;
						}

						if (data.rearCenterWheelsCount > 1)
						{
							data.averageRearCenterWheelsRadius /= data.rearCenterWheelsCount;
							data.averageRearCenterWheelsHitPoint = Utility.Divide(data.averageRearCenterWheelsHitPoint, data.rearCenterWheelsCount);
							data.averageRearCenterWheelsSpeed /= data.rearCenterWheelsCount;
						}

						if (data.rearRightWheelsCount > 1)
						{
							data.averageRearRightWheelsRadius /= data.rearRightWheelsCount;
							data.averageRearRightWheelsHitPoint = Utility.Divide(data.averageRearRightWheelsHitPoint, data.rearRightWheelsCount);
							data.averageRearRightWheelsSpeed /= data.rearRightWheelsCount;
						}
					}

					if (data.leftWheelsCount > 1)
					{
						data.averageLeftWheelsForwardExtremumSlip /= data.leftWheelsCount;
						data.averageLeftWheelsForwardSlip /= data.leftWheelsCount;
						data.averageLeftWheelsSidewaysExtremumSlip /= data.leftWheelsCount;
						data.averageLeftWheelsSidewaysSlip /= data.leftWheelsCount;
					}

					if (data.centerWheelsCount > 1)
					{
						data.averageCenterWheelsForwardExtremumSlip /= data.centerWheelsCount;
						data.averageCenterWheelsForwardSlip /= data.centerWheelsCount;
						data.averageCenterWheelsSidewaysExtremumSlip /= data.centerWheelsCount;
						data.averageCenterWheelsSidewaysSlip /= data.centerWheelsCount;
					}

					if (data.rightWheelsCount > 1)
					{
						data.averageRightWheelsForwardExtremumSlip /= data.rightWheelsCount;
						data.averageRightWheelsForwardSlip /= data.rightWheelsCount;
						data.averageRightWheelsSidewaysExtremumSlip /= data.rightWheelsCount;
						data.averageRightWheelsSidewaysSlip /= data.rightWheelsCount;
					}

					if (data.motorWheelsCount > 1)
					{
						if (data.motorLeftWheelsCount > 1)
						{
							data.averageMotorLeftWheelsForwardExtremumSlip /= data.motorLeftWheelsCount;
							data.averageMotorLeftWheelsForwardSlip /= data.motorLeftWheelsCount;
							data.averageLeftMotorWheelsSpeed /= data.motorLeftWheelsCount;
						}

						if (data.motorCenterWheelsCount > 1)
						{
							data.averageMotorCenterWheelsForwardExtremumSlip /= data.motorCenterWheelsCount;
							data.averageMotorCenterWheelsForwardSlip /= data.motorCenterWheelsCount;
							data.averageCenterMotorWheelsSpeed /= data.motorCenterWheelsCount;
						}

						if (data.motorRightWheelsCount > 1)
						{
							data.averageMotorRightWheelsForwardExtremumSlip /= data.motorRightWheelsCount;
							data.averageMotorRightWheelsForwardSlip /= data.motorRightWheelsCount;
							data.averageRightMotorWheelsSpeed /= data.motorRightWheelsCount;
						}

						data.averageMotorWheelsSidewaysSlip /= data.motorWheelsCount;
						data.averageMotorWheelsSpeed /= data.motorWheelsCount;
						data.averageMotorWheelsRPM /= data.motorWheelsCount;
						data.averageMotorWheelsRadius /= data.motorWheelsCount;
						data.averageMotorWheelsFrictionStiffnessSqr /= data.motorWheelsCount;
						data.wheelTorque /= data.motorWheelsCount;
					}

					if (data.nonMotorWheelsCount > 1)
						data.averageNonMotorWheelsSpeed /= data.nonMotorWheelsCount;

					if (data.nonSteerWheelsCount > 1)
						data.averageNonSteerWheelsSidewaysSlip /= data.nonSteerWheelsCount;

					if (data.steerWheelsCount > 1)
					{
						data.averageSteerWheelsForwardDirection /= data.steerWheelsCount;
						data.averageSteerWheelsSidewaysSlip /= data.steerWheelsCount;
					}

					data.averageWheelsForwardFriction /= wheelsCount;
					data.averageWheelsRadius /= wheelsCount;
					data.averageWheelsBrakeTorque /= wheelsCount;

					data.averageFrontLeftWheelsSpeedAbs = math.abs(data.averageFrontLeftWheelsSpeed);
					data.averageFrontCenterWheelsSpeedAbs = math.abs(data.averageFrontCenterWheelsSpeed);
					data.averageFrontRightWheelsSpeedAbs = math.abs(data.averageFrontRightWheelsSpeed);
					data.averageRearLeftWheelsSpeedAbs = math.abs(data.averageRearLeftWheelsSpeed);
					data.averageRearCenterWheelsSpeedAbs = math.abs(data.averageRearCenterWheelsSpeed);
					data.averageRearRightWheelsSpeedAbs = math.abs(data.averageRearRightWheelsSpeed);
					data.averageMotorWheelsSpeedAbs = math.abs(data.averageMotorWheelsSpeed);
					data.averageLeftMotorWheelsSpeedAbs = math.abs(data.averageLeftMotorWheelsSpeed);
					data.averageCenterMotorWheelsSpeedAbs = math.abs(data.averageCenterMotorWheelsSpeed);
					data.averageRightMotorWheelsSpeedAbs = math.abs(data.averageRightMotorWheelsSpeed);
					data.averageNonMotorWheelsSpeedAbs = math.abs(data.averageNonMotorWheelsSpeed);
					data.averageMotorWheelsRPMAbs = math.abs(data.averageMotorWheelsRPM);
					data.averageWheelsForwardFrictionStiffness = Utility.Average(data.averageFrontWheelsForwardFrictionStiffness, data.averageRearWheelsForwardFrictionStiffness);
					data.averageMotorWheelsForwardSlip = Utility.Average(data.averageMotorLeftWheelsForwardSlip, data.averageMotorRightWheelsForwardSlip);
					data.averageMotorWheelsSmoothSidewaysSlip = Utility.Lerp(data.averageMotorWheelsSmoothSidewaysSlip, data.averageMotorWheelsSidewaysSlip, 5f * deltaTime);
					data.averageFrontWheelsSmoothSidewaysSlip = Utility.Lerp(data.averageFrontWheelsSmoothSidewaysSlip, data.averageFrontWheelsSidewaysSlip, 5f * deltaTime);
					data.averageRearWheelsSmoothForwardSlip = Utility.Lerp(data.averageRearWheelsSmoothForwardSlip, data.averageRearWheelsForwardSlip, 5f * deltaTime);
					data.averageRearWheelsSmoothSidewaysSlip = Utility.Lerp(data.averageRearWheelsSmoothSidewaysSlip, data.averageRearWheelsSidewaysSlip, 5f * deltaTime);
					data.wheelPower = !data.isTrailer ? behaviourAccess.power * data.wheelTorque / behaviourAccess.torque : 0f;
					data.firstMinTargetSpeed = data.isTrailer ? 0f : transmissionAccess.minSpeedTargets[0];
					data.minTargetSpeed = data.isTrailer ? 0f : inputs.Direction >= 0 ? transmissionAccess.minSpeedTargets[data.currentGear] : transmissionAccess.reverseMinSpeedTarget;
					data.targetSpeed = data.isTrailer ? 0f : inputs.Direction < 0 ? transmissionAccess.reverseSpeedTarget : transmissionAccess.overrideGearShiftUpSpeeds[data.currentGear] ? transmissionAccess.gearShiftUpOverrideSpeeds[data.currentGear] : transmissionAccess.speedTargets[data.currentGear];
					data.isStationary = data.isNeutral && (data.isTrailer || data.engineRPM <= engineAccess.minimumRPM) && math.round(data.currentSpeed) < 1f;
					data.isOverRev = !data.isTrailer && !data.isElectric && data.isEngineStarting && data.averageMotorWheelsSpeedAbs > data.firstMinTargetSpeed || data.isEngineRunning && inputs.Direction != 0 && data.averageMotorWheelsSpeedAbs * (1f - inputs.Clutch) * engineAccess.redlineRPM / engineAccess.overRevRPM > data.targetSpeed;
					data.isUnderRev = !data.isTrailer && (data.isEngineRunning || data.isEngineStarting) && data.averageMotorWheelsSpeedAbs * (1f - inputs.Clutch) < data.minTargetSpeed && inputs.Direction != 0;
					data.brakingDistance = data.EvaluateBrakingDistance(wheelsCount, gravity, behaviourAccess.curbWeight);

					if (!data.isTrailer)
					{
						float inertia = behaviourAccess.power / engineAccess.mass * engineAccess.cylinderCount / 6f;

						#region RevLimiter

						Unity.Mathematics.Random random = new(randomSeed);

						if (behaviourAccess.useRevLimiter && (data.isLaunchControlActive && data.engineRPM > behaviourAccess.peakTorqueRPM + 100f * inputs.RawFuel || data.engineRPM > engineAccess.overRevRPM))
						{
							data.isRevLimiting = !data.isElectric || inputs.RawFuel >= engineAccess.overRevRPM / engineAccess.maximumRPM;
							data.isRevLimited = !data.isElectric && data.engineRPM >= engineAccess.overRevRPM;

							if (!data.isElectric)
								data.rawEngineRPM = data.isLaunchControlActive ? behaviourAccess.peakTorqueRPM - 100f : engineAccess.redlineRPM;

							if (behaviourAccess.useExhaustEffects && !data.isElectric && !data.isLaunchControlActive && random.NextFloat(0f, 1f) < behaviourAccess.exhaustFlameEmissionProbability)
								data.requestExhaustPop = true;
						}
						else if (data.isRevLimiting && (data.isElectric || data.engineRPM < behaviourAccess.peakTorqueRPM || inputs.RawFuel <= behaviourAccess.peakTorqueRPM / engineAccess.maximumRPM || !data.isLaunchControlActive && (data.engineRPM < math.max(engineAccess.redlineRPM, engineAccess.overRevRPM - Utility.LerpUnclamped(100f, 250f, Utility.InverseLerp(1f, 4f, inertia))) * inputs.RawFuel || inputs.RawFuel <= engineAccess.redlineRPM / engineAccess.maximumRPM)))
						{
							data.isRevLimiting = false;

							if (inputs.FuelPedalWasReleased && !data.isElectric && !data.isLaunchControlActive)
								data.requestExhaustGurgle = true;
						}
						else if (behaviourAccess.useExhaustEffects && data.isEngineRunning && !data.isElectric && data.isNeutral)
						{
							if (inputs.FuelPedalWasReleased)
								data.exhaustPopRPM = random.NextFloat(Utility.Lerp(engineAccess.minimumRPM, engineAccess.redlineRPM, .55f), Utility.Lerp(engineAccess.minimumRPM, engineAccess.redlineRPM, .85f));
							else if (data.engineRPM <= data.exhaustPopRPM)
							{
								if (random.NextFloat(0f, 1f) < behaviourAccess.exhaustFlameEmissionProbability - inputs.RawFuel)
									data.requestExhaustPop = true;

								data.exhaustPopRPM = 0f;
							}
						}

						#endregion

						#region Engine RPM

						data.rawClutchRPM = Mathf.MoveTowards(data.rawClutchRPM, inputs.Clutch, 6.8535260697760002010256092747357f * deltaTime);
						data.rawWheelsRPM = Mathf.MoveTowards(data.rawWheelsRPM, !data.isRevLimiting || data.isElectric ? Utility.InverseLerpUnclamped(!data.isEngineStarting && data.isEngineRunning && (data.useEngineStall || data.useGearLimiter) && data.isManualGearbox ? data.minTargetSpeed : 0f, data.targetSpeed, ((data.useEngineStall && !data.isElectric ? data.averageMotorWheelsSpeed * inputs.Direction : math.abs(data.averageMotorWheelsSpeed)) / transmissionAccess.efficiency) + Utility.Clamp01(inputs.RawFuel - inputs.Clutch) * math.abs(data.averageMotorWheelsSidewaysSlip)) : 1f, (data.engineRPM >= engineAccess.redlineRPM ? .5f : 1f) * 2f * deltaTime);

						float launchControlFactor = data.isElectric || !behaviourAccess.useRevLimiter ? Utility.InverseLerp(engineAccess.minimumRPM, engineAccess.redlineRPM, behaviourAccess.peakTorqueRPM) : 1f;
						float overRevFactor = Utility.InverseLerpUnclamped(engineAccess.minimumRPM, engineAccess.redlineRPM, engineAccess.overRevRPM);
						float maximumRPMFactor = Utility.InverseLerpUnclamped(engineAccess.minimumRPM, engineAccess.redlineRPM, engineAccess.maximumRPM);
						float maxInertiaFactor = Utility.BoolToNumber(data.engineRPM < engineAccess.redlineRPM || data.isElectric) + (1f - inputs.Clutch);
						float flywheelInertia = math.clamp(inertia, 1f, 4f * Utility.Lerp(.5f, 1f, maxInertiaFactor));
						float engineInertia = Utility.Lerp(1f, flywheelInertia, inputs.RawFuel + (data.engineRPM / engineAccess.redlineRPM));
						float clutchInertia = data.isChangingGear ? .5f * (.15f / (transmissionAccess.shiftDelay + Utility.Average(transmissionAccess.clutchInDelay, transmissionAccess.clutchOutDelay))) : Utility.Lerp(.25f, 1f, inputs.RawFuel);
						float rawFuelRPMDelta = engineInertia * clutchInertia * deltaTime;

						data.rawFuelRPM = Mathf.MoveTowards(data.rawFuelRPM, data.isChangingGearUp ? 0f : data.isRevLimiting ? (!data.isElectric ? 0f : data.isLaunchControlActive ? launchControlFactor : overRevFactor) : Utility.Lerp(data.rawWheelsRPM, inputs.RawFuel * (data.isLaunchControlActive ? launchControlFactor : maximumRPMFactor), inputs.Clutch), rawFuelRPMDelta);
						data.rawStarterRPM = data.isElectric || data.isEngineRunning && !data.isEngineStarting ? 0f : Utility.Lerp(data.rawStarterRPM, data.isEngineStarting ? Utility.Clamp01((engineAccess.minimumRPM / engineAccess.redlineRPM) - Utility.Lerp(data.rawWheelsRPM + inputs.Brake, data.rawFuelRPM, inputs.Clutch)) : 0f, 6.3600314464631383613276479748295f * deltaTime);
						data.rawEngineRPM = Utility.LerpUnclamped(!data.isEngineStarting && data.isEngineRunning ? engineAccess.minimumRPM : 0f, engineAccess.redlineRPM, Utility.Lerp(data.rawWheelsRPM, data.rawFuelRPM, data.rawClutchRPM) + data.rawStarterRPM);
						data.engineRPMInertia = Utility.Lerp(25f, 50f, maxInertiaFactor * Utility.InverseLerpUnclamped(1f, 4f, inertia));
						data.engineRPM = Utility.Lerp(data.engineRPM, math.clamp(data.rawEngineRPM, data.isEngineRunning && !data.isEngineStarting && !data.useEngineStall ? engineAccess.minimumRPM * (data.isManualTransmission && data.useGearLimiter ? .5f : 1f) : 0f, Utility.Average(engineAccess.overRevRPM, engineAccess.maximumRPM)), deltaTime * data.engineRPMInertia);

						if (data.useEngineStall && data.isManualGearbox && !data.isElectric && data.isEngineRunning && !data.isEngineStarting && !data.isNeutral && inputs.Clutch < 1f)
							if (Utility.Lerp(data.averageMotorWheelsSpeed * inputs.Direction / data.minTargetSpeed, data.engineRPM / engineAccess.minimumRPM, inputs.Clutch) < .5f)
								data.requestEngineStall = true;

						#endregion
					}

					stats[0] = data;
				}

				#endregion
			}

			#endregion

			#region Variables

			[NonSerialized]
			public float3 position;
			[NonSerialized]
			public quaternion rotation;
			[NonSerialized]
			public float3 angularVelocity;
			[NonSerialized]
			public float3 velocity;
			[NonSerialized]
			public float3 localVelocity;
			[NonSerialized]
			public float3 localAngularVelocity;
			[NonSerialized]
			public float3 averageSteerWheelsForwardDirection;
			[NonSerialized]
			public float3 averageFrontLeftWheelsHitPoint;
			[NonSerialized]
			public float3 averageFrontCenterWheelsHitPoint;
			[NonSerialized]
			public float3 averageFrontRightWheelsHitPoint;
			[NonSerialized]
			public float3 averageRearLeftWheelsHitPoint;
			[NonSerialized]
			public float3 averageRearCenterWheelsHitPoint;
			[NonSerialized]
			public float3 averageRearRightWheelsHitPoint;
			[NonSerialized]
			public int groundedWheelsCount;
			[NonSerialized]
			public int groundedFrontWheelsCount;
			[NonSerialized]
			public int groundedFrontLeftWheelsCount;
			[NonSerialized]
			public int groundedFrontCenterWheelsCount;
			[NonSerialized]
			public int groundedFrontRightWheelsCount;
			[NonSerialized]
			public int groundedRearWheelsCount;
			[NonSerialized]
			public int groundedRearLeftWheelsCount;
			[NonSerialized]
			public int groundedRearCenterWheelsCount;
			[NonSerialized]
			public int groundedRearRightWheelsCount;
			[NonSerialized]
			public int offRoadingWheelsCount;
			[NonSerialized]
			public int offRoadingMotorWheelsCount;
			[NonSerialized]
			public int offRoadingFrontWheelsCount;
			[NonSerialized]
			public int offRoadingFrontLeftWheelsCount;
			[NonSerialized]
			public int offRoadingFrontCenterWheelsCount;
			[NonSerialized]
			public int offRoadingFrontRightWheelsCount;
			[NonSerialized]
			public int offRoadingRearWheelsCount;
			[NonSerialized]
			public int offRoadingRearLeftWheelsCount;
			[NonSerialized]
			public int offRoadingRearCenterWheelsCount;
			[NonSerialized]
			public int offRoadingRearRightWheelsCount;
			[NonSerialized]
			public float averageFrontWheelsForwardFrictionStiffness;
			[NonSerialized]
			public float averageRearWheelsForwardFrictionStiffness;
			[NonSerialized]
			public float averageWheelsForwardFrictionStiffness;
			[NonSerialized]
			public float averageFrontWheelsSidewaysFrictionStiffness;
			[NonSerialized]
			public float averageRearWheelsSidewaysFrictionStiffness;
			[NonSerialized]
			public float averageWheelsForwardFriction;
			[NonSerialized]
			public float averageWheelsSidewaysFrictionStiffness;
			[NonSerialized]
			public float averageFrontWheelsForwardExtremumSlip;
			[NonSerialized]
			public float averageFrontWheelsForwardSlip;
			[NonSerialized]
			public float averageFrontWheelsSidewaysExtremumSlip;
			[NonSerialized]
			public float averageFrontWheelsSidewaysSlip;
			[NonSerialized]
			public float averageRearWheelsForwardExtremumSlip;
			[NonSerialized]
			public float averageRearWheelsForwardSlip;
			[NonSerialized]
			public float averageRearWheelsSidewaysExtremumSlip;
			[NonSerialized]
			public float averageRearWheelsSidewaysSlip;
			[NonSerialized]
			public float averageLeftWheelsForwardExtremumSlip;
			[NonSerialized]
			public float averageLeftWheelsForwardSlip;
			[NonSerialized]
			public float averageLeftWheelsSidewaysExtremumSlip;
			[NonSerialized]
			public float averageLeftWheelsSidewaysSlip;
			[NonSerialized]
			public float averageCenterWheelsForwardExtremumSlip;
			[NonSerialized]
			public float averageCenterWheelsForwardSlip;
			[NonSerialized]
			public float averageCenterWheelsSidewaysExtremumSlip;
			[NonSerialized]
			public float averageCenterWheelsSidewaysSlip;
			[NonSerialized]
			public float averageRightWheelsForwardExtremumSlip;
			[NonSerialized]
			public float averageRightWheelsForwardSlip;
			[NonSerialized]
			public float averageRightWheelsSidewaysExtremumSlip;
			[NonSerialized]
			public float averageRightWheelsSidewaysSlip;
			[NonSerialized]
			public float averageMotorWheelsSmoothSidewaysSlip;
			[NonSerialized]
			public float averageFrontWheelsSmoothSidewaysSlip;
			[NonSerialized]
			public float averageRearWheelsSmoothForwardSlip;
			[NonSerialized]
			public float averageRearWheelsSmoothSidewaysSlip;
			[NonSerialized]
			public float averageSteerWheelsSidewaysSlip;
			[NonSerialized]
			public float averageNonSteerWheelsSidewaysSlip;
			[NonSerialized]
			public float averageMotorWheelsForwardSlip;
			[NonSerialized]
			public float averageMotorLeftWheelsForwardExtremumSlip;
			[NonSerialized]
			public float averageMotorLeftWheelsForwardSlip;
			[NonSerialized]
			public float averageMotorCenterWheelsForwardExtremumSlip;
			[NonSerialized]
			public float averageMotorCenterWheelsForwardSlip;
			[NonSerialized]
			public float averageMotorRightWheelsForwardExtremumSlip;
			[NonSerialized]
			public float averageMotorRightWheelsForwardSlip;
			[NonSerialized]
			public float averageMotorWheelsSidewaysSlip;
			[NonSerialized]
			public float averageFrontLeftWheelsSpeed;
			[NonSerialized]
			public float averageFrontCenterWheelsSpeed;
			[NonSerialized]
			public float averageFrontRightWheelsSpeed;
			[NonSerialized]
			public float averageRearLeftWheelsSpeed;
			[NonSerialized]
			public float averageRearCenterWheelsSpeed;
			[NonSerialized]
			public float averageRearRightWheelsSpeed;
			[NonSerialized]
			public float averageMotorWheelsSpeed;
			[NonSerialized]
			public float averageLeftMotorWheelsSpeed;
			[NonSerialized]
			public float averageCenterMotorWheelsSpeed;
			[NonSerialized]
			public float averageRightMotorWheelsSpeed;
			[NonSerialized]
			public float averageNonMotorWheelsSpeed;
			[NonSerialized]
			public float averageMotorWheelsRPM;
			[NonSerialized]
			public float averageFrontLeftWheelsSpeedAbs;
			[NonSerialized]
			public float averageFrontCenterWheelsSpeedAbs;
			[NonSerialized]
			public float averageFrontRightWheelsSpeedAbs;
			[NonSerialized]
			public float averageRearLeftWheelsSpeedAbs;
			[NonSerialized]
			public float averageRearCenterWheelsSpeedAbs;
			[NonSerialized]
			public float averageRearRightWheelsSpeedAbs;
			[NonSerialized]
			public float averageMotorWheelsSpeedAbs;
			[NonSerialized]
			public float averageLeftMotorWheelsSpeedAbs;
			[NonSerialized]
			public float averageCenterMotorWheelsSpeedAbs;
			[NonSerialized]
			public float averageRightMotorWheelsSpeedAbs;
			[NonSerialized]
			public float averageNonMotorWheelsSpeedAbs;
			[NonSerialized]
			public float averageMotorWheelsRPMAbs;
			[NonSerialized]
			public float averageMotorWheelsRadius;
			[NonSerialized]
			public float averageFrontLeftWheelsRadius;
			[NonSerialized]
			public float averageFrontCenterWheelsRadius;
			[NonSerialized]
			public float averageFrontRightWheelsRadius;
			[NonSerialized]
			public float averageRearLeftWheelsRadius;
			[NonSerialized]
			public float averageRearCenterWheelsRadius;
			[NonSerialized]
			public float averageRearRightWheelsRadius;
			[NonSerialized]
			public float averageWheelsRadius;
			[NonSerialized]
			public float averageWheelsBrakeTorque;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isNeutral;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isEngineRunning;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isEngineStarting;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isEngineStall;
			[NonSerialized]
			public float currentGearSpeedTarget;
			[NonSerialized]
			public float currentGearMinSpeedTarget;
			[NonSerialized]
			public float currentGearRatio;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isReversing;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isChangingGear;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isChangingGearUp;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isChangingGearDown;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isNOSActive;
			[NonSerialized]
			public int currentGear;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isABSActive;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isESPActive;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isTCSActive;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isOverSteering;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isUnderSteering;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isLaunchControlActive;
			[NonSerialized]
			public float steerAngle;
			[NonSerialized]
			public float rawSteerAngle;
			[NonSerialized]
			public float maximumSteerAngle;
			[NonSerialized]
			public float counterSteerHelper;
			[NonSerialized]
			public float NOS;
			[NonSerialized]
			public float bottleNOS;
			[NonSerialized]
			public float NOSBoost;
			[NonSerialized]
			public float engineBoost;
			[NonSerialized]
			public float fuelConsumption;
			[NonSerialized]
			public float fuelTank;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isLightsOn;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isHighBeamHeadlightsOn;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isInteriorLightsOn;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isSignalLeftLightsOn;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isSignalRightLightsOn;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isHazardLightsOn;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool inDarknessWeatherZone;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool inFogWeatherZone;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isRevLimiting;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isStationary;
			[Obsolete("No longer supported", true)]
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isDrifting;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isOverRev;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isUnderRev;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isGrounded;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isFullyGrounded;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isOffRoading;
			[MarshalAs(UnmanagedType.U1)]
			[NonSerialized]
			public bool isFullyOffRoading;
			[NonSerialized]
			public int groundIndex;
			[NonSerialized]
			public int clampedGroundIndex;
			[NonSerialized]
			public float brakingDistance;
			[NonSerialized]
			public float engineTorque;
			[NonSerialized]
			public float rawEngineTorque;
			[NonSerialized]
			public float enginePower;
			[NonSerialized]
			public float rawEnginePower;
			[NonSerialized]
			public float engineBrakeTorque;
			[NonSerialized]
			public float wheelTorque;
			[NonSerialized]
			public float wheelPower;
			[NonSerialized]
			public float engineRPM;
			[NonSerialized]
			public float currentSpeed;
			[NonSerialized]
			public float lastAirTime;

			internal float averageMotorWheelsFrictionStiffnessSqr;
			internal int groundedSteerWheelsCount;
			internal int groundedMotorWheelsCount;
			internal int groundedNonSteerWheelsCount;
			internal float firstMinTargetSpeed;
			internal float minTargetSpeed;
			internal float targetSpeed;
			internal float rawFuelRPM;
			internal float rawStarterRPM;
			internal float rawWheelsRPM;
			internal float rawClutchRPM;
			internal float rawEngineRPM;
			internal float engineRPMInertia;
			[MarshalAs(UnmanagedType.U1)]
			internal bool isRevLimited;

			private readonly int steerWheelsCount;
			private readonly int nonSteerWheelsCount;
			private readonly int motorWheelsCount;
			private readonly int motorLeftWheelsCount;
			private readonly int motorCenterWheelsCount;
			private readonly int motorRightWheelsCount;
			private readonly int nonMotorWheelsCount;
			private readonly int frontWheelsCount;
			private readonly int rearWheelsCount;
			private readonly int leftWheelsCount;
			private readonly int centerWheelsCount;
			private readonly int rightWheelsCount;
			private readonly int frontLeftWheelsCount;
			private readonly int frontCenterWheelsCount;
			private readonly int frontRightWheelsCount;
			private readonly int rearLeftWheelsCount;
			private readonly int rearCenterWheelsCount;
			private readonly int rearRightWheelsCount;
			[MarshalAs(UnmanagedType.U1)]
			private readonly bool isElectric;
			[MarshalAs(UnmanagedType.U1)]
			private readonly bool isTrailer;
			[MarshalAs(UnmanagedType.U1)]
			private readonly bool isManualGearbox;
			[MarshalAs(UnmanagedType.U1)]
			private readonly bool isManualTransmission;
			private float exhaustPopRPM;
			private float handlingRate;
			[MarshalAs(UnmanagedType.U1)]
			private bool oldIsFullyGrounded;
			[MarshalAs(UnmanagedType.U1)]
			private bool useEngineStall;
			[MarshalAs(UnmanagedType.U1)]
			private bool useGearLimiter;
			[MarshalAs(UnmanagedType.U1)]
			private bool requestEngineStall;
			[MarshalAs(UnmanagedType.U1)]
			private bool requestExhaustGurgle;
			[MarshalAs(UnmanagedType.U1)]
			private bool requestExhaustPop;

			#endregion

			#region Methods

			public static float EvaluateBrakingDistance(Vehicle vehicle, float targetVelocity)
			{
				return EvaluateBrakingDistance(vehicle, math.length(vehicle.stats.velocity), targetVelocity);
			}
			public static float EvaluateBrakingDistance(Vehicle vehicle, float velocity, float targetVelocity)
			{
				return EvaluateBrakingDistance(vehicle, velocity, targetVelocity, vehicle.stats.averageWheelsBrakeTorque);
			}
			public static float EvaluateBrakingDistance(Vehicle vehicle, float velocity, float targetVelocity, float brakeTorque)
			{
				return EvaluateBrakingDistance(vehicle, velocity, targetVelocity, brakeTorque, Physics.gravity.magnitude);
			}
			public static float EvaluateBrakingDistance(Vehicle vehicle, float velocity, float targetVelocity, float brakeTorque, float gravity)
			{
				vehicle.stats.EvaluateGroundedWheelsFriction(vehicle.wheels.Length, out float wheelsFriction);
				vehicle.stats.EvaluateBrakeTorqueFriction(brakeTorque, vehicle.behaviour.CurbWeight, out float brakeTorqueFriction);

				return Utility.BrakingDistance(velocity, targetVelocity, math.min(wheelsFriction, brakeTorqueFriction), gravity);
			}
			public static float EvaluateGroundedWheelsFriction(Vehicle vehicle)
			{
				vehicle.stats.EvaluateGroundedWheelsFriction(vehicle.wheels.Length, out float friction);

				return friction;
			}
			public static float EvaluateGroundedWheelsFrictionStiffness(Vehicle vehicle)
			{
				vehicle.stats.EvaluateGroundedWheelsFrictionStiffness(vehicle.wheels.Length, out float friction);

				return friction;
			}
			public static float EvaluateBrakeTorqueFriction(Vehicle vehicle, float brakeTorque)
			{
				vehicle.stats.EvaluateBrakeTorqueFriction(brakeTorque, vehicle.behaviour.CurbWeight, out float friction);

				return friction;
			}

			internal readonly void SetEngineBrakeTorque(Vehicle vehicle, float value)
			{
				vehicle.stats.engineBrakeTorque = value;
			}
			internal readonly void ResetVehicle(Vehicle vehicle)
			{
				vehicle.stats.rawClutchRPM = 1f;
				vehicle.stats.rawFuelRPM = 0f;
				vehicle.stats.rawEngineRPM = vehicle.engineAccess.minimumRPM;
				vehicle.stats.rawStarterRPM = 0f;
				vehicle.stats.rawWheelsRPM = 0f;
				vehicle.stats.engineRPM = vehicle.engineAccess.minimumRPM;
				vehicle.stats.averageMotorWheelsRPM = 0f;
				vehicle.stats.averageMotorWheelsSpeed = 0f;
			}
			internal void FixedUpdate(Vehicle vehicle)
			{
				if (handlingRate != vehicle.stability.HandlingRate)
				{
					handlingRate = vehicle.stability.HandlingRate;

					int subSteps = (int)math.round(Utility.Lerp(Settings.physicsSubSteps.Min, Settings.physicsSubSteps.Max, handlingRate));

					if (ToolkitSettings.UsingWheelColliderPhysics)
						vehicle.Wheels[0].Instance.wheelCollider.ConfigureVehicleSubsteps(30f, subSteps, subSteps);
				}

				if (vehicle.engineAccess.maximumRPM != vehicle.engine.MaximumRPM)
					vehicle.RefreshGearShiftRPMFactor();

				float3 linearVelocity = vehicle.rigidbody.
#if UNITY_6000_0_OR_NEWER
					linearVelocity;
#else
					velocity;
#endif

				if (math.abs(engineTorque) > 1f && math.length(linearVelocity) < 1f)
					vehicle.rigidbody.WakeUp();

				vehicle.wheelStats = new(vehicle.Wheels.Select(wheel => (VehicleWheel.WheelStatsAccess)wheel).ToArray(), Allocator.TempJob);
#if !MVC_COMMUNITY
				vehicle.groundsAppearance = new(vehicle.grounds.Length, Allocator.TempJob);
#endif
				vehicle.engineAccess = vehicle.Engine;
				position = vehicle.rigidbody.position;
				rotation = vehicle.rigidbody.rotation;
				velocity = linearVelocity;
				angularVelocity = vehicle.rigidbody.angularVelocity;
				useEngineStall = Settings.useEngineStalling;
				useGearLimiter = Settings.useGearLimiterOnReverseAndFirstGear;

				NativeArray<StatsAccess> stats = new(1, Allocator.TempJob);

				stats[0] = this;

				FixedUpdateJob job = new()
				{
					stats = stats,
					grounds = vehicle.grounds,
					wheelsAccess = vehicle.wheelsAccess,
					wheelStats = vehicle.wheelStats,
#if !MVC_COMMUNITY
					groundsAppearance = vehicle.groundsAppearance,
#endif
					engineAccess = vehicle.engineAccess,
					behaviourAccess = vehicle.behaviourAccess,
					transmissionAccess = vehicle.transmissionAccess,
					inputs = vehicle.inputs,
					deltaTime = Time.fixedDeltaTime,
					gravity = math.length(Physics.gravity),
					randomSeed = (uint)UnityEngine.Random.Range(0, int.MaxValue)
				};

				job.Schedule().Complete();

				this = stats[0];

				if (isTrailer)
				{
					if (vehicle.trailerInstance.Joint.ConnectedVehicle)
					{
						isEngineRunning = vehicle.trailerInstance.Joint.ConnectedVehicle.stats.isEngineRunning;
						isEngineStarting = vehicle.trailerInstance.Joint.ConnectedVehicle.stats.isEngineStarting;
						isEngineStall = vehicle.trailerInstance.Joint.ConnectedVehicle.stats.isEngineStall;
						isChangingGear = vehicle.trailerInstance.Joint.ConnectedVehicle.stats.isChangingGear;
						isChangingGearDown = vehicle.trailerInstance.Joint.ConnectedVehicle.stats.isChangingGearDown;
						isChangingGearUp = vehicle.trailerInstance.Joint.ConnectedVehicle.stats.isChangingGearUp;
						isNeutral = vehicle.trailerInstance.Joint.ConnectedVehicle.stats.isNeutral;
						currentGear = vehicle.trailerInstance.Joint.ConnectedVehicle.stats.currentGear;
						isReversing = vehicle.trailerInstance.Joint.ConnectedVehicle.stats.isReversing;
						isOverRev = vehicle.trailerInstance.Joint.ConnectedVehicle.stats.isOverRev;
						isRevLimited = vehicle.trailerInstance.Joint.ConnectedVehicle.stats.isRevLimited;
						isRevLimiting = vehicle.trailerInstance.Joint.ConnectedVehicle.stats.isRevLimiting;
						isUnderRev = vehicle.trailerInstance.Joint.ConnectedVehicle.stats.isUnderRev;
					}
				}
				else
				{
					if (requestEngineStall)
					{
						vehicle.StallEngine();

						requestEngineStall = false;
					}

					if (requestExhaustGurgle)
					{
						if (!requestExhaustPop)
							vehicle.sfx.ExhaustGurgle();

						requestExhaustGurgle = false;
					}

					if (requestExhaustPop)
					{
						vehicle.sfx.ExhaustPop();
						vehicle.vfx.ShootExhaustFlames();

						requestExhaustPop = false;
					}
				}

				vehicle.stats = this;

#if !MVC_COMMUNITY
				vehicle.groundsAppearance.Dispose();
#endif
				vehicle.wheelStats.Dispose();
				stats.Dispose();
			}

			private readonly float EvaluateBrakingDistance(int wheelsCount, float gravity, float mass)
			{
				return EvaluateBrakingDistance(wheelsCount, averageWheelsBrakeTorque, gravity, mass);
			}
			private readonly float EvaluateBrakingDistance(int wheelsCount, float brakeTorque, float gravity, float mass)
			{
				EvaluateGroundedWheelsFriction(wheelsCount, out float wheelsFriction);
				EvaluateBrakeTorqueFriction(mass, brakeTorque, out float brakeTorqueFriction);

				return Utility.BrakingDistance(currentSpeed / 3.6f, math.min(wheelsFriction, brakeTorqueFriction), gravity);
			}
			private readonly void EvaluateGroundedWheelsFriction(int wheelsCount, out float friction)
			{
				float groundedWheelsMultiplier = math.max(groundedWheelsCount, 1) / wheelsCount;
				float wheelsFriction = averageWheelsForwardFriction;

				friction = wheelsFriction * groundedWheelsMultiplier;
			}
			private readonly void EvaluateGroundedWheelsFrictionStiffness(int wheelsCount, out float friction)
			{
				float groundedWheelsMultiplier = math.max(groundedWheelsCount, 1) / wheelsCount;
				float wheelsFriction = averageWheelsForwardFrictionStiffness;

				friction = wheelsFriction * groundedWheelsMultiplier;
			}
			private readonly void EvaluateBrakeTorqueFriction(float brakeTorque, float mass, out float friction)
			{
				friction = brakeTorque / (averageWheelsRadius * mass);
			}

			#endregion

			#region Constructors

			public StatsAccess(Vehicle vehicle) : this()
			{
				steerWheelsCount = vehicle.steerWheels.Length;
				nonSteerWheelsCount = vehicle.nonSteerWheels.Length;
				motorWheelsCount = vehicle.motorWheels.Length;
				motorLeftWheelsCount = vehicle.motorLeftWheels.Length;
				motorCenterWheelsCount = vehicle.motorCenterWheels.Length;
				motorRightWheelsCount = vehicle.motorRightWheels.Length;
				nonMotorWheelsCount = vehicle.nonMotorWheels.Length;
				frontWheelsCount = vehicle.frontWheels.Length;
				rearWheelsCount = vehicle.rearWheels.Length;
				leftWheelsCount = vehicle.leftWheels.Length;
				centerWheelsCount = vehicle.centerWheels.Length;
				rightWheelsCount = vehicle.rightWheels.Length;
				frontLeftWheelsCount = vehicle.frontLeftWheels.Length;
				frontCenterWheelsCount = vehicle.frontCenterWheels.Length;
				frontRightWheelsCount = vehicle.frontRightWheels.Length;
				rearLeftWheelsCount = vehicle.rearLeftWheels.Length;
				rearCenterWheelsCount = vehicle.rearCenterWheels.Length;
				rearRightWheelsCount = vehicle.rearRightWheels.Length;
				isElectric = vehicle.IsElectric;
				isTrailer = vehicle.IsTrailer;
				isManualGearbox = vehicle.transmissionAccess.gearbox == TransmissionModule.GearboxType.Manual;
				isManualTransmission = Settings.ManualTransmission;
			}

			#endregion
		}
#pragma warning restore IDE0064 // Make readonly fields writable

		internal readonly struct BehaviourAccess : IDisposable
		{
			#region Variables

			public readonly VehicleType vehicleType;
			public readonly CarClass carClass;
			public readonly HeavyTruckClass heavyTruckClass;
			public readonly float curbWeight;
			public readonly VehicleEngineAccess engine;
			public readonly int engineIndex;
			public readonly Aspiration aspiration;
			public readonly VehicleChargerAccess turbocharger;
			public readonly VehicleChargerAccess supercharger;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool isTurbocharged;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool isSupercharged;
			public readonly int turbochargerIndex;
			public readonly int superchargerIndex;
			public readonly float power;
			public readonly float powerOffset;
			public readonly float peakPowerRPM;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool overridePeakPowerRPM;
			public readonly float torque;
			public readonly float torqueOffset;
			public readonly float peakTorqueRPM;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool overridePeakTorqueRPM;
			public readonly float torqueOutputScale;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool autoCurves;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool linkPowerTorqueCurves;
			public readonly float topSpeed;
			public readonly float fuelCapacity;
			public readonly float fuelConsumptionCity;
			public readonly float fuelConsumptionHighway;
			public readonly float fuelConsumptionCombined;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useRegenerativeBrakes;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useRevLimiter;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useExhaustEffects;
			public readonly float exhaustFlameEmissionProbability;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useNOS;
			public readonly int NOSBottlesCount;
			public readonly float NOSCapacity;
			public readonly float NOSBoost;
			public readonly float NOSConsumption;
			public readonly float NOSRegenerateTime;
			public readonly BrakeAccess frontBrakes;
			public readonly BrakeAccess rearBrakes;

			#endregion

			#region Methods

			public void Dispose()
			{
				turbocharger.Dispose();
				supercharger.Dispose();
			}

			#endregion

			#region Constructors

			public BehaviourAccess(BehaviourModule module, Allocator allocator) : this()
			{
				if (!module)
					return;

				vehicleType = module.vehicleType;
				carClass = module.carClass;
				heavyTruckClass = module.heavyTruckClass;
				curbWeight = module.CurbWeight;
				engine = module.Engine;
				engineIndex = module.EngineIndex;
				aspiration = module.Aspiration;
				turbocharger = new(module.Turbocharger, allocator);
				supercharger = new(module.Supercharger, allocator);
				isTurbocharged = module.IsTurbocharged;
				isSupercharged = module.IsSupercharged;
				turbochargerIndex = module.TurbochargerIndex;
				superchargerIndex = module.SuperchargerIndex;
				power = module.Power;
				powerOffset = module.PowerOffset;
				peakPowerRPM = module.PeakPowerRPM;
				overridePeakPowerRPM = module.OverridePeakPowerRPM;
				torque = module.Torque;
				torqueOffset = module.TorqueOffset;
				peakTorqueRPM = module.PeakTorqueRPM;
				overridePeakTorqueRPM = module.OverridePeakTorqueRPM;
				torqueOutputScale = module.TorqueOutputScale;
				autoCurves = module.AutoCurves;
				linkPowerTorqueCurves = module.LinkPowerTorqueCurves;
				topSpeed = module.TopSpeed;
				fuelCapacity = module.FuelCapacity;
				fuelConsumptionCity = module.FuelConsumptionCity;
				fuelConsumptionHighway = module.FuelConsumptionHighway;
				fuelConsumptionCombined = module.FuelConsumptionCombined;
				useRegenerativeBrakes = module.UseRegenerativeBrakes;
				useExhaustEffects = module.useExhaustEffects;
				useRevLimiter = module.useRevLimiter;
				exhaustFlameEmissionProbability = module.ExhaustFlameEmissionProbability;
				useNOS = module.useNOS;
				NOSBottlesCount = module.NOSBottlesCount;
				NOSCapacity = module.NOSCapacity;
				NOSBoost = module.NOSBoost;
				NOSConsumption = module.NOSConsumption;
				NOSRegenerateTime = module.NOSRegenerateTime;
				frontBrakes = module.FrontBrakes;
				rearBrakes = module.RearBrakes;
			}

			#endregion

			#region Operators

			public static implicit operator BehaviourAccess(BehaviourModule module) => new(module, Allocator.Persistent);

			#endregion
		}
		internal readonly struct BrakeAccess
		{
			#region Variables

			public readonly BrakeModule.BrakeType type;
			public readonly float density;
			public readonly float diameter;
			public readonly int pistons;
			public readonly int pads;
			public readonly float pressure;
			public readonly float friction;
			public readonly float brakeTorque;
			public readonly float brakeHeatThreshold;

			#endregion

			#region Constructors

			public BrakeAccess(BrakeModule module) : this()
			{
				if (!module)
					return;

				type = module.Type;
				density = module.Density;
				diameter = module.Diameter;
				pistons = module.Pistons;
				pads = module.Pads;
				pressure = module.Pressure;
				friction = module.Friction;
				brakeTorque = module.BrakeTorque;
				brakeHeatThreshold = module.BrakeHeatThreshold;
			}

			#endregion

			#region Operators

			public static implicit operator BrakeAccess(BrakeModule module) => new(module);

			#endregion
		}
		internal readonly struct TransmissionAccess : IDisposable
		{
			#region Variables

			public readonly TransmissionModule.GearboxType gearbox;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useDoubleGearbox;
			public readonly int clipsGroupIndex;
			public readonly int playClipsGroupStartingFromGear;
			public readonly float maximumTorque;
			public readonly float efficiency;
			public readonly float clutchInDelay;
			public readonly float clutchOutDelay;
			public readonly float shiftDelay;
			public readonly int gearsCount;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool autoGearRatios;
			public readonly float reverseSpeedTarget;
			public readonly float reverseMinSpeedTarget;
			public readonly float reverseGearRatio;
			public readonly float reverseGearRatio2;
			public readonly NativeArray<float> speedTargets;
			public readonly NativeArray<float> minSpeedTargets;
			public readonly NativeArray<float> gearRatios;
			public readonly NativeArray<float> gearRatios2;
			public readonly NativeArray<float> gearShiftUpOverrideSpeeds;
			public readonly NativeArray<bool> overrideGearShiftUpSpeeds;
			public readonly float finalGearRatio;
			public readonly VehicleDifferentialAccess frontDifferentialAccess;
			public readonly VehicleDifferentialAccess centerDifferentialAccess;
			public readonly VehicleDifferentialAccess rearDifferentialAccess;

			#endregion

			#region Methods

			public void Dispose()
			{
				if (speedTargets.IsCreated)
					speedTargets.Dispose();

				if (minSpeedTargets.IsCreated)
					minSpeedTargets.Dispose();

				if (gearRatios.IsCreated)
					gearRatios.Dispose();

				if (gearRatios2.IsCreated)
					gearRatios2.Dispose();

				if (gearShiftUpOverrideSpeeds.IsCreated)
					gearShiftUpOverrideSpeeds.Dispose();

				if (overrideGearShiftUpSpeeds.IsCreated)
					overrideGearShiftUpSpeeds.Dispose();
			}

			#endregion

			#region Constructors

			public TransmissionAccess(TransmissionModule module, Allocator allocator) : this()
			{
				if (!module)
				{
					speedTargets = new(0, allocator);
					minSpeedTargets = new(0, allocator);
					gearRatios = new(0, allocator);
					gearRatios2 = new(0, allocator);
					gearShiftUpOverrideSpeeds = new(0, allocator);
					overrideGearShiftUpSpeeds = new(0, allocator);

					return;
				}

				gearbox = module.Gearbox;
				useDoubleGearbox = module.UseDoubleGearbox;
				clipsGroupIndex = module.ClipsGroupIndex;
				playClipsGroupStartingFromGear = module.PlayClipsStartingFromGear;
				maximumTorque = module.MaximumTorque;
				efficiency = module.Efficiency;
				clutchInDelay = module.ClutchInDelay;
				clutchOutDelay = module.ClutchOutDelay;
				shiftDelay = module.ShiftDelay;
				gearsCount = module.GearsCount;
				autoGearRatios = module.AutoGearRatios;
				reverseSpeedTarget = module.GetSpeedTarget(-1, 0);
				reverseMinSpeedTarget = module.GetMinSpeedTarget(-1, 0);
				reverseGearRatio = module.GetGearRatio(-1, 0, false, false);
				reverseGearRatio2 = module.GetGearRatio(-1, 0, false, true);
				speedTargets = new(gearsCount, allocator);
				minSpeedTargets = new(gearsCount, allocator);
				gearRatios = new(gearsCount, allocator);
				gearRatios2 = new(gearsCount, allocator);
				gearShiftUpOverrideSpeeds = new(gearsCount, allocator);
				overrideGearShiftUpSpeeds = new(gearsCount, allocator);
				finalGearRatio = module.FinalGearRatio;
				frontDifferentialAccess = module.FrontDifferential;
				centerDifferentialAccess = module.CenterDifferential;
				rearDifferentialAccess = module.RearDifferential;

				for (int i = 0; i < gearsCount; i++)
				{
					speedTargets[i] = module.GetSpeedTarget(1, i);
					minSpeedTargets[i] = module.GetMinSpeedTarget(1, i);
					gearRatios[i] = module.GetGearRatio(1, i, false, false);
					gearRatios2[i] = module.GetGearRatio(1, i, false, true);
					overrideGearShiftUpSpeeds[i] = module.GetGearShiftOverrideSpeed(i, out float gearShiftUpOverrideSpeed);
					gearShiftUpOverrideSpeeds[i] = gearShiftUpOverrideSpeed;
				}
			}

			#endregion

			#region Operators

			public static implicit operator TransmissionAccess(TransmissionModule module) => new(module, Allocator.Persistent);

			#endregion
		}
		internal readonly struct SteeringAccess
		{
			#region Variables

			public readonly SteeringModule.SteerMethod method;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool overrideMethod;
			public readonly float maximumSteerAngle;
			public readonly float minimumSteerAngle;
			public readonly float lowSteerAngleSpeed;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useDynamicSteering;
			public readonly float dynamicSteeringIntensity;

			#endregion

			#region Constructors

			public SteeringAccess(SteeringModule module)
			{
				method = module.Method;
				overrideMethod = module.overrideMethod;
				maximumSteerAngle = module.MaximumSteerAngle;
				minimumSteerAngle = module.MinimumSteerAngle;
				lowSteerAngleSpeed = module.LowSteerAngleSpeed;
				useDynamicSteering = module.UseDynamicSteering;
				dynamicSteeringIntensity = module.DynamicSteeringIntensity;
			}

			#endregion

			#region Operators

			public static implicit operator SteeringAccess(SteeringModule module) => new(module);

			#endregion
		}
		internal readonly struct StabilityAccess
		{
			#region Variables

			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useAntiSwayBars;
			public readonly float antiSwayFront;
			public readonly float antiSwayRear;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useABS;
			public readonly float ABSThreshold;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useESP;
			public readonly float ESPStrength;
			public readonly float ESPSpeedThreshold;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool ESPAllowDonuts;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useTCS;
			public readonly float TCSThreshold;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool TCSAllowBurnouts;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useSteeringHelper;
			public readonly float steerHelperLinearVelocityStrength;
			public readonly float steerHelperAngularVelocityStrength;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useCounterSteer;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useLaunchControl;
			public readonly float handlingRate;
			//public readonly float offRoadRate;
			public readonly float weightDistribution;
			public readonly float weightHeight;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useDownforce;
			public readonly float frontDownforce;
			public readonly float rearDownforce;
			public readonly float drag;
			public readonly float angularDrag;
			/*public readonly float dragCoefficient;
			public readonly float verticalDragCoefficient;
			public readonly float frontLateralDragCoefficient;
			public readonly float rearLateralDragCoefficient;
			public readonly float frontalArea;*/
			public readonly float dragScale;

			#endregion

			#region Constructors

			public StabilityAccess(StabilityModule module)
			{
				useAntiSwayBars = module.useAntiSwayBars;
				antiSwayFront = module.AntiSwayFront;
				antiSwayRear = module.AntiSwayRear;
				useABS = module.useABS;
				ABSThreshold = module.ABSThreshold;
				useESP = module.useESP;
				ESPStrength = module.ESPStrength;
				ESPAllowDonuts = module.ESPAllowDonuts;
				ESPSpeedThreshold = module.ESPSpeedThreshold;
				useTCS = module.useTCS;
				TCSThreshold = module.TCSThreshold;
				TCSAllowBurnouts = module.TCSAllowBurnouts;
				useSteeringHelper = module.useArcadeSteerHelpers;
				steerHelperLinearVelocityStrength = module.ArcadeLinearSteerHelperIntensity;
				steerHelperAngularVelocityStrength = module.ArcadeAngularSteerHelperIntensity;
				useCounterSteer = module.UseCounterSteer;
				useLaunchControl = module.UseLaunchControl;
				handlingRate = module.HandlingRate;
				//offRoadRate = module.OffRoadRate;
				weightDistribution = module.WeightDistribution;
				weightHeight = module.WeightHeight;
				useDownforce = module.useDownforce;
				frontDownforce = module.FrontDownforce;
				rearDownforce = module.RearDownforce;
				drag = module.Drag;
				angularDrag = module.AngularDrag;
				/*dragCoefficient = module.DragCoefficient;
				verticalDragCoefficient = module.VerticalDragCoefficient;
				frontLateralDragCoefficient = module.FrontLateralDragCoefficient;
				rearLateralDragCoefficient = module.RearLateralDragCoefficient;
				frontalArea = module.FrontalArea;*/
				dragScale = module.DragScale;
			}

			#endregion

			#region Operators

			public static implicit operator StabilityAccess(StabilityModule module) => new(module);

			#endregion
		}
		internal class SFXSheet
		{
			#region Modules

			#region Jobs

#if !MVC_COMMUNITY
			[BurstCompile]
#endif
			private struct UpdateEngineJob : IJob
			{
				#region Variables

				public NativeArray<VehicleEngine.AudioModule.CelerationSourceAccess> engineAccelerationSourcesAccess;
				public NativeArray<VehicleEngine.AudioModule.CelerationSourceAccess> engineDecelerationSourcesAccess;
				public NativeArray<VehicleEngine.AudioModule.CelerationSourceAccess> exhaustsAccelerationSourcesAccess;
				public NativeArray<VehicleEngine.AudioModule.CelerationSourceAccess> exhaustsDecelerationSourcesAccess;
				public NativeQueue<float> previousRPMDifferences;
				public NativeArray<float> accelRPMLowPassFreq;
				[ReadOnly] public VehicleEngineAccess engine;
				[ReadOnly] public InputsAccess inputs;
				[ReadOnly] public StatsAccess stats;
				[ReadOnly] public bool isElectric;
				[ReadOnly] public bool listener;
				[ReadOnly] public float3 listenerLocalPosition;
				[ReadOnly] public float3 engineCenter;
				[ReadOnly] public float3 exhaustsCenter;
				[ReadOnly] public float gearRatio;
				[ReadOnly] public float SFXVolume;
				[ReadOnly] public float engineVolume;
				[ReadOnly] public float exhaustVolume;
				[ReadOnly] public float oldEngineRPM;
				[ReadOnly] public float deltaTime;

				private float lowPassMaximumRPM;
				private float rpmLowPassFreqFactor;
				private float accelLowPassFreq;
				private float accelFactor;
				private float lowPassFreq;
				private float engineExhaustFactor;
				private float distortionLevel;
				private float engineAccelerationVolume;
				private float engineDecelerationVolume;
				private float exhaustAccelerationVolume;
				private float exhaustDecelerationVolume;
				private bool separatedCelerations;
				private bool hasDoubleOutputs;

				#endregion

				#region Methods

				public void Execute()
				{
					if (engine.audio.useAccelLowPass && engine.audio.accelLowPassFrequency < 22000f && engine.audio.accelLowPassRPMEnd > 0f)
					{
						lowPassMaximumRPM = Utility.Lerp(engine.minimumRPM, engine.redlineRPM, engine.audio.accelLowPassRPMEnd);
						rpmLowPassFreqFactor = Utility.InverseLerp(engine.minimumRPM, lowPassMaximumRPM, stats.engineRPM);
						accelLowPassFreq = Utility.Lerp(engine.audio.accelLowPassFrequency, 22000f, rpmLowPassFreqFactor);
						accelRPMLowPassFreq[0] = engine.audio.useLowPassDamping ? Utility.Lerp(accelRPMLowPassFreq[0], accelLowPassFreq, deltaTime * engine.audio.lowPassDamping) : accelLowPassFreq;
					}
					else
						accelRPMLowPassFreq[0] = 22000f;

					float newDifference = (stats.engineRPM - oldEngineRPM) * Utility.Clamp01(stats.engineRPM - engine.minimumRPM);

					previousRPMDifferences.Enqueue(newDifference);

					if (previousRPMDifferences.Count > 10)
						previousRPMDifferences.Dequeue();

					NativeArray<float> differences = previousRPMDifferences.ToArray(Allocator.Temp);
					float averageRPMDifference = default;

					foreach (var difference in differences)
						averageRPMDifference += difference;

					averageRPMDifference /= previousRPMDifferences.Count;

					differences.Dispose();

					accelFactor = inputs.Fuel + Utility.Clamp01(averageRPMDifference / stats.engineRPMInertia);
					separatedCelerations = engine.audio.celerationType == VehicleEngine.AudioModule.CelerationType.Separated;
					hasDoubleOutputs = !isElectric && engine.audio.outputs == VehicleEngine.AudioModule.OutputType.EngineAndExhaust;
					lowPassFreq = Utility.Lerp(engine.audio.decelLowPassFrequency * (stats.isEngineRunning ? 1f : .5f), accelRPMLowPassFreq[0], accelFactor);
					engineExhaustFactor = hasDoubleOutputs && !engineCenter.Equals(exhaustsCenter) && listener ? Utility.InverseLerp(engineCenter, exhaustsCenter, listenerLocalPosition) : 1f;
					distortionLevel = isElectric ? default : Utility.Lerp(engine.audio.overRPMDistortion, engine.audio.maxRPMDistortion, accelFactor);

					if (separatedCelerations)
					{
						engineAccelerationVolume = inputs.RawFuel * SFXVolume * engineVolume;
						exhaustAccelerationVolume = inputs.RawFuel * SFXVolume * exhaustVolume;
						engineDecelerationVolume = (1f - inputs.RawFuel) * SFXVolume * engineVolume;
						exhaustDecelerationVolume = (1f - inputs.RawFuel) * SFXVolume * exhaustVolume;
					}
					else
					{
						engineAccelerationVolume = SFXVolume * engineVolume;
						exhaustAccelerationVolume = SFXVolume * exhaustVolume;
						exhaustDecelerationVolume = engineDecelerationVolume = 0f;
					}

					float volumeMultiplier = Utility.Clamp01(stats.engineRPM / engine.minimumRPM) * Utility.Lerp(engine.audio.lowRPMVolume, 1f, Utility.InverseLerp(engine.minimumRPM, engine.overRevRPM, stats.engineRPM));

					if (engine.audio.outputs != VehicleEngine.AudioModule.OutputType.Exhaust)
					{
						for (int i = 0; i < engineAccelerationSourcesAccess.Length; i++)
							UpdateCeleration(ref engineAccelerationSourcesAccess, i, engineAccelerationVolume * volumeMultiplier, false);

						for (int i = 0; i < engineDecelerationSourcesAccess.Length; i++)
							UpdateCeleration(ref engineDecelerationSourcesAccess, i, engineDecelerationVolume * volumeMultiplier, false);
					}

					if (!isElectric && engine.audio.outputs != VehicleEngine.AudioModule.OutputType.Engine)
					{
						for (int i = 0; i < exhaustsAccelerationSourcesAccess.Length; i++)
							UpdateCeleration(ref exhaustsAccelerationSourcesAccess, i, exhaustAccelerationVolume * volumeMultiplier, true);

						for (int i = 0; i < exhaustsDecelerationSourcesAccess.Length; i++)
							UpdateCeleration(ref exhaustsDecelerationSourcesAccess, i, exhaustDecelerationVolume * volumeMultiplier, true);
					}
				}

				private readonly void UpdateCeleration(ref NativeArray<VehicleEngine.AudioModule.CelerationSourceAccess> sources, int index, float volume, bool exhaustSource)
				{
					if (sources[index].rpm < 1f)
						return;

					VehicleEngine.AudioModule.CelerationSourceAccess source = sources[index];

					if (!stats.isEngineRunning)
					{
						source.enabled = false;

						goto set_source;
					}
					else if (sources.Length < 2)
					{
						source.volume = volume;
						source.enabled = true;

						goto set_source;
					}

					if (sources.Length > 2)
					{
						if (index == 0)
							source.enabled = !Mathf.Approximately(Utility.Clamp01(stats.engineRPM / source.rpm) - Utility.Clamp01((stats.engineRPM - source.rpm) / (sources[index + 2].rpm - source.rpm)), 0f);
						else if (index == 1)
							source.enabled = !Mathf.Approximately(Utility.Clamp01((stats.engineRPM - sources[index - 1].rpm) / (source.rpm - sources[index - 1].rpm)) - Utility.Clamp01((stats.engineRPM - source.rpm) / (sources[math.min(index + 2, sources.Length - 1)].rpm - source.rpm)), 0f);
						else if (index + 2 < sources.Length)
							source.enabled = !Mathf.Approximately(Utility.Clamp01((stats.engineRPM - sources[index - 2].rpm) / (source.rpm - sources[index - 2].rpm)) - Utility.Clamp01((stats.engineRPM - source.rpm) / (sources[index + 2].rpm - source.rpm)), 0f);
						else
							source.enabled = !Mathf.Approximately(Utility.Clamp01((stats.engineRPM - sources[index - 2].rpm) / (source.rpm - sources[index - 2].rpm)), 0f);
					}
					else
						source.enabled = true;

					if (source.enabled)
					{
						if (index == 0)
							source.volume = Utility.Clamp01(stats.engineRPM / source.rpm) - Utility.Clamp01((stats.engineRPM - source.rpm) / (sources[index + 1].rpm - source.rpm));
						else if (index + 1 < sources.Length)
							source.volume = Utility.Clamp01((stats.engineRPM - sources[index - 1].rpm) / (source.rpm - sources[index - 1].rpm)) - Utility.Clamp01((stats.engineRPM - source.rpm) / (sources[index + 1].rpm - source.rpm));
						else
							source.volume = Utility.Clamp01((stats.engineRPM - sources[index - 1].rpm) / (source.rpm - sources[index - 1].rpm));

						source.volume *= volume;

						if (hasDoubleOutputs)
							source.volume *= .5f + .5f * Utility.Clamp01(exhaustSource ? engineExhaustFactor : 1f - engineExhaustFactor);
					}
					else
						source.volume = 0f;

					set_source:
					source.pitch = stats.engineRPM / source.rpm;
					source.distortionLevel = distortionLevel;
					source.cutOffFrequency = lowPassFreq;
					sources[index] = source;
				}

				#endregion
			}

			#endregion

			#region Components

			public class LevelledSourcesGroup
			{
				#region Variables

				public readonly VehicleAudioSource[] low = new VehicleAudioSource[] { };
				public readonly VehicleAudioSource[] medium = new VehicleAudioSource[] { };
				public readonly VehicleAudioSource[] high = new VehicleAudioSource[] { };
				public ToolkitSettings.LevelledClipsGroup clipsGroup;

				#endregion

				#region Constructors

				public LevelledSourcesGroup(Vehicle vehicle, ToolkitSettings.LevelledClipsGroup clipsGroup)
				{
					if (!clipsGroup)
						return;

					low = VehicleAudio.NewAudioSources(clipsGroup.low, vehicle, "LowImpactSFX", 5f, 10f, 0f, false, false, true, 0f, Settings.audioMixers.environmentalEffects);
					medium = VehicleAudio.NewAudioSources(clipsGroup.medium, vehicle, "MediumImpactSFX", 5f, 10f, 0f, false, false, true, 0f, Settings.audioMixers.environmentalEffects);
					high = VehicleAudio.NewAudioSources(clipsGroup.high, vehicle, "HighImpactSFX", 5f, 10f, 0f, false, false, true, 0f, Settings.audioMixers.environmentalEffects);
					this.clipsGroup = clipsGroup;
				}

				#endregion
			}
			public class ImpactSources
			{
				#region Variables

				public readonly LevelledSourcesGroup cars;
				public readonly LevelledSourcesGroup walls;
				/*public readonly VehicleAudioSource[] glassCrack;
				public readonly VehicleAudioSource[] glassShatter;
				public readonly VehicleAudioSource[] metalFences;
				public readonly VehicleAudioSource[] roadCones;
				public readonly VehicleAudioSource[] roadMisc;
				public readonly VehicleAudioSource[] roadSigns;*/

				#endregion

				#region Constructors

				public ImpactSources(Vehicle vehicle, ToolkitSettings.ImpactClips impactClips)
				{
					if (!impactClips || !vehicle)
						return;

					cars = new(vehicle, impactClips.cars);
					walls = new(vehicle, impactClips.walls);
				}

				#endregion
			}
			public class ExhaustsBlowoutSourcesGroup
			{
				#region Variables

				public readonly VehicleAudioSource[] gurgle;
				public readonly VehicleAudioSource[] pop;
				public readonly ToolkitSettings.ExhaustsBlowoutClipsGroup clipsGroup;

				#endregion

				#region Constructors

				public ExhaustsBlowoutSourcesGroup(Vehicle vehicle, ToolkitSettings.ExhaustsBlowoutClipsGroup clips, Vector3 localPosition)
				{
					if (!clips)
						return;

					if (clips.gurgle != null)
						gurgle = VehicleAudio.NewAudioSources(clips.gurgle, vehicle, localPosition, "ExhaustGurgleSFX", 5f, 20f, 0f, false, false, true, 0f, vehicle.audioMixers.ExhaustEffects);

					if (clips.pop != null)
						pop = VehicleAudio.NewAudioSources(clips.pop, vehicle, localPosition, "ExhaustPopSFX", 5f, 20f, 0f, false, false, true, 0f, vehicle.audioMixers.ExhaustEffects);

					clipsGroup = clips;
				}

				#endregion
			}
			public class GearShiftingSources
			{
				#region Variables

				public readonly VehicleAudioSource[] up;
				public readonly VehicleAudioSource[] down;
				public readonly ToolkitSettings.GearShiftingClips clipsGroup;

				#endregion

				#region Constructors

				public GearShiftingSources(Vehicle vehicle, ToolkitSettings.GearShiftingClips clips, Vector3 localPosition)
				{
					if (!clips)
						return;

					if (clips.up != null)
						up = VehicleAudio.NewAudioSources(clips.up, vehicle, localPosition, "GearShiftUpSFX", .5f, 1f, 0f, false, false, true, 0f, vehicle.audioMixers.Transmission);

					if (clips.down != null)
						down = VehicleAudio.NewAudioSources(clips.down, vehicle, localPosition, "GearShiftDownSFX", .5f, 1f, 0f, false, false, true, 0f, vehicle.audioMixers.Transmission);

					clipsGroup = clips;
				}

				#endregion
			}
			public class NOSSourcesGroup
			{
				#region Variables

				public readonly VehicleAudioSource[] starting;
				public readonly VehicleAudioSource[] active;
				public readonly ToolkitSettings.NOSClipsGroup clipsGroup;

				#endregion

				#region Constructors

				public NOSSourcesGroup(Vehicle vehicle, ToolkitSettings.NOSClipsGroup clips, Vector3 localPosition)
				{
					if (!clips)
						return;

					if (clips.starting != null)
						starting = VehicleAudio.NewAudioSources(clips.starting, vehicle, localPosition, "NOSStartingSFX", 2f, 5f, 0f, false, false, true, 0f, Settings.audioMixers.NOS);

					if (clips.active != null)
						active = VehicleAudio.NewAudioSources(clips.active, vehicle, localPosition, "NOSActiveSFX", 2f, 5f, 0f, false, false, true, 0f, Settings.audioMixers.NOS);

					clipsGroup = clips;
				}

				#endregion
			}

			#endregion

			#endregion

			#region Variables

			public readonly VehicleAudioSource turbochargerGearBlowout;
			public readonly VehicleAudioSource turbochargerFuelBlowout;
			public readonly VehicleAudioSource turbochargerRevBlowout;
			public readonly VehicleAudioSource superchargerGearBlowout;
			public readonly VehicleAudioSource superchargerFuelBlowout;
			public readonly VehicleAudioSource superchargerRevBlowout;
			public readonly VehicleAudioSource indicator;

			public readonly ImpactSources impactSources;
			public readonly ExhaustsBlowoutSourcesGroup exhaustBlowoutSources;
			public readonly GearShiftingSources gearShiftingSources;
			public readonly NOSSourcesGroup NOSSources;
			public readonly ToolkitSettings.TransmissionWhineClipsGroup transmissionWhineClips;
			public readonly VehicleAudioSource engineStartingSource;
			public readonly VehicleAudioSource reversing;
			public readonly VehicleAudioSource turbochargerIdle;
			public readonly VehicleAudioSource turbochargerActive;
			public readonly VehicleAudioSource superchargerIdle;
			public readonly VehicleAudioSource superchargerActive;
			public readonly VehicleAudioSource wind;
			public VehicleAudioSource NOS;

			private readonly Vehicle vehicle;
			private readonly VehicleEngine.AudioModule.CelerationClip[] engineAccelerationClips;
			private readonly VehicleEngine.AudioModule.CelerationClip[] engineDecelerationClips;
			private readonly VehicleEngine.AudioModule.CelerationClip[] exhaustsAccelerationClips;
			private readonly VehicleEngine.AudioModule.CelerationClip[] exhaustsDecelerationClips;
			private readonly VehicleEngine.AudioModule.CelerationSource[] engineAccelerationSources;
			private readonly VehicleEngine.AudioModule.CelerationSource[] engineDecelerationSources;
			private readonly VehicleEngine.AudioModule.CelerationSource[] exhaustsAccelerationSources;
			private readonly VehicleEngine.AudioModule.CelerationSource[] exhaustsDecelerationSources;
			private readonly VehicleEngine.AudioModule.CelerationSource[] transmissionWhiningSources;
			private readonly NativeArray<VehicleEngine.AudioModule.CelerationSourceAccess> engineAccelerationSourcesAccess;
			private readonly NativeArray<VehicleEngine.AudioModule.CelerationSourceAccess> engineDecelerationSourcesAccess;
			private readonly NativeArray<VehicleEngine.AudioModule.CelerationSourceAccess> exhaustsAccelerationSourcesAccess;
			private readonly NativeArray<VehicleEngine.AudioModule.CelerationSourceAccess> exhaustsDecelerationSourcesAccess;
			private readonly NativeQueue<float> previousRPMDifferences;
			private readonly Vector3 engineCenter;
			private readonly Vector3 exhaustsCenter;
			private float accelRPMLowPassFreq;
			private float maximumTargetSpeed;
			private float oldEngineRPM;
			private float volume;
			private int frameCount;

			#endregion

			#region Methods

			public void Update()
			{
				if (HasInternalErrors || !Listener || frameCount == Time.frameCount)
					return;

				UpdateEngine();
				UpdateExhaust();
				UpdateTurbocharger();
				UpdateSupercharger();
				UpdateNOS();
				UpdateTransmission();
				UpdateWind();

				frameCount = Time.frameCount;
			}
			public void EngineStart()
			{
				if (HasInternalErrors || vehicle.IsElectric || !engineStartingSource || !vehicle.Engine)
					return;

				engineStartingSource.source.volume = Settings.SFXVolume * vehicle.audioMixers.EngineVolume * vehicle.Engine.Audio.lowRPMVolume;

				engineStartingSource.PlayOnceAndDisable();
			}
			public VehicleAudioSource ExhaustGurgle()
			{
				if (vehicle.IsElectric)
					return null;

				if (exhaustBlowoutSources == null || exhaustBlowoutSources.gurgle == null || exhaustBlowoutSources.gurgle.Length < 1)
					return null;

				VehicleAudioSource source = exhaustBlowoutSources.gurgle[UnityEngine.Random.Range(0, exhaustBlowoutSources.gurgle.Length)];

				if (source)
				{
					source.source.volume = Settings.SFXVolume * vehicle.audioMixers.ExhaustEffectsVolume * exhaustBlowoutSources.clipsGroup.Volume;

					source.PlayOnceAndDisable();
				}

				return source;
			}
			public VehicleAudioSource ExhaustPop()
			{
				if (vehicle.IsElectric || vehicle.Engine.FuelType == VehicleEngine.EngineFuelType.Diesel)
					return null;

				if (exhaustBlowoutSources == null || exhaustBlowoutSources.pop == null || exhaustBlowoutSources.pop.Length < 1)
					return null;

				VehicleAudioSource source = exhaustBlowoutSources.pop[UnityEngine.Random.Range(0, exhaustBlowoutSources.pop.Length)];

				if (source)
				{
					source.source.volume = Settings.SFXVolume * vehicle.audioMixers.ExhaustEffectsVolume * exhaustBlowoutSources.clipsGroup.Volume;

					source.PlayOnceAndDisable();
				}

				return source;
			}
			public VehicleAudioSource GearboxShiftUp()
			{
				if (HasInternalErrors || gearShiftingSources == null || gearShiftingSources.up == null || gearShiftingSources.up.Length < 1 || vehicle.transmission.Gearbox != TransmissionModule.GearboxType.Manual)
					return null;

				VehicleAudioSource source = gearShiftingSources.up[UnityEngine.Random.Range(0, gearShiftingSources.up.Length)];

				source.source.volume = Settings.SFXVolume * vehicle.audioMixers.TransmissionVolume * Settings.gearShiftingClips.Volume;

				source.PlayOnceAndDisable();

				return source;
			}
			public VehicleAudioSource GearboxShiftDown()
			{
				if (HasInternalErrors || gearShiftingSources == null || gearShiftingSources.down == null || gearShiftingSources.down.Length < 1 || vehicle.transmission.Gearbox != TransmissionModule.GearboxType.Manual)
					return null;

				VehicleAudioSource source = gearShiftingSources.down[UnityEngine.Random.Range(0, gearShiftingSources.down.Length)];

				source.source.volume = Settings.SFXVolume * vehicle.audioMixers.TransmissionVolume * Settings.gearShiftingClips.Volume;

				source.PlayOnceAndDisable();

				return source;
			}
			public void TurbochargerGearBlowout()
			{
				if (!turbochargerGearBlowout)
					return;

				turbochargerGearBlowout.source.volume = Settings.SFXVolume * vehicle.audioMixers.TurbochargerVolume;

				turbochargerGearBlowout.PlayOnceAndDisable();
			}
			public void TurbochargerFuelBlowout()
			{
				if (!turbochargerFuelBlowout)
					return;

				turbochargerFuelBlowout.source.volume = Settings.SFXVolume * vehicle.inputs.Fuel * vehicle.audioMixers.TurbochargerVolume;

				turbochargerFuelBlowout.PlayOnceAndDisable();
			}
			public void TurbochargerRevBlowout()
			{
				if (!turbochargerRevBlowout)
					return;

				turbochargerRevBlowout.source.volume = Settings.SFXVolume * vehicle.audioMixers.TurbochargerVolume;

				turbochargerRevBlowout.PlayOnceAndDisable();
			}
			public void SuperchargerGearBlowout()
			{
				if (!superchargerGearBlowout)
					return;

				superchargerGearBlowout.source.volume = Settings.SFXVolume * vehicle.audioMixers.SuperchargerVolume;

				superchargerGearBlowout.PlayOnceAndDisable();
			}
			public void SuperchargerFuelBlowout()
			{
				if (!superchargerFuelBlowout)
					return;

				superchargerFuelBlowout.source.volume = Settings.SFXVolume * vehicle.inputs.Fuel * vehicle.audioMixers.SuperchargerVolume;

				superchargerFuelBlowout.PlayOnceAndDisable();
			}
			public void SuperchargerRevBlowout()
			{
				if (!superchargerRevBlowout)
					return;

				superchargerRevBlowout.source.volume = Settings.SFXVolume * vehicle.audioMixers.SuperchargerVolume;

				superchargerRevBlowout.PlayOnceAndDisable();
			}
			public VehicleAudioSource NOSStart()
			{
				if (vehicle.IsElectric || !Settings.useNOS || !vehicle.behaviour.useNOS || !Settings.useNOSDelay || NOSSources == null || NOSSources.starting == null || NOSSources.starting.Length < 1)
					return null;

				VehicleAudioSource source = NOSSources.starting[UnityEngine.Random.Range(0, NOSSources.starting.Length)];

				if (source)
				{
					source.source.volume = Settings.SFXVolume * NOSSources.clipsGroup.Volume;

					source.PlayOnceAndDisable();
				}

				return source;
			}
			public void IndicatorChange()
			{
				if (!indicator)
					return;

				indicator.source.volume = Settings.SFXVolume;

				indicator.PlayOnceAndDisable();
			}
			public void CarImpact(float relativeVelocityMagnitudeSqr, Vector3 position)
			{
				if (HasInternalErrors)
					return;

				LevelledImpact(impactSources.cars, relativeVelocityMagnitudeSqr, position);
			}
			public void WallImpact(float relativeVelocityMagnitudeSqr, Vector3 position)
			{
				if (HasInternalErrors)
					return;

				LevelledImpact(impactSources.walls, relativeVelocityMagnitudeSqr, position);
			}

			internal void Dispose()
			{
				if (engineAccelerationSourcesAccess.IsCreated)
					engineAccelerationSourcesAccess.Dispose();

				if (engineDecelerationSourcesAccess.IsCreated)
					engineDecelerationSourcesAccess.Dispose();

				if (exhaustsAccelerationSourcesAccess.IsCreated)
					exhaustsAccelerationSourcesAccess.Dispose();

				if (exhaustsDecelerationSourcesAccess.IsCreated)
					exhaustsDecelerationSourcesAccess.Dispose();

				if (previousRPMDifferences.IsCreated)
					previousRPMDifferences.Dispose();
			}

			private VehicleEngine.AudioModule.CelerationClip[] CelerationClipInitialize(Object[] resources)
			{
				if (resources == null || resources.Length < 1)
					return null;

				VehicleEngine.AudioModule.CelerationClip[] clips = new VehicleEngine.AudioModule.CelerationClip[math.min(resources.Length, Settings.maxEngineSFXClips)];

				for (int i = 0; i < clips.Length; i++)
				{
					if (!resources[i])
						continue;

					int k = (int)math.round(Utility.Lerp(0, resources.Length - 1, clips.Length > 1 ? (float)i / (clips.Length - 1) : 0f));
					string[] RPMStrSplits = resources[k].name.Split(Settings.engineSFXNameSplitter, StringSplitOptions.RemoveEmptyEntries);
					float clipRPM = 0f;

					for (int j = 0; j < RPMStrSplits.Length; j++)
						if (float.TryParse(RPMStrSplits[j], out float testRPM))
							clipRPM = math.max(clipRPM, testRPM);

					clips[i] = new()
					{
						clip = resources[k] as AudioClip,
						rpm = clipRPM
					};
				}

				Array.Sort(clips, new VehicleEngine.AudioModule.CelerationClip.SortComparer());

				return clips;
			}
			private VehicleEngine.AudioModule.CelerationSource[] CelerationSourcesInitialize(bool exhaustSource, VehicleEngine.AudioModule.CelerationClip[] clips, Transform parent, Vector3 localPosition, AudioMixerGroup mixer = null)
			{
				if (clips == null || clips.Length < 1)
					return null;

				List<VehicleEngine.AudioModule.CelerationSource> sources = new();

				for (int i = 0; i < clips.Length; i++)
				{
					VehicleAudioSource source = VehicleAudio.NewAudioSource(vehicle, $"{(exhaustSource ? "Exhaust" : "Engine")}Accel{clips[i].rpm}SFX", 5f, 75f, 0f, clips[i].clip, true, false, true, 0f, mixer);
					AudioDistortionFilter distortionFilter = source.gameObject.AddComponent<AudioDistortionFilter>();
					AudioLowPassFilter lowPassFilter = source.gameObject.AddComponent<AudioLowPassFilter>();

					source.transform.SetParent(parent, false);

					source.transform.localPosition = localPosition;
					source.source.enabled = false;
					source.source.volume = 0f;
					source.source.pitch = 0f;
					lowPassFilter.cutoffFrequency = 22000f;
					distortionFilter.distortionLevel = 0f;

					sources.Add(new()
					{
						instance = source,
						distortionFilter = distortionFilter,
						lowPassFilter = lowPassFilter,
						rpm = clips[i].rpm
					});
				}

				return sources.ToArray();
			}
			private NativeArray<VehicleEngine.AudioModule.CelerationSourceAccess> CelerationSourcesAccessInitialize(VehicleEngine.AudioModule.CelerationSource[] sources)
			{
				if (sources == null)
					return new(0, Allocator.Persistent);

				NativeArray<VehicleEngine.AudioModule.CelerationSourceAccess> access = new(sources.Length, Allocator.Persistent);

				for (int i = 0; i < sources.Length; i++)
					access[i] = new()
					{
						rpm = sources[i].rpm
					};

				return access;
			}
			private VehicleEngine.AudioModule.CelerationSource[] TransmissionSourcesInitialize(ToolkitSettings.TransmissionWhineClipsGroup group, Transform parent, Vector3 localPosition, AudioMixerGroup mixer = null)
			{
				if (group == null || group.clips.Length < 1)
					return null;

				VehicleEngine.AudioModule.CelerationSource[] sources = new VehicleEngine.AudioModule.CelerationSource[group.clips.Length];

				for (int i = 0; i < group.clips.Length; i++)
				{
					VehicleAudioSource source = VehicleAudio.NewAudioSource(vehicle, $"TransmissionWhine{group.clips[i].Speed}SFX", 5f, 100f, 0f, group.clips[i].clip, true, false, true, 0f, mixer);
					AudioLowPassFilter lowPassFilter = source.gameObject.AddComponent<AudioLowPassFilter>();

					source.transform.SetParent(parent, false);

					source.transform.localPosition = localPosition;
					source.source.volume = 0f;
					source.source.pitch = 0f;
					source.enabled = false;

					sources[i] = new()
					{
						instance = source,
						lowPassFilter = lowPassFilter,
						rpm = group.clips[i].Speed
					};
				}

				return sources;
			}
			private void UpdateCeleration(VehicleEngine.AudioModule.CelerationSource[] sources, NativeArray<VehicleEngine.AudioModule.CelerationSourceAccess> sourcesAccess)
			{
				if (sources == null || !sourcesAccess.IsCreated || sources.Length != sourcesAccess.Length)
					return;

				for (int i = 0; i < sources.Length; i++)
				{
					if (sourcesAccess[i].rpm < 1f)
					{
						if (sources[i].instance.source.isPlaying)
							sources[i].instance.source.Stop();

						continue;
					}
					else if (!sourcesAccess[i].enabled)
					{
						if (sources[i].instance.source.enabled)
							sources[i].instance.source.enabled = false;

						goto set_source;
					}
					else if (!sources[i].instance.source.enabled)
						sources[i].instance.source.enabled = true;

					if (!sources[i].instance.source.isPlaying)
						sources[i].instance.source.Play();

					set_source:
					sources[i].distortionFilter.distortionLevel = sourcesAccess[i].distortionLevel;
					sources[i].lowPassFilter.cutoffFrequency = sourcesAccess[i].cutOffFrequency;
					sources[i].instance.source.volume = sourcesAccess[i].volume;
					sources[i].instance.source.pitch = sourcesAccess[i].pitch;
				}
			}
			private void UpdateEngine()
			{
				if (oldEngineRPM == default)
					oldEngineRPM = vehicle.stats.engineRPM;

				NativeArray<float> tempAccelRPMLowPassFreq = new(1, Allocator.TempJob);

				tempAccelRPMLowPassFreq[0] = accelRPMLowPassFreq;

				UpdateEngineJob job = new()
				{
					engineAccelerationSourcesAccess = engineAccelerationSourcesAccess,
					engineDecelerationSourcesAccess = engineDecelerationSourcesAccess,
					exhaustsAccelerationSourcesAccess = exhaustsAccelerationSourcesAccess,
					exhaustsDecelerationSourcesAccess = exhaustsDecelerationSourcesAccess,
					previousRPMDifferences = previousRPMDifferences,
					accelRPMLowPassFreq = tempAccelRPMLowPassFreq,
					stats = vehicle.stats,
					inputs = vehicle.inputs,
					engine = vehicle.engineAccess,
					isElectric = vehicle.IsElectric,
					listener = Listener,
					listenerLocalPosition = vehicle.transform.InverseTransformPoint(Listener.transform.position),
					engineCenter = engineCenter,
					exhaustsCenter = exhaustsCenter,
					gearRatio = vehicle.transmission.GetGearRatio(vehicle.inputs.Direction, vehicle.stats.currentGear, true),
					SFXVolume = Settings.SFXVolume,
					engineVolume = vehicle.audioMixers.EngineVolume,
					exhaustVolume = vehicle.audioMixers.ExhaustVolume,
					oldEngineRPM = oldEngineRPM,
					deltaTime = Utility.DeltaTime
				};

				job.Schedule().Complete();

				accelRPMLowPassFreq = tempAccelRPMLowPassFreq[0];
				oldEngineRPM = vehicle.stats.engineRPM;

				tempAccelRPMLowPassFreq.Dispose();

				UpdateCeleration(engineAccelerationSources, engineAccelerationSourcesAccess);
				UpdateCeleration(engineDecelerationSources, engineDecelerationSourcesAccess);

				if (!vehicle.IsElectric)
				{
					UpdateCeleration(exhaustsAccelerationSources, exhaustsAccelerationSourcesAccess);
					UpdateCeleration(exhaustsDecelerationSources, exhaustsDecelerationSourcesAccess);
				}
			}
			private void UpdateExhaust()
			{
				if (!vehicle.stats.isEngineRunning || vehicle.IsElectric)
					return;

				if (vehicle.inputs.FuelPedalWasReleased && UnityEngine.Random.Range(0f, 1f) < vehicle.behaviour.ExhaustFlameEmissionProbability)
				{
					float minimumPeakRPM = math.min(vehicle.behaviour.PeakPowerRPM, vehicle.behaviour.PeakTorqueRPM);

					if (vehicle.behaviour.useExhaustEffects)
					{
						if (vehicle.stats.engineRPM >= minimumPeakRPM)
							ExhaustPop();
						else if (vehicle.stats.engineRPM >= Utility.Average(vehicle.Engine.MinimumRPM, vehicle.Engine.MinimumRPM, minimumPeakRPM))
							ExhaustGurgle();
					}
				}
			}
			private void UpdateTurbocharger()
			{
				if (vehicle.IsElectric)
					return;

				if (!vehicle.behaviour.IsTurbocharged)
				{
					if (turbochargerIdle)
					{
						turbochargerIdle.source.volume = 0f;

						if (turbochargerIdle.source.isPlaying)
							turbochargerIdle.StopAndDisable();
					}

					if (turbochargerActive)
					{
						turbochargerActive.source.volume = 0f;

						if (turbochargerActive.source.isPlaying)
							turbochargerActive.StopAndDisable();
					}

					return;
				}

				if (turbochargerIdle && !turbochargerIdle.source.isPlaying)
					turbochargerIdle.PlayAndEnable();

				if (turbochargerActive && !turbochargerActive.source.isPlaying)
					turbochargerActive.PlayAndEnable();

				VehicleCharger turbocharger = vehicle.Turbocharger;
				float idleVolume = vehicle.stats.engineBoost;
				float idlePitch = 1f + Utility.InverseLerp(vehicle.Engine.MinimumRPM, vehicle.Engine.RedlineRPM, vehicle.stats.engineRPM);
				float activeVolume = Utility.InverseLerp(turbocharger.InertiaRPM, vehicle.Engine.RedlineRPM, vehicle.stats.engineRPM);
				float activePitch = idlePitch + vehicle.stats.engineBoost;
				float runningFactor = Utility.Clamp01(vehicle.stats.engineRPM / vehicle.Engine.MinimumRPM);

				if (turbochargerIdle)
				{
					turbochargerIdle.source.volume = idleVolume * runningFactor * Settings.SFXVolume * vehicle.audioMixers.ChargersEffectsVolume;
					turbochargerIdle.source.pitch = idlePitch;
				}

				if (turbochargerActive)
				{
					turbochargerActive.source.volume = activeVolume * runningFactor * Settings.SFXVolume * vehicle.audioMixers.ChargersEffectsVolume;
					turbochargerActive.source.pitch = activePitch;
				}

				if (vehicle.inputs.FuelPedalWasReleased)
				{
					if (vehicle.stats.isRevLimited)
					{
						TurbochargerRevBlowout();

						if (!vehicle.behaviour.IsSupercharged)
							vehicle.stats.isRevLimiting = false;
					}
					else if (vehicle.stats.engineBoost >= .5f)
						TurbochargerFuelBlowout();
				}
			}
			private void UpdateSupercharger()
			{
				if (vehicle.IsElectric)
					return;

				if (!vehicle.behaviour.IsSupercharged)
				{
					if (superchargerIdle)
					{
						superchargerIdle.source.volume = 0f;

						if (superchargerIdle.source.isPlaying)
							superchargerIdle.StopAndDisable();
					}

					if (superchargerActive)
					{
						superchargerActive.source.volume = 0f;

						if (superchargerActive.source.isPlaying)
							superchargerActive.StopAndDisable();
					}

					return;
				}

				if (!superchargerIdle.source.isPlaying)
					superchargerIdle.PlayAndEnable();

				if (!superchargerActive.source.isPlaying)
					superchargerActive.PlayAndEnable();

				float idlePitch = 1f + Utility.InverseLerp(vehicle.Engine.MinimumRPM, vehicle.Engine.RedlineRPM, vehicle.stats.engineRPM);
				float idleVolume = vehicle.stats.engineBoost;
				float activePitch = idlePitch;
				float activeVolume = idleVolume + (idlePitch - 1f);
				float runningFactor = Utility.Clamp01(vehicle.stats.engineRPM / vehicle.Engine.MinimumRPM);

				superchargerIdle.source.volume = idleVolume * runningFactor * Settings.SFXVolume * vehicle.audioMixers.ChargersEffectsVolume;
				superchargerIdle.source.pitch = idlePitch;
				superchargerActive.source.volume = activeVolume * runningFactor * Settings.SFXVolume * vehicle.audioMixers.ChargersEffectsVolume;
				superchargerActive.source.pitch = activePitch;

				if (vehicle.inputs.FuelPedalWasReleased)
				{
					if (vehicle.stats.isRevLimited)
					{
						SuperchargerRevBlowout();

						vehicle.stats.isRevLimited = false;
					}
					else if (vehicle.stats.engineBoost >= .5f)
						SuperchargerFuelBlowout();
				}
			}
			private void UpdateNOS()
			{
				if (vehicle.IsElectric)
					return;

				if (!Settings.useNOS || !vehicle.behaviour.useNOS || NOSSources.active.Length < 1)
					return;

				if (!vehicle.stats.isNOSActive || !vehicle.isNOSReady)
				{
					if (NOS)
					{
						if (NOS.source.isPlaying)
							NOS.StopAndDisable();

						NOS = null;
					}

					return;
				}

				if (!NOS)
					NOS = NOSSources.active[UnityEngine.Random.Range(0, NOSSources.active.Length)];
				else
				{
					NOS.source.volume = Settings.SFXVolume * NOSSources.clipsGroup.Volume;

					if (!NOS.source.isPlaying)
						NOS.PlayAndEnable();
				}
			}
			private void UpdateTransmission()
			{
				maximumTargetSpeed = vehicle.stats.targetSpeed * vehicle.engine.MaximumRPM / vehicle.engine.OverRevRPM;

				if (vehicle.stats.isReversing)
				{
					if (reversing)
					{
						reversing.source.volume = Utility.Clamp01(vehicle.stats.averageMotorWheelsSpeed * vehicle.inputs.Direction) * Settings.SFXVolume * vehicle.audioMixers.TransmissionVolume;
						reversing.source.pitch = math.clamp(-vehicle.stats.averageMotorWheelsSpeed, 0f, maximumTargetSpeed) / vehicle.stats.targetSpeed;
					}

					if (reversing && !reversing.source.isPlaying)
						reversing.PlayAndEnable();

					if (transmissionWhiningSources != null)
						foreach (var source in transmissionWhiningSources)
							if (source.instance.source.isPlaying)
								source.instance.StopAndDisable();
				}
				else
				{
					if (reversing && reversing.source.isPlaying)
						reversing.StopAndDisable();

					int clipsGroupIndex = vehicle.transmission.ClipsGroupIndex;

					if (transmissionWhiningSources != null && clipsGroupIndex > -1)
					{
						if (vehicle.stats.currentGear >= vehicle.transmission.PlayClipsStartingFromGear - 1)
							for (int i = 0; i < transmissionWhiningSources.Length; i++)
							{
								if (!transmissionWhiningSources[i].instance.source.isPlaying)
									transmissionWhiningSources[i].instance.PlayAndEnable();

								transmissionWhiningSources[i].lowPassFilter.cutoffFrequency = Utility.Lerp(Settings.transmissionWhineGroups[clipsGroupIndex].DecelerationLowPassFrequency, 22000f, vehicle.inputs.Fuel);
								transmissionWhiningSources[i].instance.source.pitch = math.clamp(vehicle.stats.averageMotorWheelsSpeed, 0f, maximumTargetSpeed) / transmissionWhiningSources[i].rpm;

								volume = Utility.Lerp(1f, Settings.transmissionWhineGroups[clipsGroupIndex].ClutchVolume, vehicle.inputs.Clutch);
								volume *= Utility.InverseLerp(i == 0 ? vehicle.stats.minTargetSpeed : transmissionWhiningSources[i - 1].rpm, transmissionWhiningSources[i].rpm, vehicle.stats.averageMotorWheelsSpeed) - (i < transmissionWhiningSources.Length - 1 ? Utility.InverseLerp(transmissionWhiningSources[i].rpm, transmissionWhiningSources[i + 1].rpm, vehicle.stats.averageMotorWheelsSpeed) : 0f);
								volume *= vehicle.stats.currentGear <= vehicle.transmission.PlayClipsStartingFromGear - 1 ? Utility.InverseLerp(vehicle.stats.minTargetSpeed, vehicle.stats.targetSpeed, vehicle.stats.averageMotorWheelsSpeed) : 1f;
								volume *= Settings.SFXVolume * vehicle.audioMixers.TransmissionVolume;
								transmissionWhiningSources[i].instance.source.volume = volume;
							}
						else
							foreach (var source in transmissionWhiningSources)
								if (source.instance.source.isPlaying)
									source.instance.StopAndDisable();
					}
				}
			}
			private void UpdateWind()
			{
				if (!wind)
					return;
				else if (Mathf.Approximately(vehicle.stats.currentSpeed, 0f))
				{
					if (wind.source.isPlaying)
						wind.StopAndDisable();

					return;
				}
				else if (!wind.source.isPlaying)
					wind.PlayAndEnable();

				wind.source.volume = vehicle.stats.currentSpeed / 100f;
			}
			private void LevelledImpact(LevelledSourcesGroup sources, float relativeVelocitySqr, Vector3 position)
			{
				float lowVelocity = Settings.damageLowVelocity * Settings.damageLowVelocity;
				float mediumVelocity = Settings.damageMediumVelocity * Settings.damageMediumVelocity;
				float highVelocity = Settings.damageHighVelocity * Settings.damageHighVelocity;

				if (sources.low.Length > 0)
				{
					float lowVolume = Utility.InverseLerp(0f, lowVelocity, relativeVelocitySqr) - Utility.InverseLerp(lowVelocity, mediumVelocity, relativeVelocitySqr);
					VehicleAudioSource lowSource = sources.low[UnityEngine.Random.Range(0, sources.low.Length)];

					lowSource.source.volume = Settings.SFXVolume * lowVolume * sources.clipsGroup.Volume;
					lowSource.transform.position = position;

					lowSource.PlayOnceAndDisable();
				}

				if (sources.medium.Length > 0)
				{
					float mediumVolume = Utility.InverseLerp(lowVelocity, mediumVelocity, relativeVelocitySqr) - Utility.InverseLerp(mediumVelocity, highVelocity, relativeVelocitySqr);
					VehicleAudioSource mediumSource = sources.medium[UnityEngine.Random.Range(0, sources.medium.Length)];

					mediumSource.source.volume = Settings.SFXVolume * mediumVolume * sources.clipsGroup.Volume;
					mediumSource.transform.position = position;

					mediumSource.PlayOnceAndDisable();
				}

				if (sources.high.Length > 0)
				{
					float highVolume = Utility.InverseLerp(mediumVelocity, highVelocity, relativeVelocitySqr);
					VehicleAudioSource highSource = sources.high[UnityEngine.Random.Range(0, sources.high.Length)];

					highSource.source.volume = Settings.SFXVolume * highVolume * sources.clipsGroup.Volume;
					highSource.transform.position = position;

					highSource.PlayOnceAndDisable();
				}
			}

			#endregion

			#region Constructors

			public SFXSheet(Vehicle vehicle)
			{
				if (HasInternalErrors)
					return;

				this.vehicle = vehicle;
				impactSources = new(vehicle, Settings.impactClips);

				if (!vehicle.IsTrailer && vehicle.transmission.ClipsGroupIndex > -1)
					transmissionWhineClips = Settings.transmissionWhineGroups[vehicle.transmission.ClipsGroupIndex];

				if (!vehicle.Engine || !vehicle.Chassis)
					return;

				engineCenter = vehicle.Chassis.EngineCenterPosition;
				exhaustsCenter = vehicle.Exhausts.Length > 0 ? Utility.Average(vehicle.Exhausts.Select(exhaust => exhaust.localPosition)) : engineCenter;

				string engineClipsFolder = Path.Combine(Settings.EngineSFXFolderPath, vehicle.Engine.Audio.folderName.IsNullOrEmpty() ? vehicle.Engine.Name : vehicle.Engine.Audio.folderName).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
				bool hasSeparatedClips = vehicle.Engine.Audio.celerationType == VehicleEngine.AudioModule.CelerationType.Separated;
				bool hasEngineClips = vehicle.Engine.Audio.outputs != VehicleEngine.AudioModule.OutputType.Exhaust;
				bool hasExhaustClips = !vehicle.IsElectric && vehicle.Engine.Audio.outputs != VehicleEngine.AudioModule.OutputType.Engine;

				if (hasEngineClips)
				{
					engineAccelerationClips = CelerationClipInitialize(Resources.LoadAll<AudioClip>(Path.Combine(engineClipsFolder, "Engine", hasSeparatedClips ? "Accel" : "")));

					if (hasSeparatedClips)
						engineDecelerationClips = CelerationClipInitialize(Resources.LoadAll<AudioClip>(Path.Combine(engineClipsFolder, "Engine", "Decel")));
				}

				if (hasExhaustClips)
				{
					exhaustsAccelerationClips = CelerationClipInitialize(Resources.LoadAll<AudioClip>(Path.Combine(engineClipsFolder, "Exhaust", hasSeparatedClips ? "Accel" : "")));

					if (hasSeparatedClips)
						exhaustsDecelerationClips = CelerationClipInitialize(Resources.LoadAll<AudioClip>(Path.Combine(engineClipsFolder, "Exhaust", "Decel")));
				}

				vehicle.SFXParent = vehicle.Chassis.transform.Find("SoundEffects");

				if (!vehicle.SFXParent)
				{
					vehicle.SFXParent = new GameObject("SoundEffects").transform;

					vehicle.SFXParent.SetParent(vehicle.Chassis.transform, false);
				}

				var chassisBounds = vehicle.ChassisBounds;

				if (!vehicle.trailerInstance)
				{
					Transform accelSFXParent = vehicle.SFXParent.Find("AccelerationSFX");

					if (!accelSFXParent)
					{
						accelSFXParent = new GameObject("AccelerationSFX").transform;

						accelSFXParent.SetParent(vehicle.SFXParent, false);
					}

					Transform decelSFXParent = vehicle.SFXParent.Find("DecelerationSFX");

					if (!decelSFXParent)
					{
						decelSFXParent = new GameObject("DecelerationSFX").transform;

						decelSFXParent.SetParent(vehicle.SFXParent, false);
					}

					Transform transmissionSFXParent = vehicle.SFXParent.Find("TransmissionSFX");

					if (!transmissionSFXParent)
					{
						transmissionSFXParent = new GameObject("TransmissionSFX").transform;

						transmissionSFXParent.SetParent(vehicle.SFXParent, false);
					}

					engineStartingSource = VehicleAudio.NewAudioSource(vehicle, engineCenter, "EngineStartSFX", 5f, 20f, Settings.SFXVolume * vehicle.audioMixers.EngineVolume, vehicle.Engine.Audio.startingClip, false, false, true, 0f, vehicle.audioMixers.Engine);

					engineAccelerationSources = CelerationSourcesInitialize(false, engineAccelerationClips, accelSFXParent, engineCenter, vehicle.audioMixers.Engine);
					engineDecelerationSources = CelerationSourcesInitialize(false, engineDecelerationClips, decelSFXParent, engineCenter, vehicle.audioMixers.Engine);

					if (!vehicle.IsElectric)
					{
						exhaustsAccelerationSources = CelerationSourcesInitialize(true, exhaustsAccelerationClips, accelSFXParent, exhaustsCenter, vehicle.audioMixers.Exhaust);
						exhaustsDecelerationSources = CelerationSourcesInitialize(true, exhaustsDecelerationClips, decelSFXParent, exhaustsCenter, vehicle.audioMixers.Exhaust);
					}

					engineAccelerationSourcesAccess = CelerationSourcesAccessInitialize(engineAccelerationSources);
					engineDecelerationSourcesAccess = CelerationSourcesAccessInitialize(engineDecelerationSources);
					exhaustsAccelerationSourcesAccess = CelerationSourcesAccessInitialize(exhaustsAccelerationSources);
					exhaustsDecelerationSourcesAccess = CelerationSourcesAccessInitialize(exhaustsDecelerationSources);
					previousRPMDifferences = new(Allocator.Persistent);

					if (!vehicle.IsElectric)
					{
						exhaustBlowoutSources = new(vehicle, ToolkitSettings.ExhaustBlowoutClips.GetGroup(vehicle), exhaustsCenter);
						gearShiftingSources = new(vehicle, Settings.gearShiftingClips, Utility.Average(engineCenter, chassisBounds.center));
						NOSSources = new(vehicle, Settings.NOSClips, exhaustsCenter);

						if (vehicle.behaviour.IsTurbocharged)
						{
							turbochargerGearBlowout = VehicleAudio.NewAudioSource(vehicle, engineCenter, "TurbochargerGearBlowout", 1f, 5f, 0f, vehicle.Turbocharger.Audio.gearBlowoutClip, false, false, true, 0f, vehicle.audioMixers.Turbocharger);
							turbochargerFuelBlowout = VehicleAudio.NewAudioSource(vehicle, engineCenter, "TurbochargerFuelBlowout", 1f, 5f, 0f, vehicle.Turbocharger.Audio.fuelBlowoutClip, false, false, true, 0f, vehicle.audioMixers.Turbocharger);
							turbochargerRevBlowout = VehicleAudio.NewAudioSource(vehicle, engineCenter, "TurbochargerRevBlowout", 1f, 5f, 0f, vehicle.Turbocharger.Audio.revBlowoutClip, false, false, true, 0f, vehicle.audioMixers.Turbocharger);
							turbochargerIdle = VehicleAudio.NewAudioSource(vehicle, engineCenter, "TurbochargerIdleSFX", 1f, 3f, 0f, vehicle.Turbocharger.Audio.idleClip, true, true, false, 0f, vehicle.audioMixers.ChargersEffects);
							turbochargerActive = VehicleAudio.NewAudioSource(vehicle, engineCenter, "TurbochargerActiveSFX", 1f, 6f, 0f, vehicle.Turbocharger.Audio.activeClip, true, true, false, 0f, vehicle.audioMixers.ChargersEffects);
						}

						if (vehicle.behaviour.IsSupercharged)
						{
							superchargerGearBlowout = VehicleAudio.NewAudioSource(vehicle, engineCenter, "SuperchargerGearBlowout", 1f, 5f, 0f, vehicle.Supercharger.Audio.gearBlowoutClip, false, false, true, 0f, vehicle.audioMixers.Supercharger);
							superchargerFuelBlowout = VehicleAudio.NewAudioSource(vehicle, engineCenter, "SuperchargerFuelBlowout", 1f, 5f, 0f, vehicle.Supercharger.Audio.fuelBlowoutClip, false, false, true, 0f, vehicle.audioMixers.Supercharger);
							superchargerRevBlowout = VehicleAudio.NewAudioSource(vehicle, engineCenter, "SuperchargerRevBlowout", 1f, 5f, 0f, vehicle.Supercharger.Audio.revBlowoutClip, false, false, true, 0f, vehicle.audioMixers.Supercharger);
							superchargerIdle = VehicleAudio.NewAudioSource(vehicle, engineCenter, "SuperchargerIdleSFX", 1f, 3f, 0f, vehicle.Supercharger.Audio.idleClip, true, true, false, 0f, vehicle.audioMixers.ChargersEffects);
							superchargerActive = VehicleAudio.NewAudioSource(vehicle, engineCenter, "SuperchargerActiveSFX", 1f, 6f, 0f, vehicle.Supercharger.Audio.activeClip, true, true, false, 0f, vehicle.audioMixers.ChargersEffects);
						}
					}

					if (vehicle.transmission.ClipsGroupIndex > -1)
						transmissionWhiningSources = TransmissionSourcesInitialize(transmissionWhineClips, transmissionSFXParent, Utility.Average(engineCenter, chassisBounds.center), vehicle.audioMixers.Transmission);

					reversing = VehicleAudio.NewAudioSource(vehicle, Utility.Average(engineCenter, chassisBounds.center), "ReverseSFX", 1f, 10f, 0f, Settings.reversingClip, true, false, true, 0f, vehicle.audioMixers.Transmission);
				}

				indicator = VehicleAudio.NewAudioSource(vehicle, chassisBounds.center, "IndicatorSFX", .5f, 1f, 0f, Settings.indicatorClip, false, false, true, 0f, Settings.audioMixers.environmentalEffects);
				wind = VehicleAudio.NewAudioSource(vehicle, chassisBounds.center, "WindSFX", 1f, 10f, 0f, Settings.windClip, true, false, true, 0f, Settings.audioMixers.environmentalEffects);
			}

			#endregion

			#region Operators

			public static implicit operator bool(SFXSheet module) => module != null;

			#endregion
		}
		internal class VFXSheet
		{
			#region Jobs

#if !MVC_COMMUNITY
			[BurstCompile]
#endif
			private struct CollisionJob : IJob
			{
				#region Variables

				public NativeArray<ContactPoint> contacts;
				public NativeArray<VehicleVisuals.ContactPointAccess> output;

				#endregion

				#region Methods

				public void Execute()
				{
					VehicleVisuals.ContactPointAccess contact = new(contacts[0]);

					for (int i = 1; i < contacts.Length; i++)
					{
						contact.point += (float3)contacts[0].point;
						contact.normal += (float3)contacts[0].normal;
					}

					contact.point /= contacts.Length;
					contact.normal /= contacts.Length;

					output[0] = contact;
				}

				#endregion
			}

			#endregion

			#region Variables

			public readonly Dictionary<Transform, ParticleSystem> collisionSparks;
			public readonly ParticleSystem[] exhaustSmokes;
			public readonly ParticleSystem[] exhaustFlames;
			public readonly ParticleSystem[] NOSFlames;

			private readonly Vehicle vehicle;
			private readonly List<List<int>> flameLights;
			private readonly Light[] exhaustLights;
			private readonly float[] orgExhaustSmokesEmissionRates;
			private readonly Color exhaustFlameColor;
			private readonly Color NOSFlameColor;

			#endregion

			#region Methods

			#region Utilities

			public void EmitNOSFlames(bool emit)
			{
				if (HasInternalErrors || !Settings.useParticleSystems || vehicle.IsElectric || vehicle.Exhausts == null || vehicle.Exhausts.Length < 1 || !vehicle.behaviour.useNOS)
					return;

				for (int i = 0; i < NOSFlames.Length; i++)
				{
					if (!NOSFlames[i] || emit && NOSFlames[i].isPlaying || !emit && NOSFlames[i].isStopped)
						continue;

					if (emit && (!vehicle.exhaustHadJoint || vehicle.exhaustJoint))
						NOSFlames[i].Play();
					else
						NOSFlames[i].Stop();
				}
			}
			public void ShootExhaustFlames()
			{
				if (HasInternalErrors || !Settings.useParticleSystems || vehicle.IsElectric || !vehicle.behaviour.useExhaustEffects || vehicle.Engine.FuelType == VehicleEngine.EngineFuelType.Diesel || vehicle.Exhausts == null || vehicle.Exhausts.Length < 1 || vehicle.exhaustHadJoint && vehicle.exhaustJoint)
					return;

				for (int i = 0; i < exhaustFlames.Length; i++)
					if (exhaustFlames[i])
						exhaustFlames[i].Play();
			}
			public void CreateCollisionSpark(Collision collision)
			{
				if (collision == null || !collision.transform || !Settings.collisionSparks || collisionSparks.ContainsKey(collision.transform))
					return;

				NativeArray<ContactPoint> contacts = new(collision.contacts, Allocator.TempJob);
				NativeArray<VehicleVisuals.ContactPointAccess> contact = new(1, Allocator.TempJob);
				CollisionJob job = new()
				{
					contacts = contacts,
					output = contact
				};

				job.Schedule().Complete();
				contacts.Dispose();

				Quaternion contactRot = Quaternion.Inverse(Quaternion.LookRotation(collision.relativeVelocity.normalized, Vector3.up));
				ParticleSystem spark = VehicleVisuals.NewParticleSystem(vehicle, Settings.collisionSparks, "collision_sparks", contact[0].point, contactRot, true, true, true);

				collisionSparks.Add(collision.transform, spark);
				contact.Dispose();
			}
			public void DestroyCollisionSpark(Transform collisionTransform)
			{
				if (!collisionTransform || !collisionSparks.ContainsKey(collisionTransform))
					return;

				var sparks = collisionSparks[collisionTransform];

				if (sparks)
					vehicle.StartCoroutine(VehicleVisuals.DestroyParticleSystem(sparks, sparks.main.duration));

				collisionSparks.Remove(collisionTransform);
			}

			#endregion

			#region Update

			public void Update()
			{
				if (HasInternalErrors || !Settings.useParticleSystems)
					return;

				if (!vehicle.trailerInstance && !vehicle.IsElectric && vehicle.Exhausts != null && vehicle.Exhausts.Length > 0 && (!vehicle.exhaustHadJoint || vehicle.exhaustJoint))
				{
					int activeExhaustFlames = 0;
					int activeNOSFlames = 0;

					for (int i = 0; i < vehicle.Exhausts.Length; i++)
					{
						if (vehicle.Engine.FuelType != VehicleEngine.EngineFuelType.Diesel)
						{
							if (exhaustFlames[i] && exhaustFlames[i].isPlaying)
								activeExhaustFlames++;
						}
						else if (exhaustSmokes[i])
						{
							if (vehicle.stats.isEngineRunning || vehicle.stats.isEngineStarting)
							{
								if (!exhaustSmokes[i].isPlaying)
									exhaustSmokes[i].Play(true);

								ParticleSystem.EmissionModule emission = exhaustSmokes[i].emission;

								emission.rateOverTimeMultiplier = orgExhaustSmokesEmissionRates[i] * Utility.Clamp01((vehicle.stats.isEngineStarting ? Utility.Clamp01(UnityEngine.Random.Range(-3f, 1f)) : 0f) + Utility.Clamp01(vehicle.inputs.Fuel - (vehicle.stats.rawEnginePower / vehicle.Engine.Power)));
							}
							else if (exhaustSmokes[i].isPlaying)
								exhaustSmokes[i].Stop(true);
						}

						if (NOSFlames[i] && NOSFlames[i].isPlaying)
							activeNOSFlames++;
					}

					float exhaustColorFactor = (activeNOSFlames / NOSFlames.Length) - (vehicle.Engine.FuelType != VehicleEngine.EngineFuelType.Diesel ? activeExhaustFlames / exhaustFlames.Length : 0f) * .5f;

					for (int i = 0; i < exhaustLights.Length; i++)
					{
						if (!exhaustLights[i])
							continue;

						Color flameColor;

						if (vehicle.Engine.FuelType != VehicleEngine.EngineFuelType.Diesel)
						{
							float exhaustFlamesFactor = 0f;
							float NOSFlamesFactor = 0f;

							for (int j = 0; j < flameLights[i].Count; j++)
							{
								if (exhaustFlames[flameLights[i][j]])
									exhaustFlamesFactor += Utility.Clamp01(exhaustFlames[flameLights[i][j]].particleCount * 2f / exhaustFlames[flameLights[i][j]].main.maxParticles);

								if (NOSFlames[flameLights[i][j]])
									NOSFlamesFactor += Utility.Clamp01(NOSFlames[flameLights[i][j]].particleCount * 2f / NOSFlames[flameLights[i][j]].main.maxParticles);
							}

							exhaustFlamesFactor /= flameLights[i].Count;
							NOSFlamesFactor /= flameLights[i].Count;
							flameColor = Color.Lerp(exhaustFlameColor, NOSFlameColor, exhaustColorFactor);
							flameColor *= math.max(exhaustFlamesFactor, NOSFlamesFactor);
							flameColor.a = 1f;
						}
						else
							flameColor = NOSFlameColor * exhaustColorFactor;

						exhaustLights[i].color = flameColor;
					}
				}
			}

			#endregion

			#endregion

			#region Constructors

			public VFXSheet(Vehicle vehicle)
			{
				if (HasInternalErrors)
					return;

				this.vehicle = vehicle;

				if (!Settings.useParticleSystems)
					return;

				if (!vehicle.IsElectric && vehicle.Chassis && vehicle.Exhausts != null && vehicle.Exhausts.Length > 0)
				{
					if (!Settings.exhaustSmoke)
						ToolkitDebug.Warning($"We couldn't create the exhaust smoke particle{(vehicle.Exhausts.Length > 1 ? "s" : "")} for {vehicle.name}. The exhaust smoke particle system object seems to be missing!");
					else if (!Settings.exhaustFlame)
						ToolkitDebug.Warning($"We couldn't create the exhaust flame{(vehicle.Exhausts.Length > 1 ? "s" : "")} for {vehicle.name}. The exhaust flame particle system object seems to be missing!");
					else if (Settings.NOSFlameOverrideMode == ToolkitSettings.VFXOverrideMode.ParticleSystem && !Settings.NOSFlame)
						ToolkitDebug.Warning($"We couldn't create the exhaust flame{(vehicle.Exhausts.Length > 1 ? "s" : "")} for {vehicle.name}. The NOS flame particle system object seems to be missing!");
					else if (Settings.NOSFlameOverrideMode == ToolkitSettings.VFXOverrideMode.Material && !Settings.NOSFlameMaterial)
						ToolkitDebug.Warning($"We couldn't create the exhaust flame{(vehicle.Exhausts.Length > 1 ? "s" : "")} for {vehicle.name}. The NOS flame material seems to be missing!");
					else
					{
						orgExhaustSmokesEmissionRates = vehicle.Engine.FuelType == VehicleEngine.EngineFuelType.Diesel ? new float[vehicle.Exhausts.Length] : null;
						exhaustSmokes = vehicle.Engine.FuelType == VehicleEngine.EngineFuelType.Diesel ? new ParticleSystem[vehicle.Exhausts.Length] : null;
						exhaustFlames = vehicle.Engine.FuelType != VehicleEngine.EngineFuelType.Diesel ? new ParticleSystem[vehicle.Exhausts.Length] : null;
						NOSFlames = new ParticleSystem[vehicle.Exhausts.Length];

						for (int i = 0; i < vehicle.Exhausts.Length; i++)
						{
							Vector3 position = vehicle.Chassis.transform.TransformPoint(vehicle.Exhausts[i].localPosition);
							Quaternion rotation = vehicle.Chassis.transform.rotation * Quaternion.Euler(vehicle.Exhausts[i].localEulerAngles) * Quaternion.Euler(0f, 180f, 0f);
							Vector3 scale = vehicle.Exhausts[i].LocalScale;

							scale.z = Utility.Average(scale.x, scale.y);
							scale = Utility.Divide(scale, vehicle.Chassis.ExhaustModel ? vehicle.Chassis.ExhaustModel.lossyScale : Vector3.one);

							if (vehicle.Engine.FuelType == VehicleEngine.EngineFuelType.Diesel)
							{
								if (Settings.exhaustSmoke)
								{
									orgExhaustSmokesEmissionRates[i] = Settings.exhaustSmoke.emission.rateOverTimeMultiplier;

									if (vehicle.Chassis.ExhaustModel)
										exhaustSmokes[i] = VehicleVisuals.NewParticleSystem(vehicle.Chassis.ExhaustModel, Settings.exhaustSmoke, "ExhaustSmoke", position, rotation, false, false, true).GetComponent<ParticleSystem>();
									else
										exhaustSmokes[i] = VehicleVisuals.NewParticleSystem(vehicle, Settings.exhaustSmoke, "ExhaustSmoke", position, rotation, false, false, true).GetComponent<ParticleSystem>();

									exhaustSmokes[i].transform.localScale = scale;
								}
							}
							else if (Settings.exhaustFlame)
							{
								if (vehicle.Chassis.ExhaustModel)
									exhaustFlames[i] = VehicleVisuals.NewParticleSystem(vehicle.Chassis.ExhaustModel, Settings.exhaustFlame, "ExhaustFlame", position, rotation, false, false, true).GetComponent<ParticleSystem>();
								else
									exhaustFlames[i] = VehicleVisuals.NewParticleSystem(vehicle, Settings.exhaustFlame, "ExhaustFlame", position, rotation, false, false, true).GetComponent<ParticleSystem>();

								exhaustFlames[i].transform.localScale = scale;
							}

							if (Settings.NOSFlameOverrideMode == ToolkitSettings.VFXOverrideMode.Material)
							{
								if (Settings.NOSFlameMaterial)
								{
									if (vehicle.Chassis.ExhaustModel)
										NOSFlames[i] = VehicleVisuals.NewParticleSystem(vehicle.Chassis.ExhaustModel, Settings.exhaustFlame, "NOSFlame", position, rotation, true, false, true).GetComponent<ParticleSystem>();
									else
										NOSFlames[i] = VehicleVisuals.NewParticleSystem(vehicle, Settings.exhaustFlame, "NOSFlame", position, rotation, true, false, true).GetComponent<ParticleSystem>();

									NOSFlames[i].transform.localScale = scale;
									NOSFlames[i].GetComponent<ParticleSystemRenderer>().sharedMaterial = Settings.NOSFlameMaterial;
								}
							}
							else if (Settings.NOSFlame)
							{
								if (vehicle.Chassis.ExhaustModel)
									NOSFlames[i] = VehicleVisuals.NewParticleSystem(vehicle.Chassis.ExhaustModel, Settings.NOSFlame, "NOSFlame", position, rotation, true, false, true).GetComponent<ParticleSystem>();
								else
									NOSFlames[i] = VehicleVisuals.NewParticleSystem(vehicle, Settings.NOSFlame, "NOSFlame", position, rotation, true, false, true).GetComponent<ParticleSystem>();

								NOSFlames[i].transform.localScale = scale;
							}
						}

						if (Settings.exhaustFlame)
						{
							Material[] exhaustFlameMaterials = Settings.exhaustFlame.GetComponent<ParticleSystemRenderer>().sharedMaterials;

							if (exhaustFlameMaterials.Length > 0)
								exhaustFlameColor = exhaustFlameMaterials[0].color;
						}

						if (Settings.NOSFlameOverrideMode == ToolkitSettings.VFXOverrideMode.Material)
						{
							if (Settings.NOSFlameMaterial)
								NOSFlameColor = Settings.NOSFlameMaterial.color;
						}
						else if (Settings.NOSFlame)
						{
							Material NOSFlameMaterial = Settings.NOSFlame.GetComponent<ParticleSystemRenderer>().sharedMaterials.FirstOrDefault();

							if (NOSFlameMaterial)
								NOSFlameColor = NOSFlameMaterial.color;
						}

						if (Settings.exhaustFlameLight)
						{
							flameLights = new();

							List<Vector3> exhaustPositions = new();
							List<Vector3> exhaustRotations = new();

							for (int i = 0; i < vehicle.Exhausts.Length; i++)
							{
								int closestPosition = -1;

								if (exhaustPositions.Count < 1)
								{
									closestPosition = 0;

									exhaustPositions.Add(vehicle.Exhausts[i].localPosition);
									exhaustRotations.Add(vehicle.Exhausts[i].localEulerAngles);
								}
								else
								{
									for (int j = 0; j < exhaustPositions.Count; j++)
										if (Utility.Distance(vehicle.Exhausts[i].localPosition, exhaustPositions[j]) <= .25f)
											closestPosition = j;

									if (closestPosition > -1)
									{
										exhaustPositions[closestPosition] = Utility.Average(vehicle.Exhausts[i].localPosition, exhaustPositions[closestPosition]);
										exhaustRotations[closestPosition] = Utility.Average(vehicle.Exhausts[i].localEulerAngles, exhaustRotations[closestPosition]);
									}
									else
									{
										closestPosition = exhaustPositions.Count;

										exhaustPositions.Add(vehicle.Exhausts[i].localPosition);
										exhaustRotations.Add(vehicle.Exhausts[i].localEulerAngles);
									}
								}

								if (closestPosition > flameLights.Count - 1)
									flameLights.Add(new List<int> { i });
								else
									flameLights[closestPosition].Add(i);
							}

							vehicle.VFXParent = vehicle.Chassis.transform.Find("VisualEffects");

							if (!vehicle.VFXParent)
							{
								vehicle.VFXParent = new GameObject("VisualEffects").transform;
								vehicle.VFXParent.parent = vehicle.Chassis.transform;

								vehicle.VFXParent.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
							}

							if (Settings.useHideFlags)
								vehicle.VFXParent.hideFlags = HideFlags.HideInHierarchy;

							exhaustLights = new Light[flameLights.Count];

							for (int i = 0; i < flameLights.Count; i++)
							{
								exhaustPositions[i] = vehicle.Chassis.transform.TransformPoint(exhaustPositions[i]);
								exhaustLights[i] = Instantiate(Settings.exhaustFlameLight.gameObject, exhaustPositions[i], vehicle.transform.rotation * Quaternion.Euler(exhaustRotations[i]), vehicle.VFXParent).GetComponent<Light>();
							}
						}
					}
				}

				if (!Settings.collisionSparks)
					ToolkitDebug.Warning($"We couldn't create the damage sparks effect for {vehicle.name}. The damage sparks particle system object seems to be missing!");

				collisionSparks = new();
			}

			#endregion

			#region Operators

			public static implicit operator bool(VFXSheet module) => module != null;

			#endregion
		}

		#endregion

		#region Variables

		#region Components

		#region Wheels

		public Train Drivetrain
		{
			get
			{
				if (drivetrain == Train.None)
					RefreshWheels();

				return drivetrain;
			}
		}
		public Train Steertrain
		{
			get
			{
				if (steertrain == Train.None)
					RefreshWheels();

				return steertrain;
			}
		}
		public VehicleWheel.WheelModule[] Wheels
		{
			get
			{
				return wheels;
			}
			set
			{
				wheels = value;

				RefreshWheels();
			}
		}
		public VehicleWheel.WheelModule[] SteerWheels
		{
			get
			{
				if (steerWheels == null)
					RefreshWheels();

				return steerWheels;
			}
		}
		public VehicleWheel.WheelModule[] NonSteerWheels
		{
			get
			{
				if (nonSteerWheels == null)
					RefreshWheels();

				return nonSteerWheels;
			}
		}
		public VehicleWheel.WheelModule[] MotorWheels
		{
			get
			{
				if (motorWheels == null)
					RefreshWheels();

				return motorWheels;
			}
		}
		public VehicleWheel.WheelModule[] MotorLeftWheels
		{
			get
			{
				if (motorLeftWheels == null)
					RefreshWheels();

				return motorLeftWheels;
			}
		}
		public VehicleWheel.WheelModule[] MotorCenterWheels
		{
			get
			{
				if (motorCenterWheels == null)
					RefreshWheels();

				return motorCenterWheels;
			}
		}
		public VehicleWheel.WheelModule[] MotorRightWheels
		{
			get
			{
				if (motorRightWheels == null)
					RefreshWheels();

				return motorRightWheels;
			}
		}
		public VehicleWheel.WheelModule[] NonMotorWheels
		{
			get
			{
				if (nonMotorWheels == null)
					RefreshWheels();

				return nonMotorWheels;
			}
		}
		public VehicleWheel.WheelModule[] FrontWheels
		{
			get
			{
				if (frontWheels == null)
					RefreshWheels();

				return frontWheels;
			}
		}
		public VehicleWheel.WheelModule[] RearWheels
		{
			get
			{
				if (rearWheels == null)
					RefreshWheels();

				return rearWheels;
			}
		}
		public VehicleWheel.WheelModule[] LeftWheels
		{
			get
			{
				if (leftWheels == null)
					RefreshWheels();

				return leftWheels;
			}
		}
		public VehicleWheel.WheelModule[] RightWheels
		{
			get
			{
				if (rightWheels == null)
					RefreshWheels();

				return rightWheels;
			}
		}
		public VehicleWheel.WheelModule[] CenterWheels
		{
			get
			{
				if (centerWheels == null)
					RefreshWheels();

				return centerWheels;
			}
		}
		public VehicleWheel.WheelModule[] FrontLeftWheels
		{
			get
			{
				if (frontLeftWheels == null)
					RefreshWheels();

				return frontLeftWheels;
			}
		}
		public VehicleWheel.WheelModule[] FrontCenterWheels
		{
			get
			{
				if (frontCenterWheels == null)
					RefreshWheels();

				return frontCenterWheels;
			}
		}
		public VehicleWheel.WheelModule[] FrontRightWheels
		{
			get
			{
				if (frontRightWheels == null)
					RefreshWheels();

				return frontRightWheels;
			}
		}
		public VehicleWheel.WheelModule[] RearLeftWheels
		{
			get
			{
				if (rearLeftWheels == null)
					RefreshWheels();

				return rearLeftWheels;
			}
		}
		public VehicleWheel.WheelModule[] RearCenterWheels
		{
			get
			{
				if (rearCenterWheels == null)
					RefreshWheels();

				return rearCenterWheels;
			}
		}
		public VehicleWheel.WheelModule[] RearRightWheels
		{
			get
			{
				if (rearRightWheels == null)
					RefreshWheels();

				return rearRightWheels;
			}
		}
		public VehicleWheelSpin[] WheelSpins
		{
			get
			{
				if (wheelSpins == null)
					RefreshWheels();

				return wheelSpins;
			}
		}

		public Vector3 WheelsPosition
		{
			get
			{
				return transform.TransformPoint(wheelsLocalPosition);
			}
		}
		public Vector3 WheelsLocalPosition => wheelsLocalPosition;
		public Vector3 FrontWheelsPosition
		{
			get
			{
				return transform.TransformPoint(frontWheelsLocalPosition);
			}
		}
		public Vector3 FrontWheelsLocalPosition => frontWheelsLocalPosition;
		public Vector3 RearWheelsPosition
		{
			get
			{
				return transform.TransformPoint(rearWheelsLocalPosition);
			}
		}
		public Vector3 RearWheelsLocalPosition => rearWheelsLocalPosition;
		public Vector3 LeftWheelsPosition
		{
			get
			{
				return transform.TransformPoint(leftWheelsLocalPosition);
			}
		}
		public Vector3 LeftWheelsLocalPosition => leftWheelsLocalPosition;
		public Vector3 CenterWheelsPosition
		{
			get
			{
				return transform.TransformPoint(centerWheelsLocalPosition);
			}
		}
		public Vector3 CenterWheelsLocalPosition => centerWheelsLocalPosition;
		public Vector3 RightWheelsPosition
		{
			get
			{
				return transform.TransformPoint(rightWheelsLocalPosition);
			}
		}
		public Vector3 RightWheelsLocalPosition => rightWheelsLocalPosition;
		public Vector3 FrontLeftWheelsPosition
		{
			get
			{
				return transform.TransformPoint(frontLeftWheelsLocalPosition);
			}
		}
		public Vector3 FrontLeftWheelsLocalPosition => frontLeftWheelsLocalPosition;
		public Vector3 FrontCenterWheelsPosition
		{
			get
			{
				return transform.TransformPoint(frontCenterWheelsLocalPosition);
			}
		}
		public Vector3 FrontCenterWheelsLocalPosition => frontCenterWheelsLocalPosition;
		public Vector3 FrontRightWheelsPosition
		{
			get
			{
				return transform.TransformPoint(frontRightWheelsLocalPosition);
			}
		}
		public Vector3 FrontRightWheelsLocalPosition => frontRightWheelsLocalPosition;
		public Vector3 RearLeftWheelsPosition
		{
			get
			{
				return transform.TransformPoint(rearLeftWheelsLocalPosition);
			}
		}
		public Vector3 RearLeftWheelsLocalPosition => rearLeftWheelsLocalPosition;
		public Vector3 RearCenterWheelsPosition
		{
			get
			{
				return transform.TransformPoint(rearCenterWheelsLocalPosition);
			}
		}
		public Vector3 RearCenterWheelsLocalPosition => rearCenterWheelsLocalPosition;
		public Vector3 RearRightWheelsPosition
		{
			get
			{
				return transform.TransformPoint(rearRightWheelsLocalPosition);
			}
		}
		public Vector3 RearRightWheelsLocalPosition => rearRightWheelsLocalPosition;

		private Train drivetrain;
		private Train steertrain;
		[SerializeField]
		private VehicleWheel.WheelModule[] wheels = new VehicleWheel.WheelModule[] { };
		private VehicleWheel.WheelModule[] steerWheels = null;
		private VehicleWheel.WheelModule[] nonSteerWheels = null;
		private VehicleWheel.WheelModule[] motorWheels = null;
		private VehicleWheel.WheelModule[] motorLeftWheels = null;
		private VehicleWheel.WheelModule[] motorCenterWheels = null;
		private VehicleWheel.WheelModule[] motorRightWheels = null;
		private VehicleWheel.WheelModule[] nonMotorWheels = null;
		private VehicleWheel.WheelModule[] frontWheels = null;
		private VehicleWheel.WheelModule[] rearWheels = null;
		private VehicleWheel.WheelModule[] leftWheels = null;
		private VehicleWheel.WheelModule[] rightWheels = null;
		private VehicleWheel.WheelModule[] centerWheels = null;
		private VehicleWheel.WheelModule[] frontLeftWheels = null;
		private VehicleWheel.WheelModule[] frontCenterWheels = null;
		private VehicleWheel.WheelModule[] frontRightWheels = null;
		private VehicleWheel.WheelModule[] rearLeftWheels = null;
		private VehicleWheel.WheelModule[] rearCenterWheels = null;
		private VehicleWheel.WheelModule[] rearRightWheels = null;
		private VehicleWheelSpin[] wheelSpins = null;
		private Vector3 wheelsLocalPosition;
		private Vector3 frontWheelsLocalPosition;
		private Vector3 rearWheelsLocalPosition;
		private Vector3 leftWheelsLocalPosition;
		private Vector3 centerWheelsLocalPosition;
		private Vector3 rightWheelsLocalPosition;
		private Vector3 frontLeftWheelsLocalPosition;
		private Vector3 frontCenterWheelsLocalPosition;
		private Vector3 frontRightWheelsLocalPosition;
		private Vector3 rearLeftWheelsLocalPosition;
		private Vector3 rearCenterWheelsLocalPosition;
		private Vector3 rearRightWheelsLocalPosition;

		#endregion

		public Rigidbody Rigidbody
		{
			get
			{
				return rigidbody;
			}
		}
		public VehicleChassis Chassis
		{
			get
			{
				return chassis;
			}
			set
			{
				if (!value || !value.transform.IsChildOf(transform))
					return;

				chassis = value;
			}
		}
		public VehicleEngine Engine
		{
			get
			{
				if (IsTrailer)
					return null;

				if (!engine || engine != Behaviour.Engine)
					engine = Behaviour.Engine;

				return engine;
			}
		}
		public VehicleCharger Turbocharger
		{
			get
			{
				if (IsTrailer || !Behaviour.IsTurbocharged)
					return null;

				if (!turbocharger || turbocharger != behaviour.Turbocharger)
					turbocharger = Behaviour.Turbocharger;

				return turbocharger;
			}
		}
		public VehicleCharger Supercharger
		{
			get
			{
				if (IsTrailer || !behaviour.IsSupercharged)
					return null;

				if (!supercharger || supercharger != behaviour.Supercharger)
					supercharger = Behaviour.Supercharger;

				return supercharger;
			}
		}
		public VehicleExhaust[] Exhausts
		{
			get
			{
				if (IsTrailer)
					return null;

				exhausts ??= new VehicleExhaust[] { };

				return exhausts;
			}
			set
			{
				if (IsTrailer)
					return;

				exhausts = value;
			}
		}
		public VehicleLight[] Lights
		{
			get
			{
				lights ??= new VehicleLight[] { };

				return lights;
			}
			set
			{
				lights = value;
			}
		}
		[Obsolete("Use `AIBehaviours` instead.", true)]
		public VehicleAIBehaviour AI
		{
			get
			{
				return AIBehaviours.FirstOrDefault();
			}
		}
		public VehicleAIBehaviour[] AIBehaviours
		{
			get
			{
#if MVC_COMMUNITY
				return null;
#else
				if (IsTrailer)
					return null;

#if UNITY_EDITOR
				if (aiBehaviours == null || aiBehaviours.Length < 1 || !Application.isPlaying)
					RefreshAIBehaviours();
#endif

				return aiBehaviours;
#endif
			}
		}
		public VehicleAIBehaviour ActiveAIBehaviour
		{
			get
			{
				if (activeAIBehaviourIndex < 0)
				{
					isAIActive = false;

					return null;
				}

				VehicleAIBehaviour behaviour = AIBehaviours[activeAIBehaviourIndex];

				isAIActive = behaviour;

				return behaviour;
			}
		}
		public VehicleTrailerLink TrailerLink
		{
			get
			{
				if (!trailerLink && !Application.isPlaying)
					trailerLink = GetComponent<VehicleTrailerLink>();

				return trailerLink;
			}
		}

		internal VehicleTrailer trailerInstance;

		[SerializeField]
		private new Rigidbody rigidbody;
		[SerializeField]
		private VehicleChassis chassis;
		private VehicleEngine engine;
		private VehicleCharger turbocharger;
		private VehicleCharger supercharger;
		private VehicleDifferential frontDifferential;
		private VehicleDifferential centerDifferential;
		private VehicleDifferential rearDifferential;
		[SerializeField]
		private VehicleExhaust[] exhausts;
		[SerializeField]
		private VehicleLight[] lights;
		[SerializeField]
		private VehicleAIBehaviour[] aiBehaviours;
		private bool isAIActive;
		private bool isAI;
		private VehicleTrailerLink trailerLink;
		private Transform SFXParent;
		private Transform VFXParent;
		private HingeJoint exhaustJoint;
		private bool exhaustHadJoint;

		#endregion

		#region Modules & Sheets

		#region Modules

		public BehaviourModule Behaviour
		{
			get
			{
				if (IsTrailer)
					return null;
				else if (!behaviour || !behaviour.IsValid)
					behaviour = new(this);

				return behaviour;
			}
		}
		public TransmissionModule Transmission
		{
			get
			{
				if (IsTrailer)
					return null;
				else if (!transmission || !transmission.IsValid)
					transmission = new(this);

				return transmission;
			}
		}
		public SteeringModule Steering
		{
			get
			{
				if (IsTrailer)
					return null;
				else if (!steering || !steering.IsValid)
					steering = new(this);

				return steering;
			}
		}
		public VehicleWheel.SuspensionModule FrontSuspension
		{
			get
			{
				if (IsTrailer)
					return null;
				else if (!frontSuspension)
					frontSuspension = new(this);
				else if (!frontSuspension.IsValid)
					frontSuspension = new(this, frontSuspension);

				return frontSuspension;
			}
		}
		public VehicleWheel.SuspensionModule RearSuspension
		{
			get
			{
				if (!rearSuspension)
					rearSuspension = new(this);
				else if (!rearSuspension.IsValid)
					rearSuspension = new(this, rearSuspension);

				return rearSuspension;
			}
		}
		public StabilityModule Stability
		{
			get
			{
				if (!stability)
					stability = new(this);
				else if (!stability.IsValid)
					stability = new(this, stability);

				return stability;
			}
		}
		/*public DamageModule Damage
		{
			get
			{
				if (!damage || !damage.IsValid)
					damage = new(this);

				return damage;
			}
			set
			{
				damage = value;
			}
		}*/
		public InteriorModule Interior
		{
			get
			{
				if (IsTrailer)
					return null;
				else if (!interior || !interior.IsValid)
					interior = new(this);

				return interior;
			}
		}
		public AudioMixersModule AudioMixers
		{
			get
			{
				if (IsTrailer)
					return null;
				else if (!audioMixers || !audioMixers.IsValid)
					audioMixers = new(this);

				return audioMixers;
			}
		}
		public DriverIKModule DriverIK
		{
			get
			{
				if (IsTrailer)
					return null;
				else if (!driverIK || !driverIK.IsValid)
					driverIK = new(this);

				return driverIK;
			}
		}

		[SerializeField]
		private BehaviourModule behaviour;
		private BrakeModule frontBrakes;
		private BrakeModule rearBrakes;
		[SerializeField]
		private TransmissionModule transmission;
		[SerializeField]
		private SteeringModule steering;
		[SerializeField]
		private VehicleWheel.SuspensionModule frontSuspension;
		[SerializeField]
		private VehicleWheel.SuspensionModule rearSuspension;
		[SerializeField]
		private StabilityModule stability;
		/*[SerializeField]
		private DamageModule damage;*/
		[SerializeField]
		private InteriorModule interior;
		[SerializeField]
		private AudioMixersModule audioMixers;
		[SerializeField]
		private DriverIKModule driverIK;
		[SerializeField]
		private int activeAIBehaviourIndex;

		#endregion

		#region Sheets

		public InputsAccess Inputs
		{
			get
			{
				if (this is VehicleTrailer trailer && trailer.Joint.ConnectedVehicle)
					inputs = trailer.Joint.ConnectedVehicle.Inputs;

				return inputs;
			}
		}
		public ProblemsAccess Problems
		{
			get
			{
				if (!problems.IsSheetValid)
					problems = new(this);

				return problems;
			}
		}
		public StatsAccess Stats => stats;

		internal SFXSheet sfx;
		internal VFXSheet vfx;

		private InputsAccess inputs;
		private ProblemsAccess problems;
		private StatsAccess stats;

		#endregion

		#endregion

		#region Stats & Temp

		public Bounds Bounds
		{
			get
			{
				if (bounds == default)
					RefreshBounds();

				return bounds;
			}
		}
		public Bounds WorldBounds
		{
			get
			{
				if (worldBounds == default)
					RefreshBounds();

				worldBounds.center = transform.TransformPoint(bounds.center);

				return worldBounds;
			}
		}
		public Bounds ChassisBounds
		{
			get
			{
				if (chassisBounds == default || chassisBounds == bounds)
					RefreshBounds();

				return chassisBounds;
			}
		}
		public Bounds ChassisWorldBounds
		{
			get
			{
				if (chassisWorldBounds == default || chassisWorldBounds == chassisBounds || chassisWorldBounds == worldBounds)
					RefreshBounds();

				worldBounds.center = transform.TransformPoint(chassisBounds.center);

				return chassisWorldBounds;
			}
		}
		public float ChassisWeight => IsTrailer ? trailerInstance.Behaviour.CurbWeight : Behaviour.CurbWeight;
		public float WheelsWeight => Wheels.Where(wheel => wheel.Instance).Sum(wheel => wheel.Instance.mass);
		public float AdditionalWeight => HasInternalErrors ? default : (Settings.useWheelsMass ? WheelsWeight : default) + (Settings.useEngineMass && !IsTrailer && Engine ? Engine.Mass : default);
		public float TopSpeed
		{
			get
			{
				if (topSpeed == default && !IsTrailer)
					topSpeed = Transmission.AutoGearRatios ? Behaviour.TopSpeed : transmission.GetGearShiftOverrideSpeed(transmission.GearsCount - 1, out float newTopSpeed) ? newTopSpeed : transmission.GetSpeedTarget(0, transmission.GearsCount - 1);

				return topSpeed;
			}
		}
		public bool IsTrailer
		{
			get
			{
				if (!trailerInstance)
					trailerInstance = this as VehicleTrailer;

				return trailerInstance;
			}
		}
		public bool IsElectric
		{
			get
			{
				return !IsTrailer && (Behaviour.vehicleType == VehicleType.Car && behaviour.carClass == CarClass.Electric || behaviour.vehicleType == VehicleType.HeavyTruck && behaviour.heavyTruckClass == HeavyTruckClass.Electric) || trailerInstance && trailerInstance.Joint.ConnectedVehicle && trailerInstance.Joint.ConnectedVehicle.IsElectric;
			}
		}
		public bool IsPrefab
		{
			get
			{
				return gameObject.scene.name.IsNullOrEmpty();
			}
		}
		public string CurrentGearToString => currentGearToString;

		public float WheelBase => wheelBase;
		public float FrontTrack => frontTrack;
		public float RearTrack => rearTrack;
		public bool CanFrontSteer => canFrontSteer;
		public bool CanFrontMotor => canFrontMotor;
		public bool CanRearSteer => canRearSteer;
		public bool CanRearMotor => canRearMotor;
		public bool IsFrontMotorAxleBalanced => isFrontMotorAxleBalanced;
		public bool IsFrontSteerAxleBalanced => isFrontSteerAxleBalanced;
		public bool IsRearMotorAxleBalanced => isRearMotorAxleBalanced;
		public bool IsRearSteerAxleBalanced => isRearSteerAxleBalanced;
		public bool IsVerticalAxleBalanced => isVerticalAxleBalanced;
		public bool HasFrontWheels => hasFrontWheels;
		public bool HasRearWheels => hasRearWheels;
		public bool IsAllBalanced => isAllBalanced;
		public int FrontSteerWheelsCount => frontSteerWheelsCount;
		public int FrontCenterSteerWheelsCount => frontCenterSteerWheelsCount;
		public int RearSteerWheelsCount => rearSteerWheelsCount;
		public int RearCenterSteerWheelsCount => rearCenterSteerWheelsCount;
		public int FrontMotorWheelsCount => frontMotorWheelsCount;
		public int FrontCenterMotorWheelsCount => frontCenterMotorWheelsCount;
		public int RearMotorWheelsCount => rearMotorWheelsCount;
		public int RearCenterMotorWheelsCount => rearCenterMotorWheelsCount;
		public bool IsAI => isAI = AIBehaviours != null && aiBehaviours.Length > 0;
		public int ActiveAIBehaviourIndex
		{
			get
			{
				return activeAIBehaviourIndex;
			}
			set
			{
				RefreshAIBehaviours();

				if (value >= aiBehaviours.Length)
					throw new IndexOutOfRangeException();

				activeAIBehaviourIndex = math.max(value, -1);
				isAIActive = activeAIBehaviourIndex > -1 && aiBehaviours[activeAIBehaviourIndex];
			}
		}

		internal float burnoutIntensity;
		internal bool isBodyVisible;

		private NativeArray<VehicleGroundMapper.GroundAccess> grounds;
		private NativeArray<VehicleWheel.WheelAccess> wheelsAccess;
		private NativeArray<VehicleWheel.WheelStatsAccess> wheelStats;
#if !MVC_COMMUNITY
		private NativeArray<int> groundsAppearance;
#endif
		private VehicleEngineAccess engineAccess;
		private BehaviourAccess behaviourAccess;
		private TransmissionAccess transmissionAccess;
		private Renderer[] vehicleRenderers;
		//private Collider[] vehicleColliders;
		private List<VehicleWeatherZone> darknessWeatherZones;
		private List<VehicleWeatherZone> fogWeatherZones;
		private Quaternion steerDirection;
		private Quaternion velocityDirection;
		private Bounds bounds;
		private Bounds worldBounds;
		private Bounds chassisBounds;
		private Bounds chassisWorldBounds;
		private Utility.Interval2 COMInterval;
		private Coroutine gearboxShiftCoroutine;
		private Vector3 ackermannIntersectionPoint;
		private Vector3 ackermannP1;
		private Vector3 ackermannP2;
		private Vector3 ackermannP3;
		private Vector3 ackermannP4;
		private string currentGearToString;
		private float wheelBase;
		private float frontTrack;
		private float rearTrack;
		private bool canFrontSteer;
		private bool canFrontMotor;
		private bool canRearSteer;
		private bool canRearMotor;
		private bool isFrontMotorAxleBalanced;
		private bool isFrontSteerAxleBalanced;
		private bool isRearMotorAxleBalanced;
		private bool isRearSteerAxleBalanced;
		private bool isVerticalAxleBalanced;
		private bool hasFrontWheels;
		private bool hasRearWheels;
		private bool isAllBalanced;
		private int frontSteerWheelsCount;
		private int frontCenterSteerWheelsCount;
		private int rearSteerWheelsCount;
		private int rearCenterSteerWheelsCount;
		private int frontMotorWheelsCount;
		private int frontCenterMotorWheelsCount;
		private int rearMotorWheelsCount;
		private int rearCenterMotorWheelsCount;
		private float topSpeed;
		private float autoShutDownTimer;
		private float engineBoost;
		private float NOSRegenerateTimer;
		private float gearShiftRPMFactor;
		private float steeringWheel;
		private float ackermannSteerAngle;
		private float steerAngle;
		private float turnRadius;
		private float weightDistribution;
		private float weightHeight;
		private float oldYEulerAngle;
		private float resetTimer;
		private float resetMeshTimer;
		private float sideSignalLightsTimer;
		private float donutIntensity;
		private float donutDirection;
		private float orgAverageMotorWheelsFrictionStiffnessSqr;
		private bool requestGearShiftUp;
		private bool requestGearShiftDown;
		private bool requestGearShiftForward;
		private bool requestGearShiftNeutral;
		private bool requestGearShiftReverse;
		private bool canShiftDown;
		private bool isNOSGettingReady;
		private bool isNOSReady;
		private bool isOneShotNOSActive;
		private bool switchHighBeamHeadlightsButtonDoublePressed;
		private bool isSignalLeftLightsOn;
		private bool isSignalRightLightsOn;
		private int lastActiveAIBehaviourIndex = -1;

		#endregion

		#endregion

		#region Methods

		#region Virtual Methods

		internal virtual void OnDrawGizmosSelected()
		{
			if (!IsSetupDone || HasInternalErrors)
				return;

			if (!Application.isPlaying)
				WeightDistribution();

			if (!Chassis)
				return;

			Color orgGizmosColor = Gizmos.color;

			if (!Application.isPlaying)
			{
				Gizmos.color = Settings.exhaustGizmoColor;

				if (!IsTrailer && !Chassis.transform.Find("VisualEffects"))
					for (int i = 0; i < Exhausts.Length; i++)
						Gizmos.DrawSphere(Chassis.transform.TransformPoint(Exhausts[i].localPosition), Settings.gizmosSize * Utility.Average(Exhausts[i].LocalScale.x, Exhausts[i].LocalScale.y) / 32f);

				for (int i = 0; i < Lights.Length; i++)
				{
					if (Lights[i].IsHeadlight)
						Gizmos.color = Settings.headlightGizmoColor;
#if !MVC_COMMUNITY
					else if (Lights[i].IsInteriorLight)
						Gizmos.color = Settings.interiorLightGizmoColor;
#endif
					else if (Lights[i].IsSideSignalLight)
						Gizmos.color = Settings.sideSignalLightGizmoColor;
					else if (Lights[i].IsRearLight)
						Gizmos.color = Settings.rearLightGizmoColor;

					Vector3 position = Lights[i].GetSourcePosition();

					if (position != Vector3.zero)
						Gizmos.DrawSphere(position, Settings.gizmosSize / 32f);
				}
			}

			Gizmos.color = Settings.COMGizmoColor;

			Vector3 chassisCenter = transform.InverseTransformPoint(Chassis.transform.TransformPoint(ChassisBounds.center));
			float yCOM = Utility.Lerp(chassisCenter.y - ChassisBounds.extents.y, chassisCenter.y + ChassisBounds.extents.y, weightHeight);
			float zCOM = Utility.Lerp(IsTrailer ? chassisCenter.z + ChassisBounds.extents.z : frontWheelsLocalPosition.z, trailerInstance ? chassisCenter.z - ChassisBounds.extents.z : rearWheelsLocalPosition.z, weightDistribution);

			Gizmos.DrawSphere(transform.TransformPoint(new(0f, yCOM, zCOM)), Settings.gizmosSize / 32f);

			if (TrailerLink)
			{
				Gizmos.color = Settings.jointsGizmoColor;

				Vector3 linkPosition = transform.TransformPoint(trailerLink.LinkPoint);

				Gizmos.DrawSphere(linkPosition, math.min(Settings.gizmosSize / 16f, trailerLink.LinkRadius * .5f));
				Gizmos.DrawWireSphere(linkPosition, trailerLink.LinkRadius);
			}

			Gizmos.color = orgGizmosColor;
		}

		#endregion

		#region Global Methods

		#region Awake

		public void Restart()
		{
			Awaken = false;

			if (!IsSetupDone)
				return;

			GetOrCreateRigidbody();

			if (Problems.DisableVehicleOnInternalErrors() || Settings.Problems.DisableToolkitBehaviourOnProblems(ToolkitSettings.SettingsEditorFoldout.General, this) || Settings.Problems.DisableToolkitBehaviourOnProblems(ToolkitSettings.SettingsEditorFoldout.PlayerInputs, this) || Settings.Problems.DisableToolkitBehaviourOnProblems(ToolkitSettings.SettingsEditorFoldout.VFX, this))
				return;
			else if (!TryRestart())
				return;

			if (Lights.Length > 0)
				for (int i = 0; i < Lights.Length; i++)
					Lights[i].Restart();

			Chassis.Restart();
			RestartAlone();

			foreach (var ai in aiBehaviours)
			{
				ai.Restart();
				ai.OnStart();
			}

			if (TrailerLink)
				TrailerLink.Restart();

			Awaken = false;

			for (int i = 0; i < wheels.Length; i++)
				if (wheels[i] && wheels[i].Instance)
					wheels[i].Instance.Restart();

#if !MVC_COMMUNITY
			for (int i = 0; i < WheelSpins.Length; i++)
				WheelSpins[i].Restart();
#endif

#if !MVC_COMMUNITY
			if (!trailerInstance && Settings.useInterior)
				Interior.Restart();
#endif

			if (Problems.DisableVehicleOnWarnings() || Problems.DisableVehicleOnErrors())
				return;

			Awaken = true;

			if (Problems.HasIssues)
				ToolkitDebug.Log($"The vehicle '{name}' might have some issues that need to be fixed, for some features are currently unavailable unless they're fixed!", this);
		}
		public void RestartAlone()
		{
			Awaken = false;

			Initialize();

			if (Problems.DisableVehicleOnInternalErrors() || Settings.Problems.DisableToolkitBehaviourOnProblems(ToolkitSettings.SettingsEditorFoldout.General, this) || Settings.Problems.DisableToolkitBehaviourOnProblems(ToolkitSettings.SettingsEditorFoldout.VFX, this))
				return;
			else if (!TryRestart())
				return;

			transform.localScale = Vector3.one;

			//Damage.LoadData();
			RefreshWheels();
			RefreshAIBehaviours();
			behaviourAccess.Dispose();
			transmissionAccess.Dispose();

			behaviourAccess = Behaviour;
			transmissionAccess = Transmission;
			engineAccess = Engine;

			trailerLink = GetComponent<VehicleTrailerLink>();
			trailerInstance = this as VehicleTrailer;
			frontBrakes = behaviour ? behaviour.FrontBrakes : null;
			rearBrakes = behaviour ? behaviour.RearBrakes : null;

			if (trailerInstance)
				trailerInstance.Restart();
			else
			{
				frontDifferential = transmission ? transmission.FrontDifferential : null;
				centerDifferential = transmission ? transmission.CenterDifferential : null;
				rearDifferential = transmission ? transmission.RearDifferential : null;

				frontDifferential?.Initialize();
				centerDifferential?.Initialize();
				rearDifferential?.Initialize();
				transmission?.GetGearShiftOverrideSpeed(0, out _);
			}

			if (Chassis && Chassis.ExhaustModel && !trailerInstance && !IsElectric)
				exhaustJoint = Chassis.ExhaustModel.GetComponent<HingeJoint>();

			sfx?.Dispose();

			exhaustHadJoint = exhaustJoint;
			audioMixers = AudioMixers;
			sfx = new(this);
			vfx = new(this);
			stats = new(this)
			{
				NOS = !trailerInstance && behaviour.useNOS ? behaviour.NOSCapacity * behaviour.NOSBottlesCount : 0f,
				bottleNOS = trailerInstance ? 0f : stats.NOS / behaviour.NOSBottlesCount,
				fuelTank = !trailerInstance ? behaviour.FuelCapacity : 0f
			};
			darknessWeatherZones = new();
			fogWeatherZones = new();

			if (MotorWheels.Length > 0)
				orgAverageMotorWheelsFrictionStiffnessSqr = motorWheels.Average(wheel =>
				{
					var tireCompound = wheel.Instance.TireCompound;

					if (!tireCompound)
						return 1f;

					float frictionStiffnessSqr = tireCompound.wheelColliderAccelerationFriction.GetStiffness(0f) * tireCompound.wheelColliderSidewaysFriction.GetStiffness(0f);

					return tireCompound.GetStiffness(frictionStiffnessSqr, wheel.Instance.Width, 0f);
				});

			WeightDistribution();
			RefreshLayersAndTags();

#if !MVC_COMMUNITY
			if (Interior && Settings.useInterior)
			{
				interior.IndicatorLeft.RefreshMaterialEmissionColorPropertyName();
				interior.IndicatorRight.RefreshMaterialEmissionColorPropertyName();
				interior.Handbrake.RefreshMaterialEmissionColorPropertyName();
			}
#endif

			if (Problems.DisableVehicleOnWarnings() || Problems.DisableVehicleOnErrors())
				return;

			if (grounds.IsCreated)
				grounds.Dispose();

			grounds = new(Settings.Grounds.Select(ground => (VehicleGroundMapper.GroundAccess)ground).ToArray(), Allocator.Persistent);

			if (wheelsAccess.IsCreated)
				wheelsAccess.Dispose();

			wheelsAccess = new(wheels.Select(wheel => (VehicleWheel.WheelAccess)wheel).ToArray(), Allocator.Persistent);

			RequestChangeGear(-1, true);
			RefreshGearShiftRPMFactor();

			if ((Settings.engineStartMode == ToolkitSettings.EngineStartMode.Always || isAI) && !trailerInstance)
				stats.isEngineRunning = true;

			Awaken = true;

			if (Manager.autoRefreshVehicles)
				Manager.RefreshVehicles();

			if (Manager.autoRefreshPlayer)
				Manager.RefreshPlayer();
		}

		private void Awake()
		{
			if (Awaken)
				return;

			Restart();
		}
		private bool TryRestart()
		{
			if (HasInternalErrors || !IsSetupDone || Settings.Problems.DisableToolkitBehaviourOnProblems(ToolkitSettings.SettingsEditorFoldout.General, this))
				return false;
			else if (!Manager)
			{
				if (!VehicleManager.GetOrCreateInstance())
					return false;

				ToolkitDebug.Log("The Vehicle Manager couldn't be found! The Vehicle behaviour created a new one.", Manager);

				return false;
			}
			else if (!Manager.enabled)
			{
				ToolkitDebug.Warning("The Vehicle Manager seems to be disabled! Therefore some features aren't going to work as expected.", Manager);

				return false;
			}

			return true;
		}
		private void Initialize()
		{
			GetOrCreateRigidbody();

			if (SFXParent)
				Utility.Destroy(true, SFXParent.gameObject);

			if (VFXParent)
				Utility.Destroy(true, VFXParent.gameObject);

			darknessWeatherZones?.Clear();
			fogWeatherZones?.Clear();

			trailerInstance = null;
			aiBehaviours = null;
			trailerLink = null;
			exhaustJoint = null;
			exhaustHadJoint = default;
			frontBrakes = null;
			rearBrakes = null;
			inputs = default;
			problems = default;
			sfx = null;
			vfx = null;
			stats = default;
			vehicleRenderers = null;
			darknessWeatherZones = null;
			fogWeatherZones = null;
			steerDirection = default;
			velocityDirection = default;
			autoShutDownTimer = default;
			weightDistribution = default;
			orgAverageMotorWheelsFrictionStiffnessSqr = default;
			gearShiftRPMFactor = default;
			NOSRegenerateTimer = default;
			canShiftDown = true;
			burnoutIntensity = default;
			isBodyVisible = true;
			lastActiveAIBehaviourIndex = -1;
		}

		#endregion

		#region Utilities

		public Rigidbody GetOrCreateRigidbody()
		{
			if (HasInternalErrors)
				return null;

			if (!TryGetComponent(out rigidbody))
			{
				rigidbody = gameObject.AddComponent<Rigidbody>();
				rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			}

			trailerInstance = this as VehicleTrailer;
			rigidbody.useGravity = true;
			rigidbody.isKinematic = false;
			rigidbody.mass = trailerInstance ? trailerInstance.Behaviour.CurbWeight : Behaviour.CurbWeight;
			rigidbody.
#if UNITY_6000_0_OR_NEWER
				linearDamping
#else
				drag
#endif
				= Stability.Drag;
			rigidbody.
#if UNITY_6000_0_OR_NEWER
				angularDamping
#else
				angularDrag
#endif
				= stability.AngularDrag * stability.DragScale;
			rigidbody.
#if UNITY_6000_0_OR_NEWER
				linearVelocity
#else
				velocity
#endif
				= Vector3.zero;
			rigidbody.angularVelocity = Vector3.zero;

			if (Settings.useHideFlags)
				rigidbody.hideFlags = HideFlags.HideInInspector;

			return rigidbody;
		}
		public void RefreshLayersAndTags()
		{
			if (HasInternalErrors)
				return;

			if (gameObject.layer != Settings.vehiclesLayer)
				gameObject.layer = Settings.vehiclesLayer;

			if (!isAI && !gameObject.CompareTag(Settings.playerVehicleTag))
				gameObject.tag = Settings.playerVehicleTag;

			Transform[] transforms = GetComponentsInChildren<Transform>(true);

			foreach (Transform transform in transforms)
				if (transform.gameObject.layer != Settings.vehiclesLayer && transform.gameObject.layer != LayerMask.NameToLayer("UI"))
					transform.gameObject.layer = Settings.vehiclesLayer;

			foreach (VehicleWheel.WheelModule wheel in Wheels)
			{
				if (!wheel.Model)
					continue;

				transforms = wheel.Model.GetComponentsInChildren<Transform>(true);

				foreach (Transform transform in transforms)
					if (transform.gameObject.layer != Settings.vehicleWheelsLayer)
						transform.gameObject.layer = Settings.vehicleWheelsLayer;
			}
		}
		public void RefreshBounds()
		{
			bounds = Utility.GetObjectBounds(gameObject);
			worldBounds = bounds;
			bounds.center = transform.InverseTransformPoint(bounds.center);

			if (Chassis)
			{
				chassisBounds = Utility.GetObjectBounds(Chassis.gameObject);
				chassisWorldBounds = chassisBounds;
				chassisBounds.center = Chassis.transform.InverseTransformPoint(chassisBounds.center);
			}
			else
			{
				chassisBounds = bounds;
				chassisWorldBounds = worldBounds;
			}

			Vector3 chassisCenterFromVehicle = transform.InverseTransformPoint(chassisWorldBounds.center);

			COMInterval = new(IsTrailer ? chassisCenterFromVehicle.z + chassisBounds.extents.z : FrontWheelsLocalPosition.z, trailerInstance ? chassisCenterFromVehicle.z - chassisBounds.extents.z : rearWheelsLocalPosition.z, chassisCenterFromVehicle.y - chassisBounds.extents.y, chassisCenterFromVehicle.y + chassisBounds.extents.y);
		}
		public void RefreshWheels()
		{
			if (HasInternalErrors)
				return;

			steerWheels = Wheels.Where(wheel => wheel.Model && wheel.IsSteerWheel).ToArray();
			nonSteerWheels = wheels.Where(wheel => wheel.Model && !wheel.IsSteerWheel).ToArray();
			motorWheels = wheels.Where(wheel => wheel.Model && wheel.IsMotorWheel).ToArray();
			motorLeftWheels = MotorWheels.Where(wheel => wheel.Model && wheel.IsLeftWheel).ToArray();
			motorCenterWheels = motorWheels.Where(wheel => wheel.Model && !wheel.IsLeftWheel && wheel.IsRightWheel).ToArray();
			motorRightWheels = motorWheels.Where(wheel => wheel.Model && wheel.IsRightWheel).ToArray();
			nonMotorWheels = wheels.Where(wheel => wheel.Model && !wheel.IsMotorWheel).ToArray();
			frontWheels = wheels.Where(wheel => wheel.Model && wheel.IsFrontWheel).ToArray();
			rearWheels = wheels.Where(wheel => wheel.Model && !wheel.IsFrontWheel).ToArray();
			leftWheels = wheels.Where(wheel => wheel.Model && wheel.IsLeftWheel).ToArray();
			rightWheels = wheels.Where(wheel => wheel.Model && wheel.IsRightWheel).ToArray();
			centerWheels = wheels.Where(wheel => wheel.Model && !wheel.IsLeftWheel && !wheel.IsRightWheel).ToArray();
			frontLeftWheels = FrontWheels.Where(wheel => wheel.Model && wheel.IsLeftWheel).ToArray();
			frontCenterWheels = frontWheels.Where(wheel => wheel.Model && !wheel.IsLeftWheel && !wheel.IsRightWheel).ToArray();
			frontRightWheels = frontWheels.Where(wheel => wheel.Model && wheel.IsRightWheel).ToArray();
			rearLeftWheels = RearWheels.Where(wheel => wheel.Model && wheel.IsLeftWheel).ToArray();
			rearCenterWheels = rearWheels.Where(wheel => wheel.Model && !wheel.IsLeftWheel && !wheel.IsRightWheel).ToArray();
			rearRightWheels = rearWheels.Where(wheel => wheel.Model && wheel.IsRightWheel).ToArray();
			wheelSpins = null;
			wheelsLocalPosition = Vector3.zero;
			frontWheelsLocalPosition = Vector3.zero;
			rearWheelsLocalPosition = Vector3.zero;
			leftWheelsLocalPosition = Vector3.zero;
			centerWheelsLocalPosition = Vector3.zero;
			rightWheelsLocalPosition = Vector3.zero;
			frontLeftWheelsLocalPosition = Vector3.zero;
			frontRightWheelsLocalPosition = Vector3.zero;
			rearLeftWheelsLocalPosition = Vector3.zero;
			rearRightWheelsLocalPosition = Vector3.zero;
			frontSteerWheelsCount = 0;
			frontCenterSteerWheelsCount = 0;
			rearSteerWheelsCount = 0;
			rearCenterSteerWheelsCount = 0;
			frontMotorWheelsCount = 0;
			frontCenterMotorWheelsCount = 0;
			rearMotorWheelsCount = 0;
			rearCenterMotorWheelsCount = 0;

			foreach (var wheel in wheels)
			{
				if (!wheel.Instance)
					continue;

				wheelsLocalPosition += transform.InverseTransformPoint(wheel.Instance.transform.position);

				if (wheel.IsFrontWheel)
				{
					frontWheelsLocalPosition += transform.InverseTransformPoint(wheel.Instance.transform.position);

					if (wheel.IsSteerWheel)
						frontSteerWheelsCount++;

					if (wheel.IsMotorWheel)
						frontMotorWheelsCount++;

					if (wheel.IsLeftWheel)
						frontLeftWheelsLocalPosition += transform.InverseTransformPoint(wheel.Instance.transform.position);
					else if (wheel.IsRightWheel)
						frontRightWheelsLocalPosition += transform.InverseTransformPoint(wheel.Instance.transform.position);
					else
					{
						frontCenterWheelsLocalPosition += transform.InverseTransformPoint(wheel.Instance.transform.position);

						if (wheel.IsSteerWheel)
							frontCenterSteerWheelsCount++;

						if (wheel.IsMotorWheel)
							frontCenterMotorWheelsCount++;
					}
				}
				else
				{
					rearWheelsLocalPosition += transform.InverseTransformPoint(wheel.Instance.transform.position);

					if (wheel.IsSteerWheel)
						rearSteerWheelsCount++;

					if (wheel.IsMotorWheel)
						rearMotorWheelsCount++;

					if (wheel.IsLeftWheel)
						rearLeftWheelsLocalPosition += transform.InverseTransformPoint(wheel.Instance.transform.position);
					else if (wheel.IsRightWheel)
						rearRightWheelsLocalPosition += transform.InverseTransformPoint(wheel.Instance.transform.position);
					else
					{
						rearCenterWheelsLocalPosition += transform.InverseTransformPoint(wheel.Instance.transform.position);

						if (wheel.IsSteerWheel)
							rearCenterSteerWheelsCount++;

						if (wheel.IsMotorWheel)
							rearCenterMotorWheelsCount++;
					}
				}

				if (wheel.IsLeftWheel)
					leftWheelsLocalPosition += transform.InverseTransformPoint(wheel.Instance.transform.position);
				else if (wheel.IsRightWheel)
					rightWheelsLocalPosition += transform.InverseTransformPoint(wheel.Instance.transform.position);
				else
					centerWheelsLocalPosition += transform.InverseTransformPoint(wheel.Instance.transform.position);
			}

			wheelsLocalPosition = Utility.Divide(wheelsLocalPosition, wheels.Length);
			frontWheelsLocalPosition = Utility.Divide(frontWheelsLocalPosition, frontWheels.Length);
			frontLeftWheelsLocalPosition = Utility.Divide(frontLeftWheelsLocalPosition, frontLeftWheels.Length);
			frontCenterWheelsLocalPosition = Utility.Divide(frontCenterWheelsLocalPosition, frontCenterWheels.Length);
			frontRightWheelsLocalPosition = Utility.Divide(frontRightWheelsLocalPosition, frontRightWheels.Length);
			rearWheelsLocalPosition = Utility.Divide(rearWheelsLocalPosition, rearWheels.Length);
			rearLeftWheelsLocalPosition = Utility.Divide(rearLeftWheelsLocalPosition, rearLeftWheels.Length);
			rearCenterWheelsLocalPosition = Utility.Divide(rearCenterWheelsLocalPosition, rearCenterWheels.Length);
			rearRightWheelsLocalPosition = Utility.Divide(rearRightWheelsLocalPosition, rearRightWheels.Length);
			leftWheelsLocalPosition = Utility.Divide(leftWheelsLocalPosition, leftWheels.Length);
			centerWheelsLocalPosition = Utility.Divide(centerWheelsLocalPosition, centerWheels.Length);
			rightWheelsLocalPosition = Utility.Divide(rightWheelsLocalPosition, rightWheels.Length);
			wheelBase = Utility.Distance(frontWheelsLocalPosition, rearWheelsLocalPosition);
			frontTrack = Utility.Distance(frontLeftWheelsLocalPosition, frontRightWheelsLocalPosition);
			rearTrack = Utility.Distance(rearLeftWheelsLocalPosition, rearRightWheelsLocalPosition);
			canFrontMotor = frontMotorWheelsCount > 0;
			canFrontSteer = frontSteerWheelsCount > 0;
			canRearMotor = rearMotorWheelsCount > 0;
			canRearSteer = rearSteerWheelsCount > 0;
			isFrontMotorAxleBalanced = (frontMotorWheelsCount - frontCenterMotorWheelsCount) % 2 == 0;
			isFrontSteerAxleBalanced = (frontSteerWheelsCount - frontCenterSteerWheelsCount) % 2 == 0;
			isRearMotorAxleBalanced = (rearMotorWheelsCount - rearCenterMotorWheelsCount) % 2 == 0;
			isRearSteerAxleBalanced = (rearSteerWheelsCount - rearCenterSteerWheelsCount) % 2 == 0;
			isVerticalAxleBalanced = leftWheels.Length == rightWheels.Length;
			hasFrontWheels = frontWheels.Length > 0;
			hasRearWheels = rearWheels.Length > 0;
			isAllBalanced = isVerticalAxleBalanced && isFrontMotorAxleBalanced && isRearMotorAxleBalanced && isFrontSteerAxleBalanced && isRearSteerAxleBalanced;

			if (canFrontMotor && canRearMotor)
				drivetrain = Train.AWD;
			else if (canFrontMotor)
				drivetrain = Train.FWD;
			else if (canRearMotor)
				drivetrain = Train.RWD;
			else
				drivetrain = Train.None;

			if (canFrontSteer && canRearSteer)
				steertrain = Train.AWD;
			else if (canFrontSteer)
				steertrain = Train.FWD;
			else if (canRearSteer)
				steertrain = Train.RWD;
			else
				steertrain = Train.None;

			wheelSpins = GetComponentsInChildren<VehicleWheelSpin>();
		}
		public void RefreshWeightHeight()
		{
			RefreshWeightHeight(Stability);
		}
		[Obsolete("Use 'Vehicle.RefreshWeightHeight' instead.")]
		public void ResetWeightHeight()
		{
			RefreshWeightHeight();
		}
		public void RefillFuelTank()
		{
			if (IsTrailer)
				return;

			stats.fuelTank = Behaviour.FuelCapacity;
		}
		public void RefreshWheelsRenderers()
		{
			if (Application.isPlaying || !this)
				return;

			RefreshWheels();

			foreach (var wheel in wheels)
				if (wheel.Instance)
					wheel.Instance.RefreshRenderers();
		}
		public void RefreshIKPivots()
		{
			if (IsTrailer)
				return;

			VehicleIKPivot[] iKPivots = GetComponentsInChildren<VehicleIKPivot>(true);

			if (iKPivots.Length < 1)
				return;

			Transform parent = Chassis.transform.Find("IKPivots");

			if (!parent)
			{
				parent = new GameObject("IKPivots").transform;

				parent.SetParent(Chassis.transform, false);
			}

			if (Settings.useHideFlags)
			{
				for (int i = 0; i < iKPivots.Length; i++)
				{
					if (iKPivots[i].transform.parent != parent)
						iKPivots[i].gameObject.hideFlags = HideFlags.HideInHierarchy;
				}

				parent.gameObject.hideFlags = HideFlags.HideInHierarchy;
			}
		}
		public void RefreshAIBehaviours()
		{
			aiBehaviours = GetComponents<VehicleAIBehaviour>().Where(behaviour => behaviour && behaviour.enabled).ToArray();
			isAIActive = activeAIBehaviourIndex > 0 && activeAIBehaviourIndex < aiBehaviours.Length ? aiBehaviours[activeAIBehaviourIndex] : false;
			isAI = aiBehaviours.Length > 0;
		}

		protected internal void SetStability(StabilityModule stability)
		{
			if (!stability)
				return;

			this.stability = stability;
		}
		protected internal void ApplyCounterSteerHelper(ref float steerAngle)
		{
			if (isAIActive || Settings.counterSteerPriority == 1)
			{
				steerAngle *= 1f - math.abs(stats.counterSteerHelper);
				steerAngle += stats.counterSteerHelper * steering.MaximumSteerAngle;
			}
			else if (Settings.counterSteerPriority == 0)
			{
				steerAngle *= 1f - math.abs(stats.counterSteerHelper);
				steerAngle += stats.counterSteerHelper * steering.MaximumSteerAngle * (1f - math.abs(steeringWheel));
			}
			else
				steerAngle += stats.counterSteerHelper * steering.MaximumSteerAngle * (1f - math.abs(steeringWheel));
		}

		private void RefreshWeightHeight(StabilityModule stability)
		{
			RefreshBounds();
			RefreshWheels();

			weightHeight = Utility.Average(Bounds.extents.y, Utility.Average(frontWheelsLocalPosition.y, rearWheelsLocalPosition.y));
			weightHeight = Utility.InverseLerp(COMInterval.y.Min, COMInterval.y.Max, weightHeight);
			stability.WeightHeight = weightHeight;
		}
		private bool UpdateAI(VehicleAIBehaviour.UpdateType updateType)
		{
			if (activeAIBehaviourIndex != lastActiveAIBehaviourIndex)
			{
				if (lastActiveAIBehaviourIndex > -1)
					aiBehaviours[lastActiveAIBehaviourIndex].OnDeactivation();

				if (activeAIBehaviourIndex > -1)
					aiBehaviours[activeAIBehaviourIndex].OnActivation();

				lastActiveAIBehaviourIndex = activeAIBehaviourIndex;
			}

			int aiBehavioursCount = aiBehaviours.Length;
			bool updateInputs = false;

			for (int index = 0; index < aiBehavioursCount; index++)
			{
				var behaviour = aiBehaviours[index];

				if (!behaviour)
				{
					RefreshAIBehaviours();

					return false;
				}

				if (behaviour.updateType != updateType || index != activeAIBehaviourIndex && behaviour.updateMethod != VehicleAIBehaviour.UpdateMethod.Always)
					continue;

				InputsAccess newInputs = inputs;

				behaviour.OnUpdate(ref newInputs);

				if (index != activeAIBehaviourIndex)
					continue;

				newInputs.FuelPedalWasPressed = Utility.IsDownFromLastState(newInputs.FuelPedal, inputs.FuelPedal);
				newInputs.FuelPedalWasReleased = Utility.IsUpFromLastState(newInputs.FuelPedal, inputs.FuelPedal);
				newInputs.BrakePedalWasPressed = Utility.IsDownFromLastState(newInputs.BrakePedal, inputs.BrakePedal);
				newInputs.ClutchPedalWasPressed = Utility.IsDownFromLastState(newInputs.ClutchPedal, inputs.ClutchPedal);
				newInputs.HandbrakeWasPressed = Utility.IsDownFromLastState(newInputs.Handbrake, inputs.Handbrake);

				inputs = newInputs;
				updateInputs = true;
			}

			return updateInputs;
		}

		#endregion

		#region Update

		private void Update()
		{
			if (!Awaken)
				return;

			if (!trailerInstance)
			{
				bool updateInputs = true;

				if (isAI)
					updateInputs = UpdateAI(VehicleAIBehaviour.UpdateType.NormalUpdate);

				if (updateInputs)
					inputs.Update(this);

				StartOrShutEngine();
				LaunchControl();
				GearBox();
			}

			LightInputs();
			VehicleReset();
		}

		#region General

		public void ResetVehicle()
		{
			transform.eulerAngles = new(0f, transform.eulerAngles.y, 0f);
			transform.position += transform.up * ChassisBounds.extents.y;

			Vector3 chassisBottomPoint = Chassis.transform.TransformPoint(ChassisBounds.center) - transform.up * ChassisBounds.extents.y;
			float distanceToGround = 0f;

			if (Physics.Raycast(chassisBottomPoint, -transform.up, out RaycastHit hit, 1000f, Utility.ExclusiveMask(Settings.vehiclesLayer), QueryTriggerInteraction.Ignore))
				distanceToGround = hit.distance;

			foreach (var wheel in wheels)
			{
				Vector3 wheelBottomPoint = Vector3.zero;

				if (ToolkitSettings.UsingWheelColliderPhysics)
				{
					wheel.Instance.wheelCollider.GetWorldPose(out wheelBottomPoint, out _);
					wheelBottomPoint -= wheel.Instance.wheelCollider.transform.up * wheel.Instance.CurrentRadius;
				}

				if (wheelBottomPoint != Vector3.zero && Physics.Raycast(wheelBottomPoint, -transform.up, out hit, 1000f, Utility.ExclusiveMask(Settings.vehiclesLayer), QueryTriggerInteraction.Ignore))
					distanceToGround = distanceToGround == 0f ? hit.distance : math.min(distanceToGround, hit.distance);

				wheel.Instance.ResetWheel();
			}

			if (!IsTrailer)
				inputs.ResetInputs(this);

			rigidbody.
#if UNITY_6000_0_OR_NEWER
				linearVelocity
#else
				velocity
#endif
				= Vector3.zero;
			rigidbody.angularVelocity = Vector3.zero;
			rigidbody.isKinematic = true;
			transform.position = new(transform.position.x, trailerInstance && trailerInstance.Joint.ConnectedVehicle ? trailerInstance.Joint.ConnectedVehicle.transform.position.y : transform.position.y - distanceToGround, transform.position.z);

			if (!trailerInstance)
			{
				stats.ResetVehicle(this);
				RequestChangeGear(0, true);
			}

			vehicleRenderers = GetComponentsInChildren<Renderer>().Where(renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer).ToArray();
			//vehicleColliders = GetComponentsInChildren<Collider>().Where(collider => !collider.isTrigger).ToArray();
			resetMeshTimer = Settings.resetPreviewTime;

			Physics.IgnoreLayerCollision(Settings.vehiclesLayer, Settings.vehiclesLayer, resetMeshTimer > 0f);
		}

		private void LaunchControl()
		{
			if (!stability.UseLaunchControl || !stats.isEngineRunning || stats.isEngineStarting)
			{
				if (stats.isLaunchControlActive)
					stats.isLaunchControlActive = false;

				return;
			}

			if (inputs.LaunchControlSwitchWasPressed)
				stats.isLaunchControlActive = !stats.isLaunchControlActive;

			if (stats.isLaunchControlActive && (Utility.Round(stats.averageMotorWheelsSpeed, 1) != 0f || Utility.Round(stats.currentSpeed, 1) != 0f))
				stats.isLaunchControlActive = false;
		}
		private void LightInputs()
		{
			if (!Settings.useLights)
				return;

			if (trailerInstance)
			{
				if (trailerInstance.Joint.ConnectedVehicle)
				{
					stats.isLightsOn = trailerInstance.Joint.ConnectedVehicle.stats.isLightsOn;
					stats.inFogWeatherZone = trailerInstance.Joint.ConnectedVehicle.stats.inFogWeatherZone;
					stats.inDarknessWeatherZone = trailerInstance.Joint.ConnectedVehicle.stats.inDarknessWeatherZone;
					stats.isHighBeamHeadlightsOn = trailerInstance.Joint.ConnectedVehicle.stats.isHighBeamHeadlightsOn;
#if !MVC_COMMUNITY
					stats.isInteriorLightsOn = trailerInstance.Joint.ConnectedVehicle.stats.isInteriorLightsOn;
#endif
					stats.isSignalLeftLightsOn = trailerInstance.Joint.ConnectedVehicle.stats.isSignalLeftLightsOn;
					stats.isSignalRightLightsOn = trailerInstance.Joint.ConnectedVehicle.stats.isSignalRightLightsOn;
					stats.isHazardLightsOn = stats.isSignalLeftLightsOn || stats.isSignalRightLightsOn;
					isSignalLeftLightsOn = trailerInstance.Joint.ConnectedVehicle.isSignalLeftLightsOn;
					isSignalRightLightsOn = trailerInstance.Joint.ConnectedVehicle.isSignalRightLightsOn;
				}
				else
				{
					stats.isLightsOn = false;
					stats.inFogWeatherZone = false;
					stats.inDarknessWeatherZone = false;
					stats.isHighBeamHeadlightsOn = false;
#if !MVC_COMMUNITY
					stats.isInteriorLightsOn = false;
#endif
					stats.isSignalLeftLightsOn = false;
					stats.isSignalRightLightsOn = false;
					stats.isHazardLightsOn = false;
					isSignalLeftLightsOn = false;
					isSignalRightLightsOn = false;
				}

				return;
			}

			if (inputs.LightSwitchWasPressed)
				stats.isLightsOn = !stats.isLightsOn;

			if (Settings.inputSystem == ToolkitSettings.InputSystem.InputsManager && Settings.switchHighBeamOnDoublePress)
			{
				if (stats.isHighBeamHeadlightsOn && inputs.HighBeamLightSwitchWasPressed)
				{
					switchHighBeamHeadlightsButtonDoublePressed = false;
					stats.isHighBeamHeadlightsOn = false;
				}
				else if (inputs.HighBeamLightSwitchWasDoublePressed)
				{
					switchHighBeamHeadlightsButtonDoublePressed = true;
					stats.isHighBeamHeadlightsOn = true;
				}

				if (!switchHighBeamHeadlightsButtonDoublePressed)
					stats.isHighBeamHeadlightsOn = inputs.HighBeamLightButton;
			}
			else if (inputs.HighBeamLightSwitchWasPressed)
				stats.isHighBeamHeadlightsOn = !stats.isHighBeamHeadlightsOn;

#if !MVC_COMMUNITY
			if (inputs.InteriorLightSwitchWasPressed)
				stats.isInteriorLightsOn = !stats.isInteriorLightsOn;
#endif

			if (inputs.SideSignalLeftLightSwitchWasPressed)
			{
				sideSignalLightsTimer = 0f;
				isSignalLeftLightsOn = !isSignalLeftLightsOn || isSignalLeftLightsOn && isSignalRightLightsOn;
				isSignalRightLightsOn = false;
			}

			if (inputs.SideSignalRightLightSwitchWasPressed)
			{
				sideSignalLightsTimer = 0f;
				isSignalRightLightsOn = !isSignalRightLightsOn || isSignalLeftLightsOn && isSignalRightLightsOn;
				isSignalLeftLightsOn = false;
			}

			if (inputs.HazardLightsSwitchWasPressed)
			{
				sideSignalLightsTimer = 0f;
				isSignalLeftLightsOn = isSignalRightLightsOn = !isSignalLeftLightsOn || !isSignalRightLightsOn;
			}

			bool isFogEnabled = Settings.useIndicatorLightsInFog && (stats.inFogWeatherZone || Manager.fogTime);
			bool isEngineStallEnabled = Settings.useIndicatorLightsAtEngineStall && stats.isEngineStall;

			if (isSignalLeftLightsOn || isSignalRightLightsOn || isFogEnabled || isEngineStallEnabled)
			{
				sideSignalLightsTimer += Time.deltaTime;

				if (sideSignalLightsTimer >= 1f)
					sideSignalLightsTimer -= 1f;

				bool newIsSignalLeftLightsOn = (isSignalLeftLightsOn || isFogEnabled || isEngineStallEnabled) && Mathf.Floor(sideSignalLightsTimer * 2f) % 2 == 0f;
				bool newIsSignalRightLightsOn = (isSignalRightLightsOn || isFogEnabled || isEngineStallEnabled) && Mathf.Floor(sideSignalLightsTimer * 2f) % 2 == 0f;

				if (Utility.IsDownFromLastState(newIsSignalLeftLightsOn, stats.isSignalLeftLightsOn) || Utility.IsDownFromLastState(newIsSignalRightLightsOn, stats.isSignalRightLightsOn))
					sfx.IndicatorChange();

				stats.isSignalLeftLightsOn = newIsSignalLeftLightsOn && Mathf.Floor(sideSignalLightsTimer * 2f) % 2 == 0f;
				stats.isSignalRightLightsOn = newIsSignalRightLightsOn && Mathf.Floor(sideSignalLightsTimer * 2f) % 2 == 0f;
			}
			else
				stats.isSignalLeftLightsOn = stats.isSignalRightLightsOn = false;
		}
		private void VehicleReset()
		{
			rigidbody.isKinematic = false;

			if (!Settings.useReset)
				return;

			if (inputs.ResetButtonWasPressed)
				ResetVehicle();

			if (trailerInstance && trailerInstance.Joint.ConnectedVehicle)
			{
				if (resetMeshTimer <= 0f && trailerInstance.Joint.ConnectedVehicle.resetMeshTimer > 0f && !inputs.ResetButtonWasPressed)
					ResetVehicle();

				resetMeshTimer = trailerInstance.Joint.ConnectedVehicle.resetMeshTimer;
			}
			else
			{
				if (resetMeshTimer > 0f)
					resetMeshTimer -= Time.deltaTime;
				else if (resetMeshTimer < 0f)
				{
					for (int i = 0; i < Manager.ActiveVehicles.Length; i++)
						if (Manager.ActiveVehicles[i] != this && WorldBounds.Intersects(Manager.ActiveVehicles[i].WorldBounds))
						{
							resetMeshTimer = Settings.resetPreviewTime;

							break;
						}

					resetMeshTimer = default;

					Physics.IgnoreLayerCollision(Settings.vehiclesLayer, Settings.vehiclesLayer, resetMeshTimer > 0f);
				}
			}

			bool newIsBodyVisible = (int)math.round(resetMeshTimer * 3f) % 2 == 0;

			if (isBodyVisible != newIsBodyVisible)
			{
				isBodyVisible = newIsBodyVisible;

				if (vehicleRenderers != null)
					for (int i = 0; i < vehicleRenderers.Length; i++)
						if (vehicleRenderers[i])
							vehicleRenderers[i].enabled = isBodyVisible;
			}

			if (trailerInstance || stats.currentSpeed > 10f)
				return;

			if (transform.eulerAngles.z < 285f && transform.eulerAngles.z > 75f)
			{
				resetTimer += Time.deltaTime;

				if (resetTimer >= Settings.resetTimeout)
				{
					ResetVehicle();

					resetTimer = default;
				}
			}
			else if (resetTimer > 0f)
				resetTimer = default;
		}

		#endregion

		#region Engine

		public void StartOrShutEngine()
		{
			if (IsTrailer || !Awaken)
				return;

			bool forceStartOrShutEngine = false;

			if (!isAIActive)
			{
				if (inputs.AnyInputWasPressed || !stats.isStationary)
				{
					autoShutDownTimer = 0f;

					if (Settings.engineStartMode == ToolkitSettings.EngineStartMode.AnyKeyPress && inputs.AnyInputWasPressed && !stats.isEngineRunning && !stats.isEngineStarting)
						forceStartOrShutEngine = true;
				}

				if (stats.isEngineRunning && Settings.engineStartMode != ToolkitSettings.EngineStartMode.Always && Settings.autoEngineShutDown)
				{
					if (autoShutDownTimer < Settings.engineShutDownTimeout)
						autoShutDownTimer += Time.deltaTime;
					else
						forceStartOrShutEngine = true;
				}
			}

			if (stats.isEngineRunning && !stats.isEngineStarting && stats.fuelTank <= 0f)
				stats.isEngineRunning = false;

			if (!inputs.EngineStartSwitchWasPressed && !forceStartOrShutEngine || stats.isEngineStarting)
				return;

			if (stats.isEngineRunning)
			{
				stats.isEngineRunning = false;
				stats.rawStarterRPM = engine.MinimumRPM * Utility.Clamp01(1f - Utility.Lerp(stats.rawWheelsRPM, stats.rawFuelRPM, inputs.Clutch)) / Utility.Lerp(stats.isReversing || stats.currentGear + 1 >= transmission.GearsCount ? engine.OverRevRPM : engine.RedlineRPM, engine.RedlineRPM, inputs.Clutch);

				RefreshTargetSpeeds();
			}
			else
				StartCoroutine(StartEngine());
		}
		public void StallEngine()
		{
			if (!Settings.useEngineStalling)
				return;

			stats.isEngineStarting = false;
			stats.isEngineRunning = false;
			stats.isEngineStall = true;
		}

		private IEnumerator StartEngine()
		{
			if (stats.isEngineStall)
				isSignalLeftLightsOn = isSignalRightLightsOn = false;

			stats.isEngineStall = false;
			stats.isEngineRunning = false;
			stats.isEngineStarting = true;

			sfx.EngineStart();
			RefreshTargetSpeeds();

			yield return new WaitForSeconds(Engine.Audio.startingClip ? Engine.Audio.startingClip.length * .5f : .5f);

			stats.isEngineRunning =
#if MVC_COMMUNITY
				true;
#else
				!Settings.useFuelSystem || stats.fuelTank > 0f;
#endif

			RefreshTargetSpeeds();

			yield return new WaitForSeconds(Engine.Audio.startingClip ? Engine.Audio.startingClip.length * .5f : .5f);

			stats.isEngineStarting = false;

			RefreshTargetSpeeds();
		}

		#endregion

		#region Drivetrain

		public void RequestGearShiftUp()
		{
			requestGearShiftUp = true;
			requestGearShiftDown = false;
			requestGearShiftForward = false;
			requestGearShiftNeutral = false;
			requestGearShiftReverse = false;
		}
		public void RequestGearShiftDown()
		{
			requestGearShiftUp = false;
			requestGearShiftDown = true;
			requestGearShiftForward = false;
			requestGearShiftNeutral = false;
			requestGearShiftReverse = false;
		}
		public void RequestGearShiftToNeutral()
		{
			requestGearShiftUp = false;
			requestGearShiftDown = false;
			requestGearShiftForward = false;
			requestGearShiftNeutral = true;
			requestGearShiftReverse = false;
		}
		public void RequestGearShiftToForward()
		{
			requestGearShiftUp = false;
			requestGearShiftDown = false;
			requestGearShiftForward = true;
			requestGearShiftNeutral = false;
			requestGearShiftReverse = false;
		}
		public void RequestGearShiftToReverse()
		{
			requestGearShiftUp = false;
			requestGearShiftDown = false;
			requestGearShiftForward = false;
			requestGearShiftNeutral = false;
			requestGearShiftReverse = true;
		}

		private void GearBox()
		{
			if (stats.isChangingGear || motorWheels.Length < 1)
				return;

			float averageSpeed = stats.localVelocity.z * 3.6f * (1f + math.clamp(stats.averageMotorWheelsForwardSlip * Mathf.Sign(inputs.Direction), 0f, .25f));

			if (isAIActive && activeAIBehaviourIndex > -1)
				AutoGearboxShiftUpDown(averageSpeed);
			else if (Settings.AutomaticTransmission)
			{
				if (stats.isNeutral)
				{
					if (stats.isEngineRunning)
					{
						if (inputs.FuelPedal * (1f - (transmission.Gearbox != TransmissionModule.GearboxType.Manual && !Settings.burnoutsForAutomaticTransmissions ? inputs.BrakePedal : 0f)) - inputs.ClutchPedalOrHandbrake > 0f || math.round(averageSpeed * (1f - inputs.ClutchPedalOrHandbrake)) > 0f)
							RequestSearchGear(averageSpeed, 1f);
						else if (averageSpeed < 1f && inputs.BrakePedal * (1f - inputs.FuelPedal) - inputs.ClutchPedalOrHandbrake > 0f)
							RequestChangeGear(-2);
					}
				}
				else if (stats.isReversing)
				{
					if (!stats.isEngineRunning)
						RequestChangeGear(-1);
					else if (averageSpeed >= -(transmission.GetMinSpeedTarget(-1, 0) * 4f + transmission.GetSpeedTarget(-1, 0)) * .2f && inputs.FuelPedal * (1f - inputs.BrakePedal) - inputs.ClutchPedalOrHandbrake > 0f)
						RequestSearchGear(averageSpeed, 1f);
					else if (Utility.Round(averageSpeed, 1) == 0f && inputs.BrakePedal * (1f - inputs.FuelPedal) - inputs.ClutchPedalOrHandbrake <= 0f)
						RequestChangeGear(-1);
					else if (math.round(stats.currentSpeed) < 1f && canShiftDown && Mathf.Approximately(math.max(inputs.RawFuel - inputs.ClutchPedalOrHandbrake, 0f), 0f))
						RequestChangeGear(-1);
				}
				else
				{
					if (!stats.isEngineRunning)
						RequestChangeGear(-1);
					else if (averageSpeed < 1f && inputs.BrakePedal * (1f - inputs.FuelPedal) - inputs.ClutchPedalOrHandbrake > 0f)
						RequestChangeGear(-2);
					else if (averageSpeed < 1f && inputs.FuelPedal - (transmission.Gearbox != TransmissionModule.GearboxType.Manual && !Settings.burnoutsForAutomaticTransmissions ? inputs.BrakePedal : 0f) - inputs.ClutchPedalOrHandbrake <= 0f)
						RequestChangeGear(-1);
					else
					{
						AutoGearboxShiftUpDown(averageSpeed);

						if (!isAIActive && canShiftDown && math.round(stats.currentSpeed) <= 1f && Mathf.Approximately(math.max(inputs.RawFuel - inputs.ClutchPedalOrHandbrake, 0f), 0f))
							RequestChangeGear(-1);
					}
				}
			}
			else
			{
				if (inputs.GearShiftUpWasPressed && stats.currentGear < transmission.GearsCount - 1)
				{
					if (stats.engineRPM >= Engine.RedlineRPM)
						GearShiftUpEffects();

					if (stats.isReversing)
						RequestChangeGear(-1);
					else if (stats.isNeutral)
						RequestChangeGear(0);
					else
						RequestChangeGear(stats.currentGear + 1);
				}
				else if (inputs.GearShiftDownWasPressed)
				{
					if (stats.currentGear > 0)
						RequestChangeGear(stats.currentGear - 1);
					else if (inputs.Direction == 1)
						RequestChangeGear(-1);
					else if (!Settings.useEngineStalling || averageSpeed < transmission.GetMinSpeedTarget(1, 0))
						RequestChangeGear(-2);
				}
			}

			if (requestGearShiftForward)
				RequestChangeGear(0);
			else if (requestGearShiftNeutral)
				RequestChangeGear(-1);
			else if (requestGearShiftReverse)
				RequestChangeGear(-2);
			else if (requestGearShiftUp)
				RequestChangeGear(inputs.Direction + stats.currentGear);
			else if (requestGearShiftDown)
				RequestChangeGear(inputs.Direction + stats.currentGear - 2);
		}
		private void RequestChangeGear(int gear, bool immediate = false)
		{
			if (gearboxShiftCoroutine != null)
				StopCoroutine(gearboxShiftCoroutine);

			gearboxShiftCoroutine = StartCoroutine(ChangeGear(gear, immediate));
		}
		private void RequestSearchGear(float speed, float factor, bool immediate = false)
		{
			if (gearboxShiftCoroutine != null)
				StopCoroutine(gearboxShiftCoroutine);

			gearboxShiftCoroutine = StartCoroutine(SearchGear(speed, factor, immediate));
		}
		private void AutoGearboxShiftUpDown(float averageSpeed)
		{
			if (stats.currentGear + 1 < transmission.GearsCount && averageSpeed > GearShiftSpeed(stats.currentGear))
			{
				if (stats.engineRPM >= Engine.RedlineRPM)
					GearShiftUpEffects();

				RequestChangeGear(stats.currentGear + 1);
			}
			else if (canShiftDown && stats.currentGear > 0 && averageSpeed < GearShiftSpeed(stats.currentGear - 1) * Utility.Lerp(1f, .9f, inputs.RawFuel - inputs.ClutchPedalOrHandbrake))
				RequestChangeGear(stats.currentGear - 1);
		}
		private float GearShiftSpeed(int gear)
		{
			float shiftSpeed = (transmission.GetGearShiftOverrideSpeed(gear, out float targetSpeed) ? targetSpeed : transmission.GetSpeedTarget(1, gear)) * math.max(transmission.Efficiency, .25f);
			float shiftSpeedFactor = Utility.InverseLerp(transmission.GetMinSpeedTarget(1, gear) / shiftSpeed, 1f, gearShiftRPMFactor);

			return shiftSpeed * shiftSpeedFactor;
		}
		private IEnumerator SearchGear(float speed, float factor, bool immediate)
		{
			int gear = 0;

			while (speed >= (transmission.GetGearShiftOverrideSpeed(gear, out float shiftSpeed) ? shiftSpeed : transmission.GetSpeedTarget(0, gear)) * factor && gear + 1 < transmission.GearsCount)
				gear++;

			yield return ChangeGear(gear, immediate);
		}
		private void GearShiftUpEffects()
		{
			if (behaviour.IsTurbocharged)
				sfx.TurbochargerGearBlowout();

			if (behaviour.IsSupercharged)
				sfx.SuperchargerGearBlowout();

			if (behaviour.useExhaustEffects && UnityEngine.Random.Range(0f, 1f) < behaviour.ExhaustFlameEmissionProbability)
			{
				sfx.ExhaustPop();
				vfx.ShootExhaustFlames();
			}
		}
		private IEnumerator ChangeGear(int gear, bool immediate)
		{
			if (gear < -1 && inputs.Direction == -1 || gear >= transmission.GearsCount)
				yield break;

			stats.isChangingGear = true;
			currentGearToString = "N";

			if (gear >= 0 && (gear > stats.currentGear || stats.isNeutral) || gear < 0 && gear >= inputs.Direction)
			{
				if (stats.isEngineRunning)
					sfx.GearboxShiftUp();

				stats.isChangingGearUp = true;
			}
			else if (gear >= 0 && gear < stats.currentGear || gear < 0 && gear < inputs.Direction)
			{
				if (stats.isEngineRunning)
					sfx.GearboxShiftDown();

				stats.isChangingGearDown = true;
			}

			if (!immediate)
				immediate = inputs.Direction == 0 && (gear == 0 || gear < -1);

			if (!immediate)
				yield return new WaitForSeconds(transmission.ClutchOutDelay + transmission.ShiftDelay);

			stats.currentGear = math.max(gear, 0);

			if (!InputsAccess.GetOverrideInputs(this))
				inputs.Direction = math.clamp(gear + 1, -1, 1);

			stats.isNeutral = inputs.Direction == 0;
			stats.isReversing = inputs.Direction < 0;

			RefreshTargetSpeeds();

			if (!immediate)
				yield return new WaitForSeconds(transmission.ClutchInDelay);

			if (stats.isChangingGearUp || stats.isChangingGearDown && stats.isReversing)
				StartCoroutine(WaitForShiftDown());

			currentGearToString = stats.isReversing ? "R" : stats.isNeutral ? "N" : $"{stats.currentGear + 1}";
			stats.isChangingGear = false;
			stats.isChangingGearUp = false;
			stats.isChangingGearDown = false;
			requestGearShiftUp = false;
			requestGearShiftDown = false;
			requestGearShiftForward = false;
			requestGearShiftNeutral = false;
			requestGearShiftReverse = false;
		}
		private IEnumerator WaitForShiftDown()
		{
			canShiftDown = false;

			yield return new WaitForSeconds(math.max(transmission.ClutchOutDelay + transmission.ShiftDelay + transmission.ClutchInDelay, .25f) * 2f);

			canShiftDown = true;
		}
		private void RefreshTargetSpeeds()
		{
			stats.currentGearSpeedTarget = stats.isEngineStarting ? transmission.GetMinSpeedTarget(1, 0) : transmission.GetGearShiftOverrideSpeed(stats.currentGear, out float shiftSpeed) ? shiftSpeed : transmission.GetSpeedTarget(inputs.Direction, stats.currentGear);
			stats.currentGearMinSpeedTarget = stats.isEngineStarting ? 0f : transmission.GetMinSpeedTarget(inputs.Direction, stats.currentGear);
			stats.currentGearRatio = transmission.GetGearRatio(inputs.Direction, stats.currentGear);
		}
		private void RefreshGearShiftRPMFactor()
		{
			if (Mathf.Approximately(transmission.GearShiftTorqueMultiplier, 0f))
			{
				gearShiftRPMFactor = 1f;

				return;
			}
			else if (Mathf.Approximately(transmission.GearShiftTorqueMultiplier, 1f))
			{
				gearShiftRPMFactor = behaviour.PeakTorqueRPM / engine.MaximumRPM;

				return;
			}

			float maxTorque = behaviour.Torque;
			var torqueCurve = behaviour.TorqueCurve;
			float minTorque = maxTorque * transmission.GearShiftTorqueMultiplier;

			for (float rpm = behaviour.PeakTorqueRPM; rpm <= engine.MaximumRPM; rpm++)
			{
				float torque = torqueCurve.Evaluate(rpm);

				if (torque <= minTorque)
				{
					gearShiftRPMFactor = rpm / engine.MaximumRPM;

					return;
				}
			}

			gearShiftRPMFactor = 1f;
		}

		#endregion

		#endregion

		#region Fixed Update

		private void FixedUpdate()
		{
			if (!Awaken)
				return;

			if (!trailerInstance)
			{
				bool updateInputs = false;

				if (isAI)
					updateInputs = UpdateAI(VehicleAIBehaviour.UpdateType.FixedUpdate);

				if (updateInputs)
					inputs.Update(this);
			}

			stats.FixedUpdate(this);
			WeightDistribution();

			if (!trailerInstance)
			{
				UpdateEngine();
				CounterSteer();
				Steer();
				ArcadeSteerHelpers();
				Burnouts();
			}

			AntiSwayBars();
			Brakes();
			ABS();
			ESP();
			Aerodynamics();

			if (!trailerInstance)
			{
				Donuts();
				Drift();
			}
		}

		#region General

		private void CounterSteer()
		{
			if (stability.UseCounterSteer)
				stats.counterSteerHelper = Utility.Lerp(stats.counterSteerHelper, stats.averageRearWheelsSidewaysSlip * Settings.counterSteerIntensity, Time.fixedDeltaTime * Settings.counterSteerDamping);
			else if (stats.counterSteerHelper != default)
				stats.counterSteerHelper = default;
		}
		private void Steer()
		{
			float steerAngleSpeedFactor = stats.currentSpeed / steering.LowSteerAngleSpeed;

			if (steering.clampSteerAngle)
				steerAngleSpeedFactor = Utility.Clamp01(steerAngleSpeedFactor);
			else
				steerAngleSpeedFactor = math.max(steerAngleSpeedFactor, 0f);

			float steeringSmoothnessFactor = Mathf.Sign(inputs.SteeringWheel) != Mathf.Sign(steeringWheel) ? 1f : math.abs(steeringWheel) - math.abs(inputs.SteeringWheel);

			steeringWheel = isAI ? inputs.SteeringWheel : Settings.steeringInterpolation switch
			{
				ToolkitSettings.SteeringInterpolation.Exponential => Utility.Lerp(steeringWheel, inputs.SteeringWheel, Utility.Lerp(Settings.steerIntensity, Settings.steerReleaseIntensity, steeringSmoothnessFactor) * Time.fixedDeltaTime),
				ToolkitSettings.SteeringInterpolation.Linear => Mathf.MoveTowards(steeringWheel, inputs.SteeringWheel, Utility.Lerp(Settings.steerIntensity, Settings.steerReleaseIntensity, steeringSmoothnessFactor) * Time.fixedDeltaTime),
				_ => inputs.SteeringWheel
			};
			stats.rawSteerAngle = steering.MaximumSteerAngle * steeringWheel;
			stats.steerAngle = default;

			foreach (var wheel in wheels)
			{
				if (!wheel.IsSteerWheel && !steering.UseDynamicSteering)
					continue;

				switch (steering.Method)
				{
					case SteeringModule.SteerMethod.Responsive:
						steerAngle = math.max(Utility.LerpUnclamped(steering.MaximumSteerAngle, steering.MinimumSteerAngle, steerAngleSpeedFactor), 1f);

						break;

					case SteeringModule.SteerMethod.Ackermann:
						ackermannP1 = Vector3.zero;
						ackermannP2 = ackermannP1 + Quaternion.AngleAxis(steering.MaximumSteerAngle, Vector3.up) * Vector3.right;
						ackermannP3 = Vector3.forward * wheelBase;
						ackermannP4 = ackermannP3 + Vector3.right;

						while (Utility.FindIntersection(ackermannP1, ackermannP2, ackermannP3, ackermannP4, out ackermannIntersectionPoint))
						{
							ackermannP2 *= 2f;
							ackermannP4 *= 2f;
						}

						turnRadius = Utility.Distance(ackermannIntersectionPoint, ackermannP3) + rearTrack * .5f;
						ackermannSteerAngle = Mathf.Rad2Deg * math.atan(wheelBase / (turnRadius - Mathf.Sign(inputs.SteeringWheel) * (int)wheel.side * rearTrack * .5f));
						ackermannSteerAngle *= steering.MaximumSteerAngle / 45f;
						steerAngle = math.max(Utility.LerpUnclamped(ackermannSteerAngle, steering.MinimumSteerAngle, steerAngleSpeedFactor), 1f);

						break;

					default:
						steerAngle = steering.MaximumSteerAngle;

						break;
				}

				stats.steerAngle = math.max(stats.steerAngle, steerAngle);
				steerAngle *= steeringWheel;

				if (!wheel.IsSteerWheel)
					steerAngle *= steering.DynamicSteeringIntensity;

				if (!isAI && Settings.counterSteerType != ToolkitSettings.CounterSteerType.VisualsOnly)
					ApplyCounterSteerHelper(ref steerAngle);

				if (!wheel.IsFrontWheel && steering.invertRearSteer)
					steerAngle *= -1f;

				wheel.Instance.steerAngle = math.clamp(steerAngle, -steering.MaximumSteerAngle, steering.MaximumSteerAngle);
			}

			stats.maximumSteerAngle = stats.steerAngle;
			stats.steerAngle *= steeringWheel;
		}
		private void Brakes()
		{
			if (trailerInstance && !trailerInstance.useBrakes)
				return;

			foreach (var wheel in wheels)
			{
				if (wheel.IsFrontWheel)
				{
					wheel.Instance.brakeTorque = frontBrakes.BrakeTorque * inputs.Brake / frontWheels.Length;

					if (ToolkitSettings.UsingWheelColliderPhysics && math.abs(wheel.Instance.brakeTorque) < 10f && Mathf.Approximately(Utility.Round(inputs.Brake, 2), 0f))
						wheel.Instance.brakeTorque = 0f;
				}
				else
				{
					wheel.Instance.brakeTorque = rearBrakes.BrakeTorque * Utility.Clamp01(inputs.Brake + inputs.Handbrake) / rearWheels.Length;

					if (Drivetrain == Train.RWD)
						wheel.Instance.brakeTorque *= Utility.Clamp01(1f - inputs.RawFuel + inputs.Handbrake);

					if (ToolkitSettings.UsingWheelColliderPhysics && math.abs(math.round(wheel.Instance.RPM)) < 1f && Utility.Round(inputs.Brake + inputs.Handbrake, 2) < .01f)
						wheel.Instance.brakeTorque = 0f;
				}
			}
		}

		#endregion

		#region Engine

		private void UpdateEngine()
		{
			BoostNOS();
			AspirationBoost();
			Torque();
			ChassisTorque();
#if !MVC_COMMUNITY
			Fuel();
#endif
		}
		private void BoostNOS()
		{
			stats.isNOSActive = Settings.useNOS && behaviour.useNOS && !stats.isStationary;

			if (!stats.isNOSActive)
			{
				vfx.EmitNOSFlames(false);

				return;
			}

			bool canNOS = inputs.Direction == 1 && stats.NOS > 0f;

			if (!isNOSReady && !isNOSGettingReady && inputs.NOS && canNOS)
			{
				if (Settings.useNOSDelay)
					StartCoroutine(GetNOSReady());
				else
					isNOSReady = true;
			}

			float consumption = behaviour.NOSConsumption * Time.fixedDeltaTime;

			isOneShotNOSActive = (isOneShotNOSActive || inputs.NOS) && Settings.useOneShotNOS;
			stats.isNOSActive = canNOS && isNOSReady && (inputs.NOS || isOneShotNOSActive && stats.bottleNOS > 0f);

			if (stats.isNOSActive)
			{
				stats.NOS -= consumption;
				NOSRegenerateTimer = 0f;
			}
			else
			{
				if (stats.NOS < behaviour.NOSCapacity * behaviour.NOSBottlesCount && NOSRegenerateTimer >= behaviour.NOSRegenerateTime && inputs.Direction == 1)
					stats.NOS += consumption;
				else if (Settings.useNOSReload)
					NOSRegenerateTimer += Time.fixedDeltaTime;

				isOneShotNOSActive = false;
				isNOSReady = false;
			}

			stats.NOS = math.clamp(stats.NOS, 0f, behaviour.NOSCapacity * behaviour.NOSBottlesCount);

			float oldBottleNOS = stats.bottleNOS;

			stats.bottleNOS = stats.NOS % behaviour.NOSCapacity != 0f ? stats.NOS % behaviour.NOSCapacity : behaviour.NOSCapacity;
			stats.bottleNOS = math.clamp(stats.bottleNOS, 0f, behaviour.NOSCapacity);

			if (isOneShotNOSActive && stats.bottleNOS > oldBottleNOS)
				isOneShotNOSActive = false;

			stats.NOSBoost = stats.isNOSActive ? behaviour.NOSBoost : 0f;

			if (stats.isNOSActive && Settings.useProgressiveNOS)
				stats.NOSBoost *= stats.bottleNOS / behaviour.NOSCapacity;

			vfx.EmitNOSFlames(stats.isNOSActive);
		}
		private IEnumerator GetNOSReady()
		{
			isNOSGettingReady = true;

			sfx.NOSStart();

			yield return new WaitForSeconds(Settings.useNOSDelay ? Settings.NOSDelay : 0f);

			isNOSGettingReady = false;
			isNOSReady = true;
		}
		private void AspirationBoost()
		{
			if (!behaviour.IsTurbocharged && !behaviour.IsSupercharged)
				return;

			engineBoost = stats.engineBoost = Utility.Lerp(stats.engineBoost, inputs.Fuel, behaviour.IsTurbocharged ? 10f * Time.fixedDeltaTime * (int)Turbocharger.TurboCount / Turbocharger.ChargerSize : 1f);

			if (behaviour.IsTurbocharged)
				engineBoost *= Utility.Lerp(/*Turbocharger.isStock ? Turbocharger.MinimumBoost - Turbocharger.MaximumBoost + 1f : */Turbocharger.MinimumBoost, Turbocharger.IsStock ? 1f : Turbocharger.MaximumBoost, Utility.InverseLerp(Engine.MinimumRPM, Turbocharger.InertiaRPM, stats.engineRPM));

			if (behaviour.IsSupercharged)
				engineBoost *= Supercharger.SuperchargerType == VehicleCharger.Supercharger.Centrifugal ? Utility.Lerp(/*Supercharger.isStock ? Supercharger.MinimumBoost - Supercharger.MaximumBoost + 1f : */Supercharger.MinimumBoost, Supercharger.IsStock ? 1f : Supercharger.MaximumBoost, Utility.InverseLerp(Engine.MinimumRPM, Supercharger.InertiaRPM, stats.engineRPM)) : Supercharger.IsStock ? 1f : Supercharger.MaximumBoost;
		}
		private void Torque()
		{
			stats.rawEngineTorque = behaviour.TorqueCurve.Evaluate(stats.engineRPM);
			stats.rawEnginePower = behaviour.PowerCurve.Evaluate(stats.engineRPM);
			stats.engineTorque = stats.rawEngineTorque * ((behaviour.IsTurbocharged || behaviour.IsSupercharged ? engineBoost : 1f) + stats.NOSBoost) * math.lerp(inputs.Fuel, inputs.RawFuel, burnoutIntensity);
			stats.enginePower = stats.rawEnginePower * ((behaviour.IsTurbocharged || behaviour.IsSupercharged ? engineBoost : 1f) + stats.NOSBoost) * math.lerp(inputs.Fuel, inputs.RawFuel, burnoutIntensity);

			if (IsElectric && behaviour.useRevLimiter)
			{
				if (stats.currentGear + 1 < transmission.GearsCount)
				{
					stats.engineTorque *= 1f - Utility.InverseLerp(Engine.RedlineRPM, Engine.OverRevRPM, stats.engineRPM) * (1f - burnoutIntensity);
					stats.enginePower *= 1f - Utility.InverseLerp(Engine.RedlineRPM, Engine.OverRevRPM, stats.engineRPM) * (1f - burnoutIntensity);
				}
				else
				{
					stats.engineTorque *= 1f - Utility.InverseLerp(Engine.RedlineRPM * 2f - Engine.OverRevRPM, Engine.RedlineRPM, stats.engineRPM) * (1f - burnoutIntensity);
					stats.enginePower *= 1f - Utility.InverseLerp(Engine.RedlineRPM * 2f - Engine.OverRevRPM, Engine.RedlineRPM, stats.engineRPM) * (1f - burnoutIntensity);
				}
			}
			else
			{
				stats.engineTorque *= 1f - Utility.InverseLerp(Engine.OverRevRPM, Engine.MaximumRPM, stats.engineRPM) * (1f - burnoutIntensity);
				stats.enginePower *= 1f - Utility.InverseLerp(Engine.OverRevRPM, Engine.MaximumRPM, stats.engineRPM) * (1f - burnoutIntensity);
			}

			float clutchTorque = stats.engineTorque * inputs.Direction * behaviour.TorqueOutputScale * Utility.Clamp01(1f - inputs.Clutch) * transmission.Efficiency;

			if (drivetrain != Train.RWD)
			{
				frontDifferential.outputAngularVelocityA = frontLeftWheels.Average(wheel => wheel.Instance.AngularVelocity);
				frontDifferential.outputAngularVelocityB = frontRightWheels.Average(wheel => wheel.Instance.AngularVelocity);
				frontDifferential.OutputInertiaA = frontLeftWheels.Average(wheel => wheel.Instance.Inertia);
				frontDifferential.OutputInertiaB = frontRightWheels.Average(wheel => wheel.Instance.Inertia);
			}

			if (drivetrain != Train.FWD)
			{
				rearDifferential.outputAngularVelocityA = rearLeftWheels.Average(wheel => wheel.Instance.AngularVelocity);
				rearDifferential.outputAngularVelocityB = rearRightWheels.Average(wheel => wheel.Instance.AngularVelocity);
				rearDifferential.OutputInertiaA = rearLeftWheels.Average(wheel => wheel.Instance.Inertia);
				rearDifferential.OutputInertiaB = rearRightWheels.Average(wheel => wheel.Instance.Inertia);
			}

			if (drivetrain == Train.AWD)
			{
				centerDifferential.inputTorque = clutchTorque;
				centerDifferential.outputAngularVelocityA = Utility.Average(frontDifferential.outputAngularVelocityA, rearDifferential.outputAngularVelocityA);
				centerDifferential.outputAngularVelocityB = Utility.Average(frontDifferential.outputAngularVelocityB, rearDifferential.outputAngularVelocityB);
				centerDifferential.OutputInertiaA = frontDifferential.OutputInertiaA + rearDifferential.OutputInertiaA;
				centerDifferential.OutputInertiaB = frontDifferential.OutputInertiaB + rearDifferential.OutputInertiaB;

				centerDifferential.Step(Time.fixedDeltaTime);
			}

			if (drivetrain != Train.RWD)
			{
				frontDifferential.inputTorque = drivetrain == Train.AWD ? centerDifferential.OutputTorqueA : clutchTorque;

				frontDifferential.Step(Time.fixedDeltaTime);
			}

			if (drivetrain != Train.FWD)
			{
				rearDifferential.inputTorque = drivetrain == Train.AWD ? centerDifferential.OutputTorqueB : clutchTorque;

				rearDifferential.Step(Time.fixedDeltaTime);
			}

			foreach (var wheel in wheels)
			{
				if (!wheel.IsMotorWheel)
				{
					if (ToolkitSettings.UsingWheelColliderPhysics && stats.currentSpeed >= 1f && math.abs(math.round(wheel.Instance.RPM)) < 1f && Utility.Round(inputs.Brake + inputs.Handbrake, 2) < .05f && Utility.Round(inputs.Fuel, 2) > .05f)
						wheel.Instance.motorTorque = .001f * rigidbody.mass;
					else
						wheel.Instance.motorTorque = default;

					continue;
				}

				float gearboxRatio = wheel.IsFrontWheel ? transmission.GetGearRatio(inputs.Direction, stats.currentGear, true) : transmission.GetGearRatio(inputs.Direction, stats.currentGear, true, true);
				float differentialTorque;

				if (wheel.IsFrontWheel)
				{
					float torqueRatio = frontCenterMotorWheelsCount > 0 ? 2f / 3f : 1f;

					differentialTorque = wheel.side switch
					{
						VehicleWheel.Side.Left => frontDifferential.OutputTorqueA * torqueRatio,
						VehicleWheel.Side.Right => frontDifferential.OutputTorqueB * torqueRatio,
						_ => frontDifferential.OutputTorque / (frontMotorWheelsCount - frontCenterMotorWheelsCount > 0 ? 3f : 1f)
					};
				}
				else
				{
					float torqueRatio = rearCenterMotorWheelsCount > 0 ? 2f / 3f : 1f;

					differentialTorque = wheel.side switch
					{
						VehicleWheel.Side.Left => rearDifferential.OutputTorqueA * torqueRatio,
						VehicleWheel.Side.Right => rearDifferential.OutputTorqueB * torqueRatio,
						_ => rearDifferential.OutputTorque / (rearMotorWheelsCount - rearCenterMotorWheelsCount > 0 ? 3f : 1f)
					};
				}

				wheel.Instance.motorTorque = gearboxRatio * differentialTorque;

				TCS(wheel.Instance);
			}
		}
		private void ChassisTorque()
		{
			if (chassis.engineChassisTorque == VehicleEngineChassisTorque.Off || IsElectric)
				return;

			var enginePosition = chassis.EnginePosition;

			if (enginePosition == VehicleEngine.EnginePosition.Front && drivetrain == Train.FWD || enginePosition == VehicleEngine.EnginePosition.Rear && drivetrain == Train.RWD)
				return;

			float torqueMultiplier = enginePosition == VehicleEngine.EnginePosition.MidRear ? .5f : 1f;
			float chassisTorque = stats.engineTorque * torqueMultiplier * inputs.Clutch;

			rigidbody.AddRelativeTorque(chassisTorque * chassis.EngineChassisTorqueMultiplier * Vector3.forward);
		}
#if !MVC_COMMUNITY
		private void Fuel()
		{
			if (!Settings.useFuelSystem)
				return;

			float wheelsSpeed = math.abs(stats.averageMotorWheelsSpeed);
			float wheelsRPM = math.abs(stats.averageMotorWheelsRPM);
			float fuelRPMFactor = Utility.InverseLerpUnclamped(IsElectric ? 4000f : 2000f, IsElectric ? 10000f : 5000f, stats.engineRPM);
			float fuelSpeedFactor = Utility.InverseLerpUnclamped(IsElectric ? 40f : 110f, IsElectric ? 110f : 40f, wheelsSpeed) * Utility.Clamp01(1f - inputs.Clutch);
			float fuelConsumption = math.max(Utility.LerpUnclamped(behaviour.FuelConsumptionCity, behaviour.FuelConsumptionHighway, Utility.Average(fuelRPMFactor, fuelSpeedFactor)), 0f);
			bool useRegenerativeFuel = IsElectric && behaviour.UseRegenerativeBrakes;
			float regeneratedConsumption = useRegenerativeFuel ? wheelsRPM * Utility.Average(frontBrakes.BrakeTorque / frontBrakes.Density, rearBrakes.BrakeTorque / rearBrakes.Density) / 5000f : 0f;

			stats.fuelConsumption = inputs.Fuel * fuelConsumption + inputs.Brake * -regeneratedConsumption;

			if (stats.fuelTank > 0f && stats.fuelTank <= behaviour.FuelCapacity)
				stats.fuelTank -= stats.fuelConsumption * wheelsSpeed * Time.fixedDeltaTime / 100000f;

			stats.fuelTank = math.clamp(stats.fuelTank, 0f, behaviour.FuelCapacity);
		}
#endif

		#endregion

		#region Stability & Handling

		private void AntiSwayBars()
		{
			if (stats.isStationary)
				return;

			if (!stats.isGrounded)
			{
				if (Settings.useAirAntiRoll)
				{
					Vector3 planarVelocity = rigidbody.
#if UNITY_6000_0_OR_NEWER
						linearVelocity;
#else
						velocity;
#endif

					if (Mathf.Approximately(planarVelocity.x, 0f) && Mathf.Approximately(planarVelocity.z, 0f))
						return;

					float antiRollFactor = Settings.airAntiRollIntensity * Time.fixedDeltaTime * Utility.InverseLerp(0f, 90f, math.abs(transform.eulerAngles.x)) / math.max(Utility.Max(math.abs(rigidbody.angularVelocity.x), math.abs(rigidbody.angularVelocity.y), math.abs(rigidbody.angularVelocity.z)) * 6.853526069776f, 1f);

					planarVelocity.y = 0f;
					transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(planarVelocity.normalized), antiRollFactor);
					rigidbody.angularVelocity = Vector3.Lerp(rigidbody.angularVelocity, Vector3.zero, antiRollFactor);
				}

				return;
			}

			if (!stability.useAntiSwayBars || leftWheels.Length < 1 || rightWheels.Length < 1 || stability.AntiSwayFront <= 0f && stability.AntiSwayRear <= 0f)
				return;

			float travelFL = 1f;
			float travelFR = 1f;
			float travelRL = 1f;
			float travelRR = 1f;

			if (stats.groundedFrontLeftWheelsCount > 0)
				travelFL = Utility.Clamp01(-(stats.averageFrontLeftWheelsHitPoint.y + stats.averageFrontLeftWheelsRadius) * (frontLeftWheels.Length / stats.groundedFrontLeftWheelsCount) / (frontSuspension.Length * (1f + frontSuspension.LengthStance)));

			if (stats.groundedFrontRightWheelsCount > 0)
				travelFR = Utility.Clamp01(-(stats.averageFrontRightWheelsHitPoint.y + stats.averageFrontRightWheelsRadius) * (frontRightWheels.Length / stats.groundedFrontRightWheelsCount) / (frontSuspension.Length * (1f + frontSuspension.LengthStance)));

			if (stats.groundedRearLeftWheelsCount > 0)
				travelRL = Utility.Clamp01(-(stats.averageRearLeftWheelsHitPoint.y + stats.averageRearLeftWheelsRadius) * (rearLeftWheels.Length / stats.groundedRearLeftWheelsCount) / (rearSuspension.Length * (1f + rearSuspension.LengthStance)));

			if (stats.groundedRearRightWheelsCount > 0)
				travelRR = Utility.Clamp01(-(stats.averageRearRightWheelsHitPoint.y + stats.averageRearRightWheelsRadius) * (rearRightWheels.Length / stats.groundedRearRightWheelsCount) / (rearSuspension.Length * (1f + rearSuspension.LengthStance)));

			float frontSlip = math.abs(stats.averageFrontWheelsSidewaysSlip);
			float antiSwayForceFront = (travelFL - travelFR) * stability.AntiSwayFront;
			float frontForceClamp = Utility.Lerp(0f, -stability.AntiSwayFront * .5f, frontSlip);
			float forceFL = math.max(antiSwayForceFront, frontForceClamp);
			float forceFR = math.max(-antiSwayForceFront, frontForceClamp);

			float rearSlip = math.abs(stats.averageRearWheelsSidewaysSlip);
			float antiSwayForceRear = (travelRL - travelRR) * stability.AntiSwayRear;
			float rearForceClamp = Utility.Lerp(0f, -stability.AntiSwayRear * .5f, rearSlip);
			float forceRL = math.max(antiSwayForceRear, rearForceClamp);
			float forceRR = math.max(-antiSwayForceRear, rearForceClamp);

			if (stats.groundedFrontLeftWheelsCount > 0)
				rigidbody.AddForceAtPosition(-forceFL * transform.up, Utility.Lerp(LeftWheelsPosition, FrontLeftWheelsPosition, frontSlip));

			if (stats.groundedFrontRightWheelsCount > 0)
				rigidbody.AddForceAtPosition(-forceFR * transform.up, Utility.Lerp(RightWheelsPosition, FrontRightWheelsPosition, frontSlip));

			if (stats.groundedRearLeftWheelsCount > 0)
				rigidbody.AddForceAtPosition(-forceRL * transform.up, RearLeftWheelsPosition);

			if (stats.groundedRearRightWheelsCount > 0)
				rigidbody.AddForceAtPosition(-forceRR * transform.up, RearRightWheelsPosition);
		}
		private void ABS()
		{
			if (trailerInstance && !trailerInstance.useBrakes)
				return;

			foreach (var wheel in wheels)
				ABS(wheel.Instance);
		}
		private void ABS(VehicleWheel wheel)
		{
			stats.isABSActive = !stats.isStationary && stability.useABS;

			if (!stats.isABSActive)
				return;

			wheel.wheelCollider.GetGroundHit(out wheel.HitInfo);

			float slip = math.abs(wheel.HitInfo.forwardSlip) * Utility.Clamp01(inputs.Brake + (stability.useHandbrakeABS ? inputs.Handbrake : 0f)) * (1f - burnoutIntensity);

			stats.isABSActive = slip > stability.ABSThreshold;

			if (!stats.isABSActive)
				return;

			float groundSlipValue = ToolkitSettings.UsingWheelColliderPhysics ? wheel.CurrentWheelColliderForwardFriction.extremumSlip : .5f;

			wheel.brakeTorque *= stability.ABSThreshold < groundSlipValue ? 1f - Utility.InverseLerp(stability.ABSThreshold, groundSlipValue, slip) : 1f;
		}
		private void ESP()
		{
			if (trailerInstance)
				return;

			Vector3 velocity = rigidbody.
#if UNITY_6000_0_OR_NEWER
				linearVelocity;
#else
				velocity;
#endif
			float angle = Vector3.SignedAngle(math.normalizesafe(velocity, transform.forward), transform.forward * Mathf.Sign(inputs.Direction), transform.up);
			float steerAngle = stats.steerAngle;

			if (Settings.counterSteerType != ToolkitSettings.CounterSteerType.VisualsOnly)
				ApplyCounterSteerHelper(ref steerAngle);

			angle -= steerAngle * .5f;
			stats.isESPActive = stability.useESP && !stats.isStationary && !stats.isRevLimiting && math.abs(angle) >= 2f && stats.currentSpeed >= stability.ESPSpeedThreshold && leftWheels.Length > 0 && rightWheels.Length > 0;

			if (!stats.isESPActive)
				return;

			var frontBrakeTorque = frontBrakes.BrakeTorque;
			var rearBrakeTorque = rearBrakes.BrakeTorque;

			angle = math.clamp(angle / 90f, -1f, 1f);

			foreach (var wheel in wheels)
			{
				if (wheel.side == VehicleWheel.Side.Middle || !wheel.Instance.IsGrounded)
					continue;

				bool isFrontWheel = wheel.IsFrontWheel;
				int wheelsAxisCount = isFrontWheel ? frontWheels.Length : rearWheels.Length;
				float brakeTorque = (isFrontWheel ? frontBrakeTorque : rearBrakeTorque) / wheelsAxisCount;

				wheel.Instance.brakeTorque = math.clamp(wheel.Instance.brakeTorque - angle * math.sign(wheel.Model.localPosition.x) * brakeTorque * stability.ESPStrength, 0f, brakeTorque);
			}
		}
		private void TCS(VehicleWheel wheel)
		{
			stats.isTCSActive = stats.isEngineRunning && !stats.isStationary && stability.useTCS;

			if (!stats.isTCSActive)
				return;

			float slip = wheel.HitInfo.forwardSlip * (1f - burnoutIntensity) * inputs.RawFuel;

			stats.isTCSActive = slip > stability.TCSThreshold;

			if (!stats.isTCSActive)
				return;

			wheel.motorTorque *= 1f - Utility.InverseLerp(stability.TCSThreshold, 1f, slip);
		}
		private void Burnouts()
		{
			if (stats.isStationary)
				return;

			burnoutIntensity = (drivetrain == Train.FWD || drivetrain == Train.RWD) && (stability.TCSAllowBurnouts || !stability.useTCS) && (Settings.burnoutsForAutomaticTransmissions || transmission.Gearbox == TransmissionModule.GearboxType.Manual) && !trailerInstance ? inputs.RawFuel * inputs.Brake * (1f - inputs.Clutch) : 0f;
		}
		private void WeightDistribution()
		{
			if (weightDistribution == stability.WeightDistribution && weightHeight == stability.WeightHeight && (!Turbocharger || Turbocharger.WeightDistributionDifference != 0f) && (!Supercharger || Supercharger.WeightDistributionDifference != 0f) && (!rigidbody || rigidbody.centerOfMass != Vector3.zero))
				return;

			weightDistribution = stability.WeightDistribution;
			weightHeight = stability.WeightHeight;

			if (Turbocharger && !Turbocharger.IsStock)
				weightDistribution += Turbocharger.WeightDistributionDifference;

			if (Supercharger && !Supercharger.IsStock)
				weightDistribution += Supercharger.WeightDistributionDifference;

			if (!rigidbody || !Chassis)
				return;

			Vector3 COM = Vector3.zero;

			COM.y = Utility.Lerp(COMInterval.y.Min, COMInterval.y.Max, weightHeight);
			COM.z = Utility.Lerp(COMInterval.x.Max, COMInterval.x.Min, weightDistribution);
			rigidbody.centerOfMass = COM;
		}
		private void ArcadeSteerHelpers()
		{
			if (!stability.useArcadeSteerHelpers || stats.isStationary)
				return;

			if (stats.groundedSteerWheelsCount < 1 || stats.groundedNonSteerWheelsCount < 1)
				return;

			float velocityAngle = rigidbody.angularVelocity.y * math.clamp(stats.localVelocity.z, -1f, 1f) * Mathf.Rad2Deg;

			velocityDirection = Quaternion.Lerp(velocityDirection, Quaternion.AngleAxis(math.clamp(velocityAngle / 3f, -Settings.maximumSteerAngle, Settings.maximumSteerAngle), Vector3.up), 40f * Time.fixedDeltaTime);
			steerDirection = Quaternion.Euler(0f, stats.steerAngle * stats.groundedSteerWheelsCount / steerWheels.Length, 0f);

			int normalizer = steerDirection.y > velocityDirection.y ? 1 : -1;
			float angle = Quaternion.Angle(velocityDirection, steerDirection) * normalizer;

			rigidbody.AddRelativeTorque((math.clamp(stats.localVelocity.z, -10f, 10f) / 250f) * angle * stability.ArcadeAngularSteerHelperIntensity * Vector3.up, ForceMode.Impulse);

			if (math.abs(oldYEulerAngle - transform.eulerAngles.y) < 10f)
			{
				float turnAdjust = (transform.eulerAngles.y - oldYEulerAngle) * stability.ArcadeLinearSteerHelperIntensity * .5f * (stats.groundedSteerWheelsCount / steerWheels.Length);
				Quaternion velRotation = Quaternion.AngleAxis(turnAdjust, transform.up);
				Vector3 velocity = rigidbody.
#if UNITY_6000_0_OR_NEWER
					linearVelocity;
#else
					velocity;
#endif

				rigidbody.
#if UNITY_6000_0_OR_NEWER
					linearVelocity
#else
					velocity
#endif
					= velRotation * velocity;
			}

			oldYEulerAngle = transform.eulerAngles.y;
		}
		private void Aerodynamics()
		{
			rigidbody.
#if UNITY_6000_0_OR_NEWER
				linearDamping = default;
#else
				drag = default;
#endif

			if (Settings.useDrag)
				rigidbody.AddForce(-(stability.Drag * stability.DragScale * stats.currentSpeed / 3.6f) * math.normalizesafe(stats.velocity, transform.forward));

			if (Settings.useDownforce && (Settings.useDownforceWhenNotGrounded || stats.isGrounded))
			{
				if (!trailerInstance)
					rigidbody.AddForceAtPosition((Settings.useDownforceWhenNotGrounded ? 1f : stats.groundedFrontWheelsCount / frontWheels.Length) * .01f * stats.currentSpeed * stability.FrontDownforce * -transform.up, FrontWheelsPosition);

				rigidbody.AddForceAtPosition((Settings.useDownforceWhenNotGrounded ? 1f : stats.groundedRearWheelsCount / rearWheels.Length) * .01f * stats.currentSpeed * stability.RearDownforce * -transform.up, RearWheelsPosition);
			}
		}
		private void Donuts()
		{
			if (!ToolkitSettings.UsingWheelColliderPhysics || Steertrain == Train.AWD && !steering.invertRearSteer || stats.isStationary || !stats.isGrounded || Mathf.Approximately(stability.HandlingRate, 0f))
				return;

			float newDonutIntensity = 1f - Utility.Clamp01(Mathf.Sign(inputs.Direction) * stats.currentSpeed / 60f);

			newDonutIntensity *= inputs.RawFuel;
			newDonutIntensity *= Utility.Clamp01(stats.averageMotorWheelsSpeed / 60f) * (stats.steerAngle / steering.MaximumSteerAngle) * (stats.wheelPower / math.max(Engine.Power, stats.rawEnginePower));
			newDonutIntensity *= (float)stats.groundedSteerWheelsCount / steerWheels.Length;
			newDonutIntensity *= (float)stats.groundedMotorWheelsCount / motorWheels.Length;

			float newDonutDirection = Utility.Clamp01(Mathf.Sign(stats.localAngularVelocity.y) * Mathf.Sign(stats.steerAngle));

			donutDirection = Utility.Lerp(donutDirection, newDonutDirection, Time.fixedDeltaTime);
			newDonutIntensity *= donutDirection;
			newDonutIntensity *= stats.engineRPM / behaviour.PeakTorqueRPM;
			newDonutIntensity /= math.max(math.abs(stats.localAngularVelocity.y), .1f);
			newDonutIntensity *= motorWheels.Length * .5f;

			if (Drivetrain == Train.AWD)
			{
				newDonutIntensity *= rearWheels.Length / frontWheels.Length;
				newDonutIntensity += (centerDifferential.BiasAB - .5f) * 2f;
			}

			donutIntensity = Utility.Lerp(donutIntensity, newDonutIntensity, math.pow(1.618f, donutIntensity > newDonutIntensity ? 2.617924f : 1f) * Time.fixedDeltaTime);

			Vector3 position;

			if (Drivetrain == Train.RWD && Steertrain != Train.AWD || Steertrain == Train.RWD)
				position = Vector3.Lerp(Steertrain == Train.RWD && steering.invertRearSteer ? FrontRightWheelsPosition : FrontLeftWheelsPosition, Steertrain == Train.RWD && steering.invertRearSteer ? FrontLeftWheelsPosition : FrontRightWheelsPosition, Utility.Average(1f, inputs.SteeringWheel));
			else
				position = Vector3.Lerp(LeftWheelsPosition, RightWheelsPosition, Utility.Average(1f, inputs.SteeringWheel));

			Vector2 randomPosition = .1f * Utility.Distance(LeftWheelsPosition, RightWheelsPosition) * UnityEngine.Random.insideUnitSphere;

			position += transform.right * randomPosition.x + transform.forward * randomPosition.y;
			donutIntensity *= 1f - (rigidbody.centerOfMass.y / .5f);

			Utility.AddTorqueAtPosition(rigidbody, stats.averageMotorWheelsFrictionStiffnessSqr * donutIntensity * stability.HandlingRate * transform.up / orgAverageMotorWheelsFrictionStiffnessSqr, position, ForceMode.Acceleration);
		}
		private void Drift()
		{
			if (!ToolkitSettings.UsingWheelColliderPhysics || Steertrain == Train.AWD && !steering.invertRearSteer || stats.isStationary || !stats.isGrounded || Mathf.Approximately(stability.HandlingRate, 1f))
				return;

			float rearWheelsForwardSlip = stats.averageRearWheelsSmoothForwardSlip;
			float rearWheelsSidewaysSlip = stats.averageRearWheelsSmoothSidewaysSlip;
			float sqrRearWheelsForwardSlip = rearWheelsForwardSlip * rearWheelsForwardSlip * Mathf.Sign(rearWheelsForwardSlip);
			float sqrRearWheelsSidewaysSlip = rearWheelsSidewaysSlip * rearWheelsSidewaysSlip * Mathf.Sign(rearWheelsSidewaysSlip);
			float groundedMotorWheelsMultiplier = stats.groundedMotorWheelsCount / motorWheels.Length;
			float groundedSteerWheelsMultiplier = stats.groundedMotorWheelsCount / motorWheels.Length;
			Vector3 totalForce = math.abs(sqrRearWheelsSidewaysSlip) * Utility.Clamp01(math.abs(sqrRearWheelsForwardSlip * 10f)) * inputs.Direction * groundedMotorWheelsMultiplier * (7f / 3f) * transform.forward;

			totalForce += sqrRearWheelsSidewaysSlip * Utility.Clamp01(math.abs(math.clamp(sqrRearWheelsForwardSlip, .5f, 1f) * 10f)) * inputs.Direction * groundedSteerWheelsMultiplier * (2f / 3f) * transform.right;
			totalForce *= 1f - stability.HandlingRate;

			rigidbody.AddRelativeTorque(inputs.SteeringWheel * inputs.Direction * (1f - stability.HandlingRate) * groundedSteerWheelsMultiplier * Vector3.up, ForceMode.Acceleration);
			rigidbody.AddForceAtPosition(totalForce, rigidbody.worldCenterOfMass, ForceMode.Acceleration);
		}

		#endregion

		#endregion

		#region Late Update

		private void LateUpdate()
		{
			if (!Awaken)
				return;

			RefreshWeatherZones();

			if (!trailerInstance)
			{
				bool updateInputs = false;

				if (isAI)
					updateInputs = UpdateAI(VehicleAIBehaviour.UpdateType.LateUpdate);

				if (updateInputs)
					inputs.Update(this);

				sfx.Update();
#if !MVC_COMMUNITY

				if (Settings.useInterior)
					interior.Update();
#endif
			}

			vfx.Update();
		}

		private void RefreshWeatherZones()
		{
			stats.inDarknessWeatherZone = false;
			stats.inFogWeatherZone = false;

			for (int i = 0; i < darknessWeatherZones.Count; i++)
			{
				var zone = darknessWeatherZones[i];

				stats.inDarknessWeatherZone = zone && zone.collider && zone.collider.bounds.Intersects(WorldBounds);

				if (stats.inDarknessWeatherZone)
					break;
			}

			for (int i = 0; i < fogWeatherZones.Count; i++)
			{
				var zone = fogWeatherZones[i];

				stats.inFogWeatherZone = zone && zone.collider && zone.collider.bounds.Intersects(WorldBounds);

				if (stats.inFogWeatherZone)
					break;
			}
		}

		#endregion

		#region Collisions, Damage & Triggers

		private void OnCollisionEnter(Collision collision)
		{
			if (!Awaken)
				return;

			float3 relativeVelocity = math.abs(collision.relativeVelocity);
			float velocityMagnitudeSqr = math.lengthsq(relativeVelocity);

			if (Manager.PlayerVehicle == this && velocityMagnitudeSqr > Settings.damageMediumVelocity * Settings.damageMediumVelocity)
				Follower.ApplyCollisionShaking(new(math.clamp(relativeVelocity.x / 16f, 0f, math.min(Stats.lastAirTime * .5f, 2f)), math.clamp(math.max(relativeVelocity.y, relativeVelocity.z) / 8f, 0f, math.min(Stats.lastAirTime, 2f))));

			if (velocityMagnitudeSqr <= 1f)
				return;

			Vector3 collisionPosition = Utility.Average(collision.contacts.Select(contact => contact.point));

			if (collision.collider.gameObject.layer == Settings.vehiclesLayer)
				sfx.CarImpact(velocityMagnitudeSqr, collisionPosition);
			else
				sfx.WallImpact(velocityMagnitudeSqr, collisionPosition);

			if (Settings.useParticleSystems)
				vfx.CreateCollisionSpark(collision);
		}
		private void OnCollisionExit(Collision collision)
		{
			if (Settings.useParticleSystems)
				vfx.DestroyCollisionSpark(collision.transform);
		}
		private void OnTriggerEnter(Collider other)
		{
			if (!Awaken)
				return;

			if (!trailerInstance)
			{
				if (other.TryGetComponent(out VehicleWeatherZone weatherZone))
				{
					switch (weatherZone.zoneType)
					{
						case VehicleWeatherZone.WeatherZoneType.Darkness:
							if (darknessWeatherZones.IndexOf(weatherZone) < 0)
								darknessWeatherZones.Add(weatherZone);

							break;

						case VehicleWeatherZone.WeatherZoneType.Fog:
							if (fogWeatherZones.IndexOf(weatherZone) < 0)
								fogWeatherZones.Add(weatherZone);

							break;
					}

					return;
				}
			}

			if (other.TryGetComponent(out VehicleDamageZone damageZone))
				foreach (var wheel in wheels)
					wheel.Instance.AddDamageZone(damageZone);
		}
		private void OnTriggerExit(Collider other)
		{
			if (!Awaken)
				return;

			if (other.TryGetComponent(out VehicleWeatherZone weatherZone))
				switch (weatherZone.zoneType)
				{
					case VehicleWeatherZone.WeatherZoneType.Darkness:
						darknessWeatherZones.Remove(weatherZone);

						break;

					case VehicleWeatherZone.WeatherZoneType.Fog:
						fogWeatherZones.Remove(weatherZone);

						break;
				}
			else if (other.TryGetComponent(out VehicleDamageZone damageZone))
				foreach (var wheel in wheels)
					wheel.Instance.RemoveDamageZone(damageZone);
		}
		/*private void DeformMesh(MeshFilter meshFilter, Vector3[] orgVertices, Collision collision, float cos)
		{
			if (!ToolkitSettings.IsPlusVersion || !Settings.useDamage)
				return;

			Vector3[] vertices = meshFilter.mesh.vertices;

			foreach (ContactPoint contact in collision.contacts)
			{
				Vector3 point = meshFilter.transform.InverseTransformPoint(contact.point);
				float radius = Damage.overrideRadius ? Damage.radius : Settings.damageRadius;
				float verticesRandomization = Damage.overrideVertexRandomization ? Damage.vertexRandomization : Settings.damageVertexRandomization;

				for (int i = 0; i < vertices.Length; i++)
					if (Utility.Distance(point, vertices[i]) < radius)
					{
						vertices[i] += (cos * damageLocalVector * (radius - Utility.Distance(point, vertices[i])) / radius) + new(math.sin(vertices[i].y * 1000f), math.sin(vertices[i].z * 1000f), math.sin(vertices[i].x * 100f)).normalized * (verticesRandomization / 500f);

						if (Settings.maximumDamage > 0f && Utility.Distance(vertices[i], orgVertices[i]) > Settings.maximumDamage)
							vertices[i] = orgVertices[i] + (vertices[i] - orgVertices[i]).normalized * Settings.maximumDamage;
					}
			}

			meshFilter.mesh.vertices = vertices;

			meshFilter.mesh.RecalculateNormals();
			meshFilter.mesh.RecalculateBounds();
			meshFilter.mesh.Optimize();
		}*/

		#endregion

		#region Enable, Destroy & Reset

		private void OnEnable()
		{
			Awake();
		}
		private void OnDisable()
		{
			if (!stats.isEngineStarting)
				return;

			if (sfx.engineStartingSource && !stats.isEngineRunning)
				sfx.engineStartingSource.StopAndDisable();

			stats.isEngineStarting = false;
		}
		private void OnDestroy()
		{
			if (Rigidbody)
				Destroy(rigidbody);

			sfx?.Dispose();

			if (grounds.IsCreated)
				grounds.Dispose();

#if !MVC_COMMUNITY
			if (groundsAppearance.IsCreated)
				groundsAppearance.Dispose();
#endif

			if (wheelsAccess.IsCreated)
				wheelsAccess.Dispose();

			if (wheelStats.IsCreated)
				wheelStats.Dispose();

			behaviourAccess.Dispose();
			transmissionAccess.Dispose();
		}
		private void Reset()
		{
			if (!IsSetupDone)
				return;

			GetOrCreateRigidbody();
		}

		#endregion

		#endregion

		#endregion
	}
}

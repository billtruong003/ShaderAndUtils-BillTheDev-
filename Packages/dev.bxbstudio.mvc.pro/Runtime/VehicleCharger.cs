#region Namespaces

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Utilities;
using MVC.Utilities.Internal;

#endregion

namespace MVC.Base
{
	[Serializable]
	public class VehicleCharger : ToolkitComponent
	{
		#region Modules & Enumerators

		#region Enumerators

		public enum ChargerType { Turbocharger, Supercharger }
		public enum TurbochargerCount { Single = 1, Twin = 2, Quad = 4 }
		public enum Supercharger { Centrifugal, Roots, TwinScrew }

		#endregion

		#region Modules

		[Serializable]
		public struct AudioModule
		{
			#region Variables

			public AudioClip idleClip;
			public AudioClip activeClip;
			public AudioClip fuelBlowoutClip;
			public AudioClip gearBlowoutClip;
			public AudioClip revBlowoutClip;

			#endregion

			#region Constructors

			public AudioModule(AudioModule module)
			{
				idleClip = module.idleClip;
				activeClip = module.activeClip;
				fuelBlowoutClip = module.fuelBlowoutClip;
				gearBlowoutClip = module.gearBlowoutClip;
				revBlowoutClip = module.revBlowoutClip;
			}

			#endregion
		}

		#endregion

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
					value = "New Charger";

				string prefix = default;

				if (Settings && Settings.Chargers != null && Settings.Chargers.Length > 0)
					for (int i = 0; Array.Find(Settings.Chargers, charger => charger != this && charger.Name.ToUpper() == $"{value.ToUpper()}{prefix}"); i++)
						prefix = i > 0 ? $" ({i})" : "";

				name = $"{value}{prefix}";

				RefreshValidity();
			}
		}
		public bool IsStock
		{
			get
			{
				return isStock;
			}
			set
			{
				isStock = value;

				RefreshValidity();
			}
		}
		public float MassDifference
		{
			get
			{
				return massDifference;
			}
			set
			{
				massDifference = CompatibleEngineIndexes.Length > 0 ? value : math.clamp(value, -MinimumEnginesMass, MaximumEnginesRPM);
			}
		}
		public float WeightDistributionDifference
		{
			get
			{
				if (IsStock)
					return 0f;

				return weightDistributionDifference;
			}
			set
			{
				if (IsStock)
					return;

				weightDistributionDifference = math.clamp(value, -1f, 1f);
			}
		}
		public ChargerType Type
		{
			get
			{
				return type;
			}
			set
			{
				type = value;

				RefreshValidity();
			}
		}
		public TurbochargerCount TurboCount
		{
			get
			{
				return turboCount;
			}
			set
			{
				if ((int)value == 3)
					value = TurbochargerCount.Quad;

				turboCount = (TurbochargerCount)math.clamp((int)value, 1, 4);

				RefreshValidity();
			}
		}
		public Supercharger SuperchargerType
		{
			get
			{
				return superchargerType;
			}
			set
			{
				superchargerType = value;

				RefreshValidity();
			}
		}
		public float MinimumBoost
		{
			get
			{
				return minimumBoost;
			}
			set
			{
				minimumBoost = math.clamp(value, 0f, 2f);

				RefreshValidity();
			}
		}
		public float MaximumBoost
		{
			get
			{
				return maximumBoost;
			}
			set
			{
				maximumBoost = math.clamp(value, 1f, 3f);

				RefreshValidity();
			}
		}
		public float InertiaRPM
		{
			get
			{
				return inertiaRPM;
			}
			set
			{
				inertiaRPM = value;

				RefreshValidity();
			}
		}
		public float ChargerSize
		{
			get
			{
				return chargerSize;
			}
			set
			{
				chargerSize = math.clamp(value, 0f, 5f);

				RefreshValidity();
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
		public int[] CompatibleEngineIndexes
		{
			get
			{
				compatibleEngineIndexes ??= new int[] { };

				if (compatibleEnginesCount != compatibleEngineIndexes.Length)
					minimumEnginesMass = maximumEnginesMass = minimumEnginesRPM = redlineEnginesRPM = overRevEnginesRPM = maximumEnginesRPM = default;

				return compatibleEngineIndexes;
			}
		}
		public VehicleEngine[] CompatibleEngines
		{
			get
			{
				if (Settings && (compatibleEngines == null || compatibleEngines.Length != CompatibleEngineIndexes.Length))
				{
					compatibleEngines = new VehicleEngine[CompatibleEngineIndexes.Length];

					for (int i = 0; i < compatibleEngines.Length; i++)
						compatibleEngines[i] = Settings.Engines[CompatibleEngineIndexes[i]];
				}

				return compatibleEngines;
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
		public bool IsCompatible
		{
			get
			{
				if (!isCompatible)
					RefreshValidity();

				return isCompatible;
			}
		}
		public bool HasIssues
		{
			get
			{
				if (hasIssues)
					RefreshValidity();

				return hasIssues;
			}
		}
		public float MinimumEnginesMass
		{
			get
			{
				if ((CompatibleEngineIndexes.Length > 0 || Settings && Settings.Engines?.Length > 0) && minimumEnginesMass == 0f)
					RefreshEngines();

				return minimumEnginesMass;
			}
		}
		public float MaximumEnginesMass
		{
			get
			{
				if ((CompatibleEngineIndexes.Length > 0 || Settings && Settings.Engines?.Length > 0) && (maximumEnginesMass == 0f || maximumEnginesMass == math.INFINITY))
					RefreshEngines();

				return maximumEnginesMass;
			}
		}
		public float MinimumEnginesRPM
		{
			get
			{
				if ((CompatibleEngineIndexes.Length > 0 || Settings && Settings.Engines?.Length > 0) && minimumEnginesRPM == 0f)
					RefreshEngines();

				return minimumEnginesRPM;
			}
		}
		public float RedlineEnginesRPM
		{
			get
			{
				if ((CompatibleEngineIndexes.Length > 0 || Settings && Settings.Engines?.Length > 0) && (redlineEnginesRPM == 0f || redlineEnginesRPM == math.INFINITY))
					RefreshEngines();

				return redlineEnginesRPM;
			}
		}
		public float OverRevEnginesRPM
		{
			get
			{
				if ((CompatibleEngineIndexes.Length > 0 || Settings && Settings.Engines?.Length > 0) && (overRevEnginesRPM == 0f || overRevEnginesRPM == math.INFINITY))
					RefreshEngines();

				return overRevEnginesRPM;
			}
		}
		public float MaximumEnginesRPM
		{
			get
			{
				if ((CompatibleEngineIndexes.Length > 0 || Settings && Settings.Engines?.Length > 0) && (maximumEnginesRPM == 0f || maximumEnginesRPM == math.INFINITY))
					RefreshEngines();

				return maximumEnginesRPM;
			}
		}

		[SerializeField]
		private string name;
		[SerializeField]
		private bool isStock;
		[SerializeField]
		private float massDifference;
		[SerializeField]
		private float weightDistributionDifference;
		[SerializeField]
		private ChargerType type = ChargerType.Turbocharger;
		[SerializeField]
		private TurbochargerCount turboCount = TurbochargerCount.Single;
		[SerializeField]
		private Supercharger superchargerType = Supercharger.Roots;
		[SerializeField]
		private float minimumBoost = 1.25f;
		[SerializeField]
		private float maximumBoost = .75f;
		[SerializeField]
		private float inertiaRPM = 2000f;
		[SerializeField]
		private float chargerSize = 1f;
		[SerializeField]
		private AudioModule audio;
		[SerializeField]
		private int[] compatibleEngineIndexes = new int[] { };
		private VehicleEngine[] compatibleEngines;
		private float minimumEnginesMass;
		private float maximumEnginesMass;
		private float minimumEnginesRPM;
		private float redlineEnginesRPM;
		private float overRevEnginesRPM;
		private float maximumEnginesRPM;
		private bool isValid;
		private bool isCompatible;
		private bool hasIssues;
		private int compatibleEnginesCount;

		#endregion

		#region Methods

		public void AddCompatibleEngine(int engineIndex)
		{
			if (!Settings || HasCompatibleEngine(engineIndex))
				return;

			if (engineIndex < 0 || engineIndex >= Settings.Engines.Length)
			{
				ToolkitDebug.Error("We couldn't add the requested engine to the charger compatibility list due to it's in-existence!");

				return;
			}
			else if (Settings.Engines[engineIndex].IsElectric)
			{
				ToolkitDebug.Warning("The requested engine can't be added since it is electric!");

				return;
			}

			Array.Resize(ref compatibleEngineIndexes, CompatibleEngineIndexes.Length + 1);

			compatibleEngineIndexes[^1] = engineIndex;
			compatibleEngines = CompatibleEngines;

			RefreshEngines();
		}
		public void SetCompatibleEngine(int index, int engineIndex)
		{
			if (!Settings)
				return;

			if (index < 0 || index >= CompatibleEngineIndexes.Length)
			{
				ToolkitDebug.Error("We couldn't get the requested engine in the charger's compatibility list, seems like the index is out of range!");

				return;
			}
			else if (engineIndex < 0 || engineIndex >= Settings.Engines.Length)
			{
				ToolkitDebug.Error("We couldn't find the requested new engine in the engines list due to it's in-existence!");

				return;
			}
			else if (Settings.Engines[engineIndex].IsElectric)
			{
				ToolkitDebug.Warning("The requested engine can't be added since it is electric!");

				return;
			}

			compatibleEngineIndexes[index] = engineIndex;

			RefreshEngines();
		}
		public bool HasCompatibleEngine(int engineIndex)
		{
			return Array.Exists(CompatibleEngineIndexes, index => index == engineIndex);
		}
		public int GetCompatibleEngineIndex(int engineIndex)
		{
			if (CompatibleEngineIndexes.Length < 1)
				return -1;

			for (int i = 0; i < CompatibleEngineIndexes.Length; i++)
				if (CompatibleEngineIndexes[i] == engineIndex)
					return i;

			return -1;
		}
		public void RemoveCompatibleEngineAtIndex(int index)
		{
			if (index < 0 || index >= CompatibleEngineIndexes.Length)
			{
				ToolkitDebug.Error("We couldn't remove the requested engine from the charger compatibility list, seems like the index is out of range!");

				return;
			}

			List<int> engineIndexesList = CompatibleEngineIndexes.ToList();
			List<VehicleEngine> enginesList = CompatibleEngines.ToList();

			engineIndexesList.RemoveAt(index);
			enginesList.RemoveAt(index);

			compatibleEngineIndexes = engineIndexesList.ToArray();
			compatibleEngines = enginesList.ToArray();

			RefreshEngines();
		}
		public void ResetCompatibleEngines()
		{
			compatibleEngineIndexes = new int[] { };
			compatibleEngines = new VehicleEngine[] { };
		}

		private void RefreshEngines()
		{
			VehicleEngine[] engines = Settings && Settings.Engines?.Length > 0 ? Settings.Engines.Where(engine => !engine.IsElectric).ToArray() : null;

			minimumEnginesMass = CompatibleEngines?.Length > 0 ? CompatibleEngines.Min(engine => engine.Mass) : engines?.Length > 0 ? engines.Min(engine => engine.Mass) : default;
			maximumEnginesMass = CompatibleEngines?.Length > 0 ? CompatibleEngines.Max(engine => engine.Mass) : engines?.Length > 0 ? engines.Max(engine => engine.Mass) : math.INFINITY;
			minimumEnginesRPM = CompatibleEngines?.Length > 0 ? CompatibleEngines.Max(engine => engine.MinimumRPM) : engines?.Length > 0 ? math.min(engines.Max(engine => engine.MinimumRPM), 2000) : default;
			redlineEnginesRPM = CompatibleEngines?.Length > 0 ? CompatibleEngines.Min(engine => engine.RedlineRPM) : engines?.Length > 0 ? engines.Min(engine => engine.RedlineRPM) : math.INFINITY;
			overRevEnginesRPM = CompatibleEngines?.Length > 0 ? CompatibleEngines.Min(engine => engine.OverRevRPM) : engines?.Length > 0 ? engines.Min(engine => engine.OverRevRPM) : math.INFINITY;
			maximumEnginesRPM = CompatibleEngines?.Length > 0 ? CompatibleEngines.Min(engine => engine.MaximumRPM) : engines?.Length > 0 ? engines.Min(engine => engine.MaximumRPM) : math.INFINITY;
			compatibleEnginesCount = CompatibleEngineIndexes.Length;

			RefreshValidity();
		}
		private void RefreshValidity()
		{
			isValid = Type == ChargerType.Supercharger && SuperchargerType != Supercharger.Centrifugal || InertiaRPM >= MinimumEnginesRPM && InertiaRPM < RedlineEnginesRPM && ChargerSize > 0f;
			isCompatible = CompatibleEngineIndexes.Length < 1 && !IsStock || CompatibleEngineIndexes.Length > 0 && !Array.Exists(CompatibleEngines, engine => engine.IsElectric);
			hasIssues = isValid && isCompatible && (Type == ChargerType.Turbocharger || Type == ChargerType.Supercharger && SuperchargerType == Supercharger.Centrifugal) && CompatibleEngineIndexes.Length > 0 && InertiaRPM > Utility.Average(MinimumEnginesRPM, OverRevEnginesRPM);
		}

		#endregion

		#region Constructors

		public VehicleCharger(string name)
		{
			Name = name;

			RefreshEngines();
		}
		public VehicleCharger(VehicleCharger charger)
		{
			Name = charger.Name;
			isStock = charger.IsStock;
			massDifference = charger.MassDifference;
			weightDistributionDifference = charger.WeightDistributionDifference;
			type = charger.Type;
			turboCount = charger.TurboCount;
			superchargerType = charger.SuperchargerType;
			inertiaRPM = charger.InertiaRPM;
			chargerSize = charger.ChargerSize;
			minimumBoost = charger.MinimumBoost;
			maximumBoost = charger.MaximumBoost;
			compatibleEngineIndexes = charger.CompatibleEngineIndexes.Clone() as int[];
			compatibleEngines = charger.CompatibleEngines.Clone() as VehicleEngine[];
			audio = new(charger.Audio);
			isValid = charger.IsValid;
			isCompatible = charger.IsCompatible;
			hasIssues = charger.HasIssues;
			minimumEnginesMass = charger.MinimumEnginesMass;
			maximumEnginesMass = charger.MaximumEnginesMass;
			minimumEnginesRPM = charger.MinimumEnginesRPM;
			redlineEnginesRPM = charger.RedlineEnginesRPM;
			overRevEnginesRPM = charger.OverRevEnginesRPM;
			maximumEnginesRPM = charger.MaximumEnginesRPM;

			RefreshEngines();
		}

		#endregion
	}

	internal readonly struct VehicleChargerAccess : IDisposable
	{
		#region Variables

		public readonly NativeArray<char> name;
		[MarshalAs(UnmanagedType.U1)]
		public readonly bool isStock;
		public readonly float massDifference;
		public readonly float weightDistributionDifference;
		public readonly VehicleCharger.ChargerType type;
		public readonly VehicleCharger.TurbochargerCount turboCount;
		public readonly VehicleCharger.Supercharger superchargerType;
		public readonly float minimumBoost;
		public readonly float maximumBoost;
		public readonly float inertiaRPM;
		public readonly float chargerSize;
		public readonly NativeArray<VehicleEngineAccess> compatibleEngines;
		public readonly NativeArray<int> compatibleEngineIndexes;
		public readonly float minimumEnginesMass;
		public readonly float maximumEnginesMass;
		public readonly float minimumEnginesRPM;
		public readonly float redlineEnginesRPM;
		public readonly float overRevEnginesRPM;
		public readonly float maximumEnginesRPM;

		#endregion

		#region Methods

		public void Dispose()
		{
			if (name.IsCreated)
				name.Dispose();

			if (compatibleEngines.IsCreated)
				compatibleEngines.Dispose();

			if (compatibleEngineIndexes.IsCreated)
				compatibleEngineIndexes.Dispose();
		}

		#endregion

		#region Constructors

		public VehicleChargerAccess(VehicleCharger charger, Allocator allocator) : this()
		{
			if (!charger)
			{
				name = new(0, allocator);
				compatibleEngines = new(0, allocator);
				compatibleEngineIndexes = new(0, allocator);

				return;
			}

			name = new(charger.Name.ToCharArray(), allocator);
			isStock = charger.IsStock;
			massDifference = charger.MassDifference;
			weightDistributionDifference = charger.WeightDistributionDifference;
			type = charger.Type;
			turboCount = charger.TurboCount;
			superchargerType = charger.SuperchargerType;
			minimumBoost = charger.MinimumBoost;
			maximumBoost = charger.MaximumBoost;
			inertiaRPM = charger.InertiaRPM;
			chargerSize = charger.ChargerSize;
			compatibleEngines = new(charger.CompatibleEngines.Select(engine => (VehicleEngineAccess)engine).ToArray(), allocator);
			compatibleEngineIndexes = new(charger.CompatibleEngineIndexes, allocator);
			minimumEnginesMass = charger.MinimumEnginesMass;
			maximumEnginesMass = charger.MaximumEnginesMass;
			minimumEnginesRPM = charger.MinimumEnginesRPM;
			redlineEnginesRPM = charger.RedlineEnginesRPM;
			overRevEnginesRPM = charger.OverRevEnginesRPM;
			maximumEnginesRPM = charger.MaximumEnginesRPM;
		}

		#endregion

		#region Operators

		public static implicit operator VehicleChargerAccess(VehicleCharger charger) => new(charger, Allocator.Persistent);

		#endregion
	}
}

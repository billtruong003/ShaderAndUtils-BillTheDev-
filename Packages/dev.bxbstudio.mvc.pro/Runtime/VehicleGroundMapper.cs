#region Namespaces

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using Utilities;

#endregion

namespace MVC.Core
{
	[AddComponentMenu("Multiversal Vehicle Controller/Core/Ground Mapper", 10)]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(-90)]
	public class VehicleGroundMapper : ToolkitBehaviour
	{
		#region Enumerators

		public enum GroundType { Invalid, Collider, Terrain }

		#endregion

		#region Modules

		[Serializable]
		public struct GroundModule
		{
			#region Enumerators

			public enum DetailType { Detailed, Simple }

			#endregion

			#region Variables

			public string Name
			{
				readonly get
				{
					return name;
				}
				set
				{
					if (value.IsNullOrEmpty() || value.IsNullOrWhiteSpace())
						value = "New Ground";

					string prefix = default;

					if (Settings && Settings.Grounds != null && Settings.Grounds.Length > 0)
					{
						int j = Array.IndexOf(Settings.Grounds, this);

						for (int i = 0; Settings.Grounds.Where(ground => Array.IndexOf(Settings.Grounds, ground) != j && ground.Name.ToUpper() == $"{value.ToUpper()}{prefix}").Count() > 0; i++)
							prefix = i > 0 ? $" ({i})" : "";
					}

					name = $"{value}{prefix}";
				}
			}
			public float FrictionStiffness
			{
				readonly get
				{
					return frictionStiffness;
				}
				set
				{
					frictionStiffness = math.max(value, 0f);
				}
			}
			public float DamagedWheelStiffness
			{
				readonly get
				{
					return damagedWheelStiffness;
				}
				set
				{
					damagedWheelStiffness = math.max(value, 0f);
				}
			}
			[Obsolete("Use Wheel Collider's Extremum Value instead.", true)]
			public readonly float SlipValue
			{
				get
				{
					return default;
				}
			}
			public float WheelDampingRate
			{
				readonly get
				{
					return wheelDampingRate;
				}
				set
				{
					wheelDampingRate = math.clamp(value, .0001f, 1f);
				}
			}
			public float WheelBurnoutDampingRate
			{
				readonly get
				{
					return wheelBurnoutDampingRate;
				}
				set
				{
					wheelBurnoutDampingRate = math.clamp(value, .0001f, 1f);
				}
			}
			public ParticleSystem particleEffect;
			public ParticleSystem flatWheelParticleEffect;
			public Material markMaterial;
			public float MarkMaterialTiling
			{
				readonly get
				{
					if (!Settings.useWheelMarks)
						return default;

					return markMaterialTiling;
				}
				set
				{
					if (!Settings.useWheelMarks)
						return;

					markMaterialTiling = value;
				}
			}
			public Material flatWheelMarkMaterial;
			public float FlatWheelMarkMaterialTiling
			{
				readonly get
				{
					if (!Settings.useWheelMarks || !Settings.useDamage)
						return default;

					return flatWheelMarkMaterialTiling;
				}
				set
				{
					if (!Settings.useWheelMarks || !Settings.useDamage)
						return;

					flatWheelMarkMaterialTiling = value;
				}
			}
			public bool useSpeedEmission;
			public bool isOffRoad;
			public AudioClip forwardSkidClip;
			public AudioClip brakeSkidClip;
			public AudioClip sidewaysSkidClip;
			public AudioClip rollClip;
			public AudioClip flatSkidClip;
			public AudioClip flatRollClip;
			public float Volume
			{
				readonly get
				{
					return volume;
				}
				set
				{
					volume = Utility.Clamp01(value);
				}
			}

			[SerializeField]
			private string name;
			[SerializeField]
			private float frictionStiffness;
			[SerializeField]
			private float damagedWheelStiffness;
			[SerializeField]
			private float wheelDampingRate;
			[SerializeField]
			private float wheelBurnoutDampingRate;
			[SerializeField]
			private float markMaterialTiling;
			[SerializeField]
			private float flatWheelMarkMaterialTiling;
			[SerializeField]
			private float volume;

			#endregion

			#region Methods

			public override readonly bool Equals(object obj)
			{
				return obj is GroundModule module &&
					   EqualityComparer<ParticleSystem>.Default.Equals(particleEffect, module.particleEffect) &&
					   EqualityComparer<ParticleSystem>.Default.Equals(flatWheelParticleEffect, module.flatWheelParticleEffect) &&
					   EqualityComparer<Material>.Default.Equals(markMaterial, module.markMaterial) &&
					   EqualityComparer<Material>.Default.Equals(flatWheelMarkMaterial, module.flatWheelMarkMaterial) &&
					   useSpeedEmission == module.useSpeedEmission &&
					   isOffRoad == module.isOffRoad &&
					   EqualityComparer<AudioClip>.Default.Equals(forwardSkidClip, module.forwardSkidClip) &&
					   EqualityComparer<AudioClip>.Default.Equals(brakeSkidClip, module.brakeSkidClip) &&
					   EqualityComparer<AudioClip>.Default.Equals(sidewaysSkidClip, module.sidewaysSkidClip) &&
					   EqualityComparer<AudioClip>.Default.Equals(rollClip, module.rollClip) &&
					   EqualityComparer<AudioClip>.Default.Equals(flatSkidClip, module.flatSkidClip) &&
					   EqualityComparer<AudioClip>.Default.Equals(flatRollClip, module.flatRollClip) &&
					   name == module.name &&
					   frictionStiffness == module.frictionStiffness &&
					   damagedWheelStiffness == module.damagedWheelStiffness &&
					   wheelDampingRate == module.wheelDampingRate &&
					   wheelBurnoutDampingRate == module.wheelBurnoutDampingRate &&
					   volume == module.volume;
			}
			public override readonly int GetHashCode()
			{
				HashCode hash = new();

				hash.Add(particleEffect);
				hash.Add(flatWheelParticleEffect);
				hash.Add(markMaterial);
				hash.Add(flatWheelMarkMaterial);
				hash.Add(useSpeedEmission);
				hash.Add(isOffRoad);
				hash.Add(forwardSkidClip);
				hash.Add(brakeSkidClip);
				hash.Add(sidewaysSkidClip);
				hash.Add(rollClip);
				hash.Add(flatSkidClip);
				hash.Add(flatRollClip);
				hash.Add(name);
				hash.Add(frictionStiffness);
				hash.Add(damagedWheelStiffness);
				hash.Add(wheelDampingRate);
				hash.Add(wheelBurnoutDampingRate);
				hash.Add(volume);

				return hash.ToHashCode();
			}

			#endregion

			#region Constructors

			public GroundModule(string name) : this()
			{
				Name = name;
				frictionStiffness = 1f;
				damagedWheelStiffness = .5f;
				wheelDampingRate = 1f;
				wheelBurnoutDampingRate = .1f;
				markMaterialTiling = 4f;
				flatWheelMarkMaterialTiling = 1f;
				volume = 1f;
			}
			public GroundModule(GroundModule module) : this()
			{
				Name = module.Name;
				frictionStiffness = module.FrictionStiffness;
				damagedWheelStiffness = module.DamagedWheelStiffness;
				wheelDampingRate = module.WheelDampingRate;
				wheelBurnoutDampingRate = module.WheelDampingRate;
				particleEffect = module.particleEffect;
				flatWheelParticleEffect = module.flatWheelParticleEffect;
				markMaterial = module.markMaterial;
				markMaterialTiling = module.markMaterialTiling;
				flatWheelMarkMaterial = module.flatWheelMarkMaterial;
				flatWheelMarkMaterialTiling = module.flatWheelMarkMaterialTiling;
				useSpeedEmission = module.useSpeedEmission;
				isOffRoad = module.isOffRoad;
				forwardSkidClip = module.forwardSkidClip;
				brakeSkidClip = module.brakeSkidClip;
				sidewaysSkidClip = module.sidewaysSkidClip;
				rollClip = module.rollClip;
				flatSkidClip = module.flatSkidClip;
				flatRollClip = module.flatRollClip;
				volume = module.Volume;
			}

			#endregion

			#region Operators

			public static bool operator ==(GroundModule groundA, GroundModule groundB)
			{
				return groundA.Equals(groundB);
			}
			public static bool operator !=(GroundModule groundA, GroundModule groundB)
			{
				return !(groundA.name == groundB.name);
			}

			#endregion
		}
		[Serializable]
		public struct GroundMap
		{
			public int index;
			public float Wetness
			{
				readonly get
				{
					return wetness;
				}
				set
				{
					wetness = Utility.Clamp01(value);
				}
			}

			[SerializeField]
			private float wetness;
		}

		internal readonly struct GroundAccess
		{
			#region Variables

			public readonly float frictionStiffness;
			public readonly float damagedWheelStiffness;
			public readonly float wheelDampingRate;
			public readonly float wheelBurnoutDampingRate;
			public readonly float markMaterialTiling;
			public readonly float flatWheelMarkMaterialTiling;
			public readonly float volume;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool useSpeedEmission;
			[MarshalAs(UnmanagedType.U1)]
			public readonly bool isOffRoad;

			#endregion

			#region Constructors

			public GroundAccess(GroundModule module) : this()
			{
				frictionStiffness = module.FrictionStiffness;
				damagedWheelStiffness = module.DamagedWheelStiffness;
				wheelDampingRate = module.WheelDampingRate;
				wheelBurnoutDampingRate = module.WheelBurnoutDampingRate;
				markMaterialTiling = module.MarkMaterialTiling;
				flatWheelMarkMaterialTiling = module.FlatWheelMarkMaterialTiling;
				volume = module.Volume;
				useSpeedEmission = module.useSpeedEmission;
				isOffRoad = module.isOffRoad;
			}

			#endregion

			#region Operators

			public static implicit operator GroundAccess(GroundModule module) => new(module);

			#endregion
		}

		#endregion

		#region Variables

		public GroundType Type => type;
		public Terrain TerrainInstance => terrainInstance;
		public Collider ColliderInstance => colliderInstance;
		public GroundMap[] Map
		{
			get
			{
				if (m_map == null || type != GroundType.Invalid && m_map.Length < 1)
#pragma warning disable CS0618 // Type or member is obsolete
					m_map = map.Select(index => new GroundMap { index = index }).ToArray();
#pragma warning restore CS0618 // Type or member is obsolete

				return m_map;
			}
		}
		[Obsolete("Use `Map` instead.")]
		public int[] map = new int[] { 0 };

		private GroundType type;
		private Terrain terrainInstance;
		private Collider colliderInstance;
		private GroundMap[] m_map;

		#endregion

		#region Methods

		#region Awake

		public void Restart()
		{
			Awaken = false;

			if (HasInternalErrors || !IsSetupDone)
				return;

			terrainInstance = GetComponent<Terrain>();
			colliderInstance = GetComponent<Collider>();

			if (terrainInstance && colliderInstance is TerrainCollider)
				type = GroundType.Terrain;
			else if (!terrainInstance && colliderInstance)
				type = GroundType.Collider;
			else
				type = GroundType.Invalid;

			if (type != GroundType.Invalid)
			{
				var newSize = type switch
				{
					GroundType.Terrain => terrainInstance.terrainData.terrainLayers.Length,
					_ => 1,
				};

				if (Map.Length != newSize)
					Array.Resize(ref m_map, newSize);

				var groundsCount = Settings.Grounds.Length;

				for (int i = 0; i < m_map.Length; i++)
					m_map[i].index = math.clamp(m_map[i].index, 0, groundsCount - 1);

#if !MVC_COMMUNITY
				if (!colliderInstance.material)
					colliderInstance.material = new(name)
					{
						dynamicFriction = 1f,
						staticFriction = 1f,
						bounciness = 0f,
						bounceCombine =
#if UNITY_6000_0_OR_NEWER
							PhysicsMaterialCombine.Average,
#else
							PhysicMaterialCombine.Average,
#endif
						frictionCombine =
#if UNITY_6000_0_OR_NEWER
							PhysicsMaterialCombine.Average,
#else
							PhysicMaterialCombine.Average,
#endif
						hideFlags = HideFlags.NotEditable
					};
#endif
			}

			Awaken = true;
		}

		private void Awake()
		{
			if (Awaken)
				return;

			Restart();
		}

		#endregion

		#region Utilities

#if !MVC_COMMUNITY
		protected internal static float[] GetTerrainSplatMapMix(Terrain terrain, Vector3 worldPosition)
		{
			TerrainData terrainData = terrain.terrainData;
			Vector3 terrainPosition = terrain.transform.position;

			if (terrainData == null)
				return null;

			int mapX = (int)math.round((worldPosition.x - terrainPosition.x) / terrainData.size.x * terrainData.alphamapWidth);
			int mapZ = (int)math.round((worldPosition.z - terrainPosition.z) / terrainData.size.z * terrainData.alphamapHeight);
			float[,,] splatMapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);
			float[] cellMix = new float[splatMapData.GetUpperBound(2) + 1];

			for (int i = 0; i < cellMix.Length; ++i)
				cellMix[i] = splatMapData[0, 0, i];

			return cellMix;
		}
#endif

		#endregion

		#region Reset

		public void Reset()
		{
			Awake();
		}

		#endregion

		#endregion
	}
}

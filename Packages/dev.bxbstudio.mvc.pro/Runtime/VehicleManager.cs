#region Namespaces

using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using Utilities;
using Utilities.Inputs;
using MVC.Core;
using MVC.AI;
using MVC.VFX;
using MVC.UI;
using MVC.Utilities;
using MVC.Utilities.Internal;
using MVC.Internal;

#endregion

namespace MVC
{
	[AddComponentMenu("Multiversal Vehicle Controller/Core/Vehicle Manager", 0)]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(-99)]
	public class VehicleManager : ToolkitBehaviour
	{
		#region Variables

		#region Static Variables

		internal static VehicleManager Instance => instance;

		private static VehicleManager instance;

		#endregion

		#region Global Variables

		public Vehicle[] ActiveVehicles { get; set; }
		public Vehicle[] AIVehicles { get; set; }
		public VehicleGroundMapper[] GroundMappers
		{
			get
			{
				if (groundMappers == null)
					RefreshGroundMappers();

				return groundMappers;
			}
		}
		public Vehicle PlayerVehicle { get; private set; }
		public Vehicle PlayerTarget
		{
			get
			{
				return playerTarget;
			}
			set
			{
				playerTarget = value;

				if (autoRefreshVehicles)
					RefreshVehicles();

				if (autoRefreshPlayer)
				{
					PlayerVehicle = null;

					RefreshPlayer();
				}
			}
		}
		public sbyte PlayerGamepadIndex
		{
			get
			{
				if (Settings.inputSystem == ToolkitSettings.InputSystem.UnityLegacyInputManager)
					return 0;

				return playerGamepadIndex;
			}
			set
			{
				if (Settings.inputSystem == ToolkitSettings.InputSystem.UnityLegacyInputManager)
				{
					ToolkitDebug.Warning("Setting Gamepads/Joysticks is not available when using Unity's Legacy Input Manager. Please consider using another input system.");

					return;
				}

				playerGamepadIndex = (sbyte)math.max(value, -1);

				TryApplyPlayerGamepadIndexToInputsManager();
			}
		}
		public bool ApplyPlayerGamepadIndexToInputsManagerAtRuntime
		{
			get
			{
				return applyPlayerGamepadIndexToInputsManagerAtRuntime;
			}
			set
			{
				applyPlayerGamepadIndexToInputsManagerAtRuntime = value;

				TryApplyPlayerGamepadIndexToInputsManager();
			}
		}
		public bool autoRefreshPlayer = true;
		public bool autoRefreshVehicles = true;
		public bool nightMode;
		public bool fogTime;

		[SerializeField]
		private Vehicle playerTarget;
		[SerializeField]
		private sbyte playerGamepadIndex;
		[SerializeField]
		private bool applyPlayerGamepadIndexToInputsManagerAtRuntime = true;
		private VehicleGroundMapper[] groundMappers;
		private AudioListener[] listeners;
		private AudioLowPassFilter[] listenerLowPassFilters;
#if !MVC_COMMUNITY
		private bool followerIsInsideVehicle;
#endif
		private bool awaking;

		#endregion

		#endregion

		#region Methods

		#region Static Methods

		public static VehicleManager GetOrCreateInstance()
		{
			if (HasInternalErrors)
				return null;

			GameObject managerGameObject = GetOrCreateGameController();

			instance = managerGameObject.GetComponent<VehicleManager>();

			if (!instance)
				instance = managerGameObject.AddComponent<VehicleManager>();

			return instance;
		}

		#endregion

		#region Global Methods

		#region Awake

		public void Restart()
		{
			Awaken = false;

			Initialize();

			if (HasInternalErrors || instance && instance != this)
				return;

			awaking = true;
			instance = this;

			InputsManager.Start();
			TryApplyPlayerGamepadIndexToInputsManager();
			VehicleFollower.GetOrCreateInstance();

			if (autoRefreshVehicles)
				RefreshVehicles();
			else if (ActiveVehicles == null || AIVehicles == null)
			{
				ActiveVehicles = new Vehicle[] { };
				AIVehicles = new Vehicle[] { };
			}

			if (autoRefreshPlayer)
				RefreshPlayer();

			RefreshGroundMappers();

			listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
			listenerLowPassFilters = new AudioLowPassFilter[listeners.Length];

			for (int i = 0; i < listeners.Length; i++)
			{
				listenerLowPassFilters[i] = listeners[i].GetComponent<AudioLowPassFilter>();

				if (!listenerLowPassFilters[i])
					listenerLowPassFilters[i] = listeners[i].gameObject.AddComponent<AudioLowPassFilter>();
			}

			if (FindObjectsByType<VehicleUIController>(FindObjectsSortMode.None).Length > 1)
				ToolkitDebug.Warning("There seem to be 2 active UI Controllers available on the same scene, this is unnecessary as only both have the same purpose and may decrease performance!");

			awaking = false;
			Awaken = true;
		}

		private void Awake()
		{
			if (Awaken)
				return;

			Restart();
		}
		private void Initialize()
		{
			if (listenerLowPassFilters != null && listenerLowPassFilters.Length > 0)
				for (int i = 0; i < listenerLowPassFilters.Length; i++)
					Utility.Destroy(true, listenerLowPassFilters[i]);

			groundMappers = null;
			listeners = null;
			listenerLowPassFilters = null;
			awaking = false;
		}

		#endregion

		#region Utilities

		public void RefreshVehicles()
		{
			if (!Application.isPlaying && (HasInternalErrors || !IsSetupDone) || Application.isPlaying && !Awaken && !awaking)
				return;

			ActiveVehicles = FindObjectsByType<Vehicle>(FindObjectsSortMode.None);

			if (ActiveVehicles.Length > 0)
				ActiveVehicles = ActiveVehicles.Where(vehicle => !vehicle.IsTrailer).ToArray();

			AIVehicles = ActiveVehicles.Where(vehicle => vehicle.IsAI).ToArray();
		}
		public void RefreshPlayer()
		{
			if (!Application.isPlaying && (HasInternalErrors || !IsSetupDone) || Application.isPlaying && !Awaken && !awaking)
				return;

			if (PlayerTarget && PlayerTarget.gameObject.activeInHierarchy && !PlayerTarget.IsTrailer && PlayerVehicle != PlayerTarget)
				PlayerVehicle = PlayerTarget;
			
			if (PlayerVehicle && !PlayerVehicle.gameObject.activeInHierarchy || Array.IndexOf(ActiveVehicles, PlayerVehicle) < 0)
				PlayerVehicle = null;

			if (!PlayerVehicle && ActiveVehicles != null)
			{
				PlayerVehicle = ActiveVehicles.FirstOrDefault(vehicle => !vehicle.IsAI);

				if (!PlayerVehicle)
					PlayerVehicle = ActiveVehicles.FirstOrDefault();
			}
		}
		public void RefreshGroundMappers()
		{
			if (!Application.isPlaying && (HasInternalErrors || !IsSetupDone) || Application.isPlaying && !Awaken && !awaking)
				return;

			groundMappers = FindObjectsByType<VehicleGroundMapper>(FindObjectsInactive.Include, FindObjectsSortMode.None).ToArray();

			foreach (var groundMapper in groundMappers)
				if (groundMapper.Type == VehicleGroundMapper.GroundType.Invalid)
					groundMapper.Restart();

			groundMappers = groundMappers.Where(mapper => mapper.Type != VehicleGroundMapper.GroundType.Invalid).ToArray();
		}

		private void TryApplyPlayerGamepadIndexToInputsManager()
		{
			if (!Application.isPlaying || !applyPlayerGamepadIndexToInputsManagerAtRuntime || playerGamepadIndex < 0 || Settings.inputSystem != ToolkitSettings.InputSystem.InputsManager)
				return;

			InputsManager.DefaultGamepadIndex = playerGamepadIndex;
		}

		#endregion

		#region Update

		private void Update()
		{
			if (!Awaken || !PlayerVehicle)
				return;

			if (Settings.inputSystem == ToolkitSettings.InputSystem.InputsManager)
				InputsManager.Update();

#if !MVC_COMMUNITY
			if (Follower && followerIsInsideVehicle != Follower.IsInsideVehicle)
			{
				for (int i = 0; i < listenerLowPassFilters.Length; i++)
					listenerLowPassFilters[i].cutoffFrequency = Follower.IsInsideVehicle ? PlayerVehicle.AudioMixers.InteriorLowPassFreq : 22000f;

				followerIsInsideVehicle = Follower.IsInsideVehicle;
			}
#endif
		}

		#endregion

		#region Disable & Destroy

		private void OnDisable()
		{
			if (instance == this)
				instance = null;

			InputsManager.Dispose();
		}
		private void OnDestroy()
		{
			OnDisable();
		}

		#endregion

		#endregion

		#endregion
	}
}

#region Namespaces

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Utilities.Editor;
using Utilities.Inputs;
using MVC.Core;
using MVC.UI;
using MVC.Internal;
using MVC.Utilities.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Editor
{
	[CustomEditor(typeof(VehicleManager))]
	public class VehicleManagerEditor : ToolkitBehaviourEditor
	{
		#region Enumerators

		public enum ManagerFoldout
		{
			None,
			EnvironmentAndTime,
			Player
		}

		#endregion

		#region Variables

		#region Static Variables

		public static ManagerFoldout managerFoldout;

		#endregion

		#region Global Variables

		private VehicleUIController UIController
		{
			get
			{
				if (!uiController)
					uiController = Manager ? Manager.GetComponent<VehicleUIController>() : null;

				return uiController;
			}
		}

		private VehicleUIController uiController;

		#endregion

		#endregion

		#region Methods

		#region Virtual Methods

		public override void OnInspectorGUI()
		{
			EditorGUILayout.Space();

			#region Messages

			Color orgGUIBackgroundColor = GUI.backgroundColor;

			if (HasInternalErrors)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("The Multiversal Vehicle Controller is facing some internal problems that need to be fixed!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (GUILayout.Button("Try some quick fixes"))
					FixInternalProblems(Manager.gameObject);

				EditorGUILayout.Space();

				return;
			}
			else if (!IsSetupDone)
			{
				GUI.backgroundColor = Color.green;

				EditorGUILayout.HelpBox("It seems like the Multiversal Vehicle Controller is not ready for use yet!", MessageType.Info);

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (GUILayout.Button("What's going on?"))
					ToolkitSettingsEditor.OpenWindow();

				EditorGUILayout.Space();

				return;
			}

			#endregion

			switch (managerFoldout)
			{
				case ManagerFoldout.EnvironmentAndTime:
					EnvironmentTimeEditor();

					break;

				case ManagerFoldout.Player:
					PlayerEditor();

					break;

				default:
					GeneralEditor();

					break;
			}

			EditorGUILayout.Space();
		}

		#endregion

		#region Static Methods

		#region Utilities

		public static void RefreshPlayer()
		{
			if (!Manager)
			{
				ToolkitDebug.Error("We couldn't find the a Vehicle Manager.");

				return;
			}

			Manager.RefreshPlayer();
		}
		public static void RefreshVehicles()
		{
			if (!Manager)
			{
				ToolkitDebug.Error("We couldn't find the a Vehicle Manager.");

				return;
			}

			if (EditorApplication.isPlaying)
				Manager.RefreshVehicles();
			else
			{
				GameObject[] gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
				List<Vehicle> activeVehicles = new();
				List<Vehicle> AIVehicles = new();

				foreach (GameObject gameObject in gameObjects)
				{
					if (!gameObject.activeInHierarchy)
						continue;

					List<Vehicle> childrenVehicles = gameObject.GetComponentsInChildren<Vehicle>().ToList();
					Vehicle vehicle = gameObject.GetComponent<Vehicle>();

					if (vehicle)
						childrenVehicles.Insert(0, vehicle);

					foreach (Vehicle childVehicle in childrenVehicles)
					{
						if (!childVehicle || !childVehicle.enabled || childVehicle.IsTrailer)
							continue;

						if (childVehicle.IsAI)
							AIVehicles.Add(childVehicle);

						activeVehicles.Add(childVehicle);
					}
				}

				Manager.ActiveVehicles = activeVehicles.ToArray();
				Manager.AIVehicles = AIVehicles.ToArray();
			}
		}

		#endregion

		[MenuItem("GameObject/MVC/Manager", false, 20)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Manager", false, 20)]
		public static VehicleManager CreateNewManager()
		{
			if (Manager)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "This scene already has a Vehicle Manager in it and there's no need to create a second one. If you insist on creating a new manager, consider deleting the old one.", "Okay");
				ToolkitEditorUtility.SelectObject(Manager);

				return Manager;
			}

			GameObject managerGameObject = ToolkitBehaviour.GetOrCreateGameController();

			ToolkitEditorUtility.SelectObject(managerGameObject);

			return managerGameObject.AddComponent<VehicleManager>();
		}

		[MenuItem("GameObject/MVC/Manager", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Manager", true)]
		protected static bool CreateNewManagerCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		#endregion

		#region Global Methods

		#region Editor

		private void GeneralEditor()
		{
			EditorGUILayout.LabelField("Manager Configurations", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Environment & Time", EditorStyles.miniBoldLabel);

			if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				managerFoldout = ManagerFoldout.EnvironmentAndTime;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Player", EditorStyles.miniBoldLabel);

			if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				managerFoldout = ManagerFoldout.Player;

			EditorGUILayout.EndHorizontal();

			if (!UIController)
			{
				EditorGUILayout.BeginHorizontal(GUI.skin.box);
				EditorGUILayout.LabelField("User Interface (UI)", EditorStyles.miniBoldLabel);

				if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					VehicleUIController.GetOrCreateInstance();

				EditorGUILayout.EndHorizontal();
			}
		}
		private void EnvironmentTimeEditor()
		{
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				managerFoldout = ManagerFoldout.None;

			GUILayout.Space(5f);
			EditorGUILayout.LabelField("Environment & Time Configurations", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			bool newNightMode = ToolkitEditorUtility.ToggleButtons(new GUIContent("Night Mode", "Enabling the night mode will automatically turn on all of the running vehicles' low-beam headlights and taillights"), EditorStyles.miniBoldLabel, "On", "Off", Manager.nightMode, Manager, "Switch Mode");
			
			if (Manager.nightMode != newNightMode)
				Manager.nightMode = newNightMode;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			bool newFogTime = ToolkitEditorUtility.ToggleButtons(new GUIContent("Fog Time", "Enabling the fog time will automatically turn on all of the running vehicles' fog lights"), EditorStyles.miniBoldLabel, "On", "Off", Manager.fogTime, Manager, "Switch Mode");
			
			if (Manager.fogTime != newFogTime)
				Manager.fogTime = newFogTime;

			EditorGUILayout.EndVertical();
		}
		private void PlayerEditor()
		{
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				managerFoldout = ManagerFoldout.None;

			GUILayout.Space(5f);
			EditorGUILayout.LabelField("Player Configurations", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			string[] gamepadNames = new string[InputsManager.GamepadNames.Length + 1];

			for (int i = 0; i < gamepadNames.Length; i++)
				gamepadNames[i] = i < 1 ? "None" : InputsManager.GamepadNames[i - 1];

			EditorGUI.BeginDisabledGroup(Settings.inputSystem == ToolkitSettings.InputSystem.UnityLegacyInputManager);

			sbyte newPlayerGamepadIndex = (sbyte)(EditorGUILayout.Popup(new GUIContent("Player Gamepad", "The used gamepad/joystick to control the player's vehicle"), Manager.PlayerGamepadIndex + 1, gamepadNames) - 1);

			if (Manager.PlayerGamepadIndex != newPlayerGamepadIndex)
			{
				Undo.RegisterCompleteObjectUndo(Manager, "Change Gamepad");

				Manager.PlayerGamepadIndex = newPlayerGamepadIndex;

				EditorUtility.SetDirty(Manager);
			}

			if (newPlayerGamepadIndex > -1)
			{
				bool newApplyPlayerGamepadIndexToInputsManagerAtRuntime = ToolkitEditorUtility.ToggleButtons(new GUIContent("Apply to Inputs Manager", "By enabling this, the Player Gamepad Index will be applied to the Inputs Manager settings at runtime."), null, "Yes", "No", Manager.ApplyPlayerGamepadIndexToInputsManagerAtRuntime, Manager, "Switch Gamepad");

				if (Manager.ApplyPlayerGamepadIndexToInputsManagerAtRuntime != newApplyPlayerGamepadIndexToInputsManagerAtRuntime)
					Manager.ApplyPlayerGamepadIndexToInputsManagerAtRuntime = newApplyPlayerGamepadIndexToInputsManagerAtRuntime;
			}

			EditorGUI.EndDisabledGroup();

			if (Settings.inputSystem == ToolkitSettings.InputSystem.UnityLegacyInputManager)
				EditorGUILayout.HelpBox("Unity's Legacy Input Manager doesn't allow you to choose between gamepads! Please consider using another input system.", MessageType.Info);

			EditorGUILayout.Space();

			Vehicle newPlayerTarget = EditorGUILayout.ObjectField(new GUIContent("Target", "The suggested target vehicle to be used as a default player vehicle"), Manager.PlayerTarget, typeof(Vehicle), true) as Vehicle;

			if (Manager.PlayerTarget != newPlayerTarget)
			{
				Undo.RegisterCompleteObjectUndo(Manager, "Change Vehicle");

				Manager.PlayerTarget = newPlayerTarget;

				EditorUtility.SetDirty(Manager);
			}

			if (Manager.PlayerVehicle)
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(new GUIContent("Player", "The player vehicle"), Manager.PlayerVehicle, typeof(Vehicle), true);
				EditorGUI.EndDisabledGroup();
			}
			else
				EditorGUILayout.HelpBox("We couldn't find any active vehicle" +
#if !MVC_COMMUNITY
					"with no AI controller attached to it" +
#endif
					"! You may need to turn `Auto Player Selection` off & assign a vehicle manually.", MessageType.Info);

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			bool newAutoRefreshPlayer = ToolkitEditorUtility.ToggleButtons(new GUIContent("Auto Player Selection", "Enabling this will let the manager choose the player vehicle automatically; You can still refresh manually through scripting"), EditorStyles.miniBoldLabel, "On", "Off", Manager.autoRefreshPlayer, Manager, "Switch Refresh");

			if (Manager.autoRefreshPlayer != newAutoRefreshPlayer)
			{
				Manager.autoRefreshPlayer = newAutoRefreshPlayer;

				if (Manager.autoRefreshVehicles)
					RefreshVehicles();

				if (Manager.autoRefreshPlayer)
					RefreshPlayer();
			}

			if (!newAutoRefreshPlayer)
			{
				EditorGUILayout.Space();

				if (GUILayout.Button("Refresh Player"))
					RefreshPlayer();
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			bool newAutoRefreshVehicles = ToolkitEditorUtility.ToggleButtons(new GUIContent("Auto Vehicles Refresh", "Enabling this will let the manager refresh the vehicle list automatically; You can still refresh manually through scripting"), EditorStyles.miniBoldLabel, "On", "Off", Manager.autoRefreshVehicles, Manager, "Switch Refresh");

			if (Manager.autoRefreshVehicles != newAutoRefreshVehicles)
			{
				Manager.autoRefreshVehicles = newAutoRefreshVehicles;

				if (Manager.autoRefreshVehicles)
					RefreshVehicles();

				if (Manager.autoRefreshPlayer)
					RefreshPlayer();
			}

			if (!newAutoRefreshVehicles)
			{
				EditorGUILayout.Space();

				List<Vehicle> activeVehicles = Manager.ActiveVehicles != null ? Manager.ActiveVehicles.ToList() : new();

				for (int i = 0; i < activeVehicles.Count; i++)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField($"{(activeVehicles[i].IsAI ? "(AI) " : activeVehicles[i] == Manager.PlayerVehicle ? "(Player) " : "")}{activeVehicles[i].name}", EditorStyles.miniBoldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						Undo.RegisterCompleteObjectUndo(Manager, "Remove Vehicle");
						activeVehicles.RemoveAt(i);
					}

					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.BeginHorizontal(GUI.skin.box);

				Vehicle newVehicle = EditorGUILayout.ObjectField(null, typeof(Vehicle), true) as Vehicle;

				if (newVehicle)
				{
					if (!newVehicle.gameObject.activeInHierarchy)
						EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The selected vehicle needs to be active in order to be added to the vehicles list", "Okay");
					else
					{
						Undo.RegisterCompleteObjectUndo(Manager, "Add Vehicle");
						activeVehicles.Add(newVehicle);
					}
				}

				EditorGUI.BeginDisabledGroup(true);
				GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.EndHorizontal();

				Manager.ActiveVehicles = activeVehicles.ToArray();

				EditorGUILayout.BeginVertical(GUI.skin.box);

				if (GUILayout.Button("Refresh Vehicles List"))
					RefreshVehicles();

				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.EndVertical();
		}

		#endregion

		#region Enable & Disabled

		private void OnEnable()
		{
			VehicleManager.GetOrCreateInstance();

			if (!EditorApplication.isPlaying)
			{
				if (Manager.autoRefreshVehicles)
					RefreshVehicles();

				if (Manager.autoRefreshPlayer)
					RefreshPlayer();
			}
		}

		#endregion

		#endregion

		#endregion
	}
}

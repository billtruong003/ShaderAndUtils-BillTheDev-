#region Namespaces

using UnityEngine;
using UnityEditor;
using Utilities;
using Utilities.Editor;
using MVC.Editor;
using MVC.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Base.Editor
{
	[CustomEditor(typeof(VehicleZonesContainer))]
	public class VehicleZonesContainerEditor : ToolkitBehaviourEditor
	{
		#region Variables

		#region Editor Variables

		private readonly string[] zoneTypes = new string[]
		{
			"Audio/Reverb",
			"Damage/Repair",
			"Damage/Wheels",
			"Light/Darkness",
			"Light/Fog"
		};

		#endregion

		#region Global Variables

		public VehicleZonesContainer Instance
		{
			get
			{
				if (!instance)
					instance = target as VehicleZonesContainer;

				return instance;
			}
		}
		public VehicleZone[] ZoneInstances
		{
			get
			{
				return Instance.GetComponentsInChildren<VehicleZone>();
			}
		}

		private VehicleZonesContainer instance;
		private int newZoneType;

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
					FixInternalProblems(Instance.gameObject);

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

			EditorGUILayout.LabelField("Zones Container Configurations", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			for (int i = 0; i < ZoneInstances.Length; i++)
			{
				EditorGUILayout.BeginHorizontal(GUI.skin.box);
				EditorGUILayout.LabelField($"{i + 1}. {ZoneInstances[i].Type} Zone", EditorStyles.miniBoldLabel);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					ToolkitEditorUtility.SelectObject(ZoneInstances[i].gameObject);

				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					Undo.DestroyObjectImmediate(ZoneInstances[i].gameObject);
					EditorUtility.SetDirty(Instance.gameObject);
				}

				EditorGUILayout.EndHorizontal();
			}

			if (!EditorApplication.isPlaying)
			{
				EditorGUILayout.BeginHorizontal(GUI.skin.box);

				newZoneType = EditorGUILayout.Popup(new GUIContent("New Zone", "The new added zone type"), newZoneType, zoneTypes);

				EditorGUI.BeginDisabledGroup(true);
				GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.HelpBox("To add a new zone you need to press `ctrl` + `left mouse button` within the scene view.", MessageType.Info);
			}

			EditorGUI.EndDisabledGroup();

			if (EditorApplication.isPlaying)
				EditorGUILayout.HelpBox("You can't add or remove any item during play mode!", MessageType.Info);

			EditorGUILayout.Space();
		}

		#endregion

		#region Static Methods

		[MenuItem("GameObject/MVC/Zone/Container", false, 18)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Zone/Container", false, 18)]
		public static VehicleZonesContainer CreateNewContainer()
		{
			if (!CreateNewContainerCheck())
				return null;

			GameObject containerGameObject = new("New Zones Container");

			ToolkitEditorUtility.SelectObject(containerGameObject);

			return containerGameObject.AddComponent<VehicleZonesContainer>();
		}

		[MenuItem("GameObject/MVC/Zone/Container", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Zone/Container", true)]
		protected static bool CreateNewContainerCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		#endregion

		#region Global Methods

		private void OnSceneGUI()
		{
			Event e = Event.current;

			if (e.control && e.type == EventType.MouseDown && e.button == 0)
			{
				if (!EditorApplication.isPlaying && Physics.Raycast(HandleUtility.GUIPointToWorldRay(e.mousePosition), out RaycastHit hit))
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Add Zone");

					switch (newZoneType)
					{
						default:
							Instance.AddAudioZone(hit.point, VehicleAudioZone.AudioZoneType.Reverb);

							break;

						case 1:
							Instance.AddDamageZone(hit.point, VehicleDamageZone.DamageZoneType.RepairVehicle);

							break;

						case 2:
							Instance.AddDamageZone(hit.point, VehicleDamageZone.DamageZoneType.Wheel);

							break;

						case 3:
							Instance.AddWeatherZone(hit.point, VehicleWeatherZone.WeatherZoneType.Darkness);

							break;

						case 4:
							Instance.AddWeatherZone(hit.point, VehicleWeatherZone.WeatherZoneType.Fog);

							break;

					}

					EditorUtility.SetDirty(Instance.gameObject);
				}

				GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);

				Event.current.Use();
			}
		}

		#endregion

		#endregion
	}
}

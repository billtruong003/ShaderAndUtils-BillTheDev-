#region Namespaces

using UnityEngine;
using UnityEditor;
using Utilities.Editor;
using MVC.Editor;
using MVC.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Base.Editor
{
	[CustomEditor(typeof(VehicleWeatherZone))]
	public class VehicleWeatherZoneEditor : VehicleZoneEditor
	{
		#region Variables

		private VehicleWeatherZone Instance
		{
			get
			{
				if (!instance)
					instance = target as VehicleWeatherZone;

				return instance;
			}
		}
		private VehicleWeatherZone instance;

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
			else if (!Instance.ContainerInstance)
			{
				GUI.backgroundColor = Color.yellow;

				EditorGUILayout.HelpBox("This zone instance doesn't belong to any Zones Container in this scene!", MessageType.Warning);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}

			#endregion

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				ToolkitEditorUtility.SelectObject(Instance.ContainerInstance.gameObject);

			GUILayout.Space(5f);
			EditorGUILayout.LabelField("Weather Zone Configurations", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("General", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			VehicleZone.ZoneShape newShape = (VehicleZone.ZoneShape)EditorGUILayout.EnumPopup(new GUIContent("Shape", "The weather zone trigger collider shape"), Instance.Shape);

			if (Instance.Shape != newShape)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Shape");

				Instance.Shape = newShape;

				EditorUtility.SetDirty(Instance);
			}

			VehicleWeatherZone.WeatherZoneType newLightType = (VehicleWeatherZone.WeatherZoneType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The weather zone behaviour type"), Instance.zoneType);

			if (Instance.zoneType != newLightType)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

				Instance.zoneType = newLightType;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUILayout.EndVertical();
			base.OnInspectorGUI();
			EditorGUILayout.Space();
		}

		#endregion

		#region Static Methods

		[MenuItem("GameObject/MVC/Zone/Darkness", false, 19)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Zone/Darkness", false, 19)]
		public static VehicleWeatherZone CreateNewDarknessWeatherZone()
		{
			if (!CreateNewWeatherZoneCheck())
				return null;

			VehicleWeatherZone zone = CreateNewZone<VehicleWeatherZone, VehicleZonesContainer>();

			if (zone)
				zone.zoneType = VehicleWeatherZone.WeatherZoneType.Darkness;

			return zone;
		}
		[MenuItem("GameObject/MVC/Zone/Fog", false, 19)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Zone/Fog", false, 19)]
		public static VehicleWeatherZone CreateNewFogWeatherZone()
		{
			if (!CreateNewWeatherZoneCheck())
				return null;

			VehicleWeatherZone zone = CreateNewZone<VehicleWeatherZone, VehicleZonesContainer>();

			if (zone)
				zone.zoneType = VehicleWeatherZone.WeatherZoneType.Fog;

			return zone;
		}

		[MenuItem("GameObject/MVC/Zone/Darkness", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Zone/Darkness", true)]
		[MenuItem("GameObject/MVC/Zone/Fog", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Zone/Fog", true)]
		protected static bool CreateNewWeatherZoneCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone;
		}

		#endregion

		#endregion
	}
}

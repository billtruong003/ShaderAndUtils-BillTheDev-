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
	[CustomEditor(typeof(VehicleAudioZone))]
	public class VehicleAudioZoneEditor : VehicleZoneEditor
	{
		#region Variables

		private VehicleAudioZone Instance;

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
			EditorGUILayout.LabelField("Audio Zone Configurations", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("General", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			VehicleZone.ZoneShape newShape = (VehicleZone.ZoneShape)EditorGUILayout.EnumPopup(new GUIContent("Shape", "The audio zone trigger collider shape"), Instance.Shape);

			if (Instance.Shape != newShape)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Shape");

				Instance.Shape = newShape;

				EditorUtility.SetDirty(Instance);
			}

			VehicleAudioZone.AudioZoneType newAudioType = (VehicleAudioZone.AudioZoneType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The audio zone behaviour type"), Instance.zoneType);

			if (Instance.zoneType != newAudioType)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

				Instance.zoneType = newAudioType;

				EditorUtility.SetDirty(Instance);
			}

			float newMaxZoneDistanceMultiplier = ToolkitEditorUtility.NumberField(new GUIContent("Max Distance Multiplier", "Maximum zone distance multiplier"), Instance.MaxZoneDistanceMultiplier);

			if (Instance.MaxZoneDistanceMultiplier != newMaxZoneDistanceMultiplier)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Multiplier");

				Instance.MaxZoneDistanceMultiplier = newMaxZoneDistanceMultiplier;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUILayout.HelpBox($"Max Distance: {Instance.ReverbZone.maxDistance}", MessageType.None);

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			base.OnInspectorGUI();
			EditorGUILayout.Space();
			Instance.RefreshZoneSize();
		}

		#endregion

		#region Static Methods

		[MenuItem("GameObject/MVC/Zone/Audio Reverb", false, 19)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Zone/Audio Reverb", false, 19)]
		public static VehicleAudioZone CreateNewAudioReverbZone()
		{
			if (!CreateNewAudioZoneCheck())
				return null;

			VehicleAudioZone zone = CreateNewZone<VehicleAudioZone, VehicleZonesContainer>();

			if (zone)
				zone.zoneType = VehicleAudioZone.AudioZoneType.Reverb;

			return zone;
		}

		[MenuItem("GameObject/MVC/Zone/Audio Reverb", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Zone/Audio Reverb", true)]
		protected static bool CreateNewAudioZoneCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		#endregion

		#region Global Methods

		private void OnEnable()
		{
			Instance = target as VehicleAudioZone;
		}

		#endregion

		#endregion
	}
}

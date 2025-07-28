#region Namespaces

using UnityEngine;
using UnityEditor;
using Utilities;
using Utilities.Editor;
using MVC.Editor;
using MVC.Core.Editor;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Base.Editor
{
	[CustomEditor(typeof(VehicleLightSource))]
	public class VehicleLightSourceEditor : ToolkitBehaviourEditor
	{
		#region Variables

		private VehicleLightSource Instance
		{
			get
			{
				if (!instance)
					instance = (VehicleLightSource)target;

				return instance;
			}
		}
		private VehicleLightSource instance;

		#endregion

		#region Methods

		#region Editor

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
			else if (!Instance.VehicleInstance)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("This Vehicle Light Source component is invalid because its Vehicle parent cannot not be found or has been disabled!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}
			else if (!Instance.VehicleInstance.Chassis)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("This Vehicle Light Source component is invalid because its Vehicle Chassis parent cannot not be found!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}
			else if (!Instance.Instance)
			{
				GUI.backgroundColor = Utility.Color.orange;

				EditorGUILayout.HelpBox("This Vehicle Light Source component is invalid because there's no Unity Light component attached to it!", MessageType.Warning);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}
			else if (!Instance.Light)
			{
				GUI.backgroundColor = Utility.Color.orange;

				EditorGUILayout.HelpBox("This Vehicle Light Source component is invalid because it doesn't exist in the Vehicle Light sources list! Make sure you add light sources from within the Vehicle Editor or else it will not work properly!", MessageType.Warning);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}

			#endregion

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
			{
				VehicleChassisEditor.lightsFoldout = VehicleChassisEditor.EnableFoldout();

				ToolkitEditorUtility.SelectObject(Instance.VehicleInstance.Chassis.gameObject);
			}

			GUILayout.Space(5f);
			EditorGUILayout.LabelField($"Vehicle Light Source Configurations", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
		}

		#endregion

		#region Disable & Enable

		private void OnDisable()
		{
			OnEnable();

			Instance.Instance.enabled = false;
		}
		private void OnEnable()
		{
			Instance.Instance.enabled = true;

			if (Instance.VehicleInstance.Chassis)
			{
				Transform lightsParent = Instance.VehicleInstance.Chassis.transform.Find("LightSources");
				var hideFlag = Settings.useHideFlags ? HideFlags.HideInHierarchy : HideFlags.None;

				if (lightsParent && lightsParent.hideFlags != hideFlag)
					lightsParent.hideFlags = hideFlag;
			}
		}

		#endregion

		#endregion
	}
}

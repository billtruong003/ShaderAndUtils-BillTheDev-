#region Namespaces

using UnityEngine;
using UnityEditor;
using Utilities.Editor;
using MVC.Editor;
using MVC.Core;
using MVC.Core.Editor;
using MVC.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Base.Editor
{
	[CustomEditor(typeof(VehicleFollowerPivot))]
	public class VehicleFollowerPivotEditor : ToolkitBehaviourEditor
	{
		#region Variables

		private VehicleFollowerPivot Instance
		{
			get
			{
				if (!instance)
					instance = (VehicleFollowerPivot)target;

				return instance;
			}
		}
		private VehicleFollowerPivot instance;

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
			else if (!Instance.VehicleInstance)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("This Vehicle Follower Pivot component is invalid because it's Vehicle parent cannot not be found or has been disabled!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}
			else if (!Instance.VehicleInstance.Chassis)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("This Vehicle Follower Pivot component is invalid cause it's Chassis parent cannot not be found or has been disabled!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}

			#endregion

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
			{
				ToolkitEditorUtility.SelectObject(Instance.VehicleInstance.Chassis.gameObject);

				VehicleChassisEditor.followerPivotsFoldout = VehicleChassisEditor.EnableFoldout();
			}

			GUILayout.Space(5f);
			EditorGUILayout.LabelField("Follower Pivot Configurations", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			string[] camerasNames = new string[Settings.Cameras.Length + 1];

			camerasNames[0] = "None";

			for (int i = 1; i < camerasNames.Length; i++)
				camerasNames[i] = Settings.Cameras[i - 1].Name;

			int newCameraIndex = EditorGUILayout.Popup(new GUIContent("Camera", "The follower pivot type that the follower is going to use as a reference"), Instance.CameraIndex + 1, camerasNames) - 1;

			if (Instance.CameraIndex != newCameraIndex)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Camera");

				Instance.CameraIndex = newCameraIndex;

				if (newCameraIndex > -1)
					Instance.name = $"{Settings.Cameras[newCameraIndex].Name} Pivot";
				else
					Instance.name = "Follower Pivot";

				EditorUtility.SetDirty(Instance);
			}

			EditorGUILayout.Space();
			EditorGUILayout.HelpBox("Use the Move tool in the Scene View to change the pivot's position.", MessageType.Info);

			if (Instance.CameraIndex > -1 && Settings.Cameras[Instance.CameraIndex].Type != VehicleCamera.CameraType.Pivot)
				EditorGUILayout.HelpBox("This follower pivot is currently invalid, it's camera type has to be a pivot!", MessageType.Warning);
			else
				EditorGUILayout.HelpBox($"This follower pivot should be {(Instance.IsInsideVehicle ? "inside" : "outside")} the vehicle chassis.", MessageType.None);

			EditorGUILayout.Space();
		}

		#endregion

		#region Static Methods

		[MenuItem("GameObject/MVC/Follower Pivot", false, 22)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Follower Pivot", false, 22)]
		public static VehicleFollowerPivot CreateNewFollowerPivot()
		{
			if (!CreateNewFollowerPivotCheck())
				return null;

			Vehicle vehicle = ValidateCreateNewFollowerPivot();

			if (!vehicle || vehicle.IsTrailer)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", $"To create a Follower Pivot, please select an existing Vehicle or create a new one.", "Okay");

				return null;
			}
			else if (!vehicle.Chassis)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", $"The selected vehicle has to have a Chassis component attached to one of its children GameObjects.", "Okay");
				ToolkitEditorUtility.SelectObject(vehicle);

				VehicleEditor.componentsFoldout = VehicleEditor.EnableFoldout();

				return null;
			}

			GameObject pivotGameObject = new("Follower Pivot");
			Transform pivotTransform = pivotGameObject.transform;

			pivotTransform.position = Vector3.up;

			pivotTransform.SetParent(vehicle.Chassis.transform, false);

			VehicleFollowerPivot pivot = pivotGameObject.AddComponent<VehicleFollowerPivot>();

			vehicle.Chassis.RefreshFollowerPivots();
			ToolkitEditorUtility.SelectObject(pivotGameObject);
			EditorUtility.SetDirty(vehicle.Chassis.gameObject);

			return pivot;
		}

		[MenuItem("GameObject/MVC/Follower Pivot", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Follower Pivot", true)]
		protected static bool CreateNewFollowerPivotCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		private static Vehicle ValidateCreateNewFollowerPivot()
		{
			if (!Selection.activeGameObject)
				return null;

			Vehicle vehicle = Selection.activeGameObject.GetComponent<Vehicle>();

			if (!vehicle)
				vehicle = Selection.activeGameObject.GetComponentInParent<Vehicle>();

			return vehicle;
		}

		#endregion

		#endregion
	}
}

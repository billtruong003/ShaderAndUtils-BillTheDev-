#region Namespaces

using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using Utilities;
using MVC.Editor;
using MVC.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Core.Editor
{
	[CustomEditor(typeof(VehicleFollower))]
	public class VehicleFollowerEditor : ToolkitBehaviourEditor
	{
		#region Variables

		private VehicleFollower Instance
		{
			get
			{
				if (!instance)
					instance = (VehicleFollower)target;

				return instance;
			}
		}
		private VehicleFollower instance;

		#endregion

		#region Methods

		#region Menu Items

		[MenuItem("GameObject/MVC/Follower", false, 20)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Follower", false, 20)]
		public static VehicleFollower CreateFollower()
		{
			if (!CreateFollowerCheck())
				return null;

			if (ToolkitBehaviour.Follower)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "This scene already has a Vehicle Follower in it and there's no need to create a second one. If you insist on creating a new follower, consider deleting the old one.", "Okay");
				ToolkitEditorUtility.SelectObject(ToolkitBehaviour.Follower);

				return ToolkitBehaviour.Follower;
			}

			GameObject followerGameObject = new("Simple Follower");
			Transform followerTransform = followerGameObject.transform;
			Camera[] followerCameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);

			if (followerCameras.Length < 1)
				followerCameras = new Camera[] { new GameObject("Camera").AddComponent<Camera>() };

			followerTransform.position = new(0f, 1f, -10f);

			foreach (Camera camera in followerCameras)
				camera.transform.SetParent(followerTransform, false);

			if (ToolkitBehaviour.Listener)
			{
				AudioLowPassFilter filter = ToolkitBehaviour.Listener.GetComponent<AudioLowPassFilter>();

				if (filter)
					Utility.Destroy(true, filter);

				Utility.Destroy(true, ToolkitBehaviour.Listener);
			}

			ToolkitEditorUtility.SelectObject(followerGameObject);
			followerGameObject.AddComponent<AudioListener>();
			followerGameObject.AddComponent<AudioLowPassFilter>().cutoffFrequency = 22000f;

			return followerGameObject.AddComponent<VehicleFollower>();
		}

		[MenuItem("GameObject/MVC/Follower", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Follower", true)]
		protected static bool CreateFollowerCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		#endregion

		#region Editor

		public override void OnInspectorGUI()
		{
			EditorGUILayout.Space();

			#region Messages

			Camera camera = Instance.GetComponentInChildren<Camera>();
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
			else if (!camera)
			{
				GUI.backgroundColor = Color.yellow;

				EditorGUILayout.HelpBox("This follower is useless for now, because it doesn't has a child with a Camera component attached to it.", MessageType.Warning);
				EditorGUILayout.Space();

				GUI.backgroundColor = orgGUIBackgroundColor;

				return;
			}
			else if (camera.transform == Instance.transform)
			{
				GUI.backgroundColor = Color.yellow;

				EditorGUILayout.HelpBox("This follower is invalid! The camera component cannot be attached to the same object that has the Vehicle Follower component, to fix this problem you have to create a child object and add the camera component to it.", MessageType.Warning);
				EditorGUILayout.Space();

				GUI.backgroundColor = orgGUIBackgroundColor;

				return;
			}

			#endregion

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Vehicle Follower Configurations", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.Popup(new GUIContent("Camera", $"The current camera the follower is using.\r\n(This is only available in play mode and can be changed using the `{Settings.changeCameraButtonInput}` button)"), Instance.CurrentCameraIndex, (Instance.Cameras != null ? Instance.Cameras.Select(c => c.Name) : Settings.Cameras.Select(c => c.Name)).ToArray());

			if (!EditorApplication.isPlaying && Instance.CurrentCameraIndex != 0)
				Instance.CurrentCameraIndex = 0;

			EditorGUI.EndDisabledGroup();

			EditorGUI.indentLevel++;

			float newWeight = ToolkitEditorUtility.Slider(new GUIContent("Weight", "The amount of influence the Vehicle Follower has over its transform"), Instance.Weight, 0f, 1f, Instance, "Change Weight");

			if (Instance.Weight != newWeight)
				Instance.Weight = newWeight;

			bool newSmoothCameraChange = ToolkitEditorUtility.ToggleButtons(new GUIContent("Smooth Change", "Enabling this makes the camera changing smooth"), null, "On", "Off", Instance.useSmoothCameraChange, Instance, "Switch Smoothing");

			if (Instance.useSmoothCameraChange != newSmoothCameraChange)
				Instance.useSmoothCameraChange = newSmoothCameraChange;

			EditorGUI.BeginDisabledGroup(!Instance.useSmoothCameraChange);

			EditorGUI.indentLevel++;

			bool newIgnorePivotsSmoothing = ToolkitEditorUtility.ToggleButtons(new GUIContent("Ignore Pivots", "Enabling this makes the camera changing smoothness become ignored in case of switching to a pivot camera"), null, "On", "Off", Instance.ignorePivotsSmoothing, Instance, "Switch Ignore");

			if (Instance.ignorePivotsSmoothing != newIgnorePivotsSmoothing)
				Instance.ignorePivotsSmoothing = newIgnorePivotsSmoothing;

			float newCameraChangeTime = ToolkitEditorUtility.NumberField(new GUIContent("Time", "The time the follower takes while switching cameras"), Instance.CameraChangeTime * 1000f, Utility.Units.TimeAccurate, true, Instance, "Change Time") * .001f;

			if (Instance.CameraChangeTime != newCameraChangeTime)
				Instance.CameraChangeTime = newCameraChangeTime;

			EditorGUI.indentLevel--;
			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			bool newAutoSetDimensions = EditorGUILayout.Popup(new GUIContent("Dimensions", "Should vehicles dimension be calculated automatically or manually"), Instance.autoSetDimensions ? 0 : 1, new string[] { "Automatic", "Manual" }) == 0;

			if (Instance.autoSetDimensions != newAutoSetDimensions)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Dimensions");

				Instance.autoSetDimensions = newAutoSetDimensions;

				EditorUtility.SetDirty(Instance);
			}

			if (!Instance.autoSetDimensions)
			{
				float newDistance = ToolkitEditorUtility.NumberField(new GUIContent("Distance", "The distance offset from the vehicle"), Instance.Distance, Utility.Units.Distance, 2, Instance, "Change Distance");

				if (Instance.Distance != newDistance)
					Instance.Distance = newDistance;

				float newHeight = ToolkitEditorUtility.NumberField(new GUIContent("Height", "The height offset from the vehicle"), Instance.Height, Utility.Units.Distance, 2, Instance, "Change Height");

				if (Instance.Height != newHeight)
					Instance.Height = newHeight;
			}
			else
			{
				float newDistanceMultiplier = ToolkitEditorUtility.Slider(new GUIContent("Distance", "The auto calculated distance offset multiplier of the vehicle"), Instance.DistanceMultiplier, .5f, 10f, Instance, "Change Distance");

				if (Instance.DistanceMultiplier != newDistanceMultiplier)
					Instance.DistanceMultiplier = newDistanceMultiplier;

				float newHeightMultiplier = ToolkitEditorUtility.Slider(new GUIContent("Height", "The auto calculated height offset multiplier of the vehicle"), Instance.HeightMultiplier, .5f, 5f, Instance, "Change Height");

				if (Instance.HeightMultiplier != newHeightMultiplier)
					Instance.HeightMultiplier = newHeightMultiplier;
			}

			EditorGUI.indentLevel++;

			float newIdleHeight = Mathf.Round(ToolkitEditorUtility.Slider(new GUIContent("Height Idle", "The height offset from the vehicle when it's not moving"), Instance.IdleHeightMultiplier, 0f, 1f) * 100f) * .01f;

			if (Instance.IdleHeightMultiplier != newIdleHeight)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Height");

				Instance.IdleHeightMultiplier = newIdleHeight;

				EditorUtility.SetDirty(Instance);
			}

			if (!Instance.autoSetDimensions)
				EditorGUILayout.HelpBox($"Idle Height: {Mathf.Round(Instance.Height * newIdleHeight * 100f) * .01f} {Utility.Unit(Utility.Units.Distance, Settings.editorValuesUnit)}", MessageType.None);

			float newIdleMaxSpeed = ToolkitEditorUtility.NumberField(new GUIContent("Max Speed", "At which speed the follower quits its idle state"), Instance.IdleMaximumSpeed, Utility.Units.Speed, 1, Instance, "Change Speed");

			if (Instance.IdleMaximumSpeed != newIdleMaxSpeed)
				Instance.IdleMaximumSpeed = newIdleMaxSpeed;

			EditorGUI.indentLevel--;

			Vector3 newLookPointOffset = EditorGUILayout.Vector3Field(new GUIContent("Look Point Offset", "The camera's default look point offset"), Instance.LookPointOffset);

			if ((Vector3)Instance.LookPointOffset != newLookPointOffset)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Offset");

				Instance.LookPointOffset = newLookPointOffset;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			float newMouseOrbitIntensity = ToolkitEditorUtility.Slider(new GUIContent("Mouse Intensity", "The mouse rotation strength around the vehicle's orbit"), Instance.MouseOrbitIntensity, 0f, 10f, Instance, "Change Intensity");

			if (Instance.MouseOrbitIntensity != newMouseOrbitIntensity)
				Instance.MouseOrbitIntensity = newMouseOrbitIntensity;

			float newRotationDamping = ToolkitEditorUtility.Slider(new GUIContent("Rotation Damping", "The follower rotation smoothness damping"), Instance.RotationDamping, .1f, 10f, Instance, "Change Damping");

			if (Instance.RotationDamping != newRotationDamping)
				Instance.RotationDamping = newRotationDamping;

			float newHeightDamping = ToolkitEditorUtility.Slider(new GUIContent("Height Damping", "The follower height smoothness damping"), Instance.HeightDamping, .1f, 5f, Instance, "Change Damping");

			if (Instance.HeightDamping != newHeightDamping)
				Instance.HeightDamping = newHeightDamping;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField(new GUIContent("Obstacles Avoidance", "This feature allows the camera to avoid obstacles"));

			EditorGUI.indentLevel++;

			LayerMask newObstaclesMask = ToolkitEditorUtility.LayerMaskField(new GUIContent("Detection Mask", "The layer mask used to detect and avoid obstacles"), Instance.ObstaclesMask);

			if (Instance.ObstaclesMask != newObstaclesMask)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Mask");

				Instance.ObstaclesMask = newObstaclesMask;

				EditorUtility.SetDirty(Instance);
			}

			float newObstacleOffset = ToolkitEditorUtility.NumberField(new GUIContent("Offset", "The offset between the camera and the detected object"), Instance.ObstaclesOffset * 100f, Utility.Units.Size, 2, Instance, "Change Offset") * .01f;

			if (Instance.ObstaclesOffset != newObstacleOffset)
				Instance.ObstaclesOffset = newObstacleOffset;

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();

			Utility.Precision newShakePrecision = (Utility.Precision)EditorGUILayout.EnumPopup(new GUIContent("Camera Shake", "The camera shaking precision"), Instance.shakePrecision);

			if (Instance.shakePrecision != newShakePrecision)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Precision");

				Instance.shakePrecision = newShakePrecision;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUILayout.EndHorizontal();

			EditorGUI.indentLevel++;

			if (Instance.shakePrecision == Utility.Precision.Advanced)
			{
				float2 newShakeIntensity = math.max(EditorGUILayout.Vector2Field(new GUIContent("Intensity", "The camera shaking intensity at high speeds, crashing or when using NOS. X represents the horizontal axis of the screen, Y represents the vertical axis"), Instance.ShakeIntensity), 0f);

				if (!Instance.ShakeIntensity.Equals(newShakeIntensity))
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Intensity");

					Instance.ShakeIntensity = newShakeIntensity;

					EditorUtility.SetDirty(Instance);
				}

				float2 newShakeSpeed = math.max(EditorGUILayout.Vector2Field(new GUIContent("Speed", "The camera shaking speed when crashing. X represents the horizontal axis of the screen, Y represents the vertical axis"), Instance.ShakeSpeed), 0f);

				if (!Instance.ShakeSpeed.Equals(newShakeSpeed))
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Speed");

					Instance.ShakeSpeed = newShakeSpeed;

					EditorUtility.SetDirty(Instance);
				}
			}
			else
			{
				float newShakeIntensityMultiplier = ToolkitEditorUtility.Slider(new GUIContent("Intensity", "The camera shaking intensity at high speeds, crashing or when using NOS. This represents the movement on both the horizontal and vertical axis at the same time"), Instance.ShakeIntensityMultiplier, 0f, 10f, Instance, "Change Intensity");

				if (Instance.ShakeIntensityMultiplier != newShakeIntensityMultiplier)
					Instance.ShakeIntensityMultiplier = newShakeIntensityMultiplier;

				float newShakeSpeedMultiplier = ToolkitEditorUtility.Slider(new GUIContent("Speed", "The camera shaking speed when crashing. This represents the movement on both the horizontal and vertical axis at the same time"), Instance.ShakeSpeedMultiplier, 0f, 10f, Instance, "Change Speed");

				if (Instance.ShakeSpeedMultiplier != newShakeSpeedMultiplier)
					Instance.ShakeSpeedMultiplier = newShakeSpeedMultiplier;
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField(new GUIContent("Showcase", "This feature is enabled after a specified timeout in case the player won't touch a key"));

			EditorGUI.indentLevel++;

			float newShowcaseDistanceMultiplier = ToolkitEditorUtility.Slider(new GUIContent("Distance", "The showcase camera distance multiplier"), Instance.ShowcaseDistanceMultiplier, .5f, 10f, Instance, "Change Multiplier");

			if (Instance.ShowcaseDistanceMultiplier != newShowcaseDistanceMultiplier)
				Instance.ShowcaseDistanceMultiplier = newShowcaseDistanceMultiplier;

			float newShowcaseHeightMultiplier = ToolkitEditorUtility.Slider(new GUIContent("Height", "The showcase camera height multiplier"), Instance.ShowcaseHeightMultiplier, .1f, 5f, Instance, "Change Multiplier");

			if (Instance.ShowcaseHeightMultiplier != newShowcaseHeightMultiplier)
				Instance.ShowcaseHeightMultiplier = newShowcaseHeightMultiplier;

			EditorGUI.indentLevel++;

			float newShowcaseHeightRandomization = ToolkitEditorUtility.Slider(new GUIContent("Randomization", "The showcase camera height randomization intensity"), Instance.ShowcaseHeightRandomization, 0f, 10f, Instance, "Change Intensity");

			if (Instance.ShowcaseHeightRandomization != newShowcaseHeightRandomization)
				Instance.ShowcaseHeightRandomization = newShowcaseHeightRandomization;

			EditorGUI.indentLevel--;

			float newShowcaseTimeout = ToolkitEditorUtility.NumberField(new GUIContent("Timeout", "The time required to start the showcase"), Instance.ShowcaseTimeout, Utility.Units.Time, 1, Instance, "Change Timeout");

			if (Instance.ShowcaseTimeout != newShowcaseTimeout)
				Instance.ShowcaseTimeout = newShowcaseTimeout;

			float newShowcaseSpeed = ToolkitEditorUtility.Slider(new GUIContent("Speed", "The camera orbit speed"), Instance.ShowcaseSpeed, .01f, 5f, Instance, "Change Speed");

			if (Instance.ShowcaseSpeed != newShowcaseSpeed)
				Instance.ShowcaseSpeed = newShowcaseSpeed;

			float newShowcaseFieldOfView = ToolkitEditorUtility.Slider(new GUIContent("Field Of View", "The showcase camera field of view"), Instance.ShowcaseFieldOfView, 1f, 179f, "°", "Degrees", Instance, "Change Field Of View");

			if (Instance.ShowcaseFieldOfView != newShowcaseFieldOfView)
				Instance.ShowcaseFieldOfView = newShowcaseFieldOfView;

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
		}

		#endregion

		#region Destroy

		private void OnDestroy()
		{
			if (Instance && !EditorApplication.isPlaying)
				Instance.DisposeNativeArrays();
		}

		#endregion

		#endregion
	}
}

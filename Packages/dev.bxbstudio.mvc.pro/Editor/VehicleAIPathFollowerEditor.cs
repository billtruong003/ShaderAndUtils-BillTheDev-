#region Namespaces

using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using Utilities;
using MVC.Editor;
using MVC.Utilities.Editor;
using System.IO;

#endregion

namespace MVC.AI.Editor
{
	[CustomEditor(typeof(VehicleAIPathFollower))]
	public class VehicleAIPathFollowerEditor : ToolkitBehaviourEditor
	{
		#region Varibles

		private VehicleAIPathFollower instance;

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
					FixInternalProblems(instance.gameObject);

				EditorGUILayout.Space();

				return;
			}

			#endregion

			EditorGUILayout.LabelField("AI Path Follower Behaviour Configurations", EditorStyles.boldLabel);
			EditorGUILayout.Space();
#if MVC_COMMUNITY

			GUI.backgroundColor = Color.green;
			
			EditorGUILayout.HelpBox("This feature is only included with Multiversal Vehicle Controller Pro!", MessageType.Info);

			GUI.backgroundColor = Color.yellow;

			EditorGUILayout.HelpBox("This component exist only for the purpose of not losing references when switching between versions.", MessageType.Warning);

			GUI.backgroundColor = orgGUIBackgroundColor;

			if (GUILayout.Button("Upgrade to Pro"))
				ToolkitEditorWindow.VisitWebsite();

			EditorGUILayout.Space();
#else
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Behaviour Parameters", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			var newUpdateType = ToolkitEditorUtility.EnumField(new GUIContent("Update Type", "Indicates how often the behaviour is updated"), instance.updateType, instance, "Change Type");

			if (instance.updateType != newUpdateType)
				instance.updateType = newUpdateType;

			var newUpdateMethod = ToolkitEditorUtility.EnumField(new GUIContent("Update Method", "Indicates whether to update the behaviour even when it's inactive or not"), instance.updateMethod, instance, "Change Method");

			if (instance.updateMethod != newUpdateMethod)
				instance.updateMethod = newUpdateMethod;

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Path", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			var newPath = EditorGUILayout.ObjectField(new GUIContent("Path", "The path this vehicle is going to follow"), instance.path, typeof(VehicleAIPath), true) as VehicleAIPath;

			if (instance.path != newPath)
			{
				Undo.RegisterCompleteObjectUndo(instance, "Change Path");

				instance.path = newPath;

				EditorUtility.SetDirty(instance);
			}

			float newPathWidthMultiplier = ToolkitEditorUtility.Slider(new GUIContent("Width Multiplier", "Path width multiplier"), instance.pathWidthMultiplier, 0f, 1f, instance, "Change Multiplier");

			if (instance.pathWidthMultiplier != newPathWidthMultiplier)
				instance.pathWidthMultiplier = newPathWidthMultiplier;

			if (Mathf.Approximately(instance.pathWidthMultiplier, 0f))
				EditorGUILayout.HelpBox("Setting the `Width Multiplier` to zero (0) forces the vehicle follow the path strictly. The vehicle won't be able to follow the shortest path possible.", MessageType.Info);

			var newFollowDirection = ToolkitEditorUtility.EnumField(new GUIContent("Follow Direction", "Path follow direction"), instance.followDirection, instance, "Change Direction");

			if (instance.followDirection != newFollowDirection)
				instance.followDirection = newFollowDirection;

			VehicleAIPathFollower.StartPoint newStartPoint = ToolkitEditorUtility.EnumField(new GUIContent("Start Point", "Indicates whether to use the path's default start point or use the closest point to the vehicle on start"), instance.startPoint, instance, "Change Start");

			if (instance.startPoint != newStartPoint)
				instance.startPoint = newStartPoint;

			VehicleAIPathFollower.InputInterpolation newInputInterpolation = ToolkitEditorUtility.EnumField(new GUIContent("Input Interpolation", "Indicates whether to interpolate Fuel and Brake inputs or not"), instance.inputInterpolation, instance, "Change Interpolation");

			if (instance.inputInterpolation != newInputInterpolation)
				instance.inputInterpolation = newInputInterpolation;

			float newTargetSpeedMultiplier = ToolkitEditorUtility.Slider(new GUIContent("Target Speed Multiplier", "Multiplies each path's spaced point target speed. Higher values allows the vehicle to reach higher speeds on certain turns"), instance.targetSpeedMultiplier, .1f, 3f, instance, "Change Multiplier");

			if (instance.targetSpeedMultiplier != newTargetSpeedMultiplier)
				instance.targetSpeedMultiplier = newTargetSpeedMultiplier;

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Obstacle Sensors", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			VehicleAIPathFollower.ObstacleDetectionMethod newObstacleDetectionMethod = ToolkitEditorUtility.EnumField(new GUIContent("Obstacle Detection Method", "Method used to detect and avoid obstacles"), instance.obstacleDetectionMethod, instance, "Change Method");

			if (instance.obstacleDetectionMethod != newObstacleDetectionMethod)
				instance.obstacleDetectionMethod = newObstacleDetectionMethod;

			int newSensorsMaximumHits = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Maximum Hit Count", "Maximum hit count per raycast"), instance.sensorsMaximumHits, instance, "Change Count"), 1);

			if (instance.sensorsMaximumHits != newSensorsMaximumHits)
				instance.sensorsMaximumHits = newSensorsMaximumHits;

			var newObstaclesLayerMask = ToolkitEditorUtility.LayerMaskField(new GUIContent("Layer Mask", "Obstacles layer mask"), instance.obstaclesLayerMask);

			if (instance.obstaclesLayerMask != newObstaclesLayerMask)
			{
				Undo.RegisterCompleteObjectUndo(instance, "Change Mask");

				instance.obstaclesLayerMask = newObstaclesLayerMask;

				EditorUtility.SetDirty(instance);
			}

			float newSensorsIntensityPower = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Intensity Smoothness", "Power value of the sensors intensity. Higher values will smooth out the interpolation between normal steering input and the sensors' steering input"), instance.sensorsIntensityPower, false, instance, "Change Intensity"), .1f);

			if (instance.sensorsIntensityPower != newSensorsIntensityPower)
				instance.sensorsIntensityPower = newSensorsIntensityPower;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Diagonal Sensors", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			float newDiagonalSensorsSteerIntensity = ToolkitEditorUtility.Slider(new GUIContent("Steer Intensity", "Steer multiplier when a static obstacle is detected through the diagonal sensors"), instance.diagonalSensorsSteerIntensity, 0f, 1f, instance, "Change Intensity");

			if (instance.diagonalSensorsSteerIntensity != newDiagonalSensorsSteerIntensity)
				instance.diagonalSensorsSteerIntensity = newDiagonalSensorsSteerIntensity;

			float newDiagonalSensorsDynamicSteerIntensity = ToolkitEditorUtility.Slider(new GUIContent("Dynamic Steer Intensity", "Steer multiplier for dynamic obstacles (with attached Rigidbodies)"), instance.diagonalSensorsDynamicSteerIntensity, 0f, 1f, instance, "Change Intensity");

			if (instance.diagonalSensorsDynamicSteerIntensity != newDiagonalSensorsDynamicSteerIntensity)
				instance.diagonalSensorsDynamicSteerIntensity = newDiagonalSensorsDynamicSteerIntensity;

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Side Sensors", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			float newSideSensorsSteerIntensity = ToolkitEditorUtility.Slider(new GUIContent("Steer Intensity", "Side sensors steer multiplier for both dynamic and static obstacles"), instance.sideSensorsSteerIntensity, 0f, 1f, instance, "Change Intensity");

			if (instance.sideSensorsSteerIntensity != newSideSensorsSteerIntensity)
				instance.sideSensorsSteerIntensity = newSideSensorsSteerIntensity;

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField($"Statistics{(!EditorApplication.isPlaying ? " (PlayMode)" : "")}", EditorStyles.miniBoldLabel);

			if (EditorApplication.isPlaying)
			{
				EditorGUI.indentLevel++;

				EditorGUILayout.LabelField(new GUIContent("Point Index", "Path spaced point index"), new GUIContent($"{instance.PathCurrentPointIndex}{(instance.path ? $" / {instance.path.SpacedPointsCount}" : "")}"), EditorStyles.boldLabel);
				EditorGUILayout.LabelField(new GUIContent("Start Point Index", "Path spaced start point index"), new GUIContent($"{instance.PathStartPointIndex}"), EditorStyles.boldLabel);

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
#endif
		}

		#endregion

		#region Global Methods

		private void OnEnable()
		{
			instance = target as VehicleAIPathFollower;
		}

		#endregion

		#endregion
	}
}

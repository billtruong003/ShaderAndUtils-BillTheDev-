#region Namespaces

using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using Utilities;
using Utilities.Editor;
using MVC.Editor;
using MVC.Internal;
using MVC.Utilities;
using MVC.Utilities.Editor;

#endregion

namespace MVC.AI.Editor
{
	[CustomEditor(typeof(VehicleAIPath))]
	public class VehicleAIPathEditor : ToolkitBehaviourEditor
	{
		#region Variables

		#region Static Variables

#if !MVC_COMMUNITY
		private static VehicleAIZone.AIZoneType newZoneType;
#endif

		#endregion

		#region Global Variables

		private VehicleAIPath instance;
#if !MVC_COMMUNITY
		private VehicleAIPath.SpacedPathPoint selectedPathPoint = new(default, -1);
		private int selectedCurveAnchorIndex = -1;
		private bool moveAnchorsManually;
		private bool hideControlsGUI;
#endif

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
					FixInternalProblems(instance.gameObject);

				EditorGUILayout.Space();

				return;
			}

			#endregion

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.LabelField("AI Path Configurations", EditorStyles.boldLabel);
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
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Path", EditorStyles.miniBoldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (GUILayout.Button("New", EditorStyles.miniButtonLeft))
			{
				Undo.RegisterCompleteObjectUndo(instance, "New Path");
				instance.NewPath();
				EditorUtility.SetDirty(instance);
			}

			if (GUILayout.Button("Reset", EditorStyles.miniButtonRight))
			{
				Undo.RegisterCompleteObjectUndo(instance, "Reset Path");
				instance.ResetPath();
				EditorUtility.SetDirty(instance);
			}

			EditorGUILayout.EndHorizontal();

			EditorGUI.indentLevel++;

			var curveTypes = Enum.GetNames(typeof(VehicleAIPath.CurveType));
			var newCurveType = (VehicleAIPath.CurveType)ToolkitEditorUtility.ToggleMultipleButtons(new GUIContent("Type", "Curve Type"), null, true, (int)instance.curveType, instance, "Change Type", true, curveTypes);

			if (instance.curveType != newCurveType)
				instance.curveType = newCurveType;

			EditorGUI.indentLevel++;

			bool newUseCardinal = ToolkitEditorUtility.ToggleButtons(new GUIContent("Auto Controls (Cardinal)", "Enable/disable using cardinal calculations to generate smooth curves"), null, "On", "Off", instance.UseCardinal, instance, "Change Cardinal");

			if (instance.UseCardinal != newUseCardinal)
			{
				instance.UseCardinal = newUseCardinal;

				UndoRedoPerformed();
			}

			EditorGUI.indentLevel--;

			if (instance.CurvesCount > 0)
			{
				bool newLoopedPath = ToolkitEditorUtility.ToggleButtons("Loop Path", null, "On", "Off", instance.LoopedPath, instance, "Switch Loop");

				if (instance.LoopedPath != newLoopedPath)
				{
					instance.LoopedPath = newLoopedPath;

					UndoRedoPerformed();
				}

				if (instance.LoopedPath)
				{
					EditorGUI.indentLevel++;

					float newStartOffset = ToolkitEditorUtility.Slider(new GUIContent("Start Offset", "Where should the looped path start from?"), instance.startOffset, 0f, 1f, instance, "Change Start Offset");

					if (instance.startOffset != newStartOffset)
						instance.startOffset = newStartOffset;

					EditorGUI.indentLevel--;
				}
			}
			else
				EditorGUILayout.HelpBox("This path instance is invalid, it has to have at least one segment for it to be followable by other vehicles on this scene!", MessageType.Warning);

			EditorGUILayout.BeginHorizontal();

			float newDrawWidth = ToolkitEditorUtility.NumberField(new GUIContent("Draw Width", "Path width"), instance.DrawWidth, Utility.Units.Distance, false, instance, "Change Width");

			if (instance.DrawWidth != newDrawWidth)
				instance.DrawWidth = newDrawWidth;

			if (GUILayout.Button("Set All", EditorStyles.miniButton))
			{
				Undo.RegisterCompleteObjectUndo(instance, "Set Width");
				instance.SetCurvesWidth(instance.DrawWidth);
				EditorUtility.SetDirty(instance);
			}

			EditorGUILayout.EndHorizontal();

			if (!EditorApplication.isPlaying && !hideControlsGUI)
				EditorGUILayout.HelpBox("To add new points to the AI path, press `shift` + `left mouse button` within the scene view. Use the combination `shift` + `right mouse button` to remove a point.", MessageType.Info);

			EditorGUI.BeginDisabledGroup(instance.CurvesCount < 1);

			if (instance.CurvesCount > 0)
			{
				EditorGUI.BeginChangeCheck();

				hideControlsGUI = GUILayout.Toggle(hideControlsGUI, "Hide Controls", ToolkitEditorUtility.IndentedButton);

				EditorGUI.BeginDisabledGroup(hideControlsGUI);

				moveAnchorsManually = GUILayout.Toggle(moveAnchorsManually, "Move Anchors Using Handles", ToolkitEditorUtility.IndentedButton);

				EditorGUI.EndDisabledGroup();

				if (EditorGUI.EndChangeCheck())
					SceneView.RepaintAll();

				if (!hideControlsGUI && moveAnchorsManually)
					EditorGUILayout.HelpBox("You can select an anchor point just by moving your mouse cursor near the desired point.", MessageType.Info);
			}

			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Ground Detection for Spaced Points", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			LayerMask newGroundLayerMask = ToolkitEditorUtility.LayerMaskField(new GUIContent("Ground Layer Mask", "Layer mask used for ground detection near spaced points"), instance.groundLayerMask);

			if (instance.groundLayerMask != newGroundLayerMask)
			{
				Undo.RegisterCompleteObjectUndo(instance, "Change Mask");

				instance.groundLayerMask = newGroundLayerMask;

				UndoRedoPerformed();
				EditorUtility.SetDirty(instance);
			}

			float newGroundDetectionRayHeight = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Ray Height", "The raycast length for each spaced point"), instance.groundDetectionRayHeight, Utility.Units.Distance, false, instance, "Change Length"), math.EPSILON);

			if (instance.groundDetectionRayHeight != newGroundDetectionRayHeight)
			{
				instance.groundDetectionRayHeight = newGroundDetectionRayHeight;

				UndoRedoPerformed();
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Path Visualizer", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			float newVisualizerSpeedMultiplier = ToolkitEditorUtility.Slider(new GUIContent("Speed Multiplier", "Path curves' maximum speed multiplier"), instance.visualizerSpeedMultiplier, 0f, 2f, instance, "Change Multiplier");

			if (instance.visualizerSpeedMultiplier != newVisualizerSpeedMultiplier)
				instance.visualizerSpeedMultiplier = newVisualizerSpeedMultiplier;

			bool newShowVisualizerAtStart = ToolkitEditorUtility.ToggleButtons(new GUIContent("Show at Start", "Enabling this will show the path visualizer immediately on start"), null, "Yes", "No", instance.showVisualizerAtStart, instance, "Change Show");

			if (instance.showVisualizerAtStart != newShowVisualizerAtStart)
				instance.showVisualizerAtStart = newShowVisualizerAtStart;

			float newVisualizerShowHidePointDuration = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Fade Point Duration", "Show/hide (color fade) duration per path spaced point"), instance.visualizerShowHidePointDuration * 1000f, Utility.Units.TimeAccurate, true, instance, "Change Duration"), 0f) * .001f;

			if (instance.visualizerShowHidePointDuration != newVisualizerShowHidePointDuration)
				instance.visualizerShowHidePointDuration = newVisualizerShowHidePointDuration;

			EditorGUI.BeginDisabledGroup(instance.CurvesCount < 1 || !EditorApplication.isPlaying && instance.Visualizer || EditorApplication.isPlaying && instance.IsShowingVisualizer);

			if (GUILayout.Button("Show Visualizer", ToolkitEditorUtility.IndentedButton))
			{
				if (EditorApplication.isPlaying)
					instance.ShowPathVisualizer();
				else
				{
					if (instance.SpacedPointsCount < 1)
					{
						instance.GenerateSpacedPoints();
						instance.AlignSpacedPointsToGround();
					}

					instance.CreatePathVisualizer();
					instance.DrawPathVisualizer();
				}
			}

			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup(!EditorApplication.isPlaying && !instance.Visualizer || EditorApplication.isPlaying && !instance.IsShowingVisualizer);

			if (GUILayout.Button("Hide Visualizer", ToolkitEditorUtility.IndentedButton))
			{
				if (EditorApplication.isPlaying)
					instance.HidePathVisualizer();
				else
					DestroyImmediate(instance.Visualizer);
			}

			EditorGUI.EndDisabledGroup();

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("AI Zones", EditorStyles.miniBoldLabel);
			GUILayout.FlexibleSpace();

			bool newSnapZones = GUILayout.Toggle(instance.snapZones, new GUIContent("Snap to Path", "Snap zones to path"), EditorStyles.miniButton);

			if (instance.snapZones != newSnapZones)
			{
				Undo.RegisterCompleteObjectUndo(instance, "Change Snap");

				instance.snapZones = newSnapZones;

				EditorUtility.SetDirty(instance);
			}

			EditorGUILayout.EndHorizontal();

			EditorGUI.indentLevel++;

			var AIZones = instance.GetComponentsInChildren<VehicleAIZone>();

			for (int i = 0; i < AIZones.Length; i++)
			{
				EditorGUILayout.BeginHorizontal(GUI.skin.box);
				EditorGUILayout.LabelField($"{i + 1}. {AIZones[i].zoneType} Zone", EditorStyles.miniBoldLabel);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					ToolkitEditorUtility.SelectObject(AIZones[i].gameObject);

				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					Undo.DestroyObjectImmediate(AIZones[i].gameObject);
					EditorUtility.SetDirty(instance.gameObject);
				}

				EditorGUILayout.EndHorizontal();
			}

			if (!EditorApplication.isPlaying && !hideControlsGUI)
			{
				EditorGUILayout.BeginHorizontal(GUI.skin.box);

				newZoneType = (VehicleAIZone.AIZoneType)EditorGUILayout.EnumPopup(new GUIContent("New Zone", "The new added zone type"), newZoneType);

				EditorGUI.BeginDisabledGroup(true);
				GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide);
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.HelpBox("To add a new AI zone you need to press `ctrl` + `left mouse button` within the scene view.", MessageType.Info);
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();

			if (EditorApplication.isPlaying)
				EditorGUILayout.HelpBox("You can't add or remove any AI item during play mode!", MessageType.Info);

			if (EditorGUI.EndChangeCheck())
				SceneView.RepaintAll();
#endif
		}

		#endregion

		#region Static Methods

#if !MVC_COMMUNITY
		[MenuItem("GameObject/MVC/AI/Path", false, 19)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/AI/Path", false, 19)]
#endif
		public static VehicleAIPath CreateNewPath()
		{
			if (!CreateNewPathCheck())
				return null;

			GameObject pathGameObject = new("New AI Path");

			ToolkitEditorUtility.SelectObject(pathGameObject);

			return pathGameObject.AddComponent<VehicleAIPath>();
		}

#if !MVC_COMMUNITY
		[MenuItem("GameObject/MVC/AI/Path", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/AI/Path", true)]
#endif
		protected static bool CreateNewPathCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		#endregion

		#region Global Methods

		#region Utilities

#if !MVC_COMMUNITY
		private void UserInput()
		{
			if (hideControlsGUI)
				return;

			Event e = Event.current;

			if (!moveAnchorsManually)
			{
				if (e.shift && e.type == EventType.MouseDown)
				{
					if (e.button == 0)
					{
						if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(e.mousePosition), out RaycastHit hit))
						{
							Undo.RegisterCompleteObjectUndo(instance, "Add Point");

							if (selectedPathPoint.curveIndex < 0)
								instance.AddNextCurve(hit.point);
							else
								instance.SplitCurve(selectedPathPoint.curveIndex, hit.point);

							UndoRedoPerformed();

							EditorUtility.SetDirty(instance);
						}

						GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);

						Event.current.Use();
					}

					if (e.button == 1)
					{
						var curves = instance.BezierCurves;

						if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(e.mousePosition), out RaycastHit hit))
						{
							float3 hitPoint = hit.point;
							int closestCurveIndex = instance.ClosestCurveIndex(hitPoint, Settings.gizmosSize * 10f);

							if (closestCurveIndex > -1)
							{
								int curvesCount = curves.Length;
								var closestCurve = curves[closestCurveIndex];
								float p0Distance = Utility.DistanceSqr(hitPoint, closestCurve.P0);
								float p3Distance = Utility.DistanceSqr(hitPoint, closestCurve.P3);
								bool loopedPath = instance.LoopedPath && curvesCount > 2;
								bool mergeCurves, removeFirstCurve;
								int curve1Index, curve2Index;

								if (p0Distance < p3Distance) // P0 is closest
								{
									curve1Index = closestCurveIndex - 1;
									curve2Index = closestCurveIndex;

									while (loopedPath && curve1Index < 0)
										curve1Index += curvesCount;

									mergeCurves = loopedPath || curve1Index > 0;
									removeFirstCurve = curve1Index < 0;
								}
								else // P3 is closest
								{
									curve1Index = closestCurveIndex;
									curve2Index = closestCurveIndex + 1;

									while (loopedPath && curve2Index >= curvesCount)
										curve2Index -= curvesCount;

									mergeCurves = loopedPath || curve2Index < curvesCount;
									removeFirstCurve = curve2Index < curvesCount;
								}

								Undo.RegisterCompleteObjectUndo(instance, "Remove Anchor");

								if (curvesCount > 2 && mergeCurves)
									instance.MergeCurves(curve1Index, curve2Index);
								else
								{
									if (instance.LoopedPath)
										instance.LoopedPath = false;

									if (removeFirstCurve)
										instance.RemoveFirstCurve();
									else
										instance.RemoveLastCurve();
								}

								UndoRedoPerformed();
								EditorUtility.SetDirty(instance);
							}
						}
					}
				}

				if (e.control && e.type == EventType.MouseDown)
				{
					if (e.button == 0)
					{
						if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(e.mousePosition), out RaycastHit hit))
						{
							Undo.RegisterCompleteObjectUndo(instance, "Add Zone");
							instance.AddAIZone(hit.point, newZoneType);
							EditorUtility.SetDirty(instance.gameObject);
						}

						GUIUtility.hotControl = GUIUtility.GetControlID(FocusType.Passive);

						Event.current.Use();
					}
				}
			}

			if (e.type == EventType.MouseMove)
				if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(e.mousePosition), out RaycastHit hit))
				{
					var bezierCurves = instance.BezierCurves;
					float distanceFromCurve = math.max(Settings.gizmosSize * 10f, instance.DrawWidth * .5f);
					VehicleAIPath.SpacedPathPoint closestPathPoint = new(default, -1);

					for (int i = 0; i < bezierCurves.Length; i++)
					{
						var curve = bezierCurves[i];
						float distance = HandleUtility.DistancePointBezier(hit.point, curve.P0, curve.P3, curve.P1, curve.P2);
						float inversePosition = instance.InverseCurvePosition(i, hit.point);

						if (moveAnchorsManually && closestPathPoint.curveIndex < 0 || distance <= distanceFromCurve && inversePosition >= 0f && inversePosition <= 1f)
						{
							closestPathPoint = new(new(curve.GetPoint(inversePosition), inversePosition), i);
							distanceFromCurve = distance;
						}
					}

					int closestCurveAnchorIndex;

					if (closestPathPoint.curveIndex > -1)
					{
						int p0Index = closestPathPoint.curveIndex * 3;
						int p3Index = closestPathPoint.curveIndex * 3 + 3;

						closestCurveAnchorIndex = closestPathPoint.spacedCurvePoint.t > .5f ? p3Index : p0Index;
					}
					else
						closestCurveAnchorIndex = -1;

					if (selectedCurveAnchorIndex != closestCurveAnchorIndex || selectedPathPoint != closestPathPoint)
					{
						selectedCurveAnchorIndex = closestCurveAnchorIndex;
						selectedPathPoint = closestPathPoint;

						HandleUtility.Repaint();
					}
				}
		}
		private void CurvesGUI()
		{
			if (hideControlsGUI)
				return;

			bool useHermiteCurves = instance.curveType == VehicleAIPath.CurveType.Hermite;
			var hermiteCurves = instance.HermiteCurves;
			var bezierCurves = instance.BezierCurves;
			int curvesCount = instance.CurvesCount;
			Event e = Event.current;

			Handles.color = Utility.Color.darkGray;

			bool CurvePoint(ref float3 position, ref float width, quaternion rotation, int pointIndex)
			{
				Color controlColor = Settings.AIPathControlsGizmoColor;
				Color anchorColor = Settings.AIPathAnchorGizmoColor;
				bool controlPoint = pointIndex % 3 != 0;

				Handles.color = controlPoint ? controlColor : anchorColor;

				float3 newPosition = position;
				bool manualMovement = false;
				float newWidth = width;

				if (controlPoint)
					newPosition = Handles.FreeMoveHandle(position,
#if !UNITY_2022_2_OR_NEWER
						rotation,
#endif
						Settings.gizmosSize, Vector3.zero, Handles.CylinderHandleCap);
				else
				{
					if (manualMovement = moveAnchorsManually && selectedCurveAnchorIndex == pointIndex)
						newPosition = Handles.PositionHandle(position, rotation);
					else
					{
						Handles.color = controlColor;
						newWidth = Handles.ScaleSlider(newWidth, position, math.mul(rotation, Utility.Float3Right) * math.max(newWidth * .5f, 1f), rotation, Settings.gizmosSize * 2f, 0f);
						Handles.color = anchorColor;
						newPosition = Handles.FreeMoveHandle(position,
#if !UNITY_2022_2_OR_NEWER
							rotation,
#endif
							Settings.gizmosSize, Vector3.zero, Handles.CylinderHandleCap);
					}
				}

				if (!position.Equals(newPosition))
				{
					if (manualMovement)
						position = newPosition;
					else
					{
						if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(e.mousePosition), out RaycastHit hit))
							position = hit.point;
						else
						{
							position.x = newPosition.x;
							position.z = newPosition.z;
						}
					}

					return true;
				}
				else if (width != newWidth)
				{
					width = newWidth;

					return true;
				}

				return false;
			}
			bool HermiteVelocity(ref float3 velocity, float3 position)
			{
				Handles.color = Settings.AIPathControlsGizmoColor;

				float3 velocityPosition = position + velocity;
				float3 newVelocityPosition = (float3)Handles.FreeMoveHandle(velocityPosition,
#if !UNITY_2022_2_OR_NEWER
					quaternion.identity,
#endif
					Settings.gizmosSize * .5f, Vector3.zero, Handles.CylinderHandleCap);

				if (!velocityPosition.Equals(newVelocityPosition))
				{
					float3 finalVelocityPosition = velocityPosition;

					if (Physics.Raycast(HandleUtility.GUIPointToWorldRay(e.mousePosition), out RaycastHit hit))
						finalVelocityPosition = hit.point;
					else
					{
						finalVelocityPosition.x = newVelocityPosition.x;
						finalVelocityPosition.z = newVelocityPosition.z;
					}

					velocity = finalVelocityPosition - position;

					return true;
				}

				return false;
			}

			if (curvesCount < 1)
			{
				VehicleAIPath.SpacedPathPoint? firstPoint = instance.FirstPoint;

				if (firstPoint.HasValue)
				{
					VehicleAIPath.SpacedPathPoint firstPointValue = firstPoint.Value;
					float drawWidth = instance.DrawWidth;

					if (CurvePoint(ref firstPointValue.spacedCurvePoint.curvePoint.position, ref drawWidth, instance.transform.rotation, 0))
					{
						Undo.RegisterCompleteObjectUndo(instance, "Change Point");

						instance.FirstPoint = firstPointValue;
						instance.DrawWidth = drawWidth;

						EditorUtility.SetDirty(instance);
					}
				}

				return;
			}

			bool loopedPath = instance.LoopedPath && curvesCount > 1;

			EditorGUI.BeginChangeCheck();

			for (int i = 0; i < curvesCount; i++)
			{
				bool selectedCurve = !moveAnchorsManually && selectedPathPoint.curveIndex == i && e.shift;
				Color selectedColor = Settings.AIPathBezierSelectedColor;
				var hermiteCurve = hermiteCurves[i];
				var bezierCurve = bezierCurves[i];

				Handles.DrawBezier(bezierCurve.P0, bezierCurve.P3, bezierCurve.P1, bezierCurve.P2, selectedCurve ? selectedColor : Settings.AIPathBezierColor, null, Settings.gizmosSize * (selectedCurve ? 6f : 2f));

				if (selectedCurve)
				{
					Handles.color = selectedColor;

					Handles.FreeMoveHandle(selectedPathPoint.spacedCurvePoint.curvePoint.position,
#if !UNITY_2022_2_OR_NEWER
						selectedPathPoint.spacedCurvePoint.curvePoint.rotation,
#endif
						Settings.gizmosSize, Vector3.zero, Handles.CylinderHandleCap);
				}

				float3 p0p0 = useHermiteCurves ? hermiteCurve.P0 : bezierCurve.P0;
				float3 p1v0 = useHermiteCurves ? hermiteCurve.V0 : bezierCurve.P1;
				float3 p2v1 = useHermiteCurves ? hermiteCurve.V1 : bezierCurve.P2;
				float3 p3p1 = useHermiteCurves ? hermiteCurve.P1 : bezierCurve.P3;
				float startWidth = bezierCurve.StartWidth;
				var startPoint = bezierCurve.GetPoint(0f);
				float endWidth = bezierCurve.EndWidth;
				float temp = 0f;

				bool p1v0Changed = !instance.UseCardinal;

				if (p1v0Changed)
				{
					p1v0Changed = useHermiteCurves ? HermiteVelocity(ref p1v0, p0p0) : CurvePoint(ref p1v0, ref temp, quaternion.identity, i * 3 + 1);

					if (p1v0Changed)
						Handles.color = selectedColor;

					if (useHermiteCurves)
						Handles.DrawAAPolyLine(Settings.gizmosSize * 2f, 2, hermiteCurve.P0, hermiteCurve.P0 + hermiteCurve.V0);
					else
						Handles.DrawAAPolyLine(Settings.gizmosSize * 2f, 2, bezierCurve.P1, bezierCurve.P0);
				}

				bool p2v1Changed = !instance.UseCardinal && (!useHermiteCurves || !loopedPath && i + 1 >= curvesCount);

				if (p2v1Changed)
				{
					p2v1Changed = useHermiteCurves ? HermiteVelocity(ref p2v1, p3p1) : CurvePoint(ref p2v1, ref temp, quaternion.identity, i * 3 + 2);

					if (p2v1Changed)
						Handles.color = selectedColor;

					if (useHermiteCurves)
						Handles.DrawAAPolyLine(Settings.gizmosSize * 2f, 2, hermiteCurve.P1, hermiteCurve.P1 + hermiteCurve.V1);
					else
						Handles.DrawAAPolyLine(Settings.gizmosSize * 2f, 2, bezierCurve.P2, bezierCurve.P3);
				}

				bool p0p0Changed = CurvePoint(ref p0p0, ref startWidth, quaternion.LookRotation(startPoint.forward, startPoint.up), i * 3);
				bool p3p1Changed = !loopedPath && i + 1 >= curvesCount;

				if (p3p1Changed)
				{
					var lastPoint = bezierCurve.GetPoint(1f);

					p3p1Changed = CurvePoint(ref p3p1, ref endWidth, quaternion.LookRotation(lastPoint.forward, lastPoint.up), i * 3 + 3);
				}

				if (p0p0Changed)
				{
					if (useHermiteCurves)
					{
						hermiteCurve.StartWidth = startWidth;
						hermiteCurve.P0 = p0p0;
					}
					else
					{
						bezierCurve.StartWidth = startWidth;
						bezierCurve.P0 = p0p0;
					}
				}

				if (p3p1Changed)
				{
					if (useHermiteCurves)
					{
						hermiteCurve.EndWidth = endWidth;
						hermiteCurve.P1 = p3p1;
					}
					else
					{
						bezierCurve.EndWidth = endWidth;
						bezierCurve.P3 = p3p1;
					}
				}

				bool hasPreviousCurve = instance.GetPreviousCurveIndex(i, out int previousCurveIndex);

				if (p1v0Changed)
				{
					if (useHermiteCurves)
					{
						hermiteCurve.V0 = p1v0;

						if (hasPreviousCurve)
						{
							var previousCurve = hermiteCurves[previousCurveIndex];

							previousCurve.V1 = p1v0;
							hermiteCurves[previousCurveIndex] = previousCurve;
						}
					}
					else
					{
						bezierCurve.P1 = p1v0;

						if (hasPreviousCurve)
						{
							var previousCurve = bezierCurves[previousCurveIndex];
							float3 previousControl = previousCurve.P2;
							float3 controlDirection = Utility.Direction(p0p0, p1v0);
							float previousControlLength = Utility.Distance(p0p0, previousControl);

							previousCurve.P2 = p0p0 + previousControlLength * -controlDirection;
							bezierCurves[previousCurveIndex] = previousCurve;
						}
					}
				}

				if (!useHermiteCurves && p2v1Changed)
				{
					bezierCurve.P2 = p2v1;

					if (instance.GetNextCurveIndex(i, out int nextCurveIndex, out BezierCurve nextCurve))
					{
						float3 nextControl = nextCurve.P1;
						float3 controlDirection = Utility.Direction(p3p1, p2v1);
						float nextControlLength = Utility.Distance(p3p1, nextControl);

						nextCurve.P1 = p3p1 + nextControlLength * -controlDirection;
						bezierCurves[nextCurveIndex] = nextCurve;
					}
				}

				if (hasPreviousCurve)
				{
					if (useHermiteCurves)
					{
						var previousCurve = hermiteCurves[previousCurveIndex];

						previousCurve.EndWidth = startWidth;
						previousCurve.P1 = hermiteCurve.P0;

						hermiteCurves[previousCurveIndex] = previousCurve;
					}
					else
					{
						var previousCurve = bezierCurves[previousCurveIndex];

						previousCurve.EndWidth = startWidth;
						previousCurve.P3 = bezierCurve.P0;

						bezierCurves[previousCurveIndex] = previousCurve;
					}
				}

				if (useHermiteCurves)
					hermiteCurves[i] = hermiteCurve;
				else
					bezierCurves[i] = bezierCurve;
			}

			if (EditorGUI.EndChangeCheck())
			{
				Undo.RegisterCompleteObjectUndo(instance, "Change Point");

				switch (instance.curveType)
				{
					case VehicleAIPath.CurveType.Hermite:
						instance.HermiteCurves = hermiteCurves;

						break;

					default:
						instance.BezierCurves = bezierCurves;

						break;
				}

				UndoRedoPerformed();
				EditorUtility.SetDirty(instance);
			}

			if (loopedPath)
			{
				float[] segmentsLength = new float[curvesCount];

				for (int i = 0; i < curvesCount; i++)
					segmentsLength[i] = instance.GetCurveLength(i);
				
				float length = Mathf.Lerp(0f, instance.TotalLength, instance.startOffset);
				float curveStart = 0f;
				float curveEnd = 0f;
				int curveIndex = 0;

				for (int i = 0; i < curvesCount; i++)
				{
					if (length <= curveEnd && i > 0)
						break;

					if (i > 0)
						curveStart += segmentsLength[i - 1];

					curveEnd += segmentsLength[i];
					curveIndex = i;
				}

				var startCurve = bezierCurves[curveIndex];
				var startPoint = startCurve.GetPoint(Mathf.InverseLerp(curveStart, curveEnd, length));

				Handles.color = Settings.AIPathBezierStartAnchorColor;

				Handles.CylinderHandleCap(0, startPoint.position, startPoint.rotation, Settings.gizmosSize, EventType.Repaint);
			}
		}
#endif

		#endregion

		#region GUI, Enable & Destroy

#if !MVC_COMMUNITY
		private void OnSceneGUI()
		{
			if (EditorApplication.isPlaying)
				return;

			UserInput();
			CurvesGUI();
		}
#endif
		private void OnEnable()
		{
			instance = target as VehicleAIPath;
#if !MVC_COMMUNITY

			if (instance.BezierCurves == null || instance.HermiteCurves == null)
			{
				instance.NewPath();
				EditorUtility.SetDirty(instance);
			}

			Undo.undoRedoPerformed += UndoRedoPerformed;
#endif
		}
#if !MVC_COMMUNITY
		private void OnDestroy()
		{
			Undo.undoRedoPerformed -= UndoRedoPerformed;
		}
		private void UndoRedoPerformed()
		{
			instance.GenerateSpacedPoints();
			instance.AlignSpacedPointsToGround();

			if (instance.IsShowingVisualizer)
			{
				instance.CreatePathVisualizer();
				instance.DrawPathVisualizer();
			}
		}
#endif

		#endregion

		#endregion

		#endregion
	}
}

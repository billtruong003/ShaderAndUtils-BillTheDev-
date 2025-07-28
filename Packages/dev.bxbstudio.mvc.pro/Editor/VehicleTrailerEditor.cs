#region Namespaces

using UnityEngine;
using UnityEditor;
using Utilities;
using Utilities.Editor;
using MVC.Editor;
using MVC.Core;
using MVC.Core.Editor;
using MVC.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Base.Editor
{
	[CustomEditor(typeof(VehicleTrailer))]
	public class VehicleTrailerEditor : VehicleEditor
	{
		#region Variables

		private Transform standsEdit;
		private bool editIdleStandsPosition;
		private bool editLiftedStandsPosition;
		private bool editAnchor;
		private bool editJoint;

		#endregion

		#region Methods

		#region Virtual Methods

		public override void OnInspectorGUI()
		{
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(!Instance.isActiveAndEnabled || !Instance.gameObject.activeInHierarchy);

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

			var hideFlag = Settings.useHideFlags ? HideFlags.HideInHierarchy : HideFlags.None;

			if (Instance.Rigidbody.hideFlags != hideFlag)
				Instance.Rigidbody.hideFlags = hideFlag;

			EditorGUILayout.LabelField("Trailer Configurations", EditorStyles.boldLabel);

			#region Components Foldout

			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Components", "Add and Edit wheels, chassis and more components..."), EditorStyles.boldLabel);
			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled)
				GUILayout.Button(new GUIContent(Instance.Problems.HasComponentsWarnings ? WarningIcon : Instance.Problems.HasComponentsErrors ? ErrorIcon : Instance.Problems.HasComponentsIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(componentsFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				componentsFoldout = !componentsFoldout;

			EditorGUILayout.EndHorizontal();

			if (componentsFoldout)
			{
				EditorGUILayout.Space();

				componentsFoldout = EnableFoldout();

				ChassisEditor();
				WheelsEditor();
				StandsEditor();
			}

			EditorGUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();

			#endregion

			EditorGUI.BeginDisabledGroup(Instance.Problems.HasComponentsWarnings || Instance.Problems.HasComponentsErrors);

			#region Joint Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Joint & Connectivity", "Customize the trailer's to vehicle connectivity"), EditorStyles.boldLabel);
			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled)
				GUILayout.Button(new GUIContent(Instance.Problems.HasTrailerJointIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(trailerJointFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				trailerJointFoldout = !trailerJointFoldout;

			EditorGUILayout.EndHorizontal();

			if (trailerJointFoldout)
				JointEditor();

			EditorGUILayout.EndVertical();

			#endregion

			#region Trailer Link Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Trailer Link", "Customize the trailer's link"), EditorStyles.boldLabel);

			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled && Instance.TrailerLink)
				GUILayout.Button(new GUIContent(Instance.Problems.HasTrailerLinkIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(trailerLinkFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				trailerLinkFoldout = !trailerLinkFoldout;

			EditorGUILayout.EndHorizontal();

			if (trailerLinkFoldout)
				TrailerLinkEditor();

			EditorGUILayout.EndVertical();

			#endregion

			#region Follower Modifier Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Follower Modifier", "Customize the follower's distance & height to fit the scene"), EditorStyles.boldLabel);

			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled)
				GUILayout.Button(new GUIContent(CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(trailerFollowerModifierFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				trailerFollowerModifierFoldout = !trailerFollowerModifierFoldout;

			EditorGUILayout.EndHorizontal();

			if (trailerFollowerModifierFoldout)
				FollowerModifierEditor();

			EditorGUILayout.EndVertical();

			#endregion

			EditorGUILayout.Space();

			#region Behaviour Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Behaviour", "Customize the trailer's physics behaviour"), EditorStyles.boldLabel);

			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled)
				GUILayout.Button(new GUIContent(Instance.Problems.HasBehaviourWarnings ? WarningIcon : Instance.Problems.HasBehaviourErrors ? ErrorIcon : Instance.Problems.HasBehaviourIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(behaviourFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				behaviourFoldout = !behaviourFoldout;

			EditorGUILayout.EndHorizontal();

			if (behaviourFoldout)
				BehaviourEditor();

			EditorGUILayout.EndVertical();

			#endregion

			#region Brakes Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Brakes", "Configure the trailer's stopping forces"), EditorStyles.boldLabel);

			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled && TrailerInstance.useBrakes)
				GUILayout.Button(new GUIContent(Instance.Problems.HasBrakingIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(brakesFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				brakesFoldout = !brakesFoldout;

			EditorGUILayout.EndHorizontal();

			if (brakesFoldout)
			{
				EditorGUI.indentLevel++;

				brakesFoldout = EnableFoldout();

				if (TrailerInstance.useBrakes)
				{
					BrakesTrainEditor(TrailerInstance.Brakes);
					EditorGUILayout.Space();

					if (GUILayout.Button("Disable Brakes"))
					{
						Undo.RegisterCompleteObjectUndo(TrailerInstance, "Disable Brakes");

						TrailerInstance.useBrakes = false;

						EditorUtility.SetDirty(TrailerInstance);
					}
				}
				else
				{
					EditorGUILayout.Space();

					if (GUILayout.Button("Enable Brakes"))
					{
						Undo.RegisterCompleteObjectUndo(TrailerInstance, "Enable Brakes");

						TrailerInstance.useBrakes = true;

						EditorUtility.SetDirty(TrailerInstance);
					}
				}

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();

			#endregion

			#region Suspension Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Suspension", "Here you choose if your trailer must be bouncy or stiff"), EditorStyles.boldLabel);

			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled)
				GUILayout.Button(new GUIContent(Instance.Problems.HasSuspensionsIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(suspensionsFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				suspensionsFoldout = !suspensionsFoldout;

			EditorGUILayout.EndHorizontal();

			if (suspensionsFoldout)
			{
				EditorGUI.indentLevel++;

				suspensionsFoldout = EnableFoldout();

				SuspensionTrainEditor(TrailerInstance.RearSuspension);

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();

			#endregion

			#region Tires Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Tires", "Change tire compounds and wheel friction settings"), EditorStyles.boldLabel);
			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled)
				GUILayout.Button(new GUIContent(Instance.Problems.HasTiresWarnings ? WarningIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(tiresFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				tiresFoldout = !tiresFoldout;

			EditorGUILayout.EndHorizontal();

			if (tiresFoldout)
				TiresEditor();

			EditorGUILayout.EndVertical();

			#endregion

			#region Stability Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Stability & Physics", "Edit the trailer's stability & physics systems"), EditorStyles.boldLabel);
			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled)
				GUILayout.Button(new GUIContent(Instance.Problems.HasStabilityIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(stabilityFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				stabilityFoldout = !stabilityFoldout;

			EditorGUILayout.EndHorizontal();

			if (stabilityFoldout)
				StabilityEditor();

			EditorGUILayout.EndVertical();

			#endregion

			EditorGUI.EndDisabledGroup();

			#region Messages

			MessagesEditor();

			#endregion

			EditorGUILayout.Space();
			EditorGUI.EndDisabledGroup();
			Repaint();
		}
		public override void OnSceneGUI()
		{
			base.OnSceneGUI();

			if (editIdleStandsPosition && TrailerInstance.Stands)
			{
				if (Tools.current == Tool.Move)
				{
					if (!Tools.hidden)
						Tools.hidden = true;

					Vector3 newStandsIdleLocalPosition = TrailerInstance.Chassis.transform.InverseTransformPoint(Handles.PositionHandle(TrailerInstance.Stands.position, TrailerInstance.Stands.rotation));

					if (TrailerInstance.StandsIdleLocalPosition != newStandsIdleLocalPosition)
					{
						Undo.RegisterCompleteObjectUndo(TrailerInstance.Stands, "Change Position");

						TrailerInstance.Stands.localPosition = newStandsIdleLocalPosition;

						EditorUtility.SetDirty(TrailerInstance);
						EditorUtility.SetDirty(TrailerInstance.Stands);
					}
				}
				else if (Tools.current == Tool.Rotate)
				{
					if (!Tools.hidden)
						Tools.hidden = true;

					Quaternion newStandsIdleLocalRotation = Quaternion.Euler(TrailerInstance.Chassis.transform.InverseTransformVector(Handles.RotationHandle(TrailerInstance.Stands.rotation, TrailerInstance.Stands.position).eulerAngles));

					if (TrailerInstance.StandsIdleLocalRotation != newStandsIdleLocalRotation)
					{
						Undo.RegisterCompleteObjectUndo(TrailerInstance.Stands, "Change Rotation");

						TrailerInstance.Stands.localRotation = newStandsIdleLocalRotation;

						EditorUtility.SetDirty(TrailerInstance);
						EditorUtility.SetDirty(TrailerInstance.Stands);
					}
				}
				else if (Tools.hidden)
					Tools.hidden = false;
			}
			else if (editLiftedStandsPosition && standsEdit)
			{
				if (Tools.current == Tool.Move)
				{
					if (!Tools.hidden)
						Tools.hidden = true;

					Vector3 newStandsLiftedLocalPosition = TrailerInstance.Chassis.transform.InverseTransformPoint(Handles.PositionHandle(standsEdit.position, standsEdit.rotation));

					if (TrailerInstance.StandsLiftedLocalPosition != newStandsLiftedLocalPosition)
					{
						Undo.RegisterCompleteObjectUndo(standsEdit, "Change Position");

						standsEdit.localPosition = newStandsLiftedLocalPosition;
						TrailerInstance.StandsLiftedLocalPosition = newStandsLiftedLocalPosition;

						EditorUtility.SetDirty(TrailerInstance);
						EditorUtility.SetDirty(standsEdit);
					}
				}
				else if (Tools.current == Tool.Rotate)
				{
					if (!Tools.hidden)
						Tools.hidden = true;

					Quaternion newStandsLiftedLocalRotation = Quaternion.Euler(TrailerInstance.Chassis.transform.InverseTransformVector(Handles.RotationHandle(standsEdit.rotation, standsEdit.position).eulerAngles));

					if (TrailerInstance.StandsLiftedLocalRotation != newStandsLiftedLocalRotation)
					{
						Undo.RegisterCompleteObjectUndo(standsEdit, "Change Rotation");

						standsEdit.localRotation = newStandsLiftedLocalRotation;
						TrailerInstance.StandsLiftedLocalRotation = newStandsLiftedLocalRotation;

						EditorUtility.SetDirty(TrailerInstance);
						EditorUtility.SetDirty(standsEdit);
					}
				}
				else if (Tools.hidden)
					Tools.hidden = false;
			}
			else if (editAnchor && TrailerInstance.Chassis)
			{
				if (!Tools.hidden)
					Tools.hidden = true;

				Color orgHandlesColor = Handles.color;
				Handles.color = Settings.jointsGizmoColor;

				Vector3 position = TrailerInstance.Chassis.transform.TransformPoint(TrailerInstance.Joint.Position);
				Vector3 newPosition = TrailerInstance.Chassis.transform.InverseTransformPoint(Handles.FreeMoveHandle(position,
#if !UNITY_2022_2_OR_NEWER
					Quaternion.identity,
#endif
					Settings.gizmosSize / 8f, Vector3.zero, Handles.SphereHandleCap));

				newPosition.x = 0f;

				if (TrailerInstance.Joint.Position != newPosition)
				{
					Undo.RegisterCompleteObjectUndo(TrailerInstance, "Change Position");

					TrailerInstance.Joint.Position = newPosition;

					EditorUtility.SetDirty(TrailerInstance);
				}

				Handles.color = orgHandlesColor;
			}
			else if (Tools.hidden && (!trailerLinkFoldout || !Instance.TrailerLink))
				Tools.hidden = false;
		}

		#endregion

		#region Static Methods

		[MenuItem("GameObject/MVC/Trailer", false, 22)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Trailer", false, 22)]
		public static VehicleTrailer CreateNewTrailer()
		{
			if (!CreateNewTrailerCheck())
				return null;

			if (!Selection.activeGameObject)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "To add a Vehicle Trailer component, please select a GameObject within the inspector window.", "Okay");

				return null;
			}

			Vehicle vehicle = Selection.activeGameObject.GetComponent<Vehicle>();

			if (!vehicle)
				vehicle = Selection.activeGameObject.GetComponentInParent<Vehicle>();

			if (!vehicle)
			{
				Undo.RegisterCompleteObjectUndo(Selection.activeGameObject, "Add Controller");

				EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller", "Adding components...", 0f);

				GameObject trailerGameObject = Selection.activeGameObject;

				trailerGameObject.tag = Settings.playerVehicleTag;

				Undo.AddComponent<VehicleTrailer>(trailerGameObject);

				EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller", "Modifying the hierarchy...", .1f);

				Transform chassisTransform = trailerGameObject.transform.Find("Chassis");

				if (!chassisTransform)
				{
					chassisTransform = new GameObject("Chassis").transform;

					chassisTransform.SetParent(trailerGameObject.transform, false);
				}

				foreach (Transform children in trailerGameObject.GetComponentsInChildren<Transform>())
					if (children != trailerGameObject.transform && children != chassisTransform && children.parent == trailerGameObject.transform)
						children.SetParent(chassisTransform, true);

				EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller", "Modifying the hierarchy...", .5f);

				Transform wheelBrakesContainer = chassisTransform.Find("WheelBrakes");

				if (wheelBrakesContainer)
					wheelBrakesContainer.SetParent(trailerGameObject.transform, true);
				else
				{
					wheelBrakesContainer = new GameObject("WheelBrakes").transform;

					wheelBrakesContainer.SetParent(trailerGameObject.transform, false);
				}

				Transform wheelTransformsContainer = chassisTransform.Find("WheelTransforms");

				if (wheelTransformsContainer)
					wheelTransformsContainer.SetParent(trailerGameObject.transform, true);
				else
				{
					wheelTransformsContainer = new GameObject("WheelTransforms").transform;

					wheelTransformsContainer.SetParent(trailerGameObject.transform, false);
				}

				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "We have successfully added the Vehicle Trailer component to the selected GameObject!", "Okay");
				EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller", "Finishing...", 1f);
			}
			else
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The selected GameObject have a Vehicle Trailer component attached to it already!", "Okay");

			vehicle.RefreshLayersAndTags();
			EditorUtility.SetDirty(vehicle.gameObject);
			EditorUtility.ClearProgressBar();

			return vehicle as VehicleTrailer;
		}

		[MenuItem("GameObject/MVC/Trailer", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Trailer", true)]
		protected static bool CreateNewTrailerCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		#endregion

		#region Global Methods

		#region Utilities

		private void EditStandsLiftedPosition()
		{
			if (editLiftedStandsPosition && standsEdit || !TrailerInstance.Chassis)
				return;

			standsEdit = Instantiate(TrailerInstance.Stands.gameObject, TrailerInstance.Chassis.transform.TransformPoint(TrailerInstance.StandsLiftedLocalPosition), TrailerInstance.Chassis.transform.rotation * TrailerInstance.StandsLiftedLocalRotation, TrailerInstance.Stands.parent).transform;
			standsEdit.hideFlags = HideFlags.HideInHierarchy;
			editLiftedStandsPosition = true;
			editIdleStandsPosition = false;

			TrailerInstance.Stands.gameObject.SetActive(false);
		}
		private void SaveStandsLiftedPosition()
		{
			if (!editLiftedStandsPosition)
				return;

			if (standsEdit)
				DestroyImmediate(standsEdit.gameObject);

			editLiftedStandsPosition = false;

			TrailerInstance.Stands.gameObject.SetActive(true);
		}

		#endregion

		#region Editor

		internal void StandsEditor()
		{
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUI.BeginDisabledGroup(!TrailerInstance.Chassis);
			EditorGUILayout.LabelField("Stands", EditorStyles.miniBoldLabel);
			EditorGUILayout.BeginVertical(GUI.skin.box);

			Transform newStands = EditorGUILayout.ObjectField(new GUIContent("Model", "The trailer stands transform"), TrailerInstance.Stands, typeof(Transform), true) as Transform;

			if (TrailerInstance.Stands != newStands)
			{
				Undo.RegisterCompleteObjectUndo(TrailerInstance, "Change Model");

				TrailerInstance.Stands = newStands;

				EditorUtility.SetDirty(TrailerInstance);
			}

			EditorGUILayout.EndVertical();

			if (TrailerInstance.Stands)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUI.BeginDisabledGroup(editLiftedStandsPosition);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Idle Position", EditorStyles.miniBoldLabel);

				if (GUILayout.Button(editIdleStandsPosition ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					editIdleStandsPosition = !editIdleStandsPosition;
					editLiftedStandsPosition = false;
				}

				EditorGUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();

				if (editIdleStandsPosition)
				{
					EditorGUI.indentLevel++;

					Vector3 newStandsIdleLocalPosition = EditorGUILayout.Vector3Field(new GUIContent(EditorGUIUtility.IconContent("MoveTool")) { tooltip = "Stands idle local position" }, TrailerInstance.StandsIdleLocalPosition);

					if (TrailerInstance.StandsIdleLocalPosition != newStandsIdleLocalPosition)
					{
						Undo.RegisterCompleteObjectUndo(TrailerInstance.Stands, "Change Position");

						TrailerInstance.StandsIdleLocalPosition = newStandsIdleLocalPosition;

						EditorUtility.SetDirty(TrailerInstance);
						EditorUtility.SetDirty(TrailerInstance.Stands);
					}

					Quaternion newStandsIdleLocalRotation = Quaternion.Euler(EditorGUILayout.Vector3Field(new GUIContent(EditorGUIUtility.IconContent("RotateTool")) { tooltip = "Stands idle local rotation" }, TrailerInstance.StandsIdleLocalRotation.eulerAngles));

					if (TrailerInstance.StandsIdleLocalRotation != newStandsIdleLocalRotation)
					{
						Undo.RegisterCompleteObjectUndo(TrailerInstance.Stands, "Change Rotation");

						TrailerInstance.StandsIdleLocalRotation = newStandsIdleLocalRotation;

						EditorUtility.SetDirty(TrailerInstance);
						EditorUtility.SetDirty(TrailerInstance.Stands);
					}

					EditorGUI.indentLevel--;
				}

				EditorGUILayout.EndVertical();
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUI.BeginDisabledGroup(editIdleStandsPosition);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField("Lifted Position", EditorStyles.miniBoldLabel);

				if (editLiftedStandsPosition)
				{
					if (GUILayout.Button(EditorUtilities.Icons.ChevronUp, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						SaveStandsLiftedPosition();
				}
				else if (GUILayout.Button(EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					EditStandsLiftedPosition();

				EditorGUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();

				if (editLiftedStandsPosition)
				{
					EditorGUI.indentLevel++;

					Vector3 newStandsLiftedLocalPosition = EditorGUILayout.Vector3Field(new GUIContent(EditorGUIUtility.IconContent("MoveTool")) { tooltip = "Stands idle local position" }, TrailerInstance.StandsLiftedLocalPosition);

					if (TrailerInstance.StandsLiftedLocalPosition != newStandsLiftedLocalPosition)
					{
						Undo.RegisterCompleteObjectUndo(standsEdit, "Change Position");

						TrailerInstance.StandsLiftedLocalPosition = newStandsLiftedLocalPosition;
						standsEdit.localPosition = newStandsLiftedLocalPosition;

						EditorUtility.SetDirty(TrailerInstance);
						EditorUtility.SetDirty(standsEdit);
					}

					Quaternion newStandsLiftedLocalRotation = Quaternion.Euler(EditorGUILayout.Vector3Field(new GUIContent(EditorGUIUtility.IconContent("RotateTool")) { tooltip = "Stands idle local rotation" }, TrailerInstance.StandsLiftedLocalRotation.eulerAngles));

					if (TrailerInstance.StandsLiftedLocalRotation != newStandsLiftedLocalRotation)
					{
						Undo.RegisterCompleteObjectUndo(standsEdit, "Change Rotation");

						TrailerInstance.StandsLiftedLocalRotation = newStandsLiftedLocalRotation;
						standsEdit.localRotation = newStandsLiftedLocalRotation;

						EditorUtility.SetDirty(TrailerInstance);
						EditorUtility.SetDirty(standsEdit);
					}

					EditorGUI.indentLevel--;
				}

				EditorGUILayout.EndVertical();
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("Timing", EditorStyles.miniBoldLabel);

				EditorGUI.indentLevel++;

				float newStandsLiftTime = ToolkitEditorUtility.NumberField(new GUIContent("Lift Duration", "The time it takes the Stands transform to lift/unlift fully"), TrailerInstance.StandsLiftTime * 1000f, Utility.Units.TimeAccurate, true, Instance, "Change Duration") * .001f;

				if (TrailerInstance.StandsLiftTime != newStandsLiftTime)
					TrailerInstance.StandsLiftTime = newStandsLiftTime;

				EditorGUI.indentLevel--;

				EditorGUILayout.EndVertical();
			}

			EditorGUI.EndDisabledGroup();

			if (!TrailerInstance.Chassis)
				EditorGUILayout.HelpBox("To use Stands, please add a Chassis to this trailer!", MessageType.Info);

			EditorGUILayout.EndVertical();
		}
		internal void JointEditor()
		{
			trailerJointFoldout = EnableFoldout();

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Anchor", EditorStyles.miniBoldLabel);

			if (GUILayout.Button(editAnchor ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				editAnchor = !editAnchor;

			EditorGUILayout.EndHorizontal();

			if (editAnchor)
			{
				EditorGUI.indentLevel++;

				EditorGUILayout.Space();

				Vector3 newPosition = EditorGUILayout.Vector3Field(new GUIContent(EditorGUIUtility.IconContent("MoveTool")) { tooltip = "The Joint anchor point" }, TrailerInstance.Joint.Position);

				newPosition.x = 0f;

				if (TrailerInstance.Joint.Position != newPosition)
				{
					Undo.RegisterCompleteObjectUndo(TrailerInstance, "Change Position");

					TrailerInstance.Joint.Position = newPosition;

					EditorUtility.SetDirty(TrailerInstance);
				}

				Vector3 newRotationAxis = EditorGUILayout.Vector3Field(new GUIContent(EditorGUIUtility.IconContent("ScaleTool")) { tooltip = "The Joint rotation axis" }, TrailerInstance.Joint.RotationAxis);

				if (TrailerInstance.Joint.RotationAxis != newRotationAxis)
				{
					Undo.RegisterCompleteObjectUndo(TrailerInstance, "Change Direction");

					TrailerInstance.Joint.RotationAxis = newRotationAxis;

					EditorUtility.SetDirty(TrailerInstance);
				}

				EditorGUILayout.Space();

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Joint", EditorStyles.miniBoldLabel);

			if (GUILayout.Button(editJoint ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				editJoint = !editJoint;

			EditorGUILayout.EndHorizontal();

			if (editJoint)
			{
				EditorGUILayout.Space();

				EditorGUI.indentLevel++;

				EditorGUILayout.LabelField(new GUIContent("Angular X Limit", "The Joint rotation limit on the X axis. Measured in Degrees (°)"));

				EditorGUI.indentLevel++;

				float newLimitMin = ToolkitEditorUtility.NumberField("Min", TrailerInstance.Joint.angularMotionXLimit.Min, "°", "Degrees", 1, Instance, "Change Angle");

				if (TrailerInstance.Joint.angularMotionXLimit.Min != newLimitMin)
					TrailerInstance.Joint.angularMotionXLimit.Min = newLimitMin;

				float newLimitMax = ToolkitEditorUtility.NumberField("Max", TrailerInstance.Joint.angularMotionXLimit.Max, "°", "Degrees", 1, Instance, "Change Angle");

				if (TrailerInstance.Joint.angularMotionXLimit.Max != newLimitMax)
					TrailerInstance.Joint.angularMotionXLimit.Max = newLimitMax;

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();

				float newBreakForce = ToolkitEditorUtility.NumberField(new GUIContent("Break Force", "Required force value to break the trailer joint"), TrailerInstance.Joint.BreakForce, Utility.Units.Force, 1, Instance, "Change Force");

				if (newBreakForce != TrailerInstance.Joint.BreakForce)
					TrailerInstance.Joint.BreakForce = newBreakForce;

				float newBreakTorque = ToolkitEditorUtility.NumberField(new GUIContent("Break Torque", "Required torque value to break the trailer joint"), TrailerInstance.Joint.BreakTorque, Utility.Units.Torque, 1, Instance, "Change Torque");

				if (newBreakTorque != TrailerInstance.Joint.BreakTorque)
					TrailerInstance.Joint.BreakTorque = newBreakTorque;

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
			}

			EditorGUILayout.EndVertical();
		}
		internal void FollowerModifierEditor()
		{
			trailerFollowerModifierFoldout = EnableFoldout();

			EditorGUILayout.Space();

			EditorGUI.indentLevel++;

			float newDistance = ToolkitEditorUtility.Slider(new GUIContent("Distance", "The follower's modifier distance multiplier"), TrailerInstance.FollowerModifier.Distance, .1f, 3f, Instance, "Change Distance");

			if (TrailerInstance.FollowerModifier.Distance != newDistance)
				TrailerInstance.FollowerModifier.Distance = newDistance;

			float newHeight = ToolkitEditorUtility.Slider(new GUIContent("Height", "The follower's modifier height multiplier"), TrailerInstance.FollowerModifier.Height, .1f, 2f, Instance, "Change Height");

			if (TrailerInstance.FollowerModifier.Height != newHeight)
				TrailerInstance.FollowerModifier.Height = newHeight;

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
		}

		#endregion

		#region Destroy & Disable

		private new void OnEnable()
		{
			base.OnEnable();
		}
		private new void OnDestroy()
		{
			base.OnDestroy();

			SaveStandsLiftedPosition();

			editIdleStandsPosition = false;
			editAnchor = false;
		}
		private new void OnDisable()
		{
			OnDestroy();
		}

		#endregion

		#endregion

		#endregion
	}
}

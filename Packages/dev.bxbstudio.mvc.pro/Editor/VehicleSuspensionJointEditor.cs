/*#region Namespaces

using UnityEngine;
using UnityEditor;
using Utilities;
using MVC.Editor;

#endregion

namespace MVC.Base.Editor
{
	[CustomEditor(typeof(VehicleSuspensionJoint))]
	public class VehicleSuspensionJointEditor : ToolkitBehaviourEditor
	{
		#region Variables

		public VehicleSuspensionJoint Instance
		{
			get
			{
				if (!instance)
					instance = target as VehicleSuspensionJoint;

				return instance;
			}
		}

		private VehicleSuspensionJoint instance;
		private bool editOrigin;
		private bool editTarget;

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
			else if (!ReadyForUse || !IsLicenseValid)
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

				EditorGUILayout.HelpBox("This joint component is invalid cause it's Vehicle parent cannot not be found or has been disabled!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}

			#endregion

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, VehicleEditorUtility.UnstretchableMiniButtonWide))
				VehicleEditorUtility.SelectObject(Instance.VehicleInstance.gameObject);

			GUILayout.Space(5f);
			EditorGUILayout.LabelField("Suspension Joint Configurations", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("General", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			Vector3 newRotationAxis = EditorGUILayout.Vector3Field(new GUIContent("Axis", "The joint's pivot rotation axis"), Instance.rotationAxis);

			if (Instance.rotationAxis != newRotationAxis)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Rotation");

				Instance.rotationAxis = newRotationAxis.normalized;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Origin", EditorStyles.miniBoldLabel);

			VehicleSuspensionJoint.JointPivot newOrigin = JointPointEditor(Instance.OriginPivot, ref editOrigin);

			if (Instance.OriginPivot != newOrigin)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Origin");

				Instance.OriginPivot = newOrigin;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Target", EditorStyles.miniBoldLabel);

			VehicleSuspensionJoint.JointPivot newTarget = JointPointEditor(Instance.TargetPivot, ref editTarget);

			if (Instance.TargetPivot != newTarget)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Target");

				Instance.TargetPivot = newTarget;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}

		#endregion

		#region Global Methods

		#region Editor

		private VehicleSuspensionJoint.JointPivot JointPointEditor(VehicleSuspensionJoint.JointPivot jointPivot, ref bool foldout)
		{
			EditorGUI.indentLevel++;

			EditorGUILayout.BeginHorizontal();

			jointPivot.type = (VehicleSuspensionJoint.JointPivot.PivotType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The origin pivot type"), jointPivot.type);

			if (GUILayout.Button(foldout ? EditorUtilities.Icons.Save : EditorUtilities.Icons.Pencil, VehicleEditorUtility.UnstretchableMiniButtonWide))
				foldout = !foldout;

			EditorGUILayout.EndHorizontal();

			if (jointPivot.type == VehicleSuspensionJoint.JointPivot.PivotType.Transform && (!jointPivot.transform || foldout))
				jointPivot.transform = EditorGUILayout.ObjectField(new GUIContent("Transform", "The transform pivot object to follow"), jointPivot.transform, typeof(Transform), true) as Transform;

			if (foldout)
				jointPivot.position = EditorGUILayout.Vector3Field(new GUIContent("Position", "The pivot local position"), jointPivot.position);

			EditorGUI.indentLevel--;

			return jointPivot;
		}

		#endregion

		#region GUI

		private void OnSceneGUI()
		{
			if (editOrigin || editTarget)
			{
				if (Tools.current == Tool.Move)
				{
					if (!Tools.hidden)
						Tools.hidden = true;

					Handles.color = Settings.jointsGizmoColor;

					Vector3 gizmosDirection = Instance.VehicleInstance.transform.rotation * Instance.rotationAxis;

					if (editOrigin)
					{
						VehicleSuspensionJoint.JointPivot newOrigin = Instance.OriginPivot;

						newOrigin.position = Handles.PositionHandle(newOrigin.position, Instance.VehicleInstance.transform.rotation);

						Handles.DrawLine(Instance.OriginPivot.GetPoint() - gizmosDirection * .125f, Instance.OriginPivot.GetPoint() + gizmosDirection * .125f);
						
						if (Instance.OriginPivot != newOrigin)
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Change Origin");

							Instance.OriginPivot = newOrigin;

							EditorUtility.SetDirty(Instance);
						}

						Handles.SphereHandleCap(0, Instance.OriginPivot.GetPoint(), Quaternion.identity, Settings.gizmosSize * .0625f, EventType.Repaint);
					}

					if (editTarget)
					{
						VehicleSuspensionJoint.JointPivot newTarget = Instance.TargetPivot;

						newTarget.position = Handles.PositionHandle(newTarget.position, Instance.VehicleInstance.transform.rotation);

						Handles.DrawLine(Instance.TargetPivot.GetPoint() - gizmosDirection * .125f, Instance.TargetPivot.GetPoint() + gizmosDirection * .125f);
						
						if (Instance.TargetPivot != newTarget)
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Change Target");

							Instance.TargetPivot = newTarget;

							EditorUtility.SetDirty(Instance);
						}

						Handles.SphereHandleCap(0, Instance.TargetPivot.GetPoint(), Quaternion.identity, Settings.gizmosSize * .0625f, EventType.Repaint);
					}
				}
			}
			else if (Tools.hidden)
				Tools.hidden = false;
		}

		#endregion

		#endregion

		#endregion
	}
}*/

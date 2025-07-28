/*#region Namespaces

using UnityEditor;
using UnityEngine;
using Utilities;
using MVC.Editor;
using MVC.Core.Editor;

#endregion

namespace MVC.Base.Editor
{
	[CustomEditor(typeof(VehicleChassisPart))]
	public class VehicleChassisPartEditor : ToolkitBehaviourEditor
	{
		#region Variables

		public VehicleChassisPart Instance
		{
			get
			{
				if (!instance)
					instance = (VehicleChassisPart)target;

				return instance;
			}
		}

		private VehicleChassisPart instance;

		#endregion

		#region Methods

		public override void OnInspectorGUI()
		{
			#region Messages

			if (HasInternalErrors)
			{
				Color orgGUIBackgroundColor = GUI.backgroundColor;

				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("The Multiversal Vehicle Controller is facing some internal problems that need to be fixed!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (GUILayout.Button("Try some quick fixes"))
					FixInternalProblems(Instance.gameObject);

				EditorGUILayout.Space();

				return;
			}
			else if (!Instance.ChassisInstance)
			{
				Color orgGUIBackgroundColor = GUI.backgroundColor;

				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("This Vehicle Chassis Part component is invalid cause it's Chassis parent cannot not be found or has been disabled!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}
			else if (!ToolkitSettings.IsProVersion)
			{
				Color orgGUIBackgroundColor = GUI.backgroundColor;

				EditorGUILayout.Space();

				GUI.backgroundColor = Color.green;

				EditorGUILayout.HelpBox("For this feature to work, you have to buy the Plus/Pro version of the Multiversal Vehicle Controller!", MessageType.Info);

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (GUILayout.Button(ToolkitSettings.IsRegistered() ? "Upgrade to Plus/Pro" : "Choose a plan!"))
					ToolkitSettingsEditor.OpenSettingsWindow();

				EditorGUILayout.Space();

				return;
			}

			#endregion

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, VehicleEditorUtility.UnstretchableMiniButtonWide))
			{
				VehicleEditorUtility.SelectObject(Instance.ChassisInstance.gameObject);

				VehicleChassisEditor.partsFoldout = VehicleChassisEditor.EnableFoldout();
			}

			GUILayout.Space(5f);

			EditorGUILayout.LabelField("Chassis Part Configurations", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			VehicleChassisPart.PartType newType = (VehicleChassisPart.PartType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The type of the chassis part"), Instance.type);

			if (Instance.type != newType)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

				Instance.type = newType;

				EditorUtility.SetDirty(Instance);
			}

			float newMass = math.max(VehicleEditorUtility.NumberField(new GUIContent("Mass", "The part's mass"), Instance.mass, Utility.Units.Weight, 1, null, Instance, "Change Mass"), newWingSpeedRange.Min);

			if (Instance.mass != newMass)
				Instance.mass = newMass;

			if (Settings.useDamage)
			{
				bool newIsDamageable = EditorGUILayout.Toggle(new GUIContent("Damageable", "Is this part damageable?"), Instance.isDamageable);

				if (Instance.isDamageable != newIsDamageable)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Toggle");

					Instance.isDamageable = newIsDamageable;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUILayout.Space();

				if (Instance.type == VehicleChassisPart.PartType.Bumper || Instance.type == VehicleChassisPart.PartType.SideSkirt || Instance.type == VehicleChassisPart.PartType.Wing)
				{
					if (Instance.pivots.Length < 2)
						Instance.pivots = new Vector3[2];

					Vector3 newFirstPivot = EditorGUILayout.Vector3Field(new GUIContent("Pivot 1", "The pivot (anchor) point of the joint"), Instance.pivots[0]);

					if (Instance.pivots[0] != newFirstPivot)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Pivot Point");

						Instance.pivots[0] = newFirstPivot;

						EditorUtility.SetDirty(Instance);
					}

					Vector3 newSecondPivot = EditorGUILayout.Vector3Field(new GUIContent("Pivot 2", "The pivot (anchor) point of the joint"), Instance.pivots[1]);

					if (Instance.pivots[1] != newSecondPivot)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Pivot Point");

						Instance.pivots[1] = newSecondPivot;

						EditorUtility.SetDirty(Instance);
					}
				}
				else
				{
					if (Instance.pivots.Length < 1)
						Instance.pivots = new Vector3[1];

					Vector3 newPivot = EditorGUILayout.Vector3Field(new GUIContent("Pivot Point", "The pivot (anchor) point of the joint"), Instance.pivots[0]);

					if (Instance.pivots[0] != newPivot)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Pivot Point");

						Instance.pivots[0] = newPivot;

						EditorUtility.SetDirty(Instance);
					}
				}

				EditorGUILayout.Space();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField("Static Joint", Instance.Joints.staticJoint, typeof(FixedJoint), false);
				EditorGUILayout.ObjectField("Damaged Joint", Instance.Joints.dynamicJoint, typeof(HingeJoint), false);
				EditorGUI.EndDisabledGroup();
			}

			EditorGUILayout.Space();
		}

		#endregion
	}
}*/

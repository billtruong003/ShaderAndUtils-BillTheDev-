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

namespace MVC.IK.Editor
{
	[CustomEditor(typeof(VehicleIKPivot))]
	public class VehicleIKPivotEditor : ToolkitBehaviourExtensionEditor<Vehicle>
	{
		#region Variables

		private VehicleIKPivot Instance
		{
			get
			{
				if (!instance)
					instance = (VehicleIKPivot)target;

				return instance;
			}
		}
		private VehicleIKPivot instance;

		#endregion

		#region Methods

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
			else if (!Base)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("This Vehicle IK Pivot component is invalid because its Vehicle parent cannot not be found or has been disabled!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}
			else if (!Base.Chassis)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("This Vehicle IK Pivot component is invalid because its Vehicle Chassis parent cannot not be found!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}

			#endregion

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
			{
				ToolkitEditorUtility.SelectObject(Base.gameObject);

				VehicleEditor.driverIKFoldout = VehicleEditor.EnableFoldout();
				VehicleEditor.IKPivotsFoldout = VehicleEditor.EnableInternalFoldout();
			}

			GUILayout.Space(5f);
			EditorGUILayout.LabelField($"Vehicle IK Pivot Configurations", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
#if MVC_COMMUNITY

			GUI.backgroundColor = Color.green;
				
			EditorGUILayout.HelpBox("This feature is only included with Multiversal Vehicle Controller Pro!", MessageType.Info);

			GUI.backgroundColor = Color.yellow;

			EditorGUILayout.HelpBox("This component exist only for the purpose of not losing references when switching between versions.", MessageType.Warning);

			GUI.backgroundColor = orgGUIBackgroundColor;

			if (GUILayout.Button("Upgrade to Pro"))
				ToolkitEditorWindow.VisitWebsite();

#else
			EditorGUILayout.HelpBox("Move this IK Pivot using Scene handles or through the inspector's Transform component", MessageType.Info);
#endif
			EditorGUILayout.Space();
		}

		#endregion
	}
}

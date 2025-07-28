#region Namespaces

using UnityEngine;
using UnityEditor;
using MVC.Editor;

#endregion

namespace MVC.Base.Editor
{
	[CustomEditor(typeof(VehicleTrailerLink))]
	public class VehicleTrailerLinkEditor : ToolkitBehaviourEditor
	{
		#region Variables

		public VehicleTrailerLink Instance
		{
			get
			{
				if (!instance)
					instance = target as VehicleTrailerLink;

				return instance;
			}
		}

		private VehicleTrailerLink instance;

		#endregion

		#region Methods

		public override void OnInspectorGUI()
		{
			EditorGUILayout.Space();

			Color orgGUIBackgroundColor = GUI.backgroundColor;

			if (HasInternalErrors)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("The Multiversal Vehicle Controller is facing some internal problems that need to be fixed!", MessageType.Error);
				EditorGUILayout.Space();

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (GUILayout.Button("Try some quick fixes"))
					FixInternalProblems(Instance.gameObject);

				return;
			}
			else if (!Instance.VehicleInstance)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.Space();
				EditorGUILayout.HelpBox("This Trailer Link extension component is invalid cause it's Vehicle Component cannot not be found or has been disabled!", MessageType.Error);
				EditorGUILayout.Space();

				GUI.backgroundColor = orgGUIBackgroundColor;

				return;
			}

			EditorGUILayout.HelpBox("This Trailer Link is controlled by its main vehicle/trailer instance!", MessageType.Info);
		}

		#endregion
	}
}

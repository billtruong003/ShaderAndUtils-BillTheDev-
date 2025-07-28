#region Namespaces

using UnityEngine;
using UnityEditor;
using MVC.Editor;
using MVC.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Core.Editor
{
	[CustomEditor(typeof(VehicleGroundMapper))]
	public class VehicleGroundMapperEditor : ToolkitBehaviourEditor
	{
		#region Variables

		private VehicleGroundMapper Instance => (VehicleGroundMapper)target;

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

			#endregion

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Ground Mapper Configurations", EditorStyles.boldLabel);
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
			if (Instance.Type == VehicleGroundMapper.GroundType.Invalid)
				Instance.Restart();

			if (Instance.Type == VehicleGroundMapper.GroundType.Invalid)
			{
				EditorGUILayout.HelpBox("The ground mapper couldn't detect any type of colliders attached to this object", MessageType.Warning);
				EditorGUILayout.Space();

				return;
			}

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Properties", EditorStyles.miniBoldLabel);
			EditorGUI.BeginDisabledGroup(true);

			EditorGUI.indentLevel++;

			EditorGUILayout.EnumPopup("Ground Type", Instance.Type);

			bool simpleCollider = Instance.Type != VehicleGroundMapper.GroundType.Terrain;

			if (!simpleCollider)
				EditorGUILayout.ObjectField("Terrain", Instance.TerrainInstance, typeof(Terrain), true);

			EditorGUILayout.ObjectField("Collider", Instance.ColliderInstance, simpleCollider ? typeof(Collider) : typeof(TerrainCollider), true);

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Ground Map", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			for (int i = 0; i < (simpleCollider ? 1 : Instance.Map.Length); i++)
			{
				var newMap = Instance.Map[i];

				EditorGUILayout.BeginVertical(GUI.skin.box);

				int newIndex = EditorGUILayout.Popup($"Layer {i}", Mathf.Clamp(newMap.index, 0, Settings.Grounds.Length - 1), Settings.GetGroundsNames(true));

				if (newMap.index != newIndex)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Map Layer");

					newMap.index = newIndex;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel++;

				float newWetness = ToolkitEditorUtility.Slider(new GUIContent("Wetness", "Ground wetness factor"), newMap.Wetness, 0f, 1f, Instance, "Change Wetness");

				if (newMap.Wetness != newWetness)
					newMap.Wetness = newWetness;

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
				EditorGUILayout.EndVertical();

				Instance.Map[i] = newMap;
			}

			EditorGUI.indentLevel--;
#endif

			EditorGUILayout.Space();
		}

		#endregion

		#region Static Methods

#if !MVC_COMMUNITY
		[MenuItem("GameObject/MVC/Ground Mapper", false, 24)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Ground Mapper", false, 24)]
#endif
		public static VehicleGroundMapper CreateNewGroundMapper()
		{
			if (!CreateNewGroundMapperCheck())
				return null;

			if (!Selection.activeGameObject)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "To add a Ground Mapper component, please select a GameObject within the inspector window.", "Okay");

				return null;
			}

			VehicleGroundMapper groundMapper = Selection.activeGameObject.GetComponent<VehicleGroundMapper>();

			if (!groundMapper)
			{
				Undo.RegisterCompleteObjectUndo(Selection.activeGameObject, "Add Ground Mapper");

				groundMapper = Undo.AddComponent<VehicleGroundMapper>(Selection.activeGameObject);

				groundMapper.Restart();

				if (groundMapper.Type == VehicleGroundMapper.GroundType.Invalid)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "The created Ground Mapper is Invalid and it's going to be disabled at Runtime, as it doesn't have any type of Colliders attached to it!", "Got it");

				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "We have successfully added a Ground Mapper component to the selected GameObject!", "Okay");
			}
			else
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The selected GameObject have a Ground Mapper component attached to it already!", "Okay");

			EditorUtility.SetDirty(groundMapper.gameObject);

			return groundMapper;
		}

#if !MVC_COMMUNITY
		[MenuItem("GameObject/MVC/Ground Mapper", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Ground Mapper", true)]
#endif
		protected static bool CreateNewGroundMapperCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		#endregion

		#region Global Methods

#if !MVC_COMMUNITY
		private void MapperEditor(bool singleGround)
		{
		}
#endif

		#endregion

		#endregion
	}
}

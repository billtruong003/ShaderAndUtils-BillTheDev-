#region Namespaces

using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using Utilities.Editor;
using MVC.Core;
using MVC.Editor;
using MVC.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.VFX.Editor
{
	[CustomEditor(typeof(VehicleWheelSpin))]
	public class VehicleWheelSpinEditor : ToolkitBehaviourEditor
	{
		#region Variables

		private VehicleWheelSpin instance;
#if !MVC_COMMUNITY
		private int newMaterialIndex;
#endif

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
			else if (!Settings.useWheelSpinEffect)
			{
				EditorGUILayout.HelpBox("This feature has been disabled! In case you want to use this feature you have to go to 'Tools > Multiversal Vehicle Controller > Edit Settings... > Visual Effects' and turn on the `Wheel Spin Effect` toggle.", MessageType.Info);

				return;
			}
			else if (!instance.IsValid)
			{
				GUI.backgroundColor = Color.yellow;

				EditorGUILayout.HelpBox("This `Vehicle Wheel Spin` component is invalid, as it might not belong to any `Vehicle` or `Vehicle Wheel` component, or may not have a `Mesh Filter` or a `Mesh Renderer` component attached to it.", MessageType.Warning);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();
			}

			#endregion

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Wheel Spin Configurations", EditorStyles.boldLabel);
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
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("General", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			float newBlurAlphaOffset = ToolkitEditorUtility.Slider(new GUIContent("Alpha Offset", "The blur transparency alpha offset"), instance.BlurAlphaOffset, -1f, 1f, instance, "Change Offset");

			if (instance.BlurAlphaOffset != newBlurAlphaOffset)
				instance.BlurAlphaOffset = newBlurAlphaOffset;

			if (Settings.WheelSpinAlphaOffset != default)
			{
				EditorGUI.BeginDisabledGroup(true);

				EditorGUI.indentLevel++;

				EditorGUILayout.BeginHorizontal();
				ToolkitEditorUtility.Slider(new GUIContent("Default Offset", "The default alpha offset of the settings panel"), Settings.WheelSpinAlphaOffset, -1f, 1f);
				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					ToolkitSettingsEditor.OpenWindow(ToolkitSettings.SettingsEditorFoldout.VFX);

				EditorGUILayout.EndHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				ToolkitEditorUtility.Slider(new GUIContent("Total Offset", "The total alpha offset value"), instance.TotalBlurAlphaOffset, -1f, 1f);

				EditorGUI.indentLevel--;

				EditorGUI.EndDisabledGroup();
			}

			bool newVisibilityCulling = ToolkitEditorUtility.ToggleButtons(new GUIContent("Visibility Culling", "Should the spin mesh be hidden when it's not visible by one of the cameras?"), null, "On", "Off", instance.visibilityCulling, instance, "Switch Culling");

			if (instance.visibilityCulling != newVisibilityCulling)
				instance.visibilityCulling = newVisibilityCulling;

			bool newFlipSpinMesh = ToolkitEditorUtility.ToggleButtons(new GUIContent("Flip Mesh", "Should the runtime spin mesh normals/triangles get flipped?"), null, "Yes", "No", instance.flipSpinMesh, instance, "Switch Flip");

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (instance.flipSpinMesh != newFlipSpinMesh)
				instance.flipSpinMesh = newFlipSpinMesh;

			if (!EditorApplication.isPlaying)
				EditorGUILayout.HelpBox("If you are experiencing some visual issues with the spin mesh material, enable `Flip Mesh` to fix them. Note that enabling this feature without any purpose can cause visual issues!", MessageType.Info);

			EditorGUI.EndDisabledGroup();

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Materials", EditorStyles.miniBoldLabel);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			for (int i = 0; i < instance.SpinMaterials.Length; i++)
			{
				string materialName = $"{i + 1}. {instance.MeshRenderer.sharedMaterials[instance.SpinMaterials[i].MaterialIndex].name}";
				bool requestRemove = false;

				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(materialName, EditorStyles.miniBoldLabel);
				GUILayout.FlexibleSpace();

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					requestRemove = true;

				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel++;

				Material newSharedSpinMaterial = EditorGUILayout.ObjectField(new GUIContent("Spin Material", "The shared spin material that will replace the mesh original material"), instance.SpinMaterials[i].SharedSpinMaterial, typeof(Material), false) as Material;

				if (instance.SpinMaterials[i].SharedSpinMaterial != newSharedSpinMaterial)
				{
					Undo.RegisterCompleteObjectUndo(instance, "Change Material");

					instance.SpinMaterials[i].SharedSpinMaterial = newSharedSpinMaterial;

					EditorUtility.SetDirty(instance);
				}

				if (!instance.SpinMaterials[i].SharedSpinMaterial)
				{
					EditorGUILayout.HelpBox("To create a spin material, you can duplicate the original mesh material and make it a transparent material instead of opaque.", MessageType.Info);
					EditorGUILayout.HelpBox("Leaving the `Spin Material` field empty will lead to the spin mesh being invisible!", MessageType.Info);
				}

				EditorGUI.indentLevel--;

				EditorGUILayout.EndVertical();

				if (requestRemove)
				{
					Undo.RegisterCompleteObjectUndo(instance, "Remove Material");
					instance.RemoveSpinMaterial(instance.SpinMaterials[i].MaterialIndex);
					EditorUtility.SetDirty(instance);

					break;
				}
			}

			EditorGUILayout.BeginHorizontal(GUI.skin.box);

			string[] materialsNames = new string[] { "Add a material" };

			ArrayUtility.AddRange(ref materialsNames, instance.MeshRenderer.sharedMaterials.Select((material, index) => $"{index + 1}. {material.name}").ToArray());

			newMaterialIndex = EditorGUILayout.Popup(newMaterialIndex, materialsNames);

			EditorGUI.BeginDisabledGroup(newMaterialIndex < 1);

			if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
			{
				if (instance.SpinMaterialIndexExists(newMaterialIndex - 1))
				{
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "You can't add the selected material since it exists already.", "Okay");

					return;
				}

				Undo.RegisterCompleteObjectUndo(instance, "Add Material");
				instance.AddSpinMaterial(newMaterialIndex - 1);
				EditorUtility.SetDirty(instance);

				newMaterialIndex = default;
			}

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndVertical();
#endif
			EditorGUILayout.Space();
		}

		#endregion

		#region Static Methods

#if !MVC_COMMUNITY
		[MenuItem("GameObject/MVC/VFX/Wheel Spin Effect", false, 20)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/VFX/Wheel Spin Effect", false, 20)]
#endif
		public static VehicleWheelSpin CreateWheelSpinEffect()
		{
			if (!CreateWheelSpinEffectCheck())
				return null;

			bool selectionValid = Selection.activeGameObject && Selection.activeGameObject.GetComponent<MeshRenderer>();

			if (!selectionValid)
				goto exit;

			Vehicle vehicle = Selection.activeGameObject.GetComponentInParent<Vehicle>();

			selectionValid = vehicle && vehicle.Wheels.Length > 0 && Array.Find(vehicle.Wheels, wheel => wheel.Rim && Selection.activeGameObject.transform.IsChildOf(wheel.Rim));

			if (selectionValid)
				goto after_validation;

		exit:
			EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "In order to add a `Wheel Spin Effect` component, please select a valid wheel mesh.", "Okay");

			return null;

		after_validation:
			return Selection.activeGameObject.AddComponent<VehicleWheelSpin>();
		}

#if !MVC_COMMUNITY
		[MenuItem("GameObject/MVC/VFX/Wheel Spin Effect", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/VFX/Wheel Spin Effect", true)]
#endif
		protected static bool CreateWheelSpinEffectCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		#endregion

		#region Global Methods

		private void OnEnable()
		{
			instance = target as VehicleWheelSpin;
		}

		#endregion

		#endregion
	}
}

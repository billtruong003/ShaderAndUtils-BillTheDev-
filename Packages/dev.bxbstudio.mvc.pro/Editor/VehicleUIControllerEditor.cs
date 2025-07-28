#region Namespaces

using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Utilities;
using Utilities.Editor;
using Utilities.Inputs.Mobile;
using MVC.Editor;
using MVC.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.UI.Editor
{
	[CustomEditor(typeof(VehicleUIController))]
	public class VehicleUIControllerEditor : ToolkitBehaviourEditor
	{
		#region Variables

		#region Static Variables

		public static bool speedMeterFoldout;
		public static bool mobilePresetFoldout;

		private static int currentSpeedMeter = -1;
		private static int currentMobilePreset = -1;

		#endregion

		#region Global Variables

		public VehicleUIController Instance
		{
			get
			{
				if (!instance)
					instance = target as VehicleUIController;

				return instance;
			}
		}
		public int activeSpeedMeter = 0;

		private VehicleUIController instance;

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

			if (speedMeterFoldout)
			{
				if (currentSpeedMeter > -1 && currentSpeedMeter < Instance.SpeedMeters.Length)
					SpeedMeterEditor(currentSpeedMeter);
				else
					SpeedMetersEditor();
			}
			else if (mobilePresetFoldout)
			{
				if (currentMobilePreset > -1 && currentMobilePreset < Instance.MobilePresets.Length)
					MobilePresetEditor(currentMobilePreset);
				else
					MobilePresetsEditor();
			}
			else
			{
				EditorGUILayout.LabelField("UI Configurations", EditorStyles.boldLabel);
				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal(GUI.skin.box);
				EditorGUILayout.LabelField("Speed Meters", EditorStyles.miniBoldLabel);

				if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					EnableFoldout(ref speedMeterFoldout);

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal(GUI.skin.box);
				EditorGUILayout.LabelField("Mobile Presets", EditorStyles.miniBoldLabel);

				if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					EnableFoldout(ref mobilePresetFoldout);

				EditorGUILayout.EndHorizontal();
			}

			EditorGUILayout.Space();
		}

		#endregion

		#region Static Methods

		#region Menu Items

		[MenuItem("GameObject/MVC/UI Controller", false, 20)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/UI Controller", false, 20)]
		public static VehicleUIController CreateUIController()
		{
			if (ToolkitBehaviour.UIController)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "This scene already has a Vehicle UI Controller in it and there's no need to create a second one. If you insist on creating a new controller, consider deleting the old one.", "Okay");
				ToolkitEditorUtility.SelectObject(ToolkitBehaviour.UIController);

				return ToolkitBehaviour.UIController;
			}

			VehicleUIController controller = VehicleUIController.GetOrCreateInstance();

			ToolkitEditorUtility.SelectObject(controller.gameObject);

			return controller;
		}

		[MenuItem("GameObject/MVC/UI Controller", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/UI Controller", true)]
		protected static bool CreateUIControllerCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		#endregion

		#region Utilities

		public static void EnableFoldout(ref bool foldout)
		{
			speedMeterFoldout = false;
			//mobilePresetFoldout = false;
			foldout = true;
		}

		#endregion

		#endregion

		#region Global Methods

		#region Utilities

		private void AddSpeedMeter()
		{
			List<VehicleUIController.SpeedMeter> speedMeters = Instance.SpeedMeters.ToList();

			speedMeters.Add(new());
			Undo.RegisterCompleteObjectUndo(Instance, "Add Speed meter");

			Instance.SpeedMeters = speedMeters.ToArray();

			EditorUtility.SetDirty(Instance);
		}
		private void RemoveSpeedMeter(int index)
		{
			List<VehicleUIController.SpeedMeter> speedMeters = Instance.SpeedMeters.ToList();

			speedMeters.RemoveAt(index);
			Undo.RegisterCompleteObjectUndo(Instance, "Remove Speed meter");

			Instance.SpeedMeters = speedMeters.ToArray();

			EditorUtility.SetDirty(Instance);
		}
		private void RemoveSpeedMeters()
		{
			if (!EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Are you sure you want to remove all of the existing UI speed meters at once?", "Yes", "No"))
				return;

			Undo.RegisterCompleteObjectUndo(Instance, "Remove Speed meters");

			Instance.SpeedMeters = new VehicleUIController.SpeedMeter[] { };

			EditorUtility.SetDirty(Instance);
		}
		private void AddMobilePreset()
		{
			List<VehicleUIController.MobileInputPreset> mobilePresets = Instance.MobilePresets.ToList();

			mobilePresets.Add(new());
			Undo.RegisterCompleteObjectUndo(Instance, "Add UI Preset");

			Instance.MobilePresets = mobilePresets.ToArray();

			EditorUtility.SetDirty(Instance);
		}
		private void RemoveMobilePreset(int index)
		{
			List<VehicleUIController.MobileInputPreset> mobilePresets = Instance.MobilePresets.ToList();

			mobilePresets.RemoveAt(index);
			Undo.RegisterCompleteObjectUndo(Instance, "Remove UI Preset");

			Instance.MobilePresets = mobilePresets.ToArray();

			EditorUtility.SetDirty(Instance);
		}
		private void RemoveMobilePresets()
		{
			if (!EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Are you sure you want to remove all of the existing UI mobile presets at once?", "Yes", "No"))
				return;

			Undo.RegisterCompleteObjectUndo(Instance, "Remove UI Presets");

			Instance.MobilePresets = new VehicleUIController.MobileInputPreset[] { };

			EditorUtility.SetDirty(Instance);
		}

		#endregion

		#region GUI

		private void SpeedMetersEditor()
		{
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				speedMeterFoldout = false;

			GUILayout.Space(5f);
			EditorGUILayout.LabelField("Speed Meters", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				AddSpeedMeter();

			if (Instance.SpeedMeters.Length > 1)
				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					RemoveSpeedMeters();

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (Instance.SpeedMeters.Length > 0)
				for (int i = 0; i < Instance.SpeedMeters.Length; i++)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					
					if (Instance.SpeedMeters[i].group)
					{
						EditorGUILayout.LabelField($"Speed Meter{(Instance.SpeedMeters.Length > 1 ? $" {i + 1}" : "")}", EditorStyles.miniBoldLabel);

						if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
							currentSpeedMeter = i;
					}
					else
					{
						RectTransform newGroup = EditorGUILayout.ObjectField("New Speed Meter", Instance.SpeedMeters[i].group, typeof(RectTransform), true) as RectTransform;

						if (newGroup)
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Add Group");

							Instance.SpeedMeters[i].group = newGroup;

							EditorUtility.SetDirty(Instance);
						}
					}

					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						RemoveSpeedMeter(i);

					EditorGUILayout.EndHorizontal();
				}
			else if (!EditorApplication.isPlaying)
				EditorGUILayout.HelpBox("Click on the '+' button above to add a new UI speed meter", MessageType.Info);

			EditorGUI.EndDisabledGroup();

			if (Instance.SpeedMeters.Length > 0)
			{
				EditorGUI.BeginDisabledGroup(Instance.SpeedMeters.Length == 1);

				int newActiveSpeedMeter = Mathf.Clamp(ToolkitEditorUtility.NumberField(new GUIContent("Active Speed Meter", "The current active UI speed meter in use"), Instance.ActiveSpeedMeter), 0, Instance.SpeedMeters.Length - 1);

				if (Instance.ActiveSpeedMeter != newActiveSpeedMeter)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Active Meter");

					Instance.ActiveSpeedMeter = newActiveSpeedMeter;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.EndDisabledGroup();
			}
		}
		private void SpeedMeterEditor(int index)
		{
			VehicleUIController.SpeedMeter speedMeter = Instance.SpeedMeters[index];

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				currentSpeedMeter = -1;

			GUILayout.Space(5f);
			EditorGUILayout.LabelField("Speed Meter Configurations", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			GUIContent typeContent = new("Type", "The speed meter image behaviour type\r\nDigital: Based on dynamic Image components with fill values\r\nRadial: Based on static image sprites rotate over time");

			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("Group", speedMeter.group, typeof(RectTransform), true);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Needles", EditorStyles.miniBoldLabel);
			EditorGUILayout.BeginVertical(GUI.skin.box);

			Image newRPMNeedle = EditorGUILayout.ObjectField(new GUIContent("RPM", "The vehicle current engine RPM needle UI image"), speedMeter.RPMNeedle.Needle, typeof(Image), true) as Image;

			if (speedMeter.RPMNeedle.Needle != newRPMNeedle)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Image");

				speedMeter.RPMNeedle.Needle = newRPMNeedle;

				EditorUtility.SetDirty(Instance);
			}

			if (newRPMNeedle)
			{
				EditorGUI.indentLevel++;

				VehicleUIController.SpeedMeter.Type newRPMType = (VehicleUIController.SpeedMeter.Type)EditorGUILayout.EnumPopup(typeContent, speedMeter.RPMNeedle.type);
				float RPMFillMultiplier = newRPMType == VehicleUIController.SpeedMeter.Type.Radial ? 360f : 1f;

				if (speedMeter.RPMNeedle.type != newRPMType)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

					speedMeter.RPMNeedle.type = newRPMType;

					EditorUtility.SetDirty(Instance);
				}

				float newRPMFill = ToolkitEditorUtility.Slider("Fill", speedMeter.RPMNeedle.Fill * RPMFillMultiplier, 0f, RPMFillMultiplier) / RPMFillMultiplier;

				if (speedMeter.RPMNeedle.Fill != newRPMFill)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Fill");

					speedMeter.RPMNeedle.Fill = newRPMFill;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel++;

				bool newInvertRotation = ToolkitEditorUtility.ToggleButtons(new GUIContent("Invert Rotation", "When the Needle is of type `Radial`, you can have the option to invert its rotation direction"), null, "Yes", "No", speedMeter.RPMNeedle.InvertRotation, Instance, "Switch Invert");
				
				if (speedMeter.RPMNeedle.InvertRotation != newInvertRotation)
					speedMeter.RPMNeedle.InvertRotation = newInvertRotation;

				float newRPMFillOffset = ToolkitEditorUtility.Slider("Offset", speedMeter.RPMNeedle.Offset * RPMFillMultiplier, 0f, RPMFillMultiplier) / RPMFillMultiplier;

				if (speedMeter.RPMNeedle.Offset != newRPMFillOffset)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Offset");

					speedMeter.RPMNeedle.Offset = newRPMFillOffset;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel -= 2;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			Image newRPMOverRevNeedle = EditorGUILayout.ObjectField(new GUIContent("Over-Rev", "The vehicle engine RPM redline UI image"), speedMeter.RPMOverRevNeedle.Needle, typeof(Image), true) as Image;

			if (speedMeter.RPMOverRevNeedle.Needle != newRPMOverRevNeedle)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Image");

				speedMeter.RPMOverRevNeedle.Needle = newRPMOverRevNeedle;

				EditorUtility.SetDirty(Instance);
			}

			if (newRPMOverRevNeedle)
			{
				EditorGUI.indentLevel++;

				VehicleUIController.SpeedMeter.Type newRPMOverRevType = (VehicleUIController.SpeedMeter.Type)EditorGUILayout.EnumPopup(typeContent, speedMeter.RPMOverRevNeedle.type);
				float RPMOverRevFillMultiplier = newRPMOverRevType == VehicleUIController.SpeedMeter.Type.Radial ? 360f : 1f;

				if (speedMeter.RPMOverRevNeedle.type != newRPMOverRevType)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

					speedMeter.RPMOverRevNeedle.type = newRPMOverRevType;

					EditorUtility.SetDirty(Instance);
				}

				float newRPMOverRevFill = ToolkitEditorUtility.Slider("Fill", speedMeter.RPMOverRevNeedle.Fill * RPMOverRevFillMultiplier, 0f, RPMOverRevFillMultiplier) / RPMOverRevFillMultiplier;

				if (speedMeter.RPMOverRevNeedle.Fill != newRPMOverRevFill)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Fill");

					speedMeter.RPMOverRevNeedle.Fill = newRPMOverRevFill;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel++;

				bool newInvertRotation = ToolkitEditorUtility.ToggleButtons(new GUIContent("Invert Rotation", "When the Needle is of type `Radial`, you can have the option to invert its rotation direction"), null, "Yes", "No", speedMeter.RPMOverRevNeedle.InvertRotation, Instance, "Switch Invert");
				
				if (speedMeter.RPMOverRevNeedle.InvertRotation != newInvertRotation)
					speedMeter.RPMOverRevNeedle.InvertRotation = newInvertRotation;

				float newRPMOverRevFillOffset = ToolkitEditorUtility.Slider("Offset", speedMeter.RPMOverRevNeedle.Offset * RPMOverRevFillMultiplier, 0f, RPMOverRevFillMultiplier) / RPMOverRevFillMultiplier;

				if (speedMeter.RPMOverRevNeedle.Offset != newRPMOverRevFillOffset)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Offset");

					speedMeter.RPMOverRevNeedle.Offset = newRPMOverRevFillOffset;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel -= 2;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			Image newSpeedNeedle = EditorGUILayout.ObjectField(new GUIContent("Speed", "The vehicle current speed UI image"), speedMeter.SpeedNeedle.Needle, typeof(Image), true) as Image;
			
			if (speedMeter.SpeedNeedle.Needle != newSpeedNeedle)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Image");

				speedMeter.SpeedNeedle.Needle = newSpeedNeedle;

				EditorUtility.SetDirty(Instance);
			}

			if (newSpeedNeedle)
			{
				EditorGUI.indentLevel++;

				VehicleUIController.SpeedMeter.Type newSpeedType = (VehicleUIController.SpeedMeter.Type)EditorGUILayout.EnumPopup(typeContent, speedMeter.SpeedNeedle.type);
				float SpeedFillMultiplier = newSpeedType == VehicleUIController.SpeedMeter.Type.Radial ? 360f : 1f;

				if (speedMeter.SpeedNeedle.type != newSpeedType)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

					speedMeter.SpeedNeedle.type = newSpeedType;

					EditorUtility.SetDirty(Instance);
				}

				float newSpeedFill = ToolkitEditorUtility.Slider("Fill", speedMeter.SpeedNeedle.Fill * SpeedFillMultiplier, 0f, SpeedFillMultiplier) / SpeedFillMultiplier;

				if (speedMeter.SpeedNeedle.Fill != newSpeedFill)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Fill");

					speedMeter.SpeedNeedle.Fill = newSpeedFill;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel++;

				bool newInvertRotation = ToolkitEditorUtility.ToggleButtons(new GUIContent("Invert Rotation", "When the Needle is of type `Radial`, you can have the option to invert its rotation direction"), null, "Yes", "No", speedMeter.SpeedNeedle.InvertRotation, Instance, "Switch Invert");
				
				if (speedMeter.SpeedNeedle.InvertRotation != newInvertRotation)
					speedMeter.SpeedNeedle.InvertRotation = newInvertRotation;

				float newSpeedFillOffset = ToolkitEditorUtility.Slider("Offset", speedMeter.SpeedNeedle.Offset * SpeedFillMultiplier, 0f, SpeedFillMultiplier) / SpeedFillMultiplier;

				if (speedMeter.SpeedNeedle.Offset != newSpeedFillOffset)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Offset");

					speedMeter.SpeedNeedle.Offset = newSpeedFillOffset;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel -= 2;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			Image newNOSNeedle = EditorGUILayout.ObjectField(new GUIContent("NOS", "The vehicle current nitrous (NOS) amount UI image"), speedMeter.NOSNeedle.Needle, typeof(Image), true) as Image;

			if (speedMeter.NOSNeedle.Needle != newNOSNeedle)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Image");

				speedMeter.NOSNeedle.Needle = newNOSNeedle;

				EditorUtility.SetDirty(Instance);
			}

			if (newNOSNeedle)
			{
				EditorGUI.indentLevel++;

				VehicleUIController.SpeedMeter.Type newNOSType = (VehicleUIController.SpeedMeter.Type)EditorGUILayout.EnumPopup(typeContent, speedMeter.NOSNeedle.type);
				float NOSFillMultiplier = newNOSType == VehicleUIController.SpeedMeter.Type.Radial ? 360f : 1f;

				if (speedMeter.NOSNeedle.type != newNOSType)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

					speedMeter.NOSNeedle.type = newNOSType;

					EditorUtility.SetDirty(Instance);
				}

				float newNOSFill = ToolkitEditorUtility.Slider("Fill", speedMeter.NOSNeedle.Fill * NOSFillMultiplier, 0f, NOSFillMultiplier) / NOSFillMultiplier;

				if (speedMeter.NOSNeedle.Fill != newNOSFill)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Fill");

					speedMeter.NOSNeedle.Fill = newNOSFill;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel++;

				bool newInvertRotation = ToolkitEditorUtility.ToggleButtons(new GUIContent("Invert Rotation", "When the Needle is of type `Radial`, you can have the option to invert its rotation direction"), null, "Yes", "No", speedMeter.NOSNeedle.InvertRotation, Instance, "Switch Invert");
				
				if (speedMeter.NOSNeedle.InvertRotation != newInvertRotation)
					speedMeter.NOSNeedle.InvertRotation = newInvertRotation;

				float newNOSFillOffset = ToolkitEditorUtility.Slider("Offset", speedMeter.NOSNeedle.Offset * NOSFillMultiplier, 0f, NOSFillMultiplier) / NOSFillMultiplier;

				if (speedMeter.NOSNeedle.Offset != newNOSFillOffset)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Offset");

					speedMeter.NOSNeedle.Offset = newNOSFillOffset;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel -= 2;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			Image newBoostNeedle = EditorGUILayout.ObjectField(new GUIContent("Boost", "The vehicle engine boost pressure UI image"), speedMeter.BoostNeedle.Needle, typeof(Image), true) as Image;

			if (speedMeter.BoostNeedle.Needle != newBoostNeedle)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Image");

				speedMeter.BoostNeedle.Needle = newBoostNeedle;

				EditorUtility.SetDirty(Instance);
			}

			if (newBoostNeedle)
			{
				EditorGUI.indentLevel++;

				VehicleUIController.SpeedMeter.Type newBoostType = (VehicleUIController.SpeedMeter.Type)EditorGUILayout.EnumPopup(typeContent, speedMeter.BoostNeedle.type);
				float boostFillMultiplier = newBoostType == VehicleUIController.SpeedMeter.Type.Radial ? 360f : 1f;

				if (speedMeter.BoostNeedle.type != newBoostType)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

					speedMeter.BoostNeedle.type = newBoostType;

					EditorUtility.SetDirty(Instance);
				}

				float newBoostFill = ToolkitEditorUtility.Slider("Fill", speedMeter.BoostNeedle.Fill * boostFillMultiplier, 0f, boostFillMultiplier) / boostFillMultiplier;

				if (speedMeter.BoostNeedle.Fill != newBoostFill)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Fill");

					speedMeter.BoostNeedle.Fill = newBoostFill;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel++;

				bool newInvertRotation = ToolkitEditorUtility.ToggleButtons(new GUIContent("Invert Rotation", "When the Needle is of type `Radial`, you can have the option to invert its rotation direction"), null, "Yes", "No", speedMeter.BoostNeedle.InvertRotation, Instance, "Switch Invert");
				
				if (speedMeter.BoostNeedle.InvertRotation != newInvertRotation)
					speedMeter.BoostNeedle.InvertRotation = newInvertRotation;

				float newBoostFillOffset = ToolkitEditorUtility.Slider("Offset", speedMeter.BoostNeedle.Offset * boostFillMultiplier, 0f, boostFillMultiplier) / boostFillMultiplier;

				if (speedMeter.BoostNeedle.Offset != newBoostFillOffset)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Offset");

					speedMeter.BoostNeedle.Offset = newBoostFillOffset;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel -= 2;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Sprites", EditorStyles.miniBoldLabel);
			EditorGUILayout.BeginVertical(GUI.skin.box);

			switch (speedMeter.ABSSprite.type)
			{
				case VehicleUIController.SpeedMeter.SpriteModule.SpriteType.Image:
					Image newImage = EditorGUILayout.ObjectField(new GUIContent("ABS", "The vehicle ABS sprite image"), speedMeter.ABSSprite.Image, typeof(Image), true) as Image;

					if (speedMeter.ABSSprite.Image != newImage)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Image");

						speedMeter.ABSSprite.Image = newImage;

						EditorUtility.SetDirty(Instance);
					}
					break;

				case VehicleUIController.SpeedMeter.SpriteModule.SpriteType.Text:
					TextMeshProUGUI newText = EditorGUILayout.ObjectField(new GUIContent("ABS", "The vehicle ABS sprite text"), speedMeter.ABSSprite.Text, typeof(TextMeshProUGUI), true) as TextMeshProUGUI;

					if (speedMeter.ABSSprite.Text != newText)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Text");

						speedMeter.ABSSprite.Text = newText;

						EditorUtility.SetDirty(Instance);
					}
					break;
			}

			EditorGUI.indentLevel++;

			VehicleUIController.SpeedMeter.SpriteModule.SpriteType newABSType = (VehicleUIController.SpeedMeter.SpriteModule.SpriteType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The sprite type"), speedMeter.ABSSprite.type);

			if (speedMeter.ABSSprite.type != newABSType)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

				speedMeter.ABSSprite.type = newABSType;

				EditorUtility.SetDirty(Instance);
			}

			if (speedMeter.ABSSprite.Image || speedMeter.ABSSprite.Text)
			{
				Color newNormalColor = EditorGUILayout.ColorField(new GUIContent("Normal Color", "The sprite color at its normal state"), speedMeter.ABSSprite.NormalColor, true, false, false);

				if (speedMeter.ABSSprite.NormalColor != newNormalColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.ABSSprite.NormalColor = newNormalColor;

					EditorUtility.SetDirty(Instance);

				}

				Color newActiveColor = EditorGUILayout.ColorField(new GUIContent("Active Color", "The sprite color at its active state"), speedMeter.ABSSprite.ActiveColor, true, false, false);

				if (speedMeter.ABSSprite.ActiveColor != newActiveColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.ABSSprite.ActiveColor = newActiveColor;

					EditorUtility.SetDirty(Instance);

				}

				Color newDisabledColor = EditorGUILayout.ColorField(new GUIContent("Disabled Color", "The sprite color at its disabled state"), speedMeter.ABSSprite.DisabledColor, true, false, false);

				if (speedMeter.ABSSprite.DisabledColor != newDisabledColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.ABSSprite.DisabledColor = newDisabledColor;

					EditorUtility.SetDirty(Instance);

				}
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			switch (speedMeter.ESPSprite.type)
			{
				case VehicleUIController.SpeedMeter.SpriteModule.SpriteType.Image:
					Image newImage = EditorGUILayout.ObjectField(new GUIContent("ESP", "The vehicle ESP sprite image"), speedMeter.ESPSprite.Image, typeof(Image), true) as Image;

					if (speedMeter.ESPSprite.Image != newImage)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Image");

						speedMeter.ESPSprite.Image = newImage;

						EditorUtility.SetDirty(Instance);
					}
					break;

				case VehicleUIController.SpeedMeter.SpriteModule.SpriteType.Text:
					TextMeshProUGUI newText = EditorGUILayout.ObjectField(new GUIContent("ESP", "The vehicle ESP sprite text"), speedMeter.ESPSprite.Text, typeof(TextMeshProUGUI), true) as TextMeshProUGUI;

					if (speedMeter.ESPSprite.Text != newText)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Text");

						speedMeter.ESPSprite.Text = newText;

						EditorUtility.SetDirty(Instance);
					}
					break;
			}

			EditorGUI.indentLevel++;

			VehicleUIController.SpeedMeter.SpriteModule.SpriteType newESPType = (VehicleUIController.SpeedMeter.SpriteModule.SpriteType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The sprite type"), speedMeter.ESPSprite.type);

			if (speedMeter.ESPSprite.type != newESPType)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

				speedMeter.ESPSprite.type = newESPType;

				EditorUtility.SetDirty(Instance);
			}

			if (speedMeter.ESPSprite.Image || speedMeter.ESPSprite.Text)
			{
				Color newNormalColor = EditorGUILayout.ColorField(new GUIContent("Normal Color", "The sprite color at its normal state"), speedMeter.ESPSprite.NormalColor, true, false, false);

				if (speedMeter.ESPSprite.NormalColor != newNormalColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.ESPSprite.NormalColor = newNormalColor;

					EditorUtility.SetDirty(Instance);

				}

				Color newActiveColor = EditorGUILayout.ColorField(new GUIContent("Active Color", "The sprite color at its active state"), speedMeter.ESPSprite.ActiveColor, true, false, false);

				if (speedMeter.ESPSprite.ActiveColor != newActiveColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.ESPSprite.ActiveColor = newActiveColor;

					EditorUtility.SetDirty(Instance);

				}

				Color newDisabledColor = EditorGUILayout.ColorField(new GUIContent("Disabled Color", "The sprite color at its disabled state"), speedMeter.ESPSprite.DisabledColor, true, false, false);

				if (speedMeter.ESPSprite.DisabledColor != newDisabledColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.ESPSprite.DisabledColor = newDisabledColor;

					EditorUtility.SetDirty(Instance);

				}
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			switch (speedMeter.TCSSprite.type)
			{
				case VehicleUIController.SpeedMeter.SpriteModule.SpriteType.Image:
					Image newImage = EditorGUILayout.ObjectField(new GUIContent("TCS", "The vehicle TCS sprite image"), speedMeter.TCSSprite.Image, typeof(Image), true) as Image;

					if (speedMeter.TCSSprite.Image != newImage)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Image");

						speedMeter.TCSSprite.Image = newImage;

						EditorUtility.SetDirty(Instance);
					}
					break;

				case VehicleUIController.SpeedMeter.SpriteModule.SpriteType.Text:
					TextMeshProUGUI newText = EditorGUILayout.ObjectField(new GUIContent("TCS", "The vehicle TCS sprite text"), speedMeter.TCSSprite.Text, typeof(TextMeshProUGUI), true) as TextMeshProUGUI;

					if (speedMeter.TCSSprite.Text != newText)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Text");

						speedMeter.TCSSprite.Text = newText;

						EditorUtility.SetDirty(Instance);
					}
					break;
			}

			EditorGUI.indentLevel++;

			VehicleUIController.SpeedMeter.SpriteModule.SpriteType newTCSType = (VehicleUIController.SpeedMeter.SpriteModule.SpriteType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The sprite type"), speedMeter.TCSSprite.type);

			if (speedMeter.TCSSprite.type != newTCSType)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

				speedMeter.TCSSprite.type = newTCSType;

				EditorUtility.SetDirty(Instance);
			}

			if (speedMeter.TCSSprite.Image || speedMeter.TCSSprite.Text)
			{
				Color newNormalColor = EditorGUILayout.ColorField(new GUIContent("Normal Color", "The sprite color at its normal state"), speedMeter.TCSSprite.NormalColor, true, false, false);

				if (speedMeter.TCSSprite.NormalColor != newNormalColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.TCSSprite.NormalColor = newNormalColor;

					EditorUtility.SetDirty(Instance);

				}

				Color newActiveColor = EditorGUILayout.ColorField(new GUIContent("Active Color", "The sprite color at its active state"), speedMeter.TCSSprite.ActiveColor, true, false, false);

				if (speedMeter.TCSSprite.ActiveColor != newActiveColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.TCSSprite.ActiveColor = newActiveColor;

					EditorUtility.SetDirty(Instance);

				}

				Color newDisabledColor = EditorGUILayout.ColorField(new GUIContent("Disabled Color", "The sprite color at its disabled state"), speedMeter.TCSSprite.DisabledColor, true, false, false);

				if (speedMeter.TCSSprite.DisabledColor != newDisabledColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.TCSSprite.DisabledColor = newDisabledColor;

					EditorUtility.SetDirty(Instance);

				}
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			switch (speedMeter.HandbrakeSprite.type)
			{
				case VehicleUIController.SpeedMeter.SpriteModule.SpriteType.Image:
					Image newImage = EditorGUILayout.ObjectField(new GUIContent("Handbrake", "The vehicle handbrake sprite image"), speedMeter.HandbrakeSprite.Image, typeof(Image), true) as Image;

					if (speedMeter.HandbrakeSprite.Image != newImage)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Image");

						speedMeter.HandbrakeSprite.Image = newImage;

						EditorUtility.SetDirty(Instance);
					}
					break;

				case VehicleUIController.SpeedMeter.SpriteModule.SpriteType.Text:
					TextMeshProUGUI newText = EditorGUILayout.ObjectField(new GUIContent("Handbrake", "The vehicle handbrake sprite text"), speedMeter.HandbrakeSprite.Text, typeof(TextMeshProUGUI), true) as TextMeshProUGUI;

					if (speedMeter.HandbrakeSprite.Text != newText)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Text");

						speedMeter.HandbrakeSprite.Text = newText;

						EditorUtility.SetDirty(Instance);
					}
					break;
			}

			EditorGUI.indentLevel++;

			VehicleUIController.SpeedMeter.SpriteModule.SpriteType newHandbrakeType = (VehicleUIController.SpeedMeter.SpriteModule.SpriteType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The sprite type"), speedMeter.HandbrakeSprite.type);

			if (speedMeter.HandbrakeSprite.type != newHandbrakeType)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

				speedMeter.HandbrakeSprite.type = newHandbrakeType;

				EditorUtility.SetDirty(Instance);
			}

			if (speedMeter.HandbrakeSprite.Image || speedMeter.HandbrakeSprite.Text)
			{
				Color newNormalColor = EditorGUILayout.ColorField(new GUIContent("Normal Color", "The sprite color at normal state"), speedMeter.HandbrakeSprite.NormalColor, true, false, false);

				if (speedMeter.HandbrakeSprite.NormalColor != newNormalColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.HandbrakeSprite.NormalColor = newNormalColor;

					EditorUtility.SetDirty(Instance);

				}

				Color newActiveColor = EditorGUILayout.ColorField(new GUIContent("Active Color", "The sprite color at normal state"), speedMeter.HandbrakeSprite.ActiveColor, true, false, false);

				if (speedMeter.HandbrakeSprite.ActiveColor != newActiveColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.HandbrakeSprite.ActiveColor = newActiveColor;

					EditorUtility.SetDirty(Instance);

				}
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			switch (speedMeter.LeftSideSignalLightSprite.type)
			{
				case VehicleUIController.SpeedMeter.SpriteModule.SpriteType.Image:
					Image newImage = EditorGUILayout.ObjectField(new GUIContent("Left Side Signal Light", "The vehicle left side signal light sprite image"), speedMeter.LeftSideSignalLightSprite.Image, typeof(Image), true) as Image;

					if (speedMeter.LeftSideSignalLightSprite.Image != newImage)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Image");

						speedMeter.LeftSideSignalLightSprite.Image = newImage;

						EditorUtility.SetDirty(Instance);
					}
					break;

				case VehicleUIController.SpeedMeter.SpriteModule.SpriteType.Text:
					TextMeshProUGUI newText = EditorGUILayout.ObjectField(new GUIContent("Left Side Signal Light", "The vehicle left side signal light sprite text"), speedMeter.LeftSideSignalLightSprite.Text, typeof(TextMeshProUGUI), true) as TextMeshProUGUI;

					if (speedMeter.LeftSideSignalLightSprite.Text != newText)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Text");

						speedMeter.LeftSideSignalLightSprite.Text = newText;

						EditorUtility.SetDirty(Instance);
					}
					break;
			}

			EditorGUI.indentLevel++;

			VehicleUIController.SpeedMeter.SpriteModule.SpriteType newLeftSideSignalLightType = (VehicleUIController.SpeedMeter.SpriteModule.SpriteType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The sprite type"), speedMeter.LeftSideSignalLightSprite.type);

			if (speedMeter.LeftSideSignalLightSprite.type != newLeftSideSignalLightType)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

				speedMeter.LeftSideSignalLightSprite.type = newLeftSideSignalLightType;

				EditorUtility.SetDirty(Instance);
			}

			if (speedMeter.LeftSideSignalLightSprite.Image || speedMeter.LeftSideSignalLightSprite.Text)
			{
				Color newNormalColor = EditorGUILayout.ColorField(new GUIContent("Normal Color", "The sprite color at normal state"), speedMeter.LeftSideSignalLightSprite.NormalColor, true, false, false);

				if (speedMeter.LeftSideSignalLightSprite.NormalColor != newNormalColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.LeftSideSignalLightSprite.NormalColor = newNormalColor;

					EditorUtility.SetDirty(Instance);

				}

				Color newActiveColor = EditorGUILayout.ColorField(new GUIContent("Active Color", "The sprite color at normal state"), speedMeter.LeftSideSignalLightSprite.ActiveColor, true, false, false);

				if (speedMeter.LeftSideSignalLightSprite.ActiveColor != newActiveColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.LeftSideSignalLightSprite.ActiveColor = newActiveColor;

					EditorUtility.SetDirty(Instance);

				}
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			switch (speedMeter.RightSideSignalLightSprite.type)
			{
				case VehicleUIController.SpeedMeter.SpriteModule.SpriteType.Image:
					Image newImage = EditorGUILayout.ObjectField(new GUIContent("Right Side Signal Light", "The vehicle right side signal light sprite image"), speedMeter.RightSideSignalLightSprite.Image, typeof(Image), true) as Image;

					if (speedMeter.RightSideSignalLightSprite.Image != newImage)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Image");

						speedMeter.RightSideSignalLightSprite.Image = newImage;

						EditorUtility.SetDirty(Instance);
					}
					break;

				case VehicleUIController.SpeedMeter.SpriteModule.SpriteType.Text:
					TextMeshProUGUI newText = EditorGUILayout.ObjectField(new GUIContent("Right Side Signal Light", "The vehicle right side signal light sprite text"), speedMeter.RightSideSignalLightSprite.Text, typeof(TextMeshProUGUI), true) as TextMeshProUGUI;

					if (speedMeter.RightSideSignalLightSprite.Text != newText)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Text");

						speedMeter.RightSideSignalLightSprite.Text = newText;

						EditorUtility.SetDirty(Instance);
					}
					break;
			}

			EditorGUI.indentLevel++;

			VehicleUIController.SpeedMeter.SpriteModule.SpriteType newRightSideSignalLightType = (VehicleUIController.SpeedMeter.SpriteModule.SpriteType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The sprite type"), speedMeter.RightSideSignalLightSprite.type);

			if (speedMeter.RightSideSignalLightSprite.type != newRightSideSignalLightType)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

				speedMeter.RightSideSignalLightSprite.type = newRightSideSignalLightType;

				EditorUtility.SetDirty(Instance);
			}

			if (speedMeter.RightSideSignalLightSprite.Image || speedMeter.RightSideSignalLightSprite.Text)
			{
				Color newNormalColor = EditorGUILayout.ColorField(new GUIContent("Normal Color", "The sprite color at normal state"), speedMeter.RightSideSignalLightSprite.NormalColor, true, false, false);

				if (speedMeter.RightSideSignalLightSprite.NormalColor != newNormalColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.RightSideSignalLightSprite.NormalColor = newNormalColor;

					EditorUtility.SetDirty(Instance);

				}

				Color newActiveColor = EditorGUILayout.ColorField(new GUIContent("Active Color", "The sprite color at normal state"), speedMeter.RightSideSignalLightSprite.ActiveColor, true, false, false);

				if (speedMeter.RightSideSignalLightSprite.ActiveColor != newActiveColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					speedMeter.RightSideSignalLightSprite.ActiveColor = newActiveColor;

					EditorUtility.SetDirty(Instance);

				}
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Texts", EditorStyles.miniBoldLabel);
			EditorGUILayout.BeginVertical(GUI.skin.box);

			TextMeshProUGUI newRPMText = EditorGUILayout.ObjectField(new GUIContent("RPM", "The numeric UI representation of the vehicle current engine RPM"), speedMeter.RPMText.Text, typeof(TextMeshProUGUI), true) as TextMeshProUGUI;

			if (speedMeter.RPMText.Text != newRPMText)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Text");

				speedMeter.RPMText.Text = newRPMText;

				EditorUtility.SetDirty(Instance);
			}

			if (speedMeter.RPMText.Text)
			{
				EditorGUI.indentLevel++;

				string newRPMPlaceHolder = EditorGUILayout.TextField("Place Holder", speedMeter.RPMText.placeholder);

				if (speedMeter.RPMText.placeholder != newRPMPlaceHolder)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Place Holder");

					speedMeter.RPMText.placeholder = newRPMPlaceHolder;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			TextMeshProUGUI newSpeedText = EditorGUILayout.ObjectField(new GUIContent("Speed", $"The numeric UI representation of the vehicle current Speed in {Utility.FullUnit(Utility.Units.Speed, Settings.valuesUnit)} ({Utility.Unit(Utility.Units.Speed, Settings.valuesUnit)})"), speedMeter.SpeedText.Text, typeof(TextMeshProUGUI), true) as TextMeshProUGUI;

			if (speedMeter.SpeedText.Text != newSpeedText)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Text");

				speedMeter.SpeedText.Text = newSpeedText;

				EditorUtility.SetDirty(Instance);
			}

			if (speedMeter.SpeedText.Text)
			{
				EditorGUI.indentLevel++;

				string newSpeedPlaceHolder = EditorGUILayout.TextField("Place Holder", speedMeter.SpeedText.placeholder);

				if (speedMeter.SpeedText.placeholder != newSpeedPlaceHolder)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Holder");

					speedMeter.SpeedText.placeholder = newSpeedPlaceHolder;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			TextMeshProUGUI newGearText = EditorGUILayout.ObjectField(new GUIContent("Gear", $"The text UI representation of the vehicle current gear"), speedMeter.GearText.Text, typeof(TextMeshProUGUI), true) as TextMeshProUGUI;

			if (speedMeter.GearText.Text != newGearText)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Text");

				speedMeter.GearText.Text = newGearText;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}
		private void MobilePresetsEditor()
		{
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				mobilePresetFoldout = false;

			GUILayout.Space(5f);
			EditorGUILayout.LabelField("Mobile Presets", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				AddMobilePreset();

			if (Instance.MobilePresets.Length > 1)
				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					RemoveMobilePresets();

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			if (Instance.MobilePresets.Length > 0)
				for (int i = 0; i < Instance.MobilePresets.Length; i++)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);

					if (Instance.MobilePresets[i].group)
					{
						EditorGUILayout.LabelField($"Mobile Preset{(Instance.MobilePresets.Length > 1 ? $" {i + 1}" : "")}", EditorStyles.miniBoldLabel);

						if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
							currentMobilePreset = i;
					}
					else
					{
						RectTransform newGroup = EditorGUILayout.ObjectField("New Mobile Preset", Instance.MobilePresets[i].group, typeof(RectTransform), true) as RectTransform;

						if (newGroup)
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Add Group");

							Instance.MobilePresets[i].group = newGroup;

							EditorUtility.SetDirty(Instance);
						}
					}

					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						RemoveMobilePreset(i);

					EditorGUILayout.EndHorizontal();
				}
			else if (!EditorApplication.isPlaying)
				EditorGUILayout.HelpBox("Click on the '+' button above to add a new UI mobile preset", MessageType.Info);

			EditorGUI.EndDisabledGroup();

			if (Instance.MobilePresets.Length > 0)
			{
				EditorGUILayout.Space();
				EditorGUI.BeginDisabledGroup(Instance.MobilePresets.Length < 2);

				string[] mobilePresets = new string[Instance.MobilePresets.Length];

				for (int i = 0; i < mobilePresets.Length; i++)
					mobilePresets[i] = $"Mobile Preset {i + 1}";

				int newActiveMobilePreset = Mathf.Clamp(EditorGUILayout.Popup(new GUIContent("Active Mobile Preset", "The current active UI mobile preset in use"), Instance.ActiveMobilePreset, mobilePresets), 0, Instance.MobilePresets.Length - 1);

				if (Instance.ActiveMobilePreset != newActiveMobilePreset)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Active Preset");

					Instance.ActiveMobilePreset = newActiveMobilePreset;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.EndDisabledGroup();
			}

			if (!Settings.useMobileInputs)
				EditorGUILayout.HelpBox("Keep in mind that you've disabled Mobile Inputs, therefore using the Mobile Presets features is useless. Even though, you still can add, remove & configure them as you want.", MessageType.Info);
		}
		private void MobilePresetEditor(int index)
		{
			VehicleUIController.MobileInputPreset mobilePreset = Instance.MobilePresets[index];

			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				currentMobilePreset = -1;

			GUILayout.Space(5f);
			EditorGUILayout.LabelField("Mobile Preset Configurations", EditorStyles.boldLabel);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.ObjectField("Group", mobilePreset.group, typeof(RectTransform), true);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			mobilePreset.SteeringWheelLeft = MobileInputEditor(mobilePreset.SteeringWheelLeft, new GUIContent("Steering Wheel Left", "Steering wheel left input"), false);
			mobilePreset.SteeringWheelRight = MobileInputEditor(mobilePreset.SteeringWheelRight, new GUIContent("Steering Wheel Right", "Steering wheel right input"), false);
			mobilePreset.FuelPedal = MobileInputEditor(mobilePreset.FuelPedal, new GUIContent("Fuel Pedal", "Fuel pedal input"), false);
			mobilePreset.BrakePedal = MobileInputEditor(mobilePreset.BrakePedal, new GUIContent("Brake Pedal", "Brake pedal input"), false);
			mobilePreset.ClutchPedal = MobileInputEditor(mobilePreset.ClutchPedal, new GUIContent("Clutch Pedal", "Clutch pedal input"), false);
			mobilePreset.Handbrake = MobileInputEditor(mobilePreset.Handbrake, new GUIContent("Handbrake", "Handbrake input"), false);
			mobilePreset.GearShiftUp = MobileInputEditor(mobilePreset.GearShiftUp, new GUIContent("Gear Shift Up", "Gear shift up input"), true);
			mobilePreset.GearShiftDown = MobileInputEditor(mobilePreset.GearShiftDown, new GUIContent("Gear Shift Down", "Gear shift down input"), true);
			mobilePreset.EngineStartSwitch = MobileInputEditor(mobilePreset.EngineStartSwitch, new GUIContent("Engine Start Switch", "Turns On/Off the engine"), true);
			mobilePreset.NOS = MobileInputEditor(mobilePreset.NOS, new GUIContent("NOS", "The NOS boost input"), true);
			mobilePreset.LaunchControlSwitch = MobileInputEditor(mobilePreset.LaunchControlSwitch, new GUIContent("Launch Control Switch", "Vehicle's launch control switch input"), true);
			//mobilePreset.Horn = MobileInputEditor(mobilePreset.Horn, new GUIContent("Horn", "Horn input"), true);
			mobilePreset.Reset = MobileInputEditor(mobilePreset.Reset, new GUIContent("Reset Vehicle", "Vehicle reset input"), true);
			mobilePreset.ChangeCamera = MobileInputEditor(mobilePreset.ChangeCamera, new GUIContent("Change Camera", "Camera change mode input"), true);
			mobilePreset.LightSwitch = MobileInputEditor(mobilePreset.LightSwitch, new GUIContent("Lights Switch", "Turns On/Off the lights"), true);
			mobilePreset.HighBeamLightSwitch = MobileInputEditor(mobilePreset.HighBeamLightSwitch, new GUIContent("High-beam Lights Switch", "Turns On/Off high-beam lights"), true);
#if !MVC_COMMUNITY
			mobilePreset.InteriorLightSwitch = MobileInputEditor(mobilePreset.InteriorLightSwitch, new GUIContent("Interior Lights Switch", "Turns On/Off interior lights"), true);
#endif
			mobilePreset.LeftSideSignalSwitch = MobileInputEditor(mobilePreset.LeftSideSignalSwitch, new GUIContent("Left Side Signals Switch", "Turns On/Off left side signal (indicator) lights"), true);
			mobilePreset.RightSideSignalSwitch = MobileInputEditor(mobilePreset.RightSideSignalSwitch, new GUIContent("Right Side Signals Switch", "Turns On/Off right side signal (indicator) lights"), true);
			mobilePreset.HazardLightsSwitch = MobileInputEditor(mobilePreset.HazardLightsSwitch, new GUIContent("Hazard Lights Switch", "Turns On/Off left & right side signal (indicator) lights"), true);
			mobilePreset.TrailerLinkSwitch = MobileInputEditor(mobilePreset.TrailerLinkSwitch, new GUIContent("Trailer Link Switch", "Links/un-links the closest trailer to the player's vehicle"), true);

			EditorGUILayout.Space();
		}
		private VehicleUIController.MobileInputPreset.MobileInput MobileInputEditor(VehicleUIController.MobileInputPreset.MobileInput input, GUIContent content, bool lockType)
		{
			if (input && input.IsValid)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(content, EditorStyles.miniBoldLabel);

				if (input.editorFoldout)
				{
					if (GUILayout.Button(EditorUtilities.Icons.Save, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						input.editorFoldout = false;
				}
				else
				{
					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						input.editorFoldout = true;

					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						bool destroySource = input.Type == VehicleUIController.MobileInputPreset.InputType.Touch;

						if (destroySource)
							destroySource = !EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Do you want to destroy the UI button linked to this input?", "Keep", "Destroy");

						var inputSource = input.Source;

						Undo.RegisterCompleteObjectUndo(Instance, "Remove Input");

						input = new(VehicleUIController.MobileInputPreset.InputType.Touch, null);

						if (destroySource)
							Undo.DestroyObjectImmediate(inputSource.gameObject);

						EditorUtility.SetDirty(Instance);
					}
				}

				EditorGUILayout.EndHorizontal();

				if (input.editorFoldout)
				{
					EditorGUILayout.Space();

					EditorGUI.indentLevel++;

					switch (input.Type)
					{
						case VehicleUIController.MobileInputPreset.InputType.Sensor:
							VehicleUIController.MobileInputPreset.SensorType newSensorType = (VehicleUIController.MobileInputPreset.SensorType)EditorGUILayout.EnumPopup(new GUIContent("Sensor", "Input's sensor source type"), input.SensorType);

							if (input.SensorType != newSensorType)
							{
								Undo.RegisterCompleteObjectUndo(Instance, "Change Sensor");

								input.SensorType = newSensorType;

								EditorUtility.SetDirty(Instance);
							}

							if (input.SensorType != VehicleUIController.MobileInputPreset.SensorType.None)
							{
								VehicleUIController.MobileInputPreset.SensorAxis newSensorAxis = (VehicleUIController.MobileInputPreset.SensorAxis)EditorGUILayout.EnumPopup(new GUIContent("Axis", "Input's sensor source axis"), input.SensorAxis);

								if (input.SensorAxis != newSensorAxis)
								{
									Undo.RegisterCompleteObjectUndo(Instance, "Change Axis");

									input.SensorAxis = newSensorAxis;

									EditorUtility.SetDirty(Instance);
								}
							}

							break;

						default:
							EditorGUI.BeginDisabledGroup(true);
							EditorGUILayout.ObjectField(new GUIContent("Source", "The input's joystick source"), input.Source, typeof(Joystick), true);
							EditorGUI.EndDisabledGroup();
							EditorGUILayout.Space();

							if (input.Source && input.Source.type == JoystickType.Handle)
							{
								VehicleUIController.MobileInputPreset.JoystickAxis newHandleAxis = (VehicleUIController.MobileInputPreset.JoystickAxis)EditorGUILayout.EnumPopup(new GUIContent("Axis", "The axis name of the joystick source"), input.JoystickAxis);

								if (input.JoystickAxis != newHandleAxis)
								{
									Undo.RegisterCompleteObjectUndo(Instance, "Change Axis");

									input.JoystickAxis = newHandleAxis;

									EditorUtility.SetDirty(Instance);
								}
							}

							break;
					}

					if (input.Type == VehicleUIController.MobileInputPreset.InputType.Sensor || input.Source.type == JoystickType.Handle)
					{
						VehicleUIController.MobileInputPreset.AxisPolarity newAxisPolarityTarget = (VehicleUIController.MobileInputPreset.AxisPolarity)EditorGUILayout.EnumPopup(new GUIContent("Polarity Target", "The polarity target of the input's source axis value"), input.AxisPolarityTarget);

						if (input.AxisPolarityTarget != newAxisPolarityTarget)
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Change Axis");

							input.AxisPolarityTarget = newAxisPolarityTarget;

							EditorUtility.SetDirty(Instance);
						}
					}

					EditorGUI.indentLevel--;

					EditorGUILayout.Space();
				}

				EditorGUILayout.EndVertical();
			}
			else
			{
				EditorGUILayout.BeginHorizontal(GUI.skin.box);

				float labelWidth = EditorGUIUtility.labelWidth;

				EditorGUIUtility.labelWidth = 80f;

				EditorGUILayout.LabelField(content, EditorStyles.miniBoldLabel);

				EditorGUIUtility.labelWidth = labelWidth;

				EditorGUI.BeginDisabledGroup(lockType);

				VehicleUIController.MobileInputPreset.InputType newInputType = (VehicleUIController.MobileInputPreset.InputType)EditorGUILayout.EnumPopup(VehicleUIController.MobileInputPreset.InputType.Touch);

				EditorGUI.EndDisabledGroup();
				
				Joystick newMobileInputJoystick = EditorGUILayout.ObjectField(null, typeof(Joystick), true) as Joystick;

				if (newMobileInputJoystick || newInputType == VehicleUIController.MobileInputPreset.InputType.Sensor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Add Input");

					input = new(newInputType, newMobileInputJoystick);

					EditorUtility.SetDirty(Instance);
				}

				EditorGUILayout.EndHorizontal();
			}

			return input;
		}

		#endregion

		#endregion

		#endregion
	}
}

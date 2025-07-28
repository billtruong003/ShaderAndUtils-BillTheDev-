#region Namespaces

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Utilities;
using Utilities.Editor;
using MVC.Base;
using MVC.Editor;
using MVC.Internal;
using MVC.Utilities.Editor;
using Unity.Mathematics;

#endregion

namespace MVC.Core.Editor
{
	[CustomEditor(typeof(VehicleChassis))]
	public class VehicleChassisEditor : ToolkitBehaviourEditor
	{
		#region Variables

		#region Static Variables

		public static bool engineFoldout;
		public static bool lightsFoldout;
		public static bool wingsFoldout;
		public static bool collidersFoldout;
		//public static bool partsFoldout;
		public static bool followerPivotsFoldout;

		#endregion

		#region Global Variables

		private VehicleChassis Instance
		{
			get
			{
				if (!instance)
					instance = target as VehicleChassis;

				return instance;
			}
		}
		private VehicleChassis instance;
		private VehicleTrailer TrailerInstance
		{
			get
			{
				if (!trailerInstance && Instance && Instance.VehicleInstance && Instance.VehicleInstance is VehicleTrailer trailer)
					trailerInstance = trailer;

				return trailerInstance;
			}
		}
		private VehicleTrailer trailerInstance;
		private Transform wingEditDuplicate;
		private Transform lightEditDuplicate;
		private bool sortingLights;
		private bool LightPositionEdit
		{
			get
			{
				return lightPositionEdit && currentLight > -1 && Instance && currentLight < Instance.VehicleInstance.Lights.Length;
			}
			set
			{
				if (value)
					LightEmissionEdit = false;

				lightPositionEdit = value;
			}
		}
		private bool lightPositionEdit;
		private bool LightEmissionEdit
		{
			get
			{
				return lightEmissionEdit && currentLight > -1 && Instance && currentLight < Instance.VehicleInstance.Lights.Length;
			}
			set
			{
				if (value)
					LightPositionEdit = false;

				lightEmissionEdit = value;
			}
		}
		private bool lightEmissionEdit;
		private bool WingSpeedPositionEdit
		{
			get
			{
				return wingSpeedPositionEdit && wingEditDuplicate;
			}
			set
			{
				wingSpeedPositionEdit = value;
			}
		}
		private bool wingSpeedPositionEdit;
		private bool WingSpeedPositionAltEdit
		{
			get
			{
				return wingSpeedPositionAltEdit && wingEditDuplicate;
			}
			set
			{
				wingSpeedPositionAltEdit = value;
			}
		}
		private bool wingSpeedPositionAltEdit;
		private bool WingBrakePositionEdit
		{
			get
			{
				return wingBrakePositionEdit && wingEditDuplicate;
			}
			set
			{
				wingBrakePositionEdit = value;
			}
		}
		private bool wingBrakePositionEdit;
		private bool WingBrakePositionAltEdit
		{
			get
			{
				return wingBrakePositionAltEdit && wingEditDuplicate;
			}
			set
			{
				wingBrakePositionAltEdit = value;
			}
		}
		private bool wingBrakePositionAltEdit;
		private bool WingDecelPositionEdit
		{
			get
			{
				return wingDecelPositionEdit && wingEditDuplicate;
			}
			set
			{
				wingDecelPositionEdit = value;
			}
		}
		private bool wingDecelPositionEdit;
		private bool WingDecelPositionAltEdit
		{
			get
			{
				return wingDecelPositionAltEdit && wingEditDuplicate;
			}
			set
			{
				wingDecelPositionAltEdit = value;
			}
		}
		private bool wingDecelPositionAltEdit;
		private bool WingSteerPositionEdit
		{
			get
			{
				return wingSteerPositionEdit && wingEditDuplicate;
			}
			set
			{
				wingSteerPositionEdit = value;
			}
		}
		private bool wingSteerPositionEdit;
		private bool WingSteerPositionAltEdit
		{
			get
			{
				return wingSteerPositionAltEdit && wingEditDuplicate;
			}
			set
			{
				wingSteerPositionAltEdit = value;
			}
		}
		private bool wingSteerPositionAltEdit;
		private int currentLight = -1;
		private int currentWing = -1;

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
			else if (!Instance.VehicleInstance)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("This Vehicle Chassis component is invalid cause it's Vehicle parent cannot not be found or has been disabled!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				EditorGUILayout.Space();

				return;
			}

			#endregion

			Transform lightsParent = Instance.transform.Find("LightSources");
			var hideFlag = Settings.useHideFlags ? HideFlags.HideInHierarchy : HideFlags.None;

			if (lightsParent && lightsParent.hideFlags != hideFlag)
				lightsParent.hideFlags = hideFlag;

			Instance.RefreshFollowerPivots();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUILayout.BeginHorizontal();

			#region Foldout Headers

			if (engineFoldout)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					EnableFoldout();

				GUILayout.Space(5f);
				EditorGUILayout.LabelField("Engine Configurations", EditorStyles.boldLabel);
			}
			else if (lightsFoldout)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					if (currentLight > -1 && currentLight < Instance.VehicleInstance.Lights.Length)
					{
						CancelLightEmissionEdit();
						CancelLightPosition();

						currentLight = -1;
					}
					else if (sortingLights)
						sortingLights = false;
					else
						EnableFoldout();
				}

				GUILayout.Space(5f);

				if (currentLight > -1 && currentLight < Instance.VehicleInstance.Lights.Length)
					EditorGUILayout.LabelField($"{Instance.VehicleInstance.Lights[currentLight].GetName()} Configurations", EditorStyles.boldLabel);
				else if (sortingLights)
					EditorGUILayout.LabelField("Sorting Lights", EditorStyles.boldLabel);
				else
				{
					EditorGUILayout.LabelField("Lights", EditorStyles.boldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						AddLight();

					if (Instance.VehicleInstance.Lights.Length > 1)
					{
						if (GUILayout.Button(new GUIContent(EditorUtilities.Icons.Sort), ToolkitEditorUtility.UnstretchableMiniButtonWide))
							sortingLights = true;

						if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
							if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Are you sure you want to remove all the existing lights at once? This includes removing the existing light sources, removing a light source is an undoable action.", "Yes", "No"))
							{
								RemoveAllLights();

								return;
							}
					}
				}
			}
			else if (wingsFoldout)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					if (currentWing > -1 && currentWing < Instance.wings.Length)
					{
						CancelWingPositionEdit();

						currentWing = -1;
					}
					else
						EnableFoldout();
				}

				GUILayout.Space(5f);

				if (currentWing > -1 && currentWing < Instance.wings.Length)
					EditorGUILayout.LabelField($"{Instance.wings[currentWing].transform.name} Configurations", EditorStyles.boldLabel);
				else
				{
					EditorGUILayout.LabelField("AeroDynamic Wings", EditorStyles.boldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						AddWing();

					if (Instance.wings.Length > 1)
						if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
							if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Are you sure of removing all the existing aerodynamic wings from the selected chassis?", "Yes", "No"))
							{
								RemoveAllWings();

								return;
							}
				}
			}
			else if (collidersFoldout)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					EnableFoldout();

				GUILayout.Space(5f);
				EditorGUILayout.LabelField("Anti-Ground Colliders", EditorStyles.boldLabel);

				if (Instance.ignoredColliders != null && Instance.ignoredColliders.Length > 1)
				{
					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Are you sure you want to remove all the existing anti-ground colliders at once?", "Yes", "No"))
						{
							RemoveAllColliders();

							return;
						}
				}
			}
			/*else if (partsFoldout)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, VehicleEditorUtility.UnstretchableMiniButtonWide))
					EnableFoldout();

				GUILayout.Space(5f);

				EditorGUILayout.LabelField("Chassis Parts", EditorStyles.boldLabel);

				if (Instance.parts.Length > 1)
				{
					if (GUILayout.Button(EditorUtilities.Icons.Trash, VehicleEditorUtility.UnstretchableMiniButtonWide))
						if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Are you sure you want to remove all the existing chassis parts at once?", "Yes", "No"))
						{
							RemoveAllParts();

							return;
						}
				}
			}*/
			else if (followerPivotsFoldout)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					EnableFoldout();

				GUILayout.Space(5f);
				EditorGUILayout.LabelField("Follower Pivots", EditorStyles.boldLabel);

				if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					AddFollowerPivot();

				if (Instance.FollowerPivots.Length > 1)
					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Are you sure of removing all the existing follower pivots from the selected chassis?", "Yes", "No"))
						{
							RemoveAllFollowerPivots();

							return;
						}
			}
			else
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					ToolkitEditorUtility.SelectObject(Instance.VehicleInstance.gameObject);

				GUILayout.Space(5f);
				EditorGUILayout.LabelField($"{(TrailerInstance ? "Trailer" : "Vehicle")} Chassis Configurations", EditorStyles.boldLabel);
			}

			#endregion

			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying && (lightsFoldout || wingsFoldout || collidersFoldout ||/* partsFoldout ||*/ followerPivotsFoldout));

			#region Foldouts

			if (engineFoldout)
			{
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				VehicleEngine.EnginePosition newEnginePosition = (VehicleEngine.EnginePosition)EditorGUILayout.EnumPopup("Position", Instance.EnginePosition);

				if (Instance.EnginePosition != newEnginePosition)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Move Vehicle Engine");

					Instance.EnginePosition = newEnginePosition;

					EditorUtility.SetDirty(Instance);
				}

				float newEnginePositionOffset = ToolkitEditorUtility.Slider("Offset", Instance.EnginePositionOffset, 0f, 1f, Instance, "Move Engine Offset");

				if (Instance.EnginePositionOffset != newEnginePositionOffset)
					Instance.EnginePositionOffset = newEnginePositionOffset;

				EditorGUI.EndDisabledGroup();

				VehicleEngineChassisTorque newEngineChassisTorque = (VehicleEngineChassisTorque)ToolkitEditorUtility.ToggleMultipleButtons(new GUIContent("Torque", "Apply engine torque to the chassis to tilt it"), null, true, (int)Instance.engineChassisTorque, Instance, "Switch Torque", true, "Off", "Auto", "Always On");

				if (Instance.engineChassisTorque != newEngineChassisTorque)
					Instance.engineChassisTorque = newEngineChassisTorque;

				EditorGUI.BeginDisabledGroup(Instance.engineChassisTorque == VehicleEngineChassisTorque.Off);

				float newEngineChassisTorqueMultiplier = ToolkitEditorUtility.Slider(new GUIContent("Multiplier", "Engine chassis torque multiplier"), Instance.EngineChassisTorqueMultiplier, 0f, 15f, Instance, "Change Multiplier");

				if (Instance.EngineChassisTorqueMultiplier != newEngineChassisTorqueMultiplier)
					Instance.EngineChassisTorqueMultiplier = newEngineChassisTorqueMultiplier;

				if (Mathf.Approximately(Instance.EngineChassisTorqueMultiplier, 0f))
					EditorGUILayout.HelpBox("It is unnecessary to keep the `Chassis Engine Torque` enabled when the `Torque Multiplier` is equal or close to zero (0). Consider setting it to `Off` instead.", MessageType.Info);

				EditorGUI.EndDisabledGroup();
			}
			else if (lightsFoldout)
			{
				if (Instance.VehicleInstance.Lights.Length > 0)
					LightsEditor();
				else
					EditorGUILayout.HelpBox("Use the \"+\" button in front of the 'Lights' label to add a new light.", MessageType.Info);
			}
			else if (wingsFoldout)
			{
				if (Instance.wings.Length > 0)
					WingsEditor();
				else
					EditorGUILayout.HelpBox("In case you want to add a new wing, you can press the \"+\" button above.", MessageType.Info);
			}
			else if (collidersFoldout)
			{
				CollidersEditor();

				if (Instance.ignoredColliders.Length < 1)
					EditorGUILayout.HelpBox("In case you want to add a new collider, you can assign the empty field above with a Chassis collider.", MessageType.Info);
			}
			/*else if (partsFoldout)
			{
				PartsEditor();

				if (Instance.parts.Length < 1)
					EditorGUILayout.HelpBox("In case you want to add a new part, you can assign the empty field above with a transform child of the Chassis.", MessageType.Info);
			}*/
			else if (followerPivotsFoldout)
			{
				if (Instance.FollowerPivots.Length > 0)
					FollowerPivotsEditor();
				else
					EditorGUILayout.HelpBox("In case you want to add a new follower pivot, you can press the \"+\" button above.", MessageType.Info);
			}
			else
			{
				if (!TrailerInstance)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField("Engine", EditorStyles.miniBoldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						engineFoldout = EnableFoldout();

					EditorGUILayout.EndHorizontal();
				}

				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				if (Settings.useLights)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField("Lights", EditorStyles.miniBoldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						lightsFoldout = EnableFoldout();

					EditorGUILayout.EndHorizontal();
				}

				if (Settings.useChassisWings && !TrailerInstance)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField("AeroDynamic Wings", EditorStyles.miniBoldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						wingsFoldout = EnableFoldout();

					EditorGUILayout.EndHorizontal();
				}

				if (Settings.useAntiGroundColliders)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField("Anti-Ground Colliders", EditorStyles.miniBoldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						collidersFoldout = EnableFoldout();

					EditorGUILayout.EndHorizontal();
				}

				/*if (Settings.useDamage && !TrailerInstance)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField("Parts", EditorStyles.miniBoldLabel);

					if (!ToolkitSettings.IsProVersion)
						EditorGUILayoutProIcon();
					else if (GUILayout.Button(EditorUtilities.Icons.Pencil, VehicleEditorUtility.UnstretchableMiniButtonWide))
						partsFoldout = EnableFoldout();

					EditorGUILayout.EndHorizontal();
				}*/

				if (!TrailerInstance)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField("Follower Pivots", EditorStyles.miniBoldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						followerPivotsFoldout = EnableFoldout();

					EditorGUILayout.EndHorizontal();
				}

				EditorGUI.EndDisabledGroup();
			}

			#endregion

			EditorGUI.EndDisabledGroup();

			#region Messages

			if (!Instance.VehicleInstance.enabled)
			{
				EditorGUILayout.HelpBox("The vehicle controller of the current chassis has been disabled. Therefore this chassis is not going to be active at runtime!", MessageType.Warning);

				return;
			}

			#endregion

			EditorGUILayout.Space();
		}

		#endregion

		#region Static Methods

		public static bool EnableFoldout()
		{
			engineFoldout = false;
			lightsFoldout = false;
			wingsFoldout = false;
			collidersFoldout = false;
			//partsFoldout = false;
			followerPivotsFoldout = false;

			return true;
		}

		#endregion

		#region Global Methods

		#region Utilities

		private void AddLight()
		{
			Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Add Light");

			List<VehicleLight> lights = Instance.VehicleInstance.Lights.ToList();

			lights.Add(new(Instance.VehicleInstance));

			Instance.VehicleInstance.Lights = lights.ToArray();

			RemoveUnusedLights();
			EditorUtility.SetDirty(Instance);

			if (lightsFoldout)
				currentLight = lights.Count - 1;

			lights.Clear();
		}
		private void DuplicateLight(int index)
		{
			Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Duplicate Light");

			List<VehicleLight> lights = Instance.VehicleInstance.Lights.ToList();

			lights.Add(new(lights[index]));

			Instance.VehicleInstance.Lights = lights.ToArray();

			lights.Clear();
			RemoveUnusedLights();
			EditorUtility.SetDirty(Instance);
		}
		private void MoveLight(int index, int newIndex)
		{
			Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Sort Light");

			List<VehicleLight> lights = Instance.VehicleInstance.Lights.ToList();

			VehicleLight light = lights[index];

			lights.RemoveAt(index);
			lights.Insert(newIndex, light);

			Instance.VehicleInstance.Lights = lights.ToArray();

			RemoveUnusedLights();
			EditorUtility.SetDirty(Instance);
		}
		private void RemoveLight(int index)
		{
			Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Remove Light");

			List<VehicleLight> lights = Instance.VehicleInstance.Lights.ToList();

			lights[index].RemoveLightSource();
			lights.RemoveAt(index);

			Instance.VehicleInstance.Lights = lights.ToArray();

			RemoveUnusedLights();
			EditorUtility.SetDirty(Instance);
		}
		private void RemoveAllLights()
		{
			Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Remove Lights");

			for (int i = 0; i < Instance.VehicleInstance.Lights.Length; i++)
				Instance.VehicleInstance.Lights[i].RemoveLightSource();

			Instance.VehicleInstance.Lights = new VehicleLight[] { };

			RemoveUnusedLights();
			EditorUtility.SetDirty(Instance);
		}
		private void RemoveUnusedLights()
		{
			Transform parent = Instance.transform.Find("LightSources");

			if (!parent)
				return;

			Light[] lights = parent.GetComponentsInChildren<Light>();

			for (int i = 0; i < lights.Length; i++)
			{
				if (!lights[i].GetComponent<VehicleLightSource>())
					Utility.Destroy(true, lights[i].gameObject);
			}
		}
		private void AddWing()
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Add Wing");

			List<VehicleChassis.WingModule> wings = Instance.wings.ToList();

			wings.Add(new(Instance, wings.Count));

			Instance.wings = wings.ToArray();

			EditorUtility.SetDirty(Instance);
		}
		private void RemoveWing(int index)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Remove Wing");

			List<VehicleChassis.WingModule> wings = Instance.wings.ToList();

			wings.RemoveAt(index);

			Instance.wings = wings.ToArray();

			EditorUtility.SetDirty(Instance);
		}
		private void RemoveAllWings()
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Remove Wings");

			Instance.wings = new VehicleChassis.WingModule[] { };

			EditorUtility.SetDirty(Instance);
		}
		private void AddCollider(Collider collider)
		{
			if (!collider.transform.IsChildOf(Instance.transform))
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "The added collider seems to be not a child of the current Vehicle Chassis. Make sure you only add child objects to this list!", "Got it!");

				return;
			}

			List<Collider> colliders = Instance.ignoredColliders.ToList();

			if (colliders.IndexOf(collider) > -1)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "The added collider seems to already exists in the anti-ground colliders list!", "Okay");

				return;
			}
			
			Undo.RegisterCompleteObjectUndo(Instance, "Add Collider");
			colliders.Add(collider);

			Instance.ignoredColliders = colliders.ToArray();

			EditorUtility.SetDirty(Instance);
		}
		private void RemoveCollider(int index)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Remove Collider");

			List<Collider> colliders = Instance.ignoredColliders.ToList();

			colliders.RemoveAt(index);

			Instance.ignoredColliders = colliders.ToArray();

			EditorUtility.SetDirty(Instance);
		}
		private void RemoveAllColliders()
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Remove Colliders");

			Instance.ignoredColliders = new Collider[] { };

			EditorUtility.SetDirty(Instance);
		}
		/*private void AddPart(Transform partTransform)
		{
			if (!partTransform.IsChildOf(Instance.transform))
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "The added part transform seems to be not a child of the current Vehicle Chassis. Make sure you only add child objects to this list!", "Got it!");

				return;
			}

			List<VehicleChassisPart> parts = Instance.parts.ToList();
			VehicleChassisPart part = partTransform.GetComponent<VehicleChassisPart>();

			if (!part)
			{
				part = Undo.AddComponent<VehicleChassisPart>(partTransform.gameObject);
				
				EditorUtility.SetDirty(Instance.gameObject);
			}

			if (parts.IndexOf(part) > -1)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "The added part transform seems to already exists in the chassis parts list!", "Okay");

				return;
			}

			Undo.RegisterCompleteObjectUndo(Instance, "Add Part");
			parts.Add(part);

			Instance.parts = parts.ToArray();

			EditorUtility.SetDirty(Instance);
		}
		private void EditPart(int index)
		{
			VehicleEditorUtility.SelectObject(Instance.parts[index].gameObject);
		}
		private void RemovePart(int index)
		{
			List<VehicleChassisPart> parts = Instance.parts.ToList();

			if (parts[index])
			{
				Undo.DestroyObjectImmediate(parts[index]);
				EditorUtility.SetDirty(Instance.gameObject);
			}

			Undo.RegisterCompleteObjectUndo(Instance, "Remove Part");
			parts.RemoveAt(index);

			Instance.parts = parts.ToArray();
		
			EditorUtility.SetDirty(Instance);
		}
		private void RemoveAllParts()
		{
			for (int i = 0; i < Instance.parts.Length; i++)
			{
				Undo.DestroyObjectImmediate(Instance.parts[i]);
				EditorUtility.SetDirty(Instance.gameObject);
			}

			Undo.RegisterCompleteObjectUndo(Instance, "Remove Parts");

			Instance.parts = new VehicleChassisPart[] { };

			EditorUtility.SetDirty(Instance);
		}*/
		private void AddFollowerPivot()
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Add Pivot");

			VehicleFollowerPivot pivot = Undo.AddComponent<VehicleFollowerPivot>(new("Follower Pivot"));

			pivot.transform.SetParent(Instance.transform, false);
			Instance.RefreshFollowerPivots();

			pivot.transform.localPosition = Vector3.up;

			EditorUtility.SetDirty(Instance.gameObject);
			EditFollowerPivot(Instance.FollowerPivots.Length - 1);
		}
		private void EditFollowerPivot(int index)
		{
			ToolkitEditorUtility.SelectObject(Instance.FollowerPivots[index].gameObject);
		}
		private void RemoveFollowerPivot(int index)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Remove Pivot");
			Undo.DestroyObjectImmediate(Instance.FollowerPivots[index].gameObject);
			Instance.RefreshFollowerPivots();
			EditorUtility.SetDirty(Instance.gameObject);
		}
		private void RemoveAllFollowerPivots()
		{
			Transform parent = Instance.transform.Find("FollowerPivots");

			if (!parent)
				return;

			Undo.DestroyObjectImmediate(parent.gameObject);
			EditorUtility.SetDirty(Instance.gameObject);
		}

		#endregion

		#region Editor

		#region Utilities

		private void EditLightPosition(int index)
		{
			VehicleLight light = Instance.VehicleInstance.Lights[index];

			light.UpdateParent();

			Vector3 position = light.DynamicLocalPosition != Vector3.zero ? light.DynamicLocalPosition : light.DynamicTransform.localPosition;
			Quaternion rotation = light.DynamicLocalRotation != Quaternion.identity ? light.DynamicLocalRotation : light.DynamicTransform.localRotation;

			lightEditDuplicate = Instantiate(light.DynamicTransform.gameObject, light.DynamicTransform.parent).transform;
			lightEditDuplicate.gameObject.hideFlags = HideFlags.HideInHierarchy;

			lightEditDuplicate.SetLocalPositionAndRotation(position, rotation);
			light.DynamicTransform.gameObject.SetActive(false);

			LightPositionEdit = true;
		}
		private void SaveLightPosition(int index)
		{
			VehicleLight light = Instance.VehicleInstance.Lights[index];

			light.DynamicLocalPosition = lightEditDuplicate.localPosition;
			light.DynamicLocalRotation = lightEditDuplicate.localRotation;

			DestroyImmediate(lightEditDuplicate.gameObject);
			light.DynamicTransform.gameObject.SetActive(true);

			LightPositionEdit = false;

			EditorUtility.SetDirty(Instance.VehicleInstance);
		}
		private void CancelLightPosition()
		{
			if (!LightPositionEdit)
				return;

			VehicleLight light = Instance.VehicleInstance.Lights[currentLight];

			DestroyImmediate(lightEditDuplicate.gameObject);
			light.DynamicTransform.gameObject.SetActive(true);

			LightPositionEdit = false;
		}
		private void EditLightEmission(int index)
		{
			Instance.VehicleInstance.Lights[index].renderer.sharedMaterials[Instance.VehicleInstance.Lights[index].materialIndex].SetColor(Instance.VehicleInstance.Lights[index].GetMaterialEmissionColorPropertyName(), Instance.VehicleInstance.Lights[index].emissionColor);

			LightEmissionEdit = true;
		}
		private void SaveLightEmission(int index)
		{
			Instance.VehicleInstance.Lights[index].renderer.sharedMaterials[Instance.VehicleInstance.Lights[index].materialIndex].SetColor(Instance.VehicleInstance.Lights[index].GetMaterialEmissionColorPropertyName(), Color.black);
			
			LightEmissionEdit = false;
		}
		private void EditLightSource(int index)
		{
			Instance.VehicleInstance.Lights[index].GetLightSource().Instance.enabled = true;

			ToolkitEditorUtility.SelectObject(Instance.VehicleInstance.Lights[index].GetLightSource().gameObject);
		}
		private void CancelLightEmissionEdit()
		{
			if (!LightEmissionEdit)
				return;

			SaveLightEmission(currentLight);
		}
		private void EditWingSpeedPosition(VehicleChassis.WingModule wing)
		{
			wing.UpdateParent(instance);

			Vector3 position;
			Quaternion rotation;

			if (wing.speed.localPosition != Vector3.zero)
				position = wing.speed.localPosition;
			else
				position = wing.transform.localPosition;

			if (wing.speed.localRotation != Vector3.zero)
				rotation = Quaternion.Euler(wing.speed.localRotation);
			else
				rotation = wing.transform.localRotation;

			wingEditDuplicate = Instantiate(wing.transform.gameObject, wing.transform.parent).transform;
			wingEditDuplicate.gameObject.hideFlags = HideFlags.HideInHierarchy;

			wingEditDuplicate.SetLocalPositionAndRotation(position, rotation);
			wing.transform.gameObject.SetActive(false);

			WingSpeedPositionEdit = true;
		}
		private void EditWingSpeedPositionAlt(VehicleChassis.WingModule wing)
		{
			wing.UpdateParent(instance);

			Vector3 position;
			Quaternion rotation;

			if (wing.speed.localPositionAlt != Vector3.zero)
				position = wing.speed.localPositionAlt;
			else
				position = wing.transform.localPosition;

			if (wing.speed.localRotationAlt != Vector3.zero)
				rotation = Quaternion.Euler(wing.speed.localRotationAlt);
			else
				rotation = wing.transform.localRotation;

			wingEditDuplicate = Instantiate(wing.transform.gameObject, wing.transform.parent).transform;
			wingEditDuplicate.gameObject.hideFlags = HideFlags.HideInHierarchy;

			wingEditDuplicate.SetLocalPositionAndRotation(position, rotation);
			wing.transform.gameObject.SetActive(false);

			WingSpeedPositionAltEdit = true;
		}
		private void EditWingBrakePosition(VehicleChassis.WingModule wing)
		{
			wing.UpdateParent(instance);

			Vector3 position;
			Quaternion rotation;

			if (wing.brake.localPosition != Vector3.zero)
				position = wing.brake.localPosition;
			else
				position = wing.transform.localPosition;

			if (wing.brake.localRotation != Vector3.zero)
				rotation = Quaternion.Euler(wing.brake.localRotation);
			else
				rotation = wing.transform.localRotation;

			wingEditDuplicate = Instantiate(wing.transform.gameObject, wing.transform.parent).transform;
			wingEditDuplicate.gameObject.hideFlags = HideFlags.HideInHierarchy;

			wingEditDuplicate.SetLocalPositionAndRotation(position, rotation);
			wing.transform.gameObject.SetActive(false);

			WingBrakePositionEdit = true;
		}
		private void EditWingBrakePositionAlt(VehicleChassis.WingModule wing)
		{
			wing.UpdateParent(instance);

			Vector3 position;
			Quaternion rotation;

			if (wing.brake.localPositionAlt != Vector3.zero)
				position = wing.brake.localPositionAlt;
			else
				position = wing.transform.localPosition;

			if (wing.brake.localRotationAlt != Vector3.zero)
				rotation = Quaternion.Euler(wing.brake.localRotationAlt);
			else
				rotation = wing.transform.localRotation;

			wingEditDuplicate = Instantiate(wing.transform.gameObject, wing.transform.parent).transform;
			wingEditDuplicate.gameObject.hideFlags = HideFlags.HideInHierarchy;

			wingEditDuplicate.SetLocalPositionAndRotation(position, rotation);
			wing.transform.gameObject.SetActive(false);

			WingBrakePositionAltEdit = true;
		}
		private void EditWingDecelPosition(VehicleChassis.WingModule wing)
		{
			wing.UpdateParent(instance);

			Vector3 position;
			Quaternion rotation;

			if (wing.decel.localPosition != Vector3.zero)
				position = wing.decel.localPosition;
			else
				position = wing.transform.localPosition;

			if (wing.decel.localRotation != Vector3.zero)
				rotation = Quaternion.Euler(wing.decel.localRotation);
			else
				rotation = wing.transform.localRotation;

			wingEditDuplicate = Instantiate(wing.transform.gameObject, wing.transform.parent).transform;
			wingEditDuplicate.gameObject.hideFlags = HideFlags.HideInHierarchy;

			wingEditDuplicate.SetLocalPositionAndRotation(position, rotation);
			wing.transform.gameObject.SetActive(false);

			WingDecelPositionEdit = true;
		}
		private void EditWingDecelPositionAlt(VehicleChassis.WingModule wing)
		{
			wing.UpdateParent(instance);

			Vector3 position;
			Quaternion rotation;

			if (wing.decel.localPositionAlt != Vector3.zero)
				position = wing.decel.localPositionAlt;
			else
				position = wing.transform.localPosition;

			if (wing.decel.localRotationAlt != Vector3.zero)
				rotation = Quaternion.Euler(wing.decel.localRotationAlt);
			else
				rotation = wing.transform.localRotation;

			wingEditDuplicate = Instantiate(wing.transform.gameObject, wing.transform.parent).transform;
			wingEditDuplicate.gameObject.hideFlags = HideFlags.HideInHierarchy;

			wingEditDuplicate.SetLocalPositionAndRotation(position, rotation);
			wing.transform.gameObject.SetActive(false);

			WingDecelPositionAltEdit = true;
		}
		private void EditWingSteerPosition(VehicleChassis.WingModule wing)
		{
			wing.UpdateParent(instance);

			Vector3 position;
			Quaternion rotation;

			if (wing.steer.localPosition != Vector3.zero)
				position = wing.steer.localPosition;
			else
				position = wing.transform.localPosition;

			if (wing.steer.localRotation != Vector3.zero)
				rotation = Quaternion.Euler(wing.steer.localRotation);
			else
				rotation = wing.transform.localRotation;

			wingEditDuplicate = Instantiate(wing.transform.gameObject, wing.transform.parent).transform;
			wingEditDuplicate.gameObject.hideFlags = HideFlags.HideInHierarchy;

			wingEditDuplicate.SetLocalPositionAndRotation(position, rotation);
			wing.transform.gameObject.SetActive(false);

			WingSteerPositionEdit = true;
		}
		private void EditWingSteerPositionAlt(VehicleChassis.WingModule wing)
		{
			wing.UpdateParent(instance);

			Vector3 position;
			Quaternion rotation;

			if (wing.steer.localPositionAlt != Vector3.zero)
				position = wing.steer.localPositionAlt;
			else
				position = wing.transform.localPosition;

			if (wing.steer.localRotationAlt != Vector3.zero)
				rotation = Quaternion.Euler(wing.steer.localRotationAlt);
			else
				rotation = wing.transform.localRotation;

			wingEditDuplicate = Instantiate(wing.transform.gameObject, wing.transform.parent).transform;
			wingEditDuplicate.gameObject.hideFlags = HideFlags.HideInHierarchy;

			wingEditDuplicate.SetLocalPositionAndRotation(position, rotation);
			wing.transform.gameObject.SetActive(false);

			WingSteerPositionAltEdit = true;
		}
		private void SaveWingSpeedPosition(VehicleChassis.WingModule wing)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Change Position");

			wing.speed.localPosition = wingEditDuplicate.localPosition;
			wing.speed.localRotation = wingEditDuplicate.localEulerAngles;

			EditorUtility.SetDirty(Instance);
			DestroyImmediate(wingEditDuplicate.gameObject);
			wing.transform.gameObject.SetActive(true);

			WingSpeedPositionEdit = false;
		}
		private void SaveWingSpeedPositionAlt(VehicleChassis.WingModule wing)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Change Position");

			wing.speed.localPositionAlt = wingEditDuplicate.localPosition;
			wing.speed.localRotationAlt = wingEditDuplicate.localEulerAngles;

			EditorUtility.SetDirty(Instance);
			DestroyImmediate(wingEditDuplicate.gameObject);
			wing.transform.gameObject.SetActive(true);

			WingSpeedPositionAltEdit = false;
		}
		private void SaveWingBrakePosition(VehicleChassis.WingModule wing)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Change Position");

			wing.brake.localPosition = wingEditDuplicate.localPosition;
			wing.brake.localRotation = wingEditDuplicate.localEulerAngles;

			EditorUtility.SetDirty(Instance);
			DestroyImmediate(wingEditDuplicate.gameObject);
			wing.transform.gameObject.SetActive(true);

			WingBrakePositionEdit = false;
		}
		private void SaveWingBrakePositionAlt(VehicleChassis.WingModule wing)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Change Position");

			wing.brake.localPositionAlt = wingEditDuplicate.localPosition;
			wing.brake.localRotationAlt = wingEditDuplicate.localEulerAngles;

			EditorUtility.SetDirty(Instance);
			DestroyImmediate(wingEditDuplicate.gameObject);
			wing.transform.gameObject.SetActive(true);

			WingBrakePositionAltEdit = false;
		}
		private void SaveWingDecelPosition(VehicleChassis.WingModule wing)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Change Position");

			wing.decel.localPosition = wingEditDuplicate.localPosition;
			wing.decel.localRotation = wingEditDuplicate.localEulerAngles;

			EditorUtility.SetDirty(Instance);
			DestroyImmediate(wingEditDuplicate.gameObject);
			wing.transform.gameObject.SetActive(true);

			WingDecelPositionEdit = false;
		}
		private void SaveWingDecelPositionAlt(VehicleChassis.WingModule wing)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Change Position");

			wing.decel.localPositionAlt = wingEditDuplicate.localPosition;
			wing.decel.localRotationAlt = wingEditDuplicate.localEulerAngles;

			EditorUtility.SetDirty(Instance);
			DestroyImmediate(wingEditDuplicate.gameObject);
			wing.transform.gameObject.SetActive(true);

			WingDecelPositionAltEdit = false;
		}
		private void SaveWingSteerPosition(VehicleChassis.WingModule wing)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Change Position");

			wing.steer.localPosition = wingEditDuplicate.localPosition;
			wing.steer.localRotation = wingEditDuplicate.localEulerAngles;

			EditorUtility.SetDirty(Instance);
			DestroyImmediate(wingEditDuplicate.gameObject);
			wing.transform.gameObject.SetActive(true);

			WingSteerPositionEdit = false;
		}
		private void SaveWingSteerPositionAlt(VehicleChassis.WingModule wing)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Change Position");

			wing.steer.localPositionAlt = wingEditDuplicate.localPosition;
			wing.steer.localRotationAlt = wingEditDuplicate.localEulerAngles;

			EditorUtility.SetDirty(Instance);
			DestroyImmediate(wingEditDuplicate.gameObject);
			wing.transform.gameObject.SetActive(true);

			WingSteerPositionAltEdit = false;
		}
		private void CancelWingPositionEdit()
		{
			if (wingEditDuplicate)
				DestroyImmediate(wingEditDuplicate.gameObject);
			
			if (currentWing > -1 && currentWing < Instance.wings.Length && Instance.wings[currentWing])
				Instance.wings[currentWing].transform.gameObject.SetActive(true);

			WingSpeedPositionEdit = false;
			WingSpeedPositionAltEdit = false;
			WingBrakePositionEdit = false;
			WingBrakePositionAltEdit = false;
			WingDecelPositionEdit = false;
			WingDecelPositionAltEdit = false;
			WingSteerPositionEdit = false;
			WingSteerPositionAltEdit = false;
		}

		#endregion

		#region GUI

		private void LightsEditor()
		{
			if (sortingLights)
				EditorGUILayout.HelpBox("Sorting lights may affect lights execution order, also, lower lights (at the bottom of the list) execute later and may override some higher lights (on top of the list) that have common mesh renderers, i.e. the lights on the top have a bigger priority than the ones at the bottom.", MessageType.Info);

			for (int i = 0; i < Instance.VehicleInstance.Lights.Length; i++)
			{
				if (currentLight > -1 && i != currentLight)
					continue;

				VehicleLight light = Instance.VehicleInstance.Lights[i];

				if (currentLight < 0)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);

					if (sortingLights)
					{
						EditorGUI.BeginDisabledGroup(i == 0);

						if (GUILayout.Button(new GUIContent(EditorUtilities.Icons.CaretUp), ToolkitEditorUtility.UnstretchableMiniButtonLeft))
						{
							MoveLight(i, i - 1);

							return;
						}

						EditorGUI.EndDisabledGroup();
						EditorGUI.BeginDisabledGroup(i == Instance.VehicleInstance.Lights.Length - 1);

						if (GUILayout.Button(new GUIContent(EditorUtilities.Icons.CaretDown), ToolkitEditorUtility.UnstretchableMiniButtonRight))
						{
							MoveLight(i, i + 1);

							return;
						}

						EditorGUI.EndDisabledGroup();
						GUILayout.Space(5f);
						EditorGUILayout.LabelField(light.GetName(), EditorStyles.miniBoldLabel);
					}
					else
					{
						float orgLabelWidth = EditorGUIUtility.labelWidth;

						EditorGUIUtility.labelWidth = EditorGUIUtility.currentViewWidth - (EditorGUI.indentLevel + 10.25f) * 22f;

						EditorGUILayout.LabelField(light.GetName(), EditorStyles.miniBoldLabel);

						EditorGUIUtility.labelWidth = orgLabelWidth;

						if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
							currentLight = i;

						if (GUILayout.Button(EditorUtilities.Icons.Clone, ToolkitEditorUtility.UnstretchableMiniButtonWide))
							DuplicateLight(i);

						if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
							if (!light.GetLightSource() || EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "The light you are trying to remove has a light source attached to it, removing the light source is not a recoverable action.", "Continue", "Cancel"))
								RemoveLight(i);
					}

					EditorGUILayout.EndHorizontal();

					continue;
				}

				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("Properties", EditorStyles.miniBoldLabel);

				EditorGUI.indentLevel++;

				VehicleLight.Type newType = (VehicleLight.Type)EditorGUILayout.EnumPopup(new GUIContent("Type", "Light type"), light.type);

				if (light.type != newType)
				{
					Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Light Type");

					light.type = newType;

					EditorUtility.SetDirty(Instance.VehicleInstance);
				}

				int newBehaviour = light.behaviour;

				if (light.IsHeadlight)
					newBehaviour = (int)(VehicleLight.HeadlightBehaviour)EditorGUILayout.EnumPopup(new GUIContent("Behaviour", "Indicates how the light should behave"), (VehicleLight.HeadlightBehaviour)newBehaviour);
				else if (light.IsRearLight)
					newBehaviour = (int)(VehicleLight.HeadlightBehaviour)EditorGUILayout.EnumPopup(new GUIContent("Behaviour", "Indicates how the light should behave"), (VehicleLight.RearLightBehaviour)newBehaviour);

				if (light.behaviour != newBehaviour)
				{
					Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Light Behaviour");

					light.behaviour = newBehaviour;

					EditorUtility.SetDirty(Instance.VehicleInstance);
				}

				if (light.IsIndicatorHeadlight || light.IsIndicatorRearLight || light.IsSideSignalLight)
				{
					VehicleLight.Side newSide = (VehicleLight.Side)EditorGUILayout.EnumPopup(new GUIContent("Side", "Light side, could be left or right"), light.side);

					if (light.side != newSide)
					{
						Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Light Side");

						light.side = newSide;

						EditorUtility.SetDirty(Instance.VehicleInstance);
					}
				}

				VehicleLight.Technology newTechnology = (VehicleLight.Technology)EditorGUILayout.EnumPopup(new GUIContent("Technology", "Light technology type"), light.technologyType);

				if (light.technologyType != newTechnology)
				{
					Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Light Technology");

					light.technologyType = newTechnology;

					EditorUtility.SetDirty(Instance.VehicleInstance);
				}

				VehicleLight.EmissionType newEmissionType = (VehicleLight.EmissionType)EditorGUILayout.EnumPopup(new GUIContent("Emission", "Indicates the emission switch method"), light.emission);

				if (light.emission != newEmissionType)
				{
					Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Light Emission");

					light.emission = newEmissionType;

					EditorUtility.SetDirty(Instance.VehicleInstance);
				}

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
				EditorGUILayout.EndVertical();
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("Mechanisms", EditorStyles.miniBoldLabel);

				EditorGUI.indentLevel++;

				EditorGUI.BeginDisabledGroup(LightPositionEdit);

				VehicleLight.Position newPositionType = (VehicleLight.Position)EditorGUILayout.EnumPopup(new GUIContent("Type", "This field indicates whether the light position is of type Static or Dynamic"), light.positionType);

				if (light.positionType != newPositionType)
				{
					Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Position Type");

					light.positionType = newPositionType;

					EditorUtility.SetDirty(Instance.VehicleInstance);
				}

				EditorGUI.EndDisabledGroup();

				if (light.positionType == VehicleLight.Position.Dynamic)
				{
					EditorGUI.BeginDisabledGroup(LightPositionEdit);

					Transform newDynamicTransform = EditorGUILayout.ObjectField(new GUIContent("Transform", "The light parent's transform"), light.DynamicTransform, typeof(Transform), true) as Transform;

					if (light.DynamicTransform != newDynamicTransform)
					{
						Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Light Transform");

						light.DynamicTransform = newDynamicTransform;

						EditorUtility.SetDirty(Instance.VehicleInstance);
					}

					EditorGUI.EndDisabledGroup();

					if (light.DynamicTransform)
					{
						VehicleLight lightWithCommonDynamicTransform = Array.Find<VehicleLight>(Instance.VehicleInstance.Lights, l => l.positionType == VehicleLight.Position.Dynamic && l.DynamicTransform == light.DynamicTransform);
						int indexOfCommonLight = lightWithCommonDynamicTransform ? Array.IndexOf<VehicleLight>(Instance.VehicleInstance.Lights, lightWithCommonDynamicTransform) : -1;
						VehicleLight.Interpolation newPositionInterpolationType;
						float newDynamicInterpolationTime;

						if (indexOfCommonLight < 0 || indexOfCommonLight >= i)
						{
							newPositionInterpolationType = (VehicleLight.Interpolation)EditorGUILayout.EnumPopup(new GUIContent("Interpolation", "The light movement interpolation type"), light.positionInterpolationType);

							EditorGUI.indentLevel++;

							newDynamicInterpolationTime = ToolkitEditorUtility.NumberField(new GUIContent(newPositionInterpolationType == VehicleLight.Interpolation.Logarithmic ? "Damping" : "Time", newPositionInterpolationType == VehicleLight.Interpolation.Logarithmic ? "The speed of the light position & rotation interpolation" : "The time it takes the transform to reach the active target/original position and rotation"), light.DynamicInterpolationTime * 1000f, Utility.Units.TimeAccurate) * .001f;

							EditorGUI.indentLevel--;

							EditorGUILayout.BeginHorizontal();
							EditorGUILayout.LabelField("Position", EditorStyles.boldLabel);

							if (LightPositionEdit && lightEditDuplicate)
							{
								if (GUILayout.Button(EditorUtilities.Icons.Cross, ToolkitEditorUtility.UnstretchableMiniButtonWide))
									CancelLightPosition();

								if (GUILayout.Button(EditorUtilities.Icons.Save, ToolkitEditorUtility.UnstretchableMiniButtonWide))
									SaveLightPosition(i);
							}
							else if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
									EditLightPosition(i);

							EditorGUILayout.EndHorizontal();

							if (LightPositionEdit && lightEditDuplicate)
							{
								Vector3 newLightPosition = EditorGUILayout.Vector3Field(new GUIContent(EditorGUIUtility.IconContent("MoveTool")) { tooltip = "The light transform position in local space" }, lightEditDuplicate.localPosition);
								
								if (lightEditDuplicate.localPosition != newLightPosition)
								{
									Undo.RegisterCompleteObjectUndo(lightEditDuplicate, "Move Light");

									lightEditDuplicate.localPosition = newLightPosition;

									EditorUtility.SetDirty(Instance.gameObject);
								}

								Quaternion newLightRotation = Quaternion.Euler(EditorGUILayout.Vector3Field(new GUIContent(EditorGUIUtility.IconContent("RotateTool")) { tooltip = "The light transform rotation in local space" }, lightEditDuplicate.localRotation.eulerAngles));
								
								if (lightEditDuplicate.localRotation != newLightRotation)
								{
									Undo.RegisterCompleteObjectUndo(lightEditDuplicate, "Rotate Light");

									lightEditDuplicate.localRotation = newLightRotation;

									EditorUtility.SetDirty(Instance.gameObject);
								}
							}
						}
						else
						{
							EditorGUILayout.HelpBox($"The light position properties are being controlled by another light named \"{lightWithCommonDynamicTransform.GetName()}\" since they use the same transform.", MessageType.Info);

							newPositionInterpolationType = lightWithCommonDynamicTransform.positionInterpolationType;
							newDynamicInterpolationTime = lightWithCommonDynamicTransform.DynamicInterpolationTime;

							Vector3 newDynamicLocalPosition = lightWithCommonDynamicTransform.DynamicLocalPosition;

							if (light.DynamicLocalPosition != newDynamicLocalPosition)
							{
								if (indexOfCommonLight < 0)
									Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Position");

								light.DynamicLocalPosition = newDynamicLocalPosition;

								EditorUtility.SetDirty(Instance.VehicleInstance);
							}

							Quaternion newDynamicLocalRotation = lightWithCommonDynamicTransform.DynamicLocalRotation;

							if (light.DynamicLocalRotation != newDynamicLocalRotation)
							{
								if (indexOfCommonLight < 0)
									Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Rotation");

								light.DynamicLocalRotation = newDynamicLocalRotation;

								EditorUtility.SetDirty(Instance.VehicleInstance);
							}
						}

						if (light.positionInterpolationType != newPositionInterpolationType)
						{
							if (indexOfCommonLight < 0)
								Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Interpolation");

							light.positionInterpolationType = newPositionInterpolationType;

							EditorUtility.SetDirty(Instance.VehicleInstance);
						}

						if (light.DynamicInterpolationTime != newDynamicInterpolationTime)
						{
							if (indexOfCommonLight < 0)
								Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Movement Time");

							light.DynamicInterpolationTime = newDynamicInterpolationTime;

							EditorUtility.SetDirty(Instance.VehicleInstance);
						}
					}
				}

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
				EditorGUILayout.EndVertical();

				if (light.emission != VehicleLight.EmissionType.None)
				{
					EditorGUILayout.BeginVertical(GUI.skin.box);
					EditorGUILayout.LabelField("Emission", EditorStyles.miniBoldLabel);

					EditorGUI.indentLevel++;

					EditorGUI.BeginDisabledGroup(LightEmissionEdit);

					MeshRenderer newRenderer = EditorGUILayout.ObjectField(new GUIContent("Mesh", $"The{(light.emission == VehicleLight.EmissionType.SwitchGameObjects ? " original" : "")} light mesh renderer"), light.renderer, typeof(MeshRenderer), true) as MeshRenderer;

					if (light.renderer != newRenderer)
					{
						Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Light Mesh");

						light.renderer = newRenderer;

						EditorUtility.SetDirty(Instance.VehicleInstance);
					}

					if (light.positionType == VehicleLight.Position.Dynamic && light.emission == VehicleLight.EmissionType.SwitchGameObjects && light.renderer && light.renderer.transform == light.DynamicTransform)
						EditorGUILayout.HelpBox("It seems that the current renderer is the same as Dynamic Light Transform while using the `Switch GameObjects` emission type. This can cause visual problems at Play Mode that are un-realistic!", MessageType.Warning);

					EditorGUI.EndDisabledGroup();

					if (light.emission == VehicleLight.EmissionType.SwitchGameObjects)
					{
						MeshRenderer newEmissiveRenderer = EditorGUILayout.ObjectField(new GUIContent("Emissive Mesh", "The emissive light mesh renderer"), light.emissiveRenderer, typeof(MeshRenderer), true) as MeshRenderer;

						if (light.emissiveRenderer != newEmissiveRenderer)
						{
							Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Emissive Light Mesh");

							light.emissiveRenderer = newEmissiveRenderer;

							EditorUtility.SetDirty(Instance.VehicleInstance);
						}
					}
					else if (light.renderer && light.renderer.sharedMaterials.Length > 0)
					{
						if (light.renderer.sharedMaterials.ToList().IndexOf(null) > -1)
							EditorGUILayout.HelpBox("The selected mesh renderer has some empty material fields. Please assign them or selected a valid mesh renderer.", MessageType.Warning);
						else if (light.renderer.transform.IsChildOf(Instance.transform))
						{
							EditorGUI.BeginDisabledGroup(LightEmissionEdit);

							string[] materialNames = new string[light.renderer.sharedMaterials.Length];

							for (int j = 0; j < materialNames.Length; j++)
								materialNames[j] = $"{j + 1}. {light.renderer.sharedMaterials[j].name}";

							int newMaterial = EditorGUILayout.Popup(new GUIContent("Material", "The renderer emissive material"), Mathf.Clamp(light.materialIndex, 0, light.renderer.sharedMaterials.Length - 1), materialNames);

							if (light.materialIndex != newMaterial)
							{
								Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Light Mesh Material");

								light.materialIndex = newMaterial;

								EditorUtility.SetDirty(Instance.VehicleInstance);
							}

							EditorGUI.EndDisabledGroup();
							EditorGUILayout.BeginHorizontal();
							EditorGUI.BeginDisabledGroup(!LightEmissionEdit);

							Color newEmissionColor = EditorGUILayout.ColorField(new GUIContent("Color", "The renderer emission color when the light is turned on"), light.emissionColor, false, false, true);

							if (light.emissionColor != newEmissionColor)
							{
								Undo.RegisterCompleteObjectUndo(Instance.VehicleInstance, "Change Light Emission Color");

								light.emissionColor = newEmissionColor;

								EditorUtility.SetDirty(Instance.VehicleInstance);
							}

							EditorGUI.EndDisabledGroup();

							if (LightEmissionEdit)
							{
								light.RefreshMaterialEmissionColorPropertyName();
								light.renderer.sharedMaterials[light.materialIndex].SetColor(light.GetMaterialEmissionColorPropertyName(), light.emissionColor);

								if (GUILayout.Button(EditorUtilities.Icons.Save, ToolkitEditorUtility.UnstretchableMiniButtonWide))
									SaveLightEmission(i);
							}
							else
							{
								if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
									EditLightEmission(i);
							}

							EditorGUILayout.EndHorizontal();
						}
						else
							EditorGUILayout.HelpBox("The selected mesh renderer is not a child of the vehicle chassis. Please select another valid renderer.", MessageType.Error);
					}
					else if (light.renderer)
						EditorGUILayout.HelpBox("The selected mesh renderer has an empty materials array. Please select another renderer or assign a new material to it.", MessageType.Warning);

					if (light.emission == VehicleLight.EmissionType.SwitchGameObjects && light.technologyType == VehicleLight.Technology.Lamp)
						EditorGUILayout.HelpBox("A normal vehicle lamp has a bit of a late response as you switch the light on and off (ie. some fade in and out smoothing). Unfortunately, that realistic effect won't happen because the `Emission` value is set to \"Switch Game Objects\" instead of \"Color\".", MessageType.Info);

					EditorGUI.indentLevel--;

					EditorGUILayout.Space();
					EditorGUILayout.EndVertical();
				}

				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("Lighting", EditorStyles.miniBoldLabel);

				EditorGUI.indentLevel++;

				VehicleLightSource lightSource = light.GetLightSource();

				EditorGUILayout.BeginHorizontal();
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(new GUIContent("Source", "The light source"), lightSource, typeof(VehicleLightSource), true);
				EditorGUI.EndDisabledGroup();

				if (lightSource)
				{
					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						EditLightSource(i);

					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Are you sure you want to remove this light source? This action is not recoverable.", "I'm sure!", "Cancel"))
						{
							light.RemoveLightSource();

							return;
						}

					if (lightSource.name != light.GetLightSourceName())
						lightSource.name = light.GetLightSourceName();
				}
				else if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					light.AddLightSource();

				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space();
			}
		}
		private void WingsEditor()
		{
			if (currentWing > -1 && currentWing < Instance.wings.Length)
			{
				VehicleChassis.WingModule wing = Instance.wings[currentWing];

				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("Properties", EditorStyles.miniBoldLabel);
				EditorGUI.BeginDisabledGroup(wing.transform);

				EditorGUI.indentLevel++;

				Transform newWingTransform = EditorGUILayout.ObjectField(new GUIContent("Transform", "The wing transform"), wing.transform, typeof(Transform), true) as Transform;

				if (wing.transform != newWingTransform)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Transform");

					wing.transform = newWingTransform;
					wing.speed.localPosition = wing.transform.localPosition;
					wing.speed.localRotation = wing.transform.localEulerAngles;
					wing.speed.localPositionAlt = wing.transform.localPosition;
					wing.speed.localRotationAlt = wing.transform.localEulerAngles;
					wing.steer.localPosition = wing.transform.localPosition;
					wing.steer.localRotation = wing.transform.localEulerAngles;
					wing.steer.localPositionAlt = wing.transform.localPosition;
					wing.steer.localRotationAlt = wing.transform.localEulerAngles;
					wing.brake.localPosition = wing.transform.localPosition;
					wing.brake.localRotation = wing.transform.localEulerAngles;
					wing.brake.localPositionAlt = wing.transform.localPosition;
					wing.brake.localRotationAlt = wing.transform.localEulerAngles;
					wing.decel.localPosition = wing.transform.localPosition;
					wing.decel.localRotation = wing.transform.localEulerAngles;
					wing.decel.localPositionAlt = wing.transform.localPosition;
					wing.decel.localRotationAlt = wing.transform.localEulerAngles;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.EndDisabledGroup();

				if (wing.transform)
				{
					VehicleChassisWingBehaviour newWingBehaviour = (VehicleChassisWingBehaviour)EditorGUILayout.EnumFlagsField(new GUIContent("Translation", "This indicates the wing translation behaviour type"), wing.behaviour);

					if (wing.behaviour != newWingBehaviour)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Translation");

						wing.behaviour = newWingBehaviour;

						EditorUtility.SetDirty(Instance);
					}

					if (Settings.useDownforce && wing.behaviour == 0)
					{
						float newFixedDownforce = ToolkitEditorUtility.NumberField(new GUIContent("Downforce", "The wing force applied to the vehicle at higher speeds"), wing.fixedDownforce, Utility.Units.Force, true, Instance, "Change Downforce");

						if (wing.fixedDownforce != newFixedDownforce)
							wing.fixedDownforce = newFixedDownforce;
					}

					if (Settings.useDrag && wing.behaviour == 0)
					{
						float newFixedDrag = ToolkitEditorUtility.NumberField(new GUIContent("Drag", $"The wing drag applied to the vehicle"), wing.fixedDrag, false, Instance, "Change Drag");

						if (wing.fixedDrag != newFixedDrag)
							wing.fixedDrag = newFixedDrag;
					}

					EditorGUI.indentLevel--;

					EditorGUILayout.Space();
					EditorGUILayout.EndVertical();

					if (wing.behaviour != 0)
					{
						if (wing.IsSpeedWing)
							WingBehaviourEditor(ref wing, VehicleChassisWingBehaviour.Speed);

						if (wing.IsBrakeWing)
							WingBehaviourEditor(ref wing, VehicleChassisWingBehaviour.Brake);

						if (wing.IsDecelWing)
							WingBehaviourEditor(ref wing, VehicleChassisWingBehaviour.Deceleration);

						if (wing.IsSteerWing)
							WingBehaviourEditor(ref wing, VehicleChassisWingBehaviour.Steer);
					}
				}
				else
				{
					EditorGUI.indentLevel--;

					EditorGUILayout.Space();
					EditorGUILayout.EndVertical();
				}

				Instance.wings[currentWing] = wing;
			}
			else
				for (int i = 0; i < Instance.wings.Length; i++)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);

					if (Instance.wings[i].IsValid)
					{
						EditorGUILayout.LabelField(Instance.wings[i].transform.name, EditorStyles.miniBoldLabel);

						if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
							currentWing = i;
					}
					else
					{
						Transform newWingTransform = EditorGUILayout.ObjectField($"Wing {i + 1}", Instance.wings[i].transform, typeof(Transform), true) as Transform;

						if (Instance.wings[i].transform != newWingTransform)
						{
							if (!newWingTransform.IsChildOf(Instance.transform))
								EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "The added wing seems to be not a child of the current Vehicle Chassis. Make sure you only add child objects to this list!", "Got it!");
							else if (Instance.wings.Where(wing => wing.transform == newWingTransform).Count() > 0)
								EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "The added wing seems to already exists in the list!", "Okay");
							else
							{
								Undo.RegisterCompleteObjectUndo(Instance, "Change Wing Transform");

								Instance.wings[i].transform = newWingTransform;

								EditorUtility.SetDirty(Instance);
							}
						}

						Instance.wings[i].UpdateParent(instance);
					}

					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						RemoveWing(i);

					EditorGUILayout.EndHorizontal();
				}
		}
		private void WingBehaviourEditor(ref VehicleChassis.WingModule wing, VehicleChassisWingBehaviour behaviourType)
		{
			VehicleChassis.WingModule.BehaviourModule behaviour = null;

			switch (behaviourType)
			{
				case VehicleChassisWingBehaviour.Speed:
					behaviour = wing.speed;

					break;

				case VehicleChassisWingBehaviour.Brake:
					behaviour = wing.brake;

					break;

				case VehicleChassisWingBehaviour.Deceleration:
					behaviour = wing.decel;

					break;

				case VehicleChassisWingBehaviour.Steer:
					behaviour = wing.steer;

					break;
			}

			if (!behaviour)
				return;

			bool editingPosition = behaviourType == VehicleChassisWingBehaviour.Speed && WingSpeedPositionEdit || behaviourType == VehicleChassisWingBehaviour.Brake && WingBrakePositionEdit || behaviourType == VehicleChassisWingBehaviour.Deceleration && WingDecelPositionEdit || behaviourType == VehicleChassisWingBehaviour.Steer && WingSteerPositionEdit;
			bool editingPositionAlt = !editingPosition && (behaviourType == VehicleChassisWingBehaviour.Speed && WingSpeedPositionAltEdit || behaviourType == VehicleChassisWingBehaviour.Brake && WingBrakePositionAltEdit || behaviourType == VehicleChassisWingBehaviour.Deceleration && WingDecelPositionAltEdit || behaviourType == VehicleChassisWingBehaviour.Steer && WingSteerPositionAltEdit);

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField($"{(behaviourType == VehicleChassisWingBehaviour.Speed ? "Speed" : behaviourType == VehicleChassisWingBehaviour.Brake ? "Brake" : behaviourType == VehicleChassisWingBehaviour.Deceleration ? "Deceleration" : behaviourType == VehicleChassisWingBehaviour.Steer ? "Steer" : "NULL")} Settings", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			EditorGUI.BeginDisabledGroup(behaviourType != VehicleChassisWingBehaviour.Speed && (WingSpeedPositionEdit || WingSpeedPositionAltEdit) || behaviourType != VehicleChassisWingBehaviour.Brake && (WingBrakePositionEdit || WingBrakePositionAltEdit) || behaviourType != VehicleChassisWingBehaviour.Deceleration && (WingDecelPositionEdit || WingDecelPositionAltEdit) || behaviourType != VehicleChassisWingBehaviour.Steer && (WingSteerPositionEdit || WingSteerPositionAltEdit));

			if (!editingPositionAlt)
			{
				EditorGUILayout.BeginHorizontal();

				if (behaviourType == VehicleChassisWingBehaviour.Steer)
					EditorGUILayout.LabelField(new GUIContent("Position (Left)", "The wing's position once the vehicle is steering left"));
				else
					EditorGUILayout.LabelField(new GUIContent("Position", "The wing's position once the vehicle speed enters the speed range"));

				if (editingPosition)
					if (GUILayout.Button(EditorUtilities.Icons.Cross, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						CancelWingPositionEdit();

				if (GUILayout.Button(editingPosition ? EditorUtilities.Icons.Save : EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					if (behaviourType == VehicleChassisWingBehaviour.Speed)
					{
						if (WingSpeedPositionEdit)
							SaveWingSpeedPosition(wing);
						else
							EditWingSpeedPosition(wing);
					}
					else if (behaviourType == VehicleChassisWingBehaviour.Brake)
					{
						if (WingBrakePositionEdit)
							SaveWingBrakePosition(wing);
						else
							EditWingBrakePosition(wing);
					}
					else if (behaviourType == VehicleChassisWingBehaviour.Deceleration)
					{
						if (WingDecelPositionEdit)
							SaveWingDecelPosition(wing);
						else
							EditWingDecelPosition(wing);
					}
					else if (behaviourType == VehicleChassisWingBehaviour.Steer)
					{
						if (WingSteerPositionEdit)
							SaveWingSteerPosition(wing);
						else
							EditWingSteerPosition(wing);
					}
				}

				EditorGUILayout.EndHorizontal();
			}

			if (behaviour.speedRange.Max != Mathf.Infinity || behaviourType == VehicleChassisWingBehaviour.Steer)
			{
				if (!editingPosition)
				{
					EditorGUILayout.BeginHorizontal();

					if (behaviourType == VehicleChassisWingBehaviour.Steer)
						EditorGUILayout.LabelField(new GUIContent("Position (Right)", "The wing's position once the vehicle is steering right"));
					else
						EditorGUILayout.LabelField(new GUIContent("Position (Alt.)", "The wing's position once the vehicle speed exceeds the speed range"));

					if (editingPositionAlt)
						if (GUILayout.Button(EditorUtilities.Icons.Cross, ToolkitEditorUtility.UnstretchableMiniButtonWide))
							CancelWingPositionEdit();

					if (GUILayout.Button(editingPositionAlt ? EditorUtilities.Icons.Save : EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						if (behaviourType == VehicleChassisWingBehaviour.Speed)
						{
							if (WingSpeedPositionAltEdit)
								SaveWingSpeedPositionAlt(wing);
							else
								EditWingSpeedPositionAlt(wing);
						}
						else if (behaviourType == VehicleChassisWingBehaviour.Brake)
						{
							if (WingBrakePositionAltEdit)
								SaveWingBrakePositionAlt(wing);
							else
								EditWingBrakePositionAlt(wing);
						}
						else if (behaviourType == VehicleChassisWingBehaviour.Deceleration)
						{
							if (WingDecelPositionAltEdit)
								SaveWingDecelPositionAlt(wing);
							else
								EditWingDecelPositionAlt(wing);
						}
						else if (behaviourType == VehicleChassisWingBehaviour.Steer)
						{
							if (WingSteerPositionAltEdit)
								SaveWingSteerPositionAlt(wing);
							else
								EditWingSteerPositionAlt(wing);
						}
					}

					EditorGUILayout.EndHorizontal();
				}

				EditorGUILayout.Space();
			}

			EditorGUI.indentLevel++;

			if ((editingPosition || editingPositionAlt) && wingEditDuplicate)
			{
				Vector3 newWingPosition = EditorGUILayout.Vector3Field(new GUIContent(EditorGUIUtility.IconContent("MoveTool")) { tooltip = "The wing position in local space" }, wingEditDuplicate.localPosition);

				if (wingEditDuplicate.localPosition != newWingPosition)
				{
					Undo.RegisterCompleteObjectUndo(wingEditDuplicate, "Move Wing");

					wingEditDuplicate.localPosition = newWingPosition;

					EditorUtility.SetDirty(Instance.gameObject);
				}

				Vector3 newWingRotation = EditorGUILayout.Vector3Field(new GUIContent(EditorGUIUtility.IconContent("RotateTool")) { tooltip = "The wing euler angles in local space" }, wingEditDuplicate.localEulerAngles);

				if (wingEditDuplicate.localEulerAngles != newWingRotation)
				{
					Undo.RegisterCompleteObjectUndo(wingEditDuplicate, "Rotate Wing");

					wingEditDuplicate.localEulerAngles = newWingRotation;

					EditorUtility.SetDirty(Instance.gameObject);
				}

				Repaint();
				EditorGUILayout.Space();
			}

			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup(WingSpeedPositionEdit || WingSpeedPositionAltEdit || WingBrakePositionEdit || WingBrakePositionAltEdit || WingDecelPositionEdit || WingDecelPositionAltEdit || WingSteerPositionEdit || WingSteerPositionAltEdit);

			if (Settings.useDownforce)
			{
				float newWingDownforce = ToolkitEditorUtility.NumberField(new GUIContent(behaviourType == VehicleChassisWingBehaviour.Steer ? "Downforce (Left)" : "Downforce", $"The wing force applied to the vehicle when the wing is raised{(behaviourType == VehicleChassisWingBehaviour.Steer ? " and the vehicle is steering left" : "")}"), behaviour.downforce, Utility.Units.Force, true, Instance, "Change Downforce");

				if (behaviour.downforce != newWingDownforce)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Downforce");

					behaviour.downforce = newWingDownforce;

					EditorUtility.SetDirty(Instance);
				}

				if (behaviour.speedRange.Max != Mathf.Infinity || behaviourType == VehicleChassisWingBehaviour.Steer)
				{
					float newWingDownforceAlt = ToolkitEditorUtility.NumberField(new GUIContent(behaviourType == VehicleChassisWingBehaviour.Steer ? "Downforce (Right)" : "Downforce (Alt.)", $"The wing force applied to the vehicle when the wing is raised and {(behaviourType == VehicleChassisWingBehaviour.Steer ? "the vehicle is steering right" : "once the vehicle's speed passes the range maximum value")}"), behaviour.downforceAlt, Utility.Units.Force, true, Instance, "Change Downforce");

					if (behaviour.downforceAlt != newWingDownforceAlt)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Downforce");

						behaviour.downforceAlt = newWingDownforceAlt;

						EditorUtility.SetDirty(Instance);
					}

					EditorGUILayout.Space();
				}
			}

			if (Settings.useDrag)
			{
				float newWingDrag = ToolkitEditorUtility.NumberField(new GUIContent(behaviourType == VehicleChassisWingBehaviour.Steer ? "Drag (Left)" : "Drag", $"The wing drag applied to the vehicle when the wing is raised{(behaviourType == VehicleChassisWingBehaviour.Steer ? " and the vehicle is steering left" : "")}"), behaviour.drag, false, Instance, "Change Drag");

				if (behaviour.drag != newWingDrag)
					behaviour.drag = newWingDrag;

				if (behaviour.speedRange.Max != Mathf.Infinity || behaviourType == VehicleChassisWingBehaviour.Steer)
				{
					float newWingDragAlt = ToolkitEditorUtility.NumberField(new GUIContent(behaviourType == VehicleChassisWingBehaviour.Steer ? "Drag (Right)" : "Drag (Alt.)", $"The wing drag applied to the vehicle when the wing is raised and {(behaviourType == VehicleChassisWingBehaviour.Steer ? "the vehicle is steering right" : "once the vehicle's speed passes the range maximum value")}"), behaviour.dragAlt, false, Instance, "Change Drag");

					if (behaviour.dragAlt != newWingDragAlt)
						behaviour.dragAlt = newWingDragAlt;

					EditorGUILayout.Space();
				}
			}

			VehicleChassis.WingModule.Interpolation newWingInterpolation = (VehicleChassis.WingModule.Interpolation)EditorGUILayout.EnumPopup(new GUIContent("Interpolation", "This specifies how the wing moves in relation of time"), behaviour.interpolation);

			if (behaviour.interpolation != newWingInterpolation)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Wing Interpolation");

				behaviour.interpolation = newWingInterpolation;

				EditorUtility.SetDirty(Instance);
			}

			if (behaviour.interpolation == VehicleChassis.WingModule.Interpolation.Logarithmic)
			{
				float newWingInterpolationTime = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Damping", "The interpolation intensity multiplier"), behaviour.interpolationTime, false, Instance, "Change Damping"), .1f);

				if (behaviour.interpolationTime != newWingInterpolationTime)
					behaviour.interpolationTime = newWingInterpolationTime;

				if (behaviour.speedRange.Max != Mathf.Infinity && behaviourType != VehicleChassisWingBehaviour.Steer)
				{
					float newWingInterpolationTimeAlt = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Damping (Alt.)", "The interpolation intensity multiplier once the vehicle's speed is greater than the maximum speed range"), behaviour.interpolationTimeAlt, false, Instance, "Change Damping"), .1f);

					if (behaviour.interpolationTimeAlt != newWingInterpolationTimeAlt)
						behaviour.interpolationTimeAlt = newWingInterpolationTimeAlt;

					EditorGUILayout.Space();
				}
			}
			else
			{
				float newWingInterpolationTime = ToolkitEditorUtility.NumberField(new GUIContent("Time", "How much time it takes to reach the target position"), behaviour.interpolationTime * 1000f, Utility.Units.TimeAccurate, true, Instance, "Change Time") * .001f;

				if (behaviour.interpolationTime != newWingInterpolationTime)
					behaviour.interpolationTime = newWingInterpolationTime;

				if (behaviour.speedRange.Max != Mathf.Infinity && behaviourType != VehicleChassisWingBehaviour.Steer)
				{
					float newWingInterpolationTimeAlt = ToolkitEditorUtility.NumberField(new GUIContent("Time(Alt.)", "How much time it takes to reach the target position once the vehicle's speed is greater than the maximum speed range"), behaviour.interpolationTimeAlt * 1000f, Utility.Units.TimeAccurate, true, Instance, "Change Time") * .001f;

					if (behaviour.interpolationTimeAlt != newWingInterpolationTimeAlt)
						behaviour.interpolationTimeAlt = newWingInterpolationTimeAlt;
				}

				if (behaviour.interpolationTime == 0f || behaviour.speedRange.Max != Mathf.Infinity && behaviour.interpolationTimeAlt == 0f && behaviourType != VehicleChassisWingBehaviour.Steer)
					EditorGUILayout.HelpBox("The interpolation time seem to be null. Therefore, the wing will raise instantly without any smoothness and this is not a realistic behaviour.", MessageType.Info);

				if (behaviour.speedRange.Max != Mathf.Infinity && behaviourType != VehicleChassisWingBehaviour.Steer)
					EditorGUILayout.Space();
			}

			if (behaviourType == VehicleChassisWingBehaviour.Brake || behaviourType == VehicleChassisWingBehaviour.Steer)
			{
				float newActivePoint = ToolkitEditorUtility.Slider(new GUIContent("Active Point"), behaviour.activationThreshold, 0f, 1f, Instance, "Change Point");

				if (behaviour.activationThreshold != newActivePoint)
					behaviour.activationThreshold = newActivePoint;
			}

			string actionName = behaviourType == VehicleChassisWingBehaviour.Speed ? "accelerating" : behaviourType == VehicleChassisWingBehaviour.Brake ? "braking" : behaviourType == VehicleChassisWingBehaviour.Deceleration ? "decelerating" : behaviourType == VehicleChassisWingBehaviour.Steer ? "steering" : "NULL";
			Utility.Interval newWingSpeedRange = ToolkitEditorUtility.IntervalField(new GUIContent("Speed Range", "This indicates the speed range of the wing activity"), null, new GUIContent("Min", $"The speed where the wing gets lifted at while {actionName}"), new GUIContent("Max", $"The speed where the wing gets lowered at after being lifted while {actionName}"), Utility.Units.Speed, true, behaviour.speedRange, Instance, "Change Speed Range");

			if (behaviour.speedRange != newWingSpeedRange)
				behaviour.speedRange = newWingSpeedRange;

			if (behaviour.speedRange.Max == behaviour.speedRange.Min)
				EditorGUILayout.HelpBox("The maximum speed value seem to match the minimum speed, this may lead for the wing to never raise.", MessageType.Warning);
			else if (behaviour.speedRange.Min == 0f)
				EditorGUILayout.HelpBox("The minimum speed value seem to be null. Therefore the wing will be always lifted unless the vehicle current speed is greater than the speed range maximum value.", MessageType.Info);

			EditorGUI.EndDisabledGroup();

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
		}
		private void CollidersEditor()
		{
			if (Instance.ignoredColliders == null)
				Instance.ignoredColliders = new Collider[] { };

			for (int i = 0; i <= Instance.ignoredColliders.Length; i++)
			{
				EditorGUILayout.BeginHorizontal(GUI.skin.box);

				if (i < Instance.ignoredColliders.Length)
				{
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ObjectField($"Collider {i + 1}", Instance.ignoredColliders[i], typeof(Transform), true);
					EditorGUI.EndDisabledGroup();
					
					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						RemoveCollider(i);
				}
				else
				{
					Collider newCollider = EditorGUILayout.ObjectField($"Collider {i + 1}", null, typeof(Collider), true) as Collider;

					if (newCollider)
						AddCollider(newCollider);
				}

				EditorGUILayout.EndHorizontal();
			}
		}
		/*private void PartsEditor()
		{
			for (int i = 0; i <= Instance.parts.Length; i++)
			{
				EditorGUILayout.BeginHorizontal(GUI.skin.box);

				if (i < Instance.parts.Length)
				{
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ObjectField($"Part {i + 1}", Instance.parts[i], typeof(VehicleChassisPart), true);
					EditorGUI.EndDisabledGroup();
					EditorGUI.BeginDisabledGroup(!Instance.parts[i]);

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, VehicleEditorUtility.UnstretchableMiniButtonWide))
						EditPart(i);

					EditorGUI.EndDisabledGroup();

					if (GUILayout.Button(EditorUtilities.Icons.Trash, VehicleEditorUtility.UnstretchableMiniButtonWide))
						RemovePart(i);
				}
				else
				{
					Transform newTransform = EditorGUILayout.ObjectField($"Part {i + 1}", null, typeof(Transform), true) as Transform;

					if (newTransform)
						AddPart(newTransform);
				}

				EditorGUILayout.EndHorizontal();
			}
		}*/
		private void FollowerPivotsEditor()
		{
			for (int i = 0; i < Instance.FollowerPivots.Length; i++)
			{
				if (!Instance.FollowerPivots[i])
					continue;

				EditorGUILayout.BeginHorizontal(GUI.skin.box);
				EditorGUILayout.LabelField(Instance.FollowerPivots[i].name, EditorStyles.miniBoldLabel);

				if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					EditFollowerPivot(i);

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					RemoveFollowerPivot(i);

				EditorGUILayout.EndHorizontal();
			}
		}

		#endregion

		#endregion

		#region Destroy, Enable, Disable & GUI

		private void OnSceneGUI()
		{
			if (!Instance.VehicleInstance)
				return;

			if (engineFoldout)
			{
				Vector3 chassisCenter = Instance.VehicleInstance.ChassisBounds.center;
				Vector3 enginePositionBound = chassisCenter + (Instance.EnginePosition == VehicleEngine.EnginePosition.Front ? Vector3.forward * Instance.VehicleInstance.ChassisBounds.extents.z : -Vector3.forward * Instance.VehicleInstance.ChassisBounds.extents.z);
				Vector3 engineCenterPosition = Mathf.Lerp(enginePositionBound.z, chassisCenter.z, Instance.EnginePositionOffset) * Vector3.forward + Utility.Average(enginePositionBound.y, chassisCenter.y) * Vector3.up;
				Color orgColor = Handles.color;

				Handles.color = Settings.engineGizmoColor;

				Vector3 handlePosition = Handles.FreeMoveHandle(Instance.VehicleInstance.transform.TransformPoint(engineCenterPosition),
#if !UNITY_2022_2_OR_NEWER
					Instance.transform.rotation,
#endif
					Settings.gizmosSize / 8f, Vector3.zero, Handles.SphereHandleCap);
				float newPositionOffset = Mathf.InverseLerp(enginePositionBound.z, chassisCenter.z, Instance.VehicleInstance.transform.InverseTransformPoint(handlePosition).z);

				if (Instance.EnginePositionOffset != newPositionOffset)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Move Engine Offset");

					Instance.EnginePositionOffset = newPositionOffset;

					EditorUtility.SetDirty(Instance);
				}

				Handles.color = orgColor;
			}
			else if (lightsFoldout)
			{
				if (lightPositionEdit && lightEditDuplicate)
				{
					if (Tools.current == Tool.Move)
					{
						if (!Tools.hidden)
							Tools.hidden = true;

						Vector3 newLightPosition = Handles.PositionHandle(lightEditDuplicate.position, lightEditDuplicate.rotation);

						if (lightEditDuplicate.position != newLightPosition)
						{
							Undo.RegisterCompleteObjectUndo(lightEditDuplicate, "Move Light");

							lightEditDuplicate.position = newLightPosition;

							EditorUtility.SetDirty(Instance.gameObject);
						}
					}
					else if (Tools.current == Tool.Rotate)
					{
						if (!Tools.hidden)
							Tools.hidden = true;

						Quaternion newLightRotation = Handles.RotationHandle(lightEditDuplicate.rotation, lightEditDuplicate.position);

						if (lightEditDuplicate.rotation != newLightRotation)
						{
							Undo.RegisterCompleteObjectUndo(lightEditDuplicate, "Rotate Light");

							lightEditDuplicate.rotation = newLightRotation;

							EditorUtility.SetDirty(Instance.gameObject);
						}
					}
					else if (Tools.hidden)
						Tools.hidden = false;
				}
				else if (Tools.hidden)
					Tools.hidden = false;
			}
			else if (wingsFoldout)
			{
				if (WingSpeedPositionEdit || WingSpeedPositionAltEdit || WingBrakePositionEdit || WingBrakePositionAltEdit || WingDecelPositionEdit || WingDecelPositionAltEdit || WingSteerPositionEdit || WingSteerPositionAltEdit)
				{
					if (Tools.current == Tool.Move)
					{
						if (!Tools.hidden)
							Tools.hidden = true;

						Vector3 newWingPosition = Handles.PositionHandle(wingEditDuplicate.position, wingEditDuplicate.rotation);

						if (wingEditDuplicate.position != newWingPosition)
						{
							Undo.RegisterCompleteObjectUndo(wingEditDuplicate, "Move Wing");

							wingEditDuplicate.position = newWingPosition;

							EditorUtility.SetDirty(Instance.gameObject);
						}
					}
					else if (Tools.current == Tool.Rotate)
					{
						if (!Tools.hidden)
							Tools.hidden = true;

						Quaternion newWingRotation = Handles.RotationHandle(wingEditDuplicate.rotation, wingEditDuplicate.position);

						if (wingEditDuplicate.rotation != newWingRotation)
						{
							Undo.RegisterCompleteObjectUndo(wingEditDuplicate, "Rotate Wing");

							wingEditDuplicate.rotation = newWingRotation;

							EditorUtility.SetDirty(Instance.gameObject);
						}
					}
					else if (Tools.hidden)
						Tools.hidden = false;
				}
				else if (Tools.hidden)
					Tools.hidden = false;
			}
			else if (Tools.hidden)
				Tools.hidden = false;
		}
		private void OnDestroy()
		{
			if (Tools.hidden)
				Tools.hidden = false;

			CancelLightEmissionEdit();
			CancelWingPositionEdit();

			currentLight = -1;
			currentWing = -1;
			WingSpeedPositionEdit = false;
			WingBrakePositionEdit = false;
			WingDecelPositionEdit = false;
			WingSteerPositionEdit = false;
			engineFoldout = false;
			lightsFoldout = false;
			wingsFoldout = false;
			collidersFoldout = false;
		}
		private void OnDisable()
		{
			OnDestroy();
		}

		#endregion

		#endregion

		#endregion
	}
}

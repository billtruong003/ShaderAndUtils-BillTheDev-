#region Namespaces

using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEditor;
using Utilities;
using Utilities.Editor;
using MVC.Base;
using MVC.AI;
using MVC.AI.Editor;
using MVC.IK;
using MVC.Editor;
using MVC.Base.Editor;
using MVC.Internal;
using MVC.Utilities;
using MVC.Utilities.Internal;
using MVC.Utilities.Editor;
using System.Reflection;
using UnityEngine.EventSystems;
using UnityEditor.Build;


#endregion

namespace MVC.Core.Editor
{
	[CustomEditor(typeof(Vehicle))]
	public class VehicleEditor : ToolkitBehaviourEditor
	{
		#region Modules

		private struct ExhaustParticleSystem
		{
			public ParticleSystem particleSystem;
			public ParticleSystem[] particleSystems;
		}

		#endregion

		#region Variables

		#region Static Variables

		public static bool trailerJointFoldout;
		public static bool componentsFoldout;
		public static bool behaviourFoldout;
		public static bool transmissionFoldout;
		public static bool brakesFoldout;
		public static bool frontBrakesFoldout;
		public static bool rearBrakesFoldout;
		public static bool steeringFoldout;
		public static bool suspensionsFoldout;
		public static bool frontSuspensionFoldout;
		public static bool rearSuspensionFoldout;
		public static bool tiresFoldout;
		public static bool stabilityFoldout;
		public static bool aiFoldout;
		//public static bool damageFoldout;
		public static bool interiorFoldout;
		public static bool audioFoldout;
		public static bool driverIKFoldout;
		public static bool IKPivotsFoldout;
		public static bool trailerLinkFoldout;
		public static bool trailerFollowerModifierFoldout;

		internal static Texture2D WarningIcon => EditorUtilities.Icons.Warning;
		internal static Texture2D ErrorIcon => EditorUtilities.Icons.Error;
		internal static Texture2D IssueIcon => EditorUtilities.Icons.Info;
		internal static Texture2D CheckIcon => EditorUtilities.Icons.CheckCircleColored;

		#endregion

		#region Global Variables

		public Vehicle Instance => target as Vehicle;
		public VehicleTrailer TrailerInstance => target as VehicleTrailer;

#if !MVC_COMMUNITY
		private MeshRenderer interiorLightEdit;
#endif
		private ExhaustParticleSystem[] previewExhaustsParticleSystems;
#if !MVC_COMMUNITY
		private Type[] aiTypes;
		private int newAITypeIndex = -1;
#endif
		private Vector3 orgExhaustPosition;
		private float exhaustsTime;
		private float lastExhaustsTime;
		private bool previewExhausts;

		#endregion

		#endregion

		#region Methods

		#region Virtual Methods

		public override void OnInspectorGUI()
		{
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(!HasInternalErrors && (!Instance.isActiveAndEnabled || !Instance.gameObject.activeInHierarchy));

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

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Vehicle Configurations", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(EditorWindow.HasOpenInstances<VehicleProfilerWindow>());

			if (GUILayout.Button(new GUIContent(EditorUtilities.Icons.Chart, "Vehicle Profiler"), ToolkitEditorUtility.UnstretchableMiniButtonWide))
				VehicleProfilerWindow.OpenWindow();

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

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

				if (!Instance.IsElectric)
				{
					EditorGUI.BeginDisabledGroup(!Instance.Chassis);
					ExhaustsEditor();
					EditorGUI.EndDisabledGroup();
				}
			}

			EditorGUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();

			#endregion

			EditorGUI.BeginDisabledGroup(Instance.Problems.HasComponentsWarnings || Instance.Problems.HasComponentsErrors);
			EditorGUI.BeginDisabledGroup(Instance.MotorWheels.Length < 1);

			#region Behaviour Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Performance", "Customize the vehicle's engine"), EditorStyles.boldLabel);
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

			#region Transmission Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Transmission", "Control your vehicle's drivetrain"), EditorStyles.boldLabel);
			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled)
				GUILayout.Button(new GUIContent(Instance.Problems.HasTransmissionIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(transmissionFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				transmissionFoldout = !transmissionFoldout;

			EditorGUILayout.EndHorizontal();

			if (transmissionFoldout)
				TransmissionEditor();

			EditorGUILayout.EndVertical();

			#endregion

			EditorGUI.EndDisabledGroup();

			#region Brakes Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Brakes", "Change the vehicle stopping performance"), EditorStyles.boldLabel);
			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled)
				GUILayout.Button(new GUIContent(Instance.Problems.HasBrakingIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(brakesFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				brakesFoldout = !brakesFoldout;

			EditorGUILayout.EndHorizontal();

			if (brakesFoldout)
				BrakesEditor();

			EditorGUILayout.EndVertical();

			#endregion

			EditorGUI.BeginDisabledGroup(Instance.SteerWheels.Length < 1);

			#region Steering Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Steering", "Choose how your vehicle turn left & right"), EditorStyles.boldLabel);
			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled)
				GUILayout.Button(new GUIContent(Instance.Problems.HasSteeringErrors ? ErrorIcon : Instance.Problems.HasSteeringIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(steeringFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				steeringFoldout = !steeringFoldout;

			EditorGUILayout.EndHorizontal();

			if (steeringFoldout)
				SteeringEditor();

			EditorGUILayout.EndVertical();

			#endregion

			EditorGUI.EndDisabledGroup();

			#region Suspensions Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Suspensions", "Here you choose if your vehicle must be bouncy or stiff"), EditorStyles.boldLabel);
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
				SuspensionEditor();

			EditorGUILayout.EndVertical();

			#endregion

			EditorGUI.BeginDisabledGroup(Instance.Problems.HasComponentsWarnings || Instance.Problems.HasComponentsErrors);

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

			EditorGUI.EndDisabledGroup();

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

			#region Audio Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Audio", "Edit how your vehicle sounds like"), EditorStyles.boldLabel);
			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled)
				GUILayout.Button(new GUIContent(CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(audioFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				audioFoldout = !audioFoldout;

			EditorGUILayout.EndHorizontal();

			if (audioFoldout)
				AudioEditor();

			EditorGUILayout.EndVertical();

			#endregion

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Other Settings", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(Instance.Problems.HasComponentsWarnings || Instance.Problems.HasComponentsErrors);

#if !MVC_COMMUNITY
			#region AI Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Artificial Intelligence (AI)", "Choose if the vehicle should be driven on its own, or not..."), EditorStyles.boldLabel);
			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled && Instance.IsAI)
				GUILayout.Button(new GUIContent(Instance.Problems.HasAIWarnings ? WarningIcon : Instance.Problems.HasAIErrors ? ErrorIcon : Instance.Problems.HasAIIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(aiFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				aiFoldout = !aiFoldout;

			EditorGUILayout.EndHorizontal();

			if (aiFoldout)
				AIEditor();

			EditorGUILayout.EndVertical();

			#endregion

			#region Damage Foldout

			/*if (Settings.useDamage)
			{
				EditorGUI.BeginDisabledGroup(!ToolkitSettings.IsPlusVersion);
				EditorGUILayout.BeginVertical(GUI.skin.box);

				EditorGUI.indentLevel++;

				EditorGUILayout.BeginHorizontal();

				damageFoldout = EditorGUILayout.Foldout(damageFoldout, "Damage", true) && ToolkitSettings.IsPlusVersion;

				if (ToolkitSettings.IsPlusVersion)
				{
					if (Instance.isActiveAndEnabled)
						EditorGUILayout.LabelField(new GUIContent(/*Instance.Problems.HasDamageWarnings ? WarningIcon : Instance.Problems.HasDamageErrors ? ErrorIcon : Instance.Problems.HasDamageIssues ? IssueIcon : *\/CheckIcon), VehicleEditorUtility.MiddleRightAlignedLabel);
					else
						GUILayout.Button(new GUIContent((Texture2D)null), VehicleEditorUtility.MiddleAlignedIcon);
				}
				else
					EditorGUILayoutPlusIcon();

				EditorGUILayout.EndHorizontal();

				if (damageFoldout)
					DamageEditor();

				EditorGUI.indentLevel--;

				EditorGUILayout.EndVertical();
				EditorGUI.EndDisabledGroup();
			}*/

			#endregion

			#region Interior Foldout

			if (Settings.useInterior)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(5f);
				EditorGUILayout.LabelField(new GUIContent("Interior", "Everything interior related"), EditorStyles.boldLabel);
				GUILayout.Space(2f);

				if (Instance.isActiveAndEnabled && (Instance.Interior.SteeringWheel.transform || Instance.Interior.RPMNeedle.transform || Instance.Interior.SpeedNeedle.transform || Instance.Interior.FuelNeedle.transform || Instance.Interior.IndicatorLeft.renderer || Instance.Interior.IndicatorRight.renderer || Instance.Interior.Handbrake.renderer))
					GUILayout.Button(new GUIContent(Instance.Problems.HasInteriorIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
				else
					GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

				GUILayout.Space(2f);

				if (GUILayout.Button(interiorFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					interiorFoldout = !interiorFoldout;

				EditorGUILayout.EndHorizontal();

				if (interiorFoldout)
					InteriorEditor();

				EditorGUILayout.EndVertical();
			}

			#endregion

			#region Driver IK Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Driver IK", "Everything driver & animations IK related"), EditorStyles.boldLabel);
			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled && Instance.DriverIK.Driver)
				GUILayout.Button(new GUIContent(CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);
			else
				GUILayout.Button(new GUIContent((Texture2D)null), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(driverIKFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				driverIKFoldout = !driverIKFoldout;

			EditorGUILayout.EndHorizontal();

			if (driverIKFoldout)
				DriverIKEditor();

			EditorGUILayout.EndVertical();

			#endregion
#endif

			#region Trailer Link Foldout

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.LabelField(new GUIContent("Trailer Link", "Customize the vehicle's trailer link"), EditorStyles.boldLabel);
			GUILayout.Space(2f);

			if (Instance.isActiveAndEnabled && Instance.TrailerLink)
				GUILayout.Button(new GUIContent(Instance.Problems.HasTrailerLinkIssues ? IssueIcon : CheckIcon), ToolkitEditorUtility.MiddleAlignedIcon);

			GUILayout.Space(2f);

			if (GUILayout.Button(trailerLinkFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				trailerLinkFoldout = !trailerLinkFoldout;

			EditorGUILayout.EndHorizontal();

			if (trailerLinkFoldout)
				TrailerLinkEditor();

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
		public virtual void OnSceneGUI()
		{
			bool exhaustFoldout = Instance.Exhausts != null && Array.Find(Instance.Exhausts, exhaust => exhaust.editorFoldout);

			if (Instance.Chassis && Instance is not VehicleTrailer)
				if (componentsFoldout)
				{
					Handles.color = Settings.exhaustGizmoColor;

					if (previewExhausts)
					{
						if (exhaustFoldout)
						{
							if (Instance.Chassis.ExhaustModel)
								Instance.Chassis.ExhaustModel.localPosition = orgExhaustPosition;

							if (lastExhaustsTime == 0f)
								lastExhaustsTime = (float)EditorApplication.timeSinceStartup;

							float deltaTime = (float)EditorApplication.timeSinceStartup - lastExhaustsTime;

							if (1f / deltaTime > Settings.exhaustSimulationFPS)
								exhaustsTime += deltaTime;
							else
								exhaustsTime = 1f / Settings.exhaustSimulationFPS;

							lastExhaustsTime = (float)EditorApplication.timeSinceStartup;
						}
						else if (Instance.Chassis.ExhaustModel)
						{
							Vector3 shakePosition = .001f * 1.618f * Utility.Multiply(Instance.Chassis.ExhaustShakeIntensity, UnityEngine.Random.insideUnitSphere);

							Instance.Chassis.ExhaustModel.localPosition = orgExhaustPosition + shakePosition;
						}
					}

					for (int i = 0; i < Instance.Exhausts.Length; i++)
					{
						Vector3 position = Instance.Chassis.transform.TransformPoint(Instance.Exhausts[i].localPosition);
						Vector3 direction = Quaternion.AngleAxis(Instance.Exhausts[i].localEulerAngles.x, Instance.Chassis.transform.right) * Quaternion.AngleAxis(Instance.Exhausts[i].localEulerAngles.y, Instance.Chassis.transform.up) * Quaternion.AngleAxis(Instance.Exhausts[i].localEulerAngles.z, Instance.Chassis.transform.forward) * -Instance.Chassis.transform.forward * Settings.gizmosSize / 8f;

						if (!previewExhausts)
							Handles.SphereHandleCap(0, position, Quaternion.identity, Settings.gizmosSize * Utility.Average(Instance.Exhausts[i].LocalScale.x, Instance.Exhausts[i].LocalScale.y) / 16f, EventType.Repaint);
						else if (i < previewExhaustsParticleSystems.Length)
						{
							previewExhaustsParticleSystems[i].particleSystem.Simulate(Instance.Exhausts[i].editorFoldout ? exhaustsTime : 0f);

							for (int j = 0; j < previewExhaustsParticleSystems[i].particleSystems.Length; j++)
								previewExhaustsParticleSystems[i].particleSystems[j].Simulate(Instance.Exhausts[i].editorFoldout ? exhaustsTime : 0f);
						}

						if (Instance.Exhausts[i].editorFoldout)
						{
							if (Tools.current == Tool.Move)
							{
								if (!Tools.hidden)
									Tools.hidden = true;

								Vector3 newExhaustPosition = Instance.Chassis.transform.InverseTransformPoint(Handles.PositionHandle(position, Quaternion.LookRotation(direction)));

								if (Instance.Exhausts[i].localPosition != newExhaustPosition)
								{
									Undo.RegisterCompleteObjectUndo(Instance, "Move Exhaust");

									Instance.Exhausts[i].localPosition = newExhaustPosition;

									EditorUtility.SetDirty(Instance);
								}

								if (previewExhausts)
								{
									previewExhaustsParticleSystems[i].particleSystem.transform.localPosition = newExhaustPosition;

									for (int j = 0; j < previewExhaustsParticleSystems[i].particleSystems.Length; j++)
										previewExhaustsParticleSystems[i].particleSystems[j].transform.localPosition = newExhaustPosition;
								}
							}
							else if (Tools.current == Tool.Rotate)
							{
								if (!Tools.hidden)
									Tools.hidden = true;

								Vector3 newExhaustRotation = (Quaternion.Inverse(Instance.Chassis.transform.rotation) * Handles.RotationHandle(Instance.Chassis.transform.rotation * Quaternion.Euler(Instance.Exhausts[i].localEulerAngles), position)).eulerAngles;

								if (Instance.Exhausts[i].localEulerAngles != newExhaustRotation)
								{
									Undo.RegisterCompleteObjectUndo(Instance, "Rotate Exhaust");

									Instance.Exhausts[i].localEulerAngles = newExhaustRotation;

									EditorUtility.SetDirty(Instance);
								}

								if (previewExhausts)
								{
									Quaternion targetRotation = Quaternion.Euler(Instance.Exhausts[i].localEulerAngles) * Quaternion.Euler(0f, 180f, 0f);

									previewExhaustsParticleSystems[i].particleSystem.transform.localRotation = targetRotation;

									for (int j = 0; j < previewExhaustsParticleSystems[i].particleSystems.Length; j++)
										previewExhaustsParticleSystems[i].particleSystems[j].transform.localRotation = targetRotation;
								}
							}
							else if (Tools.current == Tool.Scale)
							{
								if (!Tools.hidden)
									Tools.hidden = true;

								Vector2 currentExhaustScale = Instance.Exhausts[i].LocalScale;
								Vector2 newExhaustScale = Handles.ScaleHandle(currentExhaustScale, position, Quaternion.LookRotation(direction), Settings.gizmosSize * math.max(Utility.Average(currentExhaustScale.x, currentExhaustScale.y), .1f));

								if (newExhaustScale != currentExhaustScale)
								{
									Undo.RegisterCompleteObjectUndo(Instance, "Scale Exhaust");

									Instance.Exhausts[i].LocalScale = newExhaustScale;

									if (previewExhausts)
									{
										previewExhaustsParticleSystems[i].particleSystem.transform.localScale = new(newExhaustScale.x, newExhaustScale.y, Utility.Average(newExhaustScale.x, newExhaustScale.y));

										for (int j = 0; j < previewExhaustsParticleSystems[i].particleSystems.Length; j++)
											previewExhaustsParticleSystems[i].particleSystems[j].transform.localScale = new(newExhaustScale.x, newExhaustScale.y, Utility.Average(newExhaustScale.x, newExhaustScale.y));
									}

									EditorUtility.SetDirty(Instance);
								}
							}
						}
						else if (!previewExhausts)
							Utility.DrawArrowForDebug(position, direction, Handles.color, Settings.gizmosSize / 32f);
					}
				}

			if (stabilityFoldout && Instance.Chassis)
			{
				Handles.color = Settings.COMGizmoColor;

				Vector3 chassisCenter = Instance.transform.InverseTransformPoint(Instance.Chassis.transform.TransformPoint(Instance.ChassisBounds.center));
				float yCOM = Mathf.Lerp(chassisCenter.y - Instance.ChassisBounds.extents.y, chassisCenter.y + Instance.ChassisBounds.extents.y, Instance.Stability.WeightHeight);
				float zCOM = Mathf.Lerp(TrailerInstance ? chassisCenter.z + Instance.ChassisBounds.extents.z : Instance.FrontWheelsLocalPosition.z, TrailerInstance ? chassisCenter.z - Instance.ChassisBounds.extents.z : Instance.RearWheelsLocalPosition.z, Instance.Stability.WeightDistribution);

				Vector3 position = yCOM * Vector3.up + zCOM * Vector3.forward;

				position = Instance.transform.TransformPoint(position);

				Vector3 newPosition = Handles.FreeMoveHandle(position,
#if !UNITY_2022_2_OR_NEWER
					Instance.transform.rotation,
#endif
					Settings.gizmosSize / 8f, Vector3.zero, Handles.SphereHandleCap);
				float newWeightDistribution = Mathf.InverseLerp(TrailerInstance ? chassisCenter.z + Instance.ChassisBounds.extents.z : Instance.FrontWheelsLocalPosition.z, TrailerInstance ? chassisCenter.z - Instance.ChassisBounds.extents.z : Instance.RearWheelsLocalPosition.z, Instance.transform.InverseTransformPoint(newPosition).z);

				if (Instance.Stability.WeightDistribution != newWeightDistribution)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Distribution");

					Instance.Stability.WeightDistribution = newWeightDistribution;

					EditorUtility.SetDirty(Instance);
				}

				float newWeightHeight = Mathf.InverseLerp(chassisCenter.y - Instance.ChassisBounds.extents.y, chassisCenter.y + Instance.ChassisBounds.extents.y, Instance.transform.InverseTransformPoint(newPosition).y);

				if (Instance.Stability.WeightHeight != newWeightHeight)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Height");

					Instance.Stability.WeightHeight = newWeightHeight;

					EditorUtility.SetDirty(Instance);
				}
			}

			if (driverIKFoldout)
				if (Instance.DriverIK.Driver)
				{
					Handles.color = Settings.driverIKGizmoColor;

					Handles.SphereHandleCap(0, Instance.DriverIK.Driver.LookAtPosition, Instance.transform.rotation, Settings.gizmosSize / 16f, EventType.Repaint);
				}

			if (trailerLinkFoldout && Instance.TrailerLink && Instance.Chassis)
			{
				Handles.color = Settings.jointsGizmoColor;

				if (!Tools.hidden)
					Tools.hidden = true;

				if (Tools.current == Tool.Move)
				{
					Vector3 linkPosition = Instance.Chassis.transform.TransformPoint(Instance.TrailerLink.LinkPoint);
					Vector3 newLinkPoint = Instance.Chassis.transform.InverseTransformPoint(Handles.PositionHandle(linkPosition, Instance.Chassis.transform.rotation));

					if (Instance.TrailerLink.LinkPoint != newLinkPoint)
					{
						Undo.RegisterCompleteObjectUndo(Instance.TrailerLink, "Change Position");

						Instance.TrailerLink.LinkPoint = newLinkPoint;

						EditorUtility.SetDirty(Instance.TrailerLink);
					}
				}
			}

			if (Tools.hidden && (!exhaustFoldout || Tools.current != Tool.Move && Tools.current != Tool.Rotate && Tools.current != Tool.Scale) && (!trailerLinkFoldout || !Instance.TrailerLink || !Instance.Chassis || Tools.current != Tool.Move))
				Tools.hidden = false;
		}

		#endregion

		#region Static Methods

		#region Utilities

		public static bool EnableFoldout()
		{
			trailerJointFoldout = false;
			componentsFoldout = false;
			behaviourFoldout = false;
			transmissionFoldout = false;
			brakesFoldout = false;
			steeringFoldout = false;
			suspensionsFoldout = false;
			tiresFoldout = false;
			stabilityFoldout = false;
			aiFoldout = false;
			//damageFoldout = false;
			interiorFoldout = false;
			audioFoldout = false;
			driverIKFoldout = false;
			trailerLinkFoldout = false;
			trailerFollowerModifierFoldout = false;

			return true;
		}
		public static bool EnableInternalFoldout()
		{
			frontSuspensionFoldout = false;
			rearSuspensionFoldout = false;
			frontBrakesFoldout = false;
			rearBrakesFoldout = false;
			IKPivotsFoldout = false;

			return true;
		}

		private static bool CheckPrefabDialog(bool isPrefab)
		{
			if (isPrefab && EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You're currently trying to modify a Prefab. We won't be able to modify its hierarchy unless it's unpacked.", "Cancel", "Continue Anyway"))
				return false;

			return true;
		}

		#endregion

		#region Menu Items

		[MenuItem("GameObject/MVC/Vehicle", false, 21)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Vehicle", false, 21)]
		public static Vehicle CreateNewVehicle()
		{
			if (!CreateNewVehicleCheck())
				return null;

			GameObject vehicleGameObject = Selection.activeGameObject;

			if (!vehicleGameObject)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "To add a Vehicle component, please select a GameObject within the inspector window.", "Okay");

				return null;
			}

			if (!vehicleGameObject.TryGetComponent(out Vehicle vehicle))
				vehicle = vehicleGameObject.GetComponentInParent<Vehicle>();

			if (!vehicle)
			{
				bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(vehicleGameObject);

				if (!CheckPrefabDialog(isPrefab))
					return null;

				Undo.RegisterCompleteObjectUndo(vehicleGameObject, "Add Controller");
				EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller", "Adding components...", 0f);

				vehicleGameObject.tag = Settings.playerVehicleTag;
				vehicle = Undo.AddComponent<Vehicle>(vehicleGameObject);

				EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller", "Modifying the hierarchy...", .1f);

				Transform chassisTransform = vehicleGameObject.transform.Find("Chassis");

				if (!chassisTransform)
				{
					chassisTransform = new GameObject("Chassis").transform;

					chassisTransform.SetParent(vehicleGameObject.transform, false);
				}

				if (!isPrefab)
					foreach (Transform children in vehicleGameObject.GetComponentsInChildren<Transform>())
						if (children != vehicleGameObject.transform && children != chassisTransform && children.parent == vehicleGameObject.transform)
							children.SetParent(chassisTransform, true);

				EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller", "Modifying the hierarchy...", .5f);

				Transform wheelBrakesContainer = chassisTransform.Find("WheelBrakes");

				if (!wheelBrakesContainer)
					wheelBrakesContainer = vehicleGameObject.transform.Find("WheelBrakes");

				if (wheelBrakesContainer && !isPrefab)
					wheelBrakesContainer.SetParent(vehicleGameObject.transform, true);
				else if (!wheelBrakesContainer)
				{
					wheelBrakesContainer = new GameObject("WheelBrakes").transform;

					wheelBrakesContainer.SetParent(vehicleGameObject.transform, false);
				}

				Transform wheelTransformsContainer = chassisTransform.Find("WheelTransforms");

				if (!wheelTransformsContainer)
					wheelTransformsContainer = vehicleGameObject.transform.Find("WheelTransforms");

				if (wheelTransformsContainer && !isPrefab)
					wheelTransformsContainer.SetParent(vehicleGameObject.transform, true);
				else if (!wheelTransformsContainer)
				{
					wheelTransformsContainer = new GameObject("WheelTransforms").transform;

					wheelTransformsContainer.SetParent(vehicleGameObject.transform, false);
				}

				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "We have successfully added the Vehicle controller to the selected GameObject!", "Okay");
				EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller", "Finishing...", 1f);
			}
			else
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The selected GameObject has a Vehicle component attached to it already!", "Okay");

			vehicle.RefreshLayersAndTags();
			EditorUtility.SetDirty(vehicle.gameObject);
			EditorUtility.ClearProgressBar();

			return vehicle;
		}

		[MenuItem("GameObject/MVC/Vehicle", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/New/Vehicle", true)]
		protected static bool CreateNewVehicleCheck()
		{
			return !EditorApplication.isPlaying && ToolkitInfo.IsSetupDone && !HasInternalErrors;
		}

		#endregion

		#endregion

		#region Global Methods

		#region Utilities

		private void CreateChassis()
		{
			bool reArrangeInspector = EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "The Multiversal Vehicle Controller recommends a re-arrangement to your vehicle's inspector structure. Would you like to use this feature?", "Yes please!", "No thanks");

			if (reArrangeInspector)
			{
				if (!CheckPrefabDialog(PrefabUtility.IsPartOfAnyPrefab(Instance.gameObject)))
					return;

				reArrangeInspector = false;
			}

			CreateChassis(reArrangeInspector);
		}
		private void CreateChassis(bool reArrangeInspector)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Add Chassis");

			Transform chassis = Instance.transform.Find("Chassis");

			if (!chassis)
			{
				chassis = new GameObject("Chassis").transform;

				chassis.SetParent(Instance.transform, false);
			}
			else if (reArrangeInspector)
				chassis.SetParent(Instance.transform, false);

			VehicleChassis chassisInstance = chassis.GetComponent<VehicleChassis>();

			Instance.Chassis = chassisInstance ? chassisInstance : Undo.AddComponent<VehicleChassis>(chassis.gameObject);

			if (reArrangeInspector)
				for (int i = 0; i < Instance.transform.childCount; i++)
				{
					Transform child = Instance.transform.GetChild(i);

					if (child.name != "Chassis" && child.name != "WheelBrakes" && child.name != "WheelTransforms" && child.name != "WheelColliders" && child.name != "COM")
						child.SetParent(chassis);
				}

			Instance.RefreshBounds();
			Instance.RefreshLayersAndTags();
			EditorUtility.SetDirty(Instance.gameObject);
		}
		private void AddExhaust()
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Add Exhaust");

			List<VehicleExhaust> exhausts = Instance.Exhausts.ToList();

			exhausts.Add(new(Instance));

			Instance.Exhausts = exhausts.ToArray();

			EditExhaust(exhausts.Count - 1);
			EditorUtility.SetDirty(Instance);
		}
		private void EditExhaust(int index)
		{
			Instance.Exhausts.ToList().ForEach(exhaust => exhaust.editorFoldout = false);

			Instance.Exhausts[index].editorFoldout = true;
		}
		private void DuplicateExhaust(int index)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Duplicate Exhaust");

			List<VehicleExhaust> exhausts = Instance.Exhausts.ToList();

			exhausts.Add(new(exhausts[index]));

			Instance.Exhausts = exhausts.ToArray();

			EditExhaust(Instance.Exhausts.Length - 1);
			EditorUtility.SetDirty(Instance);
		}
		private void RemoveExhaust(int index)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Remove Exhaust");

			List<VehicleExhaust> exhausts = Instance.Exhausts.ToList();

			exhausts.RemoveAt(index);

			Instance.Exhausts = exhausts.ToArray();

			EditorUtility.SetDirty(Instance);
		}
		private void RemoveAllExhausts()
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Remove All Exhausts");

			Instance.Exhausts = new VehicleExhaust[] { };

			EditorUtility.SetDirty(Instance);
		}
		private void PreviewExhausts()
		{
			if (!Settings.exhaustSmoke && !Settings.exhaustFlame && !Settings.NOSFlame || !Instance.Chassis)
				return;

			previewExhaustsParticleSystems = new ExhaustParticleSystem[Instance.Exhausts.Length];

			for (int i = 0; i < previewExhaustsParticleSystems.Length; i++)
			{
				ParticleSystem particleSystem = Instance.Engine.FuelType == VehicleEngine.EngineFuelType.Diesel && Settings.exhaustSmoke || Settings.exhaustFlame ? Instance.Engine.FuelType == VehicleEngine.EngineFuelType.Diesel ? Settings.exhaustSmoke : Settings.exhaustFlame : Settings.NOSFlame;
				Vector3 position = Instance.Chassis.transform.TransformPoint(Instance.Exhausts[i].localPosition);
				Quaternion rotation = Quaternion.Euler(Instance.Chassis.transform.TransformVector(Instance.Exhausts[i].localEulerAngles)) * Quaternion.Euler(0f, 180f, 0f);
				Vector3 scale = new(Instance.Exhausts[i].LocalScale.x, Instance.Exhausts[i].LocalScale.y, Utility.Average(Instance.Exhausts[i].LocalScale.x, Instance.Exhausts[i].LocalScale.y));

				previewExhaustsParticleSystems[i].particleSystem = VehicleVisuals.NewParticleSystem(Instance, particleSystem, $"ExhaustFlame_Preview_{i + 1}", position, rotation, true, true, true);
				previewExhaustsParticleSystems[i].particleSystem.transform.localScale = scale;
				previewExhaustsParticleSystems[i].particleSystems = previewExhaustsParticleSystems[i].particleSystem.GetComponentsInChildren<ParticleSystem>().Where(ps => ps != previewExhaustsParticleSystems[i].particleSystem).ToArray();

				for (int j = 0; j < previewExhaustsParticleSystems[i].particleSystems.Length; j++)
					previewExhaustsParticleSystems[i].particleSystems[j].transform.parent = previewExhaustsParticleSystems[i].particleSystem.transform.parent;

				ParticleSystem.SubEmittersModule subEmitters = previewExhaustsParticleSystems[i].particleSystem.subEmitters;

				if (subEmitters.enabled)
				{
					while (subEmitters.subEmittersCount > 0)
						subEmitters.RemoveSubEmitter(0);

					subEmitters.enabled = false;
				}
			}

			orgExhaustPosition = Instance.Chassis.ExhaustModel ? Instance.Chassis.ExhaustModel.localPosition : Vector3.zero;
			exhaustsTime = 0f;
			lastExhaustsTime = 0f;
			previewExhausts = true;
		}
		private void HideExhausts()
		{
			if (!Instance.Chassis || !previewExhausts)
				return;

			previewExhaustsParticleSystems = null;
			exhaustsTime = 0f;
			lastExhaustsTime = 0f;
			previewExhausts = false;

			if (Instance.Chassis.ExhaustModel)
				Instance.Chassis.ExhaustModel.localPosition = orgExhaustPosition;

			if (Instance.Chassis.transform.Find("VisualEffects"))
				Utility.Destroy(true, Instance.Chassis.transform.Find("VisualEffects").gameObject);
		}
		private void AddWheel()
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Add Wheel");

			List<VehicleWheel.WheelModule> wheels = Instance.Wheels.ToList();

			wheels.Add(new(Instance));

			Instance.Wheels = wheels.ToArray();

			Instance.RefreshWheels();
			EditorUtility.SetDirty(Instance);
		}
		private void GenerateWheels()
		{
			bool reArrangeInspector = EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "The Multiversal Vehicle Controller recommends a re-arrangement to your vehicle's inspector structure. Would you like to use this feature?", "Yes please!", "No thanks");

			if (reArrangeInspector)
			{
				if (!CheckPrefabDialog(PrefabUtility.IsPartOfAnyPrefab(Instance.gameObject)))
					return;

				reArrangeInspector = false;
			}

			if (!Instance.Chassis)
			{
				if (!EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "It seems like this vehicle doesn't contain a Chassis component! To proceed generating wheels, a Chassis component is required. Do you want to create one?", "Yes", "No"))
					return;

				CreateChassis(reArrangeInspector);
			}

			Undo.RegisterFullObjectHierarchyUndo(Instance, "Generate Wheels");

			Transform brakesParent = Instance.transform.Find("WheelBrakes");

			if (!brakesParent)
			{
				brakesParent = new GameObject("WheelBrakes").transform;

				brakesParent.SetParent(Instance.transform, false);
			}

			Transform transformsParent = Instance.transform.Find("WheelTransforms");

			if (!transformsParent)
				transformsParent = new GameObject("WheelTransforms").transform;

			transformsParent.SetParent(Instance.transform, false);

			Transform collidersParent = Instance.transform.Find("WheelColliders");

			if (!collidersParent)
				collidersParent = new GameObject("WheelColliders").transform;

			collidersParent.SetParent(Instance.transform, false);

			Transform frontCollidersParent = Instance.IsTrailer ? null : collidersParent.Find("FrontWheelColliders");

			if (!frontCollidersParent && !Instance.IsTrailer)
			{
				frontCollidersParent = new GameObject("FrontWheelColliders").transform;

				frontCollidersParent.SetParent(collidersParent, false);
			}

			Transform rearCollidersParent = Instance.IsTrailer ? null : collidersParent.Find("RearWheelColliders");

			if (!rearCollidersParent && !Instance.IsTrailer)
			{
				rearCollidersParent = new GameObject("RearWheelColliders").transform;

				rearCollidersParent.SetParent(collidersParent, false);
			}

			VehicleWheel[] wheels = Instance.GetComponentsInChildren<VehicleWheel>();

			for (int i = 0; i < wheels.Length; i++)
				DestroyImmediate(wheels[i].gameObject);

			bool autoAssignRimAndTire = EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Do you want to auto assign the wheel fields? Consider verifying your wheels inspector structure if you're willing to use this feature.", "Yes", "No");

			for (int i = 0; i < Instance.Wheels.Length; i++)
			{
				var wheel = Instance.Wheels[i];

				if (!wheel.Model)
					return;

				if (reArrangeInspector)
					wheel.Model.SetParent(transformsParent, true);

				Transform colliderParent = Instance.IsTrailer ? collidersParent : wheel.IsFrontWheel ? frontCollidersParent : rearCollidersParent;
				string modelName = wheel.Model.name;
				Transform colliderTransform = new GameObject(modelName).transform;
				Transform brakeTransform = brakesParent.Find(wheel.Model.name);
				string wheelPosition = $"{wheel.DriveTrain.ToString().ToUpper().FirstOrDefault()}{wheel.side.ToString().ToUpper().FirstOrDefault()}";

				if (!brakeTransform)
					brakeTransform = brakesParent.Find($"Brake{wheelPosition}");

				if (!brakeTransform)
					brakeTransform = brakesParent.Find($"Wheel{wheelPosition}");

				if (!brakeTransform)
				{
					brakeTransform = new GameObject(modelName).transform;

					brakeTransform.SetParent(brakesParent, false);
				}

				colliderTransform.gameObject.layer = Settings.vehiclesLayer;

				colliderTransform.SetParent(colliderParent, false);

				wheel.Instance = Undo.AddComponent<VehicleWheel>(colliderTransform.gameObject);
				wheel.Instance.transform.position = wheel.Model.position;
				wheel.Model = wheel.Model;
				wheel.Tire = wheel.Tire;
				wheel.Rim = wheel.Rim;
				wheel.BrakeCalliper = wheel.BrakeCalliper;
				brakeTransform.transform.position = wheel.Model.position;

				if (reArrangeInspector && wheel.BrakeCalliper)
					wheel.BrakeCalliper.SetParent(brakeTransform, true);

				if (autoAssignRimAndTire)
				{
					if (!wheel.Rim)
					{
						Transform rimTransform = wheel.Model.Find("Rim", false);

						if (rimTransform)
						{
							wheel.Rim = rimTransform;

							if (!wheel.RimEdgeRenderer)
							{
								Transform rimEdgeTransform = wheel.Rim.FindContains("edge", false);

								if (rimEdgeTransform)
								{
									MeshRenderer rimEdgeRenderer = rimEdgeTransform.GetComponent<MeshRenderer>();

									if (rimEdgeRenderer)
										wheel.RimEdgeRenderer = rimEdgeRenderer;
								}
							}
						}
					}

					if (!wheel.BrakeDiscRenderer)
					{
						Transform brakeDiskTransform = wheel.Model.FindContains("brake", false);

						if (!brakeDiskTransform)
							brakeDiskTransform = wheel.Model.FindContains("disk", false);

						if (brakeDiskTransform)
						{
							MeshRenderer brakeDiskRenderer = brakeDiskTransform.GetComponentInChildren<MeshRenderer>();

							if (brakeDiskRenderer)
								wheel.BrakeDiscRenderer = brakeDiskRenderer;
						}
					}

					if (!wheel.Tire)
					{
						Transform tireTransform = wheel.Model.Find("Tire");

						if (tireTransform)
							wheel.Tire = tireTransform;
					}

					if (!wheel.BrakeCalliper)
						wheel.BrakeCalliper = brakeTransform;
				}

				wheel.Instance.RefreshTireThickness();
			}

			Instance.RefreshWheels();
			Instance.RefreshBounds();
			Instance.RefreshWeightHeight();
			Instance.RefreshLayersAndTags();

			if (!Instance.IsTrailer)
			{
				switch (EditorUtility.DisplayDialogComplex("Multiversal Vehicle Controller: Info", "Choose your power-train setup!", "FWD", "RWD", "AWD"))
				{
					case 0:
						foreach (var wheel in Instance.Wheels)
							wheel.IsMotorWheel = wheel.IsFrontWheel;

						break;

					case 1:
						foreach (var wheel in Instance.Wheels)
							wheel.IsMotorWheel = !wheel.IsFrontWheel;

						break;

					case 2:
						foreach (var wheel in Instance.Wheels)
							wheel.IsMotorWheel = true;

						break;
				}

				switch (EditorUtility.DisplayDialogComplex("Multiversal Vehicle Controller: Info", "Choose your steer-train setup!", "FWD", "RWD", "AWD"))
				{
					case 0:
						foreach (var wheel in Instance.Wheels)
							wheel.IsSteerWheel = wheel.IsFrontWheel;

						break;

					case 1:
						Instance.Steering.invertRearSteer = true;

						foreach (var wheel in Instance.Wheels)
							wheel.IsSteerWheel = !wheel.IsFrontWheel;

						break;

					case 2:
						Instance.Steering.invertRearSteer = true;

						foreach (var wheel in Instance.Wheels)
							wheel.IsSteerWheel = true;

						break;
				}
			}

			EditorUtility.SetDirty(Instance.gameObject);
		}
		private void EditWheel(int index)
		{
			EditWheel(Instance.Wheels[index]);
		}
		private void EditWheel(VehicleWheel.WheelModule wheel)
		{
			Instance.Wheels.ToList().ForEach(wheel => wheel.editorFoldout = VehicleWheel.EditorFoldout.None);

			wheel.editorFoldout = VehicleWheel.EditorFoldout.Components;

			Instance.RefreshWheels();
		}
		private void RemoveWheel(int index)
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Remove Wheel");

			if (Instance.Wheels[index].Instance)
				DestroyImmediate(Instance.Wheels[index].Instance.gameObject);

			List<VehicleWheel.WheelModule> wheels = Instance.Wheels.ToList();

			wheels.RemoveAt(index);

			Instance.Wheels = wheels.ToArray();

			Instance.RefreshWheels();
			EditorUtility.SetDirty(Instance.gameObject);
		}
		private void RemoveAllWheels()
		{
			Undo.RegisterCompleteObjectUndo(Instance, "Remove All Wheels");

			Instance.Wheels = new VehicleWheel.WheelModule[] { };

			Transform wheelCollidersContainer = Instance.transform.Find("WheelColliders");

			if (wheelCollidersContainer)
				Undo.DestroyObjectImmediate(wheelCollidersContainer.gameObject);

			Instance.RefreshWheels();
			EditorUtility.SetDirty(Instance.gameObject);
		}
#if !MVC_COMMUNITY
		private void EditInteriorLight(MeshRenderer renderer)
		{
			interiorLightEdit = renderer;
		}
		private void SaveInteriorLight()
		{
			interiorLightEdit = null;
		}
#endif

		#endregion

		#region Editor

		internal void ChassisEditor()
		{
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();

			if (!Instance.Chassis)
			{
				VehicleChassis newChassis = EditorGUILayout.ObjectField("Chassis", Instance.Chassis, typeof(VehicleChassis), true) as VehicleChassis;

				if (Instance.Chassis != newChassis)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Chassis");

					Instance.Chassis = newChassis;

					EditorUtility.SetDirty(Instance);
				}

				if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					CreateChassis();
			}
			else
				EditorGUILayout.LabelField("Chassis", EditorStyles.miniBoldLabel);

			EditorGUILayout.EndHorizontal();

			if (Instance.Chassis)
			{
				EditorGUI.indentLevel++;

				if (!TrailerInstance)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField("Engine", EditorStyles.miniBoldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						ToolkitEditorUtility.SelectObject(Instance.Chassis.gameObject);

						VehicleChassisEditor.engineFoldout = VehicleChassisEditor.EnableFoldout();
					}

					EditorGUILayout.EndHorizontal();
				}

				if (Settings.useLights)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField("Lights", EditorStyles.miniBoldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						ToolkitEditorUtility.SelectObject(Instance.Chassis.gameObject);

						VehicleChassisEditor.lightsFoldout = VehicleChassisEditor.EnableFoldout();
					}

					EditorGUILayout.EndHorizontal();
				}
#if !MVC_COMMUNITY

				if (Settings.useChassisWings && !TrailerInstance)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField("AeroDynamic Wings", EditorStyles.miniBoldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						ToolkitEditorUtility.SelectObject(Instance.Chassis.gameObject);

						VehicleChassisEditor.wingsFoldout = VehicleChassisEditor.EnableFoldout();
					}

					EditorGUILayout.EndHorizontal();
				}

				if (Settings.useAntiGroundColliders)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField("Anti-Ground Colliders", EditorStyles.miniBoldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						ToolkitEditorUtility.SelectObject(Instance.Chassis.gameObject);

						VehicleChassisEditor.collidersFoldout = VehicleChassisEditor.EnableFoldout();
					}

					EditorGUILayout.EndHorizontal();
				}

				/*if (Settings.useDamage)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField("Parts", EditorStyles.miniBoldLabel);
				
#if !MVC_COMMUNITY
					if (!ToolkitInfo.IsProLicense)
#endif
						GUILayoutProIcon();

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, VehicleEditorUtility.UnstretchableMiniButtonWide))
					{
						VehicleEditorUtility.SelectObject(Instance.Chassis.gameObject);

						VehicleChassisEditor.partsFoldout = VehicleChassisEditor.EnableFoldout();
					}

					EditorGUILayout.EndHorizontal();
				}*/

				if (!TrailerInstance)
				{
					EditorGUILayout.BeginHorizontal(GUI.skin.box);
					EditorGUILayout.LabelField("Follower Pivots", EditorStyles.miniBoldLabel);

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						ToolkitEditorUtility.SelectObject(Instance.Chassis.gameObject);

						VehicleChassisEditor.followerPivotsFoldout = VehicleChassisEditor.EnableFoldout();
					}

					EditorGUILayout.EndHorizontal();
				}
#endif

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
		}
		internal void WheelsEditor()
		{
			float orgLabelWidth = EditorGUIUtility.labelWidth;

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();

			EditorGUIUtility.labelWidth = 50f;

			EditorGUILayout.LabelField("Wheels", EditorStyles.miniBoldLabel);

			EditorGUIUtility.labelWidth = orgLabelWidth;

			if (Instance.Wheels.Length > 1)
				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Are you sure you want to remove all the existing wheels at once?", "Yes", "No"))
						RemoveAllWheels();

			if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				AddWheel();

			EditorGUILayout.EndHorizontal();

			if (Instance.Wheels.Length > 0)
			{
				EditorGUI.indentLevel++;

				for (int i = 0; i < Instance.Wheels.Length; i++)
				{
					var wheel = Instance.Wheels[i];
					bool remove = false;

					EditorGUILayout.BeginVertical(GUI.skin.box);
					EditorGUILayout.BeginHorizontal();

					if (wheel.Model)
					{
						EditorGUIUtility.labelWidth = 50f;

						EditorGUILayout.LabelField(wheel.WheelName, EditorStyles.miniBoldLabel);

						EditorGUIUtility.labelWidth = orgLabelWidth;

						if (wheel.editorFoldout != VehicleWheel.EditorFoldout.None)
						{
							if (GUILayout.Button(EditorUtilities.Icons.ChevronUp, ToolkitEditorUtility.UnstretchableMiniButtonWide))
								wheel.editorFoldout = VehicleWheel.EditorFoldout.None;
						}
						else
						{
							if (GUILayout.Button(EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
								EditWheel(i);
						}
					}
					else
					{
						Transform newModel = EditorGUILayout.ObjectField("New Wheel", wheel.Model, typeof(Transform), true) as Transform;

						if (wheel.Model != newModel)
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Add Wheel Model");

							wheel.Model = newModel;
							wheel.editorFoldout = VehicleWheel.EditorFoldout.None;

							Instance.RefreshWheels();
							EditorUtility.SetDirty(Instance);
						}
					}

					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						remove = true;

					EditorGUILayout.EndHorizontal();

					if (wheel.Model && wheel.editorFoldout != VehicleWheel.EditorFoldout.None)
					{
						EditorGUI.indentLevel++;

						var foldout = wheel.editorFoldout;
						bool componentsFoldout = foldout == VehicleWheel.EditorFoldout.Components;
						bool dimensionsFoldout = foldout == VehicleWheel.EditorFoldout.Dimensions;

						ToolkitEditorUtility.ToggleTabButtons("Components", "Dimensions", ref componentsFoldout, ref dimensionsFoldout);

						if (componentsFoldout)
							wheel.editorFoldout = VehicleWheel.EditorFoldout.Components;
						else if (dimensionsFoldout)
							wheel.editorFoldout = VehicleWheel.EditorFoldout.Dimensions;
						else
							wheel.editorFoldout = VehicleWheel.EditorFoldout.None;

						EditorGUILayout.Space();

						switch (wheel.editorFoldout)
						{
							case VehicleWheel.EditorFoldout.Components:
								if (!wheel.IsTrailerWheel)
								{
									VehicleWheel.DriveTrain newDriveTrain = (VehicleWheel.DriveTrain)EditorGUILayout.EnumPopup("Drive Train", wheel.DriveTrain);

									if (wheel.DriveTrain != newDriveTrain)
									{
										Undo.RegisterCompleteObjectUndo(Instance, "Change Drive Train");

										wheel.DriveTrain = newDriveTrain;

										Instance.RefreshWheels();
										EditorUtility.SetDirty(Instance);
									}
								}

								VehicleWheel.Side newPosition = (VehicleWheel.Side)EditorGUILayout.EnumPopup("Position", wheel.side);

								if (wheel.side != newPosition)
								{
									Undo.RegisterCompleteObjectUndo(Instance, "Change Wheel Position");

									wheel.side = newPosition;

									Instance.RefreshWheels();
									EditorUtility.SetDirty(Instance);
								}

								if (!wheel.IsTrailerWheel)
								{
									bool newIsMotorWheel = ToolkitEditorUtility.ToggleButtons(new GUIContent("Motor", "Is this wheel a motor wheel with torque applied to it?"), null, "Yes", "No", wheel.IsMotorWheel, Instance, "Change Wheel Type");

									if (wheel.IsMotorWheel != newIsMotorWheel)
									{
										wheel.IsMotorWheel = newIsMotorWheel;

										Instance.RefreshWheels();
									}

									bool newIsSteerWheel = ToolkitEditorUtility.ToggleButtons(new GUIContent("Steer", "Is this wheel a steer wheel with a steer angle applied to it?"), null, "Yes", "No", wheel.IsSteerWheel, Instance, "Change Wheel Type");

									if (wheel.IsSteerWheel != newIsSteerWheel)
									{
										wheel.IsSteerWheel = newIsSteerWheel;

										Instance.RefreshWheels();
									}
								}

								EditorGUI.BeginDisabledGroup(true);
								EditorGUILayout.ObjectField("Model", wheel.Model, typeof(Transform), true);
								EditorGUI.EndDisabledGroup();

								EditorGUI.indentLevel++;

								Transform newRim = EditorGUILayout.ObjectField("Rim", wheel.Rim, typeof(Transform), true) as Transform;

								if (wheel.Rim != newRim)
								{
									if (!newRim || newRim.transform.IsChildOf(wheel.Model))
									{
										Undo.RegisterCompleteObjectUndo(Instance, "Change Model");

										wheel.Rim = newRim;

										Instance.RefreshWheels();
										EditorUtility.SetDirty(Instance);
									}
									else
										EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", $"The new rim GameObject has to be a child of the `{wheel.Model.name}` GameObject.", "Okay");
								}

								EditorGUI.indentLevel++;

								if (Settings.useDamage && Settings.useWheelHealth)
								{
									EditorGUI.BeginDisabledGroup(!wheel.Rim);
									EditorGUILayout.BeginHorizontal();

									MeshRenderer newRimEdgeRenderer = EditorGUILayout.ObjectField(new GUIContent("Rim Edge", "The rim edge renderer used to simulate some friction emissive effects with the ground once the vehicle tire is fully damaged"), wheel.RimEdgeRenderer, typeof(MeshRenderer), true) as MeshRenderer;

									if (wheel.RimEdgeRenderer != newRimEdgeRenderer)
									{
										if (!newRimEdgeRenderer || newRimEdgeRenderer.transform.IsChildOf(wheel.Rim))
										{
											Undo.RegisterCompleteObjectUndo(Instance, "Change Renderer");

											wheel.RimEdgeRenderer = newRimEdgeRenderer;

											Instance.RefreshWheels();
											EditorUtility.SetDirty(Instance);
										}
										else
											EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", $"The new rim edge GameObject has to be a child of the `{wheel.Rim.name}` GameObject.", "Okay");
									}

									EditorGUILayout.EndHorizontal();

									if (wheel.RimEdgeRenderer)
									{
										EditorGUI.indentLevel++;

										string[] materialNames = new string[wheel.RimEdgeRenderer.sharedMaterials.Length];

										for (int j = 0; j < materialNames.Length; j++)
											materialNames[j] = $"{j + 1}. {(wheel.RimEdgeRenderer.sharedMaterials[j] ? wheel.RimEdgeRenderer.sharedMaterials[j].name : "")}";

										int newRimEdgeMaterialIndex = EditorGUILayout.Popup(new GUIContent("Material", "The rim emissive material"), wheel.RimEdgeMaterialIndex, materialNames);

										if (wheel.RimEdgeMaterialIndex != newRimEdgeMaterialIndex)
										{
											Undo.RegisterCompleteObjectUndo(Instance, "Change Material");

											wheel.RimEdgeMaterialIndex = newRimEdgeMaterialIndex;

											Instance.RefreshWheels();
											EditorUtility.SetDirty(Instance);
										}

										bool newHideRimEdgeAtIdle = ToolkitEditorUtility.ToggleButtons(new GUIContent("Hide Per Default", "Should the rim edge be hidden while the wheel isn't fully damaged or the vehicle is sleeping?"), null, "Yes", "No", wheel.HideRimEdgePerDefault, Instance, "Switch Hiding");

										if (wheel.HideRimEdgePerDefault != newHideRimEdgeAtIdle)
											wheel.HideRimEdgePerDefault = newHideRimEdgeAtIdle;

										EditorGUI.indentLevel--;
									}

									EditorGUI.EndDisabledGroup();
								}

								if (!Instance.IsTrailer || Instance.IsTrailer && (Instance as VehicleTrailer).useBrakes)
								{
									EditorGUILayout.BeginHorizontal();

									MeshRenderer newBrakeDiscRenderer = EditorGUILayout.ObjectField("Brake Disc", wheel.BrakeDiscRenderer, typeof(MeshRenderer), true) as MeshRenderer;

									if (wheel.BrakeDiscRenderer != newBrakeDiscRenderer)
									{
										if (!newBrakeDiscRenderer || newBrakeDiscRenderer.transform.IsChildOf(wheel.Model))
										{
											Undo.RegisterCompleteObjectUndo(Instance, "Change Renderer");

											wheel.BrakeDiscRenderer = newBrakeDiscRenderer;

											Instance.RefreshWheels();
											EditorUtility.SetDirty(Instance);
										}
										else
											EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", $"The new brake disc GameObject has to be a child of the `{wheel.Model.name}` GameObject.", "Okay");
									}

									EditorGUILayout.EndHorizontal();
								}

								EditorGUI.indentLevel--;

								Transform newTire = EditorGUILayout.ObjectField("Tire", wheel.Tire, typeof(Transform), true) as Transform;

								if (wheel.Tire != newTire)
								{
									if (!newTire || newTire.transform.IsChildOf(wheel.Model))
									{
										Undo.RegisterCompleteObjectUndo(Instance, "Change Model");

										wheel.Tire = newTire;

										Instance.RefreshWheels();
										EditorUtility.SetDirty(Instance);
									}
									else
										EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", $"The new tire GameObject has to be a child of the `{wheel.Model.name}` GameObject.", "Okay");
								}

								EditorGUI.indentLevel--;

								if (!Instance.IsTrailer || Instance.IsTrailer && (Instance as VehicleTrailer).useBrakes)
								{
									Transform newWheelBrake = EditorGUILayout.ObjectField("Brake Calliper", wheel.BrakeCalliper, typeof(Transform), true) as Transform;

									if (wheel.BrakeCalliper != newWheelBrake)
									{
										Undo.RegisterCompleteObjectUndo(Instance, "Change Model");

										wheel.BrakeCalliper = newWheelBrake;

										Instance.RefreshWheels();
										EditorUtility.SetDirty(Instance);
									}
								}

								EditorGUI.BeginDisabledGroup(true);
								EditorGUILayout.ObjectField("Instance", wheel.Instance, typeof(VehicleWheel), true);
								EditorGUI.EndDisabledGroup();

								break;

							case VehicleWheel.EditorFoldout.Dimensions:
								float newMass = math.abs(ToolkitEditorUtility.NumberField(new GUIContent("Mass", "The wheel mass. Increasing this value may increase the amount of torque it takes to rotate the wheel and increase the downforce"), wheel.Instance.mass, Utility.Units.Weight, 2, wheel.Instance, "Change Mass"));

								if (wheel.Instance.mass != newMass)
									wheel.Instance.mass = newMass;

								EditorGUILayout.Space();
								EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
								EditorGUILayout.BeginHorizontal();

								float newWidth = ToolkitEditorUtility.NumberField(new GUIContent("Width", "The tire width"), wheel.Instance.Width * 1000f, Utility.Units.SizeAccurate, true, wheel.Instance, "Change Width") * .001f;

								if (wheel.Instance.Width != newWidth)
									wheel.Instance.Width = newWidth;

								EditorGUI.BeginDisabledGroup(!wheel.Model);

								if (GUILayout.Button("Auto", new GUIStyle(ToolkitEditorUtility.UnstretchableMiniButtonWide) { fixedWidth = 40f }))
								{
									Undo.RegisterCompleteObjectUndo(wheel.Instance, "Change Width");
									wheel.Instance.RefreshWidth();
									EditorUtility.SetDirty(wheel.Instance);
								}

								EditorGUILayout.EndHorizontal();
								EditorGUI.EndDisabledGroup();

								int newAspect = ToolkitEditorUtility.NumberField(new GUIContent("Aspect", "The vehicle tire aspect"), wheel.Instance.Aspect, Utility.Units.SizeAccurate, wheel.Instance, "Change Aspect");

								if (wheel.Instance.Aspect != newAspect)
									wheel.Instance.Aspect = newAspect;

								EditorGUILayout.BeginHorizontal();

								int newDiameter = Mathf.RoundToInt(ToolkitEditorUtility.NumberField(new GUIContent("Diameter", "The vehicle tire diameter"), wheel.Instance.Diameter, Utility.Unit(Utility.Units.Size, Utility.UnitType.Imperial), Utility.FullUnit(Utility.Units.Size, Utility.UnitType.Imperial), true, wheel.Instance, "Change Diameter"));

								if (wheel.Instance.Diameter != newDiameter)
									wheel.Instance.Diameter = newDiameter;

								if (Settings.editorValuesUnit == Utility.UnitType.Metric)
								{
									string diameterApproximation = $"≈ {Utility.NumberToValueWithUnit(wheel.Instance.Diameter / Utility.UnitMultiplier(Utility.Units.Size, Utility.UnitType.Imperial), Utility.Units.Size, Utility.UnitType.Metric, true)}";

									EditorGUI.indentLevel--;

									EditorGUILayout.LabelField(diameterApproximation, GUILayout.Width(EditorStyles.label.CalcSize(new(diameterApproximation)).x));

									EditorGUI.indentLevel++;
								}

								EditorGUILayout.EndHorizontal();

								if (Settings.useWheelHealth)
								{
									EditorGUILayout.BeginHorizontal();

									float newTireThickness = ToolkitEditorUtility.NumberField(new GUIContent("Tire Thickness", "The wheel tire thickness. Increasing this value may take the tire more time to fully being damaged"), wheel.Instance.tireThickness, Utility.Units.Size, 2, wheel.Instance, "Change Thickness");

									if (wheel.Instance.tireThickness != newTireThickness)
										wheel.Instance.tireThickness = newTireThickness;

									EditorGUI.BeginDisabledGroup(!wheel.Model || !wheel.Rim);

									if (GUILayout.Button("Auto", new GUIStyle(ToolkitEditorUtility.UnstretchableMiniButtonWide) { fixedWidth = 40f }))
									{
										Undo.RegisterCompleteObjectUndo(wheel.Instance, "Change Thickness");
										wheel.Instance.RefreshTireThickness();
										EditorUtility.SetDirty(wheel.Instance);
									}

									EditorGUI.EndDisabledGroup();
									EditorGUILayout.EndHorizontal();
								}

								EditorGUI.EndDisabledGroup();

								break;
						}

						EditorGUILayout.Space();

						EditorGUI.indentLevel--;
					}
					else if (wheel.editorFoldout != VehicleWheel.EditorFoldout.None && wheel.editorFoldout != VehicleWheel.EditorFoldout.Components)
						wheel.editorFoldout = VehicleWheel.EditorFoldout.Components;

					EditorGUILayout.EndVertical();

					if (remove)
						RemoveWheel(i);
				}

				EditorGUI.indentLevel--;

				if (Instance.Problems.MissingWheelTransformsCount > 0 || Instance.Problems.MissingWheelBehavioursCount > 0)
				{
					EditorGUILayout.BeginVertical(GUI.skin.box);

					EditorGUI.indentLevel--;

					if (Instance.Problems.MissingWheelTransformsCount > 0)
						EditorGUILayout.HelpBox("If you want to generate wheels, you have to assign the transform fields first!", MessageType.Info, true);

					EditorGUI.BeginDisabledGroup(Instance.Problems.MissingWheelTransformsCount > 0);

					if (GUILayout.Button("Generate Wheels"))
						GenerateWheels();

					EditorGUI.EndDisabledGroup();

					EditorGUI.indentLevel++;

					EditorGUILayout.EndVertical();
				}
				else
					EditorGUILayout.HelpBox($"Drivetrain: {Instance.Drivetrain}", MessageType.None);
			}
			else
				EditorGUILayout.HelpBox("Use the \"+\" button in front of the 'Wheels' label to add a new wheel.", MessageType.Info);

			EditorGUILayout.EndVertical();
		}
		internal void ExhaustsEditor()
		{
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();

			float orgLabelWidth = EditorGUIUtility.labelWidth;

			EditorGUIUtility.labelWidth = 50f;

			EditorGUILayout.LabelField("Exhausts", EditorStyles.miniBoldLabel);

			EditorGUIUtility.labelWidth = orgLabelWidth;

			if (Instance.Chassis && Instance.Exhausts != null && (Instance.Exhausts.Length > 0 || Instance.Chassis.ExhaustModel))
			{
				EditorGUI.BeginDisabledGroup(!Settings.useParticleSystems || !Settings.exhaustFlame && !Settings.NOSFlame);

				if (!previewExhausts)
				{
					if (GUILayout.Button(EditorUtilities.Icons.Eye, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						PreviewExhausts();
				}
				else if (GUILayout.Button(EditorUtilities.Icons.Save, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					HideExhausts();

				EditorGUI.EndDisabledGroup();
			}

			EditorGUI.BeginDisabledGroup(previewExhausts);

			if (Instance.Chassis && Instance.Exhausts != null)
				if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					AddExhaust();

			if (Instance.Exhausts != null && Instance.Exhausts.Length > 1)
				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Are you sure you want to remove all the existing exhausts at once?", "Yes", "No"))
						RemoveAllExhausts();

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (Instance.Chassis && Instance.Exhausts != null)
			{
				EditorGUI.indentLevel++;

				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginHorizontal();

				Transform newExhaustGroup = EditorGUILayout.ObjectField(new GUIContent("Model", "The exhaust model transform"), Instance.Chassis.ExhaustModel, typeof(Transform), true) as Transform;

				if (Instance.Chassis.ExhaustModel != newExhaustGroup)
				{
					if (!newExhaustGroup || newExhaustGroup.IsChildOf(Instance.Chassis.transform))
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Model");

						Instance.Chassis.ExhaustModel = newExhaustGroup;

						EditorUtility.SetDirty(Instance);
					}
					else
						EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "The new exhaust model has to be a child of the Chassis.", "Okay");
				}

				EditorGUILayout.EndHorizontal();

				if (Instance.Chassis.ExhaustModel)
				{
					EditorGUI.indentLevel++;

					Utility.Precision newExhaustShakingPrecision = (Utility.Precision)EditorGUILayout.EnumPopup(new GUIContent("Precision", "The exhaust shake precision"), Instance.Chassis.exhaustShakingPrecision);

					if (Instance.Chassis.exhaustShakingPrecision != newExhaustShakingPrecision)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Precision");

						Instance.Chassis.exhaustShakingPrecision = newExhaustShakingPrecision;

						EditorUtility.SetDirty(Instance);
					}

					if (Instance.Chassis.exhaustShakingPrecision == Utility.Precision.Advanced)
					{
						Vector3 newExhaustShakeIntensity = EditorGUILayout.Vector3Field(new GUIContent("Intensity", "Exhaust shaking intensity at higher power values"), Instance.Chassis.ExhaustShakeIntensity);

						if (Instance.Chassis.ExhaustShakeIntensity != newExhaustShakeIntensity)
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Change Intensity");

							Instance.Chassis.ExhaustShakeIntensity = newExhaustShakeIntensity;

							EditorUtility.SetDirty(Instance);
						}
					}
					else
					{
						Vector3 newExhaustShakeIntensity = Vector3.one * ToolkitEditorUtility.NumberField(new GUIContent("Intensity", "Exhaust shaking intensity at higher power values"), Instance.Chassis.ExhaustShakeIntensity.x);

						if (Instance.Chassis.ExhaustShakeIntensity != newExhaustShakeIntensity)
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Change Intensity");

							Instance.Chassis.ExhaustShakeIntensity = newExhaustShakeIntensity;

							EditorUtility.SetDirty(Instance);
						}
					}

					EditorGUI.indentLevel--;
				}

				EditorGUILayout.EndVertical();

				EditorGUI.indentLevel--;

				if (Instance.Exhausts.Length > 0)
				{
					EditorGUI.indentLevel++;

					for (int i = 0; i < Instance.Exhausts.Length; i++)
					{
						bool remove = false;

						EditorGUILayout.BeginVertical(GUI.skin.box);
						EditorGUILayout.BeginHorizontal();

						EditorGUIUtility.labelWidth = 50f;

						EditorGUILayout.LabelField("Exhaust", EditorStyles.miniBoldLabel);

						EditorGUIUtility.labelWidth = orgLabelWidth;

						if (Instance.Exhausts[i].editorFoldout)
						{
							if (GUILayout.Button(EditorUtilities.Icons.ChevronUp, ToolkitEditorUtility.UnstretchableMiniButtonWide))
								Instance.Exhausts[i].editorFoldout = false;
						}
						else if (GUILayout.Button(EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
							EditExhaust(i);

						EditorGUI.BeginDisabledGroup(previewExhausts);

						if (GUILayout.Button(EditorUtilities.Icons.Clone, ToolkitEditorUtility.UnstretchableMiniButtonWide))
							DuplicateExhaust(i);

						if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
							remove = true;

						EditorGUI.EndDisabledGroup();
						EditorGUILayout.EndHorizontal();

						if (Instance.Exhausts[i].editorFoldout)
						{
							EditorGUI.indentLevel++;

							EditorGUILayout.Space();

							Vector3 newExhaustPosition = EditorGUILayout.Vector3Field(new GUIContent(EditorGUIUtility.IconContent("MoveTool")) { tooltip = "The exhaust position in local space" }, Instance.Exhausts[i].localPosition);

							if (Instance.Exhausts[i].localPosition != newExhaustPosition)
							{
								Undo.RegisterCompleteObjectUndo(Instance, "Move Exhaust");

								Instance.Exhausts[i].localPosition = newExhaustPosition;

								if (previewExhausts)
								{
									previewExhaustsParticleSystems[i].particleSystem.transform.localPosition = newExhaustPosition;

									for (int j = 0; j < previewExhaustsParticleSystems[i].particleSystems.Length; j++)
										previewExhaustsParticleSystems[i].particleSystems[j].transform.localPosition = newExhaustPosition;
								}

								EditorUtility.SetDirty(Instance);
							}

							Vector3 newExhaustRotation = EditorGUILayout.Vector2Field(new GUIContent(EditorGUIUtility.IconContent("RotateTool")) { tooltip = "The exhaust rotation in local space" }, Instance.Exhausts[i].localEulerAngles);

							if (Instance.Exhausts[i].localEulerAngles != newExhaustRotation)
							{
								Undo.RegisterCompleteObjectUndo(Instance, "Rotate Exhaust");

								Instance.Exhausts[i].localEulerAngles = newExhaustRotation;

								if (previewExhausts)
								{
									Quaternion targetRotation = Quaternion.Euler(Instance.Chassis.transform.TransformVector(newExhaustRotation)) * Quaternion.Euler(0f, 180f, 0f);

									previewExhaustsParticleSystems[i].particleSystem.transform.rotation = targetRotation;

									for (int j = 0; j < previewExhaustsParticleSystems[i].particleSystems.Length; j++)
										previewExhaustsParticleSystems[i].particleSystems[j].transform.rotation = targetRotation;
								}

								EditorUtility.SetDirty(Instance);
							}

							Vector2 newExhaustScale = EditorGUILayout.Vector2Field(new GUIContent(EditorGUIUtility.IconContent("ScaleTool")) { tooltip = "The exhaust scale in local space" }, Instance.Exhausts[i].LocalScale);

							if (Instance.Exhausts[i].LocalScale != newExhaustScale)
							{
								Undo.RegisterCompleteObjectUndo(Instance, "Scale Exhaust");

								Instance.Exhausts[i].LocalScale = newExhaustScale;

								if (previewExhausts)
								{
									previewExhaustsParticleSystems[i].particleSystem.transform.localScale = new(newExhaustScale.x, newExhaustScale.y, Utility.Average(newExhaustScale.x, newExhaustScale.y));

									for (int j = 0; j < previewExhaustsParticleSystems[i].particleSystems.Length; j++)
										previewExhaustsParticleSystems[i].particleSystems[j].transform.localScale = new(newExhaustScale.x, newExhaustScale.y, Utility.Average(newExhaustScale.x, newExhaustScale.y));
								}

								EditorUtility.SetDirty(Instance);
							}

							EditorGUILayout.Space();

							EditorGUI.indentLevel--;
						}

						EditorGUILayout.EndVertical();

						if (remove)
							RemoveExhaust(i);
					}

					EditorGUI.indentLevel--;
				}
				else
					EditorGUILayout.HelpBox("Use the \"+\" button in front of the 'Exhausts' label to add a new exhaust.", MessageType.Info);
			}
			else
				EditorGUILayout.HelpBox("In order to modify or add new exhausts, the vehicle has to contain a chassis component.", MessageType.Info);

			EditorGUILayout.EndVertical();
		}
		internal void BehaviourEditor()
		{
			EditorGUI.indentLevel++;

			behaviourFoldout = EnableFoldout();

			Color orgGUIBackgroundColor = GUI.backgroundColor;

			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(Application.isPlaying);

			if (!TrailerInstance)
			{
				Vehicle.VehicleType newType = (Vehicle.VehicleType)EditorGUILayout.EnumPopup("Vehicle Type", Instance.Behaviour.vehicleType);

				if (Instance.Behaviour.vehicleType != newType)
					if (!EditorApplication.isPlaying || EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The vehicle type has been changed. Do you want to restart the vehicle?", "Restart", "Cancel"))
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Vehicle Type");

						Instance.Behaviour.vehicleType = newType;

						if (EditorApplication.isPlaying)
							Instance.Restart();

						EditorUtility.SetDirty(Instance);
					}

				EditorGUI.indentLevel++;

				if (Instance.Behaviour.vehicleType == Vehicle.VehicleType.Car)
				{
					Vehicle.CarClass newCarClass = (Vehicle.CarClass)EditorGUILayout.EnumPopup("Class", Instance.Behaviour.carClass);

					if (Instance.Behaviour.carClass != newCarClass)
						if (!EditorApplication.isPlaying || EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The vehicle class has been changed. Do you want to restart the vehicle?", "Restart", "Cancel"))
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Change Vehicle Class");

							Instance.Behaviour.carClass = newCarClass;

							if (EditorApplication.isPlaying)
								Instance.Restart();

							EditorUtility.SetDirty(Instance);
						}
				}
				else if (Instance.Behaviour.vehicleType == Vehicle.VehicleType.HeavyTruck)
				{
					Vehicle.HeavyTruckClass newTruckClass = (Vehicle.HeavyTruckClass)EditorGUILayout.EnumPopup("Class", Instance.Behaviour.heavyTruckClass);

					if (Instance.Behaviour.heavyTruckClass != newTruckClass)
						if (!EditorApplication.isPlaying || EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The vehicle class has been changed. Do you want to restart the vehicle?", "Restart", "Cancel"))
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Change Vehicle Class");

							Instance.Behaviour.heavyTruckClass = newTruckClass;

							if (EditorApplication.isPlaying)
								Instance.Restart();

							EditorUtility.SetDirty(Instance);
						}
				}

				EditorGUI.indentLevel--;
			}

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			if (Settings.UseAdditionalMass)
			{
				EditorGUILayout.BeginHorizontal();

				float newChassisWeight = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Chassis Weight", "Weight of the chassis"), (TrailerInstance ? TrailerInstance.Behaviour.CurbWeight : Instance.Behaviour.CurbWeight) - Instance.AdditionalWeight, Utility.Units.Weight, 1, Instance, "Change Curb Weight"), 100f) + Instance.AdditionalWeight;

				if (!TrailerInstance && Instance.Behaviour.CurbWeight != newChassisWeight || TrailerInstance && TrailerInstance.Behaviour.CurbWeight != newChassisWeight)
				{
					if (TrailerInstance)
						TrailerInstance.Behaviour.CurbWeight = newChassisWeight;
					else
						Instance.Behaviour.CurbWeight = newChassisWeight;
				}

				EditorGUILayout.EndHorizontal();
				EditorGUI.BeginDisabledGroup(true);

				if (!TrailerInstance && Settings.useEngineMass && Instance.Engine)
				{
					EditorGUILayout.BeginHorizontal();
					ToolkitEditorUtility.NumberField(new GUIContent("Engine Weight", "Weight of the vehicle's engine"), Instance.Engine.Mass, Utility.Units.Weight, 1);
					EditorGUILayout.EndHorizontal();
				}

				if (Settings.useWheelsMass && Instance.Wheels != null && Instance.Wheels.Length > 0)
				{
					EditorGUILayout.BeginHorizontal();
					ToolkitEditorUtility.NumberField(new GUIContent("Wheels Weight", "Weight of the chassis"), Instance.WheelsWeight, Utility.Units.Weight, 1);
					EditorGUILayout.EndHorizontal();
				}

				EditorGUI.EndDisabledGroup();
			}

			float newCurbWeight = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Curb Weight", "Weight of the vehicle"), TrailerInstance ? TrailerInstance.Behaviour.CurbWeight : Instance.Behaviour.CurbWeight, Utility.Units.Weight, 1, Instance, "Change Curb Weight"), 350f);

			if (!TrailerInstance && Instance.Behaviour.CurbWeight != newCurbWeight || TrailerInstance && TrailerInstance.Behaviour.CurbWeight != newCurbWeight)
			{
				if (TrailerInstance)
					TrailerInstance.Behaviour.CurbWeight = newCurbWeight;
				else
					Instance.Behaviour.CurbWeight = newCurbWeight;
			}

			if (!TrailerInstance)
			{
				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();

				int newEnginePresetIndex = EditorGUILayout.Popup("Engine", Instance.Behaviour.EngineIndex, Settings.GetEnginesNames(false));
				bool engineUpdated = false;

				if (newEnginePresetIndex > -1 && newEnginePresetIndex < Settings.Engines.Length)
				{
					if (Instance.Behaviour.EngineIndex != newEnginePresetIndex)
					{
						if (!Settings.Engines[newEnginePresetIndex].IsValid)
						{
							if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "We couldn't change the vehicle engine. It seems like that engine is invalid!", "Fix now", "Ignore"))
								VehicleEngineEditor.OpenEngineWindow(newEnginePresetIndex);
						}
						else if (!EditorApplication.isPlaying || EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The vehicle's engine has been changed. Do you want to restart the vehicle?", "Restart", "Cancel"))
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Change Engine");

							Instance.Behaviour.EngineIndex = newEnginePresetIndex;

							if (!engineUpdated)
								engineUpdated = Instance.Behaviour.AutoCurves;

							if (EditorApplication.isPlaying)
								Instance.Restart();

							EditorUtility.SetDirty(Instance);
						}
					}

					if (GUILayout.Button(new GUIContent(EditorUtilities.Icons.Pencil, "Edit Engine Settings"), ToolkitEditorUtility.UnstretchableMiniButtonWide))
						VehicleEngineEditor.OpenEngineWindow(Instance.Behaviour.EngineIndex);
				}

				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel++;

				EditorGUI.BeginDisabledGroup(!Instance.Engine);

				float newPowerOffset = ToolkitEditorUtility.NumberField(new GUIContent("Power Offset", "The added Horse Power"), Instance.Behaviour.PowerOffset, Utility.Units.Power, 1, Instance, "Change Power");

				if (Instance.Engine)
				{
					EditorGUILayout.BeginHorizontal();

					EditorGUI.indentLevel--;

					bool newOverridePeakPowerRPM = EditorGUILayout.ToggleLeft("Override Peak Torque RPM", Instance.Behaviour.OverridePeakPowerRPM);

					if (Instance.Behaviour.OverridePeakPowerRPM != newOverridePeakPowerRPM)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Override Power Peak RPM");

						Instance.Behaviour.OverridePeakPowerRPM = newOverridePeakPowerRPM;
						engineUpdated = true;

						EditorUtility.SetDirty(Instance);
					}

					EditorGUI.indentLevel++;

					EditorGUILayout.EndHorizontal();

					if (Instance.Behaviour.OverridePeakPowerRPM)
					{
						float newPeakPowerRPMOverride = ToolkitEditorUtility.Slider(new GUIContent($"{Utility.NumberToValueWithUnit(Instance.Behaviour.Power, Utility.Units.Power, Settings.editorPowerUnit, true)} @", "The override value of the power peak RPM"), Instance.Behaviour.OverridePeakPowerRPM ? Instance.Behaviour.PeakPowerRPM : Instance.Behaviour.PeakPowerRPM, Instance.Engine.MinimumRPM, Instance.Engine.RedlineRPM, "rpm", "Revolutions per Minute", Instance, "Change Power Peak RPM");

						if (Instance.Behaviour.PeakPowerRPM != newPeakPowerRPMOverride)
						{
							Instance.Behaviour.PeakPowerRPM = newPeakPowerRPMOverride;
							engineUpdated = true;
						}
					}
					else
					{
						GUI.backgroundColor = Instance.Behaviour.PeakPowerRPM < Instance.Engine.MinimumRPM || Instance.Behaviour.PeakPowerRPM > Instance.Engine.RedlineRPM ? Color.red : orgGUIBackgroundColor;

						EditorGUILayout.HelpBox($"Power: {Utility.NumberToValueWithUnit(Instance.Behaviour.Power, Utility.Units.Power, Settings.editorPowerUnit, true)} @ {Mathf.Round(Instance.Behaviour.PeakPowerRPM):0000} rpm", MessageType.None);

						GUI.backgroundColor = orgGUIBackgroundColor;
					}
				}

				EditorGUILayout.Space();

				float newTorqueOffset = ToolkitEditorUtility.NumberField(new GUIContent("Torque Offset", "The added Engine Torque"), Instance.Behaviour.TorqueOffset, Utility.Units.Torque, 1, Instance, "Change Torque");

				if (Instance.Engine)
				{
					EditorGUILayout.BeginHorizontal();

					EditorGUI.indentLevel--;

					bool newOverridePeakTorqueRPM = EditorGUILayout.ToggleLeft("Override Peak Torque RPM", Instance.Behaviour.OverridePeakTorqueRPM);

					if (Instance.Behaviour.OverridePeakTorqueRPM != newOverridePeakTorqueRPM)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Override Torque Peak RPM");

						Instance.Behaviour.OverridePeakTorqueRPM = newOverridePeakTorqueRPM;
						engineUpdated = true;

						EditorUtility.SetDirty(Instance);
					}

					EditorGUI.indentLevel++;

					EditorGUILayout.EndHorizontal();

					if (Instance.Behaviour.OverridePeakTorqueRPM)
					{
						float newPeakTorqueRPMOverride = ToolkitEditorUtility.Slider(new GUIContent($"{Utility.NumberToValueWithUnit(Instance.Behaviour.Torque, Utility.Units.Torque, Settings.editorTorqueUnit, true)} @", "The override value of the torque peak RPM"), Instance.Behaviour.OverridePeakTorqueRPM ? Instance.Behaviour.PeakTorqueRPM : Instance.Behaviour.PeakPowerRPM, Instance.Engine.MinimumRPM, Instance.Engine.RedlineRPM, "rpm", "Revolutions per Minute", Instance, "Change Torque Peak RPM");

						if (Instance.Behaviour.PeakTorqueRPM != newPeakTorqueRPMOverride)
						{
							Instance.Behaviour.PeakTorqueRPM = newPeakTorqueRPMOverride;
							engineUpdated = true;
						}
					}
					else
					{
						GUI.backgroundColor = Instance.Behaviour.PeakTorqueRPM < Instance.Engine.MinimumRPM || Instance.Behaviour.PeakTorqueRPM > Instance.Engine.RedlineRPM ? Color.red : orgGUIBackgroundColor;

						EditorGUILayout.HelpBox($"Torque: {Utility.NumberToValueWithUnit(Instance.Behaviour.Torque, Utility.Units.Torque, Settings.editorTorqueUnit, true)} @ {Mathf.Round(Instance.Behaviour.PeakTorqueRPM):0000} rpm", MessageType.None);

						GUI.backgroundColor = orgGUIBackgroundColor;
					}
				}

				if (newPowerOffset != Instance.Behaviour.PowerOffset)
				{
					Instance.Behaviour.PowerOffset = newPowerOffset;

					if (!engineUpdated)
						engineUpdated = Instance.Behaviour.AutoCurves;
				}

				if (newTorqueOffset != Instance.Behaviour.TorqueOffset)
				{
					Instance.Behaviour.TorqueOffset = newTorqueOffset;

					if (!engineUpdated)
						engineUpdated = Instance.Behaviour.AutoCurves;
				}

				EditorGUILayout.Space();

				float newPowerTorqueOutputScale = ToolkitEditorUtility.Slider(new GUIContent("Output Scale", "The engine power and torque output scale"), Instance.Behaviour.TorqueOutputScale, 0f, 5f, Instance, "Change Output Scale");

				if (Instance.Behaviour.TorqueOutputScale != newPowerTorqueOutputScale)
					Instance.Behaviour.TorqueOutputScale = newPowerTorqueOutputScale;

				/*if (Instance.Drivetrain == Vehicle.Train.AWD)
				{
					float frontOutputFactor = 1f - newFrontRearOutputFactor;
					float rearOutputFactor = newFrontRearOutputFactor;

					EditorGUILayout.HelpBox($"Front: {Mathf.Round(frontOutputFactor * 100f)}% ({Mathf.Round(Instance.Behaviour.Power * newPowerTorqueOutputScale * Utility.UnitMultiplier(Utility.Units.Power, Settings.editorPowerUnit) * frontOutputFactor * 10f) * .1f} {Utility.Unit(Utility.Units.Power, Settings.editorPowerUnit)} | {Mathf.Round(Instance.Behaviour.Torque * newPowerTorqueOutputScale * Utility.UnitMultiplier(Utility.Units.Torque, Settings.editorTorqueUnit) * frontOutputFactor * 10f) * .1f} {Utility.Unit(Utility.Units.Torque, Settings.editorTorqueUnit)})\r\nRear: {Mathf.Round(rearOutputFactor * 100f)}% ({Mathf.Round(Instance.Behaviour.Power * newPowerTorqueOutputScale * Utility.UnitMultiplier(Utility.Units.Power, Settings.editorPowerUnit) * rearOutputFactor * 10f) * .1f} {Utility.Unit(Utility.Units.Power, Settings.editorPowerUnit)} | {Mathf.Round(Instance.Behaviour.Torque * newPowerTorqueOutputScale * Utility.UnitMultiplier(Utility.Units.Torque, Settings.editorTorqueUnit) * rearOutputFactor * 10f) * .1f} {Utility.Unit(Utility.Units.Torque, Settings.editorTorqueUnit)})", MessageType.None);
				}
				else*/
					EditorGUILayout.HelpBox($"Output: {Mathf.Round(Instance.Behaviour.Power * newPowerTorqueOutputScale * Utility.UnitMultiplier(Utility.Units.Power, Settings.editorPowerUnit) * 10f) * .1f} {Utility.Unit(Utility.Units.Power, Settings.editorPowerUnit)} | {Mathf.Round(Instance.Behaviour.Torque * newPowerTorqueOutputScale * Utility.UnitMultiplier(Utility.Units.Torque, Settings.editorTorqueUnit) * 10f) * .1f} {Utility.Unit(Utility.Units.Torque, Settings.editorTorqueUnit)}", MessageType.None);

				if (Instance.Engine)
				{
					EditorGUILayout.Space();
					EditorGUILayout.BeginHorizontal();
					EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

					bool newAutoCurves = EditorGUILayout.Popup(new GUIContent("Curves", "The vehicle torque and power curves"), Instance.Behaviour.AutoCurves ? 0 : 1, new string[] { "Automatic", "Manual" }) < 1;

					if (newAutoCurves != Instance.Behaviour.AutoCurves)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Curves");

						Instance.Behaviour.AutoCurves = newAutoCurves;

						if (!engineUpdated)
							engineUpdated = newAutoCurves;

						EditorUtility.SetDirty(Instance);
					}

					EditorGUI.BeginDisabledGroup(Instance.Problems.HasBehaviourWarnings || Instance.Problems.HasBehaviourErrors);

					bool regenerateCurves = false;

					if (GUILayout.Button(EditorUtilities.Icons.Reload, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Regenerate Curves");

						engineUpdated = true;
						regenerateCurves = true;

						EditorUtility.SetDirty(Instance);
					}

					EditorGUI.EndDisabledGroup();
					EditorGUI.EndDisabledGroup();
					EditorGUILayout.EndHorizontal();

					EditorGUI.indentLevel++;

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.BeginVertical();
					EditorGUI.BeginDisabledGroup(Application.isPlaying || Instance.Behaviour.AutoCurves);

					float curvePowerMaxRPM = Instance.Behaviour.PowerCurve.keys.Length > 0 ? Instance.Behaviour.PowerCurve.keys[^1].time : Instance.Engine.MaximumRPM;
					float curveTorqueMaxRPM = Instance.Behaviour.TorqueCurve.keys.Length > 0 ? Instance.Behaviour.TorqueCurve.keys[^1].time : Instance.Engine.MaximumRPM;
					float curvesMaxRPM = Mathf.Max(curvePowerMaxRPM, curveTorqueMaxRPM);
					float curveMaxTorque = Instance.Behaviour.TorqueCurve.keys.Length > 0 ? Instance.Behaviour.TorqueCurve.keys.Max(key => key.value) : Instance.Behaviour.Torque;
					float curveMaxPower = Instance.Behaviour.PowerCurve.keys.Length > 0 ? Instance.Behaviour.PowerCurve.keys.Max(key => key.value) : Instance.Behaviour.Power;

					AnimationCurve newPowerCurve = EditorGUILayout.CurveField("Power", Instance.Behaviour.PowerCurve, Color.blue, new(0f, 0f, curvesMaxRPM, curveMaxPower));

					if (Instance.Behaviour.PowerCurve != newPowerCurve)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Power Curve");

						Instance.Behaviour.PowerCurve = newPowerCurve;

						EditorUtility.SetDirty(Instance);
					}

					AnimationCurve newTorqueCurve = EditorGUILayout.CurveField("Torque", Instance.Behaviour.TorqueCurve, Color.red, new(0f, 0f, curvesMaxRPM, curveMaxTorque));

					if (Instance.Behaviour.TorqueCurve != newTorqueCurve)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Torque Curve");

						Instance.Behaviour.TorqueCurve = newTorqueCurve;

						EditorUtility.SetDirty(Instance);
					}

					EditorGUI.EndDisabledGroup();
					EditorGUILayout.EndVertical();
					EditorGUILayout.BeginVertical();

					if (GUILayout.Button(EditorGUIUtility.IconContent(Instance.Behaviour.LinkPowerTorqueCurves ? "d_Linked" : "d_Unlinked").image, GUILayout.Width(30f), GUILayout.Height(40f)))
					{
						Undo.RegisterCompleteObjectUndo(Instance, !Instance.Behaviour.LinkPowerTorqueCurves ? "Link Curves" : "Unlink Curves");

						Instance.Behaviour.LinkPowerTorqueCurves = !Instance.Behaviour.LinkPowerTorqueCurves;
						engineUpdated = true;

						EditorUtility.SetDirty(Instance);
					}

					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();

					EditorGUI.indentLevel--;

					if (!Instance.Problems.HasBehaviourErrors)
					{
						if (Instance.Behaviour.PowerCurve.length < 2)
						{
							Instance.Behaviour.RegeneratePowerCurve();

							if (!Instance.IsElectric)
								VehicleEngineEditor.SmoothOutPowerCurve(Instance);
						}

						if (Instance.Behaviour.TorqueCurve.length < 2)
						{
							Instance.Behaviour.RegenerateTorqueCurve(Instance.Behaviour.LinkPowerTorqueCurves);

							if (!Instance.IsElectric)
								VehicleEngineEditor.SmoothOutTorqueCurve(Instance);
						}
					}

					if (engineUpdated)
					{
						if (!Instance.Problems.HasBehaviourErrors && (Instance.Behaviour.AutoCurves || Application.isPlaying || regenerateCurves))
						{
							Instance.Behaviour.RegenerateCurves();

							if (!Instance.IsElectric)
							{
								VehicleEngineEditor.SmoothOutPowerCurve(Instance);
								VehicleEngineEditor.SmoothOutTorqueCurve(Instance);
							}
						}

						Repaint();
					}
				}

				EditorGUILayout.Space();
				EditorGUI.BeginDisabledGroup(!Instance.Transmission.AutoGearRatios);

				if (Instance.Transmission.AutoGearRatios)
				{
					float newTopSpeed = math.abs(ToolkitEditorUtility.NumberField(new GUIContent("Top Speed", "The maximum speed the vehicle can reach"), Instance.Behaviour.TopSpeed, Utility.Units.Speed, 1, Instance, "Change Top Speed"));

					if (newTopSpeed != Instance.Behaviour.TopSpeed)
					{
						Instance.Behaviour.TopSpeed = newTopSpeed;

						Instance.Transmission.RefreshGears();
					}
				}
				else
				{
					EditorGUILayout.BeginHorizontal();
					ToolkitEditorUtility.NumberField(new GUIContent("Top Speed", "The maximum speed the vehicle can reach"), Instance.TopSpeed, Utility.Units.Speed, 1);
					EditorGUI.EndDisabledGroup();

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						transmissionFoldout = EnableFoldout();

					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.EndHorizontal();
				}

				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();

				EditorGUI.indentLevel--;

				if (Instance.Engine && Instance.Transmission.AutoGearRatios)
					EditorGUILayout.HelpBox("Be careful! Updating the Vehicle Top Speed overrides the Gear Speed Targets values even at manual mode. Although, you can undo that if something unexpected happens.", MessageType.None);

				EditorGUILayout.Space();

				if (!Instance.IsElectric)
				{
					EditorGUILayout.BeginHorizontal();

					Vehicle.Aspiration newAspiration = (Vehicle.Aspiration)EditorGUILayout.EnumPopup("Aspiration", Instance.Behaviour.Aspiration);

					if (Instance.Behaviour.Aspiration != newAspiration)
						if (!EditorApplication.isPlaying || EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The vehicle's aspiration type has been changed. Do you want to restart the vehicle?", "Restart", "Cancel"))
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Change Aspiration");

							Instance.Behaviour.Aspiration = newAspiration;

							if (EditorApplication.isPlaying)
								Instance.Restart();

							EditorUtility.SetDirty(Instance);
						}

					EditorGUILayout.EndHorizontal();

					EditorGUI.indentLevel++;

					if (Instance.Behaviour.IsTurbocharged)
					{
						int newTurbochargerIndex = EditorGUILayout.Popup("Turbocharger", Instance.Behaviour.TurbochargerIndex, Settings.GetChargersNames(false));

						if (Instance.Behaviour.TurbochargerIndex != newTurbochargerIndex)
							if (!EditorApplication.isPlaying || EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The vehicle's turbocharger type has been changed. Do you want to restart the vehicle?", "Restart", "Cancel"))
							{
								Undo.RegisterCompleteObjectUndo(Instance, "Change Turbocharger");

								Instance.Behaviour.TurbochargerIndex = newTurbochargerIndex;

								if (EditorApplication.isPlaying)
									Instance.Restart();

								EditorUtility.SetDirty(Instance);
							}
					}

					if (Instance.Behaviour.IsSupercharged)
					{
						int newSuperchargerIndex = EditorGUILayout.Popup("Supercharger", Instance.Behaviour.SuperchargerIndex, Settings.GetChargersNames(false));

						if (Instance.Behaviour.SuperchargerIndex != newSuperchargerIndex)
							if (!EditorApplication.isPlaying || EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The vehicle's super charger type has been changed. Do you want to restart the vehicle?", "Restart", "Cancel"))
							{
								Undo.RegisterCompleteObjectUndo(Instance, "Change Supercharger");

								Instance.Behaviour.SuperchargerIndex = newSuperchargerIndex;

								if (EditorApplication.isPlaying)
									Instance.Restart();

								EditorUtility.SetDirty(Instance);
							}
					}

					if (newAspiration != Vehicle.Aspiration.Natural && !Instance.Problems.HasComponentsWarnings && !Instance.Problems.HasComponentsErrors && !Instance.Problems.HasBehaviourWarnings && !Instance.Problems.HasBehaviourErrors)
					{
						float maximumBoost = 1f;
						bool isStockBoost = true;

						if (Instance.Behaviour.IsTurbocharged && Instance.Turbocharger && Instance.Turbocharger.IsValid && Instance.Turbocharger.IsCompatible)
						{
							maximumBoost *= Instance.Turbocharger.IsStock ? 1f : Instance.Turbocharger.MaximumBoost;
							isStockBoost &= Instance.Turbocharger.IsStock;
						}

						if (Instance.Behaviour.IsSupercharged && Instance.Supercharger && Instance.Supercharger.IsValid && Instance.Supercharger.IsCompatible)
						{
							maximumBoost *= Instance.Supercharger.IsStock ? 1f : Instance.Supercharger.MaximumBoost;
							isStockBoost &= Instance.Supercharger.IsStock;
						}

						EditorGUILayout.HelpBox($"Output Power: {Utility.NumberToValueWithUnit(Instance.Behaviour.Power * maximumBoost * Instance.Behaviour.TorqueOutputScale, Utility.Units.Power, Settings.editorPowerUnit, 1)}{(isStockBoost ? " (Stock)" : "")}", MessageType.None);
						EditorGUILayout.HelpBox($"Output Torque: {Utility.NumberToValueWithUnit(Instance.Behaviour.Torque * maximumBoost * Instance.Behaviour.TorqueOutputScale, Utility.Units.Torque, Settings.editorTorqueUnit, 1)}{(isStockBoost ? " (Stock)" : "")}", MessageType.None);
					}

					EditorGUI.indentLevel--;
				}

				EditorGUILayout.Space();
#if !MVC_COMMUNITY
				if (Settings.useFuelSystem)
				{
					EditorGUILayout.LabelField("Fuel System");

					EditorGUI.indentLevel++;

					float newFuelCapacity = ToolkitEditorUtility.NumberField(new GUIContent(Instance.IsElectric ? "Battery Capacity" : "Fuel Capacity", Instance.IsElectric ? "The electric batteries capacity" : "The fuel tank capacity"), Instance.Behaviour.FuelCapacity, Instance.IsElectric ? Utility.Units.ElectricCapacity : Utility.Units.Liquid, 2, Instance, "Change Capacity");

					if (Instance.Behaviour.FuelCapacity != newFuelCapacity)
						Instance.Behaviour.FuelCapacity = newFuelCapacity;

					if (Instance.IsElectric)
					{
						bool newUseBatteryRegenerator = ToolkitEditorUtility.ToggleButtons(new GUIContent("Regenerative Brakes", "Turning this On, will add power back to the vehicle batteries while braking"), null, "On", "Off", Instance.Behaviour.UseRegenerativeBrakes, Instance, "Switch Regenerator");

						if (Instance.Behaviour.UseRegenerativeBrakes != newUseBatteryRegenerator)
							Instance.Behaviour.UseRegenerativeBrakes = newUseBatteryRegenerator;
					}

					Utility.Precision newFuelConsumptionPrecision = (Utility.Precision)EditorGUILayout.EnumPopup(new GUIContent("Consumption", Instance.IsElectric ? "The electric batteries consumption precision" : "The fuel consumption precision"), Instance.Behaviour.fuelConsumptionPrecision);

					if (Instance.Behaviour.fuelConsumptionPrecision != newFuelConsumptionPrecision)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Precision");

						Instance.Behaviour.fuelConsumptionPrecision = newFuelConsumptionPrecision;

						EditorUtility.SetDirty(Instance);
					}

					EditorGUI.indentLevel++;

					switch (Instance.Behaviour.fuelConsumptionPrecision)
					{
						case Utility.Precision.Advanced:
							float newFuelConsumptionCity = ToolkitEditorUtility.NumberField(new GUIContent("City", Instance.IsElectric ? "The electric batteries consumption in a City environment" : "The fuel tank consumption in a City environment"), Instance.Behaviour.FuelConsumptionCity, Instance.IsElectric ? Utility.Units.ElectricConsumption : Utility.Units.FuelConsumption, 2, Instance, "Change Consumption");

							if (Instance.Behaviour.FuelConsumptionCity != newFuelConsumptionCity)
								Instance.Behaviour.FuelConsumptionCity = newFuelConsumptionCity;

							float newFuelConsumptionHighway = ToolkitEditorUtility.NumberField(new GUIContent("Highway", Instance.IsElectric ? "The electric batteries consumption in a Highway environment" : "The fuel tank consumption in a Highway environment"), Instance.Behaviour.FuelConsumptionHighway, Instance.IsElectric ? Utility.Units.ElectricConsumption : Utility.Units.FuelConsumption, 2, Instance, "Change Consumption");

							if (Instance.Behaviour.FuelConsumptionHighway != newFuelConsumptionHighway)
								Instance.Behaviour.FuelConsumptionHighway = newFuelConsumptionHighway;

							break;

						default:
							float newFuelConsumptionCombined = ToolkitEditorUtility.NumberField(new GUIContent("Value", Instance.IsElectric ? "The electric batteries combined consumption (City & Highway)" : "The fuel tank combined consumption (City & Highway)"), Instance.Behaviour.FuelConsumptionCombined, Instance.IsElectric ? Utility.Units.ElectricConsumption : Utility.Units.FuelConsumption, 2, Instance, "Change Consumption");

							if (Instance.Behaviour.FuelConsumptionCombined != newFuelConsumptionCombined)
								Instance.Behaviour.FuelConsumptionCombined = newFuelConsumptionCombined;

							break;
					}

					EditorGUI.indentLevel--;
					EditorGUI.indentLevel--;

					EditorGUILayout.Space();
				}
#endif

				bool newUseRevLimiter = ToolkitEditorUtility.ToggleButtons(new GUIContent("Rev Limiter", "The engine RPM Limiter"), null, "On", "Off", Instance.Behaviour.useRevLimiter, Instance, "Switch Limiter");

				if (Instance.Behaviour.useRevLimiter != newUseRevLimiter)
					Instance.Behaviour.useRevLimiter = newUseRevLimiter;

				if (!Instance.IsElectric)
				{
					EditorGUILayout.BeginHorizontal();

					bool newUseExhaustEffects = ToolkitEditorUtility.ToggleButtons(new GUIContent("Exhaust Effects", "Should vehicles emit exhaust effects such as smoke and flames?"), null, "On", "Off", Instance.Behaviour.useExhaustEffects, Instance, "Switch Effects");

					if (Instance.Behaviour.useExhaustEffects != newUseExhaustEffects)
						Instance.Behaviour.useExhaustEffects = newUseExhaustEffects;

					EditorGUILayout.EndHorizontal();

					if (Instance.Behaviour.useExhaustEffects)
					{
						EditorGUI.indentLevel++;

						float newExhaustFlameEmissionProbability = ToolkitEditorUtility.Slider(new GUIContent("Emission Prob.", "The exhaust effects emission probability"), Instance.Behaviour.ExhaustFlameEmissionProbability, 0f, 1f, Instance, "Change Probability");

						if (Instance.Behaviour.ExhaustFlameEmissionProbability != newExhaustFlameEmissionProbability)
							Instance.Behaviour.ExhaustFlameEmissionProbability = newExhaustFlameEmissionProbability;

						EditorGUI.indentLevel--;
					}

					if (Settings.useNOS)
					{
						EditorGUILayout.Space();
						EditorGUILayout.BeginHorizontal();

						bool newUseNOS = ToolkitEditorUtility.ToggleButtons(new GUIContent("NOS", "Switch On/Off Nitrous"), null, "On", "Off", Instance.Behaviour.useNOS, Instance, "Switch NOS");

						if (Instance.Behaviour.useNOS != newUseNOS)
							Instance.Behaviour.useNOS = newUseNOS;

						EditorGUILayout.EndHorizontal();
						EditorGUI.BeginDisabledGroup(!Instance.Behaviour.useNOS);

						EditorGUI.indentLevel++;

						int newNOSBottlesCount = ToolkitEditorUtility.Slider(new GUIContent("Bottle Count", "The number of NOS bottles"), Instance.Behaviour.NOSBottlesCount, 1, 10, Instance, "Change NOS Count");

						if (Instance.Behaviour.NOSBottlesCount != newNOSBottlesCount)
							Instance.Behaviour.NOSBottlesCount = newNOSBottlesCount;

						float newNOSCapacity = ToolkitEditorUtility.NumberField(new GUIContent("Capacity", "NOS Capacity per bottle"), Instance.Behaviour.NOSCapacity, Utility.Units.Weight, 1, Instance, "Change NOS Capacity");

						if (Instance.Behaviour.NOSCapacity != newNOSCapacity)
							Instance.Behaviour.NOSCapacity = newNOSCapacity;

						float newNOSBoost = ToolkitEditorUtility.Slider(new GUIContent("Boost Rate", "NOS torque boost multiplier"), Instance.Behaviour.NOSBoost, 0f, 1f, Instance, "Change NOS Boost");

						if (Instance.Behaviour.NOSBoost != newNOSBoost)
							Instance.Behaviour.NOSBoost = newNOSBoost;

						string NOSConsumptionUnit = $"{Utility.Unit(Utility.Units.Weight, Settings.editorValuesUnit)}/{Utility.Unit(Utility.Units.Time, Settings.editorValuesUnit)}";
						string NOSConsumptionFullUnit = $"{Utility.FullUnit(Utility.Units.Weight, Settings.editorValuesUnit)}s Per {Utility.FullUnit(Utility.Units.Time, Settings.editorValuesUnit)}";
						float newNOSConsumption = ToolkitEditorUtility.NumberField(new GUIContent("Consumption", "NOS consumption rate"), Instance.Behaviour.NOSConsumption, NOSConsumptionUnit, NOSConsumptionFullUnit, 2, Instance, "Change NOS Consumption");

						if (Instance.Behaviour.NOSConsumption != newNOSConsumption)
							Instance.Behaviour.NOSConsumption = newNOSConsumption;

						float newNOSRegenerateTime = ToolkitEditorUtility.NumberField(new GUIContent("Regeneration Delay", "NOS Regeneration delay after last use"), Instance.Behaviour.NOSRegenerateTime, Utility.Units.Time, 2, Instance, "Change NOS Delay");

						if (Instance.Behaviour.NOSRegenerateTime != newNOSRegenerateTime)
							Instance.Behaviour.NOSRegenerateTime = newNOSRegenerateTime;

						EditorGUI.indentLevel--;

						EditorGUI.EndDisabledGroup();
					}
				}
			}

			EditorGUILayout.Space();

			EditorGUI.indentLevel--;
		}
		internal void TransmissionEditor()
		{
			EditorGUI.indentLevel++;

			transmissionFoldout = EnableFoldout();

			EditorGUILayout.Space();

			Vehicle.TransmissionModule.GearboxType newShiftType = (Vehicle.TransmissionModule.GearboxType)EditorGUILayout.EnumPopup(new GUIContent("Gearbox Type", "The gearbox type may impact the vehicle performance"), Instance.Transmission.Gearbox);

			if (Instance.Transmission.Gearbox != newShiftType)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Shift Type");

				Instance.Transmission.Gearbox = newShiftType;

				EditorUtility.SetDirty(Instance);
			}

			if (Instance.Drivetrain == Vehicle.Train.AWD && Instance.Transmission.Gearbox != Vehicle.TransmissionModule.GearboxType.Manual)
			{
				bool newUseDoubleGearbox = EditorGUILayout.Popup(new GUIContent("Count", "The number of gearboxes the vehicle has. This feature is only available for AWD vehicles"), Instance.Transmission.UseDoubleGearbox ? 1 : 0, new string[] { "Single", "Double" }) > 0;

				if (Instance.Transmission.UseDoubleGearbox != newUseDoubleGearbox)
				{
					if (!EditorApplication.isPlaying || EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The vehicle gearboxes count has been changed. Do you want to restart the vehicle?", "Restart", "Cancel"))
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Count");

						Instance.Transmission.UseDoubleGearbox = newUseDoubleGearbox;

						EditorUtility.SetDirty(Instance);
					}
				}
			}

			List<string> clipGroupNames = new()
			{
				"None"
			};

			for (int i = 1; i <= Settings.transmissionWhineGroups.Length; i++)
				clipGroupNames.Add($"{i}. {Settings.transmissionWhineGroups[i - 1].name}");

			int newClipsGroupIndex = EditorGUILayout.Popup(new GUIContent("Audio Group", "The transmission whining clips group"), Instance.Transmission.ClipsGroupIndex + 1, clipGroupNames.ToArray()) - 1;

			if (Instance.Transmission.ClipsGroupIndex != newClipsGroupIndex)
				if (!EditorApplication.isPlaying || EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The vehicle's transmission audio group has been changed. Do you want to restart the vehicle?", "Restart", "Cancel"))
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Group");

					Instance.Transmission.ClipsGroupIndex = newClipsGroupIndex;

					if (EditorApplication.isPlaying)
						Instance.Restart();

					EditorUtility.SetDirty(Instance);
				}

			if (Instance.Transmission.ClipsGroupIndex > -1)
			{
				int newPlayClipsStartingFromGear = ToolkitEditorUtility.Slider(new GUIContent("Play Clips at Gear", "The vehicle transmission clips would start playing only when the current gear is greater or equal than this value"), Instance.Transmission.PlayClipsStartingFromGear, 1, Instance.Transmission.GearsCount);

				if (Instance.Transmission.PlayClipsStartingFromGear != newPlayClipsStartingFromGear)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Gear");

					Instance.Transmission.PlayClipsStartingFromGear = newPlayClipsStartingFromGear;

					EditorUtility.SetDirty(Instance);
				}
			}

			float newMaximumTorque = ToolkitEditorUtility.NumberField(new GUIContent("Maximum Torque", "The transmission clutch maximum torque capacity"), Instance.Transmission.MaximumTorque, Utility.Units.Torque, 1, Instance, "Change Torque");

			if (Instance.Transmission.MaximumTorque != newMaximumTorque)
				Instance.Transmission.MaximumTorque = newMaximumTorque;

			EditorGUILayout.HelpBox($"Efficiency: {Utility.Round(Instance.Transmission.Efficiency * 100f, 1)}%", MessageType.None);

			float newShiftDelay = ToolkitEditorUtility.NumberField(new GUIContent("Shift Delay", "The gearbox shift delay"), Instance.Transmission.ShiftDelay * 1000f, Utility.Units.TimeAccurate, true, Instance, "Change Delay") * .001f;

			if (Instance.Transmission.ShiftDelay != newShiftDelay)
				Instance.Transmission.ShiftDelay = newShiftDelay;

			float newClutchInDelay = ToolkitEditorUtility.NumberField(new GUIContent("Clutch In", "The gearbox clutch in delay"), Instance.Transmission.ClutchInDelay * 1000f, Utility.Units.TimeAccurate, true, Instance, "Change Delay") * .001f;

			if (Instance.Transmission.ClutchInDelay != newClutchInDelay)
				Instance.Transmission.ClutchInDelay = newClutchInDelay;

			float newClutchOutDelay = ToolkitEditorUtility.NumberField(new GUIContent("Clutch Out", "The gearbox clutch out delay"), Instance.Transmission.ClutchOutDelay * 1000f, Utility.Units.TimeAccurate, true, Instance, "Change Delay") * .001f;

			if (Instance.Transmission.ClutchOutDelay != newClutchOutDelay)
				Instance.Transmission.ClutchOutDelay = newClutchOutDelay;

			int newGearCount = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Gears Count"), Instance.Transmission.GearsCount, Instance, "Change Gears"), 1);

			if (Instance.Transmission.GearsCount != newGearCount)
			{
				Instance.Transmission.GearsCount = newGearCount;

				Instance.Transmission.RefreshGears();
				Repaint();
			}

			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup(Instance.Transmission.UseDoubleGearbox);

			bool newAutoGearRatios = ToolkitEditorUtility.ToggleButtons(new GUIContent("Gear Ratios", "The Gearbox ratio scales"), null, new GUIContent("A", "Auto"), new GUIContent("M", "Manual"), Instance.Transmission.AutoGearRatios, Instance, "Switch Auto Gears");

			if (Instance.Transmission.AutoGearRatios != newAutoGearRatios)
				Instance.Transmission.AutoGearRatios = newAutoGearRatios;

			EditorGUI.EndDisabledGroup();

			if (GUILayout.Button(new GUIContent("R", "Reset"), EditorStyles.miniButton))
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Reset Gears");
				Instance.Transmission.ResetGears();
				EditorUtility.SetDirty(Instance);
			}

			EditorGUILayout.EndHorizontal();

			if (!Instance.Problems.HasComponentsErrors && !Instance.Problems.HasComponentsWarnings)
			{
				EditorGUI.indentLevel++;

				if (Instance.Transmission.AutoGearRatios)
				{
					EditorGUILayout.BeginHorizontal();

					float oldReverseSpeedTarget = Instance.Transmission.GetSpeedTarget(-1, 0);
					float newReverseSpeedTarget = ToolkitEditorUtility.NumberField(new GUIContent("Reverse Gear", "The reverse top speed"), oldReverseSpeedTarget, Utility.Units.Speed, 1, Instance, "Change Speed");

					if (oldReverseSpeedTarget != newReverseSpeedTarget)
						Instance.Transmission.SetSpeedTarget(-1, 0, newReverseSpeedTarget);

					float orgLabelWidth = EditorGUIUtility.labelWidth;

					EditorGUIUtility.labelWidth = 1f;
					EditorGUI.indentLevel -= 2;

					EditorGUILayout.LabelField($"{Utility.Round(Instance.Transmission.GetGearRatio(-1, 0), 3):0.000}:1", EditorStyles.miniBoldLabel);

					EditorGUI.indentLevel += 2;
					EditorGUIUtility.labelWidth = orgLabelWidth;

					EditorGUILayout.EndHorizontal();

					for (int i = 0; i < Instance.Transmission.GearsCount; i++)
					{
						EditorGUI.BeginDisabledGroup(i >= Instance.Transmission.GearsCount - 1);
						EditorGUILayout.BeginHorizontal();

						float oldSpeedTarget = Instance.Transmission.GetSpeedTarget(0, i);
						float newSpeedTarget = ToolkitEditorUtility.NumberField(new GUIContent($"{Utility.ClassifyNumber(i + 1)} Gear", $"The {Utility.ClassifyNumber(i + 1)} gear top speed"), oldSpeedTarget, Utility.Units.Speed, 1, Instance, "Change Speed");

						if (oldSpeedTarget != newSpeedTarget)
							Instance.Transmission.SetSpeedTarget(0, i, newSpeedTarget);

						EditorGUIUtility.labelWidth = 1f;
						EditorGUI.indentLevel -= 2;

						EditorGUILayout.LabelField($"{Utility.Round(Instance.Transmission.GetGearRatio(0, i), 3):0.000}:1", EditorStyles.miniBoldLabel);

						EditorGUI.indentLevel += 2;
						EditorGUIUtility.labelWidth = orgLabelWidth;

						EditorGUILayout.EndHorizontal();
						EditorGUI.EndDisabledGroup();
					}
				}
				else
				{
					float oldReverseGearRation = Instance.Transmission.GetGearRatio(-1, 0);
					float newReverseGearRatio = ToolkitEditorUtility.Slider("Reverse Gear", Utility.Round(oldReverseGearRation, 3), Instance.Transmission.GetGearRatio(0, Instance.Transmission.GearsCount - 1), 20f, Instance, "Change Ratio");

					if (oldReverseGearRation != newReverseGearRatio)
						Instance.Transmission.SetGearRatio(-1, 0, newReverseGearRatio);

					if (Instance.Transmission.UseDoubleGearbox)
					{
						float oldReverseGearRatio2 = Instance.Transmission.GetGearRatio(-1, 0, false, true);
						float newReverseGearRatio2 = ToolkitEditorUtility.Slider(" ", Utility.Round(oldReverseGearRatio2, 3), Instance.Transmission.GetGearRatio(0, Instance.Transmission.GearsCount - 1, false, true), 20f, Instance, "Change Ratio");

						if (oldReverseGearRatio2 != newReverseGearRatio2)
							Instance.Transmission.SetGearRatio(-1, 0, newReverseGearRatio2, true);

						EditorGUILayout.LabelField(" ", Utility.NumberToValueWithUnit(Instance.Transmission.GetSpeedTarget(-1, 0), Utility.Units.Speed, Settings.editorValuesUnit, 1), EditorStyles.miniBoldLabel);
					}
					else
					{
						EditorGUI.indentLevel -= 2;

						EditorGUILayout.BeginHorizontal();
						GUILayout.Space(EditorGUIUtility.labelWidth);
						EditorGUILayout.LabelField(Utility.NumberToValueWithUnit(Instance.Transmission.GetSpeedTarget(-1, 0), Utility.Units.Speed, Settings.editorValuesUnit, 1), EditorStyles.miniBoldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));
						EditorGUILayout.EndHorizontal();

						EditorGUI.indentLevel += 2;
					}

					for (int i = 0; i < Instance.Transmission.GearsCount; i++)
					{
						float minimumGearRatio = i < Instance.Transmission.GearsCount - 1 ? (Instance.Transmission.UseDoubleGearbox ? Mathf.Max(Instance.Transmission.GetGearRatio(0, i + 1), Instance.Transmission.GetGearRatio(0, i + 1, false, true)) : Instance.Transmission.GetGearRatio(0, i + 1)) : .1f;
						float maximumGearRatio = i == 0 ? 20f : Instance.Transmission.UseDoubleGearbox ? Mathf.Min(Instance.Transmission.GetGearRatio(0, i - 1), Instance.Transmission.GetGearRatio(0, i - 1, false, true)) : Instance.Transmission.GetGearRatio(0, i - 1);
						float oldRatio = Instance.Transmission.GetGearRatio(0, i);
						float newRatio = ToolkitEditorUtility.Slider($"{Utility.ClassifyNumber(i + 1)} Gear", Utility.Round(oldRatio, 3), minimumGearRatio, maximumGearRatio);

						if (oldRatio != newRatio)
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Change Ratio");
							Instance.Transmission.SetGearRatio(0, i, newRatio);
							EditorUtility.SetDirty(Instance);
						}

						GUIContent gearShiftSpeedLabel = new(Utility.NumberToValueWithUnit(Instance.Transmission.GetSpeedTarget(0, i), Utility.Units.Speed, Settings.editorValuesUnit, 1), "Default gear shifting speed");
						GUIContent overrideLabel = new("Override Speed?", "Should the default gear shifting speed be overridden?");
						float overrideLabelWidth = EditorStyles.miniLabel.CalcSize(overrideLabel).x + EditorGUI.indentLevel * EditorGUIUtility.singleLineHeight - 5f;
						bool gearShiftOverrideSpeed = Instance.Transmission.GetGearShiftOverrideSpeed(i, out float overrideSpeed);

						if (gearShiftOverrideSpeed)
							gearShiftSpeedLabel.text = string.Empty;

						gearShiftSpeedLabel.tooltip += $" ({gearShiftSpeedLabel.text})";
						overrideSpeed = Utility.Round(overrideSpeed, 1);

						if (Instance.Transmission.UseDoubleGearbox)
						{
							float oldRatio2 = Instance.Transmission.GetGearRatio(0, i, false, true);
							float newRatio2 = ToolkitEditorUtility.Slider(" ", Utility.Round(oldRatio2, 3), minimumGearRatio, maximumGearRatio);

							if (oldRatio2 != newRatio2)
							{
								Undo.RegisterCompleteObjectUndo(Instance, "Change Ratio");
								Instance.Transmission.SetGearRatio(0, i, newRatio2, true);
								EditorUtility.SetDirty(Instance);
							}

							EditorGUILayout.BeginHorizontal();
							GUILayout.Space(EditorGUIUtility.labelWidth - overrideLabelWidth);
							EditorGUILayout.LabelField(overrideLabel, EditorStyles.miniLabel, GUILayout.Width(overrideLabelWidth));

							EditorGUI.indentLevel -= 2;

							bool newGearShiftOverrideSpeed = EditorGUILayout.ToggleLeft(gearShiftSpeedLabel, gearShiftOverrideSpeed, EditorStyles.miniBoldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));

							if (gearShiftOverrideSpeed != newGearShiftOverrideSpeed)
							{
								Undo.RegisterCompleteObjectUndo(Instance, "Switch Override");
								Instance.Transmission.SetGearShiftOverrideSpeed(i, newGearShiftOverrideSpeed);
								EditorUtility.SetDirty(Instance);

								gearShiftOverrideSpeed = newGearShiftOverrideSpeed;
							}

							EditorGUI.indentLevel += 2;

							EditorGUILayout.EndHorizontal();
						}
						else
						{
							EditorGUILayout.BeginHorizontal();
							GUILayout.Space(EditorGUIUtility.labelWidth - overrideLabelWidth);
							EditorGUILayout.LabelField(overrideLabel, EditorStyles.miniLabel, GUILayout.Width(overrideLabelWidth));

							EditorGUI.indentLevel -= 2;

							bool newGearShiftOverrideSpeed = EditorGUILayout.ToggleLeft(gearShiftSpeedLabel, gearShiftOverrideSpeed, EditorStyles.miniBoldLabel, GUILayout.Width(EditorGUIUtility.labelWidth));

							if (gearShiftOverrideSpeed != newGearShiftOverrideSpeed)
							{
								Undo.RegisterCompleteObjectUndo(Instance, "Switch Override");
								Instance.Transmission.SetGearShiftOverrideSpeed(i, newGearShiftOverrideSpeed);
								EditorUtility.SetDirty(Instance);

								gearShiftOverrideSpeed = newGearShiftOverrideSpeed;
							}

							EditorGUI.indentLevel += 2;

							EditorGUILayout.EndHorizontal();
						}

						if (gearShiftOverrideSpeed)
						{
							float newOverrideSpeed = ToolkitEditorUtility.NumberField(" ", overrideSpeed, Utility.Units.Speed, 1, Instance, "Change Speed");

							if (overrideSpeed != newOverrideSpeed)
								Instance.Transmission.SetGearShiftOverrideSpeed(i, gearShiftOverrideSpeed, newOverrideSpeed);
						}
					}
				}

				float newFinalGearRatio = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Final Ratio", "This indicates the final output torque ratio"), Utility.Round(Instance.Transmission.FinalGearRatio, 3), false, Instance, "Change Ratio"), .1f);

				if (Instance.Transmission.FinalGearRatio != newFinalGearRatio)
				{
					Instance.Transmission.FinalGearRatio = newFinalGearRatio;

					Instance.Transmission.RefreshGears();
				}

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();

				float newAutoGearShiftTorqueMultiplier = ToolkitEditorUtility.Slider(new GUIContent("Auto. Gear Shift Torque", "The torque threshold at which the gearbox is forced to shift, only if the current engine torque is below that threshold"), Instance.Transmission.GearShiftTorque, 0f, Instance.Behaviour.Torque, Utility.Units.Torque, Instance, "Change Torque") / Instance.Behaviour.Torque;

				if (Instance.Transmission.GearShiftTorqueMultiplier != newAutoGearShiftTorqueMultiplier)
					Instance.Transmission.GearShiftTorqueMultiplier = newAutoGearShiftTorqueMultiplier;

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Differentials", EditorStyles.miniBoldLabel);

				if (Instance.Drivetrain != Vehicle.Train.RWD)
					DifferentialEditor("Front Differential", Instance.Transmission.FrontDifferential, false);

				if (Instance.Drivetrain == Vehicle.Train.AWD)
					DifferentialEditor("Center Differential", Instance.Transmission.CenterDifferential, true);

				if (Instance.Drivetrain != Vehicle.Train.FWD)
					DifferentialEditor("Rear Differential", Instance.Transmission.RearDifferential, false);
			}

			EditorGUILayout.Space();

			EditorGUI.indentLevel--;
		}
		internal void DifferentialEditor(string name, VehicleDifferential differential, bool center)
		{
			EditorGUILayout.BeginVertical(GUI.skin.box);

			EditorGUI.indentLevel++;

			EditorGUILayout.LabelField(name, EditorStyles.miniBoldLabel);

			string sideA = center ? "front" : "left";
			string sideB = center ? "rear" : "right";

			VehicleDifferentialType newType = (VehicleDifferentialType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The differential type"), differential.Type);

			if (differential.Type != newType)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Differential");

				differential.Type = newType;

				EditorUtility.SetDirty(Instance);
			}

			if (differential.Type == VehicleDifferentialType.Custom)
				EditorGUILayout.HelpBox("Make sure to set a `SplitTorqueDelegate` to this differential using some code. If no delegate is set at Runtime, the differential `type` will fall back to `Open`.", MessageType.Info);

			float newGearRatio = ToolkitEditorUtility.NumberField(new GUIContent("Gear Ratio", "The differential gear ratio"), Utility.Round(differential.GearRatio, 3), false, Instance, "Change Gear Ratio");

			if (differential.GearRatio != newGearRatio)
			{
				differential.GearRatio = newGearRatio;

				Instance.Transmission.RefreshGears();
			}

			if (differential.Type != VehicleDifferentialType.Locked || Mathf.Approximately(differential.Stiffness, 0f))
			{
				EditorGUI.BeginDisabledGroup(!center && Mathf.Approximately(differential.BiasAB, .5f));

				float newBiasAB = ToolkitEditorUtility.Slider(new GUIContent("Bias AB", $"Torque bias between {sideA} (A) and {sideB} (B) output"), differential.BiasAB, 0f, 1f, Instance, "Change Bias");

				if (differential.BiasAB != newBiasAB)
				{
					differential.BiasAB = newBiasAB;

					Instance.Transmission.RefreshGears();
				}

				EditorGUI.EndDisabledGroup();
			}
			
			if (differential.Type == VehicleDifferentialType.Locked)
			{
				float newStiffness = ToolkitEditorUtility.Slider(new GUIContent("Stiffness", $"Stiffness of locking differential. Higher values will result in lower difference in rotational velocity between {sideA} and {sideB} wheel. Too high values might introduce slight oscillation due to drivetrain windup and a vehicle that is hard to steer"), differential.Stiffness, 0f, 1f, Instance, "Change Stiffness");

				if (differential.Stiffness != newStiffness)
					differential.Stiffness = newStiffness;
			}
			else if (differential.Type == VehicleDifferentialType.LimitedSlip)
			{
				float newPowerRamp = ToolkitEditorUtility.Slider(new GUIContent("Power Ramp", $"Stiffness of the LSD differential under acceleration. Higher values will result in lower difference in rotational velocity between {sideA} and {sideB} wheels. Too high values might introduce slight oscillation due to drivetrain windup and a vehicle that is hard to steer"), differential.PowerRamp, 0f, 1f, Instance, "Change Power Ramp");

				if (differential.PowerRamp != newPowerRamp)
					differential.PowerRamp = newPowerRamp;

				float newCoastRamp = ToolkitEditorUtility.Slider(new GUIContent("Coast Ramp", $"Stiffness of the LSD differential under braking. Higher values will result in lower difference in rotational velocity between {sideA} and {sideB} wheels. Too high values might introduce slight oscillation due to drivetrain windup and a vehicle that is hard to steer"), differential.CoastRamp, 0f, 1f, Instance, "Change Coast Ramp");

				if (differential.CoastRamp != newCoastRamp)
					differential.CoastRamp = newCoastRamp;

				float newSlipTorque = ToolkitEditorUtility.NumberField(new GUIContent("Slip Torque", "Slip torque of the LSD differential"), differential.SlipTorque, Utility.Units.Torque, 2, Instance, "Change Slip Torque");

				if (differential.SlipTorque != newSlipTorque)
					differential.SlipTorque = newSlipTorque;
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
		}
		internal void BrakesEditor()
		{
			EditorGUI.indentLevel++;

			brakesFoldout = EnableFoldout();

			EditorGUILayout.Space();

			bool newFrontBrakesFoldout = frontBrakesFoldout;
			bool newRearBrakesFoldout = rearBrakesFoldout;

			ToolkitEditorUtility.ToggleTabButtons("Front", "Rear", ref newFrontBrakesFoldout, ref newRearBrakesFoldout);

			if (newFrontBrakesFoldout && frontBrakesFoldout != newFrontBrakesFoldout)
				frontBrakesFoldout = EnableInternalFoldout();

			if (newRearBrakesFoldout && rearBrakesFoldout != newRearBrakesFoldout)
				rearBrakesFoldout = EnableInternalFoldout();

			if (frontBrakesFoldout || rearBrakesFoldout)
				BrakesTrainEditor(frontBrakesFoldout && !rearBrakesFoldout ? Instance.Behaviour.FrontBrakes : Instance.Behaviour.RearBrakes);

			EditorGUILayout.Space();

			EditorGUI.indentLevel--;
		}
		internal void BrakesTrainEditor(Vehicle.BrakeModule train)
		{
			EditorGUILayout.Space();

			Vehicle.BrakeModule.BrakeType newType = (Vehicle.BrakeModule.BrakeType)EditorGUILayout.EnumPopup(new GUIContent("Type", "The brake material or construction type"), train.Type);

			if (train.Type != newType)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Type");

				train.Type = newType;

				EditorUtility.SetDirty(Instance);
			}

			float newDiameter = ToolkitEditorUtility.NumberField(new GUIContent("Diameter", $"The overall {(train.Type != Vehicle.BrakeModule.BrakeType.Drum ? "brake disc" : "drum")} diameter"), train.Diameter * 1000f, Utility.Units.SizeAccurate, true, Instance, "Change Diameter") * .001f;

			if (train.Diameter != newDiameter)
				train.Diameter = newDiameter;

			if (train.Type != Vehicle.BrakeModule.BrakeType.Drum)
			{
				int newPistons = ToolkitEditorUtility.Slider(new GUIContent("Pistons", "The number of pistons each brake has"), train.Pistons, 1, 20, Instance, "Change Pistons");

				if (train.Pistons != newPistons)
					train.Pistons = newPistons;

				int newPads = ToolkitEditorUtility.Slider(new GUIContent("Pads", "The number of pads each brake has"), train.Pads, 1, 10, Instance, "Change Pads");

				if (train.Pads != newPads)
					train.Pads = newPads;
			}

			float newPressure = ToolkitEditorUtility.NumberField(new GUIContent("Pressure", $"The pressure applied to {(train.Type != Vehicle.BrakeModule.BrakeType.Drum ? "a single brake piston" : "the brake shoes")}"), train.Pressure * 2f, Utility.Units.Pressure, true, Instance, "Change Pressure") * .5f;

			if (train.Pressure != newPressure)
				train.Pressure = newPressure;

			float newFriction = ToolkitEditorUtility.Slider(new GUIContent("Friction", $"The braking {(train.Type != Vehicle.BrakeModule.BrakeType.Drum ? "pads" : "shoes")} friction coefficient"), train.Friction, .01f, 2f, Instance, "Change Friction");

			if (train.Friction != newFriction)
				train.Friction = newFriction;


			EditorGUILayout.Space();
			EditorGUILayout.HelpBox($"Brake Torque: {Utility.NumberToValueWithUnit(train.BrakeTorque / Instance.Wheels.Length, Utility.Units.Torque, Settings.editorTorqueUnit, 1)}", MessageType.None);

			if (Settings.useBrakesHeat)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.HelpBox($"Brake Density: {Utility.NumberToValueWithUnit(train.Density, Utility.Units.Density, Settings.editorValuesUnit, true)}", MessageType.None);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.HelpBox($"Heat Threshold: {train.BrakeHeatThreshold:0.00}", MessageType.None);
				EditorGUILayout.EndHorizontal();
			}
		}
		internal void SteeringEditor()
		{
			EditorGUI.indentLevel++;

			steeringFoldout = EnableFoldout();

			EditorGUILayout.Space();

			int newSteerMethodIndex = EditorGUILayout.Popup("Steer Method", Instance.Steering.overrideMethod ? (int)Instance.Steering.Method + 1 : 0, new string[] { "Use Default Settings", "Simple", "Responsive", "Ackermann" }) - 1;
			Vehicle.SteeringModule.SteerMethod newSteerMethod = (Vehicle.SteeringModule.SteerMethod)newSteerMethodIndex;
			bool newOverrideMethod = newSteerMethodIndex > -1;

			if (newOverrideMethod && Instance.Steering.Method != newSteerMethod || Instance.Steering.overrideMethod != newOverrideMethod)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Method");

				Instance.Steering.overrideMethod = newOverrideMethod;
				Instance.Steering.Method = newSteerMethod;

				EditorUtility.SetDirty(Instance);
			}

			float newMaximumSteerAngle = ToolkitEditorUtility.Slider(new GUIContent("Max Steer Angle", "The maximum steer angle of the steering wheels"), math.min(Instance.Steering.MaximumSteerAngle, Settings.maximumSteerAngle), Instance.Steering.Method != Vehicle.SteeringModule.SteerMethod.Simple ? Instance.Steering.MinimumSteerAngle : 0f, Settings.maximumSteerAngle, "°", "Degrees", Instance, "Change Max Steer Angle");

			if (Instance.Steering.MaximumSteerAngle != newMaximumSteerAngle)
				Instance.Steering.MaximumSteerAngle = newMaximumSteerAngle;

			if (Instance.Steering.Method != Vehicle.SteeringModule.SteerMethod.Simple)
			{
				float newMinimumSteerAngle = ToolkitEditorUtility.Slider(new GUIContent("Min Steer Angle", "The minimum steer angle of the steering wheels at a mentioned speed"), math.clamp(Instance.Steering.MinimumSteerAngle, Settings.minimumSteerAngle, Instance.Steering.MaximumSteerAngle), Settings.minimumSteerAngle, Instance.Steering.MaximumSteerAngle, "°", "Degrees", Instance, "Change Min Steer Angle");

				if (Instance.Steering.MinimumSteerAngle != newMinimumSteerAngle)
					Instance.Steering.MinimumSteerAngle = newMinimumSteerAngle;

				float newLowSteerAngleSpeed = math.clamp(ToolkitEditorUtility.NumberField(new GUIContent("Low Steer Angle Speed", "When the vehicle reach this speed or higher, the Maximum Steer Angle is already equal to the Minimum Steer Angle"), Instance.Steering.LowSteerAngleSpeed, Utility.Units.Speed, 1, Instance, "Change Low Steer Speed"), 1f, Instance.Behaviour.TopSpeed);

				if (Instance.Steering.LowSteerAngleSpeed != newLowSteerAngleSpeed)
					Instance.Steering.LowSteerAngleSpeed = newLowSteerAngleSpeed;

				bool newClampSteerAngle = ToolkitEditorUtility.ToggleButtons(new GUIContent("Clamp", "Clamps the steer angle interpolation over speed to not exceed the minimum steer angle when enabled"), null, "On", "Off", Instance.Steering.clampSteerAngle, Instance, "Switch Clamp");

				if (Instance.Steering.clampSteerAngle != newClampSteerAngle)
					Instance.Steering.clampSteerAngle = newClampSteerAngle;
			}

			EditorGUILayout.Space();

			bool newUseDynamicSteering = ToolkitEditorUtility.ToggleButtons(new GUIContent("Dynamic Steering", "Automatic steering for non-default steering wheels"), null, "On", "Off", Instance.Steering.UseDynamicSteering, Instance, "Switch Dynamic Steer");

			if (Instance.Steering.UseDynamicSteering != newUseDynamicSteering)
				Instance.Steering.UseDynamicSteering = newUseDynamicSteering;

			if (Instance.Steering.UseDynamicSteering)
			{
				EditorGUI.indentLevel++;

				float newDynamicSteeringIntensity = ToolkitEditorUtility.Slider(new GUIContent("Intensity", "The dynamic steering intensity"), Instance.Steering.DynamicSteeringIntensity, 0f, 1f, Instance, "Change Intensity");

				if (Instance.Steering.DynamicSteeringIntensity != newDynamicSteeringIntensity)
					Instance.Steering.DynamicSteeringIntensity = newDynamicSteeringIntensity;

				newDynamicSteeringIntensity = Utility.InverseLerp(0f, Instance.Steering.MaximumSteerAngle, ToolkitEditorUtility.NumberField(new GUIContent("Angle", "The dynamic steering maximum angle"), Utility.Lerp(0f, Instance.Steering.MaximumSteerAngle, Instance.Steering.DynamicSteeringIntensity), "°", "Degrees", 1, Instance, "Change Angle"));

				if (Instance.Steering.DynamicSteeringIntensity != newDynamicSteeringIntensity)
					Instance.Steering.DynamicSteeringIntensity = newDynamicSteeringIntensity;

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(!Instance.CanRearSteer && !Instance.Steering.UseDynamicSteering);

			bool newInvertRearSteer = ToolkitEditorUtility.ToggleButtons(new GUIContent("Rear Steer Type", "Steering type for rear wheels"), null, "Inverted", "Normal", Instance.Steering.invertRearSteer, Instance, "Switch Type");

			if (Instance.Steering.invertRearSteer != newInvertRearSteer)
				Instance.Steering.invertRearSteer = newInvertRearSteer;

			EditorGUI.EndDisabledGroup();

			if (!Instance.CanRearSteer && !Instance.Steering.UseDynamicSteering)
				EditorGUILayout.HelpBox("To change Rear Steer Type, the vehicle has to contain at least one rear steering wheel, or Dynamic Steering has to be enabled.", MessageType.None);
			else
				EditorGUILayout.Space();

			EditorGUI.indentLevel--;
		}
		internal void SuspensionEditor()
		{
			EditorGUI.indentLevel++;

			suspensionsFoldout = EnableFoldout();

			EditorGUILayout.Space();

			bool newFrontSuspensionFoldout = frontSuspensionFoldout;
			bool newRearSuspensionFoldout = rearSuspensionFoldout;

			ToolkitEditorUtility.ToggleTabButtons("Front", "Rear", ref newFrontSuspensionFoldout, ref newRearSuspensionFoldout);

			if (newFrontSuspensionFoldout && frontSuspensionFoldout != newFrontSuspensionFoldout)
				frontSuspensionFoldout = EnableInternalFoldout();

			if (newRearSuspensionFoldout && rearSuspensionFoldout != newRearSuspensionFoldout)
				rearSuspensionFoldout = EnableInternalFoldout();

			if (frontSuspensionFoldout || rearSuspensionFoldout)
				SuspensionTrainEditor(frontSuspensionFoldout && !rearSuspensionFoldout ? Instance.FrontSuspension : Instance.RearSuspension);

			EditorGUILayout.Space();

			EditorGUI.indentLevel--;
		}
		internal void SuspensionTrainEditor(VehicleWheel.SuspensionModule train)
		{
			EditorGUILayout.Space();

			float newLength = ToolkitEditorUtility.NumberField(new GUIContent("Length", "The suspension spring length"), train.Length * 1000f, Utility.Units.SizeAccurate, 1, Instance, "Change Length") * .001f;

			if (train.Length != newLength)
				train.Length = newLength;

#if !MVC_COMMUNITY
			EditorGUILayout.BeginHorizontal();

			float newLengthStance = ToolkitEditorUtility.Slider(new GUIContent("Stance", "The suspension length stance multiplier"), train.LengthStance, -Mathf.Clamp01(1f - (.05f / train.Length)), 1f, Instance, "Change Stance");

			if (train.LengthStance != newLengthStance)
				train.LengthStance = newLengthStance;

			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.HelpBox($"Total spring length: {Utility.Round(train.Length * (1f + train.LengthStance) * 1000f, 1)} {Utility.Unit(Utility.Units.SizeAccurate, Settings.editorValuesUnit)}", MessageType.None);
#endif

			float newTarget;

			switch (Settings.springTargetMeasurement)
			{
				case ToolkitSettings.EditorSpringTargetMeasurement.Length:
					newTarget = ToolkitEditorUtility.NumberField(new GUIContent("Target", "The suspension spring the minimum compress length"), train.Target * train.Length * 1000f, Utility.Units.SizeAccurate, 1, Instance, "Change Target") * .001f / train.Length;

					EditorGUILayout.HelpBox($"Maximum spring compress target: {Utility.Round(train.Target * 100f, 1)}%", MessageType.None);

					if (train.Target != newTarget)
						train.Target = newTarget;

					break;

				default:
					newTarget = ToolkitEditorUtility.Slider(new GUIContent("Target", "The suspension spring the maximum compress target"), train.Target, 0f, 1f, Instance, "Change Target");

					EditorGUILayout.HelpBox($"Minimum spring compress length: {train.Length * (1f + train.LengthStance) * 1000f * train.Target} {Utility.Unit(Utility.Units.SizeAccurate, Settings.editorValuesUnit)}", MessageType.None);

					if (train.Target != newTarget)
						train.Target = newTarget;

					break;
			}

			EditorGUILayout.Space();

			float newStiffness = ToolkitEditorUtility.NumberField(new GUIContent("Stiffness", "The suspension spring force"), train.Stiffness, Utility.Units.Force, 1, Instance, "Change Stiffness");

			if (train.Stiffness != newStiffness)
				train.Stiffness = newStiffness;

			float newDamper = ToolkitEditorUtility.NumberField(new GUIContent("Damper", "The suspension spring damper force"), train.Damper, Utility.Units.Force, 1, Instance, "Change Damper");

			if (train.Damper != newDamper)
				train.Damper = newDamper;
#if !MVC_COMMUNITY

			if (Settings.useSuspensionAdjustments)
			{
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Stancing", EditorStyles.boldLabel);

				float newCamber = ToolkitEditorUtility.Slider(new GUIContent("Camber Angle", "The default wheel camber angle"), train.Camber, -Settings.maximumCamberAngle, Settings.maximumCamberAngle, "°", "Degrees", Instance, "Change Camber");

				if (train.Camber != newCamber)
					train.Camber = newCamber;

				float newCaster = ToolkitEditorUtility.Slider(new GUIContent("Caster", "The wheel caster angle"), train.Caster, -Settings.maximumCasterAngle, Settings.maximumCasterAngle, "°", "Degrees", Instance, "Change Caster");

				if (train.Caster != newCaster)
					train.Caster = newCaster;

				float newToe = ToolkitEditorUtility.Slider(new GUIContent("Toe", "The wheel toe angle"), train.Toe, -Settings.maximumToeAngle, Settings.maximumToeAngle, "°", "Degrees", Instance, "Change Toe");

				if (train.Toe != newToe)
					train.Toe = newToe;

				float newSideOffset = ToolkitEditorUtility.Slider(new GUIContent("Side Offset", "The wheel side position offset"), train.SideOffset, -Settings.maximumSideOffset, Settings.maximumSideOffset, Utility.Units.Size, Instance, "Change Offset");

				if (train.SideOffset != newSideOffset)
					train.SideOffset = newSideOffset;
			}
#endif

			EditorGUILayout.Space();
		}
		internal void TiresEditor()
		{
			EditorGUI.indentLevel++;

			tiresFoldout = EnableFoldout();

			EditorGUILayout.Space();

			string[] tireCompoundNames = Settings.GetTireCompoundsNames(true);

			foreach (var wheel in Instance.Wheels)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginHorizontal();

				int newTireCompoundIndex = EditorGUILayout.Popup(new GUIContent(wheel.WheelName, $"{wheel.WheelName} tire friction compound"), wheel.Instance.TireCompoundIndex, tireCompoundNames);

				if (wheel.Instance.TireCompoundIndex != newTireCompoundIndex)
				{
					Undo.RegisterCompleteObjectUndo(wheel.Instance, "Change Tire");

					wheel.Instance.TireCompoundIndex = newTireCompoundIndex;

					EditorUtility.SetDirty(wheel.Instance);
				}

				if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					componentsFoldout = EnableFoldout();

					EditWheel(wheel);

					wheel.editorFoldout = VehicleWheel.EditorFoldout.Dimensions;
				}

				EditorGUILayout.EndHorizontal();
				EditorGUILayout.EndVertical();
			}

			if (!EditorWindow.HasOpenInstances<ToolkitSettingsEditorWindow>() || !ToolkitSettingsEditorWindow.instance)
				if (GUILayout.Button("Edit Tire Compounds"))
					ToolkitSettingsEditor.OpenWindow(ToolkitSettings.SettingsEditorFoldout.TireCompounds);

			EditorGUILayout.Space();

			EditorGUI.indentLevel--;
		}
		internal void StabilityEditor()
		{
			stabilityFoldout = EnableFoldout();

			EditorGUILayout.Space();

			if (Instance.LeftWheels.Length > 0 && Instance.RightWheels.Length > 0)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginVertical(GUI.skin.box);

				bool newUseAntiSwayBars = ToolkitEditorUtility.ToggleButtons(new GUIContent("Anti-Sway Bars", "Anti-Sway Bars prevent the vehicle from flipping"), EditorStyles.miniBoldLabel, "On", "Off", Instance.Stability.useAntiSwayBars, Instance, "Switch Anti-Sway");

				EditorGUILayout.EndVertical();

				if (Instance.Stability.useAntiSwayBars != newUseAntiSwayBars)
					Instance.Stability.useAntiSwayBars = newUseAntiSwayBars;

				if (Instance.Stability.useAntiSwayBars)
				{
					EditorGUI.indentLevel++;

					if (!Instance.IsTrailer && Instance.FrontLeftWheels.Length > 0 && Instance.FrontRightWheels.Length > 0)
					{
						float newAntiSwayFront = ToolkitEditorUtility.NumberField(new GUIContent("Front", "The anti-sway bar downforce on the front axis"), Instance.Stability.AntiSwayFront, Utility.Units.Force, 1, Instance, "Change Force");

						if (Instance.Stability.AntiSwayFront != newAntiSwayFront)
							Instance.Stability.AntiSwayFront = newAntiSwayFront;
					}

					if (Instance.RearLeftWheels.Length > 0 && Instance.RearRightWheels.Length > 0)
					{
						float newAntiSwayRear = ToolkitEditorUtility.NumberField(new GUIContent(TrailerInstance ? "Force" : "Rear", $"The anti-sway bar downforce on the {(TrailerInstance ? "wheel" : "rear")} axis"), Instance.Stability.AntiSwayRear, Utility.Units.Force, 1, Instance, "Change Force");

						if (Instance.Stability.AntiSwayRear != newAntiSwayRear)
							Instance.Stability.AntiSwayRear = newAntiSwayRear;
					}

					EditorGUI.indentLevel--;

					EditorGUILayout.Space();
				}

				EditorGUILayout.EndVertical();
			}

			if (!TrailerInstance)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginVertical(GUI.skin.box);

				bool newUseABS = ToolkitEditorUtility.ToggleButtons(new GUIContent("ABS", "This helps the vehicle to avoid the tire slipping while braking"), EditorStyles.miniBoldLabel, "On", "Off", Instance.Stability.useABS, Instance, "Switch ABS");

				EditorGUILayout.EndVertical();

				if (Instance.Stability.useABS != newUseABS)
					Instance.Stability.useABS = newUseABS;

				if (Instance.Stability.useABS)
				{
					EditorGUI.indentLevel++;

					float newABSThreshold = ToolkitEditorUtility.Slider("Threshold", Instance.Stability.ABSThreshold, 0f, 1f, Instance, "Change Threshold");

					if (Instance.Stability.ABSThreshold != newABSThreshold)
						Instance.Stability.ABSThreshold = newABSThreshold;

					bool newUseHandbrakeABS = ToolkitEditorUtility.ToggleButtons(new GUIContent("Handbrake ABS", "Apply ABS when using the handbrake"), null, "On", "Off", Instance.Stability.useHandbrakeABS, Instance, "Switch ABS");

					if (Instance.Stability.useHandbrakeABS != newUseHandbrakeABS)
						Instance.Stability.useHandbrakeABS = newUseHandbrakeABS;

					EditorGUI.indentLevel--;

					EditorGUILayout.Space();
				}

				EditorGUILayout.EndVertical();
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginVertical(GUI.skin.box);

				bool newUseESP = ToolkitEditorUtility.ToggleButtons(new GUIContent("ESP", "The following feature helps the vehicle to avoid Over-Steer and Under-Steer"), EditorStyles.miniBoldLabel, "On", "Off", Instance.Stability.useESP, Instance, "Switch ESP");

				EditorGUILayout.EndVertical();

				if (Instance.Stability.useESP != newUseESP)
					Instance.Stability.useESP = newUseESP;

				if (Instance.Stability.useESP)
				{
					EditorGUI.indentLevel++;

					float newESPStrength = ToolkitEditorUtility.Slider("Intensity", Instance.Stability.ESPStrength, 0f, 1f, Instance, "Change Intensity");

					if (Instance.Stability.ESPStrength != newESPStrength)
						Instance.Stability.ESPStrength = newESPStrength;

					float newESPSpeedThreshold = ToolkitEditorUtility.NumberField(new GUIContent("Speed Threshold", "The minimum speed to activate ESP"), Instance.Stability.ESPSpeedThreshold, Utility.Units.Speed, 1, Instance, "Change ESP Threshold");

					if (Instance.Stability.ESPSpeedThreshold != newESPSpeedThreshold)
						Instance.Stability.ESPSpeedThreshold = newESPSpeedThreshold;

					bool newESPAllowDonuts = ToolkitEditorUtility.ToggleButtons(new GUIContent("Allow Donuts", "Activating this will make the ESP system bypass donut stunts and only intensify at high speeds"), null, "Yes", "No", Instance.Stability.ESPAllowDonuts, Instance, "Switch Donuts");

					if (Instance.Stability.ESPAllowDonuts != newESPAllowDonuts)
						Instance.Stability.ESPAllowDonuts = newESPAllowDonuts;

					EditorGUI.indentLevel--;

					EditorGUILayout.Space();
				}

				EditorGUILayout.EndVertical();
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginVertical(GUI.skin.box);

				bool newUseTCS = ToolkitEditorUtility.ToggleButtons(new GUIContent("TCS", "This feature is helpful to avoid tractions loss when giving the engine too much fuel"), EditorStyles.miniBoldLabel, "On", "Off", Instance.Stability.useTCS, Instance, "Switch TCS");

				EditorGUILayout.EndVertical();

				if (Instance.Stability.useTCS != newUseTCS)
					Instance.Stability.useTCS = newUseTCS;

				if (Instance.Stability.useTCS)
				{
					EditorGUI.indentLevel++;

					float newTCSThreshold = ToolkitEditorUtility.Slider("Threshold", Instance.Stability.TCSThreshold, 0f, 1f, Instance, "Change Threshold");

					if (Instance.Stability.TCSThreshold != newTCSThreshold)
						Instance.Stability.TCSThreshold = newTCSThreshold;

					bool newTCSAllowBurnouts = ToolkitEditorUtility.ToggleButtons(new GUIContent("Allow Burnouts", "Activating this will make the TCS system bypass burnout stunts and only intensify when accelerating"), null, "Yes", "No", Instance.Stability.TCSAllowBurnouts, Instance, "Switch Donuts");

					if (Instance.Stability.TCSAllowBurnouts != newTCSAllowBurnouts)
						Instance.Stability.TCSAllowBurnouts = newTCSAllowBurnouts;

					EditorGUI.indentLevel--;

					EditorGUILayout.Space();
				}

				EditorGUILayout.EndVertical();
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginVertical(GUI.skin.box);

				bool newUseArcadeSteerHelpers = ToolkitEditorUtility.ToggleButtons(new GUIContent("Arcade Steer Helpers", "The linear helper helps the vehicle to turn without losing traction while the angular one helps the vehicle to lose traction and make stunts"), EditorStyles.miniBoldLabel, "On", "Off", Instance.Stability.useArcadeSteerHelpers, Instance, "Switch Helper");

				EditorGUILayout.EndVertical();

				if (Instance.Stability.useArcadeSteerHelpers != newUseArcadeSteerHelpers)
					Instance.Stability.useArcadeSteerHelpers = newUseArcadeSteerHelpers;

				if (Instance.Stability.useArcadeSteerHelpers)
				{
					EditorGUI.indentLevel++;

					float newSteerHelperLinearStrength = ToolkitEditorUtility.Slider("Linear Intensity", Instance.Stability.ArcadeLinearSteerHelperIntensity, 0f, 2f, Instance, "Change Intensity");

					if (Instance.Stability.ArcadeLinearSteerHelperIntensity != newSteerHelperLinearStrength)
						Instance.Stability.ArcadeLinearSteerHelperIntensity = newSteerHelperLinearStrength;

					float newSteerHelperAngularStrength = ToolkitEditorUtility.Slider("Angular Intensity", Instance.Stability.ArcadeAngularSteerHelperIntensity, 0f, 2f, Instance, "Change Intensity");

					if (Instance.Stability.ArcadeAngularSteerHelperIntensity != newSteerHelperAngularStrength)
						Instance.Stability.ArcadeAngularSteerHelperIntensity = newSteerHelperAngularStrength;

					EditorGUI.indentLevel--;

					EditorGUILayout.Space();
				}

				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField(new GUIContent("Handling", "The following properties indicate the vehicle handling behaviour, either being very slippery or grippy"), EditorStyles.miniBoldLabel);
			EditorGUILayout.EndVertical();

			EditorGUI.indentLevel++;

			if (Settings.useCounterSteer && !TrailerInstance)
			{
				EditorGUI.BeginDisabledGroup(!Instance.CanFrontSteer);

				bool newCounterSteerHelper = ToolkitEditorUtility.ToggleButtons(new GUIContent("Counter Steer", "Counter steering helps the vehicle regain traction when slipping"), null, "On", "Off", Instance.Stability.UseCounterSteer, Instance, "Switch Counter Steer");

				if (Instance.Stability.UseCounterSteer != newCounterSteerHelper)
					Instance.Stability.UseCounterSteer = newCounterSteerHelper;

				EditorGUI.EndDisabledGroup();

				if (!Instance.CanFrontSteer)
					EditorGUILayout.HelpBox("You cannot use counter steer with a non-front steering vehicle", MessageType.None);
			}

			float newHandlingRate = ToolkitEditorUtility.Slider(new GUIContent("Handling Rate", "The main handling rate, it indicates the Grip/Drift rate of the vehicle"), Instance.Stability.HandlingRate, 0f, 1f, Instance, "Change Rate");

			if (Instance.Stability.HandlingRate != newHandlingRate)
				Instance.Stability.HandlingRate = newHandlingRate;

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField(new GUIContent("Center Of Mass", "Control the center of mass through weight position"), EditorStyles.miniBoldLabel);
			EditorGUILayout.EndVertical();

			EditorGUI.indentLevel++;

			float newWeightDistribution = ToolkitEditorUtility.Slider(new GUIContent("Weight Distribution", "This helps determining the position of the center of mass on the Z axis depending on the wheel base.\r\nExamples:\r\n\t0.0: Full weight in the front\r\n\t0.5: Full weight in the middle\r\n\t1.0: Full weight in the rear"), Instance.Stability.WeightDistribution, 0f, 1f, Instance, "Change Distribution");

			if (Instance.Stability.WeightDistribution != newWeightDistribution)
				Instance.Stability.WeightDistribution = newWeightDistribution;

			EditorGUILayout.BeginHorizontal();

			float newWeightHeight = ToolkitEditorUtility.Slider(new GUIContent("Weight Height", "The height of the center of mass"), Instance.Stability.WeightHeight, 0f, 1f, Instance, "Change Height");

			if (Instance.Stability.WeightHeight != newWeightHeight)
				Instance.Stability.WeightHeight = newWeightHeight;

			if (GUILayout.Button(EditorUtilities.Icons.Reload, ToolkitEditorUtility.UnstretchableMiniButtonWide))
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Reset Height");

				Instance.RefreshWeightHeight();

				EditorUtility.SetDirty(Instance);
			}

			EditorGUILayout.EndHorizontal();

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField(new GUIContent("Physics", "Everything Rigidbody-related"), EditorStyles.miniBoldLabel);
			EditorGUILayout.EndVertical();

			EditorGUI.indentLevel++;

			RigidbodyInterpolation newRigidbodyInterpolation = (RigidbodyInterpolation)EditorGUILayout.EnumPopup(new GUIContent("Interpolation", "Rigidbody Interpolation"), Instance.Rigidbody.interpolation);

			if (Instance.Rigidbody.interpolation != newRigidbodyInterpolation)
			{
				Undo.RegisterChildrenOrderUndo(Instance, "Change Interpolation");

				Instance.Rigidbody.interpolation = newRigidbodyInterpolation;

				EditorUtility.SetDirty(Instance);
			}

			CollisionDetectionMode newCollisionDetection = (CollisionDetectionMode)EditorGUILayout.EnumPopup(new GUIContent("Collision Detection", "Rigidbody Collision Detection"), Instance.Rigidbody.collisionDetectionMode);

			if (Instance.Rigidbody.collisionDetectionMode != newCollisionDetection)
			{
				Undo.RegisterChildrenOrderUndo(Instance, "Change Detection");

				Instance.Rigidbody.collisionDetectionMode = newCollisionDetection;

				EditorUtility.SetDirty(Instance);
			}

			float newMaximumAngularVelocity = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Maximum Angular Velocity", "The maximum Rigidbody angular velocity"), Instance.Rigidbody.maxAngularVelocity), 0f);

			if (Instance.Rigidbody.maxAngularVelocity != newMaximumAngularVelocity)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Velocity");

				Instance.Rigidbody.maxAngularVelocity = newMaximumAngularVelocity;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();

			if (Settings.useDownforce || Settings.useDrag)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField(new GUIContent("Aero Dynamics", "The following properties indicate the vehicle handling behaviour, either being very slippery or grippy"), EditorStyles.miniBoldLabel);
				EditorGUILayout.EndVertical();

				EditorGUI.indentLevel++;

				if (Settings.useDownforce)
				{
					string downforceTooltip = "The Downforce is used to add more grip in relation to speed";
					bool newUseDownforce = ToolkitEditorUtility.ToggleButtons(new GUIContent("Downforce", downforceTooltip), null, "On", "Off", Instance.Stability.useDownforce, Instance, "Switch Downforce");

					if (Instance.Stability.useDownforce != newUseDownforce)
						Instance.Stability.useDownforce = newUseDownforce;

					if (Instance.Stability.useDownforce)
					{
						EditorGUI.indentLevel++;

						if (!Instance.IsTrailer)
						{
							float newFrontDownforce = ToolkitEditorUtility.NumberField(new GUIContent("Front", downforceTooltip), Instance.Stability.FrontDownforce, Utility.Units.Force, 1, Instance, "Change Downforce");

							if (Instance.Stability.FrontDownforce != newFrontDownforce)
								Instance.Stability.FrontDownforce = newFrontDownforce;
						}

						float newRearDownforce = ToolkitEditorUtility.NumberField(new GUIContent(Instance.IsTrailer ? "Force" : "Rear", downforceTooltip), Instance.Stability.RearDownforce, Utility.Units.Force, 1, Instance, "Change Downforce");

						if (Instance.Stability.RearDownforce != newRearDownforce)
							Instance.Stability.RearDownforce = newRearDownforce;

						EditorGUI.indentLevel--;
					}
				}

				if (Settings.useDrag)
				{
					float newDrag = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Drag", "The vehicle aero dynamics resistance coefficient"), Instance.Stability.Drag), 0f);

					if (Instance.Stability.Drag != newDrag)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Drag");

						Instance.Stability.Drag = newDrag;

						EditorUtility.SetDirty(Instance);
					}

					float newAngularDrag = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Angular Drag", "The vehicle angular aero dynamics resistance"), Instance.Stability.AngularDrag), 0f);

					if (Instance.Stability.AngularDrag != newAngularDrag)
					{
						Undo.RegisterCompleteObjectUndo(Instance, "Change Drag");

						Instance.Stability.AngularDrag = newAngularDrag;

						EditorUtility.SetDirty(Instance);
					}

					float newDragScale = ToolkitEditorUtility.Slider(new GUIContent("Drag Scale", "The vehicle aero dynamics resistance scale"), Instance.Stability.DragScale, 1e-05f, 2f, Instance, "Change Scale");

					if (Instance.Stability.DragScale != newDragScale)
						Instance.Stability.DragScale = newDragScale;
				}

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
				EditorGUILayout.EndVertical();
			}
		}
		internal void AudioEditor()
		{
			audioFoldout = EnableFoldout();

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Effects", EditorStyles.miniBoldLabel);
#if !MVC_COMMUNITY
			EditorGUI.indentLevel++;

			EditorGUILayout.BeginHorizontal();

			float newInteriorLowPassFrequency = ToolkitEditorUtility.Slider(new GUIContent("Interior Low-Pass Freq.", "The follower audio listener low-pass frequency while its inside the vehicle interior"), Instance.AudioMixers.InteriorLowPassFreq, 10f, 22000f, Utility.Units.Frequency, Instance, "Change Frequency");

			if (Instance.AudioMixers.InteriorLowPassFreq != newInteriorLowPassFrequency)
				Instance.AudioMixers.InteriorLowPassFreq = newInteriorLowPassFrequency;

			EditorGUILayout.EndHorizontal();

			EditorGUI.indentLevel--;

#endif
			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Audio Mixers", EditorStyles.miniBoldLabel);

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			AudioMixerGroup newEngine = EditorGUILayout.ObjectField("Engine", Instance.AudioMixers.engine, typeof(AudioMixerGroup), false) as AudioMixerGroup;

			if (Instance.AudioMixers.engine != newEngine)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Mixer");

				Instance.AudioMixers.engine = newEngine;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.indentLevel++;

			if (!Instance.AudioMixers.engine)
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(new GUIContent("Default", "The audio mixer used as default"), Instance.AudioMixers.Engine, typeof(AudioMixerGroup), false);
				EditorGUI.EndDisabledGroup();

				if (!Instance.AudioMixers.Engine)
					EditorGUILayout.HelpBox("It seems like the default audio mixer is missing! To fix this issue you have to go to the settings panel and assign one.", MessageType.Info);
			}

			float newEngineVolume = ToolkitEditorUtility.Slider("Volume", Instance.AudioMixers.EngineVolume * 100f, 0f, 100f, "%", "Percentage", Instance, "Change Volume") * .01f;

			if (Instance.AudioMixers.EngineVolume != newEngineVolume)
				Instance.AudioMixers.EngineVolume = newEngineVolume;

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();

			if (!Instance.IsElectric)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);

				AudioMixerGroup newExhaust = EditorGUILayout.ObjectField("Exhaust", Instance.AudioMixers.exhaust, typeof(AudioMixerGroup), false) as AudioMixerGroup;

				if (Instance.AudioMixers.exhaust != newExhaust)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Mixer");

					Instance.AudioMixers.exhaust = newExhaust;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel++;

				if (!Instance.AudioMixers.exhaust)
				{
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ObjectField(new GUIContent("Default", "The audio mixer used as default"), Instance.AudioMixers.Exhaust, typeof(AudioMixerGroup), false);
					EditorGUI.EndDisabledGroup();

					if (!Instance.AudioMixers.Exhaust)
						EditorGUILayout.HelpBox("It seems like the default audio mixer is missing! To fix this issue you have to go to the settings panel and assign one.", MessageType.Info);
				}

				float newExhaustVolume = ToolkitEditorUtility.Slider("Volume", Instance.AudioMixers.ExhaustVolume * 100f, 0f, 100f, "%", "Percentage", Instance, "Change Volume") * .01f;

				if (Instance.AudioMixers.ExhaustVolume != newExhaustVolume)
					Instance.AudioMixers.ExhaustVolume = newExhaustVolume;

				EditorGUI.indentLevel--;

				EditorGUILayout.EndVertical();
				EditorGUILayout.BeginVertical(GUI.skin.box);

				AudioMixerGroup newExhaustEffects = EditorGUILayout.ObjectField("Exhaust Effects", Instance.AudioMixers.exhaustEffects, typeof(AudioMixerGroup), false) as AudioMixerGroup;

				if (Instance.AudioMixers.exhaustEffects != newExhaustEffects)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Mixer");

					Instance.AudioMixers.exhaustEffects = newExhaustEffects;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel++;

				if (!Instance.AudioMixers.exhaustEffects)
				{
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ObjectField(new GUIContent("Default", "The audio mixer used as default"), Instance.AudioMixers.ExhaustEffects, typeof(AudioMixerGroup), false);
					EditorGUI.EndDisabledGroup();

					if (!Instance.AudioMixers.ExhaustEffects)
						EditorGUILayout.HelpBox("It seems like the default audio mixer is missing! To fix this issue you have to go to the settings panel and assign one.", MessageType.Info);
				}

				float newExhaustEffectsVolume = ToolkitEditorUtility.Slider("Volume", Instance.AudioMixers.ExhaustEffectsVolume * 100f, 0f, 100f, "%", "Percentage", Instance, "Change Volume") * .01f;

				if (Instance.AudioMixers.ExhaustEffectsVolume != newExhaustEffectsVolume)
					Instance.AudioMixers.ExhaustEffectsVolume = newExhaustEffectsVolume;

				EditorGUI.indentLevel--;

				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.BeginVertical(GUI.skin.box);

			AudioMixerGroup newTransmission = EditorGUILayout.ObjectField("Transmission", Instance.AudioMixers.transmission, typeof(AudioMixerGroup), false) as AudioMixerGroup;

			if (Instance.AudioMixers.transmission != newTransmission)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Mixer");

				Instance.AudioMixers.transmission = newTransmission;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.indentLevel++;

			if (!Instance.AudioMixers.transmission)
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(new GUIContent("Default", "The audio mixer used as default"), Instance.AudioMixers.Transmission, typeof(AudioMixerGroup), false);
				EditorGUI.EndDisabledGroup();

				if (!Instance.AudioMixers.Transmission)
					EditorGUILayout.HelpBox("It seems like the default audio mixer is missing! To fix this issue you have to go to the settings panel and assign one.", MessageType.Info);
			}

			float newTransmissionVolume = ToolkitEditorUtility.Slider("Volume", Instance.AudioMixers.TransmissionVolume * 100f, 0f, 100f, "%", "Percentage", Instance, "Change Volume") * .01f;

			if (Instance.AudioMixers.TransmissionVolume != newTransmissionVolume)
				Instance.AudioMixers.TransmissionVolume = newTransmissionVolume;

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();


			EditorGUILayout.BeginVertical(GUI.skin.box);

			AudioMixerGroup newBrakeEffects = EditorGUILayout.ObjectField("Brakes", Instance.AudioMixers.brakeEffects, typeof(AudioMixerGroup), false) as AudioMixerGroup;

			if (Instance.AudioMixers.brakeEffects != newBrakeEffects)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Mixer");

				Instance.AudioMixers.brakeEffects = newBrakeEffects;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.indentLevel++;

			if (!Instance.AudioMixers.brakeEffects)
			{
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(new GUIContent("Default", "The audio mixer used as default"), Instance.AudioMixers.BrakeEffects, typeof(AudioMixerGroup), false);
				EditorGUI.EndDisabledGroup();

				if (!Instance.AudioMixers.BrakeEffects)
					EditorGUILayout.HelpBox("It seems like the default audio mixer is missing! To fix this issue you have to go to the settings panel and assign one.", MessageType.Info);
			}

			float newBrakesVolume = ToolkitEditorUtility.Slider("Volume", Instance.AudioMixers.BrakeEffectsVolume * 100f, 0f, 100f, "%", "Percentage", Instance, "Change Volume") * .01f;

			if (Instance.AudioMixers.BrakeEffectsVolume != newBrakesVolume)
				Instance.AudioMixers.BrakeEffectsVolume = newBrakesVolume;

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();

			if (!Instance.IsElectric)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);

				AudioMixerGroup newChargersEffects = EditorGUILayout.ObjectField("Chargers Effects", Instance.AudioMixers.chargersEffects, typeof(AudioMixerGroup), false) as AudioMixerGroup;

				if (Instance.AudioMixers.chargersEffects != newChargersEffects)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Mixer");

					Instance.AudioMixers.chargersEffects = newChargersEffects;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel++;

				if (!Instance.AudioMixers.chargersEffects)
				{
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ObjectField(new GUIContent("Default", "The audio mixer used as default"), Instance.AudioMixers.ChargersEffects, typeof(AudioMixerGroup), false);
					EditorGUI.EndDisabledGroup();

					if (!Instance.AudioMixers.ChargersEffects)
						EditorGUILayout.HelpBox("It seems like the default audio mixer is missing! To fix this issue you have to go to the settings panel and assign one.", MessageType.Info);
				}

				float newChargersEffectsVolume = ToolkitEditorUtility.Slider("Volume", Instance.AudioMixers.ChargersEffectsVolume * 100f, 0f, 100f, "%", "Percentage", Instance, "Change Volume") * .01f;

				if (Instance.AudioMixers.ChargersEffectsVolume != newChargersEffectsVolume)
					Instance.AudioMixers.ChargersEffectsVolume = newChargersEffectsVolume;

				EditorGUI.indentLevel--;

				EditorGUILayout.EndVertical();
				EditorGUILayout.BeginVertical(GUI.skin.box);

				AudioMixerGroup newTurbochargerEffects = EditorGUILayout.ObjectField("Turbocharger", Instance.AudioMixers.turbocharger, typeof(AudioMixerGroup), false) as AudioMixerGroup;

				if (Instance.AudioMixers.turbocharger != newTurbochargerEffects)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Mixer");

					Instance.AudioMixers.turbocharger = newTurbochargerEffects;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel++;

				if (!Instance.AudioMixers.turbocharger)
				{
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ObjectField(new GUIContent("Default", "The audio mixer used as default"), Instance.AudioMixers.Turbocharger, typeof(AudioMixerGroup), false);
					EditorGUI.EndDisabledGroup();

					if (!Instance.AudioMixers.Turbocharger)
						EditorGUILayout.HelpBox("It seems like the default audio mixer is missing! To fix this issue you have to go to the settings panel and assign one.", MessageType.Info);
				}

				float newTurbochargerVolume = ToolkitEditorUtility.Slider("Volume", Instance.AudioMixers.TurbochargerVolume * 100f, 0f, 100f, "%", "Percentage", Instance, "Change Volume") * .01f;

				if (Instance.AudioMixers.TurbochargerVolume != newTurbochargerVolume)
					Instance.AudioMixers.TurbochargerVolume = newTurbochargerVolume;

				EditorGUI.indentLevel--;

				EditorGUILayout.EndVertical();
				EditorGUILayout.BeginVertical(GUI.skin.box);

				AudioMixerGroup newSuperchargerEffects = EditorGUILayout.ObjectField("Supercharger", Instance.AudioMixers.supercharger, typeof(AudioMixerGroup), false) as AudioMixerGroup;

				if (Instance.AudioMixers.supercharger != newSuperchargerEffects)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Mixer");

					Instance.AudioMixers.supercharger = newSuperchargerEffects;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel++;

				if (!Instance.AudioMixers.supercharger)
				{
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ObjectField(new GUIContent("Default", "The audio mixer used as default"), Instance.AudioMixers.Supercharger, typeof(AudioMixerGroup), false);
					EditorGUI.EndDisabledGroup();

					if (!Instance.AudioMixers.Supercharger)
						EditorGUILayout.HelpBox("It seems like the default audio mixer is missing! To fix this issue you have to go to the settings panel and assign one.", MessageType.Info);
				}

				float newSuperchargerVolume = ToolkitEditorUtility.Slider("Volume", Instance.AudioMixers.SuperchargerVolume * 100f, 0f, 100f, "%", "Percentage", Instance, "Change Volume") * .01f;

				if (Instance.AudioMixers.SuperchargerVolume != newSuperchargerVolume)
					Instance.AudioMixers.SuperchargerVolume = newSuperchargerVolume;

				EditorGUI.indentLevel--;

				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.EndVertical();
		}
#if !MVC_COMMUNITY
		internal void AIEditor()
		{
			aiFoldout = EnableFoldout();

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Behaviours", EditorStyles.miniBoldLabel);
			EditorGUILayout.Space();

			EditorGUI.indentLevel++;

			aiTypes ??= ToolkitReflection.GetSubClasses<VehicleAIBehaviour>().ToArray();

			string[] aiTypeNames = new string[aiTypes.Length + 1];

			for (int i = 0; i < aiTypeNames.Length; i++)
				aiTypeNames[i] = i > 0 ? aiTypes[i - 1].Name.Replace("VehicleAI", "").SpacePascalCase() : "Choose AI Behaviour...";

			newAITypeIndex = EditorGUILayout.Popup(newAITypeIndex + 1, aiTypeNames) - 1;

			var aiBehaviours = Instance.AIBehaviours;

			if (EditorApplication.isPlaying)
				EditorGUILayout.HelpBox("You are currently in play mode, therefore you can't add an AI controller to this vehicle!", MessageType.Info);
			else if (aiBehaviours.Length < 1)
				EditorGUILayout.HelpBox("In case you want this vehicle to be controlled by the AI system, press on the \"Add AI Controller\" button.", MessageType.Info, true);

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUI.BeginDisabledGroup(newAITypeIndex < 0);

			int aiBehaviourIndex = newAITypeIndex > -1 && newAITypeIndex < aiTypes.Length ? Array.FindIndex(aiBehaviours, ai => aiTypes[newAITypeIndex] == ai.GetType()) : -1;
			bool aiBehaviourExist = aiBehaviourIndex > -1;

			if (GUILayout.Button(aiBehaviourExist ? "Remove AI Behaviour" : "Add AI Behaviour", ToolkitEditorUtility.IndentedButton))
			{
				if (aiBehaviourExist)
					Undo.DestroyObjectImmediate(aiBehaviours[aiBehaviourIndex]);
				else
					Undo.AddComponent(Instance.gameObject, aiTypes[newAITypeIndex]);

				EditorUtility.SetDirty(Instance.gameObject);

				newAITypeIndex = default;
			}

			EditorGUI.EndDisabledGroup();

			if (aiBehaviours.Length > 1 && GUILayout.Button("Remove All AI Behaviours", ToolkitEditorUtility.IndentedButton))
			{
				int undoGroup = Undo.GetCurrentGroup();

				Undo.SetCurrentGroupName("Remove AI");

				foreach (var ai in aiBehaviours)
					Undo.DestroyObjectImmediate(ai);

				Undo.CollapseUndoOperations(undoGroup);
				EditorUtility.SetDirty(Instance.gameObject);
			}

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();

			if (aiBehaviours.Length > 0)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("Parameters", EditorStyles.miniBoldLabel);
				EditorGUILayout.Space();

				EditorGUI.indentLevel++;

				string[] aiBehaviourNames = new string[aiBehaviours.Length + 1];

				for (int i = 0; i < aiBehaviourNames.Length; i++)
					aiBehaviourNames[i] = i > 0 ? aiBehaviours[i - 1].GetType().Name.Replace("VehicleAI", "").SpacePascalCase() : "Disabled";

				int newActiveAIBehaviourIndex = EditorGUILayout.Popup(new GUIContent($"{(EditorApplication.isPlaying ? "Runtime" : "Default")} Behaviour", "The default active AI behaviour at Runtime"), Instance.ActiveAIBehaviourIndex + 1, aiBehaviourNames) - 1;

				if (Instance.ActiveAIBehaviourIndex != newActiveAIBehaviourIndex)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change AI");

					Instance.ActiveAIBehaviourIndex = newActiveAIBehaviourIndex;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
				EditorGUILayout.EndVertical();
			}
		}
		/*internal void DamageEditor()
		{
			damageFoldout = EnableFoldout();

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();

			bool newOverrideVertexRandomization = EditorGUILayout.Toggle(Instance.Damage.overrideVertexRandomization);

			if (Instance.Damage.overrideVertexRandomization != newOverrideVertexRandomization)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Switch Randomization");

				Instance.Damage.overrideVertexRandomization = newOverrideVertexRandomization;
		
				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.indentLevel--;
			EditorGUI.indentLevel--;

			EditorGUI.BeginDisabledGroup(!Instance.Damage.overrideVertexRandomization);

			float newVertexRandomization = math.max(VehicleEditorUtility.NumberField(new GUIContent("Vertex Randomization", "The damage vertices randomization intensity inside the contact point radius"), Instance.Damage.overrideVertexRandomization ? Instance.Damage.vertexRandomization : Settings.damageVertexRandomization));

			if (Instance.Damage.overrideVertexRandomization && Instance.Damage.vertexRandomization != newVertexRandomization)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Randomization");

				Instance.Damage.vertexRandomization = newVertexRandomization;
		
				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.EndDisabledGroup();

			EditorGUI.indentLevel++;
			EditorGUI.indentLevel++;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			bool newOverrideRadius = EditorGUILayout.Toggle(Instance.Damage.overrideRadius);

			if (Instance.Damage.overrideRadius != newOverrideRadius)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Switch Radius");

				Instance.Damage.overrideRadius = newOverrideRadius;
		
				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.indentLevel--;
			EditorGUI.indentLevel--;

			EditorGUI.BeginDisabledGroup(!Instance.Damage.overrideRadius);

			float newRadius = VehicleEditorUtility.NumberField(new GUIContent("Radius", "The maximum damage radius around the main contact point"), Instance.Damage.radius, Utility.Units.DistanceAccurate, 3, Instance, "Change Radius");

			if (Instance.Damage.overrideRadius && Instance.Damage.radius != newRadius)
				Instance.Damage.radius = newRadius;

			EditorGUI.EndDisabledGroup();

			EditorGUI.indentLevel++;
			EditorGUI.indentLevel++;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
		}*/
		internal void InteriorEditor()
		{
			interiorFoldout = EnableFoldout();

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Dashboard Components", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			EditorGUILayout.BeginVertical(GUI.skin.box);

			if (Instance.Interior.SteeringWheel.transform)
			{
				EditorGUILayout.LabelField("Steering Wheel", EditorStyles.miniBoldLabel);

				EditorGUI.indentLevel++;
			}

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying && Instance.Interior.SteeringWheel.transform);

			Transform newSteeringWheel = EditorGUILayout.ObjectField(new GUIContent(Instance.Interior.SteeringWheel.transform ? "Transform" : "Steering Wheel", "The interior steering wheel transform"), Instance.Interior.SteeringWheel.transform, typeof(Transform), true) as Transform;

			if (Instance.Interior.SteeringWheel.transform != newSteeringWheel)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Transform");

				Instance.Interior.SteeringWheel.transform = newSteeringWheel;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.EndDisabledGroup();

			if (Instance.Interior.SteeringWheel.transform)
				InteriorComponentEditor(Instance.Interior.SteeringWheel, "Steering Wheel", "steering angle");

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			if (Instance.Interior.RPMNeedle.transform)
			{
				EditorGUILayout.LabelField("RPM Needle", EditorStyles.miniBoldLabel);

				EditorGUI.indentLevel++;
			}

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying && Instance.Interior.RPMNeedle.transform);

			Transform newRPMNeedle = EditorGUILayout.ObjectField(new GUIContent(Instance.Interior.RPMNeedle.transform ? "Transform" : "RPM Needle", "The interior rpm meter needle transform"), Instance.Interior.RPMNeedle.transform, typeof(Transform), true) as Transform;

			if (Instance.Interior.RPMNeedle.transform != newRPMNeedle)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Transform");

				Instance.Interior.RPMNeedle.transform = newRPMNeedle;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.EndDisabledGroup();

			if (Instance.Interior.RPMNeedle.transform)
				InteriorComponentEditor(Instance.Interior.RPMNeedle, "RPM Needle", "maximum RPM");

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			if (Instance.Interior.SpeedNeedle.transform)
			{
				EditorGUILayout.LabelField("Speed Needle", EditorStyles.miniBoldLabel);

				EditorGUI.indentLevel++;
			}

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying && Instance.Interior.SpeedNeedle.transform);

			Transform newSpeedNeedle = EditorGUILayout.ObjectField(new GUIContent(Instance.Interior.SpeedNeedle.transform ? "Transform" : "Speed Needle", "The interior speed meter needle transform"), Instance.Interior.SpeedNeedle.transform, typeof(Transform), true) as Transform;

			if (Instance.Interior.SpeedNeedle.transform != newSpeedNeedle)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Transform");

				Instance.Interior.SpeedNeedle.transform = newSpeedNeedle;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.EndDisabledGroup();

			if (Instance.Interior.SpeedNeedle.transform)
				InteriorComponentEditor(Instance.Interior.SpeedNeedle, "Speed Needle", "top speed");

			EditorGUILayout.EndVertical();

			if (Settings.useFuelSystem)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);

				if (Instance.Interior.FuelNeedle.transform)
				{
					EditorGUILayout.LabelField("Fuel Needle", EditorStyles.miniBoldLabel);

					EditorGUI.indentLevel++;
				}

				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying && Instance.Interior.FuelNeedle.transform);

				Transform newFuelNeedle = EditorGUILayout.ObjectField(new GUIContent(Instance.Interior.FuelNeedle.transform ? "Transform" : "Fuel Needle", "The interior fuel tank needle transform"), Instance.Interior.FuelNeedle.transform, typeof(Transform), true) as Transform;

				if (Instance.Interior.FuelNeedle.transform != newFuelNeedle)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Transform");

					Instance.Interior.FuelNeedle.transform = newFuelNeedle;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.EndDisabledGroup();

				if (Instance.Interior.FuelNeedle.transform)
					InteriorComponentEditor(Instance.Interior.FuelNeedle, "Fuel Needle", "fuel capacity");

				EditorGUILayout.EndVertical();
			}

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Speed-o-Meter Lights", EditorStyles.miniBoldLabel);
			EditorGUI.BeginDisabledGroup(interiorLightEdit);

			EditorGUI.indentLevel++;

			EditorGUILayout.BeginVertical(GUI.skin.box);

			if (Instance.Interior.IndicatorLeft.renderer)
			{
				EditorGUILayout.LabelField("Indicator Left", EditorStyles.miniBoldLabel);

				EditorGUI.indentLevel++;
			}

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			MeshRenderer newIndicatorLeft = EditorGUILayout.ObjectField(new GUIContent(Instance.Interior.IndicatorLeft.renderer ? "Renderer" : "Indicator Left", "The speedometer left indicator light"), Instance.Interior.IndicatorLeft.renderer, typeof(MeshRenderer), true) as MeshRenderer;

			if (Instance.Interior.IndicatorLeft.renderer != newIndicatorLeft)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Renderer");

				Instance.Interior.IndicatorLeft.renderer = newIndicatorLeft;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.EndDisabledGroup();

			if (Instance.Interior.IndicatorLeft.renderer)
			{
				string[] materialNames = new string[Instance.Interior.IndicatorLeft.renderer.sharedMaterials.Length];

				for (int i = 0; i < Instance.Interior.IndicatorLeft.renderer.sharedMaterials.Length; i++)
					materialNames[i] = $"{i + 1}. {Instance.Interior.IndicatorLeft.renderer.sharedMaterials[i].name}";

				int newMaterialIndex = EditorGUILayout.Popup(new GUIContent("Material", "The light emissive material"), Instance.Interior.IndicatorLeft.MaterialIndex, materialNames);

				if (Instance.Interior.IndicatorLeft.MaterialIndex != newMaterialIndex)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Material");

					Instance.Interior.IndicatorLeft.MaterialIndex = newMaterialIndex;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUILayout.BeginHorizontal();
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(interiorLightEdit && Instance.Interior.IndicatorLeft.renderer != interiorLightEdit);
				EditorGUI.BeginDisabledGroup(!interiorLightEdit);

				Color newEmissionColor = EditorGUILayout.ColorField(new GUIContent("Emission Color", "The light emission color"), Instance.Interior.IndicatorLeft.emissionColor, false, false, true);

				if (Instance.Interior.IndicatorLeft.emissionColor != newEmissionColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					Instance.Interior.IndicatorLeft.emissionColor = newEmissionColor;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.EndDisabledGroup();

				if (interiorLightEdit && Instance.Interior.IndicatorLeft.renderer == interiorLightEdit)
				{
					Instance.Interior.IndicatorLeft.RefreshMaterialEmissionColorPropertyName();

					interiorLightEdit.sharedMaterials[Instance.Interior.IndicatorLeft.MaterialIndex].SetColor(Instance.Interior.IndicatorLeft.GetMaterialEmissionColorPropertyName(), newEmissionColor);

					if (GUILayout.Button(EditorUtilities.Icons.Save, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						interiorLightEdit.sharedMaterials[Instance.Interior.IndicatorLeft.MaterialIndex].SetColor(Instance.Interior.IndicatorLeft.GetMaterialEmissionColorPropertyName(), Color.black);

						SaveInteriorLight();
					}
				}
				else
				{
					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						EditInteriorLight(Instance.Interior.IndicatorLeft.renderer);
				}

				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(interiorLightEdit);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			if (Instance.Interior.IndicatorRight.renderer)
			{
				EditorGUILayout.LabelField("Indicator Right", EditorStyles.miniBoldLabel);

				EditorGUI.indentLevel++;
			}

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			MeshRenderer newIndicatorRight = EditorGUILayout.ObjectField(new GUIContent(Instance.Interior.IndicatorRight.renderer ? "Renderer" : "Indicator Right", "The speedometer right indicator light"), Instance.Interior.IndicatorRight.renderer, typeof(MeshRenderer), true) as MeshRenderer;

			if (Instance.Interior.IndicatorRight.renderer != newIndicatorRight)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Renderer");

				Instance.Interior.IndicatorRight.renderer = newIndicatorRight;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.EndDisabledGroup();

			if (Instance.Interior.IndicatorRight.renderer)
			{
				string[] materialNames = new string[Instance.Interior.IndicatorRight.renderer.sharedMaterials.Length];

				for (int i = 0; i < Instance.Interior.IndicatorRight.renderer.sharedMaterials.Length; i++)
					materialNames[i] = Instance.Interior.IndicatorRight.renderer.sharedMaterials[i] ? $"{i + 1}. {Instance.Interior.IndicatorRight.renderer.sharedMaterials[i].name}" : $"{i + 1}. NULL";

				int newMaterialIndex = EditorGUILayout.Popup(new GUIContent("Material", "The light emissive material"), Instance.Interior.IndicatorRight.MaterialIndex, materialNames);

				if (Instance.Interior.IndicatorRight.MaterialIndex != newMaterialIndex)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Material");

					Instance.Interior.IndicatorRight.MaterialIndex = newMaterialIndex;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUILayout.BeginHorizontal();
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(interiorLightEdit && Instance.Interior.IndicatorRight.renderer != interiorLightEdit);
				EditorGUI.BeginDisabledGroup(!interiorLightEdit);

				Color newEmissionColor = EditorGUILayout.ColorField(new GUIContent("Emission Color", "The light emission color"), Instance.Interior.IndicatorRight.emissionColor, false, false, true);

				if (Instance.Interior.IndicatorRight.emissionColor != newEmissionColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					Instance.Interior.IndicatorRight.emissionColor = newEmissionColor;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.EndDisabledGroup();

				if (interiorLightEdit && Instance.Interior.IndicatorRight.renderer == interiorLightEdit)
				{
					Instance.Interior.IndicatorRight.RefreshMaterialEmissionColorPropertyName();

					interiorLightEdit.sharedMaterials[Instance.Interior.IndicatorRight.MaterialIndex].SetColor(Instance.Interior.IndicatorRight.GetMaterialEmissionColorPropertyName(), newEmissionColor);

					if (GUILayout.Button(EditorUtilities.Icons.Save, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						interiorLightEdit.sharedMaterials[Instance.Interior.IndicatorRight.MaterialIndex].SetColor(Instance.Interior.IndicatorRight.GetMaterialEmissionColorPropertyName(), Color.black);

						SaveInteriorLight();
					}
				}
				else
				{
					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						EditInteriorLight(Instance.Interior.IndicatorRight.renderer);
				}

				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(interiorLightEdit);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);

			if (Instance.Interior.Handbrake.renderer)
			{
				EditorGUILayout.LabelField("Handbrake", EditorStyles.miniBoldLabel);

				EditorGUI.indentLevel++;
			}

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			MeshRenderer newHandbrake = EditorGUILayout.ObjectField(new GUIContent(Instance.Interior.Handbrake.renderer ? "Renderer" : "Handbrake", "The speedometer handbrake light"), Instance.Interior.Handbrake.renderer, typeof(MeshRenderer), true) as MeshRenderer;

			if (Instance.Interior.Handbrake.renderer != newHandbrake)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Renderer");

				Instance.Interior.Handbrake.renderer = newHandbrake;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.EndDisabledGroup();

			if (Instance.Interior.Handbrake.renderer)
			{
				string[] materialNames = new string[Instance.Interior.Handbrake.renderer.sharedMaterials.Length];

				for (int i = 0; i < Instance.Interior.Handbrake.renderer.sharedMaterials.Length; i++)
					materialNames[i] = $"{i + 1}. {Instance.Interior.Handbrake.renderer.sharedMaterials[i].name}";

				int newMaterialIndex = EditorGUILayout.Popup(new GUIContent("Material", "The light emissive material"), Instance.Interior.Handbrake.MaterialIndex, materialNames);

				if (Instance.Interior.Handbrake.MaterialIndex != newMaterialIndex)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Material");

					Instance.Interior.Handbrake.MaterialIndex = newMaterialIndex;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUILayout.BeginHorizontal();
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(interiorLightEdit && Instance.Interior.Handbrake.renderer != interiorLightEdit);
				EditorGUI.BeginDisabledGroup(!interiorLightEdit);

				Color newEmissionColor = EditorGUILayout.ColorField(new GUIContent("Emission Color", "The light emission color"), Instance.Interior.Handbrake.emissionColor, false, false, true);

				if (Instance.Interior.Handbrake.emissionColor != newEmissionColor)
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Color");

					Instance.Interior.Handbrake.emissionColor = newEmissionColor;

					EditorUtility.SetDirty(Instance);
				}

				EditorGUI.EndDisabledGroup();

				if (interiorLightEdit && Instance.Interior.Handbrake.renderer == interiorLightEdit)
				{
					Instance.Interior.Handbrake.RefreshMaterialEmissionColorPropertyName();

					interiorLightEdit.sharedMaterials[Instance.Interior.Handbrake.MaterialIndex].SetColor(Instance.Interior.Handbrake.GetMaterialEmissionColorPropertyName(), newEmissionColor);

					if (GUILayout.Button(EditorUtilities.Icons.Save, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					{
						interiorLightEdit.sharedMaterials[Instance.Interior.Handbrake.MaterialIndex].SetColor(Instance.Interior.Handbrake.GetMaterialEmissionColorPropertyName(), Color.black);

						SaveInteriorLight();
					}
				}
				else
				{
					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						EditInteriorLight(Instance.Interior.Handbrake.renderer);
				}

				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(interiorLightEdit);
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.Space();

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndVertical();
			EditorGUI.EndDisabledGroup();

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
		}
		internal void InteriorComponentEditor(Vehicle.InteriorModule.ComponentModule component, string name, string targetName)
		{
			Utility.Axis3 newRotationAxis = (Utility.Axis3)EditorGUILayout.EnumPopup(new GUIContent("Axis", $"The {name} transform rotation axis"), component.rotationAxis);

			if (component.rotationAxis != newRotationAxis)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Change Axis");

				component.rotationAxis = newRotationAxis;

				EditorUtility.SetDirty(Instance);
			}

			float newStartAngle = ToolkitEditorUtility.Slider(new GUIContent("Start Angle", $"The {name} transform idle rotation angle"), component.StartAngle, -360f, 360f, "°", "Degrees", Instance, "Change Angle");

			if (component.StartAngle != newStartAngle)
				component.StartAngle = newStartAngle;

			float newTargetAngle = ToolkitEditorUtility.Slider(new GUIContent("Target Angle", $"The {name} transform maximum rotation angle"), component.TargetAngle, -360f, 360f, "°", "Degrees", Instance, "Change Angle");

			if (component.TargetAngle != newTargetAngle)
				component.TargetAngle = newTargetAngle;

			bool newOverrideTarget = EditorGUILayout.ToggleLeft("Override Target", component.overrideTarget);

			if (component.overrideTarget != newOverrideTarget)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Switch Override");

				component.overrideTarget = newOverrideTarget;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.indentLevel++;

			EditorGUI.BeginDisabledGroup(!component.overrideTarget);

			float newTarget = ToolkitEditorUtility.NumberField(new GUIContent("Target", $"The {targetName} target of the {name}"), newOverrideTarget ? component.Target : Instance.Steering.MaximumSteerAngle, "°", "Degrees", 1, Instance, "Change Target");

			if (component.Target != newTarget)
				component.Target = newTarget;

			EditorGUI.EndDisabledGroup();

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();

			EditorGUI.indentLevel--;
		}
		internal void DriverIKEditor()
		{
			driverIKFoldout = EnableFoldout();

			float orgLabelWidth = EditorGUIUtility.labelWidth;

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal(GUI.skin.box);

			VehicleDriver newDriver;

			if (Instance.DriverIK.Driver)
			{
				newDriver = Instance.DriverIK.Driver;
				EditorGUIUtility.labelWidth = 50f;

				EditorGUILayout.LabelField(new GUIContent("Driver", "The vehicle driver"), EditorStyles.miniBoldLabel);

				EditorGUIUtility.labelWidth = orgLabelWidth;

				if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					ToolkitEditorUtility.SelectObject(Instance.DriverIK.Driver.gameObject);

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					newDriver = null;
			}
			else
				newDriver = EditorGUILayout.ObjectField(new GUIContent("Driver", "The vehicle driver"), Instance.DriverIK.Driver, typeof(VehicleDriver), true) as VehicleDriver;

			if (Instance.DriverIK.Driver != newDriver)
			{
				if (newDriver && PrefabUtility.GetPrefabInstanceStatus(newDriver) == PrefabInstanceStatus.NotAPrefab)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The new driver cannot be a prefab that is located in your project folders! Please assign an object that exists already within the current scene.", "Okay");
				else
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Change Driver");

					if (Instance.DriverIK.Driver)
						EditorUtility.SetDirty(Instance.DriverIK.Driver);

					Instance.DriverIK.Driver = newDriver;

					EditorUtility.SetDirty(Instance);
				}
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(new GUIContent("IK Pivots", "Add & edit the driver's pivot points"), EditorStyles.miniBoldLabel);

			if (GUILayout.Button(IKPivotsFoldout ? EditorUtilities.Icons.ChevronUp : EditorUtilities.Icons.ChevronDown, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				IKPivotsFoldout = !IKPivotsFoldout;

			EditorGUILayout.EndHorizontal();

			if (IKPivotsFoldout)
				IKPivotsEditor();

			EditorGUILayout.EndVertical();
		}
		internal void IKPivotsEditor()
		{
			IKPivotsFoldout = EnableInternalFoldout();

			EditorGUILayout.Space();

			if (Instance.DriverIK.Driver)
			{
				EditorGUILayout.BeginVertical(GUI.skin.box);
				EditorGUILayout.LabelField("Driver", EditorStyles.miniBoldLabel);

				EditorGUI.indentLevel++;

				float newLookAtPositionHeight = ToolkitEditorUtility.Slider(new GUIContent("Look At Height", "The height factor of the driver's eye sight"), Instance.DriverIK.Driver.LookAtPositionHeight, 0f, 1f, Instance.DriverIK.Driver, "Change Height");

				if (Instance.DriverIK.Driver.LookAtPositionHeight != newLookAtPositionHeight)
					Instance.DriverIK.Driver.LookAtPositionHeight = newLookAtPositionHeight;

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();
				EditorGUILayout.EndVertical();
			}

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Steering Wheel", EditorStyles.miniBoldLabel);

			if (Instance.Interior.SteeringWheel.transform)
			{
				if (Instance.DriverIK.HasAllSteeringWheelPivots)
				{
					EditorGUI.indentLevel++;

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Left Hand Pivot");

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						ToolkitEditorUtility.SelectObject(Instance.DriverIK.leftHandSteeringWheelPivot.gameObject);

					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Right Hand Pivot");

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						ToolkitEditorUtility.SelectObject(Instance.DriverIK.rightHandSteeringWheelPivot.gameObject);

					EditorGUILayout.EndHorizontal();

					if (!Instance.Interior.SteeringWheel.transform.GetComponent<Animator>())
						EditorGUILayout.HelpBox("To make the IK system function smoothly, you might need to animate the IK pivots of the steering wheel.", MessageType.Info);
					else
					{
						string newSteeringWheelAngleParameter = EditorGUILayout.TextField(new GUIContent("Angle Parameter", "The steering wheel rotation angle animator parameter"), Instance.DriverIK.steeringWheelAngleParameter);

						if (Instance.DriverIK.steeringWheelAngleParameter != newSteeringWheelAngleParameter)
						{
							Undo.RegisterCompleteObjectUndo(Instance, "Change Parameter");

							Instance.DriverIK.steeringWheelAngleParameter = newSteeringWheelAngleParameter;

							EditorUtility.SetDirty(Instance);
						}

						EditorGUILayout.Space();
					}

					EditorGUI.indentLevel--;
				}
				else if (GUILayout.Button("Create Steering Wheel Pivots"))
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Generate Pivots");

					Transform steeringWheel = Instance.Interior.SteeringWheel.transform;
					Bounds steeringWheelBounds = Utility.GetObjectBounds(steeringWheel.gameObject, false, false);
					Vector3 forward = steeringWheel.forward;
					Vector3 right = Instance.transform.right;
					Vector3 up = steeringWheel.up;
					float radius = steeringWheelBounds.extents.x;

					switch (Instance.Interior.SteeringWheel.rotationAxis)
					{
						case Utility.Axis3.X:
							forward = steeringWheel.right;

							break;

						case Utility.Axis3.Y:
							forward = steeringWheel.up;
							up = steeringWheel.forward;

							break;
					}

					Transform tempPivot = steeringWheel.Find("LeftHandPivot");

					if (!tempPivot)
					{
						tempPivot = new GameObject("LeftHandPivot").transform;
						tempPivot.parent = steeringWheel;
					}

					Instance.DriverIK.leftHandSteeringWheelPivot = tempPivot.GetComponent<VehicleIKPivot>();

					if (!Instance.DriverIK.leftHandSteeringWheelPivot)
						Instance.DriverIK.leftHandSteeringWheelPivot = tempPivot.gameObject.AddComponent<VehicleIKPivot>();

					tempPivot = steeringWheel.Find("RightHandPivot");

					if (!tempPivot)
					{
						tempPivot = new GameObject("RightHandPivot").transform;
						tempPivot.parent = steeringWheel;
					}

					Instance.DriverIK.rightHandSteeringWheelPivot = tempPivot.GetComponent<VehicleIKPivot>();

					if (!Instance.DriverIK.rightHandSteeringWheelPivot)
						Instance.DriverIK.rightHandSteeringWheelPivot = tempPivot.gameObject.AddComponent<VehicleIKPivot>();

					Instance.DriverIK.leftHandSteeringWheelPivot.transform.SetPositionAndRotation(steeringWheelBounds.center + (up - right) * radius, Quaternion.Euler(forward * 45f));
					Instance.DriverIK.rightHandSteeringWheelPivot.transform.SetPositionAndRotation(steeringWheelBounds.center + (up + right) * radius, Quaternion.Euler(forward * -45f));
					Instance.RefreshIKPivots();
					EditorUtility.SetDirty(Instance);
				}
			}
			else
			{
				EditorGUILayout.HelpBox("To modify Steering Wheel IK Pivots, you'll need to assign one on the Interior foldout first!", MessageType.Info, true);

				if (GUILayout.Button("Go to Interior"))
					interiorFoldout = EnableFoldout();
			}

			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("Feet Pedals", EditorStyles.miniBoldLabel);

			if (Instance.Chassis)
			{
				if (Instance.DriverIK.HasAllFeetPivots)
				{
					EditorGUI.indentLevel++;

					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Left Foot Pivot");

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						ToolkitEditorUtility.SelectObject(Instance.DriverIK.leftFootPivot.gameObject);

					EditorGUILayout.EndHorizontal();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField("Right Foot Pivot");

					if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						ToolkitEditorUtility.SelectObject(Instance.DriverIK.rightFootPivot.gameObject);

					EditorGUILayout.EndHorizontal();

					EditorGUI.indentLevel--;

					EditorGUILayout.Space();
				}
				else if (GUILayout.Button("Create Feet Pivots"))
				{
					Undo.RegisterCompleteObjectUndo(Instance, "Generate Pivots");

					Instance.RefreshIKPivots();

					Transform ikPivotsParent = Instance.Chassis.transform;
					Transform tempPivot = ikPivotsParent.Find("LeftFootPivot");

					if (!tempPivot)
					{
						tempPivot = new GameObject("LeftFootPivot").transform;
						tempPivot.parent = ikPivotsParent;
					}

					Instance.DriverIK.leftFootPivot = tempPivot.GetComponent<VehicleIKPivot>();

					if (!Instance.DriverIK.leftFootPivot)
						Instance.DriverIK.leftFootPivot = tempPivot.gameObject.AddComponent<VehicleIKPivot>();

					tempPivot = ikPivotsParent.Find("RightFootPivot");

					if (!tempPivot)
					{
						tempPivot = new GameObject("RightFootPivot").transform;
						tempPivot.parent = ikPivotsParent;
					}

					Instance.DriverIK.rightFootPivot = tempPivot.GetComponent<VehicleIKPivot>();

					if (!Instance.DriverIK.rightFootPivot)
						Instance.DriverIK.rightFootPivot = tempPivot.gameObject.AddComponent<VehicleIKPivot>();

					float pivotsHeight = (Instance.FrontWheels.Length > 0 ? Instance.FrontWheels : Instance.Wheels).Average(wheel => wheel.Instance.Radius) * 1.5f;

					Instance.DriverIK.leftFootPivot.transform.SetLocalPositionAndRotation(new Vector3(Mathf.Sign(Instance.DriverIK.Driver.transform.localPosition.x) * Instance.ChassisBounds.extents.x * .9f, pivotsHeight, .25f), Quaternion.Euler(-30f, 0f, 0f));
					Instance.DriverIK.rightFootPivot.transform.SetLocalPositionAndRotation(new Vector3(Mathf.Sign(Instance.DriverIK.Driver.transform.localPosition.x) * Instance.ChassisBounds.extents.x * .5f, pivotsHeight, .25f), Quaternion.Euler(-30f, 0f, 0f));

					Instance.RefreshIKPivots();
					EditorUtility.SetDirty(Instance);
				}
			}
			else
			{
				EditorGUILayout.HelpBox("To modify Feet IK Pivots, you'll need to assign a Chassis from the Components foldout!", MessageType.Info, true);

				if (GUILayout.Button("Go to Components"))
					componentsFoldout = EnableFoldout();
			}

			EditorGUILayout.EndVertical();

			if (!Instance.DriverIK.Driver)
				EditorGUILayout.HelpBox("For a better pivots editing experience, it is advised to add a Driver instance to visualize changes.", MessageType.Info);
		}
#endif
		internal void TrailerLinkEditor()
		{
			trailerLinkFoldout = EnableFoldout();

			EditorGUILayout.Space();

			VehicleTrailerLink trailerLink = Instance.TrailerLink;

			if (trailerLink)
			{
				EditorGUI.indentLevel++;

				float newLinkRadius = ToolkitEditorUtility.NumberField(new GUIContent("Radius", "The trailer link detection radius"), trailerLink.LinkRadius * 1000f, Utility.Units.SizeAccurate, true, Instance, "Change Radius") * .001f;

				if (trailerLink.LinkRadius != newLinkRadius)
					trailerLink.LinkRadius = newLinkRadius;

				Vector3 newLinkPoint = EditorGUILayout.Vector3Field(new GUIContent(EditorGUIUtility.IconContent("MoveTool")) { tooltip = "The trailer link position" }, trailerLink.LinkPoint);

				if (trailerLink.LinkPoint != newLinkPoint)
				{
					Undo.RegisterCompleteObjectUndo(trailerLink, "Change Position");

					trailerLink.LinkPoint = newLinkPoint;

					EditorUtility.SetDirty(trailerLink);
				}

				EditorGUI.indentLevel--;

				EditorGUILayout.Space();

				if (EditorApplication.isPlaying)
					EditorGUILayout.HelpBox("You are currently in play mode, therefore you can't remove the trailer link of this vehicle!", MessageType.Info);

				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				if (GUILayout.Button("Remove Trailer Link"))
				{
					Undo.DestroyObjectImmediate(trailerLink);
					EditorUtility.SetDirty(Instance.gameObject);
				}

				EditorGUI.EndDisabledGroup();
			}
			else
			{
				if (EditorApplication.isPlaying)
					EditorGUILayout.HelpBox("You are currently in play mode, therefore you can't add a trailer link to this vehicle!", MessageType.Info);
				else
					EditorGUILayout.HelpBox("In case you want this vehicle to have a trailer link, press on the \"Add Trailer Link\" button.", MessageType.Info, true);

				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

				if (GUILayout.Button("Add Trailer Link"))
				{
					Undo.AddComponent<VehicleTrailerLink>(Instance.gameObject).LinkPoint = new(0f, Instance.Bounds.extents.y / 3f, -Instance.Bounds.extents.z);
					EditorUtility.SetDirty(Instance.gameObject);

					Tools.current = Tool.Move;
				}

				EditorGUI.EndDisabledGroup();
			}
		}
		internal void MessagesEditor()
		{
			Color orgGUIBackgroundColor = GUI.backgroundColor;

			if (EditorApplication.isPlaying && (!Instance.transform.parent || Instance.transform.parent.gameObject.activeInHierarchy) || Instance.isActiveAndEnabled)
			{
				bool hasWarnings = Instance.Problems.HasComponentsWarnings || Instance.Problems.HasDisabledWheels || Instance.Problems.HasBehaviourWarnings || Instance.Problems.HasTiresWarnings;
				bool hasErrors = Instance.Problems.HasComponentsErrors || Instance.Problems.HasBehaviourErrors || Instance.Problems.HasSteeringErrors || Instance.Problems.HasAIErrors;
				bool hasIssues = Instance.Problems.HasComponentsIssues || Instance.Problems.HasBehaviourIssues || Instance.Problems.HasTransmissionIssues || Instance.Problems.HasBrakingIssues || Instance.Problems.HasSteeringIssues || Instance.Problems.HasSuspensionsIssues || Instance.Problems.HasStabilityIssues || Instance.Problems.HasInteriorIssues || Instance.Problems.HasTrailerJointIssues || Instance.Problems.HasTrailerLinkIssues || Instance.Problems.HasAIIssues;

				if (hasWarnings)
					GUI.backgroundColor = Color.yellow;
				else if (hasErrors)
					GUI.backgroundColor = Color.red;
				else if (hasIssues)
					GUI.backgroundColor = Color.green;
				else
					GUI.backgroundColor = Color.blue;

				EditorGUILayout.Space();
				EditorGUILayout.HelpBox($"Overall State: {(hasWarnings ? "This controller instance is going to be disabled at runtime." : hasErrors ? "This controller has some errors that need to be fixed before switching to play mode or else some features may not work." : hasIssues ? "Some features may not work as expected." : "Everything works well!")}", MessageType.None);

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (Instance.Problems.HasComponentsWarnings)
				{
					if (Instance.Wheels.Length < 1)
						EditorGUILayout.HelpBox("The vehicle doesn't have any wheels.", MessageType.Warning);
					else
					{
						if (TrailerInstance)
						{
							if (!Instance.HasRearWheels)
								EditorGUILayout.HelpBox("The trailer doesn't have any wheels.", MessageType.Warning);
						}
						else
						{
							if (!Instance.HasFrontWheels)
								EditorGUILayout.HelpBox("The vehicle doesn't have any front wheels.", MessageType.Warning);

							if (!Instance.HasRearWheels)
								EditorGUILayout.HelpBox("The vehicle doesn't have any rear wheels.", MessageType.Warning);
						}

						if (Instance.Problems.HasDisabledWheels)
							EditorGUILayout.HelpBox("Some wheels have been disabled!", MessageType.Warning);
					}

					if (Utility.Round(Instance.transform.localScale, 1) != Vector3.one)
						EditorGUILayout.HelpBox("The vehicle has to have a scale of One (1.0, 1.0, 1.0).", MessageType.Warning);
				}
				else if (Instance.Problems.HasComponentsErrors)
				{
					if (!Instance.Chassis)
						EditorGUILayout.HelpBox("The chassis component is missing!", MessageType.Error);

					if (Instance.Problems.MissingWheelTransformsCount > 0)
						EditorGUILayout.HelpBox("Some Wheel Transforms are missing! Assign the Transform fields to proceed working.", MessageType.Error);
					else if ((Instance.Problems.MissingWheelBehavioursCount - Instance.Problems.MissingWheelTransformsCount) > 0)
						EditorGUILayout.HelpBox("Some Wheel Colliders are missing! Use the 'Generate Wheels' button to recreate missing Wheel Colliders.", MessageType.Error);
				}
				else if (Instance.Problems.HasComponentsIssues)
				{
					if (!Instance.Chassis.GetComponentInChildren<Collider>())
						EditorGUILayout.HelpBox("The vehicle doesn't have any type of colliders attached to any of the Chassis GameObjects!", MessageType.Info);

					if (Instance.Problems.MissingWheelRimsCount > 0)
						EditorGUILayout.HelpBox("Some wheel Rim Models are missing! To solve this issue you have to assign the missing Rim fields.", MessageType.Info);
					else if (Instance.Problems.MissingWheelRimEdgesCount > 0)
						EditorGUILayout.HelpBox("Some wheel Rim Edge Renderers are missing! To solve this issue you have to assign the missing Rim Edge fields.", MessageType.Info);
					else if (!EditorApplication.isPlaying && Array.Find(Instance.Wheels, wheel => Utility.Round(wheel.Rim.localScale, 1) != Vector3.one))
						EditorGUILayout.HelpBox("Some wheel Rim Models doesn't have a scale of One (1.0, 1.0, 1.0). This may cause some visual issues!", MessageType.Info);

					if (Instance.Problems.MissingWheelTiresCount > 0)
						EditorGUILayout.HelpBox("Some wheel Tire Models are missing! To solve this issue you have to assign the missing Tire fields.", MessageType.Info);
					else if (!EditorApplication.isPlaying && Array.Find(Instance.Wheels, wheel => Utility.Round(wheel.Tire.localScale, 1) != Vector3.one))
						EditorGUILayout.HelpBox("Some wheel Tire Models doesn't have a scale of One (1.0, 1.0, 1.0). This may cause some visual issues!", MessageType.Info);

					if (Instance.Problems.MissingWheelBrakeCallipersCount > 0)
						EditorGUILayout.HelpBox("Some wheel Brake Callipers are missing! To solve this issue you have to assign the missing Brake Calliper fields.", MessageType.Info);
					else if (!EditorApplication.isPlaying && Array.Find(Instance.Wheels, wheel => wheel.BrakeCalliper && Utility.Round(wheel.BrakeCalliper.localScale, 1) != Vector3.one))
						EditorGUILayout.HelpBox("Some wheel Brake Calliper Renderers don't have a scale of One (1.0, 1.0, 1.0). This may cause some visual issues!", MessageType.Info);

					if (Instance.Problems.MissingWheelBrakeDiscsCount > 0)
						EditorGUILayout.HelpBox("Some wheel Brake Disc Renderers are missing! To solve this issue you have to assign the missing Brake Disc Renderer fields.", MessageType.Info);
					else if (!EditorApplication.isPlaying && Array.Find(Instance.Wheels, wheel => wheel.BrakeDiscRenderer && Utility.Round(wheel.BrakeDiscRenderer.transform.localScale, 1) != Vector3.one))
						EditorGUILayout.HelpBox("Some Wheel Brake Disc doesn't have a scale of One (1.0, 1.0, 1.0). This may cause some visual issues!", MessageType.Info);

					if (!Instance.IsAllBalanced)
						EditorGUILayout.HelpBox("You may have some balancing issues!", MessageType.Info);

					if (!Instance.IsVerticalAxleBalanced)
						EditorGUILayout.HelpBox("The numbers of both left and right wheels doesn't match each other.", MessageType.Info);

					if (!TrailerInstance)
					{
						if (!Instance.IsFrontMotorAxleBalanced)
							EditorGUILayout.HelpBox("The numbers of both left and right motor wheels on the front axle doesn't match each other.", MessageType.Info);

						if (!Instance.IsRearMotorAxleBalanced)
							EditorGUILayout.HelpBox("The numbers of both left and right motor wheels on the rear axle doesn't match each other.", MessageType.Info);

						if (!Instance.IsFrontSteerAxleBalanced)
							EditorGUILayout.HelpBox("The numbers of both left and right steering wheels on the front axle doesn't match each other.", MessageType.Info);

						if (!Instance.IsRearSteerAxleBalanced)
							EditorGUILayout.HelpBox("The numbers of both left and right steering wheels on the rear axle doesn't match each other.", MessageType.Info);

						if (Instance.Drivetrain == Vehicle.Train.None)
							EditorGUILayout.HelpBox("This vehicle isn't controllable, because its motor wheels are missing! ", MessageType.Info);

						if (Instance.Steertrain == Vehicle.Train.None)
							EditorGUILayout.HelpBox("The vehicle cannot steer, because it doesn't have any steering wheels!", MessageType.Info);

						if (!Instance.IsElectric && Instance.Exhausts.Length > 0 && !Instance.Chassis.ExhaustModel)
							EditorGUILayout.HelpBox("It seems like the exhaust model(s) group is missing!", MessageType.Info);
					}
				}

				if (TrailerInstance)
				{
					if (TrailerInstance.Problems.HasTrailerJointIssues)
					{
						if (TrailerInstance.Joint.Position == default)
							EditorGUILayout.HelpBox("The Joint Connection Point is at the bottom of the trailer, you might need to re-position it.", MessageType.Info);
					}
				}
				else
				{
					if (Instance.Problems.HasBehaviourWarnings)
					{
						if (!Instance.Engine)
							EditorGUILayout.HelpBox("The selected Engine Preset is out of bounds or doesn't exist anymore. Please select another Engine Preset from the 'Behaviour' menu under the 'Engine' field, or add a new Engine Preset from the 'Tools > Multiversal Vehicle Controller > Vehicle Settings' menu.", MessageType.Warning);
						else if (Instance.IsElectric != Instance.Engine.IsElectric)
							EditorGUILayout.HelpBox($"The engine type seems to differ from the vehicle's class, please select {(Instance.IsElectric ? "an electric" : "a normal")} engine instead of {(Instance.IsElectric ? "a normal" : "an electric")} one.", MessageType.Warning);

						if (!Instance.IsElectric)
						{
							if (Instance.Behaviour.IsTurbocharged && !Instance.Turbocharger)
								EditorGUILayout.HelpBox($"The Turbocharger instance is null, it seem to have disappeared or removed from the Charger Presets list.", MessageType.Warning);
							else if (Instance.Behaviour.IsTurbocharged && Instance.Turbocharger && !Instance.Turbocharger.IsValid)
								EditorGUILayout.HelpBox($"The selected Turbocharger has some invalid settings that need to be fixed.", MessageType.Warning);

							if (Instance.Behaviour.IsSupercharged && !Instance.Supercharger)
								EditorGUILayout.HelpBox($"The Supercharger instance is null, it seem to have disappeared or removed from the Charger Presets list.", MessageType.Warning);
							else if (Instance.Behaviour.IsSupercharged && Instance.Supercharger && !Instance.Supercharger.IsValid)
								EditorGUILayout.HelpBox($"The selected Supercharger has some invalid settings that need to be fixed.", MessageType.Warning);
						}
					}
					else if (Instance.Problems.HasBehaviourErrors)
					{
						if (Instance.Engine)
						{
							if (Instance.Engine.MinimumRPM > Instance.Behaviour.PeakTorqueRPM)
								EditorGUILayout.HelpBox($"The peak Torque RPM ({Mathf.Round(Instance.Behaviour.PeakTorqueRPM)} rpm) is bellow the minimum engine RPM ({Mathf.Round(Instance.Engine.MinimumRPM)} rpm).", MessageType.Error);

							if (Instance.Behaviour.PeakTorqueRPM > Instance.Engine.RedlineRPM)
								EditorGUILayout.HelpBox($"The peak Torque RPM ({Mathf.Round(Instance.Behaviour.PeakTorqueRPM)} rpm) exceeds the engine Redline RPM ({Mathf.Round(Instance.Engine.RedlineRPM)} rpm).", MessageType.Error);

							if (Instance.Engine.MinimumRPM > Instance.Behaviour.PeakPowerRPM)
								EditorGUILayout.HelpBox($"The peak Power RPM ({Mathf.Round(Instance.Behaviour.PeakPowerRPM)} rpm) is bellow the minimum engine RPM ({Mathf.Round(Instance.Engine.MinimumRPM)} rpm).", MessageType.Error);

							if (Instance.Behaviour.PeakPowerRPM > Instance.Engine.RedlineRPM)
								EditorGUILayout.HelpBox($"The peak Power RPM ({Mathf.Round(Instance.Behaviour.PeakPowerRPM)} rpm) exceeds the engine Redline RPM ({Mathf.Round(Instance.Engine.RedlineRPM)} rpm).", MessageType.Error);

							float power = Instance.Behaviour.Power;
							float peakPower = Instance.Behaviour.PowerCurve.Evaluate(Instance.Behaviour.PeakPowerRPM);

							if (!Mathf.Approximately(power, peakPower))
								EditorGUILayout.HelpBox($"The current vehicle power ({Utility.NumberToValueWithUnit(power, Utility.Units.Power, Settings.editorPowerUnit, 1)}) doesn't match the power curve's peak ({Utility.NumberToValueWithUnit(peakPower, Utility.Units.Power, Settings.editorPowerUnit, 1)}).", MessageType.Error);

							float torque = Instance.Behaviour.Torque;
							float peakTorque = Instance.Behaviour.TorqueCurve.Evaluate(Instance.Behaviour.PeakTorqueRPM);

							if (!Mathf.Approximately(torque, peakTorque))
								EditorGUILayout.HelpBox($"The current vehicle torque ({Utility.NumberToValueWithUnit(torque, Utility.Units.Torque, Settings.editorTorqueUnit, 1)}) doesn't match the torque curve's peak ({Utility.NumberToValueWithUnit(peakTorque, Utility.Units.Torque, Settings.editorTorqueUnit, 1)}).", MessageType.Error);

							if (!Instance.IsElectric)
							{
								if (Instance.Behaviour.TurbochargerIndex >= Settings.Chargers.Length)
									EditorGUILayout.HelpBox($"It seems like the selected Turbocharger doesn't exist anymore or has been removed!", MessageType.Error);
								else if (Instance.Behaviour.IsTurbocharged && Instance.Turbocharger && !Instance.Turbocharger.IsValid)
									EditorGUILayout.HelpBox($"It seems like the selected Turbocharger is invalid!", MessageType.Error);
								else if (Instance.Behaviour.IsTurbocharged && Instance.Turbocharger && Instance.Turbocharger.Type != VehicleCharger.ChargerType.Turbocharger)
									EditorGUILayout.HelpBox($"The selected Turbocharger is actually a {Instance.Turbocharger.Type}, and that is invalid!", MessageType.Error);
								else
								{
									if (Instance.Behaviour.IsTurbocharged && !Instance.Turbocharger.IsCompatible)
										EditorGUILayout.HelpBox($"The selected Turbocharger is incompatible with the engine.", MessageType.Error);

									if (Instance.Behaviour.IsTurbocharged && Instance.Turbocharger.CompatibleEngineIndexes.Length > 0 && Array.IndexOf(Instance.Turbocharger.CompatibleEngineIndexes, Instance.Behaviour.EngineIndex) < 0)
										EditorGUILayout.HelpBox($"The vehicle engine cannot be found in the Turbocharger compatible engines list.", MessageType.Error);
								}

								if (Instance.Behaviour.SuperchargerIndex >= Settings.Chargers.Length)
									EditorGUILayout.HelpBox($"It seems like the selected Supercharger doesn't exist anymore or has been removed!", MessageType.Error);
								else if (Instance.Behaviour.IsSupercharged && Instance.Supercharger && !Instance.Supercharger.IsValid)
									EditorGUILayout.HelpBox($"It seems like the selected Supercharger is invalid!", MessageType.Error);
								else if (Instance.Behaviour.IsSupercharged && Instance.Supercharger && Instance.Supercharger.Type != VehicleCharger.ChargerType.Supercharger)
									EditorGUILayout.HelpBox($"The selected Supercharger is actually a {Instance.Supercharger.Type}, and that is invalid!", MessageType.Error);
								else
								{
									if (Instance.Behaviour.IsSupercharged && Instance.Supercharger.CompatibleEngineIndexes.Length > 0 && Array.IndexOf(Instance.Supercharger.CompatibleEngineIndexes, Instance.Behaviour.EngineIndex) < 0)
										EditorGUILayout.HelpBox($"The vehicle engine cannot be found in the Supercharger compatible engines list.", MessageType.Error);

									if (Instance.Behaviour.IsSupercharged && !Instance.Supercharger.IsCompatible)
										EditorGUILayout.HelpBox($"The selected Supercharger is incompatible with the engine.", MessageType.Error);
								}
							}
						}
						else
							EditorGUILayout.HelpBox($"It seems like the selected Engine doesn't exist anymore or has been removed!", MessageType.Error);

					}
					else if (Instance.Problems.HasBehaviourIssues)
					{
						if (Instance.Behaviour.Torque * 10f / Instance.Behaviour.CurbWeight < 1f / 3f)
							EditorGUILayout.HelpBox("The torque amount might be too small and not enough to move the car.", MessageType.Info);

						if (Instance.Behaviour.Torque / Instance.Behaviour.Power > 3f)
						{
							EditorGUILayout.HelpBox("The torque value might be too high compared to the power value.", MessageType.Info);
							EditorGUILayout.HelpBox("The power value might be too small compared to the torque value.", MessageType.Info);
						}

						if (Instance.Behaviour.Power / Instance.Behaviour.Torque > 3f)
						{
							EditorGUILayout.HelpBox("The power value might be too high compared to the torque value.", MessageType.Info);
							EditorGUILayout.HelpBox("The torque value might be too small compared to the power value.", MessageType.Info);
						}

						if (Instance.Behaviour.IsTurbocharged && Instance.Turbocharger && Instance.Turbocharger.HasIssues)
							EditorGUILayout.HelpBox("The selected Turbocharger might have some issues.", MessageType.Info);

						if (Instance.Behaviour.IsSupercharged && Instance.Supercharger && Instance.Supercharger.HasIssues)
							EditorGUILayout.HelpBox("The selected Supercharger might have some issues.", MessageType.Info);

						if (Settings.useFuelSystem)
						{
							if (Instance.Behaviour.FuelCapacity == 0f)
								EditorGUILayout.HelpBox("While using the Fuel System, this vehicle won't be to start its engine because its fuel capacity is null (equal to 0).", MessageType.Info);

							if (Instance.Behaviour.fuelConsumptionPrecision == Utility.Precision.Advanced && Instance.Behaviour.FuelConsumptionCity == 0f && Instance.Behaviour.FuelConsumptionHighway == 0f || Instance.Behaviour.fuelConsumptionPrecision == Utility.Precision.Simple && Instance.Behaviour.FuelConsumptionCombined == 0f)
								EditorGUILayout.HelpBox("It's unnecessary to have the Fuel System enabled while the vehicle doesn't consume any fuel.", MessageType.Info);
						}
					}

					if (Instance.Problems.HasTransmissionIssues)
					{
						if (Instance.Transmission.Efficiency < .5f)
							EditorGUILayout.HelpBox("Comparing the engine torque output to the clutch torque capacity, it seems that the efficient rate is low.", MessageType.Info);

						if (Instance.Drivetrain != Vehicle.Train.None)
						{
							if (Instance.Drivetrain != Vehicle.Train.RWD)
							{
								if (Instance.Transmission.FrontDifferential.GearRatio <= 0f)
									EditorGUILayout.HelpBox("The front differential `Gear Ratio` is null (equal to 0). This won't allow the front wheels to move the vehicle.", MessageType.Info);

								if (Instance.Transmission.FrontDifferential.Type != VehicleDifferentialType.Locked && !Mathf.Approximately(Instance.Transmission.FrontDifferential.BiasAB, .5f))
									EditorGUILayout.HelpBox("The front differential `Bias AB` is not calibrated to .5. This might cause the vehicle to drift.", MessageType.Info);

								switch (Instance.Transmission.FrontDifferential.Type)
								{
									case VehicleDifferentialType.Locked:
										if (Mathf.Approximately(Instance.Transmission.FrontDifferential.Stiffness, 0f))
											EditorGUILayout.HelpBox("The front locked differential `Stiffness` is null (equal to 0) and it will act as an `Open` differential.", MessageType.Info);

										break;

									case VehicleDifferentialType.LimitedSlip:
										if (Mathf.Approximately(Instance.Transmission.FrontDifferential.PowerRamp, 0f) && Mathf.Approximately(Instance.Transmission.FrontDifferential.CoastRamp, 0f))
											EditorGUILayout.HelpBox("The front LSD differential `Power Ramp` and `Coast Ramp` are null (equal to 0) and it will act as an `Open` differential.", MessageType.Info);

										if (Mathf.Approximately(Instance.Transmission.FrontDifferential.SlipTorque, 0f))
											EditorGUILayout.HelpBox("The front LSD differential `Slip Torque` are null (equal to 0) and it will act as an `Open` differential.", MessageType.Info);

										break;
								}
							}

							if (Instance.Drivetrain == Vehicle.Train.AWD)
							{
								if (Instance.Transmission.CenterDifferential.GearRatio <= 0f)
									EditorGUILayout.HelpBox("The center differential `Gear Ratio` is null (equal to 0). This won't allow the center wheels to move the vehicle.", MessageType.Info);

								switch (Instance.Transmission.CenterDifferential.Type)
								{
									case VehicleDifferentialType.Locked:
										if (Mathf.Approximately(Instance.Transmission.CenterDifferential.Stiffness, 0f))
											EditorGUILayout.HelpBox("The center locked differential `Stiffness` is null (equal to 0) and it will act as an `Open` differential.", MessageType.Info);

										break;

									case VehicleDifferentialType.LimitedSlip:
										if (Mathf.Approximately(Instance.Transmission.CenterDifferential.PowerRamp, 0f) && Mathf.Approximately(Instance.Transmission.CenterDifferential.CoastRamp, 0f))
											EditorGUILayout.HelpBox("The center LSD differential `Power Ramp` and `Coast Ramp` are null (equal to 0) and it will act as an `Open` differential.", MessageType.Info);

										if (Mathf.Approximately(Instance.Transmission.CenterDifferential.SlipTorque, 0f))
											EditorGUILayout.HelpBox("The center LSD differential `Slip Torque` are null (equal to 0) and it will act as an `Open` differential.", MessageType.Info);

										break;
								}
							}

							if (Instance.Drivetrain != Vehicle.Train.FWD)
							{
								if (Instance.Transmission.RearDifferential.GearRatio <= 0f)
									EditorGUILayout.HelpBox("The rear differential `Gear Ratio` is null (equal to 0). This won't allow the rear wheels to move the vehicle.", MessageType.Info);

								if (Instance.Transmission.RearDifferential.Type != VehicleDifferentialType.Locked && !Mathf.Approximately(Instance.Transmission.RearDifferential.BiasAB, .5f))
									EditorGUILayout.HelpBox("The rear differential `Bias AB` is not calibrated to .5. This might cause the vehicle to drift.", MessageType.Info);

								switch (Instance.Transmission.RearDifferential.Type)
								{
									case VehicleDifferentialType.Locked:
										if (Mathf.Approximately(Instance.Transmission.RearDifferential.Stiffness, 0f))
											EditorGUILayout.HelpBox("The rear locked differential `Stiffness` is null (equal to 0) and it will act as an `Open` differential.", MessageType.Info);

										break;

									case VehicleDifferentialType.LimitedSlip:
										if (Mathf.Approximately(Instance.Transmission.RearDifferential.PowerRamp, 0f) && Mathf.Approximately(Instance.Transmission.RearDifferential.CoastRamp, 0f))
											EditorGUILayout.HelpBox("The rear LSD differential `Power Ramp` and `Coast Ramp` are null (equal to 0) and it will act as an `Open` differential.", MessageType.Info);

										if (Mathf.Approximately(Instance.Transmission.RearDifferential.SlipTorque, 0f))
											EditorGUILayout.HelpBox("The rear LSD differential `Slip Torque` are null (equal to 0) and it will act as an `Open` differential.", MessageType.Info);

										break;
								}
							}
						}
					}
				}

				if (Instance.Problems.HasBrakingIssues)
				{
					if (TrailerInstance)
					{
						if (Instance.Wheels.Length > 0 && TrailerInstance.Brakes.Diameter * 100f * Utility.UnitMultiplier(Utility.Units.Size, Utility.UnitType.Imperial) >= Instance.Wheels.Min(wheel => wheel.Instance.Diameter) * .75f)
							EditorGUILayout.HelpBox("The brakes diameter may be higher than the front wheel rims diameter, therefore they might not fit in visually!", MessageType.Info);
					}
					else
					{
						if (Instance.FrontWheels.Length > 0 && Instance.Behaviour.FrontBrakes.Diameter * 100f * Utility.UnitMultiplier(Utility.Units.Size, Utility.UnitType.Imperial) >= Instance.FrontWheels.Min(wheel => wheel.Instance.Diameter) * .75f)
							EditorGUILayout.HelpBox("The front brakes diameter may be higher than the front wheel rims diameter, therefore they might not fit in visually!", MessageType.Info);

						if (Instance.RearWheels.Length > 0 && Instance.Behaviour.RearBrakes.Diameter * 100f * Utility.UnitMultiplier(Utility.Units.Size, Utility.UnitType.Imperial) >= Instance.RearWheels.Min(wheel => wheel.Instance.Diameter) * .75f)
							EditorGUILayout.HelpBox("The rear brakes diameter may be higher than the rear wheel rims diameter, therefore they might not fit in visually!", MessageType.Info);
					}
				}

				if (!TrailerInstance)
				{
					if (Instance.Problems.HasSteeringErrors)
					{
						if (Instance.Steering.MaximumSteerAngle < Instance.Steering.MinimumSteerAngle)
							EditorGUILayout.HelpBox("The Minimum Steering Angle cannot be greater than the Maximum Steering Angle!", MessageType.Error);
					}
					else if (Instance.Problems.HasSteeringIssues)
					{
						if (Instance.Steering.MaximumSteerAngle == 0f)
							EditorGUILayout.HelpBox("The vehicle is unable to steer because due to the Maximum Steer Angle, which is null (equal to 0).", MessageType.Info);

						if (Instance.Steering.UseDynamicSteering && Instance.Steering.DynamicSteeringIntensity <= 0f)
							EditorGUILayout.HelpBox("The Dynamic Steering intensity is null (equal to 0), it is unnecessary to turn this feature on!", MessageType.Info);

						if (Instance.Steering.Method != Vehicle.SteeringModule.SteerMethod.Simple)
						{
							if (Instance.Steering.MinimumSteerAngle == 0f)
								EditorGUILayout.HelpBox($"The vehicle cannot steer at higher speeds (starting from {Utility.NumberToValueWithUnit(Instance.Steering.LowSteerAngleSpeed, Utility.Units.Speed, Settings.editorValuesUnit, true)}).", MessageType.Info);

							if (Instance.Steering.LowSteerAngleSpeed > Instance.Behaviour.TopSpeed)
								EditorGUILayout.HelpBox("The Low Steer Angle Speed value is higher than the vehicle Top Speed. This seem to be unexpected.", MessageType.Info);
						}
					}
				}

				if (Instance.Problems.HasSuspensionsIssues)
				{
					if (!TrailerInstance && Instance.Stability.useAntiSwayBars && Instance.FrontLeftWheels.Length > 0 && Instance.FrontRightWheels.Length > 0 && Instance.FrontSuspension.Length * (1f + Instance.FrontSuspension.LengthStance) * 33333f > Instance.Stability.AntiSwayFront)
						EditorGUILayout.HelpBox("The front suspension length is too long, you may have to increase the 'Front Anti-Sway' force under the 'Stability' tab to prevent the vehicle from flipping.", MessageType.Info);

					if (Instance.Stability.useAntiSwayBars && Instance.RearLeftWheels.Length > 0 && Instance.RearRightWheels.Length > 0 && Instance.RearSuspension.Length * (1f + Instance.RearSuspension.LengthStance) * 33333f > Instance.Stability.AntiSwayRear)
						EditorGUILayout.HelpBox($"The{(TrailerInstance ? "" : " rear")} suspension length is too long, you may have to increase the 'Rear Anti-Sway' values under the 'Stability' tab to prevent the vehicle from flipping.", MessageType.Info);
				}

				if (Instance.Problems.HasTiresWarnings)
				{
					if (Instance.Wheels.Any(wheel => !wheel.Instance.TireCompound || !wheel.Instance.TireCompound.IsValid))
						EditorGUILayout.HelpBox("Some of your wheels don't have a valid tire compound to simulate friction at runtime.", MessageType.Warning);
				}

				if (Instance.Problems.HasStabilityIssues)
				{
					if (!TrailerInstance)
					{
						if (Instance.Stability.useAntiSwayBars)
						{
							if (Instance.FrontLeftWheels.Length > 0 && Instance.FrontRightWheels.Length > 0 && Instance.Stability.AntiSwayFront <= 0f)
								EditorGUILayout.HelpBox("The anti-sway front force is null (equal to 0). This may cause the car to flip", MessageType.Info);

							if (Instance.RearLeftWheels.Length > 0 && Instance.RearRightWheels.Length > 0 && Instance.Stability.AntiSwayRear <= 0f)
								EditorGUILayout.HelpBox("The anti-sway rear force is null (equal to 0). This may cause the car to flip", MessageType.Info);
						}

						if (Instance.Stability.useESP && Instance.Stability.ESPStrength == 0f)
							EditorGUILayout.HelpBox("The ESP strength is equal to 0. It's unnecessary to have it enabled.", MessageType.Info);

						if (Instance.Stability.useArcadeSteerHelpers && Instance.Stability.ArcadeLinearSteerHelperIntensity == 0f && Instance.Stability.ArcadeAngularSteerHelperIntensity <= 0f)
							EditorGUILayout.HelpBox("Both of the steering helpers linear and angular strength are equal to 0. It's unnecessary to have it enabled.", MessageType.Info);
					}

					if (Instance.Stability.useDownforce)
					{
						if (Instance.Stability.FrontDownforce == 0f && Instance.Stability.RearDownforce == 0f)
							EditorGUILayout.HelpBox("The front & rear downforces are equal to 0. It's unnecessary to have it enabled.", MessageType.Info);
					}

					if (Instance.Rigidbody.maxAngularVelocity <= 0f)
						EditorGUILayout.HelpBox("The maximum angular velocity value is null (equal to 0)! Therefore, the vehicle might not be able to rotate at all.", MessageType.Info);

					if (Instance.Rigidbody.interpolation == RigidbodyInterpolation.None)
						EditorGUILayout.HelpBox("The Rigidbody Interpolation is currently set to 'None', this might cause some jitter in the movement of the vehicle or the sound effects of the vehicle. The vehicle jitter might be visible when turning left and right, while the sound jitter is mostly happening to the engine sound.", MessageType.Info);
				}

				if (!TrailerInstance)
				{
					var aiBehaviours = Instance.AIBehaviours;

					if (Instance.Problems.HasAIErrors)
					{
						if (Instance.ActiveAIBehaviourIndex >= aiBehaviours.Length)
							EditorGUILayout.HelpBox("The selected AI behaviour changed or has been removed!", MessageType.Error);

						foreach (var behaviour in aiBehaviours)
							if (behaviour.Errors != null)
								foreach (string error in behaviour.Errors)
									EditorGUILayout.HelpBox(error, MessageType.Error);
					}
					else if (Instance.Problems.HasAIWarnings)
					{
						foreach (var behaviour in aiBehaviours)
							if (behaviour.Warnings != null)
								foreach (string error in behaviour.Warnings)
									EditorGUILayout.HelpBox(error, MessageType.Warning);
					}
					else if (Instance.Problems.HasAIIssues)
					{
						foreach (var behaviour in aiBehaviours)
							if (behaviour.Issues != null)
								foreach (string error in behaviour.Issues)
									EditorGUILayout.HelpBox(error, MessageType.Info);
					}

					if (Instance.Problems.HasInteriorIssues)
					{
						if (Instance.Interior.SteeringWheel.transform && !Instance.Interior.SteeringWheel.transform.IsChildOf(Instance.Chassis.transform))
							EditorGUILayout.HelpBox("The interior steering wheel isn't a part of the vehicle's chassis.", MessageType.Info);

						if (Instance.Interior.RPMNeedle.transform && !Instance.Interior.RPMNeedle.transform.IsChildOf(Instance.Chassis.transform))
							EditorGUILayout.HelpBox("The speedometer's RPM needle isn't a part of the vehicle's chassis.", MessageType.Info);

						if (Instance.Interior.SpeedNeedle.transform && !Instance.Interior.SpeedNeedle.transform.IsChildOf(Instance.Chassis.transform))
							EditorGUILayout.HelpBox("The speedometer's speed needle isn't a part of the vehicle's chassis.", MessageType.Info);
					}
				}

				if (Instance.Problems.HasTrailerLinkIssues)
				{
					if (Instance.TrailerLink.LinkRadius > Instance.ChassisBounds.extents.x)
						EditorGUILayout.HelpBox("It seems that the Trailer Link Radius is kind of large compared to the size of the Vehicle/Trailer.", MessageType.Info);
				}

				EditorGUI.EndDisabledGroup();
			}
			else
				EditorGUILayout.HelpBox("This vehicle isn't controllable, because its controller or GameObject has been disabled.", MessageType.None);
		}

		#endregion

		#region Enable, Destroy & Disable

		internal void OnEnable()
		{
			if (!Instance || EditorApplication.isPlaying)
				return;

			Instance.RefreshLayersAndTags();
			Instance.RefreshWheels();
			Instance.GetOrCreateRigidbody();

			if (Instance.Chassis)
			{
				Transform lightsParent = Instance.Chassis.transform.Find("LightSources");
				var hideFlag = Settings.useHideFlags ? HideFlags.HideInHierarchy : HideFlags.None;

				if (lightsParent && lightsParent.hideFlags != hideFlag)
					lightsParent.hideFlags = hideFlag;

				Instance.Chassis.RefreshFollowerPivots();
				Instance.RefreshIKPivots();
			}

			Undo.undoRedoPerformed += Instance.RefreshWheelsRenderers;
		}
		internal void OnDestroy()
		{
			if (!Instance)
				return;

			if (!Instance.IsTrailer)
				HideExhausts();

#if !MVC_COMMUNITY
			SaveInteriorLight();
#endif

			Undo.undoRedoPerformed -= Instance.RefreshWheelsRenderers;
		}
		internal void OnDisable()
		{
			OnDestroy();
		}

		#endregion

		#endregion

		#endregion
	}
}

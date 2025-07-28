#region Namespaces

using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using UnityEditor;
using Utilities;
using Utilities.Editor;
using MVC.Core;
using MVC.Base;
using MVC.AI;
using MVC.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Editor
{
	public class VehicleProfilerWindow : ToolkitEditorWindow
	{
		#region Enumerators

		private enum DebugFoldout
		{
			None,
			Specifications,
			Dimensions,
			Rigidbody,
			Statistics,
			Inputs,
			Lights,
			Wheels
#if !MVC_COMMUNITY
			, AI
#endif
		}

		#endregion

		#region Modules

		[Serializable]
		private struct RigidbodyStats
		{
			#region Variables

			public Vector3 velocity;
			public Vector3 angularVelocity;
			public float maxAngularVelocityValue;
			public float maxDepenetrationVelocityValue;
			public float dragValue;
			public float angularDragValue;
			public Vector3 centerOfMass;
			public Vector3 worldCenterOfMass;
			public Vector3 inertiaTensor;
			public Vector3 inertiaTensorRotation;

			[SerializeField]
			private Rigidbody rigidbody;

			#endregion

			#region Methods

			public void Reset()
			{
				velocity = default;
				angularVelocity = default;
				maxAngularVelocityValue = default;
				maxDepenetrationVelocityValue = default;
				dragValue = default;
				angularDragValue = default;
				centerOfMass = default;
				worldCenterOfMass = default;
				inertiaTensor = default;
				inertiaTensorRotation = default;
			}
			public void Update()
			{
				if (!rigidbody)
					return;

				velocity = rigidbody.
#if UNITY_6000_0_OR_NEWER
					linearVelocity;
#else
					velocity;
#endif
				angularVelocity = rigidbody.angularVelocity;
				maxAngularVelocityValue = rigidbody.maxAngularVelocity;
				maxDepenetrationVelocityValue = rigidbody.maxDepenetrationVelocity;
				dragValue = rigidbody.
#if UNITY_6000_0_OR_NEWER
					linearDamping;
#else
					drag;
#endif
				angularDragValue = rigidbody.
#if UNITY_6000_0_OR_NEWER
					angularDamping;
#else
					angularDrag;
#endif
				centerOfMass = rigidbody.centerOfMass;
				worldCenterOfMass = rigidbody.worldCenterOfMass;
				inertiaTensor = rigidbody.inertiaTensor;
				inertiaTensorRotation = rigidbody.inertiaTensorRotation.eulerAngles;
			}

			#endregion

			#region Constructors

			public RigidbodyStats(Rigidbody rigidbody) : this()
			{
				this.rigidbody = rigidbody;

				Reset();
			}

			#endregion
		}

		#endregion

		#region Variables

		#region Static Variables

		private static VehicleProfilerWindow instance;

		#endregion

		#region Global Variables

		private Vehicle VehicleInstance
		{
			get
			{
				bool selectionChanged = false;

				if (Selection.activeGameObject && selection != Selection.activeGameObject)
				{
					selection = Selection.activeGameObject;
					selectionChanged = true;
				}

				if (selection && (!vehicleInstance || selectionChanged))
				{
					EditorApplication.update -= rigidbodyStats.Update;

					if (selection.TryGetComponent(out vehicleInstance))
					{
						rigidbodyStats = vehicleInstance ? new(vehicleInstance.Rigidbody) : default;

						if (vehicleInstance)
							EditorApplication.update += rigidbodyStats.Update;
					}
					else
					{
						vehicleInstance = null;
						selection = null;
					}
				}

				return vehicleInstance;
			}
		}
		[SerializeField]
		private GameObject selection;
		[SerializeField]
		private Vehicle vehicleInstance;
		[SerializeField]
		private RigidbodyStats rigidbodyStats;
		[SerializeField]
		private Vector2 scroll;
		[SerializeField]
		private DebugFoldout debugFoldout;
		[SerializeField]
		private bool initiated;

		#endregion

		#endregion

		#region Methods

		#region Static Methods

		[MenuItem("Tools/Multiversal Vehicle Controller/Vehicle Profiler", false, 7)]
		public static VehicleProfilerWindow OpenWindow()
		{
			if (HasOpenInstances<VehicleProfilerWindow>())
			{
				FocusWindowIfItsOpen<VehicleProfilerWindow>();

				return instance;
			}

			string title = "Vehicle Profiler";

			instance = GetWindow<VehicleProfilerWindow>(false, title, true);
			instance.titleContent = new(title, EditorGUIUtility.IconContent("d_UnityEditor.ProfilerWindow").image);
			instance.minSize = new(500, 700);

			return instance;
		}

		#endregion

		#region Global Methods

		#region GUI

		private void OnGUI()
		{
			#region Messages

			Color orgGUIBackgroundColor = GUI.backgroundColor;

			if (HasInternalErrors)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("The Multiversal Vehicle Controller is facing some internal problems that need to be fixed!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (GUILayout.Button("Try some quick fixes"))
					FixInternalProblems(Selection.activeObject);

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
			else if (!initiated && VehicleInstance)
			{
				VehicleInstance.RefreshWheels();

				initiated = true;
			}

			#endregion

			scroll = EditorGUILayout.BeginScrollView(scroll);

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField($"{(VehicleInstance ? VehicleInstance.name : "Vehicle Profiler")}", ToolkitEditorUtility.MiddleCenterAlignedBoldLabel);
			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();

			if (!VehicleInstance)
			{
				EditorGUILayout.HelpBox("Please select a Vehicle instance in your scene or a prefab in your project folder to profile it with the Vehicle Profiler!", MessageType.Info);
				EditorGUILayout.EndScrollView();
				Repaint();

				if (initiated)
					initiated = false;

				return;
			}
			else if (!VehicleInstance.gameObject.activeInHierarchy || !VehicleInstance.enabled)
			{
				EditorGUILayout.HelpBox("It seems like the selected vehicle is in-active, in order to Profile it please re-enable the Vehicle behaviour or the gameObject!", MessageType.Info);
				EditorGUILayout.EndScrollView();
				Repaint();

				return;
			}
			else if (debugFoldout == DebugFoldout.None)
			{
				GeneralEditor();
				Repaint();

				return;
			}

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				debugFoldout = DebugFoldout.None;

			EditorGUILayout.LabelField(
#if !MVC_COMMUNITY
				debugFoldout == DebugFoldout.AI ? "Artificial Intelligence (AI)" :
#endif
				debugFoldout.ToString(), EditorStyles.miniBoldLabel);

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();

			EditorGUI.indentLevel++;

			float orgLabelWidth = EditorGUIUtility.labelWidth;

			if (debugFoldout != DebugFoldout.Wheels)
				EditorGUIUtility.labelWidth = position.width * .5f;
			else
				EditorGUIUtility.labelWidth = position.width * .3f;

			switch (debugFoldout)
			{
				case DebugFoldout.Specifications:
					SpecificationsEditor();

					break;

				case DebugFoldout.Dimensions:
					DimensionsEditor();

					break;

				case DebugFoldout.Rigidbody:
					RigidbodyEditor();

					break;

				case DebugFoldout.Statistics:
					StatisticsEditor();

					break;

				case DebugFoldout.Inputs:
					InputsEditor();

					break;

				case DebugFoldout.Lights:
					LightsEditor();

					break;

				case DebugFoldout.Wheels:
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.BeginVertical();

					if (VehicleInstance.FrontLeftWheels == null)
						VehicleInstance.RefreshWheels();

					for (int i = 0; i < VehicleInstance.FrontLeftWheels.Length; i++)
						WheelEditor(VehicleInstance.FrontLeftWheels, i);

					EditorGUILayout.EndVertical();
					EditorGUILayout.BeginVertical();

					if (VehicleInstance.FrontCenterWheels == null)
						VehicleInstance.RefreshWheels();

					for (int i = 0; i < VehicleInstance.FrontCenterWheels.Length; i++)
						WheelEditor(VehicleInstance.FrontCenterWheels, i);

					EditorGUILayout.EndVertical();
					EditorGUILayout.BeginVertical();

					if (VehicleInstance.FrontRightWheels == null)
						VehicleInstance.RefreshWheels();

					for (int i = 0; i < VehicleInstance.FrontRightWheels.Length; i++)
						WheelEditor(VehicleInstance.FrontRightWheels, i);

					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.Space();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.BeginVertical();

					if (VehicleInstance.RearLeftWheels == null)
						VehicleInstance.RefreshWheels();

					for (int i = 0; i < VehicleInstance.RearLeftWheels.Length; i++)
						WheelEditor(VehicleInstance.RearLeftWheels, i);

					EditorGUILayout.EndVertical();
					EditorGUILayout.BeginVertical();

					if (VehicleInstance.RearCenterWheels == null)
						VehicleInstance.RefreshWheels();

					for (int i = 0; i < VehicleInstance.RearCenterWheels.Length; i++)
						WheelEditor(VehicleInstance.RearCenterWheels, i);

					EditorGUILayout.EndVertical();
					EditorGUILayout.BeginVertical();

					if (VehicleInstance.RearRightWheels == null)
						VehicleInstance.RefreshWheels();

					for (int i = 0; i < VehicleInstance.RearRightWheels.Length; i++)
						WheelEditor(VehicleInstance.RearRightWheels, i);

					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.Space();

					break;
#if !MVC_COMMUNITY

				case DebugFoldout.AI:
					AIEditor();

					break;
#endif
			}

			EditorGUIUtility.labelWidth = orgLabelWidth;

			EditorGUI.indentLevel--;

			EditorGUILayout.EndVertical();
			GUILayout.EndScrollView();
			Repaint();
		}
		private void GeneralEditor()
		{
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Edit Mode", ToolkitEditorUtility.MiddleCenterAlignedBoldLabel);
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Specifications", EditorStyles.miniBoldLabel);

			if (GUILayout.Button(EditorUtilities.Icons.ChevronRight, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				debugFoldout = DebugFoldout.Specifications;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Dimensions", EditorStyles.miniBoldLabel);

			if (GUILayout.Button(EditorUtilities.Icons.ChevronRight, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				debugFoldout = DebugFoldout.Dimensions;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Play Mode", ToolkitEditorUtility.MiddleCenterAlignedBoldLabel);
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Rigidbody", EditorStyles.miniBoldLabel);

			if (GUILayout.Button(EditorUtilities.Icons.ChevronRight, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				debugFoldout = DebugFoldout.Rigidbody;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Statistics", EditorStyles.miniBoldLabel);

			if (GUILayout.Button(EditorUtilities.Icons.ChevronRight, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				debugFoldout = DebugFoldout.Statistics;

			EditorGUILayout.EndHorizontal();

			if (!VehicleInstance.IsTrailer)
			{
				EditorGUILayout.BeginHorizontal(GUI.skin.box);
				EditorGUILayout.LabelField("Inputs", EditorStyles.miniBoldLabel);

				if (GUILayout.Button(EditorUtilities.Icons.ChevronRight, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					debugFoldout = DebugFoldout.Inputs;

				EditorGUILayout.EndHorizontal();
			}

			EditorGUI.BeginDisabledGroup(!Settings.useLights);
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Lights", EditorStyles.miniBoldLabel);

			if (GUILayout.Button(EditorUtilities.Icons.ChevronRight, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				debugFoldout = DebugFoldout.Lights;

			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.BeginHorizontal(GUI.skin.box);
			EditorGUILayout.LabelField("Wheels", EditorStyles.miniBoldLabel);

			if (GUILayout.Button(EditorUtilities.Icons.ChevronRight, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				debugFoldout = DebugFoldout.Wheels;

			EditorGUILayout.EndHorizontal();

#if !MVC_COMMUNITY
			if (!VehicleInstance.IsTrailer)
			{
				EditorGUILayout.BeginHorizontal(GUI.skin.box);
				EditorGUILayout.LabelField("Artificial Intelligence (AI)", EditorStyles.miniBoldLabel);

				if (GUILayout.Button(EditorUtilities.Icons.ChevronRight, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					debugFoldout = DebugFoldout.AI;

				EditorGUILayout.EndHorizontal();
			}
#endif

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
		}
		private void SpecificationsEditor()
		{
			if (VehicleInstance.IsTrailer)
				EditorGUILayout.LabelField("Curb Weight", Utility.NumberToValueWithUnit((VehicleInstance as VehicleTrailer).Behaviour.CurbWeight, Utility.Units.Weight, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
			else
			{
				EditorGUILayout.LabelField("Type", VehicleInstance.Behaviour.vehicleType.ToString(), EditorStyles.boldLabel);

				if (VehicleInstance.Behaviour.vehicleType == Vehicle.VehicleType.HeavyTruck)
					EditorGUILayout.LabelField("Class", VehicleInstance.Behaviour.heavyTruckClass.ToString(), EditorStyles.boldLabel);
				else
					EditorGUILayout.LabelField("Class", VehicleInstance.Behaviour.carClass.ToString(), EditorStyles.boldLabel);

				EditorGUILayout.LabelField("Curb Weight", Utility.NumberToValueWithUnit(VehicleInstance.Behaviour.CurbWeight, Utility.Units.Weight, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Top Speed", Utility.NumberToValueWithUnit(VehicleInstance.TopSpeed, Utility.Units.Speed, Settings.editorValuesUnit, 1), EditorStyles.boldLabel);
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Engine", VehicleInstance.Engine.Name, EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Power", $"{Utility.NumberToValueWithUnit(VehicleInstance.Behaviour.Power, Utility.Units.Power, Settings.editorPowerUnit, 1)} @ {Utility.NumberToValueWithUnit(VehicleInstance.Behaviour.PeakPowerRPM, "rpm", true)}", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Torque", $"{Utility.NumberToValueWithUnit(VehicleInstance.Behaviour.Torque, Utility.Units.Torque, Settings.editorTorqueUnit, 1)} @ {Utility.NumberToValueWithUnit(VehicleInstance.Behaviour.PeakTorqueRPM, "rpm", true)}", EditorStyles.boldLabel);

				string chargerName = "";

				if (VehicleInstance.Behaviour.IsTurbocharged)
					chargerName = VehicleInstance.Turbocharger ? VehicleInstance.Turbocharger.TurboCount.ToString() : "NULL";

				if (VehicleInstance.Behaviour.IsSupercharged)
					chargerName = VehicleInstance.Supercharger ? $"{(chargerName.IsNullOrEmpty() ? "" : $"{chargerName} | ")}{VehicleInstance.Supercharger.SuperchargerType}" : "NULL";

				if (chargerName != "NULL")
					chargerName += $"{(chargerName.IsNullOrEmpty() ? "" : " ")}{VehicleInstance.Behaviour.Aspiration}";

				EditorGUILayout.LabelField("Aspiration", chargerName, EditorStyles.boldLabel);
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Transmission", VehicleInstance.Transmission.Gearbox.ToString(), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Gears Count", VehicleInstance.Transmission.GearsCount.ToString(), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Powertrain", VehicleInstance.Drivetrain.ToString(), EditorStyles.boldLabel);
			}

			EditorGUILayout.Space();
		}
		private void DimensionsEditor()
		{
			EditorGUILayout.LabelField("Width", Utility.NumberToValueWithUnit(VehicleInstance.Bounds.size.x * 1000f, Utility.Units.SizeAccurate, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Height", Utility.NumberToValueWithUnit(VehicleInstance.Bounds.size.y * 1000f, Utility.Units.SizeAccurate, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Length", Utility.NumberToValueWithUnit(VehicleInstance.Bounds.size.z * 1000f, Utility.Units.SizeAccurate, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Front Track", Utility.NumberToValueWithUnit(VehicleInstance.FrontTrack * 1000f, Utility.Units.SizeAccurate, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Rear Track", Utility.NumberToValueWithUnit(VehicleInstance.RearTrack * 1000f, Utility.Units.SizeAccurate, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Wheel Base", Utility.NumberToValueWithUnit(VehicleInstance.WheelBase * 1000f, Utility.Units.SizeAccurate, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Weight Distribution", VehicleInstance.Stability.WeightDistribution.ToString(), EditorStyles.boldLabel);
			EditorGUILayout.Space();

			if (!VehicleInstance.IsTrailer)
			{
				EditorGUILayout.LabelField("Front Suspension", Utility.NumberToValueWithUnit(VehicleInstance.FrontSuspension.Length * 1000f, Utility.Units.SizeAccurate, Settings.editorValuesUnit, 1), EditorStyles.boldLabel);
#if !MVC_COMMUNITY
				EditorGUI.BeginDisabledGroup(!Settings.useSuspensionAdjustments);
				EditorGUILayout.LabelField("Length Stance", $"{(VehicleInstance.FrontSuspension.LengthStance > 0 ? "+" : "")}{Utility.NumberToValueWithUnit(VehicleInstance.FrontSuspension.LengthStance * VehicleInstance.FrontSuspension.Length * 1000f, Utility.Units.SizeAccurate, Settings.editorValuesUnit, 1)}", EditorStyles.boldLabel);
				EditorGUI.EndDisabledGroup();
#endif
				EditorGUILayout.LabelField("Stiffness", Utility.NumberToValueWithUnit(VehicleInstance.FrontSuspension.Stiffness, Utility.Units.Force, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Damper", Utility.NumberToValueWithUnit(VehicleInstance.FrontSuspension.Damper, Utility.Units.Force, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
#if !MVC_COMMUNITY
				EditorGUI.BeginDisabledGroup(!Settings.useSuspensionAdjustments);
				EditorGUILayout.LabelField("Camber", Utility.NumberToValueWithUnit(VehicleInstance.FrontSuspension.Camber, "°", 2), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Caster", Utility.NumberToValueWithUnit(VehicleInstance.FrontSuspension.Caster, "°", 2), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Toe", Utility.NumberToValueWithUnit(VehicleInstance.FrontSuspension.Toe, "°", 2), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Side Offset", Utility.NumberToValueWithUnit(VehicleInstance.FrontSuspension.SideOffset, "°", 2), EditorStyles.boldLabel);
				EditorGUI.EndDisabledGroup();
#endif
				EditorGUILayout.Space();
			}

			EditorGUILayout.LabelField($"{(VehicleInstance.IsTrailer ? "" : "Rear ")}Suspension", Utility.NumberToValueWithUnit(VehicleInstance.RearSuspension.Length * 1000f, Utility.Units.SizeAccurate, Settings.editorValuesUnit, 1), EditorStyles.boldLabel);
#if !MVC_COMMUNITY
			EditorGUI.BeginDisabledGroup(!Settings.useSuspensionAdjustments);
			EditorGUILayout.LabelField("Length Stance", $"{(VehicleInstance.RearSuspension.LengthStance > 0 ? "+" : "")}{Utility.NumberToValueWithUnit(VehicleInstance.RearSuspension.LengthStance * VehicleInstance.RearSuspension.Length * 1000f, Utility.Units.SizeAccurate, Settings.editorValuesUnit, 1)}", EditorStyles.boldLabel);
			EditorGUI.EndDisabledGroup();
#endif
			EditorGUILayout.LabelField("Stiffness", Utility.NumberToValueWithUnit(VehicleInstance.RearSuspension.Stiffness, Utility.Units.Force, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Damper", Utility.NumberToValueWithUnit(VehicleInstance.RearSuspension.Damper, Utility.Units.Force, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
#if !MVC_COMMUNITY
			EditorGUI.BeginDisabledGroup(!Settings.useSuspensionAdjustments);
			EditorGUILayout.LabelField("Camber", Utility.NumberToValueWithUnit(VehicleInstance.RearSuspension.Camber, "°", 2), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Caster", Utility.NumberToValueWithUnit(VehicleInstance.RearSuspension.Caster, "°", 2), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Toe", Utility.NumberToValueWithUnit(VehicleInstance.RearSuspension.Toe, "°", 2), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Side Offset", Utility.NumberToValueWithUnit(VehicleInstance.RearSuspension.SideOffset, "°", 2), EditorStyles.boldLabel);
			EditorGUI.EndDisabledGroup();
#endif
			EditorGUILayout.Space();
		}
		private void RigidbodyEditor()
		{
			EditorGUILayout.LabelField("Velocity", rigidbodyStats.velocity.ToString(), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Angular Velocity", rigidbodyStats.angularVelocity.ToString(), EditorStyles.boldLabel);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Max Angular Velocity", rigidbodyStats.maxAngularVelocityValue.ToString(), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Max Depenetration Velocity", rigidbodyStats.maxDepenetrationVelocityValue.ToString(), EditorStyles.boldLabel);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Drag", rigidbodyStats.dragValue.ToString(), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Angular Drag", rigidbodyStats.angularDragValue.ToString(), EditorStyles.boldLabel);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Center Of Mass", rigidbodyStats.centerOfMass.ToString(), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("World Center Of Mass", rigidbodyStats.worldCenterOfMass.ToString(), EditorStyles.boldLabel);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Inertia Tensor", rigidbodyStats.inertiaTensor.ToString(), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Inertia Tensor Rotation", rigidbodyStats.inertiaTensorRotation.ToString(), EditorStyles.boldLabel);
			EditorGUILayout.Space();
		}
		private void StatisticsEditor()
		{
			EditorGUILayout.LabelField("Current Speed", Utility.NumberToValueWithUnit(VehicleInstance.Stats.currentSpeed, Utility.Units.Speed, Settings.editorValuesUnit, true), EditorStyles.boldLabel);

			if (VehicleInstance.IsTrailer)
			{
				VehicleTrailer trailer = VehicleInstance as VehicleTrailer;

				EditorGUI.BeginDisabledGroup(!trailer.useBrakes);
				EditorGUILayout.LabelField("Brakes Torque", Utility.NumberToValueWithUnit(trailer.useBrakes ? trailer.Brakes.BrakeTorque * VehicleInstance.Inputs.Brake : 0f, Utility.Units.Torque, Settings.editorTorqueUnit, 1), EditorStyles.boldLabel);
				EditorGUI.EndDisabledGroup();
			}
			else
			{
				EditorGUILayout.LabelField("Steer Angle", Utility.NumberToValueWithUnit(VehicleInstance.Stats.steerAngle, "°", 1), EditorStyles.boldLabel);
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Engine RPM", Utility.NumberToValueWithUnit(VehicleInstance.Stats.engineRPM, "RPM", true), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Starting Engine", VehicleInstance.Stats.isEngineStarting ? "Yes" : "No", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Engine Running", VehicleInstance.Stats.isEngineRunning ? "Yes" : "No", EditorStyles.boldLabel);
				EditorGUI.BeginDisabledGroup(!Settings.useEngineStalling);
				EditorGUILayout.LabelField("Engine Stall", VehicleInstance.Stats.isEngineStall ? "Yes" : "No", EditorStyles.boldLabel);
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(!VehicleInstance.Behaviour.useRevLimiter);
				EditorGUILayout.LabelField("Rev-Limiting", VehicleInstance.Stats.isRevLimiting ? "Yes" : "No", EditorStyles.boldLabel);
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.LabelField("Over-Rev", VehicleInstance.Stats.isOverRev ? "Yes" : "No", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Under-Rev", VehicleInstance.Stats.isUnderRev ? "Yes" : "No", EditorStyles.boldLabel);
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Current Gear", VehicleInstance.CurrentGearToString.IsNullOrEmpty() ? "N" : VehicleInstance.CurrentGearToString, EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Changing Gear", VehicleInstance.Stats.isChangingGear ? "Yes" : "No", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Engine Speed", Utility.NumberToValueWithUnit(VehicleInstance.Stats.averageMotorWheelsSpeed, Utility.Units.Speed, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Minimum Engine Speed", Utility.NumberToValueWithUnit(VehicleInstance.Transmission.GetMinSpeedTarget(VehicleInstance.Inputs.Direction, VehicleInstance.Stats.currentGear) * math.abs(VehicleInstance.Inputs.Direction), Utility.Units.Speed, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Maximum Engine Speed", Utility.NumberToValueWithUnit(VehicleInstance.Transmission.GetSpeedTarget(VehicleInstance.Inputs.Direction, VehicleInstance.Stats.currentGear) * math.abs(VehicleInstance.Inputs.Direction), Utility.Units.Speed, Settings.editorValuesUnit, true), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Last Air Time", Utility.NumberToValueWithUnit(VehicleInstance.Stats.lastAirTime, Utility.Units.Time, Settings.editorValuesUnit, 3), EditorStyles.boldLabel);
				EditorGUILayout.Space();
#if !MVC_COMMUNITY
				EditorGUI.BeginDisabledGroup(!Settings.useFuelSystem);
				EditorGUILayout.LabelField(VehicleInstance.IsElectric ? "Batteries Capacity" : "Fuel Tank", $"{Utility.NumberToValueWithUnit(VehicleInstance.Stats.fuelTank, VehicleInstance.IsElectric ? Utility.Units.ElectricCapacity : Utility.Units.Liquid, Settings.editorValuesUnit, 1)} / {Utility.NumberToValueWithUnit(VehicleInstance.Behaviour.FuelCapacity, VehicleInstance.IsElectric ? Utility.Units.ElectricCapacity : Utility.Units.Liquid, Settings.editorValuesUnit, 1)}", EditorStyles.boldLabel);
				EditorGUILayout.LabelField(VehicleInstance.IsElectric ? "Power Consumption" : "Fuel Consumption", Utility.NumberToValueWithUnit(VehicleInstance.Stats.fuelConsumption, VehicleInstance.IsElectric ? Utility.Units.ElectricConsumption : Utility.Units.FuelConsumption, Settings.editorValuesUnit, 1), EditorStyles.boldLabel);
				EditorGUI.EndDisabledGroup();
#endif
				EditorGUILayout.LabelField("Wheels Power", Utility.NumberToValueWithUnit(VehicleInstance.Stats.wheelPower, Utility.Units.Power, Settings.editorPowerUnit, 1), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Wheels Torque", Utility.NumberToValueWithUnit(VehicleInstance.Stats.wheelTorque, Utility.Units.Torque, Settings.editorTorqueUnit, 1), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Engine Power", Utility.NumberToValueWithUnit(VehicleInstance.Stats.enginePower, Utility.Units.Power, Settings.editorPowerUnit, 1), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Engine Torque", Utility.NumberToValueWithUnit(VehicleInstance.Stats.engineTorque, Utility.Units.Torque, Settings.editorTorqueUnit, 1), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Engine Brake Torque", Utility.NumberToValueWithUnit(VehicleInstance.Stats.engineBrakeTorque, Utility.Units.Torque, Settings.editorTorqueUnit, 1), EditorStyles.boldLabel);
#if !MVC_COMMUNITY
				EditorGUILayout.LabelField("Chargers Boost", Utility.Round(VehicleInstance.Stats.engineBoost, 2).ToString("0.00"), EditorStyles.boldLabel);
#endif
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Braking Distance", Utility.NumberToValueWithUnit(VehicleInstance.Stats.brakingDistance, Utility.Units.Distance, Settings.editorValuesUnit, 1), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Front Brakes Torque", Utility.NumberToValueWithUnit(VehicleInstance.Behaviour.FrontBrakes.BrakeTorque * VehicleInstance.Inputs.Brake * VehicleInstance.FrontWheels.Length / VehicleInstance.Wheels.Length, Utility.Units.Torque, Settings.editorTorqueUnit, 1), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Rear Brakes Torque", Utility.NumberToValueWithUnit(VehicleInstance.Behaviour.RearBrakes.BrakeTorque * Utility.Clamp01(VehicleInstance.Inputs.Brake + VehicleInstance.Inputs.Handbrake) * VehicleInstance.RearWheels.Length / VehicleInstance.Wheels.Length, Utility.Units.Torque, Settings.editorTorqueUnit, 1), EditorStyles.boldLabel);
			}

			EditorGUILayout.Space();

			if (!VehicleInstance.IsTrailer)
			{
				EditorGUI.BeginDisabledGroup(!VehicleInstance.Behaviour.useNOS);
				EditorGUILayout.LabelField("NOS Active", VehicleInstance.Stats.isNOSActive ? "Yes" : "No", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("NOS Capacity", Utility.NumberToValueWithUnit(VehicleInstance.Stats.NOS, Utility.Units.Weight, Settings.editorValuesUnit, 2), EditorStyles.boldLabel);
				EditorGUILayout.LabelField("NOS Boost", Utility.Round(VehicleInstance.Stats.NOSBoost, 2).ToString("0.00"), EditorStyles.boldLabel);
				EditorGUI.EndDisabledGroup();
				EditorGUILayout.Space();
			}

			EditorGUILayout.LabelField("Average Forward Slip", Utility.Round(VehicleInstance.Stats.averageMotorWheelsForwardSlip, 2).ToString("0.00"), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Average Sideways Slip", Utility.Round(VehicleInstance.Stats.averageMotorWheelsSidewaysSlip, 2).ToString("0.00"), EditorStyles.boldLabel);

			if (!VehicleInstance.IsTrailer)
			{
				EditorGUI.BeginDisabledGroup(!VehicleInstance.Stability.useABS);
				EditorGUILayout.LabelField("ABS Active", VehicleInstance.Stats.isABSActive ? "Yes" : "No", EditorStyles.boldLabel);
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(!VehicleInstance.Stability.useESP);
				EditorGUILayout.LabelField("ESP Active", VehicleInstance.Stats.isESPActive ? "Yes" : "No", EditorStyles.boldLabel);
				EditorGUI.EndDisabledGroup();
				EditorGUI.BeginDisabledGroup(!VehicleInstance.Stability.useTCS);
				EditorGUILayout.LabelField("TCS Active", VehicleInstance.Stats.isTCSActive ? "Yes" : "No", EditorStyles.boldLabel);
				EditorGUI.BeginDisabledGroup(!VehicleInstance.Stability.UseLaunchControl);
				EditorGUILayout.LabelField("Launch Control", VehicleInstance.Stats.isLaunchControlActive ? "Yes" : "No", EditorStyles.boldLabel);
				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
			}

			EditorGUILayout.Space();
		}
		private void LightsEditor()
		{
			bool isEngineRunning = VehicleInstance.Stats.isEngineRunning || VehicleInstance.Stats.isEngineStarting || VehicleInstance.Stats.isEngineStall;
			bool isLightsOn = VehicleInstance.Stats.isLightsOn || Manager.nightMode || VehicleInstance.Stats.inDarknessWeatherZone;

			if (!VehicleInstance.IsTrailer)
			{
				EditorGUILayout.LabelField("Headlights", isLightsOn ? "On" : "Off", EditorStyles.boldLabel);

				EditorGUI.indentLevel++;

				EditorGUILayout.LabelField("High-beam Lights", (isEngineRunning || isLightsOn) && VehicleInstance.Stats.isHighBeamHeadlightsOn ? "On" : "Off", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Fog Lights", (isEngineRunning || isLightsOn) && (Manager.fogTime || VehicleInstance.Stats.inFogWeatherZone) ? "On" : "Off", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("Daytime Lights", isEngineRunning || isLightsOn ? "On" : "Off", EditorStyles.boldLabel);
				EditorGUILayout.LabelField("License Plate Lights", isLightsOn ? "On" : "Off", EditorStyles.boldLabel);

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.LabelField("Hazard Lights", VehicleInstance.Stats.isHazardLightsOn ? "On" : "Off", EditorStyles.boldLabel);

			EditorGUI.indentLevel++;
			
			EditorGUILayout.LabelField("Left Side Lights", VehicleInstance.Stats.isSignalLeftLightsOn ? "On" : "Off", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Right Side Lights", VehicleInstance.Stats.isSignalRightLightsOn ? "On" : "Off", EditorStyles.boldLabel);

			EditorGUI.indentLevel--;

#if !MVC_COMMUNITY
			EditorGUILayout.LabelField("Interior Lights", VehicleInstance.Stats.isInteriorLightsOn ? "On" : "Off", EditorStyles.boldLabel);
#endif
			EditorGUILayout.LabelField("Taillights", isLightsOn || Manager.fogTime || VehicleInstance.Stats.inFogWeatherZone ? "On" : "Off", EditorStyles.boldLabel);

			EditorGUI.indentLevel++;

			EditorGUILayout.LabelField("Daytime Lights", isEngineRunning || isLightsOn ? "On" : "Off", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Fog Lights", (isEngineRunning || isLightsOn) && (Manager.fogTime || VehicleInstance.Stats.inFogWeatherZone) ? "On" : "Off", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("License Plate Lights", isLightsOn ? "On" : "Off", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Brake Lights", (isEngineRunning || isLightsOn) && VehicleInstance.Inputs.Brake > 0f ? "On" : "Off", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Reverse Lights", (isEngineRunning || isLightsOn) && VehicleInstance.Inputs.Direction < 0 ? "On" : "Off", EditorStyles.boldLabel);

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
		}
		private void InputsEditor()
		{
			if (!VehicleInstance.IsTrailer)
				EditorGUILayout.LabelField("Steering Wheel", Utility.Round(VehicleInstance.Inputs.SteeringWheel, 2).ToString("0.00"), EditorStyles.boldLabel);
			
			EditorGUILayout.LabelField("Fuel Pedal", Utility.Round(VehicleInstance.Inputs.FuelPedal, 2).ToString("0.00"), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Brake Pedal", Utility.Round(VehicleInstance.Inputs.BrakePedal, 2).ToString("0.00"), EditorStyles.boldLabel);
			
			if (!VehicleInstance.IsTrailer)
				EditorGUILayout.LabelField("Clutch Pedal", Utility.Round(VehicleInstance.Inputs.ClutchPedal, 2).ToString("0.00"), EditorStyles.boldLabel);
			
			EditorGUILayout.LabelField("Handbrake", Utility.Round(VehicleInstance.Inputs.Handbrake, 2).ToString("0.00"), EditorStyles.boldLabel);
			
			if (!VehicleInstance.IsTrailer)
				EditorGUILayout.LabelField("NOS Button", VehicleInstance.Inputs.NOS ? "Yes" : "No", EditorStyles.boldLabel);
			
			EditorGUILayout.Space();
			
			if (!VehicleInstance.IsTrailer)
				EditorGUILayout.LabelField("Fuel", Utility.Round(VehicleInstance.Inputs.Fuel, 2).ToString("0.00"), EditorStyles.boldLabel);
			
			EditorGUILayout.LabelField("Brake", Utility.Round(VehicleInstance.Inputs.Brake, 2).ToString("0.00"), EditorStyles.boldLabel);
			
			if (!VehicleInstance.IsTrailer)
				EditorGUILayout.LabelField("Clutch", Utility.Round(VehicleInstance.Inputs.Clutch, 2).ToString("0.00"), EditorStyles.boldLabel);

			EditorGUILayout.Space();
		}
		private void WheelEditor(VehicleWheel.WheelModule[] wheels, int index)
		{
			if (!wheels[index])
				VehicleInstance.RefreshWheels();

			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField(wheels[index].WheelName, EditorStyles.miniBoldLabel);
			EditorGUILayout.EndVertical();
			EditorGUILayout.LabelField("Mass", Utility.NumberToValueWithUnit(wheels[index].Instance ? wheels[index].Instance.mass : 0f, Utility.Units.Weight, Settings.editorValuesUnit, 1), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Radius", Utility.NumberToValueWithUnit(wheels[index].Instance ? wheels[index].Instance.Radius * 100f : 0f, Utility.Units.Size, Settings.editorValuesUnit, 1), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Width", Utility.NumberToValueWithUnit(wheels[index].Instance ? wheels[index].Instance.Width * 100f : 0f, Utility.Units.Size, Settings.editorValuesUnit, 1), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Tire Thickness", Utility.NumberToValueWithUnit(wheels[index].Instance ? wheels[index].Instance.tireThickness : 0f, Utility.Units.Size, Settings.editorValuesUnit, 2), EditorStyles.boldLabel);
			EditorGUILayout.Space();
			
			if (!VehicleInstance.IsTrailer)
				EditorGUILayout.LabelField("Steer Angle", Utility.NumberToValueWithUnit(wheels[index].Instance ? wheels[index].Instance.steerAngle : 0f, "°", 1), EditorStyles.boldLabel);

			EditorGUILayout.LabelField("RPM", Utility.NumberToValueWithUnit(wheels[index].Instance ? wheels[index].Instance.RPM : 0f, "RPM", true), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Speed", Utility.NumberToValueWithUnit(wheels[index].Instance ? wheels[index].Instance.Speed : 0f, Utility.Units.Speed, Utility.UnitType.Metric, true), EditorStyles.boldLabel);
			
			if (!VehicleInstance.IsTrailer)
				EditorGUILayout.LabelField("Torque", Utility.NumberToValueWithUnit(wheels[index].Instance ? wheels[index].Instance.motorTorque : 0f, Utility.Units.Torque, Settings.editorTorqueUnit, 1), EditorStyles.boldLabel);

			EditorGUI.BeginDisabledGroup(VehicleInstance.IsTrailer && !(VehicleInstance as VehicleTrailer).useBrakes);
			EditorGUILayout.LabelField("Brake Torque", Utility.NumberToValueWithUnit(wheels[index].Instance ? wheels[index].Instance.brakeTorque : 0f, Utility.Units.Torque, Settings.editorTorqueUnit, 1), EditorStyles.boldLabel);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			if (Settings.useDamage && Settings.useWheelHealth)
				EditorGUILayout.LabelField("Tire Health", $"{Utility.Round(wheels[index].Instance ? wheels[index].Instance.TireHealth * 100f : 0f, 1):0.0}%", EditorStyles.boldLabel);
			
			EditorGUILayout.LabelField("Tire Temperature", $"{Utility.Round(wheels[index].Instance ? wheels[index].Instance.TireTemperature * 100f : 0f, 1):0.0}%", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Rim Temperature", $"{Utility.Round(wheels[index].Instance ? wheels[index].Instance.RimTemperature * 100f : 0f, 1):0.0}%", EditorStyles.boldLabel);

			if (Settings.useParticleSystems && Settings.useBrakesHeat)
			{
				EditorGUI.BeginDisabledGroup(VehicleInstance.IsTrailer && !(VehicleInstance as VehicleTrailer).useBrakes);
				EditorGUILayout.LabelField("Brake Temperature", $"{Utility.Round(wheels[index].Instance ? wheels[index].Instance.BrakeTemperature * 100f : 0f, 1):0.0}%", EditorStyles.boldLabel);
				EditorGUI.BeginDisabledGroup(!Settings.brakeHeatAffectPerformance);
				EditorGUILayout.LabelField("Brake Torque Loss", $"{Utility.Round(wheels[index].Instance ? wheels[index].Instance.BrakeTorqueLoss * 100f : 0f, 1):0.0}%", EditorStyles.boldLabel);
				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Forward Friction", Utility.Round(wheels[index].Instance ? wheels[index].Instance.CurrentWheelColliderForwardFrictionStiffness : 0f, 2).ToString("0.00"), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Forward Slip", Utility.Round(wheels[index].Instance ? wheels[index].Instance.HitInfo.forwardSlip : 0f, 2).ToString("0.00"), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Sideways Friction", Utility.Round(wheels[index].Instance ? wheels[index].Instance.CurrentWheelColliderSidewaysFrictionStiffness : 0f, 2).ToString("0.00"), EditorStyles.boldLabel);
			EditorGUILayout.LabelField("Sideways Slip", Utility.Round(wheels[index].Instance ? wheels[index].Instance.HitInfo.sidewaysSlip : 0f, 2).ToString("0.00"), EditorStyles.boldLabel);
			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();

			if (index + 1 < wheels.Length)
				EditorGUILayout.Space();
		}
#if !MVC_COMMUNITY
		private void AIEditor()
		{
			if (!VehicleInstance.IsAI)
			{
				EditorGUI.indentLevel--;

				EditorGUILayout.HelpBox("This vehicle instance doesn't have an AI behaviour attached to it!", MessageType.Info);
			   
				EditorGUI.indentLevel++;

				return;
			}

			//EditorGUILayout.LabelField("Type", VehicleInstance.AI.type.ToString(), EditorStyles.boldLabel);
			
			// TODO: Add AI stats editor here

			EditorGUILayout.Space();
		}
#endif

		#endregion

		#region Enable

		private void OnEnable()
		{
			VehicleManager.GetOrCreateInstance();
		}

		#endregion

		#endregion

		#endregion
	}
}

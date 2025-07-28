#region Namespaces

using System.IO;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Audio;
using UnityEditor;
using Utilities;
using MVC.Core;
using MVC.Editor;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Base.Editor
{
	public class VehicleEngineEditor : ToolkitEditorWindow
	{
		#region Variables

		private Vector2 scrollView;
		private int engineIndex;

		#endregion

		#region Methods

		#region Virtual Methods

		public virtual void EngineEditorGUI(ref VehicleEngine engine, string engineName)
		{
			EngineEditor(ref engine, engineName, this);
		}

		#endregion

		#region Static Methods

		#region Utility

		public static void SmoothOutPowerCurve(Vehicle vehicle)
		{
			SmoothOutCurve(vehicle.Behaviour.PowerCurve);
			vehicle.Behaviour.PowerCurve.AddKey(vehicle.Engine.RedlineRPM, vehicle.Behaviour.PowerCurve.Evaluate(vehicle.Behaviour.PeakPowerRPM - (vehicle.Engine.RedlineRPM - vehicle.Behaviour.PeakPowerRPM)));
			SmoothOutCurve(vehicle.Behaviour.PowerCurve);
		}
		public static void SmoothOutTorqueCurve(Vehicle vehicle)
		{
			SmoothOutCurve(vehicle.Behaviour.TorqueCurve);
		}

		private static void SmoothOutCurve(AnimationCurve curve)
		{
			for (int i = 0; i < curve.keys.Length; i++)
			{
				AnimationUtility.SetKeyLeftTangentMode(curve, i, i < curve.keys.Length - 1 ? AnimationUtility.TangentMode.ClampedAuto : AnimationUtility.TangentMode.Auto);
				AnimationUtility.SetKeyRightTangentMode(curve, i, i > 0 ? AnimationUtility.TangentMode.ClampedAuto : AnimationUtility.TangentMode.Auto);
			}
		}

		#endregion

		#region Editor

		public static void OpenEngineWindow(int engineIndex)
		{
			string title = Settings.Engines[engineIndex].Name;
			VehicleEngineEditor editorInstance = GetWindow<VehicleEngineEditor>(false, title, true);

			editorInstance.engineIndex = engineIndex;
			editorInstance.titleContent = new(title, EditorGUIUtility.IconContent("Settings").image);
			editorInstance.minSize = new(450f, 600f);
		}
		public static void EngineEditor(ref VehicleEngine engine, string engineName, ToolkitEditorWindow editor)
		{
			if (HasInternalErrors || !IsSetupDone)
				return;

			bool engineChanged = false;

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);

			VehicleEngine.EngineType newEngineType = (VehicleEngine.EngineType)EditorGUILayout.EnumPopup(new GUIContent("Alignment", "The engine cylinders alignment type"), engine.Type);

			if (engine.Type != newEngineType)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Alignment");

				engine.Type = newEngineType;
				engineChanged = true;
			}

			if (!engine.IsElectric)
			{
				EditorGUI.BeginDisabledGroup(engine.Type == VehicleEngine.EngineType.Rotary);

				VehicleEngine.EngineFuelType newFuelType = (VehicleEngine.EngineFuelType)EditorGUILayout.EnumPopup(new GUIContent("Fuel Type", "The engine cylinders alignment type"), engine.Type == VehicleEngine.EngineType.Rotary ? VehicleEngine.EngineFuelType.Gas : engine.FuelType);

				if (engine.FuelType != newFuelType)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Fuel Type");

					engineChanged = true;
					engine.FuelType = newFuelType;
				}

				EditorGUI.EndDisabledGroup();

				int newCylindersCount = ToolkitEditorUtility.Slider(new GUIContent($"{(engine.Type == VehicleEngine.EngineType.Rotary ? "Rotor" : "Cylinder")}s Count", $"The number of the engine {(engine.Type == VehicleEngine.EngineType.Rotary ? "rotor" : "cylinder")}s"), engine.CylinderCount, 1, 32, Settings, "Change Cylinders Count");

				if (engine.CylinderCount != newCylindersCount)
				{
					engineChanged = true;
					engine.CylinderCount = newCylindersCount;
				}
			}

			EditorGUILayout.BeginHorizontal();

			float newMass = ToolkitEditorUtility.NumberField(new GUIContent("Mass", "The engine mass"), engine.Mass, Utility.Units.Weight, 1, Settings, "Change Mass");

			if (engine.Mass != newMass)
			{
				engine.Mass = newMass;
				engineChanged = true;
			}

			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
			EditorGUI.BeginDisabledGroup(engine.IsElectric);

			float newMinimumRPM = ToolkitEditorUtility.NumberField(new GUIContent("Minimum RPM", "The engine minimum RPM"), engine.MinimumRPM, "rpm", "Revolutions per Minute", true, Settings, "Change Min RPM");

			if (engine.MinimumRPM != newMinimumRPM)
			{
				engineChanged = true;
				engine.MinimumRPM = newMinimumRPM;
			}

			EditorGUI.EndDisabledGroup();

			float newRedlineRPM = ToolkitEditorUtility.NumberField(new GUIContent("Redline RPM", "The engine redline RPM"), engine.RedlineRPM, "rpm", "Revolutions per Minute", true, Settings, "Change Redline RPM");

			if (engine.RedlineRPM != newRedlineRPM)
			{
				engine.RedlineRPM = newRedlineRPM;
				engineChanged = true;
			}

			float newOverRevRPM = ToolkitEditorUtility.NumberField(new GUIContent("Over-Rev RPM", "The engine over-rev RPM"), engine.OverRevRPM, "rpm", "Revolutions per Minute", true, Settings, "Change Over-Rev RPM");

			if (engine.OverRevRPM != newOverRevRPM)
			{
				engine.OverRevRPM = newOverRevRPM;
				engineChanged = true;
			}

			float newMaximumRPM = ToolkitEditorUtility.NumberField(new GUIContent("Maximum RPM", "The engine maximum RPM"), engine.MaximumRPM, "rpm", "Revolutions per Minute", true, Settings, "Change Max RPM");

			if (engine.MaximumRPM != newMaximumRPM)
			{
				engine.MaximumRPM = newMaximumRPM;
				engineChanged = true;
			}

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUILayout.BeginHorizontal();

			Color orgGUIColor = GUI.color;
			float orgLabelWidth = EditorGUIUtility.labelWidth;
			float newPower = ToolkitEditorUtility.NumberField(new GUIContent("Power", "The engine power"), engine.Power, Utility.Units.Power, 1, Settings, "Change Power");

			EditorGUIUtility.labelWidth = 20f;

			if (engine.PeakPowerRPM > engine.OverRevRPM || engine.PeakPowerRPM < engine.MinimumRPM)
				GUI.color = Utility.Color.orange;

			float newPeakPowerRPM = ToolkitEditorUtility.NumberField(new GUIContent(" @ ", "The engine power peak RPM. This value can be overridden in the Vehicle's inspector"), engine.PeakPowerRPM, "rpm", "Revolutions per Minute", true, Settings, "Change Peak RPM");

			EditorGUIUtility.labelWidth = orgLabelWidth;
			GUI.color = orgGUIColor;

			if (engine.PeakPowerRPM != newPeakPowerRPM)
			{
				engine.PeakPowerRPM = newPeakPowerRPM;
				engineChanged = true;
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();

			if (engine.Power != newPower)
			{
				engine.Power = newPower;
				engineChanged = true;
			}

			float newTorque = ToolkitEditorUtility.NumberField(new GUIContent("Torque", "The engine torque"), engine.Torque, Utility.Units.Torque, 1, Settings, "Change Torque");

			EditorGUIUtility.labelWidth = 20f;

			if (engine.PeakTorqueRPM > engine.OverRevRPM || engine.PeakTorqueRPM < engine.MinimumRPM)
				GUI.color = Utility.Color.orange;

			float newPeakTorqueRPM = ToolkitEditorUtility.NumberField(new GUIContent(" @ ", "The engine torque peak RPM. This value can be overridden in the Vehicle's inspector"), engine.PeakTorqueRPM, "rpm", "Revolutions per Minute", true, Settings, "Change Peak RPM");

			EditorGUIUtility.labelWidth = orgLabelWidth;
			GUI.color = orgGUIColor;

			if (engine.PeakTorqueRPM != newPeakTorqueRPM)
			{
				engine.PeakTorqueRPM = newPeakTorqueRPM;
				engineChanged = true;
			}

			EditorGUILayout.EndHorizontal();

			if (engine.Torque != newTorque)
			{
				engine.Torque = newTorque;
				engineChanged = true;
			}

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			VehicleEngine.AudioModule engineAudio = engine.Audio;

			EditorGUILayout.LabelField("Sound Effects", EditorStyles.boldLabel);

			string fullAudioPath = Path.Combine(Application.dataPath, "BxB Studio", "MVC", "Resources", Settings.EngineSFXFolderPath, engineAudio.folderName.IsNullOrEmpty() ? engineName : engineAudio.folderName).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
			bool soundsFolderExists = Directory.Exists(fullAudioPath);

			EditorGUILayout.BeginHorizontal();
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUILayout.PrefixLabel(new GUIContent("Folder Name", "The folder that contains the engine audio files"));
			EditorGUILayout.TextField(engineAudio.folderName.IsNullOrEmpty() ? "Using Engine Name..." : engineAudio.folderName);

			if (GUILayout.Button("...", new GUIStyle(GUI.skin.button) { stretchWidth = false }))
			{
				string newFolderName = EditorUtility.OpenFolderPanel("Choose a folder....", Path.Combine(Application.dataPath, "BxB Studio", "MVC", "Resources", Settings.EngineSFXFolderPath), engine.Name).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

				if (!newFolderName.IsNullOrEmpty())
				{
					bool isPathValid = newFolderName.Contains(Path.Combine(Application.dataPath, "BxB Studio", "MVC", "Resources", Settings.EngineSFXFolderPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

					if (!isPathValid)
						EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", $"The engine audio files directory has to be directly inside the Engines SFX folder: \"{Path.Combine(Path.GetFileName(Path.GetDirectoryName(Application.dataPath)), "Assets", "BxB Studio", "MVC", "Resources", Settings.EngineSFXFolderPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar}\" and not as a subfolder!", "Got it!");
					else
					{
						newFolderName = newFolderName.Replace(Path.Combine(Application.dataPath, "BxB Studio", "MVC", "Resources", Settings.EngineSFXFolderPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, "");
						isPathValid = isPathValid && Path.GetFileName(newFolderName) == newFolderName;

						if (!isPathValid)
							EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", $"The engine audio files directory has to be directly inside the Engines SFX folder: \"{Path.Combine(Path.GetFileName(Path.GetDirectoryName(Application.dataPath)), "Assets", "BxB Studio", "MVC", "Resources", Settings.EngineSFXFolderPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar}\" and not as a subfolder!", "Got it!");
						else
						{
							Undo.RegisterCompleteObjectUndo(Settings, "Change Folder Name");

							engineAudio.folderName = newFolderName;
							engineChanged = true;
						}
					}
				}
			}

			EditorGUI.BeginDisabledGroup(engineAudio.folderName.IsNullOrEmpty());

			if (GUILayout.Button("Clear", new GUIStyle(GUI.skin.button) { stretchWidth = false }))
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Clear Folder Name");

				engineAudio.folderName = string.Empty;
				engineChanged = true;
			}

			EditorGUI.EndDisabledGroup();
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();

			if (!soundsFolderExists)
				EditorGUILayout.HelpBox($"The directory \"{Path.GetFileName(fullAudioPath)}\" doesn't exist!", MessageType.Warning);
			else if (Utility.IsDirectoryEmpty(fullAudioPath))
				EditorGUILayout.HelpBox("The current folder seem to be empty, therefore the audio system is not going to run correctly!", MessageType.Warning);

			bool hasEngineFolder = Directory.Exists(Path.Combine(fullAudioPath, "Engine"));
			bool hasExhaustFolder = !engine.IsElectric && Directory.Exists(Path.Combine(fullAudioPath, "Exhaust"));

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUI.BeginDisabledGroup(!soundsFolderExists);
			EditorGUI.BeginDisabledGroup(!hasEngineFolder || !hasExhaustFolder);

			VehicleEngine.AudioModule.OutputType newOutputs = (VehicleEngine.AudioModule.OutputType)EditorGUILayout.EnumPopup(new GUIContent("Outputs", "The vehicle's engine available audio files"), engine.IsElectric || soundsFolderExists && hasEngineFolder && !hasExhaustFolder ? VehicleEngine.AudioModule.OutputType.Engine : soundsFolderExists && !hasEngineFolder && hasExhaustFolder ? VehicleEngine.AudioModule.OutputType.Exhaust : engineAudio.outputs);

			if (engineAudio.outputs != newOutputs)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Audio Outputs");

				engineAudio.outputs = newOutputs;
				engineChanged = true;
			}

			EditorGUI.EndDisabledGroup();

			if (!engine.IsElectric)
			{
				bool hasCelerationFolders = false;

				if (engineAudio.outputs == VehicleEngine.AudioModule.OutputType.Engine || engineAudio.outputs == VehicleEngine.AudioModule.OutputType.EngineAndExhaust)
					hasCelerationFolders = hasCelerationFolders || hasEngineFolder && hasCelerationFolders && Directory.Exists(Path.Combine(fullAudioPath, "Engine", "Accel")) && Directory.Exists(Path.Combine(fullAudioPath, "Engine", "Decel"));

				if (engineAudio.outputs == VehicleEngine.AudioModule.OutputType.Exhaust || engineAudio.outputs == VehicleEngine.AudioModule.OutputType.EngineAndExhaust)
					hasCelerationFolders = hasCelerationFolders || hasExhaustFolder && hasCelerationFolders && Directory.Exists(Path.Combine(fullAudioPath, "Engine", "Accel")) && Directory.Exists(Path.Combine(fullAudioPath, "Engine", "Decel"));

				EditorGUI.BeginDisabledGroup(!hasCelerationFolders);

				VehicleEngine.AudioModule.CelerationType newCelerationType = (VehicleEngine.AudioModule.CelerationType)EditorGUILayout.EnumPopup(new GUIContent("Celeration", "The vehicle's engine available celeration audio files"), hasCelerationFolders ? engineAudio.celerationType : VehicleEngine.AudioModule.CelerationType.Merged);

				if (engineAudio.celerationType != newCelerationType)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Celeration Type");

					engineAudio.celerationType = newCelerationType;
					engineChanged = true;
				}

				EditorGUI.EndDisabledGroup();
			}

			EditorGUI.EndDisabledGroup();

			if (!engine.IsElectric)
			{
				AudioClip newStartingClip = EditorGUILayout.ObjectField(new GUIContent("Starting Clip", "The engine starting clip"), engineAudio.startingClip, typeof(AudioClip), false) as AudioClip;

				if (engineAudio.startingClip != newStartingClip)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Starting Clip");

					engineAudio.startingClip = newStartingClip;
					engineChanged = true;
				}
			}

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Audio Mixing", EditorStyles.boldLabel);
			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUILayout.BeginHorizontal();

			AudioMixerGroup newEngineMixer = EditorGUILayout.ObjectField(new GUIContent("Engine", "The engine audio mixer group"), engineAudio.mixerGroups.engine, typeof(AudioMixerGroup), false) as AudioMixerGroup;

			if (engineAudio.mixerGroups.engine != newEngineMixer)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Audio Mixer");

				engineAudio.mixerGroups.engine = newEngineMixer;
				engineChanged = true;
			}

			EditorGUILayout.EndHorizontal();

			if (!engine.IsElectric)
			{
				EditorGUILayout.BeginHorizontal();

				AudioMixerGroup newExhaustMixer = EditorGUILayout.ObjectField(new GUIContent("Exhaust", "The engine exhaust audio mixer group"), engineAudio.mixerGroups.exhaust, typeof(AudioMixerGroup), false) as AudioMixerGroup;

				if (engineAudio.mixerGroups.exhaust != newExhaustMixer)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Audio Mixer");

					engineAudio.mixerGroups.exhaust = newExhaustMixer;
					engineChanged = true;
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUI.EndDisabledGroup();

			float newLowVolume = ToolkitEditorUtility.Slider(new GUIContent("Low RPM Volume", "The sound effects volume at low RPMs"), engineAudio.lowRPMVolume * 100f, 0f, 100f, "%", "Percentage", Settings, "Change Volume") * .01f;

			if (engineAudio.lowRPMVolume != newLowVolume)
			{
				engineAudio.lowRPMVolume = newLowVolume;
				engineChanged = true;
			}

			if (!engine.IsElectric)
			{
				float newDistortionAtMaxRPM = ToolkitEditorUtility.Slider(new GUIContent("Max RPMs Distortion", "The engine sound distortion effect value at higher rpms"), engineAudio.maxRPMDistortion, 0f, 1f, Settings, "Change Distortion");

				if (engineAudio.maxRPMDistortion != newDistortionAtMaxRPM)
				{
					engineAudio.maxRPMDistortion = newDistortionAtMaxRPM;
					engineChanged = true;
				}

				float newDistortionAtOverRPM = ToolkitEditorUtility.Slider(new GUIContent("Rev Limiter Distortion", "The engine sound distortion effect value while Rev-Limiting"), engineAudio.overRPMDistortion, 0f, 1f, Settings, "Change Distortion");

				if (engineAudio.overRPMDistortion != newDistortionAtOverRPM)
				{
					engineAudio.overRPMDistortion = newDistortionAtOverRPM;
					engineChanged = true;
				}
			}

			float newDecelLowPassFrequency = ToolkitEditorUtility.Slider(new GUIContent("Deceleration Low-Pass Freq.", "The engine deceleration sound low-pass filter frequency"), engineAudio.decelLowPassFrequency, 10f, 22000f, Utility.Units.Frequency, Settings, "Change Low-Pass Freq.");

			if (engineAudio.decelLowPassFrequency != newDecelLowPassFrequency)
			{
				engineAudio.decelLowPassFrequency = newDecelLowPassFrequency;
				engineChanged = true;
			}

			bool newUseAccelLowPass = ToolkitEditorUtility.ToggleButtons(new GUIContent("Acceleration Low-Pass", "If yes, a low-pass frequency filter will be applied to the engine sound at lower RPMs when accelerating to help remove some unwanted noises."), null, "On", "Off", engineAudio.useAccelLowPass, Settings, "Switch Low-Pass");
			
			if (engineAudio.useAccelLowPass != newUseAccelLowPass)
			{
				engineAudio.useAccelLowPass = newUseAccelLowPass;
				engineChanged = true;
			}

			EditorGUI.BeginDisabledGroup(!engineAudio.useAccelLowPass);

			EditorGUI.indentLevel++;

			float newAccelLowPassFrequency = ToolkitEditorUtility.Slider(new GUIContent("Frequency", "The engine acceleration sound low-pass filter frequency"), engineAudio.accelLowPassFrequency, 10f, 22000f, Utility.Units.Frequency, Settings, "Change Low-Pass Freq.");

			if (engineAudio.accelLowPassFrequency != newAccelLowPassFrequency)
			{
				engineAudio.accelLowPassFrequency = newAccelLowPassFrequency;
				engineChanged = true;
			}

			float newAccelLowPassRPMEnd = Mathf.InverseLerp(engine.MinimumRPM, engine.OverRevRPM, ToolkitEditorUtility.Slider(new GUIContent("End RPM", "The RPM at which the low-pass filter becomes transparent (passive) while accelerating"), Mathf.Lerp(engine.MinimumRPM, engine.OverRevRPM, engineAudio.accelLowPassRPMEnd), engine.MinimumRPM, engine.OverRevRPM));

			if (engineAudio.accelLowPassRPMEnd != newAccelLowPassRPMEnd)
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Low-Pass End");

				engineAudio.accelLowPassRPMEnd = newAccelLowPassRPMEnd;
				engineChanged = true;
			}

			bool newUseLowPassDamping = ToolkitEditorUtility.ToggleButtons(new GUIContent("Frequency Damping", "If on, the low-pass filter frequency will be smoother while interpolating between acceleration and the deceleration frequencies"), null, "On", "Off", engineAudio.useLowPassDamping, Settings, "Switch Low-Pass Damping");
			
			if (engineAudio.useLowPassDamping != newUseLowPassDamping)
			{
				engineAudio.useLowPassDamping = newUseLowPassDamping;
				engineChanged = true;
			}

			EditorGUI.BeginDisabledGroup(!engineAudio.useLowPassDamping);

			if (newUseAccelLowPass)
			{
				EditorGUI.indentLevel++;

				float newLowPassDamping = math.max(ToolkitEditorUtility.NumberField(new GUIContent("Damping", "The low-pass filter frequency smoothness damping, the higher the value, the faster the frequency changes"), engineAudio.lowPassDamping, false, Settings, "Change Damping"), 1f);

				if (engineAudio.lowPassDamping != newLowPassDamping)
				{
					engineAudio.lowPassDamping = newLowPassDamping;
					engineChanged = true;
				}

				EditorGUI.indentLevel--;
			}

			EditorGUI.indentLevel--;

			EditorGUI.EndDisabledGroup();
			EditorGUI.EndDisabledGroup();

			if (engineChanged)
			{
				engine.Audio = engineAudio;

				EditorUtility.SetDirty(Settings);
				editor.Repaint();
			}
		}

		#endregion

		#endregion

		#region Global Methods

		private void OnGUI()
		{
			scrollView = EditorGUILayout.BeginScrollView(scrollView);

			EditorGUILayout.Space();

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

			#endregion

			VehicleEngine engine = Settings.Engines[engineIndex];

			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.BeginVertical(GUILayout.Width(position.width - 25f));

			float orgLabelWidth = EditorGUIUtility.labelWidth;

			EditorGUIUtility.labelWidth = math.max(200f, orgLabelWidth);

			EditorGUI.BeginDisabledGroup(true);
			EditorGUILayout.TextField(new GUIContent("Name", "The engine name"), engine.Name);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
			EngineEditorGUI(ref engine, engine.Name);
			EditorGUILayout.Space();

			EditorGUIUtility.labelWidth = orgLabelWidth;

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();
		}

		#endregion

		#endregion
	}
}

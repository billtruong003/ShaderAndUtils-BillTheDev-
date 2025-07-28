#region Namespaces

using UnityEngine;
using UnityEditor;
using Utilities;
using MVC.Base;
using MVC.Editor;
using MVC.Utilities.Editor;

#endregion

namespace UnityEditorInternal
{
	[CustomEditor(typeof(AudioReverbZone))]
	public class AudioReverbZoneEditorOverride : ToolkitBehaviourEditor
	{
		#region Variables

		private AudioReverbZone Instance
		{
			get
			{
				if (!instance)
					instance = target as AudioReverbZone;

				return instance;
			}
		}
		private VehicleAudioZone AudioZone
		{
			get
			{
				if (!audioZone)
					audioZone = Instance.GetComponent<VehicleAudioZone>();

				return audioZone;
			}
		}
		private AudioReverbZone instance;
		private VehicleAudioZone audioZone;

		#endregion

		#region Methods

		public override void OnInspectorGUI()
		{
			EditorGUILayout.Space();

			if (AudioZone && AudioZone.zoneType == VehicleAudioZone.AudioZoneType.Reverb)
			{
				EditorGUILayout.HelpBox("The Min/Max Distance of this instance of the Audio Reverb Zone is controlled by the Audio Zone instance of this GameObject.", MessageType.Info);
				EditorGUILayout.Space();
			}

			EditorGUI.BeginDisabledGroup(AudioZone && AudioZone.zoneType == VehicleAudioZone.AudioZoneType.Reverb);

			float newMinDistance = ToolkitEditorUtility.NumberField(new GUIContent("Min Distance", "The distance from the center point that the reverb will have full effect at"), Instance.minDistance, Utility.Units.Distance, false, Instance, "Inspector");

			if (Instance.minDistance != newMinDistance)
				Instance.minDistance = newMinDistance;

			float newMaxDistance = ToolkitEditorUtility.NumberField(new GUIContent("Max Distance", "The distance from the center point that the reverb will not have any effect"), Instance.maxDistance, Utility.Units.Distance, false, Instance, "Inspector");

			if (Instance.maxDistance != newMaxDistance)
				Instance.maxDistance = newMaxDistance;

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			AudioReverbPreset newReverbPreset = (AudioReverbPreset)EditorGUILayout.EnumPopup(new GUIContent("Reverb Preset", "The reverb preset"), Instance.reverbPreset);

			if (Instance.reverbPreset != newReverbPreset)
			{
				Undo.RegisterCompleteObjectUndo(Instance, "Inspector");

				Instance.reverbPreset = newReverbPreset;

				EditorUtility.SetDirty(Instance);
			}

			EditorGUI.BeginDisabledGroup(Instance.reverbPreset != AudioReverbPreset.User);

			int newRoom = ToolkitEditorUtility.Slider(new GUIContent("Room", "Room effect level (at mid frequencies)"), Instance.room, -10000, 0, Instance, "Inspector");

			if (Instance.room != newRoom)
				Instance.room = newRoom;

			int newRoomHF = ToolkitEditorUtility.Slider(new GUIContent("Room HF", "Relative room effect level at high frequencies"), Instance.roomHF, -10000, 0, Instance, "Inspector");

			if (Instance.roomHF != newRoomHF)
				Instance.roomHF = newRoomHF;

			int newRoomLF = ToolkitEditorUtility.Slider(new GUIContent("Room LF", "Relative room effect level at low frequencies"), Instance.roomLF, -10000, 0, Instance, "Inspector");

			if (Instance.roomLF != newRoomLF)
				Instance.roomLF = newRoomLF;

			float newDecayTime = ToolkitEditorUtility.Slider(new GUIContent("Decay Time", "Reverberation decay time at mid frequencies"), Instance.decayTime, .1f, 20f, Instance, "Inspector");

			if (Instance.decayTime != newDecayTime)
				Instance.decayTime = newDecayTime;

			float newDecayHFRatio = ToolkitEditorUtility.Slider(new GUIContent("Decay HF Ratio", "High-frequency to mid-frequency decay time ratio"), Instance.decayHFRatio, .1f, 2f, Instance, "Inspector");

			if (Instance.decayHFRatio != newDecayHFRatio)
				Instance.decayHFRatio = newDecayHFRatio;

			int newReflections = ToolkitEditorUtility.Slider(new GUIContent("Reflections", "Early reflections level relative to room effect"), Instance.reflections, -10000, 1000, Instance, "Inspector");

			if (Instance.reflections != newReflections)
				Instance.reflections = newReflections;

			float newReflectionsDelay = ToolkitEditorUtility.Slider(new GUIContent("Reflections Delay", "Initial reflection delay time"), Instance.reflectionsDelay, 0f, .3f, Instance, "Inspector");

			if (Instance.reflectionsDelay != newReflectionsDelay)
				Instance.reflectionsDelay = newReflectionsDelay;

			int newReverb = ToolkitEditorUtility.Slider(new GUIContent("Reverb", "Late reverberation level relative to room effect"), Instance.reverb, -10000, 2000, Instance, "Inspector");

			if (Instance.reverb != newReverb)
				Instance.reverb = newReverb;

			float newReverbDelay = ToolkitEditorUtility.Slider(new GUIContent("Reverb Delay", "Late reverberation delay time relative to initial reflection"), Instance.reverbDelay, 0f, .1f, Instance, "Inspector");

			if (Instance.reverbDelay != newReverbDelay)
				Instance.reverbDelay = newReverbDelay;

			float newHFReference = ToolkitEditorUtility.Slider(new GUIContent("HF Reference", "Reference high frequency (Hz)"), Instance.HFReference, 1000f, 20000f, Instance, "Inspector");

			if (Instance.HFReference != newHFReference)
				Instance.HFReference = newHFReference;

			float newLFReference = ToolkitEditorUtility.Slider(new GUIContent("LF Reference", "Reference high frequency (Hz)"), Instance.LFReference, 20f, 1000f, Instance, "Inspector");

			if (Instance.LFReference != newLFReference)
				Instance.LFReference = newLFReference;

			float newDiffusion = ToolkitEditorUtility.Slider(new GUIContent("Diffusion", "Value that controls the echo density in the late reverberation decay"), Instance.diffusion, 0f, 100f, Instance, "Inspector");

			if (Instance.diffusion != newDiffusion)
				Instance.diffusion = newDiffusion;

			float newDensity = ToolkitEditorUtility.Slider(new GUIContent("Density", "Value that controls the modal density in the late reverberation decay"), Instance.density, 0f, 100f, Instance, "Inspector");

			if (Instance.density != newDensity)
				Instance.density = newDensity;

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();
		}

		#endregion
	}
}

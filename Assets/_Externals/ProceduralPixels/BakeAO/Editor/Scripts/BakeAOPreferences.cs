/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProceduralPixels.BakeAO.Editor
{
    [FilePath("BakeAOPreferences.asset", FilePathAttribute.Location.PreferencesFolder)]
    internal class BakeAOPreferences : ScriptableSingleton<BakeAOPreferences>
    {
        public enum BakingPriority
        {
            VeryLow = 0,
            Low = 1,
            Medium = 2,
            High = 3,
            VeryHigh = 4
        }

        [SerializeField] internal BakingPriority bakingPriority = BakingPriority.Medium;
        [SerializeField] internal int maxMemoryUsageInMB = 512;

        public float GetBakingFrameTime()
        {
            switch (bakingPriority)
            {
                case BakingPriority.VeryLow:
                    return 0.0001f;
                case BakingPriority.Low:
                    return 0.001f;
                case BakingPriority.Medium:
                    return 0.005f;
                case BakingPriority.High:
                    return 0.016f;
                case BakingPriority.VeryHigh:
                    return 0.1f;
                default:
                    throw new InvalidEnumArgumentException("Unsupported enum value");
            }
        }

        void OnDisable()
        {
            Save();
        }

        public void Save()
        {
            Save(true);
        }

        internal SerializedObject GetSerializedObject()
        {
            return new SerializedObject(this);
        }
    }

    class BakeAOPreferencesProvider : SettingsProvider
    {
        SerializedObject serializedObject;
        SerializedProperty bakingPriorityProperty;
        SerializedProperty maxMemoryUsageProperty;

        private class Styles
        {
            public static readonly GUIContent Priority = EditorGUIUtility.TrTextContent("Baking Priority", "Use low priority if you want to keep the editor performance smooth. Highest priority provides the fastest baking speed, but will make unity editor laggy during the baking process.");
            public static readonly GUIContent MaximumMemoryUsageWhenBaking = EditorGUIUtility.TrTextContent("Max memory usage in MB", "Maximum memory usage during the baking process. When more than one texture is queued for baking, Bake AO will try to use available memory before saving texture assets. Keep this value low if you want to limit the memory usage and quickly save baked textures into assets.");
        }

        public BakeAOPreferencesProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            BakeAOPreferences.instance.Save();
            serializedObject = BakeAOPreferences.instance.GetSerializedObject();
            bakingPriorityProperty = serializedObject.FindProperty("bakingPriority");
            maxMemoryUsageProperty = serializedObject.FindProperty("maxMemoryUsageInMB");
        }

        public override void OnGUI(string searchContext)
        {
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            {
                EditorGUILayout.LabelField("Baking settings", EditorStyles.boldLabel);
                bakingPriorityProperty.enumValueIndex = (int)(BakeAOPreferences.BakingPriority)EditorGUILayout.EnumPopup(Styles.Priority, (BakeAOPreferences.BakingPriority)bakingPriorityProperty.enumValueIndex);
                maxMemoryUsageProperty.intValue = EditorGUILayout.IntField(Styles.MaximumMemoryUsageWhenBaking, maxMemoryUsageProperty.intValue);
                maxMemoryUsageProperty.intValue = Mathf.Clamp(maxMemoryUsageProperty.intValue, 0, 8096);
                if (BakingManager.instance.EstimatedReservedMemory > (long)maxMemoryUsageProperty.intValue * 1024L * 1024L)
                    EditorGUILayout.HelpBox("Bake AO does not guarantee that the memory limit will not be exceeded. Baking higher resolutions will result in higher memory usage.", MessageType.Warning, true);
                EditorGUILayout.LabelField($"Current memory used for baking: {BakingManager.instance.EstimatedReservedMemory / (1024L * 1024L):#.00} MB");
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                serializedObject.Update();
                BakeAOPreferences.instance.Save();
            }
        }

        [SettingsProvider]
        public static SettingsProvider CreateBakeAOSettingProvider()
        {
            var provider = new BakeAOPreferencesProvider("Preferences/Bake AO", SettingsScope.User, GetSearchKeywordsFromGUIContentProperties<Styles>());
            return provider;
        }
    }
}
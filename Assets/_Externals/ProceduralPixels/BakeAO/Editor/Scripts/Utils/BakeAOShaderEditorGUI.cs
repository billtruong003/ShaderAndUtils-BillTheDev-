/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace ProceduralPixels.BakeAO.Editor
{
    public class BakeAOShaderEditorGUI
    {

        // Copied from Unity.RenderPipelines.Universal.Editor.BaseShaderGUI because I don't want to include in my code dependencies from URP directly.
        /// <summary>
        /// Searches and tries to find a property in an array of properties.
        /// </summary>
        /// <param name="propertyName">The property to find.</param>
        /// <param name="properties">Array of properties to search in.</param>
        /// <returns>A MaterialProperty instance for the property.</returns>
        public static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties)
        {
            return FindProperty(propertyName, properties, true);
        }

        // Copied from Unity.RenderPipelines.Universal.Editor.BaseShaderGUI because I don't want to include in my code dependencies from URP directly.
        /// <summary>
        /// Searches and tries to find a property in an array of properties.
        /// </summary>
        /// <param name="propertyName">The property to find.</param>
        /// <param name="properties">Array of properties to search in.</param>
        /// <param name="propertyIsMandatory">Should throw exception if property is not found</param>
        /// <returns>A MaterialProperty instance for the property.</returns>
        /// <exception cref="ArgumentException"></exception>
        public static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties, bool propertyIsMandatory)
        {
            for (int index = 0; index < properties.Length; ++index)
            {
                if (properties[index] != null && properties[index].name == propertyName)
                    return properties[index];
            }
            if (propertyIsMandatory)
                throw new ArgumentException("Could not find MaterialProperty: '" + propertyName + "', Num properties: " + (object)properties.Length);
            return null;
        }

        /// <summary>
        /// The text and tooltip for the surface inputs GUI.
        /// </summary>
        public static readonly GUIContent BakeAOHeader = EditorGUIUtility.TrTextContent("Bake AO",
            "These fields control the properties of Ambient Occlusion look.");

        public struct BakeAOProperties
        {
            // Surface Input Props
            public MaterialProperty applyAOToDiffuse;
            public MaterialProperty occlusionUVSet;
            public MaterialProperty occlusionStrength;
            public MaterialProperty occlusionMap;

            public static GUIContent applyAOToDiffuseText = EditorGUIUtility.TrTextContent("Apply AO To Diffuse",
                "When enabled, the occlusion is also applied to the albedo texture, making the effect stronger.");

            public static GUIContent occlusionUVSetText = EditorGUIUtility.TrTextContent("Occlusion UV",
                "Defines which UV set is used to sample occlusion texture.");

            public static GUIContent occlusionStrengthText = EditorGUIUtility.TrTextContent("Occlusion Strength",
                "Defines how strong is occlusion effect.");

            public static GUIContent occlusionMapText = EditorGUIUtility.TrTextContent("Occlusion Map",
                "Defines the texture that will be used as an occlusion map");

            public BakeAOProperties(MaterialProperty[] properties)
            {
                applyAOToDiffuse = FindProperty("_MultiplyAlbedoAndOcclusion", properties, false);
                occlusionMap = FindProperty("_OcclusionMap", properties, false);
                occlusionUVSet = FindProperty("_AOTextureUV", properties, false);
                occlusionStrength = FindProperty("_OcclusionStrength", properties, false);
            }

            [Flags]
            public enum DrawOptions
            {
                OcclusionMap = 1 << 0,
                OcclusionStrength = 1 << 1,
                OcclusionUV = 1 << 2,
                OcclusionToDiffuse = 1 << 3,
                All = -1
            }

            public void Draw(Material material, MaterialEditor materialEditor, DrawOptions drawOptions)
            {
                // AO Texture
                if (drawOptions.HasFlag(DrawOptions.OcclusionMap))
                {
                    EditorGUI.showMixedValue = occlusionMap.hasMixedValue;
                    materialEditor.TexturePropertySingleLine(occlusionMapText, occlusionMap);
                }

                // Strength
                if (drawOptions.HasFlag(DrawOptions.OcclusionStrength))
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUI.showMixedValue = occlusionStrength.hasMixedValue;
                    float strength = occlusionStrength.floatValue;
                    strength = EditorGUILayout.Slider(occlusionStrengthText, strength, 0.0f, 2.0f);

                    if (EditorGUI.EndChangeCheck())
                        occlusionStrength.floatValue = strength;
                }

                // UV set
                if (drawOptions.HasFlag(DrawOptions.OcclusionUV))
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUI.showMixedValue = occlusionUVSet.hasMixedValue;
                    UVChannel occlusionUVSetValue = (UVChannel)Mathf.RoundToInt(occlusionUVSet.floatValue);
                    occlusionUVSetValue = (UVChannel)EditorGUILayout.EnumPopup(occlusionUVSetText, occlusionUVSetValue);

                    if (EditorGUI.EndChangeCheck())
                        occlusionUVSet.floatValue = (int)occlusionUVSetValue;
                }

                // Apply to diffuse
                if (drawOptions.HasFlag(DrawOptions.OcclusionToDiffuse))
                {
                    EditorGUI.BeginChangeCheck();

                    EditorGUI.showMixedValue = applyAOToDiffuse.hasMixedValue;
                    float applyAOToDiffuseValue = applyAOToDiffuse.floatValue;
                    bool enabled = EditorGUILayout.Toggle(applyAOToDiffuseText, applyAOToDiffuseValue > 0.5f);

                    if (EditorGUI.EndChangeCheck())
                        applyAOToDiffuse.floatValue = enabled ? 1.0f : 0.0f;
                }

                EditorGUI.showMixedValue = false;
            }
        }
        // BakeAO add end
    } 
}

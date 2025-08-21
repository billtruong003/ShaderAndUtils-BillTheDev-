using UnityEngine;
using UnityEditor;
using System;

namespace BillTheDev.Editor
{
    public class ToonVATInstancedShaderGUI : ShaderGUI
    {
        private MaterialProperty positionTexture;
        private MaterialProperty positionMin;
        private MaterialProperty positionMax;

        private MaterialProperty mainTex;
        private MaterialProperty highlightColor;
        private MaterialProperty baseColor;
        private MaterialProperty midtoneColor;
        private MaterialProperty shadowColor;

        private MaterialProperty highlightThreshold;
        private MaterialProperty midtoneThreshold;
        private MaterialProperty shadowThreshold;
        private MaterialProperty smoothness;

        private MaterialProperty fakeLightDirection;
        private MaterialProperty lightIntensity;

        private void FindProperties(MaterialProperty[] properties)
        {
            positionTexture = FindProperty("_PositionTexture", properties);
            positionMin = FindProperty("_PositionMin", properties);
            positionMax = FindProperty("_PositionMax", properties);

            mainTex = FindProperty("_MainTex", properties);
            highlightColor = FindProperty("_HighlightColor", properties);
            baseColor = FindProperty("_BaseColor", properties);
            midtoneColor = FindProperty("_MidtoneColor", properties);
            shadowColor = FindProperty("_ShadowColor", properties);

            highlightThreshold = FindProperty("_HighlightThreshold", properties);
            midtoneThreshold = FindProperty("_MidtoneThreshold", properties);
            shadowThreshold = FindProperty("_ShadowThreshold", properties);
            smoothness = FindProperty("_Smoothness", properties);

            fakeLightDirection = FindProperty("_FakeLightDirection", properties);
            lightIntensity = FindProperty("_LightIntensity", properties);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);

            DrawVATSettings(materialEditor);
            DrawToonShadingSettings(materialEditor);
            DrawLightingSettings(materialEditor);
        }

        private void DrawVATSettings(MaterialEditor editor)
        {
            EditorGUILayout.LabelField("VAT Settings", EditorStyles.boldLabel);
            editor.TexturePropertySingleLine(new GUIContent(positionTexture.displayName, "Vertex Animation Texture (Position)"), positionTexture);
            editor.ShaderProperty(positionMin, positionMin.displayName);
            editor.ShaderProperty(positionMax, positionMax.displayName);
            EditorGUILayout.Space();
        }

        private void DrawToonShadingSettings(MaterialEditor editor)
        {
            EditorGUILayout.LabelField("Toon Shading", EditorStyles.boldLabel);
            editor.TexturePropertySingleLine(new GUIContent(mainTex.displayName, "Albedo Texture (RGB)"), mainTex);

            EditorGUILayout.Space();

            editor.ShaderProperty(highlightColor, highlightColor.displayName);
            editor.ShaderProperty(baseColor, baseColor.displayName);
            editor.ShaderProperty(midtoneColor, midtoneColor.displayName);
            editor.ShaderProperty(shadowColor, shadowColor.displayName);

            EditorGUILayout.Space();

            editor.ShaderProperty(highlightThreshold, highlightThreshold.displayName);
            editor.ShaderProperty(midtoneThreshold, midtoneThreshold.displayName);
            editor.ShaderProperty(shadowThreshold, shadowThreshold.displayName);
            editor.ShaderProperty(smoothness, smoothness.displayName);
            EditorGUILayout.Space();
        }

        private void DrawLightingSettings(MaterialEditor editor)
        {
            EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
            editor.ShaderProperty(fakeLightDirection, new GUIContent(fakeLightDirection.displayName, "The direction of the artificial light source."));
            editor.ShaderProperty(lightIntensity, lightIntensity.displayName);
        }
    }
}
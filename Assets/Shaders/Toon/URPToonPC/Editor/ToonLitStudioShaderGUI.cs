using UnityEngine;
using UnityEditor;
using System;

public class ToonLitStudioShaderGUI : ShaderGUI
{
    private MaterialEditor editor;
    private MaterialProperty[] properties;

    private bool showSurfaceProps = true;
    private bool showMainRamp = true;
    private bool showLighting = true;
    private bool showArtistic = true;
    private bool showSurfaceEffects = true;

    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;

    private enum MatcapBlendMode { Add, Multiply, Lerp }

    private void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                richText = true
            };
        }
        if (sectionStyle == null)
        {
            sectionStyle = new GUIStyle("ShurikenModuleTitle")
            {
                font = new GUIStyle(EditorStyles.label).font,
                border = new RectOffset(15, 7, 4, 4),
                fixedHeight = 22,
                contentOffset = new Vector2(20f, -2f)
            };
        }
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        this.editor = materialEditor;
        this.properties = properties;
        InitializeStyles();

        DrawHeader("TOON LIT SHADER - STUDIO ADVANCED");

        DrawSurfacePropertiesSection();
        DrawMainRampSection();
        DrawLightingSection();
        DrawArtisticSection();
        DrawSurfaceEffectsSection();
    }

    private void DrawHeader(string text)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"<color=#33c8ff>{text}</color>", headerStyle, GUILayout.Height(22));
        EditorGUILayout.Space();
    }

    private void DrawSectionToggle(string label, ref bool toggle)
    {
        var rect = GUILayoutUtility.GetRect(16f, 22f, sectionStyle);
        GUI.Box(rect, label, sectionStyle);
        var e = Event.current;
        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            toggle = !toggle;
            e.Use();
        }
    }

    private MaterialProperty FindProp(string name) => FindProperty(name, properties);

    private void DrawSurfacePropertiesSection()
    {
        DrawSectionToggle("Surface Properties", ref showSurfaceProps);
        if (showSurfaceProps)
        {
            EditorGUILayout.Space();
            editor.TexturePropertySingleLine(new GUIContent("Base Map (Albedo)"), FindProp("_BaseMap"));

            editor.ShaderProperty(FindProp("_EnableNormalMap"), "Enable Normal Map");
            if (FindProp("_EnableNormalMap").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.TexturePropertySingleLine(new GUIContent("Normal Map"), FindProp("_BumpMap"));
                editor.ShaderProperty(FindProp("_BumpScale"), "Normal Intensity");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableAlphaClip"), "Enable Alpha Clipping");
            if (FindProp("_EnableAlphaClip").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_Cutoff"), "Alpha Cutoff");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }
    }

    private void DrawMainRampSection()
    {
        DrawSectionToggle("Main Shading Ramp", ref showMainRamp);
        if (showMainRamp)
        {
            EditorGUILayout.Space();
            editor.ColorProperty(FindProp("_HighlightColor"), "Highlight Color");
            editor.ColorProperty(FindProp("_MidtoneColor"), "Midtone Color");
            editor.ColorProperty(FindProp("_ShadowColor"), "Shadow Color");
            EditorGUILayout.Space();

            MaterialProperty shadowProp = FindProp("_ShadowThreshold");
            MaterialProperty highlightProp = FindProp("_HighlightThreshold");

            float oldShadowVal = shadowProp.floatValue;
            float oldHighlightVal = highlightProp.floatValue;

            editor.ShaderProperty(shadowProp, "Shadow Threshold");
            editor.ShaderProperty(highlightProp, "Highlight Threshold");

            if (shadowProp.floatValue != oldShadowVal && shadowProp.floatValue > highlightProp.floatValue)
            {
                highlightProp.floatValue = shadowProp.floatValue;
            }
            if (highlightProp.floatValue != oldHighlightVal && highlightProp.floatValue < shadowProp.floatValue)
            {
                shadowProp.floatValue = highlightProp.floatValue;
            }

            editor.ShaderProperty(FindProp("_RampSmoothness"), "Ramp Smoothness");
            EditorGUILayout.Space();
        }
    }

    private void DrawLightingSection()
    {
        DrawSectionToggle("Lighting Control", ref showLighting);
        if (showLighting)
        {
            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_UseFakeLight"), "Use Fake Light Direction");
            if (FindProp("_UseFakeLight").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_FakeLightDirection"), "Fake Direction");
                EditorGUI.indentLevel--;
            }
            editor.ShaderProperty(FindProp("_CustomShadowColor"), new GUIContent("Main Light Shadow Color"));
            editor.ShaderProperty(FindProp("_ShadowTintInfluence"), new GUIContent("Light Color On Shadow", "Hòa trộn màu của bóng với màu của ánh sáng."));

            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableGradientAmbient"), "Enable Gradient Ambient");
            if (FindProp("_EnableGradientAmbient").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_SkyColor"), "Sky Color");
                editor.ShaderProperty(FindProp("_GroundColor"), "Ground Color");
                editor.ShaderProperty(FindProp("_AmbientGradientPower"), "Gradient Power");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableAdditionalLights"), "Enable Additional Lights");
            if (FindProp("_EnableAdditionalLights").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_AdditionalLightInfluence"), "Influence");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }
    }

    private void DrawArtisticSection()
    {
        DrawSectionToggle("Artistic Effects", ref showArtistic);
        if (showArtistic)
        {
            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableHatching"), "Enable Shadow Hatching");
            if (FindProp("_EnableHatching").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.TexturePropertySingleLine(new GUIContent("Hatching Map"), FindProp("_HatchingMap"));
                editor.ShaderProperty(FindProp("_HatchingTiling"), "Tiling");
                editor.ShaderProperty(FindProp("_HatchingVisibility"), "Visibility");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableMatcap"), "Enable MatCap");
            if (FindProp("_EnableMatcap").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.TexturePropertySingleLine(new GUIContent("MatCap Map"), FindProp("_MatcapMap"));

                var blendModeProp = FindProp("_MatcapBlendMode");
                var newMode = (MatcapBlendMode)EditorGUILayout.EnumPopup("Blend Mode", (MatcapBlendMode)blendModeProp.floatValue);
                if ((float)newMode != blendModeProp.floatValue)
                {
                    blendModeProp.floatValue = (float)newMode;
                }

                editor.ShaderProperty(FindProp("_MatcapTint"), new GUIContent("Tint & Lerp Alpha"));
                editor.ShaderProperty(FindProp("_MatcapIntensity"), "Intensity");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }
    }

    private void DrawSurfaceEffectsSection()
    {
        DrawSectionToggle("Surface Effects", ref showSurfaceEffects);
        if (showSurfaceEffects)
        {
            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableSpecular"), "Enable Specular Reflection");
            if (FindProp("_EnableSpecular").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_SpecularColor"), "Color");
                editor.ShaderProperty(FindProp("_SpecularThreshold"), "Threshold");
                editor.ShaderProperty(FindProp("_SpecularSmoothness"), "Smoothness");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableRimLight"), "Enable Rim Light");
            if (FindProp("_EnableRimLight").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_RimColor"), new GUIContent("Color & Intensity(A)"));
                editor.ShaderProperty(FindProp("_RimPower"), "Power");
                editor.ShaderProperty(FindProp("_RimThreshold"), "Threshold");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }
    }
}
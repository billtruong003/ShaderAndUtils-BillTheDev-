using UnityEditor;
using UnityEngine;
using System;

public class BouncyToonWaterMobileGUI : ShaderGUI
{
    private MaterialEditor editor;
    private MaterialProperty[] properties;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] materialProperties)
    {
        this.editor = materialEditor;
        this.properties = materialProperties;

        DrawMainFeatureToggles();
        DrawSurfaceOptions();
        DrawLightingSections();
        DrawEffectSections();
    }

    private void DrawMainFeatureToggles()
    {
        GUILayout.Label("Master Feature Toggles", EditorStyles.boldLabel);
        DrawProperty("_EnableBouncy");
        DrawProperty("_EnableSpecular");
        DrawProperty("_EnableRim");
        DrawProperty("_EnableSmoothEdges");
        EditorGUILayout.Space(20);
    }

    private void DrawSurfaceOptions()
    {
        GUILayout.Label("Surface Options", EditorStyles.boldLabel);
        DrawProperty("_BaseMap");
        DrawProperty("_BaseColor");
        DrawProperty("_Alpha");
        EditorGUILayout.Space();
    }

    private void DrawLightingSections()
    {
        GUILayout.Label("Lighting", EditorStyles.boldLabel);

        // CORRECTED: Use EditorGUI.indentLevel instead of editor.indentLevel
        EditorGUI.indentLevel++;

        DrawProperty("_ToonRampThreshold");
        if (IsKeywordEnabled("_SMOOTH_EDGES_ON")) DrawProperty("_ToonRampSmoothness");
        EditorGUILayout.Space();

        if (IsKeywordEnabled("_SPECULAR_ON"))
        {
            GUILayout.Label("Specular", EditorStyles.miniBoldLabel);
            DrawProperty("_SpecularColor");
            DrawProperty("_SpecularThreshold");
            if (IsKeywordEnabled("_SMOOTH_EDGES_ON")) DrawProperty("_SpecularSmoothness");
            EditorGUILayout.Space();
        }

        if (IsKeywordEnabled("_RIM_ON"))
        {
            GUILayout.Label("Rim Light", EditorStyles.miniBoldLabel);
            DrawProperty("_RimColor");
            DrawProperty("_RimThreshold");
            if (IsKeywordEnabled("_SMOOTH_EDGES_ON")) DrawProperty("_RimSmoothness");
        }

        // CORRECTED: Use EditorGUI.indentLevel instead of editor.indentLevel
        EditorGUI.indentLevel--;
    }

    private void DrawEffectSections()
    {
        if (IsKeywordEnabled("_BOUNCY_ON"))
        {
            EditorGUILayout.Space();
            GUILayout.Label("Bouncy Effect", EditorStyles.boldLabel);

            // CORRECTED: Use EditorGUI.indentLevel instead of editor.indentLevel
            EditorGUI.indentLevel++;

            EditorGUILayout.HelpBox("This effect uses a Vertex Color mask. Paint fixed areas black and moving areas white in the chosen channel.", MessageType.Info);

            DrawProperty("_MaskChannel");
            DrawProperty("_WaveAmplitude");
            DrawProperty("_WaveFrequency");
            DrawProperty("_WaveSpeed");
            DrawProperty("_WaveAxis");

            // CORRECTED: Use EditorGUI.indentLevel instead of editor.indentLevel
            EditorGUI.indentLevel--;
        }
    }

    private MaterialProperty FindProperty(string name) => FindProperty(name, properties);

    private void DrawProperty(string name)
    {
        MaterialProperty prop = FindProperty(name);
        if (prop != null) editor.ShaderProperty(prop, prop.displayName);
    }

    private bool IsKeywordEnabled(string keyword)
    {
        string propName = "";
        if (keyword == "_BOUNCY_ON") propName = "_EnableBouncy";
        else if (keyword == "_SPECULAR_ON") propName = "_EnableSpecular";
        else if (keyword == "_RIM_ON") propName = "_EnableRim";
        else if (keyword == "_SMOOTH_EDGES_ON") propName = "_EnableSmoothEdges";

        MaterialProperty prop = FindProperty(propName);
        return prop != null && prop.floatValue == 1;
    }
}

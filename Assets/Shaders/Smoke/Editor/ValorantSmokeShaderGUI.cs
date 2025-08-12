using UnityEngine;
using UnityEditor;
using System;

public class ValorantSmokeShaderGUI : ShaderGUI
{
    private MaterialEditor editor;
    private MaterialProperty[] properties;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        editor = materialEditor;
        properties = props;

        DrawHeader("Shape & Density");
        DrawProperty("_ShellThickness");
        DrawProperty("_DensityMultiplier");
        DrawProperty("_RaymarchSteps");

        DrawHeader("Noise & Animation");
        DrawProperty("_NoiseTexture");
        DrawProperty("_NoiseScale");
        DrawProperty("_NoiseScrollSpeed");

        DrawHeader("Warp Effect (Distortion)");
        MaterialProperty enableWarp = FindProperty("_EnableWarp", properties);
        DrawProperty(enableWarp);
        if (enableWarp.floatValue > 0.5f)
        {
            DrawProperty("_WarpTexture");
            DrawProperty("_WarpScale");
            DrawProperty("_WarpStrength");
        }

        DrawHeader("Color & Lighting");
        EditorGUILayout.HelpBox("Use gradients (1D Textures) for color ramps. Black/White textures work as a default.", MessageType.Info);
        DrawProperty("_LitColorRamp");
        DrawProperty("_ShadowColorRamp");
        DrawProperty("_LightAbsorption");
        DrawProperty("_RimColor");
        DrawProperty("_RimPower");

        DrawHeader("Edge & Intersection");
        DrawProperty("_EdgeColor");
        DrawProperty("_EdgeHardness");
        DrawProperty("_EdgeSoftness");
        DrawProperty("_DepthFadeDistance");
    }

    private void DrawHeader(string text)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }

    private void DrawProperty(string propertyName)
    {
        MaterialProperty prop = FindProperty(propertyName, properties);
        editor.ShaderProperty(prop, prop.displayName);
    }

    private void DrawProperty(MaterialProperty prop)
    {
        editor.ShaderProperty(prop, prop.displayName);
    }
}
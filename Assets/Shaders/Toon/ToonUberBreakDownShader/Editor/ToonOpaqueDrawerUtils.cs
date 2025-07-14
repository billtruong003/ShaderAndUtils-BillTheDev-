using UnityEngine;
using UnityEditor;
using System;

public static class ToonOpaqueDrawerUtils
{
    public enum SurfaceType { Opaque, Metallic, Foliage }

    private static bool showToonSettings = true;
    private static bool showMetallicSettings = true;
    private static bool showFoliageSettings = true;

    private static void DrawFoldout(string title, ref bool state, Action contents)
    {
        state = EditorGUILayout.BeginFoldoutHeaderGroup(state, title);
        if (state)
        {
            EditorGUILayout.BeginVertical("box");
            contents.Invoke();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(2);
    }

    public static void DrawToonSettings(MaterialEditor editor, MaterialProperty offset, MaterialProperty smoothness, MaterialProperty tint)
    {
        DrawFoldout("Toon Shading", ref showToonSettings, () =>
        {
            editor.ShaderProperty(offset, "Ramp Offset");
            editor.ShaderProperty(smoothness, "Ramp Smoothness");
            editor.ShaderProperty(tint, "Shadow Tint");
        });
    }

    public static void DrawMetallicSettings(MaterialEditor editor, MaterialProperty ramp, MaterialProperty brightness, MaterialProperty offset, MaterialProperty specColor, MaterialProperty hiOffset, MaterialProperty hiColor, MaterialProperty rimColor, MaterialProperty rimPower)
    {
        DrawFoldout("Stylized Metal", ref showMetallicSettings, () =>
        {
            editor.TexturePropertySingleLine(new GUIContent("Ramp Texture"), ramp);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Specular", EditorStyles.boldLabel);
            editor.ShaderProperty(brightness, "Brightness");
            editor.ShaderProperty(offset, "Size");
            editor.ShaderProperty(specColor, "Color");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Highlight", EditorStyles.boldLabel);
            editor.ShaderProperty(hiOffset, "Size");
            editor.ShaderProperty(hiColor, "Color");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rim Light", EditorStyles.boldLabel);
            editor.ShaderProperty(rimColor, "Color");
            editor.ShaderProperty(rimPower, "Power");
        });
    }

    public static void DrawFoliageSettings(MaterialEditor editor, MaterialProperty windFreq, MaterialProperty windAmp, MaterialProperty windDir, MaterialProperty transColor, MaterialProperty transStrength)
    {
        DrawFoldout("Foliage", ref showFoliageSettings, () =>
        {
            EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
            editor.ShaderProperty(windFreq, "Frequency");
            editor.ShaderProperty(windAmp, "Amplitude");
            editor.ShaderProperty(windDir, "Direction");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
            editor.ShaderProperty(transColor, "Translucency Color");
            editor.ShaderProperty(transStrength, "Translucency Strength");
        });
    }
}
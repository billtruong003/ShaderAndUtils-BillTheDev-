using UnityEngine;
using UnityEditor;
using System;

public class ToonAuraShaderGUI : ShaderGUI
{
    private MaterialEditor materialEditor;
    private MaterialProperty[] properties;
    private Material material;

    private static bool showAuraSettings = true;
    private static bool showBaseSettings = true;
    private static bool showLightingSettings = true;
    private static bool showToonSettings = true;
    private static bool showMetallicSettings = true;
    private static bool showFoliageSettings = true;
    private static bool showAdvancedSettings = false;

    // Property declarations
    private MaterialProperty auraToggle, surfaceType;
    private MaterialProperty auraInnerColor, auraRimColor, auraWidth, auraZOffset, auraNoiseTex, auraNoiseScale, auraSpeedX, auraSpeedY, auraNoiseOpacity, auraBrightness, auraRimEdge, auraRimPower;
    private MaterialProperty auraFadeStart, auraFadeEnd;
    private MaterialProperty baseMap, baseColor, alphaClipMode, cutoff, emissionMode, emissionColor, emissionMap;
    private MaterialProperty fakeLightMode, fakeLightColor, fakeLightDirection;
    private MaterialProperty toonRampOffset, toonRampSmoothness, shadowTint, ambientColor;
    private MaterialProperty ramp, brightness, offset, specuColor, highlightOffset, hiColor, rimColor, rimPower;
    private MaterialProperty windFrequency, windAmplitude, windDirection, translucencyColor, translucencyStrength;

    private void FindProperties(MaterialProperty[] props)
    {
        auraToggle = FindProperty("_AuraToggle", props);
        surfaceType = FindProperty("_SurfaceType", props);

        auraInnerColor = FindProperty("_AuraInnerColor", props);
        auraRimColor = FindProperty("_AuraRimColor", props);
        auraWidth = FindProperty("_AuraWidth", props);
        auraZOffset = FindProperty("_AuraZOffset", props);
        auraNoiseTex = FindProperty("_AuraNoiseTex", props);
        auraNoiseScale = FindProperty("_AuraNoiseScale", props);
        auraSpeedX = FindProperty("_AuraSpeedX", props);
        auraSpeedY = FindProperty("_AuraSpeedY", props);
        auraNoiseOpacity = FindProperty("_AuraNoiseOpacity", props);
        auraBrightness = FindProperty("_AuraBrightness", props);
        auraRimEdge = FindProperty("_AuraRimEdge", props);
        auraRimPower = FindProperty("_AuraRimPower", props);
        auraFadeStart = FindProperty("_AuraFadeStart", props);
        auraFadeEnd = FindProperty("_AuraFadeEnd", props);

        baseMap = FindProperty("_BaseMap", props);
        baseColor = FindProperty("_BaseColor", props);
        alphaClipMode = FindProperty("_AlphaClipMode", props);
        cutoff = FindProperty("_Cutoff", props);
        emissionMode = FindProperty("_EmissionMode", props);
        emissionColor = FindProperty("_EmissionColor", props);
        emissionMap = FindProperty("_EmissionMap", props);

        fakeLightMode = FindProperty("_FakeLightMode", props);
        fakeLightColor = FindProperty("_FakeLightColor", props);
        fakeLightDirection = FindProperty("_FakeLightDirection", props);

        toonRampOffset = FindProperty("_ToonRampOffset", props);
        toonRampSmoothness = FindProperty("_ToonRampSmoothness", props);
        shadowTint = FindProperty("_ShadowTint", props);
        ambientColor = FindProperty("_AmbientColor", props);

        ramp = FindProperty("_Ramp", props);
        brightness = FindProperty("_Brightness", props);
        offset = FindProperty("_Offset", props);
        specuColor = FindProperty("_SpecuColor", props);
        highlightOffset = FindProperty("_HighlightOffset", props);
        hiColor = FindProperty("_HiColor", props);
        rimColor = FindProperty("_RimColor", props);
        rimPower = FindProperty("_RimPower", props);

        windFrequency = FindProperty("_WindFrequency", props);
        windAmplitude = FindProperty("_WindAmplitude", props);
        windDirection = FindProperty("_WindDirection", props);
        translucencyColor = FindProperty("_TranslucencyColor", props);
        translucencyStrength = FindProperty("_TranslucencyStrength", props);
    }

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
    {
        materialEditor = editor;
        properties = props;
        material = materialEditor.target as Material;

        FindProperties(props);

        EditorGUI.BeginChangeCheck();

        DrawWorkflowSettings();
        DrawModuleToggles();

        if (auraToggle.floatValue > 0) DrawAuraSettings();
        DrawBaseSettings();
        DrawLightingSettings();

        var currentSurfaceType = (SurfaceType)surfaceType.floatValue;
        switch (currentSurfaceType)
        {
            case SurfaceType.Toon:
                DrawToonSettings();
                break;
            case SurfaceType.Metallic:
                DrawMetallicSettings();
                break;
            case SurfaceType.Foliage:
                DrawFoliageSettings();
                break;
        }

        DrawAdvancedSettings();

        if (EditorGUI.EndChangeCheck())
        {
            SetKeywords();
        }
    }

    private void SetKeywords()
    {
        SetKeyword("_AURA_ON", auraToggle.floatValue > 0);
        SetKeyword("_ALPHACLIP_ON", alphaClipMode.floatValue > 0);
        SetKeyword("_EMISSION_ON", emissionMode.floatValue > 0);
        SetKeyword("_FAKELIGHT_ON", fakeLightMode.floatValue > 0);

        var currentSurfaceType = (SurfaceType)surfaceType.floatValue;
        SetKeyword("_SURFACETYPE_TOON", currentSurfaceType == SurfaceType.Toon);
        SetKeyword("_SURFACETYPE_METALLIC", currentSurfaceType == SurfaceType.Metallic);
        SetKeyword("_SURFACETYPE_FOLIAGE", currentSurfaceType == SurfaceType.Foliage);
    }

    private void DrawWorkflowSettings()
    {
        EditorGUILayout.LabelField("Toon Aura Uber Shader", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select a rendering style (Surface Type) and toggle additional effects like Aura.", MessageType.Info);
        materialEditor.ShaderProperty(surfaceType, "Surface Type");
        EditorGUILayout.Space();
    }

    private void DrawModuleToggles()
    {
        EditorGUILayout.BeginHorizontal();
        materialEditor.ShaderProperty(auraToggle, new GUIContent("Aura", "Enable or disable the aura effect pass."));
        materialEditor.ShaderProperty(alphaClipMode, new GUIContent("Alpha Clip", "Enable or disable alpha clipping."));
        materialEditor.ShaderProperty(emissionMode, new GUIContent("Emission", "Enable or disable emission."));
        materialEditor.ShaderProperty(fakeLightMode, new GUIContent("Fake Light", "Use a fallback light if no main light is in the scene."));
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space();
    }

    private void DrawAuraSettings()
    {
        DrawFoldout("Aura Effect", ref showAuraSettings, () =>
        {
            materialEditor.ShaderProperty(auraInnerColor, "Inner Color");
            materialEditor.ShaderProperty(auraRimColor, "Rim Color");
            materialEditor.ShaderProperty(auraWidth, "Width");
            materialEditor.ShaderProperty(auraRimEdge, "Rim Edge");
            materialEditor.ShaderProperty(auraRimPower, "Rim Power");
            materialEditor.ShaderProperty(auraBrightness, "Brightness");
            materialEditor.ShaderProperty(auraZOffset, "Z Offset");
            EditorGUILayout.Space();
            materialEditor.TexturePropertySingleLine(new GUIContent("Noise Texture"), auraNoiseTex);
            materialEditor.ShaderProperty(auraNoiseScale, "Noise Scale (X, Y)");
            materialEditor.ShaderProperty(auraNoiseOpacity, "Noise Opacity");
            materialEditor.ShaderProperty(auraSpeedX, "Speed X");
            materialEditor.ShaderProperty(auraSpeedY, "Speed Y");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Distance Fade", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(auraFadeStart, "Fade Start");
            materialEditor.ShaderProperty(auraFadeEnd, "Fade End");
        });
    }

    private void DrawBaseSettings()
    {
        DrawFoldout("Base Properties", ref showBaseSettings, () =>
        {
            materialEditor.TexturePropertySingleLine(new GUIContent("Albedo"), baseMap, baseColor);
            if (alphaClipMode.floatValue > 0)
            {
                materialEditor.ShaderProperty(cutoff, "Alpha Cutoff");
            }
            if (emissionMode.floatValue > 0)
            {
                materialEditor.ColorProperty(emissionColor, "Emission Color");
                materialEditor.TexturePropertySingleLine(new GUIContent("Emission Map"), emissionMap);
            }
        });
    }

    private void DrawLightingSettings()
    {
        if (fakeLightMode.floatValue > 0)
        {
            DrawFoldout("Fallback Lighting", ref showLightingSettings, () =>
            {
                materialEditor.ShaderProperty(fakeLightColor, "Fake Light Color");
                materialEditor.ShaderProperty(fakeLightDirection, "Fake Light Direction");
            });
        }
    }

    private void DrawToonSettings()
    {
        DrawFoldout("Toon Shading", ref showToonSettings, () =>
        {
            materialEditor.ShaderProperty(toonRampOffset, "Ramp Offset");
            materialEditor.ShaderProperty(toonRampSmoothness, "Ramp Smoothness");
            materialEditor.ShaderProperty(shadowTint, "Shadow Tint");
            materialEditor.ColorProperty(ambientColor, "Ambient Color");
            EditorGUILayout.HelpBox("Use the Alpha channel to blend between Scene Ambient (A=0) and this custom color (A=1).", MessageType.Info);
        });
    }

    private void DrawMetallicSettings()
    {
        DrawFoldout("Stylized Metal", ref showMetallicSettings, () =>
        {
            materialEditor.TexturePropertySingleLine(new GUIContent("Ramp Texture"), ramp);
            materialEditor.ShaderProperty(brightness, "Brightness");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Specular", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(offset, "Size");
            materialEditor.ShaderProperty(specuColor, "Color");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Highlight", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(highlightOffset, "Size");
            materialEditor.ShaderProperty(hiColor, "Color");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Rim Light", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(rimColor, "Color");
            materialEditor.ShaderProperty(rimPower, "Power");
        });
    }

    private void DrawFoliageSettings()
    {
        DrawFoldout("Foliage", ref showFoliageSettings, () =>
        {
            EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(windFrequency, "Frequency");
            materialEditor.ShaderProperty(windAmplitude, "Amplitude");
            materialEditor.ShaderProperty(windDirection, "Direction");
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(translucencyColor, "Translucency Color");
            materialEditor.ShaderProperty(translucencyStrength, "Translucency Strength");
        });
    }

    private void DrawAdvancedSettings()
    {
        DrawFoldout("Advanced Options", ref showAdvancedSettings, () =>
        {
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        });
    }

    private void DrawFoldout(string title, ref bool state, Action contents)
    {
        var style = new GUIStyle("ShurikenModuleTitle");
        style.font = new GUIStyle(EditorStyles.label).font;
        style.border = new RectOffset(15, 7, 4, 4);
        style.fixedHeight = 22;
        style.contentOffset = new Vector2(20f, -2f);

        var rect = GUILayoutUtility.GetRect(16f, 22f, style);
        GUI.Box(rect, title, style);

        var e = Event.current;
        if (e.type == EventType.Repaint)
        {
            var toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);
            EditorStyles.foldout.Draw(toggleRect, false, false, state, false);
        }

        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            state = !state;
            e.Use();
        }

        if (state)
        {
            EditorGUILayout.BeginVertical("box");
            contents.Invoke();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(2);
    }

    private void SetKeyword(string keyword, bool state)
    {
        if (state) material.EnableKeyword(keyword);
        else material.DisableKeyword(keyword);
    }

    private enum SurfaceType { Toon, Metallic, Foliage }
}
using UnityEngine;
using UnityEditor;
using System;

public class Shmackle_UberDissolve_GUI : ShaderGUI
{
    private MaterialEditor materialEditor;
    private Material material;
    private MaterialProperty[] properties;

    private static class FoldoutStates
    {
        public static bool workflow = true;
        public static bool baseProps = true;
        public static bool emission = true;
        public static bool dissolveControl = true;
        public static bool dissolveEdge = true;
        public static bool dissolveVertexFx = false;
        public static bool lighting = true;
        public static bool advanced = false;
    }

    private enum LightingModel { Unlit = 0, StandardLit = 1, BasicToon = 2, StudioToon = 3, ToonBling = 4 }
    private enum DissolveType { Noise = 0, Linear = 1, Radial = 2, Pattern = 3, AlphaBlend = 4, Shatter = 5 }

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
    {
        materialEditor = editor;
        properties = props;
        material = materialEditor.target as Material;

        FindAllProperties();

        EditorGUI.BeginChangeCheck();
        DrawLayout();
        if (EditorGUI.EndChangeCheck())
        {
            ApplyAllKeywords();
        }
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);
        if (material != null)
        {
            ApplyAllKeywords();
        }
    }

    private void DrawLayout()
    {
        DrawHeader("Shmackle Uber Dissolve FX");
        DrawFoldout("Workflow", ref FoldoutStates.workflow, DrawWorkflowSettings);
        DrawFoldout("Base Properties", ref FoldoutStates.baseProps, DrawBaseSettings);

        if (enableEmission.floatValue > 0.5f || lightingModel.floatValue > 0)
        {
            DrawFoldout("Emission", ref FoldoutStates.emission, DrawEmissionSettings);
        }

        bool isDissolveEnabled = enableDissolve.floatValue > 0.5f;
        if (isDissolveEnabled)
        {
            DrawFoldout("Dissolve Control", ref FoldoutStates.dissolveControl, DrawDissolveControlSettings);
            DrawFoldout("Dissolve Edge", ref FoldoutStates.dissolveEdge, DrawDissolveEdgeSettings);
            DrawFoldout("Dissolve Vertex Effects", ref FoldoutStates.dissolveVertexFx, DrawDissolveVertexFxSettings);
        }

        if (lightingModel.floatValue > 0)
        {
            DrawFoldout("Lighting Model", ref FoldoutStates.lighting, DrawLightingSettings);
        }

        DrawFoldout("Advanced Rendering", ref FoldoutStates.advanced, DrawAdvancedSettings);
    }

    #region Find Properties
    private MaterialProperty lightingModel, baseMap, baseColor, cullMode, alphaClipMode, cutoff;
    private MaterialProperty enableEmission, emissionMap, emissionColor;
    private MaterialProperty enableDissolve, dissolveType, dissolveThreshold, useTimeAnimation, timeScale, useLocalSpace, dissolveDirection, radialDirection;
    private MaterialProperty noiseTex, noiseScale, noiseStrength, dissolveEdgeWidth, dissolveEdgeColor;
    private MaterialProperty patternType, patternFrequency, alphaFadeRange;
    private MaterialProperty enableVertexDisplacement, enableShatterEffect, vertexDisplacement, displacementWaveWidth;
    private MaterialProperty shatterStrength, shatterLiftSpeed, shatterOffsetStrength, shatterTriggerRange;
    private MaterialProperty toonRampOffset, toonRampSmoothness, shadowTint;
    private MaterialProperty studioEnableGradientAmbient, studioHighlightColor, studioMidtoneColor, studioShadowColor;
    private MaterialProperty studioHighlightThreshold, studioShadowThreshold, studioRampSmoothness;
    private MaterialProperty studioSkyColor, studioGroundColor, studioAmbientGradientPower;
    private MaterialProperty studioEnableSpecular, studioSpecularColor, studioSpecularThreshold, studioSpecularSmoothness;
    private MaterialProperty studioEnableRim, studioRimColor, studioRimPower, studioRimThreshold;
    private MaterialProperty blingSpecColor, blingSpecSmoothness, blingSpecOffset, blingRimColor, blingRimPower, blingRimMin, blingRimMax;
    private MaterialProperty enableBlingEffect, blingWorldSpace, blingColor, blingIntensity, blingScale, blingSpeed, blingFresnelPower, blingThreshold;
    private MaterialProperty zWrite;

    private void FindAllProperties()
    {
        lightingModel = FindProperty("_LightingModel", properties);
        baseMap = FindProperty("_BaseMap", properties);
        baseColor = FindProperty("_BaseColor", properties);
        cullMode = FindProperty("_CullMode", properties);
        alphaClipMode = FindProperty("_AlphaClipMode", properties);
        cutoff = FindProperty("_Cutoff", properties);
        enableEmission = FindProperty("_EnableEmission", properties);
        emissionMap = FindProperty("_EmissionMap", properties);
        emissionColor = FindProperty("_EmissionColor", properties);
        enableDissolve = FindProperty("_EnableDissolve", properties);
        dissolveType = FindProperty("_DissolveType", properties);
        dissolveThreshold = FindProperty("_DissolveThreshold", properties);
        radialDirection = FindProperty("_RadialDirection", properties);
        useTimeAnimation = FindProperty("_UseTimeAnimation", properties);
        timeScale = FindProperty("_TimeScale", properties);
        useLocalSpace = FindProperty("_UseLocalSpace", properties);
        dissolveDirection = FindProperty("_DissolveDirection", properties);
        noiseTex = FindProperty("_NoiseTex", properties);
        noiseScale = FindProperty("_NoiseScale", properties);
        noiseStrength = FindProperty("_NoiseStrength", properties);
        dissolveEdgeWidth = FindProperty("_DissolveEdgeWidth", properties);
        dissolveEdgeColor = FindProperty("_DissolveEdgeColor", properties);
        patternType = FindProperty("_PatternType", properties);
        patternFrequency = FindProperty("_PatternFrequency", properties);
        alphaFadeRange = FindProperty("_AlphaFadeRange", properties);
        enableVertexDisplacement = FindProperty("_EnableVertexDisplacement", properties);
        enableShatterEffect = FindProperty("_EnableShatterEffect", properties);
        vertexDisplacement = FindProperty("_VertexDisplacement", properties);
        displacementWaveWidth = FindProperty("_DisplacementWaveWidth", properties);
        shatterStrength = FindProperty("_ShatterStrength", properties);
        shatterLiftSpeed = FindProperty("_ShatterLiftSpeed", properties);
        shatterOffsetStrength = FindProperty("_ShatterOffsetStrength", properties);
        shatterTriggerRange = FindProperty("_ShatterTriggerRange", properties);
        toonRampOffset = FindProperty("_ToonRampOffset", properties);
        toonRampSmoothness = FindProperty("_ToonRampSmoothness", properties);
        shadowTint = FindProperty("_ShadowTint", properties);
        studioEnableGradientAmbient = FindProperty("_EnableGradientAmbient", properties);
        studioHighlightColor = FindProperty("_StudioToon_HighlightColor", properties);
        studioMidtoneColor = FindProperty("_StudioToon_MidtoneColor", properties);
        studioShadowColor = FindProperty("_StudioToon_ShadowColor", properties);
        studioHighlightThreshold = FindProperty("_StudioToon_HighlightThreshold", properties);
        studioShadowThreshold = FindProperty("_StudioToon_ShadowThreshold", properties);
        studioRampSmoothness = FindProperty("_StudioToon_RampSmoothness", properties);
        studioSkyColor = FindProperty("_StudioToon_SkyColor", properties);
        studioGroundColor = FindProperty("_StudioToon_GroundColor", properties);
        studioAmbientGradientPower = FindProperty("_StudioToon_AmbientGradientPower", properties);
        studioEnableSpecular = FindProperty("_EnableSpecular", properties);
        studioSpecularColor = FindProperty("_StudioToon_SpecularColor", properties);
        studioSpecularThreshold = FindProperty("_StudioToon_SpecularThreshold", properties);
        studioSpecularSmoothness = FindProperty("_StudioToon_SpecularSmoothness", properties);
        studioEnableRim = FindProperty("_EnableRimLight", properties);
        studioRimColor = FindProperty("_StudioToon_RimColor", properties);
        studioRimPower = FindProperty("_StudioToon_RimPower", properties);
        studioRimThreshold = FindProperty("_StudioToon_RimThreshold", properties);
        blingSpecColor = FindProperty("_Bling_SpecColor", properties);
        blingSpecSmoothness = FindProperty("_Bling_SpecSmoothness", properties);
        blingSpecOffset = FindProperty("_Bling_SpecOffset", properties);
        blingRimColor = FindProperty("_Bling_RimColor", properties);
        blingRimPower = FindProperty("_Bling_RimPower", properties);
        blingRimMin = FindProperty("_Bling_RimMin", properties);
        blingRimMax = FindProperty("_Bling_RimMax", properties);
        enableBlingEffect = FindProperty("_EnableBlingEffect", properties);
        blingWorldSpace = FindProperty("_BlingWorldSpace", properties);
        blingColor = FindProperty("_BlingColor", properties);
        blingIntensity = FindProperty("_BlingIntensity", properties);
        blingScale = FindProperty("_BlingScale", properties);
        blingSpeed = FindProperty("_BlingSpeed", properties);
        blingFresnelPower = FindProperty("_BlingFresnelPower", properties);
        blingThreshold = FindProperty("_BlingThreshold", properties);
        zWrite = FindProperty("_ZWrite", properties);
    }
    #endregion

    #region GUI Drawers
    private void DrawWorkflowSettings()
    {
        materialEditor.ShaderProperty(lightingModel, "Model");
        materialEditor.ShaderProperty(enableDissolve, "Enable Dissolve Effect");
    }

    private void DrawBaseSettings()
    {
        materialEditor.TexturePropertySingleLine(new GUIContent(baseMap.displayName), baseMap, baseColor);
        materialEditor.ShaderProperty(cullMode, cullMode.displayName);
        materialEditor.ShaderProperty(alphaClipMode, alphaClipMode.displayName);
        if (alphaClipMode.floatValue > 0.5f)
        {
            DrawProperty(cutoff.displayName, cutoff, 1);
        }
    }

    private void DrawEmissionSettings()
    {
        materialEditor.ShaderProperty(enableEmission, enableEmission.displayName);
        if (enableEmission.floatValue > 0.5f)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(emissionMap.displayName), emissionMap, emissionColor);
        }
    }

    private void DrawDissolveControlSettings()
    {
        materialEditor.ShaderProperty(dissolveType, "Type");
        materialEditor.ShaderProperty(dissolveThreshold, "Threshold");

        var currentDissolveType = (DissolveType)dissolveType.floatValue;
        if (currentDissolveType == DissolveType.Radial)
        {
            DrawProperty(radialDirection.displayName, radialDirection);
        }

        materialEditor.ShaderProperty(useTimeAnimation, "Animate Threshold");
        if (useTimeAnimation.floatValue > 0.5f)
        {
            DrawProperty("Animation Speed", timeScale);
        }

        materialEditor.ShaderProperty(useLocalSpace, "Use Local Space Coords");
        if (currentDissolveType == DissolveType.Linear || currentDissolveType == DissolveType.Shatter)
        {
            DrawProperty(dissolveDirection.displayName, dissolveDirection);
        }
        if (currentDissolveType == DissolveType.Pattern)
        {
            DrawProperty("Pattern", patternType);
            DrawProperty("Frequency", patternFrequency);
        }
        if (currentDissolveType == DissolveType.AlphaBlend)
        {
            DrawProperty("Fade Range", alphaFadeRange);
        }
    }

    private void DrawDissolveEdgeSettings()
    {
        materialEditor.TexturePropertySingleLine(new GUIContent(noiseTex.displayName), noiseTex);
        DrawProperty(noiseScale.displayName, noiseScale);
        DrawProperty(noiseStrength.displayName, noiseStrength);
        DrawProperty(dissolveEdgeWidth.displayName, dissolveEdgeWidth);
        DrawProperty(dissolveEdgeColor.displayName, dissolveEdgeColor);
    }

    private void DrawDissolveVertexFxSettings()
    {
        bool isShatter = (DissolveType)dissolveType.floatValue == DissolveType.Shatter;

        materialEditor.ShaderProperty(enableVertexDisplacement, enableVertexDisplacement.displayName);
        materialEditor.ShaderProperty(enableShatterEffect, enableShatterEffect.displayName);

        if (enableVertexDisplacement.floatValue > 0.5f && !isShatter)
        {
            DrawHeader("Standard Displacement", 12);
            DrawProperty("Intensity", vertexDisplacement, 1);
            DrawProperty(displacementWaveWidth.displayName, displacementWaveWidth, 1);
        }

        if (enableShatterEffect.floatValue > 0.5f)
        {
            DrawHeader("Shatter Properties", 12);
            DrawProperty("Outward Push", vertexDisplacement, 1);
            DrawProperty(shatterStrength.displayName, shatterStrength, 1);
            DrawProperty(shatterLiftSpeed.displayName, shatterLiftSpeed, 1);
            DrawProperty(shatterOffsetStrength.displayName, shatterOffsetStrength, 1);
            DrawProperty(shatterTriggerRange.displayName, shatterTriggerRange, 1);
        }
    }

    private void DrawLightingSettings()
    {
        switch ((LightingModel)lightingModel.floatValue)
        {
            case LightingModel.BasicToon:
                DrawHeader("Basic Toon Settings", 12);
                DrawProperty(toonRampOffset.displayName, toonRampOffset);
                DrawProperty(toonRampSmoothness.displayName, toonRampSmoothness);
                DrawProperty(shadowTint.displayName, shadowTint);
                break;
            case LightingModel.StudioToon:
                DrawStudioToonLightingSettings();
                break;
            case LightingModel.ToonBling:
                DrawToonBlingSettings();
                break;
        }
    }

    private void DrawStudioToonLightingSettings()
    {
        DrawHeader("Studio Toon - Main Ramp", 12);
        DrawProperty("Highlight Color", studioHighlightColor);
        DrawProperty("Midtone Color", studioMidtoneColor);
        DrawProperty("Shadow Color", studioShadowColor);
        EditorGUILayout.Space();

        float shadowVal = studioShadowThreshold.floatValue;
        float highlightVal = studioHighlightThreshold.floatValue;
        var label = new GUIContent("Ramp Thresholds", "Left: Shadow | Right: Highlight");

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.MinMaxSlider(label, ref shadowVal, ref highlightVal, 0.0f, 1.0f);
        if (EditorGUI.EndChangeCheck())
        {
            studioShadowThreshold.floatValue = shadowVal;
            studioHighlightThreshold.floatValue = highlightVal;
        }

        DrawProperty("Ramp Smoothness", studioRampSmoothness);

        DrawHeader("Studio Toon - Ambient", 12);
        DrawProperty(studioEnableGradientAmbient.displayName, studioEnableGradientAmbient);
        if (studioEnableGradientAmbient.floatValue > 0.5f)
        {
            DrawProperty("Sky Color", studioSkyColor, 1);
            DrawProperty("Ground Color", studioGroundColor, 1);
            DrawProperty("Gradient Power", studioAmbientGradientPower, 1);
        }

        DrawHeader("Studio Toon - Effects", 12);
        DrawProperty(studioEnableSpecular.displayName, studioEnableSpecular);
        if (studioEnableSpecular.floatValue > 0.5f)
        {
            DrawProperty("Color", studioSpecularColor, 1);
            DrawProperty("Threshold", studioSpecularThreshold, 1);
            DrawProperty("Smoothness", studioSpecularSmoothness, 1);
        }
        EditorGUILayout.Space();
        DrawProperty(studioEnableRim.displayName, studioEnableRim);
        if (studioEnableRim.floatValue > 0.5f)
        {
            DrawProperty("Color & Intensity (A)", studioRimColor, 1);
            DrawProperty("Power", studioRimPower, 1);
            DrawProperty("Threshold", studioRimThreshold, 1);
        }
    }

    private void DrawToonBlingSettings()
    {
        DrawHeader("Toon Bling - Main Lighting", 12);
        DrawProperty(shadowTint.displayName, shadowTint);
        DrawProperty(toonRampOffset.displayName, toonRampOffset);
        DrawProperty(toonRampSmoothness.displayName, toonRampSmoothness);

        DrawHeader("Toon Bling - Specular", 12);
        DrawProperty(blingSpecColor.displayName, blingSpecColor);
        DrawProperty(blingSpecOffset.displayName, blingSpecOffset);
        DrawProperty(blingSpecSmoothness.displayName, blingSpecSmoothness);

        DrawHeader("Toon Bling - Rim Light", 12);
        DrawProperty(blingRimColor.displayName, blingRimColor);
        DrawProperty(blingRimPower.displayName, blingRimPower);
        DrawProperty(blingRimMin.displayName, blingRimMin);
        DrawProperty(blingRimMax.displayName, blingRimMax);

        DrawHeader("Toon Bling - Sparkle Effect", 12);
        DrawProperty(enableBlingEffect.displayName, enableBlingEffect);
        if (enableBlingEffect.floatValue > 0.5f)
        {
            DrawProperty(blingWorldSpace.displayName, blingWorldSpace, 1);
            DrawProperty(blingColor.displayName, blingColor, 1);
            DrawProperty(blingIntensity.displayName, blingIntensity, 1);
            DrawProperty(blingScale.displayName, blingScale, 1);
            DrawProperty(blingSpeed.displayName, blingSpeed, 1);
            DrawProperty(blingFresnelPower.displayName, blingFresnelPower, 1);
            DrawProperty(blingThreshold.displayName, blingThreshold, 1);
        }
    }

    private void DrawAdvancedSettings()
    {
        DrawProperty(zWrite.displayName, zWrite);
        materialEditor.EnableInstancingField();
        materialEditor.RenderQueueField();
    }
    #endregion

    #region Keywords
    private void ApplyAllKeywords()
    {
        if (material == null) return;

        var currentLighting = (LightingModel)lightingModel.floatValue;
        SetKeyword("_LIGHTINGMODEL_UNLIT", currentLighting == LightingModel.Unlit);
        SetKeyword("_LIGHTINGMODEL_STANDARD_LIT", currentLighting == LightingModel.StandardLit);
        SetKeyword("_LIGHTINGMODEL_BASIC_TOON", currentLighting == LightingModel.BasicToon);
        SetKeyword("_LIGHTINGMODEL_STUDIO_TOON", currentLighting == LightingModel.StudioToon);
        SetKeyword("_LIGHTINGMODEL_TOON_BLING", currentLighting == LightingModel.ToonBling);

        bool dissolveOn = enableDissolve.floatValue > 0.5f;
        SetKeyword("_DISSOLVE_ON", dissolveOn);
        if (dissolveOn)
        {
            var currentDissolve = (DissolveType)dissolveType.floatValue;
            SetKeyword("_DISSOLVETYPE_NOISE", currentDissolve == DissolveType.Noise);
            SetKeyword("_DISSOLVETYPE_LINEAR", currentDissolve == DissolveType.Linear);
            SetKeyword("_DISSOLVETYPE_RADIAL", currentDissolve == DissolveType.Radial);
            SetKeyword("_DISSOLVETYPE_PATTERN", currentDissolve == DissolveType.Pattern);
            SetKeyword("_DISSOLVETYPE_ALPHA_BLEND", currentDissolve == DissolveType.AlphaBlend);
            SetKeyword("_DISSOLVETYPE_SHATTER", currentDissolve == DissolveType.Shatter);
            SetKeyword("_DISSOLVE_LOCALSPACE_ON", useLocalSpace.floatValue > 0.5f);
        }

        bool displacementOn = dissolveOn && enableVertexDisplacement.floatValue > 0.5f && (DissolveType)dissolveType.floatValue != DissolveType.Shatter;
        SetKeyword("_VERTEX_DISPLACEMENT_ON", displacementOn);
        SetKeyword("_SHATTER_EFFECT_ON", dissolveOn && enableShatterEffect.floatValue > 0.5f);

        SetKeyword("_BLING_EFFECT_ON", currentLighting == LightingModel.ToonBling && enableBlingEffect.floatValue > 0.5f);
        SetKeyword("_BLING_WORLDSPACE_ON", currentLighting == LightingModel.ToonBling && enableBlingEffect.floatValue > 0.5f && blingWorldSpace.floatValue > 0.5f);

        SetKeyword("_STUDIO_GRADIENT_AMBIENT_ON", currentLighting == LightingModel.StudioToon && studioEnableGradientAmbient.floatValue > 0.5f);
        SetKeyword("_STUDIO_SPECULAR_ON", currentLighting == LightingModel.StudioToon && studioEnableSpecular.floatValue > 0.5f);
        SetKeyword("_STUDIO_RIM_LIGHT_ON", currentLighting == LightingModel.StudioToon && studioEnableRim.floatValue > 0.5f);

        SetKeyword("_ALPHACLIP_ON", alphaClipMode.floatValue > 0.5f);
        SetKeyword("_EMISSION_ON", enableEmission.floatValue > 0.5f);

        bool isTransparent = alphaClipMode.floatValue > 0.5f || (dissolveOn && (DissolveType)dissolveType.floatValue == DissolveType.AlphaBlend);
        material.SetOverrideTag("RenderType", isTransparent ? "TransparentCutout" : "Opaque");
        material.SetInt("_ZWrite", zWrite.floatValue > 0.5f ? 1 : 0);

        EditorUtility.SetDirty(material);
    }

    private void SetKeyword(string keyword, bool state)
    {
        if (state) material.EnableKeyword(keyword);
        else material.DisableKeyword(keyword);
    }
    #endregion

    #region Helpers
    private void DrawHeader(string text, int fontSize = 14)
    {
        var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = fontSize };
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(text, style);
        EditorGUILayout.Space(2);
    }

    private void DrawFoldout(string title, ref bool state, Action contents)
    {
        var style = new GUIStyle("ShurikenModuleTitle")
        {
            font = new GUIStyle(EditorStyles.label).font,
            border = new RectOffset(15, 7, 4, 4),
            fixedHeight = 22,
            contentOffset = new Vector2(20f, -2f)
        };

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
            EditorGUILayout.Space(2);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            contents.Invoke();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(2);
    }

    private void DrawProperty(string label, MaterialProperty prop, int indent = 0)
    {
        EditorGUI.indentLevel += indent;
        materialEditor.ShaderProperty(prop, label);
        EditorGUI.indentLevel -= indent;
    }
    #endregion
}
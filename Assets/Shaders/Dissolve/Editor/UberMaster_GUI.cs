using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class UberMaster_GUI : ShaderGUI
{
    private MaterialEditor materialEditor;
    private Material material;
    private MaterialProperty[] properties;

    private static class Foldouts
    {
        public static bool workflow = true;
        public static bool baseProps = true;
        public static bool textureSwap = true;
        public static bool dissolveControl = true;
        public static bool dissolveEdge = true;
        public static bool dissolveVertexFx = false;
        public static bool lighting = true;
        public static bool advanced = false;
    }

    private enum LightingModel { Unlit, StandardLit, BasicToon, StylizedMetal, ToonBling }
    private enum DissolveType { Noise, Linear, Radial, Pattern, AlphaBlend, Shatter }
    private enum SwapShape { Sphere, Box, MaskTexture }
    private enum SwapType { Noise, Linear, Radial, Pattern }
    private enum PatternType { SinCos, Checker, Grid }

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

    private void DrawLayout()
    {
        DrawHeader("Uber Master Shader");

        DrawFoldout("Workflow & Main Features", ref Foldouts.workflow, DrawWorkflowSettings);
        DrawFoldout("Base Properties", ref Foldouts.baseProps, DrawBaseSettings);

        if (enableTextureSwap.floatValue > 0.5f)
        {
            DrawFoldout("Texture Swap Effect", ref Foldouts.textureSwap, DrawTextureSwapSettings);
        }

        if (enableDissolve.floatValue > 0.5f)
        {
            DrawFoldout("Dissolve Control", ref Foldouts.dissolveControl, DrawDissolveControlSettings);
            DrawFoldout("Dissolve Edge", ref Foldouts.dissolveEdge, DrawDissolveEdgeSettings);
            DrawFoldout("Dissolve Vertex Effects", ref Foldouts.dissolveVertexFx, DrawDissolveVertexFxSettings);
        }

        DrawFoldout("Lighting Models", ref Foldouts.lighting, DrawLightingSettings);
        DrawFoldout("Advanced Rendering", ref Foldouts.advanced, DrawAdvancedSettings);
    }

    private void DrawWorkflowSettings()
    {
        materialEditor.ShaderProperty(lightingModel, "Lighting Model");
        materialEditor.ShaderProperty(enableTextureSwap, "Enable Texture Swap");
        materialEditor.ShaderProperty(enableDissolve, "Enable Dissolve");
    }

    private void DrawBaseSettings()
    {
        materialEditor.TexturePropertySingleLine(new GUIContent(baseMap.displayName), baseMap, baseColor);
        materialEditor.ShaderProperty(cullMode, cullMode.displayName);
        materialEditor.ShaderProperty(alphaClipMode, alphaClipMode.displayName);
        if (alphaClipMode.floatValue > 0.5f)
        {
            materialEditor.ShaderProperty(cutoff, cutoff.displayName);
        }
    }

    private void DrawTextureSwapSettings()
    {
        materialEditor.TexturePropertySingleLine(new GUIContent(secondaryAlbedoMap.displayName), secondaryAlbedoMap);
        EditorGUILayout.Space();

        materialEditor.ShaderProperty(swapProgress, "Transition Progress");
        materialEditor.ShaderProperty(swapTransitionHardness, swapTransitionHardness.displayName);
        EditorGUILayout.Space();

        DrawSubHeader("Transition Shape & Pattern");
        materialEditor.ShaderProperty(useSwapLocalSpace, useSwapLocalSpace.displayName);
        if (useSwapLocalSpace.floatValue < 0.5f)
        {
            EditorGUILayout.HelpBox("World Space mode: Effect Position is set globally via script (e.g. Interactor.cs targeting '_SwapWorldPosition').", MessageType.Info);
        }
        else
        {
            materialEditor.ShaderProperty(swapEffectCenter, swapEffectCenter.displayName);
        }

        materialEditor.ShaderProperty(swapShape, swapShape.displayName);
        var currentShape = (SwapShape)swapShape.floatValue;
        if (currentShape == SwapShape.Sphere)
        {
            materialEditor.ShaderProperty(swapEffectRadius, swapEffectRadius.displayName);
        }
        else if (currentShape == SwapShape.Box)
        {
            materialEditor.ShaderProperty(swapEffectExtents, swapEffectExtents.displayName);
        }
        else if (currentShape == SwapShape.MaskTexture)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(swapShapeMask.displayName), swapShapeMask);
        }

        materialEditor.ShaderProperty(swapType, swapType.displayName);
        var currentSwapType = (SwapType)swapType.floatValue;

        if (currentSwapType == SwapType.Noise)
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(swapNoiseMap.displayName), swapNoiseMap);
            materialEditor.ShaderProperty(swapNoiseScale, swapNoiseScale.displayName);
            materialEditor.ShaderProperty(swapNoiseStrength, swapNoiseStrength.displayName);
            materialEditor.ShaderProperty(swapNoiseScrollSpeed, swapNoiseScrollSpeed.displayName);
        }
        else if (currentSwapType == SwapType.Linear)
        {
            materialEditor.ShaderProperty(swapDirection, swapDirection.displayName);
        }
        else if (currentSwapType == SwapType.Pattern)
        {
            materialEditor.ShaderProperty(swapPatternType, swapPatternType.displayName);
            materialEditor.ShaderProperty(swapPatternFrequency, swapPatternFrequency.displayName);
        }

        DrawSubHeader("Transition Edge");
        materialEditor.ShaderProperty(swapLineWidth, swapLineWidth.displayName);
        materialEditor.ShaderProperty(swapLineColor, swapLineColor.displayName);
    }

    private void DrawDissolveControlSettings()
    {
        materialEditor.ShaderProperty(dissolveType, "Type");
        materialEditor.ShaderProperty(dissolveThreshold, "Threshold");
        var currentDissolveType = (DissolveType)dissolveType.floatValue;
        if (currentDissolveType == DissolveType.Radial)
        {
            materialEditor.ShaderProperty(radialDirection, radialDirection.displayName);
        }

        materialEditor.ShaderProperty(useTimeAnimation, "Animate Threshold");
        if (useTimeAnimation.floatValue > 0.5f)
        {
            materialEditor.ShaderProperty(timeScale, "Animation Speed");
        }

        materialEditor.ShaderProperty(useLocalSpace, "Use Local Space Coords");
        if (currentDissolveType == DissolveType.Linear || currentDissolveType == DissolveType.Shatter)
        {
            materialEditor.ShaderProperty(dissolveDirection, dissolveDirection.displayName);
        }

        if (currentDissolveType == DissolveType.Pattern)
        {
            materialEditor.ShaderProperty(patternType, "Pattern");
            materialEditor.ShaderProperty(patternFrequency, "Frequency");
        }

        if (currentDissolveType == DissolveType.AlphaBlend)
        {
            materialEditor.ShaderProperty(alphaFadeRange, "Fade Range");
        }
    }

    private void DrawDissolveEdgeSettings()
    {
        materialEditor.TexturePropertySingleLine(new GUIContent(dissolveNoiseTex.displayName), dissolveNoiseTex);
        materialEditor.ShaderProperty(dissolveNoiseScale, dissolveNoiseScale.displayName);
        materialEditor.ShaderProperty(dissolveNoiseStrength, dissolveNoiseStrength.displayName);
        materialEditor.ShaderProperty(dissolveEdgeWidth, dissolveEdgeWidth.displayName);
        materialEditor.ShaderProperty(dissolveEdgeColor, dissolveEdgeColor.displayName);
    }

    private void DrawDissolveVertexFxSettings()
    {
        var currentDissolveType = (DissolveType)dissolveType.floatValue;
        bool isShatter = currentDissolveType == DissolveType.Shatter;

        if (!isShatter)
        {
            materialEditor.ShaderProperty(enableVertexDisplacement, enableVertexDisplacement.displayName);
        }
        materialEditor.ShaderProperty(enableShatterEffect, enableShatterEffect.displayName);

        if (enableVertexDisplacement.floatValue > 0.5f && !isShatter)
        {
            DrawSubHeader("Standard Displacement");
            materialEditor.ShaderProperty(useSaturateDisplacement, useSaturateDisplacement.displayName);
            materialEditor.ShaderProperty(vertexDisplacement, "Intensity");
            materialEditor.ShaderProperty(bounceWaveWidth, bounceWaveWidth.displayName);
        }

        if (enableShatterEffect.floatValue > 0.5f)
        {
            DrawSubHeader("Shatter Properties");
            materialEditor.ShaderProperty(vertexDisplacement, "Outward Push");
            materialEditor.ShaderProperty(shatterStrength, shatterStrength.displayName);
            materialEditor.ShaderProperty(shatterLiftSpeed, shatterLiftSpeed.displayName);
            materialEditor.ShaderProperty(shatterOffsetStrength, shatterOffsetStrength.displayName);
            materialEditor.ShaderProperty(shatterTriggerRange, shatterTriggerRange.displayName);
        }
    }

    private void DrawLightingSettings()
    {
        materialEditor.ShaderProperty(ambientColor, ambientColor.displayName);
        var currentLighting = (LightingModel)lightingModel.floatValue;

        if (currentLighting == LightingModel.BasicToon)
        {
            DrawSubHeader("Toon Diffuse");
            materialEditor.ShaderProperty(toonRampOffset, toonRampOffset.displayName);
            materialEditor.ShaderProperty(toonRampSmoothness, toonRampSmoothness.displayName);
            materialEditor.ShaderProperty(shadowTint, shadowTint.displayName);

            DrawSubHeader("Toon Specular");
            materialEditor.ShaderProperty(toonSpecColor, toonSpecColor.displayName);
            materialEditor.ShaderProperty(toonSpecOffset, toonSpecOffset.displayName);
            materialEditor.ShaderProperty(toonSpecSmoothness, toonSpecSmoothness.displayName);

            DrawSubHeader("Toon Rim Light");
            materialEditor.ShaderProperty(toonRimColor, toonRimColor.displayName);
            materialEditor.ShaderProperty(toonRimPower, toonRimPower.displayName);
            materialEditor.ShaderProperty(toonRimMin, toonRimMin.displayName);
            materialEditor.ShaderProperty(toonRimMax, toonRimMax.displayName);
        }

        if (currentLighting == LightingModel.ToonBling)
        {
            DrawSubHeader("Toon Ramp Settings");
            materialEditor.ShaderProperty(toonRampOffset, toonRampOffset.displayName);
            materialEditor.ShaderProperty(toonRampSmoothness, toonRampSmoothness.displayName);
            materialEditor.ShaderProperty(shadowTint, shadowTint.displayName);
        }

        if (currentLighting == LightingModel.StylizedMetal)
        {
            DrawSubHeader("Stylized Metal Settings");
            materialEditor.TexturePropertySingleLine(new GUIContent(metalRamp.displayName), metalRamp);
            materialEditor.ShaderProperty(metalBrightness, metalBrightness.displayName);
            materialEditor.ShaderProperty(metalOffset, metalOffset.displayName);
            materialEditor.ShaderProperty(metalSpecuColor, metalSpecuColor.displayName);
            materialEditor.ShaderProperty(metalHighlightOffset, metalHighlightOffset.displayName);
            materialEditor.ShaderProperty(metalHiColor, metalHiColor.displayName);
            materialEditor.ShaderProperty(metalRimColor, metalRimColor.displayName);
            materialEditor.ShaderProperty(metalRimPower, metalRimPower.displayName);
        }

        if (currentLighting == LightingModel.ToonBling)
        {
            DrawSubHeader("Bling Effect Settings");
            materialEditor.ShaderProperty(blingSpecColor, blingSpecColor.displayName);
            materialEditor.ShaderProperty(blingSpecOffset, blingSpecOffset.displayName);
            materialEditor.ShaderProperty(blingSpecSmoothness, blingSpecSmoothness.displayName);
            materialEditor.ShaderProperty(blingRimColor, blingRimColor.displayName);
            materialEditor.ShaderProperty(blingRimPower, blingRimPower.displayName);
            materialEditor.ShaderProperty(blingRimMin, blingRimMin.displayName);
            materialEditor.ShaderProperty(blingRimMax, blingRimMax.displayName);

            materialEditor.ShaderProperty(enableBlingEffect, enableBlingEffect.displayName);
            if (enableBlingEffect.floatValue > 0.5f)
            {
                materialEditor.ShaderProperty(blingWorldSpace, blingWorldSpace.displayName);
                materialEditor.ShaderProperty(blingColor, blingColor.displayName);
                materialEditor.ShaderProperty(blingIntensity, blingIntensity.displayName);
                materialEditor.ShaderProperty(blingScale, blingScale.displayName);
                materialEditor.ShaderProperty(blingSpeed, blingSpeed.displayName);
                materialEditor.ShaderProperty(blingFresnelPower, blingFresnelPower.displayName);
                materialEditor.ShaderProperty(blingThreshold, blingThreshold.displayName);
            }
        }
    }

    private void DrawAdvancedSettings()
    {
        materialEditor.ShaderProperty(zWrite, zWrite.displayName);
        materialEditor.EnableInstancingField();
        materialEditor.RenderQueueField();
    }

    private void ApplyAllKeywords()
    {
        var currentLighting = (LightingModel)lightingModel.floatValue;
        SetKeyword("_LIGHTINGMODEL_UNLIT", currentLighting == LightingModel.Unlit);
        SetKeyword("_LIGHTINGMODEL_STANDARD_LIT", currentLighting == LightingModel.StandardLit);
        SetKeyword("_LIGHTINGMODEL_BASIC_TOON", currentLighting == LightingModel.BasicToon);
        SetKeyword("_LIGHTINGMODEL_STYLIZED_METAL", currentLighting == LightingModel.StylizedMetal);
        SetKeyword("_LIGHTINGMODEL_TOON_BLING", currentLighting == LightingModel.ToonBling);

        bool textureSwapOn = enableTextureSwap.floatValue > 0.5f;
        SetKeyword("_TEXTURE_SWAP_ON", textureSwapOn);
        if (textureSwapOn)
        {
            SetKeyword("_SWAP_LOCALSPACE_ON", useSwapLocalSpace.floatValue > 0.5f);
        }

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

            bool displacementOn = enableVertexDisplacement.floatValue > 0.5f && currentDissolve != DissolveType.Shatter;
            SetKeyword("_VERTEX_DISPLACEMENT_ON", displacementOn);
            SetKeyword("_DISPLACEMENT_SATURATE_ON", displacementOn && useSaturateDisplacement.floatValue > 0.5f);
            SetKeyword("_SHATTER_EFFECT_ON", enableShatterEffect.floatValue > 0.5f);
        }
        else
        {
            SetKeyword("_VERTEX_DISPLACEMENT_ON", false);
            SetKeyword("_DISPLACEMENT_SATURATE_ON", false);
            SetKeyword("_SHATTER_EFFECT_ON", false);
        }

        if (currentLighting == LightingModel.ToonBling)
        {
            SetKeyword("_BLING_EFFECT_ON", enableBlingEffect.floatValue > 0.5f);
            SetKeyword("_BLING_WORLDSPACE_ON", enableBlingEffect.floatValue > 0.5f && blingWorldSpace.floatValue > 0.5f);
        }
        else
        {
            SetKeyword("_BLING_EFFECT_ON", false);
            SetKeyword("_BLING_WORLDSPACE_ON", false);
        }

        SetKeyword("_ALPHACLIP_ON", alphaClipMode.floatValue > 0.5f);
        SetKeyword("_ZWRITE_ON", zWrite.floatValue > 0.5f);
        EditorUtility.SetDirty(material);
    }

    #region Property Finding
    private readonly Dictionary<string, MaterialProperty> propertyCache = new Dictionary<string, MaterialProperty>();
    private MaterialProperty FindProp(string name)
    {
        if (propertyCache.TryGetValue(name, out var prop)) return prop;
        prop = FindProperty(name, properties, false);
        if (prop != null) propertyCache[name] = prop;
        return prop;
    }

    private MaterialProperty lightingModel, baseMap, baseColor, ambientColor, cullMode, alphaClipMode, cutoff;
    private MaterialProperty enableTextureSwap, secondaryAlbedoMap, swapNoiseMap, swapNoiseScale, swapNoiseStrength, swapNoiseScrollSpeed, swapLineWidth, swapLineColor, swapProgress;
    private MaterialProperty useSwapLocalSpace, swapShape, swapType, swapEffectCenter, swapEffectExtents, swapEffectRadius, swapShapeMask, swapTransitionHardness;
    private MaterialProperty swapDirection, swapPatternType, swapPatternFrequency;
    private MaterialProperty enableDissolve, dissolveType, dissolveThreshold, useTimeAnimation, timeScale, useLocalSpace, dissolveDirection, radialDirection;
    private MaterialProperty dissolveNoiseTex, dissolveNoiseScale, dissolveNoiseStrength, dissolveEdgeWidth, dissolveEdgeColor;
    private MaterialProperty patternType, patternFrequency, alphaFadeRange;
    private MaterialProperty enableVertexDisplacement, useSaturateDisplacement, enableShatterEffect, vertexDisplacement, bounceWaveWidth;
    private MaterialProperty shatterStrength, shatterLiftSpeed, shatterOffsetStrength, shatterTriggerRange;
    private MaterialProperty toonRampOffset, toonRampSmoothness, shadowTint;
    private MaterialProperty toonSpecColor, toonSpecSmoothness, toonSpecOffset, toonRimColor, toonRimPower, toonRimMin, toonRimMax;
    private MaterialProperty metalRamp, metalBrightness, metalOffset, metalSpecuColor, metalHighlightOffset, metalHiColor, metalRimColor, metalRimPower;
    private MaterialProperty blingSpecColor, blingSpecSmoothness, blingSpecOffset, blingRimColor, blingRimPower, blingRimMin, blingRimMax;
    private MaterialProperty enableBlingEffect, blingWorldSpace, blingColor, blingIntensity, blingScale, blingSpeed, blingFresnelPower, blingThreshold;
    private MaterialProperty zWrite;

    private void FindAllProperties()
    {
        propertyCache.Clear();
        lightingModel = FindProp("_LightingModel"); baseMap = FindProp("_BaseMap"); baseColor = FindProp("_BaseColor");
        ambientColor = FindProp("_AmbientColor"); cullMode = FindProp("_CullMode"); alphaClipMode = FindProp("_AlphaClipMode");
        cutoff = FindProp("_Cutoff"); enableTextureSwap = FindProp("_EnableTextureSwap"); secondaryAlbedoMap = FindProp("_SecondaryAlbedoMap");
        swapNoiseMap = FindProp("_SwapNoiseMap"); swapNoiseScale = FindProp("_SwapNoiseScale"); swapNoiseStrength = FindProp("_SwapNoiseStrength");
        swapNoiseScrollSpeed = FindProp("_SwapNoiseScrollSpeed"); swapLineWidth = FindProp("_SwapLineWidth"); swapLineColor = FindProp("_SwapLineColor");
        swapProgress = FindProp("_SwapProgress"); useSwapLocalSpace = FindProp("_UseSwapLocalSpace"); swapShape = FindProp("_SwapShape");
        swapEffectCenter = FindProp("_SwapEffectCenter"); swapEffectExtents = FindProp("_SwapEffectExtents"); swapEffectRadius = FindProp("_SwapEffectRadius");
        swapType = FindProp("_SwapType"); swapShapeMask = FindProp("_SwapShapeMask"); swapTransitionHardness = FindProp("_SwapTransitionHardness");
        swapDirection = FindProp("_SwapDirection"); swapPatternType = FindProp("_SwapPatternType"); swapPatternFrequency = FindProp("_SwapPatternFrequency");
        enableDissolve = FindProp("_EnableDissolve"); dissolveType = FindProp("_DissolveType");
        dissolveThreshold = FindProp("_DissolveThreshold"); radialDirection = FindProp("_RadialDirection"); useTimeAnimation = FindProp("_UseTimeAnimation");
        timeScale = FindProp("_TimeScale"); useLocalSpace = FindProp("_UseLocalSpace"); dissolveDirection = FindProp("_DissolveDirection");
        dissolveNoiseTex = FindProp("_DissolveNoiseTex"); dissolveNoiseScale = FindProp("_DissolveNoiseScale"); dissolveNoiseStrength = FindProp("_DissolveNoiseStrength");
        dissolveEdgeWidth = FindProp("_DissolveEdgeWidth"); dissolveEdgeColor = FindProp("_DissolveEdgeColor"); patternType = FindProp("_PatternType");
        patternFrequency = FindProp("_PatternFrequency"); alphaFadeRange = FindProp("_AlphaFadeRange"); enableVertexDisplacement = FindProp("_EnableVertexDisplacement");
        useSaturateDisplacement = FindProp("_UseSaturateDisplacement"); enableShatterEffect = FindProp("_EnableShatterEffect"); vertexDisplacement = FindProp("_VertexDisplacement");
        bounceWaveWidth = FindProp("_BounceWaveWidth"); shatterStrength = FindProp("_ShatterStrength"); shatterLiftSpeed = FindProp("_ShatterLiftSpeed");
        shatterOffsetStrength = FindProp("_ShatterOffsetStrength"); shatterTriggerRange = FindProp("_ShatterTriggerRange"); toonRampOffset = FindProp("_ToonRampOffset");
        toonRampSmoothness = FindProp("_ToonRampSmoothness"); shadowTint = FindProp("_ShadowTint");
        toonSpecColor = FindProp("_Toon_SpecColor"); toonSpecSmoothness = FindProp("_Toon_SpecSmoothness"); toonSpecOffset = FindProp("_Toon_SpecOffset");
        toonRimColor = FindProp("_Toon_RimColor"); toonRimPower = FindProp("_Toon_RimPower"); toonRimMin = FindProp("_Toon_RimMin"); toonRimMax = FindProp("_Toon_RimMax");
        metalRamp = FindProp("_Metal_Ramp");
        metalBrightness = FindProp("_Metal_Brightness"); metalOffset = FindProp("_Metal_Offset"); metalSpecuColor = FindProp("_Metal_SpecuColor");
        metalHighlightOffset = FindProp("_Metal_HighlightOffset"); metalHiColor = FindProp("_Metal_HiColor"); metalRimColor = FindProp("_Metal_RimColor");
        metalRimPower = FindProp("_Metal_RimPower"); blingSpecColor = FindProp("_Bling_SpecColor"); blingSpecSmoothness = FindProp("_Bling_SpecSmoothness");
        blingSpecOffset = FindProp("_Bling_SpecOffset"); blingRimColor = FindProp("_Bling_RimColor"); blingRimPower = FindProp("_Bling_RimPower");
        blingRimMin = FindProp("_Bling_RimMin"); blingRimMax = FindProp("_Bling_RimMax"); enableBlingEffect = FindProp("_EnableBlingEffect");
        blingWorldSpace = FindProp("_BlingWorldSpace"); blingColor = FindProp("_BlingColor"); blingIntensity = FindProp("_BlingIntensity");
        blingScale = FindProp("_BlingScale"); blingSpeed = FindProp("_BlingSpeed"); blingFresnelPower = FindProp("_BlingFresnelPower");
        blingThreshold = FindProp("_BlingThreshold"); zWrite = FindProp("_ZWrite");
    }
    #endregion

    #region Helpers
    private void SetKeyword(string keyword, bool state)
    {
        if (state) material.EnableKeyword(keyword);
        else material.DisableKeyword(keyword);
    }

    private void DrawHeader(string text)
    {
        EditorGUILayout.LabelField(text, new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 16 });
        EditorGUILayout.Space();
    }

    private void DrawSubHeader(string text)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
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

        var rect = EditorGUILayout.GetControlRect(true, 22, style);
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
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(2);
            contents.Invoke();
            EditorGUILayout.Space(2);
            EditorGUILayout.EndVertical();
        }
    }
    #endregion
}
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Rendering;

public class ToonVatUberShaderGUI : ShaderGUI
{
    private enum SurfaceType { Opaque, Metallic, Foliage, Glass, Galaxy }
    private enum OutlineMode { None, InvertedHull, Fresnel }

    private static bool showVatSettings = true;
    private static bool showBaseSettings = true;
    private static bool showLightingSettings = true;
    private static bool showSurfaceTypeSettings = true;
    private static bool showOutlineSettings = true;

    private MaterialProperty surfaceTypeProp, srcBlendProp, dstBlendProp, zWriteProp;
    private MaterialProperty vatModeProp, enableVatInstancingProp, positionTextureProp, positionMinProp, positionMaxProp;
    private MaterialProperty currentAnimTimeProp, previousAnimTimeProp, animBlendWeightProp;
    private MaterialProperty baseMapProp, baseColorProp;
    private MaterialProperty alphaClipModeProp, cutoffProp;
    private MaterialProperty emissionModeProp, emissionColorProp, emissionMapProp;
    private MaterialProperty fakeLightModeProp, fakeLightColorProp, fakeLightDirectionProp;

    private MaterialProperty toonRampOffsetProp, toonRampSmoothnessProp, shadowTintProp, ambientColorProp;
    private MaterialProperty rampProp, brightnessProp, offsetProp, specuColorProp, highlightOffsetProp, hiColorProp, rimColorProp, rimPowerProp;
    private MaterialProperty windFrequencyProp, windAmplitudeProp, windDirectionProp, translucencyColorProp, translucencyStrengthProp;
    private MaterialProperty glassColorProp, fresnelColorProp, fresnelPowerProp, refractionStrengthProp, glassSpecularPowerProp, glassSpecularIntensityProp;

    private MaterialProperty outlineModeProp;
    private MaterialProperty outlineColorProp, outlineWidthProp, outlineScaleWithDistanceProp, distanceFadeStartProp, distanceFadeEndProp;
    private MaterialProperty fresnelOutlineColorProp, fresnelOutlineWidthProp, fresnelOutlinePowerProp, fresnelOutlineSharpnessProp;
    private MaterialProperty outlineGlintProp;
    private MaterialProperty glintColorProp, glintScaleProp, glintSpeedProp, glintThresholdProp;

    private MaterialProperty starfieldMapProp, dustColor1Prop, dustColor2Prop, dustColor3Prop, starfieldColorProp, starfieldScaleProp;
    private MaterialProperty noiseScaleProp, noiseSpeed1Prop, noiseSpeed2Prop, parallaxStrengthProp;
    private MaterialProperty galaxyRimRampToggleProp, galaxyRimRampTextureProp, galaxyRimColorProp, galaxyRimPowerProp;
    private MaterialProperty fresnelRampToggleProp, fresnelRampTextureProp;
    private MaterialProperty noiseMapProp;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        Material material = materialEditor.target as Material;
        FindProperties(properties);

        EditorGUI.BeginChangeCheck();

        DrawWorkflowSettings(materialEditor);

        DrawVatSettings(materialEditor, material);
        DrawFoldout("Base Properties", ref showBaseSettings, () => DrawBaseProperties(materialEditor));
        DrawFoldout("Lighting", ref showLightingSettings, () => DrawLightingProperties(materialEditor));

        var surface = (SurfaceType)surfaceTypeProp.floatValue;
        DrawFoldout($"{surface} Settings", ref showSurfaceTypeSettings, () => DrawSurfaceTypeProperties(materialEditor, surface));

        DrawFoldout("Outline", ref showOutlineSettings, () => DrawOutlineProperties(materialEditor));

        if (EditorGUI.EndChangeCheck())
        {
            SetMaterialKeywords(material);
        }

        EditorGUILayout.Space();
        materialEditor.EnableInstancingField();
    }

    private void FindProperties(MaterialProperty[] properties)
    {
        surfaceTypeProp = FindProperty("_SurfaceType", properties);
        srcBlendProp = FindProperty("_SrcBlend", properties);
        dstBlendProp = FindProperty("_DstBlend", properties);
        zWriteProp = FindProperty("_ZWrite", properties);

        vatModeProp = FindProperty("_VatMode", properties);
        enableVatInstancingProp = FindProperty("_EnableVatInstancing", properties);
        positionTextureProp = FindProperty("_PositionTexture", properties);
        positionMinProp = FindProperty("_PositionMin", properties);
        positionMaxProp = FindProperty("_PositionMax", properties);
        currentAnimTimeProp = FindProperty("_CurrentAnimNormalizedTime", properties);
        previousAnimTimeProp = FindProperty("_PreviousAnimNormalizedTime", properties);
        animBlendWeightProp = FindProperty("_AnimationBlendWeight", properties);

        baseMapProp = FindProperty("_BaseMap", properties);
        baseColorProp = FindProperty("_BaseColor", properties);
        alphaClipModeProp = FindProperty("_AlphaClipMode", properties);
        cutoffProp = FindProperty("_Cutoff", properties);
        emissionModeProp = FindProperty("_EmissionMode", properties);
        emissionColorProp = FindProperty("_EmissionColor", properties);
        emissionMapProp = FindProperty("_EmissionMap", properties);

        fakeLightModeProp = FindProperty("_FakeLightMode", properties);
        fakeLightColorProp = FindProperty("_FakeLightColor", properties);
        fakeLightDirectionProp = FindProperty("_FakeLightDirection", properties);

        toonRampOffsetProp = FindProperty("_ToonRampOffset", properties);
        toonRampSmoothnessProp = FindProperty("_ToonRampSmoothness", properties);
        shadowTintProp = FindProperty("_ShadowTint", properties);
        ambientColorProp = FindProperty("_AmbientColor", properties);
        rampProp = FindProperty("_Ramp", properties);
        brightnessProp = FindProperty("_Brightness", properties);
        offsetProp = FindProperty("_Offset", properties);
        specuColorProp = FindProperty("_SpecuColor", properties);
        highlightOffsetProp = FindProperty("_HighlightOffset", properties);
        hiColorProp = FindProperty("_HiColor", properties);
        rimColorProp = FindProperty("_RimColor", properties);
        rimPowerProp = FindProperty("_RimPower", properties);
        windFrequencyProp = FindProperty("_WindFrequency", properties);
        windAmplitudeProp = FindProperty("_WindAmplitude", properties);
        windDirectionProp = FindProperty("_WindDirection", properties);
        translucencyColorProp = FindProperty("_TranslucencyColor", properties);
        translucencyStrengthProp = FindProperty("_TranslucencyStrength", properties);
        glassColorProp = FindProperty("_GlassColor", properties);
        fresnelColorProp = FindProperty("_FresnelColor", properties);
        fresnelPowerProp = FindProperty("_FresnelPower", properties);
        refractionStrengthProp = FindProperty("_RefractionStrength", properties);
        glassSpecularPowerProp = FindProperty("_GlassSpecularPower", properties);
        glassSpecularIntensityProp = FindProperty("_GlassSpecularIntensity", properties);

        outlineModeProp = FindProperty("_OutlineMode", properties);
        outlineColorProp = FindProperty("_OutlineColor", properties);
        outlineWidthProp = FindProperty("_OutlineWidth", properties);
        outlineScaleWithDistanceProp = FindProperty("_OutlineScaleWithDistance", properties);
        distanceFadeStartProp = FindProperty("_DistanceFadeStart", properties);
        distanceFadeEndProp = FindProperty("_DistanceFadeEnd", properties);
        fresnelOutlineColorProp = FindProperty("_FresnelOutlineColor", properties);
        fresnelOutlineWidthProp = FindProperty("_FresnelOutlineWidth", properties);
        fresnelOutlinePowerProp = FindProperty("_FresnelOutlinePower", properties);
        fresnelOutlineSharpnessProp = FindProperty("_FresnelOutlineSharpness", properties);
        outlineGlintProp = FindProperty("_OutlineGlint", properties);
        glintColorProp = FindProperty("_GlintColor", properties);
        glintScaleProp = FindProperty("_GlintScale", properties);
        glintSpeedProp = FindProperty("_GlintSpeed", properties);
        glintThresholdProp = FindProperty("_GlintThreshold", properties);

        starfieldMapProp = FindProperty("_StarfieldMap", properties);
        starfieldColorProp = FindProperty("_StarfieldColor", properties);
        starfieldScaleProp = FindProperty("_StarfieldScale", properties);
        dustColor1Prop = FindProperty("_DustColor1", properties);
        dustColor2Prop = FindProperty("_DustColor2", properties);
        dustColor3Prop = FindProperty("_DustColor3", properties);
        noiseScaleProp = FindProperty("_NoiseScale", properties);
        noiseSpeed1Prop = FindProperty("_NoiseSpeed1", properties);
        noiseSpeed2Prop = FindProperty("_NoiseSpeed2", properties);
        galaxyRimRampToggleProp = FindProperty("_GalaxyRimRampToggle", properties);
        galaxyRimRampTextureProp = FindProperty("_GalaxyRimRampTexture", properties);
        galaxyRimColorProp = FindProperty("_GalaxyRimColor", properties);
        galaxyRimPowerProp = FindProperty("_GalaxyRimPower", properties);
        fresnelRampToggleProp = FindProperty("_FresnelRampToggle", properties);
        fresnelRampTextureProp = FindProperty("_FresnelRampTexture", properties);
        parallaxStrengthProp = FindProperty("_ParallaxStrength", properties);
        noiseMapProp = FindProperty("_NoiseMap", properties);
    }

    private void DrawWorkflowSettings(MaterialEditor materialEditor)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Workflow", EditorStyles.boldLabel);
        materialEditor.ShaderProperty(surfaceTypeProp, "Surface Type");
        EditorGUILayout.EndVertical();
    }

    private void DrawVatSettings(MaterialEditor materialEditor, Material material)
    {
        DrawFoldout("VAT (Vertex Animation Texture)", ref showVatSettings, () =>
        {
            materialEditor.ShaderProperty(vatModeProp, "Enable VAT");
            if (vatModeProp.floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(enableVatInstancingProp, "Enable GPU Instancing");

                materialEditor.TexturePropertySingleLine(new GUIContent(positionTextureProp.displayName), positionTextureProp);
                materialEditor.ShaderProperty(positionMinProp, positionMinProp.displayName);
                materialEditor.ShaderProperty(positionMaxProp, positionMaxProp.displayName);

                if (enableVatInstancingProp.floatValue < 0.5f)
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Animation Debug (Read-Only)", EditorStyles.boldLabel);
                    EditorGUI.BeginDisabledGroup(true);
                    materialEditor.ShaderProperty(currentAnimTimeProp, currentAnimTimeProp.displayName);
                    materialEditor.ShaderProperty(previousAnimTimeProp, previousAnimTimeProp.displayName);
                    materialEditor.ShaderProperty(animBlendWeightProp, animBlendWeightProp.displayName);
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUI.indentLevel--;
            }
        });
    }

    private void DrawBaseProperties(MaterialEditor materialEditor)
    {
        materialEditor.TexturePropertySingleLine(new GUIContent(baseMapProp.displayName), baseMapProp, baseColorProp);

        materialEditor.ShaderProperty(alphaClipModeProp, "Enable Alpha Clip");
        if (alphaClipModeProp.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(cutoffProp, cutoffProp.displayName);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        materialEditor.ShaderProperty(emissionModeProp, "Enable Emission");
        if (emissionModeProp.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            materialEditor.ColorProperty(emissionColorProp, "Color");
            materialEditor.TexturePropertySingleLine(new GUIContent(emissionMapProp.displayName), emissionMapProp);
            EditorGUI.indentLevel--;
        }
    }

    private void DrawLightingProperties(MaterialEditor materialEditor)
    {
        materialEditor.ShaderProperty(fakeLightModeProp, "Enable Fake Light");
        if (fakeLightModeProp.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(fakeLightColorProp, "Color");
            materialEditor.ShaderProperty(fakeLightDirectionProp, "Direction");
            EditorGUI.indentLevel--;
        }
    }

    private void DrawSurfaceTypeProperties(MaterialEditor materialEditor, SurfaceType surface)
    {
        switch (surface)
        {
            case SurfaceType.Opaque:
                materialEditor.ShaderProperty(toonRampOffsetProp, "Ramp Offset");
                materialEditor.ShaderProperty(toonRampSmoothnessProp, "Ramp Smoothness");
                materialEditor.ShaderProperty(shadowTintProp, "Shadow Tint");
                materialEditor.ColorProperty(ambientColorProp, "Ambient Color");
                break;
            case SurfaceType.Metallic:
                materialEditor.TexturePropertySingleLine(new GUIContent("Ramp"), rampProp);
                materialEditor.ShaderProperty(brightnessProp, "Brightness");
                materialEditor.ShaderProperty(offsetProp, "Specular Size");
                materialEditor.ShaderProperty(specuColorProp, "Specular Color");
                materialEditor.ShaderProperty(highlightOffsetProp, "Highlight Size");
                materialEditor.ShaderProperty(hiColorProp, "Highlight Color");
                materialEditor.ShaderProperty(rimColorProp, "Rim Color");
                materialEditor.ShaderProperty(rimPowerProp, "Rim Power");
                break;
            case SurfaceType.Foliage:
                materialEditor.ShaderProperty(windFrequencyProp, "Wind Frequency");
                materialEditor.ShaderProperty(windAmplitudeProp, "Wind Amplitude");
                materialEditor.ShaderProperty(windDirectionProp, "Wind Direction");
                materialEditor.ShaderProperty(translucencyColorProp, "Translucency Color");
                materialEditor.ShaderProperty(translucencyStrengthProp, "Translucency Strength");
                break;
            case SurfaceType.Glass:
                materialEditor.ShaderProperty(glassColorProp, "Glass Tint & Transparency");
                materialEditor.ShaderProperty(fresnelColorProp, "Fresnel Color");
                materialEditor.ShaderProperty(fresnelPowerProp, "Fresnel Power");
                materialEditor.ShaderProperty(refractionStrengthProp, "Refraction Strength");
                materialEditor.ShaderProperty(glassSpecularPowerProp, "Specular Power");
                materialEditor.ShaderProperty(glassSpecularIntensityProp, "Specular Intensity");
                break;
            case SurfaceType.Galaxy:
                EditorGUILayout.LabelField("Base", EditorStyles.boldLabel);
                materialEditor.TexturePropertySingleLine(new GUIContent("Starfield Map"), starfieldMapProp, starfieldColorProp);
                materialEditor.ShaderProperty(starfieldScaleProp, "Starfield Scale");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Cosmic Dust", EditorStyles.boldLabel);
                materialEditor.TexturePropertySingleLine(new GUIContent("Noise Texture (Grayscale)"), noiseMapProp);
                materialEditor.ColorProperty(dustColor1Prop, "Dust Color 1");
                materialEditor.ColorProperty(dustColor2Prop, "Dust Color 2");
                materialEditor.ColorProperty(dustColor3Prop, "Dust Color 3");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Movement & Depth", EditorStyles.boldLabel);
                materialEditor.ShaderProperty(noiseScaleProp, "Noise Scale");
                materialEditor.ShaderProperty(parallaxStrengthProp, "Parallax Depth");
                materialEditor.ShaderProperty(noiseSpeed1Prop, "Dust Speed 1 (Far)");
                materialEditor.ShaderProperty(noiseSpeed2Prop, "Dust Speed 2 (Near)");

                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Rim Light", EditorStyles.boldLabel);
                materialEditor.ColorProperty(galaxyRimColorProp, "Rim Color / Tint");
                materialEditor.ShaderProperty(galaxyRimRampToggleProp, "Use Ramp Texture");
                if (galaxyRimRampToggleProp.floatValue > 0.5f)
                {
                    materialEditor.TexturePropertySingleLine(new GUIContent("Rim Ramp"), galaxyRimRampTextureProp);
                }
                materialEditor.ShaderProperty(galaxyRimPowerProp, "Rim Power");
                break;
        }
    }

    private void DrawOutlineProperties(MaterialEditor materialEditor)
    {
        materialEditor.ShaderProperty(outlineModeProp, "Mode");
        var mode = (OutlineMode)outlineModeProp.floatValue;

        EditorGUI.indentLevel++;
        switch (mode)
        {
            case OutlineMode.InvertedHull:
                materialEditor.ShaderProperty(outlineColorProp, "Color");
                materialEditor.ShaderProperty(outlineWidthProp, "Width");
                materialEditor.ShaderProperty(outlineScaleWithDistanceProp, "Screen-Space Scaling");
                materialEditor.ShaderProperty(distanceFadeStartProp, "Distance Fade Start");
                materialEditor.ShaderProperty(distanceFadeEndProp, "Distance Fade End");
                break;

            case OutlineMode.Fresnel:
                materialEditor.ShaderProperty(fresnelOutlineColorProp, "Color");

                materialEditor.ShaderProperty(fresnelRampToggleProp, "Use Ramp Texture");
                if (fresnelRampToggleProp.floatValue > 0.5f)
                {
                    materialEditor.TexturePropertySingleLine(new GUIContent("Fresnel Ramp"), fresnelRampTextureProp);
                }

                materialEditor.ShaderProperty(fresnelOutlineWidthProp, "Width");
                materialEditor.ShaderProperty(fresnelOutlinePowerProp, "Power");
                materialEditor.ShaderProperty(fresnelOutlineSharpnessProp, "Sharpness");

                EditorGUILayout.Space();
                materialEditor.ShaderProperty(outlineGlintProp, "Enable Glint");
                if (outlineGlintProp.floatValue > 0.5f)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(glintColorProp, "Glint Color");
                    materialEditor.ShaderProperty(glintScaleProp, "Glint Scale");
                    materialEditor.ShaderProperty(glintSpeedProp, "Glint Speed");
                    materialEditor.ShaderProperty(glintThresholdProp, "Glint Threshold");
                    EditorGUI.indentLevel--;
                }
                break;
        }
        EditorGUI.indentLevel--;
    }

    private void SetMaterialKeywords(Material material)
    {
        var surface = (SurfaceType)material.GetFloat(surfaceTypeProp.name);
        switch (surface)
        {
            case SurfaceType.Glass:
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat(srcBlendProp.name, (float)BlendMode.SrcAlpha);
                material.SetFloat(dstBlendProp.name, (float)BlendMode.OneMinusSrcAlpha);
                material.SetFloat(zWriteProp.name, 0.0f);
                material.renderQueue = (int)RenderQueue.Transparent;
                break;
            default:
                material.SetOverrideTag("RenderType", "Opaque");
                material.SetFloat(srcBlendProp.name, (float)BlendMode.One);
                material.SetFloat(dstBlendProp.name, (float)BlendMode.Zero);
                material.SetFloat(zWriteProp.name, 1.0f);
                material.renderQueue = (int)RenderQueue.Geometry;
                break;
        }

        SetKeyword(material, "_SURFACETYPE_OPAQUE", surface == SurfaceType.Opaque);
        SetKeyword(material, "_SURFACETYPE_METALLIC", surface == SurfaceType.Metallic);
        SetKeyword(material, "_SURFACETYPE_FOLIAGE", surface == SurfaceType.Foliage);
        SetKeyword(material, "_SURFACETYPE_GLASS", surface == SurfaceType.Glass);
        SetKeyword(material, "_SURFACETYPE_GALAXY", surface == SurfaceType.Galaxy);

        bool isVat = material.GetFloat(vatModeProp.name) > 0.5f;
        SetKeyword(material, "_VAT_ON", isVat);
        SetKeyword(material, "_VAT_INSTANCING_ON", isVat && material.GetFloat(enableVatInstancingProp.name) > 0.5f);

        var outlineMode = (OutlineMode)material.GetFloat(outlineModeProp.name);
        SetKeyword(material, "_OUTLINEMODE_INVERTEDHULL", outlineMode == OutlineMode.InvertedHull);
        SetKeyword(material, "_OUTLINEMODE_FRESNEL", outlineMode == OutlineMode.Fresnel);
        SetKeyword(material, "_OUTLINEGLINT_ON", outlineMode == OutlineMode.Fresnel && material.GetFloat(outlineGlintProp.name) > 0.5f);

        SetKeyword(material, "_FRESNEL_RAMP_ON", outlineMode == OutlineMode.Fresnel && material.GetFloat(fresnelRampToggleProp.name) > 0.5f);
        SetKeyword(material, "_GALAXY_RIM_RAMP_ON", surface == SurfaceType.Galaxy && material.GetFloat(galaxyRimRampToggleProp.name) > 0.5f);
    }

    private static void SetKeyword(Material m, string keyword, bool state)
    {
        if (state) m.EnableKeyword(keyword);
        else m.DisableKeyword(keyword);
    }

    private void DrawFoldout(string title, ref bool foldoutState, Action contents)
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
            EditorStyles.foldout.Draw(toggleRect, false, false, foldoutState, false);
        }

        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            foldoutState = !foldoutState;
            e.Use();
        }

        if (foldoutState)
        {
            EditorGUILayout.BeginVertical("box");
            contents();
            EditorGUILayout.EndVertical();
        }
    }
}
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class ToonMasterShaderGUI : ShaderGUI
{
    private MaterialEditor materialEditor;
    private Material material;
    private MaterialProperty[] properties;

    private static Dictionary<string, bool> foldoutStates = new Dictionary<string, bool>();

    private MaterialProperty workflow, outlineMode;
    private MaterialProperty baseMap, baseColor, alphaClipToggle, cutoff;
    private MaterialProperty lightRamp, shadowTint, ambientColor;
    private MaterialProperty specularMode, specularColor, specularStrength, specularToonSize, specularToonThreshold, specularSoftness, anisotropicOffset;
    private MaterialProperty rimLightToggle, rimColor, rimPower, rimThreshold, rimMaskedByLight;
    private MaterialProperty matCapToggle, matCapTexture, matCapBlendMode, matCapIntensity;
    private MaterialProperty hatchingToggle, hatchingTexture, hatchingTiling, hatchingColor, hatchingShadowThreshold;
    private MaterialProperty outlineColor, outlineWidth, outlineNoiseFrequency, outlineNoiseAmplitude;
    private MaterialProperty fresnelOutlineColor, fresnelOutlineWidth, fresnelOutlinePower;
    private MaterialProperty emissionToggle, emissionColor, emissionMap;
    private MaterialProperty interiorGlowToggle, interiorGlowColor, interiorGlowPower;
    private MaterialProperty opacity, srcBlend, dstBlend, zWrite;

    private void FindProperties()
    {
        workflow = FindProperty("_Workflow", properties);
        outlineMode = FindProperty("_OutlineMode", properties);
        baseMap = FindProperty("_BaseMap", properties);
        baseColor = FindProperty("_BaseColor", properties);
        alphaClipToggle = FindProperty("_AlphaClipToggle", properties);
        cutoff = FindProperty("_Cutoff", properties);
        lightRamp = FindProperty("_LightRamp", properties);
        shadowTint = FindProperty("_ShadowTint", properties);
        ambientColor = FindProperty("_AmbientColor", properties);
        specularMode = FindProperty("_SpecularMode", properties);
        specularColor = FindProperty("_SpecularColor", properties);
        specularStrength = FindProperty("_SpecularStrength", properties);
        specularToonSize = FindProperty("_SpecularToonSize", properties);
        specularToonThreshold = FindProperty("_SpecularToonThreshold", properties);
        specularSoftness = FindProperty("_SpecularSoftness", properties);
        anisotropicOffset = FindProperty("_AnisotropicOffset", properties);
        rimLightToggle = FindProperty("_RimLightToggle", properties);
        rimColor = FindProperty("_RimColor", properties);
        rimPower = FindProperty("_RimPower", properties);
        rimThreshold = FindProperty("_RimThreshold", properties);
        rimMaskedByLight = FindProperty("_RimMaskedByLight", properties);
        matCapToggle = FindProperty("_MatCapToggle", properties);
        matCapTexture = FindProperty("_MatCapTexture", properties);
        matCapBlendMode = FindProperty("_MatCapBlendMode", properties);
        matCapIntensity = FindProperty("_MatCapIntensity", properties);
        hatchingToggle = FindProperty("_HatchingToggle", properties);
        hatchingTexture = FindProperty("_HatchingTexture", properties);
        hatchingTiling = FindProperty("_HatchingTiling", properties);
        hatchingColor = FindProperty("_HatchingColor", properties);
        hatchingShadowThreshold = FindProperty("_HatchingShadowThreshold", properties);
        outlineColor = FindProperty("_OutlineColor", properties);
        outlineWidth = FindProperty("_OutlineWidth", properties);
        outlineNoiseFrequency = FindProperty("_OutlineNoiseFrequency", properties);
        outlineNoiseAmplitude = FindProperty("_OutlineNoiseAmplitude", properties);
        fresnelOutlineColor = FindProperty("_FresnelOutlineColor", properties);
        fresnelOutlineWidth = FindProperty("_FresnelOutlineWidth", properties);
        fresnelOutlinePower = FindProperty("_FresnelOutlinePower", properties);
        emissionToggle = FindProperty("_EmissionToggle", properties);
        emissionColor = FindProperty("_EmissionColor", properties);
        emissionMap = FindProperty("_EmissionMap", properties);
        interiorGlowToggle = FindProperty("_InteriorGlowToggle", properties);
        interiorGlowColor = FindProperty("_InteriorGlowColor", properties);
        interiorGlowPower = FindProperty("_InteriorGlowPower", properties);
        opacity = FindProperty("_Opacity", properties);
        srcBlend = FindProperty("_SrcBlend", properties);
        dstBlend = FindProperty("_DstBlend", properties);
        zWrite = FindProperty("_ZWrite", properties);
    }

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
    {
        materialEditor = editor;
        material = editor.target as Material;
        properties = props;

        FindProperties();

        DrawHeader("Bill's Toon Master Shader");

        EditorGUI.BeginChangeCheck();
        {
            DrawWorkflowSettings();
            DrawModule("Base Properties", () =>
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Albedo (RGB) Alpha (A)"), baseMap, baseColor);
                materialEditor.ShaderProperty(alphaClipToggle, "Enable Alpha Clip");
                if (alphaClipToggle.floatValue > 0)
                    materialEditor.ShaderProperty(cutoff, "Alpha Cutoff");
            });
            DrawModule("Lighting", () =>
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("Lighting Ramp (1D)"), lightRamp);
                materialEditor.ShaderProperty(shadowTint, "Shadow Tint");
                materialEditor.ShaderProperty(ambientColor, "Ambient Color");
            });
            DrawModule("Specular", () =>
            {
                materialEditor.ShaderProperty(specularMode, "Mode");
                if (specularMode.floatValue > 0)
                {
                    materialEditor.ShaderProperty(specularColor, "Color");
                    materialEditor.ShaderProperty(specularStrength, "Strength");
                    var mode = (int)specularMode.floatValue;
                    if (mode == 1) // Toon
                    {
                        materialEditor.ShaderProperty(specularToonThreshold, "Threshold");
                        materialEditor.ShaderProperty(specularToonSize, "Size");
                    }
                    else if (mode == 2) // Soft
                    {
                        materialEditor.ShaderProperty(specularSoftness, "Softness");
                    }
                    else if (mode == 3) // Anisotropic
                    {
                        materialEditor.ShaderProperty(specularSoftness, "Softness");
                        materialEditor.ShaderProperty(anisotropicOffset, "Offset");
                    }
                }
            });
            DrawModule("Rim Light", () =>
            {
                materialEditor.ShaderProperty(rimLightToggle, "Enable");
                if (rimLightToggle.floatValue > 0)
                {
                    materialEditor.ShaderProperty(rimColor, "Color");
                    materialEditor.ShaderProperty(rimPower, "Power");
                    materialEditor.ShaderProperty(rimThreshold, "Threshold");
                    materialEditor.ShaderProperty(rimMaskedByLight, "Mask by Light");
                }
            });
            DrawModule("MatCap", () =>
            {
                materialEditor.ShaderProperty(matCapToggle, "Enable");
                if (matCapToggle.floatValue > 0)
                {
                    materialEditor.TexturePropertySingleLine(new GUIContent("MatCap Texture"), matCapTexture);
                    materialEditor.ShaderProperty(matCapBlendMode, "Blend Mode");
                    materialEditor.ShaderProperty(matCapIntensity, "Intensity / Lerp Factor");
                }
            });
            DrawModule("Painterly Hatching", () =>
            {
                materialEditor.ShaderProperty(hatchingToggle, "Enable");
                if (hatchingToggle.floatValue > 0)
                {
                    materialEditor.TexturePropertySingleLine(new GUIContent("Hatching Texture"), hatchingTexture);
                    materialEditor.ShaderProperty(hatchingTiling, "Tiling");
                    materialEditor.ShaderProperty(hatchingColor, "Color");
                    materialEditor.ShaderProperty(hatchingShadowThreshold, "Shadow Threshold");
                }
            });
            DrawModule("Effects", () =>
            {
                materialEditor.ShaderProperty(emissionToggle, "Enable Emission");
                if (emissionToggle.floatValue > 0)
                {
                    materialEditor.TexturePropertySingleLine(new GUIContent("Emission Map"), emissionMap, emissionColor);
                }
                EditorGUILayout.Space();
                materialEditor.ShaderProperty(interiorGlowToggle, "Enable Interior Glow");
                if (interiorGlowToggle.floatValue > 0)
                {
                    materialEditor.ShaderProperty(interiorGlowColor, "Color");
                    materialEditor.ShaderProperty(interiorGlowPower, "Power");
                }
            });
        }
        if (EditorGUI.EndChangeCheck())
        {
            SetKeywordsAndRenderStates();
        }
    }

    private void DrawWorkflowSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        materialEditor.ShaderProperty(workflow, "Workflow");

        if ((int)workflow.floatValue == 1) // Transparent
        {
            materialEditor.ShaderProperty(opacity, "Opacity");
        }

        materialEditor.ShaderProperty(outlineMode, "Outline Mode");

        var currentOutlineMode = (int)outlineMode.floatValue;
        if (currentOutlineMode == 1) // Hull
        {
            EditorGUILayout.LabelField("Inverted Hull Outline", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(outlineColor, "Color");
            materialEditor.ShaderProperty(outlineWidth, "Width");
            materialEditor.ShaderProperty(outlineNoiseFrequency, "Noise Frequency");
            materialEditor.ShaderProperty(outlineNoiseAmplitude, "Noise Amplitude");
        }
        else if (currentOutlineMode == 2) // Fresnel
        {
            EditorGUILayout.LabelField("Fresnel Outline", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(fresnelOutlineColor, "Color");
            materialEditor.ShaderProperty(fresnelOutlineWidth, "Width");
            materialEditor.ShaderProperty(fresnelOutlinePower, "Power");
        }
        EditorGUILayout.EndVertical();
    }

    private void SetKeywordsAndRenderStates()
    {
        // Keywords
        SetKeyword("_ALPHACLIP_ON", alphaClipToggle.floatValue > 0);
        SetKeyword("_RIMLIGHT_ON", rimLightToggle.floatValue > 0);
        SetKeyword("_MATCAP_ON", matCapToggle.floatValue > 0);
        SetKeyword("_HATCHING_ON", hatchingToggle.floatValue > 0);
        SetKeyword("_EMISSION_ON", emissionToggle.floatValue > 0);
        SetKeyword("_INTERIORGLOW_ON", interiorGlowToggle.floatValue > 0);

        SetKeyword("_SPECULARMODE_NONE", (int)specularMode.floatValue == 0);
        SetKeyword("_SPECULARMODE_HARD_TOON", (int)specularMode.floatValue == 1);
        SetKeyword("_SPECULARMODE_SOFT", (int)specularMode.floatValue == 2);
        SetKeyword("_SPECULARMODE_ANISOTROPIC", (int)specularMode.floatValue == 3);

        SetKeyword("_MATCAPBLENDMODE_ADD", (int)matCapBlendMode.floatValue == 0);
        SetKeyword("_MATCAPBLENDMODE_MULTIPLY", (int)matCapBlendMode.floatValue == 1);
        SetKeyword("_MATCAPBLENDMODE_LERP", (int)matCapBlendMode.floatValue == 2);

        SetKeyword("_OUTLINEMODE_NONE", (int)outlineMode.floatValue == 0);
        SetKeyword("_OUTLINEMODE_INVERTED_HULL", (int)outlineMode.floatValue == 1);
        SetKeyword("_OUTLINEMODE_FRESNEL", (int)outlineMode.floatValue == 2);

        // Render States
        if ((int)workflow.floatValue == 0) // Opaque
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            material.SetOverrideTag("RenderType", "Opaque");
        }
        else // Transparent
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
        }
    }

    private void SetKeyword(string keyword, bool enabled)
    {
        if (enabled) material.EnableKeyword(keyword);
        else material.DisableKeyword(keyword);
    }

    private void DrawHeader(string text)
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        var style = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter };
        EditorGUILayout.LabelField(text, style, GUILayout.ExpandWidth(true));
        EditorGUILayout.EndVertical();
    }

    private void DrawModule(string title, Action content)
    {
        bool state = GetFoldoutState(title);
        var rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        state = EditorGUILayout.Foldout(state, title, true, EditorStyles.foldout);
        SetFoldoutState(title, state);

        if (state)
        {
            EditorGUILayout.Space(2);
            content();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private bool GetFoldoutState(string title)
    {
        if (!foldoutStates.ContainsKey(title))
            foldoutStates[title] = true;
        return foldoutStates[title];
    }
    private void SetFoldoutState(string title, bool state)
    {
        foldoutStates[title] = state;
    }
}
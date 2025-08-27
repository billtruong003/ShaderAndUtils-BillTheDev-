using UnityEngine;
using UnityEditor;
using System;

public class ToonOpaqueHighQualityShaderGUI : ShaderGUI
{
    private MaterialEditor materialEditor;

    // Foldout states
    private bool showBaseProperties = true;
    private bool showEffects = true;
    private bool showRenderStates = true;
    private bool showShadingModel = true;
    private bool showOutline = false;
    private bool showAdvanced = false;

    // Properties (same as before)
    #region Material Properties
    private MaterialProperty surfaceType;
    private MaterialProperty baseMap;
    private MaterialProperty baseColor;
    private MaterialProperty alphaClipMode;
    private MaterialProperty cutoff;
    private MaterialProperty emissionMode;
    private MaterialProperty emissionColor;
    private MaterialProperty emissionMap;
    private MaterialProperty cullMode;
    private MaterialProperty fakeLightMode;
    private MaterialProperty fakeLightColor;
    private MaterialProperty fakeLightDirection;
    private MaterialProperty highlightThreshold;
    private MaterialProperty midtoneThreshold;
    private MaterialProperty shadowThreshold;
    private MaterialProperty rampSmoothness;
    private MaterialProperty highlightColor;
    private MaterialProperty midtoneColor;
    private MaterialProperty shadowColor;
    private MaterialProperty metallicHotSpotThreshold;
    private MaterialProperty metallicSpecularThreshold;
    private MaterialProperty metallicReflectionThreshold;
    private MaterialProperty metallicRampSmoothness;
    private MaterialProperty metallicBaseColor;
    private MaterialProperty metallicReflectionColor;
    private MaterialProperty metallicSpecularColor;
    private MaterialProperty metallicHotSpotColor;
    private MaterialProperty rimColor;
    private MaterialProperty rimPower;
    private MaterialProperty windFrequency;
    private MaterialProperty windAmplitude;
    private MaterialProperty windDirection;
    private MaterialProperty translucencyColor;
    private MaterialProperty translucencyStrength;
    private MaterialProperty fresnelOutlineToggle;
    private MaterialProperty fresnelOutlineColor;
    private MaterialProperty fresnelOutlineWidth;
    private MaterialProperty fresnelOutlinePower;
    private MaterialProperty fresnelOutlineSharpness;
    private MaterialProperty glintToggle;
    private MaterialProperty glintColor;
    private MaterialProperty glintScale;
    private MaterialProperty glintSpeed;
    private MaterialProperty glintThreshold;
    private MaterialProperty ambientColor;
    #endregion

    private void FindProperties(MaterialProperty[] props)
    {
        surfaceType = FindProperty("_SurfaceType", props);
        baseMap = FindProperty("_BaseMap", props);
        baseColor = FindProperty("_BaseColor", props);
        alphaClipMode = FindProperty("_AlphaClipMode", props);
        cutoff = FindProperty("_Cutoff", props);
        emissionMode = FindProperty("_EmissionMode", props);
        emissionColor = FindProperty("_EmissionColor", props);
        emissionMap = FindProperty("_EmissionMap", props);
        cullMode = FindProperty("_CullMode", props);
        fakeLightMode = FindProperty("_FakeLightMode", props);
        fakeLightColor = FindProperty("_FakeLightColor", props);
        fakeLightDirection = FindProperty("_FakeLightDirection", props);
        highlightThreshold = FindProperty("_HighlightThreshold", props);
        midtoneThreshold = FindProperty("_MidtoneThreshold", props);
        shadowThreshold = FindProperty("_ShadowThreshold", props);
        rampSmoothness = FindProperty("_RampSmoothness", props);
        highlightColor = FindProperty("_HighlightColor", props);
        midtoneColor = FindProperty("_MidtoneColor", props);
        shadowColor = FindProperty("_ShadowColor", props);
        metallicHotSpotThreshold = FindProperty("_MetallicHotSpotThreshold", props);
        metallicSpecularThreshold = FindProperty("_MetallicSpecularThreshold", props);
        metallicReflectionThreshold = FindProperty("_MetallicReflectionThreshold", props);
        metallicRampSmoothness = FindProperty("_MetallicRampSmoothness", props);
        metallicBaseColor = FindProperty("_MetallicBaseColor", props);
        metallicReflectionColor = FindProperty("_MetallicReflectionColor", props);
        metallicSpecularColor = FindProperty("_MetallicSpecularColor", props);
        metallicHotSpotColor = FindProperty("_MetallicHotSpotColor", props);
        rimColor = FindProperty("_RimColor", props);
        rimPower = FindProperty("_RimPower", props);
        windFrequency = FindProperty("_WindFrequency", props);
        windAmplitude = FindProperty("_WindAmplitude", props);
        windDirection = FindProperty("_WindDirection", props);
        translucencyColor = FindProperty("_TranslucencyColor", props);
        translucencyStrength = FindProperty("_TranslucencyStrength", props);
        fresnelOutlineToggle = FindProperty("_FresnelOutlineToggle", props);
        fresnelOutlineColor = FindProperty("_FresnelOutlineColor", props);
        fresnelOutlineWidth = FindProperty("_FresnelOutlineWidth", props);
        fresnelOutlinePower = FindProperty("_FresnelOutlinePower", props);
        fresnelOutlineSharpness = FindProperty("_FresnelOutlineSharpness", props);
        glintToggle = FindProperty("_GlintToggle", props);
        glintColor = FindProperty("_GlintColor", props);
        glintScale = FindProperty("_GlintScale", props);
        glintSpeed = FindProperty("_GlintSpeed", props);
        glintThreshold = FindProperty("_GlintThreshold", props);
        ambientColor = FindProperty("_AmbientColor", props);
    }

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
    {
        materialEditor = editor;
        FindProperties(props);

        Material material = materialEditor.target as Material;

        DrawSurfaceType(material);
        EditorGUILayout.Space();

        // Base Properties Foldout
        showBaseProperties = EditorGUILayout.Foldout(showBaseProperties, "Base Properties", true, EditorStyles.foldout);
        if (showBaseProperties)
        {
            EditorGUI.indentLevel++;
            materialEditor.TexturePropertySingleLine(new GUIContent(baseMap.displayName), baseMap, baseColor);
            EditorGUI.indentLevel--;
        }

        // Effects Foldout
        showEffects = EditorGUILayout.Foldout(showEffects, "Effects", true, EditorStyles.foldout);
        if (showEffects)
        {
            EditorGUI.indentLevel++;
            DrawToggleSection(alphaClipMode, "_ALPHACLIP_ON", () => materialEditor.ShaderProperty(cutoff, "Alpha Cutoff"), false);
            DrawToggleSection(emissionMode, "_EMISSION_ON", () =>
            {
                materialEditor.ShaderProperty(emissionColor, "Emission Color");
                materialEditor.TexturePropertySingleLine(new GUIContent(emissionMap.displayName), emissionMap);
            });
            DrawToggleSection(fakeLightMode, "_FAKELIGHT_ON", () =>
            {
                materialEditor.ShaderProperty(fakeLightColor, "Fake Light Color");
                materialEditor.ShaderProperty(fakeLightDirection, "Fake Light Direction");
            });
            EditorGUI.indentLevel--;
        }

        // Shading Model Foldout
        string shadingModelTitle = $"Shading Model: {(SurfaceType)surfaceType.floatValue}";
        showShadingModel = EditorGUILayout.Foldout(showShadingModel, shadingModelTitle, true, EditorStyles.foldout);
        if (showShadingModel)
        {
            EditorGUI.indentLevel++;
            switch ((SurfaceType)surfaceType.floatValue)
            {
                case SurfaceType.Standard:
                    DrawStandardToonSettings();
                    break;
                case SurfaceType.Metallic:
                    DrawMetallicToonSettings();
                    break;
                case SurfaceType.Foliage:
                    DrawFoliageSettings();
                    break;
            }
            EditorGUI.indentLevel--;
        }

        // Outline Foldout
        showOutline = EditorGUILayout.Foldout(showOutline, "Outline", true, EditorStyles.foldout);
        if (showOutline)
        {
            EditorGUI.indentLevel++;
            DrawOutlineSettings(material);
            EditorGUI.indentLevel--;
        }

        // Render States Foldout
        showRenderStates = EditorGUILayout.Foldout(showRenderStates, "Render States", true, EditorStyles.foldout);
        if (showRenderStates)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(cullMode, "Culling Mode");
            EditorGUI.indentLevel--;
        }

        // Advanced Foldout
        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced", true, EditorStyles.foldout);
        if (showAdvanced)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(ambientColor, "Ambient Color");
            EditorGUI.indentLevel--;
        }
    }

    private enum SurfaceType { Standard, Metallic, Foliage }

    private void DrawSurfaceType(Material material)
    {
        EditorGUILayout.LabelField("Main", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        var type = (SurfaceType)surfaceType.floatValue;
        type = (SurfaceType)EditorGUILayout.EnumPopup("Surface Type", type);
        if (EditorGUI.EndChangeCheck())
        {
            surfaceType.floatValue = (float)type;
            SetKeyword(material, "_SURFACETYPE_OPAQUE", type == SurfaceType.Standard);
            SetKeyword(material, "_SURFACETYPE_METALLIC", type == SurfaceType.Metallic);
            SetKeyword(material, "_SURFACETYPE_FOLIAGE", type == SurfaceType.Foliage);
        }
    }

    private void DrawStandardToonSettings()
    {
        materialEditor.ShaderProperty(highlightThreshold, "Highlight Threshold");
        materialEditor.ShaderProperty(midtoneThreshold, "Midtone Threshold");
        materialEditor.ShaderProperty(shadowThreshold, "Shadow Threshold");
        materialEditor.ShaderProperty(rampSmoothness, "Ramp Smoothness");
        materialEditor.ShaderProperty(highlightColor, "Highlight Color");
        materialEditor.ShaderProperty(midtoneColor, "Midtone Color");
        materialEditor.ShaderProperty(shadowColor, "Shadow Color");
    }

    private void DrawMetallicToonSettings()
    {
        materialEditor.ShaderProperty(metallicHotSpotThreshold, "Hot Spot Threshold");
        materialEditor.ShaderProperty(metallicSpecularThreshold, "Specular Threshold");
        materialEditor.ShaderProperty(metallicReflectionThreshold, "Reflection Threshold");
        materialEditor.ShaderProperty(metallicRampSmoothness, "Ramp Smoothness");
        materialEditor.ShaderProperty(metallicBaseColor, "Base Color");
        materialEditor.ShaderProperty(metallicReflectionColor, "Reflection Color");
        materialEditor.ShaderProperty(metallicSpecularColor, "Specular Color");
        materialEditor.ShaderProperty(metallicHotSpotColor, "Hot Spot Color");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rim Light", EditorStyles.miniBoldLabel);
        materialEditor.ShaderProperty(rimColor, "Rim Color");
        materialEditor.ShaderProperty(rimPower, "Rim Power");
    }

    private void DrawFoliageSettings()
    {
        materialEditor.ShaderProperty(windFrequency, "Wind Frequency");
        materialEditor.ShaderProperty(windAmplitude, "Wind Amplitude");
        materialEditor.ShaderProperty(windDirection, "Wind Direction");
        materialEditor.ShaderProperty(translucencyColor, "Translucency Color");
        materialEditor.ShaderProperty(translucencyStrength, "Translucency Strength");
    }

    private void DrawOutlineSettings(Material material)
    {
        DrawToggleSection(fresnelOutlineToggle, "_OUTLINEMODE_FRESNEL", () =>
        {
            materialEditor.ShaderProperty(fresnelOutlineColor, "Color");
            materialEditor.ShaderProperty(fresnelOutlineWidth, "Width");
            materialEditor.ShaderProperty(fresnelOutlinePower, "Power");
            materialEditor.ShaderProperty(fresnelOutlineSharpness, "Sharpness");

            EditorGUILayout.Space();
            DrawToggleSection(glintToggle, "_OUTLINEGLINT_ON", () =>
            {
                materialEditor.ShaderProperty(glintColor, "Glint Color");
                materialEditor.ShaderProperty(glintScale, "Glint Scale");
                materialEditor.ShaderProperty(glintSpeed, "Glint Speed");
                materialEditor.ShaderProperty(glintThreshold, "Glint Threshold");
            }, false);
        }, false);
    }

    private void DrawToggleSection(MaterialProperty toggle, string keyword, Action onEnabled, bool addSpace = true)
    {
        if (addSpace) EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        materialEditor.ShaderProperty(toggle, toggle.displayName);
        bool enabled = toggle.floatValue > 0.5f;

        if (EditorGUI.EndChangeCheck())
        {
            SetKeyword(materialEditor.target as Material, keyword, enabled);
        }

        if (enabled)
        {
            EditorGUI.indentLevel++;
            onEnabled?.Invoke();
            EditorGUI.indentLevel--;
        }
    }

    private void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (enabled) material.EnableKeyword(keyword);
        else material.DisableKeyword(keyword);
    }
}
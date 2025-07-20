using UnityEngine;
using UnityEditor;

public class ToonOpaqueShaderGUI : ToonUberShaderGUIBase
{
    private static bool showBaseSettings = true;
    private static bool showLightingSettings = true;
    private static bool showFresnelOutlineSettings = true;

    private MaterialProperty surfaceTypeProp;
    private MaterialProperty baseMapProp, baseColorProp;
    private MaterialProperty alphaClipModeProp, cutoffProp;
    private MaterialProperty emissionModeProp, emissionColorProp, emissionMapProp;
    private MaterialProperty fakeLightModeProp, fakeLightColorProp, fakeLightDirectionProp;
    private MaterialProperty toonRampOffsetProp, toonRampSmoothnessProp, shadowTintProp, ambientColorProp; // <-- THÊM MỚI
    private MaterialProperty rampProp, brightnessProp, offsetProp, specuColorProp, highlightOffsetProp, hiColorProp, rimColorProp, rimPowerProp;
    private MaterialProperty windFrequencyProp, windAmplitudeProp, windDirectionProp, translucencyColorProp, translucencyStrengthProp;

    private MaterialProperty fresnelOutlineToggleProp, fresnelOutlineColorProp, fresnelOutlineWidthProp, fresnelOutlinePowerProp, fresnelOutlineSharpnessProp;
    private MaterialProperty glintToggleProp, glintColorProp, glintScaleProp, glintSpeedProp, glintThresholdProp;


    protected override void FindProperties()
    {
        surfaceTypeProp = FindProperty("_SurfaceType", properties);
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
        ambientColorProp = FindProperty("_AmbientColor", properties); // <-- THÊM MỚI
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

        fresnelOutlineToggleProp = FindProperty("_FresnelOutlineToggle", properties);
        fresnelOutlineColorProp = FindProperty("_FresnelOutlineColor", properties);
        fresnelOutlineWidthProp = FindProperty("_FresnelOutlineWidth", properties);
        fresnelOutlinePowerProp = FindProperty("_FresnelOutlinePower", properties);
        fresnelOutlineSharpnessProp = FindProperty("_FresnelOutlineSharpness", properties);

        glintToggleProp = FindProperty("_GlintToggle", properties);
        glintColorProp = FindProperty("_GlintColor", properties);
        glintScaleProp = FindProperty("_GlintScale", properties);
        glintSpeedProp = FindProperty("_GlintSpeed", properties);
        glintThresholdProp = FindProperty("_GlintThreshold", properties);
    }

    private void DrawSurfaceTypeSelector()
    {
        var currentSurfaceType = (ToonOpaqueDrawerUtils.SurfaceType)surfaceTypeProp.floatValue;
        var newSurfaceType = (ToonOpaqueDrawerUtils.SurfaceType)EditorGUILayout.EnumPopup("Surface Type", currentSurfaceType);

        if (newSurfaceType != currentSurfaceType)
        {
            surfaceTypeProp.floatValue = (float)newSurfaceType;
        }
    }

    protected override void DrawWorkflowSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Workflow", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select a Surface Type to unlock its specific settings. Use the buttons below to manage outlines.", MessageType.Info);

        DrawSurfaceTypeSelector();

        EditorGUILayout.LabelField("Outline Mode", "None / Fresnel");
        if (GUILayout.Button("Add Inverted Hull Outline (Switch Shader)"))
        {
            if (EditorUtility.DisplayDialog("Switch Shader?", "This will switch to the 'Opaque (Hull Outline)' shader. Are you sure?", "Yes", "No"))
            {
                SwitchShader("Bill's Toon/Opaque (Hull Outline)");
            }
        }
        EditorGUILayout.EndVertical();
    }

    protected override void DrawMainProperties()
    {
        DrawFoldout("Base Properties", ref showBaseSettings, () =>
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(baseMapProp.displayName), baseMapProp, baseColorProp);

            materialEditor.ShaderProperty(alphaClipModeProp, alphaClipModeProp.displayName);
            if (alphaClipModeProp.floatValue > 0)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(cutoffProp, cutoffProp.displayName);
                EditorGUI.indentLevel--;
            }

            materialEditor.ShaderProperty(emissionModeProp, emissionModeProp.displayName);
            if (emissionModeProp.floatValue > 0)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(emissionColorProp, "Emission Color");
                materialEditor.TexturePropertySingleLine(new GUIContent(emissionMapProp.displayName), emissionMapProp);
                EditorGUI.indentLevel--;
            }
        });

        DrawFoldout("Lighting", ref showLightingSettings, () =>
        {
            materialEditor.ShaderProperty(fakeLightModeProp, fakeLightModeProp.displayName);
            if (fakeLightModeProp.floatValue > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox("Fake Light acts as a fallback when no main Directional Light is present, ensuring the object is never completely black.", MessageType.Info);
                materialEditor.ShaderProperty(fakeLightColorProp, "Color");
                materialEditor.ShaderProperty(fakeLightDirectionProp, "Direction");
                EditorGUI.indentLevel--;
            }
        });

        var surface = (ToonOpaqueDrawerUtils.SurfaceType)surfaceTypeProp.floatValue;
        switch (surface)
        {
            case ToonOpaqueDrawerUtils.SurfaceType.Opaque:
                ToonOpaqueDrawerUtils.DrawToonSettings(materialEditor, toonRampOffsetProp, toonRampSmoothnessProp, shadowTintProp);
                EditorGUILayout.Space(); // <-- THÊM MỚI
                materialEditor.ColorProperty(ambientColorProp, "Ambient Color"); // <-- THÊM MỚI
                EditorGUILayout.HelpBox("Use the Alpha channel to blend between Scene Ambient (A=0) and this custom color (A=1).", MessageType.Info); // <-- THÊM MỚI
                break;
            case ToonOpaqueDrawerUtils.SurfaceType.Metallic:
                ToonOpaqueDrawerUtils.DrawMetallicSettings(materialEditor, rampProp, brightnessProp, offsetProp, specuColorProp, highlightOffsetProp, hiColorProp, rimColorProp, rimPowerProp);
                break;
            case ToonOpaqueDrawerUtils.SurfaceType.Foliage:
                ToonOpaqueDrawerUtils.DrawFoliageSettings(materialEditor, windFrequencyProp, windAmplitudeProp, windDirectionProp, translucencyColorProp, translucencyStrengthProp);
                break;
        }

        DrawFoldout("Fresnel Outline", ref showFresnelOutlineSettings, () =>
        {
            materialEditor.ShaderProperty(fresnelOutlineToggleProp, fresnelOutlineToggleProp.displayName);
            if (fresnelOutlineToggleProp.floatValue > 0)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(fresnelOutlineColorProp, "Color");
                materialEditor.ShaderProperty(fresnelOutlineWidthProp, "Width");
                materialEditor.ShaderProperty(fresnelOutlinePowerProp, "Power");
                materialEditor.ShaderProperty(fresnelOutlineSharpnessProp, "Sharpness");

                EditorGUILayout.Space();

                materialEditor.ShaderProperty(glintToggleProp, glintToggleProp.displayName);
                if (glintToggleProp.floatValue > 0)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.ShaderProperty(glintColorProp, "Glint Color");
                    materialEditor.ShaderProperty(glintScaleProp, "Glint Scale");
                    materialEditor.ShaderProperty(glintSpeedProp, "Glint Speed");
                    materialEditor.ShaderProperty(glintThresholdProp, "Glint Threshold");
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
        });
    }

    protected override void ApplyKeywords()
    {
        var surface = (ToonOpaqueDrawerUtils.SurfaceType)surfaceTypeProp.floatValue;
        SetKeyword("_SURFACETYPE_OPAQUE", surface == ToonOpaqueDrawerUtils.SurfaceType.Opaque);
        SetKeyword("_SURFACETYPE_METALLIC", surface == ToonOpaqueDrawerUtils.SurfaceType.Metallic);
        SetKeyword("_SURFACETYPE_FOLIAGE", surface == ToonOpaqueDrawerUtils.SurfaceType.Foliage);
        EditorUtility.SetDirty(material);
    }
}
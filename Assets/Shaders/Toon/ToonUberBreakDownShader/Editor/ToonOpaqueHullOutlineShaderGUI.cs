using UnityEngine;
using UnityEditor;

public class ToonOpaqueHullOutlineShaderGUI : ToonUberShaderGUIBase
{
    private static bool showBaseSettings = true;
    private static bool showLightingSettings = true;
    private static bool showHullOutlineSettings = true;

    private MaterialProperty surfaceTypeProp;
    private MaterialProperty baseMapProp, baseColorProp;
    private MaterialProperty alphaClipModeProp, cutoffProp;
    private MaterialProperty emissionModeProp, emissionColorProp, emissionMapProp;
    private MaterialProperty fakeLightModeProp, fakeLightColorProp, fakeLightDirectionProp;
    private MaterialProperty toonRampOffsetProp, toonRampSmoothnessProp, shadowTintProp, ambientColorProp; // <-- THÊM MỚI
    private MaterialProperty rampProp, brightnessProp, offsetProp, specuColorProp, highlightOffsetProp, hiColorProp, rimColorProp, rimPowerProp;
    private MaterialProperty windFrequencyProp, windAmplitudeProp, windDirectionProp, translucencyColorProp, translucencyStrengthProp;
    private MaterialProperty outlineColorProp, outlineWidthProp, outlineScaleWithDistanceProp, distanceFadeStartProp, distanceFadeEndProp;

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
        outlineColorProp = FindProperty("_OutlineColor", properties);
        outlineWidthProp = FindProperty("_OutlineWidth", properties);
        outlineScaleWithDistanceProp = FindProperty("_OutlineScaleWithDistance", properties);
        distanceFadeStartProp = FindProperty("_DistanceFadeStart", properties);
        distanceFadeEndProp = FindProperty("_DistanceFadeEnd", properties);
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

        DrawSurfaceTypeSelector();

        EditorGUILayout.LabelField("Outline Mode", "Inverted Hull");
        if (GUILayout.Button("Remove Outline (Switch to Standard Opaque)"))
        {
            if (EditorUtility.DisplayDialog("Switch Shader?", "This will switch to the standard Opaque shader and remove the outline. Are you sure?", "Yes", "No"))
            {
                SwitchShader("Bill's Toon/Opaque");
            }
        }
        EditorGUILayout.EndVertical();
    }

    protected override void DrawMainProperties()
    {
        DrawFoldout("Base Properties", ref showBaseSettings, () =>
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(baseMapProp.displayName), baseMapProp, baseColorProp);
            DrawPropertyGroup(alphaClipModeProp, "Enable Alpha Clip", () =>
            {
                materialEditor.ShaderProperty(cutoffProp, cutoffProp.displayName);
            });
            DrawPropertyGroup(emissionModeProp, "Enable Emission", () =>
            {
                materialEditor.ShaderProperty(emissionColorProp, "Emission Color");
                materialEditor.TexturePropertySingleLine(new GUIContent(emissionMapProp.displayName), emissionMapProp);
            });
        });

        DrawFoldout("Lighting", ref showLightingSettings, () =>
        {
            DrawPropertyGroup(fakeLightModeProp, "Enable Fake Light", () =>
            {
                EditorGUILayout.HelpBox("Fake Light acts as a fallback when no main Directional Light is present, ensuring the object is never completely black.", MessageType.Info);
                materialEditor.ShaderProperty(fakeLightColorProp, "Color");
                materialEditor.ShaderProperty(fakeLightDirectionProp, "Direction");
            });
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

        DrawFoldout("Inverted Hull Outline", ref showHullOutlineSettings, () =>
        {
            materialEditor.ShaderProperty(outlineColorProp, "Color");
            materialEditor.ShaderProperty(outlineWidthProp, "Width");
            materialEditor.ShaderProperty(outlineScaleWithDistanceProp, "Screen-Space Scaling");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("World-Space Distance Fade", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(distanceFadeStartProp, "Fade Start");
            materialEditor.ShaderProperty(distanceFadeEndProp, "Fade End");
            EditorGUI.indentLevel--;
        });
    }

    protected override void ApplyKeywords()
    {
        SetKeyword("_ALPHACLIP_ON", alphaClipModeProp.floatValue > 0);
        SetKeyword("_EMISSION_ON", emissionModeProp.floatValue > 0);
        SetKeyword("_FAKELIGHT_ON", fakeLightModeProp.floatValue > 0);

        var surface = (ToonOpaqueDrawerUtils.SurfaceType)surfaceTypeProp.floatValue;
        SetKeyword("_SURFACETYPE_OPAQUE", surface == ToonOpaqueDrawerUtils.SurfaceType.Opaque);
        SetKeyword("_SURFACETYPE_METALLIC", surface == ToonOpaqueDrawerUtils.SurfaceType.Metallic);
        SetKeyword("_SURFACETYPE_FOLIAGE", surface == ToonOpaqueDrawerUtils.SurfaceType.Foliage);

        SetKeyword("_OUTLINE_SCALE_WITH_DISTANCE", outlineScaleWithDistanceProp.floatValue > 0);

        EditorUtility.SetDirty(material);
    }
}
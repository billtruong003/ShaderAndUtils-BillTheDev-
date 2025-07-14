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
    private MaterialProperty toonRampOffsetProp, toonRampSmoothnessProp, shadowTintProp;
    private MaterialProperty rampProp, brightnessProp, offsetProp, specuColorProp, highlightOffsetProp, hiColorProp, rimColorProp, rimPowerProp;
    private MaterialProperty windFrequencyProp, windAmplitudeProp, windDirectionProp, translucencyColorProp, translucencyStrengthProp;
    private MaterialProperty fresnelOutlineToggleProp, fresnelOutlineColorProp, fresnelOutlineWidthProp, fresnelOutlinePowerProp;

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
            DrawPropertyGroup(fresnelOutlineToggleProp, "Enable Fresnel Outline", () =>
            {
                materialEditor.ShaderProperty(fresnelOutlineColorProp, "Color");
                materialEditor.ShaderProperty(fresnelOutlineWidthProp, "Width");
                materialEditor.ShaderProperty(fresnelOutlinePowerProp, "Power");
            });
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

        SetKeyword("_OUTLINEMODE_FRESNEL", fresnelOutlineToggleProp.floatValue > 0);

        EditorUtility.SetDirty(material);
    }
}
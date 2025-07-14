using UnityEngine;
using UnityEditor;

public class ToonTransparentShaderGUI : ToonUberShaderGUIBase
{
    private static bool showBaseSettings = true;
    private static bool showGlassSettings = true;
    private static bool showLightingSettings = true;
    private static bool showOutlineSettings = true;

    private MaterialProperty baseMapProp, baseColorProp;
    private MaterialProperty emissionModeProp, emissionColorProp, emissionMapProp;
    private MaterialProperty fakeLightModeProp, fakeLightColorProp, fakeLightDirectionProp;
    private MaterialProperty glassColorProp, fresnelColorProp, fresnelPowerProp;
    private MaterialProperty refractionStrengthProp, glassSpecularPowerProp, glassSpecularIntensityProp;
    private MaterialProperty fresnelOutlineToggleProp, fresnelOutlineColorProp, fresnelOutlineWidthProp, fresnelOutlinePowerProp;

    protected override void FindProperties()
    {
        baseMapProp = FindProperty("_BaseMap", properties);
        baseColorProp = FindProperty("_BaseColor", properties);
        emissionModeProp = FindProperty("_EmissionMode", properties);
        emissionColorProp = FindProperty("_EmissionColor", properties);
        emissionMapProp = FindProperty("_EmissionMap", properties);
        fakeLightModeProp = FindProperty("_FakeLightMode", properties, false);
        fakeLightColorProp = FindProperty("_FakeLightColor", properties, false);
        fakeLightDirectionProp = FindProperty("_FakeLightDirection", properties, false);
        glassColorProp = FindProperty("_GlassColor", properties);
        fresnelColorProp = FindProperty("_FresnelColor", properties);
        fresnelPowerProp = FindProperty("_FresnelPower", properties);
        refractionStrengthProp = FindProperty("_RefractionStrength", properties);
        glassSpecularPowerProp = FindProperty("_GlassSpecularPower", properties);
        glassSpecularIntensityProp = FindProperty("_GlassSpecularIntensity", properties);
        fresnelOutlineToggleProp = FindProperty("_FresnelOutlineToggle", properties);
        fresnelOutlineColorProp = FindProperty("_FresnelOutlineColor", properties);
        fresnelOutlineWidthProp = FindProperty("_FresnelOutlineWidth", properties);
        fresnelOutlinePowerProp = FindProperty("_FresnelOutlinePower", properties);
    }

    protected override void DrawWorkflowSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Workflow: Stylized Glass", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("This shader simulates a stylized transparent glass effect with refraction and fresnel.", MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    protected override void DrawMainProperties()
    {
        DrawFoldout("Base Properties", ref showBaseSettings, () =>
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(baseMapProp.displayName), baseMapProp, baseColorProp);
            DrawPropertyGroup(emissionModeProp, "Enable Emission", () =>
            {
                materialEditor.ShaderProperty(emissionColorProp, "Emission Color");
                materialEditor.TexturePropertySingleLine(new GUIContent(emissionMapProp.displayName), emissionMapProp);
            });
        });

        DrawFoldout("Lighting", ref showLightingSettings, () =>
        {
            if (fakeLightModeProp != null)
            {
                DrawPropertyGroup(fakeLightModeProp, "Enable Fake Light", () =>
                {
                    EditorGUILayout.HelpBox("Fake Light acts as a fallback when no main Directional Light is present, ensuring the object is never completely black.", MessageType.Info);
                    materialEditor.ShaderProperty(fakeLightColorProp, "Color");
                    materialEditor.ShaderProperty(fakeLightDirectionProp, "Direction");
                });
            }
            else
            {
                EditorGUILayout.HelpBox("Fake Light is not available for this Transparent shader.", MessageType.Warning);
            }
        });

        DrawFoldout("Stylized Glass", ref showGlassSettings, () =>
        {
            materialEditor.ShaderProperty(glassColorProp, "Glass Color & Opacity");
            materialEditor.ShaderProperty(refractionStrengthProp, "Refraction Strength");
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Fresnel (Edge Effect)", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(fresnelColorProp, "Edge Color");
            materialEditor.ShaderProperty(fresnelPowerProp, "Edge Power");
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Specular", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(glassSpecularPowerProp, "Power");
            materialEditor.ShaderProperty(glassSpecularIntensityProp, "Intensity");
        });

        DrawFoldout("Fresnel Outline", ref showOutlineSettings, () =>
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
        SetKeyword("_EMISSION_ON", emissionModeProp.floatValue > 0);
        if (fakeLightModeProp != null)
        {
            SetKeyword("_FAKELIGHT_ON", fakeLightModeProp.floatValue > 0);
        }
        SetKeyword("_OUTLINEMODE_FRESNEL", fresnelOutlineToggleProp.floatValue > 0);
        EditorUtility.SetDirty(material);
    }
}
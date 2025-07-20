using UnityEngine;
using UnityEditor;
using System;

public class ToonBlingMetallicShaderGUI : ToonUberShaderGUIBase
{
    private static bool showBaseSettings = true;
    private static bool showToonShadingSettings = true;
    private static bool showSpecularSettings = true;
    private static bool showRimLightSettings = true;
    private static bool showBlingEffectSettings = true;

    // Base Properties
    private MaterialProperty baseMapProp, baseColorProp;
    private MaterialProperty alphaClipModeProp, cutoffProp;
    private MaterialProperty emissionModeProp, emissionColorProp, emissionMapProp;

    // Toon Shading
    private MaterialProperty toonRampOffsetProp, toonRampSmoothnessProp, shadowTintProp;

    // Toon Specular
    private MaterialProperty specColorProp, specSmoothnessProp, specOffsetProp;

    // Rim Lighting
    private MaterialProperty rimColorProp, rimPowerProp, rimMinProp, rimMaxProp;

    // Bling Effect
    private MaterialProperty blingWorldSpaceProp, blingColorProp, blingIntensityProp, blingScaleProp, blingSpeedProp, blingFresnelPowerProp, blingThresholdProp;

    protected override void FindProperties()
    {
        // Base
        baseMapProp = FindProperty("_BaseMap", properties);
        baseColorProp = FindProperty("_BaseColor", properties);
        alphaClipModeProp = FindProperty("_AlphaClipMode", properties);
        cutoffProp = FindProperty("_Cutoff", properties);
        emissionModeProp = FindProperty("_EmissionMode", properties);
        emissionColorProp = FindProperty("_EmissionColor", properties);
        emissionMapProp = FindProperty("_EmissionMap", properties);

        // Toon Shading
        toonRampOffsetProp = FindProperty("_ToonRampOffset", properties);
        toonRampSmoothnessProp = FindProperty("_ToonRampSmoothness", properties);
        shadowTintProp = FindProperty("_ShadowTint", properties);

        // Specular
        specColorProp = FindProperty("_SpecColor", properties);
        specSmoothnessProp = FindProperty("_SpecSmoothness", properties);
        specOffsetProp = FindProperty("_SpecOffset", properties);

        // Rim
        rimColorProp = FindProperty("_RimColor", properties);
        rimPowerProp = FindProperty("_RimPower", properties);
        rimMinProp = FindProperty("_RimMin", properties);
        rimMaxProp = FindProperty("_RimMax", properties);

        // Bling
        blingWorldSpaceProp = FindProperty("_BlingWorldSpace", properties);
        blingColorProp = FindProperty("_BlingColor", properties);
        blingIntensityProp = FindProperty("_BlingIntensity", properties);
        blingScaleProp = FindProperty("_BlingScale", properties);
        blingSpeedProp = FindProperty("_BlingSpeed", properties);
        blingFresnelPowerProp = FindProperty("_BlingFresnelPower", properties);
        blingThresholdProp = FindProperty("_BlingThreshold", properties);
    }

    protected override void DrawWorkflowSettings()
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Workflow: Enhanced Toon (Bling)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Shader toon nâng cao, tích hợp Specular, Rim Lighting và hiệu ứng Bling động.", MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    protected override void DrawMainProperties()
    {
        DrawFoldout("Base Properties", ref showBaseSettings, () =>
        {
            materialEditor.TexturePropertySingleLine(new GUIContent(baseMapProp.displayName, "Albedo (RGB) and Alpha (A)"), baseMapProp, baseColorProp);

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

        DrawFoldout("Toon Shading", ref showToonShadingSettings, () =>
        {
            materialEditor.ShaderProperty(toonRampOffsetProp, "Ramp Offset");
            materialEditor.ShaderProperty(toonRampSmoothnessProp, "Ramp Smoothness");
            materialEditor.ShaderProperty(shadowTintProp, "Shadow Tint");
        });

        DrawFoldout("Toon Specular", ref showSpecularSettings, () =>
        {
            materialEditor.ShaderProperty(specColorProp, "Color");
            materialEditor.ShaderProperty(specOffsetProp, "Offset");
            materialEditor.ShaderProperty(specSmoothnessProp, "Smoothness");
        });

        DrawFoldout("Rim Lighting", ref showRimLightSettings, () =>
        {
            materialEditor.ShaderProperty(rimColorProp, "Color");
            materialEditor.ShaderProperty(rimPowerProp, "Power");
            materialEditor.ShaderProperty(rimMinProp, "Min");
            materialEditor.ShaderProperty(rimMaxProp, "Max");
        });

        DrawFoldout("Bling Effect", ref showBlingEffectSettings, () =>
        {
            materialEditor.ShaderProperty(blingWorldSpaceProp, "Use World Space");
            materialEditor.ShaderProperty(blingColorProp, "Color");
            materialEditor.ShaderProperty(blingIntensityProp, "Intensity");
            materialEditor.ShaderProperty(blingScaleProp, "Scale");
            materialEditor.ShaderProperty(blingSpeedProp, "Speed");
            materialEditor.ShaderProperty(blingFresnelPowerProp, "Fresnel Power");
            materialEditor.ShaderProperty(blingThresholdProp, "Threshold");
        });
    }

    protected override void ApplyKeywords()
    {
        SetKeyword("_ALPHACLIP_ON", alphaClipModeProp.floatValue > 0);
        SetKeyword("_EMISSION_ON", emissionModeProp.floatValue > 0);
        SetKeyword("_BLING_WORLDSPACE_ON", blingWorldSpaceProp.floatValue > 0);
    }

    // Giả định rằng bạn có một lớp cơ sở như thế này
    public abstract class ToonUberShaderGUIBase : ShaderGUI
    {
        protected MaterialEditor materialEditor;
        protected MaterialProperty[] properties;

        public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
        {
            materialEditor = editor;
            properties = props;
            FindProperties();

            DrawWorkflowSettings();
            EditorGUILayout.Space();
            DrawMainProperties();

            EditorGUILayout.Space();
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            base.AssignNewShaderToMaterial(material, oldShader, newShader);
            if (material != null)
            {
                SetMaterialKeywords(material);
            }
        }

        protected abstract void FindProperties();
        protected abstract void DrawWorkflowSettings();
        protected abstract void DrawMainProperties();
        protected abstract void ApplyKeywords();

        protected void SetMaterialKeywords(Material material)
        {
            if (material == null) return;
            ApplyKeywords();
        }

        protected void DrawFoldout(string title, ref bool foldoutState, Action contents)
        {
            foldoutState = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutState, title);
            if (foldoutState)
            {
                EditorGUI.indentLevel++;
                contents();
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        protected void DrawPropertyGroup(MaterialProperty toggleProp, string title, Action contents)
        {
            bool toggle = toggleProp.floatValue > 0;
            EditorGUI.BeginChangeCheck();
            toggle = EditorGUILayout.Toggle(title, toggle);
            if (EditorGUI.EndChangeCheck())
            {
                toggleProp.floatValue = toggle ? 1.0f : 0.0f;
            }

            if (toggle)
            {
                EditorGUI.indentLevel++;
                contents();
                EditorGUI.indentLevel--;
            }
        }

        protected void SetKeyword(string keyword, bool enabled)
        {
            foreach (Material mat in materialEditor.targets)
            {
                if (enabled)
                {
                    mat.EnableKeyword(keyword);
                }
                else
                {
                    mat.DisableKeyword(keyword);
                }
            }
        }
    }
}
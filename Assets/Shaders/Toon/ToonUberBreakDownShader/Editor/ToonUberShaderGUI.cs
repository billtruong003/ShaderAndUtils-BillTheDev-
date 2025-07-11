using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Linq;

public class ToonUberShaderSeparateGUI : ShaderGUI
{
    private enum SurfaceType { Opaque, Transparent, Metallic, Foliage }
    private enum OutlineMode { None, InvertedHull, Fresnel }
    private enum BooleanOnOff { Off, On }

    private MaterialEditor materialEditor;
    private Material targetMaterial;

    private static class Styles
    {
        public static readonly GUIStyle HeaderStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14, padding = new RectOffset(0, 0, 4, 4) };
        public static readonly GUIStyle FoldoutHeaderStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold, fontSize = 12 };

        public static readonly GUIContent ShaderName = new GUIContent("Toon Uber Shader (Optimized)");
        public static readonly GUIContent BasePropertiesHeader = new GUIContent("Base Properties");
        public static readonly GUIContent LightingHeader = new GUIContent("Lighting");
        public static readonly GUIContent EmissionHeader = new GUIContent("Emission");
        public static readonly GUIContent OutlineHeader = new GUIContent("Outline");

        public static readonly GUIContent BaseMap = new GUIContent("Base Map", "Albedo (RGB) and Alpha (A)");
        public static readonly GUIContent AlphaClipToggle = new GUIContent("Enable Alpha Clip");
        public static readonly GUIContent FakeLightToggle = new GUIContent("Enable Fake Light", "Overrides scene light when no directional light is present.");
        public static readonly GUIContent ColorLabel = new GUIContent("Color");
        public static readonly GUIContent DirectionLabel = new GUIContent("Direction");
        public static readonly GUIContent WindMaskInfo = new GUIContent("Wind is masked by Vertex Color Alpha. Paint vertex alpha to control wind strength.");

        public static readonly GUIContent FresnelOutlineDisabledInHullMode = new GUIContent("Fresnel Outline is disabled when Inverted Hull is active.");
        public static readonly GUIContent HullOutlineDisabledInTransparentMode = new GUIContent("Inverted Hull Outline is not available for Transparent surfaces.");
    }

    private Dictionary<string, MaterialProperty> properties = new Dictionary<string, MaterialProperty>();

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
    {
        materialEditor = editor;
        targetMaterial = editor.target as Material;

        properties = props.ToDictionary(prop => prop.name, prop => prop);

        DrawHeader();

        EditorGUI.BeginChangeCheck();

        DrawPrimarySelectors();

        SurfaceType surfaceType = (SurfaceType)properties["_SurfaceType"].floatValue;

        DrawSection(Styles.BasePropertiesHeader, () => DrawBaseProperties(surfaceType));
        DrawSection(Styles.LightingHeader, DrawLightingProperties);
        DrawSection(new GUIContent(surfaceType + " Properties"), () => DrawSurfaceSpecificProperties(surfaceType));
        DrawSection(Styles.EmissionHeader, DrawEmissionProperties);
        DrawSection(Styles.OutlineHeader, DrawOutlineProperties);

        if (EditorGUI.EndChangeCheck())
        {
            UpdateMaterialState();
        }
    }

    private void DrawHeader()
    {
        Rect headerRect = EditorGUILayout.GetControlRect(false, 24);
        EditorGUI.DrawRect(headerRect, new Color(0.25f, 0.25f, 0.25f, 1f));
        EditorGUI.LabelField(headerRect, Styles.ShaderName, Styles.HeaderStyle);
        EditorGUILayout.Space(4);
    }

    private void DrawPrimarySelectors()
    {
        materialEditor.ShaderProperty(properties["_SurfaceType"], "Surface Type");

        var outlineModeProp = properties["_OutlineMode"];
        var surfaceType = (SurfaceType)properties["_SurfaceType"].floatValue;

        EditorGUI.BeginDisabledGroup(surfaceType == SurfaceType.Transparent);
        materialEditor.ShaderProperty(outlineModeProp, "Outline Mode");
        if (surfaceType == SurfaceType.Transparent)
        {
            EditorGUILayout.HelpBox(Styles.HullOutlineDisabledInTransparentMode.text, MessageType.Info);
        }
        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space();
    }

    private void DrawSection(GUIContent header, System.Action contentDrawer)
    {
        if (EditorGUILayout.BeginFoldoutHeaderGroup(true, header, Styles.FoldoutHeaderStyle))
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space(2);
            contentDrawer();
            EditorGUILayout.Space(4);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(2);
    }

    private void DrawBaseProperties(SurfaceType surfaceType)
    {
        materialEditor.TexturePropertySingleLine(Styles.BaseMap, properties["_BaseMap"], properties["_BaseColor"]);

        if (surfaceType != SurfaceType.Transparent)
        {
            materialEditor.ShaderProperty(properties["_AlphaClipMode"], Styles.AlphaClipToggle);
            if (properties["_AlphaClipMode"].floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                materialEditor.ShaderProperty(properties["_Cutoff"], "Alpha Cutoff");
                EditorGUI.indentLevel--;
            }
        }
    }

    private void DrawLightingProperties()
    {
        materialEditor.ShaderProperty(properties["_FakeLightMode"], Styles.FakeLightToggle);
        if (properties["_FakeLightMode"].floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(properties["_FakeLightColor"], Styles.ColorLabel);
            materialEditor.ShaderProperty(properties["_FakeLightDirection"], Styles.DirectionLabel);
            EditorGUI.indentLevel--;
        }
    }

    private void DrawSurfaceSpecificProperties(SurfaceType surfaceType)
    {
        switch (surfaceType)
        {
            case SurfaceType.Opaque:
                DrawProperty("_ToonRampOffset", "Ramp Offset");
                DrawProperty("_ToonRampSmoothness", "Ramp Smoothness");
                DrawProperty("_ShadowTint", "Shadow Tint");
                break;
            case SurfaceType.Metallic:
                DrawProperty("_Ramp", "Toon Ramp (RGB)");
                EditorGUILayout.LabelField("Specular Layer", EditorStyles.miniBoldLabel);
                DrawProperty("_Brightness", "Brightness");
                DrawProperty("_Offset", "Size");
                DrawProperty("_SpecuColor");
                EditorGUILayout.LabelField("Highlight Layer", EditorStyles.miniBoldLabel);
                DrawProperty("_HighlightOffset", "Size");
                DrawProperty("_HiColor");
                EditorGUILayout.LabelField("Rim Light", EditorStyles.miniBoldLabel);
                DrawProperty("_RimColor");
                DrawProperty("_RimPower", "Power");
                break;
            case SurfaceType.Foliage:
                EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(Styles.WindMaskInfo.text, MessageType.Info);
                DrawProperty("_WindFrequency", "Frequency");
                DrawProperty("_WindAmplitude", "Amplitude");
                DrawProperty("_WindDirection", "Direction");
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Translucency", EditorStyles.boldLabel);
                DrawProperty("_TranslucencyColor");
                DrawProperty("_TranslucencyStrength", "Strength");
                break;
            case SurfaceType.Transparent:
                DrawProperty("_GlassColor", "Glass Color & Opacity");
                DrawProperty("_FresnelColor", "Fresnel (Edge) Color");
                DrawProperty("_FresnelPower", "Fresnel Power");
                DrawProperty("_RefractionStrength", "Refraction Strength");
                DrawProperty("_GlassSpecularPower", "Specular Power");
                DrawProperty("_GlassSpecularIntensity", "Specular Intensity");
                break;
        }
    }

    private void DrawEmissionProperties()
    {
        DrawProperty("_EmissionMode", "Enable Emission");
        if (properties["_EmissionMode"].floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            materialEditor.TexturePropertySingleLine(Styles.ColorLabel, properties["_EmissionMap"], properties["_EmissionColor"]);
            EditorGUI.indentLevel--;
        }
    }

    private void DrawOutlineProperties()
    {
        OutlineMode outlineMode = (OutlineMode)properties["_OutlineMode"].floatValue;
        SurfaceType surfaceType = (SurfaceType)properties["_SurfaceType"].floatValue;

        if (surfaceType == SurfaceType.Transparent)
        {
            // Fresnel Outline is still available for Transparent
            DrawProperty("_FresnelOutlineToggle", "Enable Fresnel Outline");
            if (properties.ContainsKey("_FresnelOutlineToggle") && properties["_FresnelOutlineToggle"].floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                DrawProperty("_FresnelOutlineColor");
                DrawProperty("_FresnelOutlineWidth", "Width");
                DrawProperty("_FresnelOutlinePower", "Power");
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            // Opaque-like surfaces can have either
            switch (outlineMode)
            {
                case OutlineMode.InvertedHull:
                    DrawProperty("_OutlineColor");
                    DrawProperty("_OutlineWidth", "Width");
                    DrawProperty("_OutlineScaleWithDistance", "Scale With Distance");
                    DrawProperty("_DistanceFadeStart", "Distance Fade Start");
                    DrawProperty("_DistanceFadeEnd", "Distance Fade End");
                    break;
                case OutlineMode.Fresnel:
                    DrawProperty("_FresnelOutlineColor");
                    DrawProperty("_FresnelOutlineWidth", "Width");
                    DrawProperty("_FresnelOutlinePower", "Power");
                    break;
            }
        }
    }

    private void DrawProperty(string name, string label = null)
    {
        if (properties.TryGetValue(name, out MaterialProperty prop))
        {
            materialEditor.ShaderProperty(prop, label ?? prop.displayName);
        }
    }

    private void SetKeyword(string keyword, bool enabled)
    {
        if (enabled) targetMaterial.EnableKeyword(keyword);
        else targetMaterial.DisableKeyword(keyword);
    }

    private void UpdateMaterialState()
    {
        SurfaceType surfaceType = (SurfaceType)properties["_SurfaceType"].floatValue;
        OutlineMode outlineMode = (OutlineMode)properties["_OutlineMode"].floatValue;

        // --- 1. Shader Swapping Logic ---
        string targetShaderName;
        if (surfaceType == SurfaceType.Transparent)
        {
            targetShaderName = "Custom/Toon Uber/Transparent";
            // Force disable hull outline for transparent
            if (outlineMode == OutlineMode.InvertedHull)
            {
                properties["_OutlineMode"].floatValue = (float)OutlineMode.None;
            }
        }
        else // Opaque, Metallic, Foliage
        {
            targetShaderName = outlineMode == OutlineMode.InvertedHull
                ? "Custom/Toon Uber/Opaque (Hull Outline)"
                : "Custom/Toon Uber/Opaque";
        }

        Shader newShader = Shader.Find(targetShaderName);
        if (newShader != null && targetMaterial.shader != newShader)
        {
            materialEditor.SetShader(newShader, false);
        }

        // --- 2. Set Keywords and Render State ---
        bool isOpaqueLike = surfaceType != SurfaceType.Transparent;
        if (isOpaqueLike)
        {
            if ((BooleanOnOff)properties["_AlphaClipMode"].floatValue == BooleanOnOff.On)
            {
                targetMaterial.SetOverrideTag("RenderType", "TransparentCutout");
                targetMaterial.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                targetMaterial.SetOverrideTag("RenderType", "Opaque");
                targetMaterial.renderQueue = (int)RenderQueue.Geometry;
            }
        }

        // Surface Type
        SetKeyword("_SURFACETYPE_OPAQUE", surfaceType == SurfaceType.Opaque);
        SetKeyword("_SURFACETYPE_METALLIC", surfaceType == SurfaceType.Metallic);
        SetKeyword("_SURFACETYPE_FOLIAGE", surfaceType == SurfaceType.Foliage);

        // Features
        SetKeyword("_ALPHACLIP_ON", isOpaqueLike && (BooleanOnOff)properties["_AlphaClipMode"].floatValue == BooleanOnOff.On);
        SetKeyword("_EMISSION_ON", (BooleanOnOff)properties["_EmissionMode"].floatValue == BooleanOnOff.On);
        SetKeyword("_FAKELIGHT_ON", (BooleanOnOff)properties["_FakeLightMode"].floatValue == BooleanOnOff.On);

        // Outline Features
        bool useFresnel = outlineMode == OutlineMode.Fresnel;
        if (properties.ContainsKey("_FresnelOutlineToggle")) // For transparent shader
        {
            useFresnel = useFresnel || (BooleanOnOff)properties["_FresnelOutlineToggle"].floatValue == BooleanOnOff.On;
        }

        SetKeyword("_OUTLINEMODE_FRESNEL", useFresnel);
        SetKeyword("_OUTLINE_SCALE_WITH_DISTANCE", outlineMode == OutlineMode.InvertedHull && (BooleanOnOff)properties["_OutlineScaleWithDistance"].floatValue == BooleanOnOff.On);
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);

        // Ensure the state is correct when the shader is first assigned
        targetMaterial = material;
        properties = MaterialEditor.GetMaterialProperties(new Material[] { material }).ToDictionary(prop => prop.name, prop => prop);
        UpdateMaterialState();
    }
}
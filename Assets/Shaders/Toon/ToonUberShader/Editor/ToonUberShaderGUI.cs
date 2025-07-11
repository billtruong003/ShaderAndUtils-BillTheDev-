using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ToonUberShaderGUI : ShaderGUI
{
    public enum SurfaceType { Opaque, Transparent, Metallic, Foliage }
    public enum OutlineMode { None, InvertedHull, Fresnel }
    public enum BooleanOnOff { Off, On }

    private MaterialEditor m_MaterialEditor;

    private bool basePropertiesFoldout = true;
    private bool lightingFoldout = true;
    private bool surfaceSpecificFoldout = true;
    private bool emissionFoldout = true;
    private bool outlineFoldout = true;

    private MaterialProperty surfaceTypeProp, outlineModeProp;
    private MaterialProperty baseMapProp, baseColorProp;
    private MaterialProperty alphaClipModeProp, cutoffProp;
    private MaterialProperty fakeLightModeProp, fakeLightColorProp, fakeLightDirectionProp;
    private MaterialProperty toonRampOffsetProp, toonRampSmoothnessProp, shadowTintProp;
    private MaterialProperty rampProp, brightnessProp, offsetProp, specuColorProp;
    private MaterialProperty highlightOffsetProp, hiColorProp, rimColorProp, rimPowerProp;
    private MaterialProperty glassColorProp, fresnelColorProp, fresnelPowerProp, refractionStrengthProp;
    private MaterialProperty glassSpecularPowerProp, glassSpecularIntensityProp;
    private MaterialProperty emissionModeProp, emissionColorProp, emissionMapProp;
    private MaterialProperty outlineColorProp, outlineWidthProp, outlineScaleWithDistanceProp, distanceFadeStartProp, distanceFadeEndProp;
    private MaterialProperty fresnelOutlineColorProp, fresnelOutlineWidthProp, fresnelOutlinePowerProp;
    private MaterialProperty windFrequencyProp, windAmplitudeProp, windDirectionProp, translucencyColorProp, translucencyStrengthProp;

    private static class Styles
    {
        public static readonly GUIStyle headerStyle;
        public static readonly GUIStyle foldoutHeaderStyle;

        public static readonly GUIContent shaderName = new GUIContent("Toon Uber Shader");
        public static readonly GUIContent basePropertiesHeader = new GUIContent("Base Properties");
        public static readonly GUIContent lightingHeader = new GUIContent("Lighting");
        public static readonly GUIContent emissionHeader = new GUIContent("Emission");
        public static readonly GUIContent outlineHeader = new GUIContent("Outline");

        public static readonly GUIContent baseMap = new GUIContent("Base Map", "Albedo (RGB) and Alpha (A)");
        public static readonly GUIContent alphaClipToggle = new GUIContent("Enable Alpha Clip");
        public static readonly GUIContent fakeLightToggle = new GUIContent("Enable Fake Light", "Overrides scene light when no directional light is present.");
        public static readonly GUIContent colorLabel = new GUIContent("Color");
        public static readonly GUIContent directionLabel = new GUIContent("Direction");
        public static readonly GUIContent windMaskInfo = new GUIContent("Wind is masked by Vertex Color Alpha (a). Paint vertex alpha to control wind strength.");

        static Styles()
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14, padding = new RectOffset(0, 0, 4, 4), normal = { textColor = new Color(0.85f, 0.85f, 0.85f) } };
            foldoutHeaderStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold, fontSize = 12 };
        }
    }

    private void FindProperties(MaterialProperty[] props)
    {
        surfaceTypeProp = FindProperty("_SurfaceType", props);
        outlineModeProp = FindProperty("_OutlineMode", props);
        baseMapProp = FindProperty("_BaseMap", props);
        baseColorProp = FindProperty("_BaseColor", props);
        alphaClipModeProp = FindProperty("_AlphaClipMode", props);
        cutoffProp = FindProperty("_Cutoff", props);
        fakeLightModeProp = FindProperty("_FakeLightMode", props);
        fakeLightColorProp = FindProperty("_FakeLightColor", props);
        fakeLightDirectionProp = FindProperty("_FakeLightDirection", props);
        toonRampOffsetProp = FindProperty("_ToonRampOffset", props);
        toonRampSmoothnessProp = FindProperty("_ToonRampSmoothness", props);
        shadowTintProp = FindProperty("_ShadowTint", props);
        rampProp = FindProperty("_Ramp", props);
        brightnessProp = FindProperty("_Brightness", props);
        offsetProp = FindProperty("_Offset", props);
        specuColorProp = FindProperty("_SpecuColor", props);
        highlightOffsetProp = FindProperty("_HighlightOffset", props);
        hiColorProp = FindProperty("_HiColor", props);
        rimColorProp = FindProperty("_RimColor", props);
        rimPowerProp = FindProperty("_RimPower", props);
        glassColorProp = FindProperty("_GlassColor", props);
        fresnelColorProp = FindProperty("_FresnelColor", props);
        fresnelPowerProp = FindProperty("_FresnelPower", props);
        refractionStrengthProp = FindProperty("_RefractionStrength", props);
        glassSpecularPowerProp = FindProperty("_GlassSpecularPower", props);
        glassSpecularIntensityProp = FindProperty("_GlassSpecularIntensity", props);
        emissionModeProp = FindProperty("_EmissionMode", props);
        emissionColorProp = FindProperty("_EmissionColor", props);
        emissionMapProp = FindProperty("_EmissionMap", props);
        outlineColorProp = FindProperty("_OutlineColor", props);
        outlineWidthProp = FindProperty("_OutlineWidth", props);
        outlineScaleWithDistanceProp = FindProperty("_OutlineScaleWithDistance", props);
        distanceFadeStartProp = FindProperty("_DistanceFadeStart", props);
        distanceFadeEndProp = FindProperty("_DistanceFadeEnd", props);
        fresnelOutlineColorProp = FindProperty("_FresnelOutlineColor", props);
        fresnelOutlineWidthProp = FindProperty("_FresnelOutlineWidth", props);
        fresnelOutlinePowerProp = FindProperty("_FresnelOutlinePower", props);
        windFrequencyProp = FindProperty("_WindFrequency", props);
        windAmplitudeProp = FindProperty("_WindAmplitude", props);
        windDirectionProp = FindProperty("_WindDirection", props);
        translucencyColorProp = FindProperty("_TranslucencyColor", props);
        translucencyStrengthProp = FindProperty("_TranslucencyStrength", props);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        this.m_MaterialEditor = materialEditor;
        FindProperties(props);

        Rect headerRect = EditorGUILayout.GetControlRect(false, 24);
        EditorGUI.DrawRect(headerRect, new Color(0.2f, 0.2f, 0.2f, 1f));
        EditorGUI.LabelField(headerRect, Styles.shaderName, Styles.headerStyle);
        EditorGUILayout.Space(4);

        EditorGUI.BeginChangeCheck();

        m_MaterialEditor.ShaderProperty(surfaceTypeProp, "Surface Type");
        EditorGUILayout.Space();

        SurfaceType surfaceType = (SurfaceType)surfaceTypeProp.floatValue;

        DrawBaseProperties(surfaceType);
        DrawLightingProperties();
        DrawSurfaceSpecificProperties(surfaceType);
        DrawEmissionProperties();
        DrawOutlineProperties();

        if (EditorGUI.EndChangeCheck())
        {
            foreach (var obj in m_MaterialEditor.targets)
            {
                SetupMaterialRenderingState((Material)obj);
            }
        }
    }

    private void DrawSection(ref bool foldout, GUIContent header, System.Action contentDrawer)
    {
        foldout = EditorGUILayout.Foldout(foldout, header, true, Styles.foldoutHeaderStyle);
        if (foldout)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.Space(2);
            contentDrawer();
            EditorGUILayout.Space(4);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(2);
    }

    private void DrawBaseProperties(SurfaceType surfaceType)
    {
        DrawSection(ref basePropertiesFoldout, Styles.basePropertiesHeader, () =>
        {
            m_MaterialEditor.TexturePropertySingleLine(Styles.baseMap, baseMapProp, baseColorProp);

            if (surfaceType != SurfaceType.Transparent)
            {
                m_MaterialEditor.ShaderProperty(alphaClipModeProp, Styles.alphaClipToggle);
                if (alphaClipModeProp.floatValue > 0.5f)
                {
                    EditorGUI.indentLevel++;
                    m_MaterialEditor.ShaderProperty(cutoffProp, "Alpha Cutoff");
                    EditorGUI.indentLevel--;
                }
            }
        });
    }

    private void DrawLightingProperties()
    {
        DrawSection(ref lightingFoldout, Styles.lightingHeader, () =>
        {
            m_MaterialEditor.ShaderProperty(fakeLightModeProp, Styles.fakeLightToggle);
            if (fakeLightModeProp.floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.ShaderProperty(fakeLightColorProp, Styles.colorLabel);
                m_MaterialEditor.ShaderProperty(fakeLightDirectionProp, Styles.directionLabel);
                EditorGUI.indentLevel--;
            }
        });
    }

    private void DrawSurfaceSpecificProperties(SurfaceType surfaceType)
    {
        string headerTitle = surfaceType.ToString() + " Properties";
        DrawSection(ref surfaceSpecificFoldout, new GUIContent(headerTitle), () =>
        {
            switch (surfaceType)
            {
                case SurfaceType.Opaque:
                    m_MaterialEditor.ShaderProperty(toonRampOffsetProp, "Ramp Offset");
                    m_MaterialEditor.ShaderProperty(toonRampSmoothnessProp, "Ramp Smoothness");
                    m_MaterialEditor.ShaderProperty(shadowTintProp, "Shadow Tint");
                    break;
                case SurfaceType.Metallic:
                    m_MaterialEditor.ShaderProperty(rampProp, "Toon Ramp (RGB)");
                    EditorGUILayout.LabelField("Specular Layer", EditorStyles.miniBoldLabel);
                    m_MaterialEditor.ShaderProperty(brightnessProp, "Brightness");
                    m_MaterialEditor.ShaderProperty(offsetProp, "Size");
                    m_MaterialEditor.ShaderProperty(specuColorProp, Styles.colorLabel);
                    EditorGUILayout.LabelField("Highlight Layer", EditorStyles.miniBoldLabel);
                    m_MaterialEditor.ShaderProperty(highlightOffsetProp, "Size");
                    m_MaterialEditor.ShaderProperty(hiColorProp, Styles.colorLabel);
                    EditorGUILayout.LabelField("Rim Light", EditorStyles.miniBoldLabel);
                    m_MaterialEditor.ShaderProperty(rimColorProp, Styles.colorLabel);
                    m_MaterialEditor.ShaderProperty(rimPowerProp, "Power");
                    break;
                case SurfaceType.Foliage:
                    EditorGUILayout.LabelField("Wind", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox(Styles.windMaskInfo.text, MessageType.Info);
                    m_MaterialEditor.ShaderProperty(windFrequencyProp, "Frequency");
                    m_MaterialEditor.ShaderProperty(windAmplitudeProp, "Amplitude");
                    m_MaterialEditor.ShaderProperty(windDirectionProp, "Direction");
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Translucency", EditorStyles.boldLabel);
                    m_MaterialEditor.ShaderProperty(translucencyColorProp, "Color");
                    m_MaterialEditor.ShaderProperty(translucencyStrengthProp, "Strength");
                    break;
                case SurfaceType.Transparent:
                    m_MaterialEditor.ShaderProperty(glassColorProp, "Glass Color & Opacity");
                    m_MaterialEditor.ShaderProperty(fresnelColorProp, "Fresnel (Edge) Color");
                    m_MaterialEditor.ShaderProperty(fresnelPowerProp, "Fresnel Power");
                    m_MaterialEditor.ShaderProperty(refractionStrengthProp, "Refraction Strength");
                    m_MaterialEditor.ShaderProperty(glassSpecularPowerProp, "Specular Power");
                    m_MaterialEditor.ShaderProperty(glassSpecularIntensityProp, "Specular Intensity");
                    break;
            }
        });
    }

    private void DrawEmissionProperties()
    {
        DrawSection(ref emissionFoldout, Styles.emissionHeader, () =>
        {
            m_MaterialEditor.ShaderProperty(emissionModeProp, "Enable Emission");
            if (emissionModeProp.floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                m_MaterialEditor.TexturePropertySingleLine(Styles.colorLabel, emissionMapProp, emissionColorProp);
                EditorGUI.indentLevel--;
            }
        });
    }

    private void DrawOutlineProperties()
    {
        DrawSection(ref outlineFoldout, Styles.outlineHeader, () =>
        {
            m_MaterialEditor.ShaderProperty(outlineModeProp, "Mode");
            OutlineMode outlineMode = (OutlineMode)outlineModeProp.floatValue;

            if (outlineMode != OutlineMode.None)
            {
                EditorGUI.indentLevel++;
                switch (outlineMode)
                {
                    case OutlineMode.InvertedHull:
                        m_MaterialEditor.ShaderProperty(outlineColorProp, Styles.colorLabel);
                        m_MaterialEditor.ShaderProperty(outlineWidthProp, "Width");
                        m_MaterialEditor.ShaderProperty(outlineScaleWithDistanceProp, "Scale With Distance");
                        m_MaterialEditor.ShaderProperty(distanceFadeStartProp, "Distance Fade Start");
                        m_MaterialEditor.ShaderProperty(distanceFadeEndProp, "Distance Fade End");
                        break;
                    case OutlineMode.Fresnel:
                        m_MaterialEditor.ShaderProperty(fresnelOutlineColorProp, Styles.colorLabel);
                        m_MaterialEditor.ShaderProperty(fresnelOutlineWidthProp, "Width");
                        m_MaterialEditor.ShaderProperty(fresnelOutlinePowerProp, "Power");
                        break;
                }
                EditorGUI.indentLevel--;
            }
        });
    }

    private void SetKeyword(Material material, string keyword, bool enabled)
    {
        if (enabled) material.EnableKeyword(keyword);
        else material.DisableKeyword(keyword);
    }

    private void SetupMaterialRenderingState(Material material)
    {
        SurfaceType surfaceType = (SurfaceType)material.GetFloat("_SurfaceType");
        OutlineMode outlineMode = (OutlineMode)material.GetFloat("_OutlineMode");
        BooleanOnOff isAlphaClipOn = (BooleanOnOff)material.GetFloat("_AlphaClipMode");

        SetKeyword(material, "_SURFACETYPE_OPAQUE", surfaceType == SurfaceType.Opaque);
        SetKeyword(material, "_SURFACETYPE_TRANSPARENT", surfaceType == SurfaceType.Transparent);
        SetKeyword(material, "_SURFACETYPE_METALLIC", surfaceType == SurfaceType.Metallic);
        SetKeyword(material, "_SURFACETYPE_FOLIAGE", surfaceType == SurfaceType.Foliage);

        bool isOpaqueLike = surfaceType == SurfaceType.Opaque || surfaceType == SurfaceType.Metallic || surfaceType == SurfaceType.Foliage;

        if (isOpaqueLike)
        {
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.SetInt("_Cull", (int)CullMode.Back);
            if (isAlphaClipOn == BooleanOnOff.On)
            {
                material.SetOverrideTag("RenderType", "TransparentCutout");
                material.renderQueue = (int)RenderQueue.AlphaTest;
            }
            else
            {
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = (int)RenderQueue.Geometry;
            }
        }
        else // Transparent
        {
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.SetInt("_Cull", (int)CullMode.Back);
            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        SetKeyword(material, "_ALPHACLIP_ON", isAlphaClipOn == BooleanOnOff.On && isOpaqueLike);
        SetKeyword(material, "_EMISSION_ON", (BooleanOnOff)material.GetFloat("_EmissionMode") == BooleanOnOff.On);
        SetKeyword(material, "_FAKELIGHT_ON", (BooleanOnOff)material.GetFloat("_FakeLightMode") == BooleanOnOff.On);
        SetKeyword(material, "_OUTLINEMODE_INVERTEDHULL", outlineMode == OutlineMode.InvertedHull);
        SetKeyword(material, "_OUTLINEMODE_FRESNEL", outlineMode == OutlineMode.Fresnel);
        SetKeyword(material, "_OUTLINE_SCALE_WITH_DISTANCE", outlineMode == OutlineMode.InvertedHull && (BooleanOnOff)material.GetFloat("_OutlineScaleWithDistance") == BooleanOnOff.On);
    }

    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);
        if (material.HasProperty("_SurfaceType"))
        {
            SetupMaterialRenderingState(material);
        }
    }
}
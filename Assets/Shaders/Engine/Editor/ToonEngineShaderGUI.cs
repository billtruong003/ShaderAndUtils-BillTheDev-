using UnityEngine;
using UnityEditor;
using System;

public class ToonEngineShaderGUI : ShaderGUI
{
    private MaterialEditor materialEditor;
    private Material material;
    private MaterialProperty[] properties;

    private static class FoldoutStates
    {
        public static bool SurfacePipeline = true;
        public static bool EngineAnimation = true;
        public static bool BaseProperties = true;
        public static bool Lighting = true;
        public static bool ToonShading = true;
        public static bool Metallic = false;
        public static bool Glass = false;
        public static bool Foliage = false;
        public static bool Outline = true;
    }

    private sealed class ShaderProperties
    {
        public MaterialProperty surfaceType;
        public MaterialProperty outlineMode;
        public MaterialProperty engineAnimationMode;
        public MaterialProperty speed;
        public MaterialProperty stepDelay;
        public MaterialProperty pistonDisplacement;
        public MaterialProperty pistonAxis;
        public MaterialProperty rotationPivot;
        public MaterialProperty rotationAxis;
        public MaterialProperty shakeFrequency;
        public MaterialProperty shakeAmplitude;
        public MaterialProperty baseMap;
        public MaterialProperty baseColor;
        public MaterialProperty alphaClipMode;
        public MaterialProperty cutoff;
        public MaterialProperty emissionMode;
        public MaterialProperty emissionColor;
        public MaterialProperty emissionMap;
        public MaterialProperty fakeLightMode;
        public MaterialProperty fakeLightColor;
        public MaterialProperty fakeLightDirection;
        public MaterialProperty toonRampOffset;
        public MaterialProperty toonRampSmoothness;
        public MaterialProperty shadowTint;
        public MaterialProperty ramp;
        public MaterialProperty brightness;
        public MaterialProperty offset;
        public MaterialProperty specuColor;
        public MaterialProperty highlightOffset;
        public MaterialProperty hiColor;
        public MaterialProperty rimColor;
        public MaterialProperty rimPower;
        public MaterialProperty windFrequency;
        public MaterialProperty windAmplitude;
        public MaterialProperty windDirection;
        public MaterialProperty translucencyColor;
        public MaterialProperty translucencyStrength;
        public MaterialProperty glassColor;
        public MaterialProperty fresnelColor;
        public MaterialProperty fresnelPower;
        public MaterialProperty refractionStrength;
        public MaterialProperty glassSpecularPower;
        public MaterialProperty glassSpecularIntensity;
        public MaterialProperty outlineColor;
        public MaterialProperty outlineWidth;
        public MaterialProperty outlineScaleWithDistance;
        public MaterialProperty distanceFadeStart;
        public MaterialProperty distanceFadeEnd;
        public MaterialProperty fresnelOutlineColor;
        public MaterialProperty fresnelOutlineWidth;
        public MaterialProperty fresnelOutlinePower;

        public ShaderProperties(MaterialProperty[] properties)
        {
            surfaceType = FindProperty("_SurfaceType", properties);
            outlineMode = FindProperty("_OutlineMode", properties);
            engineAnimationMode = FindProperty("_EngineAnimationMode", properties);
            speed = FindProperty("_Speed", properties);
            stepDelay = FindProperty("_StepDelay", properties);
            pistonDisplacement = FindProperty("_PistonDisplacement", properties);
            pistonAxis = FindProperty("_PistonAxis", properties);
            rotationPivot = FindProperty("_RotationPivot", properties);
            rotationAxis = FindProperty("_RotationAxis", properties);
            shakeFrequency = FindProperty("_ShakeFrequency", properties);
            shakeAmplitude = FindProperty("_ShakeAmplitude", properties);
            baseMap = FindProperty("_BaseMap", properties);
            baseColor = FindProperty("_BaseColor", properties);
            alphaClipMode = FindProperty("_AlphaClipMode", properties);
            cutoff = FindProperty("_Cutoff", properties);
            emissionMode = FindProperty("_EmissionMode", properties);
            emissionColor = FindProperty("_EmissionColor", properties);
            emissionMap = FindProperty("_EmissionMap", properties);
            fakeLightMode = FindProperty("_FakeLightMode", properties);
            fakeLightColor = FindProperty("_FakeLightColor", properties);
            fakeLightDirection = FindProperty("_FakeLightDirection", properties);
            toonRampOffset = FindProperty("_ToonRampOffset", properties);
            toonRampSmoothness = FindProperty("_ToonRampSmoothness", properties);
            shadowTint = FindProperty("_ShadowTint", properties);
            ramp = FindProperty("_Ramp", properties);
            brightness = FindProperty("_Brightness", properties);
            offset = FindProperty("_Offset", properties);
            specuColor = FindProperty("_SpecuColor", properties);
            highlightOffset = FindProperty("_HighlightOffset", properties);
            hiColor = FindProperty("_HiColor", properties);
            rimColor = FindProperty("_RimColor", properties);
            rimPower = FindProperty("_RimPower", properties);
            windFrequency = FindProperty("_WindFrequency", properties);
            windAmplitude = FindProperty("_WindAmplitude", properties);
            windDirection = FindProperty("_WindDirection", properties);
            translucencyColor = FindProperty("_TranslucencyColor", properties);
            translucencyStrength = FindProperty("_TranslucencyStrength", properties);
            glassColor = FindProperty("_GlassColor", properties);
            fresnelColor = FindProperty("_FresnelColor", properties);
            fresnelPower = FindProperty("_FresnelPower", properties);
            refractionStrength = FindProperty("_RefractionStrength", properties);
            glassSpecularPower = FindProperty("_GlassSpecularPower", properties);
            glassSpecularIntensity = FindProperty("_GlassSpecularIntensity", properties);
            outlineColor = FindProperty("_OutlineColor", properties);
            outlineWidth = FindProperty("_OutlineWidth", properties);
            outlineScaleWithDistance = FindProperty("_OutlineScaleWithDistance", properties);
            distanceFadeStart = FindProperty("_DistanceFadeStart", properties);
            distanceFadeEnd = FindProperty("_DistanceFadeEnd", properties);
            fresnelOutlineColor = FindProperty("_FresnelOutlineColor", properties);
            fresnelOutlineWidth = FindProperty("_FresnelOutlineWidth", properties);
            fresnelOutlinePower = FindProperty("_FresnelOutlinePower", properties);
        }
    }

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] allProperties)
    {
        materialEditor = editor;
        material = editor.target as Material;
        properties = allProperties;
        ShaderProperties props = new ShaderProperties(properties);

        DrawHeader("TOON ENGINE UBER SHADER");
        EditorGUI.BeginChangeCheck();

        DrawSurfacePipelineSection(props);
        DrawEngineAnimationSection(props);

        switch ((int)props.surfaceType.floatValue)
        {
            case 0: DrawOpaqueProperties(props); break;
            case 1: DrawTransparentProperties(props); break;
            case 2: DrawMetallicProperties(props); break;
            case 3: DrawFoliageProperties(props); break;
        }

        DrawOutlineSection(props);

        if (EditorGUI.EndChangeCheck())
        {
            UpdateMaterialSettings(props);
        }
    }

    private void DrawSurfacePipelineSection(ShaderProperties props)
    {
        FoldoutStates.SurfacePipeline = DrawFoldoutHeader("Surface Pipeline", FoldoutStates.SurfacePipeline);
        if (!FoldoutStates.SurfacePipeline) return;
        DrawProperty(props.surfaceType);
        DrawProperty(props.outlineMode);
    }

    private void DrawEngineAnimationSection(ShaderProperties props)
    {
        FoldoutStates.EngineAnimation = DrawFoldoutHeader("Engine Animation", FoldoutStates.EngineAnimation);
        if (!FoldoutStates.EngineAnimation) return;

        DrawProperty(props.engineAnimationMode);
        if (props.engineAnimationMode.floatValue > 0.5f)
        {
            DrawIndented(() =>
            {
                DrawProperty(props.speed);
                DrawProperty(props.stepDelay);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Piston Properties (Red)", EditorStyles.boldLabel);
                DrawProperty(props.pistonDisplacement);
                DrawProperty(props.pistonAxis);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Rotation Properties (Green)", EditorStyles.boldLabel);
                DrawProperty(props.rotationPivot);
                DrawProperty(props.rotationAxis);
                EditorGUILayout.Space();

                EditorGUILayout.LabelField("Shake Properties (Blue)", EditorStyles.boldLabel);
                DrawProperty(props.shakeFrequency);
                DrawProperty(props.shakeAmplitude);
            });
        }
    }

    private void DrawOpaqueProperties(ShaderProperties props)
    {
        DrawBaseProperties(props);
        DrawLightingSection(props, true);
        DrawToonShadingSection(props);
    }

    private void DrawMetallicProperties(ShaderProperties props)
    {
        DrawBaseProperties(props);
        DrawLightingSection(props, true);
        FoldoutStates.Metallic = DrawFoldoutHeader("Stylized Metal", FoldoutStates.Metallic);
        if (!FoldoutStates.Metallic) return;
        DrawIndented(() =>
        {
            DrawProperty(props.ramp);
            DrawProperty(props.brightness);
            DrawProperty(props.offset);
            DrawProperty(props.specuColor);
            DrawProperty(props.highlightOffset);
            DrawProperty(props.hiColor);
            DrawProperty(props.rimColor);
            DrawProperty(props.rimPower);
        });
    }

    private void DrawFoliageProperties(ShaderProperties props)
    {
        DrawBaseProperties(props);
        DrawLightingSection(props, false);
        FoldoutStates.Foliage = DrawFoldoutHeader("Foliage", FoldoutStates.Foliage);
        if (!FoldoutStates.Foliage) return;
        DrawIndented(() =>
        {
            DrawProperty(props.windFrequency);
            DrawProperty(props.windAmplitude);
            DrawProperty(props.windDirection);
            DrawProperty(props.translucencyColor);
            DrawProperty(props.translucencyStrength);
        });
    }

    private void DrawTransparentProperties(ShaderProperties props)
    {
        DrawBaseProperties(props, false);
        DrawLightingSection(props, false);
        FoldoutStates.Glass = DrawFoldoutHeader("Stylized Glass", FoldoutStates.Glass);
        if (!FoldoutStates.Glass) return;
        DrawIndented(() =>
        {
            DrawProperty(props.glassColor);
            DrawProperty(props.fresnelColor);
            DrawProperty(props.fresnelPower);
            DrawProperty(props.refractionStrength);
            DrawProperty(props.glassSpecularPower);
            DrawProperty(props.glassSpecularIntensity);
        });
    }

    private void DrawBaseProperties(ShaderProperties props, bool showAlphaClip = true)
    {
        FoldoutStates.BaseProperties = DrawFoldoutHeader("Base Properties", FoldoutStates.BaseProperties);
        if (!FoldoutStates.BaseProperties) return;
        DrawIndented(() =>
        {
            DrawProperty(props.baseMap);
            DrawProperty(props.baseColor);
            if (showAlphaClip)
            {
                DrawProperty(props.alphaClipMode);
                if (props.alphaClipMode.floatValue > 0.5f) DrawIndented(() => DrawProperty(props.cutoff));
            }
            DrawProperty(props.emissionMode);
            if (props.emissionMode.floatValue > 0.5f)
            {
                DrawIndented(() =>
                {
                    DrawProperty(props.emissionColor);
                    DrawProperty(props.emissionMap);
                });
            }
        });
    }

    private void DrawLightingSection(ShaderProperties props, bool showFakeLight)
    {
        FoldoutStates.Lighting = DrawFoldoutHeader("Lighting", FoldoutStates.Lighting);
        if (!FoldoutStates.Lighting) return;
        DrawIndented(() =>
        {
            if (showFakeLight)
            {
                DrawProperty(props.fakeLightMode);
                if (props.fakeLightMode.floatValue > 0.5f)
                {
                    DrawIndented(() =>
                    {
                        DrawProperty(props.fakeLightColor);
                        DrawProperty(props.fakeLightDirection);
                    });
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Fake Light is not available for this Surface Type.", MessageType.Info);
            }
        });
    }

    private void DrawToonShadingSection(ShaderProperties props)
    {
        FoldoutStates.ToonShading = DrawFoldoutHeader("Toon Shading", FoldoutStates.ToonShading);
        if (!FoldoutStates.ToonShading) return;
        DrawIndented(() =>
        {
            DrawProperty(props.toonRampOffset);
            DrawProperty(props.toonRampSmoothness);
            DrawProperty(props.shadowTint);
        });
    }

    private void DrawOutlineSection(ShaderProperties props)
    {
        FoldoutStates.Outline = DrawFoldoutHeader("Outline", FoldoutStates.Outline);
        if (!FoldoutStates.Outline) return;
        if (props.outlineMode.floatValue < 0.5f)
        {
            EditorGUILayout.HelpBox("Outline is disabled. Select an Outline Mode in the Surface Pipeline section.", MessageType.Info);
            return;
        }
        DrawIndented(() =>
        {
            switch ((int)props.outlineMode.floatValue)
            {
                case 1:
                    DrawProperty(props.outlineColor);
                    DrawProperty(props.outlineWidth);
                    DrawProperty(props.outlineScaleWithDistance);
                    DrawProperty(props.distanceFadeStart);
                    DrawProperty(props.distanceFadeEnd);
                    break;
                case 2:
                    DrawProperty(props.fresnelOutlineColor);
                    DrawProperty(props.fresnelOutlineWidth);
                    DrawProperty(props.fresnelOutlinePower);
                    break;
            }
        });
    }

    private void UpdateMaterialSettings(ShaderProperties props)
    {
        int surfaceType = (int)props.surfaceType.floatValue;
        if (surfaceType == 1)
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
        }
        else
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            material.SetOverrideTag("RenderType", "Opaque");
        }

        SetKeyword(material, "_SURFACETYPE_OPAQUE", surfaceType == 0);
        SetKeyword(material, "_SURFACETYPE_TRANSPARENT", surfaceType == 1);
        SetKeyword(material, "_SURFACETYPE_METALLIC", surfaceType == 2);
        SetKeyword(material, "_SURFACETYPE_FOLIAGE", surfaceType == 3);
        int outlineMode = (int)props.outlineMode.floatValue;
        SetKeyword(material, "_OUTLINEMODE_INVERTED_HULL", outlineMode == 1);
        SetKeyword(material, "_OUTLINEMODE_FRESNEL", outlineMode == 2);
        SetKeyword(material, "_ENGINEANIMATIONMODE_ON", props.engineAnimationMode.floatValue > 0.5f);
        SetKeyword(material, "_ALPHACLIP_ON", props.alphaClipMode.floatValue > 0.5f && surfaceType != 1);
        SetKeyword(material, "_EMISSION_ON", props.emissionMode.floatValue > 0.5f);
        SetKeyword(material, "_FAKELIGHT_ON", props.fakeLightMode.floatValue > 0.5f);
    }

    private void DrawProperty(MaterialProperty prop)
    {
        if (prop != null) materialEditor.ShaderProperty(prop, prop.displayName);
    }

    private void DrawIndented(Action content)
    {
        EditorGUI.indentLevel++;
        content();
        EditorGUI.indentLevel--;
    }

    private void DrawHeader(string text)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }

    private bool DrawFoldoutHeader(string text, bool state)
    {
        var backgroundRect = GUILayoutUtility.GetRect(1f, 22f);
        var labelRect = backgroundRect;
        labelRect.xMin += 16f;
        labelRect.xMax -= 20f;
        var foldoutRect = backgroundRect;
        foldoutRect.y += 1f;
        foldoutRect.width = 13f;
        foldoutRect.height = 13f;

        EditorGUI.DrawRect(backgroundRect, new Color(0.15f, 0.15f, 0.15f));
        EditorGUI.LabelField(labelRect, text, new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleLeft });
        state = GUI.Toggle(foldoutRect, state, GUIContent.none, new GUIStyle("Foldout"));

        var e = Event.current;
        if (e.type == EventType.MouseDown && backgroundRect.Contains(e.mousePosition) && e.button == 0)
        {
            state = !state;
            e.Use();
        }
        EditorGUILayout.Space(2);
        return state;
    }

    private static void SetKeyword(Material m, string keyword, bool enabled)
    {
        if (enabled) m.EnableKeyword(keyword);
        else m.DisableKeyword(keyword);
    }
}
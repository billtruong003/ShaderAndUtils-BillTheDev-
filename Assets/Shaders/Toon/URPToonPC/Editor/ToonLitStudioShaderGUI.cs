using UnityEngine;
using UnityEditor;
using System;

public class ToonLitStudioShaderGUI : ShaderGUI
{
    private MaterialEditor editor;
    private MaterialProperty[] properties;
    private Material targetMat;

    // Thêm cờ cho section mới
    private bool showSurfaceProps = true;
    private bool showAdvancedStates = true;
    private bool showMainRamp = true;
    private bool showLighting = true;
    private bool showArtistic = true;
    private bool showSurfaceEffects = true;

    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;

    private enum SurfaceType { Opaque, Cutout, Transparent }
    private enum MatcapBlendMode { Add, Multiply, Lerp }

    private void InitializeStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14,
                richText = true
            };
        }
        if (sectionStyle == null)
        {
            sectionStyle = new GUIStyle("ShurikenModuleTitle")
            {
                font = new GUIStyle(EditorStyles.label).font,
                border = new RectOffset(15, 7, 4, 4),
                fixedHeight = 22,
                contentOffset = new Vector2(20f, -2f)
            };
        }
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        this.editor = materialEditor;
        this.properties = properties;
        this.targetMat = materialEditor.target as Material;
        InitializeStyles();

        DrawHeader("TOON LIT SHADER - STUDIO ADVANCED");

        DrawSurfacePropertiesSection();
        DrawAdvancedStatesSection(); // Thêm section mới vào đây
        DrawMainRampSection();
        DrawLightingSection();
        DrawArtisticSection();
        DrawSurfaceEffectsSection();
    }

    private void DrawHeader(string text)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"<color=#33c8ff>{text}</color>", headerStyle, GUILayout.Height(22));
        EditorGUILayout.Space();
    }

    private void DrawSectionToggle(string label, ref bool toggle)
    {
        var rect = GUILayoutUtility.GetRect(16f, 22f, sectionStyle);
        GUI.Box(rect, label, sectionStyle);
        var e = Event.current;
        if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
        {
            toggle = !toggle;
            e.Use();
        }
    }

    private MaterialProperty FindProp(string name) => FindProperty(name, properties);

    private void SetKeyword(string keyword, bool enabled)
    {
        if (enabled) targetMat.EnableKeyword(keyword);
        else targetMat.DisableKeyword(keyword);
    }

    // Tái cấu trúc để sử dụng MaterialProperty cho tính nhất quán
    private void SetupMaterialWithSurfaceType(SurfaceType surfaceType)
    {
        switch (surfaceType)
        {
            case SurfaceType.Opaque:
                targetMat.SetOverrideTag("RenderType", "Opaque");
                targetMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                FindProp("_SrcBlend").floatValue = (float)UnityEngine.Rendering.BlendMode.One;
                FindProp("_DstBlend").floatValue = (float)UnityEngine.Rendering.BlendMode.Zero;
                FindProp("_ZWrite").floatValue = 1;
                SetKeyword("_ALPHATEST_ON", false);
                break;
            case SurfaceType.Cutout:
                targetMat.SetOverrideTag("RenderType", "TransparentCutout");
                targetMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                FindProp("_SrcBlend").floatValue = (float)UnityEngine.Rendering.BlendMode.One;
                FindProp("_DstBlend").floatValue = (float)UnityEngine.Rendering.BlendMode.Zero;
                FindProp("_ZWrite").floatValue = 1;
                SetKeyword("_ALPHATEST_ON", true);
                break;
            case SurfaceType.Transparent:
                targetMat.SetOverrideTag("RenderType", "Transparent");
                targetMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                FindProp("_SrcBlend").floatValue = (float)UnityEngine.Rendering.BlendMode.SrcAlpha;
                FindProp("_DstBlend").floatValue = (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha;
                FindProp("_ZWrite").floatValue = 0;
                SetKeyword("_ALPHATEST_ON", false);
                break;
        }
    }

    private void DrawSurfacePropertiesSection()
    {
        DrawSectionToggle("Surface Properties", ref showSurfaceProps);
        if (showSurfaceProps)
        {
            EditorGUILayout.Space();

            var surfaceTypeProp = FindProp("_SurfaceType");
            EditorGUI.BeginChangeCheck();
            var newSurfaceType = (SurfaceType)EditorGUILayout.EnumPopup("Surface Type", (SurfaceType)surfaceTypeProp.floatValue);
            if (EditorGUI.EndChangeCheck())
            {
                surfaceTypeProp.floatValue = (float)newSurfaceType;
                SetupMaterialWithSurfaceType(newSurfaceType);
            }

            if ((SurfaceType)surfaceTypeProp.floatValue == SurfaceType.Cutout)
            {
                editor.ShaderProperty(FindProp("_Cutoff"), "Alpha Cutoff");
            }

            EditorGUILayout.Space();
            editor.TexturePropertySingleLine(new GUIContent("Base Map (Albedo)"), FindProp("_BaseMap"));

            editor.ShaderProperty(FindProp("_EnableNormalMap"), "Enable Normal Map");
            if (FindProp("_EnableNormalMap").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.TexturePropertySingleLine(new GUIContent("Normal Map"), FindProp("_BumpMap"));
                editor.ShaderProperty(FindProp("_BumpScale"), "Normal Intensity");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }
    }

    // Hàm vẽ cho section mới
    private void DrawAdvancedStatesSection()
    {
        DrawSectionToggle("Advanced Rendering States", ref showAdvancedStates);
        if (showAdvancedStates)
        {
            EditorGUILayout.Space();

            // Sử dụng editor.ShaderProperty để tự động tạo dropdowns từ [Enum]
            editor.ShaderProperty(FindProp("_Cull"), "Culling Mode");
            editor.ShaderProperty(FindProp("_ZTest"), "Depth Test");
            editor.ShaderProperty(FindProp("_ZWrite"), "Depth Write");

            EditorGUILayout.Space();

            editor.ShaderProperty(FindProp("_SrcBlend"), "Source Blend");
            editor.ShaderProperty(FindProp("_DstBlend"), "Destination Blend");

            EditorGUILayout.Space();
        }
    }

    private void DrawMainRampSection()
    {
        DrawSectionToggle("Main Shading Ramp", ref showMainRamp);
        if (showMainRamp)
        {
            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableRampTexture"), "Use Ramp Texture");

            if (FindProp("_EnableRampTexture").floatValue > 0.5f)
            {
                editor.TexturePropertySingleLine(new GUIContent("Ramp Texture"), FindProp("_RampMap"));
            }
            else
            {
                editor.ColorProperty(FindProp("_HighlightColor"), "Highlight Color");
                editor.ColorProperty(FindProp("_MidtoneColor"), "Midtone Color");
                editor.ColorProperty(FindProp("_ShadowColor"), "Shadow Color");
                EditorGUILayout.Space();

                MaterialProperty shadowProp = FindProp("_ShadowThreshold");
                MaterialProperty highlightProp = FindProp("_HighlightThreshold");

                EditorGUI.BeginChangeCheck();
                editor.ShaderProperty(shadowProp, "Shadow Threshold");
                editor.ShaderProperty(highlightProp, "Highlight Threshold");
                if (EditorGUI.EndChangeCheck())
                {
                    highlightProp.floatValue = Mathf.Max(shadowProp.floatValue, highlightProp.floatValue);
                }

                editor.ShaderProperty(FindProp("_RampSmoothness"), "Ramp Smoothness");
            }
            EditorGUILayout.Space();
        }
    }

    private void DrawLightingSection()
    {
        DrawSectionToggle("Lighting Control", ref showLighting);
        if (showLighting)
        {
            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_UseFakeLight"), "Use Fake Light Direction");
            if (FindProp("_UseFakeLight").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_FakeLightDirection"), "Fake Direction");
                EditorGUI.indentLevel--;
            }
            editor.ShaderProperty(FindProp("_CustomShadowColor"), new GUIContent("Main Light Shadow Color"));
            editor.ShaderProperty(FindProp("_ShadowTintInfluence"), new GUIContent("Light Color On Shadow", "Hòa trộn màu của bóng với màu của ánh sáng."));

            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableGradientAmbient"), "Enable Gradient Ambient");
            if (FindProp("_EnableGradientAmbient").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_SkyColor"), "Sky Color");
                editor.ShaderProperty(FindProp("_GroundColor"), "Ground Color");
                editor.ShaderProperty(FindProp("_AmbientGradientPower"), "Gradient Power");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableAdditionalLights"), "Enable Additional Lights");
            if (FindProp("_EnableAdditionalLights").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_AdditionalLightInfluence"), "Influence");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }
    }

    private void DrawArtisticSection()
    {
        DrawSectionToggle("Artistic Effects", ref showArtistic);
        if (showArtistic)
        {
            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableHatching"), "Enable Shadow Hatching");
            if (FindProp("_EnableHatching").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.TexturePropertySingleLine(new GUIContent("Hatching Map"), FindProp("_HatchingMap"));
                editor.ShaderProperty(FindProp("_HatchingTiling"), "Tiling");
                editor.ShaderProperty(FindProp("_HatchingVisibility"), "Visibility");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableMatcap"), "Enable MatCap");
            if (FindProp("_EnableMatcap").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.TexturePropertySingleLine(new GUIContent("MatCap Map"), FindProp("_MatcapMap"));

                var blendModeProp = FindProp("_MatcapBlendMode");
                EditorGUI.BeginChangeCheck();
                var newMode = (MatcapBlendMode)EditorGUILayout.EnumPopup("Blend Mode", (MatcapBlendMode)blendModeProp.floatValue);
                if (EditorGUI.EndChangeCheck())
                {
                    blendModeProp.floatValue = (float)newMode;
                    SetKeyword("_MATCAP_BLEND_ADD", newMode == MatcapBlendMode.Add);
                    SetKeyword("_MATCAP_BLEND_MULTIPLY", newMode == MatcapBlendMode.Multiply);
                    SetKeyword("_MATCAP_BLEND_LERP", newMode == MatcapBlendMode.Lerp);
                }

                editor.ShaderProperty(FindProp("_MatcapTint"), new GUIContent("Tint & Lerp Alpha"));
                editor.ShaderProperty(FindProp("_MatcapIntensity"), "Intensity");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }
    }

    private void DrawSurfaceEffectsSection()
    {
        DrawSectionToggle("Surface Effects", ref showSurfaceEffects);
        if (showSurfaceEffects)
        {
            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableSpecular"), "Enable Specular Reflection");
            if (FindProp("_EnableSpecular").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_SpecularColor"), "Color");
                editor.ShaderProperty(FindProp("_SpecularThreshold"), "Threshold");
                editor.ShaderProperty(FindProp("_SpecularSmoothness"), "Smoothness");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            editor.ShaderProperty(FindProp("_EnableRimLight"), "Enable Rim Light");
            if (FindProp("_EnableRimLight").floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_RimColor"), new GUIContent("Color & Intensity(A)"));
                editor.ShaderProperty(FindProp("_RimPower"), "Power");
                editor.ShaderProperty(FindProp("_RimThreshold"), "Threshold");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }
    }
}
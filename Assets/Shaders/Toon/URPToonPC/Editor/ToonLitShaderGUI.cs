using UnityEngine;
using UnityEditor;
using System;

public class ToonLitShaderGUI : ShaderGUI
{
    private MaterialEditor editor;
    private MaterialProperty[] properties;

    private bool showMainRamp = true;
    private bool showLighting = true;
    private bool showEffects = true;

    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;

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

        InitializeStyles();

        DrawHeader("TOON LIT SHADER");

        DrawMainRampSection();
        DrawLightingSection();
        DrawEffectsSection();
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

    private void DrawMainRampSection()
    {
        DrawSectionToggle("Main Shading Ramp", ref showMainRamp);
        if (showMainRamp)
        {
            EditorGUILayout.Space();
            editor.TexturePropertySingleLine(new GUIContent("Base Map (Albedo)"), FindProp("_BaseMap"));
            editor.ColorProperty(FindProp("_HighlightColor"), "Highlight Color");
            editor.ColorProperty(FindProp("_MidtoneColor"), "Midtone Color");
            editor.ColorProperty(FindProp("_ShadowColor"), "Shadow Color");

            EditorGUILayout.Space();

            // --- CẢI TIẾN: SỬ DỤNG MINMAXSLIDER ---
            MaterialProperty highlightProp = FindProp("_HighlightThreshold");
            MaterialProperty shadowProp = FindProp("_ShadowThreshold");

            float shadowVal = shadowProp.floatValue;
            float highlightVal = highlightProp.floatValue;

            var label = new GUIContent("Ramp Thresholds", "Vùng bên trái là Shadow, vùng bên phải là Highlight.");

            // Bắt đầu kiểm tra xem có thay đổi nào trên GUI không
            EditorGUI.BeginChangeCheck();

            // Vẽ MinMaxSlider
            EditorGUILayout.MinMaxSlider(label, ref shadowVal, ref highlightVal, 0.0f, 1.0f);

            // Vẽ hai ô số để hiển thị và nhập giá trị chính xác
            EditorGUILayout.BeginHorizontal();
            shadowVal = EditorGUILayout.FloatField(shadowVal, GUILayout.Width(60));
            GUILayout.FlexibleSpace();
            highlightVal = EditorGUILayout.FloatField(highlightVal, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();

            // Nếu có thay đổi, cập nhật giá trị vào Material
            if (EditorGUI.EndChangeCheck())
            {
                shadowProp.floatValue = shadowVal;
                highlightProp.floatValue = highlightVal;
            }
            // --- KẾT THÚC CẢI TIẾN ---

            editor.ShaderProperty(FindProp("_RampSmoothness"), "Ramp Smoothness");
            EditorGUILayout.Space();
        }
    }

    private void DrawLightingSection()
    {
        DrawSectionToggle("Lighting Control", ref showLighting);
        if (showLighting)
        {
            EditorGUILayout.Space();
            var fakeLightToggle = FindProp("_UseFakeLight");
            editor.ShaderProperty(fakeLightToggle, "Use Fake Light Direction");
            if (fakeLightToggle.floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_FakeLightDirection"), "Fake Direction");
                EditorGUI.indentLevel--;
            }

            editor.ShaderProperty(FindProp("_CustomShadowColor"), new GUIContent("Main Light Shadow Color", "Màu được nhân vào vùng đổ bóng của ánh sáng chính (Main Light)."));
            editor.ShaderProperty(FindProp("_AmbientColor"), new GUIContent("Custom Ambient", "RGB: Màu môi trường, A: Cường độ hòa trộn."));

            EditorGUILayout.Space();

            var addLightsToggle = FindProp("_EnableAdditionalLights");
            editor.ShaderProperty(addLightsToggle, "Enable Additional Lights");
            if (addLightsToggle.floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_AdditionalLightInfluence"), "Influence");
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.Space();
        }
    }

    private void DrawEffectsSection()
    {
        DrawSectionToggle("Surface Effects", ref showEffects);
        if (showEffects)
        {
            EditorGUILayout.Space();
            var specToggle = FindProp("_EnableSpecular");
            editor.ShaderProperty(specToggle, "Enable Specular Reflection");
            if (specToggle.floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(FindProp("_SpecularColor"), "Color");
                editor.ShaderProperty(FindProp("_SpecularThreshold"), "Threshold");
                editor.ShaderProperty(FindProp("_SpecularSmoothness"), "Smoothness");
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            var rimToggle = FindProp("_EnableRimLight");
            editor.ShaderProperty(rimToggle, "Enable Rim Light");
            if (rimToggle.floatValue > 0.5f)
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
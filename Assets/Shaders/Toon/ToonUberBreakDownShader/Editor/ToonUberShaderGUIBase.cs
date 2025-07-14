using UnityEngine;
using UnityEditor;
using System;

public abstract class ToonUberShaderGUIBase : ShaderGUI
{
    protected MaterialEditor materialEditor;
    protected MaterialProperty[] properties;
    protected Material material;

    private static bool showAdvancedSettings = false;
    private static GUIStyle headerStyle;

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] props)
    {
        materialEditor = editor;
        properties = props;
        material = materialEditor.target as Material;

        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 14
            };
        }

        FindProperties();

        EditorGUI.BeginChangeCheck();

        DrawHeader();
        DrawWorkflowSettings();

        EditorGUILayout.Space();

        DrawMainProperties();

        EditorGUILayout.Space();

        DrawAdvancedSettings();

        if (EditorGUI.EndChangeCheck())
        {
            ApplyKeywords();
        }
    }

    protected abstract void FindProperties();
    protected abstract void DrawWorkflowSettings();
    protected abstract void DrawMainProperties();
    protected abstract void ApplyKeywords();

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Bill's Toon Shader", headerStyle);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
    }

    private void DrawAdvancedSettings()
    {
        DrawFoldout("Advanced Settings", ref showAdvancedSettings, () =>
        {
            materialEditor.RenderQueueField();
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        });
    }

    protected void DrawFoldout(string title, ref bool state, Action contents)
    {
        var rect = EditorGUILayout.BeginVertical();
        state = EditorGUILayout.BeginFoldoutHeaderGroup(state, title);
        if (state)
        {
            EditorGUILayout.BeginVertical("box");
            contents.Invoke();
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        var bgRect = GUILayoutUtility.GetLastRect();
        bgRect.x = rect.x - 2;
        bgRect.width = rect.width + 4;
        EditorGUI.DrawRect(bgRect, new Color(0, 0, 0, 0.05f));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    protected void DrawPropertyGroup(MaterialProperty toggleProp, string title, Action contents)
    {
        bool isEnabled = toggleProp.floatValue > 0;

        EditorGUILayout.BeginHorizontal();
        isEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(15));
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUILayout.EndHorizontal();

        toggleProp.floatValue = isEnabled ? 1.0f : 0.0f;

        if (isEnabled)
        {
            EditorGUI.indentLevel++;
            contents();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
    }

    protected void SetKeyword(string keyword, bool state)
    {
        if (state)
        {
            material.EnableKeyword(keyword);
        }
        else
        {
            material.DisableKeyword(keyword);
        }
    }

    protected void SwitchShader(string newShaderName)
    {
        var newShader = Shader.Find(newShaderName);
        if (newShader != null)
        {
            material.shader = newShader;
        }
        else
        {
            Debug.LogWarning($"Could not find shader '{newShaderName}'");
        }
    }
}
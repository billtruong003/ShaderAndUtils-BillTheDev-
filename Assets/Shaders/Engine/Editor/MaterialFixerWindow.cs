using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

public class MaterialFixerWindow : EditorWindow
{
    public enum FixerAction { FixShader, Ignore }
    private List<Material> materialsToFix;
    private Dictionary<Material, FixerAction> choices;
    private Action<Dictionary<Material, FixerAction>> onCompleteCallback;

    public static void ShowWindow(List<Material> materials, Action<Dictionary<Material, FixerAction>> onComplete)
    {
        MaterialFixerWindow window = GetWindow<MaterialFixerWindow>(true, "Material Shader Fixer", true);
        window.minSize = new Vector2(450, 300);
        window.Initialize(materials, onComplete);
    }

    private void Initialize(List<Material> materials, Action<Dictionary<Material, FixerAction>> onComplete)
    {
        materialsToFix = materials;
        onCompleteCallback = onComplete;
        choices = new Dictionary<Material, FixerAction>();
        foreach (var mat in materialsToFix)
        {
            choices[mat] = FixerAction.FixShader; // Mặc định là Fix
        }
    }

    private void OnGUI()
    {
        if (materialsToFix == null || materialsToFix.Count == 0)
        {
            Close();
            return;
        }

        EditorGUILayout.LabelField("Shader Mismatch Detected", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Some materials are not using the required animation shader. Choose an action for each material.", MessageType.Warning);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Set All to [Fix Shader]")) SetAllActions(FixerAction.FixShader);
        if (GUILayout.Button("Set All to [Ignore]")) SetAllActions(FixerAction.Ignore);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Vẽ danh sách các material và lựa chọn
        EditorGUILayout.BeginVertical("box");
        for (int i = 0; i < materialsToFix.Count; i++)
        {
            var mat = materialsToFix[i];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(mat, typeof(Material), false);
            choices[mat] = (FixerAction)EditorGUILayout.EnumPopup(choices[mat], GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        if (GUILayout.Button("Apply and Continue", GUILayout.Height(30)))
        {
            onCompleteCallback?.Invoke(choices);
            Close();
        }

        if (GUILayout.Button("Cancel"))
        {
            onCompleteCallback?.Invoke(null); // Gửi null để báo hiệu hủy bỏ
            Close();
        }
    }

    private void SetAllActions(FixerAction action)
    {
        foreach (var mat in materialsToFix)
        {
            choices[mat] = action;
        }
    }
}
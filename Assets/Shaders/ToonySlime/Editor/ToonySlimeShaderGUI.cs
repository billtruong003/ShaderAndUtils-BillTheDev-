using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class ToonySlimeShaderGUI : ShaderGUI
{
    private MaterialEditor materialEditor;
    private Dictionary<string, MaterialProperty> propertiesCache;

    public override void OnGUI(MaterialEditor editor, MaterialProperty[] properties)
    {
        this.materialEditor = editor;
        CacheProperties(properties);

        DrawBanner();
        DrawCoreProperties();
    }

    private void CacheProperties(MaterialProperty[] properties)
    {
        propertiesCache = new Dictionary<string, MaterialProperty>();
        foreach (var prop in properties)
        {
            propertiesCache[prop.name] = prop;
        }
    }

    private void DrawBanner()
    {
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            stretchWidth = true,
        };
        GUILayout.Label("Toony Slime Shader", titleStyle);

        EditorGUILayout.HelpBox(
            "Shader cho hiệu ứng chất nhờn/slime phong cách toony.\n" +
            "Để có kết quả tốt nhất, hãy đảm bảo trong Universal Render Pipeline Asset của bạn:\n" +
            "1. 'Opaque Texture' đã được BẬT.\n" +
            "2. 'Depth Texture' đã được BẬT.",
            MessageType.Info
        );
        EditorGUILayout.Space(10);
    }

    private void DrawCoreProperties()
    {
        DrawPropertyGroup("Base Properties", "Cài đặt màu sắc và texture cơ bản.",
            new[] { "_BaseMap", "_ColorTint" });

        DrawPropertyGroup("Refraction & Transparency", "Điều khiển hiệu ứng khúc xạ và độ trong suốt bề mặt.",
            new[] { "_RefractionStrength", "_SurfaceTransparency" });

        DrawPropertyGroup("Depth Effect", "Màu sắc và độ trong thay đổi theo độ sâu.",
            new[] { "_DepthColor", "_MaxDepth", "_DepthTransparency" });

        DrawPropertyGroup("Slime Animation (Noise)", "Điều khiển chuyển động phập phồng, hữu cơ của slime.",
            new[] { "_NoiseScale", "_NoiseSpeed", "_NoiseAmplitude" });

        DrawPropertyGroup("Internal Bubbles", "Mô phỏng bong bóng hoặc tạp chất bên trong slime.",
            new[] { "_BubbleMap", "_BubbleScale", "_BubbleSpeed", "_BubbleDensity" });

        DrawPropertyGroup("Toon Style & Wetness", "Tinh chỉnh phong cách toony (đổ bóng, viền sáng) và độ bóng ướt.",
            new[] { "_ToonThreshold", "_SSSStrength", "_SpecularColor", "_Shininess", "_RimColor", "_RimPower" });

        DrawPropertyGroup("Emission", "Kiểm soát ánh sáng tự phát.",
            new[] { "_EmissionColor" });
    }

    private void DrawPropertyGroup(string title, string description, string[] propertyNames)
    {
        GUILayout.Label(title, EditorStyles.boldLabel);
        if (!string.IsNullOrEmpty(description))
        {
            EditorGUILayout.HelpBox(description, MessageType.None);
        }

        EditorGUI.indentLevel++;
        foreach (var name in propertyNames)
        {
            if (propertiesCache.TryGetValue(name, out MaterialProperty prop))
            {
                this.materialEditor.ShaderProperty(prop, prop.displayName);
            }
        }
        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);
    }
}
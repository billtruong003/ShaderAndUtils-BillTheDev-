using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class ToonUberShaderGUI : ShaderGUI
{
    // Enum để code dễ đọc hơn
    public enum SurfaceType
    {
        Opaque = 0,
        Transparent = 1,
        Metallic = 2
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // Lấy material đang được chọn
        Material targetMat = materialEditor.target as Material;

        // Tìm các property chính
        var surfaceTypeProp = FindProperty("_SurfaceType", properties);

        // Bắt đầu kiểm tra thay đổi, để nếu người dùng đổi dropdown, ta cập nhật ngay
        EditorGUI.BeginChangeCheck();

        // Vẽ dropdown chọn chế độ
        materialEditor.ShaderProperty(surfaceTypeProp, "Surface Type");

        // Lấy giá trị hiện tại của dropdown
        SurfaceType surfaceType = (SurfaceType)surfaceTypeProp.floatValue;

        EditorGUILayout.Space();

        // Vẽ các thuộc tính dựa trên chế độ đã chọn
        switch (surfaceType)
        {
            case SurfaceType.Opaque:
                DrawOpaqueProperties(materialEditor, properties);
                break;
            case SurfaceType.Transparent:
                DrawTransparentProperties(materialEditor, properties);
                break;
            case SurfaceType.Metallic:
                DrawMetallicProperties(materialEditor, properties);
                break;
        }

        // Vẽ các thuộc tính chung cho mọi chế độ (Emission)
        EditorGUILayout.Space();
        DrawSharedProperties(materialEditor, properties);

        // Nếu có thay đổi (ví dụ: người dùng chọn chế độ khác)
        if (EditorGUI.EndChangeCheck())
        {
            // Gọi hàm để cài đặt lại render state và keywords
            SetupMaterial(targetMat, surfaceType);
        }
    }

    void DrawOpaqueProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        EditorGUILayout.LabelField("Standard Toon Properties", EditorStyles.boldLabel);
        materialEditor.ShaderProperty(FindProperty("_BaseMap", properties), "Albedo Texture");
        materialEditor.ShaderProperty(FindProperty("_BaseColor", properties), "Base Color");
        EditorGUILayout.Space();
        materialEditor.ShaderProperty(FindProperty("_ToonRampOffset", properties), "Toon Ramp Offset");
        materialEditor.ShaderProperty(FindProperty("_ToonRampSmoothness", properties), "Toon Ramp Smoothness");
        materialEditor.ShaderProperty(FindProperty("_ShadowTint", properties), "Shadow Tint");
    }

    void DrawMetallicProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        EditorGUILayout.LabelField("Stylized Metal Properties", EditorStyles.boldLabel);

        // Thuộc tính cơ bản
        materialEditor.ShaderProperty(FindProperty("_BaseMap", properties), "Albedo Texture");
        materialEditor.ShaderProperty(FindProperty("_BaseColor", properties), "Base Color (Multiply)");

        // Diffuse Ramp
        materialEditor.ShaderProperty(FindProperty("_Ramp", properties), "Toon Ramp (RGB)");

        // Specular
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Specular Layer", EditorStyles.miniBoldLabel);
        materialEditor.ShaderProperty(FindProperty("_Brightness", properties), "Specular Brightness");
        materialEditor.ShaderProperty(FindProperty("_Offset", properties), "Specular Size");
        materialEditor.ShaderProperty(FindProperty("_SpecuColor", properties), "Specular Color");

        // Highlight
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Highlight Layer", EditorStyles.miniBoldLabel);
        materialEditor.ShaderProperty(FindProperty("_HighlightOffset", properties), "Highlight Size");
        materialEditor.ShaderProperty(FindProperty("_HiColor", properties), "Highlight Color");

        // Rim
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rim Light", EditorStyles.miniBoldLabel);
        materialEditor.ShaderProperty(FindProperty("_RimColor", properties), "Rim Color");
        materialEditor.ShaderProperty(FindProperty("_RimPower", properties), "Rim Power");
    }

    void DrawTransparentProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        EditorGUILayout.LabelField("Stylized Glass Properties", EditorStyles.boldLabel);
        materialEditor.ShaderProperty(FindProperty("_GlassColor", properties), "Glass Color & Opacity");
        materialEditor.ShaderProperty(FindProperty("_FresnelColor", properties), "Fresnel (Edge) Color");
        materialEditor.ShaderProperty(FindProperty("_FresnelPower", properties), "Fresnel Power");
        materialEditor.ShaderProperty(FindProperty("_RefractionStrength", properties), "Refraction Strength");
    }

    void DrawSharedProperties(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        EditorGUILayout.LabelField("Shared Properties", EditorStyles.boldLabel);
        materialEditor.ShaderProperty(FindProperty("_EnableEmission", properties), "Enable Emission");
        var emissionToggle = FindProperty("_EnableEmission", properties, false);
        if (emissionToggle != null && emissionToggle.floatValue > 0.5f)
        {
            materialEditor.ShaderProperty(FindProperty("_EmissionColor", properties), "Emission Color");
            materialEditor.ShaderProperty(FindProperty("_EmissionMap", properties), "Emission Map");
        }
    }

    // Hàm quan trọng nhất: Cài đặt render state cho material
    void SetupMaterial(Material material, SurfaceType surfaceType)
    {
        // Tắt hết các keyword cũ để tránh xung đột
        material.DisableKeyword("_SURFACETYPE_OPAQUE");
        material.DisableKeyword("_SURFACETYPE_TRANSPARENT");
        material.DisableKeyword("_SURFACETYPE_METALLIC");

        switch (surfaceType)
        {
            case SurfaceType.Opaque:
            case SurfaceType.Metallic: // Metallic cũng là Opaque
                material.SetInt("_SrcBlend", (int)BlendMode.One);
                material.SetInt("_DstBlend", (int)BlendMode.Zero);
                material.SetInt("_ZWrite", 1);
                material.SetOverrideTag("RenderType", "Opaque");
                material.renderQueue = (int)RenderQueue.Geometry;
                if (surfaceType == SurfaceType.Opaque)
                    material.EnableKeyword("_SURFACETYPE_OPAQUE");
                else
                    material.EnableKeyword("_SURFACETYPE_METALLIC");
                break;

            case SurfaceType.Transparent:
                material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                material.SetInt("_ZWrite", 0); // Không ghi vào depth buffer
                material.SetOverrideTag("RenderType", "Transparent");
                material.renderQueue = (int)RenderQueue.Transparent;
                material.EnableKeyword("_SURFACETYPE_TRANSPARENT");
                break;
        }
    }

    // Hàm này được gọi khi shader được gán lần đầu cho material
    public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
    {
        base.AssignNewShaderToMaterial(material, oldShader, newShader);
        if (material.HasProperty("_SurfaceType"))
        {
            var surfaceType = (SurfaceType)material.GetFloat("_SurfaceType");
            SetupMaterial(material, surfaceType);
        }
    }
}
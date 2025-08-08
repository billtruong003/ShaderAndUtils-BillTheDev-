using UnityEditor;
using UnityEngine;
using Unity.Collections; // Required for ReadOnlySpan

public class StylizedOverlayShaderGUI : ShaderGUI
{
    public enum OverlayType
    {
        Snow, Ice, Crystal
    }

    // --- Material Properties ---
    private MaterialProperty overlayType, overlayDirection, transitionProgress, displacementHeight, transitionHardness;
    private MaterialProperty noiseMap, noiseTiling;
    private MaterialProperty snowBaseColor, snowTopColor, snowToonRamp, snowRimColor, snowRimPower;
    private MaterialProperty iceBaseColor;
    private MaterialProperty enableRefraction, edgeColor, edgeWidth, edgePulseSpeed, edgePulseStrength;
    private MaterialProperty edgeSpecularColor, edgeSpecularPower, edgeRefractionStrength;
    private MaterialProperty crystalCellColor, crystalCellScale, crystalCellHardness, crystalCellJitter;
    private MaterialProperty blingColor, blingScale, blingDensity, blingHardness, blingSpeed;

    private void FindProperties(MaterialProperty[] props)
    {
        overlayType = FindProperty("_OverlayType", props);
        overlayDirection = FindProperty("_OverlayDirection", props);
        transitionProgress = FindProperty("_TransitionProgress", props);
        displacementHeight = FindProperty("_DisplacementHeight", props);
        transitionHardness = FindProperty("_TransitionHardness", props);

        noiseMap = FindProperty("_NoiseMap", props);
        noiseTiling = FindProperty("_NoiseTiling", props);

        snowBaseColor = FindProperty("_SnowBaseColor", props);
        snowTopColor = FindProperty("_SnowTopColor", props);
        snowToonRamp = FindProperty("_SnowToonRamp", props);
        snowRimColor = FindProperty("_SnowRimColor", props);
        snowRimPower = FindProperty("_SnowRimPower", props);

        iceBaseColor = FindProperty("_IceBaseColor", props);

        enableRefraction = FindProperty("_EnableRefraction", props, false);
        edgeColor = FindProperty("_EdgeColor", props);
        edgeWidth = FindProperty("_EdgeWidth", props);
        edgePulseSpeed = FindProperty("_EdgePulseSpeed", props);
        edgePulseStrength = FindProperty("_EdgePulseStrength", props);
        edgeSpecularColor = FindProperty("_EdgeSpecularColor", props);
        edgeSpecularPower = FindProperty("_EdgeSpecularPower", props);
        edgeRefractionStrength = FindProperty("_EdgeRefractionStrength", props);

        crystalCellColor = FindProperty("_CrystalCellColor", props);
        crystalCellScale = FindProperty("_CrystalCellScale", props);
        crystalCellHardness = FindProperty("_CrystalCellHardness", props);
        crystalCellJitter = FindProperty("_CrystalCellJitter", props);

        blingColor = FindProperty("_BlingColor", props);
        blingScale = FindProperty("_BlingScale", props);
        blingDensity = FindProperty("_BlingDensity", props);
        blingHardness = FindProperty("_BlingHardness", props);
        blingSpeed = FindProperty("_BlingSpeed", props);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        FindProperties(props);
        Material material = materialEditor.target as Material;

        EditorGUI.BeginChangeCheck();

        var currentType = (OverlayType)overlayType.floatValue;

        materialEditor.ShaderProperty(overlayType, "Overlay Type");

        DrawHeader("General Overlay Settings");
        DrawGeneralSettings(materialEditor);

        DrawHeader("Breakup Noise");
        DrawNoiseSettings(materialEditor);

        switch (currentType)
        {
            case OverlayType.Snow:
                DrawSnowSettings(materialEditor);
                break;
            case OverlayType.Ice:
                DrawIceSettings(materialEditor);
                break;
            case OverlayType.Crystal:
                DrawCrystalSettings(materialEditor);
                break;
        }

        if (EditorGUI.EndChangeCheck())
        {
            SetMaterialKeywords(material);
        }
    }

    private void DrawHeader(string title)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
    }

    private void DrawGeneralSettings(MaterialEditor editor)
    {
        editor.ShaderProperty(overlayDirection, "Direction");
        editor.ShaderProperty(transitionProgress, "Progress");
        editor.ShaderProperty(displacementHeight, "Displacement");
        editor.ShaderProperty(transitionHardness, "Hardness");
    }

    private void DrawNoiseSettings(MaterialEditor editor)
    {
        editor.TexturePropertySingleLine(new GUIContent("Breakup Noise (R)"), noiseMap);
        editor.ShaderProperty(noiseTiling, "Tiling");
    }

    private void DrawSnowSettings(MaterialEditor editor)
    {
        DrawHeader("Snow Material");
        editor.ShaderProperty(snowBaseColor, "Base Color");
        editor.ShaderProperty(snowTopColor, "Top Color");
        editor.TexturePropertySingleLine(new GUIContent("Toon Ramp"), snowToonRamp);
        editor.ShaderProperty(snowRimColor, "Rim Color");
        editor.ShaderProperty(snowRimPower, "Rim Power");
    }

    private void DrawIceSettings(MaterialEditor editor)
    {
        DrawHeader("Ice Material");
        editor.ShaderProperty(iceBaseColor, "Base Color");
        DrawSharedIceCrystalSettings(editor);
        DrawBlingEffect(editor);
    }

    private void DrawCrystalSettings(MaterialEditor editor)
    {
        DrawHeader("Crystal Material (Overrides Ice)");
        editor.ShaderProperty(iceBaseColor, "Base Color (Inter-cell)");

        DrawHeader("Crystal Specific (Voronoi)");
        editor.ShaderProperty(crystalCellColor, "Cell Color");
        editor.ShaderProperty(crystalCellScale, "Cell Scale");
        editor.ShaderProperty(crystalCellHardness, "Cell Hardness");
        editor.ShaderProperty(crystalCellJitter, "Cell Jitter");

        DrawSharedIceCrystalSettings(editor);
        DrawBlingEffect(editor);
    }

    private void DrawSharedIceCrystalSettings(MaterialEditor editor)
    {
        DrawHeader("Shared Edge & Specular");
        editor.ShaderProperty(edgeColor, "Edge Color");
        editor.ShaderProperty(edgeWidth, "Edge Width");
        editor.ShaderProperty(edgePulseSpeed, "Edge Pulse Speed");
        editor.ShaderProperty(edgePulseStrength, "Edge Pulse Strength");
        editor.ShaderProperty(edgeSpecularColor, "Specular Color");
        editor.ShaderProperty(edgeSpecularPower, "Specular Power");

        if (enableRefraction != null)
        {
            editor.ShaderProperty(enableRefraction, "Enable Refraction");
            if (enableRefraction.floatValue > 0.5f)
            {
                EditorGUI.indentLevel++;
                editor.ShaderProperty(edgeRefractionStrength, "Refraction Strength");
                EditorGUI.indentLevel--;
            }
        }
    }

    private void DrawBlingEffect(MaterialEditor editor)
    {
        DrawHeader("Shared Bling Effect (Simplex)");
        editor.ColorProperty(blingColor, "Color");
        editor.ShaderProperty(blingScale, "Scale");
        editor.ShaderProperty(blingDensity, "Density Threshold");
        editor.ShaderProperty(blingHardness, "Hardness");
        editor.ShaderProperty(blingSpeed, "Speed");
    }

    private void SetMaterialKeywords(Material material)
    {
        SetKeyword(material, "_REFRACTION_ON", material.GetFloat("_EnableRefraction") > 0.5f);

        var type = (OverlayType)material.GetFloat("_OverlayType");
        SetKeyword(material, "_OVERLAY_TYPE_SNOW", type == OverlayType.Snow);
        SetKeyword(material, "_OVERLAY_TYPE_ICE", type == OverlayType.Ice);
        SetKeyword(material, "_OVERLAY_TYPE_CRYSTAL", type == OverlayType.Crystal);
    }

    private void SetKeyword(Material m, string keyword, bool state)
    {
        if (state)
            m.EnableKeyword(keyword);
        else
            m.DisableKeyword(keyword);
    }
}
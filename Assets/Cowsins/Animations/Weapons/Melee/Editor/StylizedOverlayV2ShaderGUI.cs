using UnityEditor;
using UnityEngine;
using System;

public class StylizedOverlayV3ShaderGUI : ShaderGUI
{
    private MaterialEditor editor;
    private MaterialProperty[] properties;
    private Material material;

    private enum OverlayTypes { SNOW, ICE, CRYSTAL }

    private static class Styles
    {
        public static readonly GUIContent overlayType = new GUIContent("Overlay Type", "Selects the main material type: Snow, Ice, or Crystal.");
        public static readonly GUIContent overlayDirection = new GUIContent("Overlay Direction", "The world-space direction the overlay effect is applied from (e.g., (0,1,0) for top-down snow).");
        public static readonly GUIContent transitionProgress = new GUIContent("Transition Progress", "Controls the overall coverage of the effect.");
        public static readonly GUIContent transitionHardness = new GUIContent("Transition Hardness", "The sharpness of the transition edge.");

        public static readonly string headerGeneral = "General Overlay Settings";
        public static readonly string headerNoise = "Transition Noise (Triplanar)";
        public static readonly string headerDisplacementAndSurface = "Displacement & Surface Detail";
        public static readonly string headerSnow = "Snow Material";
        public static readonly string headerIceCrystal = "Ice & Crystal Shared Settings";
        public static readonly string headerCrystal = "Crystal Specific Voronoi";
        public static readonly string headerBling = "Shared Bling Effect";
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        editor = materialEditor;
        properties = props;
        material = materialEditor.target as Material;

        DrawGeneralSettings();
        DrawNoiseSettings();
        DrawDisplacementAndSurfaceSettings();

        MaterialProperty overlayTypeProp = FindProperty("_OverlayType");
        editor.ShaderProperty(overlayTypeProp, Styles.overlayType);
        SetKeywordForOverlayType((int)overlayTypeProp.floatValue);

        EditorGUILayout.Space();

        switch ((int)overlayTypeProp.floatValue)
        {
            case (int)OverlayTypes.SNOW:
                DrawSnowSettings();
                break;
            case (int)OverlayTypes.ICE:
                DrawIceAndCrystalSharedSettings();
                break;
            case (int)OverlayTypes.CRYSTAL:
                DrawIceAndCrystalSharedSettings();
                DrawCrystalSettings();
                break;
        }

        DrawBlingSettings();
    }

    private void DrawGeneralSettings()
    {
        DrawHeader(Styles.headerGeneral);
        editor.ShaderProperty(FindProperty("_OverlayDirection"), Styles.overlayDirection);
        editor.ShaderProperty(FindProperty("_TransitionProgress"), Styles.transitionProgress);
        editor.ShaderProperty(FindProperty("_TransitionHardness"), Styles.transitionHardness);
        EditorGUILayout.Space();
    }

    private void DrawNoiseSettings()
    {
        DrawHeader(Styles.headerNoise);
        editor.ShaderProperty(FindProperty("_TransitionNoiseMap"), "Noise Map (R)");
        editor.ShaderProperty(FindProperty("_TransitionNoiseScale"), "Noise Scale");
        editor.ShaderProperty(FindProperty("_TransitionNoiseStrength"), "Noise Strength");
        editor.ShaderProperty(FindProperty("_TriplanarFalloff"), "Triplanar Blend");
        EditorGUILayout.Space();
    }

    private void DrawDisplacementAndSurfaceSettings()
    {
        DrawHeader(Styles.headerDisplacementAndSurface);

        MaterialProperty vtxDispToggle = FindProperty("_EnableVertexDisplacement");
        editor.ShaderProperty(vtxDispToggle, vtxDispToggle.displayName);
        SetKeyword("_VERTEX_DISPLACEMENT_ON", vtxDispToggle.floatValue > 0.5f);
        if (vtxDispToggle.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            editor.ShaderProperty(FindProperty("_DisplacementStrength"), "Strength");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();

        MaterialProperty normalMapToggle = FindProperty("_EnableNormalMap");
        editor.ShaderProperty(normalMapToggle, normalMapToggle.displayName);
        SetKeyword("_NORMAL_MAP_ON", normalMapToggle.floatValue > 0.5f);
        if (normalMapToggle.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            editor.TexturePropertySingleLine(new GUIContent("Normal Map"), FindProperty("_BumpMap"), FindProperty("_BumpScale"));
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();

        MaterialProperty pomToggle = FindProperty("_EnablePOM");
        editor.ShaderProperty(pomToggle, pomToggle.displayName);
        SetKeyword("_PARALLAX_OCCLUSION_ON", pomToggle.floatValue > 0.5f);
        if (pomToggle.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            editor.ShaderProperty(FindProperty("_POMHeightMap"), "Height Map (R)");
            editor.ShaderProperty(FindProperty("_POMDepth"), "Depth");
            editor.ShaderProperty(FindProperty("_POMLayers"), "Layers");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
    }

    private void DrawSnowSettings()
    {
        DrawHeader(Styles.headerSnow);
        editor.ShaderProperty(FindProperty("_SnowBaseColor"), "Base Color");
        editor.ShaderProperty(FindProperty("_SnowDeepColor"), "Subsurface Color");
        editor.ShaderProperty(FindProperty("_SnowSubsurfaceFactor"), "Subsurface Power");
        editor.ShaderProperty(FindProperty("_SnowToonRamp"), "Toon Ramp");
        editor.ShaderProperty(FindProperty("_SnowRimColor"), "Rim Color");
        editor.ShaderProperty(FindProperty("_SnowRimPower"), "Rim Power");
        editor.ShaderProperty(FindProperty("_SnowGlitterColor"), "Glitter Color");
        editor.ShaderProperty(FindProperty("_SnowGlitterScale"), "Glitter Scale");
        editor.ShaderProperty(FindProperty("_SnowGlitterDensity"), "Glitter Density");
        editor.ShaderProperty(FindProperty("_SnowGlitterHardness"), "Glitter Hardness");
        EditorGUILayout.Space();
    }

    private void DrawIceAndCrystalSharedSettings()
    {
        DrawHeader(Styles.headerIceCrystal);
        editor.ShaderProperty(FindProperty("_IceBaseColor"), "Base Color");
        editor.ShaderProperty(FindProperty("_IceInternalFogColor"), "Internal Fog Color");
        editor.ShaderProperty(FindProperty("_IceInternalFogDensity"), "Internal Fog Density");

        MaterialProperty refractionToggle = FindProperty("_EnableRefraction");
        editor.ShaderProperty(refractionToggle, refractionToggle.displayName);
        SetKeyword("_REFRACTION_ON", refractionToggle.floatValue > 0.5f);
        if (refractionToggle.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            editor.ShaderProperty(FindProperty("_EdgeRefractionStrength"), "Refraction Strength");
            editor.ShaderProperty(FindProperty("_ChromaticAberration"), "Chromatic Aberration");
            EditorGUI.indentLevel--;
        }

        editor.ShaderProperty(FindProperty("_EdgeColor"), "Edge Color");
        editor.ShaderProperty(FindProperty("_EdgeWidth"), "Edge Width");
        editor.ShaderProperty(FindProperty("_EdgePulseSpeed"), "Edge Pulse Speed");
        editor.ShaderProperty(FindProperty("_EdgePulseStrength"), "Edge Pulse Strength");
        editor.ShaderProperty(FindProperty("_EdgeSpecularColor"), "Specular Color");
        editor.ShaderProperty(FindProperty("_EdgeSpecularPower"), "Specular Power");
        EditorGUILayout.Space();
    }

    private void DrawCrystalSettings()
    {
        DrawHeader(Styles.headerCrystal);
        editor.ShaderProperty(FindProperty("_CrystalCellColor"), "Cell Color");
        editor.ShaderProperty(FindProperty("_CrystalBorderColor"), "Cell Border Color");
        editor.ShaderProperty(FindProperty("_CrystalCellScale"), "Cell Scale");
        editor.ShaderProperty(FindProperty("_CrystalCellBorderWidth"), "Cell Border Width");
        editor.ShaderProperty(FindProperty("_CrystalCellJitter"), "Cell Jitter");
        EditorGUILayout.Space();
    }

    private void DrawBlingSettings()
    {
        DrawHeader(Styles.headerBling);
        editor.ShaderProperty(FindProperty("_BlingColor"), "Bling Color");
        editor.ShaderProperty(FindProperty("_BlingScale"), "Bling Scale");
        editor.ShaderProperty(FindProperty("_BlingDensity"), "Bling Density");
        editor.ShaderProperty(FindProperty("_BlingHardness"), "Bling Hardness");
        editor.ShaderProperty(FindProperty("_BlingSpeed"), "Bling Speed");
        EditorGUILayout.Space();
    }

    private void DrawHeader(string text)
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }

    private void SetKeyword(string keyword, bool enabled)
    {
        if (enabled)
            material.EnableKeyword(keyword);
        else
            material.DisableKeyword(keyword);
    }

    private void SetKeywordForOverlayType(int index)
    {
        string[] overlayNames = Enum.GetNames(typeof(OverlayTypes));
        for (int i = 0; i < overlayNames.Length; i++)
        {
            string keyword = "_OVERLAY_TYPE_" + overlayNames[i].ToUpper();
            if (i == index)
                material.EnableKeyword(keyword);
            else
                material.DisableKeyword(keyword);
        }
    }

    private MaterialProperty FindProperty(string propertyName)
    {
        return FindProperty(propertyName, properties, true);
    }
}
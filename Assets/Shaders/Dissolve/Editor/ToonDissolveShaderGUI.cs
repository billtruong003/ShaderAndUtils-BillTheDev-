using UnityEngine;
using UnityEditor;
using System;

public class ToonDissolveShaderGUI : ShaderGUI
{
    // Dissolve
    private MaterialProperty enableDissolve, dissolveProgress, dissolveType, dissolveMap, dissolveMapTiling, dissolveVector;
    private MaterialProperty dissolveEdgeColor, dissolveEdgeWidth, dissolveEdgeHardness;

    // Swap
    private MaterialProperty enableSwap, swapProgress, swapAlbedo, enableSwapNormal, swapNormalMap;

    // Surface
    private MaterialProperty baseMap, enableNormalMap, bumpMap, bumpScale, enableAlphaClip, cutoff;

    // Shading & Effects
    private MaterialProperty highlightColor, midtoneColor, shadowColor, highlightThreshold, shadowThreshold, rampSmoothness;
    private MaterialProperty enableSpecular, specularColor, specularThreshold, specularSmoothness;
    private MaterialProperty enableRimLight, rimColor, rimPower;

    private bool showDissolveSettings = true;
    private bool showSwapSettings = true;
    private bool showSurfaceSettings = true;
    private bool showShadingSettings = true;
    private bool showEffectsSettings = true;

    private static class Styles
    {
        public static readonly GUIStyle bannerStyle = new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
        public static readonly GUIStyle sectionHeaderStyle = new GUIStyle(EditorStyles.boldLabel);
        public static readonly GUIContent noiseMapLabel = new GUIContent("Noise Map & Tiling");
        public static readonly GUIContent maskMapLabel = new GUIContent("Mask Map");
        public static readonly GUIContent directionalVectorLabel = new GUIContent("Direction (XYZ), Range (W)");
        public static readonly GUIContent sphericalVectorLabel = new GUIContent("Center (XYZ), Radius (W)");
        public static readonly GUIContent baseMapLabel = new GUIContent("Base Map (Albedo)");
        public static readonly GUIContent normalMapLabel = new GUIContent("Normal Map");
        public static readonly GUIContent newNormalMapLabel = new GUIContent("New Normal Map");
    }

    public void FindProperties(MaterialProperty[] props)
    {
        enableDissolve = FindProperty("_EnableDissolve", props);
        dissolveProgress = FindProperty("_DissolveProgress", props);
        dissolveType = FindProperty("_DissolveType", props);
        dissolveMap = FindProperty("_DissolveMap", props);
        dissolveMapTiling = FindProperty("_DissolveMapTiling", props);
        dissolveVector = FindProperty("_DissolveVector", props);
        dissolveEdgeColor = FindProperty("_DissolveEdgeColor", props);
        dissolveEdgeWidth = FindProperty("_DissolveEdgeWidth", props);
        dissolveEdgeHardness = FindProperty("_DissolveEdgeHardness", props);

        enableSwap = FindProperty("_EnableSwap", props);
        swapProgress = FindProperty("_SwapProgress", props);
        swapAlbedo = FindProperty("_SwapAlbedo", props);
        enableSwapNormal = FindProperty("_EnableSwapNormal", props);
        swapNormalMap = FindProperty("_SwapNormalMap", props);

        baseMap = FindProperty("_BaseMap", props);
        enableNormalMap = FindProperty("_EnableNormalMap", props);
        bumpMap = FindProperty("_BumpMap", props);
        bumpScale = FindProperty("_BumpScale", props);
        enableAlphaClip = FindProperty("_EnableAlphaClip", props);
        cutoff = FindProperty("_Cutoff", props);

        highlightColor = FindProperty("_HighlightColor", props);
        midtoneColor = FindProperty("_MidtoneColor", props);
        shadowColor = FindProperty("_ShadowColor", props);
        highlightThreshold = FindProperty("_HighlightThreshold", props);
        shadowThreshold = FindProperty("_ShadowThreshold", props);
        rampSmoothness = FindProperty("_RampSmoothness", props);

        enableSpecular = FindProperty("_EnableSpecular", props);
        specularColor = FindProperty("_SpecularColor", props);
        specularThreshold = FindProperty("_SpecularThreshold", props);
        specularSmoothness = FindProperty("_SpecularSmoothness", props);

        enableRimLight = FindProperty("_EnableRimLight", props);
        rimColor = FindProperty("_RimColor", props);
        rimPower = FindProperty("_RimPower", props);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        FindProperties(properties);
        Material material = materialEditor.target as Material;

        DrawBanner();

        showDissolveSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showDissolveSettings, "Dissolve Effect");
        if (showDissolveSettings) DrawDissolveControls(materialEditor);
        EditorGUILayout.EndFoldoutHeaderGroup();

        showSwapSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSwapSettings, "Texture Swap Effect");
        if (showSwapSettings) DrawSwapControls(materialEditor);
        EditorGUILayout.EndFoldoutHeaderGroup();

        showSurfaceSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showSurfaceSettings, "Surface Properties");
        if (showSurfaceSettings) DrawSurfaceControls(materialEditor);
        EditorGUILayout.EndFoldoutHeaderGroup();

        showShadingSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showShadingSettings, "Toon Shading Ramp");
        if (showShadingSettings) DrawShadingControls(materialEditor);
        EditorGUILayout.EndFoldoutHeaderGroup();

        showEffectsSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showEffectsSettings, "Lighting & Effects");
        if (showEffectsSettings) DrawEffectsControls(materialEditor);
        EditorGUILayout.EndFoldoutHeaderGroup();

        materialEditor.RenderQueueField();
        SetKeywords(material);
    }

    private void DrawBanner()
    {
        EditorGUILayout.LabelField("Advanced Toon Dissolve & Swap", Styles.bannerStyle);
        EditorGUILayout.Space(4);
    }

    private void DrawDissolveControls(MaterialEditor editor)
    {
        editor.ShaderProperty(enableDissolve, "Enable Dissolve");
        if (enableDissolve.floatValue < 0.5f) return;

        EditorGUI.indentLevel++;
        editor.ShaderProperty(dissolveProgress, "Dissolve Progress");
        editor.ShaderProperty(dissolveType, "Dissolve Type");
        DrawDissolveTypeSpecificControls(editor, (int)dissolveType.floatValue);
        editor.ShaderProperty(dissolveEdgeWidth, "Edge Width");
        editor.ShaderProperty(dissolveEdgeHardness, "Edge Hardness");
        editor.ShaderProperty(dissolveEdgeColor, "Edge Color");
        EditorGUI.indentLevel--;
    }

    private void DrawSwapControls(MaterialEditor editor)
    {
        editor.ShaderProperty(enableSwap, "Enable Texture Swap");
        if (enableSwap.floatValue < 0.5f) return;

        EditorGUI.indentLevel++;
        editor.ShaderProperty(swapProgress, "Swap Progress");
        editor.TexturePropertySingleLine(new GUIContent("New Albedo"), swapAlbedo);
        editor.ShaderProperty(enableSwapNormal, "Enable New Normal");
        if (enableSwapNormal.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            editor.TexturePropertySingleLine(Styles.newNormalMapLabel, swapNormalMap);
            EditorGUI.indentLevel--;
        }
        EditorGUI.indentLevel--;
    }

    private void DrawDissolveTypeSpecificControls(MaterialEditor editor, int type)
    {
        EditorGUI.indentLevel++;
        switch (type)
        {
            case 0: editor.TexturePropertySingleLine(Styles.noiseMapLabel, dissolveMap, dissolveMapTiling); break;
            case 1: editor.ShaderProperty(dissolveVector, Styles.directionalVectorLabel); break;
            case 2: editor.ShaderProperty(dissolveVector, Styles.sphericalVectorLabel); break;
            case 3: editor.TexturePropertySingleLine(Styles.maskMapLabel, dissolveMap); break;
            case 4: EditorGUILayout.HelpBox("Transition is driven by the mesh's Red Vertex Color channel.", MessageType.Info); break;
        }
        EditorGUI.indentLevel--;
    }

    private void DrawSurfaceControls(MaterialEditor editor)
    {
        editor.TexturePropertySingleLine(Styles.baseMapLabel, baseMap);
        editor.ShaderProperty(enableNormalMap, "Enable Normal Map");
        if (enableNormalMap.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            editor.TexturePropertySingleLine(Styles.normalMapLabel, bumpMap, bumpScale);
            EditorGUI.indentLevel--;
        }
        editor.ShaderProperty(enableAlphaClip, "Enable Alpha Clipping");
        if (enableAlphaClip.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            editor.ShaderProperty(cutoff, "Alpha Cutoff");
            EditorGUI.indentLevel--;
        }
    }

    private void DrawShadingControls(MaterialEditor editor)
    {
        editor.ShaderProperty(highlightColor, "Highlight");
        editor.ShaderProperty(midtoneColor, "Midtone");
        editor.ShaderProperty(shadowColor, "Shadow");
        EditorGUILayout.Space();
        editor.ShaderProperty(highlightThreshold, "Highlight Threshold");
        editor.ShaderProperty(shadowThreshold, "Shadow Threshold");
        editor.ShaderProperty(rampSmoothness, "Ramp Smoothness");
    }

    private void DrawEffectsControls(MaterialEditor editor)
    {
        editor.ShaderProperty(enableSpecular, "Enable Specular");
        if (enableSpecular.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            editor.ShaderProperty(specularColor, "Color");
            editor.ShaderProperty(specularThreshold, "Threshold");
            editor.ShaderProperty(specularSmoothness, "Smoothness");
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space();
        editor.ShaderProperty(enableRimLight, "Enable Rim Light");
        if (enableRimLight.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            editor.ShaderProperty(rimColor, "Color");
            editor.ShaderProperty(rimPower, "Power");
            EditorGUI.indentLevel--;
        }
    }

    private void SetKeywords(Material material)
    {
        bool dissolveOn = material.GetFloat("_EnableDissolve") > 0.5f;
        CoreUtils.SetKeyword(material, "_DISSOLVE_ON", dissolveOn);

        if (dissolveOn)
        {
            var type = (int)material.GetFloat("_DissolveType");
            CoreUtils.SetKeyword(material, "_DISSOLVE_TYPE_NOISE", type == 0);
            CoreUtils.SetKeyword(material, "_DISSOLVE_TYPE_DIRECTIONAL", type == 1);
            CoreUtils.SetKeyword(material, "_DISSOLVE_TYPE_SPHERICAL", type == 2);
            CoreUtils.SetKeyword(material, "_DISSOLVE_TYPE_MASK", type == 3);
            CoreUtils.SetKeyword(material, "_DISSOLVE_TYPE_VERTEX_COLOR", type == 4);
        }
        else
        {
            CoreUtils.SetKeyword(material, "_DISSOLVE_TYPE_NOISE", false);
            CoreUtils.SetKeyword(material, "_DISSOLVE_TYPE_DIRECTIONAL", false);
            CoreUtils.SetKeyword(material, "_DISSOLVE_TYPE_SPHERICAL", false);
            CoreUtils.SetKeyword(material, "_DISSOLVE_TYPE_MASK", false);
            CoreUtils.SetKeyword(material, "_DISSOLVE_TYPE_VERTEX_COLOR", false);
        }

        bool swapOn = material.GetFloat("_EnableSwap") > 0.5f;
        CoreUtils.SetKeyword(material, "_SWAP_ON", swapOn);

        bool swapNormalOn = swapOn && material.GetFloat("_EnableSwapNormal") > 0.5f;
        CoreUtils.SetKeyword(material, "_SWAP_NORMAL_ON", swapNormalOn);

        CoreUtils.SetKeyword(material, "_NORMALMAP_ON", material.GetFloat("_EnableNormalMap") > 0.5f);
        CoreUtils.SetKeyword(material, "_ALPHATEST_ON", material.GetFloat("_EnableAlphaClip") > 0.5f);
        CoreUtils.SetKeyword(material, "_SPECULAR_ON", material.GetFloat("_EnableSpecular") > 0.5f);
        CoreUtils.SetKeyword(material, "_RIM_LIGHT_ON", material.GetFloat("_EnableRimLight") > 0.5f);
    }

    private static class CoreUtils
    {
        public static void SetKeyword(Material material, string keyword, bool state)
        {
            if (state) material.EnableKeyword(keyword);
            else material.DisableKeyword(keyword);
        }
    }
}
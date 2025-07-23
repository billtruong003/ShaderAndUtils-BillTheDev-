using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

public class AdvancedPortalURPShaderGUI : ShaderGUI
{
    private MaterialEditor _editor;
    private Material _target;

    private enum SurfaceType { Opaque, Transparent }
    private enum WobbleMode { Off, Procedural, TextureBased }
    private enum DistortionMode { Off, SimpleDistortion, ChromaticAberration }

    private class Props
    {
        public MaterialProperty surfaceType, portalColor, portalRadius, noiseTex, noiseTilingAndSpeed;
        public MaterialProperty pullSpeed, spiralStrength, rimColor, rimWidth, edgeSoftness;
        public MaterialProperty wobbleMode, wobbleNoise, wobbleTilingAndSpeed, wobbleAmplitude, wobbleFrequency;
        public MaterialProperty distortionMode, distortionAmount, chromaticAberration;
        public MaterialProperty enableParallax, parallaxDepth, enableSoftIntersection, softIntersectionDistance;
        public MaterialProperty timeScale;
    }

    private Props _props = new Props();

    private bool _showCoreSettings = true;
    private bool _showEdgeSettings = true;
    private bool _showDynamicEffects = true;
    private bool _showTransparentEffects = true;
    private bool _showAnimationSettings = true;

    private void FindProperties(MaterialProperty[] properties)
    {
        _props.surfaceType = FindProperty("_SurfaceType", properties);
        _props.portalColor = FindProperty("_PortalColor", properties);
        _props.portalRadius = FindProperty("_PortalRadius", properties);
        _props.noiseTex = FindProperty("_NoiseTex", properties);
        _props.noiseTilingAndSpeed = FindProperty("_NoiseTilingAndSpeed", properties);
        _props.pullSpeed = FindProperty("_PullSpeed", properties);
        _props.spiralStrength = FindProperty("_SpiralStrength", properties);
        _props.rimColor = FindProperty("_RimColor", properties);
        _props.rimWidth = FindProperty("_RimWidth", properties);
        _props.edgeSoftness = FindProperty("_EdgeSoftness", properties);
        _props.wobbleMode = FindProperty("_WobbleMode", properties);
        _props.wobbleNoise = FindProperty("_WobbleNoise", properties);
        _props.wobbleTilingAndSpeed = FindProperty("_WobbleTilingAndSpeed", properties);
        _props.wobbleAmplitude = FindProperty("_WobbleAmplitude", properties);
        _props.wobbleFrequency = FindProperty("_WobbleFrequency", properties);
        _props.distortionMode = FindProperty("_DistortionMode", properties);
        _props.distortionAmount = FindProperty("_DistortionAmount", properties);
        _props.chromaticAberration = FindProperty("_ChromaticAberration", properties);
        _props.enableParallax = FindProperty("_EnableParallax", properties);
        _props.parallaxDepth = FindProperty("_ParallaxDepth", properties);
        _props.enableSoftIntersection = FindProperty("_EnableSoftIntersection", properties);
        _props.softIntersectionDistance = FindProperty("_SoftIntersectionDistance", properties);
        _props.timeScale = FindProperty("_TimeScale", properties);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        _editor = materialEditor;
        _target = materialEditor.target as Material;
        FindProperties(properties);

        DrawHeader("Portal Settings");
        DrawRenderMode();

        _showCoreSettings = DrawGroupHeader("Portal Core", _showCoreSettings);
        if (_showCoreSettings) DrawCoreSettings();

        _showEdgeSettings = DrawGroupHeader("Edge and Rim", _showEdgeSettings);
        if (_showEdgeSettings) DrawEdgeSettings();

        _showDynamicEffects = DrawGroupHeader("Dynamic Effects", _showDynamicEffects);
        if (_showDynamicEffects) DrawDynamicEffects();

        if ((SurfaceType)_props.surfaceType.floatValue == SurfaceType.Transparent)
        {
            _showTransparentEffects = DrawGroupHeader("Transparent Effects", _showTransparentEffects);
            if (_showTransparentEffects) DrawTransparentEffects();
        }

        _showAnimationSettings = DrawGroupHeader("Animation", _showAnimationSettings);
        if (_showAnimationSettings) DrawAnimationSettings();

        EditorGUILayout.Space();
        DrawHeader("Advanced Options");
        _editor.RenderQueueField();
        _editor.EnableInstancingField();
    }

    private void DrawRenderMode()
    {
        EditorGUI.BeginChangeCheck();
        var newMode = (SurfaceType)EditorGUILayout.EnumPopup("Render Mode", (SurfaceType)_props.surfaceType.floatValue);
        if (EditorGUI.EndChangeCheck())
        {
            _props.surfaceType.floatValue = (float)newMode;
            SetupMaterialForSurfaceType(newMode);
        }
    }

    private void SetupMaterialForSurfaceType(SurfaceType surfaceType)
    {
        if (surfaceType == SurfaceType.Transparent)
        {
            _target.SetOverrideTag("RenderType", "Transparent");
            _target.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _target.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _target.SetInt("_ZWrite", 0);
            _target.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            SetKeyword("_SURFACE_TYPE_TRANSPARENT", true);
            SetKeyword("_SURFACE_TYPE_OPAQUE", false);
        }
        else
        {
            _target.SetOverrideTag("RenderType", "Opaque");
            _target.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            _target.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            _target.SetInt("_ZWrite", 1);
            _target.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            SetKeyword("_SURFACE_TYPE_TRANSPARENT", false);
            SetKeyword("_SURFACE_TYPE_OPAQUE", true);
        }
    }

    private void DrawCoreSettings()
    {
        _editor.ShaderProperty(_props.portalColor, "Core Color");
        _editor.ShaderProperty(_props.portalRadius, "Radius");
        _editor.TexturePropertySingleLine(new GUIContent("Spiral Noise"), _props.noiseTex);
        _editor.ShaderProperty(_props.noiseTilingAndSpeed, "Tiling & Speed");
    }

    private void DrawEdgeSettings()
    {
        _editor.ShaderProperty(_props.rimColor, "Rim Color");
        _editor.ShaderProperty(_props.rimWidth, "Rim Width");
        _editor.ShaderProperty(_props.edgeSoftness, "Edge Softness");
    }

    private void DrawDynamicEffects()
    {
        DrawEnumProperty<WobbleMode>(_props.wobbleMode, "Wobble Mode", "_WOBBLEMODE");
        var wobbleMode = (WobbleMode)_props.wobbleMode.floatValue;
        if (wobbleMode != WobbleMode.Off)
        {
            EditorGUI.indentLevel++;
            _editor.ShaderProperty(_props.wobbleAmplitude, "Amplitude");
            if (wobbleMode == WobbleMode.Procedural)
            {
                _editor.ShaderProperty(_props.wobbleFrequency, "Frequency");
            }
            if (wobbleMode == WobbleMode.TextureBased)
            {
                _editor.TexturePropertySingleLine(new GUIContent("Noise Texture"), _props.wobbleNoise);
                _editor.ShaderProperty(_props.wobbleTilingAndSpeed, "Tiling & Speed");
            }
            EditorGUI.indentLevel--;
        }
    }

    private void DrawTransparentEffects()
    {
        DrawEnumProperty<DistortionMode>(_props.distortionMode, "Distortion Mode", "_DISTORTIONMODE");
        var distortionMode = (DistortionMode)_props.distortionMode.floatValue;
        if (distortionMode != DistortionMode.Off)
        {
            EditorGUI.indentLevel++;
            _editor.ShaderProperty(_props.distortionAmount, "Amount");
            if (distortionMode == DistortionMode.ChromaticAberration)
            {
                _editor.ShaderProperty(_props.chromaticAberration, "Aberration Amount");
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        _editor.ShaderProperty(_props.enableParallax, "View Parallax");
        if (_props.enableParallax.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            _editor.ShaderProperty(_props.parallaxDepth, "Parallax Depth");
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space();
        _editor.ShaderProperty(_props.enableSoftIntersection, "Soft Intersection");
        if (_props.enableSoftIntersection.floatValue > 0.5f)
        {
            EditorGUI.indentLevel++;
            _editor.ShaderProperty(_props.softIntersectionDistance, "Fade Distance");
            EditorGUILayout.HelpBox("This feature requires the URP Renderer Asset to have the 'Depth Texture' enabled.", MessageType.Info);
            EditorGUI.indentLevel--;
        }
    }

    private void DrawAnimationSettings()
    {
        _editor.ShaderProperty(_props.pullSpeed, "Inward Pull Speed");
        _editor.ShaderProperty(_props.spiralStrength, "Spiral Strength");
        _editor.ShaderProperty(_props.timeScale, "Global Time Scale");
    }

    private void SetKeyword(string keyword, bool state)
    {
        if (state) _target.EnableKeyword(keyword); else _target.DisableKeyword(keyword);
    }

    private void DrawEnumProperty<T>(MaterialProperty property, string label, string keywordPrefix) where T : Enum
    {
        EditorGUI.BeginChangeCheck();
        var newValue = (T)EditorGUILayout.EnumPopup(label, (T)Enum.ToObject(typeof(T), (int)property.floatValue));
        if (EditorGUI.EndChangeCheck())
        {
            property.floatValue = Convert.ToSingle(newValue);
            string[] enumNames = Enum.GetNames(typeof(T));
            foreach (string name in enumNames)
            {
                SetKeyword(keywordPrefix + "_" + name.ToUpperInvariant(), name == newValue.ToString());
            }
        }
    }

    private static void DrawHeader(string title)
    {
        var style = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14, alignment = TextAnchor.MiddleLeft };
        EditorGUILayout.LabelField(title, style, GUILayout.Height(20));
        EditorGUILayout.Space(2);
    }

    private static bool DrawGroupHeader(string title, bool state)
    {
        var style = new GUIStyle(EditorStyles.foldoutHeader) { fontStyle = FontStyle.Bold };
        return EditorGUILayout.Foldout(state, title, true, style);
    }
}
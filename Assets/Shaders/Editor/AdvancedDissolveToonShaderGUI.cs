using UnityEditor;
using UnityEngine;

// Đảm bảo script này là trình chỉnh sửa cho shader có tên "Custom/URP/AdvancedDissolveToonShader"
public class AdvancedDissolveToonShaderGUI : ShaderGUI
{
    private MaterialEditor materialEditor;
    private MaterialProperty[] properties;

    // References to shader properties
    // Main Settings
    private MaterialProperty _MainTex;
    private MaterialProperty _ToonRampSmoothness;
    private MaterialProperty _ToonRampOffset;
    private MaterialProperty _ToonRampTinting;
    private MaterialProperty _AmbientColor;

    // Dissolve Settings
    private MaterialProperty _NoiseTex;
    private MaterialProperty _DissolveThreshold;
    private MaterialProperty _EdgeWidth;
    private MaterialProperty _EdgeColor;
    private MaterialProperty _NoiseStrength;
    private MaterialProperty _UseTimeAnimation;
    private MaterialProperty _TimeScale;
    private MaterialProperty _ZWrite;
    private MaterialProperty _DissolveType;

    // Type Specific
    private MaterialProperty _Direction;
    private MaterialProperty _PatternType;
    private MaterialProperty _PatternFrequency;
    private MaterialProperty _AlphaFadeRange; // New

    // Vertex Displacement / Shatter Effect
    private MaterialProperty _LocalSpace;
    private MaterialProperty _VertexDisplacement;
    private MaterialProperty _ShatterStrength;
    private MaterialProperty _ShatterLiftSpeed;
    private MaterialProperty _ShatterOffsetStrength;
    private MaterialProperty _ShatterTriggerRange;

    // Helper method to find properties
    private MaterialProperty FindProp(string name)
    {
        return FindProperty(name, properties);
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        this.materialEditor = materialEditor;
        this.properties = properties;

        // Initialize properties
        _MainTex = FindProp("_MainTex");
        _ToonRampSmoothness = FindProp("_ToonRampSmoothness");
        _ToonRampOffset = FindProp("_ToonRampOffset");
        _ToonRampTinting = FindProp("_ToonRampTinting");
        _AmbientColor = FindProp("_AmbientColor");

        _NoiseTex = FindProp("_NoiseTex");
        _DissolveThreshold = FindProp("_DissolveThreshold");
        _EdgeWidth = FindProp("_EdgeWidth");
        _EdgeColor = FindProp("_EdgeColor");
        _NoiseStrength = FindProp("_NoiseStrength");
        _UseTimeAnimation = FindProp("_UseTimeAnimation");
        _TimeScale = FindProp("_TimeScale");
        _ZWrite = FindProp("_ZWrite");
        _DissolveType = FindProp("_DissolveType");

        _Direction = FindProp("_Direction");
        _PatternType = FindProp("_PatternType");
        _PatternFrequency = FindProp("_PatternFrequency");
        _AlphaFadeRange = FindProp("_AlphaFadeRange"); // New

        _LocalSpace = FindProp("_LocalSpace");
        _VertexDisplacement = FindProp("_VertexDisplacement");
        _ShatterStrength = FindProp("_ShatterStrength");
        _ShatterLiftSpeed = FindProp("_ShatterLiftSpeed");
        _ShatterOffsetStrength = FindProp("_ShatterOffsetStrength");
        _ShatterTriggerRange = FindProp("_ShatterTriggerRange");


        // --- Drawing the UI ---

        // Main Settings
        EditorGUILayout.LabelField("Main Settings", EditorStyles.boldLabel);
        materialEditor.TexturePropertySingleLine(new GUIContent("Base Texture", "Main albedo texture."), _MainTex);
        materialEditor.ShaderProperty(_ToonRampSmoothness, _ToonRampSmoothness.displayName);
        materialEditor.ShaderProperty(_ToonRampOffset, _ToonRampOffset.displayName);
        materialEditor.ShaderProperty(_ToonRampTinting, _ToonRampTinting.displayName);
        materialEditor.ShaderProperty(_AmbientColor, _AmbientColor.displayName);
        EditorGUILayout.Space(10);


        // Dissolve Settings
        EditorGUILayout.LabelField("Dissolve Settings", EditorStyles.boldLabel);
        materialEditor.TexturePropertySingleLine(new GUIContent("Noise Texture", "Used for noise-based dissolve and perturbation."), _NoiseTex);
        materialEditor.ShaderProperty(_NoiseStrength, _NoiseStrength.displayName);

        // Dissolve Type dropdown
        EditorGUI.BeginChangeCheck();
        int dissolveTypeInt = _DissolveType.intValue;
        string[] dissolveTypeNames = new string[] { "Noise", "Linear Gradient", "Radial Gradient", "Pattern", "Alpha Blend", "Shatter" };
        dissolveTypeInt = EditorGUILayout.Popup(new GUIContent("Dissolve Type", "Select the type of dissolve effect."), dissolveTypeInt, dissolveTypeNames);
        if (EditorGUI.EndChangeCheck())
        {
            _DissolveType.intValue = dissolveTypeInt;
        }

        materialEditor.ShaderProperty(_DissolveThreshold, _DissolveThreshold.displayName);

        // Edge properties
        materialEditor.ShaderProperty(_EdgeWidth, _EdgeWidth.displayName);
        materialEditor.ShaderProperty(_EdgeColor, _EdgeColor.displayName);

        // Time Animation Toggle
        bool useTimeAnimationToggle = _UseTimeAnimation.floatValue > 0.5f;
        EditorGUI.BeginChangeCheck();
        useTimeAnimationToggle = EditorGUILayout.Toggle(_UseTimeAnimation.displayName, useTimeAnimationToggle);
        if (EditorGUI.EndChangeCheck())
        {
            _UseTimeAnimation.floatValue = useTimeAnimationToggle ? 1.0f : 0.0f;
        }
        if (useTimeAnimationToggle)
        {
            EditorGUI.indentLevel++;
            materialEditor.ShaderProperty(_TimeScale, _TimeScale.displayName);
            EditorGUI.indentLevel--;
        }

        // ZWrite Toggle
        bool zWriteToggle = _ZWrite.floatValue > 0.5f;
        EditorGUI.BeginChangeCheck();
        zWriteToggle = EditorGUILayout.Toggle(_ZWrite.displayName, zWriteToggle);
        if (EditorGUI.EndChangeCheck())
        {
            _ZWrite.floatValue = zWriteToggle ? 1.0f : 0.0f;
        }
        EditorGUILayout.Space(10);


        // Type-specific settings
        EditorGUILayout.LabelField("Dissolve Type Specific Settings", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        // Linear Gradient / Shatter Direction
        if (_DissolveType.intValue == 1 || _DissolveType.intValue == 5) // Linear Gradient or Shatter
        {
            materialEditor.ShaderProperty(_Direction, _Direction.displayName);
        }

        // Pattern Specific
        if (_DissolveType.intValue == 3) // Pattern
        {
            EditorGUI.BeginChangeCheck();
            int patternTypeInt = _PatternType.intValue;
            string[] patternTypeNames = new string[] { "SinCos", "Checker", "Grid" };
            patternTypeInt = EditorGUILayout.Popup(new GUIContent("Pattern Type", "Select the type of pattern."), patternTypeInt, patternTypeNames);
            if (EditorGUI.EndChangeCheck())
            {
                _PatternType.intValue = patternTypeInt;
            }
            materialEditor.ShaderProperty(_PatternFrequency, _PatternFrequency.displayName);
        }

        // Alpha Blend Specific
        if (_DissolveType.intValue == 4) // Alpha Blend
        {
            materialEditor.ShaderProperty(_AlphaFadeRange, _AlphaFadeRange.displayName);
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.Space(10);


        // Vertex Displacement / Shatter Effect
        EditorGUILayout.LabelField("Vertex Displacement / Shatter Effect", EditorStyles.boldLabel);

        // Local Space Toggle & Keyword
        bool localSpaceToggle = _LocalSpace.floatValue > 0.5f;
        EditorGUI.BeginChangeCheck();
        localSpaceToggle = EditorGUILayout.Toggle(_LocalSpace.displayName, localSpaceToggle);
        if (EditorGUI.EndChangeCheck())
        {
            _LocalSpace.floatValue = localSpaceToggle ? 1.0f : 0.0f;
            foreach (Material m in materialEditor.targets)
            {
                if (localSpaceToggle)
                    m.EnableKeyword("LOCAL_ON");
                else
                    m.DisableKeyword("LOCAL_ON");
            }
        }

        materialEditor.ShaderProperty(_VertexDisplacement, _VertexDisplacement.displayName);

        // Shatter Effect specific
        if (_DissolveType.intValue == 5) // Shatter
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("Shatter Properties", EditorStyles.boldLabel);
            materialEditor.ShaderProperty(_ShatterStrength, _ShatterStrength.displayName);
            materialEditor.ShaderProperty(_ShatterLiftSpeed, _ShatterLiftSpeed.displayName);
            materialEditor.ShaderProperty(_ShatterOffsetStrength, _ShatterOffsetStrength.displayName);
            materialEditor.ShaderProperty(_ShatterTriggerRange, _ShatterTriggerRange.displayName);
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.Space(10);

        // Render queue and other advanced options at the bottom, just like standard shaders
        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
        materialEditor.DoubleSidedGIField();
    }
}
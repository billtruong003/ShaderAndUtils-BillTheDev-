using UnityEngine;
using UnityEditor;
using System.IO;

public class TextureBakerProWindow : EditorWindow
{
    private enum BakeMode
    {
        PbrLit,
        ColorReplacement,
        EmissionFromHue,
        EmissionFromIdMap,
        HeightToNormal,
        HeightToCurvature,
        HeightToAmbientOcclusion,
        HeightToMetallic
    }

    private BakeMode currentMode = BakeMode.PbrLit;
    private Vector2 scrollPosition;

    private Material sourceMaterial;
    private Texture2D albedoMap, normalMap, metallicMap, roughnessMap, aoMap, idMap, heightMap;

    private string saveFolderPath = "Assets/BakedTextures";
    private string outputFileName = "BakedTexture.png";
    private int outputResolution = 1024;
    private readonly string[] resolutionOptions = { "256", "512", "1024", "2048", "4096" };

    private Material pbrBakerMaterial, textureProcessorMaterial, heightProcessorMaterial;

    private Vector3 lightDirection = new Vector3(-0.5f, -0.8f, -0.2f);
    private Color lightColor = Color.white;
    private float lightIntensity = 1.5f;
    private Color ambientLightColor = new Color(0.2f, 0.2f, 0.2f, 1.0f);

    private Color targetColor = Color.red;
    private Color replacementColor = Color.cyan;
    private float colorDifferenceTolerance = 10f;
    private float transitionSoftness = 2f;

    private Color hueTargetColor = Color.red;
    private float hueThreshold = 0.05f;
    private float saturationThreshold = 0.2f;

    private Color targetIdColor = Color.red;
    private float idTolerance = 0.01f;

    [ColorUsage(false, true)]
    private Color emissionColor = Color.yellow;
    private float emissionIntensity = 5.0f;

    private float normalStrength = 5.0f;
    private float curvatureStrength = 1.0f;
    private float curvatureRadius = 1.0f;
    private int aoSamples = 32;
    private float aoRadius = 10.0f;
    private float aoStrength = 2.0f;
    private float metallicHigh = 1.0f;
    private float metallicLow = 0.0f;
    private float metallicContrast = 1.0f;

    [MenuItem("Tools/ShaderAndUtils/Texture Baker Pro")]
    public static void ShowWindow()
    {
        GetWindow<TextureBakerProWindow>("Texture Baker Pro");
    }

    private void OnEnable()
    {
        FindAndCreateMaterials();
        OnSelectionChange();
    }

    private void OnDisable()
    {
        DestroyImmediate(pbrBakerMaterial);
        DestroyImmediate(textureProcessorMaterial);
        DestroyImmediate(heightProcessorMaterial);
    }

    private void FindAndCreateMaterials()
    {
        pbrBakerMaterial = new Material(Shader.Find("ShaderAndUtils/Internal/PbrLitBaker"));
        textureProcessorMaterial = new Material(Shader.Find("ShaderAndUtils/TextureProcessor"));
        heightProcessorMaterial = new Material(Shader.Find("ShaderAndUtils/Internal/HeightProcessor"));
    }

    private void OnSelectionChange()
    {
        if (Selection.activeObject is Material mat)
        {
            sourceMaterial = mat;
            PopulateMapsFromMaterial(mat);
            UpdateDefaultFileName();
            Repaint();
        }
        else if (Selection.activeObject is Texture2D tex)
        {
            if (tex.name.ToLower().Contains("height"))
            {
                heightMap = tex;
                if (currentMode < BakeMode.HeightToNormal) currentMode = BakeMode.HeightToNormal;
            }
            else
            {
                albedoMap = tex;
            }
            UpdateDefaultFileName();
            Repaint();
        }
    }

    private void PopulateMapsFromMaterial(Material mat)
    {
        if (mat.HasProperty("_MainTex")) albedoMap = mat.GetTexture("_MainTex") as Texture2D;
        if (mat.HasProperty("_BaseMap")) albedoMap = mat.GetTexture("_BaseMap") as Texture2D;
        if (mat.HasProperty("_BumpMap")) normalMap = mat.GetTexture("_BumpMap") as Texture2D;
        if (mat.HasProperty("_MetallicGlossMap")) metallicMap = mat.GetTexture("_MetallicGlossMap") as Texture2D;
        if (mat.HasProperty("_SpecGlossMap")) roughnessMap = mat.GetTexture("_SpecGlossMap") as Texture2D;
        if (mat.HasProperty("_OcclusionMap")) aoMap = mat.GetTexture("_OcclusionMap") as Texture2D;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Texture Baker Pro", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawBakeModeSelection();
        EditorGUILayout.Space();
        DrawInputsGroup();
        EditorGUILayout.Space();
        DrawConfigurationGroup();
        EditorGUILayout.Space();
        DrawOutputSettingsGroup();
        EditorGUILayout.Space();
        DrawExecutionButtonGroup();

        EditorGUILayout.EndScrollView();
    }

    private void DrawBakeModeSelection()
    {
        EditorGUI.BeginChangeCheck();
        currentMode = (BakeMode)EditorGUILayout.EnumPopup("Bake Mode", currentMode);
        if (EditorGUI.EndChangeCheck())
        {
            UpdateDefaultFileName();
        }
    }

    private void DrawInputsGroup()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Input Maps", EditorStyles.boldLabel);

        bool isHeightMode = currentMode >= BakeMode.HeightToNormal;
        if (isHeightMode)
        {
            heightMap = (Texture2D)EditorGUILayout.ObjectField("Height Map", heightMap, typeof(Texture2D), false);
        }
        else
        {
            albedoMap = (Texture2D)EditorGUILayout.ObjectField("Albedo Map", albedoMap, typeof(Texture2D), false);
            if (currentMode == BakeMode.PbrLit)
            {
                normalMap = (Texture2D)EditorGUILayout.ObjectField("Normal Map", normalMap, typeof(Texture2D), false);
                metallicMap = (Texture2D)EditorGUILayout.ObjectField("Metallic Map", metallicMap, typeof(Texture2D), false);
                roughnessMap = (Texture2D)EditorGUILayout.ObjectField("Roughness Map", roughnessMap, typeof(Texture2D), false);
                aoMap = (Texture2D)EditorGUILayout.ObjectField("Occlusion Map", aoMap, typeof(Texture2D), false);
            }
            else if (currentMode == BakeMode.EmissionFromIdMap)
            {
                idMap = (Texture2D)EditorGUILayout.ObjectField("ID Map", idMap, typeof(Texture2D), false);
            }
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawConfigurationGroup()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);

        switch (currentMode)
        {
            case BakeMode.PbrLit: DrawPbrLitConfig(); break;
            case BakeMode.ColorReplacement: DrawColorReplacementConfig(); break;
            case BakeMode.EmissionFromHue: DrawEmissionFromHueConfig(); break;
            case BakeMode.EmissionFromIdMap: DrawEmissionFromIdConfig(); break;
            case BakeMode.HeightToNormal: normalStrength = EditorGUILayout.Slider("Normal Strength", normalStrength, 1.0f, 50.0f); break;
            case BakeMode.HeightToCurvature:
                curvatureRadius = EditorGUILayout.FloatField("Sample Radius (Pixels)", curvatureRadius);
                curvatureStrength = EditorGUILayout.Slider("Curvature Strength", curvatureStrength, 0.1f, 10.0f);
                break;
            case BakeMode.HeightToAmbientOcclusion:
                aoSamples = EditorGUILayout.IntSlider("Sample Count", aoSamples, 4, 128);
                aoRadius = EditorGUILayout.Slider("Sample Radius (Pixels)", aoRadius, 1, 100);
                aoStrength = EditorGUILayout.Slider("Occlusion Strength", aoStrength, 0.1f, 10.0f);
                break;
            case BakeMode.HeightToMetallic:
                metallicLow = EditorGUILayout.Slider("Metallic For Low Areas", metallicLow, 0.0f, 1.0f);
                metallicHigh = EditorGUILayout.Slider("Metallic For High Areas", metallicHigh, 0.0f, 1.0f);
                metallicContrast = EditorGUILayout.Slider("Contrast", metallicContrast, 0.1f, 5.0f);
                break;
        }
        EditorGUILayout.EndVertical();
    }

    private void DrawPbrLitConfig()
    {
        lightDirection = EditorGUILayout.Vector3Field("Light Direction", lightDirection);
        lightColor = EditorGUILayout.ColorField(new GUIContent("Light Color"), lightColor, true, false, true);
        lightIntensity = EditorGUILayout.FloatField("Light Intensity", lightIntensity);
        ambientLightColor = EditorGUILayout.ColorField("Ambient Color", ambientLightColor);
    }

    private void DrawColorReplacementConfig()
    {
        targetColor = EditorGUILayout.ColorField("Target Color", targetColor);
        replacementColor = EditorGUILayout.ColorField("Replacement Color", replacementColor);
        colorDifferenceTolerance = EditorGUILayout.Slider("Tolerance (Delta E)", colorDifferenceTolerance, 0f, 100f);
        transitionSoftness = EditorGUILayout.Slider("Transition Softness", transitionSoftness, 0.01f, 20f);
    }

    private void DrawEmissionFromHueConfig()
    {
        hueTargetColor = EditorGUILayout.ColorField("Hue Target Color", hueTargetColor);
        hueThreshold = EditorGUILayout.Slider("Hue Threshold", hueThreshold, 0f, 0.5f);
        saturationThreshold = EditorGUILayout.Slider("Saturation Threshold", saturationThreshold, 0f, 1f);
        DrawSharedEmissionProperties();
    }

    private void DrawEmissionFromIdConfig()
    {
        targetIdColor = EditorGUILayout.ColorField("Target ID Color", targetIdColor);
        idTolerance = EditorGUILayout.Slider("ID Match Tolerance", idTolerance, 0f, 0.1f);
        DrawSharedEmissionProperties();
    }

    private void DrawSharedEmissionProperties()
    {
        emissionColor = EditorGUILayout.ColorField(new GUIContent("Emission Color"), emissionColor, true, false, true);
        emissionIntensity = EditorGUILayout.FloatField("Emission Intensity", emissionIntensity);
    }

    private void DrawOutputSettingsGroup()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);

        int resolutionIndex = System.Array.IndexOf(resolutionOptions, outputResolution.ToString());
        int newResolutionIndex = EditorGUILayout.Popup("Output Resolution", resolutionIndex, resolutionOptions);
        if (resolutionIndex != newResolutionIndex)
        {
            outputResolution = int.Parse(resolutionOptions[newResolutionIndex]);
        }

        EditorGUILayout.BeginHorizontal();
        saveFolderPath = EditorGUILayout.TextField("Save Folder", saveFolderPath);
        if (GUILayout.Button("Browse", GUILayout.Width(80))) BrowseForSaveFolder();
        EditorGUILayout.EndHorizontal();

        outputFileName = EditorGUILayout.TextField("File Name", outputFileName);
        EditorGUILayout.EndVertical();
    }

    private void DrawExecutionButtonGroup()
    {
        GUI.enabled = IsConfigurationValid();
        if (GUILayout.Button($"Bake {currentMode}", GUILayout.Height(40)))
        {
            ExecuteBake();
        }
        GUI.enabled = true;
    }

    private bool IsConfigurationValid()
    {
        if (string.IsNullOrWhiteSpace(outputFileName) || string.IsNullOrWhiteSpace(saveFolderPath)) return false;
        bool isHeightMode = currentMode >= BakeMode.HeightToNormal;
        if (isHeightMode) return heightMap != null;
        if (currentMode == BakeMode.EmissionFromIdMap) return albedoMap != null && idMap != null;
        return albedoMap != null;
    }

    private void UpdateDefaultFileName()
    {
        string baseName = "Baked";
        bool isHeightMode = currentMode >= BakeMode.HeightToNormal;

        if (isHeightMode && heightMap != null)
        {
            baseName = Path.GetFileNameWithoutExtension(heightMap.name);
        }
        else if (!isHeightMode && albedoMap != null)
        {
            baseName = Path.GetFileNameWithoutExtension(albedoMap.name);
        }
        else if (sourceMaterial != null)
        {
            baseName = sourceMaterial.name;
        }
        outputFileName = $"{baseName}_{currentMode}.png";
    }

    private void BrowseForSaveFolder()
    {
        string absPath = EditorUtility.SaveFolderPanel("Select Save Folder", "Assets", "");
        if (string.IsNullOrEmpty(absPath)) return;

        if (absPath.StartsWith(Application.dataPath))
        {
            saveFolderPath = "Assets" + absPath.Substring(Application.dataPath.Length);
        }
        else
        {
            EditorUtility.DisplayDialog("Invalid Path", "Please select a folder inside the project's 'Assets' directory.", "OK");
        }
    }

    private void ExecuteBake()
    {
        string fullPath = Path.Combine(saveFolderPath, outputFileName);
        if (!File.Exists(fullPath) || EditorUtility.DisplayDialog("Confirm Overwrite", $"File '{Path.GetFileName(fullPath)}' already exists. Overwrite?", "Overwrite", "Cancel"))
        {
            switch (currentMode)
            {
                case BakeMode.PbrLit: BakePbrLit(fullPath); break;
                case BakeMode.ColorReplacement: BakeTextureProcessor(fullPath, isNormal: false); break;
                case BakeMode.EmissionFromHue: BakeTextureProcessor(fullPath, isNormal: false); break;
                case BakeMode.EmissionFromIdMap: BakeTextureProcessor(fullPath, isNormal: false); break;
                case BakeMode.HeightToNormal: BakeHeightProcessor(fullPath, isNormal: true); break;
                case BakeMode.HeightToCurvature: BakeHeightProcessor(fullPath, isNormal: false); break;
                case BakeMode.HeightToAmbientOcclusion: BakeHeightProcessor(fullPath, isNormal: false); break;
                case BakeMode.HeightToMetallic: BakeHeightProcessor(fullPath, isNormal: false); break;
            }
        }
    }

    private void BakePbrLit(string path)
    {
        pbrBakerMaterial.SetTexture("_AlbedoMap", albedoMap);
        pbrBakerMaterial.SetTexture("_NormalMap", normalMap ? normalMap : Texture2D.normalTexture);
        pbrBakerMaterial.SetTexture("_MetallicMap", metallicMap ? metallicMap : Texture2D.blackTexture);
        pbrBakerMaterial.SetTexture("_RoughnessMap", roughnessMap ? roughnessMap : Texture2D.whiteTexture);
        pbrBakerMaterial.SetTexture("_AoMap", aoMap ? aoMap : Texture2D.whiteTexture);

        pbrBakerMaterial.SetVector("_LightDirection", lightDirection.normalized);
        pbrBakerMaterial.SetColor("_LightColor", (lightColor * lightIntensity).linear);
        pbrBakerMaterial.SetColor("_AmbientColor", ambientLightColor.linear);

        RenderAndSave(albedoMap, pbrBakerMaterial, path, false, RenderTextureReadWrite.sRGB);
    }

    private void BakeTextureProcessor(string path, bool isNormal)
    {
        textureProcessorMaterial.shaderKeywords = null;
        textureProcessorMaterial.SetTexture("_MainTex", albedoMap);

        if (currentMode == BakeMode.ColorReplacement)
        {
            textureProcessorMaterial.EnableKeyword("_ENABLE_REPLACEMENT");
            textureProcessorMaterial.SetColor("_TargetColor", targetColor);
            textureProcessorMaterial.SetColor("_ReplacementColor", replacementColor);
            textureProcessorMaterial.SetFloat("_ColorDifferenceTolerance", colorDifferenceTolerance);
            textureProcessorMaterial.SetFloat("_TransitionSoftness", transitionSoftness);
        }
        else if (currentMode == BakeMode.EmissionFromHue)
        {
            textureProcessorMaterial.EnableKeyword("_ENABLE_HUE_EMISSION");
            textureProcessorMaterial.SetColor("_HueTargetColor", hueTargetColor);
            textureProcessorMaterial.SetFloat("_HueThreshold", hueThreshold);
            textureProcessorMaterial.SetFloat("_SaturationThreshold", saturationThreshold);
            textureProcessorMaterial.SetColor("_EmissionColor", emissionColor);
            textureProcessorMaterial.SetFloat("_EmissionIntensity", emissionIntensity);
        }
        else if (currentMode == BakeMode.EmissionFromIdMap)
        {
            textureProcessorMaterial.EnableKeyword("_USE_ID_MAP");
            textureProcessorMaterial.SetTexture("_IdMap", idMap);
            textureProcessorMaterial.SetColor("_TargetIdColor", targetIdColor);
            textureProcessorMaterial.SetFloat("_IdTolerance", idTolerance);
            textureProcessorMaterial.SetColor("_EmissionColor", emissionColor);
            textureProcessorMaterial.SetFloat("_EmissionIntensity", emissionIntensity);
        }

        RenderAndSave(albedoMap, textureProcessorMaterial, path, isNormal, RenderTextureReadWrite.sRGB);
    }

    private void BakeHeightProcessor(string path, bool isNormal)
    {
        heightProcessorMaterial.shaderKeywords = null;

        if (currentMode == BakeMode.HeightToNormal)
        {
            heightProcessorMaterial.EnableKeyword("_BAKE_NORMAL");
            heightProcessorMaterial.SetFloat("_NormalStrength", normalStrength);
        }
        else if (currentMode == BakeMode.HeightToCurvature)
        {
            heightProcessorMaterial.EnableKeyword("_BAKE_CURVATURE");
            heightProcessorMaterial.SetFloat("_CurvatureRadius", curvatureRadius);
            heightProcessorMaterial.SetFloat("_CurvatureStrength", curvatureStrength);
        }
        else if (currentMode == BakeMode.HeightToAmbientOcclusion)
        {
            heightProcessorMaterial.EnableKeyword("_BAKE_AO");
            heightProcessorMaterial.SetInt("_AoSamples", aoSamples);
            heightProcessorMaterial.SetFloat("_AoRadius", aoRadius);
            heightProcessorMaterial.SetFloat("_AoStrength", aoStrength);
        }
        else if (currentMode == BakeMode.HeightToMetallic)
        {
            heightProcessorMaterial.EnableKeyword("_BAKE_METALLIC");
            heightProcessorMaterial.SetFloat("_MetallicLow", metallicLow);
            heightProcessorMaterial.SetFloat("_MetallicHigh", metallicHigh);
            heightProcessorMaterial.SetFloat("_MetallicContrast", metallicContrast);
        }

        RenderAndSave(heightMap, heightProcessorMaterial, path, isNormal, RenderTextureReadWrite.Linear);
    }

    private void RenderAndSave(Texture source, Material baker, string path, bool isNormal, RenderTextureReadWrite readWrite)
    {
        RenderTexture tempRT = RenderTexture.GetTemporary(outputResolution, outputResolution, 0, RenderTextureFormat.Default, readWrite);
        Graphics.Blit(source, tempRT, baker);

        Texture2D bakedTexture = new Texture2D(outputResolution, outputResolution, TextureFormat.RGBA32, false, readWrite == RenderTextureReadWrite.Linear);
        RenderTexture.active = tempRT;
        bakedTexture.ReadPixels(new Rect(0, 0, outputResolution, outputResolution), 0, 0);
        bakedTexture.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(tempRT);

        SaveAndConfigureTexture(bakedTexture, path, isNormal);
        DestroyImmediate(bakedTexture);
    }

    private void SaveAndConfigureTexture(Texture2D texture, string path, bool isNormalMap)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllBytes(path, texture.EncodeToPNG());
        AssetDatabase.Refresh();

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.isReadable = true;
            importer.textureType = isNormalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !isNormalMap;
            importer.SaveAndReimport();
        }

        Debug.Log($"Texture successfully baked to: {path}");
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
    }
}
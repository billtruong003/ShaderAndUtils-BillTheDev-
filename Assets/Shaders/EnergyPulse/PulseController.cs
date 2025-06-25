using UnityEngine;
using System.Collections.Generic;
[ExecuteInEditMode]
[RequireComponent(typeof(Renderer))]
public class ApplyMultiPulseEffect : MonoBehaviour
{
    [Header("Pulse List")]
    public List<PulseData> pulses = new List<PulseData>();

    [Header("Base Wire Appearance")]
    [Tooltip("Texture cho vật liệu nền của sợi dây.")]
    public Texture2D baseMaterial;
    [Tooltip("Kênh Alpha của texture này quyết định hình dạng sợi dây.")]
    public Texture2D wireShapeMask;
    [Tooltip("Màu để phủ lên trên vật liệu nền.")]
    public Color wireColorTint = Color.white;

    [Header("Pulse Appearance")]
    [Tooltip("Noise được áp dụng theo UV của object.")]
    public Texture2D noiseTexture;
    public float noiseScale = 1.0f;
    public Gradient pulseGradient;

    [Header("Global Shape & Animation")]
    [Tooltip("Hướng chạy của pulse, tính theo các trục local của object (X:Đỏ, Y:Xanh lá, Z:Xanh dương).")]
    public Vector3 objectPulseDirection = new Vector3(0, 1, 0);
    [Tooltip("Độ co giãn của không gian pulse. Tăng để pulse lặp lại nhiều hơn dọc theo object.")]
    public float pulseScale = 1.0f;
    [Range(0.001f, 0.1f)] public float pulseFeather = 0.01f;
    public float emissionIntensity = 1.0f;

    // --- Private variables ---
    private const int MAX_PULSES = 10;
    private MaterialPropertyBlock propertyBlock;
    private new Renderer renderer;
    private Texture2D generatedGradient;
    private float[] pulseWidths = new float[MAX_PULSES];
    private float[] pulseSpeeds = new float[MAX_PULSES];
    private float[] timeOffsets = new float[MAX_PULSES];

    void OnEnable()
    {
        renderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        UpdateMaterialProperties();
    }

    private void OnValidate()
    {
        if (renderer == null) renderer = GetComponent<Renderer>();
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        UpdateMaterialProperties();
    }

    void OnDisable()
    {
        if (generatedGradient != null) DestroyImmediate(generatedGradient);
        if (renderer != null) renderer.SetPropertyBlock(null);
    }

    public void UpdateMaterialProperties()
    {
        if (renderer == null) return;
        renderer.GetPropertyBlock(propertyBlock);
        GenerateGradientTexture();

        if (baseMaterial != null) propertyBlock.SetTexture("_BaseMap", baseMaterial);
        if (wireShapeMask != null) propertyBlock.SetTexture("_WireShapeMask", wireShapeMask);
        propertyBlock.SetColor("_WireColorTint", wireColorTint);

        if (noiseTexture != null) propertyBlock.SetTexture("_NoiseTex", noiseTexture);
        propertyBlock.SetFloat("_NoiseScale", noiseScale);
        if (generatedGradient != null) propertyBlock.SetTexture("_PulseGradient", generatedGradient);
        propertyBlock.SetFloat("_PulseFeather", pulseFeather);
        propertyBlock.SetVector("_ObjectDirection", objectPulseDirection.normalized);
        propertyBlock.SetFloat("_PulseScale", pulseScale);
        propertyBlock.SetFloat("_EmissionIntensity", emissionIntensity);

        int pulseCount = Mathf.Min(pulses.Count, MAX_PULSES);
        for (int i = 0; i < pulseCount; i++)
        {
            pulseWidths[i] = pulses[i].width;
            pulseSpeeds[i] = pulses[i].speed;
            timeOffsets[i] = pulses[i].timeOffset;
        }

        propertyBlock.SetInt("_PulseCount", pulseCount);
        propertyBlock.SetFloatArray("_PulseWidths", pulseWidths);
        propertyBlock.SetFloatArray("_PulseSpeeds", pulseSpeeds);
        propertyBlock.SetFloatArray("_TimeOffsets", timeOffsets);

        renderer.SetPropertyBlock(propertyBlock);
    }

    private void GenerateGradientTexture()
    {
        if (pulseGradient == null) return;
        const int width = 256;
        if (generatedGradient == null)
        {
            generatedGradient = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                name = "Procedural Pulse Gradient",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }
        Color[] colors = new Color[width];
        for (int i = 0; i < width; i++)
        {
            float t = (float)i / (width - 1);
            colors[i] = pulseGradient.Evaluate(t);
        }
        generatedGradient.SetPixels(colors);
        generatedGradient.Apply();
    }
}
[System.Serializable]
public class PulseData
{
    [Range(0.01f, 1.0f)] public float width = 0.2f;
    [Range(0.1f, 5.0f)] public float speed = 1.0f;
    [Range(0f, 1f)] public float timeOffset = 0f;
}

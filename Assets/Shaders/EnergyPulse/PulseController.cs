using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Cần thiết cho so sánh Gradient

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
    [Min(0)] public float emissionIntensity = 1.0f;

    // --- Private variables ---
    private const int MAX_PULSES = 10;
    private MaterialPropertyBlock propertyBlock;
    private new Renderer renderer;
    private Texture2D generatedGradient;

    // Mảng để gửi dữ liệu đến shader
    private float[] pulseWidths = new float[MAX_PULSES];
    private float[] pulseSpeeds = new float[MAX_PULSES];
    private float[] timeOffsets = new float[MAX_PULSES];
    // <-- CẢI TIẾN: Mảng cho hiệu ứng nhấp nháy
    private float[] flickerStrengths = new float[MAX_PULSES];
    private float[] flickerFrequencies = new float[MAX_PULSES];

    // <-- CẢI TIẾN: Cache để tối ưu hóa việc tạo texture
    private Gradient cachedGradient;
    private bool gradientDirty = true;


    void OnEnable()
    {
        renderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
        gradientDirty = true; // Bắt buộc cập nhật lại gradient khi enable
        UpdateMaterialProperties();
    }

    // OnValidate được gọi trong Editor mỗi khi một giá trị được thay đổi
    private void OnValidate()
    {
        // Khi chỉnh sửa Gradient trong Inspector, nó sẽ tạo một instance mới
        // Vì vậy, ta đánh dấu là "dirty" để tạo lại texture
        gradientDirty = true;

        if (renderer == null) renderer = GetComponent<Renderer>();
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        UpdateMaterialProperties();
    }

    void OnDisable()
    {
        if (generatedGradient != null) DestroyImmediate(generatedGradient);
        if (renderer != null) renderer.SetPropertyBlock(null);
    }

    void Update()
    {
        // Vẫn cần gọi Update trong Play Mode để cập nhật các hiệu ứng động theo thời gian
        // nếu có (ví dụ: thay đổi thông số pulse qua script khác)
        if (Application.isPlaying)
        {
            UpdateMaterialProperties();
        }
    }

    public void UpdateMaterialProperties()
    {
        if (renderer == null) return;
        renderer.GetPropertyBlock(propertyBlock);

        // <-- CẢI TIẾN: Chỉ tạo lại texture khi cần thiết
        GenerateGradientTexture();

        // Gán các thuộc tính cơ bản
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

        // Chuẩn bị dữ liệu mảng
        int pulseCount = Mathf.Min(pulses.Count, MAX_PULSES);
        for (int i = 0; i < pulseCount; i++)
        {
            pulseWidths[i] = pulses[i].width;
            pulseSpeeds[i] = pulses[i].speed;
            timeOffsets[i] = pulses[i].timeOffset;
            // <-- CẢI TIẾN: Lấy dữ liệu nhấp nháy
            flickerStrengths[i] = pulses[i].flickerStrength;
            flickerFrequencies[i] = pulses[i].flickerFrequency;
        }

        // Gán dữ liệu mảng vào property block
        propertyBlock.SetInt("_PulseCount", pulseCount);
        propertyBlock.SetFloatArray("_PulseWidths", pulseWidths);
        propertyBlock.SetFloatArray("_PulseSpeeds", pulseSpeeds);
        propertyBlock.SetFloatArray("_TimeOffsets", timeOffsets);
        // <-- CẢI TIẾN: Gửi dữ liệu nhấp nháy đến shader
        propertyBlock.SetFloatArray("_FlickerStrengths", flickerStrengths);
        propertyBlock.SetFloatArray("_FlickerFrequencies", flickerFrequencies);

        renderer.SetPropertyBlock(propertyBlock);
    }

    private void GenerateGradientTexture()
    {
        if (pulseGradient == null) return;
        // Nếu gradient không "dirty" và texture đã tồn tại, không cần làm gì cả
        if (!gradientDirty && generatedGradient != null) return;

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

        // Đánh dấu là đã "sạch"
        gradientDirty = false;
    }
}
[System.Serializable]
public class PulseData
{
    [Range(0.01f, 1.0f)] public float width = 0.2f;
    [Range(-5.0f, 5.0f)] public float speed = 1.0f; // Cho phép tốc độ âm để đổi chiều
    [Range(0f, 1f)] public float timeOffset = 0f;

    [Header("Flicker Effect")]
    [Range(0f, 50f)] public float flickerFrequency = 0f; // Tần số nhấp nháy
    [Range(0f, 1f)] public float flickerStrength = 0.5f; // Độ mạnh của nhấp nháy
}
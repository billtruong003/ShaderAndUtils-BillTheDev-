using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(MeshRenderer))]
public class PortalControllerUltimate : MonoBehaviour
{
    private MeshRenderer meshRenderer;
    private MaterialPropertyBlock propBlock;

    [Title("ULTIMATE PORTAL CONTROLLER", TitleAlignment = TitleAlignments.Centered)]
    [InfoBox("Các thay đổi sẽ được áp dụng trực tiếp lên portal trong Editor.")]

    // --- TABS ---
    [TabGroup("MainTabs", "Textures & Gradient")]
    [TabGroup("MainTabs", "Nebula (Background)")]
    [TabGroup("MainTabs", "Energy Veins")]
    [TabGroup("MainTabs", "Core & Shape")]
    [TabGroup("MainTabs", "Global Effects")]

    // --- TEXTURES & GRADIENT ---
    [TabGroup("MainTabs", "Textures & Gradient")]
    [BoxGroup("MainTabs/Textures & Gradient/Textures")]
    [Required] public Texture nebulaSwirlNoise;
    [Required] public Texture energyVeinsNoise;

    [TabGroup("MainTabs", "Textures & Gradient")]
    [BoxGroup("MainTabs/Textures & Gradient/Gradient Generator")]
    [OnValueChanged("GenerateGradientTexture")]
    public Gradient colorGradient = new Gradient();
    [OnValueChanged("GenerateGradientTexture")]
    [Range(64, 512)]
    public int gradientResolution = 256;
    [ShowInInspector, ReadOnly, PreviewField(75, ObjectFieldAlignment.Center)]
    private Texture2D generatedGradientTex;

    // --- NEBULA ---
    [TabGroup("MainTabs", "Nebula (Background)")]
    [OnValueChanged("ApplyProperties")]
    [Range(0.1f, 10f)]
    public float nebulaScale = 2.0f;
    [TabGroup("MainTabs", "Nebula (Background)")]
    [OnValueChanged("ApplyProperties")]
    [Range(0f, 10f)]
    public float nebulaSwirlStrength = 3.0f;
    [TabGroup("MainTabs", "Nebula (Background)")]
    [OnValueChanged("ApplyProperties")]
    [Range(-5f, 5f)]
    public float nebulaSwirlSpeed = 0.5f;

    // --- ENERGY VEINS ---
    [TabGroup("MainTabs", "Energy Veins")]
    [OnValueChanged("ApplyProperties")]
    [Range(1f, 20f)]
    public float veinsScale = 8.0f;
    [TabGroup("MainTabs", "Energy Veins")]
    [OnValueChanged("ApplyProperties")]
    [Range(-10f, 10f)]
    public float veinsFlowSpeed = -2.0f;
    [TabGroup("MainTabs", "Energy Veins")]
    [OnValueChanged("ApplyProperties")]
    [Range(0f, 1f)]
    public float veinsVisibilityThreshold = 0.7f;
    [TabGroup("MainTabs", "Energy Veins")]
    [OnValueChanged("ApplyProperties")]
    [Range(0.001f, 0.2f)]
    public float veinsEdgeSoftness = 0.02f;
    [TabGroup("MainTabs", "Energy Veins")]
    [OnValueChanged("ApplyProperties")]
    [ColorUsage(true, true)]
    public Color veinsColor = Color.white;

    // --- CORE & SHAPE ---
    [TabGroup("MainTabs", "Core & Shape")]
    [Title("Shape")]
    [OnValueChanged("ApplyProperties")]
    [Range(0.01f, 1f)]
    public float outerEdgeSoftness = 0.4f;
    [TabGroup("MainTabs", "Core & Shape")]
    [OnValueChanged("ApplyProperties")]
    [Range(0.01f, 0.2f)]
    public float innerRimSize = 0.02f;
    [TabGroup("MainTabs", "Core & Shape")]
    [OnValueChanged("ApplyProperties")]
    [ColorUsage(true, true)]
    public Color innerRimColor = new Color(0.5f, 2f, 2f, 1f);

    [TabGroup("MainTabs", "Core & Shape")]
    [Title("Pulsating Core")]
    [OnValueChanged("ApplyProperties")]
    [Range(0f, 0.5f)]
    public float coreSize = 0.05f;
    [TabGroup("MainTabs", "Core & Shape")]
    [OnValueChanged("ApplyProperties")]
    [Range(0f, 10f)]
    public float corePulseSpeed = 3.0f;
    [TabGroup("MainTabs", "Core & Shape")]
    [OnValueChanged("ApplyProperties")]
    [Range(0f, 1f)]
    public float corePulseMagnitude = 0.5f;
    [TabGroup("MainTabs", "Core & Shape")]
    [OnValueChanged("ApplyProperties")]
    [ColorUsage(true, true)]
    public Color coreColor = new Color(2f, 2f, 2f, 1f);

    // --- GLOBAL EFFECTS ---
    [TabGroup("MainTabs", "Global Effects")]
    [OnValueChanged("ApplyProperties")]
    [Range(0f, 0.2f)]
    public float uvDistortion = 0.03f;
    [TabGroup("MainTabs", "Global Effects")]
    [OnValueChanged("ApplyProperties")]
    [Range(0f, 5f)]
    public float overallGlow = 1.5f;

    void OnValidate()
    {
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
        // Tự động tạo gradient nếu chưa có
        if (generatedGradientTex == null) GenerateGradientTexture();
        ApplyProperties();
    }

    void Awake()
    {
        propBlock = new MaterialPropertyBlock();
        meshRenderer = GetComponent<MeshRenderer>();
        GenerateGradientTexture(); // Đảm bảo có texture khi bắt đầu
    }

    [TabGroup("MainTabs", "Textures & Gradient")]
    [BoxGroup("MainTabs/Textures & Gradient/Gradient Generator")]
    [Button("Force Regenerate Gradient", ButtonSizes.Medium)]
    public void GenerateGradientTexture()
    {
        if (colorGradient == null) return;
        if (generatedGradientTex == null || generatedGradientTex.width != gradientResolution)
        {
            generatedGradientTex = new Texture2D(gradientResolution, 1, TextureFormat.RGBA32, false)
            {
                name = "Procedural_Gradient_Tex",
                wrapMode = TextureWrapMode.Clamp
            };
        }
        Color[] colors = new Color[gradientResolution];
        for (int i = 0; i < gradientResolution; i++)
        {
            colors[i] = colorGradient.Evaluate((float)i / (gradientResolution - 1));
        }
        generatedGradientTex.SetPixels(colors);
        generatedGradientTex.Apply();
        ApplyProperties();
    }

    private void ApplyProperties()
    {
        if (meshRenderer == null || propBlock == null) Awake();

        meshRenderer.GetPropertyBlock(propBlock);

        // Textures
        propBlock.SetTexture("_NoiseTex", nebulaSwirlNoise);
        propBlock.SetTexture("_VeinsTex", energyVeinsNoise);
        propBlock.SetTexture("_GradientTex", generatedGradientTex);

        // Floats
        propBlock.SetFloat("_SwirlScale", nebulaScale);
        propBlock.SetFloat("_SwirlStrength", nebulaSwirlStrength);
        propBlock.SetFloat("_SwirlSpeed", nebulaSwirlSpeed);
        propBlock.SetFloat("_VeinsScale", veinsScale);
        propBlock.SetFloat("_VeinsSpeed", veinsFlowSpeed);
        propBlock.SetFloat("_VeinsThreshold", veinsVisibilityThreshold);
        propBlock.SetFloat("_VeinsSoftness", veinsEdgeSoftness);
        propBlock.SetFloat("_EdgeSoftness", outerEdgeSoftness);
        propBlock.SetFloat("_RimSize", innerRimSize);
        propBlock.SetFloat("_CoreSize", coreSize);
        propBlock.SetFloat("_CorePulseSpeed", corePulseSpeed);
        propBlock.SetFloat("_CorePulseMagnitude", corePulseMagnitude);
        propBlock.SetFloat("_UVDisplacementStrength", uvDistortion);
        propBlock.SetFloat("_Glow", overallGlow);

        // Colors
        propBlock.SetColor("_VeinsColor", veinsColor);
        propBlock.SetColor("_RimColor", innerRimColor);
        propBlock.SetColor("_CoreColor", coreColor);

        meshRenderer.SetPropertyBlock(propBlock);
    }
}
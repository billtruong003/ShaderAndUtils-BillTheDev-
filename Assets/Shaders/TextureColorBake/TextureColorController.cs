using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Renderer))]
public class TextureColorController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Camera used for picking colors. Defaults to Camera.main.")]
    [SerializeField] private Camera pickingCamera;

    [Header("Real-time Shader Parameters")]
    [Tooltip("The color to replace matching pixels with.")]
    [SerializeField] private Color replacementColor = Color.cyan;

    [Space]
    [Tooltip("Perceptual color difference tolerance (CIEDE2000). 1.0 is a barely noticeable difference.")]
    [Range(0f, 100f)]
    [SerializeField] private float colorDifferenceTolerance = 10f;

    [Tooltip("Softness of the transition between original and replaced colors.")]
    [Range(0.01f, 20f)]
    [SerializeField] private float transitionSoftness = 2f;

    private Renderer objectRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Texture2D sourceTexture;

    private static readonly int TargetColorID = Shader.PropertyToID("_TargetColor");
    private static readonly int ReplacementColorID = Shader.PropertyToID("_ReplacementColor");
    private static readonly int ColorDifferenceToleranceID = Shader.PropertyToID("_ColorDifferenceTolerance");
    private static readonly int TransitionSoftnessID = Shader.PropertyToID("_TransitionSoftness");
    private const string ReplacementKeyword = "_ENABLE_REPLACEMENT";

    private void Awake()
    {
        InitializeDependencies();
        InitializeMaterial();
    }

    private void Start()
    {
        ApplyShaderParameters();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryPickColorFromTexture();
        }
    }

    private void OnValidate()
    {
        if (!this.enabled || !gameObject.activeInHierarchy) return;

        if (objectRenderer == null)
        {
            objectRenderer = GetComponent<Renderer>();
        }
        ApplyShaderParameters();
    }

    private void InitializeDependencies()
    {
        if (pickingCamera == null)
        {
            pickingCamera = Camera.main;
        }
        Assert.IsNotNull(pickingCamera, "TextureColorController requires a Camera to function, but Camera.main is also null.");

        objectRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock();
    }

    private void InitializeMaterial()
    {
        Assert.IsNotNull(objectRenderer.sharedMaterial, "Renderer does not have a material assigned.");

        var mainTexture = objectRenderer.sharedMaterial.mainTexture;
        Assert.IsNotNull(mainTexture, "Material must have a main texture assigned for color picking.");

        sourceTexture = mainTexture as Texture2D;
        if (sourceTexture != null && !sourceTexture.isReadable)
        {
            Debug.LogError($"Texture '{sourceTexture.name}' is not readable. Please enable 'Read/Write Enabled' in its import settings.", sourceTexture);
            this.enabled = false;
        }
    }

    private void TryPickColorFromTexture()
    {
        if (sourceTexture == null) return;

        Ray ray = pickingCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == this.gameObject)
        {
            Vector2 pixelUV = hit.textureCoord;
            Color pickedColor = sourceTexture.GetPixelBilinear(pixelUV.x, pixelUV.y);
            SetTargetColorOnMaterial(pickedColor);
        }
    }

    private void SetTargetColorOnMaterial(Color color)
    {
        objectRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(TargetColorID, color);
        objectRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ApplyShaderParameters()
    {
        if (objectRenderer == null) return;

        objectRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(ReplacementColorID, replacementColor);
        propertyBlock.SetFloat(ColorDifferenceToleranceID, colorDifferenceTolerance);
        propertyBlock.SetFloat(TransitionSoftnessID, transitionSoftness);
        objectRenderer.SetPropertyBlock(propertyBlock);

        // This ensures the correct shader variant is used
        var material = objectRenderer.material;
        if (this.enabled)
        {
            material.EnableKeyword(ReplacementKeyword);
        }
        else
        {
            material.DisableKeyword(ReplacementKeyword);
        }
    }

    private void OnDisable()
    {
        if (objectRenderer != null && objectRenderer.sharedMaterial != null)
        {
            // Reset the material to its default state
            objectRenderer.SetPropertyBlock(null);
            if (Application.isPlaying) // Avoid material leaks in editor
            {
                objectRenderer.material.DisableKeyword(ReplacementKeyword);
            }
        }
    }
}
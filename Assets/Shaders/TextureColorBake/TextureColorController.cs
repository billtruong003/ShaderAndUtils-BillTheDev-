using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Renderer))]
public class TextureColorController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("Camera used for picking colors. Will default to Camera.main if not set.")]
    [SerializeField] private Camera pickingCamera;

    [Header("Real-time Shader Parameters")]
    [SerializeField] private Color replacementColor = Color.cyan;

    [Space]
    [Tooltip("Perceptual color difference tolerance based on CIEDE2000. A value of 1.0 is a barely noticeable difference.")]
    [Range(0f, 100f)]
    [SerializeField] private float colorDifferenceTolerance = 10f;

    [Tooltip("How soft the transition between original and replaced colors should be.")]
    [Range(0.01f, 20f)]
    [SerializeField] private float transitionSoftness = 2f;


    private Material materialInstance;
    private Texture2D sourceTexture;

    private static readonly int TargetColorID = Shader.PropertyToID("_TargetColor");
    private static readonly int ReplacementColorID = Shader.PropertyToID("_ReplacementColor");
    private static readonly int ColorDifferenceToleranceID = Shader.PropertyToID("_ColorDifferenceTolerance");
    private static readonly int TransitionSoftnessID = Shader.PropertyToID("_TransitionSoftness");

    private void Start()
    {
        InitializeDependencies();
        InitializeMaterial();
        UpdateShaderParametersFromInspector();
    }

    private void Update()
    {
        HandleColorPickingInput();
    }

    private void OnValidate()
    {
        if (materialInstance == null)
        {
            Renderer objectRenderer = GetComponent<Renderer>();
            if (objectRenderer != null)
            {
                materialInstance = objectRenderer.sharedMaterial;
            }
        }
        UpdateShaderParametersFromInspector();
    }

    private void InitializeDependencies()
    {
        if (pickingCamera == null)
        {
            pickingCamera = Camera.main;
        }
        Assert.IsNotNull(pickingCamera, "TextureColorController requires a Camera to function, but Camera.main is also null.");
    }

    private void InitializeMaterial()
    {
        Renderer objectRenderer = GetComponent<Renderer>();
        Assert.IsNotNull(objectRenderer.material, "Renderer does not have a material assigned.");

        materialInstance = objectRenderer.material;
        sourceTexture = materialInstance.mainTexture as Texture2D;

        Assert.IsNotNull(sourceTexture, "Material must have a main texture assigned.");

        if (!sourceTexture.isReadable)
        {
            Debug.LogError($"Texture '{sourceTexture.name}' is not readable. Please enable 'Read/Write Enabled' in its import settings.", sourceTexture);
            this.enabled = false;
        }
    }

    private void HandleColorPickingInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryPickColorFromTexture();
        }
    }

    private void TryPickColorFromTexture()
    {
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
        if (materialInstance == null) return;
        materialInstance.SetColor(TargetColorID, color);
    }

    private void UpdateShaderParametersFromInspector()
    {
        if (materialInstance == null) return;

        materialInstance.SetColor(ReplacementColorID, replacementColor);
        materialInstance.SetFloat(ColorDifferenceToleranceID, colorDifferenceTolerance);
        materialInstance.SetFloat(TransitionSoftnessID, transitionSoftness);
    }
}
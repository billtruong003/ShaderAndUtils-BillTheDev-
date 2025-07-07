// File: Assets/MyIndieGame/Scripts/VisualEffects/AfterImageController.cs (VERSION 2 - Frame Perfect)
using Sirenix.OdinInspector;
using System.Collections; // THÊM MỚI để dùng Coroutine
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("My Indie Game/Visual Effects/After Image Controller")]
public sealed class AfterImageController : MonoBehaviour
{
    // ... (Toàn bộ các enum và class lồng nhau giữ nguyên: ActivationMode, ColorSettings, PooledAfterImage) ...
    #region Enums and Nested Classes

    public enum ActivationMode
    {
        OnMovement,
        OnCommand,
        Always
    }

    [System.Serializable]
    public struct ColorSettings
    {
        public enum ColorMode
        {
            Single,
            RandomFromList,
            Gradient
        }

        [EnumToggleButtons]
        public ColorMode Mode;

        [ShowIf("Mode", ColorMode.Single)]
        [ColorUsage(true, true)]
        public Color SingleColor;

        [ShowIf("Mode", ColorMode.RandomFromList)]
        [ColorUsage(true, true)]
        public List<Color> ColorPalette;

        [ShowIf("Mode", ColorMode.Gradient)]
        [GradientUsage(true)]
        public Gradient ColorGradient;
    }

    private class PooledAfterImage
    {
        public readonly GameObject Instance;
        public readonly MeshFilter MeshFilter;
        public readonly Renderer Renderer;
        public float FadeValue;

        public PooledAfterImage(GameObject instance)
        {
            Instance = instance;
            MeshFilter = instance.GetComponent<MeshFilter>();
            Renderer = instance.GetComponent<Renderer>();
            FadeValue = 0f;
        }

        public void Activate(Vector3 position, Quaternion rotation)
        {
            Instance.transform.SetPositionAndRotation(position, rotation);
            Instance.SetActive(true);
            FadeValue = 1f;
        }

        public void Deactivate()
        {
            if (MeshFilter.mesh != null)
            {
                MeshFilter.mesh.Clear();
            }
            Instance.SetActive(false);
            FadeValue = 0f;
        }

        public bool IsActive => Instance.activeInHierarchy;
    }

    #endregion

    // ... (Toàn bộ các biến SerializeField giữ nguyên) ...
    [Title("After Image Controller", "Manages the creation and fading of after-images.")]
    [InfoBox("This effect is performance-intensive due to mesh baking and combining in real-time. Use judiciously and keep the pool size reasonable.")]

    [BoxGroup("Core Setup")]
    [Required("The character root whose meshes will be copied.")]
    [SerializeField] private GameObject sourceCharacterRoot;

    [BoxGroup("Core Setup")]
    [Required("A prefab with a MeshFilter, MeshRenderer, and a material using a compatible transparent shader.")]
    [AssetsOnly]
    [SerializeField] private GameObject afterImagePrefab;

    [BoxGroup("Core Setup")]
    [Tooltip("The transform to use as the origin (0,0,0) for the combined after-image mesh. Usually the character's root transform.")]
    [SerializeField] private Transform afterImageOrigin;

    [BoxGroup("Activation")]
    [Tooltip("OnMovement: Creates images when moving.\nOnCommand: Creates images only when Trigger() is called.\nAlways: Creates images continuously.")]
    [SerializeField] private ActivationMode activationMode = ActivationMode.OnMovement;

    [BoxGroup("Activation")]
    [ShowIf("activationMode", ActivationMode.OnMovement)]
    [Range(0f, 1f)]
    [SerializeField] private float movementThreshold = 0.01f;

    [BoxGroup("Activation")]
    [MinValue(0.01)]
    [SerializeField] private float activationDelay = 0.05f;

    [BoxGroup("Pooling")]
    [Range(1, 50)]
    [SerializeField] private int poolSize = 10;

    [BoxGroup("Appearance")]
    [MinValue(0.01)]
    [SerializeField] private float fadeDuration = 0.5f;

    [BoxGroup("Appearance")]
    [Tooltip("Name of the float property in the shader that controls transparency (e.g., '_Fade', '_Alpha').")]
    [SerializeField] private string fadeShaderProperty = "_Fade";

    [BoxGroup("Appearance")]
    [Tooltip("Name of the color property in the shader (e.g., '_Color', '_BaseColor').")]
    [SerializeField] private string colorShaderProperty = "_Color";

    [BoxGroup("Appearance")]
    [SerializeField] private ColorSettings colorSettings;
    // ...

    private readonly List<PooledAfterImage> _pool = new List<PooledAfterImage>();
    private GameObject _poolParent;
    private SkinnedMeshRenderer[] _sourceSkinnedRenderers;
    private MeshRenderer[] _sourceMeshRenderers;
    private MeshFilter[] _sourceMeshFilters;
    private CombineInstance[] _combineInstances;
    private float _activationTimer;
    private Vector3 _previousPosition;
    private bool _isInitialized;

    // ... (Start, OnEnable, OnDisable, OnDestroy giữ nguyên) ...
    #region Unity Lifecycle

    private void Start()
    {
        Initialize();
    }

    private void OnEnable()
    {
        if (!_isInitialized) return;
        _previousPosition = afterImageOrigin.position;
    }

    private void OnDisable()
    {
        DeactivateAllImages();
    }

    private void OnDestroy()
    {
        CleanupPool();
    }
    #endregion

    // THAY ĐỔI LỚN: LateUpdate giờ chỉ xử lý kích hoạt và fade, không tạo ảnh trực tiếp
    private void LateUpdate()
    {
        if (!_isInitialized) return;

        // Luôn cập nhật fade cho các ảnh đang hoạt động
        UpdateActiveImages();

        // Xử lý logic kích hoạt
        HandleActivationTimer();
        if (ShouldCreateAfterImage())
        {
            // Thay vì gọi hàm tạo ảnh, ta khởi động coroutine
            StartCoroutine(CreateAfterImageAtFrameEnd());
            _activationTimer = activationDelay; // Reset timer ngay lập tức
        }
    }

    // ... (Initialize và các hàm public giữ nguyên) ...
    #region Public API

    [Button("Trigger Effect", ButtonSizes.Medium)]
    [ShowIf("activationMode", ActivationMode.OnCommand)]
    public void Trigger()
    {
        if (activationMode != ActivationMode.OnCommand || !_isInitialized) return;
        StartCoroutine(CreateAfterImageAtFrameEnd());
    }

    #endregion

    #region Initialization and Cleanup

    private void Initialize()
    {
        if (!ValidateSetup()) return;

        InitializePool();
        CollectSourceRenderers();

        _previousPosition = afterImageOrigin.position;
        _isInitialized = true;
    }

    private bool ValidateSetup()
    {
        if (sourceCharacterRoot == null || afterImagePrefab == null || afterImageOrigin == null)
        {
            Debug.LogError($"[{nameof(AfterImageController)}] Core setup fields are not assigned. Disabling component.", this);
            enabled = false;
            return false;
        }
        return true;
    }

    private void InitializePool()
    {
        if (_poolParent != null) CleanupPool();

        _poolParent = new GameObject($"{sourceCharacterRoot.name}_AfterImagePool");

        for (int i = 0; i < poolSize; i++)
        {
            GameObject instance = Instantiate(afterImagePrefab, _poolParent.transform);
            instance.SetActive(false);
            _pool.Add(new PooledAfterImage(instance));
        }
    }

    private void CollectSourceRenderers()
    {
        _sourceSkinnedRenderers = sourceCharacterRoot.GetComponentsInChildren<SkinnedMeshRenderer>();
        _sourceMeshRenderers = sourceCharacterRoot.GetComponentsInChildren<MeshRenderer>();
        _sourceMeshFilters = new MeshFilter[_sourceMeshRenderers.Length];

        for (int i = 0; i < _sourceMeshRenderers.Length; i++)
        {
            _sourceMeshFilters[i] = _sourceMeshRenderers[i].GetComponent<MeshFilter>();
        }

        _combineInstances = new CombineInstance[_sourceSkinnedRenderers.Length + _sourceMeshRenderers.Length];
    }

    private void CleanupPool()
    {
        if (_poolParent != null)
        {
            Destroy(_poolParent);
        }
        _pool.Clear();
    }

    #endregion

    // HÀM MỚI: Coroutine để tạo ảnh vào cuối frame
    private IEnumerator CreateAfterImageAtFrameEnd()
    {
        // ĐỢI CHO ĐẾN KHI FRAME ĐÃ RENDER XONG
        yield return new WaitForSeconds(0.1f);

        // Bây giờ, tất cả vị trí và tư thế đều là cuối cùng và chính xác
        PooledAfterImage image = GetAvailableImageFromPool();
        if (image == null) yield break;

        // Các bước còn lại giống hệt như trước
        BakeAndCombineMeshes(image.MeshFilter);
        ApplyMaterialProperties(image.Renderer);
        image.Activate(afterImageOrigin.position, afterImageOrigin.rotation);
    }

    #region Core Logic

    private void HandleActivationTimer()
    {
        if (_activationTimer > 0)
        {
            _activationTimer -= Time.deltaTime;
        }
    }

    // HÀM MỚI: Tách logic kiểm tra điều kiện ra riêng
    private bool ShouldCreateAfterImage()
    {
        if (_activationTimer > 0f) return false;

        switch (activationMode)
        {
            case ActivationMode.OnMovement:
                float movementSqrMagnitude = (afterImageOrigin.position - _previousPosition).sqrMagnitude;
                _previousPosition = afterImageOrigin.position; // Cập nhật vị trí cũ ở đây
                return movementSqrMagnitude > (movementThreshold * movementThreshold);
            case ActivationMode.Always:
                return true;
            case ActivationMode.OnCommand: // OnCommand sẽ không kích hoạt tự động
            default:
                return false;
        }
    }

    private void BakeAndCombineMeshes(MeshFilter targetFilter)
    {
        Matrix4x4 matrix = afterImageOrigin.worldToLocalMatrix;
        var tempMeshes = new List<Mesh>(_sourceSkinnedRenderers.Length);

        for (int i = 0; i < _sourceSkinnedRenderers.Length; i++)
        {
            Mesh bakedMesh = new Mesh();
            _sourceSkinnedRenderers[i].BakeMesh(bakedMesh);
            tempMeshes.Add(bakedMesh);
            _combineInstances[i].mesh = bakedMesh;
            _combineInstances[i].transform = matrix * _sourceSkinnedRenderers[i].localToWorldMatrix;
        }

        for (int i = 0; i < _sourceMeshRenderers.Length; i++)
        {
            int combineIndex = _sourceSkinnedRenderers.Length + i;
            _combineInstances[combineIndex].mesh = _sourceMeshFilters[i].sharedMesh;
            _combineInstances[combineIndex].transform = matrix * _sourceMeshRenderers[i].transform.localToWorldMatrix;
        }

        if (targetFilter.mesh == null) targetFilter.mesh = new Mesh();
        targetFilter.mesh.Clear();
        targetFilter.mesh.CombineMeshes(_combineInstances, true, true);

        foreach (var mesh in tempMeshes)
        {
            Destroy(mesh);
        }
    }

    private void ApplyMaterialProperties(Renderer targetRenderer)
    {
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        targetRenderer.GetPropertyBlock(block);

        Color finalColor = GetColorFromSettings();
        block.SetColor(colorShaderProperty, finalColor);
        block.SetFloat(fadeShaderProperty, 1f);

        targetRenderer.SetPropertyBlock(block);
    }

    private void UpdateActiveImages()
    {
        if (fadeDuration <= 0) return;
        float fadeDelta = Time.deltaTime / fadeDuration;

        foreach (var image in _pool)
        {
            if (!image.IsActive) continue;

            image.FadeValue -= fadeDelta;

            if (image.FadeValue <= 0f)
            {
                image.Deactivate();
            }
            else
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                image.Renderer.GetPropertyBlock(block);

                // THAY ĐỔI DÒNG NÀY:
                // Thay vì gửi trực tiếp FadeValue (1 -> 0), ta gửi 1.0f - FadeValue (0 -> 1)
                block.SetFloat(fadeShaderProperty, 1.0f - image.FadeValue);

                image.Renderer.SetPropertyBlock(block);
            }
        }
    }

    #endregion

    #region Helper Methods

    private PooledAfterImage GetAvailableImageFromPool()
    {
        foreach (var image in _pool)
        {
            if (!image.IsActive) return image;
        }

        PooledAfterImage oldestImage = null;
        float lowestFade = float.MaxValue;
        foreach (var image in _pool)
        {
            if (image.FadeValue < lowestFade)
            {
                lowestFade = image.FadeValue;
                oldestImage = image;
            }
        }
        return oldestImage;
    }

    private void DeactivateAllImages()
    {
        if (_pool == null) return;
        foreach (var image in _pool)
        {
            if (image != null && image.IsActive)
            {
                image.Deactivate();
            }
        }
    }

    private Color GetColorFromSettings()
    {
        switch (colorSettings.Mode)
        {
            case ColorSettings.ColorMode.Single:
                return colorSettings.SingleColor;

            case ColorSettings.ColorMode.RandomFromList:
                if (colorSettings.ColorPalette == null || colorSettings.ColorPalette.Count == 0)
                {
                    return Color.white;
                }
                return colorSettings.ColorPalette[Random.Range(0, colorSettings.ColorPalette.Count)];

            case ColorSettings.ColorMode.Gradient:
                return colorSettings.ColorGradient.Evaluate(Random.value);

            default:
                return Color.white;
        }
    }

    #endregion
}
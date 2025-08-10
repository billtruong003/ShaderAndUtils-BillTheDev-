using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Renderer))]
public sealed class ScopeController : MonoBehaviour
{
    [Tooltip("The material instance of the scope.")]
    public Material scopeMaterial;

    [Tooltip("The maximum zoom magnification level.")]
    [Min(1.0f)]
    public float zoomLevel = 4.0f;

    [Tooltip("The speed of the zoom transition.")]
    public float zoomSpeed = 10.0f;

    private const float DEFAULT_ZOOM = 1.0f;
    private bool _isScoped = false;
    private float _currentZoom;

    private static readonly int ZoomID = Shader.PropertyToID("_Zoom");
    private static readonly int CullID = Shader.PropertyToID("_Cull");

    private void Awake()
    {
        if (scopeMaterial == null)
        {
            var rend = GetComponent<Renderer>();
            if (rend != null)
            {
                scopeMaterial = rend.material;
            }
        }
        _currentZoom = DEFAULT_ZOOM;
    }

    private void Start()
    {
        if (scopeMaterial != null)
        {
            scopeMaterial.SetFloat(ZoomID, _currentZoom);
            scopeMaterial.SetFloat(CullID, (float)CullMode.Front);
        }
    }

    private void Update()
    {
        HandleInput();
        UpdateZoom();
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            _isScoped = !_isScoped;
            var cullMode = _isScoped ? CullMode.Off : CullMode.Front;
            scopeMaterial.SetFloat(CullID, (float)cullMode);
        }
    }

    private void UpdateZoom()
    {
        float targetZoom = _isScoped ? zoomLevel : DEFAULT_ZOOM;

        if (Mathf.Approximately(_currentZoom, targetZoom))
        {
            return;
        }

        _currentZoom = Mathf.Lerp(_currentZoom, targetZoom, Time.deltaTime * zoomSpeed);
        scopeMaterial.SetFloat(ZoomID, _currentZoom);
    }
}
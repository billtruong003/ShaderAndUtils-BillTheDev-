using UnityEngine;

// Script này phải được đặt trên chính object ScopeLens
[RequireComponent(typeof(Renderer))]
public sealed class ScopeMatrixUpdater : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private Camera scopeCamera;

    private MaterialPropertyBlock propertyBlock;
    private Renderer scopeLensRenderer;
    private static readonly int ScopeVpProperty = Shader.PropertyToID("_ScopeCameraVP");

    private void Awake()
    {
        scopeLensRenderer = GetComponent<Renderer>();
        propertyBlock = new MaterialPropertyBlock(); // Sử dụng MaterialPropertyBlock để tối ưu

        if (scopeCamera == null)
        {
            Debug.LogError("Scope Camera reference is not set on ScopeMatrixUpdater.", this);
            this.enabled = false;
        }
    }

    private void LateUpdate()
    {
        UpdateScopeMatrix();
    }

    private void UpdateScopeMatrix()
    {
        if (scopeCamera == null || !scopeCamera.gameObject.activeInHierarchy)
        {
            return;
        }

        // Lấy ma trận View và Projection gốc từ Scope Camera
        Matrix4x4 viewMatrix = scopeCamera.worldToCameraMatrix;
        Matrix4x4 projectionMatrix = scopeCamera.projectionMatrix;

        // ĐÂY LÀ PHẦN SỬA LỖI QUAN TRỌNG
        // Hàm GL.GetGPUProjectionMatrix sẽ tự động điều chỉnh ma trận projection
        // để xử lý vấn đề lật ngược khi render vào một texture.
        // Tham số 'true' là để chỉ định rằng chúng ta đang render vào một texture.
        Matrix4x4 gpuProjectionMatrix = GL.GetGPUProjectionMatrix(projectionMatrix, true);

        // Tạo ma trận View-Projection cuối cùng đã được hiệu chỉnh
        Matrix4x4 viewProjectionMatrix = gpuProjectionMatrix * viewMatrix;

        // Sử dụng MaterialPropertyBlock để gửi ma trận tới shader mà không tạo instance material mới
        scopeLensRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetMatrix(ScopeVpProperty, viewProjectionMatrix);
        scopeLensRenderer.SetPropertyBlock(propertyBlock);
    }
}
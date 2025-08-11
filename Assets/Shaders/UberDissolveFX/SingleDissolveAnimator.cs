using UnityEngine;

[ExecuteAlways]
public class SingleDissolveAnimator : MonoBehaviour
{
    public enum ExecutionMode
    {
        [Tooltip("Cập nhật trong cả Editor và Play Mode. Lý tưởng để làm animation.")]
        EditorAndPlay,
        [Tooltip("Tối ưu hóa: Chỉ cập nhật trong Play Mode, được điều khiển bởi Animator.")]
        PlayModeOnly
    }

    private const string DISSOLVE_PROPERTY_NAME = "_DissolveThreshold";

    [Tooltip("Chọn chế độ hoạt động: EditorAndPlay để xem trước, PlayModeOnly để tối ưu khi chạy game.")]
    [SerializeField]
    private ExecutionMode mode = ExecutionMode.EditorAndPlay;

    [Tooltip("Chỉ số của slot material trên Renderer mà component này sẽ điều khiển.")]
    [SerializeField]
    private int materialSlotIndex = 0;

    [Tooltip("Material chuẩn, nhẹ, được sử dụng khi hiệu ứng kết thúc.")]
    [SerializeField]
    private Material standardMaterial;

    [Tooltip("Material dissolve, được sử dụng trong quá trình diễn ra hiệu ứng.")]
    [SerializeField]
    private Material dissolveMaterial;

    [Tooltip("Giá trị xem trước và keyframe. Khi ở Play Mode, giá trị này sẽ bị ghi đè bởi Animator nếu có key.")]
    [Range(-2f, 2f)]
    [SerializeField]
    private float dissolveValue = 0f;

    // Public getters để các script khác (như Coordinator) có thể truy cập
    public int MaterialSlotIndex => materialSlotIndex;
    public Material StandardMaterial => standardMaterial;
    public Material DissolveMaterial => dissolveMaterial;

    [SerializeField] private Renderer _targetRenderer;
    private MaterialPropertyBlock _propertyBlock;
    private int _dissolvePropertyID;

    private void OnEnable()
    {
        Initialize();
    }

    private void LateUpdate()
    {
        // Chỉ thực thi logic cập nhật nếu ở chế độ EditorAndPlay,
        // hoặc nếu đang ở trong Play Mode (cho cả hai chế độ).
        if (mode == ExecutionMode.EditorAndPlay || Application.isPlaying)
        {
            if (_targetRenderer == null) return;
            ApplyDissolveValue();
        }
    }

    private void OnValidate()
    {
        if (mode == ExecutionMode.EditorAndPlay && !Application.isPlaying)
        {
            Initialize();
            ApplyDissolveValue();
        }
    }

    private void Initialize()
    {
        if (_targetRenderer == null)
        {
            _targetRenderer = GetComponentInParent<Renderer>();
        }

        if (_propertyBlock == null)
        {
            _propertyBlock = new MaterialPropertyBlock();
        }

        _dissolvePropertyID = Shader.PropertyToID(DISSOLVE_PROPERTY_NAME);
    }

    public void SetDissolveValue(float value)
    {
        dissolveValue = value;
        ApplyDissolveValue();
    }
    private void ApplyDissolveValue()
    {
        if (_targetRenderer == null) return;

        _targetRenderer.GetPropertyBlock(_propertyBlock, materialSlotIndex);
        _propertyBlock.SetFloat(_dissolvePropertyID, dissolveValue);
        _targetRenderer.SetPropertyBlock(_propertyBlock, materialSlotIndex);
    }
}
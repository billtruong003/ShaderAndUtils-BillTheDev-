using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Renderer))]
public class InteractiveToonController : MonoBehaviour
{
    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;

    // Cache các ID của shader
    private static readonly int RimEnableID = Shader.PropertyToID("_RimEnable");
    private static readonly int FlashAmountID = Shader.PropertyToID("_FlashAmount");
    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");
    private static readonly int OutlineEnableID = Shader.PropertyToID("_EnableOutline");
    private static readonly int OutlineWidthID = Shader.PropertyToID("_OutlineWidth");

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        // Luôn lấy trạng thái hiện tại của material để không ghi đè giá trị set trong Inspector
        _renderer.GetPropertyBlock(_propBlock);
    }

    /// <summary>
    /// Bật/tắt viền outline cho đối tượng.
    /// </summary>
    public void SetOutline(bool enabled)
    {
        _propBlock.SetFloat(OutlineEnableID, enabled ? 1.0f : 0.0f);
        _renderer.SetPropertyBlock(_propBlock);
    }

    /// <summary>
    /// Bật/tắt hiệu ứng viền sáng (rim) để làm nổi bật lựa chọn.
    /// </summary>
    public void SetSelectionRim(bool enabled)
    {
        _propBlock.SetFloat(RimEnableID, enabled ? 1.0f : 0.0f);
        _renderer.SetPropertyBlock(_propBlock);
    }

    public void TriggerFlash(float duration = 0.25f)
    {
        StartCoroutine(FlashCoroutine(duration));
    }

    private IEnumerator FlashCoroutine(float duration)
    {
        LeanTween.value(gameObject, 1f, 0f, duration)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnUpdate((float val) =>
            {
                _propBlock.SetFloat(FlashAmountID, val);
                _renderer.SetPropertyBlock(_propBlock);
            });
        yield return null;
    }

    public void TriggerDissolve(bool appear, float duration = 0.5f)
    {
        float startValue = appear ? 0f : 1f;
        float endValue = appear ? 1f : 0f;
        LeanTween.value(gameObject, startValue, endValue, duration)
            .setEase(LeanTweenType.easeInQuad)
            .setOnUpdate((float val) =>
            {
                _propBlock.SetFloat(DissolveAmountID, val);
                _renderer.SetPropertyBlock(_propBlock);
            });
    }
}
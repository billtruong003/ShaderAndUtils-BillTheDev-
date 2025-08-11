using UnityEngine;
using System.Linq;
public class DissolveCoordinator : MonoBehaviour
{
    [SerializeField] private Renderer _targetRenderer;
    [SerializeField] private SingleDissolveAnimator[] _dissolveAnimators;

    private void Awake()
    {
        InitializeAndValidate();
    }

    public void ActivateDissolveState()
    {
        if (_targetRenderer == null || _dissolveAnimators == null) return;

        var newMaterials = _targetRenderer.materials;
        foreach (var animator in _dissolveAnimators)
        {
            newMaterials[animator.MaterialSlotIndex] = animator.DissolveMaterial;
        }
        _targetRenderer.materials = newMaterials;
    }

    public void RestoreStandardState()
    {
        if (_targetRenderer == null || _dissolveAnimators == null) return;

        var standardMaterials = _targetRenderer.materials;
        foreach (var animator in _dissolveAnimators)
        {
            standardMaterials[animator.MaterialSlotIndex] = animator.StandardMaterial;
        }
        _targetRenderer.materials = standardMaterials;
    }

    private void InitializeAndValidate()
    {
        if (_targetRenderer == null)
            _targetRenderer = GetComponent<Renderer>();

        if (_dissolveAnimators.Length == 0)
        {
            return;
        }

        int materialCount = _targetRenderer.sharedMaterials.Length;
        var indices = _dissolveAnimators.Select(a => a.MaterialSlotIndex);

        if (indices.Any(i => i < 0 || i >= materialCount))
        {
            Debug.LogError($"Lỗi cấu hình: Một SingleDissolveAnimator có MaterialSlotIndex nằm ngoài phạm vi [0, {materialCount - 1}] của Renderer.", this);
            enabled = false;
            return;
        }

        if (indices.Distinct().Count() != indices.Count())
        {
            Debug.LogError("Lỗi cấu hình: Có nhiều SingleDissolveAnimator cùng trỏ vào một MaterialSlotIndex. Mỗi index phải là duy nhất.", this);
            enabled = false;
        }
    }
}
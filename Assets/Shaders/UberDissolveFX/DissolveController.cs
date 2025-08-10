#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
#endif

using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Renderer))]
public class AdvancedDissolveController : MonoBehaviour
{
    // Struct để định nghĩa một cặp material (chuẩn và dissolve)
    [System.Serializable]
    public struct MaterialPair
    {
        [Required("Phải gán Standard Material.")]
        [AssetsOnly]
        public Material StandardMaterial;

        [Required("Phải gán Dissolve Material.")]
        [AssetsOnly]
        public Material DissolveMaterial;
    }

    [Title("Cấu Hình Hiệu Ứng", bold: true)]
    [InfoBox("Kéo các cặp Material vào danh sách bên dưới. Script sẽ tự động tìm và thay thế các material tương ứng trên Renderer.", InfoMessageType.None)]

    [ListDrawerSettings(Expanded = true, AddCopiesLastElement = true)]
    [ValidateInput("HasAtLeastOnePair", "Cần có ít nhất một cặp Material để script hoạt động.")]
    [ValidateInput("ArePairsValid", "Một hoặc nhiều cặp Material không hợp lệ (bị thiếu hoặc trùng lặp).")]
    public List<MaterialPair> materialPairs = new List<MaterialPair>();

    [Title("Thiết Lập Animation", bold: true)]
    [Tooltip("Thời gian (giây) để hoàn thành hiệu ứng tan biến.")]
    [Min(0.1f)]
    public float dissolveDuration = 2.0f;

    private Renderer _renderer;
    private Coroutine _dissolveCoroutine;

    private readonly Dictionary<Material, Material> _standardToDissolveMap = new Dictionary<Material, Material>();
    private readonly Dictionary<Material, Material> _dissolveToStandardMap = new Dictionary<Material, Material>();

    private static readonly int DissolveAmountID = Shader.PropertyToID("_DissolveAmount");

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        InitializeMaterialMaps();
    }

    private void OnDisable()
    {
        StopRunningCoroutine();
    }

    // --- Public API for Code Control ---
    [Button("Bắt Đầu Tan Biến (Dissolve)", ButtonSizes.Large), GUIColor(1.0f, 0.6f, 0.6f)]
    public void StartDissolve()
    {
        StartEffect(false);
    }

    [Button("Bắt Đầu Xuất Hiện (Appear)", ButtonSizes.Large), GUIColor(0.6f, 1.0f, 0.6f)]
    public void StartAppear()
    {
        StartEffect(true);
    }

    public void ActivateDissolveMaterials()
    {
        ActivateMaterials(true);
    }

    public void ActivateStandardMaterials()
    {
        ActivateMaterials(false);
    }

    public void DeactivateGameObject()
    {
        gameObject.SetActive(false);
    }

    private void StartEffect(bool isAppearing)
    {
        StopRunningCoroutine();

        if (isAppearing)
        {
            gameObject.SetActive(true);
        }

        _dissolveCoroutine = StartCoroutine(DissolveRoutine(isAppearing));
    }

    private IEnumerator DissolveRoutine(bool isAppearing)
    {
        ActivateDissolveMaterials();

        float elapsedTime = 0f;
        float startValue = isAppearing ? 1f : 0f;
        float endValue = isAppearing ? 0f : 1f;

        // Lấy danh sách các material dissolve cần animate
        var dissolveMaterialsToAnimate = _standardToDissolveMap.Values;

        while (elapsedTime < dissolveDuration)
        {
            elapsedTime += Time.deltaTime;
            float newDissolveValue = Mathf.Lerp(startValue, endValue, elapsedTime / dissolveDuration);

            foreach (var mat in dissolveMaterialsToAnimate)
            {
                mat.SetFloat(DissolveAmountID, newDissolveValue);
            }
            yield return null;
        }

        foreach (var mat in dissolveMaterialsToAnimate)
        {
            mat.SetFloat(DissolveAmountID, endValue);
        }

        if (isAppearing)
        {
            ActivateStandardMaterials();
        }
        else
        {
            DeactivateGameObject();
        }
    }

    private void InitializeMaterialMaps()
    {
        _standardToDissolveMap.Clear();
        _dissolveToStandardMap.Clear();
        foreach (var pair in materialPairs)
        {
            if (pair.StandardMaterial != null && pair.DissolveMaterial != null)
            {
                _standardToDissolveMap[pair.StandardMaterial] = pair.DissolveMaterial;
                _dissolveToStandardMap[pair.DissolveMaterial] = pair.StandardMaterial;
            }
        }
    }

    private void ActivateMaterials(bool useDissolve)
    {
        var targetMap = useDissolve ? _standardToDissolveMap : _dissolveToStandardMap;
        var currentMaterials = _renderer.materials;

        bool materialsChanged = false;
        for (int i = 0; i < currentMaterials.Length; i++)
        {
            if (targetMap.TryGetValue(currentMaterials[i], out Material newMaterial))
            {
                currentMaterials[i] = newMaterial;
                materialsChanged = true;
            }
        }

        if (materialsChanged)
        {
            _renderer.materials = currentMaterials;
        }
    }

    private void StopRunningCoroutine()
    {
        if (_dissolveCoroutine != null)
        {
            StopCoroutine(_dissolveCoroutine);
            _dissolveCoroutine = null;
        }
    }

    private bool HasAtLeastOnePair()
    {
        return materialPairs.Count > 0;
    }

    private bool ArePairsValid()
    {
        var standardMaterials = new HashSet<Material>();
        var dissolveMaterials = new HashSet<Material>();

        foreach (var pair in materialPairs)
        {
            if (pair.StandardMaterial == null || pair.DissolveMaterial == null) return false;
            if (!standardMaterials.Add(pair.StandardMaterial)) return false; // Trùng lặp
            if (!dissolveMaterials.Add(pair.DissolveMaterial)) return false; // Trùng lặp
        }
        return true;
    }
}
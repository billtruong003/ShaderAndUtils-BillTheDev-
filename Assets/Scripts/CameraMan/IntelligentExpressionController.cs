using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

[AddComponentMenu("Character/Intelligent Expression Controller")]
public class IntelligentExpressionController : MonoBehaviour
{
    // SECTION: Core Configuration
    [TitleGroup("Core Setup")]
    [Required("Phải cung cấp ít nhất một Skinned Mesh Renderer để component hoạt động.")]
    [OnValueChanged("BuildBlendShapeCache")]
    [SerializeField] private List<SkinnedMeshRenderer> targetMeshes = new List<SkinnedMeshRenderer>();

    // SECTION: Expression Pool
    [TitleGroup("Expression Control")]
    [InfoBox("Kéo thả các 'Expression Profile' bạn muốn sử dụng vào danh sách bên dưới. Chế độ Live Edit sẽ hoạt động trực tiếp trên các profile này.")]
    [SerializeField] private List<ExpressionProfile> expressionPool = new List<ExpressionProfile>();

    // SECTION: Live Editing Mode
    [TitleGroup("Live Editing", "Chế độ chỉnh sửa trực tiếp không cần vào Play Mode")]
    [OnValueChanged("OnLiveEditModeToggled")]
    [LabelWidth(150)]
    [SerializeField] private bool isLiveEditMode;

    [ShowIf("isLiveEditMode")]
    [Range(0f, 1f)]
    [OnValueChanged("ReapplyActiveLiveProfile")]
    [LabelText("Live Edit Intensity")]
    [SerializeField] private float liveEditIntensity = 1f;

    // SECTION: Private State
    private Coroutine expressionCycleCoroutine;
    private ExpressionProfile activeProfile;
    private ExpressionProfile liveEditActiveProfile;
    private int lastActiveProfileIndex = -1;

    private readonly Dictionary<string, List<(SkinnedMeshRenderer renderer, int blendShapeIndex)>> blendShapeCache =
        new Dictionary<string, List<(SkinnedMeshRenderer, int)>>();

    private static List<string> availableBlendShapeNames = new List<string>();

#if UNITY_EDITOR
    private void OnEnable()
    {
        ExpressionProfile.OnProfileChanged += HandleProfileUpdateInEditor;
        BuildBlendShapeCache();
    }

    private void OnDisable()
    {
        ExpressionProfile.OnProfileChanged -= HandleProfileUpdateInEditor;
        if (isLiveEditMode)
        {
            ResetAllBlendShapesToNeutral();
        }
    }

    private void HandleProfileUpdateInEditor(ExpressionProfile updatedProfile)
    {
        if (isLiveEditMode && expressionPool.Contains(updatedProfile))
        {
            ValidateCache();
            liveEditActiveProfile = updatedProfile;
            ApplyProfileStatically(liveEditActiveProfile, liveEditIntensity);
        }
    }
#endif

    private void Start()
    {
        if (Application.isPlaying && !isLiveEditMode)
        {
            if (!IsConfigurationValid()) return;
            ValidateCache();
            StartExpressionCycle();
        }
    }

    private void SetBlendShapeWeight(string blendShapeName, float weight)
    {
        if (!blendShapeCache.TryGetValue(blendShapeName, out var targets)) return;

        foreach (var (renderer, index) in targets)
        {
            if (renderer != null && index >= 0 && index < renderer.sharedMesh.blendShapeCount)
            {
                // 1. Thay đổi giá trị
                renderer.SetBlendShapeWeight(index, weight);

                // 2. ÉP BUỘC EDITOR CẬP NHẬT - ĐÂY LÀ DÒNG LỆNH QUYẾT ĐỊNH
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(renderer);
                }
#endif
            }
        }
    }

    private void ApplyProfileStatically(ExpressionProfile profile, float intensity)
    {
        ResetAllBlendShapesToNeutral();
        if (profile == null) return;

        foreach (var setting in profile.ShapeSettings)
        {
            if (!string.IsNullOrEmpty(setting.BlendShapeName))
            {
                SetBlendShapeWeight(setting.BlendShapeName, setting.Weight * intensity);
            }
        }
    }

    [Button("Force Rebuild BlendShape Cache", ButtonSizes.Medium), GUIColor(0.2f, 0.8f, 1f)]
    private void BuildBlendShapeCache()
    {
        blendShapeCache.Clear();
        var uniqueNames = new HashSet<string>();
        if (targetMeshes == null) return;

        foreach (var meshRenderer in targetMeshes.Where(m => m != null && m.sharedMesh != null))
        {
            for (int i = 0; i < meshRenderer.sharedMesh.blendShapeCount; i++)
            {
                string blendShapeName = meshRenderer.sharedMesh.GetBlendShapeName(i);
                uniqueNames.Add(blendShapeName);
                if (!blendShapeCache.ContainsKey(blendShapeName))
                {
                    blendShapeCache[blendShapeName] = new List<(SkinnedMeshRenderer, int)>();
                }
                blendShapeCache[blendShapeName].Add((meshRenderer, i));
            }
        }
        availableBlendShapeNames = uniqueNames.OrderBy(n => n).ToList();
    }

    private void ResetAllBlendShapesToNeutral()
    {
        ValidateCache();
        foreach (var blendShapeName in blendShapeCache.Keys)
        {
            SetBlendShapeWeight(blendShapeName, 0f);
        }
    }

    private void OnLiveEditModeToggled()
    {
        ValidateCache();
        ResetAllBlendShapesToNeutral();
        liveEditActiveProfile = null;

        if (!isLiveEditMode && Application.isPlaying)
        {
            StartExpressionCycle();
        }
        else
        {
            StopExpressionCycle();
        }
    }

    private void ReapplyActiveLiveProfile()
    {
        if (isLiveEditMode && liveEditActiveProfile != null)
        {
            ApplyProfileStatically(liveEditActiveProfile, liveEditIntensity);
        }
    }

    private void ValidateCache()
    {
        if (blendShapeCache == null || blendShapeCache.Count == 0 || availableBlendShapeNames.Count == 0)
        {
            BuildBlendShapeCache();
        }
    }

    public static List<string> GetAvailableBlendShapeNames() => availableBlendShapeNames;

    // ----- Các phương thức cho Runtime không thay đổi -----
    #region Unchanged Runtime Logic

    [TitleGroup("Live Status", boldTitle: false)]
    [HideIf("isLiveEditMode")]
    [ReadOnly]
    [ShowInInspector] private string ActiveExpressionName => activeProfile?.name ?? "Neutral";

    [HideIf("isLiveEditMode")]
    [ProgressBar(0, 1, ColorGetter = "GetTransitionProgressColor")]
    [ShowInInspector]
    [ReadOnly] private float transitionProgress;

    [TitleGroup("Timing & Transition")]
    [HideIf("isLiveEditMode")]
    [BoxGroup("Timing & Transition/Settings")]
    [MinMaxSlider(0.5f, 30f, true)]
    [SerializeField] private Vector2 intervalRange = new Vector2(3f, 10f);

    [HideIf("isLiveEditMode")]
    [BoxGroup("Timing & Transition/Settings")]
    [Range(0.1f, 2f), SuffixLabel("seconds")]
    [SerializeField] private float transitionDuration = 0.4f;

    private void StartExpressionCycle()
    {
        if (isLiveEditMode) return;
        StopExpressionCycle();
        expressionCycleCoroutine = StartCoroutine(RunExpressionCycle());
    }

    private void StopExpressionCycle()
    {
        if (expressionCycleCoroutine != null)
        {
            StopCoroutine(expressionCycleCoroutine);
            expressionCycleCoroutine = null;
        }
    }

    private IEnumerator RunExpressionCycle()
    {
        yield return TransitionToProfile(null, transitionDuration);
        while (true)
        {
            float waitTime = Random.Range(intervalRange.x, intervalRange.y);
            yield return new WaitForSeconds(waitTime);

            int nextIndex = GetRandomNextProfileIndex();
            if (nextIndex < 0) continue;

            ExpressionProfile nextProfile = expressionPool[nextIndex];
            yield return TransitionToProfile(nextProfile, transitionDuration);
            lastActiveProfileIndex = nextIndex;
        }
    }

    private IEnumerator TransitionToProfile(ExpressionProfile toProfile, float duration)
    {
        if (isLiveEditMode) yield break;

        ExpressionProfile fromProfile = activeProfile;
        activeProfile = toProfile;

        HashSet<string> allInvolvedShapes = GetAllInvolvedBlendShapes(fromProfile, toProfile);
        List<(string name, float startWeight, float endWeight)> transitionData = BuildTransitionData(allInvolvedShapes, fromProfile, toProfile);

        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            transitionProgress = (duration > 0) ? Mathf.Clamp01(elapsedTime / duration) : 1f;
            float easedProgress = Mathf.SmoothStep(0, 1, transitionProgress);

            foreach (var data in transitionData)
            {
                float currentWeight = Mathf.Lerp(data.startWeight, data.endWeight, easedProgress);
                SetBlendShapeWeight(data.name, currentWeight);
            }
            yield return null;
        }

        foreach (var data in transitionData)
        {
            SetBlendShapeWeight(data.name, data.endWeight);
        }
        transitionProgress = 0f;
    }

    private HashSet<string> GetAllInvolvedBlendShapes(ExpressionProfile from, ExpressionProfile to)
    {
        var names = new HashSet<string>();
        if (from != null) { foreach (var setting in from.ShapeSettings) names.Add(setting.BlendShapeName); }
        if (to != null) { foreach (var setting in to.ShapeSettings) names.Add(setting.BlendShapeName); }
        return names;
    }

    private List<(string name, float startWeight, float endWeight)> BuildTransitionData(HashSet<string> shapeNames, ExpressionProfile from, ExpressionProfile to)
    {
        var data = new List<(string, float, float)>();
        var fromWeights = from?.ShapeSettings.ToDictionary(s => s.BlendShapeName, s => s.Weight) ?? new Dictionary<string, float>();
        var toWeights = to?.ShapeSettings.ToDictionary(s => s.BlendShapeName, s => s.Weight) ?? new Dictionary<string, float>();

        foreach (string name in shapeNames)
        {
            fromWeights.TryGetValue(name, out float startWeight);
            toWeights.TryGetValue(name, out float endWeight);
            data.Add((name, startWeight, endWeight));
        }
        return data;
    }

    private int GetRandomNextProfileIndex()
    {
        int poolCount = expressionPool.Count(p => p != null);
        if (poolCount == 0) return -1;
        if (poolCount == 1) return expressionPool.FindIndex(p => p != null);

        int nextIndex;
        do { nextIndex = Random.Range(0, expressionPool.Count); }
        while (nextIndex == lastActiveProfileIndex || expressionPool[nextIndex] == null);

        return nextIndex;
    }

    private bool IsConfigurationValid() => targetMeshes.Count > 0 && !targetMeshes.Any(m => m == null) &&
                                            expressionPool.Count > 0 && !expressionPool.Any(p => p == null);

    private Color GetTransitionProgressColor() => Color.Lerp(new Color(0.4f, 0.8f, 1f), Color.green, transitionProgress);
    #endregion
}
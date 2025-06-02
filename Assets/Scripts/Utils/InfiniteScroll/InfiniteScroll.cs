using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
[AddComponentMenu("UI/Infinite Scroll", 152)]
public class InfiniteScroll : MonoBehaviour
{
    [Header("Infinite Scroll Settings")]
    [SerializeField] private bool enableInfiniteScroll;
    [SerializeField] private bool scrollHorizontal;
    [SerializeField] private bool scrollVertical = true;
    [SerializeField] private float scrollSpeed = 50f;
    [SerializeField] private bool enableCenterSnapping;
    [SerializeField] private float snapSpeed = 10f;
    [SerializeField] private bool enablePickingFocus;
    [SerializeField] private int focusIndex;
    [SerializeField] private bool useScaleCurve;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0, 1, 1, 1);
    [SerializeField] private float scaleCurveFactor = 1f;

    private ScrollRect scrollRect;
    private RectTransform content;
    private float currentScrollOffset;
    private readonly List<RectTransform> childRects = new List<RectTransform>(16);
    private float targetScrollOffset;
    private bool isSnapping;

    private void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        content = scrollRect.content;
        scrollRect.onValueChanged.AddListener(OnScroll);
    }

    private void OnDestroy()
    {
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScroll);
        }
    }

    private void Update()
    {
        if (!enableInfiniteScroll || !enableCenterSnapping || !isSnapping)
            return;

        // Center snapping logic
        currentScrollOffset = Mathf.Lerp(currentScrollOffset, targetScrollOffset, snapSpeed * Time.deltaTime);
        UpdateChildPositions();
        if (Mathf.Abs(currentScrollOffset - targetScrollOffset) < 0.01f)
        {
            currentScrollOffset = targetScrollOffset;
            isSnapping = false;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }
        if (scrollRect != null)
        {
            scrollRect.onValueChanged.RemoveListener(OnScroll);
            if (enableInfiniteScroll)
            {
                scrollRect.onValueChanged.AddListener(OnScroll);
            }
        }
        if (content != null)
        {
            UpdateChildPositions();
        }
    }
#endif

    private void OnScroll(Vector2 scrollValue)
    {
        if (!enableInfiniteScroll)
            return;

        if (scrollHorizontal)
        {
            currentScrollOffset += scrollValue.x * scrollSpeed * Time.deltaTime;
        }
        if (scrollVertical)
        {
            currentScrollOffset += scrollValue.y * scrollSpeed * Time.deltaTime;
        }

        if (enableCenterSnapping)
        {
            // Tìm phần tử gần trung tâm nhất
            int closestIndex = GetClosestChildIndex();
            targetScrollOffset = closestIndex;
            isSnapping = true;
        }

        UpdateChildPositions();
    }

    private void UpdateChildPositions()
    {
        childRects.Clear();
        int childCount = content.childCount;
        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = (RectTransform)content.GetChild(i);
            if (child.gameObject.activeInHierarchy)
            {
                childRects.Add(child);
            }
        }

        int activeChildCount = childRects.Count;
        if (activeChildCount == 0)
            return;

        for (int i = 0; i < activeChildCount; i++)
        {
            RectTransform child = childRects[i];
            float t = enableInfiniteScroll
                ? Mathf.Repeat((i + currentScrollOffset) / (activeChildCount - 1f), 1f)
                : i / (float)(activeChildCount - 1);

            if (useScaleCurve)
            {
                float scale = scaleCurve.Evaluate(t * scaleCurveFactor);
                child.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                child.localScale = Vector3.one;
            }

            // Cập nhật vị trí dựa trên bố trí gốc (RadialLayoutGroup sẽ xử lý)
            content.GetComponent<LayoutGroup>()?.SetLayoutHorizontal();
        }

        // Picking Focus
        if (enablePickingFocus && focusIndex >= 0 && focusIndex < activeChildCount)
        {
            currentScrollOffset = focusIndex;
            targetScrollOffset = focusIndex;
            isSnapping = true;
            UpdateChildPositions();
        }
    }

    private int GetClosestChildIndex()
    {
        int activeChildCount = childRects.Count;
        if (activeChildCount == 0)
            return 0;

        float minDistance = float.MaxValue;
        int closestIndex = 0;
        for (int i = 0; i < activeChildCount; i++)
        {
            float t = Mathf.Repeat((i + currentScrollOffset) / (activeChildCount - 1f), 1f);
            float distance = Mathf.Abs(t - 0.5f); // Trung tâm là t=0.5
            if (distance < minDistance)
            {
                minDistance = distance;
                closestIndex = i;
            }
        }
        return closestIndex;
    }
}
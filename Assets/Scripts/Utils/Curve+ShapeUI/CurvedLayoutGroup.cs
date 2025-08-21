using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Layout/Curved Radial Layout Group", 150)]
[ExecuteAlways]
public class CurvedLayoutGroup : LayoutGroup
{
    public enum LayoutType
    {
        Curved,
        Radial,
        Spiral,
    }

    public enum LayoutAxis
    {
        Horizontal,
        Vertical
    }

    [Header("Layout Configuration")]
    public LayoutType layoutType = LayoutType.Curved;
    public Vector2 positionOffset = Vector2.zero;
    public float globalRotationOffset = 0f;
    public bool rotateChildren = false;
    public bool ignoreInactiveChildren = true;

    [Header("Child Sizing")]
    public bool controlChildSize = false;
    public Vector2 childSize = new Vector2(100, 100);

    [Header("Curved Layout Settings")]
    public LayoutAxis layoutAxis = LayoutAxis.Horizontal;
    public float pathWidth = 500f;
    public float pathHeight = 100f;
    [Range(0, 1)] public float pathStart = 0f;
    [Range(0, 1)] public float pathEnd = 1f;
    public AnimationCurve shapeCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));

    [Header("Radial & Spiral Distribution")]
    [Tooltip("Modifies radius for Radial layout or distance increment for Spiral layout.")]
    public AnimationCurve distributionCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 1));
    public float distributionScale = 1f;

    [Header("Radial Layout Settings")]
    public float startAngle = 0f;
    public float endAngle = 360f;
    public float radius = 200f;
    public bool fitToAngleRange = true;

    [Header("Spiral Layout Settings")]
    public float spiralStartAngle = 0f;
    public float spiralStartDistance = 0f;
    public float spiralAngleIncrement = 30f;
    public float spiralDistanceIncrement = 10f;

    private readonly List<RectTransform> m_ActiveChildren = new List<RectTransform>();

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateLayout();
    }

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        UpdateActiveChildrenList();
        CalculateLayoutSize();
    }

    public override void CalculateLayoutInputVertical()
    {
    }

    public override void SetLayoutHorizontal()
    {
        ArrangeElements();
    }

    public override void SetLayoutVertical()
    {
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (pathEnd < pathStart) pathEnd = pathStart;
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }
#endif

    public void UpdateLayout()
    {
        if (!IsActive()) return;
        LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
    }

    private void UpdateActiveChildrenList()
    {
        m_ActiveChildren.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i) as RectTransform;
            if (child != null && (!ignoreInactiveChildren || child.gameObject.activeInHierarchy))
            {
                m_ActiveChildren.Add(child);
            }
        }
    }

    private void CalculateLayoutSize()
    {
        if (m_ActiveChildren.Count == 0)
        {
            SetLayoutInputForAxis(0, 0, 0, 0);
            SetLayoutInputForAxis(0, 0, 0, 1);
            return;
        }

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < m_ActiveChildren.Count; i++)
        {
            RectTransform child = m_ActiveChildren[i];
            Vector2 position = GetChildPosition(i, m_ActiveChildren.Count);
            Vector2 size = controlChildSize ? childSize : child.sizeDelta;
            Vector2 pivotOffset = new Vector2(size.x * child.pivot.x, size.y * child.pivot.y);
            Vector2 childMin = position - pivotOffset;
            Vector2 childMax = position - pivotOffset + size;

            min.x = Mathf.Min(min.x, childMin.x);
            min.y = Mathf.Min(min.y, childMin.y);
            max.x = Mathf.Max(max.x, childMax.x);
            max.y = Mathf.Max(max.y, childMax.y);
        }

        float totalWidth = max.x - min.x;
        float totalHeight = max.y - min.y;

        SetLayoutInputForAxis(totalWidth, totalWidth, -1, 0);
        SetLayoutInputForAxis(totalHeight, totalHeight, -1, 1);
    }

    private void ArrangeElements()
    {
        m_Tracker.Clear();
        UpdateActiveChildrenList();

        if (m_ActiveChildren.Count == 0) return;

        for (int i = 0; i < m_ActiveChildren.Count; i++)
        {
            RectTransform child = m_ActiveChildren[i];
            Vector2 position = GetChildPosition(i, m_ActiveChildren.Count);
            Quaternion rotation = GetChildRotation(position, i, m_ActiveChildren.Count);

            child.anchoredPosition = position;
            child.rotation = rotation;

            ApplyDrivenProperties(child);
        }
    }

    private Vector2 GetChildPosition(int index, int childCount)
    {
        switch (layoutType)
        {
            case LayoutType.Curved: return GetCurvedPosition(index, childCount);
            case LayoutType.Radial: return GetRadialPosition(index, childCount);
            case LayoutType.Spiral: return GetSpiralPosition(index, childCount);
            default: return Vector2.zero;
        }
    }

    private Quaternion GetChildRotation(Vector2 childPosition, int index, int childCount)
    {
        if (!rotateChildren)
        {
            return Quaternion.Euler(0, 0, globalRotationOffset);
        }

        float angle = 0;
        switch (layoutType)
        {
            case LayoutType.Curved:
                float normalizedTime = pathStart + (GetNormalizedIndex(index, childCount) * (pathEnd - pathStart));
                angle = GetCurveTangentAngle(normalizedTime, 0.01f);
                break;
            case LayoutType.Radial:
                float totalAngle = endAngle - startAngle;
                float angleStep = totalAngle / (fitToAngleRange && childCount > 1 ? (childCount - 1) : childCount);
                angle = startAngle + index * angleStep;
                angle -= 90f; // Align child's 'up' to the radial direction
                break;
            case LayoutType.Spiral:
                float spiralAngle = spiralStartAngle + (index * spiralAngleIncrement);
                angle = spiralAngle - 90f;
                break;
        }

        return Quaternion.Euler(0, 0, angle + globalRotationOffset);
    }

    private Vector2 GetCurvedPosition(int index, int count)
    {
        float normalizedIndex = GetNormalizedIndex(index, count);
        float time = pathStart + (normalizedIndex * (pathEnd - pathStart));

        float x = (-pathWidth / 2f) + (time * pathWidth);
        float y = shapeCurve.Evaluate(time) * pathHeight;

        return (layoutAxis == LayoutAxis.Horizontal ? new Vector2(x, y) : new Vector2(y, x)) + positionOffset;
    }

    private float GetCurveTangentAngle(float time, float delta)
    {
        float t1 = Mathf.Clamp01(time - delta);
        float t2 = Mathf.Clamp01(time + delta);

        float x1 = (-pathWidth / 2f) + (t1 * pathWidth);
        float y1 = shapeCurve.Evaluate(t1) * pathHeight;
        Vector2 p1 = (layoutAxis == LayoutAxis.Horizontal) ? new Vector2(x1, y1) : new Vector2(y1, x1);

        float x2 = (-pathWidth / 2f) + (t2 * pathWidth);
        float y2 = shapeCurve.Evaluate(t2) * pathHeight;
        Vector2 p2 = (layoutAxis == LayoutAxis.Horizontal) ? new Vector2(x2, y2) : new Vector2(y2, x2);

        Vector2 direction = (p2 - p1).normalized;
        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private Vector2 GetRadialPosition(int index, int count)
    {
        float totalAngle = endAngle - startAngle;
        float angleStep = totalAngle / (fitToAngleRange && count > 1 ? (count - 1) : count);
        float angle = startAngle + index * angleStep;
        float radAngle = angle * Mathf.Deg2Rad;

        float normalizedIndex = GetNormalizedIndex(index, count);
        float distributionValue = distributionCurve.Evaluate(normalizedIndex) * distributionScale;
        float currentRadius = radius * distributionValue;

        float x = Mathf.Cos(radAngle) * currentRadius;
        float y = Mathf.Sin(radAngle) * currentRadius;

        return new Vector2(x, y) + positionOffset;
    }

    private Vector2 GetSpiralPosition(int index, int count)
    {
        float normalizedIndex = GetNormalizedIndex(index, count);
        float distributionValue = distributionCurve.Evaluate(normalizedIndex) * distributionScale;

        float angle = spiralStartAngle + (index * spiralAngleIncrement);
        float distance = spiralStartDistance + (index * spiralDistanceIncrement * distributionValue);

        float radAngle = angle * Mathf.Deg2Rad;
        float x = Mathf.Cos(radAngle) * distance;
        float y = Mathf.Sin(radAngle) * distance;

        return new Vector2(x, y) + positionOffset;
    }

    private float GetNormalizedIndex(int index, int count)
    {
        return (count > 1) ? (float)index / (count - 1) : 0.5f;
    }

    private void ApplyDrivenProperties(RectTransform child)
    {
        var properties = DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Rotation;
        if (controlChildSize)
        {
            properties |= DrivenTransformProperties.SizeDelta;
            child.sizeDelta = childSize;
        }
        m_Tracker.Add(this, child, properties);
    }
}
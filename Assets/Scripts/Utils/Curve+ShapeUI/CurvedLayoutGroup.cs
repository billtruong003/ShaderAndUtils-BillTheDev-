using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[AddComponentMenu("Layout/Curved Radial Layout Group", 150)]
[ExecuteAlways]
public class CurvedLayoutGroup : LayoutGroup
{
    [Header("General Settings")]
    public bool ignoreInactive;
    public float rotateOffset;

    [Header("Animation Curve Settings")]
    public bool useAnimationCurve;
    public AnimationCurve curve;
    public float curveScale = 1;

    [Header("Layout Type")]
    public LayoutType layoutType = LayoutType.Curved;

    [Header("Curved Settings")]
    public bool horizontal;
    public float width;
    public float height;

    [Header("Radial Settings")]
    public float StartAngle = 0;
    public float EndAngle = 360;
    public bool rotateTowards = false;
    public float radius = 5f;

    [Header("Spiral Settings")]
    public float spiralAngleIncrement = 30f;
    public float spiralDistanceIncrement = 1f;

    protected override void OnEnable()
    {
        base.OnEnable();
        CalculateLayoutInputHorizontal();
        CalculateLayoutInputVertical();
        SetLayoutHorizontal();
    }

    public override void CalculateLayoutInputHorizontal()
    {
        float minWidth, preferredWidth;
        switch (layoutType)
        {
            case LayoutType.Radial:
                minWidth = preferredWidth = 2 * radius;
                break;
            case LayoutType.Spiral:
                float childCount = transform.childCount;
                float maxDistance = 0f;
                float tempAngle = StartAngle;
                for (int i = 0; i < childCount; i++)
                {
                    float t = childCount > 1 ? i / (childCount - 1f) : 0f;
                    float curveValue = useAnimationCurve ? curve.Evaluate(t * curveScale) : 1f;
                    maxDistance += spiralDistanceIncrement * curveValue;
                    tempAngle += spiralAngleIncrement;
                }
                minWidth = preferredWidth = 2 * maxDistance;
                break;
            case LayoutType.Curved:
            case LayoutType.CustomCurve:
            default:
                minWidth = preferredWidth = width;
                break;
        }
        SetLayoutInputForAxis(minWidth, preferredWidth, -1, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        float minHeight, preferredHeight;
        switch (layoutType)
        {
            case LayoutType.Radial:
                minHeight = preferredHeight = 2 * radius;
                break;
            case LayoutType.Spiral:
                float childCount = transform.childCount;
                float maxDistance = 0f;
                float tempAngle = StartAngle;
                for (int i = 0; i < childCount; i++)
                {
                    float t = childCount > 1 ? i / (childCount - 1f) : 0f;
                    float curveValue = useAnimationCurve ? curve.Evaluate(t * curveScale) : 1f;
                    maxDistance += spiralDistanceIncrement * curveValue;
                    tempAngle += spiralAngleIncrement;
                }
                minHeight = preferredHeight = 2 * maxDistance;
                break;
            case LayoutType.Curved:
            case LayoutType.CustomCurve:
            default:
                minHeight = preferredHeight = 2 * height;
                break;
        }
        SetLayoutInputForAxis(minHeight, preferredHeight, -1, 1);
    }

    public override void SetLayoutHorizontal()
    {
        CalcAlongCurve();
    }

    public override void SetLayoutVertical()
    {
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        CalculateLayoutInputHorizontal();
        CalculateLayoutInputVertical();
        SetLayoutHorizontal();
    }
#endif

    void CalcAlongCurve()
    {
        m_Tracker.Clear();
        if (transform.childCount == 0)
            return;

        List<RectTransform> childRects = new List<RectTransform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = (RectTransform)transform.GetChild(i);
            if (ignoreInactive && !child.gameObject.activeInHierarchy)
                continue;
            childRects.Add(child);
        }

        float childCount = childRects.Count;
        if (childCount == 0)
            return;

        switch (layoutType)
        {
            case LayoutType.Curved:
                SetCurvedLayout(childRects, childCount);
                break;
            case LayoutType.Radial:
                SetRadialLayout(childRects, childCount);
                break;
            case LayoutType.Spiral:
                SetSpiralLayout(childRects, childCount);
                break;
            case LayoutType.CustomCurve:
                SetCustomCurveLayout(childRects, childCount);
                break;
        }
    }

    private void SetCurvedLayout(List<RectTransform> childRects, float childCount)
    {
        float spacing = childCount > 1 ? width / (childCount - 1) : 0;
        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = childRects[i];
            float t = childCount > 1 ? i / (childCount - 1f) : 0.5f;
            float x = childCount > 1 ? (-width / 2f) + (i * spacing) : 0;
            float y = useAnimationCurve ? curve.Evaluate(t) * height : Mathf.Sin(t * Mathf.PI) * height;

            child.anchoredPosition = horizontal ? new Vector2(x, y) : new Vector2(y, x);
            child.rotation = Quaternion.Euler(0, 0, rotateOffset);
            m_Tracker.Add(this, child, DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Rotation);
        }
    }

    private void SetRadialLayout(List<RectTransform> childRects, float childCount)
    {
        if (childCount == 1)
        {
            RectTransform child = childRects[0];
            child.anchoredPosition = Vector2.zero;
            child.rotation = Quaternion.Euler(0, 0, rotateOffset);
            m_Tracker.Add(this, child, DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Rotation);
            return;
        }

        float angleStep = (EndAngle - StartAngle) / Mathf.Max(1, childCount);
        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = childRects[i];
            float t = childCount > 1 ? i / (childCount) : 0f;
            float extra = useAnimationCurve ? curve.Evaluate(t * curveScale) : 1f;
            float angle = StartAngle + i * angleStep;
            float radAngle = angle * Mathf.Deg2Rad;
            float extraRadius = radius * extra;
            float x = Mathf.Cos(radAngle) * extraRadius;
            float y = Mathf.Sin(radAngle) * extraRadius;
            child.anchoredPosition = new Vector2(x, y);

            if (rotateTowards)
            {
                child.rotation = Quaternion.Euler(0, 0, angle + rotateOffset - 90);
            }
            else
            {
                child.rotation = Quaternion.Euler(0, 0, rotateOffset);
            }

            m_Tracker.Add(this, child, DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Rotation);
        }
    }

    private void SetSpiralLayout(List<RectTransform> childRects, float childCount)
    {
        float angle = StartAngle;
        float distance = 0f;
        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = childRects[i];
            float t = childCount > 1 ? i / (childCount - 1f) : 0f;
            float curveValue = useAnimationCurve ? curve.Evaluate(t * curveScale) : 1f;
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * distance;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * distance;
            child.anchoredPosition = new Vector2(x, y);

            if (rotateTowards)
            {
                child.rotation = Quaternion.Euler(0, 0, angle + rotateOffset - 90);
            }
            else
            {
                child.rotation = Quaternion.Euler(0, 0, rotateOffset);
            }

            m_Tracker.Add(this, child, DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Rotation);
            distance += spiralDistanceIncrement * curveValue; // Tăng sau khi đặt vị trí
            angle += spiralAngleIncrement;
        }
    }

    private void SetCustomCurveLayout(List<RectTransform> childRects, float childCount)
    {
        float spacing = childCount > 1 ? width / (childCount - 1) : 0;
        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = childRects[i];
            float t = childCount > 1 ? i / (childCount - 1f) : 0.5f;
            float x = -width / 2f + i * spacing;
            float y = curve.Evaluate(t * curveScale) * height; // Thêm curveScale
            child.anchoredPosition = horizontal ? new Vector2(x, y) : new Vector2(y, x);
            child.rotation = Quaternion.Euler(0, 0, rotateOffset);
            m_Tracker.Add(this, child, DrivenTransformProperties.AnchoredPosition | DrivenTransformProperties.Rotation);
        }
    }
}

public enum LayoutType
{
    Curved,
    Radial,
    Spiral,
    CustomCurve
}
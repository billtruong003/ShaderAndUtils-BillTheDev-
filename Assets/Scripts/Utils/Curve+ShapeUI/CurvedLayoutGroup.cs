using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A custom layout group that arranges child elements along a curve, in a circle, or in a spiral.
/// Offers advanced controls for rotation, sizing, and distribution.
/// </summary>
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

    [Header("General Settings")]
    [Tooltip("The type of layout to use.")]
    public LayoutType layoutType = LayoutType.Curved;

    [Tooltip("An overall position offset for the entire group.")]
    public Vector2 positionOffset = Vector2.zero;

    [Tooltip("An overall rotation offset applied to all children.")]
    public float rotateOffset = 0f;

    [Tooltip("If true, children will be rotated to align with their direction in the layout.")]
    public bool rotateTowards = false;

    [Tooltip("If true, inactive children will be ignored by the layout.")]
    public bool ignoreInactive = true;

    [Header("Child Sizing")]
    [Tooltip("If true, the layout group will control the size of its children.")]
    public bool controlChildSize = false;
    [Tooltip("The size to apply to children if controlChildSize is true.")]
    public Vector2 childSize = new Vector2(100, 100);

    [Header("Animation Curve Settings")]
    [Tooltip("The curve that defines the shape (for Curved) or modifies the distribution (for Radial/Spiral).")]
    public AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.5f, 1), new Keyframe(1, 0));
    [Tooltip("Scales the influence of the curve.")]
    public float curveScale = 1f;

    [Header("Curved Layout Settings")]
    [Tooltip("The total width of the curved layout path.")]
    public float width = 500f;
    [Tooltip("The maximum height (amplitude) of the curve.")]
    public float height = 100f;
    [Tooltip("The starting point on the curve (0 = start, 1 = end).")]
    [Range(0, 1)] public float curveStart = 0f;
    [Tooltip("The ending point on the curve (0 = start, 1 = end).")]
    [Range(0, 1)] public float curveEnd = 1f;
    [Tooltip("Flips the layout axis from horizontal (X-based) to vertical (Y-based).")]
    public bool horizontal = true;

    [Header("Radial Layout Settings")]
    [Tooltip("The starting angle of the circle in degrees.")]
    public float startAngle = 0f;
    [Tooltip("The ending angle of the circle in degrees.")]
    public float endAngle = 360f;
    [Tooltip("The radius of the circle.")]
    public float radius = 200f;
    [Tooltip("If true, the last child will be placed exactly at EndAngle. If false, spacing is divided among all children.")]
    public bool fitToRange = true;

    [Header("Spiral Layout Settings")]
    [Tooltip("The starting angle for the spiral.")]
    public float spiralStartAngle = 0f;
    [Tooltip("The angle increment for each subsequent child in the spiral.")]
    public float spiralAngleIncrement = 30f;
    [Tooltip("The distance increment for each subsequent child in the spiral.")]
    public float spiralDistanceIncrement = 10f;

    protected override void OnEnable()
    {
        base.OnEnable();
        UpdateLayout();
    }

    public override void CalculateLayoutInputHorizontal()
    {
        // This layout group controls positions, not necessarily layout sizes.
        // Let's set a minimal size, but the user should size the RectTransform manually for best results.
        base.CalculateLayoutInputHorizontal();
        float minWidth = 0;
        if (layoutType == LayoutType.Curved) minWidth = width;
        else if (layoutType == LayoutType.Radial) minWidth = radius * 2;
        // Spiral size is dynamic, hard to predict.

        SetLayoutInputForAxis(minWidth, minWidth, -1, 0);
    }

    public override void CalculateLayoutInputVertical()
    {
        float minHeight = 0;
        if (layoutType == LayoutType.Curved) minHeight = height * 2; // *2 to account for negative curve values
        else if (layoutType == LayoutType.Radial) minHeight = radius * 2;

        SetLayoutInputForAxis(minHeight, minHeight, -1, 1);
    }

    public override void SetLayoutHorizontal()
    {
        ArrangeElements();
    }

    public override void SetLayoutVertical()
    {
        // SetLayoutHorizontal handles both axes.
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        if (curveEnd < curveStart) curveEnd = curveStart;
        UpdateLayout();
    }
#endif

    public void UpdateLayout()
    {
        if (!IsActive()) return;
        ArrangeElements();
    }

    private void ArrangeElements()
    {
        m_Tracker.Clear();
        if (transform.childCount == 0)
            return;

        var childRects = new List<RectTransform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = (RectTransform)transform.GetChild(i);
            if (child != null && (!ignoreInactive || child.gameObject.activeInHierarchy))
            {
                childRects.Add(child);
            }
        }

        int childCount = childRects.Count;
        if (childCount == 0) return;

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
        }
    }

    private void SetCurvedLayout(List<RectTransform> children, int count)
    {
        for (int i = 0; i < count; i++)
        {
            RectTransform child = children[i];
            float normalizedIndex = (count > 1) ? (float)i / (count - 1) : 0.5f;

            // Apply curve range
            float curveRange = curveEnd - curveStart;
            float t = curveStart + (normalizedIndex * curveRange);

            float x = (-width / 2f) + (t * width);
            float y = curve.Evaluate(t) * height * curveScale;

            Vector2 position = horizontal ? new Vector2(x, y) : new Vector2(y, x);
            child.anchoredPosition = position + positionOffset;

            Quaternion rotation = Quaternion.Euler(0, 0, rotateOffset);
            if (rotateTowards)
            {
                // Approximate the derivative to find the tangent angle
                float tangentAngle = GetCurveTangentAngle(t, 0.01f);
                rotation = Quaternion.Euler(0, 0, tangentAngle + rotateOffset);
            }
            child.rotation = rotation;

            ApplyDrivenProperties(child);
        }
    }

    private float GetCurveTangentAngle(float t, float delta)
    {
        // Clamp t to avoid going out of bounds of the curve
        float t1 = Mathf.Clamp01(t - delta);
        float t2 = Mathf.Clamp01(t + delta);

        // Get points on the curve
        float x1 = (-width / 2f) + (t1 * width);
        float y1 = curve.Evaluate(t1) * height * curveScale;

        float x2 = (-width / 2f) + (t2 * width);
        float y2 = curve.Evaluate(t2) * height * curveScale;

        Vector2 dir = horizontal ? new Vector2(x2 - x1, y2 - y1) : new Vector2(y2 - y1, x2 - x1);
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    private void SetRadialLayout(List<RectTransform> children, int count)
    {
        if (count == 0) return;

        float totalAngle = endAngle - startAngle;
        // If fitToRange and count > 1, we divide by count-1 to make the last element land on endAngle.
        // Otherwise, we divide by count to distribute them within the range.
        float angleStep = totalAngle / (fitToRange && count > 1 ? (count - 1) : count);

        for (int i = 0; i < count; i++)
        {
            RectTransform child = children[i];
            float t = (count > 1) ? (float)i / (count - 1) : 0.5f;
            float curveValue = curve.Evaluate(t) * curveScale;

            float angle = startAngle + i * angleStep;
            float radAngle = angle * Mathf.Deg2Rad;

            float currentRadius = radius * curveValue;

            float x = Mathf.Cos(radAngle) * currentRadius;
            float y = Mathf.Sin(radAngle) * currentRadius;

            child.anchoredPosition = new Vector2(x, y) + positionOffset;

            Quaternion rotation = Quaternion.Euler(0, 0, rotateOffset);
            if (rotateTowards)
            {
                // Point away from the center
                rotation = Quaternion.Euler(0, 0, angle + rotateOffset - 90f);
            }
            child.rotation = rotation;

            ApplyDrivenProperties(child);
        }
    }

    private void SetSpiralLayout(List<RectTransform> children, int count)
    {
        float currentAngle = spiralStartAngle;
        float currentDistance = 0f;

        for (int i = 0; i < count; i++)
        {
            RectTransform child = children[i];
            float t = (count > 1) ? (float)i / (count - 1) : 0.5f;
            float curveValue = curve.Evaluate(t) * curveScale;

            // First child (i=0) is placed at the center, then distance increases.
            if (i > 0)
            {
                currentDistance += spiralDistanceIncrement * curveValue;
                currentAngle += spiralAngleIncrement;
            }

            float radAngle = currentAngle * Mathf.Deg2Rad;
            float x = Mathf.Cos(radAngle) * currentDistance;
            float y = Mathf.Sin(radAngle) * currentDistance;

            child.anchoredPosition = new Vector2(x, y) + positionOffset;

            Quaternion rotation = Quaternion.Euler(0, 0, rotateOffset);
            if (rotateTowards)
            {
                // Point away from the center
                rotation = Quaternion.Euler(0, 0, currentAngle + rotateOffset - 90f);
            }
            child.rotation = rotation;

            ApplyDrivenProperties(child);
        }
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
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;

[DisallowMultipleComponent]
public class SequentialActivator : MonoBehaviour
{
    [BoxGroup("Cài đặt Timing", centerLabel: true)]
    [Tooltip("Đường cong điều khiển thời gian chờ. Trục X (0->1) là tiến trình, Trục Y (0->1) là hệ số tốc độ (1=nhanh, 0=chậm).")]
    [SerializeField]
    private AnimationCurve activationTimingCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [BoxGroup("Cài đặt Timing")]
    [Tooltip("Thời gian chờ ngắn nhất giữa các lần kích hoạt (khi đường cong ở giá trị 1).")]
    [SerializeField]
    private float fastestInterval = 0.1f;

    [BoxGroup("Cài đặt Timing")]
    [Tooltip("Thời gian chờ dài nhất giữa các lần kích hoạt (khi đường cong ở giá trị 0).")]
    [SerializeField]
    private float slowestInterval = 1.0f;

    [ReadOnly]
    [BoxGroup("Trạng thái", centerLabel: true)]
    [Tooltip("Danh sách các đối tượng con cấp 1 sẽ được kích hoạt tuần tự.")]
    [SerializeField]
    private List<GameObject> childObjectsToActivate = new List<GameObject>();

    private Coroutine activationCoroutine;

    private void OnValidate()
    {
        PopulateImmediateChildrenList();
    }

    private void Awake()
    {
        PopulateImmediateChildrenList();
        ResetChildrenToInactiveState();
    }

    [Button("Bắt đầu Kích hoạt Tuần tự", ButtonSizes.Large)]
    [GUIColor(0.2f, 0.8f, 0.2f)]
    [PropertyOrder(-2)]
    public void StartActivationSequence()
    {
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
        }

        PopulateImmediateChildrenList();
        ResetChildrenToInactiveState();

        activationCoroutine = StartCoroutine(ActivateChildrenSequentially());
    }

    [Button("Dừng và Reset")]
    [GUIColor(0.9f, 0.3f, 0.3f)]
    [PropertyOrder(-1)]
    public void StopAndReset()
    {
        if (activationCoroutine != null)
        {
            StopCoroutine(activationCoroutine);
            activationCoroutine = null;
        }
        ResetChildrenToInactiveState();
    }

    [Button("Cập nhật lại Danh sách Con")]
    [PropertyOrder(1)]
    private void PopulateImmediateChildrenList()
    {
        childObjectsToActivate.Clear();
        foreach (Transform child in transform)
        {
            childObjectsToActivate.Add(child.gameObject);
        }
    }

    private void ResetChildrenToInactiveState()
    {
        foreach (var childObject in childObjectsToActivate)
        {
            if (childObject != null)
            {
                childObject.SetActive(false);
            }
        }
    }

    private IEnumerator ActivateChildrenSequentially()
    {
        if (childObjectsToActivate.Count == 0)
        {
            yield break;
        }

        for (int i = 0; i < childObjectsToActivate.Count; i++)
        {
            GameObject currentChild = childObjectsToActivate[i];
            if (currentChild != null)
            {
                currentChild.SetActive(true);
            }

            if (i < childObjectsToActivate.Count - 1)
            {
                float progress = (float)i / (childObjectsToActivate.Count - 1);
                float curveValue = activationTimingCurve.Evaluate(progress);
                float waitTime = Mathf.Lerp(slowestInterval, fastestInterval, curveValue);
                yield return new WaitForSeconds(waitTime);
            }
        }

        activationCoroutine = null;
    }
}
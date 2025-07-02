// File: Assets/MyIndieGame/Scripts/UI/FloatingText.cs (Phiên bản 3D)

using UnityEngine;
using TMPro; // Vẫn cần thư viện này
using System;

// Yêu cầu component TextMeshPro 3D
[RequireComponent(typeof(TextMeshPro))]
public class FloatingText : MonoBehaviour
{
    [SerializeField] private float floatDistance = 2f;
    [SerializeField] private float fadeDuration = 1f;

    private TextMeshPro textComponent;
    private Action<FloatingText> onCompleteCallback;

    private void Awake()
    {
        // Lấy component TextMeshPro 3D
        textComponent = GetComponent<TextMeshPro>();
    }

    public void Show(string text, Color color, Action<FloatingText> onComplete)
    {
        textComponent.text = text;
        textComponent.color = color;
        onCompleteCallback = onComplete;

        // Luôn hướng về phía camera khi được kích hoạt
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        Animate();
    }

    private void Animate()
    {
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * floatDistance;

        // Di chuyển lên (dùng local space)
        LeanTween.moveLocal(gameObject, endPos, fadeDuration).setEaseOutQuad();

        // Mờ dần (dùng alpha của vertex color)
        LeanTween.value(gameObject, 1f, 0f, fadeDuration)
                 .setEaseInQuad()
                 .setOnUpdate((float alpha) =>
                 {
                     textComponent.alpha = alpha;
                 })
                 .setOnComplete(() =>
                 {
                     // Khi animation kết thúc, gọi callback để trả về pool
                     onCompleteCallback?.Invoke(this);
                 });
    }
}
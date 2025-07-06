using UnityEngine;
using TMPro;
using System;

[RequireComponent(typeof(TextMeshPro))]
public class FloatingText : MonoBehaviour
{
    [SerializeField] private float floatDistance = 2f;
    [SerializeField] private float fadeDuration = 1f;

    private TextMeshPro textComponent;
    private Action<FloatingText> onCompleteCallback;

    private void Awake()
    {
        textComponent = GetComponent<TextMeshPro>();
    }

    public void Show(string text, Color color, Action<FloatingText> onComplete)
    {
        textComponent.text = text;
        textComponent.color = color;
        onCompleteCallback = onComplete;
        Animate();
    }

    private void Animate()
    {
        Vector3 startPos = transform.localPosition;
        Vector3 endPos = startPos + Vector3.up * floatDistance;

        LeanTween.moveLocal(gameObject, endPos, fadeDuration).setEaseOutQuad();
        LeanTween.value(gameObject, 1f, 0f, fadeDuration)
                 .setEaseInQuad()
                 .setOnUpdate((float alpha) => textComponent.alpha = alpha)
                 .setOnComplete(() => onCompleteCallback?.Invoke(this));
    }
}
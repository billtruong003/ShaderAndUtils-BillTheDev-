using UnityEngine;
using TMPro;

public class FloatingTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshPro textMeshPro;
    [SerializeField] private float moveSpeed = 1f;
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float lifeTime = 2f;
    [Header("Text Settings")]
    [SerializeField] private Color NormalColor = Color.white;
    [SerializeField] private Color CriticalColor = Color.red;

    private void Awake()
    {
        if (textMeshPro == null)
        {
            textMeshPro = GetComponent<TextMeshPro>();
        }
    }

    public void ShowFloatingText(Vector3 position, string text, bool isCritical = false)
    {
        transform.position = position;
        textMeshPro.text = text;
        textMeshPro.color = isCritical ? Color.red : Color.white;
        textMeshPro.alpha = 1f;

        // Di chuyển lên và fade out
        LeanTween.moveLocalY(gameObject, transform.localPosition.y + 1f, lifeTime).setEaseInOutQuad();
        LeanTween.alphaText(textMeshPro.rectTransform, 0f, fadeDuration).setDelay(lifeTime - fadeDuration).setOnComplete(KillText);

        // Tự hủy sau lifeTime
        Invoke("KillText", lifeTime);
    }

    private void KillText()
    {
        LeanTween.cancel(gameObject);
        Destroy(gameObject);
    }
}
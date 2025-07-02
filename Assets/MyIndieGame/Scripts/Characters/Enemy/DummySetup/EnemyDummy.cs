// File: Assets/MyIndieGame/Scripts/Characters/EnemyDummy.cs (Phiên bản kiểm soát hoàn toàn)

using UnityEngine;
using TMPro;

[RequireComponent(typeof(Health))]
public class EnemyDummy : MonoBehaviour
{
    [Header("Core Component References")]
    [Tooltip("Kéo Renderer chính của thân thể địch vào đây. Renderer này sẽ nháy màu khi bị đánh.")]
    [SerializeField] private Renderer mainBodyRenderer;
    [Tooltip("Kéo Collider chính của địch vào đây. Collider này sẽ bị tắt khi chết.")]
    [SerializeField] private Collider mainCollider;

    [Header("3D Health Display")]
    [Tooltip("Kéo Renderer của thanh máu (Quad) vào đây.")]
    [SerializeField] private Renderer healthBarRenderer;
    [Tooltip("Kéo đối tượng TextMeshPro 3D hiển thị máu tĩnh vào đây.")]
    [SerializeField] private TextMeshPro staticHealthText;

    [Header("Damage Text")]
    [SerializeField] private Color damageTextColor = Color.yellow;

    [Header("Feedback Settings")]
    [SerializeField] private Color hitColor = Color.white;
    [SerializeField] private float hitFlashDuration = 0.2f;
    [SerializeField] private float punchScaleAmount = 1.1f; // Giảm nhẹ để trông tự nhiên hơn
    [SerializeField] private float punchDuration = 0.3f;
    [SerializeField] private float deathShrinkDuration = 0.5f;

    // --- Các biến nội bộ, không cần gán từ Inspector ---
    private Health health;
    private MaterialPropertyBlock _propBlock;
    private Color _originalColor;
    private Vector3 _originalScale;
    private int _colorPropertyID;
    private Material healthBarMaterial;
    private int _progressPropertyID;

    private void Awake()
    {
        health = GetComponent<Health>();

        // Thiết lập cho hiệu ứng nháy màu trên thân địch
        _propBlock = new MaterialPropertyBlock();
        _colorPropertyID = Shader.PropertyToID("_BaseColor"); // Giả sử shader của địch dùng _BaseColor
        if (mainBodyRenderer != null)
        {
            // Lấy màu gốc từ material của renderer được chỉ định
            _originalColor = mainBodyRenderer.sharedMaterial.GetColor(_colorPropertyID);
        }

        // Thiết lập cho thanh máu
        if (healthBarRenderer != null)
        {
            healthBarMaterial = healthBarRenderer.material;
            _progressPropertyID = Shader.PropertyToID("_Progress");
        }

        _originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        health.OnHealthChanged += HandleHealthChanged;
        health.OnTakeDamage += HandleTakeDamage;
        health.OnDie += HandleDeath;
    }

    private void OnDisable()
    {
        health.OnHealthChanged -= HandleHealthChanged;
        health.OnTakeDamage -= HandleTakeDamage;
        health.OnDie -= HandleDeath;
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        // Cập nhật thanh máu shader
        if (healthBarMaterial != null)
        {
            healthBarMaterial.SetFloat(_progressPropertyID, currentHealth / maxHealth);
        }
        // Cập nhật text máu 3D
        if (staticHealthText != null)
        {
            staticHealthText.text = $"{Mathf.Ceil(currentHealth)} / {Mathf.Ceil(maxHealth)}";
        }
    }

    private void HandleTakeDamage(float damageAmount, Vector3 hitPoint)
    {
        // Hiển thị số sát thương bay lên tại điểm va chạm
        FloatingTextPool.Instance.ShowText(Mathf.Ceil(damageAmount).ToString(), hitPoint, damageTextColor);

        // Hiệu ứng giật nảy (punch) cho toàn bộ đối tượng
        LeanTween.cancel(gameObject, false); // Không hủy tween của con
        transform.localScale = _originalScale;
        LeanTween.scale(gameObject, _originalScale * punchScaleAmount, punchDuration).setEasePunch();

        // Hiệu ứng nháy màu CHỈ trên renderer thân thể được chỉ định
        if (mainBodyRenderer != null)
        {
            mainBodyRenderer.GetPropertyBlock(_propBlock); // Lấy block hiện tại
            _propBlock.SetColor(_colorPropertyID, _originalColor); // Đặt lại màu gốc
            LeanTween.value(gameObject, hitColor, _originalColor, hitFlashDuration)
                     .setOnUpdate((Color val) =>
                     {
                         _propBlock.SetColor(_colorPropertyID, val);
                         mainBodyRenderer.SetPropertyBlock(_propBlock);
                     });
        }
    }

    private void HandleDeath()
    {
        // Ẩn các thành phần 3D
        if (healthBarRenderer != null) healthBarRenderer.gameObject.SetActive(false);
        if (staticHealthText != null) staticHealthText.gameObject.SetActive(false);

        // Tắt collider được chỉ định
        if (mainCollider != null) mainCollider.enabled = false;

        // Hiệu ứng chết
        LeanTween.cancel(gameObject);
        LeanTween.scale(gameObject, Vector3.zero, deathShrinkDuration)
                 .setEaseInBack()
                 .setOnComplete(() => Destroy(gameObject));
    }
}
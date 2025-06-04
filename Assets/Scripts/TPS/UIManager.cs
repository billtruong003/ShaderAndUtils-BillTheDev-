using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private ThirdPersonCharacterController player;
    [SerializeField] private Image healthBar;
    [SerializeField] private Image rageBar;
    [SerializeField] private TextMeshProUGUI characterNameText;
    [SerializeField] private FloatingTextUI floatingTextPrefab;

    void Start()
    {
        if (player == null) player = FindObjectOfType<ThirdPersonCharacterController>();
        if (healthBar != null) healthBar.material.SetFloat("_Progress", 1f);
        if (rageBar != null) rageBar.material.SetFloat("_Progress", 0f);
        if (characterNameText != null) characterNameText.text = player.GetCharacterName();
    }

    void Update()
    {
        if (player != null)
        {
            UpdateHealthBar();
            UpdateRageBar();
        }
    }

    void UpdateHealthBar()
    {
        float progress = player.GetCurrentHP() / player.GetMaxHP();
        healthBar.material.SetFloat("_Progress", Mathf.Clamp01(progress));
    }

    void UpdateRageBar()
    {
        float progress = player.GetCurrentRage() / player.GetMaxRage();
        rageBar.material.SetFloat("_Progress", Mathf.Clamp01(progress));
    }

    public void ShowDamageText(Vector3 position, float damage, bool isCritical)
    {
        if (floatingTextPrefab != null)
        {
            FloatingTextUI textInstance = Instantiate(floatingTextPrefab, position, Quaternion.identity, transform);
            textInstance.ShowFloatingText(position, damage.ToString(), isCritical);
        }
    }

    public void UpdateCharacterName(string name)
    {
        if (characterNameText != null) characterNameText.text = name;
    }
}
using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "Luminaria/Items/Consumable Item")]
public class ConsumableItemData : ItemDefinition, IUsable
{
    [Header("Consumable Effects")]
    public float healthToRestore;
    public float manaToRestore;
    public float staminaToRestore;
    public bool Use(GameObject user)
    {
        if (user.TryGetComponent<StatController>(out var stats))
        {
            if (healthToRestore > 0) stats.RestoreHealth(healthToRestore);
            if (manaToRestore > 0) stats.RestoreMana(manaToRestore);
            if (staminaToRestore > 0) stats.RestoreStamina(staminaToRestore);

            Debug.Log($"Used {this.Name}.");
            return true;
        }
        return false;
    }
}
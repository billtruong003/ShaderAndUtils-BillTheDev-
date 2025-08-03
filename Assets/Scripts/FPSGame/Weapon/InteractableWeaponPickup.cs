using UnityEngine;
using Sirenix.OdinInspector;

namespace FPS
{
    [RequireComponent(typeof(Collider))]
    public class InteractableWeaponPickup : MonoBehaviour, IInteractable
    {
        [SerializeField, Required("You must assign a WeaponData to this pickup.")]
        [AssetsOnly]
        private WeaponData weaponToGrant;

        public string InteractionPrompt => $"Pick up {weaponToGrant.weaponName}";

        public void Interact(PlayerInteraction interactor)
        {
            var inventory = interactor.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.EquipWeapon(weaponToGrant);
                Destroy(gameObject);
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

namespace FPS
{
    public class PlayerInventory : MonoBehaviour
    {
        [Title("Configuration")]
        [SerializeField, Required] private Transform weaponHolder;
        [SerializeField, Required, AssetsOnly] private WeaponData unarmedData;

        [Title("Runtime State")]
        [ShowInInspector, ReadOnly]
        private readonly Dictionary<WeaponSlot, Weapon> equippedWeapons = new Dictionary<WeaponSlot, Weapon>();

        [ShowInInspector, ReadOnly]
        private Weapon currentWeapon;

        private PlayerAnimationController animationController;

        private void Awake()
        {
            animationController = GetComponent<PlayerAnimationController>();
        }

        private void Start()
        {
            EquipWeapon(unarmedData);
        }

        public Weapon GetCurrentWeapon() => currentWeapon;

        public void EquipWeapon(WeaponData weaponData)
        {
            if (equippedWeapons.ContainsKey(weaponData.slot))
            {
                Destroy(equippedWeapons[weaponData.slot].gameObject);
                equippedWeapons.Remove(weaponData.slot);
            }

            GameObject weaponObject = Instantiate(weaponData.weaponPrefab, weaponHolder);
            Weapon newWeapon = weaponObject.GetComponent<Weapon>();
            newWeapon.Initialize(weaponData);

            equippedWeapons[weaponData.slot] = newWeapon;
            SwitchWeapon(weaponData.slot);
        }

        public void SwitchWeapon(WeaponSlot slot)
        {
            if (!equippedWeapons.ContainsKey(slot)) return;
            if (currentWeapon != null && currentWeapon.Data.slot == slot) return;

            if (currentWeapon != null)
            {
                currentWeapon.gameObject.SetActive(false);
            }

            currentWeapon = equippedWeapons[slot];
            currentWeapon.gameObject.SetActive(true);
            animationController.UpdateWeaponAnimation(currentWeapon);
        }
    }
}
using UnityEngine;
using Sirenix.OdinInspector;

namespace FPS
{
    public class Weapon : MonoBehaviour
    {
        [field: SerializeField, ReadOnly]
        public WeaponData Data { get; private set; }

        private int currentAmmo;

        public void Initialize(WeaponData data)
        {
            Data = data;
            currentAmmo = data.magazineSize;
            gameObject.name = data.weaponName;
        }

        public void Attack()
        {
            // Logic tấn công chi tiết ở đây
            Debug.Log($"Attacking with {Data.weaponName}");
        }
    }
}
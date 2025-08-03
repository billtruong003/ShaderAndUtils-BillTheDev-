using UnityEngine;
using Sirenix.OdinInspector;

namespace FPS
{
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "FPS/Weapon Data")]
    public class WeaponData : ScriptableObject
    {
        [Title("WEAPON CONFIGURATION", bold: true)]
        [BoxGroup("G_INFO", ShowLabel = false)]
        [HorizontalGroup("G_INFO/SPLIT", 75)]
        [PreviewField(75, ObjectFieldAlignment.Left), HideLabel]
        public GameObject weaponPrefab;

        [HorizontalGroup("G_INFO/SPLIT")]
        [BoxGroup("G_INFO/SPLIT/RIGHT")]
        [Required("Weapon Name is missing.")]
        public string weaponName;

        [BoxGroup("G_INFO/SPLIT/RIGHT")]
        public WeaponSlot slot;

        [Title("Animation")]
        [InfoBox("ID này sẽ được dùng trong Animator để chọn bộ animation phù hợp. 0 thường là Unarmed.")]
        [Range(0, 10)]
        public int weaponAnimationId;

        [Title("Combat Stats")]
        [MinValue(0)]
        public float damage;

        [SuffixLabel("shots/sec", true), MinValue(0.1f)]
        public float fireRate;

        [MinValue(0)]
        public int magazineSize;
    }
}
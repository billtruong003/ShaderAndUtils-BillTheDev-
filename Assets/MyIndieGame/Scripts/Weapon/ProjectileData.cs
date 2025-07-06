// File: Assets/MyIndieGame/Scripts/Weapons/ProjectileData.cs (TẠO MỚI)
using UnityEngine;

[CreateAssetMenu(fileName = "New Projectile", menuName = "Luminaria/Data/Projectile Data")]
public class ProjectileData : ScriptableObject
{
    [Header("Core Properties")]
    [Tooltip("Prefab của viên đạn sẽ được bắn ra.")]
    public GameObject Prefab;

    [Tooltip("Tốc độ bay của viên đạn.")]
    public float speed = 50f;

    [Header("Effects")]
    [Tooltip("Hiệu ứng sẽ được tạo ra tại điểm va chạm.")]
    public GameObject impactEffectPrefab;

    [Tooltip("(Tùy chọn) Âm thanh được phát ra khi bắn.")]
    public AudioClip fireSound;

    [Tooltip("(Tùy chọn) Âm thanh được phát ra khi va chạm.")]
    public AudioClip impactSound;
}
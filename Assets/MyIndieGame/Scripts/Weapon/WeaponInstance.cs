using UnityEngine;
using System.Collections.Generic;

public class WeaponInstance : MonoBehaviour
{
    [Header("Attachment Points")]
    [Tooltip("Điểm nhân vật sẽ cầm vào. Dùng để căn chỉnh vũ khí.")]
    public Transform GripPoint;

    [Header("Combat Points")]
    [Tooltip("Các điểm dùng để cast hitbox cho vũ khí cận chiến.")]
    public List<Transform> MeleeCastPoints;
    [Tooltip("Vị trí đạn hoặc hiệu ứng sẽ được tạo ra.")]
    public Transform ProjectileSpawnPoint;
}
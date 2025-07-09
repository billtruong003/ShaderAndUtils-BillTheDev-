using UnityEngine;
using System.Collections.Generic;

public sealed class WeaponInstance : MonoBehaviour
{
    [Header("Attachment Points")]
    public Transform GripPoint;

    [Header("Combat Points")]
    public List<Transform> MeleeCastPoints;
    public Transform ProjectileSpawnPoint;
}
// File: Assets/MyIndieGame/Scripts/Weapons/ProjectileController.cs (Bản Nâng Cấp Hoàn Chỉnh)
using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class ProjectileController : MonoBehaviour
{
    private float damage;
    private float poiseDamage;
    private LayerMask hitLayer;
    private Rigidbody rb;
    private HashSet<Collider> hitTargets = new HashSet<Collider>();
    private GameObject owner; // THÊM MỚI: Biến để lưu trữ người đã bắn ra viên đạn

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        GetComponent<Collider>().isTrigger = true;
    }

    // SỬA ĐỔI: Thêm tham số 'GameObject owner' vào cuối
    public void Initialize(float dmg, float poise, float speed, LayerMask layer, Vector3 initialDirection, GameObject owner)
    {
        this.damage = dmg;
        this.poiseDamage = poise;
        this.hitLayer = layer;
        this.owner = owner; // THÊM MỚI: Lưu lại thông tin người bắn

        transform.forward = initialDirection;
        rb.linearVelocity = initialDirection * speed;

        Destroy(gameObject, 10f);
    }

    void OnTriggerEnter(Collider other)
    {
        // SỬA ĐỔI: Thêm điều kiện kiểm tra 'owner'
        // Dùng `transform.root` để đảm bảo an toàn khi va chạm vào các collider con của chủ sở hữu
        if (owner != null && other.transform.root == owner.transform.root)
        {
            return; // Bỏ qua va chạm với chính người bắn
        }

        if ((hitLayer.value & (1 << other.gameObject.layer)) == 0)
        {
            return;
        }

        if (hitTargets.Contains(other)) return;
        hitTargets.Add(other);

        ProcessHit(other.gameObject, other.ClosestPoint(transform.position));

        Destroy(gameObject);
    }

    private void ProcessHit(GameObject hitObject, Vector3 hitPoint)
    {
        if (hitObject.TryGetComponent<Health>(out var health))
        {
            health.TakeDamage(damage, hitPoint);
        }

        if (hitObject.TryGetComponent<PoiseController>(out var poise))
        {
            poise.TakePoiseDamage(poiseDamage);
        }
    }
}
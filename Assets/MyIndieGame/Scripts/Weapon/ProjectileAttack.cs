// File: Assets/MyIndieGame/Scripts/Weapons/Strategies/ProjectileAttack.cs
using UnityEngine;

public class ProjectileAttack : IWeaponAttackStrategy
{
    private WeaponController controller;
    private float elapsedTime;
    private bool hasFired;

    public void OnEquip(WeaponController controller)
    {
        this.controller = controller;
    }

    public void StartAttack(int comboIndex)
    {
        elapsedTime = 0f;
        hasFired = false;

        // <-- LOGIC MỚI: XOAY NGƯỜI KHI BẮN
        if (controller.Targeting?.CurrentTarget != null)
        {
            // Nếu có mục tiêu, xoay về phía mục tiêu
            controller.Locomotion.ForceLookAtTarget(controller.Targeting.CurrentTarget);
        }
        else
        {
            // Nếu không có mục tiêu, xoay về phía camera đang nhìn
            controller.Locomotion.ForceLookAtDirection(Camera.main.transform.forward);
        }

        controller.Animator.PlayAction(controller.CurrentAttackData.AttackID);
    }

    public void Tick(float deltaTime)
    {
        elapsedTime += deltaTime;
        AttackData data = controller.CurrentAttackData;

        if (!hasFired && elapsedTime >= data.hitboxStartTime)
        {
            FireProjectile();
            hasFired = true;
        }

        if (elapsedTime >= data.Duration)
        {
            controller.AnimationFinished();
        }
    }

    private void FireProjectile()
    {
        ProjectileData pData = controller.CurrentWeaponData.projectileData;

        if (pData == null || pData.Prefab == null || controller.CurrentWeaponInstance?.ProjectileSpawnPoint == null)
        {
            Debug.LogError($"Weapon '{controller.CurrentWeaponData.weaponName}' is missing ProjectileData, its Prefab, or a ProjectileSpawnPoint.", controller.gameObject);
            return;
        }

        Transform spawnPoint = controller.CurrentWeaponInstance.ProjectileSpawnPoint;
        GameObject projectileObject = Object.Instantiate(pData.Prefab, spawnPoint.position, spawnPoint.rotation);

        if (!projectileObject.TryGetComponent<ProjectileController>(out var projController))
        {
            Debug.LogError($"Projectile prefab '{pData.Prefab.name}' is missing a ProjectileController component.", pData.Prefab);
            Object.Destroy(projectileObject);
            return;
        }

        float weaponDamage = controller.CurrentWeaponData.baseDamage;
        float statBonus = controller.Stats.GetStatValue(StatType.PhysicalDamageBonus);
        float totalBaseDamage = weaponDamage * (1 + statBonus / 100f);
        float finalDamage = totalBaseDamage * controller.CurrentAttackData.damageMultiplier;

        projController.Initialize(finalDamage, controller.CurrentAttackData.poiseDamage, pData.speed, controller.Targeting.EnemyLayer, spawnPoint.forward, controller.gameObject);
    }

    public void StopAttack() { }

    public void OnDrawGizmos()
    {
        if (controller.CurrentWeaponInstance?.ProjectileSpawnPoint != null)
        {
            Gizmos.color = Color.cyan;
            Transform spawnPoint = controller.CurrentWeaponInstance.ProjectileSpawnPoint;
            Gizmos.DrawLine(spawnPoint.position, spawnPoint.position + spawnPoint.forward * 5f);
            Gizmos.DrawWireSphere(spawnPoint.position, 0.1f);
        }
    }
}
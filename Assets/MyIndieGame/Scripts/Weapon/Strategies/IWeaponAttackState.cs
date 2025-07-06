// File: Assets/MyIndieGame/Scripts/Weapons/Strategies/IWeaponAttackStrategy.cs
using UnityEngine;

public interface IWeaponAttackStrategy
{
    void OnEquip(WeaponController controller);
    void StartAttack(int comboIndex);
    void Tick(float deltaTime);
    void StopAttack();
    void OnDrawGizmos();
}
// TẠO FILE MỚI: Assets/MyIndieGame/Scripts/Characters/CharacterVisualController.cs
using UnityEngine;

[RequireComponent(typeof(EquipmentManager), typeof(CharacterSocketController))]
public class CharacterVisualController : MonoBehaviour
{
    private EquipmentManager equipmentManager;
    private CharacterSocketController socketController;

    private GameObject currentWeaponInstance;

    private void Awake()
    {
        equipmentManager = GetComponent<EquipmentManager>();
        socketController = GetComponent<CharacterSocketController>();
    }

    private void OnEnable()
    {
        equipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
        equipmentManager.OnWeaponStanceChanged += HandleWeaponStanceChanged;
    }

    private void OnDisable()
    {
        equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
        equipmentManager.OnWeaponStanceChanged -= HandleWeaponStanceChanged;
    }

    private void HandleEquipmentChanged(WeaponData newWeapon, WeaponData oldWeapon)
    {
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
        }

        bool isUnarmed = (newWeapon == equipmentManager.UnarmedWeaponData);
        if (isUnarmed || newWeapon?.WeaponModelPrefab == null) return;

        Transform initialSocket = socketController.GetSocket(newWeapon.SheathSocket);
        if (initialSocket == null) return;

        currentWeaponInstance = Instantiate(newWeapon.WeaponModelPrefab, initialSocket);
        currentWeaponInstance.transform.localPosition = Vector3.zero;
        currentWeaponInstance.transform.localRotation = Quaternion.identity;
    }

    private void HandleWeaponStanceChanged(bool isDrawn, WeaponData weaponData)
    {
        if (currentWeaponInstance == null || weaponData == null) return;

        CharacterSocketType targetSocketType = isDrawn ? weaponData.EquipSocket : weaponData.SheathSocket;
        Transform targetSocket = socketController.GetSocket(targetSocketType);

        if (targetSocket != null)
        {
            currentWeaponInstance.transform.SetParent(targetSocket, false);
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;
        }
    }
}
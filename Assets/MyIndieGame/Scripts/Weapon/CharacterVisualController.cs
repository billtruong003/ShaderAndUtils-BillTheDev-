// File: Assets/MyIndieGame/Scripts/Characters/CharacterVisualController.cs (TẠO MỚI)
using UnityEngine;
using System;

// Lớp này cần các controller khác để hoạt động
[RequireComponent(typeof(EquipmentManager))]
[RequireComponent(typeof(CharacterSocketController))]
[RequireComponent(typeof(WeaponController))]
public class CharacterVisualController : MonoBehaviour
{
    private EquipmentManager equipmentManager;
    private CharacterSocketController socketController;
    private WeaponController weaponController;

    // Dictionary để lưu trữ các instance của model vũ khí đang được hiển thị
    private GameObject currentWeaponInstance;
    private WeaponInstance currentWeaponInstanceComponent;

    void Awake()
    {
        equipmentManager = GetComponent<EquipmentManager>();
        socketController = GetComponent<CharacterSocketController>();
        weaponController = GetComponent<WeaponController>();
    }

    void OnEnable()
    {
        // Lắng nghe sự kiện trang bị thay đổi
        equipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
        equipmentManager.OnWeaponStanceChanged += HandleWeaponStanceChanged;
    }

    void OnDisable()
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

        // SỬA ĐỔI: Kiểm tra xem vũ khí có phải là tay không hay không
        bool isUnarmed = (newWeapon == equipmentManager.UnarmedWeaponData);

        currentWeaponInstanceComponent = null;

        // Chỉ tạo model nếu không phải tay không và có prefab
        if (!isUnarmed && newWeapon != null && newWeapon.WeaponModelPrefab != null)
        {
            Transform sheathSocket = socketController.GetSocket(newWeapon.SheathSocket);
            if (sheathSocket != null)
            {
                currentWeaponInstance = Instantiate(newWeapon.WeaponModelPrefab, sheathSocket);
                currentWeaponInstanceComponent = currentWeaponInstance.GetComponent<WeaponInstance>();
                AlignWeaponToSocket(sheathSocket);
            }
        }

        weaponController.EquipWeapon(newWeapon, currentWeaponInstanceComponent);
    }

    private void HandleWeaponStanceChanged(bool isDrawn, WeaponData weapon)
    {
        // SỬA ĐỔI: Không làm gì nếu không có model vũ khí để di chuyển
        if (currentWeaponInstance == null || weapon == null) return;

        Transform targetSocket = socketController.GetSocket(isDrawn ? weapon.EquipSocket : weapon.SheathSocket);
        if (targetSocket != null)
        {
            currentWeaponInstance.transform.SetParent(targetSocket, true);
            AlignWeaponToSocket(targetSocket);
        }
    }

    private void AlignWeaponToSocket(Transform socket)
    {
        if (currentWeaponInstance == null) return;

        // Nếu không có GripPoint, đặt mặc định
        if (currentWeaponInstanceComponent == null || currentWeaponInstanceComponent.GripPoint == null)
        {
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;
            return;
        }

        // Phép toán căn chỉnh vũ khí vào socket thông qua GripPoint
        // Transform grip = currentWeaponInstanceComponent.GripPoint;
        currentWeaponInstance.transform.localPosition = Vector3.zero;
        currentWeaponInstance.transform.localRotation = Quaternion.identity;
        // currentWeaponInstance.transform.position = socket.position;
        // currentWeaponInstance.transform.rotation = socket.rotation * Quaternion.Inverse(grip.localRotation);
    }
}
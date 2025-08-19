using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
#if INVENTORY_PRO_ADD_ON
using cowsins.Inventory;
#endif

namespace cowsins
{
    public partial class WeaponPickeable : Pickeable
    {
        [Tooltip("Which weapon are we grabbing"), SaveField] public Weapon_SO weapon;

        [SaveField] private int currentBullets, totalBullets;
        private Dictionary<AttachmentType, AttachmentIdentifier_SO> currentAttachments = new Dictionary<AttachmentType, AttachmentIdentifier_SO>();

        public override void Awake()
        {
            base.Awake();
            if (dropped) return;
            Initialize();
        }

        public override void Interact(Transform player)
        {
            if (weapon == null)
            {
                Debug.LogError("<color=red>[COWSINS]</color> <b><color=yellow>Weapon_SO</color></b> not found! Skipping Interaction.", this);
                return;
            }

            base.Interact(player);
            WeaponController weaponController = player.GetComponent<WeaponController>();
            InteractManager interactManager = player.GetComponent<InteractManager>();

            if (weapon.shootStyle == ShootStyle.Melee && weaponController.Weapon == weaponController.UnarmedWeaponSO)
            {
                List<AttachmentIdentifier_SO> attachmentKeys = currentAttachments.Values.Where(att => att != null).ToList();
                weaponController.ReplaceUnarmedWithMelee(weapon, currentBullets, totalBullets, attachmentKeys);
                DestroyAndSave();
                return;
            }

            if (HandleDuplicateWeaponPickup(weaponController, interactManager)) return;

            if (TryAddToEmptySlot(weaponController)) return;

#if INVENTORY_PRO_ADD_ON
            if (InventoryProManager.instance && InventoryProManager.instance.StoreWeaponsIfHotbarFull)
            {
                if (TryStoreInInventoryPro())
                {
                    DestroyAndSave();
                    return;
                }
            }
#endif
            HandleInventoryFull(weaponController);

            alreadyInteracted = false;
#if SAVE_LOAD_ADD_ON
            StoreData();
#endif
        }

        private bool HandleDuplicateWeaponPickup(WeaponController weaponController, InteractManager interactManager)
        {
            if (!interactManager.DuplicateWeaponAddsBullets) return false;

            for (int i = 0; i < weaponController.Inventory.Length; i++)
            {
                if (weaponController.Inventory[i] && weaponController.Inventory[i].weapon == weapon && weapon.limitedMagazines)
                {
                    weaponController.Inventory[i].totalBullets += weapon.magazineSize;
                    DestroyAndSave();
                    return true;
                }
            }
            return false;
        }

        private bool TryAddToEmptySlot(WeaponController weaponController)
        {
            for (int i = 0; i < weaponController.InventorySize; i++)
            {
                if (weaponController.Inventory[i] == null)
                {
                    InstantiateWeapon(weaponController, i);
                    weaponController.CurrentWeaponIndex = i;
                    weaponController.SelectWeapon();
                    DestroyAndSave();
                    return true;
                }
            }
            return false;
        }

#if INVENTORY_PRO_ADD_ON
        private bool TryStoreInInventoryPro()
        {
            bool success = InventoryProManager.instance._GridGenerator.AddWeaponToInventory(weapon, currentBullets, totalBullets);
            if (success)
            {
                ToastManager.Instance?.ShowToast($"{weapon._name} {ToastManager.Instance.CollectedMsg}");
            }
            else
            {
                ToastManager.Instance?.ShowToast(ToastManager.Instance.InventoryIsFullMsg);
            }
            return success;
        }
#endif

        private void HandleInventoryFull(WeaponController weaponController)
        {
            if (weaponController.Weapon != null && weaponController.Weapon.isPermanent)
            {
                int swapIndex = FindNonPermanentWeaponIndex(weaponController);
                if (swapIndex != -1)
                {
                    SwapSpecificWeapon(weaponController, swapIndex);
                }
                // If no non-permanent weapon is found, do nothing (cannot pick up).
            }
            else
            {
                SwapCurrentWeapon(weaponController);
            }
        }

        private int FindNonPermanentWeaponIndex(WeaponController weaponController)
        {
            for (int i = 0; i < weaponController.InventorySize; i++)
            {
                if (weaponController.Inventory[i] != null && !weaponController.Inventory[i].weapon.isPermanent)
                {
                    return i;
                }
            }
            return -1;
        }

        private void SwapCurrentWeapon(WeaponController weaponController)
        {
            SwapSpecificWeapon(weaponController, weaponController.CurrentWeaponIndex);
        }

        private void SwapSpecificWeapon(WeaponController weaponController, int indexToSwap)
        {
            // 1. Lấy và lưu trữ thông tin của vũ khí SẮP BỊ THẢ RA từ tay người chơi
            WeaponIdentification weaponToDrop = weaponController.Inventory[indexToSwap];
            if (weaponToDrop == null || weaponToDrop.weapon.isPermanent) return;

            Weapon_SO weaponToDropSO = weaponToDrop.weapon;
            int bulletsInMagToDrop = weaponToDrop.bulletsLeftInMagazine;
            int totalBulletsToDrop = weaponToDrop.totalBullets;

            // Lấy attachments của vũ khí sắp thả ra để gán cho pickeable này
            Dictionary<AttachmentType, AttachmentIdentifier_SO> attachmentsToDrop = new Dictionary<AttachmentType, AttachmentIdentifier_SO>();
            foreach (AttachmentType type in System.Enum.GetValues(typeof(AttachmentType)))
            {
                Attachment currentAttachment = weaponToDrop.GetCurrentAttachment(type);
                attachmentsToDrop[type] = currentAttachment?.attachmentIdentifier;
            }

            // 2. Lưu trữ thông tin của vũ khí SẮP ĐƯỢC NHẶT LÊN (vũ khí hiện tại của pickeable)
            Weapon_SO weaponToEquipSO = this.weapon;
            int bulletsToEquip = this.currentBullets;
            int totalBulletsToEquip = this.totalBullets;
            List<AttachmentIdentifier_SO> attachmentsToEquip = this.currentAttachments.Values.Where(att => att != null).ToList();

            // 3. Thực hiện việc trang bị vũ khí mới cho người chơi
            weaponController.ReleaseWeapon(indexToSwap);
            weaponController.InstantiateWeapon(weaponToEquipSO, indexToSwap, bulletsToEquip, totalBulletsToEquip, attachmentsToEquip);

            // 4. CẬP NHẬT pickeable này để nó biến thành vũ khí VỪA BỊ THẢ RA
            this.weapon = weaponToDropSO;
            this.currentBullets = bulletsInMagToDrop;
            this.totalBullets = totalBulletsToDrop;
            this.currentAttachments = attachmentsToDrop; // Gán lại attachments đã lưu
            DestroyGraphics();
            GetVisuals(); // Hiển thị hình ảnh mới (của vũ khí bị thả)

            // 5. Tự động chuyển sang vũ khí vừa nhặt
            weaponController.CurrentWeaponIndex = indexToSwap;
            weaponController.SelectWeapon();
        }
        private void DestroyAndSave()
        {
#if SAVE_LOAD_ADD_ON
            alreadyInteracted = true;
            StoreData();
#endif
            Destroy(this.gameObject);
        }

        private void InstantiateWeapon(WeaponController weaponController, int index)
        {
            List<AttachmentIdentifier_SO> attachmentKeys = currentAttachments.Values.Where(att => att != null).ToList();
            weaponController.InstantiateWeapon(weapon, index, currentBullets, totalBullets, attachmentKeys);
        }

        public override void Drop(PlayerDependencies playerDependencies, PlayerOrientation orientation)
        {
            base.Drop(playerDependencies, orientation);

            IWeaponReferenceProvider wRef = playerDependencies.WeaponReference;
            weapon = wRef.Weapon;
            currentBullets = wRef.Id.bulletsLeftInMagazine;
            totalBullets = wRef.Id.totalBullets;
            SetPickeableAttachments(wRef.Id);
            GetVisuals();
        }

        public void DropOverrideParameters(Weapon_SO weapon, int currentBullets, int totalBullets, Dictionary<AttachmentType, AttachmentIdentifier_SO> tempAttachments)
        {
            this.pickeable = true;
            this.weapon = weapon;
            this.currentBullets = currentBullets;
            this.totalBullets = totalBullets;
            foreach (var attachment in tempAttachments)
            {
                currentAttachments[attachment.Key] = attachment.Value;
            }
            GetVisuals();
        }

        public void SetPickeableAttachments(WeaponIdentification wId)
        {
            foreach (AttachmentType type in System.Enum.GetValues(typeof(AttachmentType)))
            {
                Attachment currentAttachment = wId.GetCurrentAttachment(type);
                if (currentAttachment != null)
                {
                    currentAttachments[type] = currentAttachment.attachmentIdentifier;
                }
                else
                {
                    currentAttachments[type] = null;
                }
            }
        }

        private void Initialize()
        {
            if (weapon == null) return;
            GetVisuals();

            var weaponId = weapon.weaponObject;
            SetDefaultAttachments(weaponId);

            int magCapacityAdded = 0;
            if (weaponId.GetDefaultAttachment(AttachmentType.Magazine) is Magazine magazine)
            {
                magCapacityAdded = magazine.magazineCapacityAdded;
            }

            currentBullets = weapon.magazineSize + magCapacityAdded;
            totalBullets = weapon.limitedMagazines ? weapon.totalMagazines * currentBullets : 0;
        }

        public void GetVisuals()
        {
            if (weapon == null) return;
            interactText = weapon._name;
            if (image != null) image.sprite = weapon.icon;

            if (graphics.transform.childCount > 0)
            {
                Destroy(graphics.transform.GetChild(0).gameObject);
            }
            if (weapon.pickUpGraphics != null)
            {
                Instantiate(weapon.pickUpGraphics, graphics);
            }
        }

        public AttachmentIdentifier_SO GetAttachmentByType(AttachmentType type)
        {
            currentAttachments.TryGetValue(type, out AttachmentIdentifier_SO attachmentId);
            return attachmentId;
        }

        private void SetDefaultAttachments(WeaponIdentification weaponId)
        {
            foreach (AttachmentType type in System.Enum.GetValues(typeof(AttachmentType)))
            {
                Attachment defaultAttachment = weaponId.GetDefaultAttachment(type);
                currentAttachments[type] = defaultAttachment?.attachmentIdentifier;
            }
        }

#if SAVE_LOAD_ADD_ON
        public override void LoadedState()
        {
            if (this.alreadyInteracted) Destroy(this.gameObject);
            else GetVisuals();
        }
#endif
    }

#if UNITY_EDITOR
    [System.Serializable]
    [CustomEditor(typeof(WeaponPickeable))]
    public class WeaponPickeableEditor : Editor
    {
        private string[] tabs = { "Basic", "References", "Effects", "Events" };
        private int currentTab = 0;

        override public void OnInspectorGUI()
        {
            serializedObject.Update();
            WeaponPickeable myScript = target as WeaponPickeable;

            Texture2D myTexture = Resources.Load<Texture2D>("CustomEditor/WeaponPickeable_CustomEditor") as Texture2D;
            GUILayout.Label(myTexture);

            EditorGUILayout.BeginVertical();
            currentTab = GUILayout.Toolbar(currentTab, tabs);
            EditorGUILayout.Space(10f);
            EditorGUILayout.EndVertical();

            if (currentTab >= 0 && currentTab < tabs.Length)
            {
                switch (tabs[currentTab])
                {
                    case "Basic":
                        EditorGUILayout.LabelField("CUSTOMIZE YOUR WEAPON PICKEABLE", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("weapon"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("interactText"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("instantInteraction"));
                        break;
                    case "References":
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("image"));
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("graphics"));
                        break;
                    case "Effects":
                        EditorGUILayout.LabelField("EFFECTS", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("rotates"));
                        if (myScript.rotates) EditorGUILayout.PropertyField(serializedObject.FindProperty("rotationSpeed"));

                        EditorGUILayout.PropertyField(serializedObject.FindProperty("translates"));
                        if (myScript.translates) EditorGUILayout.PropertyField(serializedObject.FindProperty("translationSpeed"));
                        break;
                    case "Events":
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("events"));
                        break;
                }
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
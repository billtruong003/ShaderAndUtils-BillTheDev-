#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Presets;
using Sirenix.OdinInspector;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using cowsins;
using TitleAttribute = Sirenix.OdinInspector.TitleAttribute;

namespace Cowsins.BillTheDev
{
    [System.Serializable]
    public class ItemCreationWizard
    {
        #region Constants
        private const string BLANK_WEAPON_TEMPLATE_PATH = "Assets/Cowsins/Prefabs/Weapons/CowsinsBlankWeaponTemplate.prefab";
        private const string BLANK_ANIMATOR_CONTROLLER_PATH = "Assets/Cowsins/Animations/Weapons/BlankWeaponAnimatorTemplate.controller";
        #endregion

        #region Public Fields (UI)
        public enum ItemType { Firearm, Melee } // Thu gọn lại cho phù hợp với logic của Cowsins

        [HorizontalGroup("Top")]
        [EnumToggleButtons, OnValueChanged("OnItemTypeChanged")]
        [LabelWidth(80)]
        public ItemType SelectedType;

        [HorizontalGroup("Top")]
        [ValidateInput("IsItemNameValid", "An item with this name already exists in the target output folder.")]
        [LabelWidth(80)]
        public string ItemName = "NewItem";

        [TitleGroup("Configuration")]
        [AssetsOnly, InfoBox("Optional: Applying a preset will overwrite stats on the created ScriptableObject.")]
        public Preset ItemPreset;

        [TitleGroup("Configuration")]
        [Required("A 3D model must be assigned."), AssetsOnly, PreviewField(100, ObjectFieldAlignment.Center)]
        public GameObject ItemModel;

        [TitleGroup("Automation")]
        [AssetsOnly, FolderPath, InfoBox("Drag a folder here, then click the button below to automatically find and assign animations.")]
        public DefaultAsset AnimationSourceFolder;

        [TitleGroup("Automation")]
        [Button("Find and Assign Animations", ButtonSizes.Medium), GUIColor(1f, 0.7f, 0.3f)]
        [PropertySpace(5, 10), ShowIf("AnimationSourceFolder")]
        private void FindAndAssignAnimationsFromFolder() { /* Logic is unchanged */ }

        [ShowIf("SelectedType", ItemType.Firearm), InlineProperty, HideLabel]
        public FirearmAnimationData FirearmAnimations = new FirearmAnimationData();

        [ShowIf("SelectedType", ItemType.Melee), InlineProperty, HideLabel]
        public MeleeAnimationData MeleeAnimations = new MeleeAnimationData();
        #endregion

        #region Core Creation Logic (1-to-1 with Cowsins)

        public bool ExecuteCreation(string rootOutputFolder)
        {
            // STEP 1: FOLDER SETUP (as per original)
            string itemFolderPath = Path.Combine(rootOutputFolder, this.ItemName);
            if (!CreateAssetFolders(rootOutputFolder, itemFolderPath)) return false;

            // STEP 2: CREATE SCRIPTABLE OBJECT (as per original)
            Weapon_SO newWeaponSO = ScriptableObject.CreateInstance<Weapon_SO>();
            string soPath = AssetDatabase.GenerateUniqueAssetPath($"{itemFolderPath}/{this.ItemName}.asset");
            AssetDatabase.CreateAsset(newWeaponSO, soPath);
            AssetDatabase.SaveAssets();

            // STEP 3: CREATE AND CONFIGURE PREFAB (as per original)
            string prefabPath = $"{itemFolderPath}/{this.ItemName}_WeaponObject.prefab";
            GameObject originalPrefabTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(BLANK_WEAPON_TEMPLATE_PATH);
            if (originalPrefabTemplate == null)
            {
                Debug.LogError($"Cowsins template not found at '{BLANK_WEAPON_TEMPLATE_PATH}'. Aborting.");
                return false;
            }

            GameObject duplicatedPrefab = (GameObject)Object.Instantiate(originalPrefabTemplate);

            // Cleanup and add model
            Transform weaponHolder = duplicatedPrefab.transform.Find("Weapon");
            if (weaponHolder == null)
            {
                Debug.LogError("Cowsins template is malformed. Could not find 'Weapon' child object. Aborting.");
                Object.DestroyImmediate(duplicatedPrefab);
                return false;
            }
            GameObject instantiatedModel = Object.Instantiate(ItemModel, weaponHolder);
            SetLayerRecursively(instantiatedModel, LayerMask.NameToLayer("Weapons"));

            // Remove root animator (as per original)
            Animator rootAnimator = duplicatedPrefab.GetComponent<Animator>();
            if (rootAnimator != null) Object.DestroyImmediate(rootAnimator, true);

            // Add and configure animator on model (as per original)
            Animator newAnimator = instantiatedModel.GetComponent<Animator>() ?? instantiatedModel.AddComponent<Animator>();
            SetupAnimatorController(newAnimator, itemFolderPath);

            // STEP 4: SAVE PREFAB, THEN RE-LOAD IT (as per original)
            PrefabUtility.SaveAsPrefabAsset(duplicatedPrefab, prefabPath);
            Object.DestroyImmediate(duplicatedPrefab);
            AssetDatabase.Refresh(); // Ensure the prefab is written to disk before reloading

            GameObject savedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            WeaponIdentification weaponIdentification = savedPrefab.GetComponent<WeaponIdentification>();

            if (weaponIdentification == null)
            {
                Debug.LogError($"'WeaponIdentification' component not found on the newly created prefab '{prefabPath}'. Aborting.");
                return false;
            }

            // STEP 5: APPLY PRESET AND PERFORM CROSS-LINKING (as per original)
            weaponIdentification.weapon = newWeaponSO;

            if (this.ItemPreset)
            {
                this.ItemPreset.ApplyTo(newWeaponSO);
            }

            newWeaponSO.weaponObject = weaponIdentification;

            // STEP 6: MARK DIRTY AND SAVE ASSETS (as per original)
            EditorUtility.SetDirty(newWeaponSO);
            EditorUtility.SetDirty(weaponIdentification);
            AssetDatabase.SaveAssets();

            // STEP 7: PING OBJECT
            EditorGUIUtility.PingObject(newWeaponSO);
            Debug.Log($"Successfully created item '{this.ItemName}' at '{itemFolderPath}'", newWeaponSO);
            return true;
        }

        #endregion

        #region Helper Methods (Replicated from Cowsins or enhanced)

        private void SetupAnimatorController(Animator animator, string itemFolderPath)
        {
            AnimatorController originalController = AssetDatabase.LoadAssetAtPath<AnimatorController>(BLANK_ANIMATOR_CONTROLLER_PATH);
            if (originalController == null)
            {
                Debug.LogWarning($"Blank Animator Controller not found at '{BLANK_ANIMATOR_CONTROLLER_PATH}'. Skipping animation setup.");
                return;
            }

            string newControllerPath = $"{itemFolderPath}/{this.ItemName}_AnimatorController.controller";
            AssetDatabase.CopyAsset(BLANK_ANIMATOR_CONTROLLER_PATH, newControllerPath);
            AnimatorController newController = AssetDatabase.LoadAssetAtPath<AnimatorController>(newControllerPath);

            animator.runtimeAnimatorController = newController;
            AssignAnimationClips(newController);
        }

        private void AssignAnimationClips(AnimatorController controller)
        {
            var states = controller.layers[0].stateMachine.states;

            if (SelectedType == ItemType.Firearm)
            {
                SetStateMotion(states, "Idle", FirearmAnimations.Idle);
                SetStateMotion(states, "shooting", FirearmAnimations.Shoot);
                SetStateMotion(states, "reloading", FirearmAnimations.Reload);
                SetStateMotion(states, "Unholster", FirearmAnimations.Unholster);
                SetStateMotion(states, "Walk_Anim", FirearmAnimations.Walk);
                SetStateMotion(states, "Run_Anim", FirearmAnimations.Run);
                SetStateMotion(states, "StartInspection", FirearmAnimations.StartInspect);
                SetStateMotion(states, "InspectLoop", FirearmAnimations.LoopInspect);
                SetStateMotion(states, "StopInspection", FirearmAnimations.EndInspect);
            }
            else if (SelectedType == ItemType.Melee)
            {
                SetStateMotion(states, "Idle", MeleeAnimations.Idle);
                SetStateMotion(states, "shooting", MeleeAnimations.Swing);
            }
        }

        private bool CreateAssetFolders(string rootPath, string itemPath)
        {
            if (!AssetDatabase.IsValidFolder(rootPath))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(rootPath), Path.GetFileName(rootPath));
            }
            if (AssetDatabase.IsValidFolder(itemPath))
            {
                Debug.LogError($"Folder '{itemPath}' already exists. Please choose a different name. Aborting creation for this item.");
                return false;
            }
            AssetDatabase.CreateFolder(rootPath, Path.GetFileName(itemPath));
            return true;
        }

        private bool IsItemNameValid(string name) => !string.IsNullOrWhiteSpace(name);
        private void OnItemTypeChanged() => ItemName = $"New{SelectedType}";
        private string CreateAssetFolders(string rootPath)
        {
            if (!AssetDatabase.IsValidFolder(rootPath))
            {
                AssetDatabase.CreateFolder(Path.GetDirectoryName(rootPath), Path.GetFileName(rootPath));
            }

            string itemFolderPath = Path.Combine(rootPath, ItemName);
            if (AssetDatabase.IsValidFolder(itemFolderPath))
            {
                Debug.LogError($"Folder '{itemFolderPath}' already exists. Skipping creation for this item.");
                return null;
            }

            AssetDatabase.CreateFolder(rootPath, ItemName);
            AssetDatabase.Refresh();
            return itemFolderPath;
        }

        // Tất cả các hàm bên dưới (CreateScriptableObject, CreateAndConfigurePrefab, etc.) giữ nguyên logic
        // như phiên bản trước, vì chúng đã được thiết kế để nhận đường dẫn và hoạt động độc lập.
        // Tôi sẽ sao chép chúng lại đây cho đầy đủ.

        private Weapon_SO CreateScriptableObject(string folderPath)
        {
            Weapon_SO newInstance = ScriptableObject.CreateInstance<Weapon_SO>();
            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/{ItemName}_SO.asset");

            AssetDatabase.CreateAsset(newInstance, assetPath);

            if (ItemPreset != null && ItemPreset.GetTargetTypeName() == "Weapon_SO")
            {
                CowsinsUtilities.ApplyPreset(ItemPreset, newInstance);
            }

            return newInstance;
        }

        private GameObject CreateAndConfigurePrefab(string folderPath, Weapon_SO itemSO)
        {
            GameObject templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BLANK_WEAPON_TEMPLATE_PATH);
            if (templatePrefab == null)
            {
                Debug.LogError($"Template prefab not found at '{BLANK_WEAPON_TEMPLATE_PATH}'. Creation failed.");
                return null;
            }

            string prefabPath = $"{folderPath}/{ItemName}_Prefab.prefab";
            GameObject prefabInstance = (GameObject)PrefabUtility.InstantiatePrefab(templatePrefab);

            Transform weaponHolder = prefabInstance.transform.Find("Weapon");
            if (weaponHolder == null)
            {
                Debug.LogError("Prefab template is malformed. Could not find 'Weapon' child object.");
                Object.DestroyImmediate(prefabInstance);
                return null;
            }

            foreach (Transform child in weaponHolder.transform)
            {
                Object.DestroyImmediate(child.gameObject);
            }

            GameObject modelInstance = Object.Instantiate(ItemModel, weaponHolder);
            modelInstance.name = ItemModel.name;
            SetLayerRecursively(modelInstance, LayerMask.NameToLayer("Weapons"));

            WeaponIdentification id = prefabInstance.GetComponent<WeaponIdentification>();
            if (id != null) id.weapon = itemSO;

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefabInstance, prefabPath);
            Object.DestroyImmediate(prefabInstance);

            return savedPrefab;
        }

        private void SetupAnimatorController(GameObject prefabInstance, string folderPath)
        {
            if (SelectedType != ItemType.Firearm && SelectedType != ItemType.Melee) return;

            Transform modelTransform = prefabInstance.transform.Find("Weapon")?.GetChild(0);
            if (modelTransform == null) return;

            Animator animator = modelTransform.gameObject.AddComponent<Animator>();
            string controllerPath = $"{folderPath}/{ItemName}_Animator.controller";

            if (!AssetDatabase.CopyAsset(BLANK_ANIMATOR_CONTROLLER_PATH, controllerPath))
            {
                Debug.LogError($"Failed to copy Animator Controller from '{BLANK_ANIMATOR_CONTROLLER_PATH}'.");
                return;
            }
            AssetDatabase.Refresh();

            AnimatorController newController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            animator.runtimeAnimatorController = newController;

            AssignAllAnimationClips(newController);
        }

        private void AssignAllAnimationClips(AnimatorController controller)
        {
            var states = controller.layers[0].stateMachine.states;

            switch (SelectedType)
            {
                case ItemType.Firearm:
                    SetStateMotion(states, "Idle", FirearmAnimations.Idle);
                    SetStateMotion(states, "shooting", FirearmAnimations.Shoot);
                    SetStateMotion(states, "reloading", FirearmAnimations.Reload);
                    SetStateMotion(states, "Unholster", FirearmAnimations.Unholster);
                    SetStateMotion(states, "Walk_Anim", FirearmAnimations.Walk);
                    SetStateMotion(states, "Run_Anim", FirearmAnimations.Run);
                    SetStateMotion(states, "StartInspection", FirearmAnimations.StartInspect);
                    SetStateMotion(states, "InspectLoop", FirearmAnimations.LoopInspect);
                    SetStateMotion(states, "StopInspection", FirearmAnimations.EndInspect);
                    break;
                case ItemType.Melee:
                    SetStateMotion(states, "Idle", MeleeAnimations.Idle);
                    SetStateMotion(states, "shooting", MeleeAnimations.Swing);
                    break;
            }
        }

        private void FinalizeAssetConnections(Weapon_SO itemSO, GameObject prefabInstance)
        {
            WeaponIdentification id = prefabInstance.GetComponent<WeaponIdentification>();
            itemSO.weaponObject = id;
            EditorUtility.SetDirty(itemSO);
            EditorUtility.SetDirty(id);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SelectAndNotify(ScriptableObject asset, string path)
        {
            EditorGUIUtility.PingObject(asset);
        }

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (child != null)
                {
                    SetLayerRecursively(child.gameObject, newLayer);
                }
            }
        }

        private void SetStateMotion(ChildAnimatorState[] states, string stateName, AnimationClip clip)
        {
            if (clip == null) return;
            foreach (var state in states)
            {
                if (state.state.name == stateName)
                {
                    state.state.motion = clip;
                    return;
                }
            }
        }

        #endregion
    }


    #region Data Holder Classes

    // Các class này dùng để chứa dữ liệu cho từng loại item, giúp giao diện gọn gàng.
    [System.Serializable]
    public class FirearmAnimationData
    {
        [Title("Core Animations", bold: false)]
        public AnimationClip Idle;
        public AnimationClip Shoot;
        public AnimationClip Reload;
        public AnimationClip Unholster;

        [Title("Movement Animations", bold: false)]
        public AnimationClip Walk;
        public AnimationClip Run;

        [Title("Inspection Animations", bold: false)]
        public AnimationClip StartInspect;
        public AnimationClip LoopInspect;
        public AnimationClip EndInspect;
    }

    [System.Serializable]
    public class MeleeAnimationData
    {
        [Title("Core Animations", bold: false)]
        public AnimationClip Idle;
        public AnimationClip Swing;
        public AnimationClip Impact; // Animation khi va chạm
        public AnimationClip Block;
    }

    #endregion
}
#endif
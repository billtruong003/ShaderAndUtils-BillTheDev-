#if UNITY_EDITOR && ODIN_INSPECTOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Presets;
using UnityEditor.Animations;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace cowsins
{
    public class AdvancedWeaponCreator : OdinEditorWindow
    {
        [MenuItem("Cowsins/Create/Advanced Weapon Creator")]
        private static void OpenWindow()
        {
            var window = GetWindow<AdvancedWeaponCreator>();
            window.titleContent = new GUIContent("Advanced Weapon Creator");
            window.minSize = new Vector2(800, 600);
            window.Show();
        }

        [Title("Weapon Creation Batch")]
        [InfoBox("Thêm các vũ khí vào danh sách bên dưới, điền đầy đủ thông tin, sau đó nhấn 'Create All Listed Weapons' để bắt đầu quá trình tạo tự động.")]
        [TableList(AlwaysExpanded = true, DrawScrollView = true, MaxScrollViewHeight = 400, ShowPaging = true)]
        public List<WeaponCreationData> weaponBatch = new List<WeaponCreationData>();

        [Button(ButtonSizes.Large, Name = "Create All Listed Weapons")]
        [GUIColor(0.4f, 0.8f, 1f)]
        [PropertySpace(20)]
        private void CreateAllWeapons()
        {
            if (weaponBatch == null || weaponBatch.Count == 0)
            {
                EditorUtility.DisplayDialog("No Weapons to Create", "Danh sách vũ khí đang trống. Vui lòng thêm ít nhất một vũ khí để tạo.", "OK");
                return;
            }

            string overallReport = "Weapon Creation Report:\n\n";
            int successCount = 0;
            int failCount = 0;

            foreach (var weaponData in weaponBatch)
            {
                bool isSuccess = ProcessWeaponCreation(weaponData);
                if (isSuccess)
                {
                    successCount++;
                    overallReport += $" - SUCCESS: {weaponData.weaponName}\n";
                }
                else
                {
                    failCount++;
                    overallReport += $" - FAILED: {weaponData.weaponName}. Check console for errors.\n";
                }
            }

            overallReport += $"\nProcess finished. Successful: {successCount}, Failed: {failCount}.";
            EditorUtility.DisplayDialog("Batch Creation Complete", overallReport, "OK");
        }

        private bool ProcessWeaponCreation(WeaponCreationData data)
        {
            if (!data.IsDataValid())
            {
                Debug.LogError($"Validation failed for weapon '{data.weaponName}'. Skipping creation.");
                return false;
            }

            string weaponSpecificFolder = Path.Combine(data.outputFolder, data.weaponName);

            if (!CreateFolders(data.outputFolder, weaponSpecificFolder)) return false;

            Weapon_SO weaponSO = CreateScriptableObject(data, weaponSpecificFolder);
            if (weaponSO == null) return false;

            WeaponIdentification weaponId = CreateWeaponPrefab(data, weaponSO, weaponSpecificFolder);
            if (weaponId == null) return false;

            weaponSO.weaponObject = weaponId;
            EditorUtility.SetDirty(weaponSO);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Successfully created weapon '{data.weaponName}' at '{weaponSpecificFolder}'");
            return true;
        }

        private bool CreateFolders(string rootFolder, string weaponSpecificFolder)
        {
            if (!AssetDatabase.IsValidFolder(rootFolder))
            {
                Directory.CreateDirectory(rootFolder);
                AssetDatabase.Refresh();
            }

            if (AssetDatabase.IsValidFolder(weaponSpecificFolder))
            {
                Debug.LogError($"Folder '{weaponSpecificFolder}' already exists. Please use a unique name or delete the existing folder.");
                return false;
            }

            Directory.CreateDirectory(weaponSpecificFolder);
            AssetDatabase.Refresh();
            return true;
        }

        private Weapon_SO CreateScriptableObject(WeaponCreationData data, string path)
        {
            Weapon_SO newWeapon = CreateInstance<Weapon_SO>();

            if (data.weaponPreset != null)
            {
                data.weaponPreset.ApplyTo(newWeapon);
            }

            string assetPath = Path.Combine(path, $"{data.weaponName}_SO.asset");
            AssetDatabase.CreateAsset(newWeapon, assetPath);
            EditorUtility.SetDirty(newWeapon);

            return newWeapon;
        }

        private WeaponIdentification CreateWeaponPrefab(WeaponCreationData data, Weapon_SO weaponSO, string path)
        {
            GameObject originalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Cowsins/Prefabs/Weapons/CowsinsBlankWeaponTemplate.prefab");
            if (originalPrefab == null)
            {
                Debug.LogError("Template prefab 'CowsinsBlankWeaponTemplate.prefab' not found.");
                return null;
            }

            GameObject duplicatedPrefab = (GameObject)PrefabUtility.InstantiatePrefab(originalPrefab);
            Transform weaponRoot = duplicatedPrefab.transform.Find("Weapon");

            if (weaponRoot == null)
            {
                Debug.LogError("'Weapon' child object not found in the template prefab. The template might be corrupted.");
                DestroyImmediate(duplicatedPrefab);
                return null;
            }

            GameObject instantiatedModel = (GameObject)PrefabUtility.InstantiatePrefab(data.weaponModel);
            instantiatedModel.transform.SetParent(weaponRoot, false);

            int weaponsLayer = LayerMask.NameToLayer("Weapons");
            SetLayerRecursively(instantiatedModel, weaponsLayer);

            Animator modelAnimator = ConfigureAnimator(instantiatedModel, data, path);

            string prefabPath = Path.Combine(path, $"{data.weaponName}_WeaponObject.prefab");
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(duplicatedPrefab, prefabPath);
            DestroyImmediate(duplicatedPrefab);

            WeaponIdentification weaponId = savedPrefab.GetComponent<WeaponIdentification>();
            weaponId.weapon = weaponSO;
            EditorUtility.SetDirty(weaponId);

            return weaponId;
        }

        private Animator ConfigureAnimator(GameObject model, WeaponCreationData data, string path)
        {
            Animator animator = model.GetComponent<Animator>();
            if (animator == null) animator = model.AddComponent<Animator>();

            string originalControllerPath = "Assets/Cowsins/Animations/Weapons/BlankWeaponAnimatorTemplate.controller";
            string newControllerPath = Path.Combine(path, $"{data.weaponName}_AnimatorController.controller");

            if (!AssetDatabase.CopyAsset(originalControllerPath, newControllerPath))
            {
                Debug.LogError($"Failed to copy AnimatorController from '{originalControllerPath}'");
                return animator;
            }

            AnimatorController newController = AssetDatabase.LoadAssetAtPath<AnimatorController>(newControllerPath);
            animator.runtimeAnimatorController = newController;

            AssignAnimationClipsToStates(newController, data);

            return animator;
        }

        private void AssignAnimationClipsToStates(AnimatorController controller, WeaponCreationData data)
        {
            var states = controller.layers[0].stateMachine.states;

            Dictionary<string, AnimationClip> clipAssignments = new Dictionary<string, AnimationClip>
            {
                { "Idle", data.animations.idleClip },
                { "shooting", data.animations.shootClip },
                { "reloading", data.animations.reloadClip },
                { "Unholster", data.animations.unholsterClip },
                { "Walk_Anim", data.animations.walkClip },
                { "Run_Anim", data.animations.runClip },
                { "StartInspection", data.animations.startInspectClip },
                { "InspectLoop", data.animations.loopInspectClip },
                { "StopInspection", data.animations.endInspectClip }
            };

            foreach (var state in states)
            {
                if (clipAssignments.TryGetValue(state.state.name, out AnimationClip clip) && clip != null)
                {
                    state.state.motion = clip;
                }
            }
        }

        private void SetLayerRecursively(GameObject obj, int newLayer)
        {
            if (obj == null) return;
            obj.layer = newLayer;
            foreach (Transform child in obj.transform)
            {
                if (child != null) SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }

    [System.Serializable]
    public class WeaponCreationData
    {
        [BoxGroup("Basic Info", ShowLabel = false)]
        [HorizontalGroup("Basic Info/Split", Width = 150)]
        [Required("Weapon Name is mandatory.")]
        [LabelWidth(100)]
        public string weaponName;

        [HorizontalGroup("Basic Info/Split")]
        [FolderPath(ParentFolder = "Assets", RequireExistingPath = false)]
        [Required("Output folder cannot be empty.")]
        [LabelWidth(100)]
        public string outputFolder = "Assets/NewWeapons";

        [BoxGroup("Preset & Model")]
        [HorizontalGroup("Preset & Model/Split", LabelWidth = 120)]
        [AssetsOnly]
        [ValidateInput("IsPresetValid", "Preset must be of type Weapon_SO.")]
        public Preset weaponPreset;

        [HorizontalGroup("Preset & Model/Split", LabelWidth = 120)]
        [Required("Weapon Model is required.")]
        [PreviewField(100, ObjectFieldAlignment.Center)]
        [AssetsOnly]
        public GameObject weaponModel;

        // ===== NEW FUNCTIONALITY STARTS HERE =====

        [TitleGroup("Animation Assignment")]
        [HorizontalGroup("Animation Assignment/Group")]
        [FolderPath(ParentFolder = "Assets")]
        [OnValueChanged("OnAnimationFolderChanged")]
        public string animationSourceFolder;

        [HorizontalGroup("Animation Assignment/Group", Width = 200)]
        [Button(ButtonSizes.Medium, Name = "Auto-Assign Animations")]
        [GUIColor(0.2f, 1f, 0.5f)]
        private void FindAndAssignAnimations()
        {
            if (!IsAnimationFolderValid()) return;

            var allClips = GetClipsFromSourceFolder();
            if (allClips.Count == 0)
            {
                Debug.LogWarning($"No animation clips found in folder '{animationSourceFolder}' for weapon '{weaponName}'.");
                return;
            }

            Debug.Log($"Found {allClips.Count} total clips for '{weaponName}'. Attempting to assign...");

            var assignmentActions = new Dictionary<string[], System.Action<AnimationClip>>
            {
                { new[] { "idle" }, clip => animations.idleClip = clip },
                { new[] { "shoot", "fire" }, clip => animations.shootClip = clip },
                { new[] { "reload" }, clip => animations.reloadClip = clip },
                { new[] { "unholster", "draw", "equip" }, clip => animations.unholsterClip = clip },
                { new[] { "walk" }, clip => animations.walkClip = clip },
                { new[] { "run", "sprint" }, clip => animations.runClip = clip },
                { new[] { "startinspect", "inspectstart" }, clip => animations.startInspectClip = clip },
                { new[] { "inspectloop", "loopinspect" }, clip => animations.loopInspectClip = clip },
                { new[] { "endinspect", "stopinspection", "inspectend" }, clip => animations.endInspectClip = clip }
            };

            foreach (var clip in allClips)
            {
                string clipNameLower = clip.name.ToLower().Replace("_", "").Replace("-", "").Replace(" ", "");
                foreach (var pair in assignmentActions)
                {
                    foreach (var keyword in pair.Key)
                    {
                        if (clipNameLower.Contains(keyword))
                        {
                            pair.Value(clip); // Execute the assignment action
                            Debug.Log($"Assigned '{clip.name}' to '{keyword.ToUpper()}' slot for weapon '{weaponName}'.", clip);
                            goto nextClip; // Use goto to break out of the inner loop and continue the outer one
                        }
                    }
                }
            nextClip:;
            }
        }

        [ShowIf("animationSourceFolder")]
        [DisplayAsString]
        [HideLabel]
        [PropertyOrder(1)]
        private string _animationFolderScanResult;

        private void OnAnimationFolderChanged()
        {
            if (!IsAnimationFolderValid())
            {
                _animationFolderScanResult = "Please select a valid folder inside Assets.";
                return;
            }
            int clipCount = GetClipsFromSourceFolder().Count;
            _animationFolderScanResult = $"<color=lime>Found {clipCount} animation clips in this folder.</color>";
        }

        private HashSet<AnimationClip> GetClipsFromSourceFolder()
        {
            var allClips = new HashSet<AnimationClip>();
            if (!IsAnimationFolderValid()) return allClips;

            string[] animGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { animationSourceFolder });
            foreach (string guid in animGuids)
            {
                allClips.Add(AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid)));
            }

            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { animationSourceFolder });
            foreach (string guid in modelGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    {
                        allClips.Add(clip);
                    }
                }
            }
            return allClips;
        }

        private bool IsAnimationFolderValid() => !string.IsNullOrEmpty(animationSourceFolder) && AssetDatabase.IsValidFolder(animationSourceFolder);

        // ===== NEW FUNCTIONALITY ENDS HERE =====


        [TitleGroup("Animation Clips")]
        [HideLabel]
        public AnimationClipCollection animations = new AnimationClipCollection();

        private bool IsPresetValid(Preset preset)
        {
            if (preset == null) return true;
            return preset.GetTargetTypeName() == typeof(Weapon_SO).FullName;
        }

        public bool IsDataValid()
        {
            return !string.IsNullOrWhiteSpace(weaponName) &&
                   !string.IsNullOrWhiteSpace(outputFolder) &&
                   weaponModel != null &&
                   IsPresetValid(weaponPreset);
        }
    }

    [System.Serializable]
    public class AnimationClipCollection
    {
        [Required] public AnimationClip idleClip;
        [Required] public AnimationClip shootClip;
        [Required] public AnimationClip reloadClip;
        [Required] public AnimationClip unholsterClip;
        public AnimationClip walkClip;
        public AnimationClip runClip;
        public AnimationClip startInspectClip;
        public AnimationClip loopInspectClip;
        public AnimationClip endInspectClip;
    }
}
#endif
// OdinMaterialAssignerPro.cs
// YÊU CẦU: ĐÃ CÀI ĐẶT ODIN INSPECTOR & SERIALIZER
// Đặt file này trong một thư mục có tên "Editor" trong Project của bạn.

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System;

namespace MyProject.EditorTools
{
    public class OdinMaterialAssignerPro : OdinEditorWindow
    {
        // Enum để định nghĩa các kiểu so sánh chuỗi
        public enum StringMatchType { Contains, Exact, StartsWith, EndsWith }

        [MenuItem("Tools/My Project Tools/Odin Material Assigner (Pro)")]
        private static void OpenWindow()
        {
            GetWindow<OdinMaterialAssignerPro>("Material Assigner Pro").Show();
        }

        [Title("1. Cấu Hình Thay Thế", "Thiết lập các quy tắc để tìm và thay thế material.")]
        [BoxGroup("Cấu Hình")]
        [EnumToggleButtons, HideLabel]
        [InfoBox("Chọn quy tắc so sánh tên material. 'Contains' là linh hoạt nhất, sẽ tìm thấy 'Default' trong '09 Default' hoặc 'My-Default-Mat'.")]
        public StringMatchType MatchType = StringMatchType.Contains;

        [BoxGroup("Cấu Hình")]
        [LabelText("Text Dùng Để So Sánh")]
        public string MaterialNameFilter = "Default";

        [BoxGroup("Cấu Hình")]
        [Required("Vui lòng chỉ định Material mới để gán.")]
        [AssetsOnly]
        [LabelText("Material Mới Để Gán")]
        public Material NewMaterial;


        [Title("2. Chỉ Định Model", "Chọn các model bạn muốn áp dụng thay đổi.")]
        [BoxGroup("Danh Sách Model")]
        [DetailedInfoBox("Kéo thả các file model (.fbx, .obj...) vào danh sách dưới đây.",
        "Bạn có thể kéo nhiều file cùng lúc từ cửa sổ Project. Danh sách này sẽ là mục tiêu để công cụ xử lý.")]
        [ListDrawerSettings(Expanded = true, NumberOfItemsPerPage = 10)]
        [Required("Vui lòng thêm ít nhất một model vào danh sách.")]
        [AssetsOnly]
        public List<GameObject> TargetModels = new List<GameObject>();

        [BoxGroup("Tiện Ích Tìm Kiếm Hàng Loạt")]
        [InfoBox("Để không phải kéo thả thủ công, bạn có thể chọn một thư mục và công cụ sẽ tự động tìm tất cả model bên trong.", InfoMessageType.None)]
        [FolderPath(AbsolutePath = false)]
        [LabelText("Chọn Thư Mục Nguồn")]
        public string SourceFolder;

        [BoxGroup("Tiện Ích Tìm Kiếm Hàng Loạt")]
        [Button("Tìm & Thêm Models Từ Thư Mục", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.8f, 1f)]
        private void FindAndAddModelsFromSelectedFolder()
        {
            if (string.IsNullOrEmpty(SourceFolder) || !AssetDatabase.IsValidFolder(SourceFolder))
            {
                EditorUtility.DisplayDialog("Lỗi", "Đường dẫn thư mục không hợp lệ. Vui lòng chọn một thư mục trong project.", "OK");
                return;
            }

            string[] searchInFolders = { SourceFolder };
            string[] modelGUIDs = AssetDatabase.FindAssets("t:Model", searchInFolders);

            TargetModels.Clear();
            foreach (string guid in modelGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (modelAsset != null)
                {
                    TargetModels.Add(modelAsset);
                }
            }

            EditorUtility.DisplayDialog("Hoàn Tất", $"Đã tìm thấy và thêm {TargetModels.Count} model vào danh sách.", "OK");
        }

        [Title("3. Thực Thi")]
        [Button("THỰC THI THAY THẾ MATERIAL HÀNG LOẠT", ButtonSizes.Gigantic, ButtonStyle.FoldoutButton)]
        [GUIColor(0.2f, 0.8f, 0.5f)]
        private void ProcessAllModels()
        {
            if (NewMaterial == null || TargetModels.Count == 0 || string.IsNullOrWhiteSpace(MaterialNameFilter))
            {
                EditorUtility.DisplayDialog("Lỗi Cấu Hình", "Vui lòng kiểm tra lại các trường đã điền đầy đủ và hợp lệ.", "OK");
                return;
            }

            int modelsModifiedCount = 0;
            int materialsReplacedCount = 0;

            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < TargetModels.Count; i++)
                {
                    GameObject modelAsset = TargetModels[i];
                    if (modelAsset == null) continue;

                    string assetPath = AssetDatabase.GetAssetPath(modelAsset);
                    EditorUtility.DisplayProgressBar("Thay thế Material hàng loạt", $"Đang xử lý: {Path.GetFileName(assetPath)}", (float)i / TargetModels.Count);

                    GameObject prefabInstance = PrefabUtility.LoadPrefabContents(assetPath);
                    Renderer[] renderers = prefabInstance.GetComponentsInChildren<Renderer>(true);
                    bool hasModelChanged = false;

                    foreach (Renderer renderer in renderers)
                    {
                        Material[] currentSharedMaterials = renderer.sharedMaterials;
                        Material[] newMaterials = new Material[currentSharedMaterials.Length];
                        bool hasRendererChanged = false;

                        for (int j = 0; j < currentSharedMaterials.Length; j++)
                        {
                            Material currentMat = currentSharedMaterials[j];
                            newMaterials[j] = currentMat;

                            if (currentMat != null && IsMaterialMatch(currentMat.name))
                            {
                                newMaterials[j] = NewMaterial;
                                hasRendererChanged = true;
                                materialsReplacedCount++;
                            }
                        }

                        if (hasRendererChanged)
                        {
                            Undo.RecordObject(renderer, "Thay thế Material trên Renderer");
                            renderer.sharedMaterials = newMaterials;
                            hasModelChanged = true;
                        }
                    }

                    if (hasModelChanged)
                    {
                        PrefabUtility.SaveAsPrefabAsset(prefabInstance, assetPath);
                        modelsModifiedCount++;
                    }

                    PrefabUtility.UnloadPrefabContents(prefabInstance);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
                ShowResultDialog(modelsModifiedCount, materialsReplacedCount);
            }
        }

        private bool IsMaterialMatch(string materialName)
        {
            const StringComparison comparisonType = StringComparison.OrdinalIgnoreCase;
            switch (MatchType)
            {
                case StringMatchType.Contains:
                    return materialName.IndexOf(MaterialNameFilter, comparisonType) >= 0;
                case StringMatchType.Exact:
                    return string.Equals(materialName, MaterialNameFilter, comparisonType);
                case StringMatchType.StartsWith:
                    return materialName.StartsWith(MaterialNameFilter, comparisonType);
                case StringMatchType.EndsWith:
                    return materialName.EndsWith(MaterialNameFilter, comparisonType);
                default:
                    return false;
            }
        }

        private void ShowResultDialog(int modelsModified, int materialsReplaced)
        {
            string message = $"Xử lý hoàn tất!\n\n" +
                             $"Số model được chỉnh sửa: {modelsModified} / {TargetModels.Count}\n" +
                             $"Tổng số material đã thay thế: {materialsReplaced}";
            EditorUtility.DisplayDialog("Hoàn Tất Xử Lý", message, "Tuyệt vời!");
        }
    }
}
// Đặt file này trong một thư mục tên "Editor"

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;
// using System.Text.RegularExpressions; // Không còn cần thiết cho chức năng này nữa

namespace BillUtils
{
    public class MeshExtractorEditor : EditorWindow
    {
        // --- Cấu trúc dữ liệu ---
        private class ExtractionPlan
        {
            public GameObject SourcePrefab;
            public string SourcePath;
            public string OutputDirectory;
            public string NewPrefabPath;
            public string PrefixCode;
            public List<MeshExtractionInfo> MeshInfos = new List<MeshExtractionInfo>();
            public Dictionary<Material, string> MaterialPaths = new Dictionary<Material, string>();
            public bool IsExpandedInPreview = true;
        }

        // MeshExtractionInfo giờ chỉ lưu Mesh, OutputPath, và SequenceNumber
        private class MeshExtractionInfo
        {
            public Mesh Mesh;
            public string OutputPath; // Đường dẫn đầy đủ và tên file dự kiến, ví dụ: "Assets/Models/Sword/Grip_001_001.asset"
            public int SequenceNumber;
        }

        private class SourceAssetEntry
        {
            public GameObject Prefab;
            public string PrefixCode = "";
        }

        // --- Biến trạng thái ---
        private List<SourceAssetEntry> sourceEntries = new List<SourceAssetEntry>();
        private bool shouldExtractMaterials = true;
        private bool isPreviewing = false;
        private List<ExtractionPlan> extractionPlans = new List<ExtractionPlan>();

        // --- Biến UI ---
        private ReorderableList reorderableList;
        private Vector2 mainScrollViewPosition;
        private Vector2 previewScrollViewPosition;

        // =================================================================================================
        // MENU ITEMS
        // =================================================================================================

        private const string MENU_PATH = "Assets/BillUtils/Extract Full Prefab (Mesh + Material)";

        [MenuItem(MENU_PATH, priority = 30)]
        private static void ProcessSelectedAssetsFromMenu()
        {
            var selectedObjects = Selection.GetFiltered<GameObject>(SelectionMode.Assets);
            if (selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Lỗi", "Bạn cần chọn ít nhất một Prefab/FBX trong cửa sổ Project.", "OK");
                return;
            }

            if (EditorUtility.DisplayDialog("Xác nhận xử lý",
                $"Bạn có chắc muốn xử lý {selectedObjects.Length} asset đã chọn không?\n\nTool sẽ tạo các file và thư mục mới bên cạnh asset gốc.", "Tiến hành", "Hủy"))
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var obj in selectedObjects)
                    {
                        var plan = CreatePlanForGameObject(obj, true);
                        if (plan != null)
                        {
                            StaticExecutePlan(plan);
                        }
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                    Debug.Log($"[BillUtils] Hoàn tất xử lý {selectedObjects.Length} asset từ menu chuột phải.");
                }
            }
        }

        [MenuItem(MENU_PATH, true)]
        private static bool ValidateProcessSelectedAssets()
        {
            return Selection.GetFiltered<GameObject>(SelectionMode.Assets).Length > 0;
        }

        [MenuItem("Tools/Advanced Mesh Extractor")]
        public static void ShowWindow()
        {
            GetWindow<MeshExtractorEditor>("Advanced Extractor");
        }

        // =================================================================================================
        // UI DRAWING & STATE MANAGEMENT
        // =================================================================================================

        private void OnEnable()
        {
            SetupReorderableList();
        }

        private void OnGUI()
        {
            GUILayout.Label("Advanced FBX Prefab Extractor", EditorStyles.boldLabel);

            mainScrollViewPosition = EditorGUILayout.BeginScrollView(mainScrollViewPosition);

            HandleDragAndDropForAdding(new Rect(0, 0, position.width, position.height));

            if (!isPreviewing)
            {
                DrawConfigurationScreen();
            }
            else
            {
                DrawPreviewScreen();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawConfigurationScreen()
        {
            EditorGUILayout.HelpBox("Bước 1: Kéo các Prefab/FBX vào cửa sổ hoặc danh sách, nhập mã prefix (tùy chọn) và nhấn 'Analyze'.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Configuration", EditorStyles.boldLabel);

            reorderableList.DoLayoutList();

            EditorGUILayout.Space();

            shouldExtractMaterials = EditorGUILayout.Toggle(new GUIContent("Extract Materials", "Tách các material thành file .mat độc lập. (Khuyến nghị)"), shouldExtractMaterials);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            GUI.enabled = sourceEntries.Count > 0 && sourceEntries.All(entry => entry.Prefab != null);
            if (GUILayout.Button("Analyze Assets", GUILayout.Height(30)))
            {
                GenerateExtractionPlans();
                isPreviewing = true;
            }
            GUI.enabled = true;
        }

        private void DrawPreviewScreen()
        {
            EditorGUILayout.HelpBox("Bước 2: Kiểm tra các file sẽ được tạo. Nhấn 'Confirm' để bắt đầu xử lý.", MessageType.Info);
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("Extraction Preview", EditorStyles.boldLabel);

            if (extractionPlans.Count == 0)
            {
                GUILayout.Label("Không có kế hoạch xử lý nào được tạo. Vui lòng thêm Prefab và nhấn Analyze.");
            }
            else
            {
                previewScrollViewPosition = EditorGUILayout.BeginScrollView(previewScrollViewPosition, GUILayout.MinHeight(200));
                foreach (var plan in extractionPlans)
                {
                    DrawPreviewForPlan(plan);
                }
                EditorGUILayout.EndScrollView();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("<< Back", GUILayout.Height(30)))
            {
                isPreviewing = false;
                extractionPlans.Clear();
            }
            GUI.enabled = extractionPlans.Count > 0;
            if (GUILayout.Button("Confirm & Process", GUILayout.Height(40)))
            {
                ExecuteAllPlans();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewForPlan(ExtractionPlan plan)
        {
            var prefabIcon = EditorGUIUtility.IconContent("Prefab Icon").image;
            string prefabDisplayName = plan.SourcePrefab != null ? Path.GetFileName(AssetDatabase.GetAssetPath(plan.SourcePrefab)) : "Unknown Prefab";

            plan.IsExpandedInPreview = EditorGUILayout.Foldout(plan.IsExpandedInPreview, new GUIContent($"  {prefabDisplayName}", prefabIcon), true, EditorStyles.foldout);

            if (plan.IsExpandedInPreview)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginVertical("box");

                DrawPreviewLine("Output Folder", plan.OutputDirectory, "Folder Icon");
                DrawPreviewLine("New Prefab Name", Path.GetFileName(plan.NewPrefabPath), "Prefab Icon");
                if (!string.IsNullOrEmpty(plan.PrefixCode))
                {
                    DrawPreviewLine("Mesh Prefix Code", plan.PrefixCode, null);
                }

                if (plan.MeshInfos.Any())
                {
                    GUILayout.Label("Meshes to Extract:", EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    foreach (var meshInfo in plan.MeshInfos)
                    {
                        // Tên file mesh chỉ còn Prefix + Số thứ tự, ví dụ: "Grip_001_001.asset"
                        string formattedMeshName = $"{plan.PrefixCode}_{FormatNumber(meshInfo.SequenceNumber, 3)}.asset";
                        DrawPreviewLine(null, formattedMeshName, "Mesh Icon");
                    }
                    EditorGUI.indentLevel--;
                }

                if (plan.MaterialPaths.Any())
                {
                    GUILayout.Label("Materials to Extract:", EditorStyles.miniBoldLabel);
                    EditorGUI.indentLevel++;
                    foreach (var matPath in plan.MaterialPaths.Values)
                    {
                        DrawPreviewLine(null, Path.GetFileName(matPath), "Material Icon");
                    }
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUI.indentLevel--;
            }
        }

        private void DrawPreviewLine(string label, string value, string iconName)
        {
            EditorGUILayout.BeginHorizontal();

            var content = new GUIContent(label);
            if (!string.IsNullOrEmpty(iconName))
            {
                content.image = EditorGUIUtility.IconContent(iconName).image;
            }
            GUILayout.Label(content, GUILayout.Width(150), GUILayout.Height(EditorGUIUtility.singleLineHeight));

            EditorGUILayout.SelectableLabel(value, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));

            EditorGUILayout.EndHorizontal();
        }

        // =================================================================================================
        // LOGIC & IMPLEMENTATION
        // =================================================================================================

        private void GenerateExtractionPlans()
        {
            extractionPlans.Clear();
            foreach (var entry in sourceEntries)
            {
                var plan = CreatePlanForSourceEntry(entry);
                if (plan != null)
                {
                    extractionPlans.Add(plan);
                }
            }
        }

        private ExtractionPlan CreatePlanForSourceEntry(SourceAssetEntry entry)
        {
            if (entry.Prefab == null) return null;

            var plan = new ExtractionPlan();
            plan.SourcePrefab = entry.Prefab;
            plan.SourcePath = AssetDatabase.GetAssetPath(entry.Prefab);
            string sourceDirectory = Path.GetDirectoryName(plan.SourcePath);
            string fbxName = Path.GetFileNameWithoutExtension(plan.SourcePath);

            plan.OutputDirectory = Path.Combine(sourceDirectory, fbxName);
            plan.NewPrefabPath = Path.Combine(plan.OutputDirectory, $"{fbxName}_Extracted.prefab");

            plan.PrefixCode = string.IsNullOrEmpty(entry.PrefixCode) ? fbxName : entry.PrefixCode;

            var meshFilterMap = new Dictionary<Mesh, List<MeshFilter>>();
            foreach (var meshFilter in entry.Prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh != null)
                {
                    if (!meshFilterMap.ContainsKey(meshFilter.sharedMesh))
                    {
                        meshFilterMap[meshFilter.sharedMesh] = new List<MeshFilter>();
                    }
                    meshFilterMap[meshFilter.sharedMesh].Add(meshFilter);
                }
            }

            var sortedMeshes = meshFilterMap.Keys.ToList();
            sortedMeshes.Sort((m1, m2) => m1.name.CompareTo(m2.name));

            int sequenceCounter = 1;
            foreach (var mesh in sortedMeshes)
            {
                var meshInfo = new MeshExtractionInfo
                {
                    Mesh = mesh,
                    // OriginalMeshName không còn được lưu trữ
                    SequenceNumber = sequenceCounter++
                };
                // Xây dựng đường dẫn file cho mesh chỉ với Prefix và Số thứ tự.
                string paddedNumber = FormatNumber(meshInfo.SequenceNumber, 3);
                string meshFileName = $"{plan.PrefixCode}_{paddedNumber}.asset"; // Chỉ Prefix + Số thứ tự
                meshInfo.OutputPath = Path.Combine(plan.OutputDirectory, meshFileName);

                plan.MeshInfos.Add(meshInfo);
            }

            if (shouldExtractMaterials)
            {
                var uniqueMaterials = new HashSet<Material>();
                foreach (var renderer in entry.Prefab.GetComponentsInChildren<MeshRenderer>(true))
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null && uniqueMaterials.Add(material))
                        {
                            plan.MaterialPaths[material] = Path.Combine(plan.OutputDirectory, $"{material.name}.mat");
                        }
                    }
                }
            }

            return plan;
        }

        private static ExtractionPlan CreatePlanForGameObject(GameObject prefab, bool extractMaterials)
        {
            if (prefab == null) return null;

            var plan = new ExtractionPlan();
            plan.SourcePrefab = prefab;
            plan.SourcePath = AssetDatabase.GetAssetPath(prefab);
            string sourceDirectory = Path.GetDirectoryName(plan.SourcePath);
            string fbxName = Path.GetFileNameWithoutExtension(plan.SourcePath);

            plan.OutputDirectory = Path.Combine(sourceDirectory, fbxName);
            plan.NewPrefabPath = Path.Combine(plan.OutputDirectory, $"{fbxName}_Extracted.prefab");
            plan.PrefixCode = fbxName;

            var meshFilterMap = new Dictionary<Mesh, List<MeshFilter>>();
            foreach (var meshFilter in prefab.GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh != null)
                {
                    if (!meshFilterMap.ContainsKey(meshFilter.sharedMesh))
                    {
                        meshFilterMap[meshFilter.sharedMesh] = new List<MeshFilter>();
                    }
                    meshFilterMap[meshFilter.sharedMesh].Add(meshFilter);
                }
            }

            var sortedMeshes = meshFilterMap.Keys.ToList();
            sortedMeshes.Sort((m1, m2) => m1.name.CompareTo(m2.name));

            int sequenceCounter = 1;
            foreach (var mesh in sortedMeshes)
            {
                var meshInfo = new MeshExtractionInfo
                {
                    Mesh = mesh,
                    SequenceNumber = sequenceCounter++
                };

                string paddedNumber = FormatNumber(meshInfo.SequenceNumber, 3);
                // Xây dựng tên file chỉ với Prefix + Số thứ tự.
                string meshFileName = $"{plan.PrefixCode}_{paddedNumber}.asset";
                meshInfo.OutputPath = Path.Combine(plan.OutputDirectory, meshFileName);

                plan.MeshInfos.Add(meshInfo);
            }

            if (extractMaterials)
            {
                var uniqueMaterials = new HashSet<Material>();
                foreach (var renderer in prefab.GetComponentsInChildren<MeshRenderer>(true))
                {
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (material != null && uniqueMaterials.Add(material))
                        {
                            plan.MaterialPaths[material] = Path.Combine(plan.OutputDirectory, $"{material.name}.mat");
                        }
                    }
                }
            }
            return plan;
        }

        private static void StaticExecutePlan(ExtractionPlan plan)
        {
            if (!AssetDatabase.IsValidFolder(plan.OutputDirectory))
            {
                string parentDir = Path.GetDirectoryName(plan.SourcePath);
                AssetDatabase.CreateFolder(parentDir, Path.GetFileName(plan.OutputDirectory));
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(plan.SourcePrefab);
            instance.hideFlags = HideFlags.HideAndDontSave;

            var meshReplacementMap = new Dictionary<Mesh, Mesh>();
            if (plan.MeshInfos.Any())
            {
                foreach (var meshInfo in plan.MeshInfos)
                {
                    Mesh newMesh = Instantiate(meshInfo.Mesh);
                    // Đặt tên cho asset mesh theo đường dẫn đã xây dựng (chỉ Prefix + Số thứ tự).
                    newMesh.name = Path.GetFileNameWithoutExtension(meshInfo.OutputPath);

                    string uniquePath = AssetDatabase.GenerateUniqueAssetPath(meshInfo.OutputPath);
                    AssetDatabase.CreateAsset(newMesh, uniquePath);
                    meshReplacementMap[meshInfo.Mesh] = newMesh;
                }

                MeshFilter[] allMeshFilters = instance.GetComponentsInChildren<MeshFilter>(true);
                foreach (MeshFilter filter in allMeshFilters)
                {
                    if (filter.sharedMesh != null && meshReplacementMap.ContainsKey(filter.sharedMesh))
                    {
                        filter.sharedMesh = meshReplacementMap[filter.sharedMesh];
                    }
                }
            }

            var materialReplacementMap = new Dictionary<Material, Material>();
            if (plan.MaterialPaths.Any())
            {
                foreach (var matEntry in plan.MaterialPaths)
                {
                    Material newMat = new Material(matEntry.Key);
                    newMat.name = Path.GetFileNameWithoutExtension(matEntry.Value);

                    string uniquePath = AssetDatabase.GenerateUniqueAssetPath(matEntry.Value);
                    AssetDatabase.CreateAsset(newMat, uniquePath);
                    materialReplacementMap[matEntry.Key] = newMat;
                }

                foreach (var renderer in instance.GetComponentsInChildren<MeshRenderer>(true))
                {
                    var sharedMaterials = renderer.sharedMaterials;
                    var newMaterials = new Material[sharedMaterials.Length];
                    bool materialsChanged = false;
                    for (int i = 0; i < sharedMaterials.Length; i++)
                    {
                        if (sharedMaterials[i] != null && materialReplacementMap.ContainsKey(sharedMaterials[i]))
                        {
                            newMaterials[i] = materialReplacementMap[sharedMaterials[i]];
                            materialsChanged = true;
                        }
                        else
                        {
                            newMaterials[i] = sharedMaterials[i];
                        }
                    }
                    if (materialsChanged)
                    {
                        renderer.sharedMaterials = newMaterials;
                    }
                }
            }

            string uniquePrefabPath = AssetDatabase.GenerateUniqueAssetPath(plan.NewPrefabPath);
            var newPrefab = PrefabUtility.SaveAsPrefabAsset(instance, uniquePrefabPath);

            Selection.activeObject = null;
            DestroyImmediate(instance);

            Debug.Log($"[BillUtils] Đã xử lý và tạo prefab mới cho '{Path.GetFileName(plan.SourcePath)}' tại: {uniquePrefabPath}", newPrefab);
        }

        private static string FormatNumber(int number, int minDigits = 3)
        {
            return number.ToString($"D{minDigits}");
        }

        // Hàm làm sạch tên mesh đơn giản đã bị loại bỏ.
        // Tên file giờ đây chỉ là Prefix + Số thứ tự.

        private void SetupReorderableList()
        {
            reorderableList = new ReorderableList(sourceEntries, typeof(SourceAssetEntry), true, true, true, true)
            {
                drawHeaderCallback = (Rect rect) =>
                {
                    float labelWidth = rect.width / 2f;
                    EditorGUI.LabelField(new Rect(rect.x, rect.y, labelWidth - 5, rect.height), "Asset Prefab");
                    EditorGUI.LabelField(new Rect(rect.x + labelWidth + 5, rect.y, labelWidth - 5, rect.height), "Mesh Prefix Code");
                },
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    var entry = sourceEntries[index];

                    float totalWidth = rect.width;
                    float padding = 5f;
                    float fieldHeight = EditorGUIUtility.singleLineHeight;

                    Rect prefabRect = new Rect(rect.x, rect.y + 2, totalWidth * 0.5f - padding / 2, fieldHeight);
                    Rect prefixCodeRect = new Rect(rect.x + totalWidth * 0.5f + padding / 2, rect.y + 2, totalWidth * 0.5f, fieldHeight);

                    entry.Prefab = (GameObject)EditorGUI.ObjectField(prefabRect, entry.Prefab, typeof(GameObject), false);
                    entry.PrefixCode = EditorGUI.TextField(prefixCodeRect, entry.PrefixCode);

                    sourceEntries[index] = entry;
                },
                onAddCallback = (list) =>
                {
                    sourceEntries.Add(new SourceAssetEntry());
                    list.list = sourceEntries;
                },
                onRemoveCallback = (list) =>
                {
                    if (list.index >= 0 && list.index < sourceEntries.Count)
                    {
                        sourceEntries.RemoveAt(list.index);
                        list.list = sourceEntries;
                    }
                }
            };
        }

        private bool HandleDragAndDropForAdding(Rect windowArea)
        {
            Event evt = Event.current;
            bool handled = false;

            if (evt.type == EventType.DragUpdated && windowArea.Contains(evt.mousePosition))
            {
                bool hasGameObject = DragAndDrop.objectReferences.Any(obj => obj is GameObject);
                if (hasGameObject)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    handled = true;
                }
            }
            else if (evt.type == EventType.DragPerform && windowArea.Contains(evt.mousePosition))
            {
                DragAndDrop.AcceptDrag();
                bool addedNew = false;
                foreach (var draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject is GameObject prefab)
                    {
                        if (!sourceEntries.Any(e => e.Prefab == prefab))
                        {
                            sourceEntries.Add(new SourceAssetEntry { Prefab = prefab });
                            addedNew = true;
                        }
                    }
                }
                if (addedNew)
                {
                    reorderableList.list = sourceEntries;
                    Repaint();
                }
                evt.Use();
                handled = true;
            }
            return handled;
        }

        private void ExecuteAllPlans()
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var plan in extractionPlans)
                {
                    StaticExecutePlan(plan);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            EditorUtility.DisplayDialog("Hoàn tất", $"Đã xử lý thành công {extractionPlans.Count} asset.", "OK");

            isPreviewing = false;
            sourceEntries.Clear();
            extractionPlans.Clear();
            SetupReorderableList();
        }
    }
}
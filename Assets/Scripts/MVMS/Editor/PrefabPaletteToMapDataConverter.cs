// Assets/Scripts/MVMS/Editor/PrefabPaletteToMapDataConverter.cs
// PHIÊN BẢN ĐÃ SỬA LỖI BIÊN DỊCH CS7036

using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using MVMS.Core;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MVMS.Editor
{
    public class PrefabPaletteToMapDataConverter : OdinEditorWindow
    {
        private const string TILE_DEFINITIONS_FOLDER = "TileDefinitions";

        [MenuItem("Assets/Create/MVMS/Create Map Data From Prefabs", false, 10)]
        private static void CreateMapFromPrefabsMenu()
        {
            var selectedPrefabs = Selection.GetFiltered<GameObject>(SelectionMode.Assets)
                                           .Where(go => PrefabUtility.IsPartOfPrefabAsset(go))
                                           .ToList();

            if (selectedPrefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("No Prefabs Selected", "Please select one or more prefab assets in the Project window.", "OK");
                return;
            }

            var window = GetWindow<PrefabPaletteToMapDataConverter>("Prefab to Map Converter");
            window.Initialize(selectedPrefabs);
        }

        [MenuItem("Assets/Create/MVMS/Create Map Data From Prefabs", true)]
        private static bool ValidateCreateMapFromPrefabsMenu()
        {
            return Selection.GetFiltered<GameObject>(SelectionMode.Assets).Any(go => PrefabUtility.IsPartOfPrefabAsset(go));
        }

        [Title("Input Prefabs", "These are the prefabs you selected.")]
        [ListDrawerSettings(IsReadOnly = true, ShowPaging = false, DraggableItems = false)]
        [ReadOnly]
        [PropertyOrder(-1)]
        private List<GameObject> sourcePrefabs;

        [Title("Output Configuration")]
        [FolderPath(AbsolutePath = false, RequireExistingPath = false)]
        [Required]
        public string SavePath = "Assets/GeneratedMaps";

        [Required("The name for the new Map Data asset is required.")]
        public string MapDataName = "NewGeneratedMap";

        [Title("Grid Layout Settings")]
        [InfoBox("The prefabs will be arranged in a grid within the new Map Data.")]
        [MinValue(1)]
        public int Columns = 8;

        public Vector3 CellSize = Vector3.one;

        private void Initialize(List<GameObject> prefabs)
        {
            sourcePrefabs = prefabs;
        }

        [Button(ButtonSizes.Large, Name = "Generate Map and Definitions")]
        [GUIColor(0.4f, 0.8f, 1f)]
        private void ProcessAndGenerateAssets()
        {
            if (!ValidateInput()) return;

            Directory.CreateDirectory(SavePath);
            string definitionsPath = Path.Combine(SavePath, TILE_DEFINITIONS_FOLDER);
            Directory.CreateDirectory(definitionsPath);

            var mapData = CreateMapDataAsset();
            var existingDefinitions = FindAllExistingDefinitions();

            for (int i = 0; i < sourcePrefabs.Count; i++)
            {
                GameObject prefab = sourcePrefabs[i];
                var tileDef = FindOrCreateTileDefinition(prefab, definitionsPath, existingDefinitions);

                int row = i / Columns;
                int col = i % Columns;
                Vector3Int gridPos = new Vector3Int(col, 0, row);

                // === DÒNG SỬA LỖI ===
                // Thêm tham số thứ 3: Quaternion.identity để cung cấp giá trị xoay mặc định.
                mapData.AddOrUpdateTile(gridPos, tileDef.TileID, Quaternion.identity);
            }

            int totalRows = Mathf.CeilToInt((float)sourcePrefabs.Count / Columns);
            mapData.MapDimensions = new Vector3Int(Columns, 1, totalRows);

            EditorUtility.SetDirty(mapData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"<color=green>Successfully generated '{MapDataName}.asset' with {sourcePrefabs.Count} tiles.</color>");
            EditorGUIUtility.PingObject(mapData);
            this.Close();
        }

        private MapData CreateMapDataAsset()
        {
            var mapData = ScriptableObject.CreateInstance<MapData>();
            mapData.CellSize = this.CellSize;
            string mapAssetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(SavePath, $"{MapDataName}.asset"));
            AssetDatabase.CreateAsset(mapData, mapAssetPath);
            return mapData;
        }

        private TileDefinition FindOrCreateTileDefinition(GameObject prefab, string newDefPath, Dictionary<GameObject, TileDefinition> existingDefs)
        {
            if (existingDefs.TryGetValue(prefab, out TileDefinition existingDef))
            {
                return existingDef;
            }

            var newDef = ScriptableObject.CreateInstance<TileDefinition>();
            newDef.Prefab = prefab;
            newDef.TileID = $"{prefab.name}_{GUID.Generate().ToString().Substring(0, 8)}";
            string defAssetPath = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(newDefPath, $"{prefab.name}.asset"));
            AssetDatabase.CreateAsset(newDef, defAssetPath);
            existingDefs.Add(prefab, newDef);
            return newDef;
        }

        private Dictionary<GameObject, TileDefinition> FindAllExistingDefinitions()
        {
            var definitions = new Dictionary<GameObject, TileDefinition>();
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(TileDefinition)}");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<TileDefinition>(path);
                if (definition != null && definition.Prefab != null && !definitions.ContainsKey(definition.Prefab))
                {
                    definitions.Add(definition.Prefab, definition);
                }
            }
            return definitions;
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(SavePath))
            {
                EditorUtility.DisplayDialog("Validation Error", "Save Path cannot be empty.", "OK");
                return false;
            }
            if (string.IsNullOrWhiteSpace(MapDataName))
            {
                EditorUtility.DisplayDialog("Validation Error", "Map Data Name cannot be empty.", "OK");
                return false;
            }
            return true;
        }
    }
}
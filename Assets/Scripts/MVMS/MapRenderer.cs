// Assets/Scripts/MVMS/MapRenderer.cs
// ÁP DỤNG ROTATION KHI VẼ OBJECT

using UnityEngine;
using Sirenix.OdinInspector;
using MVMS.Core;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MVMS.Rendering
{
    [AddComponentMenu("MVMS/Map Renderer")]
    [ExecuteInEditMode]
    public sealed class MapRenderer : MonoBehaviour
    {
        [Title("Data Source")]
        [Required("A Map Data asset is required to build the map.")]
        [OnValueChanged("RequestFullSync", true)]
        [InlineEditor(ObjectFieldMode = InlineEditorObjectFieldModes.Boxed)]
        public MapData MapDataSource;

        private Transform tileContainer;
        private readonly Dictionary<Vector3Int, GameObject> spawnedTiles = new Dictionary<Vector3Int, GameObject>();
        private readonly Dictionary<string, TileDefinition> tileDefinitionCache = new Dictionary<string, TileDefinition>();

        private bool fullSyncRequested = false;
        private const string TILE_CONTAINER_NAME = "[Generated] Map Tiles";

        private void SynchronizeMapWithData()
        {
            if (!ValidateState()) return;

            var dataTiles = MapDataSource.TileData;
            var tilesToSpawn = new HashSet<Vector3Int>(dataTiles.Keys);
            var tilesToDestroy = new List<Vector3Int>();

            foreach (var spawnedPair in spawnedTiles)
            {
                var gridPos = spawnedPair.Key;
                var spawnedObj = spawnedPair.Value;

                if (spawnedObj == null)
                {
                    tilesToDestroy.Add(gridPos);
                    continue;
                }

                if (dataTiles.TryGetValue(gridPos, out TileState dataTileState))
                {
                    // KIỂM TRA CẢ ID VÀ ROTATION
                    if (spawnedObj.name != dataTileState.TileID ||
                        spawnedObj.transform.localRotation != dataTileState.Rotation)
                    {
                        DestroyImmediate(spawnedObj);
                        tilesToDestroy.Add(gridPos);
                    }
                    else
                    {
                        tilesToSpawn.Remove(gridPos);
                    }
                }
                else
                {
                    DestroyImmediate(spawnedObj);
                    tilesToDestroy.Add(gridPos);
                }
            }

            foreach (var pos in tilesToDestroy)
            {
                spawnedTiles.Remove(pos);
            }

            foreach (var gridPos in tilesToSpawn)
            {
                // Lấy cả state thay vì chỉ ID
                if (dataTiles.TryGetValue(gridPos, out TileState tileState))
                {
                    SpawnTileAt(gridPos, tileState);
                }
            }
        }

        // Thay đổi signature để nhận TileState
        private void SpawnTileAt(Vector3Int gridPosition, TileState tileState)
        {
            if (spawnedTiles.ContainsKey(gridPosition) || string.IsNullOrEmpty(tileState.TileID)) return;

            if (tileDefinitionCache.TryGetValue(tileState.TileID, out TileDefinition definition) && definition.Prefab != null)
            {
                GameObject spawnedTile;
#if UNITY_EDITOR
                spawnedTile = (GameObject)PrefabUtility.InstantiatePrefab(definition.Prefab, tileContainer);
                Undo.RegisterCreatedObjectUndo(spawnedTile, "Spawn Tile");
#else
                spawnedTile = Instantiate(definition.Prefab, tileContainer);
#endif
                // Đặt tên theo ID để dễ dàng kiểm tra lại
                spawnedTile.name = tileState.TileID;

                Vector3 cornerPosition = Vector3.Scale(gridPosition, MapDataSource.CellSize);
                Vector3 centerPosition = cornerPosition + (MapDataSource.CellSize * 0.5f);
                spawnedTile.transform.localPosition = centerPosition;

                // ÁP DỤNG ROTATION ĐÃ LƯU
                spawnedTile.transform.localRotation = tileState.Rotation;

                spawnedTiles[gridPosition] = spawnedTile;
            }
        }

        // --- PHẦN CÒN LẠI CỦA FILE (GIỮ NGUYÊN) ---
        #region Unchanged Code
        private void OnEnable()
        {
            Initialize();
#if UNITY_EDITOR
            EditorApplication.update += EditorUpdate;
            Undo.undoRedoPerformed += OnUndoRedo;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
            Undo.undoRedoPerformed -= OnUndoRedo;
#endif
        }

        private void Initialize()
        {
            FindOrCreateTileContainer();
            PopulateDefinitionCache();
            RequestFullSync();
        }

        private void EditorUpdate()
        {
            if (fullSyncRequested)
            {
                SynchronizeMapWithData();
                fullSyncRequested = false;
            }
        }

        public void RequestFullSync()
        {
            if (!Application.isPlaying)
            {
                fullSyncRequested = true;
            }
        }

        private void OnUndoRedo()
        {
            PopulateDefinitionCache();
            RequestFullSync();
        }

        [Button(ButtonSizes.Large, Name = "Force Sync Map With Data"), PropertySpace(20), GUIColor(0.4f, 0.8f, 1f)]
        private void ForceSync()
        {
            Initialize();
        }

        private void FindOrCreateTileContainer()
        {
            var existingContainer = transform.Find(TILE_CONTAINER_NAME);
            if (existingContainer != null)
            {
                tileContainer = existingContainer;
            }
            else
            {
                tileContainer = new GameObject(TILE_CONTAINER_NAME).transform;
                tileContainer.SetParent(this.transform, false);
            }
            tileContainer.hideFlags = HideFlags.NotEditable | HideFlags.HideInHierarchy;
        }

        private void PopulateDefinitionCache()
        {
            tileDefinitionCache.Clear();
#if UNITY_EDITOR
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(TileDefinition)}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<TileDefinition>(path);
                if (definition != null && !string.IsNullOrEmpty(definition.TileID) && !tileDefinitionCache.ContainsKey(definition.TileID))
                {
                    tileDefinitionCache.Add(definition.TileID, definition);
                }
            }
#endif
        }

        private bool ValidateState(bool logErrors = true)
        {
            if (MapDataSource == null)
            {
                if (logErrors) Debug.LogError("Map Data Source is not assigned.", this);
                return false;
            }
            if (tileContainer == null)
            {
                FindOrCreateTileContainer();
            }
            return true;
        }
        #endregion
    }
}
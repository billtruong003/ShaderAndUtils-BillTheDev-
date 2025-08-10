// Assets/Scripts/MVMS/MapData.cs
// NÂNG CẤP ĐỂ LƯU TRỮ ROTATION

using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MVMS.Core
{
    /// <summary>
    /// Struct chứa thông tin trạng thái của một ô, bao gồm ID và hướng xoay.
    /// </summary>
    [System.Serializable]
    public struct TileState
    {
        public string TileID;
        public Quaternion Rotation;

        public static TileState Empty => new TileState { TileID = null, Rotation = Quaternion.identity };
    }

    [CreateAssetMenu(fileName = "NewMapData", menuName = "MVMS/Map Data")]
    public sealed class MapData : SerializedScriptableObject
    {
        [Title("Map Configuration")]
        [BoxGroup("Settings")]
        public Vector3Int MapDimensions = new Vector3Int(100, 20, 100);

        [BoxGroup("Settings")]
        public Vector3 CellSize = Vector3.one;

        [Title("Tile Data Store")]
        [InfoBox("This dictionary stores the state (ID and Rotation) of each tile at a specific grid coordinate.")]
        [DictionaryDrawerSettings(IsReadOnly = true, KeyLabel = "Grid Position", ValueLabel = "Tile State")]
        [ShowInInspector]
        private Dictionary<Vector3Int, TileState> tileData = new Dictionary<Vector3Int, TileState>();
        public IReadOnlyDictionary<Vector3Int, TileState> TileData => tileData;

        /// <summary>
        /// Thêm hoặc cập nhật một tile với thông tin xoay.
        /// </summary>
        public void AddOrUpdateTile(Vector3Int gridPosition, string tileID, Quaternion rotation)
        {
            if (!IsWithinBounds(gridPosition)) return;

            tileData[gridPosition] = new TileState { TileID = tileID, Rotation = rotation };
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
#endif
        }

        public void RemoveTile(Vector3Int gridPosition)
        {
            if (tileData.ContainsKey(gridPosition))
            {
                tileData.Remove(gridPosition);
#if UNITY_EDITOR
                EditorUtility.SetDirty(this);
#endif
            }
        }

        /// <summary>
        /// Lấy toàn bộ trạng thái của tile tại một vị trí.
        /// </summary>
        public TileState GetTileStateAt(Vector3Int gridPosition)
        {
            tileData.TryGetValue(gridPosition, out TileState state);
            return state; // Sẽ trả về TileState rỗng nếu không tìm thấy
        }

        public string GetTileIDAt(Vector3Int gridPosition)
        {
            return GetTileStateAt(gridPosition).TileID;
        }

        public bool IsWithinBounds(Vector3Int gridPosition)
        {
            return gridPosition.x >= 0 && gridPosition.x < MapDimensions.x &&
                   gridPosition.y >= 0 && gridPosition.y < MapDimensions.y &&
                   gridPosition.z >= 0 && gridPosition.z < MapDimensions.z;
        }

        [Button(ButtonSizes.Large, Name = "Clear All Map Data")]
        [GUIColor(1, 0.6f, 0.6f)]
        private void ClearAllData()
        {
#if UNITY_EDITOR
            if (EditorUtility.DisplayDialog("Clear All Map Data?",
                "This will erase all tile information stored in this asset. This action cannot be undone.",
                "Yes, Clear Everything", "Cancel"))
            {
                tileData.Clear();
                EditorUtility.SetDirty(this);
            }
#endif
        }
    }
}
using UnityEngine;
using Sirenix.OdinInspector;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MVMS.Core
{
    [CreateAssetMenu(fileName = "NewTileDefinition", menuName = "MVMS/Tile Definition")]
    public sealed class TileDefinition : ScriptableObject
    {
        [Title("Metadata", "Unique identifier and user-facing name for this tile.")]
        [InfoBox("The Tile ID must be unique across all tile definitions.", InfoMessageType.Warning, "@string.IsNullOrEmpty(TileID)")]
        [ValidateInput("ValidateIDIsNotEmpty", "Tile ID cannot be empty.")]
        public string TileID;

        [Title("Visuals")]
        [Required("A prefab must be assigned to be instantiated.")]
        [AssetsOnly]
        [PreviewField(ObjectFieldAlignment.Left, Height = 75)]
        public GameObject Prefab;

        [Title("Gameplay Properties")]
        public bool IsWalkable = true;
        public bool IsDestructible = false;

#if UNITY_EDITOR
        [Button(ButtonSizes.Medium, Name = "Generate Unique ID")]
        private void GenerateUniqueID()
        {
            if (string.IsNullOrEmpty(TileID))
            {
                TileID = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(this));
            }
            TileID = $"{TileID.Replace(" ", "")}_{GUID.Generate().ToString().Substring(0, 8)}";
            EditorUtility.SetDirty(this);
        }

        private bool ValidateIDIsNotEmpty(string id)
        {
            return !string.IsNullOrEmpty(id);
        }
#endif
    }
}
// Assets/Scripts/MVMS/Editor/MapEditorWindow.cs
// THÊM CÔNG CỤ ĐIỀU CHỈNH ROTATION

using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using MVMS.Core;
using MVMS.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace MVMS.Editor
{
    public class MapEditorWindow : OdinEditorWindow
    {
        public enum BrushMode { Paint, Erase }

        // --- CORE REFERENCES ---
        [Title("Active Map & Renderer")]
        [InfoBox("Select a MapRenderer in the scene to enable live editing.", InfoMessageType.Info, "@ActiveMapRenderer == null")]
        [SceneObjectsOnly, Required, OnValueChanged("OnRendererChanged"), PropertyOrder(-10)]
        public MapRenderer ActiveMapRenderer;

        [ShowInInspector, ReadOnly, PropertyOrder(-9), LabelText("Active Map Data")]
        public MapData CurrentMapData => ActiveMapRenderer?.MapDataSource;

        // --- TOOLBOX ---
        [Title("Toolbox")]
        [EnumToggleButtons, HideLabel, PropertyOrder(-8)]
        public BrushMode currentBrushMode = BrushMode.Paint;

        [ShowIf("currentBrushMode", BrushMode.Paint)]
        [Title("Selected Tile"), PropertyOrder(-7)]
        [ShowInInspector, ReadOnly, InlineEditor(InlineEditorModes.SmallPreview)]
        public TileDefinition SelectedTile { get; private set; }

        // --- CÔNG CỤ XOAY MỚI ---
        [Title("Brush Rotation")]
        [ShowIf("currentBrushMode", BrushMode.Paint)]
        [PropertyOrder(-6)]
        [InlineButton("RotateY90", "Rotate Y 90°")]
        public Vector3 BrushEulerRotation;
        private void RotateY90() { BrushEulerRotation.y = Mathf.Repeat(BrushEulerRotation.y + 90, 360); }

        // --- TILE PALETTE (Không thay đổi) ---
        private List<TileDefinition> fullTilePalette = new List<TileDefinition>();
        // ... (Các biến khác của palette)

        // ... (Các thiết lập khác)

        // Các hàm xử lý sự kiện và logic vẽ chính
        private void ProcessBrushAction(Vector3Int gridPos, bool isClick)
        {
            if (!CurrentMapData.IsWithinBounds(gridPos)) return;

            Undo.RecordObject(CurrentMapData, "MVMS Brush Action");
            bool dataChanged = false;

            if (currentBrushMode == BrushMode.Paint)
            {
                if (SelectedTile == null) return;

                var currentState = CurrentMapData.GetTileStateAt(gridPos);
                var targetRotation = Quaternion.Euler(BrushEulerRotation);

                bool isSameTile = currentState.TileID == SelectedTile.TileID;
                bool isSameRotation = currentState.Rotation == targetRotation;

                if (isClick && ClickToToggle && isSameTile && isSameRotation)
                {
                    CurrentMapData.RemoveTile(gridPos);
                    dataChanged = true;
                }
                else if (!isSameTile || !isSameRotation) // Nếu khác tile hoặc khác rotation -> vẽ lại
                {
                    CurrentMapData.AddOrUpdateTile(gridPos, SelectedTile.TileID, targetRotation);
                    dataChanged = true;
                }
            }
            else if (currentBrushMode == BrushMode.Erase)
            {
                if (!string.IsNullOrEmpty(CurrentMapData.GetTileIDAt(gridPos)))
                {
                    CurrentMapData.RemoveTile(gridPos);
                    dataChanged = true;
                }
            }

            if (dataChanged)
            {
                if (ActiveMapRenderer != null) ActiveMapRenderer.RequestFullSync();
                EditorUtility.SetDirty(CurrentMapData);
            }
        }

        private void DrawGridAndPreview(Vector3Int gridPos)
        {
            if (ShowGrid)
            {
                // ... (code vẽ lưới không đổi)
            }

            bool isWithinBounds = CurrentMapData.IsWithinBounds(gridPos);
            Color previewColor = isWithinBounds ? (currentBrushMode == BrushMode.Paint ? Color.green : Color.red) : Color.gray;
            Vector3 cornerPosition = GridToWorld(gridPos);
            Vector3 cellCenter = cornerPosition + (CurrentMapData.CellSize * 0.5f);
            Handles.color = previewColor;

            // Cập nhật preview để hiển thị cả hướng xoay
            Handles.matrix = Matrix4x4.TRS(cellCenter, Quaternion.Euler(BrushEulerRotation), CurrentMapData.CellSize);
            Handles.DrawWireCube(Vector3.zero, Vector3.one);
            Handles.matrix = Matrix4x4.identity; // Reset matrix
        }

        // --- PHẦN CÒN LẠI CỦA FILE (GIỮ NGUYÊN) ---
        // (Bao gồm các hàm OnEnable, OnGUI, DrawPalette, Pagination, v.v...)
        #region Unchanged Code
        private List<TileDefinition> filteredPalette = new List<TileDefinition>();
        private Dictionary<TileDefinition, Texture2D> previewCache = new Dictionary<TileDefinition, Texture2D>();
        private string searchQuery = "";
        private Vector2 paletteScrollPosition;
        private int currentPage = 0;
        private const int ITEMS_PER_PAGE = 30;

        [Title("Brush & Grid Settings"), PropertyOrder(1)]
        [ToggleLeft]
        [Tooltip("When painting, a single click on a tile of the same type will erase it. Dragging will always paint over.")]
        public bool ClickToToggle = true;

        [HorizontalGroup("YLevelControls", 100), GUIColor(0.7f, 1f, 0.7f)]
        public int CurrentYLevel = 0;

        [HorizontalGroup("YLevelControls"), Button("▲", ButtonSizes.Small), GUIColor(0.8f, 1f, 0.8f)]
        private void IncreaseYLevel() => CurrentYLevel++;
        [HorizontalGroup("YLevelControls"), Button("▼", ButtonSizes.Small), GUIColor(1f, 0.8f, 0.8f)]
        private void DecreaseYLevel() => CurrentYLevel--;

        [ToggleLeft] public bool ShowGrid = true;
        [ShowIf("ShowGrid"), Range(5, 50)] public int GridDrawRange = 20;

        private Vector3Int lastActionGridPosition;
        private bool isDragging;

        [MenuItem("Tools/MVMS/Map Editor")]
        private static void OpenWindow() => GetWindow<MapEditorWindow>("Map Editor").Show();

        protected override void OnEnable()
        {
            base.OnEnable();
            SceneView.duringSceneGui += OnSceneGUI;
            if (ActiveMapRenderer == null) ActiveMapRenderer = FindFirstObjectByType<MapRenderer>();
            Undo.undoRedoPerformed += OnUndoRedo;
            RefreshPalette();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            SceneView.duringSceneGui -= OnSceneGUI;
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        protected override void OnImGUI()
        {
            base.OnImGUI();
            if (currentBrushMode == BrushMode.Paint)
            {
                DrawOptimizedPaletteGUI();
            }
        }

        [Button(ButtonSizes.Large, Name = "Refresh Tile Palette"), PropertyOrder(-5)]
        private void RefreshPalette()
        {
            fullTilePalette.Clear();
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(TileDefinition)}");
            foreach (string guid in guids)
            {
                var def = AssetDatabase.LoadAssetAtPath<TileDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (def != null && def.Prefab != null)
                {
                    fullTilePalette.Add(def);
                }
            }
            fullTilePalette = fullTilePalette.OrderBy(def => def.name).ToList();
            ApplyFilterAndPagination();
        }

        private void ApplyFilterAndPagination()
        {
            IEnumerable<TileDefinition> palette = fullTilePalette;
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                palette = palette.Where(def => def.name.ToLower().Contains(searchQuery.ToLower()));
            }
            filteredPalette = palette.ToList();
            Repaint();
        }

        private void DrawOptimizedPaletteGUI()
        {
            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            string newSearchQuery = EditorGUILayout.TextField(searchQuery);
            if (newSearchQuery != searchQuery)
            {
                searchQuery = newSearchQuery;
                currentPage = 0;
                ApplyFilterAndPagination();
            }
            EditorGUILayout.EndHorizontal();

            paletteScrollPosition = EditorGUILayout.BeginScrollView(paletteScrollPosition, GUILayout.MinHeight(150), GUILayout.MaxHeight(400));
            if (filteredPalette.Count == 0)
            {
                EditorGUILayout.HelpBox("No tiles match your search or found in project.", MessageType.Info);
            }
            else
            {
                int startIndex = currentPage * ITEMS_PER_PAGE;
                int endIndex = Mathf.Min(startIndex + ITEMS_PER_PAGE, filteredPalette.Count);

                float availableWidth = EditorGUIUtility.currentViewWidth - 40;
                int columnCount = Mathf.Max(1, Mathf.FloorToInt(availableWidth / 72));
                int currentColumn = 0;

                EditorGUILayout.BeginHorizontal();
                for (int i = startIndex; i < endIndex; i++)
                {
                    var def = filteredPalette[i];
                    if (currentColumn > 0 && currentColumn % columnCount == 0)
                    {
                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        currentColumn = 0;
                    }

                    previewCache.TryGetValue(def, out Texture2D preview);
                    if (preview == null)
                    {
                        Texture2D loadedPreview = AssetPreview.GetAssetPreview(def.Prefab);
                        if (loadedPreview != null)
                        {
                            preview = loadedPreview;
                            previewCache[def] = preview;
                            Repaint();
                        }
                    }

                    GUIContent buttonContent = new GUIContent(preview, def.name);
                    Color originalColor = GUI.backgroundColor;
                    if (SelectedTile == def) GUI.backgroundColor = Color.cyan;

                    if (GUILayout.Button(buttonContent, GUILayout.Width(64), GUILayout.Height(64)))
                    {
                        SelectedTile = def;
                        Repaint();
                    }

                    GUI.backgroundColor = originalColor;
                    currentColumn++;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            DrawPaginationControls();
        }

        private void DrawPaginationControls()
        {
            if (filteredPalette.Count <= ITEMS_PER_PAGE) return;
            int totalPages = Mathf.CeilToInt((float)filteredPalette.Count / ITEMS_PER_PAGE);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.enabled = currentPage > 0;
            if (GUILayout.Button("<< First")) { currentPage = 0; }
            if (GUILayout.Button("< Prev")) { currentPage--; }
            GUI.enabled = true;
            GUILayout.Label($"Page {currentPage + 1} / {totalPages}", GUILayout.Width(80), GUILayout.ExpandWidth(false));
            GUI.enabled = currentPage < totalPages - 1;
            if (GUILayout.Button("Next >")) { currentPage++; }
            if (GUILayout.Button("Last >>")) { currentPage = totalPages - 1; }
            GUI.enabled = true;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void OnRendererChanged()
        {
            if (ActiveMapRenderer != null) ActiveMapRenderer.RequestFullSync();
            Repaint();
        }

        private void OnUndoRedo()
        {
            if (ActiveMapRenderer != null) ActiveMapRenderer.RequestFullSync();
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (CurrentMapData == null || ActiveMapRenderer == null) return;
            Event e = Event.current;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            Plane drawingPlane = new Plane(Vector3.up, new Vector3(0, CurrentYLevel * CurrentMapData.CellSize.y, 0));
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (drawingPlane.Raycast(ray, out float enter))
            {
                Vector3Int gridPos = WorldToGrid(ray.GetPoint(enter));
                DrawGridAndPreview(gridPos);
                if (!e.alt) HandleMouseEvents(e, gridPos);
            }
            sceneView.Repaint();
        }

        private void HandleMouseEvents(Event e, Vector3Int gridPos)
        {
            switch (e.type)
            {
                case EventType.MouseDown when e.button == 0:
                    isDragging = false;
                    lastActionGridPosition = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
                    e.Use();
                    break;
                case EventType.MouseUp when e.button == 0:
                    if (!isDragging) ProcessBrushAction(gridPos, isClick: true);
                    e.Use();
                    break;
                case EventType.MouseDrag when e.button == 0:
                    isDragging = true;
                    if (gridPos != lastActionGridPosition)
                    {
                        ProcessBrushAction(gridPos, isClick: false);
                        lastActionGridPosition = gridPos;
                    }
                    e.Use();
                    break;
            }
        }

        private Vector3Int WorldToGrid(Vector3 worldPosition) => new Vector3Int(Mathf.FloorToInt(worldPosition.x / CurrentMapData.CellSize.x), CurrentYLevel, Mathf.FloorToInt(worldPosition.z / CurrentMapData.CellSize.z));
        private Vector3 GridToWorld(Vector3Int gridPosition) => Vector3.Scale(gridPosition, CurrentMapData.CellSize);
        #endregion
    }
}
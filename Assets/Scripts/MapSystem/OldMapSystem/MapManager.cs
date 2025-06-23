using UnityEngine;
using System.Collections.Generic;
using System.IO;
using Sirenix.OdinInspector;

public class MapManager : MonoBehaviour
{
    [Header("Tham chiếu chung")]
    [Tooltip("Transform của người chơi để xác định vị trí")]
    public Transform playerTransform;
    [Tooltip("Texture hình bút vẽ (màu đen trên nền trong suốt) để lộ bản đồ")]
    public Texture2D brushTexture;
    [Tooltip("Camera riêng để render bản đồ")]
    public Camera minimapCamera;

    [Header("Cài đặt chung")]
    [Tooltip("Layer chứa các đối tượng Map Plane để Raycast chỉ tìm chúng")]
    public LayerMask mapLayerMask;
    [Tooltip("Kích thước của bút vẽ khi khám phá bản đồ")]
    public float brushSize = 50f;
    [Tooltip("Độ phân giải của texture bản đồ che (nên là số mũ của 2, vd: 512, 1024)")]
    public int mapResolution = 1024;
    [Tooltip("Tần suất vẽ lên bản đồ (giây) để tối ưu hiệu suất")]
    public float paintInterval = 0.1f;

    [Header("Hiển thị nhân vật")]
    [Tooltip("Chọn cách hiển thị nhân vật trên bản đồ")]
    public MapDisplayMode displayMode = MapDisplayMode.Canvas2D;
    [ShowIf("displayMode", MapDisplayMode.Canvas2D)]
    [Tooltip("Icon nhân vật cho minimap 2D")]
    public Texture2D playerIcon;
    [ShowIf("displayMode", MapDisplayMode.WorldSpace)]
    [Tooltip("GameObject đại diện nhân vật trên bản đồ world space")]
    public GameObject playerMarker;

    [Header("Quản lý các tầng")]
    [Tooltip("Danh sách thông tin của tất cả các tầng trong màn chơi")]
    public List<MapLevelInfo> mapLevels;

    private int currentLevelIndex = 0;
    private float lastPaintTime = 0f;
    private Vector2 lastPlayerUV = Vector2.zero;
    private Dictionary<MapLevelInfo, float> unExploredPixels = new Dictionary<MapLevelInfo, float>();

    public enum MapDisplayMode { Canvas2D, WorldSpace }

    [Button("Reset Tiến độ Khám Phá")]
    private void ResetAllMaps()
    {
        foreach (var level in mapLevels)
        {
            Texture2D initialPaintMap = level.mapMaterial.GetTexture("_PaintMap") as Texture2D;
            if (initialPaintMap == null)
            {
                Debug.LogError("Initial PaintMap texture is null! Assign a red/white texture to _PaintMap in the material.");
                continue;
            }
            Graphics.Blit(initialPaintMap, level.paintMap);
            CountUnexploredPixels(level);

            string filePath = Path.Combine(Application.persistentDataPath, $"map_level_{mapLevels.IndexOf(level)}.png");
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        Debug.Log("Đã reset tiến độ khám phá tất cả các tầng.");
    }

    void Start()
    {
        if (!ValidateSetup())
            return;

        initializeMapLevels();
        ResetAllMaps();
        LoadAllMaps();
        SwitchLevel(0);
    }

    void Update()
    {
        if (Time.time >= lastPaintTime + paintInterval)
        {
            RaycastToMap();
            UpdatePlayerPosition();
            lastPaintTime = Time.time;
        }
    }

    private bool ValidateSetup()
    {
        if (minimapCamera == null || playerTransform == null || brushTexture == null)
        {
            Debug.LogError("Thiếu tham chiếu: " + (minimapCamera == null ? "MinimapCamera" : playerTransform == null ? "PlayerTransform" : "BrushTexture"));
            return false;
        }
        if (displayMode == MapDisplayMode.Canvas2D && playerIcon == null)
            Debug.LogWarning("Chưa gán PlayerIcon cho Canvas2D!");
        if (displayMode == MapDisplayMode.WorldSpace && playerMarker == null)
            Debug.LogWarning("Chưa gán PlayerMarker cho WorldSpace!");
        return true;
    }

    private void initializeMapLevels()
    {
        foreach (var level in mapLevels)
        {
            level.paintMap = new RenderTexture(mapResolution, mapResolution, 0, RenderTextureFormat.ARGB32);
            level.mapMaterial.SetTexture("_PaintMap", level.paintMap);
            if (displayMode == MapDisplayMode.Canvas2D && playerIcon != null)
                level.mapMaterial.SetTexture("_PlayerIcon", playerIcon);
        }
    }

    private void RaycastToMap()
    {
        Vector3 startRay = playerTransform.position + Vector3.up * 50;
        if (Physics.Raycast(startRay, Vector3.down, out RaycastHit hit, 100f, mapLayerMask))
        {
            if (hit.collider.gameObject == mapLevels[currentLevelIndex].mapPlaneObject)
            {
                lastPlayerUV = hit.textureCoord;
                PaintOnMap(hit.textureCoord);
            }
            else
            {
                Debug.LogWarning($"Raycast trúng {hit.collider.name}, mong đợi {mapLevels[currentLevelIndex].mapPlaneObject.name}");
            }
        }
    }

    private void PaintOnMap(Vector2 uvCoords)
    {
        RenderTexture.active = mapLevels[currentLevelIndex].paintMap;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, mapResolution, mapResolution, 0);

        float drawX = uvCoords.x * mapResolution - brushSize / 2f;
        float drawY = (1 - uvCoords.y) * mapResolution - brushSize / 2f;
        Graphics.DrawTexture(new Rect(drawX, drawY, brushSize, brushSize), brushTexture);

        GL.PopMatrix();
        RenderTexture.active = null;
    }

    private void UpdatePlayerPosition()
    {
        MapLevelInfo currentLevel = mapLevels[currentLevelIndex];
        if (displayMode == MapDisplayMode.Canvas2D)
        {
            currentLevel.mapMaterial.SetVector("_PlayerPosition", lastPlayerUV);
        }
        else if (displayMode == MapDisplayMode.WorldSpace && playerMarker != null)
        {
            Vector3 planePos = currentLevel.mapPlaneObject.transform.position;
            Vector3 worldPos = new Vector3(
                planePos.x + (lastPlayerUV.x - 0.5f) * 10f, // Giả sử Plane 10x10
                planePos.y + 0.1f,
                planePos.z + (0.5f - lastPlayerUV.y) * 10f
            );
            playerMarker.transform.position = worldPos;
        }
    }

    public void SwitchLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= mapLevels.Count)
        {
            Debug.LogError($"Không thể chuyển đến tầng {levelIndex}!");
            return;
        }

        currentLevelIndex = levelIndex;
        MapLevelInfo currentLevel = mapLevels[currentLevelIndex];
        minimapCamera.transform.position = currentLevel.mapPlaneObject.transform.position + currentLevel.cameraOffset;
        UpdatePlayerPosition();
    }

    [Button("Tính Phần Trăm Khám Phá")]
    public float GetExploredPercentage()
    {
        MapLevelInfo level = mapLevels[currentLevelIndex];
        Texture2D tempTex = new Texture2D(mapResolution, mapResolution, TextureFormat.ARGB32, false);
        RenderTexture.active = level.paintMap;
        tempTex.ReadPixels(new Rect(0, 0, mapResolution, mapResolution), 0, 0);
        tempTex.Apply();
        RenderTexture.active = null;

        float mapExplored = 0;
        Color[] pixels = tempTex.GetPixels();
        foreach (Color pixel in pixels)
        {
            if (pixel.r > 0.5f) // Red pixels
                mapExplored++;
        }
        Destroy(tempTex);

        float unExplored = unExploredPixels.ContainsKey(level) ? unExploredPixels[level] : CountUnexploredPixels(level);
        float percentage = (1 - (mapExplored / unExplored)) * 100;
        Debug.Log($"Tầng {level.levelName}: {percentage:F2}% khám phá");
        return percentage;
    }

    private float CountUnexploredPixels(MapLevelInfo level)
    {
        Texture2D initialPaintMap = level.mapMaterial.GetTexture("_PaintMap") as Texture2D;
        if (initialPaintMap == null)
        {
            Debug.LogError("Không thể đếm pixel chưa khám phá: PaintMap null!");
            return 0;
        }

        Color[] pixels = initialPaintMap.GetPixels();
        float unExplored = 0;
        foreach (Color pixel in pixels)
        {
            if (pixel.r > 0.5f) // Red pixels
                unExplored++;
        }
        unExploredPixels[level] = unExplored;
        return unExplored;
    }

    public void SaveAllMaps()
    {
        for (int i = 0; i < mapLevels.Count; i++)
            SaveMap(mapLevels[i], i);
        Debug.Log("Đã lưu tất cả các tầng.");
    }

    public void LoadAllMaps()
    {
        for (int i = 0; i < mapLevels.Count; i++)
            LoadMap(mapLevels[i], i);
        Debug.Log("Đã tải tất cả các tầng.");
    }

    private void SaveMap(MapLevelInfo level, int levelIndex)
    {
        Texture2D tempTex = new Texture2D(mapResolution, mapResolution, TextureFormat.ARGB32, false);
        RenderTexture.active = level.paintMap;
        tempTex.ReadPixels(new Rect(0, 0, mapResolution, mapResolution), 0, 0);
        tempTex.Apply();
        RenderTexture.active = null;

        byte[] mapData = tempTex.EncodeToPNG();
        Destroy(tempTex);

        string filePath = Path.Combine(Application.persistentDataPath, $"map_level_{levelIndex}.png");
        File.WriteAllBytes(filePath, mapData);
    }

    private void LoadMap(MapLevelInfo level, int levelIndex)
    {
        string filePath = Path.Combine(Application.persistentDataPath, $"map_level_{levelIndex}.png");
        if (File.Exists(filePath))
        {
            byte[] mapData = File.ReadAllBytes(filePath);
            Texture2D tempTex = new Texture2D(2, 2);
            tempTex.LoadImage(mapData);
            Graphics.Blit(tempTex, level.paintMap);
            Destroy(tempTex);
        }
    }

    private void OnApplicationQuit()
    {
        SaveAllMaps();
    }
}
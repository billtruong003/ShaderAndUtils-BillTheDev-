using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using Utils.Bill.InspectorCustom;

[RequireComponent(typeof(BoxCollider))]
public class MinimapController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IScrollHandler
{
    // ... (Các khai báo Enum và biến Public giữ nguyên y hệt) ...
    #region Public Variables
    public enum InteractionMode
    {
        WorldSpace_Physics,
        ScreenSpace_UIEvents
    }

    [CustomHeader("Interaction Mode", "#8E24AA")]
    public InteractionMode interactionMode = InteractionMode.ScreenSpace_UIEvents;

    public static List<MinimapTrackable> TrackedObjects = new List<MinimapTrackable>();
    public static void AddTrackable(MinimapTrackable trackable) { if (!TrackedObjects.Contains(trackable)) TrackedObjects.Add(trackable); }
    public static void RemoveTrackable(MinimapTrackable trackable) { if (TrackedObjects.Contains(trackable)) TrackedObjects.Remove(trackable); }

    [CustomHeader("Core System References", "#00B0FF")]
    public Transform playerTransform;
    public RawImage mapDisplayImage;
    public RectTransform playerIcon;

    [CustomHeader("Object Tracking & Pooling", "#00E676")]
    public RectTransform iconsContainer;
    public GameObject iconPrefab;

    [CustomHeader("Map Data Configuration", "#FFD600")]
    public MapAreaData[] mapAreas;
    public string startingAreaName;

    [CustomHeader("Gameplay Settings", "#FF9100")]
    [CustomSlider(0.01f, 1f)] public float zoomLevel = 0.5f;
    [Tooltip("Mức zoom mặc định khi reset.")]
    public float defaultZoomLevel = 0.5f;

    [CustomHeader("Visuals: Sight Cone", "#E040FB")]
    [ColorUsage(true, true)] public Color sightConeColor = new Color(1, 1, 0, 0.25f);
    [CustomSlider(10, 180)] public float sightConeAngle = 90f;
    [CustomSlider(0.01f, 1.0f)] public float sightConeSoftness = 0.1f;

    [CustomHeader("Edit Mode Settings", "#D500F9")]
    public float autoDisableTime = 5.0f;
    public float zoomSpeed = 0.05f;
    public float buttonZoomStep = 0.1f;
    public float minZoom = 0.05f;
    public float maxZoom = 1.0f;
    public string fingerTag = "PlayerFinger";
    #endregion

    private Material minimapMaterial;
    private MapAreaData currentMapArea;
    private Vector2 currentWorldSize;
    private List<RectTransform> iconPool = new List<RectTransform>();

    private bool isPanModeEnabled = false;
    private float timeSinceLastInteraction = 0f;
    private Vector2 panOffset = Vector2.zero;

    // --- BIẾN CHO LOGIC "MỎ NEO" ---
    private Vector2 panOffsetOnDown;
    private Vector2 pointerDownLocalPosition;

    private Transform fingerTransform;
    private bool isInteracting = false;
    private Camera mainCameraForRaycast;


    #region Public Control Methods
    public void ZoomIn() { AdjustZoomByAmount(-buttonZoomStep); }
    public void ZoomOut() { AdjustZoomByAmount(buttonZoomStep); }
    public void ResetView() { DisablePanMode(); zoomLevel = defaultZoomLevel; }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        var col = GetComponent<BoxCollider>();
        if (interactionMode == InteractionMode.WorldSpace_Physics)
        {
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }
        else if (col != null) { col.enabled = false; }
    }

    void Start()
    {
        if (!ValidateSetup()) { this.enabled = false; return; }
        minimapMaterial = new Material(mapDisplayImage.material);
        mapDisplayImage.material = minimapMaterial;
        SwitchMapArea(startingAreaName);
        if (iconPrefab != null) { iconPrefab.SetActive(false); }
        mainCameraForRaycast = Camera.main;
    }

    void OnDisable() { DisablePanMode(); }

    void LateUpdate()
    {
        if (minimapMaterial == null || currentMapArea == null || playerTransform == null) return;

#if UNITY_EDITOR
        if (interactionMode == InteractionMode.WorldSpace_Physics) HandleEditorMouseInput();
#endif

        if (isPanModeEnabled && !isInteracting)
        {
            timeSinceLastInteraction += Time.deltaTime;
            if (timeSinceLastInteraction > autoDisableTime) DisablePanMode();
        }

        if (interactionMode == InteractionMode.WorldSpace_Physics) UpdateVRPan();

        UpdateShaderProperties();
        UpdateAllIcons();
    }
    #endregion

    // ... (Các hàm UpdateAllIcons, UpdateShaderProperties giữ nguyên) ...
    #region Core Update Logic
    void UpdateAllIcons()
    {
        if (mapDisplayImage == null) return;

        float mapRadiusUI = mapDisplayImage.rectTransform.rect.width / 2f;
        Vector2 safeWorldSize = new Vector2(Mathf.Abs(currentWorldSize.x), Mathf.Abs(currentWorldSize.y));
        if (safeWorldSize.x == 0 || safeWorldSize.y == 0) return;

        Vector2 playerUV = (new Vector2(playerTransform.position.x, playerTransform.position.z) - currentMapArea.worldMinBounds) / safeWorldSize;
        Vector2 viewCenterUV = playerUV - panOffset;
        Vector3 viewCenterWorldPos = new Vector3(
            viewCenterUV.x * safeWorldSize.x + currentMapArea.worldMinBounds.x,
            playerTransform.position.y,
            viewCenterUV.y * safeWorldSize.y + currentMapArea.worldMinBounds.y
        );

        float worldRadiusOnMap = ((safeWorldSize.x + safeWorldSize.y) / 2f) * zoomLevel;
        if (worldRadiusOnMap <= 0) return;
        float scale = mapRadiusUI / worldRadiusOnMap;

        if (playerIcon != null)
        {
            Vector3 playerDiff = playerTransform.position - viewCenterWorldPos;
            Vector2 playerIconPos = new Vector2(playerDiff.x, playerDiff.z) * scale;

            playerIcon.anchoredPosition = playerIconPos;

            bool shouldBeActive = playerIcon.anchoredPosition.magnitude <= mapRadiusUI;
            if (playerIcon.gameObject.activeSelf != shouldBeActive)
            {
                playerIcon.gameObject.SetActive(shouldBeActive);
            }
        }

        if (iconsContainer != null && iconPrefab != null)
        {
            foreach (var icon in iconPool) { if (icon.gameObject.activeSelf) icon.gameObject.SetActive(false); }

            int poolIndex = 0;
            foreach (var trackable in TrackedObjects)
            {
                if (trackable.transform == playerTransform) continue;

                Vector3 trackableDiff = trackable.transform.position - viewCenterWorldPos;
                Vector2 finalIconPos = new Vector2(trackableDiff.x, trackableDiff.z) * scale;

                if (finalIconPos.magnitude < mapRadiusUI)
                {
                    RectTransform iconRect;
                    if (poolIndex < iconPool.Count) { iconRect = iconPool[poolIndex]; }
                    else
                    {
                        GameObject newIconObj = Instantiate(iconPrefab, iconsContainer);
                        iconRect = newIconObj.GetComponent<RectTransform>();
                        iconPool.Add(iconRect);
                    }
                    iconRect.gameObject.SetActive(true);
                    iconRect.anchoredPosition = finalIconPos;
                    Image img = iconRect.GetComponent<Image>();
                    img.sprite = trackable.iconSprite;
                    img.color = trackable.iconColor;
                    iconRect.sizeDelta = new Vector2(trackable.iconSize, trackable.iconSize);
                    poolIndex++;
                }
            }
        }
    }

    void UpdateShaderProperties()
    {
        Vector3 playerWorldPos = playerTransform.position;
        Vector2 playerPos2D = new Vector2(playerWorldPos.x, playerWorldPos.z);
        Vector2 safeWorldSize = new Vector2(Mathf.Abs(currentWorldSize.x), Mathf.Abs(currentWorldSize.y));
        if (safeWorldSize.x == 0 || safeWorldSize.y == 0) return;

        Vector2 normalizedPos = (playerPos2D - currentMapArea.worldMinBounds) / safeWorldSize;

        minimapMaterial.SetVector("_PlayerPosUV", normalizedPos - panOffset);
        minimapMaterial.SetFloat("_ZoomLevel", zoomLevel);

        Vector3 forward = playerTransform.forward;
        Vector2 forward2D = new Vector2(forward.x, forward.z).normalized;
        minimapMaterial.SetVector("_PlayerForward", forward2D);
        minimapMaterial.SetColor("_SightConeColor", sightConeColor);
        minimapMaterial.SetFloat("_SightConeAngle", sightConeAngle);
        minimapMaterial.SetFloat("_SightConeSoftness", sightConeSoftness);

        if (playerIcon != null)
        {
            Vector2 playerIconUV = new Vector2(
                (playerIcon.anchoredPosition.x / mapDisplayImage.rectTransform.rect.width) + 0.5f,
                (playerIcon.anchoredPosition.y / mapDisplayImage.rectTransform.rect.height) + 0.5f
            );
            minimapMaterial.SetVector("_PlayerIconScreenUV", playerIconUV);
        }
    }
    #endregion

    #region Interaction Handlers & Helper Functions
    void EnablePanMode()
    {
        if (isPanModeEnabled) { timeSinceLastInteraction = 0f; return; }
        isPanModeEnabled = true;
        timeSinceLastInteraction = 0f;
    }

    void DisablePanMode()
    {
        if (!isPanModeEnabled) return;
        isPanModeEnabled = false;
        panOffset = Vector2.zero;
        isInteracting = false;
    }

    void AdjustZoomByAmount(float amount)
    {
        if (!isPanModeEnabled) EnablePanMode();
        zoomLevel = Mathf.Clamp(zoomLevel + amount, minZoom, maxZoom);
        timeSinceLastInteraction = 0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (interactionMode != InteractionMode.ScreenSpace_UIEvents) return;
        isInteracting = true;
        EnablePanMode();

        // --- LOGIC "THẢ NEO" ---
        // Ghi lại vị trí offset và vị trí nhấn chuột ban đầu
        panOffsetOnDown = panOffset;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapDisplayImage.rectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out pointerDownLocalPosition
        );
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (interactionMode != InteractionMode.ScreenSpace_UIEvents || !isInteracting) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapDisplayImage.rectTransform,
            eventData.position,
            eventData.enterEventCamera, // Dùng eventData.camera cho OnDrag để ổn định hơn
            out Vector2 currentLocalPosition))
        {
            // --- LOGIC PANNING MỚI ---
            // 1. Tính tổng quãng đường di chuyển từ lúc nhấn chuột
            Vector2 totalDelta = currentLocalPosition - pointerDownLocalPosition;

            // 2. Chuyển đổi tổng quãng đường đó thành offset UV
            float conversionFactor = zoomLevel / mapDisplayImage.rectTransform.rect.width;
            Vector2 panDeltaFromStart = totalDelta * conversionFactor;

            // 3. Đặt panOffset mới bằng offset lúc "thả neo" + delta từ đầu
            panOffset = panOffsetOnDown + panDeltaFromStart;

            timeSinceLastInteraction = 0f;
        }
    }

    public void OnPointerUp(PointerEventData eventData) { if (interactionMode == InteractionMode.ScreenSpace_UIEvents) isInteracting = false; }
    public void OnScroll(PointerEventData eventData) { if (interactionMode == InteractionMode.ScreenSpace_UIEvents) AdjustZoomByAmount(-eventData.scrollDelta.y * zoomSpeed); }
    private void OnTriggerEnter(Collider other)
    {
        if (interactionMode == InteractionMode.WorldSpace_Physics && !string.IsNullOrEmpty(fingerTag) && other.CompareTag(fingerTag))
        {
            fingerTransform = other.transform;
            isInteracting = true;
            EnablePanMode();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (interactionMode == InteractionMode.WorldSpace_Physics && !string.IsNullOrEmpty(fingerTag) && other.CompareTag(fingerTag))
        {
            fingerTransform = null;
            isInteracting = false;
        }
    }
    private void UpdateVRPan() { if (interactionMode == InteractionMode.WorldSpace_Physics && isInteracting && fingerTransform != null) timeSinceLastInteraction = 0f; }
#if UNITY_EDITOR
    private void HandleEditorMouseInput()
    {
        if (mainCameraForRaycast == null) return;

        // Dùng logic "Mỏ Neo" cho cả Editor
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCameraForRaycast.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 2000f) && hit.collider == GetComponent<Collider>())
            {
                isInteracting = true;
                EnablePanMode();
                panOffsetOnDown = panOffset;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(mapDisplayImage.rectTransform, Input.mousePosition, mainCameraForRaycast, out pointerDownLocalPosition);
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isInteracting = false;
        }

        if (isInteracting && Input.GetMouseButton(0))
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mapDisplayImage.rectTransform, Input.mousePosition, mainCameraForRaycast, out Vector2 currentLocalPosition))
            {
                Vector2 totalDelta = currentLocalPosition - pointerDownLocalPosition;
                float conversionFactor = zoomLevel / mapDisplayImage.rectTransform.rect.width;
                Vector2 panDeltaFromStart = totalDelta * conversionFactor;
                panOffset = panOffsetOnDown + panDeltaFromStart;
                timeSinceLastInteraction = 0f;
            }
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            Ray ray = mainCameraForRaycast.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 2000f) && hit.collider == GetComponent<Collider>()) AdjustZoomByAmount(-scroll * zoomSpeed);
        }
    }
#endif
    public void SwitchMapArea(string areaName)
    {
        if (currentMapArea != null && currentMapArea.areaName == areaName) return;
        MapAreaData newArea = mapAreas.FirstOrDefault(area => area.areaName == areaName);
        if (newArea != null)
        {
            currentMapArea = newArea;
            currentWorldSize = newArea.worldMaxBounds - newArea.worldMinBounds;
            minimapMaterial.SetTexture("_MainTex", currentMapArea.mapTexture);
            minimapMaterial.SetTexture("_DetailTex", currentMapArea.detailMapTexture);
            minimapMaterial.SetFloat("_DetailZoomThreshold", currentMapArea.detailZoomThreshold);
        }
    }

    private bool ValidateSetup()
    {
        bool isValid = true;
        if (playerTransform == null) { Debug.LogError("Minimap Controller Error: 'Player Transform' chưa được gán!", this); isValid = false; }
        if (mapDisplayImage == null) { Debug.LogError("Minimap Controller Error: 'Map Display Image' (RawImage) chưa được gán!", this); isValid = false; }
        if (playerIcon == null) { Debug.LogError("Minimap Controller Error: 'Player Icon' chưa được gán!", this); isValid = false; }
        if (mapAreas == null || mapAreas.Length == 0) { Debug.LogError("Minimap Controller Error: 'Map Areas' chưa được định nghĩa!", this); isValid = false; }
        else
        {
            foreach (var area in mapAreas)
            {
                if (area.worldMinBounds.x >= area.worldMaxBounds.x || area.worldMinBounds.y >= area.worldMaxBounds.y)
                {
                    Debug.LogWarning($"Minimap Controller Warning: Trong map area '{area.areaName}', giá trị 'worldMinBounds' lớn hơn hoặc bằng 'worldMaxBounds'. Điều này có thể gây lỗi hiển thị.", this);
                }
            }
        }
        if (iconsContainer == null || iconPrefab == null) { Debug.LogWarning("Minimap Controller Warning: 'Icons Container' hoặc 'Icon Prefab' chưa được gán. Tính năng theo dõi đối tượng sẽ không hoạt động.", this); }
        if (string.IsNullOrEmpty(fingerTag)) { Debug.LogWarning("Minimap Controller Warning: 'Finger Tag' chưa được gán cho tương tác World Space.", this); }
        return isValid;
    }
    #endregion
}
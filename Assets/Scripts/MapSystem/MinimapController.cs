using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using Utils.Bill.InspectorCustom;

public class MinimapController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IScrollHandler
{
    #region Enums and Public Variables
    public enum InteractionMode
    {
        WorldSpace_Physics,
        ScreenSpace_UIEvents
    }

    public enum Axis
    {
        X_Positive, X_Negative,
        Y_Positive, Y_Negative,
        Z_Positive, Z_Negative
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
    [Tooltip("Đảo ngược hướng kéo map. Nếu true, kéo sang trái sẽ làm map di chuyển sang phải (kiểu kéo nội dung).")]
    public bool reversePanDirection = false;

    [CustomHeader("World Space Axis Configuration")]
    [Tooltip("Hệ số nhân cho độ nhạy khi kéo map, dùng để bù lại cho việc scale Canvas.")]
    public float vrPanSensitivity = 1.0f;
    [Tooltip("Trục local của GameObject này tương ứng với chiều NGANG (phải) của bản đồ.")]
    public Axis mapRightAxis = Axis.X_Positive;
    [Tooltip("Trục local của GameObject này tương ứng với chiều DỌC (lên) của bản đồ.")]
    public Axis mapUpAxis = Axis.Y_Positive;

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

    private Vector3 lastPointerWorldPositionOnMapPlane;
    private Vector2 lastPointerLocalPosition;

    private Plane minimapPlane;
    private Transform fingerTransform;
    private bool isInteracting = false;
    private Camera mainCameraForRaycast;


    #region Public Control Methods
    public void ZoomIn() { AdjustZoomByAmount(-buttonZoomStep); }
    public void ZoomOut() { AdjustZoomByAmount(buttonZoomStep); }
    public void ResetView() { DisablePanMode(); }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        var col = GetComponent<BoxCollider>();
        if (interactionMode == InteractionMode.WorldSpace_Physics)
        {
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            minimapPlane = new Plane(transform.up, transform.position);
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
        if (interactionMode == InteractionMode.WorldSpace_Physics)
        {
            HandleEditorMouseInputForWorldSpace();
        }
#endif

        if (isPanModeEnabled && !isInteracting)
        {
            timeSinceLastInteraction += Time.deltaTime;
            if (timeSinceLastInteraction > autoDisableTime) isPanModeEnabled = false; // Chỉ tắt mode, không reset
        }

        if (interactionMode == InteractionMode.WorldSpace_Physics && isInteracting && fingerTransform != null)
        {
            UpdateVRPan();
        }

        UpdateShaderProperties();
        UpdateAllIcons();
    }
    #endregion

    #region Core Logic (Unchanged)
    void UpdateAllIcons()
    {
        if (mapDisplayImage == null || currentMapArea == null || playerTransform == null) return;
        float mapDisplayWidthPixels = mapDisplayImage.rectTransform.rect.width;
        float mapDisplayHeightPixels = mapDisplayImage.rectTransform.rect.height;
        if (mapDisplayWidthPixels == 0 || mapDisplayHeightPixels == 0) return;
        Vector2 safeWorldSize = new Vector2(Mathf.Abs(currentWorldSize.x), Mathf.Abs(currentWorldSize.y));
        if (safeWorldSize.x == 0 || safeWorldSize.y == 0) return;
        Vector2 playerActualUV = (new Vector2(playerTransform.position.x, playerTransform.position.z) - currentMapArea.worldMinBounds) / safeWorldSize;
        Vector2 viewCenterUV = playerActualUV - panOffset;
        Vector3 viewCenterWorldPos = new Vector3(viewCenterUV.x * safeWorldSize.x + currentMapArea.worldMinBounds.x, playerTransform.position.y, viewCenterUV.y * safeWorldSize.y + currentMapArea.worldMinBounds.y);
        float worldUnitsVisibleOnMapX = safeWorldSize.x * zoomLevel;
        float worldUnitsVisibleOnMapZ = safeWorldSize.y * zoomLevel;
        if (worldUnitsVisibleOnMapX == 0 || worldUnitsVisibleOnMapZ == 0) return;
        float pixelsPerWorldUnitX = mapDisplayWidthPixels / worldUnitsVisibleOnMapX;
        float pixelsPerWorldUnitZ = mapDisplayHeightPixels / worldUnitsVisibleOnMapZ;
        if (playerIcon != null)
        {
            Vector3 playerDiffWorld = playerTransform.position - viewCenterWorldPos;
            Vector2 playerIconPosPixels = new Vector2(playerDiffWorld.x * pixelsPerWorldUnitX, playerDiffWorld.z * pixelsPerWorldUnitZ);
            playerIcon.anchoredPosition = playerIconPosPixels;
            bool shouldBeActive = Mathf.Abs(playerIconPosPixels.x) <= mapDisplayWidthPixels / 2f && Mathf.Abs(playerIconPosPixels.y) <= mapDisplayHeightPixels / 2f;
            if (playerIcon.gameObject.activeSelf != shouldBeActive) { playerIcon.gameObject.SetActive(shouldBeActive); }
        }
        if (iconsContainer != null && iconPrefab != null)
        {
            foreach (var icon in iconPool) { if (icon.gameObject.activeSelf) icon.gameObject.SetActive(false); }
            int poolIndex = 0;
            foreach (var trackable in TrackedObjects)
            {
                if (trackable == null || trackable.transform == null || trackable.transform == playerTransform) continue;
                Vector3 trackableDiffWorld = trackable.transform.position - viewCenterWorldPos;
                Vector2 finalIconPosPixels = new Vector2(trackableDiffWorld.x * pixelsPerWorldUnitX, trackableDiffWorld.z * pixelsPerWorldUnitZ);
                if (Mathf.Abs(finalIconPosPixels.x) <= mapDisplayWidthPixels / 2f && Mathf.Abs(finalIconPosPixels.y) <= mapDisplayHeightPixels / 2f)
                {
                    RectTransform iconRect;
                    if (poolIndex < iconPool.Count) iconRect = iconPool[poolIndex];
                    else
                    {
                        GameObject newIconObj = Instantiate(iconPrefab, iconsContainer);
                        iconRect = newIconObj.GetComponent<RectTransform>();
                        if (iconRect == null) { Destroy(newIconObj); continue; }
                        iconPool.Add(iconRect);
                    }
                    iconRect.gameObject.SetActive(true);
                    iconRect.anchoredPosition = finalIconPosPixels;
                    Image img = iconRect.GetComponent<Image>();
                    if (img != null) { img.sprite = trackable.iconSprite; img.color = trackable.iconColor; }
                    iconRect.sizeDelta = new Vector2(trackable.iconSize, trackable.iconSize);
                    poolIndex++;
                }
            }
        }
    }

    void UpdateShaderProperties()
    {
        if (currentMapArea == null || playerTransform == null || minimapMaterial == null) return;
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
        if (playerIcon != null && mapDisplayImage != null && mapDisplayImage.rectTransform.rect.width > 0 && mapDisplayImage.rectTransform.rect.height > 0)
        {
            Vector2 playerIconUV = new Vector2((playerIcon.anchoredPosition.x / mapDisplayImage.rectTransform.rect.width) + 0.5f, (playerIcon.anchoredPosition.y / mapDisplayImage.rectTransform.rect.height) + 0.5f);
            minimapMaterial.SetVector("_PlayerIconScreenUV", playerIconUV);
        }
    }
    #endregion

    #region Interaction Handlers
    void EnablePanMode()
    {
        if (isPanModeEnabled) { timeSinceLastInteraction = 0f; return; }
        isPanModeEnabled = true;
        timeSinceLastInteraction = 0f;
    }

    void DisablePanMode()
    {
        isPanModeEnabled = false;
        panOffset = Vector2.zero; // Reset vị trí
        isInteracting = false;
        fingerTransform = null;
    }

    void AdjustZoomByAmount(float amount)
    {
        if (!isPanModeEnabled) EnablePanMode();
        zoomLevel = Mathf.Clamp(zoomLevel + amount, minZoom, maxZoom);
        timeSinceLastInteraction = 0f;
        ClampPanOffset();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (interactionMode != InteractionMode.ScreenSpace_UIEvents) return;
        isInteracting = true;
        EnablePanMode();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(mapDisplayImage.rectTransform, eventData.position, eventData.pressEventCamera, out lastPointerLocalPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (interactionMode != InteractionMode.ScreenSpace_UIEvents || !isInteracting) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(mapDisplayImage.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 currentLocalPosition))
        {
            Vector2 frameDeltaPixels = currentLocalPosition - lastPointerLocalPosition;
            Vector2 panDeltaUV = Vector2.zero;
            float directionMultiplier = reversePanDirection ? 1f : -1f;
            if (mapDisplayImage.rectTransform.rect.width > 0)
                panDeltaUV.x = directionMultiplier * frameDeltaPixels.x * (zoomLevel / mapDisplayImage.rectTransform.rect.width);
            if (mapDisplayImage.rectTransform.rect.height > 0)
                panDeltaUV.y = directionMultiplier * frameDeltaPixels.y * (zoomLevel / mapDisplayImage.rectTransform.rect.height);
            panOffset += panDeltaUV;
            timeSinceLastInteraction = 0f;
            lastPointerLocalPosition = currentLocalPosition;
            ClampPanOffset();
        }
    }

    public void OnPointerUp(PointerEventData eventData) { if (interactionMode == InteractionMode.ScreenSpace_UIEvents) isInteracting = false; }
    public void OnScroll(PointerEventData eventData) { if (interactionMode == InteractionMode.ScreenSpace_UIEvents) AdjustZoomByAmount(-eventData.scrollDelta.y * zoomSpeed); }

    private int interactingFingerCount = 0;
    private void OnTriggerEnter(Collider other)
    {
        if (interactionMode != InteractionMode.WorldSpace_Physics || !other.CompareTag(fingerTag)) return;
        interactingFingerCount++;
        if (interactingFingerCount == 1)
        {
            fingerTransform = other.transform;
            isInteracting = true;
            EnablePanMode();
            Vector3 closestPointOnFinger = other.ClosestPoint(transform.position);
            lastPointerWorldPositionOnMapPlane = minimapPlane.ClosestPointOnPlane(closestPointOnFinger);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (interactionMode != InteractionMode.WorldSpace_Physics || !other.CompareTag(fingerTag)) return;
        interactingFingerCount--;
        if (interactingFingerCount <= 0)
        {
            interactingFingerCount = 0;
            fingerTransform = null;
            isInteracting = false;
        }
    }

    private void UpdateVRPan()
    {
        if (currentMapArea == null || fingerTransform == null) return;
        timeSinceLastInteraction = 0f;
        Vector3 currentFingerWorldPosOnMapPlane = minimapPlane.ClosestPointOnPlane(fingerTransform.position);
        Vector3 frameWorldDelta = currentFingerWorldPosOnMapPlane - lastPointerWorldPositionOnMapPlane;
        ApplyWorldSpacePan(frameWorldDelta);
        lastPointerWorldPositionOnMapPlane = currentFingerWorldPosOnMapPlane;
    }

#if UNITY_EDITOR
    private void HandleEditorMouseInputForWorldSpace()
    {
        if (mainCameraForRaycast == null || currentMapArea == null) return;
        Collider minimapCollider = GetComponent<Collider>();
        if (minimapCollider == null) return;
        Ray ray = mainCameraForRaycast.ScreenPointToRay(Input.mousePosition);
        if (Input.GetMouseButtonDown(0))
        {
            if (minimapCollider.Raycast(ray, out RaycastHit hit, 2000f) && !isInteracting)
            {
                isInteracting = true;
                EnablePanMode();
                lastPointerWorldPositionOnMapPlane = hit.point;
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            if (fingerTransform == null) { isInteracting = false; }
        }
        if (isInteracting && fingerTransform == null && Input.GetMouseButton(0))
        {
            if (minimapCollider.Raycast(ray, out RaycastHit hit, 2000f))
            {
                timeSinceLastInteraction = 0f;
                Vector3 currentMouseWorldPosOnMapPlane = hit.point;
                Vector3 frameWorldDelta = currentMouseWorldPosOnMapPlane - lastPointerWorldPositionOnMapPlane;
                ApplyWorldSpacePan(frameWorldDelta);
                lastPointerWorldPositionOnMapPlane = currentMouseWorldPosOnMapPlane;
            }
        }
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0 && minimapCollider.Raycast(ray, out _, 2000f))
        {
            AdjustZoomByAmount(-scroll * zoomSpeed);
        }
    }
#endif

    private float GetComponentFromAxis(Vector3 delta, Axis axis)
    {
        switch (axis)
        {
            case Axis.X_Positive: return delta.x;
            case Axis.X_Negative: return -delta.x;
            case Axis.Y_Positive: return delta.y;
            case Axis.Y_Negative: return -delta.y;
            case Axis.Z_Positive: return delta.z;
            case Axis.Z_Negative: return -delta.z;
            default: return 0f;
        }
    }

    private void ApplyWorldSpacePan(Vector3 worldDelta)
    {
        Vector3 localDelta = transform.InverseTransformDirection(worldDelta);
        Vector3 scaledLocalDelta = localDelta * vrPanSensitivity;

        float panComponentX = GetComponentFromAxis(scaledLocalDelta, mapRightAxis);
        float panComponentY = GetComponentFromAxis(scaledLocalDelta, mapUpAxis);

        Vector2 safeWorldSize = new Vector2(Mathf.Abs(currentWorldSize.x), Mathf.Abs(currentWorldSize.y));
        if (safeWorldSize.x == 0 || safeWorldSize.y == 0) return;

        float worldUnitsVisibleOnMapX = safeWorldSize.x * zoomLevel;
        float worldUnitsVisibleOnMapZ = safeWorldSize.y * zoomLevel;
        if (worldUnitsVisibleOnMapX == 0 || worldUnitsVisibleOnMapZ == 0) return;

        Vector2 framePanDeltaUV = new Vector2(panComponentX / worldUnitsVisibleOnMapX, panComponentY / worldUnitsVisibleOnMapZ);
        float directionMultiplier = reversePanDirection ? 1f : -1f;
        panOffset += directionMultiplier * framePanDeltaUV;
        ClampPanOffset();
    }

    private void ClampPanOffset()
    {
        if (currentMapArea == null || playerTransform == null || currentWorldSize.x == 0 || currentWorldSize.y == 0) return;
        if (zoomLevel >= 1.0f)
        {
            panOffset = Vector2.zero;
            return;
        }
        Vector2 playerActualUV = (new Vector2(playerTransform.position.x, playerTransform.position.z) - currentMapArea.worldMinBounds) / currentWorldSize;
        float halfZoom = zoomLevel / 2.0f;
        Vector2 minPanOffset = new Vector2(playerActualUV.x + halfZoom - 1.0f, playerActualUV.y + halfZoom - 1.0f);
        Vector2 maxPanOffset = new Vector2(playerActualUV.x - halfZoom, playerActualUV.y - halfZoom);
        panOffset.x = Mathf.Clamp(panOffset.x, minPanOffset.x, maxPanOffset.x);
        panOffset.y = Mathf.Clamp(panOffset.y, minPanOffset.y, maxPanOffset.y);
    }

    public void SwitchMapArea(string areaName)
    {
        if (currentMapArea != null && currentMapArea.areaName == areaName) return;
        MapAreaData newArea = mapAreas.FirstOrDefault(area => area.areaName == areaName);
        if (newArea != null)
        {
            currentMapArea = newArea;
            currentWorldSize = newArea.worldMaxBounds - newArea.worldMinBounds;
            if (minimapMaterial != null)
            {
                minimapMaterial.SetTexture("_MainTex", newArea.mapTexture);
                minimapMaterial.SetTexture("_DetailTex", newArea.detailMapTexture);
                minimapMaterial.SetFloat("_DetailZoomThreshold", newArea.detailZoomThreshold);
            }
            minimapPlane = new Plane(transform.up, transform.position);
        }
    }

    private bool ValidateSetup()
    {
        bool isValid = true;
        if (playerTransform == null) { Debug.LogError("Minimap Controller Error: 'Player Transform' chưa được gán!", this); isValid = false; }
        if (mapDisplayImage == null) { Debug.LogError("Minimap Controller Error: 'Map Display Image' (RawImage) chưa được gán!", this); isValid = false; }
        else if (mapDisplayImage.material == null || mapDisplayImage.material.shader == null)
        {
            Debug.LogError("Minimap Controller Error: 'Map Display Image' không có Material hoặc Material không có Shader!", this); isValid = false;
        }
        if (playerIcon == null) { Debug.LogError("Minimap Controller Error: 'Player Icon' chưa được gán!", this); isValid = false; }
        if (mapAreas == null || mapAreas.Length == 0) { Debug.LogError("Minimap Controller Error: 'Map Areas' chưa được định nghĩa!", this); isValid = false; }
        else
        {
            bool hasStartingArea = false;
            foreach (var area in mapAreas)
            {
                if (area.worldMinBounds.x >= area.worldMaxBounds.x || area.worldMinBounds.y >= area.worldMaxBounds.y)
                {
                    Debug.LogWarning($"Minimap Controller Warning: Trong map area '{area.areaName}', giá trị 'worldMinBounds' lớn hơn hoặc bằng 'worldMaxBounds'. Điều này có thể gây lỗi hiển thị.", this);
                }
                if (area.areaName == startingAreaName) hasStartingArea = true;
            }
            if (!hasStartingArea && mapAreas.Length > 0)
            {
                Debug.LogWarning($"Minimap Controller Warning: 'Starting Area Name' ({startingAreaName}) không tồn tại trong 'Map Areas'. Sẽ sử dụng area đầu tiên.", this);
                startingAreaName = mapAreas[0].areaName;
            }
            else if (mapAreas.Length > 0 && string.IsNullOrEmpty(startingAreaName))
            {
                startingAreaName = mapAreas[0].areaName;
            }
            else if (mapAreas.Length == 0 && !string.IsNullOrEmpty(startingAreaName))
            {
                Debug.LogError("Minimap Controller Error: 'Starting Area Name' được gán nhưng không có 'Map Areas' nào được định nghĩa!", this); isValid = false;
            }
        }
        if (iconsContainer == null || iconPrefab == null) { Debug.LogWarning("Minimap Controller Warning: 'Icons Container' hoặc 'Icon Prefab' chưa được gán. Tính năng theo dõi đối tượng sẽ không hoạt động.", this); }
        if (interactionMode == InteractionMode.WorldSpace_Physics && string.IsNullOrEmpty(fingerTag)) { Debug.LogWarning("Minimap Controller Warning: 'Finger Tag' chưa được gán cho tương tác World Space.", this); }
        if (interactionMode == InteractionMode.WorldSpace_Physics && GetComponent<Collider>() == null) { Debug.LogError("Minimap Controller Error: Chế độ WorldSpace_Physics yêu cầu một Collider trên GameObject này.", this); isValid = false; }

        return isValid;
    }
    #endregion
}
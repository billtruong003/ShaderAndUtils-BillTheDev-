using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using Utils.Bill.InspectorCustom;

[RequireComponent(typeof(BoxCollider))]
public class MinimapController : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IScrollHandler
{
    public enum InteractionMode
    {
        WorldSpace_Physics,
        ScreenSpace_UIEvents
    }

    [CustomHeader("Interaction Mode", "#8E24AA")]
    [Tooltip("Chọn phương thức tương tác cho minimap.")]
    public InteractionMode interactionMode = InteractionMode.WorldSpace_Physics;

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

    [CustomHeader("Visuals: Sight Cone", "#E040FB")]
    [ColorUsage(true, true)] public Color sightConeColor = new Color(1, 1, 0, 0.25f);
    [CustomSlider(10, 180)] public float sightConeAngle = 90f;
    [CustomSlider(0.01f, 1.0f)] public float sightConeSoftness = 0.1f;

    [CustomHeader("Edit Mode Settings", "#D500F9")]
    public float autoDisableTime = 5.0f;
    public float zoomSpeed = 0.2f;
    public float minZoom = 0.05f;
    public float maxZoom = 1.0f;
    public string fingerTag = "PlayerFinger";

    private Material minimapMaterial;
    private MapAreaData currentMapArea;
    private Vector2 currentWorldSize;
    private List<RectTransform> iconPool = new List<RectTransform>();

    private bool isPanModeEnabled = false;
    private float timeSinceLastInteraction = 0f;
    private Vector2 panOffset = Vector2.zero;

    private Transform fingerTransform;
    private Vector2 lastPanPosition;
    private bool isInteracting = false;

#if UNITY_EDITOR
    private Camera mainCameraForRaycast;
#endif

    void Awake()
    {
        if (interactionMode == InteractionMode.WorldSpace_Physics)
        {
            var col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }
            col.isTrigger = true;
        }
    }

    void Start()
    {
        if (!ValidateSetup()) { this.enabled = false; return; }
        minimapMaterial = new Material(mapDisplayImage.material);
        mapDisplayImage.material = minimapMaterial;
        SwitchMapArea(startingAreaName);
        if (iconPrefab != null) { iconPrefab.SetActive(false); }
#if UNITY_EDITOR
        mainCameraForRaycast = Camera.main;
#endif
    }

    void OnDisable() { DisablePanMode(); }

    void LateUpdate()
    {
        if (minimapMaterial == null || currentMapArea == null || playerTransform == null) return;

#if UNITY_EDITOR
        if (interactionMode == InteractionMode.WorldSpace_Physics)
        {
            HandleEditorMouseInput();
        }
#endif

        if (isPanModeEnabled && !isInteracting)
        {
            timeSinceLastInteraction += Time.deltaTime;
            if (timeSinceLastInteraction > autoDisableTime)
            {
                DisablePanMode();
            }
        }

        if (interactionMode == InteractionMode.WorldSpace_Physics) UpdateVRPan();

        UpdateShaderProperties();
        UpdateTrackedIcons();
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

        Vector2 mapCenterUV = normalizedPos - panOffset;
        Vector3 mapCenterWorldPos = new Vector3(
            mapCenterUV.x * safeWorldSize.x + currentMapArea.worldMinBounds.x, 0,
            mapCenterUV.y * safeWorldSize.y + currentMapArea.worldMinBounds.y
        );

        float mapDisplayRadius = mapDisplayImage.rectTransform.rect.width / 2f;
        float worldRadiusOnMap = ((safeWorldSize.x + safeWorldSize.y) / 2f) * zoomLevel;
        if (worldRadiusOnMap <= 0) return;
        float scale = mapDisplayRadius / worldRadiusOnMap;
        Vector3 diff = playerTransform.position - mapCenterWorldPos;
        Vector2 playerIconScreenPos = new Vector2(diff.x, diff.z) * scale;
        Vector2 playerIconUV = new Vector2(
            (playerIconScreenPos.x / mapDisplayImage.rectTransform.rect.width) + 0.5f,
            (playerIconScreenPos.y / mapDisplayImage.rectTransform.rect.height) + 0.5f
        );
        minimapMaterial.SetVector("_PlayerIconScreenUV", playerIconUV);

        Vector3 forward = playerTransform.forward;
        Vector2 forward2D = new Vector2(forward.x, forward.z).normalized;
        minimapMaterial.SetVector("_PlayerForward", forward2D);
        minimapMaterial.SetColor("_SightConeColor", sightConeColor);
        minimapMaterial.SetFloat("_SightConeAngle", sightConeAngle);
        minimapMaterial.SetFloat("_SightConeSoftness", sightConeSoftness);

        playerIcon.gameObject.SetActive(!isPanModeEnabled);
    }

    void UpdateTrackedIcons()
    {
        if (iconPrefab == null || iconsContainer == null) return;

        foreach (var icon in iconPool)
        {
            if (icon.gameObject.activeSelf) icon.gameObject.SetActive(false);
        }

        float mapDisplayRadius = mapDisplayImage.rectTransform.rect.width / 2f;
        if (mapDisplayRadius <= 0) return;

        Vector2 safeWorldSize = new Vector2(Mathf.Abs(currentWorldSize.x), Mathf.Abs(currentWorldSize.y));
        if (safeWorldSize.x == 0 || safeWorldSize.y == 0) return;

        float worldRadiusOnMap = ((safeWorldSize.x + safeWorldSize.y) / 2f) * zoomLevel;
        if (worldRadiusOnMap <= 0) return;
        float scale = mapDisplayRadius / worldRadiusOnMap;

        Vector2 playerUV = (new Vector2(playerTransform.position.x, playerTransform.position.z) - currentMapArea.worldMinBounds) / safeWorldSize;
        Vector2 mapCenterUV = playerUV - panOffset;
        Vector3 mapCenterWorldPos = new Vector3(
            mapCenterUV.x * safeWorldSize.x + currentMapArea.worldMinBounds.x,
            playerTransform.position.y,
            mapCenterUV.y * safeWorldSize.y + currentMapArea.worldMinBounds.y
        );

        int poolIndex = 0;
        foreach (var trackable in TrackedObjects)
        {
            if (!isPanModeEnabled && trackable.transform == playerTransform)
            {
                continue;
            }

            Vector3 diff = trackable.transform.position - mapCenterWorldPos;
            Vector2 diff2D = new Vector2(diff.x, diff.z);
            Vector2 finalIconPos = diff2D * scale;

            if (finalIconPos.magnitude < mapDisplayRadius)
            {
                RectTransform iconRect;
                if (poolIndex < iconPool.Count)
                {
                    iconRect = iconPool[poolIndex];
                }
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

    void EnablePanMode()
    {
        if (isPanModeEnabled)
        {
            timeSinceLastInteraction = 0f;
            return;
        }
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

    public void AdjustZoom(float amount)
    {
        if (!isPanModeEnabled) EnablePanMode();
        zoomLevel = Mathf.Clamp(zoomLevel - amount * zoomSpeed, minZoom, maxZoom);
        timeSinceLastInteraction = 0f;
    }

    #region Interaction Handlers
    public void OnPointerDown(PointerEventData eventData)
    {
        if (interactionMode != InteractionMode.ScreenSpace_UIEvents) return;
        isInteracting = true;
        EnablePanMode();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapDisplayImage.rectTransform, eventData.position, eventData.pressEventCamera, out lastPanPosition);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (interactionMode != InteractionMode.ScreenSpace_UIEvents || !isInteracting) return;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            mapDisplayImage.rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 currentPos))
        {
            Vector2 delta = currentPos - lastPanPosition;
            lastPanPosition = currentPos;
            float conversionFactor = zoomLevel / mapDisplayImage.rectTransform.rect.width;
            panOffset += delta * conversionFactor;
            timeSinceLastInteraction = 0f;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (interactionMode != InteractionMode.ScreenSpace_UIEvents) return;
        isInteracting = false;
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (interactionMode != InteractionMode.ScreenSpace_UIEvents) return;
        AdjustZoom(eventData.scrollDelta.y);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (interactionMode != InteractionMode.WorldSpace_Physics) return;
        if (!string.IsNullOrEmpty(fingerTag) && other.CompareTag(fingerTag))
        {
            fingerTransform = other.transform;
            isInteracting = true;
            EnablePanMode();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (interactionMode != InteractionMode.WorldSpace_Physics) return;
        if (!string.IsNullOrEmpty(fingerTag) && other.CompareTag(fingerTag))
        {
            fingerTransform = null;
            isInteracting = false;
        }
    }

    private void UpdateVRPan()
    {
        if (interactionMode != InteractionMode.WorldSpace_Physics || !isInteracting || fingerTransform == null) return;
        // Panning logic for VR needs custom implementation based on your VR toolkit
        timeSinceLastInteraction = 0f;
    }

#if UNITY_EDITOR
    private void HandleEditorMouseInput()
    {
        if (mainCameraForRaycast == null) return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCameraForRaycast.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 2000f) && hit.collider == GetComponent<Collider>())
            {
                isInteracting = true;
                EnablePanMode();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    mapDisplayImage.rectTransform, Input.mousePosition, mainCameraForRaycast, out lastPanPosition);
            }
        }

        if (Input.GetMouseButtonUp(0)) isInteracting = false;

        if (isInteracting && Input.GetMouseButton(0))
        {
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                mapDisplayImage.rectTransform, Input.mousePosition, mainCameraForRaycast, out Vector2 currentMousePos))
            {
                Vector2 delta = currentMousePos - lastPanPosition;
                lastPanPosition = currentMousePos;
                float conversionFactor = zoomLevel / mapDisplayImage.rectTransform.rect.width;
                panOffset += delta * conversionFactor;
                timeSinceLastInteraction = 0f;
            }
        }

        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            Ray ray = mainCameraForRaycast.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 2000f) && hit.collider == GetComponent<Collider>())
            {
                AdjustZoom(scroll);
            }
        }
    }
#endif

    #endregion

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
        if (iconsContainer == null || iconPrefab == null) { Debug.LogWarning("Minimap Controller Warning: 'Icons Container' hoặc 'Icon Prefab' chưa được gán.", this); }
        if (string.IsNullOrEmpty(fingerTag)) { Debug.LogWarning("Minimap Controller Warning: 'Finger Tag' chưa được gán.", this); }
        return isValid;
    }
}
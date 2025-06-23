using UnityEngine;

[System.Serializable]
public class MapLevelInfo
{
    [Tooltip("Tên để bạn dễ nhận biết (vd: Tầng Trệt, Hầm Ngục)")]
    public string levelName;
    [Tooltip("Đối tượng Quad/Plane hiển thị bản đồ của tầng này")]
    public GameObject mapPlaneObject;
    [Tooltip("Vật liệu (Material) chứa shader MapReveal của tầng này")]
    public Material mapMaterial;
    [Tooltip("Vị trí của camera so với tâm của Map Plane để nhìn thấy toàn bộ bản đồ")]
    public Vector3 cameraOffset = new Vector3(0, 10, 0);
    [HideInInspector]
    public RenderTexture paintMap;
}
using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
[AddComponentMenu("Utilities/Layout/3D Grid Layout")]
public sealed class GridLayout3D : MonoBehaviour
{
    [Min(1)]
    [Tooltip("Số lượng cột trong lưới.")]
    public int columns = 4;

    [Tooltip("Khoảng cách giữa mỗi ô trong lưới trên cả ba trục.")]
    public Vector3 cellSpacing = new Vector3(1.5f, 0f, 1.5f);

    [Tooltip("Tự động sắp xếp các đối tượng con khi thuộc tính được thay đổi trong Inspector.")]
    public bool autoUpdate = true;

    [Tooltip("Danh sách các Material sẽ được gán cho các đối tượng con.")]
    public List<Material> materials = new List<Material>();

    private void OnValidate()
    {
        if (autoUpdate)
        {
            ArrangeChildren();
        }
    }

    public void ArrangeChildren()
    {
        if (columns <= 0)
        {
            return;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null) continue;

            int currentRow = i / columns;
            int currentColumn = i % columns;
            Vector3 targetPosition = CalculateTargetPosition(currentRow, currentColumn);
            child.localPosition = targetPosition;
        }
    }

    public void AssignMaterialsToChildren()
    {
        if (materials == null || materials.Count == 0)
        {
            Debug.LogWarning("Không có material trong danh sách để gán.", this);
            return;
        }

        // THAY ĐỔI CHÍNH: Lấy tất cả các loại Renderer (MeshRenderer, SkinnedMeshRenderer, v.v.)
        // bằng cách tìm kiếm lớp cơ sở 'Renderer'.
        Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);

        List<Renderer> activeRenderers = new List<Renderer>();
        foreach (Renderer renderer in allRenderers)
        {
            // Chỉ thêm vào danh sách nếu renderer được bật VÀ GameObject của nó đang hoạt động.
            if (renderer.enabled && renderer.gameObject.activeInHierarchy)
            {
                activeRenderers.Add(renderer);
            }
        }

        if (activeRenderers.Count == 0)
        {
            Debug.Log("Không tìm thấy Renderer (MeshRenderer hoặc SkinnedMeshRenderer) nào đang hoạt động trong các đối tượng con.", this);
            return;
        }

        // Lặp qua danh sách các renderer đã được lọc và gán material.
        // Logic này hoạt động cho cả hai loại vì chúng đều có thuộc tính 'sharedMaterial'.
        for (int i = 0; i < activeRenderers.Count; i++)
        {
            Renderer renderer = activeRenderers[i];

            int materialIndex = i % materials.Count;
            Material materialToAssign = materials[materialIndex];

            if (materialToAssign != null)
            {
                renderer.sharedMaterial = materialToAssign;
            }
        }

        Debug.Log($"Đã gán materials cho {activeRenderers.Count} Renderer đang hoạt động.", this);
    }

    private Vector3 CalculateTargetPosition(int row, int column)
    {
        float xPosition = column * cellSpacing.x;
        float yPosition = row * cellSpacing.y;
        float zPosition = row * cellSpacing.z;
        return new Vector3(xPosition, yPosition, zPosition);
    }
}
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MeshInspectorWindow : EditorWindow
{
    // Sử dụng [SerializeField] để Unity lưu trữ danh sách này
    [SerializeField]
    private List<Mesh> meshesToInspect = new List<Mesh>();

    private SerializedObject serializedObject;
    private SerializedProperty serializedMeshesProperty;

    // Đổi tên menu item một chút để phân biệt
    [MenuItem("Tools/BillTheDev/Mesh Inspector (Multiple)")]
    public static void ShowWindow()
    {
        // Lấy hoặc tạo cửa sổ Inspector
        GetWindow<MeshInspectorWindow>("Mesh Inspector");
    }

    // OnEnable được gọi khi cửa sổ được mở hoặc script được compile lại
    private void OnEnable()
    {
        // Thiết lập SerializedObject để làm việc với list trong UI
        serializedObject = new SerializedObject(this);
        serializedMeshesProperty = serializedObject.FindProperty("meshesToInspect");
    }

    private void OnGUI()
    {
        GUILayout.Label("Kéo các file Mesh (.asset) vào danh sách dưới đây", EditorStyles.boldLabel);

        // Bắt đầu kiểm tra các thay đổi trên UI
        serializedObject.Update();

        // Dòng này sẽ tự động vẽ ra một list UI hoàn chỉnh
        EditorGUILayout.PropertyField(serializedMeshesProperty, true);

        // Áp dụng các thay đổi từ UI vào object
        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(10);

        // Vô hiệu hóa nút bấm nếu danh sách trống
        GUI.enabled = meshesToInspect.Count > 0;
        if (GUILayout.Button($"Inspect {meshesToInspect.Count} Meshes", GUILayout.Height(30)))
        {
            InspectAllMeshes();
        }
        // Bật lại UI
        GUI.enabled = true;
    }

    private void InspectAllMeshes()
    {
        if (meshesToInspect == null || meshesToInspect.Count == 0)
        {
            Debug.LogWarning("Danh sách Mesh trống. Vui lòng kéo Mesh vào để kiểm tra.");
            return;
        }

        int meshIndex = 0;
        foreach (var mesh in meshesToInspect)
        {
            meshIndex++;
            if (mesh == null)
            {
                Debug.LogWarning($"--- Vị trí #{meshIndex} trong danh sách bị trống (null), bỏ qua. ---");
                continue;
            }

            // In ra log với context, khi bấm vào log sẽ highlight file mesh tương ứng
            Debug.Log($"--- [{meshIndex}/{meshesToInspect.Count}] Bắt đầu kiểm tra Mesh: '{mesh.name}' ---", mesh);

            // 1. Thông tin cơ bản
            Debug.Log($"Số đỉnh (Vertices): {mesh.vertexCount}");
            Debug.Log($"Số tam giác (Triangles): {mesh.triangles.Length} (Tạo thành {mesh.triangles.Length / 3} mặt)");
            Debug.Log($"Số Sub-Mesh: {mesh.subMeshCount}");
            Debug.Log($"Mesh có thể đọc/ghi (Is Readable): {mesh.isReadable}");

            // 2. Kiểm tra dữ liệu bắt buộc cho rendering
            if (mesh.vertexCount == 0)
            {
                Debug.LogError($"LỖI NGHIÊM TRỌNG: Mesh '{mesh.name}' không có đỉnh (vertex) nào!", mesh);
            }
            if (mesh.triangles.Length == 0)
            {
                Debug.LogError($"LỖI NGHIÊM TRỌNG: Mesh '{mesh.name}' không có tam giác (triangle) nào để vẽ!", mesh);
            }

            // 3. Ranh giới (Bounds) - THÔNG TIN QUAN TRỌNG NHẤT
            Bounds bounds = mesh.bounds;
            Debug.Log($"Ranh giới (Bounds) Center: {bounds.center}, Size: {bounds.size}");
            if (bounds.size == Vector3.zero)
            {
                Debug.LogError($"LỖI CULLING: Bounds của mesh '{mesh.name}' có kích thước bằng 0. Unity sẽ luôn cho rằng nó nằm ngoài camera và không vẽ!", mesh);
            }
            else if (bounds.extents.magnitude < 0.1f)
            {
                Debug.LogWarning($"CẢNH BÁO CULLING: Bounds của mesh '{mesh.name}' rất nhỏ. Đây là nguyên nhân hàng đầu gây ra lỗi không render được khi dùng DrawMeshInstanced.", mesh);
            }

            // 4. Dữ liệu phụ (cho shader)
            Debug.Log($"Có Normal: {mesh.normals.Length > 0} (Số lượng: {mesh.normals.Length})");
            Debug.Log($"Có UV0 (Main Texcoord): {mesh.uv.Length > 0} (Số lượng: {mesh.uv.Length})");
            Debug.Log($"Có UV1 (VertexIdUV): {mesh.uv2.Length > 0} (Số lượng: {mesh.uv2.Length})");

            Debug.Log($"--- Kết thúc kiểm tra '{mesh.name}' ---");
        }
    }
}
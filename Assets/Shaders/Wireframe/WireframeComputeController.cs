using UnityEngine;

[ExecuteAlways]
public class WireframeComputeController : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material wireframeMaterial;

    private Mesh mesh;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer clipSpaceBuffer;
    private ComputeBuffer edgeDistancesBuffer;

    // Cấu trúc dữ liệu đỉnh (phải khớp với compute shader)
    private struct VertexData
    {
        public Vector3 positionOS;
        public Vector2 uv;
        public Vector3 normal;
    }

    // Cấu trúc dữ liệu tam giác (phải khớp với compute shader)
    private struct TriangleData
    {
        public uint index0;
        public uint index1;
        public uint index2;
    }

    void Start()
    {
        // Lấy mesh từ đối tượng
        SkinnedMeshRenderer meshFilter = GetComponent<SkinnedMeshRenderer>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogError("Không tìm thấy MeshFilter hoặc Mesh!");
            return;
        }

        mesh = meshFilter.sharedMesh;

        // Thiết lập buffers
        SetupBuffers();

        // Gán buffer vào material
        wireframeMaterial.SetBuffer("_EdgeDistances", edgeDistancesBuffer);
    }

    void SetupBuffers()
    {
        // Lấy dữ liệu đỉnh và tam giác từ mesh
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        Vector3[] normals = mesh.normals;
        int[] triangles = mesh.triangles;

        // Tạo dữ liệu đỉnh
        VertexData[] vertexData = new VertexData[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertexData[i] = new VertexData
            {
                positionOS = vertices[i],
                uv = uvs.Length > i ? uvs[i] : Vector2.zero,
                normal = normals.Length > i ? normals[i] : Vector3.up
            };
        }

        // Tạo dữ liệu tam giác
        TriangleData[] triangleData = new TriangleData[triangles.Length / 3];
        for (int i = 0; i < triangleData.Length; i++)
        {
            triangleData[i] = new TriangleData
            {
                index0 = (uint)triangles[i * 3],
                index1 = (uint)triangles[i * 3 + 1],
                index2 = (uint)triangles[i * 3 + 2]
            };
        }

        // Tạo buffers
        vertexBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 8); // 3 + 2 + 3 (positionOS + uv + normal)
        triangleBuffer = new ComputeBuffer(triangleData.Length, sizeof(uint) * 3); // 3 indices
        clipSpaceBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 4); // float4
        edgeDistancesBuffer = new ComputeBuffer(vertices.Length, sizeof(float) * 3); // float3

        // Gửi dữ liệu vào buffers
        vertexBuffer.SetData(vertexData);
        triangleBuffer.SetData(triangleData);

        // Gán buffers vào compute shader
        int kernel = computeShader.FindKernel("ComputeWireframeDistances");
        computeShader.SetBuffer(kernel, "_VertexBuffer", vertexBuffer);
        computeShader.SetBuffer(kernel, "_TriangleBuffer", triangleBuffer);
        computeShader.SetBuffer(kernel, "_ClipSpacePositions", clipSpaceBuffer);
        computeShader.SetBuffer(kernel, "_EdgeDistances", edgeDistancesBuffer);
    }

    void Update()
    {
        // Tính toán tọa độ clip space cho các đỉnh
        Vector3[] vertices = mesh.vertices;
        Vector4[] clipSpacePositions = new Vector4[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector4 worldPos = transform.localToWorldMatrix * new Vector4(vertices[i].x, vertices[i].y, vertices[i].z, 1);
            clipSpacePositions[i] = Camera.main.projectionMatrix * Camera.main.worldToCameraMatrix * worldPos;
        }
        clipSpaceBuffer.SetData(clipSpacePositions);

        // Dispatch compute shader
        int kernel = computeShader.FindKernel("ComputeWireframeDistances");
        computeShader.SetFloat("_WireThickness", wireframeMaterial.GetFloat("_WireThickness"));
        int threadGroups = Mathf.CeilToInt(vertices.Length / 64.0f);
        computeShader.Dispatch(kernel, threadGroups, 1, 1);
    }

    void OnDestroy()
    {
        // Giải phóng buffers
        if (vertexBuffer != null) vertexBuffer.Release();
        if (triangleBuffer != null) triangleBuffer.Release();
        if (clipSpaceBuffer != null) clipSpaceBuffer.Release();
        if (edgeDistancesBuffer != null) edgeDistancesBuffer.Release();
    }
}
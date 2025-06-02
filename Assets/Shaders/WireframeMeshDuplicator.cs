using UnityEngine;

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class WireframeMeshDuplicator : MonoBehaviour
{
    void Start()
    {
        SkinnedMeshRenderer skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        Mesh originalMesh = skinnedMeshRenderer.sharedMesh;

        int triangleCount = originalMesh.triangles.Length / 3;
        int newVertexCount = triangleCount * 3;

        Vector3[] newVertices = new Vector3[newVertexCount];
        Vector3[] newNormals = new Vector3[newVertexCount];
        Vector2[] newUVs = new Vector2[newVertexCount];
        Color[] newColors = new Color[newVertexCount];
        BoneWeight[] newBoneWeights = new BoneWeight[newVertexCount];
        int[] newTriangles = new int[triangleCount * 3];

        for (int i = 0; i < triangleCount; i++)
        {
            int idx0 = originalMesh.triangles[i * 3];
            int idx1 = originalMesh.triangles[i * 3 + 1];
            int idx2 = originalMesh.triangles[i * 3 + 2];

            int newIdx0 = i * 3;
            int newIdx1 = i * 3 + 1;
            int newIdx2 = i * 3 + 2;

            newVertices[newIdx0] = originalMesh.vertices[idx0];
            newVertices[newIdx1] = originalMesh.vertices[idx1];
            newVertices[newIdx2] = originalMesh.vertices[idx2];

            newNormals[newIdx0] = originalMesh.normals[idx0];
            newNormals[newIdx1] = originalMesh.normals[idx1];
            newNormals[newIdx2] = originalMesh.normals[idx2];

            newUVs[newIdx0] = originalMesh.uv[idx0];
            newUVs[newIdx1] = originalMesh.uv[idx1];
            newUVs[newIdx2] = originalMesh.uv[idx2];

            newBoneWeights[newIdx0] = originalMesh.boneWeights[idx0];
            newBoneWeights[newIdx1] = originalMesh.boneWeights[idx1];
            newBoneWeights[newIdx2] = originalMesh.boneWeights[idx2];

            newColors[newIdx0] = new Color(1, 0, 0);
            newColors[newIdx1] = new Color(0, 1, 0);
            newColors[newIdx2] = new Color(0, 0, 1);

            newTriangles[i * 3] = newIdx0;
            newTriangles[i * 3 + 1] = newIdx1;
            newTriangles[i * 3 + 2] = newIdx2;
        }

        Mesh newMesh = new Mesh();
        newMesh.vertices = newVertices;
        newMesh.normals = newNormals;
        newMesh.uv = newUVs;
        newMesh.colors = newColors;
        newMesh.boneWeights = newBoneWeights;
        newMesh.triangles = newTriangles;
        newMesh.bindposes = originalMesh.bindposes;

        skinnedMeshRenderer.sharedMesh = newMesh;
    }
}
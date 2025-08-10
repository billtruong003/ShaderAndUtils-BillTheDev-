using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Rendering/Simple Decal Controller")]
public class DecalController : MonoBehaviour
{
    [Header("Decal Setup")]
    public Material decalMaterial;
    public Texture2D texture;
    public Color tinting = Color.white;

    private MaterialPropertyBlock propertyBlock;
    private static Mesh unitCubeMesh;

    private static void EnsureCubeMeshExists()
    {
        if (unitCubeMesh == null)
        {
            GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            unitCubeMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(primitive);
        }
    }

    public void OnEnable()
    {
        propertyBlock = new MaterialPropertyBlock();
        EnsureCubeMeshExists();
    }

    void LateUpdate()
    {
        if (decalMaterial == null || unitCubeMesh == null)
        {
            return;
        }
        DrawDecal();
    }

    private void DrawDecal()
    {
        if (texture != null)
        {
            propertyBlock.SetTexture("_MainTex", texture);
        }
        propertyBlock.SetColor("_Tint", tinting);

        Graphics.DrawMesh(
            unitCubeMesh,
            transform.localToWorldMatrix,
            decalMaterial,
            gameObject.layer,
            null, 0, propertyBlock, false, true, false
        );
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var gizmoColor = new Color(0.0f, 0.7f, 1f, 0.3f);
        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(Vector3.zero, Vector3.forward * 0.6f);
    }
#endif
}
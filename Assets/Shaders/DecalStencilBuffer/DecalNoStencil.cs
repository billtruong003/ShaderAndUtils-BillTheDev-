using UnityEngine;
using UnityEditor;

using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[ExecuteInEditMode]
public class DecalNoStencil : MonoBehaviour
{
    public Material m_Material;
    public Texture2D texture;
    public Color tinting = Color.white;
    [SerializeField]
    Camera cam;
    public Mesh m_CubeMesh;
    MaterialPropertyBlock props;

    public void OnEnable()
    {
        props = new MaterialPropertyBlock();
    }

    void LateUpdate()
    {
        // drawing to all cameras, but you can set it to just a specific one here
        Draw(cam);
    }
#if UNITY_EDITOR
    private void DrawGizmo(bool selected)
    {
        var col = new Color(0.0f, 0.7f, 1f, 1.0f);
        col.a = selected ? 0.3f : 0.1f;
        Gizmos.color = col;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        col.a = selected ? 0.5f : 0.2f;
        Gizmos.color = col;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Handles.matrix = transform.localToWorldMatrix;
        Handles.DrawBezier(Vector3.zero, Vector3.down, Vector3.zero, Vector3.down, Color.red, null, selected ? 4f : 2f);
    }
#endif


#if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        DrawGizmo(false);
    }
    public void OnDrawGizmosSelected()
    {
        DrawGizmo(true);
    }
#endif


    private void Draw(Camera camera)
    {

        // set up property block for decal texture and tinting
        if (texture != null)
        {
            props.SetTexture("_MainTex", texture);
        }
        props.SetColor("_Tint", tinting);
        // draw the decal on the main camera
        Graphics.DrawMesh(m_CubeMesh, transform.localToWorldMatrix, m_Material, 0, null, 0, props, false, true, false);


    }
}

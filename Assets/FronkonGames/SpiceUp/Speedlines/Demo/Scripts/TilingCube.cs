using UnityEngine;

namespace FronkonGames.SpiceUp.Speedlines
{
  /// <summary> Auto tiling cube. </summary>
  /// <remarks>
  /// This code is designed for a simple demo, not for production environments.
  /// </remarks>
  [ExecuteInEditMode]
  public sealed class TilingCube : MonoBehaviour
  {
    [SerializeField]
    private Vector3 textureScale = Vector3.one;

    [SerializeField]
    private Vector3 snapScale = Vector3.one * 0.5f;
    
    private Vector3 lastLocalScale;

    private void UpdateMesh()
    {
      MeshFilter meshFilter = this.gameObject.GetComponent<MeshFilter>();
      if (meshFilter == null)
      {
        GameObject temporal = GameObject.CreatePrimitive(PrimitiveType.Cube);
      
        meshFilter = this.gameObject.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = temporal.GetComponent<MeshFilter>().sharedMesh;
        meshFilter.hideFlags = HideFlags.HideInInspector;

#if UNITY_EDITOR
        DestroyImmediate(temporal);
#else
        Destroy(temporal);
#endif      
        MeshRenderer meshRenderer = this.gameObject.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        meshRenderer.hideFlags = HideFlags.HideInInspector;
        
        this.gameObject.AddComponent<BoxCollider>().hideFlags = HideFlags.HideInHierarchy;
      }
      
      this.gameObject.transform.localScale = new Vector3(Mathf.Round(this.gameObject.transform.localScale.x / snapScale.x) * snapScale.x,
        Mathf.Round(this.gameObject.transform.localScale.y / snapScale.y) * snapScale.y,
        Mathf.Round(this.gameObject.transform.localScale.z / snapScale.z) * snapScale.z);
        
      float depth = this.gameObject.transform.localScale.z * textureScale.z;
      float width = this.gameObject.transform.localScale.x * textureScale.x;
      float height = this.gameObject.transform.localScale.y * textureScale.y;

      Vector2[] meshUVs = meshFilter.sharedMesh.uv;

      // Front.
      meshUVs[2] = new Vector2(0, height);
      meshUVs[3] = new Vector2(width, height);
      meshUVs[0] = new Vector2(0, 0);
      meshUVs[1] = new Vector2(width, 0);

      // Back.
      meshUVs[7] = Vector2.zero;
      meshUVs[6] = new Vector2(width, 0.0f);
      meshUVs[11] = new Vector2(0.0f, height);
      meshUVs[10] = new Vector2(width, height);

      // Left.
      meshUVs[19] = new Vector2(depth, 0.0f);
      meshUVs[17] = new Vector2(0.0f, height);
      meshUVs[16] = Vector2.zero;
      meshUVs[18] = new Vector2(depth, height);

      // Right.
      meshUVs[23] = new Vector2(depth, 0.0f);
      meshUVs[21] = new Vector2(0.0f, height);
      meshUVs[20] = Vector2.zero;
      meshUVs[22] = new Vector2(depth, height);

      // Top.
      meshUVs[4] = new Vector2(width, 0.0f);
      meshUVs[5] = Vector2.zero;
      meshUVs[8] = new Vector2(width, depth);
      meshUVs[9] = new Vector2(0.0f, depth);

      // Bottom.
      meshUVs[13] = new Vector2(width, 0.0f);
      meshUVs[14] = new Vector2(0.0f, 0.0f);
      meshUVs[12] = new Vector2(width, depth);
      meshUVs[15] = new Vector2(0.0f, depth);

      meshFilter.sharedMesh.uv = meshUVs;
    }
    
    private void Update()
    {
      if (lastLocalScale != this.gameObject.transform.localScale)
      {
        UpdateMesh();

        lastLocalScale = this.gameObject.transform.localScale;
      }
    }
  }
}
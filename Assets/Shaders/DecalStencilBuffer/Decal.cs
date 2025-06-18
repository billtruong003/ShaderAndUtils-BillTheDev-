using UnityEngine;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class Decal : MonoBehaviour
{
    public Material m_Material;

    [Header("PropertyBlock Settings")]
    public Texture2D texture;
    public string textureName = "_MainTex";

    [ColorUsage(true, true)]
    public Color color = Color.white;
    public string colorName = "_Tint";

    [Header("Gradient Settings")]
    public bool useCodeGradient = false;
    public Gradient codeGradient;
    public Texture2D gradientTexture;
    public bool saveGradientTexture = false;

    Mesh m_CubeMesh;
    MaterialPropertyBlock props;
    RenderParams m_renderParams;

    void SetPropertyBlockSettings()
    {
        if (props == null)
        {
            props = new MaterialPropertyBlock();
        }
        if (texture)
        {
            props.SetTexture(textureName, texture);
        }
        props.SetColor(colorName, color);

        if (useCodeGradient && codeGradient != null)
        {
            if (gradientTexture == null || gradientTexture.width == 0)
            {
                gradientTexture = GenerateGradientTexture(codeGradient);
            }
            props.SetTexture("_Gradient", gradientTexture);
        }
        else if (m_Material.HasProperty("_Gradient"))
        {
            props.SetTexture("_Gradient", m_Material.GetTexture("_Gradient"));
        }
    }

    [Button("Generate Gradient Texture")]
    private Texture2D GenerateGradientTexture(Gradient gradient)
    {
        const int width = 256;
        Texture2D tex = new Texture2D(width, 1, TextureFormat.RGBA32, false);
        for (int i = 0; i < width; i++)
        {
            float t = (float)i / (width - 1);
            Color color = gradient.Evaluate(t);
            tex.SetPixel(i, 0, color);
        }
        tex.Apply();

        if (saveGradientTexture)
        {
            SaveGradientTexture(); // Call parameterless method
        }

        return tex;
    }

    [Button("Save Gradient Texture")]
    private void SaveGradientTexture()
    {
        if (gradientTexture == null)
        {
            Debug.LogWarning("No gradient texture to save!");
            return;
        }

        string gradientName = "Gradient";
        if (codeGradient != null)
        {
            Color startColor = codeGradient.Evaluate(0f);
            Color endColor = codeGradient.Evaluate(1f);
            gradientName = $"Gradient_{startColor.ToString("F2").Replace(".", "")}_{endColor.ToString("F2").Replace(".", "")}";
        }
        else
        {
            gradientName = $"Gradient_{System.DateTime.Now:yyyyMMdd_HHmmss}";
        }

        gradientName = System.Text.RegularExpressions.Regex.Replace(gradientName, "[^a-zA-Z0-9_-]", "");

        string path = EditorUtility.SaveFilePanel("Save Gradient Texture", "Assets", gradientName, "png");
        if (!string.IsNullOrEmpty(path))
        {
            byte[] bytes = gradientTexture.EncodeToPNG();
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.Refresh();
            Debug.Log($"Gradient texture saved at: {path}");
        }
    }

    private void OnValidate()
    {
        SetPropertyBlockSettings();
    }

    public void OnEnable()
    {
        SetPropertyBlockSettings();
        m_CubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        m_renderParams = new(m_Material) { matProps = props, receiveShadows = false };
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(Decal))]
    public class DecalEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            Decal decal = (Decal)target;

            GUILayout.Space(10);
            if (GUILayout.Button("Generate Gradient Texture"))
            {
                decal.gradientTexture = decal.GenerateGradientTexture(decal.codeGradient);
                decal.SetPropertyBlockSettings();
                EditorUtility.SetDirty(decal);
            }

            if (GUILayout.Button("Save Gradient Texture"))
            {
                decal.SaveGradientTexture();
                EditorUtility.SetDirty(decal);
            }
        }
    }

    private void DrawGizmo(bool selected)
    {
        var col = new Color(0.0f, 0.7f, 1f, 1.0f);
        col.a = selected ? 0.3f : 0.1f;
        Gizmos.color = col;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(Vector3.zero, Vector3.one);
        col.a = selected ? 0.5f : 0.05f;
        Gizmos.color = col;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Handles.matrix = transform.localToWorldMatrix;
        Handles.DrawBezier(Vector3.zero, Vector3.down, Vector3.zero, Vector3.down, Color.red, null, selected ? 4f : 2f);
    }

    public void OnDrawGizmos()
    {
        DrawGizmo(false);
    }

    public void OnDrawGizmosSelected()
    {
        DrawGizmo(true);
    }
#endif

    void Update()
    {
        Graphics.RenderMesh(m_renderParams, m_CubeMesh, 0, transform.localToWorldMatrix);
    }
}
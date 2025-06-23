using UnityEngine;
using NaughtyAttributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class PortalGradientGenerator : MonoBehaviour
{
    [Header("Material Settings")]
    public Material portalMaterial;

    [Header("Gradient Settings")]
    public Gradient gradient;
    public Texture2D gradientTexture;
    [Tooltip("Save the generated gradient texture as an asset")]
    public bool saveGradientTexture = false;

    private MaterialPropertyBlock props;

    private void SetPropertyBlockSettings()
    {
        if (props == null)
        {
            props = new MaterialPropertyBlock();
        }

        if (gradient != null)
        {
            if (gradientTexture == null || gradientTexture.width == 0)
            {
                gradientTexture = GenerateGradientTexture(gradient);
            }
            props.SetTexture("_GradientTexture", gradientTexture);
        }
        else if (portalMaterial != null && portalMaterial.HasProperty("_GradientTexture"))
        {
            props.SetTexture("_GradientTexture", portalMaterial.GetTexture("_GradientTexture"));
        }

        var renderer = GetComponent<Renderer>();
        if (renderer != null && portalMaterial != null)
        {
            renderer.SetPropertyBlock(props);
        }
    }

    [Button("Generate Gradient Texture")]
    private Texture2D GenerateGradientTexture(Gradient gradient)
    {
        const int width = 256;
        Texture2D tex = new Texture2D(width, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        for (int i = 0; i < width; i++)
        {
            float t = (float)i / (width - 1);
            Color color = gradient.Evaluate(t);
            tex.SetPixel(i, 0, color);
        }
        tex.Apply();

        if (saveGradientTexture)
        {
            // FIX: This call must be wrapped because the method it calls is editor-only
#if UNITY_EDITOR
            SaveGradientTexture();
#endif
        }

        return tex;
    }

    // FIX: This entire method and its button attribute must be wrapped in #if UNITY_EDITOR
#if UNITY_EDITOR
    [Button("Save Gradient Texture")]
    private void SaveGradientTexture()
    {
        if (gradientTexture == null)
        {
            Debug.LogWarning("No gradient texture to save!");
            return;
        }

        string gradientName = "PortalGradient";
        if (gradient != null)
        {
            Color startColor = gradient.Evaluate(0f);
            Color endColor = gradient.Evaluate(1f);
            gradientName = $"PortalGradient_{startColor.ToString("F2").Replace(".", "")}_{endColor.ToString("F2").Replace(".", "")}";
        }
        else
        {
            gradientName = $"PortalGradient_{System.DateTime.Now:yyyyMMdd_HHmmss}";
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
#endif

    private void OnValidate()
    {
        SetPropertyBlockSettings();
    }

    private void OnEnable()
    {
        SetPropertyBlockSettings();
    }

    // The rest of your editor code is already correctly wrapped.
#if UNITY_EDITOR
    [CustomEditor(typeof(PortalGradientGenerator))]
    public class PortalGradientGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            PortalGradientGenerator generator = (PortalGradientGenerator)target;

            GUILayout.Space(10);
            if (GUILayout.Button("Generate Gradient Texture"))
            {
                generator.gradientTexture = generator.GenerateGradientTexture(generator.gradient);
                generator.SetPropertyBlockSettings();
                EditorUtility.SetDirty(generator);
            }

            if (GUILayout.Button("Save Gradient Texture"))
            {
                generator.SaveGradientTexture();
                EditorUtility.SetDirty(generator);
            }
        }
    }

    private void DrawGizmo(bool selected)
    {
        var col = new Color(0.0f, 0.7f, 1f, 1.0f);
        col.a = selected ? 0.3f : 0.1f;
        Gizmos.color = col;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawSphere(Vector3.zero, 0.5f); // Sphere to represent portal
        col.a = selected ? 0.5f : 0.05f;
        Gizmos.color = col;
        Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
    }

    private void OnDrawGizmos()
    {
        DrawGizmo(false);
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmo(true);
    }
#endif
}
using UnityEngine;
using System.IO;

public class BakingController : MonoBehaviour
{
    public enum BakeType { NormalFromDepth, Curvature, AOFromDepth, Height, BentNormal }

    [Header("Đối tượng cần Bake")]
    public MeshRenderer objectToBake;

    [Header("Cài đặt Baking")]
    public BakeType bakeType = BakeType.NormalFromDepth;
    public int resolution = 2048;

    [Header("Cài đặt Shader")]
    public Shader uberBakeShader;

    [Header("Cài đặt AO")]
    [Range(0, 1)] public float aoIntensity = 1.0f;
    [Range(0, 0.5f)] public float aoRadius = 0.1f;

    [Header("Đường dẫn lưu file")]
    public string savePath = "Assets/BakedTextures";

    private Material bakeMaterial;

    [ContextMenu("Thực hiện Bake")]
    void Bake()
    {
        if (objectToBake == null || uberBakeShader == null)
        {
            Debug.LogError("Vui lòng gán Object to Bake và Uber Bake Shader.");
            return;
        }

        // Tạo Material từ shader
        if (bakeMaterial == null)
        {
            bakeMaterial = new Material(uberBakeShader);
        }

        // Tạo một camera tạm thời để bake
        GameObject bakeCamGo = new GameObject("BakeCam");
        Camera bakeCam = bakeCamGo.AddComponent<Camera>();
        bakeCam.orthographic = true;
        bakeCam.orthographicSize = 0.5f;
        bakeCam.transform.position = new Vector3(0.5f, 0.5f, -10f);
        bakeCam.clearFlags = CameraClearFlags.SolidColor;
        bakeCam.backgroundColor = Color.black;
        bakeCam.enabled = false;

        // Tạo Render Texture
        RenderTexture renderTexture = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGBFloat);
        bakeCam.targetTexture = renderTexture;

        // Cấu hình material dựa trên loại bake
        int passIndex = (int)bakeType;
        bakeMaterial.SetFloat("_AOIntensity", aoIntensity);
        bakeMaterial.SetFloat("_AORadius", aoRadius);

        // Render đối tượng với shader bake
        Graphics.SetRenderTarget(renderTexture);
        GL.Clear(true, true, Color.black);
        bakeMaterial.SetPass(passIndex);
        Graphics.DrawMeshNow(objectToBake.GetComponent<MeshFilter>().sharedMesh, objectToBake.transform.localToWorldMatrix);

        // Lưu texture
        SaveTexture(renderTexture, $"{objectToBake.name}_{bakeType}.png");

        // Dọn dẹp
        DestroyImmediate(bakeCamGo);
        RenderTexture.active = null;
        DestroyImmediate(renderTexture);
    }

    void SaveTexture(RenderTexture rt, string fileName)
    {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();

        byte[] bytes = tex.EncodeToPNG();
        DestroyImmediate(tex);

        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }

        File.WriteAllBytes(Path.Combine(savePath, fileName), bytes);
        Debug.Log($"Đã lưu texture tại: {Path.Combine(savePath, fileName)}");

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh();
#endif
    }
}
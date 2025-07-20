using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class VAT_BakerEditorWindow : EditorWindow
{
    private GameObject _sourceObject;
    private const int FRAMERATE_OVERRIDE = 30;

    [MenuItem("Tools/BillTheDev/VAT Baker")]
    public static void ShowWindow()
    {
        GetWindow<VAT_BakerEditorWindow>("VAT Baker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Vertex Animation Texture Baker", EditorStyles.boldLabel);
        _sourceObject = EditorGUILayout.ObjectField("Source GameObject (Prefab/Scene)", _sourceObject, typeof(GameObject), true) as GameObject;

        if (GUILayout.Button("Bake Animations") && IsSourceValid())
        {
            BakeAnimationData();
        }
    }

    private bool IsSourceValid()
    {
        if (_sourceObject == null)
        {
            EditorUtility.DisplayDialog("Bake Error", "Please assign a source GameObject.", "OK");
            return false;
        }
        if (_sourceObject.GetComponentInChildren<SkinnedMeshRenderer>() == null)
        {
            EditorUtility.DisplayDialog("Bake Error", "Source GameObject must have a SkinnedMeshRenderer.", "OK");
            return false;
        }
        var animator = _sourceObject.GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null || animator.runtimeAnimatorController.animationClips.Length == 0)
        {
            EditorUtility.DisplayDialog("Bake Error", "Source GameObject must have an Animator with clips.", "OK");
            return false;
        }
        return true;
    }

    private void BakeAnimationData()
    {
        string savePath = EditorUtility.SaveFilePanelInProject("Save Baked Assets", _sourceObject.name + "_VAT", "", "Enter a base name for baked assets");
        if (string.IsNullOrEmpty(savePath)) return;

        string directory = Path.GetDirectoryName(savePath);
        string baseName = Path.GetFileNameWithoutExtension(savePath);

        var sourceSkinnedRenderer = _sourceObject.GetComponentInChildren<SkinnedMeshRenderer>();
        var animationClips = _sourceObject.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips.Where(c => !c.legacy && c.length > 0).Distinct().ToArray();

        if (animationClips.Length == 0)
        {
            EditorUtility.DisplayDialog("Bake Error", "No valid animation clips found in the Animator Controller.", "OK");
            return;
        }

        var tempInstance = Instantiate(_sourceObject);
        tempInstance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        tempInstance.transform.localScale = Vector3.one;

        try
        {
            EditorUtility.DisplayProgressBar("VAT Baker", "1/4: Calculating Total Animation Bounds...", 0.1f);
            var totalAnimationBounds = CalculateTotalLocalSpaceBounds(tempInstance, animationClips);

            EditorUtility.DisplayProgressBar("VAT Baker", "2/4: Baking Animations to Texture...", 0.4f);
            var (positionTexture, clipInfos) = BakeAnimationsToTexture(tempInstance, animationClips, totalAnimationBounds);

            EditorUtility.DisplayProgressBar("VAT Baker", "3/4: Creating Final Assets...", 0.8f);
            var bakedMesh = CreateMeshWithVertexIdUVs(sourceSkinnedRenderer.sharedMesh, totalAnimationBounds, baseName);
            var animationData = CreateAnimationDataAsset(bakedMesh, positionTexture, totalAnimationBounds, clipInfos, baseName);
            var material = CreateOptimizedMaterial(baseName);

            EditorUtility.DisplayProgressBar("VAT Baker", "4/4: Saving Assets to Disk...", 0.95f);
            SaveAllAssets(directory, baseName, animationData, bakedMesh, positionTexture, material);

            EditorUtility.DisplayDialog("Success", $"VAT assets for '{baseName}' baked successfully at {directory}", "OK");
            Selection.activeObject = animationData;
            EditorGUIUtility.PingObject(animationData);
        }
        finally
        {
            if (tempInstance != null) DestroyImmediate(tempInstance);
            EditorUtility.ClearProgressBar();
        }
    }

    private Bounds CalculateTotalLocalSpaceBounds(GameObject instance, AnimationClip[] clips)
    {
        var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
        var totalBounds = new Bounds();
        var tempBakedMesh = new Mesh();
        bool first = true;

        foreach (var clip in clips)
        {
            float timeStep = 1.0f / FRAMERATE_OVERRIDE;
            for (float time = 0; time <= clip.length; time += timeStep)
            {
                clip.SampleAnimation(instance, time);
                renderer.BakeMesh(tempBakedMesh, true);
                var meshBounds = tempBakedMesh.bounds;

                if (first)
                {
                    totalBounds = meshBounds;
                    first = false;
                }
                else
                {
                    totalBounds.Encapsulate(meshBounds);
                }
            }
        }
        totalBounds.Expand(0.01f); // Thêm một vùng đệm nhỏ để đảm bảo an toàn
        DestroyImmediate(tempBakedMesh);
        return totalBounds;
    }

    private (Texture2D, List<VAT_AnimationData.ClipInfo>) BakeAnimationsToTexture(GameObject instance, AnimationClip[] clips, Bounds totalBounds)
    {
        var clipInfos = new List<VAT_AnimationData.ClipInfo>();
        var renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
        int vertexCount = renderer.sharedMesh.vertexCount;
        var tempBakedMesh = new Mesh();
        var allFramesData = new List<Color[]>();
        int totalFrames = 0;

        foreach (var clip in clips)
        {
            int frameCount = Mathf.Max(2, Mathf.CeilToInt(clip.length * FRAMERATE_OVERRIDE)); // Luôn có ít nhất 2 frame
            clipInfos.Add(new VAT_AnimationData.ClipInfo { name = clip.name, startFrame = totalFrames, frameCount = frameCount, duration = clip.length, wrapMode = clip.wrapMode });

            for (int frame = 0; frame < frameCount; frame++)
            {
                float sampleTime = (frame / (float)(frameCount - 1)) * clip.length;
                clip.SampleAnimation(instance, sampleTime);
                renderer.BakeMesh(tempBakedMesh, true);

                var frameColors = new Color[vertexCount];
                var vertices = tempBakedMesh.vertices;
                for (int i = 0; i < vertexCount; i++)
                {
                    frameColors[i] = EncodeLocalPositionToColor(vertices[i], totalBounds);
                }
                allFramesData.Add(frameColors);
            }
            totalFrames += frameCount;
        }

        DestroyImmediate(tempBakedMesh);

        var positionTexture = new Texture2D(vertexCount, totalFrames, TextureFormat.RGBAHalf, false)
        {
            name = "VAT_PositionTexture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        for (int y = 0; y < totalFrames; y++)
        {
            positionTexture.SetPixels(0, y, vertexCount, 1, allFramesData[y]);
        }
        positionTexture.Apply(false, true); // No mips, non-readable
        return (positionTexture, clipInfos);
    }

    // ĐÂY LÀ HÀM SỬA LỖI QUAN TRỌNG NHẤT
    private Mesh CreateMeshWithVertexIdUVs(Mesh originalMesh, Bounds animationBounds, string baseName)
    {
        var newMesh = new Mesh
        {
            name = $"{baseName}_Mesh",
            // Giữ lại dữ liệu gốc, vì vị trí thực sự sẽ được tính trong shader
            vertices = originalMesh.vertices,
            normals = originalMesh.normals,
            tangents = originalMesh.tangents,
            uv = originalMesh.uv,
            triangles = originalMesh.triangles,
            // SỬA LỖI CHÍ MẠNG: Gán bounds bao trọn toàn bộ animation
            // Điều này ngăn Unity culling đối tượng một cách sai lầm.
            bounds = animationBounds
        };

        int vertexCount = originalMesh.vertexCount;
        var vertexIdUVs = new Vector2[vertexCount];
        float invTexWidth = 1.0f / vertexCount;
        float halfTexel = 0.5f * invTexWidth;

        for (int i = 0; i < vertexCount; i++)
        {
            // Tọa độ U được chuẩn hóa và dịch nửa texel để sample chính xác vào giữa pixel
            // Tránh lỗi bilinear filtering theo chiều ngang
            vertexIdUVs[i] = new Vector2(i * invTexWidth + halfTexel, 0);
        }

        // Gán mảng tọa độ U vào kênh UV thứ hai (TEXCOORD1)
        newMesh.SetUVs(1, vertexIdUVs);
        newMesh.UploadMeshData(true); // Đánh dấu không thể đọc từ script để tăng hiệu năng
        return newMesh;
    }

    private Color EncodeLocalPositionToColor(Vector3 localPosition, Bounds totalBounds)
    {
        float r = Mathf.InverseLerp(totalBounds.min.x, totalBounds.max.x, localPosition.x);
        float g = Mathf.InverseLerp(totalBounds.min.y, totalBounds.max.y, localPosition.y);
        float b = Mathf.InverseLerp(totalBounds.min.z, totalBounds.max.z, localPosition.z);
        return new Color(r, g, b, 1.0f);
    }

    private VAT_AnimationData CreateAnimationDataAsset(Mesh mesh, Texture2D tex, Bounds bounds, List<VAT_AnimationData.ClipInfo> infos, string baseName)
    {
        var dataAsset = CreateInstance<VAT_AnimationData>();
        dataAsset.name = $"{baseName}_Data";
        dataAsset.bakedMesh = mesh;
        dataAsset.positionTexture = tex;
        dataAsset.positionMinBounds = bounds.min;
        dataAsset.positionMaxBounds = bounds.max;
        dataAsset.animationClips = infos;
        return dataAsset;
    }

    private Material CreateOptimizedMaterial(string baseName)
    {
        var shader = Shader.Find("BillTheDev/VAT/Optimized_VAT");
        if (shader == null)
        {
            Debug.LogError("Shader 'BillTheDev/VAT/Optimized_VAT' not found. Ensure it is compiled.");
            return null;
        }
        var material = new Material(shader) { name = $"{baseName}_Mat" };
        material.enableInstancing = true;
        return material;
    }

    private void SaveAllAssets(string dir, string name, VAT_AnimationData data, Mesh mesh, Texture2D tex, Material mat)
    {
        string dataPath = Path.Combine(dir, $"{name}_Data.asset");
        AssetDatabase.CreateAsset(data, dataPath);

        // Lưu các asset khác như là sub-asset của Data asset để gọn gàng hơn
        AssetDatabase.AddObjectToAsset(mesh, data);
        AssetDatabase.AddObjectToAsset(tex, data);
        AssetDatabase.AddObjectToAsset(mat, data);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class VertexColorPainter : EditorWindow
{
    private enum PaintMode { ReplaceAll, ReplaceChannel }
    private enum TargetChannel { R, G, B, A }

    private const string TargetShaderName = "Custom/ToonUberShader_WithEngine_Fixed";
    private GameObject targetObject;
    private Color paintColor = Color.white;
    private PaintMode paintMode = PaintMode.ReplaceAll;
    private TargetChannel targetChannel = TargetChannel.R;

    [MenuItem("Tools/BillTheDev/PERFECTION Vertex Color Painter")]
    public static void ShowWindow() => GetWindow<VertexColorPainter>("PERFECTION VC Painter");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("PERFECTION Vertex Color Painter", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("The definitive vertex painter. Color values are clamped to the [0, 1] range, matching professional workflows like Blender.", MessageType.Info);

        targetObject = (GameObject)EditorGUILayout.ObjectField("Target GameObject", targetObject, typeof(GameObject), true);
        EditorGUILayout.Space();

        paintMode = (PaintMode)EditorGUILayout.EnumPopup("Paint Mode", paintMode);

        // **THAY ĐỔI QUAN TRỌNG NHẤT**
        // Sử dụng overload của ColorField để tắt HDR, ép dải màu về [0, 1]
        paintColor = EditorGUILayout.ColorField(new GUIContent("Paint Color"), paintColor, true, true, false);

        if (paintMode == PaintMode.ReplaceChannel)
        {
            targetChannel = (TargetChannel)EditorGUILayout.EnumPopup("Target Channel", targetChannel);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Presets (Replace All)", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.HelpBox("These presets will ERASE all existing vertex colors and replace them with a single color.", MessageType.None);
        if (GUILayout.Button("Set All To Piston (Red)")) SetAndReplaceAll(Color.red);
        if (GUILayout.Button("Set All To Rotation (Green)")) SetAndReplaceAll(Color.green);
        if (GUILayout.Button("Set All To Shake (Blue)")) SetAndReplaceAll(Color.blue);
        if (GUILayout.Button("Set All To Black (Clear Animation)")) SetAndReplaceAll(Color.black);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(20);
        GUI.enabled = targetObject != null;
        if (GUILayout.Button("Analyze, Paint, and Create Prefab", GUILayout.Height(30))) InitiatePaintProcess();
        GUI.enabled = true;
    }

    private void SetAndReplaceAll(Color color)
    {
        paintMode = PaintMode.ReplaceAll;
        paintColor = color;
        Repaint();
    }

    private Mesh CreateMeshWithNewVertexColor(Mesh originalMesh)
    {
        Mesh newMesh = new Mesh
        {
            name = originalMesh.name + "_vcolor_baked",
            vertices = originalMesh.vertices,
            normals = originalMesh.normals,
            tangents = originalMesh.tangents,
            uv = originalMesh.uv,
            uv2 = originalMesh.uv2,
            subMeshCount = originalMesh.subMeshCount
        };

        for (int i = 0; i < originalMesh.subMeshCount; i++)
        {
            newMesh.SetTriangles(originalMesh.GetTriangles(i), i);
        }

        int vertexCount = originalMesh.vertexCount;
        Color32[] newColors = new Color32[vertexCount];
        Color32 colorToApply32 = paintColor;

        if (paintMode == PaintMode.ReplaceAll)
        {
            for (int i = 0; i < vertexCount; i++)
            {
                newColors[i] = colorToApply32;
            }
        }
        else // paintMode == PaintMode.ReplaceChannel
        {
            Color32[] originalColors = originalMesh.colors32;
            bool hasOriginalColors = originalColors != null && originalColors.Length == vertexCount;

            for (int i = 0; i < vertexCount; i++)
            {
                Color32 currentColor = hasOriginalColors ? originalColors[i] : (Color32)Color.black;
                switch (targetChannel)
                {
                    case TargetChannel.R: currentColor.r = colorToApply32.r; break;
                    case TargetChannel.G: currentColor.g = colorToApply32.g; break;
                    case TargetChannel.B: currentColor.b = colorToApply32.b; break;
                    case TargetChannel.A: currentColor.a = colorToApply32.a; break;
                }
                newColors[i] = currentColor;
            }
        }

        newMesh.colors32 = newColors;
        newMesh.RecalculateBounds();
        return newMesh;
    }

    private void InitiatePaintProcess()
    {
        if (targetObject == null) return;

        var renderers = targetObject.GetComponentsInChildren<Renderer>(true);
        var materialsToValidate = new List<Material>();
        foreach (var renderer in renderers)
        {
            materialsToValidate.AddRange(renderer.sharedMaterials.Where(mat => mat != null && mat.shader.name != TargetShaderName));
        }

        if (materialsToValidate.Any())
        {
            MaterialFixerWindow.ShowWindow(materialsToValidate.Distinct().ToList(), (choices) =>
            {
                if (choices != null)
                {
                    ContinuePaintProcess(choices);
                }
            });
        }
        else
        {
            ContinuePaintProcess(new Dictionary<Material, MaterialFixerWindow.FixerAction>());
        }
    }

    private void ContinuePaintProcess(Dictionary<Material, MaterialFixerWindow.FixerAction> choices)
    {
        string savePath = GetSaveDirectory();
        if (string.IsNullOrEmpty(savePath)) return;

        GameObject instanceRoot = Instantiate(targetObject);
        instanceRoot.name = targetObject.name + "_VColored";

        ApplyMaterialFixes(instanceRoot, choices);

        try
        {
            ProcessAndApplyColorToRenderers(instanceRoot, savePath);
            CreatePrefab(instanceRoot, savePath);
        }
        finally
        {
            DestroyImmediate(instanceRoot);
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        EditorUtility.DisplayDialog("Success", $"Prefab '{instanceRoot.name}.prefab' was created successfully at:\n{savePath}", "OK");
    }

    private void ApplyMaterialFixes(GameObject root, Dictionary<Material, MaterialFixerWindow.FixerAction> choices)
    {
        if (choices == null || choices.Count == 0) return;

        Shader targetShader = Shader.Find(TargetShaderName);
        if (targetShader == null) return;

        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            var currentMaterials = renderer.sharedMaterials;
            var newMaterials = new Material[currentMaterials.Length];
            bool changed = false;

            for (int i = 0; i < currentMaterials.Length; i++)
            {
                var mat = currentMaterials[i];
                if (mat != null && choices.TryGetValue(mat, out var action) && action == MaterialFixerWindow.FixerAction.FixShader)
                {
                    var newMat = new Material(targetShader) { name = mat.name + "_Fixed" };
                    if (mat.HasProperty("_BaseMap")) newMat.SetTexture("_BaseMap", mat.GetTexture("_BaseMap"));
                    if (mat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", mat.GetColor("_BaseColor"));
                    newMaterials[i] = newMat;
                    changed = true;
                }
                else
                {
                    newMaterials[i] = mat;
                }
            }
            if (changed) renderer.sharedMaterials = newMaterials;
        }
    }

    private void ProcessAndApplyColorToRenderers(GameObject root, string basePath)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        string meshDir = Path.Combine(basePath, "Meshes");
        if (!Directory.Exists(meshDir)) Directory.CreateDirectory(meshDir);

        for (int i = 0; i < renderers.Length; i++)
        {
            EditorUtility.DisplayProgressBar("Painting Meshes", $"Processing {renderers[i].name}...", (float)i / renderers.Length);
            Mesh originalMesh = GetMeshFromRenderer(renderers[i]);
            if (originalMesh == null || !originalMesh.isReadable) continue;

            Mesh newMesh = CreateMeshWithNewVertexColor(originalMesh);
            string meshPath = Path.Combine(meshDir, $"{renderers[i].gameObject.name}_{newMesh.name}.asset");
            string uniqueMeshPath = AssetDatabase.GenerateUniqueAssetPath(meshPath);

            AssetDatabase.CreateAsset(newMesh, uniqueMeshPath);
            SetMeshOnRenderer(renderers[i], newMesh);
        }
    }

    private string GetSaveDirectory()
    {
        string projectPath = Application.dataPath;
        string absolutePath = EditorUtility.OpenFolderPanel("Select Folder to Save Prefab and Meshes", projectPath, "");
        if (string.IsNullOrEmpty(absolutePath) || !absolutePath.StartsWith(projectPath))
        {
            if (!string.IsNullOrEmpty(absolutePath)) EditorUtility.DisplayDialog("Error", "Please select a folder inside the project's Assets directory.", "OK");
            return null;
        }
        return "Assets" + absolutePath.Substring(projectPath.Length);
    }

    private void CreatePrefab(GameObject rootObject, string basePath)
    {
        string prefabPath = Path.Combine(basePath, $"{rootObject.name}.prefab");
        string uniquePrefabPath = AssetDatabase.GenerateUniqueAssetPath(prefabPath);
        PrefabUtility.SaveAsPrefabAsset(rootObject, uniquePrefabPath, out bool success);
        if (success) EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(uniquePrefabPath));
    }

    private Mesh GetMeshFromRenderer(Renderer renderer)
    {
        if (renderer is SkinnedMeshRenderer smr) return smr.sharedMesh;
        if (renderer.TryGetComponent<MeshFilter>(out var mf)) return mf.sharedMesh;
        return null;
    }

    private void SetMeshOnRenderer(Renderer renderer, Mesh mesh)
    {
        if (renderer is SkinnedMeshRenderer smr) smr.sharedMesh = mesh;
        if (renderer.TryGetComponent<MeshFilter>(out var mf)) mf.sharedMesh = mesh;
    }
}
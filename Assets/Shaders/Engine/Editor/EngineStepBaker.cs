using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class EngineStepBaker : EditorWindow
{
    private enum BakeMode { LinearProjection, Radial, BoundingBoxAxis }
    private enum BoundingBoxDirection { X, Y, Z }

    private const string TargetShaderName = "Custom/ToonUberShader_WithEngine_Fixed";
    private GameObject sourceObject;
    private BakeMode bakeMode = BakeMode.LinearProjection;
    private Vector3 stepOrigin = Vector3.zero;
    private Vector3 stepDirection = Vector3.forward;
    private float stepSize = 0.5f;
    private BoundingBoxDirection boundingBoxAxis = BoundingBoxDirection.Y;

    [MenuItem("Tools/BillTheDev/ULTIMATE Engine Step Baker")]
    public static void ShowWindow() => GetWindow<EngineStepBaker>("ULTIMATE Step Baker");

    private void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    private void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    private void OnGUI()
    {
        EditorGUILayout.LabelField("ULTIMATE Engine Step Baker", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Bakes animation data into UV2, intelligently handling multi-material objects by ensuring all relevant materials use the required animation shader.", MessageType.Info);

        sourceObject = (GameObject)EditorGUILayout.ObjectField("Source GameObject", sourceObject, typeof(GameObject), true);
        EditorGUILayout.Space();
        bakeMode = (BakeMode)EditorGUILayout.EnumPopup("Bake Mode", bakeMode);

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.BeginVertical("box");
        switch (bakeMode)
        {
            case BakeMode.LinearProjection:
                stepOrigin = EditorGUILayout.Vector3Field("Step Origin", stepOrigin);
                stepDirection = EditorGUILayout.Vector3Field("Step Direction", stepDirection.normalized);
                stepSize = EditorGUILayout.FloatField("Step Size", stepSize);
                break;
            case BakeMode.Radial:
                stepOrigin = EditorGUILayout.Vector3Field("Radial Origin", stepOrigin);
                stepSize = EditorGUILayout.FloatField("Step Size (Distance)", stepSize);
                break;
            case BakeMode.BoundingBoxAxis:
                boundingBoxAxis = (BoundingBoxDirection)EditorGUILayout.EnumPopup("Baking Axis", boundingBoxAxis);
                stepSize = EditorGUILayout.FloatField("Step Size", stepSize);
                EditorGUILayout.HelpBox("Origin/Direction are calculated from the object's bounds.", MessageType.None);
                break;
        }
        EditorGUILayout.EndVertical();
        if (EditorGUI.EndChangeCheck()) SceneView.RepaintAll();

        EditorGUILayout.Space();
        GUI.enabled = sourceObject != null;
        if (GUILayout.Button("Analyze, Bake, and Create Prefab")) BakeAndCreatePrefab();
        GUI.enabled = true;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (sourceObject == null) return;
        switch (bakeMode)
        {
            case BakeMode.LinearProjection: DrawLinearProjectionGizmos(); break;
            case BakeMode.Radial: DrawRadialGizmos(); break;
        }
    }

    private void DrawLinearProjectionGizmos()
    {
        EditorGUI.BeginChangeCheck();
        Vector3 newOrigin = Handles.PositionHandle(stepOrigin, Quaternion.identity);
        Quaternion newRotation = Handles.RotationHandle(Quaternion.LookRotation(stepDirection), stepOrigin);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Modify Bake Gizmos");
            stepOrigin = newOrigin;
            stepDirection = newRotation * Vector3.forward;
            Repaint();
        }
        Handles.color = Color.yellow;
        Handles.DrawDottedLine(stepOrigin - stepDirection.normalized * 5, stepOrigin + stepDirection.normalized * 5, 4);
    }

    private void DrawRadialGizmos()
    {
        EditorGUI.BeginChangeCheck();
        Vector3 newOrigin = Handles.PositionHandle(stepOrigin, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(this, "Move Radial Origin");
            stepOrigin = newOrigin;
            Repaint();
        }
        Handles.color = new Color(0, 1, 1, 0.5f);
        for (int i = 1; i < 6; i++) Handles.DrawWireDisc(stepOrigin, Camera.current.transform.forward, stepSize * i);
    }

    private void BakeAndCreatePrefab()
    {
        if (sourceObject == null) return;

        string savePath = GetSaveDirectory();
        if (string.IsNullOrEmpty(savePath)) return;

        GameObject instanceRoot = Instantiate(sourceObject);
        instanceRoot.name = sourceObject.name + "_StepBaked";

        if (!ValidateAndFixMaterials(instanceRoot))
        {
            DestroyImmediate(instanceRoot);
            EditorUtility.DisplayDialog("Cancelled", "Bake process was cancelled by the user.", "OK");
            return;
        }

        try
        {
            ProcessAndBakeRenderers(instanceRoot, savePath);
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

    private bool ValidateAndFixMaterials(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        var materialsToFix = new List<Material>();

        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat != null && mat.shader.name != TargetShaderName)
                {
                    materialsToFix.Add(mat);
                }
            }
        }

        if (materialsToFix.Count == 0) return true;

        string message = $"Found {materialsToFix.Count} material(s) not using the required '{TargetShaderName}' shader. Without this shader, the animation data will not be used.\n\nHow would you like to proceed?";
        int choice = EditorUtility.DisplayDialogComplex("Shader Mismatch Detected", message, "Auto-Fix and Continue (Recommended)", "Cancel", "Continue Without Fixing");

        switch (choice)
        {
            case 0: // Auto-Fix
                Shader targetShader = Shader.Find(TargetShaderName);
                if (targetShader == null)
                {
                    EditorUtility.DisplayDialog("Error", $"Could not find the target shader '{TargetShaderName}'. Cannot auto-fix.", "OK");
                    return false;
                }
                foreach (var renderer in renderers)
                {
                    var currentMaterials = renderer.sharedMaterials;
                    var newMaterials = new Material[currentMaterials.Length];
                    bool changed = false;
                    for (int i = 0; i < currentMaterials.Length; i++)
                    {
                        var mat = currentMaterials[i];
                        if (mat != null && mat.shader.name != TargetShaderName)
                        {
                            var newMat = new Material(targetShader);
                            if (mat.HasProperty("_BaseMap")) newMat.SetTexture("_BaseMap", mat.GetTexture("_BaseMap"));
                            if (mat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", mat.GetColor("_BaseColor"));
                            newMat.name = mat.name + "_Fixed";
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
                return true;
            case 1: // Cancel
                return false;
            case 2: // Continue Without Fixing
                return true;
        }
        return false;
    }

    private void ProcessAndBakeRenderers(GameObject root, string basePath)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0) return;

        string meshDir = Path.Combine(basePath, "Meshes");
        if (!Directory.Exists(meshDir)) Directory.CreateDirectory(meshDir);

        Vector3 bakeOrigin = stepOrigin;
        Vector3 bakeDirection = stepDirection.normalized;

        if (bakeMode == BakeMode.BoundingBoxAxis)
        {
            Bounds bounds = CalculateCombinedBounds(root);
            bakeOrigin = bounds.center;
            if (boundingBoxAxis == BoundingBoxDirection.X) bakeDirection = Vector3.right;
            if (boundingBoxAxis == BoundingBoxDirection.Y) bakeDirection = Vector3.up;
            if (boundingBoxAxis == BoundingBoxDirection.Z) bakeDirection = Vector3.forward;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            EditorUtility.DisplayProgressBar("Baking Meshes", $"Processing {renderers[i].name}...", (float)i / renderers.Length);
            Mesh originalMesh = GetMeshFromRenderer(renderers[i]);
            if (originalMesh == null || !originalMesh.isReadable) continue;

            Mesh newMesh = CreateBakedMesh(originalMesh, renderers[i].transform, bakeOrigin, bakeDirection);
            string meshPath = Path.Combine(meshDir, $"{renderers[i].gameObject.name}_{newMesh.name}.asset");
            string uniqueMeshPath = AssetDatabase.GenerateUniqueAssetPath(meshPath);

            AssetDatabase.CreateAsset(newMesh, uniqueMeshPath);
            SetMeshOnRenderer(renderers[i], newMesh);
        }
    }

    private Mesh CreateBakedMesh(Mesh originalMesh, Transform meshTransform, Vector3 origin, Vector3 direction)
    {
        Mesh newMesh = new Mesh
        {
            name = originalMesh.name + "_StepBaked",
            vertices = originalMesh.vertices,
            normals = originalMesh.normals,
            tangents = originalMesh.tangents,
            uv = originalMesh.uv,
            colors32 = originalMesh.colors32,
            subMeshCount = originalMesh.subMeshCount
        };
        for (int i = 0; i < originalMesh.subMeshCount; i++)
        {
            newMesh.SetTriangles(originalMesh.GetTriangles(i), i);
        }

        Vector3[] vertices = originalMesh.vertices;
        var uv2Data = new List<Vector2>(vertices.Length);

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = meshTransform.TransformPoint(vertices[i]);
            float distance = 0;
            if (bakeMode == BakeMode.LinearProjection || bakeMode == BakeMode.BoundingBoxAxis)
                distance = Vector3.Dot(worldPos - origin, direction);
            else if (bakeMode == BakeMode.Radial)
                distance = Vector3.Distance(worldPos, origin);

            uv2Data.Add(new Vector2(Mathf.Floor(distance / stepSize), 0));
        }

        newMesh.SetUVs(1, uv2Data);
        newMesh.RecalculateBounds();
        return newMesh;
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

    private Bounds CalculateCombinedBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }
}
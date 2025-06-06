using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class VATBaker : EditorWindow
{
    private SkinnedMeshRenderer sourceSkinnedMeshRenderer;
    private AnimationClip animationClip;
    private int frameRate = 30;
    private bool bakeNormals = true;
    private string savePath = "Assets/BakedVAT/";

    private Mesh bakedMesh; // To store the original mesh without skinning

    [MenuItem("Tools/VAT Baker")]
    public static void ShowWindow()
    {
        GetWindow<VATBaker>("VAT Baker");
    }

    private void OnGUI()
    {
        GUILayout.Label("Vertex Animation Texture Baker", EditorStyles.boldLabel);

        sourceSkinnedMeshRenderer = (SkinnedMeshRenderer)EditorGUILayout.ObjectField(
            "Source Skinned Mesh", sourceSkinnedMeshRenderer, typeof(SkinnedMeshRenderer), true);

        animationClip = (AnimationClip)EditorGUILayout.ObjectField(
            "Animation Clip", animationClip, typeof(AnimationClip), false);

        frameRate = EditorGUILayout.IntSlider("Frame Rate", frameRate, 10, 120);
        bakeNormals = EditorGUILayout.Toggle("Bake Normals", bakeNormals);

        EditorGUILayout.Space();

        savePath = EditorGUILayout.TextField("Save Path", savePath);
        if (GUILayout.Button("Browse Save Path"))
        {
            string newPath = EditorUtility.OpenFolderPanel("Select Save Path", savePath, "");
            if (!string.IsNullOrEmpty(newPath))
            {
                savePath = "Assets" + newPath.Replace(Application.dataPath, "");
                if (!savePath.EndsWith("/")) savePath += "/";
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Bake Vertex Animation"))
        {
            if (sourceSkinnedMeshRenderer == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign a Skinned Mesh Renderer.", "OK");
                return;
            }
            if (animationClip == null)
            {
                EditorUtility.DisplayDialog("Error", "Please assign an Animation Clip.", "OK");
                return;
            }

            BakeAnimation();
        }
    }

    private void BakeAnimation()
    {
        EditorUtility.DisplayProgressBar("Baking VAT", "Initializing...", 0f);

        // Ensure save path exists
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
            AssetDatabase.Refresh();
        }

        string assetName = sourceSkinnedMeshRenderer.name + "_" + animationClip.name;

        // 1. Setup temporary GameObject for baking
        GameObject tempGO = new GameObject("TEMP_VAT_BAKER_" + assetName);
        tempGO.SetActive(false); // Keep it inactive during baking
        SkinnedMeshRenderer tempSMR = tempGO.AddComponent<SkinnedMeshRenderer>();
        tempSMR.sharedMesh = sourceSkinnedMeshRenderer.sharedMesh;
        tempSMR.bones = sourceSkinnedMeshRenderer.bones;
        tempSMR.rootBone = sourceSkinnedMeshRenderer.rootBone;
        tempSMR.sharedMaterials = sourceSkinnedMeshRenderer.sharedMaterials; // Copy materials too if needed for preview

        Animator animator = tempGO.AddComponent<Animator>();
        // Create a temporary AnimatorController
        UnityEditor.Animations.AnimatorController tempController = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(savePath + "TEMP_AnimatorController.controller");
        tempController.AddLayer("Base Layer");
        tempController.AddMotion(animationClip, 0);
        animator.runtimeAnimatorController = tempController;

        // 2. Determine bake parameters
        int vertexCount = sourceSkinnedMeshRenderer.sharedMesh.vertexCount;
        float animationLength = animationClip.length;
        int frameCount = Mathf.CeilToInt(animationLength * frameRate);

        if (vertexCount == 0)
        {
            EditorUtility.DisplayDialog("Error", "Source mesh has no vertices.", "OK");
            DestroyImmediate(tempGO);
            AssetDatabase.DeleteAsset(savePath + "TEMP_AnimatorController.controller");
            EditorUtility.ClearProgressBar();
            return;
        }

        Debug.Log($"Baking {vertexCount} vertices over {frameCount} frames ({animationLength}s at {frameRate}fps)");

        // 3. Create Textures
        Texture2D positionTexture = new Texture2D(frameCount, vertexCount, TextureFormat.RGBAFloat, false);
        positionTexture.name = assetName + "_PositionTex";
        positionTexture.filterMode = FilterMode.Point; // Crucial for direct sampling
        positionTexture.wrapMode = TextureWrapMode.Clamp; // Prevent wrapping issues

        Texture2D normalTexture = null;
        if (bakeNormals)
        {
            normalTexture = new Texture2D(frameCount, vertexCount, TextureFormat.RGBAFloat, false);
            normalTexture.name = assetName + "_NormalTex";
            normalTexture.filterMode = FilterMode.Point;
            normalTexture.wrapMode = TextureWrapMode.Clamp;
        }

        Mesh bakedMeshInstance = new Mesh(); // A temporary mesh to bake into
        bakedMeshInstance.name = assetName + "_BakedMesh";

        // 4. Bake Loop
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();

        for (int i = 0; i < frameCount; i++)
        {
            EditorUtility.DisplayProgressBar("Baking VAT", $"Processing frame {i + 1}/{frameCount}", (float)i / frameCount);

            float time = (float)i / frameRate;

            // Set animation time and force update
            // Using Play with a normalized time (0 to 1) for the animation clip
            animator.Play(animationClip.name, 0, time / animationLength);
            animator.Update(0); // Forces a single frame update

            // Bake the current state of the skinned mesh
            tempSMR.BakeMesh(bakedMeshInstance);

            // Get vertex data
            bakedMeshInstance.GetVertices(vertices);
            if (bakeNormals)
            {
                bakedMeshInstance.GetNormals(normals);
            }

            // Store data in textures
            for (int j = 0; j < vertexCount; j++)
            {
                // Position: Store in RGBA as XYZ (W can be 1 or unused)
                positionTexture.SetPixel(i, j, new Color(vertices[j].x, vertices[j].y, vertices[j].z, 1.0f));

                // Normal: Store in RGBA as XYZ (W can be 1 or unused)
                if (bakeNormals)
                {
                    normalTexture.SetPixel(i, j, new Color(normals[j].x, normals[j].y, normals[j].z, 1.0f));
                }
            }
        }

        positionTexture.Apply();
        if (bakeNormals)
        {
            normalTexture.Apply();
        }

        // 5. Save Assets
        AssetDatabase.CreateAsset(positionTexture, savePath + positionTexture.name + ".asset");
        if (bakeNormals)
        {
            AssetDatabase.CreateAsset(normalTexture, savePath + normalTexture.name + ".asset");
        }

        // Create a static mesh from the original shared mesh (no skinning info needed for VAT playback)
        Mesh staticMesh = new Mesh();
        staticMesh.name = assetName + "_StaticMesh";
        staticMesh.vertices = sourceSkinnedMeshRenderer.sharedMesh.vertices;
        staticMesh.triangles = sourceSkinnedMeshRenderer.sharedMesh.triangles;
        staticMesh.uv = sourceSkinnedMeshRenderer.sharedMesh.uv;
        staticMesh.normals = sourceSkinnedMeshRenderer.sharedMesh.normals; // Keep original normals as fallback or for shader
        staticMesh.tangents = sourceSkinnedMeshRenderer.sharedMesh.tangents;
        staticMesh.subMeshCount = sourceSkinnedMeshRenderer.sharedMesh.subMeshCount;
        for (int i = 0; i < sourceSkinnedMeshRenderer.sharedMesh.subMeshCount; i++)
        {
            staticMesh.SetTriangles(sourceSkinnedMeshRenderer.sharedMesh.GetTriangles(i), i);
        }
        staticMesh.RecalculateBounds();
        AssetDatabase.CreateAsset(staticMesh, savePath + staticMesh.name + ".asset");
        this.bakedMesh = staticMesh; // Store reference for potential automatic setup

        // 6. Create Material
        Shader vatShader = Shader.Find("VAT/VertexAnimationURP");
        if (vatShader == null)
        {
            EditorUtility.DisplayDialog("Error", "VAT/VertexAnimationURP shader not found! Please create the shader first.", "OK");
            // Clean up temporary assets before exiting
            DestroyImmediate(tempGO);
            AssetDatabase.DeleteAsset(savePath + "TEMP_AnimatorController.controller");
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(positionTexture));
            if (bakeNormals) AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(normalTexture));
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(staticMesh));
            EditorUtility.ClearProgressBar();
            return;
        }

        Material vatMaterial = new Material(vatShader);
        vatMaterial.name = assetName + "_VAT_Material";
        vatMaterial.SetTexture("_VAT_PositionTex", positionTexture);
        vatMaterial.SetFloat("_VAT_FrameCount", frameCount);
        vatMaterial.SetFloat("_VAT_VertexCount", vertexCount); // Pass vertex count for V coord calculation in shader
        vatMaterial.SetFloat("_VAT_AnimationLength", animationLength);

        if (bakeNormals)
        {
            vatMaterial.SetTexture("_VAT_NormalTex", normalTexture);
            vatMaterial.EnableKeyword("_VAT_BAKE_NORMALS"); // Enable keyword for shader
        }
        else
        {
            vatMaterial.DisableKeyword("_VAT_BAKE_NORMALS");
        }

        // Set default PBR properties for the new material
        vatMaterial.SetColor("_BaseColor", Color.white);
        vatMaterial.SetFloat("_Metallic", 0.0f);
        vatMaterial.SetFloat("_Smoothness", 0.5f);

        AssetDatabase.CreateAsset(vatMaterial, savePath + vatMaterial.name + ".asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // 7. Cleanup
        DestroyImmediate(tempGO); // Destroy the temporary GameObject
        AssetDatabase.DeleteAsset(savePath + "TEMP_AnimatorController.controller"); // Delete the temporary controller
        DestroyImmediate(bakedMeshInstance); // Destroy the temporary mesh used for baking

        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Bake Complete", $"Vertex Animation Texture for '{assetName}' baked successfully!", "OK");
    }

    private void OnDestroy()
    {
        // Optional: Provide an option to create a GameObject after baking
        if (bakedMesh != null)
        {
            if (EditorUtility.DisplayDialog("Create GameObject", "Do you want to create a GameObject with the baked VAT mesh and material?", "Yes", "No"))
            {
                GameObject vatGO = new GameObject(bakedMesh.name);
                MeshFilter meshFilter = vatGO.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = bakedMesh;
                MeshRenderer meshRenderer = vatGO.AddComponent<MeshRenderer>();

                // Find the material created during baking
                string assetName = sourceSkinnedMeshRenderer.name + "_" + animationClip.name;
                Material vatMaterial = AssetDatabase.LoadAssetAtPath<Material>(savePath + assetName + "_VAT_Material.asset");
                if (vatMaterial != null)
                {
                    meshRenderer.sharedMaterial = vatMaterial;
                }
                else
                {
                    Debug.LogWarning("Could not find the baked VAT material. Assign it manually.");
                }
                Selection.activeGameObject = vatGO;
            }
        }
    }
}
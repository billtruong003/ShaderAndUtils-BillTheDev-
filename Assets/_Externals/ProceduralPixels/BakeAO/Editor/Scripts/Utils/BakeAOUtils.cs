/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Component = UnityEngine.Component;

namespace ProceduralPixels.BakeAO.Editor
{
    internal static class BakeAOUtils
    {
        public static void RefreshAllBakeAOComponents()
        {
#if UNITY_2023_1_OR_NEWER
            var bakeAOs = UnityEngine.Object.FindObjectsByType<GenericBakeAO>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            var bakeAOs = UnityEngine.Object.FindObjectsOfType<GenericBakeAO>();
#endif
            for (int i = 0; i < bakeAOs.Length; i++)
                bakeAOs[i].Refresh();
        }

        public static bool IsBuiltInAsset(UnityEngine.Object obj)
        {
            return AssetDatabase.GetAssetPath(obj).Contains("unity_builtin_extra");
        }

        public static Mesh FirstOrDefaultMesh(UnityEngine.Object obj)
        {
            if (obj is Mesh mesh)
                return mesh;

            if (obj is Component component)
                return FirstOrDefaultMesh(component);

            if (obj is GameObject go)
                return FirstOrDefaultMesh(go);

            return null;
        }

        public static string FirstOrDefaultMeshGUID(UnityEngine.Object obj)
        {
            if (obj == null)
                return "";

            Mesh mesh = FirstOrDefaultMesh(obj);
            if (mesh == null)
                return "";

            string guid;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out guid, out long _))
                return guid;

            return "";
        }

        public static bool TryGetMesh(UnityEngine.Object obj, out Mesh outMesh)
        {
            outMesh = FirstOrDefaultMesh(obj);
            return outMesh != null;
        }

        public static bool TryGetMaterial(UnityEngine.Object obj, out Material outMaterial)
        {
            outMaterial = FirstOrDefaultMaterial(obj);
            return outMaterial != null;
        }

        public static bool TryGetMaterials(UnityEngine.Object obj, out Material[] outMaterials)
        {
            outMaterials = FirstOrDefaultMaterials(obj);
            return outMaterials != null;
        }

        public static Material FirstOrDefaultMaterial(UnityEngine.Object obj)
        {
            if (obj is GameObject go)
                return FirstOrDefaultMaterial(go);
            else if (obj is Component component)
                return FirstOrDefaultMaterial(component.gameObject);

            return null;
        }

        public static Material[] FirstOrDefaultMaterials(UnityEngine.Object obj)
        {
            if (obj is GameObject go)
                return FirstOrDefaultMaterials(go);
            else if (obj is Component component)
                return FirstOrDefaultMaterials(component.gameObject);

            return null;
        }

        public static Material FirstOrDefaultMaterial(GameObject go)
        {
            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                return meshRenderer.sharedMaterial;

            var skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
                return skinnedMeshRenderer.sharedMaterial;

            return null;
        }

        public static Material[] FirstOrDefaultMaterials(GameObject go)
        {
            var meshRenderer = go.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                return meshRenderer.sharedMaterials.ToArray();

            var skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
                return skinnedMeshRenderer.sharedMaterials.ToArray();

            return null;
        }

        public static Mesh FirstOrDefaultMesh(Component component)
        {
            return FirstOrDefaultMesh(component.gameObject);
        }

        public static Mesh FirstOrDefaultMesh(GameObject go)
        {
            var meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter != null)
                return meshFilter.sharedMesh;

            var skinnedMeshRenderer = go.GetComponent<SkinnedMeshRenderer>();
            if (skinnedMeshRenderer != null)
                return skinnedMeshRenderer.sharedMesh;

            return null;
        }

        public static LODGroup FirstOrDefaultLODGroup(UnityEngine.Object obj)
        {
            GameObject go = null;
            if (obj is Component component)
                go = component.gameObject;
            if (obj is GameObject gameObject)
                go = gameObject;

            if (go == null)
                return null;

            LODGroup lodGroup = null;
            lodGroup = go.GetComponent<LODGroup>();

            if (lodGroup != null)
                return lodGroup;

            lodGroup = go.GetComponentInChildren<LODGroup>();

            if (lodGroup != null)
                return lodGroup;

            lodGroup = go.GetComponentInParent<LODGroup>();

            return lodGroup;
        }

        public static bool DoesHaveUVSet(this Mesh mesh, UVChannel uv)
        {
            var attributeDescriptors = mesh.GetVertexAttributes();

            VertexAttribute searchAttribute = (VertexAttribute)(-1);
            switch (uv)
            {
                case UVChannel.UV0:
                    searchAttribute = VertexAttribute.TexCoord0;
                    break;
                case UVChannel.UV1:
                    searchAttribute = VertexAttribute.TexCoord1;
                    break;
                case UVChannel.UV2:
                    searchAttribute = VertexAttribute.TexCoord2;
                    break;
                case UVChannel.UV3:
                    searchAttribute = VertexAttribute.TexCoord3;
                    break;
            }

            return attributeDescriptors.Any(a => a.attribute == searchAttribute);
        }

        public static Texture2D SaveOcclusionTexture(RenderTexture renderedTexture, string textureAssetPath, string userData)
        {
            var textureSize = renderedTexture.width;  
            var aoTexture = new Texture2D(textureSize, textureSize, TextureFormat.RGBAFloat, false, true);

            var activeTexture = RenderTexture.active;  

            RenderTexture.active = renderedTexture;
            aoTexture.ReadPixels(new Rect(0, 0, renderedTexture.width, renderedTexture.height), 0, 0, false);
            aoTexture.Apply();

            RenderTexture.active = activeTexture;

            byte[] pngBytes = aoTexture.EncodeToPNG();
            File.WriteAllBytes(textureAssetPath, pngBytes);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
             
            TextureImporter aoTextureImporter = AssetImporter.GetAtPath(textureAssetPath) as TextureImporter;

            if (aoTextureImporter == null) // After Unity 2021 it seems like AssetDatabase can't be force refreshed on beforeAssemblyReload event, this is why we need to configure the importer after assembly reload.
            {
                BakeAOTemporaryData.AddPendingTextureConfiguration(new BakeAOTemporaryData.ConfigureTextureRecord(
                    textureAssetPath,
                    "BakeAO",
                    TextureImporterCompression.CompressedHQ,
                    false,
                    userData
                ));

                return null;
            }

            // Add BakeAO label to the asset
            var labels = AssetDatabase.GetLabels(AssetDatabase.GUIDFromAssetPath(textureAssetPath)).ToList();
            var textureAsset = AssetDatabase.LoadAssetAtPath(textureAssetPath, typeof(Texture2D));
            if (!labels.Contains("BakeAO"))
            {
                labels.Add("BakeAO");
                AssetDatabase.SetLabels(textureAsset, labels.ToArray());
            }

            aoTextureImporter.textureCompression = TextureImporterCompression.CompressedHQ;
            aoTextureImporter.sRGBTexture = false;
            aoTextureImporter.userData = userData; 
            aoTextureImporter.SaveAndReimport();
            AOTextureSearch.MarkDatabaseDirty();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(textureAssetPath);
        }

        public struct PathResolver
        {
            public const string GameObjectName = "<GameObjectName>";
            public const string AssetName = "<AssetName>";
            public const string MeshName = "<MeshName>";
            public const string MeshFolder = "<MeshFolder>";
            public static string[] AllParameters = new string[] { GameObjectName, AssetName, MeshName, MeshFolder};

            public string gameObjectName;
            public string assetObjectName;
            public string meshName;
            public string meshFolderPath;
            public string fallbackMeshFolderPath;
            public char invalidCharReplacement;

            public string ReplaceInvalidFileNameChars(string name)
            {
                var invalidChars = new string(Path.GetInvalidFileNameChars());
                return Regex.Replace(name, $"[{Regex.Escape(invalidChars)}]", "_");
            }

            private void FixInvalidNames()
            {
                var invalidChars = new string(Path.GetInvalidFileNameChars());
                gameObjectName = Regex.Replace(gameObjectName, $"[{Regex.Escape(invalidChars)}]", "_");
                assetObjectName = Regex.Replace(assetObjectName, $"[{Regex.Escape(invalidChars)}]", "_");
                meshName = Regex.Replace(meshName, $"[{Regex.Escape(invalidChars)}]", "_");
            }

            private void InitializeDefaults()
            {
                assetObjectName = "";
                gameObjectName = "";
                meshName = "";
                meshFolderPath = "";
                fallbackMeshFolderPath = "";
                invalidCharReplacement = '_';
            }

            public PathResolver(GameObject gameObject, Mesh mesh) : this()
            {
                InitializeDefaults();
                meshName = mesh.name;

                UnityEngine.Object assetObject;
                string assetPath;
                gameObjectName = gameObject.name;

                if (BakeAOUtils.TryFindSourceGameObjectAsset(mesh, out assetObject, out assetPath))
                {
                    assetObjectName = assetObject.name;
                    meshFolderPath = PathUtils.GetContainingFolderPath(assetPath);
                }

                FixInvalidNames();
            }

            public PathResolver(Mesh mesh) : this()
            {
                InitializeDefaults();
                meshName = mesh.name;

                UnityEngine.Object assetObject;
                string assetPath;
                if (BakeAOUtils.TryFindSourceGameObjectAsset(mesh, out assetObject, out assetPath))
                {
                    assetObjectName = assetObject.name;
                    gameObjectName = "";
                    meshFolderPath = PathUtils.GetContainingFolderPath(assetPath);
                }

                FixInvalidNames();
            }

            public PathResolver(UnityEngine.Object unityObject, Mesh mesh) : this()
            {
                InitializeDefaults();
                meshName = mesh.name;

                UnityEngine.Object assetObject;

                if (unityObject is GameObject go)
                    gameObjectName = go.name;
                else if (unityObject is Component component)
                    gameObjectName = component.gameObject.name;

                string assetPath;

                if (BakeAOUtils.TryFindSourceGameObjectAsset(mesh, out assetObject, out assetPath))
                {
                    assetObjectName = assetObject.name;
                    meshFolderPath = PathUtils.GetContainingFolderPath(assetPath);
                }

                FixInvalidNames();
            }

            public string Resolve(string template)
            {
                if (!ValidateTemplate(template, out int invalidCharPosition, out bool notInAssets))
                {
                    if (invalidCharPosition >= 0)
                        throw new ArgumentException($"Template contains invalid parameters or chacters at index {invalidCharPosition}: \'{template[invalidCharPosition]}\'");
                    else if (notInAssets)
                        throw new ArgumentException($"Path is not inside the Assets folder");
                        
                }

                template = template.Replace(GameObjectName, gameObjectName);
                template = template.Replace(AssetName, assetObjectName);
                template = template.Replace(MeshName, meshName);
                var meshFolderPath = this.meshFolderPath;
                if (string.IsNullOrEmpty(meshFolderPath))
                    meshFolderPath = this.fallbackMeshFolderPath;
                template = template.Replace(MeshFolder, meshFolderPath);

                // Replace all invalid characters
                char[] invalidPathChars = Path.GetInvalidPathChars();
                int invalidCharIndex;
                while ((invalidCharIndex = template.IndexOfAny(invalidPathChars)) != -1)
                    template = template.Replace(template[invalidCharIndex], invalidCharReplacement);

                return template;
            }

            public bool IsPathInAssets(string template)
            {
                var path = Resolve(template);
                if (path.StartsWith("Assets", StringComparison.InvariantCulture))
                    return true;
                return false;
            }

            public static bool ValidateTemplate(string template, out int invalidCharacterPosition, out bool notInAssets) // True if OK
            {
                notInAssets = false;

                if (!template.StartsWith("Assets") && !template.StartsWith("<MeshFolder>"))
                    notInAssets = true;

                var fixedTemplate = template;
                for (int i = 0; i < AllParameters.Length; i++)
                {
                    var parameter = AllParameters[i];
                    fixedTemplate = fixedTemplate.Replace(parameter, GetUnderscore(parameter.Length));
                }

                invalidCharacterPosition = fixedTemplate.IndexOfAny(Path.GetInvalidPathChars());

                return invalidCharacterPosition == -1 && !notInAssets;
            }

            public static bool ValidatePath(string path, out int invalidCharacterPosition, out bool notInAssets) //true if OK
            {
                notInAssets = !path.StartsWith("Assets");
                invalidCharacterPosition = path.IndexOfAny(Path.GetInvalidPathChars());
                return invalidCharacterPosition == -1 && !notInAssets;
            }

            private static StringBuilder sb;
            private static string GetUnderscore(int length)
            {
                if (sb == null)
                    sb = new StringBuilder();

                sb.Clear();
                for (int i = 0; i < length; i++)
                    sb.Append('_');

                return sb.ToString();
            }
        }

        public static bool IsSceneObject(UnityEngine.Object obj)
        {
            if (obj is GameObject go)
                return go.scene.IsValid();

            if (obj is Component component)
                return component.gameObject.scene.IsValid();

            return false;
        }

        public static void Bake(BakingSetup bakingSetup, BakePostprocessor postprocessor, UnityEngine.Object context)
        {
            BakingManager.instance.Enqueue(bakingSetup, postprocessor, context);
        }

        public static void BakeAndSaveTextureToAssets(BakingSetup bakingSetup, string textureAssetPath, bool overrideExistingTexture = false, UnityEngine.Object context = null)
        {
            BakingManager.instance.Enqueue(bakingSetup, SaveTextureIntoAssetPostprocessor.Create(textureAssetPath, overrideExistingTexture), context);
        }

        public static bool TryFindSourceGameObjectAsset(Mesh mesh, out UnityEngine.Object obj, out string assetPath)
        {
            assetPath = AssetDatabase.GetAssetPath(mesh);
            obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            return (assetPath != null && obj != null);
        }

        public static bool TryGetBakingSetup(Mesh mesh, UVChannel uvChannel, BakingQuality quality, out BakingSetup bakingSetup)
        {
            bakingSetup = BakingSetup.Default;

            if (mesh == null)
                return false;

            bakingSetup.quality = quality;
            bakingSetup.meshesToBake.Add(new MeshContext(mesh, uvChannel));
            bakingSetup.occluders.Add(new MeshContext(mesh));
            return true;
        }

        public static bool IsGameObjectOnLoadedScene(GameObject gameObject)
        {
            for(int i = 0; i < SceneManager.sceneCount; i++)
            {
                var loadedScene = SceneManager.GetSceneAt(i);
                if (loadedScene.Equals(gameObject.scene))
                    return true;
            }

            return false;
        }

        public static bool TryGetBakingSetup(MeshFilter meshFilter, UVChannel uvChannel, BakingQuality quality, ContextBakingSettings contextSettings, out BakingSetup bakingSetup)
        {
            bakingSetup = BakingSetup.Default;

            if (meshFilter.sharedMesh == null)
                return false;

            bakingSetup.quality = quality;
            bakingSetup.meshesToBake.Add(new MeshContext(meshFilter, uvChannel));
            bakingSetup.occluders.Add(new MeshContext(meshFilter));

            AddContextOccluders(bakingSetup, contextSettings, meshFilter.gameObject);

            return true;
        }

        public static bool TryGetBakingSetup(SkinnedMeshRenderer skinnedRenderer, UVChannel uvChannel, BakingQuality quality, ContextBakingSettings contextSettings, out BakingSetup bakingSetup)
        {
            bakingSetup = BakingSetup.Default;

            if (skinnedRenderer.sharedMesh == null)
                return false;

            bakingSetup.quality = quality;
            bakingSetup.meshesToBake.Add(new MeshContext(skinnedRenderer, uvChannel));
            bakingSetup.occluders.Add(new MeshContext(skinnedRenderer));

            bakingSetup.AddContextOccluders(contextSettings, skinnedRenderer.gameObject);

            return true;
        }

        public static Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
        {
            List<Vector3> corners = new List<Vector3>();
            corners.Add(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, -1, -1)));
            corners.Add(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, -1, 1)));
            corners.Add(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, 1, -1)));
            corners.Add(bounds.center + Vector3.Scale(bounds.extents, new Vector3(-1, 1, 1)));
            corners.Add(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, -1, -1)));
            corners.Add(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, -1, 1)));
            corners.Add(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, 1, -1)));
            corners.Add(bounds.center + Vector3.Scale(bounds.extents, new Vector3(1, 1, 1)));

            for (int i = 0; i < corners.Count; i++)
                corners[i] = matrix.MultiplyPoint(corners[i]);

            Bounds transformedBounds = new Bounds(corners[0], Vector3.zero);
            for (int i = 1; i < corners.Count; i++)
                transformedBounds.Encapsulate(corners[i]);

            return transformedBounds;
        }

        public static void AddContextOccluders(this BakingSetup bakingSetup, ContextBakingSettings contextSettings, GameObject gameObject)
        {
            List<MeshRenderer> mrOccluders = new List<MeshRenderer>();
            List<SkinnedMeshRenderer> smrOccluders = new List<SkinnedMeshRenderer>();

            Bounds bakedMeshesBounds = TransformBounds(bakingSetup.meshesToBake[0].mesh.bounds, bakingSetup.meshesToBake[0].objectToWorld);
            for (int i = 1; i < bakingSetup.meshesToBake.Count; i++)
            {
                var meshContext = bakingSetup.meshesToBake[i];
                bakedMeshesBounds.Encapsulate(TransformBounds(meshContext.mesh.bounds, meshContext.objectToWorld));
            }

            bakedMeshesBounds.extents += Vector3.one * bakingSetup.quality.MaxOccluderDistance;

            if (contextSettings.bakeInWholeSceneContext && IsGameObjectOnLoadedScene(gameObject))
            {
                // Add meshes from mesh renderers and skinned mesh renderers
#if UNITY_2023_1_OR_NEWER
                mrOccluders.AddRange(UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(r => r.enabled && !r.forceRenderingOff && BakeAOSettings.Instance.DoesObjectsInteract(gameObject.layer, r.gameObject.layer)));

                smrOccluders.AddRange(UnityEngine.Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Where(r => r.enabled && !r.forceRenderingOff && BakeAOSettings.Instance.DoesObjectsInteract(gameObject.layer, r.gameObject.layer)));
#else
                mrOccluders.AddRange(UnityEngine.Object.FindObjectsOfType<MeshRenderer>().Where(r => r.enabled && !r.forceRenderingOff && BakeAOSettings.Instance.DoesObjectsInteract(gameObject.layer, r.gameObject.layer)));
                smrOccluders.AddRange(UnityEngine.Object.FindObjectsOfType<SkinnedMeshRenderer>().Where(r => r.enabled && !r.forceRenderingOff && BakeAOSettings.Instance.DoesObjectsInteract(gameObject.layer, r.gameObject.layer)));
#endif

                // Add terrains
                var terrainMeshes = Terrain.activeTerrains
                    .Where(t => t.enabled && t.drawHeightmap)
                    .Select(t => TerrainUtility.GetTerrainMeshAtBounds(t, bakedMeshesBounds))
                    .Where(m => m != null)
                    .Select(m => new MeshContext(m, UVChannel.UV0, MeshContextUseFlags.IsNotUsedInAnyAsset | MeshContextUseFlags.ShouldApplyNormalBias));

                bakingSetup.occluders.AddRange(terrainMeshes);
            }

            mrOccluders.RemoveAll(mr => !CanBeBaked(mr) || !mr.bounds.Intersects(bakedMeshesBounds) || mr.gameObject == gameObject);
            smrOccluders.RemoveAll(smr => !CanBeBaked(smr) || !smr.bounds.Intersects(bakedMeshesBounds) || smr.gameObject == gameObject);

            bakingSetup.occluders.AddRange(mrOccluders.Select(mr => new MeshContext(mr, UVChannel.UV0, MeshContextUseFlags.ShouldApplyNormalBias)));
            bakingSetup.occluders.AddRange(smrOccluders.Select(smr => new MeshContext(smr, UVChannel.UV0, MeshContextUseFlags.ShouldApplyNormalBias)));
        }

        public static float GetUVToWSRatio(MeshContext meshContext)
        {
            if (!meshContext.mesh.DoesHaveUVSet(meshContext.uv))
                return 0.0f;

            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<int> triangles = new List<int>();

            meshContext.mesh.GetVertices(vertices);
            meshContext.mesh.GetTriangles(triangles, 0, true);
            meshContext.mesh.GetUVs((int)meshContext.uv, uvs);

            double ratioSum = 0.0f;
            int iterationsCount = 0;

            for (int i = 0; i < triangles.Count - 3; i++)
            {
                int startIndex = triangles[i];
                int endIndex = triangles[i + 1];
                Vector3 positionOSStart = vertices[startIndex];
                Vector3 positionOSEnd = vertices[endIndex];
                Vector3 positionWSStart = meshContext.objectToWorld.MultiplyPoint(positionOSStart);
                Vector3 positionWSEnd = meshContext.objectToWorld.MultiplyPoint(positionOSEnd);
                Vector2 uvStart = uvs[startIndex];
                Vector2 uvEnd = uvs[endIndex];

                float ratio = (uvStart - uvEnd).magnitude / Mathf.Max((positionWSStart - positionWSEnd).magnitude, 0.000001f);
                if (ratio < 100000.0f)
                {
                    ratioSum += ratio;
                    iterationsCount++;
                }
            }

            return (float)(ratioSum / Mathf.Max(iterationsCount, 1));
        }

        public static bool CanBeBaked(MeshRenderer meshRenderer)
        {
            if (meshRenderer == null)
                return false;

            var meshFilter = meshRenderer.GetComponent<MeshFilter>();
            if (meshFilter == null)
                return false;

            if (meshFilter.sharedMesh == null)
                return false;

            return true;
        }

        public static bool CanBeBaked(SkinnedMeshRenderer skinnedMeshRenderer)
        {
            if (skinnedMeshRenderer == null)
                return false;

            if (skinnedMeshRenderer.sharedMesh == null)
                return false;

            return true;
        }

        public static void PropertyFieldWithButton(Rect controlRect, SerializedProperty property, string buttonName, float buttonWidth, Action onButton)
        {
            HorizontalRectLayout layout = new HorizontalRectLayout(controlRect);
            Rect buttonRect = layout.GetFromRight(buttonWidth);
            layout.GetFromRight(4);
            Rect propertyRect = layout.GetReminder();

            EditorGUI.PropertyField(propertyRect, property, true);
            if (GUI.Button(buttonRect, buttonName))
            {
                onButton?.Invoke();
            }
        }

        public static void FoldoutWithButton(Rect controlRect, ref bool foldoutState, string foldoutLabel, string buttonName, float buttonWidth, Action onButton)
        {
            HorizontalRectLayout layout = new HorizontalRectLayout(controlRect);
            Rect buttonRect = layout.GetFromRight(buttonWidth);
            layout.GetFromRight(4);
            Rect propertyRect = layout.GetReminder();

            if (GUI.Button(buttonRect, buttonName))
                onButton?.Invoke();

            foldoutState = EditorGUI.Foldout(propertyRect, foldoutState, foldoutLabel);
        }

        public static Mesh CloneMesh(Mesh source)
        {
            Mesh mesh = new Mesh();

            // Using unity combine mesh to combine a single mesh into another one, making a full copy.
            CombineInstance[] instancesToCombine = new CombineInstance[source.subMeshCount];
            for (int i = 0; i < source.subMeshCount; i++)
            {
                instancesToCombine[i] = new CombineInstance()
                {
                    mesh = source,
                    subMeshIndex = i,
                    lightmapScaleOffset = new Vector4(1, 1, 0, 0),
                    realtimeLightmapScaleOffset = new Vector4(1, 1, 0, 0),
                    transform = Matrix4x4.identity
                };
            }
            mesh.CombineMeshes(instancesToCombine, false, false, false);

            mesh.name = source.name;
            return mesh;
        }

        public struct HorizontalRectLayout
        {
            public Rect controlRect;

            public HorizontalRectLayout(Rect controlRect)
            {
                this.controlRect = controlRect;
            }

            public Rect GetFromLeft(float width)
            {
                var newRect = new Rect(controlRect.x, controlRect.y, width, controlRect.height);
                controlRect = new Rect(controlRect.x + width, controlRect.y, controlRect.width - width, controlRect.height);
                return newRect;
            }

            public Rect GetFromRight(float width)
            {
                var newRect = new Rect(controlRect.x + controlRect.width - width, controlRect.y, width, controlRect.height);
                controlRect = new Rect(controlRect.x, controlRect.y, controlRect.width - width, controlRect.height);
                return newRect;
            }

            public void AddPadding(float width)
            {
                GetFromLeft(width);
                GetFromRight(width);
            }

            public Rect GetReminder()
            {
                Rect rect = controlRect;
                controlRect.width = 0;
                return rect;
            }
        }

        public static bool TestPlanesAABB(Plane[] planes, Bounds bounds)
        {
            for (int i = 0; i < planes.Length; i++)
            {
                Plane plane = planes[i];
                Vector3 normal_sign = FastSignVec3(plane.normal);
                Vector3 test_point = (bounds.center) + Vector3.Scale(bounds.extents, normal_sign);

                float dot = Vector3.Dot(test_point, plane.normal);
                if (dot + plane.distance < 0)
                    return false;
            }

            return true;

            Vector3 FastSignVec3(Vector3 a)
            {
                return new Vector3(a.x < 0 ? -1 : 1, a.y < 0 ? -1 : 1, a.z < 0 ? -1 : 1);
            }
        }

        public struct FastBoundsTransform
        {
            Vector3[] corners;

            public static FastBoundsTransform Create()
            {
                return new FastBoundsTransform()
                {
                    corners = new Vector3[]
                    {
                        new Vector3(-1, -1, -1),
                        new Vector3(-1, -1, 1),
                        new Vector3(-1, 1, -1),
                        new Vector3(-1, 1, 1),
                        new Vector3(1, -1, -1),
                        new Vector3(1, -1, 1),
                        new Vector3(1, 1, -1),
                        new Vector3(1, 1, 1)
                    }
                };
            }

            public Bounds TransformBounds(Bounds bounds, Matrix4x4 matrix)
            {
                Vector3 minValue = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Vector3 maxValue = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                for (int i = 0; i < corners.Length; i++)
                {
                    Vector3 transfomedCorner = matrix.MultiplyPoint(bounds.center + Vector3.Scale(bounds.extents, corners[i]));
                    minValue = MinVec3(minValue, transfomedCorner);
                    maxValue = MaxVec3(maxValue, transfomedCorner);
                }

                return new Bounds(minValue * 0.5f + maxValue * 0.5f, maxValue - minValue);

                Vector3 MinVec3(Vector3 a, Vector3 b)
                {
                    return new Vector3(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Min(a.z, b.z));
                }

                Vector3 MaxVec3(Vector3 a, Vector3 b)
                {
                    return new Vector3(Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y), Mathf.Max(a.z, b.z));
                }
            }
        }
    }
}
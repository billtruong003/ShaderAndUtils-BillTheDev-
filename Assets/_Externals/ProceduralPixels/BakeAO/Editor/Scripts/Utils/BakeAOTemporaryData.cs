/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    // From Unity 2021, synchronous asset importing does not create Asset Importers when import was forced after beforeAssemblyReload event.
    // So when Bake AO serializes the textures on this event, it can't configure the asset importers. In this case there is a need of
    // serializing all needed data into some ScriptableObject saving there all the information needed to configure Texture Importer
    // after assembly is reloaded. So if this scriptable object exist, Baking Manager should try to configure importers
    [CreateInFolderWith(typeof(BakeAOResources))]
    internal class BakeAOTemporaryData : SingletonScriptableObject<BakeAOTemporaryData>
    {
        public List<ConfigureTextureRecord> pendingConfigurations = new();
        public List<Mesh> temporaryMeshes = new();

        public static void AddPendingTextureConfiguration(ConfigureTextureRecord configurationRecord)
        {
            if (Instance == null)
                CreateInstance();

            Instance.pendingConfigurations.Add(configurationRecord);
            EditorUtility.SetDirty(Instance);
        }

        public static void SerializeAndAddTemporaryMesh(Mesh mesh)
        {
            if (Instance == null)
                CreateInstance();

            if (!Instance.temporaryMeshes.Contains(mesh))
            {
                Instance.temporaryMeshes.Add(mesh);
            }
            AssetDatabase.AddObjectToAsset(mesh, Instance);
            AssetDatabase.Refresh();
        }

        public bool ShouldDestroy()
        {
            return pendingConfigurations.Count == 0 && temporaryMeshes.Count == 0;
        }

        public static bool TryDestroy()
        {
            if (Instance.ShouldDestroy())
            {
                AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(Instance));
                DestroyImmediate(Instance);
                AssetDatabase.Refresh();
                return true;
            }

            return false;
        }

        public static void TryClear(IEnumerable<Mesh> neededMeshes)
        {
            if (Instance == null)
                return;

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            for (int i = Instance.pendingConfigurations.Count - 1; i >= 0; i--)
            {
                ConfigureTextureRecord configuration = Instance.pendingConfigurations[i];
                bool finalized = configuration.TryFinalize();
                if (finalized)
                    Instance.pendingConfigurations.RemoveAt(i);
            }

            Instance.temporaryMeshes = Instance.temporaryMeshes.Intersect(neededMeshes).ToList();

            if (Instance.pendingConfigurations.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("BakeAO: Not able to configure texture some of the textures! BakeAO will not be able to recognise those textures as AO for baked models.");
                foreach (var config in Instance.pendingConfigurations)
                    sb.AppendLine($"Path: {config.textureAssetPath}");
                Debug.LogError(sb);
                Instance.pendingConfigurations.Clear();
            }

            TryDestroy();
        }

        public static Mesh[] GetAllTemporaryMeshes()
        {
            if (Instance == null)
                return null;

            Instance.temporaryMeshes.RemoveAll(m => m == null);
            return Instance.temporaryMeshes.ToArray();
        }

        public static void RemoveTemporaryMesh(Mesh mesh)
        {
            if (Instance == null)
                return;

            Instance.temporaryMeshes.Remove(mesh);
            AssetDatabase.RemoveObjectFromAsset(mesh);
        }

        [System.Serializable]
        internal class ConfigureTextureRecord
        {
            // Texture to configure
            public string textureAssetPath;
            public string labelToAdd;
            public TextureImporterCompression compression;
            public bool isSRGB;
            public string userData;

            public ConfigureTextureRecord(string textureAssetPath, string labelToAdd, TextureImporterCompression comression, bool isSRGB, string userData)
            {
                this.textureAssetPath = textureAssetPath;
                this.labelToAdd = labelToAdd;
                this.compression = comression;
                this.isSRGB = isSRGB;
                this.userData = userData;
            }

            public bool TryFinalize()
            {
                // If asset path is invalid, just skip
                if (string.IsNullOrWhiteSpace(textureAssetPath))
                    return true;

                // If asset file does not exist, skip configuration
                if (!File.Exists(textureAssetPath))
                    return true;

                TextureImporter textureImporter = AssetImporter.GetAtPath(textureAssetPath) as TextureImporter;

                // If texture importer does not exist, it means that unity did not fully import the asset yet. We postpone the configuration then
                if (textureImporter == null)
                    return false;

                // Add BakeAO label to the asset
                var labels = AssetDatabase.GetLabels(AssetDatabase.GUIDFromAssetPath(textureAssetPath)).ToList();
                var textureAsset = AssetDatabase.LoadAssetAtPath(textureAssetPath, typeof(Texture2D));
                if (!labels.Contains("BakeAO"))
                {
                    labels.Add("BakeAO");
                    AssetDatabase.SetLabels(textureAsset, labels.ToArray());
                }

                // configure importer
                textureImporter.textureCompression = compression;
                textureImporter.sRGBTexture = isSRGB;
                textureImporter.userData = userData;
                textureImporter.SaveAndReimport();

                AOTextureSearch.MarkDatabaseDirty();

                return true;
            }
        }

    }
}
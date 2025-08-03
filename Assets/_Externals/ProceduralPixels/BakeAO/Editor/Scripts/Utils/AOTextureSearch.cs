/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static ProceduralPixels.BakeAO.Editor.AOTextureSearch;

namespace ProceduralPixels.BakeAO.Editor
{
    internal class AOTextureSearchInitializer : IDisposable
    {
        Queue<string> assetGUIDsToProcess = null;
        List<AOTextureImporter> initializedImporters = null;

        private int progressID = -1;
        public bool Finished { get; private set; } = false;
        public float Progress { get; private set; } = 0.0f;
        public Action OnFinish = null;
        private int startCount = 0;

        public AOTextureSearchInitializer(Action onFinish)
        {
            OnFinish = onFinish;
        }

        private float MaxTimePerFrame => 0.016f;

        public void Dispose()
        {
            if (!Finished)
            {
                UnityEditor.EditorApplication.update -= OnEditorUpdate;
                AssemblyReloadEvents.beforeAssemblyReload -= AssemblyReloadEvents_beforeAssemblyReload;
                UnityEditor.Progress.Finish(progressID);
            }
        }

        public void Start()
        {
            Finished = false;
            progressID = UnityEditor.Progress.Start("Initializing Bake AO", null, UnityEditor.Progress.Options.Managed);
            UnityEditor.Progress.Report(progressID, 0.0f, "Initializing Bake AO database");

            UnityEngine.Debug.Assert(assetGUIDsToProcess == null);
            assetGUIDsToProcess = new Queue<string>();
            initializedImporters = new List<AOTextureImporter>();

            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D l:BakeAO"))
                assetGUIDsToProcess.Enqueue(guid);

            startCount = assetGUIDsToProcess.Count;

            UnityEditor.EditorApplication.update += OnEditorUpdate;
            AssemblyReloadEvents.beforeAssemblyReload += AssemblyReloadEvents_beforeAssemblyReload;
        }

        private void AssemblyReloadEvents_beforeAssemblyReload()
        {
            Dispose();
        }

        private void OnEditorUpdate()
        {  
            if (Finished)
                return;

            Stopwatch stopwatch = Stopwatch.StartNew();
            while (assetGUIDsToProcess.Count > 0)
            {
                var guid = assetGUIDsToProcess.Dequeue();

                try
                {
                    var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    var assetImporter = AssetImporter.GetAtPath(assetPath);
                    if (assetImporter is TextureImporter textureImporter)
                    {
                        if (!string.IsNullOrWhiteSpace(textureImporter.userData))
                        {
                            var aoTextureImporter = new AOTextureImporter(textureImporter);
                            initializedImporters.Add(aoTextureImporter);
                        }
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"Invalid AO texture with GUID {guid}, exception:\n{e.Message}\n{e.StackTrace}");
                }

                if (stopwatch.Elapsed.TotalSeconds > MaxTimePerFrame)
                    break;
            }

            Progress = 1.0f - ((float)assetGUIDsToProcess.Count / (float)startCount);  
            UnityEditor.Progress.Report(progressID, Progress);

            stopwatch.Stop();

            if (assetGUIDsToProcess.Count == 0)
            {
                UnityEditor.Progress.Finish(progressID);
                Finished = true;
                OnFinish?.Invoke();
                UnityEditor.EditorApplication.update -= OnEditorUpdate;
            }
        }
    }

    internal class AOTextureSearch : ScriptableSingleton<AOTextureSearch>
    {
        internal event Action OnInitializationFinished = null;
        internal float? InitializationProgress => Initializer?.Progress;
        private AOTextureSearchInitializer Initializer { get; set; } = default;

        [SerializeField] private bool initialized = false;
        [SerializeField] private AOTextureImporter[] allAOTextureImporters = new AOTextureImporter[0];
        [SerializeField] private bool allAOTextureImporters_isDirty = true;

        public bool IsInitialized => initialized;

        public void WaitForInitialized()
        {
            if (!initialized && Initializer == null)
            {
                Initializer = new AOTextureSearchInitializer(() =>
                {
                    initialized = true; 
                    OnInitializationFinished?.Invoke();
                });
                Initializer.Start();
            }
        }

        internal void TryFindAOTextures(Mesh[] meshes, out TextureSearchResult[] results)
        {
            var importers = GetAOTextureImporters();

            results = new TextureSearchResult[meshes.Length];
            for (int i = 0; i < meshes.Length; i++)
            {
                var mesh = meshes[i];
                if (mesh == null)
                    continue;

                if (TryFindAOTexture(importers, mesh, out Texture2D aoTexture, out SerializableBakingSetup bakingSetup))
                    results[i] = new TextureSearchResult(aoTexture, bakingSetup);
            }
        }

        internal AOTextureImporter[] GetAOTextureImporters()
        {
            if (allAOTextureImporters_isDirty)
            {
                allAOTextureImporters = AssetDatabase.FindAssets("t:Texture2D l:BakeAO")
                    .Select(guid => AssetDatabase.GUIDToAssetPath(guid))
                    .Select(assetPath => AssetImporter.GetAtPath(assetPath))
                    .Where(assetImporter => assetImporter is TextureImporter && !string.IsNullOrWhiteSpace(assetImporter.userData))
                    .Select(assetImporter => new AOTextureImporter((TextureImporter)assetImporter))
                    .ToArray();
                 
                allAOTextureImporters_isDirty = false; 
            }

            allAOTextureImporters = allAOTextureImporters.Where(i => i.importer != null).ToArray();
            return allAOTextureImporters;
        }

        public bool TryGetBakingSetupFromTexture(Texture2D aoTexture, ref GenericBakingSetup genericBakingSetup)
        {
            if (!AssetDatabase.IsMainAsset(aoTexture))
                return false;

            var aoTextureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(aoTexture)) as TextureImporter;

            try
            {
                var bakingSetup = SerializableBakingSetup.FromJson(aoTextureImporter.userData).GetBakingSetup();
                genericBakingSetup = GenericBakingSetup.Default;
                genericBakingSetup.uvChannel = bakingSetup.meshesToBake[0].uv;
                genericBakingSetup.quality = bakingSetup.quality;
                genericBakingSetup.contextBakingSettings = new ContextBakingSettings(bakingSetup.Occluders.Count > 1);
            }
            catch
            {
                return false;
            }

            return true;
        }

        internal static void MarkDatabaseDirty()
        {
            instance.allAOTextureImporters_isDirty = true;
        }
         
        internal bool TryFindAOTexture(Mesh mesh, out Texture2D aoTexture, out SerializableBakingSetup bakingSetup)
        {
            aoTexture = null;
            bakingSetup = null;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string meshGUID, out long meshLocalFileID))
            {
                var allTextureImportersWithCustomData = GetAOTextureImporters();

                foreach (var textureImporter in allTextureImportersWithCustomData)
                {
                    SerializableBakingSetup serializableBakingSetup = textureImporter.bakingSetup;
                    if (serializableBakingSetup.meshesToBake.Any(context => context.meshFileID == meshLocalFileID && meshGUID.Equals(context.meshGUID)))
                    {
                        aoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureImporter.importer.assetPath);
                        bakingSetup = serializableBakingSetup;
                        return true;
                    }
                }
            }
            
            return false;
        }

        [System.Serializable]
        internal class AOTextureImporter
        {
            public TextureImporter importer;
            public SerializableBakingSetup bakingSetup;
            public string guid;
            public long fileID;

            public AOTextureImporter(TextureImporter textureImporter)
            {
                this.importer = textureImporter;
                if (!string.IsNullOrWhiteSpace(textureImporter.userData))
                    bakingSetup = SerializableBakingSetup.FromJson(textureImporter.userData);
            }
        }

        internal bool TryFindAOTexture(AOTextureImporter[] importers, Mesh mesh, out Texture2D aoTexture, out SerializableBakingSetup bakingSetup)
        {
            aoTexture = null;
            bakingSetup = null;
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string meshGUID, out long meshLocalFileID))
            {
                for (int i = 0; i < importers.Length; i++)
                {
                    AOTextureImporter textureImporter = importers[i];
                    if (textureImporter.bakingSetup.meshesToBake.Any(context => context.meshFileID == meshLocalFileID && meshGUID.Equals(context.meshGUID)))
                    {
                        aoTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(textureImporter.importer.assetPath);
                        bakingSetup = textureImporter.bakingSetup;
                        return true;
                    }
                }
            }

            return false;
        }

        internal class TextureSearchResult
        {
            public Texture2D aoTexture;
            public SerializableBakingSetup bakingSetup;

            public TextureSearchResult(Texture2D aoTexture, SerializableBakingSetup bakingSetup)
            {
                this.aoTexture = aoTexture;
                this.bakingSetup = bakingSetup; 
            }
        }

        internal bool TryFindAllAOTextures(Mesh mesh, List<TextureSearchResult> result)
        {
            result.Clear();
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out string meshGUID, out long meshLocalFileID))
            {
                var allTextureImportersWithCustomData = instance.GetAOTextureImporters();

                for (int i = 0; i < allTextureImportersWithCustomData.Length; i++)
                {
                    AOTextureImporter textureImporter = allTextureImportersWithCustomData[i];
                    if (textureImporter.importer == null)
                        continue;

                    SerializableBakingSetup serializableBakingSetup = textureImporter.bakingSetup;
                    if (serializableBakingSetup.meshesToBake.Any(context => context.meshFileID == meshLocalFileID && meshGUID.Equals(context.meshGUID)))
                        result.Add(new TextureSearchResult(AssetDatabase.LoadAssetAtPath<Texture2D>(textureImporter.importer.assetPath), serializableBakingSetup));
                }
            }

            return result.Count > 0;
        }
    }
}
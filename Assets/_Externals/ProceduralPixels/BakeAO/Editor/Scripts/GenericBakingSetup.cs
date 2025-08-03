/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System.Linq;
using UnityEngine;
using static ProceduralPixels.BakeAO.Editor.BakeAOUtils;

namespace ProceduralPixels.BakeAO.Editor
{
    [CreateAssetMenu(menuName = "Procedural Pixels/Bake AO/Generic Baking Setup", fileName = "Bake AO Setup")]
    internal class GenericBakingSetup : ScriptableObject
    {
        [Tooltip("Source UV channel for baked texture.")]
        public UVChannel uvChannel;

        [Tooltip("Baking quality settings. Defines quality and resolution of the baked texture.")]
        public BakingQuality quality;

        [Tooltip("How nearby objects should affect the baking.")]
        public ContextBakingSettings contextBakingSettings;

        [Tooltip("Where to save the baked texture. Allows to input parameters like <MeshFolder>, <MeshName>, <GameObjectName> and <AssetName>")]
        public string filePath = "<MeshFolder>/AOTextures/<MeshName>_AO.png";

        [Tooltip("If file path uses <MeshFolder> parameter, and mesh is not saved in the project folder (is built-in or from packages) this folder will be used instead")]
        public string meshFolderFallback = "Assets/BakeAO/Textures"; 
         
        public static GenericBakingSetup Default
        {
            get
            {
                var setup = ScriptableObject.CreateInstance<GenericBakingSetup>();

                setup.uvChannel = UVChannel.UV0;
                setup.quality = BakingQuality.Default;
                setup.contextBakingSettings = new ContextBakingSettings();
                setup.filePath = "<MeshFolder>/AOTextures/<MeshName>_AO.png";
                setup.meshFolderFallback = "Assets/BakeAO/Textures";

                return setup; 
            }
        } 

        public static GenericBakingSetup DefaultFromPresets
        {
            get
            {
                var setup = Default;
                var presets = UnityEditor.Presets.Preset.GetDefaultPresetsForObject(setup);
                foreach (var preset in presets)
                    preset.ApplyTo(setup);

                return setup;
            }
        }

        public void SetData(BakingSetup bakingSetup)
        {
            bakingSetup = bakingSetup.Copy();
            if (bakingSetup.meshesToBake != null)
                if (bakingSetup.meshesToBake.Count > 0)
                    uvChannel = bakingSetup.meshesToBake[0].uv;

            quality = bakingSetup.quality;
        }

        public void SetData(GenericBakingSetup other)
        {
            this.contextBakingSettings = other.contextBakingSettings;
            this.filePath = other.filePath;
            this.meshFolderFallback = other.meshFolderFallback;
            this.quality = other.quality;
            this.uvChannel = other.uvChannel;
        }

        public GenericBakingSetup Clone()
        {
            var setup = CreateInstance<GenericBakingSetup>();
            setup.hideFlags = HideFlags.DontSave;

            setup.uvChannel = uvChannel;
            setup.quality = quality;
            setup.contextBakingSettings = contextBakingSettings;
            setup.filePath = filePath;
            setup.meshFolderFallback = meshFolderFallback;

            return setup;
        }

        public bool TryGetBakingSetup(UnityEngine.Object unityObject, out BakingSetup bakingSetup)
        {
            bakingSetup = null;
            bool isSuccess = false;

            if (unityObject is Mesh mesh)
                isSuccess = BakeAOUtils.TryGetBakingSetup(mesh, uvChannel, quality, out bakingSetup);
            else if (unityObject is MeshFilter meshFilter)
                isSuccess = BakeAOUtils.TryGetBakingSetup(meshFilter, uvChannel, quality, contextBakingSettings, out bakingSetup);
            else if (unityObject is SkinnedMeshRenderer skinnedMeshRenderer)
                isSuccess = BakeAOUtils.TryGetBakingSetup(skinnedMeshRenderer, uvChannel, quality, contextBakingSettings, out bakingSetup);

            if (!isSuccess)
                return false;

            LODGroup lodGroup = BakeAOUtils.FirstOrDefaultLODGroup(unityObject);

            if (lodGroup == null)
                return true;

            Mesh meshToBake = bakingSetup.meshesToBake[0].mesh;
            LOD[] lods = lodGroup.GetLODs();
            bool containsMeshToBake = lods.Any(lod => lod.renderers.Any(r => BakeAOUtils.FirstOrDefaultMesh(r) == meshToBake));
            if (containsMeshToBake)
            {
                var allLodMeshes = lods.SelectMany(lod => lod.renderers.Select(r => BakeAOUtils.FirstOrDefaultMesh(r))).Where(m => m != null && m != meshToBake);
                bakingSetup.occluders.RemoveAll(m => allLodMeshes.Contains(m.mesh));
            }

            return true;
        }

        public bool ArePathsValid()
        {
            return PathResolver.ValidateTemplate(filePath, out var _, out bool _) && PathResolver.ValidatePath(meshFolderFallback, out var _, out bool _);
        }

        public void Validate()
        {
            quality.Validate();
        }

        private void OnValidate()
        {
            Validate();
        }
    }
}
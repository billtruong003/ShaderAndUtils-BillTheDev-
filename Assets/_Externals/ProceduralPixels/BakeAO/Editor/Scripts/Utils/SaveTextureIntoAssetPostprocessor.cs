/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using UnityEditor;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    [System.Serializable]
    internal class SaveTextureIntoAssetPostprocessor : BakePostprocessor
    {
        [SerializeField] private string textureAssetPath;
        [SerializeField] private bool overrideExistingTexture;

        private SaveTextureIntoAssetPostprocessor() { }

        public static SaveTextureIntoAssetPostprocessor Create(string textureAssetPath, bool overrideExistingTexture = false)
        {
            SaveTextureIntoAssetPostprocessor postprocessor = CreateInstance<SaveTextureIntoAssetPostprocessor>();
            postprocessor.hideFlags = HideFlags.DontSave;
            postprocessor.textureAssetPath = textureAssetPath;
            postprocessor.overrideExistingTexture = overrideExistingTexture;
            return postprocessor;
        }

        public override void AfterBake(BakingSetup bakingSetup, RenderTexture renderedTexture)
        {
            string bakingSetupJson = new SerializableBakingSetup(bakingSetup).ToJson(true);
            if (!overrideExistingTexture)
                textureAssetPath = AssetDatabase.GenerateUniqueAssetPath(textureAssetPath);
            var aoTexture = BakeAOUtils.SaveOcclusionTexture(renderedTexture, textureAssetPath, bakingSetupJson);
            Result = aoTexture;
        }
    }
}
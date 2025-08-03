/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System;
using UnityEditor;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    [System.Serializable]
    internal class SaveTextureForBakeAOPostprocessor : BakePostprocessor
    {
        [SerializeField] private string textureAssetPath;
        [SerializeField] private GenericBakeAO bakeAOComponent;
        [SerializeField] private bool replaceExistingTexture;
        [NonSerialized] private Action afterBake;

        public static SaveTextureForBakeAOPostprocessor Create(string textureAssetPath, bool replaceExistingTexture, GenericBakeAO bakeAOComponent, Action afterBake)
        {
            if (string.IsNullOrWhiteSpace(textureAssetPath))
                throw new Exception("Hey! Asset path is wrong!");

            SaveTextureForBakeAOPostprocessor postprocessor = CreateInstance<SaveTextureForBakeAOPostprocessor>();
            postprocessor.hideFlags = HideFlags.DontSave;
            postprocessor.replaceExistingTexture = replaceExistingTexture;
            postprocessor.textureAssetPath = textureAssetPath;
            postprocessor.bakeAOComponent = bakeAOComponent;
            postprocessor.afterBake = afterBake; 
            return postprocessor;
        }

        private SaveTextureForBakeAOPostprocessor()
        {
        }

        public override void AfterBake(BakingSetup bakingSetup, RenderTexture renderedTexture)
        {
            if (string.IsNullOrWhiteSpace(textureAssetPath)) 
                Debug.LogError("Hey! Asset path is very wrong");

            string bakingSetupJson = new SerializableBakingSetup(bakingSetup).ToJson(true);
            if (!replaceExistingTexture)
                textureAssetPath = AssetDatabase.GenerateUniqueAssetPath(textureAssetPath);
            var aoTexture = BakeAOUtils.SaveOcclusionTexture(renderedTexture, textureAssetPath, bakingSetupJson);
            Result = aoTexture;

            if (bakeAOComponent != null)
            {
                bakeAOComponent.AmbientOcclusionTexture = aoTexture;
                bakeAOComponent.OcclusionUVSet = bakingSetup.meshesToBake[0].uv;
                bakeAOComponent.UpdateAmbientOcclusionProperties();
                afterBake?.Invoke();
            }
        }

        public override bool IsValid()
        {
            if (!base.IsValid())
                return false;

            return !string.IsNullOrWhiteSpace(textureAssetPath);
        }
    }
}
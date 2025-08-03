/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using UnityEngine;

namespace ProceduralPixels.BakeAO
{
    [ExecuteAlways]
    [AddComponentMenu("Procedural Pixels/Bake AO/Bake AO")]
    [HelpURL("https://proceduralpixels.com/BakeAO/Documentation/BakeAOComponent")]
    public sealed class BakeAO : GenericBakeAO
    {
        public const string Version = "1.0.1";

        protected override void SetupPropertyBlock(MaterialPropertyBlock propertyBlock)
        {
            propertyBlock.SetTexture(occlusionMapStandardID, ambientOcclusionTexture);
            propertyBlock.SetFloat(occlusionStrengthStandardID, occlusionStrength);
            propertyBlock.SetInt(occlusionUVSetStandardID, (int)occlusionUVSet);
            propertyBlock.SetInt(applyOcclusionToDiffuseStandardID, applyOcclusionIntoDiffuse ? 1 : 0);
        }
    }
}

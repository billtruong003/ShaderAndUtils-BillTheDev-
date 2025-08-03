/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    [System.Serializable]
    internal struct ContextBakingSettings
    {
        [Tooltip("Includes all scene objects when baking. Meshes nearby the baked object will affect the baked texture.")]
        public bool bakeInWholeSceneContext;

        public ContextBakingSettings(bool bakeInWholeSceneContext)
        {
            this.bakeInWholeSceneContext = bakeInWholeSceneContext;
        }
    }
}
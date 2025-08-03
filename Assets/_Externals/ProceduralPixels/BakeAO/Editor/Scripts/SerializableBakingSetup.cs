/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    [System.Serializable]
    internal class SerializableBakingSetup
    {
        public uint version = 1;
        public BakingQuality quality;
        public List<SerializableMeshContext> meshesToBake;
        public List<SerializableMeshContext> occluders;

        public SerializableBakingSetup(BakingSetup bakingSetup) : this(bakingSetup.quality, bakingSetup.meshesToBake, bakingSetup.occluders)
        { }

        public SerializableBakingSetup(BakingQuality quality, List<MeshContext> meshesToBake, List<MeshContext> occluders)
        {
            version = 1;
            this.quality = quality;
            this.meshesToBake = meshesToBake.Select(context => new SerializableMeshContext(context)).ToList();
            this.occluders = occluders.Select(context => new SerializableMeshContext(context)).ToList();
        }

        public BakingSetup GetBakingSetup()
        {
            BakingSetup bakingSetup = new BakingSetup();
            bakingSetup.quality = quality;
            bakingSetup.meshesToBake = meshesToBake.Select(context => context.GetMeshContext()).ToList();
            bakingSetup.occluders = occluders.Select(context => context.GetMeshContext()).ToList();

            return bakingSetup;
        }

        public string ToJson(bool prettyPrint)
        {
            return JsonUtility.ToJson(this, prettyPrint);
        }

        public static SerializableBakingSetup FromJson(string json)
        {
            return JsonUtility.FromJson<SerializableBakingSetup>(json);
        }
    }

    [System.Serializable]
    internal class SerializableBakingSetupVersion
    {
        public uint version;

        public static SerializableBakingSetupVersion FromJson(string json)
        {
            return JsonUtility.FromJson<SerializableBakingSetupVersion>(json);
        }
    }
}
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
    [Flags]
    internal enum MeshContextUseFlags
    {
        Default = 0,
        IsNotUsedInAnyAsset = 1 << 0,
        DontCombine = 1 << 1,
        IsTemporary = 1 << 2,
        ShouldApplyNormalBias = 1 << 3,
    }

    [System.Serializable]
    internal struct MeshContext
    {
        public Mesh mesh;
        public UVChannel uv;
        public Matrix4x4 objectToWorld;
        public MeshContextUseFlags useFlags;
        public float uvToWorldRatio;

        public MeshContext(Mesh mesh, UVChannel uvChannel = UVChannel.UV0, MeshContextUseFlags useFlags = MeshContextUseFlags.Default)
        {
            this.mesh = mesh;
            uv = uvChannel;
            objectToWorld = Matrix4x4.identity;
            this.useFlags = useFlags;
            this.uvToWorldRatio = 0.0f;
            this.uvToWorldRatio = BakeAOUtils.GetUVToWSRatio(this);
        }

        public MeshContext(MeshRenderer meshRenderer, UVChannel uvChannel = UVChannel.UV0, MeshContextUseFlags useFlags = MeshContextUseFlags.Default) : this(meshRenderer.GetComponent<MeshFilter>(), uvChannel, useFlags)
        {
        }

        public MeshContext(SkinnedMeshRenderer skinnedRenderer, UVChannel uvChannel = UVChannel.UV0, MeshContextUseFlags useFlags = MeshContextUseFlags.Default)
        {
            this.mesh = skinnedRenderer.sharedMesh;
            uv = uvChannel;
            objectToWorld = skinnedRenderer.transform.localToWorldMatrix;
            this.useFlags = useFlags;
            this.uvToWorldRatio = 0.0f;
            this.uvToWorldRatio = BakeAOUtils.GetUVToWSRatio(this);
        }

        public MeshContext(MeshFilter meshFilter, UVChannel uvChannel = UVChannel.UV0, MeshContextUseFlags useFlags = MeshContextUseFlags.Default)
        {
            mesh = meshFilter.sharedMesh;
            uv = uvChannel;
            objectToWorld = meshFilter.transform.localToWorldMatrix;
            this.useFlags = useFlags;
            this.uvToWorldRatio = 0.0f;
            this.uvToWorldRatio = BakeAOUtils.GetUVToWSRatio(this);
        }

        public CombineInstance[] GetCombineInstances()
        {
            CombineInstance[] instances = new CombineInstance[mesh.subMeshCount];
            for (int i = 0; i < mesh.subMeshCount; i++)
            {
                instances[i] = new CombineInstance()
                {
                    mesh = mesh,
                    transform = objectToWorld,
                    subMeshIndex = i,
                    lightmapScaleOffset = new Vector4(1, 1, 0, 0),
                    realtimeLightmapScaleOffset = new Vector4(1, 1, 0, 0)
                };
            }

            return instances;
        }
    }

}
/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using ProceduralPixels.BakeAO.Editor;
using System;
using System.Drawing;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using UnityEngine.UIElements.Experimental;

namespace ProceduralPixels.BakeAO.Editor
{
    [System.Serializable]
    internal struct BakingQuality : IProvideBakingQuality
    {
        [Tooltip("Maximum distance to the occluder")]
        [SerializeField] public float MaxOccluderDistance;

        [Tooltip("BakeAO detects more detailed areas of the mesh and will trace more rays for those areas. Increase the value if the detailed mesh areas aren't looking good. Large values will greatly increase the baking times for detailed meshes. Usually 4 samples is enough.")]
        [SerializeField] public MsaaSamples MsaaSamples;

        [Tooltip("How sensitive is the mesh detail detection.")]
        [SerializeField] public MsaaDetailDetection MsaaDetailDetection;

        [Tooltip("What is the target texture size.")]
        [SerializeField] public TextureSize TextureSize;

        [Tooltip("Defines how many rays are traced for each pixel.")]
        [SerializeField] public TracerResolution TracerTextureSize;

        [Tooltip("Field of view of the ambient occlusion")]
        [SerializeField] public float TracerFov;

        [Tooltip("Makes the pixels of the texture \"spread\" to the not baked areas of a texture")]
        [SerializeField] public int DilateIterations;

        // TODO: Denoising seems to break data where very small triangles are rendered into the texture. Denosing should be limited only to the particular UV island.
        [Tooltip("How many denoise iterations should be performed")]
        [SerializeField] public int DenoiseIterations;

        [Tooltip("What gamma correction should be applied to the image. Greater values will make the resulted texture darker.")]
        [SerializeField, Range(0.25f, 4.0f)] public float Gamma;

        public bool FixShadowArtifacts => true;

        BakingQuality IProvideBakingQuality.BakingQuality => this;

        public static readonly BakingQuality Default = new BakingQuality()
        {
            MaxOccluderDistance = 2,
            MsaaSamples = MsaaSamples._4,
            MsaaDetailDetection = MsaaDetailDetection.Medium,
            TextureSize = TextureSize._128,
            TracerTextureSize = TracerResolution._64,
            TracerFov = 170,
            DilateIterations = 20,
            DenoiseIterations = 2,
            Gamma = 1.0f
        };

        public void Validate()
        {
            if ((int)MsaaSamples > (int)TextureSize)
                MsaaSamples = (MsaaSamples)TextureSize;
            MaxOccluderDistance = Mathf.Clamp(MaxOccluderDistance, 0.000001f, 1000000.0f);
            TracerFov = Mathf.Clamp(TracerFov, 90.0f, 170.0f);
            DilateIterations = Mathf.Clamp(DilateIterations, 0, 1024);
            DenoiseIterations = Mathf.Clamp(DenoiseIterations, 0, 10);
            Gamma = Mathf.Clamp(Gamma, 0.25f, 4.0f);
        }
    }
}


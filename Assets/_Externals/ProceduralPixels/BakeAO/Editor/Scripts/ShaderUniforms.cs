/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    internal static class ShaderUniforms
    {
        public static readonly int _TracerTarget = Shader.PropertyToID("_TracerTarget");
        public static readonly int _TracerDepthTarget = Shader.PropertyToID("_TracerDepthTarget");
        public static readonly int _TempAoRT = Shader.PropertyToID("_TempAoRT");
        public static readonly int _PositionWSTexture = Shader.PropertyToID("_PositionWSTexture");
        public static readonly int _NormalWSTexture = Shader.PropertyToID("_NormalWSTexture");
        public static readonly int _AOBake_MatrixM = Shader.PropertyToID("_AOBake_MatrixM");
        public static readonly int _AOBake_MatrixMInv = Shader.PropertyToID("_AOBake_MatrixMInv");
        public static readonly int _AOBake_MatrixV = Shader.PropertyToID("_AOBake_MatrixV");
        public static readonly int _AOBake_MatrixP = Shader.PropertyToID("_AOBake_MatrixP");
        public static readonly int _AOBake_PixelScaleWS = Shader.PropertyToID("_AOBake_PixelScaleWS");
        public static readonly int _PixelCoord = Shader.PropertyToID("_PixelCoord");
        public static readonly int _TextureSize = Shader.PropertyToID("_TextureSize");
        public static readonly int _ScaleAndTransform = Shader.PropertyToID("_ScaleAndTransform");
        public static readonly int _TracerTextureSize = Shader.PropertyToID("_TracerTextureSize");
        public static readonly int _MaxOccluderDistance = Shader.PropertyToID("_MaxOccluderDistance");
        public static readonly int _GammaFactor = Shader.PropertyToID("_GammaFactor");
    }
}

/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    internal enum TracerResolution
    {
        _16 = 16,
        _32 = 32,
        _64 = 64,
        _128 = 128,
        _256 = 256
    }

    internal static class TracerResolutionUtility
    {
        public static void SetupShaderUniforms(TracerResolution tracerResolution)
        {
            if (tracerResolution == TracerResolution._16)
                Shader.EnableKeyword("TRACER_RESOLUTION_16");
            else
                Shader.DisableKeyword("TRACER_RESOLUTION_16");

            if (tracerResolution == TracerResolution._32)
                Shader.EnableKeyword("TRACER_RESOLUTION_32");
            else
                Shader.DisableKeyword("TRACER_RESOLUTION_32");

            if (tracerResolution == TracerResolution._64)
                Shader.EnableKeyword("TRACER_RESOLUTION_64");
            else
                Shader.DisableKeyword("TRACER_RESOLUTION_64");

            if (tracerResolution == TracerResolution._128)
                Shader.EnableKeyword("TRACER_RESOLUTION_128");
            else
                Shader.DisableKeyword("TRACER_RESOLUTION_128");

            if (tracerResolution == TracerResolution._256)
                Shader.EnableKeyword("TRACER_RESOLUTION_256");
            else
                Shader.DisableKeyword("TRACER_RESOLUTION_256");
        }
    }
}
/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralPixels.BakeAO.Editor
{
    internal class BakeAOPass : BakePass
    {
        public enum BakeStatus
        {
            None,
            Prepaired,
            Baking,
            Completed
        }

        public float Progress => lastFrameIndex / (float)bakingData.RayGenerationData.Count;

        private BakingData bakingData;
        private BakingSetup bakingSetup;

        private int maxSampleCountPerFrame = 10000;
        private int lastFrameIndex = 0;
        private float maxTimeInSeconds = 0.1f;

        public BakeStatus Status => status;
        private BakeStatus status = BakeStatus.None;

        public BakeAOPass() : base()
        {

        }

        public void Setup(BakingData bakingData, BakingSetup bakingSetup)
        {
            this.bakingData = bakingData;
            this.bakingSetup = bakingSetup;
            maxSampleCountPerFrame = 10;

            TracerResolutionUtility.SetupShaderUniforms(bakingSetup.quality.TracerTextureSize);

            status = BakeStatus.Prepaired;
        }

        private void PrepareTracerTexture(CommandBuffer cmd)
        {
            int textureSize = (int)bakingSetup.quality.TracerTextureSize;
            cmd.SetGlobalFloat(ShaderUniforms._TracerTextureSize, textureSize);
            cmd.GetTemporaryRT(ShaderUniforms._TracerTarget, GetTracerDescriptor(textureSize));
            cmd.GetTemporaryRT(ShaderUniforms._TracerDepthTarget, GetTracerDepthDescriptor(textureSize));
        }

        public static RenderTextureDescriptor GetTracerDescriptor(int textureSize)
        {
            return new RenderTextureDescriptor()
            {
                width = textureSize,
                height = textureSize,
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R16_SFloat,
                colorFormat = RenderTextureFormat.RHalf,
                dimension = TextureDimension.Tex2D,
                volumeDepth = 1,
                msaaSamples = 8,
                depthBufferBits = 0,
                autoGenerateMips = false,
                useMipMap = false,
                sRGB = true,
            };
        }

        public static RenderTextureDescriptor GetTracerDepthDescriptor(int textureSize)
        {
            return new RenderTextureDescriptor()
            {
                width = textureSize,
                height = textureSize,
                graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat,
                colorFormat = RenderTextureFormat.RFloat,
                dimension = TextureDimension.Tex2D,
                volumeDepth = 1,
                msaaSamples = 8,
                depthBufferBits = 32,
                autoGenerateMips = false,
                useMipMap = false,
                sRGB = false,
            };
        }

        List<MeshContext> combinedOccluder;
        List<UnityEngine.Object> allocatedAssets = new();

        public override void Execute()
        {
            CommandBuffer cmd = GlobalPool<CommandBuffer>.Get();
            cmd.Clear();
            PrepareTracerTexture(cmd);

            if (status == BakeStatus.Baking)
            {
                var stopwatch = Stopwatch.StartNew();

                float textureSize = bakingData.aoRT.width;

                cmd.SetGlobalFloat(ShaderUniforms._TextureSize, textureSize);
                cmd.SetGlobalFloat(ShaderUniforms._MaxOccluderDistance, bakingSetup.quality.MaxOccluderDistance);
                cmd.SetGlobalFloat(ShaderUniforms._GammaFactor, bakingSetup.quality.Gamma);

                for (int i = lastFrameIndex; i <= lastFrameIndex + maxSampleCountPerFrame && i < bakingData.RayGenerationData.Count; i++)
                {
                    var sample = bakingData.RayGenerationData[i];

                    if ((-sample.normalWS.normalized).magnitude > 0.1f)
                    {
                        TraceIntoRenderTarget(cmd, sample);

                        // Apply pixel
                        cmd.SetRenderTarget(bakingData.aoRT);
                        cmd.SetGlobalVector(ShaderUniforms._PixelCoord, new Vector4(sample.uv.x * textureSize, sample.uv.y * textureSize, 0, 0));
                        cmd.DrawMesh(BakeAOResources.Instance.QuadMesh, Matrix4x4.identity, BakeAOResources.Instance.AOWritePixelMaterial);
                    }
                }

                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

                lastFrameIndex += maxSampleCountPerFrame;
                if (lastFrameIndex >= bakingData.RayGenerationData.Count)
                {
                    cmd.GetTemporaryRT(ShaderUniforms._TempAoRT, bakingData.aoRTDescriptor);

                    cmd.SetGlobalFloat(ShaderUniforms._TextureSize, (float)bakingData.textureSize);

                    cmd.Blit(bakingData.aoRT, ShaderUniforms._TempAoRT, BakeAOResources.Instance.PostprocessMaterial);
                    cmd.Blit(ShaderUniforms._TempAoRT, bakingData.aoRT);

                    if (bakingSetup.quality.FixShadowArtifacts)
                    {
                        cmd.Blit(bakingData.aoRT, ShaderUniforms._TempAoRT, BakeAOResources.Instance.ShiftShadowsMaterial);
                        cmd.Blit(ShaderUniforms._TempAoRT, bakingData.aoRT);
                    }

                    for (int i = 0; i < bakingSetup.quality.DenoiseIterations; i++)
                    {
                        cmd.SetGlobalTexture(ShaderUniforms._PositionWSTexture, bakingData.worldRT);
                        cmd.SetGlobalTexture(ShaderUniforms._NormalWSTexture, bakingData.normalRT);
                        cmd.Blit(bakingData.aoRT, ShaderUniforms._TempAoRT, BakeAOResources.Instance.DenoiseMaterial);
                        cmd.Blit(ShaderUniforms._TempAoRT, bakingData.aoRT);
                    }

                    for (int i = 0; i < bakingSetup.quality.DilateIterations; i++)
                    {
                        cmd.Blit(bakingData.aoRT, ShaderUniforms._TempAoRT, BakeAOResources.Instance.DilateMaterial);
                        cmd.Blit(ShaderUniforms._TempAoRT, bakingData.aoRT);
                    }

                    cmd.ReleaseTemporaryRT(ShaderUniforms._TempAoRT);

                    status = BakeStatus.Completed;
                }

                var measuredTime = (float)stopwatch.Elapsed.TotalSeconds;
                measuredTime = Mathf.Clamp(measuredTime, 0.000001f, 1.0f);

                maxTimeInSeconds = BakeAOPreferences.instance.GetBakingFrameTime();
                float adjustment = maxTimeInSeconds / measuredTime;

                if (adjustment > 1.0)
                    adjustment = Mathf.Lerp(adjustment, 1.0f, 0.1f);
                if (adjustment < 1.0)
                    adjustment = Mathf.Lerp(adjustment, 1.0f, 0.5f);

                adjustment = Mathf.Clamp(adjustment, 0.25f, 4.0f);
                maxSampleCountPerFrame = Mathf.Clamp(Mathf.FloorToInt(maxSampleCountPerFrame * adjustment), 1, 50000);
            }

            if (status == BakeStatus.Prepaired)
            {
                lastFrameIndex = 0;
                status = BakeStatus.Baking;

                cmd.SetRenderTarget(bakingData.aoRT);
                cmd.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
            }

            ReleaseTracerTexture(cmd);

            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            GlobalPool<CommandBuffer>.Return(cmd);
        }

        internal void ReleaseAllocatedResources()
        {
            if (allocatedAssets != null)
            {
                for (int i = 0; i < allocatedAssets.Count; i++)
                {
                    Object createdAsset = allocatedAssets[i];
                    UnityEngine.Object.DestroyImmediate(createdAsset, true);
                }

                allocatedAssets.Clear();
                allocatedAssets = null;
            }

            if (combinedOccluder != null)
            {
                combinedOccluder.Clear();
                combinedOccluder = null;
            }    
        }

        private void TraceIntoRenderTarget(CommandBuffer cmd, SampleData sample)
        {
            float tracerFov = bakingSetup.quality.TracerFov;
            float maxOccluderDistance = bakingSetup.quality.MaxOccluderDistance;
            if (combinedOccluder == null)
                combinedOccluder = CreateCombinedOccluder(bakingSetup.occluders);
            //TraceIntoRenderTarget(cmd, ShaderUniforms._TracerTarget, Vector2.one * (int)bakingSetup.BakingQuality.TextureSize, ShaderUniforms._TracerDepthTarget, sample, tracerFov, maxOccluderDistance, bakingSetup, combinedOccluder);
            TraceIntoRenderTarget(cmd, ShaderUniforms._TracerTarget, Vector2.one * (int)bakingSetup.BakingQuality.TextureSize, ShaderUniforms._TracerDepthTarget, sample, tracerFov, maxOccluderDistance, bakingSetup, combinedOccluder);
        }

        private static int CombineMeshCounter = 0;

        private List<MeshContext> CreateCombinedOccluder(List<MeshContext> occluders)
        {
            if (allocatedAssets == null)
                allocatedAssets = new List<Object>();

            var combineInstancesNormalBias = occluders.Where(o => !o.useFlags.HasFlag(MeshContextUseFlags.DontCombine) && o.useFlags.HasFlag(MeshContextUseFlags.ShouldApplyNormalBias)).SelectMany(o => o.GetCombineInstances()).ToArray();
            var combineInstancesNoBias = occluders.Where(o => !o.useFlags.HasFlag(MeshContextUseFlags.DontCombine) && !o.useFlags.HasFlag(MeshContextUseFlags.ShouldApplyNormalBias)).SelectMany(o => o.GetCombineInstances()).ToArray();

            var combinedOccluders = occluders.Where(o => o.useFlags.HasFlag(MeshContextUseFlags.DontCombine));

            Mesh meshBias;

            if (combineInstancesNormalBias.Length > 0)
            {
                meshBias = new Mesh();
                meshBias.name = "BakeAOPass_CombinedMesh_Bias_" + (CombineMeshCounter++);
                meshBias.hideFlags = HideFlags.DontSaveInEditor;
                meshBias.indexFormat = IndexFormat.UInt32;
                meshBias.CombineMeshes(combineInstancesNormalBias, true, true, false);
                meshBias.RecalculateBounds();

                allocatedAssets.Add(meshBias);

                combinedOccluders = combinedOccluders.Append(new MeshContext(meshBias, UVChannel.UV0, MeshContextUseFlags.ShouldApplyNormalBias));
            }

            Mesh meshNoBias;

            if (combineInstancesNoBias.Length > 0)
            {
                meshNoBias = new Mesh();
                meshNoBias.name = "BakeAOPass_CombinedMesh_NoBias_" + (CombineMeshCounter++);
                meshNoBias.hideFlags = HideFlags.DontSaveInEditor;
                meshNoBias.indexFormat = IndexFormat.UInt32;
                meshNoBias.CombineMeshes(combineInstancesNoBias, true, true, false);
                meshNoBias.RecalculateBounds();

                allocatedAssets.Add(meshNoBias);

                combinedOccluders = combinedOccluders.Append(new MeshContext(meshNoBias, UVChannel.UV0, MeshContextUseFlags.Default));
            }

            return combinedOccluders.ToList();
        }

        public static void TraceIntoRenderTarget(CommandBuffer cmd, RenderTargetIdentifier renderTarget, Vector2 renderTargetResolution, RenderTargetIdentifier depthRT, SampleData sample, float tracerFov, float maxOccluderDistance, IProvideMeshesToBake meshesToBake, IReadOnlyList<MeshContext> occluders, bool deterministic = false)
        {
            Vector3 cameraForward = -sample.normalWS.normalized;
            Vector3 randomVector;

            if (deterministic)
                randomVector = new Vector3(2.64875234586f, 0.36845782354f, 1.27863123124f);
            else
                randomVector = new Vector3(Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f), Random.Range(-1.0f, 1.0f)).normalized;

            Quaternion rotation = Quaternion.LookRotation(cameraForward, Vector3.Cross(randomVector, cameraForward).normalized);
            Matrix4x4 viewToWorld = Matrix4x4.TRS(sample.positionWS, rotation, Vector3.one);
            Matrix4x4 worldToView = viewToWorld.inverse;

            Matrix4x4 projection = GL.GetGPUProjectionMatrix(Matrix4x4.Perspective(tracerFov, 1.0f, 0.001f, maxOccluderDistance), true);

            cmd.SetGlobalFloat(ShaderUniforms._MaxOccluderDistance, maxOccluderDistance);

            cmd.SetGlobalMatrix(ShaderUniforms._AOBake_MatrixV, worldToView);
            cmd.SetGlobalMatrix(ShaderUniforms._AOBake_MatrixP, projection);

            cmd.SetRenderTarget(renderTarget, depthRT);
            cmd.ClearRenderTarget(true, true, Color.white);

            BakeAOUtils.FastBoundsTransform boundsTransformer = BakeAOUtils.FastBoundsTransform.Create();

            float maxUVDistributionMetric = 0.0f;

            // TODO: Make occluder only occlude
            for (int meshIndex = 0; meshIndex < meshesToBake.MeshesToBake.Count; meshIndex++)
            {
                var meshToBake = meshesToBake.MeshesToBake[meshIndex];
                if (ShouldRender(meshToBake))
                {
                    //float uvDistributionMetric = Mathf.Sqrt(meshToBake.mesh.GetUVDistributionMetric((int)meshToBake.uv));
                    //uvDistributionMetric *= meshToBake.objectToWorld.lossyScale.magnitude; // TODO: Check it, something is wrong with UV distribution
                    maxUVDistributionMetric = Mathf.Max(maxUVDistributionMetric, 1.0f / meshToBake.uvToWorldRatio);
                    cmd.SetGlobalMatrix(ShaderUniforms._AOBake_MatrixM, meshToBake.objectToWorld);
                    cmd.SetGlobalFloat(ShaderUniforms._AOBake_PixelScaleWS, 0.0f);
                    for (int submeshIndex = 0; submeshIndex < meshToBake.mesh.subMeshCount; submeshIndex++)
                        cmd.DrawMesh(meshToBake.mesh, meshToBake.objectToWorld, BakeAOResources.Instance.AOBakeMaterial, submeshIndex, 0);
                }
            }

            float pixelScaleWS = maxUVDistributionMetric / renderTargetResolution.x;

            if (occluders != null)
            {
                for (int meshIndex = 0; meshIndex < occluders.Count; meshIndex++)
                {
                    var meshToBake = occluders[meshIndex];
                    if (ShouldRender(meshToBake))
                    {
                        cmd.SetGlobalFloat(ShaderUniforms._AOBake_PixelScaleWS, pixelScaleWS * 0.5f * (meshToBake.useFlags.HasFlag(MeshContextUseFlags.ShouldApplyNormalBias) ? 1.0f : 0.0f));
                        cmd.SetGlobalMatrix(ShaderUniforms._AOBake_MatrixM, meshToBake.objectToWorld);
                        for (int submeshIndex = 0; submeshIndex < meshToBake.mesh.subMeshCount; submeshIndex++)
                            cmd.DrawMesh(meshToBake.mesh, meshToBake.objectToWorld, BakeAOResources.Instance.AOBakeMaterial, submeshIndex, 0);
                    }
                }
            }

            bool ShouldRender(MeshContext context)
            {
                var boundsWS = boundsTransformer.TransformBounds(context.mesh.bounds, context.objectToWorld);
                float boundsRadius = boundsWS.extents.magnitude;
                if (Vector3.Distance(boundsWS.center, sample.positionWS) > boundsRadius + maxOccluderDistance)
                    return false;
                //return BakeAOUtils.TestPlanesAABB(frustumPlanes, boundsTransformer.TransformBounds(context.mesh.bounds, context.objectToWorld));

                return true;
            }
        }

        public void ReleaseTracerTexture(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(ShaderUniforms._TracerTarget);
            cmd.ReleaseTemporaryRT(ShaderUniforms._TracerDepthTarget);
        }
    }
}
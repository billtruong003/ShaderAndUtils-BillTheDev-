using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace INab.AdvancedEdgeDetection.URP
{
    public class CustomDataPass : ScriptableRenderPass
    {
        public readonly RTHandle CustomDataRT;
        private readonly Material m_Material;
        private readonly Material m_CustomTextureMaterial;

        private readonly List<ShaderTagId> shaderTagIdList;
        private FilteringSettings filteringSettings;

        private EdgeDetectionSettings m_settings;

        public CustomDataPass(RenderPassEvent renderPassEvent, EdgeDetectionSettings settings) 
        {
            this.renderPassEvent = renderPassEvent;
            m_settings = settings;

            m_Material = CoreUtils.CreateEngineMaterial(Shader.Find("Shader Graphs/CustomData"));
            m_Material.SetInt("_UseCustomTexture", 0);

            m_CustomTextureMaterial = CoreUtils.CreateEngineMaterial(Shader.Find("Shader Graphs/CustomData"));
            m_CustomTextureMaterial.SetTexture("_CustomTexture", settings._CustomTexture);
            m_CustomTextureMaterial.EnableKeyword("_USECUSTOMTEXTURE");

            shaderTagIdList = new List<ShaderTagId>()
            {
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("LightweightForward"),
                new ShaderTagId("SRPDefoultUnlit"),
                new ShaderTagId("DepthOnly"),
                new ShaderTagId("UniversalGBuffer"),
                new ShaderTagId("DepthNormalsOnly"),
                new ShaderTagId("Universal2D"),
                new ShaderTagId("SRPDefaultUnlit"),
            };

            CustomDataRT = RTHandles.Alloc("_CustomDataRT", name: "_CustomDataRT");
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            RenderTextureDescriptor textureDescriptor = cameraTextureDescriptor;
            textureDescriptor.colorFormat = RenderTextureFormat.ARGBFloat;
            textureDescriptor.msaaSamples = 1;

            cmd.GetTemporaryRT(Shader.PropertyToID(CustomDataRT.name), textureDescriptor, FilterMode.Point);
            ConfigureTarget(CustomDataRT);
            ConfigureClear(ClearFlag.All, Color.black);
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType == CameraType.Preview) return;

            if (!m_Material)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, new ProfilingSampler(
                "Edge Detection Custom Data")))
            {
                DrawingSettings drawingSettings = CreateDrawingSettings(shaderTagIdList, ref renderingData, renderingData.cameraData.defaultOpaqueSortFlags);
                drawingSettings.overrideMaterial = m_Material;
                drawingSettings.enableDynamicBatching = true;

                filteringSettings = new FilteringSettings(RenderQueueRange.all, m_settings._CustomDataLayerMask);
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);

                if(m_settings._UseCustomTexture)
                {
                    drawingSettings.overrideMaterial = m_CustomTextureMaterial;
                    filteringSettings = new FilteringSettings(RenderQueueRange.all, m_settings._CustomTextureLayerMask);
                    context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
                }
              
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(Shader.PropertyToID(CustomDataRT.name));
        }
    }
}
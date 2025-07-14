using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class TransparentQueueProfilerFeature : ScriptableRendererFeature
{
    private class TransparentQueueProfilingPass : ScriptableRenderPass
    {
        private readonly ProfilingSampler _profilingSampler;
        private readonly FilteringSettings _filteringSettings;
        private readonly List<ShaderTagId> _shaderTagIdList = new List<ShaderTagId>();

        public TransparentQueueProfilingPass(string profilerTag, LayerMask layerMask)
        {
            _profilingSampler = new ProfilingSampler(profilerTag);
            _filteringSettings = new FilteringSettings(RenderQueueRange.transparent, layerMask);

            _shaderTagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            _shaderTagIdList.Add(new ShaderTagId("UniversalForward"));
            _shaderTagIdList.Add(new ShaderTagId("UniversalForwardOnly"));
            _shaderTagIdList.Add(new ShaderTagId("LightweightForward"));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var drawingSettings = CreateDrawingSettings(_shaderTagIdList, ref renderingData, SortingCriteria.CommonTransparent);
            var cmd = CommandBufferPool.Get();
            var filteringSettings = _filteringSettings;

            using (new ProfilingScope(cmd, _profilingSampler))
            {
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    [System.Serializable]
    public class Settings
    {
        public string profilerTag = "Transparent Queue Profile";
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public LayerMask layerMask = -1;
    }

    public Settings settings = new Settings();
    private TransparentQueueProfilingPass _scriptablePass;

    public override void Create()
    {
        _scriptablePass = new TransparentQueueProfilingPass(settings.profilerTag, settings.layerMask)
        {
            renderPassEvent = settings.renderPassEvent
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_scriptablePass);
    }
}
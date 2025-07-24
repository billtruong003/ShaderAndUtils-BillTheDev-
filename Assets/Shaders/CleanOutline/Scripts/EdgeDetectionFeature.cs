using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

[DisallowMultipleComponent]
[Tooltip("Thêm hiệu ứng phát hiện cạnh toàn màn hình với nhiều thuật toán tùy chọn.")]
public sealed class AdvancedEdgeDetectionFeature : ScriptableRendererFeature
{
    public enum EdgeDetectionAlgorithm
    {
        RobertsCross,
        Prewitt,
        Sobel,
        Laplacian
    }

    [Serializable]
    public sealed class EdgeDetectionSettings
    {
        [Tooltip("Thuật toán phát hiện cạnh sẽ được sử dụng.")]
        public EdgeDetectionAlgorithm algorithm = EdgeDetectionAlgorithm.Sobel;

        [Tooltip("Thời điểm thực thi hiệu ứng trong pipeline.")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        [Tooltip("Màu của đường viền.")]
        public Color outlineColor = Color.black;

        [Tooltip("Độ dày của đường viền.")]
        [Range(1, 10)] public float outlineThickness = 3;

        [Tooltip("Độ nhạy với sự thay đổi về chiều sâu.")]
        [Range(0.1f, 100f)] public float depthSensitivity = 20f;

        [Tooltip("Độ nhạy với sự thay đổi về pháp tuyến bề mặt.")]
        [Range(0.1f, 10f)] public float normalSensitivity = 1f;

        [Tooltip("Độ nhạy với sự thay đổi về độ sáng.")]
        [Range(0.1f, 5f)] public float luminanceSensitivity = 1f;
    }

    [SerializeField]
    private EdgeDetectionSettings settings = new EdgeDetectionSettings();

    [Tooltip("Shader dùng cho hiệu ứng phát hiện cạnh.")]
    [SerializeField]
    private Shader edgeDetectionShader;

    private Material edgeDetectionMaterial;
    private EdgeDetectionPass edgeDetectionPass;

    private static readonly string[] ALGORITHM_KEYWORDS =
    {
        "_ALGORITHM_ROBERTS_CROSS",
        "_ALGORITHM_PREWITT",
        "_ALGORITHM_SOBEL",
        "_ALGORITHM_LAPLACIAN"
    };

    private static class ShaderPropertyIDs
    {
        internal static readonly int OutlineColor = Shader.PropertyToID("_OutlineColor");
        internal static readonly int OutlineThickness = Shader.PropertyToID("_OutlineThickness");
        internal static readonly int DepthSensitivity = Shader.PropertyToID("_DepthSensitivity");
        internal static readonly int NormalSensitivity = Shader.PropertyToID("_NormalSensitivity");
        internal static readonly int LuminanceSensitivity = Shader.PropertyToID("_LuminanceSensitivity");
    }

    public override void Create()
    {
        edgeDetectionPass ??= new EdgeDetectionPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!ShouldRender(renderingData.cameraData)) return;

        if (!EnsureMaterialCreated())
        {
            Debug.LogWarningFormat("Shader for Advanced Edge Detection not assigned or invalid.");
            return;
        }

        edgeDetectionPass.Setup(settings, edgeDetectionMaterial);
        renderer.EnqueuePass(edgeDetectionPass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(edgeDetectionMaterial);
        edgeDetectionPass = null;
    }

    private bool ShouldRender(CameraData cameraData)
    {
        return cameraData.cameraType == CameraType.Game || cameraData.cameraType == CameraType.SceneView;
    }

    private bool EnsureMaterialCreated()
    {
        if (edgeDetectionMaterial != null) return true;
        if (edgeDetectionShader == null) return false;
        edgeDetectionMaterial = CoreUtils.CreateEngineMaterial(edgeDetectionShader);
        return edgeDetectionMaterial != null;
    }

    private class EdgeDetectionPass : ScriptableRenderPass
    {
        private Material material;
        private ProfilingSampler profilingSampler = new ProfilingSampler("Advanced Edge Detection");

        public EdgeDetectionPass()
        {
            ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Color);
        }

        public void Setup(EdgeDetectionSettings settings, Material edgeDetectionMaterial)
        {
            material = edgeDetectionMaterial;
            renderPassEvent = settings.renderPassEvent;

            material.SetColor(ShaderPropertyIDs.OutlineColor, settings.outlineColor);
            material.SetFloat(ShaderPropertyIDs.OutlineThickness, settings.outlineThickness);
            material.SetFloat(ShaderPropertyIDs.DepthSensitivity, settings.depthSensitivity);
            material.SetFloat(ShaderPropertyIDs.NormalSensitivity, settings.normalSensitivity);
            material.SetFloat(ShaderPropertyIDs.LuminanceSensitivity, settings.luminanceSensitivity);

            for (int i = 0; i < ALGORITHM_KEYWORDS.Length; i++)
            {
                material.DisableKeyword(ALGORITHM_KEYWORDS[i]);
            }
            material.EnableKeyword(ALGORITHM_KEYWORDS[(int)settings.algorithm]);
        }

        private class PassData { }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Advanced Edge Detection Pass", out _, profilingSampler))
            {
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.UseAllGlobalTextures(true);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData _, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, Vector2.one, material, 0);
                });
            }
        }
    }
}
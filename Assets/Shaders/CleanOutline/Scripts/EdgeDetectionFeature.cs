using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class AdvancedEdgeDetectionFeature : ScriptableRendererFeature
{
    [Serializable]
    public class EdgeDetectionSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;

        [Range(0, 15)]
        public float outlineThickness = 3; // Đổi thành float để có sự tinh chỉnh tốt hơn
        public Color outlineColor = Color.black;

        [Header("Edge Sensitivities")]
        [Tooltip("Controls sensitivity to depth differences. Higher values detect only large depth gaps, ideal for exterior outlines.")]
        [Range(0.0f, 1000.0f)]
        public float depthSensitivity = 200.0f;

        [Tooltip("Controls sensitivity to surface normal changes. Higher values detect sharp angles, ideal for interior details.")]
        [Range(0.0f, 50.0f)]
        public float normalSensitivity = 4.0f;

        [Tooltip("Controls sensitivity to luminance (color brightness) differences. Higher values detect edges between different colored areas.")]
        [Range(0.0f, 20.0f)]
        public float luminanceSensitivity = 2.0f;

        [Tooltip("Controls the transition softness of the outline. Higher values create a softer, more feathered edge.")]
        [Range(0.0f, 5.0f)] // Bắt đầu từ > 1 để tránh chia cho 0
        public float edgeSoftness = 1.5f; // Tham số mới
    }

    [SerializeField]
    private EdgeDetectionSettings settings;

    private Material edgeDetectionMaterial;
    private AdvancedEdgeDetectionPass edgeDetectionPass;

    private static readonly int OutlineThicknessProperty = Shader.PropertyToID("_OutlineThickness");
    private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");
    private static readonly int DepthSensitivityProperty = Shader.PropertyToID("_DepthSensitivity");
    private static readonly int NormalSensitivityProperty = Shader.PropertyToID("_NormalSensitivity");
    private static readonly int LuminanceSensitivityProperty = Shader.PropertyToID("_LuminanceSensitivity");
    private static readonly int EdgeSoftnessProperty = Shader.PropertyToID("_EdgeSoftness"); // ID mới

    public override void Create()
    {
        edgeDetectionPass ??= new AdvancedEdgeDetectionPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Preview || renderingData.cameraData.cameraType == CameraType.Reflection)
        {
            return;
        }

        if (!EnsureMaterialIsCreated())
        {
            return;
        }

        edgeDetectionPass.ConfigureInput(ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal | ScriptableRenderPassInput.Color);
        edgeDetectionPass.requiresIntermediateTexture = true;

        edgeDetectionPass.Setup(settings, edgeDetectionMaterial);

        renderer.EnqueuePass(edgeDetectionPass);
    }

    protected override void Dispose(bool disposing)
    {
        edgeDetectionPass = null;
        CoreUtils.Destroy(edgeDetectionMaterial);
        edgeDetectionMaterial = null;
    }

    private bool EnsureMaterialIsCreated()
    {
        if (edgeDetectionMaterial != null) return true;
        edgeDetectionMaterial = CoreUtils.CreateEngineMaterial("Hidden/Advanced Edge Detection");
        if (edgeDetectionMaterial == null)
        {
            Debug.LogError("Advanced Edge Detection material could not be created. The effect will not be rendered.");
            return false;
        }
        return true;
    }

    private class AdvancedEdgeDetectionPass : ScriptableRenderPass
    {
        private Material material;

        private class PassData { }

        public AdvancedEdgeDetectionPass()
        {
            profilingSampler = new ProfilingSampler(nameof(AdvancedEdgeDetectionPass));
        }

        public void Setup(EdgeDetectionSettings passSettings, Material edgeDetectionMaterial)
        {
            this.material = edgeDetectionMaterial;
            renderPassEvent = passSettings.renderPassEvent;

            if (material != null)
            {
                material.SetFloat(OutlineThicknessProperty, passSettings.outlineThickness);
                material.SetColor(OutlineColorProperty, passSettings.outlineColor);
                material.SetFloat(DepthSensitivityProperty, passSettings.depthSensitivity);
                material.SetFloat(NormalSensitivityProperty, passSettings.normalSensitivity);
                material.SetFloat(LuminanceSensitivityProperty, passSettings.luminanceSensitivity);
                material.SetFloat(EdgeSoftnessProperty, passSettings.edgeSoftness); // Gửi tham số mới
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            using var builder = renderGraph.AddRasterRenderPass<PassData>("Advanced Edge Detection (Sobel)", out _);

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
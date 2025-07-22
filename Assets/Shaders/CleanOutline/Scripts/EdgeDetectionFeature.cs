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

        [Range(0f, 15f)]
        public float outlineThickness = 1.0f;
        public Color outlineColor = Color.black;

        [Header("Edge Sensitivities")]
        [Tooltip("Controls sensitivity to depth differences. Higher values detect smaller depth gaps.")]
        [Range(0.0f, 500.0f)]
        public float depthSensitivity = 200.0f;

        [Tooltip("Controls sensitivity to surface normal changes. Higher values detect more subtle angle changes.")]
        [Range(0.0f, 50.0f)]
        public float normalSensitivity = 4.0f;

        [Tooltip("Controls sensitivity to luminance differences. Higher values detect edges between similar colors.")]
        [Range(0.0f, 20.0f)]
        public float luminanceSensitivity = 2.0f;

        [Header("Appearance")]
        [Tooltip("Controls the falloff of the outline. A value of 0.01 is a sharp, hard edge. A value of 1.0 is a very soft, smooth gradient.")]
        [Range(0.01f, 1.0f)]
        public float outlineSoftness = 0.5f;
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
    private static readonly int OutlineSoftnessProperty = Shader.PropertyToID("_OutlineSoftness");

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

        var shader = Shader.Find("Hidden/Advanced Edge Detection");
        if (shader == null)
        {
            Debug.LogError("Shader 'Hidden/Advanced Edge Detection' not found. The effect will not be rendered.");
            return false;
        }

        edgeDetectionMaterial = CoreUtils.CreateEngineMaterial(shader);
        return edgeDetectionMaterial != null;
    }

    private class AdvancedEdgeDetectionPass : ScriptableRenderPass
    {
        private Material material;
        private EdgeDetectionSettings currentSettings;

        private class PassData { }

        public AdvancedEdgeDetectionPass()
        {
            profilingSampler = new ProfilingSampler(nameof(AdvancedEdgeDetectionPass));
        }

        public void Setup(EdgeDetectionSettings passSettings, Material edgeDetectionMaterial)
        {
            this.material = edgeDetectionMaterial;
            this.currentSettings = passSettings;
            renderPassEvent = currentSettings.renderPassEvent;
        }

        private void UpdateMaterialProperties()
        {
            if (material == null || currentSettings == null) return;

            material.SetFloat(OutlineThicknessProperty, currentSettings.outlineThickness);
            material.SetColor(OutlineColorProperty, currentSettings.outlineColor);
            material.SetFloat(DepthSensitivityProperty, currentSettings.depthSensitivity);
            material.SetFloat(NormalSensitivityProperty, currentSettings.normalSensitivity);
            material.SetFloat(LuminanceSensitivityProperty, currentSettings.luminanceSensitivity);
            material.SetFloat(OutlineSoftnessProperty, currentSettings.outlineSoftness);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            using var builder = renderGraph.AddRasterRenderPass<PassData>("Advanced Edge Detection (Sobel)", out _);

            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.UseAllGlobalTextures(true);
            builder.AllowPassCulling(false);

            UpdateMaterialProperties();

            builder.SetRenderFunc((PassData _, RasterGraphContext context) =>
            {
                Blitter.BlitTexture(context.cmd, Vector2.one, material, 0);
            });
        }
    }
}
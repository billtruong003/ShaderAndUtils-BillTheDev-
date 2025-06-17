using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomFogFeature : ScriptableRendererFeature
{
    public enum FogMode
    {
        LinearDepth,
        ExponentialDepth,
        ExponentialSquaredDepth,
        LinearDistance,
        ExponentialDistance,
        ExponentialSquaredDistance
    }

    [System.Serializable]
    public class FogSettings
    {
        public Material fogMaterial;
        public FogMode fogMode = FogMode.LinearDepth;
        public Color fogColor = new Color(0.8f, 0.9f, 1f, 1f);
        public float fogNear = 0f;
        public float fogFar = 100f;
        public float fogDensity = 0.1f;
    }

    class FogPass : ScriptableRenderPass
    {
        private FogSettings settings;
        private RTHandle source;
        private RenderTargetIdentifier tempTexture;
        private int tempTextureId = Shader.PropertyToID("_TempFogTexture");

        public FogPass(FogSettings settings)
        {
            this.settings = settings;
            this.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }

        public void Setup(RTHandle source)
        {
            this.source = source;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.GetTemporaryRT(tempTextureId, cameraTextureDescriptor);
            tempTexture = new RenderTargetIdentifier(tempTextureId);
            ConfigureInput(ScriptableRenderPassInput.Depth); // Yêu cầu depth texture
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("FogPass");

            // Thiết lập các thuộc tính material
            Material mat = settings.fogMaterial;
            mat.SetColor("_FogColor", settings.fogColor);
            mat.SetFloat("_FogNear", settings.fogNear);
            mat.SetFloat("_FogFar", settings.fogFar);
            mat.SetFloat("_Density", settings.fogDensity);

            // Xóa tất cả từ khóa trước khi bật từ khóa mới
            mat.DisableKeyword("FOG_LINEAR_DEPTH");
            mat.DisableKeyword("FOG_EXP_DEPTH");
            mat.DisableKeyword("FOG_EXP2_DEPTH");
            mat.DisableKeyword("FOG_LINEAR_DISTANCE");
            mat.DisableKeyword("FOG_EXP_DISTANCE");
            mat.DisableKeyword("FOG_EXP2_DISTANCE");

            // Bật từ khóa dựa trên chế độ được chọn
            switch (settings.fogMode)
            {
                case FogMode.LinearDepth:
                    mat.EnableKeyword("FOG_LINEAR_DEPTH");
                    break;
                case FogMode.ExponentialDepth:
                    mat.EnableKeyword("FOG_EXP_DEPTH");
                    break;
                case FogMode.ExponentialSquaredDepth:
                    mat.EnableKeyword("FOG_EXP2_DEPTH");
                    break;
                case FogMode.LinearDistance:
                    mat.EnableKeyword("FOG_LINEAR_DISTANCE");
                    break;
                case FogMode.ExponentialDistance:
                    mat.EnableKeyword("FOG_EXP_DISTANCE");
                    break;
                case FogMode.ExponentialSquaredDistance:
                    mat.EnableKeyword("FOG_EXP2_DISTANCE");
                    break;
            }

            // Thực hiện blit để áp dụng hiệu ứng sương mù
            cmd.Blit(source, tempTexture, mat);
            cmd.Blit(tempTexture, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempTextureId);
        }
    }

    [SerializeField]
    private FogSettings settings = new FogSettings();
    private FogPass fogPass;

    public override void Create()
    {
        fogPass = new FogPass(settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.fogMaterial == null) return;
        fogPass.Setup(renderer.cameraColorTargetHandle);
        renderer.EnqueuePass(fogPass);
    }
}
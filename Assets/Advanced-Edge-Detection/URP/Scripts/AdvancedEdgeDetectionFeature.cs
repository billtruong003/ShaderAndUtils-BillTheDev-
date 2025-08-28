using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace INab.AdvancedEdgeDetection.URP
{
	[System.Serializable]
	public class EdgeDetectionSettings
	{
		public RenderPassEvent Event = RenderPassEvent.BeforeRenderingTransparents;
		public LayerMask _CustomDataLayerMask;
		public LayerMask _DepthMaskLayerMask;

		public bool _UseCustomData = false;
		public bool _UseDepthMask = false;

		// Custom Data
		public bool _UseCustomTexture;
		public Texture _CustomTexture;
		public LayerMask _CustomTextureLayerMask;

		// Edge Detection properties

		// Main
		[Range(0,5)]
		public float _Thickness = 1.0f;
		public bool _ResolutionAdjust = false;

		// Depth Fade
		public bool _UseDepthFade = false;
		public float _FadeStart = 20;
		public float _FadeEnd = 40;

		// Normals
		public bool _NormalsEdgeDetection = true;
		[Range(.01f,1.5f)]
		public float _NormalsOffset = 0.1f;
		[Range(0,.99f)]
		public float _NormalsHardness = 0;
		[Range(1, 5)]
		public float _NormalsPower = 1;

		// Depth
		public bool _DepthEdgeDetection = true;
		public bool _AcuteAngleFix = false;
		[Range(0, 1)]
		public float _ViewDirThreshold = 1;
		[Range(0, 100)]
		public float _ViewDirThresholdScale = 50;
		[Range(0, 3)]
		public float _DepthThreshold = 1;
		[Range(0, 1)]
		public float _DepthHardness = .9f;
		[Range(1, 5)]
		public float _DepthPower = 5;


		// Edge Blend properties

		// Colors
		public Color _EdgeColor = Color.black;
		
		public bool _UseEdgeBlendDepthFade = false;
		public float _EdgeBlendFadeStart = 10;
		public float _EdgeBlendFadeEnd = 20;

		// Sketch
		public bool _UseSketchEdges = false;
		[Range(0, .01f)]
		public float _Amplitude = .005f;
		[Range(0, 150)]
		public float _Frequency = 40;
		[Range(0, 10)]
		public float _ChangesPerSecond = 0;


		// Grain
		public bool _UseGrain = false;
		public Texture2D _GrainTexture;
		[Range(0, 1)]
		public float _GrainStrength = 1;
		[Range(0, 3)]
		public float _GrainScale = 1;

		// UV Offset
		public bool _UseUvOffset = false;
		public Texture2D _OffsetNoise;
		[Range(0, 4)]
		public float _OffsetNoiseScale = .4f;
		[Range(0, 10)]
		public float _OffsetChangesPerSecond = 0;
		[Range(0, .01f)]
		public float _OffsetStrength = .005f;
	}


	public class AdvancedEdgeDetectionFeature : ScriptableRendererFeature
	{
		// Edge Detection Settings 
		public EdgeDetectionSettings settings = new EdgeDetectionSettings();

		// Custom Colors + Edge Detection + Edge Blend
		private EdgeDetectionPass edgeDetectionPass;

		// Objects ID, Vertex Colors, Custom Texture, Other custom stuff
		private CustomDataPass customDataPass;

		// Custom Depth Mask
		private DepthMaskPass depthMaskPass;

		private Material EdgeDetection;
		private Material EdgeBlend;

		public override void Create()
		{
			// Create materials
			EdgeDetection = CoreUtils.CreateEngineMaterial(Shader.Find("Shader Graphs/EdgeDetection"));
			EdgeBlend = CoreUtils.CreateEngineMaterial(Shader.Find("Shader Graphs/EdgeBlend"));

			edgeDetectionPass = new EdgeDetectionPass(settings.Event, settings,EdgeBlend,EdgeDetection);
			depthMaskPass = new DepthMaskPass(settings.Event, settings._DepthMaskLayerMask);
			customDataPass = new CustomDataPass(settings.Event, settings);
		}
		public override void SetupRenderPasses(ScriptableRenderer renderer,
									  in RenderingData renderingData)
		{
			// The target is used after allocation
			edgeDetectionPass.SetCameraTarget(renderer.cameraColorTargetHandle);
		}
		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			if (renderingData.cameraData.cameraType != CameraType.Game && renderingData.cameraData.cameraType != CameraType.SceneView) return;

			if (settings._UseDepthMask)renderer.EnqueuePass(depthMaskPass);
			if (settings._UseCustomData)renderer.EnqueuePass(customDataPass);

			edgeDetectionPass.Setup();
			renderer.EnqueuePass(edgeDetectionPass);
		}

		protected override void Dispose(bool disposing)
		{
			CoreUtils.Destroy(EdgeDetection);
			CoreUtils.Destroy(EdgeBlend);
		}
		public class EdgeDetectionPass : ScriptableRenderPass
		{
			public Material m_EdgeDetection;
			public Material m_EdgeBlend;

			private RTHandle cameraColorTarget;

			private RTHandle m_TemporaryColorTexture;
			private RTHandle m_EdgeRT; 

			private EdgeDetectionSettings m_settings;

			public EdgeDetectionPass(RenderPassEvent renderPassEvent, EdgeDetectionSettings settings, Material EdgeBlend, Material EdgeDetection)
			{
				this.renderPassEvent = renderPassEvent;
				m_settings = settings;

				m_EdgeDetection = EdgeDetection;
				m_EdgeBlend = EdgeBlend;

				m_TemporaryColorTexture = RTHandles.Alloc("_TemporaryColorTexture", name: "_TemporaryColorTexture");
				m_EdgeRT = RTHandles.Alloc("_EdgeRT", name: "_EdgeRT");

			}
			public void SetCameraTarget(RTHandle cameraColorTarget)
			{
				this.cameraColorTarget = cameraColorTarget;
			}

			private void SetEdgeDetectionProperties()
			{
				// Main
				m_EdgeDetection.SetFloat("_Thickness", m_settings._Thickness);
				m_EdgeDetection.SetInt("_ResolutionAdjust", m_settings._ResolutionAdjust ? 1 : 0);

				// Depth Fade
				if (m_settings._UseDepthFade)
				{
					m_EdgeDetection.EnableKeyword("_USEDEPTHFADE");
					m_EdgeDetection.SetFloat("_FadeStart", m_settings._FadeStart);
					m_EdgeDetection.SetFloat("_FadeEnd", m_settings._FadeEnd);
				}
				else
				{
					m_EdgeDetection.DisableKeyword("_USEDEPTHFADE_ON");
				}

				// Normals
				if (m_settings._NormalsEdgeDetection)
				{
					m_EdgeDetection.EnableKeyword("_NORMALS_EDGES");
					m_EdgeDetection.SetFloat("_NormalsOffset", m_settings._NormalsOffset);
					m_EdgeDetection.SetFloat("_NormalsHardness", m_settings._NormalsHardness);
					m_EdgeDetection.SetFloat("_NormalsPower", m_settings._NormalsPower);
				}
				else
				{
					m_EdgeDetection.DisableKeyword("_NORMALS_EDGES");
				}

				// Depth
				if (m_settings._DepthEdgeDetection)
				{
					m_EdgeDetection.EnableKeyword("_DEPTH_EDGES");
					m_EdgeDetection.SetFloat("_ViewDirThreshold", m_settings._ViewDirThreshold);
					m_EdgeDetection.SetFloat("_ViewDirThresholdScale", m_settings._ViewDirThresholdScale);
					m_EdgeDetection.SetFloat("_DepthThreshold", m_settings._DepthThreshold);
					m_EdgeDetection.SetFloat("_DepthHardness", m_settings._DepthHardness);
					m_EdgeDetection.SetFloat("_DepthPower", m_settings._DepthPower);
				}
				else
				{
					m_EdgeDetection.DisableKeyword("_DEPTH_EDGES");
				}

				if (m_settings._AcuteAngleFix)
				{
					m_EdgeDetection.EnableKeyword("_ACUTE_ANGLES_FIX");
				}
				else
				{
					m_EdgeDetection.DisableKeyword("_ACUTE_ANGLES_FIX");
				}

				// Custom Data
				if (m_settings._UseCustomData)
				{
					m_EdgeDetection.EnableKeyword("_CUSTOM_DATA_EDGES");
				}
				else
				{
					m_EdgeDetection.DisableKeyword("_CUSTOM_DATA_EDGES");
				}
			}

			private void SetEdgeBlendProperties()
			{
				if (m_settings._UseDepthMask)
				{
					m_EdgeBlend.EnableKeyword("_USEDEPTHMASK");
				}
				else
				{
					m_EdgeBlend.DisableKeyword("_USEDEPTHMASK");
				}

				// Colors
				m_EdgeBlend.SetColor("_EdgeColor", m_settings._EdgeColor);

				// Sketch
				if (m_settings._UseSketchEdges)
				{
					m_EdgeBlend.EnableKeyword("_USESKETCHEDGES");
					m_EdgeBlend.SetFloat("_Amplitude", m_settings._Amplitude);
					m_EdgeBlend.SetFloat("_Frequency", m_settings._Frequency);
					m_EdgeBlend.SetFloat("_ChangesPerSecond", m_settings._ChangesPerSecond);
				}
				else
				{
					m_EdgeBlend.DisableKeyword("_USESKETCHEDGES");
				}

				if (m_settings._UseEdgeBlendDepthFade)
				{
					m_EdgeBlend.EnableKeyword("_USEDEPTHFADE");
					m_EdgeBlend.SetFloat("_EdgeBlendFadeStart", m_settings._EdgeBlendFadeStart);
					m_EdgeBlend.SetFloat("_EdgeBlendFadeEnd", m_settings._EdgeBlendFadeEnd);
				}
				else
				{
					m_EdgeBlend.DisableKeyword("_USEDEPTHFADE");
				}

				// Grain
				if (m_settings._UseGrain)
				{
					m_EdgeBlend.EnableKeyword("_USEGRAIN");
					m_EdgeBlend.SetTexture("_GrainTexture", m_settings._GrainTexture);
					m_EdgeBlend.SetFloat("_GrainStrength", m_settings._GrainStrength);
					m_EdgeBlend.SetFloat("_GrainScale", m_settings._GrainScale);
				}
				else
				{
					m_EdgeBlend.DisableKeyword("_USEGRAIN");
				}

				// UV Offset
				if (m_settings._UseUvOffset)
				{
					m_EdgeBlend.EnableKeyword("_USEUVOFFSET");
					m_EdgeBlend.SetTexture("_OffsetNoise", m_settings._OffsetNoise);
					m_EdgeBlend.SetFloat("_OffsetNoiseScale", m_settings._OffsetNoiseScale);
					m_EdgeBlend.SetFloat("_OffsetChangesPerSecond", m_settings._OffsetChangesPerSecond);
					m_EdgeBlend.SetFloat("_OffsetStrength", m_settings._OffsetStrength);
				}
				else
				{
					m_EdgeBlend.DisableKeyword("_USEUVOFFSET");
				}
			}

			public void Setup()
			{
				ConfigureInput(ScriptableRenderPassInput.Normal);
			}

			public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
			{
				RenderTextureDescriptor textureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
				textureDescriptor.msaaSamples = 1;
				textureDescriptor.depthBufferBits = 0; // 32 bits breaks everytinhg

				RenderingUtils.ReAllocateIfNeeded(ref m_TemporaryColorTexture, textureDescriptor, FilterMode.Point, name: "_TemporaryRT");

				textureDescriptor.depthBufferBits = 0; // 32 bits breaks everytinhg
				textureDescriptor.colorFormat = RenderTextureFormat.RHalf;
				RenderingUtils.ReAllocateIfNeeded(ref m_EdgeRT, textureDescriptor, FilterMode.Point, name: "_FogFactor_RT");
			}

			public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
			{
				CommandBuffer cmd = CommandBufferPool.Get("Advanced Edge Detection");

				SetEdgeDetectionProperties();
				SetEdgeBlendProperties();

				Shader.SetGlobalMatrix("_InverseView", renderingData.cameraData.camera.cameraToWorldMatrix);

				Blitter.BlitCameraTexture(cmd, cameraColorTarget, m_EdgeRT, m_EdgeDetection, 0);
				cmd.SetGlobalTexture("_EdgeRT", m_EdgeRT);

				//cmd.SetGlobalTexture("_MainTex", cameraColorTarget);
				Blitter.BlitCameraTexture(cmd, cameraColorTarget, m_TemporaryColorTexture, m_EdgeBlend, 0);
				Blitter.BlitCameraTexture(cmd, m_TemporaryColorTexture, cameraColorTarget);

				//Blit(cmd, destination, m_EdgeRT.Identifier(), m_EdgeDetection, 0);
				//Blit(cmd, destination, m_TemporaryColorTexture.Identifier(), m_EdgeBlend, 0);
				//Blit(cmd, m_TemporaryColorTexture.Identifier(), destination);

				context.ExecuteCommandBuffer(cmd);
				CommandBufferPool.Release(cmd);
			}

			public override void FrameCleanup(CommandBuffer cmd)
			{
				cmd.ReleaseTemporaryRT(Shader.PropertyToID(m_TemporaryColorTexture.name));
				cmd.ReleaseTemporaryRT(Shader.PropertyToID(m_EdgeRT.name));

			}
		}
	}
}
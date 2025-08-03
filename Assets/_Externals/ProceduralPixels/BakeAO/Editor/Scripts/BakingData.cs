/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralPixels.BakeAO.Editor
{
    internal class BakingData
	{
		public RenderTexture normalRT;
		public RenderTexture worldRT;
		public RenderTexture depthRT;
		public RenderTexture aoRT;

		public Texture2D normalTex2D;
		public Texture2D worldTex2D;

		public List<SampleData> RayGenerationData = null;// new List<SampleData>(4000000);

		public TextureSize textureSize;

		public RenderTextureDescriptor normalRTDescriptor =>
			new RenderTextureDescriptor()
			{
				width = (int)textureSize,
				height = (int)textureSize,
				graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
				colorFormat = RenderTextureFormat.ARGBFloat,
				dimension = TextureDimension.Tex2D,
				volumeDepth = 1,
				msaaSamples = 1,
				depthBufferBits = 0,
				autoGenerateMips = false,
				sRGB = false,
			};

		public RenderTextureDescriptor worldRTDescriptor =>
			new RenderTextureDescriptor()
			{
				width = (int)textureSize,
				height = (int)textureSize,
				graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
				colorFormat = RenderTextureFormat.ARGBFloat,
				dimension = TextureDimension.Tex2D,
				volumeDepth = 1,
				msaaSamples = 1,
				depthBufferBits = 0,
				autoGenerateMips = false,
				sRGB = false,
			};

		public RenderTextureDescriptor depthRTDescriptor =>
			new RenderTextureDescriptor()
			{
				width = (int)textureSize,
				height = (int)textureSize,
				graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32_SFloat,
				colorFormat = RenderTextureFormat.Depth,
				dimension = TextureDimension.Tex2D,
				volumeDepth = 1,
				msaaSamples = 1,
				depthBufferBits = 32,
				autoGenerateMips = false,
				sRGB = false,
			};

		public RenderTextureDescriptor aoRTDescriptor =>
			new RenderTextureDescriptor()
			{
				width = (int)textureSize,
				height = (int)textureSize,
				graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat,
				colorFormat = RenderTextureFormat.ARGBFloat,
				dimension = TextureDimension.Tex2D,
				volumeDepth = 1,
				msaaSamples = 1,
				depthBufferBits = 32,
				autoGenerateMips = false,
				sRGB = false,
			};

		public BakingData(TextureSize textureSize)
		{
			this.textureSize = textureSize;
		}

		public long GetEstimatedMemorySize()
		{
			long pixelCount = (int)textureSize * (int)textureSize;
			return (RayGenerationData != null ? SampleData.Size * RayGenerationData.Count : 0)
				+ (pixelCount * 4 * 4) * 6; // approx 6 textures with 16 bytes per pixel
		}

		public void Initialize()
		{
			RayGenerationData = GlobalPool<List<SampleData>>.Get(); // Using bool to avoid memory fragmentation during long baking batches.

			normalRT = new RenderTexture(normalRTDescriptor);
			normalRT.Create();
			normalRT.hideFlags = HideFlags.DontSaveInEditor;
			normalRT.name = "BakingData_NormalRT";

			worldRT = new RenderTexture(worldRTDescriptor);
			worldRT.Create();
			worldRT.hideFlags = HideFlags.DontSaveInEditor;
			worldRT.name = "BakingData_WorldRT";

			depthRT = new RenderTexture(depthRTDescriptor);
			depthRT.Create();
			depthRT.hideFlags = HideFlags.DontSaveInEditor;
			depthRT.name = "BakingData_DepthRT";

			aoRT = new RenderTexture(aoRTDescriptor);
			aoRT.Create();
			aoRT.hideFlags = HideFlags.DontSaveInEditor;
			aoRT.name = "BakingData_AORT";

			normalTex2D = new Texture2D((int)textureSize, (int)textureSize, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
			normalTex2D.hideFlags = HideFlags.DontSaveInEditor;
			normalTex2D.name = "BakingData_NormalTex";

			worldTex2D = new Texture2D((int)textureSize, (int)textureSize, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, UnityEngine.Experimental.Rendering.TextureCreationFlags.None);
			worldTex2D.hideFlags = HideFlags.DontSaveInEditor;
			worldTex2D.name = "BakingData_WorldTex";
		}

		public void ReleaseRayGenerationData()
		{
            if (RayGenerationData != null)
            {
                RayGenerationData.Clear();
                GlobalPool<List<SampleData>>.Return(RayGenerationData);
                RayGenerationData = null;
            }
        }

		public void Release()
		{
			ReleaseRayGenerationData();

			ReleaseRTSafe(normalRT);
			ReleaseRTSafe(worldRT);
			ReleaseRTSafe(depthRT);
			ReleaseRTSafe(aoRT);

			if (worldTex2D != null)
				Object.DestroyImmediate(worldTex2D);
			if (normalTex2D != null)
				Object.DestroyImmediate(normalTex2D);
		}

		private void ReleaseRTSafe(RenderTexture rt)
		{
			if (rt == null)
				return;
			rt.Release();
			Object.DestroyImmediate(rt);
		}

		public void CreateTextures2D()
		{
			var activeTexture = RenderTexture.active;

			RenderTexture.active = normalRT;
			normalTex2D.ReadPixels(new Rect(0, 0, normalRT.width, normalRT.height), 0, 0, false);

			RenderTexture.active = worldRT;
			worldTex2D.ReadPixels(new Rect(0, 0, worldRT.width, worldRT.height), 0, 0, false);

			RenderTexture.active = activeTexture;
		}

		public void PrepareRayGenerationData()
		{
			RayGenerationData.Clear();

			var positions = worldTex2D.GetPixels(0, 0, normalTex2D.width, normalTex2D.height);
			var normals = normalTex2D.GetPixels(0, 0, normalTex2D.width, normalTex2D.height);
			for (int y = 0; y < worldTex2D.height; y++)
			{
				for (int x = 0; x < worldTex2D.width; x++)
				{
					var position = (Vector4)positions[y * worldTex2D.width + x];
					var normal = (Vector4)normals[y * worldTex2D.width + x];

					if (normal.x == 0.0f && normal.y == 0.0f && normal.z == 0.0f)
						continue;

					SampleData sampleData;
					sampleData.uv = new Vector2((x + 0.5f) / worldTex2D.width, (y + 0.5f) / worldTex2D.width);
					sampleData.positionWS = position;
					sampleData.normalWS = normal;

					RayGenerationData.Add(sampleData);
				}
			}
		}

		public void SaveDebugTextures2D()
		{
			var directoryPath = Path.Combine(Application.dataPath, "DebugTextures");
			if (!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);

			var bytes = normalTex2D.EncodeToEXR();
			var normalPath = Path.Combine(directoryPath, "bakedNormal.exr");
			File.WriteAllBytes(normalPath, bytes);

			bytes = worldTex2D.EncodeToEXR();
			var worldPath = Path.Combine(directoryPath, "bakedWorld.exr");
			File.WriteAllBytes(worldPath, bytes);

			bytes = normalTex2D.EncodeToPNG();
			normalPath = Path.Combine(directoryPath, "bakedNormal.png");
			File.WriteAllBytes(normalPath, bytes);

			bytes = worldTex2D.EncodeToEXR();
			worldPath = Path.Combine(directoryPath, "bakedWorld.png");
			File.WriteAllBytes(worldPath, bytes);
		}

		public RenderTargetIdentifier[] GetMRT()
		{
			RenderTargetIdentifier[] array = new RenderTargetIdentifier[2];
			array[0] = worldRT;
			array[1] = normalRT;

			return array;
		}
	}
}
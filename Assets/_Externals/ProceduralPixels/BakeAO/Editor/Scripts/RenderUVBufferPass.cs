/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using UnityEngine;
using UnityEngine.Rendering;

namespace ProceduralPixels.BakeAO.Editor
{
    internal struct TileData
	{
		public Vector4 scaleAndTransform;
		public Vector2Int coord;
		public float tilesCount;

		public TileData(Vector2Int tileCoord, float tilesCount) : this()
		{
			coord = tileCoord;
			this.tilesCount = tilesCount;

			Vector2 tileScale = new Vector2(1.0f / tilesCount, 1.0f / tilesCount);
			Vector2 tileOffset = tileCoord * tileScale;
			scaleAndTransform = new Vector4(tileScale.x, tileScale.y, tileOffset.x, tileOffset.y);
		}
	}

    internal class RenderUVBufferPass : BakePass
	{
		private BakingData bakingData;
		private BakingSetup bakingSetup;
		private TileData tileData;

		public RenderUVBufferPass() : base()
		{
		}

		public void Setup(BakingData bakingData, BakingSetup bakingSetup, TileData tileData)
		{
			this.bakingData = bakingData;
			this.bakingSetup = bakingSetup;
			this.tileData = tileData;
		}

		public override void Execute()
		{
			CommandBuffer cmd = GlobalPool<CommandBuffer>.Get();
			cmd.Clear();
			cmd.SetGlobalVector(ShaderUniforms._ScaleAndTransform, tileData.scaleAndTransform);

			cmd.Blit(bakingData.normalRT, bakingData.normalRT);
			cmd.SetRenderTarget(bakingData.GetMRT(), bakingData.depthRT);

			cmd.ClearRenderTarget(true, true, new Color(0.0f, 0.0f, 0.0f, 0.0f));

			cmd.DisableShaderKeyword(GetUVKeyword(UVChannel.UV0));
			cmd.DisableShaderKeyword(GetUVKeyword(UVChannel.UV1));
			cmd.DisableShaderKeyword(GetUVKeyword(UVChannel.UV2));
			cmd.DisableShaderKeyword(GetUVKeyword(UVChannel.UV3));

			for (int i = 0; i < bakingSetup.MeshesToBake.Count; i++)
			{
				var meshToBake = bakingSetup.MeshesToBake[i];
				cmd.SetGlobalMatrix(ShaderUniforms._AOBake_MatrixM, meshToBake.objectToWorld);
				cmd.SetGlobalMatrix(ShaderUniforms._AOBake_MatrixMInv, meshToBake.objectToWorld.inverse);
				var keyword = GetUVKeyword(meshToBake.uv);
				cmd.EnableShaderKeyword(keyword);
				for (int submeshIndex = 0; submeshIndex < meshToBake.mesh.subMeshCount; submeshIndex++)
					cmd.DrawMesh(meshToBake.mesh, Matrix4x4.identity, BakeAOResources.Instance.RenderUVBufferMaterial, submeshIndex);
				cmd.DisableShaderKeyword(keyword);
			}

			cmd.SetGlobalFloat(ShaderUniforms._TextureSize, (float)bakingData.textureSize);

			cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
			Graphics.ExecuteCommandBuffer(cmd);

			cmd.Clear();
			GlobalPool<CommandBuffer>.Return(cmd);
		}

		private string GetUVKeyword(UVChannel uvChannel)
		{
			switch (uvChannel)
			{
				case UVChannel.UV0:
					return ShaderKeywords.UV_CHANNEL_0;
				case UVChannel.UV1:
					return ShaderKeywords.UV_CHANNEL_1;
				case UVChannel.UV2:
					return ShaderKeywords.UV_CHANNEL_2;
				case UVChannel.UV3:
					return ShaderKeywords.UV_CHANNEL_3;
				default:
					return null;
			}
		}
	}
}
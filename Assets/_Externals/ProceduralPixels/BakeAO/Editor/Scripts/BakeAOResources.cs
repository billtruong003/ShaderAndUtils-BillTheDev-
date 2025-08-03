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
    internal class BakeAOResources : ResourcesScriptableObject<BakeAOResources>
	{
		[field: SerializeField, SubassetResource] public Material AOBakeMaterial { get; private set; }
		[field: SerializeField, SubassetResource] public Material AOWritePixelMaterial { get; private set; }
		[field: SerializeField, SubassetResource] public Material DenoiseMaterial { get; private set; }
		[field: SerializeField, SubassetResource] public Material DilateMaterial { get; private set; }
		[field: SerializeField, SubassetResource] public Material ShiftShadowsMaterial { get; private set; }
		[field: SerializeField, SubassetResource] public Material PostprocessMaterial { get; private set; }
		[field: SerializeField, SubassetResource] public Material RenderUVBufferMaterial { get; private set; }
		[field: SerializeField, SubassetResource] public Material DebugDrawMaterial { get; private set; }
		[field: SerializeField, SubassetResource] public Material DebugLineMaterial { get; private set; }
		[field: SerializeField, SubassetResource] public Mesh QuadMesh { get; private set; }

		[ContextMenu("Rebuild")]
		private void Rebuild()
		{
			RecreateAllAssets();
		}

		protected override void RecreateAssets()
		{
			CreateSubassetIfNotExist(() => AOBakeMaterial, m => AOBakeMaterial = m, () => new Material(Shader.Find("Hidden/BakeAO/Bake")));
			CreateSubassetIfNotExist(() => AOWritePixelMaterial, m => AOWritePixelMaterial = m, () => new Material(Shader.Find("Hidden/BakeAO/WritePixel")));
			CreateSubassetIfNotExist(() => DenoiseMaterial, m => DenoiseMaterial = m, () => new Material(Shader.Find("Hidden/BakeAO/Denoise")));
			CreateSubassetIfNotExist(() => DilateMaterial, m => DilateMaterial = m, () => new Material(Shader.Find("Hidden/BakeAO/Dilate")));
			CreateSubassetIfNotExist(() => ShiftShadowsMaterial, m => ShiftShadowsMaterial = m, () => new Material(Shader.Find("Hidden/BakeAO/ShiftShadows")));
			CreateSubassetIfNotExist(() => PostprocessMaterial, m => PostprocessMaterial = m, () => new Material(Shader.Find("Hidden/BakeAO/Postprocess")));
			CreateSubassetIfNotExist(() => RenderUVBufferMaterial, m => RenderUVBufferMaterial = m, () => new Material(Shader.Find("Hidden/BakeAO/RenderUVBuffer")));
			CreateSubassetIfNotExist(() => DebugDrawMaterial, m => DebugDrawMaterial = m, () => new Material(Shader.Find("Hidden/BakeAO/DebugDraw")));
			CreateSubassetIfNotExist(() => DebugLineMaterial, m => DebugLineMaterial = m, () => new Material(Shader.Find("Hidden/BakeAO/DebugLine")));
			CreateSubassetIfNotExist(() => QuadMesh, m => QuadMesh = m, CreateQuadMesh);
		}

		private static Mesh CreateQuadMesh()
		{
			Vector3[] positions = {
				new Vector3(-1.0f, -1.0f, 0.0f),
				new Vector3(-1.0f, -1.0f, 0.0f),
				new Vector3(1.0f, -1.0f, 0.0f),
				new Vector3(1.0f, 1.0f, 0.0f),
			};

			int[] indices = { 0, 1, 2, 3, 2, 1 };

			Mesh mesh = new Mesh();
			mesh.indexFormat = IndexFormat.UInt16;
			mesh.vertices = positions;
			mesh.triangles = indices;
			mesh.name = "ProceduralPixels-BakeAO-QuadMesh";

			return mesh;
		}

	}
}
using System;
using System.Collections;
using UnityEngine;

namespace AllIn13DShader
{
	[ExecuteInEditMode]
	public class MaterialPrewarmer : MonoBehaviour
	{
		[SerializeField] private Material[] materialsToPrewarm;
		[SerializeField] private GameObject[] objectsToPrewarm;
		[SerializeField] private Transform spawnPosT;
		[SerializeField] private GameObject overlayUI;

		[SerializeField] private ShaderVariantCollection shaderVariantCollection;


		private void Awake()
		{
#if UNITY_EDITOR
			PrewarmEditor();
#endif
		}

		private void Start()
		{
#if !UNITY_EDITOR
			StartRuntime();
#endif
		}

		private IEnumerator PrewarmMaterials()
		{
			overlayUI.SetActive(true);
    
			// Graphics.DrawMesh uses Matrix4x4 instead of spawn position
			Matrix4x4 matrix = Matrix4x4.TRS(spawnPosT.position, Quaternion.identity, Vector3.one * 0.01f);
			Mesh quadMesh = CreateQuadMesh();
			for(int i = 0; i < materialsToPrewarm.Length; i++) Graphics.DrawMesh(quadMesh, matrix, materialsToPrewarm[i], 0);
			DestroyImmediate(quadMesh);
    
			// Only use spawn position for complex prefabs that still need instantiation
			for(int i = 0; i < objectsToPrewarm.Length; i++)
			{
				GameObject temp = Instantiate(objectsToPrewarm[i], spawnPosT.position, Quaternion.identity);
				temp.transform.localScale = Vector3.one * 0.01f;
				DestroyImmediate(temp);
			}
    
			yield return null;
			overlayUI.SetActive(false);
			Destroy(gameObject);
		}

		private void StartRuntime()
		{
			if(!Application.isPlaying) return;
			StartCoroutine(PrewarmMaterials());
		}
		
		private Mesh CreateQuadMesh()
		{
			Mesh mesh = new Mesh
			{
				vertices = new Vector3[] {
					new Vector3(-0.5f, -0.5f, 0),
					new Vector3(0.5f, -0.5f, 0),
					new Vector3(0.5f, 0.5f, 0),
					new Vector3(-0.5f, 0.5f, 0)
				},
				triangles = new int[] { 0, 1, 2, 0, 2, 3 },
				uv = new Vector2[] {
					new Vector2(0, 0), new Vector2(1, 0), 
					new Vector2(1, 1), new Vector2(0, 1)
				}
			};
			return mesh;
		}

#if UNITY_EDITOR
		private void PrewarmEditor()
		{
			ShaderVariantCollection shaderVariantCollection = new ShaderVariantCollection();
			for (int i = 0; i < materialsToPrewarm.Length; i++)
			{
				Material mat = materialsToPrewarm[i];

				string[] enabledKeywords = new string[mat.enabledKeywords.Length];
				for(int kwIndex = 0; kwIndex < enabledKeywords.Length; kwIndex++)
				{
					enabledKeywords[kwIndex] = mat.enabledKeywords[kwIndex].name;
				}

				foreach (UnityEngine.Rendering.PassType passType in Enum.GetValues(typeof(UnityEngine.Rendering.PassType)))
				{
					try
					{
						shaderVariantCollection.Add(new ShaderVariantCollection.ShaderVariant(mat.shader, passType, enabledKeywords));
					}
					catch (Exception e)
					{
#if ALLIN13DSHADER_DEVELOP && ALLIN13DSHADER_LOG
						Debug.LogWarning($"Failed to add shader variant for material: {mat.name} || Error Message: {e.Message}");
#endif
					}
				}
			}

			this.shaderVariantCollection = shaderVariantCollection;

			shaderVariantCollection.WarmUp();
		}
#endif
	}
}
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Sirenix.OdinInspector;

namespace BillTheDev.QuickOutline
{

    [DisallowMultipleComponent]
    public class Outline : SerializedMonoBehaviour
    {

        #region Enums and Classes
        public enum Mode
        {
            OutlineAll,
            OutlineVisible,
            OutlineHidden,
            OutlineAndSilhouette,
            SilhouetteOnly
        }

        [Serializable]
        private class ListVector3
        {
            public List<Vector3> data;
        }
        #endregion

        #region Shader Property IDs
        private static readonly int ZTestProperty = Shader.PropertyToID("_ZTest");
        private static readonly int OutlineColorProperty = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthProperty = Shader.PropertyToID("_OutlineWidth");
        #endregion

        #region Dependencies
        // THAY ĐỔI: Phụ thuộc vào Shader thay vì Material Template
        [TitleGroup("Dependencies")]
        [Required("Outline Mask Shader is missing."), AssetsOnly]
        [SerializeField, Tooltip("Assign the Outline Mask shader asset here.")]
        private Shader outlineMaskShader;

        [TitleGroup("Dependencies")]
        [Required("Outline Fill Shader is missing."), AssetsOnly]
        [SerializeField, Tooltip("Assign the Outline Fill shader asset here.")]
        private Shader outlineFillShader;
        #endregion

        #region Settings
        [TabGroup("Settings", "General")]
        [SerializeField]
        private Mode outlineMode = Mode.OutlineAll;

        [TabGroup("Settings", "General")]
        [SerializeField]
        private Color outlineColor = Color.white;

        [TabGroup("Settings", "General")]
        [SerializeField, Range(0f, 10f), SuffixLabel("px", true)]
        private float outlineWidth = 2f;

        [TabGroup("Settings", "Advanced")]
        [OnValueChanged("OnPrecomputeToggled")]
        [SerializeField, Tooltip("Precompute enabled: Per-vertex calculations are performed in the editor and serialized with the object. "
        + "Precompute disabled: Per-vertex calculations are performed at runtime in Awake(). This may cause a pause for large meshes.")]
        private bool precomputeOutline;
        #endregion

        #region Baking
        [TitleGroup("Baking", "Manage precomputed smooth normals.")]
        [ShowIf("precomputeOutline")]
        [Button(ButtonSizes.Large), PropertyOrder(1)]
        public void Bake()
        {
            var bakedMeshes = new HashSet<Mesh>();
            bakeKeys.Clear();
            bakeValues.Clear();

            foreach (var meshFilter in GetComponentsInChildren<MeshFilter>(true))
            {
                if (meshFilter.sharedMesh == null || !bakedMeshes.Add(meshFilter.sharedMesh))
                {
                    continue;
                }

                var smoothNormals = SmoothNormals(meshFilter.sharedMesh);
                bakeKeys.Add(meshFilter.sharedMesh);
                bakeValues.Add(new ListVector3 { data = smoothNormals });
            }
            Debug.Log($"[QuickOutline] Baked smooth normals for {bakeKeys.Count} unique meshes.", this);
        }

        [TitleGroup("Baking")]
        [ShowIf("precomputeOutline")]
        [Button, PropertyOrder(2)]
        public void ClearBakeData()
        {
            bakeKeys.Clear();
            bakeValues.Clear();
            Debug.Log("[QuickOutline] Cleared baked smooth normals data.", this);
        }

        [TitleGroup("Baking")]
        [ShowIf("precomputeOutline")]
        [ReadOnly, ListDrawerSettings(IsReadOnly = true, Expanded = false)]
        [SerializeField, HideInInspector]
        private List<Mesh> bakeKeys = new List<Mesh>();

        [TitleGroup("Baking")]
        [ShowIf("precomputeOutline")]
        [ReadOnly, ListDrawerSettings(IsReadOnly = true, Expanded = false)]
        [SerializeField, HideInInspector]
        private List<ListVector3> bakeValues = new List<ListVector3>();

        #endregion

        #region Private Fields
        private Renderer[] renderers;
        private Material outlineMaskMaterial;
        private Material outlineFillMaterial;
        private bool isInitialized = false;
        private HashSet<Mesh> processedMeshes;
        private List<Material> sharedMaterialsBuffer = new List<Material>();
        #endregion

        #region Properties
        public Mode OutlineMode { get => outlineMode; set { outlineMode = value; UpdateMaterialProperties(); } }
        public Color OutlineColor { get => outlineColor; set { outlineColor = value; UpdateMaterialProperties(); } }
        public float OutlineWidth { get => outlineWidth; set { outlineWidth = value; UpdateMaterialProperties(); } }
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (outlineMaskShader != null && outlineFillShader != null)
            {
                Initialize();
            }
        }
        private void OnEnable()
        {
            if (!isInitialized) Initialize();
            if (!isInitialized) return;

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.GetSharedMaterials(sharedMaterialsBuffer);
                sharedMaterialsBuffer.Add(outlineMaskMaterial);
                sharedMaterialsBuffer.Add(outlineFillMaterial);
                renderer.materials = sharedMaterialsBuffer.ToArray();
            }
        }

        private void OnDisable()
        {
            if (!isInitialized) return;

            foreach (var renderer in renderers)
            {
                if (renderer == null) continue;
                renderer.GetSharedMaterials(sharedMaterialsBuffer);
                sharedMaterialsBuffer.Remove(outlineMaskMaterial);
                sharedMaterialsBuffer.Remove(outlineFillMaterial);
                renderer.materials = sharedMaterialsBuffer.ToArray();
            }
        }

        private void OnDestroy()
        {
            if (outlineMaskMaterial != null) Destroy(outlineMaskMaterial);
            if (outlineFillMaterial != null) Destroy(outlineFillMaterial);
        }

        private void OnValidate()
        {
            if (isInitialized)
            {
                UpdateMaterialProperties();
            }
        }
        #endregion

        #region Configuration & Initialization
        public void Configure(Shader maskShader, Shader fillShader, OutlineConfiguration configuration)
        {
            // Gán các phụ thuộc
            this.outlineMaskShader = maskShader;
            this.outlineFillShader = fillShader;

            // Áp dụng cấu hình từ ScriptableObject
            this.outlineMode = configuration.outlineMode;
            this.outlineColor = configuration.outlineColor;
            this.outlineWidth = configuration.outlineWidth;

            // Bắt đầu khởi tạo
            Initialize();
        }

        #endregion

        #region Initialization
        private void Initialize()
        {
            if (isInitialized) return;

            // 1. Kiểm tra các asset SHADER đã được gán hay chưa.
            if (outlineMaskShader == null || outlineFillShader == null)
            {
                Debug.LogError("Outline shaders are not assigned in the Inspector. Disabling component.", this);
                enabled = false;
                return;
            }

            // 2. TẠO MỚI các instance material trực tiếp từ shader.
            outlineMaskMaterial = new Material(outlineMaskShader);
            outlineFillMaterial = new Material(outlineFillShader);

            // 3. Phần còn lại của hàm giữ nguyên.
            renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                Debug.LogWarning("No Renderers found in children. Outline will not be visible.", this);
            }

            processedMeshes = new HashSet<Mesh>();

            outlineMaskMaterial.name = "OutlineMask (Instance)";
            outlineFillMaterial.name = "OutlineFill (Instance)";

            LoadSmoothNormals();
            UpdateMaterialProperties();

            isInitialized = true;
        }
        // =========================================================================================
        // === KẾT THÚC PHẦN SỬA ĐỔI ================================================================
        // =========================================================================================

        [InfoBox("No renderers found in children. The outline effect will not be visible.", InfoMessageType.Warning, "HasNoRenderers")]
        private bool HasNoRenderers()
        {
            if (renderers == null)
            {
                renderers = GetComponentsInChildren<Renderer>(true);
            }
            return renderers.Length == 0;
        }
        #endregion

        #region Core Logic
        private void LoadSmoothNormals()
        {
            foreach (var meshFilter in GetComponentsInChildren<MeshFilter>(true))
            {
                ProcessMesh(meshFilter.sharedMesh, meshFilter.GetComponent<Renderer>());
            }

            foreach (var skinnedMeshRenderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                ProcessMesh(skinnedMeshRenderer.sharedMesh, skinnedMeshRenderer, true);
            }
        }

        private void ProcessMesh(Mesh mesh, Renderer renderer, bool isSkinned = false)
        {
            if (mesh == null || !processedMeshes.Add(mesh))
            {
                return;
            }

            var index = bakeKeys.IndexOf(mesh);
            var smoothNormals = (index >= 0) ? bakeKeys[index] != null ? bakeValues[index].data : SmoothNormals(mesh) : SmoothNormals(mesh);

            mesh.SetUVs(3, smoothNormals);

            if (isSkinned)
            {
                mesh.uv4 = new Vector2[mesh.vertexCount];
            }

            if (renderer != null)
            {
                CombineSubmeshes(mesh, renderer.sharedMaterials);
            }
        }

        private List<Vector3> SmoothNormals(Mesh mesh)
        {
            var groups = new Dictionary<Vector3, List<int>>();
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                if (!groups.ContainsKey(mesh.vertices[i]))
                {
                    groups[mesh.vertices[i]] = new List<int>();
                }
                groups[mesh.vertices[i]].Add(i);
            }

            var smoothNormals = new List<Vector3>(mesh.normals);
            foreach (var group in groups.Values)
            {
                if (group.Count == 1) continue;

                var smoothNormal = Vector3.zero;
                foreach (var index in group)
                {
                    smoothNormal += mesh.normals[index];
                }
                smoothNormal.Normalize();

                foreach (var index in group)
                {
                    smoothNormals[index] = smoothNormal;
                }
            }
            return smoothNormals;
        }

        private void CombineSubmeshes(Mesh mesh, Material[] materials)
        {
            if (mesh.subMeshCount == 1 || mesh.subMeshCount > materials.Length)
            {
                return;
            }
            mesh.subMeshCount++;
            mesh.SetTriangles(mesh.triangles, mesh.subMeshCount - 1);
        }

        private void UpdateMaterialProperties()
        {
            if (outlineMaskMaterial == null || outlineFillMaterial == null) return;

            outlineFillMaterial.SetColor(OutlineColorProperty, outlineColor);

            switch (outlineMode)
            {
                case Mode.OutlineAll:
                    outlineMaskMaterial.SetFloat(ZTestProperty, (float)CompareFunction.Always);
                    outlineFillMaterial.SetFloat(ZTestProperty, (float)CompareFunction.Always);
                    outlineFillMaterial.SetFloat(OutlineWidthProperty, outlineWidth);
                    break;
                case Mode.OutlineVisible:
                    outlineMaskMaterial.SetFloat(ZTestProperty, (float)CompareFunction.Always);
                    outlineFillMaterial.SetFloat(ZTestProperty, (float)CompareFunction.LessEqual);
                    outlineFillMaterial.SetFloat(OutlineWidthProperty, outlineWidth);
                    break;
                case Mode.OutlineHidden:
                    outlineMaskMaterial.SetFloat(ZTestProperty, (float)CompareFunction.Always);
                    outlineFillMaterial.SetFloat(ZTestProperty, (float)CompareFunction.Greater);
                    outlineFillMaterial.SetFloat(OutlineWidthProperty, outlineWidth);
                    break;
                case Mode.OutlineAndSilhouette:
                    outlineMaskMaterial.SetFloat(ZTestProperty, (float)CompareFunction.LessEqual);
                    outlineFillMaterial.SetFloat(ZTestProperty, (float)CompareFunction.Always);
                    outlineFillMaterial.SetFloat(OutlineWidthProperty, outlineWidth);
                    break;
                case Mode.SilhouetteOnly:
                    outlineMaskMaterial.SetFloat(ZTestProperty, (float)CompareFunction.LessEqual);
                    outlineFillMaterial.SetFloat(ZTestProperty, (float)CompareFunction.Greater);
                    outlineFillMaterial.SetFloat(OutlineWidthProperty, 0f);
                    break;
            }
        }

        private void OnPrecomputeToggled()
        {
            if (!precomputeOutline)
            {
                ClearBakeData();
            }
        }
        #endregion
    }
}
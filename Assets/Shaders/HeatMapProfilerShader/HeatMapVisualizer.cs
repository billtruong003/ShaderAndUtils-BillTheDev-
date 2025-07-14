using UnityEngine;
using System.Linq;

namespace PerfHeatMap
{
    [AddComponentMenu("")] // Hide from Add Component menu
    public class PerfHeatMapVisualizer : MonoBehaviour
    {
        private const string ShaderName = "PerfHeatMap/Volume";

        public PerfHeatMapData CurrentData { get; private set; }

        private Material _material;
        private Texture3D _volumeTexture;
        private MeshRenderer _renderer;
        private MeshFilter _meshFilter;

        public Vector4 MinValues { get; private set; }
        public Vector4 MaxValues { get; private set; }

        public void Initialize()
        {
            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                UnityEngine.Debug.LogError($"Shader '{ShaderName}' not found. Please ensure it is in a Resources folder or included in build.");
                return;
            }
            _material = new Material(shader);

            gameObject.hideFlags = HideFlags.HideAndDontSave;
            _renderer = gameObject.AddComponent<MeshRenderer>();
            _meshFilter = gameObject.AddComponent<MeshFilter>();
            _meshFilter.mesh = CreateInvertedCubeMesh();
            _renderer.material = _material;
        }

        public void Display(PerfHeatMapData data)
        {
            CurrentData = data;
            if (data == null || data.Samples.Count == 0)
            {
                Clear();
                return;
            }

            transform.position = data.AnalysisBounds.center;
            transform.localScale = data.AnalysisBounds.size;

            CalculateMinMaxValues();
            BakeToVolumeTexture();
            UpdateShaderProperties();

            gameObject.SetActive(true);
        }

        public void Clear()
        {
            if (gameObject != null)
            {
                gameObject.SetActive(false);
            }
        }

        public void UpdateShaderProperties()
        {
            if (_material == null) return;
            // This method would be called by the editor window when sliders change
        }

        private void CalculateMinMaxValues()
        {
            if (CurrentData.Samples.Count == 0)
            {
                MinValues = MaxValues = Vector4.zero;
                return;
            }

            var min = new Vector4(float.MaxValue, float.MaxValue, float.MaxValue, float.MaxValue);
            var max = new Vector4(float.MinValue, float.MinValue, float.MinValue, float.MinValue);

            foreach (var sample in CurrentData.Samples)
            {
                min.x = Mathf.Min(min.x, sample.Stat1_DrawCalls);
                max.x = Mathf.Max(max.x, sample.Stat1_DrawCalls);
                min.y = Mathf.Min(min.y, sample.Stat2_Triangles);
                max.y = Mathf.Max(max.y, sample.Stat2_Triangles);
                min.z = Mathf.Min(min.z, sample.Stat3_GpuTimeMS);
                max.z = Mathf.Max(max.z, sample.Stat3_GpuTimeMS);
                min.w = Mathf.Min(min.w, sample.Stat4_FrameTimeMS);
                max.w = Mathf.Max(max.w, sample.Stat4_FrameTimeMS);
            }
            MinValues = min;
            MaxValues = max;
        }

        private void BakeToVolumeTexture()
        {
            Vector3Int dim = CurrentData.GetGridDimensions();
            if (_volumeTexture == null || _volumeTexture.width != dim.x || _volumeTexture.height != dim.y || _volumeTexture.depth != dim.z)
            {
                if (_volumeTexture != null) Object.DestroyImmediate(_volumeTexture);
                _volumeTexture = new Texture3D(dim.x, dim.y, dim.z, TextureFormat.RGBAFloat, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear
                };
            }

            var colors = new Color[dim.x * dim.y * dim.z];

            foreach (var sample in CurrentData.Samples)
            {
                Vector3 localPos = sample.Position - CurrentData.AnalysisBounds.min;
                int ix = Mathf.Clamp(Mathf.FloorToInt(localPos.x / CurrentData.CellSize.x), 0, dim.x - 1);
                int iy = Mathf.Clamp(Mathf.FloorToInt(localPos.y / CurrentData.CellSize.y), 0, dim.y - 1);
                int iz = Mathf.Clamp(Mathf.FloorToInt(localPos.z / CurrentData.CellSize.z), 0, dim.z - 1);
                int index = ix + iy * dim.x + iz * dim.x * dim.y;

                colors[index] = new Color(sample.Stat1_DrawCalls, sample.Stat2_Triangles, sample.Stat3_GpuTimeMS, sample.Stat4_FrameTimeMS);
            }

            _volumeTexture.SetPixels(colors);
            _volumeTexture.Apply();
            _material.SetTexture("_VolumeTex", _volumeTexture);
        }

        private Mesh CreateInvertedCubeMesh()
        {
            var mesh = new Mesh();
            float s = 0.5f;
            var vertices = new Vector3[]
            {
                new Vector3(-s, -s, -s), new Vector3(s, -s, -s), new Vector3(s, s, -s), new Vector3(-s, s, -s),
                new Vector3(-s, s, s), new Vector3(s, s, s), new Vector3(s, -s, s), new Vector3(-s, -s, s),
            };
            var triangles = new int[]
            {
                0, 2, 1, 0, 3, 2, 2, 3, 4, 2, 4, 5, 1, 2, 5, 1, 5, 6, 0, 7, 4, 0, 4, 3, 5, 4, 7, 5, 7, 6, 0, 6, 7, 0, 1, 6
            }.Reverse().ToArray();

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            return mesh;
        }

        private void OnDestroy()
        {
            if (_material != null) DestroyImmediate(_material);
            if (_volumeTexture != null) DestroyImmediate(_volumeTexture);
        }
    }
}
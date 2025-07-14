using UnityEngine;

namespace BillTheDev.ProfilerDirector
{
    internal readonly struct RendererProfile
    {
        public readonly Renderer SourceRenderer;
        public readonly string ObjectName;
        public readonly string MeshName;
        public readonly float Score;
        public readonly int PassCount;
        public readonly int VertexCount;
        public readonly int TriangleCount;
        public readonly int SubMeshCount;
        public readonly int MaterialCount;
        public readonly string ShaderName;
        public readonly bool IsTransparent;
        public readonly Color PerformanceColor;
        public readonly float ScreenSpacePercentage;

        public RendererProfile(Renderer renderer, ProfilerDirectorSettings settings, Camera camera, float gpuTimeMs)
        {
            SourceRenderer = renderer;
            ObjectName = renderer.gameObject.name;

            int vertexCount = 0;
            int triangleCount = 0;
            int subMeshCount = 0;
            string meshName = "N/A";

            if (renderer is MeshRenderer mr && mr.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
            {
                vertexCount = mf.sharedMesh.vertexCount;
                triangleCount = mf.sharedMesh.triangles.Length / 3;
                subMeshCount = mf.sharedMesh.subMeshCount;
                meshName = mf.sharedMesh.name;
            }
            else if (renderer is SkinnedMeshRenderer smr && smr.sharedMesh != null)
            {
                vertexCount = smr.sharedMesh.vertexCount;
                triangleCount = smr.sharedMesh.triangles.Length / 3;
                subMeshCount = smr.sharedMesh.subMeshCount;
                meshName = smr.sharedMesh.name;
            }

            VertexCount = vertexCount;
            TriangleCount = triangleCount;
            SubMeshCount = subMeshCount;
            MeshName = meshName;

            GetPassInfo(renderer, out int passCount, out bool isTransparent, out string shaderName, out int materialCount);
            PassCount = passCount;
            IsTransparent = isTransparent;
            ShaderName = shaderName;
            MaterialCount = materialCount;

            ScreenSpacePercentage = CalculateScreenSpacePercentage(renderer.bounds, camera);

            Score = CalculateScore(settings, gpuTimeMs, PassCount, VertexCount, IsTransparent, ScreenSpacePercentage);
            PerformanceColor = GetPerformanceColor(Score, settings);
        }

        private static float CalculateScore(ProfilerDirectorSettings settings, float gpuTimeMs, int passCount, int vertexCount, bool isTransparent, float screenSpacePercentage)
        {
            // Tạm thời chưa đưa GPU time vào điểm số vì nó dao động nhiều
            // Sẽ là một cải tiến tốt trong tương lai khi có hệ thống trọng số ổn định hơn
            return (passCount * settings.PassCountWeight) +
                   ((vertexCount / 1000f) * settings.VertexCountWeight) +
                   (isTransparent ? settings.TransparencyPenalty : 0) +
                   (screenSpacePercentage * settings.ScreenSizeWeight);
        }

        private static void GetPassInfo(Renderer renderer, out int passCount, out bool isTransparent, out string shaderName, out int materialCount)
        {
            passCount = 0;
            isTransparent = false;
            shaderName = "N/A";
            materialCount = renderer.sharedMaterials.Length;

            if (materialCount == 0 || renderer.sharedMaterial == null) return;

            shaderName = renderer.sharedMaterial.shader?.name ?? "Missing Shader";

            foreach (var mat in renderer.sharedMaterials)
            {
                if (mat == null) continue;
                passCount += mat.passCount;
                if (mat.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent) isTransparent = true;
            }
        }

        private static Color GetPerformanceColor(float score, ProfilerDirectorSettings settings)
        {
            if (score < settings.GoodScoreThreshold) return settings.GoodColor;
            if (score < settings.WarningScoreThreshold) return settings.WarningColor;
            return settings.PoorColor;
        }

        private static float CalculateScreenSpacePercentage(Bounds bounds, Camera camera)
        {
            if (camera == null) return 0f;

            Vector3[] corners = new Vector3[8];
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            corners[0] = new Vector3(min.x, min.y, min.z);
            corners[1] = new Vector3(max.x, min.y, min.z);
            corners[2] = new Vector3(min.x, max.y, min.z);
            corners[3] = new Vector3(max.x, max.y, min.z);
            corners[4] = new Vector3(min.x, min.y, max.z);
            corners[5] = new Vector3(max.x, min.y, max.z);
            corners[6] = new Vector3(min.x, max.y, max.z);
            corners[7] = new Vector3(max.x, max.y, max.z);

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            for (int i = 0; i < 8; i++)
            {
                Vector3 screenPoint = camera.WorldToScreenPoint(corners[i]);
                if (screenPoint.z > 0)
                {
                    minX = Mathf.Min(minX, screenPoint.x);
                    minY = Mathf.Min(minY, screenPoint.y);
                    maxX = Mathf.Max(maxX, screenPoint.x);
                    maxY = Mathf.Max(maxY, screenPoint.y);
                }
            }

            if (minX > camera.pixelWidth || maxX < 0 || minY > camera.pixelHeight || maxY < 0) return 0f;

            float width = Mathf.Max(0, Mathf.Min(maxX, camera.pixelWidth) - Mathf.Max(minX, 0));
            float height = Mathf.Max(0, Mathf.Min(maxY, camera.pixelHeight) - Mathf.Max(minY, 0));

            return (width * height) / (camera.pixelWidth * camera.pixelHeight) * 100f;
        }
    }

    internal readonly struct GlobalMetrics
    {
        public readonly float Fps;
        public readonly float CpuRenderThreadMs;
        public readonly long DrawCalls;
        public readonly long SetPassCalls;

        public GlobalMetrics(float deltaTime, float cpuRenderThreadMs, long drawCalls, long setPassCalls)
        {
            Fps = deltaTime > 0 ? 1.0f / deltaTime : 0;
            CpuRenderThreadMs = cpuRenderThreadMs;
            DrawCalls = drawCalls;
            SetPassCalls = setPassCalls;
        }
    }
}
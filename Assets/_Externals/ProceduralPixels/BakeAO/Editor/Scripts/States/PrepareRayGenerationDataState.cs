/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    internal class PrepareRayGenerationDataState : BakeAOState
	{
		public override float Progress => 1.0f - (tilesToRender.Count / (float)(MsaaSamplesCount * MsaaSamplesCount));
		public override string StateDescription => bakingData.RayGenerationData.Count == 0 ? "Preparing" : $"{bakingData.RayGenerationData.Count} samples prepared";

		private int MsaaSamplesCount => (int)bakingSetup.quality.MsaaSamples;

		private const float NORMAL_ERROR_MULTIPLIER = 0.000001f;

		private int frameCount = 0;
		private BakingData bakingData;

		private RenderUVBufferPass renderUVBuffer;
		private BakingSetup bakingSetup;

        private Queue<TileData> tilesToRender = new();

		public void Setup(BakingData bakingData, BakingSetup bakingSetup)
		{
			this.bakingData = bakingData;
			this.bakingSetup = bakingSetup;
		}

		public override void Start()
		{
			base.Start();

			bakingData.Initialize();
			bakingData.RayGenerationData.Clear();
			tilesToRender.Clear();
			calculatingThread = null;

			renderUVBuffer = new RenderUVBufferPass();

			for (int y = 0; y < MsaaSamplesCount; y++)
			{
				for (int x = 0; x < MsaaSamplesCount; x++)
				{
					tilesToRender.Enqueue(new TileData(new Vector2Int(x, y), MsaaSamplesCount));
				}
			}
			frameCount = 0;
		}

		private RayGenerationThread calculatingThread = null;

		public override void Update()
		{
			if (tilesToRender.Count == 0 && calculatingThread == null)
			{
				TransitionToNextState();
			}
			else
			{
                if (calculatingThread == null || !calculatingThread.IsRunning)
                {
                    TileData tileData;
                    switch (frameCount)
                    {
                        case 4:
                            tileData = tilesToRender.Peek();
                            renderUVBuffer.Setup(bakingData, bakingSetup, tileData);
                            renderUVBuffer.Execute();
                            break;
                        case 8:
                            bakingData.CreateTextures2D(); // This creates lags when baking textures larger than 512x512, I will be better to: limit max baking area to 256x256 or async readback from GPU. This is to be done when improving baking algorithm.
                            tileData = tilesToRender.Dequeue();
                            int processorCount = Mathf.ClosestPowerOfTwo(SystemInfo.processorCount);
                            processorCount = Math.Min((int)bakingSetup.quality.TextureSize / (int)bakingSetup.quality.MsaaSamples, processorCount);
                            calculatingThread = new RayGenerationThread(bakingData, bakingSetup, tileData, processorCount);
                            calculatingThread.Start(() =>
                            {
                                frameCount = 0;
                                calculatingThread = null;
                            });
                            break;
                    }

                    frameCount++;
                }
            }
		}

		public class RayGenerationThread
		{
			private BakingData bakingData;
            private BakingSetup bakingSetup;

			private TileData tileData;
            private int textureSize;
            private UnsafeTexture2D<Vector4> positions;
            private UnsafeTexture2D<Vector4> normals;
            private int processorCount;
            private int MsaaSamplesCount => (int)bakingSetup.quality.MsaaSamples;

            private Action onFinish;

            public bool IsRunning => bakingSetup != null;

            public RayGenerationThread(BakingData bakingData, BakingSetup bakingSetup, TileData tileData, int processorCount)
			{
				this.bakingData = bakingData;
				this.tileData = tileData;
                this.bakingSetup = bakingSetup;
                this.processorCount = processorCount;

                textureSize = (int)bakingData.textureSize;
                positions = new UnsafeTexture2D<Vector4>(bakingData.worldTex2D);
                normals = new UnsafeTexture2D<Vector4>(bakingData.normalTex2D);
            }

            void ExecuteRayGeneration()
            {
                try
                {
                    PrepareRayGenerationData(tileData);
                    positions = default;
                    normals = default;
                    bakingData = null;
                    bakingSetup = null;

                    onFinish?.Invoke();
                }
                catch (Exception e)
                {
                    Debug.Log($"Oops, something went wrong when processing the ray generation data in Bake AO. If the error appears again, report this to dev@proceduralpixels.com. More information below:\n" +
                        $"Exception: {e.Message}\n" +
                        $"Exception type: {e.GetType().FullName}" +
                        $"Stack trace:\n" +
                        $"{e.StackTrace}");
                }
            }

            public void Start(Action onFinish)
            {
                this.onFinish = onFinish;

                // There was an idea to run this on another thread, but I noticed that it caused a lot of memory fragmentation.
                // But making it inline will cause lags on each beginning of baking, so there is another issue. For now I prefer lags over out-of-memory error.
                // TODO: figure out how to make this smooth.
                ExecuteRayGeneration(); 
            }

            private void PrepareRayGenerationData(TileData tileData)  
            {
                int threadCount = processorCount / 2;
                threadCount = Mathf.Clamp(threadCount, 1, (int)MsaaSamplesCount * (int)MsaaSamplesCount);

                List<SampleData>[] sampleCollectors = new List<SampleData>[threadCount];

                for (int i = 0; i < sampleCollectors.Length; i++)
                    sampleCollectors[i] = GlobalPool<List<SampleData>>.Get(); // Using pool to avoid memory fragmentation over long baking batches

                if (threadCount > 1)
                    Parallel.For(0, threadCount, new ParallelOptions() { MaxDegreeOfParallelism = threadCount}, threadIndex =>
                    {
                        int kernelWidth = textureSize / threadCount;
                        GenerateSamples(sampleCollectors[threadIndex], bakingSetup.quality.MsaaDetailDetection, kernelWidth * threadIndex, kernelWidth * (threadIndex + 1));
                    });
                else
                {
                    int kernelWidth = textureSize;
                    GenerateSamples(sampleCollectors[0], bakingSetup.quality.MsaaDetailDetection, 0, kernelWidth);
                }

                for (int i = 0; i < sampleCollectors.Length; i++)
                {
                    bakingData.RayGenerationData.AddRange(sampleCollectors[i]);

                    sampleCollectors[i].Clear();
                    GlobalPool<List<SampleData>>.Return(sampleCollectors[i]);
                    sampleCollectors[i] = null;
                }

                void GenerateSamples(List<SampleData> sampleCollector, MsaaDetailDetection msaaDetailDetection, int yMinInclusive, int yMaxExclusive)
                {
                    int msaaSamplesCount = MsaaSamplesCount;
                    for (int y = yMinInclusive; y < yMaxExclusive; y += msaaSamplesCount)
                    {
                        for (int x = 0; x < textureSize; x += msaaSamplesCount)
                        {
                            if (msaaSamplesCount >= 2)
                            {
                                //Gathering center
                                SampleData centerSample;

                                centerSample.uv = (new Vector2(x, y) / (float)msaaSamplesCount + (Vector2.one * 0.5f)) / (float)bakingData.textureSize;
                                centerSample.uv = centerSample.uv + ((Vector2.one / (float)msaaSamplesCount) * tileData.coord);

                                centerSample.normalWS = Vector3.zero;
                                centerSample.positionWS = Vector3.zero;

                                int addCount = 0;

                                bool hasSomeData = false;
                                SampleData firstData;
                                firstData.uv = centerSample.uv;
                                firstData.normalWS = Vector3.zero;
                                firstData.positionWS = Vector3.zero;
                                float normalError = 0.0f;

                                //center square
                                for (int tileY = (msaaSamplesCount - 1) / 2; tileY <= (msaaSamplesCount - 1) / 2 + 1; tileY++)
                                {
                                    for (int tileX = (msaaSamplesCount - 1) / 2; tileX <= (msaaSamplesCount - 1) / 2 + 1; tileX++)
                                    {
                                        var normal = (Vector4)normals[(y + tileY) * textureSize + (x + tileX)];

                                        if (normal.w < 0.5f)
                                            continue;

                                        var position = (Vector4)positions[(y + tileY) * textureSize + (x + tileX)];

                                        centerSample.normalWS = centerSample.normalWS + ((Vector3)normal).normalized;
                                        addCount++;

                                        if (!hasSomeData)
                                        {
                                            firstData.normalWS = ((Vector3)normal).normalized;
                                            firstData.positionWS = position;
                                            hasSomeData = true;
                                        }
                                        else
                                            normalError += Mathf.Abs(Vector3.Dot(firstData.normalWS, ((Vector3)normal).normalized) - 1.0f);
                                    }
                                }

                                bool normalErrorTooBig = normalError > NORMAL_ERROR_MULTIPLIER * msaaSamplesCount * msaaSamplesCount * (float)msaaDetailDetection;

                                if (!normalErrorTooBig)
                                {
                                    normalError = 0.0f;

                                    for (int tileY = 0; tileY < msaaSamplesCount; tileY++)
                                    {
                                        for (int tileX = 0; tileX < msaaSamplesCount; tileX++)
                                        {
                                            if (normalErrorTooBig)
                                                break;

                                            SampleData sampleData;

                                            sampleData.uv = centerSample.uv;

                                            int index = Mathf.Clamp((y + tileY) * textureSize + (x + tileX), 0, positions.length - 1);
                                            var position = (Vector3)(Vector4)positions[index];
                                            var normal = (Vector4)normals[index];

                                            if (normal.w < 0.5)
                                                continue;

                                            sampleData.positionWS = position;
                                            sampleData.normalWS = ((Vector3)normal).normalized;

                                            if (!hasSomeData)
                                            {
                                                firstData.normalWS = sampleData.normalWS;
                                                hasSomeData = true;
                                            }
                                            else
                                                normalError += Mathf.Abs(Vector3.Dot(firstData.normalWS, sampleData.normalWS) - 1.0f);

                                            normalErrorTooBig = normalError > NORMAL_ERROR_MULTIPLIER * msaaSamplesCount * msaaSamplesCount * (float)msaaDetailDetection;
                                        }
                                    }
                                }

                                // Enqueue center ray for baking
                                if (addCount > 0)
                                {
                                    centerSample.positionWS = firstData.positionWS;
                                    centerSample.normalWS /= (float)addCount;
                                    centerSample.normalWS = centerSample.normalWS.normalized;
                                    centerSample.normalWS = firstData.normalWS;

                                    sampleCollector.Add(centerSample);
                                }

                                // If error is too big, enqueue all valid samples for baking
                                if (addCount == 0 || normalErrorTooBig)
                                {
                                    for (int tileY = 0; tileY < msaaSamplesCount; tileY++)
                                    {
                                        for (int tileX = 0; tileX < msaaSamplesCount; tileX++)
                                        {
                                            SampleData sampleData;

                                            sampleData.uv = (new Vector2(x, y) / (float)msaaSamplesCount + (Vector2.one * 0.5f)) / (float)bakingData.textureSize;
                                            sampleData.uv = sampleData.uv + ((Vector2.one / (float)msaaSamplesCount) * tileData.coord);

                                            int index = Mathf.Clamp((y + tileY) * textureSize + (x + tileX), 0, positions.length - 1);
                                            var position = (Vector4)positions[index];
                                            var normal = (Vector4)normals[index];

                                            if (normal.w < 0.5)
                                                continue;

                                            sampleData.positionWS = (Vector3)position;
                                            sampleData.normalWS = (Vector3)normal.normalized;

                                            sampleCollector.Add(sampleData);
                                        }
                                    }
                                }

                            }
                            else
                            {
                                // no msaa

                                SampleData sampleData;

                                sampleData.uv = (new Vector2(x, y) + (Vector2.one * 0.5f)) / (float)bakingData.textureSize;

                                int index = Mathf.Clamp(y * textureSize + x, 0, positions.length - 1);
                                var position = (Vector4)positions[index];
                                var normal = (Vector4)normals[index];

                                if (Mathf.Approximately(normal.x, 0.0f) && Mathf.Approximately(normal.y, 0.0f) && Mathf.Approximately(normal.z, 0.0f))
                                    continue;

                                sampleData.positionWS = (Vector3)position;
                                sampleData.normalWS = (Vector3)normal;

                                sampleData.normalWS = sampleData.normalWS.normalized;

                                sampleCollector.Add(sampleData);
                            }
                        }
                    }
                }
            }
        }
	}
}

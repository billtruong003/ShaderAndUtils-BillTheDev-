#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.IO;
using System.Collections.Generic;


public class UltimateTextureOptimizerWindow : OdinEditorWindow
{
    #region Odin Properties & Enums

    public enum OptimizationAlgorithm
    {
        [InspectorName("Saliency Guided Compression")]
        SaliencyGuided,
        [InspectorName("GPU Vector Quantization")]
        VectorQuantization,
        [InspectorName("Perceptual Block Deduplication")]
        BlockDeduplication
    }

    // Sửa lỗi tương thích Odin/Unity
    protected override void OnEnable()
    {
        base.OnEnable();
        // this.DisableAutomaticHeightAdjustment();
    }

    [Title("1. Select Texture", "Drag and drop the texture you want to optimize here.")]
    [PreviewField(128, ObjectFieldAlignment.Center)]
    [Required("Please select a texture to optimize.")]
    public Texture2D SourceTexture;

    [Title("2. Choose Algorithm & Configure")]
    [EnumToggleButtons, OnValueChanged("OnAlgorithmChanged")]
    public OptimizationAlgorithm Algorithm;

    // --- Settings for Saliency Guided Compression ---
    [BoxGroup("Saliency Settings"), ShowIf("Algorithm", OptimizationAlgorithm.SaliencyGuided)]
    [Range(10, 60)] public int SG_LowQuality = 30;
    [BoxGroup("Saliency Settings"), ShowIf("Algorithm", OptimizationAlgorithm.SaliencyGuided)]
    [Range(70, 100)] public int SG_HighQuality = 95;

    // --- Settings for Vector Quantization ---
    [BoxGroup("Vector Quantization Settings"), ShowIf("Algorithm", OptimizationAlgorithm.VectorQuantization)]
    [InfoBox("Number of colors/blocks in the final palette. Lower means higher compression but lower quality.")]
    [ValueDropdown("GetCodebookSizes")] public int VQ_CodebookSize = 256;
    [BoxGroup("Vector Quantization Settings"), ShowIf("Algorithm", OptimizationAlgorithm.VectorQuantization)]
    [Required] public ComputeShader VQComputeShader;

    // --- Settings for Block Deduplication ---
    [BoxGroup("Block Deduplication Settings"), ShowIf("Algorithm", OptimizationAlgorithm.BlockDeduplication)]
    [InfoBox("How similar blocks must be to be considered duplicates (Hamming Distance). Lower means more duplicates found.")]
    [Range(0, 10)] public int BD_SimilarityThreshold = 2;
    [BoxGroup("Block Deduplication Settings"), ShowIf("Algorithm", OptimizationAlgorithm.BlockDeduplication)]
    [Required] public ComputeShader BDComputeShader;

    private int[] GetCodebookSizes = new int[] { 64, 128, 256, 512, 1024 };

    // Đảm bảo Odin vẽ lại giao diện khi đổi thuật toán
    private void OnAlgorithmChanged() => this.Repaint();

    #endregion

    #region Main Logic

    [Title("3. Execute")]
    [InfoBox("This is a destructive operation that will overwrite the source file. Please ensure you have a backup.", InfoMessageType.Warning)]
    [Button(ButtonSizes.Large, Name = "PERFORM OPTIMIZATION")]
    private void Optimize()
    {
        if (SourceTexture == null) return;

        string path = AssetDatabase.GetAssetPath(SourceTexture);
        Texture2D resultTexture = null;

        try
        {
            switch (Algorithm)
            {
                case OptimizationAlgorithm.SaliencyGuided:
                    resultTexture = ExecuteSaliencyGuidedMethod(SourceTexture);
                    break;
                case OptimizationAlgorithm.VectorQuantization:
                    resultTexture = ExecuteVectorQuantizationMethod(SourceTexture);
                    break;
                case OptimizationAlgorithm.BlockDeduplication:
                    resultTexture = ExecuteBlockDeduplicationMethod(SourceTexture);
                    break;
            }

            if (resultTexture != null)
            {
                byte[] finalBytes = resultTexture.EncodeToJPG(100);
                File.WriteAllBytes(path, finalBytes);
                DestroyImmediate(resultTexture);
                EditorUtility.DisplayDialog("Success", $"Texture has been optimized with '{Algorithm}' and overwritten.", "OK");
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }
    }

    #endregion

    #region Algorithm Implementations

    // --- IMPLEMENTATION: Saliency Guided ---
    private Texture2D ExecuteSaliencyGuidedMethod(Texture2D source)
    {
        Debug.Log("Executing Saliency Guided Compression...");
        Texture2D uncompressedCopy = CreateUncompressedCopy(source);

        EditorUtility.DisplayProgressBar("Saliency Guided Compression", "1/4: Analyzing texture saliency...", 0.25f);
        float[,] saliencyMap = SaliencyProcessor.GenerateSaliencyMap(uncompressedCopy);

        EditorUtility.DisplayProgressBar("Saliency Guided Compression", "2/4: Generating quality variants...", 0.5f);
        Texture2D highQualityTexture = DecodeJPG(uncompressedCopy.EncodeToJPG(this.SG_HighQuality));
        Texture2D lowQualityTexture = DecodeJPG(uncompressedCopy.EncodeToJPG(this.SG_LowQuality));

        EditorUtility.DisplayProgressBar("Saliency Guided Compression", "3/4: Blending textures based on saliency...", 0.75f);
        Texture2D finalTexture = BlendTextures(highQualityTexture, lowQualityTexture, saliencyMap, uncompressedCopy.width, uncompressedCopy.height);

        EditorUtility.DisplayProgressBar("Saliency Guided Compression", "4/4: Finalizing...", 1.0f);

        DestroyImmediate(uncompressedCopy);
        DestroyImmediate(highQualityTexture);
        DestroyImmediate(lowQualityTexture);

        return finalTexture;
    }

    // --- IMPLEMENTATION: Vector Quantization ---
    private Texture2D ExecuteVectorQuantizationMethod(Texture2D source)
    {
        if (VQComputeShader == null)
        {
            Debug.LogError("Vector Quantization Compute Shader is not assigned!");
            return null;
        }

        Texture2D readableSource = CreateUncompressedCopy(source);
        int kernel = VQComputeShader.FindKernel("KMeans");
        int width = readableSource.width;
        int height = readableSource.height;

        RenderTexture resultRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32) { enableRandomWrite = true };
        resultRT.Create();

        ComputeBuffer codebookBuffer = new ComputeBuffer(VQ_CodebookSize, sizeof(float) * 4);
        Color[] initialCodebook = new Color[VQ_CodebookSize];
        Color[] sourcePixels = readableSource.GetPixels();
        for (int i = 0; i < VQ_CodebookSize; i++) initialCodebook[i] = sourcePixels[Random.Range(0, sourcePixels.Length)];
        codebookBuffer.SetData(initialCodebook);

        VQComputeShader.SetTexture(kernel, "PixelsInput", readableSource);
        VQComputeShader.SetBuffer(kernel, "Codebook", codebookBuffer);
        VQComputeShader.SetTexture(kernel, "Result", resultRT);
        VQComputeShader.SetInt("Width", width);
        VQComputeShader.SetInt("Height", height);
        VQComputeShader.SetInt("CodebookSize", VQ_CodebookSize);

        int threadGroupX = Mathf.CeilToInt(width / 8.0f);
        int threadGroupY = Mathf.CeilToInt(height / 8.0f);
        VQComputeShader.Dispatch(kernel, threadGroupX, threadGroupY, 1);

        Texture2D resultTexture = ReadbackFromRT(resultRT);

        codebookBuffer.Release();
        resultRT.Release();
        DestroyImmediate(readableSource);

        return resultTexture;
    }

    // --- IMPLEMENTATION: Block Deduplication ---
    private Texture2D ExecuteBlockDeduplicationMethod(Texture2D source)
    {
        if (BDComputeShader == null)
        {
            Debug.LogError("Block Deduplication Compute Shader is not assigned!");
            return null;
        }
        const int BLOCK_SIZE = 8;
        if (source.width % BLOCK_SIZE != 0 || source.height % BLOCK_SIZE != 0)
        {
            EditorUtility.DisplayDialog("Error", $"Source texture dimensions ({source.width}x{source.height}) must be a multiple of {BLOCK_SIZE} for this algorithm.", "OK");
            return null;
        }

        Texture2D readableSource = null;
        ComputeBuffer hashesBuffer = null;
        ComputeBuffer blockIndexMapBuffer = null;
        RenderTexture resultRT = null;

        try
        {
            readableSource = CreateUncompressedCopy(source);
            int width = readableSource.width;
            int height = readableSource.height;
            int blockCountX = width / BLOCK_SIZE;
            int blockCountY = height / BLOCK_SIZE;
            int totalBlockCount = blockCountX * blockCountY;

            EditorUtility.DisplayProgressBar("Block Deduplication", "1/4: Calculating hashes on GPU...", 0.1f);
            hashesBuffer = new ComputeBuffer(totalBlockCount, sizeof(uint) * 2);
            int hashKernel = BDComputeShader.FindKernel("CalculateHashes");
            BDComputeShader.SetTexture(hashKernel, "SourceTexture", readableSource);
            BDComputeShader.SetBuffer(hashKernel, "Hashes", hashesBuffer);
            BDComputeShader.SetInt("Width", width);
            BDComputeShader.SetInt("Height", height);
            BDComputeShader.SetInt("BlockSize", BLOCK_SIZE);
            BDComputeShader.Dispatch(hashKernel, blockCountX, blockCountY, 1);

            EditorUtility.DisplayProgressBar("Block Deduplication", "2/4: Analyzing duplicates on CPU...", 0.4f);
            var hashes = new uint2[totalBlockCount];
            hashesBuffer.GetData(hashes);

            var uniqueBlocks = new List<(int originalIndex, uint2 hash)>();
            var blockToUniqueMap = new int[totalBlockCount];

            for (int i = 0; i < totalBlockCount; i++)
            {
                bool foundMatch = false;
                for (int j = 0; j < uniqueBlocks.Count; j++)
                {
                    if (HammingDistance(hashes[i], uniqueBlocks[j].hash) <= BD_SimilarityThreshold)
                    {
                        blockToUniqueMap[i] = j;
                        foundMatch = true;
                        break;
                    }
                }
                if (!foundMatch)
                {
                    blockToUniqueMap[i] = uniqueBlocks.Count;
                    uniqueBlocks.Add((i, hashes[i]));
                }
            }
            Debug.Log($"Block Deduplication: Found {uniqueBlocks.Count} unique blocks out of {totalBlockCount}. Reduction: {100f * (1f - (float)uniqueBlocks.Count / totalBlockCount):F2}%");

            EditorUtility.DisplayProgressBar("Block Deduplication", "3/4: Reconstructing image on GPU...", 0.7f);
            var blockIndexMapData = new uint2[totalBlockCount];
            for (int i = 0; i < totalBlockCount; i++)
            {
                int uniqueIndex = blockToUniqueMap[i];
                int originalIndexOfUnique = uniqueBlocks[uniqueIndex].originalIndex;
                blockIndexMapData[i] = new uint2((uint)uniqueIndex, (uint)originalIndexOfUnique);
            }

            blockIndexMapBuffer = new ComputeBuffer(totalBlockCount, sizeof(uint) * 2);
            blockIndexMapBuffer.SetData(blockIndexMapData);

            resultRT = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32) { enableRandomWrite = true };
            resultRT.Create();

            int reconstructKernel = BDComputeShader.FindKernel("ReconstructImage");

            BDComputeShader.SetTexture(reconstructKernel, "SourceTexture", readableSource);
            BDComputeShader.SetBuffer(reconstructKernel, "BlockIndexMap", blockIndexMapBuffer);
            BDComputeShader.SetTexture(reconstructKernel, "ResultTexture", resultRT);
            BDComputeShader.SetInt("Width", width);
            BDComputeShader.SetInt("Height", height);
            BDComputeShader.SetInt("BlockSize", BLOCK_SIZE);

            BDComputeShader.Dispatch(reconstructKernel, blockCountX, blockCountY, 1);

            EditorUtility.DisplayProgressBar("Block Deduplication", "4/4: Finalizing...", 1.0f);
            return ReadbackFromRT(resultRT);
        }
        finally
        {
            if (hashesBuffer != null) hashesBuffer.Release();
            if (blockIndexMapBuffer != null) blockIndexMapBuffer.Release();
            if (resultRT != null) resultRT.Release();
            if (readableSource != null) DestroyImmediate(readableSource);
        }
    }

    #endregion

    #region Utility & Helpers

    public struct uint2 { public uint x, y; public uint2(uint x, uint y) { this.x = x; this.y = y; } }

    private Texture2D CreateUncompressedCopy(Texture2D source)
    {
        RenderTexture rt = RenderTexture.GetTemporary(source.width, source.height);
        Graphics.Blit(source, rt);
        Texture2D readableTexture = ReadbackFromRT(rt);
        RenderTexture.ReleaseTemporary(rt);
        return readableTexture;
    }

    private Texture2D ReadbackFromRT(RenderTexture rt)
    {
        Texture2D texture = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        RenderTexture.active = rt;
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;
        return texture;
    }

    private Texture2D BlendTextures(Texture2D highQ, Texture2D lowQ, float[,] saliencyMap, int width, int height)
    {
        Color[] highPixels = highQ.GetPixels();
        Color[] lowPixels = lowQ.GetPixels();
        Color[] finalPixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float saliencyValue = saliencyMap[x, y];
                finalPixels[index] = Color.Lerp(lowPixels[index], highPixels[index], saliencyValue);
            }
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(finalPixels);
        result.Apply();
        return result;
    }

    private Texture2D DecodeJPG(byte[] imageData)
    {
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageData);
        return tex;
    }

    private static int HammingDistance(uint2 h1, uint2 h2)
    {
        ulong diffX = h1.x ^ h2.x;
        ulong diffY = h1.y ^ h2.y;
        int distance = 0;
        while (diffX > 0) { distance++; diffX &= diffX - 1; }
        while (diffY > 0) { distance++; diffY &= diffY - 1; }
        return distance;
    }

    [MenuItem("Tools/Advanced/Ultimate GPU Texture Optimizer")]
    private static void OpenWindow() => GetWindow<UltimateTextureOptimizerWindow>("Ultimate Optimizer").Show();

    #endregion
}
#endif
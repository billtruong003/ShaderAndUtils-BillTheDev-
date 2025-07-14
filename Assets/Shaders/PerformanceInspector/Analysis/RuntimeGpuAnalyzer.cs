#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class RuntimeGpuAnalyzer
{
    private readonly GpuTimeRecorder _gpuTimeRecorder;
    private readonly MonoBehaviour _coroutineRunner;

    private bool _isAnalyzing;
    private Renderer _lastAnalyzedTarget;
    private const int AnalysisIterations = 100;
    private const string CommandBufferName = "PerformanceInspector.RuntimeGpuAnalysis";

    public bool IsAnalyzing => _isAnalyzing;
    public Renderer LastAnalyzedTarget => _lastAnalyzedTarget;
    public float GpuTimeMilliseconds { get; private set; } = -1f;
    public bool IsMeasurementReady => GpuTimeMilliseconds >= 0 && !_isAnalyzing;

    public RuntimeGpuAnalyzer(MonoBehaviour coroutineRunner)
    {
        _coroutineRunner = coroutineRunner;
        _gpuTimeRecorder = new GpuTimeRecorder("TargetObjectGpuTime");
    }

    public void Update()
    {
        _gpuTimeRecorder.Update();
    }

    public void TriggerAnalysis(Renderer target)
    {
        if (_isAnalyzing || target == null || !_gpuTimeRecorder.IsRecordingSupported)
        {
            return;
        }
        _coroutineRunner.StartCoroutine(AnalyzeTargetRuntimeCost(target));
    }

    private IEnumerator AnalyzeTargetRuntimeCost(Renderer target)
    {
        _isAnalyzing = true;
        _lastAnalyzedTarget = target;
        GpuTimeMilliseconds = -1f;

        var cmd = new CommandBuffer { name = CommandBufferName };

        _gpuTimeRecorder.BeginRecording(cmd);

        for (int i = 0; i < target.sharedMaterials.Length; i++)
        {
            if (target.sharedMaterials[i] != null)
            {
                // Vẽ đối tượng nhiều lần để có được phép đo ổn định hơn trên GPU
                cmd.DrawRenderer(target, target.sharedMaterials[i], i, -1);
            }
        }

        _gpuTimeRecorder.EndRecording(cmd);

        Graphics.ExecuteCommandBuffer(cmd);
        cmd.Release();

        // Chờ GPU hoàn thành và ProfilerRecorder cập nhật
        yield return new WaitUntil(() => _gpuTimeRecorder.HasValidRecording);

        GpuTimeMilliseconds = _gpuTimeRecorder.GpuTimeNanoseconds * 1e-6f;
        _isAnalyzing = false;
    }

    public void Dispose()
    {
        _gpuTimeRecorder.Dispose();
    }
}
#endif
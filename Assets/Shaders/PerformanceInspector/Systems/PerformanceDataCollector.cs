using Unity.Profiling;

public class PerformanceDataCollector
{
    public float GpuFrameTimeMs { get; private set; }
    public float CpuFrameTimeMs { get; private set; }

    private readonly ProfilerRecorder _gpuFrameTimeRecorder;
    private readonly ProfilerRecorder _cpuFrameTimeRecorder;

    public PerformanceDataCollector()
    {
        _gpuFrameTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time");
        _cpuFrameTimeRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "CPU Main Thread Frame Time");
    }

    public void Start() { }

    public void Stop()
    {
        _gpuFrameTimeRecorder.Dispose();
        _cpuFrameTimeRecorder.Dispose();
    }

    public void Update()
    {
        const float nanosecondsToMilliseconds = 1e-6f;
        GpuFrameTimeMs = _gpuFrameTimeRecorder.LastValue * nanosecondsToMilliseconds;
        CpuFrameTimeMs = _cpuFrameTimeRecorder.LastValue * nanosecondsToMilliseconds;
    }
}
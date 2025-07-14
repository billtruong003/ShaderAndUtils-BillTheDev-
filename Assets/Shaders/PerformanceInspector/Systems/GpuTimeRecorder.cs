using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using System;

#if UNITY_EDITOR
public class GpuTimeRecorder : IDisposable
{
    private const int QueryFrameLatency = 4;
    private readonly GraphicsFence[] _queryFences = new GraphicsFence[QueryFrameLatency];
    private readonly ProfilerMarker _profilerMarker;
    private readonly ProfilerRecorder _recorder;
    private int _fenceIndex = 0;

    public long GpuTimeNanoseconds { get; private set; }
    public bool IsRecordingSupported => SystemInfo.supportsGraphicsFence;
    public bool HasValidRecording { get; private set; }

    public GpuTimeRecorder(string profilerTag)
    {
        _profilerMarker = new ProfilerMarker(profilerTag);
        _recorder = ProfilerRecorder.StartNew(_profilerMarker);
    }

    public void BeginRecording(CommandBuffer cmd)
    {
        if (!IsRecordingSupported) return;
        HasValidRecording = false;
        GpuTimeNanoseconds = -1;
        cmd.BeginSample(_profilerMarker);
    }

    public void EndRecording(CommandBuffer cmd)
    {
        if (!IsRecordingSupported) return;
        cmd.EndSample(_profilerMarker);
        _queryFences[_fenceIndex] = cmd.CreateGraphicsFence(GraphicsFenceType.AsyncQueueSynchronisation, SynchronisationStageFlags.AllGPUOperations);
        _fenceIndex = (_fenceIndex + 1) % QueryFrameLatency;
    }

    public void Update()
    {
        if (!IsRecordingSupported || !_recorder.Valid || HasValidRecording)
        {
            return;
        }

        for (int i = 0; i < _queryFences.Length; i++)
        {
            ref var fence = ref _queryFences[i];

            if (!fence.Equals(default) && fence.passed)
            {
                GpuTimeNanoseconds = _recorder.LastValue;
                HasValidRecording = true;
                fence = default;
                return;
            }
        }
    }

    public void Dispose()
    {
        _recorder.Dispose();
    }
}
#endif
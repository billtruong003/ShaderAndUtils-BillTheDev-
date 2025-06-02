using UnityEngine;
using UnityEngine.Profiling;
using System.Diagnostics;
using System.Collections.Generic;
using System;

public static class PerformanceProfiler
{
    private class MethodStats
    {
        public long TotalTicks { get; set; }
        public int CallCount { get; set; }
        public long TotalGCBytes { get; set; }
        public long LastGCBytes { get; set; } // Lưu GC allocation của lần gọi cuối
    }

    private static readonly Dictionary<string, MethodStats> stats = new Dictionary<string, MethodStats>();
    private static readonly Stopwatch stopwatch = new Stopwatch();
    private static long gcBefore; // Lưu giá trị GC trước khi gọi phương thức

    public static void BeginProfile(string methodName)
    {
        gcBefore = GC.GetTotalMemory(false); // Lấy giá trị GC trước khi bắt đầu
        Profiler.BeginSample(methodName);
        stopwatch.Restart();
    }
    public static void EndProfile(string methodName)
    {
        stopwatch.Stop();
        long gcAfter = GC.GetTotalMemory(false); // Lấy giá trị GC sau khi kết thúc
        Profiler.EndSample();

        if (!stats.TryGetValue(methodName, out var methodStats))
        {
            methodStats = new MethodStats();
            stats[methodName] = methodStats;
        }

        methodStats.TotalTicks += stopwatch.ElapsedTicks;
        methodStats.CallCount++;
        methodStats.LastGCBytes = gcAfter - gcBefore; // GC allocation cho lần gọi này
        methodStats.TotalGCBytes += methodStats.LastGCBytes;

        UnityEngine.Debug.Log($"[{methodName}] Took {stopwatch.ElapsedTicks} ticks (~{(stopwatch.ElapsedTicks * 1000f / Stopwatch.Frequency):F3} ms), GC Alloc: {methodStats.LastGCBytes} bytes");
    }

    public static void LogStats()
    {
        UnityEngine.Debug.Log("=== Performance Profiler Stats ===");
        foreach (var kvp in stats)
        {
            string methodName = kvp.Key;
            var methodStats = kvp.Value;
            float totalMs = methodStats.TotalTicks * 1000f / Stopwatch.Frequency;
            float avgMs = methodStats.CallCount > 0 ? totalMs / methodStats.CallCount : 0;
            UnityEngine.Debug.Log($"[{methodName}] Calls: {methodStats.CallCount}, Total: {totalMs:F3} ms, Avg: {avgMs:F3} ms/call, Total GC: {methodStats.TotalGCBytes} bytes");
        }
    }

    public static void ResetStats()
    {
        stats.Clear();
        UnityEngine.Debug.Log("Performance Profiler stats reset.");
    }
}
using UnityEngine;

public class TestPerformanceWriter : MonoBehaviour
{
    // Các phương thức mẫu để kiểm tra hiệu suất
    public void TestMethod1()
    {
        PerformanceProfiler.BeginProfile("TestMethod1");
        // Mô phỏng công việc nặng
        for (int i = 0; i < 1000000; i++)
        {
            float temp = Mathf.Sin(i * 0.01f);
        }
        PerformanceProfiler.EndProfile("TestMethod1");
    }

    public void TestMethod2()
    {
        PerformanceProfiler.BeginProfile("TestMethod2");
        // Mô phỏng cấp phát bộ nhớ
        var list = new System.Collections.Generic.List<float>();
        for (int i = 0; i < 10000; i++)
        {
            list.Add(i * 1.5f);
        }
        PerformanceProfiler.EndProfile("TestMethod2");
    }

    public void LogProfilerStats()
    {
        PerformanceProfiler.LogStats();
    }

    public void ResetProfilerStats()
    {
        PerformanceProfiler.ResetStats();
    }
}
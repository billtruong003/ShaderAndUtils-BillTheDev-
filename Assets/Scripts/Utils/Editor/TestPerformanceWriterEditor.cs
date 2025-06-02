using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TestPerformanceWriter))]
public class TestPerformanceWriterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TestPerformanceWriter script = (TestPerformanceWriter)target;

        if (GUILayout.Button("Run Test Method 1"))
        {
            script.TestMethod1();
        }

        if (GUILayout.Button("Run Test Method 2"))
        {
            script.TestMethod2();
        }

        if (GUILayout.Button("Log Profiler Stats"))
        {
            script.LogProfilerStats();
        }

        if (GUILayout.Button("Reset Profiler Stats"))
        {
            script.ResetProfilerStats();
        }
    }
}
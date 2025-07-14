using UnityEngine;

public interface IPerformanceVisualizer
{
    void DrawScreenOverlays(PerformanceInspector inspector);
    void DrawSceneVisuals(PerformanceInspector inspector);
}
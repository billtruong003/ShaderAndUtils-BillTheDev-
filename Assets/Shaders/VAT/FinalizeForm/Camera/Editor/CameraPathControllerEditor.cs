#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;

// THAY ĐỔI DUY NHẤT Ở ĐÂY:
[CustomEditor(typeof(CameraPathController))]
public class CameraPathControllerEditor : OdinEditor
{
    // Mọi thứ còn lại giữ nguyên
    private CameraPathController script;

    protected override void OnEnable()
    {
        base.OnEnable();
        script = (CameraPathController)target;
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        SirenixEditorGUI.BeginBox("Hướng Dẫn Scene View");
        EditorGUILayout.HelpBox(
            " - Đường bay sẽ được vẽ trong Scene View khi bạn chọn GameObject này.\n" +
            " - Để chỉnh sửa vị trí, hướng, và độ cong, hãy chọn trực tiếp các GameObject Waypoint trong Hierarchy.",
            MessageType.Info
        );
        SirenixEditorGUI.EndBox();
    }

    // Phần OnSceneGUI hoàn toàn không cần thay đổi vì nó chỉ phụ thuộc vào list `waypoints`.
    private void OnSceneGUI()
    {
        if (script == null || script.waypoints == null) return;

        DrawPath();
        DrawWaypointHandles();
    }

    // ... (toàn bộ các hàm DrawPath, DrawWaypointHandles, DrawControlPointHandle giữ nguyên y hệt)
    private void DrawPath()
    {
        for (int i = 0; i < script.waypoints.Count - 1; i++)
        {
            var p0 = script.waypoints[i];
            var p1 = script.waypoints[i + 1];

            if (p0 == null || p1 == null) continue;

            Handles.color = new Color(1f, 1f, 1f, 0.5f);
            Handles.DrawLine(p0.Position, p0.GetGlobalControlPoint());
            Handles.DrawLine(p1.Position, p1.GetGlobalInverseControlPoint());

            Handles.DrawBezier(
                p0.Position,
                p1.Position,
                p0.GetGlobalControlPoint(),
                p1.GetGlobalInverseControlPoint(),
                Color.white,
                null,
                2f
            );
        }
    }

    private void DrawWaypointHandles()
    {
        if (script.waypoints == null) return;

        for (int i = 0; i < script.waypoints.Count; i++)
        {
            var waypoint = script.waypoints[i];
            if (waypoint == null) continue;

            Handles.Label(waypoint.Position + Vector3.up * 0.5f, $"Waypoint {i}", EditorStyles.boldLabel);

            if (Selection.activeGameObject == waypoint.gameObject)
            {
                DrawControlPointHandle(waypoint, i, true);
                DrawControlPointHandle(waypoint, i, false);
            }
        }
    }

    private void DrawControlPointHandle(CameraWaypoint waypoint, int index, bool isMainControlPoint)
    {
        if (isMainControlPoint && index >= script.waypoints.Count - 1) return;
        if (!isMainControlPoint && index == 0) return;

        Handles.color = Color.yellow;
        Vector3 globalControlPos = isMainControlPoint ? waypoint.GetGlobalControlPoint() : waypoint.GetGlobalInverseControlPoint();

        EditorGUI.BeginChangeCheck();
        Vector3 newGlobalControlPos = Handles.PositionHandle(globalControlPos, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(waypoint, "Move Control Point");
            Vector3 localOffset = Quaternion.Inverse(waypoint.Rotation) * (newGlobalControlPos - waypoint.Position);

            if (isMainControlPoint)
            {
                waypoint.controlPoint = localOffset;
            }
            else
            {
                waypoint.inverseControlPoint = localOffset;
            }
            EditorUtility.SetDirty(waypoint);
        }
    }
}
#endif
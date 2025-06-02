using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CurvedLayoutGroup))]
public class CurvedLayoutGroupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Curved Layout Group", EditorStyles.boldLabel);

        // Hiển thị các trường chung
        EditorGUILayout.PropertyField(serializedObject.FindProperty("ignoreInactive"), new GUIContent("Ignore Inactive Children"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("rotateOffset"), new GUIContent("Rotation Offset"));

        // Hiển thị Animation Curve Settings
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Animation Curve Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("useAnimationCurve"), new GUIContent("Use Animation Curve"));
        if (serializedObject.FindProperty("useAnimationCurve").boolValue)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("curve"), new GUIContent("Curve"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("curveScale"), new GUIContent("Curve Scale"));
        }

        // Hiển thị Layout Type
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Layout Type Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("layoutType"), new GUIContent("Layout Type"));

        // Hiển thị các trường theo Layout Type
        LayoutType layoutType = (LayoutType)serializedObject.FindProperty("layoutType").enumValueIndex;
        EditorGUILayout.Space();
        switch (layoutType)
        {
            case LayoutType.Curved:
                EditorGUILayout.LabelField("Curved Layout Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("horizontal"), new GUIContent("Horizontal"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("width"), new GUIContent("Width"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("height"), new GUIContent("Height"));
                break;
            case LayoutType.Radial:
                EditorGUILayout.LabelField("Radial Layout Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("StartAngle"), new GUIContent("Start Angle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("EndAngle"), new GUIContent("End Angle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotateTowards"), new GUIContent("Rotate Towards"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"), new GUIContent("Radius"));
                break;
            case LayoutType.Spiral:
                EditorGUILayout.LabelField("Spiral Layout Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("StartAngle"), new GUIContent("Start Angle"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spiralAngleIncrement"), new GUIContent("Angle Increment"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spiralDistanceIncrement"), new GUIContent("Distance Increment"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("rotateTowards"), new GUIContent("Rotate Towards"));
                break;
            case LayoutType.CustomCurve:
                EditorGUILayout.LabelField("Custom Curve Layout Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(serializedObject.FindProperty("horizontal"), new GUIContent("Horizontal"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("width"), new GUIContent("Width"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("height"), new GUIContent("Height"));
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    [MenuItem("GameObject/UI/Curved Layout Group")]
    public static void CreateCurvedLayoutGroup()
    {
        GameObject go = new GameObject("Curved Layout Group");
        go.AddComponent<CurvedLayoutGroup>();
        if (Selection.activeGameObject != null)
            go.transform.SetParent(Selection.activeGameObject.transform);
        Selection.activeGameObject = go;
    }
}
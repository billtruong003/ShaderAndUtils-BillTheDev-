using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GridLayout3D))]
public sealed class GridLayout3DEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GridLayout3D gridLayout = (GridLayout3D)target;

        CreateVerticalSpacing(10);

        // Nút thứ nhất: Sắp xếp vị trí
        if (CreateLayoutButton("Arrange Children"))
        {
            gridLayout.ArrangeChildren();
        }

        CreateVerticalSpacing(5); // Thêm một khoảng trống nhỏ giữa hai nút

        // Nút thứ hai: Gán material
        if (CreateLayoutButton("Assign Materials"))
        {
            gridLayout.AssignMaterialsToChildren();
        }
    }

    private void CreateVerticalSpacing(float height)
    {
        GUILayout.Space(height);
    }

    private bool CreateLayoutButton(string buttonText)
    {
        return GUILayout.Button(buttonText, GUILayout.Height(30));
    }
}
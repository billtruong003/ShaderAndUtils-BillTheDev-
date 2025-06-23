// File: MapAreaData.cs
using UnityEngine;

/// <summary>
/// Một lớp chứa dữ liệu để định nghĩa một khu vực bản đồ cụ thể.
/// Không phải là một MonoBehaviour, chỉ dùng để lưu trữ thông tin.
/// </summary>
[System.Serializable]
public class MapAreaData
{
    [Tooltip("Tên định danh cho khu vực này (ví dụ: 'Overworld', 'DungeonLevel1'). Phải là duy nhất.")]
    public string areaName;

    [Header("Textures")]
    [Tooltip("Ảnh bản đồ chính, độ phân giải thấp hoặc trung bình.")]
    public Texture mapTexture;
    [Tooltip("Ảnh bản đồ chi tiết, sẽ hiện ra khi zoom gần. Có thể để trống.")]
    public Texture detailMapTexture;

    [Header("World Bounds")]
    [Tooltip("Tọa độ X, Z của góc dưới-trái của khu vực này trong thế giới game.")]
    public Vector2 worldMinBounds;
    [Tooltip("Tọa độ X, Z của góc trên-phải của khu vực này trong thế giới game.")]
    public Vector2 worldMaxBounds;

    [Header("LOD Settings")]
    [Tooltip("Mức zoom (giá trị _ZoomLevel) để bắt đầu hiện map chi tiết. Giá trị càng nhỏ, zoom càng gần.")]
    [Range(0.01f, 1f)]
    public float detailZoomThreshold = 0.2f;
}
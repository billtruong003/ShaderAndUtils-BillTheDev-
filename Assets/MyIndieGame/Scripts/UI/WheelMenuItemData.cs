// Đặt file này riêng hoặc đặt ngay trên file WheelMenuController.cs
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class WheelMenuItemData
{
    public string Name; // Tên để hiển thị tooltip
    public Sprite Icon; // Icon cho nút
    public UnityEvent OnItemSelected; // Hành động khi được chọn
}
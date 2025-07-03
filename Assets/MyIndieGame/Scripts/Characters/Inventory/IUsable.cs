using UnityEngine;

public interface IUsable
{
    // Trả về true nếu sử dụng thành công
    bool Use(GameObject user);
}
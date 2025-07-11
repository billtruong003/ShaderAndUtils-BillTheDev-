using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
// Giả sử bạn đang dùng LeanTween, nếu chưa có hãy import từ Asset Store.
// Nếu không dùng LeanTween, bạn sẽ cần thay thế logic animation.

public class WheelMenuController : MonoBehaviour
{
    // --- Các biến cũ ---
    [Header("Menu Configuration")]
    [SerializeField] private float menuRadius = 150f;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;

    [Header("Asset References")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset menuItemAsset;

    [Header("Menu Items Data")]
    [SerializeField] private List<WheelMenuItemData> menuItems = new List<WheelMenuItemData>();

    // --- THÊM CÁC BIẾN TÙY CHỈNH MỚI ---
    [Header("Animation Settings")]
    [SerializeField] private float animationDuration = 0.3f;
    [SerializeField] private float itemAnimationDelay = 0.05f;
    [SerializeField] private EasingMode easing = EasingMode.EaseOutBack;

    // --- Các biến private ---
    private VisualElement _root;
    private VisualElement _menuContainer;
    private Label _infoLabel;
    private VisualElement _wheelBackground; // THÊM BIẾN CHO NỀN

    private bool _isMenuVisible = false;
    private bool _isAnimating = false; // Cờ để tránh spam animation
    private List<VisualElement> _spawnedItems = new List<VisualElement>();

    void OnEnable()
    {
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument is not assigned.");
            return;
        }

        _root = uiDocument.rootVisualElement;
        _menuContainer = _root.Q<VisualElement>("menu-container");
        _infoLabel = _root.Q<Label>("info-label");
        _wheelBackground = _root.Q<VisualElement>("wheel-background"); // Lấy tham chiếu đến nền

        if (_menuContainer == null || _infoLabel == null || _wheelBackground == null)
        {
            Debug.LogError("Required elements not found in UXML. Check 'menu-container', 'info-label', 'wheel-background'.");
            return;
        }

        GenerateMenuItems();
        HideMenu(true); // Ẩn menu ngay lập tức khi bắt đầu
    }

    void Update()
    {
        // Thêm điều kiện `!_isAnimating` để không toggle khi đang chạy animation
        if (Input.GetKeyDown(toggleKey) && !_isAnimating)
        {
            ToggleMenu();
        }
    }

    private void GenerateMenuItems()
    {
        _menuContainer.Clear(); // Xóa các item cũ
        _menuContainer.Add(_wheelBackground); // Thêm lại background vào container
        _spawnedItems.Clear();

        if (menuItemAsset == null) return;

        for (int i = 0; i < menuItems.Count; i++)
        {
            VisualElement menuItemInstance = menuItemAsset.Instantiate();
            // LƯU Ý: Không thêm vào class "wheel-menu-item" ở đây nữa
            // Chúng ta sẽ thêm nó vào sau khi tạo

            var iconElement = menuItemInstance.Q<VisualElement>("icon");
            if (menuItems[i].Icon != null)
                iconElement.style.backgroundImage = new StyleBackground(menuItems[i].Icon);

            menuItemInstance.userData = menuItems[i];

            // Đăng ký sự kiện
            menuItemInstance.RegisterCallback<PointerEnterEvent>(OnItemEnter);
            menuItemInstance.RegisterCallback<PointerLeaveEvent>(OnItemLeave);
            menuItemInstance.RegisterCallback<PointerDownEvent>(OnItemSelect);

            // Ban đầu ẩn các item đi để chuẩn bị cho animation
            menuItemInstance.style.opacity = 0;
            menuItemInstance.style.scale = new Scale(Vector3.zero);

            _menuContainer.Add(menuItemInstance);
            _spawnedItems.Add(menuItemInstance);
        }
    }

    private void ToggleMenu()
    {
        _isMenuVisible = !_isMenuVisible;
        if (_isMenuVisible)
        {
            ShowMenu();
        }
        else
        {
            HideMenu(false); // Ẩn với animation
        }
    }

    private void ShowMenu()
    {
        if (_isAnimating) return;
        _root.style.display = DisplayStyle.Flex;
        _isMenuVisible = true;

        // Không cần set vị trí container nữa vì nó đã được căn giữa bằng USS

        // Bắt đầu Coroutine animation
        StartCoroutine(AnimateMenu(true));
    }

    private void HideMenu(bool immediate = false)
    {
        if (_isAnimating && !immediate) return;
        _isMenuVisible = false;
        _infoLabel.text = "";

        if (immediate)
        {
            _root.style.display = DisplayStyle.None;
        }
        else
        {
            // Bắt đầu Coroutine animation
            StartCoroutine(AnimateMenu(false));
        }
    }

    private IEnumerator AnimateMenu(bool show)
    {
        _isAnimating = true;

        if (show)
        {
            _root.style.display = DisplayStyle.Flex;
        }

        float angleIncrement = 360f / _spawnedItems.Count;

        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            var item = _spawnedItems[i];
            float startAngle = i * angleIncrement;
            float targetAngleRad = startAngle * Mathf.Deg2Rad;

            Vector2 targetPos = new Vector2(
                Mathf.Cos(targetAngleRad) * menuRadius,
                -Mathf.Sin(targetAngleRad) * menuRadius
            );

            // Sửa đổi này sử dụng Coroutine thay vì LeanTween để không bị phụ thuộc vào thư viện bên ngoài
            // và để tương thích với nhiều phiên bản Unity hơn.
            float duration = animationDuration;
            float startTime = Time.time;
            float endTime = startTime + duration;
            float t = 0;

            Vector2 startPos = new Vector2(item.style.left.value.value, item.style.top.value.value);

            while (Time.time < endTime)
            {
                t = (Time.time - startTime) / duration;
                // Áp dụng easing (đây là một hàm easing out-back đơn giản)
                float easedT = Ease(t, easing);

                if (show)
                {
                    Vector2 currentPos = Vector2.LerpUnclamped(Vector2.zero, targetPos, easedT);
                    item.style.left = currentPos.x;
                    item.style.top = currentPos.y;
                    // **SỬA LỖI 1**: Sử dụng `style.scale` thay vì `style.transform`.
                    item.style.scale = new Scale(Vector3.one * easedT);
                    item.style.opacity = easedT;
                }
                else
                {
                    Vector2 currentPos = Vector2.LerpUnclamped(startPos, Vector2.zero, easedT);
                    item.style.left = currentPos.x;
                    item.style.top = currentPos.y;
                    // **SỬA LỖI 2**: Sử dụng `style.scale` thay vì `style.transform`.
                    item.style.scale = new Scale(Vector3.one * (1 - easedT));
                    item.style.opacity = 1 - easedT;
                }
                yield return null;
            }

            // Đảm bảo giá trị cuối cùng được đặt chính xác
            if (show)
            {
                item.style.left = targetPos.x;
                item.style.top = targetPos.y;
                item.style.scale = new Scale(Vector3.one);
                item.style.opacity = 1;
            }
            else
            {
                item.style.left = 0;
                item.style.top = 0;
                item.style.scale = new Scale(Vector3.zero);
                item.style.opacity = 0;
            }

            // Delay cho item tiếp theo
            yield return new WaitForSeconds(itemAnimationDelay);
        }

        _isAnimating = false;
        if (!show)
        {
            _root.style.display = DisplayStyle.None;
        }
    }


    // --- Event Handlers (Không thay đổi) ---
    // Các lỗi "cannot convert from 'void' to 'string'" thường là lỗi ảo
    // xuất hiện khi trình biên dịch bị "bối rối" bởi các lỗi khác (như lỗi transform ở trên).
    // Khi sửa các lỗi chính, những lỗi này sẽ tự biến mất.
    private void OnItemEnter(PointerEnterEvent evt)
    {
        if (_isAnimating) return; // Không xử lý hover khi đang animation
        VisualElement item = evt.currentTarget as VisualElement;
        if (item?.userData is WheelMenuItemData data)
        {
            _infoLabel.text = data.Name;
            item.AddToClassList("hovered");
        }
    }

    private void OnItemLeave(PointerLeaveEvent evt)
    {
        VisualElement item = evt.currentTarget as VisualElement;
        _infoLabel.text = "";
        item.RemoveFromClassList("hovered");
    }

    private void OnItemSelect(PointerDownEvent evt)
    {
        if (_isAnimating) return;
        VisualElement item = evt.currentTarget as VisualElement;
        if (item?.userData is WheelMenuItemData data)
        {
            data.OnItemSelected?.Invoke();
            HideMenu(false); // Ẩn với animation khi chọn
        }
    }

    // Hàm easing thay thế cho LeanTween để giải quyết các vấn đề tương thích
    // và lỗi `EasingMode` không có 'Expo'.
    private float Ease(float t, EasingMode mode)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;

        switch (mode)
        {
            case EasingMode.EaseIn:
                return t * t * t;
            case EasingMode.EaseOut:
                return 1 - Mathf.Pow(1 - t, 3);
            case EasingMode.EaseInOut:
                return t < 0.5 ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2;
            case EasingMode.EaseInBack:
                return c3 * t * t * t - c1 * t * t;
            case EasingMode.EaseOutBack:
                return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
            case EasingMode.EaseInOutBack:
                const float c2 = c1 * 1.525f;
                return t < 0.5
                  ? (Mathf.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
                  : (Mathf.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
            case EasingMode.Linear:
            default:
                return t;
        }
    }
}
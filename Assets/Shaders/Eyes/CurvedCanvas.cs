using UnityEngine;
using UnityEngine.UI;
public class CurvedCanvas : MonoBehaviour
{
    [SerializeField] private float curveRadius = 500f; // Bán kính đường cong
    [SerializeField] private float curveAngle = 30f;   // Góc cong tính bằng độ

    private Canvas canvas;

    private void Start()
    {
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Script CurvedCanvas phải được gắn vào một Canvas.");
            return;
        }

        if (canvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogWarning("Chế độ render của Canvas nên được đặt thành World Space để bẻ cong.");
        }

        CurveUIElements();
    }

    private void CurveUIElements()
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>();
        foreach (Graphic graphic in graphics)
        {
            RectTransform rectTransform = graphic.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Kiểm tra nếu đã có CurvedGraphic
                CurvedGraphic curvedGraphic = graphic.gameObject.GetComponent<CurvedGraphic>();
                if (curvedGraphic == null)
                {
                    // Tạo GameObject mới để chứa CurvedGraphic
                    GameObject curvedObj = new GameObject("Curved_" + graphic.name, typeof(RectTransform));
                    curvedObj.transform.SetParent(graphic.transform.parent, false);
                    curvedObj.transform.localPosition = graphic.transform.localPosition;
                    curvedObj.transform.localScale = graphic.transform.localScale;
                    curvedObj.transform.localRotation = graphic.transform.localRotation;

                    // Copy RectTransform properties
                    RectTransform curvedRect = curvedObj.GetComponent<RectTransform>();
                    RectTransform origRect = graphic.GetComponent<RectTransform>();
                    curvedRect.anchorMin = origRect.anchorMin;
                    curvedRect.anchorMax = origRect.anchorMax;
                    curvedRect.offsetMin = origRect.offsetMin;
                    curvedRect.offsetMax = origRect.offsetMax;

                    // Thêm CurvedGraphic
                    curvedGraphic = curvedObj.AddComponent<CurvedGraphic>();
                    curvedGraphic.curveRadius = curveRadius;
                    curvedGraphic.curveAngle = curveAngle;
                    curvedGraphic.rectTransform = curvedRect;

                    // Di chuyển Graphic gốc thành con của curvedObj
                    graphic.transform.SetParent(curvedObj.transform, false);
                }
            }
        }
    }
}

public class CurvedGraphic : Graphic
{
    public float curveRadius = 500f;
    public float curveAngle = 30f;
    public RectTransform rectTransform;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        base.OnPopulateMesh(vh);

        // Biến đổi các đỉnh
        UIVertex vertex = new UIVertex();
        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            Vector3 pos = vertex.position;

            // Tính góc dựa trên vị trí x
            float theta = (pos.x / rectTransform.rect.width) * (curveAngle * Mathf.Deg2Rad);

            // Áp dụng biến đổi hình trụ
            pos.x = curveRadius * Mathf.Sin(theta);
            pos.z = curveRadius * (1 - Mathf.Cos(theta));

            vertex.position = pos;
            vh.SetUIVertex(vertex, i);
        }
    }
}
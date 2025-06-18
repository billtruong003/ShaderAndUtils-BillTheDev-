using UnityEngine;

public class SimpleLiquid : MonoBehaviour
{
    public enum UpdateMode { Normal, UnscaledTime }
    public UpdateMode updateMode;

    [SerializeField, Range(0, 1)]
    private float fillAmount = 0.5f;
    [SerializeField]
    Vector3 maxFillPosition = new Vector3(0, 1, 0);
    [SerializeField]
    Vector3 minFillPosition = new Vector3(0, -1, 0);
    [SerializeField]
    Mesh mesh;
    [SerializeField]
    Renderer rend;

    void Start()
    {
        GetMeshAndRend();
        if (rend != null)
        {
            rend.material = Instantiate(rend.sharedMaterial);
        }
        UpdatePos();
    }

    void OnValidate()
    {
        GetMeshAndRend();
        UpdatePos();
    }

    void GetMeshAndRend()
    {
        if (mesh == null)
        {
            mesh = GetComponent<MeshFilter>().sharedMesh;
        }
        if (rend == null)
        {
            rend = GetComponent<Renderer>();
        }
    }

    private Vector3 pos;
    void UpdatePos()
    {
        pos = Vector3.Lerp(minFillPosition, maxFillPosition, fillAmount);
        rend.sharedMaterial.SetVector("_FillAmount", pos);
    }

    public void SetFillAmount(float value)
    {
        fillAmount = Mathf.Clamp01(value);
        UpdatePos();
    }

    public float GetFillAmount()
    {
        return fillAmount;
    }
}
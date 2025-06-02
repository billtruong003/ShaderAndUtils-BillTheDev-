using UnityEngine;

public class Shape : PersistableObject
{
    private int shapeId = int.MinValue;
    private Color color;
    private MeshRenderer meshRenderer;
    private static int colorPropertyId = Shader.PropertyToID("_Color");
    private static int texturePropertyId = Shader.PropertyToID("_MainTex");
    private static int smoothnessPropertyId = Shader.PropertyToID("_Smoothness");
    private static int metallicPropertyId = Shader.PropertyToID("_Metallic");
    private static MaterialPropertyBlock sharedPropertyBlock;

    public int ShapeId
    {
        get => shapeId;
        set
        {
            if (shapeId == int.MinValue && value != int.MinValue)
            {
                shapeId = value;
            }
            else
            {
                Debug.LogError("Not allowed to change ShapeId.");
            }
        }
    }

    public int MaterialId { get; private set; }
    public int TextureIndex { get; private set; }

    private void Awake()
    {
        if (meshRenderer != null)
            return;
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetMaterial(Material material, int materialId)
    {
        meshRenderer.material = material;
        MaterialId = materialId;
    }

    public void SetTexture(Texture texture, int textureIndex)
    {
        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }
        sharedPropertyBlock.SetTexture(texturePropertyId, texture);
        meshRenderer.SetPropertyBlock(sharedPropertyBlock);
        TextureIndex = textureIndex;
    }

    public void SetSmoothness(float smoothness)
    {
        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }
        sharedPropertyBlock.SetFloat(smoothnessPropertyId, smoothness);
        meshRenderer.SetPropertyBlock(sharedPropertyBlock);
    }

    public void SetMetallic(float metallic)
    {
        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }
        sharedPropertyBlock.SetFloat(metallicPropertyId, metallic);
        meshRenderer.SetPropertyBlock(sharedPropertyBlock);
    }

    public void SetColor(Color color)
    {
        this.color = color;
        if (sharedPropertyBlock == null)
        {
            sharedPropertyBlock = new MaterialPropertyBlock();
        }
        sharedPropertyBlock.SetColor(colorPropertyId, color);
        meshRenderer.SetPropertyBlock(sharedPropertyBlock);
    }

    public override void Save(ObjectDataWriter writer)
    {
        PerformanceProfiler.BeginProfile("Shape.Save");
        base.Save(writer);
        writer.WriteColor(color);
        writer.WriteFloat(sharedPropertyBlock.GetFloat(metallicPropertyId));
        writer.WriteFloat(sharedPropertyBlock.GetFloat(smoothnessPropertyId));
        PerformanceProfiler.EndProfile("Shape.Save");
    }

    public override void Load(ObjectDataReader reader)
    {
        PerformanceProfiler.BeginProfile("Shape.Load");
        base.Load(reader);
        SetColor(reader.ReadColor());
        SetMetallic(reader.ReadFloat());
        SetSmoothness(reader.ReadFloat());
        PerformanceProfiler.EndProfile("Shape.Load");
    }
}
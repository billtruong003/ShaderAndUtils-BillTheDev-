using UnityEngine;

[CreateAssetMenu(fileName = "ShapeFactory", menuName = "ObjectVariety/ShapeFactory")]
public class ShapeFactory : ScriptableObject
{
    [SerializeField] private Shape[] prefabs;
    [SerializeField] private Material[] materials;
    [SerializeField] private Texture[] textures;
    private int textureIndex = 0;
    public Shape Get(int shapeId, int materialID = 0)
    {
        Shape shape = Instantiate(prefabs[shapeId]);
        shape.ShapeId = shapeId;
        shape.SetMaterial(materials[materialID], materialID);
        textureIndex = GetRandomTextureIndex();
        shape.SetTexture(textures[textureIndex], textureIndex);
        return shape;
    }

    public Shape Get(int shapeId, Transform parent, int materialID = 0, int textureIndex = 0)
    {
        Shape shape = Instantiate(prefabs[shapeId], parent);
        shape.ShapeId = shapeId;
        shape.SetMaterial(materials[materialID], materialID);
        shape.SetTexture(textures[textureIndex], textureIndex);
        return shape;
    }

    public Shape GetRandom()
    {
        return Get(Random.Range(0, prefabs.Length));
    }

    public Shape GetRandom(Transform parent)
    {
        return Get(Random.Range(0, prefabs.Length), parent);
    }

    public int GetRandomTextureIndex()
    {
        return Random.Range(0, textures.Length);

    }

    public Texture GetTexture(int index)
    {
        if (index < 0 || index >= textures.Length)
        {
            Debug.LogError("Texture index out of range.");
            return null;
        }
        return textures[index];
    }

    public Texture GetRandomTexture()
    {
        return textures[Random.Range(0, textures.Length)];
    }
}
using UnityEngine;

[AddComponentMenu("Tools/Material Baker")]
public class MaterialBaker : MonoBehaviour
{
    [Tooltip("The Material you have configured with the TextureAuthoringTool shader.")]
    public Material materialToBake;

    [Tooltip("The resolution of the output texture.")]
    public Vector2Int textureSize = new Vector2Int(1024, 1024);

    [Tooltip("The relative path within Assets where the texture will be saved.")]
    public string saveFolderPath = "Baked Textures";

    [Tooltip("The name of the final baked texture file.")]
    public string fileName = "BakedTexture";
}
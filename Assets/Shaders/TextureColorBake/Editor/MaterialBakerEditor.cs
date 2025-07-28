using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(MaterialBaker))]
public class MaterialBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MaterialBaker baker = (MaterialBaker)target;
        EditorGUILayout.Space();
        if (GUILayout.Button("Bake Material to Texture", GUILayout.Height(40)))
        {
            BakeTexture(baker);
        }
    }

    private void BakeTexture(MaterialBaker baker)
    {
        if (baker.materialToBake == null)
        {
            Debug.LogError("Material Baker: 'Material To Bake' is not assigned!");
            return;
        }

        RenderTexture rt = RenderTexture.GetTemporary(baker.textureSize.x, baker.textureSize.y, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(null, rt, baker.materialToBake, 0);

        Texture2D bakedTexture = new Texture2D(rt.width, rt.height, TextureFormat.ARGB32, false);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = rt;
        bakedTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        bakedTexture.Apply();
        RenderTexture.active = previousActive;
        RenderTexture.ReleaseTemporary(rt);

        byte[] bytes = bakedTexture.EncodeToPNG();
        DestroyImmediate(bakedTexture);

        try
        {
            string fullDirectoryPath = Path.Combine(Application.dataPath, baker.saveFolderPath);
            Directory.CreateDirectory(fullDirectoryPath);
            string fullFilePath = Path.Combine(fullDirectoryPath, $"{baker.fileName}.png");
            File.WriteAllBytes(fullFilePath, bytes);
            Debug.Log($"<color=lime>Bake successful! Texture saved to: Assets/{baker.saveFolderPath}/{baker.fileName}.png</color>");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save texture: {e.Message}");
            return;
        }
        AssetDatabase.Refresh();
    }
}
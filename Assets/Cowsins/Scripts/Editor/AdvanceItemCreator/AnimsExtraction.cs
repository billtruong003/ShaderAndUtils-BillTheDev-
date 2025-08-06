// FbxAnimationExtractor.cs

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;

/// <summary>
/// Cung cấp công cụ trong Unity Editor để trích xuất các Animation Clip từ file FBX.
/// </summary>
public static class FbxAnimationExtractor
{
    private const string MENU_PATH = "Assets/Tools/Extract Animations From Selected Folders";
    private const string DESTINATION_FOLDER_NAME = "Anims";

    /// <summary>
    /// Hàm xác thực để bật/tắt menu item.
    /// Menu item chỉ được kích hoạt khi có ít nhất một thư mục được chọn.
    /// </summary>
    [MenuItem(MENU_PATH, true)]
    private static bool ValidateExtractAnimations()
    {
        return GetSelectedFolderPaths().Any();
    }

    /// <summary>
    /// Hàm chính được gọi khi người dùng nhấp vào menu item.
    /// Quét qua các thư mục được chọn và trích xuất animation từ các file FBX.
    /// </summary>
    [MenuItem(MENU_PATH, false, 100)]
    private static void ExtractAnimationsFromSelectedFolders()
    {
        List<string> folderPaths = GetSelectedFolderPaths();
        int totalFbxProcessed = 0;
        int totalClipsExtracted = 0;

        try
        {
            AssetDatabase.StartAssetEditing();

            foreach (string folderPath in folderPaths)
            {
                string[] fbxGuids = AssetDatabase.FindAssets("t:Model", new[] { folderPath });

                foreach (string fbxGuid in fbxGuids)
                {
                    string fbxPath = AssetDatabase.GUIDToAssetPath(fbxGuid);
                    int clipsExtracted = ProcessSingleFbxFile(fbxPath);
                    if (clipsExtracted > 0)
                    {
                        totalFbxProcessed++;
                        totalClipsExtracted += clipsExtracted;
                    }
                }
            }
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        ShowCompletionDialog(totalFbxProcessed, totalClipsExtracted);
    }

    /// <summary>
    /// Lấy danh sách các đường dẫn của những thư mục đang được chọn trong cửa sổ Project.
    /// </summary>
    private static List<string> GetSelectedFolderPaths()
    {
        return Selection.GetFiltered<DefaultAsset>(SelectionMode.Assets)
            .Select(AssetDatabase.GetAssetPath)
            .Where(AssetDatabase.IsValidFolder)
            .ToList();
    }

    /// <summary>
    /// Xử lý một file FBX duy nhất: tìm, sao chép và lưu các animation clip của nó.
    /// </summary>
    /// <returns>Số lượng clip đã được trích xuất.</returns>
    private static int ProcessSingleFbxFile(string fbxPath)
    {
        IEnumerable<AnimationClip> sourceClips = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>();

        if (!sourceClips.Any())
        {
            return 0;
        }

        string sourceDirectory = Path.GetDirectoryName(fbxPath);
        string destinationFolderPath = Path.Combine(sourceDirectory, DESTINATION_FOLDER_NAME);
        EnsureFolderExists(destinationFolderPath);

        int extractedCount = 0;
        foreach (AnimationClip sourceClip in sourceClips)
        {
            // Bỏ qua các animation rỗng hoặc mặc định mà đôi khi FBX import vào.
            if (sourceClip.legacy || sourceClip.name.StartsWith("__preview__"))
            {
                continue;
            }

            AnimationClip newClipInstance = new AnimationClip();
            EditorUtility.CopySerialized(sourceClip, newClipInstance);

            string newAssetPath = Path.Combine(destinationFolderPath, $"{newClipInstance.name}.anim");
            newAssetPath = AssetDatabase.GenerateUniqueAssetPath(newAssetPath);

            AssetDatabase.CreateAsset(newClipInstance, newAssetPath);
            extractedCount++;
        }

        return extractedCount;
    }

    /// <summary>
    /// Đảm bảo rằng một thư mục tồn tại tại đường dẫn đã cho. Nếu không, tạo nó.
    /// </summary>
    private static void EnsureFolderExists(string path)
    {
        if (!Directory.Exists(path))
        {
            string parentFolder = Path.GetDirectoryName(path);
            string newFolderName = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parentFolder, newFolderName);
        }
    }

    /// <summary>
    /// Hiển thị hộp thoại thông báo kết quả cho người dùng.
    /// </summary>
    private static void ShowCompletionDialog(int fbxCount, int clipCount)
    {
        string title = "Animation Extraction Complete";
        string message = $"Successfully extracted {clipCount} animation clip(s) from {fbxCount} FBX file(s).";

        if (fbxCount == 0)
        {
            message = "No FBX files with animations were found in the selected folder(s).";
        }

        EditorUtility.DisplayDialog(title, message, "OK");
    }
}
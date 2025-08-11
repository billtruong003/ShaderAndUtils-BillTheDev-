using UnityEditor;
using UnityEngine;

public class AnimationRetargetingTool : EditorWindow
{
    private GameObject targetModelRoot;
    private AnimationClip sourceClip;
    private Vector2 scrollPosition;

    [MenuItem("Tools/Animation Retargeting Tool")]
    public static void ShowWindow()
    {
        GetWindow<AnimationRetargetingTool>("Animation Retargeting");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawHeader();
        DrawInputFields();
        DrawActionButton();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("Animation Path Retargeting Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "This tool remaps an Animation Clip from an old hierarchy to a new model hierarchy based on bone names.\n\n" +
            "1. Drag your new model's root GameObject into the 'Target Model Root' field.\n" +
            "2. Drag the old Animation Clip you want to fix into the 'Source Animation Clip' field.\n" +
            "3. Click the button to generate a new, remapped animation asset.",
            MessageType.Info);
        EditorGUILayout.Space();
    }

    private void DrawInputFields()
    {
        targetModelRoot = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Target Model Root", "The root GameObject of the new model whose hierarchy you want to target."),
            targetModelRoot,
            typeof(GameObject),
            true);

        sourceClip = (AnimationClip)EditorGUILayout.ObjectField(
            new GUIContent("Source Animation Clip", "The original Animation Clip with broken paths."),
            sourceClip,
            typeof(AnimationClip),
            false);
    }

    private void DrawActionButton()
    {
        EditorGUILayout.Space(20);

        if (GUILayout.Button("Remap And Save New Animation", GUILayout.Height(40)))
        {
            ProcessAnimationRemapping();
        }
    }

    private void ProcessAnimationRemapping()
    {
        if (!AreInputsValid())
        {
            return;
        }

        AnimationClip remappedClip = AnimationPathRemapper.CreateRemappedClip(targetModelRoot, sourceClip);

        if (remappedClip != null)
        {
            SaveRemappedClip(remappedClip);
        }
        else
        {
            EditorUtility.DisplayDialog(
                "Remapping Failed",
                "Could not create the remapped animation clip. Check the console for more details.",
                "OK");
        }
    }

    private bool AreInputsValid()
    {
        if (targetModelRoot == null || sourceClip == null)
        {
            EditorUtility.DisplayDialog(
                "Input Missing",
                "Please assign both the Target Model Root and the Source Animation Clip before proceeding.",
                "OK");
            return false;
        }
        return true;
    }

    private void SaveRemappedClip(AnimationClip clipToSave)
    {
        string suggestedPath = AssetDatabase.GetAssetPath(sourceClip);
        string suggestedDirectory = System.IO.Path.GetDirectoryName(suggestedPath);
        string suggestedFileName = $"{sourceClip.name}_Remapped.anim";
        string defaultPath = System.IO.Path.Combine(suggestedDirectory, suggestedFileName);

        string savePath = EditorUtility.SaveFilePanel("Save Remapped Animation Clip", suggestedDirectory, suggestedFileName, "anim");

        if (string.IsNullOrEmpty(savePath))
        {
            // User cancelled the save dialog.
            // We should destroy the temporary clip object to avoid memory leaks.
            DestroyImmediate(clipToSave);
            return;
        }

        // Convert absolute path to a project-relative path.
        string relativePath = "Assets" + savePath.Substring(Application.dataPath.Length);

        AssetDatabase.CreateAsset(clipToSave, relativePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Success",
            $"Successfully remapped and saved the new animation clip at:\n{relativePath}",
            "OK");

        // Highlight the newly created asset in the project window.
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<AnimationClip>(relativePath));
    }
}
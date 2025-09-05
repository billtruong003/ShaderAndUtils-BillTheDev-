using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class SceneSwitcher : MonoBehaviour
{
    [Title("Scene Quick Switch")]
#if UNITY_EDITOR
    [ValueDropdown(nameof(GetAllBuildScenes), AppendNextDrawer = true, NumberOfItemsBeforeEnablingSearch = 10)]
#endif
    [SerializeField]
    [Required("Vui lòng chọn một scene.")]
    private string sceneToLoad;

    [Button(ButtonSizes.Large, Name = "Switch to Selected Scene")]
    [GUIColor(0.2f, 0.8f, 0.2f)]
    [PropertySpace(10)]
    public void SwitchToSelectedScene()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            Debug.LogError("Scene name is not selected. Cannot switch scene.");
            return;
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            LoadSceneInEditor();
            return;
        }
#endif
        SceneManager.LoadScene(sceneToLoad);
    }

#if UNITY_EDITOR
    private void LoadSceneInEditor()
    {
        var scenePath = GetScenePathByName(sceneToLoad);
        if (string.IsNullOrEmpty(scenePath))
        {
            Debug.LogError($"Could not find path for scene '{sceneToLoad}'. Is it added to Build Settings?");
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }
    }

    private static IEnumerable<string> GetAllBuildScenes()
    {
        return EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => Path.GetFileNameWithoutExtension(scene.path))
            .ToList();
    }

    private static string GetScenePathByName(string sceneName)
    {
        var scene = EditorBuildSettings.scenes
            .FirstOrDefault(s => Path.GetFileNameWithoutExtension(s.path) == sceneName);
        return scene?.path;
    }
#endif
}
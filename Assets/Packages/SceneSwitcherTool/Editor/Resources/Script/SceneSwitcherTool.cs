using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using UnityEditor.SceneManagement;


public class SceneSwitcherTool : EditorWindow
{
    private SceneListSO sceneList;
    private string searchQuery = "";
    private int selectedTab = 0;
    private string[] tabs = { "All Scenes", "Bookmarked Scenes" };
    private SceneAsset selectedSceneAsset;
    private double lastClickTime = 0;
    private SceneAsset lastClickedSceneAsset;
    private const float doubleClickThreshold = 0.3f;
    private SceneAsset newSceneAsset;
    private Vector2 scrollPosition;
    private const string SceneListAssetPath = "Assets/Editor/SceneList.asset";

    [MenuItem("Tools/Bill Utils/Scene Switcher Tool")]
    public static void ShowWindow()
    {
        GetWindow<SceneSwitcherTool>("Scene Switcher Tool");
    }

    private void OnEnable()
    {
        // Load the SceneListSO
        LoadSceneList();

        // Subscribe to play mode state changes to handle persistence
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Before entering play mode, ensure the SceneListSO is saved
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            SaveSceneList();
        }
        // After exiting play mode, reload the SceneListSO
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            LoadSceneList();
        }
    }

    private void LoadSceneList()
    {
        sceneList = AssetDatabase.LoadAssetAtPath<SceneListSO>(SceneListAssetPath);
        if (sceneList == null)
        {
            // Ensure the Editor folder exists
            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            {
                AssetDatabase.CreateFolder("Assets", "Editor");
                AssetDatabase.Refresh(); // Ensure Unity recognizes the new folder
            }

            // Create the SceneListSO asset
            sceneList = ScriptableObject.CreateInstance<SceneListSO>();

            // Initialize with scenes from Build Settings
            foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
            {
                string scenePath = buildScene.path;
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
                if (sceneAsset != null && !sceneList.scenes.Contains(sceneAsset))
                {
                    sceneList.scenes.Add(sceneAsset);
                }
            }

            AssetDatabase.CreateAsset(sceneList, SceneListAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"SceneListSO created at {SceneListAssetPath} with {sceneList.scenes.Count} scenes from Build Settings");
        }
    }

    private void SaveSceneList()
    {
        if (sceneList != null)
        {
            EditorUtility.SetDirty(sceneList);
            AssetDatabase.SaveAssets();
        }
    }

    private void OnGUI()
    {
        GUILayout.Space(10); // Top padding

        // Search Bar
        GUILayout.Label("Search Scenes", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        searchQuery = EditorGUILayout.TextField(searchQuery);
        if (GUILayout.Button("Clear", GUILayout.Width(50)))
        {
            searchQuery = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();
        GUILayout.Space(10);

        // Tabs
        selectedTab = GUILayout.Toolbar(selectedTab, tabs);

        // Scene Management and List
        if (selectedTab == 0)
        {
            DrawAllScenesTab();
        }
        else
        {
            DrawBookmarkedScenesTab();
        }

        // Action Buttons
        DrawActionButtons();

        // Credit
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Made by BillTheDev", EditorStyles.miniLabel);
    }

    private void DrawAllScenesTab()
    {
        // Scene Management Controls
        GUILayout.BeginHorizontal();
        newSceneAsset = EditorGUILayout.ObjectField(newSceneAsset, typeof(SceneAsset), false) as SceneAsset;
        if (GUILayout.Button("Add to List", GUILayout.Width(100)))
        {
            if (newSceneAsset != null && !sceneList.scenes.Contains(newSceneAsset))
            {
                sceneList.scenes.Add(newSceneAsset);
                SaveSceneList();
            }
        }
        if (GUILayout.Button("Remove Selected", GUILayout.Width(100)))
        {
            if (selectedSceneAsset != null && sceneList.scenes.Contains(selectedSceneAsset))
            {
                sceneList.scenes.Remove(selectedSceneAsset);
                sceneList.bookmarkedScenes.Remove(selectedSceneAsset);
                SaveSceneList();
            }
        }
        GUILayout.EndHorizontal();

        DrawSceneList(sceneList.scenes);
    }

    private void DrawBookmarkedScenesTab()
    {
        DrawSceneList(sceneList.bookmarkedScenes);
    }

    private void DrawSceneList(List<SceneAsset> scenes)
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        foreach (SceneAsset sceneAsset in scenes)
        {
            if (sceneAsset == null) continue;
            string sceneName = sceneAsset.name;
            if (!string.IsNullOrEmpty(searchQuery) && !sceneName.ToLower().Contains(searchQuery.ToLower()))
            {
                continue;
            }

            EditorGUILayout.BeginVertical("Box");
            GUILayout.BeginHorizontal();

            if (sceneAsset == selectedSceneAsset)
            {
                GUI.backgroundColor = Color.cyan;
            }
            else
            {
                GUI.backgroundColor = Color.white;
            }

            if (GUILayout.Button(new GUIContent(sceneName, EditorGUIUtility.IconContent("d_UnityEditor.GameView").image, "Click to select, double-click to open")))
            {
                HandleSceneButtonClick(sceneAsset);
            }

            GUI.backgroundColor = Color.white; // Reset

            string bookmarkIcon = sceneList.bookmarkedScenes.Contains(sceneAsset) ? "★" : "☆";
            if (GUILayout.Button(new GUIContent(bookmarkIcon, "Toggle bookmark"), GUILayout.Width(20)))
            {
                ToggleBookmark(sceneAsset);
            }

            GUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }
        GUILayout.EndScrollView();
    }

    private void HandleSceneButtonClick(SceneAsset sceneAsset)
    {
        double currentTime = EditorApplication.timeSinceStartup;
        if (lastClickedSceneAsset == sceneAsset && (currentTime - lastClickTime) < doubleClickThreshold)
        {
            string path = AssetDatabase.GetAssetPath(sceneAsset);
            EditorSceneManager.OpenScene(path);
            lastClickedSceneAsset = null;
        }
        else
        {
            selectedSceneAsset = sceneAsset;
            lastClickedSceneAsset = sceneAsset;
            lastClickTime = currentTime;
        }
    }

    private void ToggleBookmark(SceneAsset sceneAsset)
    {
        if (sceneList.bookmarkedScenes.Contains(sceneAsset))
        {
            sceneList.bookmarkedScenes.Remove(sceneAsset);
        }
        else
        {
            sceneList.bookmarkedScenes.Add(sceneAsset);
        }
        SaveSceneList();
    }

    private void DrawActionButtons()
    {
        GUILayout.Space(10);
        GUILayout.BeginHorizontal();

        GUI.enabled = selectedSceneAsset != null;
        if (GUILayout.Button(new GUIContent("Open", "Open the selected scene")))
        {
            if (selectedSceneAsset != null)
            {
                string path = AssetDatabase.GetAssetPath(selectedSceneAsset);
                EditorSceneManager.OpenScene(path);
                SaveSceneList();
            }
        }
        if (GUILayout.Button(new GUIContent("Open Additive", "Open the selected scene additively")))
        {
            if (selectedSceneAsset != null)
            {
                string path = AssetDatabase.GetAssetPath(selectedSceneAsset);
                EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                SaveSceneList();
            }
        }
        if (GUILayout.Button(new GUIContent("Play", "Play the selected scene")))
        {
            if (selectedSceneAsset != null)
            {
                string path = AssetDatabase.GetAssetPath(selectedSceneAsset);
                EditorSceneManager.OpenScene(path);
                SaveSceneList(); // Save before entering play mode
                EditorApplication.isPlaying = true;
            }
        }

        GUI.enabled = true;
        if (GUILayout.Button(new GUIContent("Remove All Bookmarks", "Remove all bookmarked scenes")))
        {
            sceneList.bookmarkedScenes.Clear();
            SaveSceneList();
        }

        GUILayout.EndHorizontal();
    }
}
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class SceneSwitcherPro : EditorWindow
{
    private SceneSwitcherProData data;
    private Vector2 scrollPosition;
    private string searchQuery = "";
    private int selectedTab = 0;
    private string[] tabs = { "All Scenes", "Bookmarked" };

    private const string DataAssetPath = "Assets/Editor/SceneSwitcherProData.asset";
    private const string DefaultGroupName = "Uncategorized";

    private static class Icons
    {
        public static readonly Texture2D SceneIcon = EditorGUIUtility.FindTexture("d_SceneAsset Icon");
        public static readonly Texture2D FolderIcon = EditorGUIUtility.FindTexture("d_Folder Icon");
        public static readonly Texture2D BookmarkActiveIcon = EditorGUIUtility.FindTexture("d_Favorite On Icon");
        public static readonly Texture2D BookmarkInactiveIcon = EditorGUIUtility.FindTexture("d_Favorite Icon");
        public static readonly Texture2D BuildSettingsIcon = EditorGUIUtility.FindTexture("d_BuildSettings.DefaultIcon");
        public static readonly Texture2D AddIcon = EditorGUIUtility.FindTexture("d_Toolbar Plus");
        public static readonly Texture2D RefreshIcon = EditorGUIUtility.FindTexture("d_Refresh");
    }

    [MenuItem("Tools/Bill Utils/Scene Switcher Pro")]
    public static void ShowWindow()
    {
        GetWindow<SceneSwitcherPro>("Scene Switcher Pro");
    }

    private void OnEnable()
    {
        LoadData();
    }

    private void LoadData()
    {
        data = AssetDatabase.LoadAssetAtPath<SceneSwitcherProData>(DataAssetPath);
        if (data == null)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Editor"))
            {
                AssetDatabase.CreateFolder("Assets", "Editor");
            }
            data = CreateInstance<SceneSwitcherProData>();
            data.GetOrCreateGroup(DefaultGroupName);
            AssetDatabase.CreateAsset(data, DataAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private void SaveData()
    {
        if (data != null)
        {
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }
    }

    private void OnGUI()
    {
        DrawHeader();
        HandleDragAndDrop();
        DrawContent();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        searchQuery = EditorGUILayout.TextField(searchQuery, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
        if (GUILayout.Button(Icons.RefreshIcon, EditorStyles.toolbarButton, GUILayout.Width(28)))
        {
            ScanProjectForScenes();
        }
        EditorGUILayout.EndHorizontal();
        selectedTab = GUILayout.Toolbar(selectedTab, tabs);
    }

    private void DrawContent()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        if (selectedTab == 0)
        {
            DrawAllScenes();
        }
        else
        {
            DrawBookmarkedScenes();
        }

        EditorGUILayout.EndScrollView();

        if (GUILayout.Button(new GUIContent(" Create New Group", Icons.AddIcon), GUILayout.Height(25)))
        {
            CreateNewGroup();
        }
    }

    private void DrawAllScenes()
    {
        if (data.sceneGroups == null) return;

        foreach (var group in data.sceneGroups.ToList())
        {
            DrawGroup(group);
        }
    }

    private void DrawBookmarkedScenes()
    {
        var bookmarkedEntries = data.sceneGroups
            .SelectMany(g => g.scenes)
            .Where(s => data.bookmarkedSceneGUIDs.Contains(s.guid))
            .ToList();

        if (!bookmarkedEntries.Any())
        {
            EditorGUILayout.HelpBox("No bookmarked scenes. Click the star icon next to a scene to bookmark it.", MessageType.Info);
            return;
        }

        foreach (var sceneEntry in bookmarkedEntries)
        {
            var sceneAsset = sceneEntry.SceneAsset;
            if (sceneAsset != null && DoesSceneMatchSearch(sceneAsset))
            {
                DrawSceneEntry(sceneEntry, null, false);
            }
        }
    }

    private void DrawGroup(SceneGroup group)
    {
        Rect groupHeaderRect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        GUI.Box(groupHeaderRect, GUIContent.none);

        group.isExpanded = EditorGUILayout.Foldout(group.isExpanded, new GUIContent($" {group.name}", Icons.FolderIcon), true, EditorStyles.foldout);

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(new GUIContent("Open All", "Opens all scenes in this group additively."), EditorStyles.toolbarButton))
        {
            OpenGroupScenes(group);
        }

        EditorGUILayout.EndHorizontal();

        if (Event.current.type == EventType.ContextClick && groupHeaderRect.Contains(Event.current.mousePosition))
        {
            ShowGroupContextMenu(group);
            Event.current.Use();
        }

        if (group.isExpanded)
        {
            EditorGUI.indentLevel++;
            foreach (var sceneEntry in group.scenes.ToList())
            {
                var sceneAsset = sceneEntry.SceneAsset;
                if (sceneAsset != null && DoesSceneMatchSearch(sceneAsset))
                {
                    DrawSceneEntry(sceneEntry, group, true);
                }
            }
            EditorGUI.indentLevel--;
        }
    }

    private void DrawSceneEntry(SceneEntry sceneEntry, SceneGroup parentGroup, bool allowReorder)
    {
        var sceneAsset = sceneEntry.SceneAsset;
        if (sceneAsset == null) return;

        Rect entryRect = EditorGUILayout.BeginHorizontal();

        bool isBookmarked = data.bookmarkedSceneGUIDs.Contains(sceneEntry.guid);
        var bookmarkIcon = isBookmarked ? Icons.BookmarkActiveIcon : Icons.BookmarkInactiveIcon;
        if (GUILayout.Button(bookmarkIcon, GUIStyle.none, GUILayout.Width(20), GUILayout.Height(18)))
        {
            ToggleBookmark(sceneEntry.guid);
        }

        bool isInBuildSettings = IsSceneInBuildSettings(sceneAsset);
        if (isInBuildSettings)
        {
            GUILayout.Label(new GUIContent(Icons.BuildSettingsIcon, "This scene is in Build Settings."), GUILayout.Width(20));
        }
        else
        {
            GUILayout.Space(24); // Keep alignment consistent
        }

        var labelContent = new GUIContent(sceneAsset.name, Icons.SceneIcon, AssetDatabase.GetAssetPath(sceneAsset));

        if (EditorSceneManager.GetActiveScene().path == AssetDatabase.GetAssetPath(sceneAsset))
        {
            GUI.contentColor = Color.cyan;
        }

        EditorGUILayout.LabelField(labelContent);
        GUI.contentColor = Color.white;


        EditorGUILayout.EndHorizontal();

        if (Event.current.type == EventType.MouseDown && entryRect.Contains(Event.current.mousePosition))
        {
            if (Event.current.button == 0 && Event.current.clickCount == 2)
            {
                OpenScene(sceneAsset, OpenSceneMode.Single);
                Event.current.Use();
            }
            else if (Event.current.button == 1)
            {
                ShowSceneContextMenu(sceneAsset, parentGroup, sceneEntry);
                Event.current.Use();
            }
        }
    }

    private void HandleDragAndDrop()
    {
        Rect dropArea = new Rect(0, GUILayoutUtility.GetLastRect().yMax, position.width, position.height - GUILayoutUtility.GetLastRect().yMax);
        EventType eventType = Event.current.type;

        if (eventType == EventType.DragUpdated || eventType == EventType.DragPerform)
        {
            if (!dropArea.Contains(Event.current.mousePosition)) return;

            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (eventType == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                var defaultGroup = data.GetOrCreateGroup(DefaultGroupName);

                foreach (var draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject is SceneAsset sceneAsset)
                    {
                        AddSceneToGroup(sceneAsset, defaultGroup);
                    }
                }
                SaveData();
            }
            Event.current.Use();
        }
    }

    private bool DoesSceneMatchSearch(SceneAsset sceneAsset)
    {
        if (string.IsNullOrEmpty(searchQuery)) return true;
        return sceneAsset.name.ToLower().Contains(searchQuery.ToLower());
    }

    private void ToggleBookmark(string guid)
    {
        if (data.bookmarkedSceneGUIDs.Contains(guid))
        {
            data.bookmarkedSceneGUIDs.Remove(guid);
        }
        else
        {
            data.bookmarkedSceneGUIDs.Add(guid);
        }
        SaveData();
    }

    private void OpenScene(SceneAsset sceneAsset, OpenSceneMode mode)
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(sceneAsset), mode);
        }
    }

    private void OpenGroupScenes(SceneGroup group)
    {
        if (group.scenes.Count == 0 || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        var firstScene = group.scenes[0].SceneAsset;
        if (firstScene != null)
        {
            EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(firstScene), OpenSceneMode.Single);
        }

        for (int i = 1; i < group.scenes.Count; i++)
        {
            var sceneAsset = group.scenes[i].SceneAsset;
            if (sceneAsset != null)
            {
                EditorSceneManager.OpenScene(AssetDatabase.GetAssetPath(sceneAsset), OpenSceneMode.Additive);
            }
        }
    }

    private bool IsSceneInBuildSettings(SceneAsset sceneAsset)
    {
        string path = AssetDatabase.GetAssetPath(sceneAsset);
        return EditorBuildSettings.scenes.Any(scene => scene.enabled && scene.path == path);
    }

    private void AddSceneToBuildSettings(SceneAsset sceneAsset)
    {
        string path = AssetDatabase.GetAssetPath(sceneAsset);
        var currentBuildScenes = EditorBuildSettings.scenes.ToList();

        if (currentBuildScenes.Any(s => s.path == path)) return;

        var newBuildScene = new EditorBuildSettingsScene(path, true);
        currentBuildScenes.Add(newBuildScene);
        EditorBuildSettings.scenes = currentBuildScenes.ToArray();
    }

    private void RemoveSceneFromBuildSettings(SceneAsset sceneAsset)
    {
        string path = AssetDatabase.GetAssetPath(sceneAsset);
        var currentBuildScenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToArray();
        EditorBuildSettings.scenes = currentBuildScenes;
    }

    private void RemoveSceneFromGroup(SceneEntry sceneEntry, SceneGroup group)
    {
        group.scenes.Remove(sceneEntry);
        SaveData();
    }

    private void CreateNewGroup()
    {
        string newGroupName = $"New Group {data.sceneGroups.Count + 1}";
        data.GetOrCreateGroup(newGroupName);
        SaveData();
    }

    private void RenameGroup(SceneGroup group, string newName)
    {
        group.name = newName;
        SaveData();
    }

    private void DeleteGroup(SceneGroup group)
    {
        if (EditorUtility.DisplayDialog("Delete Group", $"Are you sure you want to delete the group '{group.name}'? Scenes within it will be moved to '{DefaultGroupName}'.", "Delete", "Cancel"))
        {
            var defaultGroup = data.GetOrCreateGroup(DefaultGroupName);
            foreach (var scene in group.scenes)
            {
                if (!defaultGroup.scenes.Any(s => s.guid == scene.guid))
                {
                    defaultGroup.scenes.Add(scene);
                }
            }
            data.sceneGroups.Remove(group);
            SaveData();
        }
    }

    private void ScanProjectForScenes()
    {
        string[] sceneGUIDs = AssetDatabase.FindAssets("t:Scene");
        var defaultGroup = data.GetOrCreateGroup(DefaultGroupName);

        foreach (string guid in sceneGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            bool alreadyExists = data.sceneGroups.Any(g => g.scenes.Any(s => s.guid == guid));
            if (sceneAsset != null && !alreadyExists)
            {
                AddSceneToGroup(sceneAsset, defaultGroup);
            }
        }
        EditorUtility.DisplayDialog("Scan Complete", $"Found and added new scenes to the '{DefaultGroupName}' group.", "OK");
        SaveData();
    }

    private void AddSceneToGroup(SceneAsset sceneAsset, SceneGroup group)
    {
        string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sceneAsset));
        if (!group.scenes.Any(s => s.guid == guid))
        {
            group.scenes.Add(new SceneEntry(sceneAsset));
        }
    }

    private void ShowSceneContextMenu(SceneAsset sceneAsset, SceneGroup parentGroup, SceneEntry sceneEntry)
    {
        GenericMenu menu = new GenericMenu();
        string path = AssetDatabase.GetAssetPath(sceneAsset);

        menu.AddItem(new GUIContent("Open Scene"), false, () => OpenScene(sceneAsset, OpenSceneMode.Single));
        menu.AddItem(new GUIContent("Open Scene Additive"), false, () => OpenScene(sceneAsset, OpenSceneMode.Additive));
        menu.AddSeparator("");

        if (IsSceneInBuildSettings(sceneAsset))
        {
            menu.AddItem(new GUIContent("Remove from Build Settings"), false, () => RemoveSceneFromBuildSettings(sceneAsset));
        }
        else
        {
            menu.AddItem(new GUIContent("Add to Build Settings"), false, () => AddSceneToBuildSettings(sceneAsset));
        }

        menu.AddSeparator("");

        menu.AddItem(new GUIContent("Reveal in Project"), false, () => EditorGUIUtility.PingObject(sceneAsset));

        if (parentGroup != null)
        {
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Remove from Group"), false, () => RemoveSceneFromGroup(sceneEntry, parentGroup));
        }

        menu.ShowAsContext();
    }

    private void ShowGroupContextMenu(SceneGroup group)
    {
        GenericMenu menu = new GenericMenu();

        menu.AddItem(new GUIContent("Rename Group"), false, () =>
        {
            // A simple popup would be needed here, for simplicity we skip this UI but the function is ready
            // Example: StringInputDialog.Show("Rename Group", "Enter new name:", group.name, newName => RenameGroup(group, newName));
        });

        if (group.name != DefaultGroupName)
        {
            menu.AddItem(new GUIContent("Delete Group"), false, () => DeleteGroup(group));
        }

        menu.ShowAsContext();
    }
}
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

[Serializable]
public class SceneEntry
{
    public string guid;
    public SceneAsset SceneAsset => AssetDatabase.LoadAssetAtPath<SceneAsset>(AssetDatabase.GUIDToAssetPath(guid));

    public SceneEntry(SceneAsset sceneAsset)
    {
        guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sceneAsset));
    }
}

[Serializable]
public class SceneGroup
{
    public string name;
    public bool isExpanded = true;
    public List<SceneEntry> scenes = new List<SceneEntry>();

    public SceneGroup(string name)
    {
        this.name = name;
    }
}

public class SceneSwitcherProData : ScriptableObject
{
    public List<SceneGroup> sceneGroups = new List<SceneGroup>();
    public List<string> bookmarkedSceneGUIDs = new List<string>();

    public SceneGroup GetOrCreateGroup(string name)
    {
        SceneGroup group = sceneGroups.Find(g => g.name == name);
        if (group == null)
        {
            group = new SceneGroup(name);
            sceneGroups.Add(group);
        }
        return group;
    }
}
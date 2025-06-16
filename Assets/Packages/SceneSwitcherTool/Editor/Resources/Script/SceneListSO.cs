
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SceneListSO : ScriptableObject
{
    public List<SceneAsset> scenes = new List<SceneAsset>();
    public List<SceneAsset> bookmarkedScenes = new List<SceneAsset>();
}
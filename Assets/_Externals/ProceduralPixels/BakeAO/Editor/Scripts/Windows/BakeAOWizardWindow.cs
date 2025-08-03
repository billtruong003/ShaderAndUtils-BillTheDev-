/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using static ProceduralPixels.BakeAO.Editor.BakeAOUtils;

namespace ProceduralPixels.BakeAO.Editor
{
    internal class BakeAOWizardWindow : EditorWindow
    {
        private const string windowIconGUID = "4ddfaa2a48125ff4288ea52083181fa5";

        [MenuItem("Window/Procedural Pixels/Bake AO/Baking wizard")]
        public static void OpenWindow()
        {
            var window = GetWindow<BakeAOWizardWindow>(false, "Bake AO - Wizard", true);
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(windowIconGUID));
            window.titleContent = new GUIContent("Bake AO - Wizard", icon);
        }

        DragArea dragArea = null;
        [SerializeField] List<SourceObject> sourceObjects = new List<SourceObject>();
        List<GenericBakingSetup> selectedBakingSetups = new List<GenericBakingSetup>();
        GenericBakingSetupEditor selectedBakingSetupsEditor = null;
        UnityEditor.Editor allSourceObjectsEditor = null;
        SerializedProperty selectedProperty = null;

        GUIRecycledList sourceObjectsGUI;

        private void OnEnable()
        {
            minSize = new Vector2(500, 650);

            if (sourceObjects == null)
                sourceObjects = new List<SourceObject>();

            sourceObjects.RemoveAll(s => s == null);

            if (selectedBakingSetups == null)
                selectedBakingSetups = new List<GenericBakingSetup>();

            dragArea = new DragArea(Rect.zero, DragAcceptCheck, OnDragAccept);
            sourceObjectsGUI = new GUIRecycledList(EditorGUIUtility.singleLineHeight, DrawSourceObject, GetSourceObjectsCount, EmptyContainerGUI);
            RefreshSelectedSettings();
            RefreshSourceObjectsEditor();
            EditorApplication.update += Repaint;
        }

        private void EmptyContainerGUI(Rect contentRect)
        {
            Rect content = new Rect(contentRect.xMin, contentRect.yMin, contentRect.width, contentRect.height / 2);
            GUI.Box(contentRect, "");
            EditorGUI.DropShadowLabel(content, "Drag files here");
        }

        private void OnDisable()
        {
            EditorApplication.update -= Repaint;
        }

        Vector2 scrollPos;
        bool bakingFoldoutState = true;
        bool wasPresetWindowVisible = false;

        bool requireRepaint = false;

        private void Update()
        {
            if (requireRepaint)
                Repaint();

            requireRepaint = false;
        }

        private void OnGUI()
        {
            float windowHeight = position.height;
            float windowWidth = position.width;

            sourceObjects.RemoveAll(so => so == null);
            sourceObjects.RemoveAll(so => so.genericBakingSetup == null || so.bakeContext == null);

            var rect = EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight);
            EditorGUI.DropShadowLabel(rect, "Objects selected for baking:");
            HorizontalRectLayout helpRect = new HorizontalRectLayout(rect);
            GUIUtility.HelpButton(helpRect.GetFromRight(21), "https://proceduralpixels.com/BakeAO/Documentation/BakingWizard");

            if (selectedProperty != null)
            {
                EditorGUI.BeginChangeCheck();
                HorizontalRectLayout rectLayout = new HorizontalRectLayout(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight));
                rectLayout.AddPadding(4);
                EditorGUI.PropertyField(rectLayout.GetFromLeft(rectLayout.controlRect.height), selectedProperty, new GUIContent(""), false);
                if (EditorGUI.EndChangeCheck())
                {
                    allSourceObjectsEditor.serializedObject.ApplyModifiedProperties();
                    allSourceObjectsEditor.serializedObject.Update();
                }
            }

            var sourceObjectsGUIRect = EditorGUILayout.GetControlRect(true, windowHeight - 436);
            sourceObjectsGUI.Draw(sourceObjectsGUIRect);

            dragArea.dragRect = sourceObjectsGUIRect;
            dragArea.OnGUI();

            EditorGUI.BeginDisabledGroup(selectedBakingSetups.Count == 0);

            if (GUILayout.Button("Remove selected from the list"))
            {
                sourceObjects.RemoveAll(s => s.selected);
                RefreshSourceObjectsEditor();
            }

            EditorGUI.EndDisabledGroup();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            RefreshSelectedSettings();
            if (selectedBakingSetupsEditor != null)
            {
                EditorGUIUtility.labelWidth = windowWidth * 0.4f;
                EditorGUILayout.Space();

                HorizontalRectLayout layout = new HorizontalRectLayout(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight));
                var presetButtonRect = layout.GetFromRight(21);
                var helpButton = layout.GetFromRight(21);
                layout.GetFromRight(4);

                bakingFoldoutState = EditorGUI.Foldout(layout.GetReminder(), bakingFoldoutState, "Baking");

                if (bakingFoldoutState)
                {
                    EditorGUI.indentLevel++;
                    selectedBakingSetupsEditor.OnInspectorGUI();
                    EditorGUI.indentLevel--;
                }

                GUIUtility.HelpButton(helpButton, "https://proceduralpixels.com/BakeAO/Documentation/BakingParameters");
                PresetSelector.DrawPresetButton(presetButtonRect, selectedBakingSetupsEditor.targets);

                // I cant get preset to refresh data immediately after data was changed so I'm hacking the editor here.
                bool isPresetWindowVisible = HasOpenInstances<PresetSelector>();
                if (wasPresetWindowVisible && !isPresetWindowVisible)
                {
                    RefreshSelectedSettings(true);
                    requireRepaint = true;
                }
                wasPresetWindowVisible = isPresetWindowVisible;
            }

            const float DotsButtonSize = 21.0f;
            EditorGUILayout.Space();

            HorizontalRectLayout bakeButtonLayout = new HorizontalRectLayout(EditorGUILayout.GetControlRect(true, EditorGUIUtility.singleLineHeight));
            
            if (GUI.Button(bakeButtonLayout.GetFromRight(DotsButtonSize), "..."))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("Bake all and replace existing textures"), false, () =>
                {
                    bool hasAnyError = StartBaking(true);
                    RefreshSourceObjectsEditor();
                    ActiveTasksWindow.FocusOrOpen(GetType());
                });
                menu.ShowAsContext();
            }

            bakeButtonLayout.GetFromRight(2);

            if (GUI.Button(bakeButtonLayout.GetReminder(), "Bake all"))
            {
                bool hasAnyError = StartBaking(false);

                RefreshSourceObjectsEditor();
                ActiveTasksWindow.FocusOrOpen(GetType());
            }

            EditorGUILayout.EndScrollView();
        }

        private bool StartBaking(bool overrideExistingTextures = false)
        {
            bool hasAnyError = false;

            for (int i = sourceObjects.Count - 1; i >= 0; i--)
            {
                var sourceObject = sourceObjects[i];

                if (sourceObject == null)
                    continue;

                if (sourceObject.genericBakingSetup.ArePathsValid())
                {
                    if (sourceObject.Bake(overrideExistingTextures))
                        sourceObjects.RemoveAt(i);
                    else
                        hasAnyError = true;
                }
                else
                    Debug.LogWarning("Some of the objects have invalid paths and can't be baked. Please make sure that all the paths are valid to start baking.");
            }

            return hasAnyError;
        }

        private int GetSourceObjectsCount()
        {
            return sourceObjects.Count;
        }

        private void DrawSourceObject(int index, Rect rect)
        {
            if (index >= sourceObjects.Count)
                return;

            var sourceObject = sourceObjects[index];
            if (sourceObject == null)
                sourceObjects.RemoveAt(index);
            else
                DrawSourceObject(sourceObject, index, rect);
        }

        private int lastToggledIndex = -1;

        private void DrawSourceObject(SourceObject sourceObject, int index, Rect rect)
        {
            if (sourceObject.selected)
            {
                var color = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.5f, 0.7f, 0.7f);
                GUI.Box(rect, "");
                GUI.backgroundColor = color;
            }

            HorizontalRectLayout layout = new HorizontalRectLayout(rect);

            layout.GetFromLeft(4);
            layout.GetFromRight(4);

            Rect checkboxRect = layout.GetFromLeft(rect.height);

            Rect propertyRect = layout.GetReminder();

            EditorGUI.BeginDisabledGroup(true);
            sourceObject.bakeContext = EditorGUI.ObjectField(propertyRect, sourceObject.bakeContext, typeof(UnityEngine.Object), true);
            EditorGUI.EndDisabledGroup();

            bool toggleValue = EditorGUI.Toggle(checkboxRect, sourceObject.selected);
            if (toggleValue != sourceObject.selected)
            {
                if (Event.current.shift)
                {
                    int diff = Mathf.Clamp(index - lastToggledIndex, -1, 1);
                    for (int i = lastToggledIndex; ; i += diff)
                    {
                        sourceObjects[i].selected = toggleValue;
                        if (i == index)
                            break;
                    }
                }
                else
                    sourceObject.selected = toggleValue;

                RefreshSelectedSettings();
                RefreshSourceObjectsEditor();

                lastToggledIndex = index;
            }
        }

        private void RefreshSelectedSettings(bool forceRefresh = false)
        {
            var newBakingSetupsCount = sourceObjects.Count(s => s.selected);

            if (selectedBakingSetups.Count != newBakingSetupsCount || selectedBakingSetupsEditor == null || forceRefresh)
            {
                if (selectedBakingSetupsEditor != null)
                {
                    DestroyImmediate(selectedBakingSetupsEditor);
                    selectedBakingSetupsEditor = null;
                    selectedBakingSetups.Clear();
                }

                if (newBakingSetupsCount == 0)
                {
                    Repaint();
                    return;
                }

                selectedBakingSetups.Clear();
                selectedBakingSetups.AddRange(sourceObjects.Where(s => s.selected).Select(s => s.genericBakingSetup));

                selectedBakingSetupsEditor = (GenericBakingSetupEditor)UnityEditor.Editor.CreateEditor(selectedBakingSetups.ToArray(), typeof(GenericBakingSetupEditor));
            }

            Repaint();
        }

        public DragAndDropVisualMode DragAcceptCheck(UnityEngine.Object[] objectReferences, string[] paths)
        {
            return DragAndDropVisualMode.Link;
        }

        public void OnDragAccept(UnityEngine.Object[] objectReferences, string[] paths)
        {
            List<SourceObject> distinctObjects = new List<SourceObject>();
            List<SourceObject> uniqueObjects = new List<SourceObject>();

            foreach (var obj in objectReferences)
            {
                if (BakeAOUtils.IsSceneObject(obj))
                    AddObjectToBake(obj, (o) => uniqueObjects.Add(o));
                else
                    AddObjectToBake(obj, (o) => distinctObjects.Add(o));
            }

            foreach (var path in paths)
                AddPathToBake(path, (o) => distinctObjects.Add(o));

            this.sourceObjects.AddRange(distinctObjects.Distinct(new SourceObjectGUIDComparer()));
            this.sourceObjects.AddRange(uniqueObjects);

            RefreshSourceObjectsEditor();
        }

        private void RefreshSourceObjectsEditor()
        {
            DestroyImmediate(allSourceObjectsEditor);
            allSourceObjectsEditor = null;
            selectedProperty = null;

            if (sourceObjects.Count < 1)
                return;

            allSourceObjectsEditor = UnityEditor.Editor.CreateEditor(sourceObjects.ToArray());
            selectedProperty = allSourceObjectsEditor.serializedObject.FindProperty("selected");
        }

        public void AddObjectToBake(UnityEngine.Object obj, Action<SourceObject> addObjectAction)
        {
            if (obj is Component c)
                AddObjectToBake(c.gameObject, addObjectAction);
            else if (obj is Transform t)
                AddObjectToBake(t.gameObject, addObjectAction);
            else if (obj is GameObject gameObject)
            {
                AddMeshFilterToBake(gameObject.GetComponent<MeshFilter>(), addObjectAction);
                AddSkinnedMeshToBake(gameObject.GetComponent<SkinnedMeshRenderer>(), addObjectAction);

                Transform transform = gameObject.transform;
                foreach (Transform childTransform in transform)
                {
                    if (childTransform == transform)
                        continue;

                    AddObjectToBake(childTransform.gameObject, addObjectAction);
                }
            }
            else if (obj is Mesh mesh)
            {
                AddMeshToBake(mesh, addObjectAction);
            }
            else
            {
                // Here can process not supported object, if needed
            }
        }

        private void AddSkinnedMeshToBake(SkinnedMeshRenderer skinnedMeshRenderer, Action<SourceObject> addObjectAction)
        {
            if (skinnedMeshRenderer == null)
                return;

            if (skinnedMeshRenderer.sharedMesh != null)
                addObjectAction(SourceObject.Create(skinnedMeshRenderer, true));
        }

        private void AddMeshFilterToBake(MeshFilter meshFilter, Action<SourceObject> addObjectAction)
        {
            if (meshFilter == null)
                return;

            if (meshFilter.sharedMesh != null)
                addObjectAction(SourceObject.Create(meshFilter, true));
        }

        private void AddMeshToBake(Mesh mesh, Action<SourceObject> addObjectAction)
        {
            if (mesh == null)
                return;

            addObjectAction(SourceObject.Create(mesh, true));
        }

        public void AddPathToBake(string path, Action<SourceObject> addObjectAction)
        {
            if (Path.IsPathRooted(path))
            {
                return;
            }

            // Find meshes in the path and add them for baking. Scan through children recursively (if there are any).
            if (AssetDatabase.IsValidFolder(path))
            {
                DirectoryInfo directory = new DirectoryInfo(path);
                PathUtils.EnumerateFilesRecursively(path, (p) => AddPathToBake(p, addObjectAction));
            }
            else
            {
                var filePath = path;
                var folderPath = PathUtils.GetContainingFolderPath(filePath);
                if (AssetDatabase.IsValidFolder(folderPath))
                {
                    FileInfo fileInfo = new FileInfo(filePath);
                    if (fileInfo.Extension.Equals(".meta", StringComparison.OrdinalIgnoreCase))
                        return; // Meta file
                    else
                    {
                        // Valid file
                        var allObjs = AssetDatabase.LoadAllAssetRepresentationsAtPath(filePath);
                        foreach (var obj in allObjs)
                            AddObjectToBake(obj, addObjectAction);
                    }
                }
                else
                {
                    // Files from packages can be processed here.
                }
            }
        }

        public class SourceObjectGUIDComparer : EqualityComparer<SourceObject>
        {
            public override bool Equals(SourceObject x, SourceObject y)
            {
                if (x == null && y == null)
                    return true;

                if (x == null || y == null)
                    return false;

                return string.Equals(x.meshGUID, y.meshGUID);
            }

            public override int GetHashCode(SourceObject obj)
            {
                if (obj == null)
                    return 0;
                else 
                    return obj.meshGUID.GetHashCode();
            }
        }

        [Serializable]
        public class SourceObject : ScriptableObject
        {
            public bool selected = false;
            public UnityEngine.Object bakeContext;
            public GenericBakingSetup genericBakingSetup;
            public string meshGUID;

            public void SetContextObject(MeshFilter meshFilter)
            {
                bakeContext = meshFilter;
                meshGUID = BakeAOUtils.FirstOrDefaultMeshGUID(bakeContext);
            }

            public void SetContextObject(Mesh mesh)
            {
                bakeContext = mesh;
                meshGUID = BakeAOUtils.FirstOrDefaultMeshGUID(bakeContext);
            }

            public void SetContextObject(SkinnedMeshRenderer skinnedMeshRenderer)
            {
                bakeContext = skinnedMeshRenderer;
                meshGUID = BakeAOUtils.FirstOrDefaultMeshGUID(bakeContext);
            }

            public static SourceObject Default
            {
                get
                {
                    var instance = CreateInstance<SourceObject>();
                    instance.genericBakingSetup = GenericBakingSetup.DefaultFromPresets;
                    return instance;
                }
            }

            public static SourceObject Create(SkinnedMeshRenderer skinnedMeshRenderer, bool selected = false)
            {
                var instance = Default;
                instance.SetContextObject(skinnedMeshRenderer);
                instance.selected = selected;
                return instance;
            }

            public static SourceObject Create(Mesh mesh, bool selected = false)
            {
                var instance = Default;
                instance.SetContextObject(mesh);
                instance.selected = selected;
                return instance;
            }

            public static SourceObject Create(MeshFilter meshFilter, bool selected = false)
            {
                var instance = Default;
                instance.SetContextObject(meshFilter);
                instance.selected = selected;
                return instance;
            }

            public bool Bake(bool overrideExistingTexture = false)
            {
                if (genericBakingSetup.TryGetBakingSetup(bakeContext, out var bakingSetup))
                {
                    var sourceMesh = bakingSetup.meshesToBake[0].mesh;

                    PathResolver resolver = new PathResolver(bakeContext, sourceMesh);
                    resolver.fallbackMeshFolderPath = genericBakingSetup.meshFolderFallback;

                    string filePath = resolver.Resolve(genericBakingSetup.filePath);
                    filePath = PathUtils.EnsureContainsExtension(filePath, ".png");

                    PathUtils.EnsureDirectoryExists(PathUtils.GetContainingFolderPath(filePath));

                    foreach (var meshToBake in bakingSetup.meshesToBake)
                    {
                        if (!meshToBake.mesh.DoesHaveUVSet(meshToBake.uv))
                        {
                            BakeAOErrorMeshList.AddError(new BakeAOErrorMeshList.ErrorData(meshToBake.uv, meshToBake.mesh));
                            return false;
                        }
                    }

                    BakeAndSaveTextureToAssets(bakingSetup, filePath, overrideExistingTexture, bakeContext);

                    return true;
                }

                Debug.LogError("Error when getting baking setup.");
                return false;
            }
        }

        public class DragArea
        {
            public Rect dragRect;
            public DragAcceptPredicate dragAcceptPredicate; // Return rejected or none if no drag acceptance.
            public DragPerformAction dragPerformAction;

            public DragArea(Rect dragRect, DragAcceptPredicate dragAcceptPredicate, DragPerformAction dragPerformAction)
            {
                this.dragRect = dragRect;
                this.dragAcceptPredicate = dragAcceptPredicate;
                this.dragPerformAction = dragPerformAction;
            }

            public void OnGUI()
            {
                switch (Event.current.type)
                {
                    case EventType.DragUpdated:
                        OnDragUpdate();
                        break;
                    case EventType.DragExited:
                        OnDragExit();
                        break;
                    case EventType.DragPerform:
                        TryPerformDrag();
                        break;
                }
            }

            private void TryPerformDrag()
            {
                if (!IsMouseInArea)
                    return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

                if (dragAcceptPredicate != null)
                    DragAndDrop.visualMode = dragAcceptPredicate.Invoke(DragAndDrop.objectReferences, DragAndDrop.paths);

                if (DragAndDrop.visualMode != DragAndDropVisualMode.Rejected && DragAndDrop.visualMode != DragAndDropVisualMode.None)
                    dragPerformAction?.Invoke(DragAndDrop.objectReferences, DragAndDrop.paths);
            }

            private void OnDragExit()
            {
                if (!IsMouseInArea)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }
            }

            private void OnDragUpdate()
            {
                if (!IsMouseInArea)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                    return;
                }

                if (dragAcceptPredicate != null)
                    DragAndDrop.visualMode = dragAcceptPredicate.Invoke(DragAndDrop.objectReferences, DragAndDrop.paths);
            }

            private bool IsMouseInArea => dragRect.Contains(Event.current.mousePosition);

            public delegate DragAndDropVisualMode DragAcceptPredicate(UnityEngine.Object[] objectReferences, string[] paths);
            public delegate void DragPerformAction(UnityEngine.Object[] objectReferences, string[] paths);
        }
    }
}
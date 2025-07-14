#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Mighty;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;
using static Mighty.MightyCoreData;
using static Mighty.MightyWindowManagerStateful;
using static MightyTracking.TrackingRenders;
using UnityEditor.Build;
using UnityEngine.SceneManagement;

namespace MightyTracking
{
    [InitializeOnLoad]
    [ExecuteInEditMode]
    public class TrackingCore : ModuleBase
    {
        // Module identification
        public override string ModuleId => "com.mighty.tracking";
        public override string ModuleName => "Mighty Tracking";

        // Module data - now instance fields instead of static
        private MightyCoreData _core;
        private TrackingData _data;
        private TrackingData.SceneData _sceneData;
        private int _sceneIndex = -1;
        private VisualElement sceneViewContainer;
        private MappableTypeInfo _trackingType;

        /// <summary>
        /// Static registration method called by Unity's InitializeOnLoad
        /// </summary>
        static TrackingCore()
        {
            DevLog("TrackingCore - Registering with new lifecycle system");

            // Register with new lifecycle system
            var module = new TrackingCore();
            MightyCore.RegisterModule(module);

            // Keep the old Scene GUI subscription for now since it's not part of normal lifecycle
            SceneView.duringSceneGui -= OnSceneStatic;
            SceneView.duringSceneGui += OnSceneStatic;

#if MIGHTY_TRACKING
#else
            DevLog($"Thank you for installing Mighty Tracking!  If you enjoy, don't forget to leave a review!  Version");
            MightyTrackingDefineSymbol();
#endif
        }

        /// <summary>
        /// Initialize the module - load data, setup references
        /// </summary>
        protected override void OnInitialize()
        {
            DevLog($"Initializing {ModuleName}...");

            // Load module data
            _core = MightyCoreData.Load();
            _data = TrackingData.Load();

            // Ensure MappableTypeInfo exists
            _trackingType = MightyCore.data.MappableTypesInfo.FirstOrDefault(types => types.Name == "Tracking");

            DevLog($"{ModuleName} initialization complete");
        }

        /// <summary>
        /// Start the module - subscribe to events, setup UI
        /// </summary>
        protected override void OnStart()
        {
            DevLog($"Starting {ModuleName}...");


            // Subscribe to Unity events safely
            SafeSubscribeSceneOpened(GetSceneData);
            SafeSubscribeSceneClosing(SceneClosing);
            SafeSubscribeEditorCallback(ref EditorApplication.update, EditorUpdate);
            SafeSubscribeEditorWantsToQuit(WantsToQuit);

            // Subscribe to core events
            SafeSubscribe(ref EndTrackingCleanup, EndTracking);
            SafeSubscribe(ref DeletePlaythroughData, DeletePlaythrough);
            SafeSubscribe(ref MapInitialized, MapInit);
            SafeSubscribe(ref RunPlaybackMinMaxUpdated, BuildSceneView);
            SafeSubscribe(ref MightyCoreData.RecordPlaythroughChanged, OnRecordPlaythroughChanged);
            SafeSubscribe(ref RunPlaybackChanged, RunPlaybackChange);

            // Setup UI and components
            SetupUnityComponents();
            BuildSceneView();
            TrackingViews.Rebuild();

            // Show getting started window for first-time users
            ShowGettingStartedIfFirstRun();

            DevLog($"{ModuleName} started successfully");
        }

        /// <summary>
        /// Stop the module - cleanup UI, but keep data
        /// </summary>
        protected override void OnStop()
        {
            DevLog($"Stopping {ModuleName}...");

            // Clear scene view elements
            ClearSceneViewElements();

            // Events are automatically unsubscribed by base class
            DevLog($"{ModuleName} stopped");
        }

        /// <summary>
        /// Shutdown the module completely - cleanup all resources
        /// </summary>
        protected override void OnShutdown()
        {
            DevLog($"Shutting down {ModuleName}...");

            // Save data before shutdown
            SaveModuleData();

            // Clear all references
            _core = null;
            _data = null;
            _sceneData = null;
            sceneViewContainer = null;
            _trackingType = null;
            _sceneIndex = -1;

            DevLog($"{ModuleName} shutdown complete");
        }

        #region Helper Methods for Safe Event Subscription

        /// <summary>
        /// Safely subscribe to Unity's scene opened event
        /// </summary>
        private void SafeSubscribeSceneOpened(EditorSceneManager.SceneOpenedCallback handler)
        {
            EditorSceneManager.sceneOpened -= handler;
            EditorSceneManager.sceneOpened += handler;
            eventUnsubscribers.Add(() => EditorSceneManager.sceneOpened -= handler);
        }

        /// <summary>
        /// Safely subscribe to Unity's scene closing event
        /// </summary>
        private void SafeSubscribeSceneClosing(EditorSceneManager.SceneClosingCallback handler)
        {
            EditorSceneManager.sceneClosing -= handler;
            EditorSceneManager.sceneClosing += handler;
            eventUnsubscribers.Add(() => EditorSceneManager.sceneClosing -= handler);
        }

        /// <summary>
        /// Safely subscribe to Unity's editor wants to quit event
        /// </summary>
        private void SafeSubscribeEditorWantsToQuit(Func<bool> handler)
        {
            EditorApplication.wantsToQuit -= handler;
            EditorApplication.wantsToQuit += handler;
            eventUnsubscribers.Add(() => EditorApplication.wantsToQuit -= handler);
        }

        #endregion

        #region Module-Specific Methods

        private void SetupUnityComponents()
        {
            GameObject anchor = GameObject.Find("MightySceneAnchor");
            if (anchor != null)
            {
                TrackingViews views = anchor.GetComponent<TrackingViews>();
                if (views == null)
                {
                    anchor.AddComponent<TrackingViews>();
                }
            }
        }

        private void SaveModuleData()
        {
            if (_data != null)
            {
                EditorUtility.SetDirty(_data);
                AssetDatabase.SaveAssets();
            }
        }

        private void ClearSceneViewElements()
        {
            // Clear any scene view tracking elements
            if (sceneViewContainer != null)
            {
                sceneViewContainer.Clear();
                sceneViewContainer = null;
            }
        }

        private void ShowGettingStartedIfFirstRun()
        {
            string firstRunKey = "MightyTracking_FirstRun";
            if (!EditorPrefs.GetBool(firstRunKey, false))
            {
                ICommand command = new OpenGettingStartedWindowCommand();
                command.Execute();
                EditorPrefs.SetBool(firstRunKey, true);
            }
        }

        #endregion

        #region Event Handlers

        private void EditorUpdate()
        {
            if (State != ModuleLifecycleState.Started || _data == null)
                return;

            // Handle tracking-specific editor updates
        }

        private bool WantsToQuit()
        {
            DevLog("TrackingCore WantsToQuit");
            SaveModuleData();
            return true;
        }

        private void SceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
        {
            DevLog($"TrackingCore SceneClosing({scene.name}, {removingScene})");
            // Scene-specific cleanup handled by core system
        }

        public void GetSceneData(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            DevLog($"TrackingCore GetSceneData({scene.name}, {mode})");
            GetSceneDataInstance();
        }

        #endregion

        #region Static Methods (Legacy Support)

        /// <summary>
        /// Static scene GUI handler - kept for compatibility
        /// </summary>
        private static void OnSceneStatic(SceneView view)
        {
            // Find the active module instance and delegate to it
            var module = MightyCore.GetModule("com.mighty.tracking") as TrackingCore;
            if (module?.State == ModuleLifecycleState.Started)
            {
                module.OnScene(view);
            }
        }

        private void OnScene(SceneView view)
        {
            if (State != ModuleLifecycleState.Started)
                return;

            // Handle scene GUI
        }

        static void MightyTrackingDefineSymbol()
        {
#if UNITY_2023_2_OR_NEWER // Unity 2023.2, 2023.3, 6.0+
            const string symbol = "MIGHTY_TRACKING";
            NamedBuildTarget buildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string scriptingDefines = PlayerSettings.GetScriptingDefineSymbols(buildTarget);

            if (!scriptingDefines.Contains(symbol))
            {
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, string.IsNullOrEmpty(scriptingDefines) ? symbol : scriptingDefines + ";" + symbol);
            }
#else
            // Fallback for older Unity versions
            const string symbol = "MIGHTY_TRACKING";
            string scriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            if (!scriptingDefines.Contains(symbol))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.IsNullOrEmpty(scriptingDefines) ? symbol : scriptingDefines + ";" + symbol);
            }
#endif
        }

        #endregion

        private void RunPlaybackChange()
        {
            DevLog("RunPlaybackChange");
            TrackingViews.UpdateSelectedTrackers();
        }

        private void MapInit()
        {
            DevLog("TrackingCore MapInit");
            if (EditorApplication.isPlaying || _trackingType?.IsActive != true)
            {
                return;
            }

            // Add null checks to prevent NullReferenceException during scene switching
            if (Map == null)
            {
                DevLog("MapInit: map is null, skipping initialization");
                // initializing = false;
                // MightyMap.Init();
                return;
            }

            if (_data == null)
            {
                DevLog("MapInit: data is null, skipping initialization");
                return;
            }

            DevLog("MapInit: Getting map");

            TrackingViews.UpdateSelectedTrackers();

            Map.generateVisualContent -= _data.OnGenerateVisualContent;
            Map.generateVisualContent += _data.OnGenerateVisualContent;
        }

        private void EndTracking()
        {
            // Debug.Log("EndTracking() called");

            if (MightyCoreData.sceneData?.PlayTrackingList == null || MightyCoreData.sceneData.PlayTrackingList.Count == 0)
            {
                // Debug.LogWarning("No playthrough data available for EndTracking");
                return;
            }

            var lastPlay = MightyCoreData.sceneData.PlayTrackingList[MightyCoreData.sceneData.PlayTrackingList.Count - 1];
            // Debug.Log($"Got last playthrough with startTicks: {lastPlay.startTicks}, endTicks: {lastPlay.endTicks}");

            var trackers = TrackingViews.GetFilteredTrackers(lastPlay.startTicks, lastPlay.endTicks);
            // Debug.Log($"Retrieved {trackers.Count()} filtered trackers between timestamps");

            if (_sceneData?.transforms == null)
            {
                // Debug.LogWarning("Scene data transforms not available");
                return;
            }

            foreach (var tracker in trackers)
            {
                // Debug.Log($"Processing tracker with timestamp: {tracker.timeStamp}");

                var bytes = GetRenderTextureBytes(tracker.timeStamp);
                // Debug.Log(bytes != null
                //     ? $"Successfully retrieved {bytes.Length} bytes of render texture data"
                //     : "Failed to retrieve render texture bytes");

                // Find the index of the original tracker in the scene data
                int index = _sceneData.transforms.FindIndex(t => t.timeStamp == tracker.timeStamp && t.name == tracker.name);
                if (index >= 0)
                {
                    // Debug.Log($"Updating original tracker at index {index} with timestamp {tracker.timeStamp}");
                    var originalTracker = _sceneData.transforms[index];
                    originalTracker.renderTextureBytes = bytes;
                    _sceneData.transforms[index] = originalTracker;
                    // Debug.Log($"Updated tracker bytes length: {originalTracker.renderTextureBytes?.Length}");
                }
                else
                {
                    // Debug.LogWarning($"Could not find original tracker for {tracker.name} with timestamp {tracker.timeStamp}");
                }
            }

            // Debug.Log("EndTracking() completed");
        }

        private void DeletePlaythrough()
        {
            DevLog("DeletePlaythrough() called, received by Mighty Tracking");

            if (_sceneData?.transforms == null)
            {
                Debug.LogWarning("Scene data or transforms not available for deletion");
                return;
            }

            // Debug.Log($"Total transforms before deletion: {_sceneData.transforms.Count}");

            //find all the new transform timestamps between the min/max run and remove them
            var filteredTransforms = TrackingViews.GetFilteredTrackers(
                MightyCoreData.sceneData.RunPlaybackSelectedMin,
                MightyCoreData.sceneData.RunPlaybackSelectedMax
            );

            // Debug.Log($"Found {filteredTransforms.Count} transforms to delete between timestamps {MightyCoreData.sceneData.RunPlaybackSelectedMin} and {MightyCoreData.sceneData.RunPlaybackSelectedMax}");

            // Remove all filtered transforms from the scene data
            foreach (var transform in filteredTransforms)
            {
                // Debug.Log($"Removing transform name {transform.name} with timestamp {transform.timeStamp} from scene data");
                _sceneData.transforms.Remove(transform);
            }

            //
        }

        public class OpenGettingStartedWindowCommand : ICommand
        {
            public OpenGettingStartedWindowCommand()
            {
                // this.root = root;
            }

            public void Execute()
            {
                // Check if the window system is ready
                if (MightyCoreData.window == null)
                {
                    // Defer the window creation until the next editor update when the window system should be ready
                    EditorApplication.delayCall += Execute;
                    return;
                }

                var gettingStarted = new TrackingGettingStartedWindow();
                gettingStarted.BuildView();

                var win = new MightyWindowStateful(gettingStarted.view,
                    typeof(OpenGettingStartedWindowCommand),
                    "Tracking - Getting Started",
                    new Vector2(32, 32),
                    typeof(OpenGettingStartedWindowCommand));

                if (win.content != null)
                {
                    root.Add(win);
                }
                BuildWindowBar?.Invoke();
            }
        }


        private void BuildSceneView()
        {
            if (MightySceneViewManager.root == null)
            {
                MightySceneViewManager.Init();
            }
            var trackingOverlay = GetSceneView().rootVisualElement.Q<VisualElement>("TrackingOverlay");
            if (trackingOverlay != null)
            {
                trackingOverlay.Clear();
            }

            // Debug.Log("BuildSceneView() called");
            GetSceneDataInstance();

            // Add null checks to prevent NullReferenceException during scene switching
            if (_data == null || _data.scenes == null || _sceneIndex < 0 || _sceneIndex >= _data.scenes.Count)
            {
                DevLog("BuildSceneView: data or scene data is not ready, skipping build");
                return;
            }

            _sceneData = _data.scenes[_sceneIndex];

            // Check if MightyCoreData.sceneData is available before accessing its properties
            if (MightyCoreData.sceneData == null)
            {
                DevLog("BuildSceneView: MightyCoreData.sceneData is null, skipping build");
                return;
            }

            // Get only the transforms within the current selected playthrough timestamp range
            var filteredTransforms = TrackingViews.GetFilteredTrackers(
                MightyCoreData.sceneData.RunPlaybackSelectedMin,
                MightyCoreData.sceneData.RunPlaybackSelectedMax
            );

            // Debug.Log($"Filtered {filteredTransforms.Count} transforms from {sceneData.transforms.Count} total transforms");

            for (int i = 0; i < filteredTransforms.Count; i++)
            {
                var transform = filteredTransforms[i];

                //get the transform's render width and height, keep the same aspect ratio but no bigger than 16x16
                var renderWidth = transform.renderTextureWidth;
                var renderHeight = transform.renderTextureHeight;
                var aspectRatio = renderHeight != 0 ? (float)renderWidth / renderHeight : 1f;
                var newWidth = Mathf.Min(16, renderWidth);
                var newHeight = aspectRatio != 0 ? newWidth / aspectRatio : newWidth;

                VisualElement container = new()
                {
                    // name = $"Landmark {i}",
                    name = $"sv_{transform.timeStamp}",



                    style = {
                            position = Position.Absolute,
                            width = newWidth,
                            height = newHeight,
                            flexDirection = FlexDirection.Row,
                            alignItems = Align.Center,
                            justifyContent = Justify.Center,
                            backgroundImage = BytesToTexture2D(transform.renderTextureBytes, transform.renderTextureWidth, transform.renderTextureHeight),
                            // backgroundSize = BackgroundSize.Cover,
                            borderTopLeftRadius = 10,
                            borderTopRightRadius = 10,
                            borderBottomLeftRadius = 10,
                            borderBottomRightRadius = 10,
                            borderTopColor = transform.color,
                            borderTopWidth = 1,
                            borderRightColor = transform.color,
                            borderRightWidth = 1,
                            borderBottomColor = transform.color,
                            borderBottomWidth = 1,
                            borderLeftColor = transform.color,
                            borderLeftWidth = 1,
                            // boxShadow = new Shadow() {blurRadius = 10, color = new Color(0, 0, 0, 0.75f), offsetX = 5, offsetY = 5}
                        }
                };


                // Creating a semi-transparent overlay for text to improve readability
                VisualElement textOverlay = new()
                {

                    style = {
                                flexGrow = 1,
                                flexShrink = 1,
                                backgroundColor = new Color(0, 0, 0, 0.5f),
                                justifyContent = Justify.Center,
                                alignItems = Align.Center,
                                borderTopLeftRadius = 10,
                                borderTopRightRadius = 10,
                                borderBottomLeftRadius = 10,
                                borderBottomRightRadius = 10,
                            }
                };
                // container.Add(textOverlay);


                // Overlaying the landmark name on the image
                Label label = new()
                {
                    // text = sceneData.landmarks[i].Name,
                    name = "TextOverlay",
                    style = {
                        color = Color.white,
                        fontSize = 16,
                        unityTextAlign = TextAnchor.MiddleCenter,
                        unityFontStyleAndWeight = FontStyle.Bold,
                    }
                };
                // textOverlay.Add(label);

                container.AddToClassList("scalewithopacitytrackedrender");
                // container.Add(label);
                // container.Add(thumbnail);
                int ii = i;
                container.RegisterCallback<MouseDownEvent>(evt =>
                {
                    if (evt.button == 1) // Right click
                    {
                        // Toggle expanded state
                        if (container.ClassListContains("expanded"))
                        {
                            container.RemoveFromClassList("expanded");
                        }
                        else
                        {
                            container.AddToClassList("expanded");
                        }
                        evt.StopPropagation();
                    }
                    else if (evt.button == 0 && container.ClassListContains("expanded")) // Left click when expanded
                    {
                        if (transform.renderTextureBytes != null && transform.renderTextureBytes.Length > 0)
                        {
                            // Determine file extension based on compression format
                            string fileExtension = transform.compressionFormat == TrackingData.Tracking.ImageCompressionFormat.JPG ? ".jpg" : ".png";
                            string fileName = SanitizeFileName(transform.name) + fileExtension;
                            string filePath = System.IO.Path.Combine(Application.dataPath, fileName);

                            try
                            {
                                byte[] imageBytes;

                                // Check if we need to re-encode the image with different settings
                                if (transform.compressionFormat == TrackingData.Tracking.ImageCompressionFormat.JPG)
                                {
                                    // Convert to JPG with specified quality
                                    var texture = BytesToTexture2D(transform.renderTextureBytes, transform.renderTextureWidth, transform.renderTextureHeight);
                                    imageBytes = texture.EncodeToJPG(transform.jpgQuality);
                                    UnityEngine.Object.DestroyImmediate(texture);
                                }
                                else
                                {
                                    // Use existing PNG bytes or re-encode if compression settings changed
                                    if (transform.pngCompression)
                                    {
                                        var texture = BytesToTexture2D(transform.renderTextureBytes, transform.renderTextureWidth, transform.renderTextureHeight);
                                        imageBytes = texture.EncodeToPNG();
                                        UnityEngine.Object.DestroyImmediate(texture);
                                    }
                                    else
                                    {
                                        // Use original bytes if no re-compression needed
                                        imageBytes = transform.renderTextureBytes;
                                    }
                                }

                                // Save the image bytes to file
                                System.IO.File.WriteAllBytes(filePath, imageBytes);

                                // Refresh the asset database to show the new file in Unity
                                AssetDatabase.Refresh();

                                // Open the image with the default image editor
                                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = filePath,
                                    UseShellExecute = true
                                });
                            }
                            catch (System.Exception ex)
                            {
                                Debug.LogError($"Failed to save or open image: {ex.Message}");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("No image data available to save");
                        }
                        evt.StopPropagation();
                    }
                });



                container.tooltip = transform.name;

                if (_data?.scenes != null && _sceneIndex >= 0 && _sceneIndex < _data.scenes.Count)
                {
                    var scene = _data.scenes[_sceneIndex];
                    if (scene?.Tracking == null || scene.Tracking.Count == 0)
                        scene.Tracking.Add(new TrackingData.Tracking.Root());

                    // {
                    MightySceneViewManager.Instance.RegisterElement("TrackingOverlay", container, scene.Tracking[0], transform.position);
                    // }
                }

                MightySceneViewManager.Instance.ToggleCategory("TrackingOverlay", _data?.settings?.showTrackingCaptures ?? false);

            }

            MightySceneViewManager.Instance.ToggleCategory("TrackingOverlay", _data.settings.showTrackingCaptures);

            // MightySceneViewManager.Rebuild();
        }

        public void GetSceneDataInstance()
        {
            DevLog("TrackingCore.GetSceneData");
            _core ??= MightyCoreData.Load();
            if (_core == null) return;

            _core.CheckSceneData();
            _data ??= TrackingData.Load();
            _data.scenes ??= new();

            if (!isSceneAnchored) return;

            _sceneIndex = -1;
            for (int i = 0; i < _data.scenes.Count; i++)
            {
                DevLog($"Scene {i} is {_data.scenes[i].name}");
                DevLog($"Scene {i} is {MightyCoreData.dataSetName}");
                DevLog($"Scene {i} is {MightyCoreData.dataSetName}");
                if (_data.scenes[i].name == MightyCoreData.dataSetName) _sceneIndex = i;
            }

            if (_sceneIndex < 0)
            {
                DevLog("SceneIndex not found");
                _data.scenes.Add(new TrackingData.SceneData());
                _sceneIndex = _data.scenes.Count - 1;
                _data.scenes[_sceneIndex].name = MightyCoreData.dataSetName;
                _data.scenes[_sceneIndex].Tracking = new List<TrackingData.Tracking.Root>();
            }
            DevLog($"SceneIndex is {_sceneIndex}");
            _sceneData = _data.scenes[_sceneIndex];

            // Ensure Tracking list is initialized
            if (_sceneData.Tracking == null)
            {
                _sceneData.Tracking = new List<TrackingData.Tracking.Root>();
            }

            for (int i = 0; i < _sceneData.Tracking.Count; i++)
            {
                _sceneData.Tracking[i].RegisterMappable();
            }
        }

        static private void OnUpdate()
        {

        }

        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            // Remove invalid characters for file names
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            // Also replace some additional problematic characters
            fileName = fileName.Replace(' ', '_');
            fileName = fileName.Replace('.', '_');

            // Ensure the filename isn't empty after sanitization
            if (string.IsNullOrEmpty(fileName))
                return "unnamed";

            return fileName;
        }

        #region Static Accessors for Backward Compatibility

        /// <summary>
        /// Static accessor for data - delegates to current active instance
        /// </summary>
        public static TrackingData data
        {
            get
            {
                var module = MightyCore.GetModule("com.mighty.tracking") as TrackingCore;
                return module?._data;
            }
            set
            {
                var module = MightyCore.GetModule("com.mighty.tracking") as TrackingCore;
                if (module != null) module._data = value;
            }
        }

        /// <summary>
        /// Static accessor for core - delegates to current active instance
        /// </summary>
        public static MightyCoreData core
        {
            get
            {
                var module = MightyCore.GetModule("com.mighty.tracking") as TrackingCore;
                return module?._core;
            }
            set
            {
                var module = MightyCore.GetModule("com.mighty.tracking") as TrackingCore;
                if (module != null) module._core = value;
            }
        }

        /// <summary>
        /// Static accessor for sceneData - delegates to current active instance
        /// </summary>
        public static TrackingData.SceneData sceneData
        {
            get
            {
                var module = MightyCore.GetModule("com.mighty.tracking") as TrackingCore;
                return module?._sceneData;
            }
            set
            {
                var module = MightyCore.GetModule("com.mighty.tracking") as TrackingCore;
                if (module != null) module._sceneData = value;
            }
        }

        /// <summary>
        /// Static accessor for sceneIndex - delegates to current active instance
        /// </summary>
        public static int sceneIndex
        {
            get
            {
                var module = MightyCore.GetModule("com.mighty.tracking") as TrackingCore;
                return module?._sceneIndex ?? -1;
            }
            set
            {
                var module = MightyCore.GetModule("com.mighty.tracking") as TrackingCore;
                if (module != null) module._sceneIndex = value;
            }
        }

        /// <summary>
        /// Static accessor for trackingType - delegates to current active instance
        /// </summary>
        public static MappableTypeInfo trackingType
        {
            get
            {
                var module = MightyCore.GetModule("com.mighty.tracking") as TrackingCore;
                return module?._trackingType;
            }
            set
            {
                var module = MightyCore.GetModule("com.mighty.tracking") as TrackingCore;
                if (module != null) module._trackingType = value;
            }
        }

        /// <summary>
        /// Static method for GetSceneData - delegates to current active instance
        /// </summary>
        public static void GetSceneData()
        {
            var module = MightyCore.GetModule("com.mighty.tracking") as TrackingCore;
            module?.GetSceneDataInstance();
        }

        #endregion

        private void OnRecordPlaythroughChanged(bool enabled)
        {
            if (_sceneData != null)
            {
                _sceneData.RecordPlaythrough = enabled;
                DevLog($"TrackingCore: RecordPlaythrough {(enabled ? "enabled" : "disabled")}");
            }
        }

    }
}
#endif
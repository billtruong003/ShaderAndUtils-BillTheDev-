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
using UnityEditor.Build;

namespace MightyFPSHeatmap
{
    [InitializeOnLoad]
    [ExecuteInEditMode]
    public class FPSHeatmapCore : ModuleBase
    {
        // Module identification
        public override string ModuleId => "com.mighty.fpsheatmap";
        public override string ModuleName => "Mighty FPS Heatmap";

        // Module data - now instance fields instead of static
        private MightyCoreData _core;
        private FPSHeatmapData _data;
        private FPSHeatmapData.SceneData _sceneData;
        private int _sceneIndex = -1;
        private VisualElement sceneViewContainer;
        private MappableTypeInfo _trackingType;

        static FPSHeatmapCore()
        {
            DevLog("FPSHeatmap Core - Registering with new lifecycle system");

            // Register with new lifecycle system
            var module = new FPSHeatmapCore();
            MightyCore.RegisterModule(module);

            // Keep the old Scene GUI subscription for now since it's not part of normal lifecycle
            SceneView.duringSceneGui -= OnSceneStatic;
            SceneView.duringSceneGui += OnSceneStatic;

#if MIGHTY_FPS_HEATMAP
#else
            DevLog($"Thank you for installing Mighty FPS Heatmap!  If you enjoy, don't forget to leave a review!  Version");
            MightyFPSHeatmapDefineSymbol();
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
            _data = FPSHeatmapData.Load();

            // Ensure MappableTypeInfo exists
            var existingType = MightyCore.data.MappableTypesInfo.FirstOrDefault(types => types.Name == "FPSHeatmap");
            if (existingType == null)
            {
                var heatmapRoot = new FPSHeatmapData.HeatmapTracking.Root();
                MightyCore.data.MappableTypesInfo.Add(new MappableTypeInfo(typeof(FPSHeatmapData.HeatmapTracking.Root).FullName, "FPSHeatmap", true, heatmapRoot));
            }

            _trackingType = MightyCore.data.MappableTypesInfo.FirstOrDefault(types => types.Name == "FPSHeatmap");

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
            SafeSubscribe(ref DeletePlaythroughData, DeletePlaythrough);
            SafeSubscribe(ref UpdateMiniMap, MapInit);
            SafeSubscribe(ref MapInitialized, MapInit);
            SafeSubscribe(ref MightyCoreData.RecordPlaythroughChanged, OnRecordPlaythroughChanged);
            SafeSubscribe(ref RunPlaybackChanged, RunPlaybackChange);

            // Setup UI and components
            SetupUnityComponents();
            GetSceneData();
            if (_sceneIndex >= 0 && _data?.scenes != null)
            {
                _sceneData = _data.scenes[_sceneIndex];
            }
            FPSHeatmapViews.Rebuild();

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
                FPSHeatmapViews views = anchor.GetComponent<FPSHeatmapViews>();
                if (views == null)
                {
                    anchor.AddComponent<FPSHeatmapViews>();
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
            // Clear any scene view heatmap elements
            if (sceneViewContainer != null)
            {
                sceneViewContainer.Clear();
                sceneViewContainer = null;
            }
        }

        private void ShowGettingStartedIfFirstRun()
        {
            string firstRunKey = "MightyFPSHeatmap_FirstRun";
            if (!EditorPrefs.GetBool(firstRunKey, false))
            {
                ICommand command = new OpenGettingStartedWindowCommand();
                command.Execute();
                EditorPrefs.SetBool(firstRunKey, true);
            }
        }

        #endregion

        #region Static Methods (Legacy Support)

        /// <summary>
        /// Static scene GUI handler - kept for compatibility
        /// </summary>
        private static void OnSceneStatic(SceneView view)
        {
            // Find the active module instance and delegate to it
            var module = MightyCore.GetModule("com.mighty.fpsheatmap") as FPSHeatmapCore;
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

        #endregion

        private void DeletePlaythrough()
        {
            DevLog("DeletePlaythrough() called, received by Mighty FPS Heatmap");

            if (_sceneData?.heatmapTracking == null)
            {
                DevLog("Scene data or heatmap tracking not available for deletion");
                return;
            }

            DevLog($"Total transforms before deletion: {_sceneData.heatmapTracking.Count}");

            //find all the new transform timestamps between the min/max run and remove them
            var filteredTransforms = FPSHeatmapViews.GetFilteredHeatmapTrackers(
                MightyCoreData.sceneData.RunPlaybackSelectedMin,
                MightyCoreData.sceneData.RunPlaybackSelectedMax
            );

            foreach (var transform in filteredTransforms)
            {
                DevLog($"Removing transform name {transform.name} with timestamp {transform.timeStamp} from scene data");
                _sceneData.transforms.Remove(transform);
            }

            DevLog($"Total transforms after deletion: {_sceneData.heatmapTracking.Count}");
        }

        static void MightyFPSHeatmapDefineSymbol()
        {
#if UNITY_2023_2_OR_NEWER
            const string symbol = "MIGHTY_FPS_HEATMAP";
            NamedBuildTarget buildTarget = NamedBuildTarget.FromBuildTargetGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            string scriptingDefines = PlayerSettings.GetScriptingDefineSymbols(buildTarget);

            if (!scriptingDefines.Contains(symbol))
            {
                PlayerSettings.SetScriptingDefineSymbols(buildTarget, string.IsNullOrEmpty(scriptingDefines) ? symbol : scriptingDefines + ";" + symbol);
            }
#else
            const string symbol = "MIGHTY_FPS_HEATMAP";
            string scriptingDefines = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);

            if (!scriptingDefines.Contains(symbol))
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup, string.IsNullOrEmpty(scriptingDefines) ? symbol : scriptingDefines + ";" + symbol);
            }
#endif
        }

        private void RunPlaybackChange()
        {
            DevLog("RunPlaybackChange");
            FPSHeatmapViews.UpdateSelectedHeatmapTrackers();
        }

        private void MapInit()
        {
            DevLog("MapInit");

            if (EditorApplication.isPlaying || _trackingType?.IsActive != true || quickActions == null || quickActions.Q<Button>("HeatmapButton") != null)
            {
                return;
            }

            DevLog("MapInit: Getting map");

            Map.generateVisualContent -= _data.OnGenerateVisualContent;
            Map.generateVisualContent += _data.OnGenerateVisualContent;

            Texture2D iconHeatmapOn = Resources.Load<Texture2D>("mighty_icon_heatmap_on");
            Texture2D iconHeatmapOff = Resources.Load<Texture2D>("mighty_icon_heatmap_off");
            Texture2D iconHeatmapGizmosOn = Resources.Load<Texture2D>("mighty_icon_heatmap_gizmos_on");
            Texture2D iconHeatmapGizmosOff = Resources.Load<Texture2D>("mighty_icon_heatmap_gizmos_off");
            Texture2D iconHeatmapFollowOn = Resources.Load<Texture2D>("mighty_icon_heatmap_follow_on");
            Texture2D iconHeatmapFollowOff = Resources.Load<Texture2D>("mighty_icon_heatmap_follow_off");
            Texture2D iconHeatmapAvg = Resources.Load<Texture2D>("mighty_icon_heatmap_avg");


            VisualElement buttonContainer = new()
            {
                name = "HeatmapButtonContainer",
                style = {
                    // position = Position.Absolute,
                    // top = 16,
                    width = Length.Percent(100),
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.Center,
                    alignItems = Align.Center
                }
            };

            Button heatmapButton, gizmosButton, followButton, avgButton;

            gizmosButton = new()
            {
                name = "GizmosButton",
                tooltip = "Toggle FPS Gizmos - Show/hide FPS indicators in the scene view",
                style = {
                    width = 24,
                    height = 24,
                    backgroundImage = new StyleBackground(_data.settings.showFPSGizmos ? iconHeatmapGizmosOn : iconHeatmapGizmosOff),
                    backgroundColor = new Color(0, 0, 0, 0),
                    marginRight = 8,
                    transitionProperty = new List<StylePropertyName> { new("scale") },
                    transitionDuration = new List<TimeValue> { new(0.15f, TimeUnit.Second) },
                    transitionTimingFunction = new List<EasingFunction> { new(EasingMode.EaseInOut) }
                }
            };
            gizmosButton.RegisterCallback<MouseEnterEvent>((evt) => gizmosButton.style.scale = new Scale(new Vector2(1.1f, 1.1f)));
            gizmosButton.RegisterCallback<MouseLeaveEvent>((evt) => gizmosButton.style.scale = new Scale(Vector2.one));
            gizmosButton.clicked += () =>
            {
                _data.settings.showFPSGizmos = !_data.settings.showFPSGizmos;
                gizmosButton.style.backgroundImage = new StyleBackground(_data.settings.showFPSGizmos ? iconHeatmapGizmosOn : iconHeatmapGizmosOff);
                Dirty = true;
                RunPlaybackMinMaxUpdated?.Invoke();
                UpdateMiniMap?.Invoke();
            };

            followButton = new()
            {
                name = "FollowButton",
                tooltip = "Toggle Scene View Following - Make heatmap follow your scene view camera",
                style = {
                    width = 24,
                    height = 24,
                    backgroundImage = new StyleBackground(_data.settings.useDirectionalWeighting ? iconHeatmapFollowOn : iconHeatmapFollowOff),
                    backgroundColor = new Color(0, 0, 0, 0),
                    marginRight = 8,
                    transitionProperty = new List<StylePropertyName> { new("scale") },
                    transitionDuration = new List<TimeValue> { new(0.15f, TimeUnit.Second) },
                    transitionTimingFunction = new List<EasingFunction> { new(EasingMode.EaseInOut) }
                }
            };
            followButton.RegisterCallback<MouseEnterEvent>((evt) => followButton.style.scale = new Scale(new Vector2(1.1f, 1.1f)));
            followButton.RegisterCallback<MouseLeaveEvent>((evt) => followButton.style.scale = new Scale(Vector2.one));
            followButton.clicked += () =>
            {
                _data.settings.useDirectionalWeighting = !_data.settings.useDirectionalWeighting;
                followButton.style.backgroundImage = new StyleBackground(_data.settings.useDirectionalWeighting ? iconHeatmapFollowOn : iconHeatmapFollowOff);
                Dirty = true;
                RunPlaybackMinMaxUpdated?.Invoke();
                UpdateMiniMap?.Invoke();
            };

            heatmapButton = new()
            {
                name = "HeatmapButton",
                tooltip = "Toggle Heatmap - Show/hide the FPS heatmap overlay",
                style = {
                    width = 24,
                    height = 24,
                    backgroundImage = new StyleBackground(_data.settings.showHeatmap ? iconHeatmapOn : iconHeatmapOff),
                    backgroundColor = new Color(0, 0, 0, 0),
                    marginRight = 8,
                    transitionProperty = new List<StylePropertyName> { new("scale") },
                    transitionDuration = new List<TimeValue> { new(0.15f, TimeUnit.Second) },
                    transitionTimingFunction = new List<EasingFunction> { new(EasingMode.EaseInOut) }
                }
            };
            heatmapButton.RegisterCallback<MouseEnterEvent>((evt) => heatmapButton.style.scale = new Scale(new Vector2(1.1f, 1.1f)));
            heatmapButton.RegisterCallback<MouseLeaveEvent>((evt) => heatmapButton.style.scale = new Scale(Vector2.one));
            heatmapButton.clicked += () =>
            {
                _data.settings.showHeatmap = !_data.settings.showHeatmap;
                heatmapButton.style.backgroundImage = new StyleBackground(_data.settings.showHeatmap ? iconHeatmapOn : iconHeatmapOff);
                Dirty = true;
                RunPlaybackMinMaxUpdated?.Invoke();
                UpdateMiniMap?.Invoke();
            };

            avgButton = new()
            {
                name = "AvgButton",
                tooltip = "Set Target FPS to Median - Calculate and use the median FPS as target",
                style = {
                    width = 24,
                    height = 24,
                    backgroundImage = new StyleBackground(iconHeatmapAvg),
                    backgroundColor = new Color(0, 0, 0, 0),
                    transitionProperty = new List<StylePropertyName> { new("scale") },
                    transitionDuration = new List<TimeValue> { new(0.15f, TimeUnit.Second) },
                    transitionTimingFunction = new List<EasingFunction> { new(EasingMode.EaseInOut) }
                }
            };
            avgButton.RegisterCallback<MouseEnterEvent>((evt) => avgButton.style.scale = new Scale(new Vector2(1.1f, 1.1f)));
            avgButton.RegisterCallback<MouseLeaveEvent>((evt) => avgButton.style.scale = new Scale(Vector3.one));
            avgButton.clicked += () =>
            {
                float medianFPS = FPSHeatmapViews.CalculateMedianFPS();
                if (medianFPS > 0)
                {
                    _data.settings.targetFPS = medianFPS;
                    Dirty = true;
                    RunPlaybackMinMaxUpdated?.Invoke();
                    UpdateMiniMap?.Invoke();
                    ShowToast($"Target FPS set to {medianFPS:F1}");
                }
            };

            quickActions.Add(heatmapButton);
            quickActions.Add(gizmosButton);
            quickActions.Add(followButton);
            quickActions.Add(avgButton);

            // top.Add(buttonContainer);
            FPSHeatmapViews.UpdateSelectedHeatmapTrackers();

        }

        // Old static methods removed - functionality moved to new lifecycle methods

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

                var gettingStarted = new FPSHeatmapGettingStartedWindow();
                gettingStarted.BuildView();

                var win = new MightyWindowStateful(gettingStarted.view,
                    typeof(OpenGettingStartedWindowCommand),
                    "FPS Heatmaps - Getting Started",
                    new Vector2(32, 32),
                    typeof(OpenGettingStartedWindowCommand));

                if (win.content != null)
                {
                    root.Add(win);

                }
                BuildWindowBar?.Invoke();
            }
        }

        #region Event Handlers (converted from static methods)

        private void EditorUpdate()
        {
            if (State != ModuleLifecycleState.Started || _data == null)
                return;

            // Handle FPS heatmap-specific editor updates
            if (SceneView.lastActiveSceneView != null)
            {
                var camera = SceneView.lastActiveSceneView.camera;
                if (camera != null)
                {
                    if (camera.transform.position != lastCameraPosition ||
                        camera.transform.rotation != lastCameraRotation)
                    {
                        DevLog("Camera Moved");
                        if (_data.settings.showHeatmap && _data.settings.useDirectionalWeighting)
                        {
                            Dirty = true;
                            RunPlaybackMinMaxUpdated?.Invoke();
                            UpdateMiniMap?.Invoke();
                        }
                        lastCameraPosition = camera.transform.position;
                        lastCameraRotation = camera.transform.rotation;
                    }
                }
            }
        }

        private bool WantsToQuit()
        {
            DevLog("FPSHeatmapCore WantsToQuit");
            SaveModuleData();
            return true;
        }

        private void SceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
        {
            DevLog($"FPSHeatmapCore SceneClosing({scene.name}, {removingScene})");
            // Scene-specific cleanup handled by core system
        }

        public void GetSceneData(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            DevLog($"FPSHeatmapCore GetSceneData({scene.name}, {mode})");
            GetSceneDataInstance();
        }

        public static void GetSceneData()
        {
            var module = MightyCore.GetModule("com.mighty.fpsheatmap") as FPSHeatmapCore;
            module?.GetSceneDataInstance();
        }

        public void GetSceneDataInstance()
        {
            DevLog("FPSHeatmapCore.GetSceneData");
            _core ??= MightyCoreData.Load();
            if (_core == null) return;

            _core.CheckSceneData();
            _data ??= FPSHeatmapData.Load();
            _data.scenes ??= new();

            if (!isSceneAnchored) return;

            _sceneIndex = -1;
            for (int i = 0; i < _data.scenes.Count; i++)
            {
                DevLog($"Scene {i} is {_data.scenes[i].name}");
                DevLog($"Scene {i} is {MightyCoreData.dataSetName}");
                if (_data.scenes[i].name == MightyCoreData.dataSetName) _sceneIndex = i;
            }

            if (_sceneIndex < 0)
            {
                DevLog("SceneIndex not found");
                _data.scenes.Add(new FPSHeatmapData.SceneData());
                _sceneIndex = _data.scenes.Count - 1;
                _data.scenes[_sceneIndex].name = MightyCoreData.dataSetName;
                _data.scenes[_sceneIndex].heatmapTracking = new List<FPSHeatmapData.HeatmapTracking.Root>();
            }
            DevLog($"SceneIndex is {_sceneIndex}");
            _sceneData = _data.scenes[_sceneIndex];
            for (int i = 0; i < _sceneData.heatmapTracking.Count; i++)
            {
                _sceneData.heatmapTracking[i].RegisterMappable();
            }
        }

        #endregion

        #region Instance Variables for Camera Tracking

        private Vector3 lastCameraPosition;
        private Quaternion lastCameraRotation;

        #endregion

        #region Static Accessors for Backward Compatibility

        /// <summary>
        /// Static accessor for data - delegates to current active instance
        /// </summary>
        public static FPSHeatmapData data
        {
            get
            {
                var module = MightyCore.GetModule("com.mighty.fpsheatmap") as FPSHeatmapCore;
                return module?._data;
            }
        }

        /// <summary>
        /// Static accessor for core - delegates to current active instance
        /// </summary>
        public static MightyCoreData core
        {
            get
            {
                var module = MightyCore.GetModule("com.mighty.fpsheatmap") as FPSHeatmapCore;
                return module?._core;
            }
        }

        /// <summary>
        /// Static accessor for sceneData - delegates to current active instance
        /// </summary>
        public static FPSHeatmapData.SceneData sceneData
        {
            get
            {
                var module = MightyCore.GetModule("com.mighty.fpsheatmap") as FPSHeatmapCore;
                return module?._sceneData;
            }
        }

        /// <summary>
        /// Static accessor for sceneIndex - delegates to current active instance
        /// </summary>
        public static int sceneIndex
        {
            get
            {
                var module = MightyCore.GetModule("com.mighty.fpsheatmap") as FPSHeatmapCore;
                return module?._sceneIndex ?? -1;
            }
        }

        /// <summary>
        /// Static accessor for trackingType - delegates to current active instance
        /// </summary>
        public static MappableTypeInfo trackingType
        {
            get
            {
                var module = MightyCore.GetModule("com.mighty.fpsheatmap") as FPSHeatmapCore;
                return module?._trackingType;
            }
        }

        #endregion

        private void OnRecordPlaythroughChanged(bool enabled)
        {
            if (_sceneData != null)
            {
                _sceneData.RecordPlaythrough = enabled;
                DevLog($"FPSHeatmapCore: RecordPlaythrough {(enabled ? "enabled" : "disabled")}");
            }
        }
    }
}
#endif
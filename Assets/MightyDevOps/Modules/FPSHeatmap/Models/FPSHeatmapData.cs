#if UNITY_EDITOR
using Mighty;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static Mighty.MightyCoreData;
using static Mighty.MightyCoreData.SceneData;
using static Mighty.MightyHeatmap;
using static Mighty.MightyWindowManagerStateful;

namespace MightyFPSHeatmap
{
    public class FPSHeatmapData : ScriptableObject
    {
        // public static string Version = "1.0.0";

        public struct ConsolidatedPoint
        {
            public Vector3 pos;
            public float finalFps;
            public int polyCount;

            public ConsolidatedPoint(Vector3 position, float fps, int poly)
            {
                pos = position;
                finalFps = fps;
                polyCount = poly;
            }
        }
        public static List<ConsolidatedPoint> consolidatedPoints = new List<ConsolidatedPoint>();

        public enum FPSHeatmapMode
        {
            Origin,
            Projection,
            VisibleObjects
        }

        [Serializable]
        public class Settings
        {
            public FPSHeatmapMode fpsHeatmapMode = FPSHeatmapMode.VisibleObjects;
            public float cellsize = 3f;
            public float sigma = 1f;
            public int kernal = 5;
            public float targetFPS = 10;
            public bool relativeFPS = true, showHeatmap = true;
            public float maxProjectionDistance = 10f;
            // public float distanceDecayModifier = 1.0f;
            public int polygonThreshold = 100000;
            public float maxDeltaAngle = 45f;
            public float fustrumFOV = 60f;
            public bool useDirectionalWeighting = true;
            public int fpsMin = 1, fpsMax = 150;
            public int polyMin = 1, polyMax = 100;
            public Color lowFPSColor = Color.red;
            public Color highFPSColor = Color.green;
            public float opacityFPSHeatmap = 0.5f;

            [SerializeField]
            public bool showFPSGizmos = true;

            public float SvFadeDistance
            {
                get { return svFadeDistance; }
                set { svFadeDistance = value; }
            }

            public AnimationCurve SvOpacityCurve
            {
                get { return svOpacityCurve; }
                set { svOpacityCurve = value; }
            }

            private float svFadeDistance = 50f;
            private AnimationCurve svOpacityCurve = AnimationCurve.Linear(0, 1, 1, 0);
            public MightySceneViewManager.Settings sceneView = new MightySceneViewManager.Settings();

            [SerializeField]
            public Color borderColor = new Color(0, 0, 0, 0);
            [SerializeField]
            public float distanceStart = 5, distanceEnd = 100;
            [SerializeField]
            public bool show = true;
            [SerializeField]
            public AggregationMethod aggregationMethod = AggregationMethod.Average;
        }
        public static void Save()
        {
            string path = $"{corePath}/Modules/FPSHeatmap/Data/HeatmapData.asset";
            if (File.Exists(path))
            {
                DevLog($"{path} already exists...");
                return;
            }

            FPSHeatmapData asset = ScriptableObject.CreateInstance<FPSHeatmapData>();

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }

        public static FPSHeatmapData Load()
        {
            string path = $"{corePath}/Modules/FPSHeatmap/Data/HeatmapData.asset";
            // string fallbackPath = $"{corePath}/Modules/FPSHeatmap/Data/HeatmapData_.asset";

            if (!File.Exists(path))
            {
                // if (File.Exists(fallbackPath))
                // {
                //     DevLog($"Renaming {fallbackPath} to {path}");
                //     File.Copy(fallbackPath, path);
                //     AssetDatabase.Refresh();
                // }
                // else
                // {
                Save();
                // }
            }

            return AssetDatabase.LoadAssetAtPath<FPSHeatmapData>(path);
        }

        [SerializeField]
        public Settings settings = new Settings();
        [SerializeField]
        public List<SceneData> scenes;
        private static Texture2D thumbnail;

        public Dictionary<string, Heatmap> heatmaps = new Dictionary<string, Heatmap>();

        static public Heatmap fpsHeatMap;

        [Serializable]
        public class VisibilityData
        {
            public float cellSize;
            public int width, height, depth;
            public Vector3 origin;
            public Vector3[] directions;
            public VisibilityCell[] cells;
        }

        [Serializable]
        public class VisibilityCell
        {
            public float[] depths;
        }

        public class MeshData
        {
            public int totalPolygons;
            public int totalVertices;

            public MeshData(int polygons, int vertices)
            {
                totalPolygons = polygons;
                totalVertices = vertices;
            }
        }

        public static Dictionary<Transform, WeakReference<MeshData>> meshDataCache = new Dictionary<Transform, WeakReference<MeshData>>();

        [Serializable]
        public class SceneData
        {
            public string name;
            [SerializeField]
            public List<HeatmapTracking.HeatmapTracker> transforms;
            [SerializeField]
            public List<HeatmapTracking.Root> heatmapTracking;
            [SerializeField]
            public VisibilityData visibilityData;

            public bool RecordPlaythrough = false;
        }

        public int GetSceneIndex()
        {
            if (dataSetName == null) return -1;
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].name == dataSetName)
                {
                    return i;
                }
            }
            return -1;
        }

        public SceneData GetSceneData()
        {
            if (dataSetName == null) return null;
            return scenes.FirstOrDefault(x => x.name == dataSetName);
        }



        [Serializable]
        public enum FPSCategory
        {
            Excellent,
            Good,
            Fair,
            Poor,
            Stall
        }

        public void OnGenerateVisualContent(MeshGenerationContext mgc)
        {


        }

        [Serializable]
        public class HeatmapTracking
        {
            [System.Flags]
            public enum TriggerReason
            {
                None = 0,
                FPSDrop = 1 << 0,
                MemorySpike = 1 << 1,
            }
            [Serializable]
            public struct HeatmapTracker
            {
                [SerializeField]
                public string name;

                [SerializeField]
                public long timeStamp;
                [SerializeField]
                public Vector3 position;
                [SerializeField]
                public Quaternion rotation;
                [SerializeField]
                public Vector3 scale;

                [SerializeField]
                public float fps;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                [SerializeField]
                public long totalAllocatedMemory;
                [SerializeField]
                public long totalReservedMemory;
                [SerializeField]
                public long totalUnusedReservedMemory;
                [SerializeField]
                public long monoHeapSize;
                [SerializeField]
                public long monoUsedSize;
                [SerializeField]
                public long managedMemoryUsage;

                [SerializeField]
                public int drawCalls;
                [SerializeField]
                public int visibleObjectsCount;

                [SerializeField]
                public long totalPolygons;
                [SerializeField]
                public long totalVertices;
                [SerializeField]
                public long totalMaterials;
                [SerializeField]
                public float recordTimeMs;

                [SerializeField]
                public List<Vector3> visibleObjectPositions;
                [SerializeField]
                public List<int> visibleObjectPolyCounts;
                [SerializeField]
                public List<int> visibleObjectLODLevels;

                [SerializeField]
                public int objectsAboveThreshold;
#endif

                [SerializeField]
                public float fieldOfView;

                [SerializeField]
                public TriggerReason triggerReason;

                public HeatmapTracker(
                string n,
                long timeStamp,
                Vector3 pos,
                Quaternion rot,
                Vector3 scl,
                float f,
                float fov,
                TriggerReason reason,
                long totalAllocated = 0,
                long totalReserved = 0,
                long totalUnused = 0,
                long monoHeap = 0,
                long monoUsed = 0,
                long managedMem = 0,
                int drawCalls = 0,
                int visObjs = 0,
                long totalPolys = 0,
                long totalVerts = 0,
                long totalMats = 0,
                float recordTimeMs = 0,
                List<Vector3> visibleObjectPositions = null,
                List<int> visibleObjectPolyCounts = null,
                List<int> visibleObjectLODLevels = null,
                int objectsAboveThreshold = 0
)
                {
                    name = n;
                    this.timeStamp = timeStamp;
                    position = pos;
                    rotation = rot;
                    scale = scl;
                    fps = f;

                    fieldOfView = fov;

                    triggerReason = reason;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    totalAllocatedMemory = totalAllocated;
                    totalReservedMemory = totalReserved;
                    totalUnusedReservedMemory = totalUnused;
                    monoHeapSize = monoHeap;
                    monoUsedSize = monoUsed;
                    managedMemoryUsage = managedMem;
                    this.drawCalls = drawCalls;
                    visibleObjectsCount = visObjs;

                    totalPolygons = totalPolys;
                    totalVertices = totalVerts;
                    totalMaterials = totalMats;
                    this.recordTimeMs = recordTimeMs;

                    this.visibleObjectPositions = visibleObjectPositions ?? new List<Vector3>();
                    this.visibleObjectPolyCounts = visibleObjectPolyCounts ?? new List<int>();
                    this.visibleObjectLODLevels = visibleObjectLODLevels ?? new List<int>();
                    this.objectsAboveThreshold = objectsAboveThreshold;
#endif
                }
            }
            [Serializable]
            public class Positional
            {
                [SerializeField]
                public string name;

                [SerializeField]
                public long timeStamp;
                [SerializeField]
                public Vector3 position;
                [SerializeField]
                public Quaternion rotation;
                [SerializeField]
                public Vector3 scale;
            }

            [Serializable]
            public class TransformData
            {
                [SerializeField]
                public string name;

                public List<Positional> positional;

                public TransformData(string n, Color color)
                {
                    name = $"{n}|{sceneData.RunID}";
                    positional = new List<Positional>();
                }

                public void AddPosition(Transform transform, Color color)
                {
                    if (sceneData.RunID == "")
                    {
                        DevLog("No run_id");
                        return;
                    }

                    positional.Add(new Positional()
                    {
                        name = "Heatmap taken on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        timeStamp = DateTime.Now.Ticks,
                        position = transform.position,
                        rotation = transform.rotation,
                        scale = transform.localScale
                    });
                }

            }

            [Serializable]
            public class SceneViewLabel
            {
                public bool fade;
                public bool show;
                public Vector3 offset;
                public int fadeMax;
                public int fadeMin;
            }

            [Serializable]
            public class Root : IMappable
            {
                private string _version = "1.1.0";
                public string Version
                {
                    get => _version;
                    set => _version = value;
                }

                private string _packageName = "Mighty Heatmaps";
                public string PackageName
                {
                    get => _packageName;
                    set => _packageName = value;
                }

                public bool UpdateAvailable()
                {
                    // var updateInfo = dataCore.moduleUpdates.FirstOrDefault(update => update.module == "heatmaps");
                    // if (updateInfo != null)
                    // {
                    //     return updateInfo.version != Version;
                    // }
                    return false;
                }

                public void InitViews()
                {
                }

                public void RegisterMappable()
                {
                    if (mappables.Contains(this)) return;
                    if (sceneData == null) FPSHeatmapCore.GetSceneData();
                    if (AnchorTo == sceneData.Name)
                    {
                        PlayTracking playthrough = sceneData.PlayTrackingList.FirstOrDefault(x => x.name == sceneData.SelectedRun);
                        if (playthrough == null) return;

                        mappables.Add(this);
                        InitViews();
                    }
                    else
                    {
                        // DevLog($"Not Adding Mappable: {this.Name} as it is anchored to {AnchorTo} and not {sceneData.Name}");
                    }
                }

                public void OnGenerateVisualContent(MeshGenerationContext mgc)
                {
                }

                public void CheckIntegrity()
                {
                }

                public void LoadImage()
                {
                }

                public void PopulatePlayTrackingLane(int laneIndex)
                {
                }

                public Button AddMappable(bool setClickedCallback = true)
                {
                    return null;
                }

                public CustomToggleButton AddModuleToggle(MappableTypeInfo mappableTypeInfo)
                {
                    DevLog($"AddModuleToggle named {mappableTypeInfo.Name}");
                    return new(Icon, mappableTypeInfo, "FPSHeatmapOverlay");
                }

                public VisualElement SceneSummary(MightyCoreData.SceneData scene)
                {
                    return new VisualElement();
                }

                public VisualElement SettingsView()
                {
                    VisualElement settingsView = new VisualElement()
                    {
                        name = "HeatmapSettingsView",
                        style = {
                            flexDirection = FlexDirection.Column,
                            width = Length.Percent(100),
                            height = Length.Percent(100),
                            flexGrow = 1,
                            backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f, 1f)),
                            paddingLeft = 5,
                            paddingRight = 5,
                        }
                    };



                    // Add centralized feedback section
                    var feedbackSection = CreateFeedbackSection(
                        "FPS Heatmap",
                        "https://prf.hn/click/camref:1011lf9gY/pubref:editor/ar:internal/destination:https%3A%2F%2Fassetstore.unity.com%2Fpackages%2Fslug%2F319014",
                        "https://github.com/ShrinkRayEntertainment/Mighty-FPS-Heatmaps/issues/new?template=%F0%9F%90%9B-bug-report.md&labels=bug&title=[{VERSION}%20|%20{UNITY_VERSION}]%20Your%20Title",
                        "https://github.com/ShrinkRayEntertainment/Mighty-FPS-Heatmaps/issues/new?template=%E2%9C%A8-feature-request.md&labels=enhancement&title=[{VERSION}%20|%20{UNITY_VERSION}]%20Your%20Title",
                        Version
                    );
                    settingsView.Add(feedbackSection);

                    // Getting Started button
                    Button gettingStartedButton = new Button(() =>
                    {
                        ICommand command = new FPSHeatmapCore.OpenGettingStartedWindowCommand();
                        command.Execute();
                    })
                    {
                        text = "Getting Started",
                        style = {
                            height = 32,
                            backgroundColor = new Color(0.3f, 0.6f, 0.9f, 1f),
                            color = Color.white,
                            borderTopLeftRadius = 6,
                            borderTopRightRadius = 6,
                            borderBottomLeftRadius = 6,
                            borderBottomRightRadius = 6,
                            borderTopWidth = 0,
                            borderBottomWidth = 0,
                            borderLeftWidth = 0,
                            borderRightWidth = 0,
                            fontSize = 12,
                            unityFontStyleAndWeight = FontStyle.Bold,
                            marginTop = 10,
                            marginBottom = 10,
                        }
                    };
                    settingsView.Add(gettingStartedButton);

                    settingsView.Add(CreateSectionHeader("FPS Heatmap Settings", "Configure FPS visualization", new Color(0.4f, 0.8f, 1f, 1f)));

                    VisualElement CreateSectionHeader(string title, string tooltip, Color accentColor, bool showline = true)
                    {
                        var c = new Color(0.3f, 0.3f, 0.3f, 1f);

                        var header = new VisualElement()
                        {
                            style = {
                                flexDirection = FlexDirection.Row,
                                alignItems = Align.Center,
                                marginTop = 15,
                                marginBottom = 10,
                                borderTopWidth = 1,
                                paddingTop = 10,
                            }
                        };

                        if (showline)
                        {
                            header.style.borderTopColor = new StyleColor(c);
                        }

                        var titleLabel = new Label(title)
                        {
                            tooltip = tooltip,
                            style = {
                                unityFontStyleAndWeight = FontStyle.Bold,
                                fontSize = 13,
                                color = new StyleColor(accentColor),
                                marginLeft = 5,
                            }
                        };
                        header.Add(titleLabel);
                        return header;
                    }

                    VisualElement CreateInfoBox(string text, string tooltip)
                    {
                        var infoBox = new VisualElement()
                        {
                            style = {
                                backgroundColor = new StyleColor(new Color(0.25f, 0.25f, 0.25f, 1f)),
                                paddingLeft = 8,
                                paddingRight = 8,
                                paddingTop = 8,
                                paddingBottom = 8,
                                marginBottom = 10,
                                borderTopLeftRadius = 4,
                                borderTopRightRadius = 4,
                                borderBottomLeftRadius = 4,
                                borderBottomRightRadius = 4,
                            }
                        };

                        var label = new Label(text)
                        {
                            tooltip = tooltip,
                            style = {
                                unityFontStyleAndWeight = FontStyle.Italic,
                                fontSize = 11,
                                whiteSpace = WhiteSpace.Normal,
                                color = new StyleColor(new Color(0.8f, 0.8f, 0.8f, 1f)),
                            }
                        };
                        infoBox.Add(label);
                        return infoBox;
                    }

                    VisualElement heatmapSettingsContainer = new VisualElement();

                    Toggle showHeatmapToggle = new Toggle("Show Heatmap")
                    {
                        name = "HeatmapToggle",
                        value = FPSHeatmapCore.data.settings.showHeatmap,
                        tooltip = "Toggle the visibility of the FPS Heatmap overlay",
                        style = { marginBottom = 10, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 12 }
                    };

                    Toggle showFPSGizmosToggle = new Toggle("Show FPS Gizmos")
                    {
                        name = "ShowFPSGizmosToggle",
                        value = FPSHeatmapCore.data.settings.showFPSGizmos,
                        tooltip = "Toggle visibility of FPS indicators in the Scene view",
                        style = {
                            marginBottom = 10,
                            unityFontStyleAndWeight = FontStyle.Bold,
                            fontSize = 12
                        }
                    };


                    settingsView.Add(FloatSliderWithField("Max Gizmo Distance", "Lower this if you are experience slowdowns", "FPSHeatmapSvFadeDistanceSlider", FPSHeatmapCore.data.settings.SvFadeDistance, 1f, 200f, newValue =>
                    {
                        FPSHeatmapCore.data.settings.SvFadeDistance = newValue;
                    }, 10f));

                    Label opacityCurveLabel = new Label("Opacity")
                    {
                        tooltip = "Control how quickly trails fade out",
                        style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5, marginTop = 10, fontSize = 12 }
                    };
                    settingsView.Add(opacityCurveLabel);

                    CurveField opacityCurveField = new CurveField()
                    {
                        name = "FPSHeatmapSvOpacityCurve",
                        value = FPSHeatmapCore.data.settings.SvOpacityCurve,
                        tooltip = "Adjust the fade-out curve of the trails",
                        style = { marginBottom = 10, width = 150 }
                    };
                    opacityCurveField.RegisterValueChangedCallback(evt =>
                    {
                        FPSHeatmapCore.data.settings.SvOpacityCurve = evt.newValue;
                    });
                    settingsView.Add(opacityCurveField);

                    Toggle followSceneViewToggle = new Toggle("Follow Scene View Angle")
                    {
                        name = "HeatmapFollowSceneViewToggle",
                        value = FPSHeatmapCore.data.settings.useDirectionalWeighting,
                        tooltip = "Make the heatmap angle follow the Scene view camera",
                        style = { marginBottom = 10, unityFontStyleAndWeight = FontStyle.Bold, fontSize = 12 }
                    };

                    showHeatmapToggle.RegisterValueChangedCallback(evt =>
                    {
                        FPSHeatmapCore.data.settings.showHeatmap = evt.newValue;
                        heatmapSettingsContainer.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                        showFPSGizmosToggle.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                        followSceneViewToggle.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
                        MightyCoreData.Dirty = true;
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                    });

                    showFPSGizmosToggle.RegisterValueChangedCallback(evt =>
                    {
                        FPSHeatmapCore.data.settings.showFPSGizmos = evt.newValue;
                        MightyCoreData.Dirty = true;
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                    });

                    followSceneViewToggle.RegisterValueChangedCallback(evt =>
                    {
                        FPSHeatmapCore.data.settings.useDirectionalWeighting = evt.newValue;
                        consolidatedPoints = new List<ConsolidatedPoint>();
                        MightyCoreData.Dirty = true;
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                    });

                    settingsView.Add(showHeatmapToggle);
                    settingsView.Add(showFPSGizmosToggle);
                    settingsView.Add(followSceneViewToggle);


                    Label modeDescription = new Label("How should FPS be visualized?")
                    {
                        style = {
                            unityFontStyleAndWeight = FontStyle.Italic,
                            fontSize = 11,
                            marginBottom = 5,
                            color = new StyleColor(new Color(0.7f, 0.7f, 0.7f, 1f)),
                        }
                    };
                    heatmapSettingsContainer.Add(modeDescription);
                    VisualElement modeSettingsContainer = new VisualElement()
                    {
                        style = {
                            flexDirection = FlexDirection.Column,
                            marginLeft = 5,
                            marginRight = 5,
                        }
                    };
                    EnumField heatmapModeDropdown = new EnumField(FPSHeatmapCore.data.settings.fpsHeatmapMode)
                    {
                        name = "FPSHeatmapModeDropdown",
                        tooltip = "Select the visualization mode for the FPS heatmap",
                        style = { marginBottom = 10, fontSize = 12, width = 150 }
                    };
                    heatmapModeDropdown.RegisterValueChangedCallback(evt =>
                    {
                        FPSHeatmapCore.data.settings.fpsHeatmapMode = (FPSHeatmapMode)evt.newValue;
                        MightyCoreData.Dirty = true;
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                        UpdateModeContainersVisibility(modeSettingsContainer);
                    });
                    heatmapSettingsContainer.Add(heatmapModeDropdown);



                    void UpdateModeContainersVisibility(VisualElement container)
                    {
                        container.Clear();

                        switch (FPSHeatmapCore.data.settings.fpsHeatmapMode)
                        {
                            case FPSHeatmapMode.Origin:
                                container.Add(new Label("Origin Mode")
                                {
                                    style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 }
                                });
                                container.Add(new Label("Uses the player's position as the center for FPS heatmap.")
                                {
                                    style = { unityFontStyleAndWeight = FontStyle.Italic, marginBottom = 10, fontSize = 11, whiteSpace = WhiteSpace.Normal }
                                });

                                break;

                            case FPSHeatmapMode.Projection:
                                container.Add(new Label("Projection Mode")
                                {
                                    style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 }
                                });
                                container.Add(new Label("Projects FPS forward from the player's view, factoring in distance.")
                                {
                                    style = { unityFontStyleAndWeight = FontStyle.Italic, marginBottom = 10, fontSize = 11, whiteSpace = WhiteSpace.Normal }
                                });
                                container.Add(CreateFloatSliderWithField("Projection Distance", "How far ahead to project FPS data", "HeatmapMaxProjectionDistanceSlider", FPSHeatmapCore.data.settings.maxProjectionDistance, 1f, 50f, newValue =>
                                {
                                    FPSHeatmapCore.data.settings.maxProjectionDistance = newValue;
                                    MightyCoreData.Dirty = true;
                                    RunPlaybackMinMaxUpdated?.Invoke();
                                    UpdateMiniMap?.Invoke();
                                }));
                                // container.Add(CreateFloatSliderWithField("Decay", "How quickly FPS impact fades with distance", "HeatmapDistanceDecayModifierSlider", FPSHeatmapCore.data.settings.distanceDecayModifier, 0.5f, 4.0f, newValue =>
                                // {
                                //     FPSHeatmapCore.data.settings.distanceDecayModifier = newValue;
                                //     MightyCoreData.Dirty = true;
                                //     RunPlaybackMinMaxUpdated?.Invoke();
                                //     UpdateMiniMap?.Invoke();
                                // }));

                                break;

                            case FPSHeatmapMode.VisibleObjects:
                                container.Add(new Label("Visible Objects Mode")
                                {
                                    style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5 }
                                });
                                container.Add(new Label("Applies FPS directly to the positions of visible objects.")
                                {
                                    style = { unityFontStyleAndWeight = FontStyle.Italic, marginBottom = 10, fontSize = 11, whiteSpace = WhiteSpace.Normal }
                                });

                                break;
                        }
                    }

                    UpdateModeContainersVisibility(modeSettingsContainer);
                    heatmapSettingsContainer.Add(modeSettingsContainer);

                    heatmapSettingsContainer.Add(CreateSectionHeader("Ranges", "Set visualization thresholds", new Color(1f, 0.8f, 0.4f, 1f)));
                    heatmapSettingsContainer.Add(CreateInfoBox("Define the ranges for FPS and polygon visualization", "Ranges explanation"));

                    VisualElement fpsRangeContainer = new VisualElement()
                    {
                        style = { flexDirection = FlexDirection.Column, marginBottom = 10 }
                    };

                    Label fpsRangeLabel = new Label("FPS Range")
                    {
                        style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5, fontSize = 12 }
                    };
                    fpsRangeContainer.Add(fpsRangeLabel);

                    VisualElement sliderRow = new VisualElement()
                    {
                        style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }
                    };
                    MinMaxSlider fpsRangeSlider = new MinMaxSlider("", FPSHeatmapCore.data.settings.fpsMin, FPSHeatmapCore.data.settings.fpsMax, 1, 150)
                    {
                        style = { flexGrow = 1, height = 16 }
                    };
                    FloatField minField = new FloatField()
                    {
                        value = FPSHeatmapCore.data.settings.fpsMin,
                        style = { width = 40, marginRight = 5, fontSize = 12 }
                    };
                    FloatField maxField = new FloatField()
                    {
                        value = FPSHeatmapCore.data.settings.fpsMax,
                        style = { width = 40, marginLeft = 5, fontSize = 12 }
                    };

                    fpsRangeSlider.RegisterValueChangedCallback(evt =>
                    {
                        FPSHeatmapCore.data.settings.fpsMin = (int)evt.newValue.x;
                        FPSHeatmapCore.data.settings.fpsMax = (int)evt.newValue.y;
                        minField.SetValueWithoutNotify(evt.newValue.x);
                        maxField.SetValueWithoutNotify(evt.newValue.y);
                        Debug.Log($"Dirty current value: {MightyCoreData.Dirty}");
                        MightyCoreData.Dirty = true;
                        Debug.Log($"Dirty new value: {MightyCoreData.Dirty} and IsDirty: {MightyCoreData.IsDirty(true)}");
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                        Debug.Log($"Dirty after UpdateMiniMap: {MightyCoreData.Dirty}");
                        Debug.Log("FPS Range Slider Changed");
                    });

                    minField.RegisterValueChangedCallback(evt =>
                    {
                        FPSHeatmapCore.data.settings.fpsMin = (int)evt.newValue;
                        fpsRangeSlider.minValue = evt.newValue;
                        MightyCoreData.Dirty = true;
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                    });

                    maxField.RegisterValueChangedCallback(evt =>
                    {
                        FPSHeatmapCore.data.settings.fpsMax = (int)evt.newValue;
                        fpsRangeSlider.maxValue = evt.newValue;
                        MightyCoreData.Dirty = true;
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                    });

                    sliderRow.Add(minField);
                    sliderRow.Add(fpsRangeSlider);
                    sliderRow.Add(maxField);
                    fpsRangeContainer.Add(sliderRow);
                    heatmapSettingsContainer.Add(fpsRangeContainer);

                    VisualElement polyRangeContainer = new VisualElement()
                    {
                        style = { flexDirection = FlexDirection.Column, marginBottom = 10 }
                    };

                    Label polyRangeLabel = new Label("Polygon Range")
                    {
                        style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5, fontSize = 12 }
                    };
                    polyRangeContainer.Add(polyRangeLabel);

                    VisualElement polySliderRow = new VisualElement()
                    {
                        style = { flexDirection = FlexDirection.Row, alignItems = Align.Center }
                    };
                    MinMaxSlider polyRangeSlider = new MinMaxSlider("", FPSHeatmapCore.data.settings.polyMin, FPSHeatmapCore.data.settings.polyMax, 0, 100000)
                    {
                        style = { flexGrow = 1, height = 16 }
                    };
                    IntegerField polyMinField = new IntegerField()
                    {
                        value = (int)FPSHeatmapCore.data.settings.polyMin,
                        style = { width = 40, marginRight = 5, fontSize = 12 }
                    };
                    IntegerField polyMaxField = new IntegerField()
                    {
                        value = (int)FPSHeatmapCore.data.settings.polyMax,
                        style = { width = 40, marginLeft = 5, fontSize = 12 }
                    };
                    polyRangeSlider.RegisterValueChangedCallback(evt =>
                    {
                        FPSHeatmapCore.data.settings.polyMin = (int)evt.newValue.x;
                        FPSHeatmapCore.data.settings.polyMax = (int)evt.newValue.y;
                        polyMinField.SetValueWithoutNotify((int)evt.newValue.x);
                        polyMaxField.SetValueWithoutNotify((int)evt.newValue.y);
                        MightyCoreData.Dirty = true;
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                    });

                    polyMinField.RegisterValueChangedCallback(evt =>
                    {
                        FPSHeatmapCore.data.settings.polyMin = evt.newValue;
                        polyRangeSlider.minValue = evt.newValue;
                        MightyCoreData.Dirty = true;
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                    });

                    polyMaxField.RegisterValueChangedCallback(evt =>
                    {
                        FPSHeatmapCore.data.settings.polyMax = evt.newValue;
                        polyRangeSlider.maxValue = evt.newValue;
                        MightyCoreData.Dirty = true;
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                    });

                    polySliderRow.Add(polyMinField);
                    polySliderRow.Add(polyRangeSlider);
                    polySliderRow.Add(polyMaxField);
                    polyRangeContainer.Add(polySliderRow);
                    heatmapSettingsContainer.Add(polyRangeContainer);

                    heatmapSettingsContainer.Add(CreateSectionHeader("Performance Scale", "Configure FPS scaling and colors", new Color(1f, 0.6f, 0.4f, 1f)));

                    Button setMedianFPSButton = new Button(() =>
                    {
                        float medianFPS = FPSHeatmapViews.CalculateMedianFPS();
                        if (medianFPS > 0)
                        {
                            FPSHeatmapCore.data.settings.targetFPS = medianFPS;
                            MightyCoreData.Dirty = true;
                            RunPlaybackMinMaxUpdated?.Invoke();
                            UpdateMiniMap?.Invoke();

                            var targetFPSField = settingsView.Q<FloatField>("HeatmapTargetFPSField");
                            var targetFPSSlider = settingsView.Q<Slider>("HeatmapTargetFPSField");
                            if (targetFPSField != null)
                            {
                                targetFPSField.SetValueWithoutNotify(Mathf.Round(medianFPS * 10f) / 10f);
                                targetFPSField.MarkDirtyRepaint();
                            }
                            if (targetFPSSlider != null)
                            {
                                targetFPSSlider.SetValueWithoutNotify(medianFPS);
                                targetFPSSlider.MarkDirtyRepaint();
                            }

                            ShowToast($"Target FPS set to {medianFPS:F1}");
                        }
                    })
                    {
                        text = "Set Target FPS to Median",
                        tooltip = "Set the target FPS to the median value of the current dataset",
                        style = {
                            marginBottom = 10,
                            unityFontStyleAndWeight = FontStyle.Bold,
                            fontSize = 12,
                            backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f)),
                            borderTopColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderLeftColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderRightColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderTopWidth = 1,
                            borderBottomWidth = 1,
                            borderLeftWidth = 1,
                            borderRightWidth = 1,
                            paddingTop = 5,
                            paddingBottom = 5,
                            paddingLeft = 10,
                            paddingRight = 10,
                        }
                    };
                    heatmapSettingsContainer.Add(setMedianFPSButton);

                    VisualElement targetFPSField = CreateFloatSliderWithField("Target FPS", "Set the target FPS for scaling", "HeatmapTargetFPSField", FPSHeatmapCore.data.settings.targetFPS, 1.0f, 90.0f, newValue =>
                    {
                        FPSHeatmapCore.data.settings.targetFPS = Mathf.Round(newValue * 10f) / 10f;
                        MightyCoreData.Dirty = true;
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                    });
                    heatmapSettingsContainer.Add(targetFPSField);

                    VisualElement colorContainer = new VisualElement()
                    {
                        style = {
                            flexDirection = FlexDirection.Column,
                            marginBottom = 10,
                        }
                    };

                    ColorField lowFPSColorField = new ColorField()
                    {
                        value = FPSHeatmapCore.data.settings.lowFPSColor,
                        style = { marginBottom = 5 }
                    };
                    lowFPSColorField.RegisterValueChangedCallback(evt =>
                    {
                        FPSHeatmapCore.data.settings.lowFPSColor = evt.newValue;
                        MightyCoreData.Dirty = true;
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                    });

                    ColorField highFPSColorField = new ColorField()
                    {
                        value = FPSHeatmapCore.data.settings.highFPSColor,
                        style = { marginBottom = 5 }
                    };
                    highFPSColorField.RegisterValueChangedCallback(evt =>
                    {
                        FPSHeatmapCore.data.settings.highFPSColor = evt.newValue;
                        MightyCoreData.Dirty = true;
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                    });

                    colorContainer.Add(new Label("Low FPS") { style = { fontSize = 11, marginBottom = 2 } });
                    colorContainer.Add(lowFPSColorField);
                    colorContainer.Add(new Label("High FPS") { style = { fontSize = 11, marginBottom = 2 } });
                    colorContainer.Add(highFPSColorField);
                    heatmapSettingsContainer.Add(colorContainer);

                    settingsView.Add(heatmapSettingsContainer);

                    return settingsView;
                }
                public static VisualElement CreateFloatSliderWithField(string labelText, string tooltip, string name, float value, float lowValue, float highValue, Action<float> onValueChanged, float stepSize = 0.1f)
                {
                    VisualElement container = new VisualElement();
                    container.style.flexDirection = FlexDirection.Column;
                    container.style.marginBottom = 10;

                    Label label = new Label(labelText)
                    {
                        tooltip = tooltip,
                        style = {
                            unityFontStyleAndWeight = FontStyle.Bold,
                            marginBottom = 2,
                            fontSize = 12,
                        }
                    };
                    container.Add(label);

                    VisualElement sliderContainer = new VisualElement()
                    {
                        style = {
                            flexDirection = FlexDirection.Row,
                            alignItems = Align.Center,
                            width = 180,
                        }
                    };

                    Slider slider = new Slider(lowValue, highValue)
                    {
                        name = name,
                        value = value,
                        tooltip = tooltip,
                        style = {
                            flexGrow = 1,
                            height = 16,
                            width = 80,
                        }
                    };

                    FloatField valueField = new FloatField()
                    {
                        name = name,
                        value = value,
                        style = {
                            width = 48,
                            marginLeft = 5,
                            fontSize = 12,
                        },
                        tooltip = tooltip
                    };

                    Button decrementButton = new Button(() =>
                    {
                        float currentValue = slider.value;
                        float newValue = Mathf.Clamp(currentValue - stepSize, lowValue, highValue);
                        if (newValue != currentValue)
                        {
                            slider.SetValueWithoutNotify(newValue);
                            valueField.SetValueWithoutNotify(newValue);
                            onValueChanged?.Invoke(newValue);
                        }
                    })
                    {
                        text = "−",
                        style = {
                            width = 20,
                            height = 20,
                            marginRight = 2,
                            paddingLeft = 0,
                            paddingRight = 0,
                            paddingTop = 0,
                            paddingBottom = 0,
                            fontSize = 12,
                            backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f)),
                            borderLeftColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderRightColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderTopColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                        }
                    };
                    sliderContainer.Add(decrementButton);

                    Button incrementButton = new Button(() =>
                    {
                        float currentValue = slider.value;
                        float newValue = Mathf.Clamp(currentValue + stepSize, lowValue, highValue);
                        if (newValue != currentValue)
                        {
                            slider.SetValueWithoutNotify(newValue);
                            valueField.SetValueWithoutNotify(newValue);
                            onValueChanged?.Invoke(newValue);
                        }
                    })
                    {
                        text = "+",
                        style = {
                            width = 20,
                            height = 20,
                            marginLeft = 2,
                            paddingTop = 0,
                            paddingRight = 0,
                            paddingBottom = 0,
                            paddingLeft = 0,
                            fontSize = 12,
                            backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f)),
                            borderLeftColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderRightColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderTopColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                        }
                    };

                    slider.RegisterValueChangedCallback(evt =>
                    {
                        float newValue = Mathf.Round(evt.newValue * 10f) / 10f;
                        slider.SetValueWithoutNotify(newValue);
                        valueField.SetValueWithoutNotify(newValue);
                        valueField.MarkDirtyRepaint();
                        slider.MarkDirtyRepaint();
                        onValueChanged?.Invoke(newValue);
                    });
                    valueField.RegisterValueChangedCallback(evt =>
                    {
                        float clampedValue = Mathf.Clamp(evt.newValue, lowValue, highValue);
                        slider.SetValueWithoutNotify(clampedValue);
                        valueField.SetValueWithoutNotify(clampedValue);
                        valueField.MarkDirtyRepaint();
                        slider.MarkDirtyRepaint();
                        onValueChanged?.Invoke(clampedValue);
                    });

                    sliderContainer.Add(slider);
                    sliderContainer.Add(incrementButton);
                    sliderContainer.Add(valueField);

                    container.Add(sliderContainer);

                    return container;
                }

                public static VisualElement CreateIntSliderWithField(string labelText, string tooltip, string name, int value, int lowValue, int highValue, Action<int> onValueChanged)
                {
                    VisualElement container = new VisualElement();
                    container.style.flexDirection = FlexDirection.Column;
                    container.style.marginBottom = 10;

                    Label label = new Label(labelText)
                    {
                        tooltip = tooltip,
                        style = {
                            unityFontStyleAndWeight = FontStyle.Bold,
                            marginBottom = 2,
                            fontSize = 12,
                        }
                    };
                    container.Add(label);

                    VisualElement sliderContainer = new VisualElement()
                    {
                        style = {
                            flexDirection = FlexDirection.Row,
                            alignItems = Align.Center,
                            width = 180,
                        }
                    };

                    SliderInt slider = new SliderInt(lowValue, highValue)
                    {
                        name = name,
                        value = value,
                        tooltip = tooltip,
                        style = {
                            flexGrow = 1,
                            height = 16,
                            width = 80,
                        }
                    };

                    IntegerField valueField = new IntegerField()
                    {
                        value = value,
                        style = {
                            width = 48,
                            marginLeft = 5,
                            fontSize = 12,
                        },
                        tooltip = tooltip
                    };

                    Button decrementButton = new Button(() =>
                    {
                        int currentValue = slider.value;
                        int newValue = Mathf.Clamp(currentValue - 1, lowValue, highValue);
                        if (newValue != currentValue)
                        {
                            slider.SetValueWithoutNotify(newValue);
                            valueField.SetValueWithoutNotify(newValue);
                            onValueChanged?.Invoke(newValue);
                        }
                    })
                    {
                        text = "−",
                        style = {
                            width = 20,
                            height = 20,
                            marginRight = 2,
                            paddingLeft = 0,
                            paddingRight = 0,
                            paddingTop = 0,
                            paddingBottom = 0,
                            fontSize = 12,
                            backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f)),
                            borderLeftColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderRightColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderTopColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                        }
                    };
                    sliderContainer.Add(decrementButton);

                    Button incrementButton = new Button(() =>
                    {
                        int currentValue = slider.value;
                        int newValue = Mathf.Clamp(currentValue + 1, lowValue, highValue);
                        if (newValue != currentValue)
                        {
                            slider.SetValueWithoutNotify(newValue);
                            valueField.SetValueWithoutNotify(newValue);
                            onValueChanged?.Invoke(newValue);
                        }
                    })
                    {
                        text = "+",
                        style = {
                            width = 20,
                            height = 20,
                            marginLeft = 2,
                            paddingTop = 0,
                            paddingRight = 0,
                            paddingBottom = 0,
                            paddingLeft = 0,
                            fontSize = 12,
                            backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f)),
                            borderLeftColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderRightColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderTopColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                            borderBottomColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f, 1f)),
                        }
                    };

                    slider.RegisterValueChangedCallback(evt =>
                    {
                        valueField.SetValueWithoutNotify(evt.newValue);
                        slider.SetValueWithoutNotify(evt.newValue);
                        valueField.MarkDirtyRepaint();
                        slider.MarkDirtyRepaint();
                        onValueChanged?.Invoke(evt.newValue);
                    });
                    valueField.RegisterValueChangedCallback(evt =>
                    {
                        int clampedValue = Mathf.Clamp(evt.newValue, lowValue, highValue);
                        valueField.SetValueWithoutNotify(clampedValue);
                        slider.SetValueWithoutNotify(clampedValue);
                        valueField.MarkDirtyRepaint();
                        slider.MarkDirtyRepaint();
                        onValueChanged?.Invoke(clampedValue);
                    });

                    sliderContainer.Add(slider);
                    sliderContainer.Add(incrementButton);
                    sliderContainer.Add(valueField);

                    container.Add(sliderContainer);

                    return container;
                }

                public MightySceneViewManager.Settings GetSceneViewSettings()
                {
                    return FPSHeatmapCore.data.settings.sceneView;
                }

                private Color ParseRgbToColor(string rgb)
                {
                    var match = Regex.Match(rgb, @"rgb\((\d+),\s*(\d+),\s*(\d+)\)");
                    if (match.Success)
                    {
                        return new Color(
                            int.Parse(match.Groups[1].Value) / 255f,
                            int.Parse(match.Groups[2].Value) / 255f,
                            int.Parse(match.Groups[3].Value) / 255f
                        );
                    }
                    return Color.white;
                }

                private Color GetColorFromFile(string fullPath)
                {
                    if (!File.Exists(fullPath))
                    {
                        DevLogError("File not found: " + fullPath);
                        return Color.white;
                    }

                    string content = File.ReadAllText(fullPath);
                    string pattern = @"\.screenshotable\s*{\s*[^}]*background-color:\s*(rgb\(\d{1,3}, \d{1,3}, \d{1,3}\));?";

                    var match = Regex.Match(content, pattern);
                    if (match.Success)
                    {
                        // DevLog($"LLL {match.Groups[1].Value}");
                        return ParseRgbToColor(match.Groups[1].Value);
                    }

                    return Color.white;
                }

                [SerializeField] private string name;
                [SerializeField] private string description;
                [SerializeField] private string anchorTo;
                [SerializeField] private Location mapLocation;
                [SerializeField] private long createdAt;
                [SerializeField] private long lastModified;
                [SerializeField] private long lastQueried;
                [SerializeField] private string status;
                [SerializeField] private SceneViewLabel label;
                [SerializeField] private int id;
                [SerializeField] private int parentId;
                [SerializeField] private bool active;
                [SerializeField] private bool front;
                [SerializeField] private bool dirty;
                [SerializeField] private bool hasVisualContent = false;
                [SerializeField] private bool hasPlayTracking = true;
                [SerializeField] private bool showAlways = false;
                [SerializeField] private Views viewUI;
                [SerializeField] private VisualElement prevView;

                [SerializeField] private VisualElement view;
                [SerializeField] private Vector3 offset;
                [SerializeField] private Attributes mapAttributes;
                [SerializeField] private Picture pic;

                public string Name { get => name; set => name = value; }
                public string Description { get => description; set => description = value; }
                public string AnchorTo { get => anchorTo; set => anchorTo = value; }
                public Location MapLocation { get => mapLocation; set => mapLocation = value; }
                public long CreatedAt { get => createdAt; set => createdAt = value; }
                public long LastModified { get => lastModified; set => lastModified = value; }
                public long LastQueried { get => lastQueried; set => lastQueried = value; }
                public string Status { get => status; set => status = value; }
                public SceneViewLabel Label { get => label; set => label = value; }
                public int ID { get => id; set => id = value; }
                public int ParentId { get => parentId; set => parentId = value; }
                public bool Active { get => active; set => active = value; }
                public bool Front { get => front; set => front = value; }
                public bool Dirty { get => dirty; set => dirty = value; }
                public VisualElement PrevView { get => prevView; set => prevView = value; }
                public bool HasVisualContent { get => hasVisualContent; set => hasVisualContent = value; }
                public bool HasPlayTracking { get => hasPlayTracking; set => hasPlayTracking = value; }
                public bool ShowAlways { get => showAlways; set => showAlways = value; }
                public Views ViewUI { get => viewUI; set => viewUI = value; }
                public VisualElement View { get => view; set => view = value; }
                public Vector3 Offset { get => offset; set => offset = value; }

                public Attributes MapAttributes { get => mapAttributes; set => mapAttributes = value; }
                public Picture Pic { get => pic; set => pic = value; }

                private Texture2D _icon;

                public Texture2D Icon
                {
                    get
                    {
                        if (_icon == null)
                        {
                            _icon = Resources.Load<Texture2D>("mighty_icon_toggle_heatmap");
                        }
                        return _icon;
                    }
                    set
                    {
                        _icon = value;
                    }
                }

                public Texture2D iconAddMappable { get; set; }

                public override string ToString()
                {
                    return "FPSHeatmap";
                }

                public void Delete()
                {
                    MightyCoreData.mappables.Remove(this);
                    FPSHeatmapCore.sceneData.heatmapTracking.Remove(this);
                }

                public Root()
                {
                }

                public Root(string n, Camera camera)
                {
                    DevLog($"Creating Tracking {n} at {camera.transform.position} with rotation {camera.transform.rotation} Camera Name: {camera.name}");
                    ID = 0;
                    FPSHeatmapCore.sceneData.heatmapTracking ??= new List<Root>();
                    if (FPSHeatmapCore.sceneData.heatmapTracking.Count > 0)
                        ID = FPSHeatmapCore.sceneData.heatmapTracking.Max(lm => lm.ID) + 1;
                    ParentId = 0;

                    AnchorTo = sceneData.Name;
                    Name = n;
                    Description = "New Heatmap";
                    Status = "active";

                    CreatedAt = LastModified = LastQueried = DateTime.Now.Ticks;

                    HasVisualContent = false;
                    HasPlayTracking = true;

                    MapAttributes = new Attributes
                    {
                        textMainColor = new Color(1, 1, 1, 1f),
                        backgroundColor = new Color(0, 0, 0, 1f),
                        textAccentColor = new Color(0, 1, 1, 1f),
                        backgroundAccentColor = new Color(0, 1, 1, 1.0f)
                    };

                    MapLocation = new Location
                    {
                        worldPosition = camera.transform.position,
                        worldRotation = camera.transform.rotation
                    };

                    Label = new SceneViewLabel
                    {
                        show = true,
                        fade = true,
                        fadeMin = 100,
                        fadeMax = 1000,
                        offset = new Vector3()
                    };
                    Label.offset.x = Label.offset.y = Label.offset.z = 0;

                    RegisterMappable();
                }
            }
        }

        public void OnDisable()
        {
            if (thumbnail != null)
            {
                DestroyImmediate(thumbnail);
                thumbnail = null;
            }
        }
    }
}
#endif
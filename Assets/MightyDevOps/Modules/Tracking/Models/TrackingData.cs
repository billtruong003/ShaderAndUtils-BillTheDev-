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

namespace MightyTracking
{
    public class TrackingData : ScriptableObject
    {
        // public static string Version = "1.0.0";

        [Serializable]
        public class Settings
        {

            [SerializeField]
            public bool showTrackingTrails = true; // Default to on
            [SerializeField]
            public bool showTrackingCaptures = true; // Default to on

            // Existing properties
            public float SvThickness
            {
                get { return svThickness; }
                set { svThickness = value; }
            }

            public float SvIntensity
            {
                get { return svIntensity; }
                set { svIntensity = value; }
            }

            public float SvSpeed
            {
                get { return svSpeed; }
                set { svSpeed = value; }
            }

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


            public int lineWidth = 2;
            private float svThickness = 7f;
            private float svIntensity = 1f;
            private float svSpeed = 1f;
            private float svFadeDistance = 50f;
            private AnimationCurve svOpacityCurve = AnimationCurve.Linear(0, 1, 1, 0);
            public MightySceneViewManager.Settings sceneView = new MightySceneViewManager.Settings()
            {
                borderColor = new Color(0, 0, 0, 0),
                distanceStart = 2,
                distanceEnd = 50,
                show = true
            };

        }
        public static void Save()
        {
            string path = $"{corePath}/Modules/Tracking/Data/TrackingData.asset";
            string directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AssetDatabase.Refresh();
            }

            if (File.Exists(path))
            {
                DevLog($"{path} already exists...");
                return;
            }

            TrackingData asset = ScriptableObject.CreateInstance<TrackingData>();

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }

        public static TrackingData Load()
        {
            string path = $"{corePath}/Modules/Tracking/Data/TrackingData.asset";
            string fallbackPath = $"{corePath}/Modules/Tracking/Data/TrackingData_.asset";

            if (!File.Exists(path))
            {
                if (File.Exists(fallbackPath))
                {
                    DevLog($"Renaming {fallbackPath} to {path}");
                    File.Copy(fallbackPath, path);
                    AssetDatabase.Refresh();
                }
                else
                {
                    Save();
                }
            }
            //return Resources.Load("TrackingModuleData", typeof(TrackingData)) as TrackingData;
            return AssetDatabase.LoadAssetAtPath<TrackingData>(path);
        }

        [SerializeField]
        public Settings settings = new Settings();
        // [SerializeField]
        // public List<Tracking.Root> Tracking;
        [SerializeField]
        public List<SceneData> scenes;
        private static Texture2D thumbnail;

        [Serializable]
        public class SceneData
        {
            public string name;
            [SerializeField]
            public List<Tracking.TransformTracker> transforms;
            [SerializeField]
            public List<Tracking.Root> Tracking;

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


        public void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            DevLog("TrackingData OnGenerateVisualContent");
            // DevLog($"Name {TrackingCore.trackingType.Name} is active: {TrackingCore.trackingType.IsActive}");
            if (!TrackingCore.trackingType.IsActive)
            {
                if (heatmaps.Count > 0)
                {
                    RunPlaybackMinMaxUpdated?.Invoke();
                    Dirty = true;
                    DevLog("TrackingData OnGenerateVisualContent: Dirty = true");
                }
                DevLog("TrackingData OnGenerateVisualContent: return");
                return;
            }

            var transformTracker = TrackingViews.transformTracker;
            DevLog("TrackingData OnGenerateVisualContent: transformTracker with count: " + transformTracker.Count);

            if (transformTracker == null)
            {
                DevLog("TrackingData OnGenerateVisualContent: transformTracker is null");
                return;
            }

            var paint2D = mgc.painter2D;
            paint2D.lineWidth = settings.lineWidth;



            foreach (var kvp in transformTracker)
            {
                var playThrough = kvp.Value;
                DevLog("TrackingData OnGenerateVisualContent: playThrough with count: " + playThrough.Count);
                var initialColor = playThrough[0].color;
                var initialPosition = playThrough[0].position;

                // Precompute coordinates
                var coordsList = new List<Vector2>(playThrough.Count);
                for (int i = 0; i < playThrough.Count; i++)
                {
                    coordsList.Add(GetMapCoords(playThrough[i].position.x, playThrough[i].position.z));
                }

                // Draw path
                paint2D.strokeColor = initialColor;
                paint2D.fillColor = initialColor * new Color(3f, 3f, 3f, 1f);

                paint2D.BeginPath();
                paint2D.MoveTo(coordsList[0]);
                for (int i = 1; i < coordsList.Count; i++)
                {
                    paint2D.LineTo(coordsList[i]);
                }
                paint2D.Stroke();
                paint2D.ClosePath();

                // Cluster arrows
                float clusterDistance = 10f;  // Adjust this value as needed
                List<Vector2> clusteredPositions = new List<Vector2>();
                List<Vector2> directions = new List<Vector2>();

                Vector2 currentClusterCenter = coordsList[0];
                Vector2 currentDirection = Vector2.zero;
                int currentClusterSize = 1;

                for (int i = 1; i < coordsList.Count; i++)
                {
                    Vector2 direction = (coordsList[i] - coordsList[i - 1]).normalized;
                    if (Vector2.Distance(coordsList[i], currentClusterCenter) < clusterDistance)
                    {
                        currentClusterCenter = (currentClusterCenter * currentClusterSize + coordsList[i]) / (currentClusterSize + 1);
                        currentDirection = (currentDirection * currentClusterSize + direction) / (currentClusterSize + 1);
                        currentClusterSize++;
                    }
                    else
                    {
                        clusteredPositions.Add(currentClusterCenter);
                        directions.Add(currentDirection);

                        currentClusterCenter = coordsList[i];
                        currentDirection = direction;
                        currentClusterSize = 1;
                    }
                }
                clusteredPositions.Add(currentClusterCenter);
                directions.Add(currentDirection);

                // Draw clustered arrows at the midpoint of each cluster
                float arrowSize = 10f;  // Size of the arrowheads
                for (int i = 1; i < clusteredPositions.Count; i++)
                {
                    Vector2 start = clusteredPositions[i - 1];
                    Vector2 end = clusteredPositions[i];
                    Vector2 direction = (end - start).normalized;
                    Vector2 midpoint = (start + end) / 2;

                    // DevLog($"Midpoint: {midpoint} Direction: {direction}");

                    Vector2 arrowTip = midpoint + direction * arrowSize * 0.5f;
                    Vector2 arrowLeft = midpoint + new Vector2(-direction.y, direction.x) * arrowSize * 0.5f;
                    Vector2 arrowRight = midpoint + new Vector2(direction.y, -direction.x) * arrowSize * 0.5f;

                    paint2D.strokeColor = initialColor;
                    paint2D.fillColor = initialColor * new Color(3f, 3f, 3f, 1f);

                    paint2D.BeginPath();
                    paint2D.MoveTo(arrowTip);
                    paint2D.LineTo(arrowLeft);
                    paint2D.LineTo(arrowRight);
                    paint2D.LineTo(arrowTip);
                    paint2D.Fill();
                    paint2D.Stroke();
                    paint2D.ClosePath();
                }

                // Draw a filled circle at the end of the path using an Arc
                Vector2 endCoords = coordsList[coordsList.Count - 1];
                float circleRadius = 5f;  // Radius of the circle

                paint2D.strokeColor = initialColor;
                paint2D.fillColor = initialColor * new Color(3f, 3f, 3f, 1f);

                paint2D.BeginPath();
                paint2D.MoveTo(new Vector2(endCoords.x + circleRadius, endCoords.y));
                paint2D.Arc(endCoords, circleRadius, 0, 360);
                paint2D.ClosePath();
                paint2D.Fill();
                paint2D.Stroke();
            }
        }

        [Serializable]
        public class Tracking
        {
            [Serializable]
            public enum ImageCompressionFormat
            {
                PNG,
                JPG
            }

            [Serializable]
            public enum CustomRenderTextureFormat
            {
                // Standard formats
                ARGB32,
                RGB24,
                RGBA32,

                // High precision formats
                RGBAFloat,
                RGBAHalf,

                // HDR formats for modern pipelines
                R16G16B16A16_SFloat,  // High precision HDR
                R11G11B10_UFloat,     // Packed HDR (common in HDRP)
                RGB111110Float,       // HDR without alpha
                ARGB2101010,          // 10-bit per channel
                DefaultHDR            // Let Unity choose best HDR format
            }

            [Serializable]
            public struct TransformTracker
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
                public Color color;


                [SerializeField]
                public float movementSpeed;       // Speed of object movement
                [SerializeField]
                public float rotationSpeed;       // Speed of object rotation


                [SerializeField]
                public byte[] renderTextureBytes;

                [SerializeField]
                public Texture2D texture;

                [SerializeField]
                public int renderTextureWidth;

                [SerializeField]
                public int renderTextureHeight;

                // New compression and format settings
                [SerializeField]
                public ImageCompressionFormat compressionFormat;

                [SerializeField]
                public int jpgQuality; // 1-100 for JPG quality

                [SerializeField]
                public bool pngCompression; // Enable PNG compression

                [SerializeField]
                public CustomRenderTextureFormat renderFormat;

                [SerializeField]
                public int depthBuffer; // 0, 16, 24, or 32 bit depth buffer

                public TransformTracker(
                    string n,
                    long timeStamp,
                    Vector3 pos,
                    Quaternion rot,
                    Vector3 scl,
                    Color c,
                    float moveSpeed,
                    float rotSpeed,
                    byte[] renderTextureBytes,
                    int renderTextureWidth,
                    int renderTextureHeight,
                    ImageCompressionFormat compressionFormat = ImageCompressionFormat.PNG,
                    int jpgQuality = 75,
                    bool pngCompression = true,
                    CustomRenderTextureFormat renderFormat = CustomRenderTextureFormat.ARGB32,
                    int depthBuffer = 24,
                    Texture2D capture = null
)
                {
                    name = n;
                    this.timeStamp = timeStamp;
                    position = pos;
                    rotation = rot;
                    scale = scl;
                    color = c;

                    movementSpeed = moveSpeed;
                    rotationSpeed = rotSpeed;

                    this.renderTextureBytes = renderTextureBytes;
                    this.renderTextureWidth = renderTextureWidth;
                    this.renderTextureHeight = renderTextureHeight;
                    this.texture = capture;

                    // Assign new compression and format fields
                    this.compressionFormat = compressionFormat;
                    this.jpgQuality = jpgQuality;
                    this.pngCompression = pngCompression;
                    this.renderFormat = renderFormat;
                    this.depthBuffer = depthBuffer;
                }

                public Texture2D GetTexture()
                {
                    Debug.Log($"GetTexture() called for tracker {name} at timestamp {timeStamp}");
                    if (texture == null)
                    {
                        Debug.Log($"Texture is null, creating new texture from bytes. Width: {renderTextureWidth}, Height: {renderTextureHeight}, Bytes length: {(renderTextureBytes != null ? renderTextureBytes.Length : 0)}");
                        texture = TrackingRenders.BytesToTexture2D(renderTextureBytes, renderTextureWidth, renderTextureHeight);
                        Debug.Log($"Created new texture: {(texture != null ? "Success" : "Failed")}");
                    }
                    else
                    {
                        Debug.Log($"Returning existing texture for {name}");
                    }
                    return texture;
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

                [SerializeField]
                public Color trackingColor;

                public List<Positional> positional;


                public TransformData(string n, Color color)
                {
                    // sceneData.PlayTrackingList.Add(sceneData.RunID);
                    // RunID = sceneData.RunID;
                    name = $"{n}|{sceneData.RunID}";
                    positional = new List<Positional>();
                    trackingColor = color;
                }

                public void AddPosition(Transform transform, Color color, Camera camera)
                {
                    //   DevLog("AddData");
                    if (sceneData.RunID == "")
                    {
                        DevLog("No run_id");
                        return;
                    }

                    trackingColor = color;

                    positional.Add(new Positional()
                    {
                        name = "Tracking taken on " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
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
                private string _version = "1.3.0";
                public string Version
                {
                    get => _version;
                    set => _version = value;
                }

                private string _packageName = "Mighty Tracking";
                public string PackageName
                {
                    get => _packageName;
                    set => _packageName = value;
                }

                public bool UpdateAvailable()
                {
                    // var updateInfo = dataCore.moduleUpdates.FirstOrDefault(update => update.module == "tracking");
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
                    if (sceneData == null) TrackingCore.GetSceneData();
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
                    return new(Icon, mappableTypeInfo, "TrackingOverlay");
                }

                public VisualElement SceneSummary(MightyCoreData.SceneData scene)
                {
                    return new VisualElement();
                }

                public VisualElement SettingsView()
                {


                    VisualElement settingsView = new VisualElement()
                    {
                        name = "TrackingSettingsView",
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

                    // Helper function to create section headers with icons
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

                    // Helper function to create info boxes
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

                    // Add centralized feedback section
                    var feedbackSection = CreateFeedbackSection(
                        "Mighty Tracking",
                        "https://prf.hn/click/camref:1011lf9gY/pubref:editor/ar:internal/destination:https%3A%2F%2Fassetstore.unity.com%2Fpackages%2Fslug%2F318759",
                        "https://github.com/ShrinkRayEntertainment/Mighty-Tracking/issues/new?template=%F0%9F%90%9B-bug-report.md&labels=bug&title=[{VERSION}%20|%20{UNITY_VERSION}]%20Your%20Title",
                        "https://github.com/ShrinkRayEntertainment/Mighty-Tracking/issues/new?template=%E2%9C%A8-feature-request.md&labels=enhancement&title=[{VERSION}%20|%20{UNITY_VERSION}]%20Your%20Title",
                        Version
                    );
                    settingsView.Add(feedbackSection);

                    // Getting Started button
                    Button gettingStartedButton = new Button(() =>
                    {
                        ICommand command = new TrackingCore.OpenGettingStartedWindowCommand();
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

                    // Object Trails Settings
                    settingsView.Add(CreateSectionHeader("Object Trails Settings", "Configure movement visualization", new Color(0.4f, 1f, 1f, 1f)));

                    settingsView.Add(CreateInfoBox("Customize how movement trails appear in the Scene view", "Trails settings explanation"));

                    // Add Show Tracking Trails Toggle
                    Toggle showTrackingTrailsToggle = new Toggle("Show Tracking Trails")
                    {
                        name = "TrackingTrailsToggle",
                        value = TrackingCore.data.settings.showTrackingTrails,
                        tooltip = "Toggle visibility of movement trails in the Scene view",
                        style = {
                            marginBottom = 10,
                            unityFontStyleAndWeight = FontStyle.Bold,
                            fontSize = 12
                        }
                    };
                    showTrackingTrailsToggle.RegisterValueChangedCallback(evt =>
                    {
                        TrackingCore.data.settings.showTrackingTrails = evt.newValue;
                        Dirty = true;
                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                    });
                    settingsView.Add(showTrackingTrailsToggle);

                    // Add Show Tracking Captures Toggle
                    Toggle showTrackingCapturesToggle = new Toggle("Show Camera Captures")
                    {
                        name = "TrackingCapturesToggle",
                        value = TrackingCore.data.settings.showTrackingCaptures,
                        tooltip = "Toggle visibility of tracking captures in the Scene view",
                        style = {
                            marginBottom = 10,
                            unityFontStyleAndWeight = FontStyle.Bold,
                            fontSize = 12
                        }
                    };
                    showTrackingCapturesToggle.RegisterValueChangedCallback(evt =>
                    {
                        TrackingCore.data.settings.showTrackingCaptures = evt.newValue;
                        Dirty = true;
                        MightySceneViewManager.Instance.ToggleCategory("TrackingOverlay", evt.newValue);

                        RunPlaybackMinMaxUpdated?.Invoke();
                        UpdateMiniMap?.Invoke();
                    });
                    settingsView.Add(showTrackingCapturesToggle);


                    settingsView.Add(FloatSliderWithField("Thickness", "Width of the movement trails", "TrackingSVThicknessSlider", TrackingCore.data.settings.SvThickness, 1f, 20f, newValue =>
                    {
                        TrackingCore.data.settings.SvThickness = newValue;
                    }));

                    settingsView.Add(FloatSliderWithField("Intensity", "Brightness of the trails", "TrackingSVIntensitySlider", TrackingCore.data.settings.SvIntensity, 0.1f, 5f, newValue =>
                    {
                        TrackingCore.data.settings.SvIntensity = newValue;
                    }));

                    settingsView.Add(FloatSliderWithField("Speed", "Animation speed of the trails", "TrackingSvSpeedSlider", TrackingCore.data.settings.SvSpeed, 0.1f, 5f, newValue =>
                    {
                        TrackingCore.data.settings.SvSpeed = newValue;
                    }));

                    settingsView.Add(FloatSliderWithField("Fade", "How quickly trails disappear", "TrackingSvFadeDistanceSlider", TrackingCore.data.settings.SvFadeDistance, 1f, 200f, newValue =>
                    {
                        TrackingCore.data.settings.SvFadeDistance = newValue;
                    }));

                    Label opacityCurveLabel = new Label("Opacity")
                    {
                        tooltip = "Control how quickly trails fade out",
                        style = { unityFontStyleAndWeight = FontStyle.Bold, marginBottom = 5, marginTop = 10, fontSize = 12 }
                    };
                    settingsView.Add(opacityCurveLabel);

                    CurveField opacityCurveField = new CurveField()
                    {
                        name = "TrackingSvOpacityCurve",
                        value = TrackingCore.data.settings.SvOpacityCurve,
                        tooltip = "Adjust the fade-out curve of the trails",
                        style = { marginBottom = 10, width = 150 }
                    };
                    opacityCurveField.RegisterValueChangedCallback(evt =>
                    {
                        TrackingCore.data.settings.SvOpacityCurve = evt.newValue;
                    });
                    settingsView.Add(opacityCurveField);


                    return settingsView;
                }

                // Helper method to create a float slider with an editable field next to it
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

                    // Declare variables at the top
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

                    // Decrement button
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

                    // Increment button
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

                    // Synchronize slider and field
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

                // Helper method to create an int slider with an editable field next to it
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

                    // Declare variables at the top
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

                    // Decrement button
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

                    // Increment button
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

                    // Synchronize slider and field
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
                    return TrackingCore.data.settings.sceneView;
                }

                // Function to parse RGB string to Color
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
                    return Color.white; // Default to white if parsing fails
                }

                private Color GetColorFromFile(string fullPath)
                {
                    if (!File.Exists(fullPath))
                    {
                        DevLogError("File not found: " + fullPath);
                        return Color.white; // Default to white if file not found
                    }

                    string content = File.ReadAllText(fullPath);
                    string pattern = @"\.screenshotable\s*{\s*[^}]*background-color:\s*(rgb\(\d{1,3}, \d{1,3}, \d{1,3}\));?";

                    var match = Regex.Match(content, pattern);
                    if (match.Success)
                    {
                        // DevLog($"LLL {match.Groups[1].Value}");
                        return ParseRgbToColor(match.Groups[1].Value);
                    }

                    return Color.white; // Default to white if color not found
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
                            _icon = Resources.Load<Texture2D>("mighty_icon_toggle_tracking1");
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
                    return "Tracking";
                }

                public void Delete()
                {
                    MightyCoreData.mappables.Remove(this);
                    TrackingCore.sceneData.Tracking.Remove(this);
                }

                public Root()
                {
                }

                public Root(string n, Camera camera)
                {
                    DevLog($"Creating Tracking {n} at {camera.transform.position} with rotation {camera.transform.rotation} Camera Name: {camera.name}");
                    ID = 0;
                    TrackingCore.sceneData.Tracking ??= new List<Root>();
                    if (TrackingCore.sceneData.Tracking.Count > 0)
                        ID = TrackingCore.sceneData.Tracking.Max(lm => lm.ID) + 1;
                    ParentId = 0;

                    AnchorTo = sceneData.Name;
                    Name = n;
                    Description = "New Tracking";
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
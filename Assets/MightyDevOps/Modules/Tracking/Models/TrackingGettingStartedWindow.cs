#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MightyLandmarks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static Mighty.MightyCoreData;

namespace Mighty
{
    public class TrackingGettingStartedWindow
    {
        public VisualElement view;

        private struct TutorialStep
        {
            public string title;
            public string description;
            public string actionText;
            public System.Action action;
            public Texture2D image;
        }

        List<Texture2D> tutorialImages = new();

        public void BuildView()
        {
            for (int i = 1; i < 6; i++)
            {
                int zeroPad = 0;
                if (i > 9) zeroPad = 1;
                var texture = Resources.Load<Texture2D>("mighty_tutorial_tracking_" + zeroPad + i);
                Debug.Log($"Loading texture {i}: mighty_tutorial_tracking_{zeroPad}{i} - Success: {texture != null}, Size: {(texture != null ? $"{texture.width}x{texture.height}" : "N/A")}");
                tutorialImages.Add(texture);
            }

            if (view == null)
                view = new VisualElement
                {
                    name = "TrackingGettingStarted",
                    style = {
                    height = Length.Percent(100),
                    flexGrow = 0,
                    flexDirection = FlexDirection.Column,
                    flexShrink = 0,
                    overflow = Overflow.Hidden,
                    flexWrap = Wrap.Wrap,
                    justifyContent = Justify.SpaceAround,
                    minHeight = 420,
                    maxHeight = 700,
                    minWidth = 380,
                    maxWidth = 480,
                    backgroundColor = new Color(0.98f, 0.98f, 0.98f, 1f),
                    borderTopLeftRadius = 8,
                    borderTopRightRadius = 8,
                    borderBottomLeftRadius = 8,
                    borderBottomRightRadius = 8,
                }
                };
            else view.Clear();

            // Header
            VisualElement header = new()
            {
                name = "Header",
                style = {
                flexDirection = FlexDirection.Row,
                flexGrow = 0,
                flexShrink = 0,
                height = 64,
                width = Length.Percent(100),
                justifyContent = Justify.Center,
                alignItems = Align.Center,
                backgroundColor = new StyleColor(new Color(0.13f, 0.13f, 0.13f, 1f)),
                paddingLeft = 20,
                paddingRight = 20,
                borderTopLeftRadius = 8,
                borderTopRightRadius = 8,
            }
            };

            VisualElement titleContainer = new()
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                }
            };

            // Header icon (using tracking icon if available)
            VisualElement headerIcon = new()
            {
                style = {
                    width = 24,
                    height = 24,
                    marginRight = 12,
                    backgroundImage = icons.previewTracking,
                    backgroundColor = new Color(0.7f, 0.3f, 1f, 1f), // Fallback color
                }
            };

            Label title = new()
            {
                text = "Tracking - Getting Started",
                style = {
                fontSize = 20,
                color = new StyleColor(Color.white),
                unityFontStyleAndWeight = FontStyle.Bold,
                letterSpacing = 0.5f,
            }
            };

            titleContainer.Add(headerIcon);
            titleContainer.Add(title);
            header.Add(titleContainer);

            // Content scroll area
            ScrollView content = new()
            {
                name = "Content",
                style = {
                flexDirection = FlexDirection.Column,
                flexGrow = 1,
                flexShrink = 1,
                height = Length.Percent(100),
                width = Length.Percent(100),
                paddingLeft = 24,
                paddingRight = 24,
                paddingTop = 24,
                paddingBottom = 24,
            }
            };

            // Callout section
            VisualElement calloutCard = new()
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    paddingBottom = 12,
                    paddingLeft = 16,
                    paddingRight = 16,
                    paddingTop = 12,
                    marginBottom = 16,
                    backgroundColor = new Color(0.95f, 0.95f, 1f, 1f),
                    borderTopLeftRadius = 8,
                    borderTopRightRadius = 8,
                    borderBottomLeftRadius = 8,
                    borderBottomRightRadius = 8,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopWidth = 1,
                    borderBottomColor = new Color(0.7f, 0.7f, 0.9f, 1f),
                    borderLeftColor = new Color(0.7f, 0.7f, 0.9f, 1f),
                    borderRightColor = new Color(0.7f, 0.7f, 0.9f, 1f),
                    borderTopColor = new Color(0.7f, 0.7f, 0.9f, 1f),
                    alignItems = Align.Center,
                }
            };

            Label calloutText = new("You can reopen this window anytime by clicking 'Getting Started' at the top of the settings pane.")
            {
                style = {
                    fontSize = 12,
                    color = new Color(0.3f, 0.3f, 0.6f, 1f),
                    whiteSpace = WhiteSpace.Normal,
                    flexGrow = 1,
                }
            };

            calloutCard.Add(calloutText);

            // Welcome section
            VisualElement welcomeCard = CreateCard();
            Label welcomeTitle = new("Welcome to Tracking!")
            {
                style = {
                    fontSize = 18,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.15f, 0.15f, 0.15f, 1f),
                    marginBottom = 12,
                }
            };

            Label welcomeText = new("Tracking helps you monitor and record game object movement, interactions, and behaviors in your scenes. This powerful tool captures detailed data about how game objects move so you can fix AI, physics, and level design issues quickly.")
            {
                style = {
                    fontSize = 13,
                    color = new Color(0.4f, 0.4f, 0.4f, 1f),
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 16,
                }
            };

            welcomeCard.Add(welcomeTitle);
            welcomeCard.Add(welcomeText);

            // Tutorial steps
            List<TutorialStep> steps = new()
            {
                new TutorialStep
                {
                    title = "1. Set up your tracking target",
                    description = "Click on your main character or camera and ensure the inspector window is available. Scroll to the bottom of the inspector and Add Component, then search for 'tracker' and select 'Mighty Tracker'.",
                    actionText = "",
                    action = () => {
                        if (SceneView.sceneViews.Count > 0)
                            ((SceneView)SceneView.sceneViews[0]).Focus();
                        else
                            EditorWindow.GetWindow<SceneView>();
                    },
                    image = tutorialImages[0]
                },
                new TutorialStep
                {
                    title = "2. Enable Camera Capture",
                    description = "Click on the 'Camera Capture' button and find the 'Auto-Detect Pipeline Settings' button and hit apply.  Drag the main camera into the 'Capture Camera' slot and make sure 'Is Main Camera' is checked (this ensures it will capture the full UI and post processing).",
                    actionText = "",
                    action = () => ShowToast("Recording enabled for tracking data collection!"),
                    image = tutorialImages[1]
                },
                new TutorialStep
                {
                    title = "3. Enable tracking recording",
                    description = "Look for the 'R' button at the bottom left of the Mighty Map window and make sure it is RED to indicate recording is active.",
                    actionText = "",
                    action = () => ShowToast("Recording enabled for tracking data collection!"),
                    image = tutorialImages[2]
                },
                new TutorialStep
                {
                    title = "4. Enter Play Mode",
                    description = "Click the Play button at the top of the screen to begin tracking recording.",
                    actionText = "",
                    action = () => EditorApplication.isPlaying = true,
                },
                new TutorialStep
                {
                    title = "5. Move and interact",
                    description = "Move around your scene and interact with objects to generate tracking data. The system will record your position, rotation, and interactions automatically.",
                    actionText = "",
                    action = () => EditorApplication.isPlaying = true,
                },
                new TutorialStep
                {
                    title = "6. Exit Play Mode",
                    description = "Click the Stop button at the top of the screen to stop recording. Your tracking data will be saved automatically.",
                    actionText = "",
                    action = () => EditorApplication.isPlaying = true
                },
                new TutorialStep
                {
                    title = "7. View your tracking data",
                    description = "You should see your movement trail overlaid on the Mighty Map! The colored path shows where you moved during the session.  You should also see the path laid out in 3d in the sceneview, if you do not, ensure that TrackingViews is enabled in your gizmos list.",
                    actionText = "",
                    action = () => EditorApplication.isPlaying = true,
                    image = tutorialImages[3]
                },
                new TutorialStep
                {
                    title = "8. Camera Captures",
                    description = "Along the 3d Gizmo path you should also see small circles bordered with the same color as the trail.  Hover over it and RIGHT CLICK!  If you want to see it full resolution and be able to save as an actual image, LEFT CLICK it and it will open in your default image viewer with a temporary filename.  To close the view, RIGHT CLICK the circle again.",
                    actionText = "",
                    action = () => EditorApplication.isPlaying = true,
                    image = tutorialImages[4]
                },
                new TutorialStep
                {
                    title = "9. Advanced tracking settings",
                    description = "Click the black bar on the left of the Mighty Map window to open the modules bar, then click on the Tracking icon. This opens advanced settings where you can configure tracking frequency, data retention, and visualization options.",
                    actionText = "",
                    action = () => EditorApplication.isPlaying = true
                }
            };

            // Tips section
            VisualElement tipsCard = CreateCard();
            Label tipsTitle = new("Pro Tips")
            {
                style = {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = new Color(0.15f, 0.15f, 0.15f, 1f),
                    marginBottom = 12,
                }
            };

            VisualElement tipsList = new()
            {
                style = { flexDirection = FlexDirection.Column }
            };

            string[] tips = {
                "- Do the same process on an enemy or NPC to see how they move around the level",
                "- Camera Capture for non-main camera will not produce full post processing effects, it's meant to confirm 'what they saw'",
                "- Pay attention to the 'Storage Estimation' at the bottom of the Tracker component, camera capture can potential use a lot of storage.",
                "- Deleting a playthrough will also delete the saved captures",
                "- Check the Unity Asset Store page frequently for updates and new features!",
            };

            foreach (string tip in tips)
            {
                Label tipLabel = new(tip)
                {
                    style = {
                        fontSize = 12,
                        color = new Color(0.4f, 0.4f, 0.4f, 1f),
                        whiteSpace = WhiteSpace.Normal,
                        marginBottom = 6,
                    }
                };
                tipsList.Add(tipLabel);
            }

            tipsCard.Add(tipsTitle);
            tipsCard.Add(tipsList);

            // Close button
            Button closeButton = new(() =>
            {
                if (view.parent != null)
                {
                    view.parent.RemoveFromHierarchy();
                }
            })
            {
                text = "Get Started!",
                style = {
                    height = 40,
                    backgroundColor = new Color(0.2f, 0.4f, 0.8f, 1f),
                    color = Color.white,
                    borderTopLeftRadius = 20,
                    borderTopRightRadius = 20,
                    borderBottomLeftRadius = 20,
                    borderBottomRightRadius = 20,
                    borderTopWidth = 0,
                    borderBottomWidth = 0,
                    borderLeftWidth = 0,
                    borderRightWidth = 0,
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginTop = 16,
                }
            };

            closeButton.RegisterCallback<MouseEnterEvent>(evt =>
            {
                closeButton.style.backgroundColor = new Color(0.15f, 0.35f, 0.75f, 1f);
            });
            closeButton.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                closeButton.style.backgroundColor = new Color(0.2f, 0.4f, 0.8f, 1f);
            });

            content.Add(calloutCard);
            content.Add(welcomeCard);
            foreach (var step in steps)
            {
                content.Add(CreateStepCard(step));
            }
            content.Add(tipsCard);
            // content.Add(closeButton);

            view.Add(header);
            view.Add(content);
        }

        private VisualElement CreateCard()
        {
            return new VisualElement
            {
                style = {
                        flexDirection = FlexDirection.Column,
                        paddingBottom = 20,
                        paddingLeft = 20,
                        paddingRight = 20,
                        paddingTop = 20,
                    marginBottom = 16,
                    backgroundColor = Color.white,
                        borderTopLeftRadius = 12,
                        borderTopRightRadius = 12,
                        borderBottomLeftRadius = 12,
                        borderBottomRightRadius = 12,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftWidth = 1,
                    borderTopWidth = 1,
                    borderBottomColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                    borderLeftColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                    borderRightColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                    borderTopColor = new Color(0.9f, 0.9f, 0.9f, 1f),
                }
            };
        }

        private VisualElement CreateStepCard(TutorialStep step)
        {
            VisualElement card = CreateCard();

            // Generate a unique color for this step based on its title
            Color stepColor = MightyCoreData.StringToColor(step.title, 0.8f);

            VisualElement headerContainer = new()
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexStart,
                    alignItems = Align.Center,
                    backgroundColor = stepColor,
                    paddingLeft = 20,
                    paddingRight = 20,
                    paddingTop = 12,
                    paddingBottom = 12,
                    borderTopLeftRadius = 12,
                    borderTopRightRadius = 12,
                    marginLeft = -20,
                    marginRight = -20,
                    marginTop = -20,
                    marginBottom = 8,
                }
            };

            Label stepTitle = new(step.title)
            {
                style = {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    color = Color.white,
                }
            };

            headerContainer.Add(stepTitle);
            card.Add(headerContainer);

            // Add image if provided
            if (step.image != null)
            {
                Debug.Log($"Creating image for step '{step.title}': Size: {step.image.width}x{step.image.height}, Valid: {step.image != null}");

                VisualElement imageContainer = new()
                {
                    style = {
                        width = Length.Percent(100),
                        alignItems = Align.Center,
                        justifyContent = Justify.Center,
                    }
                };

                // Get the card's content width (accounting for padding)
                float cardWidth = card.style.width.value.value;
                float padding = 40; // Total horizontal padding (20px left + 20px right)
                float maxWidth = cardWidth - padding;

                float aspectRatio = (float)step.image.height / step.image.width;
                float imageWidth = Mathf.Min(step.image.width, maxWidth);
                float imageHeight = imageWidth * aspectRatio;

                Debug.Log($"Image dimensions - CardWidth: {cardWidth}, MaxWidth: {maxWidth}, Calculated Width: {imageWidth}, Height: {imageHeight}, Aspect: {aspectRatio}");

                Image image = new()
                {
                    image = step.image,
                    scaleMode = ScaleMode.ScaleToFit,
                    style = {
                        width = imageWidth,
                        height = imageHeight,
                    }
                };

                imageContainer.Add(image);
                card.Add(imageContainer);
            }

            Label description = new(step.description)
            {
                style = {
                    fontSize = 13,
                    color = new Color(0.4f, 0.4f, 0.4f, 1f),
                    whiteSpace = WhiteSpace.Normal,
                    marginBottom = 16,
                    marginTop = 8, // Small margin to separate from image
                }
            };

            card.Add(description);

            // Only add action button if actionText is not empty
            if (step.action != null && !string.IsNullOrEmpty(step.actionText))
            {
                Button actionButton = new(() => step.action?.Invoke())
                {
                    text = step.actionText,
                    style = {
                        height = 32,
                        backgroundColor = new Color(0.3f, 0.6f, 0.9f, 1f),
                        color = Color.white,
                        borderTopLeftRadius = 16,
                        borderTopRightRadius = 16,
                        borderBottomLeftRadius = 16,
                        borderBottomRightRadius = 16,
                        borderTopWidth = 0,
                        borderBottomWidth = 0,
                        borderLeftWidth = 0,
                        borderRightWidth = 0,
                        fontSize = 12,
                        unityFontStyleAndWeight = FontStyle.Bold,
                    }
                };

                actionButton.RegisterCallback<MouseEnterEvent>(evt =>
                {
                    actionButton.style.backgroundColor = new Color(0.25f, 0.55f, 0.85f, 1f);
                });
                actionButton.RegisterCallback<MouseLeaveEvent>(evt =>
                {
                    actionButton.style.backgroundColor = new Color(0.3f, 0.6f, 0.9f, 1f);
                });

                card.Add(actionButton);
            }

            return card;
        }
    }
}
#endif
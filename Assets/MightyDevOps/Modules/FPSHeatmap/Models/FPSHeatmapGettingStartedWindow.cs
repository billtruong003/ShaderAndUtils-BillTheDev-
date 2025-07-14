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
    public class FPSHeatmapGettingStartedWindow
    {
        public VisualElement view;

        public void BuildView()
        {
            // Load tutorial images
            LoadTutorialImages("mighty_tutorial_fpsheatmaps", 4);

            if (view == null)
                view = new VisualElement
                {
                    name = "FPSHeatmapGettingStarted",
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

            VisualElement titleContainer = new VisualElement
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                }
            };

            // Header icon (using heatmap icon if available)
            VisualElement headerIcon = new VisualElement
            {
                style = {
                    width = 24,
                    height = 24,
                    marginRight = 12,
                    backgroundImage = icons.previewHeatmaps,
                    backgroundColor = new Color(0.3f, 0.7f, 1f, 1f), // Fallback color
                }
            };

            Label title = new Label
            {
                text = "FPS Heatmap - Getting Started",
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

            // Add callout card
            content.Add(CreateCalloutCard("You can reopen this window anytime by clicking 'Getting Started' at the top of the settings pane."));

            // Add welcome card
            content.Add(CreateWelcomeCard("Welcome to FPS Heatmap!", "FPS Heatmap helps you visualize performance hotspots in your scenes. This tool tracks frame rates during play mode and displays them as color-coded overlays, making it easy to identify areas where your game might struggle."));

            // Tutorial steps
            content.Add(CreateTutorialCard(
                "1. Find your main character",
                "Click on your main character and ensure the inspector window is available. Now scroll to the bottom of the inspector and Add Component and search for 'heatmapper' and select 'Mighty FPS Heatmapper'. Leave all settings to default for now.",
                "",
                () =>
                {
                    if (SceneView.sceneViews.Count > 0)
                        ((SceneView)SceneView.sceneViews[0]).Focus();
                    else
                        EditorWindow.GetWindow<SceneView>();
                },
                tutorialImages.Count > 0 ? tutorialImages[0] : null
            ));

            content.Add(CreateTutorialCard(
                "2. Enable Playthrough Recording",
                "Look for the 'R' button at the bottom left of this screen, make sure it is RED.",
                "",
                () => ShowToast("Check the Unity Performance documentation for optimization tips!"),
                tutorialImages.Count > 1 ? tutorialImages[1] : null
            ));

            content.Add(CreateTutorialCard(
                "3. Enter Play Mode",
                "Click the Play button at the top of the screen to begin recording.",
                "",
                () => EditorApplication.isPlaying = true
            ));

            content.Add(CreateTutorialCard(
                "4. Move around",
                "Move around your scene to generate heatmap data points. For this initial run, play for at least 30 to 60 seconds to get a good sample.",
                "",
                () => EditorApplication.isPlaying = true
            ));

            content.Add(CreateTutorialCard(
                "5. Exit Play Mode",
                "Click the Stop button at the top of the screen to stop recording.",
                "",
                () => EditorApplication.isPlaying = true
            ));

            content.Add(CreateTutorialCard(
                "6. View your heatmap",
                "You should automatically see the heatmap overlaid on the Mighty Map!",
                "",
                () => ShowToast("Check the Unity Performance documentation for optimization tips!"),
                tutorialImages.Count > 2 ? tutorialImages[2] : null
            ));

            content.Add(CreateTutorialCard(
                "7. View your sceneview",
                "You should also see a 3d gizmo based heatmap directly in your sceneview.",
                "",
                () => ShowToast("Check the Unity Performance documentation for optimization tips!"),
                tutorialImages.Count > 3 ? tutorialImages[3] : null
            ));

            // Add tips card
            string[] tips = {
                "- The heatmap colors indicate performance: red areas are where FPS drops occurred",
                "- Longer play sessions will give you more accurate data",
                "- You can adjust the sensitivity of the heatmap in the settings",
                "- Try different camera angles to get a better view of performance issues",
                "- Check the Unity Performance documentation for optimization tips!"
            };
            content.Add(CreateTipsCard("Pro Tips", tips));

            view.Add(header);
            view.Add(content);
        }
    }
}
#endif
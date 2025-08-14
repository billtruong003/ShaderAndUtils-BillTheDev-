using UnityEngine;
using UnityEditor;

namespace BillTheDev.Editor.BillOutline.Common.Utils
{
    public static class EditorUtils
    {
        public static class CommonStyles
        {
            // === General & Wide Outline Styles ===
            public static readonly GUIContent InjectionPoint = EditorGUIUtility.TrTextContent("Stage", "Controls when the render pass executes.");
            public static readonly GUIContent ShowInSceneView = EditorGUIUtility.TrTextContent("Show In Scene View", "Sets whether to render the pass in the scene view.");
            public static readonly GUIContent Outlines = EditorGUIUtility.TrTextContent("Outlines", "The list of outlines to render.");
            public static readonly GUIContent MaterialType = EditorGUIUtility.TrTextContent("Type", "The material type to use for the outline effect.");
            public static readonly GUIContent CustomMaterial = EditorGUIUtility.TrTextContent("Material", "A custom material to use for rendering the outline.");
            public static readonly GUIContent WidthControl = EditorGUIUtility.TrTextContent("Width Control", "Use a shared width or a width per outline.");
            public static readonly GUIContent OutlineWidth = EditorGUIUtility.TrTextContent("Width", "The width of the outline.");
            public static readonly GUIContent OutlineGap = EditorGUIUtility.TrTextContent("Gap", "The gap between the object and the outline.");
            public static readonly GUIContent OutlineBlendMode = EditorGUIUtility.TrTextContent("Blend", "How to blend the outline with the rest of the scene.");
            public static readonly GUIContent FixBleeding = EditorGUIUtility.TrTextContent("Fix Bleeding (Experimental)", "Use a custom depth buffer to determine the occlusion state of the outlined pixels.");
            public static readonly GUIContent OutlineOccludedColor = EditorGUIUtility.TrTextContent("Occluded Color", "The color of the outline when it is occluded.");
            public static readonly GUIContent OutlineLayer = EditorGUIUtility.TrTextContent("Rendering Layer", "Only mesh renderers on this rendering layer will receive an outline.");
            public static readonly GUIContent LayerMask = EditorGUIUtility.TrTextContent("Layer Mask", "Only gameobjects on this layer will receive an outline.");
            public static readonly GUIContent RenderQueue = EditorGUIUtility.TrTextContent("Queue", "Only gameobjects using this render queue will receive an outline.");
            public static readonly GUIContent OutlineOcclusion = EditorGUIUtility.TrTextContent("Render", "For which occlusion states to render the outline.");
            public static readonly GUIContent ClosedLoop = EditorGUIUtility.TrTextContent("Closed Loop", "Whether to render a closed loop outline.");
            public static readonly GUIContent CullMode = EditorGUIUtility.TrTextContent("Cull", "The culling mode for the outline geometry.");
            public static readonly GUIContent AlphaCutout = EditorGUIUtility.TrTextContent("Alpha Cutout", "Enable alpha cutout.");
            public static readonly GUIContent AlphaCutoutTexture = EditorGUIUtility.TrTextContent("Texture", "The alpha cutout texture.");
            public static readonly GUIContent AlphaCutoutThreshold = EditorGUIUtility.TrTextContent("Threshold", "The alpha clip threshold.");
            public static readonly GUIContent AlphaCutoutUVTransform = EditorGUIUtility.TrTextContent("UV Transform", "The transform applied to the UVs (tiling x, tiling y, offset x, offset y).");
            public static readonly GUIContent GpuInstancing = EditorGUIUtility.TrTextContent("GPU Instancing", "Use GPU instancing to render this outline layer.");
            public static readonly GUIContent VertexAnimation = EditorGUIUtility.TrTextContent("Vertex Animation", "Make the outline follow the vertex animation of the mesh.");
            public static readonly GUIContent OutlineColor = EditorGUIUtility.TrTextContent("Color", "The color of the outline.");

            // === Edge Detection Styles ===
            public static readonly GUIContent DebugStage = new("Debug View", "Show specific buffers for debugging.");
            public static readonly GUIContent SectionsRawValues = new("Show Raw Values", "Display raw, non-normalized section IDs.");
            public static readonly GUIContent SectionMapPrecision = new("Precision", "The precision of the section map buffer.");
            public static readonly GUIContent SectionMapClearValue = new("Clear Value", "The value to clear the section map with.");
            public static readonly GUIContent SectionLayer = new("Section Layer", "The rendering layers to include in the section map.");
            public static readonly GUIContent SectionMapInput = new("Input", "The data source for generating section IDs.");
            public static readonly GUIContent VertexColorChannel = new("Vertex Color Channel", "The vertex color channel to use.");
            public static readonly GUIContent SectionTexture = new("Texture", "The texture to read section IDs from.");
            public static readonly GUIContent SectionTextureUVSet = new("UV Set", "The UV set for the section texture.");
            public static readonly GUIContent SectionTextureChannel = new("Texture Channel", "The texture channel to use.");
            public static readonly GUIContent ObjectId = new("Object ID", "Generate unique IDs per object.");
            public static readonly GUIContent Particles = new("Particles", "Generate unique IDs per particle.");
            public static readonly GUIContent DiscontinuityInput = new("Sources", "The buffers to use for detecting edges.");
            public static readonly GUIContent MaskLayer = new("Mask Layer", "Rendering layers to mask out the effect.");
            public static readonly GUIContent MaskInfluence = new("Mask Influence", "Which edge sources are affected by the mask.");
            public static readonly GUIContent Sensitivity = new("Sensitivity", "How sensitive the edge detection is.");
            public static readonly GUIContent DepthDistanceModulation = new("Distance Modulation", "Adjusts sensitivity based on distance from the camera.");
            public static readonly GUIContent GrazingAngleMaskPower = new("Grazing Angle Power", "Controls the influence of grazing angles on depth sensitivity.");
            public static readonly GUIContent GrazingAngleMaskHardness = new("Grazing Angle Hardness", "Controls the hardness of the grazing angle mask.");
            public static readonly GUIContent Kernel = new("Operator", "The edge detection algorithm to use.");
            public static readonly GUIContent OutlineThickness = new("Thickness", "The thickness of the outline in pixels.");
            public static readonly GUIContent ScaleWithResolution = new("Scale With Resolution", "Scales the outline thickness based on screen resolution.");
            public static readonly GUIContent EdgeColor = new("Color", "The main color of the outline.");
            public static readonly GUIContent OverrideShadow = new("Override In Shadow", "Use a different color for outlines in shadow.");
            public static readonly GUIContent BackgroundColor = new("Background Color", "The color of non-edge pixels.");
            public static readonly GUIContent OutlineFillColor = new("Fill Color", "The color to fill objects with.");
            public static readonly GUIContent FadeByDistance = new("Fade By Distance", "Fade the outline based on distance from the camera.");
            public static readonly GUIContent FadeStart = new("Fade Start", "The distance at which fading begins.");
            public static readonly GUIContent FadeDistance = new("Fade Distance", "The length over which the fade occurs.");
            public static readonly GUIContent FadeColor = new("Fade Color", "The color to fade to.");
            public static readonly GUIContent FadeByHeight = new("Fade By Height", "Fade the outline based on world-space Y position.");
        }

        public static void OpenInspectorWindow(Object obj)
        {
#if UNITY_EDITOR
            UnityEditor.Selection.activeObject = obj;
#endif
        }
    }
}
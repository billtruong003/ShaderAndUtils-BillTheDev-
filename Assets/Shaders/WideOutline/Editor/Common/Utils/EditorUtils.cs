using UnityEngine;
using UnityEditor;

// FILE HỢP NHẤT: Giữ nguyên namespace gốc để dùng chung
namespace BillTheDev.Editor.BillOutline.Common.Utils
{
    public static class EditorUtils
    {
        public static class CommonStyles
        {
            // === General & Wide Outline Styles ===
            public static readonly GUIContent InjectionPoint = new("Injection Point", "Determines where in the render pipeline the effect is rendered.");
            public static readonly GUIContent ShowInSceneView = new("Show In Scene View", "Whether to show the effect in the Scene View.");
            public static readonly GUIContent Outlines = new("Outlines", "The list of outlines to render.");
            public static readonly GUIContent MaterialType = new("Material Type", "The type of material to use for rendering the outline.");
            public static readonly GUIContent CustomMaterial = new("Custom Material", "A custom material to use for rendering the outline.");
            public static readonly GUIContent WidthControl = new("Width Control", "Determines whether the outline width is shared or per-outline.");
            public static readonly GUIContent OutlineWidth = new("Width", "The width of the outline in pixels.");
            public static readonly GUIContent OutlineGap = new("Gap", "The gap from the object's edge to the start of the outline.");
            public static readonly GUIContent OutlineBlendMode = new("Blend Mode", "The blending mode for the outline.");
            public static readonly GUIContent FixBleeding = new("Fix Bleeding", "Use a custom depth buffer to prevent the outline from bleeding through objects. May impact performance.");
            public static readonly GUIContent OutlineOccludedColor = new("Occluded Color", "The color of the outline when it is occluded by other objects.");
            public static readonly GUIContent OutlineLayer = new("Rendering Layer", "The rendering layer mask to use for this outline.");
            public static readonly GUIContent LayerMask = new("Layer Mask", "The layer mask to use for this outline.");
            public static readonly GUIContent RenderQueue = new("Render Queue", "The render queue to use for this outline.");
            public static readonly GUIContent OutlineOcclusion = new("Occlusion", "How the outline should be occluded by other objects.");
            public static readonly GUIContent ClosedLoop = new("Closed Loop", "Ensures the outline forms a closed loop, which can help with certain occlusion artifacts.");
            public static readonly GUIContent CullMode = new("Culling", "The culling mode for the outline geometry.");
            public static readonly GUIContent AlphaCutout = new("Alpha Cutout", "Use an alpha texture to clip parts of the object.");
            public static readonly GUIContent AlphaCutoutTexture = new("Texture", "The alpha texture to use for cutout.");
            public static readonly GUIContent AlphaCutoutThreshold = new("Threshold", "The alpha threshold for the cutout.");
            public static readonly GUIContent AlphaCutoutUVTransform = new("UV Transform", "The tiling and offset for the alpha cutout texture.");
            public static readonly GUIContent GpuInstancing = new("GPU Instancing", "Use GPU instancing to render the outlines. This can improve performance but may break SRP batching.");
            public static readonly GUIContent VertexAnimation = new("Vertex Animation", "Indicates that the object has vertex animation. The outline color should be set by the object's shader.");
            public static readonly GUIContent OutlineColor = new("Color", "The color of the outline.");

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
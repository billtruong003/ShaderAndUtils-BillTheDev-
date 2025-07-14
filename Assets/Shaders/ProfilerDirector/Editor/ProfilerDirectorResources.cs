using UnityEngine;
using UnityEditor;
using System;

namespace BillTheDev.ProfilerDirector
{
    internal static class ProfilerDirectorResources
    {
        public static GUIStyle BaseStyle { get; private set; }
        public static GUIStyle TitleStyle { get; private set; }
        public static GUIStyle ValueStyle { get; private set; }
        public static GUIStyle HeaderStyle { get; private set; }
        public static GUIStyle ShaderNameStyle { get; private set; }
        public static GUIStyle ToggleStyle { get; private set; }
        public static Texture2D GaugeIcon { get; private set; }
        public static Texture2D PassIcon { get; private set; }
        public static Texture2D VertIcon { get; private set; }
        public static Texture2D TriIcon { get; private set; }
        public static Texture2D MatIcon { get; private set; }
        public static Shader HeatmapOverlayShader { get; private set; }
        private static bool _areResourcesInitialized;

        public static void Initialize()
        {
            if (_areResourcesInitialized) return;

            BaseStyle = new GUIStyle(EditorStyles.label) { richText = true, alignment = TextAnchor.MiddleLeft, normal = { textColor = Color.white } };
            TitleStyle = new GUIStyle(BaseStyle) { fontStyle = FontStyle.Bold };
            ValueStyle = new GUIStyle(BaseStyle) { alignment = TextAnchor.MiddleRight };
            HeaderStyle = new GUIStyle(BaseStyle) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            ShaderNameStyle = new GUIStyle(BaseStyle) { alignment = TextAnchor.MiddleRight };
            ToggleStyle = new GUIStyle(EditorStyles.toolbarButton) { fontSize = 10, fixedHeight = 18 };

            GaugeIcon = CreateTextureFromBase64("iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAADISURBVEhL7dOxCsJgEAXQXbvoIYiL+Cg6OInawsWlnaXo7uLg5ir4GA6ubaGgKBa+iIOdwMGe/wTflxce+GAmoGEYy9YF7KsgkHCO4xM2GKcKzpgMViWc4zyxAgpYQAHfFBCKkU9KKf8XwMYgC3j5wB2E+Z0LAk9U4K+g/Lso4JIRBbyUK2AIhlyy82EWSHm+z0/K/CgLOHkEbejj+yV8pIAnGlDCaTj1i2hACScp8ElIgeI/UzC4LwUT9kMglmAaFkYxx3j/y8A1nZc1SbsAAAAASUVORK5CYII=");
            PassIcon = CreateTextureFromBase64("iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAACNSURBVEhL7daxCYAwEAXRPUUXsIKjOITZzV0c3F0dxEEcxIWc5V5CgpoEifdwkBe8/CYhgNbW0s35JS2sJcAbxBzjVn3gI5hYxKk/eKOsxBm+cUMpY4IQTowBCmFCiBAnhCVhQggnZgQnhAlhQggnZgSnhAlhQgiXjgnPGEPGEUYI4/AV4A0S9hQBLG/kKgAAAABJRU5ErkJggg==");
            VertIcon = CreateTextureFromBase64("iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAACWSURBVEhL7daxCgAgEATAs3gED+GBPBQHsbiKR/CwE3gYjwN4jI2l9E+E8WJvLpALnmzyx4QpG0CEEG3tK+P50Q14GgJ4A2HGOP/eA0+A4Ygjjz4Q0xhjxBEHeEAwxxhzhAGGMMYcYAQyxhxghDLGGGeMEMYcYQQzxhxghDPG+acJ4xlz/gd4A2gBE7TEG14mAAAAAElFTkSuQmCC");
            TriIcon = CreateTextureFromBase64("iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAABzSURBVEhL7dNbCoAgEAXQe49e2Ae9eDAv9qIHW3sQ3KIoAYpMkgQG+7Bv+JEMC+wAAwD4/akpG7OwsKywKDEsMSoKLDosCqw6LAosCCwKLPosSgRLLIoCSy2LAssyS4pYltmSBLHIkpRj/gC/gQoO6wJ6w44XAAAAAElFTkSuQmCC");
            MatIcon = CreateTextureFromBase64("iVBORw0KGgoAAAANSUhEUgAAABgAAAAYCAYAAADgdz34AAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAACDSURBVEhL7dNBCsAgDIXQ4x56YB/0Yl7sRR/sRT+YyksQkpCEgBTo4C/sD/xEIm2tO2Q6aQvYSiBfCHPMm/uAXkAixj39wS+kEsaYMQpCBDEmhhBDhAlhQghxYgpMkCFChBDixBSYIEOECCHEialwxlR4xohlhBHm8BfwC9sUARsWJjJ4AAAAAElFTkSuQmCC");

            HeatmapOverlayShader = Shader.Find("Hidden/ProfilerDirector/HeatmapOverlay");
            if (HeatmapOverlayShader == null)
            {
                Debug.LogError("Profiler Director: HeatmapOverlay shader not found. Please ensure 'ProfilerDirectorHeatmapOverlay.shader' is in a Resources folder.");
            }

            _areResourcesInitialized = true;
        }

        public static void Dispose()
        {
            if (!_areResourcesInitialized) return;
            UnityEngine.Object.DestroyImmediate(GaugeIcon);
            UnityEngine.Object.DestroyImmediate(PassIcon);
            UnityEngine.Object.DestroyImmediate(VertIcon);
            UnityEngine.Object.DestroyImmediate(TriIcon);
            UnityEngine.Object.DestroyImmediate(MatIcon);
            _areResourcesInitialized = false;
        }

        private static Texture2D CreateTextureFromBase64(string base64)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            tex.LoadImage(Convert.FromBase64String(base64));
            tex.hideFlags = HideFlags.HideAndDontSave;
            return tex;
        }
    }
}
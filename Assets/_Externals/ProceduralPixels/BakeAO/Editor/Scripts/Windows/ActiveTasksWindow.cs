/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

﻿using UnityEditor;
using System;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    internal class ActiveTasksWindow : EditorWindow
    {
        private static ActiveTasksWindow openedWindow = null;
        private const string windowIconGUID = "4ddfaa2a48125ff4288ea52083181fa5";
        public static void FocusOrOpen(Type dockToType)
        {
            if (openedWindow != null)
                openedWindow.Focus();
            else
                Open(dockToType);
        }

        [MenuItem("Window/Procedural Pixels/Bake AO/Active tasks")]
        public static void Open()
        {
            if (openedWindow != null)
            {
                openedWindow.Close();
                openedWindow = null; 
            }

            openedWindow = GetWindow<ActiveTasksWindow>(false, "BakeAO - Active tasks", true);
        }

        public static void Open(Type dockTo)
        {
            if (openedWindow != null)
            {
                openedWindow.Close();
                openedWindow = null;
            }

            openedWindow = GetWindow<ActiveTasksWindow>("BakeAO - Active tasks", true, dockTo);
        }

        private void OnEnable()
        {
            minSize = new Vector2(400, 280);
            EditorApplication.update += OnEditorUpdate;
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(windowIconGUID));
            this.titleContent = new GUIContent("BakeAO - Active tasks", icon);
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            openedWindow = null;
        }

        private void OnEditorUpdate()
        {
            Repaint();
        }

        private void OnGUI()
        {
            BakingManager.instance.DrawGUI(position);
        }
    }
}
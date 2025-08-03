/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    public static class GUIUtility
    {
        public static GUIContent _helpIcon;
        public static GUIContent HelpIcon => _helpIcon != null ? _helpIcon : (_helpIcon = EditorGUIUtility.IconContent("_Help"));
        public static void HelpButton(Rect rect, string url)
        {
            if (GUI.Button(rect, HelpIcon, EditorStyles.iconButton))
                Application.OpenURL(url);
        }
    } 
}

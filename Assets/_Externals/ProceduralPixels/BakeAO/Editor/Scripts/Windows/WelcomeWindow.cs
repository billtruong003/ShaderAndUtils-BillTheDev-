/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using UnityEngine;
using UnityEditor;
using System;

namespace ProceduralPixels.BakeAO.Editor
{
    public class WelcomeWindow : EditorWindow
    {
        public enum CloseReason
        {
            AssemblyReload,
            BuiltInButton,
            CloseButton
        }

        public enum CloseMode
        {
            DontShowThisWindowAgain,
            DontShowAgainDuringThisSession,
            DontShowAgainForTheNextDay,
        }

        static WelcomeWindow()
        {
            RegisterForDelayCall();
        }

        [InitializeOnLoadMethod]
        private static void RegisterForDelayCall()
        {
            EditorApplication.delayCall += Open;
        }

        public static void Open()
        {
            if (ShouldOpenWindow())
            {
                OpenWelcomeWindow();
            }
        }

        [MenuItem("Help/Procedural Pixels/Bake AO - About")]
        private static void OpenWelcomeWindow()
        {
            if (!HasOpenInstances<WelcomeWindow>())
            {
                var window = GetWindow<WelcomeWindow>(true, "Bake AO Welcome", true);
                window.closeReason = CloseReason.BuiltInButton;
                if (GetLastCloseModePref().HasValue)
                    window.closeMode = GetLastCloseModePref().Value;
                window.maxSize = size;
                window.minSize = size;
            }
        }

        private static bool ShouldOpenWindow()
        {
            CloseMode? lastCloseMode = GetLastCloseModePref();
            if (!lastCloseMode.HasValue)
                return true;

            switch (lastCloseMode.Value)
            {
                case CloseMode.DontShowThisWindowAgain:
                    return false;
                case CloseMode.DontShowAgainDuringThisSession:
                    return (GetLastSessionID() != EditorAnalyticsSessionInfo.id);
                case CloseMode.DontShowAgainForTheNextDay:
                    return (GetDateInSeconds() - (60 * 60 * 24) > GetLastCloseTimeSeconds());
            }

            return true;
        }

        private void OnEnable()
        {
            AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
        }

        private void BeforeAssemblyReload()
        {
            closeReason = CloseReason.AssemblyReload;
        }

        private void OnDisable()
        {
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
        }

        private void OnDestroy()
        {
            if (closeReason.HasValue)
            {
                closeReason = CloseReason.BuiltInButton;
                OnCloseWindow();
            }
        }

        readonly static Vector2 size = new Vector2(600.0f, 582.0f);

        private CloseMode closeMode = CloseMode.DontShowAgainDuringThisSession;
        private CloseReason? closeReason = CloseReason.AssemblyReload;

        private void OnGUI()
        {
            maxSize = size;
            minSize = size;

            InitializeStyles();

            EditorGUILayout.LabelField("Welcome to Bake AO!", titleStyle);
            var lastRect = GUILayoutUtility.GetLastRect();
            var versionRect = new Rect(lastRect.position + new Vector2(lastRect.width - 100.0f, -6.0f), new Vector2(100.0f, lastRect.height));
            EditorGUI.LabelField(versionRect, $"Version: {BakeAO.Version}", versionStyle);
            EditorGUILayout.LabelField("by Procedural Pixels", headingStyle);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Get ready to improve your game's visuals with Bake AO - the solution for baking ambient occlusion textures right within Unity Editor.", bodyStyle);
            var imageRect = EditorGUILayout.GetControlRect(true, 300.0f);
            EditorGUI.DrawTextureTransparent(imageRect, image, ScaleMode.ScaleToFit);
            EditorGUILayout.LabelField("Enhance your game's look with just a few clicks! Explore more using the links below:", bodyStyle);
            EditorGUILayout.Space();

            if (LinkLabel(new GUIContent("Documentation     "), documentLogo)) // Yes, those spaces fix the window content xD
                OpenDocumentation();

            if (LinkLabel(new GUIContent("Asset Store     "), unityLogo))
                Application.OpenURL("https://assetstore.unity.com/packages/slug/263743");

            if (LinkLabel(new GUIContent("Website (proceduralpixels.com)     "), websiteLogo))
                Application.OpenURL("https://proceduralpixels.com");

            if (LinkLabel(new GUIContent("Discord community     "), discordLogo))
                Application.OpenURL("https://discord.gg/NT2pyQ28Jx");

            EditorGUILayout.Space();

            if (GUILayout.Button("Open baking wizard"))
                BakeAOWizardWindow.OpenWindow();

            EditorGUILayout.Space(22); 

            EditorGUILayout.BeginHorizontal();
            {
                closeMode = (CloseMode)EditorGUILayout.EnumPopup(closeMode);
                EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                if (GUILayout.Button("Close"))
                {
                    closeReason = CloseReason.CloseButton;
                    OnCloseWindow();
                    Close();
                } 
            }
            EditorGUILayout.EndHorizontal();
        }

        [MenuItem("Help/Procedural Pixels/Bake AO - Documentation")]
        private static void OpenDocumentation()
        {
            Application.OpenURL("https://proceduralpixels.com/BakeAO/Documentation/");
        }

        private const string EditorPrefsKey_CloseMode = "BakeAO-CloseMode";
        private const string EditorPrefsKey_CloseTime = "BakeAO-CloseTime";
        private const string EditorPrefsKey_SessionID = "BakeAO-SessionID";
        private static CloseMode? GetLastCloseModePref()
        {
            string pref = EditorPrefs.GetString(EditorPrefsKey_CloseMode, "");

            if (pref.Equals(CloseMode.DontShowAgainDuringThisSession.ToString()))
                return CloseMode.DontShowAgainDuringThisSession;
            else if (pref.Equals(CloseMode.DontShowAgainForTheNextDay.ToString()))
                return CloseMode.DontShowAgainForTheNextDay;
            else if (pref.Equals(CloseMode.DontShowThisWindowAgain.ToString()))
                return CloseMode.DontShowThisWindowAgain;
            else
                return null;
        }

        private static long GetLastSessionID()
        {
            string pref = EditorPrefs.GetString(EditorPrefsKey_SessionID, "");
            if (long.TryParse(pref, out long sessionID))
                return sessionID;
            else
                return -1;
        }

        private static long GetLastCloseTimeSeconds()
        {
            string pref = EditorPrefs.GetString(EditorPrefsKey_CloseTime, "0");
            if (long.TryParse(pref, out long seconds))
                return seconds;
            else
                return 0;
        }

        private static long GetDateInSeconds()
        {
            return (long)(new TimeSpan(System.DateTime.UtcNow.Ticks).TotalSeconds);
        }

        private void OnCloseWindow()
        { 
            switch (closeReason) 
            {
                case CloseReason.AssemblyReload:
                    return; // If assembly reload caused the window to close, just ignore it
                case CloseReason.BuiltInButton:
                    closeMode = GetLastCloseModePref().HasValue ? GetLastCloseModePref().Value : CloseMode.DontShowAgainDuringThisSession; // If closed by built in button, don't open this window only in this session.
                    break;
                case CloseReason.CloseButton:
                    break;
                default:
                    return;
            }

            switch (closeMode)
            {
                case CloseMode.DontShowThisWindowAgain:
                    EditorPrefs.SetString(EditorPrefsKey_CloseMode, closeMode.ToString());
                    break;
                case CloseMode.DontShowAgainDuringThisSession:
                    EditorPrefs.SetString(EditorPrefsKey_CloseMode, closeMode.ToString());
                    EditorPrefs.SetString(EditorPrefsKey_SessionID, EditorAnalyticsSessionInfo.id.ToString());
                    break;
                case CloseMode.DontShowAgainForTheNextDay:
                    EditorPrefs.SetString(EditorPrefsKey_CloseMode, closeMode.ToString());
                    EditorPrefs.SetString(EditorPrefsKey_CloseTime, GetDateInSeconds().ToString());
                    break;
            }

            closeReason = null;
        }

        bool initialized;
        GUIStyle linkStyle;
        GUIStyle titleStyle;
        GUIStyle headingStyle;
        GUIStyle bodyStyle; 
        GUIStyle versionStyle;

        Texture2D image;
        Texture2D websiteLogo;
        Texture2D discordLogo;
        Texture2D documentLogo;
        Texture2D unityLogo;

        const string imageGUID = "5d91a7964a777c44886085d1b3d23a01";
        const string websiteLogoGUID = "360100f6b7f9fe243bd43c5048ce6987";
        const string discordLogoGUID = "933043bc957850f448962c59d8a57788";
        const string documentLogoGUID = "9da20019cc5884840ae8dd483e5a0c15";

        void InitializeStyles()
        {
            if (initialized)
                return;

            bodyStyle = new GUIStyle(EditorStyles.label);
            bodyStyle.wordWrap = true; 
            bodyStyle.fontSize = 14; 

            titleStyle = new GUIStyle(bodyStyle);
            if (EditorGUIUtility.isProSkin)
                titleStyle.normal.textColor = new Color(1.0f, 0.6f, 0.0f); 
            else
                titleStyle.normal.textColor = new Color(1.0f, 0.6f, 0.0f) * new Color(0.5f, 0.5f, 0.5f, 1.0f);
            titleStyle.fontSize = 26; 

            headingStyle = new GUIStyle(bodyStyle);
            if (EditorGUIUtility.isProSkin)
                headingStyle.normal.textColor = new Color(1.0f, 0.6f, 0.0f);
            else
                headingStyle.normal.textColor = new Color(1.0f, 0.6f, 0.0f) * new Color(0.5f, 0.5f, 0.5f, 1.0f);
            headingStyle.fontSize = 18;

            linkStyle = new GUIStyle(bodyStyle);
            linkStyle.wordWrap = false;

            // Match selection color which works nicely for both light and dark skins
            linkStyle.normal.textColor = new Color(0.0f, 0.5f, 0.9f, 1f);
            linkStyle.stretchWidth = false;

            versionStyle = new GUIStyle(bodyStyle);
            versionStyle.fontSize = 12;
            versionStyle.normal.textColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

            image = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(imageGUID));
            discordLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(discordLogoGUID));
            documentLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(documentLogoGUID));
            websiteLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(websiteLogoGUID));
            unityLogo = EditorGUIUtility.IconContent("UnityLogo").image as Texture2D;

            initialized = true;
        }

        bool LinkLabel(GUIContent label, Texture icon, params GUILayoutOption[] options)
        {
            EditorGUILayout.BeginHorizontal();
            var position = GUILayoutUtility.GetRect(label, linkStyle);
            Rect iconRect = new Rect(position.position, new Vector2(16, position.height));
            position = new Rect(position.position + new Vector2(20.0f, 0.0f), position.size - new Vector2(20, 0));

            Handles.BeginGUI();
            Handles.color = linkStyle.normal.textColor;
            Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
            Handles.color = Color.white;
            Handles.EndGUI();

            EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

            GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);

            bool click = GUI.Button(position, label, linkStyle);
            EditorGUILayout.EndHorizontal();
            return click;
        }
    }
}

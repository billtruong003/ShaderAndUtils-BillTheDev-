/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using JetBrains.Annotations;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    public class URPSupportInstaller : EditorWindow
    {
        public const string URP12SupportGUID = "864111a15a126c443b731078721406fb";
        public const string URP13SupportGUID = "f62ecaeda58b742488ebe658fdb9434b";
        public const string URP14SupportGUID = "e2d4ab0a9c78be649ba4d3a01f1dcc2a";
        public const string URP15SupportGUID = "8a2f5a687e9df8348969cf2a833d9301";
        public const string URP16SupportGUID = "22b4fe3d5f67ce14abefadd801860599";
        public const string URP17SupportGUID = "bb6e35de6cebf594da7347cfd7d7a568";

        public Dictionary<int, string> versionToGUID = null;

        private static ListRequest listRequest;

        [SerializeField] private bool requestRestart = false;

        private void OnEnable()
        {
            versionToGUID = new Dictionary<int, string>
            {
                { 12, URP12SupportGUID },
                { 13, URP13SupportGUID },
                { 14, URP14SupportGUID },
                { 15, URP15SupportGUID },
                { 16, URP16SupportGUID },
                { 17, URP17SupportGUID }
            };

            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        [MenuItem("Window/Procedural Pixels/Bake AO/URP Support Installer")]
        public static void Open()
        {
            CreateWindow<URPSupportInstaller>("URP Support Installer");
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Open Documentation"))
                Application.OpenURL("https://proceduralpixels.com/BakeAO/Documentation/URPSupportInstaller");

            EditorGUILayout.HelpBox("This installer will help you install URP support for Bake AO in your project.", MessageType.Info, true);

            if (listRequest == null || !listRequest.IsCompleted)
            {
                CheckURPInstallation();
                EditorGUILayout.HelpBox("Checking installed URP version, please wait...", MessageType.Info);
                return;
            }



            if (listRequest.Status == StatusCode.Success)
            {
                bool isURPInstalled = false;
                bool isURPSupportInstalled = false;
                string installedURPVersion = "";
                string installedURPSupportVersion = "";

                var urpPackage = listRequest.Result.FirstOrDefault(package => package.name.Contains("com.unity.render-pipelines.universal", System.StringComparison.InvariantCultureIgnoreCase));
                isURPInstalled = urpPackage != null;
                if (isURPInstalled)
                    installedURPVersion = urpPackage.version;

                var supportVersionField = TypeCache.GetFieldsWithAttribute(typeof(BakeAOURPSupportVersionAttribute)).FirstOrDefault();
                if (supportVersionField != null)
                {
                    isURPSupportInstalled = true;
                    installedURPSupportVersion = supportVersionField.GetValue(null) as string;
                }

                if (isURPInstalled)
                {
                    EditorGUILayout.LabelField("URP is installed. Detected Version: " + installedURPVersion);

                    if (isURPSupportInstalled)
                    {
                        EditorGUILayout.LabelField("Bake AO URP Support is installed. Detected version: " + installedURPSupportVersion);
                        ShowURPSupportOptions(installedURPVersion, installedURPSupportVersion);
                    }
                    else
                    {
                        EditorGUILayout.LabelField("Bake AO support for URP is not installed.");
                        if (GUILayout.Button("Install URP Support"))
                            InstallURPSupport(installedURPVersion);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("URP is not installed. Please install URP using Package Manager first.", MessageType.Warning);
                }
            }
            else if (listRequest.Status >= StatusCode.Failure)
            {
                EditorGUILayout.HelpBox("Failed to check installed packages: " + listRequest.Error.message, MessageType.Error);
            }
        }

        private void RestartEditor()
        {
            // Restart the editor by reopening the current project, but save the changed assets firtst
            AssetDatabase.SaveAssets();
            string projectPath = Path.GetDirectoryName(Application.dataPath);
            EditorApplication.OpenProject(projectPath);
        }

        private void CheckURPInstallation()
        {
            if (listRequest == null || listRequest.Status == StatusCode.Failure)
            {
                listRequest = Client.List(); // Initiates an asynchronous request to list all installed packages

            }
        }

        private void OnEditorUpdate()
        {

            if (listRequest != null && listRequest.IsCompleted)
            {
                Repaint();
            }

            if (requestRestart)
            {
                bool shouldRestart = EditorUtility.DisplayDialog("Restart required", "Bake AO support for URP was installed. You need to restart the editor to make Bake AO support for URP work correctly.", "Save project and restart", "Ignore");
                requestRestart = false;
                if (shouldRestart)
                    RestartEditor();
            }
        }

        private void ShowURPSupportOptions(string urpVersion, string supportVersion)
        {
            bool doesSupportExist = versionToGUID.ContainsKey(GetMajorVersion(urpVersion));
            if (!doesSupportExist)
            {
                EditorGUILayout.HelpBox("Bake AO support for currently installed URP does not exist. If you think that this error should not exist, please contact me at dev@proceduralpixels.com", MessageType.Error, true);
                return;
            }

            if (GetMajorVersion(supportVersion) != GetMajorVersion(urpVersion))
            {
                EditorGUILayout.HelpBox("URP Support is not matching installed URP version. Please update.", MessageType.Warning);

                if (GUILayout.Button("Update URP Support"))
                {
                    UninstallURPSupport();
                    InstallURPSupport(urpVersion);
                }
            }
            else
            {
                if (GUILayout.Button("Uninstall URP Support"))
                {
                    UninstallURPSupport();
                    EditorUtility.DisplayDialog("Bake AO support for URP", "Bake AO support for URP was uninstalled", "Ok");
                }
            }
        }

        private void UninstallURPSupport()
        {
            var folderGUIDField = TypeCache.GetFieldsWithAttribute<BakeAOURPSupportFolderGUIDAttribute>().FirstOrDefault();
            if (folderGUIDField == null)
                Debug.LogError("No folder found for installed Bake AO URP support. It was probably manually modified.");

            var folderPath = AssetDatabase.GUIDToAssetPath(folderGUIDField.GetValue(null) as string);
            if (!string.IsNullOrEmpty(folderPath) && AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.DeleteAsset(folderPath);
                AssetDatabase.Refresh();
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                listRequest = null;
            }
            else
                Debug.LogError("No folder found for installed Bake AO URP support. It was probably manually modified. To uninstall Bake AO URP Support, find the folder URPxx, where xx is the major URP version and delete this folder manually.");
        }

        private void InstallURPSupport(string urpVersion)
        {
            var packageGUID = versionToGUID[GetMajorVersion(urpVersion)];
            var packagePath = AssetDatabase.GUIDToAssetPath(packageGUID);
            AssetDatabase.ImportPackage(packagePath, false);
            listRequest = null;
            requestRestart = true;
        }

        private int GetMajorVersion(string fullVersion)
        {
            try
            {
                if (int.TryParse(fullVersion.Split('.')[0], out int majorVersion))
                    return majorVersion;
                else
                    return -1;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error when parsing major version of ${fullVersion}\nException:\n{e.Message}\n{e.StackTrace}");
            }

            return -1;
        }

    }

    // Any const in the code with this attribute should contain installed URP support version. If no field exists, there is no URP support installed.
    public class BakeAOURPSupportVersionAttribute : System.Attribute
    { }

    // Any const in the code with this attribute should contain installed URP support folder GUID
    public class BakeAOURPSupportFolderGUIDAttribute : System.Attribute
    {

    }
}

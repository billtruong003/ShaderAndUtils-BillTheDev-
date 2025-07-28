#region Namespaces

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Utilities;
using MVC.Internal;
using MVC.Internal.Editor;

using Object = UnityEngine.Object;

#endregion

namespace MVC.Editor
{
	[CustomEditor(typeof(ToolkitSettings))]
	public class ToolkitSettingsEditor : ToolkitBehaviourEditor
	{
		#region Modules

		private struct AssetGUID
		{
			#region Variables

			public long fileID;
			public string guid;
			public int type;

			#endregion

			#region Methods

			public override readonly bool Equals(object obj)
			{
				return obj is AssetGUID GUID &&
						fileID == GUID.fileID &&
						guid == GUID.guid &&
						type == GUID.type;
			}
			public override readonly int GetHashCode()
			{
				return HashCode.Combine(fileID, guid, type);
			}

			#endregion

			#region Operators

			public static bool operator ==(AssetGUID a, AssetGUID b)
			{
				return a.Equals(b);
			}
			public static bool operator !=(AssetGUID a, AssetGUID b)
			{
				return !(a == b);
			}

			#endregion

			#region Constructors

			public AssetGUID(long fileID, string guid, int type)
			{
				this.fileID = fileID;
				this.guid = guid;
				this.type = type;
			}

			#endregion
		}

		#endregion

		#region Variables

		#region Static Variables

		public static string FullAssetPath
		{
			get
			{
				string assetName = Path.GetFileName(ToolkitSettings.AssetPath);
				string[] assetPaths = AssetDatabase.FindAssets(assetName, new string[] { "Assets/" }).Select(guid => AssetDatabase.GUIDToAssetPath(guid).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)).ToArray();
				string assetResourcesPath = Path.Combine("Resources", ToolkitSettings.AssetPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

				return assetPaths.FirstOrDefault(path => Path.GetFileNameWithoutExtension(path) == assetName && path.Contains(assetResourcesPath));
			}
		}

		private static readonly Dictionary<string, AssetGUID> DLLScriptGUIDs = new()
		{
			{ "ToolkitBehaviour", new(-870620321L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "ToolkitBehaviourExtension`1", new(-1916904778L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "ToolkitScriptableObject", new(219461184L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "ToolkitSettings", new(1278821849L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "Vehicle", new(-59341653L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleAI", new(-1357007334L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleAIPath", new(1316760407L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleAIZone", new(2075908664L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleAudioSource", new(1465263057L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleAudioZone", new(-211443923L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleCameraPivot", new(881450874L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleChassis", new(-1111805404L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleDamageZone", new(-1705925379L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleDriver", new(-2087680280L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleFollower", new(-2028803727L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleFollowerPivot", new(881450874L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleGroundMapper", new(-975421185L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleIKPivot", new(-1227526772L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleLightSource", new(-1073286560L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleManager", new(-676771069L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleTrailer", new(-1236221970L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleTrailerLink", new(-1763949111L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleUIController", new(-1348322033L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleWeatherZone", new(-1935017210L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleWheel", new(-1041476857L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleWheelMark", new(-841338471L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleWheelSpin", new(413881835L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleZone", new(-2074915350L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) },
			{ "VehicleZonesContainer", new(977046559L, "7c0bac27308dcf24eb0bf21c68fb9c41", 3) }
		};
		private static readonly Dictionary<string, AssetGUID> OpenSourceScriptGUIDs = new()
		{
			{ "ToolkitBehaviour", new(11500000L, "d4f9ab1e6331c5b408422bdac8b9d335", 3) },
			{ "ToolkitBehaviourExtension", new(11500000L, "4a8ae6c78faf01648aceb7e7f738cb78", 3) },
			{ "ToolkitScriptableObject", new(11500000L, "767ce2235a548e342b5c46cf2b6cbfcd", 3) },
			{ "ToolkitSettings", new(11500000L, "880dd1f8248774449b1e072bc742765a", 3) },
			{ "Vehicle", new(11500000L, "3c7ba39d267f137459d1640ee4673d97", 3) },
			{ "VehicleAI", new(11500000L, "001dd5d8802dc7d429082436cd40d25c", 3) },
			{ "VehicleAILayer", new(11500000L, "87f9725a99bf6264bb300447d966bc38", 3) },
			{ "VehicleAIPath", new(11500000L, "7b499d24220470047be2accc06c6a1cd", 3) },
			{ "VehicleAIZone", new(11500000L, "3a2fface626421b43a3eb3218ac0c067", 3) },
			{ "VehicleAudioSource", new(11500000L, "a9c32c6954b0da14e99403ea9627d4ce", 3) },
			{ "VehicleAudioZone", new(11500000L, "32de522e7ee8f7b42a5c69f3b0f6ca22", 3) },
			{ "VehicleCameraPivot", new(11500000L, "da31fbe4dbb5614418fcf649fc07b2c3", 3) },
			{ "VehicleFollowerPivot", new(11500000L, "da31fbe4dbb5614418fcf649fc07b2c3", 3) },
			{ "VehicleChassis", new(11500000L, "5257045fb408008419619be4624b4d5b", 3) },
			{ "VehicleDamageZone", new(11500000L, "dfb50f68879dc534a9b078ce08fb1df2", 3) },
			{ "VehicleDriver", new(11500000L, "939960585f954164e96d5c9cf05441ae", 3) },
			{ "VehicleFollower", new(11500000L, "efdce6ad816f14a479f13cc1263a19dd", 3) },
			{ "VehicleGroundMapper", new(11500000L, "2deab5d4de7ad3147a293a9968f511c6", 3) },
			{ "VehicleIKPivot", new(11500000L, "3596b750a6293654aa9b7a02e00747e5", 3) },
			{ "VehicleLightSource", new(11500000L, "716a84112255c42488d6f554a3cbad78", 3) },
			{ "VehicleManager", new(11500000L, "6af017a68d200a041a35022fa9bc5d3f", 3) },
			{ "VehicleTrailer", new(11500000L, "20fc1e4d0e863e94a8a84631ce94b6c0", 3) },
			{ "VehicleTrailerLink", new(11500000L, "608a82b0ac2084e4a9b09c3d74475f17", 3) },
			{ "VehicleUIController", new(11500000L, "5c81315658f14c64eac2fc99d289cf94", 3) },
			{ "VehicleWeatherZone", new(11500000L, "47d2e3a1024a3a54dbf64f8782d50a64", 3) },
			{ "VehicleWheel", new(11500000L, "5631fbb23f6c8404bbbd328112d94384", 3) },
			{ "VehicleWheelMark", new(11500000L, "d35fd2aa3efc5f4428ebd8964b2efd99", 3) },
			{ "VehicleWheelSpin", new(11500000L, "9a9bb7ca082fd5a4296a77281e6ccdf4", 3) },
			{ "VehicleZone", new(11500000L, "9ead068d0876a5744b1c8ed12d767c7c", 3) },
			{ "VehicleZonesContainer", new(11500000L, "0d526d1cb6d6cfd40bbc33c36a202b5e", 3) }
		};

		#endregion

		#region Global Variables

		private string InstancePath => AssetDatabase.GetAssetPath(target).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		private bool IsValidInstance => InstancePath.StartsWith(Path.Combine("Assets", "BxB Studio", "MVC", "Resources", ToolkitSettings.AssetPath.Replace(Path.GetFileName(ToolkitSettings.AssetPath), "")));
		private bool IsActiveInstance => IsValidInstance && InstancePath.EndsWith($"{Path.DirectorySeparatorChar}{ToolkitSettings.AssetPath}.asset");

		#endregion

		#endregion

		#region Methods

		#region Virtual Methods

		public override void OnInspectorGUI()
		{
			EditorGUILayout.Space();

			#region Messages

			Color orgGUIBackgroundColor = GUI.backgroundColor;

			if (HasInternalErrors)
			{
				GUI.backgroundColor = Color.red;

				EditorGUILayout.HelpBox("The Multiversal Vehicle Controller is facing some internal problems that need to be fixed!", MessageType.Error);

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (GUILayout.Button("Try some quick fixes"))
					FixInternalProblems(target);

				EditorGUILayout.Space();

				return;
			}
			else if (!IsSetupDone)
			{
				GUI.backgroundColor = Color.green;

				EditorGUILayout.HelpBox("It seems like the Multiversal Vehicle Controller is not ready for use yet!", MessageType.Info);

				GUI.backgroundColor = orgGUIBackgroundColor;

				if (GUILayout.Button("What's going on?"))
					OpenWindow();

				EditorGUILayout.Space();

				return;
			}

			#endregion

			EditorGUI.indentLevel--;

			EditorGUILayout.LabelField("Multiversal Vehicle Controller\r\nSettings Asset", new GUIStyle(EditorStyles.boldLabel) { fixedHeight = EditorGUIUtility.singleLineHeight * 2f, alignment = TextAnchor.MiddleCenter });
			EditorGUILayout.Space();
			EditorGUILayout.Space();
			EditorGUILayout.Space();

			EditorGUI.indentLevel++;

			if (!IsValidInstance)
			{
				EditorGUILayout.HelpBox($"This asset is invalid because it's not a child of the following path: \"{Path.Combine("Assets", "BxB Studio", "MVC", "Resources", ToolkitSettings.AssetPath.Replace(Path.GetFileName(ToolkitSettings.AssetPath), "")).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)}\".", MessageType.Warning);

				return;
			}

			if (IsActiveInstance)
				EditorGUILayout.HelpBox("This asset contains the currently active settings for the Multiversal Vehicle Controller.", MessageType.Info);
			else
				EditorGUILayout.HelpBox("This asset is used to save older settings data and not as an active asset!", MessageType.Info);

			EditorGUI.BeginDisabledGroup(!IsActiveInstance);

			if (GUILayout.Button("Open MVC Settings Window", new GUIStyle(GUI.skin.button) { fixedHeight = 35f }))
				OpenWindow();

			EditorGUI.EndDisabledGroup();

			if (Selection.objects.Length < 2)
			{
				if (!IsActiveInstance)
				{
					if (GUILayout.Button("Make this asset as Active", new GUIStyle(GUI.skin.button) { fixedHeight = 35f }))
						RestoreSettings(InstancePath.Replace(Path.Combine("Assets", "BxB Studio", "MVC", "Resources").Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, "").Replace(".asset", ""));
				}
				else if (GUILayout.Button("Create a Settings Data Backup ", new GUIStyle(GUI.skin.button) { fixedHeight = 35f }))
					BackupSettings();
			}
		}

		#endregion

		#region Static Methods

		#region Menu Items

		[MenuItem("Tools/Multiversal Vehicle Controller/Edit Settings", false, 0)]
		public static ToolkitSettingsEditorWindow OpenWindow()
		{
			if (EditorWindow.HasOpenInstances<ToolkitSettingsEditorWindow>())
			{
				EditorWindow.FocusWindowIfItsOpen<ToolkitSettingsEditorWindow>();

				return ToolkitSettingsEditorWindow.instance;
			}

			string title = "MVC Settings";

			ToolkitSettingsEditorWindow.instance = EditorWindow.GetWindow<ToolkitSettingsEditorWindow>(false, title, true);
			ToolkitSettingsEditorWindow.instance.titleContent = new(title, EditorGUIUtility.IconContent("Settings").image);
			ToolkitSettingsEditorWindow.instance.minSize = new(800f, 600f);

			if (Settings && !SettingsEditorResetAndBackupCheck())
				FixInternalProblems(Selection.activeObject);

			return ToolkitSettingsEditorWindow.instance;
		}
		public static bool ResetSettings()
		{
			if (!SettingsEditorResetAndBackupCheck())
				return false;

			bool result = false;

			if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Are you sure you want to reset the MVC Settings?", "Yes I'm sure!", "No thanks"))
			{
				if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "In order to keep your files safe, Do you want to create a backup from the original 'MVCSettings_Data' file?", "Yes", "No"))
					BackupSettings();

				if (RecreateAssetFile(false))
					result = EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The MVC Settings have been reset to their original state.", "Okay");
				else
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", $"Oops! We are very sorry but we couldn't reset the MVC Settings to their original state.\r\nHint: You can check for the \"{Path.Combine("Assets", "BxB Studio", "MVC", "Resources", "Settings")}\" directory existence.", "Okay");
			}

			return result;
		}
		[MenuItem("Tools/Multiversal Vehicle Controller/Install\\Repair", false, 2)]
		public static void ResetSetup()
		{
			if (!ResetSetupCheck())
				return;

			if (IsSetupDone)
			{
				if (!EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You are about to reset all MVC setup parameters! Do you want to proceed?\r\nNote: No assets are going to be removed from your project, but some might be overridden.", "Yes", "No"))
					return;

				ToolkitPrefs.DeleteFlag(ToolkitConstants.SetupDone);
			}

			ToolkitSetupWizardWindow.OpenWindow();
		}
		[MenuItem("Tools/Multiversal Vehicle Controller/Backup/Create", false, 3)]
		public static bool BackupSettings()
		{
			if (!IsSetupDone || !Settings)
				return false;

			if (BackupAsset)
				return EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "We have found an old backup asset. Do you want to override it?", "Yes", "No") ? RecreateAssetFile(true) : false;

			return RecreateAssetFile(true);
		}
		[MenuItem("Tools/Multiversal Vehicle Controller/Backup/Restore", false, 4)]
		public static bool RestoreSettings()
		{
			if (!RestoreSettingsCheck())
				return false;

			return RestoreSettings($"{ToolkitSettings.AssetPath}_Backup");
		}
		[MenuItem("Tools/Multiversal Vehicle Controller/Fix Missing Components", false, 5)]
		public static void FixMissingComponents()
		{
			FixMissingComponents(true, true);
		}

		[MenuItem("Tools/Multiversal Vehicle Controller/Install\\Repair", true)]
		protected static bool ResetSetupCheck()
		{
			return !EditorApplication.isPlaying;
		}
		[MenuItem("Tools/Multiversal Vehicle Controller/Backup/Restore", true)]
		protected static bool RestoreSettingsCheck()
		{
			return !EditorApplication.isPlaying && IsSetupDone && BackupAsset;
		}
		[MenuItem("Tools/Multiversal Vehicle Controller/Backup/Create", true)]
		[MenuItem("Tools/Multiversal Vehicle Controller/Reset Settings", true)]
		protected static bool SettingsEditorResetAndBackupCheck()
		{
			return !EditorApplication.isPlaying && IsSetupDone && Settings && ToolkitSettings.LoadData(true);
		}

		#endregion

		#region Utilities

		public static ToolkitSettingsEditorWindow OpenWindow(ToolkitSettings.SettingsEditorFoldout foldout)
		{
			Settings.settingsFoldout = foldout;

			return OpenWindow();
		}
		public static ToolkitSettingsEditorWindow OpenWindow(ToolkitSettings.SettingsEditorSFXFoldout sfxFoldout)
		{
			Settings.soundEffectsFoldout = sfxFoldout;

			return OpenWindow(ToolkitSettings.SettingsEditorFoldout.SFX);
		}
		public static void SaveSettings()
		{
			if (!Settings)
				return;

			AssetDatabase.SaveAssetIfDirty(Settings);
		}
		public static bool RestoreSettings(string path)
		{
			if (!Resources.Load(path))
				return !EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", $"Welp, it seems like the desired Settings Data asset has been deleted or doesn't exist anymore! Please check your file location and try again.\r\n\"{path}\"", "Okay");

			bool result = false;

			if (Settings)
			{
				if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Do you really want to replace the current active Settings Data asset with another one?", "Yes I do", "No thank you!"))
					result = DeleteSettingsAsset(ToolkitSettings.AssetPath);
				else
					return result;

				if (!result)
					return !EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "Sorry but we couldn't delete the existent active Settings Data asset! It may be deleted or inaccessible.", "Okay");
			}

		retry_restore:
			result = RestoreSettingsAsset(path);

			if (result)
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The backup asset has been restored successfully!", "Okay");
			else if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", $"We have been failed to restore the Settings Data asset: \"{Path.GetFileNameWithoutExtension(path)}\"!", "Retry", "Cancel"))
				goto retry_restore;

			ToolkitSettings.LoadData(true);
			AssetDatabase.Refresh();

			return result;
		}
		public static void FixMissingComponents(bool showDialog, bool refreshAssets)
		{
			if (refreshAssets)
				AssetDatabase.Refresh();

			if (showDialog)
				if (!EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "This process might take some time depending on the size of your project. Do you want to continue?", "Yes", "No"))
					return;

			static string GetScriptMetaText(AssetGUID scriptGUID)
			{
				return $"m_Script: {{fileID: {scriptGUID.fileID}, guid: {scriptGUID.guid}, type: {scriptGUID.type}}}";
			}
			static bool ConfirmDialog()
			{
				return EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "This process is undoable! Make sure to make a backup of your project or use a Version Control system before proceeding. Do you want to continue?", "Continue", "Cancel");
			}
			static void SettingsFailDialog()
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "We couldn't fix the Vehicle Settings asset.", "Continue");
			}
			static void FindObjectsWithMissingScripts(GameObject[] gameObjects, out List<Object> output, out int count)
			{
				int k = 0;

				output = new();
				count = 0;

				foreach (GameObject gameObject in gameObjects)
				{
					int newCount = gameObject.GetComponentsInChildren<MonoBehaviour>().Count(behaviour => behaviour == null);

					if (newCount > 0)
					{
						output.Add(gameObject);

						count += newCount;
					}

					EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", "Fetching prefabs...", (k + 1f) / gameObjects.Length);

					k++;
				}
			}
			static void ReplaceMissingGUIDsInAsset(string path, Dictionary<string, AssetGUID> oldGUIDs, Dictionary<string, AssetGUID> newGUIDs)
			{
				string orgContent = File.ReadAllText(path), newContent = orgContent;

				foreach (var oldGUIDPair in oldGUIDs)
				{
					if (!newGUIDs.ContainsKey(oldGUIDPair.Key))
						continue;

					AssetGUID oldGUID = oldGUIDPair.Value;
					string oldGUIDMeta = GetScriptMetaText(oldGUID);

					if (!orgContent.Contains(oldGUIDMeta))
						continue;

					AssetGUID newGUID = newGUIDs[oldGUIDPair.Key];
					string newGUIDMeta = GetScriptMetaText(newGUID);

					newContent = newContent.Replace(oldGUIDMeta, newGUIDMeta);
				}

				if (orgContent != newContent)
					File.WriteAllText(path, newContent);
			}

			var oldScriptGUIDs =
#if MVC_COMMUNITY
				OpenSourceScriptGUIDs
#else
				DLLScriptGUIDs
#endif
				;
			var newScriptGUIDs =
#if MVC_COMMUNITY
				DLLScriptGUIDs
#else
				OpenSourceScriptGUIDs
#endif
				;

			EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", "Finding assets...", 0f);

			ToolkitSettings settingsAsset = Resources.Load<ToolkitSettings>(ToolkitSettings.AssetPath);
			bool agreed = false;

			EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", "Fetching assets...", 1f);

			if (!settingsAsset)
			{
				string settingsAssetPath = FullAssetPath;

				if (!settingsAssetPath.IsNullOrEmpty())
				{
					if (showDialog)
						if (!ConfirmDialog())
							goto exit_fix;

					agreed = true;

					static bool IsScriptMetaLine(string line)
					{
						return line.Contains("m_Script:");
					}

					string[] settingsAssetLines = File.ReadAllLines(settingsAssetPath);
					string newSettingsMeta = GetScriptMetaText(newScriptGUIDs["ToolkitSettings"]);
					int metaLineIndex = Array.FindIndex(settingsAssetLines, line => IsScriptMetaLine(line));

					if (metaLineIndex == Array.FindLastIndex(settingsAssetLines, line => IsScriptMetaLine(line)))
					{
						string orgLine = settingsAssetLines[metaLineIndex];

						settingsAssetLines[metaLineIndex] = orgLine.Remove(orgLine.IndexOf("m_Script:")) + newSettingsMeta;

						File.WriteAllLines(settingsAssetPath, settingsAssetLines);
					}
					else
					{
						string settingsAssetContent = string.Join("\r\n", settingsAssetLines);
						string oldSettingsMeta = GetScriptMetaText(oldScriptGUIDs["ToolkitSettings"]);

						if (settingsAssetContent.Contains(oldSettingsMeta))
						{
							settingsAssetContent = settingsAssetContent.Replace(oldSettingsMeta, newSettingsMeta);

							File.WriteAllText(settingsAssetPath, settingsAssetContent);
						}
						else if (showDialog)
							SettingsFailDialog();
					}

					ToolkitSettings.LoadData(true);
				}
				else if (showDialog)
					SettingsFailDialog();
			}

			EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", "Finding prefabs...", 0f);

			string[] projectPrefabs = AssetDatabase.FindAssets("t:prefab", new string[] { "Assets/" }).Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();

			EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", "Fetching prefabs...", 1f);

			FindObjectsWithMissingScripts(projectPrefabs.Select(prefabPath => AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)).Where(prefab => prefab).ToArray(), out List<Object> projectAssetsWithMissingScripts, out int projectMissingCount);

			if (projectMissingCount > 0)
			{
				Debug.Log($"Missing Components in Prefabs ({projectMissingCount}):\r\n{string.Join("\r\n", projectAssetsWithMissingScripts.Select(@object => @object.name))}");

				if (!agreed && showDialog)
					if (!(agreed = ConfirmDialog()))
						goto exit_fix;

				int i = 0;

				foreach (Object prefab in projectAssetsWithMissingScripts)
				{
					EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", $"Updating Prefab ({i + 1}/{projectAssetsWithMissingScripts.Count})...", (i + 1f) / projectAssetsWithMissingScripts.Count);
					ReplaceMissingGUIDsInAsset(AssetDatabase.GetAssetPath(prefab), oldScriptGUIDs, newScriptGUIDs);

					i++;
				}

				if (refreshAssets)
					AssetDatabase.Refresh();
			}

			EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", "Finding scenes...", 0f);

			string[] projectScenes = AssetDatabase.FindAssets("t:scene", new string[] { "Assets/" }).Select(guid => AssetDatabase.GUIDToAssetPath(guid)).ToArray();

			EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", "Fetching scenes...", 1f);

			string[] missingProjectScenes = projectScenes.Where(path => oldScriptGUIDs.Values.Any(guid => File.ReadAllText(path).Contains(GetScriptMetaText(guid)))).ToArray();
			int sceneMissingCount = missingProjectScenes.Length;

			if (sceneMissingCount > 0)
			{
				Debug.Log($"Missing Components in Scenes ({sceneMissingCount}):\r\n{string.Join("\r\n", missingProjectScenes.Select(path => Path.GetFileNameWithoutExtension(path)))}");
				EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

				if (!agreed && showDialog)
					if (!(agreed = ConfirmDialog()))
						goto exit_fix;

				int i = 0;

				foreach (string scenePath in missingProjectScenes)
				{
					EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", $"Updating Scene ({i + 1}/{projectScenes.Length})...", (i + 1f) / projectScenes.Length);
					ReplaceMissingGUIDsInAsset(scenePath, oldScriptGUIDs, newScriptGUIDs);

					i++;
				}

				if (refreshAssets)
				{
					string activeScenePath = SceneManager.GetActiveScene().path;

					if (!activeScenePath.IsNullOrEmpty())
						EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Additive);

					AssetDatabase.Refresh();
				}
			}

			if (projectMissingCount < 1 && sceneMissingCount < 1 && showDialog && settingsAsset)
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Hooray! Your prefabs don't contain any missing scripts.", "Okay");

		exit_fix:
			EditorUtility.ClearProgressBar();
		}

		private static bool RestoreSettingsAsset(string path)
		{
			bool result = AssetDatabase.RenameAsset(Path.Combine("Assets", "BxB Studio", "MVC", "Resources", $"{path}.asset"), $"{Path.GetFileName(ToolkitSettings.AssetPath)}.asset").IsNullOrEmpty();

			AssetDatabase.Refresh();

			return result;
		}
		private static bool DeleteSettingsAsset(string path)
		{
			bool deleted = AssetDatabase.DeleteAsset(Path.Combine("Assets", "BxB Studio", "MVC", "Resources", $"{path}.asset"));

			AssetDatabase.Refresh();

			return deleted;
		}

		#endregion

		#endregion

		#endregion
	}
}

#region Namespaces

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using Utilities;
using Utilities.Editor;
using MVC.Core;
using MVC.Editor;
using MVC.Utilities.Internal;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Utility = Utilities.Utility;

#endregion

namespace MVC.Internal.Editor
{
	public struct ToolkitUpdate
	{
		#region Variables

		#region Static Variables

		private static ToolkitUpdate latestData;

		#endregion

		#region Global Variables

		public string version;
		public string releaseDate;
		public string type;
		public long responseCode;
		public string repairVersion;

		#endregion

		#endregion

		#region Methods

		public static void CheckForUpdates(bool showProgress, bool autoCheck)
		{
			if (showProgress)
				EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please wait...", "Checking for Updates...", 0f);

			latestData = GetLatestVersion();

			if (showProgress)
				EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please wait...", "Checking for Updates...", 1f);

			if (string.Compare(latestData.version, ToolkitInfo.Version) > 0)
			{
				EditorUtility.ClearProgressBar();

				if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", $"Yay! A new version is available!\r\nNew version: {latestData.version}\r\nRelease Date: {latestData.releaseDate}\r\nCurrent Version: {ToolkitInfo.Version}", "Update", "Remind me later"))
					ToolkitBehaviourEditor.VisitAssetStore();
				else if (!ToolkitPrefs.HasFlag(ToolkitConstants.HasUpdate))
					ToolkitPrefs.SetFlag(ToolkitConstants.HasUpdate);

				return;
			}
			else if (ToolkitPrefs.HasFlag(ToolkitConstants.HasUpdate))
				ToolkitPrefs.DeleteFlag(ToolkitConstants.HasUpdate);

			if (!ToolkitPrefs.HasString(ToolkitConstants.InstallVersion))
				ToolkitPrefs.SetString(ToolkitConstants.InstallVersion, ToolkitInfo.Version);

			if (!ToolkitPrefs.HasStruct(ToolkitConstants.InstallRenderPipeline))
				ToolkitPrefs.SetStruct(ToolkitConstants.InstallRenderPipeline, Utility.GetCurrentRenderPipeline());

			if (!ToolkitPrefs.HasStruct(ToolkitConstants.InstallPlatform))
				ToolkitPrefs.SetStruct(ToolkitConstants.InstallPlatform, EditorUtilities.GetCurrentBuildTarget());

			EditorUtility.ClearProgressBar();

			if (latestData.responseCode == 200 && !autoCheck)
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "You're all set! There's no update for the moment... But, we'll inform you of newer updates once they're available.", "Okay");
		}

		private static ToolkitUpdate GetLatestVersion()
		{
			ToolkitUpdate data = new()
			{
				version = ToolkitInfo.Version,
				type = ToolkitInfo.Version.Contains("alpha") ? "ALPHA" : ToolkitInfo.Version.Contains("beta") ? "BETA" : "STABLE"
			};

			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				ToolkitDebug.Warning("We couldn't check for updates! Please check your internet connection and retry again...");

				return data;
			}

			string requestURL = $"{ToolkitInfo.Website}/client/unity/update?ver={Uri.EscapeUriString(ToolkitInfo.Version)}";

			using UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(requestURL);

			try
			{
				request.disposeDownloadHandlerOnDispose = true;
				request.disposeCertificateHandlerOnDispose = true;
				request.certificateHandler = new ToolkitNetworking.BypassCertificate();

				request.SendWebRequest();

				while (request.result == UnityEngine.Networking.UnityWebRequest.Result.InProgress)
					continue;

				if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
					data = JsonUtility.FromJson<ToolkitUpdate>(request.downloadHandler.text);
				else
					ToolkitDebug.Warning($"We couldn't receive data from our servers while checking for updates!\r\nError ({request.responseCode}): {request.error}");

				data.responseCode = request.responseCode;
			}
			catch (Exception e)
			{
				ToolkitDebug.Error($"We couldn't check for updates!\r\n\r\nError: {e.Message}\r\nStackTrace: {e.StackTrace}");
			}
			finally
			{
				request.Dispose();
				EditorUtility.ClearProgressBar();
			}

			return data;
		}

		#endregion
	}

	internal struct ReportResponse
	{
#pragma warning disable CS0649
		public int response;
#pragma warning restore CS0649
	}

	[InitializeOnLoad]
	internal class ToolkitReloadUtility : ToolkitComponent
	{
		#region Variables

		private static readonly List<string> previousStackTraces = new();
		private static readonly List<string> previousLogs = new();

		#endregion

		#region Methods

		public static void PackagesValidations()
		{
			AssetDatabase.Refresh();

			bool projectSettingsFolderExists = AssetDatabase.IsValidFolder(ToolkitAssetsImporter.ProjectSettingsFolder) && !File.Exists($"{ToolkitAssetsImporter.ProjectSettingsFolder}_");
			bool audioFolderExists = AssetDatabase.IsValidFolder(ToolkitAssetsImporter.AudioFolder) && !File.Exists($"{ToolkitAssetsImporter.AudioFolder}_");

			if (audioFolderExists || projectSettingsFolderExists)
			{
				if (projectSettingsFolderExists)
					ToolkitAssetsImporter.ImportProjectSettings();

				if (audioFolderExists)
					ToolkitAssetsImporter.ImportAudio();

				AssetDatabase.Refresh();
			}

			if (ToolkitPrefs.HasFlag(ToolkitConstants.ForceFixMissingComponents))
			{
				ToolkitSettingsEditor.FixMissingComponents(false, true);
				ToolkitPrefs.DeleteFlag(ToolkitConstants.ForceFixMissingComponents);
			}
		}

#if !MVC_DEBUG
		private static void ReportExceptions(string log, string stackTrace, LogType type)
		{
			if (type != LogType.Exception || previousLogs.Contains(log) || previousStackTraces.Contains(stackTrace))
				return;
			else if (Application.internetReachability == NetworkReachability.NotReachable)
				goto submission_error;

			try
			{
				string requestURL = $"{ToolkitInfo.Website}/client/log";
				string ipAddress = ToolkitNetworking.GetIPAddresses().FirstOrDefault();
				string macAddress = ToolkitNetworking.GetPhysicalAddresses().FirstOrDefault();
				string deviceID = ToolkitNetworking.GetDeviceID();
				int licenseTypeID =
#if MVC_COMMUNITY
					0;
#elif MVC_PLATINUM
					2;
#else
					1;
#endif

				requestURL += $"?ip_address={Uri.EscapeUriString(ipAddress)}";
				requestURL += $"&mac_address={Uri.EscapeUriString(macAddress)}";
				requestURL += $"&device_id={Uri.EscapeUriString(deviceID)}";
				requestURL += $"&license_type_id={licenseTypeID}";
				requestURL += $"&installer=0";
				requestURL += $"&version={ToolkitInfo.Version}";
				requestURL += $"&type={type}";
				requestURL += $"&log={Uri.EscapeUriString(log.Replace('\\', '/')).Replace("&", "(and_symbol)").Replace("=", "(equal_symbol)")}";
				requestURL += $"&stack_trace={Uri.EscapeUriString(stackTrace.Replace('\\', '/')).Replace("&", "(and_symbol)").Replace("=", "(equal_symbol)")}";

				using UnityWebRequest request = UnityWebRequest.Get(requestURL);

				request.disposeDownloadHandlerOnDispose = true;
				request.disposeCertificateHandlerOnDispose = true;
				request.certificateHandler = new ToolkitNetworking.BypassCertificate();

				request.SendWebRequest();

				while (request.result == UnityWebRequest.Result.InProgress)
					continue;

				if (request.result == UnityWebRequest.Result.Success)
				{
					if (JsonUtility.FromJson<ReportResponse>(request.downloadHandler.text).response >= 300)
						goto submission_error;
				}
				else
				{
					Debug.LogError($"<b>Multiversal Vehicle Controller:</b> We have had some errors while reporting errors...\r\nError Code: {request.responseCode}\r\nError: {request.error}");

					goto submission_error;
				}

				request.Dispose();
			}
			catch (Exception e)
			{
				Debug.LogError(e);

				goto submission_error;
			}

			previousStackTraces.Add(stackTrace);
			previousLogs.Add(log);

			return;

		submission_error:
			Debug.LogWarning($"<b>Multiversal Vehicle Controller:</b> We couldn't report this error to our servers! Please contact us directly <a href=\"{ToolkitInfo.Website}/contact\">{ToolkitInfo.Website}/contact</a> or via email <a href=\"mailto:{ToolkitInfo.Email}\">{ToolkitInfo.Email}</a> to help you resolve this issue!");
		}
#endif
		private static void WaitAfterCompilation()
		{
			if (EditorApplication.isCompiling || PlayerPrefs.HasKey(ToolkitConstants.Installer))
				return;

			PrefsValidation();
			MVCValidations();
			SettingsValidations();
			ComponentsValidations();
			PackagesValidations();
			ReviewMessage();
			EditorUtility.ClearProgressBar();

			EditorApplication.update -= WaitAfterCompilation;
		}
		private static void PrefsValidation()
		{
			static void PlayerPrefToToolkitPref(string key, ToolkitPrefs.PrefType type)
			{
				if (!PlayerPrefs.HasKey(key))
					return;

				switch (type)
				{
					case ToolkitPrefs.PrefType.String:
						ToolkitPrefs.SetString(key, PlayerPrefs.GetString(key));

						break;

					case ToolkitPrefs.PrefType.Float:
						ToolkitPrefs.SetFloat(key, PlayerPrefs.GetFloat(key));

						break;

					case ToolkitPrefs.PrefType.Flag:
						ToolkitPrefs.SetFlag(key);

						break;

					case ToolkitPrefs.PrefType.Boolean:
						ToolkitPrefs.SetBool(key, Utility.NumberToBool(PlayerPrefs.GetInt(key)));

						break;

					case ToolkitPrefs.PrefType.Integer:
						ToolkitPrefs.SetInt(key, PlayerPrefs.GetInt(key));

						break;

					case ToolkitPrefs.PrefType.Struct:
						ToolkitPrefs.SetStruct(key, PlayerPrefs.GetInt(key));

						break;

					default:
						return;
				}

				PlayerPrefs.DeleteKey(key);
			}

			PlayerPrefToToolkitPref($"{ToolkitConstants.ChangeLogPrefix}{ToolkitInfo.Version.ToUpper()}", ToolkitPrefs.PrefType.Flag);
			PlayerPrefToToolkitPref(ToolkitConstants.ForceFixMissingComponents, ToolkitPrefs.PrefType.Flag);
			PlayerPrefToToolkitPref(ToolkitConstants.ImportingUnityPackages, ToolkitPrefs.PrefType.String);
			PlayerPrefToToolkitPref(ToolkitConstants.InstallRenderPipeline, ToolkitPrefs.PrefType.Struct);
			PlayerPrefToToolkitPref(ToolkitConstants.InstallPlatform, ToolkitPrefs.PrefType.Struct);
			PlayerPrefToToolkitPref(ToolkitConstants.InstallVersion, ToolkitPrefs.PrefType.String);
			PlayerPrefToToolkitPref(ToolkitConstants.HasUpdate, ToolkitPrefs.PrefType.Flag);
			PlayerPrefToToolkitPref(ToolkitConstants.SetupDone, ToolkitPrefs.PrefType.Flag);
		}
		private static void MVCValidations()
		{
			ToolkitSettings.LoadData(true);

			if (ToolkitInfo.IsSetupDone)
			{
				ToolkitSettings.RefreshInternalErrors();

				if (!HasInternalErrors)
				{
					if (Settings)
					{
						EditorUtility.SetDirty(Settings);
						ToolkitSettingsEditor.SaveSettings();
					}

					ToolkitSettings.LoadData();
				}

				string[] scriptingDefineSymbols = EditorUtilities.GetScriptingDefineSymbols();
				bool defineSymbolsChanged = false;

#if MVC_COMMUNITY
				if (EditorUtilities.ScriptingDefineSymbolExists(ToolkitConstants.PlatinumScriptingSymbol, out int symbolIndex))
				{
					EditorUtilities.RemoveScriptingDefineSymbol(symbolIndex);
					
					defineSymbolsChanged = true;
				}

				if (EditorUtilities.ScriptingDefineSymbolExists(ToolkitConstants.ProScriptingSymbol, out symbolIndex))
				{
					EditorUtilities.RemoveScriptingDefineSymbol(symbolIndex);
					
					defineSymbolsChanged = true;
				}
				
				if (!EditorUtilities.ScriptingDefineSymbolExists(ToolkitConstants.CommunityScriptingSymbol))
				{
					EditorUtilities.AddScriptingDefineSymbol(ToolkitConstants.CommunityScriptingSymbol);
					
					defineSymbolsChanged = true;
				}
#elif MVC_PLATINUM
				if (EditorUtilities.ScriptingDefineSymbolExists(ToolkitConstants.CommunityScriptingSymbol, out int symbolIndex))
				{
					EditorUtilities.RemoveScriptingDefineSymbol(symbolIndex);
					
					defineSymbolsChanged = true;
				}

				if (EditorUtilities.ScriptingDefineSymbolExists(ToolkitConstants.ProScriptingSymbol, out symbolIndex))
				{
					EditorUtilities.RemoveScriptingDefineSymbol(symbolIndex);
					
					defineSymbolsChanged = true;
				}
				
				if (!EditorUtilities.ScriptingDefineSymbolExists(ToolkitConstants.PlatinumScriptingSymbol))
				{
					EditorUtilities.AddScriptingDefineSymbol(ToolkitConstants.PlatinumScriptingSymbol);
					
					defineSymbolsChanged = true;
				}
#else
				if (EditorUtilities.ScriptingDefineSymbolExists(ToolkitConstants.CommunityScriptingSymbol, out int symbolIndex))
				{
					EditorUtilities.RemoveScriptingDefineSymbol(symbolIndex);

					defineSymbolsChanged = true;
				}

				if (EditorUtilities.ScriptingDefineSymbolExists(ToolkitConstants.PlatinumScriptingSymbol, out symbolIndex))
				{
					EditorUtilities.RemoveScriptingDefineSymbol(symbolIndex);

					defineSymbolsChanged = true;
				}

				if (!EditorUtilities.ScriptingDefineSymbolExists(ToolkitConstants.ProScriptingSymbol))
				{
					EditorUtilities.AddScriptingDefineSymbol(ToolkitConstants.ProScriptingSymbol);

					defineSymbolsChanged = true;
				}
#endif

				if (defineSymbolsChanged)
					return;

				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				if (Application.internetReachability != NetworkReachability.NotReachable)
				{
					if (Settings && Settings.autoCheckForUpdates)
						ToolkitUpdate.CheckForUpdates(true, true);

					EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please wait...", "Fetching session...", 1f);

					ToolkitUserSession.SubmitSession(new()
					{
						physicalAddress = ToolkitNetworking.GetPhysicalAddresses().FirstOrDefault(),
						ipAddress = ToolkitNetworking.GetIPAddresses().FirstOrDefault(),
						deviceID = ToolkitNetworking.GetDeviceID(),
						version = ToolkitInfo.Version,
						licenseType = ToolkitInfo.License
					});

					EditorUtility.ClearProgressBar();
				}

				string changeLogPrefKey = $"{ToolkitConstants.ChangeLogPrefix}{ToolkitInfo.Version.ToUpper()}";

				if (!ToolkitPrefs.HasFlag(changeLogPrefKey))
				{
					ToolkitPrefs.SetFlag(changeLogPrefKey);
					ToolkitChangeLogEditor.OpenWindow();
				}

				return;
			}

			ToolkitSetupWizardWindow.OpenWindow();
		}
		private static void SettingsValidations()
		{
			ToolkitSettings.LoadData(true);

			static bool HasProblems()
			{
				return Settings && Settings.Problems.IsSheetValid && Settings.Problems.HasProblems;
			}

			if (!HasProblems() && !Settings && !ToolkitSettingsEditor.FullAssetPath.IsNullOrEmpty())
				ToolkitSettingsEditor.FixMissingComponents(false, true);

			if (HasProblems())
			{
				if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "The Settings Panel have some issues and errors that need to be fixed for the MVC to work properly. If you're willing to ignore them, most features might get disabled until they're fixed!", "Open Settings Panel", "Ignore"))
				{
					ToolkitSettingsEditor.OpenWindow();

					if (Settings.Problems.HasGeneralProblems)
						Settings.settingsFoldout = ToolkitSettings.SettingsEditorFoldout.General;
#if !MVC_COMMUNITY
					else if (Settings.Problems.HasAIProblems)
						Settings.settingsFoldout = ToolkitSettings.SettingsEditorFoldout.AI;
#endif
					else if (Settings.Problems.HasBehaviourProblems)
						Settings.settingsFoldout = ToolkitSettings.SettingsEditorFoldout.Behaviour;
					else if (Settings.Problems.HasDamageProblems)
						Settings.settingsFoldout = ToolkitSettings.SettingsEditorFoldout.Damage;
					else if (Settings.Problems.HasCamerasProblems)
						Settings.settingsFoldout = ToolkitSettings.SettingsEditorFoldout.Cameras;
					else if (Settings.Problems.HasPlayerInputsProblems)
						Settings.settingsFoldout = ToolkitSettings.SettingsEditorFoldout.PlayerInputs;
					else if (Settings.Problems.HasEnginesProblems)
						Settings.settingsFoldout = ToolkitSettings.SettingsEditorFoldout.EnginesChargers;
					else if (Settings.Problems.HasGroundsProblems)
						Settings.settingsFoldout = ToolkitSettings.SettingsEditorFoldout.Grounds;
					else if (Settings.Problems.HasSFXProblems)
						Settings.settingsFoldout = ToolkitSettings.SettingsEditorFoldout.SFX;
					else if (Settings.Problems.HasVFXProblems)
						Settings.settingsFoldout = ToolkitSettings.SettingsEditorFoldout.VFX;

					EditorApplication.isPlaying = false;
				}
			}
			else if (ToolkitInfo.IsSetupDone)
			{
				if (!ToolkitPrefs.HasFlag(ToolkitConstants.DontRemindAudioSettingsIssues))
				{
					var audioConfiguration = AudioSettings.GetConfiguration();
					int recommendedVirtualVoicesCount = 2048;
					int recommendedRealVoicesCount = 128;

					if (audioConfiguration.numVirtualVoices < recommendedVirtualVoicesCount || audioConfiguration.numRealVoices < recommendedRealVoicesCount)
					{
						if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "The current project audio settings might cause some sounds to be cut-off during Play Mode. To fix this, it is suggested to increase the `Max Virtual Voices` && `Max Real Voices` in the `Project Settings` window. Do you want us to fix this for you?", "Yes", "No"))
						{
							audioConfiguration.numVirtualVoices = recommendedVirtualVoicesCount;
							audioConfiguration.numRealVoices = recommendedRealVoicesCount;

							AudioSettings.Reset(audioConfiguration);
							EditorUtility.SetDirty(AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/AudioManager.asset"));
						}
						else if (!EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "We will remind you of this issue in the future!", "Okay", "Don't Remind"))
							ToolkitPrefs.SetFlag(ToolkitConstants.DontRemindAudioSettingsIssues);
					}
				}
			}
		}
		private static void ComponentsValidations()
		{
			if (!Object.FindAnyObjectByType<ToolkitBehaviour>(FindObjectsInactive.Include))
				return;

			VehicleManager.GetOrCreateInstance();
			VehicleFollower.GetOrCreateInstance();
		}
		private static void ReviewMessage()
		{
			if (!ToolkitInfo.IsSetupDone || ToolkitBehaviour.HasInternalErrors || ToolkitPrefs.HasFlag(ToolkitConstants.DontRemindAssetReview) || Random.Range(0f, 1f) > ToolkitConstants.ReviewPopupProbability)
				return;

			switch (EditorUtility.DisplayDialogComplex("Multiversal Vehicle Controller: Info", "What do you think about the Multiversal Vehicle Controller? Give us your review to help us improve our toolkit!", "Give Review", "Later", "Don't Remind Me"))
			{
				case 2:
					ToolkitPrefs.SetFlag(ToolkitConstants.DontRemindAssetReview);

					break;

				case 0:
					ToolkitEditorWindow.ReviewOnAssetStore();

					break;
			}
		}

		#endregion

		#region Constructor

		static ToolkitReloadUtility()
		{
#if !MVC_DEBUG
			Application.logMessageReceived += ReportExceptions;
#endif
			EditorApplication.update += WaitAfterCompilation;
		}

		#endregion
	}
	internal class ToolkitAssetPostProcessor : AssetPostprocessor
	{
#pragma warning disable IDE0060 // Remove unused parameter
		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths, bool didDomainReload)
#pragma warning restore IDE0060 // Remove unused parameter
		{
			if (didDomainReload)
				return;

			ToolkitReloadUtility.PackagesValidations();
		}
	}
	internal struct ToolkitAssetsImporter
	{
		#region Constants

		public const string ProjectSettingsFolder = "Assets/BxB Studio/MVC/ProjectSettings";
		public const string AudioFolder = "Assets/BxB Studio/MVC/Audio";

		#endregion

		#region Methods

		public static void ImportProjectSettings()
		{
			string projectSettingsFolderPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), ProjectSettingsFolder);
			string[] files = Directory.GetFiles(projectSettingsFolderPath, "*.*", SearchOption.AllDirectories);

			if (files == null || files.Length < 1)
			{
				ToolkitDebug.Log("No project settings found to import. Deleting the `ProjectSettings` folder...");

				goto delete_folder;
			}

			ToolkitIO.MoveAssets(projectSettingsFolderPath, files, Path.Combine(Path.GetDirectoryName(Application.dataPath), "ProjectSettings"), true);
		delete_folder:
			if (Directory.Exists(projectSettingsFolderPath))
				Directory.Delete(projectSettingsFolderPath, true);

			string projectSettingsFolderMeta = AssetDatabase.GetTextMetaFilePathFromAssetPath(projectSettingsFolderPath);

			if (File.Exists(projectSettingsFolderMeta))
				File.Delete(projectSettingsFolderMeta);
		}
		public static void ImportAudio()
		{
			string audioFolderPath = Path.Combine(Path.GetDirectoryName(Application.dataPath), AudioFolder);
			string[] files = Directory.GetFiles(audioFolderPath, "*.*", SearchOption.AllDirectories);

			if (files == null || files.Length < 1)
			{
				ToolkitDebug.Log("No audio file found to import. Deleting the `Audio` folder...");

				goto delete_folder;
			}

			ToolkitSettings settings = ToolkitBehaviour.Settings;

			if (!settings)
				return;

			ToolkitIO.MoveAssets(audioFolderPath, files, Path.Combine(Application.dataPath, "BxB Studio", "MVC", "Resources", settings.audioFolderPath), true);
		delete_folder:
			if (Directory.Exists(audioFolderPath))
				Directory.Delete(audioFolderPath, true);

			string audioFolderMeta = AssetDatabase.GetTextMetaFilePathFromAssetPath(audioFolderPath);

			if (File.Exists(audioFolderMeta))
				File.Delete(audioFolderMeta);
		}

		#endregion
	}
}

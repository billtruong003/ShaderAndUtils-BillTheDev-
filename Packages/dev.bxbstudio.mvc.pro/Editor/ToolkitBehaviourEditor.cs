#region Namespaces

using System.IO;
using UnityEngine;
using UnityEditor;
using Utilities.Editor;
using MVC.Core;
using MVC.Internal;
using MVC.Internal.Editor;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Editor
{
	public class ToolkitBehaviourEditor : UnityEditor.Editor
	{
		#region Variables

		public static ToolkitSettings Settings => ToolkitBehaviour.Settings;
		public static VehicleManager Manager => ToolkitBehaviour.Manager;
		public static bool HasInternalErrors => ToolkitBehaviour.HasInternalErrors;
		public static bool IsSetupDone => ToolkitInfo.IsSetupDone;

		internal static ToolkitSettings BackupAsset
		{
			get
			{
				return ToolkitSettings.LoadData($"{ToolkitSettings.AssetPath}_Backup");
			}
		}

		#endregion

		#region Methods

		public static void VisitWebsite()
		{
			OpenExternalWebPage(ToolkitInfo.Website);
		}
		public static void BuyLicense(bool askForPermission = true)
		{
			OpenExternalWebPage($"{ToolkitInfo.Website}/buy", askForPermission);
		}
		public static void VisitDownloadPage(bool askForPermission = true)
		{
			OpenExternalWebPage($"{ToolkitInfo.Website}/download", askForPermission);
		}
		public static void VisitAssetStore(ToolkitInfo.LicenseType license)
		{
			OpenExternalWebPage($"{ToolkitInfo.Website}/buy/{license.ToString().ToLower()}");
		}
		public static void VisitAssetStore()
		{
			VisitAssetStore(ToolkitInfo.License);
		}
		public static void ReviewOnAssetStore(ToolkitInfo.LicenseType license)
		{
			OpenExternalWebPage($"{ToolkitInfo.Website}/buy/{license.ToString().ToLower()}?review");
		}
		public static void ReviewOnAssetStore()
		{
			VisitAssetStore(ToolkitInfo.License);
		}
		[MenuItem("Tools/Multiversal Vehicle Controller/Help/Open Documentation...", false, 5)]
		public static void VisitDocumentation()
		{
			OpenExternalWebPage($"{ToolkitInfo.Website}/docs/manual?version={ToolkitInfo.Version}");
		}
		[MenuItem("Tools/Multiversal Vehicle Controller/Help/Tutorials/Full Playlist...", false, 7)]
		public static void OpenTutorialsPlaylist()
		{
			OpenExternalWebPage("https://www.youtube.com/playlist?list=PLrjFal5KVAgozr9jZGjv9gEWbCQitUYTR");
		}
		[MenuItem("Tools/Multiversal Vehicle Controller/Help/Tutorials/Setup and Activation...", false, 8)]
		public static void WatchSetupInstallTutorial()
		{
			OpenExternalWebPage("https://www.youtube.com/watch?v=mXhJ4VX5fhE&list=PLrjFal5KVAgozr9jZGjv9gEWbCQitUYTR");
		}
		[MenuItem("Tools/Multiversal Vehicle Controller/Help/Tutorials/Create and Setup a Vehicle...", false, 9)]
		public static void WatchSetupTutorial()
		{
			OpenExternalWebPage("https://www.youtube.com/watch?v=5lRViTYHm9k&list=PLrjFal5KVAgozr9jZGjv9gEWbCQitUYTR");
		}
		[MenuItem("Tools/Multiversal Vehicle Controller/Help/Report Error", false, 8)]
		public static void ReportError()
		{
			ToolkitContactWindow.OpenWindow(true);
		}
		[MenuItem("Tools/Multiversal Vehicle Controller/Help/Contact Us", false, 9)]
		public static void ContactUs()
		{
			ToolkitContactWindow.OpenWindow(false);
		}
		public static void About()
		{
			OpenExternalWebPage($"{ToolkitInfo.Website}/about");
		}

		internal static ToolkitSettings RecreateAssetFile(bool backup)
		{
			string backupExtension = backup ? "_Backup" : "";
			string path = Path.Combine("Assets", "BxB Studio", "MVC", "Resources", ToolkitSettings.AssetPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

			if (Settings)
			{
				if (!backup || backup && BackupAsset)
				{
					if (!AssetDatabase.DeleteAsset($"{path}{backupExtension}.asset"))
						return null;
					else
						AssetDatabase.Refresh();
				}

				if (backup)
				{
					bool result = AssetDatabase.CopyAsset($"{path}.asset", $"{path}{backupExtension}.asset");

					if (result)
					{
						EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Backup file created successfully!", "Okay");
						AssetDatabase.Refresh();

						return BackupAsset;
					}
					else
						EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "Oops! The backup process has failed! Something wrong happened while trying to access the original 'MVCSettings_Data' asset.", "Okay");

					return null;
				}
			}

			ToolkitSettings asset = ScriptableObjectUtility.CreateAsset<ToolkitSettings>(path);

			asset.ResetAILayers();
			asset.ResetCameras();
			asset.ResetEngines();
			asset.ResetChargers();
			asset.ResetTireCompounds();
			asset.ResetGrounds();
			AssetDatabase.Refresh();

			return asset;
		}
		internal static void FixInternalProblems(Object selectAfterFix)
		{
			if (!HasInternalErrors)
				return;

			bool vehicleSettingsFixed = Settings;
			bool vehicleSettingsBeforeReferenceFix = Settings;

			ToolkitSettingsEditor.FixMissingComponents(false, true);

			bool vehicleSettingsAfterReferenceFix = Settings;

			if (!vehicleSettingsFixed)
				if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Internal Error", "The MVC Settings asset is missing! Do you want to create a new one or maybe load an existing backup?", "Yeah sure!", "No thanks"))
				{
					if (BackupAsset)
					{
						if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "A backup file has been found do you want to load it?", "Yes", "No"))
						{
							AssetDatabase.RenameAsset(Path.Combine("Assets", "BxB Studio", "MVC", "Resources", $"{ToolkitSettings.AssetPath}_Backup.asset").Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar), $"{Path.GetFileName(ToolkitSettings.AssetPath)}.asset");

							if (Settings)
								vehicleSettingsFixed = EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "The MVC Settings backup file has been loaded successfully!", "Okay");
							else if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "Oopsie! We couldn't load the MVC Settings backup asset due to some internal problems!", "Report Error", "Cancel"))
							{
								ReportError();

								return;
							}
							else
								return;
						}
					}

					if (!vehicleSettingsFixed)
					{
						if (RecreateAssetFile(false))
						{
							vehicleSettingsFixed = EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "A new MVCSettings asset has been created successfully!", "Okay");

							ToolkitSettings.LoadData(true);
						}
						else if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "Oops! We couldn't create a new MVCSettings asset due to some internal problems!", "Report Error", "Cancel"))
						{
							ReportError();

							return;
						}
					}
				}

			bool vehicleAILayersFixed = vehicleSettingsFixed && Settings.AILayers != null && Settings.AILayers.Length > 0;
			bool vehicleEnginesFixed = vehicleSettingsFixed && Settings.Engines != null && Settings.Engines.Length > 0;
			bool vehicleTireCompoundsFixed = vehicleSettingsFixed && Settings.TireCompounds != null && Settings.TireCompounds.Length > 0;
			bool vehicleGroundsFixed = vehicleSettingsFixed && Settings.Grounds != null && Settings.Grounds.Length > 0;
			bool vehicleCamerasFixed = vehicleSettingsFixed && Settings.Cameras != null && Settings.Cameras.Length > 0;

			if (!vehicleSettingsFixed)
				return;

		retry_fix:
			if (!vehicleAILayersFixed)
			{
				if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Internal Error", "The AI layers array is empty! You can reset the MVC Settings or either add a new layer manually.", "Add me a new layer", "Reset All Now!"))
				{
					Settings.ResetAILayers();

					vehicleAILayersFixed = true;
				}
				else
					vehicleAILayersFixed = ToolkitSettingsEditor.ResetSettings();
			}

			if (!vehicleEnginesFixed)
			{
				if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Internal Error", "The engines array is empty! You can reset the MVC Settings or either add some new presets manually.", "Add me new engines", "Reset All Now!"))
				{
					Settings.ResetEngines();

					vehicleEnginesFixed = true;
				}
				else
					vehicleEnginesFixed = ToolkitSettingsEditor.ResetSettings();
			}

			if (!vehicleTireCompoundsFixed)
			{
				if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Internal Error", "The tire compounds array is empty! You can reset the MVC Settings or either add some new presets manually.", "Add me new tire compounds", "Reset All Now!"))
				{
					Settings.ResetTireCompounds();

					vehicleTireCompoundsFixed = true;
				}
				else
					vehicleTireCompoundsFixed = ToolkitSettingsEditor.ResetSettings();
			}

			if (!vehicleGroundsFixed)
			{
				if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Internal Error", "The grounds array is empty! You can reset the MVC Settings or either add a new preset manually.", "Add me a new ground", "Reset All Now!"))
				{
					Settings.ResetGrounds();

					vehicleGroundsFixed = true;
				}
				else
					vehicleGroundsFixed = ToolkitSettingsEditor.ResetSettings();
			}

			if (!vehicleCamerasFixed)
			{
				if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Internal Error", "The cameras array is empty! You can reset the MVC Settings or either add a new preset manually.", "Add me a new camera", "Reset All Now!"))
				{
					Settings.ResetCameras();

					vehicleCamerasFixed = true;
				}
				else
					vehicleCamerasFixed = ToolkitSettingsEditor.ResetSettings();
			}

			if (vehicleSettingsFixed && vehicleAILayersFixed && vehicleEnginesFixed && vehicleGroundsFixed && vehicleCamerasFixed)
			{
				if (vehicleSettingsAfterReferenceFix == vehicleSettingsBeforeReferenceFix)
					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Great News! All the internal errors have been fixed successfully! You may now proceed with the setup of your vehicles.", "Okay");

				Vehicle[] vehicles = FindObjectsByType<Vehicle>(FindObjectsSortMode.None);

				for (int i = 0; i < vehicles.Length; i++)
					vehicles[i].RefreshLayersAndTags();

				if (selectAfterFix)
					ToolkitEditorUtility.SelectObject(selectAfterFix);

				ToolkitSettings.RefreshInternalErrors();
			}
			else
				switch (EditorUtility.DisplayDialogComplex("Multiversal Vehicle Controller: Error", "Some internal errors may have not been fixed! In order to proceed using the Multiversal Vehicle Controller you need to press the 'Retry' button bellow! If this problem keeps prompting you, try contacting us and hopefully we can help you out.", "Report Error", "Cancel", "Retry"))
				{
					case 0:
						ReportError();

						break;

					case 2:
						goto retry_fix;
				}
		}
		internal static void GUILayoutExperimentalIcon(bool expandLabel)
		{
			GUIContent label = new(expandLabel ? "Experimental" : "Exp.", expandLabel ? "" : "Experimental");
			Color orgGUIBackgroundColor = GUI.backgroundColor;

			GUI.backgroundColor = new(.25f, .25f, .25f);

			EditorGUI.BeginDisabledGroup(true);
			GUILayout.Button(label, new GUIStyle(EditorStyles.miniButton)
			{
				fixedWidth = EditorStyles.miniButton.CalcSize(label).x * 1.125f
			});
			EditorGUI.EndDisabledGroup();

			GUI.backgroundColor = orgGUIBackgroundColor;
		}
		internal static void OpenExternalWebPage(string url, bool askForPermission = true)
		{
			if (!askForPermission)
				goto open_url;

			if (!EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "You are about to go to an external web page. Do you want to proceed?", "Yes", "No"))
				return;

		open_url:
			Application.OpenURL(url);
		}

		private static void GUILayoutVersionIcon(Color color, string label)
		{
			Color orgGUIBackgroundColor = GUI.backgroundColor;

			GUI.backgroundColor = color;

			if (GUILayout.Button(label, new GUIStyle(EditorStyles.miniButton)
			{
				fixedWidth = EditorStyles.miniButton.CalcSize(new(label)).x * 1.2f
			}))
				OpenExternalWebPage($"{ToolkitInfo.Website}/buy");

			GUI.backgroundColor = orgGUIBackgroundColor;
		}

		#endregion
	}
}

#region Namespaces

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEditor;
using Utilities;
using Utilities.Editor;
using MVC.Utilities.Internal;
using MVC.Utilities.Editor;
using MVC.Editor;

using Object = UnityEngine.Object;

#endregion

namespace MVC.Internal.Editor
{
	internal class ToolkitSetupWizardWindow : ToolkitEditorWindow
	{
		#region Enumerators

		private enum SetupFoldout { Startup, Error, Configurations, Import, Finish, SetupDone }

		#endregion

		#region Variables

		#region Static Variables

		private static string SuggestedAudioFolderFullPath => Path.Combine(Application.dataPath, suggestedAudioFolderParentPath, suggestedAudioFolderPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		private static readonly string suggestedAudioFolderParentPath = Path.Combine("BxB Studio", "MVC", "Resources").Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
		private static readonly string suggestedAudioFolderPath = "Audio";
		private static readonly string[] renderPipelines = new string[]
		{
				"Built-In Render Pipeline (BiRP)",
				"Universal Render Pipeline (URP)",
				"High Definition Render Pipeline (HDRP)",
				"Custom Render Pipeline"
		};
		private static ToolkitSetupWizardWindow instance;
		private static GUIStyle termsTextAreaStyle;

		#endregion

		#region Global Variables

		[SerializeField]
		private Utility.RenderPipeline renderPipeline;
		[SerializeField]
		private ToolkitSettings.PhysicsType physicsType;
		[SerializeField]
		private Utility.UnitType unitsType;
		[SerializeField]
		private Utility.UnitType editorUnitsType;
		[SerializeField]
		private ToolkitSettings.InputSystem inputSystem;
		[SerializeField]
		private string audioFolderPath;
		[SerializeField]
		private string audioFolderFullPath;
		private SetupFoldout foldout;
		private Vector2 finalizingScroll;
		private string happySmiley;
		private string errorSmiley;
		private string setupTask;
		private string setupDownloadSpeed;
		private float setupProgress;
		private bool renderPipelineInitialized;
		private bool showSetupProgressBar;
		[SerializeField]
		private string vehiclesLayer;
		[SerializeField]
		private string vehicleWheelsLayer;
		[SerializeField]
		private string[] setupPackages;
		[SerializeField]
		private int currentSetupPackage;

		#endregion

		#endregion

		#region Methods

		#region Static Methods

		public static ToolkitSetupWizardWindow OpenWindow()
		{
			if (instance)
			{
				FocusWindowIfItsOpen<ToolkitSetupWizardWindow>();

				return instance;
			}

			instance = GetWindow<ToolkitSetupWizardWindow>(true, "Multiversal Vehicle Controller: Setup Wizard", true);
			instance.minSize = new(860f, 450f);

			return instance;
		}

		#endregion

		#region Global Methods

		#region Utilities

		#region Import Packages

		private void StartImport()
		{
			foldout = SetupFoldout.Import;

			RandomizeHappySmiley();
			DisplaySetupProgressBar("Preparing Setup...", 0f, "Please wait..."); // Don't panic! It might seem stuck but it's alright.
			Repaint();

			List<string> setupPackages = ToolkitUnityPackages.GetSetupPackages(renderPipeline);

			if (setupPackages == null)
				goto setup_folder_error;

			if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Do you want to import the Getting Started Demo packages?", "Yes", "No"))
			{
				List<string> samples = ToolkitUnityPackages.GetSamplePackages(renderPipeline);

				if (samples == null)
					goto samples_folder_error;

				setupPackages.AddRange(samples);
			}
			else
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "To import the Getting Started Demo packages in the future, go to \"Tools > Multiversal Vehicle Controller > Samples > Getting Started Demo\"", "Got it!");

			this.setupPackages = setupPackages.ToArray();
			currentSetupPackage = -1;

			ImportNextPackage(string.Empty);

			return;

		setup_folder_error:
			ShowErrorFoldout("Setup packages folder cannot be found!");

			return;

		samples_folder_error:
			ShowErrorFoldout("Samples folder cannot be found!");
		}
		private void ImportPackages()
		{
			if (EditorApplication.isCompiling)
				return;

			EditorApplication.update -= ImportPackages;

			if (currentSetupPackage >= setupPackages.Length)
			{
				AssetDatabase.Refresh();

				EditorApplication.update += ImportCompleteAfterCompilation;

				return;
			}

			AssetDatabase.ImportPackage(setupPackages[currentSetupPackage],
#if MVC_DEBUG
				true
#else
				false
#endif
				);

			AssetDatabase.importPackageCompleted -= ImportNextPackage;
			AssetDatabase.importPackageCompleted += ImportNextPackage;
#if MVC_DEBUG
			AssetDatabase.importPackageCancelled -= ImportNextPackage;
			AssetDatabase.importPackageCancelled += ImportNextPackage;
#else
			AssetDatabase.importPackageCancelled -= ImportPackageCancelled;
			AssetDatabase.importPackageCancelled += ImportPackageCancelled;
#endif
			AssetDatabase.importPackageFailed -= ImportPackageFailed;
			AssetDatabase.importPackageFailed += ImportPackageFailed;
		}
		private void ImportNextPackage(string _)
		{
			if (setupPackages == null)
			{
				AssetDatabase.importPackageCompleted -= ImportNextPackage;

				return;
			}

			currentSetupPackage++;

			DisplaySetupProgressBar($"Importing Packages ({math.min(currentSetupPackage + 1, setupPackages.Length)} of {setupPackages.Length})...", (currentSetupPackage + 1f) / setupPackages.Length, "Don't panic! It might seem stuck but it's alright.");
			Repaint();

			EditorApplication.update += ImportPackages;
		}
		private void ImportPackageFailed(string packageName, string errorMessage)
		{
			ShowErrorFoldout($"{packageName} couldn't be imported\r\nError: {errorMessage}");
		}
		private void ImportPackageCancelled(string packageName)
		{
			ShowErrorFoldout($"{packageName} couldn't be imported\r\nError: Import cancelled by user");
		}
		[InitializeOnLoadMethod]
		private static void ImportOnInitialize()
		{
			if (ToolkitInfo.IsSetupDone)
				return;

			EditorApplication.update += ImportAfterCompilation;
		}
		private static void ImportAfterCompilation()
		{
			if (EditorApplication.isCompiling || PlayerPrefs.HasKey(ToolkitConstants.Installer))
				return;

			try
			{
				if (!instance)
					OpenWindow();

				if (instance.setupPackages == null || instance.setupPackages.Length < 1)
					return;

				instance.ImportNextPackage(string.Empty);
			}
			catch
			{
				throw;
			}
			finally
			{
				EditorApplication.update -= ImportAfterCompilation;
			}
		}
		private void ImportCompleteAfterCompilation()
		{
			setupPackages = null;

			DisplaySetupProgressBar("Importing Assets...", 1f, "Finalizing...");

			if (EditorApplication.isCompiling)
				return;

			ToolkitSettingsEditor.FixMissingComponents(false, true);
			ApplySetupSettings();
			TerminateSetup();

			EditorApplication.update -= ImportCompleteAfterCompilation;

			EditorUtility.ClearProgressBar();
		}

		#endregion

		#region Setup

		private void RandomizeHappySmiley()
		{
			string[] smileys = new string[]
			{
				"＼(^o^)／",
				"↖(^▽^)↗",
				"ヽ(•‿•)ノ",
				"(ﾉ^_^)ﾉ",
				"ヽ(＾Д＾)ﾉ",
				"ლ(╹◡╹ლ)",
				"\\(◦'◡'◦)/",
				"ヽ(^o^)ノ",
				"✌(◠▽◠)",
				"ヽ(ʘ‿ʘ)ノ",
				"＼(ʘ‿ʘ)／",
				"ヽ(◉‿◉)ノ",
				"＼(◉‿◉)／"
			};
			string newSmiley = smileys[UnityEngine.Random.Range(0, smileys.Length)];

			while (happySmiley == newSmiley)
				newSmiley = smileys[UnityEngine.Random.Range(0, smileys.Length)];

			happySmiley = newSmiley;
		}
		private void RandomizeErrorSmiley()
		{
			string[] smileys = new string[]
			{
				"¯\\_(ʘ_ʘ)_/¯",
				"(・_・)",
				"(˘_˘٥)",
				"(✖╭╮✖)",
				"へ（>_<へ)",
				"¯\\_(Ω_Ω)_/¯",
				"(´･_･`)",
				"(-_-)ゞ",
				"(・_・)ゞ",
				"「(°ヘ°)",
				"ヽ(ಠ_ಠ)ノ",
				"┌(ಠ_ಠ)ノ",
				"¯\\_(´・_・`)_/¯"
			};
			string newSmiley = smileys[UnityEngine.Random.Range(0, smileys.Length)];

			while (errorSmiley == newSmiley)
				newSmiley = smileys[UnityEngine.Random.Range(0, smileys.Length)];

			errorSmiley = newSmiley;
		}
		private void AssignRecommendedLayers()
		{
			// List of layers to add
			List<string> layersToAdd = new()
			{
				"Post Processing",
				"Vehicles",
				"Vehicle Wheels",
				"Ground Surfaces",
				"Obstacles",
				"Buildings"
			};

			// Remove layers that already exist; We start from the end of the list to prevent errors
			for (int i = layersToAdd.Count - 1; i >= 0; i--)
				if (LayersManager.LayerExists(layersToAdd[i]))
					layersToAdd.RemoveAt(i);

			// A flag to check if recommended layers have been added or not
			bool layersAdded = true;

			// Check if there are layers to be added
			if (layersToAdd.Count > 0)
			{
				// Ask for confirmation from user
				if (layersAdded = EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", $"We are about to add the following layers to your project:\r\n- {string.Join("\r\n- ", layersToAdd)}", "Continue", "Cancel"))
				{
					// Check if user has enough spots left to add the recommended layers; 
					if (LayersManager.GetLayers().Length + layersToAdd.Count >= LayersManager.MaxLayersCount)
						EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "We couldn't add the recommended layers to your project because there are not enough spots.", "Okay");
					else
						for (int i = 0; i < layersToAdd.Count; i++)
							LayersManager.AddLayer(layersToAdd[i]);

					EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "All layers have been added successfully!", "Okay");
				}
			}

			if (layersAdded)
			{
				// Continue with assigning layers
				vehiclesLayer = "Vehicles";
				vehicleWheelsLayer = "Vehicle Wheels";
			}
		}
		private void AssignSuggestedAudioPath()
		{
			// Assign the suggested path to the audio path
			audioFolderFullPath = SuggestedAudioFolderFullPath;
			audioFolderPath = suggestedAudioFolderPath;
		}
		private void DisplaySetupProgressBar(string task, float progress, string downloadSpeed)
		{
			if (!showSetupProgressBar)
				showSetupProgressBar = true;

			setupProgress = Utility.Clamp01(progress);
			setupTask = task;
			setupDownloadSpeed = downloadSpeed;

			Repaint();
		}
		private void ClearSetupProgressBar()
		{
			showSetupProgressBar = false;

			Repaint();
		}
		private void ApplySetupSettings()
		{
			Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets().Where(asset => asset != null).ToArray();
			ToolkitSettings settings = ToolkitSettings.LoadData(true);

			if (!settings)
				settings = ToolkitBehaviourEditor.RecreateAssetFile(false);

			settings.Physics = physicsType;
			settings.vehiclesLayer = LayerMask.NameToLayer(vehiclesLayer);
			settings.vehicleWheelsLayer = LayerMask.NameToLayer(vehicleWheelsLayer);
			settings.valuesUnit = unitsType;
			settings.torqueUnit = unitsType;
			settings.powerUnit = Utility.UnitType.Metric;
			settings.editorValuesUnit = editorUnitsType;
			settings.editorTorqueUnit = editorUnitsType;
			settings.editorPowerUnit = Utility.UnitType.Metric;
			settings.inputSystem = inputSystem;
			settings.audioFolderPath = audioFolderPath;

			int settingsAssetIndex = Array.IndexOf(preloadedAssets, preloadedAssets.FirstOrDefault(asset => asset != null && asset.GetType() == typeof(ToolkitSettings)));

			if (settingsAssetIndex < 0)
			{
				Array.Resize(ref preloadedAssets, preloadedAssets.Length + 1);

				preloadedAssets[^1] = settings;
			}
			else
				preloadedAssets[settingsAssetIndex] = settings;

			InputSettings inputSystemSettings = Resources.Load(Path.Combine("Settings", "InputSystemSettings")) as InputSettings;
			int inputSystemSettingsAssetIndex = Array.IndexOf(preloadedAssets, preloadedAssets.FirstOrDefault(asset => asset != null && asset.GetType() == typeof(InputSettings)));

			if (inputSystemSettingsAssetIndex < 0)
			{
				Array.Resize(ref preloadedAssets, preloadedAssets.Length + 1);

				preloadedAssets[^1] = inputSystemSettings;
			}
			else
				preloadedAssets[inputSystemSettingsAssetIndex] = inputSystemSettings;

			PlayerSettings.SetPreloadedAssets(preloadedAssets);
		}
		private void TerminateSetup()
		{
			ToolkitPrefs.SetStruct(ToolkitConstants.InstallRenderPipeline, Utility.GetCurrentRenderPipeline());
			ToolkitPrefs.SetStruct(ToolkitConstants.InstallPlatform, EditorUtilities.GetCurrentBuildTarget());
			ToolkitPrefs.SetString(ToolkitConstants.InstallVersion, ToolkitInfo.Version);
			ToolkitPrefs.SetFlag(ToolkitConstants.SetupDone);
			ToolkitSettings.RefreshInternalErrors();

			ToolkitSettings settings = ToolkitSettings.LoadData(true);

			if (settings)
				EditorUtility.SetDirty(settings);
			
			ClearSetupProgressBar();
			RandomizeHappySmiley();

			foldout = SetupFoldout.Finish;

			Repaint();
		}
		private void CloseWindow()
		{
			ToolkitSettingsEditor.OpenWindow(ToolkitSettings.SettingsEditorFoldout.None);
			Close();
		}
		private void ShowErrorFoldout(string error)
		{
			foldout = SetupFoldout.Error;

			RandomizeErrorSmiley();
			Repaint();

			if (!string.IsNullOrEmpty(error))
				ToolkitDebug.Error(error);
		}

		#endregion

		#endregion

		#region Editor

		private void OnGUI()
		{
			if (ToolkitInfo.IsSetupDone && foldout != SetupFoldout.SetupDone && foldout != SetupFoldout.Finish)
			{
				foldout = SetupFoldout.SetupDone;

				Repaint();
			}
			else if (EditorApplication.isPlaying)
			{
				Close();

				return;
			}

			if (termsTextAreaStyle == default)
				termsTextAreaStyle = new(GUI.skin.box)
				{
					stretchWidth = true,
					stretchHeight = true,
					alignment = TextAnchor.UpperLeft,
					richText = true,
					normal = new()
					{
						textColor = EditorGUIUtility.isProSkin ? Color.white : GUI.contentColor,
						background = GUI.skin.box.normal.background,
						scaledBackgrounds = GUI.skin.box.normal.scaledBackgrounds
					}
				};

			if (happySmiley.IsNullOrEmpty())
				RandomizeHappySmiley();

			if (errorSmiley.IsNullOrEmpty())
				RandomizeErrorSmiley();

			switch (foldout)
			{
				case SetupFoldout.Startup:
					StartEditor();

					break;

				case SetupFoldout.Error:
					ErrorEditor();

					break;

				case SetupFoldout.Configurations:
					ConfigurationsEditor();

					break;

				case SetupFoldout.Import:
					InstallEditor();

					break;

				case SetupFoldout.Finish:
					FinishEditor();

					break;

				case SetupFoldout.SetupDone:
					SetupDoneEditor();

					break;
			}
		}
		private void StartEditor()
		{
			EditorGUI.LabelField(new(position.width * .5f - 400f, position.height * .5f - 340f, 800f, 512f), happySmiley, new GUIStyle(EditorStyles.boldLabel) { fontSize = 72, alignment = TextAnchor.MiddleCenter });
			EditorGUI.LabelField(new(position.width * .5f - 256f, position.height * .5f + 16f, 512f, EditorGUIUtility.singleLineHeight * 2.5f), "Thank you for downloading\r\nthe Multiversal Vehicle Controller!", new GUIStyle(EditorStyles.boldLabel) { fontSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight), alignment = TextAnchor.MiddleCenter });
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if (Application.internetReachability == NetworkReachability.NotReachable)
				EditorGUILayout.HelpBox("The Multiversal Vehicle Controller requires\r\ninternet connection to proceed the setup process!", MessageType.None);

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(Application.internetReachability == NetworkReachability.NotReachable);
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Let's get started!", new GUIStyle(GUI.skin.button) { fixedWidth = 256f, fixedHeight = EditorGUIUtility.singleLineHeight * 2f }))
				foldout = SetupFoldout.Configurations;

			if (foldout == SetupFoldout.Configurations && EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "Would you like to use the MVC recommended values?", "Yes", "No"))
			{
				AssignRecommendedLayers();
				AssignSuggestedAudioPath();
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Watch Tutorials", new GUIStyle(GUI.skin.button) { fixedWidth = 256f, fixedHeight = EditorGUIUtility.singleLineHeight * 2f }))
				OpenTutorialsPlaylist();

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
			GUILayout.Space(position.height * .5f - 192f);
		}
		private void ErrorEditor()
		{
			EditorGUI.LabelField(new(position.width * .5f - 400f, position.height * .5f - 350f, 800f, 512f), errorSmiley, new GUIStyle(EditorStyles.boldLabel) { fontSize = 72, alignment = TextAnchor.MiddleCenter });
			EditorGUI.LabelField(new(position.width * .5f - 300f, position.height * .5f, 600f, EditorGUIUtility.singleLineHeight * 5f), "Oopsie!\r\nWe have encountered some issues while setting up\r\nthe MVC for you! Please check the Console\r\nwindow for more details.", new GUIStyle(EditorStyles.boldLabel) { fontSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight), alignment = TextAnchor.MiddleCenter });
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if (Application.internetReachability == NetworkReachability.NotReachable)
				EditorGUILayout.HelpBox("The Multiversal Vehicle Controller requires\r\ninternet connection to proceed the setup process!", MessageType.None);

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUI.BeginDisabledGroup(Application.internetReachability == NetworkReachability.NotReachable);
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Try again", new GUIStyle(GUI.skin.button) { fixedWidth = 256f, fixedHeight = EditorGUIUtility.singleLineHeight * 2f }))
				foldout = SetupFoldout.Configurations;

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();
			GUILayout.Space(position.height * .5f - 208f + EditorGUIUtility.singleLineHeight * 2f);
		}
		private void ConfigurationsEditor()
		{
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(16f);
			EditorGUILayout.BeginVertical();
			GUILayout.Space(16f);
			EditorGUILayout.LabelField("Configurations", new GUIStyle(EditorStyles.boldLabel) { fixedHeight = EditorGUIUtility.singleLineHeight * 2f, fontSize = 24, alignment = TextAnchor.UpperLeft });
			GUILayout.Space(16f);

			float orgLabelWidth = EditorGUIUtility.labelWidth;

			EditorGUIUtility.labelWidth = 256f;

			EditorGUILayout.LabelField("Change the following settings to whatever fit your needs!", EditorStyles.boldLabel);
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("General", EditorStyles.miniBoldLabel);

			finalizingScroll = EditorGUILayout.BeginScrollView(finalizingScroll, GUILayout.Height(position.height - 134f));

			EditorGUILayout.BeginVertical(GUILayout.Width(600f));

			EditorGUI.indentLevel++;

			RenderPipelineAsset renderPipelineAsset = GraphicsSettings.currentRenderPipeline;

			if (!renderPipelineInitialized)
			{
				renderPipeline = Utility.GetCurrentRenderPipeline();
				renderPipelineInitialized = true;
			}

			Utility.RenderPipeline currentRenderPipeline = Utility.GetCurrentRenderPipeline();

			renderPipeline = (Utility.RenderPipeline)EditorGUILayout.Popup(new GUIContent("Render Pipeline", "The render pipeline used in this project"), (int)renderPipeline, renderPipelines);

			string renderPipelineIssue = default;
			bool hasIssues = false;

			switch (renderPipeline)
			{
				case Utility.RenderPipeline.HDRP:
					if (!renderPipelineAsset)
					{
						renderPipelineIssue = "This project doesn't have an HD Render Pipeline Asset added to its graphics settings! Please consider changing the render pipeline option above or go to 'Edit > Project Settings > Graphics' and add a Scriptable Render Pipeline Asset.";
						hasIssues = true;
					}
					else if (renderPipeline != currentRenderPipeline)
					{
						renderPipelineIssue = "This project doesn't have an HD Render Pipeline Asset added to its graphics settings, it's either a URP or a Custom Render Pipeline asset! Please consider changing the render pipeline option above or go to 'Edit > Project Settings > Graphics' and add an HD Render Pipeline Asset.";
						hasIssues = true;
					}

					break;

				case Utility.RenderPipeline.URP:
					if (!renderPipelineAsset)
					{
						renderPipelineIssue = "This project doesn't have a Universal Render Pipeline Asset added to its graphics settings! Please consider changing the render pipeline option above or go to 'Edit > Project Settings > Graphics' and add a Scriptable Render Pipeline Asset.";
						hasIssues = true;
					}
					else if (renderPipeline != currentRenderPipeline)
					{
						renderPipelineIssue = "This project doesn't have a Universal Render Pipeline Asset added to its graphics settings, it's either an HDRP or a Custom Render Pipeline asset! Please consider changing the render pipeline option above or go to 'Edit > Project Settings > Graphics' and add a Universal Render Pipeline Asset.";
						hasIssues = true;
					}

					break;

				case Utility.RenderPipeline.Custom:
					if (!renderPipelineAsset)
					{
						renderPipelineIssue = "This project doesn't have a Custom Render Pipeline Asset added to its graphics settings! Please consider changing the render pipeline option above or go to 'Edit > Project Settings > Graphics' and add a Scriptable Render Pipeline Asset.";
						hasIssues = true;
					}
					else if (renderPipeline != currentRenderPipeline)
					{
						renderPipelineIssue = "This project doesn't have a Custom Render Pipeline Asset added to its graphics settings, it's either a URP or an HDRP asset! Please consider changing the render pipeline option above or go to 'Edit > Project Settings > Graphics' and add a custom Scriptable Render Pipeline Asset.";
						hasIssues = true;
					}

					break;

				default:
					if (renderPipelineAsset)
					{
						renderPipelineIssue = "This project has currently a Render Pipeline Asset added to its graphics settings! Please consider changing the render pipeline option above or go to 'Edit > Project Settings > Graphics' and remove the current Scriptable Render Pipeline Asset.";
						hasIssues = true;
					}

					break;
			}

			if (!renderPipelineIssue.IsNullOrEmpty())
			{
				EditorGUI.indentLevel--;

				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUIUtility.labelWidth + 5f);
				EditorGUILayout.HelpBox(renderPipelineIssue, MessageType.Error);
				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel++;
			}

			physicsType = (ToolkitSettings.PhysicsType)EditorGUILayout.EnumPopup(new GUIContent("Physics", "The MVC physics type"), physicsType);

			bool isLegacyInputManagerEnabled = true;
			string inputsSystemIssue = default;

			inputSystem = (ToolkitSettings.InputSystem)EditorGUILayout.EnumPopup(new GUIContent("Inputs System", "The input system the MVC is going to use"), inputSystem);

			switch (inputSystem)
			{
				case ToolkitSettings.InputSystem.UnityLegacyInputManager:
					try
					{
						Input.GetKey(KeyCode.Backspace);

						EditorGUI.indentLevel--;

						EditorGUILayout.BeginHorizontal();
						GUILayout.Space(EditorGUIUtility.labelWidth + 5f);
						EditorGUILayout.HelpBox("In order to setup the Legacy Unity Input Manager we have to overwrite the existing `InputSettings` asset, located under the `ProjectSettings` directory. It's best to make a backup of your project before launching the install process.", MessageType.Info);
						EditorGUILayout.EndHorizontal();

						EditorGUI.indentLevel++;
					}
					catch
					{
						isLegacyInputManagerEnabled = false;
						inputsSystemIssue = "In order to use Unity's Legacy Input System, you need to enable it inside the Project's Player Settings.";
					}

					break;
			}

			if (!inputsSystemIssue.IsNullOrEmpty())
			{
				EditorGUI.indentLevel--;

				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUIUtility.labelWidth + 5f);
				EditorGUILayout.HelpBox(inputsSystemIssue, MessageType.Warning);
				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel++;
			}

			unitsType = (Utility.UnitType)EditorGUILayout.EnumPopup(new GUIContent("Gameplay Units", "The in-game units type"), unitsType);
			editorUnitsType = (Utility.UnitType)EditorGUILayout.EnumPopup(new GUIContent("Editor Units", "The MVC.units.Editor type"), editorUnitsType);

			EditorGUI.indentLevel--;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Layers", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			vehiclesLayer = LayerMask.LayerToName(EditorGUILayout.LayerField(new GUIContent("Vehicles Layer", "The vehicles layer used by the MVC"), LayerMask.NameToLayer(vehiclesLayer)));

			EditorGUI.indentLevel++;

			vehicleWheelsLayer = LayerMask.LayerToName(EditorGUILayout.LayerField(new GUIContent("Wheels Layer", "The vehicles layer used by the MVC"), LayerMask.NameToLayer(vehicleWheelsLayer)));

			EditorGUI.indentLevel--;

			bool vehiclesLayerIsEmpty = vehiclesLayer.IsNullOrEmpty() || vehiclesLayer == "Default";
			bool vehicleWheelsLayerIsEmpty = vehicleWheelsLayer.IsNullOrEmpty() || vehicleWheelsLayer == "Default";
			bool vehiclesLayerAndVehicleWheelsLayerAreSame = vehiclesLayer == vehicleWheelsLayer;
			bool hasLayerIssues = false;

			if (vehiclesLayerIsEmpty || vehicleWheelsLayerIsEmpty)
			{
				EditorGUI.indentLevel--;

				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUIUtility.labelWidth + 5f);
				EditorGUILayout.HelpBox("Please assign both the `Vehicles Layer` & `Wheels Layer` for the toolkit to differentiate between vehicle components.", MessageType.Warning);
				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel++;

				hasLayerIssues = true;
				hasIssues = true;
			}
			else if (vehiclesLayerAndVehicleWheelsLayerAreSame)
			{
				EditorGUI.indentLevel--;

				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUIUtility.labelWidth + 5f);
				EditorGUILayout.HelpBox("Setting the `Vehicles Layer` the same as the `Wheels Layer` might confuse the toolkit systems at Runtime.", MessageType.Warning);
				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel++;

				hasLayerIssues = true;
				hasIssues = true;
			}

			EditorGUI.indentLevel++;
			EditorGUI.indentLevel--;

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(EditorGUIUtility.labelWidth + 5f);

			if (GUILayout.Button(hasLayerIssues ? "Add Recommended Layers" : "Assign Recommended Layers"))
				AssignRecommendedLayers();

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			EditorGUI.indentLevel--;

			EditorGUILayout.LabelField("Audio", EditorStyles.miniBoldLabel);

			EditorGUI.indentLevel++;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.TextField(new GUIContent("Folder Path", "The audio folder in which the MVC necessary audio files are going to be extracted"), audioFolderPath.IsNullOrEmpty() ? "Choose a folder..." : Path.Combine("Resources", audioFolderPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar));

			if (GUILayout.Button(new GUIContent("...", "Browse..."), new GUIStyle(GUI.skin.button) { fixedWidth = 25f }))
			{
				string newAudioFolderFullPath = EditorUtility.OpenFolderPanel("Locate the Audio folder...", Path.Combine(Application.dataPath, suggestedAudioFolderParentPath, Path.GetDirectoryName(suggestedAudioFolderPath)), Path.GetFileName(suggestedAudioFolderPath)).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

				if (!newAudioFolderFullPath.IsNullOrEmpty())
				{
					string resourcesFolderPart = $"{Path.DirectorySeparatorChar}Resources{Path.DirectorySeparatorChar}";
					int lastResourcesFolderIndex = newAudioFolderFullPath.LastIndexOf(resourcesFolderPart);

					if (lastResourcesFolderIndex < 0)
						EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", $"The audio folder has to be somewhere inside a Project's Resources folder.\r\n\r\nExample: \"{Path.Combine(Path.GetFileName(Path.GetDirectoryName(Application.dataPath)), "Assets", suggestedAudioFolderParentPath).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)}\"", "Got it!");
					else
					{
						audioFolderFullPath = newAudioFolderFullPath;
						audioFolderPath = newAudioFolderFullPath.Remove(0, lastResourcesFolderIndex + resourcesFolderPart.Length);
					}
				}
			}

			EditorGUILayout.EndHorizontal();
			if (audioFolderPath.IsNullOrEmpty() || !audioFolderFullPath.Equals(SuggestedAudioFolderFullPath, StringComparison.OrdinalIgnoreCase))
			{
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUIUtility.labelWidth + 5f);

				if (GUILayout.Button("Use Suggested Path"))
					AssignSuggestedAudioPath();

				EditorGUILayout.EndHorizontal();
			}

			if (!audioFolderFullPath.IsNullOrEmpty() && Directory.Exists(audioFolderFullPath) && Directory.GetFiles(audioFolderFullPath, "*.*", SearchOption.TopDirectoryOnly).Any())
			{
				EditorGUI.indentLevel--;

				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(EditorGUIUtility.labelWidth + 5f);
				EditorGUILayout.HelpBox("It seems like the selected folder isn't empty and its content could be overridden if the names of some files match each other while extracting the necessary audio files.", MessageType.Info);
				EditorGUILayout.EndHorizontal();

				EditorGUI.indentLevel++;
			}

			EditorGUI.indentLevel--;

			EditorGUIUtility.labelWidth = orgLabelWidth;

			EditorGUILayout.EndVertical();
			EditorGUILayout.EndScrollView();
			EditorGUILayout.BeginHorizontal();

			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				hasIssues = true;

				EditorGUILayout.LabelField("The Multiversal Vehicle Controller requires internet connection to proceed the setup process!", EditorStyles.miniBoldLabel);
			}
			else if (hasIssues && !renderPipelineIssue.IsNullOrEmpty())
				EditorGUILayout.LabelField("We have a render pipeline issue that need to be fixed before proceeding with the setup process!", EditorStyles.miniBoldLabel);
			else if (hasIssues)
				EditorGUILayout.LabelField("Some issues need to be fixed before proceeding with the setup process!", EditorStyles.miniBoldLabel);
			else if (inputSystem == ToolkitSettings.InputSystem.UnityLegacyInputManager && !isLegacyInputManagerEnabled)
			{
				hasIssues = true;

				EditorGUILayout.LabelField("There are some issues that need to be fixed before proceeding with the setup process", EditorStyles.miniBoldLabel);
			}
			else if (audioFolderPath.IsNullOrEmpty())
			{
				hasIssues = true;

				EditorGUILayout.LabelField("You need to choose where to save the MVC audio files", EditorStyles.miniBoldLabel);
			}
			else
				EditorGUILayout.LabelField("Everything is ready!", EditorStyles.miniBoldLabel);

			if (GUILayout.Button("< Back", new GUIStyle(GUI.skin.button) { fixedWidth = 144f, fixedHeight = EditorGUIUtility.singleLineHeight * 1.25f }))
				foldout = SetupFoldout.Startup;

			EditorGUI.BeginDisabledGroup(hasIssues || EditorApplication.isCompiling);

			if (GUILayout.Button(EditorApplication.isCompiling ? "Please Wait..." : "Start Install", new GUIStyle(GUI.skin.button) { fixedWidth = 144f, fixedHeight = EditorGUIUtility.singleLineHeight * 1.25f }))
				if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Info", "You are about to start the setup! Do you want to proceed?", "Yes", "No"))
					StartImport();

			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndVertical();
			GUILayout.Space(16f);
			EditorGUILayout.EndHorizontal();
		}
		private void InstallEditor()
		{
			EditorGUI.LabelField(new(position.width * .5f - 400f, position.height * .5f - 320f, 800f, 512f), happySmiley, new GUIStyle(EditorStyles.boldLabel) { fontSize = 72, alignment = TextAnchor.MiddleCenter });
			EditorGUI.LabelField(new(position.width * .5f - 256f, position.height * .5f + 48f, 512f, EditorGUIUtility.singleLineHeight * 2.5f), "Please wait while we setup\r\nthe Multiversal Vehicle Controller for you...", new GUIStyle(EditorStyles.boldLabel) { fontSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight), alignment = TextAnchor.MiddleCenter });

			if (showSetupProgressBar)
			{
				EditorGUI.LabelField(new(position.width * .5f - 256f, position.height * .5f + 192f - EditorGUIUtility.singleLineHeight * 2.5f, 512f, EditorGUIUtility.singleLineHeight), setupTask, new GUIStyle(EditorStyles.boldLabel) { fixedWidth = 512f, alignment = TextAnchor.MiddleCenter });
				EditorGUI.ProgressBar(new(position.width * .5f - 192f, position.height * .5f + 192f - EditorGUIUtility.singleLineHeight, 384f, EditorGUIUtility.singleLineHeight * 1.25f), setupProgress, $"{Utility.Round(setupProgress * 100f, 1):0.0}%");
				EditorGUI.LabelField(new(position.width * .5f - 256f, position.height * .5f + 192f + EditorGUIUtility.singleLineHeight, 512f, EditorGUIUtility.singleLineHeight), setupDownloadSpeed, new GUIStyle(EditorStyles.boldLabel) { fixedWidth = 512f, alignment = TextAnchor.MiddleCenter });
			}
		}
		private void FinishEditor()
		{
			EditorGUI.LabelField(new(position.width * .5f - 400f, position.height * .5f - 360f, 800f, 512f), happySmiley, new GUIStyle(EditorStyles.boldLabel) { fontSize = 72, alignment = TextAnchor.MiddleCenter });
			EditorGUI.LabelField(new(position.width * .5f - 256f, position.height * .5f - EditorGUIUtility.singleLineHeight, 512f, EditorGUIUtility.singleLineHeight * 6f), "<size=36>Congratulations!</size>\r\n\r\nThe setup process has been done successfully.\r\n" +
#if MVC_COMMUNITY
				"Have fun!"
#else
				"All you have to do now is insert your license key!"
#endif
				, new GUIStyle(EditorStyles.boldLabel) { fontSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight), alignment = TextAnchor.MiddleCenter, richText = true });
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginVertical();

			if (GUILayout.Button("Open Settings Panel", new GUIStyle(GUI.skin.button) { fixedWidth = 256f, fixedHeight = EditorGUIUtility.singleLineHeight * 2f }))
				CloseWindow();

			if (GUILayout.Button("Watch tutorials...", new GUIStyle(GUI.skin.button) { fixedWidth = 256f, fixedHeight = EditorGUIUtility.singleLineHeight * 2f }))
				ToolkitBehaviourEditor.OpenTutorialsPlaylist();

			EditorGUILayout.EndVertical();
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(position.height * .5f - 160f - EditorGUIUtility.singleLineHeight * 2f);
		}
		private void SetupDoneEditor()
		{
			EditorGUI.LabelField(new(position.width * .5f - 400f, position.height * .5f - 320f, 800f, 512f), happySmiley, new GUIStyle(EditorStyles.boldLabel) { fontSize = 72, alignment = TextAnchor.MiddleCenter });
			EditorGUI.LabelField(new(position.width * .5f - 256f, position.height * .5f + 48f, 512f, EditorGUIUtility.singleLineHeight * 2.5f), "Well! We have already setup everything for you...", new GUIStyle(EditorStyles.boldLabel) { fontSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight), alignment = TextAnchor.MiddleCenter, richText = true });
			GUILayout.FlexibleSpace();
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if (GUILayout.Button("Close", new GUIStyle(GUI.skin.button) { fixedWidth = 256f, fixedHeight = EditorGUIUtility.singleLineHeight * 2f }))
				Close();

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			GUILayout.Space(position.height * .5f - 192f + EditorGUIUtility.singleLineHeight * 2f);
		}

		#endregion

		#endregion

		#endregion
	}
}

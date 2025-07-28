#region Namespaces

using System;
using UnityEngine;
using UnityEditor;
using Utilities;
using Utilities.Editor;
using MVC.Editor;
using MVC.Internal;
using MVC.Internal.Editor;
using MVC.Utilities.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.AI.Editor
{
	public class VehicleAILayersEditorWindow : ToolkitEditorWindow
	{
		#region Enumerators

		public enum AILayersEditorFoldout { LayersList, ChaseMatrix }

		#endregion

		#region Variables

		#region Static Variables

		private static VehicleAILayersEditorWindow instance;
		private static AILayersEditorFoldout foldout;

		#endregion

		#region Global Variables

		private Vector2 scrollView;
		private string errorSmiley;
		private string layerName;
		private bool openSettingsPanelOnClose;
		private int currentLayer = -1;

		#endregion

		#endregion

		#region Methods

		#region Static Methods

		public static VehicleAILayersEditorWindow OpenWindow()
		{
			if (HasOpenInstances<VehicleAILayersEditorWindow>())
			{
				FocusWindowIfItsOpen<VehicleAILayersEditorWindow>();

				return instance;
			}

			string title = "MVC AI Layers"
#if MVC_COMMUNITY
				+ " (Pro Only)"
#endif
				;

			instance = GetWindow<VehicleAILayersEditorWindow>(false, title, true);
			instance.titleContent = new(
				title, EditorGUIUtility.IconContent("CustomTool").image);
			instance.minSize = new(350f, 600f);

			if (!WindowEditorCheck())
				FixInternalProblems(Selection.activeObject);

			return instance;
		}
		public static VehicleAILayersEditorWindow OpenWindow(bool openSettingsPanelOnClose)
		{
			instance = OpenWindow();
			instance.openSettingsPanelOnClose = openSettingsPanelOnClose;

			return instance;
		}
		public static void EnableFoldout(AILayersEditorFoldout foldout)
		{
			VehicleAILayersEditorWindow.foldout = foldout;
		}

		private static bool WindowEditorCheck()
		{
			return !EditorApplication.isPlaying && (ToolkitInfo.IsSetupDone || HasInternalErrors) && ToolkitSettings.LoadData(true);
		}

		#endregion

		#region Global Methods

		#region Utilities

		private void AddLayer()
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Add New Layer");
			Settings.AddAILayer(new("New Layer"));
			EditorUtility.SetDirty(Settings);
		}
		private void EditLayer(int index)
		{
			currentLayer = index;
			layerName = Settings.AILayers[index].Name;
		}
		private void RemoveLayer(int index)
		{
			if (Settings.AILayers.Length < 2)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You are unable to remove the last remaining layer because the MVC AI system depends on it to work properly!", "Okay");

				return;
			}

			Undo.RegisterCompleteObjectUndo(Settings, "Remove Layer");
			Settings.RemoveAILayer(index);
			EditorUtility.SetDirty(Settings);
		}
		private void DuplicateLayer(int index)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Duplicate Layer");
			Settings.AddAILayer(new(Settings.AILayers[index]));
			EditorUtility.SetDirty(Settings);
		}
		private void SortLayers()
		{
			currentLayer = -2;
		}
		private void MoveLayer(int index, int newIndex)
		{
			Undo.RegisterCompleteObjectUndo(Settings, "Sort Layers");
			Settings.MoveAILayer(index, newIndex);
			EditorUtility.SetDirty(Settings);
		}
		private void SaveLayer()
		{
			if (currentLayer < -1)
				currentLayer = -1;

			if (currentLayer < 0 || currentLayer >= Settings.AILayers.Length)
				return;

			if (layerName.IsNullOrEmpty() || layerName.IsNullOrWhiteSpace())
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "A valid layer name is required!", "Okay");
			else if (Array.Find(Settings.AILayers, layer => layer.Name == layerName && layer != Settings.AILayers[currentLayer]))
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "The layer name does exist already!", "Okay");
			else
			{
				Undo.RegisterCompleteObjectUndo(Settings, "Change Layer");

				Settings.AILayers[currentLayer].Name = layerName;
				layerName = string.Empty;
				currentLayer = -1;

				EditorUtility.SetDirty(Settings);
			}
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

		#endregion

		#region Editor

		private void OnGUI()
		{
			if (EditorApplication.isCompiling)
			{
				EditorGUI.LabelField(new(position.width * .5f - 256f, position.height * .5f - EditorGUIUtility.singleLineHeight * 1.75f, 512f, EditorGUIUtility.singleLineHeight * 5f), "Please wait...\r\n\r\nIt seems like something\r\nis compiling...", new GUIStyle(EditorStyles.boldLabel) { fontSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight), alignment = TextAnchor.MiddleCenter });
				Repaint();

				return;
			}
			else if (HasInternalErrors || !ToolkitInfo.IsSetupDone)
			{
				if (errorSmiley.IsNullOrEmpty())
					RandomizeErrorSmiley();

				EditorGUI.LabelField(new(0, 0, position.width, position.height - EditorGUIUtility.singleLineHeight * 5.5f), errorSmiley, new GUIStyle(EditorStyles.boldLabel) { fontSize = 72, alignment = TextAnchor.MiddleCenter });
				EditorGUI.LabelField(new(position.width * .5f - 150f, position.height * .5f + EditorGUIUtility.singleLineHeight, 300f, EditorGUIUtility.singleLineHeight * 4f), "The Multiversal Vehicle Controller\r\nhas some issues that need\r\nto be fixed!", new GUIStyle(EditorStyles.boldLabel) { fontSize = Mathf.FloorToInt(EditorGUIUtility.singleLineHeight), alignment = TextAnchor.MiddleCenter });
				GUILayout.FlexibleSpace();
				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Let me see!", new GUIStyle(GUI.skin.button) { fixedWidth = 250f, fixedHeight = EditorGUIUtility.singleLineHeight * 2f }))
					ToolkitSettingsEditor.OpenWindow();

				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
				GUILayout.Space(position.height * .5f - 144f);
				Repaint();

				return;
			}
			else if (openSettingsPanelOnClose)
			{
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Width(position.width));

				if (GUILayout.Button(new GUIContent(" Settings Panel", EditorUtilities.Icons.ChevronLeft, "Go back to Settings Panel"), new GUIStyle(EditorStyles.toolbarButton) { fixedWidth = 105f, stretchWidth = false, stretchHeight = true }))
					Close();

				EditorGUILayout.EndHorizontal();
			}

			if (EditorApplication.isPlaying)
			{
				EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Width(position.width));
				EditorGUILayout.LabelField("You can't edit some settings in play mode.", EditorStyles.miniLabel, GUILayout.Width(position.width - 100f));
				GUILayout.FlexibleSpace();

				if (GUILayout.Button("Exit Play Mode", EditorStyles.toolbarButton))
					EditorApplication.isPlaying = false;

				EditorGUILayout.EndHorizontal();
			}

			bool showList = foldout == AILayersEditorFoldout.LayersList;
			bool showMatrix = foldout == AILayersEditorFoldout.ChaseMatrix;

			scrollView = EditorGUILayout.BeginScrollView(scrollView);

			GUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayout.BeginVertical();
			ToolkitEditorUtility.ToggleTabButtons("Layers List", "Chase Matrix", ref showList, ref showMatrix, EditorGUIUtility.singleLineHeight * 1.5f);
			EditorGUILayout.Space();

			if (showList)
				EnableFoldout(AILayersEditorFoldout.LayersList);
			else if (showMatrix)
				EnableFoldout(AILayersEditorFoldout.ChaseMatrix);

			switch (foldout)
			{
				case AILayersEditorFoldout.ChaseMatrix:
					ChaseMatrixEditor();

					break;

				default:
					LayersEditor();

					break;
			}

			GUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayout.EndVertical();
			GUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndScrollView();
			Repaint();
		}
		private void LayersEditor()
		{
			EditorGUILayout.BeginHorizontal();

			if (currentLayer < -1)
			{
				if (GUILayout.Button(EditorUtilities.Icons.ChevronLeft, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					SaveLayer();

				GUILayout.Space(5f);
				EditorGUILayout.LabelField("Sorting Layers", EditorStyles.boldLabel);
			}
			else
			{
				EditorGUILayout.LabelField($"Layers ({Settings.AILayers.Length})", EditorStyles.boldLabel);
				GUILayout.FlexibleSpace();
				EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying || currentLayer > -1);
				EditorGUI.BeginDisabledGroup(Settings.AILayers.Length < 2);

				if (GUILayout.Button(EditorUtilities.Icons.Sort, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					SortLayers();

				EditorGUI.EndDisabledGroup();

				if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					AddLayer();

				EditorGUI.BeginDisabledGroup(Settings.AILayers.Length < 2);

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
					if (EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "You are trying to remove all the available AI layers. Are you sure?", "Yes", "No"))
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Remove Layers");
						Settings.ResetAILayers();
						EditorUtility.SetDirty(Settings);
					}

				EditorGUI.EndDisabledGroup();
				EditorGUI.EndDisabledGroup();
			}

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			for (int i = 0; i < Settings.AILayers.Length; i++)
			{
				EditorGUILayout.BeginHorizontal(GUI.skin.box);

				if (currentLayer < -1)
				{
					EditorGUI.BeginDisabledGroup(i == 0);

					if (GUILayout.Button(EditorUtilities.Icons.CaretUp, ToolkitEditorUtility.UnstretchableMiniButtonLeft))
						MoveLayer(i, i - 1);

					EditorGUI.EndDisabledGroup();
					EditorGUI.BeginDisabledGroup(i >= Settings.AILayers.Length - 1);

					if (GUILayout.Button(EditorUtilities.Icons.CaretDown, ToolkitEditorUtility.UnstretchableMiniButtonRight))
						MoveLayer(i, i + 1);

					EditorGUI.EndDisabledGroup();
					GUILayout.Space(5f);
				}

				if (currentLayer == i)
					layerName = EditorGUILayout.TextField(layerName);
				else
					EditorGUILayout.LabelField(Settings.AILayers[i].Name, EditorStyles.miniBoldLabel);

				if (currentLayer > -2)
				{
					EditorGUI.BeginDisabledGroup(currentLayer > -1 || EditorApplication.isPlaying);

					if (currentLayer == i)
					{
						EditorGUI.EndDisabledGroup();

						if (GUILayout.Button(EditorUtilities.Icons.Save, ToolkitEditorUtility.UnstretchableMiniButtonWide))
							SaveLayer();

						EditorGUI.BeginDisabledGroup(true);
					}
					else if (GUILayout.Button(EditorUtilities.Icons.Pencil, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						EditLayer(i);

					if (GUILayout.Button(EditorUtilities.Icons.Clone, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						DuplicateLayer(i);

					if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
						RemoveLayer(i);

					EditorGUI.EndDisabledGroup();
				}

				EditorGUILayout.EndHorizontal();
			}
		}
		private void ChaseMatrixEditor()
		{
			GUILayoutOption[] toggleOptions = new GUILayoutOption[]
			{
				GUILayout.Width(EditorGUIUtility.singleLineHeight),
				GUILayout.Height(EditorGUIUtility.singleLineHeight)
			};

			float maxVerticalLabelWidth = default;
			float maxLabelWidth = default;

			for (int i = 0; i < Settings.AILayers.Length; i++)
			{
				maxVerticalLabelWidth = Mathf.Max(maxVerticalLabelWidth, Settings.AILayers[i].Name.Length * (3f + (EditorGUIUtility.singleLineHeight * 2f / 3f)));
				maxLabelWidth = Mathf.Max(maxLabelWidth, EditorStyles.label.CalcSize(new GUIContent(Settings.AILayers[i].Name)).x);
			}

			GUILayoutOption[] verticalLabelOptions = new GUILayoutOption[]
			{
				GUILayout.Width(EditorGUIUtility.singleLineHeight),
				GUILayout.Height(maxVerticalLabelWidth + EditorGUIUtility.singleLineHeight * 2f)
			};

			EditorGUI.BeginDisabledGroup(EditorApplication.isPlaying);
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			bool selectAll = GUILayout.Button("Select All", EditorStyles.miniButton);
			bool deselectAll = GUILayout.Button("Deselect All", EditorStyles.miniButton);

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();

			for (int i = 0; i < Settings.AILayers.Length; i++)
			{
				EditorGUILayout.BeginHorizontal();

				if (i < 1)
				{
					EditorGUILayout.BeginVertical(verticalLabelOptions);
					GUILayout.FlexibleSpace();
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.BeginVertical();
					GUILayout.FlexibleSpace();
				}

				EditorGUILayout.PrefixLabel(Settings.AILayers[i].Name);

				if (i < 1)
				{
					EditorGUILayout.EndVertical();
					EditorGUILayout.BeginVertical();
					GUILayout.FlexibleSpace();

					string layerName = "Player";

					for (int k = 0; k < layerName.Length; k++)
						EditorGUILayout.LabelField(new GUIContent(layerName[k].ToString(), layerName), GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f / 3f));

					GUILayout.Space(EditorGUIUtility.singleLineHeight * .5f);
				}

				bool canChase = Settings.CanAILayerChasePlayer(i);
				bool newCanChase = EditorGUILayout.Toggle(canChase && !deselectAll || selectAll, toggleOptions);

				if (canChase != newCanChase)
				{
					Undo.RegisterCompleteObjectUndo(Settings, "Change Chase");
					Settings.SetAILayerChasePlayer(i, newCanChase);
					EditorUtility.SetDirty(Settings);

					if (!selectAll && !deselectAll)
						ToolkitDebug.Log($"{Settings.AILayers[i].Name} {(newCanChase ? "will" : "won't")} chase Player{(newCanChase ? "" : " anymore")}.");
				}

				if (i < 1)
				{
					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.EndVertical();
				}

				for (int j = 0; j < Settings.AILayers.Length; j++)
				{
					if (i < 1)
					{
						EditorGUILayout.BeginVertical(verticalLabelOptions);
						GUILayout.FlexibleSpace();

						string layerName = Settings.AILayers[j].Name;

						for (int k = 0; k < layerName.Length; k++)
							EditorGUILayout.LabelField(new GUIContent(layerName[k].ToString(), layerName), GUILayout.Width(EditorGUIUtility.singleLineHeight), GUILayout.Height(EditorGUIUtility.singleLineHeight * 2f / 3f));
						
						GUILayout.Space(EditorGUIUtility.singleLineHeight * .5f);
					}
					else
						GUILayout.Space(3f);

					EditorGUI.BeginDisabledGroup(i == j);

					canChase = Settings.CanAILayersChase(i, j);
					newCanChase = EditorGUILayout.Toggle(canChase && !deselectAll || selectAll, toggleOptions);

					if (canChase != newCanChase)
					{
						Undo.RegisterCompleteObjectUndo(Settings, "Change Chase");
						Settings.SetAILayerPairChase(i, j, newCanChase);
						EditorUtility.SetDirty(Settings);

						if (!selectAll && !deselectAll)
							ToolkitDebug.Log($"{Settings.AILayers[i].Name} {(newCanChase ? "will" : "won't")} chase {Settings.AILayers[j].Name}{(newCanChase ? "" : " anymore")}.");
					}

					EditorGUI.EndDisabledGroup();

					if (i < 1)
						EditorGUILayout.EndVertical();
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUI.EndDisabledGroup();
		}
		private void OnDestroy()
		{
			if (openSettingsPanelOnClose)
				ToolkitSettingsEditor.OpenWindow();
		}

		#endregion

		#endregion

		#endregion
	}
}

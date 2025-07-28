#region Namespaces

using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEditor;
using Utilities;
using MVC.Utilities.Internal;
using MVC.Editor;

#endregion

namespace MVC.Internal.Editor
{
	public class ToolkitChangeLogEditor : ToolkitEditorWindow
	{
		#region Variables

		#region Static Variables

		private static ToolkitChangeLogEditor instance;

		#endregion

		#region Global Variables

		private Vector2 scroll;
		private string changeLog;
		private bool loaded;

		#endregion

		#endregion

		#region Methods

		#region Static Methods

		[MenuItem("Tools/Multiversal Vehicle Controller/Help/View Changelog", false, 6)]
		public static ToolkitChangeLogEditor OpenWindow()
		{
			if (HasOpenInstances<ToolkitChangeLogEditor>())
			{
				FocusWindowIfItsOpen<ToolkitChangeLogEditor>();

				return instance;
			}

			instance = GetWindow<ToolkitChangeLogEditor>(true, "Multiversal Vehicle Controller: Change Log", true);
			instance.minSize = new(450f, 600f);
			instance.maxSize = instance.minSize;

			instance.Focus();

			return instance;
		}

		#endregion

		#region Global Methods

		private void OnGUI()
		{
			if (!loaded)
			{
				try
				{
					string requestURL = $"{ToolkitInfo.Website}/client/unity/changelog";

					requestURL += $"?ver={Uri.EscapeUriString(ToolkitInfo.Version)}";

					EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller", "Connecting...", 0f);

					using UnityWebRequest request = UnityWebRequest.Get(requestURL);
					request.disposeDownloadHandlerOnDispose = true;
					request.disposeCertificateHandlerOnDispose = true;
					request.certificateHandler = new ToolkitNetworking.BypassCertificate();

					request.SendWebRequest();
					EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller", "Getting data...", .5f);

					while (request.result == UnityWebRequest.Result.InProgress)
						continue;

					EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller", "Parsing data...", 1f);

					if (request.result == UnityWebRequest.Result.Success)
						changeLog = request.downloadHandler.text;
					else
						ToolkitDebug.Error($"We have had some errors receiving the MVC's change log...\r\nError Code: {request.responseCode}\r\nError: {request.error}");

					EditorUtility.ClearProgressBar();
					request.Dispose();
				}
				catch (Exception e)
				{
					ToolkitDebug.Error($"We have had some errors receiving the MVC's change log...\r\nError: {e.Message}");
				}
				finally
				{
					EditorUtility.ClearProgressBar();

					loaded = true;
				}
			}
			else if (changeLog.IsNullOrEmpty())
				changeLog = "We've had some errors getting data from our servers!";

			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(5f);
			EditorGUILayout.BeginVertical();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField($"Change Log (v{ToolkitInfo.Version})", new GUIStyle(EditorStyles.boldLabel) { fixedWidth = 400f, fixedHeight = 32f, fontSize = 24 });
			EditorGUILayout.LabelField("See what's new on this version and other older versions!", new GUIStyle(EditorStyles.boldLabel) { fixedWidth = 400f, fixedHeight = 32f });
			EditorGUILayout.Space();

			scroll = EditorGUILayout.BeginScrollView(scroll);

			EditorGUILayout.TextArea(changeLog, new GUIStyle(GUI.skin.box)
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
			});
			EditorGUILayout.EndScrollView();
			EditorGUILayout.Space();
			EditorGUILayout.EndVertical();
			GUILayout.Space(5f);
			EditorGUILayout.EndHorizontal();
		}

		#endregion

		#endregion
	}
}

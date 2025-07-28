#region Namespaces

using System;
using System.Net;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Utilities;
using Utilities.Editor;
using MVC.Editor;
using MVC.Utilities.Internal;
using MVC.Utilities.Editor;

#endregion

namespace MVC.Internal.Editor
{
	internal class ToolkitContactWindow : ToolkitEditorWindow
	{
		#region Enumerators

		private enum ReportAbout { Select, Toolkit, InputsManager, Website, Other }
		private enum Subject { Select, AskingForHelp, Suggestion, FeedbackOrReview, PlanUpgrade, TermsOfUse, CCPAPolicies, PrivacyPolicies, RefundAndReturnPolicies, AskingForRefund, ReportError, Other }
		private enum Occurrence { Select, ThisIsTheFirstTime, SometimesButNotAlways, Always }
		private enum Foldout { Contact, Error, Success }

		#endregion

		#region Variables

		#region Static Variables

		private static ToolkitContactWindow instance;
		private static Vector2 scrollView;

		#endregion

		#region Global Variables

		private bool IsErrorReporter => subject == Subject.ReportError;
		private Foldout foldout;
		private string fullName;
		private string email;
		private Subject subject;
		private string reportTitle;
		private ReportAbout reportAbout;
		private string otherAbout;
		private Occurrence occurrence;
		private string description;
		private string reproduction;
		private bool includeConsoleLog = true;
		private bool includeLog = true;
		private bool includeWarning = true;
		private bool includeError = true;
		private readonly List<string> attachments = new();
		private readonly List<bool> attachmentsValidity = new();
		private ToolkitLogger.Log[] logs;

		#endregion

		#endregion

		#region Methods

		#region Static Methods

		public static ToolkitContactWindow OpenWindow(bool errorReporter)
		{
			if (instance)
				instance.subject = errorReporter ? Subject.ReportError : instance.subject;

			if (HasOpenInstances<ToolkitContactWindow>())
			{
				FocusWindowIfItsOpen<ToolkitContactWindow>();

				return instance;
			}

			instance = GetWindow<ToolkitContactWindow>(true, $"Multiversal Vehicle Controller: {(errorReporter ? "Error Reporter" : "Contact Us")}", true);
			instance.minSize = new(450f, 600f);
			instance.maxSize = instance.minSize;
			instance.subject = errorReporter ? Subject.ReportError : Subject.Select;

			return instance;
		}

		#endregion

		#region Global Methods

		#region Utilities

		private void Submit()
		{
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Error", "Please verify your internet connection!", "Okay");

				return;
			}

			EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", "Validating Data...", 0f);

			if (!Utility.ValidateName(fullName))
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "A valid full name is required!", "Okay");
			else if (!Utility.ValidateEmail(email, true))
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "A valid email address is required!", "Okay");
			else if (subject == Subject.Select || (IsErrorReporter || subject == Subject.Other) && (reportTitle?.Trim()).IsNullOrEmpty())
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", $"A valid {(IsErrorReporter ? "title" : "subject")} is required!", "Okay");
			else if (reportAbout == ReportAbout.Select || reportAbout == ReportAbout.Other && (otherAbout?.Trim()).IsNullOrEmpty())
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", $"A valid {(IsErrorReporter ? "report" : "message")} concern is required!", "Okay");
			else if (IsErrorReporter && occurrence == Occurrence.Select)
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "A valid problem occurrence is required!", "Okay");
			else if ((description?.Trim()).IsNullOrEmpty() || description.Trim().Length < (IsErrorReporter ? 10 : 50))
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", $"A valid {(IsErrorReporter ? "description" : "message")} is required!", "Okay");
			else if (IsErrorReporter && ((reproduction?.Trim()).IsNullOrEmpty() || (reproduction?.Trim()).Length < 10))
				EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", "Valid reproduction steps are required!", "Okay");
			else
			{
				for (int i = 0; i < attachments.Count; i++)
				{
					EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", $"Validating Attachments... ({i}/{attachments.Count})", (float)i / attachments.Count);

					string attachment = attachments[i];

					if (!attachmentsValidity[i] && !Utility.ValidateURL(ref attachment, true, false))
					{
						EditorUtility.DisplayDialog("Multiversal Vehicle Controller: Warning", $"Attachment number {i + 1} is invalid. A valid URL is required!", "Okay");

						return;
					}

					attachmentsValidity[i] = true;
					attachments[i] = attachment;
				}

				if (IsErrorReporter)
				{
					if (includeConsoleLog)
					{
						EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", "Collecting Logs...", 1f);

						List<LogType> logTypes = new();

						if (includeLog)
							logTypes.Add(LogType.Log);

						if (includeWarning)
							logTypes.Add(LogType.Warning);

						if (includeError)
						{
							logTypes.Add(LogType.Error);
							logTypes.Add(LogType.Assert);
							logTypes.Add(LogType.Exception);
						}

						logs = ToolkitLogger.Get(true, logTypes.ToArray());

						logTypes.Clear();
					}
					else
						logs = null;
				}

				EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", $"Preparing {(IsErrorReporter ? "Report" : "Message")}...", 1f);
				Repaint();
				SendReport();
			}
		}
		private string GetContactDetailsJson()
		{
			string message = string.Empty;

			message += $"Concern: {(reportAbout == ReportAbout.Other ? otherAbout : reportAbout)}\n";

			if (IsErrorReporter)
			{
				message += $"Occurrence: {occurrence}\n";
				message += $"Report Title: {description}\n";
				message += $"Description:\n{description}\r\n";
				message += $"Reproduction Steps:\n{reproduction}";

				if (includeConsoleLog && logs != null && logs.Length > 0)
				{
					message += $"\r\nConsole Log:\n";

					foreach (ToolkitLogger.Log log in logs)
						message += $"{log}\r\n";
				}
			}
			else
				message += $"Message:\n{description}";

			if (attachments.Count > 0)
			{
				message += "\r\nAttachments:";

				for (int i = 0; i < attachments.Count; i++)
					message += $"\nLink/URL {i:00}: {attachments[i]}";
			}

			Dictionary<string, string> details = new()
			{
				{ "full_name", fullName },
				{ "email", email },
				{
					"subject",
					subject switch
					{
						Subject.AskingForHelp => "help",
						Subject.PlanUpgrade => "upgrade",
						Subject.AskingForRefund => "refund",
						Subject.CCPAPolicies => "ccpa",
						Subject.FeedbackOrReview => "feedback",
						Subject.PrivacyPolicies => "privacy_policies",
						Subject.RefundAndReturnPolicies => "return_policies",
						Subject.ReportError => "report_error",
						Subject.Suggestion => "suggestion",
						Subject.TermsOfUse => "terms_of_use",
						_ => "other"
					}
				},
				{ "other_subject", reportTitle },
				{ "message", message }
			};

			return $"{{{string.Join(",", details.Select(pair => $"\"{pair.Key}\": \"{pair.Value}\""))}}}";
		}
		private void SendReport()
		{
			string requestURL = $"{ToolkitInfo.Website}/contact";
			string contactDetailsJson = GetContactDetailsJson();

			requestURL += $"?contact_details={WebUtility.UrlEncode(Convert.ToBase64String(Encoding.UTF8.GetBytes(contactDetailsJson)))}";

			using UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(requestURL);

			try
			{
				request.disposeDownloadHandlerOnDispose = true;
				request.disposeCertificateHandlerOnDispose = true;
				request.certificateHandler = new ToolkitNetworking.BypassCertificate();

				request.SendWebRequest();

				while (request.result == UnityEngine.Networking.UnityWebRequest.Result.InProgress)
				{
					EditorUtility.DisplayProgressBar("Multiversal Vehicle Controller: Please Wait...", $"Sending {(IsErrorReporter ? "Report" : "Message")}...", 1f);

					continue;
				}

				if (request.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
				{
					foldout = request.downloadHandler.text?.Trim() switch
					{
						null or "" => Foldout.Success,
						_ => Foldout.Error
					};

					if (foldout == Foldout.Error)
						ToolkitDebug.Error($"{(IsErrorReporter ? "Report" : "Message")} couldn't be sent due to server errors, please try again!");
				}
				else
				{
					ToolkitDebug.Error($"{(IsErrorReporter ? "Report" : "Message")} couldn't be sent, please try again!\r\nResponse: {request.result} ({request.responseCode})\r\nError: {request.error}");

					foldout = Foldout.Error;
				}
			}
			catch (WebException e)
			{
				ToolkitDebug.Error($"{(IsErrorReporter ? "Report" : "Message")} couldn't be sent due to internal errors!\r\nError: {e.Message}\r\nStackTrace: {e.StackTrace}");

				foldout = Foldout.Error;
			}
			finally
			{
				request.Dispose();
				EditorUtility.ClearProgressBar();
			}
		}

		#endregion

		#region GUI

		private void OnGUI()
		{
			switch (foldout)
			{
				case Foldout.Error:
					ErrorEditor();

					break;

				case Foldout.Success:
					SuccessEditor();

					break;

				default:
					scrollView = EditorGUILayout.BeginScrollView(scrollView);

					EditorGUILayout.BeginHorizontal();
					GUILayout.Space(10f);
					EditorGUILayout.BeginVertical();
					GUILayout.Space(10f);
					ContactEditor();
					GUILayout.Space(10f);
					EditorGUILayout.EndVertical();
					GUILayout.Space(10f);
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.EndScrollView();

					break;
			}
		}
		private void ContactEditor()
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(IsErrorReporter ? "Report Error" : "Contact Us", new GUIStyle(EditorStyles.boldLabel)
			{
				fixedHeight = EditorGUIUtility.singleLineHeight * 3f,
				alignment = TextAnchor.UpperLeft,
				fontSize = 20
			});
			GUILayout.FlexibleSpace();

			string submitButtonText = IsErrorReporter ? "Submit Report" : "Send Message";

			if (GUILayout.Button(submitButtonText, new GUIStyle(GUI.skin.button)
			{
				fixedHeight = EditorGUIUtility.singleLineHeight * 1.5f,
				fixedWidth = GUI.skin.button.CalcSize(new GUIContent(submitButtonText)).x * 1.125f
			}))
				Submit();

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Fill in the form bellow with the necessary details and try to make it as clear as possible, so we can reply to you faster. We appreciate your effort helping us improve the toolkit!", new GUIStyle(EditorStyles.miniBoldLabel)
			{
				fixedHeight = EditorGUIUtility.singleLineHeight * 3.5f,
				fixedWidth = position.width - 100f,
				alignment = TextAnchor.UpperLeft,
				wordWrap = true
			});
			EditorGUILayout.Space();

			float orgLabelWidth = EditorGUIUtility.labelWidth;

			EditorGUIUtility.labelWidth = 180f;
			fullName = EditorGUILayout.TextField("Full Name *", fullName);
			email = EditorGUILayout.TextField("Email Address *", email);

			EditorGUILayout.Space();

			if (!IsErrorReporter)
			{
				subject = (Subject)EditorGUILayout.EnumPopup("Subject *", subject);

				if (subject == Subject.ReportError && instance)
					instance.titleContent = new("Multiversal Vehicle Controller: Error Reporter");
			}

			if (IsErrorReporter || subject == Subject.Other)
				reportTitle = EditorGUILayout.TextField(IsErrorReporter ? "Title *" : " ", reportTitle);

			reportAbout = (ReportAbout)EditorGUILayout.EnumPopup($"What is the {(IsErrorReporter ? "problem" : "message")} about? *", reportAbout);

			if (reportAbout == ReportAbout.Other)
				otherAbout = EditorGUILayout.TextField(" ", otherAbout);

			if (IsErrorReporter)
			{
				occurrence = (Occurrence)EditorGUILayout.EnumPopup("How often does it happen? *", occurrence);
				EditorGUIUtility.labelWidth = position.width - 26f;

				EditorGUILayout.PrefixLabel("Describe what's happening? *");

				description = EditorGUILayout.TextArea(description, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 4f), GUILayout.Width(position.width - 26f));

				EditorGUILayout.PrefixLabel("How to reproduce the error? (In detailed steps) *");

				reproduction = EditorGUILayout.TextArea(reproduction, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 4f), GUILayout.Width(position.width - 26f));
				EditorGUIUtility.labelWidth = 180f;
				includeConsoleLog = ToolkitEditorUtility.ToggleButtons("Include Current Console Log?", null, "Yes", "No", includeConsoleLog);

				if (includeConsoleLog)
				{
					EditorGUIUtility.labelWidth = 20f;

					EditorGUILayout.BeginHorizontal();

					EditorGUI.indentLevel++;

					includeLog = EditorGUILayout.ToggleLeft("Logs", includeLog);

					EditorGUI.indentLevel--;
					EditorGUI.indentLevel--;

					includeWarning = EditorGUILayout.ToggleLeft("Warnings", includeWarning);

					EditorGUI.indentLevel--;

					includeError = EditorGUILayout.ToggleLeft("Errors", includeError);

					EditorGUI.indentLevel++;
					EditorGUI.indentLevel++;

					EditorGUILayout.EndHorizontal();

					if (!includeLog && !includeWarning && !includeError)
					{
						includeLog = includeWarning = includeError = true;
						includeConsoleLog = false;
					}
				}
			}
			else
			{
				EditorGUILayout.PrefixLabel("Message Body *");

				description = EditorGUILayout.TextArea(description, GUILayout.MinHeight(EditorGUIUtility.singleLineHeight * 4f));
			}

			EditorGUILayout.Space();

			EditorGUIUtility.labelWidth = orgLabelWidth;

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Attachments");

			if (GUILayout.Button(EditorUtilities.Icons.Add, ToolkitEditorUtility.UnstretchableMiniButtonWide))
			{
				attachments.Add(string.Empty);
				attachmentsValidity.Add(false);
			}

			EditorGUILayout.EndHorizontal();

			EditorGUI.indentLevel++;

			for (int i = 0; i < attachments.Count; i++)
			{
				EditorGUILayout.BeginHorizontal(GUI.skin.box);

				string newAttachment = EditorGUILayout.TextField($"Link/URL {i + 1:00}", attachments[i]);

				if (attachments[i] != newAttachment)
				{
					attachmentsValidity[i] = false;
					attachments[i] = newAttachment;
				}

				if (GUILayout.Button(EditorUtilities.Icons.Trash, ToolkitEditorUtility.UnstretchableMiniButtonWide))
				{
					attachmentsValidity.RemoveAt(i);
					attachments.RemoveAt(i);
				}

				EditorGUILayout.EndHorizontal();
			}

			EditorGUI.indentLevel--;
		}
		private void ErrorEditor()
		{
			EditorGUI.DrawPreviewTexture(new(position.width * .5f - 64f, position.height * .5f - 128f, 128f, 128f), EditorUtilities.Icons.Error, new(Shader.Find("Unlit/Transparent")));
			EditorGUI.LabelField(new(0f, position.height * .5f, position.width, EditorGUIUtility.singleLineHeight * 4f), $"We've had some errors while sending your {(IsErrorReporter ? "report" : "message")}.\r\nCheck console for more details!", new GUIStyle(EditorStyles.boldLabel) { fontSize = 15, alignment = TextAnchor.MiddleCenter, fixedHeight = EditorGUIUtility.singleLineHeight * 4f });

			if (GUI.Button(new(position.width * .5f - 96f, position.height * .5f + EditorGUIUtility.singleLineHeight * 4f, 192f, EditorGUIUtility.singleLineHeight * 2f), "Try Again"))
				foldout = Foldout.Contact;
		}
		private void SuccessEditor()
		{
			EditorGUI.DrawPreviewTexture(new(position.width * .5f - 64f, position.height * .5f - 128f, 128f, 128f), EditorUtilities.Icons.CheckCircle, new(Shader.Find("Unlit/Transparent")) { color = Color.green });
			EditorGUI.LabelField(new(0f, position.height * .5f, position.width, EditorGUIUtility.singleLineHeight * 4f), $"Your {(IsErrorReporter ? "report" : "message")} has been sent successfully!", new GUIStyle(EditorStyles.boldLabel) { fontSize = 15, alignment = TextAnchor.MiddleCenter, fixedHeight = EditorGUIUtility.singleLineHeight * 4f });

			if (GUI.Button(new(position.width * .5f - 96f, position.height * .5f + EditorGUIUtility.singleLineHeight * 4f, 192f, EditorGUIUtility.singleLineHeight * 2f), $"New {(IsErrorReporter ? "report" : "message")}"))
			{
				foldout = Foldout.Contact;

				if (!IsErrorReporter)
					subject = Subject.Select;

				reportTitle = string.Empty;
				reportAbout = ReportAbout.Select;
				otherAbout = string.Empty;
				occurrence = Occurrence.Select;
				description = string.Empty;
				reproduction = string.Empty;
				includeConsoleLog = true;
				includeLog = true;
				includeWarning = true;
				includeError = true;

				attachments.Clear();
				attachmentsValidity.Clear();

				logs = null;
			}

			if (GUI.Button(new(position.width * .5f - 96f, position.height * .5f + EditorGUIUtility.singleLineHeight * 6.125f, 192f, EditorGUIUtility.singleLineHeight * 2f), "Close Window"))
				Close();
		}

		#endregion

		#endregion

		#endregion
	}
}

#region Namespaces

using UnityEngine;
using UnityEditor;
using MVC.Internal;

#endregion

namespace MVC.Editor
{
	public class ToolkitEditorWindow : EditorWindow
	{
		#region Variables

		public static ToolkitSettings Settings => ToolkitBehaviourEditor.Settings;
		public static VehicleManager Manager => ToolkitBehaviourEditor.Manager;
		public static bool HasInternalErrors => ToolkitBehaviourEditor.HasInternalErrors;
		public static bool IsSetupDone => ToolkitInfo.IsSetupDone;

		internal static ToolkitSettings BackupAsset => ToolkitBehaviourEditor.BackupAsset;

		#endregion

		#region Methods

		public static void VisitWebsite()
		{
			ToolkitBehaviourEditor.VisitWebsite();
		}
		public static void OpenTutorialsPlaylist()
		{
			ToolkitBehaviourEditor.OpenTutorialsPlaylist();
		}
		public static void WatchSetupInstallTutorial()
		{
			ToolkitBehaviourEditor.WatchSetupInstallTutorial();
		}
		public static void WatchSetupTutorial()
		{
			ToolkitBehaviourEditor.WatchSetupTutorial();
		}
		public static void VisitDownloadPage()
		{
			ToolkitBehaviourEditor.VisitDownloadPage();
		}
		public static void VisitAssetStore(ToolkitInfo.LicenseType license)
		{
			ToolkitBehaviourEditor.VisitAssetStore(license);
		}
		public static void VisitAssetStore()
		{
			ToolkitBehaviourEditor.VisitAssetStore(ToolkitInfo.License);
		}
		public static void ReviewOnAssetStore(ToolkitInfo.LicenseType license)
		{
			ToolkitBehaviourEditor.ReviewOnAssetStore(license);
		}
		public static void ReviewOnAssetStore()
		{
			ToolkitBehaviourEditor.ReviewOnAssetStore(ToolkitInfo.License);
		}
		public static void VisitDocumentation()
		{
			ToolkitBehaviourEditor.VisitDocumentation();
		}
		public static void ReportError()
		{
			ToolkitBehaviourEditor.ReportError();
		}
		public static void ContactUs()
		{
			ToolkitBehaviourEditor.ContactUs();
		}
		public static void About()
		{
			ToolkitBehaviourEditor.About();
		}
		public static ToolkitSettings RecreateAssetFile(bool backup)
		{
			return ToolkitBehaviourEditor.RecreateAssetFile(backup);
		}

		internal static void FixInternalProblems(Object selectAfterFix)
		{
			ToolkitBehaviourEditor.FixInternalProblems(selectAfterFix);
		}
		internal static void GUILayoutExperimentalIcon(bool expandLabel)
		{
			ToolkitBehaviourEditor.GUILayoutExperimentalIcon(expandLabel);
		}

		#endregion
	}
}

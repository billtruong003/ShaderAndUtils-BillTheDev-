#region Namespaces

using UnityEngine;
using MVC.Utilities.Internal;

#endregion

namespace MVC.Internal
{
	internal static class ToolkitPreBuild
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void OnLoad()
		{
			if (Application.isEditor)
				return;

			ToolkitSettings.LoadData(true);

			if (!ToolkitInfo.IsSetupDone)
				ToolkitDebug.Error("Setup is not done!");
			else if (ToolkitSettings.HasInternalErrors)
				ToolkitDebug.Error("We have had some internal errors");
		}
	}
}

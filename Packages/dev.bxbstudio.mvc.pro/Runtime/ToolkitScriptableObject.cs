#region Namespaces

using System;
using UnityEngine;
using MVC.Core;
using MVC.UI;

#endregion

namespace MVC
{
	[Serializable]
	public class ToolkitScriptableObject : ScriptableObject
	{
		public static ToolkitSettings Settings => ToolkitBehaviour.Settings;
		public static VehicleManager Manager => VehicleManager.Instance;
		public static VehicleUIController UIController => VehicleUIController.Instance;
		public static VehicleFollower Follower => VehicleFollower.Instance;
		public static AudioListener Listener => ToolkitBehaviour.Listener;
		public static bool HasInternalErrors => ToolkitBehaviour.HasInternalErrors;
	}
}

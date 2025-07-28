#region Namespaces

using System;
using UnityEngine;
using MVC.Core;
using MVC.UI;

#endregion

namespace MVC
{
	[Serializable]
	public class ToolkitComponent
	{
		#region Variables

		public static ToolkitSettings Settings => ToolkitBehaviour.Settings;
		public static VehicleManager Manager => VehicleManager.Instance;
		public static VehicleUIController UIController => VehicleUIController.Instance;
		public static VehicleFollower Follower => VehicleFollower.Instance;
		public static AudioListener Listener => ToolkitBehaviour.Listener;
		public static bool HasInternalErrors => ToolkitBehaviour.HasInternalErrors;

		#endregion

		#region Operators

		public static implicit operator bool(ToolkitComponent component) => component != null;

		#endregion
	}
}

#region Namespaces

using UnityEngine;
using MVC.Core;
using MVC.UI;
using MVC.Internal;

#endregion

namespace MVC
{
	[AddComponentMenu("")]
	public class ToolkitBehaviour : MonoBehaviour
	{
		#region Variables

		#region Static Variables

		public static ToolkitSettings Settings => ToolkitSettings.Instance;
		public static VehicleManager Manager => VehicleManager.Instance;
		public static VehicleUIController UIController => VehicleUIController.Instance;
		public static VehicleFollower Follower => VehicleFollower.Instance;
		public static AudioListener Listener => listener;
		public static bool HasInternalErrors => ToolkitSettings.HasInternalErrors;
		public static bool IsSetupDone => ToolkitInfo.IsSetupDone;

		private static AudioListener listener;

		#endregion

		#region Global Variables

		internal bool Awaken;

		#endregion

		#endregion

		#region Methods

		#region Static Methods

		public static GameObject GetOrCreateGameController()
		{
			if (HasInternalErrors)
				return null;

			string gameControllerName = $"_{Settings.gameControllerTag}";
			GameObject controller = GameObject.Find(gameControllerName);

			if (!controller)
				controller = GameObject.FindGameObjectWithTag(Settings.gameControllerTag);

			if (!controller)
				controller = new(gameControllerName);

			controller.tag = Settings.gameControllerTag;

			return controller;
		}

		#endregion

		#region Global Methods

		private void Awake()
		{
			if (listener)
				return;

			listener = FindAnyObjectByType<AudioListener>();
		}

		#endregion

		#endregion
	}
}

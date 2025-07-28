#region Namespaces

using System;
using System.Linq;
using UnityEngine;
using Utilities;
using MVC.Utilities.Internal;

using Object = UnityEngine.Object;

#endregion

namespace MVC.AI
{
	[Serializable]
	public class VehicleAILayer : ToolkitComponent
	{
		#region Enumerators

		#endregion

		#region Variables

		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				if (value.IsNullOrEmpty() || value.IsNullOrWhiteSpace())
					value = "New Layer";

				string prefix = default;

				if (Settings && Settings.AILayers != null && Settings.AILayers.Length > 0)
					for (int i = 0; Array.Find(Settings.AILayers, layer => layer != this && layer.Name.ToUpper() == $"{value.ToUpper()}{prefix}"); i++)
						prefix = i > 0 ? $" ({i})" : "";

				name = $"{value}{prefix}";
			}
		}

		[SerializeField]
		private string name;

		#endregion

		#region Methods

		public static VehicleAILayer GetLayerFromName(string name)
		{
			int index = GetLayerIndexFromName(name);

			if (index < 0)
				return null;

			return Settings.AILayers[index];
		}
		public static int GetLayerIndexFromName(string name)
		{
			var layers = Settings.AILayers;

			if (layers == null || layers.Length < 1)
			{
				ToolkitDebug.Error("We have some internal errors that need to be fixed! Head to the Settings Panel for more information.");

				return -1;
			}

			if (name.IsNullOrEmpty() || name.IsNullOrWhiteSpace())
				throw new ArgumentNullException(nameof(name), $"The argument `{nameof(name)}` cannot empty or null.");

			int index = Array.FindIndex(layers, l => l.Name == name);

			if (index < 0)
				ToolkitDebug.Warning($"We couldn't find an AI layer with the name of \"{name}\"");

			return index;
		}
		public static VehicleAILayer GetLayerFromIndex(int index)
		{
			var layers = Settings.AILayers;
			int layersCount = layers.Length;

			if (layers == null || layersCount < 1)
			{
				ToolkitDebug.Error("We have some internal errors that need to be fixed! Head to the Settings Panel for more information.");

				return null;
			}

			if (index < 0 || index >= layersCount)
				throw new IndexOutOfRangeException("Cannot get layer as `index` is out range");

			return layers[index];
		}
		public static VehicleAIBehaviour[] FindVehiclesWithLayer(string layer)
		{
			if (GetLayerIndexFromName(layer) < 0)
				return null;

			return Object.FindObjectsByType<VehicleAIBehaviour>(FindObjectsSortMode.None).Where(vehicle => vehicle.AILayerName == layer).ToArray();
		}

		#endregion

		#region Constructors

		public VehicleAILayer(string name)
		{
			Name = name;
		}
		public VehicleAILayer(VehicleAILayer layer)
		{
			Name = layer.Name;
		}

		#endregion
	}
}

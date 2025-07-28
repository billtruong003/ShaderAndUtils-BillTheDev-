#region Namespaces

using System;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
using MVC.Core;
using MVC.Utilities.Internal;
using System.Collections.Generic;


#endregion

namespace MVC.AI
{
	[AddComponentMenu("")]
	[RequireComponent(typeof(Vehicle))]
	[DefaultExecutionOrder(-60)]
	public abstract class VehicleAIBehaviour : ToolkitBehaviourExtension<Vehicle>
	{
		#region Enumerators

		public enum UpdateType { NormalUpdate, FixedUpdate, LateUpdate }
		public enum UpdateMethod { WhenActive, Always }

		#endregion

		#region Variables

		#region Properties

		public virtual VehicleAILayer AILayer
		{
			get
			{
				return Settings.AILayers[AILayerIndex];
			}
		}
		public virtual int AILayerIndex
		{
			get
			{
				return !AILayerName.IsNullOrEmpty() ? aiLayerIndex : default;
			}
		}
		public virtual string AILayerName
		{
			get
			{
				var layers = Settings.AILayers;
				int layersCount = layers.Length;

				if (layers != null && layersCount > 0)
				{
					if (aiLayerName.IsNullOrEmpty() || aiLayerIndex < 0 || aiLayerIndex >= layersCount)
					{
						aiLayerIndex = math.clamp(aiLayerIndex, 0, layersCount - 1);
						aiLayerName = layers[aiLayerIndex].Name;
					}
					else if (layers[aiLayerIndex].Name != aiLayerName)
					{
						int newIndex = Array.IndexOf(layers, aiLayerName);

						if (newIndex > -1)
							aiLayerIndex = newIndex;
						else
						{
							ToolkitDebug.Log($"{name}: An AI layer named \"{aiLayerName}\" has been removed or changed, therefore the a new AI layer called \"{layers[aiLayerIndex].Name}\" has been assigned.", gameObject);

							aiLayerName = layers[aiLayerIndex].Name;
						}
					}
				}

				return aiLayerName;
			}
			set
			{
				if (value.IsNullOrEmpty() || value.IsNullOrWhiteSpace())
				{
					ToolkitDebug.Error($"Cannot set AI Layer to a null or an empty value.");

					return;
				}

				int layerIndex = VehicleAILayer.GetLayerIndexFromName(value);

				if (layerIndex < 0)
					ToolkitDebug.Warning("AI Layer has been set to default");

				var layers = Settings.AILayers;

				if (aiLayerName.IsNullOrEmpty() || layerIndex < 0)
					if (layers != null && layers.Length > 0)
						aiLayerName = layers.FirstOrDefault().Name;

				aiLayerName = value;
				aiLayerIndex = layerIndex;
			}
		}
		public virtual string[] Errors { get; }
		public virtual string[] Warnings
		{
			get
			{
				List<string> messages = new();
				Vehicle vehicleInstance = Base;

				if (!vehicleInstance)
					messages.Add("No Vehicle component found attached to this GameObject.");
				else if (vehicleInstance.IsTrailer)
					messages.Add("An AI behaviour cannot be attached to any Trailer instances.");

				return messages.ToArray();
			}
		}
		public virtual string[] Issues { get; }

		#endregion

		#region Fields

		[Header("Behaviour Parameters")]
		public UpdateType updateType;
		public UpdateMethod updateMethod;

		[SerializeField]
		private int aiLayerIndex = -1;
		[SerializeField, HideInInspector]
		private string aiLayerName;

		#endregion

		#endregion

		#region Methods

		public virtual bool Restart()
		{
#if MVC_COMMUNITY
			return false;
#else
			Awaken = false;

			Vehicle vehicle = Base;

			if (!vehicle || !vehicle.isActiveAndEnabled || Settings.Problems.DisableToolkitBehaviourOnProblems(ToolkitSettings.SettingsEditorFoldout.AI, this))
				return false;

			Awaken = true;

			return true;
#endif
		}
		public virtual void OnStart() { }
		public abstract void OnUpdate(ref Vehicle.InputsAccess inputs);
		public virtual void OnActivation() { }
		public virtual void OnDeactivation() { }
		private void OnDestroy()
		{
			Base.RefreshAIBehaviours();
		}

		#endregion
	}
}

#region Namespaces

using MVC.Core;
using System;
using System.Globalization;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Utilities;

#endregion

namespace MVC.Base
{
	[Serializable]
	public class VehicleTireCompound : ToolkitComponent
	{
		#region Enumerators

		public enum FrictionComplexity { Simple, Complex }

		#endregion

		#region Modules

		[Serializable]
		public struct WidthFrictionModifier
		{
			#region Variables

			[Min(0f)]
			public int width;
			[Min(0f)]
			public float frictionMultiplier;

			#endregion

			#region Constructors

			public WidthFrictionModifier(int width, float frictionMultiplier)
			{
				this.width = width;
				this.frictionMultiplier = frictionMultiplier;
			}

			#endregion
		}
		[Serializable]
		public struct WheelColliderFrictionCurve
		{
			#region Variables

			public float extremumSlip;
			public float extremumValue;
			public float asymptoteSlip;
			public float asymptoteValue;
			public FrictionComplexity stiffnessComplexity;
			public float stiffness;
			public Utility.SimpleInterval stiffnessInterval;
			public Utility.SimpleInterval stiffnessSpeedInterval;

			#endregion

			#region Utilities

			public readonly float GetStiffness(float speed)
			{
				return stiffnessComplexity switch
				{
					FrictionComplexity.Complex => stiffnessInterval.LerpUnclamped(stiffnessSpeedInterval.InverseLerp(speed)),
					_ => stiffness,
				};
			}

			#endregion
		}

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
					value = "New Camera";

				string prefix = default;

				if (Settings && Settings.TireCompounds != null && Settings.TireCompounds.Length > 0)
					for (int i = 0; Array.Find(Settings.TireCompounds, camera => camera != this && camera.Name.ToUpper() == $"{value.ToUpper()}{prefix}"); i++)
						prefix = i > 0 ? $" ({i})" : "";

				name = $"{value}{prefix}";
			}
		}
		public FrictionComplexity frictionComplexity;
		public WheelColliderFrictionCurve wheelColliderAccelerationFriction = new()
		{
			extremumSlip = .2f,
			extremumValue = 1f,
			asymptoteSlip = .8f,
			asymptoteValue = .75f,
			stiffness = 1f,
			stiffnessInterval = new(1f, .9f),
			stiffnessSpeedInterval = new(18f, 54f),
		};
		public WheelColliderFrictionCurve wheelColliderBrakeFriction = new()
		{
			extremumSlip = .2f,
			extremumValue = 1f,
			asymptoteSlip = .8f,
			asymptoteValue = .75f,
			stiffness = 1f,
			stiffnessInterval = new(1f, .9f),
			stiffnessSpeedInterval = new(18f, 54f),
		};
		public WheelColliderFrictionCurve wheelColliderSidewaysFriction = new()
		{
			extremumSlip = .25f,
			extremumValue = 1f,
			asymptoteSlip = .5f,
			asymptoteValue = .75f,
			stiffness = 1f,
			stiffnessInterval = new(1f, .9f),
			stiffnessSpeedInterval = new(18f, 54f),
		};
		public WidthFrictionModifier[] widthFrictionModifiers = new WidthFrictionModifier[]
		{
			new(150, .8625f),
			new(400, 1.03125f),
			new(650, 1.3476f),
		};
		public float wetFrictionMultiplier = .955f;
		public float wetSlipMultiplier = 1.15f;
		public bool IsValid
		{
			get
			{
				if (!isValid || !Application.isPlaying)
					RefreshValidity();

				return isValid;
			}
		}

		[SerializeField]
		private string name;
		private bool isValid;

		#endregion

		#region Utilities

		public float GetStiffness(float frictionCurveStiffness, float width, float wetness)
		{
			var firstWidthModifier = widthFrictionModifiers.FirstOrDefault();
			var lastWidthModifier = widthFrictionModifiers.LastOrDefault();
			bool widthSmallerThanRange = width < firstWidthModifier.width;
			bool widthGreaterThanRange = width > lastWidthModifier.width;
			float widthStiffness = (widthSmallerThanRange ? firstWidthModifier : lastWidthModifier).frictionMultiplier;
			float wetnessStiffness = Utility.Lerp(1f, wetFrictionMultiplier, wetness);

			if (!widthSmallerThanRange && !widthGreaterThanRange)
				for (int i = 0; i < widthFrictionModifiers.Length - 1; i++)
				{
					var currentModifier = widthFrictionModifiers[i];
					var nextModifier = widthFrictionModifiers[i + 1];

					if (width >= currentModifier.width && width <= nextModifier.width)
					{
						widthStiffness = Utility.LerpUnclamped(currentModifier.frictionMultiplier, nextModifier.frictionMultiplier, Utility.InverseLerp(currentModifier.width, nextModifier.width, width));

						break;
					}
				}

			return frictionCurveStiffness * widthStiffness * wetnessStiffness;
		}

		private void RefreshValidity()
		{
			bool isSortValid = widthFrictionModifiers != null && widthFrictionModifiers.Length > 1;

			if (isSortValid)
			{
				var sortedWidthFrictionModifiers = widthFrictionModifiers.ToArray();

				Array.Sort(sortedWidthFrictionModifiers.Select(modifier => modifier.width).ToArray(), sortedWidthFrictionModifiers);

				for (int i = 0; i < widthFrictionModifiers.Length && isSortValid; i++)
					isSortValid &= widthFrictionModifiers[i].width == sortedWidthFrictionModifiers[i].width;
			}

			isValid = wetFrictionMultiplier >= 0f && wetSlipMultiplier >= 0f && isSortValid;
		}

		#endregion

		#region Constructors

		public VehicleTireCompound(string name)
		{
			Name = name;
		}
		public VehicleTireCompound(VehicleTireCompound tireCompound)
		{
			Name = tireCompound.name;
			wheelColliderAccelerationFriction = tireCompound.wheelColliderAccelerationFriction;
			wheelColliderSidewaysFriction = tireCompound.wheelColliderSidewaysFriction;
			widthFrictionModifiers = tireCompound.widthFrictionModifiers;
			wetFrictionMultiplier = tireCompound.wetFrictionMultiplier;
			wetSlipMultiplier = tireCompound.wetSlipMultiplier;
		}

		#endregion
	}
}

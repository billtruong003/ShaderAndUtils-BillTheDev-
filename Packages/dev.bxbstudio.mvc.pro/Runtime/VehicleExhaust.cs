#region Namespaces

using System;
using Unity.Mathematics;
using UnityEngine;
using MVC.Core;

#endregion

namespace MVC.Base
{
	[Serializable]
	public class VehicleExhaust : ToolkitComponent
	{
		#region Variables

		public Vector3 localPosition = new(0f, .25f, -2f);
		public Vector3 localEulerAngles;
		public Vector2 LocalScale
		{
			get
			{
				return localScale;
			}
			set
			{
				localScale = math.max(value, 0f);
			}
		}
		[NonSerialized]
		public bool editorFoldout;

		[SerializeField]
		private readonly Vehicle vehicle;
		[SerializeField]
		private Vector2 localScale = Vector2.one;

		#endregion

		#region Constructors

		public VehicleExhaust(Vehicle vehicle)
		{
			this.vehicle = vehicle;
		}
		public VehicleExhaust(VehicleExhaust exhaust)
		{
			vehicle = exhaust.vehicle;
			localPosition = exhaust.localPosition;
			localEulerAngles = exhaust.localEulerAngles;
			localScale = exhaust.localScale;
		}

		#endregion

		#region Operators

		public static implicit operator bool(VehicleExhaust exhaust) => exhaust != null;

		#endregion
	}
}

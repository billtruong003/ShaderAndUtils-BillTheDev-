#region Namespaces

using System.Runtime.InteropServices;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
using MVC.Core;
using System;

#endregion

namespace MVC.Base
{
	[AddComponentMenu("")]
	[DisallowMultipleComponent]
	public class VehicleFollowerPivot : ToolkitBehaviour
	{
		public Vehicle VehicleInstance
		{
			get
			{
				if (!vehicle)
					vehicle = GetComponentInParent<Vehicle>();

				return vehicle;
			}
		}
		public int CameraIndex
		{
			get
			{
				if (HasInternalErrors)
					return -1;

				var cameras = Settings.Cameras;
				var camerasCount = cameras.Length;

				if (cameraIndex < -1 || cameraIndex >= camerasCount)
				{
					cameraIndex = math.clamp(cameraIndex, -1, camerasCount - 1);
					cameraName = cameraIndex > -1 ? cameras[cameraIndex].Name : string.Empty;
				}
				else if (cameraIndex > -1 && cameras[cameraIndex].Name != cameraName)
				{
					if (cameraName.IsNullOrEmpty())
						cameraName = cameras[cameraIndex].Name;
					else
					{
						cameraIndex = Array.FindIndex(cameras, camera => camera.Name == cameraName);

						if (cameraIndex < 0)
							cameraName = string.Empty;
					}
				}

				return cameraIndex;
			}
			set
			{
				if (HasInternalErrors)
					return;

				var cameras = Settings.Cameras;
				var camerasCount = cameras.Length;

				cameraIndex = math.clamp(value, -1, camerasCount - 1);
				cameraName = cameraIndex > -1 ? cameras[cameraIndex].Name : string.Empty;
			}
		}
		public bool IsInsideVehicle
		{
			get
			{
				if (CameraIndex < 0)
					return false;

				return Settings.Cameras[CameraIndex].PivotPosition == VehicleCamera.PivotPositionType.Inside;
			}
		}

		private Vehicle vehicle;
		[SerializeField]
		private string cameraName;
		[SerializeField]
		private int cameraIndex;
	}

	internal readonly struct VehicleFollowerPivotAccess
	{
		#region Variables

		public readonly Utility.TransformAccess transform;
		[MarshalAs(UnmanagedType.U1)]
		public readonly bool isInsideVehicle;
		public readonly int cameraIndex;

		#endregion

		#region Constructors

		public VehicleFollowerPivotAccess(VehicleFollowerPivot pivot) : this()
		{
			if (!pivot)
				return;

			transform = pivot.transform;
			isInsideVehicle = pivot.IsInsideVehicle;
			cameraIndex = pivot.CameraIndex;
		}

		#endregion

		#region Operators

		public static implicit operator VehicleFollowerPivotAccess(VehicleFollowerPivot pivot) => new(pivot);

		#endregion
	}
}

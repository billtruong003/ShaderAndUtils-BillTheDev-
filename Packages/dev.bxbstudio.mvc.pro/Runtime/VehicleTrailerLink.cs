#region Namespaces

using Unity.Mathematics;
using UnityEngine;
using Utilities;
using MVC.Core;

#endregion

namespace MVC.Base
{
	[AddComponentMenu("")]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(Vehicle))]
	public class VehicleTrailerLink : ToolkitBehaviourExtension<Vehicle>
	{
		#region Variables

		public Vehicle VehicleInstance => Base;
		public VehicleTrailer ConnectedTrailer { get; internal set; }
		public Vector3 LinkPoint
		{
			get
			{
				return linkPoint;
			}
			set
			{
				linkPoint = value;

				SphereCollider detector = GetTrailerDetector();

				if (detector)
					detector.transform.localPosition = linkPoint;
			}
		}
		public float LinkRadius
		{
			get
			{
				return linkRadius;
			}
			set
			{
				linkRadius = math.max(value, 0f);

				SphereCollider detector = GetTrailerDetector();

				if (detector)
					detector.radius = linkRadius;
			}
		}

		[SerializeField]
		private Vector3 linkPoint;
		[SerializeField]
		private float linkRadius = .25f;

		#endregion

		#region Methods

		#region Awake

		public void Restart()
		{
			Awaken = false;

			Initialize();

			if (HasInternalErrors || !IsSetupDone || !VehicleInstance || !VehicleInstance.isActiveAndEnabled)
				return;

			AddTrailerDetector();

			Awaken = true;
		}

		private void Initialize()
		{
			RemoveTrailerDetector();
		}

		#endregion

		#region Utilities

		protected internal SphereCollider AddTrailerDetector()
		{
			if (!VehicleInstance || !VehicleInstance.Chassis || GetTrailerDetector())
				return null;

			Transform detectorTransform = new GameObject("_TrailerDetector").transform;

			detectorTransform.parent = VehicleInstance.Chassis.transform;

			detectorTransform.SetLocalPositionAndRotation(LinkPoint, Quaternion.identity);

			SphereCollider detector = detectorTransform.gameObject.AddComponent<SphereCollider>();

			detectorTransform.gameObject.layer = Settings.vehiclesLayer;

			if (Settings.useHideFlags)
				detectorTransform.gameObject.hideFlags = HideFlags.HideInHierarchy;

			detector.radius = LinkRadius;
			detector.isTrigger = true;

			return detector;
		}
		protected internal SphereCollider GetTrailerDetector()
		{
			if (!VehicleInstance || !VehicleInstance.Chassis)
				return null;

			Transform detectorTransform = VehicleInstance.Chassis.transform.Find("_TrailerDetector");
			SphereCollider detector = detectorTransform ? detectorTransform.GetComponent<SphereCollider>() : null;

			if (detector)
			{
				if (Settings.useHideFlags)
					detectorTransform.gameObject.hideFlags = HideFlags.HideInHierarchy;

				detector.isTrigger = true;
			}

			return detector;
		}
		protected internal void RemoveTrailerDetector()
		{
			SphereCollider detector = GetTrailerDetector();

			if (!detector)
				return;

			Utility.Destroy(true, detector.gameObject);
		}

		#endregion

		#endregion
	}
}

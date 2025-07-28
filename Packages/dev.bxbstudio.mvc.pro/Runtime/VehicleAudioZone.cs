#region Namespaces

using Unity.Mathematics;
using UnityEngine;
using Utilities;

#endregion

namespace MVC.Base
{
	[AddComponentMenu("")]
	[DefaultExecutionOrder(20)]
	public class VehicleAudioZone : VehicleZone
	{
		#region Enumerators

		public enum AudioZoneType { Reverb }

		#endregion

		#region Variables

		public AudioZoneType zoneType;
		public AudioReverbZone ReverbZone
		{
			get
			{
				if (zoneType == AudioZoneType.Reverb)
				{
					if (!reverbZone)
					{
						reverbZone = GetComponent<AudioReverbZone>();

						if (!reverbZone)
							reverbZone = gameObject.AddComponent<AudioReverbZone>();
					}
				}
				else if (reverbZone)
					Utility.Destroy(true, reverbZone);

				return reverbZone;
			}
		}
		public float MaxZoneDistanceMultiplier
		{
			get
			{
				return maxZoneDistanceMultiplier;
			}
			set
			{
				maxZoneDistanceMultiplier = math.max(value, 1f);

				RefreshZoneSize();
			}
		}

		[SerializeField]
		private AudioReverbZone reverbZone;
		[SerializeField]
		private float maxZoneDistanceMultiplier = 1.25f;

		#endregion

		#region Methods

		#region Virtual Methods

		public override void OnDrawGizmosSelected()
		{
			Gizmos.color = Settings.audioReverbZoneGizmoColor;

			base.OnDrawGizmosSelected();

			Gizmos.color = Color.white;
		}

		#endregion

		#region Global Methods

		#region Awake

		public void Restart()
		{
			Awaken = false;

			if (HasInternalErrors || !IsSetupDone || !Settings || !Listener)
				return;

			RefreshZoneSize();

			Awaken = true;
		}

		private void Awake()
		{
			if (Awaken)
				return;

			Restart();
		}

		#endregion

		#region Utilities

		public void RefreshZoneSize()
		{
			if (!ReverbZone)
				return;


			if (Application.isPlaying && Shape != ZoneShape.Sphere)
			{
				Vector3 listenerPosition = Listener.transform.position;
				Vector3 closestPoint = BoxCollider.ClosestPoint(listenerPosition);
				float minimumDistance = Utility.Distance(reverbZone.transform.position, closestPoint);

				// TODO: Fix this
				/*// Check if listener is inside the box
				if (closestPoint == listenerPosition)
				{
					Vector3 listenerLocalPosition = transform.InverseTransformPoint(listenerPosition);
					Vector3 localScale = transform.InverseTransformVector(transform.lossyScale * .5f);
					Vector3 min = -localScale;
					Vector3 max = localScale;
					// Compute distances to each face
					float dXMin = listenerLocalPosition.x - min.x;
					float dXMax = max.x - listenerLocalPosition.x;
					float dYMin = listenerLocalPosition.y - min.y;
					float dYMax = max.y - listenerLocalPosition.y;
					float dZMin = listenerLocalPosition.z - min.z;
					float dZMax = max.z - listenerLocalPosition.z;
					// Initialize the result as a copy of listener position
					Vector3 result = listenerLocalPosition;
					// Find the smallest distance and update the corresponding coordinate
					float dMin = dXMin;
					string face = "x_min";

					if (dXMax < dMin)
					{
						dMin = dXMax;
						face = "x_max";
					}

					if (dYMin < dMin)
					{
						dMin = dYMin;
						face = "y_min";
					}

					if (dYMax < dMin)
					{
						dMin = dYMax;
						face = "y_max";
					}

					if (dZMin < dMin)
					{
						dMin = dZMin;
						face = "z_min";
					}

					if (dZMax < dMin)
						face = "z_max";

					// Set result to lie on the chosen face
					if (face == "x_min")
						result.x = min.x;
					else if (face == "x_max")
						result.x = max.x;
					else if (face == "y_min")
						result.y = min.y;
					else if (face == "y_max")
						result.y = max.y;
					else if (face == "z_min")
						result.z = min.z;
					else if (face == "z_max")
						result.z = max.z;

					closestPoint = transform.TransformPoint(result);
					minimumDistance = math.max(Utility.Distance(reverbZone.transform.position, listenerPosition), Utility.Distance(reverbZone.transform.position, closestPoint));
				}*/

				reverbZone.maxDistance = minimumDistance * maxZoneDistanceMultiplier;
				reverbZone.minDistance = minimumDistance;
			}
			else
			{
				Vector3 lossyScale = transform.lossyScale;
				float minDistance = Shape == ZoneShape.Sphere ? SphereRadius : Utility.Max(lossyScale.x, lossyScale.y, lossyScale.z) * .5f;

				reverbZone.maxDistance = maxZoneDistanceMultiplier * minDistance;
				reverbZone.minDistance = minDistance;
			}
		}

		#endregion

		#region Late Update

		private void LateUpdate()
		{
			if (!Awaken)
				return;

			RefreshZoneSize();
		}

		#endregion

		#endregion

		#endregion
	}
}

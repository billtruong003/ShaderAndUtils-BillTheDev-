#region Namespaces

using System.Linq;
using UnityEngine;
using Utilities;
using MVC.AI;

#endregion

namespace MVC.Base
{
	[AddComponentMenu("")]
	[DisallowMultipleComponent]
	public class VehicleZone : ToolkitBehaviour
	{
		#region Enumerators

		public enum ZoneShape { Box, Sphere }
		public enum ZoneType { Unknown, AI, Audio, Damage, Weather }

		#endregion

		#region Variables

		public VehicleZonesContainer ContainerInstance
		{
			get
			{
				if (!containerInstance)
					containerInstance = GetComponentInParent<VehicleZonesContainer>();

				return containerInstance;
			}
		}
		public BoxCollider BoxCollider
		{
			get
			{
				if (!boxCollider && collider is BoxCollider)
					boxCollider = collider as BoxCollider;

				if (boxCollider && boxCollider.size != Vector3.one)
				{
					transform.localScale = Utility.Multiply(boxCollider.size, transform.localScale);
					boxCollider.size = Vector3.one;
				}

				return boxCollider;
			}
		}
		public SphereCollider SphereCollider
		{
			get
			{
				if (!sphereCollider && collider is SphereCollider)
					sphereCollider = collider as SphereCollider;

				if (sphereCollider && sphereCollider.radius != 1f)
				{
					transform.localScale *= sphereCollider.radius;
					sphereCollider.radius = 1f;
				}

				return sphereCollider;
			}
		}
		public ZoneShape Shape
		{
			get
			{
				if (!collider || !collider.transform.IsChildOf(transform))
					RecreateCollider();

				return shape;
			}
			set
			{
				bool shapeChanged = shape != value;

				shape = value;

				if (shapeChanged)
					RecreateCollider();
			}
		}
		public ZoneType Type
		{
			get
			{
				if (type == default)
					type = GetType().Name switch
					{
						"VehicleAIZone" => ZoneType.AI,
						"VehicleAudioZone" => ZoneType.Audio,
						"VehicleDamageZone" => ZoneType.Damage,
						"VehicleWeatherZone" => ZoneType.Weather,
						_ => ZoneType.Unknown
					};

				return type;
			}
		}
		public float SphereRadius
		{
			get
			{
				if (!SphereCollider)
					return default;

				return Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);
			}
		}

#if UNITY_EDITOR
		internal new Collider collider;
#else
		internal Collider collider;
#endif

		private VehicleZonesContainer containerInstance;
		private BoxCollider boxCollider;
		private SphereCollider sphereCollider;
		[SerializeField]
		private ZoneShape shape;
		private ZoneType type;

		#endregion

		#region Methods

		#region Utilities

		private void RecreateCollider()
		{
			if (!collider)
				collider = GetComponent<Collider>();

			if (collider)
				Utility.Destroy(true, collider);

			switch (shape)
			{
				case ZoneShape.Box:
					collider = gameObject.AddComponent<BoxCollider>();

					break;

				case ZoneShape.Sphere:
					collider = gameObject.AddComponent<SphereCollider>();

					break;
			}

			if (Settings.useHideFlags)
				collider.hideFlags = HideFlags.HideInInspector;

			collider.isTrigger = true;
		}

		#endregion

		#region Enable & Destroy

		private void OnEnable()
		{
			if (!collider)
				RecreateCollider();
		}
		private void OnDestroy()
		{
			if (collider)
				Utility.Destroy(false, collider);
		}

		#endregion

		#region Gizmos

		public virtual void OnDrawGizmosSelected()
		{
			switch (Shape)
			{
				case ZoneShape.Box:
					Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Utility.Multiply(transform.lossyScale, BoxCollider.size));

					Gizmos.DrawCube(Vector3.zero, Vector3.one);

					break;

				case ZoneShape.Sphere:
					Gizmos.DrawSphere(transform.position, Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z) * SphereCollider.radius);
					
					break;
			}
		}

		#endregion

		#endregion
	}
}

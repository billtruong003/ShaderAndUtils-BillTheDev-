#region Namespaces

using System;
using UnityEngine;
using Unity.Mathematics;
using Utilities;
using MVC.Core;

#endregion

namespace MVC.VFX
{
	[AddComponentMenu("")]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(-10)]
	public class VehicleWheelMark : ToolkitBehaviour
	{
		#region Modules

		private struct MarkAnchor
		{
			public Vector4 tangent;
			public float3 position;
			public float3 normal;
			public float3 left;
			public float3 right;
			public float opacity;
		}

		#endregion

		#region Variables

		public float MarkLength
		{
			get
			{
				return markLength;
			}
		}
		public int GroundIndex
		{
			get
			{
				return groundIndex;
			}
		}
		public VehicleGroundMapper.GroundModule Ground
		{
			get
			{
				if (GroundIndex < 0 || GroundIndex >= Settings.Grounds.Length)
					return default;

				return Settings.Grounds[groundIndex];
			}
		}
		public int AnchorsCount
		{
			get
			{
				return markAnchors != null ? markAnchors.Length : default;
			}
		}

		internal MeshFilter filter;
#if UNITY_EDITOR
		internal new MeshRenderer renderer;
#else
		internal MeshRenderer renderer;
#endif

		private MarkAnchor[] markAnchors;
		private float uvTiling;
		private float markLength;
		private int groundIndex = -1;

		#endregion

		#region Methods

		#region Static Methods

		public static VehicleWheelMark CreateNew(VehicleWheel wheel, float slip)
		{
			if (HasInternalErrors || !wheel || !wheel.VehicleInstance)
				return null;

			bool useNormalMark = wheel.TireHealth > 0f || !Settings.useDamage || !Settings.useWheelHealth;
			VehicleGroundMapper.GroundModule ground = Settings.Grounds[wheel.CurrentGroundIndex];
			Material markMaterial = useNormalMark ? ground.markMaterial : ground.flatWheelMarkMaterial;

			if (!markMaterial)
				return null;

			Transform markTransform = new GameObject("Mark").transform;

			if (Settings.useHideFlags)
				markTransform.hideFlags = HideFlags.HideInHierarchy;

			markTransform.SetParent(wheel.GroundMapper.transform, true);

			VehicleWheelMark mark = markTransform.gameObject.AddComponent<VehicleWheelMark>();

			mark.groundIndex = wheel.CurrentGroundIndex;
			mark.uvTiling = useNormalMark ? ground.MarkMaterialTiling : ground.FlatWheelMarkMaterialTiling;
			mark.filter = mark.gameObject.AddComponent<MeshFilter>();
			mark.markAnchors = new MarkAnchor[] { };
			mark.renderer = mark.gameObject.AddComponent<MeshRenderer>();
			mark.renderer.sharedMaterial = markMaterial;
			mark.renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
			mark.renderer.allowOcclusionWhenDynamic = true;

			mark.AddMarkAnchor(wheel, slip);

			return mark;
		}

		#endregion

		#region Global Methods

		#region Utilities

		public bool UpdateMark(VehicleWheel wheel, float opacity)
		{
			if (HasInternalErrors || markAnchors == null || markAnchors.Length < 1 || !wheel || !wheel.VehicleInstance || wheel.CurrentGroundIndex != groundIndex)
				return false;

			opacity = Utility.Clamp01(opacity);

			int updateStartIndex = markAnchors.Length - 1;

			if (!AddMarkAnchor(wheel, opacity))
				return false;

			UpdateMesh(wheel, updateStartIndex);

			return true;
		}

		private bool AddMarkAnchor(VehicleWheel wheel, float opacity)
		{
			if (!filter || !renderer || !renderer.sharedMaterial)
				return false;

			bool isGrounded = wheel.LateHitInfo.point != default;
			
			if (isGrounded)
			{
				float3 normal = wheel.LateHitInfo.normal;
				float3 position = (float3)wheel.LateHitInfo.point + normal * Settings.wheelMarkGroundOffset;

				if (opacity > 0f && markAnchors.Length > 1 && Utility.DistanceSqr(markAnchors[^1].position, position) < .01f)
					return true;

				float3 forward = Utility.Lerp((float3)wheel.LateHitInfo.forwardDir, math.normalize(wheel.transform.TransformDirection(wheel.LateLocalVelocity)), math.length(wheel.LateLocalVelocity));
				float3 right1 = Utility.DirectionRight(forward, normal);
				float3 right2 = Mathf.Sign(wheel.LateLocalVelocity.z) * (math.min(Vector3.Angle((Mathf.Sign(wheel.LateLocalVelocity.z) * wheel.LateHitInfo.forwardDir).normalized, wheel.VehicleInstance.Rigidbody.
#if UNITY_6000_0_OR_NEWER
					linearVelocity
#else
					velocity
#endif
					.normalized) / 90f, 1f) + 1f) * wheel.LateHitInfo.sidewaysDir;
				float3 right = wheel.Width * .5f * Utility.Lerp(right1, Utility.Average(right1, right1, right2), math.abs(math.normalize(wheel.LateLocalVelocity).x));
				Vector4 tangent = new((position - right).x, (position - right).y, (position - right).z, 1f);

				Array.Resize(ref markAnchors, markAnchors.Length + 1);

				markAnchors[^1] = new()
				{
					tangent = tangent,
					position = position,
					normal = normal,
					opacity = opacity,
					left = position - right,
					right = position + right
				};
			}
			else if (markAnchors.Length > 0)
			{
				markAnchors[^1].opacity = default;

				return false;
			}

			return true;
		}
		private void UpdateMesh(VehicleWheel wheel, int updateStartIndex = 1)
		{
			if (!wheel || !wheel.VehicleInstance || !filter || !renderer || !renderer.sharedMaterial)
				return;

			Vector4[] tangents;
			Vector3[] vertices;
			Vector3[] normals;
			Vector2[] uvs;
			Color[] colors;
			int[] triangles;

			if (filter.mesh)
			{
				updateStartIndex = math.clamp(updateStartIndex, 1, markAnchors.Length - 1);

				if (updateStartIndex < 2)
					filter.mesh.Clear();

				tangents = filter.mesh.tangents;
				vertices = filter.mesh.vertices;
				normals = filter.mesh.normals;
				uvs = filter.mesh.uv;
				colors = filter.mesh.colors;
				triangles = filter.mesh.triangles;

				Array.Resize(ref tangents, markAnchors.Length * 4);
				Array.Resize(ref vertices, markAnchors.Length * 4);
				Array.Resize(ref normals, markAnchors.Length * 4);
				Array.Resize(ref uvs, markAnchors.Length * 4);
				Array.Resize(ref colors, markAnchors.Length * 4);
				Array.Resize(ref triangles, markAnchors.Length * 6);
			}
			else
			{
				filter.mesh = new()
				{
					name = $"{wheel.VehicleInstance.name}_{wheel.name}_{wheel.vfx.groundMarks.Length}"
				};
				tangents = new Vector4[markAnchors.Length * 4];
				vertices = new Vector3[markAnchors.Length * 4];
				normals = new Vector3[markAnchors.Length * 4];
				uvs = new Vector2[markAnchors.Length * 4];
				colors = new Color[markAnchors.Length * 4];
				triangles = new int[markAnchors.Length * 6];
				updateStartIndex = 1;
			}

			for (int i = updateStartIndex; i < markAnchors.Length; i++)
			{
				MarkAnchor lastAnchor = markAnchors[i - 1];
				MarkAnchor currentAnchor = markAnchors[i];
				float distance = Utility.Distance(lastAnchor.position, currentAnchor.position) * uvTiling;

				triangles[i * 6 + 0] = i * 4 + 0;
				triangles[i * 6 + 2] = i * 4 + 1;
				triangles[i * 6 + 1] = i * 4 + 2;
				triangles[i * 6 + 3] = i * 4 + 2;
				triangles[i * 6 + 5] = i * 4 + 1;
				triangles[i * 6 + 4] = i * 4 + 3;
				vertices[i * 4 + 0] = lastAnchor.left;
				vertices[i * 4 + 1] = lastAnchor.right;
				vertices[i * 4 + 2] = currentAnchor.left;
				vertices[i * 4 + 3] = currentAnchor.right;
				tangents[i * 4 + 0] = lastAnchor.tangent;
				tangents[i * 4 + 1] = lastAnchor.tangent;
				tangents[i * 4 + 2] = currentAnchor.tangent;
				tangents[i * 4 + 3] = currentAnchor.tangent;
				normals[i * 4 + 0] = lastAnchor.normal;
				normals[i * 4 + 1] = lastAnchor.normal;
				normals[i * 4 + 2] = currentAnchor.normal;
				normals[i * 4 + 3] = currentAnchor.normal;
				colors[i * 4 + 0] = new(1f, 1f, 1f, lastAnchor.opacity);
				colors[i * 4 + 1] = new(1f, 1f, 1f, lastAnchor.opacity);
				colors[i * 4 + 2] = new(1f, 1f, 1f, currentAnchor.opacity);
				colors[i * 4 + 3] = new(1f, 1f, 1f, currentAnchor.opacity);
				uvs[i * 4 + 0] = new(0f, markLength);
				uvs[i * 4 + 1] = new(1f, markLength);
				uvs[i * 4 + 2] = new(0f, markLength + distance);
				uvs[i * 4 + 3] = new(1f, markLength + distance);

				markLength += distance;
			}

#if !MVC_DEBUG
			Mesh meshBackup = Instantiate(filter.mesh);

			try
			{
#endif
				filter.mesh.vertices = vertices;
				filter.mesh.normals = normals;
				filter.mesh.tangents = tangents;
				filter.mesh.triangles = triangles;
				filter.mesh.colors = colors;
				filter.mesh.uv = uvs;
#if !MVC_DEBUG
			}
			catch
			{
				filter.mesh = meshBackup;
			}
#endif
		}

		#endregion

		#region Start, Destroy & Gizmos

		private void OnDestroy()
		{
			if (transform && transform.gameObject)
				Utility.Destroy(false, transform.gameObject);
		}
		private void OnDrawGizmosSelected()
		{
			if (!IsSetupDone || HasInternalErrors || markAnchors == null || markAnchors.Length < 1)
				return;

			Color color = Settings.wheelMarkGizmoColor;

			for (int i = 1; i < markAnchors.Length; i++)
			{
				color.a = Utility.Average(markAnchors[i - 1].opacity, markAnchors[i].opacity);
				Gizmos.color = color;

				Gizmos.DrawLine(markAnchors[i - 1].position, markAnchors[i].position);
			}
		}

		#endregion

		#endregion

		#endregion
	}
}

#region Namespaces

using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;
using Utilities;
using MVC.Core;
using MVC.Internal;
using MVC.Utilities.Internal;
using UnityEngine.UIElements;
using MVC.Utilities;

#endregion

namespace MVC.VFX
{
	[AddComponentMenu("Multiversal Vehicle Controller/VFX/Wheel Spin Effect", 49)]
	[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(-10)]
	public class VehicleWheelSpin : ToolkitBehaviour
	{
		#region Modules

		[Serializable]
		public class SpinMaterialModule
		{
			#region Variables

			public Material RuntimeSpinMaterial
			{
				get
				{
					if (!runtimeSpinMaterial && SharedSpinMaterial)
						runtimeSpinMaterial = new(sharedSpinMaterial)
						{
							enableInstancing = SystemInfo.supportsInstancing
						};

					return runtimeSpinMaterial;
				}
			}
			public Material SharedSpinMaterial
			{
				get
				{
					return sharedSpinMaterial;
				}
				set
				{
					sharedSpinMaterial = value;
				}
			}
			public int MaterialIndex
			{
				get
				{
					return materialIndex;
				}
				set
				{
					if (!IsValid || !wheelSpin.MeshRenderer)
						return;

					materialIndex = Mathf.Clamp(value, 0, wheelSpin.meshRenderer.sharedMaterials.Length - 1);
				}
			}
			public bool IsValid
			{
				get
				{
					return wheelSpin;
				}
			}

			[SerializeField]
#pragma warning disable IDE0044 // Add readonly modifier
			private VehicleWheelSpin wheelSpin;
#pragma warning restore IDE0044 // Add readonly modifier
			[SerializeField]
			private int materialIndex;
			[SerializeField]
			private Material sharedSpinMaterial;
			private Material runtimeSpinMaterial;

			#endregion

			#region Methods

			public void ResetRuntimeSpinMaterial()
			{
				runtimeSpinMaterial = null;
			}

			#endregion

			#region Constructors

			public SpinMaterialModule(VehicleWheelSpin wheelSpin, int materialIndex)
			{
				this.wheelSpin = wheelSpin;
				MaterialIndex = materialIndex;
			}
			public SpinMaterialModule(VehicleWheelSpin wheelSpin, int materialIndex, Material sharedSpinMaterial) : this(wheelSpin, materialIndex)
			{
				SharedSpinMaterial = sharedSpinMaterial;
			}

			#endregion

			#region Operators

			public static implicit operator bool(SpinMaterialModule module)
			{
				return module != null;
			}

			#endregion
		}

		#endregion

		#region Variables

		#region Fields

		public SpinMaterialModule[] SpinMaterials
		{
			get
			{
				spinMaterials ??= new SpinMaterialModule[] { };

				return spinMaterials;
			}
		}
		public float BlurAlphaOffset
		{
			get
			{
				return blurAlphaOffset;
			}
			set
			{
				blurAlphaOffset = Mathf.Clamp(value, -1f, 1f);
			}
		}
		public float TotalBlurAlphaOffset
		{
			get
			{
				return Mathf.Clamp(blurAlphaOffset + Settings.WheelSpinAlphaOffset, -1f, 1f);
			}
		}
		public bool visibilityCulling = true;
		public bool flipSpinMesh;

		[SerializeField]
		private SpinMaterialModule[] spinMaterials;
		[SerializeField]
		private float blurAlphaOffset;

		#endregion

		#region Stats

		public Vehicle VehicleInstance
		{
			get
			{
				if (!vehicleInstance)
					vehicleInstance = GetComponentInParent<Vehicle>();

				return vehicleInstance;
			}
		}
		public VehicleWheel WheelInstance
		{
			get
			{
				if (!VehicleInstance)
					return null;

				if (!wheelInstance)
					for (int i = 0; i < vehicleInstance.Wheels.Length; i++)
					{
						if (!vehicleInstance.Wheels[i].Model || !vehicleInstance.Wheels[i].Instance)
							continue;

						if (transform.IsChildOf(vehicleInstance.Wheels[i].Model))
						{
							wheelInstance = vehicleInstance.Wheels[i].Instance;

							break;
						}
					}

				return wheelInstance;
			}
		}
		public MeshFilter MeshFilter
		{
			get
			{
				if (!meshFilter)
					meshFilter = GetComponent<MeshFilter>();

				return meshFilter;
			}
		}
		public MeshRenderer MeshRenderer
		{
			get
			{
				if (!meshRenderer)
					meshRenderer = GetComponent<MeshRenderer>();

				return meshRenderer;
			}
		}
		public bool IsValid
		{
			get
			{
				return WheelInstance && MeshFilter && MeshRenderer;
			}
		}

		private Vehicle vehicleInstance;
		private VehicleWheel wheelInstance;
		private MeshFilter meshFilter;
		private MeshRenderer meshRenderer;

		#endregion

		#region Temp

#if !MVC_COMMUNITY
		private Camera[] cameras;
		private Mesh spinMesh;
		private Color spinColor;
		private float lastAlpha;
		private bool visible;
#endif

		#endregion

		#endregion

		#region Methods

		#region Awake

		public void Restart()
		{
#if !MVC_COMMUNITY
			Awaken = false;

			Initialize();

			if (!Settings.useWheelSpinEffect || !WheelInstance || !MeshFilter || !MeshRenderer || SpinMaterials.Length < 1 || Settings.Problems.DisableToolkitBehaviourOnProblems(ToolkitSettings.SettingsEditorFoldout.VFX, this))
				return;
#if UNITY_EDITOR
			else if (Application.isEditor && !(spinMesh = meshFilter.sharedMesh).isReadable)
				ToolkitDebug.Warning($"The mesh \"{spinMesh.name}\" is not set as Readable. This might cause errors when building the game player. Make sure to change the import settings of your mesh before building.", spinMesh);
#endif

			cameras = FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			spinMesh = Instantiate(meshFilter.sharedMesh);

			if (spinMesh && flipSpinMesh)
				spinMesh.triangles = spinMesh.triangles.Reverse().ToArray();

			TrimSpinMaterials();

			var wheelModule = wheelInstance.Module;

			if (!wheelModule.BrakeCalliper)
			{
				var wheelModel = wheelModule.Model;
				var brakeCalliper = new GameObject($"{wheelModule.WheelName}_TempBrakeCalliper").transform;

				brakeCalliper.transform.SetPositionAndRotation(wheelModel.position, wheelModel.rotation);
				brakeCalliper.SetParent(vehicleInstance.transform, true);

				if (Settings.useHideFlags)
					brakeCalliper.gameObject.hideFlags = HideFlags.HideInHierarchy;

				wheelModule.BrakeCalliper = brakeCalliper;
			}

			Awaken = true;
#endif
		}

		private void Initialize()
		{
			meshFilter = null;
			meshRenderer = null;
#if !MVC_COMMUNITY
			cameras = null;
			spinMesh = null;
#endif
		}

		#endregion

		#region Utilities

		public void AddSpinMaterial(int materialIndex)
		{
			AddSpinMaterial(materialIndex, null);
		}
		public void AddSpinMaterial(int materialIndex, Material sharedSpinMaterial)
		{
			if (!MeshRenderer)
			{
				ToolkitDebug.Warning("The used `Vehicle Wheel Spin` component doesn't have a valid Mesh Renderer attached to it.", gameObject);

				return;
			}

			if (materialIndex < 0 || materialIndex >= MeshRenderer.sharedMaterials.Length)
				throw new ArgumentOutOfRangeException("materialIndex", materialIndex, "The parameter `materialIndex` is out of range.");

			if (SpinMaterialIndexExists(materialIndex))
				return;

			List<SpinMaterialModule> spinIndexes = SpinMaterials.ToList();

			spinIndexes.Add(new(this, materialIndex, sharedSpinMaterial));

			spinMaterials = spinIndexes.Distinct().ToArray();

			TrimSpinMaterials();
		}
		public bool SpinMaterialIndexExists(int materialIndex)
		{
			return GetSpinMaterial(materialIndex);
		}
		public SpinMaterialModule GetSpinMaterial(int materialIndex)
		{
			if (!MeshRenderer)
			{
				ToolkitDebug.Warning("The used `Vehicle Wheel Spin` component doesn't have a valid Mesh Renderer attached to it.", gameObject);

				return null;
			}

			if (materialIndex < 0 || materialIndex >= MeshRenderer.sharedMaterials.Length)
				throw new ArgumentOutOfRangeException("materialIndex", materialIndex, "The parameter `materialIndex` is out of range.");

			TrimSpinMaterials();

			return Array.Find(spinMaterials, module => module.MaterialIndex == materialIndex);
		}
		public void RemoveSpinMaterial(int materialIndex)
		{
			if (!MeshRenderer)
			{
				ToolkitDebug.Warning("The used `Vehicle Wheel Spin` component doesn't have a valid Mesh Renderer attached to it.", gameObject);

				return;
			}

			if (materialIndex < 0 || materialIndex >= MeshRenderer.sharedMaterials.Length)
				throw new ArgumentOutOfRangeException("materialIndex", materialIndex, "The parameter `materialIndex` is out of range.");

			List<SpinMaterialModule> spinIndexes = SpinMaterials.ToList();

			spinIndexes.RemoveAt(spinIndexes.FindIndex(material => material.MaterialIndex == materialIndex));

			spinMaterials = spinIndexes.Distinct().ToArray();

			TrimSpinMaterials();
		}

		private void TrimSpinMaterials()
		{
			if (!MeshRenderer)
				return;

			spinMaterials = SpinMaterials.Where(material => material.MaterialIndex > -1 && material.MaterialIndex < MeshRenderer.sharedMaterials.Length).ToArray();

			for (int i = 0; i < spinMaterials.Length; i++)
			{
				if (!spinMaterials[i].RuntimeSpinMaterial)
					continue;

				// Ensure instancing is enabled if system supports it
				if (SystemInfo.supportsInstancing)
				{
					spinMaterials[i].RuntimeSpinMaterial.enableInstancing = true;

					spinMaterials[i].RuntimeSpinMaterial.EnableKeyword("_INSTANCING_ON");
				}

				// Ensure transparency settings
				spinMaterials[i].RuntimeSpinMaterial.SetFloat("_ZWrite", 0); // Disable ZWrite for transparency

				spinMaterials[i].RuntimeSpinMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
			}
		}

		#endregion

		#region Update

#if !MVC_COMMUNITY
		private void Update()
		{
			if (!Awaken)
				return;

			float absRPM = math.abs(wheelInstance.RPM);

			if (absRPM > 1f && vehicleInstance.isBodyVisible)
			{
				visible = !visibilityCulling;

				var transformPosition = transform.position;
				var wheelModule = wheelInstance.Module;

				if (visibilityCulling)
				{
					float cullingDistance = Settings.WheelSpinCullingDistance;
					var brakeCalliper = wheelModule.BrakeCalliper;
					var wheelForward = brakeCalliper.forward;

					foreach (var camera in cameras)
					{
						if (!camera.isActiveAndEnabled)
							continue;

						var cameraPosition = camera.transform.position;
						var cameraDistance = Utility.Distance(cameraPosition, transformPosition);

						if (cameraDistance > cullingDistance || !camera.PointInFrontOfViewport(transformPosition))
							continue;

						var cameraDirection = Utility.DirectionUnNormalized(cameraPosition, transformPosition) / cameraDistance;
						float angleBetween = Vector3.SignedAngle(wheelForward, cameraDirection, camera.transform.up);

						if (angleBetween < -180f)
							angleBetween += 360f;

						if (angleBetween > 180f)
							angleBetween -= 360f;

						visible = wheelModule.side switch
						{
							VehicleWheel.Side.Left => angleBetween >= 0f && angleBetween <= 180f,
							VehicleWheel.Side.Right => angleBetween <= 0f && angleBetween >= -180f,
							_ => true,
						};
					}
				}

				if (visible)
				{
					float rpmMultiplier = Utility.InverseLerp(1f, Settings.WheelSpinRPMThreshold, absRPM);
					Quaternion vehicleRotation = vehicleInstance.transform.rotation;
					int wheelSpinSamplesCount = Settings.WheelSpinSamplesCount;
					float totalBlurAlphaOffset = TotalBlurAlphaOffset;
					var wheelModel = wheelModule.Model;
					var modelLocalRotation = wheelModel.localRotation;
					var transformRotation = transform.rotation;
					var lossyScale = transform.lossyScale;
					Quaternion rotation;

					for (int i = 0; i < spinMaterials.Length; i++)
					{
						var spinMaterial = spinMaterials[i];
						Material runtimeSpinMaterial = spinMaterial ? spinMaterial.RuntimeSpinMaterial : null;

						if (!runtimeSpinMaterial)
							continue;

						spinColor = runtimeSpinMaterial.color;
						spinColor.a = Mathf.Clamp01((2f / wheelSpinSamplesCount) + totalBlurAlphaOffset);

						if (lastAlpha != spinColor.a)
							runtimeSpinMaterial.color = spinColor;

						NativeList<Matrix4x4> matrixList = new(Allocator.Temp);

						for (int j = 0; j <= wheelSpinSamplesCount; j++)
						{
							rotation = ValidateQuaternion(vehicleRotation * Quaternion.Lerp(modelLocalRotation, LerpRotationToAngle(modelLocalRotation, Vector3.right, 180f), j * rpmMultiplier / wheelSpinSamplesCount), transformRotation);

							if (wheelModel != transform)
								rotation = RotationToParent(rotation, transform, wheelModel);

							rotation = ValidateQuaternion(Quaternion.Normalize(rotation), rotation);

							Matrix4x4 matrix = Matrix4x4.TRS(transformPosition, rotation, lossyScale);

							if (IsValidMatrix(matrix))
								matrixList.Add(matrix);
						}

						RenderParams renderParams = new(runtimeSpinMaterial)
						{
							layer = gameObject.layer,
							shadowCastingMode = meshRenderer.shadowCastingMode,
							receiveShadows = meshRenderer.receiveShadows
						};

						if (SystemInfo.supportsInstancing)
							Graphics.RenderMeshInstanced(renderParams, spinMesh, i, matrixList.AsArray());
						else
							foreach (Matrix4x4 matrix in matrixList)
								Graphics.RenderMesh(renderParams, spinMesh, i, matrix);

						matrixList.Dispose();
					}
				}

				if (lastAlpha >= 1f && spinColor.a < 1f)
					meshRenderer.enabled = absRPM < Settings.WheelHideRPMThreshold;

				lastAlpha = spinColor.a;
			}
			else if (lastAlpha < 1f || meshRenderer.enabled != vehicleInstance.isBodyVisible)
			{
				meshRenderer.enabled = vehicleInstance.isBodyVisible;
				lastAlpha = 1f;
			}
		}

		private bool IsValidMatrix(Matrix4x4 matrix)
		{
			for (int i = 0; i < 16; i++)
				if (float.IsNaN(matrix[i]) || float.IsInfinity(matrix[i]))
				{
					ToolkitDebug.Error($"Invalid matrix detected\r\n{matrix}");

					return false;
				}

			return true;
		}
		private Quaternion RotationToParent(Quaternion rotation, Transform self, Transform parent)
		{
			Transform currentParent = self.parent;

			while (currentParent && currentParent != parent)
			{
				rotation = ValidateQuaternion(rotation * currentParent.localRotation, rotation);
				currentParent = currentParent.parent;
			}

			return ValidateQuaternion(rotation * self.localRotation, rotation);
		}
		private Quaternion ValidateQuaternion(Quaternion q, Quaternion @default)
		{
			if (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w))
				return @default;

			return q;
		}
		private Quaternion LerpRotationToAngle(Quaternion rotation, Vector3 rotationAxis, float targetAngle)
		{
			return rotation * Quaternion.Euler(targetAngle * rotationAxis);
		}
#endif

		#endregion

		#region Reset

		private void Reset()
		{
			Initialize();
		}

		#endregion

		#endregion
	}
}

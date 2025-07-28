#region Namespaces

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Serialization;
using UnityEngine.InputSystem.EnhancedTouch;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Utilities;
using Utilities.Inputs;
using MVC.Base;
using MVC.Internal;
using MVC.UI;
using System.Runtime.InteropServices;

#endregion

namespace MVC.Core
{
	[AddComponentMenu("Multiversal Vehicle Controller/Vehicle Follower", 60)]
	[DisallowMultipleComponent]
	[DefaultExecutionOrder(100)]
	public class VehicleFollower : ToolkitBehaviour
	{
		#region Modules

		#region Jobs

#if !MVC_COMMUNITY
		[BurstCompile]
#endif
		private struct UpdateJob : IJob
		{
			#region Variables

			[ReadOnly] public NativeArray<VehicleFollowerPivotAccess> pivots;
			[ReadOnly] public NativeArray<VehicleCameraAccess> cameras;
			public NativeArray<StatsAccess> stats;
			[ReadOnly] public Vehicle.StatsAccess vehicleStats;
			[ReadOnly] public InputsAccess inputs;
			[ReadOnly] public Utility.TransformAccess vehicleTransform;
			[ReadOnly] public float3 vehicleExtents;
			[ReadOnly] public float showcaseDistanceMultiplier;
			[ReadOnly] public float showcaseHeightMultiplier;
			[ReadOnly] public float showcaseHeightRandomization;
			[ReadOnly] public float cameraChangeTime;
			[ReadOnly] public float showcaseTimeout;
			[ReadOnly] public float showcaseSpeed;
			[ReadOnly] public float deltaTime;
			[ReadOnly] public float time;
			[ReadOnly] public bool useSmoothCameraChange;
			[ReadOnly] public bool ignorePivotsSmoothing;

			#endregion

			#region Methods

			public void Execute()
			{
				StatsAccess data = stats[0];

				#region Change Camera

				if (inputs.changeCameraWasPressed)
				{
					if (data.currentCamera.isPivotCamera)
					{
						if (data.currentPivotIndex < 0)
							data.currentPivotIndex = 0;
						else
							data.currentPivotIndex++;

						if (data.currentPivotIndex >= pivots.Length)
							data.currentPivotIndex = 0;

						data.lastPivotIndex = LoopCameraIndex(data.currentPivotIndex - 1, pivots.Length);
					}

					data.currentCameraIndex++;

					if (data.currentCameraIndex >= cameras.Length)
						data.currentCameraIndex = 0;

					data.lastCameraIndex = LoopCameraIndex(data.currentCameraIndex - 1, cameras.Length);

					if (data.currentCameraIndex != data.lastCameraIndex)
					{
						data.currentCamera = cameras[data.currentCameraIndex];
						data.lastCamera = cameras[data.lastCameraIndex];

						data.cameraChangeTimer = 0f;
						data.orbitTimer = 0f;
					}
				}

				if (useSmoothCameraChange && data.cameraChangeTimer < cameraChangeTime)
				{
					data.cameraChangeTimer += deltaTime;
					data.cameraChangeTimer = math.clamp(data.cameraChangeTimer, 0f, cameraChangeTime);
				}

				data.cameraChangeFactor = useSmoothCameraChange && cameraChangeTime > 0f && (!ignorePivotsSmoothing || !data.currentCamera.isPivotCamera && !data.lastCamera.isPivotCamera) ? data.cameraChangeTimer / cameraChangeTime : 1f;
				data.followerPivotChangeFactor = data.currentCamera.isPivotCamera ? data.cameraChangeFactor : 0f;

				#endregion

				#region Showcase

				data.canShowcase = data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.Showcase);

				if (data.canShowcase && math.round(data.xOrbitAngle) == 0f && math.round(data.mouseYOrbitAngle) == 0f && data.showcaseTimer < showcaseTimeout)
					data.showcaseTimer += deltaTime;

				if (inputs.anyKeyWasPressed || math.round(vehicleStats.currentSpeed) > 0f || !data.canShowcase && data.showcaseTimer > 0f)
					data.showcaseTimer = 0f;

				data.showcasing = data.canShowcase && data.showcaseTimer >= showcaseTimeout;

				if (data.showcasing && data.showcaseFactor < 1f)
					data.showcaseFactor += deltaTime;

				if (!data.showcasing && data.showcaseFactor > 0f)
					data.showcaseFactor -= deltaTime;

				if (data.showcasing)
				{
					data.showcaseOrbitAngle += deltaTime * showcaseSpeed;

					while (data.showcaseOrbitAngle > 180f)
						data.showcaseOrbitAngle -= 360f;

					while (data.showcaseOrbitAngle < -180f)
						data.showcaseOrbitAngle += 360f;
				}
				else if (data.showcaseFactor <= 0f && data.showcaseOrbitAngle != 0f)
					data.showcaseOrbitAngle = 0f;

				data.showcaseFactor = Utility.Clamp01(data.showcaseFactor);
				data.showcaseLookPoint = vehicleTransform.position + showcaseHeightMultiplier * vehicleExtents.y * vehicleTransform.up;
				data.showcaseLookPoint += .25f * math.sin(time * math.PI * .03444691862212037543122204359625f * showcaseHeightRandomization) * vehicleExtents.y * vehicleTransform.up;
				data.showcasePosition = data.showcaseLookPoint - math.mul(math.mul(vehicleTransform.rotation, quaternion.Euler(0f, math.radians(data.showcaseOrbitAngle), 0f)), vehicleExtents.z * 3f * showcaseDistanceMultiplier * Utility.Float3Forward);

				#endregion

				stats[0] = data;
			}

			#endregion
		}
#if !MVC_COMMUNITY
		[BurstCompile]
#endif
		private struct OrbitJob : IJob
		{
			#region Variables

			public NativeArray<StatsAccess> stats;
			public NativeArray<VehicleCameraAccess> cameras;
			public InputsAccess inputs;
			public float mouseOrbitIntensity;
			public float rotationDamping;
			public float heightDamping;
			public float deltaTime;
			public bool unityLegacyInputManager;

			#endregion

			#region Methods

			public void Execute()
			{
				StatsAccess data = stats[0];

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.HorizontalOrbit) || data.currentCamera.HasPivotFeature(VehicleCamera.PivotFeature.HorizontalOrbit))
				{
					data.mouseOrbitOffset = !data.currentCamera.orbitUsingMouseButton || inputs.orbitMouseButton || inputs.touchUsed ? (data.currentCamera.invertXOrbit ? -1f : 1f) * (inputs.mouseMovement.x + inputs.touchMovement.x) * .1618f * mouseOrbitIntensity : 0f;

					if (unityLegacyInputManager)
						data.mouseOrbitOffset *= 10f;

					if (inputs.sidewaysCameraViewWasPressed || inputs.forwardCameraViewWasPressed)
					{
						data.mouseTargetXOrbitAngle = 0f;
						data.mouseOrbitOffset = 0f;
					}
					else
					{
						data.mouseTargetXOrbitAngle += data.mouseOrbitOffset;
						data.mouseTargetXOrbitAngle = math.clamp(data.mouseTargetXOrbitAngle, data.currentCamera.orbitInterval.x.Min, data.currentCamera.orbitInterval.x.Max);

						while (data.mouseTargetXOrbitAngle > 180f)
							data.mouseTargetXOrbitAngle -= 360f;

						while (data.mouseTargetXOrbitAngle < -180f)
							data.mouseTargetXOrbitAngle += 360f;
					}

					data.mouseOrbitChanged = data.mouseOrbitOffset != 0f;
				}

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.VerticalOrbit) || data.currentCamera.HasPivotFeature(VehicleCamera.PivotFeature.VerticalOrbit))
				{
					data.mouseOrbitOffset = !data.currentCamera.orbitUsingMouseButton || inputs.orbitMouseButton || inputs.touchUsed ? (data.currentCamera.invertYOrbit ? -1f : 1f) * (inputs.mouseMovement.y + inputs.touchMovement.y) * .1618f * .5f * mouseOrbitIntensity : 0f;

					if (unityLegacyInputManager)
						data.mouseOrbitOffset *= 10f;

					if (inputs.forwardCameraViewWasPressed)
					{
						data.mouseTargetYOrbitAngle = 0f;
						data.mouseOrbitOffset = 0f;
					}
					else
					{
						data.mouseTargetYOrbitAngle += data.mouseOrbitOffset;
						data.mouseTargetYOrbitAngle = math.clamp(data.mouseTargetYOrbitAngle, data.currentCamera.orbitInterval.y.Min, data.currentCamera.orbitInterval.y.Max);

						while (data.mouseTargetYOrbitAngle > 180f)
							data.mouseTargetYOrbitAngle -= 360f;

						while (data.mouseTargetYOrbitAngle < -180f)
							data.mouseTargetYOrbitAngle += 360f;
					}

					data.mouseOrbitChanged = data.mouseOrbitChanged || data.mouseOrbitOffset != 0f;
				}

				if (data.mouseOrbitChanged)
					data.orbitTimer = data.currentCamera.orbitTimeout;
				else if (data.orbitTimer <= 0f)
				{
					data.mouseTargetXOrbitAngle = 0f;
					data.mouseTargetYOrbitAngle = 0f;
				}
				else
					data.orbitTimer -= deltaTime;

				data.mouseXOrbitAngle = Mathf.LerpAngle(data.mouseXOrbitAngle, data.mouseTargetXOrbitAngle, deltaTime * rotationDamping);
				data.mouseYOrbitAngle = Mathf.LerpAngle(data.mouseYOrbitAngle, data.mouseTargetYOrbitAngle, deltaTime * (rotationDamping + heightDamping));
				data.keysOrbitDirection = new float3(-inputs.cameraView.x, 0f, data.currentCamera.isPivotCamera ? Utility.Clamp01(inputs.cameraView.y) : inputs.cameraView.y);
				data.keysTargetOrbitAngle = Vector3.SignedAngle(data.keysOrbitDirection, -Vector3.forward, Vector3.up);
				data.skippingDistantOrbit = data.currentCamera.skipDistantOrbit && Mathf.DeltaAngle(data.keysOrbitAngle, data.keysTargetOrbitAngle) > data.currentCamera.orbitSkipAngle;
				data.keysOrbitAngle = Mathf.LerpAngle(data.keysOrbitAngle, math.clamp(data.keysTargetOrbitAngle, data.currentCamera.orbitInterval.x.Min, data.currentCamera.orbitInterval.x.Max), data.skippingDistantOrbit ? 1f : deltaTime * rotationDamping);
				data.xOrbitAngle = data.keysOrbitAngle + data.mouseXOrbitAngle;

				while (data.xOrbitAngle > 180f)
					data.xOrbitAngle -= 360f;

				while (data.xOrbitAngle < -180f)
					data.xOrbitAngle += 360f;

				while (data.mouseYOrbitAngle > 180f)
					data.mouseYOrbitAngle -= 360f;

				while (data.mouseYOrbitAngle < -180f)
					data.mouseYOrbitAngle += 360f;

				stats[0] = data;
			}

			#endregion
		}
#if !MVC_COMMUNITY
		[BurstCompile]
#endif
		private struct CalculationsJob : IJob
		{
			#region Variables

			public NativeArray<StatsAccess> stats;
			[ReadOnly] public NativeArray<VehicleCameraAccess> cameras;
			[ReadOnly] public Vehicle.SteeringAccess vehicleSteering;
			[ReadOnly] public Vehicle.InputsAccess vehicleInputs;
			[ReadOnly] public VehicleEngineAccess vehicleEngine;
			[ReadOnly] public Vehicle.StatsAccess vehicleStats;
			[ReadOnly] public VehicleTrailer.FollowerModifierAccess trailerFollowerModifier;
			[ReadOnly] public Utility.TransformAccess vehicleTransform;
			[ReadOnly] public float3 vehicleLastPivotPoint;
			[ReadOnly] public float3 vehiclePivotPoint;
			[ReadOnly] public float3 lookPointOffset;
			[ReadOnly] public float3 vehicleExtents;
			[ReadOnly] public float3 randomInsideUnitSphere;
			[ReadOnly] public float2 shakingSpeed;
			[ReadOnly] public float distanceMultiplier;
			[ReadOnly] public float distance;
			[ReadOnly] public float idleHeightMultiplier;
			[ReadOnly] public float heightMultiplier;
			[ReadOnly] public float height;
			[ReadOnly] public float heightDamping;
			[ReadOnly] public float idleMaximumSpeed;
			[ReadOnly] public float deltaTime;
			[ReadOnly] public float time;
			[ReadOnly] public bool vehicleTrailerLinkConnected;
			[ReadOnly] public bool autoSetDimensions;
			[ReadOnly] public int vehicleWheelsCount;
			[ReadOnly] public int vehicleMotorWheelsCount;

			#endregion

			#region Methods

			public void Execute()
			{
				StatsAccess data = stats[0];

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.StuntsShowcase))
				{
					data.targetStuntFactor = 1f - Utility.Clamp01(vehicleStats.currentSpeed / 60f);
					data.targetStuntFactor *= Utility.Clamp01(vehicleStats.averageMotorWheelsSpeed) * math.abs(vehicleStats.steerAngle / vehicleSteering.maximumSteerAngle) * (vehicleStats.wheelPower / math.max(vehicleEngine.power, vehicleStats.rawEnginePower));
					data.targetStuntFactor *= Utility.Clamp01(-Mathf.Sign(vehicleStats.localAngularVelocity.y) * vehicleStats.averageMotorWheelsSmoothSidewaysSlip);
					data.currentStuntFactor = Utility.Lerp(data.currentStuntFactor, data.targetStuntFactor, math.pow(1.618f, data.targetStuntFactor > data.currentStuntFactor ? 2.617924f : 1f) * deltaTime);
				}
				else
				{
					data.targetStuntFactor = Utility.Lerp(data.targetStuntFactor, 0f, data.cameraChangeFactor);
					data.currentStuntFactor = Utility.Lerp(data.currentStuntFactor, 0f, data.cameraChangeFactor);
				}

				data.autoHeight = autoSetDimensions ? vehicleExtents.y * 1.618f * heightMultiplier : height;
				data.targetHeight = Utility.Lerp(data.autoHeight * idleHeightMultiplier, data.autoHeight * Utility.Lerp(1f, .5f, data.currentStuntFactor), Utility.InverseLerp(0f, idleMaximumSpeed, vehicleStats.currentSpeed));
				data.autoDistance = autoSetDimensions ? vehicleExtents.z * distanceMultiplier : distance;
				data.autoDistance *= Utility.Lerp(1f, 1.618f, data.currentStuntFactor);
				data.autoDistance *= vehicleTrailerLinkConnected ? trailerFollowerModifier.distance : 1f;
				data.currentHeight = Utility.Lerp(data.currentHeight, data.targetHeight, data.currentHeight == 0f ? 1f : deltaTime * math.max(data.vehicleLocalVelocity.y, 1f / heightDamping) * heightDamping);
				data.lookPoint = Utility.PointLocalToWorld(Utility.Lerp(vehicleLastPivotPoint, vehiclePivotPoint, data.cameraChangeFactor), vehicleTransform.position, vehicleTransform.rotation, vehicleTransform.lossyScale);
				data.lookPoint.y += Utility.Lerp(0f, vehicleExtents.y, (-Utility.Lerp(data.lastCamera.pivotPoint.y, data.currentCamera.pivotPoint.y, data.cameraChangeFactor) + 1f) * .5f) + lookPointOffset.y;

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.SteeringTilt) || data.currentCamera.HasPivotFeature(VehicleCamera.PivotFeature.SteeringTilt))
				{
					data.maxTiltAngle = Utility.Lerp(data.lastCamera.tiltAngle, data.currentCamera.tiltAngle, data.cameraChangeFactor);
					data.tiltAngle = math.clamp(Utility.Lerp(data.lastCamera.invertTiltAngle ? -1f : 1f, data.currentCamera.invertTiltAngle ? -1f : 1f, data.cameraChangeFactor) * data.vehicleLocalVelocity.x * data.maxTiltAngle * .1f, -data.maxTiltAngle, data.maxTiltAngle);
				}
				else
					data.tiltAngle = Utility.Lerp(data.tiltAngle, 0f, data.cameraChangeFactor);

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.NOSFieldOfView) || data.currentCamera.HasPivotFeature(VehicleCamera.PivotFeature.NOSFieldOfView))
					data.NOSFieldOfViewFactor = Utility.Lerp(data.NOSFieldOfViewFactor, vehicleStats.NOSBoost, deltaTime * 6.853526069776f);
				else
					data.NOSFieldOfViewFactor = Utility.Lerp(data.NOSFieldOfViewFactor, 0f, data.cameraChangeFactor);

				if (data.currentCamera.HasPivotFeature(VehicleCamera.PivotFeature.SpeedFieldOfView))
					data.speedFieldOfViewFactor = Utility.Clamp01(vehicleStats.currentSpeed / idleMaximumSpeed * 6.853526069776f);
				else
					data.speedFieldOfViewFactor = Utility.Lerp(data.speedFieldOfViewFactor, 0f, data.cameraChangeFactor);

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.SpeedShaking) || data.currentCamera.HasPivotFeature(VehicleCamera.PivotFeature.SpeedShaking))
					data.speedShake = randomInsideUnitSphere.y * .02128981373431419989568729517692f * Utility.InverseLerp(Utility.Lerp(data.lastCamera.shakeSpeedInterval.Min, data.currentCamera.shakeSpeedInterval.Min, data.cameraChangeFactor), Utility.Lerp(data.lastCamera.shakeSpeedInterval.Max, data.currentCamera.shakeSpeedInterval.Max, data.cameraChangeFactor), vehicleStats.currentSpeed);
				else
					data.speedShake = Utility.Lerp(data.speedShake, 0f, data.cameraChangeFactor);

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.OffRoadShaking) || data.currentCamera.HasPivotFeature(VehicleCamera.PivotFeature.OffRoadShaking))
					data.offRoadShake = randomInsideUnitSphere.y * .02128981373431419989568729517692f * (vehicleStats.averageMotorWheelsSpeedAbs / idleMaximumSpeed) * vehicleStats.offRoadingMotorWheelsCount / vehicleWheelsCount;
				else
					data.offRoadShake = Utility.Lerp(data.offRoadShake, 0f, data.cameraChangeFactor);

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.NOSShaking) || data.currentCamera.HasPivotFeature(VehicleCamera.PivotFeature.NOSShaking))
					data.NOSShake = randomInsideUnitSphere.x * .02128981373431419989568729517692f * vehicleStats.NOSBoost;
				else
					data.NOSShake = Utility.Lerp(data.NOSShake, 0f, data.cameraChangeFactor);

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.ForwardSlipShaking))
					data.forwardSlipShake = randomInsideUnitSphere.z * .02128981373431419989568729517692f * Utility.Clamp01(vehicleStats.averageMotorWheelsForwardSlip);
				else
					data.forwardSlipShake = Utility.Lerp(data.forwardSlipShake, 0f, data.cameraChangeFactor);

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.BrakeSlipShaking))
					data.brakeSlipShake = randomInsideUnitSphere.z * .02128981373431419989568729517692f * Utility.Clamp01(-vehicleStats.averageMotorWheelsForwardSlip);
				else
					data.brakeSlipShake = Utility.Lerp(data.brakeSlipShake, 0f, data.cameraChangeFactor);

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.SidewaysSlipShaking))
					data.sidewaysSlipShake = randomInsideUnitSphere.x * .02128981373431419989568729517692f * math.abs(vehicleStats.averageMotorWheelsSidewaysSlip);
				else
					data.sidewaysSlipShake = Utility.Lerp(data.sidewaysSlipShake, 0f, data.cameraChangeFactor);

				if (data.currentCamera.impactAxes != 0)
				{
					data.impactVelocity = Utility.Lerp(data.impactVelocity, float2.zero, deltaTime * 2.617924f);
					data.smoothImpactVelocity = data.impactVelocity;

					if (data.currentCamera.impactShaking == VehicleCamera.ShakingType.Intense)
						data.smoothImpactVelocity *= math.sin(time * math.PI * 3.236f * shakingSpeed);

					data.smoothImpactVelocity.y = math.clamp(data.smoothImpactVelocity.y, -data.autoHeight + .1618f, math.INFINITY);
				}
				else if (!data.impactVelocity.Equals(float2.zero) || !data.smoothImpactVelocity.Equals(float2.zero))
				{
					data.impactVelocity = Utility.Lerp(data.impactVelocity, float2.zero, data.cameraChangeFactor);
					data.smoothImpactVelocity = Utility.Lerp(data.smoothImpactVelocity, float2.zero, data.cameraChangeFactor);
				}

				data.pulse = float3.zero;
				data.pulseMultiplier = Utility.Float3One;
				data.pulseMultiplier.z = 1f - Utility.Clamp01(math.abs(data.xOrbitAngle) / 90f);

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.SpeedPulse))
				{
					data.pulse.z -= .5f * Utility.Clamp01(vehicleInputs.Direction) * Utility.Clamp01(vehicleStats.currentSpeed);
					data.pulse.z *= Utility.Clamp01(vehicleInputs.RawFuel - (vehicleStats.currentSpeed / idleMaximumSpeed));
					data.pulse.z += (data.autoDistance - vehicleExtents.z) * .12720062892926276722655295949659f * vehicleInputs.Direction * math.clamp(data.vehicleLocalVelocity.z, -.2617924f, 1f);
					data.pulse.z -= math.max(data.vehicleLocalVelocity.z, 0f) * 3.6f * (data.autoDistance - vehicleExtents.z) / 300f;
				}

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.GearPulse))
					data.pulse.z += vehicleStats.isChangingGearUp ? (data.autoDistance - vehicleExtents.z) * .4045f * Utility.Clamp01(vehicleInputs.Direction) * vehicleStats.currentSpeed * .015f : 0f;

				if (data.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.SidewaysSlipPulse))
				{
					data.pulse.x -= math.clamp(vehicleStats.averageRearWheelsSidewaysSlip * 2f, -1f, 1f);
					data.pulse.z += (data.autoDistance - vehicleExtents.z) * .61804697156983930778739184177998f * Utility.Clamp01(math.abs(vehicleStats.averageRearWheelsSidewaysSlip) * 2f);
				}

				if (data.currentCamera.HasPivotFeature(VehicleCamera.PivotFeature.SteeringSidePulse))
					data.pulse.x += math.clamp(data.vehicleLocalVelocity.x * .1f, -1f, 1f) * .38198205906664975759418531630406f;

				data.smoothPulse = Utility.Lerp(data.smoothPulse, Utility.Multiply(data.pulse, data.pulseMultiplier), deltaTime);

				stats[0] = data;
			}

			#endregion
		}
#if !MVC_COMMUNITY
		[BurstCompile]
#endif
		private struct UpdateCameraJob : IJobParallelFor
		{
			#region Variables

			[ReadOnly] public VehicleTrailer.FollowerModifierAccess trailerFollowerModifier;
			[ReadOnly] public NativeArray<VehicleFollowerPivotAccess> pivots;
			[ReadOnly] public NativeArray<VehicleCameraAccess> cameras;
			public NativeArray<quaternion> currentRotations;
			public NativeArray<quaternion> targetRotations;
			public NativeArray<float3> targetPositions;
			[ReadOnly] public NativeArray<int> followerPivotsIndexes;
			[ReadOnly] public StatsAccess stats;
			[ReadOnly] public Utility.TransformAccess vehicleTransform;
			[ReadOnly] public float rotationDamping;
			[ReadOnly] public float deltaTime;
			[ReadOnly] public bool vehicleTrailerLinkConnected;

			#endregion

			#region Methods

			public void Execute(int index)
			{
				switch (cameras[index].type)
				{
					case VehicleCamera.CameraType.Pivot:
						targetPositions[index] = pivots[followerPivotsIndexes[index]].transform.position;
						targetRotations[index] = pivots[followerPivotsIndexes[index]].transform.rotation;
						currentRotations[index] = math.mul(targetRotations[index], quaternion.Euler(math.radians(stats.mouseYOrbitAngle), math.radians(stats.xOrbitAngle), 0f));

						break;

					default:
						targetRotations[index] = Utility.Lerp(targetRotations[index], vehicleTransform.rotation, deltaTime * rotationDamping * (1f - stats.currentStuntFactor));
						currentRotations[index] = math.mul(targetRotations[index], Utility.Lerp(quaternion.Euler(math.radians(stats.mouseYOrbitAngle), math.radians(stats.xOrbitAngle), 0f), quaternion.identity, stats.showcaseFactor + stats.currentStuntFactor));
						targetPositions[index] = vehicleTransform.position + (vehicleTrailerLinkConnected ? trailerFollowerModifier.height : 1f) * stats.currentHeight * vehicleTransform.up;
						targetPositions[index] -= math.mul(currentRotations[index], Utility.Float3Forward) * stats.autoDistance;

						break;
				}

				targetPositions[index] += Utility.Lerp(vehicleTransform.right * stats.smoothPulse.x + vehicleTransform.up * stats.smoothPulse.y + vehicleTransform.forward * stats.smoothPulse.z, float3.zero, stats.currentStuntFactor);
			}

			#endregion
		}
#if !MVC_COMMUNITY
		[BurstCompile]
#endif
		private struct ApplyTargetsJob : IJob
		{
			#region Variables

			public NativeArray<Utility.TransformAccess> transform;
			public NativeArray<float> camerasFieldOfView;
			[ReadOnly] public NativeArray<quaternion> currentRotations;
			[ReadOnly] public NativeArray<float3> targetPositions;
			[ReadOnly] public NativeArray<VehicleCameraAccess> cameras;
			[ReadOnly] public StatsAccess stats;
			[ReadOnly] public float showcaseFieldOfView;
			[ReadOnly] public float userFieldOfView;
			[ReadOnly] public bool setUserFieldOfView;
			[ReadOnly] public float weight;

			#endregion

			#region Methods

			public void Execute()
			{
				if (Mathf.Approximately(weight, 0f))
					return;

				Utility.TransformAccess targetTransform = transform[0];
				Utility.TransformAccess orgTransform = targetTransform;

				targetTransform.position = Utility.Lerp(Utility.Lerp(targetPositions[stats.lastCameraIndex], targetPositions[stats.currentCameraIndex], stats.cameraChangeFactor), stats.showcasePosition, stats.showcaseFactor);

				if (cameras[stats.currentCameraIndex].isPivotCamera)
					targetTransform.rotation = Utility.Lerp(currentRotations[stats.lastCameraIndex], currentRotations[stats.currentCameraIndex], stats.followerPivotChangeFactor);
				else
				{
					quaternion targetRotation = quaternion.LookRotation(Utility.Direction(targetTransform.position, Utility.Lerp(stats.lookPoint, stats.showcaseLookPoint, stats.showcaseFactor)), Utility.Float3Up);

					targetTransform.rotation = Utility.Lerp(cameras[stats.lastCameraIndex].isPivotCamera ? currentRotations[stats.lastCameraIndex] : targetRotation, targetRotation, stats.cameraChangeFactor);
				}

				if (cameras[stats.currentCameraIndex].HasFollowerFeature(VehicleCamera.FollowerFeature.SteeringTilt))
					targetTransform.rotation = math.mul(targetTransform.rotation, quaternion.Euler(0f, 0f, math.radians(stats.tiltAngle)));

				targetTransform.rotation = Quaternion.LerpUnclamped(orgTransform.rotation, targetTransform.rotation, weight);
				targetTransform.position = math.lerp(orgTransform.position, targetTransform.position, weight);
				transform[0] = targetTransform;

				for (int i = 0; i < camerasFieldOfView.Length; i++)
					camerasFieldOfView[i] = math.lerp(camerasFieldOfView[i], setUserFieldOfView ? userFieldOfView : Utility.Lerp(Utility.LerpUnclamped(Utility.Lerp(cameras[stats.lastCameraIndex].fieldOfViewInterval.Min, cameras[stats.currentCameraIndex].fieldOfViewInterval.Min, stats.cameraChangeFactor), Utility.Lerp(cameras[stats.lastCameraIndex].fieldOfViewInterval.Max, cameras[stats.currentCameraIndex].fieldOfViewInterval.Max, stats.cameraChangeFactor), stats.NOSFieldOfViewFactor + stats.speedFieldOfViewFactor), showcaseFieldOfView, stats.showcaseFactor), weight);
			}

			#endregion
		}
#if !MVC_COMMUNITY
		[BurstCompile]
#endif
		private struct ShakingJob : IJobParallelFor
		{
			#region Variables

			public NativeArray<Utility.TransformAccess> transforms;
			[ReadOnly] public StatsAccess stats;
			[ReadOnly] public float2 shakeIntensity;
			[ReadOnly] public float weight;

			#endregion

			#region Methods

			public void Execute(int index)
			{
				Utility.TransformAccess transform = transforms[index];

				transform.localPosition = math.lerp(transform.localPosition, new(shakeIntensity.x * (stats.smoothImpactVelocity.x + stats.NOSShake + stats.sidewaysSlipShake), shakeIntensity.y * (stats.smoothImpactVelocity.y + stats.speedShake + stats.offRoadShake + stats.forwardSlipShake - stats.brakeSlipShake), 0f), weight);
				transforms[index] = transform;
			}

			#endregion
		}
#if !MVC_COMMUNITY
		[BurstCompile]
#endif
		private struct ObstaclesDetectionJob : IJob
		{
			#region Variables

			public NativeArray<Utility.TransformAccess> transforms;
			public NativeArray<StatsAccess> stats;
			[ReadOnly] public NativeArray<float> obstacleDistances;
			[ReadOnly] public NativeArray<bool> obstacleIsChildOfTrailer;
			[ReadOnly] public NativeArray<bool> obstacleIsChildOfVehicle;
			[ReadOnly] public float obstaclesOffset;
			[ReadOnly] public bool hasTrailer;
			[ReadOnly] public float weight;

			#endregion

			#region Methods

			public void Execute()
			{
				StatsAccess data = stats[0];

				data.maxObstaclesDistance = 0f;

				for (int i = 0; i < data.obstacleHitsCount; i++)
				{
					if (obstacleIsChildOfVehicle[i] || hasTrailer && obstacleIsChildOfTrailer[i])
						continue;

					if (data.maxObstaclesDistance < obstacleDistances[i])
						data.maxObstaclesDistance = obstacleDistances[i];
				}

				if (data.maxObstaclesDistance > 0f)
				{
					data.obstaclesDistance -= data.maxObstaclesDistance;

					float3 localPosition = (data.currentCamera.isPivotCamera ? -1f : 1f) * (obstaclesOffset + data.obstaclesDistance) * Utility.Float3Forward;

					for (int i = 0; i < transforms.Length; i++)
					{
						Utility.TransformAccess transform = transforms[i];

						transform.localPosition = math.lerp(transform.localPosition, transform.localPosition + localPosition, weight);
						transforms[i] = transform;
					}
				}

				stats[0] = data;
			}

			#endregion
		}

		#endregion

		#region Components

		public class InputsSheet
		{
			#region Variables

			public float2 MouseMovement { get; private set; }
			public float2 TouchMovement { get; private set; }
			public float2 CameraView { get; private set; }
			public bool ChangeCameraWasPressed { get; private set; }
			public bool OrbitMouseButton { get; private set; }
			public bool ForwardCameraViewWasPressed { get; private set; }
			public bool SidewaysCameraViewWasPressed { get; private set; }
			public bool TouchUsed { get; private set; }
			public bool AnyKeyWasPressed { get; private set; }
			public bool AnyMobilePresetInputInUse { get; private set; }
			public bool AnyMobilePresetInputWasPressed { get; private set; }

			private readonly VehicleFollower follower;

			#endregion

			#region Methods

			public void Update()
			{
				if (HasInternalErrors)
					return;

				if (Settings.inputSystem == ToolkitSettings.InputSystem.InputsManager)
					UpdateInputsManager();
				else
					UpdateUnityLegacyInputSystem();

				UpdateMobileInputs();
			}

			private void UpdateInputsManager()
			{
				if (!EnhancedTouchSupport.enabled)
					EnhancedTouchSupport.Enable();

				MouseMovement = !AnyMobilePresetInputInUse ? (float2)InputsManager.InputMouseMovement() : float2.zero;
				CameraView = new(InputsManager.InputValue(Settings.sidewaysCameraViewInput), InputsManager.InputValue(Settings.forwardCameraViewInput));
				ChangeCameraWasPressed = InputsManager.InputDown(Settings.changeCameraButtonInput);
				OrbitMouseButton = follower.Cameras != null && InputsManager.InputMouseButtonPress(follower.stats.currentCamera.orbitMouseButton);
				ForwardCameraViewWasPressed = InputsManager.InputDown(Settings.forwardCameraViewInput);
				SidewaysCameraViewWasPressed = InputsManager.InputDown(Settings.sidewaysCameraViewInput);
				TouchUsed = InputsManager.TouchCount > 0;
				TouchMovement = TouchUsed && !AnyMobilePresetInputInUse ? (float2)InputsManager.Touches.FirstOrDefault().delta : float2.zero;
				AnyKeyWasPressed = InputsManager.AnyInputDown() || AnyMobilePresetInputWasPressed;
			}
			private void UpdateUnityLegacyInputSystem()
			{
				MouseMovement = !AnyMobilePresetInputInUse ? new(UnityEngine.Input.GetAxis("Mouse X"), UnityEngine.Input.GetAxis("Mouse Y")) : float2.zero;
				CameraView = new(UnityEngine.Input.GetAxis(Settings.sidewaysCameraViewInput), UnityEngine.Input.GetAxis(Settings.forwardCameraViewInput));
				ChangeCameraWasPressed = UnityEngine.Input.GetButtonDown(Settings.changeCameraButtonInput);
				OrbitMouseButton = follower.Cameras != null && UnityEngine.Input.GetMouseButton((int)follower.stats.currentCamera.orbitMouseButton);
				ForwardCameraViewWasPressed = UnityEngine.Input.GetButtonDown(Settings.forwardCameraViewInput);
				SidewaysCameraViewWasPressed = UnityEngine.Input.GetButtonDown(Settings.sidewaysCameraViewInput);
				TouchUsed = UnityEngine.Input.touchCount > 0;
				TouchMovement = TouchUsed && !AnyMobilePresetInputInUse ? (float2)UnityEngine.Input.touches.FirstOrDefault().deltaPosition : float2.zero;
				AnyKeyWasPressed = UnityEngine.Input.anyKeyDown || AnyMobilePresetInputWasPressed;
			}
			private void UpdateMobileInputs()
			{
				if (InputsManager.AnyInputPress(true) || !Settings.useMobileInputs || !UIController || UIController.MobilePresets.Length < 1)
					return;

				VehicleUIController.MobileInputPreset mobilePreset = UIController.MobilePresets[UIController.ActiveMobilePreset];

				AnyMobilePresetInputInUse = mobilePreset.AnyInputInUse;
				AnyMobilePresetInputWasPressed = mobilePreset.AnyInputWasPressed;
				ChangeCameraWasPressed = mobilePreset.ChangeCamera.IsValid && mobilePreset.ChangeCamera.Source.WasPressed;
			}

			#endregion

			#region Constructors

			public InputsSheet(VehicleFollower follower)
			{
				this.follower = follower;
			}

			#endregion

			#region Operators

			public static implicit operator bool(InputsSheet inputs) => inputs != null;

			#endregion
		}

		private struct InputsAccess
		{
			#region Variables

			public float2 mouseMovement;
			public float2 touchMovement;
			public float2 cameraView;
			[MarshalAs(UnmanagedType.U1)]
			public bool changeCameraWasPressed;
			[MarshalAs(UnmanagedType.U1)]
			public bool orbitMouseButton;
			[MarshalAs(UnmanagedType.U1)]
			public bool forwardCameraViewWasPressed;
			[MarshalAs(UnmanagedType.U1)]
			public bool sidewaysCameraViewWasPressed;
			[MarshalAs(UnmanagedType.U1)]
			public bool touchUsed;
			[MarshalAs(UnmanagedType.U1)]
			public bool anyKeyWasPressed;
			[MarshalAs(UnmanagedType.U1)]
			public bool anyMobilePresetInputInUse;
			[MarshalAs(UnmanagedType.U1)]
			public bool anyMobilePresetInputWasPressed;

			#endregion

			#region Constructors

			public InputsAccess(InputsSheet sheet)
			{
				mouseMovement = sheet.MouseMovement;
				touchMovement = sheet.TouchMovement;
				cameraView = sheet.CameraView;
				changeCameraWasPressed = sheet.ChangeCameraWasPressed;
				orbitMouseButton = sheet.OrbitMouseButton;
				forwardCameraViewWasPressed = sheet.ForwardCameraViewWasPressed;
				sidewaysCameraViewWasPressed = sheet.SidewaysCameraViewWasPressed;
				touchUsed = sheet.TouchUsed;
				anyKeyWasPressed = sheet.AnyKeyWasPressed;
				anyMobilePresetInputInUse = sheet.AnyMobilePresetInputInUse;
				anyMobilePresetInputWasPressed = sheet.AnyMobilePresetInputWasPressed;
			}

			#endregion

			#region Operators

			public static implicit operator InputsAccess(InputsSheet sheet) => new(sheet);

			#endregion
		}
		private struct StatsAccess
		{
			public VehicleCameraAccess currentCamera;
			public VehicleCameraAccess lastCamera;
			public float3 showcasePosition;
			public float3 lastVehicleLocalVelocity;
			public float3 vehicleLocalVelocity;
			public float3 pulse;
			public float3 pulseMultiplier;
			public float3 smoothPulse;
			public float3 lookPoint;
			public float3 showcaseLookPoint;
			public float3 keysOrbitDirection;
			public float3 obstaclesCenter;
			public float2 impactVelocity;
			public float2 smoothImpactVelocity;
			public float cameraChangeTimer;
			public float cameraChangeFactor;
			public float followerPivotChangeFactor;
			public float autoHeight;
			public float targetHeight;
			public float currentHeight;
			public float autoDistance;
			public float mouseOrbitOffset;
			public float mouseTargetXOrbitAngle;
			public float xOrbitAngle;
			public float mouseXOrbitAngle;
			public float mouseTargetYOrbitAngle;
			public float mouseYOrbitAngle;
			public float keysOrbitAngle;
			public float keysTargetOrbitAngle;
			public float orbitTimer;
			public float showcaseOrbitAngle;
			public float speedShake;
			public float NOSShake;
			public float offRoadShake;
			public float forwardSlipShake;
			public float brakeSlipShake;
			public float sidewaysSlipShake;
			public float maxTiltAngle;
			public float tiltAngle;
			public float NOSFieldOfViewFactor;
			public float speedFieldOfViewFactor;
			public float showcaseTimer;
			public float showcaseFactor;
			public float obstaclesDistance;
			public float maxObstaclesDistance;
			public float targetStuntFactor;
			public float currentStuntFactor;
			[MarshalAs(UnmanagedType.U1)]
			public bool skippingDistantOrbit;
			[MarshalAs(UnmanagedType.U1)]
			public bool mouseOrbitChanged;
			[MarshalAs(UnmanagedType.U1)]
			public bool canShowcase;
			[MarshalAs(UnmanagedType.U1)]
			public bool showcasing;
			public int vehiclesLayerMask;
			public int currentCameraIndex;
			public int lastCameraIndex;
			public int currentPivotIndex;
			public int lastPivotIndex;
			public int obstacleHitsCount;
		}

		#endregion

		#endregion

		#region Variables

		#region Static Variables

		internal static VehicleFollower Instance => instance;

		private static VehicleFollower instance;

		#endregion

		#region Global Variables

		#region Fields

		public float Weight
		{
			get
			{
				return weight;
			}
			set
			{
				weight = Utility.Clamp01(value);
			}
		}
		public int CurrentCameraIndex
		{
			get
			{
				if (HasInternalErrors)
					return -1;

				return stats.currentCameraIndex;
			}
			set
			{
				if (HasInternalErrors)
					return;

				stats.currentCameraIndex = math.clamp(value, 0, Settings.Cameras.Length - 1);

				RefreshCurrentCamera();
			}
		}
		public int CurrentPivotIndex
		{
			get
			{
				if (Pivots.Length < 1)
					return -1;

				return stats.currentPivotIndex;
			}
		}
		public bool useSmoothCameraChange = true;
		public bool ignorePivotsSmoothing;
		public float CameraChangeTime
		{
			get
			{
				if (!useSmoothCameraChange)
					return 0f;

				return cameraChangeTime;
			}
			set
			{
				if (!useSmoothCameraChange)
					return;

				cameraChangeTime = math.max(value, math.EPSILON);
			}
		}
		public bool autoSetDimensions;
		public float Distance
		{
			get
			{
				return distance;
			}
			set
			{
				distance = math.max(value, 0f);
			}
		}
		public float Height
		{
			get
			{
				return height;
			}
			set
			{
				height = math.max(value, 0f);
			}
		}
		public float DistanceMultiplier
		{
			get
			{
				return distanceMultiplier;
			}
			set
			{
				distanceMultiplier = math.clamp(value, .5f, 10f);
			}
		}
		public float HeightMultiplier
		{
			get
			{
				return heightMultiplier;
			}
			set
			{
				heightMultiplier = math.clamp(value, .5f, 5f);
			}
		}
		public float IdleHeightMultiplier
		{
			get
			{
				return idleHeightMultiplier;
			}
			set
			{
				idleHeightMultiplier = Utility.Clamp01(value);
			}
		}
		public float IdleMaximumSpeed
		{
			get
			{
				return idleMaximumSpeed;
			}
			set
			{
				idleMaximumSpeed = math.max(value, 1f);
			}
		}
		public float3 LookPointOffset
		{
			get
			{
				return lookPointOffset;
			}
			set
			{
				lookPointOffset = value;
			}
		}
		public Utility.Precision shakePrecision;
		public float2 ShakeIntensity
		{
			get
			{
				if (shakePrecision == Utility.Precision.Simple)
					return Utility.Float2One;

				return shakeIntensity;
			}
			set
			{
				if (shakePrecision == Utility.Precision.Simple)
					return;

				shakeIntensity = math.max(value, 0f);
			}
		}
		public float2 ShakeSpeed
		{
			get
			{
				if (shakePrecision == Utility.Precision.Simple)
					return Utility.Float2One;

				return shakeSpeed;
			}
			set
			{
				if (shakePrecision == Utility.Precision.Simple)
					return;

				shakeSpeed = math.max(value, 0f);
			}
		}
		public float ShakeIntensityMultiplier
		{
			get
			{
				if (shakePrecision == Utility.Precision.Advanced)
					return 1f;

				return shakeIntensityMultiplier;
			}
			set
			{
				if (shakePrecision == Utility.Precision.Advanced)
					return;

				shakeIntensityMultiplier = math.max(value, 0f);
			}
		}
		public float ShakeSpeedMultiplier
		{
			get
			{
				if (shakePrecision == Utility.Precision.Advanced)
					return 1f;

				return shakeSpeedMultiplier;
			}
			set
			{
				if (shakePrecision == Utility.Precision.Advanced)
					return;

				shakeSpeedMultiplier = math.max(value, 0f);
			}
		}
		public float MouseOrbitIntensity
		{
			get
			{
				return mouseOrbitIntensity;
			}
			set
			{
				mouseOrbitIntensity = math.max(value, 0f);
			}
		}
		public float RotationDamping
		{
			get
			{
				return rotationDamping;
			}
			set
			{
				rotationDamping = math.max(value, .1f);
			}
		}
		public float HeightDamping
		{
			get
			{
				return heightDamping;
			}
			set
			{
				heightDamping = math.max(value, .1f);
			}
		}
		public LayerMask ObstaclesMask
		{
			get
			{
				return obstaclesMask;
			}
			set
			{
				obstaclesMask = value;
			}
		}
		public float ObstaclesOffset
		{
			get
			{
				return obstaclesOffset;
			}
			set
			{
				obstaclesOffset = math.max(value, 0f);
			}
		}
		public float ShowcaseDistanceMultiplier
		{
			get
			{
				return showcaseDistanceMultiplier;
			}
			set
			{
				showcaseDistanceMultiplier = math.clamp(value, .5f, 10f);
			}
		}
		public float ShowcaseHeightMultiplier
		{
			get
			{
				return showcaseHeightMultiplier;
			}
			set
			{
				showcaseHeightMultiplier = math.clamp(value, .1f, 5f);
			}
		}
		public float ShowcaseTimeout
		{
			get
			{
				return showcaseTimeout;
			}
			set
			{
				showcaseTimeout = math.max(value, 1f);
			}
		}
		public float ShowcaseSpeed
		{
			get
			{
				return showcaseSpeed;
			}
			set
			{
				showcaseSpeed = math.clamp(value, .01f, 5f);
			}
		}
		public float ShowcaseFieldOfView
		{
			get
			{
				return showcaseFieldOfView;
			}
			set
			{
				showcaseFieldOfView = math.clamp(value, 1f, 179f);
			}
		}
		public float ShowcaseHeightRandomization
		{
			get
			{
				return showcaseHeightRandomization;
			}
			set
			{
				showcaseHeightRandomization = math.clamp(value, 0f, 10f);
			}
		}

		[SerializeField]
		private float weight = 1f;
		[SerializeField]
		private float cameraChangeTime = 1f;
		[SerializeField]
		private float distance = 4f;
		[SerializeField]
		private float distanceMultiplier = 2f;
		[SerializeField]
		private float height = 1.5f;
		[SerializeField]
		private float heightMultiplier = 1.5f;
		[SerializeField]
		private float idleHeightMultiplier = .75f;
		[SerializeField]
		private float idleMaximumSpeed = 30f;
		[SerializeField]
		private Vector3 lookPointOffset;
		[SerializeField]
		private float2 shakeIntensity = Utility.Float2One;
		[SerializeField]
		private float2 shakeSpeed = Utility.Float2One;
		[SerializeField, FormerlySerializedAs("shakeMultiplier")]
		private float shakeIntensityMultiplier = 1f;
		[SerializeField]
		private float shakeSpeedMultiplier = 1f;
		[SerializeField]
		private float mouseOrbitIntensity = 1f;
		[SerializeField]
		private float rotationDamping = 3f;
		[SerializeField]
		private float heightDamping = 2f;
		[SerializeField]
		private LayerMask obstaclesMask = -1;
		[SerializeField]
		private float obstaclesOffset = .05f;
		[SerializeField]
		private float showcaseTimeout = 30f;
		[SerializeField]
		private float showcaseSpeed = 1f;
		[SerializeField]
		private float showcaseDistanceMultiplier = 2f;
		[SerializeField]
		private float showcaseHeightMultiplier = 1f;
		[SerializeField]
		private float showcaseFieldOfView = 15f;
		[SerializeField]
		private float showcaseHeightRandomization;

		#endregion

		#region Sheets & Temp

		public Vehicle VehicleInstance
		{
			get
			{
				if (Manager && vehicleInstance != Manager.PlayerVehicle)
				{
					vehicleInstance = Manager.PlayerVehicle;

					if (Awaken)
						Restart();
				}

				return vehicleInstance;
			}
		}
		public InputsSheet Inputs
		{
			get
			{
				if (!inputs)
					inputs = new(this);

				return inputs;
			}
		}
		public VehicleCamera[] Cameras
		{
			get
			{
				if (HasInternalErrors || !VehicleInstance)
					return null;

				if (cameras == null || cameras.Length < 1 || !camerasAccess.IsCreated || camerasAccess.Length != cameras.Length || !followerPivotsIndexes.IsCreated || followerPivotsIndexes.Length != cameras.Length || !currentRotations.IsCreated || !targetRotations.IsCreated || !targetPositions.IsCreated)
				{
					bool camerasChanged = false;

					if (cameras == null || cameras.Length < 1)
					{
						cameras = new VehicleCamera[] { };

						int firstFollowerIndex = Array.FindIndex(Settings.Cameras, camera => camera.Type == VehicleCamera.CameraType.Follower);

						for (int i = 0; i < Settings.Cameras.Length; i++)
							switch (Settings.Cameras[i].Type)
							{
								case VehicleCamera.CameraType.Pivot:
									if (Pivots == null)
										continue;

									for (int j = 0; j < Pivots.Length; j++)
										if (Pivots[j].CameraIndex == i && Settings.Cameras[i].Type == VehicleCamera.CameraType.Pivot)
										{
											Array.Resize(ref cameras, cameras.Length + 1);

											cameras[^1] = Settings.Cameras[i];
										}

									continue;

								default:
									Array.Resize(ref cameras, cameras.Length + 1);

									cameras[^1] = Settings.Cameras[i];

									continue;
							}

						camerasChanged = true;
					}

					if (camerasAccess.IsCreated)
						camerasAccess.Dispose();

					camerasAccess = new(cameras.Select(camera => (VehicleCameraAccess)camera).ToArray(), Allocator.Persistent);

					if (!followerPivotsIndexes.IsCreated || followerPivotsIndexes.Length != cameras.Length || camerasChanged)
					{
						if (followerPivotsIndexes.IsCreated)
							followerPivotsIndexes.Dispose();

						followerPivotsIndexes = new(camerasAccess.Length, Allocator.Persistent);

						int k = 0;

						for (int i = 0; i < camerasAccess.Length; i++)
							if (camerasAccess[i].isPivotCamera)
							{
								followerPivotsIndexes[i] = k;

								k++;
							}
							else
								followerPivotsIndexes[i] = -1;
					}

					if (currentRotations.IsCreated)
						currentRotations.Dispose();

					if (targetRotations.IsCreated)
						targetRotations.Dispose();

					if (targetPositions.IsCreated)
						targetPositions.Dispose();

					currentRotations = new(cameras.Length, Allocator.Persistent);
					targetRotations = new(cameras.Length, Allocator.Persistent);
					targetPositions = new(cameras.Length, Allocator.Persistent);
				}

				return cameras;
			}
		}
		public VehicleFollowerPivot[] Pivots
		{
			get
			{
				if (!VehicleInstance || !vehicleInstance.Chassis)
					return null;

				return vehicleInstance.Chassis.FollowerPivots;
			}
		}
		public bool Showcasing
		{
			get
			{
				return stats.showcasing;
			}
		}
		public bool IsInsideVehicle
		{
			get
			{
				return Cameras != null && Pivots != null && stats.currentCamera.isPivotCamera && Pivots[stats.currentPivotIndex].IsInsideVehicle;
			}
		}
		public float YOrbitAngle
		{
			get
			{
				return stats.xOrbitAngle + stats.keysOrbitAngle;
			}
		}
		public float ZOrbitAngle
		{
			get
			{
				return stats.mouseYOrbitAngle;
			}
		}

		private StatsAccess stats;
		private InputsAccess inputsAccess;
		private NativeArray<int> followerPivotsIndexes;
		private Camera[] cameraInstances;
		private Vehicle vehicleInstance;
		private InputsSheet inputs;
		private VehicleCamera[] cameras;
		private NativeArray<VehicleFollowerPivotAccess> pivotsAccess;
		private NativeArray<VehicleCameraAccess> camerasAccess;
		private RaycastHit[] obstacleHits;
		private NativeArray<quaternion> targetRotations;
		private NativeArray<quaternion> currentRotations;
		private NativeArray<float3> targetPositions;
		private float3 VehiclePivotPoint
		{
			get
			{
				return new(stats.currentCamera.pivotPoint.x * vehicleInstance.Bounds.extents.x + lookPointOffset.x, 0f, stats.currentCamera.pivotPoint.z * Utility.Lerp(-1f, 1f, math.abs(stats.xOrbitAngle) / 180f) * vehicleInstance.WheelBase * .5f + lookPointOffset.z);
			}
		}
		private float3 VehicleLastPivotPoint
		{
			get
			{
				return new(stats.lastCamera.pivotPoint.x * vehicleInstance.Bounds.extents.x + lookPointOffset.x, 0f, -stats.lastCamera.pivotPoint.z * Utility.Lerp(-1f, 1f, math.abs(stats.xOrbitAngle) / 180f) * vehicleInstance.WheelBase * .5f + lookPointOffset.z);
			}
		}
		private float userFieldOfView;
		private bool setUserFieldOfView;

		#endregion

		#endregion

		#endregion

		#region Methods

		#region Static Methods

		public static VehicleFollower GetOrCreateInstance()
		{
			if (HasInternalErrors)
				return null;

			if (!instance)
				instance = FindAnyObjectByType<VehicleFollower>(FindObjectsInactive.Include);

			if (instance)
				return instance;

			GameObject followerObject = new("VehicleFollower");
			Camera camera = new GameObject("Camera").AddComponent<Camera>();

			camera.transform.SetParent(followerObject.transform, false);

			instance = followerObject.AddComponent<VehicleFollower>();

			AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

			if (listeners.Length > 0)
				instance.gameObject.SetActive(false);

			if (!instance.gameObject.TryGetComponent(out AudioListener listener))
				listener = instance.gameObject.AddComponent<AudioListener>();

			if (listeners.Length > 0)
			{
				listener.enabled = false;

				instance.gameObject.SetActive(true);
			}

			return instance;
		}

		#endregion

		#region Global Methods

		#region Awake

		public void Restart()
		{
			Awaken = false;

			Initialize();

			if (HasInternalErrors || !IsSetupDone || instance && instance != this || !enabled)
				return;

			instance = this;

			cameraInstances = GetComponentsInChildren<Camera>();

			if (cameraInstances == null || cameraInstances.Length < 1 || cameraInstances.Where(camera => camera.transform == transform).Count() > 0)
				return;

			stats.cameraChangeTimer = useSmoothCameraChange ? CameraChangeTime : 0f;

			if (Settings.Problems.DisableToolkitBehaviourOnProblems(ToolkitSettings.SettingsEditorFoldout.PlayerInputs, this))
				return;

			RefreshCurrentCamera();

			if (VehicleInstance && vehicleInstance.isActiveAndEnabled)
			{
				foreach (Camera camera in cameraInstances)
					camera.fieldOfView = math.lerp(camera.fieldOfView, Settings.Cameras[stats.currentCameraIndex].FieldOfViewInterval.Min, weight);

				transform.SetPositionAndRotation(Vector3.LerpUnclamped(transform.position, vehicleInstance.transform.position, weight), Quaternion.LerpUnclamped(transform.rotation, vehicleInstance.transform.rotation, weight));
			}

			stats.vehiclesLayerMask = 1 << Settings.vehiclesLayer;
			obstacleHits = new RaycastHit[30];
			transform.parent = null;
			Awaken = true;
		}

		private void Awake()
		{
			if (Awaken)
				return;

			Restart();
		}
		private void Initialize()
		{
			if (followerPivotsIndexes.IsCreated)
				followerPivotsIndexes.Dispose();

			if (targetRotations.IsCreated)
				targetRotations.Dispose();

			if (currentRotations.IsCreated)
				currentRotations.Dispose();

			if (targetPositions.IsCreated)
				targetPositions.Dispose();

			if (pivotsAccess.IsCreated)
				pivotsAccess.Dispose();

			cameraInstances = null;
			vehicleInstance = null;
			inputs = null;
			cameras = null;
			obstacleHits = null;
			stats = default;
		}

		#endregion

		#region Utilities

		#region Static Methods

		private static int LoopCameraIndex(int index, int length)
		{
			if (index >= length)
				return index - length;
			else if (index < 0)
				return index + length;
			else
				return index;
		}

		#endregion

		#region Global Methods

		public void ApplyCollisionShaking(float2 velocity)
		{
			if (Cameras == null || stats.currentCamera.impactAxes == 0)
				return;

			if (math.lengthsq(velocity) < 1f || math.lengthsq(stats.impactVelocity) > .0625f)
				return;

			stats.impactVelocity = new(velocity.x, velocity.y);

			if (!stats.currentCamera.HasImpactAxis(VehicleCamera.ImpactAxis.HorizontalX))
				stats.impactVelocity.x = 0f;

			if (!stats.currentCamera.HasImpactAxis(VehicleCamera.ImpactAxis.VerticalY))
				stats.impactVelocity.y = 0f;
		}
		public void SetFieldOfView(float angle, bool state)
		{
			userFieldOfView = math.max(angle, 0f);
			setUserFieldOfView = state;
		}
		public void SetFieldOfView(bool state)
		{
			setUserFieldOfView = state;
		}
		public bool GetFieldOfView(out float angle)
		{
			angle = userFieldOfView;

			return GetFieldOfView();
		}
		public bool GetFieldOfView()
		{
			return setUserFieldOfView;
		}
		public void DisposeNativeArrays()
		{
			if (currentRotations.IsCreated)
				currentRotations.Dispose();

			if (targetRotations.IsCreated)
				targetRotations.Dispose();

			if (targetPositions.IsCreated)
				targetPositions.Dispose();

			if (camerasAccess.IsCreated)
				camerasAccess.Dispose();

			if (pivotsAccess.IsCreated)
				pivotsAccess.Dispose();

			if (followerPivotsIndexes.IsCreated)
				followerPivotsIndexes.Dispose();
		}

		private void RefreshCurrentCamera()
		{
			if (Cameras == null || Pivots == null)
				return;

			stats.lastCameraIndex = LoopCameraIndex(stats.currentCameraIndex - 1);
			stats.currentCamera = camerasAccess[stats.currentCameraIndex];
			stats.lastCamera = camerasAccess[stats.lastCameraIndex];
			stats.lastPivotIndex = LoopCameraIndex(stats.currentPivotIndex - 1, Pivots.Length);
		}
		private int LoopCameraIndex(int index)
		{
			if (index >= camerasAccess.Length)
				return index - camerasAccess.Length;
			else if (index < 0)
				return index + camerasAccess.Length;
			else
				return index;
		}

		#endregion

		#endregion

		#region Update

		private void Update()
		{
			if (!Awaken || !VehicleInstance || !vehicleInstance.isActiveAndEnabled)
				return;

			Inputs.Update();

			pivotsAccess = new(Pivots.Select(pivot => (VehicleFollowerPivotAccess)pivot).ToArray(), Allocator.TempJob);
			inputsAccess = Inputs;

			NativeArray<StatsAccess> access = new(1, Allocator.TempJob);

			access[0] = stats;

			UpdateJob job = new()
			{
				pivots = pivotsAccess,
				cameras = camerasAccess,
				stats = access,
				vehicleStats = vehicleInstance.Stats,
				inputs = inputsAccess,
				vehicleTransform = vehicleInstance.transform,
				vehicleExtents = vehicleInstance.Bounds.extents,
				showcaseDistanceMultiplier = showcaseDistanceMultiplier,
				showcaseHeightMultiplier = showcaseHeightMultiplier,
				showcaseHeightRandomization = showcaseHeightRandomization,
				cameraChangeTime = cameraChangeTime,
				showcaseTimeout = showcaseTimeout,
				showcaseSpeed = showcaseSpeed,
				deltaTime = Time.deltaTime,
				time = Time.time,
				ignorePivotsSmoothing = ignorePivotsSmoothing,
				useSmoothCameraChange = useSmoothCameraChange
			};

			job.Schedule().Complete();

			stats = access[0];

			pivotsAccess.Dispose();
			access.Dispose();
		}

		#endregion

		#region Fixed Update

		private void FixedUpdate()
		{
			if (!Awaken || !VehicleInstance || !vehicleInstance.isActiveAndEnabled)
				return;

			stats.lastVehicleLocalVelocity = stats.vehicleLocalVelocity;
			stats.vehicleLocalVelocity = vehicleInstance.Stats.localVelocity;

			float velocityDifference = math.abs(stats.lastVehicleLocalVelocity.y - stats.vehicleLocalVelocity.y);
			float averageDamageVelocity = Utility.Average(Settings.damageLowVelocity, Settings.damageMediumVelocity);

			if (velocityDifference > averageDamageVelocity)
			{
				ApplyCollisionShaking(new(0f, math.clamp(velocityDifference / 8f, 0f, math.min(vehicleInstance.Stats.lastAirTime, 2f))));
				vehicleInstance.sfx.WallImpact(velocityDifference * velocityDifference / 8f, vehicleInstance.transform.position);
			}
		}

		#endregion

		#region Late Update

		private void LateUpdate()
		{
			if (!Awaken || !vehicleInstance || !vehicleInstance.isActiveAndEnabled)
				return;

			Orbit();
			Calculations();
			UpdateCameras();
			ApplyTargets();
			ShakingAndObstaclesDetection();
		}
		private void Calculations()
		{
			pivotsAccess = new(Pivots.Select(pivot => (VehicleFollowerPivotAccess)pivot).ToArray(), Allocator.TempJob);

			bool vehicleTrailerLinkConnected = vehicleInstance.TrailerLink && vehicleInstance.TrailerLink.ConnectedTrailer;
			NativeArray<StatsAccess> access = new(1, Allocator.TempJob);

			access[0] = stats;

			CalculationsJob job = new()
			{
				stats = access,
				cameras = camerasAccess,
				vehicleSteering = vehicleInstance.Steering,
				vehicleInputs = vehicleInstance.Inputs,
				vehicleEngine = vehicleInstance.Engine,
				vehicleStats = vehicleInstance.Stats,
				trailerFollowerModifier = vehicleTrailerLinkConnected ? vehicleInstance.TrailerLink.ConnectedTrailer.FollowerModifier : default,
				vehicleTransform = vehicleInstance.transform,
				vehicleLastPivotPoint = VehicleLastPivotPoint,
				vehiclePivotPoint = VehiclePivotPoint,
				lookPointOffset = LookPointOffset,
				vehicleExtents = vehicleInstance.Bounds.extents,
				randomInsideUnitSphere = UnityEngine.Random.insideUnitSphere,
				shakingSpeed = shakePrecision == Utility.Precision.Advanced ? shakeSpeed : shakeSpeedMultiplier,
				distanceMultiplier = distanceMultiplier,
				distance = distance,
				idleHeightMultiplier = idleHeightMultiplier,
				heightMultiplier = heightMultiplier,
				height = height,
				heightDamping = heightDamping,
				idleMaximumSpeed = idleMaximumSpeed,
				deltaTime = Time.deltaTime,
				time = Time.time,
				vehicleTrailerLinkConnected = vehicleTrailerLinkConnected,
				autoSetDimensions = autoSetDimensions,
				vehicleWheelsCount = vehicleInstance.Wheels.Length,
				vehicleMotorWheelsCount = vehicleInstance.MotorWheels.Length
			};

			job.Schedule().Complete();

			stats = access[0];

			access.Dispose();
		}
		private void Orbit()
		{
			NativeArray<StatsAccess> access = new(1, Allocator.TempJob);

			access[0] = stats;

			OrbitJob job = new()
			{
				stats = access,
				cameras = camerasAccess,
				inputs = inputsAccess,
				mouseOrbitIntensity = mouseOrbitIntensity,
				rotationDamping = rotationDamping,
				heightDamping = heightDamping,
				deltaTime = Time.deltaTime,
				unityLegacyInputManager = Settings.inputSystem == ToolkitSettings.InputSystem.UnityLegacyInputManager
			};

			job.Schedule().Complete();

			stats = access[0];

			access.Dispose();

			AudioListener listener = Listener;

			if (!listener)
				return;

			if (stats.skippingDistantOrbit)
				listener.enabled = false;
			else if (!listener.enabled)
				listener.enabled = true;
		}
		private void UpdateCameras()
		{
			bool vehicleTrailerLinkConnected = vehicleInstance.TrailerLink && vehicleInstance.TrailerLink.ConnectedTrailer;
			UpdateCameraJob job = new()
			{
				trailerFollowerModifier = vehicleTrailerLinkConnected ? vehicleInstance.TrailerLink.ConnectedTrailer.FollowerModifier : default,
				pivots = pivotsAccess,
				cameras = camerasAccess,
				currentRotations = currentRotations,
				targetRotations = targetRotations,
				targetPositions = targetPositions,
				followerPivotsIndexes = followerPivotsIndexes,
				stats = stats,
				vehicleTransform = vehicleInstance.transform,
				rotationDamping = rotationDamping,
				deltaTime = Time.deltaTime,
				vehicleTrailerLinkConnected = vehicleTrailerLinkConnected,
			};

			job.Schedule(camerasAccess.Length, camerasAccess.Length).Complete();
		}
		private void ApplyTargets()
		{
			NativeArray<Utility.TransformAccess> transformAccess = new(1, Allocator.TempJob);

			transformAccess[0] = transform;

			NativeArray<float> camerasFieldOfView = new(cameraInstances.Select(camera => camera.fieldOfView).ToArray(), Allocator.TempJob);
			ApplyTargetsJob job = new()
			{
				transform = transformAccess,
				camerasFieldOfView = camerasFieldOfView,
				currentRotations = currentRotations,
				targetPositions = targetPositions,
				cameras = camerasAccess,
				stats = stats,
				showcaseFieldOfView = ShowcaseFieldOfView,
				setUserFieldOfView = setUserFieldOfView,
				userFieldOfView = userFieldOfView,
				weight = weight,
			};

			job.Schedule().Complete();

			for (int i = 0; i < camerasFieldOfView.Length; i++)
				cameraInstances[i].fieldOfView = camerasFieldOfView[i];

			transform.SetPositionAndRotation(transformAccess[0].position, transformAccess[0].rotation);
			camerasFieldOfView.Dispose();
			transformAccess.Dispose();

			AudioListener listener = Listener;

			if (listener && !listener.enabled)
				listener.enabled = true;
		}
		private void ShakingAndObstaclesDetection()
		{
			NativeArray<Utility.TransformAccess> transforms = new(cameraInstances.Length, Allocator.TempJob);

			for (int i = 0; i < cameraInstances.Length; i++)
				transforms[i] = (Utility.TransformAccess)cameraInstances[i].transform;

			ShakingJob job = new()
			{
				transforms = transforms,
				shakeIntensity = shakePrecision == Utility.Precision.Advanced ? ShakeIntensity : ShakeIntensityMultiplier,
				stats = stats,
				weight = weight,
			};

			job.Schedule(cameraInstances.Length, cameraInstances.Length).Complete();

			if (stats.cameraChangeFactor > 0f && stats.cameraChangeFactor < 1f)
				if ((!stats.currentCamera.isPivotCamera || stats.currentCamera.isPivotCamera && !Pivots[stats.currentPivotIndex].IsInsideVehicle) && (!stats.lastCamera.isPivotCamera || stats.lastCamera.isPivotCamera && !Pivots[stats.lastPivotIndex].IsInsideVehicle))
				{
					float3 raycastStart = /*stats.cameraChangeFactor >= .5f ? targetPositions[stats.currentCameraIndex] : */targetPositions[stats.lastCameraIndex];
					float3 raycastEnd = Utility.Lerp(targetPositions[stats.lastCameraIndex], targetPositions[stats.currentCameraIndex], stats.cameraChangeFactor);

					if (Physics.Raycast(raycastStart, Utility.Direction(raycastStart, raycastEnd), out RaycastHit hit1, Utility.Distance(raycastStart, raycastEnd), stats.vehiclesLayerMask, QueryTriggerInteraction.Ignore))
						if (hit1.transform.IsChildOf(vehicleInstance.transform))
							if (Physics.Raycast((Vector3)raycastEnd + vehicleInstance.transform.up * vehicleInstance.Bounds.size.y, -vehicleInstance.transform.up, out RaycastHit hit2, vehicleInstance.Bounds.size.y, stats.vehiclesLayerMask, QueryTriggerInteraction.Ignore))
								if (hit2.transform.IsChildOf(vehicleInstance.transform))
								{
									float3 localPosition = Utility.PointWorldToLocal(hit2.point + vehicleInstance.transform.up * obstaclesOffset, base.transform.position, base.transform.rotation, base.transform.lossyScale);

									for (int i = 0; i < transforms.Length; i++)
									{
										Utility.TransformAccess t = transforms[i];

										t.localPosition += localPosition;
										transforms[i] = t;
									}
								}
				}

			if (stats.currentCamera.HasFollowerFeature(VehicleCamera.FollowerFeature.ObstaclesDetection) || stats.currentCamera.HasPivotFeature(VehicleCamera.PivotFeature.ObstaclesDetection))
			{
				stats.obstaclesCenter = stats.currentCamera.isPivotCamera ? (float3)vehicleInstance.Chassis.transform.TransformPoint(vehicleInstance.ChassisBounds.center) : stats.lookPoint;
				stats.obstaclesDistance = Utility.Distance(stats.obstaclesCenter, (float3)base.transform.position);
				stats.obstacleHitsCount = Physics.RaycastNonAlloc(stats.obstaclesCenter, Utility.Direction(stats.obstaclesCenter, (float3)base.transform.position), obstacleHits, stats.obstaclesDistance, obstaclesMask, QueryTriggerInteraction.Ignore);

				if (stats.obstacleHitsCount > 0)
				{
					bool hasTrailer = vehicleInstance.TrailerLink && vehicleInstance.TrailerLink.ConnectedTrailer;
					NativeArray<bool> obstacleIsChildOfTrailer = new(hasTrailer ? stats.obstacleHitsCount : 0, Allocator.TempJob);
					NativeArray<bool> obstacleIsChildOfVehicle = new(stats.obstacleHitsCount, Allocator.TempJob);
					NativeArray<float> obstacleDistances = new(stats.obstacleHitsCount, Allocator.TempJob);

					for (int i = 0; i < stats.obstacleHitsCount; i++)
					{
						if (hasTrailer)
							obstacleIsChildOfTrailer[i] = obstacleHits[i].transform.IsChildOf(vehicleInstance.TrailerLink.ConnectedTrailer.transform);

						obstacleIsChildOfVehicle[i] = obstacleHits[i].transform.IsChildOf(vehicleInstance.transform);
						obstacleDistances[i] = obstacleHits[i].distance;
					}

					NativeArray<StatsAccess> access = new(1, Allocator.TempJob);

					access[0] = stats;

					ObstaclesDetectionJob obstaclesDetectionJob = new()
					{
						obstacleIsChildOfVehicle = obstacleIsChildOfVehicle,
						obstacleIsChildOfTrailer = obstacleIsChildOfTrailer,
						obstacleDistances = obstacleDistances,
						obstaclesOffset = obstaclesOffset,
						hasTrailer = hasTrailer,
						transforms = transforms,
						stats = access,
						weight = weight,
					};

					obstaclesDetectionJob.Schedule().Complete();

					stats = access[0];

					obstacleIsChildOfTrailer.Dispose();
					obstacleIsChildOfVehicle.Dispose();
					obstacleDistances.Dispose();
					access.Dispose();
				}
			}

			for (int i = 0; i < transforms.Length; i++)
			{
				if (Physics.Raycast(base.transform.TransformPoint(transforms[i].localPosition) + base.transform.up * 1f, -base.transform.up, out RaycastHit hit, 1f, obstaclesMask, QueryTriggerInteraction.Ignore))
				{
					float3 hitPoint = base.transform.InverseTransformPoint(hit.point);
					Utility.TransformAccess transformAccess = transforms[i];

					transformAccess.localPosition.y = math.max(transformAccess.localPosition.y, hitPoint.y + obstaclesOffset);
					transforms[i] = transformAccess;
				}

				cameraInstances[i].transform.localPosition = transforms[i].localPosition;
			}

			pivotsAccess.Dispose();
			transforms.Dispose();
		}

		#endregion

		#region Enable, Disable & Destroy

		private void OnEnable()
		{
			Awake();
		}
		private void OnDisable()
		{
			if (instance == this)
				instance = null;
		}
		private void OnDestroy()
		{
			OnDisable();
			DisposeNativeArrays();
		}

		#endregion

		#endregion

		#endregion
	}
}

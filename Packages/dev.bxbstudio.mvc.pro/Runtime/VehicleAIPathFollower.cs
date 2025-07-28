#region Namespaces

using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
using MVC.Core;
using MVC.Utilities.Internal;

#endregion

namespace MVC.AI
{
	[DisallowMultipleComponent]
	public sealed class VehicleAIPathFollower : VehicleAIBehaviour
	{
		#region Constants

		public const int ObstacleRaycastSensorsCount = 5;

		#endregion

		#region Enumerators

		public enum StartPoint { PathStartPoint, ClosestPoint }
		public enum FollowDirection { Default, Inverse }
		public enum InputInterpolation { Instant, Smooth }
		public enum ObstacleDetectionMethod { Raycasts/*, ConeMeshBased, Lidar*/ }

		#endregion

		#region Variables

		#region Properties

		public override string[] Issues
		{
			get
			{
				List<string> messages = new();

#if !MVC_COMMUNITY
				if (!path)
					messages.Add("The path field is empty. A valid path is required!");
				else if (!path.IsValid)
					messages.Add("The assigned AI Path has some issues that need to be fixed.");

				if (messages.Count > 0)
					messages.Add("This AI behaviour will be disabled at runtime!");
#endif

				return messages.ToArray();
			}
		}
		public int PathCurrentPointIndex =>
#if MVC_COMMUNITY
			-1;
#else
			pathCurrentPointIndex;
#endif
		public int PathStartPointIndex =>
#if MVC_COMMUNITY
			-1;
#else
			pathStartPointIndex;
#endif

#if !MVC_COMMUNITY
		private List<VehicleAIZone> handbrakeZones;
		private List<VehicleAIZone> brakeZones;
		private List<VehicleAIZone> nosZones;
		private Vehicle vehicleInstance;
		private VehicleAIPath lastPath;
		private FollowDirection lastFollowDirection;
		private VehicleAIPath.SpacedPathPoint currentPathPoint;
		private float3 currentPathPointPosition;
		private float3 vehiclePivotPoint;
		private float3 vehicleCenter;
		private float3 vehicleExtents;
		private float vehicleAverageBrakeTorque;
		private float vehicleMinPower;
		private float vehicleCurbWeight;
		private float vehicleTopVelocity;
		private float vehicleReverseTimer;
		private float vehicleReversingTimer;
		private float normalSteeringInput;
		private float sensorsSteeringInputIntensity;
		private float sensorsSteeringInput;
		private float sensorsBrakeInput;
		private float zonesBrakeInput;
		private float zonesHandbrakeInput;
		private int vehicleReverseIterationCount;
		private RaycastCommand frontRaycastSensorCommand;
		private RaycastCommand frontLeftRaycastSensorCommand;
		private RaycastCommand frontRightRaycastSensorCommand;
		private RaycastCommand leftRaycastSensorCommand;
		private RaycastCommand rightRaycastSensorCommand;
		private RaycastHit frontRaycastSensorHit;
		private RaycastHit frontLeftRaycastSensorHit;
		private RaycastHit frontRightRaycastSensorHit;
		private RaycastHit leftRaycastSensorHit;
		private RaycastHit rightRaycastSensorHit;
		private Quaternion frontSensorsRotation;
		private bool frontRaycastSensorHasHit;
		private bool frontLeftRaycastSensorHasHit;
		private bool frontRightRaycastSensorHasHit;
		private bool leftRaycastSensorHasHit;
		private bool rightRaycastSensorHasHit;
		private bool zonesNOSInput;
		private int pathCurrentPointIndex;
		private int pathStartPointIndex;
		private float gravity;
#endif

		#endregion

		#region Fields

		[Header("Path")]
		public VehicleAIPath path;
		[Range(0f, 1f)]
		public float pathWidthMultiplier = .5f;
		public FollowDirection followDirection;
		public StartPoint startPoint;
		public InputInterpolation inputInterpolation = InputInterpolation.Smooth;
		[Range(.1f, 3f)]
		public float targetSpeedMultiplier = 1f;
		[Header("Obstacle Sensors")]
		public ObstacleDetectionMethod obstacleDetectionMethod;
		[Min(1)]
		public int sensorsMaximumHits = 3;
		public LayerMask obstaclesLayerMask = -1;
		[Min(.1f)]
		public float sensorsIntensityPower = 3f;
		[Range(0f, 1f)]
		public float diagonalSensorsSteerIntensity = .25f;
		[Range(0f, 1f)]
		public float diagonalSensorsDynamicSteerIntensity = .25f;
		[Range(0f, 1f)]
		public float sideSensorsSteerIntensity = .125f;

		#endregion

		#endregion

		#region Methods

		#region Start

		public override void OnStart()
		{
#if !MVC_COMMUNITY
			vehicleInstance = Base;

			if (!path || !path.IsValid)
				return;

			lastFollowDirection = followDirection;
			handbrakeZones = new();
			brakeZones = new();
			nosZones = new();
			lastPath = path;

			RefreshCoefficients();

			pathCurrentPointIndex = pathStartPointIndex;
#endif
		}

		#endregion

		#region Update

		public override void OnUpdate(ref Vehicle.InputsAccess inputs)
		{
#if !MVC_COMMUNITY
			if (vehicleInstance.ActiveAIBehaviour == this && (!path || !path.IsValid))
			{
				ToolkitDebug.Warning("The AI Path Follower has an invalid path, therefore it has been disabled!");

				vehicleInstance.ActiveAIBehaviourIndex = -1;

				return;
			}
			else if (path != lastPath || followDirection != lastFollowDirection)
				OnStart();

			FindPathPoint();
			ObstacleDetection();
			Zones();
			Gearbox(inputs);
			Steering(ref inputs);
			Inputs(ref inputs);
#endif
		}
		public void RefreshCoefficients()
		{
#if !MVC_COMMUNITY
			var vehicleBounds = vehicleInstance.Bounds;
			var vehicleBehaviour = vehicleInstance.Behaviour;
			float vehicleFrontBrakeTorque = vehicleBehaviour.FrontBrakes.BrakeTorque;
			float vehicleRearBrakeTorque = vehicleBehaviour.RearBrakes.BrakeTorque;
			int vehicleFrontWheelsCount = vehicleInstance.FrontWheels.Length;
			int vehicleRearWheelsCount = vehicleInstance.RearWheels.Length;
			int vehicleWheelsCount = vehicleFrontWheelsCount + vehicleRearWheelsCount;

			vehicleAverageBrakeTorque = (vehicleFrontBrakeTorque * vehicleFrontWheelsCount + vehicleRearBrakeTorque * vehicleRearWheelsCount) / vehicleWheelsCount;
			vehicleMinPower = vehicleBehaviour.PowerCurve.Evaluate(vehicleBehaviour.Engine.MinimumRPM);
			vehiclePivotPoint = vehicleInstance.FrontWheelsPosition;
			vehicleTopVelocity = vehicleInstance.TopSpeed / 3.6f;
			vehicleCurbWeight = vehicleBehaviour.CurbWeight;
			vehicleExtents = vehicleBounds.extents;
			vehicleCenter = vehicleBounds.center;
			vehicleReverseIterationCount = 1;

			pathStartPointIndex = startPoint switch
			{
				StartPoint.PathStartPoint => path.StartPointIndex + (followDirection == FollowDirection.Inverse ? path.SpacedPointsCount - 1 : 0),
				_ => path.ClosestSpacedPointIndex(vehiclePivotPoint),
			};

			gravity = Physics.gravity.magnitude;
#endif
		}

#if !MVC_COMMUNITY
		private void FindPathPoint()
		{
			vehiclePivotPoint = vehicleInstance.FrontWheelsPosition;

			var settings = Settings;
			var vehicleStats = vehicleInstance.Stats;
			var pathPointDistanceInterval = settings.AIPathPointDistanceInterval;
			float targetPathPointDistance = settings.useDynamicAIPathPointDistance ? pathPointDistanceInterval.Lerp(pathPointDistanceInterval.InverseLerp(vehicleStats.brakingDistance, false)) : settings.AIPathPointDistance;
			var tempPoint = path.GetSpacedPoint(pathCurrentPointIndex);
			int newPathPointIndex = pathCurrentPointIndex;
			int pathPointsCount = path.SpacedPointsCount;
			bool searchReachedEndPath = false;

			bool IsCloseToPoint()
			{
				float3 closestPointForward = tempPoint.spacedCurvePoint.curvePoint.forward;
				float targetPathPointDistanceSqr = targetPathPointDistance * targetPathPointDistance;
				float3 closestPointPosition = tempPoint.spacedCurvePoint.curvePoint.ClosestPoint(vehiclePivotPoint, pathWidthMultiplier);
				float pointForwardDistanceSqr = math.distancesq(closestPointForward * vehiclePivotPoint, closestPointForward * closestPointPosition);

				return pointForwardDistanceSqr <= targetPathPointDistanceSqr;
			}

			if (followDirection == FollowDirection.Inverse)
				while ((!searchReachedEndPath || newPathPointIndex != pathCurrentPointIndex) && IsCloseToPoint())
				{
					bool reachedPathEnd = newPathPointIndex <= pathStartPointIndex - pathPointsCount + 1;

					if (path.LoopedPath || !reachedPathEnd)
						newPathPointIndex--;
					else if (reachedPathEnd)
						break;

					if (newPathPointIndex <= pathStartPointIndex - pathPointsCount)
					{
						newPathPointIndex += pathPointsCount;
						searchReachedEndPath = true;
					}

					tempPoint = path.GetSpacedPoint(newPathPointIndex);
				}
			else
				while ((!searchReachedEndPath || newPathPointIndex != pathCurrentPointIndex) && IsCloseToPoint())
				{
					bool reachedPathEnd = newPathPointIndex + 1 >= pathStartPointIndex + pathPointsCount;

					if (path.LoopedPath || !reachedPathEnd)
						newPathPointIndex++;
					else if (reachedPathEnd)
						break;

					if (newPathPointIndex >= pathStartPointIndex + pathPointsCount)
					{
						newPathPointIndex -= pathPointsCount;
						searchReachedEndPath = true;
					}

					tempPoint = path.GetSpacedPoint(newPathPointIndex);
				}

			currentPathPoint = path.GetSpacedPoint(newPathPointIndex);

			float3 newCurrentPathPointPosition = currentPathPoint.spacedCurvePoint.curvePoint.ClosestPoint(vehiclePivotPoint, pathWidthMultiplier);

			if (newCurrentPathPointPosition.Equals(float3.zero))
				currentPathPointPosition = newCurrentPathPointPosition;
			else
				currentPathPointPosition = Utility.Average(currentPathPointPosition, newCurrentPathPointPosition);

			pathCurrentPointIndex = newPathPointIndex;
		}
		private void ObstacleDetection()
		{
			switch (obstacleDetectionMethod)
			{
				default:
					ObstacleRaycastDetection();

					break;
			}
		}
		private void ObstacleRaycastDetection()
		{
			#region Raycasting

			var vehicleStats = vehicleInstance.Stats;
			float frontLength = math.max(vehicleStats.currentSpeed, vehicleExtents.x);
			float adjacent = math.sqrt(frontLength * frontLength - vehicleExtents.x * vehicleExtents.x);
			float diagonalSensorsAngle = math.degrees(math.atan(vehicleExtents.x / adjacent)) * .5f;
			float frontDiagonalAngleFactor = diagonalSensorsAngle / 45f;
			NativeArray<RaycastCommand> raycastCommands = new(ObstacleRaycastSensorsCount, Allocator.TempJob);
			Vector3 center = vehicleInstance.transform.TransformPoint(vehicleCenter);
			Vector3 forwardDirection = transform.forward;
			Vector3 upDirection = transform.up;
			Vector3 frontLeftDirection = Quaternion.Euler(-diagonalSensorsAngle * upDirection) * forwardDirection;
			Vector3 frontRightDirection = Quaternion.Euler(diagonalSensorsAngle * upDirection) * forwardDirection;
			Vector3 rightDirection = transform.right;
			Vector3 frontSource = center + vehicleExtents.z * forwardDirection;
			Vector3 rightExtent = vehicleExtents.x * rightDirection;
			Vector3 frontLeftSource = frontSource - rightExtent;
			Vector3 frontRightSource = frontSource + rightExtent;
			Vector3 leftSource = center - rightExtent;
			Vector3 rightSource = center + rightExtent;
#if UNITY_2022_2_OR_NEWER
			QueryParameters queryParameters = QueryParameters.Default;
			
			queryParameters.layerMask = obstaclesLayerMask;
			queryParameters.hitTriggers = QueryTriggerInteraction.Ignore;
#else
			bool orgQueriesHitTriggers = Physics.queriesHitTriggers;

			Physics.queriesHitTriggers = false;
#endif

			frontSensorsRotation = Quaternion.Euler(0f, math.clamp(normalSteeringInput, -1f, 1f) * vehicleStats.maximumSteerAngle * (1f - frontDiagonalAngleFactor), 0f);
			raycastCommands[0] = frontRaycastSensorCommand = new(frontSource, frontSensorsRotation * forwardDirection,
#if UNITY_2022_2_OR_NEWER
				queryParameters, frontLength
#else
				frontLength, obstaclesLayerMask, sensorsMaximumHits
#endif
				);
			raycastCommands[1] = frontLeftRaycastSensorCommand = new(frontLeftSource, frontSensorsRotation * frontLeftDirection,
#if UNITY_2022_2_OR_NEWER
				queryParameters, frontLength
#else
				frontLength, obstaclesLayerMask, sensorsMaximumHits
#endif
				);
			raycastCommands[2] = frontRightRaycastSensorCommand = new(frontRightSource, frontSensorsRotation * frontRightDirection,
#if UNITY_2022_2_OR_NEWER
				queryParameters, frontLength
#else
				frontLength, obstaclesLayerMask, sensorsMaximumHits
#endif
				);
			raycastCommands[3] = leftRaycastSensorCommand = new(leftSource, -rightDirection,
#if UNITY_2022_2_OR_NEWER
				queryParameters, vehicleExtents.x
#else
				vehicleExtents.x, obstaclesLayerMask, sensorsMaximumHits
#endif
				);
			raycastCommands[4] = rightRaycastSensorCommand = new(rightSource, rightDirection,
#if UNITY_2022_2_OR_NEWER
				queryParameters, vehicleExtents.x
#else
				vehicleExtents.x, obstaclesLayerMask, sensorsMaximumHits
#endif
				);

			NativeArray<RaycastHit> raycastHits = new(ObstacleRaycastSensorsCount * sensorsMaximumHits, Allocator.TempJob);

			RaycastCommand.ScheduleBatch(raycastCommands, raycastHits, raycastCommands.Length).Complete();

#if !UNITY_2022_2_OR_NEWER
			Physics.queriesHitTriggers = orgQueriesHitTriggers;

#endif
			void CheckAndAssignRaycastHit(ref RaycastHit hit, ref bool hasHit, RaycastHit newHit)
			{
				Collider hitCollider = newHit.collider;

				if (!hitCollider)
					return;
				else if (newHit.transform.IsChildOf(transform))
					return;
				else if (hasHit && hit.distance < newHit.distance)
					return;

				hit = newHit;
				hasHit = true;
			}

			frontRaycastSensorHasHit = false;
			frontLeftRaycastSensorHasHit = false;
			frontRightRaycastSensorHasHit = false;
			leftRaycastSensorHasHit = false;
			rightRaycastSensorHasHit = false;

			for (int i = 0; i < sensorsMaximumHits; i++)
			{
				CheckAndAssignRaycastHit(ref frontRaycastSensorHit, ref frontRaycastSensorHasHit, raycastHits[i]);
				CheckAndAssignRaycastHit(ref frontLeftRaycastSensorHit, ref frontLeftRaycastSensorHasHit, raycastHits[sensorsMaximumHits + i]);
				CheckAndAssignRaycastHit(ref frontRightRaycastSensorHit, ref frontRightRaycastSensorHasHit, raycastHits[sensorsMaximumHits * 2 + i]);
				CheckAndAssignRaycastHit(ref leftRaycastSensorHit, ref leftRaycastSensorHasHit, raycastHits[sensorsMaximumHits * 3 + i]);
				CheckAndAssignRaycastHit(ref rightRaycastSensorHit, ref rightRaycastSensorHasHit, raycastHits[sensorsMaximumHits * 4 + i]);
			}

			raycastCommands.Dispose();
			raycastHits.Dispose();

			#endregion

			#region Inputs

			sensorsSteeringInputIntensity = sensorsSteeringInput = sensorsBrakeInput = 0f;

			if (frontRaycastSensorHasHit)
			{
				float distanceFactor = 1f - (frontRaycastSensorHit.distance / frontLength);
				bool dynamicHit = frontRaycastSensorHit.collider.attachedRigidbody;

				if (!dynamicHit)
					sensorsBrakeInput += distanceFactor;
			}

			float frontLeftDistanceFactor = 0f;
			float frontLeftSteeringInput = 0f;

			if (frontLeftRaycastSensorHasHit)
			{
				bool dynamicHit = frontLeftRaycastSensorHit.collider.attachedRigidbody;

				frontLeftDistanceFactor = 1f - (frontLeftRaycastSensorHit.distance / frontLength);

				float steerIntensity = math.lerp(dynamicHit ? diagonalSensorsDynamicSteerIntensity : diagonalSensorsSteerIntensity, sideSensorsSteerIntensity, frontDiagonalAngleFactor);

				frontLeftSteeringInput += frontLeftDistanceFactor * steerIntensity;
				sensorsSteeringInputIntensity += math.pow(frontLeftDistanceFactor, sensorsIntensityPower);
			}

			float frontRightDistanceFactor = 0f;
			float frontRightSteeringInput = 0f;

			if (frontRightRaycastSensorHasHit)
			{
				bool dynamicHit = frontRightRaycastSensorHit.collider.attachedRigidbody;

				frontRightDistanceFactor = 1f - (frontRightRaycastSensorHit.distance / frontLength);

				float steerIntensity = math.lerp(dynamicHit ? diagonalSensorsDynamicSteerIntensity : diagonalSensorsSteerIntensity, sideSensorsSteerIntensity, frontDiagonalAngleFactor);

				frontRightSteeringInput -= frontRightDistanceFactor * steerIntensity;
				sensorsSteeringInputIntensity += math.pow(frontRightDistanceFactor, sensorsIntensityPower);
			}

			float smoothDiagonalSteerInput = Utility.Lerp(frontLeftSteeringInput, frontRightSteeringInput, (1f + frontRightDistanceFactor - frontLeftDistanceFactor) * .5f);
			float strictDiagonalSteerInput = math.select(frontLeftSteeringInput, frontRightSteeringInput, frontRightDistanceFactor > frontLeftDistanceFactor);

			sensorsSteeringInput += math.lerp(strictDiagonalSteerInput, smoothDiagonalSteerInput, diagonalSensorsAngle / 45f);

			if (leftRaycastSensorHasHit)
			{
				float distanceFactor = 1f - (leftRaycastSensorHit.distance / vehicleExtents.x);

				sensorsSteeringInput += distanceFactor * sideSensorsSteerIntensity;
				sensorsSteeringInputIntensity += math.pow(distanceFactor, sensorsIntensityPower);
			}

			if (rightRaycastSensorHasHit)
			{
				float distanceFactor = 1f - (rightRaycastSensorHit.distance / vehicleExtents.x);

				sensorsSteeringInput -= distanceFactor * sideSensorsSteerIntensity;
				sensorsSteeringInputIntensity += math.pow(distanceFactor, sensorsIntensityPower);
			}

			#endregion
		}
		private void Zones()
		{
			Bounds vehicleBounds = vehicleInstance.WorldBounds;
			int handbrakeZoneIndex = -1;
			int brakeZoneIndex = -1;
			int nosZoneIndex = -1;

			for (int i = 0; i < handbrakeZones.Count; i++)
			{
				var zone = handbrakeZones[i];

				if (zone.collider.bounds.Intersects(vehicleBounds))
				{
					handbrakeZoneIndex = i;

					break;
				}
			}

			for (int i = 0; i < brakeZones.Count; i++)
			{
				var zone = brakeZones[i];

				if (zone.collider.bounds.Intersects(vehicleBounds))
				{
					brakeZoneIndex = i;

					break;
				}
			}

			for (int i = 0; i < nosZones.Count; i++)
			{
				var zone = nosZones[i];

				if (zone.collider.bounds.Intersects(vehicleBounds))
				{
					nosZoneIndex = i;

					break;
				}
			}

			if (handbrakeZoneIndex > -1)
			{
				var zone = handbrakeZones[handbrakeZoneIndex];

				zonesHandbrakeInput = vehicleInstance.Stats.averageRearWheelsForwardSlip < zone.HandbrakeSlipTarget ? zone.BrakingIntensity : 0f;
			}
			else
				zonesHandbrakeInput = default;

			if (brakeZoneIndex > -1)
			{
				var zone = brakeZones[brakeZoneIndex];

				zonesBrakeInput = vehicleInstance.Stats.currentSpeed > zone.BrakeSpeedTarget ? zone.BrakingIntensity : 0f;
			}
			else
				zonesBrakeInput = default;

			if (nosZoneIndex > -1 && brakeZoneIndex < 0 && handbrakeZoneIndex < 0)
			{
				var zone = nosZones[nosZoneIndex];

				zonesNOSInput = vehicleInstance.Stats.NOS > zone.MinimumNOSTarget;
			}
			else
				zonesNOSInput = false;
		}
		private void Gearbox(Vehicle.InputsAccess inputs)
		{
			var vehicleStats = vehicleInstance.Stats;

			if (vehicleStats.isChangingGear)
				return;

			if (inputs.Direction > -1 && math.round(vehicleStats.currentSpeed) < 5f)
				vehicleReverseTimer += Utility.DeltaTime;
			else if (inputs.Direction < 0)
				vehicleReversingTimer += Utility.DeltaTime;
			else if (vehicleReverseTimer > 0f || vehicleReversingTimer > 0f || vehicleReverseIterationCount != 1)
			{
				vehicleReverseTimer = vehicleReversingTimer = default;
				vehicleReverseIterationCount = 1;
			}

			var settings = Settings;

			if (vehicleReverseTimer >= settings.AIReverseTimeout * vehicleReverseIterationCount)
			{
				vehicleInstance.RequestGearShiftToReverse();

				vehicleReverseTimer = default;
			}
			else if (vehicleReversingTimer >= settings.AIReverseTimeSpan * vehicleReverseIterationCount)
			{
				vehicleInstance.RequestGearShiftToForward();

				vehicleReversingTimer = default;
				vehicleReverseIterationCount++;
			}
			else if (vehicleStats.isNeutral)
				vehicleInstance.RequestGearShiftUp();
		}
		private void Steering(ref Vehicle.InputsAccess inputs)
		{
			Vector3 relativeDirection = Utility.Direction(vehiclePivotPoint, currentPathPointPosition);

			normalSteeringInput = Vector3.SignedAngle(transform.forward, relativeDirection, transform.up) / vehicleInstance.Stats.maximumSteerAngle;
			normalSteeringInput *= Mathf.Sign(inputs.Direction);
			inputs.SteeringWheel = math.clamp(math.lerp(normalSteeringInput, sensorsSteeringInput, Utility.Clamp01(sensorsSteeringInputIntensity) - Utility.Clamp01(-inputs.Direction)), -1f, 1f);
		}
		private void Inputs(ref Vehicle.InputsAccess inputs)
		{
			var vehicleStats = vehicleInstance.Stats;
			float vehicleVelocity = vehicleStats.currentSpeed / 3.6f;
			float currentPathPointDistance = math.distance(currentPathPointPosition, vehiclePivotPoint);
			float vehicleBrakeFriction = Vehicle.StatsAccess.EvaluateBrakeTorqueFriction(vehicleInstance, vehicleAverageBrakeTorque);
			float targetPathPointVelocity = math.min(currentPathPoint.spacedCurvePoint.curvePoint.TargetVelocity(math.min(vehicleStats.averageWheelsForwardFrictionStiffness, vehicleBrakeFriction), gravity) * targetSpeedMultiplier, vehicleTopVelocity);
			float desiredAcceleration = (targetPathPointVelocity * targetPathPointVelocity - vehicleVelocity * vehicleVelocity) / (2f * currentPathPointDistance);
			float desiredVelocity = targetPathPointVelocity - vehicleVelocity;
			float maximumVelocity = math.max(vehicleStats.enginePower, vehicleMinPower) * 745.7f / vehicleCurbWeight / math.max(vehicleVelocity, 1f);
			float smoothnessDelta = inputInterpolation == InputInterpolation.Smooth ? Utility.DeltaTime * 10f : 1f;
			float pointOverheadModifier = Utility.InverseLerp(1f, 4f, math.abs(normalSteeringInput)) * Utility.Clamp01(vehicleVelocity / targetPathPointVelocity);
			float NOSInputModifier = Utility.BoolToNumber(zonesNOSInput);

			inputs.FuelPedal = math.lerp(inputs.FuelPedal, Utility.Lerp(1f, Utility.Clamp01(Utility.Clamp01(math.max(desiredVelocity, desiredAcceleration) / maximumVelocity) - sensorsBrakeInput - zonesBrakeInput - pointOverheadModifier + NOSInputModifier), vehicleVelocity / 10f), smoothnessDelta);
			inputs.BrakePedal = math.lerp(inputs.BrakePedal, Utility.Lerp(0f, Utility.Clamp01(Utility.Clamp01(math.max(-desiredAcceleration / maximumVelocity, sensorsBrakeInput + zonesBrakeInput)) - NOSInputModifier + pointOverheadModifier), vehicleVelocity / 10f), smoothnessDelta);
			inputs.Handbrake = Utility.Clamp01(zonesHandbrakeInput - NOSInputModifier);
			inputs.NOS = zonesNOSInput;

			if (vehicleInstance.ActiveAIBehaviour != this)
				return;
		}
		private void OnDrawGizmos()
		{
			if (!Application.isPlaying)
				return;

			Gizmos.color = Settings.AIPathAnchorGizmoColor;

			Gizmos.DrawCube(currentPathPointPosition, 1f / 3f * Vector3.one);

			if (frontRaycastSensorHasHit)
			{
				Gizmos.color = Settings.AIObstaclesSensorActiveGizmoColor;

				Gizmos.DrawLine(frontRaycastSensorCommand.from, frontRaycastSensorHit.point);
			}
			else
			{
				Gizmos.color = Settings.AIObstaclesSensorGizmoColor;

				Gizmos.DrawRay(frontRaycastSensorCommand.from, frontRaycastSensorCommand.distance * frontRaycastSensorCommand.direction);
			}

			if (frontLeftRaycastSensorHasHit)
			{
				Gizmos.color = Settings.AIObstaclesSensorActiveGizmoColor;

				Gizmos.DrawLine(frontLeftRaycastSensorCommand.from, frontLeftRaycastSensorHit.point);
			}
			else
			{
				Gizmos.color = Settings.AIObstaclesSensorGizmoColor;

				Gizmos.DrawRay(frontLeftRaycastSensorCommand.from, frontLeftRaycastSensorCommand.distance * frontLeftRaycastSensorCommand.direction);
			}

			if (frontRightRaycastSensorHasHit)
			{
				Gizmos.color = Settings.AIObstaclesSensorActiveGizmoColor;

				Gizmos.DrawLine(frontRightRaycastSensorCommand.from, frontRightRaycastSensorHit.point);
			}
			else
			{
				Gizmos.color = Settings.AIObstaclesSensorGizmoColor;

				Gizmos.DrawRay(frontRightRaycastSensorCommand.from, frontRightRaycastSensorCommand.distance * frontRightRaycastSensorCommand.direction);
			}

			if (leftRaycastSensorHasHit)
			{
				Gizmos.color = Settings.AIObstaclesSensorActiveGizmoColor;

				Gizmos.DrawLine(leftRaycastSensorCommand.from, leftRaycastSensorHit.point);
			}
			else
			{
				Gizmos.color = Settings.AIObstaclesSensorGizmoColor;

				Gizmos.DrawRay(leftRaycastSensorCommand.from, leftRaycastSensorCommand.distance * leftRaycastSensorCommand.direction);
			}

			if (rightRaycastSensorHasHit)
			{
				Gizmos.color = Settings.AIObstaclesSensorActiveGizmoColor;

				Gizmos.DrawLine(rightRaycastSensorCommand.from, rightRaycastSensorHit.point);
			}
			else
			{
				Gizmos.color = Settings.AIObstaclesSensorGizmoColor;

				Gizmos.DrawRay(rightRaycastSensorCommand.from, rightRaycastSensorCommand.distance * rightRaycastSensorCommand.direction);
			}
		}
#endif

		#endregion

		#region Collisions & Triggers

#if !MVC_COMMUNITY
		private void OnTriggerEnter(Collider other)
		{
			if (!other.TryGetComponent(out VehicleAIZone zone))
				return;

			switch (zone.zoneType)
			{
				case VehicleAIZone.AIZoneType.Brake:
					if (brakeZones.IndexOf(zone) < 0)
						brakeZones.Add(zone);

					break;

				case VehicleAIZone.AIZoneType.Handbrake:
					if (handbrakeZones.IndexOf(zone) < 0)
						handbrakeZones.Add(zone);

					break;

				case VehicleAIZone.AIZoneType.NOS:
					if (nosZones.IndexOf(zone) < 0)
						nosZones.Add(zone);

					break;
			}
		}
		private void OnTriggerExit(Collider other)
		{
			if (!other.TryGetComponent(out VehicleAIZone zone))
				return;

			switch (zone.zoneType)
			{
				case VehicleAIZone.AIZoneType.Brake:
					brakeZones.Remove(zone);

					break;

				case VehicleAIZone.AIZoneType.Handbrake:
					handbrakeZones.Remove(zone);

					break;

				case VehicleAIZone.AIZoneType.NOS:
					nosZones.Remove(zone);

					break;
			}
		}
#endif

		#endregion

		#endregion
	}
}

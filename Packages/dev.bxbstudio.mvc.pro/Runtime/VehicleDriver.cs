#region Namespaces

using System;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using MVC.Core;
using MVC.Internal;
using MVC.Utilities.Internal;

#endregion

namespace MVC.IK
{
	[AddComponentMenu("Multiversal Vehicle Controller/IK/Driver")]
	[DisallowMultipleComponent]
	[ExecuteInEditMode]
	[DefaultExecutionOrder(30)]
	public class VehicleDriver : ToolkitBehaviour
	{
		#region Modules

		#region Components

		[Serializable]
		public class AnimationModule
		{
			#region Variables

			public Animator Animator
			{
				get
				{
					if (!controller)
					{
						controller = driver.GetComponent<Animator>();

						if (!controller)
							controller = driver.gameObject.AddComponent<Animator>();
					}

					controller.applyRootMotion = false;

					return controller;
				}
			}
			public AnimationClip[] AnimationClips
			{
				get
				{
					if (!Animator || !Animator.runtimeAnimatorController)
						return new AnimationClip[] { };

					return Animator.runtimeAnimatorController.animationClips;
				}
			}
			public bool IsValid
			{
				get
				{
					return driver;
				}
			}

			private Animator controller;
			[SerializeField]
			private VehicleDriver driver;

			#endregion

			#region Constructors

			public AnimationModule(VehicleDriver driver)
			{
				this.driver = driver;
			}

			#endregion

			#region Operators

			public static implicit operator bool(AnimationModule module) => module != null;

			#endregion
		}

		#endregion

		#region Sheets

		public readonly struct ProblemsSheet
		{
			#region Variables

			public bool HasAnimationsWarnings => !HasInternalErrors && !driver.Animation.Animator.runtimeAnimatorController;
			public bool HasAnimationsErrors => !HasInternalErrors && driver.Animation.AnimationClips.Length < 1;
			public bool HasAnimationsIssues => !HasInternalErrors && !driver.Animation.Animator.avatar;
			public bool IsValid => driver;

			private readonly VehicleDriver driver;

			#endregion

			#region Methods

			#region Virtual Methods

			public override bool Equals(object obj)
			{
				return obj is ProblemsSheet sheet &&
					   EqualityComparer<VehicleDriver>.Default.Equals(driver, sheet.driver);
			}
			public override int GetHashCode()
			{
				return -115724361 + EqualityComparer<VehicleDriver>.Default.GetHashCode(driver);
			}

			#endregion

			#region Utilities

			public void DisableDriverOnInternalErrors()
			{
				if (!driver || !HasInternalErrors)
					return;

				DisableAllBehaviours();
				ToolkitDebug.Error($"The {driver.name}'s MVC Driver behaviour has been disabled! We have had some internal errors that need to be fixed in order for the MVC behaviours to work properly. You can check the Vehicle inspector editor log for more information.");
			}
			public void DisableDriverOnWarnings()
			{
				if (!driver || !HasAnimationsWarnings)
					return;

				DisableAllBehaviours();
				ToolkitDebug.Warning($"The {driver.name}'s Vehicle Driver component has been disabled! It may have some errors that need to be fixed. You can check the Vehicle inspector editor log for more information.");
			}
			public void DisableDriverOnErrors()
			{
				if (!driver || !HasAnimationsErrors)
					return;

				DisableAllBehaviours();
				ToolkitDebug.Error($"The {driver.name}'s Vehicle Driver component has been disabled! It may have some problems that need to be fixed. You can check the Vehicle inspector editor log for more information.");
			}

			private void DisableAllBehaviours()
			{
				driver.enabled = false;
			}

			#endregion

			#endregion

			#region Constructors

			public ProblemsSheet(VehicleDriver driver)
			{
				this.driver = driver;
			}

			#endregion

			#region Operators

			public static bool operator ==(ProblemsSheet sheetA, ProblemsSheet sheetB)
			{
				return sheetA.Equals(sheetB);
			}
			public static bool operator !=(ProblemsSheet sheetA, ProblemsSheet sheetB)
			{
				return !(sheetA == sheetB);
			}

			#endregion
		}

		#endregion

		#endregion

		#region Variables

		#region Modules & Sheets

		#region Modules

		public AnimationModule Animation
		{
			get
			{
				if (!animation || !animation.IsValid)
					animation = new(this);

				return animation;
			}
		}

		[SerializeField]
#if UNITY_EDITOR
		private new AnimationModule animation;
#else
		private AnimationModule animation;
#endif

		#endregion

		#region Sheets

		public ProblemsSheet Problems
		{
			get
			{
				if (!problems.IsValid)
					problems = new(this);

				return problems;
			}
		}

		private ProblemsSheet problems;

		#endregion

		#endregion

		#region Properties

		public Vehicle TargetVehicle
		{
			get
			{
				if (!targetVehicle)
					targetVehicle = GetComponentInParent<Vehicle>();

				return targetVehicle;
			}
		}
		public Vector3 LookAtPosition
		{
			get
			{
				return lookAtEndPosition;
			}
		}
		public bool forceEditSecondaryPivots;

		private Animator animator;
		private Vehicle targetVehicle;
		private Vehicle.DriverIKModule driverIK;
		private Vector3 lookAtStartPosition;
		private Vector3 lookAtEndPosition;
		private Bounds bounds;

		#endregion

		#region Fields

		public float LookAtPositionHeight
		{
			get
			{
				return lookAtPositionHeight;
			}
			set
			{
				lookAtPositionHeight = Mathf.Clamp01(value);
			}
		}

		[SerializeField]
		private float lookAtPositionHeight = .75f;

		#endregion

		#endregion

		#region Methods

		#region Awake

		public void Restart()
		{
			Awaken = false;

			Initialize();

			if (!TargetVehicle)
				return;

			bounds = targetVehicle.Bounds;
			Awaken = true;
		}

		private void Initialize()
		{
			bounds = default;
		}

		#endregion

		#region Utilities

		private void RefreshLookAtPosition()
		{
			var extents = bounds.extents;
			var size = extents * 2f;

			lookAtStartPosition = targetVehicle.transform.TransformPoint(bounds.center) + extents.z * targetVehicle.transform.forward + (size.y * lookAtPositionHeight - extents.y) * targetVehicle.transform.up;
			lookAtEndPosition = lookAtStartPosition + targetVehicle.transform.forward * 2f;
		}
		private void PreviewAnimation()
		{
			if (Application.isPlaying || !Animation || !animation.Animator || !animation.Animator.runtimeAnimatorController || !TargetVehicle)
				return;

			bounds = targetVehicle.Bounds;

			RefreshLookAtPosition();

			Animator[] animators = targetVehicle.GetComponentsInChildren<Animator>();

			for (int i = 0; i < animators.Length; i++)
				if (animators[i].enabled)
					animators[i].Update(0f);
		}

		#endregion

		#region IK

		private void OnAnimatorIK(int layerIndex)
		{
			if (
#if MVC_COMMUNITY
				Application.isPlaying ||
#endif
				!animation && !(animation = Animation) || !animator && !(animator = animation.Animator) || !targetVehicle && !(targetVehicle = TargetVehicle) || !driverIK && !(driverIK = targetVehicle.DriverIK))
				return;

			RefreshLookAtPosition();
			animator.SetLookAtPosition(lookAtEndPosition);
			animator.SetLookAtWeight(1f);

			if (driverIK.HasAllSteeringWheelPivots)
			{
				Vehicle.InteriorModule.ComponentModule steeringWheel = targetVehicle.Interior.SteeringWheel;

				if (steeringWheel.transform)
				{
					Animator steeringWheelAnimator = steeringWheel.transform.GetComponent<Animator>();

					if (steeringWheelAnimator)
					{
						float steeringWheelAngle = steeringWheel.rotationAxis switch
						{
							Utility.Axis3.X => steeringWheel.transform.localEulerAngles.x,
							Utility.Axis3.Y => steeringWheel.transform.localEulerAngles.y,
							_ => steeringWheel.transform.localEulerAngles.z,
						};

						if (steeringWheelAngle < -180f)
							steeringWheelAngle += 360f;
						else if (steeringWheelAngle > 180f)
							steeringWheelAngle -= 360f;

						steeringWheelAnimator.SetFloat(driverIK.steeringWheelAngleParameter, steeringWheelAngle);
					}
				}

				animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1f);
				animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1f);
				animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1f);
				animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1f);
				animator.SetIKPosition(AvatarIKGoal.LeftHand, driverIK.leftHandSteeringWheelPivot.transform.position);
				animator.SetIKRotation(AvatarIKGoal.LeftHand, driverIK.leftHandSteeringWheelPivot.transform.rotation);
				animator.SetIKPosition(AvatarIKGoal.RightHand, driverIK.rightHandSteeringWheelPivot.transform.position);
				animator.SetIKRotation(AvatarIKGoal.RightHand, driverIK.rightHandSteeringWheelPivot.transform.rotation);
			}

			if (driverIK.HasAllFeetPivots)
			{
				animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1f);
				animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1f);
				animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1f);
				animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1f);
				animator.SetIKPosition(AvatarIKGoal.LeftFoot, driverIK.leftFootPivot.transform.position);
				animator.SetIKRotation(AvatarIKGoal.LeftFoot, driverIK.leftFootPivot.transform.rotation);
				animator.SetIKPosition(AvatarIKGoal.RightFoot, driverIK.rightFootPivot.transform.position);
				animator.SetIKRotation(AvatarIKGoal.RightFoot, driverIK.rightFootPivot.transform.rotation);
			}
		}

		#endregion

		#region Gizmos

		private void OnDrawGizmos()
		{
			if (!IsSetupDone
#if MVC_COMMUNITY
				|| Application.isPlaying
#endif
				)
				return;

			PreviewAnimation();
		}
		private void OnDrawGizmosSelected()
		{
			if (
#if MVC_COMMUNITY
				Application.isPlaying ||
#endif
				!TargetVehicle)
				return;

			Gizmos.color = Settings.driverIKGizmoColor;

			Utility.DrawArrowForGizmos(lookAtEndPosition - targetVehicle.transform.forward, targetVehicle.transform.forward * (1f - (Settings.gizmosSize / 32f)), Settings.gizmosSize / 8f);
		}

		#endregion

		#endregion
	}
}

#region Namespaces

using System;
using Unity.Mathematics;
using UnityEngine;
using Utilities;
using MVC.Utilities.Internal;
using UnityEngine.Events;

#endregion

namespace MVC.Core
{
	#region Enumerators

	public enum VehicleDifferentialType
	{
		Open,
		Custom,
		Locked,
		LimitedSlip
	}

	#endregion

	#region Modules

	[Serializable]
	public class VehicleDifferential : VehicleDrivetrainComponent
	{
		#region Variables

		#region Properties

		/// <summary>Type of the differential. Setting the `type` to `Custom` requires a `SplitTorqueDelegate` to be set.</summary>
		public VehicleDifferentialType Type
		{
			get
			{
				return type;
			}
			set
			{
				type = value;

				RefreshSplitTorqueDelegate();
			}
		}

		/// <summary>Torque bias between left (A) and right (B) output respectively in range of [0, 1].</summary>
		public float BiasAB
		{
			get
			{
				return biasAB;
			}
			set
			{
				biasAB = Utility.Clamp01(value);
			}
		}
		/// <summary>Output gear ratio.</summary>
		public float GearRatio
		{
			get
			{
				return gearRatio;
			}
			set
			{
				gearRatio = math.abs(value);
			}
		}
		/// <summary>
		/// Stiffness of locking differential in range of [0, 1]. Higher values will result in lower difference in rotational velocity between left and right wheel. Too high values might introduce slight oscillation due to drivetrain windup and a vehicle that is hard to steer.
		/// </summary>
		public float Stiffness
		{
			get
			{
				return stiffness;
			}
			set
			{
				stiffness = Utility.Clamp01(value);
			}
		}
		/// <summary>
		/// Stiffness of the LSD differential under acceleration in range of [0, 1]. Higher values will result in lower difference in rotational velocity between left and right wheels. Too high values might introduce slight oscillation due to drivetrain windup and a vehicle that is hard to steer.
		/// </summary>
		public float PowerRamp
		{
			get
			{
				return powerRamp;
			}
			set
			{
				powerRamp = Utility.Clamp01(value);
			}
		}
		/// <summary>
		/// Stiffness of the LSD differential under braking in range of [0, 1]. Higher values will result in lower difference in rotational velocity between left and right wheels. Too high values might introduce slight oscillation due to drivetrain windup and a vehicle that is hard to steer.
		/// </summary>
		public float CoastRamp
		{
			get
			{
				return coastRamp;
			}
			set
			{
				coastRamp = Utility.Clamp01(value);
			}
		}
		/// <summary>Slip torque of the LSD differential. Measured in Newton·Meters (N·m).</summary>
		public float SlipTorque
		{
			get
			{
				return slipTorque;
			}
			set
			{
				slipTorque = math.abs(value);
			}
		}
		/// <summary>Defines the behaviour of the differential. You can only set a custom delegate if the differential `type` is set to `Custom`.</summary>
		public SplitTorque SplitTorqueDelegate
		{
			get
			{
				if (Application.isPlaying && splitTorqueDelegate == null)
					RefreshSplitTorqueDelegate();

				return splitTorqueDelegate;
			}
			set
			{
				if (type != VehicleDifferentialType.Custom)
				{
					ToolkitDebug.Warning("Cannot set differential `SplitTorqueDelegate` unless `type` is set to `Custom`.");

					return;
				}

				splitTorqueDelegate = value;
			}
		}
		public override float OutputAngularVelocity
		{
			get
			{
				return Utility.Average(outputAngularVelocityA, outputAngularVelocityB);
			}
		}
		public override float OutputInertia
		{
			get
			{
				return outputInertiaA + outputInertiaB;
			}
		}
		/// <summary>The inertia of the left output (A).</summary>
		public float OutputInertiaA
		{
			get
			{
				return outputInertiaA;
			}
			set
			{
				outputInertiaA = math.max(value, math.EPSILON);
			}
		}
		/// <summary>The inertia of the right output (B).</summary>
		public float OutputInertiaB
		{
			get
			{
				return outputInertiaB;
			}
			set
			{
				outputInertiaB = math.max(value, math.EPSILON);
			}
		}
		public override float OutputTorque
		{
			get
			{
				return OutputTorqueA + OutputTorqueB;
			}
		}
		/// <summary>The left output (A).</summary>
		public float OutputTorqueA
		{
			get
			{
				return outputTorqueA;
			}
		}
		/// <summary>The right output (B). Measured in Newton·Meters (N·m).</summary>
		public float OutputTorqueB
		{
			get
			{
				return outputTorqueB;
			}
		}

		[SerializeField]
		private VehicleDifferentialType type = VehicleDifferentialType.LimitedSlip;
		[SerializeField]
		private float biasAB = .5f;
		[SerializeField]
		private float gearRatio = 1f;
		[SerializeField]
		private float stiffness = .5f;
		[SerializeField]
		private float powerRamp = 1f;
		[SerializeField]
		private float coastRamp = .125f;
		[SerializeField]
		private float slipTorque = 1000f;
		[SerializeField]
		private SplitTorque splitTorqueDelegate;

		#endregion

		#region Runtime

		/// <summary>Angular velocity of the left output (A). Measured in Radians per Second (rad/s).</summary>
		public float outputAngularVelocityA;
		/// <summary>Angular velocity of the right output (B). Measured in Radians per Second (rad/s).</summary>
		public float outputAngularVelocityB;

		private float outputInertiaA;
		private float outputInertiaB;
		private float outputTorqueA;
		private float outputTorqueB;

		#endregion

		#endregion

		#region Delegates

		/// <summary>
		/// 
		/// </summary>
		/// <param name="inputTorque">Input torque</param>
		/// <param name="angularVelocityA">Angular velocity of the outputA</param>
		/// <param name="angularVelocityB">Angular velocity of the outputB</param>
		/// <param name="inertiaA">Inertia of the outputA</param>
		/// <param name="inertiaB">Inertia of the outputB</param>
		/// <param name="deltaTime">Time step</param>
		/// <param name="biasAB">Torque bias between outputA and outputB. 0 = all torque goes to A, 1 = all torque goes to B</param>
		/// <param name="stiffness">Stiffness of the limited slip or locked differential</param>
		/// <param name="powerRamp">Stiffness under power</param>
		/// <param name="coastRamp">Stiffness under braking</param>
		/// <param name="slipTorque">Slip torque of the limited slip differential</param>
		/// <param name="outputTorqueA">Torque output towards outputA</param>
		/// <param name="outputTorqueB">Torque output towards outputB</param>
		public delegate void SplitTorque(float inputTorque, float angularVelocityA, float angularVelocityB, float inertiaA, float inertiaB, float deltaTime, float biasAB,
			float stiffness, float powerRamp, float coastRamp, float slipTorque, out float outputTorqueA, out float outputTorqueB);

		#endregion

		#region Utilities

		public void LSDTorqueSplit(float inputTorque, float angularVelocityA, float angularVelocityB, float _inertiaA, float _inertiaB, float _deltaTime, float biasAB, float stiffness, float powerRamp, float coastRamp, float slipTorque, out float outputTorqueA, out float outputTorqueB)
		{
			if (Mathf.Approximately(slipTorque, 0f) || (Mathf.Approximately(powerRamp, 0f) && Mathf.Approximately(coastRamp, 0f)))
			{
				OpenTorqueSplit(inputTorque, angularVelocityA, angularVelocityB, _inertiaA, _inertiaB, _deltaTime, biasAB, stiffness, powerRamp, coastRamp, slipTorque, out outputTorqueA, out outputTorqueB);

				return;
			}

			float coefficient = inputTorque < 0 ? coastRamp : powerRamp;
			float angularVelocityTotal = math.abs(angularVelocityA) + math.abs(angularVelocityB);
			float slip = angularVelocityTotal != 0f ? (angularVelocityA - angularVelocityB) / angularVelocityTotal : 0f;
			float torqueDifference = slip * stiffness * coefficient * slipTorque;
			float inputTorqueAbs = Mathf.Abs(inputTorque);

			torqueDifference = Mathf.Clamp(torqueDifference, -inputTorqueAbs * .5f, inputTorqueAbs * .5f);
			outputTorqueA = inputTorque * .5f - torqueDifference;
			outputTorqueB = inputTorque * .5f + torqueDifference;
		}
		public void LockingTorqueSplit(float inputTorque, float angularVelocityA, float angularVelocityB, float inertiaA, float inertiaB, float _deltaTime, float _biasAB, float stiffness, float _powerRamp, float _coastRamp, float _slipTorque, out float outputTorqueA, out float outputTorqueB)
		{
			if (Mathf.Approximately(stiffness, 0f))
			{
				OpenTorqueSplit(inputTorque, angularVelocityA, angularVelocityB, inertiaA, inertiaB, _deltaTime, _biasAB, stiffness, _powerRamp, _coastRamp, _slipTorque, out outputTorqueA, out outputTorqueB);

				return;
			}

			float inertiaSum = inertiaA + inertiaB;
			float averageAngularVelocity = inertiaA / inertiaSum * angularVelocityA + inertiaB / inertiaSum * angularVelocityB;
			float torqueACorrective = (averageAngularVelocity - angularVelocityA) * inertiaA;
			float torqueBCorrective = (averageAngularVelocity - angularVelocityB) * inertiaB;

			torqueACorrective *= stiffness;
			torqueBCorrective *= stiffness;

			float inputTorqueAbs = math.abs(inputTorque);

			torqueACorrective = math.clamp(torqueACorrective, -inputTorqueAbs, inputTorqueAbs);
			torqueBCorrective = math.clamp(torqueBCorrective, -inputTorqueAbs, inputTorqueAbs);

			float biasA = Utility.Clamp01(0.5f + (angularVelocityB - angularVelocityA) * 10f * stiffness);

			outputTorqueA = inputTorque * biasA + torqueACorrective;
			outputTorqueB = inputTorque * (1f - biasA) + torqueBCorrective;
		}
		public void OpenTorqueSplit(float inputTorque, float _angularVelocityA, float _angularVelocityB, float _inertiaA, float _inertiaB, float _deltaTime, float biasAB, float _stiffness, float _powerRamp, float _coastRamp, float _slipTorque, out float outputTorqueA, out float outputTorqueB)
		{
			outputTorqueA = inputTorque * (1f - biasAB);
			outputTorqueB = inputTorque * biasAB;
		}

		private void RefreshSplitTorqueDelegate()
		{
			if (type == VehicleDifferentialType.Custom && splitTorqueDelegate == null)
			{
				type = VehicleDifferentialType.Open;

				ToolkitDebug.Log("Differential `type` is set to `Custom` but `SplitTorqueDelegate` is unassigned. Reverting differential `type` to `Open`...");
			}

			splitTorqueDelegate = type switch
			{
				VehicleDifferentialType.Open => OpenTorqueSplit,
				VehicleDifferentialType.Custom => splitTorqueDelegate,
				VehicleDifferentialType.Locked => LockingTorqueSplit,
				VehicleDifferentialType.LimitedSlip =>	LSDTorqueSplit,
				_ => null
			};
		}

		#endregion

		#region Methods

		public override void Initialize()
		{
			RefreshSplitTorqueDelegate();

			outputAngularVelocityA = default;
			outputAngularVelocityB = default;
			outputInertiaA = default;
			outputInertiaB = default;
			outputTorqueA = default;
			outputTorqueB = default;
		}
		public override void Step(float deltaTime)
		{
			if (splitTorqueDelegate == null)
				throw new ArgumentNullException("splitTorqueDelegate", "Updating an uninitialized component is not allowed");

			splitTorqueDelegate.Invoke(inputTorque, outputAngularVelocityA, outputAngularVelocityB, outputInertiaA, outputInertiaB, deltaTime, biasAB, stiffness, powerRamp, coastRamp, slipTorque, out outputTorqueA, out outputTorqueB);
		}

		#endregion
	}

	internal struct VehicleDifferentialAccess
	{
		#region Variables

		public VehicleDifferentialType type;
		public float biasAB;
		public float gearRatio;
		public float stiffness;
		public float powerRamp;
		public float coastRamp;
		public float slipTorque;
		public float outputAngularVelocity;
		public float outputAngularVelocityA;
		public float outputAngularVelocityB;
		public float outputInertia;
		public float outputInertiaA;
		public float outputInertiaB;
		public float outputTorque;
		public float outputTorqueA;
		public float outputTorqueB;

		#endregion

		#region Constructors

		public VehicleDifferentialAccess(VehicleDifferential differential) : this()
		{
			if (!differential)
				return;

			type = differential.Type;
			biasAB = differential.BiasAB;
			gearRatio = differential.GearRatio;
			stiffness = differential.Stiffness;
			powerRamp = differential.PowerRamp;
			coastRamp = differential.CoastRamp;
			slipTorque = differential.SlipTorque;
			outputAngularVelocity = differential.OutputAngularVelocity;
			outputAngularVelocityA = differential.outputAngularVelocityA;
			outputAngularVelocityB = differential.outputAngularVelocityB;
			outputInertia = differential.OutputInertia;
			outputInertiaA = differential.OutputInertiaA;
			outputInertiaB = differential.OutputInertiaB;
			outputTorque = differential.OutputTorque;
			outputTorqueA = differential.OutputTorqueA;
			outputTorqueB = differential.OutputTorqueB;
		}

		#endregion

		#region Operators

		public static implicit operator VehicleDifferentialAccess(VehicleDifferential differential) => new(differential);

		#endregion
	}

	#endregion
}
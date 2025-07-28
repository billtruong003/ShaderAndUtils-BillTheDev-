#region Namespaces

using System;

#endregion

namespace MVC.Core
{
	[Serializable]
	public abstract class VehicleDrivetrainComponent : ToolkitComponent
	{
		#region Variables

		/// <summary>Average angular velocity output of both left (A) and right (B) sides of the differential. Measured in Radians per Second (rad/s).</summary>
		public abstract float OutputAngularVelocity { get; }
		/// <summary>Sum inertia output of both left (A) and right (B) sides of the differential.</summary>
		public abstract float OutputInertia { get; }
		/// <summary>Sum torque output of both left (A) and right (B) sides of the differential. Measured in Newton·Meters (N·m).</summary>
		public abstract float OutputTorque { get; }

		[NonSerialized]
		public float inputTorque;

		#endregion

		#region Methods

		public abstract void Initialize();
		public abstract void Step(float deltaTime);

		#endregion
	}
}

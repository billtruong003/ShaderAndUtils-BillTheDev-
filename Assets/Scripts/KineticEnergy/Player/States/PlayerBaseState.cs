using StateSystem;

namespace Kaelia.Player.States
{
    public abstract class PlayerBaseState : State
    {
        protected readonly PlayerController Controller;
        protected readonly Data.PlayerDataSO Data;

        protected PlayerBaseState(PlayerController controller, StateMachine stateMachine) : base(stateMachine)
        {
            this.Controller = controller;
            this.Data = controller.Data;
        }

        public override void LogicUpdate()
        {
            if (Controller.Input.DashDown && Controller.CanDash)
            {
                if (Controller.IsWeaponDrawn && Controller.MoveDirection.sqrMagnitude > 0.1f)
                {
                    StateMachine.ChangeState(new Player.States.PlayerDashAttackState(Controller, StateMachine));
                }
                else
                {
                    StateMachine.ChangeState(new PlayerDashState(Controller, StateMachine));
                }
                return;
            }

            if (Controller.Input.DrawWeaponDown)
            {
                StateMachine.ChangeState(new Player.States.PlayerStanceState(Controller, StateMachine));
                return;
            }
        }

        protected void HandleRotation()
        {
            if (Controller.MoveDirection.sqrMagnitude > 0.01f)
            {
                float targetAngle = UnityEngine.Mathf.Atan2(Controller.MoveDirection.x, Controller.MoveDirection.z) * UnityEngine.Mathf.Rad2Deg;
                float currentSmoothVelocity = Controller.TurnSmoothVelocity;
                float angle = UnityEngine.Mathf.SmoothDampAngle(Controller.transform.eulerAngles.y, targetAngle, ref currentSmoothVelocity, Data.RotationSmoothTime);
                Controller.transform.rotation = UnityEngine.Quaternion.Euler(0f, angle, 0f);
                Controller.TurnSmoothVelocity = currentSmoothVelocity;
            }
        }

        protected UnityEngine.Vector3 GetSlopeMoveDirection()
        {
            return UnityEngine.Vector3.ProjectOnPlane(UnityEngine.Vector3.down, Controller.GroundHit.normal).normalized;
        }
    }
}
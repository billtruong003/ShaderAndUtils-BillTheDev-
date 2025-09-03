using UnityEngine;
using StateSystem;

namespace Kaelia.Player.States
{
    public class PlayerAirborneState : PlayerBaseState
    {
        public PlayerAirborneState(PlayerController controller, StateMachine stateMachine) : base(controller, stateMachine) { }

        public override void Enter()
        {
            Controller.Rb.linearDamping = 0;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (Controller.IsGrounded)
            {
                StateMachine.ChangeState(new PlayerGroundedState(Controller, StateMachine));
                return;
            }

            if (Controller.Input.LightAttackDown && Controller.IsWeaponDrawn)
            {
                StateMachine.ChangeState(new Player.States.PlayerAirborneAttackState(Controller, StateMachine));
                return;
            }

            if ((Controller.IsWallLeft || Controller.IsWallRight) && Controller.Input.Vertical > 0)
            {
                StateMachine.ChangeState(new PlayerWallRunState(Controller, StateMachine));
                return;
            }

            if (Controller.Input.JumpDown && Controller.CanDoubleJump)
            {
                PerformDoubleJump();
            }
        }

        public override void PhysicsUpdate()
        {
            HandleAirborneMovement();
            HandleRotation();
        }

        private void HandleAirborneMovement()
        {
            Vector3 flatVel = new Vector3(Controller.Rb.linearVelocity.x, 0f, Controller.Rb.linearVelocity.z);

            if (flatVel.magnitude < Data.MaxAirSpeed)
            {
                Controller.Rb.AddForce(Controller.MoveDirection * Data.AirAcceleration, ForceMode.Force);
            }

            if (Controller.MoveDirection.sqrMagnitude < 0.01f)
            {
                Controller.Rb.linearVelocity = new Vector3(flatVel.x * Data.AirDamping, Controller.Rb.linearVelocity.y, flatVel.z * Data.AirDamping);
            }
        }

        private void PerformDoubleJump()
        {
            Controller.CanDoubleJump = false;
            Controller.Rb.linearVelocity = new Vector3(Controller.Rb.linearVelocity.x, 0f, Controller.Rb.linearVelocity.z);
            Controller.Rb.AddForce(Controller.transform.up * Data.JumpForce, ForceMode.Impulse);
            Controller.Animator.SetTrigger("Jump");
        }
    }
}
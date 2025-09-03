using UnityEngine;
using StateSystem;

namespace Kaelia.Player.States
{
    public class PlayerGroundedState : PlayerBaseState
    {
        public PlayerGroundedState(PlayerController controller, StateMachine stateMachine) : base(controller, stateMachine) { }

        public override void Enter()
        {
            Controller.Rb.linearDamping = Data.GroundLinearDamping;
            Controller.CanDoubleJump = true;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (Controller.Input.LightAttackDown && Controller.IsWeaponDrawn)
            {
                StateMachine.ChangeState(new Player.States.PlayerGroundComboState(Controller, StateMachine));
                return;
            }

            if (Controller.Input.JumpDown)
            {
                Controller.JumpBufferCounter = Data.JumpBufferTime;
            }

            if (!Controller.IsGrounded && Controller.CoyoteTimeCounter > 0 && Controller.JumpBufferCounter > 0)
            {
                PerformJump();
                return;
            }

            if (!Controller.IsGrounded && Controller.CoyoteTimeCounter <= 0)
            {
                StateMachine.ChangeState(new PlayerAirborneState(Controller, StateMachine));
                return;
            }

            if (Controller.JumpBufferCounter > 0)
            {
                PerformJump();
                return;
            }

            if (Controller.Input.SlideDown && Controller.Rb.linearVelocity.magnitude > Data.WalkSpeed)
            {
                StateMachine.ChangeState(new PlayerSlideState(Controller, StateMachine));
                return;
            }
        }

        public override void PhysicsUpdate()
        {
            HandleGroundedMovement();
            HandleRotation();
        }

        private void HandleGroundedMovement()
        {
            float currentSpeed = Controller.Input.RunHeld ? Data.RunSpeed : Data.WalkSpeed;

            if (Controller.MoveDirection.sqrMagnitude > 0.1f)
            {
                Controller.Rb.AddForce(Controller.MoveDirection * currentSpeed * 10f, ForceMode.Force);
            }
        }

        private void PerformJump()
        {
            Controller.CoyoteTimeCounter = 0f;
            Controller.JumpBufferCounter = 0f;
            Controller.Rb.linearVelocity = new Vector3(Controller.Rb.linearVelocity.x, 0f, Controller.Rb.linearVelocity.z);
            Controller.Rb.AddForce(Controller.transform.up * Data.JumpForce, ForceMode.Impulse);
            Controller.Animator.SetTrigger("Jump");
            StateMachine.ChangeState(new PlayerAirborneState(Controller, StateMachine));
        }
    }
}
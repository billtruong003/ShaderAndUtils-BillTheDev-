using UnityEngine;
using StateSystem;

namespace Kaelia.Player.States
{
    public class PlayerSlideState : PlayerBaseState
    {
        public PlayerSlideState(PlayerController controller, StateMachine stateMachine) : base(controller, stateMachine) { }

        public override void Enter()
        {
            Controller.Animator.SetBool("IsSliding", true);
            Controller.SetColliderHeight(Data.SlideColliderHeight, new Vector3(0, Data.SlideColliderHeight / 2, 0));
            Controller.Rb.AddForce(Controller.transform.forward * Data.SlideStartBoost, ForceMode.Impulse);
        }

        public override void Exit()
        {
            Controller.Animator.SetBool("IsSliding", false);
            Controller.ResetCollider();
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            bool isMovingFastEnough = Controller.Rb.linearVelocity.magnitude > Data.WalkSpeed;
            if (Controller.Input.SlideUp || !isMovingFastEnough)
            {
                StateMachine.ChangeState(new PlayerGroundedState(Controller, StateMachine));
            }
        }

        public override void PhysicsUpdate()
        {
            ApplySlideMovement();
        }

        private void ApplySlideMovement()
        {
            Vector3 inputForce = (Controller.transform.right * Controller.Input.Horizontal) * Data.SlideSteeringControl;
            Vector3 slopeForce = GetSlopeMoveDirection() * Data.SlopeSlideMultiplier;
            Controller.Rb.AddForce(inputForce + slopeForce, ForceMode.Force);

            Controller.Rb.linearVelocity *= Data.SlideFriction;
            Controller.Rb.linearVelocity = Vector3.ClampMagnitude(Controller.Rb.linearVelocity, Data.MaxSlideSpeed);
        }
    }
}
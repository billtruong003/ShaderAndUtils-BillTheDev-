using UnityEngine;
using StateSystem;

namespace Kaelia.Player.States
{
    public class PlayerAirborneAttackState : PlayerBaseState
    {
        public PlayerAirborneAttackState(PlayerController controller, StateMachine stateMachine) : base(controller, stateMachine) { }

        public override void Enter()
        {
            Controller.Rb.linearVelocity = Vector3.zero;
            Controller.Rb.AddForce(Vector3.down * Data.AirborneAttackDownwardForce, ForceMode.Impulse);
            Controller.Animator.SetTrigger("AirAttack");
        }

        public override void LogicUpdate()
        {
            // Transition to grounded state is handled by the landing animation or ground check
            if (Controller.IsGrounded)
            {
                // Optionally trigger a landing animation/effect here
                StateMachine.ChangeState(new PlayerGroundedState(Controller, StateMachine));
            }
        }
    }
}
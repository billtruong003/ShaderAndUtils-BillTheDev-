using UnityEngine;
using StateSystem;
using System.Collections;

namespace Kaelia.Player.States
{
    public class PlayerDashState : PlayerBaseState
    {
        private float dashTimer;
        private float originalDamping;

        public PlayerDashState(PlayerController controller, StateMachine stateMachine) : base(controller, stateMachine) { }

        public override void Enter()
        {
            dashTimer = Data.DashDuration;
            Controller.CanDash = false;
            Controller.StartCoroutine(DashCooldownRoutine());

            Vector3 dashDirection = Controller.MoveDirection.sqrMagnitude > 0.1f ? Controller.MoveDirection : Controller.transform.forward;

            originalDamping = Controller.Rb.linearDamping;
            Controller.Rb.linearDamping = 0;
            Controller.Rb.useGravity = false;
            Controller.Rb.linearVelocity = dashDirection * Data.DashSpeed;

            Controller.ChangeLayer(Data.InvincibleLayer);
            Controller.Animator.SetTrigger("Dash");
        }

        public override void Exit()
        {
            Controller.ChangeLayer(Data.PlayerLayer);
            Controller.Rb.useGravity = true;
            Controller.Rb.linearDamping = originalDamping;
        }

        public override void LogicUpdate()
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                if (Controller.IsGrounded)
                {
                    StateMachine.ChangeState(new PlayerGroundedState(Controller, StateMachine));
                }
                else
                {
                    StateMachine.ChangeState(new PlayerAirborneState(Controller, StateMachine));
                }
            }
        }

        private IEnumerator DashCooldownRoutine()
        {
            yield return new WaitForSeconds(Data.DashCooldown);
            Controller.CanDash = true;
        }
    }
}
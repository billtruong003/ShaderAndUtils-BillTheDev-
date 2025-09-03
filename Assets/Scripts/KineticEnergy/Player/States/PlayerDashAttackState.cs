using UnityEngine;
using StateSystem;
using System.Collections;

namespace Kaelia.Player.States
{
    public class PlayerDashAttackState : PlayerBaseState
    {
        private float dashAttackTimer;

        public PlayerDashAttackState(PlayerController controller, StateMachine stateMachine) : base(controller, stateMachine) { }

        public override void Enter()
        {
            dashAttackTimer = Data.DashAttackDuration;
            Controller.CanDash = false;
            Controller.StartCoroutine(DashCooldownRoutine());

            Vector3 dashDirection = Controller.MoveDirection;

            Controller.Rb.useGravity = false;
            Controller.Rb.linearVelocity = dashDirection * Data.DashAttackSpeed;

            Controller.ChangeLayer(Data.InvincibleLayer);
            Controller.Animator.SetTrigger("DashAttack");
        }

        public override void Exit()
        {
            Controller.ChangeLayer(Data.PlayerLayer);
            Controller.Rb.useGravity = true;
        }

        public override void LogicUpdate()
        {
            dashAttackTimer -= Time.deltaTime;
            if (dashAttackTimer <= 0)
            {
                Controller.Rb.linearVelocity *= 0.2f; // Reduce speed after dash attack
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
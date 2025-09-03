using UnityEngine;
using StateSystem;

namespace Kaelia.Player.States
{
    public class PlayerStanceState : PlayerBaseState
    {
        private float stateTimer;

        public PlayerStanceState(PlayerController controller, StateMachine stateMachine) : base(controller, stateMachine) { }

        public override void Enter()
        {
            Controller.IsWeaponDrawn = !Controller.IsWeaponDrawn;

            string trigger = Controller.IsWeaponDrawn ? "DrawWeapon" : "SheatheWeapon";
            stateTimer = Controller.IsWeaponDrawn ? Data.DrawWeaponDuration : Data.SheatheWeaponDuration;

            Controller.Animator.SetTrigger(trigger);
            Controller.Input.DisableInputsForDuration(stateTimer);
        }

        public override void LogicUpdate()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0)
            {
                ReturnToPreviousState();
            }
        }

        private void ReturnToPreviousState()
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
}
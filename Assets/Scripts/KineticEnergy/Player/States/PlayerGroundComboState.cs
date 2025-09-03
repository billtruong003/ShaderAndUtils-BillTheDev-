using UnityEngine;
using StateSystem;

namespace Kaelia.Player.States
{
    public class PlayerGroundComboState : PlayerBaseState
    {
        private int comboCounter;
        private float comboWindowTimer;
        private bool hasNextComboInput;

        public PlayerGroundComboState(PlayerController controller, StateMachine stateMachine) : base(controller, stateMachine) { }

        public override void Enter()
        {
            comboCounter = 0;
            comboWindowTimer = Data.ComboWindow;
            hasNextComboInput = false;

            PerformAttack();
        }

        public override void Exit()
        {
            Controller.Animator.SetInteger("ComboStep", 0);
        }

        public override void LogicUpdate()
        {
            comboWindowTimer -= Time.deltaTime;

            if (Controller.Input.LightAttackDown)
            {
                hasNextComboInput = true;
            }

            if (comboWindowTimer <= 0)
            {
                if (hasNextComboInput && comboCounter < Data.ComboMoveForce.Length - 1)
                {
                    comboCounter++;
                    PerformAttack();
                    comboWindowTimer = Data.ComboWindow;
                    hasNextComboInput = false;
                }
                else
                {
                    StateMachine.ChangeState(new PlayerGroundedState(Controller, StateMachine));
                }
            }
        }

        private void PerformAttack()
        {
            Controller.Animator.SetInteger("ComboStep", comboCounter + 1);

            Vector3 force = Controller.transform.TransformDirection(Data.ComboMoveForce[comboCounter]);
            Controller.Rb.AddForce(force, ForceMode.Impulse);
        }
    }
}
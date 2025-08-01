// Assets/Orion/Scripts/StateMachine/States/PlayerParryState.cs

using UnityEngine;

namespace Orion
{
    public class PlayerParryState : State
    {
        public PlayerParryState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine) { }

        public override void Enter()
        {
            base.Enter();
            player.AnimationController.SetParrying(true);
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (!player.Input.ParryIsHeld)
            {
                stateMachine.ChangeState(player.GroundedState);
            }
        }

        public override void Exit()
        {
            base.Exit();
            player.AnimationController.SetParrying(false);
        }
    }
}
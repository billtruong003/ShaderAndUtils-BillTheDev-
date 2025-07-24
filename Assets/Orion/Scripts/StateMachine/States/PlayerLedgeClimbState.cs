using UnityEngine;

namespace Orion
{
    public class PlayerLedgeClimbState : State
    {
        private Vector3 _targetPosition;
        private bool _hasReachedLedge;

        public PlayerLedgeClimbState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public override void Enter()
        {
            base.Enter();
            player.AnimationController.SetClimbingLedge(true);
            player.Rigidbody.isKinematic = true;
            _hasReachedLedge = false;
            CalculateLedgePosition();
        }

        public override void Exit()
        {
            base.Exit();
            player.AnimationController.SetClimbingLedge(false);
            player.Rigidbody.isKinematic = false;
        }

        public override void LogicUpdate()
        {
            base.LogicUpdate();

            if (_hasReachedLedge)
            {
                stateMachine.ChangeState(player.GroundedState);
                return;
            }

            MoveToLedge();
        }

        private void CalculateLedgePosition()
        {
            Vector3 worldLedgeDetectPoint = player.transform.TransformPoint(player.GetLedgeDetectOffset());
            if (Physics.Raycast(worldLedgeDetectPoint, Vector3.down, out RaycastHit hit, 2f, player.GetLedgeLayer()))
            {
                _targetPosition = hit.point + Vector3.up * player.CapsuleCollider.height * 0.5f;
            }
        }

        private void MoveToLedge()
        {
            player.transform.position = Vector3.Lerp(player.transform.position, _targetPosition, Time.deltaTime * 10f);
            if (Vector3.Distance(player.transform.position, _targetPosition) < 0.1f)
            {
                player.transform.position = _targetPosition;
                _hasReachedLedge = true;
            }
        }
    }
}
using UnityEngine;

namespace Orion
{
    public struct LedgeData
    {
        public Vector3 SurfacePoint;
        public Vector3 WallNormal;
    }

    public class PlayerLedgeClimbState : State
    {
        private LedgeData _ledgeData;
        private Vector3 _startPosition;
        private Vector3 _endPosition;
        private float _climbTimer;

        public PlayerLedgeClimbState(PlayerController player, StateMachine stateMachine) : base(player, stateMachine)
        {
        }

        public void SetLedgeData(LedgeData data)
        {
            _ledgeData = data;
        }

        public override void Enter()
        {
            base.Enter();
            player.AnimationController.SetClimbingLedge(true);
            player.Rigidbody.isKinematic = true;

            _startPosition = player.transform.position;
            _climbTimer = 0f;

            Vector3 relativeStandPosition = Quaternion.LookRotation(-_ledgeData.WallNormal) * player.LedgeClimbStandPositionOffset;
            _endPosition = _ledgeData.SurfacePoint + relativeStandPosition;
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

            _climbTimer += Time.deltaTime;
            float climbProgress = _climbTimer / player.LedgeClimbDuration;

            MovePlayer(climbProgress);

            if (climbProgress >= 1f)
            {
                stateMachine.ChangeState(player.GroundedState);
            }
        }

        private void MovePlayer(float progress)
        {
            float smoothedProgress = Mathf.SmoothStep(0f, 1f, progress);
            player.transform.position = Vector3.Lerp(_startPosition, _endPosition, smoothedProgress);
        }
    }
}
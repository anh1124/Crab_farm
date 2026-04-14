using UnityEngine;
using Script.Game.Other.Nature;
using Script.Utils;

namespace Script.Game.Other.Nature.States
{
    public class RabbitMoveToHuntingAreaState : RabbitState
    {
        public override void Enter(RabbitBehavior ctx)
        {
#if UNITY_EDITOR
            if (ctx.IsDebug) Debug.Log("[RabbitMoveToHuntingAreaState] Enter");
#endif
            ctx.PlayAnimation("move");
        }

        public override void UpdateBehavior(RabbitBehavior ctx)
        {
#if UNITY_EDITOR
            if (ctx.IsDebug) Debug.Log($"[RabbitMoveToHuntingAreaState] Speed: {ctx.Speed}, Pos: {ctx.transform.position}");
#endif

            // Nếu đã có Bush được gán, không cần lang thang nữa
            if (ctx.RabbitBush != null)
            {
                ctx.ChangeState(new RabbitMoveNearBushState());
                return;
            }

            // Tính target mới nếu chưa có
            if (_targetPosition == Vector3.zero)
            {
                var side = WorldUtility.GetNearestHuntingSide(ctx.transform.position);
                if (side == KingdomSide.None)
                {
                    ctx.ChangeState(new RabbitIdleState());
                    return;
                }

                bool isLeftSide = side == KingdomSide.Left;

                // Dùng WorldUtility — không tự viết lại logic tính vị trí ngoài SafeZone
                Vector3 targetPos = WorldUtility.GetRandomOutsideSafePos(isLeftSide, 5f, 50f);
                if (targetPos == Vector3.zero)
                {
                    ctx.ChangeState(new RabbitIdleState());
                    return;
                }

                _targetPosition = targetPos;
                ctx.SetTargetPosition(_targetPosition);

#if UNITY_EDITOR
                if (ctx.IsDebug) Debug.Log($"[RabbitMoveToHuntingAreaState] New hunt target: {_targetPosition}");
#endif
            }
        }

        public override void Update(RabbitBehavior ctx)
        {
            if (!ctx.CanMove) return;
            if (_targetPosition == Vector3.zero) return;

            Vector2 currentPosition = ctx.transform.position;
            float newX = Mathf.MoveTowards(currentPosition.x, _targetPosition.x, ctx.Speed * Time.deltaTime);
            float newY = Mathf.MoveTowards(currentPosition.y, _targetPosition.y, ctx.Speed * Time.deltaTime);
            ctx.transform.position = new Vector3(newX, newY, ctx.transform.position.z);

            MovementUtility.FlipDirection(ctx.gameObject, _targetPosition);

            // Dùng MovementUtility.IsMoving thay vì inline 0.001f
            ctx.IsMove = MovementUtility.IsMoving(currentPosition.x, newX) ||
                         MovementUtility.IsMoving(currentPosition.y, newY);

#if UNITY_EDITOR
            if (ctx.IsDebug) Debug.Log($"[RabbitMoveToHuntingAreaState] Moving → {ctx.transform.position}");
#endif

            // Dùng MovementUtility.HasReachedDestination thay vì inline 0.1f
            if (MovementUtility.HasReachedDestination(ctx.transform.position, _targetPosition))
            {
                _targetPosition = Vector3.zero;
                ctx.SetTargetPosition(Vector3.zero);
                ctx.ChangeState(new RabbitIdleState());

#if UNITY_EDITOR
                if (ctx.IsDebug) Debug.Log("[RabbitMoveToHuntingAreaState] Reached target → Idle");
#endif
            }
        }

        public override void Exit(RabbitBehavior ctx)
        {
#if UNITY_EDITOR
            if (ctx.IsDebug) Debug.Log("[RabbitMoveToHuntingAreaState] Exit");
#endif
            _targetPosition = Vector3.zero;
        }
    }
}

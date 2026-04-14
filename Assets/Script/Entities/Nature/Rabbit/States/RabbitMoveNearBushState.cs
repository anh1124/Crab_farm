using UnityEngine;
using Script.Game.Other.Nature;
using Script.Utils;
using Script.Data_Model;

namespace Script.Game.Other.Nature.States
{
    public class RabbitMoveNearBushState : RabbitState
    {
        public override void Enter(RabbitBehavior ctx)
        {
#if UNITY_EDITOR
            if (ctx.IsDebug) Debug.Log("[RabbitMoveNearBushState] Enter");
#endif
            ctx.PlayAnimation("move");
        }

        public override void UpdateBehavior(RabbitBehavior ctx)
        {
            if (ctx.RabbitBush == null)
            {
                ctx.ChangeState(new RabbitIdleState());
                return;
            }

            if (PrefabData.Instance == null)
            {
                Debug.LogError("[RabbitMoveNearBushState] PrefabData.Instance is null!");
                ctx.ChangeState(new RabbitIdleState());
                return;
            }

            // Tính target ngẫu nhiên quanh Bush — dùng WorldUtility.GetRandomPositionNearby thay vì inline
            if (_targetPosition == Vector3.zero)
            {
                float maxDistance = PrefabData.Instance.maxDistanceFromBush;
                _targetPosition = WorldUtility.GetRandomPositionNearby(ctx.RabbitBush.transform.position, maxDistance);
                _targetPosition.y = 0f; // Cố định Y = 0 (game 2D)
                _targetPosition.z = 0f;
                ctx.SetTargetPosition(_targetPosition);

#if UNITY_EDITOR
                if (ctx.IsDebug) Debug.Log($"[RabbitMoveNearBushState] New target: {_targetPosition}");
#endif
            }
        }

        public override void Update(RabbitBehavior ctx)
        {
            if (!ctx.CanMove) return;
            if (_targetPosition == Vector3.zero) return;

            Vector2 currentPosition = ctx.transform.position;
            float newX = Mathf.MoveTowards(currentPosition.x, _targetPosition.x, ctx.Speed * Time.deltaTime);
            ctx.transform.position = new Vector3(newX, 0f, 0f);

            MovementUtility.FlipDirection(ctx.gameObject, _targetPosition);

            // Dùng MovementUtility.IsMoving thay vì magic number 0.001f
            ctx.IsMove = MovementUtility.IsMoving(currentPosition.x, newX);

#if UNITY_EDITOR
            if (ctx.IsDebug) Debug.Log($"[RabbitMoveNearBushState] Moving → {new Vector3(newX, 0f, 0f)}");
#endif

            // Dùng MovementUtility.HasReachedDestination thay vì magic number 0.1f
            if (MovementUtility.HasReachedDestination(ctx.transform.position, _targetPosition))
            {
                _targetPosition = Vector3.zero;
                ctx.SetTargetPosition(Vector3.zero);

#if UNITY_EDITOR
                if (ctx.IsDebug) Debug.Log("[RabbitMoveNearBushState] Reached target, picking new one next frame.");
#endif
            }
        }

        public override void Exit(RabbitBehavior ctx)
        {
#if UNITY_EDITOR
            if (ctx.IsDebug) Debug.Log("[RabbitMoveNearBushState] Exit");
#endif
            _targetPosition = Vector3.zero;
        }
    }
}

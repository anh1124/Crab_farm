using UnityEngine;
using Script.Game.Other.Nature;
using Script.Utils;

namespace Script.Game.Other.Nature.States
{
    public class RabbitIdleState : RabbitState
    {
        public override void Enter(RabbitBehavior ctx)
        {
#if UNITY_EDITOR
            if (ctx.IsDebug) Debug.Log("[RabbitIdleState] Enter");
#endif
            ctx.PlayAnimation("idle");
        }

        public override void UpdateBehavior(RabbitBehavior ctx)
        {
#if UNITY_EDITOR
            if (ctx.IsDebug) Debug.Log("[RabbitIdleState] Checking for bush and hunt zone");
#endif

            // ── 1. Có BushID nhưng chưa có tham chiếu (vừa load từ save) ────────
            if (!string.IsNullOrEmpty(ctx.BushID))
            {
                if (ctx.RabbitBush != null)
                {
                    // Đã có tham chiếu → chuyển sang đi về gần Bush
                    ctx.ChangeState(new RabbitMoveNearBushState());
                    return;
                }

                // Thử tìm lại Bush qua Manager (chỉ cần 1 lần sau khi load)
                // Đây là trường hợp hợp lệ duy nhất để Rabbit tự lookup qua Manager
                var foundBush = Script.Manager.NatureManager.RabbitBushManager.Instance?.GetBushByID(ctx.BushID);
                if (foundBush != null)
                {
                    foundBush.RegisterRabbit(ctx);
                    ctx.ChangeState(new RabbitMoveNearBushState());
                }
                else
                {
#if UNITY_EDITOR
                    if (ctx.IsDebug) Debug.Log($"[RabbitIdleState] Waiting for bush ID: {ctx.BushID} — not found yet.");
#endif
                }

                // Không tiếp tục tìm Hunt Zone khi đang đợi Bush
                return;
            }

            // ── 2. Không có Bush → tìm khu vực săn gần nhất ──────────────────────
            var side = WorldUtility.GetNearestHuntingSide(ctx.transform.position);
            if (side != KingdomSide.None)
            {
                // Chỉ cần xác nhận có khu vực hợp lệ; tính vị trí cụ thể sẽ do State mới xử lý
                ctx.ChangeState(new RabbitMoveToHuntingAreaState());
                return;
            }

            // ── 3. Giữ Idle — không có Bush, không có Hunt Zone hợp lệ ──────────
        }

        public override void Update(RabbitBehavior ctx)
        {
            ctx.IsMove = false;
        }

        public override void Exit(RabbitBehavior ctx)
        {
#if UNITY_EDITOR
            if (ctx.IsDebug) Debug.Log("[RabbitIdleState] Exit");
#endif
        }
    }
}

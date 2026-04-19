using UnityEngine;
using Script.Game.Other.Nature;

namespace Script.Game.Other.Nature.States
{
    public class RabbitDeadState : RabbitState
    {
        public override void Enter(RabbitBehavior ctx)
        {
#if UNITY_EDITOR
            if (ctx.IsDebug) Debug.Log("[RabbitDeadState] Enter");
#endif
            ctx.PlayAnimation("die");

            // Phát âm thanh khi rabbit chết
            if (Script.Audio.AudioManager.Instance != null)
                Script.Audio.AudioManager.Instance.PlaySFXByKey("NPC_Blood_Dead", ctx.transform.position);
        }

        public override void UpdateBehavior(RabbitBehavior ctx)
        {
            // Dead state — no behavior, waiting for CleanupAndDestroy coroutine
        }

        public override void Update(RabbitBehavior ctx)
        {
            ctx.IsMove = false;
        }

        public override void Exit(RabbitBehavior ctx)
        {
#if UNITY_EDITOR
            if (ctx.IsDebug) Debug.Log("[RabbitDeadState] Exit");
#endif
        }
    }
}

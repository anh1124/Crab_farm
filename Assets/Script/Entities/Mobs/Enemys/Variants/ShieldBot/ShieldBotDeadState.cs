using Script.Game.Enemy.Core;
using Script.Game.Enemy.States;
using Script.Audio;
using UnityEngine;

namespace Script.Game.Enemy.Variants.ShieldBot
{
    public class ShieldBotDeadState : DeadState
    {
        protected override void PlayDeathSound(EnemyContext ctx)
        {
            // Phát âm thanh Power_Down khi ShieldBot chết
            var emitter = ctx.Self.GetComponent<AudioEmitter>();
            if (emitter != null)
                emitter.PlaySound("Power_Down", PlaySoundConfig.Default);
        }
    }
}

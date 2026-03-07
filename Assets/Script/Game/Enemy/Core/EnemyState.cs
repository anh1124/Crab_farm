using UnityEngine;

namespace Script.Game.Enemy.Core
{
    public abstract class EnemyState
    {
        public abstract void Enter(EnemyContext ctx);
        public abstract void Update(EnemyContext ctx);
        public abstract void Exit(EnemyContext ctx);
    }
}

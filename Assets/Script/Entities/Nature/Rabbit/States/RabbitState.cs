using UnityEngine;
using Script.Game.Other.Nature;

namespace Script.Game.Other.Nature.States
{
    public abstract class RabbitState
    {
        protected Vector3 _targetPosition;

        public Vector3 TargetPosition => _targetPosition;

        public abstract void Enter(RabbitBehavior ctx);

        public virtual void UpdateBehavior(RabbitBehavior ctx)
        {
        }

        public virtual void Update(RabbitBehavior ctx)
        {
        }

        public abstract void Exit(RabbitBehavior ctx);
    }
}

using UnityEngine;

namespace Script.Game.Mobs.Denizen.Core
{
    /// <summary>
    /// DenizenJob: Linh hồn (Soul) của cư dân.
    /// CHỈ: Điều phối trạng thái (State Machine) và tham chiếu Context.
    /// KHÔNG: Giữ logic chiến đấu, kinh tế trực tiếp. Logic nằm ở State hoặc Job con.
    /// </summary>
    public abstract class DenizenJob : MonoBehaviour
    {
        public DenizenState CurrentState { get; protected set; }
        
        [Tooltip("Optional: Animator riêng của từng Job. Nếu để trống sẽ tự lấy mặc định từ DenizenManager.")]
        public RuntimeAnimatorController JobController; 

        [Header("Runtime Context")]
        [Tooltip("Tài liệu tham khảo chung của cư dân (Cơ thể, Status, Animator...)")]
        public DenizenContext Context;

        [Header("Debug")]
        public string currentStateName;

        public void InjectContext(DenizenContext ctx)
        {
            Context = ctx;
        }

        /// <summary>
        /// Logic chung cho mọi ChangeState: Exit → Reset Anim → Assign → Enter.
        /// Từng Job gọi hàm này bên trong ChangeState() riêng của nó.
        /// </summary>
        protected void DoChangeState(DenizenState newState)
        {
            if (newState == null || CurrentState == newState) return;

            CurrentState?.Exit(Context);
            ResetAllAnimatorParams();

            CurrentState = newState;
            currentStateName = newState.GetType().Name;
            if (Context?.Status != null)
                Context.Status.currentStateName = currentStateName;

            CurrentState.Enter(Context);
        }

        private void ResetAllAnimatorParams()
        {
            if (Context?.Animator == null) return;
            foreach (var p in Context.Animator.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool)
                    Context.Animator.SetBool(p.name, false);
                if (p.type == AnimatorControllerParameterType.Trigger)
                    Context.Animator.ResetTrigger(p.name);
            }
        }

        // Virtual hooks for routing (satisfied by job-specific implementations)
        public virtual void ReceiveDamage(float damage, GameObject instigator) { }
        public virtual void HandleAnimationEvent(string eventName) { }
        public virtual bool HandleItem(GameObject item) => false;
        public virtual void UpdateBehavior() { }

        // Tactical Hooks for Global Managers (GuardManager, SquadSystem, etc.)
        public virtual void OnGuardManagerStatusChanged(bool isAttack) { }
        public virtual void ForceRetreat(GameObject safeWall) { }
    }
}

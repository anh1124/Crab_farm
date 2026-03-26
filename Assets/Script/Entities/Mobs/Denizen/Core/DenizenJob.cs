using UnityEngine;
using Script.Data_Model.Interfaces;

namespace Script.Game.Mobs.Denizen.Core
{
    /// <summary>
    /// Base class cho các Job (VagrantJob, VillagerJob, ArcherJob).
    /// Quản lý State Machine và vòng đời của Role.
    /// </summary>
    public abstract class DenizenJob : MonoBehaviour, INPCBehavior, IItemHandle
    {
        protected DenizenContext Context;
        public DenizenState CurrentState { get; protected set; }

        [Header("Animation Settings")]
        [SerializeField] private RuntimeAnimatorController jobController; // Slot để kéo .controller vào

        [Header("Debug Info")]
        [SerializeField] private string currentStateName; // Hiển thị state hiện tại trên Inspector

        public RuntimeAnimatorController JobController => jobController;

        #region Context & Lifecycle
        public void InjectContext(DenizenContext ctx)
        {
            Context = ctx;
        }

        public Animator GetAnimator() => Context != null ? Context.Animator : null;
        #endregion

        protected abstract DenizenState GetInitialState();
        protected abstract void PreAllocateStates();

        public void ChangeState(DenizenState next)
        {
            if (next == CurrentState) return;

            CurrentState?.Exit(Context);
            ResetAnimatorParams();
            CurrentState = next;
            
            // Cập nhật tên state để User quan sát trên Inspector
            currentStateName = next != null ? next.GetType().Name : "None";
            
            CurrentState?.Enter(Context);
        }

        [ContextMenu("Debug: Print State Info")]
        public void PrintStateDebug()
        {
            if (CurrentState == null)
            {
                Debug.Log($"[DEBUG] Job: {GetType().Name} | State: NULL");
                return;
            }

            string info = CurrentState.GetDebugInfo(Context);
            Debug.Log($"<color=cyan>[DEBUG]</color> <b>Job: {GetType().Name}</b> | <b>State: {CurrentState.GetType().Name}</b>\nDetails: {info}");
        }

        protected virtual void ResetAnimatorParams()
        {
            Animator anim = GetAnimator();
            if (anim == null) return;
            
            foreach (var p in anim.parameters)
            {
                if (p.type == AnimatorControllerParameterType.Bool) anim.SetBool(p.name, false);
                if (p.type == AnimatorControllerParameterType.Trigger) anim.ResetTrigger(p.name);
            }
        }

        protected virtual void OnEnterJob() { }
        protected virtual void OnExitJob() { }

        public virtual void UpdateBehavior()
        {
            // [SCAN INTEGRATION] Chủ động quét tia và vòng tròn (do Helper đã gỡ Update loop)
            if (Context != null)
            {
                if (Context.RayCaster != null && Context.RayCaster.isActive)
                {
                    Context.RayCaster.DrawRay();
                }

                if (Context.CircleDetect != null && Context.CircleDetect.isActive)
                {
                    Context.CircleDetect.ScanCircle();
                }
            }

            CurrentState?.Update(Context);
        }

        private void Update()
        {
            CurrentState?.FrameUpdate(Context);
        }

        protected virtual void OnEnable()
        {
            // [DETECTOR RESET] Unactive cả 2 detect trước khi Job mới bật lại cái nó cần
            if (Context != null)
            {
                if (Context.RayCaster != null)
                {
                    Context.RayCaster.isActive = false;
                    Context.RayCaster.isDrawRay = false;
                }
                if (Context.CircleDetect != null)
                {
                    Context.CircleDetect.isActive = false;
                    Context.CircleDetect.isShowGizmoUser = false;
                }
            }

            OnEnterJob();
            ChangeState(GetInitialState());
        }

        protected virtual void OnDisable()
        {
            CurrentState?.Exit(Context);
            CurrentState = null;
            currentStateName = "Disabled";
            OnExitJob();
        }

        public virtual void OnObjectsDetected(System.Collections.Generic.List<GameObject> detectedObjects) { }
        public virtual void HandleAnimationEvent(string eventName) { }
        
        // IItemHandle implementation - Đẩy logic xuống các Job cụ thể
        public virtual bool HandleItem(GameObject item) => false;

        protected abstract void Awake();
    }
}

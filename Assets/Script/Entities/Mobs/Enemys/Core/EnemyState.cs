using UnityEngine;
using Script.Utils;

namespace Script.Game.Enemy.Core
{
    public abstract class EnemyState
    {
        // GameObject đích đến (do Controller hoặc State con gán)
        protected GameObject _moveTarget = null;
        
        // Cờ hiệu nội bộ để gọi Trigger Animator một lần
        protected bool _isMovingInternal = false;

        // [MỚI] Quyền của State: Cho phép di chuyển khi đang ở State này hay không?
        // Mặc định là True (như MoveToTargetState), các State đứng im (như Attack) sẽ ghi đè thành False.
        protected virtual bool AllowMovementInState => true;

        protected bool _isCanMove(EnemyContext ctx) => ctx.Status != null && ctx.Status.CanMove;

        public abstract void Enter(EnemyContext ctx);
        
        public virtual void UpdateBehavior(EnemyContext ctx) 
        { 
            // Cập nhật đích đến dựa trên lệnh tập trung từ Base
            if (ctx.CurrentTarget != null) 
            {
                _moveTarget = ctx.CurrentTarget;
            }
            else _moveTarget = null;
        }

        public virtual void Update(EnemyContext ctx)
        {
            if (ctx.Controller == null || ctx.Status == null) return;
            Movement(ctx);
        }

        /// <summary>
        /// Hàm di chuyển lõi: Phải vượt qua 3 lớp kiểm tra (Skill, Status, State)
        /// </summary>
        protected virtual void Movement(EnemyContext ctx)
        {
            // 1. [SKILL CHECK]: Skill đang thi triển có cho phép đi không?
            bool skillAllows = !ctx.IsSkillCasting || ctx.SkillAllowsMovement;

            // 2. [STATUS CHECK]: Trạng thái cơ thể (Đóng băng, Choáng...) có cho phép đi không?
            bool statusAllows = _isCanMove(ctx);

            // 3. [STATE CHECK]: Bản thân trạng thái hiện tại (Attack, Idle...) có cho phép đi không?
            bool stateAllows = AllowMovementInState;

            // THỰC THI: Cả 3 cùng đồng ý + Có mục tiêu thì mới nhấc chân
            if (_moveTarget != null && skillAllows && statusAllows && stateAllows)
            {
                if (ctx.Status != null) ctx.Status.IsMoving = true;

                if (!_isMovingInternal)
                {
                    _isMovingInternal = true;
                    ctx.Controller.SetTriggerSafely("Walk");
                }
                
                HandleMovement(ctx, _moveTarget.transform.position);
            }
            else
            {
                StopMovement(ctx);
            }
        }

        protected virtual void StopMovement(EnemyContext ctx)
        {
            if (ctx.Status != null) ctx.Status.IsMoving = false;

            if (_isMovingInternal)
            {
                _isMovingInternal = false;
                OnIdleUpdate(ctx);
            }
        }

        protected virtual void OnIdleUpdate(EnemyContext ctx)
        {
            ctx.Controller.SetTriggerSafely("Idle");
        }
        
        // --- QUAN TRỌNG: Gọi hàm này mỗi khi quái bước vào state mới ---
        public virtual void ResetFlags()
        {
            _isMovingInternal = false;
        }

        public abstract void Exit(EnemyContext ctx);

        protected virtual void HandleMovement(EnemyContext ctx, Vector3 target)
        {
            MovementUtility.FaceTarget(ctx.Self, target);
            MovementUtility.MoveTowardsTarget(ctx.Self, target, ctx.Status.MoveSpeed);
        }
        
        // Signal khi hoàn thành hoạt ảnh tấn công
        public virtual void OnAttackAnimationComplete(EnemyContext ctx) { }

        // Signal khi hoàn thành hoạt ảnh Chết
        public virtual void OnDieAnimationComplete(EnemyContext ctx) { }
    }
}

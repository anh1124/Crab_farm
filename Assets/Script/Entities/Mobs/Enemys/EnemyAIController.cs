using System;
using System.Collections.Generic; 
using System.Linq;
using Script.Game.Enemy.Core;
using Script.Game.Enemy.States;
using Script.Data_Model.Interfaces;
using Script.Manager.GamePlayManager;
using UnityEngine;

namespace Script.Game.Enemy.Core
{
    [RequireComponent(typeof(Script.Game.PlayerCharacter.Skills.SkillManager))]
    public class EnemyAIController : MonoBehaviour, INPCBehavior
    {
        [Header("Debug Info")]
        public bool isDebug = false;
        public string currentStateName;
        public GameObject currentTarget;
        public GameObject currentPortal;

        public event Action<EnemyAIController> OnDespawn;

        protected EnemyContext _ctx;
        protected EnemyState _currentState;

        // Pre-allocated States
        protected IdleState _idleState;
        protected MoveToTargetState _moveState;
        protected ReturnToPortalState _returnState;
        protected AttackState _attackState;
        protected DeadState _deadState;

        public EnemyState IdleStateInstance => _idleState;
        public EnemyState MoveStateInstance => _moveState;
        public EnemyState ReturnStateInstance => _returnState;
        public EnemyState AttackStateInstance => _attackState;

        [SerializeField] protected string[] _animatorTriggers;

        private void Awake()
        {
            EnemyStatus status = GetComponent<EnemyStatus>();
            Animator animator = GetComponent<Animator>();
            RayCastHelper rch = GetComponent<RayCastHelper>();
            
            // Xóa bỏ dòng overwrite isDebug để giữ nguyên giá trị bạn tích ở Inspector
            
            _ctx = new EnemyContext
            {
                Self           = gameObject,
                Transform      = transform,
                Animator       = animator,
                Controller     = this,
                Status         = status,
                TargetManager  = EnemyTargetManager.Instance, 
                RaycastHelper  = rch,
                SkillManager   = GetComponent<Script.Game.PlayerCharacter.Skills.SkillManager>(),
                IsAlive        = true
            };

            // Pre-allocate instances
            _idleState   = new IdleState();
            _moveState   = GetMoveState() as MoveToTargetState;
            _returnState = new ReturnToPortalState();
            _attackState = GetAttackState(); 
            _deadState   = new DeadState();

            _currentState = _idleState;
            UpdateAnimatorTriggers();

            if (status != null) status.OnDieEvent += OnDeath;
        }

        public void UpdateAnimatorTriggers()
        {
            if (_ctx.Animator != null && _ctx.Animator.runtimeAnimatorController != null)
            {
                _animatorTriggers = _ctx.Animator.parameters
                    .Where(p => p.type == AnimatorControllerParameterType.Trigger)
                    .Select(p => p.name)
                    .ToArray();
                if (isDebug) Debug.Log($"[EnemyAIController] Đã nạp {_animatorTriggers.Length} triggers cho {gameObject.name}: {string.Join(", ", _animatorTriggers)}");
            }
        }

        private void OnEnable() => NPCUpdateManager.Instance?.Register(this);
        private void OnDisable() => NPCUpdateManager.Instance?.Unregister(this);

        public virtual void UpdateBehavior()
        {
            // [LIVE CHECK]: Nếu chết, nhảy ngay về DeadState (chạy Anim Die một lần)
            if (!_ctx.IsAlive) 
            {
                if (!(_currentState is Script.Game.Enemy.States.DeadState)) 
                    ChangeState(_deadState);
                return;
            }
            
            // [HÀNG RÀO BẢO VỆ CẤP CAO]: Nếu đang dở dang chiêu thức, CHỈ UPDATE STATE HIỆN TẠI, không cho phép đổi State
            if (_ctx.IsSkillCasting)
            {
                if (isDebug) Debug.Log($"<color=white>[AI Controller]</color> {gameObject.name} Casting locked - Skipping high-level logic");
                _currentState?.UpdateBehavior(_ctx);
                return;
            }

            // [TIME-BASED STATE TRANSITION]
            if (Script.Manager.WorldManager.GameTimeManager.Instance != null)
            {
                bool isDay = Script.Manager.WorldManager.GameTimeManager.Instance.IsDaytime;
                if (!(_currentState is Script.Game.Enemy.States.AttackState))
                {
                    // [CORRECTED]: Đêm thì Tiến công, Ngày thì Rút lui
                    if (!isDay)
                    {
                        var target = _ctx.TargetManager?.GetTarget(gameObject);
                        _ctx.CurrentTarget = target;
                        
                        if (target != null && !(_currentState is Script.Game.Enemy.States.MoveToTargetState)) 
                        {
                            ChangeState(_moveState);
                        }
                        else if (target == null && !(_currentState is Script.Game.Enemy.States.IdleState)) ChangeState(_idleState);
                    }
                    else
                    {
                        // Ngày: Tìm Portal rút về
                        if (_ctx.HomePortal == null && Script.Manager.GamePlayManager.EnemyPortalManager.Instance != null)
                        {
                            var portalMgr = Script.Manager.GamePlayManager.EnemyPortalManager.Instance;
                            var townCenter = Script.Utility.GetTownCenter();
                            if (townCenter != null)
                                _ctx.HomePortal = _ctx.Transform.position.x > townCenter.transform.position.x ? portalMgr.portalR : portalMgr.portalL;
                        }

                        _ctx.CurrentTarget = _ctx.HomePortal;

                        if (_ctx.HomePortal != null && !(_currentState is Script.Game.Enemy.States.ReturnToPortalState)) 
                        {
                            ChangeState(_returnState);
                        }
                        else if (_ctx.HomePortal == null && !(_currentState is Script.Game.Enemy.States.IdleState)) ChangeState(_idleState);
                    }
                }
            }

            // Đồng bộ debug cho Editor
            currentTarget = _ctx.CurrentTarget;
            currentPortal = _ctx.HomePortal;
            if (isDebug && _currentState != null) currentStateName = _currentState.GetType().Name;

            // [HÀNG RÀO QUAY ĐẦU]: Đảm bảo luôn nhìn về hướng đích đến cuối cùng
            if (_ctx.CurrentTarget != null && !(_currentState is Script.Game.Enemy.States.DeadState))
            {
                Script.Utils.MovementUtility.FaceTarget(gameObject, _ctx.CurrentTarget.transform.position);
            }

            _currentState?.UpdateBehavior(_ctx);
        }

        // --- ANIMATION EVENTS ---
        
        /// <summary>
        /// Frame Event cuối cùng trong hoạt ảnh "Die" (Gắn tại Animator)
        /// </summary>
        public void AnimEvent_OnDieComplete()
        {
            _currentState?.OnDieAnimationComplete(_ctx);
        }

        public void OnObjectsDetected(List<GameObject> detectedObjects) { }

        public void Update() => _currentState?.Update(_ctx);

        public void ChangeState(EnemyState newState)
        {
            if (newState == null || _currentState == newState) return;

            _currentState?.Exit(_ctx);
            _currentState = newState;
            
            // [CỰC KỲ QUAN TRỌNG]: Đặt lại cờ hiệu di chuyển để State mới có thể gọi Trigger Animator
            _currentState.ResetFlags();
            
            _currentState.Enter(_ctx);
            if (isDebug) currentStateName = _currentState.GetType().Name;
        }

        public void ResetEnemy(GameObject portal)
        {
            _ctx.IsAlive = true;
            _ctx.HomePortal = portal;
            _ctx.CurrentTarget = null;
            UpdateAnimatorTriggers(); // Đảm bảo trigger luôn sẵn sàng khi spawn lại
            ChangeState(_idleState);
        }

        public void InjectDependencies(EnemyTargetManager targetMgr, Action<GameObject> returnAction) 
        { 
            _ctx.TargetManager = targetMgr;
            _ctx.ReturnToPoolAction = returnAction;
        }

        public void Despawn() => OnDespawn?.Invoke(this);

        private void OnDeath(GameObject go)
        {
            _ctx.IsAlive = false; // Đánh dấu đã chết để UpdateBehavior nhận diện
            ChangeState(_deadState);
        }

        public virtual EnemyState GetMoveState() => new MoveToTargetState();
        public virtual AttackState GetAttackState() => new AttackState(); 

        [ContextMenu("Print Detailed Debug")]
        public virtual void PrintDetailedDebug()
        {
            if (_ctx == null) return;
            
            string statusInfo = _ctx.Status != null ? $"HP: {_ctx.Status.CurrentHp}/{_ctx.Status.MaxHp}, Spd: {_ctx.Status.MoveSpeed}" : "NULL";
            string targetInfo = _ctx.CurrentTarget != null ? $"{_ctx.CurrentTarget.name} (Tag: {_ctx.CurrentTarget.tag})" : "None";
            
            string skillInfo = "";
            if (_ctx.SkillManager != null)
            {
                for (int i = 0; i < _ctx.SkillManager.SlotCount; i++)
                {
                    var skill = _ctx.SkillManager.GetSkill(i);
                    skillInfo += $"\n   - Slot {i}: {(skill != null ? skill.skillName : "Empty")} | State: {_ctx.SkillManager.GetState(i)} | CD: {_ctx.SkillManager.GetCooldownRemaining(i):F1}s";
                }
            }

            string allDetected = "";
            if (_ctx.RaycastHelper != null)
            {
                var hits = _ctx.RaycastHelper.GetListHitObjects();
                allDetected = hits != null ? string.Join(", ", hits.Select(h => h == null ? "NULL" : $"{h.name}({h.tag})")) : "None";
            }

            // Thêm thông tin Animator thực tế
            string currentAnim = "Unknown";
            if (_ctx.Animator != null)
            {
                var info = _ctx.Animator.GetCurrentAnimatorStateInfo(0);
                if (info.IsName("idle")) currentAnim = "idle";
                else if (info.IsName("walk")) currentAnim = "walk";
                else if (info.IsName("shock attack")) currentAnim = "shock attack";
                else if (info.IsName("Die")) currentAnim = "Die";
            }

            // Thêm danh sách Trigger để soi lỗi hoa thường
            string availableTriggers = _animatorTriggers != null ? string.Join(", ", _animatorTriggers) : "None";

            Debug.Log($"<color=orange>[AI FULL REPORT: {gameObject.name}]</color>\n" +
                      $" - State: {currentStateName}\n" +
                      $" - Animator: {currentAnim}\n" +
                      $" - Triggers Available: [{availableTriggers}]\n" +
                      $" - Casting: {_ctx.IsSkillCasting} | MoveAllowed: {_ctx.SkillAllowsMovement}\n" +
                      $" - Target: {targetInfo}\n" +
                      $" - Detected All: [{allDetected}]\n" +
                      $" - Status: {statusInfo}\n" +
                      $" - Skills: {skillInfo}");
        }

        public void SetTriggerSafely(string triggerName)
        {
            if (_ctx.Animator == null) return;
            
            // Xóa khoảng trắng thừa (Tránh lỗi 'shock attack ' so với 'shock attack')
            string cleanTrigger = triggerName.Trim();

            // Tìm chính xác tên Trigger trong list (Lấy cái tên 'xịn' có chữ hoa chữ thường đúng chuẩn Animator)
            string exactTrigger = _animatorTriggers?.FirstOrDefault(t => t.Equals(cleanTrigger, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(exactTrigger))
            {
                _ctx.Animator.SetTrigger(exactTrigger);
            }
        }
    }
}

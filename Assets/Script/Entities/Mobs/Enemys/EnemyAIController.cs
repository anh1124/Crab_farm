using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Script.Game.Enemy.Core;
using Script.Game.Enemy.States;
using Script.Data_Model.Interfaces;
using Script.Manager.GamePlayManager;

namespace Script.Game.Enemy
{
    [RequireComponent(typeof(EnemyStatus), typeof(Animator))]
    public class EnemyAIController : MonoBehaviour, INPCBehavior
    {
        protected EnemyState _currentState;
        protected EnemyContext _ctx;
        protected string[] _animatorTriggers;

        // Pre-allocated states
        private Script.Game.Enemy.States.IdleState _idleState;
        private Script.Game.Enemy.States.MoveToTargetState _moveState;
        private Script.Game.Enemy.States.ReturnToPortalState _returnState;
        private Script.Game.Enemy.States.DeadState _deadState;

        public Script.Game.Enemy.States.IdleState IdleStateInstance => _idleState;
        public Script.Game.Enemy.States.MoveToTargetState MoveStateInstance => _moveState;
        public Script.Game.Enemy.States.ReturnToPortalState ReturnStateInstance => _returnState;
        public Script.Game.Enemy.States.DeadState DeadStateInstance => _deadState;

        [Header("Debug Info")]
        [SerializeField] private string currentStateName = "None";
        [SerializeField] private GameObject currentTarget;
        [SerializeField] private GameObject currentPortal;
        [SerializeField] private float currentAttackTimer;

        public float DebugAttackTimer 
        { 
            get => currentAttackTimer; 
            set => currentAttackTimer = value; 
        }

        // Sự kiện báo hiệu bị tiêu diệt hoặc thu hồi vào pool (cho GuardManager)
        public event System.Action<EnemyAIController> OnDespawn;



        private void Awake()
        {
            EnemyStatus status = GetComponent<EnemyStatus>();
            Animator animator = GetComponent<Animator>();
            RayCastHelper rch = GetComponent<RayCastHelper>();
            
            _ctx = new EnemyContext
            {
                Self           = gameObject,
                Animator       = animator,
                Controller     = this,
                Status         = status,
                // Fallback nếu không có Pool inject (Test mode)
                TargetManager  = EnemyTargetManager.Instance, 
                ReturnToPoolAction = null,
                RaycastHelper  = rch,
                IsAlive        = true
            };

            // Pre-allocate instances
            _idleState   = new Script.Game.Enemy.States.IdleState();
            _moveState   = GetMoveState() as Script.Game.Enemy.States.MoveToTargetState;
            _returnState = new Script.Game.Enemy.States.ReturnToPortalState();
            _deadState   = new Script.Game.Enemy.States.DeadState();

            // Cache Trigger names để tránh memory allocation lặp lại trong ChangeState
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                _animatorTriggers = animator.parameters
                    .Where(p => p.type == AnimatorControllerParameterType.Trigger)
                    .Select(p => p.name)
                    .ToArray();
            }
            else
            {
                _animatorTriggers = new string[0];
            }

            // Đăng ký Event Lắng nghe cái chết
            if (status != null)
            {
                status.OnDieEvent += OnDeath;
            }
        }

        private void OnEnable()
        {
            NPCUpdateManager.Instance?.Register(this);
        }

        private void OnDisable()
        {
            NPCUpdateManager.Instance?.Unregister(this);
            // Khi bị tắt (vào pool) hoặc bị hủy, báo cho GuardManager gỡ khỏi danh sách theo dõi
            OnDespawn?.Invoke(this); 
        }

        private void Start()
        {
            // Bắt đầu bằng Idle state
            ChangeState(_idleState);

            // Đăng ký Event Scanner nếu Enemy này có mang công cụ quét RayCastHelper (như ShieldBot)
            var rch = GetComponent<RayCastHelper>();
            if (rch != null)
            {
                // [SYNC TAGS] Đồng bộ danh sách Tag cần phát hiện từ TargetManager
                if (EnemyTargetManager.Instance != null)
                {
                    rch.tagsToDetect = new List<string>(EnemyTargetManager.Instance.advanceTags);
                    rch.targetLayer = EnemyTargetManager.Instance.detectionLayer;
                }
                
                rch.OnDetectedObjectsChanged += OnObjectsDetected;
            }
        }

        private void OnDestroy()
        {
            var rch = GetComponent<RayCastHelper>();
            if (rch != null)
            {
                rch.OnDetectedObjectsChanged -= OnObjectsDetected;
            }
            
            if (_ctx != null && _ctx.Status != null)
            {
                _ctx.Status.OnDieEvent -= OnDeath;
            }
        }

        // Event Receiver: Được gọi tự động mỗi khi máy dò RayCast quét được đồ
        public virtual void OnObjectsDetected(System.Collections.Generic.List<GameObject> detectedObjects)
        {
            if (!_ctx.IsAlive || detectedObjects == null || detectedObjects.Count == 0) return;

            // Chỉ dùng sự kiện phát hiện mục tiêu để "Đánh thức" AI đang đứng chơi
            bool shouldChangeState = false;

            if (_currentState is IdleState || _currentState is ReturnToPortalState) 
            {
                shouldChangeState = true;
            }

            if (shouldChangeState)
            {
                ChangeState(GetMoveState());
            }
        }

        public void InjectDependencies(EnemyTargetManager targetManager, System.Action<GameObject> returnToPoolAction)
        {
            if (_ctx != null)
            {
                _ctx.TargetManager = targetManager;
                _ctx.ReturnToPoolAction = returnToPoolAction;
            }
        }

        public virtual void UpdateBehavior()
        {
            if (!_ctx.IsAlive) return;

            // Đồng bộ dữ liệu Greed từ EnemyStatus vào Context để State có thể nhận biết
            if (_ctx.Status != null)
            {
                _ctx.CurrentGreed = _ctx.Status.Coin;
            }

            // [SCAN] Chủ động quét tia để cập nhật danh sách vật cản (do Helper đã gỡ Update loop)
            _ctx.RaycastHelper?.DrawRay();

            if (_ctx.Status != null && _ctx.Status.debugEnemy) 
                Debug.Log($"[EnemyAI] {gameObject.name} ticking in state: {(_currentState != null ? _currentState.GetType().Name : "NULL")}");

            // Cập nhật giá trị hiển thị Debug
            if (_currentState != null)
                currentStateName = _currentState.GetType().Name;
            currentTarget = _ctx.CurrentTarget;
            currentPortal = _ctx.HomePortal;

            _currentState?.Update(_ctx);
        }

        private void Update()
        {
            if (!_ctx.IsAlive) return;

            // Interpolation movement liên tục ở cấp độ Frame (60Hz) thay vì Tick (5Hz)
            _currentState?.FrameUpdate(_ctx);
        }

        public void ChangeState(EnemyState newState)
        {
            _currentState?.Exit(_ctx);

            // Xóa sạch trigger cũ để không bị pending sai state
            if (_animatorTriggers != null)
            {
                foreach (var t in _animatorTriggers)
                {
                    _ctx.Animator.ResetTrigger(t);
                }
            }

            _currentState = newState;
            _currentState.Enter(_ctx);
        }

        // Gọi từ Event OnDieEvent của EnemyStatus
        public virtual void OnDeath(GameObject deadObject)
        {
            if (!_ctx.IsAlive) return; // Guard double-call

            _ctx.IsAlive = false;
            OnDespawn?.Invoke(this); // Gọi event báo tử
            ChangeState(_deadState);
        }

        // Gọi từ pool khi lấy enemy ra để tái sử dụng
        public void ResetEnemy(GameObject homePortal)
        {
            _currentState?.Exit(_ctx);
            _currentState = null;

            _ctx.IsAlive        = true;
            _ctx.CurrentGreed   = 0;
            _ctx.CurrentTarget  = null;
            _ctx.HomePortal     = homePortal;
            _ctx.PendingRecycle = false;

            // Reset Animation Triggers để tránh quái vừa spawn đã tung đòn của kiếp trước
            if (_ctx.Animator != null && _animatorTriggers != null)
            {
                foreach (var t in _animatorTriggers)
                {
                    _ctx.Animator.ResetTrigger(t);
                }
            }

            // Sync lại GreedThreshold từ Status nếu có (hoặc set default)
            if (_ctx.Status != null)
            {
                _ctx.GreedThreshold = _ctx.Status.MaxCoins;
            }

            ChangeState(_idleState);
        }

        // Factory Method để các Enemy đặc thù (như ShieldBot) có thể ghi đè (Override)
        // và trả về Custom State của riêng nó.
        public virtual EnemyState GetMoveState()
        {
            return new MoveToTargetState();
        }

        // ==========================================
        // ANIMATION EVENTS
        // ==========================================
        public void AnimEvent_DeathComplete()
        {
            // Trả xác về pool NGAY LẬP TỨC sau khi diễn xong animation gục ngã
            if (_ctx.ReturnToPoolAction != null)
            {
                _ctx.ReturnToPoolAction(_ctx.Self);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void AnimEvent_EndAttack()
        {
            if (_currentState is MoveToTargetState moveState)
            {
                moveState.OnAttackAnimationComplete(_ctx);
            }
            // For other attack states if they arise in the base AI
        }

        public void AnimEvent_EndAttack_Return()
        {
            if (_currentState is ReturnToPortalState returnState)
            {
                returnState.OnAttackAnimationComplete(_ctx);
            }
        }
    }
}

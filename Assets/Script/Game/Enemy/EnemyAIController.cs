using System.Linq;
using UnityEngine;
using Script.Game.Enemy.Core;
using Script.Game.Enemy.States;

namespace Script.Game.Enemy
{
    [RequireComponent(typeof(EnemyStatus), typeof(Animator))]
    public class EnemyAIController : MonoBehaviour
    {
        protected EnemyState _currentState;
        protected EnemyContext _ctx;
        protected string[] _animatorTriggers;

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
            
            _ctx = new EnemyContext
            {
                Self           = gameObject,
                Animator       = animator,
                Controller     = this,
                Status         = status,
                TargetManager  = EnemyTargetManager.Instance,
                Pool           = EnemyPool.Instance,
                IsAlive        = true
            };

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

        private void Start()
        {
            // Bắt đầu bằng Idle state
            ChangeState(new IdleState());

            // Đăng ký Event Scanner nếu Enemy này có mang công cụ quét RayCastHelper (như ShieldBot)
            var rch = GetComponent<RayCastHelper>();
            if (rch != null)
            {
                rch.OnDetectedObjectsChanged += HandleDetectedObjectsChanged;
            }
        }

        private void OnDestroy()
        {
            var rch = GetComponent<RayCastHelper>();
            if (rch != null)
            {
                rch.OnDetectedObjectsChanged -= HandleDetectedObjectsChanged;
            }
            
            if (_ctx != null && _ctx.Status != null)
            {
                _ctx.Status.OnDieEvent -= OnDeath;
            }
        }

        // Event Receiver: Được gọi tự động mỗi khi máy dò RayCast quét được đồ
        protected virtual void HandleDetectedObjectsChanged(System.Collections.Generic.List<GameObject> detectedObjects)
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

        protected virtual void Update()
        {
            // Flag bảo vệ đơn giản: Không chạy AI khi đã chết
            if (!_ctx.IsAlive) return;

            // Cập nhật giá trị hiển thị Debug
            if (_currentState != null)
                currentStateName = _currentState.GetType().Name;
            currentTarget = _ctx.CurrentTarget;
            currentPortal = _ctx.HomePortal;

            _currentState?.Update(_ctx);
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
            ChangeState(new DeadState());
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

            // Xem trong EnemyStatus component, currentHp sẽ được gán lại thông qua Setup

            ChangeState(new IdleState());
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
            if (_ctx.Pool != null)
            {
                _ctx.Pool.ReturnEnemy(_ctx.Self);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}

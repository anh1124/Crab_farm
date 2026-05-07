using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;
using Script.Manager.WorldManager;
using Script.Game.Mobs.MobsManager;
using Script.Interfaces;
using Script.Data_Model.Interfaces;
using Script.Manager.GamePlayManager;
using Script.Data_Model;
using Script.Game.Mobs.Subjects;
using Script.Game.Item.Coin;
using Script.Game.Other;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
   
    public class ArcherJob : DenizenJob, INPCBehavior
    {
        // States
        public ArcherIdleState         IdleState         { get; private set; }
        public ArcherHuntingState      HuntingState      { get; private set; }
        public ArcherAttackState       AttackState       { get; private set; }
        public ArcherHoldWallState     HoldWallState     { get; private set; }
        public ArcherRetreatState      RetreatState      { get; private set; }
        public ArcherReturnToBaseState ReturnToBaseState { get; private set; }
        public ArcherDropCoinState     DropCoinState     { get; private set; }
        private ArcherItemHandler _itemHandler;
        private DenizenSchedule _schedule;
        private ArcherSensorHandler _sensor;
        private ArcherTacticalHandler _tacticalHandler;
        private ArcherCombatValidator _combatValidator;
        private ArcherTargetSelector _targetSelector;
        private ArcherDebugger _debugger;

        [Header("Tactical Settings")]
        [SerializeField] private bool ignoreGuardManagerCommand = false;
        public bool IgnoreGuardManagerCommand => ignoreGuardManagerCommand;
        
        private bool _isInitialized;
        private bool _handlersInitialized;

        protected void Awake()
        {
            InitializeBrain();
        }

        private void InitializeBrain()
        {
            if (_isInitialized) return;

            if (Context == null) Context = GetComponentInParent<DenizenContext>();
            IdleState         = new ArcherIdleState();
            HuntingState      = new ArcherHuntingState();
            AttackState       = new ArcherAttackState();
            HoldWallState     = new ArcherHoldWallState();
            RetreatState      = new ArcherRetreatState();
            ReturnToBaseState = new ArcherReturnToBaseState();
            DropCoinState     = new ArcherDropCoinState();

            _isInitialized = true;

            if (Context != null) InitializeHandlers();
        }

        private void InitializeHandlers()
        {
            if (_handlersInitialized) return;

            _targetSelector  = new ArcherTargetSelector(Context);
            _itemHandler     = new ArcherItemHandler(Context);
            _schedule        = new DenizenSchedule(Context);
            _sensor          = new ArcherSensorHandler(Context, this, _targetSelector);
            _tacticalHandler = new ArcherTacticalHandler(Context, this);
            _combatValidator = new ArcherCombatValidator(Context);
            _debugger        = new ArcherDebugger(Context, this, _combatValidator);

            _handlersInitialized = true;
        }

        protected override void OnContextInjected()
        {
            InitializeBrain();
            _handlersInitialized = false; // Force re-init with fresh Context
            InitializeHandlers();

            // PHẢI dùng Context.GameObject (root Denizen) vì ArcherJob là child component.
            if (Context.GuardManager != null)
                Context.GuardManager.RegisterArcher(Context.GameObject);
        }

        #region Brain & Transitions
        public override void UpdateBehavior()
        {
            InitializeBrain();
            if (Context == null) return;

            // [WATCHDOG + SCAN] — Khôi phục detector nếu tắt bất thường, sau đó quét thủ công
            _sensor.CheckAndRecover();

            if (Context.IsSkillCasting) return;

            // [NIGHT / DEFENSE GATE] — nếu đang ở ngoài vùng an toàn, về gốc ngay
            if (Context.TimeManager != null && !Context.TimeManager.IsDaytime)
            {
                bool isSafe = WorldUtility.IsInKingdomSafeZone(transform.position.x) 
                           || WorldUtility.IsInPlazaZone(transform.position.x);
                
                if (!isSafe)
                {
                    // Nếu đang ở ReturnToBase hoặc Retreat thì cứ để nó tiếp tục chạy về
                    if (CurrentState != ReturnToBaseState && CurrentState != RetreatState)
                    {
                        _tacticalHandler.ExecuteDefensiveOrder();
                    }
                    
                    // Cho phép state hiện tại cập nhật tiếp (để check reached)
                    bool allowDefenseStateUpdate = CurrentState == ReturnToBaseState || CurrentState == RetreatState;
                    if (!allowDefenseStateUpdate)
                        return;
                }
            }

            // [IDLE GATE] — tái đánh giá khi rảnh
            if (CurrentState == IdleState)
            {
                DecideFromIdle();
                return;
            }

            CurrentState?.UpdateBehavior(Context);
        }

        public void ChangeState(DenizenState newState) => DoChangeState(newState);

        private void Update() => CurrentState?.Update(Context);

        public void SetMoveTarget(Vector3 pos, float speed = -1f) => Context.Movement?.SetTarget(pos, speed);
        public void StopMovement()            => Context.Movement?.Stop();

        public bool HasReachedPosition(Vector3 pos, float threshold = 0.2f) =>
            Mathf.Abs(transform.position.x - pos.x) < threshold;

        /// <summary>
        /// Kiểm tra vị trí hiện tại có đủ an toàn để nhắm bắn không.
        /// Mọi State muốn chuyển sang AttackState PHẢI gọi hàm này trước.
        /// </summary>
        public void SetIgnoreGuard(bool val) => ignoreGuardManagerCommand = val;

        public bool CanAttackFromHere(float squadPadding = -1f)
        {
            return _combatValidator.CanAttackFromHere(this, squadPadding);
        }

        public bool IsInsideWall(bool logDebug) => _combatValidator.IsInsideWall(logDebug);

        /// <summary>Quyết định state ban đầu theo ưu tiên: Alarm > Night > Day.</summary>
        private void DecideInitialState()
        {
            bool hasAlarm = Context.GuardManager != null && Context.GuardManager.isEnemyAttack;
            bool isDay    = Context.TimeManager == null || Context.TimeManager.IsDaytime;

            if (hasAlarm || !isDay)
                _tacticalHandler.ExecuteDefensiveOrder();
            else
                ChangeState(HuntingState);
        }

        private void DecideFromIdle() => DecideInitialState();
        #endregion

        #region Lifecycle
        private void OnEnable()
        {
            InitializeBrain();
            if (Context != null)
            {
                // Đảm bảo detector references tồn tại trước khi sử dụng
                if (Context.CircleDetect == null)
                    Context.CircleDetect = GetComponentInChildren<CircleDetectHelper>(true);
                
                Context.ResetDetectors(); // Swab previous job's detectors
                _sensor.Enable();
                
                // Force scan ngay lập tức để detect enemy/coin ngay
                if (Context.CircleDetect != null && Context.CircleDetect.IsActive)
                    Context.CircleDetect.ScanCircle();
                
                if (Context.Status != null)
                {
                    Context.Status.OnCoinChanged  += OnCoinChangedHandler;
                    Context.Status.CanMove = true;
                    Context.Status.isMoving = false;
                }

                // [REGISTRATION] Job tự đăng ký với Manager chuyên trách.
                // PHẢI dùng Context.GameObject (root Denizen) vì ArcherJob là child component.
                if (Context.GuardManager != null)
                {
                    Context.GuardManager.RegisterArcher(Context.GameObject);
                }
            }

            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Register(this);

            // Sử dụng cached TimeManager từ Context thay vì FindObjectOfType
            var gtm = Context.TimeManager;
            if (gtm != null)
            {
                _schedule.AddSchedule(7, () => {
                    if (Context.GuardManager != null && !Context.GuardManager.isEnemyAttack)
                        ChangeState(HuntingState);
                });
                _schedule.AddSchedule(19, () => _tacticalHandler.ExecuteDefensiveOrder());
                _schedule.Enable();
            }

            // Không hard-code HuntingState — gọi DecideInitialState để kiểm tra đúng thứ tự
            ChangeState(IdleState);      // Vào Idle trước, UpdateBehavior sẽ gọi DecideFromIdle
        }

        private void OnDisable()
        {
            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Unregister(this);
            
            // Job tự gỡ đăng ký khi bị vô hiệu hóa
            if (Context != null && Context.GuardManager != null)
            {
                Context.GuardManager.UnregisterArcher(gameObject);
            }

            if (Context != null)
            {
                _sensor.Disable();
                
                if (Context.Status != null)
                {
                    Context.Status.OnCoinChanged -= OnCoinChangedHandler;
                    // Clear targets để tránh stale data khi chuyển job
                    Context.Status.currentTarget = null;
                    Context.Status.coinTarget = null;
                }
            }

            // Sử dụng cached TimeManager từ Context thay vì FindObjectOfType
            var gtm = Context.TimeManager;
            if (gtm != null)
            {
                _schedule.Disable();
            }

            CurrentState?.Exit(Context);
            CurrentState = null;

            if (Context?.Status != null)
                Context.Status.isMoving = false;
        }
        #endregion

        #region Save/Load State Recovery
        /// <summary>
        /// Được gọi sau khi load từ save để khôi phục state machine.
        /// OnEnable không chạy lại khi load nên cần hàm này để tạo lại state.
        /// </summary>
        public void ResetStateAfterLoad()
        {
            InitializeBrain();
            if (Context?.Status == null) return;
            
            // Đảm bảo archer được đăng ký vào squad sau khi load.
            // PHẢI dùng Context.GameObject (root Denizen) vì ArcherJob là child component.
            if (Context.GuardManager != null)
            {
                Context.GuardManager.RegisterArcher(Context.GameObject);
            }
            
            // Đảm bảo detector references tồn tại
            if (Context.CircleDetect == null)
                Context.CircleDetect = GetComponentInChildren<CircleDetectHelper>(true);
            
            // Bật lại detectors
            if (Context.CircleDetect != null)
            {
                Context.CircleDetect.IsActive = true;
                Context.CircleDetect.ScanCircle();
            }
            
            // Khôi phục state từ enum đã lưu
            var savedState = Context.Status.archerState;
            DenizenState newState = savedState switch
            {
                Script.Data_Model.Status.DenizenStatus.ArcherState.Hunting => HuntingState,
                Script.Data_Model.Status.DenizenStatus.ArcherState.Attack => AttackState,
                Script.Data_Model.Status.DenizenStatus.ArcherState.HoldWall => HoldWallState,
                Script.Data_Model.Status.DenizenStatus.ArcherState.Retreat => RetreatState,
                Script.Data_Model.Status.DenizenStatus.ArcherState.ReturningToBase => ReturnToBaseState,
                Script.Data_Model.Status.DenizenStatus.ArcherState.DropCoin => DropCoinState,
                _ => IdleState
            };
            
            // Chỉ đổi state nếu hiện tại khác hoặc null
            if (CurrentState == null || CurrentState != newState)
            {
                // Nếu là Retreat, khôi phục target từ assignedWall đã lưu
                if (newState == RetreatState && Context.Status.assignedWall != null)
                {
                    RetreatState.SetRetreatTarget(Context.Status.assignedWall);
                }

                ChangeState(newState);
            }
            
            if (Context.Status.debugDenizen)
                Debug.Log($"[ArcherJob] ResetStateAfterLoad: {newState.GetType().Name} (saved enum: {savedState})");
        }
        #endregion

        #region Tactical & Core Handlers
        public override void ReceiveDamage(float damage, GameObject instigator)
        {
            _itemHandler.ProcessDamage(damage, instigator);
        }

        private void OnCoinChangedHandler(int newCoins)
        {
            _itemHandler.OnCoinChanged(newCoins);
        }

        public void OnObjectsDetected(List<GameObject> objects)
        {
            _sensor.OnObjectsDetected(objects);
        }

        public override void HandleAnimationEvent(string eventName)
        {
            if (Context.Status.debugDenizen) Debug.Log($"<color=cyan>[ArcherJob]</color> {gameObject.name} nhận được Event: {eventName}");
            
            if (CurrentState is IArcherAttackState att)
            {
                if (eventName == "DrawComplete") att.OnDrawComplete(Context);
                else if (eventName == "ArrowRelease") att.OnArrowRelease(Context);
            }
        }

        public override bool HandleItem(GameObject item)
        {
            return _itemHandler.HandleItem(item);
        }

        public override void OnGuardManagerStatusChanged(bool isAttack)
        {
            _tacticalHandler.OnGuardManagerStatusChanged(isAttack);
        }

        public override void ForceRetreat(GameObject safeWall)
        {
            _tacticalHandler.ForceRetreat(safeWall);
        }

        #endregion

        #region Defense Logic
        public void ExecuteDefensiveOrder()
        {
            _tacticalHandler.ExecuteDefensiveOrder();
        }

        public void HandleDayBreaks()
        {
            if (Context.GuardManager != null && !Context.GuardManager.isEnemyAttack)
                ChangeState(HuntingState);
        }
        #endregion
        #region Tactical Debugger
        [ContextMenu("DebugArcherCombat")]
        public void DebugArcherCombat() => _debugger?.RunCombatDebug();
        #endregion
    }
}

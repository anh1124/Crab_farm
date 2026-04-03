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

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherJob : DenizenJob, INPCBehavior, IDamageable
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

        [Header("Tactical Settings")]
        [SerializeField] private bool ignoreGuardManagerCommand = false;
        public bool IgnoreGuardManagerCommand => ignoreGuardManagerCommand;

        protected void Awake()
        {
            if (Context == null) Context = GetComponentInParent<DenizenContext>();
            IdleState         = new ArcherIdleState();
            HuntingState      = new ArcherHuntingState();
            AttackState       = new ArcherAttackState();
            HoldWallState     = new ArcherHoldWallState();
            RetreatState      = new ArcherRetreatState();
            ReturnToBaseState = new ArcherReturnToBaseState();
            DropCoinState     = new ArcherDropCoinState();

            _itemHandler      = new ArcherItemHandler(Context);
            _schedule         = new DenizenSchedule(Context);
            _targetSelector   = new ArcherTargetSelector(Context);
            _sensor           = new ArcherSensorHandler(Context, this, _targetSelector);
            _tacticalHandler  = new ArcherTacticalHandler(Context, this);
            _combatValidator  = new ArcherCombatValidator(Context);
        }

        #region Brain & Transitions
        public override void UpdateBehavior()
        {
            if (Context == null) return;

            // [DETECTOR] — Gọi scanner thủ công, giống Enemy
            if (Context.CircleDetect != null && Context.CircleDetect.IsActive)
                Context.CircleDetect.ScanCircle();

            if (Context.IsSkillCasting) return;

            // [NIGHT / DEFENSE GATE] — nếu đang ở ngoài vùng an toàn, về gốc ngay
            if (Context.TimeManager != null && !Context.TimeManager.IsDaytime)
            {
                bool isSafe = WorldUtility.IsInKingdomSafeZone(transform.position.x) 
                           || WorldUtility.IsInPlazaZone(transform.position.x);
                if (!isSafe)
                {
                    _tacticalHandler.ExecuteDefensiveOrder();
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

        /// <summary>Quyết định state ban đầu theo ưu tiên: Alarm > Night > Day.</summary>
        private void DecideInitialState()
        {
            bool hasAlarm = Context.GuardManager != null && Context.GuardManager.Alarm.isEnemyAttack;
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
            if (Context != null)
            {
                Context.ResetDetectors(); // Swab previous job's detectors
                _sensor.Enable();
                
                if (Context.Status != null)
                {
                    Context.Status.OnCoinChanged  += OnCoinChangedHandler;
                    Context.Status.isMovementAllowed = true;
                    Context.Status.isMoving = false;
                }
            }

            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Register(this);
            if (DenizenManager.Instance  != null) DenizenManager.Instance.RegisterDenizen(gameObject);

            if (GameTimeManager.Instance != null)
            {
                _schedule.AddSchedule(7, () => {
                    if (Context.GuardManager != null && !Context.GuardManager.Alarm.isEnemyAttack)
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
            if (DenizenManager.Instance  != null) DenizenManager.Instance.UnregisterDenizen(gameObject);

            if (Context != null)
            {
                _sensor.Disable();
                
                if (Context.Status != null)
                    Context.Status.OnCoinChanged -= OnCoinChangedHandler;
            }

            if (GameTimeManager.Instance != null)
            {
                _schedule.Disable();
            }

            CurrentState?.Exit(Context);
            CurrentState = null;

            if (Context?.Status != null)
                Context.Status.isMoving = false;
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
            if (Context.GuardManager != null && !Context.GuardManager.Alarm.isEnemyAttack)
                ChangeState(HuntingState);
        }
        #endregion
        #region Tactical Debugger
        [ContextMenu("DebugArcherCombat")]
        public void DebugArcherCombat()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"===== ARCHER COMBAT DEBUG: {gameObject.name} =====");
            sb.AppendLine($"- Trạng thái hiện tại: {CurrentState?.GetType().Name}");
            sb.AppendLine($"- Tọa độ X: {transform.position.x:F2}");
            sb.AppendLine($"- Mục tiêu đang nhắm: {(Context.Status.currentTarget != null ? Context.Status.currentTarget.name : "NULL")}");

            // 1. Phân tích danh sách Detector
            if (Context.CircleDetect != null)
            {
                var detected = Context.CircleDetect.GetDetectedObjects();
                sb.AppendLine($"\n[Danh sách vật thể quét được] ({detected.Count} đối tượng):");
                for (int i = 0; i < detected.Count; i++)
                {
                    if (detected[i] == null) continue;
                    float d = Vector2.Distance(transform.position, detected[i].transform.position);
                    sb.AppendLine($" [{i}] {detected[i].name} (Tag: {detected[i].tag}) - Cách: {d:F2}m");
                }
            }

            // 2. Giải mã logic CanAttackFromHere
            sb.AppendLine("\n[Kiểm tra điều kiện tấn công (CanAttackFromHere)]:");
            bool inPlaza = WorldUtility.IsInPlazaZone(transform.position.x);
            sb.AppendLine($"- Đang trong Plaza: {inPlaza}");

            int defendableCount = 0;
            var wallManager = Script.Game.Structures.Wall_And_Stump.WallAndStumpManager.Instance;
            if (wallManager != null)
            {
                var flankWalls = Context.Status.isLeftFlank ? wallManager.leftWalls : wallManager.rightWalls;
                foreach (var wall in flankWalls)
                    if (wallManager.IsWallDefendable(wall)) defendableCount++;
            }
            sb.AppendLine($"- Số tường phòng thủ được trên cánh này: {defendableCount}");

            float padding = 7.0f;
            if (Context.GuardManager != null && Context.GuardManager.Squad != null)
                padding = Context.GuardManager.Squad.defaultSlotOffset;
            sb.AppendLine($"- Khoảng cách an toàn (Padding) áp dụng: {padding}");

            bool isSafeZone = WorldUtility.IsInKingdomSafeZone(transform.position.x, padding);
            sb.AppendLine($"- Đứng sau vạch an toàn (SafeZone + Padding): {isSafeZone}");

            bool canAttack = CanAttackFromHere();
            sb.AppendLine($"\n=> KẾT QUẢ: {(canAttack ? "<color=green>ĐỦ ĐIỀU KIỆN BẮN</color>" : "<color=red>KHÔNG ĐƯỢC PHÉP BẮN</color>")}");

            if (!canAttack)
            {
                sb.AppendLine("LÝ DO:");
                if (!inPlaza && !isSafeZone) sb.AppendLine("+ Archer đang đứng QUÁ GẦN nguy hiểm (vượt quá vạch Padding của tường xa nhất).");
                else if (inPlaza && defendableCount == 1 && !IgnoreGuardManagerCommand && !isSafeZone)
                     sb.AppendLine("+ Đang ở Plaza nhưng chỉ còn 1 tường cuối, yêu cầu đứng lùi sâu hơn nữa.");
            }

            Debug.Log(sb.ToString());
        }
        #endregion
    }
}

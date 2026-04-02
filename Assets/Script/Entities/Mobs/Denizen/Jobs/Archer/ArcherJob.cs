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
                    ExecuteDefensiveOrder();
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

        /// <summary>
        /// 4-zone model:
        /// [Bãi săn]     - ngoài wall xa nhất: không bắn
        /// [Chiến tuyến] - từ wall xa nhất vào đến wall-offset: kỷ luật (HoldWallState gate)
        /// [Nội thành]   - giữa 2 vùng wall-offset: bắn được, ignoreGuard tạm thời
        /// [Plaza]       - tâm thành: fallback cá nhân nếu cánh mình hết wall
        /// </summary>
        public bool CanAttackFromHere(float squadPadding = -1f)
        {
            float posX = transform.position.x;

            // --- 1. Xác định số wall còn lại ---
            int defendableCount = 0;
            var wallManager = Script.Game.Structures.Wall_And_Stump.WallAndStumpManager.Instance;
            if (wallManager != null)
            {
                var flankWalls = Context.Status.isLeftFlank ? wallManager.leftWalls : wallManager.rightWalls;
                foreach (var w in flankWalls)
                    if (wallManager.IsWallDefendable(w)) defendableCount++;
            }

            // --- 2. Xác định các vạch ranh giới ---
            bool inPlaza = WorldUtility.IsInPlazaZone(posX);
            
            // [PADDING LOGIC]
            // Nếu không truyền vào, dùng khoảng cách SafeZone tối thiểu (bằng đúng offset của slot)
            if (squadPadding < 0) squadPadding = Context.GuardManager != null ? Context.GuardManager.defaultSlotOffset : 3.0f;
            
            // inKingdom: Đã bước qua tường (padding 0)
            bool inKingdom = WorldUtility.IsInKingdomSafeZone(posX, 0f);
            
            // inSafePosition: Đã đứng sau vạch Offset kỷ luật (padding 6-7m)
            bool inSafePosition = WorldUtility.IsInKingdomSafeZone(posX, squadPadding);

            // =========================================================
            // ZONE 1: LOẠI BỎ CÁC VÙNG NGUY HIỂM (NGOÀI TƯỜNG / CHƯA LÙI ĐỦ SÂU)
            // =========================================================
            // Nếu ở ngoài tường và không phải ở Plaza (fallback cuối) -> KHÔNG BẮN
            if (!inKingdom && !inPlaza) return false;

            // Nếu đã vào trong tường nhưng chưa lùi đủ sâu (vượt quá vạch Padding)
            // mà cũng chưa chạm tới Plaza -> KHÔNG BẮN (Phải chạy tiếp cho đủ an toàn)
            if (inKingdom && !inSafePosition && !inPlaza) return false;

            // =========================================================
            // ZONE 2: VÙNG AN TOÀN KỶ LUẬT (Đã đứng sau vạch Offset)
            // =========================================================
            if (inSafePosition)
            {
                // [NỘI THÀNH] Nếu đứng sâu trong thành cách Plaza < 13m -> Set ignore để combat liên tục
                if (Mathf.Abs(posX) < 13f && Context.Status.currentTarget != null) 
                    SetIgnoreGuard(true);
                
                return true; // Đứng đúng slot/vị trí an toàn -> ĐƯỢC PHÉP BẮN
            }

            // =========================================================
            // ZONE 3: FALLBACK PLAZA (Chưa tới slot nhưng đã chạm Plaza)
            // =========================================================
            if (inPlaza)
            {
                // Chỉ cho phép bắn ở Plaza nếu wall đã mất sạch (tử thủ)
                // hoặc đang giữ cự ly bắn dở từ trước (ignoreGuard)
                if (defendableCount == 0 || ignoreGuardManagerCommand) return true;
                
                return false; // Còn tường mà chưa đứng đúng slot -> Phải chạy tới slot, không được dừng bắn bừa bãi
            }

            // =========================================================
            // ZONE 4: CHIẾN TUYẾN & NỘI THÀNH (Đã đứng sau vạch Offset)
            // =========================================================
            if (inSafePosition)
            {
                // Kiểm tra xem có phải "Nội thành" (sâu hơn vạch offset một đoạn) không
                // Nếu cực kỳ an toàn (X tiến gần về 0/Plaza) -> Nội thành
                float distToPlaza = Mathf.Abs(posX);
                float plazaRadius = 13f; // Ước lượng bán kính an toàn nội thành quanh Plaza
                
                if (distToPlaza < plazaRadius)
                {
                    // Vùng Nội Thành: Cho phép bắn địch lọt tâm, set ignore tạm thời
                    if (Context.Status.currentTarget != null) SetIgnoreGuard(true);
                }

                return true; 
            }

            return false;
        }

        /// <summary>Quyết định state ban đầu theo ưu tiên: Alarm > Night > Day.</summary>
        private void DecideInitialState()
        {
            bool hasAlarm = Context.GuardManager != null && Context.GuardManager.Alarm.isEnemyAttack;
            bool isDay    = Context.TimeManager == null || Context.TimeManager.IsDaytime;

            if (hasAlarm || !isDay)
                ExecuteDefensiveOrder();
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
                if (Context.CircleDetect != null)
                {
                    Context.CircleDetect.OnDetectedObjectsChanged -= OnObjectsDetected;
                    Context.CircleDetect.OnDetectedObjectsChanged += OnObjectsDetected;
                    Context.CircleDetect.tagsToDetect = new List<string>(TargetLists.ArcherTargets) { GameTags.Player };
                    Context.CircleDetect.detectRadius = Context.Status.archerDetectRange;
                    Context.CircleDetect.IsActive = true;
                }
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
                GameTimeManager.Instance.SubscribeToHourlyEvent(7,  HandleDayBreaks);
                GameTimeManager.Instance.SubscribeToHourlyEvent(19, HandleNightFalls);
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
                if (Context.CircleDetect != null)
                    Context.CircleDetect.OnDetectedObjectsChanged -= OnObjectsDetected;
                if (Context.Status != null)
                    Context.Status.OnCoinChanged -= OnCoinChangedHandler;
            }

            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.UnsubscribeFromHourlyEvent(7,  HandleDayBreaks);
                GameTimeManager.Instance.UnsubscribeFromHourlyEvent(19, HandleNightFalls);
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
            if (damage <= 0) return;
            if (Context.Status.CurrentCoins > 1)
            {
                Context.Status.SubtractCoin(1);
                global::Spawner.Instance?.DropByDenizen(transform.position, 1, Vector2.up);
            }
            else
            {
                DropBowOnHurt();
                Context.Status.isFearPending = true;
                Context.RoleController.ChangeRole(NPCRole.Villager);
            }
        }

        private void OnCoinChangedHandler(int newCoins)
        {
            if (newCoins <= 0)
            {
                DropBowOnHurt();
                Context.RoleController.ChangeRole(NPCRole.Vagrant);
            }
        }

        private void DropBowOnHurt()
        {
            if (PrefabData.Instance?.bow != null)
                Instantiate(PrefabData.Instance.bow, transform.position + Vector3.up, Quaternion.identity);
        }

        public void OnObjectsDetected(List<GameObject> objects)
        {
            if (!enabled || CurrentState == null || CurrentState == DropCoinState) return;
            GameObject target = SelectCombatTarget(objects);
            if (target != null)
            {
                Context.Status.currentTarget = target;
                if (Context.GuardManager != null)
                    Context.GuardManager.RegisterEnemy(target);
                return;
            }
            Context.Status.currentTarget = null;
            if (IsPlayerInList(objects) && Context.CoinSharing != null && Context.CoinSharing.CanDropCoin())
                ChangeState(DropCoinState);
        }

        private GameObject SelectCombatTarget(List<GameObject> objects)
        {
            GameObject bestTarget = null;
            float minDistance = float.MaxValue;

            foreach (var tag in TargetLists.ArcherTargets)
            {
                foreach (var go in objects)
                {
                    if (go != null && go.CompareTag(tag))
                    {
                        float dist = Vector2.Distance(transform.position, go.transform.position);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestTarget = go;
                        }
                    }
                }
                // Nếu đã tìm thấy target ở độ ưu tiên này, trả về luôn (ưu tiên Enemy > Rabbit/Deer)
                if (bestTarget != null) return bestTarget;
            }
            return null;
        }

        private bool IsPlayerInList(List<GameObject> objects)
        {
            foreach (var go in objects)
                if (go.CompareTag(GameTags.Player)) return true;
            return false;
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
            var pItem = item.GetComponent<PhysicalItem>();
            if (pItem == null || !pItem.IsAvailable()) return false;

            // Archer nhặt xu
            if (pItem.itemType == ItemType.Coin && !Context.Status.IsFull)
            {
                Context.Status.AddCoin(1);
                return true;
            }

            return false;
        }

        public override void OnGuardManagerStatusChanged(bool isAttack)
        {
            if (!isAttack)
            {
                // Báo động kết thúc → reset ignoreGuard, về chế độ ban ngày
                SetIgnoreGuard(false);
                HandleDayBreaks();
                return;
            }

            // Báo động mới (wall thay đổi) → kiểm tra lại vị trí theo tinh thần "ignoreGuard tạm thời"
            // Nếu đang bắn enemy trong nội thành/plaza → tự kiểm tra xem còn hợp lệ không
            if (ignoreGuardManagerCommand && (CurrentState == AttackState || CurrentState == HoldWallState))
            {
                // Re-check vị trí dựa trên trạng thái tường MỚI
                bool stillValid = CanAttackFromHere();
                if (stillValid)
                {
                    if (Context.Status.debugDenizen) Debug.Log($"<color=cyan>[ArcherJob]</color> {gameObject.name} IgnoreGuard: Vị trí vẫn hợp lệ sau khi tường thay đổi. Tiếp tục chiến đấu.");
                    return; // Tiếp tục, không rút lui
                }
                else
                {
                    // Vị trí không còn hợp lệ → reset ignore và rút lui
                    if (Context.Status.debugDenizen) Debug.Log($"<color=orange>[ArcherJob]</color> {gameObject.name} IgnoreGuard: Vị trí không còn hợp lệ. Rút lui!");
                    SetIgnoreGuard(false);
                }
            }

            ExecuteDefensiveOrder();
        }

        public override void ForceRetreat(GameObject safeWall)
        {
            // [NỘI THÀNH/PLAZA] — Nếu đang dùng ignoreGuard tạm thời, 
            // kiểm tra xem vị trí hiện tại có còn hợp lệ không
            if (ignoreGuardManagerCommand && Context.Status.currentTarget != null)
            {
                bool stillValid = CanAttackFromHere();
                if (stillValid)
                {
                    if (Context.Status.debugDenizen) Debug.Log($"<color=cyan>[ArcherJob]</color> {gameObject.name} ForceRetreat bị từ chối: Đang giao chiến tại vị trí hợp lệ.");
                    return; // Vẫn an toàn → tiếp tục chiến đấu, bỏ qua lệnh rút
                }
                // Vị trí không còn hợp lệ → reset flag, tiến hành rút lui
                SetIgnoreGuard(false);
            }

            // [LAST STAND] — Không còn wall nào → tử thủ Plaza
            if (safeWall == null)
            {
                if (Context.Status.debugDenizen) Debug.Log($"<color=red>[ArcherJob]</color> {gameObject.name} KHÔNG CÒN TƯỜNG. Tử thủ tại Plaza!");
                // Kể cả đang AttackState → phải về HoldWall để reposition về Plaza
                ChangeState(ReturnToBaseState);
                return;
            }

            // [RETREAT] — Gán wall mới và rút lui
            if (Context.Status != null)
                Context.Status.assignedWall = safeWall;
            RetreatState.SetRetreatTarget(safeWall);
            ChangeState(RetreatState);
        }

        #endregion

        #region Defense Logic
        public void ExecuteDefensiveOrder()
        {
            bool isSafe = WorldUtility.IsInKingdomSafeZone(transform.position.x) || WorldUtility.IsInPlazaZone(transform.position.x);
            
            if (!isSafe)
            {
                if (CurrentState != ReturnToBaseState && CurrentState != RetreatState)
                    ChangeState(ReturnToBaseState);
                return;
            }

            // Nếu đã an toàn và đang tấn công, đừng ngắt quãng nó
            if (CurrentState == AttackState) return;

            // Cho phép chuyển từ ReturnToBase sang HoldWall ngay khi vừa chạm vùng an toàn
            if (CurrentState != RetreatState && CurrentState != HoldWallState)
                ChangeState(HoldWallState);
        }

        public void HandleDayBreaks()
        {
            if (Context.GuardManager != null && !Context.GuardManager.Alarm.isEnemyAttack)
                ChangeState(HuntingState);
        }

        public void HandleNightFalls() => ExecuteDefensiveOrder();
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

using System.Collections.Generic;
using UnityEngine;
using Script.Game.Structures.Wall_And_Stump;
using Script.Data_Model.Interfaces;
using Script.Data_Model;
using Script.Data_Model.Status;
using Script.Manager.WorldManager;
using Script.Manager.Building;

namespace Script.Game.Mobs.MobsManager
{
    /// <summary>
    /// GuardManager - Quản lý hệ thống phòng thủ
    /// Điều phối tất cả archer và đặt thiết quân luật
    /// điều phối tường phòng thủ
    /// </summary>
    public class GuardManager : MonoBehaviour, IDataListBootstrap
    {
        public static GuardManager Instance { get; private set; }

        [Header("Archer Management")]
        public List<GameObject> leftFlankArchers = new List<GameObject>();
        public List<GameObject> rightFlankArchers = new List<GameObject>();
        
        [Header("Defensive Structures")]
        public GameObject currentLeftWall;
        public GameObject currentRightWall;

        [Header("Defense Slot Settings")]
        public int archerSlotCount = 7;
        public float archerSlotSpacing = 1.2f;
        [Tooltip("Khoảng cách từ tường đến slot đầu tiên")]
        public float defaultSlotOffset = 3.0f;

        [Header("Runtime State")]
        public bool isEnemyAttack = false;
        
        [Header("Debug")]
        public bool isDebugHuntingRange = true;
        public float archerHuntingRange = 25f;
        public List<GameObject> detectedEnemies = new List<GameObject>();
        
        [Tooltip("Thời gian chờ trước khi tắt báo động (seconds)")]
        private const float ALARM_COOLDOWN_DURATION = 30f;
        
        [Header("Alarm Status")]
        [Tooltip("Thời gian còn lại trước khi tắt báo động (Chỉ đếm ngược khi KHÔNG còn kẻ thù)")]
        public float alarmTimer = 0f;
        
        [Header("Debug Settings")]
        public bool showDebugLogs = true;

        // Cached references
        private WallAndStumpManager wallManager;

        #region Initialization

        private void Awake()
        {
            InitializeSingleton();
        }

        private void Start()
        {
            if (wallManager == null && WallAndStumpManager.Instance != null)
            {
                InitializeReferences();
            }
        }

        private void InitializeSingleton()
        {
            if (Instance == null)
            {
                Instance = this;
                DebugLog("GuardManager initialized");
            }
            else if (Instance != this)
            {
                Debug.LogWarning("[GuardManager] Duplicate instance detected, destroying...");
                Destroy(gameObject);
            }
        }

        private void InitializeReferences()
        {
            wallManager = WallAndStumpManager.Instance;
            
            if (wallManager == null)
            {
                Debug.LogError("[GuardManager] WallAndStumpManager phải được setup trước GuardManager");
            }
            else
            {
                //lấy thông tin wall xa nhất mỗi bên
                UpdateDefensiveWalls();
            }
        }

        /// <summary>
        /// Cập nhật vị trí phòng thủ tối ưu từ WallManager
        /// QUAN TRỌNG: Hàm này được gọi từ:
        /// 1. InitializeReferences() lúc khởi tạo để lấy wall nếu có 
        /// 2. WallAndStumpManager khi có wall mới (nếu KHÔNG đang combat-lưu ý là hàm này sẽ không lấy wall xa nhất nếu đang trong comabt,chỉ lấy wall phía sau)
        /// 3. ClearAlarm() sau khi hết thiết quân luật
        /// </summary>
        public void UpdateDefensiveWalls()
        {
            if (wallManager == null) return;
            // Lấy wall mới từ WallManager
            GameObject newLeftWall = wallManager.GetFarthestDefendableWall(true);
            GameObject newRightWall = wallManager.GetFarthestDefendableWall(false);
            
            // === XỬ LÝ LEFT WALL ===
            UpdateSpecificWall(ref currentLeftWall, newLeftWall, true);
            
            // === XỬ LÝ RIGHT WALL ===
            UpdateSpecificWall(ref currentRightWall, newRightWall, false);
            
            // Thông báo cho tất cả Archer cập nhật vị trí phòng thủ
            NotifyArchersDefensivePositionChanged();
        }
        
        private void UpdateSpecificWall(ref GameObject currentWall, GameObject newWall, bool isLeft)
        {
            if (currentWall != newWall)
            {
                // Hủy đăng ký event của wall cũ
                if (currentWall != null)
                {
                    var oldBehavior = currentWall.GetComponent<Script.Game.Structures.Wall_And_Stump.WallBehavior>();
                    if (oldBehavior != null)
                    {
                        oldBehavior.OnCriticalHP -= HandleWallCritical;
                    }
                }

                // Gán wall mới
                currentWall = newWall;

                // Đăng ký event cho wall mới
                if (currentWall != null)
                {
                    var newBehavior = currentWall.GetComponent<Script.Game.Structures.Wall_And_Stump.WallBehavior>();
                    if (newBehavior != null)
                    {
                        newBehavior.OnCriticalHP += HandleWallCritical;
                    }
                }

                DebugLog($"[GuardManager] Updated {(isLeft ? "LEFT" : "RIGHT")} defensive wall to: {currentWall?.name ?? "NULL"}");
            }
        }
        
        /// <summary>
        /// Callback khi wall trong currentLeftWall/currentRightWall xuống critical HP = rút quân ra tường phía sau
        /// </summary>
        private void HandleWallCritical(GameObject wall)
        {
            if (wall == null) return;
            
            bool isLeftSide = (wall == currentLeftWall);
           // DebugLog($"⚠️ Wall Critical Event: {wall.name} ({(isLeftSide ? "LEFT" : "RIGHT")})");
            
            // Gọi logic rút lui hiện tại
            NotifyWallCritical(wall, isLeftSide);
        }
        
        /// <summary>
        /// Thông báo cho Archer cập nhật vị trí phòng thủ mới
        /// (Không cần Archer tự check mỗi giây)
        /// </summary>
        private void NotifyArchersDefensivePositionChanged()
        {
            NotifyFlankArchers(leftFlankArchers, currentLeftWall);
            NotifyFlankArchers(rightFlankArchers, currentRightWall);
        }

        private void NotifyFlankArchers(List<GameObject> archers, GameObject wall)
        {
            if (wall == null) return;

            foreach (var archer in archers)
            {
                if (archer == null) continue;
                var status = archer.GetComponent<DenizenStatus>();
                if (status != null)
                {
                    // Chỉ cập nhật nếu Archer đang ở trạng thái phòng thủ/canh gác
                    if (status.archerState == DenizenStatus.ArcherState.ReturningToBase ||
                        status.archerState == DenizenStatus.ArcherState.HoldingWall)
                    {
                        status.assignedWall = wall;
                        // Archer behavior sẽ tự xử lý di chuyển đến assignedWall trong Update
                    }
                }
            }
        }

        #region IDataListBootstrap Implementation
        // High priority (1) to ensure it's initialized early
        public int Order => 1; 

        public void InitializeData(PrefabData context)
        {
            InitializeSingleton();
            InitializeReferences();
            DebugLog("✅ GuardManager initialized via Bootstrap");
        }
        #endregion

        #endregion

        #region Archer Registration

        public void RegisterArcher(GameObject archer)
        {
            if (archer == null)
            {
                Debug.LogWarning("[GuardManager] Cannot register null archer");
                return;
            }

            if (leftFlankArchers.Contains(archer) || rightFlankArchers.Contains(archer))
            {
               // Debug.LogWarning($"[GuardManager] Archer {archer.name} already registered");
                return;
            }

            // ... (rest of logic)
            // ...
            
            var archerStatus = archer.GetComponent<DenizenStatus>(); // Simplified using
            if (archerStatus == null)
            {
                Debug.LogError($"[GuardManager] Archer {archer.name} missing DenizenStatus component!");
                return;
            }

            // Assign to flank based on balance
            if (leftFlankArchers.Count <= rightFlankArchers.Count)
            {
                leftFlankArchers.Add(archer);
                archerStatus.isLeftFlank = true;
                DebugLog($"Registered {archer.name} to LEFT flank");
            }
            else
            {
                rightFlankArchers.Add(archer);
                archerStatus.isLeftFlank = false;
                DebugLog($"Registered {archer.name} to RIGHT flank");
            }

            // Check if alarm active -> force ReturningToBase immediately
            if (isEnemyAttack)
            {
                 archerStatus.archerState = DenizenStatus.ArcherState.ReturningToBase;
            }
        }

        public void UnregisterArcher(GameObject archer)
        {
            if (archer == null)
            {
                Debug.LogWarning("[GuardManager] UnregisterArcher called with NULL archer");
                return;
            }

            //Debug.Log($"[GuardManager] UnregisterArcher called for {archer.name}, LEFT count={leftFlankArchers.Count}, RIGHT count={rightFlankArchers.Count}");
            
            bool wasRemoved = false;
            if (leftFlankArchers.Contains(archer))
            {
                leftFlankArchers.Remove(archer);
                wasRemoved = true;
               // Debug.Log($"[GuardManager] ✅ Unregistered {archer.name} from LEFT flank");
            }
            else if (rightFlankArchers.Contains(archer))
            {
                rightFlankArchers.Remove(archer);
                wasRemoved = true;
              //  Debug.Log($"[GuardManager] ✅ Unregistered {archer.name} from RIGHT flank");
            }
            else
            {
                Debug.LogWarning($"[GuardManager] ⚠️ Attempted to unregister {archer.name} but it was NOT in any flank list!");
            }

            if (wasRemoved)
            {
                RebalanceSquads();
            }
        }

        public void RebalanceSquads()
        {
            // Clean up lists first
            leftFlankArchers.RemoveAll(x => x == null);
            rightFlankArchers.RemoveAll(x => x == null);

            int diff = leftFlankArchers.Count - rightFlankArchers.Count;

            // Difference of 1 is acceptable (e.g. 5 vs 4)
            if (Mathf.Abs(diff) <= 1) return;

            DebugLog($"[GuardManager] Rebalancing squads... (Diff: {diff})");

            if (diff > 1) 
            {
                // Left has too many (e.g. 6 vs 2 -> diff 4 -> move 2), move to Right
                int countToMove = diff / 2;
                for (int i = 0; i < countToMove; i++)
                {
                    MoveArcher(leftFlankArchers, rightFlankArchers, false);
                }
            }
            else // diff < -1
            {
                // Right has too many, move to Left
                int countToMove = Mathf.Abs(diff) / 2;
                for (int i = 0; i < countToMove; i++)
                {
                     MoveArcher(rightFlankArchers, leftFlankArchers, true);
                }
            }
        }

        private void MoveArcher(List<GameObject> fromList, List<GameObject> toList, bool toLeft)
        {
            if (fromList.Count == 0) return;

            // Take from end to minimize shift cost
            GameObject archer = fromList[fromList.Count - 1];
            fromList.RemoveAt(fromList.Count - 1);
            toList.Add(archer);

            var status = archer.GetComponent<DenizenStatus>();
            if (status != null)
            {
                status.isLeftFlank = toLeft;
                // Update assigned wall immediately
                status.assignedWall = toLeft ? currentLeftWall : currentRightWall;
                DebugLog($"[GuardManager] Reassigned {archer.name} to {(toLeft ? "LEFT" : "RIGHT")} flank");
            }
        }

        #endregion

        #region Alarm System

        // Use coroutine to handle alert cooldown
        private Coroutine alertCooldownCoroutine;

        /// <summary>
        /// Gọi khi Archer phát hiện Enemy
        /// </summary>
        public void RegisterEnemy(GameObject enemy)
        {
            if (enemy == null) return;

            // Prevent duplicates
            if (detectedEnemies.Contains(enemy)) return;

            detectedEnemies.Add(enemy);
            //DebugLog($"[GuardManager] 🚨 Detected NEW Enemy: {enemy.name}");

            // Subscribe to Despawn event
            var enemyBehavior = enemy.GetComponent<Script.Game.Enemy.EnemyBehavior>();
            if (enemyBehavior != null)
            {
                enemyBehavior.OnDespawn += HandleEnemyDespawn;
            }

            // If we detect a new enemy, CANCEL any pending peacetime cooldown
            if (alertCooldownCoroutine != null)
            {
                StopCoroutine(alertCooldownCoroutine);
                alertCooldownCoroutine = null;
            }
            // Always reset timer to max when enemy is present (Combat Mode)
            alarmTimer = ALARM_COOLDOWN_DURATION;

            // ACTIVATE ALARM immediately
            if (!isEnemyAttack)
            {
                isEnemyAttack = true;
                DebugLog("[GuardManager] ⚔️ ALARM ACTIVATED - Mobilizing Archers!");
                NotifyAllArchersValues(true);
            }
        }
        private void HandleEnemyDespawn(Script.Game.Enemy.EnemyBehavior enemyBehavior)
        {
            // Unsubscribe to prevent memory leaks
            enemyBehavior.OnDespawn -= HandleEnemyDespawn;

            // Remove from list
            if (detectedEnemies.Contains(enemyBehavior.gameObject))
            {
                detectedEnemies.Remove(enemyBehavior.gameObject);
            }

            // Check if threat is gone
            if (detectedEnemies.Count == 0)
            {
                // Start cooldown to ensure it's safe
                if (alertCooldownCoroutine == null)
                {
                    alertCooldownCoroutine = StartCoroutine(AlertCooldownRoutine());
                }
            }
        }

        private System.Collections.IEnumerator AlertCooldownRoutine()
        {
            DebugLog($"[GuardManager] Enemy list cleared. Waiting {ALARM_COOLDOWN_DURATION}s...");
            
            // Set timer and count down visually
            alarmTimer = ALARM_COOLDOWN_DURATION;
            while (alarmTimer > 0)
            {
                alarmTimer -= Time.deltaTime;
                yield return null;
            }
            alarmTimer = 0f;

            // If we are here, no new enemies appeared for 30s
            // Check logic for Day/Night
            if (GameTimeManager.Instance != null && GameTimeManager.Instance.IsDaytime)
            {
                ClearAlarm();
            }
            else
            {
                // If Night, we do NOT clear alarm logic completely?
                // User requirement: "If night... continue holding wall..."
                // So we basically keep isEnemyAttack = true? 
                // OR we clear it but Archer logic handles Night autonomously?
                // Archer logic: On Update/Idle -> Check Time -> If Night -> ReturningToBase.
                // So it is SAFE to ClearAlarm here. Archer will just go back to wall because it's Night.
                // Clearing alarm lets them enter "Peaceful Night Guard" instead of "Active Combat Alert".
                ClearAlarm();
            }
            
            alertCooldownCoroutine = null;
        }

        private void Update()
        {
            // Draw debug visualization
            UpdateHuntingRangeVisualization();
        }

        public void ClearAlarm()
        {
            isEnemyAttack = false;
            detectedEnemies.Clear();
            alarmTimer = 0f;
            DebugLog("✅ Alarm cleared - Returning to peacetime duties");
            
            // CẬP NHẬT VỊ TRÍ PHÒNG THỦ TỐI ƯU SAU KHI HẾT CHIẾN ĐẤU
            UpdateDefensiveWalls();
            
            //xếp lại đội hình 
            RebalanceSquads();
            // Notify transition to Hunting if Day
            NotifyAllArchersValues(false);
        }

        private void NotifyAllArchersValues(bool isAttack)
        {
            // Hàm helper này chỉ dùng để đổi trạng thái của tất cả cung thủ thành rút về phòng  thủ 
            
            List<GameObject> allArchers = new List<GameObject>();
            allArchers.AddRange(leftFlankArchers);
            allArchers.AddRange(rightFlankArchers);
            
            foreach(var archer in allArchers)
            {
                if(archer == null) continue;
                var behavior = archer.GetComponent<Script.Game.Mobs.Subjects.ArcherBehavior>();
                
                 if(behavior != null)
                 {
                     behavior.OnGuardManagerStatusChanged(isAttack);
                 }
            }
        }

        #endregion

        #region Wall Management Integration

        /// <summary>
        /// Logic Retreat khi wall sắp vỡ
        /// </summary>
        public void NotifyWallCritical(GameObject wall, bool isLeftSide)
        {
            if (wall == null || wallManager == null) return;
            
            DebugLog($"[GuardManager] ⚠️ WALL CRITICAL: {wall.name} on {(isLeftSide ? "LEFT" : "RIGHT")}");

            // Ask WallManager for the wall behind
            GameObject wallBehind = wallManager.GetDefendableWallBehind(wall);
            
            // Update global wall reference for new incoming archers
            if (isLeftSide)
                currentLeftWall = wallBehind;
            else
                currentRightWall = wallBehind;

            // Notify archers on that flank to retreat
            List<GameObject> flank = isLeftSide ? leftFlankArchers : rightFlankArchers;
            foreach (var archer in flank)
            {
                if (archer == null) continue;
                var status = archer.GetComponent<DenizenStatus>();
                if (status != null && status.assignedWall == wall)
                {
                    // Force state change
                    var behavior = archer.GetComponent<Script.Game.Mobs.Subjects.ArcherBehavior>();
                    if (behavior != null && wallBehind != null)
                    {
                         behavior.ForceRetreat(wallBehind); // Helper method on behavior
                    }
                    else if (behavior != null && wallBehind == null)
                    {
                        // No wall -> HoldingWall (Stand Ground)
                         // User req: "nếu ... null tức là không còn wall để lui nữa thù thôi" -> Keep holding current spot/state
                    }
                }
            }
        }

        public GameObject GetCurrentDefensiveWall(bool isLeft)
        {
            // Ensure data is fresh if null
            if (isLeft && currentLeftWall == null) UpdateDefensiveWalls();
            if (!isLeft && currentRightWall == null) UpdateDefensiveWalls();
            
            return isLeft ? currentLeftWall : currentRightWall;
        }

        /// <summary>
        /// Tính toán vị trí phòng thủ cho archer dựa trên slot system
        /// Mỗi flank có archerSlotCount slots, cách nhau archerSlotSpacing
        /// </summary>
        public Vector3 GetDefensePositionForArcher(GameObject archer)
        {
            if (archer == null) return Vector3.zero;

            var status = archer.GetComponent<DenizenStatus>();
            if (status == null) return archer.transform.position;

            bool isLeft = status.isLeftFlank;
            List<GameObject> flankList = isLeft ? leftFlankArchers : rightFlankArchers;
            GameObject wall = isLeft ? currentLeftWall : currentRightWall;
            
            // Tìm index của archer trong danh sách flank
            int index = flankList.IndexOf(archer);
            // Tính slot index (tuần hoàn)
            int slotIndex = (index >= 0) ? index % archerSlotCount : 0;

            if (wall == null)
            {
                // Fallback: Use TownCenter position if available
                if (BuildingManager.instance != null && BuildingManager.instance.currentTownCenter != null)
                {
                    var tc = BuildingManager.instance.currentTownCenter;
                    Transform tcTransform = tc.transform;
                    Vector3 tcPos = tcTransform.position;
                    
                    // Logic: Xếp hàng xung quanh TownCenter (Plaza)
                    // Lấy bán kính Plaza thực tế
                    float startDist = 5f; // Default fallback
                    var tcBehavior = tc.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
                    if(tcBehavior != null)
                    {
                        startDist = tcBehavior.plazaDistance;
                    }

                    // Bên trái đứng bên trái TC, bên phải đứng bên phải TC
                    float dir = isLeft ? -1f : 1f;
                    
                    // Khoảng cách an toàn từ TC (plazaDistance)
                    // Archer sẽ xếp hàng từ rìa Plaza VỀ PHÍA TownCenter
                    // Slot 0: plazaDistance
                    // Slot 1: plazaDistance - spacing
                    // Slot 2: plazaDistance - 2*spacing
                    float totalOffset = startDist - (slotIndex * archerSlotSpacing);
                    
                    // Đảm bảo không đứng quá gần tâm (giữ khoảng cách tối thiểu 2f)
                    if (totalOffset < 2f) totalOffset = 2f;
                    
                    return new Vector3(tcPos.x + (dir * totalOffset), tcPos.y, archer.transform.position.z);
                }
                
                // No wall, no TC -> Stand still
                return archer.transform.position;
            }

            // Tính hướng (left flank = +, right flank = -)
            float direction = isLeft ? 1f : -1f; // Lưu ý: Wall logic direction có thể khác TC logic direction tùy vào pivot của Wall
            // Kiểm tra lại logic direction của Wall:
            // Wall bên trái (LeftFlank): Quái đến từ bên trái -> Archer đứng Hướng +1 (Bên phải của Wall - Phía trong) ??? 
            // KHÔNG, logic cũ là: isLeft ? 1f : -1f.
            // Nếu Wall trái, quái bên trái. Archer đứng sau wall (bên phải wall). -> +1. Đúng.
            // Nếu Wall phải, quái bên phải. Archer đứng sau wall (bên trái wall). -> -1. Đúng.

            // Tính offset: defaultSlotOffset + (slotIndex * spacing)
            float offset = defaultSlotOffset + (slotIndex * archerSlotSpacing);

            Vector3 basePos = wall.transform.position;

            return new Vector3(
                basePos.x + direction * offset,
                basePos.y,
                archer.transform.position.z
            );
        }

        #endregion

        #region Statistics & Debug

       

        [ContextMenu("Debug Archer Positions")]
        public void DebugArcherPositions()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== ARCHER POSITION DEBUG INFO ===");
            
            if (BuildingManager.instance != null && BuildingManager.instance.currentTownCenter != null)
            {
                var tc = BuildingManager.instance.currentTownCenter;
                sb.AppendLine($"TownCenter Pos: {tc.transform.position}");
                
                var tcBehavior = tc.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
                if (tcBehavior != null)
                {
                    sb.AppendLine($"Plaza Distance: {tcBehavior.plazaDistance}");
                }
                else
                {
                    sb.AppendLine("Plaza Distance: N/A (Behavior Missing)");
                }
            }
            else
            {
                sb.AppendLine("TownCenter: NULL");
            }
            
            sb.AppendLine($"--- LEFT FLANK ({leftFlankArchers.Count}) ---");
            for(int i=0; i<leftFlankArchers.Count; i++)
            {
                var archer = leftFlankArchers[i];
                if(archer != null)
                {
                    int slotIndex = i % archerSlotCount;
                    sb.AppendLine($"[{i}] Slot: {slotIndex} | Pos: {archer.transform.position} | Target: {GetDefensePositionForArcher(archer)}");
                }
            }
            
            sb.AppendLine($"--- RIGHT FLANK ({rightFlankArchers.Count}) ---");
            for(int i=0; i<rightFlankArchers.Count; i++)
            {
                var archer = rightFlankArchers[i];
                if(archer != null)
                {
                    int slotIndex = i % archerSlotCount;
                    sb.AppendLine($"[{i}] Slot: {slotIndex} | Pos: {archer.transform.position} | Target: {GetDefensePositionForArcher(archer)}");
                }
            }
            
            Debug.Log(sb.ToString());
        }

        [ContextMenu("Debug Clear Enemies")]
        public void DebugClearEnemies()
        {
            if (detectedEnemies.Count > 0)
            {
                DebugLog("[GuardManager] 🛠️ Debug Clear ALL Enemies");
                // Create copy to iterate safely
                var enemies = new List<GameObject>(detectedEnemies);
                foreach (var enemy in enemies)
                {
                    if (enemy != null)
                    {
                        var behavior = enemy.GetComponent<Script.Game.Enemy.EnemyBehavior>();
                        if (behavior != null)
                        {
                            // Trigger despawn logic manually or just simulate it
                            // Better to just clear list and trigger logic as if they all died
                             behavior.OnDespawn -= HandleEnemyDespawn; // Unsub manually
                        }
                    }
                }
                detectedEnemies.Clear();
                
                // Trigger Cooldown Routine
                if (alertCooldownCoroutine == null)
                {
                    alertCooldownCoroutine = StartCoroutine(AlertCooldownRoutine());
                }
            }
        }

        private void DebugLog(string message)
        {
            if (showDebugLogs) Debug.Log($"[GuardManager] {message}");
        }

        private void OnDestroy()
        {
            // Cleanup event subscriptions
            if (currentLeftWall != null)
            {
                var leftBehavior = currentLeftWall.GetComponent<Script.Game.Structures.Wall_And_Stump.WallBehavior>();
                if (leftBehavior != null)
                {
                    leftBehavior.OnCriticalHP -= HandleWallCritical;
                }
            }
            
            if (currentRightWall != null)
            {
                var rightBehavior = currentRightWall.GetComponent<Script.Game.Structures.Wall_And_Stump.WallBehavior>();
                if (rightBehavior != null)
                {
                    rightBehavior.OnCriticalHP -= HandleWallCritical;
                }
            }
            
            leftFlankArchers.Clear();
            rightFlankArchers.Clear();
            detectedEnemies.Clear();
        }
        
        /// <summary>
        /// Xác định vị trí bắt đầu cho vùng săn bắn
        /// Ưu tiên: Wall → TownCenter → null (không vẽ)
        /// </summary>
        public Vector3? GetHuntingRangeStartPosition(bool isLeft)
        {
            GameObject wall = isLeft ? currentLeftWall : currentRightWall;
            
            // Ưu tiên 1: Dùng tường nếu có
            if (wall != null)
                return wall.transform.position;
            
            // Ưu tiên 2: Fallback về TownCenter
            if (BuildingManager.instance != null && BuildingManager.instance.currentTownCenter != null)
                return BuildingManager.instance.currentTownCenter.transform.position;
            
            // Ưu tiên 3: Không có điểm tham chiếu - trả về null để không vẽ
            return null;
        }
        
        /// <summary>
        /// Update logic for visualization
        /// </summary>
        private void UpdateHuntingRangeVisualization()
        {
             Vector3? leftStart = GetHuntingRangeStartPosition(true);
             if (leftStart.HasValue)
                 DrawHuntingRange(leftStart.Value, true); // Left is Left
                 
             Vector3? rightStart = GetHuntingRangeStartPosition(false);
             if (rightStart.HasValue)
                 DrawHuntingRange(rightStart.Value, false); // Right is Right
        }
        
        /// <summary>
        /// Visualization of hunting range for blue prints
        /// </summary>
        private void DrawHuntingRange(Vector3 wallPos, bool isLeft)
        {
            if (!isDebugHuntingRange) return;
            
            float range = archerHuntingRange;
            Vector3 rayDir = isLeft ? Vector3.left : Vector3.right;
            
             // Draw Blue Ray
            Debug.DrawRay(wallPos, rayDir * range, Color.blue);
        }

    #endregion
    }
}

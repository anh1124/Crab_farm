using System.Collections.Generic;
using UnityEngine;
using Script.Game.Structures.Wall_And_Stump;
using Script.Data_Model.Interfaces;
using Script.Data_Model;
using Script.Data_Model.Status;

namespace Script.Game.Mobs.MobsManager
{
    /// <summary>
    /// GuardManager - Quản lý hệ thống phòng thủ của kingdom
    /// Điều phối tất cả archer và phản ứng với các mối đe dọa
    /// </summary>
    public class GuardManager : MonoBehaviour, IDataListBootstrap
    {
        public static GuardManager Instance { get; private set; }

        [Header("Archer Management")]
        [Tooltip("Tất cả archer được assign vào flank trái")]
        public List<GameObject> leftFlankArchers = new List<GameObject>();
        
        [Tooltip("Tất cả archer được assign vào flank phải")]
        public List<GameObject> rightFlankArchers = new List<GameObject>();

        [Header("Alarm System")]
        [Tooltip("Có đang trong tình trạng báo động không")]
        public bool isAlarmActive = false;
        
        [Tooltip("Enemy đã trigger alarm")]
        public GameObject alarmSource;
        
        [Tooltip("Thời gian alarm sẽ tự động tắt nếu không có enemy (seconds)")]
        public float alarmCooldownTime = 30f;
        
        private float alarmTimer = 0f;

        [Header("Defense Statistics")]
        [Tooltip("Tổng số archer hiện có")]
        public int totalArchers = 0;
        
        [Tooltip("Số archer đang active combat")]
        public int archersInCombat = 0;

        [Header("Debug Settings")]
        [Tooltip("Show debug logs")]
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
            // InitializeReferences() is now called via Bootstrap if this component exists in scene
            // But for safety, check if it was initialized
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
                // This might happen if WallAndStumpManager hasn't initialized yet
                // But since we are using bootstrap order, we can control this if WallAndStumpManager also uses Bootstrap
                // For now, just log warning if not found
                Debug.LogWarning("[GuardManager] WallAndStumpManager not found during init! Will retry in Update or when needed.");
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

        /// <summary>
        /// Đăng ký một archer mới vào hệ thống
        /// GỌI HÀM NÀY từ ArcherBehavior.Start() hoặc DenizenController khi chuyển role
        /// </summary>
        /// <param name="archer">GameObject của archer</param>
        public void RegisterArcher(GameObject archer)
        {
            if (archer == null)
            {
                Debug.LogWarning("[GuardManager] Cannot register null archer");
                return;
            }

            // Check if already registered
            if (leftFlankArchers.Contains(archer) || rightFlankArchers.Contains(archer))
            {
                Debug.LogWarning($"[GuardManager] Archer {archer.name} already registered");
                return;
            }

            // Get archer's status component
            var archerStatus = archer.GetComponent<Script.Data_Model.Status.ArcherStatus>();
            if (archerStatus == null)
            {
                Debug.LogError($"[GuardManager] Archer {archer.name} missing ArcherStatus component!");
                return;
            }

            // Assign to flank based on current balance
            // Strategy: Keep flanks balanced (equal or ±1)
            if (leftFlankArchers.Count <= rightFlankArchers.Count)
            {
                leftFlankArchers.Add(archer);
                archerStatus.isLeftFlank = true;
                DebugLog($"✅ Registered {archer.name} to LEFT flank (Total: {leftFlankArchers.Count} left, {rightFlankArchers.Count} right)");
            }
            else
            {
                rightFlankArchers.Add(archer);
                archerStatus.isLeftFlank = false;
                DebugLog($"✅ Registered {archer.name} to RIGHT flank (Total: {leftFlankArchers.Count} left, {rightFlankArchers.Count} right)");
            }

            // Update stats
            UpdateStatistics();
        }

        /// <summary>
        /// Hủy đăng ký archer (khi chết hoặc chuyển role)
        /// GỌI HÀM NÀY từ ArcherBehavior.OnDestroy() hoặc khi archer chết
        /// </summary>
        /// <param name="archer">GameObject của archer</param>
        public void UnregisterArcher(GameObject archer)
        {
            if (archer == null) return;

            bool wasRemoved = false;

            if (leftFlankArchers.Contains(archer))
            {
                leftFlankArchers.Remove(archer);
                wasRemoved = true;
                DebugLog($"❌ Unregistered {archer.name} from LEFT flank (Remaining: {leftFlankArchers.Count})");
            }
            else if (rightFlankArchers.Contains(archer))
            {
                rightFlankArchers.Remove(archer);
                wasRemoved = true;
                DebugLog($"❌ Unregistered {archer.name} from RIGHT flank (Remaining: {rightFlankArchers.Count})");
            }

            if (wasRemoved)
            {
                UpdateStatistics();
            }
        }

        #endregion

        #region Alarm System

        /// <summary>
        /// Raise alarm khi có enemy xuất hiện hoặc archer bị tấn công
        /// GỌI HÀM NÀY từ ArcherBehavior.TakeDamage() hoặc khi archer detect enemy
        /// </summary>
        /// <param name="enemy">Enemy đã trigger alarm</param>
        public void RaiseAlarm(GameObject enemy)
        {
            if (enemy == null)
            {
                Debug.LogWarning("[GuardManager] Cannot raise alarm with null enemy");
                return;
            }

            // If alarm already active, just update source and reset timer
            if (isAlarmActive)
            {
                alarmSource = enemy;
                alarmTimer = alarmCooldownTime;
                // Don't spam notifications
                return;
            }

            // Activate alarm
            isAlarmActive = true;
            alarmSource = enemy;
            alarmTimer = alarmCooldownTime;

            Debug.Log($"[GuardManager] 🚨 ALARM RAISED! Enemy: {enemy.name} at position {enemy.transform.position}");

            // Notify all archers about the threat
            NotifyAllArchersOfThreat(enemy);
        }

        /// <summary>
        /// Tắt alarm thủ công
        /// </summary>
        public void ClearAlarm()
        {
            if (!isAlarmActive) return;

            isAlarmActive = false;
            alarmSource = null;
            alarmTimer = 0f;

            DebugLog("✅ Alarm cleared");
        }

        /// <summary>
        /// Update alarm timer
        /// </summary>
        private void Update()
        {
            if (isAlarmActive)
            {
                alarmTimer -= Time.deltaTime;

                if (alarmTimer <= 0f)
                {
                    DebugLog("⏰ Alarm timeout - auto clearing");
                    ClearAlarm();
                }
            }
        }

        /// <summary>
        /// Thông báo tất cả archer về mối đe dọa
        /// NOTE: Archer sẽ tự xử lý state transition trong UpdateBehavior()
        /// </summary>
        private void NotifyAllArchersOfThreat(GameObject enemy)
        {
            int notifiedCount = 0;

            // Notify left flank
            foreach (var archer in leftFlankArchers)
            {
                if (archer == null) continue;
                
                // Just log - archer will handle state in their own UpdateBehavior()
                DebugLog($"📢 Notified {archer.name} (LEFT) about threat: {enemy.name}");
                notifiedCount++;
            }

            // Notify right flank
            foreach (var archer in rightFlankArchers)
            {
                if (archer == null) continue;
                
                DebugLog($"📢 Notified {archer.name} (RIGHT) about threat: {enemy.name}");
                notifiedCount++;
            }

            DebugLog($"Total archers notified: {notifiedCount}");
        }

        #endregion

        #region Wall Management Integration

        /// <summary>
        /// Nhận thông báo từ WallAndStumpManager khi wall HP < threshold
        /// GỌI HÀM NÀY từ WallAndStumpManager.CheckWallCriticalHP()
        /// </summary>
        /// <param name="wall">Wall bị critical</param>
        /// <param name="isLeftSide">Wall ở bên trái hay phải</param>
        public void NotifyWallCritical(GameObject wall, bool isLeftSide)
        {
            if (wall == null)
            {
                Debug.LogWarning("[GuardManager] NotifyWallCritical called with null wall");
                return;
            }

            var wallStatus = wall.GetComponent<WallStatus>();
            string hpInfo = wallStatus != null ? $"HP={wallStatus.currentHp}" : "HP=???";

            Debug.Log($"[GuardManager] ⚠️ WALL CRITICAL: {wall.name} ({hpInfo}) on {(isLeftSide ? "LEFT" : "RIGHT")} side");

            // Notify archers on that flank
            List<GameObject> affectedArchers = isLeftSide ? leftFlankArchers : rightFlankArchers;

            int notifiedCount = 0;
            foreach (var archer in affectedArchers)
            {
                if (archer == null) continue;

                var archerStatus = archer.GetComponent<Script.Data_Model.Status.ArcherStatus>();
                if (archerStatus == null) continue;

                // Check if this archer is assigned to this specific wall
                if (archerStatus.assignedWall == wall)
                {
                    DebugLog($"🚨 Notifying {archer.name} that their wall {wall.name} is critical!");
                    notifiedCount++;
                    
                    // Archer will detect this in their HandleHoldingWallState() 
                    // and transition to RetreatingToInnerWall automatically
                }
            }

            DebugLog($"Notified {notifiedCount} archers about wall {wall.name} being critical");
        }

        /// <summary>
        /// Lấy số lượng archer đang phòng thủ một wall cụ thể
        /// </summary>
        public int GetArcherCountAtWall(GameObject wall, bool isLeftSide)
        {
            if (wall == null) return 0;

            List<GameObject> archers = isLeftSide ? leftFlankArchers : rightFlankArchers;
            int count = 0;

            foreach (var archer in archers)
            {
                if (archer == null) continue;

                var archerStatus = archer.GetComponent<Script.Data_Model.Status.ArcherStatus>();
                if (archerStatus != null && archerStatus.assignedWall == wall)
                {
                    count++;
                }
            }

            return count;
        }

        #endregion

        #region Statistics & Info

        /// <summary>
        /// Cập nhật các thống kê
        /// </summary>
        private void UpdateStatistics()
        {
            totalArchers = leftFlankArchers.Count + rightFlankArchers.Count;
            
            // Count archers in combat (optional - implement later)
            archersInCombat = 0;
            // TODO: Count archers with currentState == AttackingFromWall
        }

        /// <summary>
        /// Lấy tổng số archer
        /// </summary>
        public int GetTotalArchers()
        {
            return totalArchers;
        }

        /// <summary>
        /// Lấy số archer ở một flank
        /// </summary>
        public int GetFlankArcherCount(bool isLeftFlank)
        {
            return isLeftFlank ? leftFlankArchers.Count : rightFlankArchers.Count;
        }

        /// <summary>
        /// Kiểm tra xem một flank có cần thêm archer không
        /// </summary>
        public bool FlankNeedsReinforcement(bool isLeftFlank)
        {
            int flankCount = GetFlankArcherCount(isLeftFlank);
            int otherFlankCount = GetFlankArcherCount(!isLeftFlank);

            // Cần reinforcement nếu flank này ít archer hơn bên kia >= 2
            return (otherFlankCount - flankCount) >= 2;
        }

        #endregion

        #region Debug Helpers

        /// <summary>
        /// Debug log với option để bật/tắt
        /// </summary>
        private void DebugLog(string message)
        {
            if (showDebugLogs)
            {
                Debug.Log($"[GuardManager] {message}");
            }
        }

        /// <summary>
        /// In ra phân bổ archer hiện tại
        /// </summary>
        [ContextMenu("Debug: Print Archer Distribution")]
        public void DebugPrintArcherDistribution()
        {
            Debug.Log("=== GUARD MANAGER - ARCHER DISTRIBUTION ===");
            Debug.Log($"Total Archers: {totalArchers}");
            Debug.Log($"LEFT Flank: {leftFlankArchers.Count} archers");
            
            foreach (var archer in leftFlankArchers)
            {
                if (archer == null) continue;
                var status = archer.GetComponent<Script.Data_Model.Status.ArcherStatus>();
                string wallInfo = status?.assignedWall != null ? $"at {status.assignedWall.name}" : "no wall assigned";
                Debug.Log($"  - {archer.name} {wallInfo}");
            }

            Debug.Log($"RIGHT Flank: {rightFlankArchers.Count} archers");
            
            foreach (var archer in rightFlankArchers)
            {
                if (archer == null) continue;
                var status = archer.GetComponent<Script.Data_Model.Status.ArcherStatus>();
                string wallInfo = status?.assignedWall != null ? $"at {status.assignedWall.name}" : "no wall assigned";
                Debug.Log($"  - {archer.name} {wallInfo}");
            }

            Debug.Log($"Alarm Status: {(isAlarmActive ? "🚨 ACTIVE" : "✅ CLEAR")}");
            if (isAlarmActive)
            {
                Debug.Log($"  - Source: {alarmSource?.name ?? "unknown"}");
                Debug.Log($"  - Time remaining: {alarmTimer:F1}s");
            }
        }

        /// <summary>
        /// Test raise alarm
        /// </summary>
        [ContextMenu("Debug: Test Raise Alarm")]
        public void DebugTestRaiseAlarm()
        {
            GameObject dummyEnemy = new GameObject("TestEnemy");
            RaiseAlarm(dummyEnemy);
            Destroy(dummyEnemy, 2f);
        }

        /// <summary>
        /// Test clear alarm
        /// </summary>
        [ContextMenu("Debug: Clear Alarm")]
        public void DebugClearAlarm()
        {
            ClearAlarm();
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Clear all lists
            leftFlankArchers.Clear();
            rightFlankArchers.Clear();
        }

        #endregion
    }
}

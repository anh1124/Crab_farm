using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model;
using Script.Data_Model.Interfaces;

namespace Script.Game.Mobs.MobsManager
{
    [RequireComponent(typeof(GuardAlarmSystem))]
    [RequireComponent(typeof(GuardSquadSystem))]
    [RequireComponent(typeof(GuardWallSystem))]
    public class GuardManager : MonoBehaviour, IDataListBootstrap, IGuardProvider
    {

        public GuardAlarmSystem Alarm { get; private set; }
        public GuardSquadSystem Squad { get; private set; }
        public GuardWallSystem Wall { get; private set; }
        
        public float archerHuntingRange => _archerHuntingRange;
        [SerializeField] private float _archerHuntingRange = 25f;

        [Header("Visualization Debug")]
        public bool isDebugHuntingRange = true;
        public bool showDebugLogs = true;

        private Script.Manager.Building.BuildingManager _bm;
        private Script.Manager.Building.BuildingManager BM => _bm != null ? _bm : (_bm = FindObjectOfType<Script.Manager.Building.BuildingManager>());

        private void Awake()
        {
            Alarm = GetComponent<GuardAlarmSystem>();
            if (Alarm == null) Alarm = gameObject.AddComponent<GuardAlarmSystem>();
            
            Squad = GetComponent<GuardSquadSystem>();
            if (Squad == null) Squad = gameObject.AddComponent<GuardSquadSystem>();
            
            Wall = GetComponent<GuardWallSystem>();
            if (Wall == null) Wall = gameObject.AddComponent<GuardWallSystem>();
        }

        public void Setup(Script.Manager.Building.BuildingPlacementManager placementManager, Script.Manager.EntitySaveManager saveManager)
        {
            // Inject dependencies
            Debug.Log("[GuardManager] Dependencies wired via Setup.");
        }

        private void InitializeSingleton()
        {
            // Singleton removed
        }

        #region IDataListBootstrap Implementation
        public int Order => 1; 

        public void InitializeData(PrefabData context)
        {
            InitializeSingleton();
            // Sub-systems self-initialize via Start() where possible
            DebugLog("✅ GuardManager initialized via Bootstrap");
        }
        #endregion

        #region Delegation 

        // Public getters/setters specifically used by other components to preserve compatibility
        public bool isEnemyAttack 
        { 
            get => Alarm != null && Alarm.isEnemyAttack; 
            set { if (Alarm != null) Alarm.isEnemyAttack = value; } 
        }

        public List<GameObject> leftFlankArchers => Squad?.leftFlankArchers;
        public List<GameObject> rightFlankArchers => Squad?.rightFlankArchers;
        public List<GameObject> detectedEnemies => Alarm?.detectedEnemies;

        public GameObject currentLeftWall => Wall?.currentLeftWall;
        public GameObject currentRightWall => Wall?.currentRightWall;

        public int archerSlotCount => Squad != null ? Squad.archerSlotCount : 7;
        public float archerSlotSpacing => Squad != null ? Squad.archerSlotSpacing : 1.2f;
        public float defaultSlotOffset => Squad != null ? Squad.defaultSlotOffset : 3.0f;

        public float alarmTimer => Alarm != null ? Alarm.alarmTimer : 0f;

        public void RegisterArcher(GameObject archer) => Squad?.RegisterArcher(archer);
        public void UnregisterArcher(GameObject archer) => Squad?.UnregisterArcher(archer);
        public Vector3 GetDefensePositionForArcher(GameObject archer) => Squad != null ? Squad.GetDefensePositionForArcher(archer) : archer.transform.position;
        public void RebalanceSquads() => Squad?.RebalanceSquads();

        public void OnWallListUpdated() => Wall?.OnWallListUpdated();
        public GameObject GetCurrentDefensiveWall(bool isLeft) => Wall?.GetCurrentDefensiveWall(isLeft);
        public void UpdateDefensiveWalls() => Wall?.UpdateDefensiveWalls();

        public void RegisterEnemy(GameObject enemy) => Alarm?.RegisterEnemy(enemy);
        public void ClearAlarm() => Alarm?.ClearAlarm();
        public void NotifyTownCenterUnderAttack()
        {
            if (Alarm != null && !Alarm.isEnemyAttack)
            {
                Alarm.isEnemyAttack = true;
                Alarm.alarmTimer = Alarm.alarmCooldownDuration;
                Squad?.NotifyAllArchersValues(true);
                DebugLog("[GuardManager] ⚔️ TownCenter under attack - emergency defense mode enabled.");
            }

            Wall?.ForceFrontlineToPlaza();
        }

        #endregion

        #region Logs & Visuals

        public void DebugLog(string message)
        {
            if (showDebugLogs) Debug.Log($"[GuardManager] {message}");
        }

        private void Update()
        {
            UpdateHuntingRangeVisualization();
        }

        public Vector3? GetHuntingRangeStartPosition(bool isLeft)
        {
            GameObject wall = isLeft ? Wall?.currentLeftWall : Wall?.currentRightWall;
            if (wall != null) return wall.transform.position;

            // Fallback to farthest point of plaza if no wall found
            if (BM != null && BM.currentTownCenter != null)
            {
                var tc = BM.currentTownCenter.transform.position;
                var tcBehavior = BM.currentTownCenter.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
                float plazaDistance = 10f; // default
                if (tcBehavior != null) plazaDistance = tcBehavior.plazaDistance;

                // Bắt đầu từ điểm xa nhất của plaza
                float plazaEdgeX = isLeft ? (tc.x - plazaDistance) : (tc.x + plazaDistance);
                return new Vector3(plazaEdgeX, tc.y, tc.z);
            }

            return null;
        }
        
        private void UpdateHuntingRangeVisualization()
        {
             Vector3? leftStart = GetHuntingRangeStartPosition(true);
             if (leftStart.HasValue) DrawHuntingRange(leftStart.Value, true);
                 
             Vector3? rightStart = GetHuntingRangeStartPosition(false);
             if (rightStart.HasValue) DrawHuntingRange(rightStart.Value, false);
        }
        
        private void DrawHuntingRange(Vector3 wallPos, bool isLeft)
        {
            if (!isDebugHuntingRange) return;
            float range = archerHuntingRange;
            Vector3 rayDir = isLeft ? Vector3.left : Vector3.right;
            // Elevate ray for better visibility (+2m)
            Debug.DrawRay(wallPos + Vector3.up * 2.0f, rayDir * range, Color.blue);
        }

        [ContextMenu("Debug Archer Positions")]
        public void DebugArcherPositions()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== ARCHER POSITION DEBUG INFO ===");
            sb.AppendLine($"Enemy Attack: {isEnemyAttack}");
            
            float plazaDistance = -1f;
            if (BM != null && BM.currentTownCenter != null)
            {
                var tc = BM.currentTownCenter;
                sb.AppendLine($"TownCenter Pos: {tc.transform.position}");
                var tcBehavior = tc.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
                if (tcBehavior != null)
                {
                    plazaDistance = tcBehavior.plazaDistance;
                    sb.AppendLine($"Plaza Distance: {plazaDistance}");
                }
                else sb.AppendLine("Plaza Distance: N/A (Behavior Missing)");
            }
            else
            {
                sb.AppendLine("TownCenter: NULL");
            }
            
            if (Squad != null)
            {
                sb.AppendLine("--- SLOT SETTINGS (Inspector) ---");
                sb.AppendLine($"archerSlotCount: {Squad.archerSlotCount}");
                sb.AppendLine($"defaultSlotOffset: {Squad.defaultSlotOffset}");
                sb.AppendLine($"archerSlotSpacing: {Squad.archerSlotSpacing}");
                sb.AppendLine($"rowDepthOffset: {Squad.rowDepthOffset}");
                sb.AppendLine($"currentLeftWall: {(currentLeftWall != null ? currentLeftWall.name : "NULL")}");
                sb.AppendLine($"currentRightWall: {(currentRightWall != null ? currentRightWall.name : "NULL")}");

                sb.AppendLine($"--- LEFT FLANK ({Squad.leftFlankArchers.Count}) ---");
                for(int i=0; i<Squad.leftFlankArchers.Count; i++)
                {
                    var archer = Squad.leftFlankArchers[i];
                    if(archer != null)
                    {
                        int slotIndex = i % Squad.archerSlotCount;
                        int rowIndex = i / Squad.archerSlotCount;
                        float requestedOffset = Squad.defaultSlotOffset + (slotIndex * Squad.archerSlotSpacing) + (rowIndex * Squad.rowDepthOffset);
                        float effectiveOffset = requestedOffset;
                        bool isPlazaFallback = currentLeftWall == null;
                        if (isPlazaFallback && plazaDistance > 0f)
                        {
                            float maxSafeOffset = Mathf.Max(0.5f, plazaDistance - 0.5f);
                            effectiveOffset = Mathf.Min(requestedOffset, maxSafeOffset);
                        }

                        sb.AppendLine($"[{i}] Slot: {slotIndex} Row: {rowIndex} | Pos: {archer.transform.position} | Target: {Squad.GetDefensePositionForArcher(archer)}");
                        sb.AppendLine($"     requestedOffset: {requestedOffset:F2} | effectiveOffset: {effectiveOffset:F2} | mode: {(isPlazaFallback ? "PlazaFallback" : "WallDefense")}");
                    }
                }
                
                sb.AppendLine($"--- RIGHT FLANK ({Squad.rightFlankArchers.Count}) ---");
                for(int i=0; i<Squad.rightFlankArchers.Count; i++)
                {
                    var archer = Squad.rightFlankArchers[i];
                    if(archer != null)
                    {
                        int slotIndex = i % Squad.archerSlotCount;
                        int rowIndex = i / Squad.archerSlotCount;
                        float requestedOffset = Squad.defaultSlotOffset + (slotIndex * Squad.archerSlotSpacing) + (rowIndex * Squad.rowDepthOffset);
                        float effectiveOffset = requestedOffset;
                        bool isPlazaFallback = currentRightWall == null;
                        if (isPlazaFallback && plazaDistance > 0f)
                        {
                            float maxSafeOffset = Mathf.Max(0.5f, plazaDistance - 0.5f);
                            effectiveOffset = Mathf.Min(requestedOffset, maxSafeOffset);
                        }

                        sb.AppendLine($"[{i}] Slot: {slotIndex} Row: {rowIndex} | Pos: {archer.transform.position} | Target: {Squad.GetDefensePositionForArcher(archer)}");
                        sb.AppendLine($"     requestedOffset: {requestedOffset:F2} | effectiveOffset: {effectiveOffset:F2} | mode: {(isPlazaFallback ? "PlazaFallback" : "WallDefense")}");
                    }
                }
            }
            Debug.Log(sb.ToString());
        }

        [ContextMenu("Debug Clear Enemies")]
        public void DebugClearEnemies()
        {
            Alarm?.DebugClearEnemies();
        }

        #endregion
    }
}

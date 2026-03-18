using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model;
using Script.Data_Model.Interfaces;

namespace Script.Game.Mobs.MobsManager
{
    [RequireComponent(typeof(GuardAlarmSystem))]
    [RequireComponent(typeof(GuardSquadSystem))]
    [RequireComponent(typeof(GuardWallSystem))]
    public class GuardManager : MonoBehaviour, IDataListBootstrap
    {
        public static GuardManager Instance { get; private set; }

        public GuardAlarmSystem Alarm { get; private set; }
        public GuardSquadSystem Squad { get; private set; }
        public GuardWallSystem Wall { get; private set; }

        [Header("Debug Settings")]
        public bool showDebugLogs = false;
        
        [Header("Visualization Debug")]
        public bool isDebugHuntingRange = true;
        public float archerHuntingRange = 25f;

        private void Awake()
        {
            InitializeSingleton();
             
            Alarm = GetComponent<GuardAlarmSystem>();
            if (Alarm == null) Alarm = gameObject.AddComponent<GuardAlarmSystem>();
            
            Squad = GetComponent<GuardSquadSystem>();
            if (Squad == null) Squad = gameObject.AddComponent<GuardSquadSystem>();
            
            Wall = GetComponent<GuardWallSystem>();
            if (Wall == null) Wall = gameObject.AddComponent<GuardWallSystem>();
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
            
            if (Script.Manager.Building.BuildingManager.instance != null && Script.Manager.Building.BuildingManager.instance.currentTownCenter != null)
                return Script.Manager.Building.BuildingManager.instance.currentTownCenter.transform.position;
            
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
            Debug.DrawRay(wallPos, rayDir * range, Color.blue);
        }

        [ContextMenu("Debug Archer Positions")]
        public void DebugArcherPositions()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== ARCHER POSITION DEBUG INFO ===");
            
            if (Script.Manager.Building.BuildingManager.instance != null && Script.Manager.Building.BuildingManager.instance.currentTownCenter != null)
            {
                var tc = Script.Manager.Building.BuildingManager.instance.currentTownCenter;
                sb.AppendLine($"TownCenter Pos: {tc.transform.position}");
                var tcBehavior = tc.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
                if (tcBehavior != null) sb.AppendLine($"Plaza Distance: {tcBehavior.plazaDistance}");
                else sb.AppendLine("Plaza Distance: N/A (Behavior Missing)");
            }
            else
            {
                sb.AppendLine("TownCenter: NULL");
            }
            
            if (Squad != null)
            {
                sb.AppendLine($"--- LEFT FLANK ({Squad.leftFlankArchers.Count}) ---");
                for(int i=0; i<Squad.leftFlankArchers.Count; i++)
                {
                    var archer = Squad.leftFlankArchers[i];
                    if(archer != null)
                    {
                        int slotIndex = i % Squad.archerSlotCount;
                        sb.AppendLine($"[{i}] Slot: {slotIndex} | Pos: {archer.transform.position} | Target: {Squad.GetDefensePositionForArcher(archer)}");
                    }
                }
                
                sb.AppendLine($"--- RIGHT FLANK ({Squad.rightFlankArchers.Count}) ---");
                for(int i=0; i<Squad.rightFlankArchers.Count; i++)
                {
                    var archer = Squad.rightFlankArchers[i];
                    if(archer != null)
                    {
                        int slotIndex = i % Squad.archerSlotCount;
                        sb.AppendLine($"[{i}] Slot: {slotIndex} | Pos: {archer.transform.position} | Target: {Squad.GetDefensePositionForArcher(archer)}");
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

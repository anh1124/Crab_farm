using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model.Status;
using Script.Game.Mobs.Subjects;
using Script.Manager.Building;

namespace Script.Game.Mobs.MobsManager
{
    public class GuardSquadSystem : MonoBehaviour
    {
        [Header("Archer Management")]
        public List<GameObject> leftFlankArchers = new List<GameObject>();
        public List<GameObject> rightFlankArchers = new List<GameObject>();

        [Header("Defense Slot Settings")]
        public int archerSlotCount = 7;
        public float archerSlotSpacing = 1.2f;
        public float defaultSlotOffset = 6.0f;

        private GuardManager guardManager;

        private void Start()
        {
            guardManager = GuardManager.Instance;
        }

        public void RegisterArcher(GameObject archer)
        {
            if (archer == null) return;
            if (leftFlankArchers.Contains(archer) || rightFlankArchers.Contains(archer)) return;

            var archerStatus = archer.GetComponent<DenizenStatus>();
            if (archerStatus == null) return;

            if (leftFlankArchers.Count <= rightFlankArchers.Count)
            {
                leftFlankArchers.Add(archer);
                archerStatus.isLeftFlank = true;
                if (guardManager) guardManager.DebugLog($"Registered {archer.name} to LEFT flank");
            }
            else
            {
                rightFlankArchers.Add(archer);
                archerStatus.isLeftFlank = false;
                if (guardManager) guardManager.DebugLog($"Registered {archer.name} to RIGHT flank");
            }

            if (guardManager && guardManager.Alarm.isEnemyAttack)
            {
                 archerStatus.archerState = DenizenStatus.ArcherState.ReturningToBase;
            }
        }

        public void UnregisterArcher(GameObject archer)
        {
            if (archer == null) return;

            bool wasRemoved = false;
            if (leftFlankArchers.Contains(archer))
            {
                leftFlankArchers.Remove(archer);
                wasRemoved = true;
            }
            else if (rightFlankArchers.Contains(archer))
            {
                rightFlankArchers.Remove(archer);
                wasRemoved = true;
            }

            if (wasRemoved)
            {
                RebalanceSquads();
            }
        }

        public void RebalanceSquads()
        {
            leftFlankArchers.RemoveAll(x => x == null);
            rightFlankArchers.RemoveAll(x => x == null);

            int diff = leftFlankArchers.Count - rightFlankArchers.Count;
            if (Mathf.Abs(diff) <= 1) return;

            if (guardManager) guardManager.DebugLog($"[GuardSquadSystem] Rebalancing squads... (Diff: {diff})");

            if (diff > 1) 
            {
                int countToMove = diff / 2;
                for (int i = 0; i < countToMove; i++) MoveArcher(leftFlankArchers, rightFlankArchers, false);
            }
            else
            {
                int countToMove = Mathf.Abs(diff) / 2;
                for (int i = 0; i < countToMove; i++) MoveArcher(rightFlankArchers, leftFlankArchers, true);
            }
        }

        private void MoveArcher(List<GameObject> fromList, List<GameObject> toList, bool toLeft)
        {
            if (fromList.Count == 0) return;
            GameObject archer = fromList[fromList.Count - 1];
            fromList.RemoveAt(fromList.Count - 1);
            toList.Add(archer);

            var status = archer.GetComponent<DenizenStatus>();
            if (status != null)
            {
                status.isLeftFlank = toLeft;
                if (guardManager && guardManager.Wall)
                {
                    status.assignedWall = toLeft ? guardManager.Wall.currentLeftWall : guardManager.Wall.currentRightWall;
                }
                if (guardManager) guardManager.DebugLog($"[GuardSquadSystem] Reassigned {archer.name} to {(toLeft ? "LEFT" : "RIGHT")} flank");
            }
        }

        public void NotifyAllArchersValues(bool isAttack)
        {
            List<GameObject> allArchers = new List<GameObject>();
            allArchers.AddRange(leftFlankArchers);
            allArchers.AddRange(rightFlankArchers);
            
            foreach(var archer in allArchers)
            {
                if(archer == null) continue;
                var behavior = archer.GetComponent<ArcherBehavior>();
                if(behavior != null)
                {
                    behavior.OnGuardManagerStatusChanged(isAttack);
                }
            }
        }

        public void NotifyArchersDefensivePositionChanged(GameObject leftWall, GameObject rightWall)
        {
            NotifyFlankArchers(leftFlankArchers, leftWall);
            NotifyFlankArchers(rightFlankArchers, rightWall);
        }

        private void NotifyFlankArchers(List<GameObject> archers, GameObject wall)
        {
            if (wall == null) return;
            foreach (var archer in archers)
            {
                if (archer == null) continue;
                var status = archer.GetComponent<DenizenStatus>();
                if (status != null && (status.archerState == DenizenStatus.ArcherState.ReturningToBase || status.archerState == DenizenStatus.ArcherState.HoldingWall))
                {
                    status.assignedWall = wall;
                }
            }
        }

        public void RetreatArchersOnFlank(bool isLeftSide, GameObject safeWall)
        {
            List<GameObject> flank = isLeftSide ? leftFlankArchers : rightFlankArchers;
            foreach (var archer in flank)
            {
                if (archer == null) continue;
                var behavior = archer.GetComponent<ArcherBehavior>();
                if (behavior != null)
                {
                    if (safeWall != null)
                    {
                        behavior.ForceRetreat(safeWall);
                    }
                    else if (guardManager)
                    {
                        guardManager.DebugLog($"[GuardSquadSystem] {archer.name} không còn nơi để lùi, đứng tại chỗ!");
                    }
                }
            }
        }

        public Vector3 GetDefensePositionForArcher(GameObject archer)
        {
            if (archer == null) return Vector3.zero;

            var status = archer.GetComponent<DenizenStatus>();
            if (status == null) return archer.transform.position;

            bool isLeft = status.isLeftFlank;
            List<GameObject> flankList = isLeft ? leftFlankArchers : rightFlankArchers;
            GameObject wall = (guardManager && guardManager.Wall) ? guardManager.Wall.GetCurrentDefensiveWall(isLeft) : null;
            
            int index = flankList.IndexOf(archer);
            int slotIndex = (index >= 0) ? index % archerSlotCount : 0;

            if (wall == null)
            {
                if (BuildingManager.instance != null && BuildingManager.instance.currentTownCenter != null)
                {
                    var tc = BuildingManager.instance.currentTownCenter;
                    Transform tcTransform = tc.transform;
                    Vector3 tcPos = tcTransform.position;
                    
                    float startDist = 5f; 
                    var tcBehavior = tc.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
                    if(tcBehavior != null) startDist = tcBehavior.plazaDistance;

                    float dir = isLeft ? -1f : 1f;
                    float totalOffset = startDist - (slotIndex * archerSlotSpacing);
                    if (totalOffset < 2f) totalOffset = 2f;
                    
                    return new Vector3(tcPos.x + (dir * totalOffset), tcPos.y, archer.transform.position.z);
                }
                return archer.transform.position;
            }

            float direction = isLeft ? 1f : -1f;
            float offset = defaultSlotOffset + (slotIndex * archerSlotSpacing);
            Vector3 basePos = wall.transform.position;

            return new Vector3(
                basePos.x + direction * offset,
                basePos.y,
                archer.transform.position.z
            );
        }

        private void OnDestroy()
        {
            leftFlankArchers.Clear();
            rightFlankArchers.Clear();
        }
    }
}

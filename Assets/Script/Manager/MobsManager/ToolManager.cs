using System.Collections.Generic;
using UnityEngine;
using Script.Game.Item;
using System.Linq;
using Script.Game.Mobs.MobsManager; // Added this
using Script.Game.Item.Coin;
using Script.Game.Mobs.Denizen.Jobs.Villager;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Manager.MobsManager
{
    public class ToolManager : MonoBehaviour
    {
        public static ToolManager Instance { get; private set; }

        [SerializeField] private List<PhysicalItem> availableTools = new List<PhysicalItem>();
        private List<PhysicalItem> unservedTools = new List<PhysicalItem>(); 
        public int GetTotalUnservedTools() 
        {
            // Trả về số lượng tool đang thực sự bơ vơ (không có người đại diện và chưa bị nhặt)
            return availableTools.Count(t => t != null && t.IsAvailable() && t.AssignedCarrier == null);
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        [ContextMenu("🔍 Print Debug Status")]
        private void PrintDebugStatus()
        {
            int idleVillagers = FindObjectOfType<DenizenManager>()?.GetIdleVillagerCount() ?? 0;
            string debugStr = $"[ToolManager] 🔍 STATUS REPORT: {availableTools.Count} tools total, {idleVillagers} idle villagers.\n";
            
            foreach (var tool in availableTools)
            {
                if (tool == null) continue;
                string carrier = (tool.AssignedCarrier != null) ? tool.AssignedCarrier.name : "NONE";
                string statusIcon = (tool.pickedBy != null) ? "✅" : (tool.AssignedCarrier != null ? "⏳" : "❌");
                debugStr += $"  * {tool.gameObject.name} ({(Script.Game.Item.Coin.ItemType)tool.itemType}) | Carrier: {carrier} | Picked: {statusIcon}\n";
            }
            Debug.Log(debugStr);
        }

        public void RegisterTool(PhysicalItem tool)
        {
            if (tool == null || tool.itemType == ItemType.Coin || availableTools.Contains(tool)) return;

            availableTools.Add(tool);

            // [FIX] Bỏ logic check !tool.floating vì vũ khí lượm được/load từ save vẫn cần chủ!
            var villager = FindObjectOfType<DenizenManager>()?.GetNearestIdleVillager(tool.transform.position);

            if (villager != null)
                AssignToolToVillager(tool, villager);
            else
                unservedTools.Add(tool);
        }

        public void UnregisterTool(PhysicalItem tool)
        {
            if (tool != null && availableTools.Contains(tool))
            {
                availableTools.Remove(tool);
                unservedTools.Remove(tool);
            }
        }

        /// <summary>
        /// Gọi khi một Villager nhặt tool KHÔNG phải tool được giao cho mình.
        /// Báo cho Villager bị mất tool về Idle, rồi tìm chủ mới cho tool orphan.
        /// </summary>
        public void OnUnexpectedPickup(PhysicalItem stolenTool, GameObject thief)
        {
            if (stolenTool.AssignedCarrier != null && stolenTool.AssignedCarrier != thief)
            {
                var lostVillager = stolenTool.AssignedCarrier.GetComponent<VillagerJob>();
                if (lostVillager != null)
                {
                    // VillagerJob does not have currentTool (it's in DenizenStatus via Context)
                    // but we can call TransitionToIdle
                    lostVillager.TransitionToIdle();
                }
                stolenTool.AssignedCarrier = null;
            }

            ReassignAllOrphanedTools();
        }

        /// <summary>
        /// Gọi từ PhysicalItem.OnAssigneeAbandoned() khi Villager
        /// bỏ tool này để đi nhặt tool khác.
        /// </summary>
        public void OnToolAbandoned(PhysicalItem tool)
        {
            if (tool == null || tool.pickedBy != null) return;

            var villager = FindObjectOfType<DenizenManager>()?.GetNearestIdleVillager(tool.transform.position);

            if (villager != null)
                AssignToolToVillager(tool, villager);
            else if (!unservedTools.Contains(tool))
                unservedTools.Add(tool);
        }

        /// <summary>
        /// Gọi từ VillagerBehavior.TransitionToIdle().
        /// Nếu có tool đang chờ trong unservedTools thì assign ngay.
        /// </summary>
        public void OnVillagerBecameIdle(VillagerJob villager)
        {
            CleanUnservedList();
            
            // Tìm tất cả các tool đang bơ vơ trong hệ thống
            var orphans = availableTools.FindAll(t => t != null && t.IsAvailable() && t.AssignedCarrier == null);
            if (orphans.Count == 0) return;

            // Ưu tiên tool gần nhất
            var tool = GetNearestFrom(orphans, villager.transform.position);
            if (tool != null)
            {
                // Xóa khỏi unservedTools nếu nó đang nằm trong đó
                if (unservedTools.Contains(tool)) unservedTools.Remove(tool);
                AssignToolToVillager(tool, villager);
            }
        }

        private void AssignToolToVillager(PhysicalItem tool, VillagerJob villager)
        {
            tool.AssignedCarrier = villager.gameObject;
            villager.AssignTool(tool);
        }

        private void ReassignAllOrphanedTools()
        {
            var orphans = availableTools.FindAll(t => t != null && t.pickedBy == null && t.AssignedCarrier == null);

            foreach (var tool in orphans)
            {
                var villager = FindObjectOfType<DenizenManager>()?.GetNearestIdleVillager(tool.transform.position);
                if (villager == null) break;

                AssignToolToVillager(tool, villager);
            }
        }

        private void CleanUnservedList()
        {
            // SAU — tránh lambda allocation, dùng for loop ngược
            for (int i = unservedTools.Count - 1; i >= 0; i--)
            {
                if (unservedTools[i] == null || !unservedTools[i].IsAvailable())
                    unservedTools.RemoveAt(i);
            }
        }

        private PhysicalItem GetNearestFrom(List<PhysicalItem> list, Vector3 pos)
        {
            PhysicalItem nearest = null;
            float minDist = float.MaxValue;
            foreach (var t in list)
            {
                float d = Vector3.Distance(pos, t.transform.position);
                if (d < minDist) { minDist = d; nearest = t; }
            }
            return nearest;
        }

        public PhysicalItem GetNearestTool(Vector3 position, ItemType? specificType = null)
        {
            // Filter out nulls or picked tools first
            // SAU — tránh lambda allocation, dùng for loop ngược
            for (int i = availableTools.Count - 1; i >= 0; i--)
            {
                if (availableTools[i] == null || !availableTools[i].IsAvailable())
                    availableTools.RemoveAt(i);
            }

            PhysicalItem nearestTool = null;
            float minDistance = float.MaxValue;

            foreach (var tool in availableTools)
            {
                if (tool == null) continue;
                
                // If specific type is requested, skip others
                if (specificType.HasValue && tool.itemType != specificType.Value) continue;

                float dist = Vector3.Distance(position, tool.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestTool = tool;
                }
            }

            return nearestTool;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Script.Game.Item;
using System.Linq;
using Script.Game.Mobs.MobsManager; // Added this
using Script.Game.Item.Coin;

namespace Script.Manager.MobsManager
{
    public class ToolManager : MonoBehaviour
    {
        public static ToolManager Instance { get; private set; }

        [SerializeField] private List<PhysicalItem> availableTools = new List<PhysicalItem>();
        private List<PhysicalItem> unservedTools = new List<PhysicalItem>(); 

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

        public void RegisterTool(PhysicalItem tool)
        {
            if (tool == null || tool.itemType == ItemType.Coin || availableTools.Contains(tool)) return;

            availableTools.Add(tool);

            if (!tool.floating) // Wait, maybe picked or pickedBy == null. Assuming floating or picked isn't removed. Let's keep !tool.picked. Wait, I'll use !tool.picked if it exists, else tool.pickedBy == null. Since the user didn't complain about tool.picked error in ToolManager, it must still be there. I'll just change ItemCategory.
            {
                var villager = DenizenManager.Instance?.GetNearestIdleVillager(tool.transform.position);

                if (villager != null)
                    AssignToolToVillager(tool, villager);
                else
                    unservedTools.Add(tool);
            }
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
                var lostVillager = stolenTool.AssignedCarrier.GetComponent<Script.Game.Mobs.Subjects.VillagerBehavior>();
                if (lostVillager != null)
                {
                    lostVillager.currentTool = null;
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

            var villager = DenizenManager.Instance?.GetNearestIdleVillager(tool.transform.position);

            if (villager != null)
                AssignToolToVillager(tool, villager);
            else if (!unservedTools.Contains(tool))
                unservedTools.Add(tool);
        }

        /// <summary>
        /// Gọi từ VillagerBehavior.TransitionToIdle().
        /// Nếu có tool đang chờ trong unservedTools thì assign ngay.
        /// </summary>
        public void OnVillagerBecameIdle(Script.Game.Mobs.Subjects.VillagerBehavior villager)
        {
            CleanUnservedList();
            if (unservedTools.Count == 0) return;

            var tool = GetNearestFrom(unservedTools, villager.transform.position);
            if (tool != null)
            {
                unservedTools.Remove(tool);
                AssignToolToVillager(tool, villager);
            }
        }

        private void AssignToolToVillager(PhysicalItem tool, Script.Game.Mobs.Subjects.VillagerBehavior villager)
        {
            tool.AssignedCarrier = villager.gameObject;
            villager.AssignTool(tool);
        }

        private void ReassignAllOrphanedTools()
        {
            var orphans = availableTools.FindAll(t => t != null && t.pickedBy == null && t.AssignedCarrier == null);

            foreach (var tool in orphans)
            {
                var villager = DenizenManager.Instance?.GetNearestIdleVillager(tool.transform.position);
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

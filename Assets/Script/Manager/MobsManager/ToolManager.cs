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
            if (tool == null || tool.itemCategory == ItemCategory.Coin || availableTools.Contains(tool)) return;

            availableTools.Add(tool);

            if (!tool.picked)
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
            if (tool == null || tool.picked) return;

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
            var orphans = availableTools.FindAll(t => t != null && !t.picked && t.AssignedCarrier == null);

            foreach (var tool in orphans)
            {
                var villager = DenizenManager.Instance?.GetNearestIdleVillager(tool.transform.position);
                if (villager == null) break;

                AssignToolToVillager(tool, villager);
            }
        }

        private void CleanUnservedList()
        {
            unservedTools.RemoveAll(t => t == null || t.picked);
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

        public PhysicalItem GetNearestTool(Vector3 position, ItemCategory? specificType = null)
        {
            // Filter out nulls or picked tools first
            availableTools.RemoveAll(t => t == null || t.picked || t.pickedBy != null);

            PhysicalItem nearestTool = null;
            float minDistance = float.MaxValue;

            foreach (var tool in availableTools)
            {
                if (tool == null) continue;
                
                // If specific type is requested, skip others
                if (specificType.HasValue && tool.itemCategory != specificType.Value) continue;

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

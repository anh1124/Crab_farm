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
            if (tool != null && tool.itemCategory != ItemCategory.Coin && !availableTools.Contains(tool))
            {
                availableTools.Add(tool);
                Debug.Log($"[ToolManager] Registered tool: {tool.name}. Total tools: {availableTools.Count}");
                
                // Try to find a villager immediately
                if (DenizenManager.Instance != null && !tool.picked)
                {
                    var villager = DenizenManager.Instance.GetNearestIdleVillager(tool.transform.position);
                    if (villager != null)
                    {
                        villager.AssignTool(tool);
                    }
                }
            }
        }

        public void UnregisterTool(PhysicalItem tool)
        {
            if (tool != null && availableTools.Contains(tool))
            {
                availableTools.Remove(tool);
                Debug.Log($"[ToolManager] Unregistered tool: {tool.name}. Total tools remaining: {availableTools.Count}");
            }
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

using System.Collections.Generic;
using UnityEngine;
using Script.Game.Item;
using System.Linq;
using Script.Game.Mobs.MobsManager; // Added this

namespace Script.Manager.MobsManager
{
    public class ToolManager : MonoBehaviour
    {
        public static ToolManager Instance { get; private set; }

        [SerializeField] private List<ToolItem> availableTools = new List<ToolItem>();

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

        public void RegisterTool(ToolItem tool)
        {
            if (tool != null && !availableTools.Contains(tool))
            {
                availableTools.Add(tool);
                
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

        public void UnregisterTool(ToolItem tool)
        {
            if (tool != null && availableTools.Contains(tool))
            {
                availableTools.Remove(tool);
            }
        }

        public ToolItem GetNearestTool(Vector3 position, ToolType? specificType = null)
        {
            // Filter out nulls or picked tools first
            availableTools.RemoveAll(t => t == null || t.picked || t.pickedBy != null);

            ToolItem nearestTool = null;
            float minDistance = float.MaxValue;

            foreach (var tool in availableTools)
            {
                if (tool == null) continue;
                
                // If specific type is requested, skip others
                if (specificType.HasValue && tool.type != specificType.Value) continue;

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

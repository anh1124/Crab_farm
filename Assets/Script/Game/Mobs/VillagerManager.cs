using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Game.Mobs
{
    public class VillagerManager : MonoBehaviour
    {
        public static VillagerManager Instance { get; private set; }

        // Tool data structure
        [Serializable]
        public class ToolData
        {
            public Vector2 Position;
            public GameObject ToolObject;

            public ToolData(Vector2 pos, GameObject obj)
            {
                Position = pos;
                ToolObject = obj;
            }
        }

        // Lists for tracking
        [SerializeField] private List<ToolData> availableTools = new List<ToolData>();
        [SerializeField] private List<GameObject> villagers = new List<GameObject>();

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

        #region Tool Management
        public void RegisterTool(GameObject tool)
        {
            if (tool != null)
            {
                Vector2 position = tool.transform.position;
                availableTools.Add(new ToolData(position, tool));
                Debug.Log($"[VillagerManager] Tool registered: {tool.name} at position {position}");
            }
        }

        public void UnregisterTool(GameObject tool)
        {
            availableTools.RemoveAll(t => t.ToolObject == tool);
            Debug.Log($"[VillagerManager] Tool unregistered: {tool.name}");
        }

        public List<ToolData> GetAvailableTools()
        {
            // Clean up any null references first
            availableTools.RemoveAll(t => t.ToolObject == null);
            return availableTools;
        }

        public ToolData GetNearestTool(Vector2 position)
        {
            float nearestDistance = float.MaxValue;
            ToolData nearestTool = null;

            foreach (var tool in availableTools.ToArray()) // Use ToArray to avoid modification during enumeration
            {
                if (tool.ToolObject == null)
                {
                    availableTools.Remove(tool);
                    continue;
                }

                float distance = Vector2.Distance(position, tool.Position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTool = tool;
                }
            }

            return nearestTool;
        }
        #endregion

        #region Villager Management
        public void RegisterVillager(GameObject villager)
        {
            if (villager != null && !villagers.Contains(villager))
            {
                villagers.Add(villager);
                Debug.Log($"[VillagerManager] Villager registered: {villager.name}");
            }
        }

        public void UnregisterVillager(GameObject villager)
        {
            if (villagers.Contains(villager))
            {
                villagers.Remove(villager);
                Debug.Log($"[VillagerManager] Villager unregistered: {villager.name}");
            }
        }

        public List<GameObject> GetVillagers()
        {
            // Clean up any null references
            villagers.RemoveAll(v => v == null);
            return villagers;
        }

        public GameObject GetRandomVillager()
        {
            // Clean up null references first
            villagers.RemoveAll(v => v == null);
            
            if (villagers.Count == 0)
            {
                return null;
            }

            int randomIndex = UnityEngine.Random.Range(0, villagers.Count);
            return villagers[randomIndex];
        }
        #endregion
    }
} 
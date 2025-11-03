using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Script.Game.Item;
using Script.Game.Mobs.Subjects;

namespace Script.Game.Mobs
{
    /// <summary>
    /// Quản lý việc phân công tool cho villager một cách tối ưu.
    /// 
    /// Cách hoạt động:
    /// 1. Khi có tool mới -> Tìm villager gần nhất chưa có tool để giao nhiệm vụ
    /// 2. Khi có villager mới -> Tìm tool gần nhất chưa được gán để giao cho villager
    /// 3. Sử dụng cache để lưu khoảng cách, tránh tính toán lại nhiều lần
    /// 4. Tự động dọn dẹp cache định kỳ để tránh memory leak
    /// </summary>
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
        private List<GameObject> villagers = new List<GameObject>();
        private List<ToolData> availableTools = new List<ToolData>();

        // Cache khoảng cách để tránh tính toán lại
        private Dictionary<(GameObject villager, GameObject tool), float> distanceCache = new Dictionary<(GameObject villager, GameObject tool), float>();
        
        // Thời gian để clear cache
        private float cacheClearInterval = 2f;
        private float lastCacheClearTime;

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

        private void Update()
        {
            // Clear cache định kỳ để tránh memory leak
            if (Time.time - lastCacheClearTime > cacheClearInterval)
            {
                ClearDistanceCache();
                lastCacheClearTime = Time.time;
            }
        }

        #region Tool Management
        public void RegisterTool(GameObject tool)
        {
            if (tool != null)
            {
                Vector2 position = tool.transform.position;
                var toolData = new ToolData(position, tool);
                availableTools.Add(toolData);
                
                // Khi có tool mới, tìm villager gần nhất để assign
                AssignToolToNearestVillager(toolData);
            }
        }

        public void UnregisterTool(GameObject tool)
        {
            availableTools.RemoveAll(t => t.ToolObject == tool);
            // Clear cache entries cho tool này
            ClearCacheEntriesForTool(tool);
        }

        private void AssignToolToNearestVillager(ToolData toolData)
        {
            var availableVillager = FindNearestAvailableVillager(toolData.Position);
            if (availableVillager != null)
            {
                var villagerBehavior = availableVillager.GetComponent<VillagerBehavior>();
                if (villagerBehavior != null && villagerBehavior.currentTool == null)
                {
                    villagerBehavior.currentTool = toolData.ToolObject;
                }
            }
        }

        private GameObject FindNearestAvailableVillager(Vector2 toolPosition)
        {
            float nearestDistance = float.MaxValue;
            GameObject nearestVillager = null;

            foreach (var villager in villagers)
            {
                if (villager == null) continue;

                var villagerBehavior = villager.GetComponent<VillagerBehavior>();
                if (villagerBehavior != null && villagerBehavior.currentTool == null)
                {
                    float distance = GetCachedDistance(villager, toolPosition);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestVillager = villager;
                    }
                }
            }

            return nearestVillager;
        }

        private float GetCachedDistance(GameObject villager, Vector2 position)
        {
            var key = (villager, null as GameObject);
            if (distanceCache.TryGetValue(key, out float distance))
            {
                return distance;
            }

            distance = Vector2.Distance(villager.transform.position, position);
            distanceCache[key] = distance;
            return distance;
        }

        private void ClearDistanceCache()
        {
            distanceCache.Clear();
        }

        private void ClearCacheEntriesForTool(GameObject tool)
        {
            var keysToRemove = distanceCache.Keys.Where(k => k.Item2 == tool).ToList();
            foreach (var key in keysToRemove)
            {
                distanceCache.Remove(key);
            }
        }
        #endregion

        #region Villager Management
        public void RegisterVillager(GameObject villager)
        {
            if (villager != null && !villagers.Contains(villager))
            {
                villagers.Add(villager);
                // Khi có villager mới, kiểm tra xem có tool nào cần assign không
                CheckForUnassignedTools(villager);
            }
        }

        public void UnregisterVillager(GameObject villager)
        {
            villagers.Remove(villager);
            // Clear cache entries cho villager này
            ClearCacheEntriesForVillager(villager);
        }

        private void CheckForUnassignedTools(GameObject villager)
        {
                var villagerBehavior = villager.GetComponent<VillagerBehavior>();
                if (villagerBehavior != null && villagerBehavior.currentTool == null)
            {
                // Tìm tool gần nhất chưa được assign
                var nearestTool = FindNearestUnassignedTool(villager.transform.position);
                if (nearestTool != null)
                {
                        villagerBehavior.currentTool = nearestTool;
                }
            }
        }

        private GameObject FindNearestUnassignedTool(Vector2 villagerPosition)
        {
            float nearestDistance = float.MaxValue;
            GameObject nearestTool = null;

            foreach (var toolData in availableTools)
            {
                if (toolData.ToolObject == null) continue;

                // Kiểm tra xem tool đã được assign chưa
                bool isAssigned = false;
                foreach (var v in villagers)
                {
                    if (v == null) continue;
                var vBehavior = v.GetComponent<VillagerBehavior>();
                if (vBehavior != null && vBehavior.currentTool == toolData.ToolObject)
                    {
                        isAssigned = true;
                        break;
                    }
                }

                if (!isAssigned)
                {
                    float distance = Vector2.Distance(villagerPosition, toolData.Position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestTool = toolData.ToolObject;
                    }
                }
            }

            return nearestTool;
        }

        private void ClearCacheEntriesForVillager(GameObject villager)
        {
            var keysToRemove = distanceCache.Keys.Where(k => k.Item1 == villager).ToList();
            foreach (var key in keysToRemove)
            {
                distanceCache.Remove(key);
            }
        }
        #endregion
    }
} 
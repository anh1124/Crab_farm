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

        // Lists for tracking villagers and tools
        public List<GameObject> allVillagers = new List<GameObject>();  // Public for access in AssignToolToNearestVillager
        private List<ToolItem> availableTools = new List<ToolItem>();

        // Cache khoảng cách để tránh tính toán lại
        private Dictionary<(GameObject villager, ToolItem tool), float> distanceCache = new Dictionary<(GameObject villager, ToolItem tool), float>();
        
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
        /// <summary>
        /// Register a new tool with the manager. Called by ToolItem.OnEnable()
        /// </summary>
        public void RegisterTool(ToolItem tool)
        {
            if (tool != null && !tool.picked && !availableTools.Contains(tool))
            {
                availableTools.Add(tool);
                
                // Khi có tool mới, tìm villager gần nhất để assign
                AssignToolToNearestVillager(tool);
            }
        }

        /// <summary>
        /// Unregister a tool from the manager. Called by ToolItem.OnDisable()
        /// </summary>
        public void UnregisterTool(ToolItem tool)
        {
            availableTools.Remove(tool);
            // Clear cache entries cho tool này
            ClearCacheEntriesForTool(tool);
        }

        /// <summary>
        /// Assign a tool to the nearest available villager
        /// </summary>
        private void AssignToolToNearestVillager(ToolItem tool)
        {
            if (tool == null || tool.picked) return;

            float minDist = Mathf.Infinity;
            VillagerBehavior nearest = null;

            foreach (var villager in allVillagers)
            {
                if (villager == null) continue;

                var villagerBehavior = villager.GetComponent<VillagerBehavior>();
                if (villagerBehavior == null || villagerBehavior.currentTool != null) continue;

                float dist = Mathf.Abs(villager.transform.position.x - tool.transform.position.x);

                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = villagerBehavior;
                }
            }

            if (nearest != null)
            {
                nearest.currentTool = tool;
                availableTools.Remove(tool);
                Debug.Log($"[VillagerManager] Assigned {tool.type} tool to {nearest.gameObject.name} at distance {minDist:F2}");
            }
            else
            {
                Debug.LogWarning($"[VillagerManager] No available villager found for {tool.type} tool at position {tool.transform.position}");
            }
        }

        private void ClearDistanceCache()
        {
            distanceCache.Clear();
        }

        private void ClearCacheEntriesForTool(ToolItem tool)
        {
            var keysToRemove = distanceCache.Keys.Where(k => k.Item2 == tool).ToList();
            foreach (var key in keysToRemove)
            {
                distanceCache.Remove(key);
            }
        }
        #endregion

        #region Villager Management
        // New API preferred by DenizenManager
        public void OnRegister(GameObject denizen)
        {
            if (denizen == null) return;
            var ctrl = denizen.GetComponent<DenizenController>();
            if (ctrl == null || ctrl.CurrentRole != NPCRole.Villager) return;

            if (!allVillagers.Contains(denizen))
            {
                allVillagers.Add(denizen);
                CheckForUnassignedTools(denizen);
            }
        }

        public void OnUnregister(GameObject denizen)
        {
            if (denizen == null) return;
            allVillagers.Remove(denizen);
            ClearCacheEntriesForVillager(denizen);
        }

        public void RegisterVillager(GameObject villager)
        {
            if (villager != null && !allVillagers.Contains(villager))
            {
                allVillagers.Add(villager);
                // Khi có villager mới, kiểm tra xem có tool nào cần assign không
                CheckForUnassignedTools(villager);
            }
        }

        public void UnregisterVillager(GameObject villager)
        {
            allVillagers.Remove(villager);
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

        private ToolItem FindNearestUnassignedTool(Vector2 villagerPosition)
        {
            float nearestDistance = float.MaxValue;
            ToolItem nearestTool = null;

            foreach (var tool in availableTools)
            {
                if (tool == null || tool.picked) continue;

                // Kiểm tra xem tool đã được assign chưa
                bool isAssigned = false;
                foreach (var v in allVillagers)
                {
                    if (v == null) continue;
                    var vBehavior = v.GetComponent<VillagerBehavior>();
                    if (vBehavior != null && vBehavior.currentTool == tool)
                    {
                        isAssigned = true;
                        break;
                    }
                }

                if (!isAssigned)
                {
                    float distance = Vector2.Distance(villagerPosition, tool.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestTool = tool;
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
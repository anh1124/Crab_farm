using System;
using System.Collections.Generic;
using Script.Data_Model.Status;
using UnityEngine;

namespace Script.Manager.Building
{
    /// <summary>
    /// Hệ thống registry chứa thông tin về các công trình có thể xây.
    /// Map BuildingType -> (Prefab + UI Sprite).
    /// Tách biệt UI sprite khỏi PrefabData để PrefabData chỉ chứa game prefabs.
    /// </summary>
    public class BuildingRegistry : MonoBehaviour
    {
        public static BuildingRegistry Instance { get; private set; }

        [Serializable]
        public class BuildingEntry
        {
            [Tooltip("Loại công trình")]
            public BuildingType buildingType;

            [Tooltip("Prefab của công trình (tham chiếu đến PrefabData)")]
            public GameObject prefab;

            [Tooltip("Sprite hiển thị trên UI build menu")]
            public Sprite uiSprite;

            [Tooltip("Tên hiển thị trên UI (nếu null sẽ dùng prefab.name)")]
            public string displayName;
        }

        [Header("Building Entries")]
        [Tooltip("Danh sách map BuildingType -> (Prefab + UI Sprite)")]
        public List<BuildingEntry> buildingEntries;

        private Dictionary<BuildingType, BuildingEntry> buildingDict;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeRegistry();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeRegistry()
        {
            buildingDict = new Dictionary<BuildingType, BuildingEntry>();

            if (buildingEntries == null)
            {
                Debug.LogError("[BuildingRegistry] buildingEntries is null!");
                return;
            }

            foreach (var entry in buildingEntries)
            {
                if (entry.buildingType == BuildingType.TownCenter && entry.prefab == null)
                {
                    // Auto-assign from PrefabData if not set
                    entry.prefab = Script.Data_Model.PrefabData.Instance.townCenterList[0];
                }

                if (entry.buildingType == BuildingType.Wall && entry.prefab == null)
                {
                    entry.prefab = Script.Data_Model.PrefabData.Instance.prefabWall;
                }

                if (entry.buildingType == BuildingType.HammerVendor && entry.prefab == null)
                {
                    entry.prefab = Script.Data_Model.PrefabData.Instance.hammerVendor;
                }

                if (entry.buildingType == BuildingType.BowVendor && entry.prefab == null)
                {
                    entry.prefab = Script.Data_Model.PrefabData.Instance.bowVendor;
                }

                // Fallback: extract sprite from prefab if uiSprite not set
                if (entry.uiSprite == null && entry.prefab != null)
                {
                    SpriteRenderer sr = entry.prefab.GetComponent<SpriteRenderer>();
                    if (sr == null)
                    {
                        sr = entry.prefab.GetComponentInChildren<SpriteRenderer>();
                    }
                    if (sr != null)
                    {
                        entry.uiSprite = sr.sprite;
                    }
                }

                if (!buildingDict.ContainsKey(entry.buildingType))
                {
                    buildingDict.Add(entry.buildingType, entry);
                }
                else
                {
                    Debug.LogWarning($"[BuildingRegistry] Duplicate BuildingType {entry.buildingType} detected. Skipping.");
                }
            }

            Debug.Log($"[BuildingRegistry] Initialized with {buildingDict.Count} building entries.");
        }

        /// <summary>
        /// Lấy prefab cho một loại công trình
        /// </summary>
        public GameObject GetPrefab(BuildingType buildingType)
        {
            if (buildingDict.TryGetValue(buildingType, out BuildingEntry entry))
            {
                return entry.prefab;
            }

            Debug.LogWarning($"[BuildingRegistry] No prefab found for BuildingType: {buildingType}");
            return null;
        }

        /// <summary>
        /// Lấy UI sprite cho một loại công trình
        /// </summary>
        public Sprite GetUISprite(BuildingType buildingType)
        {
            if (buildingDict.TryGetValue(buildingType, out BuildingEntry entry))
            {
                return entry.uiSprite;
            }

            Debug.LogWarning($"[BuildingRegistry] No UI sprite found for BuildingType: {buildingType}");
            return null;
        }

        /// <summary>
        /// Lấy tên hiển thị cho một loại công trình
        /// </summary>
        public string GetDisplayName(BuildingType buildingType)
        {
            if (buildingDict.TryGetValue(buildingType, out BuildingEntry entry))
            {
                return string.IsNullOrEmpty(entry.displayName) ? entry.prefab?.name : entry.displayName;
            }

            Debug.LogWarning($"[BuildingRegistry] No display name found for BuildingType: {buildingType}");
            return buildingType.ToString();
        }

        /// <summary>
        /// Lấy tất cả thông tin cho một loại công trình
        /// </summary>
        public BuildingEntry GetBuildingEntry(BuildingType buildingType)
        {
            if (buildingDict.TryGetValue(buildingType, out BuildingEntry entry))
            {
                return entry;
            }

            Debug.LogWarning($"[BuildingRegistry] No entry found for BuildingType: {buildingType}");
            return null;
        }

        /// <summary>
        /// Kiểm tra xem một loại công trình có trong registry không
        /// </summary>
        public bool HasBuilding(BuildingType buildingType)
        {
            return buildingDict.ContainsKey(buildingType);
        }
    }
}

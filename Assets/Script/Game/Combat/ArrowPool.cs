using System.Collections.Generic;
using UnityEngine;

namespace Script.Game.Combat
{
    /// <summary>
    /// Hybrid Arrow Pool:
    /// - Preload: 50 arrows
    /// - Max: 250 arrows
    /// - Auto-expand when needed
    /// </summary>
    public class ArrowPool : MonoBehaviour, IArrowPool
    {
        [Header("Pool Configuration")]
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private int minPoolSize = 50;   // Preload
        [SerializeField] private int maxPoolSize = 250;  // Hard limit
        [SerializeField] private Transform poolParent;
        
        private Queue<GameObject> pool = new Queue<GameObject>();
        private int totalCreated = 0;

        private Script.Data_Model.PrefabData _prefabData;

        public void Setup(Script.Data_Model.PrefabData prefabData)
        {
            _prefabData = prefabData;
        }

        public void Init()
        {
            if (_prefabData != null && _prefabData.arrow != null)
            {
                Initialize(_prefabData.arrow);
            }
            else if (arrowPrefab != null)
            {
                Initialize(arrowPrefab);
            }
            Debug.Log("✅ ArrowPool initialized via Init()");
        }
        
        private void Awake()
        {
            if (poolParent == null)
            {
                GameObject parentObj = new GameObject("ArrowPool_Container");
                poolParent = parentObj.transform;
                poolParent.SetParent(transform);
            }
            
            // Legacy/Inspector support
            if (arrowPrefab != null)
            {
                Initialize(arrowPrefab);
            }
        }

        public void Initialize(GameObject prefab) 
        {
            if (prefab == null) 
            {
                Debug.LogError("[ArrowPool] Cannot initialize with null prefab!");
                return;
            }

            // Avoid double initialization if called manually after Inspector setup
            if (totalCreated > 0 && arrowPrefab == prefab) return;

            arrowPrefab = prefab;

            // Preload minimum arrows
            for (int i = 0; i < minPoolSize; i++)
            {
                CreateArrow();
            }
            
            Debug.Log($"[ArrowPool] Initialized: {minPoolSize} arrows preloaded, max {maxPoolSize}");
        }
        
        public GameObject GetArrow()
        {
            if (pool.Count == 0)
            {
                if (totalCreated < maxPoolSize)
                {
                    CreateArrow();
                    Debug.Log($"[ArrowPool] Expanded: {totalCreated}/{maxPoolSize}");
                }
                else
                {
                    Debug.LogWarning($"[ArrowPool] Limit reached! {maxPoolSize} arrows active.");
                    return null; 
                }
            }
            
            if (pool.Count == 0) return null; // Double check

            GameObject arrow = pool.Dequeue();
            arrow.SetActive(true);
            return arrow;
        }
        
        public void ReturnArrow(GameObject arrow)
        {
            if (arrow == null) return;

            // Reset Rigidbody trước khi tắt để đảm bảo velocity không còn
            Rigidbody2D rb = arrow.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.simulated = false;
            }

            arrow.SetActive(false);
            arrow.transform.SetParent(poolParent);
            arrow.transform.position = Vector3.zero;
            arrow.transform.rotation = Quaternion.identity;
            arrow.transform.localScale = Vector3.one;

            pool.Enqueue(arrow);
        }
        
        private void CreateArrow()
        {
            if (arrowPrefab == null)
            {
                Debug.LogError("[ArrowPool] Arrow Prefab is missing!");
                return;
            }

            GameObject arrow = Instantiate(arrowPrefab, poolParent);
            arrow.SetActive(false);
            
            ArrowController controller = arrow.GetComponent<ArrowController>();
            if (controller != null)
            {
                controller.SetPool(this);
            }
            
            pool.Enqueue(arrow);
            totalCreated++;
        }
        
        public void LogPoolStats()
        {
            int available = pool.Count;
            int active = totalCreated - available;
            float usagePercent = (totalCreated > 0) ? (active / (float)totalCreated) * 100f : 0f;
            
            if (active >= maxPoolSize * 0.8f)
            {
                Debug.LogWarning($"[ArrowPool] CẢNH BÁO: Pool gần hết! Active: {active}/{maxPoolSize} ({usagePercent:F1}%)");
            }
            else
            {
                Debug.Log($"[ArrowPool] Stats - Total: {totalCreated}, Active: {active}, Available: {available}, Usage: {usagePercent:F1}%");
            }
        }
        
        /// <summary>
        /// Kiểm tra xem pool có đang bị cạn kiện không (active > 80% max)
        /// </summary>
        public bool IsPoolNearExhausted()
        {
            int available = pool.Count;
            int active = totalCreated - available;
            return active >= maxPoolSize * 0.8f;
        }
        
        /// <summary>
        /// Lấy số arrows đang active (không còn trong pool)
        /// </summary>
        public int GetActiveArrowCount()
        {
            return totalCreated - pool.Count;
        }
    }
}

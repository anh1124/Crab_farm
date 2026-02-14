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
    public class ArrowPool : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] private GameObject arrowPrefab;
        [SerializeField] private int minPoolSize = 50;   // Preload
        [SerializeField] private int maxPoolSize = 250;  // Hard limit
        [SerializeField] private Transform poolParent;
        
        public static ArrowPool Instance { get; private set; }

        private Queue<GameObject> pool = new Queue<GameObject>();
        private int totalCreated = 0;
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (poolParent == null)
            {
                GameObject parentObj = new GameObject("ArrowPool_Container");
                poolParent = parentObj.transform;
                poolParent.SetParent(transform);
            }
            
            // If prefab is already set via Inspector, initialize
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
            
            arrow.SetActive(false);
            arrow.transform.SetParent(poolParent);
            arrow.transform.position = Vector3.zero;
            arrow.transform.rotation = Quaternion.identity;
            
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
            Debug.Log($"[ArrowPool] Stats - Total: {totalCreated}, Active: {active}, Available: {available}");
        }
    }
}

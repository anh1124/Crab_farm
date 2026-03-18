using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model;

namespace Script.Game.Item.Coin
{
    /// <summary>
    /// Pool quản lý Coin dựa trên thiết kế của ArrowPool.
    /// Script này được tự động AddComponent vào CoinContainer bởi EntitySaveManager.
    /// </summary>
    public class CoinPool : MonoBehaviour
    {
        [Header("Pool Configuration")]
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private int minPoolSize = 30;   // Preload
        [SerializeField] private int maxPoolSize = 250;  // Hard limit
        
        public static CoinPool Instance { get; private set; }

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
                Destroy(this); // Nếu có CoinPool khác chạy trước, hủy component này đi
                return;
            }
        }

        private void Start()
        {
            // Tự động tìm coinPrefab từ PrefabData nếu inspector null
            if (coinPrefab == null && PrefabData.Instance != null)
            {
                coinPrefab = PrefabData.Instance.coinPrefab;
            }

            if (coinPrefab != null)
            {
                Initialize(coinPrefab);
            }
            else
            {
                Debug.LogError("[CoinPool] Missing Coin Prefab in PrefabData!");
            }
        }

        public void Initialize(GameObject prefab) 
        {
            if (prefab == null) 
            {
                Debug.LogError("[CoinPool] Cannot initialize with null prefab!");
                return;
            }

            if (totalCreated > 0 && coinPrefab == prefab) return;

            coinPrefab = prefab;

            // Preload coins ban đầu
            for (int i = 0; i < minPoolSize; i++)
            {
                CreateCoin();
            }
            
            Debug.Log($"[CoinPool] Initialized: {minPoolSize} coins preloaded, max {maxPoolSize}");
        }
        
        public GameObject GetCoin()
        {
            // Clean up null references caused by EntitySaveManager.RemoveAllChildObjects wiping the container
            while (pool.Count > 0 && pool.Peek() == null)
            {
                pool.Dequeue();
                totalCreated--;
            }

            if (pool.Count == 0)
            {
                if (totalCreated < maxPoolSize)
                {
                    CreateCoin();
                    Debug.Log($"[CoinPool] Expanded: {totalCreated}/{maxPoolSize}");
                }
                else
                {
                    Debug.LogWarning($"[CoinPool] Limit reached! {maxPoolSize} coins active.");
                    return null; 
                }
            }
            
            // Re-check after potential CreateCoin
            while (pool.Count > 0 && pool.Peek() == null)
            {
                pool.Dequeue();
                totalCreated--;
            }

            if (pool.Count == 0) return null;

            GameObject coin = pool.Dequeue();
            if (coin != null) coin.SetActive(true);
            return coin;
        }
        
        public void ReturnCoin(GameObject coin)
        {
            if (coin == null) return;
            
            coin.SetActive(false);
            
            // Trả nó về CoinContainer (hay chính transform của script này)
            coin.transform.SetParent(transform);
            coin.transform.position = Vector3.zero;
            coin.transform.rotation = Quaternion.identity;
            
            // Reset vận tốc vật lý nếu có
            Rigidbody2D rb = coin.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            
            pool.Enqueue(coin);
        }
        
        private void CreateCoin()
        {
            if (coinPrefab == null)
            {
                Debug.LogError("[CoinPool] Coin Prefab is missing!");
                return;
            }

            GameObject coin = Instantiate(coinPrefab, transform);
            coin.SetActive(false);
            
            pool.Enqueue(coin);
            totalCreated++;
        }
    }
}

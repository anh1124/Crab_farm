using System.Collections.Generic;
using UnityEngine;

namespace Script.Game.Enemy
{
    public class EnemyPool : MonoBehaviour
    {
        public static EnemyPool Instance { get; private set; }

        private Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>(System.StringComparer.OrdinalIgnoreCase);
        private Transform _poolParent;

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

            GameObject parentObj = new GameObject("EnemyPool_Container");
            _poolParent = parentObj.transform;
            _poolParent.SetParent(transform);
        }

        // Gọi sau khi TotalEnemyList load xong (từ Start() hoặc EnemyWaveManager)
        public void InitializePools()
        {
            if (TotalEnemyList.Instance == null || TotalEnemyList.Instance.EnemyDataList == null)
            {
                Debug.LogError("[EnemyPool] TotalEnemyList.Instance is null or empty!");
                return;
            }

            // Luồng tạo Pool:
            // 1. TotalEnemyList đọc json EnemyDataConfig.
            // 2. Chắp vá GameObject prefab từ Inspector (circleHead, shieldBot...) sang mỗi object data.
            // 3. EnemyPool chạy vòng lặp với TotalEnemyList.EnemyDataList đã được gắn prefab.
            foreach (var data in TotalEnemyList.Instance.EnemyDataList)
            {
                if (data.enemyPrefab == null)
                {
                    Debug.LogWarning($"[EnemyPool] Prefab missing for {data.enemyName}. Bỏ qua, không tạo pool cho loại này.");
                    continue;
                }

                GameObject subContainer = new GameObject($"Pool_{data.enemyName}");
                subContainer.transform.SetParent(_poolParent);

                Queue<GameObject> queue = new Queue<GameObject>();
                for (int i = 0; i < 20; i++)
                {
                    GameObject enemy = _CreateEnemy(data, subContainer.transform);
                    queue.Enqueue(enemy);
                }

                _pools[data.enemyName] = queue;
                Debug.Log($"[EnemyPool] Initialized 20 '{data.enemyName}'");
            }
        }

        public int GetPoolCount()
        {
            return _pools.Count;
        }

        public GameObject GetEnemy(string enemyName)
        {
            if (!_pools.TryGetValue(enemyName, out var queue))
            {
                Debug.LogWarning($"[EnemyPool] No pool found for '{enemyName}'");
                return null;
            }

            GameObject enemy;
            if (queue.Count == 0)
            {
                // Dynamic pool expansion
                Debug.LogWarning($"[EnemyPool] Pool '{enemyName}' is empty! Creating a new one dynamically...");
                
                var data = TotalEnemyList.Instance.EnemyDataList.Find(d => d.enemyName == enemyName);
                if (data == null || data.enemyPrefab == null) 
                {
                    Debug.LogError($"[EnemyPool] Data missing for expanding '{enemyName}'");
                    return null;
                }
                
                Transform parentTransform = _poolParent.Find($"Pool_{enemyName}");
                if (parentTransform == null) parentTransform = _poolParent;
                enemy = _CreateEnemy(data, parentTransform);
            }
            else
            {
                enemy = queue.Dequeue();
            }

            enemy.SetActive(true);
            
            // Note: EnemyAIController.ResetEnemy(portal) needs to be called by the spawner
            return enemy;
        }

        public void ReturnEnemy(GameObject enemy)
        {
            if (enemy == null) return;
            
            // Tìm tên enemy (có text "(Clone)" phía sau)
            string enemyName = enemy.name.Replace("(Clone)", "").Trim();
            
            if (!_pools.TryGetValue(enemyName, out var queue))
            {
                Debug.LogError($"[EnemyPool] '{enemy.name}' does not belong to any pool! Was it instantiated manually or missing from EnemyData?");
                enemy.SetActive(false); // Fallback
                return;
            }

            enemy.SetActive(false);
            enemy.transform.SetParent(_poolParent.Find($"Pool_{enemyName}"));
            enemy.transform.position = Vector3.zero;
            enemy.transform.rotation = Quaternion.identity;

            queue.Enqueue(enemy);
        }

        private GameObject _CreateEnemy(TotalEnemyList.EnemyDataBase data, Transform parent)
        {
            GameObject enemy = Instantiate(data.enemyPrefab, parent);
            enemy.name = data.enemyName; // Xóa chữ "(Clone)" mặc định của Unity
            enemy.SetActive(false);
            
            // Cài đặt stats lúc sinh ra
            EnemyStatus status = enemy.GetComponent<EnemyStatus>();
            if (status != null)
            {
                status.Setup(data);
            }
            
            return enemy;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Script.Game.Enemy.Core;
using System;

namespace Script.Game.Enemy
{
    public class EnemyPool : MonoBehaviour, IEnemyPool
    {
        private Dictionary<string, Queue<GameObject>> _pools = new Dictionary<string, Queue<GameObject>>(System.StringComparer.OrdinalIgnoreCase);
        private Transform _poolParent;
        private IEnemyDataProvider _enemyDataProvider;
        private Data_Model.PrefabData _prefabData;
        private EnemyTargetManager _targetManager;

        public void Setup(IEnemyDataProvider dataProvider, Data_Model.PrefabData prefabData, EnemyTargetManager targetManager)
        {
            _enemyDataProvider = dataProvider;
            _prefabData = prefabData;
            _targetManager = targetManager;
        }

        private void Awake()
        {
            if (_poolParent == null)
            {
                GameObject parentObj = new GameObject("EnemyPool_Container");
                _poolParent = parentObj.transform;
                _poolParent.SetParent(transform);
            }
        }

        private bool _isInitialized = false;

        // Gọi sau khi TotalEnemyList load xong (từ Start() hoặc EnemyWaveManager)
        public void InitializePools()
        {
            if (_isInitialized) return;

            if (_enemyDataProvider == null || _enemyDataProvider.ReadyToUseEnemyDataList == null)
            {
                Debug.LogError("[EnemyPool] _enemyDataProvider is null or data list is missing!");
                return;
            }
            
            _isInitialized = true;
            foreach (var data in _enemyDataProvider.ReadyToUseEnemyDataList)
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
                
                var data = _enemyDataProvider.ReadyToUseEnemyDataList.Find(d => d.enemyName == enemyName);
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
            
            EnemyAIController controller = enemy.GetComponent<EnemyAIController>();
            if (controller != null)
            {
                controller.InjectDependencies(_targetManager, this.ReturnEnemy);
            }
            
            return enemy;
        }
    }
}

using UnityEngine;
using Script.Manager.WorldManager;
using Script.Utils;
using Script.Manager;
using Script.Manager.GamePlayManager;
using System.Collections;
using System.Collections.Generic;
using Script.Game.Enemy;
using Script.Interfaces;
using Script.Game.Enemy.Interfaces;
using Script.Data_Model.Interfaces;
using System;

namespace Script.Entities.Structures.SummondPortal
{
    [RequireComponent(typeof(EnemyPortalStatus))]
    public class EnemyPortalBehavior : MonoBehaviour, IDamageable, ISaveable
    {
        private EnemyPortalStatus status;

        public bool IsReadyToSpawn 
        { 
            get 
            { 
                if (status == null) status = GetComponent<EnemyPortalStatus>();
                return status != null && status.isReadyToSpawn; 
            } 
        }

        [Header("Spawn Settings")]
        [Tooltip("Thời gian giãn cách giữa mỗi lần sinh quái")]
        public float spawnInterval = 2f;
        
        // Dữ liệu mảng quái vật nhận từ Manager khi đêm xuống
        private List<EnemyWaveData> enemyWaveData;

        private PortalVisualAnimator visualAnimator;

        private void Awake()
        {
            status = GetComponent<EnemyPortalStatus>();
            visualAnimator = GetComponentInChildren<PortalVisualAnimator>();
        }

        private void Start()
        {
            // Đăng ký vào EnemyPortalManager trong Start() vì đây là nơi đảm bảo rằng
            // tất cả Awake() đã chạy xong (kể cả EnemyPortalManager.Awake() set Instance).
            if (EnemyPortalManager.Instance != null)
            {
                GameObject townCenter = Utility.GetTownCenter();
                float centerPosX = townCenter != null ? townCenter.transform.position.x : 0f;

                if (transform.position.x > centerPosX)
                {
                    EnemyPortalManager.Instance.portalR = gameObject;
                    Debug.Log($"[EnemyPortalBehavior] Start: Tự đăng ký vào portalR tại {transform.position}");
                }
                else
                {
                    EnemyPortalManager.Instance.portalL = gameObject;
                    Debug.Log($"[EnemyPortalBehavior] Start: Tự đăng ký vào portalL tại {transform.position}");
                }
            }
            else
            {
                Debug.LogWarning("[EnemyPortalBehavior] EnemyPortalManager.Instance là NULL khi Start()! Portal sẽ không được đăng ký.");
            }

            // Khi load từ Save, nếu isReadyToSpawn đã = true → bật đồ họa sáng luôn
            if (status != null && status.isReadyToSpawn)
            {
                if (visualAnimator) visualAnimator.SetActive();
            }

            // Lắng nghe sự kiện chết từ Status
            status.OnDeath += OnDeathHandler;
        }

        /// <summary>
        /// Được gọi bởi EnemyPortalManager lúc Instaniate GameObject.
        /// </summary>
        public void Setup()
        {
            // Set FPS visual Animator thành Dormant (nghỉ)
            if (visualAnimator)
            {
                visualAnimator.SetDormant();
            }

            // Gọi shake màn hình
            /*
            if (ScreenShakeController.Instance)
            {
                ScreenShakeController.Instance.Shake(0.4f, 0.6f);
            }
            */
            Debug.LogWarning("[ScreenShakeController] Shake(0.4f, 0.6f) triggered but temporarily disabled.");

            // Ghi lại ngày sinh ra cổng
            status.spawnDay = GameTimeManager.Instance.GetElapsedDays();
            Debug.Log($"[EnemyPortal] Cổng vừa xuất hiện tại {transform.position}! Sẽ kích hoạt vào ngày {(status.spawnDay + status.activeDelay)}.");

            // Đăng ký sự kiện kiểm tra mỗi giờ để xem cổng đã sẵn sàng chưa (check buổi sáng)
            GameTimeManager.Instance.SubscribeToHourlyEvent(GameTimeManager.MORNING_HOUR, CheckActivation);
        }

        private void CheckActivation()
        {
            if (status.isReadyToSpawn) return;

            int currentDay = GameTimeManager.Instance.GetElapsedDays();
            // Đã sống đủ ngưỡng delay
            if (currentDay >= status.spawnDay + status.activeDelay)
            {
                ForceActivate();
            }
        }

        public void ForceActivate()
        {
            if (status == null) status = GetComponent<EnemyPortalStatus>();
            if (status.isReadyToSpawn) return; // Đã active rồi thì thôi

            status.isReadyToSpawn = true;
            Debug.Log($"[EnemyPortalBehavior] Cổng tại {transform.position} đã kích hoạt và sẵn sàng sinh quái!");
            
            // Kích hoạt Visual Animator tăng frame rate lên bùng cháy
            if (visualAnimator)
            {
                visualAnimator.SetActive();
            }

            // Hủy đăng ký vì ngày nào sau đó cũng là Active rồi
            if (GameTimeManager.Instance)
            {
                GameTimeManager.Instance.UnsubscribeFromHourlyEvent(GameTimeManager.MORNING_HOUR, CheckActivation);
            }
        }

        // --- IDamageable Implementation ---
        public void ReceiveDamage(float damage, GameObject instigator)
        {
            status.TakeDamage(damage);
        }

        private void OnDeathHandler()
        {
            // Destruction logic is handled in OnDestroy and via this method
            Destroy(gameObject);
        }

        #region ISaveable Implementation

        [Serializable]
        public struct PortalSaveData
        {
            public int spawnDay;
            public bool isReadyToSpawn;
            public float currentHp;
            public float x, y, z;
        }

        public string GetPrefabID()
        {
            return "EnemyPortalPrefab_ID";
        }

        public object CaptureData()
        {
            return new PortalSaveData
            {
                spawnDay = status.spawnDay,
                isReadyToSpawn = status.isReadyToSpawn,
                currentHp = status.CurrentHp,
                x = transform.position.x,
                y = transform.position.y,
                z = transform.position.z
            };
        }

        public void RestoreData(object data)
        {
            if (data is PortalSaveData saveData)
            {
                status.spawnDay = saveData.spawnDay;
                status.isReadyToSpawn = saveData.isReadyToSpawn;
                status.CurrentHp = saveData.currentHp;
                transform.position = new Vector3(saveData.x, saveData.y, saveData.z);

                // --- LOGIC PHỤC HỒI BEHAVIOR ---
                // Sau khi load xong data, nếu portal đã ready -> kích hoạt visual ngay lập tức
                if (status.isReadyToSpawn && visualAnimator != null)
                {
                    visualAnimator.SetActive();
                }
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Báo Manager clear biến Left/Right của nó
            if (EnemyPortalManager.Instance)
            {
                EnemyPortalManager.Instance.OnPortalDestroyed(this);
            }

            // Dọn dẹp event nếu nó chết lúc đang chờ
            if (!status.isReadyToSpawn && GameTimeManager.Instance != null && GameTimeManager.Instance.gameObject)
            {
                GameTimeManager.Instance.UnsubscribeFromHourlyEvent(GameTimeManager.MORNING_HOUR, CheckActivation);
            }

            // Chỉ log khi đây là phá hủy thật trong game,
            // không log khi hệ thống save đang dọn container để load lại (RemoveAllChildObjects).
            if (!EntitySaveManager.IsLoading)
            {
                Debug.Log($"[EnemyPortalBehavior] Cổng tại {transform.position} đã bị phá hủy.");
            }
        }

        #region Spawning Logic (Hợp nhất từ SummondPortal)

        public void StartSpawning()
        {
            // Lấy danh sách enemy từ EnemyWaveManager VÀO LÚC BẮT ĐẦU SPAWN (ban đêm)
            enemyWaveData = EnemyWaveManager.Instance.currentWaveEnemies;
            
            if (enemyWaveData != null)
            {
                Debug.Log($"[EnemyPortalBehavior - {gameObject.name}] Nhận lệnh xuất quân: {enemyWaveData.Count} loại enemy khác nhau.");
                StartCoroutine(SpawnEnemies());
            }
        }

        private IEnumerator SpawnEnemies()
        {
            int totalSpawned = 0;
            foreach (var enemyData in enemyWaveData)
            {
                for (int i = 0; i < enemyData.count; i++)
                {
                    SpawnEnemy(enemyData.enemyData);
                    totalSpawned++;
                    Debug.Log($"[EnemyPortalBehavior - {gameObject.name}] >> Rớt ra quái thứ {totalSpawned}: {enemyData.enemyData.enemyName}");
                    
                    yield return new WaitForSeconds(spawnInterval);
                }
            }
            
            Debug.Log($"[EnemyPortalBehavior - {gameObject.name}] Hoàn thành wave. Đã spawn tổng cộng {totalSpawned} con quái.");
        }

        private void SpawnEnemy(TotalEnemyList.EnemyDataBase enemyData)
        {
            if (enemyData.enemyPrefab != null)
            {
                // Gọi enemy từ Object Pool thay vì Instantiate
                GameObject enemy = EnemyPool.Instance.GetEnemy(enemyData.enemyName);
                if (enemy != null)
                {
                    enemy.transform.position = gameObject.transform.position;
                    enemy.transform.rotation = Quaternion.identity;

                    EnemyAIController controller = enemy.GetComponent<EnemyAIController>();
                    if (controller != null)
                    {
                        // ResetState & thiết lập lại target
                        controller.ResetEnemy(gameObject);
                    }
                    else
                    {
                        Debug.LogWarning($"[EnemyPortalBehavior] {enemyData.enemyName} thiếu Component EnemyAIController!");
                    }
                }
                else
                {
                    Debug.LogWarning($"[EnemyPortalBehavior] Pool không còn đủ {enemyData.enemyName} để spawn!");
                }
            }
            else
            {
                Debug.LogError($"[EnemyPortalBehavior] EnemyPrefab của {enemyData.enemyName} bị trống null!");
            }
        }

        #endregion
    }
}

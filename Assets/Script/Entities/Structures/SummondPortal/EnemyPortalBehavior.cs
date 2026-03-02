using UnityEngine;
using Script.Manager.WorldManager;
using Script.Utils;
using Script.Manager;
using Script.Manager.GamePlayManager;
using System.Collections;
using System.Collections.Generic;
using Script.Game.Enemy;

namespace Script.Entities.Structures.SummondPortal
{
    [RequireComponent(typeof(EnemyPortalStatus))]
    public class EnemyPortalBehavior : MonoBehaviour
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
            // Tự động đăng ký lại với Manager nếu nó được tải lên từ Save game
            if (EnemyPortalManager.Instance != null)
            {
                GameObject townCenter = Utility.GetTownCenter();
                float centerPosX = townCenter != null ? townCenter.transform.position.x : 0f;
                
                if (transform.position.x > centerPosX)
                    EnemyPortalManager.Instance.portalR = gameObject;
                else
                    EnemyPortalManager.Instance.portalL = gameObject;
            }

            // Tự động đẩy bản thân vào EnemyContainer để được Save Game lưu lại
            if (EntitySaveManager.Instance != null && EntitySaveManager.Instance.enemyContainer != null)
            {
                transform.SetParent(EntitySaveManager.Instance.enemyContainer.transform, true);
            }
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
        }

        // Tạm thời Disable / Comment drop loot như ý User, để phần sau 
        /*
        public void TakeDamage(int dmg)
        {
            // hp -= dmg
            // if hp <= 0: DestroyPortal()
        }
        */

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

            Debug.Log($"[EnemyPortalBehavior] Cổng tại {transform.position} đã bị phá hủy.");
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
                // Spawn enemy
                GameObject enemy = Instantiate(enemyData.enemyPrefab, gameObject.transform.position, Quaternion.identity);
            }
            else
            {
                Debug.LogError($"[EnemyPortalBehavior] EnemyPrefab của {enemyData.enemyName} bị trống null!");
            }
        }

        #endregion
    }
}

using UnityEngine;
using Script.Manager.WorldManager;
using Script.Data_Model;
using System.Collections.Generic;
using Script.Entities.Structures.SummondPortal;
using Script.Game.Enemy;

namespace Script.Manager.GamePlayManager
{
    public partial class EnemyPortalManager : MonoBehaviour
    {
        public void RescuePlayerIfStuck(Vector3 portalPos, bool isRightSide)
        {
            if (Script.Controller.PlayerController.PlayerController.Instance == null || 
                Script.Controller.PlayerController.PlayerController.Instance.player == null) return;
            
            GameObject playerObj = Script.Controller.PlayerController.PlayerController.Instance.player;
            float playerX = playerObj.transform.position.x;
            float rescueOffset = rescuePlayerOffset; // Dùng biến thay cho magic number 5f

            if (isRightSide)
            {
                // Nếu player ở bên phải của portal phía bên phải (vùng bị kẹt)
                if (playerX > portalPos.x)
                {
                    Vector3 newPos = playerObj.transform.position;
                    newPos.x = portalPos.x - rescueOffset; // Đẩy vào trong vương quốc (về phía 0)
                    playerObj.transform.position = newPos;
                    Debug.LogWarning($"[EnemyPortalManager] Giải cứu Player bị kẹt bên ngoài cổng PHẢI (x={portalPos.x})! Dịch chuyển về x={newPos.x}");
                }
            }
            else
            {
                // Nếu player ở bên trái của portal phía bên trái (vùng bị kẹt)
                if (playerX < portalPos.x)
                {
                    Vector3 newPos = playerObj.transform.position;
                    newPos.x = portalPos.x + rescueOffset; // Đẩy vào trong vương quốc (về phía 0)
                    playerObj.transform.position = newPos;
                    Debug.LogWarning($"[EnemyPortalManager] Giải cứu Player bị kẹt bên ngoài cổng TRÁI (x={portalPos.x})! Dịch chuyển về x={newPos.x}");
                }
            }
        }

      

        [ContextMenu("Test Spawn Random Portal")]
        public void TestSpawnRandomPortal()
        {
            Debug.Log("[EnemyPortalManager] ---> Nhận lệnh TestSpawnRandomPortal từ ContextMenu...");
            SpawnAvailablePortal();
        }

        [ContextMenu("Spawn Starting Portals")]
        public void SpawnStartingPortals()
        {
            if (Utility.IsTutorialScene())
            {
                Debug.Log("[EnemyPortalManager] Tutorial scene detected. Skipping SpawnStartingPortals().");
                return;
            }
            // FIX: Đảm bảo đã được setup trước khi spawn (tránh lỗi khi ChunkManager gọi trước InitializeEnemyBase)
            // Chỉ auto-setup TimeManager nếu autoRegisterTimeManager = true
            if (!_isSubscribedToTime && autoRegisterTimeManager)
            {
                Debug.LogWarning("[EnemyPortalManager] SpawnStartingPortals() gọi trước khi Setup()! Tự động fallback setup...");
                var tp = FindObjectOfType<GameTimeManager>();
                if (tp != null)
                {
                    _timeProvider = tp;
                    if (_timeProvider != null && !_isSubscribedToTime)
                    {
                        _timeProvider.SubscribeToHourlyEvent(6, SpawnAvailablePortal);
                        _timeProvider.SubscribeToHourlyEvent(6, ProcessFrontlineDelays);
                        _timeProvider.SubscribeToHourlyEvent(19, MovePortalsTowardsTownCenter);
                        _isSubscribedToTime = true;
                        Debug.Log("[EnemyPortalManager] Lazy Setup: Đã đăng ký time events!");
                    }
                }
                else
                {
                    Debug.LogError("[EnemyPortalManager] Không tìm thấy GameTimeManager! Portal sẽ không nhận time events.");
                }
            }

            // Cho Manager tự quản: Luôn cố gắng lấp đầy portal
            SpawnAvailablePortal();
            
            // Tự động kích hoạt cổng tuyến đầu (như yêu cầu "khi khởi đầu sinh ra portal")
            ForceActivateFrontlinePortals();
        }

        private void SpawnAvailablePortal()
        {
            // Bắt đầu chuỗi đệ quy để spawn portal cho cả 2 bên
            SpawnPortalRecursive(false); // Trái
            SpawnPortalRecursive(true);  // Phải
        }

        /// <summary>
        /// Hàm đệ quy để sinh từng portal một cho đến khi đủ số lượng.
        /// </summary>
        private void SpawnPortalRecursive(bool isRightSide, int failSafeCounter = 0)
        {
            // Safety check tuyệt đối chống vô hạn đệ quy nếu có lỗi logic
            if (failSafeCounter > 10)
            {
                Debug.LogError($"[EnemyPortalManager] Đã ngắt đệ quy do chạm ngưỡng failSafe (10 lần). Kiểm tra lỗi SpawnPortal!");
                return;
            }

            var existingPortals = isRightSide ? rightPortals : leftPortals;
            int currentCount = existingPortals.Count;

            // Safety check / Base case (Thoát đệ quy khi đã đạt hoặc vượt max)
            if (currentCount >= maxPortalsPerSide)
            {
                Debug.Log($"[EnemyPortalManager] Đã đủ {maxPortalsPerSide} portal bên {(isRightSide ? "PHẢI" : "TRÁI")}. Dừng đệ quy.");
                return;
            }

            Debug.Log($"[EnemyPortalManager] Đệ quy: Cần sinh portal bên {(isRightSide ? "PHẢI" : "TRÁI")} ({currentCount}/{maxPortalsPerSide})");
            
            // Gọi hàm sinh 1 portal
            bool success = SpawnPortal(isRightSide);

            // Chỉ đệ quy tiếp nếu tạo thành công (tránh loop vô hạn nếu lỗi)
            if (success)
            {
                SpawnPortalRecursive(isRightSide, failSafeCounter + 1);
            }
            else
            {
                Debug.LogWarning($"[EnemyPortalManager] SpawnPortal thất bại, dừng chuỗi đệ quy bên {(isRightSide ? "PHẢI" : "TRÁI")} để tránh loop vô hạn.");
            }
        }

        private bool SpawnPortal(bool isRightSide)
        {
            if (PrefabData.Instance == null || PrefabData.Instance.summoningPortal == null)
            {
                Debug.LogError("[EnemyPortalManager] Không tìm thấy PrefabData hoặc summoningPortal prefab!");
                return false;
            }

            // FIX: Auto-find dependencies nếu chưa được inject (khi spawn trước khi Setup() hoàn tất)
            if (_enemyPool == null)
            {
                _enemyPool = FindObjectOfType<Script.Game.Enemy.EnemyPool>(true);
                Debug.LogWarning($"[EnemyPortalManager] _enemyPool chưa được inject, auto-find: {_enemyPool != null}");
            }
            if (_enemyDataProvider == null)
            {
                _enemyDataProvider = FindObjectOfType<Script.Game.Enemy.TotalEnemyList>(true);
                Debug.LogWarning($"[EnemyPortalManager] _enemyDataProvider chưa được inject, auto-find: {_enemyDataProvider != null}");
            }

            // Sort trước để tìm portal xa nhất (farthest from TownCenter)
            SortPortals();

            Vector3 spawnPos;
            var existingPortals = isRightSide ? rightPortals : leftPortals;
            
            if (existingPortals.Count > 0)
            {
                // Đã có portal - spawn SAU (xa hơn) portal xa nhất hiện tại
                // Portal xa nhất = cuối danh sách sau khi sort (gần nhất = index 0)
                float farthestX = existingPortals[existingPortals.Count - 1].transform.position.x;
                float backOffset = backPortalSpawnOffset > 0 ? backPortalSpawnOffset : 100f;
                
                // Bên phải: xa hơn = +x, Bên trái: xa hơn = -x
                float spawnX = isRightSide ? farthestX + backOffset : farthestX - backOffset;
                
                GameObject townCenter = Utility.GetTownCenter();
                float yPos = townCenter != null ? townCenter.transform.position.y : 0f;
                spawnPos = new Vector3(spawnX, yPos, 0f);
                
                Debug.Log($"[EnemyPortalManager] Spawn portal {(isRightSide ? "PHẢI" : "TRÁI")} SAU portal xa nhất: x={farthestX:F1} -> x={spawnX:F1}");
            }
            else
            {
                // Chưa có portal - spawn ở vị trí random ban đầu
                spawnPos = Utility.GetRandomOutsideSafePos(isRightSide, minSpawnDistance, maxSpawnDistance);
                Debug.Log($"[EnemyPortalManager] Spawn portal {(isRightSide ? "PHẢI" : "TRÁI")} ban đầu tại: {spawnPos}");
            }
            
            // Vector3.zero được trả về nếu lỗi (không có TownCenter)
            if (spawnPos == Vector3.zero) return false;

            // Instantiate prefab
            GameObject newPortal = Instantiate(PrefabData.Instance.summoningPortal, spawnPos, Quaternion.identity);
            newPortal.name = isRightSide ? "EnemyPortal_Right" : "EnemyPortal_Left";

            // BẮT BUỘC: Đưa Portal vào EnemyContainer để EntitySaveManager quét được và lưu vào JSON
            if (EntitySaveManager.Instance != null && EntitySaveManager.Instance.enemyContainer != null)
            {
                newPortal.transform.SetParent(EntitySaveManager.Instance.enemyContainer.transform, true);
            }

            // Khởi tạo Setup cho portal behavior
            var behavior = newPortal.GetComponent<EnemyPortalBehavior>();
            if (behavior) 
            {
                // Gọi tới hàm setup của behavior với các dependency được tiêm vào
                behavior.Setup(_enemyPool, _enemyDataProvider, _timeProvider);

                // Tính toán và set MaxHP cho portal mới (không phải load từ save)
                // Formula: basePortalMaxHp + (currentDay * hpIncreasePerDay)
                int currentDay = _timeProvider != null ? _timeProvider.GetElapsedDays() : 0;
                // [GUARD] Đảm bảo currentDay không âm (ngày 1 = 0, ngày âm = lỗi time system)
                if (currentDay < 0)
                {
                    Debug.LogWarning($"[EnemyPortalManager] [GUARD] currentDay={currentDay} bị âm! Set về 0");
                    currentDay = 0;
                }
                float calculatedMaxHp = basePortalMaxHp + (currentDay * hpIncreasePerDay);
                
                // [DEBUG] Log chi tiết để kiểm tra giá trị
                Debug.Log($"[EnemyPortalManager] [DEBUG] Spawn {(isRightSide ? "PHẢI" : "TRÁI")}: basePortalMaxHp={basePortalMaxHp}, hpIncreasePerDay={hpIncreasePerDay}, currentDay={currentDay}, calculatedMaxHp={calculatedMaxHp}");
                
                var status = behavior.GetStatus();
                if (status != null)
                {
                    float oldMaxHp = status.MaxHp;
                    float oldCurrentHp = status.CurrentHp;
                    
                    status.MaxHp = calculatedMaxHp;
                    status.CurrentHp = calculatedMaxHp; // Full HP khi spawn
                    // [FIX] Đảm bảo HP được set đầy sau khi spawn
                    status.ResetHealthToFull();
                    
                    Debug.Log($"[EnemyPortalManager] Portal mới {(isRightSide ? "PHẢI" : "TRÁI")}: MaxHP = {calculatedMaxHp} (Base {basePortalMaxHp} + Day {currentDay} * {hpIncreasePerDay}) | Status: oldMax={oldMaxHp}, oldCurrent={oldCurrentHp}, newMax={status.MaxHp}, newCurrent={status.CurrentHp}");
                }
                else
                {
                    Debug.LogError($"[EnemyPortalManager] [CRITICAL] Status là NULL cho portal {(isRightSide ? "PHẢI" : "TRÁI")}! HP sẽ không được set!");
                }
                
                // FIX: Đăng ký ngay lập tức thay vì chờ Start() frame sau
                // Tránh race condition với ProcessFrontlineDelays (6:00)
                RegisterPortal(behavior);
            }

            // Giải cứu player nếu lỡ đứng ở vị trí bị kẹt khi portal vừa xuất hiện
            RescuePlayerIfStuck(spawnPos, isRightSide);

            Debug.Log($"[EnemyPortalManager] Đã tạo thành công cổng {(isRightSide ? "PHẢI" : "TRÁI")} tại: {spawnPos}");
            return true;
        }

        /// <summary>
        /// Được gọi từ EnemyPortalBehavior khi cổng bị phá
        /// </summary>
    }
}

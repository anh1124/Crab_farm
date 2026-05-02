using System.IO;
using UnityEngine;
using Script.Data_Model;
using Script.Game.Other;
using System.Collections.Generic;
using Script.Data_Model.Status;
using Script.Manager;
using Script.Manager.Building;
using Script.Manager.WorldManager;
using Script.Game.Mobs.Subjects;
using Script.Game.Mobs.Denizen.Core;
using Script.Manager.GamePlayManager;

namespace Script.Game.Mobs.MobsManager
{
    [System.Serializable]
    public class VagrantSpawnConfig
    {
        public float spawnDistanceFromWall = 3f;
        public int maxVagrants = 5;
    }

    public class VagrantManager : MonoBehaviour
    {

        [Header("Thiết lập sinh vagrant (đọc từ file JSON)")]
        public VagrantSpawnConfig config;
        
        [Header("Spawn Settings")]
        [Tooltip("Tổng số denizen tối đa để spawn vagrant (nếu > 30 thì không spawn)")]
        public int maxTotalDenizens = 30;
        [Tooltip("Số ngày giữa mỗi lần spawn (3 = mỗi 3 ngày)")]
        public int spawnIntervalDays = 3;
        
        [Header("Runtime State")]
        public bool disableNaturalSpawn = false;
        
        private IGameTimeProvider _timeProvider;
        private BuildingPlacementManager _buildingManager;
        private EntitySaveManager _saveManager;
        private DenizenManager _denizenManager;
        private IGuardProvider _guardProvider;

        [Header("Active Vagrants")]
        public List<GameObject> activeVagrants = new List<GameObject>();

        public void RegisterVagrant(GameObject vagrant)
        {
            if (vagrant == null || activeVagrants.Contains(vagrant)) return;
            activeVagrants.Add(vagrant);
        }

        public void UnregisterVagrant(GameObject vagrant)
        {
            if (vagrant == null) return;
            activeVagrants.Remove(vagrant);
        }

        public void Setup(IGameTimeProvider timeProvider, BuildingPlacementManager buildingManager, EntitySaveManager saveManager, DenizenManager denizenManager, IGuardProvider guardProvider)
        {
            _timeProvider = timeProvider;
            _buildingManager = buildingManager;
            _saveManager = saveManager;
            _denizenManager = denizenManager;
            _guardProvider = guardProvider;

            if (_timeProvider != null)
            {
                // Nếu là cảnh tutorial, tắt tự động spawn vagrant
                if (Script.Utility.IsTutorialScene())
                {
                    disableNaturalSpawn = true;
                    Debug.Log("[VagrantManager] Tutorial scene detected. Natural vagrant spawn disabled.");
                }

                if (!disableNaturalSpawn)
                {
                    _timeProvider.SubscribeToHourlyEvent(6, HandleDailyVagrantSpawn);
                }
            }
        }

        private void Awake()
        {
            LoadConfig();
        }

        private void LoadConfig()
        {
            TextAsset configText = Resources.Load<TextAsset>("DataConfig/Vagrant/VagrantSpawnConfig");
            if (configText != null)
            {
                config = JsonUtility.FromJson<VagrantSpawnConfig>(configText.text);
            }
            else
            {
                config = new VagrantSpawnConfig();
            }
        }

        private void Start()
        {
            // Registration moved to Setup()
        }

        private void OnDestroy()
        {
            if (_timeProvider != null)
            {
                _timeProvider.UnsubscribeFromHourlyEvent(6, HandleDailyVagrantSpawn);
            }
        }

        // Theo dõi ngày spawn tiếp theo
        private int _nextSpawnDay = 0;

        private void HandleDailyVagrantSpawn()
        {
            if (disableNaturalSpawn) return;

            // Kiểm tra tổng số denizen trong hệ thống
            int totalDenizenCount = CountTotalDenizens();
            if (totalDenizenCount > maxTotalDenizens)
            {
                Debug.Log($"[VagrantManager] Không spawn: Tổng denizen ({totalDenizenCount}) > {maxTotalDenizens}");
                return;
            }
            
            var buildingManager = FindObjectOfType<Script.Manager.Building.BuildingManager>();
            if (buildingManager == null || buildingManager.currentTownCenter == null) return;
            
            int townCenterLevel = buildingManager.currentTownCenter.GetComponent<TownCenterStatus>().tier;
            if (townCenterLevel <= 0) return;
            
            // Lấy ngày hiện tại
            int currentDay = _timeProvider != null ? _timeProvider.GetElapsedDays() : 0;
            
            // Khởi tạo ngày spawn đầu tiên nếu chưa có
            if (_nextSpawnDay == 0)
            {
                _nextSpawnDay = currentDay + spawnIntervalDays;
            }
            
            // Kiểm tra đến ngày spawn chưa
            if (currentDay < _nextSpawnDay)
            {
                // Chưa đến ngày spawn
                return;
            }
            
            // Đã đến ngày spawn
            int currentVagrantCount = CountExistingVagrants();
            if (currentVagrantCount >= config.maxVagrants)
            {
                // Vẫn cập nhật ngày spawn tiếp theo dù không spawn được
                _nextSpawnDay = currentDay + spawnIntervalDays;
                return;
            }
            
            // Spawn 1 vagrant mỗi 2 ngày (xóa logic random)
            Debug.Log($"[VagrantManager] Spawn vagrant ngày {currentDay} (kỳ spawn tiếp theo: {_nextSpawnDay})");
            
            // Luân phiên spawn trái/phải dựa trên ngày
            bool spawnOnRight = (currentDay / spawnIntervalDays) % 2 == 0;
            SpawnVagrants(1, spawnOnRight);
            
            // Cập nhật ngày spawn tiếp theo
            _nextSpawnDay = currentDay + spawnIntervalDays;
        }
        
        /// <summary>
        /// Đếm tổng số denizen trong hệ thống (vagrant + villager + archer + ...)
        /// </summary>
        private int CountTotalDenizens()
        {
            if (_denizenManager == null) _denizenManager = FindObjectOfType<DenizenManager>();
            return _denizenManager != null ? _denizenManager.GetTotalDenizens() : 0;
        }

        private int CountExistingVagrants()
        {
            return activeVagrants.Count;
        }

        /// <summary>
        /// Spawn vagrant ban đầu khi khởi tạo game mới tại vị trí x=10
        /// </summary>
        public void SpawnInitialVagrantForNewGame()
        {
            Debug.Log("[VagrantManager] Spawn vagrant ban đầu cho game mới tại x=10");
            // Spawn 1 vagrant tại x=10, z=0 (vị trí cố định bên phải)
            Vector3 initialPosition = new Vector3(10f, 0f, 0f);
            SpawnVagrants(1, true, initialPosition);
        }

        public void SpawnVagrants(int count, bool? isRight = null, Vector3? customPosition = null)
        {
            if (PrefabData.Instance == null || PrefabData.Instance.denizen == null) return;

            if (count <= 0) return;

            for (int i = 0; i < count; i++)
            {
                Vector3 spawnPosition;
                if (customPosition.HasValue) spawnPosition = customPosition.Value;
                else
                {
                    spawnPosition = DetermineSpawnPosition(isRight);
                    if (spawnPosition == Vector3.zero) continue;
                }

                spawnPosition.y = 0;
                
                // Sử dụng Spawner trung tâm
                GameObject vagrantObj = Spawner.Instance.SpawnMob(spawnPosition, PrefabData.Instance.denizen);

                if (vagrantObj != null)
                {
                    // Thiết lập vai trò Vagrant
                    var roleCtrl = vagrantObj.GetComponent<DenizenRoleController>();
                    if (roleCtrl != null)
                    {
                        roleCtrl.ChangeRole(NPCRole.Vagrant);
                    }
                    
                    // Ghi chú: DenizenRoleController sẽ tự động đăng ký với DenizenManager 
                    // và chui vào container thông qua ContainerEntityManager.
                }
            }
        }
        
        private Vector3 DetermineSpawnPosition(bool? isRight = null)
        {
            // Nếu không chỉ định bên, mặc định spawn bên phải
            bool spawnOnRight = isRight ?? true;
            GameObject farthestWall = _saveManager.FindFarthestWall(spawnOnRight);
        
            if (farthestWall == null) return Vector3.zero;
        
            Vector3 wallPosition = farthestWall.transform.position;
            float directionMultiplier = spawnOnRight ? 1 : -1;
        
            // Vị trí cố định cách tường (không random)
            return new Vector3(
                wallPosition.x + (directionMultiplier * config.spawnDistanceFromWall),
                0,
                wallPosition.z
            );
        }
    }
}
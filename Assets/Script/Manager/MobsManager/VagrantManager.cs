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
        public float dailySpawnChance = 0.7f;
        public int minDailySpawnCount = 0;
        public int maxDailySpawnCount = 11;
        [Tooltip("Khoảng random vị trí x khi sinh vagrant quanh tường (theo đơn vị world)")]
        public float randomXRange = 2f;
    }

    public class VagrantManager : MonoBehaviour
    {

        [Header("Thiết lập sinh vagrant (đọc từ file JSON)")]
        public VagrantSpawnConfig config;
        
        private IGameTimeProvider _timeProvider;
        private BuildingPlacementManager _buildingManager;
        private EntitySaveManager _saveManager;
        private DenizenManager _denizenManager;
        private IGuardProvider _guardProvider;
        
        private float currentSpawnChance;

        public void Setup(IGameTimeProvider timeProvider, BuildingPlacementManager buildingManager, EntitySaveManager saveManager, DenizenManager denizenManager, IGuardProvider guardProvider)
        {
            _timeProvider = timeProvider;
            _buildingManager = buildingManager;
            _saveManager = saveManager;
            _denizenManager = denizenManager;
            _guardProvider = guardProvider;

            if (_timeProvider != null)
            {
                _timeProvider.SubscribeToHourlyEvent(6, HandleDailyVagrantSpawn);
            }
        }

        private void Awake()
        {
            LoadConfig();
            currentSpawnChance = config.dailySpawnChance; 
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

        private void HandleDailyVagrantSpawn()
        {
            var buildingManager = FindObjectOfType<Script.Manager.Building.BuildingManager>();
            if (buildingManager == null || buildingManager.currentTownCenter == null) return;
            
            int townCenterLevel = buildingManager.currentTownCenter.GetComponent<TownCenterStatus>().tier;
            if (townCenterLevel <= 0) return;
            
            int currentVagrantCount = CountExistingVagrants();
            if (currentVagrantCount >= config.maxVagrants) return;
            
            if (Random.value > currentSpawnChance)
            {
                IncreaseSpawnChance();
                return;
            }

            int spawnCount;
            int maxAllowedPerDay = Mathf.Min(2, townCenterLevel); 
            int remainingSlots = config.maxVagrants - currentVagrantCount;
            spawnCount = Mathf.Min(maxAllowedPerDay, remainingSlots);
            
            if (Mathf.Approximately(currentSpawnChance, 1.0f) && Random.value < 0.5f && spawnCount < remainingSlots)
            {
                spawnCount += 1;
            }
            
            if (spawnCount <= 0) return;
            
            bool spawnOnRight = Random.value > 0.5f;
            SpawnVagrants(spawnCount, spawnOnRight);
        }
        
        private void IncreaseSpawnChance()
        {
            currentSpawnChance += 0.3f;
            if (currentSpawnChance > 1.0f) currentSpawnChance = 1.0f;
        }
        
        private void ResetSpawnChance()
        {
            currentSpawnChance = config.dailySpawnChance;
        }

        private int CountExistingVagrants()
        {
            if (_saveManager == null || _saveManager.denizenContainer == null) return 0;

            List<GameObject> objects = _saveManager.GetObjectsFromContainerList(_saveManager.denizenContainer);
            
            int vagrantCount = 0;
            foreach (GameObject obj in objects)
            {
                if (obj != null && obj.CompareTag(Script.Utils.GameTags.Vagrant))
                {
                    vagrantCount++;
                }
            }
            return vagrantCount;
        }

        public void SpawnVagrants(int count, bool? isRight = null, Vector3? customPosition = null)
        {
            if (PrefabData.Instance == null || PrefabData.Instance.denizen == null) return;

            if (count <= 0) return;
            int successfulSpawns = 0;

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
                GameObject vagrantObj = _saveManager.InstanceAndMoveObjectToContainer(PrefabData.Instance.denizen, spawnPosition);
                if (vagrantObj != null)
                {
                    // Đảm bảo vagrant mới được khởi tạo context và Managers
                    if (_denizenManager != null)
                    {
                        _denizenManager.InitializeNewDenizen(vagrantObj, _guardProvider);
                    }
                    
                    vagrantObj.tag = "Vagrant";
                    var roleCtrl = vagrantObj.GetComponent<DenizenRoleController>();
                    if (roleCtrl != null)
                    {
                        roleCtrl.ChangeRole(NPCRole.Vagrant);
                    }
                }
            
                if (vagrantObj != null) successfulSpawns++;
            }
            
            if (successfulSpawns > 0) ResetSpawnChance();
        }
        
        private Vector3 DetermineSpawnPosition(bool? isRight = null)
        {
            bool spawnOnRight = isRight ?? (Random.value > 0.5f);
            GameObject farthestWall = _saveManager.FindFarthestWall(spawnOnRight);
        
            if (farthestWall == null) return Vector3.zero;
        
            Vector3 wallPosition = farthestWall.transform.position;
            float directionMultiplier = spawnOnRight ? 1 : -1;
            float randomOffset = Random.Range(-config.randomXRange, config.randomXRange);
        
            return new Vector3(
                wallPosition.x + (directionMultiplier * config.spawnDistanceFromWall) + randomOffset,
                0,
                wallPosition.z
            );
        }
    }
}
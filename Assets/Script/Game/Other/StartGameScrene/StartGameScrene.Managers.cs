using Script.Data_Model;
using Script.Manager;
using Script.Manager.GamePlayManager;
using Script.Manager.WorldManager;
using Script.Game.Structures.Wall_And_Stump;
using Script.Game.Combat;
using Script.Game.Mobs.MobsManager;
using System.Linq;
using UnityEngine;
using Script.Data_Model.Interfaces;

namespace Script.Game.Other
{
    public partial class StartGameScrene
    {
        [Header("World Managers")]
        [SerializeField] private GameTimeManager timeManager;
        [SerializeField] private TechManager techManager;
        [SerializeField] private ChunkManager chunkManager;
        [SerializeField] private CelestialManager celestialManager;
        [SerializeField] private Script.Save_Controller.WorldFileManager worldFileManager;
        [SerializeField] private EntitySaveManager entitySaveManager;
        [SerializeField] private Script.Manager.SaveManager.AutoSaveManager autoSaveManager;

        [Header("Mob Managers")]
        [SerializeField] private DenizenManager denizenManager;
        [SerializeField] private GuardManager guardManager;
        [SerializeField] private VagrantManager vagrantManager;
        [SerializeField] private Script.Manager.Building.BuildingManager buildingManager;
        [SerializeField] private Script.Manager.Building.BuildingPlacementManager buildingPlacementManager;

        [Header("Enemy Managers")]
        [SerializeField] private Script.Game.Enemy.TotalEnemyList totalEnemyList;
        [SerializeField] private Script.Game.Enemy.EnemyPool enemyPool;
        [SerializeField] private EnemyWaveManager waveManager;
        [SerializeField] private EnemyPortalManager portalManager;
        [SerializeField] private Script.Game.Enemy.EnemyTargetManager enemyTargetManager;

        [Header("Other Managers")]
        [SerializeField] private Script.Game.Item.Coin.CoinPool coinPool;
        [SerializeField] private Spawner spawner;
        [SerializeField] private NPCUpdateManager npcUpdateManager;
        [SerializeField] private ArrowPool arrowPool;
        [SerializeField] private WallAndStumpManager wallManager;

        // ==================== PHASED LOADING METHODS ====================

        private void InitializeCoreData()
        {
            if (PrefabData.Instance == null)
            {
                var prefabData = FindObjectOfType<PrefabData>(true);
                if (prefabData == null)
                {
                    throw new System.Exception("[Bootstrap] PrefabData missing. Put a PrefabData in the scene before loading.");
                }
            }
            PrefabData.Instance.Init();

            if (FindObjectOfType<GuardManager>(true) == null)
            {
                Debug.LogWarning(" GuardManager missing in scene! Creating new instance automatically.");
                GameObject gmObj = new GameObject("GuardManager");
                gmObj.AddComponent<GuardManager>();
            }

            if (FindObjectOfType<TechManager>(true) == null)
            {
                Debug.LogWarning(" TechManager missing in scene! Creating new instance automatically.");
                GameObject tmObj = new GameObject("TechManager");
                tmObj.AddComponent<TechManager>();
            }

            var dataLists = FindObjectsOfType<MonoBehaviour>(true)
                .OfType<IDataListBootstrap>()
                .OrderBy(x => x.Order)
                .ToList();

            Debug.Log($" Initializing {dataLists.Count} data managers via bootstrap...");
            foreach (var list in dataLists)
            {
                try
                {
                    list.InitializeData(PrefabData.Instance);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($" Failed to initialize {list.GetType().Name}: {ex.Message}");
                    throw;
                }
            }

            InitializeArrowPool();
            InitializeWorldBase();
            InitializeBuildingBase();
            InitializeMobsBase();
        }

        private void InitializeBuildingBase()
        {
            if (buildingManager == null) buildingManager = FindObjectOfType<Script.Manager.Building.BuildingManager>(true);
            if (buildingPlacementManager == null) buildingPlacementManager = FindObjectOfType<Script.Manager.Building.BuildingPlacementManager>(true);
            if (entitySaveManager == null) entitySaveManager = FindObjectOfType<EntitySaveManager>(true);

            if (buildingManager == null || buildingPlacementManager == null)
            {
                Debug.LogError("[StartGameScrene] One or more Building Managers are missing in the scene!");
                return;
            }

            buildingPlacementManager.Setup(buildingManager, entitySaveManager);
            // buildingManager.UpdateAvailableBuildings() moved to after Save Loading phase
            Debug.Log(" Building Systems (DI) initialized and wired.");
        }

        private void InitializeMobsBase()
        {
            if (denizenManager == null) denizenManager = FindObjectOfType<DenizenManager>(true);
            if (guardManager == null) guardManager = FindObjectOfType<GuardManager>(true);
            if (vagrantManager == null) vagrantManager = FindObjectOfType<VagrantManager>(true);
            if (timeManager == null) timeManager = FindObjectOfType<GameTimeManager>(true);
            if (entitySaveManager == null) entitySaveManager = FindObjectOfType<EntitySaveManager>(true);
            if (buildingManager == null) buildingManager = FindObjectOfType<Script.Manager.Building.BuildingManager>(true);
            if (buildingPlacementManager == null) buildingPlacementManager = FindObjectOfType<Script.Manager.Building.BuildingPlacementManager>(true);

            if (denizenManager == null || guardManager == null || vagrantManager == null)
            {
                Debug.LogError("[StartGameScrene] One or more Mob Managers are missing in the scene!");
                return;
            }

            denizenManager.Setup(timeManager, buildingPlacementManager, buildingManager, entitySaveManager);
            if (guardManager == null) guardManager = FindObjectOfType<GuardManager>(true);
            guardManager.Setup(buildingPlacementManager, entitySaveManager);

            if (vagrantManager == null) vagrantManager = FindObjectOfType<VagrantManager>(true);
            if (vagrantManager != null) 
                vagrantManager.Setup(timeManager, buildingPlacementManager, entitySaveManager, denizenManager, guardManager);

            if (wallManager == null) wallManager = FindObjectOfType<WallAndStumpManager>(true);
            if (npcUpdateManager == null) npcUpdateManager = FindObjectOfType<NPCUpdateManager>(true);
            if (arrowPool == null) arrowPool = FindObjectOfType<ArrowPool>(true);

            if (denizenManager != null)
            {
                denizenManager.SetExtraProviders(wallManager, npcUpdateManager, arrowPool);
                denizenManager.InitializeAllDenizensInScene(guardManager);
            }

            guardManager.InitializeData(PrefabData.Instance);
            Debug.Log(" Mobs Systems (DI) initialized and wired.");
        }

        private void InitializeWorldBase()
        {
            if (timeManager == null) timeManager = FindObjectOfType<GameTimeManager>(true);
            if (techManager == null) techManager = FindObjectOfType<TechManager>(true);
            if (chunkManager == null) chunkManager = FindObjectOfType<ChunkManager>(true);
            if (celestialManager == null) celestialManager = FindObjectOfType<CelestialManager>(true);
            if (worldFileManager == null) worldFileManager = FindObjectOfType<Script.Save_Controller.WorldFileManager>(true);
            if (entitySaveManager == null) entitySaveManager = FindObjectOfType<EntitySaveManager>(true);
            if (autoSaveManager == null) autoSaveManager = FindObjectOfType<Script.Manager.SaveManager.AutoSaveManager>(true);

            if (timeManager == null) Debug.LogError("[StartGameScrene] GameTimeManager missing!");
            if (techManager == null) techManager = FindObjectOfType<TechManager>(true);
            if (techManager == null) Debug.LogError("[StartGameScrene] TechManager missing!");
            if (chunkManager == null) Debug.LogError("[StartGameScrene] ChunkManager missing!");
            if (worldFileManager == null) Debug.LogError("[StartGameScrene] WorldFileManager missing!");

            if (timeManager == null || techManager == null || chunkManager == null || worldFileManager == null) return;

            techManager.Setup(worldFileManager);
            chunkManager.Setup(entitySaveManager);
            if (celestialManager != null) celestialManager.Setup(timeManager);
            if (autoSaveManager != null) autoSaveManager.Setup(timeManager);

            timeManager.InitializeTimeSystem();
            worldFileManager.SyncTechToPrefabData();
            Debug.Log(" World Systems (DI) initialized and wired.");
        }

        private void InitializeCoinBase()
        {
            if (entitySaveManager == null) entitySaveManager = FindObjectOfType<EntitySaveManager>(true);
            if (coinPool == null) coinPool = FindObjectOfType<Script.Game.Item.Coin.CoinPool>(true);
            
            // Auto-Add if missing on container
            if (coinPool == null && entitySaveManager != null && entitySaveManager.coinContainer != null)
            {
                coinPool = entitySaveManager.coinContainer.GetComponent<Script.Game.Item.Coin.CoinPool>();
                if (coinPool == null)
                {
                    coinPool = entitySaveManager.coinContainer.AddComponent<Script.Game.Item.Coin.CoinPool>();
                     Debug.LogWarning(" CoinPool missing! Adding component to CoinContainer.");
                }
            }

            if (spawner == null) spawner = FindObjectOfType<Spawner>(true);
            if (spawner == null && entitySaveManager != null && entitySaveManager.otherContainer != null)
            {
                spawner = entitySaveManager.otherContainer.GetComponent<Spawner>();
                if (spawner == null)
                {
                    spawner = entitySaveManager.otherContainer.AddComponent<Spawner>();
                    Debug.LogWarning(" Spawner missing! Adding component to OtherContainer.");
                }
            }

            if (coinPool == null || spawner == null)
            {
                Debug.LogError("[StartGameScrene] CoinPool or Spawner missing and could not be auto-created!");
                return;
            }

            coinPool.Setup(PrefabData.Instance);
            coinPool.Init(); // Important: Pool must be initialized
            spawner.Setup(coinPool, enemyPool);

            var lootPickups = FindObjectsOfType<Script.Utils.LootPickupTrigger>(true);
            foreach (var lp in lootPickups) lp.Setup(coinPool);

            var coinVisuals = FindObjectsOfType<Script.Game.Item.Coin.CoinSlotVisual>(true);
            foreach (var csv in coinVisuals) csv.SetupPool(coinPool);

            Debug.Log(" Coin Systems (DI) initialized and wired.");
        }

        private void InitializeEnemyBase()
        {
            if (entitySaveManager == null) entitySaveManager = FindObjectOfType<EntitySaveManager>(true);
            if (totalEnemyList == null) totalEnemyList = FindObjectOfType<Script.Game.Enemy.TotalEnemyList>(true);
            if (enemyPool == null) enemyPool = FindObjectOfType<Script.Game.Enemy.EnemyPool>(true);

            // Auto-Add if missing on container
            if (enemyPool == null && entitySaveManager != null && entitySaveManager.enemyContainer != null)
            {
                enemyPool = entitySaveManager.enemyContainer.GetComponent<Script.Game.Enemy.EnemyPool>();
                if (enemyPool == null)
                {
                    enemyPool = entitySaveManager.enemyContainer.AddComponent<Script.Game.Enemy.EnemyPool>();
                    Debug.LogWarning(" EnemyPool missing! Adding component to EnemyContainer.");
                }
            }

            if (waveManager == null) waveManager = FindObjectOfType<EnemyWaveManager>(true);
            if (portalManager == null) portalManager = FindObjectOfType<EnemyPortalManager>(true);

            if (enemyTargetManager == null) enemyTargetManager = FindObjectOfType<Script.Game.Enemy.EnemyTargetManager>(true);
            if (enemyTargetManager == null && entitySaveManager != null && entitySaveManager.enemyContainer != null)
            {
                enemyTargetManager = entitySaveManager.enemyContainer.GetComponent<Script.Game.Enemy.EnemyTargetManager>();
                if (enemyTargetManager == null)
                {
                    enemyTargetManager = entitySaveManager.enemyContainer.AddComponent<Script.Game.Enemy.EnemyTargetManager>();
                    Debug.LogWarning(" EnemyTargetManager missing! Adding component to EnemyContainer.");
                }
            }

            if (totalEnemyList == null || enemyPool == null || waveManager == null || portalManager == null || enemyTargetManager == null)
            {
                Debug.LogError("[StartGameScrene] One or more Enemy Managers are missing and could not be auto-created!");
                return;
            }

            enemyPool.Setup(totalEnemyList, PrefabData.Instance, enemyTargetManager);

            if (timeManager == null) timeManager = FindObjectOfType<GameTimeManager>(true);
            waveManager.Setup(totalEnemyList, enemyPool, timeManager);
            portalManager.Setup(enemyPool, totalEnemyList, timeManager);

            totalEnemyList.InitializeEnemyData();
            enemyPool.InitializePools();

            if (spawner == null) spawner = FindObjectOfType<Spawner>(true);
            if (spawner != null)
            {
                // Đồng bộ Spawner để luồng load-from-save luôn đi qua pool enemy.
                spawner.Setup(coinPool, enemyPool);
            }

            Debug.Log(" Enemy Systems (DI) initialized and wired.");
        }

        private void InitializeArrowPool()
        {
            if (PrefabData.Instance == null || PrefabData.Instance.arrow == null) return;

            if (arrowPool == null) arrowPool = FindObjectOfType<ArrowPool>(true);
            if (arrowPool == null)
            {
                GameObject poolObj = new GameObject("ArrowPool");
                arrowPool = poolObj.AddComponent<ArrowPool>();
            }
            arrowPool.Initialize(PrefabData.Instance.arrow);
        }
    }
}

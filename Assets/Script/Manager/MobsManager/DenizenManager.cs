using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Subjects; 
using Script.Game.Mobs.Denizen.Core;
using Script.Game.Mobs.Denizen.Jobs.Villager;
using Script.Data_Model.Status;
using Script.Data_Model.Interfaces;
using Script.Manager;
using Script.Manager.Building;
using Script.Manager.GamePlayManager;
using Script.Manager.WorldManager;
using Script.Game.Combat;
using Script.Game.Structures.Wall_And_Stump;

namespace Script.Game.Mobs.MobsManager
{
    public class DenizenManager : MonoBehaviour, IDenizenProvider
    {

        [SerializeField]
        private List<GameObject> allDenizens = new List<GameObject>();

        [Header("Animator Controllers")]
        [SerializeField] public RuntimeAnimatorController vagrantAnimatorController;
        [SerializeField] public RuntimeAnimatorController villagerAnimatorController;
        [SerializeField] public RuntimeAnimatorController archerAnimatorController;
        [SerializeField] public RuntimeAnimatorController workerAnimatorController;
        [SerializeField] public RuntimeAnimatorController farmerAnimatorController;

        public RuntimeAnimatorController VagrantAnimatorController => vagrantAnimatorController;
        public RuntimeAnimatorController VillagerAnimatorController => villagerAnimatorController;
        public RuntimeAnimatorController ArcherAnimatorController => archerAnimatorController;
        public RuntimeAnimatorController WorkerAnimatorController => workerAnimatorController;
        public RuntimeAnimatorController FarmerAnimatorController => farmerAnimatorController;

        RuntimeAnimatorController IDenizenProvider.vagrantAnimatorController => vagrantAnimatorController;
        RuntimeAnimatorController IDenizenProvider.villagerAnimatorController => villagerAnimatorController;
        RuntimeAnimatorController IDenizenProvider.archerAnimatorController => archerAnimatorController;
        RuntimeAnimatorController IDenizenProvider.workerAnimatorController => workerAnimatorController;
        RuntimeAnimatorController IDenizenProvider.farmerAnimatorController => farmerAnimatorController;

        private void Awake()
        {
            // Singleton removed
        }

        private void Start()
        {
            if (vagrantAnimatorController == null) Debug.LogError($"[DenizenManager] Vagrant Animator Controller is missing!");
            if (villagerAnimatorController == null) Debug.LogError($"[DenizenManager] Villager Animator Controller is missing!");
            if (archerAnimatorController == null) Debug.LogError($"[DenizenManager] Archer Animator Controller is missing!");
        }
        
        private IGameTimeProvider _timeProvider;
        private BuildingPlacementManager _placementManager;
        private BuildingManager _buildingManager;
        private EntitySaveManager _saveManager;
        private WallAndStumpManager _wallManager;
        private NPCUpdateManager _npcUpdateManager;
        private ArrowPool _arrowPool;
        private VagrantManager _vagrantManager;
        private GuardManager _guardManager;

        public void Setup(IGameTimeProvider timeProvider, BuildingPlacementManager placementManager, BuildingManager buildingManager, EntitySaveManager saveManager)
        {
            _timeProvider = timeProvider;
            _placementManager = placementManager;
            _buildingManager = buildingManager;
            _saveManager = saveManager;
            Debug.Log("[DenizenManager] Essential dependencies wired via Setup.");
        }

        public void SetExtraProviders(WallAndStumpManager wallManager, NPCUpdateManager npcUpdateManager, ArrowPool arrowPool)
        {
            _wallManager = wallManager;
            _npcUpdateManager = npcUpdateManager;
            _arrowPool = arrowPool;
            Debug.Log("[DenizenManager] Extra providers wired via SetExtraProviders.");
        }

        /// <summary>
        /// Called by StartGameScrene to boot all denizens currently in scene (e.g. loaded from save)
        /// </summary>
        public void InitializeAllDenizensInScene(IGuardProvider guardProvider)
        {
            var controllers = FindObjectsOfType<DenizenRoleController>(true);
            foreach (var ctrl in controllers)
            {
                // Fallback providers if not wired yet
                if (_timeProvider == null) _timeProvider = FindObjectOfType<GameTimeManager>(true);
                if (_wallManager == null) _wallManager = FindObjectOfType<WallAndStumpManager>(true);
                if (_npcUpdateManager == null) _npcUpdateManager = FindObjectOfType<NPCUpdateManager>(true);
                if (_arrowPool == null) _arrowPool = FindObjectOfType<ArrowPool>(true);

                ctrl.Setup(this, guardProvider, _timeProvider, _wallManager, _npcUpdateManager, _arrowPool, _saveManager);
                RegisterDenizen(ctrl.gameObject);
            }
            Debug.Log($"[DenizenManager] Bootstrapped {controllers.Length} denizens in scene.");
        }

        public void InitializeNewDenizen(GameObject denizen, IGuardProvider guardProvider)
        {
            if (denizen == null) return;
            var ctrl = denizen.GetComponent<DenizenRoleController>();
            if (ctrl != null)
            {
                // Fallback providers if not wired yet
                if (_timeProvider == null) _timeProvider = FindObjectOfType<GameTimeManager>(true);
                if (_wallManager == null) _wallManager = FindObjectOfType<WallAndStumpManager>(true);
                if (_npcUpdateManager == null) _npcUpdateManager = FindObjectOfType<NPCUpdateManager>(true);
                if (_arrowPool == null) _arrowPool = FindObjectOfType<ArrowPool>(true);

                ctrl.Setup(this, guardProvider, _timeProvider, _wallManager, _npcUpdateManager, _arrowPool, _saveManager);
                RegisterDenizen(denizen);
            }
        }

        public void RegisterDenizen(GameObject denizen)
        {
            if (denizen == null || allDenizens.Contains(denizen)) return;
            allDenizens.Add(denizen);
        }

        public void UnregisterDenizen(GameObject denizen)
        {
            if (denizen == null) return;
            allDenizens.Remove(denizen);
        }

        public int GetTotalDenizens() => allDenizens.Count;

        public List<GameObject> GetDenizensByRole(NPCRole role)
        {
            var filtered = new List<GameObject>();
            foreach (var denizen in allDenizens)
            {
                if (denizen == null) continue;
                var roleCtrl = denizen.GetComponent<DenizenRoleController>();
                if (roleCtrl != null && roleCtrl.CurrentRole == role)
                {
                    filtered.Add(denizen);
                }
            }
            return filtered;
        }

        public VillagerJob GetNearestIdleVillager(Vector3 position)
        {
            VillagerJob nearestVillager = null;
            float minDistance = float.MaxValue;

            foreach (var denizen in allDenizens)
            {
                if (denizen == null) continue;

                var roleCtrl = denizen.GetComponent<DenizenRoleController>();
                if (roleCtrl != null && roleCtrl.CurrentRole == NPCRole.Villager)
                {
                    var job = denizen.GetComponent<VillagerJob>();
                    if (job != null && job.enabled)
                    {
                         var status = denizen.GetComponent<DenizenStatus>();
                         bool hasNoTool = (status != null && status.currentTool == null);
                         bool notInDanger = (status != null && !status.isInDanger);
                         
                         bool isAvailable = status != null && (job.CurrentState is VillagerIdleState || 
                                            job.CurrentState is VillagerToTownState);

                         if (hasNoTool && notInDanger && isAvailable)
                         {
                            float dist = Vector3.Distance(position, denizen.transform.position);
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                nearestVillager = job;
                            }
                         }
                    }
                }
            }
            return nearestVillager;
        }

        public int GetIdleVillagerCount()
        {
            int count = 0;
            foreach (var denizen in allDenizens)
            {
                if (denizen == null) continue;
                var roleCtrl = denizen.GetComponent<DenizenRoleController>();
                if (roleCtrl != null && roleCtrl.CurrentRole == NPCRole.Villager)
                {
                    var job = denizen.GetComponent<VillagerJob>();
                    if (job != null && job.enabled)
                    {
                         var status = denizen.GetComponent<DenizenStatus>();
                         if (status == null) continue;
                         
                         bool hasNoTool = status.currentTool == null;
                         bool notInDanger = !status.isInDanger;
                         bool isAvailable = job.CurrentState is VillagerIdleState || job.CurrentState is VillagerToTownState;

                         if (hasNoTool && notInDanger && isAvailable) count++;
                    }
                }
            }
            return count;
        }
    }
}

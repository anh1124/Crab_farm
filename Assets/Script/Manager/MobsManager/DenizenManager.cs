using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Subjects; // DenizenRoleController, NPCRole

namespace Script.Game.Mobs.MobsManager
{
    public class DenizenManager : MonoBehaviour
    {
        public static DenizenManager Instance { get; private set; }

        //script này lưu danh sách các cư dân (trừ kẻ lang thang )
        //lưu animcontroller
         [SerializeField]
        private List<GameObject> allDenizens = new List<GameObject>();

        [Header("Animator Controllers")]
        [SerializeField] public RuntimeAnimatorController vagrantAnimatorController;
        [SerializeField] public RuntimeAnimatorController villagerAnimatorController;
        [SerializeField] public RuntimeAnimatorController archerAnimatorController;
        [SerializeField] public RuntimeAnimatorController workerAnimatorController;
        [SerializeField] public RuntimeAnimatorController farmerAnimatorController;


        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
             // Validate Animator Controllers
            if (vagrantAnimatorController == null) Debug.LogError($"[DenizenManager] Vagrant Animator Controller is missing!");
            if (villagerAnimatorController == null) Debug.LogError($"[DenizenManager] Villager Animator Controller is missing!");
            if (archerAnimatorController == null) Debug.LogError($"[DenizenManager] Archer Animator Controller is missing!");
            // Worker and Farmer checks optional
        }

        // API chung để đăng ký 1 Denizen
        public void RegisterDenizen(GameObject denizen)
        {
            if (denizen == null || allDenizens.Contains(denizen)) return;

            allDenizens.Add(denizen);
            
            // Debug.Log($"[DenizenManager] Registered {denizen.name}");
        }

        // API chung để hủy đăng ký 1 Denizen
        public void UnregisterDenizen(GameObject denizen)
        {
            if (denizen == null) return;
            allDenizens.Remove(denizen);

            // Debug.Log($"[DenizenManager] Unregistered {denizen.name}");
        }

        // Helpers
        public int GetTotalDenizens() => allDenizens.Count;

        public List<GameObject> GetDenizensByRole(NPCRole role)
        {
            var filtered = new List<GameObject>();
            foreach (var denizen in allDenizens)
            {
                if (denizen == null) continue;
                var controller = denizen.GetComponent<DenizenRoleController>();
                if (controller != null && controller.CurrentRole == role)
                {
                    filtered.Add(denizen);
                }
            }
            return filtered;
        }
        /// <summary>
        /// Find the nearest Villager that can be assigned a tool.
        /// Accepts Villagers in Idle OR MoveBackToTownCenter states (e.g., spawned at night).
        /// </summary>
        public Script.Game.Mobs.Subjects.VillagerBehavior GetNearestIdleVillager(Vector3 position)
        {
            Script.Game.Mobs.Subjects.VillagerBehavior nearestVillager = null;
            float minDistance = float.MaxValue;
            int checkedCount = 0;

            foreach (var denizen in allDenizens)
            {
                if (denizen == null) continue;
                checkedCount++;

                var controller = denizen.GetComponent<DenizenRoleController>();
                if (controller != null && controller.CurrentRole == NPCRole.Villager)
                {
                    var behavior = denizen.GetComponent<Script.Game.Mobs.Subjects.VillagerBehavior>();
                    if (behavior != null && behavior.enabled)
                    {
                         // Check conditions
                         bool hasNoTool = behavior.currentTool == null;
                         bool notInDanger = !behavior.isInDanger;
                         var status = denizen.GetComponent<Script.Data_Model.Status.DenizenStatus>();
                         
                         // Accept both Idle and MoveBackToTownCenter states
                         // This allows tool assignment even if Villager spawned at night
                         bool isAvailable = status.villagerState == Script.Data_Model.Status.DenizenStatus.VillagerState.Idle ||
                                           status.villagerState == Script.Data_Model.Status.DenizenStatus.VillagerState.MoveBackToTownCenter;

                         if (hasNoTool && notInDanger && isAvailable)
                         {
                            float dist = Vector3.Distance(position, denizen.transform.position);
                            if (dist < minDistance)
                            {
                                minDistance = dist;
                                nearestVillager = behavior;
                            }
                         }
                         else
                         {
                             // Debug.Log($"[DenizenManager] Skip {denizen.name}: NoTool={hasNoTool}, Danger={!notInDanger}, Available={isAvailable} (State: {status.villagerState})");
                         }
                    }
                }
            }
            
            // if (nearestVillager != null) Debug.Log($"[DenizenManager] Found idle villager for tool: {nearestVillager.name}");
            // else Debug.Log($"[DenizenManager] No idle villager found among {checkedCount} denizens.");
            
            return nearestVillager;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Subjects; 
using Script.Game.Mobs.Denizen.Core;
using Script.Game.Mobs.Denizen.Jobs.Villager;
using Script.Data_Model.Status;

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
                var core = denizen.GetComponent<DenizenCore>();
                if (core != null && core.CurrentRole == role)
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
        public VillagerJob GetNearestIdleVillager(Vector3 position)
        {
            VillagerJob nearestVillager = null;
            float minDistance = float.MaxValue;

            foreach (var denizen in allDenizens)
            {
                if (denizen == null) continue;

                var core = denizen.GetComponent<DenizenCore>();
                if (core != null && core.CurrentRole == NPCRole.Villager)
                {
                    var job = denizen.GetComponent<VillagerJob>();
                    if (job != null && job.enabled)
                    {
                         var status = denizen.GetComponent<DenizenStatus>();
                         bool hasNoTool = (status != null && status.currentTool == null);
                         bool notInDanger = (status != null && !status.isInDanger);
                         
                         // Kiểm tra Availability dựa trên State pattern mới
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
    }
}

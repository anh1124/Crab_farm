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
            if (vagrantAnimatorController == null) Debug.LogError($"[DenizenManager] Vagrant Animator Controller is missing!");
            if (villagerAnimatorController == null) Debug.LogError($"[DenizenManager] Villager Animator Controller is missing!");
            if (archerAnimatorController == null) Debug.LogError($"[DenizenManager] Archer Animator Controller is missing!");
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

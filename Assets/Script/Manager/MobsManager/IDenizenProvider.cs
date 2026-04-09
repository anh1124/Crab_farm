using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model.Status;
using Script.Game.Mobs.Subjects;

namespace Script.Game.Mobs.MobsManager
{
    public interface IDenizenProvider
    {
        void RegisterDenizen(GameObject denizen);
        void UnregisterDenizen(GameObject denizen);
        int GetTotalDenizens();
        List<GameObject> GetDenizensByRole(NPCRole role);
        int GetIdleVillagerCount();
        
        // Trả về Job cụ thể cho các tác vụ đặc thù
        Script.Game.Mobs.Denizen.Jobs.Villager.VillagerJob GetNearestIdleVillager(Vector3 position);
        
        RuntimeAnimatorController vagrantAnimatorController { get; }
        RuntimeAnimatorController villagerAnimatorController { get; }
        RuntimeAnimatorController archerAnimatorController { get; }
        RuntimeAnimatorController workerAnimatorController { get; }
        RuntimeAnimatorController farmerAnimatorController { get; }
    }
}

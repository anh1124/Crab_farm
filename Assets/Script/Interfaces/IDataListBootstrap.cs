using Script.Data_Model;

namespace Script.Data_Model.Interfaces
{
    public interface IDataListBootstrap
    {
        int Order { get; }
        void InitializeData(PrefabData context);
    }
    /*
     * REGISTERED IMPLEMENTATIONS & ORDER:
     * 1. WallAndStumpManager (Script.Game.Structures.Wall_And_Stump) - Order: 0
     *    - Base structure management. Required by GuardManager.
     * 
     * 2. GuardManager (Script.Game.Mobs.MobsManager) - Order: 1
     *    - Initializes Singleton & Wall references. High priority for defense system.
     * 
     * 3. TownCenterDataList (Script.Game.Structures.TownCenter) - Order: 110
     *    - Initializes TownCenter tier data list. Lower priority.
     *    
     * EXCLUDED FROM BOOTSTRAP (REASON):
     * - GameTimeManager: Uses Coroutine (requires Start), Bootstrap runs in Phase 1 (Awake-like context).
     * - WallDataList: ScriptableObject (pure data), needs no runtime initialization.
     * - BuildingManager/PlacementManager: Runtime logic or UI dependent, not core data initialization.
     * - Others (EnemyWave, Denizen, etc): Runtime management logic, handled via standard MonoBehaviour flow.
     */
}


using System.Collections.Generic;
using UnityEngine;

namespace Script.Game.Mobs.MobsManager
{
    public interface IGuardProvider
    {
        bool isEnemyAttack { get; set; }
        List<GameObject> leftFlankArchers { get; }
        List<GameObject> rightFlankArchers { get; }
        List<GameObject> detectedEnemies { get; }
        GameObject currentLeftWall { get; }
        GameObject currentRightWall { get; }
        GuardAlarmSystem Alarm { get; }
        GuardSquadSystem Squad { get; }
        GuardWallSystem Wall { get; }
        
        float defaultSlotOffset { get; }
        
        void RegisterArcher(GameObject archer);
        void UnregisterArcher(GameObject archer);
        Vector3 GetDefensePositionForArcher(GameObject archer);
        void RebalanceSquads();
        
        void OnWallListUpdated();
        GameObject GetCurrentDefensiveWall(bool isLeft);
        void UpdateDefensiveWalls();
        
        void RegisterEnemy(GameObject enemy);
        void ClearAlarm();
        
        Vector3? GetHuntingRangeStartPosition(bool isLeft);
        float archerHuntingRange { get; }
    }
}

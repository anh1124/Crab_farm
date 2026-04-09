using UnityEngine;
using Script.Utils;

namespace Script
{
    public static class Utility
    {
        public static bool isDebug = false;

        static Utility()
        {
            // DebugUtility.IsDebugEnabled = isDebug; // Removed to prevent overwriting with false
        }

        public static bool IsAlly(GameObject obj)
        {
            return CombatUtility.IsAlly(obj);
        }

        public static void ProcessAttackHits(GameObject instigator, Collider2D[] hits, int count, float damageAmount)
        {
            CombatUtility.ProcessAttackHits(instigator, hits, count, damageAmount);
        }

        public static void LogCollidedObjects(string colliderName, Collider2D[] hits, int count)
        {
            DebugUtility.LogCollidedObjects(colliderName, hits, count);
        }

        public static void RotateObjectTowardsTownCenter(GameObject obj, Vector3 referencePos, Vector3 townCenterPos)
        {
            WorldUtility.RotateObjectTowardsTownCenter(obj, referencePos, townCenterPos);
        }

        public static float GetRandomPlazaPos()
        {
            return WorldUtility.GetRandomPlazaPos();
        }

        public static float GetRandomKingdomSafePos()
        {
            return WorldUtility.GetRandomKingdomSafePos();
        }

        public static float GetRandomHuntingPos()
        {
            return WorldUtility.GetRandomHuntingPos();
        }

        public static Vector3 GetRandomOutsideSafePos(bool isRight, float minDistance, float maxDistance, float noWallFallbackDistance = 30f)
        {
            return WorldUtility.GetRandomOutsideSafePos(isRight, minDistance, maxDistance, noWallFallbackDistance);
        }

        public static Vector2 GetWallPosition(bool getLeftWall)
        {
            return WorldUtility.GetWallPosition(getLeftWall);
        }

        public static bool MoveTowardsTarget(GameObject objectToMove, Vector3 target, float speed)
        {
            return MovementUtility.MoveTowardsTarget(objectToMove, target, speed);
        }

        public static void FlipDirection(GameObject objectToFlip, Vector3 target)
        {
            MovementUtility.FlipDirection(objectToFlip, target);
        }

        public static void TransformToVillager(GameObject archerObject, int currentCoins)
        {
            NpcUtility.TransformToVillager(archerObject, currentCoins);
        }

        public static GameObject GetTownCenter()
        {
            return WorldUtility.GetTownCenter();
        }

        public static int GetTownCenterTier()
        {
            return WorldUtility.GetTownCenterTier();
        }

        public static void ProcessCoinPickup(GameObject coinObj, System.Func<bool> condition, System.Action onPickupSuccess)
        {
            if (coinObj == null) return;

            // Check condition if provided
            if (condition != null && !condition.Invoke()) return;

            // Destroy coin (Deactivate first to let LootPickupTrigger know it was successfully consumed)
            coinObj.SetActive(false);
            UnityEngine.Object.Destroy(coinObj);

            // Success callback
            onPickupSuccess?.Invoke();
        }

        public static bool IsInKingdomSafeZone(float posX)
        {
            return WorldUtility.IsInKingdomSafeZone(posX);
        }
    }
}

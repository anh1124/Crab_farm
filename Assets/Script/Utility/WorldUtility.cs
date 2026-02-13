using UnityEngine;
using Script.Game.Other;
using Script.Manager.Building;

namespace Script.Utils
{
    public static class WorldUtility
    {
        public static void RotateObjectTowardsTownCenter(GameObject obj, Vector3 referencePos, Vector3 townCenterPos)
        {
            Vector3 scale = obj.transform.localScale;

            Vector3 directionFromTownCenter = referencePos - townCenterPos;

            if (directionFromTownCenter.x < 0)
            {
                scale.x = -1f * Mathf.Abs(scale.x);
            }
            else
            {
                scale.x = Mathf.Abs(scale.x);
            }

            if (Utility.isDebug)
            {
                Debug.Log($"Utility.RotateObjectTowardsTownCenter: Object {obj.name} at pos {referencePos}, town center at {townCenterPos}, direction {directionFromTownCenter}, scale.x={scale.x}");
            }
            
            obj.transform.localScale = scale;
            obj.transform.rotation = Quaternion.identity;
        }

        public static float GetRandomPlazaPos()
        {
            if (BuildingManager.instance == null || BuildingManager.instance.currentTownCenter == null)
            {
                Debug.LogWarning("[WorldUtility] TownCenter not found! Returning random position near origin.");
                // Fallback: Random position trong phạm vi 10f từ (0,0)
                return Random.Range(-10f, 10f);
            }

            var townCenter = BuildingManager.instance.currentTownCenter;
            float townCenterX = townCenter.transform.position.x;
            
            // Get plaza distance from TownCenterBehavior
            var tcBehavior = townCenter.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
            float radius = 10f; // default fallback
            if (tcBehavior != null)
            {
                radius = tcBehavior.plazaDistance;
            }
            else
            {
                 // Try getting from PrefabData directly if behavior missing (unlikely)
                 if (Script.Data_Model.PrefabData.Instance != null) radius = Script.Data_Model.PrefabData.Instance.plazaDistance;
            }

            return Random.Range(townCenterX - radius, townCenterX + radius);
        }

        /// <summary>
        /// Trả về vị trí random trong phạm vi radius từ điểm center (chỉ trục X)
        /// </summary>
        public static Vector3 GetRandomPositionNearby(Vector3 center, float radius)
        {
            float randomX = Random.Range(center.x - radius, center.x + radius);
            return new Vector3(randomX, center.y, center.z);
        }

        public static float GetRandomKingdomSafePos()
        {
            if (BuildingManager.instance == null || BuildingManager.instance.currentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found!");
                return 0f;
            }

            float townCenterX = BuildingManager.instance.currentTownCenter.transform.position.x;

            GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
            float farthestLeftWall = townCenterX;
            float farthestRightWall = townCenterX;

            foreach (GameObject wall in walls)
            {
                float wallX = wall.transform.position.x;
                if (wallX < farthestLeftWall)
                {
                    farthestLeftWall = wallX;
                }
                if (wallX > farthestRightWall)
                {
                    farthestRightWall = wallX;
                }
            }

            if (farthestLeftWall == townCenterX) farthestLeftWall = townCenterX - 15f;
            if (farthestRightWall == townCenterX) farthestRightWall = townCenterX + 15f;

            return Random.Range(farthestLeftWall, farthestRightWall);
        }

        public static Vector2 GetWallPosition(bool getLeftWall)
        {
            if (BuildingManager.instance == null || BuildingManager.instance.currentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found!");
                return Vector2.zero;
            }

            float townCenterX = BuildingManager.instance.currentTownCenter.transform.position.x;
            float townCenterY = BuildingManager.instance.currentTownCenter.transform.position.y;

            GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
            float wallX = getLeftWall ? float.MinValue : float.MaxValue;
            float defaultOffset = getLeftWall ? -15f : 15f;

            foreach (GameObject wall in walls)
            {
                float currentWallX = wall.transform.position.x;
                if (getLeftWall)
                {
                    if (currentWallX < townCenterX && currentWallX > wallX)
                    {
                        wallX = currentWallX;
                    }
                }
                else
                {
                    if (currentWallX > townCenterX && currentWallX < wallX)
                    {
                        wallX = currentWallX;
                    }
                }
            }

            if ((getLeftWall && wallX == float.MinValue) || (!getLeftWall && wallX == float.MaxValue))
            {
                wallX = townCenterX + defaultOffset;
            }

            return new Vector2(wallX, townCenterY);
        }

        public static GameObject GetTownCenter()
        {
            if (BuildingManager.instance == null || BuildingManager.instance.currentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found!");
                return null;
            }
            return BuildingManager.instance.currentTownCenter.gameObject;
        }
    }
}



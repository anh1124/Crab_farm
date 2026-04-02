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

        public static Vector3 GetRandomOutsideSafePos(bool isRight, float minDistance, float maxDistance, float noWallFallbackDistance = 30f)
        {
            if (BuildingManager.instance == null || BuildingManager.instance.currentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found! Returning zero. Cannot determine outside safe zone.");
                return Vector3.zero;
            }

            Vector3 townCenterPos = BuildingManager.instance.currentTownCenter.transform.position;
            float townCenterX = townCenterPos.x;
            float townCenterY = townCenterPos.y; // Assuming ground level is Y of TownCenter
            float townCenterZ = townCenterPos.z;

            GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
            
            // Tìm bức tường xa nhất theo hướng được chọn
            float farthestWallX = townCenterX;
            bool wallFound = false;

            foreach (GameObject wall in walls)
            {
                float wallX = wall.transform.position.x;
                if (isRight)
                {
                    if (wallX > townCenterX && wallX > farthestWallX)
                    {
                        farthestWallX = wallX;
                        wallFound = true;
                    }
                }
                else
                {
                    if (wallX < townCenterX && wallX < farthestWallX)
                    {
                        farthestWallX = wallX;
                        wallFound = true;
                    }
                }
            }

            // Nếu không tìm thấy tường nào ở hướng đó, dùng TownCenter làm mốc + fallback offset
            float referenceX = farthestWallX;
            if (!wallFound)
            {
                referenceX = isRight ? (townCenterX + noWallFallbackDistance) : (townCenterX - noWallFallbackDistance);
            }

            // Tính toán khoảng min, max thực tế dựa trên hướng
            float randomX;
            if (isRight)
            {
                float minX = referenceX + minDistance;
                float maxX = referenceX + maxDistance;
                randomX = Random.Range(minX, maxX);
            }
            else
            {
                // Khi bên trái, vị trí sẽ là trừ đi khoảng cách
                float maxX = referenceX - minDistance;
                float minX = referenceX - maxDistance;
                randomX = Random.Range(minX, maxX); // Random.Range automatically handles min/max order if reversed, but good practice to keep it correct
            }

            return new Vector3(randomX, townCenterY, townCenterZ);
        }

        public static float GetRandomHuntingPos()
        {
            // Đi săn thường ở xa tường thành một chút
            bool isRight = Random.value > 0.5f;
            return GetRandomOutsideSafePos(isRight, 5f, 50f).x;
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

        public static bool IsInKingdomSafeZone(float posX, float padding = 0f)
        {
            var wallManager = Script.Game.Structures.Wall_And_Stump.WallAndStumpManager.Instance;
            if (wallManager == null) return false;

            float townCenterX = 0f;
            var tc = BuildingManager.instance?.currentTownCenter;
            if (tc != null) townCenterX = tc.transform.position.x;

            // Mặc định vùng an toàn là Plaza nếu chưa xây được bức tường nào
            float plazaRadius = 10f;
            var tcBehavior = BuildingManager.instance.currentTownCenter?.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
            if (tcBehavior != null) plazaRadius = tcBehavior.plazaDistance;

            // [TACTICAL BOUNDARIES] Lấy mốc an toàn từ GuardManager (Điểm chiến tuyến thực tế)
            var gm = Script.Game.Mobs.MobsManager.GuardManager.Instance;
            
            float leftBound = townCenterX - plazaRadius;
            if (gm != null && gm.currentLeftWall != null)
            {
                leftBound = gm.currentLeftWall.transform.position.x;
            }

            float rightBound = townCenterX + plazaRadius;
            if (gm != null && gm.currentRightWall != null)
            {
                rightBound = gm.currentRightWall.transform.position.x;
            }

            // Thu hẹp vùng an toàn lại bằng padding, giới hạn không vượt qua TownCenter
            float paddedLeft = Mathf.Min(townCenterX, leftBound + padding);
            float paddedRight = Mathf.Max(townCenterX, rightBound - padding);

            return posX >= paddedLeft && posX <= paddedRight;
        }

        public static bool IsInPlazaZone(float posX)
        {
            if (BuildingManager.instance == null || BuildingManager.instance.currentTownCenter == null) return false;
            float townCenterX = BuildingManager.instance.currentTownCenter.transform.position.x;
            float radius = 10f;
            var tcBehavior = BuildingManager.instance.currentTownCenter.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
            if (tcBehavior != null) radius = tcBehavior.plazaDistance;
            return posX >= townCenterX - radius && posX <= townCenterX + radius;
        }

        public static bool IsInSavePos(float posX)
        {
            return IsInPlazaZone(posX) || IsInKingdomSafeZone(posX);
        }
    }
}



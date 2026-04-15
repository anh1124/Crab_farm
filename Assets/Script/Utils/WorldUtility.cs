using UnityEngine;
using Script.Game.Other;
using Script.Manager.Building;

namespace Script.Utils
{
    public enum KingdomSide
    {
        Left,
        Right,
        None
    }

    public static class WorldUtility
    {
        private static Script.Manager.Building.BuildingManager _bm;
        private static Script.Manager.Building.BuildingManager BM => _bm != null ? _bm : (_bm = Object.FindObjectOfType<Script.Manager.Building.BuildingManager>());

        private static Script.Game.Mobs.MobsManager.GuardManager _gm;
        private static Script.Game.Mobs.MobsManager.GuardManager GM => _gm != null ? _gm : (_gm = Object.FindObjectOfType<Script.Game.Mobs.MobsManager.GuardManager>());

        private static Script.Game.Structures.Wall_And_Stump.WallAndStumpManager _wm;
        private static Script.Game.Structures.Wall_And_Stump.WallAndStumpManager WM => _wm != null ? _wm : (_wm = Object.FindObjectOfType<Script.Game.Structures.Wall_And_Stump.WallAndStumpManager>());
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
            if (BM == null || BM.currentTownCenter == null)
            {
                Debug.LogWarning("[WorldUtility] TownCenter not found! Returning random position near origin.");
                // Fallback: Random position trong phạm vi 10f từ (0,0)
                return Random.Range(-10f, 10f);
            }

            var townCenter = BM.currentTownCenter;
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
            if (BM == null || BM.currentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found!");
                return 0f;
            }

            float townCenterX = BM.currentTownCenter.transform.position.x;

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
            if (BM == null || BM.currentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found! Returning zero. Cannot determine outside safe zone.");
                return Vector3.zero;
            }

            Vector3 townCenterPos = BM.currentTownCenter.transform.position;
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

            // Nếu không tìm thấy tường nào ở hướng đó, dùng điểm xa nhất của plaza làm mốc
            float referenceX = farthestWallX;
            if (!wallFound)
            {
                // Lấy plaza distance từ TownCenterBehavior
                float plazaDistance = 10f; // default
                var tcBehavior = BM.currentTownCenter.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
                if (tcBehavior != null) plazaDistance = tcBehavior.plazaDistance;

                referenceX = isRight ? (townCenterX + plazaDistance) : (townCenterX - plazaDistance);
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

        public static KingdomSide GetNearestHuntingSide(Vector3 position)
        {
            if (BM == null || BM.currentTownCenter == null) return KingdomSide.None;

            float townCenterX = BM.currentTownCenter.transform.position.x;

            // Tìm bức tường xa nhất về trái và phải
            GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
            float farthestLeft = townCenterX;
            float farthestRight = townCenterX;

            foreach (var wall in walls)
            {
                float wx = wall.transform.position.x;
                if (wx < farthestLeft) farthestLeft = wx;
                if (wx > farthestRight) farthestRight = wx;
            }

            // Nếu không có tường, coi bãi săn bắt đầu từ điểm xa nhất của plaza
            if (farthestLeft == townCenterX || farthestRight == townCenterX)
            {
                float plazaDistance = 10f; // default
                var tcBehavior = BM.currentTownCenter.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
                if (tcBehavior != null) plazaDistance = tcBehavior.plazaDistance;

                if (farthestLeft == townCenterX) farthestLeft -= plazaDistance;
                if (farthestRight == townCenterX) farthestRight += plazaDistance;
            }

            float distToLeft = Mathf.Abs(position.x - farthestLeft);
            float distToRight = Mathf.Abs(position.x - farthestRight);

            return distToLeft < distToRight ? KingdomSide.Left : KingdomSide.Right;
        }

        public static KingdomSide GetObjectSideInKingdom(Vector3 position)
        {
            if (BM == null || BM.currentTownCenter == null)
            {
                return KingdomSide.None;
            }

            float townCenterX = BM.currentTownCenter.transform.position.x;
            float posX = position.x;

            if (posX < townCenterX)
            {
                return KingdomSide.Left;
            }
            else if (posX > townCenterX)
            {
                return KingdomSide.Right;
            }
            else
            {
                return KingdomSide.None; // Exactly at town center
            }
        }

        public static KingdomSide GetObjectSideInKingdom(GameObject obj)
        {
            if (obj == null)
            {
                return KingdomSide.None;
            }
            return GetObjectSideInKingdom(obj.transform.position);
        }

        public static Vector2 GetWallPosition(bool getLeftWall)
        {
            if (BM == null || BM.currentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found!");
                return Vector2.zero;
            }

            float townCenterX = BM.currentTownCenter.transform.position.x;
            float townCenterY = BM.currentTownCenter.transform.position.y;

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
            if (BM == null || BM.currentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found!");
                return null;
            }
            return BM.currentTownCenter.gameObject;
        }

        public static int GetTownCenterTier()
        {
            if (BM == null || BM.currentTownCenter == null) return 0;
            var tcBehavior = BM.currentTownCenter.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
            if (tcBehavior != null && tcBehavior.status != null)
            {
                return tcBehavior.status.tier;
            }
            return 0;
        }

        public static bool IsInKingdomSafeZone(float posX, float padding = 0f)
        {
            var wallManager = WM;
            if (wallManager == null) return false;

            float townCenterX = 0f;
            var tc = BM?.currentTownCenter;
            if (tc != null) townCenterX = tc.transform.position.x;

            // Mặc định vùng an toàn là Plaza nếu chưa xây được bức tường nào
            float plazaRadius = 10f;
            var tcBehavior = BM.currentTownCenter?.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
            if (tcBehavior != null) plazaRadius = tcBehavior.plazaDistance;

            // [TACTICAL BOUNDARIES] Lấy mốc an toàn từ GuardManager (Điểm chiến tuyến thực tế)
            var gm = GM;
            
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
            if (BM == null || BM.currentTownCenter == null) return false;
            float townCenterX = BM.currentTownCenter.transform.position.x;
            float radius = 10f;
            var tcBehavior = BM.currentTownCenter.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
            if (tcBehavior != null) radius = tcBehavior.plazaDistance;
            return posX >= townCenterX - radius && posX <= townCenterX + radius;
        }

        public static bool IsInSavePos(float posX)
        {
            return IsInPlazaZone(posX) || IsInKingdomSafeZone(posX);
        }

        public static bool IsInHuntingZone(float posX, Script.Game.Mobs.MobsManager.IGuardProvider gm, bool isLeft)
        {
            if (gm == null) return false;
            
            var startPos = gm.GetHuntingRangeStartPosition(isLeft);
            if (!startPos.HasValue) return false;

            float startX = startPos.Value.x;
            float endX = isLeft ? (startX - gm.archerHuntingRange) : (startX + gm.archerHuntingRange);

            if (isLeft) return posX <= startX && posX >= endX;
            else return posX >= startX && posX <= endX;
        }

        public static (float start, float end) GetHuntingBounds(Script.Game.Mobs.MobsManager.IGuardProvider gm, bool isLeft)
        {
            if (gm == null) return (0f, 0f);
            var startPos = gm.GetHuntingRangeStartPosition(isLeft);
            if (!startPos.HasValue) return (0f, 0f);

            float startX = startPos.Value.x;
            float endX = isLeft ? (startX - gm.archerHuntingRange) : (startX + gm.archerHuntingRange);
            return (startX, endX);
        }

        #region Pause Menu Report Utilities

        /// <summary>
        /// Lấy số coin TownCenter đang giữ
        /// </summary>
        public static int GetTownCenterCoinCount()
        {
            var tc = GetTownCenter();
            if (tc == null) return 0;
            
            var tcStatus = tc.GetComponent<Script.Data_Model.Status.TownCenterStatus>();
            if (tcStatus == null) return 0;
            
            return tcStatus.storedCoinCount;
        }

        /// <summary>
        /// Đếm số wall còn lành lặn (HP >= criticalHPThreshold) theo hướng
        /// </summary>
        public static int GetHealthyWallCount(bool isLeft)
        {
            if (WM == null) return 0;
            
            var walls = isLeft ? WM.leftWalls : WM.rightWalls;
            if (walls == null) return 0;
            
            int count = 0;
            int threshold = WM.criticalHPThreshold;
            
            foreach (var wall in walls)
            {
                if (wall == null) continue;
                var wallStatus = wall.GetComponent<Script.Data_Model.Status.WallStatus>();
                if (wallStatus != null && wallStatus.currentHp >= threshold)
                {
                    count++;
                }
            }
            
            return count;
        }

        /// <summary>
        /// Lấy thông tin portal (isActive, remainingDays)
        /// </summary>
        public static (bool isActive, int remainingDays) GetPortalInfo(bool isRight)
        {
            var portalManager = Script.Manager.GamePlayManager.EnemyPortalManager.Instance;
            if (portalManager == null) return (false, 0);
            
            var portal = isRight ? portalManager.portalR : portalManager.portalL;
            if (portal == null) return (false, 0);
            
            var portalStatus = portal.GetComponent<Script.Entities.Structures.SummondPortal.EnemyPortalStatus>();
            if (portalStatus == null) return (false, 0);
            
            if (portalStatus.isReadyToSpawn) return (true, 0);
            
            // Tính ngày còn lại để active
            var timeProvider = Object.FindObjectOfType<Script.Manager.WorldManager.GameTimeManager>();
            if (timeProvider == null) return (false, 0);
            
            int currentDay = timeProvider.GetElapsedDays();
            int activationDay = portalStatus.spawnDay + portalStatus.activeDelay;
            int remainingDays = Mathf.Max(0, activationDay - currentDay);
            
            return (false, remainingDays);
        }

        #endregion
    }
}



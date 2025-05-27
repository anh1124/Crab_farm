using UnityEngine;
using Script.Game.Other;

namespace Script
{
    public static class Utility 
    {
        public static bool isDebug = false;
        
        public static void RotateObjectTowardsTownCenter(GameObject obj, Vector3 referencePos, Vector3 townCenterPos)
        {
            Vector3 scale = obj.transform.localScale;
            
            // Calculate direction from reference position to town center
            Vector3 directionFromTownCenter = referencePos - townCenterPos;
            
            // We want objects to face RIGHT by default (x positive)
            // If object is to the left of town center, face right (scale.x positive)
            // If object is to the right of town center, face left (scale.x negative)
            if (directionFromTownCenter.x < 0)
            {
                // Object is to the LEFT of town center - should face RIGHT
                scale.x =-1f * Mathf.Abs(scale.x); // Ensure positive scale (face right)
            }
            else
            {
                // Object is to the RIGHT of town center - should face LEFT
                scale.x =  Mathf.Abs(scale.x); // Ensure negative scale (face left)
            }
            
            if (isDebug)
            {
                Debug.Log($"Utility.RotateObjectTowardsTownCenter: Object {obj.name} at pos {referencePos}, town center at {townCenterPos}, direction {directionFromTownCenter}, scale.x={scale.x}");
            }
            
            obj.transform.localScale = scale;
            
            // Ensure Z rotation is always 0
            obj.transform.rotation = Quaternion.identity;
        }

        /// <summary>
        /// Gets a random X position between town center and nearest walls (plaza area)
        /// </summary>
        public static float GetRandomPlazaPos()
        {
            if (BuildingManager.instance == null || BuildingManager.instance.CurrentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found!");
                return 0f;
            }

            float townCenterX = BuildingManager.instance.CurrentTownCenter.transform.position.x;
            
            // Find nearest walls on both sides
            GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
            float nearestLeftWall = float.MinValue;
            float nearestRightWall = float.MaxValue;

            foreach (GameObject wall in walls)
            {
                float wallX = wall.transform.position.x;
                if (wallX < townCenterX && wallX > nearestLeftWall)
                {
                    nearestLeftWall = wallX;
                }
                if (wallX > townCenterX && wallX < nearestRightWall)
                {
                    nearestRightWall = wallX;
                }
            }

            // If no walls found, use a default range
            if (nearestLeftWall == float.MinValue) nearestLeftWall = townCenterX - 5f;
            if (nearestRightWall == float.MaxValue) nearestRightWall = townCenterX + 5f;

            return Random.Range(nearestLeftWall, nearestRightWall);
        }

        /// <summary>
        /// Gets a random X position between farthest walls (kingdom safe area)
        /// </summary>
        public static float GetRandomKingdomSafePos()
        {
            if (BuildingManager.instance == null || BuildingManager.instance.CurrentTownCenter == null)
            {
                Debug.LogError("BuildingManager or TownCenter not found!");
                return 0f;
            }

            float townCenterX = BuildingManager.instance.CurrentTownCenter.transform.position.x;
            
            // Find farthest walls on both sides
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

            // If no walls found, use a default range
            if (farthestLeftWall == townCenterX) farthestLeftWall = townCenterX - 15f;
            if (farthestRightWall == townCenterX) farthestRightWall = townCenterX + 15f;

            return Random.Range(farthestLeftWall, farthestRightWall);
        }

        /// <summary>
        /// Di chuyển một GameObject đến vị trí mục tiêu với tốc độ cho trước
        /// </summary>
        /// <param name="objectToMove">GameObject cần di chuyển</param>
        /// <param name="target">Vị trí đích</param>
        /// <param name="speed">Tốc độ di chuyển</param>
        /// <returns>True nếu đã đến đích (khoảng cách < 0.1f), False nếu chưa</returns>
        public static bool MoveTowardsTarget(GameObject objectToMove, Vector3 target, float speed)
        {
            FlipDirection(objectToMove, target);
            
            Vector2 currentPosition = objectToMove.transform.position;
            Vector2 newPosition = Vector2.MoveTowards(
                new Vector2(currentPosition.x, currentPosition.y),
                new Vector2(target.x, target.y),
                speed * Time.deltaTime
            );
            
            objectToMove.transform.position = new Vector3(newPosition.x, newPosition.y, objectToMove.transform.position.z);

            // Kiểm tra xem đã đến đích chưa
            return Vector2.Distance(objectToMove.transform.position, target) < 0.1f;
        }

        /// <summary>
        /// Xoay hướng GameObject dựa vào vị trí mục tiêu
        /// </summary>
        /// <param name="objectToFlip">GameObject cần xoay hướng</param>
        /// <param name="target">Vị trí mục tiêu</param>
        public static void FlipDirection(GameObject objectToFlip, Vector3 target)
        {
            Vector3 currentScale = objectToFlip.transform.localScale;
            objectToFlip.transform.localScale = new Vector3(
                target.x < objectToFlip.transform.position.x ? -1 : 1,
                currentScale.y,
                currentScale.z
            );
        }
    }
}

using UnityEngine;

namespace Script.Utils
{
    public static class MovementUtility
    {
        /// <summary>
        /// Di chuyển một GameObject đến vị trí mục tiêu với tốc độ cho trước (chỉ di chuyển theo trục X)
        /// </summary>
        public static bool MoveTowardsTarget(GameObject objectToMove, Vector3 target, float speed)
        {
            FlipDirection(objectToMove, target);

            Vector2 currentPosition = objectToMove.transform.position;
            float newX = Mathf.MoveTowards(currentPosition.x, target.x, speed * Time.deltaTime);
            objectToMove.transform.position = new Vector3(newX, objectToMove.transform.position.y, objectToMove.transform.position.z);

            return Mathf.Abs(objectToMove.transform.position.x - target.x) < 0.1f;
        }

        /// <summary>
        /// Xoay hướng GameObject dựa vào vị trí mục tiêu
        /// </summary>
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



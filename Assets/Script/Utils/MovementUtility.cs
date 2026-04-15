using UnityEngine;

namespace Script.Utils
{
    public static class MovementUtility
    {
       
        /// Khoảng cách mặc định để coi là "đã đến nơi" (dùng chung cho tất cả State)
        public const float DEFAULT_STOPPING_DISTANCE = 0.1f;

        /// Ngưỡng tối thiểu để coi là đang di chuyển (tránh rung giật nhỏ)
        public const float MOVEMENT_EPSILON = 0.001f;

        /// Kiểm tra xem vị trí hiện tại có đủ gần mục tiêu không.
        /// Dùng thay cho inline `Vector3.Distance(...) &lt; 0.1f` trong các State.
        public static bool HasReachedDestination(Vector3 current, Vector3 target, float tolerance = DEFAULT_STOPPING_DISTANCE)
        {
            return Vector3.Distance(current, target) < tolerance;
        }
        /// Kiểm tra xem đối tượng có thực sự dịch chuyển không (tránh rung giật nhỏ).
        /// Dùng thay cho inline `Mathf.Abs(newX - currentX) > 0.001f`. Áp dụng được cho cả trục X và Y.
        public static bool IsMoving(float previous, float current, float epsilon = MOVEMENT_EPSILON)
        {
            return Mathf.Abs(current - previous) > epsilon;
        }

        /// Di chuyển một GameObject đến vị trí mục tiêu với tốc độ cho trước (chỉ di chuyển theo trục X)
        /// Trả về true nếu đã đến nơi.
        public static bool MoveTowardsTarget(GameObject objectToMove, Vector3 target, float speed)
        {
            FlipDirection(objectToMove, target);

            Vector2 currentPosition = objectToMove.transform.position;
            float newX = Mathf.MoveTowards(currentPosition.x, target.x, speed * Time.deltaTime);
            objectToMove.transform.position = new Vector3(newX, objectToMove.transform.position.y, objectToMove.transform.position.z);

            return HasReachedDestination(objectToMove.transform.position, target);
        }

  
        /// Xoay hướng GameObject dựa vào vị trí mục tiêu (Lật Scale)
        public static void FlipDirection(GameObject objectToFlip, Vector3 target)
        {
            Vector3 currentScale = objectToFlip.transform.localScale;
            objectToFlip.transform.localScale = new Vector3(
                target.x < objectToFlip.transform.position.x ? -1 : 1,
                currentScale.y,
                currentScale.z
            );
        }

        /// Quay mặt về phía mục tiêu (Sử dụng cho logic AI)
        public static void FaceTarget(GameObject self, Vector3 target)
        {
            FlipDirection(self, target);
        }
    }
}


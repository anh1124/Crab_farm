using UnityEngine;

namespace Script.Manager
{
    /// <summary>
    /// Chịu trách nhiệm di chuyển và xoay một đối tượng (mũi tên).
    /// Gắn trên Manager và điều khiển transform của mũi tên được truyền vào.
    /// </summary>
    public class ArrowGuideMover : MonoBehaviour
    {
        // Runtime references
        private Transform objectToMove; // Đối tượng thực tế sẽ bị di chuyển (Mũi tên)
        private Transform target;
        private Transform player;
        private bool isMovingToTarget;

        /// <summary>
        /// Thiết lập đối tượng cần di chuyển.
        /// </summary>
        public void SetObjectToMove(Transform guideTransform)
        {
            objectToMove = guideTransform;
        }

        /// <summary>
        /// Khởi tạo mover với player, target và chế độ (follow hay di chuyển đến).
        /// </summary>
        public void Initialize(Transform playerRef, Transform targetRef, bool moveToTarget)
        {
            player = playerRef;
            target = targetRef;
            isMovingToTarget = moveToTarget;
            
            if (objectToMove != null && player != null && ArrowGuideManager.Instance != null)
            {
                objectToMove.position = player.position + ArrowGuideManager.Instance.spawnOffsetFromPlayer;
            }
        }

        /// <summary>
        /// Cập nhật target hoặc chế độ di chuyển sau khi khởi tạo.
        /// </summary>
        public void SetTarget(Transform playerRef, Transform newTarget, bool moveToTarget)
        {
            player = playerRef;
            target = newTarget;
            isMovingToTarget = moveToTarget;
        }

        /// <summary>
        /// Gọi mỗi frame từ Manager.Update().
        /// Thực hiện di chuyển (follow hoặc đến target) và xoay hướng về target.
        /// </summary>
        public void UpdateMovement()
        {
            var manager = ArrowGuideManager.Instance;
            if (objectToMove == null || target == null || player == null || manager == null) return;

            // 1. Di chuyển
            if (isMovingToTarget)
                objectToMove.position = Vector3.MoveTowards(objectToMove.position, target.position, manager.moveSpeed * Time.deltaTime);
            else
                objectToMove.position = player.position + manager.spawnOffsetFromPlayer;

            // 2. Xoay về phía target
            Vector3 dir = target.position - objectToMove.position;
            if (dir != Vector3.zero)
            {
                float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                float offset = GetDirectionOffset(manager.defaultDirection);
                float targetAngle = baseAngle + offset + manager.customRotationOffset;
                Quaternion targetRot = Quaternion.Euler(0, 0, targetAngle);
                objectToMove.rotation = Quaternion.Slerp(objectToMove.rotation, targetRot, manager.rotationSpeed * Time.deltaTime);
            }
        }

        /// <summary>
        /// Kiểm tra đã đến target chưa (dùng squared distance).
        /// </summary>
        public bool HasReachedTarget()
        {
            var manager = ArrowGuideManager.Instance;
            if (objectToMove == null || target == null || manager == null) return false;
            
            float sqrReachThreshold = manager.reachThreshold * manager.reachThreshold;
            return (objectToMove.position - target.position).sqrMagnitude < sqrReachThreshold;
        }

        private float GetDirectionOffset(ArrowGuideManager.SpriteDirection direction)
        {
            return direction switch
            {
                ArrowGuideManager.SpriteDirection.Up => -90f,
                ArrowGuideManager.SpriteDirection.Right => 0f,
                ArrowGuideManager.SpriteDirection.Down => 90f,
                ArrowGuideManager.SpriteDirection.Left => 180f,
                _ => -90f
            };
        }
    }
}

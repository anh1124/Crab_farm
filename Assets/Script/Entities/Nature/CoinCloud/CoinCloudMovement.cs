using UnityEngine;

namespace Script.Game.CoinCloud
{
    /// <summary>
    /// Xử lý di chuyển của đám mây tiền đến target position
    /// </summary>
    public class CoinCloudMovement : MonoBehaviour
    {
        [Tooltip("Tốc độ bay của đám mây")]
        public float moveSpeed = 5f;
        
        [Tooltip("Khoảng cách tối thiểu để coi là đã đến target")]
        public float arrivalThreshold = 0.1f;
        
        // State
        private Vector3 targetPosition;
        private bool isMoving = false;
        private bool hasReachedTarget = false;
        
        public bool IsMoving => isMoving;
        public bool HasReachedTarget => hasReachedTarget;
        public Vector3 TargetPosition => targetPosition;
        
        /// <summary>
        /// Bắt đầu di chuyển đến target
        /// </summary>
        public void StartMoving(Vector3 target)
        {
            targetPosition = target;
            isMoving = true;
            hasReachedTarget = false;
        }
        
        /// <summary>
        /// Dừng di chuyển
        /// </summary>
        public void StopMoving()
        {
            isMoving = false;
        }
        
        /// <summary>
        /// Force đánh dấu đã đến target (dùng khi restore từ save)
        /// </summary>
        public void ForceReachTarget()
        {
            hasReachedTarget = true;
            isMoving = false;
        }
        
        private void Update()
        {
            if (isMoving && !hasReachedTarget)
            {
                MoveTowardsTarget();
            }
        }
        
        private void MoveTowardsTarget()
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            
            if (distanceToTarget <= arrivalThreshold)
            {
                transform.position = targetPosition;
                hasReachedTarget = true;
                isMoving = false;
            }
            else
            {
                float moveDistance = moveSpeed * Time.deltaTime;
                if (moveDistance >= distanceToTarget)
                {
                    transform.position = targetPosition;
                    hasReachedTarget = true;
                    isMoving = false;
                }
                else
                {
                    transform.position += direction * moveDistance;
                }
            }
        }
        
        /// <summary>
        /// Tính toán vị trí target dựa trên TownCenter
        /// </summary>
        public Vector3 CalculateTargetPosition(Vector3 townCenterPosition, float targetY, float targetXRange)
        {
            float randomXOffset = Random.Range(-targetXRange, targetXRange);
            return new Vector3(
                townCenterPosition.x + randomXOffset,
                targetY,
                0f
            );
        }
        
        /// <summary>
        /// Khởi tạo vị trí spawn
        /// </summary>
        public void InitializeSpawnPosition(float spawnXRange, float spawnY)
        {
            float randomX = Random.Range(-spawnXRange, spawnXRange);
            transform.position = new Vector3(randomX, spawnY, 0f);
        }
    }
}

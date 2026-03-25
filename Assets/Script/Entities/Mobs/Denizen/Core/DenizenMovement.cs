using UnityEngine;

namespace Script.Game.Mobs.Denizen.Core
{
    /// <summary>
    /// DenizenMovement: Tách biệt logic di chuyển ra khỏi Behavior.
    /// Là component duy nhất được phép ghi transform.position mỗi frame.
    /// </summary>
    public class DenizenMovement : MonoBehaviour
    {
        private Vector3 _moveTarget;
        private float _moveSpeed;
        private bool _isMovingToTarget;

        public const float REACHED_THRESHOLD = 0.15f;
        public bool HasReachedTarget => !_isMovingToTarget || Vector3.Distance(transform.position, _moveTarget) < REACHED_THRESHOLD;
        public bool IsMoving => _isMovingToTarget;

        /// <summary>
        /// Bắt đầu di chuyển tới vị trí đích.
        /// </summary>
        public void MoveTo(Vector3 target, float speed)
        {
            _moveTarget = new Vector3(target.x, transform.position.y, transform.position.z);
            _moveSpeed = speed;
            _isMovingToTarget = true;
        }

        /// <summary>
        /// Dừng di chuyển ngay lập tức.
        /// </summary>
        public void StopMovement()
        {
            _isMovingToTarget = false;
        }

        /// <summary>
        /// Quay mặt về phía target.
        /// </summary>
        public void FaceTarget(Vector3 targetPos)
        {
            float dir = targetPos.x - transform.position.x;
            if (Mathf.Abs(dir) < 0.01f) return;

            Vector3 scale = transform.localScale;
            scale.x = dir > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        /// <summary>
        /// Quay mặt ra phía ngoài (Dành cho Archer đứng thủ Wall).
        /// </summary>
        public void FaceOutward(bool isLeftFlank)
        {
            Vector3 scale = transform.localScale;
            scale.x = isLeftFlank ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }

        private void Update()
        {
            if (!_isMovingToTarget) return;

            transform.position = Vector3.MoveTowards(transform.position, _moveTarget, _moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _moveTarget) < REACHED_THRESHOLD)
            {
                _isMovingToTarget = false;
            }
        }
    }
}

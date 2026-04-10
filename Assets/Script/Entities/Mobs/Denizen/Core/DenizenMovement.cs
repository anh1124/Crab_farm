using UnityEngine;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Core
{
    /// <summary>
    /// DenizenMovement: Thành phần tự trị xử lý di chuyển vật lý.
    /// CHỈ: Di chuyển và flip — KHÔNG set animation.
    /// Animation do từng State quyết định.
    /// </summary>
    public class DenizenMovement : MonoBehaviour
    {
        [Header("Movement State")]
        [SerializeField] private Vector3 _moveTargetPos;
        public Vector3 MoveTargetPos => _moveTargetPos;
        public float CurrentSpeed { get; private set; }

        /// <summary>Readonly: có đang yêu cầu di chuyển không.</summary>
        public bool IsMoving => _context != null && _context.Status != null && _context.Status.isMoving;

        private DenizenContext _context;
        private float _currentAssignedSpeed = -1f;

        private void Awake()
        {
            _context = GetComponent<DenizenContext>();
        }

        public void SetTarget(Vector3 target, float speed = -1f)
        {
            if (_context != null && _context.Status != null)
                _context.Status.isMoving = true;

            _moveTargetPos = target;
            _currentAssignedSpeed = speed;
        }

        public void Stop()
        {
            if (_context != null && _context.Status != null)
                _context.Status.isMoving = false;
            _currentAssignedSpeed = -1f;
        }

        private void Update()
        {
            if (_context == null || _context.Status == null) return;

            bool skillAllows = !_context.IsSkillCasting || _context.SkillAllowsMovement;
            bool statusAllows = _context.Status.CanMove;
            bool isMoving    = _context.Status.isMoving;

            if (isMoving && skillAllows && statusAllows)
            {
                float speed = _currentAssignedSpeed > 0f ? _currentAssignedSpeed : _context.Status.walkSpeed;
                CurrentSpeed = speed;
                _context.Status.currentSpeed = speed; // Sync to Status for Debugging
                MovementUtility.MoveTowardsTarget(gameObject, _moveTargetPos, speed);

                // Flip theo hướng di chuyển
                float dir = _moveTargetPos.x - transform.position.x;
                if (Mathf.Abs(dir) > 0.05f)
                    transform.localScale = new Vector3(dir > 0 ? 1 : -1, 1, 1);
            }
            else
            {
                CurrentSpeed = 0f;
                if (_context.Status != null) _context.Status.currentSpeed = 0f;
            }
        }
    }
}

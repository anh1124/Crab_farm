using UnityEngine;

namespace Script.Game.Enemy.Enemy_Type
{
    public class Penguin : Base.Enemy
    {
        public enum PenguinState
        {
            Idle,
            Walk,
            Attack
        }

        [SerializeField] private float patrolSpeed = 2f;
        [SerializeField] private float idleDuration = 3f;
        [SerializeField] private float raycastDistance = 2f;
        [SerializeField] private LayerMask groundLayer;

        public GameObject raycastHolder;

        [SerializeField] private PenguinState currentState;
        [SerializeField] private float idleTimer;
        private Animator _animator;

        private void Start()
        {
            Initialize();
            _animator = GetComponent<Animator>();
            currentState = PenguinState.Walk;
        }

        private void Update()
        {
            switch (currentState)
            {
                case PenguinState.Idle:
                    _animator.Play("idle");
                    HandleIdleState();
                    break;
                case PenguinState.Walk:
                    _animator.Play("walk");
                    HandleWalkState();
                    break;
                case PenguinState.Attack:
                    _animator.Play("attack");
                    HandleAttackState();
                    break;
            }
        }

        private void HandleIdleState()
        {
            idleTimer += Time.deltaTime;
            if (idleTimer >= idleDuration)
            {
                idleTimer = 0;
                currentState = PenguinState.Walk;
                Flip();
            }
        }

        private void HandleWalkState()
        {
            Vector2 velocity = IsFacingRight ? Vector2.right : Vector2.left;
            MoveEnemy(velocity * patrolSpeed);

            // Use the position of raycastHolder for raycasting
            Vector2 origin = raycastHolder.transform.position;
            Vector2 direction = IsFacingRight ? Vector2.right : Vector2.left;
            RaycastHit2D hit = Physics2D.Raycast(origin, direction, raycastDistance, groundLayer);

            // Debug log for hit information
            //Debug.Log(hit.collider != null ? $"Hit: {hit.collider.name}" : "No hit");

            if (hit.collider != null && hit.collider.CompareTag("Ground"))
            {
                currentState = PenguinState.Idle;
                Rb.velocity = Vector2.zero;
            }
        }

        private void HandleAttackState()
        {
            // Chạy animation attack
            // TODO: Thêm logic tìm collidel có tag player và tấn công
        }

        private void Flip()
        {
            IsFacingRight = !IsFacingRight;
            Vector3 scaler = transform.localScale;
            scaler.x *= -1;
            transform.localScale = scaler;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = IsFacingRight ? Color.red : Color.blue;
            Vector3 direction = IsFacingRight ? Vector3.right : Vector3.left;
            Vector3 position = raycastHolder.transform.position;
            Gizmos.DrawLine(position, position + direction * raycastDistance);
        }
    }
}

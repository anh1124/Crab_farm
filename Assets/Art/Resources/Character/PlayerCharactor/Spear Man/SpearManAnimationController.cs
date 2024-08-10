using System;
using UnityEngine;

namespace Art.Resources.Character.PlayerCharactor.Spear_Man
{
    public class SpearManAnimationController : MonoBehaviour
    {
        public Rigidbody2D rb;
        public bool isGround;

        private PlayerController _playerController;
        public Status status;

        private Animator _animator;
        private Vector2 _previousPosition;
        private bool _isFacingRight = true;

        public float speedNeedToShowRun = 0.1f;
        private void Start()
        {
            status = gameObject.GetComponent<Status>();
            _animator = GetComponent<Animator>();
            _previousPosition = rb.position;
        }

        private void Update()
        {
            UpdateStatus();
            UpdateFacingDirection();
            UpdateAnimationState();
        }

        public void UpdateStatus()
        {
            isGround = status.isGround;
        }

        private void UpdateFacingDirection()
        {
            Vector2 currentPosition = rb.position;
            Vector2 movement = currentPosition - _previousPosition;

            if (movement.x > 0 && !_isFacingRight)
            {
                Flip();
            }
            else if (movement.x < 0 && _isFacingRight)
            {
                Flip();
            }

            _previousPosition = currentPosition;
        }

        private void Flip()
        {
            _isFacingRight = !_isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
        }

        private void UpdateAnimationState()
        {
            float speed = rb.velocity.magnitude;

            if (isGround)
            {
                if (speed >= speedNeedToShowRun)
                {
                    _animator.Play("Run");
                }
                else
                {
                    _animator.Play("Idle");
                }
            }
        }
    }
}
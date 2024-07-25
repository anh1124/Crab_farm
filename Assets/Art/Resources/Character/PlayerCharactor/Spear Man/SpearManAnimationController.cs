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

        private Animator animator;
        private Vector2 previousPosition;
        private bool isFacingRight = true;

        private void Start()
        {
            status = gameObject.GetComponent<Status>();
            animator = GetComponent<Animator>();
            previousPosition = rb.position;
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
            Vector2 movement = currentPosition - previousPosition;

            if (movement.x > 0 && !isFacingRight)
            {
                Flip();
            }
            else if (movement.x < 0 && isFacingRight)
            {
                Flip();
            }

            previousPosition = currentPosition;
        }

        private void Flip()
        {
            isFacingRight = !isFacingRight;
            Vector3 localScale = transform.localScale;
            localScale.x *= -1;
            transform.localScale = localScale;
        }

        private void UpdateAnimationState()
        {
            float speed = rb.velocity.magnitude;

            if (isGround)
            {
                if (speed > 0.1f)
                {
                    animator.Play("Run");
                }
                else
                {
                    animator.Play("Idle");
                }
            }
        }
    }
}
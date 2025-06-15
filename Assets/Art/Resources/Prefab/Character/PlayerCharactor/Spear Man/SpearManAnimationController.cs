using System;
using Script.PlayerController;
using UnityEngine;

namespace Art.Resources.Character.PlayerCharactor.Spear_Man
{
    public class SpearManAnimationController : MonoBehaviour
    {
        #region Fields
        //lưu ý:
        //tất cả các player phải có tag Player và layer Player 
        public Rigidbody2D rb;
        public bool isGround;
        public bool IsSkillAnimationPlaying = false; // Mới: Thêm biến để theo dõi trạng thái animation kỹ năng

        private PlayerController _playerController;
        public BaseStatus status;

        private Animator _animator;
        private Vector2 _previousPosition;
        private bool _isFacingRight = true;

        public float speedNeedToShowRun = 0.1f;
        #endregion

        #region Unity Lifecycle Methods
        private void Start()
        {
            status = gameObject.GetComponent<BaseStatus>();
            _animator = GetComponent<Animator>();
            _previousPosition = rb.position;
        }

        private void Update()
        {
            UpdateStatus();
            UpdateFacingDirection();
            UpdateAnimationState();
        }
        #endregion

        #region Core Animation Logic
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

            // Mới: Chỉ cập nhật animation di chuyển nếu không có animation kỹ năng đang chạy
            if (IsSkillAnimationPlaying)
            {
                return; // Thoát nếu đang phát animation kỹ năng
            }

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
        #endregion

        #region Skill Animation Control
        // Hàm để kích hoạt animation ChopDown
        public void TriggerChopDownAnimation()
        {
            if (_animator == null)
            {
                Debug.LogError("[SpearManAnimationController] Animator không được tìm thấy. Không thể phát animation ChopDown.");
                return;
            }
            IsSkillAnimationPlaying = true; // Báo hiệu animation kỹ năng đang chạy
            _animator.SetTrigger("ChopDown");
            Debug.Log("[SpearManAnimationController] Kích hoạt Trigger ChopDown.");
        }

        // Hàm để kích hoạt animation Thrust
        public void TriggerThrustAnimation()
        {
            if (_animator == null)
            {
                Debug.LogError("[SpearManAnimationController] Animator không được tìm thấy. Không thể phát animation Thrust.");
                return;
            }
            IsSkillAnimationPlaying = true; // Báo hiệu animation kỹ năng đang chạy
            _animator.SetTrigger("Thrust");
            Debug.Log("[SpearManAnimationController] Kích hoạt Trigger Thrust.");
        }

        // Hàm để kích hoạt animation ThrowSpear
        public void TriggerThrowSpearAnimation()
        {
            if (_animator == null)
            {
                Debug.LogError("[SpearManAnimationController] Animator không được tìm thấy. Không thể phát animation ThrowSpear.");
                return;
            }
            IsSkillAnimationPlaying = true; // Báo hiệu animation kỹ năng đang chạy
            _animator.SetTrigger("ThrowSpear");
            Debug.Log("[SpearManAnimationController] Kích hoạt Trigger ThrowSpear.");
        }

        /// <summary>
        /// Hàm này được gọi từ Animation Event trong Unity Editor khi animation kỹ năng kết thúc.
        /// Nó không được gọi trực tiếp từ code C#.
        /// Đảm bảo đã thêm Animation Event vào cuối các Animation Clip của kỹ năng (ChopDown, Thrust, ThrowSpear).
        /// </summary>
        public void OnSkillAnimationEnd()
        {
            Debug.Log("[SpearManAnimationController] Animation kỹ năng kết thúc. Reset trạng thái.");
            IsSkillAnimationPlaying = false; // Reset trạng thái để quay lại animation di chuyển cơ bản
        }
        #endregion
    }
}
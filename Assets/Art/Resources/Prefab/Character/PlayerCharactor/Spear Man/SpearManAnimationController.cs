using System;
using Script.Controller.PlayerController;
using Script.Game.PlayerCharacter;
using UnityEngine;
using Script.Game.Player; // Namespace chứa PlayerBehavior

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

        // Skill system references
        private PlayerSkillManager skillManager;
        private int currentExecutingSkillSlot = -1;
        private string currentSkillTrigger = ""; // Lưu trigger hiện tại để reset sau

        private PlayerController _playerController;

        private Animator _animator;
        private Vector2 _previousPosition;
        private bool _isFacingRight = true;

        public float speedNeedToShowRun = 0.1f;
        #endregion

        #region Unity Lifecycle Methods
        private void Start()
        {
            _animator = GetComponent<Animator>();
            _previousPosition = rb.position;
            
            // NEW: Get skill manager reference
            skillManager = GetComponent<PlayerSkillManager>();
            if (skillManager == null)
            {
                Debug.LogError("[SpearManAnimationController] PlayerSkillManager not found!");
            }
            
            // NEW: Auto-register to PlayerBehavior
            var behavior = GetComponent<PlayerBehavior>();
            if (behavior != null)
            {
                behavior.playerAnim = this;
            }
            else
            {
                 Debug.LogError("[SpearManAnimationController] PlayerBehavior not found!");
            }
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
            // Lấy trạng thái grounded từ PlayerController (đã tính theo hệ thống hiện tại)
            isGround = PlayerController.Instance != null && PlayerController.Instance.isGrounded;
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
        /// <summary>
        /// Trigger skill animation với animation trigger name và slot index
        /// </summary>
        /// <param name="animationTrigger">Tên trigger trong Animator</param>
        /// <param name="slotIndex">Index của skill slot đang execute</param>
        public void TriggerSkillAnimation(string animationTrigger, int slotIndex)
        {
            Debug.Log($"[SpearManAnimationController] TriggerSkillAnimation called: trigger='{animationTrigger}', slot={slotIndex}");
            
            if (_animator == null)
            {
                Debug.LogError("[SpearManAnimationController] Animator not found!");
                return;
            }
            
            if (string.IsNullOrEmpty(animationTrigger))
            {
                // Thay vì Warning, dùng Log bình thường vì đây có thể là chủ ý (hoặc quên uncheck useAnimation)
                Debug.Log($"[SpearManAnimationController] TriggerSkillAnimation: Empty trigger for slot {slotIndex}. (Tip: Uncheck 'Use Animation' in Skill Asset if not needed). Calling OnSkillAnimationEnd.");
                if (skillManager != null && slotIndex >= 0)
                {
                    skillManager.OnSkillAnimationEnd(slotIndex);
                }
                return;
            }
            
            IsSkillAnimationPlaying = true;
            currentExecutingSkillSlot = slotIndex;
            currentSkillTrigger = animationTrigger; // Lưu trigger để reset sau
            _animator.SetTrigger(animationTrigger);
            
            Debug.Log($"[SpearManAnimationController] Triggered animation: {animationTrigger} for slot {slotIndex}");
        }

        /// <summary>
        /// Được gọi từ Animation Event tại frame đầu tiên (Frame 0) để kiểm tra độ trễ.
        /// </summary>
        public void DebugAnimationStart(int slotIndex)
        {
            Debug.Log($"<color=blue>[{System.DateTime.Now:HH:mm:ss.fff}] [SpearManAnimationController] Frame 0 reached for slot {slotIndex}</color>");
        }

        /// <summary>
        /// Được gọi từ Animation Event tại frame thứ 2 để xác nhận animation đang chạy thực sự.
        /// </summary>
        public void DebugAnimationNextFrame(int slotIndex)
        {
            Debug.Log($"<color=orange>[{System.DateTime.Now:HH:mm:ss.fff}] [SpearManAnimationController] Frame 2 reached for slot {slotIndex}</color>");
        }

        /// <summary>
        /// Được gọi từ Animation Event tại frame damage
        /// </summary>
        public void OnSkillAttackHit(int slotIndex)
        {
            if (skillManager != null && slotIndex >= 0)
            {
                skillManager.OnSkillAnimationDamageFrame(slotIndex);
            }
        }

        /// <summary>
        /// Được gọi từ Animation Event khi animation kỹ năng kết thúc.
        /// </summary>
        /// <param name="slotIndex">Index của skill slot</param>
        public void OnAnimationFinish(int slotIndex)
        {
            // Guard: Tránh gọi nhiều lần
            if (!IsSkillAnimationPlaying)
            {
                return;
            }
            
            Debug.Log($"[SpearManAnimationController] OnAnimationFinish (formerly OnSkillAnimationEnd) called for slot {slotIndex}.");
            IsSkillAnimationPlaying = false;
            
            // CRITICAL: Reset trigger để tránh Any State transition lại
            if (_animator != null && !string.IsNullOrEmpty(currentSkillTrigger))
            {
                _animator.ResetTrigger(currentSkillTrigger);
                Debug.Log($"[SpearManAnimationController] Reset trigger: {currentSkillTrigger}");
                currentSkillTrigger = "";
            }
            
            // Force chuyển về Idle animation
            if (_animator != null)
            {
                _animator.Play("Idle", 0, 0f); // Layer 0, normalized time 0
                Debug.Log("[SpearManAnimationController] Forced transition to Idle.");
            }
            
            // NEW: Notify skill manager
            if (skillManager != null && slotIndex >= 0)
            {
                // Gọi sang Manager (Manager vẫn giữ tên hàm cũ, không sao cả)
                skillManager.OnSkillAnimationEnd(slotIndex);
            }
            
            currentExecutingSkillSlot = -1;
        }
        #endregion
    }
}
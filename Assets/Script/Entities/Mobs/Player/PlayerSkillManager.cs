using UnityEngine;
using Script.Game.Player;
using Script.Game.PlayerCharacter.Skills;
using Script.Controller.PlayerController;

namespace Script.Game.PlayerCharacter
{
    /// <summary>
    /// Mở rộng SkillManager cho Player.
    /// Thêm: check stamina/mana, lock movement khi skill chạy, và reset safe khi Player chết.
    /// </summary>
    public class PlayerSkillManager : SkillManager
    {
        // ── Fields ────────────────────────────────────────────────────────

        public PlayerBehavior playerBehavior;
        private int _currentExecutingSlot = -1;

        // ── Unity Lifecycle ───────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            playerBehavior = GetComponent<PlayerBehavior>();
            if (playerBehavior == null)
                Debug.LogError("[PlayerSkillManager] PlayerBehavior not found!");

            // Tạo container để chứa hitbox prefab của các skill
            Transform container = transform.Find("SkillContainer");
            if (container == null)
            {
                GameObject containerObj = new GameObject("SkillContainer");
                containerObj.transform.SetParent(transform, false);
                containerObj.transform.localPosition = Vector3.zero;
                containerObj.transform.localScale = Vector3.one;
                container = containerObj.transform;
                Debug.Log("[PlayerSkillManager] Created SkillContainer automatically.");
            }

            InitializeSkills(container);
        }

        /// <summary>
        /// FIX BUG: Khi Player bị disable (chết), đảm bảo canMove luôn được reset để tránh kẹt input.
        /// </summary>
        private void OnDisable()
        {
            UnlockMovement();
        }

        // ── Public API ────────────────────────────────────────────────────

        /// <summary>
        /// Gọi từ PlayerGui, truyền slotIndex và instigator là Player GameObject.
        /// </summary>
        public bool ExecuteSkill(int slotIndex)
        {
            return ExecuteSkill(slotIndex, playerBehavior != null ? playerBehavior.gameObject : gameObject);
        }

        // ── Animation Event Bridges (giữ lại để Animation Event trong Animator vẫn hoạt động) ──

        /// <summary>
        /// Gọi từ Animation Event tại frame damage. Giữ nguyên chữ ký cũ để không cần sửa Animator.
        /// </summary>
        public void OnSkillAnimationDamageFrame(int slotIndex)
        {
            OnSkillAnimationDamageFrame(slotIndex, playerBehavior != null ? playerBehavior.gameObject : gameObject);
        }

        /// <summary>
        /// Gọi từ Animation Event khi animation kết thúc. Giữ nguyên chữ ký cũ để không cần sửa Animator.
        /// </summary>
        public void OnSkillAnimationEnd(int slotIndex)
        {
            _currentExecutingSlot = -1;
            OnSkillAnimationEnd(slotIndex, playerBehavior != null ? playerBehavior.gameObject : gameObject);
        }

        // ── Override Hooks ─────────────────────────────────────────────────

        protected override bool CanAfford(SkillBase skill, GameObject instigator)
        {
            if (playerBehavior == null) return true;
            
            // Sử dụng hệ thống ResourceType mới
            if (skill.resourceType == SkillBase.ResourceType.Mana)
                return playerBehavior.CanUseSkill(0, skill.resourceCost);
            else
                return playerBehavior.CanUseSkill(skill.resourceCost, 0);
        }

        protected override void OnBeforeExecute(int index, GameObject instigator)
        {
            _currentExecutingSlot = index;
            var skill = GetSkill(index);
            if (skill == null) return;

            // Trừ tài nguyên dựa trên ResourceType
            if (skill.resourceType == SkillBase.ResourceType.Mana)
                playerBehavior?.ConsumeSkillResources(0, skill.resourceCost);
            else
                playerBehavior?.ConsumeSkillResources(skill.resourceCost, 0);

            // Trigger animation
            if (playerBehavior?.playerAnim != null)
            {
                playerBehavior.playerAnim.TriggerSkillAnimation(skill.animationTrigger, index);
            }

            // Lock movement nếu skill yêu cầu
            if (!skill.allowMovementDuringSkill)
            {
                if (PlayerController.Instance != null)
                    PlayerController.Instance.canMove = false;
            }
        }

        protected override void OnAfterExecute(int index, GameObject instigator)
        {
            UnlockMovement();
        }

        // ── Private ────────────────────────────────────────────────────────

        private void UnlockMovement()
        {
            if (PlayerController.Instance != null)
                PlayerController.Instance.canMove = true;
        }
    }
}

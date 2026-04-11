using UnityEngine;
using Script.Game.Player;
using Script.Game.PlayerCharacter.Skills;
using Script.Controller.PlayerController;
using Script.Save_Controller;

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
        /// Sau khi bơm snapshot từ save: tránh kẹt movement / slot executing từ trạng thái đã chuẩn hóa.
        /// </summary>
        public override void ApplySaveSnapshot(SkillManagerSaveData data, GameObject owner)
        {
            base.ApplySaveSnapshot(data, owner);
            _currentExecutingSlot = -1;
            UnlockMovement();
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
        /// Gọi từ Animation Event tại frame gây damage. Giữ nguyên chữ ký cũ để không cần sửa Animator.
        /// Cộng thêm bonus damage từ player.status.damage trước khi OnHit().
        /// </summary>
        public void OnSkillAnimationDamageFrame(int slotIndex)
        {
            // Inject bonus damage từ player stats vào skill trước khi gọi OnHit
            if (playerBehavior != null)
            {
                var skill = GetSkill(slotIndex);
                if (skill != null)
                    skill.SetBonusDamage(playerBehavior.status.damage);
            }
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
            bool canAfford = playerBehavior.CanAfford(skill.resourceType, skill.resourceCost);
            if (!canAfford)
            {
                // Trigger UI effect (shake, flash) trực tiếp qua ResourceStat
                if (skill.resourceType == ResourceType.Mana)
                    playerBehavior.status.Mana.TriggerUI();
                else if (skill.resourceType == ResourceType.Health)
                    playerBehavior.status.HP.TriggerUI();
            }
            return canAfford;
        }

        protected override void OnBeforeExecute(int index, GameObject instigator)
        {
            _currentExecutingSlot = index;
            var skill = GetSkill(index);
            if (skill == null) return;

            // Trừ tài nguyên (Tự động theo ResourceType)
            playerBehavior?.ConsumeResource(skill.resourceType, skill.resourceCost);

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

        protected void UnlockMovement()
        {
            if (PlayerController.Instance != null)
                PlayerController.Instance.canMove = true;
        }
    }
}

using UnityEngine;

namespace Script.Game.PlayerCharacter.Skills
{
    /// <summary>
    /// Base class quản lý skill slot, cooldown và trạng thái.
    /// Dùng được cho Player, NPC, Enemy — KHÔNG chứa logic Player-specific hay UI-specific.
    /// </summary>
    public class SkillManager : MonoBehaviour
    {
        // ── Nested Types ──────────────────────────────────────────────────

        public enum SkillState
        {
            Ready,      // Skill sẵn sàng để sử dụng
            Executing,  // Skill đang thực thi animation
            Cooling     // Skill đang trong cooldown
        }

        [System.Serializable]
        public class SkillSlot
        {
            public SkillBase skill;
            public SkillState state = SkillState.Ready;
            public float cooldownTimer = 0f;
        }

        // ── Fields ────────────────────────────────────────────────────────

        [Header("Skill Slots")]
        [SerializeField] protected SkillSlot[] skillSlots = new SkillSlot[0];

        protected Transform _skillContainer;

        // ── Public API ────────────────────────────────────────────────────

        public int SlotCount => skillSlots.Length;

        public SkillState GetState(int index)
            => IsValid(index) ? skillSlots[index].state : SkillState.Ready;

        public float GetCooldownRemaining(int index)
            => IsValid(index) ? skillSlots[index].cooldownTimer : 0f;

        public float GetCooldownTotal(int index)
            => IsValid(index) && skillSlots[index].skill != null
                ? skillSlots[index].skill.cooldown : 1f;

        public SkillBase GetSkill(int index)
            => IsValid(index) ? skillSlots[index].skill : null;

        public bool IsReady(int index)
            => IsValid(index) && skillSlots[index].state == SkillState.Ready;

        /// <summary>
        /// Trang bị một kỹ năng mới vào slot chỉ định. 
        /// Tự động reset trạng thái về Ready và xóa cooldown.
        /// </summary>
        public virtual void EquipSkill(int index, SkillBase newSkill)
        {
            if (!IsValid(index)) return;

            skillSlots[index].skill = newSkill;
            skillSlots[index].state = SkillState.Ready;
            skillSlots[index].cooldownTimer = 0f;

            if (newSkill != null && _skillContainer != null)
            {
                newSkill.Initialize(_skillContainer);
            }
            
            Debug.Log($"[SkillManager] Equipped skill '{(newSkill != null ? newSkill.skillName : "None")}' to slot {index}");
        }

        // ── Execute ────────────────────────────────────────────────────────

        /// <summary>
        /// Gọi bởi PlayerController (từ UI) hoặc StateMachine (từ AI).
        /// Trả về true nếu skill được kích hoạt thành công.
        /// </summary>
        public bool ExecuteSkill(int index, GameObject instigator)
        {
            if (!IsValid(index)) return false;

            var slot = skillSlots[index];

            if (slot.skill == null)
            {
                Debug.LogError($"[SkillManager] No skill assigned to slot {index}");
                return false;
            }

            if (slot.state != SkillState.Ready)
            {
                Debug.Log($"[SkillManager] Skill '{slot.skill.skillName}' is not ready. State: {slot.state}");
                return false;
            }

            if (!CanAfford(slot.skill, instigator)) return false;

            // --- Execute ---
            OnBeforeExecute(index, instigator);
            slot.state = SkillState.Executing;
            slot.skill.OnStart(instigator);

            Debug.Log($"[SkillManager] Executed skill '{slot.skill.skillName}' at slot {index}");

            // Nếu skill không dùng animation, kết thúc ngay lập tức để vào Cooldown
            if (!slot.skill.useAnimation)
            {
                OnSkillAnimationEnd(index, instigator);
            }

            return true;
        }

        /// <summary>
        /// Gọi từ Animation Event tại frame gây damage.
        /// </summary>
        public void OnSkillAnimationDamageFrame(int index, GameObject instigator)
        {
            if (!IsValid(index)) return;

            var slot = skillSlots[index];
            if (slot.skill != null && slot.state == SkillState.Executing)
            {
                slot.skill.OnHit(instigator);
            }
        }

        /// <summary>
        /// Gọi từ Animation Event khi animation kết thúc.
        /// </summary>
        public void OnSkillAnimationEnd(int index, GameObject instigator)
        {
            if (!IsValid(index)) return;

            var slot = skillSlots[index];
            if (slot.skill == null) return;

            slot.skill.OnEnd(instigator);

            OnAfterExecute(index, instigator);

            if (slot.skill.cooldown > 0f)
            {
                slot.state = SkillState.Cooling;
                slot.cooldownTimer = slot.skill.cooldown;
                Debug.Log($"[SkillManager] Skill '{slot.skill.skillName}' entered cooldown: {slot.cooldownTimer}s");
            }
            else
            {
                slot.state = SkillState.Ready;
            }
        }

        // ── Update ─────────────────────────────────────────────────────────

        protected virtual void Update()
        {
            for (int i = 0; i < skillSlots.Length; i++)
            {
                var slot = skillSlots[i];
                if (slot == null || slot.state != SkillState.Cooling) continue;

                slot.cooldownTimer -= Time.deltaTime;
                if (slot.cooldownTimer <= 0f)
                {
                    slot.cooldownTimer = 0f;
                    slot.state = SkillState.Ready;
                }
            }
        }

        // ── Virtual Hooks (override trong subclass) ───────────────────────

        /// <summary>
        /// Kiểm tra tài nguyên trước khi thực thi skill.
        /// Mặc định: luôn cho phép (dùng cho NPC/Enemy).
        /// </summary>
        protected virtual bool CanAfford(SkillBase skill, GameObject instigator) => true;

        /// <summary>
        /// Gọi ngay trước khi skill bắt đầu thực thi. dành cho subclass (vd: lock movement).
        /// </summary>
        protected virtual void OnBeforeExecute(int index, GameObject instigator) { }

        /// <summary>
        /// Gọi ngay sau khi animation kết thúc, trước khi cooldown bắt đầu. dành cho subclass (vd: unlock movement).
        /// </summary>
        protected virtual void OnAfterExecute(int index, GameObject instigator) { }

        // ── Initialization ─────────────────────────────────────────────────

        protected virtual void Awake()
        {
            if (skillSlots == null || skillSlots.Length == 0)
            {
                skillSlots = new SkillSlot[0];
            }

            for (int i = 0; i < skillSlots.Length; i++)
            {
                if (skillSlots[i] == null)
                    skillSlots[i] = new SkillSlot();
            }
        }

        /// <summary>
        /// Gọi sau khi Awake để spawn hitbox prefab, v.v.
        /// Override trong subclass nếu cần khởi tạo đặc thù.
        /// </summary>
        protected virtual void InitializeSkills(Transform container)
        {
            _skillContainer = container;

            for (int i = 0; i < skillSlots.Length; i++)
            {
                if (skillSlots[i] != null && skillSlots[i].skill != null)
                {
                    skillSlots[i].skill.Initialize(container);
                    skillSlots[i].state = SkillState.Ready;
                    skillSlots[i].cooldownTimer = 0f;
                    Debug.Log($"[SkillManager] Initialized slot {i}: {skillSlots[i].skill.skillName}");
                }
            }
        }

        // ── Private Helpers ────────────────────────────────────────────────

        private bool IsValid(int index)
            => index >= 0 && index < skillSlots.Length && skillSlots[index] != null;
    }
}

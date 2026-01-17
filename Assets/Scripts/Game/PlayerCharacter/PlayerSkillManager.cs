using UnityEngine;
using Script.Game.PlayerCharacter.Skills;
using Script.Controller.PlayerController;
using Art.Resources.Character.PlayerCharactor.Spear_Man;

using Script.Game.Player; // NEW

namespace Script.Game.PlayerCharacter
{
    /// <summary>
    /// Quản lý tất cả skills của player.
    /// Xử lý state machine, resource checking, cooldown, và execution flow.
    /// </summary>
    public class PlayerSkillManager : MonoBehaviour
    {
        // === NESTED TYPES ===
        
        /// <summary>
        /// State của skill: Ready (có thể dùng), Executing (đang chạy), Cooling (cooldown)
        /// </summary>
        private enum SkillState
        {
            Ready,      // Skill sẵn sàng để sử dụng
            Executing,  // Skill đang thực thi animation
            Cooling     // Skill đang trong cooldown
        }
        
        /// <summary>
        /// Container cho mỗi skill slot
        /// </summary>
        [System.Serializable]
        private class SkillSlot
        {
            public SkillBase skill;                     // Kéo thả Skill Asset (SO) vào đây
            public SkillState state = SkillState.Ready; // State hiện tại
            public float cooldownTimer = 0f;            // Runtime
        }
        
        // === FIELDS ===
        
        [Header("Skill Slots")]
        [Tooltip("Mảng chứa 3 skill slots (0, 1, 2) tương ứng với 3 buttons")]
        [SerializeField] private SkillSlot[] skillSlots = new SkillSlot[3];
        
        [Header("References")]
        [Header("References")]
        [Tooltip("Reference đến PlayerBehavior (auto-assigned)")]
        public PlayerBehavior playerBehavior;
        

        
        private int currentExecutingSlot = -1; // Track slot nào đang execute
        
        // === UNITY LIFECYCLE ===
        
        private void Awake()
        {
            playerBehavior = GetComponent<PlayerBehavior>();
            if (playerBehavior == null)
            {
                Debug.LogError("[PlayerSkillManager] PlayerBehavior not found!");
            }
            
            // Initialize skill slots array
            if (skillSlots == null || skillSlots.Length == 0)
            {
                skillSlots = new SkillSlot[3];
            }
            
            // Initialize each slot
            for (int i = 0; i < skillSlots.Length; i++)
            {
                if (skillSlots[i] == null)
                {
                    skillSlots[i] = new SkillSlot();
                }
            }
            
            // Setup skills
            InitializeSkills();
        }
        
        private void Update()
        {
            // Update tất cả skill timers
            for (int i = 0; i < skillSlots.Length; i++)
            {
                if (skillSlots[i] == null || skillSlots[i].skill == null) continue;
                
                SkillSlot slot = skillSlots[i];
                
                switch (slot.state)
                {
                    case SkillState.Executing:
                        // Waiting for Animation Event OnEnd...
                        // Không làm gì cả, chỉ chờ
                        break;
                    
                    case SkillState.Cooling:
                        // Countdown cooldown timer
                        slot.cooldownTimer -= Time.deltaTime;
                        if (slot.cooldownTimer <= 0f)
                        {
                            // Transition về Ready state
                            slot.state = SkillState.Ready;
                            slot.cooldownTimer = 0f;
                        }
                        break;
                }
            }
        }
        
        // === SKILL INITIALIZATION ===
        
        /// <summary>
        /// Tự động khởi tạo skill ChopDown vào slot 0
        /// </summary>
        
                // === SKILL INITIALIZATION ===
        
        private void InitializeSkills()
        {
            // 1. Tìm Container trực tiếp bằng tên trong child objects
            Transform container = transform.Find("SkillContainer");
            
            // Nếu không tìm thấy thì tự tạo mới
            if (container == null)
            {
                GameObject containerObj = new GameObject("SkillContainer");
                containerObj.transform.SetParent(transform, false);
                containerObj.transform.localPosition = Vector3.zero;
                containerObj.transform.localScale = Vector3.one;
                container = containerObj.transform;
                
                Debug.Log("[PlayerSkillManager] Created SkillContainer automatically.");
            }

            // 2. Loop qua các slot và khởi tạo skill
            for (int i = 0; i < skillSlots.Length; i++)
            {
                if (skillSlots[i] != null && skillSlots[i].skill != null)
                {
                    // Quan trọng: SkillBase là SO, nên nó tồn tại xuyên suốt.
                    // Chúng ta nên tạo bản sao (Instantiate SO) để tránh share state giữa các player khác nhau?
                    // Nhưng ở đây Single Player nên dùng trực tiếp cũng tạm ổn.
                    // Tuy nhiên để an toàn, ta sẽ dùng trực tiếp method Initialize của nó.
                    
                    skillSlots[i].skill.Initialize(container);
                    
                    // Reset Runtime States
                    skillSlots[i].state = SkillState.Ready;
                    skillSlots[i].cooldownTimer = 0f;
                    
                    Debug.Log($"[PlayerSkillManager] Initialized skill slot {i}: {skillSlots[i].skill.skillName}");
                }
            }
        }

        
        // === PUBLIC API ===
        
        /// <summary>
        /// Execute skill tại slot index (được gọi từ PlayerGui)
        /// </summary>
        /// <param name="slotIndex">Index của skill slot (0-2)</param>
        /// <returns>True nếu skill được execute thành công</returns>
        public bool ExecuteSkill(int slotIndex)
        {
            Debug.Log($"[PlayerSkillManager] ExecuteSkill called with slotIndex: {slotIndex}");
            
            // Validate slot index
            if (slotIndex < 0 || slotIndex >= skillSlots.Length)
            {
                Debug.LogError($"[PlayerSkillManager] Invalid slot index: {slotIndex}");
                return false;
            }
            
            SkillSlot slot = skillSlots[slotIndex];
            Debug.Log($"[PlayerSkillManager] Slot retrieved. Skill: {(slot.skill != null ? slot.skill.skillName : "NULL")}");
            
            // Check if slot has skill assigned
            if (slot.skill == null)
            {
                Debug.LogError($"[PlayerSkillManager] No skill assigned to slot {slotIndex}");
                return false;
            }
            
            Debug.Log($"[PlayerSkillManager] Skill state: {slot.state}");
            
            // Check state
            if (slot.state != SkillState.Ready)
            {
                Debug.LogError($"[PlayerSkillManager] Skill '{slot.skill.skillName}' is not ready. Current state: {slot.state}");
                return false;
            }
            
            Debug.Log($"[PlayerSkillManager] Checking resources: Stamina={slot.skill.staminaCost}, Mana={slot.skill.manaCost}");
            
            // Check resources
            if (!playerBehavior.CanUseSkill(slot.skill.staminaCost, slot.skill.manaCost))
            {
                Debug.LogError($"[PlayerSkillManager] Not enough resources for skill '{slot.skill.skillName}'");
                return false;
            }
            
            Debug.Log("[PlayerSkillManager] All checks passed! Executing skill...");
            
            // === EXECUTE SKILL ===
            
            // Consume resources
            playerBehavior.ConsumeSkillResources(slot.skill.staminaCost, slot.skill.manaCost);
            
            // Update state
            slot.state = SkillState.Executing;
            currentExecutingSlot = slotIndex;
            
            // Lock movement if needed
            if (!slot.skill.allowMovementDuringSkill)
            {
                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.canMove = false;
                    Debug.Log("[PlayerSkillManager] Movement locked.");
                }
            }
            
            Debug.Log($"[PlayerSkillManager] Triggering animation: {slot.skill.animationTrigger}");
            
            // Trigger animation via PlayerBehavior
            if (playerBehavior != null && playerBehavior.playerAnim != null)
            {
                playerBehavior.playerAnim.TriggerSkillAnimation(slot.skill.animationTrigger, slotIndex);
            }
            else
            {
                Debug.LogError("[PlayerSkillManager] PlayerBehavior or AnimationController is null!");
            }
            
            // Call skill OnStart
            slot.skill.OnStart(gameObject);
            
            Debug.Log($"[PlayerSkillManager] Successfully executed skill: {slot.skill.skillName}");
            return true;
        }
        
        /// <summary>
        /// Được gọi từ Animation Event tại frame damage
        /// </summary>
        public void OnSkillAnimationDamageFrame(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= skillSlots.Length) return;
            
            SkillSlot slot = skillSlots[slotIndex];
            if (slot.skill != null && slot.state == SkillState.Executing)
            {
                slot.skill.OnHit(gameObject);
            }
        }
        
        /// <summary>
        /// Được gọi từ Animation Event khi animation kết thúc
        /// </summary>
        public void OnSkillAnimationEnd(int slotIndex)
        {
            Debug.Log($"[PlayerSkillManager] OnSkillAnimationEnd called for slot {slotIndex}");
            
            if (slotIndex < 0 || slotIndex >= skillSlots.Length)
            {
                Debug.LogError($"[PlayerSkillManager] Invalid slot index in OnSkillAnimationEnd: {slotIndex}");
                return;
            }
            
            SkillSlot slot = skillSlots[slotIndex];
            
            if (slot.skill == null)
            {
                Debug.LogError($"[PlayerSkillManager] Slot {slotIndex} has no skill!");
                return;
            }
            
            Debug.Log($"[PlayerSkillManager] Current slot state: {slot.state}");
            
            // Call skill OnEnd
            slot.skill.OnEnd(gameObject);
            Debug.Log($"[PlayerSkillManager] Called OnEnd for skill '{slot.skill.skillName}'");
            
            // Unlock movement
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.canMove = true;
                Debug.Log("[PlayerSkillManager] Movement UNLOCKED");
            }
            else
            {
                Debug.LogError("[PlayerSkillManager] PlayerController.Instance is NULL!");
            }
            
            // Transition sang Cooling state
            slot.state = SkillState.Cooling;
            slot.cooldownTimer = slot.skill.cooldown;
            Debug.Log($"[PlayerSkillManager] State changed to Cooling, cooldown={slot.cooldownTimer}s");
            
            currentExecutingSlot = -1;
            
            Debug.Log($"[PlayerSkillManager] Skill '{slot.skill.skillName}' animation ended - ALL CLEANUP DONE");
        }
    }
}

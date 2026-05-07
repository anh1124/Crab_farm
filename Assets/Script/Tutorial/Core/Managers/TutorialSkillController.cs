using UnityEngine;
using Script.Game.PlayerCharacter.Skills;
using Script.Controller.PlayerController;

namespace Script.Tutorial.Core.Managers
{
    /// <summary>
    /// Script hỗ trợ Tutorial điều khiển hệ thống Skill (Thêm/Xóa skill của Player).
    /// Tự động kết nối với PlayerController.Instance nếu không được gán thủ công.
    /// </summary>
    public class TutorialSkillController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Nếu để trống, script sẽ tự tìm PlayerController.Instance.SkillManager lúc runtime")]
        [SerializeField] private SkillManager playerSkillManager;

        /// <summary>
        /// Lấy SkillManager của player (ưu tiên gán tay, sau đó là Singleton)
        /// </summary>
        private SkillManager GetSkillManager()
        {
            if (playerSkillManager != null) return playerSkillManager;

            if (PlayerController.Instance != null)
            {
                return PlayerController.Instance.SkillManager;
            }

            Debug.LogError("[TutorialSkillController] Không tìm thấy SkillManager! Hãy đảm bảo Player đã được spawn.");
            return null;
        }

        /// <summary>
        /// Bật hoặc tắt toàn bộ hệ thống skill (component SkillManager).
        /// </summary>
        public void SetSkillManagerActive(bool isActive)
        {
            var sm = GetSkillManager();
            if (sm != null) sm.enabled = isActive;
        }

        /// <summary>
        /// Gán một skill vào slot chỉ định bằng tên của skill.
        /// </summary>
        public void AddSkillByName(string skillName, int slotIndex)
        {
            var skill = SkillLibrary.Instance?.GetSkillByName(skillName);
            if (skill != null)
            {
                AddSkillToPlayer(skill, slotIndex);
            }
            else
            {
                Debug.LogError($"[TutorialSkillController] Không tìm thấy skill tên '{skillName}' trong SkillLibrary!");
            }
        }

        /// <summary>
        /// Gán một skill vào slot chỉ định.
        /// </summary>
        public void AddSkillToPlayer(SkillBase skill, int slotIndex)
        {
            var sm = GetSkillManager();
            if (sm != null) sm.EquipSkill(slotIndex, skill);
        }

        /// <summary>
        /// Xóa skill tại slot chỉ định (gán về null).
        /// </summary>
        public void RemoveSkillFromPlayer(int slotIndex)
        {
            var sm = GetSkillManager();
            if (sm != null) sm.EquipSkill(slotIndex, null);
        }

        /// <summary>
        /// Xóa toàn bộ skill của player.
        /// </summary>
        public void ClearAllSkills()
        {
            var sm = GetSkillManager();
            if (sm == null) return;

            for (int i = 0; i < sm.SlotCount; i++)
            {
                sm.EquipSkill(i, null);
            }
        }
    }
}

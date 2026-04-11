using System.Collections.Generic;
using Script.Game.PlayerCharacter.Skills;
using UnityEngine;

namespace Script.Save_Controller
{
    /// <summary>
    /// Capture / restore runtime SkillManager vào khối JSON của entity.
    /// </summary>
    public static class SkillManagerSaveUtility
    {
        public static SkillManagerSaveData Capture(SkillManager sm)
        {
            if (sm == null) return null;

            var list = new List<SkillSlotSaveData>();
            for (int i = 0; i < sm.SlotCount; i++)
            {
                var skill = sm.GetSkill(i);
                list.Add(new SkillSlotSaveData
                {
                    skillName = skill != null ? skill.skillName : null,
                    state = (int)sm.GetState(i),
                    cooldownTimer = sm.GetCooldownRemaining(i)
                });
            }

            return new SkillManagerSaveData { slots = list };
        }

        public static SkillManagerSaveData CaptureFrom(MonoBehaviour host)
        {
            if (host == null) return null;
            return Capture(host.GetComponent<SkillManager>());
        }

        public static void Restore(SkillManager sm, SkillManagerSaveData data, GameObject owner)
        {
            if (sm == null || data == null || owner == null) return;
            sm.ApplySaveSnapshot(data, owner);
        }
    }
}

using System;
using System.Collections.Generic;

namespace Script.Save_Controller
{
    /// <summary>
    /// Dữ liệu serialize cho SkillManager — nằm trong cùng object JSON với ISaveable (field "skillManager").
    /// </summary>
    [Serializable]
    public class SkillManagerSaveData
    {
        public List<SkillSlotSaveData> slots = new List<SkillSlotSaveData>();
    }

    [Serializable]
    public class SkillSlotSaveData
    {
        public string skillName;
        public int state;
        public float cooldownTimer;
    }
}

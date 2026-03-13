using UnityEngine;
using System.Collections.Generic;

namespace Script.Game.PlayerCharacter.Skills
{
    /// <summary>
    /// Thư viện chứa toàn bộ các Skill có trong game. 
    /// Giúp quản lý tập trung và tra cứu nhanh để đổi skill.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillLibrary", menuName = "Skills/System/Library")]
    public class SkillLibrary : ScriptableObject
    {
        private static SkillLibrary _instance;
        public static SkillLibrary Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Thử tìm ở gốc folder Resources
                    _instance = Resources.Load<SkillLibrary>("SkillLibrary");
                    
                    // Nếu không thấy, thử tìm trong folder con "Data/" (như của bạn hiện tại)
                    if (_instance == null)
                        _instance = Resources.Load<SkillLibrary>("Data/SkillLibrary");
                    
                    if (_instance == null)
                        Debug.LogWarning("[SkillLibrary] Không tìm thấy file 'SkillLibrary.asset' trong bất kỳ folder Resources nào! (Đã thử tìm ở gốc và Data/)");
                }
                return _instance;
            }
        }

        [Header("General Library")]
        [Tooltip("Kéo tất cả các file .asset của skill vào đây")]
        [SerializeField] private List<SkillBase> allSkills = new List<SkillBase>();

        /// <summary>
        /// Tìm skill theo tên (skillName)
        /// </summary>
        public SkillBase GetSkillByName(string name)
        {
            return allSkills.Find(s => s != null && s.skillName == name);
        }

        /// <summary>
        /// Lấy toàn bộ danh sách skill (để hiển thị bảng chọn skill chẳng hạn)
        /// </summary>
        public List<SkillBase> GetAllSkills() => allSkills;

        #if UNITY_EDITOR
        /// <summary>
        /// Tự động tìm tất cả skill trong project và add vào (Dùng trong Editor cho nhanh)
        /// </summary>
        [ContextMenu("Auto Populate Skills")]
        private void AutoPopulate()
        {
            allSkills.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SkillBase");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                SkillBase skill = UnityEditor.AssetDatabase.LoadAssetAtPath<SkillBase>(path);
                if (skill != null) allSkills.Add(skill);
            }
            UnityEditor.EditorUtility.SetDirty(this);
            Debug.Log($"[SkillLibrary] Đã tự động cập nhật {allSkills.Count} skills.");
        }
        #endif
    }
}

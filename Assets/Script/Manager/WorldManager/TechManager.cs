using UnityEngine;
using Script.Save_Controller;

namespace Script.Manager.WorldManager
{
    /// <summary>
    /// TechManager
    /// - Quản lý trạng thái các công nghệ (tech) của thế giới game: FireArrow, Penetrating, Forge, Adhesive.
    /// - Trước đây các hàm này nằm trong PrefabData; tách ra để PrefabData chỉ còn trách nhiệm lưu trữ prefabWall.
    /// Cách dùng:
    /// - Đặt TechManager vào scene gameplay.
    /// - Gọi: TechManager.Instance.SetFireArrowTech(true); hoặc UpdateTechStatus(...)
    /// - Dữ liệu sẽ được ghi vào WorldFileManager.Instance.worldInfo và lưu bằng SaveTechStatus.
    /// </summary>
    public class TechManager : MonoBehaviour
    {
        public static TechManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance == null) Instance = this; else Destroy(gameObject);
        }

        public void UpdateTechStatus(bool fireArrow, bool penetrating, bool forge, bool adhesive)
        {
            if (WorldFileManager.Instance != null)
            {
                WorldFileManager.Instance.SaveTechStatus(fireArrow, penetrating, forge, adhesive);
            }
        }

        public void SetFireArrowTech(bool value)
        {
            if (WorldFileManager.Instance == null) return;
            WorldFileManager.Instance.worldInfo.TechFireArrow = value;
            WorldFileManager.Instance.SaveTechStatus(value,
                WorldFileManager.Instance.worldInfo.TechPenetrating,
                WorldFileManager.Instance.worldInfo.TechForge,
                WorldFileManager.Instance.worldInfo.TechAdhesive);
        }

        public void SetPenetratingTech(bool value)
        {
            if (WorldFileManager.Instance == null) return;
            WorldFileManager.Instance.worldInfo.TechPenetrating = value;
            WorldFileManager.Instance.SaveTechStatus(
                WorldFileManager.Instance.worldInfo.TechFireArrow,
                value,
                WorldFileManager.Instance.worldInfo.TechForge,
                WorldFileManager.Instance.worldInfo.TechAdhesive);
        }

        public void SetForgeTech(bool value)
        {
            if (WorldFileManager.Instance == null) return;
            WorldFileManager.Instance.worldInfo.TechForge = value;
            WorldFileManager.Instance.SaveTechStatus(
                WorldFileManager.Instance.worldInfo.TechFireArrow,
                WorldFileManager.Instance.worldInfo.TechPenetrating,
                value,
                WorldFileManager.Instance.worldInfo.TechAdhesive);
        }

        public void SetAdhesiveTech(bool value)
        {
            if (WorldFileManager.Instance == null) return;
            WorldFileManager.Instance.worldInfo.TechAdhesive = value;
            WorldFileManager.Instance.SaveTechStatus(
                WorldFileManager.Instance.worldInfo.TechFireArrow,
                WorldFileManager.Instance.worldInfo.TechPenetrating,
                WorldFileManager.Instance.worldInfo.TechForge,
                value);
        }
    }
}



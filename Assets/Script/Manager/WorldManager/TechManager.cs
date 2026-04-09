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
    public class TechManager : MonoBehaviour, ITechProvider
    {
        private WorldFileManager _worldFileManager;

        public void Setup(WorldFileManager worldFileManager)
        {
            _worldFileManager = worldFileManager;
        }

        private void Awake()
        {
            // Singleton removed
        }

        public void UpdateTechStatus(bool fireArrow, bool penetrating, bool forge, bool adhesive)
        {
            if (_worldFileManager != null)
            {
                _worldFileManager.SaveTechStatus(fireArrow, penetrating, forge, adhesive);
            }
        }

        public void SetFireArrowTech(bool value)
        {
            if (_worldFileManager == null) return;
            _worldFileManager.worldInfo.TechFireArrow = value;
            _worldFileManager.SaveTechStatus(value,
                _worldFileManager.worldInfo.TechPenetrating,
                _worldFileManager.worldInfo.TechForge,
                _worldFileManager.worldInfo.TechAdhesive);
        }

        public void SetPenetratingTech(bool value)
        {
            if (_worldFileManager == null) return;
            _worldFileManager.worldInfo.TechPenetrating = value;
            _worldFileManager.SaveTechStatus(
                _worldFileManager.worldInfo.TechFireArrow,
                value,
                _worldFileManager.worldInfo.TechForge,
                _worldFileManager.worldInfo.TechAdhesive);
        }

        public void SetForgeTech(bool value)
        {
            if (_worldFileManager == null) return;
            _worldFileManager.worldInfo.TechForge = value;
            _worldFileManager.SaveTechStatus(
                _worldFileManager.worldInfo.TechFireArrow,
                _worldFileManager.worldInfo.TechPenetrating,
                value,
                _worldFileManager.worldInfo.TechAdhesive);
        }

        public void SetAdhesiveTech(bool value)
        {
            if (_worldFileManager == null) return;
            _worldFileManager.worldInfo.TechAdhesive = value;
            _worldFileManager.SaveTechStatus(
                _worldFileManager.worldInfo.TechFireArrow,
                _worldFileManager.worldInfo.TechPenetrating,
                _worldFileManager.worldInfo.TechForge,
                value);
        }
    }
}



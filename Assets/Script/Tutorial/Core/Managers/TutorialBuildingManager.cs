using System.Collections.Generic;
using UnityEngine;
using Script.Manager.Building;

namespace Script.Tutorial.Core.Managers
{
    public class TutorialBuildingManager : MonoBehaviour
    {
        [Header("Custom Building List")]
        public List<StructureInfo> customAllowedStructures = new List<StructureInfo>();

        public void AddBuildingToTutorialBuildMenu(BuildingType allowedType)
        {
            // Kiểm tra xem công trình đã tồn tại trong list chưa để tránh add trùng
            if (!customAllowedStructures.Exists(s => s.buildingType == allowedType))
            {
                customAllowedStructures.Add(new StructureInfo()
                {
                    buildingType = allowedType,
                    maxQuantity = 1,
                    maxTier = 1
                });
                Debug.Log($"[TutorialBuildingManager] Đã THÊM {allowedType} vào danh sách build của Tutorial.");
            }
        }
        
        public void ClearTutorialBuildMenu()
        {
            customAllowedStructures.Clear();
            Debug.Log($"[TutorialBuildingManager] Đã XÓA danh sách build của Tutorial.");
        }
    }
}

using System.Collections.Generic;
using Script.Game.Structures.Wall_And_Stump;
using UnityEngine;

namespace Script.Entities.Structures.Wall_And_Stump.Wall
{
    [CreateAssetMenu(fileName = "WallDataList", menuName = "CrabFarm/Structure/Wall Data List", order = 0)]
    public class WallDataList : ScriptableObject
    {
        [Tooltip("Danh sách các cấp tường (WallTierData). Cấp thấp nhất ở đầu danh sách.")]
        public List<WallTierData> walls = new List<WallTierData>();
        
        /// Lấy data theo cấp (tier). Nếu không tồn tại thì trả về null.
        public WallTierData GetByTier(int tier)
        {
            return walls.Find(w => w.tier == tier);
        }
        /// Lấy data cấp kế tiếp (nâng cấp tường).
        public WallTierData GetNext(WallTierData current)
        {
            if (current == null || walls == null || walls.Count == 0)
                return null;

            int nextTier = current.tier + 1;
            WallTierData next = GetByTier(nextTier);

            // Nếu không có data cấp kế tiếp, trả về null
            return next != null ? next : null;
        }
        
        /// Lấy data cấp đầu tiên (cấp 1).cấp 0 không phải wall
        public WallTierData GetBaseTier()
        {
            return walls.Count > 1 ? walls[1] : null;
        }
    }
}
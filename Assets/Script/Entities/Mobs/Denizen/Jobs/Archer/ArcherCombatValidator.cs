using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Manager.WorldManager;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherCombatValidator : DenizenModule
    {
        public ArcherCombatValidator(DenizenContext context) : base(context) { }

        /// <summary>
        /// 4-zone model:
        /// [Bãi săn]     - ngoài wall xa nhất: không bắn
        /// [Chiến tuyến] - từ wall xa nhất vào đến wall-offset: kỷ luật (HoldWallState gate)
        /// [Nội thành]   - giữa 2 vùng wall-offset: bắn được, ignoreGuard tạm thời
        /// [Plaza]       - tâm thành: fallback cá nhân nếu cánh mình hết wall
        /// </summary>
        public bool CanAttackFromHere(ArcherJob job, float squadPadding = -1f)
        {
            // Bỏ qua kỷ luật vị trí nếu mục tiêu là săn thú hoặc phá cổng (không phải Enemy chính quy)
            if (Context.Status.currentTarget != null && !Context.Status.currentTarget.CompareTag(GameTags.Enemy))
                return true;

            float posX = Context.transform.position.x;

            // --- 1. Xác định số wall còn lại ---
            int defendableCount = 0;
            var wallManager = Script.Game.Structures.Wall_And_Stump.WallAndStumpManager.Instance;
            if (wallManager != null)
            {
                var flankWalls = Context.Status.isLeftFlank ? wallManager.leftWalls : wallManager.rightWalls;
                foreach (var w in flankWalls)
                    if (wallManager.IsWallDefendable(w)) defendableCount++;
            }

            // --- 2. Xác định các vạch ranh giới ---
            bool inPlaza = WorldUtility.IsInPlazaZone(posX);
            
            // [PADDING LOGIC]
            if (squadPadding < 0) squadPadding = Context.GuardManager != null ? Context.GuardManager.defaultSlotOffset : 3.0f;
            
            bool inKingdom = WorldUtility.IsInKingdomSafeZone(posX, 0f);
            bool inSafePosition = WorldUtility.IsInKingdomSafeZone(posX, squadPadding);

            // ZONE 1: LOẠI BỎ CÁC VÙNG NGUY HIỂM (NGOÀI TƯỜNG / CHƯA LÙI ĐỦ SÂU)
            if (!inKingdom && !inPlaza) return false;
            if (inKingdom && !inSafePosition && !inPlaza) return false;

            // ZONE 2: VÙNG AN TOÀN KỶ LUẬT (Đã đứng sau vạch Offset)
            if (inSafePosition)
            {
                // [NỘI THÀNH] Nếu đứng sâu trong thành cách Plaza < 13m -> Set ignore để combat liên tục
                if (Mathf.Abs(posX) < 13f && Context.Status.currentTarget != null) 
                    job.SetIgnoreGuard(true);
                
                return true;
            }

            // ZONE 3: FALLBACK PLAZA (Chưa tới slot nhưng đã chạm Plaza)
            if (inPlaza)
            {
                // Chỉ cho phép bắn ở Plaza nếu wall đã mất sạch hoặc đang ignoreGuard
                if (defendableCount == 0 || job.IgnoreGuardManagerCommand) return true;
                return false;
            }

            // ZONE 4: CHIẾN TUYẾN & NỘI THÀNH (Dự phòng)
            if (inSafePosition)
            {
                float distToPlaza = Mathf.Abs(posX);
                float plazaRadius = 13f; 
                
                if (distToPlaza < plazaRadius)
                {
                    if (Context.Status.currentTarget != null) job.SetIgnoreGuard(true);
                }
                return true; 
            }

            return false;
        }
    }
}

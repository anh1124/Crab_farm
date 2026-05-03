using UnityEngine;
using System.Collections.Generic;
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
            // --- KIỂM TRA TƯỜNG (WALLZONE) ---
            // Nếu đang đứng lọt trong tường -> Tuyệt đối không bắn (tránh lỗi mũi tên)
            if (IsInsideWall(Context.Status.debugDenizen))
                return false;

            // --- ƯU TIÊN SỐ 1: ĐANG GIỮ VỊ TRÍ PHÒNG THỦ ---
            // Nếu Archer đã được lệnh gác thành (HoldWallState), luôn cho phép bắn.
            if (job.CurrentState is ArcherHoldWallState)
                return true;

            // ZONE 0: KIỂM TRA BÃI SĂN (HUNTING ZONE)
            // Nếu là mục tiêu săn bắn (Animal/Portal) hoặc đang trong Hunting State
            if (Context.Status.currentTarget != null)
            {
                bool isHuntingTarget = Context.Status.currentTarget.CompareTag(GameTags.Animal) || Context.Status.currentTarget.CompareTag(GameTags.EnemyPortal);
                if (isHuntingTarget)
                {
                    // Cho phép bắn nếu đang đứng trong bãi săn hợp lệ (ngoài vương quốc)
                    bool inKingdomSafe = WorldUtility.IsInKingdomSafeZone(Context.transform.position.x, 0f);
                    if (!inKingdomSafe) return true;
                }
                
                // --- 0. Bỏ qua kỷ luật vị trí nếu mục tiêu KHÔNG phải Enemy chính quy ---
                if (!Context.Status.currentTarget.CompareTag(GameTags.Enemy))
                    return true;
            }

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

        public bool IsInsideWall(bool logDebug = false)
        {
            Collider2D myCol = Context.GetComponent<Collider2D>();
            if (myCol == null)
            {
                if (logDebug) Debug.LogWarning($"[ArcherWallCheck] {Context.gameObject.name} does not have a Collider2D!");
                return false; 
            }

            int currentLayer = Context.gameObject.layer;
            string currentLayerName = LayerMask.LayerToName(currentLayer);

            // Sử dụng trực tiếp Collider của Archer để kiểm tra va chạm/overlap
            List<Collider2D> results = new List<Collider2D>();
            ContactFilter2D filter = new ContactFilter2D().NoFilter();
            myCol.OverlapCollider(filter, results);

            bool insideWall = false;

            System.Text.StringBuilder sb = null;
            if (logDebug)
            {
                sb = new System.Text.StringBuilder();
                sb.AppendLine($"<color=white>[ArcherWallCheck]</color> Checking {Context.gameObject.name} using its own collider | Layer: {currentLayerName}");
                sb.AppendLine($"Colliders overlapping: {results.Count}");
            }

            for (int i = 0; i < results.Count; i++)
            {
                var col = results[i];
                if (col == null || col.gameObject == Context.gameObject) continue;

                bool isWall = col.CompareTag(GameTags.Wall);
                if (isWall) insideWall = true;

                if (logDebug)
                {
                    int colLayer = col.gameObject.layer;
                    bool interacts = !Physics2D.GetIgnoreLayerCollision(currentLayer, colLayer);
                    sb.AppendLine($" - Hit[{i}]: {col.gameObject.name} | Tag: {col.tag} | Layer: {LayerMask.LayerToName(colLayer)} | Trigger: {col.isTrigger} | Interacts: {interacts}");
                    if (isWall) sb.AppendLine("   <color=red>=> FOUND WALL TAG!</color>");
                }
                
                if (!logDebug && insideWall) return true;
            }

            if (logDebug)
            {
                sb.AppendLine($"=> Result: <color={(insideWall ? "red" : "green")}>InsideWall = {insideWall}</color>");
                Debug.Log(sb.ToString());
            }

            return insideWall;
        }
    }
}

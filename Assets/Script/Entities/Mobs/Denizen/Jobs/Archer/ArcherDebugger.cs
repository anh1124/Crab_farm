using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    /// <summary>
    /// ArcherDebugger: Tập trung toàn bộ logic debug của Archer.
    /// Tách ra khỏi ArcherJob để giảm kích thước lớp điều phối chính.
    /// </summary>
    public class ArcherDebugger : DenizenModule
    {
        private ArcherJob _job;
        private ArcherCombatValidator _validator;

        public ArcherDebugger(DenizenContext context, ArcherJob job, ArcherCombatValidator validator) : base(context)
        {
            _job = job;
            _validator = validator;
        }

        public void RunCombatDebug()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"===== ARCHER COMBAT DEBUG: {Context.GameObject?.name} =====");
            sb.AppendLine($"- Trạng thái hiện tại: {_job.CurrentState?.GetType().Name}");
            sb.AppendLine($"- Tọa độ X: {Context.transform.position.x:F2}");
            sb.AppendLine($"- Mục tiêu đang nhắm: {(Context.Status.currentTarget != null ? Context.Status.currentTarget.name : "NULL")}");

            // Arrow Pool
            sb.AppendLine("\n[Arrow Pool Status]:");
            if (Context.ArcherShooting != null)
            {
                var pool = Object.FindObjectOfType<Script.Game.Combat.ArrowPool>();
                if (pool != null)
                {
                    sb.AppendLine("- ArcherShooting: Đã thiết lập");
                    sb.AppendLine("- ArrowPool: Tìm thấy");
                    if (pool.IsPoolNearExhausted())
                        sb.AppendLine($"- <color=red>CẢNH BÁO: Pool gần hết arrows!</color> Active: {pool.GetActiveArrowCount()}");
                    else
                        sb.AppendLine($"- Pool còn arrows: {pool.GetActiveArrowCount()} đang sử dụng");
                }
                else sb.AppendLine("- <color=red>ArrowPool: KHÔNG TÌM THẤY!</color>");
            }
            else sb.AppendLine("- <color=red>ArcherShooting: NULL! Kiểm tra Setup()</color>");

            // Detector
            if (Context.CircleDetect != null)
            {
                var detected = Context.CircleDetect.GetDetectedObjects();
                sb.AppendLine($"\n[Danh sách vật thể quét được] ({detected.Count} đối tượng):");
                for (int i = 0; i < detected.Count; i++)
                {
                    if (detected[i] == null) continue;
                    float d = Vector2.Distance(Context.transform.position, detected[i].transform.position);
                    sb.AppendLine($" [{i}] {detected[i].name} (Tag: {detected[i].tag}) - Cách: {d:F2}m");
                }
            }

            // Zones
            sb.AppendLine("\n[Kiểm tra vị trí và vùng hoạt động]:");
            bool inPlaza   = WorldUtility.IsInPlazaZone(Context.transform.position.x);
            bool inKingdom = WorldUtility.IsInKingdomSafeZone(Context.transform.position.x, 0f);
            sb.AppendLine($"- Trong nội thành (Kingdom): {inKingdom}");
            sb.AppendLine($"- Trong trung tâm (Plaza): {inPlaza}");

            if (Context.GuardManager != null)
            {
                var bounds = WorldUtility.GetHuntingBounds(Context.GuardManager, Context.Status.isLeftFlank);
                bool inHunt = WorldUtility.IsInHuntingZone(Context.transform.position.x, Context.GuardManager, Context.Status.isLeftFlank);
                sb.AppendLine($"- Trong bãi săn (Hunting Zone): {inHunt} (Vùng: {bounds.start:F1} -> {bounds.end:F1})");
            }

            // Wall
            bool isInsideWall = _validator.IsInsideWall(false);
            sb.AppendLine($"- Đang đứng trong tường (WallZone): <color={(isInsideWall ? "red" : "white")}>{isInsideWall}</color>");

            int defendableCount = 0;
            var wallManager = Script.Game.Structures.Wall_And_Stump.WallAndStumpManager.Instance;
            if (wallManager != null)
            {
                var flankWalls = Context.Status.isLeftFlank ? wallManager.leftWalls : wallManager.rightWalls;
                foreach (var wall in flankWalls)
                    if (wallManager.IsWallDefendable(wall)) defendableCount++;
            }
            sb.AppendLine($"- Số tường phòng thủ được (Cánh này): {defendableCount}");

            // Skill Manager
            sb.AppendLine("\n[Skill Manager Status]:");
            if (Context.SkillManager != null)
            {
                bool isReady = Context.SkillManager.IsReady(0);
                var state = Context.SkillManager.GetState(0);
                float cd = Context.SkillManager.GetCooldownRemaining(0);
                var skill = Context.SkillManager.GetSkill(0);

                sb.AppendLine($"- Slot 0 (Attack): {(skill != null ? skill.skillName : "NULL")}");
                sb.AppendLine($"- Trạng thái: {state} (Ready: {isReady})");
                if (cd > 0) sb.AppendLine($"- Cooldown còn lại: {cd:F2}s");
            }
            else sb.AppendLine("- <color=red>SkillManager: NULL!</color>");

            // Attack validation
            float padding   = Context.GuardManager?.Squad != null ? Context.GuardManager.defaultSlotOffset : 7.0f;
            bool isSafePos  = WorldUtility.IsInKingdomSafeZone(Context.transform.position.x, padding);
            bool canAttack  = _job.CanAttackFromHere();
            bool isSkillReady = Context.SkillManager != null && Context.SkillManager.IsReady(0);

            sb.AppendLine($"\n=> KẾT QUẢ: {(canAttack && isSkillReady ? "<color=green>ĐỦ ĐIỀU KIỆN BẮN</color>" : "<color=red>KHÔNG ĐỦ ĐIỀU KIỆN BẮN</color>")}");

            if (!canAttack || !isSkillReady)
            {
                sb.AppendLine("LÝ DO:");
                if (!isSkillReady)
                {
                    if (Context.SkillManager == null) sb.AppendLine("+ Thiếu SkillManager.");
                    else if (Context.SkillManager.GetSkill(0) == null) sb.AppendLine("+ Slot 0 chưa gán Skill.");
                    else sb.AppendLine($"+ Skill đang trong trạng thái: {Context.SkillManager.GetState(0)} (Cooldown?)");
                }

                if (!canAttack)
                {
                    if (_job.CurrentState is ArcherHuntingState)
                        sb.AppendLine("+ Đang ở trạng thái Săn Bắt nhưng lại đứng trong Vương Quốc (SafeZone). Yêu cầu ra ngoài tường.");
                    else if (!inKingdom && !inPlaza)
                        sb.AppendLine("+ Archer đang đứng QUÁ XA bảo vệ của tường (ngoài vương quốc) khi đang cần phòng thủ.");
                    else if (inKingdom && !isSafePos && !inPlaza)
                        sb.AppendLine($"+ Archer chưa lùi đủ sâu đằng sau tường (Padding={padding}).");
                    else if (inPlaza && defendableCount == 1 && !isSafePos)
                        sb.AppendLine("+ Đang ở Plaza nhưng chỉ còn 1 tường cuối, yêu cầu đứng lùi sâu hơn để bảo toàn mạng sống.");
                    else
                        sb.AppendLine("+ Không xác định được vị trí an toàn hợp lệ.");
                }
            }

            Debug.Log(sb.ToString());
        }
    }
}

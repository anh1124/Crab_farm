using UnityEngine;
using Script.Game.Enemy.Core;
using Script.Game.PlayerCharacter.Skills;
using Script.Utils;

namespace Script.Game.Enemy.Skills
{
    /// <summary>
    /// Skill dùng chung cho AI Enemy. 
    /// Tích hợp với SkillBase và gọi Animator Trigger được khai báo trong Asset.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemySkill", menuName = "Enemy/Skill/GenericAttack")]
    public class EnemyAttackSkill : SkillBase
    {
        // Ghi đè phương thức khởi động Skill
        public override void OnStart(GameObject owner, SkillManager manager)
        {
            // Kiểm tra field animationTrigger có sẵn trong SkillBase chưa
            string trigger = string.IsNullOrEmpty(animationTrigger) ? "attack" : animationTrigger;

            // Ưu tiên gọi qua AI Controller để quản lý Parameter an toàn
            var controller = owner.GetComponent<EnemyAIController>();
            if (controller != null)
            {
                if (controller.isDebug) Debug.Log($"<color=cyan>[EnemyAttackSkill]</color> Asset '{this.name}' đang yêu cầu Controller kích hoạt: <b>{trigger}</b>");
                controller.SetTriggerSafely(trigger);
            }
            else
            {
                // Fallback nếu không dùng Controller
                var animator = owner.GetComponent<Animator>();
                if (animator != null)
                {
                    Debug.Log($"<color=cyan>[EnemyAttackSkill]</color> Không có Controller, gọi trực tiếp Animator.SetTrigger('<b>{trigger}</b>') cho {owner.name}");
                    animator.SetTrigger(trigger);
                }
            }
        }

        // Logic gây sát thương (thường do Animation Event "AnimEvent_EnableShockCollider" hoặc tương tự gọi)
        // Nếu dùng hệ thống Collider trên GameObject con như ShieldBot, hàm này có thể để trống
        public override void OnHit(GameObject owner, SkillManager manager)
        {
            // 1. Tìm Controller để lấy dữ liệu về Hitbox (Collider)
            var controller = owner.GetComponent<EnemyAIController>();
            if (controller == null) return;

            Collider2D hitbox = null;

            // 2. Đặc thù cho ShieldBot: Lấy shockAttackCollider
            if (controller is ShieldBotAIController shieldBot)
            {
                var colliderObj = shieldBot.ShockAttackCollider;
                if (colliderObj != null)
                {
                    hitbox = colliderObj.GetComponent<Collider2D>();
                }
            }

            // 3. Nếu tìm thấy Hitbox, kích hoạt xử lý qua HitboxController
            // HitboxController sẽ tự động log ra các đối tượng trúng đòn (HIT) vào Console
            if (hitbox != null)
            {
                var enemyStatus = owner.GetComponent<Script.Game.Enemy.EnemyStatus>();
                bool debugLogs = (enemyStatus != null && enemyStatus.debugEnemy) || controller.isDebug;
                if (debugLogs) Debug.Log($"<color=orange>[EnemyAttackSkill]</color> {owner.name} đang kích hoạt Hitbox: {hitbox.gameObject.name} với Damage: {damage}");
                HitboxController.ActivateHitbox(hitbox, damage, owner, true, debugLogs);
            }
            else
            {
                if (controller.isDebug) Debug.LogWarning($"<color=red>[EnemyAttackSkill]</color> {owner.name} không tìm thấy Hitbox để gây sát thương!");
            }
        }

        // Kết thúc Skill (được gọi bởi Animation Event OnEnd hoặc kết thúc đòn tấn công)
        public override void OnEnd(GameObject owner, SkillManager manager)
        {
            // Reset trạng thái thực thi nếu cần
        }
    }
}

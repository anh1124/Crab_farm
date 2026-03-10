using Script.Game.Enemy.Core;
using Script.Game.Enemy.States;
using Script.Game.Enemy.Variants.ShieldBot;
using UnityEngine;

namespace Script.Game.Enemy
{
    // anim:
    /*
    -death
    -hit
    -shield finished
    -shield prep
    -shield puulse
    -shock attack
    -walk
    */

    public class ShieldBotAIController : EnemyAIController
    {
        [Header("ShieldBot Settings")]
        [Tooltip("Kéo thả GameObject con chứa Collider sát thương của đòn Shock Attack vào đây")]
        [SerializeField] private GameObject shockAttackCollider;

        public GameObject ShockAttackCollider => shockAttackCollider;

        public override EnemyState GetMoveState()
        {
            return new ShieldBotAttackState(); // Chạy State riêng của ShieldBot
        }

        // ==========================================
        // ANIMATION EVENTS - Cần Add Event vào Animation
        // ==========================================

        // 1. Gọi hàm này vào frame bắt đầu vung búa để BẬT vùng damage child lên
        public void AnimEvent_EnableShockCollider()
        {
            if (shockAttackCollider != null)
            {
                shockAttackCollider.SetActive(true);
            }
        }

        // 2. Gắn hàm này vào cái frame CUỐI CÙNG (hoặc frame đập nát) của animation "shock attack"
        // Gọi 1 phát là nó sẽ tự TÍNH SÁT THƯƠNG, TẮT CHILD COLLIDER, và hoàn thành đòn chém.
        public void AnimEvent_EndShockAttack()
        {
            if (_currentState is ShieldBotAttackState attackState)
            {
                // Áp dụng sát thương
                attackState.DealDamageFromEvent(_ctx);
                
                // Tắt child collider
                ForceDisableShockCollider();
                
                // Trả cờ tấn công về false, tiếp tục đi bộ
                attackState.EndAttackEvent(_ctx);
            }
        }

        // Tính năng an toàn: Gọi từ script AttackState.Exit() nếu AI tự dưng đổi mục tiêu hoặc dính án tử (DeadState)
        public void ForceDisableShockCollider()
        {
            if (shockAttackCollider != null)
            {
                shockAttackCollider.SetActive(false);
            }
        }
    }
}

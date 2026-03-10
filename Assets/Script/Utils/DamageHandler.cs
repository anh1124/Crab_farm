using UnityEngine;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;

namespace Script.Utils
{
    // Centralized damage resolver supporting optional coin shield
    public static class DamageHandler
    {
        // Returns true if damage was applied (or absorbed), false if target died
        public static bool ApplyDamage(GameObject target, int damage, GameObject instigator)
        {
            if (target == null) return true;

            // 0. KIỂM TRA FLAG CHẾT TRƯỚC: Bỏ qua hoàn toàn nếu nó đã là xác chết
            var dieInterface = target.GetComponent<IDie>();
            if (dieInterface != null && dieInterface.IsDead)
            {
                return true; 
            }

            // 1. Xử lý Tool Shield (Cung, Giáo, Kiếm... dùng làm khiên thay cho coin cuối cùng)
            var toolShield = target.GetComponent<IToolShield>();
            if (toolShield != null && toolShield.HasToolShield)
            {
                if (toolShield.ConsumeToolShield())
                {
                    Debug.Log($"[DamageHandler] Mục tiêu {target.name} đã rơi Vũ Khí để chặn sát thương cuối cùng!");
                    return true;
                }
            }

            // 1.5. Xử lý khiên thường (Coin Shield)
            var coinShield = target.GetComponent<ICoinShield>();
            
            if (coinShield != null)
            {
                Debug.Log($"[DamageHandler] Xử lý mục tiêu {target.name} (Có ICoinShield. HasShield = {coinShield.HasCoinShield})");
            }
            
            if (coinShield != null && coinShield.HasCoinShield)
            {
                if (coinShield.ConsumeCoinShield()) // Nếu có coinshield thì nó sẽ văng coin và chặn sát thương
                {
                    Debug.Log($"[DamageHandler] Mục tiêu {target.name} đã dùng Khiên Xu để đỡ đòn!");
                    return true; // đã hấp thụ sát thương
                }
            }
            
            // 2. Xử lý sát thương máu thường
            var health = target.GetComponent<IHealth>();
            if (health != null)
            {
                if (damage > 0)
                {
                    health.TakeDamage(damage);
                }
                return health.CurrentHp > 0;
            }

            // Nếu không có Health nhưng có CoinShield (như Denizen mất khiên), coi như quá trình đánh hoàn tất
            if (coinShield != null) return true;

            // Báo lỗi nếu đánh trúng 1 cục đá vô tri vô giác không thuộc thể loại nào
            Debug.LogWarning($"[DamageHandler] Target {target.name} has no IHealth or ICoinShield interface.");
            return true;
        }
    }
}



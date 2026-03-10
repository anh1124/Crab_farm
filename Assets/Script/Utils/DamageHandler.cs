using UnityEngine;
using Script.Interfaces;

namespace Script.Utils
{
    /// <summary>
    /// Trung tâm điều phối sát thương.
    /// Bên tấn công chỉ cần gọi ApplyDamage() và không cần quan tâm
    /// đến cơ chế của bên nhận (Coin, Vũ khí, Máu...).
    /// </summary>
    public static class DamageHandler
    {
        /// <summary>
        /// Áp dụng sát thương lên mục tiêu.
        /// Yêu cầu mục tiêu phải implement IDamageable.
        /// </summary>
        /// <returns>True nếu đã giao sát thương thành công, False nếu mục tiêu không hợp lệ.</returns>
        public static bool ApplyDamage(GameObject target, int damage, GameObject instigator)
        {
            if (target == null) return false;

            // Kiểm tra cờ chết trước (tránh đánh vào xác chết)
            var dieInterface = target.GetComponent<IDie>();
            if (dieInterface != null && dieInterface.IsDead) return false;

            // Chỉ quan tâm một điều: Mục tiêu có thể nhận sát thương không?
            var damageable = target.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.ReceiveDamage(damage, instigator);
                return true;
            }

            // Fallback: Cảnh báo nếu tấn công đối tượng không thể nhận sát thương
            Debug.LogWarning($"[DamageHandler] {target.name} không có IDamageable. Không thể gây sát thương.");
            return false;
        }
    }
}

using UnityEngine;
using Script.Interfaces;
using Script.Audio;

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
        public static bool ApplyDamage(GameObject target, float damage, GameObject instigator)
        {
            if (target == null) return false;

            // 1. Resolve effective target (lấy parent 1 cấp )
            GameObject effectiveTarget = CombatUtility.ResolveTargetFromTrigger(target);
            if (effectiveTarget == null) return false;

            // 2. Kiểm tra cờ chết trước (tránh đánh vào xác chết)
            var dieInterface = effectiveTarget.GetComponent<IDie>();
            if (dieInterface != null && dieInterface.IsDead) return false;

            // Generic route: chỉ làm việc với IDamageable trên object đã resolve từ collider.
            bool damaged = false;
            var damageables = effectiveTarget.GetComponents<IDamageable>();
            foreach (var damageable in damageables)
            {
                var mb = damageable as MonoBehaviour;
                if (mb != null && mb.isActiveAndEnabled)
                {
                    damageable.ReceiveDamage(damage, instigator);
                    damaged = true;
                }
                else if (mb == null)
                {
                    damageable.ReceiveDamage(damage, instigator);
                    damaged = true;
                }
            }

            if (damaged)
            {
                // Phát âm thanh Hit
                // Ưu tiên dùng AudioEmitter trên entity nếu có
                var emitter = effectiveTarget.GetComponent<Audio.AudioEmitter>();
                if (emitter != null)
                {
                    emitter.PlaySound("Hit");
                }
                else if (AudioManager.Instance != null)
                {
                    // Fallback: dùng AudioManager nếu không có AudioEmitter
                    AudioManager.Instance.PlaySFXByKey("Hit", effectiveTarget.transform.position);
                }
                return true;
            }

            // Không in log nếu trúng Tilemap (tránh spam khi arrow bắn xuống đất)
            if (!effectiveTarget.name.Contains("Tilemap") && effectiveTarget.GetComponent<UnityEngine.Tilemaps.Tilemap>() == null)
            {
                Debug.LogWarning($"[DamageHandler] {effectiveTarget.name} (from {target.name}) không có IDamageable nào đang ACTIVE. Không thể gây sát thương.");
            }
            return false;
        }
    }
}

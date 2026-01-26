using UnityEngine;
using Script.Utils;

namespace Script.Utils
{
    /// <summary>
    /// Static utility để xử lý hitbox detection một cách tối ưu.
    /// Sử dụng NonAlloc OverlapCollider để tránh GC allocation.
    /// </summary>
    public static class HitboxController
    {
        // Static buffer để tái sử dụng, tránh tạo array mới mỗi lần
        private static readonly Collider2D[] buffer = new Collider2D[20];
        
        /// <summary>
        /// Kích hoạt hitbox, phát hiện va chạm và gây damage.
        /// Method này enable collider tạm thời, detect hits, rồi disable ngay.
        /// </summary>
        /// <param name="hitbox">Collider2D của skill</param>
        /// <param name="damage">Damage gây ra</param>
        /// <param name="instigator">GameObject gây damage (player)</param>
        /// <param name="damageAlly">Có cho phép damage đồng minh không</param>
        public static void ActivateHitbox(Collider2D hitbox, int damage, GameObject instigator, bool damageAlly)
        {
            if (hitbox == null)
            {
                Debug.LogError("[HitboxController] Hitbox collider is null!");
                return;
            }
            
            // CRITICAL: Enable GameObject trước (nếu bị tắt)
            GameObject hitboxObj = hitbox.gameObject;
            bool wasActive = hitboxObj.activeSelf;
            if (!wasActive)
            {
                hitboxObj.SetActive(true);
            }
            
            // Enable hitbox collider
            hitbox.enabled = true;
            
            // NonAlloc overlap detection
            ContactFilter2D filter = new ContactFilter2D().NoFilter();
            int hitCount = Physics2D.OverlapCollider(hitbox, filter, buffer);
            
            Debug.Log($"[HitboxController] HitCount: {hitCount}");
            for (int i = 0; i < hitCount; i++)
            {
                if (buffer[i] != null)
                {
                    Debug.Log($"[HitboxController] Hit[{i}]: {buffer[i].name} (Tag: {buffer[i].tag})");
                }
            }
            
            // Process damage thông qua CombatUtility (existing system)
            CombatUtility.ProcessAttackHits(instigator, buffer, hitCount, damage, damageAlly);
            
            // Disable hitbox ngay sau khi xử lý
            hitbox.enabled = false;
            
            // Disable GameObject nếu nó đang inactive trước đó
            if (!wasActive)
            {
                hitboxObj.SetActive(false);
            }
        }
    }
}

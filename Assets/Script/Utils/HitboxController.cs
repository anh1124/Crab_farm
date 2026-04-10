using UnityEngine;

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
        public static void ActivateHitbox(Collider2D hitbox, float damage, GameObject instigator, bool isEnemyAttack = false, bool debugLogs = false)
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
            
            // Cấu hình bộ lọc dựa trên Layer Collision Matrix
            ContactFilter2D filter = new ContactFilter2D();
            filter.useLayerMask = true;
            int hitboxLayer = hitbox.gameObject.layer;
            int collisionMask = Physics2D.GetLayerCollisionMask(hitboxLayer);
            filter.layerMask = collisionMask;
            filter.useTriggers = true; // Cho phép đánh trúng các NPC sử dụng Trigger Collider

            int hitCount = Physics2D.OverlapCollider(hitbox, filter, buffer);
            if (debugLogs)
            {
                string names = "";
                for (int i = 0; i < hitCount; i++)
                {
                    if (buffer[i] == null) continue;
                    if (names.Length > 0) names += ", ";
                    names += $"{buffer[i].name}(Tag:{buffer[i].tag})";
                }
                Debug.Log($"[HitboxController] {instigator.name} hitbox '{hitboxObj.name}' overlapped {hitCount} collider(s): [{names}]");
            }

            // Process damage thông qua CombatUtility (existing system)
            CombatUtility.ProcessAttackHits(instigator, buffer, hitCount, damage, isEnemyAttack, debugLogs);
            
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

using System.Collections.Generic;
using UnityEngine;
using Script.Data_Model;
using Script.Data_Model.Status;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using Script.Game.Structures.Wall_And_Stump;


namespace Script.Utils
{
    public static class CombatUtility
    {
        public static bool IsAlly(GameObject obj)
        {
            if (Script.Utils.TargetLists.AllyTags == null)
            {
                Debug.LogWarning("AllyTags là null. Không thể kiểm tra đồng minh.");
                return false;
            }
            return Script.Utils.TargetLists.AllyTags.Contains(obj.tag);
        }

        /// <summary>
        /// Hàm tiện ích: Nếu chém trúng GameObject thuộc layer "TriggerLayer", tự động chuyển lên parent 1 cấp.
        /// Việc này giúp Combat quy về đối tượng Entity gốc để xử lý.
        /// </summary>
        public static GameObject ResolveTargetFromTrigger(GameObject hitObject)
        {
            if (hitObject == null) return null;

            // NẾU BẢN THÂN hitObject LÀ IDamageable, KHÔNG CẦN CHUYỂN LÊN PARENT.
            // (Ví dụ Archer tự có Collider isTrigger, không được nhảy nhầm lên DenizenContainer)
            if (hitObject.GetComponent<IDamageable>() != null)
            {
                return hitObject;
            }

            // 1. Kiểm tra bằng Tag "Trigger" (Được ưu tiên nhất)
            // LƯU Ý: Phải đảm bảo Tag "Trigger" đã được tạo trong Tag Manager của Unity để tránh log lỗi.
            if (hitObject.CompareTag(Script.Utils.GameTags.Trigger))
            {
                if (hitObject.transform.parent != null)
                    return hitObject.transform.parent.gameObject;
            }

            // 2. Kiểm tra Layer (Dự phòng)
            int triggerLayer = Script.Utils.GameLayers.TriggerLayerIndex;
            if (triggerLayer != -1 && hitObject.layer == triggerLayer)
            {
                if (hitObject.transform.parent != null)
                    return hitObject.transform.parent.gameObject;
            }

            // 3. Kiểm tra nếu là Trigger Collider (Dự phòng - dành cho các child hitbox chưa đặt tag)
            var col = hitObject.GetComponent<Collider2D>();
            if (col != null && col.isTrigger)
            {
                if (hitObject.transform.parent != null)
                    return hitObject.transform.parent.gameObject;
            }

            return hitObject;
        }

        /// <summary>
        /// Kiểm tra mục tiêu có hợp lệ và CÒN SỐNG hay không (Có Health > 0 hoặc Có Shield) VÀ chưa bị gán cờ IDie chết.
        /// </summary>
        public static bool IsTargetValidAndAlive(GameObject target)
        {
            if (target == null) return false;

            // Kiểm tra cờ chết
            var dieInterface = target.GetComponent<Script.Interfaces.IDie>();
            if (dieInterface != null && dieInterface.IsDead) return false;

            // Mục tiêu có thể nhận sát thương (IDamageable) => hợp lệ
            if (target.GetComponent<Script.Interfaces.IDamageable>() != null)
            {
                // Nếu có máu, phải còn > 0 mới tính là còn sống
                var health = target.GetComponent<IHealth>();
                if (health != null) return health.CurrentHp > 0;

                return true; // Có IDamageable nhưng không dùng HP (Denizen dùng coin)
            }

            return false;
        }

        /// <summary>
        /// hiện tại hàm gây dame chỉ mới sử dụng cho player
        /// khi enemy dùng sẽ cần thêm logic và chuyển isAlly thành false 
        /// </summary>
        public static void ProcessAttackHits(GameObject instigator, Collider2D[] hits, int count, float damageAmount)
        {
            bool allowDamageAlly = false;
            // Xác định xem instigator có được chém đồng minh không
            if (instigator != null && instigator.CompareTag(Script.Utils.GameTags.Player))
            {
                if (Script.Controller.PlayerController.PlayerController.Instance != null)
                {
                    allowDamageAlly = Script.Controller.PlayerController.PlayerController.Instance.damageAlly;
                }
            }

            Debug.Log($"[Utility.ProcessAttackHits] {instigator.name} bắt đầu xử lý HIT {count} đối tượng. Damage: {damageAmount}. Damage Ally: {allowDamageAlly}.");

            HashSet<GameObject> processedTargets = new HashSet<GameObject>();

            for (int i = 0; i < count; i++)
            {
                if (hits[i] == null) continue;

                GameObject hitObject = hits[i].gameObject;
                if (hitObject == null) continue;

                if (hitObject == instigator) continue;

                // Dọn mảng: Chuyển lên Parent nếu hit trúng Trigger Box
                hitObject = ResolveTargetFromTrigger(hitObject);
                if (hitObject == null) continue;
                
                // Tránh chém trúng 1 enemy 2 lần
                if (processedTargets.Contains(hitObject)) continue;
                processedTargets.Add(hitObject);

                // TÌM IDamageable: Kiểm tra chính nó hoặc cha của nó (cho trường hợp trúng Blocker con)
                IDamageable damageable = hitObject.GetComponentInParent<IDamageable>();

                if (damageable != null)
                {
                    GameObject effectiveTarget = (damageable as MonoBehaviour).gameObject;

                    // 1. Kiểm tra sống chết (Valid/Alive)
                    if (!IsTargetValidAndAlive(effectiveTarget))
                    {
                        Debug.Log($"[Utility.ProcessAttackHits] SKIP {effectiveTarget.name} - Not valid or already dead.");
                        continue;
                    }

                    // 2. ƯU TIÊN: Xử lý Wall Tier 0 (không nhận dame khi là móng)
                    if (effectiveTarget.CompareTag(Script.Utils.GameTags.Wall) || effectiveTarget.CompareTag(Script.Utils.GameTags.Stump))
                    {
                        var wallStatus = effectiveTarget.GetComponent<WallStatus>();
                        if (wallStatus != null && wallStatus.tier == 0)
                        {
                            Debug.Log($"[Utility.ProcessAttackHits] SKIP Wall Tier 0: {effectiveTarget.name}");
                            continue;
                        }
                    }

                    // 3. Ally Check
                    bool isTargetAlly = IsAlly(effectiveTarget);
                    if (!allowDamageAlly && isTargetAlly)
                    {
                        Debug.Log($"[Utility.ProcessAttackHits] SKIP Ally: {effectiveTarget.name} - IsAlly: {isTargetAlly}");
                        continue;
                    }

                    // 4. Gây sát thương
                    Debug.Log($"[Utility.ProcessAttackHits] HIT {effectiveTarget.name} (Tag: {effectiveTarget.tag}) -> Apply Damage {damageAmount}");
                    DamageHandler.ApplyDamage(effectiveTarget, damageAmount, instigator);
                }
                else
                {
                    Debug.Log($"[Utility.ProcessAttackHits] SKIP {hitObject.name} - No IDamageable found in hierarchy.");
                }
            }
        }
    }
}

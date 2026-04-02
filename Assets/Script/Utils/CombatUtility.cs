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
        public static void ProcessAttackHits(GameObject instigator, Collider2D[] hits, int count, float damageAmount, bool isEnemyAttack = false)
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

            HashSet<GameObject> processedTargets = new HashSet<GameObject>();

            for (int i = 0; i < count; i++)
            {
                if (hits[i] == null) continue;

                GameObject hitObject = hits[i].gameObject;
                if (hitObject == null) continue;
                if (hitObject == instigator) continue;

                // Dọn mảng: Chuyển lên Parent nếu hit trúng Trigger Box
                hitObject = ResolveTargetFromTrigger(hitObject);
                
                // Tránh chém trúng 1 enemy 2 lần
                if (processedTargets.Contains(hitObject)) continue;
                processedTargets.Add(hitObject);

                // TÌM IDamageable: Kiểm tra chính nó, cha nó, hoặc con của nó
                IDamageable damageable = hitObject.GetComponentInParent<IDamageable>() ?? hitObject.GetComponentInChildren<IDamageable>();

                if (damageable != null)
                {
                    GameObject effectiveTarget = (damageable as MonoBehaviour)?.gameObject;
                    if (effectiveTarget == null) 
                    {
                        continue;
                    }

                    // QUAN TRỌNG: Tránh việc tự gây dame cho chính mình khi Hitbox quét trúng Root/Child Collider của bản thân
                    if (effectiveTarget == instigator)
                    {
                        continue;
                    }

                    // 1. Kiểm tra sống chết (Valid/Alive)
                    if (!IsTargetValidAndAlive(effectiveTarget))
                    {
                        continue;
                    }

                    // 2. ƯU TIÊN: Xử lý Wall Tier 0 (không nhận dame khi là móng)
                    if (effectiveTarget.CompareTag(Script.Utils.GameTags.Wall) || effectiveTarget.CompareTag(Script.Utils.GameTags.Stump))
                    {
                        var wallStatus = effectiveTarget.GetComponent<WallStatus>();
                        if (wallStatus != null && wallStatus.tier == 0)
                        {
                            continue;
                        }
                    }

                    // 3. Phân biệt phe phái (Ally/Enemy)
                    if (isEnemyAttack)
                    {
                        // Nếu AI Enemy đánh: Chỉ được gây dame lên những thứ thuộc phe Player (AllyTags)
                        // Bất cứ thứ gì không nằm trong AllyTags (bao gồm quái khác, portal, target rác) đều bị bỏ qua.
                        bool isTargetPlayerAlly = IsAlly(effectiveTarget);
                        if (!isTargetPlayerAlly)
                        {
                            // Debug.Log($"[Utility.ProcessAttackHits] Enemy SKIP Non-Ally target (Friendly Fire prevention): {effectiveTarget.name}");
                            continue;
                        }
                    }
                    else
                    {
                        // Nếu Player hoặc Archer đánh: Ta cần tránh đánh trúng đồng minh
                        bool isTargetAlly = IsAlly(effectiveTarget);
                        if (isTargetAlly && !allowDamageAlly)
                        {
                            Debug.Log($"[Utility.ProcessAttackHits] SKIP Ally: {effectiveTarget.name} - IsAlly: {isTargetAlly}");
                            continue;
                        }
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

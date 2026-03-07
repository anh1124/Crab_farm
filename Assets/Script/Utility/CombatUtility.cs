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
            if (PrefabData.Instance == null || PrefabData.Instance.allyTags == null)
            {
                Debug.LogWarning("PrefabData.Instance hoặc allyTags là null. Không thể kiểm tra đồng minh.");
                return false;
            }
            return PrefabData.Instance.allyTags.Contains(obj.tag);
        }

        /// <summary>
        /// Hàm tiện ích: Nếu chém trúng GameObject thuộc layer "TriggerLayer", tự động chuyển lên parent 1 cấp.
        /// Việc này giúp Combat quy về đối tượng Entity gốc để xử lý.
        /// </summary>
        public static GameObject ResolveTargetFromTrigger(GameObject hitObject)
        {
            if (hitObject == null) return null;

            int triggerLayer = LayerMask.NameToLayer("TriggerLayer");
            if (hitObject.layer == triggerLayer)
            {
                if (hitObject.transform.parent != null)
                {
                    return hitObject.transform.parent.gameObject;
                }
            }
            return hitObject;
        }

        /// <summary>
        /// Kiểm tra mục tiêu có hợp lệ và CÒN SỐNG hay không (Có Health > 0 hoặc Có Shield) VÀ chưa bị gán cờ IDie chết.
        /// </summary>
        public static bool IsTargetValidAndAlive(GameObject target)
        {
            if (target == null) return false;

            // KIỂM TRA FLAG CHẾT ĐẦU TIÊN: Nếu flag Die đã bật thì mặc kệ máu hay khiên, nó là một cái xác.
            var dieInterface = target.GetComponent<Script.Interfaces.IDie>();
            if (dieInterface != null && dieInterface.IsDead)
            {
                Debug.Log($"[CombatUtility] {target.name} bị loại vì mang cờ IDie.IsDead.");
                return false; 
            }

            // Nếu có Health, phải còn > 0 máu
            var health = target.GetComponent<IHealth>();
            if (health != null)
            {
                bool hasHp = health.CurrentHp > 0;
                Debug.Log($"[CombatUtility] {target.name} có IHealth. CurentHp = {health.CurrentHp} -> Valid: {hasHp}");
                return hasHp;
            }

            // Nếu KHÔNG CÓ Health nhưng có CoinShield (Ví dụ: Denizen), thì coi như "Còn sống" nếu còn khiên
            var coinShield = target.GetComponent<ICoinShield>();
            if (coinShield != null)
            {
                Debug.Log($"[CombatUtility] {target.name} KHÔNG CÓ MÁU, nhưng có ICoinShield. Valid: True");
                return true; 
            }

            Debug.Log($"[CombatUtility] {target.name} bị loại vì không có cả Máu lẫn Khiên.");
            return false;
        }

        /// <summary>
        /// hiện tại hàm gây dame chỉ mới sử dụng cho player
        /// khi enemy dùng sẽ cần thêm logic và chuyển isAlly thành false 
        /// </summary>
        public static void ProcessAttackHits(GameObject instigator, Collider2D[] hits, int count, int damageAmount, bool allowDamageAlly)
        {
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
                
                // Tránh chém trúng 1 enemy 2 lần (ví dụ trúng cả Body thật lẫn Body Trigger sau khi resolve)
                if (processedTargets.Contains(hitObject)) continue;
                processedTargets.Add(hitObject);

                Debug.Log($"[Utility.ProcessAttackHits] Processing Hit[{i}]: {hitObject.name} (Tag: {hitObject.tag})");

                IHealth target = hitObject.GetComponent<IHealth>();
                ICoinShield coinTarget = hitObject.GetComponent<ICoinShield>();

                if (target != null || coinTarget != null)
                {
                    // ƯU TIÊN 1: Xử lý Wall trước (để tránh bị Ally check chặn)
                    if (hitObject.CompareTag("Wall"))
                    {
                        WallStatus wall = hitObject.GetComponent<WallStatus>();
                        if (wall != null)
                        {
                            // Bỏ qua wall tier 0 (stump/foundation chưa xây xong?)
                            if (wall.tier == 0)
                            {
                                Debug.Log($"[Utility.ProcessAttackHits] SKIP Wall Tier 0: {hitObject.name}");
                                continue;
                            }
                            else
                            {
                                Debug.Log($"[Utility.ProcessAttackHits] HIT Wall {hitObject.name} (Tier: {wall.tier}) -> Apply Damage {damageAmount}");
                                Script.Utils.DamageHandler.ApplyDamage(hitObject, (int)damageAmount, instigator);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[Utility.ProcessAttackHits] Object {hitObject.name} tag 'Wall' missing WallStatus script.");
                        }
                        // Wall đã xử lý xong, tiếp tục vòng lặp
                        continue;
                    }

                    // ƯU TIÊN 2: Ally Check
                    bool isTargetAlly = IsAlly(hitObject);
                    if (!allowDamageAlly && isTargetAlly)
                    {
                        Debug.Log($"[Utility.ProcessAttackHits] SKIP Ally: {hitObject.name} (Tag: {hitObject.tag}) - IsAlly: {isTargetAlly}");
                        continue;
                    }

                    // ƯU TIÊN 3: Default Damage (Enemy, Object khác...)
                    Debug.Log($"[Utility.ProcessAttackHits] HIT Object {hitObject.name} (Tag: {hitObject.tag}) -> Apply Damage {damageAmount}");
                    Script.Utils.DamageHandler.ApplyDamage(hitObject, (int)damageAmount, instigator);
                }
                else
                {
                    Debug.Log($"[Utility.ProcessAttackHits] SKIP Object {hitObject.name} - No IHealth or ICoinShield component");
                }
            }
        }
    }
}

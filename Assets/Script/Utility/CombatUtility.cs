using UnityEngine;
using Script.Data_Model;
using Script.Data_Model.Status;
using Script.Game.Enemy.Interfaces;
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
        /// hiện tại hàm gây dame chỉ mới sử dụng cho player
        /// khi enemy dùng sẽ cần thêm logic và chuyển isAlly thành false 
        /// </summary>
        public static void ProcessAttackHits(GameObject instigator, Collider2D[] hits, int count, int damageAmount, bool allowDamageAlly)
        {
            Debug.Log($"[Utility.ProcessAttackHits] {instigator.name} bắt đầu xử lý HIT {count} đối tượng. Damage: {damageAmount}. Damage Ally: {allowDamageAlly}.");

            for (int i = 0; i < count; i++)
            {
                if (hits[i] == null) continue;

                GameObject hitObject = hits[i].gameObject;
                if (hitObject == null) continue;

                if (hitObject == instigator) continue;

                Debug.Log($"[Utility.ProcessAttackHits] Processing Hit[{i}]: {hitObject.name} (Tag: {hitObject.tag})");

                IHealth target = hitObject.GetComponent<IHealth>();

                if (target != null)
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
                    Debug.Log($"[Utility.ProcessAttackHits] SKIP Object {hitObject.name} - No IHealth component");
                }
            }
        }
    }
}

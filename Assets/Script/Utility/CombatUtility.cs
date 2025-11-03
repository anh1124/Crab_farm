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
            Debug.Log($"[Utility.ProcessAttackHits] {instigator.name} bắt đầu xử lý {count} đối tượng va chạm. Damage: {damageAmount}. Damage Ally: {allowDamageAlly}.");

            for (int i = 0; i < count; i++)
            {
                if (hits[i] == null)
                {
                    Debug.LogWarning($"[Utility.ProcessAttackHits] Collider tại vị trí {i} trong mảng hits là null. Bỏ qua.");
                    continue;
                }

                GameObject hitObject = hits[i].gameObject;
                if (hitObject == null)
                {
                    Debug.LogWarning($"[Utility.ProcessAttackHits] GameObject của collider {hits[i].name} tại vị trí {i} là null. Bỏ qua.");
                    continue;
                }

                if (hitObject == instigator)
                {
                    Debug.Log($"[Utility.ProcessAttackHits] Bỏ qua đối tượng gây tấn công {hitObject.name} (Tag: {hitObject.tag}).");
                    continue;
                }

                IHealth target = hitObject.GetComponent<IHealth>();

                if (target != null)
                {
                    bool isTargetAlly = IsAlly(hitObject);

                    if (!allowDamageAlly && isTargetAlly)
                    {
                        Debug.Log($"[Utility.ProcessAttackHits] Bỏ qua đồng minh {hitObject.name} (Tag: {hitObject.tag}).");
                        continue;
                    }

                    // Kiểm tra tier của Wall
                    //hiện tại đang test hát thương lên wall bằng dame của player
                    //khi thêm enemy tấn công sẽ thêm wall và danh sách allyTags
                    if (hitObject.CompareTag("Wall"))
                    {
                        WallStatus wall = hitObject.GetComponent<WallStatus>();
                        if (wall != null)
                        {
                            //bỏ qua wall tier 0 vì vì đây không phải wall
                            if (wall.tier == 0)
                            {
                                continue;
                            }
                            else
                            {
                                Script.Utils.DamageHandler.ApplyDamage(hitObject, (int)damageAmount, instigator);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[Utility.ProcessAttackHits] Đối tượng {hitObject.name} có tag 'Wall' nhưng không có script Wall. Bỏ qua.");
                        }
                    }
                    else
                    {
                        Debug.Log($"[Utility.ProcessAttackHits] {hitObject.name} (Tag: {hitObject.tag}) nhận sát thương {damageAmount} từ {instigator.name}.");
                        Script.Utils.DamageHandler.ApplyDamage(hitObject, (int)damageAmount, instigator);
                    }
                }
                else
                {
                    Debug.Log($"[Utility.ProcessAttackHits] Đối tượng {hitObject.name} (Tag: {hitObject.tag}) không implement IHealth. Bỏ qua.");
                }
            }
        }
    }
}



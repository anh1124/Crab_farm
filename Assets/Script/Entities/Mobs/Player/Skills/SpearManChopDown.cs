using UnityEngine;
using Script.Controller.PlayerController;
using Script.Utils;

namespace Script.Game.PlayerCharacter.Skills
{
    /// <summary>
    /// Skill ChopDown - Skill 1 của SpearMan
    /// Skill chém thẳng xuống, gây 10 damage, cooldown 0.8s
    /// </summary>
    [CreateAssetMenu(fileName = "SpearManChopDown", menuName = "Skills/SpearMan/ChopDown")]
    public class SpearManChopDown : SkillBase
    {
        [Header("ChopDown Specific")]
        [Tooltip("Prefab chứa Collider của skill này")]
        public GameObject hitboxPrefab;
        
        // Xóa hoàn toàn các biến _runtimeHitbox và _hitbox ở đây.
        // ScriptableObject PHẢI phi trạng thái (stateless) để nhiều nhân vật
        // có thể dùng chung 1 skill mà không ghi đè dữ liệu của nhau.

        public override void Initialize(Transform parent)
        {
            if (hitboxPrefab == null)
            {
                Debug.LogError($"[SpearManChopDown] Hitbox Prefab is missing in Skill Asset: {name}");
                return;
            }

            string runtimeName = hitboxPrefab.name + "_Runtime";
            Transform existing = parent.Find(runtimeName);
            
            // Tái sử dụng nếu đã tồn tại (hỗ trợ khi re-equip skill)
            if (existing != null)
            {
                return;
            }

            // Spawn prefab vào scene dưới container (parent)
            GameObject runtimeHitbox = Instantiate(hitboxPrefab, parent);
            runtimeHitbox.name = runtimeName;
            runtimeHitbox.SetActive(false); // Ẩn mặc định
        }
        
        public override void OnStart(GameObject owner)
        {
            // Called khi skill bắt đầu execute
            // Empty for now - có thể thêm VFX, sound effects sau
        }
        
        public override void OnHit(GameObject owner)
        {
            // Tìm container chứa hitbox
            Transform container = owner.transform.Find("SkillContainer");
            if (container == null)
            {
                Debug.LogError("[SpearManChopDown] Không tìm thấy SkillContainer trên owner.");
                return;
            }

            string runtimeName = hitboxPrefab.name + "_Runtime";
            Transform runtimeHitboxTransform = container.Find(runtimeName);
            
            if (runtimeHitboxTransform == null)
            {
                Debug.LogError($"[SpearManChopDown] Không tìm thấy hitbox {runtimeName}. Cần gọi Initialize() trước.");
                return;
            }

            Collider2D hitbox = runtimeHitboxTransform.GetComponent<Collider2D>();
            if (hitbox == null) return;
            
            Debug.Log($"[SpearManChopDown] Calling ActivateHitbox with damage={damage}");
            
            // Use HitboxController để process damage
            HitboxController.ActivateHitbox(hitbox, damage, owner);
        }
        
        public override void OnEnd(GameObject owner)
        {
            // Called khi animation kết thúc
            // Empty for now
        }
    }
}

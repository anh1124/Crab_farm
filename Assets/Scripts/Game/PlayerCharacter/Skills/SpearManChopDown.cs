using UnityEngine;
using Script.Controller.PlayerController;
using Script.Utils;

namespace Script.Game.PlayerCharacter.Skills
{
    /// <summary>
    /// Skill ChopDown - Skill 1 của SpearMan
    /// Skill chém thẳng xuống, gây 10 damage, tiêu 15 stamina, cooldown 0.8s
    /// </summary>
    [CreateAssetMenu(fileName = "SpearManChopDown", menuName = "Skills/SpearMan/ChopDown")]
    public class SpearManChopDown : SkillBase
    {
        [Header("ChopDown Specific")]
        [Tooltip("Prefab chứa Collider của skill này")]
        public GameObject hitboxPrefab;
        
        // Runtime refernece
        private GameObject runtimeHitbox;

        public override void Initialize(Transform parent)
        {
            if (hitboxPrefab == null)
            {
                Debug.LogError($"[SpearManChopDown] Hitbox Prefab is missing in Skill Asset: {name}");
                return;
            }

            // Spawn prefabs
            if (runtimeHitbox == null)
            {
                runtimeHitbox = Instantiate(hitboxPrefab, parent);
                runtimeHitbox.name = hitboxPrefab.name + "_Runtime";
                runtimeHitbox.SetActive(false); // Hide by default
            }

            // Get collider
            hitbox = runtimeHitbox.GetComponent<Collider2D>();
            if (hitbox == null)
            {
                 Debug.LogError($"[SpearManChopDown] Created object has no Collider2D!");
            }
        }
        
        public override void OnStart(GameObject owner)
        {
            // Called khi skill bắt đầu execute
            // Empty for now - có thể thêm VFX, sound effects sau
        }
        
        public override void OnHit(GameObject owner)
        {
            // Called tại frame damage trong animation
            Debug.Log($"[SpearManChopDown] OnHit called. runtimeHitbox={runtimeHitbox}, hitbox={hitbox}");
            
            if (hitbox == null)
            {
                Debug.LogError("[SpearManChopDown] Hitbox is NULL! Skill not initialized properly.");
                return;
            }
            
            // Get damageAlly flag từ PlayerController
            bool damageAlly = false;
            if (PlayerController.Instance != null)
            {
                damageAlly = PlayerController.Instance.damageAlly;
            }
            
            Debug.Log($"[SpearManChopDown] Calling ActivateHitbox with damage={damage}, damageAlly={damageAlly}");
            
            // Use HitboxController để process damage
            HitboxController.ActivateHitbox(hitbox, damage, owner, damageAlly);
        }
        
        public override void OnEnd(GameObject owner)
        {
            // Called khi animation kết thúc
            // Empty for now
        }
    }
}

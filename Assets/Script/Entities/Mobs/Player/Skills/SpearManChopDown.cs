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
        
        // Runtime references: [NonSerialized] đảm bảo Unity KHÔNG lưu vào Asset file.
        // Sau khi Exit Play Mode, các field này tự reset về null — an toàn tuyệt đối.
        [System.NonSerialized] private GameObject _runtimeHitbox;
        [System.NonSerialized] private Collider2D _hitbox;

        public override void Initialize(Transform parent)
        {
            if (hitboxPrefab == null)
            {
                Debug.LogError($"[SpearManChopDown] Hitbox Prefab is missing in Skill Asset: {name}");
                return;
            }

            // Cleanup object cũ nếu còn sót (ví dụ: re-initialize khi reload scene)
            if (_runtimeHitbox != null)
            {
                Destroy(_runtimeHitbox);
                _runtimeHitbox = null;
                _hitbox = null;
            }

            // Spawn prefab vào scene dưới parent của Player
            _runtimeHitbox = Instantiate(hitboxPrefab, parent);
            _runtimeHitbox.name = hitboxPrefab.name + "_Runtime";
            _runtimeHitbox.SetActive(false); // Ẩn mặc định

            // Lấy collider từ object vừa spawn — lưu cục bộ, KHÔNG ghi vào SO field
            _hitbox = _runtimeHitbox.GetComponent<Collider2D>();
            if (_hitbox == null)
            {
                Debug.LogError($"[SpearManChopDown] Spawned hitbox has no Collider2D component!");
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
            Debug.Log($"[SpearManChopDown] OnHit called. runtimeHitbox={_runtimeHitbox}, hitbox={_hitbox}");
            
            if (_hitbox == null)
            {
                Debug.LogError("[SpearManChopDown] Hitbox is NULL! Call Initialize() first.");
                return;
            }
            
            // Get damageAlly flag từ PlayerController
            bool damageAlly = PlayerController.Instance?.damageAlly ?? false;
            
            Debug.Log($"[SpearManChopDown] Calling ActivateHitbox with damage={damage}, damageAlly={damageAlly}");
            
            // Use HitboxController để process damage
            HitboxController.ActivateHitbox(_hitbox, damage, owner, damageAlly);
        }
        
        public override void OnEnd(GameObject owner)
        {
            // Called khi animation kết thúc
            // Empty for now
        }
    }
}

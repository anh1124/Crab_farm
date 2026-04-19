using UnityEngine;
using Script.Controller.PlayerController;
using Script.Utils;
using Script.Audio;

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

        private void LogBlue(string message)
        {
            Debug.Log($"<color=blue>[{System.DateTime.Now:HH:mm:ss.fff}] {message}</color>");
        }

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
            LogBlue($"[SpearManChopDown] OnStart - Skill Execution started (animation trigger should happen now)");
            // Called khi skill bắt đầu execute
        }
        
        public override void OnHit(GameObject owner)
        {
            LogBlue($"[SpearManChopDown] OnHit - Animation Event Damage Frame reached");

            // Phát âm thanh swing weapon khi animation event fire
            var emitter = owner.GetComponent<AudioEmitter>();
            if (emitter != null)
                emitter.PlaySound("SwingWeapon", PlaySoundConfig.Default);

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
            
            Debug.Log($"[SpearManChopDown] Calling ActivateHitbox with damage={TotalDamage} (Base: {damage})");
            
            // Use HitboxController để process damage
            HitboxController.ActivateHitbox(hitbox, TotalDamage, owner);
        }
        
        public override void OnEnd(GameObject owner)
        {
            LogBlue($"[SpearManChopDown] OnEnd - Skill finished");
            // Called khi animation kết thúc
            // Empty for now
        }
    }
}

using UnityEngine;

namespace Script.Game.PlayerCharacter.Skills
{
    /// <summary>
    /// Abstract base class cho tất cả skills trong game.
    /// Mỗi skill cụ thể sẽ kế thừa class này và implement các method abstract.
    /// </summary>
    public abstract class SkillBase : ScriptableObject
    {
        // === CONFIGURATION PROPERTIES ===
        
        /// <summary>Tên skill hiển thị</summary>
        public string skillName;
        
        /// <summary>Damage gây ra (int để tương thích với DamageHandler)</summary>
        public int damage;
        
        /// <summary>Stamina tiêu hao khi dùng skill</summary>
        public float staminaCost;
        
        /// <summary>Mana tiêu hao khi dùng skill</summary>
        public float manaCost;
        
        /// <summary>Thời gian cooldown (giây) sau khi skill kết thúc</summary>
        /// <summary>Thời gian cooldown (giây) sau khi skill kết thúc</summary>
        public float cooldown;
        
        /// <summary>Cho phép di chuyển trong lúc skill đang chạy hay không</summary>
        public bool allowMovementDuringSkill;
        
        /// <summary>Collider dùng để phát hiện va chạm và gây damage</summary>
        public Collider2D hitbox;
        
        /// <summary>Tên trigger animation trong Animator Controller</summary>
        public string animationTrigger;
        
        /// <summary>
        /// Khởi tạo skill (Spawn prefab hitbox, setup resource, etc)
        /// </summary>
        /// <param name="parent">Container để chứa các object runtime của skill</param>
        public virtual void Initialize(Transform parent) { }

        // === LIFECYCLE METHODS ===
        
        /// <summary>
        /// Được gọi khi skill bắt đầu thực thi (trước khi animation chạy)
        /// </summary>
        /// <param name="owner">GameObject của player sở hữu skill</param>
        public abstract void OnStart(GameObject owner);
        
        /// <summary>
        /// Được gọi tại frame gây damage trong animation (từ Animation Event)
        /// Đây là nơi xử lý logic damage chính
        /// </summary>
        /// <param name="owner">GameObject của player sở hữu skill</param>
        public abstract void OnHit(GameObject owner);
        
        /// <summary>
        /// Được gọi khi animation kết thúc (từ Animation Event)
        /// </summary>
        /// <param name="owner">GameObject của player sở hữu skill</param>
        public abstract void OnEnd(GameObject owner);
    }
}

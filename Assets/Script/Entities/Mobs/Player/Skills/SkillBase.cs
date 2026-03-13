using UnityEngine;

namespace Script.Game.PlayerCharacter.Skills
{
    /// <summary>
    /// Abstract base class cho tất cả skills trong game.
    /// Mỗi skill cụ thể sẽ kế thừa class này và implement các method abstract.
    /// </summary>
    public abstract class SkillBase : ScriptableObject
    {
        public enum ResourceType
        {
            Mana,
            Stamina
        }

        // === CONFIGURATION PROPERTIES ===
        
        [Header("Resource")]
        public ResourceType resourceType;
        public int resourceCost;
        
        [Header("UI")]
        /// <summary>Tên hiển thị trên UI</summary>
        public string displayName;
        
        /// <summary>Icon hiển thị trên UI (dùng cho cooldown overlay)</summary>
        public Sprite icon;
        
        [Header("Stats")]
        /// <summary>Tên skill</summary>
        public string skillName;
        
        /// <summary>Damage gây ra (int để tương thích với DamageHandler)</summary>
        public int damage;
        
        /// <summary>Thời gian cooldown (giây) sau khi skill kết thúc</summary>
        public float cooldown;
        
        /// <summary>Cho phép di chuyển trong lúc skill đang chạy hay không</summary>
        public bool allowMovementDuringSkill;
        
        /// <summary>Collider dùng để phát hiện va chạm và gây damage</summary>
        public Collider2D hitbox;
        
        [Header("Animation")]
        /// <summary>Nếu true, skill sẽ đợi Animation Event gọi OnEnd. Nếu false, skill kết thúc ngay sau OnStart.</summary>
        public bool useAnimation = true;

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

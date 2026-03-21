using UnityEngine;

namespace Script.Data_Model.Config
{
    /// <summary>
    /// Cấu hình mặc định của Player — là nguồn sự thật duy nhất cho giá trị khởi tạo.
    /// Tạo asset: chuột phải trong Project → Create → Config → PlayerConfig
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerConfig", menuName = "Config/PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [Header("HP")]
        [Tooltip("Máu tối đa khi bắt đầu game mới")]
        public float maxHp = 10f;
        [Tooltip("Lượng máu hồi mỗi giây (phân số)")]
        public float hpRegenRate = 0.015f;

        [Header("Movement")]
        [Tooltip("Tốc độ di chuyển mặc định")]
        public float moveSpeed = 12f;

        [Header("Inventory")]
        [Tooltip("Số coin tối đa có thể giữ")]
        public int maxCoins = 30;

        [Header("Mana")]
        [Tooltip("Mana tối đa")]
        public float maxMana= 10f;
        [Tooltip("Lượng mana hồi mỗi giây")]
        public float manaRegenRate = 0.1f;
    }
}

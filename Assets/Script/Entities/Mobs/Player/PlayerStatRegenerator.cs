using Script.Data_Model.Status;
using UnityEngine;

namespace Script.Game.Player
{
    /// <summary>
    /// Chuyên xử lý hồi phục các chỉ số theo thời gian (HP, Mana, ...)
    /// Tách biệt khỏi PlayerBehavior để giữ Hub sạch sẽ.
    /// </summary>
    [RequireComponent(typeof(PlayerStatus))]
    public class PlayerStatRegenerator : MonoBehaviour
    {
        private PlayerStatus status;

        private void Awake()
        {
            status = GetComponent<PlayerStatus>();
        }

        private void Update()
        {
            HandleManaRegen();
            HandleHpRegen();
        }

        private void HandleManaRegen()
        {
            if (status.currentMana < status.maxMana && status.manaRegenRate > 0)
            {
                status.currentMana += status.manaRegenRate * Time.deltaTime;
                status.currentMana = Mathf.Min(status.currentMana, status.maxMana);
            }
        }

        private void HandleHpRegen()
        {
            // Chỉ hồi máu nếu còn sống và chưa đầy máu
            if (status.currentHp > 0 && status.currentHp < status.maxHp && status.hpRegenRate > 0)
            {
                status.currentHp += status.hpRegenRate * Time.deltaTime;
                status.currentHp = Mathf.Min(status.currentHp, status.maxHp);
            }
        }
    }
}

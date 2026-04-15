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
            // [ANTI-CHEAT] Nếu chưa có TownCenter hoặc Tier 0 -> không hồi máu/mana
            if (Script.Utility.GetTownCenterTier() < 1) return;

            HandleManaRegen();
            HandleHpRegen();
        }

        private void HandleManaRegen()
        {
            if (status.manaRegenRate > 0 && status.Mana.Current < status.Mana.Max)
            {
                status.Mana.Add(status.manaRegenRate * Time.deltaTime);
            }
        }

        private void HandleHpRegen()
        {
            // Chỉ hồi máu nếu còn sống và chưa đầy máu
            if (status.HP.Current > 0 && status.HP.Current < status.HP.Max && status.hpRegenRate > 0)
            {
                status.HP.Add(status.hpRegenRate * Time.deltaTime);
            }
        }
    }
}

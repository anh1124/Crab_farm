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
        private Script.Manager.WorldManager.GameTimeManager _timeManager;

        private void Awake()
        {
            status = GetComponent<PlayerStatus>();
        }

        private void Start()
        {
            _timeManager = FindObjectOfType<Script.Manager.WorldManager.GameTimeManager>();
        }

        private void Update()
        {
            //Nếu chưa có TownCenter hoặc Tier 0 -> không hồi máu/mana
            if (Script.Utility.GetTownCenterTier() < 1) return;

            // Lấy time scale để hồi phục đồng bộ với tốc độ game
            float timeScale = GetGameTimeScale();
            float scaledDeltaTime = Time.deltaTime * timeScale;

            HandleManaRegen(scaledDeltaTime);
            HandleHpRegen(scaledDeltaTime);
        }

        private float GetGameTimeScale()
        {
            if (_timeManager == null)
            {
                _timeManager = FindObjectOfType<Script.Manager.WorldManager.GameTimeManager>();
                if (_timeManager == null)
                {
                    Debug.LogError("[PlayerStatRegenerator] GameTimeManager not found! Using Time.timeScale as fallback.");
                }
            }
            return _timeManager != null ? _timeManager.GameTimeScale : Time.timeScale;
        }

        private void HandleManaRegen(float deltaTime)
        {
            if (status.manaRegenRate > 0 && status.Mana.Current < status.Mana.Max)
            {
                status.Mana.Add(status.manaRegenRate * deltaTime);
            }
        }

        private void HandleHpRegen(float deltaTime)
        {
            // Chỉ hồi máu nếu còn sống và chưa đầy máu
            if (status.HP.Current > 0 && status.HP.Current < status.HP.Max && status.hpRegenRate > 0)
            {
                status.HP.Add(status.hpRegenRate * deltaTime);
            }
        }
    }
}

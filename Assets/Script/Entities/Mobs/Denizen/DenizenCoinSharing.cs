using UnityEngine;
using Script.Data_Model.Status;
using Script.Game.Item.Coin;

namespace Script.Game.Mobs.Subjects
{
    [RequireComponent(typeof(DenizenStatus))]
    public class DenizenCoinSharing : MonoBehaviour
    {
        [Header("Coin Drop Settings")]
        [Tooltip("Thời gian tối thiểu giữa các lần drop coin khi gặp player (giây)")]
        public float COIN_DROP_COOLDOWN = 0.7f;
        private float lastCoinDropTime = 0f;

        private DenizenStatus status;

        private void Awake()
        {
            status = GetComponent<DenizenStatus>();
        }

        /// <summary>
        /// Kiểm tra xem Denizen này có đủ điều kiện nhả coin hay không.
        /// (Phải có lớn hơn 1 coin để giữ lại ít nhất 1 coin phòng thân)
        /// </summary>
        public bool CanDropCoin()
        {
            return status != null && status.CurrentCoins > 1;
        }

        /// <summary>
        /// Thực hiện nhả coin nếu đủ điều kiện (cooldown & số lượng coin).
        /// </summary>
        /// <returns>Trả về true nếu đã thực sự nhả coin trong lần gọi này.</returns>
        public bool TryDropCoin()
        {
            if (!CanDropCoin()) return false;

            float currentTime = Time.time;
            if (currentTime - lastCoinDropTime >= COIN_DROP_COOLDOWN)
            {
                InternalDropCoin();
                lastCoinDropTime = currentTime;
                return true;
            }
            return false;
        }

        private void InternalDropCoin()
        {
            // Rút 1 coin
            status.CurrentCoins -= 1;
            
            if (Spawner.Instance != null)
            {
                // Dùng pool của Spawner để tránh memory allocation liên tục
                Vector2 dropDirection = UnityEngine.Random.insideUnitCircle.normalized;
                Vector2 spawnPosition = new Vector2(transform.position.x, transform.position.y + 2f);
                Spawner.Instance.DropByDenizen(spawnPosition, 1, dropDirection);
            }
        }
    }
}

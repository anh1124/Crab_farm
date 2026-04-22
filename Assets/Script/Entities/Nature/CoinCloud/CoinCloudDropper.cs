using Script.Game.Item.Coin;
using Script.Manager;
using UnityEngine;

namespace Script.Game.CoinCloud
{
    /// <summary>
    /// Xử lý drop coin từ đám mây
    /// </summary>
    public class CoinCloudDropper : MonoBehaviour
    {
        [Tooltip("Số lượng coin sẽ drop")]
        public int coinAmount = 10;
        
        [Tooltip("Thời gian giữa mỗi lần drop coin (giây)")]
        public float dropInterval = 0.5f;
        
        // State
        private int coinsDropped = 0;
        private int coinsRemaining = 0;
        private bool isDropping = false;
        private float dropTimer = 0f;
        
        public int CoinsDropped => coinsDropped;
        public int CoinsRemaining => coinsRemaining;
        public bool IsDropping => isDropping;
        public int CoinAmount => coinAmount;
        public float DropInterval => dropInterval;
        
        private void Update()
        {
            if (isDropping && coinsRemaining > 0)
            {
                dropTimer += Time.deltaTime;
                
                if (dropTimer >= dropInterval)
                {
                    DropSingleCoin();
                    coinsDropped++;
                    coinsRemaining--;
                    dropTimer = 0f;
                    
                    if (coinsRemaining <= 0)
                    {
                        isDropping = false;
                    }
                }
            }
        }
        
        /// <summary>
        /// Bắt đầu drop coin
        /// </summary>
        public void StartDropping()
        {
            if (coinsRemaining <= 0)
            {
                coinsRemaining = coinAmount;
            }
            isDropping = true;
            dropTimer = 0f;
        }
        
        /// <summary>
        /// Dừng drop coin
        /// </summary>
        public void StopDropping()
        {
            isDropping = false;
        }
        
        /// <summary>
        /// Khởi tạo số coin còn lại (dùng khi restore từ save)
        /// </summary>
        public void SetCoinsRemaining(int amount)
        {
            coinsRemaining = amount;
        }
        
        /// <summary>
        /// Đặt số coin đã drop (dùng khi restore từ save)
        /// </summary>
        public void SetCoinsDropped(int amount)
        {
            coinsDropped = amount;
        }
        
        private void DropSingleCoin()
        {
            if (Spawner.Instance == null) return;
            
            Vector2 dropDirection = Random.insideUnitCircle.normalized;
            Spawner.Instance.DropByNature(transform.position, 1, dropDirection);
        }
        
        /// <summary>
        /// Kiểm tra đã drop hết coin chưa
        /// </summary>
        public bool IsFinished => coinsRemaining <= 0 && !isDropping;
    }
}

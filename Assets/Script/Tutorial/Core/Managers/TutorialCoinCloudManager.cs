using UnityEngine;

namespace Script.Tutorial
{
    /// <summary>
    /// Quản lý CoinCloud trong tutorial.
    /// CoinCloud được gán sẵn từ Inspector, chỉ cần trigger để hoạt động.
    /// </summary>
    public class TutorialCoinCloudManager : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("GameObject CoinCloud đã có sẵn trong scene (kéo từ scene vào đây)")]
        [SerializeField] private GameObject coinCloud;

        private Script.Game.CoinCloud.CoinCloudDropper _coinCloudDropper;
        private Script.Game.CoinCloud.CoinCloudController _coinCloudController;
        private System.Action _onCoinCloudFinished;
        
        /// <summary>
        /// Event khi coin cloud drop xong
        /// </summary>
        public event System.Action OnCoinCloudFinished
        {
            add => _onCoinCloudFinished += value;
            remove => _onCoinCloudFinished -= value;
        }
        
        /// <summary>
        /// Trigger CoinCloud đã có sẵn trong scene để bắt đầu drop coins.
        /// </summary>
        public void SpawnCoinCloud(Vector3 pos, int amount)
        {
            if (coinCloud == null)
            {
                Debug.LogWarning("[TutorialCoinCloudManager] CoinCloud reference chưa được gán trong Inspector!");
                return;
            }

            // Cache components
            if (_coinCloudDropper == null)
                _coinCloudDropper = coinCloud.GetComponent<Script.Game.CoinCloud.CoinCloudDropper>();
            if (_coinCloudController == null)
                _coinCloudController = coinCloud.GetComponent<Script.Game.CoinCloud.CoinCloudController>();

            if (_coinCloudController == null)
            {
                Debug.LogWarning("[TutorialCoinCloudManager] CoinCloud không có CoinCloudController!");
                return;
            }

            // Chỉ kích hoạt controller, không ghi đè bất kỳ thông số nào (vị trí, số lượng tiền, delay...)
            _coinCloudController.ActivateCloud();

            // Subscribe event để thông báo khi hoàn thành
            if (_coinCloudDropper != null)
            {
                _coinCloudDropper.OnFinishedDropping += HandleCoinCloudFinished;
            }

            Debug.Log($"[TutorialCoinCloudManager] Đã kích hoạt CoinCloud hướng dẫn.");
        }
        
        /// <summary>
        /// Xử lý khi coin cloud drop xong
        /// </summary>
        private void HandleCoinCloudFinished()
        {
            Debug.Log("[TutorialCoinCloudManager] CoinCloud đã drop xong!");

            // Unsubscribe để tránh memory leak
            if (_coinCloudDropper != null)
            {
                _coinCloudDropper.OnFinishedDropping -= HandleCoinCloudFinished;
            }

            // Fire event
            _onCoinCloudFinished?.Invoke();
        }

        /// <summary>
        /// Cleanup khi cần
        /// </summary>
        public void Cleanup()
        {
            if (_coinCloudDropper != null)
            {
                _coinCloudDropper.OnFinishedDropping -= HandleCoinCloudFinished;
            }
        }

        /// <summary>
        /// Kiểm tra có coinCloud reference không
        /// </summary>
        public bool HasActiveCoinCloud => coinCloud != null;
    }
}

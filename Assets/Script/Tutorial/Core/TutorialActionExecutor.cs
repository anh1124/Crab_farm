using UnityEngine;
// using Script.Controller.GUI_Controller.PlayerGui; // No longer needed directly here

namespace Script.Tutorial
{
    /// <summary>
    /// Thực thi các action khi bắt đầu dialog (SpawnCoinCloud, ShowArrow, etc.)
    /// </summary>
    public class TutorialActionExecutor : MonoBehaviour
    {
        private TutorialGuideArrowController _arrowController;
        private TutorialCoinCloudManager _coinCloudManager;
        private TutorialConditionChecker _conditionChecker;
        private TutorialCoinButtonPulse _buttonPulse;
        
        /// <summary>
        /// Event khi coin cloud hoàn thành - TutorialController sẽ subscribe để advance dialog
        /// </summary>
        public event System.Action OnCoinCloudComplete;
        
        /// <summary>
        /// Initialize với các controller reference
        /// </summary>
        public void Initialize(TutorialGuideArrowController arrowController, TutorialCoinCloudManager coinCloudManager, TutorialConditionChecker conditionChecker, TutorialCoinButtonPulse buttonPulse)
        {
            _arrowController = arrowController;
            _coinCloudManager = coinCloudManager;
            _conditionChecker = conditionChecker;
            _buttonPulse = buttonPulse;
            
            // Subscribe coin cloud finish event
            if (_coinCloudManager != null)
            {
                _coinCloudManager.OnCoinCloudFinished += OnCoinCloudFinished;
            }
        }
        
        /// <summary>
        /// Thực thi action khi bắt đầu dialog
        /// </summary>
        public void ExecuteOnStartAction(TutorialDialogLineSO line)
        {
            if (line == null || line.onStartAction == TutorialActionType.None) return;
            
            switch (line.onStartAction)
            {
                case TutorialActionType.SpawnCoinCloud:
                    _coinCloudManager?.SpawnCoinCloud(line.onStartPosition, line.onStartParam);
                    break;
                    
                case TutorialActionType.ShowArrowToTownCenter:
                    // Bắt đầu hiển thị arrow chỉ về TownCenter
                    _arrowController?.StartArrowToTownCenter();
                    break;

                case TutorialActionType.PulseCoinButton:
                    // Kích hoạt thông qua script coordinator
                    _buttonPulse?.StartPulse();
                    break;
            }
        }
        
        /// <summary>
        /// Xử lý khi coin cloud drop xong - fire event để TutorialController advance dialog
        /// </summary>
        private void OnCoinCloudFinished()
        {
            Debug.Log("[TutorialActionExecutor] CoinCloud finished - firing OnCoinCloudComplete event");
            OnCoinCloudComplete?.Invoke();
        }
        
        /// <summary>
        /// Dừng tất cả action đang chạy
        /// </summary>
        public void StopAllActions()
        {
            _arrowController?.StopAllArrows();
            _buttonPulse?.StopPulse();
        }
        
        /// <summary>
        /// Cleanup khi destroy
        /// </summary>
        public void Cleanup()
        {
            if (_coinCloudManager != null)
            {
                _coinCloudManager.OnCoinCloudFinished -= OnCoinCloudFinished;
            }
        }
    }
}

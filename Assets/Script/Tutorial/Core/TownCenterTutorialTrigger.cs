using UnityEngine;

namespace Script.Tutorial
{
    /// <summary>
    /// Gắn vào TownCenter để báo event cho tutorial
    /// </summary>
    public class TownCenterTutorialTrigger : MonoBehaviour
    {
        private bool _hasTriggered = false;
        private bool _hasUpgraded = false;
        
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_hasTriggered) return;
            
            // Kiểm tra player
            if (other.GetComponent<Script.Game.Player.PlayerBehavior>() != null)
            {
                _hasTriggered = true;
                TutorialConditionChecker.Instance?.TriggerCondition("TownCenterEntered");
                Debug.Log("[TownCenterTutorialTrigger] Player entered TownCenter!");
            }
        }
        
        /// <summary>
        /// Gọi từ TownCenterBehavior khi nâng cấp thành công
        /// </summary>
        public void OnTownCenterUpgraded()
        {
            if (_hasUpgraded) return;
            
            _hasUpgraded = true;
            TutorialConditionChecker.Instance?.TriggerCondition("TownCenterUpgraded");
            Debug.Log("[TownCenterTutorialTrigger] TownCenter upgraded!");
        }
    }
}

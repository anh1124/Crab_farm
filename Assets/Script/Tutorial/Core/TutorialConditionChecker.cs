using UnityEngine;
using System;
using System.Collections.Generic;
using Script.Game.Structures.TownCenter;

namespace Script.Tutorial
{
    /// <summary>
    /// Lắng nghe các event từ game và báo cho TutorialController
    /// </summary>
    public class TutorialConditionChecker : MonoBehaviour
    {
        public static TutorialConditionChecker Instance { get; private set; }
        
        // Events để TutorialController đăng ký
        public event Action<string> OnConditionMet;
        
        // Track state
        private HashSet<string> _metConditions = new HashSet<string>();
        
        // References để unsubscribe khi cần
        private TownCenterBehavior _currentTownCenter;
        private Script.Game.Item.Coin.CoinSlotVisual _currentCoinSlotVisual;
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        /// <summary>
        /// Đăng ký lắng nghe TownCenter events
        /// </summary>
        public void SetupTownCenterListener(TownCenterBehavior townCenter)
        {
            if (townCenter == null) return;
            
            // Unsubscribe cũ nếu có
            if (_currentTownCenter != null)
            {
                _currentTownCenter.OnTutorialPlayerEntered -= OnTownCenterPlayerEntered;
                _currentTownCenter.OnTutorialUpgraded -= OnTownCenterUpgraded;
            }
            
            _currentTownCenter = townCenter;
            
            // Subscribe events từ TownCenter
            _currentTownCenter.OnTutorialPlayerEntered += OnTownCenterPlayerEntered;
            _currentTownCenter.OnTutorialUpgraded += OnTownCenterUpgraded;
            
            Debug.Log("[TutorialConditionChecker] Subscribed to TownCenter events");
        }
        
        private void OnDestroy()
        {
            // Cleanup subscriptions
            if (_currentTownCenter != null)
            {
                _currentTownCenter.OnTutorialPlayerEntered -= OnTownCenterPlayerEntered;
                _currentTownCenter.OnTutorialUpgraded -= OnTownCenterUpgraded;
            }
            if (_currentCoinSlotVisual != null)
            {
                _currentCoinSlotVisual.OnSlotsShown -= OnCoinSlotsShown;
                _currentCoinSlotVisual.OnSlotsHidden -= OnCoinSlotsHidden;
            }
        }
        
        /// <summary>
        /// Đăng ký lắng nghe CoinSlotVisual trên TownCenter (hoặc bất kỳ building nào)
        /// </summary>
        public void SetupCoinSlotListener(Script.Game.Item.Coin.CoinSlotVisual coinSlotVisual)
        {
            if (coinSlotVisual == null) return;
            
            // Unsubscribe cũ nếu có
            if (_currentCoinSlotVisual != null)
            {
                _currentCoinSlotVisual.OnSlotsShown -= OnCoinSlotsShown;
                _currentCoinSlotVisual.OnSlotsHidden -= OnCoinSlotsHidden;
            }
            
            _currentCoinSlotVisual = coinSlotVisual;
            _currentCoinSlotVisual.OnSlotsShown += OnCoinSlotsShown;
            _currentCoinSlotVisual.OnSlotsHidden += OnCoinSlotsHidden;
            
            Debug.Log("[TutorialConditionChecker] Subscribed to CoinSlotVisual events");
        }
        
        private void OnCoinSlotsShown()
        {
            TriggerCondition("CoinSlotsShown");
        }
        
        private void OnCoinSlotsHidden()
        {
            // Có thể dùng nếu cần detect khi player rời xa
        }
        
        private void OnTownCenterPlayerEntered()
        {
            TriggerCondition("TownCenterEntered");
        }
        
        private void OnTownCenterUpgraded()
        {
            TriggerCondition("TownCenterUpgraded");
        }
        
        /// <summary>
        /// Báo rằng một condition đã được thỏa mãn
        /// </summary>
        public void TriggerCondition(string conditionName)
        {
            if (_metConditions.Contains(conditionName)) return;
            
            _metConditions.Add(conditionName);
            Debug.Log($"[TutorialConditionChecker] Condition met: {conditionName}");
            OnConditionMet?.Invoke(conditionName);
        }
        
        /// <summary>
        /// Kiểm tra xem condition đã được thỏa mãn chưa
        /// </summary>
        public bool IsConditionMet(string conditionName)
        {
            return _metConditions.Contains(conditionName);
        }
        
        /// <summary>
        /// Reset tất cả conditions (khi restart tutorial)
        /// </summary>
        public void ResetConditions()
        {
            _metConditions.Clear();
        }
    }
}

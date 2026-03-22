using System;
using UnityEngine;
using Script.Game.Player;
using Script.Interfaces;

namespace Script.Game.Item.Coin
{
    /*
    =========================================================
      COIN USER - ORCHESTRATOR
    =========================================================
      Luồng hoạt động:
      1. Player bước vào trigger → OnPlayerEntered() 
         → Hỏi ICoinUserOwner số coin cần thiết → CoinSlotVisual.SetupCoinSlots()
      2. PlayerController gọi HandlePlayerCoinInput() → TryInsertCoin()
      3. Bay coin: CoinSlotVisual sinh coin bay tới slot.
         Khi coin đến đích → TrackCoinArrival() → OnCollectionProgress()
      4. Khi đủ coin → *** FinalizeCollection() được gọi ***
                    → ICoinUserOwner.OnAllCoinsCollected() được gọi
      5. Behavior (Tree/Wall/ItemSeller/...) xử lý logic riêng (chặt cây, nâng cấp, v.v.)
    =========================================================
    */

    [RequireComponent(typeof(CoinSlotVisual))]
    [RequireComponent(typeof(CoinCollectionTracker))]
    public class CoinUser : MonoBehaviour, IPlayerInteractable
    {
        [Header("Components")]
        [SerializeField] private CoinSlotVisual visual;
        [SerializeField] private CoinCollectionTracker tracker;
        [SerializeField] private ICoinUserOwner owner;

        [Header("State")]
        [SerializeField] private bool playerTrigger;
        [SerializeField] private int cachedRequiredCoins;

        public bool playerTriggerActive => playerTrigger;

        private void Awake()
        {
            visual = GetComponent<CoinSlotVisual>();
            tracker = GetComponent<CoinCollectionTracker>();
        }

        private void Start()
        {
            // Auto-find owner on this GameObject
            owner = GetComponent<ICoinUserOwner>();
            if (owner == null)
            {
                Debug.LogWarning($"[CoinUser] {gameObject.name} missing ICoinUserOwner component!");
            }

            // Subscribe to tracker events
            tracker.OnTimeout += HandleCollectionTimeout;
            tracker.OnCollectionProgress += CheckUpgradeStatus;
        }

        private void OnDestroy()
        {
            if (tracker != null)
            {
                tracker.OnTimeout -= HandleCollectionTimeout;
                tracker.OnCollectionProgress -= CheckUpgradeStatus;
            }
        }

        // --- IPlayerInteractable ---
        
        public void OnPlayerEnter(PlayerBehavior player)
        {
            OnPlayerEntered();
        }

        public void OnPlayerExit(PlayerBehavior player)
        {
            if (tracker.IsCollectingCoins)
            {
                ReleaseInsertedCoins();
            }
            OnPlayerExited();
        }

        public bool CanInteract()
        {
            return owner != null ? owner.CanReceiveCoins() : true;
        }

        // --- Core Logic ---

        private void OnPlayerEntered()
        {
            playerTrigger = true;
            RefreshSlotVisibility();
        }

        private void OnPlayerExited()
        {
            playerTrigger = false;
            visual.HideSlots();
        }

        public void RefreshSlotVisibility()
        {
            if (!playerTrigger) return;

            if (owner != null)
            {
                cachedRequiredCoins = owner.GetRequiredCoinCount();
                
                if (owner.CanReceiveCoins() && cachedRequiredCoins > 0)
                {
                    visual.SetupCoinSlots(cachedRequiredCoins);
                }
                else
                {
                    visual.HideSlots();
                }
            }
        }

        // Called by PlayerController
        public bool ReceiveCoin(GameObject player)
        {
            return HandlePlayerCoinInput(player);
        }

        public bool HandlePlayerCoinInput(GameObject player)
        {
            if (owner == null) return false;

            cachedRequiredCoins = owner.GetRequiredCoinCount();

            if (!owner.CanReceiveCoins()) return false;
            if (!visual.HasAvailableSlot(cachedRequiredCoins)) return false;

            PlayerBehavior playerBehavior = player.GetComponent<PlayerBehavior>();
            if (playerBehavior == null || playerBehavior.Coin <= 0) return false;

            return TryInsertCoin(playerBehavior);
        }

        private bool TryInsertCoin(PlayerBehavior playerBehavior)
        {
            playerBehavior.SubtractCoin(1);
            
            // Start the visual transfer
            visual.SpawnAndMoveCoin(playerBehavior.gameObject, OnCoinArrivedAtSlot);
            
            // Start the collection timeout tracker
            tracker.StartCollection();
            
            return true;
        }

        private void OnCoinArrivedAtSlot(GameObject coin)
        {
            // Khi visual báo coin đã đáp vào slot, ta báo cho tracker biết
            tracker.TrackCoinArrival();
        }

        private void CheckUpgradeStatus(int totalCollectedData)
        {
            if (owner != null) cachedRequiredCoins = owner.GetRequiredCoinCount();
            
            if (totalCollectedData >= cachedRequiredCoins)
            {
                FinalizeCollection();
            }
        }

        private void FinalizeCollection()
        {
            visual.StopAllCoinCoroutines();
            visual.ClearTransitVisuals();
            visual.HideSlots();
            
            tracker.ResetCollection();

            if (owner != null)
            {
                owner.OnAllCoinsCollected();
            }
        }

        private void ReleaseInsertedCoins()
        {
            visual.StopAllCoinCoroutines();
            visual.ReleaseFloatingCoins();
            tracker.ResetCollection();
        }

        private void HandleCollectionTimeout()
        {
            ReleaseInsertedCoins();
            // Optional: Hide slots or refresh visibility
            RefreshSlotVisibility();
        }

        // Được gọi bởi các Behavior (như Wall, ItemSeller) khi chúng đã xử lý xong và muốn mở lại CoinUser
        public void ResetUpgradeState()
        {
            visual.StopAllCoinCoroutines();
            visual.ClearTransitVisuals();
            tracker.ResetCollection();
            
            RefreshSlotVisibility();
        }

        // Backward compatibility method
        public void SetBuildingInfo(Script.Manager.Building.BuildingType type, int tier)
        {
            // Can be left empty or log for debug purposes since we don't store BuildingType locally anymore
            // Variables needed for building upgrades should naturally be accessed inside the relevant Behavior
        }

        public string GetTriggerDebugInfo(out bool shouldShowSlots)
        {
            shouldShowSlots = false;
            bool canRecv = owner != null && owner.CanReceiveCoins();
            int req = owner != null ? owner.GetRequiredCoinCount() : 0;
            
            string result = $"CoinUser[{gameObject.name}]: CanRecv={canRecv}, Required={req}, PlayerTrigger={playerTrigger}";

            if (canRecv && req > 0)
            {
                shouldShowSlots = true;
                result += " | WILL SHOW SLOTS";
            }
            
            return result;
        }
    }
}

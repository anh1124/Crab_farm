using System.Collections.Generic;
using Script.Interfaces;
using Script.UI;
using UnityEngine;
using Script.Controller.GUI_Controller.PlayerGui;

namespace Script.Game.Player
{
    /// <summary>
    /// PlayerInteraction quản lý tự động dò tìm và tương tác với các IPlayerInteractable xung quanh.
    /// Tách rời khỏi PlayerBehavior để giảm tải logic.
    /// </summary>
    [RequireComponent(typeof(PlayerBehavior))]
    public class PlayerInteraction : MonoBehaviour
    {
        private PlayerBehavior playerBehavior;
        [SerializeField] private bool isDebug = false;

        // === INTERACTION LOGIC ===
        private List<IPlayerInteractable> interactablesInRange = new List<IPlayerInteractable>();
        private HashSet<IPlayerInteractable> interactablesWithEnterCalled = new HashSet<IPlayerInteractable>();
        public IPlayerInteractable currentInteractable;

        private float _interactionCheckTimer = 0f;
        private const float InteractionCheckInterval = 0.1f;

        private void Awake()
        {
            playerBehavior = GetComponent<PlayerBehavior>();
        }

        private void Update()
        {
            _interactionCheckTimer += Time.deltaTime;
            if (_interactionCheckTimer >= InteractionCheckInterval)
            {
                _interactionCheckTimer = 0f;
                ValidateInteraction();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Tránh va chạm với trigger của chính mình hoặc con cái của mình nếu cần
            if (other.isTrigger && (other.CompareTag(Script.Utils.GameTags.Player) || other.transform.IsChildOf(transform))) return;

            // HPText: cùng điều kiện overlap với IPlayerInteractable (tường solid vẫn báo từ phía player).
            var hpText = other.GetComponentInParent<HPText>(true);
            if (hpText != null)
            {
                hpText.OnPlayerProximityEnter();
            }

            // Xử lý IPlayerInteractable - lấy tất cả trên GameObject
            var interactables = other.GetComponents<IPlayerInteractable>();
            foreach (var interactable in interactables)
            {
                if (interactable != null && !interactablesInRange.Contains(interactable))
                {
                    interactablesInRange.Add(interactable);
                    // Call OnPlayerEnter immediately for all interactables on this GameObject
                    if (!interactablesWithEnterCalled.Contains(interactable))
                    {
                        interactable.OnPlayerEnter(playerBehavior);
                        interactablesWithEnterCalled.Add(interactable);
                    }
                }
            }
            UpdateCurrentInteractable();
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // Xử lý IPlayerInteractable - lấy tất cả trên GameObject
            var interactables = other.GetComponents<IPlayerInteractable>();
            foreach (var interactable in interactables)
            {
                if (interactable != null && interactablesInRange.Contains(interactable))
                {
                    interactablesInRange.Remove(interactable);
                    // Remove from tracking set and call OnPlayerExit
                    interactablesWithEnterCalled.Remove(interactable);
                    interactable.OnPlayerExit(playerBehavior);

                    // Nếu đang tương tác với đối tượng này thì tắt đi
                    if (currentInteractable == interactable)
                    {
                        currentInteractable = null;
                    }

                    UpdateCurrentInteractable();
                }
            }
        }

        private void UpdateCurrentInteractable()
        {
            // Clean up destroyed interactables just in case
            interactablesInRange.RemoveAll(i => i == null || (i is MonoBehaviour mb && mb == null));

            if (interactablesInRange.Count == 0)
            {
                if (currentInteractable != null)
                {
                    currentInteractable.OnPlayerExit(playerBehavior);
                    currentInteractable = null;
                }
                return;
            }

            // Find the most recent valid interactable
            IPlayerInteractable candidate = null;
            for (int i = interactablesInRange.Count - 1; i >= 0; i--)
            {
                if (interactablesInRange[i].CanInteract())
                {
                    candidate = interactablesInRange[i];
                    break;
                }
            }

            // Switch to the new candidate if it's different
            if (currentInteractable != candidate)
            {
                if (currentInteractable != null)
                {
                    currentInteractable.OnPlayerExit(playerBehavior);
                }

                currentInteractable = candidate;

                if (currentInteractable != null)
                {
                    currentInteractable.OnPlayerEnter(playerBehavior);
                }
            }

            // --- Cập nhật hiệu ứng Pulse cho nút UseCoin trên GUI ---
            Script.Game.Item.Coin.CoinUser activeCoinUser = null;
            if (currentInteractable is MonoBehaviour mb)
            {
                activeCoinUser = mb.GetComponent<Script.Game.Item.Coin.CoinUser>();
            }

            if (isDebug)
            {
                if (currentInteractable != null)
                {
                    string stateInfo = activeCoinUser != null ? $"IsInteractionActive={activeCoinUser.IsInteractionActive}" : "No CoinUser Component";
                    Debug.Log($"[PlayerInteraction] Current: {currentInteractable.GetType().Name}, FoundCoinUser: {activeCoinUser != null}, {stateInfo}");
                }
            }

            if (activeCoinUser != null && activeCoinUser.IsInteractionActive)
            {
                if (isDebug) Debug.Log($"[PlayerInteraction] TRIGGER PULSE START: {activeCoinUser.gameObject.name}");
                PlayerGui.Instance?.StartGoldPulse();
            }
            else
            {
                PlayerGui.Instance?.StopGoldPulse();
            }
        }



        
        // Validate interactable periodically or in Update
        private void ValidateInteraction()
        {
             // Continuously check if we need to switch or drop the current interactable
             // without permanently removing it from the physically-in-range list.
             UpdateCurrentInteractable();
        }
        
        public void LogCurrentInteractables()
        {
            Debug.Log($"[PlayerInteraction-DEBUG] Current interactables in range count: {interactablesInRange.Count}");
            for (int i = 0; i < interactablesInRange.Count; i++)
            {
                var interactable = interactablesInRange[i];
                var go = (interactable as MonoBehaviour)?.gameObject;
                Debug.Log($"[PlayerInteraction-DEBUG] [{i}] Name: {go?.name}, InstanceID: {go?.GetInstanceID()}, Pos: {go?.transform.position}");
            }
        }
    }
}

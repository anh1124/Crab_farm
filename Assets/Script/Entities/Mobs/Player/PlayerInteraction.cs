using System.Collections.Generic;
using Script.Interfaces;
using UnityEngine;

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

        // === INTERACTION LOGIC ===
        private List<IPlayerInteractable> interactablesInRange = new List<IPlayerInteractable>();
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

            // Xử lý IPlayerInteractable
            IPlayerInteractable interactable = other.GetComponent<IPlayerInteractable>();
            if (interactable != null)
            {
                if (!interactablesInRange.Contains(interactable))
                {
                    interactablesInRange.Add(interactable);
                    UpdateCurrentInteractable();
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // Xử lý IPlayerInteractable
            IPlayerInteractable interactable = other.GetComponent<IPlayerInteractable>();
            if (interactable != null)
            {
                if (interactablesInRange.Contains(interactable))
                {
                    interactablesInRange.Remove(interactable);
                    
                    // Nếu đang tương tác với đối tượng này thì tắt đi
                    if (currentInteractable == interactable)
                    {
                        interactable.OnPlayerExit(playerBehavior);
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
        }
        
        // Validate interactable periodically or in Update
        private void ValidateInteraction()
        {
             // Continuously check if we need to switch or drop the current interactable
             // without permanently removing it from the physically-in-range list.
             UpdateCurrentInteractable();
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Script.GUI_Controller.PlayerGui
{
    public class PlayerGui : MonoBehaviour
    {
        public static PlayerGui Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        [Header("Move Button")]
        public Button moveLeft;
        public Button moveRight;
        
        
        [Header("skill Button")]
        public Button skill1;
        public Button skill2;
        public Button skill3;
    
        [Header("item Button")]
        public Button itemSlot1;
        public Button itemSlot2;
        
        [Header("other Button")]
        public Button useCoin;
        // Events
        
        
        private void Start()
        {
            // Add event triggers for move buttons
            AddEventTriggers(moveLeft, PlayerController.Instance.MoveLeftDown, PlayerController.Instance.MoveLeftUp);
            AddEventTriggers(moveRight, PlayerController.Instance.MoveRightDown, PlayerController.Instance.MoveRightUp);
            
            AddEventTriggers(useCoin, PlayerController.Instance.UseCoinDownAction, PlayerController.Instance.UseCoinUpAction);
        }
        private void AddEventTriggers(Button button, Action onPointerDown, Action onPointerUp)
        {
            EventTrigger trigger = button.gameObject.AddComponent<EventTrigger>();

            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerDown
            };
            pointerDownEntry.callback.AddListener((eventData) => { onPointerDown(); });
            trigger.triggers.Add(pointerDownEntry);

            EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerUp
            };
            pointerUpEntry.callback.AddListener((eventData) => { onPointerUp(); });
            trigger.triggers.Add(pointerUpEntry);
        }
    }
}

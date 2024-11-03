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
        public Action OnSkill3PressedACT;//debug btn
        
        private void Start()
        {
            
            //cái này phải sửa lại,không được đặt nó vào hàm start
            // Add event triggers for move buttons
            AddEventTriggers(moveLeft, PlayerController.Instance.MoveLeftDown, PlayerController.Instance.MoveLeftUp);
            AddEventTriggers(moveRight, PlayerController.Instance.MoveRightDown, PlayerController.Instance.MoveRightUp);
            AddEventTriggers(useCoin, PlayerController.Instance.UseCoinDownAction, PlayerController.Instance.UseCoinUpAction);
            skill3.onClick.AddListener(OnSkill3Pressed);
        }
        // Hàm được gọi khi skill3 được nhấn
        private void OnSkill3Pressed()
        {
            Debug.Log("Skill 3 button pressed!");

            // Gọi event nếu có class nào đó đã đăng ký lắng nghe
            OnSkill3PressedACT?.Invoke();
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

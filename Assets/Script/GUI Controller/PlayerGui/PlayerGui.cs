using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Script.Data_Model;
using Script.PlayerController;

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
        public Action OnSkill2PressedACT;//spawn vagrant btn
        
        private void Start()
        {
            
            //cái này phải sửa lại,không được đặt nó vào hàm start
            // Add event triggers for move buttons
            AddEventTriggers(moveLeft, PlayerController.PlayerController.Instance.MoveLeftDown, PlayerController.PlayerController.Instance.MoveLeftUp);
            AddEventTriggers(moveRight, PlayerController.PlayerController.Instance.MoveRightDown, PlayerController.PlayerController.Instance.MoveRightUp);
            AddEventTriggers(useCoin, PlayerController.PlayerController.Instance.UseCoinDownAction, PlayerController.PlayerController.Instance.UseCoinUpAction);
            
            // Add listeners for skill buttons
            skill2.onClick.AddListener(OnSkill2Pressed);
            skill3.onClick.AddListener(OnSkill3Pressed);
        }
        
        // Hàm được gọi khi skill2 được nhấn
        private void OnSkill2Pressed()
        {
            Debug.Log("Skill 2 button pressed! Spawning Vagrant...");
            
            // Get player position
            Vector2 playerPosition = PlayerController.PlayerController.Instance.player.transform.position;
            
            // Spawn the vagrant slightly offset from the player
            if (PrefabData.Instance.vagrant != null)
            {
                Debug.Log("Vagrant prefab tag before instantiate: " + PrefabData.Instance.vagrant.tag);
                
                // Đảm bảo prefab có tag đúng
                if (PrefabData.Instance.vagrant.tag != "Vagrant")
                {
                    Debug.LogWarning("Prefab không có tag 'Vagrant', đang set lại tag cho prefab");
                    PrefabData.Instance.vagrant.tag = "Vagrant";
                }
                
                // Calculate spawn position (2 units in front of the player based on direction)
                float direction = PlayerController.PlayerController.Instance.player.transform.localScale.x;
                Vector2 spawnPosition = new Vector2(playerPosition.x + (direction * 2), playerPosition.y);
                
                // Use SetUpContainer to spawn and organize the vagrant
                GameObject vagrant = SetUpContainer.Instance.InstanceAndMoveObjectToContainer(
                    PrefabData.Instance.vagrant, 
                    spawnPosition
                );
                
                if (vagrant != null)
                {
                    // Đảm bảo đối tượng đã tạo có tag Vagrant đúng
                    if (vagrant.tag != "Vagrant")
                    {
                        Debug.LogWarning("Vagrant đã tạo không có tag 'Vagrant', đang gán lại tag...");
                        vagrant.tag = "Vagrant";
                        // Set lại container
                        SetUpContainer.Instance.MoveObjectToContainer(vagrant);
                    }
                    
                    Debug.Log("Vagrant spawned successfully at position: " + spawnPosition);
                    Debug.Log("Vagrant tag: " + vagrant.tag);
                    Debug.Log("Vagrant parent container: " + vagrant.transform.parent.name);
                }
                else
                {
                    Debug.LogError("Failed to spawn Vagrant!");
                }
            }
            else
            {
                Debug.LogError("Vagrant prefab not found in PrefabData.Instance.vagrant!");
            }
            
            // Gọi event nếu có class nào đó đã đăng ký lắng nghe
            OnSkill2PressedACT?.Invoke();
        }
        
        // Hàm được gọi khi skill3 được nhấn
        private void OnSkill3Pressed()
        {
            Debug.Log("Skill 3 button pressed!");
            
            // Gọi hàm sửa container
            FixContainers();

            // Gọi event nếu có class nào đó đã đăng ký lắng nghe
            OnSkill3PressedACT?.Invoke();
        }
        
        // Hàm để fix lỗi container không nằm đúng vị trí
        private void FixContainers()
        {
            Debug.Log("Fixing containers structure...");
            SetUpContainer.Instance.ResetupAllContainers();
            Debug.Log("Containers fixed!");
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

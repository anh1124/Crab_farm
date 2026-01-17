using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// For PlayerController.Instance

namespace Script.Controller.GUI_Controller.PlayerGui
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
        public Action OnSkill1PressedACT; // New: Event for Skill 1
        
        private void Start()
        {
            // Add event triggers for move buttons
            AddEventTriggers(moveLeft, PlayerController.PlayerController.Instance.MoveLeftDown, PlayerController.PlayerController.Instance.MoveLeftUp);
            AddEventTriggers(moveRight, PlayerController.PlayerController.Instance.MoveRightDown, PlayerController.PlayerController.Instance.MoveRightUp);
            AddEventTriggers(useCoin, PlayerController.PlayerController.Instance.UseCoinDownAction, PlayerController.PlayerController.Instance.UseCoinUpAction);
            
            // Add listeners for skill buttons
            skill1.onClick.AddListener(OnSkill1Pressed);
            skill2.onClick.AddListener(OnSkill2Pressed);
            skill3.onClick.AddListener(OnSkill3Pressed);
        }
        
        // Hàm được gọi khi skill2 được nhấn
        private void OnSkill2Pressed()
        {
            Debug.Log("[PlayerGui] OnSkill2Pressed called!");
            if (PlayerController.PlayerController.Instance?.playerBehavior == null)
            {
                Debug.LogError("[PlayerGui] Player not spawned yet!");
                return;
            }
            
            var skillManager = PlayerController.PlayerController.Instance.playerBehavior.GetComponent<Script.Game.PlayerCharacter.PlayerSkillManager>();
            skillManager?.ExecuteSkill(1);
            OnSkill2PressedACT?.Invoke();
        }
        
        // Hàm được gọi khi skill3 được nhấn
        private void OnSkill3Pressed()
        {
            Debug.Log("[PlayerGui] OnSkill3Pressed called!");
            if (PlayerController.PlayerController.Instance?.playerBehavior == null)
            {
                Debug.LogError("[PlayerGui] Player not spawned yet!");
                return;
            }
            
            var skillManager = PlayerController.PlayerController.Instance.playerBehavior.GetComponent<Script.Game.PlayerCharacter.PlayerSkillManager>();
            skillManager?.ExecuteSkill(2);
            OnSkill3PressedACT?.Invoke();
        }

        // Hàm được gọi khi skill1 được nhấn
        private void OnSkill1Pressed()
        {
            Debug.Log("[PlayerGui] OnSkill1Pressed called!");
            if (PlayerController.PlayerController.Instance?.playerBehavior == null)
            {
                Debug.LogError("[PlayerGui] Player not spawned yet!");
                return;
            }
            
            var skillManager = PlayerController.PlayerController.Instance.playerBehavior.GetComponent<Script.Game.PlayerCharacter.PlayerSkillManager>();
            skillManager?.ExecuteSkill(0);
            OnSkill1PressedACT?.Invoke();
        }
        
        // Hàm để fix lỗi container không nằm đúng vị trí
        // private void FixContainers()
        // {
        //     Debug.Log("Fixing containers structure...");
        //     EntitySaveManager.Instance.ResetupAllContainers();
        //     Debug.Log("Containers fixed!");
        // }
        
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
        
        /*//demo spawn vagrant
          // Đảm bảo prefabWall có tag đúng
                if (PrefabData.Instance.vagrant.tag != "Vagrant")
                {
                    Debug.LogWarning("Prefab không có tag 'Vagrant', đang set lại tag cho prefabWall");
                    PrefabData.Instance.vagrant.tag = "Vagrant";
                }
                
                // Calculate spawn position (2 units in front of the player based on direction)
                float direction = PlayerController.PlayerController.Instance.player.transform.localScale.x;
                Vector2 spawnPosition = new Vector2(playerPosition.x + (direction * 2), playerPosition.y);
                
                // Use EntitySaveManager to spawn and organize the vagrant
                GameObject vagrant = EntitySaveManager.Instance.InstanceAndMoveObjectToContainer(
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
                        EntitySaveManager.Instance.MoveObjectToContainer(vagrant);
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
                Debug.LogError("Vagrant prefabWall not found in PrefabData.Instance.vagrant!");
            }
         */
    }
}

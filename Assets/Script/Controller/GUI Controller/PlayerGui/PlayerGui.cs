using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Script.Game.Mobs.MobsManager;

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
            if(skill1 != null) skill1.onClick.AddListener(OnSkill1Pressed);
            if(skill2 != null) skill2.onClick.AddListener(OnSkill2Pressed);
            if(skill3 != null) skill3.onClick.AddListener(OnSkill3Pressed);
            
            // Add listeners for item buttons (Debug Time)
            if(itemSlot1 != null) itemSlot1.onClick.AddListener(OnItem1Pressed);
            if(itemSlot2 != null) itemSlot2.onClick.AddListener(OnItem2Pressed);
        }
        
        // --- DEBUG TIME CONTROLS ---
        
        // Item Slot 1 -> Night (19:00)
        private void OnItem1Pressed()
        {
            Debug.Log("[PlayerGui] Item 1 Pressed -> Set Time to NIGHT");
            if (Script.Manager.WorldManager.GameTimeManager.Instance != null)
            {
                Script.Manager.WorldManager.GameTimeManager.Instance.SetTimeToNight();
            }
        }

        // Item Slot 2 -> Morning (07:00)
        private void OnItem2Pressed()
        {
            Debug.Log("[PlayerGui] Item 2 Pressed -> Set Time to MORNING");
            if (Script.Manager.WorldManager.GameTimeManager.Instance != null)
            {
                Script.Manager.WorldManager.GameTimeManager.Instance.SetTimeToMorning();
            }
        }

        // Hàm được gọi khi skill2 được nhấn - DEBUG: SPAWN VAGRANT
        private void OnSkill2Pressed()
        {
            Debug.Log("[PlayerGui] OnSkill2Pressed called - Spawning Vagrant!");
            if (PlayerController.PlayerController.Instance?.player == null)
            {
                Debug.LogError("[PlayerGui] Player instance not found!");
                return;
            }
            
            // Lấy vị trí trước mặt player
            float direction = PlayerController.PlayerController.Instance.player.transform.localScale.x;
            Vector3 spawnPos = PlayerController.PlayerController.Instance.player.transform.position + new Vector3(direction * 2, 2, 0); // +2Y to drop from sky

            if (VagrantManager.Instance != null)
            {
                VagrantManager.Instance.SpawnVagrants(1, null, spawnPos);
                Debug.Log($"[PlayerGui] Spawned vagrant at {spawnPos}");
            }
            else
            {
                Debug.LogError("[PlayerGui] VagrantManager instance not found!");
            }
            
            /* Old skill logic
            var skillManager = PlayerController.PlayerController.Instance.playerBehavior.GetComponent<Script.Game.PlayerCharacter.PlayerSkillManager>();
            skillManager?.ExecuteSkill(1);
            */
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
        
    
    }
}

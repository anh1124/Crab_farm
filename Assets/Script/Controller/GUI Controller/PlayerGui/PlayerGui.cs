using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Script.Controller;
using Script.Game.Mobs.MobsManager;
using Script.Game.Player;

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
        
        // Cache SkillManager một lần duy nhất để tránh GetComponent mỗi lần bấm nút
        private Script.Game.PlayerCharacter.PlayerSkillManager _cachedSkillManager;
        
        private void Start()
        {
            // Add event triggers for move buttons
            // Redirect mobile UI buttons → InputManager (unified input state)
            AddEventTriggers(moveLeft,
                () => InputManager.Instance.SetMoveLeft(true),
                () => InputManager.Instance.SetMoveLeft(false));

            AddEventTriggers(moveRight,
                () => InputManager.Instance.SetMoveRight(true),
                () => InputManager.Instance.SetMoveRight(false));

            AddEventTriggers(useCoin,
                () => PlayerController.PlayerController.Instance.UseCoinDownAction(),
                () => PlayerController.PlayerController.Instance.UseCoinUpAction());
            
            // Add listeners for skill buttons
            if(skill1 != null) skill1.onClick.AddListener(OnSkill1Pressed);
            if(skill2 != null) skill2.onClick.AddListener(OnSkill2Pressed);
            if(skill3 != null) skill3.onClick.AddListener(OnSkill3Pressed);
            
            // Add listeners for item buttons (Debug Time)
            if(itemSlot1 != null) itemSlot1.onClick.AddListener(OnItem1Pressed);
            if(itemSlot2 != null) itemSlot2.onClick.AddListener(OnItem2Pressed);
            
            // Cache SkillManager một lần. Được assign bởi SetUpControllerPlayer() sau khi Player spawn.
            _cachedSkillManager = PlayerController.PlayerController.Instance?.SkillManager;
        }

        /// <summary>
        /// Gọi sau khi Player spawn và SetUpControllerPlayer() hoàn tất để làm mới cache.
        /// VD: PlayerSpawnManager gọi hàm này sau khi spawn.
        /// </summary>
        public void RefreshSkillManagerCache()
        {
            _cachedSkillManager = PlayerController.PlayerController.Instance?.SkillManager;
            Debug.Log($"[PlayerGui] SkillManager cache refreshed: {(_cachedSkillManager != null ? "OK" : "NULL")}");

            InitSkillButtonsFromExisting();
        }
        
        /// <summary>
        /// Lấy SkillButtonUI từ các Button đã khai báo sẵn (skill1, skill2, skill3)
        /// và gọi Init() trên từng cái theo slot index.
        /// </summary>
        private void InitSkillButtonsFromExisting()
        {
            if (_cachedSkillManager == null) return;

            var behavior = PlayerController.PlayerController.Instance?.playerBehavior;
            var buttons  = new Button[] { skill1, skill2, skill3 };

            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;

                var ui = buttons[i].GetComponent<SkillButtonUI>();
                if (ui != null)
                {
                    ui.Init(_cachedSkillManager, behavior, i);
                    Debug.Log($"[PlayerGui] SkillButtonUI slot {i} initialized.");
                }
                else
                {
                    Debug.LogWarning($"[PlayerGui] skill{i+1} không có component SkillButtonUI!");
                }
            }
        }
        

        
        // --- DEBUG TIME CONTROLS ---
        
        // Item Slot 1 -> Night (19:00)
        private void OnItem1Pressed()
        {
            var gtm = FindObjectOfType<Script.Manager.WorldManager.GameTimeManager>();
            if (gtm != null)
            {
                gtm.SetTimeToNight();
            }
        }

        // Item Slot 2 -> Morning (07:00)
        private void OnItem2Pressed()
        {
            var gtm = FindObjectOfType<Script.Manager.WorldManager.GameTimeManager>();
            if (gtm != null)
            {
                gtm.SetTimeToMorning();
            }
        }

        // Hàm được gọi khi skill2 được nhấn - SPAWN VAGRANT (Hệ thống mới)
        private void OnSkill2Pressed()
        {
            Debug.Log("[PlayerGui] OnSkill2Pressed called!");
            if (PlayerController.PlayerController.Instance?.playerBehavior == null)
            {
                Debug.LogError("[PlayerGui] Player not spawned yet!");
                return;
            }
            _cachedSkillManager?.ExecuteSkill(1);
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
            _cachedSkillManager?.ExecuteSkill(2);
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
            _cachedSkillManager?.ExecuteSkill(0);
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

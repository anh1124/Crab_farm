using System;
using Script.Data_Model.Status;
using Script.Data_Model.Config;
using Script.Data_Model.Interfaces;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using UnityEngine;
using Script.Game.PlayerCharacter;
using Script.Controller.PlayerController;
using Art.Resources.Character.PlayerCharactor.Spear_Man;
using Script.Game.Item.Coin;

namespace Script.Game.Player
{
    // Chứa logic gameplay của Player, sử dụng PlayerStatus để lưu trữ trạng thái
    
    //[TODO] thêm script kiểm tra script player đã đủ chưa
    
    [RequireComponent(typeof(PlayerStatus))]
    [RequireComponent(typeof(PlayerCombat))]
    [RequireComponent(typeof(PlayerInteraction))]
    [RequireComponent(typeof(PlayerItemHandler))]
    [RequireComponent(typeof(PlayerUISetup))]
    [RequireComponent(typeof(PlayerStatRegenerator))]
    public class PlayerBehavior : MonoBehaviour, ISaveable
    {
        [Header("Player Status Data")]
        public PlayerStatus status;

        [Header("Default Config")]
        [Tooltip("ScriptableObject chứa giá trị mặc định. Tạo qua: Create > Config > PlayerConfig")]
        public PlayerConfig defaultConfig;
        
        [Header("Animation Reference")]
        public SpearManAnimationController playerAnim; 

        // Components References (Cached for bridge properties)
        private PlayerCombat _combat;
        private PlayerInteraction _interaction;

        // Flag: true nếu RestoreData() đã chạy thành công → bỏ qua ApplyConfig trong Awake
        private bool _dataRestoredFromSave = false;

        [Serializable]
        public class PlayerStatusData
        {
            public float maxHp;
            public float currentHp;
            public int coin;
            public float moveSpeed;
            public float positionX;
            public float positionY;
            public bool isGround;
            
            // NEW: Resource data for save/load
            public float currentMana;
        }

        private void Awake()
        {
            // Register to PlayerController
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.playerBehavior = this;
                Debug.Log("[PlayerBehavior] Registered to PlayerController.");
            }
            else
            {
                Debug.LogError("[PlayerBehavior] PlayerController.Instance is null! Cannot register.");
            }
            
            // Đã chuyển sang sử dụng RequireComponent cho các dependency
            status = GetComponent<PlayerStatus>();


            // Cache references for bridge properties
            _combat = GetComponent<PlayerCombat>();
            _interaction = GetComponent<PlayerInteraction>();

            // Tự động tìm config nếu chưa gán trong Inspector (Ưu tiên logic Resources)
            if (defaultConfig == null)
            {
                defaultConfig = Resources.Load<PlayerConfig>("Data/Player/PlayerConfig");
                if (defaultConfig != null)
                    Debug.Log("[PlayerBehavior] Successfully auto-loaded PlayerConfig from Resources.");
                else
                    Debug.LogWarning("[PlayerBehavior] Could not find PlayerConfig at: Assets/Art/Resources/Data/Player/PlayerConfig.asset");
            }

            // Khởi tạo giá trị mặc định từ Config — chỉ khi chưa load từ save.
            if (!_dataRestoredFromSave)
            {
                if (defaultConfig != null)
                {
                    ApplyConfig(defaultConfig);
                    Debug.Log("[PlayerBehavior] Applied default config (no save data found).");
                }
                else
                {
                    Debug.LogWarning("[PlayerBehavior] defaultConfig is null! Please assign PlayerConfig asset in Inspector.");
                }
            }
        }

        /// <summary>
        /// Áp dụng giá trị mặc định từ PlayerConfig.
        /// Gọi khi: (1) game mới, (2) new game, (3) debug reset.
        /// </summary>
        public void ApplyConfig(PlayerConfig config)
        {
            status.maxHp        = config.maxHp;
            status.currentHp    = config.maxHp;      // Bắt đầu full HP
            status.moveSpeed    = config.moveSpeed;
            status.hpRegenRate  = config.hpRegenRate; // New: HP regen
            status.maxCoins     = config.maxCoins;
            status.maxMana      = config.maxMana;
            status.currentMana  = config.maxMana;    // Bắt đầu full Mana
            status.manaRegenRate = config.manaRegenRate;
        }



        private void OnDestroy()
        {
            // Unregister from PlayerController
            if (PlayerController.Instance != null && PlayerController.Instance.playerBehavior == this)
            {
                PlayerController.Instance.playerBehavior = null;
                Debug.Log("[PlayerBehavior] Unregistered from PlayerController.");
            }
        }
        
        // === RESOURCE HELPERS ===
        public bool CanUseSkill(float manaCost)
        {
            return status.currentMana >= manaCost;
        }

        public void ConsumeSkillResources(float manaCost)
        {
            status.currentMana -= manaCost;
            
            // Validate không âm
            status.currentMana = Mathf.Max(0f, status.currentMana);
        }

        public int Coin 
        { 
            get => status.coin; 
            set => status.coin = value; 
        }

        // --- BRIDGE PROPERTIES (For backward compatibility) ---
        public bool HasHp => _combat != null ? _combat.HasHp : status.currentHp > 0;
        public Script.Interfaces.IPlayerInteractable currentInteractable => _interaction != null ? _interaction.currentInteractable : null;

        public int MaxCoins 
        { 
            get => status.maxCoins; 
            set => status.maxCoins = value; 
        }

        public void SubtractCoin(int amount)
        {
            if (amount <= 0) return;
            status.coin = Mathf.Max(0, status.coin - amount);
        }

        public void AddCoin(int amount)
        {
            if (amount <= 0) return;
            status.coin += amount;
        }


        public string GetPrefabID() => "player";

        public object CaptureData()
        {
            // Resource data is now in status, no need for ResourceManager
            
            return new PlayerStatusData
            {
                maxHp = status.maxHp,
                currentHp = status.currentHp,
                coin = status.coin,
                moveSpeed = status.moveSpeed,
                positionX = transform.position.x,
                positionY = transform.position.y,
                isGround = status.isGround,
                
                // Save current resources directly from status
                currentMana = status.currentMana
            };
        }

        public void RestoreData(object data)
        {
            PlayerStatusData loaded = data as PlayerStatusData;
            if (loaded == null) return;

            // Đánh dấu đã load từ save — Awake sẽ bỏ qua ApplyConfig
            _dataRestoredFromSave = true;

            status.maxHp        = loaded.maxHp;
            status.currentHp    = loaded.currentHp;
            status.coin         = loaded.coin;
            status.moveSpeed    = loaded.moveSpeed;
            status.isGround     = loaded.isGround;
            transform.position  = new Vector3(loaded.positionX, loaded.positionY, 0);
            status.currentMana  = loaded.currentMana;

            Debug.Log($"[PlayerBehavior] Restored save data. HP={status.currentHp}/{status.maxHp}, Mana={status.currentMana}/{status.maxMana}");
        }



    }
}
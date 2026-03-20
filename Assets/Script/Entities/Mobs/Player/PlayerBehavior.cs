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
using Script.Game.PlayerCharacter.Skills;

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
            status.HP.SetMax(config.maxHp, refillCurrent: true);
            status.moveSpeed    = config.moveSpeed;
            status.hpRegenRate  = config.hpRegenRate; // New: HP regen
            status.maxCoins     = config.maxCoins;
            status.Mana.SetMax(config.maxMana, refillCurrent: true);
            status.manaRegenRate = config.manaRegenRate;
        }

        private void Start()
        {
            // Tìm kiếm tất cả ParallaxLayer và assign target follow là transform của player
            Script.Manager.WorldManager.ParallaxLayer[] parallaxLayers = FindObjectsOfType<Script.Manager.WorldManager.ParallaxLayer>();
            foreach (var layer in parallaxLayers)
            {
                layer.followTarget = this.transform;
            }
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
        
        // === RESOURCE HELPERS (Multi-Type) ===
        public bool CanAfford(ResourceType type, float amount)
        {
            switch (type)
            {
                case ResourceType.Mana:
                    return status.Mana.Current >= amount;
                case ResourceType.Coin:
                    return status.coin >= amount;
                case ResourceType.Health:
                    return status.HP.Current >= amount; // Tránh dùng HP làm cost nếu chết
                default:
                    return false;
            }
        }

        public void ConsumeResource(ResourceType type, float amount)
        {
            switch (type)
            {
                case ResourceType.Mana:
                    status.Mana.Reduce(amount);
                    break;
                case ResourceType.Coin:
                    SubtractCoin(Mathf.RoundToInt(amount));
                    break;
                case ResourceType.Health:
                    float reduceAmount = amount;
                    if (status.HP.Current - amount < 0.1f)
                    {
                        reduceAmount = status.HP.Current - 0.1f;
                    }
                    if (reduceAmount > 0) status.HP.Reduce(reduceAmount);
                    break;
            }
        }

        [Obsolete("Use CanAfford instead")]
        public bool CanUseSkill(float manaCost) => CanAfford(ResourceType.Mana, manaCost);

        [Obsolete("Use ConsumeResource instead")]
        public void ConsumeSkillResources(float manaCost) => ConsumeResource(ResourceType.Mana, manaCost);

        public int Coin 
        { 
            get => status.coin; 
            set => status.coin = value; 
        }

        // --- BRIDGE PROPERTIES (For backward compatibility) ---
        public bool HasHp => _combat != null ? _combat.HasHp : status.HP.Current > 0;
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
                maxHp = status.HP.Max,
                currentHp = status.HP.Current,
                coin = status.coin,
                moveSpeed = status.moveSpeed,
                positionX = transform.position.x,
                positionY = transform.position.y,
                isGround = status.isGround,
                
                // Save current resources directly from status
                currentMana = status.Mana.Current
            };
        }

        public void RestoreData(object data)
        {
            PlayerStatusData loaded = data as PlayerStatusData;
            if (loaded == null) return;

            // Đánh dấu đã load từ save — Awake sẽ bỏ qua ApplyConfig
            _dataRestoredFromSave = true;

            status.HP.SetMax(loaded.maxHp, true);
            if (loaded.currentHp < loaded.maxHp)
                status.HP.Reduce(loaded.maxHp - loaded.currentHp);

            status.coin         = loaded.coin;
            status.moveSpeed    = loaded.moveSpeed;
            status.isGround     = loaded.isGround;
            transform.position  = new Vector3(loaded.positionX, loaded.positionY, 0);
            
            // Mana chỉ lưu currentMana trong save, maxMana dùng từ config mặc định
            float maxMana = defaultConfig != null ? defaultConfig.maxMana : 50f;
            status.Mana.SetMax(maxMana, true);
            if (loaded.currentMana < maxMana)
                status.Mana.Reduce(maxMana - loaded.currentMana);

            Debug.Log($"[PlayerBehavior] Restored save data. HP={status.HP.Current}/{status.HP.Max}, Mana={status.Mana.Current}/{status.Mana.Max}");
        }



    }
}
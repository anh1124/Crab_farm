using System;
using System.Text;
using Script.Data_Model.Status;
using Script.Data_Model.Config;
using Script.Data_Model.Interfaces;
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
    public class PlayerBehavior : MonoBehaviour
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
            if (!status.isDataRestoredFromSave)
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

            LogPlayerSkillsAtStart();
        }

        /// <summary>
        /// Một lần ở Start: log toàn bộ slot skill + cooldown (debug save/load & runtime).
        /// </summary>
        private void LogPlayerSkillsAtStart()
        {
            var skillMgr = GetComponent<SkillManager>();
            if (skillMgr == null)
            {
                Debug.Log("[playerskill] No SkillManager on this player.");
                return;
            }

            var sb = new StringBuilder();
            sb.Append("[playerskill] Skills at Start (slot | name | state | cooldown remaining / total):\n");
            for (int i = 0; i < skillMgr.SlotCount; i++)
            {
                var sk = skillMgr.GetSkill(i);
                string name = sk != null ? sk.skillName : "(empty)";
                float rem = skillMgr.GetCooldownRemaining(i);
                float total = skillMgr.GetCooldownTotal(i);
                var state = skillMgr.GetState(i);
                sb.Append($"[playerskill]   [{i}] {name} | {state} | {rem:F2}s / {total:F2}s\n");
            }

            Debug.Log(sb.ToString());
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
                    // Logic mới: Yêu cầu máu hiện tại > 1 + cost (để sau khi dùng còn > 1 HP)
                    return status.HP.Current > 1f + amount;
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
                    // Đã check CanAfford trước đó, nhưng vẫn giữ bảo hiểm để không bao giờ chết do dùng skill
                    float targetHp = status.HP.Current - amount;
                    if (targetHp < 1.0f)
                    {
                        // Nếu lỡ gọi mà không đủ (logic game lỗi), thì chỉ giảm đến mức tối thiểu là 1.0f
                        amount = Mathf.Max(0, status.HP.Current - 1.0f);
                    }
                    if (amount > 0) status.HP.Reduce(amount);
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


    }
}
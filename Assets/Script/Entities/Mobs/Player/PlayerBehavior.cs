using System;
using Script.Data_Model.Status;
using Script.Data_Model.Interfaces;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using UnityEngine;
using Script.Game.PlayerCharacter;
using Script.Controller.PlayerController; // For PlayerController.Instance
using Art.Resources.Character.PlayerCharactor.Spear_Man; // Namespace cho AnimController
using Script.Game.Item.Coin;

namespace Script.Game.Player
{
    // Chứa logic gameplay của Player, sử dụng PlayerStatus để lưu trữ trạng thái
    
    //[TODO] thêm script kiểm tra script player đã đủ chưa
    
    public class PlayerBehavior : MonoBehaviour, ISaveable
    {
        [Header("Player Status Data")]
        public PlayerStatus status;
        
        [Header("Animation Reference")]
        public SpearManAnimationController playerAnim; 

        // Components References (Cached for bridge properties)
        private PlayerCombat _combat;
        private PlayerInteraction _interaction;

        [Serializable]
        public class PlayerStatusData
        {
            public int maxHp;
            public int currentHp;
            public int coin;
            public float moveSpeed;
            public float positionX;
            public float positionY;
            public int attackDame;
            public float attackSpeed;
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
            
            if (status == null)
            {
                status = GetComponent<PlayerStatus>();
                if (status == null)
                {
                    status = gameObject.AddComponent<PlayerStatus>();
                }
            }

            // Tự động thêm PlayerInteraction nếu chưa có
            if (GetComponent<PlayerInteraction>() == null)
            {
                gameObject.AddComponent<PlayerInteraction>();
            }

            // Tự động thêm PlayerItemHandler nếu chưa có
            if (GetComponent<PlayerItemHandler>() == null)
            {
                gameObject.AddComponent<PlayerItemHandler>();
            }

            // Tự động thêm PlayerCombat nếu chưa có

            if (GetComponent<PlayerCombat>() == null)
            {
                gameObject.AddComponent<PlayerCombat>();
            }

            // Tự động thêm PlayerUISetup nếu chưa có trên GameObject này
            if (GetComponent<PlayerUISetup>() == null)
            {
                gameObject.AddComponent<PlayerUISetup>();
            }

            // Cache references for bridge properties
            _combat = GetComponent<PlayerCombat>();
            _interaction = GetComponent<PlayerInteraction>();
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

        private void Update()
        {
            // Tự động regen mana
            if (status.currentMana < status.maxMana)
            {
                status.currentMana += status.manaRegenRate * Time.deltaTime;
                status.currentMana = Mathf.Min(status.currentMana, status.maxMana);
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
                attackDame = status.attackDame,
                attackSpeed = status.attackSpeed,
                isGround = status.isGround,
                
                // Save current resources directly from status
                currentMana = status.currentMana
            };
        }

        public void RestoreData(object data)
        {
            PlayerStatusData loaded = data as PlayerStatusData;
            if (loaded != null)
            {
                status.maxHp = loaded.maxHp;
                status.currentHp = loaded.currentHp;
                status.coin = loaded.coin;
                status.moveSpeed = loaded.moveSpeed;
                status.attackDame = loaded.attackDame;
                status.attackSpeed = loaded.attackSpeed;
                status.isGround = loaded.isGround;
                transform.position = new Vector3(loaded.positionX, loaded.positionY, 0);
                
                // NEW: Restore resources to PlayerStatus
                status.currentMana = loaded.currentMana;
                
                // ResourceManager removed, data lives in PlayerStatus now
            }
        }



    }
}
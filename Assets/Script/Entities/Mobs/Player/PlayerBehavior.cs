using System;
using Script.Data_Model.Status;
using Script.Data_Model.Interfaces;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using UnityEngine;
using Script.Game.PlayerCharacter;
using Script.Controller.PlayerController; // For PlayerController.Instance
using Art.Resources.Character.PlayerCharactor.Spear_Man; // Namespace cho AnimController

namespace Script.Game.Player
{
    // Chứa logic gameplay của Player, sử dụng PlayerStatus để lưu trữ trạng thái
    
    //[TODO] thêm script kiểm tra script player đã đủ chưa
    
    public class PlayerBehavior : MonoBehaviour, IAttacker, ICoinable, ICoinShield, IDeathDrop, IHealth, ISaveable
    {
        [Header("Player Status Data")]
        public PlayerStatus status;
        
        [Header("Animation Reference")]
        public SpearManAnimationController playerAnim; // Script Animation tự đăng ký vào đây(sau này tạo quy chuẩn riêng cho anim, cứ anim player là đăng ký vào đượkc)

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
            public float currentStamina;
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
            // Tự động regen stamina
            if (status.currentStamina < status.maxStamina)
            {
                status.currentStamina += status.staminaRegenRate * Time.deltaTime;
                status.currentStamina = Mathf.Min(status.currentStamina, status.maxStamina);
            }
            
            // Tự động regen mana
            if (status.currentMana < status.maxMana)
            {
                status.currentMana += status.manaRegenRate * Time.deltaTime;
                status.currentMana = Mathf.Min(status.currentMana, status.maxMana);
            }
        }
        
        // === RESOURCE HELPERS ===
        public bool CanUseSkill(float staminaCost, float manaCost)
        {
            return status.currentStamina >= staminaCost && status.currentMana >= manaCost;
        }

        public void ConsumeSkillResources(float staminaCost, float manaCost)
        {
            status.currentStamina -= staminaCost;
            status.currentMana -= manaCost;
            
            // Validate không âm
            status.currentStamina = Mathf.Max(0f, status.currentStamina);
            status.currentMana = Mathf.Max(0f, status.currentMana);
        }

        public float MoveSpeed { get => status.moveSpeed; set => status.moveSpeed = value; }
        public int AttackDame { get => status.attackDame; set => status.attackDame = value; }
        public float AttackSpeed { get => status.attackSpeed; set => status.attackSpeed = value; }
        public int MaxHp { get => status.maxHp; set => status.maxHp = value; }

     

        public void TakeDamage(int amount)
        {
            throw new System.NotImplementedException();
        }

        public void Die()
        {
            throw new NotImplementedException();
        }

        public void Heal(int amount)
        {
            throw new System.NotImplementedException();
        }

        public bool IsDead { get; }
        public int CurrentHp { get => status.currentHp; set => status.currentHp = value; }
        public int Coin { get => status.coin; set => status.coin = value; }
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

        public void PickUpCoin(GameObject coinGameObject)
        {
            Script.Utility.ProcessCoinPickup(coinGameObject,
                condition: () => true,
                onPickupSuccess: () => AddCoin(1)
            );
        }

        // Implement các interface khác nếu cần

        public void Attack()
        {
            // Logic tấn công
        }

        public bool TakeDamage(int damage, GameObject whoDoDame)
        {
            if (HasCoinShield && Coin > 0)
            {
                Coin -= 1;
                if (Coin < 0) Coin = 0;
                DropCoin(whoDoDame);
                return true;
            }
            if (HasHp)
            {
                CurrentHp -= damage;
                if (CurrentHp <= 0)
                {
                    Die(whoDoDame);
                    return false;
                }
                return true;
            }
            return true;
        }

        public void Die(GameObject whoDoDame)
        {
            Debug.Log($"{gameObject.name} đã chết bởi {whoDoDame?.name ?? "unknown"}");
            gameObject.SetActive(false);
        }

        public int CurrentCoin { get; set; }
        public bool HasCoinShield => Coin > 0;

        public bool ConsumeCoinShield()
        {
            if (!HasCoinShield) return false;
            Coin -= 1;
            if (Coin < 0) Coin = 0;
            return true;
        }
        public bool HasHp => CurrentHp > 0;

        public void DropCoin(GameObject whoDoDame)
        {
            // Logic rơi coin khi bị tấn công
        }

        // ISaveable
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
                currentStamina = status.currentStamina,
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
                status.currentStamina = loaded.currentStamina;
                status.currentMana = loaded.currentMana;
                
                // ResourceManager removed, data lives in PlayerStatus now
            }
        }

        public void DropOnDeath()
        {
            throw new System.NotImplementedException();
        }
    }
}
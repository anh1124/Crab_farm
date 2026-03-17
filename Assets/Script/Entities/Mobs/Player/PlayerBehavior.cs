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
    
    public class PlayerBehavior : MonoBehaviour, IAttacker, ICoinable, ICoinShield, IDeathDrop, IHealth, ISaveable, IDie, IItemHandle, IDamageable
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

        private void Start()
        {
            SetupHealBarUI();
        }

        private void SetupHealBarUI()
        {
            var canvasManager = FindObjectOfType<Script.GUI_Controller.GuiCanvasManager>();
            if (canvasManager != null && canvasManager.healBarUI != null)
            {
                canvasManager.healBarUI.Setup(status);
            }
            else
            {
                Debug.LogWarning("[PlayerBehavior] Khong tim thay GuiCanvasManager hoac HealBarUI chua duoc gan vao CanvasManager de setup!");
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
            // Tự động regen mana
            if (status.currentMana < status.maxMana)
            {
                status.currentMana += status.manaRegenRate * Time.deltaTime;
                status.currentMana = Mathf.Min(status.currentMana, status.maxMana);
            }
            
            ValidateInteraction();
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

        public float MoveSpeed { get => status.moveSpeed; set => status.moveSpeed = value; }
        public int AttackDame { get => status.attackDame; set => status.attackDame = value; }
        public float AttackSpeed { get => status.attackSpeed; set => status.attackSpeed = value; }
        public int MaxHp { get => status.maxHp; set => status.maxHp = value; }

     

        public void TakeDamage(int amount)
        {
            TakeDamage(amount, null);
        }

        public void Die()
        {
            Die(null);
        }

        public void Heal(int amount)
        {
            if (IsDead) return;
            CurrentHp += amount;
            if (CurrentHp > MaxHp) CurrentHp = MaxHp;
        }

        public bool IsDead { get; private set; }
        public int CurrentHp { get => status.currentHp; set => status.currentHp = value; }
        public int Coin { get => status.coin; set => status.coin = value; }
        public int MaxCoins { get => status.maxCoins; set => status.maxCoins = value; }
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

        // PickUpCoin was removed as HandleItem directly performs this logic now.

        public bool HandleItem(GameObject item)
        {
            var physicalItem = item.GetComponent<PhysicalItem>();
            Debug.Log($"[PlayerBehavior] HandleItem triggered for {item.name}. physicalItem: {physicalItem != null}");
            
            if (physicalItem == null) return false;

            if (physicalItem.itemType == ItemType.Coin)
            {
                Debug.Log($"[PlayerBehavior] HandleItem: Coin={Coin}, MaxCoins={MaxCoins}");
                if (Coin < MaxCoins)
                {
                    AddCoin(1);
                    Debug.Log($"[PlayerBehavior] HandleItem: Picked up coin successfully!");
                    return true;
                }
            }
            return false;
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
            if (IsDead) return;
            IsDead = true;
            Debug.Log($"{gameObject.name} đã chết bởi {whoDoDame?.name ?? "unknown"}");
            gameObject.SetActive(false);
        }

        public int CurrentCoin { get => status.coin; set => status.coin = value; }
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

        // --- IDamageable Interface Implementation ---
        /// <summary>
        /// Điểm nhận sát thương duy nhất cho Player.
        /// Bên tấn công chỉ gọi hàm này và không cần biết gì thêm.
        /// </summary>
        public void ReceiveDamage(int damage, UnityEngine.GameObject instigator)
        {
            if (damage <= 0 || IsDead) return;

            // Ưu tiên dùng Coin đở đòn (giống cơ chế gốc)
            if (HasCoinShield && Coin > 0)
            {
                Coin -= 1;
                if (Coin < 0) Coin = 0;
                DropCoin(instigator);
                return;
            }

            // Hết Coin thì trừ máu
            if (HasHp)
            {
                CurrentHp -= damage;
                if (CurrentHp <= 0)
                {
                    Die(instigator);
                }
            }
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

        public void DropOnDeath()
        {
            // [TODO] Implement drop logic (e.g. drop coins, items)
            // For now, empty to prevent crash
        }
        // === INTERACTION LOGIC ===
        private System.Collections.Generic.List<IPlayerInteractable> interactablesInRange = new System.Collections.Generic.List<IPlayerInteractable>();
        public IPlayerInteractable currentInteractable;

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Tránh va chạm với trigger của chính mình hoặc con cái của mình nếu cần
            if (other.isTrigger && (other.CompareTag("Player") || other.transform.IsChildOf(transform))) return;

            // Xử lý IPlayerInteractable
            IPlayerInteractable interactable = other.GetComponent<IPlayerInteractable>();
            if (interactable != null)
            {
                if (!interactablesInRange.Contains(interactable))
                {
                    interactablesInRange.Add(interactable);
                    UpdateCurrentInteractable();
                }
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            // Xử lý IPlayerInteractable
            IPlayerInteractable interactable = other.GetComponent<IPlayerInteractable>();
            if (interactable != null)
            {
                if (interactablesInRange.Contains(interactable))
                {
                    interactablesInRange.Remove(interactable);
                    
                    // Nếu đang tương tác với đối tượng này thì tắt đi
                    if (currentInteractable == interactable)
                    {
                        interactable.OnPlayerExit(this);
                        currentInteractable = null;
                    }
                    
                    UpdateCurrentInteractable();
                }
            }
        }


        private void UpdateCurrentInteractable()
        {
            // Clean up destroyed interactables just in case
            interactablesInRange.RemoveAll(i => i == null || (i is MonoBehaviour mb && mb == null));

            if (interactablesInRange.Count == 0)
            {
                if (currentInteractable != null)
                {
                    currentInteractable.OnPlayerExit(this);
                    currentInteractable = null;
                }
                return;
            }

            // Find the most recent valid interactable
            IPlayerInteractable candidate = null;
            for (int i = interactablesInRange.Count - 1; i >= 0; i--)
            {
                if (interactablesInRange[i].CanInteract())
                {
                    candidate = interactablesInRange[i];
                    break;
                }
            }

            // Switch to the new candidate if it's different
            if (currentInteractable != candidate)
            {
                if (currentInteractable != null)
                {
                    currentInteractable.OnPlayerExit(this);
                }

                currentInteractable = candidate;
                
                if (currentInteractable != null)
                {
                    currentInteractable.OnPlayerEnter(this);
                }
            }
        }
        
        // Validate interactable periodically or in Update
        private void ValidateInteraction()
        {
             // Continuously check if we need to switch or drop the current interactable
             // without permanently removing it from the physically-in-range list.
             UpdateCurrentInteractable();
        }
    }
}
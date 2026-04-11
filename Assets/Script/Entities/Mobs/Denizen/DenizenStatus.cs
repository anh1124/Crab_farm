using System;
using UnityEngine;
using Script.Data_Model.Interfaces;
using Script.Game.Mobs.Subjects;
using Script.Game.Item.Coin;
using Script.Save_Controller;
using Newtonsoft.Json;

namespace Script.Data_Model.Status
{
    [System.Serializable]
    public class DenizenStatus : MonoBehaviour, INPCStatus, ICoinable
    {
        [Header("Runtime State")]
        public string currentStateName = "Idle";
        public bool isSkillCasting = false;
        public bool skillAllowsMovement = true;
        public bool debugDenizen = false;  // Bật để xem log chi tiết cho entity này

        public enum ArcherState { Idle, Hunting, Attack, HoldWall, Retreat, ReturningToBase, DropCoin }

        [field: SerializeField] public bool CanMove { get; set; } = true;
        public bool isMoving = false;

        [SerializeField] private NPCRole role = NPCRole.Vagrant;
        [Header("General Settings")]
        [SerializeField] private int currentCoins = 0;
        [SerializeField] private int maxCoins = 6;
        
        // Unified Movement Settings
        [Header("Movement")]
        public float walkSpeed = 1.0f;
        public float runSpeed = 3.5f;
        public float huntingMoveSpeed = 1.2f;
        public float currentSpeed;

        // Vagrant Specifics
        public PhysicalItem currentTool; // Added for Villager/Tool logic

        // Villager Specifics
        public float fearTimer;
        [Tooltip("Thời gian bị hoảng sợ tối đa (như 3s)")]
        public float maxFearDuration = 3f;
        public bool isInDanger;
        /// <summary>Cờ chung: Bất cứ role nào (Archer, Vagrant...) có thể bật lên trước khi đổi sang Villager.
        /// VillagerBehavior sẽ đọc cờ này trong OnEnable và vào Fear ngay lập tức nếu fearTimer > 0.</summary>
        public bool isFearPending;
        // targetPosition is reused from Vagrant

        // Archer Specifics
        [Tooltip("Phạm vi phát hiện kẻ thù bằng RaycastHelper (Dùng cho Villager/Vagrant)")]
        public float detectRangeRaycast = 10f; // Was detectRange

        [Tooltip("Phạm vi tấn công/phát hiện của Archer (Dùng Circular Check)")]
        public float archerDetectRange = 25f; // Was attackRange & huntingRange

        public float attackDame = 1f;
        public float attackSpeed = 2.5f; // Control attack frequency / debug log frequency
        public float lastAttackTime = 0f;
        public bool isLeftFlank;
        public GameObject assignedWall;
        public GameObject previousWall;
        public GameObject currentTarget;
        public GameObject coinTarget; // Added for Vagrant/Coin logic

        [Header("Legacy / Squad Tracking")]
        public ArcherState archerState; // For GuardSquadSystem compatibility

        // Events
        public event Action<int> OnCoinChanged;

        // Properties for INPCStatus / Interfaces
        public NPCRole Role 
        { 
            get => role; 
            set => role = value; 
        }
        
        public int CurrentCoins 
        { 
            get => currentCoins; 
            set 
            {
                int oldVal = currentCoins;
                currentCoins = Mathf.Clamp(value, 0, maxCoins); 
                if (currentCoins != oldVal)
                {
                    OnCoinChanged?.Invoke(currentCoins);
                }
            }
        }

        public bool IsAlive => true; // Simple implementation for now
        public float AttackCooldown => attackSpeed;

        public int MaxCoins
        {
             get => maxCoins;
             set => maxCoins = value;
        }

        public bool IsFull => currentCoins >= maxCoins;

        // Basic helper properties
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;

        // Save Data Logic
        [System.Serializable]
        public class DenizenSaveData
        {
            public float positionX;
            public float positionY;
            public float scaleX;
            public int currentCoins;
            public string role;
            
            // Vagrant Save Data
            public int vagrantState;
            public float startPositionX;

            // Villager Save Data
            public int villagerState;
            public bool isFearPending;
            public bool isInDanger;
            public float fearTimer;

            // Archer Save Data
            public int archerState;
            public bool isLeftFlank;

            [JsonProperty("skillManager")]
            public SkillManagerSaveData skillManager;
        }

        // ICoinable Implementation
        public int Coin
        {
            get => currentCoins;
            set => CurrentCoins = value;
        }

        public void AddCoin(int amount)
        {
            CurrentCoins += amount;
        }

        public void SubtractCoin(int amount)
        {
            CurrentCoins -= amount;
        }

        /// <summary>
        /// Route dame vào DenizenRoleController để xử lý tập trung.
        /// Đây là method thường (không phải IDamageable) nên DamageHandler sẽ không
        /// tự động gọi nó — tránh double-damage.
        /// Gọi từ CombatUtility hoặc DamageHandler khi target là Denizen.
        /// </summary>
        public void RouteDamage(float damage, GameObject instigator)
        {
            var ctx = GetComponent<Script.Game.Mobs.Denizen.Core.DenizenContext>();
            if (ctx != null && ctx.CurrentJob != null)
            {
                // Route directly to the job brain
                ctx.CurrentJob.ReceiveDamage(damage, instigator);
            }
        }


        public DenizenSaveData CaptureData(Transform t)
        {
            return new DenizenSaveData
            {
                positionX = t.position.x,
                positionY = t.position.y,
                scaleX = t.localScale.x,
                currentCoins = currentCoins,
                role = role.ToString(),
                
                isFearPending = isFearPending,
                isInDanger = isInDanger,
                fearTimer = fearTimer,
                isLeftFlank = isLeftFlank
            };
        }

        public void RestoreData(DenizenSaveData data, Transform t)
        {
            t.position = new Vector3(data.positionX, data.positionY, 0);
            t.localScale = new Vector3(data.scaleX, 1, 1);
            currentStateName = data.role + "Idle";
            currentCoins = data.currentCoins;
            if (System.Enum.TryParse(data.role, out NPCRole savedRole))
            {
                role = savedRole;
            }

            isFearPending = data.isFearPending;
            isInDanger = data.isInDanger;
            fearTimer = data.fearTimer;
            isLeftFlank = data.isLeftFlank;
        }
    }
}

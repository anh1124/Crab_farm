using System;
using UnityEngine;
using Script.Data_Model.Interfaces;
using Script.Game.Mobs.Subjects;

namespace Script.Data_Model.Status
{
    [System.Serializable]
    public class DenizenStatus : MonoBehaviour, INPCStatus, ICoinable
    {
        // Enums for distinct role states
        public enum VagrantState
        {
            Fear,
            Idle,
            GetCoin
        }



        public enum VillagerState
        {
            Idle,                   // Wandering around kingdom
            MoveBackToTownCenter,   // Walking back to town center at night
            RunToGetTool,           // Running to get tools (highest priority)
            Fear,                   // Running away from danger
            DropCoinToPlayer        // Drop coin when seeing player
        }

        public enum ArcherState
        {
            Idle,
            Hunting,
            HoldingWall,
            RetreatingToInnerWall,
            ReturningToBase
        }

       
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
        [Header("Vagrant Data")]
        public VagrantState vagrantState = VagrantState.Idle;
        public float maxIdleWalkDistance = 3.0f;
        public float roadToTarget = 0f;
        public Vector2 targetPosition; // Shared with Villager?
        public GameObject coinTarget;
        public Vector3 startPosition;
        public Vector3 currentIdleWalkTarget;
        public bool isWalking;

        // Villager Specifics
        [Header("Villager Data")]
        public VillagerState villagerState = VillagerState.Idle;
        public float fearTimer;
        // targetPosition is reused from Vagrant

        // Archer Specifics
        [Header("Archer Data")]
        public ArcherState archerState = ArcherState.Idle;
        public ArcherState previousArcherState = ArcherState.Idle;
        [Tooltip("Phạm vi phát hiện kẻ thù bằng RaycastHelper (Dùng cho Villager/Vagrant)")]
        public float detectRangeRaycast = 10f; // Was detectRange

        [Tooltip("Phạm vi tấn công/phát hiện của Archer (Dùng Circular Check)")]
        public float archerDetectRange = 18f; // Was attackRange & huntingRange

        public float attackDame = 1f;
        public float attackSpeed = 2.0f; // Control attack frequency / debug log frequency
        public float lastAttackTime = 0f;
        public bool isLeftFlank;
        public GameObject assignedWall;
        public GameObject previousWall;
        public GameObject currentTarget;

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

        public int MaxCoins
        {
             get => maxCoins;
             set => maxCoins = value;
        }

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

            // Archer Save Data
            public int archerState;
            public bool isLeftFlank;
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


        public DenizenSaveData CaptureData(Transform t)
        {
            return new DenizenSaveData
            {
                positionX = t.position.x,
                positionY = t.position.y,
                scaleX = t.localScale.x,
                currentCoins = currentCoins,
                role = role.ToString(),
                
                vagrantState = (int)vagrantState,
                startPositionX = startPosition.x,

                villagerState = (int)villagerState,

                archerState = (int)archerState,
                isLeftFlank = isLeftFlank
            };
        }

        public void RestoreData(DenizenSaveData data, Transform t)
        {
            t.position = new Vector3(data.positionX, data.positionY, 0);
            t.localScale = new Vector3(data.scaleX, 1, 1);
            currentCoins = data.currentCoins;
            if (System.Enum.TryParse(data.role, out NPCRole savedRole))
            {
                role = savedRole;
            }

            // Restore states (careful as they depend on role)
            vagrantState = (VagrantState)data.vagrantState;
            startPosition = new Vector3(data.startPositionX, data.positionY, 0);
            
            villagerState = (VillagerState)data.villagerState;

            archerState = (ArcherState)data.archerState;
            isLeftFlank = data.isLeftFlank;
        }
    }
}

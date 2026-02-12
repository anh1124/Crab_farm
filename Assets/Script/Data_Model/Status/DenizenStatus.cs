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
        [SerializeField] private int currentHp = 1;
        [SerializeField] private int maxHp = 1;
        
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
        [Tooltip("Phạm vi tấn công của Archer (Dùng riêng cho Archer)")]
        public float attackRange = 10f;
        [Tooltip("Phạm vi phát hiện kẻ thù bằng RaycastHelper (Dùng chung)")]
        public float detectRange = 10f;
        public float attackCooldown = 2.0f;
        public float lastAttackTime = 0f;
        public bool isLeftFlank;
        public GameObject assignedWall;
        public GameObject previousWall;
        public float standBehindWallOffset = 0.8f;
        public GameObject currentTarget;

        // Properties for INPCStatus / Interfaces
        public NPCRole Role 
        { 
            get => role; 
            set => role = value; 
        }
        
        public int CurrentCoins 
        { 
            get => currentCoins; 
            set => currentCoins = Mathf.Clamp(value, 0, maxCoins); 
        }

        public int MaxCoins
        {
             get => maxCoins;
             set => maxCoins = value;
        }

        public int CurrentHp 
        { 
            get => currentHp; 
            set => currentHp = Mathf.Clamp(value, 0, maxHp); 
        }

        public int MaxHp 
        { 
            get => maxHp; 
            set => maxHp = value; 
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
            set => currentCoins = Mathf.Clamp(value, 0, maxCoins);
        }

        public void AddCoin(int amount)
        {
            currentCoins = Mathf.Clamp(currentCoins + amount, 0, maxCoins);
        }

        public void SubtractCoin(int amount)
        {
            currentCoins = Mathf.Clamp(currentCoins - amount, 0, maxCoins);
        }

        public void PickUpCoin(GameObject coinGameObject)
        {
             // This is a fallback if CoinCollector is missing or Coin.cs triggers this directly
             // We just add coin and destroy the object? 
             // Ideally Coin.cs handles destruction if this returns true?
             // Checking Coin.cs logic: it calls pickUpCoin, then it's done. 
             // Actually Coin.cs does NOT destroy coin in HandlePlayerPickup/HandleDenizensPickup.
             // It expects the receiver to handle it? 
             // Wait, Coin.cs:208 calls coinable.PickUpCoin(gameObject).
             // It does NOT destroy the coin itself in that method. 
             // So DenizenStatus.PickUpCoin MUST destroy the coin.
             
             AddCoin(1);
             // Destroy is handled by ProcessCoinPickup utility usually, but if called directly:
             if(coinGameObject != null) Destroy(coinGameObject);
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

using System;
using Script.Data_Model;
using Script.Game.Item.Coin;
using Script.Interfaces;
using UnityEngine;

namespace Script.Game.Other.Nature
{   
    [RequireComponent(typeof(NatureTreeStatus))]
    public class NatureTreeBehavior : MonoBehaviour, IDeathDrop
    {
        [Header("Tree Components")]
        [SerializeField] private CoinUser coinUser;
        [SerializeField] private NatureTreeStatus status;
        
        [Header("Tree Settings")]
        public int treeChopCost; // biến này ko cần lưu
        
        private bool _isDead = false; // Guard chống double-process khi bị destroy
        
        private void Awake()
        {
            // Check and auto-add required components
            CheckAndAddRequiredComponents();
            
            // Get components
            coinUser = GetComponent<CoinUser>();
            status = GetComponent<NatureTreeStatus>();
        }
        
        private void CheckAndAddRequiredComponents()
        {
            // Check and add NatureTreeStatus if missing
            if (GetComponent<NatureTreeStatus>() == null)
            {
                status = gameObject.AddComponent<NatureTreeStatus>();
                Debug.Log($"[NatureTreeBehavior] Auto-added NatureTreeStatus component to {gameObject.name}");
            }
            
            // Check and add CoinUser if missing
            if (GetComponent<CoinUser>() == null)
            {
                coinUser = gameObject.AddComponent<CoinUser>();
                Debug.Log($"[NatureTreeBehavior] Auto-added CoinUser component to {gameObject.name}");
            }
        }
        
        private void Start()
        {
            if (coinUser == null)
            {
                Debug.LogError("[NatureTreeBehavior] CoinUser component not found on GameObject");
                return;
            }
            
            if (status == null)
            {
                Debug.LogError("[NatureTreeBehavior] NatureTreeStatus component not found on GameObject");
                return;
            }
            
            InitializeTree();
            // [BUGFIX] Destroy ghost trees that loaded with 0 progress
            if (status.currentWorkProgress <= 0)
            {
                _isDead = true;
                Destroy(gameObject);
            }
        }
        
        private void InitializeTree()
        {
            treeChopCost = PrefabData.Instance.treeChopCost;
            
            // Set maxWorkProgress to 8 as requested by user
            if (!status.isSetupCalled)
            {
                status.maxWorkProgress = 8;
                status.currentWorkProgress = status.maxWorkProgress;
            }
            
            UpdateCoinUserData();
        }
        
        private void UpdateCoinUserData()
        {
            coinUser.requiredCoins = treeChopCost;
            coinUser.onUpgradeComplete = SetChopFlag;
        }
        
        public void SetChopFlag()
        {
            Debug.Log($"[NatureTree-DEBUG] SetChopFlag CALLED on {gameObject.name}. isReadyToChop was {status.isReadyToChop}");
            status.isReadyToChop = true;
            // Tắt nhận coin ngay lập tức để cây không bị interact thêm trong frame destroy
            if (coinUser != null) coinUser.canAcceptCoins = false;
            TakeDame(status.maxWorkProgress); // Harvest toàn bộ hp của cây
        }

        public void TakeDame(int dame)
        {
            Debug.Log($"[NatureTree-DEBUG] TakeDame CALLED on {gameObject.name}: dame={dame}, currentHP={status.currentWorkProgress}, _isDead={_isDead}");
            if (_isDead || dame <= 0) return;
            
            if (dame >= status.currentWorkProgress)
            {
                Debug.Log($"[NatureTree-DEBUG] {gameObject.name} IS DEAD. Calling Destroy.");
                _isDead = true;
                status.currentWorkProgress = 0;
                DropOnDeath();
                // Do animation treefall
                Destroy(gameObject);
            }
            else
            {
                status.currentWorkProgress -= dame;
                Debug.Log($"[NatureTree-DEBUG] {gameObject.name} reduced HP to {status.currentWorkProgress}");
            }
        }
        
        public void DropOnDeath()
        {
            // Tỉ lệ rơi coin 80%
            if (UnityEngine.Random.Range(0, 100) < 80)
            {
                int amount = UnityEngine.Random.Range(1, 3); // Rơi 1 hoặc 2 coin
                Spawner.Instance.DropByNature(transform.position, amount, Vector2.zero);
            }
        }
        
        // Public getters for external access, routing to status
        public int CurrentWorkProgress => status.currentWorkProgress;
        public int MaxWorkProgress => status.maxWorkProgress;
        public bool IsReadyToChop => status.isReadyToChop;
        public string TreeType => status.treeType;
        public int TreeLevel => status.treeLevel;
    }
}

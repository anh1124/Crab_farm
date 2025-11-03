using System;
using Script.Data_Model;
using Script.Game.Item.Coin;
using Script.Interfaces;
using UnityEngine;

namespace Script.Game.Other.Nature
{   
    public class NatureTreeBehavior : MonoBehaviour, IDeathDrop
    {
        [Header("Tree Components")]
        [SerializeField] private CoinUser coinUser;
        [SerializeField] private NatureTreeStatus status;
        
        [Header("Tree Settings")]
        public int treeChopCost; // biến này ko cần lưu
        
        [Header("Tree Properties")]
        public int currentWorkProgress;
        public int maxWorkProgress;
        public bool isReadyToChop;
        
        [Header("Tree Type")]
        public string treeType = "Default";
        public int treeLevel = 1;
        
        private bool isSetupCalled = false;
        
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
        }
        
        private void InitializeTree()
        {
            treeChopCost = PrefabData.Instance.treeChopCost;
            maxWorkProgress = PrefabData.Instance.treeMaxWorkProgress;
            UpdateCoinUserData();
            
            // Nếu hàm SetWorkProgress chưa được gọi thì tức là đây là cây mới(chưa có trong save nên phải set full hp cho nó)
            if (!isSetupCalled)
            {
                currentWorkProgress = PrefabData.Instance.treeMaxWorkProgress;
            }
        }
        
        public void SetWorkProgress(int currentWorkProgress)
        {
            isSetupCalled = true;
            this.currentWorkProgress = currentWorkProgress;
        }
        
        private void UpdateCoinUserData()
        {
            coinUser.requiredCoins = treeChopCost;
            coinUser.onUpgradeComplete = SetChopFlag;
        }
        
        public void SetChopFlag()
        {
            isReadyToChop = true;
            // Đặt tạm hàm take dame ở đây để test xóa cây
            Debug.LogWarning("đang test xóa cây sau khi thêm tiền ở hàm SetChopFlag");
            TakeDame(11);
        }

        public void TakeDame(int dame)
        {   
            Debug.Log("NatureTreeBehavior: take dame " + dame);
            if (dame <= 0)
            {
                return;
            }
            
            if (dame >= currentWorkProgress)
            {
                DropOnDeath();
                // Do animation treefall
                Destroy(gameObject);
            }
            else
            {
                currentWorkProgress -= dame;
            }
        }
        
        public void DropOnDeath()
        {
            Spawner.Instance.DropByNature(transform.position,
                Spawner.Instance.GetRandomNumber(3), 
                Vector2.zero);
        }
        
        // Public getters for external access
        public int CurrentWorkProgress => currentWorkProgress;
        public int MaxWorkProgress => maxWorkProgress;
        public bool IsReadyToChop => isReadyToChop;
        public string TreeType => treeType;
        public int TreeLevel => treeLevel;
    }
}

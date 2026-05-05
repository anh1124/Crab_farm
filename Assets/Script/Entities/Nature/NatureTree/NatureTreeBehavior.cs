using Script.Data_Model;
using Script.Game.Item.Coin;
using Script.Interfaces;
using Script.Manager.GamePlayManager;
using UnityEngine;

namespace Script.Game.Other.Nature
{   
    [RequireComponent(typeof(NatureTreeStatus))]
    [RequireComponent(typeof(CoinUser))]
    public class NatureTreeBehavior : MonoBehaviour, IDeathDrop, ICoinUserOwner
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
            
            // Get components (re-fetch after auto-add to ensure we have reference)
            coinUser = GetComponent<CoinUser>();
            status = GetComponent<NatureTreeStatus>();
            
            if (status == null)
            {
                Debug.LogError($"[Tree] {gameObject.name} status is NULL after CheckAndAddRequiredComponents! Adding as fallback.", this);
                status = gameObject.AddComponent<NatureTreeStatus>();
            }
            
            if (coinUser == null)
            {
                Debug.LogError($"[Tree] {gameObject.name} coinUser is NULL after CheckAndAddRequiredComponents! Adding as fallback.", this);
                coinUser = gameObject.AddComponent<CoinUser>();
            }
            
            // Initialize tree state immediately in Awake so that currentWorkProgress is set
            // BEFORE any SaveAllContainer() call (which may happen before Start() runs).
            InitializeTree();
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
            // InitializeTree() is now called in Awake() to ensure correct state before saves.
        }
        
        private void InitializeTree()
        {
            if (status == null)
            {
                Debug.LogError($"[Tree] {gameObject.name} InitializeTree: status is NULL! Re-trying to get component.", this);
                status = GetComponent<NatureTreeStatus>();
                if (status == null)
                {
                    status = gameObject.AddComponent<NatureTreeStatus>();
                    Debug.LogWarning($"[Tree] {gameObject.name} Added NatureTreeStatus component in InitializeTree as fallback.", this);
                }
            }

            if (PrefabData.Instance != null)
                treeChopCost = PrefabData.Instance.treeChopCost;
            else
                Debug.LogError($"[Tree] {gameObject.name} PrefabData.Instance is null!", this);
            
            // Set maxWorkProgress to 8 as requested by user
            if (status != null && !status.isSetupCalled)
            {
                status.maxWorkProgress = 8;
                status.currentWorkProgress = status.maxWorkProgress;
            }
        }
        
        // --- ICoinUserOwner Implementation ---
        public int GetRequiredCoinCount()
        {
            return treeChopCost;
        }

        public bool CanReceiveCoins()
        {
            return !status.isReadyToChop; // Cây chưa sẵn sàng bị chặt thì nhận được coin
        }

        public void OnAllCoinsCollected()
        {
            SetChopFlag();
        }
        // -----------------------------------
        
        public void SetChopFlag()
        {
            status.isReadyToChop = true;

            // [TEMP] Chờ Denizen Builder: hỏi CheatManager có chặt cây ngay không
            if (CheatManager.Instance != null)
                CheatManager.Instance.TrySimulateChop(this);
            else
                Debug.LogWarning("[NatureTree] CheatManager not found in scene — tree will wait for a Denizen Builder.");
        }

        /// <summary>
        /// Denizen Builder gọi hàm này để cộng tiến độ chặt cây mỗi lần cuốc.
        /// Khi tổng tiến độ >= maxWorkProgress, cây sẽ ngã.
        /// </summary>
        public void AddWorkProcess(int amount)
        {
            if (_isDead || amount <= 0) return;
            
            if (amount >= status.currentWorkProgress)
            {
                Debug.Log($"[NatureTree-DEBUG] {gameObject.name} work complete. Tree falling.");
                _isDead = true;
                status.currentWorkProgress = 0;
                DropOnDeath();
                // Do animation treefall
                Destroy(gameObject);
            }
            else
            {
                status.currentWorkProgress -= amount;
                Debug.Log($"[NatureTree-DEBUG] {gameObject.name} remaining progress: {status.currentWorkProgress}");
            }
        }
        private void OnEnable()
        {
        }

        private void OnDisable()
        {
        }

        public void OnDestroy()
        {
            // CHỈ drop coin nếu cây thực sự chết trong gameplay (_isDead = true)
            // KHÔNG drop coin khi bị destroy do: chuyển parent, scene unload, clear map, v.v.
            if (_isDead)
            {
                DropOnDeath();
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
      
    }
}

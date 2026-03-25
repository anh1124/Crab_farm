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
            Debug.Log($"[NatureTree-DEBUG] SetChopFlag CALLED on {gameObject.name}. isReadyToChop was {status.isReadyToChop}");
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
            Debug.Log($"[NatureTree-DEBUG] AddWorkProcess CALLED on {gameObject.name}: amount={amount}, currentProgress={status.currentWorkProgress}, _isDead={_isDead}");
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
        public void OnDestroy()
        {
            DropOnDeath();
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

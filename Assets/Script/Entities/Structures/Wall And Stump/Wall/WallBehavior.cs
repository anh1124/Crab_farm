using System;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using Script.Entities.Structures.Wall_And_Stump.Wall;
using Script.Game.Enemy.Interfaces;
using Script.Game.Item.Coin;
using Script.Game.Other;
using Script.Game.Structures.Wall_And_Stump;
using Script.Manager.Building;
using Script.Save_Controller;
using UnityEngine;
using UnityEngine.Serialization;
using static Script.Utils.DebugUtility;

namespace Script.Game.Structures.Wall_And_Stump
{
    //wall tier 0 không nhận dame
    // Chứa logic behavior của Wall, sử dụng WallStatus để quản lý state
    public class WallBehavior : MonoBehaviour, IHealth, IMinimumDistance, ISaveable, IUpgradeable
    {
        // wall có 2 trạng thái là wall và stump
        //khi trở thành stump sẽ unactive gameobejct chứa collider chặn enemy
        
        /*
           gameobject wall có cấu trúc:
           baseobject:collier[trigger,layer=wall_p(p là playinterrac,phân biệt với layer mà enemy bị chặn)](player-villart interaction),status,behavior.
           -child1:
           collier[trigger=false,layer = enemy_cannot_wallthrow(không physic layer player,villager để chúng đi xuyên qua)]()
           sprite:
           */
        
        //TODO: kiểm tra physic như trên
        //thêm wallmanager để wall tự đăng ký khi chúng được xây
      
        #region === Reference Variables ===
        [SerializeField] private WallStatus status;
        [SerializeField] private CoinUser coinUser;
        #endregion
        
        
        //các biến dùng để logic tham chiếu
        public Collider2D triggerCollider2D;//collider dùng để player va chạm
        public Collider2D blockerCollider2D;//collider chặn enemy đi xuyên
        public SpriteRenderer spriteRenderer;//hiển thị hình ảnh 
        
        public GameObject childBlocker;//child chứa collier chặn enemy
        
        
        [SerializeField] public WallDataList wallDataList;
       
        // Events cho wall manager
        public event Action<WallBehavior> OnWallDestroyed;//thông báo cho manager xếp lại đội hình lính và rút lui
        public event Action<WallBehavior> OnWallUpgraded;//wall sẵn sàng upgrade ,bảo manager điều thợ đến xây
        public event Action<WallBehavior> OnWallRepaired;//thông báo cho manager xếp lại đội hình vì tường vừa sửa xong
        //add more even ondame,onupgradedone,onrepaireddone,onlowhealth
        
        
        #region Properties
        public WallStatus Status => status;
        
        // IHealth Implementation
        public int MaxHp { get => status.maxHp; set => status.maxHp = value; }
        public int CurrentHp { get => status.currentHp; set => status.currentHp = value; }
        
        // IMinimumDistance Implementation
        public float MinimumDistance => status.minimumDistance;
        
        // IUpgradeable Implementation
        public int Tier { get => status.tier; set => status.tier = value; }
        public int UpgradeCost { get => status.upgradeCost; set => status.upgradeCost = value; }
       public bool ReadyToUpgrade { get => status.readyToUpgrade; set => status.readyToUpgrade = value; }
        public bool MaxTier { get => status.isMaxTier; set => status.isMaxTier = value; }
        
        // ICoinReceiver Implementation
        public int CurrentCoin { get => coinUser != null ? coinUser.collectedCoins : 0; set { if (coinUser != null) coinUser.collectedCoins = value; } }
        public int RequiredCoin { get => coinUser != null ? coinUser.requiredCoins : 0; set { if (coinUser != null) coinUser.requiredCoins = value; } }
        public bool CanAcceptCoins => coinUser != null && coinUser.canAcceptCoins;
        public bool IsReadyToUpgrade => coinUser != null && coinUser.isReadyToUpgrade;
        
        // Helper properties
        public int RebuildCost => status.rebuildCost;
        
        // Wall state properties
        public WallState CurrentWallState => status.state;
        public bool IsWall => status.state == WallState.Wall;
        public bool IsStump => status.state == WallState.Stump;
        #endregion
        //nhớ sửa lại hàm điều chỉnh tiền và event cho coin user khi là stump hoặc wall
        private void Awake()
        {
            InitializeComponents();
            status.state = WallState.Wall;
            //load cấp 1
            ApplyTierToStatus(wallDataList.GetBaseTier());
            //load tier theo tier từ status
            LoadWallComponents(GetCurrentTier(),status.state);//cái đầu là data,cái 2 là state để biết load dạng wall hay stump
            
            // Xử lý logic sau khi load data
            UpdateMaxHp();
            status.currentHp = status.maxHp;  // Tường mới có HP đầy
            
            Debug.Log($"[Wall] Initialized. Tier: {status.tier}, MaxHP: {status.maxHp}, State: {status.state}");
        }
        private void Start()
        {
            
            UpdateCoinUserData();//cậpt nhật coinuser theo status
            RegisterWithManager();//đăng ký với manager
            SubscribeToWorldInfoChanges(); //đăng ký file thế giới (buff tăng máu)
        }
        public WallTierData GetCurrentTier()
        {
            return wallDataList.GetByTier(status.tier);
        }
        
        //TODO: kieemr tra lại  hàm này
        private void OnDestroy()
        {
            UnregisterFromManager();
            UnsubscribeFromWorldInfoChanges();//hủy lắng nghe worldinfo(script này sử dụng biến blessing)
        }
        
        //hàm này chỉ dùng để load data vào status
        void ApplyTierToStatus(WallTierData data)
        {
            // Copy tất cả data từ WallTierData sang WallStatus
            status.tier = data.tier;
            status.normalMaxHp = data.normalMaxHp;
            status.blessedMaxHp = data.blessedMaxHp;
            status.upgradeCost = data.upgradeCost;
            status.rebuildCost = data.reBuildCost;
            status.upgradeProgressMax = data.upgradeProgressMax;
            
            // Reset upgrade progress
            status.upgradeProgress = 0;
            
            // Kiểm tra xem tường max cấp chưa
            if (wallDataList.GetNext(data) != null)
            {
                status.isMaxTier = false;
            }
            else status.isMaxTier = true;
 
            
            Debug.Log($"[Wall] Applied tier {data.tier} data. MaxHP: {data.normalMaxHp}, UpgradeCost: {data.upgradeCost}, RebuildCost: {data.reBuildCost}");
            
        }
        
        #region Load new sprite and collider to current
    
        /// Load các component từ WallTierData.prefabWall và trích xuất collider/sprite renderer
        private void LoadWallComponents(WallTierData tierData,WallState wallState)
        {
            if (tierData?.prefabWall == null)
            {
                Debug.LogError("[Wall] WallTierData prefabWall is null", this);
                return;
            }
            
            CopySpriteRenderer(tierData, status.state);
            ChangeColliders(tierData, status.state);
        }
        
        /// Copy sprite renderer từ prefab
        private void CopySpriteRenderer(WallTierData tierData,WallState currentState)
        {
            SpriteRenderer prefabSpriteRenderer = null;
            if (currentState == WallState.Wall)
            {
                prefabSpriteRenderer = tierData.prefabWall.GetComponent<SpriteRenderer>();
            }
            else if (currentState == WallState.Stump)
            {
                prefabSpriteRenderer = tierData.prefabStump.GetComponent<SpriteRenderer>();
            }
            
            if (prefabSpriteRenderer != null && this.spriteRenderer != null)
            {
                this.spriteRenderer.sprite = prefabSpriteRenderer.sprite;
                this.spriteRenderer.color = prefabSpriteRenderer.color;
                this.spriteRenderer.sortingOrder = prefabSpriteRenderer.sortingOrder;
                this.spriteRenderer.sortingLayerName = prefabSpriteRenderer.sortingLayerName;
                
                Debug.Log($"[Wall] Copied sprite renderer from tier {tierData.tier}");
                
            }
        }
        
        /// Đổi colliders từ prefabdata vào collider hiện tại
        private void ChangeColliders(WallTierData tierData,WallState currentState)
        {
            GameObject sourcePrefab = currentState == WallState.Wall ? tierData.prefabWall : tierData.prefabStump;
            
            if (sourcePrefab == null)
            {
                Debug.LogError($"[Wall] Source prefab is null for state {currentState}");
                return;
            }
            
            // Lấy collider TRIGGER từ prefab CHÍNH (parent)
            Collider2D prefabTriggerCollider = null;
            Collider2D[] parentColliders = sourcePrefab.GetComponents<Collider2D>();
            
            foreach (var collider in parentColliders)
            {
                if (collider.isTrigger)
                {
                    prefabTriggerCollider = collider;
                    break;
                }
            }
            
            if (prefabTriggerCollider == null)
            {
                Debug.LogWarning("[Wall] No trigger collider found in prefab");
                return;
            }
            
            // Copy vào triggerCollider2D (trên GameObject chính)
            if (triggerCollider2D != null)
            {
                // Copy properties, GIỮ isTrigger = true
                CopyColliderProperties(prefabTriggerCollider, triggerCollider2D);
                triggerCollider2D.isTrigger = true;
                Debug.Log($"[Wall] Updated triggerCollider2D properties (isTrigger=true)");
            }
            else
            {
                // Tạo mới nếu chưa có
                triggerCollider2D = ReplaceCollider(prefabTriggerCollider, null);
                if (triggerCollider2D != null)
                {
                    triggerCollider2D.isTrigger = true;
                    Debug.Log($"[Wall] Created new triggerCollider2D (isTrigger=true)");
                }
            }
            
            // Copy VÀO CẢ blockerCollider2D (trên childBlocker) - giống properties nhưng isTrigger = false
            if (childBlocker != null)
            {
                blockerCollider2D = childBlocker.GetComponent<Collider2D>();
                
                if (blockerCollider2D != null)
                {
                    // Copy properties GIỐNG Y HỆT triggerCollider2D, nhưng isTrigger = false
                    CopyColliderProperties(prefabTriggerCollider, blockerCollider2D);
                    blockerCollider2D.isTrigger = false;
                    Debug.Log($"[Wall] Updated blockerCollider2D properties (isTrigger=false)");
                }
                else
                {
                    // Tạo mới trên childBlocker
                    blockerCollider2D = CreateColliderOnChild(prefabTriggerCollider, childBlocker);
                    if (blockerCollider2D != null)
                    {
                        blockerCollider2D.isTrigger = false;
                        Debug.Log($"[Wall] Created new blockerCollider2D on childBlocker (isTrigger=false)");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[Wall] childBlocker is null");
            }
        }
        
        /// Tạo collider mới trên child object
        private Collider2D CreateColliderOnChild(Collider2D prefabCollider, GameObject childObject)
        {
            Collider2D newCollider = null;
            
            if (prefabCollider is BoxCollider2D)
            {
                newCollider = childObject.AddComponent<BoxCollider2D>();
            }
            else if (prefabCollider is CircleCollider2D)
            {
                newCollider = childObject.AddComponent<CircleCollider2D>();
            }
            else if (prefabCollider is PolygonCollider2D)
            {
                newCollider = childObject.AddComponent<PolygonCollider2D>();
            }
            
            if (newCollider != null)
            {
                CopyColliderProperties(prefabCollider, newCollider);
            }
            
            return newCollider;
        }
        
        /// Thay thế hoặc tạo mới collider từ prefab
        private Collider2D ReplaceCollider(Collider2D prefabCollider, Collider2D oldCollider)
        {
            // Tạo collider mới từ prefab collider
            Collider2D newCollider = CreateColliderFromPrefab(prefabCollider);
            
            if (newCollider != null)
            {
                // Copy properties từ prefab
                CopyColliderProperties(prefabCollider, newCollider);
                
                // Destroy old collider nếu có
                if (oldCollider != null)
                {
                    DestroyImmediate(oldCollider);
                }
            }
            
            return newCollider;
        }
        
        /// Tạo collider mới từ prefab collider
        private Collider2D CreateColliderFromPrefab(Collider2D prefabCollider)
        {
            if (prefabCollider is BoxCollider2D)
            {
                return gameObject.AddComponent<BoxCollider2D>();
            }
            else if (prefabCollider is CircleCollider2D)
            {
                return gameObject.AddComponent<CircleCollider2D>();
            }
            else if (prefabCollider is PolygonCollider2D)
            {
                return gameObject.AddComponent<PolygonCollider2D>();
            }
            
            return null;
        }
        
        /// Copy tất cả thuộc tính từ prefab collider sang collider mới
        private void CopyColliderProperties(Collider2D prefabCollider, Collider2D newCollider)
        {
            // Copy thuộc tính chung
            newCollider.isTrigger = prefabCollider.isTrigger;
            newCollider.offset = prefabCollider.offset;
            
            // Copy thuộc tính riêng cho từng loại collider
            if (prefabCollider is BoxCollider2D prefabBox && newCollider is BoxCollider2D newBox)
            {
                newBox.size = prefabBox.size;
            }
            else if (prefabCollider is CircleCollider2D prefabCircle && newCollider is CircleCollider2D newCircle)
            {
                newCircle.radius = prefabCircle.radius;
            }
            else if (prefabCollider is PolygonCollider2D prefabPolygon && newCollider is PolygonCollider2D newPolygon)
            {
                newPolygon.points = prefabPolygon.points;
            }
        }
        
       
        #endregion
        
        #region Wall-Stump Conversion
        //[TODO] sau khi chuyển  giữa wall và stump sẽ thông báo lên manager
        //[TODO]: tạo wallmanager(kiểm tra buildmanager có dính gì tới không)

        //hàm này sẽ được gọi khi wall hết hp
        public void Collapse() //sụp đổ
        {
            status.state = WallState.Stump;
            
            // Lấy data stump từ tier hiện tại
            WallTierData currentTierData =GetCurrentTier();
            if (currentTierData?.prefabStump == null)
            {
                Debug.LogError("[Wall] Không tìm thấy prefabStump cho tier này", this);
                return;
            }
            
            // Chuyển đổi components
            ConvertToStump();
            
             // Cập nhật CoinUser cho chế độ sửa chữa
             UpdateCoinUserData();
             
             
             Debug.Log($"[Wall] Đã chuyển thành stump tại tier {status.tier}");
             
        }
        
        /// Chuyển đổi components để trở thành stump
        private void ConvertToStump()
        {
            LoadWallComponents(GetCurrentTier(),status.state);
            
            //sau khi load component mới của stump, vô hiệu hóa child gameobject blocker
          
            // Disable blocker object nếu có
            if (childBlocker != null)
            {
                childBlocker.SetActive(false);
            }
        }
        
        /// Chuyển đổi components để trở thành wall
        private void ConvertToWall()
        {
            LoadWallComponents(GetCurrentTier(),status.state);
            // Enable blocker object nếu có
            if (childBlocker != null)
            {
                childBlocker.SetActive(true);
            }
        }
        
        /// <summary>
        /// Sửa chữa stump (tạm thời tự động, sau này sẽ cần thợ xây)
        
        public void RepairStump()
        {
            
            // Reset HP về đầy
            status.currentHp = status.maxHp;
            status.state = WallState.Wall;
            LoadWallComponents(GetCurrentTier(),status.state);
                
            // Cập nhật CoinUser cho chế độ nâng cấp
            UpdateCoinUserData();
            
            // Thông báo cho manager
            OnWallRepaired?.Invoke(this);
        
        }
        /// <summary>
        /// hàm này được gọi sau khi wall vừa chuyển từ wall thành stump và ngược lại
        /// Cập nhật CoinUser theo WallState: Wall = nâng cấp, Stump = sửa chữa
        
        private void UpdateCoinUserData()
        {
            if (coinUser == null) return;
            
            if (status.state == WallState.Wall)
            {
                // Wall: Chế độ nâng cấp
                coinUser.requiredCoins = status.upgradeCost;
                coinUser.onUpgradeComplete = OnCoinCollectionComplete; // Gọi hàm xử lý chung
                coinUser.SetBuildingInfo(BuildingType.Wall, status.tier);
            
                bool canBeUpgraded = CanUpgradeByTownCenterLevel();
                bool isMaxTierForTownCenter = !canBeUpgraded;
                bool isMaxTierForWall = status.isMaxTier;
            
                coinUser.canAcceptCoins = !isMaxTierForTownCenter && !isMaxTierForWall;
            }
            else if (status.state == WallState.Stump)
            {
                // Stump: Chế độ sửa chữa
                coinUser.requiredCoins = status.rebuildCost;
                coinUser.onUpgradeComplete = OnCoinCollectionComplete; // Gọi hàm xử lý chung
                coinUser.SetBuildingInfo(BuildingType.Wall, status.tier);
                
                // Stump luôn có thể nhận coin để sửa chữa
                coinUser.canAcceptCoins = true;
            }
            
            
            Debug.Log($"[Wall] Updated CoinUser for {status.state}. Required coins: {coinUser.requiredCoins}, Can accept: {coinUser.canAcceptCoins}");
            
        }
        #endregion
        
        //sau khi thêm 2 hàm trên xong,tiếp tục phần đổi coinuser theo wallstate
        #region Initialization
        private void InitializeComponents()
        {
            coinUser = this.DebugUtilityGetRequiredComponent<CoinUser>();
            status = this.DebugUtilityGetRequiredComponent<WallStatus>();
        }
        private void RegisterWithManager()
        {
            if (WallAndStumpManager.Instance != null)
            {
                if (gameObject.CompareTag("Wall"))
                {
                    WallAndStumpManager.Instance.RegisterWall(gameObject);
                }
                else
                {
                    Debug.LogWarning($"[WallBehavior] GameObject {gameObject.name} has unexpected tag: {gameObject.tag}");
                }
            }
            else
            {
                Debug.LogError("[WallBehavior] WallAndStumpManager instance not found!");
            }
        }
        
        private void UnregisterFromManager()
        {
            if (WallAndStumpManager.Instance != null && gameObject.CompareTag("Wall"))
            {
                WallAndStumpManager.Instance.UnregisterWall(gameObject);
            }
        }
        
        private void SubscribeToWorldInfoChanges()
        {
            if (WorldFileManager.Instance != null)
            {
                WorldFileManager.Instance.OnWorldInfoChanged += OnWorldInfoChanged;
            }
        }
        
        private void UnsubscribeFromWorldInfoChanges()
        {
            if (WorldFileManager.Instance != null)
            {
                WorldFileManager.Instance.OnWorldInfoChanged -= OnWorldInfoChanged;
            }
        }
        #endregion
        
        #region Health System
        //điều chỉnh thông số max hp bằng cách kiểm tra buffbuff
        public void UpdateMaxHp()
        {
            bool techAdhesive = false;
            if (WorldFileManager.Instance != null && WorldFileManager.Instance.worldInfo != null)
            {
                techAdhesive = WorldFileManager.Instance.worldInfo.TechAdhesive;
            }
            status.maxHp = techAdhesive ? status.blessedMaxHp : status.normalMaxHp;
            status.currentHp = status.maxHp;
            
        }
        private void OnWorldInfoChanged(WorldInfo worldInfo)
        {
            UpdateMaxHp();
            Debug.Log($"Wall {gameObject.name} updated HP due to WorldInfo change. TechAdhesive: {worldInfo.TechAdhesive}, MaxHP: {status.maxHp}");
            
        }
        public void TakeDamage(int amount)
        {
            status.currentHp -= amount;
            if (status.currentHp <= 0)
            {
                // Tường bị xập -> gọi Die()
                //die là hàm đi theo interface để sử dụng interface takedame
                Die();
            }
        }
        //dùng cho cả stump và wall, là wall sẽ hồi hp, là stump thì sẽ thành wall khi đầy hp
        public void Heal(int amount)
        {
            if (status.state == WallState.Stump)
            {
                // Nếu là stump và heal đủ HP -> rebuild thành wall
                if (status.currentHp + amount >= status.maxHp)
                {
                     RepairStump();
                }
                else
                {
                    status.currentHp += amount;
                }
            }
            else
            {
                // Nếu là wall -> chỉ tăng HP
                status.currentHp = Mathf.Min(status.currentHp + amount, status.maxHp);
            }
            
            Debug.Log($"[Wall] Healed {amount} HP. Current HP: {status.currentHp}/{status.maxHp}");
            
        }
        public void Die()
        {
            Debug.Log($"{gameObject.name} đã bị xập");
            
            // Tường bị xập -> chuyển thành stump
            Collapse();
            
            // Thông báo cho manager biết tường bị xập
            OnWallDestroyed?.Invoke(this);
        }
        #endregion
        
        #region OnUpgradeComplete System
        //khi trigger,gọi hàm này kiểm tra xem có thể nâng cấp không để hiện coinslot 
        private bool CanUpgradeByTownCenterLevel()
        {
            if (BuildingManager.instance == null) return false;
            
            int townCenterLevel = BuildingManager.instance.currentTownCenter.GetComponent<TownCenterStatus>().tier;
            int maxAllowedTier = BuildingManager.instance.GetMaxAllowedTier(BuildingType.Wall);
            
            
            Debug.Log($"Town Center Level: {townCenterLevel}, Max Wall tier Allowed: {maxAllowedTier}, Current Wall tier: {status.tier}");
            
            return BuildingManager.instance.CanBuildingBeUpgraded(BuildingType.Wall, status.tier);
        }
        
         /// <summary>
         /// Xử lý khi nhét đủ coin (xử lý cả nâng cấp wall và sửa chữa stump)
         
        private void OnCoinCollectionComplete()
        {
             if (status.state == WallState.Wall)
             {
                 // Wall: Chế độ nâng cấp
            status.readyToUpgrade = true;
            
            if (PrefabData.Instance.skipBuildingProcess)
            {
                OnUpgradeComplete();
            }
            else
            {
                 Debug.LogWarning($"[Hệ thống xây dựng] Tường cấp {status.tier} tại vị trí {transform.position} đã sẵn sàng để nâng cấp! Cần thợ xây đến làm việc.");
            }
        }
             else if (status.state == WallState.Stump)
             {
                 // Stump: Chế độ sửa chữa
                 Debug.Log($"[Wall] Stump tại vị trí {transform.position} đã nhận đủ {status.rebuildCost} coin để sửa chữa!");
                 Debug.LogWarning($"[Hệ thống xây dựng] Stump cấp {status.tier} tại vị trí {transform.position} đã sẵn sàng để sửa chữa! Cần thợ xây đến làm việc.");
                 
                 // [TODO] Khi có thợ xây, sẽ gọi hàm RepairStump()
                 // Hiện tại: Tạm thời tự động sửa chữa để test
                 if (PrefabData.Instance.skipBuildingProcess)
                 {
                     RepairStump();
                 }
             }
         }
         

        //TODO: sửa lại hàm update này ,nó đang thay gameobejct mới thay vì load newtier
        public void OnUpgradeComplete()
        {
            if (!CanUpgradeByTownCenterLevel())
            {
                Debug.LogWarning($"Wall at tier {status.tier} cannot be upgraded: Town Center tier is too low.");
                
                return;
            }
            
            // Get next tier data
            WallTierData nextTierData = wallDataList.GetNext(GetCurrentTier());
            if (nextTierData != null)
            {
                Debug.Log($"Upgrading wall from tier {status.tier} to tier {nextTierData.tier}");
                

                status.state = WallState.Wall;
                ApplyTierToStatus(nextTierData);
                LoadWallComponents(nextTierData,status.state);
                
                status.currentHp = status.maxHp; //làm mới hp
                
                // QUAN TRỌNG: Reset CoinUser state để có thể nhận coin tiếp
                if (coinUser != null)
                {
                    coinUser.ResetUpgradeState();
                    Debug.Log("[WallBehavior] CoinUser reset để có thể nhận coin tiếp");
                }
                
                // Cập nhật CoinUser với cost mới
                UpdateCoinUserData();
            
                OnWallUpgraded?.Invoke(this);
            }
            else
            {
                Debug.LogWarning($"Cannot upgrade wall at tier {status.tier}: No next tier data available");
                
            }
        }
        
        public int GetUpgradeCost()
        {
            return status.upgradeCost;
        }
        //sửa sau khi thêm builder
        public void AddWorkingProcess(int amount)
        {
            status.upgradeProgress += amount;
            if (status.upgradeProgress > status.upgradeProgressMax)
            {
                status.upgradeProgress = status.upgradeProgressMax;
            }
            
            Debug.LogWarning($"[WallBehavior] AddWorkingProcess: upgradeProgress = {status.upgradeProgress}/{status.upgradeProgressMax}");
            
            
            if (status.upgradeProgress >= status.upgradeProgressMax)
            {
                Debug.LogWarning("[WallBehavior] upgradeProgress reached max, upgrading!");
                OnUpgradeComplete();
            }
        }
        
        
        #endregion
        
        #region ICoinReceiver Methods
        public bool ReceiveCoin(int amount, GameObject player)
        {
            if (coinUser != null)
            {
                return coinUser.ReceiveCoin(amount, player);
            }
            return false;
        }
        
        public void PerformUpgrade()
        {
            if (coinUser != null)
            {
                coinUser.PerformUpgrade();
            }
        }
        
        public void ResetUpgradeState()
        {
            if (coinUser != null)
            {
                coinUser.ResetUpgradeState();
            }
        }
        #endregion
        
        #region Save System Implementation
        [System.Serializable]
        public class WallBehaviorData
        {
            public float positionX;
            public float positionY;
            public int tier;
            public int currentHp;
            public int maxHp;
            public bool readyToUpgrade;
            public int buildProgress;
            public int buildProgressMax;
            public WallState wallState;
        }
        //kiểm tra lại cách hệ thống save
        public string GetPrefabID()
        {
            // Return prefab ID based on current state and tier
            if (status.state == WallState.Stump)
            {
                return $"stump_{status.tier}";
            }
            return $"wall_{status.tier}";
        }
        
        public object CaptureData()
        {
            return new WallBehaviorData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                tier = status.tier,
                currentHp = status.currentHp,
                maxHp = status.maxHp,
                readyToUpgrade = status.readyToUpgrade,
                buildProgress = status.upgradeProgress,
                buildProgressMax = status.upgradeProgressMax,
                wallState = status.state
            };
        }
        
        public void RestoreData(object data)
        {
            WallBehaviorData wallData = (WallBehaviorData)data;
            transform.position = new Vector3(wallData.positionX, wallData.positionY, 0);
            
            // Lưu lại các giá trị từ save data
            int savedCurrentHp = wallData.currentHp;
            int savedMaxHp = wallData.maxHp;
            bool savedReadyToUpgrade = wallData.readyToUpgrade;
            int savedBuildProgress = wallData.buildProgress;
            int savedBuildProgressMax = wallData.buildProgressMax;
            WallState savedWallState = wallData.wallState;
            // Khôi phục state TRƯỚC KHI load components để sử dụng đúng prefab wall/stump
            status.state = savedWallState;
            
            // Apply tier data based on saved tier
            WallTierData tierData =  wallDataList.GetByTier(wallData.tier);
            if (tierData != null)
            {
                ApplyTierToStatus(tierData);
                LoadWallComponents(tierData, status.state);
                
                // Xử lý logic sau khi load data
                UpdateMaxHp();  // Tính toán maxHp dựa trên tech hiện tại
            }
            else
            {
                Debug.LogError("wall.tierData is null");
            }
            
            // Khôi phục giá trị từ save data
            status.currentHp = savedCurrentHp;
            status.readyToUpgrade = savedReadyToUpgrade;
            status.upgradeProgress = savedBuildProgress;
            status.upgradeProgressMax = savedBuildProgressMax;
            
            // Đảm bảo HP không âm và không vượt quá maxHp
            if (status.currentHp <= 0)
            {
                status.currentHp = status.maxHp;
            }
            else if (status.currentHp > status.maxHp)
            {
                status.currentHp = status.maxHp;
            }
        }
        #endregion
    }
} 

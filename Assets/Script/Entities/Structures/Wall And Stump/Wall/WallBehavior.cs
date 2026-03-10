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
        public event Action<GameObject> OnCriticalHP; // Sự kiện khi máu wall xuống thấp critical
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
    
            // Load cấp 1 (base tier)
            WallTierData baseTierData = wallDataList.GetBaseTier();
            InitializeFromTier(baseTierData, WallState.Wall);//helper khởi tạo data 
           // Debug.Log($"[Wall] Initialized. Tier: {status.tier}, MaxHP: {status.maxHp}, State: {status.state}");
        }
        private void Start()
        {

            UpdateCoinUserData();//cậpt nhật coinuser theo status
            RegisterWithManager();//đăng ký với manager
            SubscribeToWorldInfoChanges(); //đăng ký file thế giới (buff tăng máu)

            // FIX: Wall mới build cũng cần check upgrade status sau khi BuildingManager ready
            StartCoroutine(DelayedUpgradeCheck());
        }
        public WallTierData GetCurrentTier()
        {
            return wallDataList.GetByTier(status.tier);
        }
        
        
        private void OnDestroy()
        {
            UnregisterFromManager();
            UnsubscribeFromWorldInfoChanges();//hủy lắng nghe worldinfo(script này sử dụng biến blessing)
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                // Khi player trigger wall, cập nhật lại trạng thái coin user
                // để đảm bảo check town center level một lần nữa
                UpdateCoinUserData();
              //  Debug.Log($"[Wall] Player triggered wall - rechecking upgrade capability");
            }
        }
        
        /// Sao chép data từ inputStatus vào status hiện tại, sau đó load tier và components dựa trên status mới.
        public void ApplyAndLoadFromStatus(WallStatus inputStatus)
        {
            if (inputStatus == null)
            {
                Debug.LogError("[WallBehavior] Input status is null");
                return;
            }

            // Sao chép (paste) toàn bộ fields từ inputStatus vào status
            status.tier = inputStatus.tier;
            status.state = inputStatus.state;
            status.maxHp = inputStatus.maxHp;
            status.currentHp = inputStatus.currentHp;
            status.normalMaxHp = inputStatus.normalMaxHp;
            status.blessedMaxHp = inputStatus.blessedMaxHp;
            status.upgradeCost = inputStatus.upgradeCost;
            status.readyToUpgrade = inputStatus.readyToUpgrade;
            status.isMaxTier = inputStatus.isMaxTier;
            status.upgradeProgress = inputStatus.upgradeProgress;
            status.upgradeProgressMax = inputStatus.upgradeProgressMax;
            status.minimumDistance = inputStatus.minimumDistance;
            status.rebuildCost = inputStatus.rebuildCost;

            // Load tier từ status.tier mới
            WallTierData tierData = wallDataList.GetByTier(status.tier);
            if (tierData != null)
            {
                // Apply tier data nếu cần (cập nhật các fields từ tierData, nhưng không overwrite những fields đã sao chép từ input)
                ApplyTierToStatus(tierData);  // Gọi nếu bạn muốn sync với tierData (ví dụ: update upgradeCost nếu cần)

                // Load components dựa trên state và tier
                LoadWallComponents(tierData, status.state);
            }
            else
            {
                Debug.LogError($"[WallBehavior] No tier data found for tier {status.tier}");
            }

            // Cập nhật thêm (nếu cần): Update maxHp dựa trên world info
            UpdateMaxHp();

            // Cập nhật coinUser nếu cần
            UpdateCoinUserData();

         //   Debug.Log($"[WallBehavior] Applied status from input. New tier: {status.tier}, State: {status.state}");

            // Sau khi load xong data, cập nhật lại trạng thái defendable trên manager
            if (WallAndStumpManager.Instance != null)
            {
                WallAndStumpManager.Instance.UpdateDefendableWalls();
            }
        }
        //hàm này chỉ dùng để load data vào status
        
        // Hàm này chỉ dùng để load data vào status từ WallTierData
        private void ApplyTierToStatus(WallTierData data)
        {
            if (data == null)
            {
                Debug.LogError("[Wall] WallTierData is null");
                return;
            }

            // Sao chép data từ WallTierData sang WallStatus
            status.tier = data.tier;
            status.normalMaxHp = data.normalMaxHp;
            status.blessedMaxHp = data.blessedMaxHp;
            status.upgradeCost = data.upgradeCost;
            status.rebuildCost = data.rebuildCost;  // Sửa typo từ rebuildCost thành rebuildCost để nhất quán
            if (status.upgradeProgressMax == 0)  // Chỉ set nếu chưa set, tránh overwrite building process max
            {
                status.upgradeProgressMax = data.upgradeProgressMax;
            }

            // Kiểm tra xem tường max cấp chưa
            status.isMaxTier = (wallDataList.GetNext(data) == null);

            Debug.Log($"[Wall] Applied tier {data.tier} data. MaxHP: {data.normalMaxHp}, UpgradeCost: {data.upgradeCost}, RebuildCost: {data.rebuildCost}");
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
                
               // Debug.Log($"[Wall] Copied sprite renderer from tier {tierData.tier}");
                
            }
        }
        
        /// Đổi colliders từ prefabdata vào collider hiện tại
        /// Theo logic mới: xóa collider cũ -> setup trigger -> setup blocker (nếu tier > 0)
        private void ChangeColliders(WallTierData tierData, WallState currentState)
        {
           // Debug.Log($"[Wall-Debug] Bắt đầu ChangeColliders cho {gameObject.name}. State: {currentState}, Tier: {tierData.tier}");

            // Bước 1: Lấy data từ tier hiện tại
            WallTierData currentTierData = GetCurrentTier();
            if (currentTierData == null || currentTierData.prefabWall == null)
            {
                Debug.LogError("[Wall-Debug] Không thể lấy data tier hoặc prefabWall null");
                return;
            }

            // Bước 2: Xóa tất cả collider cũ trong gameObject và children
            RemoveAllColliders();

            // Bước 3: Lấy collider từ prefab data
            Collider2D sourceCollider = GetTriggerColliderFromPrefab(currentTierData.prefabWall);
            if (sourceCollider == null)
            {
                Debug.LogWarning("[Wall-Debug] Không tìm thấy trigger collider trong prefab");
                return;
            }

            // Bước 4: Setup TRIGGER COLLIDER - luôn có cho mọi tier
            SetupTriggerCollider(sourceCollider);

            // Bước 5: Setup BLOCKER COLLIDER - chỉ cho tier > 0 (wall chặn được enemy)
            if (status.tier > 0)
            {
                SetupBlockerCollider(sourceCollider);
            }
            else
            {
                // Tier 0: không có blocker collider
                blockerCollider2D = null;
             //   Debug.Log($"[Wall-Debug] Tier {status.tier} không cần blocker collider");
            }

            // Đảm bảo childBlocker được setup đúng
            if (childBlocker != null)
            {
                childBlocker.transform.localPosition = Vector3.zero;
                childBlocker.transform.localRotation = Quaternion.identity;
                childBlocker.transform.localScale = Vector3.one;
            }
            //Debug.Log($"[Wall-Debug] Hoàn tất ChangeColliders cho {gameObject.name}.");
        }

        /// Xóa tất cả Collider2D trong gameObject chính và tất cả children
        private void RemoveAllColliders()
        {
            //Debug.Log($"[Wall-Debug] Bắt đầu RemoveAllColliders cho {gameObject.name}");
            
            // Xóa collider trong gameObject chính
            Collider2D[] mainColliders = GetComponents<Collider2D>();
            //Debug.Log($"[Wall-Debug] Tìm thấy {mainColliders.Length} colliders trên Main Object ({gameObject.name})");
            foreach (var collider in mainColliders)
            {
                if (collider != null)
                {
                    //Debug.Log($"[Wall-Debug] >> Đánh dấu DESTROY collider: {collider.GetType().Name} trên {gameObject.name}");
                    Destroy(collider); // Sử dụng Destroy thay vì DestroyImmediate để tránh lỗi trong physics callback
                }
            }

            // Xóa collider trong childBlocker Explicitly
            if (childBlocker != null)
            {
                Collider2D[] blockerColliders = childBlocker.GetComponents<Collider2D>();
               // Debug.Log($"[Wall-Debug] Tìm thấy {blockerColliders.Length} colliders trên ChildBlocker ({childBlocker.name})");
                foreach (var collider in blockerColliders)
                {
                    if (collider != null)
                    {
                        //Debug.Log($"[Wall-Debug] >> Đánh dấu DESTROY collider: {collider.GetType().Name} trên {childBlocker.name}");
                        Destroy(collider);
                    }
                }
            }
            else
            {
                Debug.LogError($"[Wall-Debug] ChildBlocker là null trên {gameObject.name}!");
            }

            // Xóa collider trong tất cả children khác (quét dọn sạch sẽ)
            foreach (Transform child in transform)
            {
                // Skip childBlocker vì đã xử lý ở trên
                if (childBlocker != null && child == childBlocker.transform) continue;

                Collider2D[] childColliders = child.GetComponents<Collider2D>();
                if (childColliders.Length > 0)
                {
                   // Debug.Log($"[Wall-Debug] Tìm thấy {childColliders.Length} colliders trên Child khác ({child.name})");
                    foreach (var collider in childColliders)
                    {
                        if (collider != null)
                        {
                            //Debug.Log($"[Wall-Debug] >> Đánh dấu DESTROY collider: {collider.GetType().Name} trên {child.name}");
                            Destroy(collider);
                        }
                    }
                }
            }

            // Reset references
            triggerCollider2D = null;
            blockerCollider2D = null;
            
            // Verify sau khi xóa
            int remainingColliders = GetComponentsInChildren<Collider2D>(true).Length;
          //  Debug.Log($"[Wall-Debug] Kết thúc RemoveAllColliders. Tổng số collider còn active (lưu ý: Destroy sẽ thực hiện cuối frame nên số lượng chưa giảm ngay): {remainingColliders}");
        }

        /// Lấy trigger collider từ prefab
        private Collider2D GetTriggerColliderFromPrefab(GameObject prefab)
        {
            if (prefab == null) return null;

            Collider2D[] colliders = prefab.GetComponents<Collider2D>();
            foreach (var collider in colliders)
            {
                if (collider.isTrigger)
                {
                    return collider;
                }
            }
            return null;
        }

        /// Setup trigger collider trên gameObject chính (luôn trigger = true)
        private void SetupTriggerCollider(Collider2D sourceCollider)
        {
            triggerCollider2D = CreateColliderFromPrefab(sourceCollider);
            if (triggerCollider2D != null)
            {
                CopyColliderProperties(sourceCollider, triggerCollider2D);
                triggerCollider2D.isTrigger = true;
               // Debug.Log($"[Wall-Debug] Đã tạo mới Trigger Collider ({triggerCollider2D.GetType().Name}) trên {gameObject.name}");
            }
            else
            {
                Debug.LogError($"[Wall-Debug] Thất bại khi tạo Trigger Collider trên {gameObject.name}");
            }
        }

        /// Setup blocker collider trên childBlocker (trigger = false, chỉ cho tier > 0)
        private void SetupBlockerCollider(Collider2D sourceCollider)
        {
            if (childBlocker == null)
            {
                Debug.LogWarning("[Wall-Debug] childBlocker null, không thể setup blocker collider");
                return;
            }

            blockerCollider2D = CreateColliderOnChild(sourceCollider, childBlocker);
            if (blockerCollider2D != null)
            {
                CopyColliderProperties(sourceCollider, blockerCollider2D);
                blockerCollider2D.isTrigger = false;
               // Debug.Log($"[Wall-Debug] Đã tạo mới Blocker Collider ({blockerCollider2D.GetType().Name}) trên {childBlocker.name}");
            }
            else
            {
                Debug.LogError($"[Wall-Debug] Thất bại khi tạo Blocker Collider trên {childBlocker.name}");
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
             
             
           //  Debug.Log($"[Wall] Đã chuyển thành stump tại tier {status.tier}");
             
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
            
            // Explicitly re-enable childBlocker to ensure physics blocker is active
            if (childBlocker != null)
            {
                childBlocker.SetActive(true);
              //  Debug.Log($"[Wall-Debug] RepairStump: Reactivated childBlocker for {gameObject.name}");
            }
            else
            {
                Debug.LogWarning($"[Wall-Debug] RepairStump: childBlocker is null for {gameObject.name}");
            }

            // Cập nhật CoinUser cho chế độ nâng cấp
            UpdateCoinUserData();
            
            // Thông báo cho manager
            OnWallRepaired?.Invoke(this);

            // Sync with manager explicitly
            if (WallAndStumpManager.Instance != null)
            {
                WallAndStumpManager.Instance.UpdateDefendableWalls();
            }
        
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

                // Kiểm tra khả năng nâng cấp thực tế thay vì dùng cached value
                bool canActuallyUpgrade = CanUpgradeByTownCenterLevel();
                bool isMaxTierForWall = status.isMaxTier;

                coinUser.canAcceptCoins = canActuallyUpgrade && !isMaxTierForWall;
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


           // Debug.Log($"[Wall] Updated CoinUser for {status.state}. Required coins: {coinUser.requiredCoins}, Can accept: {coinUser.canAcceptCoins}");

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
            //Debug.Log($"Wall {gameObject.name} updated HP due to WorldInfo change. TechAdhesive: {worldInfo.TechAdhesive}, MaxHP: {status.maxHp}");
            
        }
        public void TakeDamage(int amount)
        {
            status.currentHp -= amount;
            
            // Notify manager to check critical HP logic (still needed for other systems if any)
            if (WallAndStumpManager.Instance != null)
            {
                WallAndStumpManager.Instance.CheckWallCriticalHP(gameObject);
                
                // NEW: Trigger internal event system for GuardManager
                CheckAndFireCriticalEvent(WallAndStumpManager.Instance.criticalHPThreshold);
            }
            
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
                     return; // Đã xử lý xong notify trong RepairStump, return để tránh duplicate
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
            
            // NEW LOGIC: Chỉ gọi OnWallRepaired khi wall đã ĐẦY MÁU
            // Điều này tránh spam update logic đội hình mỗi lần gõ búa
            if (WallAndStumpManager.Instance != null)
            {
                // Luôn cập nhật trạng thái reset cờ critical (để đảm bảo không báo động giả)
                 ResetCriticalFlag(WallAndStumpManager.Instance.criticalHPThreshold);

                // Chỉ notify Manager khi đầy máu
                if (status.currentHp >= status.maxHp)
                {
                    WallAndStumpManager.Instance.OnWallRepaired(gameObject);
                    WallAndStumpManager.Instance.UpdateDefendableWalls();
                }
                // KHÔNG GỌI UpdateDefendableWalls mỗi lần Heal nữa
                // Vì danh sách defendable chỉ cần update khi:
                // 1. Wall đầy máu (đã gọi ở trên)
                // 2. Wall vượt ngưỡng critical lần đầu (event OnCriticalHP đã xử lý)
                // 3. Wall bị phá hủy/xây mới (RegisterWall/UnregisterWall đã gọi)
            }
            
           // Debug.Log($"[Wall] Healed {amount} HP. Current HP: {status.currentHp}/{status.maxHp}");
            
        }
        public void Die()
        {
            Debug.Log($"{gameObject.name} đã bị xập");
            
            // Tường bị xập -> chuyển thành stump
            Collapse();
            
            // Thông báo cho manager biết tường bị xập
            OnWallDestroyed?.Invoke(this);
            
            // Note: WallAndStumpManager handles unregister/register in its own logic or via this event?
            // Actually, Collapse() usually doesn't destroy the object, just changes state.
            // But if it needs to update defendable list:
            if (WallAndStumpManager.Instance != null)
            {
                WallAndStumpManager.Instance.UpdateDefendableWalls();
            }
        }
        #endregion

        #region Critical HP Event Logic
        private bool hasFiredCriticalEvent = false;

        public void CheckAndFireCriticalEvent(int criticalThreshold)
        {
            if (!hasFiredCriticalEvent && status.currentHp < criticalThreshold)
            {
                hasFiredCriticalEvent = true;
                OnCriticalHP?.Invoke(gameObject);
            }
        }
        
        public void ResetCriticalFlag(int criticalThreshold)
        {
            if (status.currentHp >= criticalThreshold)
            {
                hasFiredCriticalEvent = false;
            }
        }
        #endregion
        
        #region OnUpgradeComplete System
        //khi trigger,gọi hàm này kiểm tra xem có thể nâng cấp không để hiện coinslot 
        private bool CanUpgradeByTownCenterLevel()
        {
            if (BuildingManager.instance == null)
            {
                Debug.LogError("[Wall] BuildingManager.instance is null");
                return false;
            }

            if (BuildingManager.instance.currentTownCenter == null)
            {
               Debug.LogError("[Wall] currentTownCenter is null");
                return false;
            }

            var townCenterStatus = BuildingManager.instance.currentTownCenter.GetComponent<TownCenterStatus>();
            if (townCenterStatus == null)
            {
                Debug.LogError("[Wall] TownCenterStatus component not found on currentTownCenter");
                return false;
            }

            int townCenterLevel = townCenterStatus.tier;
            int maxAllowedTier = BuildingManager.instance.GetMaxAllowedTier(BuildingType.Wall);
            bool canUpgrade = BuildingManager.instance.CanBuildingBeUpgraded(BuildingType.Wall, status.tier);

            // Detailed logging now handled in GetWallDebugInfo()

            return canUpgrade;
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
                // Debug.Log($"[Wall] Stump tại vị trí {transform.position} đã nhận đủ {status.rebuildCost} coin để sửa chữa!");
                 //Debug.LogWarning($"[Hệ thống xây dựng] Stump cấp {status.tier} tại vị trí {transform.position} đã sẵn sàng để sửa chữa! Cần thợ xây đến làm việc.");
                 
                 // [TODO] Khi có thợ xây, sẽ gọi hàm RepairStump()
                 // Hiện tại: Tạm thời tự động sửa chữa để test
                 if (PrefabData.Instance.skipBuildingProcess)
                 {
                     RepairStump();
                 }
             }
         }
         

        
      
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
               // Debug.Log($"Upgrading wall from tier {status.tier} to tier {nextTierData.tier}");

                // Sử dụng helper để khởi tạo từ tier mới và state Wall
                InitializeFromTier(nextTierData, WallState.Wall);

                // QUAN TRỌNG: Reset CoinUser state để có thể nhận coin tiếp
                if (coinUser != null)
                {
                    coinUser.ResetUpgradeState();
                   // Debug.Log("[WallBehavior] CoinUser reset để có thể nhận coin tiếp");
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
            
           // Debug.LogWarning($"[WallBehavior] AddWorkingProcess: upgradeProgress = {status.upgradeProgress}/{status.upgradeProgressMax}");
            
            
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
            public bool readyToUpgrade;
            public int buildProgress;
            public WallState wallState;
            public float scaleX = 1f;
        }
        
        public string GetPrefabID()
        {
            // Return prefab ID based on current state (bỏ tier vì tier lưu trong data, load base prefab)
            return status.state == WallState.Stump ? "stump" : "wall";
        }
        
        public object CaptureData()
        {
            return new WallBehaviorData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                scaleX = transform.localScale.x,
                tier = status.tier,
                currentHp = status.currentHp,
                readyToUpgrade = status.readyToUpgrade,
                buildProgress = status.upgradeProgress,
                wallState = status.state
            };
        }
        
        public void RestoreData(object data)
        {
            WallBehaviorData wallData = (WallBehaviorData)data;
            transform.position = new Vector3(wallData.positionX, wallData.positionY, 0);

            // Lưu lại các giá trị từ save data để overwrite sau
            int savedCurrentHp = wallData.currentHp;
            bool savedReadyToUpgrade = wallData.readyToUpgrade;
            int savedBuildProgress = wallData.buildProgress;
            WallState savedWallState = wallData.wallState;

            // Apply tier data based on saved tier
            WallTierData tierData = wallDataList.GetByTier(wallData.tier);
            if (tierData != null)
            {
                // Sử dụng helper để khởi tạo từ tier và state từ save
                InitializeFromTier(tierData, savedWallState);
            }
            else
            {
                Debug.LogError("[WallBehavior] wall.tierData is null during restore");
            }

            // Overwrite giá trị từ save data (sau khi init để không bị reset)
            status.currentHp = savedCurrentHp;
            status.readyToUpgrade = savedReadyToUpgrade;
            status.upgradeProgress = savedBuildProgress;
           
            // Đảm bảo HP không âm và không vượt quá maxHp
            // Note: Stump có thể có hp <= 0 (đang chờ sửa), chỉ auto-revive nếu là Wall bị lỗi data
            if (status.currentHp < 0)
            {
                status.currentHp = 0;
            }
            else if (status.currentHp > status.maxHp)
            {
                status.currentHp = status.maxHp;
            }
            // Flip hướng nếu scaleX từ save
            transform.localScale = new Vector3(wallData.scaleX, transform.localScale.y, transform.localScale.z);
            
            // Debug log trạng thái trước khi cập nhật
           // Debug.Log($"[WallBehavior] Before Update - coinUser: {coinUser != null}, readyToUpgrade: {status.readyToUpgrade}, State: {status.state}, Tier: {status.tier}");
            
            // Kiểm tra và cập nhật readyToUpgrade dựa trên trạng thái hiện tại
            // FIX: Defer việc kiểm tra CanUpgradeByTownCenterLevel() đến sau khi load hoàn tất
            // để tránh trường hợp TownCenter chưa được set
            if (status.state == WallState.Wall)
            {
                // Tạm thời set readyToUpgrade = true để có thể nhận coin
                // Sẽ được update lại sau khi TownCenter được load xong
                status.readyToUpgrade = true;
               // Debug.Log("[WallBehavior] Temporarily set readyToUpgrade to true during restore (will be updated after TownCenter loads)");
            }
            else if (status.state == WallState.Stump)
            {
                // Nếu là gốc cây, luôn cho phép ném tiền để xây lại
                status.readyToUpgrade = true;
                //.Log("[WallBehavior] Stump state - Set readyToUpgrade to true");
            }

            // Cập nhật coinUser với trạng thái tạm thời
            UpdateCoinUserData();

            // Debug log trạng thái sau khi cập nhật
            // Debug.Log($"[WallBehavior] After UpdateCoinUserData - canAcceptCoins: {coinUser?.canAcceptCoins}");

            // Đảm bảo coinUser có thể nhận coin nếu readyToUpgrade là true
            if (coinUser != null)
            {
                coinUser.canAcceptCoins = status.readyToUpgrade;
               // Debug.Log($"[WallBehavior] Final set - canAcceptCoins: {coinUser.canAcceptCoins}, readyToUpgrade: {status.readyToUpgrade}");
            }
            else
            {
                Debug.LogError("[WallBehavior] coinUser is null when trying to set canAcceptCoins");
                coinUser = GetComponentInChildren<CoinUser>(true);
                if (coinUser != null)
                {
                  //  Debug.Log($"[WallBehavior] Đã tìm thấy CoinUser. Trạng thái: {status.state}, Có thể nhận coin: {coinUser.canAcceptCoins}");
                }
                else
                {
                    Debug.LogError("[WallBehavior] Failed to find CoinUser in children");
                }
            }

            // Schedule update sau khi TownCenter được load xong
            StartCoroutine(DelayedUpgradeCheck());

           // Debug.Log($"[WallBehavior] Restored from save. Tier: {status.tier}, State: {status.state}, HP: {status.currentHp}/{status.maxHp}, ReadyToUpgrade: {status.readyToUpgrade}, CanAcceptCoins: {coinUser?.canAcceptCoins}");
        }
        #endregion
        
        /// Update sprite, colliders, và set active blocker dựa trên tier và state. Giảm lặp code ở các nơi thay đổi state/tier.
        private void UpdateVisualAndPhysics(WallTierData tierData, WallState currentState)
        {
            LoadWallComponents(tierData, currentState);

            // Set active blocker dựa trên state
            if (childBlocker != null)
            {
                childBlocker.SetActive(currentState == WallState.Wall);
            }
        }
        
        /// Khởi tạo từ tier data và state. Giảm lặp ở Awake và RestoreData.
        private void InitializeFromTier(WallTierData tierData, WallState initialState)
        {
            status.state = initialState;
            ApplyTierToStatus(tierData);
            UpdateVisualAndPhysics(tierData, initialState);
            UpdateMaxHp();
            status.currentHp = status.maxHp;
        }
     
        /// Load tier/components từ current status (tier/state), update maxHp/coin. Dùng cho RestoreData sau set fields.
        private void LoadFromCurrentStatus()
        {
            WallTierData tierData = wallDataList.GetByTier(status.tier);
            if (tierData != null)
            {
                ApplyTierToStatus(tierData);  // Sync costs/maxHp variants từ tierData (không overwrite progress)
                UpdateVisualAndPhysics(tierData, status.state);  // Load components theo tier/state
            }
            else
            {
                Debug.LogError($"[WallBehavior] No tier data found for tier {status.tier}");
            }

            UpdateMaxHp();  // Update maxHp dựa trên world info
            UpdateCoinUserData();  // Update coin theo state
        }

        /// Coroutine để kiểm tra upgrade status sau khi TownCenter đã được load xong
        private System.Collections.IEnumerator DelayedUpgradeCheck()
        {
            // Đợi 1 frame để đảm bảo TownCenter đã được set
            yield return null;

            // Kiểm tra lại upgrade status với TownCenter đã load
            if (status.state == WallState.Wall)
            {
                bool canActuallyUpgrade = CanUpgradeByTownCenterLevel();
                status.readyToUpgrade = canActuallyUpgrade;

                // Cập nhật CoinUser với trạng thái chính xác
                if (coinUser != null)
                {
                    coinUser.canAcceptCoins = canActuallyUpgrade;
                  //  Debug.Log($"[WallBehavior] Delayed upgrade check - Can upgrade: {canActuallyUpgrade}, CanAcceptCoins: {coinUser.canAcceptCoins}");
                }
            }
            // Stump không cần kiểm tra lại vì luôn có thể nhận coin
        }

        #region Debug Methods
        /// <summary>
        /// Debug method để tạo string thông tin wall khi player trigger
        /// </summary>
        public string GetWallDebugInfo()
        {
            string tcInfo = "NULL";
            bool canUpgradeByTC = false;

            if (BuildingManager.instance?.currentTownCenter != null)
            {
                var tcStatus = BuildingManager.instance.currentTownCenter.GetComponent<TownCenterStatus>();
                if (tcStatus != null)
                {
                    tcInfo = $"Lv.{tcStatus.tier}";
                    canUpgradeByTC = CanUpgradeByTownCenterLevel();
                }
            }

            string coinUserInfo = "NULL";
            if (coinUser != null)
            {
                coinUserInfo = $"Accept:{coinUser.canAcceptCoins}, Req:{coinUser.requiredCoins}, Collected:{coinUser.collectedCoins}, Ready:{coinUser.isReadyToUpgrade}, Trigger:{coinUser.playerTrigger}";
            }

            return $"WALL[{gameObject.name}]: Pos{transform.position}, State:{status.state}, Tier:{status.tier}, HP:{status.currentHp}/{status.maxHp}, ReadyUpgrade:{status.readyToUpgrade}, MaxTier:{status.isMaxTier}, UpgradeCost:{status.upgradeCost}, RebuildCost:{status.rebuildCost}, TC:{tcInfo}, CanUpgradeByTC:{canUpgradeByTC}, CoinUser:{coinUserInfo}, CanAccept:{CanAcceptCoins}, IsReady:{IsReadyToUpgrade}";
        }
        #endregion
    }
}

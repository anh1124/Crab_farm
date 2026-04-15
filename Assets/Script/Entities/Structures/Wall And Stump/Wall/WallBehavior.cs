using System;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using Script.Entities.Structures.Wall_And_Stump.Wall;
using Script.Interfaces;
using Script.Game.Item.Coin;
using Script.Game.Other;
using Script.Game.Structures.Wall_And_Stump;
using Script.Game.Mobs.MobsManager;
using Script.Manager.Building;
using Script.Save_Controller;
using UnityEngine;
using UnityEngine.Serialization;
using static Script.Utils.DebugUtility;

namespace Script.Game.Structures.Wall_And_Stump
{
    //wall tier 0 không nhận dame
    // Chứa logic behavior của Wall, sử dụng WallStatus để quản lý state
    [RequireComponent(typeof(Script.Utils.DamageFlashEffect))]
    public class WallBehavior : MonoBehaviour, ISaveable, IDamageable, ICoinUserOwner
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

        [Header("Hồi máu bằng coin (CoinUser, không dùng LootPickupTrigger)")]
        [Tooltip("Khi tường thiếu máu và GuardManager không ở trạng thái enemy attack: mỗi lần đủ 1 coin → hồi tỷ lệ maxHp.")]
        [SerializeField] [Range(0.01f, 1f)] private float coinHealMaxHpFraction = 0.5f;

        private GuardManager _guardManager;
        #endregion
        
        [SerializeField] public WallDataList wallDataList;
       
        private BuildingManager _bm;
        private BuildingManager BM => _bm != null ? _bm : (_bm = FindObjectOfType<BuildingManager>());
       
        // Events cho wall manager
        public event Action<WallBehavior> OnWallDestroyed;//thông báo cho manager xếp lại đội hình lính và rút lui
        public event Action<WallBehavior> OnWallUpgraded;//wall sẵn sàng upgrade ,bảo manager điều thợ đến xây
        public event Action<WallBehavior> OnWallRepaired;//thông báo cho manager xếp lại đội hình vì tường vừa sửa xong
        public event Action<GameObject> OnCriticalHP; // Sự kiện khi máu wall xuống thấp critical
        //add more even ondame,onupgradedone,onrepaireddone,onlowhealth
        
        
        #region Properties
        public WallStatus Status => status;

        // Upgrade State Properties
        public int Tier { get => status.tier; set => status.tier = value; }
        public int UpgradeCost { get => status.upgradeCost; set => status.upgradeCost = value; }
       public bool ReadyToUpgrade { get => status.readyToUpgrade; set => status.readyToUpgrade = value; }
        public bool MaxTier { get => status.isMaxTier; set => status.isMaxTier = value; }
        
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
            _guardManager = FindObjectOfType<GuardManager>(true);
            InitializeComponents();
    
            // Load cấp 1 (base tier)
            WallTierData baseTierData = wallDataList.GetBaseTier();
            InitializeFromTier(baseTierData, WallState.Wall);//helper khởi tạo data 
           // Debug.Log($"[Wall] Initialized. Tier: {status.tier}, MaxHP: {status.maxHp}, State: {status.state}");
        }
        private void Start()
        {
            // [State First] Đảm bảo visual và data khớp với status.state trước khi chạy tiếp
            LoadFromCurrentStatus();

            RegisterWithManager(); // Đăng ký với manager (đã bao gồm sync tag và list)
            
            SubscribeToWorldInfoChanges(); // Đăng ký buff máu từ thế giới
            if (coinUser != null) coinUser.RefreshSlotVisibility(); // Đồng bộ trạng thái khe cắm tiền

            // Đăng ký nhận sự kiện máu từ Status
            status.OnDeath += OnDeathHandler;
            status.OnDamaged += OnDamagedHandler;
            status.OnHealed += OnHealedHandler;

            // FIX: Wall mới build cũng cần check upgrade status sau khi BuildingManager ready
            StartCoroutine(DelayedUpgradeCheck());
        }
        public WallTierData GetCurrentTier()
        {
            return wallDataList.GetByTier(status.tier);
        }
        
        
        private void OnDisable()
        {
            // [Cleanup] Đảm bảo gỡ bỏ khỏi manager khi object bị disable (về pool hoặc bị tắt)
            UnregisterFromManager();
            UnsubscribeFromWorldInfoChanges();
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
                // Khi player trigger wall, CoinUser sẽ tự gọi GetRequiredCoinCount()
                // và CanReceiveCoins() từ ICoinUserOwner interface
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

            // Cập nhật maxHp dựa trên world info (coinUser tự reset lại data)
            UpdateMaxHp();

         //   Debug.Log($"[WallBehavior] Applied status from input. New tier: {status.tier}, State: {status.state}");

            // Sau khi load xong data, cập nhật lại danh sách và phân loại
            if (WallAndStumpManager.Instance != null)
            {
                WallAndStumpManager.Instance.SortAndNotify();
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
    
        private void LoadWallComponents(WallTierData tierData,WallState wallState)
        {
            if (tierData == null) return;
            
            CopySpriteRenderer(tierData, status.state);
            ChangeColliders(tierData, status.state);
        }
        
        /// Copy sprite renderer từ prefab
        private void CopySpriteRenderer(WallTierData tierData,WallState currentState)
        {
            SpriteRenderer targetRenderer = GetComponent<SpriteRenderer>();
            if (targetRenderer == null) return;

            // Lấy prefab tương ứng với state
            GameObject prefab = (currentState == WallState.Wall) ? tierData.prefabWall : tierData.prefabStump;
            if (prefab != null)
            {
                SpriteRenderer prefabRenderer = prefab.GetComponent<SpriteRenderer>();
                if (prefabRenderer != null)
                {
                    targetRenderer.sprite = prefabRenderer.sprite;
                    targetRenderer.color = prefabRenderer.color;
                    targetRenderer.sortingOrder = prefabRenderer.sortingOrder;
                    targetRenderer.sortingLayerName = prefabRenderer.sortingLayerName;
                }
            }
        }
        
        private Coroutine _swapColliderCoroutine;
        
        private void ChangeColliders(WallTierData tierData, WallState currentState)
        {
            WallTierData currentTierData = GetCurrentTier();
            if (currentTierData == null) return;

            GameObject prefab = (currentState == WallState.Wall) ? currentTierData.prefabWall : currentTierData.prefabStump;
            if (prefab == null) return;

            Collider2D sourceCollider = GetTriggerColliderFromPrefab(prefab);
            if (sourceCollider == null) return;

            if (_swapColliderCoroutine != null) StopCoroutine(_swapColliderCoroutine);
            _swapColliderCoroutine = StartCoroutine(SwapColliderNextFrame(sourceCollider, currentState));
        }

        private System.Collections.IEnumerator SwapColliderNextFrame(Collider2D sourceCollider, WallState wallState)
        {
            // Disable + Destroy collider cũ (Destroy là deferred, phải chờ 1 frame)
            foreach (var col in GetComponents<Collider2D>())
            {
                if (col != null) { col.enabled = false; Destroy(col); }
            }

            yield return null; // Chờ 1 frame để Unity dọn xong

            // Add collider mới với đúng isTrigger
            Collider2D newCol = CreateColliderFromPrefab(sourceCollider);
            if (newCol != null)
            {
                CopyColliderProperties(sourceCollider, newCol);
                // Wall luôn là solid (không trigger); Stump copy từ prefab
                if (wallState == WallState.Wall) newCol.isTrigger = false;
            }
            
            _swapColliderCoroutine = null;
        }

        private void RemoveAllColliders()
        {
            Collider2D[] colliders = GetComponents<Collider2D>();
            foreach (var col in colliders)
            {
                if (col != null)
                {
                    col.enabled = false;
                    Destroy(col);
                }
            }
        }

        /// Lấy collider đầu tiên từ root của prefab
        private Collider2D GetTriggerColliderFromPrefab(GameObject prefab)
        {
            if (prefab == null) return null;
            return prefab.GetComponent<Collider2D>();
        }

        /// Setup collider trên gameObject chính
        private void SetupCollider(Collider2D sourceCollider)
        {
            Collider2D newCol = CreateColliderFromPrefab(sourceCollider);
            if (newCol != null)
            {
                // Tự động copy isTrigger và các thuộc tính khác từ source
                CopyColliderProperties(sourceCollider, newCol);
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
                // IMPORTANT: `.points` chỉ copy path[0]. Nếu PolygonCollider có nhiều paths
                // (ví dụ: collider dạng donut hoặc tách rời), phải copy từng path riêng.
                newPolygon.pathCount = prefabPolygon.pathCount;
                for (int i = 0; i < prefabPolygon.pathCount; i++)
                {
                    newPolygon.SetPath(i, prefabPolygon.GetPath(i));
                }
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

            // [TAG] Chuyển tag thành 'Stump' để hệ thống khác (GuardManager v.v.) biết wall này không còn hoạt động
            gameObject.tag = Script.Utils.GameTags.Stump;

            // [SECURITY] Đảm bảo bắn event lùi quân nếu chưa bắn (trường hợp bị phá hủy quá nhanh)
            int threshold = (WallAndStumpManager.Instance != null) ? WallAndStumpManager.Instance.criticalHPThreshold : 10;
            CheckAndFireCriticalEvent(threshold);
            
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
             if (coinUser != null) coinUser.RefreshSlotVisibility();

             // Thông báo cho WallAndStumpManager: đồng bộ lại state mới (chuyển sang Stump list)
             SyncRegistrationWithManager();

             
             
           //  Debug.Log($"[Wall] Đã chuyển thành stump tại tier {status.tier}");
             
        }
        
        /// Chuyển đổi components để trở thành stump
        private void ConvertToStump()
        {
            LoadWallComponents(GetCurrentTier(),status.state);
        }
        
        /// Chuyển đổi components để trở thành wall
        private void ConvertToWall()
        {
            LoadWallComponents(GetCurrentTier(),status.state);
        }
        
        /// <summary>
        /// Sửa chữa stump (tạm thời tự động, sau này sẽ cần thợ xây)
        
        public void RepairStump()
        {
            // Reset HP về đầy (dùng property để bắn OnHealthChanged cho HPText và listener khác)
            status.CurrentHp = status.MaxHp;
            status.state = WallState.Wall;

            // [TAG] Khôi phục tag thành 'Wall' để hệ thống khác nhận biết wall đã chạy lại
            gameObject.tag = Script.Utils.GameTags.Wall;

            LoadWallComponents(GetCurrentTier(), status.state);

            // Gọi ResetUpgradeState để CoinUser setup lại slot nếu đang trong trigger
            if (coinUser != null)
            {
                coinUser.ResetUpgradeState();
            }

            // Thông báo cho manager
            OnWallRepaired?.Invoke(this);

            // [Flow] Đồng bộ lại state mới (chuyển về Wall list)
            SyncRegistrationWithManager();
        }
        /// <summary>Enemy đang tấn công (GuardManager) → không cho hồi máu bằng 1 coin.</summary>
        private bool IsEnemyCombatBlockingCoinHeal()
        {
            if (_guardManager == null) _guardManager = FindObjectOfType<GuardManager>(true);
            return _guardManager != null && _guardManager.isEnemyAttack;
        }

        /// <summary>Tường còn Wall, thiếu máu, và không trong combat → CoinUser chỉ cần 1 coin để hồi.</summary>
        private bool CanOfferCoinHealForWall()
        {
            if (status.state != WallState.Wall) return false;
            if (status.maxHp <= 0f) return false;
            return status.currentHp < status.maxHp && !IsEnemyCombatBlockingCoinHeal();
        }

        // --- ICoinUserOwner Implementation ---
        public int GetRequiredCoinCount()
        {
            if (status.state == WallState.Wall)
            {
                if (CanOfferCoinHealForWall()) return 1;
                return status.upgradeCost;
            }

            return status.rebuildCost;
        }

        public bool CanReceiveCoins()
        {
            if (status.state == WallState.Wall)
            {
                if (CanOfferCoinHealForWall()) return true;

                bool canActuallyUpgrade = CanUpgradeByTownCenterLevel();
                return canActuallyUpgrade && !status.isMaxTier;
            }

            return true;
        }

        public void OnAllCoinsCollected()
        {
            if (status.state == WallState.Wall)
            {
                if (status.currentHp < status.maxHp && !IsEnemyCombatBlockingCoinHeal())
                {
                    status.Heal(status.maxHp * coinHealMaxHpFraction);
                    if (coinUser != null) coinUser.ResetUpgradeState();
                    OnWallRepaired?.Invoke(this);
                    if (WallAndStumpManager.Instance != null)
                        WallAndStumpManager.Instance.SortAndNotify();
                    return;
                }

                OnCoinCollectionComplete();
            }
            else if (status.state == WallState.Stump)
            {
                RepairStump();
            }
        }
        // -------------------------------------
        
        #endregion
        
        //sau khi thêm 2 hàm trên xong,tiếp tục phần đổi coinuser theo wallstate
        #region Initialization
        private void InitializeComponents()
        {
            coinUser = this.DebugUtilityGetRequiredComponent<CoinUser>();
            status = this.DebugUtilityGetRequiredComponent<WallStatus>();
            
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();

            // Xóa legacy children: Blocker, Visual
            Transform blocker = transform.Find("Blocker");
            if (blocker != null) Destroy(blocker.gameObject);
            Transform visual = transform.Find("Visual");
            if (visual != null) Destroy(visual.gameObject);
        }
        private void RegisterWithManager()
        {
            if (status == null)
            {
                Debug.LogError($"[WallBehavior] {gameObject.name} RegisterWithManager failed: Status component is missing!");
                return;
            }

            // Đồng bộ tag và danh sách dựa trên status.state hiện tại
            SyncRegistrationWithManager();
        }

        private void SyncRegistrationWithManager()
        {
            if (WallAndStumpManager.Instance == null) return;

            // 1. Đảm bảo tag khớp với state hiện tại
            if (status.state == WallState.Stump)
                gameObject.tag = Script.Utils.GameTags.Stump;
            else
                gameObject.tag = Script.Utils.GameTags.Wall;

            // 2. Đồng bộ lại với Manager (Hàm UnregisterEntity sẽ xóa khỏi mọi list cũ, RegisterEntity sẽ thêm vào list mới dựa trên Tag)
            WallAndStumpManager.Instance.UnregisterEntity(gameObject);
            WallAndStumpManager.Instance.RegisterEntity(gameObject);
        }

        private void UnregisterFromManager()
        {
            if (WallAndStumpManager.Instance != null)
            {
                WallAndStumpManager.Instance.UnregisterEntity(gameObject);
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
            float nextMax = techAdhesive ? status.blessedMaxHp : status.normalMaxHp;
            status.MaxHp = nextMax;
            status.CurrentHp = status.MaxHp;
        }
        private void OnWorldInfoChanged(WorldInfo worldInfo)
        {
            UpdateMaxHp();
            //Debug.Log($"Wall {gameObject.name} updated HP due to WorldInfo change. TechAdhesive: {worldInfo.TechAdhesive}, MaxHP: {status.maxHp}");
            
        }
        private void OnDamagedHandler(float amount)
        {
            // Notify manager and guard system
            if (WallAndStumpManager.Instance != null)
            {
                CheckAndFireCriticalEvent(WallAndStumpManager.Instance.criticalHPThreshold);
            }
        }

        private void OnHealedHandler(float amount)
        {
            if (status.state == WallState.Stump)
            {
                // Nếu là stump và heal đủ HP -> rebuild thành wall
                if (status.currentHp >= status.maxHp)
                {
                    RepairStump();
                }
            }
            
            if (WallAndStumpManager.Instance != null)
            {
                ResetCriticalFlag(WallAndStumpManager.Instance.criticalHPThreshold);

                if (status.currentHp >= status.maxHp)
                {
                    WallAndStumpManager.Instance.SortAndNotify();
                }
            }
        }

        private void OnDeathHandler()
        {
            Debug.Log($"{gameObject.name} đã bị xập");
            Collapse();
            OnWallDestroyed?.Invoke(this);
        }

        // --- IDamageable Implementation ---
        public void ReceiveDamage(float damage, GameObject instigator)
        {
            if (!this.enabled || gameObject == null || !gameObject.activeSelf) return;

            // Tier 0 (Stump) không thể bị target hoặc phá hủy bởi enemy
            if (status.tier == 0 && instigator != null && instigator.CompareTag(Script.Utils.GameTags.Enemy))
                return;

            status.TakeDamage(damage);
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
                WallAndStumpManager.Instance.SortAndNotify();
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
            if (BM == null)
            {
                Debug.LogError("[Wall] BM is null");
                return false;
            }

            if (BM.currentTownCenter == null)
            {
               Debug.LogError("[Wall] currentTownCenter is null");
                return false;
            }

            var townCenterStatus = BM.currentTownCenter.GetComponent<TownCenterStatus>();
            if (townCenterStatus == null)
            {
                Debug.LogError("[Wall] TownCenterStatus component not found on currentTownCenter");
                return false;
            }

            int townCenterLevel = townCenterStatus.tier;
            int maxAllowedTier = BM.GetMaxAllowedTier(BuildingType.Wall);
            bool canUpgrade = BM.CanBuildingBeUpgraded(BuildingType.Wall, status.tier);

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
                if (coinUser != null) coinUser.RefreshSlotVisibility();

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
        

        
        #region Save System Implementation
        [System.Serializable]
        public class WallBehaviorData
        {
            public float positionX;
            public float positionY;
            public int tier;
            public float currentHp;
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
            float savedCurrentHp = wallData.currentHp;
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
            if (coinUser != null) coinUser.RefreshSlotVisibility();

            // Debug log trạng thái sau khi cập nhật
            // Debug.Log($"[WallBehavior] After RefreshSlotVisibility");

            // Đảm bảo coinUser có thể nhận coin nếu readyToUpgrade là true
            if (coinUser != null)
            {
                coinUser.RefreshSlotVisibility();
               // Debug.Log($"[WallBehavior] Final set - readyToUpgrade: {status.readyToUpgrade}");
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

           // Debug.Log($"[WallBehavior] Restored from save. Tier: {status.tier}, State: {status.state}, HP: {status.currentHp}/{status.maxHp}, ReadyToUpgrade: {status.readyToUpgrade}");
        }
        #endregion
        
        /// Update sprite, colliders, và set active blocker dựa trên tier và state. Giảm lặp code ở các nơi thay đổi state/tier.
        private void UpdateVisualAndPhysics(WallTierData tierData, WallState currentState)
        {
            LoadWallComponents(tierData, currentState);
        }
        
        /// Khởi tạo từ tier data và state. Giảm lặp ở Awake và RestoreData.
        private void InitializeFromTier(WallTierData tierData, WallState initialState)
        {
            status.state = initialState;
            ApplyTierToStatus(tierData);
            UpdateVisualAndPhysics(tierData, initialState);
            UpdateMaxHp();
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
            if (coinUser != null) coinUser.RefreshSlotVisibility();  // Update coin theo state
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
                    coinUser.RefreshSlotVisibility();
                  //  Debug.Log($"[WallBehavior] Delayed upgrade check - Can upgrade: {canActuallyUpgrade}");
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

            if (BM?.currentTownCenter != null)
            {
                var tcStatus = BM.currentTownCenter.GetComponent<TownCenterStatus>();
                if (tcStatus != null)
                {
                    tcInfo = $"Lv.{tcStatus.tier}";
                    canUpgradeByTC = CanUpgradeByTownCenterLevel();
                }
            }

            string coinUserInfo = "NULL";
            if (coinUser != null)
            {
                coinUserInfo = coinUser.GetTriggerDebugInfo(out _);
            }

            return $"WALL[{gameObject.name}]: Pos{transform.position}, State:{status.state}, Tier:{status.tier}, HP:{status.currentHp}/{status.maxHp}, ReadyUpgrade:{status.readyToUpgrade}, MaxTier:{status.isMaxTier}, UpgradeCost:{status.upgradeCost}, RebuildCost:{status.rebuildCost}, TC:{tcInfo}, CanUpgradeByTC:{canUpgradeByTC}, CoinUser:{coinUserInfo}";
        }
        #endregion
        
        [ContextMenu("Debug: Force to Critical HP")]
        public void DebugForceToCriticalHP()
        {
            if (WallAndStumpManager.Instance == null)
            {
                Debug.LogError("[WallDebug] WallAndStumpManager.Instance is null! Cannot find threshold.");
                return;
            }

            int threshold = WallAndStumpManager.Instance.criticalHPThreshold;
            
            // QUAN TRỌNG: threshold và targetHp dùng float để khớp với status.currentHp
            float targetHp = threshold - 1f;
            float damageToInfict = status.currentHp - targetHp;

            if (damageToInfict > 0)
            {
                Debug.Log($"[Debug] Forcing {gameObject.name} to critical state. Inflicting {damageToInfict} damage to reach {targetHp} HP.");
                status.TakeDamage(damageToInfict);
            }
            else
            {
                Debug.LogWarning($"[Debug] Wall {gameObject.name} is already below or at critical HP!");
            }
        }
    }
}

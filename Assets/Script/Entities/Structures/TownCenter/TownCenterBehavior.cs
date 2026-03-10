using System;
using UnityEngine;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using Script.Game.Item.Coin;
using Script.Manager.Building;
using UnityEngine.Serialization;
using Script.Interfaces;

namespace Script.Game.Structures.TownCenter
{
    // Behavior for TownCenter, with upgrade via CoinUser
    
    /*
     hiện tại,dùng tạm data trong TownCenterDataList,chưa táoave như bên  wall vì bên wall cần collider
     */
    public class TownCenterBehavior : MonoBehaviour, IUpgradeable, ISaveable, IDamageable
    {
        
        public TownCenterStatus status ;
        public float plazaDistance = 20f;
        public CoinUser coinUser;
        [FormerlySerializedAs("currentPrefab")] public GameObject curentPrefab;
        
        [Header("Debug")]
        [SerializeField] private bool isDebugPlazaZone = true;
        
        private void Awake()
        {
            if (coinUser == null) coinUser = GetComponent<CoinUser>();
        }

        private void Start()
        {
            // Ensure TownCenterDataList is ready before using it
            EnsureDataListReady();

            // lấy TownCenterStatus component
            status = GetComponent<TownCenterStatus>();
            if (status == null)
            {
                Debug.LogError("[TownCenterBehavior] Missing required component TownCenterStatus");
                return;
            }

           // Debug.Log($"[TownCenterBehavior] Starting. Current tier: {status.tier}");

            // Nếu tier chưa đúng (ví dụ từ save cũ không lưu tier), thử suy ra từ visuals
            TryInferTierFromVisuals();

            // Sync status data from TownCenterDataList based on current tier
            GetDataFromDataList();

            Ini();
            BuildingManager.instance.currentTownCenter = gameObject;//tự khai báo đến BuildingManager
            BuildingManager.instance.UpdateAvailableBuildings(); // Cập nhật giới hạn công trình ngay khi TownCenter xuất hiện
            
            // Tự khai báo luôn cho EnemyTarget
            if (Script.Game.Enemy.EnemyTargetManager.Instance != null)
            {
                Script.Game.Enemy.EnemyTargetManager.Instance.SetPrimaryTarget(gameObject);
            }



        }

        private void Update()
        {
            if (isDebugPlazaZone)
            {
                Vector3 center = transform.position;
                Vector3 start = new Vector3(center.x - plazaDistance, center.y + 5f, center.z);
                Vector3 end = new Vector3(center.x + plazaDistance, center.y + 5f, center.z);
                Debug.DrawLine(start, end, Color.white);
            }
        }

        private void AddAndRemoveTentForTier2()
        {
            if (status.tier == 2)
            {
                // Check if tent already exists to avoid duplicates
                if (transform.Find("tent") == null)
                {
                    var tent = Instantiate(PrefabData.Instance.tent, transform);
                    tent.name = "tent";
                    Debug.Log("Added tent as child");
                }
            }
            else if (status.tier == 3)
            {
                var tent = transform.Find("tent");
                if (tent != null)
                {
                    Destroy(tent.gameObject);
                    Debug.LogWarning("destroy tent");
                }
            }
        }

        private void Ini()
        {
            // Khởi tạo coinUser sau khi status đã sẵn sàng
            if (coinUser != null)
            {
                coinUser.requiredCoins = status.upgradeCost;
                coinUser.onUpgradeComplete = Upgrade;
                coinUser.SetBuildingInfo(BuildingType.TownCenter, status.tier);
                
              //  Debug.Log($"[TownCenterBehavior] Initialized CoinUser. Tier: {status.tier}, Required: {status.upgradeCost}, Can accept: {coinUser.canAcceptCoins}");
            }
            else
            {
                Debug.LogError("[TownCenterBehavior] CoinUser is null!");
            }
        }

        private void EnsureDataListReady()
        {
            try
            {
                // Force initialize TownCenterDataList if not already done
                if (TownCenterDataList.Instance == null)
                {
                    Debug.LogWarning("[TownCenterBehavior] TownCenterDataList.Instance is null, trying to initialize...");
                    var dataList = FindObjectOfType<TownCenterDataList>(true);
                    if (dataList != null)
                    {
                        dataList.InitializeData(PrefabData.Instance);
                        Debug.Log("[TownCenterBehavior] TownCenterDataList initialized manually");
                    }
                    else
                    {
                        Debug.LogError("[TownCenterBehavior] TownCenterDataList not found in scene!");
                    }
                }
                else
                {
                    // Ensure it's initialized
                    TownCenterDataList.Instance.InitializeData(PrefabData.Instance);
                    Debug.Log("[TownCenterBehavior] TownCenterDataList ready");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[TownCenterBehavior] Failed to ensure TownCenterDataList ready: {ex.Message}");
                throw; // Re-throw to fail initialization
            }
        }

        private void GetDataFromDataList()
        {
            var dataList = TownCenterDataList.Instance;
            if (dataList == null)
            {
                Debug.LogError("[TownCenterBehavior] TownCenterDataList.Instance is null");
                return;
            }
            var data = dataList.GetByTier(status.tier);
            if (data == null)
            {
                Debug.LogError($"[TownCenterBehavior] Không tìm thấy data cho tier {status.tier}");
                return;
            }
            status.upgradeCost = data.upgradeCost;
            curentPrefab = data.curentPrefab;
            Debug.Log($"[TownCenterBehavior] Data loaded. Tier: {status.tier}, UpgradeCost: {status.upgradeCost}");
        }

        private void Upgrade()
        {
            status.readyToUpgrade = true;
            //ở đây do chưa tạo builder nên chạy  OnUpgradeComplete luôn
            Debug.LogWarning("[TownCenterBehavior] call OnUpgradeComplete() vì chưa có builder");
            
            OnUpgradeComplete();
        }

        public void OnUpgradeComplete()
        {
            var nextTierData = GetNextTierData();
            if (nextTierData == null)
            {
                Debug.LogError("nextTierData is null. towncenterberhavior can't upgrade");
                return;
            }
            UpdateVisuals(nextTierData);
            UpdateStatusAndData(nextTierData);
            UpdateCoinUser(nextTierData);
            NotifyBuildingManager();
            AddAndRemoveTentForTier2();
            
            Debug.Log($"[TownCenterBehavior] Upgraded to tier {status.tier}");
        }

        private TownCenterDataList.TownCenterData GetNextTierData()
        {
            var data = TownCenterDataList.Instance.GetByTier(status.tier + 1);
            if (data == null)
            {
                Debug.LogError($"[TownCenterBehavior] Không có tier tiếp theo.");
                return null;
            }
            return data;
        }

        private void UpdateVisuals(TownCenterDataList.TownCenterData data)
        {
            UpdateSpriteRenderer(data);
            UpdateAnimator(data);
        }

        private void UpdateSpriteRenderer(TownCenterDataList.TownCenterData data)
        {
            if (data.curentPrefab == null)
            {
                Debug.LogWarning("[TownCenterBehavior] data.curentPrefab is null");
                return;
            }

            var currentSpriteRenderer = GetComponent<SpriteRenderer>();
            if (currentSpriteRenderer == null) return;
            
            var prefabSpriteRenderer = data.curentPrefab.GetComponent<SpriteRenderer>();
            if (prefabSpriteRenderer != null)
            {
                currentSpriteRenderer.sprite = prefabSpriteRenderer.sprite;
                Debug.Log($"[TownCenterBehavior] Sprite updated for tier {data.tier}");
            }
            else
            {
                Debug.LogWarning("[TownCenterBehavior] SpriteRenderer không tìm thấy trong prefab");
            }
        }

        private void UpdateAnimator(TownCenterDataList.TownCenterData data)
        {
            var currentAnimator = GetComponent<Animator>();
            var prefabAnimator = data.curentPrefab != null ? data.curentPrefab.GetComponent<Animator>() : null;

            if (prefabAnimator != null)
            {
                if (currentAnimator == null)
                {
                    currentAnimator = gameObject.AddComponent<Animator>();
                    Debug.Log("[TownCenterBehavior] Added Animator component");
                }
                currentAnimator.runtimeAnimatorController = prefabAnimator.runtimeAnimatorController;
                Debug.Log("[TownCenterBehavior] Animator controller updated");
            }
            else if (currentAnimator != null)
            {
                Debug.Log("[TownCenterBehavior] New prefab doesn't have animation, removing current Animator");
                Destroy(currentAnimator);
            }
        }

        private void UpdateStatusAndData(TownCenterDataList.TownCenterData data)
        {
            status.tier = data.tier;
            status.upgradeCost = data.upgradeCost;
            curentPrefab = data.curentPrefab;
        }

        private void UpdateCoinUser(TownCenterDataList.TownCenterData data)
        {
            if (coinUser == null) return;
            
            coinUser.requiredCoins = data.upgradeCost;
            coinUser.SetBuildingInfo(BuildingType.TownCenter, data.tier);
            coinUser.ResetUpgradeState();
            Debug.Log("[TownCenterBehavior] CoinUser reset để có thể nhận coin tiếp");
        }

        private void NotifyBuildingManager()
        {
            if (BuildingManager.instance != null)
            {
                BuildingManager.instance.UpdateAvailableBuildings();
            }
        }

        public int GetUpgradeCost() => status.upgradeCost;

        // --- IDamageable Implementation ---
        public void ReceiveDamage(int damage, GameObject instigator)
        {
            if (damage <= 0) return;
            
            status.currentHp -= damage;
            Debug.Log($"[TownCenter] Received {damage} damage from {instigator?.name ?? "unknown"}. HP: {status.currentHp}/{status.maxHp}");
            
            if (status.currentHp <= 0)
            {
                status.currentHp = 0;
                OnTownCenterDestroyed();
            }
        }

        private void OnTownCenterDestroyed()
        {
            Debug.LogError("[TownCenter] GAME OVER - Town Center has been destroyed!");
            // Thêm logic Game Over hoặc phá hủy tại đây
            // Destroy(gameObject); // Thường thì Game Over thay vì biến mất hoàn toàn
        }
  
        public void AddWorkingProcess(int amount)
        {
            status.buildProgress = Mathf.Min(status.buildProgress + amount, status.buildProgressMax);
            if (status.buildProgress >= status.buildProgressMax) OnUpgradeComplete();
        }

        [Serializable]
        public class TownCenterData
        {
            public float positionX;
            public float positionY;
            public int tier;
            public bool readyToUpgrade;
            public int buildProgress;
            public int buildProgressMax;
        }
        public string GetPrefabID() => $"town_center_{status.tier}";
        public object CaptureData() => new TownCenterData
        {
            positionX = transform.position.x,
            positionY = transform.position.y,
            tier = status.tier,
            readyToUpgrade = status.readyToUpgrade,
            buildProgress = status.buildProgress,
            buildProgressMax = status.buildProgressMax
        };
        public void RestoreData(object data)
        {
            var d = (TownCenterData)data;
            if (status == null) status = GetComponent<TownCenterStatus>();
            transform.position = new Vector3(d.positionX, d.positionY, 0);
            // Khôi phục tier trước để đồng bộ lại dữ liệu phụ thuộc (fallback suy luận nếu dữ liệu cũ không có tier)
            if (d.tier > 0)
            {
                status.tier = d.tier;
            }
            else
            {
                // Nếu tier đã được đặt trước (ví dụ bởi EntitySaveManager dựa trên prefabID), giữ nguyên
                if (status.tier <= 0)
                {
                    TryInferTierFromVisuals();
                }
            }
            // Đồng bộ lại dữ liệu theo tier hiện tại (upgradeCost, prefab tham chiếu, ...)
            GetDataFromDataList();

            // Phần trạng thái còn lại
            status.readyToUpgrade = d.readyToUpgrade;
            status.buildProgress = d.buildProgress;
            status.buildProgressMax = d.buildProgressMax;
            if (coinUser == null) coinUser = GetComponent<CoinUser>();
            if (coinUser != null)
            {
                coinUser.requiredCoins = status.upgradeCost;
                coinUser.onUpgradeComplete = Upgrade;
                coinUser.SetBuildingInfo(BuildingType.TownCenter, status.tier);
            }
            // Thông báo BuildingManager tham chiếu lại currentTownCenter nếu cần
            if (BuildingManager.instance != null)
            {
                BuildingManager.instance.currentTownCenter = gameObject;
                BuildingManager.instance.UpdateAvailableBuildings();
                //Debug.Log("[TownCenterBehavior] Updated BuildingManager with current town center and available buildings");
            }

            // Tự khai báo lại cho EnemyTargetManager khi load save
            if (Script.Game.Enemy.EnemyTargetManager.Instance != null)
            {
                Script.Game.Enemy.EnemyTargetManager.Instance.SetPrimaryTarget(gameObject);
            }
        }

        // Cố gắng suy ra tier hiện tại dựa trên Sprite/Animator của đối tượng so với danh sách prefab trong PrefabData
        private void TryInferTierFromVisuals()
        {
            try
            {
                if (status != null && status.tier > 0) return;
                if (PrefabData.Instance == null || PrefabData.Instance.townCenterList == null) return;

                var sr = GetComponent<SpriteRenderer>();
                var anim = GetComponent<Animator>();
                for (int i = 0; i < PrefabData.Instance.townCenterList.Count; i++)
                {
                    var prefab = PrefabData.Instance.townCenterList[i];
                    if (prefab == null) continue;
                    bool match = false;
                    var psr = prefab.GetComponent<SpriteRenderer>();
                    if (sr != null && psr != null && sr.sprite == psr.sprite)
                    {
                        match = true;
                    }
                    if (!match)
                    {
                        var pAnim = prefab.GetComponent<Animator>();
                        if (anim != null && pAnim != null && anim.runtimeAnimatorController == pAnim.runtimeAnimatorController)
                        {
                            match = true;
                        }
                    }
                    if (match)
                    {
                        status.tier = i;
                        Debug.Log($"[TownCenterBehavior] Inferred tier from visuals: {status.tier}");
                        break;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[TownCenterBehavior] Failed to infer tier from visuals: {e.Message}");
            }
        }
    }
}

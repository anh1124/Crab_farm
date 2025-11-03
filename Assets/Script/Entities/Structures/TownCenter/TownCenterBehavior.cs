using System;
using UnityEngine;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using Script.Game.Item.Coin;
using Script.Game.Other;
using Script.Manager.Building;
using Script.Utils;
using UnityEngine.Serialization;

namespace Script.Game.Structures.TownCenter
{
    // Behavior for TownCenter, with upgrade via CoinUser
    
    /*
     hiện tại,dùng tạm data trong TownCenterDataList,chưa táoave như bên  wall vì bên wall cần collider
     */
    public class TownCenterBehavior : MonoBehaviour, IUpgradeable, ISaveable
    {
        
        public TownCenterStatus status ;
        public float plazaDistance;
        public CoinUser coinUser;
        [FormerlySerializedAs("currentPrefab")] public GameObject curentPrefab;
        
        private void Awake()
        {
            if (coinUser == null) coinUser = GetComponent<CoinUser>();
            GetPlazaDistance();
        }

        private void Start()
        {
            // lấy TownCenterStatus component
            status = GetComponent<TownCenterStatus>();
            if (status == null)
            {
                Debug.LogError("[TownCenterBehavior] Missing required component TownCenterStatus");
                return;
            }
            
            Debug.Log($"[TownCenterBehavior] Starting. Current tier: {status.tier}");
            
            // Sync status data from TownCenterDataList based on current tier
            GetDataFromDataList();
            
            Ini();
            BuildingManager.instance.currentTownCenter = gameObject;//tự khai báo đến BuildingManager
        }

        private void Ini()
        {
            // Khởi tạo coinUser sau khi status đã sẵn sàng
            if (coinUser != null)
            {
                coinUser.requiredCoins = status.upgradeCost;
                coinUser.onUpgradeComplete = Upgrade;
                coinUser.SetBuildingInfo(BuildingType.TownCenter, status.tier);
                
                Debug.Log($"[TownCenterBehavior] Initialized CoinUser. Tier: {status.tier}, Required: {status.upgradeCost}, Can accept: {coinUser.canAcceptCoins}");
            }
            else
            {
                Debug.LogError("[TownCenterBehavior] CoinUser is null!");
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
            //chạy nâng cấp,tiến hành thay đổi sprite và data
            var data = TownCenterDataList.Instance.GetByTier(status.tier + 1);//lấy data mới
            
            // kiểm tra xem có tier tiếp theo không
            if (data == null)
            {
                Debug.LogError($"[TownCenterBehavior] Không có tier tiếp theo.");
                return;
            }
            
            // thay đổi sprite từ prefab mới
            SpriteRenderer currentSpriteRenderer = GetComponent<SpriteRenderer>();
            if (currentSpriteRenderer != null && data.curentPrefab != null)
            {
                SpriteRenderer prefabSpriteRenderer = data.curentPrefab.GetComponent<SpriteRenderer>();
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
            else
            {
                Debug.LogWarning("[TownCenterBehavior] SpriteRenderer hoặc data.curentPrefab is null");
            }
            
           
            // copy animation nếu prefab mới có animation
            Animator currentAnimator = GetComponent<Animator>();
            Animator prefabAnimator = data.curentPrefab != null ? data.curentPrefab.GetComponent<Animator>() : null;

            if (prefabAnimator != null)
            {
                // prefab mới có animation
                if (currentAnimator == null)
                {
                    // thêm Animator vào gameObject nếu chưa có
                    currentAnimator = gameObject.AddComponent<Animator>();
                    Debug.Log("[TownCenterBehavior] Added Animator component");
                }

                // copy animator controller từ prefab
                currentAnimator.runtimeAnimatorController = prefabAnimator.runtimeAnimatorController;
                Debug.Log("[TownCenterBehavior] Animator controller updated");
            }
            else if (currentAnimator != null)
            {
                // prefab mới không có animation nhưng current có → xóa animator cũ
                Debug.Log("[TownCenterBehavior] New prefab doesn't have animation, removing current Animator");
                Destroy(currentAnimator);
            }
            
            // cập nhật tier và data
            status.tier = data.tier;
            status.upgradeCost = data.upgradeCost;
            curentPrefab = data.curentPrefab;
            
            // cập nhật CoinUser với cost mới VÀ reset state để nó có thể nhận coin tiếp
            if (coinUser != null)
            {
                coinUser.requiredCoins = status.upgradeCost;
                coinUser.SetBuildingInfo(BuildingType.TownCenter, status.tier);
                
                // QUAN TRỌNG: Reset state để CoinUser có thể hoạt động trở lại
                coinUser.ResetUpgradeState();
                Debug.Log("[TownCenterBehavior] CoinUser reset để có thể nhận coin tiếp");
            }
            
            // thông báo BuildingManager về việc upgrade
            if (BuildingManager.instance != null)
            {
                BuildingManager.instance.UpdateAvailableBuildings();
            }
            
            Debug.Log($"[TownCenterBehavior] Upgraded to tier {status.tier}");
        }

        public int GetUpgradeCost() => status.upgradeCost;
        public void GetPlazaDistance() => plazaDistance = PrefabData.Instance != null ? PrefabData.Instance.plazaDistance : 30f;
        
        public void AddWorkingProcess(int amount)
        {
            status.buildProgress = Mathf.Min(status.buildProgress + amount, status.buildProgressMax);
            if (status.buildProgress >= status.buildProgressMax) OnUpgradeComplete();
        }

        [System.Serializable]
        public class TownCenterData
        {
            public float positionX;
            public float positionY;
            public bool readyToUpgrade;
            public int buildProgress;
            public int buildProgressMax;
        }
        public string GetPrefabID() => $"town_center_{status.tier}";
        public object CaptureData() => new TownCenterData
        {
            positionX = transform.position.x,
            positionY = transform.position.y,
            readyToUpgrade = status.readyToUpgrade,
            buildProgress = status.buildProgress,
            buildProgressMax = status.buildProgressMax
        };
        public void RestoreData(object data)
        {
            var d = (TownCenterData)data;
            transform.position = new Vector3(d.positionX, d.positionY, 0);
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
        }
    }
}



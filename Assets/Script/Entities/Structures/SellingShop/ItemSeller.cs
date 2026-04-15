using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Game.Item;
using Script.Game.Item.Coin;
using Script.Game.Other;
using UnityEngine;
using System.Linq;
using Script.Manager.Building;
using Script.Interfaces;

namespace Script.Game.Structures.SellingShop
{
    public class ItemSeller : MonoBehaviour, ISaveable, ICoinUserOwner
    {
        public enum ItemType
        {
            BowVendor, HammerVendor, ScytheVendor, SwordVendor, SpikeVendor
        }
        
        public GameObject itemPrefab; // Prefab của vật phẩm định bán
        public List<Transform> itemSlot; // Danh sách vị trí trên kệ
        public List<GameObject> currentItems = new List<GameObject>(); // Danh sách vật phẩm hiện tại
        public int maxSellableItems;
        public ItemType itemType; // Loại vật phẩm đang bán
        public bool isFull = false; // Kệ hàng có đầy không
        private CoinUser coinUser;
        
        // New fields for JSON configuration
        [JsonIgnore]
        private static List<ItemSellerConfig> itemSellerConfigs;
      
        // Classes for JSON deserialization
        [Serializable]
        private class ItemSellerConfig
        {
            public string id;
            public string itemType;
            public string prefabId;
            public int defaultPrice;
            public int maxItems;
            public string itemPrefabId;
        }
        
        [Serializable]
        private class ItemSellerConfigWrapper
        {
            public List<ItemSellerConfig> itemSellers;
        }

        void Start()
        {
            InitializeComponents();
            InitializeData();
        }
        
        private void InitializeComponents()
        {
            coinUser = gameObject.GetComponent<CoinUser>();
            
            // Set building type information for the CoinUser
            BuildingType buildingType;
            switch (itemType)
            {
                case ItemType.BowVendor:
                    buildingType = BuildingType.BowVendor;
                    break;
                case ItemType.HammerVendor:
                    buildingType = BuildingType.HammerVendor;
                    break;
                // Add other cases as needed
                default:
                    // Default to a generic type
                    Debug.LogWarning($"No matching BuildingType for {itemType}, using default");
                    buildingType = BuildingType.HammerVendor;
                    break;
            }
            
            // Set building type with tier 1 (vendors typically don't have tiers)
            coinUser.SetBuildingInfo(buildingType, 1);
        }

        private void InitializeData()
        {
            LoadItemSellerConfigs();
            GetItemValueFromPrefabDataUseItemTypeName();
            CheckInventory();
            maxSellableItems = itemSlot.Count();
        }
        
        private static void LoadItemSellerConfigs()
        {
            // Only load configs once
            if (itemSellerConfigs != null && itemSellerConfigs.Count > 0)
                return;
                
            try
            {
                // Load from Resources/DataConfig/Item/ItemSellerConfig.json
                TextAsset jsonTextAsset = Resources.Load<TextAsset>("DataConfig/Item/ItemSellerConfig");
                if (jsonTextAsset != null)
                {
                    var configWrapper = JsonConvert.DeserializeObject<ItemSellerConfigWrapper>(jsonTextAsset.text);
                    if (configWrapper?.itemSellers != null)
                    {
                        itemSellerConfigs = configWrapper.itemSellers;
                        Debug.Log($"Loaded {itemSellerConfigs.Count} item seller configurations from JSON");
                    }
                }
                else
                {
                    Debug.LogError("ItemSellerConfig.json not found in Resources/DataConfig");
                    itemSellerConfigs = new List<ItemSellerConfig>();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading item seller configs: {e.Message}");
                itemSellerConfigs = new List<ItemSellerConfig>();
            }
        }

        private void GetItemValueFromPrefabDataUseItemTypeName()
        {
            if (!PrefabData.Instance.productPrices.ContainsKey(itemType.ToString()))
            {
                Debug.LogError($"Giá của {itemType} không tồn tại trong productPrices!");
            }
        }
        
        // --- ICoinUserOwner Implementation ---
        public int GetRequiredCoinCount()
        {
            if (PrefabData.Instance.productPrices.TryGetValue(itemType.ToString(), out var price))
            {
                return price;
            }
            return 999; // Fallback an toàn
        }

        public bool CanReceiveCoins()
        {
            return !isFull;
        }

        public void OnAllCoinsCollected()
        {
            AddItem();
            // Báo CoinUser reset để chuẩn bị item tiếp theo (nếu kệ chưa đầy)
            if (coinUser != null)
            {
                coinUser.ResetUpgradeState();
            }
        }
        // -------------------------------------
        
        public void AddItem()
        {
            if (isFull)
            {
                Debug.Log("Kệ hàng đã đầy, không thể thêm vật phẩm mới.");
                return;
            }

            Transform emptySlot = FindEmptySlot();
            if (emptySlot != null)
            {
                // Dùng Spawner để spawn tool để đảm bảo enemyPickupAble được set đúng
                // ignoreOffset = true để tool nằm im trên kệ không bị offset cộng thêm
                GameObject newItem = Spawner.Instance.DropItem(itemPrefab, emptySlot.position, playerPickupAble: false, denizensPickupAble: true, ignoreOffset: true);
                
                if (newItem == null)
                {
                    Debug.LogError("[ItemSeller] DropItem trả về null!");
                    return;
                }

                // Đặt item vào slot và vô hiệu hóa trọng lực
                newItem.transform.SetParent(emptySlot);
                PhysicalItem physItem = newItem.GetComponent<PhysicalItem>();
                if (physItem != null)
                {
                    physItem.floating = true;
                    physItem.onSellerStock = true; // Đánh dấu đang trong stock để tránh save 2 lần
                    // Spawner đã set enemyPickupAble = true rồi
                }
                
                currentItems.Add(newItem);

                // Gắn thêm SellerItemNotifier và gán seller cho nó
                SellerItemNotifier notifier = newItem.GetComponent<SellerItemNotifier>();
                if (notifier == null)
                {
                    notifier = newItem.AddComponent<SellerItemNotifier>();
                }
                notifier.seller = this;

                CheckInventory();
            }
        }

        private Transform FindEmptySlot()
        {
            foreach (Transform slot in itemSlot)
            {
                bool occupied = false;
                foreach (GameObject item in currentItems)
                {
                    // Nếu item đã bị destroy, nó sẽ bằng null
                    if (item == null || item.transform.parent == slot)
                    {
                        occupied = true;
                        break;
                    }
                }
                if (!occupied) return slot;
            }
            return null;
        }

        public void CheckInventory()
        {
            // Loại bỏ các item bị null (đã bị destroy) một cách an toàn cho Inspector
            for (int i = currentItems.Count - 1; i >= 0; i--)
            {
                if (currentItems[i] == null)
                {
                    currentItems.RemoveAt(i);
                }
            }

            isFull = currentItems.Count >= itemSlot.Count;
            
            // Cập nhật lại UI hiển thị coin (nếu player đang đứng trong trigger)
            if (coinUser != null)
            {
                coinUser.RefreshSlotVisibility();
            }
        }

        // Phương thức để loại bỏ item khi nó bị destroy
        public void RemoveItem(GameObject item)
        {
            if (currentItems.Contains(item))
            {
                currentItems.Remove(item);
                CheckInventory();
            }
        }
        
        #region ISaveable Interface Implementation
        [Serializable]
        public class ItemSellerData
        {
            public float positionX;
            public float positionY;
            public string itemType;
            public int currentItemCount;
        }
        
        public string GetPrefabID()
        {
            // Change format to match the JSON config structure with underscores
            string id = itemType.ToString().ToLower().Replace("vendor", "_vendor");
            Debug.Log($"Generated prefabWall ID for {itemType}: {id}");
            return id;
        }
        
        public object CaptureData()
        {
            // Clean up the current items list before saving
            currentItems.RemoveAll(item => item == null);
            
            return new ItemSellerData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                itemType = itemType.ToString(),
                currentItemCount = currentItems.Count
            };
        }
        
        public void RestoreData(object data)
        {
            ItemSellerData sellerData = (ItemSellerData)data;
            transform.position = new Vector3(sellerData.positionX, sellerData.positionY, 0);
            
            // Parse the itemType from string
            if (Enum.TryParse(sellerData.itemType, out ItemType loadedItemType))
            {
                itemType = loadedItemType;
            }
            
            // Initialize components and data
            InitializeComponents();
            // KHÔNG gọi InitializeData() ở đây vì nó sẽ gọi CheckInventory() và có thể gây vấn đề
            // maxSellableItems = itemSlot.Count(); // Sẽ được set sau
            
            // Restore the items bằng cách Instantiate trực tiếp (không dùng Spawner để tránh physics)
            for (int i = 0; i < sellerData.currentItemCount; i++)
            {
                Transform emptySlot = FindEmptySlot();
                if (emptySlot != null)
                {
                    GameObject newItem = Instantiate(itemPrefab, emptySlot.position, Quaternion.identity);
                    newItem.transform.SetParent(emptySlot);
                    
                    // Set properties cho item
                    PhysicalItem physItem = newItem.GetComponent<PhysicalItem>();
                    if (physItem != null)
                    {
                        physItem.floating = true;
                        physItem.enemyPickupAble = true; // Cho phép enemy nhặt tool
                        physItem.onSellerStock = true; // Đánh dấu đang trong stock để tránh save 2 lần
                    }
                    
                    currentItems.Add(newItem);

                    // Gắn SellerItemNotifier
                    SellerItemNotifier notifier = newItem.GetComponent<SellerItemNotifier>();
                    if (notifier == null)
                    {
                        notifier = newItem.AddComponent<SellerItemNotifier>();
                    }
                    notifier.seller = this;
                }
            }
            
            // Sau khi restore xong, mới gọi CheckInventory() để cập nhật state
            maxSellableItems = itemSlot.Count();
            CheckInventory();
        }
        #endregion
    }
}

using System;
using System.Collections.Generic;
using Script.Data_Model;
using Script.Game.Item;
using Script.Game.Item.Coin;
using UnityEngine;
using System.Linq;
namespace Script.Game.Structures.SellingShop
{
    public class ItemSeller : MonoBehaviour
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
        public bool canAcceptCoins = true; // Kiểm soát việc nhận xu

        void Start()
        {
            InitializeComponents();
            InitializeData();
        }

        private void InitializeComponents()
        {
            coinUser = gameObject.GetComponent<CoinUser>();
            coinUser.onUpgradeComplete += OnUpgradeComplete;
        }

        private void InitializeData()
        {
            GetItemValueFromPrefabDataUseItemTypeName();
            CheckInventory();
            maxSellableItems = itemSlot.Count();
        }

        private void GetItemValueFromPrefabDataUseItemTypeName()
        {
            if (PrefabData.Instance.productPrices.TryGetValue(itemType.ToString(), out var price))
            {
                coinUser.requiredCoins = price;
            }
            else
            {
                Debug.LogError($"Giá của {itemType} không tồn tại trong productPrices!");
            }
        }
        private void OnUpgradeComplete()
        {
            AddItem();
            // Reset CoinUser state after adding item
            coinUser.ResetUpgradeState();
        }
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
                GameObject newItem = Instantiate(itemPrefab, emptySlot.position, Quaternion.identity);
                newItem.transform.SetParent(emptySlot);
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

        private void CheckInventory()
        {
            // Loại bỏ các item bị null (đã bị destroy)
            currentItems.RemoveAll(item => item == null);

            isFull = currentItems.Count >= itemSlot.Count;
            canAcceptCoins = !isFull; // Nếu kệ đầy, không nhận xu
            coinUser.canAcceptCoins = canAcceptCoins; // Cập nhật CoinUser
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
    }
}

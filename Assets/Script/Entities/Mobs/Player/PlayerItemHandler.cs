using Script.Game.Item.Coin;
using Script.Interfaces;
using Script.Data_Model.Interfaces;
using UnityEngine;
using Script.Utils;

namespace Script.Game.Player
{
    /// <summary>
    /// Component chuyên xử lý việc nhặt và xử lý vật phẩm (Item) của Player.
    /// Tách rời logic HandleItem khỏi PlayerBehavior.
    /// </summary>
    [RequireComponent(typeof(PlayerBehavior))]
    public class PlayerItemHandler : MonoBehaviour, IItemHandle
    {
        private PlayerBehavior playerBehavior;

        private void Awake()
        {
            playerBehavior = GetComponent<PlayerBehavior>();
        }

        public bool HandleItem(GameObject item)
        {
            // 1. Kiểm tra nhanh loại item bằng tag/name trước khi GetComponent để tối ưu
            if (item.CompareTag(GameTags.Coin))
            {
                // Nếu túi đầy thì không nhặt, trả về false ngay lập tức mà không cần GetComponent PhysicalItem
                if (playerBehavior.Coin >= playerBehavior.MaxCoins) return false;

                var physicalItem = item.GetComponent<PhysicalItem>();
                if (physicalItem != null && physicalItem.itemType == ItemType.Coin)
                {
                    playerBehavior.AddCoin(1);
                    return true;
                }
            }
            else // Các loại tool khác
            {
                var physicalItem = item.GetComponent<PhysicalItem>();
                if (physicalItem != null)
                {
                    // [TODO] Xử lý nhặt Tool ở đây
                }
            }
            
            return false;
        }
    }
}

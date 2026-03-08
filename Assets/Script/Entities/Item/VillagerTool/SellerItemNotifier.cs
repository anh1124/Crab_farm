using UnityEngine;
using Script.Game.Structures.SellingShop;

namespace Script.Game.Item
{
    public class SellerItemNotifier : MonoBehaviour
    {
        
        //script này được gán lên các item còn trên trệ hàng,
        //tức là nếu nó bị phá thì sẽ gọi notice đến script của cửa hàng bán nó
        public ItemSeller seller; //script đã bán item này

        private bool isRemoved = false; // Để đảm bảo chỉ thông báo một lần

        private void OnDestroy()
        {
            // Khi item bị destroy (ví dụ: bị village lấy), thông báo cho seller loại bỏ nó khỏi danh sách
            if (!isRemoved && seller != null)
            {
                seller.CheckInventory();
                seller.RemoveItem(gameObject);

            }
        }

        // Phương thức này sẽ được gọi khi enemy lấy item
        public void OnPickedUp()
        {
            if (!isRemoved && seller != null)
            {
                seller.RemoveItem(gameObject);
                isRemoved = true;
            }
        }
    }
}
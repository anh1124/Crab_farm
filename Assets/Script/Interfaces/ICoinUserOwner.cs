using UnityEngine;

namespace Script.Interfaces
{
    /// <summary>
    /// Implement interface này trên bất kỳ MonoBehaviour nào muốn sử dụng CoinUser.
    /// CoinUser sẽ tự tìm component này qua GetComponent<ICoinUserOwner>().
    /// Behavior KHÔNG cần biết đến CoinUser — chỉ cần implement interface này
    /// để khai báo số coin cần thiết và hành động xảy ra khi nạp đủ coin.
    /// </summary>
    public interface ICoinUserOwner
    {
        /// <summary>
        /// Khai báo số coin player cần nạp. 
        /// CoinUser sẽ KIỂM TRA LẠI giá trị này mỗi khi player tới gần và bật coin slot lên.
        /// </summary>
        int GetRequiredCoinCount();

        /// <summary>
        /// CoinUser có được phép nhận coin không?
        /// (ví dụ: kệ chưa đầy, wall chưa max tier, cây chưa bị chặt)
        /// </summary>
        bool CanReceiveCoins();

        /// <summary>
        /// *** HÀM ĐƯỢC GỌI KHI PLAYER NẠP ĐỦ COIN ***
        /// CoinUser gọi hàm này sau khi tất cả coin đã bay đến đích.
        /// Behavior tự xử lý logic của mình trong hàm này (chặt cây, nâng cấp, thêm item...).
        /// </summary>
        void OnAllCoinsCollected();
    }
}

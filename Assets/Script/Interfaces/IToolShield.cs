namespace Script.Interfaces
{
    // Interface dùng cho các class chiến đấu (Archer, Pikeman, Swordman...)
    // Có khả năng dùng chính vũ khí (Tool) làm 1 lần chặn sát thương thay vì rớt đồng coin cuối cùng.
    public interface IToolShield
    {
        // Kiểm tra xem thực thể có đủ điều kiện để dùng Tool làm khiên không (ví dụ: chỉ còn 1 coin cuối)
        bool HasToolShield { get; }
        
        // Trả về true nếu đã dùng tool để chặn sát thương (và rớt tool ra ngoài)
        bool ConsumeToolShield();
    }
}

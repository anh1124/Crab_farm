using UnityEngine;
using Script.Game.Mobs.Subjects;

public class DenizenAnimationEvents : MonoBehaviour
{
    // 1. Archer Events
    // ---------------------------------------------------------
    
    // Hàm này sẽ xuất hiện trong danh sách Event của Animation
    public void OnDrawComplete()
    {
        // Thư lấy ArcherBehavior trên cùng GameObject
        var archer = GetComponent<ArcherBehavior>();
        if (archer != null && archer.enabled)
        {
            archer.HandleDrawComplete();
        }
    }

    public void OnArrowRelease()
    {
        var archer = GetComponent<ArcherBehavior>();
        if (archer != null && archer.enabled)
        {
            archer.HandleArrowRelease();
        }
    }

    // 2. Các Job khác (Sau này mở rộng)
    // ---------------------------------------------------------
    // public void OnHammerHit() { ... gọi BuilderBehavior ... }
    // public void OnFarmWork() { ... gọi FarmerBehavior ... }
}

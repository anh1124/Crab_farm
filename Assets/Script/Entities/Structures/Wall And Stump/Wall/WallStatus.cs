using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Data_Model.Status
{
    // Chỉ chứa data và state của Wall, không có logic behavior
    
    public class WallStatus : MonoBehaviour
    {
        [Header("Basic Info")]
        public int tier;
        public WallState state;
        
        [Header("Health System")]
        public int maxHp;//máu tối đa hiện tại
        public int currentHp;//máu hiện tại
        //2 biến dưới dùng để làm cột mộc khi reset maxhp
        public int normalMaxHp;//máu tối đa chưa ban phước
        public int blessedMaxHp;//máu tối đa đã ban phước
        
        [Header("OnUpgradeComplete System")]
        public int upgradeCost;//số tiền cần cho level tiếp theo
        public bool readyToUpgrade;//wallmanager sẽ yêu cầu villagermanager đem thợ xây đến để dập khi đủ tiền
        public bool isMaxTier;//cấp tối đa,không hiện coin slot
        
        public int upgradeProgress;//tiến độ xây dựng hiện tại
        public int upgradeProgressMax;// cần đạt đến đây để nâng cấp
        
        [Header("Building Settings")]
        public float minimumDistance = 20f;//khoảng cách xây tối thiểu
        public int rebuildCost;//giá xây lại nếu bị phá
    }
    public enum WallState
    {
        Wall,
        Stump
    }

} 
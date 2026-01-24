using UnityEngine;
using Script.Data_Model.Interfaces;
using Script.Game.Mobs.Subjects;

namespace Script.Data_Model.Status
{
    [System.Serializable]
    public class ArcherStatus : MonoBehaviour, INPCStatus
    {
        public enum ArcherState
        {
            Idle,//[state chung chuyển],archer sẽ kiểm tra biến isGuard để xác định xem là hunter hay guard,sau dó chuyển state tiếp theo
            Hunting,//state này chạy thay idle khi isguard false
            Patrol,//state này chạy thay idle khi isguard true
            HoldingWall,//ban đêm hoặc có báo động,đứng canh ở tường được chỉ định,không tự di chuyển nếu không có lệnh từ manager
            AttackingFromWall,//có địch trong tầm nhìn ,thực hiện tấn công
            RetreatingToInnerWall,//khi bức tường đang phòng thủ xuống mức hp báo động, chạy nhanh về wall chỉ định
            ReturningToBase//quay trở lại wall khu vực plaza- khẩn cấp 
        }

        // Basic Settings
        [SerializeField] private float walkSpeed = 1.0f;
        [SerializeField] private float runSpeed = 2.5f;
        [SerializeField] private float currentSpeed;
        [SerializeField] private ArcherState currentState;

        // Properties
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public float CurrentSpeed
        {
            get => currentSpeed;
            set => currentSpeed = value;
        }
        public ArcherState CurrentState
        {
            get => currentState;
            set => currentState = value;
        }

        // Coin System (for INPCStatus)
        [SerializeField] private int currentCoins = 0;

        // INPCStatus implementation
        public NPCRole Role => NPCRole.Archer;
        public int CurrentCoins
        {
            get => currentCoins;
            set => currentCoins = value;
        }

        // Save Data Structure
        [System.Serializable]
        public class ArcherData
        {
            public float positionX;
            public float positionY;
            public float scaleX;
            public int stateValue;
        }

        public ArcherData CaptureData(Transform transform)
        {
            return new ArcherData
            {
                positionX = transform.position.x,
                positionY = transform.position.y,
                scaleX = transform.localScale.x,
                stateValue = (int)currentState
            };
        }

        public void RestoreData(ArcherData data, Transform transform)
        {
            transform.position = new Vector3(data.positionX, data.positionY, 0);
            transform.localScale = new Vector3(data.scaleX, transform.localScale.y, transform.localScale.z);
            currentState = (ArcherState)data.stateValue;
        }
    }
} 
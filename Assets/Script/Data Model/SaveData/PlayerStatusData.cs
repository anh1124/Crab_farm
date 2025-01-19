using System;
using Script.Data_Model.Status;

namespace Script.Data_Model.SaveData
{
    [Serializable]
    public class PlayerStatusData
    {
        public int maxHp;
        public int currentHp;
        public int coin;
        public float moveSpeed;
        public float positionX;
        public float positionY;
        public int attackDame;
        public float attackSpeed;

        // Thêm constructor không tham số
        public PlayerStatusData()
        {
        }

        // Constructor hiện tại
        public PlayerStatusData(PlayerStatus status)
        {
            maxHp = status.MaxHp;
            currentHp = status.CurrentHp;
            coin = status.Coin;
            moveSpeed = status.moveSpeed;
            positionX = status.transform.position.x;
            positionY = status.transform.position.y;
            attackDame = status.AttackDame;
            attackSpeed = status.AttackSpeed;
        }
    }
}

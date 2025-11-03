using Script.Data_Model.Interfaces;
using Script.Game.Enemy.Interfaces;
using Script.Interfaces;
using UnityEngine;

namespace Script.Data_Model.Status
{
    // Data component cho Player (MonoBehaviour, khởi tạo qua AddComponent)
    public class PlayerStatus: MonoBehaviour
    {
        public int maxHp;
        public int currentHp;
        public int coin;
        public float moveSpeed;
        public float positionX;
        public float positionY;
        public int attackDame;
        public float attackSpeed;
        public bool isGround;
        
        // Thiết lập mặc định khi thêm component trong Editor (không ghi đè runtime)
        private void Reset()
        {
            maxHp = 100;
            currentHp = 100;
            coin = 0;
            moveSpeed = 7f;
            positionX = 0f;
            positionY = 0f;
            attackDame = 1;
            attackSpeed = 1f;
            isGround = false;
        }
    }
}

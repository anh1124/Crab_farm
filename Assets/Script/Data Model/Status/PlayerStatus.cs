using Script.Data_Model.Interfaces;
using UnityEngine;

namespace Script.Data_Model.Status
{
    public class PlayerStatus : BaseStatus,IAttackable,ISaveable
    {
        //base status contain other element like hp,...
        //this script only show other element that don't use on basétatus
        
        public int AttackDame { get; set; }              // Attack power
        public float AttackSpeed { get; set; }      // Attack speed
    
        private void Start()
        {
            moveSpeed = PrefabData.Instance.playerSpeed;
        }
        public void Attack()
        {
        }
        public override bool TakeDamage(int damage, GameObject whoDoDame)
        {
            if (HasCoinShield && CurrentCoin > 0)
            {
                CurrentCoin -= 1;
                if (CurrentCoin < 0) CurrentCoin = 0;
                DropCoin(whoDoDame);
                return true;
            }
            if (HasHp)
            {
                CurrentHp -= damage;
                if (CurrentHp <= 0)
                {
                    Die(whoDoDame);
                    return false;
                }
                return true;
            }
            return true;
        }
        public override void Die(GameObject whoDoDame)
        {
            // Logic chết của player (ẩn player, gọi hiệu ứng, v.v.)
            Debug.Log($"{gameObject.name} đã chết bởi {whoDoDame?.name ?? "unknown"}");
            gameObject.SetActive(false);
        }
        
        // ISaveable
        public string GetPrefabID()
        {
            return "player"; 
        }

        public object CaptureData()
        {
            return new PlayerStatusData(this);
        }

        public void RestoreData(object data)
        {
            PlayerStatusData playerData = (PlayerStatusData)data;
            MaxHp = playerData.maxHp;
            CurrentHp = playerData.currentHp;
            Coin = playerData.coin;
            moveSpeed = playerData.moveSpeed;
            AttackDame = playerData.attackDame;
            AttackSpeed = playerData.attackSpeed;
            transform.position = new Vector3(playerData.positionX, playerData.positionY, 0);
        }
        
        [System.Serializable]
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
  
}

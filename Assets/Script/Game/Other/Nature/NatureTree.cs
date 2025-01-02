using System;
using Script.Data_Model;
using Script.Game.Item.Coin;
using UnityEngine;
namespace Script.Game.Other.Nature
{   
    public class NatureTree: MonoBehaviour,IDropReward
    {   
        /*
         * để ý:
         * có thể cần 1 hàm để load tree từ save
         */
        public class TreeStatus
        {
            public float positionX;
            public int Type;
            public int TreeChopCost;
            public int TreeHp;
            public bool IsReadyToChop;

            public TreeStatus(int type, int treeChopCost, int treeHp, bool isReadyToChop, float positionX, float positionY)
            {
                Type = type;
                TreeChopCost = treeChopCost;
                TreeHp = treeHp;
                IsReadyToChop = isReadyToChop;
                this.positionX = positionX;
            }
        }
       
        public int Type;//loại cây,nhớ thêm biến này trong editto
        public int treeChopCost;
        public int treeHp;

        public bool isReadyToChop;
        [SerializeField] private CoinUser coinUser;
        private void Start()
        {
            coinUser = gameObject.GetComponent<CoinUser>();

            treeChopCost = PrefabData.Instance.treeChopCost;
            treeHp = PrefabData.Instance.treeHp;
            UpdateCoinUserData();
        }
        private void UpdateCoinUserData()
        {
            coinUser.requiredCoins = treeChopCost;
            coinUser.onUpgradeComplete = SetChopFlag;
        }
        
        public void SetChopFlag()
        {
            isReadyToChop = true;
            //đặt tạm hàm take dame ở đây để test xóa cây
            Debug.LogWarning("đang test xóa cây sau khi thêm tiền ở hàm SetChopFlag");
            TakeDame(11);
        }

        public void TakeDame(int dame)
        {   
            Debug.Log("take dame "+ dame);
            if (dame <= 0)
            {
                return;
            }
            if (dame >= treeHp)
            {
                DropReward(gameObject.transform.position, Spawner.Instance.GetRandomNumber(3), Vector2.zero);
                //do animation treefall
                Destroy(gameObject);
            }
            else
            {
                treeHp -= dame;
            }
        }
        public void DropReward(Vector2 position, int amount, Vector2 direction)
        {
            Spawner.Instance.DropByNature(position,amount,direction);
        }
        


    }
}
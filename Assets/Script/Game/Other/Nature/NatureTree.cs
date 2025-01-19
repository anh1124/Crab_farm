using System;
using Script.Data_Model;
using Script.Data_Model.Status;
using Script.Game.Item.Coin;
using UnityEngine;
namespace Script.Game.Other.Nature
{   
    public class NatureTree: MonoBehaviour,IDropable
    {
        public TreeStatus status;
        [SerializeField] private CoinUser coinUser;

        public int treeChopCost;//biến này ko cần lưu  
        private bool isSetupCalled = false;//
        
        private void Start()
        {
            status = gameObject.GetComponent<TreeStatus>();
            coinUser = gameObject.GetComponent<CoinUser>();

            treeChopCost = PrefabData.Instance.treeChopCost;
            status.maxWorkProgress = PrefabData.Instance.TreeMaxWorkProgress;
            UpdateCoinUserData();
            
            //nếu hàm SetWorkProgress chưa được gọi thì tức là đây là cây mới(chưa có trong save nên phải set full hp cho nó)
            if (!isSetupCalled)
            {
                status.currentWorkProgress = PrefabData.Instance.TreeMaxWorkProgress;
            }
        }
        public void SetWorkProgress(int currentWorkProgress)
        {
            isSetupCalled = true;
            status.currentWorkProgress = currentWorkProgress;
        }
        private void UpdateCoinUserData()
        {
            coinUser.requiredCoins = treeChopCost;
            coinUser.onUpgradeComplete = SetChopFlag;
        }
        
        public void SetChopFlag()
        {
            status.isReadyToChop = true;
            //đặt tạm hàm take dame ở đây để test xóa cây
            Debug.LogWarning("đang test xóa cây sau khi thêm tiền ở hàm SetChopFlag");
            TakeDame(11);
        }

        public void TakeDame(int dame)
        {   
            Debug.Log("NatureTree:take dame "+ dame);
            if (dame <= 0)
            {
                return;
            }
            if (dame >= status.currentWorkProgress)
            {
                Drop(gameObject,
                    PrefabData.Instance.coinPrefab, 
                    Spawner.Instance.GetRandomNumber(3),
                    Vector2.zero);
                
                //do animation treefall
                Destroy(gameObject);
            }
            else
            {
                status.currentWorkProgress -= dame;
            }
        }
        
        public void Drop(GameObject whoDrop, GameObject dropWhat, int amount, Vector2 direction)
        {
            // Gọi phương thức DropByNature từ Spawner để spawn coin
            Spawner.Instance.DropByNature(transform.position, amount, direction);
        }
        


    }
}
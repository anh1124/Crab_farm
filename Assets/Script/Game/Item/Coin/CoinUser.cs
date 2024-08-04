using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model;
using UnityEngine;

namespace Script.Game.Item.Coin
{
    public class CoinUser : MonoBehaviour
    {
    
        public Action actionHaveEnoughCoin;//chạy trong UpgradeStructure
    
        private BoxCollider2D _boxCollider2D;
        public int coinQuantity;
        public int currentCoinCount = 0;
    
        private GameObject _coinPrefab;
    
        public float timeCurrentFloatCoin;
        private float _timeMaxFloatCoin;
    
        private GameObject _coinSlotLocation;
    
        private float _coinMoveSpeed;
        public bool readyToUpgrade;

        private bool _isCounting = false;
        private List<GameObject> _coinSlotList = new List<GameObject>(); //danh sách lưu vị trí các coinSLot
        private int _currentSlotIndex = 0; // bộ đếm coin đang được chuyển đến vị trí 
        private List<GameObject> coinsInTransit = new List<GameObject>(); // Lưu danh sách các coin đang di chuyển
        private List<Coroutine> moveCoinCoroutines = new List<Coroutine>(); // Lưu danh sách các Coroutine của hàm chuyển 

        private void Start()
        {
            _coinPrefab = PrefabInit.Instance.coinPrefab;
            _coinMoveSpeed = PrefabInit.Instance.coinMoveSpeed;
            _timeMaxFloatCoin = PrefabInit.Instance.timeMaxFloatCoin;
            
            _coinSlotLocation = GameObject.Find("CoinSlotLocation");
            _coinSlotLocation.SetActive(false);

        
        }
        public int GetChildCount(GameObject parent)//kiểm tra Con
        {
            if (parent == null)
            {
                Debug.LogWarning("Parent GameObject is null.");
                return 0;
            }
        
            // Get the Transform component of the parent GameObject
            Transform parentTransform = parent.transform;
        
            // Return the number of child transforms
            return parentTransform.childCount;
        }
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                //Debug.Log("Player triggered this collider!");
                if (!readyToUpgrade)
                {
                    //kiểm tra xem có đủ coinslot không,nếu không đủ thì tạo thêm
                    _coinSlotLocation.SetActive(true);
                    int currentSlot = GetChildCount(_coinSlotLocation);
                    if (currentSlot < coinQuantity)
                    {
                        DestroyAllChildren(_coinSlotLocation);
                        //hàm bên dưới vừa tạo 1 coinslot vừa truyền thao chiếu vào list bên 
                        _coinSlotList = InstanceCoinSlot.Instance.CreateCoinSlot(coinQuantity, _coinSlotLocation);
                    }
                
                }
        
            }
        }
    
        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                //Debug.Log("Player leave this collider!");
                _coinSlotLocation.SetActive(false);
           
                ResetCoinStatusInTransit();
                _currentSlotIndex = 0;
                //coinSlotList.Clear();
            }
        }
        // Phương thức để xóa tất cả các đối tượng con
        private void DestroyAllChildren(GameObject parent)
        {
            foreach (Transform child in parent.transform)
            {
                Destroy(child.gameObject);
            }
            _currentSlotIndex = 0;
        }

        public void InsertCoin(Status playerInsertCoinStatus, GameObject playerGameObject)//hàm nhét tiền
        {
            if (playerInsertCoinStatus.coin <= 0 && coinQuantity > currentCoinCount)//nếu đang chuyển tiền mà hết tiền
            {
                ResetCoinStatusInTransit();
                return;
            }
            TransferCoin(playerGameObject);
            _isCounting = true;//bật bộ đếm
            timeCurrentFloatCoin = _timeMaxFloatCoin;//đặt lại bộ đếm thời gian 
       
      
        }
    
        //hàm di chuyển coin đến vị trí slot
        public void TransferCoin(GameObject player)
        {
            if (_coinSlotList.Count == 0) // Check if there are no slots available
            {
                Debug.LogWarning("No coin slots available.");
                return;
            }

        
            Vector3 playerPosition = player.transform.position;
            Vector3 targetPosition = _coinSlotList[_currentSlotIndex].transform.position;

            GameObject coin = Instantiate(_coinPrefab, playerPosition, Quaternion.identity);
        
            //chuyển các coin vừa được instance ra thành floatting 
            Coin coinScriptInstance = coin.GetComponent<Coin>();
            if (coinScriptInstance != null)//chuyển trạng thái coin thành bay
            {
                coinScriptInstance.playerPickupAble = false;
                coinScriptInstance.denizensPickupAble = false;
                coinScriptInstance.floating = true;
            }
        
            coinsInTransit.Add(coin);//thêm coin đó vào danh sách chờ 
     
            Coroutine coroutine = StartCoroutine(MoveCoinToSlot(coin, targetPosition));  //chạy coin từ player vào slot
            moveCoinCoroutines.Add(coroutine); // Lưu Coroutine vào danh sách
        
            _currentSlotIndex = (_currentSlotIndex + 1) % _coinSlotList.Count; // di chuyển bộ đếm slot coin
        }
        private IEnumerator MoveCoinToSlot(GameObject coin, Vector3 targetPosition)//hàm di chuyển hình ảnh của coin
        {
        
            float elapsedTime = 0.0f;
            Vector3 startingPosition = coin.transform.position;

            while (elapsedTime < _coinMoveSpeed)
            {
                coin.transform.position = Vector3.Lerp(startingPosition, targetPosition, elapsedTime / _coinMoveSpeed);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            coin.transform.position = targetPosition;
            currentCoinCount++;//tổng coin hiện tại
            CheckUpgrade();
        }

        public void CheckUpgrade()
        {
            if (currentCoinCount == coinQuantity)//nếu đủ tiền thi nâng cấp và hủy khả năng thêm coin
            {
                UpgradeStructure();
                _isCounting = false;//dừng đếm trong update
                _currentSlotIndex = 0;
            }
        }
    
        private void Update()
        {
            if (_isCounting)
            {
                CountdownCoin();
            }
        
        }

        public void CountdownCoin()
        {

            timeCurrentFloatCoin -= Time.deltaTime;
            if (timeCurrentFloatCoin <= 0f)
            {
                ResetCoinStatusInTransit();
                _isCounting = false;
                _currentSlotIndex = 0;
            }
        }



        public void UpgradeStructure()
        {
            readyToUpgrade = true;
            Debug.Log("cong trinh da san sang nang cap");
            actionHaveEnoughCoin?.Invoke();
        
            DeleteCoinsInTransitList();
        
            _coinSlotLocation.SetActive(false);
            _currentSlotIndex = 0;
        }
    
  



        //chuyển các coin về lại trạng thái rơi
        //và xóa danh sách coin đang chơ 
        public void ResetCoinStatusInTransit()
        {
            foreach (GameObject coin in coinsInTransit)
            {
                Coin coinScriptInstance = coin.GetComponent<Coin>();
                if (coinScriptInstance != null)
                {
                    coinScriptInstance.floating = false;
                    coinScriptInstance.playerPickupAble = true;
                    coinScriptInstance.denizensPickupAble = true;
                }
            }
            StopAllMoveCoinCoroutines();
            coinsInTransit.Clear();
            currentCoinCount = 0;
            _currentSlotIndex = 0;
        }
        // Hàm để dừng tất cả các Coroutine trong danh sách moveCoinCoroutines
        private void StopAllMoveCoinCoroutines()
        {
        
            foreach (Coroutine coroutine in moveCoinCoroutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            moveCoinCoroutines.Clear();
        }
    
        // Hàm để hủy tất cả các GameObject trong danh sách coinsInTransit
        private void DeleteCoinsInTransitList()
        {
            StopAllMoveCoinCoroutines(); 
            foreach (GameObject coin in coinsInTransit)
            {
                if (coin != null)
                {
                    Destroy(coin);
                }
            }
            coinsInTransit.Clear();
            currentCoinCount = 0;
            _currentSlotIndex = 0;
        }

    }
}

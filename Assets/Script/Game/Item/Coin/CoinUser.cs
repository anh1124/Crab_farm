using System;
using System.Collections;
using System.Collections.Generic;
using Script.Game.Item.Coin;
using Unity.VisualScripting;
using UnityEngine;

public class CoinUser : MonoBehaviour
{
    
    public Action actionHaveEnoughCoin;//chạy trong UpgradeStructure
    
    public BoxCollider2D boxCollider2D;
    public int coinQuantity;
    public int currentCoinCount = 0;
    public GameObject coinPrefab;
    
    public float timeCurrentFloatCoin;
    public float timeMaxFloatCoin;
    
    public GameObject coinSlotLocation;
    
    public float coinMoveSpeed;
    public bool readyToUpgrade;

    public bool isCounting = false;
    public List<GameObject> coinSlotList = new List<GameObject>(); //danh sách lưu vị trí các coinSLot
    public int currentSlotIndex = 0; // bộ đếm coin đang được chuyển đến vị trí 
    public List<GameObject> coinsInTransit = new List<GameObject>(); // Lưu danh sách các coin đang di chuyển
    private List<Coroutine> moveCoinCoroutines = new List<Coroutine>(); // Lưu danh sách các Coroutine của hàm chuyển 

    private void Start()
    {

        coinSlotLocation = GameObject.Find("CoinSlotLocation");
        coinSlotLocation.SetActive(false);
    
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
                coinSlotLocation.SetActive(true);
                int currentSlot = GetChildCount(coinSlotLocation);
                if (currentSlot < coinQuantity)
                {
                    DestroyAllChildren(coinSlotLocation);
                    //hàm bên dưới vừa tạo 1 coinslot vừa truyền thao chiếu vào list bên 
                    coinSlotList = InstanceCoinSlot.Instance.CreateCoinSlot(coinQuantity, coinSlotLocation);
                }
                
            }
        
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            //Debug.Log("Player leave this collider!");
            coinSlotLocation.SetActive(false);
           
            ResetCoinStatusInTransit();
            currentSlotIndex = 0;
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
        currentSlotIndex = 0;
    }

    public void InsertCoin(Status playerInsertCoinStatus, GameObject playerGameObject)//hàm nhét tiền
    {
        if (playerInsertCoinStatus.coin <= 0 && coinQuantity > currentCoinCount)//nếu đang chuyển tiền mà hết tiền
        {
            ResetCoinStatusInTransit();
            return;
        }
        TransferCoin(playerGameObject);
        isCounting = true;//bật bộ đếm
        timeCurrentFloatCoin = timeMaxFloatCoin;//đặt lại bộ đếm thời gian 
       
      
    }
    
    //hàm di chuyển coin đến vị trí slot
    public void TransferCoin(GameObject player)
    {
        if (coinSlotList.Count == 0) // Check if there are no slots available
        {
            Debug.LogWarning("No coin slots available.");
            return;
        }

        
        Vector3 playerPosition = player.transform.position;
        Vector3 targetPosition = coinSlotList[currentSlotIndex].transform.position;

        GameObject coin = Instantiate(coinPrefab, playerPosition, Quaternion.identity);
        
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
        
        currentSlotIndex = (currentSlotIndex + 1) % coinSlotList.Count; // di chuyển bộ đếm slot coin
    }
    private IEnumerator MoveCoinToSlot(GameObject coin, Vector3 targetPosition)//hàm di chuyển hình ảnh của coin
    {
        
        float elapsedTime = 0.0f;
        Vector3 startingPosition = coin.transform.position;

        while (elapsedTime < coinMoveSpeed)
        {
            coin.transform.position = Vector3.Lerp(startingPosition, targetPosition, elapsedTime / coinMoveSpeed);
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
            isCounting = false;//dừng đếm trong update
            currentSlotIndex = 0;
        }
    }
    
    private void Update()
    {
        if (isCounting)
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
            isCounting = false;
            currentSlotIndex = 0;
        }
    }



    public void UpgradeStructure()
    {
        readyToUpgrade = true;
        Debug.Log("cong trinh da san sang nang cap");
        actionHaveEnoughCoin?.Invoke();
        
        DeleteCoinsInTransitList();
        
        coinSlotLocation.SetActive(false);
        currentSlotIndex = 0;
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
        currentSlotIndex = 0;
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
        currentSlotIndex = 0;
    }

}

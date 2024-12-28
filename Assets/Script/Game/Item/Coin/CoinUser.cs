using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model;
using UnityEngine;

namespace Script.Game.Item.Coin
{
    public class CoinUser : MonoBehaviour
    {
        /// <summary>
        /// khi player va chạm với collider của gameobject chứa coinUser thì se kiểm tra :
        /// -có đang va chạm với player không?
        /// -script CoinUser.isReadyToUpgrade có false không thì mới hiện các coin slot
        /// </summary>
        [Header("Upgrade Settings")]
        public int requiredCoins; // tổng số coin cần để thực hiện hành động upgrade
        public Action onUpgradeComplete; // cái này sẽ được gọi khi nhét đủ coin

        [Header("Coin Collection")]
        public int collectedCoins ; // số coin hiện tại đã lấy được
        public float coinCollectionTimeout ; // Time before coin collection resets
        public float coinCollectionTimer; //  biến đếm ngược
        [SerializeField] private GameObject coinSlotParent; // Parent object for coin slots

        [Header("Upgrade Status")]
        public bool isReadyToUpgrade; // Indicates if enough coins have been collected for upgrade

        [Header("Coin Prefabs")]
        [SerializeField] private GameObject coinPrefab;

        [SerializeField] private List<GameObject> coinSlots = new List<GameObject>(); // List of coin slot positions
        [SerializeField] private List<GameObject> coinsInTransit = new List<GameObject>(); // List of coins being moved
         private List<Coroutine> coinMoveCoroutines = new List<Coroutine>();

        private float coinMoveSpeed;
        private int currentSlotIndex ;
        private bool isCollectingCoins ;

        public bool playerTrigger;
        private void Start()
        {
            InitializeCoinUser();
        }

        private void InitializeCoinUser()
        {
            coinPrefab = PrefabData.Instance.coinPrefab;
            coinMoveSpeed = PrefabData.Instance.coinMoveSpeed;
            coinCollectionTimeout = PrefabData.Instance.timeMaxFloatCoin;

            if (coinSlotParent == null)
            {
                coinSlotParent = transform.GetChild(0).gameObject;
            }
            coinSlotParent.SetActive(false);
        }

        public void PlayerTriggerOn()
        {
            SetupCoinSlots();
            playerTrigger = true;
        }

        public void PlayerTriggerOff()
        {
            ClearCoinSlots();
            playerTrigger = false;
        }
        /*private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player") && !isReadyToUpgrade)
            {
                SetupCoinSlots();
                playerTrigger = true;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                //ResetCoinCollection();
                ClearCoinSlots();
                playerTrigger = false;
            }
        }*/

        private void SetupCoinSlots()
        {
            coinSlotParent.SetActive(true);
            int currentSlotCount = coinSlotParent.transform.childCount;
            if (currentSlotCount < requiredCoins)
            {
                ClearCoinSlots();
                coinSlots = InstanceCoinSlot.Instance.CreateCoinSlot(requiredCoins, coinSlotParent);
            }
        }

        private void ClearCoinSlots()
        {
            foreach (Transform child in coinSlotParent.transform)
            {
                Destroy(child.gameObject);
            }
            currentSlotIndex = 0;
        }

        public void InsertCoin(int availableCoins, GameObject player)
        {
            if (availableCoins <= 0 || isReadyToUpgrade)
            {
                return;
            }
            StartCoinTransfer(player);
            isCollectingCoins = true;
            coinCollectionTimer = coinCollectionTimeout; 
        }
        private void Update()
        {
            // Kiểm tra và giảm dần giá trị đếm ngược
            if (isCollectingCoins && !isReadyToUpgrade)
            {
                coinCollectionTimer -= Time.deltaTime;
                if (coinCollectionTimer <= 0)
                {
                    ReleaseCoins();
                }
            }
        }
        private void StartCoinTransfer(GameObject player)
        {
            if (coinSlots.Count == 0 || currentSlotIndex >= coinSlots.Count)
            {
                Debug.LogWarning("No available coin slots.");
                return;
            }

            Vector3 playerPosition = player.transform.position;//chỗ này cho y cao hơn 1 tí 
            Vector3 targetPosition = coinSlots[currentSlotIndex].transform.position;

            GameObject coin = Instantiate(coinPrefab, playerPosition, Quaternion.identity);
            SetCoinProperties(coin);
            
            coinsInTransit.Add(coin);
            Coroutine coroutine = StartCoroutine(MoveCoinToSlot(coin, targetPosition));
            coinMoveCoroutines.Add(coroutine);

            currentSlotIndex++;
        }

        private void SetCoinProperties(GameObject coin)
        {
            Coin coinScript = coin.GetComponent<Coin>();
            if (coinScript != null)
            {
                coinScript.playerPickupAble = false;
                coinScript.denizensPickupAble = false;
                coinScript.floating = true;
            }
        }

        private IEnumerator MoveCoinToSlot(GameObject coin, Vector3 targetPosition)
        {
            Vector3 startPosition = coin.transform.position;
            float elapsedTime = 0f;

            while (elapsedTime < coinMoveSpeed)
            {
                coin.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / coinMoveSpeed);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            coin.transform.position = targetPosition;
            collectedCoins++;
            CheckUpgradeStatus();
        }

        private void CheckUpgradeStatus()
        {
            if (collectedCoins >= requiredCoins)
            {
                PerformUpgrade();
            }
        }

     
        private void ReleaseCoins()
        {   
            Debug.LogWarning("đã gọi hàm ReleaseCoins");
            foreach (GameObject coin in coinsInTransit)
            {
                if (coin != null)
                {
                    
                    ReleaseCoin(coin);
                }
            }
            StopAllCoinCoroutines();
            ClearTransitCoins();
            isCollectingCoins = false;
            currentSlotIndex = 0;
        }
        
        private void ReleaseCoin(GameObject coin)
        {
            Coin coinScript = coin.GetComponent<Coin>();
            if (coinScript != null)
            {
                coinScript.floating = false;
                coinScript.playerPickupAble = true;
                coinScript.denizensPickupAble = true;
            }
        }
        
        private void StopAllCoinCoroutines()
        {
            foreach (Coroutine coroutine in coinMoveCoroutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            coinMoveCoroutines.Clear();
        }

        private void ClearTransitCoins()
        {
            coinsInTransit.Clear();
            collectedCoins = 0;
        }

        private void PerformUpgrade()
        {
            if (collectedCoins >= requiredCoins)
            {
                foreach (GameObject coin in coinsInTransit)
                {
                    if (coin != null)
                    {
                        Destroy(coin);
                    }
                }
                ClearTransitCoins();
                coinSlotParent.SetActive(false);
                currentSlotIndex = 0;
                isReadyToUpgrade = true;
                onUpgradeComplete?.Invoke();
            }
        }
    }
}

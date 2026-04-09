using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Script.Game.Player;
using Script.Data_Model;

namespace Script.Game.Item.Coin
{
    public class CoinSlotVisual : MonoBehaviour
    {
        [Header("Coin Prefabs")]
        [SerializeField] private GameObject coinPrefab;
        [SerializeField] private GameObject coinSlotParent; // Parent object for coin slots
        
        [Header("Runtime Visuals")]
        [SerializeField] private List<GameObject> coinSlots = new List<GameObject>();
        [SerializeField] private List<GameObject> coinsInTransit = new List<GameObject>();
        private List<Coroutine> coinMoveCoroutines = new List<Coroutine>();
        private HashSet<GameObject> arrivedCoins = new HashSet<GameObject>();
        
        private ICoinPool _coinPool;

        public void SetupPool(ICoinPool pool)
        {
            _coinPool = pool;
        }
        private float coinMoveSpeed;
        private int currentSlotIndex;

        public int CurrentSlotIndex => currentSlotIndex;
        public int TransitCount => coinsInTransit.Count;

        private void Start()
        {
            coinPrefab = PrefabData.Instance.coinPrefab;
            coinMoveSpeed = PrefabData.Instance.coinMoveSpeed;

            if (coinSlotParent == null)
            {
                var child = transform.Find("CoinSlotLocation");
                if (child != null)
                {
                    coinSlotParent = child.gameObject;
                }
                else
                {
                    var newObj = new GameObject("CoinSlotLocation");
                    newObj.transform.SetParent(transform, false);
                    coinSlotParent = newObj;
                }
            }
            coinSlotParent.SetActive(false);
        }

        public void SetupCoinSlots(int requiredCoins)
        {
            if (coinSlotParent != null)
            {
                coinSlotParent.SetActive(true);
                
                int currentSlotCount = coinSlotParent.transform.childCount;
                //nếu coinslot đã được tạo đủ số lượng thì không tạo lại
                if (currentSlotCount == requiredCoins && requiredCoins > 0)
                {
                    if (coinSlots.Count != currentSlotCount)
                    {
                        coinSlots.Clear();
                        foreach (Transform child in coinSlotParent.transform)
                        {
                            coinSlots.Add(child.gameObject);
                        }
                    }
                }
                else
                {
                    ClearCoinSlots();
                    if (InstanceCoinSlot.Instance != null)
                    {
                         coinSlots = InstanceCoinSlot.Instance.CreateCoinSlot(requiredCoins, coinSlotParent);
                    }
                }
            }
        }

        public void HideSlots()
        {
            if (coinSlotParent != null)
            {
                coinSlotParent.SetActive(false);
            }
        }

        public void ClearCoinSlots()
        {
            foreach (Transform child in coinSlotParent.transform)
            {
                Destroy(child.gameObject);
            }
            coinSlots.Clear();
            currentSlotIndex = 0;
        }

        public void ClearTransitVisuals()
        {
            foreach (GameObject coin in coinsInTransit)
            {
                if (coin != null)
                {
                    if (_coinPool != null)
                    {
                        _coinPool.ReturnCoin(coin);
                    }
                    else
                    {
                        Destroy(coin);
                    }
                }
            }
            coinsInTransit.Clear();
            arrivedCoins.Clear();
            currentSlotIndex = 0;
        }

        public void ReleaseFloatingCoins()
        {
            foreach (GameObject transitCoin in coinsInTransit)
            {
                if (transitCoin != null)
                {
                    PhysicalItem coinScript = transitCoin.GetComponent<PhysicalItem>();
                    if (coinScript != null)
                    {
                        coinScript.floating = false;
                        coinScript.playerPickupAble = true;
                        coinScript.denizensPickupAble = true;
                    }
                }
            }
            coinsInTransit.Clear();
            arrivedCoins.Clear();
            currentSlotIndex = 0;
        }

        public void StopAllCoinCoroutines()
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

        public bool HasAvailableSlot(int requiredCoins)
        {
            return currentSlotIndex < requiredCoins && coinSlots.Count > 0 && currentSlotIndex < coinSlots.Count;
        }

        public void SpawnAndMoveCoin(GameObject player, System.Action<GameObject> onCoinArrived)
        {
            if (!HasAvailableSlot(999)) return;

            if (coinSlots[currentSlotIndex] == null) return;

            Vector3 playerPosition = player.transform.position;
            Vector3 targetPosition = coinSlots[currentSlotIndex].transform.position;

            GameObject coin = _coinPool != null ? _coinPool.GetCoin() : Instantiate(coinPrefab);
            if (coin != null) coin.transform.position = playerPosition;
            
            PhysicalItem coinScript = coin.GetComponent<PhysicalItem>();
            if (coinScript != null)
            {
                coinScript.playerPickupAble = false;
                coinScript.denizensPickupAble = false;
                coinScript.floating = true;
            }
            
            coinsInTransit.Add(coin);
            
            Coroutine coroutine = StartCoroutine(MoveCoinToSlot(coin, targetPosition, currentSlotIndex, player, onCoinArrived));
            coinMoveCoroutines.Add(coroutine);

            currentSlotIndex++;
        }

        private IEnumerator MoveCoinToSlot(GameObject coin, Vector3 targetPosition, int slotIndex, GameObject player, System.Action<GameObject> onCoinArrived)
        {
            Vector3 startPosition = coin.transform.position;
            float elapsedTime = 0f;

            // Truy tìm orchestrator để lấy isReadyToUpgrade flag nếu muốn break giữa chừng.
            // Nhưng thiết kế tốt hơn là orchestrator sẽ gọi StopAllCoinCoroutines() từ ngoài.
            while (elapsedTime < coinMoveSpeed)
            {
                if (coin == null) yield break;
                coin.transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / coinMoveSpeed);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            if (coin == null) yield break;

            coin.transform.position = targetPosition;
            arrivedCoins.Add(coin);
            
            // Bắn event báo đã đến đích
            onCoinArrived?.Invoke(coin);
        }
    }
}

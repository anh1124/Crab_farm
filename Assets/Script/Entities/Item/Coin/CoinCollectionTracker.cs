using UnityEngine;
using Script.Data_Model;
using System;

namespace Script.Game.Item.Coin
{
    public class CoinCollectionTracker : MonoBehaviour
    {
        [Header("Collection Status")]
        [SerializeField] private int collectedCoins;
        [SerializeField] private float coinCollectionTimer;
        private float coinCollectionTimeout;
        private bool isCollectingCoins;

        public int CollectedCoins => collectedCoins;
        public bool IsCollectingCoins => isCollectingCoins;

        public event Action OnTimeout;
        public event Action<int> OnCollectionProgress; // Gửi update mỗi khi nhận 1 coin

        private void Start()
        {
            coinCollectionTimeout = PrefabData.Instance.timeMaxFloatCoin;
        }

        private void Update()
        {
            if (isCollectingCoins)
            {
                coinCollectionTimer -= Time.deltaTime;
                if (coinCollectionTimer <= 0)
                {
                    HandleTimeout();
                }
            }
        }

        public void StartCollection()
        {
            isCollectingCoins = true;
            coinCollectionTimer = coinCollectionTimeout;
        }

        public void TrackCoinArrival()
        {
            collectedCoins++;
            OnCollectionProgress?.Invoke(collectedCoins);
        }

        public void ResetCollection()
        {
            isCollectingCoins = false;
            collectedCoins = 0;
            coinCollectionTimer = 0;
        }

        private void HandleTimeout()
        {
            ResetCollection();
            OnTimeout?.Invoke(); // Bắn event để Orchestrator gọi VisualScript nhả coin ra
        }
    }
}

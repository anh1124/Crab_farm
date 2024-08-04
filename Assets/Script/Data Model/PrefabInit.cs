using UnityEngine;

namespace Script.Data_Model
{
    public class PrefabInit : MonoBehaviour
    {
        public static PrefabInit Instance { get; private set; }
        
        private void Awake()
        {
            // Implementing the singleton pattern
            if (Instance == null)
            {
                Instance = this;
                // DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Coin
        public GameObject coinPrefab;
        
        public float coinMoveSpeed = 0.5f;

        public float timeMaxFloatCoin = 3f;

        #endregion

    }
}

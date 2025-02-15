using System;
using Script.Data_Model;
using Script.Data_Model.Interfaces;
using Script.Game.Enemy.Interfaces;
using Script.Game.Item.Coin;
using UnityEngine;
using UnityEngine.Serialization;

namespace Script.Game.Structures.Wall_And_Stump
{
    public class Wall : MonoBehaviour,IDamageable,IMinimumDistance
    { 
        /*
         * chú ý, stump ở layer defautl
         *         wall ở layer wall
         *          layer wall
         */
        // Properties từ IDamageable
        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }
        
        public bool getData;
        [SerializeField] private int tier;
        
        [SerializeField] private int normalMaxHp;
        [SerializeField] private int blessedMaxHp;
        
        [SerializeField] private GameObject curentPrefab;
        [SerializeField] private GameObject stumpPrefab;
        [SerializeField] private GameObject nextUpgradePrefab;
        
        [SerializeField] private int rebuildCost;
        [SerializeField] private int upgradeCost;
        
        [SerializeField] private bool readyToUpgrade;
        [SerializeField] private bool maxLevel;
  
        [SerializeField] private CoinUser coinUser;
        [SerializeField] private float minimumDistance = 20f;
    
        public float MinimumDistance => minimumDistance;
        private void Start()
        {
            coinUser = GetComponent<CoinUser>();
            if (coinUser == null)
            {
                Debug.LogError($"CoinUser component missing on {gameObject.name}");
                return;
            }
            
            InitializeWallData();
            UpdateCoinUserData();
        }
        private void InitializeWallData()
        {
            //Debug.LogWarning("InitializeWallData called for tier: " + tier);
            WallDataList.WallData wallData = WallDataList.Instance.GetWallDataByTier(tier);
            
            if (wallData != null)
            {
                
                upgradeCost = wallData.upgradeCost;
                rebuildCost = wallData.rebuildCost;
                normalMaxHp = wallData.normalMaxHp;
                blessedMaxHp = wallData.blessedMaxHp;
                
                curentPrefab = wallData.curentPrefab;
                stumpPrefab = wallData.stumpPrefab;
                nextUpgradePrefab = wallData.nextUpgradePrefab;
                maxLevel = wallData.maxLevel;
                UpdateMaxHp();
                CurrentHp = MaxHp;

                //UpdateCoinUserData();
            }
            else
            {
                Debug.LogWarning($"No WallData found for tier: {tier}");
            }
        }
        private void UpdateMaxHp()
        {
            MaxHp = PrefabData.Instance.techAdhesive ? blessedMaxHp : normalMaxHp;
        }

        private void UpdateCoinUserData()
        {
            coinUser.requiredCoins = upgradeCost;
            coinUser.onUpgradeComplete = DoUpgrade;
        }

        public void TakeDamage(int damageAmount, GameObject whoDoDame)
        {
            CurrentHp -= damageAmount;
            if (CurrentHp <= 0)
            {
                Die(whoDoDame);
            }
        }

        public void Repair()
        {
            UpdateMaxHp();
            CurrentHp = MaxHp;
        }

        public void Die(GameObject WhoDoDame)
        {
            GameObject stump =Instantiate(stumpPrefab, transform.position, Quaternion.identity);
            Stump stumpScriptInstance = stump.GetComponent<Stump>();
            stumpScriptInstance.InitializeStumpData(tier);
            Destroy(gameObject);
        }

        private void DoUpgrade()
        {
            if (nextUpgradePrefab != null)
            {
                // Get the current parent of this object
                Transform parentTransform = transform.parent;
        
                // Instantiate the new upgrade prefab as a child of the same parent
                GameObject newUpgrade = Instantiate(nextUpgradePrefab, transform.position, Quaternion.identity, parentTransform);
        
                // Destroy the current object
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning(nextUpgradePrefab + "is null in" + curentPrefab.name);
            }
        }
    }
}

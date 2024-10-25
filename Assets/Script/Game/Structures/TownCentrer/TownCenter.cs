using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model;
using Script.Game.Item.Coin;
using Script.Game.Structures.TownCentrer;
using UnityEngine;

public class TownCenter : MonoBehaviour
{
    /*
     * tag Structure
     * layer Defautl
     */
    #region Initdata
    
    
    public int tier;
    public int upgradeCost;
    public GameObject curentPrefab;
    public GameObject nextUpgradePrefab;
   
    //lvl-1 campfire
    public bool bowVendor;
    public bool hammerVendor;
            
    //lvl-2 Village tent
    public bool hoeVendor;
            
    //lvl-3 Townhall
    public bool coinStock;
    public bool pykeVendor;
            
    //lvl-4 Big TownHall
    public bool shieldVendor;
    public bool siegeWorkshop;
            
    //lvl-5>???
    public int centralWallLowerTier;
    public int centralTowerLowerTier;
    
    public bool readyToUpgrade;
    public bool maxLevel;

    public float plazaDistance;
    public  CoinUser coinUser;
    
    #endregion
    private void Start()
    {
        coinUser = GetComponent<CoinUser>();
        if (coinUser == null)
        {
            Debug.LogError($"CoinUser component missing on {gameObject.name}");
            return;
        }

        plazaDistance = PrefabData.Instance.plazaDistance;
        InitializeWallData();
        UpdateCoinUserData();
    }
    private void InitializeWallData()
    {
        //Debug.LogWarning("InitializeWallData called for tier: " + tier);
        TownCenterDataList.TownCentrerData townCentrerData = TownCenterDataList.Instance.GetWallDataByTier(tier);
            
        if (townCentrerData != null)
        {
                
            upgradeCost = townCentrerData.upgradeCost;
            curentPrefab = townCentrerData.curentPrefab;
            nextUpgradePrefab = townCentrerData.nextUpgradePrefab;
            maxLevel = townCentrerData.maxLevel;
            //1
            bowVendor = townCentrerData.bowVendor;
            hammerVendor = townCentrerData.hammerVendor;
            //2
            hoeVendor = townCentrerData.hoeVendor;
            //3
            coinStock = townCentrerData.coinStock;
            pykeVendor = townCentrerData.pykeVendor;
            //4
            shieldVendor = townCentrerData.shieldVendor;
            siegeWorkshop = townCentrerData.siegeWorkshop;
            //5??
            
            
            centralWallLowerTier = townCentrerData.centralWallLowerTier;
            centralTowerLowerTier = townCentrerData.centralTowerLowerTier;
        }
        else
        {
            Debug.LogWarning($"No townCentrerData found for tier: {tier}");
        }
    }
    private void UpdateCoinUserData()
    {
        coinUser.requiredCoins = upgradeCost;
        coinUser.onUpgradeComplete = DoUpgrade;
    }
    private void DoUpgrade()
    {
        if (nextUpgradePrefab != null)
        {
            Instantiate(nextUpgradePrefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning(nextUpgradePrefab + "is null in" + curentPrefab.name);
        }
    }

    private void Update()
    {
        plazaDistance = PrefabData.Instance.plazaDistance;
        Debug.DrawRay(transform.position, transform.forward * plazaDistance, Color.blue);
    }
}

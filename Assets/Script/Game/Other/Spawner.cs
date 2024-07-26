using System.Collections;
using System.Collections.Generic;
using Script.Game.Item.Coin;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    /*
     * hàm chung cho việc spawn mob ở bất cứ đâu
     */
    public static Spawner Instance { get; private set; }
    public float coinThrowForce = 10f;
    // Reference to the Coin prefab
    public GameObject coinPrefab;
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

    public void SpawnCoin(Vector2 position)
    {
        if (coinPrefab != null)
        {
            Instantiate(coinPrefab, position, Quaternion.identity);
        }
        else
        {
            Debug.LogError("Coin prefab is not assigned in the Spawner script.");
        }
    }
    
    public void DropByPlayer(Vector2 position)
    {
        GameObject coin = Instantiate(coinPrefab, position, Quaternion.identity);
        Coin coinScriptInstance = coin.GetComponent<Coin>();
        
        if (coinScriptInstance != null)
        {
            // Set initial properties of the coin
            coinScriptInstance.playerPickupAble = false;
            coinScriptInstance.denizensPickupAble = true;
            coinScriptInstance.floating = false;
            
            
            coinScriptInstance.CallFunctionCoolDown("Player");
        }
    }
    
    public void DropByDenizen(Vector2 position ,Vector2 direction)
    {
        GameObject coin = Instantiate(coinPrefab, position, Quaternion.identity);
        Coin coinScriptInstance = coin.GetComponent<Coin>();
        if (coinScriptInstance != null)//chuyển trạng thái coin thành bay
        {
            coinScriptInstance.playerPickupAble = true;
            coinScriptInstance.denizensPickupAble = false;
            coinScriptInstance.floating = false;
            coinScriptInstance.CallFunctionCoolDown("Denizen");
        }
    }
    #endregion
    
}

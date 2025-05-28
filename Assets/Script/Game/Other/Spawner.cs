using System;
using System.Collections;
using System.Collections.Generic;
using Script;
using Script.Data_Model;
using Script.Game.Item.Coin;
using UnityEngine;
using Random = UnityEngine.Random;

public class Spawner : MonoBehaviour
{
    /*
     * hàm chung cho việc spawn mob ở bất cứ đâu
   
     */
    //thêm cho coin nảy 2 lần khi chạm đất
    
    public static Spawner Instance { get; private set; }
    public float coinThrowForce = 5f;
    public float coinThrowDistance = 2f;
    public GameObject coinPrefab;
    

    
    int yOffset = 2;
    private void Awake()
    {
        // Implementing the singleton pattern
        if (Instance == null)
        {
            Instance = this;
           
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        
        coinPrefab = PrefabData.Instance.coinPrefab;
   
    }


 
    #region Coin Spawn
    public int GetRandomNumber( int max)
    {
        return Random.Range(0, max); 
    }
     
    // Phương thức để drop coin khi cây bị đốn
    public void DropByNature(Vector2 position, int amount, Vector2 direction)
    {
        // Chờ đợi và spawn coin, gọi một coroutine
        StartCoroutine(DropByNatureCoroutine(position, direction, amount));
    }

    public void DropByPlayer(Vector2 position, int amount, Vector2 direction)
    {
        StartCoroutine(DropByPlayerCoroutine(position, direction, amount));
    }

    public void DropByDenizen(Vector2 position, int amount, Vector2 direction)
    {
        StartCoroutine(DropByDenizenCoroutine(position, direction, amount));
    }

    public void InitializeCoinThrow(GameObject coin, Vector2 direction, float randomVariation = 0.2f)
    {
        Coin coinScript = coin.GetComponent<Coin>();
        if (coinScript != null)
        {
            // Add some random variation to throw distance and force
            float randomizedForce = coinThrowForce * (1f + Random.Range(-randomVariation, randomVariation));
            float randomizedDistance = coinThrowDistance * (1f + Random.Range(-randomVariation, randomVariation));
            
            coinScript.InitializeThrow(direction, randomizedForce, randomizedDistance);
        }
    }

    private IEnumerator DropByNatureCoroutine(Vector2 position, Vector2 direction, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector2 newPosition = new Vector2(position.x, position.y + yOffset + (i * 0.1f));
            GameObject coin = Instantiate(coinPrefab, newPosition, Quaternion.identity, EntitySaveManager.Instance.coinContainer.transform);

            Coin coinScriptInstance = coin.GetComponent<Coin>();
            if (coinScriptInstance != null)
            {
                coinScriptInstance.playerPickupAble = true;
                coinScriptInstance.denizensPickupAble = true;
                coinScriptInstance.floating = false;
                InitializeCoinThrow(coin, direction);
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    private IEnumerator DropByPlayerCoroutine(Vector2 position, Vector2 direction, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector2 newPosition = new Vector2(position.x, position.y + yOffset);
            GameObject coin = Instantiate(coinPrefab, newPosition, Quaternion.identity, EntitySaveManager.Instance.coinContainer.transform);

            Coin coinScriptInstance = coin.GetComponent<Coin>();
            if (coinScriptInstance != null)
            {
                coinScriptInstance.playerPickupAble = false;
                coinScriptInstance.denizensPickupAble = true;
                coinScriptInstance.floating = false;
                coinScriptInstance.CallFunctionCoolDown("Player");
                InitializeCoinThrow(coin, direction);
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    private IEnumerator DropByDenizenCoroutine(Vector2 position, Vector2 direction, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector2 newPosition = new Vector2(position.x, position.y);
            GameObject coin = Instantiate(coinPrefab, newPosition, Quaternion.identity, EntitySaveManager.Instance.coinContainer.transform);

            Coin coinScriptInstance = coin.GetComponent<Coin>();
            if (coinScriptInstance != null)
            {
                coinScriptInstance.playerPickupAble = true;
                coinScriptInstance.denizensPickupAble = false;
                coinScriptInstance.floating = false;
                coinScriptInstance.CallFunctionCoolDown("Denizen");
                InitializeCoinThrow(coin, direction);
            }

            yield return new WaitForSeconds(0.3f);
        }
    }


    #endregion
   
    /// <summary>
    /// Spawns a mob at the specified position
    /// </summary>
    /// <param name="position">Spawn position</param>
    /// <param name="prefab">The prefab to spawn</param>
    /// <returns>The spawned GameObject or null if spawn failed</returns>
    public GameObject SpawnMob(Vector3 position, GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[Spawner] Cannot spawn mob - prefab is null!");
            return null;
        }

        // Spawn mob and move it to the correct container
        GameObject spawnedMob = Instantiate(prefab, position, Quaternion.identity);
        if (spawnedMob != null && EntitySaveManager.Instance != null)
        {
            EntitySaveManager.Instance.MoveObjectToContainer(spawnedMob);
        }
        else
        {
            Debug.LogWarning("[Spawner] EntitySaveManager not found, mob will not be saved!");
        }

        return spawnedMob;
    }
}

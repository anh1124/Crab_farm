using System;
using System.Collections;
using System.Collections.Generic;
using Script;
using Script.Data_Model;
using Script.Game.Item.Coin;
using Script.Manager;
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
        PhysicalItem coinScript = coin.GetComponent<PhysicalItem>();
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
            GameObject coin = CoinPool.Instance != null ? CoinPool.Instance.GetCoin() : Instantiate(coinPrefab, EntitySaveManager.Instance.coinContainer.transform);
            if (coin != null) coin.transform.position = newPosition;

            PhysicalItem coinScriptInstance = coin.GetComponent<PhysicalItem>();
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
            GameObject coin = CoinPool.Instance != null ? CoinPool.Instance.GetCoin() : Instantiate(coinPrefab, EntitySaveManager.Instance.coinContainer.transform);
            if (coin != null) coin.transform.position = newPosition;

            PhysicalItem coinScriptInstance = coin.GetComponent<PhysicalItem>();
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
            GameObject coin = CoinPool.Instance != null ? CoinPool.Instance.GetCoin() : Instantiate(coinPrefab, EntitySaveManager.Instance.coinContainer.transform);
            if (coin != null) coin.transform.position = newPosition;

            PhysicalItem coinScriptInstance = coin.GetComponent<PhysicalItem>();
            if (coinScriptInstance != null)
            {
                coinScriptInstance.playerPickupAble = true;
                coinScriptInstance.denizensPickupAble = false;
                coinScriptInstance.floating = false;
                coinScriptInstance.CallFunctionCoolDown("Denizens");
                InitializeCoinThrow(coin, direction);
            }

            yield return new WaitForSeconds(0.3f);
        }
    }


    #endregion

    #region Item Drop

    /// <summary>
    /// Drop 1 item prefab ra ngoài với throw animation giống coin.
    /// </summary>
    /// <param name="prefab">Prefab cần drop</param>
    /// <param name="position">Vị trí spawn (thường là transform.position của người drop)</param>
    /// <param name="playerPickupAble">Player có nhặt được không</param>
    /// <param name="denizensPickupAble">Denizen khác có nhặt được không</param>
    public GameObject DropItem(GameObject prefab, Vector2 position, bool playerPickupAble, bool denizensPickupAble = true)
    {
        if (prefab == null)
        {
            Debug.LogError("[Spawner.DropItem] prefab là NULL! Kiểm tra PrefabData đã gán đúng chưa.");
            return null;
        }
        if (EntitySaveManager.Instance == null)
        {
            Debug.LogError("[Spawner.DropItem] EntitySaveManager.Instance là NULL! Không thể drop item.");
            return null;
        }

        Vector2 dropDirection = UnityEngine.Random.insideUnitCircle.normalized;
        Vector2 spawnPos = new Vector2(position.x, position.y + 1.5f);

        Transform container = EntitySaveManager.Instance.otherContainer != null
            ? EntitySaveManager.Instance.otherContainer.transform
            : null;

        GameObject item = Instantiate(prefab, spawnPos, Quaternion.identity, container);

        PhysicalItem itemScript = item.GetComponent<PhysicalItem>();
        if (itemScript == null)
        {
            Debug.LogError($"[Spawner.DropItem] {item.name} không có component PhysicalItem! Throw sẽ không hoạt động.");
            return item;
        }

        itemScript.playerPickupAble = playerPickupAble;
        // Luôn bắt đầu với denizensPickupAble = false để tránh race condition:
        // Item vừa spawn đã bị LootPickupTrigger của chính người drop kích hoạt ngay lập tức.
        // CallFunctionCoolDown("Denizens") sẽ bật denizensPickupAble lại sau một khoảng delay.
        itemScript.denizensPickupAble = false;
        itemScript.floating = false;
        itemScript.InitializeThrow(dropDirection, coinThrowForce, coinThrowDistance);

        if (denizensPickupAble)
        {
            itemScript.CallFunctionCoolDown("Denizens");
        }

        return item;
    }

    #endregion
   
    /// <summary>
    /// Spawns a mob at the specified position
    /// </summary>
    /// <param name="position">Spawn position</param>
    /// <param name="prefab">The prefabWall to spawn</param>
    /// <returns>The spawned GameObject or null if spawn failed</returns>
    public GameObject SpawnMob(Vector3 position, GameObject prefab)
    {
        if (prefab == null)
        {
            Debug.LogError("[Spawner] Cannot spawn mob - prefabWall is null!");
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

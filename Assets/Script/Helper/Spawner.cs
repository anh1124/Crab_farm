using System;
using System.Collections;
using System.Collections.Generic;
using Script;
using Script.Data_Model;
using Script.Game.Enemy;
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
    [Header("Denizen Drop Config")]
    [Tooltip("Cooldown (giây) cho Denizen khác nhặt coin khi coin được drop do bị tấn công.")]
    public float denizenHitDropCoinCooldown = 10f;
    public GameObject coinPrefab;
    
    private ICoinPool _coinPool;
    private IEnemyPool _enemyPool;

    public void Setup(ICoinPool coinPool, IEnemyPool enemyPool = null)
    {
        _coinPool = coinPool;
        _enemyPool = enemyPool;
    }

    
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

    public void DropByDenizen(Vector2 position, int amount, Vector2 direction, float denizenPickupCooldown = -1f)
    {
        StartCoroutine(DropByDenizenCoroutine(position, direction, amount, denizenPickupCooldown));
    }

    public void DropCoinFromDenizenHit(Vector2 denizenPosition, int amount = 1)
    {
        Vector2 dropDirection = UnityEngine.Random.insideUnitCircle.normalized;
        if (dropDirection.sqrMagnitude < 0.01f) dropDirection = Vector2.right;
        Vector2 spawnPosition = new Vector2(denizenPosition.x, denizenPosition.y + 2f);
        DropByDenizen(spawnPosition, amount, dropDirection, denizenHitDropCoinCooldown);
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
            GameObject coin = _coinPool != null ? _coinPool.GetCoin() : Instantiate(coinPrefab, EntitySaveManager.Instance.coinContainer.transform);
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
            GameObject coin = _coinPool != null ? _coinPool.GetCoin() : Instantiate(coinPrefab, EntitySaveManager.Instance.coinContainer.transform);
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

    private IEnumerator DropByDenizenCoroutine(Vector2 position, Vector2 direction, int amount, float denizenPickupCooldown = -1f)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector2 newPosition = new Vector2(position.x, position.y);
            GameObject coin = _coinPool != null ? _coinPool.GetCoin() : Instantiate(coinPrefab, EntitySaveManager.Instance.coinContainer.transform);
            if (coin != null) coin.transform.position = newPosition;

            PhysicalItem coinScriptInstance = coin.GetComponent<PhysicalItem>();
            if (coinScriptInstance != null)
            {
                coinScriptInstance.playerPickupAble = true;
                coinScriptInstance.denizensPickupAble = false;
                coinScriptInstance.floating = false;
                if (denizenPickupCooldown >= 0f)
                    coinScriptInstance.CallFunctionCoolDown("Denizens", denizenPickupCooldown);
                else
                    coinScriptInstance.CallFunctionCoolDown("Denizens");
                InitializeCoinThrow(coin, direction);
            }

            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>
    /// Drop coin from TownCenter - returns coin reference for cooldown setting
    /// </summary>
    public GameObject DropByTownCenter(Vector2 position, Vector2 direction)
    {
        GameObject coin = _coinPool != null ? _coinPool.GetCoin() : Instantiate(coinPrefab, EntitySaveManager.Instance.coinContainer.transform);
        if (coin != null) coin.transform.position = position;

        PhysicalItem coinScriptInstance = coin.GetComponent<PhysicalItem>();
        if (coinScriptInstance != null)
        {
            coinScriptInstance.playerPickupAble = true;
            coinScriptInstance.denizensPickupAble = false;
            coinScriptInstance.floating = false;
            coinScriptInstance.CallFunctionCoolDown("Denizens");
            InitializeCoinThrow(coin, direction);
        }

        return coin;
    }

    #endregion

    #region Rabbit and Bush Spawn

    /// <summary>
    /// Spawn a rabbit at the given position, parented to animalsContainer
    /// </summary>
    public GameObject SpawnRabbit(Vector2 position)
    {
        GameObject rabbitPrefab = PrefabData.Instance.rabbit;
        if (rabbitPrefab == null)
        {
            Debug.LogError("[Spawner.SpawnRabbit] Rabbit prefab not found in PrefabData!");
            return null;
        }

        if (EntitySaveManager.Instance == null || EntitySaveManager.Instance.animalsContainer == null)
        {
            Debug.LogError("[Spawner.SpawnRabbit] EntitySaveManager.Instance or animalsContainer is null!");
            return null;
        }

        GameObject rabbitObj = Instantiate(rabbitPrefab, position, Quaternion.identity, EntitySaveManager.Instance.animalsContainer.transform);
        return rabbitObj;
    }

    /// <summary>
    /// Spawn a rabbit bush at the given position, parented to natureContainer
    /// </summary>
    public GameObject SpawnRabbitBush(Vector3 position)
    {
        GameObject bushPrefab = PrefabData.Instance.rabbitBush;
        if (bushPrefab == null)
        {
            Debug.LogError("[Spawner.SpawnRabbitBush] RabbitBush prefab not found in PrefabData!");
            return null;
        }

        if (EntitySaveManager.Instance == null || EntitySaveManager.Instance.natureContainer == null)
        {
            Debug.LogError("[Spawner.SpawnRabbitBush] EntitySaveManager.Instance or natureContainer is null!");
            return null;
        }

        GameObject bushObj = Instantiate(bushPrefab, position, Quaternion.identity, EntitySaveManager.Instance.natureContainer.transform);
        return bushObj;
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

    /// <summary>
    /// Spawn enemy cho luồng load save. Spawner tự tương tác với pool.
    /// </summary>
    public GameObject SpawnEnemyFromLoad(string enemyName, GameObject fallbackPrefab, Transform fallbackParent, bool debug = false)
    {
        if (string.IsNullOrEmpty(enemyName))
        {
            Debug.LogError("[Spawner] SpawnEnemyFromLoad failed: enemyName is null/empty.");
            return null;
        }

        if (_enemyPool == null)
            _enemyPool = FindObjectOfType<EnemyPool>();

        if (_enemyPool != null)
        {
            if (_enemyPool is EnemyPool concretePool)
                concretePool.InitializePools();

            var pooledEnemy = _enemyPool.GetEnemy(enemyName);
            if (pooledEnemy != null)
            {
                if (debug) Debug.Log($"[Spawner] SpawnEnemyFromLoad -> pooled enemy: {enemyName}");
                return pooledEnemy;
            }
        }

        if (fallbackPrefab == null)
        {
            Debug.LogError($"[Spawner] SpawnEnemyFromLoad failed: fallback prefab null for '{enemyName}'.");
            return null;
        }

        var instance = Instantiate(fallbackPrefab, fallbackParent);
        if (debug) Debug.Log($"[Spawner] SpawnEnemyFromLoad -> instantiated fallback enemy: {enemyName}");
        return instance;
    }
}

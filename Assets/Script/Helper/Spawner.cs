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
   
    
    public static Spawner Instance { get; private set; }
    public float coinThrowForce = 5f;
    public float coinThrowDistance = 2f;
    [Header("Denizen Drop Config")]
    [Tooltip("Cooldown (giây) cho Denizen khác nhặt coin khi coin được drop do bị tấn công.")]
    public float denizenHitDropCoinCooldown = 10f;
    private GameObject coinPrefab;
    
    private ICoinPool _coinPool;
    private IEnemyPool _enemyPool;

    public void Setup(ICoinPool coinPool, IEnemyPool enemyPool = null)
    {
        _coinPool = coinPool;
        _enemyPool = enemyPool;
    }

    public float defaultDropYOffset = 2f;
    public float itemDropYOffset = 1.5f;

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
        if(coinPrefab == null)
        {
            Debug.LogError("[Spawner]: Coin prefab is null");
        }
    }
   

 
    #region Coin Spawn
    
     
    // Phương thức để drop coin khi cây bị đốn
    public void DropByNature(Vector2 position, int amount, Vector2 direction)
    {
        StartCoroutine(DropCoinsInternalCoroutine(position, direction, amount, (script) => {
            script.playerPickupAble = true;
            script.denizensPickupAble = true;
            script.enemyPickupAble = true; // Cho phép enemy nhặt coin
            script.floating = false;
            script.droppedByAnimal = true; // Mark as loot for archers
        }, defaultDropYOffset, 0.1f));
    }

    public void DropByPlayer(Vector2 position, int amount, Vector2 direction)
    {
        StartCoroutine(DropCoinsInternalCoroutine(position, direction, amount, (script) => {
            script.playerPickupAble = false;
            script.denizensPickupAble = true;
            script.enemyPickupAble = true; // Cho phép enemy nhặt coin
            script.floating = false;
            script.CallFunctionCoolDown("Player");
        }, defaultDropYOffset, 0f));
    }

    public void DropByDenizen(Vector2 position, int amount, Vector2 direction, float denizenPickupCooldown = -1f)
    {
        StartCoroutine(DropCoinsInternalCoroutine(position, direction, amount, (script) => {
            script.playerPickupAble = true;
            script.denizensPickupAble = false;
            script.enemyPickupAble = true; // Cho phép enemy nhặt coin
            script.floating = false;
            if (denizenPickupCooldown >= 0f)
                script.CallFunctionCoolDown("Denizens", denizenPickupCooldown);
            else
                script.CallFunctionCoolDown("Denizens");
        }, defaultDropYOffset, 0f));
    }

    public void DropFromDenizenHit(Vector2 denizenPosition, int amount = 1)
    {
        Vector2 dropDirection = UnityEngine.Random.insideUnitCircle.normalized;
        if (dropDirection.sqrMagnitude < 0.01f) dropDirection = Vector2.right;
        DropByDenizen(denizenPosition, amount, dropDirection, denizenHitDropCoinCooldown);
    }

    public void ThrowCoin(GameObject coin, Vector2 direction, float randomVariation = 0.2f)
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

    private IEnumerator DropCoinsInternalCoroutine(Vector2 position, Vector2 direction, int amount, Action<PhysicalItem> configureAction, float startYOffset, float spreadY)
    {
        for (int i = 0; i < amount; i++)
        {
            Vector2 newPosition = new Vector2(position.x, position.y + startYOffset + (i * spreadY));
            
            Transform parentTransform = ContainerEntityManager.Instance != null && ContainerEntityManager.Instance.coinContainer != null ? ContainerEntityManager.Instance.coinContainer.transform : null;
            GameObject coin = _coinPool != null ? _coinPool.GetCoin() : Instantiate(coinPrefab, parentTransform);
            if (coin != null) coin.transform.position = newPosition;

            PhysicalItem coinScriptInstance = coin.GetComponent<PhysicalItem>();
            if (coinScriptInstance != null)
            {
                configureAction?.Invoke(coinScriptInstance);
                ThrowCoin(coin, direction);
            }

            // Play coin drop sound at coin position
            Script.Audio.AudioManager.Instance?.PlaySFXByKey("Coin_Drop", newPosition);

            yield return new WaitForSeconds(0.3f);
        }
    }

    /// <summary>
    /// Drop coin from TownCenter - returns coin reference for cooldown setting
    /// </summary>
    public GameObject DropByTownCenter(Vector2 position, Vector2 direction)
    {
        Transform parentTransform = ContainerEntityManager.Instance != null && ContainerEntityManager.Instance.coinContainer != null ? ContainerEntityManager.Instance.coinContainer.transform : null;
        GameObject coin = _coinPool != null ? _coinPool.GetCoin() : Instantiate(coinPrefab, parentTransform);
        if (coin != null) coin.transform.position = position;

        PhysicalItem coinScriptInstance = coin.GetComponent<PhysicalItem>();
        if (coinScriptInstance != null)
        {
            coinScriptInstance.playerPickupAble = true;
            coinScriptInstance.denizensPickupAble = false;
            coinScriptInstance.enemyPickupAble = true; // Cho phép enemy nhặt coin
            coinScriptInstance.floating = false;
            coinScriptInstance.CallFunctionCoolDown("Denizens");
            ThrowCoin(coin, direction);
        }

        // Play coin drop sound at coin position
        Script.Audio.AudioManager.Instance?.PlaySFXByKey("Coin_Drop", position);

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

        if (ContainerEntityManager.Instance == null || ContainerEntityManager.Instance.animalsContainer == null)
        {
            Debug.LogError("[Spawner.SpawnRabbit] ContainerEntityManager.Instance or animalsContainer is null!");
            return null;
        }

        GameObject rabbitObj = Instantiate(rabbitPrefab, position, Quaternion.identity, ContainerEntityManager.Instance.animalsContainer.transform);
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

        if (ContainerEntityManager.Instance == null || ContainerEntityManager.Instance.natureContainer == null)
        {
            Debug.LogError("[Spawner.SpawnRabbitBush] ContainerEntityManager.Instance or natureContainer is null!");
            return null;
        }

        GameObject bushObj = Instantiate(bushPrefab, position, Quaternion.identity, ContainerEntityManager.Instance.natureContainer.transform);
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
    /// <param name="ignoreOffset">Bỏ qua offset và throw animation (cho vendor)</param>
    public GameObject DropItem(GameObject prefab, Vector2 position, bool playerPickupAble, bool denizensPickupAble = true, bool ignoreOffset = false)
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
        Vector2 spawnPos = position;
        
        // Chỉ thêm offset nếu không ignoreOffset
        if (!ignoreOffset)
        {
            spawnPos = new Vector2(position.x, position.y + itemDropYOffset);
        }

        Transform container = ContainerEntityManager.Instance != null && ContainerEntityManager.Instance.otherContainer != null
            ? ContainerEntityManager.Instance.otherContainer.transform
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
        itemScript.enemyPickupAble = true; // Cho phép enemy nhặt tool
        itemScript.floating = false;
        
        // Chỉ throw nếu không ignore offset (vendor items nằm im trên kệ)
        if (!ignoreOffset)
        {
            itemScript.InitializeThrow(dropDirection, coinThrowForce, coinThrowDistance);
        }

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
        if (spawnedMob != null && ContainerEntityManager.Instance != null)
        {
            ContainerEntityManager.Instance.MoveToContainer(spawnedMob);
        }
        else
        {
            Debug.LogWarning("[Spawner] ContainerEntityManager not found, mob will not be parented correctly!");
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

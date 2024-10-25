using System;
using System.Collections;
using System.Collections.Generic;
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
    public float coinThrowForce = 10f;
   
    public GameObject coinPrefab;
    
    public GameObject StructureContainer;// spawm các gameobject thuộc lớp Structure ở đây
    public GameObject CoinContainer;//spawm coin trong này
    public GameObject NatureContainer;//spawm cây trong này
    
    int yOffset = 2;
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

    private void Start()
    {
        
        coinPrefab = PrefabData.Instance.coinPrefab;
        SetUpContainer();
    }


    private void SetUpContainer()
    {
        // Gắn StructureContainer bằng tag hoặc tạo mới nếu không tìm thấy
        if (StructureContainer == null)
        {
            StructureContainer = GameObject.FindGameObjectWithTag("StructureContainer");
            if (StructureContainer == null)
            {
                StructureContainer = new GameObject("StructureContainer");
                StructureContainer.tag = "StructureContainer";
            }
        }

        // Gắn CoinContainer bằng tag hoặc tạo mới nếu không tìm thấy
        if (CoinContainer == null)
        {
            CoinContainer = GameObject.FindGameObjectWithTag("CoinContainer");
            if (CoinContainer == null)
            {
                CoinContainer = new GameObject("CoinContainer");
                CoinContainer.tag = "CoinContainer";
            }
        }
        // Gắn NatureContainer bằng tag hoặc tạo mới nếu không tìm thấy
        if (NatureContainer == null)
        {
            NatureContainer = GameObject.FindGameObjectWithTag("NatureContainer");
            if (NatureContainer == null)
            {
                NatureContainer = new GameObject("NatureContainer");
                NatureContainer.tag = "NatureContainer";
            }
        }
    }
    #region Coin Spawn
    public int GetRandomNumber( int max)
    {
        return Random.Range(0, max); 
    }
     
    public void DropByNature(Vector2 position, int amount, Vector2 direction)
    {
        Debug.LogWarning("roi ra "+ amount);
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

    private IEnumerator DropByNatureCoroutine(Vector2 position, Vector2 direction, int amount)
    {
        Vector2 newPosition;

        for (int i = 0; i < amount; i++)
        {
            
            newPosition = new Vector2(position.x + direction.x * i, position.y + yOffset + (i * 0.1f));
            GameObject coin = Instantiate(coinPrefab, newPosition, Quaternion.identity, CoinContainer.transform);

            Coin coinScriptInstance = coin.GetComponent<Coin>();
            if (coinScriptInstance != null)
            {
                coinScriptInstance.playerPickupAble = true;
                coinScriptInstance.denizensPickupAble = true;
                coinScriptInstance.floating = false;
            }

            yield return new WaitForSeconds(0.3f); // Wait for 0.3 seconds before spawning the next coin
        }
    }

    private IEnumerator DropByPlayerCoroutine(Vector2 position, Vector2 direction, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            

            Vector2 newPosition = new Vector2(position.x + direction.x * i, position.y + yOffset);
            GameObject coin = Instantiate(coinPrefab, newPosition, Quaternion.identity, CoinContainer.transform);

            Coin coinScriptInstance = coin.GetComponent<Coin>();
            if (coinScriptInstance != null)
            {
                coinScriptInstance.playerPickupAble = false;
                coinScriptInstance.denizensPickupAble = true;
                coinScriptInstance.floating = false;
                coinScriptInstance.CallFunctionCoolDown("Player");
            }

            yield return new WaitForSeconds(0.3f); // Wait for 0.3 seconds to avoid concurrent execution issues
        }
    }

    private IEnumerator DropByDenizenCoroutine(Vector2 position, Vector2 direction, int amount)
    {
        for (int i = 0; i < amount; i++)
        {
           

            Vector2 newPosition = new Vector2(position.x + direction.x * i, position.y);
            GameObject coin = Instantiate(coinPrefab, newPosition, Quaternion.identity, CoinContainer.transform);

            Coin coinScriptInstance = coin.GetComponent<Coin>();
            if (coinScriptInstance != null)
            {
                coinScriptInstance.playerPickupAble = true;
                coinScriptInstance.denizensPickupAble = false;
                coinScriptInstance.floating = false;
                coinScriptInstance.CallFunctionCoolDown("Denizen");
            }

            yield return new WaitForSeconds(0.3f); // Wait for 0.3 seconds to avoid concurrent execution issues
        }
    }


    #endregion
    public void SummonMods(GameObject gameObject,Vector2 position )
    {
        Instantiate(gameObject, position,Quaternion.identity);
    }
}

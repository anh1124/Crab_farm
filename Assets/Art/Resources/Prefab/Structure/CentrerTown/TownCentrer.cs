using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TownCentrer : MonoBehaviour
{
    public BoxCollider2D boxCollider2D;
    public int coinQuantity;
    public GameObject coinSlotLocation;
   
    private void Start()
    {
        boxCollider2D = gameObject.GetComponent<BoxCollider2D>();

        coinSlotLocation = GameObject.Find("CoinSlotLocation");
        
        // Gọi hàm CreateCoinSlot trong InstanceCoinSlot script
        //InstanceCoinSlot.Instance.CreateCoinSlot(coinQuantity, coinSlotLocation);

        // Ẩn đối tượng coinSlotLocation
        //coinSlotLocation.SetActive(false);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player triggered this collider!");
            coinSlotLocation.SetActive(true);
            DestroyAllChildren(coinSlotLocation);
            InstanceCoinSlot.Instance.CreateCoinSlot(coinQuantity, coinSlotLocation);
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player leave this collider!");
            coinSlotLocation.SetActive(false);
            DestroyAllChildren(coinSlotLocation);
        }
    }
    // Phương thức để xóa tất cả các đối tượng con
    private void DestroyAllChildren(GameObject parent)
    {
        foreach (Transform child in parent.transform)
        {
            Destroy(child.gameObject);
        }
    }
}

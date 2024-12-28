using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model;
using Script.Game.Item.Coin;
using UnityEngine;

public class PlayerTriggerHelper : MonoBehaviour
{
    private TriggerCheckSetup triggerCheckSetup;//script này chứa danh sách trigger 
    private List<GameObject> collidingObjectsList;      //danh sách này của triggerCheckSetup chứa các trigger mới
    public GameObject gameObjectHasShowCoinSlot;//gameobject này chứa gameobject vừa hiện cái coinslot lên
    
    public GameObject nowStructureCollider2D;//gameobject này chứa gameobject hiện tại đang va chạm,dungf cais nayf trong playercontroller    
    private void Start()
    {
        triggerCheckSetup = gameObject.GetComponent<TriggerCheckSetup>();
        if (triggerCheckSetup == null)
        {
            // Nếu chưa có, thêm vào
            triggerCheckSetup = gameObject.AddComponent<TriggerCheckSetup>();
        }
    }

    private void Update()
    {
       ShowCoinSlotOfStructurePlayerInTrigger();
    }

    private void ShowCoinSlotOfStructurePlayerInTrigger()
    {
        // Tạo một bản sao của danh sách để duyệt qua
        List<GameObject> objectsToProcess = new List<GameObject>(triggerCheckSetup.GetCollidingObjects());

        foreach (GameObject obj in objectsToProcess)
        {
            if (PrefabData.Instance.playerCoinUserInteractable.Contains(obj.tag))
            {
                nowStructureCollider2D = obj;
                CoinUser nowStructureCollider2DCoinUser = nowStructureCollider2D.GetComponent<CoinUser>();
                nowStructureCollider2DCoinUser.PlayerTriggerOn();

                if (gameObjectHasShowCoinSlot != null && gameObjectHasShowCoinSlot != nowStructureCollider2D)
                {
                    CoinUser gameObjectHasShowCoinSlotCoinUser = gameObjectHasShowCoinSlot.GetComponent<CoinUser>();
                    if (gameObjectHasShowCoinSlotCoinUser != null)
                    {
                        gameObjectHasShowCoinSlotCoinUser.PlayerTriggerOff();
                    }
                }

                gameObjectHasShowCoinSlot = nowStructureCollider2D;
                return;
            }
            if (obj.tag == "Coin")
            {
                Coin coin = obj.GetComponent<Coin>();
                coin.HandlePlayerPickup(gameObject);
            }
        }

        if (gameObjectHasShowCoinSlot != null)
        {
            CoinUser gameObjectHasShowCoinSlotCoinUser = gameObjectHasShowCoinSlot.GetComponent<CoinUser>();
            if (gameObjectHasShowCoinSlotCoinUser != null)
            {
                gameObjectHasShowCoinSlotCoinUser.PlayerTriggerOff();
            }
            gameObjectHasShowCoinSlot = null;
        }
    }

}

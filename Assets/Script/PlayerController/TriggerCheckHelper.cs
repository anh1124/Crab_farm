using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model;
using Script.Game.Item.Coin;
using UnityEngine;

public class TriggerCheckHelper : MonoBehaviour
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
        // Kiểm tra triggerCheckSetup không null
        if (triggerCheckSetup == null) return;

        List<GameObject> objectsToProcess = new List<GameObject>(triggerCheckSetup.GetCollidingObjects());

        // Lọc ra các object không null
        objectsToProcess = objectsToProcess.FindAll(obj => obj != null);

        foreach (GameObject obj in objectsToProcess)
        {
            if (PrefabData.Instance.playerCoinUserInteractable.Contains(obj.tag))
            {
                nowStructureCollider2D = obj;
                
                // Kiểm tra component CoinUser
                CoinUser nowStructureCollider2DCoinUser = nowStructureCollider2D.GetComponent<CoinUser>();
                if (nowStructureCollider2DCoinUser != null)
                {
                    nowStructureCollider2DCoinUser.PlayerTriggerOn();
                }

                // Kiểm tra gameObjectHasShowCoinSlot không null và khác với nowStructureCollider2D
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
            
            if (obj != null && obj.tag == "Coin")
            {
                Coin coin = obj.GetComponent<Coin>();
                if (coin != null)
                {
                    coin.HandlePlayerPickup(gameObject);
                }
            }
        }

        // Kiểm tra cuối cùng cho gameObjectHasShowCoinSlot
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

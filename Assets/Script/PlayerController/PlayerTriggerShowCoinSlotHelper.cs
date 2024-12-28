using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model;
using Script.Game.Item.Coin;
using UnityEngine;

public class PlayerTriggerShowCoinSlotHelper : MonoBehaviour
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
        collidingObjectsList = triggerCheckSetup.GetCollidingObjects(); // Lấy danh sách đang va chạm

        foreach (GameObject obj in collidingObjectsList)
        {
            // Kiểm tra nếu tag của collider nằm trong danh sách tags
            if (PrefabData.Instance.playerCoinUserInteractable.Contains(obj.tag))
            {
                nowStructureCollider2D = obj;
                CoinUser nowStructureCollider2DCoinUser = nowStructureCollider2D.GetComponent<CoinUser>();
                nowStructureCollider2DCoinUser.PlayerTriggerOn();

                // Nếu `gameObjectHasShowCoinSlot` khác với `nowStructureCollider2D`, xử lý tắt trigger
                if (gameObjectHasShowCoinSlot != null && gameObjectHasShowCoinSlot != nowStructureCollider2D)
                {
                    CoinUser gameObjectHasShowCoinSlotCoinUser = gameObjectHasShowCoinSlot.GetComponent<CoinUser>();
                    if (gameObjectHasShowCoinSlotCoinUser != null)
                    {
                        gameObjectHasShowCoinSlotCoinUser.PlayerTriggerOff();
                    }
                }

                // Cập nhật `gameObjectHasShowCoinSlot` sau khi xử lý
                gameObjectHasShowCoinSlot = nowStructureCollider2D;
                return; // Thoát khỏi phương thức sau khi xử lý xong
            }
        }

        // Nếu không có đối tượng hợp lệ trong danh sách, tắt trigger của `gameObjectHasShowCoinSlot`
        if (gameObjectHasShowCoinSlot != null)
        {
            CoinUser gameObjectHasShowCoinSlotCoinUser = gameObjectHasShowCoinSlot.GetComponent<CoinUser>();
            if (gameObjectHasShowCoinSlotCoinUser != null)
            {
                gameObjectHasShowCoinSlotCoinUser.PlayerTriggerOff();
            }
            gameObjectHasShowCoinSlot = null; // Đặt về null sau khi tắt
        }
    }

}

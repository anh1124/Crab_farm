using System;
using System.Collections;
using System.Collections.Generic;
using Script.Data_Model;
using Script.Game.Item.Coin;
using UnityEngine;

public class TriggerCheckHelper : MonoBehaviour
{
    private TriggerCheckSetup triggerCheckSetup;//script này chứa danh sách trigger
    private List<GameObject> collidingObjectsList;//danh sách này của triggerCheckSetup chứa các trigger mới
    public GameObject gameObjectHasShowCoinSlot;//gameobject này chứa gameobject vừa hiện cái coinslot lên đồng thời chỉ có 1 gameobejct hiện coinslot ở 1 thời điểm

    public GameObject nowStructureCollider2D;//gameobject này chứa gameobject hiện tại đang va chạm,dungf cais nayf trong playercontroller

    // Debug control - prevent spam logging
    private GameObject lastLoggedStructure;// Track last structure we logged for
    private float lastLogTime;// Prevent logging too frequently
    private const float LOG_COOLDOWN = 1.0f;// Minimum time between logs for same structure
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
        if (triggerCheckSetup == null) return;

        List<GameObject> objectsToProcess = new List<GameObject>(triggerCheckSetup.GetCollidingObjects());
        objectsToProcess = objectsToProcess.FindAll(obj => obj != null);

        // First handle coins to ensure they're always processed
        foreach (GameObject obj in objectsToProcess)
        {
            if (obj != null && obj.tag == "Coin")
            {
                Coin coin = obj.GetComponent<Coin>();
                if (coin != null)
                {
                    coin.HandlePlayerPickup(gameObject);
                }
            }
        }

        // Then handle structures
        foreach (GameObject obj in objectsToProcess)
        {
            if (PrefabData.Instance.playerCoinUserInteractable.Contains(obj.tag))
            {
                HandleStructureInteraction(obj);
                return; // Exit after handling one structure
            }
        }

        // If no valid structures found, clear the current structure
        if (!objectsToProcess.Exists(obj => PrefabData.Instance.playerCoinUserInteractable.Contains(obj.tag)))
        {
            ClearCurrentStructure();
        }
    }

    private void HandleStructureInteraction(GameObject structure)
    {
        nowStructureCollider2D = structure;

        // DEBUG: Only log when it's a new structure or enough time has passed
        bool shouldLog = false;
        float currentTime = Time.time;

        if (lastLoggedStructure != structure)
        {
            // Different structure - always log
            shouldLog = true;
            lastLoggedStructure = structure;
            lastLogTime = currentTime;
        }
        else if (currentTime - lastLogTime >= LOG_COOLDOWN)
        {
            // Same structure but cooldown passed - log again
            shouldLog = true;
            lastLogTime = currentTime;
        }

        if (shouldLog)
        {
            string debugInfo = $"[TRIGGER] {structure.name}: ";

            // Get wall info
            var wallBehavior = nowStructureCollider2D.GetComponent<Script.Game.Structures.Wall_And_Stump.WallBehavior>();
            if (wallBehavior != null)
            {
                debugInfo += wallBehavior.GetWallDebugInfo();
            }
            else
            {
                debugInfo += "No WallBehavior";
            }

            // Get coinuser info
            CoinUser nowStructureCollider2DCoinUser = nowStructureCollider2D.GetComponent<CoinUser>();
            if (nowStructureCollider2DCoinUser != null)
            {
                debugInfo += " | " + nowStructureCollider2DCoinUser.GetTriggerDebugInfo(out bool shouldShowSlots);

                if (shouldShowSlots)
                {
                    nowStructureCollider2DCoinUser.PlayerTriggerOn();
                }
            }
            else
            {
                debugInfo += " | No CoinUser";
            }

          //  Debug.Log(debugInfo);
        }
        else
        {
            // Still process coin user logic even if not logging
            CoinUser nowStructureCollider2DCoinUser = nowStructureCollider2D.GetComponent<CoinUser>();
            if (nowStructureCollider2DCoinUser != null)
            {
                nowStructureCollider2DCoinUser.GetTriggerDebugInfo(out bool shouldShowSlots);

                if (shouldShowSlots)
                {
                    nowStructureCollider2DCoinUser.PlayerTriggerOn();
                }
            }
        }

        // Handle previous coinuser off
        if (gameObjectHasShowCoinSlot != null && gameObjectHasShowCoinSlot != nowStructureCollider2D)
        {
            CoinUser gameObjectHasShowCoinSlotCoinUser = gameObjectHasShowCoinSlot.GetComponent<CoinUser>();
            if (gameObjectHasShowCoinSlotCoinUser != null)
            {
                gameObjectHasShowCoinSlotCoinUser.PlayerTriggerOff();
            }
        }

        gameObjectHasShowCoinSlot = nowStructureCollider2D;
    }

    private void ClearCurrentStructure()
    {
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

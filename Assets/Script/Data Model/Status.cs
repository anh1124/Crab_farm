using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Status : MonoBehaviour
{
     
    
    //thông số cơ bản
    public int maxHp;
    public int currentHp;
    public int maxMp;
    public int currentMp;
    public int maxStamina;
    public int currentStamina;
    public int def;

    public int atk;
    public int ats;
    public int critRate;
    public int critDame;//defautl 150
    
    public int jumpForce;
    public int moveSpeed;
    public int coin;
    public bool isGround;
    
    public void AddCoin(GameObject coinGameObject)
    {
        coin++;
        Destroy(coinGameObject);
    }

    private void Start()
    {
        CheckRequireScript();
    }

    void CheckRequireScript()
    {
        GroundCheck groundCheck = gameObject.GetComponent<GroundCheck>();
        if (groundCheck == null)
        {
            Debug.LogWarning(gameObject.name+" không có scriptGroundCheck");
        }

        TriggerCheckSetup triggerCheckSetup = gameObject.GetComponent<TriggerCheckSetup>();
        if (triggerCheckSetup == null)
        {
            Debug.LogWarning(gameObject.name+" không có TriggerCheckSetup");
        }
        
    }

  
        
}

using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Status : MonoBehaviour
{
    public int hp;
    public int jumpForce;
    public int moveSpeed;
    public int coin;

    public bool isGround;
    private TriggerCheckSetup triggerCheckSetup;//script này chứa danh sách trigger
    private List<GameObject> collidingObjectsList;//danh sách này của triggerCheckSetup
   
    private void Start()
    {
        triggerCheckSetup = gameObject.GetComponent<TriggerCheckSetup>();
        if (triggerCheckSetup == null)
        {
            // Nếu chưa có, thêm vào
            triggerCheckSetup = gameObject.AddComponent<TriggerCheckSetup>();
            Debug.Log("TriggerCheckSetup script has been added to the GameObject.");
        }
    }


    private void Update()
    {
        GroundCheck();
    }
    public void GroundCheck()
    {
        collidingObjectsList = triggerCheckSetup.GetCollidingObjects();

        // Kiểm tra xem có bất kỳ đối tượng nào trong danh sách thuộc layer "Tilemap"
        isGround = false; // Đặt mặc định là false
        foreach (GameObject obj in collidingObjectsList)
        {
            if (obj != null && obj.layer == LayerMask.NameToLayer("Ground"))
            {
                isGround = true;
                break; // Thoát vòng lặp ngay khi tìm thấy
            }
        }
    }

    public void AddCoin(GameObject coinGameObject)
    {
        coin++;
        Destroy(coinGameObject);
    }

}

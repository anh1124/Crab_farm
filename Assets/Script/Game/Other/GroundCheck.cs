using System.Collections.Generic;
using Script.Data_Model.Status;
using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    private TriggerCheckSetup triggerCheckSetup; // Script quản lý danh sách các trigger
    private List<GameObject> collidingObjectsList; // Danh sách các đối tượng đang va chạm
    private PlayerStatus status; // Truy cập trạng thái từ script Status

    private void Start()
    {
        // Lấy tham chiếu tới script Status
        status = gameObject.GetComponent<PlayerStatus>();
        // Lấy tham chiếu tới TriggerCheckSetup
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
        CheckGround();
    }

    private void CheckGround()
    {
        if (status == null)
        {
            Debug.LogError($"GameObject '{gameObject.name}' does not have a Status script required by GroundCheck.");
            return; // Nếu không có Status, không làm gì cả
        }

        collidingObjectsList = triggerCheckSetup.GetCollidingObjects();

        // Kiểm tra danh sách đối tượng thuộc layer "Ground"
        status.isGround = false; // Mặc định không chạm đất
        foreach (GameObject obj in collidingObjectsList)
        {
            if (obj != null && obj.layer == LayerMask.NameToLayer("Ground"))
            {
                status.isGround = true;
                break; // Ngừng kiểm tra nếu phát hiện đối tượng hợp lệ
            }
        }
    }
}
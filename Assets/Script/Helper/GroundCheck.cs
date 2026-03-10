using System.Collections.Generic;
using Script.Data_Model.Status;
using UnityEngine;

public class GroundCheck : MonoBehaviour
{
    private PlayerStatus status; // Truy cập trạng thái từ script Status
    private HashSet<GameObject> groundObjects = new HashSet<GameObject>(); // Danh sách các đối tượng Ground đang va chạm

    private void Start()
    {
        // Lấy tham chiếu tới script Status
        status = gameObject.GetComponent<PlayerStatus>();
        if (status == null)
        {
            Debug.LogError($"GameObject '{gameObject.name}' does not have a PlayerStatus script required by GroundCheck.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            groundObjects.Add(other.gameObject);
            UpdateGroundStatus();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            groundObjects.Remove(other.gameObject);
            UpdateGroundStatus();
        }
    }

    private void UpdateGroundStatus()
    {
        if (status != null)
        {
            status.isGround = groundObjects.Count > 0;
        }
    }
}

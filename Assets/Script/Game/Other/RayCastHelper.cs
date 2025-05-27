using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RayCastHelper : MonoBehaviour
{
    //avealance fuj test net -
    public Color rayColor = Color.red;
    
    public float raycastDistance = 5f;
    public float raycastOffsetY = 2f;
    
    // Cờ để bật/tắt vẽ ray trong Debug
    public bool isDrawRay = true;
    public bool showDebugLog = false;  // Thêm biến để bật/tắt debug log
    
    // Thay LayerMask bằng List<string> để chứa các tag cần kiểm tra
    [Header("Tag Settings")]
    public List<string> tagsToDetect = new List<string>();
    public List<GameObject> hitObjects = new List<GameObject>();

    private void Update()
    {
        DeBugDrawRay();
    }

    public List<GameObject> DrawRayAndReturnListHitObjects()
    {
        DrawRay();
        return new List<GameObject>(hitObjects);
    }

    public void DrawRay()
    {
        // Làm sạch danh sách các GameObject bị bắn trúng trước đó
        hitObjects.Clear();

        // Lấy vị trí hiện tại của đối tượng và thay đổi y thành y + raycastOffsetY
        Vector2 origin = new Vector2(transform.position.x, transform.position.y + raycastOffsetY);

        // Xác định hướng raycast dựa trên scale X
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        
        // Thực hiện raycast với tất cả các layer (vì ta sẽ lọc bằng tag sau)
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, raycastDistance);

     

        // Vẽ raycast nếu isDrawRay = true
        if (isDrawRay)
        {
            Debug.DrawRay(origin, direction * raycastDistance, rayColor);
        }

        // Kiểm tra và xử lý các va chạm
        foreach (RaycastHit2D hit in hits)
        {
            GameObject hitObject = hit.collider.gameObject;
            
          
            // Bỏ qua nếu là chính GameObject phát ray
            if (hitObject == gameObject)
            {
                continue;
            }

            // Kiểm tra nếu object có tag nằm trong danh sách cần phát hiện
            if (tagsToDetect.Contains(hitObject.tag))
            {
               
                // Thêm vào danh sách các GameObject bị bắn trúng nếu chưa có
                if (!hitObjects.Contains(hitObject))
                {
                    hitObjects.Add(hitObject);
                    
                }
            }
           
        }

     
    }

    public void DeBugDrawRay()
    {
        if (isDrawRay)
        {
            Vector2 origin = new Vector2(transform.position.x, transform.position.y + raycastOffsetY);
            Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            Debug.DrawRay(origin, direction * raycastDistance, rayColor);
        }
    }

    public List<GameObject> GetHitObjects()
    {
        return new List<GameObject>(hitObjects);
    }

    public void SetRayLength(float length)
    {
        raycastDistance = length;
    }
}

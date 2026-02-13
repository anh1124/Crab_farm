using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RayCastHelper : MonoBehaviour
{
    //avealance fuj test net -
    public Color rayColor = Color.red;
    
    public float raycastDistance = 5f;
    public float raycastOffsetY = 0.25f;
    
    // Cờ để bật/tắt vẽ ray trong Debug (System control)
    public bool isDrawRay = true;
    public bool isShowGizmoUser = true; // User control
    public bool showDebugLog = false;  // Thêm biến để bật/tắt debug log
    
    // Thay LayerMask bằng List<string> để chứa các tag cần kiểm tra
    [Header("Tag Settings")]
    public List<string> tagsToDetect = new List<string> { "Player", "Coin", "Enemy", "Animal" };
    public List<GameObject> hitObjects = new List<GameObject>();

    // Update removed to control drawing via behavior call
    
    public List<GameObject> DrawRayAndReturnListHitObjects(float? overrideDistance = null)
    {
        float currentDistance = overrideDistance ?? raycastDistance;
        DrawRay(currentDistance);
        if (isDrawRay && isShowGizmoUser)
        {
            DeBugDrawRay(currentDistance);
        }
        return new List<GameObject>(hitObjects);
    }

    public void DrawRay(float distance)
    {
        // Làm sạch danh sách các GameObject bị bắn trúng trước đó
        hitObjects.Clear();

        // Lấy vị trí hiện tại của đối tượng và thay đổi y thành y + raycastOffsetY
        Vector2 origin = new Vector2(transform.position.x, transform.position.y + raycastOffsetY);

        // Xác định hướng raycast dựa trên scale X
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        
        // Thực hiện raycast với tất cả các layer (vì ta sẽ lọc bằng tag sau)
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance);

     

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

    public void DeBugDrawRay(float distance)
    {
        if (isDrawRay && isShowGizmoUser)
        {
            Vector2 origin = new Vector2(transform.position.x, transform.position.y + raycastOffsetY);
            Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            Debug.DrawRay(origin, direction * distance, rayColor);
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

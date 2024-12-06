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
    
    // LayerMask để chọn các layer cần raycast
    [Header("Layer Settings")]
    public LayerMask raycastLayerMask;
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
        // Nếu scale X âm thì raycast sang trái, dương thì sang phải
        Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

        // Thực hiện raycast với LayerMask
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, raycastDistance, raycastLayerMask);

        // Vẽ raycast nếu isDrawRay = true
        if (isDrawRay)
        {
            // Vẽ ray trong chế độ Scene (Debug)
            Debug.DrawRay(origin, direction * raycastDistance, rayColor);
        }

        // Kiểm tra và xử lý các va chạm
        foreach (RaycastHit2D hit in hits)
        {
            // Bỏ qua nếu là chính GameObject phát ray
            if (hit.collider.gameObject == gameObject)
                continue;

            // Thêm vào danh sách các GameObject bị bắn trúng
            if (!hitObjects.Contains(hit.collider.gameObject))
            {
                hitObjects.Add(hit.collider.gameObject);
            }
        }
    }

// Cập nhật phương thức DeBugDrawRay tương tự
    public void DeBugDrawRay()
    {
        // Nếu isDrawRay = true, vẽ ray trong mỗi Update
        if (isDrawRay)
        {
            Vector2 origin = new Vector2(transform.position.x, transform.position.y + raycastOffsetY);
        
            // Xác định hướng raycast dựa trên scale X
            Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;

            // Vẽ ray trong chế độ Scene (Debug) mỗi frame
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

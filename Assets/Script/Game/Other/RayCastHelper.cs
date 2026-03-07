using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class RayCastHelper : MonoBehaviour
{
    private RaycastHit2D[] _raycastResults = new RaycastHit2D[200];

    //avealance fuj test net -
    public Color rayColor = Color.red;
    
    public float raycastDistance = 5f;
    public float raycastOffsetY = 0.25f; // Sửa lại mốc offset ban đầu theo ảnh User
    
    // Cờ để bật/tắt vẽ ray trong Debug
    public bool isDrawRay = true;
    public bool isShowGizmoUser = true;
    public bool showDebugLog = false;  // Thêm biến để bật/tắt debug log
    
    // Core Event để hệ thống bên ngoài lắng nghe mỗi khi Scanner quét xong
    public event Action<List<GameObject>> OnDetectedObjectsChanged;

    [Header("Behavior Settings")]
    public bool isActive = true; // Cờ cho phép bật/tắt quét tia độc lập
    
    [Header("Scan Settings")]
    [Tooltip("Số lần quét raycast mỗi giây (VD: 5 tức là 5 lần/s ~ 0.2s một lần)")]
    public float scanFrequencyHz = 5f;
    private float scanTimer = 0f;

    // Thay LayerMask bằng List<string> để chứa các tag cần kiểm tra
    [Header("Tag Settings")]
    public List<string> tagsToDetect = new List<string>();
    
    [Header("Debug Display")]
    [Tooltip("Danh sách hiển thị cho Inspector (chỉ update khi bật showDebugLog)")]
    [SerializeField] private List<GameObject> debugHitObjects = new List<GameObject>();
    
    // Core logic
    private List<GameObject> hitObjects = new List<GameObject>();

    private void Update()
    {
        if (!isActive) 
        {
            if (hitObjects.Count > 0) hitObjects.Clear();
            return;
        }

        if (scanFrequencyHz > 0)
        {
            scanTimer += Time.deltaTime;
            float interval = 1f / scanFrequencyHz;
            
            if (scanTimer >= interval)
            {
                scanTimer = 0f;
                DrawRay();
            }
        }
    }

    public List<GameObject> GetListHitObjects(float? overrideDistance = null)
    {
        if (overrideDistance.HasValue && overrideDistance.Value > 0)
        {
            float oldDist = raycastDistance;
            raycastDistance = overrideDistance.Value;
            DrawRay();
            raycastDistance = oldDist;
        }
        else
        {
            DrawRay(); // Luôn quét fresh list khi có unit gọi hàm để tránh lấy list cũ do 5Hz không kịp
        }
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

        
        // Cấu hình Filter cho phép quét xuyên qua cả Trigger Colliders, đồng thời bỏ qua Layer Matrix ảo
        ContactFilter2D filter = new ContactFilter2D();
        filter.useTriggers = true; // Bắt buộc đọc cả các trigger ảo (VD: vùng nhận damage của Player)
        filter.useDepth = false;
        
        // ~0 = tất cả layer, bỏ qua Layer Collision Matrix (ta tự lọc bằng tag bên dưới)
        filter.useLayerMask = true;
        filter.layerMask = ~0;

        // Dùng BoxCast (cao 0.3f) thay Raycast 1 pixel để bắt mục tiêu lệch trục Y nhẹ (VD: ShieldBot cận chiến)
        // Tham số: origin, size box, góc, hướng, contactFilter, kết quả, khoảng cách
        int hitCount = Physics2D.BoxCast(origin, new Vector2(0.05f, 0.3f), 0f, direction, filter, _raycastResults, raycastDistance);

        // ================== DEBUG LOG ==================
        if (showDebugLog)
        {
            Debug.Log($"[RCH:{gameObject.name}] TỔNG HITS ({hitCount}) | origin={origin} | dir={direction} | dist={raycastDistance}");
            for (int i = 0; i < hitCount; i++)
            {
                if (_raycastResults[i].collider != null)
                {
                    GameObject obj = _raycastResults[i].collider.gameObject;
                    Debug.Log($"   -> HitCollider: {obj.name} | Tag: {obj.tag} | Layer: {LayerMask.LayerToName(obj.layer)}");
                }
            }
        }
        // ===============================================

        // Vẽ raycast nếu isDrawRay = true
        if (isDrawRay && isShowGizmoUser)
        {
            Debug.DrawRay(origin, direction * raycastDistance, rayColor);
        }

        // Kiểm tra và xử lý các va chạm
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit2D hit = _raycastResults[i];
            if (hit.collider == null) continue;
            GameObject hitObject = hit.collider.gameObject;
          
            // Bỏ qua nếu hit collider thuộc về ĐÚNG CÁI CÂY GIA PHẢ CỦA BẢN THÂN (Theo Fix 3 của User)
            if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
            {
                continue;
            }

            // [FIX LỖI TICK VÀO TRIGGER LAYER]: 
            // Nếu đánh trúng 1 child collider (ví dụ hộp Trigger nhận Damage của Player),
            // ta chỉ cần dò lên duy nhất 1 cấp cha để kiểm tra Tag.
            GameObject validTarget = null;

            if (tagsToDetect.Contains(hitObject.tag))
            {
                validTarget = hitObject;
            }
            else if (hitObject.transform.parent != null && tagsToDetect.Contains(hitObject.transform.parent.tag))
            {
                validTarget = hitObject.transform.parent.gameObject;
            }

            // Nếu tìm thấy một target hợp lệ (có thể là chính nó hoặc cha của nó)
            if (validTarget != null)
            {
                // Thêm vào danh sách các GameObject bị bắn trúng nếu chưa có
                if (!hitObjects.Contains(validTarget))
                {
                    hitObjects.Add(validTarget);
                }
            }
        }

        if (showDebugLog)
        {
            debugHitObjects = new List<GameObject>(hitObjects);
        }

        // Bắn Event Notify cho các AI Controller (Observer Pattern)
        if (hitObjects.Count > 0)
        {
            OnDetectedObjectsChanged?.Invoke(hitObjects);
        }
    }

    public void DeBugDrawRay(float objDst)
    {
        if (isDrawRay && isShowGizmoUser)
        {
            Vector2 origin = new Vector2(transform.position.x, transform.position.y + raycastOffsetY);
            Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            Debug.DrawRay(origin, direction * objDst, rayColor);
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

    private void OnDrawGizmos()
    {
        if (isDrawRay && isShowGizmoUser)
        {
            Vector2 origin = new Vector2(transform.position.x, transform.position.y + raycastOffsetY);
            Vector2 direction = transform.localScale.x > 0 ? Vector2.right : Vector2.left;
            
            Gizmos.color = rayColor;
            Gizmos.DrawRay(origin, direction * raycastDistance);
        }
    }
}

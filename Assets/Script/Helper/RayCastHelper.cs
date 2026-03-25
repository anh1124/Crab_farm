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
    

    // Thay LayerMask bằng List<string> để chứa các tag cần kiểm tra
    [Header("Tag Settings")]
    public List<string> tagsToDetect = new List<string>();
    
    [Header("Layer Settings")]
    public LayerMask targetLayer = ~0;
    
    [Header("Debug Display")]
    [Tooltip("Danh sách hiển thị cho Inspector (chỉ update khi bật showDebugLog)")]
    [SerializeField] private List<GameObject> debugHitObjects = new List<GameObject>();
    
    // Core logic
    private List<GameObject> hitObjects = new List<GameObject>();
    private ContactFilter2D _filter;

    private void Awake()
    {
        _filter = new ContactFilter2D();
        _filter.useTriggers = true;
        _filter.useDepth = false;
        _filter.useLayerMask = true;
    }

    private void Update()
    {
        // [REFECTOR] Logic Scan thực tế vẫn do Behavior gọi (GetListHitObjects/DrawRay).
        // Tuy nhiên, ta cần cập nhật Gizmo mỗi frame để người dùng thấy tia quay theo hướng Denizen mượt mà (60Hz).
        if (isActive && isDrawRay && isShowGizmoUser)
        {
            Vector2 origin = new Vector2(transform.position.x, transform.position.y + raycastOffsetY);
            Vector2 direction = transform.lossyScale.x > 0 ? Vector2.right : Vector2.left;
            Debug.DrawRay(origin, direction * raycastDistance, rayColor);
        }
    }

    /// <summary>
    /// Hàm Scan chủ động: Vừa thực hiện quét tia, vừa trả về danh sách đối tượng.
    /// Thường được gọi bởi các Behavior Script trong Update của chúng.
    /// </summary>
    public List<GameObject> GetListHitObjects(float? overrideDistance = null)
    {
        if (!isActive) return new List<GameObject>();

        if (overrideDistance.HasValue && overrideDistance.Value > 0)
        {
            float oldDist = raycastDistance;
            raycastDistance = overrideDistance.Value;
            DrawRay();
            raycastDistance = oldDist;
        }
        else
        {
            DrawRay();
        }
        return hitObjects;
    }

    public void DrawRay()
    {
        // Làm sạch danh sách các GameObject bị bắn trúng trước đó
        hitObjects.Clear();

        // Lấy vị trí hiện tại của đối tượng và thay đổi y thành y + raycastOffsetY
        Vector2 origin = new Vector2(transform.position.x, transform.position.y + raycastOffsetY);

        // Xác định hướng raycast dựa trên scale X (world scale/lossyScale để bắt kịp cha bị flip)
        Vector2 direction = transform.lossyScale.x > 0 ? Vector2.right : Vector2.left;

        _filter.layerMask = targetLayer;

        // Dùng BoxCast (cao 0.3f) thay Raycast 1 pixel để bắt mục tiêu lệch trục Y nhẹ (VD: ShieldBot cận chiến)
        // Tham số: origin, size box, góc, hướng, contactFilter, kết quả, khoảng cách
        int hitCount = Physics2D.BoxCast(origin, new Vector2(0.05f, 0.3f), 0f, direction, _filter, _raycastResults, raycastDistance);

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
            Vector2 direction = transform.lossyScale.x > 0 ? Vector2.right : Vector2.left;
            Debug.DrawRay(origin, direction * objDst, rayColor);
        }
    }

    public List<GameObject> GetHitObjects()
    {
        return hitObjects;
    }

    public void SetRayLength(float length)
    {
        raycastDistance = length;
    }

    [ContextMenu("Debug Log All Hits")]
    public void DebugLogAllHits()
    {
        Vector2 origin = new Vector2(transform.position.x, transform.position.y + raycastOffsetY);
        Vector2 direction = transform.lossyScale.x > 0 ? Vector2.right : Vector2.left;
        
        _filter.layerMask = targetLayer;

        RaycastHit2D[] results = new RaycastHit2D[500];
        int hitCount = Physics2D.BoxCast(origin, new Vector2(0.05f, 0.3f), 0f, direction, _filter, results, raycastDistance);

        Script.Utils.DebugUtility.LogPhysicsResults(gameObject.name, "Raycast(Box)", origin, hitCount, results, tagsToDetect);
    }
    private void OnDrawGizmos()
    {
        if (isDrawRay && isShowGizmoUser)
        {
            Vector2 origin = new Vector2(transform.position.x, transform.position.y + raycastOffsetY);
            Vector2 direction = transform.lossyScale.x > 0 ? Vector2.right : Vector2.left;
            
            Gizmos.color = rayColor;
            Gizmos.DrawRay(origin, direction * raycastDistance);
        }
    }
}

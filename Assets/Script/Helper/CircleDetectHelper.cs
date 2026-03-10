using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Script.Game.Other
{
    public class CircleDetectHelper : MonoBehaviour
    {
        public event Action<List<GameObject>> OnDetectedObjectsChanged;

        private Collider2D[] _overlapResults = new Collider2D[200]; // Rổ chứa tái sử dụng, tối đa 200 kết quả 1 lần quét

        public Color gizmoColor = Color.magenta;
        public float detectRadius = 10f;
        public bool isShowGizmo = true;//hệ thống tự set
        public bool isShowGizmoUser = true;//user có cho debug không
        
        [Header("Behavior Settings")]
        public bool isActive = true; // Cờ cho phép bật/tắt quét tia độc lập
        

        [Header("Tag Settings")]
        public List<string> tagsToDetect = new List<string> { "Player", "Coin", "Enemy", "Animal" };

        [Header("Layer Settings")]
        public LayerMask targetLayer = ~0;

        [Header("Debug Display")]
        [Tooltip("Bật cờ này để xem danh sách Object bên dưới trên Inspector")]
        public bool showDebugLog = false;
        
        [SerializeField, Tooltip("Danh sách hiển thị (An toàn cho Inspector, chỉ cập nhật khi bật showDebugLog)")]
        private List<GameObject> debugDetectedObjects = new List<GameObject>();

        // Cache result (Core Logic)
        private List<GameObject> detectedObjects = new List<GameObject>();

        private void Update()
        {
            // [REFECTOR] Logic Update đã được chuyển sang NPC Behavior chủ động gọi.
        }

        public List<GameObject> GetListDetectedObjects(float? overrideRadius = null)
        {
            if (!isActive) return new List<GameObject>();

            if (overrideRadius.HasValue && overrideRadius.Value > 0)
            {
                float oldRad = detectRadius;
                detectRadius = overrideRadius.Value;
                ScanCircle();
                detectRadius = oldRad;
            }
            else
            {
                ScanCircle();
            }
            return new List<GameObject>(detectedObjects);
        }

        public void ScanCircle()
        {
            detectedObjects.Clear();
            
            // Visual Debug (Runtime)
            if (isShowGizmo && isShowGizmoUser)
            {
                DrawDebugCircle(detectRadius);
            }

            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.useDepth = false;
            filter.useLayerMask = true;
            filter.layerMask = targetLayer; // Quét mọi layer, tự lọc bằng code bên dưới
            
            int hitCount = Physics2D.OverlapCircle(transform.position, detectRadius, filter, _overlapResults);
            
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D col = _overlapResults[i];
                if (col == null) continue;

                GameObject hitObj = col.gameObject;

                // Bỏ qua nếu hit collider thuộc về ĐÚNG CÁI CÂY GIA PHẢ CỦA BẢN THÂN (Theo Fix User)
                if (hitObj.transform.IsChildOf(transform) || hitObj.transform == transform)
                {
                    continue;
                }

                // Dò Tag: Kiểm tra bản thân hoặc 1 cấp cha
                GameObject validTarget = null;
                if (tagsToDetect.Contains(hitObj.tag))
                {
                    validTarget = hitObj;
                }
                else if (hitObj.transform.parent != null && tagsToDetect.Contains(hitObj.transform.parent.tag))
                {
                    validTarget = hitObj.transform.parent.gameObject;
                }

                if (validTarget != null)
                {
                    if (!detectedObjects.Contains(validTarget))
                    {
                        detectedObjects.Add(validTarget);
                    }
                }
            }

            // Đồng bộ List an toàn cho Inspector nếu đang bật Debug
            if (showDebugLog)
            {
                // Thay vi Clear va Add, ta tao thuc thi List moi de Inspector khong bi loi Index
                debugDetectedObjects = new List<GameObject>(detectedObjects);
            }

            // Emit event with the newly detected objects
            OnDetectedObjectsChanged?.Invoke(detectedObjects);
        }

        private void DrawDebugCircle(float radius)
        {
            int segments = 36;
            float angle = 0f;
            float angleStep = 360f / segments;
            
            Vector3 center = transform.position;
            Vector3 prevPoint = center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0);

            for (int i = 0; i <= segments; i++)
            {
                angle += angleStep;
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * radius, Mathf.Sin(angle * Mathf.Deg2Rad) * radius, 0);
                Debug.DrawLine(prevPoint, nextPoint, gizmoColor);
                prevPoint = nextPoint;
            }
        }

        public void SetDetectRadius(float radius)
        {
            detectRadius = radius;
        }

        [ContextMenu("Debug Log All Hits")]
        public void DebugLogAllHits()
        {
            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = true;
            filter.useDepth = false;
            filter.useLayerMask = true;
            filter.layerMask = targetLayer;

            Collider2D[] results = new Collider2D[500];
            int hitCount = Physics2D.OverlapCircle(transform.position, detectRadius, filter, results);

            Script.Utils.DebugUtility.LogPhysicsResults(gameObject.name, "CircleOverlap", transform.position, hitCount, results, tagsToDetect);
        }

        private void OnDrawGizmos()
        {
            if (isShowGizmo && isShowGizmoUser)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(transform.position, detectRadius);
            }
        }
    }
}

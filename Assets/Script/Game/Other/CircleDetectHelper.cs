using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Game.Other
{
    public class CircleDetectHelper : MonoBehaviour
    {
        private Collider2D[] _overlapResults = new Collider2D[200]; // Rổ chứa tái sử dụng, tối đa 200 kết quả 1 lần quét

        public Color gizmoColor = Color.magenta;
        public float detectRadius = 10f;
        public bool isShowGizmo = true;//hệ thống tự set
        public bool isShowGizmoUser = true;//user có cho debug không
        
        [Header("Behavior Settings")]
        public bool isActive = true; // Cờ cho phép bật/tắt quét tia độc lập
        
        [Header("Scan Settings")]
        [Tooltip("Số lần quét mỗi giây (0 = mỗi frame)")]
        public float scanFrequencyHz = 5f;
        private float scanTimer = 0f;

        [Header("Tag Settings")]
        public List<string> tagsToDetect = new List<string> { "Player", "Coin", "Enemy", "Animal" };

        [Header("Debug Display")]
        [Tooltip("Bật cờ này để xem danh sách Object bên dưới trên Inspector")]
        public bool showDebugLog = false;
        
        [SerializeField, Tooltip("Danh sách hiển thị (An toàn cho Inspector, chỉ cập nhật khi bật showDebugLog)")]
        private List<GameObject> debugDetectedObjects = new List<GameObject>();

        // Cache result (Core Logic)
        private List<GameObject> detectedObjects = new List<GameObject>();

        private void Update()
        {
            if (!isActive) 
            {
                if (detectedObjects.Count > 0) detectedObjects.Clear();
                return;
            }

            if (scanFrequencyHz > 0)
            {
                scanTimer += Time.deltaTime;
                float interval = 1f / scanFrequencyHz;
                
                if (scanTimer >= interval)
                {
                    scanTimer = 0f;
                    ScanCircle();
                }
            }
            else
            {
                ScanCircle(); // Quét mỗi frame nếu Hz = 0
            }
        }

        public List<GameObject> GetListDetectedObjects(float? overrideRadius = null)
        {
            if (overrideRadius.HasValue && overrideRadius.Value > 0)
            {
                float oldRad = detectRadius;
                detectRadius = overrideRadius.Value;
                ScanCircle();
                detectRadius = oldRad;
            }
            else
            {
                ScanCircle(); // Luôn scan fresh khi được gọi
            }
            return new List<GameObject>(detectedObjects);
        }

        private void ScanCircle()
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
            filter.layerMask = ~0; // Quét mọi layer, tự lọc bằng code bên dưới
            
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
                // Thay vi Clear va Add, ta tao thuc the List moi de Inspector khong bi loi Index
                debugDetectedObjects = new List<GameObject>(detectedObjects);
            }
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

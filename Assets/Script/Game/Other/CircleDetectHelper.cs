using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Game.Other
{
    public class CircleDetectHelper : MonoBehaviour
    {
        public Color gizmoColor = Color.magenta;
        public float detectRadius = 10f;
        public bool isShowGizmo = true;//hệ thống tự set
        public bool isShowGizmoUser = true;//user có cho debug không
        [Header("Tag Settings")]
        public List<string> tagsToDetect = new List<string> { "Player", "Coin", "Enemy", "Animal" };

        // Cache result
        [SerializeField] private List<GameObject> detectedObjects = new List<GameObject>();

        private void OnDrawGizmos()
        {
            if (isShowGizmo && isShowGizmoUser)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawWireSphere(transform.position, detectRadius);
            }
        }


        public List<GameObject> ScanAndGetDetectedObjects(float? radius = null)
        {
            detectedObjects.Clear();
            float currentRadius = radius ?? detectRadius;
            
            // Visual Debug (Runtime)
            if (isShowGizmo && isShowGizmoUser)
            {
                DrawDebugCircle(currentRadius);
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, currentRadius);
            
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;

                if (tagsToDetect.Contains(hit.tag))
                {
                    detectedObjects.Add(hit.gameObject);
                }
            }
            
            return new List<GameObject>(detectedObjects);
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
    }
}

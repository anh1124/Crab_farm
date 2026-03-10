using Script.Game.Item.Coin;
using UnityEngine;

namespace Script.Utils
{
    public static class DebugUtility
    {
        public static readonly bool IsDebugEnabled = true;

        public static void LogCollidedObjects(string colliderName, Collider2D[] hits, int count)
        {
            if (!IsDebugEnabled) return;

            if (string.IsNullOrEmpty(colliderName)) colliderName = "(unknown)";
            if (hits == null || count <= 0)
            {
                Debug.Log($"[Utility.LogCollidedObjects] Collider \"{colliderName}\" không va chạm với đối tượng nào.");
                return;
            }

            int safeCount = Mathf.Min(count, hits.Length);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append($"[Utility.LogCollidedObjects] Collider \"{colliderName}\" đã va chạm với {safeCount} đối tượng: ");
            for (int i = 0; i < safeCount; i++)
            {
                var h = hits[i];
                if (h != null && h.gameObject != null)
                {
                    sb.Append(h.gameObject.name);
                    sb.Append(" (Tag: ");
                    sb.Append(h.gameObject.tag);
                    sb.Append(")");
                    if (i < safeCount - 1) sb.Append(", ");
                }
            }
            Debug.Log(sb.ToString());
        }
        
        public static T DebugUtilityGetRequiredComponent<T>(this Component component) where T : Component
        { 
            /*
            Cách dùng:
            gameObject.DebugUtilityGetRequiredComponent<T>()
            this.DebugUtilityGetRequiredComponent<T>()
            otherTransform.DebugUtilityGetRequiredComponent<T>()
            */
            
            var comp = component.GetComponent<T>();
            if (comp == null)
            {
                Debug.LogError($"[{component.GetType().Name}] Missing required component <{typeof(T).Name}> on {component.gameObject.name}");
            }
            return comp;
        }

     
        public static void DrawTargetIndicator(Vector3 position, Color color, float size = 0.5f)
        {
            if (!IsDebugEnabled) return;

            // Draw a cross (duration: 0.1s to persist across frames)
            Debug.DrawLine(position + Vector3.up * size, position + Vector3.down * size, color, 0.1f);
            Debug.DrawLine(position + Vector3.left * size, position + Vector3.right * size, color, 0.1f);

            // Draw a circle (simplified)
            int segments = 16;
            float angleStep = 360f / segments;
            Vector3 prevPoint = position + new Vector3(size, 0, 0);
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 nextPoint = position + new Vector3(Mathf.Cos(angle) * size, Mathf.Sin(angle) * size, 0);
                Debug.DrawLine(prevPoint, nextPoint, color, 0.1f);
                prevPoint = nextPoint;
            }
        }
        /// <summary>
        /// Log kết quả quét vật lý (Raycast/Circle/Box) ra console để debug.
        /// </summary>
        public static void LogPhysicsResults(string ownerName, string scanType, Vector2 origin, int hitCount, RaycastHit2D[] results, System.Collections.Generic.List<string> tagsToDetect)
        {
            if (!IsDebugEnabled) return;

            string color = scanType.Contains("Circle") ? "magenta" : "cyan";
            Debug.Log($"<color={color}>--- [{scanType} Detect: {ownerName}] ---</color>");
            Debug.Log($"Origin: {origin} | Actual Hits: {hitCount}");

            for (int i = 0; i < hitCount; i++)
            {
                if (results[i].collider != null)
                {
                    LogSingleHit(i, results[i].collider.gameObject, tagsToDetect);
                }
            }
            Debug.Log($"<color={color}>----------------------------------------------------</color>");
        }

        public static void LogPhysicsResults(string ownerName, string scanType, Vector2 origin, int hitCount, Collider2D[] results, System.Collections.Generic.List<string> tagsToDetect)
        {
            if (!IsDebugEnabled) return;

            string color = scanType.Contains("Circle") ? "magenta" : "cyan";
            Debug.Log($"<color={color}>--- [{scanType} Detect: {ownerName}] ---</color>");
            Debug.Log($"Origin: {origin} | Actual Hits: {hitCount}");

            for (int i = 0; i < hitCount; i++)
            {
                if (results[i] != null)
                {
                    LogSingleHit(i, results[i].gameObject, tagsToDetect);
                }
            }
            Debug.Log($"<color={color}>----------------------------------------------------</color>");
        }

        private static void LogSingleHit(int index, GameObject obj, System.Collections.Generic.List<string> tagsToDetect)
        {
            bool isTarget = tagsToDetect != null && (tagsToDetect.Contains(obj.tag) || (obj.transform.parent != null && tagsToDetect.Contains(obj.transform.parent.tag)));
            string highlight = isTarget ? "<b><color=lime>[MATCH]</color></b>" : "<color=white>[IGNORE]</color>";
            Debug.Log($"{highlight} [{index}] Name: {obj.name} | Tag: {obj.tag} | Layer: {LayerMask.LayerToName(obj.layer)}");
        }
    }
}



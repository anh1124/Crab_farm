using Script.Game.Item.Coin;
using UnityEngine;

namespace Script.Utils
{
    public static class DebugUtility
    {
        public static bool IsDebugEnabled = true;

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

     
    }
}



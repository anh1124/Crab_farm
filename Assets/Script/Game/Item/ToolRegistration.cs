using Script.Game.Mobs;
using UnityEngine;

namespace Script.Game.Item
{
    public class ToolRegistration : MonoBehaviour
    {
        //thêm cái này vào tool
        private void OnEnable()
        {
            // Register with VillagerManager when enabled/spawned
            if (VillagerManager.Instance != null)
            {
                VillagerManager.Instance.RegisterTool(gameObject);
            }
            else
            {
                Debug.LogError("[ToolRegistration] VillagerManager instance not found!");
            }
        }

        private void OnDisable()
        {
            // Unregister when disabled/destroyed
            if (VillagerManager.Instance != null)
            {
                VillagerManager.Instance.UnregisterTool(gameObject);
            }
        }
    }
} 
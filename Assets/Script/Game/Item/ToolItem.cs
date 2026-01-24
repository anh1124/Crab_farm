using UnityEngine;
using Script.Game.Mobs;

namespace Script.Game.Item
{
    public enum ToolType { Bow, Hammer, Scythe }

    /// <summary>
    /// Unified tool system that replaces ToolStats + ToolRegistration.
    /// Each tool has a type that maps to a specific NPCRole.
    /// Auto-registers with VillagerManager on spawn/enable.
    /// </summary>
    public class ToolItem : MonoBehaviour
    {
        [Header("Tool Configuration")]
        [Tooltip("Type of tool: Bow = Archer, Hammer = Worker, Scythe = Farmer")]
        public ToolType type;

        [Header("Pickup State")]
        [Tooltip("True when a villager has picked up this tool")]
        public bool picked = false;

        [Tooltip("Reference to the villager who picked up this tool")]
        public GameObject pickedBy;

        private void OnEnable()
        {
            // Validate tool type
            if (!System.Enum.IsDefined(typeof(ToolType), type))
            {
                Debug.LogError($"[ToolItem] {gameObject.name} has invalid ToolType: {type}. Tool will not be registered.");
                return;
            }

            // Tool spawned or enabled => register with VillagerManager
            if (VillagerManager.Instance != null)
            {
                VillagerManager.Instance.RegisterTool(this);
            }
            else
            {
                Debug.LogError("[ToolItem] VillagerManager instance not found! Destroying tool to prevent orphaned tools.");
                Destroy(gameObject);
            }
        }

        private void OnDisable()
        {
            // Tool disabled or destroyed => unregister from VillagerManager
            if (VillagerManager.Instance != null)
            {
                VillagerManager.Instance.UnregisterTool(this);
            }
        }
    }
}

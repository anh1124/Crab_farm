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
            if (Script.Manager.MobsManager.ToolManager.Instance != null)
            {
                Script.Manager.MobsManager.ToolManager.Instance.RegisterTool(this);
            }
        }

        private void OnDisable()
        {
            if (Script.Manager.MobsManager.ToolManager.Instance != null)
            {
                Script.Manager.MobsManager.ToolManager.Instance.UnregisterTool(this);
            }
        }
    }
}

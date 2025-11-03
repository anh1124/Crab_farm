using Script.Game.Other.Nature;
using UnityEngine;

namespace Script.Game.Other.Nature
{   
    /// <summary>
    /// Main component for tree. Logic and data are handled by NatureTreeBehavior.
    /// </summary>
    public class NatureTreeStatus : MonoBehaviour
    {
        [Header("Tree Components")]
        [SerializeField] private NatureTreeBehavior behavior;
        
        private void Awake()
        {
            // Get or add required components
            if (behavior == null)
                behavior = GetComponent<NatureTreeBehavior>();
            if (behavior == null)
                behavior = gameObject.AddComponent<NatureTreeBehavior>();
        }
        
        // Public getters for external access
        public NatureTreeBehavior Behavior => behavior;
        
        // Convenience methods that delegate to behavior
        public void SetWorkProgress(int currentWorkProgress) => behavior?.SetWorkProgress(currentWorkProgress);
        public void TakeDame(int dame) => behavior?.TakeDame(dame);
        public int CurrentWorkProgress => behavior?.CurrentWorkProgress ?? 0;
        public int MaxWorkProgress => behavior?.MaxWorkProgress ?? 0;
        public bool IsReadyToChop => behavior?.IsReadyToChop ?? false;
        public string TreeType => behavior?.TreeType ?? "Default";
        public int TreeLevel => behavior?.TreeLevel ?? 1;
    }
}
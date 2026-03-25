using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Core
{
    public class DenizenAnimationEvents : MonoBehaviour
    {
        private DenizenCore _core;

        private void Awake()
        {
            _core = GetComponent<DenizenCore>();
        }

        // 1. Archer Events
        // ---------------------------------------------------------
        
        public void OnDrawComplete()
        {
            if (_core != null)
            {
                _core.OnAnimationEvent("DrawComplete");
            }
        }

        public void OnArrowRelease()
        {
            if (_core != null)
            {
                _core.OnAnimationEvent("ArrowRelease");
            }
        }
    }
}

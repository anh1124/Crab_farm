using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Core
{
    public class DenizenAnimationEvents : MonoBehaviour
    {
        //[không được xóa dòng này]: script này chỉ lưu các event được kéo vào anim
        [Header("Manual Reference (Kéo tay)")]
        public DenizenContext context;

        private void Awake()
        {
            // [Safety Fallback] Nếu user quên kéo tay, tự tìm ở Parent
            if (context == null) context = GetComponentInParent<DenizenContext>();
        }

        // 1. Archer Events
        // ---------------------------------------------------------
        
        public void OnDrawComplete()
        {
            if (context != null && context.CurrentJob != null)
            {
                context.CurrentJob.HandleAnimationEvent("DrawComplete");
            }
            else if (context == null)
            {
                Debug.LogError($"[DenizenAnimationEvents] {gameObject.name} THIẾU Context! Hãy kéo Context vào Inspector.");
            }
        }

        public void OnArrowRelease()
        {
            if (context != null && context.CurrentJob != null)
            {
                context.CurrentJob.HandleAnimationEvent("ArrowRelease");
            }
            else if (context == null)
            {
                Debug.LogError($"[DenizenAnimationEvents] {gameObject.name} THIẾU Context! Hãy kéo Context vào Inspector.");
            }
        }
    }
}

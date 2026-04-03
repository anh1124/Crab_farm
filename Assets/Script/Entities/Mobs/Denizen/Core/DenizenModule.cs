using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Core
{
    /// <summary>
    /// Base class for all Denizen Modules/Handlers.
    /// Helps splitting God Object jobs into smaller, specialized components.
    /// </summary>
    public abstract class DenizenModule
    {
        protected DenizenContext Context;

        public DenizenModule(DenizenContext context)
        {
            Context = context;
        }

        public virtual void Enable() { }
        public virtual void Disable() { }
    }
}

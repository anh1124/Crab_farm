using UnityEngine;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Core
{
    public abstract class DenizenItemHandler
    {
        protected DenizenContext Context;

        public DenizenItemHandler(DenizenContext context)
        {
            Context = context;
        }

        public abstract bool HandleItem(GameObject item);
    }
}

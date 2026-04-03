using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Core
{
    public class DenizenSensor : DenizenModule
    {
        public DenizenSensor(DenizenContext context) : base(context) { }

        public bool IsPlayerInList(List<GameObject> objects)
        {
            foreach (var go in objects)
                if (go != null && go.CompareTag(GameTags.Player)) return true;
            return false;
        }
    }
}

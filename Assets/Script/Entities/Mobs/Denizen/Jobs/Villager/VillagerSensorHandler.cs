using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerSensorHandler : DenizenModule
    {
        private VillagerJob _job;

        public VillagerSensorHandler(DenizenContext context, VillagerJob job) : base(context) 
        { 
            _job = job;
        }

        public override void Enable()
        {
            if (Context.RayCaster != null)
            {
                Context.RayCaster.OnDetectedObjectsChanged -= OnObjectsDetected;
                Context.RayCaster.OnDetectedObjectsChanged += OnObjectsDetected;
                Context.RayCaster.tagsToDetect = new List<string>(TargetLists.FleeTargets);
                Context.RayCaster.raycastDistance = Context.Status.detectRangeRaycast;
                Context.RayCaster.IsActive = true;
            }
        }

        public override void Disable()
        {
            if (Context.RayCaster != null)
            {
                Context.RayCaster.OnDetectedObjectsChanged -= OnObjectsDetected;
                Context.RayCaster.IsActive = false;
            }
        }

        public void OnObjectsDetected(List<GameObject> objects)
        {
            if (objects == null) return;
            bool detected = objects.Count > 0;
            if (detected) Context.Status.isFearPending = true;
        }
    }
}

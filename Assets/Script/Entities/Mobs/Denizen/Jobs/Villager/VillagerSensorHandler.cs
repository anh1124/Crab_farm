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
                Context.RayCaster.tagsToDetect = new List<string>(TargetLists.VillagerTargets); // Enemy, Player
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
            if (objects == null || objects.Count == 0 || !_job.enabled) return;

            // Đang đi trả coin thì không ngắt
            if (_job.CurrentState == _job.DropCoinState) return;

            bool enemyDetected = false;
            bool playerDetected = false;

            foreach (var obj in objects)
            {
                if (TargetLists.FleeTargets.Contains(obj.tag)) enemyDetected = true;
                if (obj.tag == GameTags.Player) playerDetected = true;
            }

            if (enemyDetected) 
            {
                Context.Status.isFearPending = true;
            }
            else if (playerDetected && Context.CoinSharing != null && Context.CoinSharing.CanDropCoin())
            {
                _job.ChangeState(_job.DropCoinState);
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public class ArcherSensorHandler : DenizenModule
    {
        private ArcherJob _job;
        private ArcherTargetSelector _targetSelector;

        public ArcherSensorHandler(DenizenContext context, ArcherJob job, ArcherTargetSelector targetSelector) : base(context) 
        { 
            _job = job;
            _targetSelector = targetSelector;
        }

        public override void Enable()
        {
            if (Context.CircleDetect != null)
            {
                Context.CircleDetect.OnDetectedObjectsChanged -= OnObjectsDetected;
                Context.CircleDetect.OnDetectedObjectsChanged += OnObjectsDetected;
                Context.CircleDetect.tagsToDetect = new List<string>(TargetLists.ArcherTargets) { GameTags.Player, GameTags.Coin };
                Context.CircleDetect.detectRadius = Context.Status.archerDetectRange;
                Context.CircleDetect.IsActive = true;
            }
        }

        public override void Disable()
        {
            if (Context.CircleDetect != null)
            {
                Context.CircleDetect.OnDetectedObjectsChanged -= OnObjectsDetected;
            }
        }

        public void OnObjectsDetected(List<GameObject> objects)
        {
            if (!_job.enabled || _job.CurrentState == null || _job.CurrentState == _job.DropCoinState) return;
            
            GameObject target = _targetSelector.SelectCombatTarget(objects);
            if (target != null)
            {
                Context.Status.currentTarget = target;
                if (Context.GuardManager != null && target.CompareTag(GameTags.Enemy))
                    Context.GuardManager.RegisterEnemy(target);
                return;
            }
            Context.Status.currentTarget = null;
            
            if (IsPlayerInList(objects) && Context.CoinSharing != null && Context.CoinSharing.CanDropCoin())
                _job.ChangeState(_job.DropCoinState);
        }

        public bool IsPlayerInList(List<GameObject> objects)
        {
            foreach (var go in objects)
                if (go != null && go.CompareTag(GameTags.Player)) return true;
            return false;
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Game.Item.Coin;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Vagrant
{
    public class VagrantSensorHandler : DenizenModule
    {
        private VagrantJob _job;

        public VagrantSensorHandler(DenizenContext context, VagrantJob job) : base(context) 
        { 
            _job = job;
        }

        public override void Enable()
        {
            if (Context.RayCaster != null)
            {
                Context.RayCaster.OnDetectedObjectsChanged -= OnObjectsDetected;
                Context.RayCaster.OnDetectedObjectsChanged += OnObjectsDetected;
                Context.RayCaster.tagsToDetect = new List<string>
                {
                    GameTags.Enemy,
                    GameTags.Coin
                };
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
            if (!_job.enabled || objects == null) return;

            foreach (var obj in objects)
            {
                if (obj == null) continue;

                // --- Enemy: ưu tiên cao nhất ---
                if (obj.CompareTag(GameTags.Enemy))
                {
                    Context.Status.isFearPending = true;
                    Context.Status.fearTimer = Context.Status.maxFearDuration;
                    return;
                }

                // --- Coin: chỉ nếu không đang sợ ---
                if (obj.CompareTag(GameTags.Coin) && _job.CurrentState != _job.FearState)
                {
                    var coin = obj.GetComponent<PhysicalItem>();
                    if (coin == null || !coin.IsAvailable() || !coin.denizensPickupAble)
                        continue;

                    if (Context.Status.coinTarget != obj)
                    {
                        Context.Status.coinTarget = obj;
                    }
                    if (_job.CurrentState != _job.GetCoinState)
                        _job.ChangeState(_job.GetCoinState);
                    return;
                }
            }
        }
    }
}

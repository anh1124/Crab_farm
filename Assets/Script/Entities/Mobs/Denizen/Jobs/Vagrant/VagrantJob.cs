using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;
using Script.Data_Model;

namespace Script.Game.Mobs.Denizen.Jobs.Vagrant
{
    public class VagrantJob : DenizenJob
    {
        // States
        public VagrantIdleState IdleState { get; private set; }
        public VagrantFearState FearState { get; private set; }
        public VagrantGetCoinState GetCoinState { get; private set; }

        protected override void Awake()
        {
            PreAllocateStates();
        }

        protected override void PreAllocateStates()
        {
            IdleState = new VagrantIdleState(this);
            FearState = new VagrantFearState(this);
            GetCoinState = new VagrantGetCoinState(this);
        }

        protected override DenizenState GetInitialState()
        {
            return IdleState;
        }

        protected override void OnEnterJob()
        {
            if (Context.RayCaster != null)
            {
                Context.RayCaster.OnDetectedObjectsChanged -= OnObjectsDetected;
                Context.RayCaster.OnDetectedObjectsChanged += OnObjectsDetected;
                Context.RayCaster.tagsToDetect = TargetLists.VagrantTargets;
                Context.RayCaster.SetRayLength(Context.Status.detectRangeRaycast); // Thiết lập cứng tầm quét
                Context.RayCaster.isActive = true;
                Context.RayCaster.isDrawRay = true; // Bật gizmo Raycast cho Vagrant
            }
        }

        protected override void OnExitJob()
        {
            if (Context.RayCaster != null)
            {
                Context.RayCaster.OnDetectedObjectsChanged -= OnObjectsDetected;
                Context.RayCaster.isActive = false;
            }
        }

        public override void OnObjectsDetected(List<GameObject> objects)
        {
            if (!enabled || CurrentState == null) return;

            GameObject enemy = null;
            GameObject coin = null;

            foreach (var go in objects)
            {
                if (go == null) continue;
                if (go.CompareTag(GameTags.Enemy)) { enemy = go; break; } // Enemy ưu tiên tuyệt đối
                if (go.CompareTag(GameTags.Coin)) coin = go;
            }

            if (enemy != null)
            {
                if (CurrentState == FearState)
                {
                    // Đang sợ mà vẫn thấy enemy -> gia hạn thêm thời gian sợ (theo ctx.Status.maxFearDuration)
                    FearState.RefreshFear(Context);
                }
                else
                {
                    ChangeState(FearState);
                }
            }
            else if (coin != null && CurrentState == IdleState)
            {
                // Chỉ nhặt coin khi đang Idle (không nhặt khi đang sợ)
                Context.Status.coinTarget = coin;
                ChangeState(GetCoinState);
            }
        }

        public override bool HandleItem(GameObject item)
        {
            var coinScript = item.GetComponent<Script.Game.Item.Coin.PhysicalItem>();
            if (coinScript == null) return false;

            if (coinScript.itemType == Script.Game.Item.Coin.ItemType.Coin && coinScript.denizensPickupAble)
            {
                if (Context.Status.IsFull) return false;

                // Khi Vagrant nhặt xu, họ sẽ đổi đời thành Villager (Thông qua Core.OnCoinChangedHandler)
                Context.Status.AddCoin(1);
                return true;
            }
            return false;
        }
    }
}

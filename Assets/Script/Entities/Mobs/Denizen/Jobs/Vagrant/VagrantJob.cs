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

        public override void UpdateBehavior()
        {
            // 1. Gọi base để thực hiện quét tia/vòng tròn (sẽ kích hoạt OnObjectsDetected nếu thấy đồ)
            base.UpdateBehavior();

            // 2. CHỐT CHẶN FEAR: Kiểm tra cờ sợ hãi (có thể bật do sát thương hoặc cảm biến khác)
            if (Context != null && Context.Status != null && Context.Status.isFearPending)
            {
                Context.Status.isFearPending = false;
                if (CurrentState != FearState)
                {
                    ChangeState(FearState);
                }
                else
                {
                    FearState.RefreshFear(Context);
                }
                return; // Đã đổi state hoặc gia hạn fear -> Không xử lý tiếp bên dưới
            }

            // 3. TASK VALIDATION: Kiểm tra tính hợp lệ của nhiệm vụ nhặt xu
            if (CurrentState == GetCoinState)
            {
                if (Context.Status.coinTarget == null || !Context.Status.coinTarget.gameObject.activeInHierarchy)
                {
                    Context.Status.coinTarget = null;
                    ChangeState(IdleState);
                }
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
                // Thay vì tự ChangeState ở đây, ta gạt cờ Fear để UpdateBehavior xử lý tập trung
                Context.Status.isFearPending = true;
            }
            else if (coin != null && CurrentState == IdleState)
            {
                // Chỉ nhặt coin khi đang Idle (không nhặt khi đang sợ) - Giữ logic này vì nó đặc thù cho Vagrant
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

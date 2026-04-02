using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;
using Script.Manager.WorldManager;
using Script.Game.Mobs.MobsManager;
using Script.Interfaces;
using Script.Data_Model.Interfaces;
using Script.Manager.GamePlayManager;
using Script.Game.Mobs.Subjects;
using Script.Game.Item.Coin;

namespace Script.Game.Mobs.Denizen.Jobs.Vagrant
{
    public class VagrantJob : DenizenJob, INPCBehavior, IDamageable
    {
        // States
        public VagrantIdleState   IdleState    { get; private set; }
        public VagrantFearState   FearState    { get; private set; }
        public VagrantGetCoinState GetCoinState { get; private set; }

        protected void Awake()
        {
            if (Context == null) Context = GetComponentInParent<DenizenContext>();
            IdleState    = new VagrantIdleState();
            FearState    = new VagrantFearState();
            GetCoinState = new VagrantGetCoinState();
        }

        #region Brain & Transitions
        public override void UpdateBehavior()
        {
            if (Context == null) return;

            // [DETECTOR] — Gọi scanner thủ công, giống cách Enemy làm
            if (Context.RayCaster != null && Context.RayCaster.IsActive)
                Context.RayCaster.DrawRay();

            if (Context.IsSkillCasting) return;

            // [FEAR GATE] — Cổng ưu tiên cao nhất
            if (Context.Status.isFearPending && CurrentState != FearState)
            {
                ChangeState(FearState);
                return;
            }

            CurrentState?.UpdateBehavior(Context);
        }

        public void ChangeState(DenizenState newState) => DoChangeState(newState);

        private void Update()
        {
            CurrentState?.Update(Context);
        }

        public void SetMoveTarget(Vector3 pos, float speed = -1f)    => Context.Movement?.SetTarget(pos, speed);
        public void StopMovement()               => Context.Movement?.Stop();

        public bool HasReachedPosition(Vector3 pos, float threshold = 0.2f) =>
            Mathf.Abs(transform.position.x - pos.x) < threshold;
        #endregion

        #region Lifecycle
        private void OnEnable()
        {
            if (Context != null)
            {
                Context.ResetDetectors(); // Swab previous job's detectors
                if (Context.RayCaster != null)
                {
                    Context.RayCaster.OnDetectedObjectsChanged -= OnObjectsDetected;
                    Context.RayCaster.OnDetectedObjectsChanged += OnObjectsDetected;
                    // Vagrant quét Enemy VÀ Coin
                    Context.RayCaster.tagsToDetect = new List<string>
                    {
                        GameTags.Enemy,
                        GameTags.Coin
                    };
                    Context.RayCaster.raycastDistance = Context.Status.detectRangeRaycast;
                    Context.RayCaster.IsActive = true;
                }
                if (Context.Status != null)
                {
                    Context.Status.OnCoinChanged += OnCoinChangedHandler;
                    Context.Status.isMovementAllowed = true;
                    Context.Status.isMoving = false;
                }
            }

            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Register(this);

            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.SubscribeToHourlyEvent(6, OnShiftChange);
                GameTimeManager.Instance.SubscribeToHourlyEvent(18, OnShiftChange);
            }

            ChangeState(IdleState);
        }

        private void OnDisable()
        {
            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Unregister(this);

            if (Context != null)
            {
                if (Context.RayCaster != null)
                {
                    Context.RayCaster.OnDetectedObjectsChanged -= OnObjectsDetected;
                    Context.RayCaster.IsActive = false;
                }
                if (Context.Status != null)
                    Context.Status.OnCoinChanged -= OnCoinChangedHandler;
            }

            if (GameTimeManager.Instance != null)
            {
                GameTimeManager.Instance.UnsubscribeFromHourlyEvent(6, OnShiftChange);
                GameTimeManager.Instance.UnsubscribeFromHourlyEvent(18, OnShiftChange);
            }

            CurrentState?.Exit(Context);
            CurrentState = null;

            if (Context?.Status != null)
            {
                Context.Status.isMoving = false;
                Context.Status.coinTarget = null;
            }
        }

        private void OnShiftChange() { if (CurrentState == IdleState) ChangeState(IdleState); }
        #endregion

        #region Interface Handlers
        public override void ReceiveDamage(float damage, GameObject instigator)
        {
            if (damage <= 0) return;
            Context.Status.isFearPending = true;
            // Fear gate trong UpdateBehavior() sẽ xử lý tiếp
        }

        /// <summary>
        /// Xử lý kết quả detect (Enemy hoặc Coin).
        /// Priority: Enemy > Coin — giống legacy VagrantBehavior.
        /// </summary>
        public void OnObjectsDetected(List<GameObject> objects)
        {
            if (!enabled || objects == null) return;

            foreach (var obj in objects)
            {
                if (obj == null) continue;

                // --- Enemy: ưu tiên cao nhất ---
                if (obj.CompareTag(GameTags.Enemy))
                {
                    Context.Status.isFearPending = true;
                    Context.Status.fearTimer = Context.Status.maxFearDuration;
                    // Fear gate trong UpdateBehavior() sẽ trigger ChangeState
                    return;
                }

                // --- Coin: chỉ nếu không đang sợ ---
                if (obj.CompareTag(GameTags.Coin) && CurrentState != FearState)
                {
                    if (Context.Status.coinTarget != obj)
                    {
                        Context.Status.coinTarget = obj;
                    }
                    if (CurrentState != GetCoinState)
                        ChangeState(GetCoinState);
                    return;
                }
            }
        }

        private void OnCoinChangedHandler(int newCoins)
        {
            if (newCoins > 0)
                Context.RoleController.ChangeRole(NPCRole.Villager);
        }

        /// <summary>
        /// Khi Vagrant đi vào trigger của Coin — lượm coin và đổi nghề.
        /// </summary>
        public override bool HandleItem(GameObject item)
        {
            var coin = item.GetComponent<PhysicalItem>();
            if (coin != null && coin.itemType == ItemType.Coin && coin.IsAvailable())// vagrant luôn có thể nhặt coin (không cần điều kiện check túi)
            {
                Context.Status.coinTarget = null;
                // Thêm xu vào túi cư dân
                Context.Status.AddCoin(1);

                // LootPickupTrigger sẽ tự set pickedBy sau khi HandleItem trả true
                // Chỉ cần đổi nghề — dùng Context.RoleController (root NPC)
                Context.RoleController?.ChangeRole(NPCRole.Villager);
                return true;
            }
            return false;
        }
        #endregion
    }
}

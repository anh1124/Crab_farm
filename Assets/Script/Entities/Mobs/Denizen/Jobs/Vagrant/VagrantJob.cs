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
    public class VagrantJob : DenizenJob, INPCBehavior
    {
        public VagrantIdleState   IdleState    { get; private set; }
        public VagrantFearState   FearState    { get; private set; }
        public VagrantGetCoinState GetCoinState { get; private set; }
        
        private VagrantItemHandler _itemHandler;
        private DenizenSchedule _schedule;
        private VagrantSensorHandler _sensor;
        private bool _isInitialized;

        protected void Awake()
        {
            InitializeBrain();
        }

        private void InitializeBrain()
        {
            if (_isInitialized) return;

            if (Context == null) Context = GetComponentInParent<DenizenContext>();
            IdleState    = new VagrantIdleState();
            FearState    = new VagrantFearState();
            GetCoinState = new VagrantGetCoinState();

            // Initialize minimal handlers for safety during early activation (e.g. Loading)
            if (Context != null)
            {
                _itemHandler = new VagrantItemHandler(Context);
                _schedule = new DenizenSchedule(Context);
                _sensor = new VagrantSensorHandler(Context, this);
            }
            _isInitialized = true;
        }

        protected override void OnContextInjected()
        {
            InitializeBrain();
            _itemHandler = new VagrantItemHandler(Context);
            _schedule = new DenizenSchedule(Context);
            _sensor = new VagrantSensorHandler(Context, this);
        }

        #region Brain & Transitions
        public override void UpdateBehavior()
        {
            InitializeBrain();
            if (Context == null || Context.Status == null) return;

            // [DETECTOR] — Gọi scanner thủ công, giống cách Enemy làm
            if (Context.RayCaster != null && Context.RayCaster.IsActive)
            {
                 // Đảm bảo tag luôn đúng nếu bị Reset() lung tung
                 if (Context.RayCaster.tagsToDetect.Count == 0) _sensor.Enable();
                 Context.RayCaster.DrawRay();
            }

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
            InitializeBrain();
            if (Context != null)
            {
                Context.ResetDetectors(); // Swab previous job's detectors
                _sensor.Enable();
                
                if (Context.Status != null)
                {
                    Context.Status.OnCoinChanged += OnCoinChangedHandler;
                    Context.Status.CanMove = true;
                    Context.Status.isMoving = false;
                }
            }

            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Register(this);

            var vm = FindObjectOfType<VagrantManager>();
            if (vm != null) vm.RegisterVagrant(gameObject);

            var gtm = FindObjectOfType<GameTimeManager>();
            if (gtm != null)
            {
                _schedule.AddSchedule(6, OnShiftChange);
                _schedule.AddSchedule(18, OnShiftChange);
                _schedule.Enable();
            }

            ChangeState(IdleState);
        }

        private void OnDisable()
        {
            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Unregister(this);

            var vm = FindObjectOfType<VagrantManager>();
            if (vm != null) vm.UnregisterVagrant(gameObject);

            if (Context != null)
            {
                _sensor.Disable();
                
                if (Context.Status != null)
                    Context.Status.OnCoinChanged -= OnCoinChangedHandler;
            }

            var gtm = FindObjectOfType<GameTimeManager>();
            if (gtm != null)
            {
                _schedule.Disable();
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

        private void OnCoinChangedHandler(int newCoins)
        {
            _itemHandler.OnCoinChanged(newCoins);
        }

        public void OnObjectsDetected(List<GameObject> objects)
        {
            _sensor.OnObjectsDetected(objects);
        }

        /// <summary>
        /// Khi Vagrant đi vào trigger của Coin — lượm coin và đổi nghề.
        /// </summary>
        public override bool HandleItem(GameObject item)
        {
            return _itemHandler.HandleItem(item);
        }
        #endregion
    }
}

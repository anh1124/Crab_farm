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

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerJob : DenizenJob, INPCBehavior, IDamageable
    {
        // States
        public VillagerIdleState    IdleState    { get; private set; }
        public VillagerToTownState  ToTownState  { get; private set; }
        public VillagerRunToolState RunToolState { get; private set; }
        public VillagerFearState    FearState    { get; private set; }

        private VillagerItemHandler _itemHandler;
        private VillagerSensorHandler _sensor;

        protected void Awake()
        {
            if (Context == null) Context = GetComponentInParent<DenizenContext>();
            IdleState    = new VillagerIdleState();
            ToTownState  = new VillagerToTownState();
            RunToolState = new VillagerRunToolState();
            FearState    = new VillagerFearState();

            _itemHandler = new VillagerItemHandler(Context);
            _sensor = new VillagerSensorHandler(Context, this);
        }

        #region Brain & Transitions
        public override void UpdateBehavior()
        {
            if (Context == null) return;

            // [DETECTOR] — Gọi scanner thủ công (giống Enemy pattern)
            if (Context.RayCaster != null && Context.RayCaster.IsActive)
                Context.RayCaster.DrawRay();

            if (Context.IsSkillCasting) return;

            // [FEAR GATE] — Cổng ưu tiên cao nhất, chỉ dùng isFearPending
            if (Context.Status.isFearPending && CurrentState != FearState)
            {
                ChangeState(FearState);
                return;
            }

            // [NIGHT GATE] — Ban đêm về thành phố
            if (Context.TimeManager != null && !Context.TimeManager.IsDaytime)
            {
                if (!WorldUtility.IsInPlazaZone(transform.position.x) && CurrentState != ToTownState)
                {
                    ChangeState(ToTownState);
                    return;
                }
            }

            CurrentState?.UpdateBehavior(Context);
        }

        public void ChangeState(DenizenState newState) => DoChangeState(newState);

        private void Update() => CurrentState?.Update(Context);

        public void SetMoveTarget(Vector3 pos, float speed = -1f) => Context.Movement?.SetTarget(pos, speed);

        public bool HasReachedPosition(Vector3 pos, float threshold = 0.2f) =>
            Mathf.Abs(transform.position.x - pos.x) < threshold;
        #endregion

        #region Lifecycle
        private void OnEnable()
        {
            if (Context != null)
            {
                Context.ResetDetectors(); // Swab previous job's detectors
                _sensor.Enable();
            }

            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Register(this);
            if (DenizenManager.Instance != null) DenizenManager.Instance.RegisterDenizen(gameObject);

            ChangeState(Context.Status != null && Context.Status.isFearPending ? (DenizenState)FearState : IdleState);
        }

        private void OnDisable()
        {
            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Unregister(this);
            if (DenizenManager.Instance != null) DenizenManager.Instance.UnregisterDenizen(gameObject); // Bug fix: was Register

            if (Context != null)
                _sensor.Disable();

            CurrentState?.Exit(Context);
            CurrentState = null;
        }
        #endregion

        #region Interface & Core Handlers
        public override void ReceiveDamage(float damage, GameObject instigator)
        {
            if (damage <= 0) return;
            // Chỉ set flag — Fear gate trong UpdateBehavior() xử lý transition
            Context.Status.isFearPending = true;
        }

        public void OnObjectsDetected(List<GameObject> objects)
        {
            _sensor.OnObjectsDetected(objects);
        }

        public override void HandleAnimationEvent(string eventName) { }

        public override bool HandleItem(GameObject item)
        {
            return _itemHandler.HandleItem(item);
        }
        #endregion

        #region ToolManager Support
        public void TransitionToIdle() => ChangeState(IdleState);

        public void AssignTool(PhysicalItem tool)
        {
            if (Context?.Status != null)
            {
                Context.Status.currentTool = tool;
                ChangeState(RunToolState);
            }
        }
        #endregion
    }
}

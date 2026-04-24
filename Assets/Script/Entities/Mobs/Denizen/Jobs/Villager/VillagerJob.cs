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
    public class VillagerJob : DenizenJob, INPCBehavior
    {
        // States
        public VillagerIdleState    IdleState    { get; private set; }
        public VillagerToTownState  ToTownState  { get; private set; }
        public VillagerRunToolState RunToolState { get; private set; }
        public VillagerFearState    FearState    { get; private set; }
        public VillagerDropCoinState DropCoinState { get; private set; }
        
        private VillagerItemHandler _itemHandler;
        private VillagerSensorHandler _sensor;
        private bool _isInitialized;

        protected void Awake()
        {
            InitializeBrain();
        }

        private void InitializeBrain()
        {
            if (_isInitialized) return;

            if (Context == null) Context = GetComponentInParent<DenizenContext>();
            IdleState    = new VillagerIdleState();
            ToTownState  = new VillagerToTownState();
            RunToolState = new VillagerRunToolState();
            FearState    = new VillagerFearState();
            DropCoinState = new VillagerDropCoinState();

            // Initialize minimal handlers for safety during early activation (e.g. Loading)
            if (Context != null)
            {
                _itemHandler = new VillagerItemHandler(Context);
                _sensor = new VillagerSensorHandler(Context, this);
            }
            _isInitialized = true;
        }

        protected override void OnContextInjected()
        {
            InitializeBrain();
            _itemHandler = new VillagerItemHandler(Context);
            _sensor = new VillagerSensorHandler(Context, this);
        }

        #region Brain & Transitions
        public override void UpdateBehavior()
        {
            InitializeBrain();
            if (Context == null) return;

            // [WATCHDOG] Tự động recovery nếu detector bị tắt bất thường
            if (Context.RayCaster != null && !Context.RayCaster.IsActive)
            {
                if (Context.Status != null && Context.Status.debugDenizen)
                    Debug.LogWarning($"[VillagerJob] Detector bị tắt, đang tự động bật lại...");
                _sensor.Enable();
            }

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
                // Đảm bảo detector references tồn tại trước khi sử dụng
                if (Context.RayCaster == null)
                    Context.RayCaster = GetComponentInChildren<RayCastHelper>(true);
                
                Context.ResetDetectors(); // Swab previous job's detectors
                _sensor.Enable();
                
                // Force scan ngay lập tức
                if (Context.RayCaster != null && Context.RayCaster.IsActive)
                    Context.RayCaster.DrawRay();
            }

            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Register(this);
            var dm = FindObjectOfType<DenizenManager>();
            if (dm != null) dm.RegisterDenizen(gameObject);

            ChangeState(Context.Status != null && Context.Status.isFearPending ? (DenizenState)FearState : IdleState);
        }

        private void OnDisable()
        {
            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Unregister(this);
            var dm = FindObjectOfType<DenizenManager>();
            if (dm != null) dm.UnregisterDenizen(gameObject); // Bug fix: was Register

            if (Context != null)
            {
                _sensor.Disable();
                
                // Clear fear state và targets để tránh stale data
                if (Context.Status != null)
                {
                    Context.Status.isFearPending = false;
                    Context.Status.currentTarget = null;
                    Context.Status.coinTarget = null;
                }
            }

            CurrentState?.Exit(Context);
            CurrentState = null;
        }
        #endregion

        #region Save/Load State Recovery
        /// <summary>
        /// Được gọi sau khi load từ save để khôi phục state machine.
        /// OnEnable không chạy lại khi load nên cần hàm này để tạo lại state.
        /// </summary>
        public void ResetStateAfterLoad()
        {
            InitializeBrain();
            if (Context?.Status == null) return;
            
            // Đảm bảo detector references tồn tại
            if (Context.RayCaster == null)
                Context.RayCaster = GetComponentInChildren<RayCastHelper>(true);
            
            // Bật lại detectors
            if (Context.RayCaster != null)
            {
                Context.RayCaster.IsActive = true;
                Context.RayCaster.DrawRay();
            }
            
            // Khôi phục state từ enum đã lưu
            var savedState = Context.Status.villagerState;
            DenizenState newState = savedState switch
            {
                Script.Data_Model.Status.DenizenStatus.VillagerState.ToTown => ToTownState,
                Script.Data_Model.Status.DenizenStatus.VillagerState.RunTool => RunToolState,
                Script.Data_Model.Status.DenizenStatus.VillagerState.Fear => FearState,
                Script.Data_Model.Status.DenizenStatus.VillagerState.DropCoin => DropCoinState,
                _ => IdleState
            };
            
            // Chỉ đổi state nếu hiện tại khác hoặc null
            if (CurrentState == null || CurrentState != newState)
            {
                ChangeState(newState);
            }
            
            if (Context.Status.debugDenizen)
                Debug.Log($"[VillagerJob] ResetStateAfterLoad: {newState.GetType().Name} (saved enum: {savedState})");
        }
        #endregion

        #region Interface & Core Handlers
        public override void ReceiveDamage(float damage, GameObject instigator)
        {
            if (damage <= 0) return;
            if (Context == null || Context.Status == null) return;

            // Villager mất 1 coin mỗi lần bị trúng đòn.
            // Khi hết coin -> trở về Vagrant ngay theo kịch bản gameplay.
            if (Context.Status.CurrentCoins > 0)
            {
                Context.Status.SubtractCoin(1);
                global::Spawner.Instance?.DropFromDenizenHit(Context.transform.position);
            }

            if (Context.Status.CurrentCoins <= 0)
            {
                Context.Status.CurrentCoins = 0;
                Context.RoleController?.ChangeRole(NPCRole.Vagrant);
                return;
            }

            // Còn coin thì vào Fear để chạy trốn.
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

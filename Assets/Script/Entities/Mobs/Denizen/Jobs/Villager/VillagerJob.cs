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

        protected void Awake()
        {
            if (Context == null) Context = GetComponentInParent<DenizenContext>();
            IdleState    = new VillagerIdleState();
            ToTownState  = new VillagerToTownState();
            RunToolState = new VillagerRunToolState();
            FearState    = new VillagerFearState();
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
                if (Context.RayCaster != null)
                {
                    Context.RayCaster.OnDetectedObjectsChanged -= OnObjectsDetected;
                    Context.RayCaster.OnDetectedObjectsChanged += OnObjectsDetected;
                    Context.RayCaster.tagsToDetect = new List<string>(TargetLists.FleeTargets);
                    Context.RayCaster.raycastDistance = Context.Status.detectRangeRaycast;
                    Context.RayCaster.IsActive = true;
                }
            }

            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Register(this);
            if (DenizenManager.Instance != null) DenizenManager.Instance.RegisterDenizen(gameObject);

            ChangeState(Context.Status != null && Context.Status.isFearPending ? (DenizenState)FearState : IdleState);
        }

        private void OnDisable()
        {
            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Unregister(this);
            if (DenizenManager.Instance != null) DenizenManager.Instance.UnregisterDenizen(gameObject); // Bug fix: was Register

            if (Context?.RayCaster != null)
                Context.RayCaster.OnDetectedObjectsChanged -= OnObjectsDetected;

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

        /// <summary>
        /// Chỉ set flag khi phát hiện nguy hiểm — không gọi ChangeState trực tiếp.
        /// Job's UpdateBehavior() là cổng duy nhất quyết định chuyển state.
        /// </summary>
        public void OnObjectsDetected(List<GameObject> objects)
        {
            if (objects == null) return;
            bool detected = objects.Count > 0;
            if (detected) Context.Status.isFearPending = true;
            // Không cần set isInDanger riêng — isFearPending là đủ
        }

        public override void HandleAnimationEvent(string eventName) { }

        public override bool HandleItem(GameObject item)
        {
            var pItem = item.GetComponent<PhysicalItem>();
            if (pItem == null || !pItem.IsAvailable()) return false;

            // 1. Chuyển nghề khi nhặt vũ khí/công cụ (Logic Tiến Hóa) — Ưu tiên cao nhất
            if (pItem.itemType == ItemType.Tool_Bow)
            {
                Debug.Log($"[VillagerJob] Picked up Bow! Requesting Archer role. Context={Context != null}, RC={Context?.RoleController != null}");
                item.SetActive(false); // Ẩn vật phẩm ngay lập tức để né double-trigger
                Context.RoleController?.ChangeRole(NPCRole.Archer);
                return true;
            }
            else if (pItem.itemType == ItemType.Tool_Hammer)
            {
                Debug.Log($"[VillagerJob] Picked up Hammer! Requesting Worker role.");
                item.SetActive(false);
                Context.RoleController?.ChangeRole(NPCRole.Worker);
                return true;
            }
            else if (pItem.itemType == ItemType.Tool_Scythe)
            {
                Debug.Log($"[VillagerJob] Picked up Scythe! Requesting Farmer role.");
                item.SetActive(false);
                Context.RoleController?.ChangeRole(NPCRole.Farmer);
                return true;
            }

            // 2. Nếu là Tool mình đang được chỉ định nhặt (Gán từ ToolManager)
            if (pItem == Context.Status.currentTool)
            {
                Debug.Log($"[VillagerJob] Picked up assigned tool: {pItem.itemType}");
                return true;
            }

            // 3. Nếu là Xu (Villager cũng nhặt được xu)
            if (pItem.itemType == ItemType.Coin && !Context.Status.IsFull)
            {
                Context.Status.AddCoin(1);
                return true;
            }

            return false;
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

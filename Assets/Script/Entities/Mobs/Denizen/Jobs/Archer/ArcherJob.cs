using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;

namespace Script.Game.Mobs.Denizen.Jobs.Archer
{
    public interface IArcherAttackState
    {
        void OnDrawComplete(DenizenContext ctx);
        void OnArrowRelease(DenizenContext ctx);
    }

    public class ArcherJob : DenizenJob
    {
        // States
        public ArcherIdleState IdleState { get; private set; }
        public ArcherHuntingState HuntingState { get; private set; }
        public ArcherHoldWallState HoldWallState { get; private set; }
        public ArcherRetreatState RetreatState { get; private set; }
        public ArcherReturnToBaseState ReturnToBaseState { get; private set; }
        public ArcherDropCoinState DropCoinState { get; private set; }

        protected override void Awake()
        {
            PreAllocateStates();
        }

        protected override void PreAllocateStates()
        {
            IdleState = new ArcherIdleState(this);
            HuntingState = new ArcherHuntingState(this);
            HoldWallState = new ArcherHoldWallState(this);
            RetreatState = new ArcherRetreatState(this);
            ReturnToBaseState = new ArcherReturnToBaseState(this);
            DropCoinState = new ArcherDropCoinState(this);
        }

        protected override DenizenState GetInitialState()
        {
            bool isAlarm = Context.GuardManager != null && Context.GuardManager.Alarm.isEnemyAttack;
            bool isDaytime = Context.TimeManager != null && Context.TimeManager.IsDaytime;

            Debug.Log($"[ArcherJob] GetInitialState: GuardManager={Context.GuardManager != null}, TimeManager={Context.TimeManager != null}, isAlarm={isAlarm}, isDaytime={isDaytime}, CircleDetect={Context.CircleDetect != null}");

            if (isAlarm) return ReturnToBaseState;
            return isDaytime ? (DenizenState)HuntingState : HoldWallState;
        }

        protected override void OnEnterJob()
        {
           // Debug.Log($"[ArcherJob] OnEnterJob called. CircleDetect={(Context.CircleDetect != null ? "OK" : "NULL")}");

            if (Context.CircleDetect != null)
            {
                Context.CircleDetect.OnDetectedObjectsChanged -= OnObjectsDetected;
                Context.CircleDetect.OnDetectedObjectsChanged += OnObjectsDetected;
                
                // Thiết lập tag: ArcherTargets cho combat, có thêm Player cho detect
                List<string> detectorTags = new List<string>(TargetLists.ArcherTargets);
                if (!detectorTags.Contains(GameTags.Player)) detectorTags.Add(GameTags.Player);
                
                Context.CircleDetect.tagsToDetect = detectorTags;
                Context.CircleDetect.detectRadius = Context.Status.archerDetectRange; // Thêm dòng này để sync tầm radar
                Context.CircleDetect.isActive = true;
                Context.CircleDetect.isShowGizmoUser = true; // Bật Gizmo Circle
               // Debug.Log($"[ArcherJob] CircleDetect activated. Tags={string.Join(",", Context.CircleDetect.tagsToDetect)}");
            }
            else
            {
                Debug.LogError($"[ArcherJob] CircleDetect is NULL on {gameObject.name}! Check DenizenContext inspector assignment.");
            }
        }

        private List<string> detectorPlusTags(List<string> baseTags)
        {
            return baseTags;
        }

        protected override void OnExitJob()
        {
            if (Context.CircleDetect != null)
            {
                Context.CircleDetect.OnDetectedObjectsChanged -= OnObjectsDetected;
                Context.CircleDetect.isActive = false;
                Context.CircleDetect.isShowGizmoUser = false;
            }
            Context.Status.currentTarget = null;
        }

        public override void OnObjectsDetected(List<GameObject> objects)
        {
            if (!enabled || CurrentState == null) return;

            // 1. Nếu đang DropCoin thì để DropCoin tự xử lý
            if (CurrentState == DropCoinState) return;

            // 2. Ưu tiên Combat Target
            GameObject target = SelectCombatTarget(objects);
            if (target != null)
            {
                Context.Status.currentTarget = target;

                // [LOGIC BÁO ĐỘNG] 
                // Khi phát hiện mục tiêu (Enemy), báo cáo lên GuardManager để chuyển trạng thái "Báo Động",
                // Hệ thống GuardManager sẽ tự động ra hiệu cho tất cả Archer (kể cả người vừa detect)
                // gọi hàm OnGuardManagerStatusChanged(true) -> lùi về HoldWallState hoặc ReturnToBaseState.
                if (Context.GuardManager != null)
                {
                    Context.GuardManager.RegisterEnemy(target);
                }
                return;
            }

            Context.Status.currentTarget = null;

            // 3. Xét Player để DropCoin
            if (IsPlayerInList(objects) && Context.CoinSharing != null && Context.CoinSharing.CanDropCoin())
            {
                if (!IsPlayingAttackAnimation())
                {
                     ChangeState(DropCoinState);
                }
            }
        }

        private GameObject SelectCombatTarget(List<GameObject> objects)
        {
            List<GameObject> validTargets = new List<GameObject>();
            foreach (var go in objects)
            {
                if (go == null) continue;
                if (IsAlly(go)) continue;
                if (IsArcherTarget(go)) validTargets.Add(go);
            }

            foreach (var tag in TargetLists.ArcherTargets)
            {
                foreach (var go in validTargets)
                {
                    if (go.CompareTag(tag)) return go;
                }
            }
            return null;
        }

        private bool IsAlly(GameObject go)
        {
            foreach (var tag in TargetLists.AllyTags)
                if (go.CompareTag(tag)) return true;
            return false;
        }

        private bool IsArcherTarget(GameObject go)
        {
            foreach (var tag in TargetLists.ArcherTargets)
                if (go.CompareTag(tag)) return true;
            return false;
        }

        private bool IsPlayerInList(List<GameObject> objects)
        {
            foreach (var go in objects) if (go.CompareTag(GameTags.Player)) return true;
            return false;
        }

        private bool IsPlayingAttackAnimation()
        {
            if (Context.Animator == null) return false;
            var state = Context.Animator.GetCurrentAnimatorStateInfo(0);
            return state.IsName("archerDraw") || state.IsName("archerRelease");
        }

        public override void HandleAnimationEvent(string eventName)
        {
            if (CurrentState is IArcherAttackState attackState)
            {
                if (eventName == "DrawComplete") attackState.OnDrawComplete(Context);
                else if (eventName == "ArrowRelease") attackState.OnArrowRelease(Context);
            }
        }

        public override bool HandleItem(GameObject item)
        {
            var coinScript = item.GetComponent<Script.Game.Item.Coin.PhysicalItem>();
            if (coinScript == null) return false;

            if (coinScript.itemType == Script.Game.Item.Coin.ItemType.Coin && coinScript.denizensPickupAble)
            {
                if (Context.Status.IsFull) return false;
                Context.Status.AddCoin(1);
                return true;
            }
            return false;
        }

        #region Public Bridge Methods
        public void ForceRetreat(GameObject wall)
        {
            RetreatState.SetRetreatTarget(wall);
            ChangeState(RetreatState);
        }

        public void OnGuardManagerStatusChanged(bool isAttack)
        {
            if (isAttack)
            {
                if (CurrentState != ReturnToBaseState && CurrentState != RetreatState && CurrentState != HoldWallState)
                {
                    ChangeState(ReturnToBaseState);
                }
            }
        }

        public void HandleDayBreaks()
        {
            if (Context.GuardManager != null && !Context.GuardManager.Alarm.isEnemyAttack)
            {
                ChangeState(HuntingState);
            }
        }

        public void HandleNightFalls()
        {
            ChangeState(ReturnToBaseState);
        }
        #endregion
    }
}

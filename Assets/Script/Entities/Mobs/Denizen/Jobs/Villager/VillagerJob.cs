using System.Collections.Generic;
using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Utils;
using Script.Game.Item.Coin;
using Script.Game.Mobs.Subjects;

namespace Script.Game.Mobs.Denizen.Jobs.Villager
{
    public class VillagerJob : DenizenJob
    {
        // States
        public VillagerIdleState IdleState { get; private set; }
        public VillagerFearState FearState { get; private set; }
        public VillagerToTownState ToTownState { get; private set; }
        public VillagerRunToolState RunToolState { get; private set; }
        public VillagerDropCoinState DropCoinState { get; private set; }

        protected override void Awake()
        {
            PreAllocateStates();
        }

        protected override void PreAllocateStates()
        {
            IdleState    = new VillagerIdleState(this);
            FearState    = new VillagerFearState(this);
            ToTownState  = new VillagerToTownState(this);
            RunToolState = new VillagerRunToolState(this);
            DropCoinState = new VillagerDropCoinState(this);
        }

        protected override DenizenState GetInitialState()
        {
            // Xử lý Fear Pending (khi bị rớt đồ từ Archer xuống Villager ưu tiên chạy trốn trước)
            if (Context.Status.isFearPending)
            {
                Context.Status.isFearPending = false;
                return FearState;
            }

            // Ban đêm → Về nhà ngay
            if (Context.TimeManager != null && !Context.TimeManager.IsDaytime)
            {
                return ToTownState;
            }

            return IdleState;
        }

        protected override void OnEnterJob()
        {
            // Dân làng chỉ dùng Raycast 
            if (Context.RayCaster != null)
            {
                Context.RayCaster.OnDetectedObjectsChanged -= OnObjectsDetected;
                Context.RayCaster.OnDetectedObjectsChanged += OnObjectsDetected;
                
                // Gộp danh sách tag: VillagerTargets + ToolTags
                List<string> detectTags = new List<string>(TargetLists.VillagerTargets);
                foreach (var tool in TargetLists.ToolTags) detectTags.Add(tool);
                Context.RayCaster.tagsToDetect = detectTags;
                
                Context.RayCaster.SetRayLength(Context.Status.detectRangeRaycast); 
                Context.RayCaster.isActive = true;
                Context.RayCaster.isDrawRay = true; // Bật gizmo Raycast
            }

            // Nếu chưa có tool, hãy báo cho ToolManager để nhận tool ngay lập tức
            if (Context.Status.currentTool == null)
            {
                TransitionToIdle();
            }
        }

        protected override void OnExitJob()
        {
            if (Context.RayCaster != null)
            {
                Context.RayCaster.OnDetectedObjectsChanged -= OnObjectsDetected;
                Context.RayCaster.isActive = false;
                Context.RayCaster.isDrawRay = false;
            }
        }

        public override void OnObjectsDetected(List<GameObject> objects)
        {
            if (!enabled || CurrentState == null) return;

            // 1. Ưu tiên Enemy - tuyệt đối
            foreach (var go in objects)
            {
                if (go == null) continue;
                if (go.CompareTag(GameTags.Enemy))
                {
                    if (CurrentState == FearState)
                    {
                        // Đang sợ mà vẫn thấy enemy -> gia hạn thêm (theo status.maxFearDuration)
                        FearState.RefreshFear(Context);
                    }
                    else
                    {
                        ChangeState(FearState);
                    }
                    return;
                }
            }

            // 2. Ưu tiên Player để DropCoin (chỉ khi đang Idle)
            if (CurrentState == IdleState)
            {
                foreach (var go in objects)
                {
                    if (go == null) continue;
                    if (go.CompareTag(GameTags.Player))
                    {
                        // Kiểm tra nếu thực sự có xu để nhả
                        if (Context.Status.CurrentCoins > 0 && Context.CoinSharing != null && Context.CoinSharing.CanDropCoin())
                        {
                            ChangeState(DropCoinState);
                            return;
                        }
                    }
                }
            }

            // 3. Cuối cùng mới xét nhặt Tool (chỉ khi đang Idle)
            if (CurrentState == IdleState)
            {
                foreach (var go in objects)
                {
                    if (go == null) continue;
                    bool isTool = false;
                    foreach (var tag in TargetLists.ToolTags) if (go.CompareTag(tag)) isTool = true;

                    if (isTool)
                    {
                        var physicalItem = go.GetComponent<PhysicalItem>();
                        if (physicalItem != null)
                        {
                            AssignTool(physicalItem);
                            return;
                        }
                    }
                }
            }
        }

        
        public override bool HandleItem(GameObject item)
        {
            if (!enabled) return false;

            var physicalItem = item.GetComponent<PhysicalItem>();
            if (physicalItem == null) return false;

            // Nếu Item này hiện không cho phép Denizen nhặt (đang lơ lửng chờ rớt xuống do Spawner.DropItem)
            if (!physicalItem.denizensPickupAble) return false;

            // 1. Logic nhặt Tool để đổi nghề (port từ legacy VillagerBehavior.HandleItem)
            if (physicalItem.itemType != ItemType.Coin)
            {
                // Nếu tool này đã được giao cho người khác → thông báo ToolManager
                if (physicalItem.AssignedCarrier != null && physicalItem.AssignedCarrier != gameObject)
                {
                    physicalItem.OnPickedByStranger(gameObject);
                }

                // Bỏ tool cũ trước khi đổi nghề
                if (Context.Status.currentTool != null && Context.Status.currentTool != physicalItem)
                {
                    var abandonedTool = Context.Status.currentTool;
                    Context.Status.currentTool = null;
                    abandonedTool.AssignedCarrier = null;
                    abandonedTool.OnAssigneeAbandoned();
                }
                else
                {
                    Context.Status.currentTool = null;
                }

                // Đổi nghề theo loại tool
                ChangeRoleByTool(physicalItem.itemType);
                return true;
            }

            // 2. Logic nhặt Coin
            if (Context.Status.IsFull) return false;
            Context.Status.AddCoin(1);
            return true;
        }

        private void ChangeRoleByTool(ItemType toolType)
        {
            NPCRole newRole;
            switch (toolType)
            {
                case ItemType.Tool_Bow:
                    newRole = NPCRole.Archer;
                    break;
                case ItemType.Tool_Hammer:
                    newRole = NPCRole.Worker;
                    break;
                case ItemType.Tool_Scythe:
                    newRole = NPCRole.Farmer;
                    break;
                default:
                    Debug.LogWarning($"[VillagerJob] Unknown tool type: {toolType}");
                    return;
            }

            Debug.Log($"[VillagerJob] Picking up {toolType} → ChangeRole to {newRole}");
            Context.Core.ChangeRole(newRole);
        }

        public void HandleDayTime()
        {
            if (CurrentState == ToTownState) ChangeState(IdleState);
        }

        public void HandleNightTime()
        {
             if (CurrentState != FearState) ChangeState(ToTownState);
        }

        // Bridge cho code ngoài gọi (ví dụ AssignTool từ physical item)
        public void AssignTool(PhysicalItem tool)
        {
            Context.Status.currentTool = tool;
            ChangeState(RunToolState);
        }

        public void TransitionToIdle()
        {
            ChangeState(IdleState);
            // Thông báo cho ToolManager để có thể được giao việc (nhặt tool)
            Script.Manager.MobsManager.ToolManager.Instance?.OnVillagerBecameIdle(this);
        }
    }
}

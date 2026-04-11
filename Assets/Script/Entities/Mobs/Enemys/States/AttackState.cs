using System.Collections.Generic;
using System.Linq;
using Script.Game.Enemy.Core;
using Script.Game.PlayerCharacter.Skills;
using Script.Utils;
using UnityEngine;

namespace Script.Game.Enemy.States
{
    public class AttackState : EnemyState
    {
        // Khi ở trạng thái tấn công thì mặc định không được phép di chuyển (máy ủi đứng lại đấm)
        protected override bool AllowMovementInState => false;

        public override void Enter(EnemyContext ctx)
        {
            CheckSkillCasting(ctx);
            // Ép dừng di chuyển ngay khi vào state tấn công nếu state này không cho phép đi
            if (!AllowMovementInState) StopMovement(ctx);
        }

        public override void UpdateBehavior(EnemyContext ctx)
        {
            // [CỰC KỲ QUAN TRỌNG]: Nếu đang dở dang chiêu thức thì ĐỨNG IM ĐỢI XONG, không được nhảy State lung tung
            if (ctx.IsSkillCasting) 
            {
                return;
            }

            // [LÀM THEO YÊU CẦU]: Luôn chạy base để quay đầu về hướng Đích Chính (TownCenter/Portal) trước khi quét radar
            base.UpdateBehavior(ctx);

            var gtm = UnityEngine.Object.FindObjectOfType<Script.Manager.WorldManager.GameTimeManager>();
            bool isDay = gtm != null && gtm.IsDaytime;
            bool ignoreReturn = ctx.Status != null && ctx.Status.IgnoreReturnToPortal;

            // [FIXED TAGS]: Sử dụng các danh sách tập trung từ TargetLists
            List<string> scanTags = (isDay && !ignoreReturn) ? TargetLists.EnemyRetreatTargets : TargetLists.EnemyAdvanceTargets;

            // 2. [SCAN TARGET] Kiểm tra trong danh sách Detector
            var detected = (ctx.RaycastHelper != null) ? ctx.RaycastHelper.GetListHitObjects() : null;
            var detectedObj = detected?.FirstOrDefault(obj => obj != null && scanTags.Contains(obj.tag));
            
            // Nếu không quét trúng cái gì -> Lập tức quay lại nhiệm vụ làm con máy ủi đẩy tới đích
            if (detectedObj == null)
            {
                if (!isDay || ignoreReturn) ctx.Controller.ChangeState(ctx.Controller.MoveStateInstance);
                else
                {
                    ctx.Controller.ChangeState(ctx.Controller.ReturnStateInstance);
                }
                return;
            }

            // 3. [SKILL EXECUTION] Nếu có mục tiêu -> Thử tung chiêu
            if (ctx.SkillManager != null)
            {
                for (int i = 0; i < ctx.SkillManager.SlotCount; i++)
                {
                    if (ctx.IsSkillCasting) break;

                    if (ctx.SkillManager.IsReady(i))
                    {
                        var skill = ctx.SkillManager.GetSkill(i);
                        if (skill == null) continue;

                        bool canExecute = skill.moveRequired ? _isCanMove(ctx) : true;

                        if (canExecute)
                        {
                            if (ctx.SkillManager.ExecuteSkill(i, ctx.Self))
                            {
                                ctx.IsSkillCasting = true;
                                ctx.SkillAllowsMovement = skill.allowMovementDuringSkill; 
                                break;
                            }
                        }
                    }
                }
            }
        }

        protected override void OnIdleUpdate(EnemyContext ctx)
        {
            ctx.Controller.SetTriggerSafely("Idle");
        }

        public override void OnAttackAnimationComplete(EnemyContext ctx)
        {
            // Kết thúc casting để gỡ khóa di chuyển ở Tick tiếp theo
            ctx.IsSkillCasting = false;

            if (ctx.SkillManager != null)
            {
                for (int i = 0; i < ctx.SkillManager.SlotCount; i++)
                {
                    if (ctx.SkillManager.GetState(i) == SkillManager.SkillState.Executing)
                    {
                        ctx.SkillManager.OnSkillAnimationEnd(i, ctx.Self);
                    }
                }
            }

            // [MŨI TIÊM PHỤC HỒI]: Ép quái vật về trạng thái Idle để ép Animator thoát khỏi thế đánh
            if (ctx.Controller != null)
            {
                ctx.Controller.ChangeState(ctx.Controller.IdleStateInstance);
            }
        }

        public override void Exit(EnemyContext ctx)
        {
        }

        public void CheckSkillCasting(EnemyContext ctx)
        {
            bool isActuallyCasting = false;
            if (ctx.SkillManager != null)
            {
                for (int i = 0; i < ctx.SkillManager.SlotCount; i++)
                {
                    if (ctx.SkillManager.GetState(i) == SkillManager.SkillState.Executing)
                    {
                        isActuallyCasting = true;
                        break;
                    }
                }
            }
            ctx.IsSkillCasting = isActuallyCasting;
        }
    }
}

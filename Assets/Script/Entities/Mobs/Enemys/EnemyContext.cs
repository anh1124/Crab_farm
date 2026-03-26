using UnityEngine;
using Script.Game.Enemy.Core;

namespace Script.Game.Enemy
{
    public class EnemyContext
    {
        // References
        public GameObject Self;
        public Transform Transform;
        public Animator Animator;
        public EnemyAIController Controller;
        public EnemyStatus Status;
        public Script.Game.PlayerCharacter.Skills.SkillManager SkillManager;

        // Managers
        public EnemyTargetManager TargetManager;
        public System.Action<GameObject> ReturnToPoolAction;

        // States
        public GameObject CurrentTarget;
        // [BRIDGED FLAGS]: Trỏ thẳng vào Status để đồng bộ dữ liệu
        public bool IsSkillCasting 
        { 
            get => Status != null && Status.IsSkillCasting; 
            set { if (Status != null) Status.IsSkillCasting = value; } 
        }
        public bool SkillAllowsMovement 
        { 
            get => Status == null || Status.SkillAllowsMovement; 
            set { if (Status != null) Status.SkillAllowsMovement = value; } 
        }
        public GameObject HomePortal;
        
        public float GreedThreshold;
        public float CurrentGreed;
        
        public RayCastHelper RaycastHelper; // Cache helper để tránh GetComponent mỗi frame

        public bool IsAlive = true;
        public bool PendingRecycle = false;

        public bool GreedIsFull => GreedThreshold > 0 && CurrentGreed >= GreedThreshold;
    }
}

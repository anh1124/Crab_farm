using UnityEngine;
using Script.Game.Enemy.Core;

namespace Script.Game.Enemy
{
    public class EnemyContext
    {
        // References
        public GameObject Self;
        public Animator Animator;
        public EnemyAIController Controller;
        public EnemyStatus Status;

        // Managers
        public EnemyTargetManager TargetManager;
        public System.Action<GameObject> ReturnToPoolAction;

        // States
        public GameObject CurrentTarget;
        public GameObject HomePortal;
        
        public float GreedThreshold;
        public float CurrentGreed;
        
        public RayCastHelper RaycastHelper; // Cache helper để tránh GetComponent mỗi frame

        public bool IsAlive = true;
        public bool PendingRecycle = false;

        public bool GreedIsFull => GreedThreshold > 0 && CurrentGreed >= GreedThreshold;
    }
}

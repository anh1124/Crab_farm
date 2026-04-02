using UnityEngine;
using Script.Data_Model.Status;
using Script.Game.Mobs.Subjects;
using Script.Game.Other;
using Script.Manager.WorldManager;
using Script.Manager.GamePlayManager;
using Script.Manager.Building;
using Script.Game.Structures.Wall_And_Stump;
using Script.Manager;
using Script.Game.Mobs.MobsManager;
using Script.Game.Mobs.Denizen.Jobs.Vagrant;
using Script.Game.Mobs.Denizen.Jobs.Villager;
using Script.Game.Mobs.Denizen.Jobs.Archer;
using Script.Data_Model.Interfaces;

namespace Script.Game.Mobs.Denizen.Core
{
    //[không được xóa dòng này]: script này chỉ được lưu context
    public class DenizenContext : MonoBehaviour, IItemHandle
    {
        #region IItemHandle Implementation
        /// <summary>
        /// Bridge cho LootPickupTrigger: Luôn chuyển quyền xử lý cho Job hiện tại.
        /// </summary>
        public bool HandleItem(GameObject item)
        {
            var job = CurrentJob;
            if (job != null && job.enabled)
                return job.HandleItem(item);
            return false;
        }
        #endregion

        #region Core Components — Luôn có trên mọi Denizen
        [Header("Core Components")]
        public DenizenStatus Status;
        public DenizenRoleController RoleController;
        public Animator Animator;
        public SpriteRenderer SpriteRenderer;
        public DenizenMovement Movement;

        public DenizenJob CurrentJob
        {
            get
            {
                if (VagrantJob != null && VagrantJob.enabled) return VagrantJob;
                if (VillagerJob != null && VillagerJob.enabled) return VillagerJob;
                if (ArcherJob != null && ArcherJob.enabled) return ArcherJob;
                return null;
            }
        }

        // Shortcuts để tương thích với code cũ/States
        public GameObject GameObject => gameObject;
        public Transform Transform => transform;
        #endregion

        #region Detection — Nằm ở Child "Detection"
        [Header("Detection")]
        public RayCastHelper RayCaster;
        public CircleDetectHelper CircleDetect;
        #endregion

        #region Job-Specific — Có thể null tùy theo Role
        [Header("Job-Specific")]
        public ArcherShooting ArcherShooting;
        public DenizenCoinSharing CoinSharing;
        public VagrantJob VagrantJob;
        public VillagerJob VillagerJob;
        public ArcherJob ArcherJob;
        #endregion

        #region Managers — Local Cache (resolved at Runtime or Editor)
        [Header("Managers")]
        public GuardManager GuardManager;
        public GameTimeManager TimeManager;
        public WallAndStumpManager WallManager;
        public NPCUpdateManager NPCUpdateManager;
        #endregion

        [Header("Skills & Casting")]
        public Script.Game.PlayerCharacter.Skills.SkillManager SkillManager;
        public bool IsSkillCasting => Status != null && Status.isSkillCasting;
        public bool SkillAllowsMovement => Status != null && Status.skillAllowsMovement;

        // Cấu trúc cũ để không break code bên ngoài: IsAlive property
        public bool IsAlive => Status != null && Status.IsAlive;

        /// <summary>
        /// Hàm tự động tìm kiếm và điền các tham chiếu trên Inspector.
        /// Click chuột phải vào component DenizenContext chọn "Auto Find References".
        /// </summary>
        [ContextMenu("Auto Find References")]
        public void AutoFindReferences()
        {
            // Root components
            Status = GetComponent<DenizenStatus>();
            RoleController = GetComponent<DenizenRoleController>();
            Animator = GetComponent<Animator>();
            SpriteRenderer = GetComponent<SpriteRenderer>();
            CoinSharing = GetComponent<DenizenCoinSharing>();
            Movement = GetComponent<DenizenMovement>();
            SkillManager = GetComponent<Script.Game.PlayerCharacter.Skills.SkillManager>();

            // Child components
            RayCaster = GetComponentInChildren<RayCastHelper>(true);
            CircleDetect = GetComponentInChildren<CircleDetectHelper>(true);
            ArcherShooting = GetComponentInChildren<ArcherShooting>(true);
            VagrantJob = GetComponentInChildren<VagrantJob>(true);
            VillagerJob = GetComponentInChildren<VillagerJob>(true);
            ArcherJob = GetComponentInChildren<ArcherJob>(true);

            // Managers (Thường là Singleton nên có thể gán luôn)
            GuardManager = GuardManager.Instance;
            TimeManager = GameTimeManager.Instance;
            WallManager = WallAndStumpManager.Instance;
            NPCUpdateManager = NPCUpdateManager.Instance;

            //Debug.Log($"[DenizenContext] Auto-find complete for {gameObject.name}");
        }

        private void Reset()
        {
            AutoFindReferences();
        }

        public void ResetDetectors()
        {
            if (RayCaster != null)    RayCaster.IsActive = false;
            if (CircleDetect != null) CircleDetect.IsActive = false;
        }

        private void Awake()
        {
            // Fallback: tự tìm nếu chưa gán qua Inspector
            if (RoleController == null) RoleController = GetComponent<DenizenRoleController>();
            
            // Ưu tiên tìm Animator ở child (Skin/Body) theo cấu trúc prefab chuẩn
            if (Animator == null)       Animator = GetComponentInChildren<Animator>(true);
            if (SpriteRenderer == null) SpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            
            if (Movement == null)       Movement = GetComponent<DenizenMovement>();
            if (RayCaster == null)      RayCaster = GetComponentInChildren<RayCastHelper>(true);
            if (CircleDetect == null)   CircleDetect = GetComponentInChildren<CircleDetectHelper>(true);
            if (VagrantJob == null)     VagrantJob = GetComponentInChildren<VagrantJob>(true);
            if (VillagerJob == null)    VillagerJob = GetComponentInChildren<VillagerJob>(true);
            if (ArcherJob == null)      ArcherJob = GetComponentInChildren<ArcherJob>(true);

            // Managers
            if (GuardManager == null)    GuardManager = GuardManager.Instance;
            if (TimeManager == null)     TimeManager = GameTimeManager.Instance;
            if (WallManager == null)     WallManager = WallAndStumpManager.Instance;
            if (NPCUpdateManager == null) NPCUpdateManager = NPCUpdateManager.Instance;
        }
    }
}

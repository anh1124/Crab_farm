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

namespace Script.Game.Mobs.Denizen.Core
{
    /// <summary>
    /// DenizenContext: Component chứa tất cả các tham chiếu được cache sẵn.
    /// Giúp quản lý tham chiếu tập trung tại Inspector và tối ưu hiệu năng Runtime.
    /// </summary>
    public class DenizenContext : MonoBehaviour
    {
        #region Core Components — Luôn có trên mọi Denizen
        [Header("Core Components")]
        public DenizenStatus Status;
        public DenizenCore Core;
        public DenizenMovement Movement;
        public Animator Animator;
        public SpriteRenderer SpriteRenderer;

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
            Core = GetComponent<DenizenCore>();
            Movement = GetComponent<DenizenMovement>();
            Animator = GetComponent<Animator>();
            SpriteRenderer = GetComponent<SpriteRenderer>();
            CoinSharing = GetComponent<DenizenCoinSharing>();

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

        private void Awake()
        {
            // Nếu chạy Runtime mà chưa gán Manager thì gán lại lần cuối
            if (GuardManager == null) GuardManager = GuardManager.Instance;
            if (TimeManager == null) TimeManager = GameTimeManager.Instance;
            if (WallManager == null) WallManager = WallAndStumpManager.Instance;
            if (NPCUpdateManager == null) NPCUpdateManager = NPCUpdateManager.Instance;
        }
    }
}

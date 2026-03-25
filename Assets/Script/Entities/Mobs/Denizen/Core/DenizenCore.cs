using UnityEngine;
using Script.Data_Model.Interfaces;
using Script.Data_Model.Status;
using Script.Game.Mobs.Subjects;
using Script.Game.Mobs.MobsManager;
using Script.Manager.GamePlayManager;
using Script.Data_Model;
using Script.Game.Mobs.Denizen.Jobs.Vagrant;
using Script.Game.Mobs.Denizen.Jobs.Villager;
using Script.Game.Mobs.Denizen.Jobs.Archer;
using Script.Interfaces;

namespace Script.Game.Mobs.Denizen.Core
{
    /// <summary>
    /// DenizenCore: Bộ điều phối trung tâm thay thế DenizenRoleController.
    /// Quản lý việc chuyển đổi Job, Animator và đăng ký các Manager.
    /// </summary>
    [RequireComponent(typeof(DenizenStatus))]
    [RequireComponent(typeof(DenizenTimeHandler))]
    [RequireComponent(typeof(DenizenMovement))]
    [RequireComponent(typeof(DenizenCoinSharing))]
    [RequireComponent(typeof(DenizenAnimationEvents))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(DenizenContext))]
    /// <summary>
    /// BỘ XỬ LÝ TRUNG TÂM (CORE) CỦA HỆ THỐNG DENIZEN
    /// Kiến trúc FSM (Finite State Machine) kết hợp giữa Chủ Động và Thụ Động:
    /// 
    /// 1. [Tầng System] NPCUpdateManager đập nhịp 5Hz (0.2s) gọi vào UpdateBehavior() của Core.
    /// 2. [Tầng Core] Gọi tiếp UpdateBehavior() xuống Job đang Active (vd: VillagerJob).
    /// 3. [Tầng Job] Job là bộ não điều phối:
    ///    - Dẫn truyền nhịp đập UpdateBehavior xuống cho các State hiện tại chạy Logic/Animation.
    ///    - CHỦ ĐỘNG quát mắng Detector (RayCaster/CircleDetect) soi radar.
    /// 4. [Tầng Detector] Khi bị Job chọt, Detector đụng trúng các Object được chỉ định qua Tag thì:
    ///    - Bắn EVENT (THỤ ĐỘNG) OnDetectedObjectsChanged ngược lại.
    /// 5. [Tầng Job lắng nghe] Hàm OnObjectsDetected(List) bên trong Job hứng event này, 
    ///    đọc quyền Ưu Tiên Sinh Tồn (vd có Enemy -> chạy nhanh) rồi ép State phải đổi bằng ChangeState().
    /// </summary>
    public class DenizenCore : MonoBehaviour, ISaveable, IItemHandle, INPCBehavior, IDamageable
    {
        [Header("Role Configuration")]
        public NPCRole CurrentRole = NPCRole.Vagrant;

        [Header("Context Reference")]
        [SerializeField] private DenizenContext _context;

        private DenizenJob _currentJob;
        private DenizenStatus _status;
        private NPCRole _previousRole = NPCRole.Vagrant;

        private void Awake()
        {
            _status = GetComponent<DenizenStatus>();
            if (_context == null) _context = GetComponent<DenizenContext>();

            // Inject context vào tất cả các Job
            if (_context.VagrantJob != null) _context.VagrantJob.InjectContext(_context);
            if (_context.VillagerJob != null) _context.VillagerJob.InjectContext(_context);
            if (_context.ArcherJob != null) _context.ArcherJob.InjectContext(_context);

            DisableAllJobs();
            _previousRole = CurrentRole;
        }

        private void Start()
        {
            ApplyRole(CurrentRole);
            RegisterWithManager(CurrentRole);
        }

        private void OnEnable()
        {
            if (_status != null) _status.OnCoinChanged += OnCoinChangedHandler;
            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Register(this);
        }

        private void OnDisable()
        {
            if (_status != null) _status.OnCoinChanged -= OnCoinChangedHandler;
            if (NPCUpdateManager.Instance != null) NPCUpdateManager.Instance.Unregister(this);
        }

        private void OnDestroy()
        {
            UnregisterFromManager(CurrentRole);
        }

        public void ChangeRole(NPCRole newRole)
        {
            if (newRole == CurrentRole) return;

            UnregisterFromManager(CurrentRole);
            if (_currentJob != null) _currentJob.enabled = false;

            _previousRole = CurrentRole;
            CurrentRole = newRole;
            if (_status != null) _status.Role = newRole;

            ApplyRole(newRole);
            RegisterWithManager(newRole);
        }

        private void ApplyRole(NPCRole role)
        {
            // Reset animator để tránh kẹt trigger/state cũ
            if (_context.Animator != null) _context.Animator.Rebind();

            DisableAllJobs();

            switch (role)
            {
                case NPCRole.Vagrant:
                    gameObject.tag = Script.Utils.GameTags.Vagrant;
                    _currentJob = _context.VagrantJob;
                    break;
                case NPCRole.Villager:
                    gameObject.tag = Script.Utils.GameTags.Villager;
                    _currentJob = _context.VillagerJob;
                    break;
                case NPCRole.Archer:
                    gameObject.tag = Script.Utils.GameTags.Archer;
                    _currentJob = _context.ArcherJob;
                    GuardManager.Instance?.RegisterArcher(gameObject);
                    break;
            }

            if (_currentJob != null)
            {
                // Ưu tiên sử dụng JobController gán từ Inspector trên mỗi Job
                if (_context.Animator != null && _currentJob.JobController != null)
                {
                    _context.Animator.runtimeAnimatorController = _currentJob.JobController;
                    _context.Animator.Rebind(); // Đảm bảo controller mới được áp dụng ngay
                }

                _currentJob.enabled = true;
            }

            // Sync container
            Script.Manager.EntitySaveManager.Instance?.MoveObjectToContainer(gameObject);
        }

        private void DisableAllJobs()
        {
            if (_context.VagrantJob != null) _context.VagrantJob.enabled = false;
            if (_context.VillagerJob != null) _context.VillagerJob.enabled = false;
            if (_context.ArcherJob != null) _context.ArcherJob.enabled = false;
            _currentJob = null;
        }

        private void RegisterWithManager(NPCRole role)
        {
            DenizenManager.Instance?.RegisterDenizen(gameObject);
        }

        private void UnregisterFromManager(NPCRole role)
        {
            DenizenManager.Instance?.UnregisterDenizen(gameObject);
            if (role == NPCRole.Archer)
            {
                GuardManager.Instance?.UnregisterArcher(gameObject);
            }
        }

        #region Interface Implementations
        public void UpdateBehavior() => _currentJob?.UpdateBehavior();

        public void OnObjectsDetected(System.Collections.Generic.List<GameObject> detectedObjects)
        {
            _currentJob?.OnObjectsDetected(detectedObjects);
        }

        public bool HandleItem(GameObject item)
        {
            if (_currentJob is IItemHandle handler) return handler.HandleItem(item);
            return false;
        }

        public string GetPrefabID() => "denizen";

        public object CaptureData() => _status != null ? _status.CaptureData(transform) : null;

        public void RestoreData(object data)
        {
            var d = data as DenizenStatus.DenizenSaveData;
            if (d == null || _status == null) return;

            _status.RestoreData(d, transform);
            if (_status.Role != CurrentRole) ChangeRole(_status.Role);
            else ApplyRole(CurrentRole);
        }

        public void ReceiveDamage(float damage, GameObject instigator)
        {
            if (damage <= 0) return;
            if (CurrentRole == NPCRole.Vagrant) return;

            if (CurrentRole == NPCRole.Archer)
            {
                if (_status.CurrentCoins > 1)
                {
                    _status.SubtractCoin(1);
                    global::Spawner.Instance?.DropByDenizen(transform.position, 1, Vector2.up);
                }
                else if (_status.CurrentCoins == 1)
                {
                    if (global::Spawner.Instance != null && PrefabData.Instance != null)
                    {
                        global::Spawner.Instance.DropItem(PrefabData.Instance.bow, transform.position,
                            playerPickupAble: false, denizensPickupAble: true);
                    }
                    _status.isFearPending = true;
                    _status.fearTimer = 3f; 
                    _status.isInDanger = true;
                    ChangeRole(NPCRole.Villager);
                }
            }
            else if (CurrentRole == NPCRole.Villager)
            {
                if (_status.CurrentCoins > 0)
                {
                    _status.SubtractCoin(1);
                    global::Spawner.Instance?.DropByDenizen(transform.position, 1, Vector2.up);
                    
                    // Transition to Fear mode immediately
                    if (_currentJob is VillagerJob villager)
                    {
                        if (villager.CurrentState == villager.FearState) villager.FearState.RefreshFear(_context);
                        else villager.ChangeState(villager.FearState);
                    }
                }
                if (_status.CurrentCoins <= 0)
                {
                    ChangeRole(NPCRole.Vagrant);
                }
            }
        }
        #endregion

        #region Event Routing
        public void OnAnimationEvent(string eventName) => _currentJob?.HandleAnimationEvent(eventName);

        public void OnDayStart()
        {
            if (_currentJob is VillagerJob villager) villager.HandleDayTime();
        }

        public void OnArcherHuntingTime()
        {
            if (_currentJob is ArcherJob archer) archer.HandleDayBreaks();
        }

        public void OnNightStart()
        {
            if (_currentJob is VillagerJob villager) villager.HandleNightTime();
            else if (_currentJob is ArcherJob archer) archer.HandleNightFalls();
        }
        #endregion

        private void OnCoinChangedHandler(int newCoins)
        {
            if (!isActiveAndEnabled) return;

            // 1. Logic Thăng chức: Vagrant cầm tiền -> Villager
            if (CurrentRole == NPCRole.Vagrant && newCoins > 0)
            {
                ChangeRole(NPCRole.Villager);
                return;
            }

            // 2. Logic Giáng chức (Hurt): Archer/Villager hết tiền -> Vagrant
            if (CurrentRole == NPCRole.Archer && newCoins <= 0)
            {
                DropBowOnHurt();
                ChangeRole(NPCRole.Vagrant);
            }
            else if (CurrentRole != NPCRole.Vagrant && newCoins <= 0)
            {
                ChangeRole(NPCRole.Vagrant);
            }
        }

        private void DropBowOnHurt()
        {
             if (PrefabData.Instance != null && PrefabData.Instance.bow != null)
             {
                Transform container = transform.parent;
                if (Script.Manager.EntitySaveManager.Instance != null && Script.Manager.EntitySaveManager.Instance.otherContainer != null)
                    container = Script.Manager.EntitySaveManager.Instance.otherContainer.transform;
                
                Vector3 dropPos = transform.position + new Vector3(Random.Range(-0.5f, 0.5f), 1f, 0);
                Instantiate(PrefabData.Instance.bow, dropPos, Quaternion.identity, container);
             }
        }

        #region Public Bridge Methods
        public void ForceRetreat(GameObject wall)
        {
            if (_currentJob is ArcherJob archer) archer.ForceRetreat(wall);
        }

        public void OnGuardManagerStatusChanged(bool isAttack)
        {
            if (_currentJob is ArcherJob archer) archer.OnGuardManagerStatusChanged(isAttack);
        }

        public ArcherJob GetArcherJob() => _context != null ? _context.ArcherJob : null;
        #endregion

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (CurrentRole == _previousRole) return;

            NPCRole oldRole = _previousRole;
            _previousRole = CurrentRole;

            UnityEditor.EditorApplication.delayCall += () => {
                if (this == null || !Application.isPlaying) return;
                NPCRole newRole = CurrentRole;
                CurrentRole = oldRole;
                ChangeRole(newRole);
            };
        }
#endif
    }
}

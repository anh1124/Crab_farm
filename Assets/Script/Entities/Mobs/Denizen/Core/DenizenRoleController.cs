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
using Script.Manager.WorldManager;
using Script.Manager.SaveManager;
using Script.Game.Structures.Wall_And_Stump;
using Script.Save_Controller;
using Newtonsoft.Json.Linq;
using Script.Game.PlayerCharacter.Skills;
using Script.Audio;
using Script.Game.Other;

namespace Script.Game.Mobs.Denizen.Core
{
    /// <summary>
    /// DenizenRoleController: Bộ điều phối vai trò (Role Controller).
    /// CHỈ: Hoán đổi Job, Animator và Bridge cho SaveLoad.
    /// </summary>
    [RequireComponent(typeof(DenizenStatus))]
    [RequireComponent(typeof(DenizenCoinSharing))]
    [RequireComponent(typeof(DenizenAnimationEvents))]
    [RequireComponent(typeof(Animator))]
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(DenizenContext))]
    [RequireComponent(typeof(Script.Game.PlayerCharacter.Skills.SkillManager))]
    [RequireComponent(typeof(Script.Utils.DamageFlashEffect))]
    [RequireComponent(typeof(AudioEmitter))]
    public class DenizenRoleController : MonoBehaviour, ISaveable, IDamageable, Script.Interfaces.IHealthReactive
    {
        [Header("Role Configuration")]
        public NPCRole CurrentRole = NPCRole.Vagrant;//role mặc định tránh lỗi khởi tạo

        [Header("Context Reference")]
        [SerializeField] private DenizenContext _context;//context chung

        // Audio
        protected AudioEmitter _audioEmitter;

        private DenizenJob _currentJob;//job hiện tại
        private DenizenStatus _status;//status hiện tại
        private NPCRole _previousRole = NPCRole.Vagrant;//role trước đó
        private NPCRole? _pendingRoleFromSave = null;//role từ save chờ apply sau Setup

        private IDenizenProvider _denizenProvider;
        private IGuardProvider _guardProvider;
        private IGameTimeProvider _timeProvider;
        private WallAndStumpManager _wallManager;
        private NPCUpdateManager _npcUpdateManager;
        private Script.Game.Combat.IArrowPool _arrowPool;
        private Script.Manager.EntitySaveManager _saveManager;

        public void Setup(IDenizenProvider denizen, IGuardProvider guard, IGameTimeProvider time, WallAndStumpManager wall, NPCUpdateManager npcUpdate, Script.Game.Combat.IArrowPool arrowPool, Script.Manager.EntitySaveManager save)
        {
            _denizenProvider = denizen;
            _guardProvider = guard;
            _timeProvider = time;
            _wallManager = wall;
            _npcUpdateManager = npcUpdate;
            _arrowPool = arrowPool;
            _saveManager = save;

            // Injects context and managers to jobs via private _context
            if (_context != null)
            {
                _context.Setup(_guardProvider, _timeProvider, _wallManager, _npcUpdateManager, _arrowPool);

                if (_context.VagrantJob != null) _context.VagrantJob.InjectContext(_context);
                if (_context.VillagerJob != null) _context.VillagerJob.InjectContext(_context);
                if (_context.ArcherJob != null) _context.ArcherJob.InjectContext(_context);
            }

            if (_denizenProvider != null) _denizenProvider.RegisterDenizen(gameObject);

            // Apply role từ save hoặc current, đảm bảo _denizenProvider đã sẵn sàng
            NPCRole roleToApply = _pendingRoleFromSave ?? CurrentRole;
            _pendingRoleFromSave = null;
            ApplyRole(roleToApply);
        }

        public DenizenJob JobBrain => _currentJob;

        // IHealthReactive events
        public event System.Action<float, float> OnHealthChanged;
        public event System.Action<float> OnTakeDamageEvent;

        public void ReceiveDamage(float damage, GameObject instigator)
        {
            if (damage <= 0f) return;

            OnTakeDamageEvent?.Invoke(damage);
            OnHealthChanged?.Invoke(0f, 0f); // Dummy values - Denizen is immortal, no HP system

            // Root nhận damage, sau đó route về job đang active để xử lý hành vi cụ thể theo role.
            var activeJob = _currentJob != null && _currentJob.enabled ? _currentJob : _context?.CurrentJob;
            activeJob?.ReceiveDamage(damage, instigator);
        }

        private void Awake()
        {
            _status = GetComponent<DenizenStatus>();
            if (_context == null) _context = GetComponent<DenizenContext>();
            _audioEmitter = GetComponent<AudioEmitter>();

            DisableAllJobs();
            _previousRole = CurrentRole;
        }

        private void Start()
        {
            // Initial role application moved to Setup
        }

        private void OnDestroy()
        {
            _denizenProvider?.UnregisterDenizen(gameObject);
            if (CurrentRole == NPCRole.Archer) _guardProvider?.UnregisterArcher(gameObject);
        }

        public void ChangeRole(NPCRole newRole)
        {
            if (newRole == CurrentRole) return;

            // Kiểm tra xem Role mới có Job script tương ứng không trước khi thực hiện bất kỳ thay đổi nào
            if (!IsRoleSupported(newRole))
            {
                Debug.LogError($"[DenizenRoleController] Cannot change to {newRole}: No Job script found in DenizenContext for this role.");
                return; // Giữ nguyên role cũ, tránh làm hỏng AI
            }

            if (CurrentRole == NPCRole.Archer) _guardProvider?.UnregisterArcher(gameObject);
            if (_status != null) _status.isSkillCasting = false; // Tránh kẹt skill khi đổi role

            // Clear movement/target tức thì khi đổi role để tránh role mới thừa hưởng
            // target + move target cũ (nguồn gây chạy lung tung giữa các state).
            if (_context != null)
            {
                _context.Movement?.Stop();
                if (_context.Status != null)
                {
                    _context.Status.currentTarget = null;
                    _context.Status.coinTarget = null;
                    _context.Status.isMoving = false;
                }
                
                // Đảm bảo detector references tồn tại trước khi chuyển job
                // để tránh lỗi detector null khi job mới được enable
                if (_context.CircleDetect == null)
                    _context.CircleDetect = GetComponentInChildren<Script.Game.Other.CircleDetectHelper>(true);
                if (_context.RayCaster == null)
                    _context.RayCaster = GetComponentInChildren<RayCastHelper>(true);
            }
            
            // Chỉ vô hiệu hóa job cũ khi đã chắc chắn có job mới thay thế
            if (_currentJob != null) _currentJob.enabled = false;

            _previousRole = CurrentRole;
            CurrentRole = newRole;
            if (_status != null) _status.Role = newRole;

            ApplyRole(newRole);
        }

        private bool IsRoleSupported(NPCRole role)
        {
            switch (role)
            {
                case NPCRole.Vagrant:  return _context.VagrantJob != null;
                case NPCRole.Villager: return _context.VillagerJob != null;
                case NPCRole.Archer:   return _context.ArcherJob != null;
                // Hiện tại các role Worker và Farmer chưa có Job script nên sẽ trả về false
                case NPCRole.Worker:   return false; 
                case NPCRole.Farmer:   return false;
                default: return false;
            }
        }

        private void ApplyRole(NPCRole role)
        {
            if (_status != null && _status.debugDenizen)
                Debug.Log($"[DenizenRoleController] Applying Role: {role} to {gameObject.name}");
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
                    
                    // Chỉ đăng ký với GuardManager nếu không đang trong quá trình load từ save
                    // Tránh tình trạng phân công sai do đăng ký lần lượt
                    if (!Script.Manager.EntitySaveManager.IsLoadingFromSave)
                    {
                        _guardProvider?.RegisterArcher(gameObject);
                    }
                    else if (_status != null && _status.debugDenizen)
                    {
                        Debug.Log($"[DenizenRoleController] {gameObject.name}: Bỏ qua RegisterArcher vì đang loading từ save");
                    }
                    break;
            }

            if (_currentJob != null)
            {
                // Fallback to DenizenProvider for Animator Controller
                RuntimeAnimatorController targetController = _currentJob.JobController;
                
                // Nếu chưa có provider, thử gọi Setup lại từ DenizenManager nếu có thể
                if (_denizenProvider == null)
                {
                    var manager = FindObjectOfType<DenizenManager>();
                    if (manager != null) _denizenProvider = manager;
                }

                if (targetController == null && _denizenProvider != null)
                {
                    switch (role)
                    {
                        case NPCRole.Vagrant:  targetController = _denizenProvider.vagrantAnimatorController; break;
                        case NPCRole.Villager: targetController = _denizenProvider.villagerAnimatorController; break;
                        case NPCRole.Archer:   targetController = _denizenProvider.archerAnimatorController; break;
                        case NPCRole.Worker:   targetController = _denizenProvider.workerAnimatorController; break;
                        case NPCRole.Farmer:   targetController = _denizenProvider.farmerAnimatorController; break;
                    }
                }

                // CHỈ Swap và Rebind nếu Controller thực sự khác hoặc Animator chưa có controller
                if (_context.Animator != null && targetController != null)
                {
                    if (_context.Animator.runtimeAnimatorController != targetController)
                    {
                        if (_status != null && _status.debugDenizen)
                            Debug.Log($"[DenizenRoleController] Swapping AnimatorController to: {targetController.name} for Job: {role}");
                        _context.Animator.runtimeAnimatorController = targetController;
                        _context.Animator.Rebind();
                    }
                }
                else
                {
                    Debug.LogWarning($"[DenizenRoleController] Failed to swap Animator for {role}. Animator={(_context.Animator != null)} Controller={(targetController != null)}");
                }

                // Kích hoạt Job mới
                _currentJob.enabled = true;
                if (_status != null && _status.debugDenizen)
                    Debug.Log($"[DenizenRoleController] Job Activated: {_currentJob.GetType().Name} for {gameObject.name}");
            }
            else
            {
                Debug.LogError($"[DenizenRoleController] CRITICAL: No Job script found for role: {role} in children of {gameObject.name}!");
            }

            // Đồng bộ lại role dữ liệu
            CurrentRole = role;
            if (_status != null) _status.Role = role;

            _saveManager?.MoveObjectToContainer(gameObject);
        }

        private void DisableAllJobs()
        {
            if (_context.VagrantJob != null) _context.VagrantJob.enabled = false;
            if (_context.VillagerJob != null) _context.VillagerJob.enabled = false;
            if (_context.ArcherJob != null) _context.ArcherJob.enabled = false;
            _currentJob = null;
        }

        #region Save & Load
        public string GetPrefabID() => "denizen";

        public object CaptureData()
        {
            if (_status == null) return null;
            var d = _status.CaptureData(transform);
            d.skillManager = SkillManagerSaveUtility.CaptureFrom(this);
            return d;
        }

        public void RestoreData(object data)
        {
            DenizenStatus.DenizenSaveData d = null;
            if (data is JObject jObj)
                d = jObj.ToObject<DenizenStatus.DenizenSaveData>();
            else
                d = data as DenizenStatus.DenizenSaveData;
            if (d == null || _status == null) return;

            _status.RestoreData(d, transform);
            if (_status.Role != CurrentRole)
            {
                // Không gọi ChangeRole ngay, lưu lại để Setup gọi
                _pendingRoleFromSave = _status.Role;
            }
            else
            {
                _pendingRoleFromSave = null;
            }
            // KHÔNG gọi ApplyRole hay ChangeRole ở đây nữa

            var skillMgr = GetComponent<SkillManager>();
            SkillManagerSaveUtility.Restore(skillMgr, d.skillManager, gameObject);
        }
        #endregion

        #region Debug Context Menu
        [ContextMenu("Debug Denizen Status")] // Đảm bảo đúng tên Context Menu như trong file gốc
        public void DebugDenizenStatus()
        {
            var ctx = GetComponent<DenizenContext>();
            if (ctx == null || _status == null)
            {
                Debug.LogWarning($"[DenizenRoleController] {gameObject.name}: Context or Status missing.");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"===== DEBUG STATUS: {gameObject.name} =====");
            sb.AppendLine($"- Role: {CurrentRole}");
            
            string stateName = _currentJob != null && _currentJob.CurrentState != null 
                ? _currentJob.CurrentState.GetType().Name 
                : "None";
            sb.AppendLine($"- Current State: {stateName}");
            sb.AppendLine($"- Position: {transform.position}");

            if (ctx.TimeManager != null)
            {
                string dayNight = ctx.TimeManager.IsDaytime ? "Day" : "Night";
                sb.AppendLine($"- World Time: {ctx.TimeManager.GameHour:00}:{ctx.TimeManager.GameMinute:00} ({dayNight})");
                sb.AppendLine($"- World Date: Day {ctx.TimeManager.GameDay} / Month {ctx.TimeManager.GameMonth} / Year {ctx.TimeManager.GameYear}");
            }
            else
            {
                sb.AppendLine("- World Time: [TimeManager = null]");
            }
            
            if (_status.isMoving && _context.Movement != null)
            {
                sb.AppendLine($"- Move Target: {_context.Movement.MoveTargetPos}");
                sb.AppendLine($"- Current Speed: {_context.Movement.CurrentSpeed}");
            }

            sb.AppendLine("\n--- DenizenStatus Parameters ---");
            sb.AppendLine($"* General *");
            sb.AppendLine($"currentStateName: {(_status.currentStateName)}");
            sb.AppendLine($"isSkillCasting: {_status.isSkillCasting}");
            sb.AppendLine($"skillAllowsMovement: {_status.skillAllowsMovement}");
            sb.AppendLine($"canMove: {_status.CanMove}");
            sb.AppendLine($"isMoving: {_status.isMoving}");
            sb.AppendLine($"currentCoins: {_status.CurrentCoins}/{_status.MaxCoins}");
            
            sb.AppendLine($"\n* Movement *");
            sb.AppendLine($"walkSpeed: {_status.walkSpeed}");
            sb.AppendLine($"runSpeed: {_status.runSpeed}");
            sb.AppendLine($"huntingMoveSpeed: {_status.huntingMoveSpeed}");
            sb.AppendLine($"currentSpeed: {_status.currentSpeed}");

            // Archer Specific tactical logs
            if (CurrentRole == NPCRole.Archer && ctx.ArcherJob != null)
            {
                sb.AppendLine($"\n* Archer *");
                sb.AppendLine($"detectRangeRaycast: {_status.detectRangeRaycast}");
                sb.AppendLine($"archerDetectRange: {_status.archerDetectRange}");
                sb.AppendLine($"attackDame: {_status.attackDame}");
                sb.AppendLine($"attackSpeed: {_status.attackSpeed}");
                sb.AppendLine($"lastAttackTime: {_status.lastAttackTime}");
                sb.AppendLine($"isLeftFlank: {_status.isLeftFlank}");
                sb.AppendLine($"assignedWall: {(_status.assignedWall != null ? _status.assignedWall.name : "None")}");
                sb.AppendLine($"currentTarget: {(_status.currentTarget != null ? _status.currentTarget.name : "None")}");
                
                var squad = _guardProvider;
                if (squad != null)
                {
                    Vector3 defPos = squad.GetDefensePositionForArcher(gameObject);
                    sb.AppendLine($" - Defense Offset Pos: {defPos}");

                    // Log tọa độ bãi săn theo yêu cầu
                    Vector3? huntStart = squad.GetHuntingRangeStartPosition(_status.isLeftFlank);
                    if (huntStart.HasValue)
                    {
                        float range = squad.archerHuntingRange;
                        float endX = _status.isLeftFlank ? huntStart.Value.x - range : huntStart.Value.x + range;
                        sb.AppendLine($" - [HUNTING ZONE] Start: {huntStart.Value.x:F2} | End: {endX:F2} | Range: {range}");
                       
                        bool isOut = _status.isLeftFlank ? transform.position.x < endX : transform.position.x > endX;
                        if (isOut) sb.AppendLine(" <color=red>-> ĐÃ RA NGOÀI VÙNG SĂN BẮN!</color>");
                    }
                }
            }

            // In danh sách vật thể đang quét được
            sb.AppendLine($"\n* Detection Scanner *");
            if (_context.CircleDetect != null)
            {
                var detected = _context.CircleDetect.GetDetectedObjects();
                sb.AppendLine($"Detected Count: {detected.Count}");
                for (int i = 0; i < detected.Count; i++)
                {
                    if (detected[i] == null) continue;
                    float dist = Vector2.Distance(transform.position, detected[i].transform.position);
                    sb.AppendLine($" [{i}] {detected[i].name} (Tag: {detected[i].tag}) - Dist: {dist:F2}");
                }
            }

            Debug.Log(sb.ToString());
        }
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

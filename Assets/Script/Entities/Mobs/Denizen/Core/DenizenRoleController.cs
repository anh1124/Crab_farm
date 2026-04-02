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
    public class DenizenRoleController : MonoBehaviour, ISaveable
    {
        [Header("Role Configuration")]
        public NPCRole CurrentRole = NPCRole.Vagrant;//role mặc định tránh lỗi khởi tạo

        [Header("Context Reference")]
        [SerializeField] private DenizenContext _context;//context chung

        private DenizenJob _currentJob;//job hiện tại
        private DenizenStatus _status;//status hiện tại
        private NPCRole _previousRole = NPCRole.Vagrant;//role trước đó

        public DenizenJob JobBrain => _currentJob;

        private void Awake()
        {
            _status = GetComponent<DenizenStatus>();
            if (_context == null) _context = GetComponent<DenizenContext>();

            // Đẩy context vào script job 
            if (_context.VagrantJob != null) _context.VagrantJob.InjectContext(_context);
            if (_context.VillagerJob != null) _context.VillagerJob.InjectContext(_context);
            if (_context.ArcherJob != null) _context.ArcherJob.InjectContext(_context);
            
            DisableAllJobs();
            _previousRole = CurrentRole;
        }

        private void Start()
        {
            ApplyRole(CurrentRole);
            DenizenManager.Instance?.RegisterDenizen(gameObject);
        }

        private void OnDestroy()
        {
            DenizenManager.Instance?.UnregisterDenizen(gameObject);
            if (CurrentRole == NPCRole.Archer) GuardManager.Instance?.UnregisterArcher(gameObject);
        }

        public void ChangeRole(NPCRole newRole)
        {
            if (newRole == CurrentRole) return;

            if (CurrentRole == NPCRole.Archer) GuardManager.Instance?.UnregisterArcher(gameObject);
            if (_status != null) _status.isSkillCasting = false; // Tránh kẹt skill khi đổi role
            if (_currentJob != null) _currentJob.enabled = false;

            _previousRole = CurrentRole;
            CurrentRole = newRole;
            if (_status != null) _status.Role = newRole;

            ApplyRole(newRole);
        }

        private void ApplyRole(NPCRole role)
        {
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
                    GuardManager.Instance?.RegisterArcher(gameObject);
                    break;
            }

            if (_currentJob != null)
            {
                // Fallback to DenizenManager if JobController was not assigned directly on the Job component
                RuntimeAnimatorController targetController = _currentJob.JobController;
                if (targetController == null && DenizenManager.Instance != null)
                {
                    switch (role)
                    {
                        case NPCRole.Vagrant:  targetController = DenizenManager.Instance.vagrantAnimatorController; break;
                        case NPCRole.Villager: targetController = DenizenManager.Instance.villagerAnimatorController; break;
                        case NPCRole.Archer:   targetController = DenizenManager.Instance.archerAnimatorController; break;
                        case NPCRole.Worker:   targetController = DenizenManager.Instance.workerAnimatorController; break;
                        case NPCRole.Farmer:   targetController = DenizenManager.Instance.farmerAnimatorController; break;
                    }
                }

                if (_context.Animator != null && targetController != null)
                {
                    Debug.Log($"[DenizenRoleController] Swapping AnimatorController to: {targetController.name} for Job: {role}");
                    _context.Animator.runtimeAnimatorController = targetController;
                    _context.Animator.Rebind();
                }
                else
                {
                    Debug.LogWarning($"[DenizenRoleController] Failed to swap Animator for {role}. Animator={(_context.Animator!=null)} Controller={(targetController!=null)}");
                }

                _currentJob.enabled = true;
            }
            else
            {
                Debug.LogError($"[DenizenRoleController] No Job script found for role: {role}");
            }

            // Đồng bộ lại role dữ liệu
            CurrentRole = role;
            if (_status != null) _status.Role = role;

            Script.Manager.EntitySaveManager.Instance?.MoveObjectToContainer(gameObject);
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

        public object CaptureData() => _status != null ? _status.CaptureData(transform) : null;

        public void RestoreData(object data)
        {
            var d = data as DenizenStatus.DenizenSaveData;
            if (d == null || _status == null) return;

            _status.RestoreData(d, transform);
            if (_status.Role != CurrentRole) ChangeRole(_status.Role);
            else ApplyRole(CurrentRole);
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
            sb.AppendLine($"isMovementAllowed: {_status.isMovementAllowed}");
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
                
                var squad = GuardManager.Instance;
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

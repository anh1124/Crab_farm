using UnityEngine;
using System.Collections.Generic;
using Script.Tutorial.Core.Managers;

namespace Script.Tutorial
{
    /// <summary>
    /// Thực thi các action khi bắt đầu dialog.
    /// Thêm action mới: viết hàm private + đăng ký vào _actionRegistry trong RegisterActions().
    /// Không cần enum, không cần switch-case.
    /// </summary>
    public class TutorialActionExecutor : MonoBehaviour
    {
        private TutorialGuideArrowController _arrowController;
        private TutorialCoinCloudManager _coinCloudManager;
        private TutorialConditionChecker _conditionChecker;
        private TutorialUIStateManager _uiStateManager;
        private TutorialSkillController _skillController;

        /// <summary>
        /// Registry ánh xạ tên hàm (string) → delegate Action.
        /// Thêm action mới tại RegisterActions().
        /// </summary>
        private Dictionary<string, System.Action<TutorialActionData>> _actionRegistry;

        /// <summary>
        /// Event khi coin cloud hoàn thành - TutorialController sẽ subscribe để advance dialog
        /// </summary>
        public event System.Action OnCoinCloudComplete;

        /// <summary>
        /// Initialize với các controller reference
        /// </summary>
        public void Initialize(
            TutorialGuideArrowController arrowController,
            TutorialCoinCloudManager coinCloudManager,
            TutorialConditionChecker conditionChecker,
            TutorialUIStateManager uiStateManager = null,
            TutorialSkillController skillController = null)
        {
            _arrowController = arrowController;
            _coinCloudManager = coinCloudManager;
            _conditionChecker = conditionChecker;
            _uiStateManager = uiStateManager;
            _skillController = skillController;

            if (_coinCloudManager != null)
                _coinCloudManager.OnCoinCloudFinished += OnCoinCloudFinished;

            RegisterActions();
        }

        // ─────────────────────────────────────────────────────────────
        //  REGISTRY: Đăng ký tên hàm → delegate ở đây
        //  Khi thêm action mới: viết hàm bên dưới, rồi đăng ký tại đây.
        // ─────────────────────────────────────────────────────────────
        private void RegisterActions()
        {
            _actionRegistry = new Dictionary<string, System.Action<TutorialActionData>>
            {
                { "ActiveCoinCloud",        Action_ActiveCoinCloud },
                { "ShowArrowToTownCenter",  Action_ShowArrowToTownCenter },
                { "SetUIElementActive",     Action_SetUIElementActive },
                { "SetAllUIActive",         Action_SetAllUIActive },
                { "HideAllUI",              Action_HideAllUI },
                { "ShowAllUI",              Action_ShowAllUI },
                { "HideElement",            Action_HideElement },
                { "ShowElement",            Action_ShowElement },
                { "SetSkillActive",         Action_SetSkillActive },
                { "AddSkill",               Action_AddSkill },
                { "RemoveSkill",            Action_RemoveSkill },
                { "ClearSkills",            Action_ClearSkills },
                { "AddBuildingToTutorialBuildMenu", Action_AddBuildingToTutorialBuildMenu },
                { "ClearTutorialBuildMenu", Action_ClearTutorialBuildMenu },
            };
        }

        // ─────────────────────────────────────────────────────────────
        //  EXECUTOR
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Thực thi toàn bộ danh sách action khi bắt đầu dialog
        /// </summary>
        public void ExecuteOnStartAction(TutorialDialogLineSO line)
        {
            if (line == null || line.onStartActions == null || line.onStartActions.Count == 0) return;

            foreach (var actionData in line.onStartActions)
            {
                ExecuteSingleAction(actionData);
            }
        }

        /// <summary>
        /// Tra cứu tên hàm trong registry và thực thi
        /// </summary>
        private void ExecuteSingleAction(TutorialActionData action)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.methodName)) return;

            if (_actionRegistry.TryGetValue(action.methodName.Trim(), out var handler))
            {
                handler.Invoke(action);
            }
            else
            {
                Debug.LogWarning($"[TutorialActionExecutor] Không tìm thấy action: '{action.methodName}'. " +
                                 $"Kiểm tra lại tên hàm hoặc đăng ký trong RegisterActions().");
            }
        }

        // ─────────────────────────────────────────────────────────────
        //  CÁC HÀM ACTION
        //  Thêm action mới: viết hàm ở đây rồi đăng ký trong RegisterActions()
        // ─────────────────────────────────────────────────────────────

        private void Action_ActiveCoinCloud(TutorialActionData data)
        {
            _coinCloudManager?.SpawnCoinCloud(data.position, data.param);
        }

        private void Action_ShowArrowToTownCenter(TutorialActionData data)
        {
            _arrowController?.StartArrowToTownCenter();
        }

        private void Action_SetUIElementActive(TutorialActionData data)
        {
            if (!string.IsNullOrEmpty(data.stringParam))
                _uiStateManager?.SetActiveElement(data.stringParam, data.boolParam);
        }

        private void Action_SetAllUIActive(TutorialActionData data)
        {
            _uiStateManager?.SetActiveAll(data.boolParam);
        }

        private void Action_HideAllUI(TutorialActionData data) => _uiStateManager?.SetActiveAll(false);
        private void Action_ShowAllUI(TutorialActionData data) => _uiStateManager?.SetActiveAll(true);
        private void Action_HideElement(TutorialActionData data) => _uiStateManager?.SetActiveElement(data.stringParam, false);
        private void Action_ShowElement(TutorialActionData data) => _uiStateManager?.SetActiveElement(data.stringParam, true);

        private void Action_AddBuildingToTutorialBuildMenu(TutorialActionData data)
        {
            var bm = GetComponent<Script.Tutorial.Core.Managers.TutorialBuildingManager>();
            if (bm != null)
            {
                if (System.Enum.TryParse<Script.Manager.Building.BuildingType>(data.stringParam, true, out var type))
                {
                    bm.AddBuildingToTutorialBuildMenu(type);
                }
                else
                {
                    Debug.LogError($"[TutorialActionExecutor] BuildingType không hợp lệ: {data.stringParam}");
                }
            }
            else
            {
                Debug.LogError("[TutorialActionExecutor] KHÔNG TÌM THẤY TutorialBuildingManager trên GameObject này! Vui lòng Add Component 'Tutorial Building Manager' vào GameObject.");
            }
        }
        private void Action_ClearTutorialBuildMenu(TutorialActionData data)
        {
            var bm = GetComponent<Script.Tutorial.Core.Managers.TutorialBuildingManager>();
            if (bm != null)
            {
                bm.ClearTutorialBuildMenu();
            }
        }
        
        private void Action_SetSkillActive(TutorialActionData data)
        {
            _skillController?.SetSkillManagerActive(data.boolParam);
        }

        private void Action_AddSkill(TutorialActionData data)
        {
            _skillController?.AddSkillByName(data.stringParam, data.param);
        }

        private void Action_RemoveSkill(TutorialActionData data)
        {
            _skillController?.RemoveSkillFromPlayer(data.param);
        }

        private void Action_ClearSkills(TutorialActionData data)
        {
            _skillController?.ClearAllSkills();
        }

        // ─────────────────────────────────────────────────────────────
        //  LIFECYCLE
        // ─────────────────────────────────────────────────────────────

        private void OnCoinCloudFinished()
        {
            Debug.Log("[TutorialActionExecutor] CoinCloud finished - firing OnCoinCloudComplete event");
            OnCoinCloudComplete?.Invoke();
        }

        public void StopAllActions()
        {
            _arrowController?.StopAllArrows();
        }

        public void Cleanup()
        {
            if (_coinCloudManager != null)
                _coinCloudManager.OnCoinCloudFinished -= OnCoinCloudFinished;
        }
    }
}

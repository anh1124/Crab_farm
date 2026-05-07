using UnityEngine;
using System;
using System.Collections.Generic;
using Script.Tutorial.Core.Managers;
using Script.Game.Structures.TownCenter;

namespace Script.Tutorial
{
    /// <summary>
    /// TutorialActionExecutor: Thực thi các lệnh (Actions) được định nghĩa trong ScriptableObject.
    /// Giúp tách biệt logic kịch bản và code hệ thống.
    /// </summary>
    public class TutorialActionExecutor : MonoBehaviour
    {
        // Events
        public event Action OnCoinCloudComplete;

        // Managers/Controllers
        private TutorialUIStateManager _uiStateManager;
        private TutorialSkillController _skillController;
        private TutorialCoinCloudManager _coinCloudManager;
        private TutorialGuideArrowController _arrowController;
        private TutorialRabbitManager _rabbitManager;

        // Registry: Map tên chuỗi (string) trong SO -> Hàm thực thi (delegate)
        private Dictionary<string, Action<TutorialActionData>> _actionRegistry;

        public void Initialize(
            TutorialGuideArrowController arrowController, 
            TutorialCoinCloudManager coinCloudManager, 
            TutorialConditionChecker conditionChecker, 
            TutorialUIStateManager uiStateManager, 
            TutorialSkillController skillController,
            TutorialRabbitManager rabbitManager)
        {
            _arrowController = arrowController;
            _coinCloudManager = coinCloudManager;
            _uiStateManager = uiStateManager;
            _skillController = skillController;
            _rabbitManager = rabbitManager;

            if (_coinCloudManager != null)
                _coinCloudManager.OnCoinCloudFinished += OnCoinCloudFinished;

            RegisterActions();
        }

        private void RegisterActions()
        {
            _actionRegistry = new Dictionary<string, Action<TutorialActionData>>
            {
                { "ActiveCoinCloud",                Action_ActiveCoinCloud },
                { "ShowGuideArrowToTarget",         Action_ShowGuideArrowToTarget },
                { "HideGuideArrow",                 Action_HideGuideArrow },
                { "SetUIElementActive",             Action_SetUIElementActive },
                { "SetAllUIActive",                 Action_SetAllUIActive },
                { "HideAllUI",                      Action_HideAllUI },
                { "ShowAllUI",                      Action_ShowAllUI },
                { "HideElement",                    Action_HideElement },
                { "ShowElement",                    Action_ShowElement },
                { "SetSkillActive",                 Action_SetSkillActive },
                { "AddSkill",                       Action_AddSkill },
                { "RemoveSkill",                    Action_RemoveSkill },
                { "ClearSkills",                    Action_ClearSkills },
                { "AddBuildingToTutorialBuildMenu", Action_AddBuildingToTutorialBuildMenu },
                { "ClearTutorialBuildMenu",         Action_ClearTutorialBuildMenu },
                { "SpawnRabbits",                   Action_SpawnRabbits },
            };
        }

        public void ExecuteOnStartAction(TutorialDialogLineSO line)
        {
            if (line == null || line.onStartActions == null || line.onStartActions.Count == 0) return;

            foreach (var actionData in line.onStartActions)
            {
                ExecuteSingleAction(actionData);
            }
        }

        private void ExecuteSingleAction(TutorialActionData action)
        {
            if (action == null || string.IsNullOrWhiteSpace(action.methodName)) return;

            if (_actionRegistry.TryGetValue(action.methodName.Trim(), out var handler))
            {
                Debug.Log($"<color=white>[TutorialActionExecutor]</color> Đang thực thi: <b>{action.methodName}</b>");
                handler.Invoke(action);
            }
            else
            {
                Debug.LogWarning($"[TutorialActionExecutor] Không tìm thấy action: '{action.methodName}'.");
            }
        }

        // ── Actions ──────────────────────────────────────────────────

        private void Action_ActiveCoinCloud(TutorialActionData data) => _coinCloudManager?.SpawnCoinCloud(data.position, data.param);

        private void Action_ShowGuideArrowToTarget(TutorialActionData data)
        {
            if (string.IsNullOrEmpty(data.stringParam))
            {
                Debug.LogWarning("[TutorialActionExecutor] ShowGuideArrowToTarget được gọi nhưng không có Tag (stringParam)!");
                return;
            }
            _arrowController?.StartGuideArrowToTag(data.stringParam);
        }

        private void Action_HideGuideArrow(TutorialActionData data)
        {
            _arrowController?.StopAllArrows();
        }

        private void Action_SetUIElementActive(TutorialActionData data) => _uiStateManager?.SetActiveElement(data.stringParam, data.boolParam);
        private void Action_SetAllUIActive(TutorialActionData data) => _uiStateManager?.SetActiveAll(data.boolParam);
        private void Action_HideAllUI(TutorialActionData data) => _uiStateManager?.SetActiveAll(false);
        private void Action_ShowAllUI(TutorialActionData data) => _uiStateManager?.SetActiveAll(true);
        private void Action_HideElement(TutorialActionData data) => _uiStateManager?.SetActiveElement(data.stringParam, false);
        private void Action_ShowElement(TutorialActionData data) => _uiStateManager?.SetActiveElement(data.stringParam, true);

        private void Action_SetSkillActive(TutorialActionData data) => _skillController?.SetSkillManagerActive(data.boolParam);
        private void Action_AddSkill(TutorialActionData data) => _skillController?.AddSkillByName(data.stringParam, data.param);
        private void Action_RemoveSkill(TutorialActionData data) => _skillController?.RemoveSkillFromPlayer(data.param);
        private void Action_ClearSkills(TutorialActionData data) => _skillController?.ClearAllSkills();

        private void Action_AddBuildingToTutorialBuildMenu(TutorialActionData data)
        {
            var bm = GetComponent<TutorialBuildingManager>();
            if (bm != null)
            {
                if (Enum.TryParse<Script.Manager.Building.BuildingType>(data.stringParam, true, out var type))
                    bm.AddBuildingToTutorialBuildMenu(type);
                else
                    Debug.LogError($"[TutorialActionExecutor] BuildingType không hợp lệ: {data.stringParam}");
            }
            else Debug.LogError("[TutorialActionExecutor] Thiếu TutorialBuildingManager!");
        }

        private void Action_ClearTutorialBuildMenu(TutorialActionData data) => GetComponent<TutorialBuildingManager>()?.ClearTutorialBuildMenu();

        private void Action_SpawnRabbits(TutorialActionData data)
        {
            if (_rabbitManager != null)
            {
                _rabbitManager.SpawnRabbits(data.param);
            }
            else
            {
                Debug.LogError("<color=red>[TutorialActionExecutor]</color> LỖI: TutorialRabbitManager chưa được gán vào ActionExecutor!");
            }
        }

        // ── Lifecycle ──────────────────────────────────────────────────

        private void OnCoinCloudFinished() => OnCoinCloudComplete?.Invoke();

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

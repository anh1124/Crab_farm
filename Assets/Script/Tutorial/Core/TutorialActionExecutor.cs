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
        private TutorialCoinButtonPulse _buttonPulse;
        private TutorialUIStateManager _uiStateManager;

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
            TutorialCoinButtonPulse buttonPulse,
            TutorialUIStateManager uiStateManager = null)
        {
            _arrowController = arrowController;
            _coinCloudManager = coinCloudManager;
            _conditionChecker = conditionChecker;
            _buttonPulse = buttonPulse;
            _uiStateManager = uiStateManager;

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
                { "SpawnCoinCloud",         Action_SpawnCoinCloud },
                { "ShowArrow",              Action_ShowArrowToTownCenter },
                { "PulseCoinButton",        Action_PulseCoinButton },
                { "SetUIElementActive",     Action_SetUIElementActive },
                { "SetAllUIActive",         Action_SetAllUIActive },
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

        private void Action_SpawnCoinCloud(TutorialActionData data)
        {
            _coinCloudManager?.SpawnCoinCloud(data.position, data.param);
        }

        private void Action_ShowArrowToTownCenter(TutorialActionData data)
        {
            _arrowController?.StartArrowToTownCenter();
        }

        private void Action_PulseCoinButton(TutorialActionData data)
        {
            _buttonPulse?.StartPulse();
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
            _buttonPulse?.StopPulse();
        }

        public void Cleanup()
        {
            if (_coinCloudManager != null)
                _coinCloudManager.OnCoinCloudFinished -= OnCoinCloudFinished;
        }
    }
}

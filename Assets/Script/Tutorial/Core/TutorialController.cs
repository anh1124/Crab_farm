using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
// using Script.Controller.GUI_Controller.PlayerGui;

namespace Script.Tutorial
{
    /// <summary>
    /// Controller chính chạy tutorial - xử lý flow và dialog advancement.
    /// Arrow và CoinCloud được quản lý bởi các manager riêng.
    /// </summary>
    public class TutorialController : MonoBehaviour
    {
        public static TutorialController Instance { get; private set; }
        
        [Header("Data")]
        [SerializeField] private List<TutorialScriptSO> tutorials = new List<TutorialScriptSO>();
        
        [Header("UI")]
        [SerializeField] private TutorialDialogPanel dialogPanel;
        
        [Header("Controllers")]
        [SerializeField] private TutorialGuideArrowController arrowController;
        [SerializeField] private TutorialCoinCloudManager coinCloudManager;
        [SerializeField] private TutorialActionExecutor actionExecutor;
        [SerializeField] private TutorialCoinButtonPulse buttonPulse;
        [SerializeField] private Core.Managers.TutorialUIStateManager uiStateManager;
        
        [Header("Completion Dialog Text")]
        [SerializeField] private string completionTitle = "";
        [SerializeField, TextArea(3, 5)]
        private string completionMessage = "You have completed all tutorials!\n\n<size=18><i>Click anywhere or press Skip to return to Main Menu...</i></size>";
        
        public event System.Action<int> OnDialogIndexChanged;
        
        // State
        private int _currentTutorialIndex = 0;
        private int _currentDialogIndex = 0;
        private bool _waitingForCondition = false;
        private string _currentCondition = "";
        private bool _isRunning = false;
        private bool _showingCompletionDialog = false;
        
        private TutorialScriptSO CurrentScript => _currentTutorialIndex < tutorials.Count ? tutorials[_currentTutorialIndex] : null;
        
        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }
        
        private void Start()
        {
            // Đảm bảo panel được active ngay lập tức
            if (dialogPanel != null)
            {
                dialogPanel.gameObject.SetActive(true);
            }

            SetupScene();
            
            // Initialize managers
            InitializeManagers();
            
            // Đăng ký lắng nghe condition events
            TutorialConditionChecker.Instance.OnConditionMet += OnConditionMet;
            
            // Bắt đầu tutorial
            StartTutorial();
        }
        
        private void OnDestroy()
        {
            if (TutorialConditionChecker.Instance != null)
                TutorialConditionChecker.Instance.OnConditionMet -= OnConditionMet;
            
            // Unsubscribe from coin cloud complete event
            if (actionExecutor != null)
                actionExecutor.OnCoinCloudComplete -= AdvanceDialog;
            
            // Cleanup controllers
            actionExecutor?.Cleanup();
            coinCloudManager?.Cleanup();
            arrowController?.StopAllArrows();
        }
        
        /// <summary>
        /// Initialize các controller với references
        /// </summary>
        private void InitializeManagers()
        {
            if (actionExecutor != null)
            {
                actionExecutor.Initialize(arrowController, coinCloudManager, TutorialConditionChecker.Instance, buttonPulse, uiStateManager);
                // Subscribe để advance dialog khi coin cloud hoàn thành
                actionExecutor.OnCoinCloudComplete += AdvanceDialog;
            }
        }
        
        private void SetupScene()
        {
            if (CurrentScript == null) return;
            
            // Setup TownCenter - tìm trong scene trước, spawn từ PrefabData nếu chưa có
            SetupTownCenter();
        }
        
        private void SetupTownCenter()
        {
            // Tìm TownCenter đã có trong scene (spawn bởi StartGameScrene)
            var existingTC = FindObjectOfType<Script.Game.Structures.TownCenter.TownCenterBehavior>();
            if (existingTC != null)
            {
                TutorialConditionChecker.Instance?.SetupTownCenterListener(existingTC);
                
                // Lắng nghe CoinSlotVisual trên TownCenter
                var coinSlotVisual = existingTC.GetComponentInChildren<Script.Game.Item.Coin.CoinSlotVisual>();
                if (coinSlotVisual != null)
                {
                    TutorialConditionChecker.Instance?.SetupCoinSlotListener(coinSlotVisual);
                }
                return;
            }
            
            // Spawn từ PrefabData nếu chưa có
            var pd = Script.Data_Model.PrefabData.Instance;
            if (pd != null && pd.townCenterList.Count > 0)
            {
                var tcPrefab = pd.townCenterList[0];
                if (tcPrefab != null)
                {
                    var tc = Instantiate(tcPrefab, Vector3.zero, Quaternion.identity);
                    
                    var tcBehavior = tc.GetComponent<Script.Game.Structures.TownCenter.TownCenterBehavior>();
                    if (tcBehavior != null)
                    {
                        TutorialConditionChecker.Instance?.SetupTownCenterListener(tcBehavior);
                        
                        // Lắng nghe CoinSlotVisual
                        var coinSlotVisual = tc.GetComponentInChildren<Script.Game.Item.Coin.CoinSlotVisual>();
                        if (coinSlotVisual != null)
                        {
                            TutorialConditionChecker.Instance?.SetupCoinSlotListener(coinSlotVisual);
                        }
                    }
                }
            }
        }
        
        // Arrow và CoinCloud được quản lý bởi TutorialArrowManager và TutorialCoinCloudManager
        // ExecuteOnStartAction được quản lý bởi TutorialActionExecutor
        
        private void StartTutorial()
        {
            if (CurrentScript == null || CurrentScript.dialogLines.Count == 0)
            {
                Debug.LogError("[TutorialController] No script or empty dialog list!");
                EndTutorial();
                return;
            }
            
            _currentTutorialIndex = 0;
            _currentDialogIndex = 0;
            _isRunning = true;
            
            // Tắt các UI không cần thiết
            uiStateManager?.DisableForTutorial();
            
            TutorialConditionChecker.Instance?.ResetConditions();
            ShowCurrentDialog();
        }
        
        private void ShowCurrentDialog()
        {
            if (CurrentScript == null || _currentDialogIndex >= CurrentScript.dialogLines.Count)
            {
                EndTutorial();
                return;
            }
            
            var line = CurrentScript.dialogLines[_currentDialogIndex];
            if (line == null) return;
            
            // Fire event để external scripts biết đang ở dialog nào
            OnDialogIndexChanged?.Invoke(_currentDialogIndex);
            
            // Chạy action khi bắt đầu dialog (spawn, animation, v.v.)
            actionExecutor?.ExecuteOnStartAction(line);
            
            // Hiện dialog (dùng speakerName làm title, content làm dialog)
            dialogPanel.Show(line.speakerName, line.content, line.dialogType);
            
            // Xử lý theo type
            switch (line.dialogType)
            {
                case TutorialDialogType.Skipable:
                    _waitingForCondition = false;
                    break;
                    
                case TutorialDialogType.ExitConditional:
                    _waitingForCondition = true;
                    _currentCondition = line.conditionName;
                    
                    // Kiểm tra xem condition đã được thỏa mãn trước đó chưa
                    if (TutorialConditionChecker.Instance?.IsConditionMet(_currentCondition) == true)
                    {
                        // Đã thỏa mãn rồi, chuyển luôn
                        AdvanceDialog();
                    }
                    break;
            }
        }
        
        /// <summary>
        /// Gọi khi player click (cho skipable dialogs)
        /// </summary>
        public void OnDialogClicked()
        {
            // Xử lý dialog hoàn thành (khi đã hết tất cả tutorial)
            if (_showingCompletionDialog)
            {
                _showingCompletionDialog = false;
                dialogPanel.Hide();
                ExitTutorial();
                return;
            }
            
            if (!_isRunning) return;
            if (_waitingForCondition) return; // Không cho click nếu đang đợi condition
            
            var line = CurrentScript.dialogLines[_currentDialogIndex];
            if (line != null && line.dialogType == TutorialDialogType.Skipable)
            {
                AdvanceDialog();
            }
        }
        
        private void OnConditionMet(string conditionName)
        {
            if (!_isRunning) return;
            if (!_waitingForCondition) return;
            
            if (conditionName == _currentCondition)
            {
                // Condition đúng cái đang đợi - dừng arrow/pulse tương ứng
                arrowController?.StopArrowByCondition(conditionName);
                if (conditionName == "ToUseCoinButton")
                {
                    buttonPulse?.StopPulse();
                }
                _waitingForCondition = false;
                _currentCondition = "";
                AdvanceDialog();
            }
        }
        
        private void AdvanceDialog()
        {
            _currentDialogIndex++;
            ShowCurrentDialog();
        }
        
        private void AdvanceTutorial()
        {
            _currentTutorialIndex++;
            _currentDialogIndex = 0;
            
            if (CurrentScript != null)
            {
                ShowCurrentDialog();
            }
            else
            {
                EndTutorial();
            }
        }
        
        public void EndTutorial()
        {
            // Dừng tất cả arrow coroutine nếu đang chạy
            arrowController?.StopAllArrows();
            
            // Nếu còn tutorial trong queue, chuyển sang tutorial tiếp theo
            if (_currentTutorialIndex + 1 < tutorials.Count)
            {
                AdvanceTutorial();
                return;
            }
            
            // Đã hết tất cả tutorial - hiển thị dialog hoàn thành
            _showingCompletionDialog = true;
            dialogPanel.Show(completionTitle, completionMessage, TutorialDialogType.Skipable);
            
            // Bật lại các UI
            uiStateManager?.EnableAfterTutorial();
            
            Debug.Log("[TutorialController] All tutorials completed!");
        }
        
        public void ExitTutorial()
        {
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}

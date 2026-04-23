using System;
using Script.Save_Controller;
using Script.Manager.WorldManager;
using Script.Manager.GamePlayManager;
using Script.Game.Mobs.MobsManager;
using Script.Game.Mobs.Subjects;
using Script.Data_Model.Status;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;


namespace Script.GUI_Controller.PauseMenu
{
    public class PauseMenuController : MonoBehaviour
    {


        private GuiCanvasManager guiCanvasManager;
        
        #region Pause menu component

        public Button resumeButton;
        public Button optionBtn;
        public Button saveExitBtn;
        public TextMeshProUGUI reportText;
        
        #endregion
        
        #region Debug
        
        [TextArea(5, 20)]
        public string debugText;
        
        #endregion
        
        private void Start()
        {
            guiCanvasManager = GuiCanvasManager.Instance;
           
            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (optionBtn != null) optionBtn.onClick.AddListener(OpenOption);
            if (saveExitBtn != null) saveExitBtn.onClick.AddListener(SaveAndExit);
        }

        private void OnEnable()
        {
            UpdateReport();
        }

        public void Resume()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            guiCanvasManager.ResumeGame();
        }
        public void OpenMenu()
        {
            guiCanvasManager.ShowPauseMenu();
        }

        private void UpdateReport()
        {
            if (reportText == null) return;

            try
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();

                // TownCenter Coin
                try
                {
                    int coinCount = Script.Utils.WorldUtility.GetTownCenterCoinCount();
                    sb.AppendLine($"TownCenter Coin: {coinCount}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PauseMenuController] Error getting coin count: {e.Message}");
                    sb.AppendLine("TownCenter Coin: Error");
                }

                // World Time
                try
                {
                    var gameTimeManager = FindObjectOfType<GameTimeManager>();
                    if (gameTimeManager != null)
                    {
                        sb.AppendLine($"World Day: {gameTimeManager.GameDay}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PauseMenuController] Error getting world time: {e.Message}");
                }

                // Denizens
                try
                {
                    var denizenManager = FindObjectOfType<Script.Game.Mobs.MobsManager.DenizenManager>();
                    if (denizenManager != null)
                    {
                        int villagerCount = denizenManager.GetDenizensByRole(NPCRole.Villager)?.Count ?? 0;
                        int vagrantCount = denizenManager.GetDenizensByRole(NPCRole.Vagrant)?.Count ?? 0;
                        int archerCount = denizenManager.GetDenizensByRole(NPCRole.Archer)?.Count ?? 0;
                        sb.AppendLine($"Villagers: {villagerCount}");
                        sb.AppendLine($"Vagrants: {vagrantCount}");
                        sb.AppendLine($"Archers: {archerCount}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PauseMenuController] Error getting denizen count: {e.Message}");
                }

                // Walls (Left/Right)
                try
                {
                    int leftWalls = Script.Utils.WorldUtility.GetHealthyWallCount(true);
                    int rightWalls = Script.Utils.WorldUtility.GetHealthyWallCount(false);
                    sb.AppendLine($"Walls (Left/Right): {leftWalls}/{rightWalls}");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PauseMenuController] Error getting wall count: {e.Message}");
                    sb.AppendLine("Walls (Left/Right): Error");
                }

                // Portals
                try
                {
                    var (leftActive, leftDays) = Script.Utils.WorldUtility.GetPortalInfo(false);
                    var (rightActive, rightDays) = Script.Utils.WorldUtility.GetPortalInfo(true);
                    
                    if (leftActive)
                        sb.AppendLine("Left Portal: Active");
                    else
                        sb.AppendLine($"Left Portal: Not Active (cooldown: {leftDays} days)");

                    if (rightActive)
                        sb.AppendLine("Right Portal: Active");
                    else
                        sb.AppendLine($"Right Portal: Not Active (cooldown: {rightDays} days)");
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PauseMenuController] Error getting portal info: {e.Message}");
                    sb.AppendLine("Portal Info: Error");
                }

                // Wave Power
                try
                {
                    var waveManager = EnemyWaveManager.Instance;
                    var gameTimeManager = FindObjectOfType<GameTimeManager>();
                    if (waveManager != null && gameTimeManager != null)
                    {
                        int daysElapsed = gameTimeManager.GetElapsedDays();
                        int wavePower = daysElapsed * 6;
                        sb.AppendLine($"Wave Power: {wavePower}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[PauseMenuController] Error getting wave power: {e.Message}");
                }

                reportText.text = sb.ToString();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[PauseMenuController] Critical error in UpdateReport: {e.Message}\n{e.StackTrace}");
                reportText.text = "Error loading report data";
            }
        }

        [ContextMenu("Debug Update Report")]
        private void DebugUpdateReport()
        {
            Debug.Log("[PauseMenuController] Debug: Starting debug check...");
            
            // Check reportText assignment
            if (reportText == null)
            {
                Debug.LogWarning("[PauseMenuController] Debug: reportText is NULL! Please assign it in Inspector.");
            }
            else
            {
                Debug.Log($"[PauseMenuController] Debug: reportText is assigned to: {reportText.name}");
            }
            
            // Check managers
            var gameTimeManager = FindObjectOfType<GameTimeManager>();
            Debug.Log($"[PauseMenuController] Debug: GameTimeManager found: {gameTimeManager != null}");
            
            var denizenManager = FindObjectOfType<Script.Game.Mobs.MobsManager.DenizenManager>();
            Debug.Log($"[PauseMenuController] Debug: DenizenManager found: {denizenManager != null}");
            
            var waveManager = EnemyWaveManager.Instance;
            Debug.Log($"[PauseMenuController] Debug: EnemyWaveManager.Instance found: {waveManager != null}");
            
            var portalManager = Script.Manager.GamePlayManager.EnemyPortalManager.Instance;
            Debug.Log($"[PauseMenuController] Debug: EnemyPortalManager.Instance found: {portalManager != null}");
            
            Debug.Log("[PauseMenuController] Debug: Calling UpdateReport()");
            UpdateReport();
            Debug.Log("[PauseMenuController] Debug: UpdateReport() completed");
            
            if (reportText != null)
            {
                Debug.Log($"[PauseMenuController] Debug: Report text content:\n{reportText.text}");
            }
        }

        [ContextMenu("Debug Test Scroll")]
        private void DebugTestScroll()
        {
            if (reportText == null)
            {
                Debug.LogWarning("[PauseMenuController] Debug: reportText is NULL! Please assign it in Inspector.");
                return;
            }
            
            if (string.IsNullOrEmpty(debugText))
            {
                Debug.LogWarning("[PauseMenuController] Debug: debugText is empty! Please enter text in Inspector.");
                return;
            }
            
            Debug.Log("[PauseMenuController] Debug: Setting debug text to reportText");
            reportText.text = debugText;
            Debug.Log($"[PauseMenuController] Debug: Text set successfully. Length: {debugText.Length} characters");
        }

        public void OpenOption()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            if (guiCanvasManager != null)
            {
                guiCanvasManager.ShowSoundSetting();
            }
        }

        public void SaveAndExit()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            WorldFileManager.Instance.SaveCurrentMap();
            Script.Audio.AudioManager.Instance?.PauseNonUISounds(true);
            SceneManager.LoadScene("MainMenuScene");
        }
    }
}

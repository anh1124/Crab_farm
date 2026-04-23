using System;
using Script.Save_Controller;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

using Script.GUI_Controller.StartMenu;
using Script.Data_Model;
using Script.Manager.SaveManager;

namespace Script.GUI_Controller.StartMenu
{
    public class StartMenuController : MonoBehaviour
    {
        public WorldInfo worldInfo;
        [Header("homeMenuPanel")]
        public GameObject homeMenuPanel;//pannel name
        public TextMeshProUGUI versionText;
        
        public Button buttonSetting;
        public Button buttonOpenChooseSavePanel;
        public Button buttonInfo;
        
        [Header("chooseSavePanel")]
        public GameObject chooseSavePanel;//pannel name
    
        //back ground btn
        public Button chooseSavePanelBackBtn;
        public Button playBtn;
        public Button newGame;
        
        //save info
        public GameObject emptySaveTMP;//cái này sẽ setactive = true nếu ko tìm dc save nào
    
        //các thông số sẽ hiện ra nếu tìm được thế giới
        public TextMeshProUGUI worldName;
        public TextMeshProUGUI worldPlayTime;
    
        public Button deleteSaveBtn;
        public Button nextSaveBtn;
        public Button preSaveBtn;
        public Button openWorldInfoPanelBtn;

        public GameObject worldInfoContainer;
        
        public int currentWorldIndex ;// thế giới người chơi đang nhìn\
        public int totalWorld = 0;
        // việc  chuyển currentWorldIndex = 0 thực hiện ở nút mở save
        
        public TextMeshProUGUI totalWorldTmp;//tổng số save hiện tại
        [Header("ShowWorldInfoPanel")] 
        public GameObject showWorldInfoPanel;
        public Button exitShowWorldInfoPanel;
        public TextMeshProUGUI WorldInfoContent;
        
        [Header("optionsPanel")]
        public GameObject optionsPanel;//pannel name

        public Button optionsPanelBackBtn;

        [Header("CreateNewWorldPanel")] 
        private bool hasClickedInputWorldName = false;// Biến để kiểm tra xem người dùng đã nhấp vào inputWorldName chưa

        public GameObject createNewWorldPanel;

        public TMP_InputField inputWorldName;
        public TMP_InputField inputWorldSeed;
        
        public Button createNewWorldBtn;
        public Button backChooseSavePanel;


        public GameObject confirmationPanel;
      
        public static StartMenuController Instance { get; set; }
        
        
        private void Start()
        {
            if (WorldSaveListManager.Instance == null)
            {
                Debug.LogError("[StartMenuController] WorldSaveListManager không tìm thấy trong scene! Hãy thêm nó vào MainMenuScene.");
            }

            // OpenHomeMenuPanel(); // Không gọi ở Start() để tránh phát nhạc khi scene load
            SetActiveFalseAllPanel();
            homeMenuPanel.SetActive(true); // Active home menu thay vì gọi OpenHomeMenuPanel()

            AssignButtonActions();
            //chạy hàm kiểm tra và tạo thư mục lưu file saves
            if (WorldSaveListManager.Instance != null)
            {
                WorldSaveListManager.Instance.CreateSavesFolderIfNotExists();
            }
            
            inputWorldSeed.characterLimit = 10;
            inputWorldSeed.contentType = TMP_InputField.ContentType.IntegerNumber;
            
            // Gọi hàm auto điền khi mở CreateNewWorldPanel
            newGame.onClick.AddListener(() =>
            {
                OpenCreateNewGamePanel();
                AutoFillWorldName(); // Tự động điền tên
                hasClickedInputWorldName = false; // Đặt lại trạng thái
            });
            // Đăng ký sự kiện click vào inputWorldName
            inputWorldName.onSelect.AddListener(delegate { OnInputWorldNameClick(); });
        }
    
        public void AssignButtonActions()
        {
            //gán nút
            buttonOpenChooseSavePanel.onClick.AddListener(OpenChooseSavePanel);
            buttonSetting.onClick.AddListener(OpenOptionsPanel);
            //change save
            nextSaveBtn.onClick.AddListener(NextSave);
            preSaveBtn.onClick.AddListener(PreSave);
            //btn back
            chooseSavePanelBackBtn.onClick.AddListener(OpenHomeMenuPanel);
            optionsPanelBackBtn.onClick.AddListener(OpenHomeMenuPanel);
            backChooseSavePanel.onClick.AddListener(OpenChooseSavePanel);

            newGame.onClick.AddListener(OpenCreateNewGamePanel);

            openWorldInfoPanelBtn.onClick.AddListener(OpenShowWorldInfoPanel);
            //btn in show world info panel
            exitShowWorldInfoPanel.onClick.AddListener(ExitWorldInfoPanel);

            //btn create new world
            createNewWorldBtn.onClick.AddListener(createNewWorldAction);
            deleteSaveBtn.onClick.AddListener(DeleteCurrentWorldSave);

            //truyền save path đến screenGamePlay
            //đối với save đã có sẵn
            playBtn.onClick.AddListener(() => LoadSelectedWorldAndTransitionToGame(worldName.text));
            //đối với save được tạo bằng createBtn thì được ghi bên trong luôn
        }

        public void LoadSelectedWorldAndTransitionToGame(string worldName)
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            if (WorldSaveListManager.Instance == null) return;
            string worldFolderPath = WorldSaveListManager.Instance.GetWorldFolderPath(worldName);

            if (worldFolderPath != null)
            {
                WorldSaveListManager.Instance.SetActiveWorldPath(worldFolderPath);
                SceneManager.LoadScene("GamePlayScene");
            }
           else Debug.LogError("StartMenuController: world folder path is not found");
        }
        
        public void createNewWorldAction()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            if (WorldSaveListManager.Instance == null) return;
            bool createWorldDone = false;
            if (string.IsNullOrEmpty(inputWorldSeed.text))
            {
                inputWorldSeed.text = GenerateRandomSeed();
            }
            createWorldDone = WorldSaveListManager.Instance.CreateWorldSave(inputWorldName.text,inputWorldSeed.text);//tạo thế giới mới dựa theo tên truyền vào
            //createWorldDone sẽ true nếu tạo thành công

            if (createWorldDone)
            {
                //mở thế giới vừa được tạo ra
                LoadSelectedWorldAndTransitionToGame(inputWorldName.text);
            }
            //ở hàm WorldSaveListManager.Instance.CreateWorldSave đã có sẵn debug khi sai rồi nên không cần tạo nữa
        }
        // Hàm tạo số ngẫu nhiên gồm 8 chữ số
        private string GenerateRandomSeed()
        {
            System.Random random = new System.Random();
            return random.Next(00000000, 99999999).ToString();
        }
        public void PreSave()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            int preIndex = currentWorldIndex;//khởi tạo số trước giảm 1 đơn vị
            preIndex--;
            if (preIndex <= 0)//nếu số trước = 0
            {
                currentWorldIndex = 1;
            }
            else
            {
                currentWorldIndex = preIndex;
            }
            //Debug.Log("Button PreSave clicked!");
            DisplayWorldInfo();
            UpdateTotalWorldTmp();
        }
        public void NextSave()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            int nextIndex = currentWorldIndex;
            nextIndex++;
            if (nextIndex >= totalWorld)
            {
                currentWorldIndex = totalWorld;
            }
            else
            {
                currentWorldIndex = nextIndex;
            }
            //Debug.Log("Button NextSave clicked!");
            DisplayWorldInfo();
            UpdateTotalWorldTmp();
        }
        
        public void DisplayWorldInfo()
        {
            if (WorldSaveListManager.Instance == null) return;
            worldInfo = WorldSaveListManager.Instance.GetWorldInfoByIndex(currentWorldIndex);

            if (worldInfo != null)
            {
                worldName.text = worldInfo.Name;
                worldPlayTime.text = worldInfo.CreationDate.ToString();
                //Debug.Log($"DisplayWorldInfo: World Name: {worldInfo.Name}, Creation Date: {worldInfo.CreationDate}");
            }
            else
            {
                Debug.Log("DisplayWorldInfo: No world info found for the given index.");
            }
        }
        //cập nhật tổng số thế giới=====================
        public void UpdateTotalWorld(int a)
        {
            totalWorld = a;
            UpdateTotalWorldTmp();
        }
        public void UpdateCurrentWorldIndex(int a)
        {
            currentWorldIndex = a;
            UpdateTotalWorldTmp();
        }
        public void UpdateTotalWorldTmp()
        {
            totalWorldTmp.text = "World " + currentWorldIndex.ToString() + "/" + totalWorld.ToString();
        }
        
        //===============================================

        public void SetActiveFalseAllPanel()
        {
            homeMenuPanel.SetActive(false);
            chooseSavePanel.SetActive(false);
            optionsPanel.SetActive(false);
            createNewWorldPanel.SetActive(false);
            showWorldInfoPanel.SetActive(false);
            if (confirmationPanel != null) confirmationPanel.SetActive(false);
        }

        #region Open panel metho
        public void OpenShowWorldInfoPanel()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            if (worldInfo != null)
            {
                showWorldInfoPanel.SetActive(true);
                LoadWorldInfoContent();
            }
        }

        public void ExitWorldInfoPanel()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            showWorldInfoPanel.SetActive(false);
        }

        public void LoadWorldInfoContent()
        {
            // Lấy thông tin thế giới hiện tại theo chỉ số
            //worldInfo = WorldFileManager.Instance.GetWorldInfoByIndex(currentWorldIndex);

           
                // Tạo nội dung hiển thị dựa trên dữ liệu của WorldInfo
                string worldInfoText = $"World Name: {worldInfo.Name}\n" +
                                       $"Seed: {worldInfo.Seed}\n" +
                                       $"Creation Date: {worldInfo.CreationDate}\n" +
                                       $"Game Time: {worldInfo.GameHour:D2}:{worldInfo.GameMinute:D2}\n" +
                                       $"Game Date: {worldInfo.GameDay}/{worldInfo.GameMonth}/{worldInfo.GameYear}";

                // Hiển thị nội dung lên TextMeshProUGUI
                WorldInfoContent.text = worldInfoText;
                Debug.Log($"LoadWorldInfo: {worldInfoText}");
        }

        public void OpenHomeMenuPanel()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            SetActiveFalseAllPanel();
            homeMenuPanel.SetActive(true);
        }

        public void OpenCreateNewGamePanel()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            SetActiveFalseAllPanel();
            createNewWorldPanel.SetActive(true);
            ClearCreateNewGamePanel();
        }

        public void ClearCreateNewGamePanel()
        {
            inputWorldName.text = "";
            inputWorldSeed.text = "";
        }

        public void OpenChooseSavePanel()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            EmptyWorldSave();//mặc định là không tìm thấy thế giới
            currentWorldIndex = 1;//đặt bộ đếm thế giới hiện tại
            SetActiveFalseAllPanel();
            chooseSavePanel.SetActive(true);

            if (WorldSaveListManager.Instance == null) return;

            //cập nhật thế tổng số thế giới
            WorldSaveListManager.Instance.LoadWorldInfosToChooseSavePanel();
            UpdateTotalWorld(WorldSaveListManager.Instance.CountWorldArr());
            UpdateCurrentWorldIndex(currentWorldIndex);
            UpdateTotalWorldTmp();
            //hiện thị empty nếu không thấy file lưu thế giới
            if (totalWorld != 0)
            {
                FoundWorldSave();
                DisplayWorldInfo();
            }
            else
            {
                EmptyWorldSave();
            }
        }
        public void OpenOptionsPanel()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            SetActiveFalseAllPanel();
            optionsPanel.SetActive(true);

            // Refresh all volume sliders to ensure they display correct saved values
            RefreshVolumeSliders();
        }

        private void RefreshVolumeSliders()
        {
            var sliders = optionsPanel.GetComponentsInChildren<Script.Audio.AudioVolumeSlider>(true);
            foreach (var slider in sliders)
            {
                slider.Refresh();
            }
        }
        //Cập nhật lại giao diện nếu tìm thấy thế giới


        #endregion
        
        public void EmptyWorldSave()
        {
            emptySaveTMP.SetActive(true);
            worldInfoContainer.SetActive(false);
        }

        public void FoundWorldSave()
        {
            emptySaveTMP.SetActive(false);
            worldInfoContainer.SetActive(true);
        }

        
        public void DeleteCurrentWorldSave()
        {
            // Play UI click sound
            Script.Audio.AudioManager.Instance?.PlayUISound("UI_Click");

            if (confirmationPanel == null)
            {
                Debug.LogError("confirmationPanel is null! Please assign the scene instance in the Inspector.");
                return;
            }

            // Lấy component ConfirmationMenu từ instance đã có sẵn trong scene
            ConfirmationMenu confirmationMenu = confirmationPanel.GetComponent<ConfirmationMenu>();
            if (confirmationMenu != null)
            {
                // Active panel lên (nếu ConfirmationMenu.Initialize chưa làm việc này cho root object)
                confirmationPanel.SetActive(true);

                // Hiển thị menu xác nhận
                confirmationMenu.Show(
                    "Delete World",
                    $"Are you sure you want to delete the world \"{worldName.text}\"?",
                    () => PerformDelete(worldName.text) // Hàm được gọi khi xác nhận
                );
            }
            else
            {
                Debug.LogError("ConfirmationMenu component not found on the confirmation panel!");
            }
        }
        
        private void PerformDelete(string nameOfChooseWorld)
        {
            if (WorldSaveListManager.Instance == null) return;
            
            // Thực hiện xóa thế giới ở đây
            WorldSaveListManager.Instance.DeleteWorldSave(nameOfChooseWorld);
            // Cập nhật danh sách thế giới sau khi xóa
            WorldSaveListManager.Instance.LoadWorldInfosToChooseSavePanel();
            UpdateTotalWorld(WorldSaveListManager.Instance.CountWorldArr());

            if (totalWorld == 0)
            {
                EmptyWorldSave();
            }
            else
            {
                currentWorldIndex = 1;
                DisplayWorldInfo();
                UpdateTotalWorldTmp();
            }
            Debug.Log($"World \"{nameOfChooseWorld}\" has been deleted.");
        }

        private void AutoFillWorldName()
        {
            string basePath = @"C:/Users/Acer/AppData/LocalLow/DefaultCompany/Crab_farm/saves";
            string baseName = "new_world";
            int index = 1;

            string potentialName = baseName;
            while (System.IO.Directory.Exists(System.IO.Path.Combine(basePath, potentialName)))
            {
                potentialName = $"{baseName} {index}";
                index++;
            }

            inputWorldName.text = potentialName;
        }
        // Hàm xử lý khi người dùng click vào inputWorldName
        private void OnInputWorldNameClick()
        {
            if (!hasClickedInputWorldName)
            {
                inputWorldName.text = ""; // Xóa nội dung cũ
                hasClickedInputWorldName = true; // Đánh dấu đã click
            }
        }

    }
}

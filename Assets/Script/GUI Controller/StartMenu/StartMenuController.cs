using System;
using Script.Save_Controller;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Script.GUI_Controller.StartMenu
{
    public class StartMenuController : MonoBehaviour
    {
        [Header("homeMenuPanel")]
        public GameObject homeMenuPanel;//pannel name
        public TextMeshProUGUI versionText;
        
        public Button buttonSetting;
        [FormerlySerializedAs("buttonPlay")] public Button buttonOpenChooseSavePanel; 
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
    
        public Button deleteSave;
        public Button nextSave;
        public Button preSave;
        public Button worldInfo;

        public GameObject worldInfoContainer;
        
        public int currentWorldIndex ;// thế giới người chơi đang nhìn\
        public int totalWorld = 0;
        // việc  chuyển currentWorldIndex = 0 thực hiện ở nút mở save
        
        public TextMeshProUGUI totalWorldTmp;//tổng số save hiện tại
    

        
        [Header("optionsPanel")]
        public GameObject optionsPanel;//pannel name

        public Button optionsPanelBackBtn;

        [Header("CreateNewWorldPanel")] 
        public GameObject createNewWorldPanel;

        public TMP_InputField inputWorldName;
        public TMP_InputField inputWorldSeed;
        
        public Button createNewWorldBtn;
        public Button backChooseSavePanel;
        
        public static StartMenuController Instance { get; set; }

        private void Start()
        {
            OpenHomeMenuPanel();
            AssignButtonActions();
            //chạy hàm kiểm tra và tạo thư mục lưu file saves
            SaveManager.Instance.CreateSavesFolderIfNotExists();
            
            inputWorldSeed.characterLimit = 10;
            inputWorldSeed.contentType = TMP_InputField.ContentType.IntegerNumber;
        }
    
        public void AssignButtonActions()
        {
            //gán nút
            buttonOpenChooseSavePanel.onClick.AddListener(OpenChooseSavePanel);
            buttonSetting.onClick.AddListener(OpenOptionsPanel);
            //change save
            nextSave.onClick.AddListener(NextSave);
            preSave.onClick.AddListener(PreSave);
            //btn back
            chooseSavePanelBackBtn.onClick.AddListener(OpenHomeMenuPanel);
            optionsPanelBackBtn.onClick.AddListener(OpenHomeMenuPanel);
            backChooseSavePanel.onClick.AddListener(OpenChooseSavePanel);
            //nuts info chưa làm
            newGame.onClick.AddListener(OpenCreateNewGamePanel);
            //btn create new world
            createNewWorldBtn.onClick.AddListener(createNewWorldAction);
            deleteSave.onClick.AddListener(DeleteCurrentWorldSave);
            
            //truyền save path đến screenGamePlay
            //đối với save đã có sẵn 
            playBtn.onClick.AddListener(() => SendDataAndOpenPlayScreend(worldName.text));
            //đối với save được tạo bằng createBtn thì được ghi bên trong luôn 
        }

        public void SendDataAndOpenPlayScreend(string name)
        {
            PlayerPrefs.SetString("SaveFilePath",  SaveManager.Instance.GetWorldFolderPath(name)); 
            SceneManager.LoadScene("GamePlayScene");
        }
        public void createNewWorldAction()
        {
            if (inputWorldName != null)
            {
                SaveManager.Instance.CreateWorldSave(inputWorldName.text,inputWorldSeed.text);//tạo thế giới mới dựa theo tên truyền vào
                //mở thế giới vừa được tạo ra 
                SendDataAndOpenPlayScreend(inputWorldName.text);
            }
            
           
        }
        public void PreSave()
        {
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
            SaveManager.WorldInfo worldInfo = SaveManager.Instance.GetWorldInfoByIndex(currentWorldIndex);

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
        /*
        int currentWorldIndex 
        int totalWorld = 0;
        tmp totalWorldTMP;
         */
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
        }

        #region Open panel metho

        public void OpenHomeMenuPanel()
        {
            SetActiveFalseAllPanel();
            homeMenuPanel.SetActive(true);
        }

        public void OpenCreateNewGamePanel()
        {
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
            EmptyWorldSave();//mặc định là không tìm thấy thế giới
            currentWorldIndex = 1;//đặt bộ đếm thế giới hiện tại
            SetActiveFalseAllPanel();
            chooseSavePanel.SetActive(true);
            //cập nhật thế tổng số thế giới
            SaveManager.Instance.LoadWorldInfosToChooseSavePanel();
            UpdateTotalWorld(SaveManager.Instance.CountWorldArr());
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
            SetActiveFalseAllPanel();
            optionsPanel.SetActive(true);
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
            string worldNameToDelete = worldName.text;
            SaveManager.Instance.DeleteWorldSave(worldNameToDelete);

            // Cập nhật danh sách thế giới sau khi xóa
            SaveManager.Instance.LoadWorldInfosToChooseSavePanel();
            UpdateTotalWorld(SaveManager.Instance.CountWorldArr());

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
        }

    }
}

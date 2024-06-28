using System;
using Script.Save_Controller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Script.GUI_Controller.StartMenu
{
    public class StartMenuController : MonoBehaviour
    {
        [Header("homeMenuPanel")]
        public GameObject homeMenuPanel;//pannel name
        public TextMeshProUGUI versionText;
        
        public Button buttonSetting;
        public Button buttonPlay; 
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

        
        
        public static StartMenuController Instance { get; set; }

        private void Start()
        {
            OpenHomeMenuPanel();
            
            //gán nút
            buttonPlay.onClick.AddListener(OpenChooseSavePanel);
            buttonSetting.onClick.AddListener(OpenOptionsPanel);
            //change save
            nextSave.onClick.AddListener(NextSave);
            preSave.onClick.AddListener(PreSave);
            //btn back
            chooseSavePanelBackBtn.onClick.AddListener(OpenHomeMenuPanel);
            optionsPanelBackBtn.onClick.AddListener(OpenHomeMenuPanel);
            //nuts info chưa làm
            
            
            //chạy hàm kiểm tra và tạo thư mục lưu file saves
            SaveManager.Instance.InitializeSaves();
        }

        public void PreSave()
        {
            Debug.Log("Button PreSave clicked!");
        }

        public void NextSave()
        {
            Debug.Log("Button NextSave clicked!");
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
        }
        
        public void OpenHomeMenuPanel()
        {
            
            SetActiveFalseAllPanel();
            homeMenuPanel.SetActive(true);
        }
        public void OpenChooseSavePanel()
        {
            EmptyWorldSave();//mặc định là không tìm thấy thế giới
            currentWorldIndex = 1;//đặt bộ đếm thế giới hiện tại
            SetActiveFalseAllPanel();
            chooseSavePanel.SetActive(true);
            //cập nhật thế tổng số thế giới
            SaveManager.Instance.LoadWorldInfos();
            UpdateTotalWorld(SaveManager.Instance.CountWorldArr());
            UpdateCurrentWorldIndex(currentWorldIndex);
            UpdateTotalWorldTmp();
            //hiện thị empty nếu không thấy file lưu thế giới
            if (totalWorld != 0)
            {
                FoundWorldSave();
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
        
    }
}

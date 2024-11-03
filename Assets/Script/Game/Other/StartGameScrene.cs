using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StartGameScrene : MonoBehaviour
{
    
    public void Start()
    {
        //đường dẫn mặc định để test
        LoadFromSave.Instance.saveFilePath = @"C:/Users/Acer/AppData/LocalLow/DefaultCompany/Crab_farm\saves\holyCrab";
       
        //LoadFromSave.Instance.saveFilePath = PlayerPrefs.GetString("SaveFilePath", "");
        LoadFromSave.Instance.DoLoadFromSave();
        GameTimeManager.Instance.SetGameTimeSave();
        //instancePlayer
        //activeCamera

        //wakeUpOtherCharactor

    }
}

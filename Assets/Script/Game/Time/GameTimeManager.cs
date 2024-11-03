using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Script.Save_Controller;
using UnityEngine;
using TMPro;
using System;

public class GameTimeManager : MonoBehaviour
{
    public static GameTimeManager Instance { get; private set; }
    public TextMeshProUGUI timeDisplay;
    
    public int gameHour = 0;
    public int gameMinute = 0;
    public int gameDay = 1;
    public int gameMonth = 1;
    public int gameYear = 1;

    public float realSecondsPerGameDay = 240f;
    private float timeMultiplier;
    private float elapsedTime = 0f;

    private const int MONTHS_PER_YEAR = 4;
    private const int DAYS_PER_MONTH = 30;

    #region Init Hour Even Action
    public event Action Hour5;
    public event Action Hour6;

    #endregion
    private void Awake()
    {
        AddSingleton();
    }

    public void TimeEvent()
    {
        switch (gameHour)
        {
            case 5:
                Hour5?.Invoke();
                break;
                //
            case 6:
                Hour6?.Invoke(); //đã có
                //bụi cỏ spawm thỏ 
                break;
            case 17:
                // Thêm sự kiện cho 17 giờ chiều
                // Ví dụ: tạo làn sóng quái vật hoặc gọi sự kiện khác
                // Event17?.Invoke();
                break;
            // Thêm các case khác nếu bạn muốn kích hoạt sự kiện vào các giờ khác
            // case <giờ>:
            //      Event?.Invoke();
            //      break;
            default:
                break;
        }
    }

    public void AddSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        timeMultiplier = 86400f / realSecondsPerGameDay;
       // UpdateTimeDisplay();
    }

    void Update()
    {
        UpdateGameTime();
        //ở đây gọi các hàm như trời tối 
        UpdateTimeDisplay();
    }

    void UpdateGameTime()
    {
        elapsedTime += Time.deltaTime * timeMultiplier;

        if (elapsedTime >= 60f)
        {
            gameMinute += (int)(elapsedTime / 60);
            elapsedTime = 0f;

            if (gameMinute >= 60)
            {
                gameMinute = 0;
                gameHour++;
                //timeEvent được gọi thi giờ được cật nhật 
               TimeEvent();//gọi các event khi đến time được đăng ký 

                if (gameHour >= 24)
                {
                    gameHour = 0;
                    gameDay++;
                    CheckNewMonth();
                }
            }
        }
    }

    void CheckNewMonth()
    {
        if (gameDay > DAYS_PER_MONTH)
        {
            gameDay = 1;
            gameMonth++;

            if (gameMonth > MONTHS_PER_YEAR)
            {
                gameMonth = 1;
                gameYear++;
            }
        }
    }

   
    public void SetGameTime(int hour, int minute, int day, int month, int year)
    {
        gameHour = Mathf.Clamp(hour, 0, 23);
        gameMinute = Mathf.Clamp(minute, 0, 59);
        gameDay = Mathf.Clamp(day, 1, DAYS_PER_MONTH);
        gameMonth = Mathf.Clamp(month, 1, MONTHS_PER_YEAR);
        gameYear = year;
        UpdateTimeDisplay();
    }

    public void SetGameTimeSave()
    {
        string saveFilePath = LoadFromSave.Instance.saveFilePath;
        string worldInfoPath = Path.Combine(saveFilePath, "worldInfo.json");
        // Kiểm tra nếu file JSON tồn tại
        if (File.Exists(worldInfoPath))
        {
            
            // Đọc dữ liệu từ file JSON
            string json = File.ReadAllText(worldInfoPath);

            SaveManager.WorldInfo worldInfo = JsonConvert.DeserializeObject<SaveManager.WorldInfo>(json);
            
            SetGameTime(worldInfo.GameHour,worldInfo.GameMinute,worldInfo.GameDay,worldInfo.GameMonth,worldInfo.GameYear);
            Debug.Log($"Loaded WorldInfo: Name={worldInfo.Name}, GameTime={worldInfo.GameHour}:{worldInfo.GameMinute}, Date={worldInfo.GameDay}/{worldInfo.GameMonth}/{worldInfo.GameYear}");
        }
        else
        {
            Debug.LogError("File worldInfo.json không tồn tại trong thư mục " + saveFilePath);
            Debug.LogError("hệ thống sẽ khởi tạo Time mặc định " );
            SetGameTime(0,0,0,0,0);
        }
    }

    public string GetGameTime()
    {
        return string.Format("{0:D2}:{1:D2} - {2}/{3}/{4}", gameHour, gameMinute, gameDay, gameMonth, gameYear);
    }

    void UpdateTimeDisplay()
    {
        if (timeDisplay != null)
        {
            timeDisplay.text = GetGameTime();
        }
    }
    
    public int GetTotalDaysElapsed()
    {
        // Tính tổng số ngày của các năm trước (giả sử mỗi năm có 4 tháng x 30 ngày)
        int daysInPreviousYears = (gameYear - 1) * (MONTHS_PER_YEAR * DAYS_PER_MONTH);

        // Tính tổng số ngày của các tháng trước trong năm hiện tại
        int daysInPreviousMonths = (gameMonth - 1) * DAYS_PER_MONTH;

        // Tổng số ngày đã qua bao gồm tháng hiện tại
        int totalDaysElapsed = daysInPreviousYears + daysInPreviousMonths + (gameDay - 1);

        return totalDaysElapsed;
    }

}
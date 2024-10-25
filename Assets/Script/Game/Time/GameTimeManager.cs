using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    private void Awake()
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
        UpdateTimeDisplay();
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
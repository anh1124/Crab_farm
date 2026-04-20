using System;
using System.Collections;
using System.Collections.Generic;
using Script.Save_Controller;
using UnityEngine;
using TMPro;
using Script.Manager.SaveManager;
namespace Script.Manager.WorldManager
{
    [RequireComponent(typeof(AutoSaveManager))]
public class GameTimeManager : MonoBehaviour, IGameTimeProvider
{
    public TextMeshProUGUI timeDisplay;

    // Time properties with auto-correction
    private int _totalMinutes;

    [Header("Chỉnh thời gian để test (Inspector)")]
    [SerializeField] private int inspectorHour;
    [SerializeField] private int inspectorMinute;
    [SerializeField] private int inspectorDay = 1;
    [SerializeField] private int inspectorMonth = 1;
    [SerializeField] private int inspectorYear = 1;

    public const int MORNING_HOUR = 6;
    /// <summary>12:00 game — dùng cho sự kiện theo giờ (ví dụ hồi máu tường).</summary>
    public const int NOON_HOUR = 12;
    public const int NIGHT_HOUR = 19;

    public int GameHour => _totalMinutes / 60 % 24;
    public int GameMinute => _totalMinutes % 60;
    public int GameDay => (_totalMinutes / 1440 % 30) + 1;
    public int GameMonth => (_totalMinutes / 43200 % 4) + 1;
    public int GameYear => (_totalMinutes / 172800) + 1;

    [SerializeField] private float realSecondsPerGameDay = 240f;

    [Header("Tăng tốc thời gian (Debug)")]
    [Tooltip("Hệ số tốc độ thời gian.\n" +
             "0   = Đóng băng (dừng thời gian)\n" +
             "0.5 = Chậm gấp đôi (ngày dài hơn)\n" +
             "1   = Bình thường\n" +
             "5   = Nhanh x5")]
    [SerializeField] [Range(0f, 5f)] private float gameTimeScale = 1f;

    private float _secondsPerMinute;
    private float _accumulatedTime; // Tích lũy giây thực để tính phút game

    // Event system
    private readonly Dictionary<int, Action> _hourlyEvents = new();
    private int _lastProcessedHour = -1;

    #if UNITY_EDITOR
    [Header("Debug: Các event đã đăng ký theo giờ (chỉ đọc)")]
    [SerializeField] private List<string> hourlyEventDebug = new List<string>();
    #endif

    #region Initialization
    private void Awake() 
    {
        // Singleton pattern removed - initialized via StartGameScrene
    }

    public void InitializeTimeSystem()
    {
        RecalculateSecondsPerMinute();
        _accumulatedTime = 0f;
        StartCoroutine(TimeUpdateCoroutine());
    }

    private void RecalculateSecondsPerMinute()
    {
        // gameTimeScale = 0: đóng băng → dùng giá trị lớn để không bao giờ tiến
        float scale = Mathf.Max(gameTimeScale, 0.001f);
        _secondsPerMinute = (realSecondsPerGameDay / 1440f) / scale;
    }
    #endregion

    #region Event Management
    
    private void CheckForHourlyEvent()
    {
        if (GameHour == _lastProcessedHour) return;
        
        if (_hourlyEvents.TryGetValue(GameHour, out Action action))
        {
            action?.Invoke();
        }
        _lastProcessedHour = GameHour;
    }

    private void UpdateHourlyEventDebug()
    {
    #if UNITY_EDITOR
        hourlyEventDebug.Clear();
        foreach (var kvp in _hourlyEvents)
        {
            if (kvp.Value != null)
            {
                var delegates = kvp.Value.GetInvocationList();
                foreach (var del in delegates)
                {
                    string methodName = del.Method.Name;
                    string className = del.Target != null ? del.Target.GetType().Name : "(static)";
                    hourlyEventDebug.Add($"Giờ: {kvp.Key} - {className}.{methodName}");
                }
            }
            else
            {
                hourlyEventDebug.Add($"Giờ: {kvp.Key} - Không có event");
            }
        }
    #endif
    }

    public void SubscribeToHourlyEvent(int hour, Action callback)
    {
        if (_hourlyEvents.ContainsKey(hour))
            _hourlyEvents[hour] += callback;
        else
            _hourlyEvents[hour] = callback;
        UpdateHourlyEventDebug();
    }
    public void UnsubscribeFromHourlyEvent(int hour, Action callback)
    {
        if (_hourlyEvents.ContainsKey(hour))
        {
            _hourlyEvents[hour] -= callback;
            if (_hourlyEvents[hour] == null)
                _hourlyEvents.Remove(hour);
        }
        UpdateHourlyEventDebug();
    }
    #endregion

    #region Time Calculation
    public void AddMinutes(int minutes)
    {
        _totalMinutes += minutes;
        UpdateInspectorFieldsFromTotalMinutes();
        UpdateTimeDisplay();
    }

    public bool IsDaytime => GameHour >= MORNING_HOUR && GameHour < NIGHT_HOUR;

    private IEnumerator TimeUpdateCoroutine()
    {
        while (true)
        {
            yield return null; // Tick mỗi frame
            
            // [ANTI-CHEAT] Nếu chưa có TownCenter hoặc Tier 0 -> đóng băng thời gian
            if (Script.Utility.GetTownCenterTier() < 1) continue;

            // Cập nhật lại _secondsPerMinute mỗi frame để hệ số thay đổi có hiệu lực ngay
            RecalculateSecondsPerMinute();

            _accumulatedTime += Time.deltaTime;

            // gameTimeScale = 0 → đóng băng thời gian, không cộng dồn
            if (gameTimeScale <= 0f) continue;

            // Tính số phút game cần tiến theo thời gian tích lũy
            int minutesToAdvance = Mathf.FloorToInt(_accumulatedTime / _secondsPerMinute);
            if (minutesToAdvance <= 0) continue;

            _accumulatedTime -= minutesToAdvance * _secondsPerMinute;

            int hourBefore = GameHour;
            _totalMinutes += minutesToAdvance;

            UpdateInspectorFieldsFromTotalMinutes();
            UpdateTimeDisplay();

            // Kích hoạt tất cả event của các giờ bị bỏ qua khi tốc độ cao
            FireHourlyEventsForSkippedHours(hourBefore, minutesToAdvance);
        }
    }

    /// <summary>
    /// Đảm bảo không bỏ sót event của bất kỳ giờ nào kể cả khi skip nhiều giờ 1 tick.
    /// </summary>
    private void FireHourlyEventsForSkippedHours(int hourBefore, int minutesAdvanced)
    {
        // Số giờ thực sự đã trôi qua trong tick này
        int hoursAdvanced = minutesAdvanced / 60;
        if (hoursAdvanced <= 0)
        {
            // Vẫn trong cùng giờ, kiểm tra bình thường
            CheckForHourlyEvent();
            return;
        }

        // Kích hoạt từng giờ bị bỏ qua theo thứ tự
        for (int i = 1; i <= hoursAdvanced; i++)
        {
            int skippedHour = (hourBefore + i) % 24;
            if (_hourlyEvents.TryGetValue(skippedHour, out Action action))
            {
                action?.Invoke();
            }
        }

        _lastProcessedHour = GameHour;
    }
    public int GetElapsedDays()
    {
        return _totalMinutes / 1440;
    }
    public void SetGameTimeFromSave(Script.Save_Controller.WorldInfo worldInfo)
    {
        if (worldInfo == null)
        {
            Debug.LogWarning("[GameTimeManager] No worldInfo provided for setup!");
            return;
        }

        // Convert saved time to total minutes
        int totalMinutes = (worldInfo.GameYear - 1) * 172800 + 
                           (worldInfo.GameMonth - 1) * 43200 + 
                           (worldInfo.GameDay - 1) * 1440 + 
                           worldInfo.GameHour * 60 + 
                           worldInfo.GameMinute;

        SetGameTime(totalMinutes);
        
        Debug.Log($"[GameTimeManager] Time Setup: " +
                  $"{worldInfo.GameHour}:{worldInfo.GameMinute}, " +
                  $"{worldInfo.GameDay}/{worldInfo.GameMonth}/{worldInfo.GameYear}");
    }
    #endregion

    #region Time Display
    private void UpdateTimeDisplay()
    {
        if (timeDisplay == null) return;
        
        timeDisplay.text = $"Day {GameDay}";
    }
    #endregion

    #region Save/Load Integration
    public void SetGameTime(int totalMinutes)
    {
        _totalMinutes = Mathf.Max(totalMinutes, 0);
        UpdateInspectorFieldsFromTotalMinutes();
        UpdateTimeDisplay();
        CheckForHourlyEvent(); // Force check sự kiện khi bị nhảy giờ bằng tay
    }
    
    public int GetTotalMinutes => _totalMinutes;

    /// <summary>
    /// Số giây thực tế tương đương 1 ngày game.
    /// CelestialManager dùng giá trị này để drive smooth movement theo Time.deltaTime.
    /// </summary>
    public float SecondsPerDay => realSecondsPerGameDay;

    public bool IsTimeFlowing => gameTimeScale > 0f && Script.Utility.GetTownCenterTier() >= 1;
    #endregion


    private void OnValidate()
    {
        // Khi chỉnh Inspector, cập nhật _totalMinutes
        _totalMinutes = (inspectorYear - 1) * 172800 + (inspectorMonth - 1) * 43200 + (inspectorDay - 1) * 1440 + inspectorHour * 60 + inspectorMinute;
        UpdateTimeDisplay();
    }

    private void UpdateInspectorFieldsFromTotalMinutes()
    {
        inspectorHour = GameHour;
        inspectorMinute = GameMinute;
        inspectorDay = GameDay;
        inspectorMonth = GameMonth;
        inspectorYear = GameYear;
    }

    [ContextMenu("Set Time To Day (Morning - 06:00)")]
    public void SetTimeToMorning()
    {
        // Set time to 06:00 of the CURRENT day
        int currentDayStartMinutes = (GameYear - 1) * 172800 + (GameMonth - 1) * 43200 + (GameDay - 1) * 1440;
        int morningMinutes = currentDayStartMinutes + (MORNING_HOUR * 60); // 6:00 AM
        
        SetGameTime(morningMinutes);
        Debug.Log("Time set to Morning (06:00)");
    }

    [ContextMenu("Set Time To Night (Evening - 19:00)")]
    public void SetTimeToNight()
    {
        // Set time to 19:00 of the CURRENT day
        int currentDayStartMinutes = (GameYear - 1) * 172800 + (GameMonth - 1) * 43200 + (GameDay - 1) * 1440;
        int nightMinutes = currentDayStartMinutes + (NIGHT_HOUR * 60); // 19:00 PM
        
        SetGameTime(nightMinutes);
        Debug.Log("Time set to Night (19:00)");
    }
    [ContextMenu("Set Time To 7h50 ")]
    public void SetTimeTo7h50()
    {
        // Set time to 7:50 of the CURRENT day
        int currentDayStartMinutes = (GameYear - 1) * 172800 + (GameMonth - 1) * 43200 + (GameDay - 1) * 1440;
        int nightMinutes = currentDayStartMinutes + (7 * 60 + 50); // 7:50 AM
        
        SetGameTime(nightMinutes);
        Debug.Log("Time set to 7:50");
    }
}
}
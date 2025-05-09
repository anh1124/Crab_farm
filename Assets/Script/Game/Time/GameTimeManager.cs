using System;
using System.Collections;
using System.Collections.Generic;
using Script.Save_Controller;
using UnityEngine;
using TMPro;

public class GameTimeManager : MonoBehaviour
{
    public static GameTimeManager Instance { get; private set; }
    public TextMeshProUGUI timeDisplay;

    // Time properties with auto-correction
    private int _totalMinutes;

    [Header("Chỉnh thời gian để test (Inspector)")]
    [SerializeField] private int inspectorHour;
    [SerializeField] private int inspectorMinute;
    [SerializeField] private int inspectorDay = 1;
    [SerializeField] private int inspectorMonth = 1;
    [SerializeField] private int inspectorYear = 1;

    public int GameHour => _totalMinutes / 60 % 24;
    public int GameMinute => _totalMinutes % 60;
    public int GameDay => (_totalMinutes / 1440 % 30) + 1;
    public int GameMonth => (_totalMinutes / 43200 % 4) + 1;
    public int GameYear => (_totalMinutes / 172800) + 1;

    [SerializeField] private float realSecondsPerGameDay = 240f;
    private float _secondsPerMinute;

    // Event system
    private readonly Dictionary<int, Action> _hourlyEvents = new();
    private int _lastProcessedHour = -1;

    #if UNITY_EDITOR
    [Header("Debug: Các event đã đăng ký theo giờ (chỉ đọc)")]
    [SerializeField] private List<string> hourlyEventDebug = new List<string>();
    #endif

    private void Awake() => InitializeSingleton();
    private void Start() => InitializeTimeSystem();

    #region Initialization
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
           
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeTimeSystem()
    {
        _secondsPerMinute = realSecondsPerGameDay / 1440f;
  
        StartCoroutine(TimeUpdateCoroutine());
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

    public bool IsDaytime => GameHour >= 6 && GameHour < 18;

    private IEnumerator TimeUpdateCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_secondsPerMinute);
            _totalMinutes++;
            UpdateInspectorFieldsFromTotalMinutes();
            UpdateTimeDisplay();
            CheckForHourlyEvent();
        }
    }
    public int GetElapsedDays()
    {
        return _totalMinutes / 1440;
    }
    public void SetGameTimeFromSave()
    {
        if (LoadFromSave.Instance == null || LoadFromSave.Instance.worldInfo == null)
        {
            Debug.LogWarning("No save data found!");
            return;
        }

        WorldInfo worldInfo = LoadFromSave.Instance.worldInfo;
        
        // Convert saved time to total minutes
        int totalMinutes = (worldInfo.GameYear - 1) * 172800 + 
                           (worldInfo.GameMonth - 1) * 43200 + 
                           (worldInfo.GameDay - 1) * 1440 + 
                           worldInfo.GameHour * 60 + 
                           worldInfo.GameMinute;

        SetGameTime(totalMinutes);
        
        Debug.Log($"Loaded WorldInfo: Name={worldInfo.Name}, " +
                  $"GameTime={worldInfo.GameHour}:{worldInfo.GameMinute}, " +
                  $"Date={worldInfo.GameDay}/{worldInfo.GameMonth}/{worldInfo.GameYear}");
    }
    #endregion

    #region Time Display
    private void UpdateTimeDisplay()
    {
        if (timeDisplay == null) return;
        
        timeDisplay.text = $"{GameHour:D2}:{GameMinute:D2} - {GameDay:D2}/{GameMonth:D2}/{GameYear:D4}";
    }
    #endregion

    #region Save/Load Integration
    public void SetGameTime(int totalMinutes)
    {
        _totalMinutes = Mathf.Max(totalMinutes, 0);
        UpdateInspectorFieldsFromTotalMinutes();
        UpdateTimeDisplay();
    }
    
    public int GetTotalMinutes => _totalMinutes;
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
}
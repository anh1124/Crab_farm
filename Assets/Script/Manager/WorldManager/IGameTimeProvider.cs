using System;

namespace Script.Manager.WorldManager
{
    public interface IGameTimeProvider
    {
        int GameHour { get; }
        int GameMinute { get; }
        int GameDay { get; }
        int GameMonth { get; }
        int GameYear { get; }
        int GetTotalMinutes { get; }
        bool IsDaytime { get; }
        
        void AddMinutes(int minutes);
        void SetGameTime(int totalMinutes);
        void SubscribeToHourlyEvent(int hour, Action callback);
        void UnsubscribeFromHourlyEvent(int hour, Action callback);
        int GetElapsedDays();
    }
}

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
        
        /// <summary> True nếu thời gian đang thực sự trôi (scale > 0 và đủ điều kiện gameplay). </summary>
        bool IsTimeFlowing { get; }
        
        /// <summary> Số giây thực tế tương đương 1 ngày game. </summary>
        float SecondsPerDay { get; }
        
        /// <summary> Hệ số tốc độ thời gian hiện tại (1 = bình thường, 2 = x2, 0.5 = x0.5). </summary>
        float GameTimeScale { get; }
    }
}


using UnityEngine;
using Script.Game.Mobs.Denizen.Core;
using Script.Manager.WorldManager;

namespace Script.Game.Mobs.Subjects
{
    /// <summary>
    /// Component chuyên biệt để quản lý việc đăng ký và hủy đăng ký sự kiện thời gian.
    /// Tách biệt hoàn toàn khỏi logic nghiệp vụ để tránh duplicate listener và dễ bảo trì.
    /// </summary>
    public class DenizenTimeHandler : MonoBehaviour
    {
        private DenizenCore _core;

        private void Awake()
        {
            _core = GetComponent<DenizenCore>();
        }

        private void OnEnable()
        {
            var timeManager = GameTimeManager.Instance;
            if (timeManager != null)
            {
                timeManager.SubscribeToHourlyEvent(6, OnHour6);
                timeManager.SubscribeToHourlyEvent(7, OnHour7);
                timeManager.SubscribeToHourlyEvent(19, OnHour19);
            }
        }

        private void OnDisable()
        {
            var timeManager = GameTimeManager.Instance;
            if (timeManager != null)
            {
                timeManager.UnsubscribeFromHourlyEvent(6, OnHour6);
                timeManager.UnsubscribeFromHourlyEvent(7, OnHour7);
                timeManager.UnsubscribeFromHourlyEvent(19, OnHour19);
            }
        }

        private void OnHour6()
        {
            if (_core != null) _core.OnDayStart();
        }

        private void OnHour7()
        {
            if (_core != null) _core.OnArcherHuntingTime();
        }

        private void OnHour19()
        {
            if (_core != null) _core.OnNightStart();
        }
    }
}

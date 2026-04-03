using System;
using System.Collections.Generic;
using Script.Manager.WorldManager;
using Script.Game.Mobs.Denizen.Core;

namespace Script.Game.Mobs.Denizen.Core
{
    public class DenizenSchedule : DenizenModule
    {
        private List<(int hour, Action action)> _schedules = new List<(int, Action)>();

        public DenizenSchedule(DenizenContext context) : base(context) { }

        public void AddSchedule(int hour, Action action)
        {
            _schedules.Add((hour, action));
        }

        public override void Enable()
        {
            if (GameTimeManager.Instance != null)
            {
                foreach (var schedule in _schedules)
                {
                    GameTimeManager.Instance.SubscribeToHourlyEvent(schedule.hour, schedule.action);
                }
            }
        }

        public override void Disable()
        {
            if (GameTimeManager.Instance != null)
            {
                foreach (var schedule in _schedules)
                {
                    GameTimeManager.Instance.UnsubscribeFromHourlyEvent(schedule.hour, schedule.action);
                }
            }
        }
    }
}

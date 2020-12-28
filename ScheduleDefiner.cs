using System;

namespace ConsoleApp2
{
    partial class Program
    {
        public class ScheduleDefiner
        {
            internal Schedule Schedule { get; private set; } = new Schedule();
            public ScheduleDefiner OnMonthsOfYear(params MonthOfYear[] months)
            {
                Schedule.MonthsOfYear.AddRange(months);
                return this;
            }
            public ScheduleDefiner OnDaysOfMonth(params int[] days)
            {
                Schedule.DaysOfMonth.AddRange(days);
                return this;
            }

            public ScheduleDefiner OnWeekDays(params DayOfWeek[] days)
            {
                Schedule.WeekDays.AddRange(days);
                return this;
            }

            public ScheduleDefiner OnTimes(params Time[] times)
            {
                Schedule.Times.AddRange(times);
                ValidateEveryIsValid();
                return this;
            }
            public ScheduleDefiner Every(TimeSpan duration)
            {
                if (Schedule.Every.HasValue)
                    throw new ArgumentException("Every can only be set once per schedule");
                Schedule.Every = duration;
                ValidateEveryIsValid();
                return this;
            }

            private void ValidateEveryIsValid()
            {
                foreach(var t in Schedule.Times)
                {
                    if(t.Minute.HasValue)
                    {
                        if(Schedule.Every.HasValue)
                        {
                            throw new ArgumentException("Times with minute declarations cannot be mixed with periodic 'Every' events");
                        }
                    }
                }
            }
        }
    }
}

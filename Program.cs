using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp2
{
    class Program
    {
        static void Main(string[] args)
        {
            var scheduled = new ScheduleDefiner();
            var schedule = scheduled
                .OnMonthsOfYear(MonthOfYear.Jan)
                .OnWeekDays(DayOfWeek.Monday)
                .OnTimes(Time.At(12, 00))
                .Every(TimeSpan.FromMinutes(5))
                .Schedule;
            foreach(var t in schedule.GetScheduledTimes(DateTime.Now))
            {
                Console.WriteLine(t);
            }
        }

        public class Schedule
        {
            public bool IsTime(DateTime currentTime, DateTime? lastExecution = null)
            {
                if (MonthsOfYear.Any() && !MonthsOfYear.Any(x => (int)x == currentTime.Month))
                    return false;
                if (DaysOfMonth.Any() && !DaysOfMonth.Any(x => x == currentTime.Day))
                    return false;
                if (WeekDays.Any() && !WeekDays.Any(x => x == currentTime.DayOfWeek))
                    return false;
                if (Times.Any() && !Times.Any(x => x.IsTime(currentTime)))
                    return false;

                if (lastExecution == null)
                {
                    lastExecution = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day);
                }

                return !Every.HasValue || (currentTime - lastExecution) >= Every.Value;
            }

            public IEnumerable<DateTime> GetScheduledTimes(DateTime current)
            {
                IEnumerable<DateTime> monthDates;

                if (MonthsOfYear.Any())
                {
                    monthDates = MonthsOfYear.Select(m => new DateTime(current.Year, (int)m, 1, 0, 0, 0));
                }
                else
                {
                    monthDates = new[] { new DateTime(current.Year, current.Month, 1, 0, 0, 0) };
                }
                IEnumerable<DateTime> dayDates;
                if (!DaysOfMonth.Any() && !WeekDays.Any())
                {
                    dayDates = monthDates.SelectMany(d => PartsInPeriod(Every.Value, d.AddMonths(1) - d).Select(p => d + p));
                }
                else
                {
                    dayDates = monthDates.SelectMany(m => DaysOfMonth.Select(d => new DateTime(m.Year, m.Month, d, 0, 0, 0)));
                    var dayDates2 = monthDates.SelectMany(m => WeekDays.SelectMany(wd => DayOfWeekDatesForMonth(wd, m.Year, m.Day)));
                    if (dayDates.Any() && dayDates2.Any())
                    {
                        dayDates = dayDates.Intersect(dayDates2);
                    }
                    else
                    {
                        dayDates = dayDates.Concat(dayDates2);
                    }
                }

                if (Times.Any())
                {
                    dayDates = dayDates.SelectMany(d => Times.Where(t => t.Minute.HasValue).Select(t => new DateTime(d.Year, d.Month, d.Day, t.Hour, t.Minute.Value, 0)));
                }

                return dayDates;
            }

            private static IEnumerable<TimeSpan> PartsInPeriod(TimeSpan span, TimeSpan max)
            {
                var r = span;
                while (r < max)
                {
                    yield return r;
                    r = r + span;
                }
            }

            private static IEnumerable<DateTime> DayOfWeekDatesForMonth(DayOfWeek dw, int year, int month)
            {
                var dt = new DateTime(year, month, 1);
                var offset = (int)dw - (int)dt.DayOfWeek;
                dt.AddDays(offset);
                while (dt.Month == month)
                {
                    yield return dt;
                    dt = dt.AddDays(7);
                }
            }

            public List<MonthOfYear> MonthsOfYear { get; private set; } = new List<MonthOfYear>();
            public List<int> DaysOfMonth { get; private set; } = new List<int>();
            public List<DayOfWeek> WeekDays { get; private set; } = new List<DayOfWeek>();
            public List<Time> Times { get; private set; } = new List<Time>();
            public TimeSpan? Every { get; set; }
        }

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
                return this;
            }
            public ScheduleDefiner Every(TimeSpan duration)
            {
                if (Schedule.Every.HasValue)
                    throw new ArgumentException("Every can only be set once per schedule");
                Schedule.Every = duration;
                return this;
            }
        }

        public class Time
        {
            public Time(int hour, int? minute)
            {
                Hour = hour;
                Minute = minute;
            }

            public bool IsTime(DateTime dateTime)
            {
                return dateTime.Hour == Hour & dateTime.Minute == Minute;
            }

            public static Time At(int hour, int? minute)
            {
                return new Time(hour, minute);
            }

            public int Hour { get; }
            public int? Minute { get; }
        }

        public enum MonthOfYear { Jan = 1, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec };
    }
}

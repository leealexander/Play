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
                .OnDaysOfMonth(MonthOfYear.Jan, MonthOfYear.Feb)
                .OnWeekDays(DayOfWeek.Monday)
                .OnTimes(Time.At(12))
                .Every(TimeSpan.FromMinutes(5))
                .RunifMissedWithin(TimeSpan.FromDays(5))
                .Schedule;
            foreach(var t in schedule.GetScheduledTimes(new DateTime(2021,2,1), TimeSpan.FromDays(1)))
            {
                Console.WriteLine(t);
            }
        }

        public class Schedule
        {
            public List<MonthOfYear> MonthsOfYear { get; private set; } = new List<MonthOfYear>();
            public List<int> DaysOfMonth { get; private set; } = new List<int>();
            public List<DayOfWeek> WeekDays { get; private set; } = new List<DayOfWeek>();
            public List<Time> Times { get; private set; } = new List<Time>();
            public TimeSpan? Every { get; set; }

            public bool IsTime(DateTime currentTime)
            {
                if (MonthsOfYear.Any() && !MonthsOfYear.Any(x => (int)x == currentTime.Month))
                    return false;
                if (DaysOfMonth.Any() && !DaysOfMonth.Any(x => x == currentTime.Day))
                    return false;
                if (WeekDays.Any() && !WeekDays.Any(x => x == currentTime.DayOfWeek))
                    return false;
                if (Times.Any() && !Times.Any(x => x.IsTime(currentTime)))
                    return false;

                return true;
            }

            public TimeSpan? GetNext

            public IEnumerable<DateTime> GetScheduledTimes(DateTime start,  TimeSpan howFarAhead)
            {
                IEnumerable<DateTime> monthDates;
                var endDate = start + howFarAhead;
                bool IsValid(DateTime dt) => dt >= start &&  dt < endDate;

                if (MonthsOfYear.Any())
                {
                    var validMonths = new List<DateTime>();
                    for(var current = start; current <= endDate; current = current.AddMonths(1))
                    {
                        if(IsValid(current))
                        {
                            validMonths.Add(current);
                        }
                        if(current.Day > 1)
                        {
                            current = new DateTime(current.Year, current.Month, current.Day);
                        }
                    }
                    monthDates = validMonths;
                }
                else
                {
                    monthDates = new[] { start };
                }
                IEnumerable<DateTime> dayDates;
                if (!DaysOfMonth.Any() && !WeekDays.Any())
                {
                    dayDates = new[] { start };
                }
                else
                {
                    dayDates = monthDates.SelectMany(m => DaysOfMonth.Select(d => new DateTime(m.Year, m.Month, d, 0, 0, 0))).Where(IsValid);
                    var dayDates2 = monthDates.SelectMany(m => WeekDays.SelectMany(wd => DayOfWeekDatesForMonth(wd, m.Year, m.Month))).Where(IsValid);
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
                    dayDates = dayDates.SelectMany(d => Times.Select(t => new DateTime(d.Year, d.Month, d.Day, t.Hour, t.Minute ?? 0, 0))).Where(IsValid);
                }

                if(Every.HasValue)
                {
                    dayDates = dayDates.SelectMany(d=>PartsInValidTime(d, Every.Value)).Where(IsValid);
                }

                return dayDates;
            }
            private IEnumerable<DateTime> PartsInValidTime(DateTime start, TimeSpan span)
            {
                var c = start;
                while (IsTime(c))
                {
                    yield return c;
                    c = c + span;
                }
            }

            private static IEnumerable<DateTime> DayOfWeekDatesForMonth(DayOfWeek dw, int year, int month)
            {
                var dt = new DateTime(year, month, 1);
                var offset = (int)dw - (int)dt.DayOfWeek;
                dt = dt.AddDays(offset);
                if(dt.Month != month)
                {
                    dt = dt.AddDays(7);
                }
                while (dt.Month == month)
                {
                    yield return dt;
                    dt = dt.AddDays(7);
                }
            }
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

        public class Time
        {
            public Time(int hour, int? minute)
            {
                Hour = hour;
                Minute = minute;
            }

            public bool IsTime(DateTime dateTime)
            {
                return dateTime.Hour == Hour && (!Minute.HasValue || dateTime.Minute == Minute.Value);
            }

            public static Time At(int hour, int? minute = null)
            {
                return new Time(hour, minute);
            }

            public int Hour { get; }
            public int? Minute { get; }
        }

        public enum MonthOfYear { Jan = 1, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec };
    }
}

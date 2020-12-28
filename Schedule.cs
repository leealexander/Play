using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ConsoleApp2
{
    partial class Program
    {
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

            public IEnumerable<DateTime> GetScheduledTimes(DateTime start,  TimeSpan howFarAhead)
            {
                IEnumerable<DateTime> monthDates;
                var endDate = start + howFarAhead;
                bool IsValid(DateTime dt) => dt >= start &&  dt <= endDate;

                if (MonthsOfYear.Any())
                {
                    var months = new List<DateTime>();
                    for(var monthDate = start; monthDate < endDate; )
                    {
                        months.Add(monthDate);
                        monthDate = monthDate.AddMonths(1);
                        monthDate = new DateTime(monthDate.Year, monthDate.Month, 1);
                    }
                    monthDates = months.ToArray();
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
                    c += span;
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
    }
}

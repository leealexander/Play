using System;
using System.Collections.Concurrent;
using System.IO;
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
    partial class Program
    {
        static void Main()
        {
            var scheduled = new ScheduleDefiner();
            var schedule = scheduled
                .OnMonthsOfYear(MonthOfYear.Jan)
                .OnWeekDays(DayOfWeek.Monday)
                .OnTimes(Time.At(12))
                .Every(TimeSpan.FromMinutes(5))
                .Schedule;
            foreach(var t in schedule.GetScheduledTimes(DateTime.Now, TimeSpan.FromDays(200)))
            {
                Console.WriteLine(t);
            }
        }
    }
}

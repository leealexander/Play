using System;

namespace ConsoleApp2
{
    partial class Program
    {
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
    }
}


using System;

namespace ExtensionMethods
{
    public static class DateExtensions
    {
        public static DateTime FirstDayOfMonth(this DateTime dDate)
        {
            return dDate.AddDays(1 - dDate.Day);
        }

        public static DateTime GetMondayBefore(this DateTime date)
        {
            // Return monday before a date
            var previousDate = date;
            while (previousDate.DayOfWeek != DayOfWeek.Monday)
                previousDate = previousDate.AddDays(-1);
            return previousDate;
        }
    }
}

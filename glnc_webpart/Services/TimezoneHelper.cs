using System;

namespace glnc_webpart.Services
{
    /// <summary>
    /// Helper class for handling New Caledonia timezone (UTC+11)
    /// </summary>
    public static class TimezoneHelper
    {
        // New Caledonia timezone (UTC+11)
        private static readonly TimeZoneInfo NewCaledoniaTimeZone;
        
        static TimezoneHelper()
        {
            try
            {
                // Try to get Pacific/Noumea timezone (New Caledonia)
                NewCaledoniaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific/Noumea");
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback: Create a fixed UTC+11 offset timezone
                NewCaledoniaTimeZone = TimeZoneInfo.CreateCustomTimeZone(
                    "New Caledonia Standard Time",
                    TimeSpan.FromHours(11),
                    "New Caledonia Standard Time",
                    "New Caledonia Standard Time"
                );
            }
        }
        
        /// <summary>
        /// Gets the current time in New Caledonia timezone (UTC+11)
        /// </summary>
        public static DateTime GetNewCaledoniaTime()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, NewCaledoniaTimeZone);
        }
        
        /// <summary>
        /// Converts UTC time to New Caledonia timezone
        /// </summary>
        public static DateTime ToNewCaledoniaTime(DateTime utcTime)
        {
            if (utcTime.Kind == DateTimeKind.Unspecified)
            {
                // Assume it's UTC if unspecified
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
            }
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime.ToUniversalTime(), NewCaledoniaTimeZone);
        }
        
        /// <summary>
        /// Converts New Caledonia time to UTC
        /// </summary>
        public static DateTime ToUtc(DateTime newCaledoniaTime)
        {
            if (newCaledoniaTime.Kind == DateTimeKind.Unspecified)
            {
                // Assume it's in New Caledonia timezone
                return TimeZoneInfo.ConvertTimeToUtc(newCaledoniaTime, NewCaledoniaTimeZone);
            }
            return TimeZoneInfo.ConvertTimeToUtc(newCaledoniaTime, NewCaledoniaTimeZone);
        }
        
        /// <summary>
        /// Gets the New Caledonia timezone info
        /// </summary>
        public static TimeZoneInfo GetNewCaledoniaTimeZone()
        {
            return NewCaledoniaTimeZone;
        }
    }
}

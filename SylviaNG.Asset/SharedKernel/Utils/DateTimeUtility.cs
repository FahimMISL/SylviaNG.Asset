namespace SylviaNG.Assets.SharedKernel.Utils
{
    public static class DateTimeUtility
    {
        private static TimeZoneInfo _tz = TimeZoneInfo.Utc;

        public static void Initialize(string timezoneId)
        {
            _tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }

        // CURRENT
        public static DateTime NowUtc() => DateTime.UtcNow;
        public static DateOnly TodayLocal() => ToLocalDate(DateTime.UtcNow);

        // PARSE
        public static DateOnly ParseDate(string value) => DateOnly.Parse(value);
        public static TimeOnly ParseTime(string value) => TimeOnly.Parse(value);

        // FORMAT
        public static string FormatDate(DateOnly date) => date.ToString("yyyy-MM-dd");
        public static string FormatTime(TimeOnly time) => time.ToString("HH:mm");

        // LOCAL -> UTC (save / filter)
        public static DateTime ConvertLocalToUtc(DateTime localTime)
        {
            if (localTime.Kind == DateTimeKind.Utc)
                return localTime;

            var unspecified = DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(unspecified, _tz);
        }

        public static DateTime StartOfDayUtc(DateOnly date)
        {
            var local = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(local, _tz);
        }

        public static DateTime EndOfDayUtc(DateOnly date)
        {
            var local = date.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
            return TimeZoneInfo.ConvertTimeToUtc(local, _tz);
        }

        // UTC -> LOCAL (display)
        public static DateTime ConvertUtcToLocal(DateTime utcTime)
        {
            if (utcTime.Kind != DateTimeKind.Utc)
                utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, _tz);
        }

        public static DateOnly ToLocalDate(DateTime utc) =>
            DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
                utc.Kind != DateTimeKind.Utc ? DateTime.SpecifyKind(utc, DateTimeKind.Utc) : utc, _tz));

        public static TimeOnly ToLocalTime(DateTime utc) =>
            TimeOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(
                utc.Kind != DateTimeKind.Utc ? DateTime.SpecifyKind(utc, DateTimeKind.Utc) : utc, _tz));

        public static string FormatLocalDate(DateTime utc) => FormatDate(ToLocalDate(utc));
        public static string FormatLocalTime(DateTime utc) => FormatTime(ToLocalTime(utc));

        // DURATION
        public static TimeSpan GetDuration(DateTime fromUtc, DateTime toUtc) => toUtc - fromUtc;
        public static string FormatDuration(TimeSpan duration) =>
            $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}";
    }
}

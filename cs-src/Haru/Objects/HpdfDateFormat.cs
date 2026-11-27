/* Formats dates as PDF date strings (D:YYYYMMDDHHmmSSOHH'mm'). */
using System;
using System.Globalization;

namespace Haru.Objects
{
    /// <summary>
    /// Builds PDF date strings, used by the document information and by annotations.
    /// </summary>
    internal static class HpdfDateFormat
    {
        /// <summary>
        /// Creates the date string of a date: a <see cref="DateTimeKind.Utc"/> value is written as UTC (Z),
        /// a local or unspecified value with the offset of the local time zone.
        /// </summary>
        public static HpdfString Create(DateTime date)
        {
            TimeSpan offset = date.Kind == DateTimeKind.Utc ? TimeSpan.Zero : TimeZoneInfo.Local.GetUtcOffset(date);
            return Create(new DateTimeOffset(DateTime.SpecifyKind(date, DateTimeKind.Unspecified), offset));
        }

        /// <summary>
        /// Creates the date string of a date with an explicit offset from UTC.
        /// </summary>
        public static HpdfString Create(DateTimeOffset date)
        {
            // Example: D:20231215143045+05'30'; Gregorian digits whatever the current culture
            // (e.g. not the Thai Buddhist year)
            string text = date.ToString("'D:'yyyyMMddHHmmss", CultureInfo.InvariantCulture);

            TimeSpan offset = date.Offset;
            if (offset.TotalMinutes == 0)
            {
                text += "Z";
            }
            else
            {
                char sign = offset.TotalMinutes >= 0 ? '+' : '-';
                int hours = Math.Abs((int)offset.TotalHours);
                int minutes = Math.Abs(offset.Minutes);
                text += string.Format(CultureInfo.InvariantCulture, "{0}{1:D2}'{2:D2}'", sign, hours, minutes);
            }

            return new HpdfString(text);
        }
    }
}

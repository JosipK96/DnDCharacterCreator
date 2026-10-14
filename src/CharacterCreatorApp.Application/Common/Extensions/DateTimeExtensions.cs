using System.Globalization;

namespace CharacterCreatorApp.Application.Common.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Formats DateTimeOffset to Croatian format (dd.MM.yyyy HH:mm)
    /// </summary>
    public static string ToCroatianFormat(this DateTimeOffset dateTime)
    {
        return dateTime.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats DateTimeOffset to Croatian date only (dd.MM.yyyy)
    /// </summary>
    public static string ToCroatianDateFormat(this DateTimeOffset dateTime)
    {
        return dateTime.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Formats DateTimeOffset to relative time ("2 hours ago", "yesterday", etc.)
    /// </summary>
    public static string ToRelativeTime(this DateTimeOffset dateTime, TimeProvider timeProvider)
    {
        var now = timeProvider.GetUtcNow();
        var diff = now - dateTime;

        if (diff.TotalMinutes < 1)
            return "upravo sada";  // just now
        if (diff.TotalMinutes < 60)
            return $"prije {(int)diff.TotalMinutes} min";  // X minutes ago
        if (diff.TotalHours < 24)
            return $"prije {(int)diff.TotalHours} h";  // X hours ago
        if (diff.TotalDays < 7)
            return $"prije {(int)diff.TotalDays} dana";  // X days ago

        return dateTime.ToCroatianFormat();
    }
}
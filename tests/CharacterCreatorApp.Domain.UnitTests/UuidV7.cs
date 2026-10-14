namespace CharacterCreatorApp.Domain.UnitTests;

/// <summary>
/// Reads the creation time back out of a version 7 GUID, whose first 48 bits are the Unix time
/// in milliseconds, most significant byte first.
/// </summary>
internal static class UuidV7
{
    public static DateTimeOffset TimestampOf(Guid id)
    {
        var bytes = id.ToByteArray(bigEndian: true);
        var milliseconds = 0L;

        for (var i = 0; i < 6; i++)
        {
            milliseconds = (milliseconds << 8) | bytes[i];
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
    }
}

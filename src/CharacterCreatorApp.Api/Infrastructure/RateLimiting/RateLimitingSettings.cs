namespace CharacterCreatorApp.Api.Infrastructure.RateLimiting;

/// <summary>
/// How often one client address may sign in or register (RateLimiting__PermitLimit and
/// RateLimiting__WindowSeconds on a host). Wrong passwords and wrong invite codes both count,
/// because every request does.
/// </summary>
public class RateLimitingSettings
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; init; } = 10;

    public int WindowSeconds { get; init; } = 60;
}

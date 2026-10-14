using System.ComponentModel.DataAnnotations;

namespace CharacterCreatorApp.Api.Helpers.Jwt;

/// <summary>
/// Token signing and validation settings. The key is a secret and is not kept in
/// appsettings.json: supply it through user secrets in development, or an environment
/// variable (JwtSettings__Key) elsewhere. The application refuses to start without it.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// HMAC-SHA256 signing key. At least 32 characters, since HS256 needs a 256-bit key.
    /// </summary>
    internal const string MissingKeyMessage =
        "JwtSettings:Key is not configured. Set it with " +
        "'dotnet user-secrets set \"JwtSettings:Key\" \"<value>\"' or the " +
        "JwtSettings__Key environment variable.";

    [Required(AllowEmptyStrings = false, ErrorMessage = MissingKeyMessage)]
    [MinLength(32, ErrorMessage = "JwtSettings:Key must be at least 32 characters for HMAC-SHA256 signing.")]
    public string Key { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Issuer { get; init; } = string.Empty;

    [Required(AllowEmptyStrings = false)]
    public string Audience { get; init; } = string.Empty;

    [Range(1, 1440)]
    public int ExpiryInMinutes { get; init; } = 60;
}

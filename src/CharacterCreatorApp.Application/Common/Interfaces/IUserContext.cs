namespace CharacterCreatorApp.Application.Common.Interfaces
{
    /// <summary>
    /// The signed-in user. Only handlers behind an [Authorize] endpoint take this, so there is
    /// always a user; reading UserId when there is none is a wiring mistake and throws, rather
    /// than every handler checking for null.
    /// </summary>
    public interface IUserContext
    {
        Guid UserId { get; }

        string? Email { get; }
    }
}

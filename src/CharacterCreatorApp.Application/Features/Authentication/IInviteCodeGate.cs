namespace CharacterCreatorApp.Application.Features.Authentication;

/// <summary>Decides whether a registration's invite code lets it through.</summary>
public interface IInviteCodeGate
{
    /// <summary>True when no invite code is configured, or when <paramref name="inviteCode"/> is it.</summary>
    bool Admits(string? inviteCode);
}

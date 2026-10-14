namespace CharacterCreatorApp.Application.Features.Authentication;

public interface IJwtHelper
{
    string GenerateJwtToken(AuthenticatedUser user);
}

namespace CharacterCreatorApp.Contracts.Requests.User;

public record ResetPasswordRequest(string Email, string Token, string NewPassword);

namespace CharacterCreatorApp.Contracts.Requests.User;

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);

namespace CharacterCreatorApp.Contracts.Requests.ClientError;

/// <summary>
/// What the client tells the API about an error it hit. No message text on purpose: an
/// exception's message can carry what the user typed.
/// </summary>
public record ReportClientErrorRequest(string ExceptionType, string StackTrace, string Path, string Version);

namespace CharacterCreatorApp.Client.Services;

/// <summary>A file the API sent to be saved, such as the character sheet PDF.</summary>
public sealed record DownloadedFile(string FileName, byte[] Content);

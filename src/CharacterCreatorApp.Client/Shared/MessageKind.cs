namespace CharacterCreatorApp.Client.Shared;

/// <summary>What a <see cref="Message"/> tells the player, which sets its colour, icon and role.</summary>
public enum MessageKind
{
    /// <summary>Something went wrong or was refused: Red, role "alert".</summary>
    Error,

    /// <summary>Something was done or saved: Green, role "status".</summary>
    Success,

    /// <summary>A pick is still needed, or something to choose more of: Amber, role "status".</summary>
    Warning,

    /// <summary>A note that explains: Ink on the Room highlight, role "note".</summary>
    Info,
}

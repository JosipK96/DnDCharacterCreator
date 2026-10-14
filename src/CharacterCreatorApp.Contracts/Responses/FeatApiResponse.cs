namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// A feat as the improvements step lists it. Edition is "2014" or "2024"; Category is the kind
/// of 2024 feat ("Origin", "General", "Fighting Style", "Epic Boon") and null for a 2014 one.
/// Prerequisites are worded for the player, such as "Level 19". AbilityIncrease is what the feat
/// raises, null when it raises nothing. CanBeTakenForImprovement is false for a 2014 Fighting
/// Style, which only the class's Fighting Style feature gives.
/// </summary>
public record FeatApiResponse(
    string Id,
    string Name,
    string Edition,
    string? Category,
    string Description,
    List<string> Prerequisites,
    List<string> GrantedProficiencyIds,
    FeatAbilityIncreaseApiResponse? AbilityIncrease,
    bool CanBeTakenForImprovement
);

/// <summary>
/// Points to spend on Options (ability ids, "STR"), all on one if the player likes, up to Maximum:
/// 20, or 30 for an Epic Boon. One option means the feat raises that ability and needs no pick.
/// </summary>
public record FeatAbilityIncreaseApiResponse(
    List<string> Options,
    int Points,
    int Maximum
);

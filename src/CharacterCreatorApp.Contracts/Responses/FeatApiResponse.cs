namespace CharacterCreatorApp.Contracts.Responses;

/// <summary>
/// A feat as the improvements step lists it. Edition is "2014" or "2024"; Category is the kind
/// of 2024 feat ("Origin", "General", "Fighting Style", "Epic Boon") and null for a 2014 one.
/// Prerequisites are worded for the player, such as "Level 19". AbilityIncrease is what the feat
/// raises, null when it raises nothing. CanBeTakenForImprovement is false for a 2014 Fighting
/// Style, which only the class's Fighting Style feature gives. Choice is the pick it asks for
/// beside the increase, null when none.
/// </summary>
public record FeatApiResponse(
    string Id,
    string Name,
    string Edition,
    string Source,
    string? Category,
    string Description,
    List<string> Prerequisites,
    List<string> GrantedProficiencyIds,
    FeatAbilityIncreaseApiResponse? AbilityIncrease,
    bool CanBeTakenForImprovement,
    FeatChoiceApiResponse? Choice = null
);

/// <summary>
/// A pick a feat asks for: Type "skills" (Count skill ids from Options), "skills_tools" (Count
/// skill or tool ids from Options, Skilled's three), or "saving-throw" (Resilient's, which follows
/// the ability it raises and is not picked on its own).
/// </summary>
public record FeatChoiceApiResponse(
    string Type,
    int Count,
    List<string> Options
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

namespace CharacterCreatorApp.Infrastructure.Seed;

/// <summary>
/// The one row saying which seed files the reference tables were last filled from, so startup
/// can tell whether they need replacing. See <see cref="SeedService"/>.
/// </summary>
public class ReferenceDataVersion
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    /// <summary>The <see cref="ReferenceDataHash"/> of the files the tables now hold.</summary>
    public required string Hash { get; set; }

    public DateTimeOffset AppliedAt { get; set; }
}

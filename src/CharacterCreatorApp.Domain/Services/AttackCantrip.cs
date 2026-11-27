namespace CharacterCreatorApp.Domain.Services;

/// <summary>
/// A damaging cantrip the character knows (<see cref="CantripDamage.IsAttackCantrip"/>) and the
/// ability it is cast with, in any spelling the data has; the calculator turns it into a row of the
/// sheet's attacks table.
/// </summary>
public sealed record AttackCantrip(
    string Name,
    string AbilityId,
    string? AttackType,
    string? SaveAbilityId,
    string DamageType,
    IReadOnlyList<string> DamageDice);

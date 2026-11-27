using CharacterCreatorApp.Application.ReadModels.Feats;
using CharacterCreatorApp.Contracts.Responses.Feats;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Feats;

namespace CharacterCreatorApp.Api.Mapping;

/// <summary>Turns the feat read models, and the feat picks a draft stores, into what the client reads.</summary>
public static class FeatMappings
{
    public static FeatApiResponse ToResponse(this FeatReadModel feat) => new(
        Id: feat.Id,
        Name: feat.Name,
        Edition: feat.Edition,
        Source: feat.Source,
        Category: feat.Category,
        Description: feat.Description,
        // The client shows the prerequisites as sentences and asks no rule about them
        Prerequisites: feat.Prerequisites.Describe().ToList(),
        GrantedProficiencyIds: feat.GrantedProficiencyIds.ToList(),
        AbilityIncrease: feat.AbilityIncrease?.ToResponse(),
        // Read from the domain rule, so the improvements step never works it out itself
        CanBeTakenForImprovement: FeatCategory.CanBeTakenForImprovement(feat.Edition, feat.Category),
        Choice: feat.Choice?.ToResponse());

    public static FeatChoiceApiResponse ToResponse(this FeatChoice choice) =>
        new(Type: choice.Type, Count: choice.Count, Options: choice.Options);

    public static FeatAbilityIncreaseApiResponse ToResponse(this FeatAbilityIncrease increase) =>
        new(Options: increase.Options, Points: increase.Points, Maximum: increase.Maximum);

    public static FeatEligibilityApiResponse ToResponse(this FeatEligibilityReadModel eligibility) => new(
        FeatId: eligibility.FeatId,
        Unmet: eligibility.Unmet,
        UnmetAsRaceFeat: eligibility.UnmetAsRaceFeat,
        CheckWithDm: eligibility.CheckWithDm);

    public static FeatSpellPickApiModel ToResponse(this FeatSpellPick pick) => new()
    {
        FeatId = pick.FeatId,
        ClassId = pick.ClassId,
        SpellIds = pick.SpellIds,
        AbilityId = pick.AbilityId,
    };

    public static FeatProficiencyPickApiModel ToResponse(this FeatProficiencyPick pick) => new()
    {
        FeatId = pick.FeatId,
        ProficiencyIds = pick.ProficiencyIds,
    };
}

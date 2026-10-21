using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetAbilityScoreImprovements;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class SetAbilityScoreImprovementsCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly SetAbilityScoreImprovementsCommandHandler _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _draftId = Guid.NewGuid();

    public SetAbilityScoreImprovementsCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        _sut = new SetAbilityScoreImprovementsCommandHandler(
            _draftRepositoryMock.Object,
            _classRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _featRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithAFeatInsteadOfAnImprovement_SavesBoth()
    {
        // Arrange - a level 8 fighter has three improvements (levels 4, 6 and 8)
        var draft = ArrangeDraft(level: 8);
        ArrangeFeats(Feat("alert"));

        var command = new SetAbilityScoreImprovementsCommand(
            _draftId,
            [new Dictionary<string, int> { ["strength"] = 2 }, new Dictionary<string, int> { ["dexterity"] = 1, ["constitution"] = 1 }],
            ["alert"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.AbilityScoreImprovements.Should().HaveCount(2);
        draft.SelectedFeatIds.Should().BeEquivalentTo(["alert"]);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenImprovementsAndFeatsDoNotAddUp_ReturnsValidationError()
    {
        // Arrange - three improvements owed, only two spent
        ArrangeDraft(level: 8);
        ArrangeFeats(Feat("alert"));

        var command = new SetAbilityScoreImprovementsCommand(
            _draftId,
            [new Dictionary<string, int> { ["strength"] = 2 }],
            ["alert"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.InvalidAsiCount");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAllImprovementsSpentOnFeats_Succeeds()
    {
        // Arrange
        var draft = ArrangeDraft(level: 4);
        ArrangeFeats(Feat("alert"));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["alert"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.AbilityScoreImprovements.Should().BeEmpty();
        draft.SelectedFeatIds.Should().BeEquivalentTo(["alert"]);
    }

    [Fact]
    public async Task Handle_WithUnknownFeat_ReturnsValidationError()
    {
        // Arrange
        ArrangeDraft(level: 4);
        ArrangeFeats();

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["not_a_feat"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.InvalidFeatIds");
    }

    [Fact]
    public async Task Handle_TakingANonRepeatableFeatTwice_ReturnsValidationError()
    {
        // Arrange - a level 8 fighter could otherwise spend two improvements on the same feat
        ArrangeDraft(level: 8);
        ArrangeFeats(Feat("alert"));

        var command = new SetAbilityScoreImprovementsCommand(
            _draftId,
            [new Dictionary<string, int> { ["strength"] = 2 }],
            ["alert", "alert"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.DuplicateFeats");
    }

    [Fact]
    public async Task Handle_TakingARepeatableFeatTwice_Succeeds()
    {
        // Arrange
        var draft = ArrangeDraft(level: 8);
        ArrangeFeats(Feat("ability_score_improvement", isRepeatable: true));

        var command = new SetAbilityScoreImprovementsCommand(
            _draftId,
            [new Dictionary<string, int> { ["strength"] = 2 }],
            ["ability_score_improvement", "ability_score_improvement"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedFeatIds.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_WithAFeatBelowItsAbilityScorePrerequisite_ReturnsValidationError()
    {
        // Arrange - the draft has STR 15, so ask for more
        ArrangeDraft(level: 4);
        ArrangeFeats(Feat("heavily_armored", ability: ("STR", 18)));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["heavily_armored"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.FeatPrerequisiteNotMet");
        result.Errors[0].Description.Should().Be("heavily_armored requires Strength 18.");
    }

    [Fact]
    public async Task Handle_WithAFeatMeetingItsAbilityScorePrerequisite_Succeeds()
    {
        // Arrange - the draft has STR 15
        ArrangeDraft(level: 4);
        ArrangeFeats(Feat("heavily_armored", ability: ("STR", 13)));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["heavily_armored"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAFeatOnlyTheRacialBonusQualifiesFor_Succeeds()
    {
        // Arrange - base CON 13 is 15 with the dwarf's +2; the base scores alone used to be checked
        ArrangeDraft(level: 4);
        ArrangeFeats(Feat("durable_2014", ability: ("CON", 15)));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["durable_2014"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAFeatAnEarlierImprovementQualifiesFor_Succeeds()
    {
        // Arrange - STR 15 is 17 after the level 4 improvement, in time for the feat at level 6
        var draft = ArrangeDraft(level: 6);
        ArrangeFeats(Feat("grappler", ability: ("STR", 17)));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [new() { ["STR"] = 2 }], ["grappler"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedFeatIds.Should().Equal("grappler");
    }

    [Fact]
    public async Task Handle_WithAFeatOnlyAnImprovementBeyondTwentyWouldQualifyFor_ReturnsValidationError()
    {
        // Arrange - a level 12 fighter has four improvements; one stops at 20, so STR 15 + 2 + 2 + 2 is 20, not 21
        ArrangeDraft(level: 12);
        ArrangeFeats(Feat("grappler", ability: ("STR", 21)));

        var command = new SetAbilityScoreImprovementsCommand(
            _draftId, [new() { ["STR"] = 2 }, new() { ["STR"] = 2 }, new() { ["STR"] = 2 }], ["grappler"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.FeatPrerequisiteNotMet");
    }

    [Fact]
    public async Task Handle_WithARaceFeatOnlyAnImprovementQualifiesFor_ReturnsValidationError()
    {
        // Arrange - the variant human's feat comes at level 1, before the level 4 improvement:
        // STR 15 + 1 is 16 then, whatever the improvement raises it to later
        ArrangeDraft(level: 4, race: Human2014(), subraceId: "variant_human", raceIncreases: new() { ["STR"] = 1, ["CON"] = 1 });
        ArrangeFeats(Feat("grappler", ability: ("STR", 17)));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [new() { ["STR"] = 2 }], ["grappler"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.FeatPrerequisiteNotMet");
        result.Errors[0].Description.Should().Be("grappler requires Strength 17.");
    }

    [Fact]
    public async Task Handle_WithAnEpicBoonBeforeLevel19_ReturnsValidationError()
    {
        // Arrange - the 2024 epic boons are listed beside every other feat
        ArrangeDraft(level: 4);
        ArrangeFeats(Feat("boon_of_fate", minimumLevel: 19));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["boon_of_fate"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.FeatPrerequisiteNotMet");
        result.Errors[0].Description.Should().Be("boon_of_fate requires Level 19.");
    }

    [Fact]
    public async Task Handle_WithAFeatRestrictedToAnotherClass_ReturnsValidationError()
    {
        // Arrange
        ArrangeDraft(level: 4);
        ArrangeFeats(Feat("metamagic_adept", classId: "sorcerer"));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["metamagic_adept"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.FeatPrerequisiteNotMet");
    }

    [Fact]
    public async Task Handle_AtALevelWithNoImprovements_RejectsAnyChoice()
    {
        // Arrange - a level 1 fighter has earned nothing yet
        ArrangeDraft(level: 1);
        ArrangeFeats(Feat("alert"));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["alert"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.InvalidAsiCount");
    }

    [Fact]
    public async Task Handle_WithAFeatFromABookTheDraftDoesNotAllow_ReturnsSourceNotAllowed()
    {
        // Arrange - a feat from Tasha's Cauldron, and a draft allowing the Player's Handbook alone
        var draft = ArrangeDraft(level: 4);
        var feyTouched = Feat("fey_touched", name: "Fey Touched");
        feyTouched.Source = "TCE";
        ArrangeFeats(feyTouched);

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["fey_touched"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Description.Should().Be(
            "The feat Fey Touched comes from TCE, a book this character does not allow.");
        draft.SelectedFeatIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFeatFromTheOtherEdition_ReturnsValidationError()
    {
        // Arrange - a 2014 fighter offered the 2024 Alert
        ArrangeDraft(level: 4);
        ArrangeFeats(Feat("alert_2024", name: "Alert", edition: RulesEdition.Edition2024));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["alert_2024"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.EditionMismatch");
        result.Errors[0].Description.Should().Be("The feat Alert follows the 2024 rules, but this character is built with the 2014 rules.");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFightingStyleFeatForAClassThatHasTheFeature_Succeeds()
    {
        // Arrange - a 2024 fighter has Fighting Style from level 1
        var draft = ArrangeDraft(level: 4, Fighter2024());
        ArrangeFeats(Feat("archery", edition: RulesEdition.Edition2024, feature: "Fighting Style"));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["archery"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedFeatIds.Should().Equal("archery");
    }

    [Fact]
    public async Task Handle_WithAFightingStyleFeatForAClassWithoutTheFeature_ReturnsValidationError()
    {
        // Arrange - a wizard has no Fighting Style, so none of its feats
        ArrangeDraft(level: 4, Wizard2024());
        ArrangeFeats(Feat("archery", edition: RulesEdition.Edition2024, feature: "Fighting Style"));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["archery"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.FeatPrerequisiteNotMet");
        result.Errors[0].Description.Should().Be("archery requires The Fighting Style feature.");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTheFeatTheBackgroundGave_ReturnsDuplicateFeats()
    {
        // Arrange - a 2024 criminal has Alert from the background already
        ArrangeDraft(level: 4, Fighter2024(), Criminal2024(), new Dictionary<string, int> { ["DEX"] = 2, ["CON"] = 1 });
        ArrangeFeats(Feat("alert_2024", name: "Alert", edition: RulesEdition.Edition2024));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["alert_2024"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.DuplicateFeats");
        result.Errors[0].Description.Should().Be("These feats can only be taken once: Alert");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFeatOnlyTheBackgroundIncreaseQualifiesFor_Succeeds()
    {
        // Arrange - base STR 15 is 17 with the 2024 soldier's +2; the 2024 dwarf adds nothing
        var draft = ArrangeDraft(level: 4, Fighter2024());
        ArrangeFeats(Feat("grappler_2024", ability: ("STR", 17), edition: RulesEdition.Edition2024));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["grappler_2024"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedFeatIds.Should().Equal("grappler_2024");
    }

    [Fact]
    public async Task Handle_ForAVariantHumanWithoutItsFeat_ReturnsValidationError()
    {
        // Arrange - a level 1 variant human owes one feat, and no improvements yet
        ArrangeDraft(level: 1, race: Human2014(), subraceId: "variant_human", raceIncreases: new() { ["STR"] = 1, ["CON"] = 1 });

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], []);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.BonusFeatsMissing");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ForAVariantHumanAtLevelOne_TakesItsFeatBesideTheImprovements()
    {
        // Arrange - the feat is the subrace's, of any category; a level 1 fighter has no improvements
        var draft = ArrangeDraft(level: 1, race: Human2014(), subraceId: "variant_human", raceIncreases: new() { ["STR"] = 1, ["CON"] = 1 });
        ArrangeFeats(Feat("alert"));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["alert"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedFeatIds.Should().Equal("alert");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_For2024HumanWithAnOriginFeat_Succeeds()
    {
        // Arrange - Versatile: an Origin feat, taken at level 1
        var draft = ArrangeDraft(level: 1, Fighter2024(), race: Human2024());
        ArrangeFeats(Feat("skilled_2024", name: "Skilled", edition: RulesEdition.Edition2024, category: "Origin"));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["skilled_2024"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedFeatIds.Should().Equal("skilled_2024");
    }

    [Fact]
    public async Task Handle_For2024HumanWithAFeatOutsideOrigin_ReturnsValidationError()
    {
        // Arrange - the race's feat comes first; Grappler is a General feat
        ArrangeDraft(level: 4, Fighter2024(), race: Human2024());
        ArrangeFeats(
            Feat("grappler_2024", name: "Grappler", edition: RulesEdition.Edition2024, category: "General"),
            Feat("skilled_2024", name: "Skilled", edition: RulesEdition.Edition2024, category: "Origin"));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["grappler_2024", "skilled_2024"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.BonusFeatNotOfCategory");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFeatRaisingOneOfTwoAbilities_StoresThePick()
    {
        // Arrange - Athlete raises Strength or Dexterity; the pick arrives in any spelling
        var draft = ArrangeDraft(level: 4);
        ArrangeFeats(Feat("athlete", increase: new FeatAbilityIncrease { Options = ["STR", "DEX"] }));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["athlete"], [new() { ["dexterity"] = 1 }]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.FeatAbilityIncreases.Should().BeEquivalentTo([new Dictionary<string, int> { ["DEX"] = 1 }]);
    }

    [Fact]
    public async Task Handle_WithAFeatRaisingAFixedAbility_StoresItWithoutAPick()
    {
        // Arrange - Actor raises Charisma, so the player picks nothing
        var draft = ArrangeDraft(level: 4);
        ArrangeFeats(Feat("actor", increase: new FeatAbilityIncrease { Options = ["CHA"] }));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["actor"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.FeatAbilityIncreases.Should().BeEquivalentTo([new Dictionary<string, int> { ["CHA"] = 1 }]);
    }

    public static TheoryData<FeatReadModel, string> PicksTheFeatDoesNotOffer => new()
    {
        // Athlete raises Strength or Dexterity
        { Feat("athlete", increase: new FeatAbilityIncrease { Options = ["STR", "DEX"] }), "CHA" },

        // Alert raises nothing
        { Feat("alert"), "DEX" },
    };

    [Theory]
    [MemberData(nameof(PicksTheFeatDoesNotOffer))]
    public async Task Handle_WithAnAbilityPickTheFeatDoesNotOffer_ReturnsValidationError(FeatReadModel feat, string ability)
    {
        // Arrange
        ArrangeDraft(level: 4);
        ArrangeFeats(feat);

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], [feat.Id], [new() { [ability] = 1 }]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.InvalidFeatAbilityIncrease");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFeatOnlyTheRacePicksQualifyFor_Succeeds()
    {
        // Arrange - base STR 15 is 16 with the half-elf's +1 picked for Strength
        ArrangeDraft(level: 4, race: HalfElf(), raceIncreases: new() { ["STR"] = 1, ["CON"] = 1 });
        ArrangeFeats(Feat("heavily_armored", ability: ("STR", 16)));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["heavily_armored"]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAFeatAskingForASkill_StoresThePick()
    {
        // Arrange - Keen Mind's one skill of five
        var draft = ArrangeDraft(level: 4, Fighter2024());
        ArrangeFeats(KeenMind2024());

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["keen_mind_2024"], null, [Pick("keen_mind_2024", "arcana")]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.FeatProficiencyPicks.Should().BeEquivalentTo([Pick("keen_mind_2024", "arcana")]);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSkilledFromTheBackgroundAndTheRace_StoresBothSetsInOrder()
    {
        // Arrange - a Noble human: Skilled from the background, then again as the human's Origin feat
        var draft = ArrangeNobleHuman();
        FeatProficiencyPick[] picks = [Pick("skilled_2024", "stealth", "lute", "arcana"), Pick("skilled_2024", "insight", "thieves-tools", "deception")];

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["skilled_2024"], null, [.. picks]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.FeatProficiencyPicks.Should().Equal(picks);
    }

    public static TheoryData<string, string[], string> RepeatedPicks => new()
    {
        // History comes with the Noble
        { "the background's", ["insight", "thieves-tools", "history"], "history comes with Noble already; pick another in its place." },

        // Stealth is the first Skilled's already
        { "the other Skilled's", ["insight", "thieves-tools", "stealth"], "stealth comes with a feat's skill or tool picks already; pick another in its place." },
    };

    [Theory]
    [MemberData(nameof(RepeatedPicks))]
    public async Task Handle_WithAFeatPickTheCharacterHasAlready_ReturnsProficiencyAlreadyGiven(string whose, string[] secondPicks, string expected)
    {
        // Arrange
        var draft = ArrangeNobleHuman();

        var command = new SetAbilityScoreImprovementsCommand(
            _draftId, [], ["skilled_2024"], null, [Pick("skilled_2024", "stealth", "lute", "arcana"), Pick("skilled_2024", secondPicks)]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse(because: $"the pick is {whose}");
        result.Errors[0].Code.Should().Be("CharacterDraft.ProficiencyAlreadyGiven");
        result.Errors[0].Description.Should().Be(expected);
        draft.FeatProficiencyPicks.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFeatPickTheProficienciesStepPicked_ReturnsProficiencyAlreadyGiven()
    {
        // Arrange - the fighter's class skills are picked a step before the feats
        var draft = ArrangeDraft(level: 4, Fighter2024());
        draft.SetClassProficiencies(["arcana"]);
        ArrangeFeats(KeenMind2024());

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["keen_mind_2024"], null, [Pick("keen_mind_2024", "arcana")]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.ProficiencyAlreadyGiven");
    }

    public static TheoryData<string[]> PicksKeenMindDoesNotOffer => new()
    {
        // Too many, too few, and one that is not among its five
        { ["arcana", "history"] },
        { [] },
        { ["stealth"] },
    };

    [Theory]
    [MemberData(nameof(PicksKeenMindDoesNotOffer))]
    public async Task Handle_WithAFeatPickTheFeatDoesNotOffer_ReturnsInvalidFeatProficiencyPick(string[] picks)
    {
        // Arrange
        ArrangeDraft(level: 4, Fighter2024());
        ArrangeFeats(KeenMind2024());

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], ["keen_mind_2024"], null, [Pick("keen_mind_2024", picks)]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.InvalidFeatProficiencyPick");
        result.Errors[0].Description.Should().Be("Keen Mind offers 1 different pick(s) from arcana, history, investigation, nature, religion.");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    public static TheoryData<string[], FeatProficiencyPick[]> PicksNotOnePerFeat => new()
    {
        // Keen Mind's pick left out
        { ["keen_mind_2024"], [] },

        // A pick for Alert, which asks for none
        { ["alert_2024"], [new FeatProficiencyPick { FeatId = "alert_2024", ProficiencyIds = ["arcana"] }] },
    };

    [Theory]
    [MemberData(nameof(PicksNotOnePerFeat))]
    public async Task Handle_WithPicksNotOnePerFeatThatAsks_ReturnsFeatProficiencyPicksIncomplete(string[] featIds, FeatProficiencyPick[] picks)
    {
        // Arrange
        ArrangeDraft(level: 4, Fighter2024());
        ArrangeFeats(KeenMind2024(), Feat("alert_2024", name: "Alert", edition: RulesEdition.Edition2024, category: "Origin"));

        var command = new SetAbilityScoreImprovementsCommand(_draftId, [], [.. featIds], null, [.. picks]);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.FeatProficiencyPicksIncomplete");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // A level 1 Noble human fighter: Skilled from the background, and Skilled again as the human's feat
    private CharacterDraft ArrangeNobleHuman()
    {
        var draft = ArrangeDraft(level: 1, Fighter2024(), Noble2024(), new Dictionary<string, int> { ["STR"] = 2, ["CHA"] = 1 }, Human2024());
        ArrangeFeats(Skilled2024());
        return draft;
    }

    // The races and backgrounds of the class's edition: a 2014 dwarf soldier gets CON +2 from
    // the race, a 2024 one +2/+1 from the background
    private CharacterDraft ArrangeDraft(
        int level,
        Class? @class = null,
        Background? background = null,
        Dictionary<string, int>? backgroundIncreases = null,
        Race? race = null,
        string? subraceId = null,
        Dictionary<string, int>? raceIncreases = null)
    {
        @class ??= Fighter2014();

        var is2024 = @class.Edition == RulesEdition.Edition2024;
        race ??= is2024 ? Dwarf2024() : Dwarf2014();
        background ??= is2024 ? Soldier2024() : Soldier2014();
        backgroundIncreases ??= is2024 ? new Dictionary<string, int> { ["STR"] = 2, ["CON"] = 1 } : [];

        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider());
        draft.ChooseEdition(@class.Edition);
        draft.SetNameAndLevel("Thorin", level);
        draft.SetRace(race.Id, subraceId, raceIncreases);
        draft.SetClass(@class.Id);
        draft.SetBackground(background.Id, backgroundIncreases);
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["STR"] = 15,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["WIS"] = 10,
            ["CHA"] = 8
        }, AbilityScoreMethod.Rolled);

        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync(draft);
        _classRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(@class.Id, _cts.Token))
            .ReturnsAsync(@class);

        _raceRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(race.Id, _cts.Token))
            .ReturnsAsync(race);
        _backgroundRepositoryMock
            .Setup(r => r.GetByIdWithDetailsAsync(background.Id, _cts.Token))
            .ReturnsAsync(background);

        return draft;
    }

    // Prerequisites are checked against the scores with racial bonuses: a dwarf's CON +2
    private static Race Dwarf2014() => new()
    {
        Id = "dwarf",
        Name = "Dwarf",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        Size = "Medium",
        Speed = 25,
        AbilityBonuses = [new RaceAbilityBonus { RaceId = "dwarf", AbilityScoreId = "CON", Bonus = 2 }]
    };

    private static Race Dwarf2024() => new()
    {
        Id = "dwarf_2024",
        Name = "Dwarf",
        Edition = RulesEdition.Edition2024,
        Source = "XPHB",
        Size = "Medium",
        Speed = 30
    };

    // +2 Charisma, and +1 to two others of the player's choice
    private static Race HalfElf() => new()
    {
        Id = "half_elf",
        Name = "Half-Elf",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        Size = "Medium",
        Speed = 30,
        SkillChoiceCount = 2,
        AbilityChoiceCount = 2,
        AbilityChoiceOptions = ["STR", "DEX", "CON", "INT", "WIS"],
        AbilityBonuses = [new RaceAbilityBonus { RaceId = "half_elf", AbilityScoreId = "CHA", Bonus = 2 }]
    };

    // The variant human's +1 to two abilities, a skill and a feat of any category
    private static Race Human2014() => new()
    {
        Id = "human",
        Name = "Human",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        Size = "Medium",
        Speed = 30,
        Subraces =
        [
            new Subrace
            {
                Id = "variant_human",
                Name = "Variant Human",
                Source = "PHB",
                RaceId = "human",
                AbilityChoiceCount = 2,
                AbilityChoiceOptions = ["STR", "DEX", "CON", "INT", "WIS", "CHA"],
                SkillChoiceCount = 1,
                FeatChoiceCount = 1,
            },
        ]
    };

    // Versatile: an Origin feat of the player's choice
    private static Race Human2024() => new()
    {
        Id = "human_2024",
        Name = "Human",
        Edition = RulesEdition.Edition2024,
        Source = "XPHB",
        Size = "Small or Medium",
        Speed = 30,
        SkillChoiceCount = 1,
        FeatChoiceCount = 1,
        FeatChoiceCategory = "Origin"
    };

    // Athletics and Intimidation
    private static Background Soldier2014() => new()
    {
        Id = "soldier",
        Name = "Soldier",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        BackgroundProficiencies =
        [
            new BackgroundProficiency { BackgroundId = "soldier", ProficiencyId = "athletics" },
            new BackgroundProficiency { BackgroundId = "soldier", ProficiencyId = "intimidation" },
        ],
    };

    // Savage Attacker, and +2/+1 or +1 to each of Strength, Dexterity and Constitution
    private static Background Soldier2024() => new()
    {
        Id = "soldier_2024",
        Name = "Soldier",
        Edition = RulesEdition.Edition2024,
        Source = "XPHB",
        AbilityScoreOptions = ["STR", "DEX", "CON"],
        FeatId = "savage_attacker_2024",
    };

    // Alert, and +2/+1 or +1 to each of Dexterity, Constitution and Intelligence
    private static Background Criminal2024() => new()
    {
        Id = "criminal_2024",
        Name = "Criminal",
        Edition = RulesEdition.Edition2024,
        Source = "XPHB",
        AbilityScoreOptions = ["DEX", "CON", "INT"],
        FeatId = "alert_2024",
    };

    // Skilled, History and Persuasion, and +2/+1 or +1 to each of Strength, Intelligence and Charisma
    private static Background Noble2024() => new()
    {
        Id = "noble_2024",
        Name = "Noble",
        Edition = RulesEdition.Edition2024,
        Source = "XPHB",
        AbilityScoreOptions = ["STR", "INT", "CHA"],
        FeatId = "skilled_2024",
        BackgroundProficiencies =
        [
            new BackgroundProficiency { BackgroundId = "noble_2024", ProficiencyId = "history" },
            new BackgroundProficiency { BackgroundId = "noble_2024", ProficiencyId = "persuasion" },
        ],
    };

    // Three skills or tools, as many times as it is taken
    private static FeatReadModel Skilled2024() => new()
    {
        Id = "skilled_2024",
        Name = "Skilled",
        Description = "A feat.",
        Edition = RulesEdition.Edition2024,
        Source = "XPHB",
        Category = "Origin",
        IsRepeatable = true,
        Choice = new FeatChoice
        {
            Type = FeatChoice.SkillsTools,
            Count = 3,
            Options = ["arcana", "deception", "history", "insight", "persuasion", "stealth", "lute", "thieves-tools"],
        },
    };

    // One of five skills, and +1 Intelligence
    private static FeatReadModel KeenMind2024() => new()
    {
        Id = "keen_mind_2024",
        Name = "Keen Mind",
        Description = "A feat.",
        Edition = RulesEdition.Edition2024,
        Source = "XPHB",
        Category = "General",
        AbilityIncrease = new FeatAbilityIncrease { Options = ["INT"] },
        Choice = new FeatChoice { Type = FeatChoice.Skills, Count = 1, Options = ["arcana", "history", "investigation", "nature", "religion"] },
    };

    private static FeatProficiencyPick Pick(string featId, params string[] proficiencyIds) =>
        new() { FeatId = featId, ProficiencyIds = [.. proficiencyIds] };

    private void ArrangeFeats(params FeatReadModel[] feats) =>
        _featRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token))
            .ReturnsAsync([.. feats]);

    private static FeatReadModel Feat(
        string id,
        bool isRepeatable = false,
        (string AbilityId, int Score)? ability = null,
        string? classId = null,
        int? minimumLevel = null,
        string? name = null,
        string edition = RulesEdition.Edition2014,
        string? feature = null,
        string? category = null,
        FeatAbilityIncrease? increase = null) => new()
        {
            Id = id,
            Name = name ?? id,
            Description = "A feat.",
            Edition = edition,
            Source = RulesEdition.PlayersHandbook(edition),
            Category = category,
            IsRepeatable = isRepeatable,
            AbilityIncrease = increase,
            Prerequisites = new FeatPrerequisites
            {
                MinimumLevel = minimumLevel,
                AbilityMinimums = ability is { } a ? [new AbilityMinimum { AbilityId = a.AbilityId, Score = a.Score }] : [],
                ClassIds = classId is null ? [] : [classId],
                Features = feature is null ? [] : [feature],
            }
        };

    private static Class Fighter2014() => ClassWithLevelOneFeatures(
        "fighter", RulesEdition.Edition2014, "Fighter", "1d10", [4, 6, 8, 12, 14, 16, 19], "Fighting Style", "Second Wind");

    private static Class Fighter2024() => ClassWithLevelOneFeatures(
        "fighter-2024", RulesEdition.Edition2024, "Fighter", "1d10", [4, 6, 8, 12, 14, 16, 19], "Fighting Style", "Second Wind", "Weapon Mastery");

    private static Class Wizard2024() => ClassWithLevelOneFeatures(
        "wizard-2024", RulesEdition.Edition2024, "Wizard", "1d6", [4, 8, 12, 16, 19], "Spellcasting", "Ritual Adept", "Arcane Recovery");

    // Named as the class feature data names them: a Fighting Style feat looks for the name
    private static Class ClassWithLevelOneFeatures(
        string id,
        string edition,
        string name,
        string hitDie,
        List<int> asiLevels,
        params string[] levelOneFeatures) => new()
    {
        Id = id,
        Name = name,
        Edition = edition,
        Source = RulesEdition.PlayersHandbook(edition),
        HitDie = hitDie,
        SubclassLevel = 3,
        AsiLevels = asiLevels,
        Features = levelOneFeatures.Select(feature => new Feature
        {
            Id = $"{id}-1-{feature.ToLowerInvariant().Replace(' ', '-')}",
            Name = feature,
            Description = feature,
            Level = 1,
            ClassId = id,
            Type = "class",
        }).ToList(),
    };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}

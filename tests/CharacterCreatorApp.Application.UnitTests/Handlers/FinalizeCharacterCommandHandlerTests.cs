using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.FinalizeCharacter;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Backgrounds;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.DomainModels.Proficiencies;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class FinalizeCharacterCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ICharacterRepository> _characterRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ILanguageRepository> _languageRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ISpellRepository> _spellRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));
    private readonly CancellationTokenSource _cts = new();
    private readonly FinalizeCharacterCommandHandler _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _draftId = Guid.NewGuid();

    private Character? _createdCharacter;

    public FinalizeCharacterCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(() => _userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        _languageRepositoryMock.Setup(r => r.GetAllForEditionAsync(RulesEdition.Edition2014, _cts.Token)).ReturnsAsync(PhbLanguages());
        _languageRepositoryMock.Setup(r => r.GetAllForEditionAsync(RulesEdition.Edition2024, _cts.Token)).ReturnsAsync(Phb2024Languages());
        _featRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token))
            .ReturnsAsync([]);

        _characterRepositoryMock
            .Setup(r => r.Create(It.IsAny<Character>(), _cts.Token))
            .Callback<Character, CancellationToken>((character, _) => _createdCharacter = character)
            .Returns(Task.CompletedTask);

        _sut = new FinalizeCharacterCommandHandler(
            _draftRepositoryMock.Object,
            _characterRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _classRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _languageRepositoryMock.Object,
            _featRepositoryMock.Object,
            _spellRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object,
            _timeProvider);
    }

    private static List<LanguageReadModel> PhbLanguages() =>
    [
        new() { Id = "common-phb", Name = "Common", Type = "standard" },
        new() { Id = "dwarvish-phb", Name = "Dwarvish", Type = "standard" },
        new() { Id = "elvish-phb", Name = "Elvish", Type = "standard" },
        new() { Id = "giant-phb", Name = "Giant", Type = "standard" },
        new() { Id = "orc-phb", Name = "Orc", Type = "exotic" }
    ];

    // The 2024 book's, which a 2024 character's names resolve against
    private static List<LanguageReadModel> Phb2024Languages() =>
    [
        new() { Id = "common-xphb", Name = "Common", Type = "standard" },
        new() { Id = "draconic-xphb", Name = "Draconic", Type = "standard" },
        new() { Id = "giant-xphb", Name = "Giant", Type = "standard" }
    ];

    [Fact]
    public async Task Handle_With2024Background_AppliesItsIncreasesAndGivesItsFeatAndCommon()
    {
        // Arrange - a 2024 dwarf soldier: STR 15 and CON 13 with the background's +2 and +1,
        // Savage Attacker, and Common with two picks
        var draft = Create2024SoldierDraft(new Dictionary<string, int> { ["STR"] = 2, ["CON"] = 1 });

        // Act
        var created = await ArrangeAndFinalize(_draftId, _userId, draft, Dwarf2024(), Soldier2024(), Fighter2024());

        // Assert
        (created.AbilityScores["STR"], created.AbilityScores["CON"]).Should().Be((17, 14));
        created.FeatIds.Should().Equal("savage_attacker_2024");
        created.LanguageIds.Should().BeEquivalentTo(["common-xphb", "draconic-xphb", "giant-xphb"]);
    }

    [Fact]
    public async Task Handle_With2024BackgroundAndNoIncreasesChosen_ReturnsBackgroundAbilityIncreasesIncomplete()
    {
        // Arrange - a 2024 character's increases come only from the background
        var draft = Create2024SoldierDraft(increases: []);
        ArrangeRepositories(_draftId, _userId, draft, Dwarf2024(), Soldier2024(), Fighter2024());

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.BackgroundAbilityIncreasesIncomplete");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_With2014BackgroundThatGivesAFeat_RecordsTheFeat()
    {
        // Arrange - the Giant Foundling gives Strike of the Giants
        var draft = CreateCompleteDraft(_userId, level: 1);
        draft.SetBackground("giant_foundling");

        var background = CreateTestBackground();
        background.Id = "giant_foundling";
        background.Name = "Giant Foundling";
        background.FeatId = "strike_of_the_giants";

        // Act
        var created = await ArrangeAndFinalize(_draftId, _userId, draft, background: background);

        // Assert
        created.FeatIds.Should().Equal("strike_of_the_giants");
    }

    [Fact]
    public async Task Handle_WithCompleteDraft_CreatesCharacter()
    {
        // Arrange
        var draft = CreateCompleteDraft(_userId, level: 1);
        var race = CreateTestRace();
        var @class = CreateTestClass();
        var background = CreateTestBackground();

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync(race);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.ClassId!, _cts.Token)).ReturnsAsync(@class);
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.BackgroundId!, _cts.Token)).ReturnsAsync(background);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), _cts.Token), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
        draft.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AppliesAbilityScoreImprovementsFromDraft()
    {
        // Arrange
        // A level 6 fighter has two ASIs (gained at levels 4 and 6)
        var draft = CreateCompleteDraft(_userId, level: 6);
        draft.SetAbilityScoreImprovements(
        [
            new() { ["strength"] = 2 },
            new() { ["dexterity"] = 1, ["constitution"] = 1 }
        ]);

        var created = await ArrangeAndFinalize(_draftId, _userId, draft);

        // Assert - base STR 15 + 2, DEX 14 + 1, CON 13 + 1
        created.AbilityScores["STR"].Should().Be(17);
        created.AbilityScores["DEX"].Should().Be(15);
        created.AbilityScores["CON"].Should().Be(14);
        created.AbilityScores.Should().HaveCount(6);
    }

    [Fact]
    public async Task Handle_AppliesRacialBonusesUsingSeedStyleAbbreviations()
    {
        // Arrange
        var draft = CreateCompleteDraft(_userId, level: 1);
        var race = CreateTestRace();
        race.AbilityBonuses = [new RaceAbilityBonus { RaceId = "dwarf", AbilityScoreId = "con", Bonus = 2 }];

        var created = await ArrangeAndFinalize(_draftId, _userId, draft, race);

        // Assert - base CON 13 + 2, and no duplicate "con" entry
        created.AbilityScores["CON"].Should().Be(15);
        created.AbilityScores.Should().HaveCount(6);
    }

    [Fact]
    public async Task Handle_IncludesChosenRaceSkillsInProficiencies()
    {
        // Arrange
        // Half-elves choose two skill proficiencies
        var draft = CreateCompleteDraft(_userId, level: 1);
        draft.SetRace("half_elf");
        draft.SetRaceSkills(["perception", "stealth"]);

        var race = CreateTestRace();
        race.Id = "half_elf";
        race.Name = "Half-Elf";
        race.SkillChoiceCount = 2;
        race.LanguageChoiceCount = 1;
        race.RaceProficiencies = [new RaceProficiency { RaceId = "half_elf", ProficiencyId = "insight" }];

        // Half-elves also get one language choice
        draft.SetLanguages(["giant-phb"]);

        var created = await ArrangeAndFinalize(_draftId, _userId, draft, race);

        // Assert
        created.ProficiencyIds.Should().Contain(["perception", "stealth", "insight"]);
    }

    [Fact]
    public async Task Handle_ResolvesLanguagesToPhbIdsAndIncludesChosenOnes()
    {
        // Arrange
        var draft = CreateCompleteDraft(_userId, level: 1);
        draft.SetLanguages(["giant-phb"]);

        var race = CreateTestRace();
        race.RaceLanguages =
        [
            new RaceLanguage { RaceId = "dwarf", Language = "Common" },
            new RaceLanguage { RaceId = "dwarf", Language = "Dwarvish" }
        ];

        var background = CreateTestBackground();
        background.Languages = ["any"];

        var created = await ArrangeAndFinalize(_draftId, _userId, draft, race, background);

        // Assert - race language names resolved to ids, "any" placeholder dropped,
        // and the language the player chose for it included
        created.LanguageIds.Should().BeEquivalentTo(["common-phb", "dwarvish-phb", "giant-phb"]);
    }

    [Fact]
    public async Task Handle_WithNoChosenLanguages_KeepsOnlyGrantedLanguages()
    {
        // Arrange
        var draft = CreateCompleteDraft(_userId, level: 1);

        var race = CreateTestRace();
        race.RaceLanguages = [new RaceLanguage { RaceId = "dwarf", Language = "Common" }];

        // A dwarf soldier gets no language choices at all
        var background = CreateTestBackground();
        background.Languages = [];

        var created = await ArrangeAndFinalize(_draftId, _userId, draft, race, background);

        // Assert
        created.LanguageIds.Should().BeEquivalentTo(["common-phb"]);
    }

    [Fact]
    public async Task Handle_RecordsFeatsAndWhatTheyGrant()
    {
        // Arrange - a level 4 fighter spends its one improvement on a feat
        var draft = CreateCompleteDraft(_userId, level: 4);
        draft.SetAbilityScoreImprovements([]);
        draft.SetFeats(["linguist"]);

        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync([
                new FeatReadModel
                {
                    Id = "linguist",
                    Name = "Linguist",
                    Edition = "2014",
                    Description = "You learn three languages.",
                    GrantedProficiencyIds = ["calligrapher_supplies"],
                    GrantedLanguageIds = ["Elvish"],
                    GrantedSpellIds = []
                }
            ]);

        var created = await ArrangeAndFinalize(_draftId, _userId, draft);

        // Assert
        created.FeatIds.Should().BeEquivalentTo(["linguist"]);
        created.ProficiencyIds.Should().Contain("calligrapher_supplies");
        created.LanguageIds.Should().Contain("elvish-phb");
    }

    [Fact]
    public async Task Handle_WithAFeatTakenInsteadOfAnImprovement_CountsTowardsTheAsiTotal()
    {
        // Arrange - a level 6 fighter has two improvements: one spent on points, one on a feat
        var draft = CreateCompleteDraft(_userId, level: 6);
        draft.SetAbilityScoreImprovements([new Dictionary<string, int> { ["strength"] = 2 }]);
        draft.SetFeats(["alert"]);
        ArrangeFeat(new FeatPrerequisites(), id: "alert", name: "Alert");

        var created = await ArrangeAndFinalize(_draftId, _userId, draft);

        // Assert
        created.AbilityScores["STR"].Should().Be(17);
        created.FeatIds.Should().BeEquivalentTo(["alert"]);
    }

    [Fact]
    public async Task Handle_WithMissingAbilityScoreImprovements_ReturnsValidationError()
    {
        // Arrange - a level 4 fighter owes one ASI
        var draft = CreateCompleteDraft(_userId, level: 4);
        draft.SetAbilityScoreImprovements([]);

        ArrangeRepositories(_draftId, _userId, draft);

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.AbilityScoreImprovementsIncomplete");

        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithMissingRaceSkillChoices_ReturnsValidationError()
    {
        // Arrange - half-elves owe two skill choices
        var draft = CreateCompleteDraft(_userId, level: 1);
        draft.SetRace("half_elf");

        var race = CreateTestRace();
        race.Id = "half_elf";
        race.Name = "Half-Elf";
        race.SkillChoiceCount = 2;
        race.LanguageChoiceCount = 1;

        ArrangeRepositories(_draftId, _userId, draft, race);

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.RaceSkillChoicesIncomplete");
    }

    [Fact]
    public async Task Handle_WithMissingLanguageChoices_ReturnsValidationError()
    {
        // Arrange - the background grants one free language the player never picked
        var draft = CreateCompleteDraft(_userId, level: 1);

        var background = CreateTestBackground();
        background.Languages = ["any"];

        ArrangeRepositories(_draftId, _userId, draft, background: background);

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.LanguageChoicesIncomplete");
    }

    [Fact]
    public async Task Handle_WithTheHighElfsExtraLanguageUnpicked_ReturnsValidationError()
    {
        // Arrange - an elf picks no language, a high elf one
        var draft = CreateCompleteDraft(_userId, level: 1);
        draft.SetRace("elf", "high_elf");

        var race = CreateTestRace();
        race.Id = "elf";
        race.Name = "Elf";
        race.Subraces = [new Subrace { Id = "high_elf", Name = "High Elf", RaceId = "elf", LanguageChoiceCount = 1 }];

        ArrangeRepositories(_draftId, _userId, draft, race);

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.LanguageChoicesIncomplete");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithoutASubraceForARaceThatHasThem_ReturnsSubraceRequired()
    {
        // Arrange - a dwarf is a hill or a mountain dwarf
        var draft = CreateCompleteDraft(_userId, level: 1);

        var race = CreateTestRace();
        race.Subraces =
        [
            new Subrace { Id = "hill_dwarf", Name = "Hill Dwarf", RaceId = "dwarf" },
            new Subrace { Id = "mountain_dwarf", Name = "Mountain Dwarf", RaceId = "dwarf" },
        ];

        ArrangeRepositories(_draftId, _userId, draft, race);

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.SubraceRequired");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTheHalfElfsIncreasesPicked_AppliesThemBesideTheCharisma()
    {
        // Arrange - STR 15 and DEX 14 with the +1s picked, CHA 8 with the fixed +2
        var draft = CreateHalfElfDraft(_userId, new Dictionary<string, int> { ["STR"] = 1, ["DEX"] = 1 });

        // Act
        var created = await ArrangeAndFinalize(_draftId, _userId, draft, HalfElf());

        // Assert
        (created.AbilityScores["STR"], created.AbilityScores["DEX"], created.AbilityScores["CHA"]).Should().Be((16, 15, 10));
    }

    [Fact]
    public async Task Handle_WithTheHalfElfsIncreasesUnpicked_ReturnsRaceAbilityIncreasesIncomplete()
    {
        // Arrange
        var draft = CreateHalfElfDraft(_userId, increases: null);
        ArrangeRepositories(_draftId, _userId, draft, HalfElf());

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.RaceAbilityIncreasesIncomplete");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ForAVariantHumanWithoutItsFeat_ReturnsBonusFeatsIncomplete()
    {
        // Arrange - a level 1 fighter owes no improvements, but the variant human a feat
        var draft = CreateVariantHumanDraft(_userId);
        ArrangeRepositories(_draftId, _userId, draft, Human());

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.BonusFeatsIncomplete");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ForAVariantHumanWithAthlete_AppliesEveryPickedIncrease()
    {
        // Arrange - STR 15 and CON 13 with the variant's +1s, DEX 14 with Athlete's
        var draft = CreateVariantHumanDraft(_userId);
        draft.SetFeats(["athlete"], [new() { ["DEX"] = 1 }]);
        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync([Athlete()]);

        // Act
        var created = await ArrangeAndFinalize(_draftId, _userId, draft, Human());

        // Assert
        (created.AbilityScores["STR"], created.AbilityScores["DEX"], created.AbilityScores["CON"]).Should().Be((16, 15, 14));
        created.FeatIds.Should().Equal("athlete");
    }

    [Fact]
    public async Task Handle_WithAFeatIncreaseUnpicked_ReturnsFeatAbilityIncreaseIncomplete()
    {
        // Arrange - Athlete raises Strength or Dexterity, and the draft says neither
        var draft = CreateVariantHumanDraft(_userId);
        draft.SetFeats(["athlete"]);
        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync([Athlete()]);
        ArrangeRepositories(_draftId, _userId, draft, Human());

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.FeatAbilityIncreaseIncomplete");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAnEpicBoon_RaisesTheScorePastTwenty()
    {
        // Arrange - a level 19 2024 fighter: STR 15, +2 from the soldier background and +3 from
        // improvements is 20; Boon of Combat Prowess makes it 21, where any other feat stops
        var draft = Create2024SoldierDraft(new Dictionary<string, int> { ["STR"] = 2, ["CON"] = 1 });
        draft.SetNameAndLevel("Bruenor", 19);
        draft.SetClass("fighter-2024", "champion-2024");
        draft.SetAbilityScoreImprovements(
        [
            new() { ["STR"] = 2 },
            new() { ["STR"] = 1, ["CON"] = 1 },
            new() { ["DEX"] = 2 },
            new() { ["DEX"] = 2 },
            new() { ["CON"] = 2 },
            new() { ["WIS"] = 2 },
        ]);
        draft.SetFeats(["boon_of_combat_prowess"], [new() { ["STR"] = 1 }]);

        var fighter = Fighter2024();
        fighter.Subclasses = [new() { Id = "champion-2024", ClassId = "fighter-2024", Name = "Champion", Features = [] }];

        _featRepositoryMock.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<string>>(), _cts.Token)).ReturnsAsync([
            new FeatReadModel
            {
                Id = "boon_of_combat_prowess",
                Name = "Boon of Combat Prowess",
                Edition = RulesEdition.Edition2024,
                Category = "Epic Boon",
                Description = "You gain a boon.",
                AbilityIncrease = new FeatAbilityIncrease { Options = ["STR", "DEX", "CON", "INT", "WIS", "CHA"], Maximum = 30 }
            }
        ]);

        // Act
        var created = await ArrangeAndFinalize(_draftId, _userId, draft, Dwarf2024(), Soldier2024(), fighter);

        // Assert
        created.AbilityScores["STR"].Should().Be(21);
    }

    [Fact]
    public async Task Handle_AddsTheSpellsTheRaceGivesByTheCharactersLevel()
    {
        // Arrange - a level 3 tiefling has Thaumaturgy and Hellish Rebuke, and Darkness at 5
        var draft = CreateCompleteDraft(_userId, level: 3);
        draft.SetRace("tiefling");

        var race = CreateTestRace();
        race.Id = "tiefling";
        race.Name = "Tiefling";
        race.Spells =
        [
            new RaceSpell { RaceId = "tiefling", SpellId = "thaumaturgy", Level = 1 },
            new RaceSpell { RaceId = "tiefling", SpellId = "hellish_rebuke", Level = 3 },
            new RaceSpell { RaceId = "tiefling", SpellId = "darkness", Level = 5 },
        ];

        // Act
        var created = await ArrangeAndFinalize(_draftId, _userId, draft, race);

        // Assert
        created.SpellIds.Should().BeEquivalentTo(["thaumaturgy", "hellish_rebuke"]);
    }

    [Fact]
    public async Task Handle_AddsTheSpellsTheSubraceGivesByTheCharactersLevel()
    {
        // Arrange - a level 3 drow has Dancing Lights and Faerie Fire, and Darkness at 5
        var draft = CreateCompleteDraft(_userId, level: 3);
        draft.SetRace("elf", "drow_elf");

        var race = CreateTestRace();
        race.Id = "elf";
        race.Name = "Elf";
        race.Subraces =
        [
            new Subrace
            {
                Id = "drow_elf",
                Name = "Drow Elf",
                RaceId = "elf",
                Spells =
                [
                    new SubraceSpell { SubraceId = "drow_elf", SpellId = "dancing_lights", Level = 1 },
                    new SubraceSpell { SubraceId = "drow_elf", SpellId = "faerie_fire", Level = 3 },
                    new SubraceSpell { SubraceId = "drow_elf", SpellId = "darkness", Level = 5 },
                ]
            }
        ];

        // Act
        var created = await ArrangeAndFinalize(_draftId, _userId, draft, race);

        // Assert
        created.SpellIds.Should().BeEquivalentTo(["dancing_lights", "faerie_fire"]);
    }

    [Fact]
    public async Task Handle_WithMissingClassProficiencyChoices_ReturnsValidationError()
    {
        // Arrange - fighters choose two skills from their proficiency list
        var draft = CreateCompleteDraft(_userId, level: 1);

        var @class = CreateTestClass();
        @class.ProficiencyChoice = new ProficiencyChoice { ClassId = "fighter", Type = "Skill", Count = 2 };

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync(CreateTestRace());
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.ClassId!, _cts.Token)).ReturnsAsync(@class);
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.BackgroundId!, _cts.Token)).ReturnsAsync(CreateTestBackground());

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.ProficiencyChoicesIncomplete");
    }

    [Fact]
    public async Task Handle_WithLevel1ClericAndNoSubclass_ReturnsValidationError()
    {
        // Arrange - clerics pick a domain at level 1, not level 3
        var draft = CreateCompleteDraft(_userId, level: 1);
        draft.SetClass("cleric");

        var @class = CreateTestClass();
        @class.Id = "cleric";
        @class.Name = "Cleric";
        @class.SubclassLevel = 1;

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync(CreateTestRace());
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.ClassId!, _cts.Token)).ReturnsAsync(@class);
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.BackgroundId!, _cts.Token)).ReturnsAsync(CreateTestBackground());

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.SubclassRequired");
        result.Errors[0].Description.Should().Contain("level 1");
    }

    [Fact]
    public async Task Handle_WithARaceSkillTheBackgroundAlsoGives_ReturnsProficiencyAlreadyGiven()
    {
        // Arrange - the half-elf picked Athletics before taking a background that gives it, which
        // used to finalize with one proficiency fewer; the rules let the player pick another
        var draft = CreateHalfElfDraft(_userId, new Dictionary<string, int> { ["STR"] = 1, ["DEX"] = 1 });
        var background = CreateTestBackground();
        background.BackgroundProficiencies = [new BackgroundProficiency { BackgroundId = "soldier", ProficiencyId = "athletics" }];
        ArrangeRepositories(_draftId, _userId, draft, HalfElf(), background);

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.ProficiencyAlreadyGiven");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFeatAboveTheLevelReached_ReturnsFeatPrerequisiteNotMet()
    {
        // Arrange - the feat was taken at level 19, then the level was lowered to 4
        var draft = CreateCompleteDraft(_userId, level: 4);
        draft.SetAbilityScoreImprovements([]);
        draft.SetFeats(["boon_of_fortitude"]);
        ArrangeFeat(new FeatPrerequisites { MinimumLevel = 19 }, id: "boon_of_fortitude", name: "Boon of Fortitude");
        ArrangeRepositories(_draftId, _userId, draft);

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.FeatPrerequisiteNotMet");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFeatForAnotherRace_ReturnsFeatPrerequisiteNotMet()
    {
        // Arrange - Elven Accuracy was taken as an elf, then the race changed to dwarf
        var draft = CreateCompleteDraft(_userId, level: 4);
        draft.SetAbilityScoreImprovements([]);
        draft.SetFeats(["elven_accuracy"]);
        ArrangeFeat(new FeatPrerequisites { RaceIds = ["elf", "half_elf"] }, id: "elven_accuracy", name: "Elven Accuracy");
        ArrangeRepositories(_draftId, _userId, draft);

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.FeatPrerequisiteNotMet");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFeatForAnotherSubraceOfTheRace_ReturnsFeatPrerequisiteNotMet()
    {
        // Arrange - Wood Elf Magic names the wood elf; a high elf is an elf and still may not keep it
        var draft = CreateElfDraftWithWoodElfMagic("high_elf");
        ArrangeRepositories(_draftId, _userId, draft, CreateElfRace());

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.FeatPrerequisiteNotMet");
        result.Errors[0].Description.Should().Contain("Wood Elf");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFeatForTheCharactersSubrace_CreatesTheCharacter()
    {
        // Arrange
        var draft = CreateElfDraftWithWoodElfMagic("wood_elf");

        // Act
        var created = await ArrangeAndFinalize(_draftId, _userId, draft, CreateElfRace());

        // Assert
        created.FeatIds.Should().Equal("wood_elf_magic");
    }

    [Theory]
    [InlineData("Small", true)]
    [InlineData("Medium", false)]
    public async Task Handle_WithAFeatForASize_ChecksTheRacesSize(string size, bool expectedSuccess)
    {
        // Arrange - Squat Nimbleness takes a dwarf or a Small race; the race here is neither by id
        var draft = CreateCompleteDraft(_userId, level: 4);
        draft.SetAbilityScoreImprovements([]);
        draft.SetFeats(["squat_nimbleness"]);
        ArrangeFeat(new FeatPrerequisites { RaceIds = ["halfling"], Sizes = ["Small"] }, id: "squat_nimbleness", name: "Squat Nimbleness");

        var race = CreateTestRace();
        race.Size = size;

        ArrangeRepositories(_draftId, _userId, draft, race);

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().Be(expectedSuccess);
    }

    [Fact]
    public async Task Handle_WithAFeatForAnotherClass_ReturnsFeatPrerequisiteNotMet()
    {
        // Arrange - a feat asking for the Spellcasting feature, taken as a wizard, kept on a fighter
        var draft = CreateCompleteDraft(_userId, level: 4);
        draft.SetAbilityScoreImprovements([]);
        draft.SetFeats(["war_caster"]);
        ArrangeFeat(new FeatPrerequisites { Features = ["Spellcasting"] }, id: "war_caster", name: "War Caster");
        ArrangeRepositories(_draftId, _userId, draft);

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.FeatPrerequisiteNotMet");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFeatWhoseAbilityMinimumIsNoLongerMet_ReturnsFeatPrerequisiteNotMet()
    {
        // Arrange - Grappler asks for Strength 13; the base Strength was lowered to 12 afterwards
        var draft = CreateCompleteDraft(_userId, level: 4);
        draft.SetBaseAbilityScores(new Dictionary<string, int> { ["STR"] = 12, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 15, ["WIS"] = 10, ["CHA"] = 8 });
        draft.SetAbilityScoreImprovements([]);
        draft.SetFeats(["grappler"]);
        ArrangeFeat(new FeatPrerequisites { AbilityMinimums = [new AbilityMinimum { AbilityId = "STR", Score = 13 }] }, id: "grappler", name: "Grappler");
        ArrangeRepositories(_draftId, _userId, draft);

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.FeatPrerequisiteNotMet");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFeatTheReferenceDataNoLongerHas_ReturnsInvalidFeatIds()
    {
        // Arrange - a reseed dropped the feat; the repository finds nothing for its id
        var draft = CreateCompleteDraft(_userId, level: 4);
        draft.SetAbilityScoreImprovements([]);
        draft.SetFeats(["retired_feat"]);
        ArrangeRepositories(_draftId, _userId, draft);

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.InvalidFeatIds");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAFeatWhoseAbilityMinimumTheRaceMeets_CreatesTheCharacter()
    {
        // Arrange - base Strength 12 is 14 with the race's +2, enough for a feat asking for 13
        var draft = CreateCompleteDraft(_userId, level: 4);
        draft.SetBaseAbilityScores(new Dictionary<string, int> { ["STR"] = 12, ["DEX"] = 14, ["CON"] = 13, ["INT"] = 15, ["WIS"] = 10, ["CHA"] = 8 });
        draft.SetAbilityScoreImprovements([]);
        draft.SetFeats(["grappler"]);
        ArrangeFeat(new FeatPrerequisites { AbilityMinimums = [new AbilityMinimum { AbilityId = "STR", Score = 13 }] }, id: "grappler", name: "Grappler");

        var race = CreateTestRace();
        race.AbilityBonuses = [new RaceAbilityBonus { RaceId = "dwarf", AbilityScoreId = "STR", Bonus = 2 }];

        // Act
        var created = await ArrangeAndFinalize(_draftId, _userId, draft, race);

        // Assert
        created.FeatIds.Should().Equal("grappler");
    }

    [Fact]
    public async Task Handle_ForAWizardWithItsSpellsPicked_KeepsThem()
    {
        // Arrange - a level 1 wizard: two cantrips and two spells for its spellbook here
        var draft = CreateWizardDraft(["fire_bolt", "light", "shield", "magic_missile"]);
        ArrangeWizardSpellList();

        // Act
        var created = await ArrangeAndFinalize(_draftId, _userId, draft, @class: Wizard());

        // Assert
        created.SpellIds.Should().BeEquivalentTo(["fire_bolt", "light", "shield", "magic_missile"]);
    }

    [Fact]
    public async Task Handle_ForAWizardWithNoSpellsPicked_ReturnsCantripCountMismatch()
    {
        // Arrange - the spells step was never reached, which used to finalize a wizard with no spells
        var draft = CreateWizardDraft([]);
        ArrangeWizardSpellList();
        ArrangeRepositories(_draftId, _userId, draft, @class: Wizard());

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Spell.CantripCountMismatch");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithASpellAboveTheLevelReached_ReturnsLevelTooHigh()
    {
        // Arrange - picked at level 3, then the level was lowered to 1, where Hold Person is out of reach
        var draft = CreateWizardDraft(["fire_bolt", "light", "shield", "hold_person"]);
        ArrangeWizardSpellList();
        ArrangeRepositories(_draftId, _userId, draft, @class: Wizard());

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Spell.LevelTooHigh");
        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ForAFighterWithSpellsLeftFromAnotherClass_ReturnsNoSpellcasting()
    {
        // Arrange - the spells were picked as a wizard before the class changed to fighter
        var draft = CreateCompleteDraft(_userId, level: 1);
        draft.SetSpells(["fire_bolt"]);
        ArrangeRepositories(_draftId, _userId, draft);

        // Act
        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Spell.NoSpellcasting");
        _spellRepositoryMock.Verify(r => r.GetByClassIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Stubs the lookup repositories so the handler can get as far as building a Character.
    /// </summary>
    private void ArrangeRepositories(
        Guid _draftId,
        Guid _userId,
        CharacterDraft draft,
        Race? race = null,
        Background? background = null,
        Class? @class = null)
    {
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync(race ?? CreateTestRace());
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.ClassId!, _cts.Token)).ReturnsAsync(@class ?? CreateTestClass());
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.BackgroundId!, _cts.Token)).ReturnsAsync(background ?? CreateTestBackground());
    }

    /// <summary>
    /// Runs a finalize that is expected to succeed and returns the Character that was
    /// handed to the repository.
    /// </summary>
    private async Task<Character> ArrangeAndFinalize(
        Guid _draftId,
        Guid _userId,
        CharacterDraft draft,
        Race? race = null,
        Background? background = null,
        Class? @class = null)
    {
        ArrangeRepositories(_draftId, _userId, draft, race, background, @class);

        var result = await _sut.Handle(new FinalizeCharacterCommand(_draftId), _cts.Token);

        result.IsSuccess.Should().BeTrue(because: string.Join("; ", result.Errors.Select(e => e.Description)));
        _createdCharacter.Should().NotBeNull();

        return _createdCharacter!;
    }

    [Fact]
    public async Task Handle_WithNonExistentDraft_ReturnsNotFoundError()
    {
        // Arrange
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync((CharacterDraft?)null);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.NotFound");

        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithIncompleteDraft_ReturnsValidationError()
    {
        // Arrange
        var draft = CreateTestDraft(_userId);
        // Draft is incomplete - missing required fields
        draft.SetNameAndLevel("Test", 1);
        // Missing: Race, Class, Background, AbilityScores

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("CharacterDraft.Invalid");

        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    public async Task Handle_WithLevel3OrHigher_WithoutSubclass_ReturnsValidationError(int level)
    {
        // Arrange - D&D 5e rule: All classes require subclass at level 3
        var draft = CreateCompleteDraft(_userId, level);
        draft.SetSubclass(null);

        ArrangeRepositories(_draftId, _userId, draft);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.SubclassRequired");
        result.Errors[0].Description.Should().Contain($"level {level}");

        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Handle_WithLevel1Or2_WithoutSubclass_Succeeds(int level)
    {
        // Arrange - Levels 1-2 don't require subclass
        var draft = CreateCompleteDraft(_userId, level);
        draft.SetSubclass(null);

        var race = CreateTestRace();
        var @class = CreateTestClass();
        var background = CreateTestBackground();

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync(race);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.ClassId!, _cts.Token)).ReturnsAsync(@class);
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.BackgroundId!, _cts.Token)).ReturnsAsync(background);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    // Base scores are what the player rolled or bought, before racial bonuses and improvements.
    // 4d6-drop-lowest tops out at 18 at every level, and improvements are applied on top rather
    // than folded into the base, so the ceiling no longer varies with level.
    [Theory]
    [InlineData(1, 18)]
    [InlineData(3, 18)]
    [InlineData(4, 18)]
    [InlineData(8, 18)]
    [InlineData(20, 18)]
    public async Task Handle_WithValidAbilityScoresForLevel_Succeeds(int level, int maxScore)
    {
        // Arrange
        var draft = CreateCompleteDraft(_userId, level);
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["STR"] = maxScore,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["WIS"] = 10,
            ["CHA"] = 8
        });

        var race = CreateTestRace();
        var @class = CreateTestClass();
        var background = CreateTestBackground();

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync(race);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.ClassId!, _cts.Token)).ReturnsAsync(@class);
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.BackgroundId!, _cts.Token)).ReturnsAsync(background);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(1, 19)]  // Level 1 max is 18
    [InlineData(3, 21)]  // Level 3 max is 18
    [InlineData(4, 21)]  // Level 4 max is 20
    public async Task Handle_WithAbilityScoresTooHighForLevel_ReturnsValidationError(int level, int invalidScore)
    {
        // Arrange
        var draft = CreateCompleteDraft(_userId, level);
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["STR"] = invalidScore, // Too high for this level
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["WIS"] = 10,
            ["CHA"] = 8
        });

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.AbilityScoreTooHigh");
        result.Errors[0].Description.Should().Contain($"level {level}");

        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentRace_ReturnsNotFoundError()
    {
        // Arrange
        var draft = CreateCompleteDraft(_userId, level: 1);

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync((Race?)null);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.RaceNotFound");

        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentClass_ReturnsNotFoundError()
    {
        // Arrange
        var draft = CreateCompleteDraft(_userId, level: 1);
        var race = CreateTestRace();

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync(race);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.ClassId!, _cts.Token)).ReturnsAsync((Class?)null);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.ClassNotFound");

        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNonExistentBackground_ReturnsNotFoundError()
    {
        // Arrange
        var draft = CreateCompleteDraft(_userId, level: 1);
        var race = CreateTestRace();
        var @class = CreateTestClass();

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync(race);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.ClassId!, _cts.Token)).ReturnsAsync(@class);
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.BackgroundId!, _cts.Token)).ReturnsAsync((Background?)null);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.BackgroundNotFound");

        _characterRepositoryMock.Verify(r => r.Create(It.IsAny<Character>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AppliesRacialAbilityBonuses()
    {
        // Arrange
        var draft = CreateCompleteDraft(_userId, level: 1);
        var race = CreateTestRace();
        // Add racial bonus: +2 CON for dwarves
        race.AbilityBonuses.Add(new RaceAbilityBonus
        {
            RaceId = "dwarf",
            AbilityScoreId = "CON",
            Bonus = 2
        });

        var @class = CreateTestClass();
        var background = CreateTestBackground();

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync(race);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.ClassId!, _cts.Token)).ReturnsAsync(@class);
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.BackgroundId!, _cts.Token)).ReturnsAsync(background);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _createdCharacter.Should().NotBeNull();
        // Base CON was 13, racial bonus +2 = 15
        _createdCharacter!.AbilityScores["CON"].Should().Be(15);
    }

    [Fact]
    public async Task Handle_AppliesSubracialAbilityBonuses()
    {
        // Arrange
        var draft = CreateCompleteDraft(_userId, level: 1);
        draft.SetRace("elf", "high-elf");

        var race = CreateTestRace();
        race.Id = "elf";
        var subrace = new Subrace
        {
            Id = "high-elf",
            RaceId = "elf",
            Name = "High Elf",
            AbilityBonuses =
            [
                new() { SubraceId = "high-elf", AbilityScoreId = "INT", Bonus = 1 }
            ],
            SubraceProficiencies = [],
            Traits = []
        };
        race.Subraces.Add(subrace);

        var @class = CreateTestClass();
        var background = CreateTestBackground();

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync(race);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.ClassId!, _cts.Token)).ReturnsAsync(@class);
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.BackgroundId!, _cts.Token)).ReturnsAsync(background);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        var result = await _sut.Handle(command, _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _createdCharacter.Should().NotBeNull();
        // Base INT was 12, subrace bonus +1 = 13
        _createdCharacter!.AbilityScores["INT"].Should().Be(13);
    }

    [Fact]
    public async Task Handle_MarksDraftAsCompleted()
    {
        // Arrange
        var draft = CreateCompleteDraft(_userId, level: 1);
        var race = CreateTestRace();
        var @class = CreateTestClass();
        var background = CreateTestBackground();

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync(race);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.ClassId!, _cts.Token)).ReturnsAsync(@class);
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.BackgroundId!, _cts.Token)).ReturnsAsync(background);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        await _sut.Handle(command, _cts.Token);

        // Assert
        draft.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UsesTimeProvider()
    {
        // Arrange
        var draft = CreateCompleteDraft(_userId, level: 1);
        var race = CreateTestRace();
        var @class = CreateTestClass();
        var background = CreateTestBackground();

        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.RaceId!, _cts.Token)).ReturnsAsync(race);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.ClassId!, _cts.Token)).ReturnsAsync(@class);
        _backgroundRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(draft.BackgroundId!, _cts.Token)).ReturnsAsync(background);

        var command = new FinalizeCharacterCommand(_draftId);

        // Act
        await _sut.Handle(command, _cts.Token);

        // Assert
        _createdCharacter.Should().NotBeNull();
        _createdCharacter!.CreatedAt.Should().Be(_timeProvider.GetUtcNow());
    }

    // A 2014 draft, for the 2014 dwarf fighter soldier the other helpers build
    private static CharacterDraft CreateTestDraft(Guid _userId)
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));
        var draft = CharacterDraft.CreateEmpty(_userId, timeProvider);
        draft.ChooseEdition(RulesEdition.Edition2014);
        return draft;
    }

    private static CharacterDraft CreateCompleteDraft(Guid _userId, int level)
    {
        var draft = CreateTestDraft(_userId);
        draft.SetNameAndLevel("Thorin Oakenshield", level);
        draft.SetRace("dwarf");
        draft.SetClass("fighter", level >= 3 ? "champion" : null);
        draft.SetBackground("soldier");
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["STR"] = 15,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["WIS"] = 10,
            ["CHA"] = 8
        });
        draft.SetAbilityScoreImprovements(AsisForLevel(level));
        return draft;
    }

    /// <summary>
    /// The ability score improvements a fighter of this level is entitled to. Finalize
    /// rejects a draft whose ASI count does not match, so a "complete" draft needs them.
    /// </summary>
    private static List<Dictionary<string, int>> AsisForLevel(int level) =>
        [.. Enumerable
            .Range(0, CreateTestClass().AsiLevels.Count(l => l <= level))
            .Select(_ => new Dictionary<string, int> { ["strength"] = 2 })];

    private CharacterDraft CreateElfDraftWithWoodElfMagic(string subraceId)
    {
        var draft = CreateCompleteDraft(_userId, level: 4);
        draft.SetRace("elf", subraceId);
        draft.SetAbilityScoreImprovements([]);
        draft.SetFeats(["wood_elf_magic"]);
        ArrangeFeat(new FeatPrerequisites { SubraceIds = ["wood_elf"] }, id: "wood_elf_magic", name: "Wood Elf Magic");
        return draft;
    }

    private static Race CreateElfRace()
    {
        var race = CreateTestRace();
        race.Id = "elf";
        race.Name = "Elf";
        race.Subraces =
        [
            new Subrace { Id = "high_elf", Name = "High Elf", RaceId = "elf" },
            new Subrace { Id = "wood_elf", Name = "Wood Elf", RaceId = "elf" },
        ];
        return race;
    }

    private static Race CreateTestRace()
    {
        return new Race
        {
            Id = "dwarf",
            Name = "Dwarf",
            Edition = RulesEdition.Edition2014,
            Speed = 25,
            Size = "Medium",
            AbilityBonuses = [],
            RaceLanguages = [],
            RaceProficiencies = [],
            Traits = [],
            Subraces = []
        };
    }

    private static Class CreateTestClass()
    {
        return new Class
        {
            Id = "fighter",
            Name = "Fighter",
            Edition = RulesEdition.Edition2014,
            HitDie = "1d10",
            SubclassLevel = 3,
            AsiLevels = [4, 6, 8, 12, 14, 16, 19],
            SavingThrowAbilityIds = ["STR", "CON"],
            ClassProficiencies = [],
            Features = [],
            Subclasses =
            [
                new() { Id = "champion", ClassId = "fighter", Name = "Champion", Features = [] }
            ]
        };
    }

    // The one feat a soldier with no feat of its own picked; the lookup is pinned to its id
    private void ArrangeFeat(FeatPrerequisites prerequisites, string id, string name)
    {
        _featRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { id })), _cts.Token))
            .ReturnsAsync(
            [
                new FeatReadModel
                {
                    Id = id,
                    Name = name,
                    Edition = RulesEdition.Edition2014,
                    Description = "A feat.",
                    Prerequisites = prerequisites
                }
            ]);
    }

    // A level 1 dwarf wizard soldier with these spells picked
    private CharacterDraft CreateWizardDraft(List<string> spellIds)
    {
        var draft = CreateCompleteDraft(_userId, level: 1);
        draft.SetClass("wizard");
        draft.SetSpells(spellIds);
        return draft;
    }

    // Two cantrips and two level 1 spells at level 1, a shorter table than the book's
    private static Class Wizard()
    {
        var wizard = CreateTestClass();
        wizard.Id = "wizard";
        wizard.Name = "Wizard";
        wizard.HitDie = "1d6";
        wizard.SubclassLevel = 2;
        wizard.Subclasses = [];
        wizard.SpellProgression =
        [
            new ClassSpellProgression { ClassId = "wizard", CharacterLevel = 1, CantripsKnown = 2, SpellsKnown = 2, MaxSpellLevel = 1 }
        ];
        return wizard;
    }

    private void ArrangeWizardSpellList()
    {
        _spellRepositoryMock.Setup(r => r.GetByClassIdAsync("wizard", _cts.Token)).ReturnsAsync(
        [
            WizardSpell("fire_bolt", 0),
            WizardSpell("light", 0),
            WizardSpell("shield", 1),
            WizardSpell("magic_missile", 1),
            WizardSpell("hold_person", 2),
        ]);
    }

    private static SpellReadModel WizardSpell(string id, int level) => new()
    {
        Id = id,
        Name = id,
        Level = level,
        School = "Evocation",
        CastingTimeUnit = "action",
        RangeType = "point",
        DurationType = "instant",
        Description = "A spell."
    };

    private static Background CreateTestBackground()
    {
        return new Background
        {
            Id = "soldier",
            Name = "Soldier",
            Edition = RulesEdition.Edition2014,
            FeatureId = "military-rank",
            Languages = [],
            BackgroundProficiencies = []
        };
    }

    // A level 1 2024 dwarf fighter soldier, who picked Draconic and Giant besides Common
    private static CharacterDraft Create2024SoldierDraft(Dictionary<string, int> increases)
    {
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero));
        var draft = CharacterDraft.CreateEmpty(Guid.NewGuid(), timeProvider);
        draft.ChooseEdition(RulesEdition.Edition2024);
        draft.SetNameAndLevel("Bruenor", 1);
        draft.SetRace("dwarf_2024");
        draft.SetClass("fighter-2024");
        draft.SetBackground("soldier_2024", increases);
        draft.SetLanguages(["draconic-xphb", "giant-xphb"]);
        draft.SetBaseAbilityScores(new Dictionary<string, int>
        {
            ["STR"] = 15,
            ["DEX"] = 14,
            ["CON"] = 13,
            ["INT"] = 12,
            ["WIS"] = 10,
            ["CHA"] = 8
        });
        return draft;
    }

    // A level 1 half-elf fighter soldier with its two skills and a language picked, and these +1s
    private static CharacterDraft CreateHalfElfDraft(Guid _userId, Dictionary<string, int>? increases)
    {
        var draft = CreateCompleteDraft(_userId, level: 1);
        draft.SetRace("half_elf", abilityIncreases: increases);
        draft.SetRaceSkills(["athletics", "stealth"]);
        draft.SetLanguages(["giant-phb"]);
        return draft;
    }

    // +2 Charisma and +1 to two others of the player's choice, two skills and a language
    private static Race HalfElf()
    {
        var race = CreateTestRace();
        race.Id = "half_elf";
        race.Name = "Half-Elf";
        race.Speed = 30;
        race.SkillChoiceCount = 2;
        race.LanguageChoiceCount = 1;
        race.AbilityChoiceCount = 2;
        race.AbilityChoiceOptions = ["STR", "DEX", "CON", "INT", "WIS"];
        race.AbilityBonuses = [new RaceAbilityBonus { RaceId = "half_elf", AbilityScoreId = "CHA", Bonus = 2 }];
        return race;
    }

    // A level 1 variant human fighter soldier with +1 Strength and Constitution, a skill and a
    // language picked, and no feat yet
    private static CharacterDraft CreateVariantHumanDraft(Guid _userId)
    {
        var draft = CreateCompleteDraft(_userId, level: 1);
        draft.SetRace("human", "variant_human", new Dictionary<string, int> { ["STR"] = 1, ["CON"] = 1 });
        draft.SetRaceSkills(["persuasion"]);
        draft.SetLanguages(["giant-phb"]);
        return draft;
    }

    // A language of the player's choice; the standard human's +1 to everything, or the variant's
    // +1 to two, a skill and a feat
    private static Race Human()
    {
        var race = CreateTestRace();
        race.Id = "human";
        race.Name = "Human";
        race.Speed = 30;
        race.LanguageChoiceCount = 1;
        race.Subraces =
        [
            new Subrace
            {
                Id = "standard_human",
                Name = "Standard Human",
                RaceId = "human",
                AbilityBonuses = [.. new[] { "STR", "DEX", "CON", "INT", "WIS", "CHA" }.Select(id => new SubraceAbilityBonus { SubraceId = "standard_human", AbilityScoreId = id, Bonus = 1 })]
            },
            new Subrace
            {
                Id = "variant_human",
                Name = "Variant Human",
                RaceId = "human",
                AbilityChoiceCount = 2,
                AbilityChoiceOptions = ["STR", "DEX", "CON", "INT", "WIS", "CHA"],
                SkillChoiceCount = 1,
                FeatChoiceCount = 1
            },
        ];
        return race;
    }

    // +1 to Strength or Dexterity
    private static FeatReadModel Athlete() => new()
    {
        Id = "athlete",
        Name = "Athlete",
        Edition = RulesEdition.Edition2014,
        Description = "You have undergone extensive physical training.",
        AbilityIncrease = new FeatAbilityIncrease { Options = ["STR", "DEX"] }
    };

    private static Race Dwarf2024() => new()
    {
        Id = "dwarf_2024",
        Name = "Dwarf",
        Edition = RulesEdition.Edition2024,
        Size = "Medium",
        Speed = 30
    };

    private static Class Fighter2024()
    {
        var fighter = CreateTestClass();
        fighter.Id = "fighter-2024";
        fighter.Edition = RulesEdition.Edition2024;
        fighter.Subclasses = [];
        return fighter;
    }

    // Savage Attacker, +2/+1 or +1 to each of Strength, Dexterity and Constitution, and the
    // Common and two picks the 2024 rules give every character
    private static Background Soldier2024() => new()
    {
        Id = "soldier_2024",
        Name = "Soldier",
        Edition = RulesEdition.Edition2024,
        AbilityScoreOptions = ["STR", "DEX", "CON"],
        FeatId = "savage_attacker_2024",
        Languages = ["common", "any", "any"]
    };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}

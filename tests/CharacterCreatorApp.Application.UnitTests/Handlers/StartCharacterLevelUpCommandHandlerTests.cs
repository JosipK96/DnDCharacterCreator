using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterManagement.LevelUpCharacter;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class StartCharacterLevelUpCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterRepository> _characterRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 10, 23, 9, 0, 0, TimeSpan.Zero));
    private readonly CancellationTokenSource _cts = new();
    private readonly StartCharacterLevelUpCommandHandler _sut;

    private readonly Guid _userId = Guid.CreateVersion7();
    private readonly Character _character;
    private CharacterDraft? _created;

    public StartCharacterLevelUpCommandHandlerTests()
    {
        _character = Mirabel(level: 4);

        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);
        _characterRepositoryMock.Setup(r => r.GetByIdAsync(_character.Id, _userId, _cts.Token)).ReturnsAsync(_character);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("wizard", _cts.Token)).ReturnsAsync(Wizard());
        _draftRepositoryMock.Setup(r => r.GetEditAsync(_character.Id, _userId, _cts.Token)).ReturnsAsync((CharacterDraft?)null);
        _draftRepositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<CharacterDraft>(), _cts.Token))
            .Callback<CharacterDraft, CancellationToken>((draft, _) => _created = draft)
            .Returns(Task.CompletedTask);

        _sut = new StartCharacterLevelUpCommandHandler(
            _characterRepositoryMock.Object,
            _draftRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _classRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object,
            _timeProvider);
    }

    [Fact]
    public async Task Handle_WithABuild_OpensItOneLevelUpWithEveryPickAndTheStepsTheLevelBrings()
    {
        // Arrange - a wizard's fifth level opens 3rd-level spells, and brings no improvement
        var build = Build();
        _draftRepositoryMock.Setup(r => r.GetBuildAsync(_character.Id, _userId, _cts.Token)).ReturnsAsync(build);

        // Act
        var result = await _sut.Handle(new StartCharacterLevelUpCommand(_character.Id), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(_created!.Id);
        _created.CharacterId.Should().Be(_character.Id);
        _created.IsCompleted.Should().BeFalse();
        _created.Level.Should().Be(5);
        _created.SelectedSpellIds.Should().Equal("fire_bolt", "shield");
        _created.AbilityScoreImprovements.Should().ContainSingle();
        _created.LevelUpChoices.Should().Equal(DraftChoice.HitPoints, DraftChoice.Spells);
        _created.ChoicesNeeded.Should().Equal(DraftChoice.HitPoints, DraftChoice.Spells);
        build.Level.Should().Be(4, "the build stays as the character was saved");

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAnEditLeftUnsaved_DropsItAndStartsFromTheBuild()
    {
        // Arrange - a character has one unsaved edit or level-up at most
        var build = Build();
        var unsaved = CharacterDraft.StartEdit(build, _timeProvider);
        _draftRepositoryMock.Setup(r => r.GetEditAsync(_character.Id, _userId, _cts.Token)).ReturnsAsync(unsaved);
        _draftRepositoryMock.Setup(r => r.DeleteAsync(unsaved, _cts.Token)).Returns(Task.CompletedTask);
        _draftRepositoryMock.Setup(r => r.GetBuildAsync(_character.Id, _userId, _cts.Token)).ReturnsAsync(build);

        // Act
        var result = await _sut.Handle(new StartCharacterLevelUpCommand(_character.Id), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _created!.Id.Should().NotBe(unsaved.Id);
        _created.Level.Should().Be(5);
        _draftRepositoryMock.Verify(r => r.DeleteAsync(unsaved, _cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutABuild_AlsoAsksForTheStepsTheCharacterKeepsNoChoiceFor()
    {
        // Arrange - a character saved before the drafts were linked to their characters
        _draftRepositoryMock.Setup(r => r.GetBuildAsync(_character.Id, _userId, _cts.Token)).ReturnsAsync((CharacterDraft?)null);
        _raceRepositoryMock.Setup(r => r.GetByIdWithSubracesAsync("elf", _cts.Token)).ReturnsAsync(new Race
        {
            Id = "elf",
            Name = "Elf",
            Edition = RulesEdition.Edition2014,
            Source = "PHB",
            Size = "Medium",
            Subraces = [new Subrace { Id = "elf_high", RaceId = "elf", Name = "High Elf", Source = "PHB" }],
        });
        _classRepositoryMock.Setup(r => r.GetByIdWithSubclassesAsync("wizard", _cts.Token)).ReturnsAsync(Wizard());
        _backgroundRepositoryMock.Setup(r => r.GetByIdAsync("sage", _cts.Token))
            .ReturnsAsync(new BackgroundReadModel { Id = "sage", Name = "Sage", Edition = RulesEdition.Edition2014, Source = "PHB" });

        // Act
        var result = await _sut.Handle(new StartCharacterLevelUpCommand(_character.Id), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        _created!.Level.Should().Be(5);
        _created.LevelUpChoices.Should().Equal(
            new[] { DraftChoice.HitPoints, DraftChoice.Spells }.Concat(DraftChoice.NotKeptByCharacter).Distinct());
    }

    [Fact]
    public async Task Handle_ACharacterAtTheHighestLevel_ReturnsAtMaxLevelAndOpensNothing()
    {
        // Arrange
        var highest = Mirabel(level: CharacterDraft.MaxLevel);
        _characterRepositoryMock.Setup(r => r.GetByIdAsync(highest.Id, _userId, _cts.Token)).ReturnsAsync(highest);

        // Act
        var result = await _sut.Handle(new StartCharacterLevelUpCommand(highest.Id), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.AtMaxLevel");
        _draftRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<CharacterDraft>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SomeoneElsesCharacter_ReturnsNotFoundAndOpensNothing()
    {
        // Arrange - the repository finds only the user's own characters
        var someoneElses = Guid.CreateVersion7();
        _characterRepositoryMock.Setup(r => r.GetByIdAsync(someoneElses, _userId, _cts.Token)).ReturnsAsync((Character?)null);

        // Act
        var result = await _sut.Handle(new StartCharacterLevelUpCommand(someoneElses), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.NotFound");
        _draftRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<CharacterDraft>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AClassTheDataNoLongerHas_ReturnsClassNotFoundAndOpensNothing()
    {
        // Arrange
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("wizard", _cts.Token)).ReturnsAsync((Class?)null);

        // Act
        var result = await _sut.Handle(new StartCharacterLevelUpCommand(_character.Id), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors[0].Code.Should().Be("Character.ClassNotFound");
        _draftRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<CharacterDraft>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }

    private Character Mirabel(int level) => Character.Create(
        _userId, "Mirabel", level, RulesEdition.Edition2014, "elf", "elf_high", "wizard", "wizard_evocation", "sage",
        new Dictionary<string, int> { ["STR"] = 8, ["DEX"] = 16, ["CON"] = 14, ["INT"] = 17, ["WIS"] = 12, ["CHA"] = 10 },
        _timeProvider);

    // A 2014 wizard's data as far as levels 4 and 5 go
    private static Class Wizard() => new()
    {
        Id = "wizard",
        Name = "Wizard",
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        HitDie = "1d6",
        SubclassLevel = 2,
        AsiLevels = [4, 8, 12, 16, 19],
        SpellProgression =
        [
            new ClassSpellProgression { ClassId = "wizard", CharacterLevel = 4, CantripsKnown = 4, SpellsKnown = 12, MaxSpellLevel = 2 },
            new ClassSpellProgression { ClassId = "wizard", CharacterLevel = 5, CantripsKnown = 4, SpellsKnown = 14, MaxSpellLevel = 3 },
        ],
        Subclasses = [new Subclass { Id = "wizard_evocation", ClassId = "wizard", Name = "Evocation", Source = "PHB" }],
    };

    // The completed draft Mirabel was made from, at level 4 with its improvement
    private CharacterDraft Build()
    {
        var build = CharacterDraft.CreateEmpty(_userId, _timeProvider);
        build.ChooseEdition(RulesEdition.Edition2014);
        build.SetNameAndLevel("Mirabel", 4);
        build.SetRace("elf", "elf_high");
        build.SetClass("wizard", "wizard_evocation");
        build.SetBackground("sage");
        build.SetBaseAbilityScores(
            new Dictionary<string, int> { ["STR"] = 8, ["DEX"] = 15, ["CON"] = 14, ["INT"] = 15, ["WIS"] = 12, ["CHA"] = 10 },
            AbilityScoreMethod.PointBuy);
        build.SetAbilityScoreImprovements([new Dictionary<string, int> { ["INT"] = 2 }]);
        build.SetSpells(["fire_bolt", "shield"]);
        build.MarkAsCompleted(_character.Id);
        return build;
    }
}

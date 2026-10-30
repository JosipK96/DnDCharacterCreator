using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.ChooseLanguages;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Races;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class ChooseLanguagesCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ILanguageRepository> _languageRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly ChooseLanguagesCommandHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");

    public ChooseLanguagesCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);

        // What GetChoosableAsync returns for 2014: the PHB list without Druidic and Thieves' Cant
        _languageRepositoryMock
            .Setup(r => r.GetChoosableAsync(RulesEdition.Edition2014, _cts.Token))
            .ReturnsAsync(
            [
                Language("celestial-phb", "Celestial", "exotic"),
                Language("common-phb", "Common", "standard"),
                Language("dwarvish-phb", "Dwarvish", "standard"),
                Language("elvish-phb", "Elvish", "standard"),
                Language("gnomish-phb", "Gnomish", "standard"),
                Language("goblin-phb", "Goblin", "standard"),
                Language("sylvan-phb", "Sylvan", "exotic"),
            ]);

        // And for 2024: the 2024 book's standard languages only
        _languageRepositoryMock
            .Setup(r => r.GetChoosableAsync(RulesEdition.Edition2024, _cts.Token))
            .ReturnsAsync(
            [
                Language("common-xphb", "Common", "standard"),
                Language("draconic-xphb", "Draconic", "standard"),
                Language("dwarvish-xphb", "Dwarvish", "standard"),
                Language("elvish-xphb", "Elvish", "standard"),
                Language("giant-xphb", "Giant", "standard"),
            ]);

        _sut = new ChooseLanguagesCommandHandler(
            _draftRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _languageRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithLanguagesTheCharacterDoesNotKnowYet_SavesThem()
    {
        // Arrange - a hill dwarf acolyte: Common and Dwarvish from the race, two free picks
        var draft = ArrangeHillDwarfAcolyte();

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["celestial-phb", "elvish-phb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedLanguageIds.Should().Equal("celestial-phb", "elvish-phb");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithALanguageTheRaceAlreadyGrants_ReturnsLanguagesAlreadyKnown()
    {
        // Arrange - the character would be a language short: the builder keeps each one once
        var draft = ArrangeHillDwarfAcolyte();

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["dwarvish-phb", "celestial-phb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.LanguagesAlreadyKnown");
        result.Errors[0].Description.Should().Contain("Dwarvish").And.NotContain("Celestial");
        draft.SelectedLanguageIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithALanguageTheBackgroundAlreadyGrants_ReturnsLanguagesAlreadyKnown()
    {
        // Arrange - Feylost names Elvish, Gnomish, Goblin and Sylvan, plus one free pick; a
        // human adds Common and one more
        var draft = ArrangeDraft(raceId: "human", backgroundId: "feylost");
        ArrangeRace(RaceWithLanguages("human", "Human", ["Common"], languageChoiceCount: 1));
        ArrangeBackground(new BackgroundReadModel
        {
            Id = "feylost",
            Name = "Feylost",
            LanguageChoiceCount = 1,
            Languages =
            [
                new BackgroundLanguageReadModel { Id = "elvish", Name = "Elvish" },
                new BackgroundLanguageReadModel { Id = "gnomish", Name = "Gnomish" },
                new BackgroundLanguageReadModel { Id = "goblin", Name = "Goblin" },
                new BackgroundLanguageReadModel { Id = "sylvan", Name = "Sylvan" },
            ],
        });

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["celestial-phb", "elvish-phb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.LanguagesAlreadyKnown");
        result.Errors[0].Description.Should().Contain("Elvish").And.NotContain("Celestial");
        draft.SelectedLanguageIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithASecretLanguage_ReturnsInvalidLanguageIds()
    {
        // Arrange - Druidic comes with the druid class and is not on the choosable list
        var draft = ArrangeHillDwarfAcolyte();

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["druidic-phb", "celestial-phb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidLanguageIds");
        result.Errors[0].Description.Should().Contain("druidic-phb").And.NotContain("celestial-phb");
        draft.SelectedLanguageIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(new[] { "celestial-phb", "dwarvish-phb", "goblin-phb" }, true)]
    [InlineData(new[] { "celestial-phb", "dwarvish-phb" }, false)]
    public async Task Handle_ForAHighElf_CountsTheSubracesExtraLanguage(string[] languageIds, bool accepted)
    {
        // Arrange - an elf acolyte picks two, a high elf acolyte three
        var draft = ArrangeDraft(raceId: "elf", backgroundId: "acolyte", subraceId: "high_elf");
        var elf = RaceWithLanguages("elf", "Elf", ["Common", "Elvish"]);
        elf.Subraces = [new Subrace { Id = "high_elf", Name = "High Elf", Source = "PHB", RaceId = "elf", LanguageChoiceCount = 1 }];
        ArrangeRace(elf);
        ArrangeBackground(new BackgroundReadModel { Id = "acolyte", Name = "Acolyte", LanguageChoiceCount = 2 });

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, [.. languageIds]), _cts.Token);

        // Assert
        result.IsSuccess.Should().Be(accepted);
        draft.SelectedLanguageIds.Should().Equal(accepted ? languageIds : []);

        if (!accepted)
        {
            result.Errors.Should().ContainSingle().Which.Description.Should().Be("Must choose exactly 3 language(s). You selected 2.");
        }
    }

    [Fact]
    public async Task Handle_For2024Character_SavesTwoStandardLanguagesBesideCommon()
    {
        // Arrange - the 2024 rules give Common and two picks, which the 2024 background carries
        var draft = ArrangeDwarfAcolyte2024();

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["draconic-xphb", "giant-xphb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedLanguageIds.Should().Equal("draconic-xphb", "giant-xphb");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_For2024CharacterWithARareLanguage_ReturnsInvalidLanguageIds()
    {
        // Arrange - Celestial is on the 2024 book's rare list, which a class feature gives
        var draft = ArrangeDwarfAcolyte2024();

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["draconic-xphb", "celestial-xphb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidLanguageIds");
        result.Errors[0].Description.Should().Contain("celestial-xphb").And.NotContain("draconic-xphb");
        draft.SelectedLanguageIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheDraftIsNotTheirs_ReturnsNotFound()
    {
        // Arrange
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["celestial-phb", "elvish-phb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BeforeARaceIsChosen_ReturnsRaceRequired()
    {
        // Arrange
        var draft = NewDraft();
        draft.SetBackground("acolyte");
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["celestial-phb", "elvish-phb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.RaceRequired");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_BeforeABackgroundIsChosen_ReturnsBackgroundRequired()
    {
        // Arrange
        var draft = NewDraft();
        draft.SetRace("dwarf");
        _draftRepositoryMock.Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(draft);

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["celestial-phb", "elvish-phb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.BackgroundRequired");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheRaceIsGone_ReturnsRaceNotFound()
    {
        // Arrange
        ArrangeDraft(raceId: "dwarf", backgroundId: "acolyte");
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("dwarf", _cts.Token)).ReturnsAsync((Race?)null);

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["celestial-phb", "elvish-phb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Race.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheBackgroundIsGone_ReturnsBackgroundNotFound()
    {
        // Arrange
        ArrangeDraft(raceId: "dwarf", backgroundId: "acolyte");
        ArrangeRace(RaceWithLanguages("dwarf", "Dwarf", ["Common", "Dwarvish"]));
        _backgroundRepositoryMock.Setup(r => r.GetByIdAsync("acolyte", _cts.Token)).ReturnsAsync((BackgroundReadModel?)null);

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["celestial-phb", "elvish-phb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("Background.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNeitherGivesAChoice_SavesNone()
    {
        // Arrange
        var draft = ArrangeDraft(raceId: "dwarf", backgroundId: "soldier");
        draft.SetLanguages(["celestial-phb"]);
        ArrangeRace(RaceWithLanguages("dwarf", "Dwarf", ["Common", "Dwarvish"]));
        ArrangeBackground(new BackgroundReadModel { Id = "soldier", Name = "Soldier", LanguageChoiceCount = 0 });

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["elvish-phb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.SelectedLanguageIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithTooFewLanguages_ReturnsInvalidLanguageCount()
    {
        // Arrange
        var draft = ArrangeHillDwarfAcolyte();

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["celestial-phb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.InvalidLanguageCount");
        draft.SelectedLanguageIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithTheSameLanguageTwice_ReturnsDuplicateLanguages()
    {
        // Arrange
        var draft = ArrangeHillDwarfAcolyte();

        // Act
        var result = await _sut.Handle(new ChooseLanguagesCommand(_draftId, ["celestial-phb", "celestial-phb"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.DuplicateLanguages");
        draft.SelectedLanguageIds.Should().BeEmpty();
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private CharacterDraft NewDraft()
    {
        var draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(RulesEdition.Edition2014);

        return draft;
    }
    private CharacterDraft ArrangeHillDwarfAcolyte()
    {
        var draft = ArrangeDraft(raceId: "dwarf", backgroundId: "acolyte");
        ArrangeRace(RaceWithLanguages("dwarf", "Dwarf", ["Common", "Dwarvish"]));
        ArrangeBackground(new BackgroundReadModel { Id = "acolyte", Name = "Acolyte", LanguageChoiceCount = 2 });
        return draft;
    }

    // A 2024 species has no languages; the background names Common and leaves two picks
    private CharacterDraft ArrangeDwarfAcolyte2024()
    {
        var draft = ArrangeDraft(raceId: "dwarf_2024", backgroundId: "acolyte_2024", edition: RulesEdition.Edition2024);
        ArrangeRace(new Race { Id = "dwarf_2024", Name = "Dwarf", Edition = RulesEdition.Edition2024, Source = "XPHB", Size = "Medium", Speed = 30 });
        ArrangeBackground(new BackgroundReadModel
        {
            Id = "acolyte_2024",
            Name = "Acolyte",
            Edition = RulesEdition.Edition2024,
            Source = "XPHB",
            LanguageChoiceCount = 2,
            Languages = [new BackgroundLanguageReadModel { Id = "common", Name = "Common" }],
        });
        return draft;
    }

    private CharacterDraft ArrangeDraft(string raceId, string backgroundId, string? subraceId = null, string edition = RulesEdition.Edition2014)
    {
        var draft = CharacterDraft.CreateEmpty(
            _userId,
            new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        draft.ChooseEdition(edition);
        draft.SetRace(raceId, subraceId);
        draft.SetBackground(backgroundId);

        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync(draft);

        return draft;
    }

    private void ArrangeRace(Race race) =>
        _raceRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync(race.Id, _cts.Token)).ReturnsAsync(race);

    private static Race RaceWithLanguages(string id, string name, string[] languages, int languageChoiceCount = 0) => new()
    {
        Id = id,
        Name = name,
        Edition = RulesEdition.Edition2014,
        Source = "PHB",
        Size = "Medium",
        Speed = 30,
        LanguageChoiceCount = languageChoiceCount,
        RaceLanguages = languages.Select(language => new RaceLanguage { RaceId = id, Language = language }).ToList(),
    };

    private void ArrangeBackground(BackgroundReadModel background) =>
        _backgroundRepositoryMock.Setup(r => r.GetByIdAsync(background.Id, _cts.Token)).ReturnsAsync(background);

    private static LanguageReadModel Language(string id, string name, string type) =>
        new() { Id = id, Name = name, Type = type };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}

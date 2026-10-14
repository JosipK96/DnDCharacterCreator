using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.SetAllowedBooks;
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

public class SetAllowedBooksCommandHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBookRepository> _bookRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IRaceRepository> _raceRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IBackgroundRepository> _backgroundRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<ISpellRepository> _spellRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly SetAllowedBooksCommandHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");

    public SetAllowedBooksCommandHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync(_cts.Token)).Returns(Task.CompletedTask);
        _bookRepositoryMock
            .Setup(r => r.GetAllForEditionAsync(RulesEdition.Edition2024, _cts.Token))
            .ReturnsAsync([Book("XPHB", isPlayersHandbook: true), Book("EFA"), Book("AU")]);

        _sut = new SetAllowedBooksCommandHandler(
            _draftRepositoryMock.Object,
            _bookRepositoryMock.Object,
            _raceRepositoryMock.Object,
            _classRepositoryMock.Object,
            _backgroundRepositoryMock.Object,
            _featRepositoryMock.Object,
            _spellRepositoryMock.Object,
            _userContextMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_WithBooksOfTheDraftsEdition_AllowsThemBesideThePlayersHandbook()
    {
        // Arrange
        var draft = ArrangeDraft();
        ArrangeFeatsAndSpells([], []);

        // Act
        var result = await _sut.Handle(new SetAllowedBooksCommand(_draftId, ["EFA", "AU"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.AllowedSources.Should().Equal("XPHB", "EFA", "AU");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithABookOfAnotherEditionOrNone_ReturnsUnknownBooks()
    {
        // Arrange - Xanathar's Guide is a 2014 book; "HOMEBREW" is no book at all
        var draft = ArrangeDraft();

        // Act
        var result = await _sut.Handle(new SetAllowedBooksCommand(_draftId, ["EFA", "XGE", "HOMEBREW"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Description.Should().Be("No 2024 book has the code XGE, HOMEBREW.");
        draft.AllowedSources.Should().Equal("XPHB");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NarrowingTheBooks_ClearsTheChoicesFromTheRemovedOnes()
    {
        // Arrange - a fighter with a style and a feat from Arcana Unleashed and a race from the
        // Player's Handbook, whose books now leave Arcana Unleashed out
        var draft = ArrangeDraft();
        draft.AllowBooks(["AU"], new DraftChoiceSources(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>()));
        draft.SetNameAndLevel("Tester", 4);
        draft.SetRace("elf_2024", "elf_drow_lineage_2024");
        draft.SetClass("fighter-2024");
        draft.SetFeats(["arcane_adept"]);
        draft.ChooseFightingStyles(["arcane_warrior", "defense_2024"]);

        _raceRepositoryMock.Setup(r => r.GetByIdWithSubracesAsync("elf_2024", _cts.Token)).ReturnsAsync(new Race
        {
            Id = "elf_2024",
            Name = "Elf",
            Edition = RulesEdition.Edition2024,
            Source = "XPHB",
            Size = "Medium",
            Subraces = [new Subrace { Id = "elf_drow_lineage_2024", Name = "Drow Lineage", Source = "XPHB", RaceId = "elf_2024" }],
        });
        _classRepositoryMock.Setup(r => r.GetByIdWithSubclassesAsync("fighter-2024", _cts.Token)).ReturnsAsync(new Class
        {
            Id = "fighter-2024",
            Name = "Fighter",
            Edition = RulesEdition.Edition2024,
            Source = "XPHB",
            HitDie = "1d10",
        });
        ArrangeFeatsAndSpells(
            [Feat("arcane_adept", "AU"), Feat("arcane_warrior", "AU"), Feat("defense_2024", "XPHB")],
            []);

        // Act
        var result = await _sut.Handle(new SetAllowedBooksCommand(_draftId, ["EFA"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        draft.AllowedSources.Should().Equal("XPHB", "EFA");
        draft.RaceId.Should().Be("elf_2024");
        draft.SubraceId.Should().Be("elf_drow_lineage_2024");
        draft.ClassId.Should().Be("fighter-2024");
        draft.SelectedFeatIds.Should().BeEmpty();
        draft.FightingStyleIds.Should().Equal("defense_2024");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(_cts.Token), Times.Once);
    }

    [Fact]
    public async Task Handle_WithAMissingDraft_ReturnsNotFound()
    {
        // Arrange
        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync((CharacterDraft?)null);

        // Act
        var result = await _sut.Handle(new SetAllowedBooksCommand(_draftId, ["EFA"]), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Code.Should().Be("CharacterDraft.NotFound");
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private CharacterDraft ArrangeDraft()
    {
        var draft = CharacterDraft.CreateEmpty(
            _userId,
            new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));

        _draftRepositoryMock
            .Setup(r => r.GetByIdForUpdateAsync(_draftId, _userId, _cts.Token))
            .ReturnsAsync(draft);

        return draft;
    }

    // Pinned to the feats and Fighting Styles the draft holds, and to its spells
    private void ArrangeFeatsAndSpells(List<FeatReadModel> feats, List<SpellReadModel> spells)
    {
        var featIds = feats.ConvertAll(f => f.Id);
        var spellIds = spells.ConvertAll(s => s.Id);

        _featRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.Is<IEnumerable<string>>(ids => ids.SequenceEqual(featIds)), _cts.Token))
            .ReturnsAsync(feats);
        _spellRepositoryMock
            .Setup(r => r.GetByIdsAsync(It.Is<List<string>>(ids => ids.SequenceEqual(spellIds)), _cts.Token))
            .ReturnsAsync(spells);
    }

    private static BookReadModel Book(string code, bool isPlayersHandbook = false) =>
        new() { Code = code, Name = code, Edition = RulesEdition.Edition2024, IsPlayersHandbook = isPlayersHandbook };

    private static FeatReadModel Feat(string id, string source) =>
        new() { Id = id, Name = id, Description = "A feat.", Edition = RulesEdition.Edition2024, Source = source };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}

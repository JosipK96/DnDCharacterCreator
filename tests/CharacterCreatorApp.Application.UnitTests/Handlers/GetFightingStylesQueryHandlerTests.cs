using CharacterCreatorApp.Application.Common.Interfaces;
using CharacterCreatorApp.Application.Features.CharacterCreation.GetFightingStyles;
using CharacterCreatorApp.Application.Persistence;
using CharacterCreatorApp.Application.ReadModels;
using CharacterCreatorApp.Domain.DomainModels.Characters;
using CharacterCreatorApp.Domain.DomainModels.Classes;
using CharacterCreatorApp.Domain.DomainModels.Feats;
using CharacterCreatorApp.Domain.ValueObjects;

using FluentAssertions;

using Microsoft.Extensions.Time.Testing;

using Moq;

namespace CharacterCreatorApp.Application.UnitTests.Handlers;

public class GetFightingStylesQueryHandlerTests : IDisposable
{
    private readonly Mock<ICharacterDraftRepository> _draftRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IClassRepository> _classRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IFeatRepository> _featRepositoryMock = new(MockBehavior.Strict);
    private readonly Mock<IUserContext> _userContextMock = new(MockBehavior.Strict);
    private readonly CancellationTokenSource _cts = new();
    private readonly GetFightingStylesQueryHandler _sut;

    private readonly Guid _userId = new("00000000-0000-0000-0000-000000000001");
    private readonly Guid _draftId = new("00000000-0000-0000-0000-000000000002");
    private readonly CharacterDraft _draft;

    public GetFightingStylesQueryHandlerTests()
    {
        _userContextMock.Setup(x => x.UserId).Returns(_userId);

        // A level 1 2024 fighter, offered the Player's Handbook's Defense and Arcana Unleashed's Arcane Warrior
        _draft = CharacterDraft.CreateEmpty(_userId, new FakeTimeProvider(new DateTimeOffset(2025, 1, 5, 14, 30, 0, TimeSpan.Zero)));
        _draft.SetNameAndLevel("Tester", 1);
        _draft.SetClass("fighter-2024");

        _draftRepositoryMock.Setup(r => r.GetByIdAsync(_draftId, _userId, _cts.Token)).ReturnsAsync(_draft);
        _classRepositoryMock.Setup(r => r.GetByIdWithDetailsAsync("fighter-2024", _cts.Token)).ReturnsAsync(new Class
        {
            Id = "fighter-2024",
            Name = "Fighter",
            Edition = RulesEdition.Edition2024,
            Source = "XPHB",
            HitDie = "1d10",
            Features = [new Feature { Id = "fighter-2024-1-fighting-style", Name = "Fighting Style", Description = string.Empty, Level = 1, ClassId = "fighter-2024", Type = "class" }],
        });
        _featRepositoryMock.Setup(r => r.GetAllAsync(RulesEdition.Edition2024, _cts.Token)).ReturnsAsync(
        [
            Style("defense_2024", "Defense", "XPHB"),
            Style("arcane_warrior", "Arcane Warrior", "AU"),
        ]);

        _sut = new GetFightingStylesQueryHandler(
            _draftRepositoryMock.Object,
            _classRepositoryMock.Object,
            _featRepositoryMock.Object,
            _userContextMock.Object);
    }

    [Fact]
    public async Task Handle_WithOnlyThePlayersHandbookAllowed_OffersOnlyItsStyles()
    {
        // Act
        var result = await _sut.Handle(new GetFightingStylesQuery(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Owed.Should().Be(1);
        result.Value.Options.Select(o => o.Id).Should().Equal("defense_2024");
    }

    [Fact]
    public async Task Handle_WithTheStylesBookAllowed_OffersItToo()
    {
        // Arrange
        _draft.AllowBooks(["AU"], new DraftChoiceSources(null, null, null, null, null, new Dictionary<string, string>(), new Dictionary<string, string>()));

        // Act
        var result = await _sut.Handle(new GetFightingStylesQuery(_draftId), _cts.Token);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Options.Select(o => o.Id).Should().Equal("arcane_warrior", "defense_2024");
    }

    private static FeatReadModel Style(string id, string name, string source) => new()
    {
        Id = id,
        Name = name,
        Description = "A Fighting Style.",
        Edition = RulesEdition.Edition2024,
        Source = source,
        Category = FeatCategory.FightingStyle,
        Prerequisites = new FeatPrerequisites { Features = ["Fighting Style"] },
    };

    public void Dispose()
    {
        _cts.Dispose();
        GC.SuppressFinalize(this);
    }
}
